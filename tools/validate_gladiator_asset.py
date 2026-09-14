#!/usr/bin/env python3
"""Offline asset-data regression checks. Not a Unity compilation or visual-quality gate."""
from __future__ import annotations
import hashlib
import json
import math
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]

def mul(a, b):
    return [[sum(a[r][k] * b[k][c] for k in range(3)) for c in range(3)] for r in range(3)]

def mv(a, v):
    return [sum(a[r][k] * v[k] for k in range(3)) for r in range(3)]

def add(a, b): return [a[i] + b[i] for i in range(3)]
def sub(a, b): return [a[i] - b[i] for i in range(3)]
def length(v): return math.sqrt(sum(x * x for x in v))

def rotation(e):
    x, y, z = (math.radians(e[key]) for key in ('x', 'y', 'z'))
    cx, sx, cy, sy, cz, sz = math.cos(x), math.sin(x), math.cos(y), math.sin(y), math.cos(z), math.sin(z)
    return mul(mul([[cy, 0, sy], [0, 1, 0], [-sy, 0, cy]], [[1, 0, 0], [0, cx, -sx], [0, sx, cx]]), [[cz, -sz, 0], [sz, cz, 0], [0, 0, 1]])

def require(value, message):
    if not value: raise RuntimeError(message)

def validate():
    path = ROOT / 'Assets/Resources/Gladiators/Gladiator.json'
    data = json.loads(path.read_text(encoding='utf-8'))
    manifest = json.loads(path.with_name('asset_manifest.json').read_text())
    require(hashlib.sha256(path.read_bytes()).hexdigest() == manifest['model_sha256'], 'Data hash differs from manifest')
    require(data['schemaVersion'] == 1 and len(data['bones']) == 16, 'Unexpected skeleton schema')
    for i, bone in enumerate(data['bones']):
        require(-1 <= bone['parent'] < i, 'Invalid parent ordering')
    totals = {}
    for label in ('body', 'sword', 'shield'):
        mesh = data[label]; count = len(mesh['vertices']) // 3
        require(len(mesh['normals']) == count * 3 and len(mesh['uv']) == count * 2, 'Invalid mesh dimensions')
        require(len(mesh['boneIndices']) == len(mesh['weights']) == count * 4, 'Invalid skin dimensions')
        for key in ('vertices', 'normals', 'uv', 'weights'):
            require(all(math.isfinite(x) for x in mesh[key]), 'Non-finite mesh value')
        for i in range(count):
            weights = mesh['weights'][i * 4:i * 4 + 4]
            require(min(weights) >= 0 and abs(sum(weights) - 1) < .00001, 'Invalid weight sum')
            require(all(0 <= b < 16 for b in mesh['boneIndices'][i * 4:i * 4 + 4]), 'Invalid bone reference')
        for group in mesh['submeshes']:
            require(0 <= group['material'] < len(data['materials']), 'Invalid material reference')
            require(len(group['triangles']) % 3 == 0 and all(0 <= x < count for x in group['triangles']), 'Invalid triangle index')
        totals[label] = {'vertices': count, 'triangles': sum(len(s['triangles']) // 3 for s in mesh['submeshes'])}
    require(totals == {'body': {'vertices': 2270, 'triangles': 2222}, 'sword': {'vertices': 258, 'triangles': 120}, 'shield': {'vertices': 322, 'triangles': 164}}, 'Unexpected topology change')
    mesh = data['body']; vertices = [mesh['vertices'][i:i + 3] for i in range(0, len(mesh['vertices']), 3)]
    edges = set()
    for group in mesh['submeshes']:
        t = group['triangles']
        for i in range(0, len(t), 3):
            for a, b in ((t[i], t[i + 1]), (t[i + 1], t[i + 2]), (t[i + 2], t[i])):
                if a != b: edges.add(tuple(sorted((a, b))))
    edges = [(a, b, length(sub(vertices[a], vertices[b]))) for a, b in edges]
    edges = [(a, b, size) for a, b, size in edges if size > .004]
    poses = []
    for pose in data['poses']:
        require(len(pose['euler']) == 16, 'Incomplete pose')
        transforms = []
        for i, bone in enumerate(data['bones']):
            parent = bone['parent']; r = rotation(pose['euler'][i])
            offset = sub(bone['position'], data['bones'][parent]['position']) if parent >= 0 else bone['position']
            if parent >= 0:
                pr, pt = transforms[parent]; r, offset = mul(pr, r), add(pt, mv(pr, offset))
            transforms.append((r, offset))
        posed = []
        for i, vertex in enumerate(vertices):
            result = [0., 0., 0.]
            for j in range(4):
                weight = mesh['weights'][i * 4 + j]
                if weight == 0: continue
                b = mesh['boneIndices'][i * 4 + j]; r, t = transforms[b]
                point = add(mv(r, sub(vertex, data['bones'][b]['position'])), t)
                result = add(result, [x * weight for x in point])
            posed.append(result)
        maximum = max(length(sub(posed[a], posed[b])) / size for a, b, size in edges)
        require(maximum < 4., 'Severe disconnected skin deformation in ' + pose['name'])
        poses.append({'name': pose['name'], 'maximum_edge_stretch_ratio': round(maximum, 4)})
    for material in data['materials']:
        if material['texture']: require(path.with_name(material['texture'] + '.png').is_file(), 'Missing texture dependency')
    result = {'status': 'OFFLINE_ASSET_DATA_PASS_UNITY_NOT_RUN', 'model_sha256': manifest['model_sha256'], 'bones': 16, 'meshes': totals, 'key_pose_deformation': poses}
    print(json.dumps(result, indent=2))
    return result

if __name__ == '__main__': validate()
