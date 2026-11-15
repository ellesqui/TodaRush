import bpy, math, os, json, csv, random
from mathutils import Vector
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
random.seed(22)
bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)
for c in list(bpy.data.collections):
 if c.name!='Collection': bpy.data.collections.remove(c)
bpy.context.scene.unit_settings.system='METRIC'; bpy.context.scene.unit_settings.scale_length=1
COLORS={'wood':(.40,.20,.075),'woodlight':(.66,.39,.16),'red':(.85,.075,.045),'blue':(.025,.32,.72),'green':(.12,.53,.20),'yellow':(1,.66,.055),'purple':(.51,.15,.67),'pink':(.96,.34,.55),'cream':(.94,.84,.59),'concrete':(.49,.52,.50),'metal':(.40,.47,.51),'chrome':(.72,.77,.78),'dark':(.045,.055,.06),'black':(.012,.017,.02),'white':(.95,.96,.90),'glass':(.12,.40,.50),'orange':(1,.30,.025),'leaf':(.09,.34,.075),'fish':(.36,.63,.70),'meat':(.67,.16,.20),'fat':(.95,.67,.55),'ice':(.67,.88,.91),'coal':(.09,.065,.05)}
M={}
for n,c in COLORS.items():
 m=bpy.data.materials.new('TR_MAT_'+n); m.diffuse_color=(*c,1); m.use_nodes=True
 bs=m.node_tree.nodes.get('Principled BSDF'); bs.inputs['Base Color'].default_value=(*c,1); bs.inputs['Roughness'].default_value=.78
 if n in ['metal','chrome']: bs.inputs['Metallic'].default_value=.65; bs.inputs['Roughness'].default_value=.35
 M[n]=m
objects=[]; rows=[]; current=None

def finish(o,n,mat):
 o.name=n; o.data.materials.append(M[mat]);
 for c in list(o.users_collection): c.objects.unlink(o)
 current.objects.link(o); objects.append(o); return o

def mesh_obj(n,verts,faces,p,mat,rot=None):
 me=bpy.data.meshes.new(n+'_Mesh'); me.from_pydata(verts,[],faces); me.update()
 o=bpy.data.objects.new(n,me); bpy.context.scene.collection.objects.link(o); o.location=p
 if rot: o.rotation_euler=rot
 return finish(o,n,mat)
def box(n,p,s,mat,rot=None):
 x,y,z=[v/2 for v in s]
 v=[(-x,-y,-z),(-x,-y,z),(-x,y,-z),(-x,y,z),(x,-y,-z),(x,-y,z),(x,y,-z),(x,y,z)]
 f=[(0,4,6,2),(1,3,7,5),(0,1,5,4),(2,6,7,3),(0,2,3,1),(4,5,7,6)]
 return mesh_obj(n,v,[tuple(reversed(face)) for face in f],p,mat,rot)
def cyl(n,p,r,d,mat,verts=12,rot=None,r2=None):
 r2=r if r2 is None else r2
 v=[(rr*math.cos(i*math.tau/verts),rr*math.sin(i*math.tau/verts),z) for rr,z in [(r,-d/2),(r2,d/2)] for i in range(verts)]
 f=[tuple(reversed(range(verts))),tuple(range(verts,2*verts))]+[(i,(i+1)%verts,(i+1)%verts+verts,i+verts) for i in range(verts)]
 return mesh_obj(n,v,f,p,mat,rot)
import bmesh
_bm=bmesh.new(); bmesh.ops.create_icosphere(_bm,subdivisions=1,radius=1);_bm.verts.ensure_lookup_table();_bm.verts.index_update()
_IV=[tuple(v.co) for v in _bm.verts];_IF=[tuple(v.index for v in f.verts) for f in _bm.faces];_bm.free()
def ico(n,p,s,mat):
 return mesh_obj(n,[tuple(v[i]*s[i] for i in range(3)) for v in _IV],_IF,p,mat)
def beam(n,a,b,w,mat):
 a,b=Vector(a),Vector(b); o=box(n,(a+b)/2,(w,w,(b-a).length),mat); o.rotation_euler=(b-a).to_track_quat('Z','Y').to_euler(); return o

def text(label,p,size=.18,mat='white'):
 bpy.ops.object.text_add(location=p,rotation=(math.pi/2,0,0)); o=bpy.context.object; o.data.body=label; o.data.size=size; o.data.align_x='CENTER'; o.data.align_y='CENTER'; o.data.extrude=.0005; o.data.resolution_u=2
 bpy.ops.object.convert(target='MESH'); return finish(bpy.context.object,'Label_'+label.replace(' ','_'),mat)
def sign(label,p,w=2,h=.45,mat='green',size=.19):
 box('SignPanel',p,(w,.07,h),mat); text(label,(p[0],p[1]-.041,p[2]),size,'dark' if mat in ['cream','yellow'] else 'white')
def roof(w,d,z,color='red'):
 rise=.5; sl=math.sqrt((d/2)**2+rise**2); ang=math.atan2(rise,d/2)
 for side in [-1,1]:
  box('RoofSlope',(0,side*d/4,z+rise/2),(w,sl,.065),color,(side*-ang,0,0))
  for i in range(int(w/.3)+1):
   x=-w/2+i*.3
   beam('RoofRib',(x,0,z+rise+.04),(x,side*d/2,z+.04),.025,'metal')

def table(w=1.8,d=.8,z=.85,mat='woodlight'):
 box('TableTop',(0,0,z),(w,d,.09),mat)
 for x in [-w/2+.09,w/2-.09]:
  for y in [-d/2+.09,d/2-.09]: box('TableLeg',(x,y,z/2),(.09,.09,z),'wood')
 box('Shelf',(0,0,.24),(w-.1,d-.1,.05),'wood')
def crate(w=.65,d=.46,h=.4):
 box('CrateBase',(0,0,.025),(w,d,.05),'woodlight')
 for x in [-w/2+.025,w/2-.025]:
  for y in [-d/2+.025,d/2-.025]: box('CratePost',(x,y,h/2),(.055,.055,h),'wood')
 for z in [.12,.27,h-.035]:
  for y in [-d/2,d/2]: box('Slat',(0,y,z),(w,.035,.065),'woodlight')
  for x in [-w/2,w/2]: box('Slat',(x,0,z),(.035,d,.065),'woodlight')
def tray(w=1,d=.6,z=.05,mat='blue'):
 box('TrayBase',(0,0,z),(w,d,.055),mat)
 for x in [-w/2,w/2]: box('TrayRim',(x,0,z+.065),(.045,d,.13),mat)
 for y in [-d/2,d/2]: box('TrayRim',(0,y,z+.065),(w,.045,.13),mat)

def produce(kind,p=(0,0,0),scale=1):
 x,y,z=p
 if kind in ['Fish','Tilapia']:
  ico('FishBody',(x,y,z+.055*scale),(.20*scale,.065*scale,.06*scale),'fish')
  ico('FishTail',(x-.22*scale,y,z+.055*scale),(.08*scale,.10*scale,.016*scale),'blue')
  ico('FishEye',(x+.13*scale,y-.052*scale,z+.073*scale),(.012*scale,)*3,'black')
  ico('DorsalFin',(x,y,z+.12*scale),(.10*scale,.015*scale,.05*scale),'metal')
 elif kind in ['Meat','Pork']:
  ico('PorkCut',(x,y,z+.05*scale),(.16*scale,.11*scale,.06*scale),'meat'); ico('FatCap',(x,y,z+.098*scale),(.10*scale,.07*scale,.018*scale),'fat')
 elif kind=='Banana':
  for j in range(4):
   beam('Banana',(x+j*.026*scale,y,z+.06*scale),(x+j*.026*scale+.04*scale,y+.16*scale,z+.095*scale),.035*scale,'yellow')
 elif kind=='Carrot':
  cyl('Carrot',(x,y,z+.12*scale),.045*scale,.23*scale,'orange',6,r2=.008*scale)
  for dx in [-.025,.025]: beam('CarrotLeaf',(x,y,z+.24*scale),(x+dx*scale,y,z+.33*scale),.02*scale,'leaf')
 elif kind=='Eggplant':
  ico('Eggplant',(x,y,z+.08*scale),(.055*scale,.14*scale,.06*scale),'purple'); cyl('Stem',(x,y+.10*scale,z+.11*scale),.025*scale,.055*scale,'leaf',6)
 elif kind=='Mango': ico('Mango',(x,y,z+.07*scale),(.075*scale,.12*scale,.065*scale),'yellow')
 elif kind=='Watermelon':
  ico('Melon',(x,y,z+.14*scale),(.18*scale,.14*scale,.14*scale),'green')
  for dx in [-.065,.065]: beam('Stripe',(x+dx*scale,y-.12*scale,z+.10*scale),(x+dx*scale,y+.12*scale,z+.10*scale),.014*scale,'leaf')
 else:
  mat={'Tomato':'red','Cabbage':'green','Orange':'orange','Coconut':'wood'}.get(kind,'red'); s=.10 if kind!='Cabbage' else .15
  ico(kind,(x,y,z+s*scale),(s*scale,)*3,mat); cyl('Stem',(x,y,z+2*s*scale),.018*scale,.04*scale,'leaf',6)

def display(kind,z=0):
 tray(z=z+.04,mat='metal' if kind in ['Fish','Meat'] else 'woodlight')
 for i in range(4):
  for j in range(3): produce((['Cabbage','Tomato','Eggplant','Carrot'][i] if kind=='Vegetable' else ['Mango','Orange','Banana','Coconut'][i] if kind=='Fruit' else kind),(-.36+i*.24,-.19+j*.19,z+.08),.75)

def stall(kind,color):
 table(2,1)
 for x in [-1,1]:
  for y in [-.55,.55]: box('CanopyPost',(x,y,1.38),(.065,.065,2.76),'metal')
 roof(2.4,1.6,2.55,color); sign(kind.upper()+' / PALENGKE',(0,-.8,2.42),2.35,.33,color,.125)
 box('FrontApron',(0,-.48,.55),(1.95,.055,.52),color)
 display(kind,.9)
 sign({'Vegetable':'GULAY','Fruit':'PRUTAS','Fish':'ISDA','Meat':'KARNE'}[kind],(0,-.525,.60),1.3,.3,'cream',.18)

def umbrella(color):
 cyl('Pole',(0,0,1.25),.035,2.5,'metal')
 cyl('WeightedBase',(0,0,.07),.27,.14,'concrete')
 # closed canopy with alternating solid triangular segments
 verts=[(0,0,2.65)]+[(1.3*math.cos(i*math.pi/4),1.3*math.sin(i*math.pi/4),2.22) for i in range(8)]
 for i in range(8):
  mesh=bpy.data.meshes.new('CanopySegment'); mesh.from_pydata([verts[0],verts[i+1],verts[(i+1)%8+1]],[],[(0,1,2)]); mesh.update(); o=bpy.data.objects.new('CanopySegment',mesh); current.objects.link(o); objects.append(o); o.data.materials.append(M[color if i%2 else 'cream'])
  mod=o.modifiers.new('CanopyThickness','SOLIDIFY'); mod.thickness=.018
  bpy.context.view_layer.objects.active=o; o.select_set(True); bpy.ops.object.modifier_apply(modifier=mod.name); o.select_set(False)
  beam('CanopyRib',(0,0,2.61),verts[i+1],.018,'metal')

def kiosk(kind,color):
 box('KioskBody',(0,0,.55),(2,1.25,1.1),color); box('ServiceCounter',(0,-.12,1.13),(2.2,1.55,.10),'chrome')
 for x in [-.95,.95]:
  for y in [-.55,.55]: box('Post',(x,y,1.85),(.07,.07,1.5),'metal')
 roof(2.5,1.8,2.55,color); sign(kind.upper(),(0,-.91,2.42),2.3,.34,color,.19)
 if kind=='IHAW':
  box('Grill',(0,0,1.28),(1.2,.65,.22),'dark')
  for i in range(12): box('GrillBar',(-.52+i*.095,0,1.41),(.018,.63,.02),'chrome')
  for j in range(5):
   y=-.24+j*.12; beam('Skewer',(-.48,y,1.46),(.48,y,1.46),.014,'woodlight')
   for x in [-.25,0,.25]: ico('BBQ',(x,y,1.48),(.085,.045,.045),'wood')
 elif kind=='HALO-HALO':
  box('IceChest',(-.48,0,1.35),(.72,.7,.32),'ice')
  for x in [.12,.40,.68]:
   cyl('DessertCup',(x,-.15,1.35),.10,.31,'cream',10,r2=.13); ico('UbeScoop',(x,-.15,1.56),(.13,.13,.10),'purple')
   beam('Spoon',(x+.06,-.15,1.48),(x+.12,-.15,1.79),.015,'pink')
 elif kind=='INUMIN':
  for x,ma in [(-.6,'orange'),(0,'green'),(.6,'purple')]:
   cyl('JuiceDispenser',(x,0,1.49),.22,.58,ma); cyl('Lid',(x,0,1.80),.24,.06,'white'); box('Tap',(x,-.24,1.31),(.06,.12,.07),'chrome')
 else:
  tray(1.4,.7,1.22,'metal')
  for x in [-.5,-.15,.2,.5]: ico('Bread',(x,0,1.34),(.13,.23,.09),'cream')

def wheel(x,y,z,r=.35):
 cyl('Tire',(x,y,z),r,.18,'black',12,(math.pi/2,0,0)); cyl('WheelHub',(x,y-.10 if y<0 else y+.10,z),r*.56,.025,'chrome',12,(math.pi/2,0,0))
def vehicle(kind,color):
 # Front is +X in source; exported glTF +X also. Wheels are independent named mesh parts.
 if kind=='Jeepney':
  box('Chassis',(0,0,.55),(4.9,1.85,.22),'dark'); box('PassengerBody',(-.45,0,1.03),(3.55,1.85,.78),color)
  box('Floor',(-.45,0,.81),(3.65,1.65,.12),'chrome')
  for side in [-1,1]:
   box('Bench',(-.6,side*.64,1.1),(2.5,.32,.22),'dark')
   for x in [-2.15,-1.3,-.45,.4,1.1]: box('WindowPillar',(x,side*.85,1.76),(.065,.065,.80),'chrome')
   beam('SideRail',(-2.15,side*.94,1.38),(1.1,side*.94,1.38),.07,'chrome')
   for x in [-1.6,1.45]: wheel(x,side*.92,.40,.39)
  box('Roof',(-.5,0,2.22),(3.8,2.05,.14),'chrome'); box('CabWindshield',(1.12,0,1.81),(.065,1.55,.65),'glass')
  box('Hood',(1.85,0,1.20),(1.30,1.65,.40),color); box('Grille',(2.53,0,1.03),(.07,1.33,.47),'chrome')
  for y in [-.50,.50]: cyl('Headlamp',(2.58,y,1.25),.13,.04,'cream',12,(0,math.pi/2,0))
  for y in [-.45,-.22,0,.22,.45]: box('GrilleSlot',(2.57,y,1.04),(.025,.08,.26),'dark')
  box('FrontBumper',(2.66,0,.74),(.16,2,.14),'chrome'); box('RearStep',(-2.52,0,.59),(.38,1.0,.13),'chrome')
  sign('PALENGKE - BAYAN',(-.5,-1.04,2.23),2.8,.25,'yellow',.15)
  for y in [-1.1,1.1]: beam('MirrorArm',(1.1,y*.8,1.85),(1.33,y,1.85),.04,'chrome'); box('Mirror',(1.33,y,1.85),(.08,.16,.23),'metal')
 else:
  # motorcycle to left of enclosed sidecar
  for x in [-.72,.77]: wheel(x,-.48,.30,.29)
  wheel(-.45,.80,.30,.29)
  beam('BikeFrame',(-.72,-.48,.4),(.55,-.48,.65),.09,'metal'); beam('FrontFork',(.77,-.48,.3),(.60,-.48,1.05),.07,'chrome')
  box('Seat',(-.25,-.48,.88),(.65,.34,.13),'black'); ico('FuelTank',(.15,-.48,.83),(.28,.18,.18),color)
  box('Engine',(0,-.48,.5),(.36,.28,.32),'metal'); box('SidecarFloor',(-.20,.40,.44),(1.45,.95,.15),'metal')
  box('SidecarFront',(.48,.40,.92),(.13,.95,.85),color); box('SidecarBack',(-.91,.4,.94),(.12,.95,.88),color)
  box('OuterPanel',(-.25,.89,.79),(1.4,.10,.53),color); box('PassengerSeat',(-.55,.42,.70),(.42,.74,.17),'black')
  for x in [-.91,.5]:
   for y in [-.05,.85]: box('RoofPost',(x,y,1.33),(.045,.045,1.0),'chrome')
  box('SidecarRoof',(-.2,.4,1.86),(1.65,1.2,.12),color); box('Windshield',(.54,.40,1.43),(.045,.83,.40),'glass')
  beam('Handlebar',(.60,-.77,1.05),(.60,-.20,1.05),.045,'chrome'); cyl('Headlight',(.79,-.48,.94),.115,.09,'cream',12,(0,math.pi/2,0))
  sign('TODA',(-.20,-.22,1.85),.85,.23,color,.15)

def asset(cat,name,fn):
 global current,objects
 current=bpy.data.collections.new(name); bpy.context.scene.collection.children.link(current); objects=[]; fn()
 # materialize all transforms and triangulate; root origin at ground center, preserve part names
 bpy.ops.object.select_all(action='DESELECT')
 for o in objects: o.select_set(True)
 bpy.context.view_layer.objects.active=objects[0]; bpy.ops.object.transform_apply(location=False,rotation=True,scale=True)
 for i,o in enumerate(objects):
  o.name=name+'__'+o.name+'_%03d'%i
  mod=o.modifiers.new('TR_Triangulate','TRIANGULATE'); bpy.context.view_layer.objects.active=o; bpy.ops.object.modifier_apply(modifier=mod.name)
 corners=[o.matrix_world@Vector(c) for o in objects for c in o.bound_box]; lo=Vector([min(p[i] for p in corners) for i in range(3)]); hi=Vector([max(p[i] for p in corners) for i in range(3)])
 # Ensure ground anchor; center XY bounding box
 shift=Vector((-(lo.x+hi.x)/2,-(lo.y+hi.y)/2,-lo.z))
 for o in objects: o.location+=shift
 dims=hi-lo
 path=ROOT/'GLB'/cat/(name+'.glb'); path.parent.mkdir(parents=True,exist_ok=True)
 # Combine export geometry by material to reduce draw calls; keep editable components in blend.
 bpy.ops.object.duplicate(linked=False)
 export_parts=list(bpy.context.selected_objects)
 bpy.context.view_layer.objects.active=export_parts[0]; bpy.ops.object.join()
 exported=bpy.context.object; exported.name=name
 bpy.ops.export_scene.gltf(filepath=str(path),export_format='GLB',use_selection=True,export_yup=True,export_materials='EXPORT',export_extras=True)
 bpy.ops.object.delete(use_global=False)
 tris=sum(len(o.data.polygons) for o in objects)
 rows.append(dict(name=name,category=cat,file=str(path.relative_to(ROOT)),width_m=round(dims.x,4),depth_m=round(dims.y,4),height_m=round(dims.z,4),triangles=tris,mesh_parts=len(objects)))
 # editable source catalog, separate asset roots on inspection grid, never a game map
 root=bpy.data.objects.new(name+'_ROOT',None); current.objects.link(root); root['asset_id']=name; root['glb_path']=str(path.relative_to(ROOT)); root['units']='meters'; root['forward']='+X (vehicles); -Y (sign fronts in Blender)'
 for o in objects: o.parent=root
 i=len(rows)-1; root.location=((i%10)*16,(i//10)*13,0)
 current.use_fake_user=True
 bpy.context.scene.collection.children.unlink(current)
 print('ASSET_DONE',name,tris,flush=True)

asset('01_Public_Market','TR_Market_Main',lambda:market()) if False else None

def market():
 box('Foundation',(0,0,.15),(12,8,.3),'concrete')
 for x in [-5.65,-2,2,5.65]:
  for y in [-3.65,3.65]: box('Column',(x,y,2.0),(.32,.32,3.7),'cream')
 box('RearWall',(0,3.7,1.9),(11.6,.18,3.2),'cream')
 for x in [-5.7,5.7]: box('SideWall',(x,0,1.1),(.2,7.4,1.6),'cream')
 box('FrontLintel',(0,-3.65,3.3),(11.5,.26,.65),'yellow'); roof(12.5,8.5,3.7,'red')
 sign('PAMILIHANG BAYAN',(0,-3.82,3.32),7.5,.6,'green',.48)
 for x in [-4,4]:
  box('RollUpDoor',(x,3.56,1.5),(2.25,.055,2.4),'blue')
  for z in [.5,.8,1.1,1.4,1.7,2,2.3,2.6]: box('DoorRib',(x,3.52,z),(2.2,.025,.024),'metal')
asset('01_Public_Market','TR_Market_Main',market)
for k,c in [('Vegetable','green'),('Fruit','orange'),('Fish','blue'),('Meat','red')]: asset('02_Market_Stalls','TR_Stall_'+k,lambda k=k,c=c:stall(k,c))
for k,c in [('IHAW','red'),('MERYENDA','yellow'),('INUMIN','green'),('HALO-HALO','purple')]: asset('03_Food_Kiosks','TR_Kiosk_'+k.replace('-','_'),lambda k=k,c=c:kiosk(k,c))
for c in ['red','blue','green','yellow','purple']:
 asset('04_Canopies','TR_Umbrella_'+c,lambda c=c:umbrella(c))
 def tarp(c=c):
  box('Tarp',(0,0,2.4),(3,2,.035),c,(.10,0,0))
  for x in [-1.5,1.5]:
   for y in [-1,1]: beam('TieRope',(x,y,2.4+y*.1),(x*1.1,y*1.1,0),.015,'cream')
 asset('04_Canopies','TR_Tarpaulin_Tied_'+c,tarp)
for n,w,d in [('Small',1.2,.65),('Wide',2.4,.9)]: asset('05_Tables_Counters','TR_Table_'+n,lambda w=w,d=d:table(w,d))
def counter():
 box('CounterBody',(0,0,.5),(1.8,.75,1),'woodlight'); box('SteelTop',(0,0,1.05),(1.95,.85,.10),'chrome')
 for x in [-.45,.45]: box('FrontPanel',(x,-.385,.53),(.78,.035,.77),'wood'); cyl('Handle',(x,-.42,.72),.025,.18,'chrome',8,(0,math.pi/2,0))
asset('05_Tables_Counters','TR_Counter_Service',counter)
for k in ['Tomato','Cabbage','Carrot','Eggplant','Mango','Banana','Orange','Coconut','Watermelon','Fish','Meat']:
 asset('06_Produce_Displays','TR_Produce_'+k,lambda k=k:produce(k))
 asset('06_Produce_Displays','TR_Display_'+k,lambda k=k:display(k))
asset('07_Supplies','TR_Crate_Empty',crate)
for k in ['Tomato','Mango','Cabbage']:
 def filled(k=k):
  crate()
  for x in [-.2,0,.2]:
   for y in [-.12,.12]: produce(k,(x,y,.32),.65)
 asset('07_Supplies','TR_Crate_'+k,filled)
def basket():
 # open basket with separate base + tapered slat lattice
 cyl('BasketBase',(0,0,.025),.25,.05,'woodlight',12)
 for i in range(12):
  a=i*math.tau/12; beam('WovenUpright',(.24*math.cos(a),.24*math.sin(a),.03),(.34*math.cos(a),.34*math.sin(a),.43),.035,'woodlight')
 for z in [.10,.22,.34,.43]:
  r=.24+.10*z/.43
  for i in range(12):
   a=i*math.tau/12;b=(i+1)*math.tau/12; beam('WovenBand',(r*math.cos(a),r*math.sin(a),z),(r*math.cos(b),r*math.sin(b),z),.028,'wood')
asset('07_Supplies','TR_Basket_Woven',basket)
asset('07_Supplies','TR_Tray_Plastic',lambda:tray())
def sack():
 ico('RiceSack',(0,0,.35),(.30,.23,.40),'cream'); cyl('TiedNeck',(0,0,.72),.07,.12,'woodlight',8); sign('BIGAS',(0,-.22,.4),.35,.18,'green',.085)
asset('07_Supplies','TR_Sack_Rice',sack)
def cooler():
 box('CoolerBody',(0,0,.25),(.8,.48,.50),'blue'); box('CoolerLid',(0,0,.53),(.84,.52,.07),'white')
 for x in [-.43,.43]: box('Handle',(x,0,.33),(.06,.24,.08),'white')
asset('07_Supplies','TR_Cooler_IceBox',cooler)
def scale():
 box('ScaleBase',(0,0,.07),(.32,.32,.14),'green'); cyl('Dial',(0,-.12,.29),.14,.07,'cream',16,(math.pi/2,0,0)); beam('Needle',(0,-.165,.29),(.065,-.165,.36),.012,'red'); cyl('Tray',(0,0,.49),.25,.04,'chrome')
asset('07_Supplies','TR_Weighing_Scale',scale)
def loading():
 box('Platform',(0,0,.1),(7,2.5,.2),'concrete')
 for x in [-3.1,0,3.1]:
  for y in [-1.05,1.05]: box('ShelterPost',(x,y,1.5),(.10,.1,3),'metal')
 roof(7.5,3,3,'blue'); sign('SAKAYAN NG JEEP',(0,-1.51,2.85),5,.45,'yellow',.32)
 box('BenchSeat',(0,.75,.62),(5,.45,.12),'woodlight')
 for x in [-2,0,2]: box('BenchLeg',(x,.75,.36),(.13,.3,.50),'metal')
asset('08_Jeepney_Loading','TR_Jeepney_Shelter',loading)
def post_sign(label,color='blue'):
 cyl('SignPost',(0,0,1.15),.045,2.3,'metal'); box('Base',(0,0,.06),(.38,.38,.12),'concrete'); sign(label,(0,-.055,2.12),1.15,.65,color,.16)
for n,label,c in [('Parking','PARKING','blue'),('NoParking','BAWAL\nPUMARADA','red'),('Loading','LOADING\nZONE','green'),('Speed20','20 KPH','yellow'),('OneWay','ONE WAY >','blue')]: asset('09_Parking_Signs','TR_Sign_'+n,lambda label=label,c=c:post_sign(label,c))
def bay():
 for x in [-1.25,1.25]: box('PaintStripe',(x,0,.004),(.09,5,.008),'white')
 box('EndStripe',(0,2.45,.004),(2.5,.10,.008),'white')
asset('09_Parking_Signs','TR_Marking_ParkingBay',bay)
def arrow():
 box('ArrowStem',(0,-.5,.005),(.2,1.6,.01),'white')
 me=bpy.data.meshes.new('ArrowHead'); me.from_pydata([(-.6,.2,0),(.6,.2,0),(0,1.1,0),(-.6,.2,.01),(.6,.2,.01),(0,1.1,.01)],[],[(0,2,1),(3,4,5),(0,1,4,3),(1,2,5,4),(2,0,3,5)]);me.update();o=bpy.data.objects.new('ArrowHead',me); current.objects.link(o);objects.append(o);o.data.materials.append(M['white'])
asset('09_Parking_Signs','TR_Marking_Arrow',arrow)
def crossing():
 for i in range(6): box('CrosswalkStripe',(-1.5+i*.6,0,.005),(.35,2.5,.01),'white')
asset('09_Parking_Signs','TR_Marking_Crosswalk',crossing)
asset('09_Parking_Signs','TR_Wheel_Stop',lambda:box('WheelStop',(0,0,.075),(1.7,.19,.15),'yellow'))
for kind in ['Tricycle','Jeepney']:
 for c in ['red','blue','yellow']: asset('10_Vehicles','TR_'+kind+'_'+c,lambda kind=kind,c=c:vehicle(kind,c))
for name,label,c in [('Palengke','PALENGKE','green'),('Gulay','SARIWANG GULAY','green'),('Isda','SARIWANG ISDA','blue'),('Karne','KARNEHAN','red'),('Prutas','PRUTAS','orange')]: asset('11_Market_Boards','TR_Board_'+name,lambda label=label,c=c:sign(label,(0,0,.25),2,.5,c,.19))
def price():
 for y in [-.20,.20]:
  for x in [-.34,.34]: beam('AFrameLeg',(x,y,0),(x,0,1.05),.045,'woodlight')
 box('Chalkboard',(0,-.12,.64),(.78,.055,.75),'dark'); sign('PRESYO\nGULAY  P60/KG\nISDA  P160/KG',(0,-.156,.64),.73,.68,'dark',.085)
asset('11_Market_Boards','TR_PriceBoard_AFrame',price)
def chair(color='red'):
 box('Seat',(0,0,.45),(.45,.45,.065),color)
 for x in [-.17,.17]:
  for y in [-.17,.17]: box('Leg',(x,y,.23),(.045,.045,.46),color)
 for x in [-.18,.18]: box('BackPost',(x,.18,.66),(.045,.045,.5),color)
 box('Backrest',(0,.18,.78),(.4,.055,.22),color)
for c in ['red','blue','green']: asset('12_Furniture','TR_Chair_'+c,lambda c=c:chair(c))
def bench():
 for y in [-.15,0,.15]: box('SeatSlat',(0,y,.48),(1.8,.12,.07),'woodlight')
 for x in [-.65,.65]:
  for y in [-.15,.15]: box('Leg',(x,y,.24),(.10,.10,.48),'metal')
  beam('BackSupport',(x,.2,.2),(x,.3,.98),.07,'metal')
 for z in [.72,.90]: box('BackSlat',(0,.29,z),(1.8,.08,.13),'woodlight')
asset('12_Furniture','TR_Bench_Wood',bench)
for c,label in [('green','NABUBULOK'),('blue','RECYCLABLE'),('red','RESIDUAL')]:
 def bin(c=c,label=label):
  box('BinBody',(0,0,.42),(.55,.55,.84),c); box('BinLid',(0,0,.88),(.61,.61,.09),'dark'); box('Opening',(0,-.309,.79),(.31,.018,.13),'black'); sign(label,(0,-.284,.46),.48,.20,c,.062)
 asset('12_Furniture','TR_Bin_'+c,bin)

for col in bpy.data.collections:
 if col.name.startswith('TR_') and col.name not in bpy.context.scene.collection.children: bpy.context.scene.collection.children.link(col)
# Save editable model library with explicit catalog metadata.
bpy.ops.object.select_all(action='DESELECT')
scene=bpy.context.scene
scene['description']='TODA RUSH reusable asset catalog; spacing is for inspection only, NOT a level map.'
scene['asset_count']=len(rows)
with open(ROOT/'asset_manifest.json','w') as f: json.dump({'units':'meters','dimensions_order':'Blender X width, Y depth, Z height; glTF exports Y-up','pivot':'ground center of bounds','assets':rows},f,indent=2)
with open(ROOT/'asset_manifest.csv','w',newline='') as f:
 writer=csv.DictWriter(f,fieldnames=list(rows[0]));writer.writeheader();writer.writerows(rows)
bpy.ops.outliner.orphans_purge(do_recursive=True)
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'TODA_RUSH_Palengke_Assets.blend'))
print('BUILD_COMPLETE',len(rows),sum(r['triangles'] for r in rows),flush=True)
