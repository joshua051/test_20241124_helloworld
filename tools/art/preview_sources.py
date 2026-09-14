"""Run with Blender --background --factory-startup --disable-autoexec --python."""
import bpy
import json
import math
from pathlib import Path
from mathutils import Vector

OUT = Path('ArtInspection').resolve()
source = OUT / 'low-poly-warrior/base-char-male.obj'
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
if bpy.app.version >= (3, 3, 0):
    bpy.ops.wm.obj_import(filepath=str(source), forward_axis='NEGATIVE_Z', up_axis='Y')
else:
    bpy.ops.import_scene.obj(filepath=str(source), axis_forward='-Z', axis_up='Y')
meshes = [obj for obj in bpy.context.scene.objects if obj.type == 'MESH']
report = {'renderer': 'Blender Cycles CPU', 'blender_version': bpy.app.version_string, 'source': str(source.name), 'meshes': []}
for obj in meshes:
    obj.data.calc_loop_triangles()
    points = [obj.matrix_world @ Vector(corner) for corner in obj.bound_box]
    report['meshes'].append({'name': obj.name, 'vertices': len(obj.data.vertices), 'triangles': len(obj.data.loop_triangles), 'materials': [slot.material.name if slot.material else None for slot in obj.material_slots], 'bounds_min': [min(p[i] for p in points) for i in range(3)], 'bounds_max': [max(p[i] for p in points) for i in range(3)]})
    for polygon in obj.data.polygons:
        polygon.use_smooth = False
for mat in bpy.data.materials:
    if not mat.use_nodes:
        mat.use_nodes = True
    node = mat.node_tree.nodes.get('Principled BSDF')
    if node:
        node.inputs['Metallic'].default_value = 0.65 if 'Metal' in mat.name else 0.0
        node.inputs['Roughness'].default_value = 0.38 if 'Metal' in mat.name else 0.65
        for key in ('Emission Color', 'Emission'):
            if key in node.inputs:
                node.inputs[key].default_value = (0, 0, 0, 1)
scene = bpy.context.scene
scene.render.engine = 'CYCLES'
scene.cycles.device = 'CPU'
scene.cycles.samples = 24
scene.cycles.use_denoising = True
scene.render.resolution_x = 720
scene.render.resolution_y = 900
scene.render.resolution_percentage = 100
scene.world.color = (0.10, 0.10, 0.10)
scene.render.image_settings.file_format = 'PNG'
bpy.ops.mesh.primitive_plane_add(size=200, location=(0, 0, -0.005))
floor = bpy.context.object
floor.name = 'StudioFloor'
mat = bpy.data.materials.new('StudioFloorMaterial'); mat.use_nodes = True
mat.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value = (0.075, 0.085, 0.11, 1)
mat.node_tree.nodes['Principled BSDF'].inputs['Roughness'].default_value = 0.75
floor.data.materials.append(mat)
def area(name, location, power, size):
    data = bpy.data.lights.new(name, 'AREA'); data.energy = power; data.shape = 'DISK'; data.size = size
    obj = bpy.data.objects.new(name, data); scene.collection.objects.link(obj); obj.location = location
    obj.rotation_euler = (Vector((0, 0, 0.9)) - obj.location).to_track_quat('-Z', 'Y').to_euler()
area('Key', (3, -4, 5), 700, 4)
area('Fill', (-3, -2, 3), 400, 4)
area('Rim', (0, 3, 4), 900, 3)
data = bpy.data.cameras.new('InspectionCamera'); camera = bpy.data.objects.new('InspectionCamera', data); scene.collection.objects.link(camera)
data.type = 'ORTHO'; data.ortho_scale = 2.6; scene.camera = camera
for label, location in [('front', (3, -6, 2.5)), ('back', (-3, 6, 2.5))]:
    camera.location = location; camera.rotation_euler = (Vector((0, 0, 0.9)) - camera.location).to_track_quat('-Z', 'Y').to_euler()
    scene.render.filepath = str(OUT / ('warrior_' + label + '.png'))
    bpy.ops.render.render(write_still=True)
(OUT / 'blender_mesh_report.json').write_text(json.dumps(report, indent=2) + '\n', encoding='utf-8')
print(json.dumps(report, indent=2))
