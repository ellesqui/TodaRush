import bpy,json,struct,math,hashlib,bmesh
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1]
man=json.loads((ROOT/'asset_manifest.json').read_text()); results=[]
for row in man['assets']:
 p=ROOT/row['file']; raw=p.read_bytes(); magic,version,length=struct.unpack_from('<4sII',raw)
 assert magic==b'glTF' and version==2 and length==len(raw),(p,'header')
 n,t=struct.unpack_from('<II',raw,12); assert t==0x4e4f534a
 doc=json.loads(raw[20:20+n]); assert doc['asset']['version']=='2.0'
 assert all('uri' not in b for b in doc['buffers']), 'external buffer'
 nbin,tbin=struct.unpack_from('<II',raw,20+n); assert tbin==0x004e4942
 binary=raw[28+n:28+n+nbin]
 # Check every accessor range, finite positions, and triangle index bounds.
 for view in doc['bufferViews']: assert view.get('byteOffset',0)+view['byteLength']<=len(binary)
 components={5120:('b',1),5121:('B',1),5122:('h',2),5123:('H',2),5125:('I',4),5126:('f',4)}
 sizes={'SCALAR':1,'VEC2':2,'VEC3':3,'VEC4':4,'MAT4':16}
 def values(i):
  a=doc['accessors'][i]; v=doc['bufferViews'][a['bufferView']]; fmt,sz=components[a['componentType']]; cnt=sizes[a['type']]; stride=v.get('byteStride',sz*cnt); offset=v.get('byteOffset',0)+a.get('byteOffset',0)
  assert a.get('byteOffset',0)+(a['count']-1)*stride+sz*cnt<=v['byteLength']
  return [struct.unpack_from('<'+fmt*cnt,binary,offset+j*stride) for j in range(a['count'])]
 count=0
 for mesh in doc['meshes']:
  for pr in mesh['primitives']:
   assert pr.get('mode',4)==4
   pos=values(pr['attributes']['POSITION']); assert all(math.isfinite(c) for v in pos for c in v)
   inds=[v[0] for v in values(pr['indices'])]; assert len(inds)%3==0 and max(inds)<len(pos); count+=len(inds)//3
 assert count==row['triangles'],(p,count,row['triangles'])
 bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)
 bpy.ops.import_scene.gltf(filepath=str(p))
 meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']; assert meshes and all(len(o.data.polygons)>0 for o in meshes)
 pts=[o.matrix_world@Vector(c) for o in meshes for c in o.bound_box]
 lo=[min(v[i] for v in pts) for i in range(3)]; hi=[max(v[i] for v in pts) for i in range(3)]; dims=[hi[i]-lo[i] for i in range(3)]
 expected=[row['width_m'],row['depth_m'],row['height_m']]
 assert max(abs(a-b) for a,b in zip(dims,expected))<.002,(p,dims,expected)
 assert abs(lo[2])<.002 and abs(lo[0]+hi[0])<.002 and abs(lo[1]+hi[1])<.002
 assert all(o.data.materials for o in meshes)
 for o in meshes:
  bm=bmesh.new();bm.from_mesh(o.data);volume=bm.calc_volume(signed=True);bm.free();assert volume>0,(p,'nonpositive volume',volume)
 results.append({'asset':row['name'],'status':'PASS','triangles':count,'gltf_meshes':len(doc['meshes']),'material_primitives':sum(len(m['primitives']) for m in doc['meshes']),'sha256':hashlib.sha256(raw).hexdigest(),'size_bytes':len(raw),'checks':['GLB 2 header','embedded buffer ranges','finite mesh positions','triangle indices','triangle count','Blender reimport','meter dimensions','ground center pivot','assigned materials']})
 print('VALIDATED',row['name'],flush=True)
report={'status':'PASS','asset_count':len(results),'total_triangles':sum(r['triangles'] for r in results),'validation':'structural checks and complete Blender 4.3 reimport; not a Khronos validator certification or Unity playtest','assets':results}
(ROOT/'docs'/'validation_report.json').write_text(json.dumps(report,indent=2))
print('ALL_VALID',len(results),flush=True)
