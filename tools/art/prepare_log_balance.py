"""Run with Blender 4.x: blender -b --python tools/art/prepare_log_balance.py -- <repo>.
Derives FBX and original embedded textures from the supplied GLBs; never edits the sources.
Runtime pose/animation is authored in Unity over the existing Castaway rig.
"""
import bpy,json,struct,sys,hashlib,math
from pathlib import Path
from mathutils import Vector,Matrix
root=Path(sys.argv[sys.argv.index('--')+1]).resolve()
sources=root/'Unity/Assets/_Project/Art/LogBalance/Sources'
out=root/'Unity/Assets/_Project/Resources/LogBalance'
(out/'Models').mkdir(parents=True,exist_ok=True);(out/'Textures').mkdir(exist_ok=True)
selected=sys.argv[sys.argv.index('--asset')+1] if '--asset' in sys.argv else None
manifest_path=out/'asset-manifest.json'
manifest=json.loads(manifest_path.read_text()) if selected and manifest_path.exists() else {'source':'User-supplied GLB files; embedded Sketchfab attribution retained','assets':[]}
if selected:manifest['assets']=[a for a in manifest['assets'] if a['name']!=selected]
for name in ([selected] if selected else ['trunk','low_poly_trees','Vagabundo','low_poly_grass_pack']):
 path=sources/(name+'.glb');data=path.read_bytes();size,kind=struct.unpack_from('<II',data,12)
 gltf=json.loads(data[20:20+size]);offset=20+size;bin_size,bin_type=struct.unpack_from('<II',data,offset);binary=data[offset+8:offset+8+bin_size]
 images=[]
 for i,image in enumerate(gltf.get('images',[])):
  view=gltf['bufferViews'][image['bufferView']];extension='jpg' if image.get('mimeType')=='image/jpeg' else 'png'
  filename=f'{name}_{i}.{extension}';(out/'Textures'/filename).write_bytes(binary[view.get('byteOffset',0):view.get('byteOffset',0)+view['byteLength']]);images.append(filename)
 materials=[]
 for m in gltf.get('materials',[]):
  p=m.get('pbrMetallicRoughness',{});tex=p.get('baseColorTexture');idx=gltf['textures'][tex['index']]['source'] if tex else None
  materials.append({'name':m['name'],'color':p.get('baseColorFactor',[1,1,1,1]),'texture':images[idx] if idx is not None else ''})
 bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.gltf(filepath=str(path));bpy.context.scene.frame_set(0);bpy.context.view_layer.update()
 meshes=[o for o in bpy.context.scene.objects if o.type=='MESH' and any(m.type=='ARMATURE' for m in o.modifiers)] if name=='Vagabundo' else [o for o in bpy.context.scene.objects if o.type=='MESH']
 if name in {'low_poly_trees','low_poly_grass_pack'}:
  prefix='Grass' if name=='low_poly_grass_pack' else 'Tree'
  # The pack is laid out for inspection. Export each variety separately, grounded and centered.
  for index,mesh in enumerate(meshes):
   bpy.ops.object.select_all(action='DESELECT');mesh.select_set(True);bpy.context.view_layer.objects.active=mesh
   world=mesh.matrix_world.copy();mesh.parent=None;mesh.matrix_world=world
   bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
   pts=[mesh.matrix_world@Vector(v) for v in mesh.bound_box];lo=Vector([min(p[i] for p in pts) for i in range(3)]);hi=Vector([max(p[i] for p in pts) for i in range(3)])
   # Pack uses Blender Z-up after import; each model's pivot is now at its own trunk base.
   center=Vector(((lo.x+hi.x)/2,(lo.y+hi.y)/2,lo.z));height=max(.01,hi.z-lo.z)
   for v in mesh.data.vertices:v.co=(v.co-center)*(1/height)
   filename=f'{prefix}_{index:02d}.fbx'
   bpy.ops.export_scene.fbx(filepath=str(out/'Models'/filename),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',add_leaf_bones=False,bake_anim=False,use_mesh_modifiers=True)
  model_files=[f'{prefix}_{i:02d}.fbx' for i in range(len(meshes))]
 else:
  # Keep the source hierarchy, bone weights and authored action. A parent normalizes world units.
  pts=[o.matrix_world@Vector(v) for o in meshes for v in o.bound_box]
  lo=Vector([min(p[i] for p in pts) for i in range(3)]);hi=Vector([max(p[i] for p in pts) for i in range(3)])
  center=Vector(((lo.x+hi.x)/2,(lo.y+hi.y)/2,lo.z))
  normalizer=bpy.data.objects.new(name+'_Normalized',None);bpy.context.collection.objects.link(normalizer)
  top=[o for o in bpy.context.scene.objects if o.parent is None and o!=normalizer]
  for o in top:o.parent=normalizer
  if name=='Vagabundo':
   scale=2.35/(hi.z-lo.z);normalizer.scale=(scale,)*3;normalizer.location=-center*scale
  else:
   # Original trunk runs on Y. Export upright metadata so Unity can scale length on local Z.
   scale=18/(hi.y-lo.y);normalizer.scale=(scale,)*3;normalizer.location=-Vector(((lo.x+hi.x)/2,(lo.y+hi.y)/2,(lo.z+hi.z)/2))*scale
  bpy.ops.object.select_all(action='DESELECT')
  for o in bpy.context.scene.objects:
   if o.type in {'MESH','ARMATURE','EMPTY'} and o.name!='Icosphere':o.select_set(True)
  filename=name+'.fbx'
  bpy.ops.export_scene.fbx(filepath=str(out/'Models'/filename),use_selection=True,object_types={'MESH','ARMATURE','EMPTY'},axis_forward='-Z',axis_up='Y',add_leaf_bones=False,bake_anim=name=='Vagabundo',bake_anim_use_all_actions=False,bake_anim_use_nla_strips=False,bake_anim_force_startend_keying=True,bake_anim_step=1,path_mode='STRIP',use_mesh_modifiers=True)
  model_files=[filename]
 manifest['assets'].append({'name':name,'sha256':hashlib.sha256(data).hexdigest(),'credits':gltf['asset'].get('extras',{}),'materials':materials,'models':model_files,'skins':len(gltf.get('skins',[])),'animations':[a.get('name') for a in gltf.get('animations',[])]})
(out/'asset-manifest.json').write_text(json.dumps(manifest,indent=2,ensure_ascii=False)+'\n')
print('LOG_BALANCE_ASSETS_READY')
