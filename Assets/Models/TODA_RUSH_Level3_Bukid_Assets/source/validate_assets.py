import bpy,bmesh,json,struct,math,hashlib
from pathlib import Path
from mathutils import Vector
R=Path(__file__).resolve().parents[1];assets=json.loads((R/'asset_manifest.json').read_text())['assets'];results=[]
for a in assets:
 path=R/a['file'];raw=path.read_bytes();magic,ver,total=struct.unpack_from('<4sII',raw);assert magic==b'glTF' and ver==2 and total==len(raw)
 l,t=struct.unpack_from('<II',raw,12);assert t==0x4e4f534a;g=json.loads(raw[20:20+l]);assert g['asset']['version']=='2.0'
 bl,bt=struct.unpack_from('<II',raw,20+l);assert bt==0x004e4942;buf=raw[28+l:28+l+bl];assert all('uri' not in b for b in g['buffers']);assert all(v.get('byteOffset',0)+v['byteLength']<=len(buf) for v in g['bufferViews'])
 fmt={5121:('B',1),5123:('H',2),5125:('I',4),5126:('f',4)};size={'SCALAR':1,'VEC2':2,'VEC3':3,'VEC4':4}
 def vals(i):
  ac=g['accessors'][i];v=g['bufferViews'][ac['bufferView']];f,s=fmt[ac['componentType']];n=size[ac['type']];step=v.get('byteStride',s*n);offset=v.get('byteOffset',0)+ac.get('byteOffset',0)
  assert ac.get('byteOffset',0)+(ac['count']-1)*step+s*n<=v['byteLength']
  return [struct.unpack_from('<'+f*n,buf,offset+j*step) for j in range(ac['count'])]
 tris=0
 for m in g['meshes']:
  for pr in m['primitives']:
   assert pr.get('mode',4)==4 and 'material' in pr
   v=vals(pr['attributes']['POSITION']);idx=[p[0] for p in vals(pr['indices'])];assert len(idx)%3==0 and min(idx)>=0 and max(idx)<len(v);assert all(math.isfinite(x) for p in v for x in p);tris+=len(idx)//3
 assert tris==a['triangles']
 bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False);bpy.ops.import_scene.gltf(filepath=str(path));ms=[o for o in bpy.context.scene.objects if o.type=='MESH'];assert ms and all(o.data.polygons and o.data.materials for o in ms)
 pts=[o.matrix_world@Vector(c) for o in ms for c in o.bound_box];lo=[min(v[i] for v in pts) for i in range(3)];hi=[max(v[i] for v in pts) for i in range(3)];dims=[hi[i]-lo[i] for i in range(3)]
 assert all(abs(x-y)<.002 for x,y in zip(dims,[a['width_m'],a['depth_m'],a['height_m']]))
 assert abs(lo[2])<.002 and abs(lo[0]+hi[0])<.002 and abs(lo[1]+hi[1])<.002
 for o in ms:
  bm=bmesh.new();bm.from_mesh(o.data);volume=bm.calc_volume(signed=True);bm.free();assert volume>0,(a['name'],volume)
 results.append({'name':a['name'],'status':'PASS','triangles':tris,'material_primitives':sum(len(m['primitives']) for m in g['meshes']),'size_bytes':len(raw),'sha256':hashlib.sha256(raw).hexdigest()})
 print('PASS',a['name'],flush=True)
report={'status':'PASS','asset_count':len(results),'total_triangles':sum(r['triangles'] for r in results),'checks':['GLB 2 header and length','embedded buffer bounds','triangle indices and counts','finite vertex positions','materials assigned','full Blender reimport','dimensions match manifest within 2 mm','ground centered origin','positive signed mesh volume'],'limitations':'Not tested in Unity Editor; not a Khronos validator certification. Visual inspection performed separately on representative models.','assets':results}
(R/'docs/validation_report.json').write_text(json.dumps(report,indent=2));print('ALL_VALID',len(results),flush=True)
