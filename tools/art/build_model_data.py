#!/usr/bin/env python3
"""Compile hash-verified CC0 OBJ geometry for Unity and Blender. No source code execution."""
from __future__ import annotations
import argparse, hashlib, json, math, shutil
from pathlib import Path
HASHES={'gladiator-pack.zip':'b341a28b9e777588bd218457ace9c37ce7fa2ec97e16d0690c3cca1da4d281ec','low-poly-warrior.zip':'7f6ecd8044093b6c8ab2e594224a3524c2510317ae0b8ec6d2cf742877137c04'}
SCALE=1.08
BONES=[('Hips',-1,(0,.93,.015)),('Spine',0,(0,1.15,.015)),('Chest',1,(0,1.37,.02)),('Head',2,(0,1.53,.055)),('LeftUpperArm',2,(-.205,1.425,.025)),('LeftForearm',4,(-.465,1.275,.025)),('LeftHand',5,(-.688,1.147,.028)),('RightUpperArm',2,(.205,1.425,.025)),('RightForearm',7,(.465,1.275,.025)),('RightHand',8,(.688,1.147,.028)),('LeftThigh',0,(-.105,.90,.008)),('LeftShin',10,(-.125,.50,.045)),('LeftFoot',11,(-.130,.09,.025)),('RightThigh',0,(.105,.90,.008)),('RightShin',13,(.125,.50,.045)),('RightFoot',14,(.130,.09,.025))]
def clamp(x):return max(0.,min(1.,x))
def mix(a,b,t):
 t=clamp(t)
 return [(a,1-t),(b,t)] if 0<t<1 else [(b if t>=1 else a,1.)]
def weights(v,obj):
 x,y,z=v;ax=abs(x);side=0 if x<0 else 3
 if obj.startswith('helmet') or y>=1.555:return [(3,1.)]
 if ax>.22 and y>1.03:
  if ax<.30:return mix(2,4+side,(ax-.22)/.08)
  if ax<.425:return [(4+side,1.)]
  if ax<.515:return mix(4+side,5+side,(ax-.425)/.09)
  if ax<.66:return [(5+side,1.)]
  return mix(5+side,6+side,(ax-.66)/.075)
 if obj.startswith('torso') and y<1.015:return [(0,1.)]
 if y<.87:
  leg=10 if x<0 else 13
  if y<.10:return [(leg+2,1.)]
  if y<.17:return mix(leg+2,leg+1,(y-.10)/.07)
  if y<.445:return [(leg+1,1.)]
  if y<.565:return mix(leg+1,leg,(y-.445)/.12)
  if y<.78:return [(leg,1.)]
  return mix(leg,0,(y-.78)/.09)
 if y<1.08:return [(0,1.)]
 if y<1.24:return mix(0,1,(y-1.08)/.16)
 if y<1.40:return mix(1,2,(y-1.24)/.16)
 return mix(2,3,(y-1.48)/.075)
def obj_mesh(path,transform,material_for,skinned=False):
 positions=[];tex=[];norms=[];current='';material='';faces=[]
 for line in path.read_text(encoding='utf-8').splitlines():
  t=line.split()
  if not t or t[0].startswith('#'):continue
  if t[0]=='v':positions.append(tuple(map(float,t[1:4])))
  elif t[0]=='vt':tex.append(tuple(map(float,t[1:3])))
  elif t[0]=='vn':norms.append(tuple(map(float,t[1:4])))
  elif t[0]=='o':current=t[1]
  elif t[0]=='usemtl':material=t[1]
  elif t[0]=='f':faces.append((current,material,t[1:]))
 result={'vertices':[],'normals':[],'uv':[],'boneIndices':[],'weights':[],'submeshes':[]};groups={};unique={}
 def index(value,n):
  k=int(value);out=k-1 if k>0 else n+k
  if not 0<=out<n:raise ValueError('OBJ index out of range')
  return out
 for obj,mat,corners in faces:
  tokens=[]
  for token in corners:
   s=token.split('/');vi=index(s[0],len(positions));ti=index(s[1],len(tex)) if len(s)>1 and s[1] else -1;ni=index(s[2],len(norms)) if len(s)>2 and s[2] else -1;tokens.append((vi,ti,ni))
  center=[sum(positions[v[0]][i] for v in tokens)/len(tokens) for i in range(3)];tri_group=groups.setdefault(material_for(obj,mat,center),[]);ids=[]
  for vi,ti,ni in tokens:
   key=(vi,ti,ni,obj)
   if key not in unique:
    unique[key]=len(result['vertices'])//3
    point,normal=transform(positions[vi],norms[ni] if ni>=0 else (0,1,0))
    result['vertices'].extend(round(a,7) for a in point);result['normals'].extend(round(a,7) for a in normal);result['uv'].extend(tex[ti] if ti>=0 else (0,0))
    ws=weights(positions[vi],obj) if skinned else [(0,1.)];ws=[p for p in ws if p[1]>0]
    result['boneIndices'].extend([p[0] for p in ws]+[0]*(4-len(ws)));result['weights'].extend([round(p[1],7) for p in ws]+[0.]*(4-len(ws)))
   ids.append(unique[key])
  for j in range(1,len(ids)-1):tri_group.extend((ids[0],ids[j],ids[j+1]))
 result['submeshes']=[{'material':mat,'triangles':indices} for mat,indices in sorted(groups.items())]
 return result

def validate(data):
 assert data['schemaVersion']==1
 for i,b in enumerate(data['bones']):assert -1<=b['parent']<i and all(math.isfinite(v) for v in b['position'])
 counts={}
 for label in ('body','sword','shield'):
  mesh=data[label];n=len(mesh['vertices'])//3
  assert 0<n<65000 and len(mesh['normals'])==n*3 and len(mesh['uv'])==n*2
  assert len(mesh['weights'])==len(mesh['boneIndices'])==n*4
  assert all(math.isfinite(v) for k in ('vertices','normals','uv','weights') for v in mesh[k])
  for i in range(n):
   w=mesh['weights'][i*4:i*4+4];bi=mesh['boneIndices'][i*4:i*4+4]
   assert abs(sum(w)-1)<.00001 and min(w)>=0 and all(0<=j<len(data['bones']) for j in bi)
  for group in mesh['submeshes']:
   assert 0<=group['material']<len(data['materials']) and len(group['triangles'])%3==0 and all(0<=j<n for j in group['triangles'])
  counts[label]={'vertices':n,'triangles':sum(len(s['triangles'])//3 for s in mesh['submeshes'])}
 return counts

def build(source,out):
 for name,digest in HASHES.items():
  if hashlib.sha256((source/name).read_bytes()).hexdigest()!=digest:raise ValueError('Archive hash mismatch: '+name)
 for archive in json.loads((source/'manifest.json').read_text()):
  for item in archive['files']:
   p=source/archive['id']/item['path']
   if hashlib.sha256(p.read_bytes()).hexdigest()!=item['sha256']:raise ValueError('Extracted hash mismatch: '+str(p))
 mats=[{'name':'Skin','color':[1,1,1,1],'metallic':0,'roughness':.78,'texture':'skin','teamTint':False},{'name':'Steel','color':[.82,.84,.86,1],'metallic':.7,'roughness':.36,'texture':'metal2','teamTint':False},{'name':'TeamLeather','color':[.20,.32,.48,1],'metallic':0,'roughness':.75,'texture':'','teamTint':True},{'name':'Bronze','color':[.48,.28,.095,1],'metallic':.7,'roughness':.33,'texture':'','teamTint':False},{'name':'Blade','color':[.62,.67,.70,1],'metallic':.8,'roughness':.27,'texture':'','teamTint':False},{'name':'DarkLeather','color':[.055,.024,.012,1],'metallic':0,'roughness':.8,'texture':'','teamTint':False}]
 bones=[{'name':n,'parent':p,'position':[round(v[0]*SCALE,7),round((v[1]+.00044)*SCALE-1,7),round(v[2]*SCALE,7)]} for n,p,v in BONES]
 body=obj_mesh(source/'low-poly-warrior/base-char-male.obj',lambda v,n:((v[0]*SCALE,(v[1]+.00044)*SCALE-1,v[2]*SCALE),n),lambda o,m,c:0 if m=='default' else 2 if o.startswith('torso') and c[1]<1.015 else 1,True)
 pack=source/'gladiator-pack/GladiatorPack/OBJ'
 sword=obj_mesh(pack/'Gladii.obj',lambda v,n:((v[0]*2.1,-v[2]*2.1,(v[1]-.05)*2.1-.48),(n[0],-n[2],n[1])),lambda o,m,c:4 if m=='Metal' else 5 if m=='Belt2' else 3)
 shield=obj_mesh(pack/'Shield.obj',lambda v,n:((v[0]*1.55,(v[1]-.275)*1.55,-v[2]*1.55),(n[0],n[1],-n[2])),lambda o,m,c:3 if m=='Metal' else 2)
 for group in shield['submeshes']:
  t=group['triangles']
  for i in range(0,len(t),3):t[i+1],t[i+2]=t[i+2],t[i+1]
 data={'schemaVersion':1,'name':'IronSand_CC0_Gladiator_v1','axes':'Y-up +Z-forward; CCW triangles; feet -1m','bones':bones,'materials':mats,'body':body,'sword':sword,'shield':shield}
 def pose(name,changes):return {'name':name,'euler':[{'x':changes.get(i,(0,0,0))[0],'y':changes.get(i,(0,0,0))[1],'z':changes.get(i,(0,0,0))[2]} for i in range(len(bones))]}
 idle={2:(0,-8,0),3:(0,8,0),4:(-8,0,55),5:(0,35,0),6:(-8,0,0),7:(-10,0,-55),8:(0,-35,0),9:(-8,0,0)}
 guard={**idle,2:(0,-15,0),3:(0,15,0),4:(-35,-10,40),5:(0,55,0),6:(-12,0,0),7:(-18,0,-42),8:(0,-45,0)}
 windup={**idle,2:(0,-28,0),3:(0,15,0),7:(-35,25,-50),8:(0,-30,0)}
 strike={**idle,2:(0,28,-4),3:(0,-12,0),7:(-75,-35,-10),8:(0,-60,0)}
 data['poses']=[pose('idle',idle),pose('guard',guard),pose('windup',windup),pose('strike',strike)]
 data['swordSocket']={'position':[.06,-.038,.015],'euler':[0,0,0]};data['shieldSocket']={'position':[-.035,-.015,.12],'euler':[0,0,-55]}
 counts=validate(data);out.mkdir(parents=True,exist_ok=True)
 (out/'Gladiator.json').write_text(json.dumps(data,separators=(',',':'),allow_nan=False)+'\n',encoding='utf-8')
 for name in ('skin.png','metal2.png'):shutil.copyfile(source/'low-poly-warrior'/name,out/name)
 report={'schema':1,'status':'DATA_VALIDATED_NOT_UNITY_VALIDATED','archives':HASHES,'model_sha256':hashlib.sha256((out/'Gladiator.json').read_bytes()).hexdigest(),'bones':len(bones),'meshes':counts,'bounds_note':'actor feet -1m; not an engine collision test'}
 (out/'asset_manifest.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8');print(json.dumps(report,indent=2));return data
if __name__=='__main__':
 p=argparse.ArgumentParser();p.add_argument('--source',type=Path,default=Path('ArtInspection'));p.add_argument('--out',type=Path,default=Path('Assets/Resources/Gladiators'));a=p.parse_args();build(a.source,a.out)
