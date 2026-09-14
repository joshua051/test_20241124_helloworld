"""Cycles/CPU rendering of the same skinned mesh data consumed by Unity. Blender 4+."""
import bpy,json,math,hashlib
from pathlib import Path
from mathutils import Matrix,Vector
ROOT=Path.cwd();SOURCE=ROOT/'Assets/Resources/Gladiators';OUT=ROOT/'ArtExports';OUT.mkdir(exist_ok=True)
DATA=json.loads((SOURCE/'Gladiator.json').read_text());C=Matrix.Rotation(math.pi/2,4,'X');CI=C.inverted()
def convert(v):return C@Vector(v)
def rot(v):
 x,y,z=(math.radians(a) for a in v)
 return Matrix.Rotation(y,4,'Y')@Matrix.Rotation(x,4,'X')@Matrix.Rotation(z,4,'Z')
def preset(name):return [(v['x'],v['y'],v['z']) for v in next(p for p in DATA['poses'] if p['name']==name)['euler']]
def material(spec,enemy=False):
 mat=bpy.data.materials.new(spec['name']+('_enemy' if enemy else ''));mat.use_nodes=True;nodes=mat.node_tree.nodes;node=nodes.get('Principled BSDF')
 color=spec['color'] if not(enemy and spec['teamTint']) else [.46,.12,.07,1]
 node.inputs['Base Color'].default_value=color;node.inputs['Metallic'].default_value=spec['metallic'];node.inputs['Roughness'].default_value=spec['roughness']
 if spec['texture']:
  texture=nodes.new('ShaderNodeTexImage');texture.image=bpy.data.images.load(str(SOURCE/(spec['texture']+'.png')),check_existing=True);texture.interpolation='Linear';mat.node_tree.links.new(texture.outputs['Color'],node.inputs['Base Color'])
 return mat

def mesh_object(name,source,materials,rig,bone=None,offset=None):
 vertices=[Vector(source['vertices'][i:i+3]) for i in range(0,len(source['vertices']),3)]
 if bone is not None:
  bind=Matrix.Translation(Vector(DATA['bones'][bone]['position']))@offset;vertices=[bind@v for v in vertices]
 faces=[];material_indices=[]
 for group in source['submeshes']:
  t=group['triangles'];faces.extend([tuple(t[i:i+3]) for i in range(0,len(t),3)]);material_indices.extend([group['material']]*(len(t)//3))
 mesh=bpy.data.meshes.new(name);mesh.from_pydata([convert(v) for v in vertices],[],faces);mesh.update()
 obj=bpy.data.objects.new(name,mesh);bpy.context.collection.objects.link(obj)
 for mat in materials:mesh.materials.append(mat)
 uv=mesh.uv_layers.new(name='UVMap')
 for poly,mi in zip(mesh.polygons,material_indices):
  poly.material_index=mi;poly.use_smooth=True
  for li in poly.loop_indices:
   vi=mesh.loops[li].vertex_index;uv.data[li].uv=source['uv'][vi*2:vi*2+2]
 normals=[Vector(source['normals'][i:i+3]) for i in range(0,len(source['normals']),3)]
 if bone is not None:normals=[offset.to_3x3()@v for v in normals]
 mesh.normals_split_custom_set_from_vertices([convert(n).normalized() for n in normals])
 for b in DATA['bones']:obj.vertex_groups.new(name=b['name'])
 for i in range(len(vertices)):
  if bone is not None:obj.vertex_groups[bone].add([i],1,'REPLACE')
  else:
   for j in range(4):
    weight=source['weights'][i*4+j];index=source['boneIndices'][i*4+j]
    if weight>0:obj.vertex_groups[index].add([i],weight,'REPLACE')
 modifier=obj.modifiers.new('Authoritative_LBS','ARMATURE');modifier.object=rig;modifier.use_deform_preserve_volume=False
 obj.parent=rig;obj.matrix_parent_inverse=Matrix.Identity(4)
 return obj

def actor(name,location,yaw=0,enemy=False,shield=True):
 bpy.ops.object.select_all(action='DESELECT');arm=bpy.data.armatures.new(name+'_Skeleton');rig=bpy.data.objects.new(name,arm);bpy.context.collection.objects.link(rig)
 bpy.context.view_layer.objects.active=rig;rig.select_set(True);bpy.ops.object.mode_set(mode='EDIT')
 for i,b in enumerate(DATA['bones']):
  eb=arm.edit_bones.new(b['name']);eb.head=convert(b['position']);eb.tail=eb.head+Vector((0,.1,0))
  if b['parent']>=0:eb.parent=arm.edit_bones[DATA['bones'][b['parent']]['name']]
  eb.use_connect=False
 bpy.ops.object.mode_set(mode='OBJECT');mats=[material(m,enemy) for m in DATA['materials']]
 mesh_object(name+'_Body',DATA['body'],mats,rig)
 socket=DATA['swordSocket'];offset=Matrix.Translation(Vector(socket['position']))@rot(socket['euler'])@Matrix.Translation(Vector((0,0,.48)))
 mesh_object(name+'_Gladius',DATA['sword'],mats,rig,9,offset)
 if shield:
  socket=DATA['shieldSocket'];offset=Matrix.Translation(Vector(socket['position']))@rot(socket['euler']);mesh_object(name+'_Shield',DATA['shield'],mats,rig,6,offset)
 rig.location=location;rig.rotation_euler.z=math.radians(yaw);return rig

def pose(rig,name,frame=None):
 for b,euler in zip(DATA['bones'],preset(name)):
  p=rig.pose.bones[b['name']];p.rotation_mode='QUATERNION';p.rotation_quaternion=(C@rot(euler)@CI).to_quaternion()
  if frame is not None:p.keyframe_insert(data_path='rotation_quaternion',frame=frame)
 bpy.context.view_layer.update()
def area(name,location,power,size,target=(0,0,1)):
 light=bpy.data.lights.new(name,'AREA');light.energy=power;light.shape='DISK';light.size=size
 obj=bpy.data.objects.new(name,light);bpy.context.collection.objects.link(obj);obj.location=location;obj.rotation_euler=(Vector(target)-obj.location).to_track_quat('-Z','Y').to_euler();return obj
def camera(location,focus,orthographic=False,scale=3):
 obj=bpy.context.scene.camera;obj.location=location;obj.rotation_euler=(Vector(focus)-obj.location).to_track_quat('-Z','Y').to_euler();obj.data.type='ORTHO' if orthographic else 'PERSP';obj.data.ortho_scale=scale;obj.data.lens=52
def render(name,x,y):
 scene=bpy.context.scene;scene.render.resolution_x=x;scene.render.resolution_y=y;scene.render.filepath=str(OUT/(name+'.png'));bpy.ops.render.render(write_still=True)

bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.device='CPU';scene.cycles.samples=64;scene.cycles.use_denoising=False
scene.render.resolution_percentage=100;scene.render.image_settings.file_format='PNG';scene.world.color=(.12,.12,.12);scene.view_settings.view_transform='AgX';scene.render.film_transparent=False;scene.render.fps=30
hero=actor('IronSand_Gladiator',(0,0,1))
# Demonstration action, not a complete combat animation library.
for name,frame in [('idle',1),('guard',21),('idle',41),('windup',56),('strike',65),('idle',85)]:pose(hero,name,frame)
scene.frame_start=1;scene.frame_end=85;scene.frame_set(1)
bpy.ops.object.select_all(action='DESELECT');hero.select_set(True)
for child in hero.children:child.select_set(True)
bpy.context.view_layer.objects.active=hero
bpy.ops.export_scene.fbx(filepath=str(OUT/'IronSand_Gladiator.fbx'),use_selection=True,add_leaf_bones=False,axis_forward='-Z',axis_up='Y',path_mode='COPY',embed_textures=True,bake_anim=True,bake_anim_use_all_bones=True,bake_anim_use_all_actions=False,bake_anim_use_nla_strips=False)
bpy.ops.export_scene.gltf(filepath=str(OUT/'IronSand_Gladiator.glb'),export_format='GLB',use_selection=True,export_animations=True)
hero.animation_data_clear()
mat=bpy.data.materials.new('ArenaSand');mat.use_nodes=True;node=mat.node_tree.nodes['Principled BSDF'];node.inputs['Base Color'].default_value=(.28,.185,.09,1);node.inputs['Roughness'].default_value=.95
noise=mat.node_tree.nodes.new('ShaderNodeTexNoise');noise.inputs['Scale'].default_value=120
bump=mat.node_tree.nodes.new('ShaderNodeBump');bump.inputs['Strength'].default_value=.25;bump.inputs['Distance'].default_value=.03
mat.node_tree.links.new(noise.outputs['Fac'],bump.inputs['Height']);mat.node_tree.links.new(bump.outputs['Normal'],node.inputs['Normal'])
bpy.ops.mesh.primitive_plane_add(size=50);bpy.context.object.name='Sand';bpy.context.object.data.materials.append(mat)
area('Key',(3,-4,5),800,4);area('Fill',(-3,-2,3),400,4);area('Rim',(1,3,4),1100,3)
bpy.ops.object.camera_add();scene.camera=bpy.context.object
pose(hero,'guard');camera((3,-5,2.7),(0,0,1),True,2.8);render('gladiator_guard',1000,1200)
pose(hero,'idle');camera((-3,5,2.6),(0,0,1),True,2.8);render('gladiator_rear',1000,1200)
pose(hero,'strike');camera((3,-5,2.7),(0,0,1),True,3);render('gladiator_strike',1000,1200)
# Separate offline presentation stage, not a Unity screenshot.
stone=bpy.data.materials.new('ArenaStone');stone.use_nodes=True;stone.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value=(.24,.19,.13,1)
for i in range(24):
 a=2*math.pi*i/24;bpy.ops.mesh.primitive_cube_add(size=1,location=(math.cos(a)*8,math.sin(a)*8,1.3));wall=bpy.context.object;wall.name='Offline_ArenaWall';wall.dimensions=(2.25,.6,2.6);wall.rotation_euler.z=a+math.pi/2;wall.data.materials.append(stone)
hero.location=(-1,0,1);hero.rotation_euler.z=math.radians(90);pose(hero,'guard')
foe=actor('Opponent',(1.2,0,1),-90,True,False);pose(foe,'windup')
area('ArenaKey',(0,-4,8),1700,5);camera((5,-8,4.2),(0,0,1));render('gladiator_arena',1440,900)
bpy.ops.file.pack_all();bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'IronSand_Gladiator_Stage.blend'))
report={'status':'BLENDER_RENDERED_NOT_UNITY_VALIDATED','blender_version':bpy.app.version_string,'engine':'Cycles CPU','samples':64,'denoising':False,'source_data_sha256':hashlib.sha256((SOURCE/'Gladiator.json').read_bytes()).hexdigest(),'bones':len(DATA['bones']),'source_credits':['BlackScorp / Low poly warrior / CC0','Astarribadebirra / Nando / Gladiator Pack / CC0'],'files':[]}
for path in sorted(OUT.iterdir()):
 if path.is_file():report['files'].append({'name':path.name,'bytes':path.stat().st_size,'sha256':hashlib.sha256(path.read_bytes()).hexdigest()})
(OUT/'render_manifest.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report,indent=2))
