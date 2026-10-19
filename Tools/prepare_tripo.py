import bpy, json, math, struct
from pathlib import Path
from mathutils import Vector, Matrix

ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'Assets/TripoProps'
PREVIEW=ROOT.parent/'output/model_integration_20261018'
NAMES=['industrial+panel','vintage+dial+timer','vintage+calculator','space+station+tile','old+parchment+folder','metal+cabinet','golden+key','metal+doorbell+button','vintage+submarine+lantern','industrial+connector+base','industrial+panel_1','metal+tray','metal+storage+box','vintage+metal+toolbox','rusty+framed+mirror','metal+stool','industrial+monitor','old+light+switch']
OUT.mkdir(parents=True,exist_ok=True);PREVIEW.mkdir(parents=True,exist_ok=True)
report=[]
for name in NAMES:
    fn=name.replace('_1','')+'+3d+model'+(' (1)' if name.endswith('_1') else '')+'.glb'
    src=Path('/Users/xiaobang/Downloads')/fn
    id=name.replace('+','_');dest=OUT/id;dest.mkdir(exist_ok=True)
    bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
    bpy.ops.import_scene.gltf(filepath=str(src))
    objs=[o for o in bpy.context.scene.objects if o.type=='MESH']
    verts=[o.matrix_world@Vector(c) for o in objs for c in o.bound_box]
    lo=Vector(tuple(min(v[i] for v in verts) for i in range(3)));hi=Vector(tuple(max(v[i] for v in verts) for i in range(3)))
    center=(lo+hi)/2;size=hi-lo
    for o in objs:
        o.location-=center
    # Tripo exports use inconsistent front axes. Normalize the observed fronts.
    if id not in ('metal_stool','rusty_framed_mirror'):
        rot=Matrix.Rotation(-math.pi/2,4,'Z')
        for o in objs:o.matrix_world=rot@o.matrix_world
        size=Vector((size.y,size.x,size.z))
    original=sum(len(o.data.polygons) for o in objs)
    # Keep close-up Room 2 meshes intact: collapse decimation tears their UV islands.
    preserve_mesh = id in {'vintage_metal_toolbox','metal_tray','old_parchment_folder','metal_stool','industrial_monitor'}
    for o in ([] if preserve_mesh else objs):
        bpy.context.view_layer.objects.active=o
        mod=o.modifiers.new('GameMesh','DECIMATE');mod.ratio=min(1,14000/max(original,1))
        bpy.ops.object.modifier_apply(modifier=mod.name)
    # Extract embedded original texture maps by glTF material role.
    raw=src.read_bytes();jl=struct.unpack_from('<I',raw,12)[0];g=json.loads(raw[20:20+jl]);binstart=20+jl+8
    mat=g['materials'][0];pbr=mat.get('pbrMetallicRoughness',{})
    maps={}
    for role,info in [('BaseColor',pbr.get('baseColorTexture')),('Normal',mat.get('normalTexture')),('MetalRough',pbr.get('metallicRoughnessTexture'))]:
        if not info:continue
        image=g['images'][g['textures'][info['index']]['source']];bv=g['bufferViews'][image['bufferView']]
        ext='.png' if image.get('mimeType')=='image/png' else '.jpg'
        path=dest/(role+ext);start=binstart+bv.get('byteOffset',0);path.write_bytes(raw[start:start+bv['byteLength']]);maps[role]=path.name
    bpy.ops.object.select_all(action='DESELECT')
    for o in objs:o.select_set(True)
    bpy.ops.export_scene.fbx(filepath=str(dest/(id+'.fbx')),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,bake_space_transform=False,add_leaf_bones=False,path_mode='AUTO',use_mesh_modifiers=True)
    # Render a front view; glTF +Z becomes Blender -Y.
    scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=12
    scene.render.resolution_x=512;scene.render.resolution_y=512;scene.render.resolution_percentage=100
    scene.world.color=(0.35,0.35,0.35)
    bpy.ops.object.camera_add(location=(0,-max(size)*3,0));cam=bpy.context.object;cam.rotation_euler=(Vector((0,0,0))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.type='ORTHO';cam.data.ortho_scale=max(size.x,size.z)*1.18;scene.camera=cam
    for loc,power,scale in [((2,-3,4),350,4),((-3,-2,1),220,3)]:
        bpy.ops.object.light_add(type='AREA',location=loc);light=bpy.context.object;light.data.energy=power;light.data.shape='DISK';light.data.size=scale;light.rotation_euler=(-light.location).to_track_quat('-Z','Y').to_euler()
    scene.render.film_transparent=False;scene.render.filepath=str(PREVIEW/(id+'.png'));bpy.ops.render.render(write_still=True)
    report.append({'id':id,'source':str(src),'original_faces':original,'game_faces':sum(len(o.data.polygons) for o in objs),'blender_size':list(size),'maps':maps})
    (OUT/'manifest.json').write_text(json.dumps(report,indent=2))
    print('PREPARED',id,flush=True)
