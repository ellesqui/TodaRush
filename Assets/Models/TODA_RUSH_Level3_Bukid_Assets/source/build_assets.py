import bpy,bmesh,math,json,csv,random
from pathlib import Path
from mathutils import Vector
R=Path(__file__).resolve().parents[1];random.seed(310)
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
bpy.context.scene.unit_settings.system='METRIC';bpy.context.scene.unit_settings.scale_length=1
palette={'timber':(.36,.17,.065),'bamboo':(.67,.44,.19),'cream':(.91,.81,.56),'red':(.77,.10,.06),'blue':(.055,.36,.65),'green':(.15,.52,.13),'leaf':(.075,.32,.07),'rice':(.42,.67,.12),'grain':(.80,.63,.20),'yellow':(.96,.61,.025),'orange':(.98,.24,.025),'white':(.93,.93,.83),'dark':(.04,.05,.055),'black':(.015,.018,.021),'metal':(.36,.42,.44),'silver':(.65,.69,.68),'soil':(.32,.17,.065),'earth':(.47,.29,.12),'mud':(.25,.20,.10),'water':(.13,.47,.53),'glass':(.12,.43,.54),'stone':(.40,.42,.37),'hay':(.72,.53,.16),'brown':(.34,.14,.055),'pink':(.88,.43,.40),'amber':(1,.43,.025),'purple':(.38,.12,.48)}
MAT={}
for n,c in palette.items():
 m=bpy.data.materials.new('TR3_MAT_'+n);m.diffuse_color=(*c,1);m.use_nodes=True;p=m.node_tree.nodes.get('Principled BSDF');p.inputs['Base Color'].default_value=(*c,1);p.inputs['Roughness'].default_value=.82
 if n in ['silver','metal']:p.inputs['Metallic'].default_value=.55;p.inputs['Roughness'].default_value=.4
 if n=='water':p.inputs['Roughness'].default_value=.23
 if n=='amber':p.inputs['Emission Color'].default_value=(*c,1);p.inputs['Emission Strength'].default_value=.7
 MAT[n]=m
current=None;parts=[];records=[]
def obj(n,v,f,p,ma,rot=None):
 me=bpy.data.meshes.new(n+'_Mesh');me.from_pydata(v,[],f);me.update();o=bpy.data.objects.new(n,me);current.objects.link(o);o.location=p
 if rot:o.rotation_euler=rot
 o.data.materials.append(MAT[ma]);parts.append(o);return o
def box(n,p,s,ma,rot=None):
 x,y,z=[t/2 for t in s];v=[(-x,-y,-z),(-x,-y,z),(-x,y,-z),(-x,y,z),(x,-y,-z),(x,-y,z),(x,y,-z),(x,y,z)]
 f=[(2,6,4,0),(5,7,3,1),(4,5,1,0),(3,7,6,2),(1,3,2,0),(6,7,5,4)];return obj(n,v,f,p,ma,rot)
def cyl(n,p,r,h,ma,N=10,rot=None,top=None):
 top=r if top is None else top
 v=[(rr*math.cos(i*math.tau/N),rr*math.sin(i*math.tau/N),z) for rr,z in [(r,-h/2),(top,h/2)] for i in range(N)]
 f=[tuple(reversed(range(N))),tuple(range(N,2*N))]+[(i,(i+1)%N,(i+1)%N+N,i+N) for i in range(N)]
 return obj(n,v,f,p,ma,rot)
bm=bmesh.new();bmesh.ops.create_icosphere(bm,subdivisions=1,radius=1);bm.verts.ensure_lookup_table();bm.verts.index_update();IV=[tuple(v.co) for v in bm.verts];IF=[tuple(v.index for v in f.verts) for f in bm.faces];bm.free()
def ico(n,p,s,ma):return obj(n,[tuple(v[i]*s[i] for i in range(3)) for v in IV],IF,p,ma)
def beam(n,a,b,w,ma):
 a,b=Vector(a),Vector(b);o=box(n,(a+b)/2,(w,w,(b-a).length),ma);o.rotation_euler=(b-a).to_track_quat('Z','Y').to_euler();return o
def text(s,p,size=.2,ma='white'):
 bpy.ops.object.text_add(location=p,rotation=(math.pi/2,0,0));o=bpy.context.object;o.data.body=s;o.data.align_x='CENTER';o.data.align_y='CENTER';o.data.size=size;o.data.resolution_u=2;o.data.extrude=.0007;bpy.ops.object.convert(target='MESH');o=bpy.context.object
 for c in list(o.users_collection):c.objects.unlink(o)
 current.objects.link(o);o.name='Lettering';o.data.materials.append(MAT[ma]);parts.append(o)
def panel(s,p,w,h,ma='green',size=.18):
 box('SignPanel',p,(w,.065,h),ma);text(s,(p[0],p[1]-.038,p[2]),size,'dark' if ma in ['yellow','cream','white','orange'] else 'white')
def roof(w,d,z,ma='red',rise=.6):
 sl=math.hypot(d/2,rise);ang=math.atan2(rise,d/2)
 for side in [-1,1]:
  box('RoofPanel',(0,side*d/4,z+rise/2),(w,sl,.07),ma,(-side*ang,0,0))
  for i in range(int(w/.4)+1):beam('RoofRib',(-w/2+i*.4,0,z+rise+.04),(-w/2+i*.4,side*d/2,z+.04),.025,'silver')
def pole(x,y,h,ma='bamboo'):box('Post',(x,y,h/2),(.10,.10,h),ma)
def triangle_prism(n,vs,thick,ma):
 v=[(x,y-thick/2,z) for x,y,z in vs]+[(x,y+thick/2,z) for x,y,z in vs]
 f=[(0,1,2),(5,4,3),(0,3,4,1),(1,4,5,2),(2,5,3,0)]
 o=obj(n,v,f,(0,0,0),ma);bm=bmesh.new();bm.from_mesh(o.data);bmesh.ops.recalc_face_normals(bm,faces=bm.faces);bm.to_mesh(o.data);bm.free();return o

def asset(cat,name,fn,notes=''):
 global current,parts
 parts=[];current=bpy.data.collections.new(name);bpy.context.scene.collection.children.link(current);fn()
 bpy.ops.object.select_all(action='DESELECT')
 for o in parts:o.select_set(True)
 bpy.context.view_layer.objects.active=parts[0];bpy.ops.object.transform_apply(location=False,rotation=True,scale=True)
 for i,o in enumerate(parts):
  o.name=name+'__'+o.name+'_%03d'%i;mod=o.modifiers.new('Triangulate','TRIANGULATE');bpy.context.view_layer.objects.active=o;bpy.ops.object.modifier_apply(modifier=mod.name)
 pts=[o.matrix_world@Vector(c) for o in parts for c in o.bound_box];lo=Vector([min(v[i] for v in pts) for i in range(3)]);hi=Vector([max(v[i] for v in pts) for i in range(3)])
 shift=Vector((-(hi.x+lo.x)/2,-(hi.y+lo.y)/2,-lo.z))
 for o in parts:o.location+=shift
 path=R/'GLB'/cat/(name+'.glb');path.parent.mkdir(parents=True,exist_ok=True)
 bpy.ops.object.duplicate(linked=False);bpy.context.view_layer.objects.active=bpy.context.selected_objects[0];bpy.ops.object.join();bpy.context.object.name=name
 bpy.ops.export_scene.gltf(filepath=str(path),export_format='GLB',use_selection=True,export_yup=True,export_materials='EXPORT')
 bpy.ops.object.delete(use_global=False)
 d=hi-lo;records.append(dict(name=name,category=cat,file=str(path.relative_to(R)),width_m=round(d.x,4),depth_m=round(d.y,4),height_m=round(d.z,4),triangles=sum(len(o.data.polygons) for o in parts),source_mesh_parts=len(parts),notes=notes))
 root=bpy.data.objects.new(name+'_ROOT',None);current.objects.link(root);root['asset_id']=name;root['glb_path']=str(path.relative_to(R))
 for o in parts:o.parent=root
 i=len(records)-1;root.location=(i%9*18,i//9*16,0);current.use_fake_user=True;bpy.context.scene.collection.children.unlink(current)
 print('BUILT',name,records[-1]['triangles'],flush=True)
C1='01_Farm_Agricultural_Structures';C2='02_Rice_Fields_Farmland';C3='03_Road_Construction_Obstacles';C4='04_Animals_Countryside_Props'

def farmhouse(ma):
 box('StiltBase',(0,0,.20),(5,4,.40),'stone');box('House',(0,0,1.65),(4.6,3.6,2.9),'cream')
 for x in [-2.2,2.2]:
  for y in [-1.75,1.75]:box('CornerTrim',(x,y,1.65),(.14,.14,2.9),'timber')
 for z in [.65,1.05,1.45,1.85,2.25,2.65]:box('FrontCladding',(0,-1.815,z),(4.5,.035,.035),'bamboo')
 box('Door',(.65,-1.84,1.40),(.85,.08,2.10),'timber');ico('Knob',(.95,-1.90,1.38),(.035,)*3,'yellow')
 for x in [-1.15,1.6]:
  box('WindowFrame',(x,-1.86,1.94),(.87,.10,.90),'timber');box('Window',(x,-1.93,1.94),(.7,.025,.73),'glass');box('WindowCross',(x,-1.95,1.94),(.04,.035,.74),'cream')
 roof(5.3,4.4,3.12,ma,.8);box('Porch',(0,-2.35,.22),(3,1,.22),'bamboo');box('Step',(0,-2.98,.08),(1.3,.4,.16),'stone')
 for x in [-1.4,1.4]:pole(x,-2.7,2.7)
 box('PorchAwning',(0,-2.4,2.7),(3.3,1.35,.08),ma,(-.10,0,0))
asset(C1,'TR3_Farmhouse_RedRoof',lambda:farmhouse('red'))
asset(C1,'TR3_Farmhouse_BlueRoof',lambda:farmhouse('blue'))
def shed():
 box('Foundation',(0,0,.08),(3.2,2.7,.16),'stone');box('RearWall',(0,1.2,1.25),(3,.10,2.4),'bamboo')
 for x in [-1.45,1.45]:box('SideWall',(x,0,1.25),(.10,2.5,2.4),'bamboo')
 for x in [-1.45,0,1.45]:pole(x,-1.2,2.5,'timber')
 box('DoorPanel',(-.75,-1.24,1.15),(1.35,.06,2.15),'timber');beam('DoorBrace',(-1.3,-1.29,.3),(-.2,-1.29,2.1),.07,'cream');roof(3.5,3,2.5,'green',.45)
asset(C1,'TR3_Farm_Storage_Shed',shed)
def barn():
 box('Floor',(0,0,.07),(4.5,3.5,.14),'soil')
 for x in [-2,0,2]:
  for y in [-1.5,1.5]:pole(x,y,2.65,'timber')
 box('RearWall',(0,1.5,1.12),(4.1,.10,2.2),'bamboo')
 for x in [-2,2]:
  for z in [.55,1.1]:box('Rail',(x,0,z),(.09,3.1,.10),'timber')
 roof(4.8,3.9,2.7,'red',.6)
asset(C1,'TR3_Barn_Animal_Shelter',barn)
def windmill():
 for x in [-.7,.7]:
  for y in [-.7,.7]:
   beam('TowerLeg',(x,y,0),(x*.23,y*.23,4.7),.09,'metal')
 for z in [1.1,2.3,3.5]:
  t=.7*(1-z/5)+.05
  for y in [-t,t]:beam('TowerCross',(-t,y,z-.7),(t,y,z+.5),.055,'silver');beam('TowerCross',(t,y,z-.7),(-t,y,z+.5),.055,'silver')
 cyl('RotorHub',(0,-.29,4.75),.17,.48,'red',12,(math.pi/2,0,0))
 for i in range(8):
  a=i*math.tau/8;rad=Vector((math.cos(a),0,math.sin(a)));side=Vector((-math.sin(a),0,math.cos(a)));origin=Vector((0,-.56,4.75))
  beam('Spoke',origin,origin+rad*1.25,.035,'silver')
  vs=[tuple(origin+rad*.45-side*.055),tuple(origin+rad*1.25-side*.19),tuple(origin+rad*1.25+side*.19),tuple(origin+rad*.45+side*.055)]
  v=[(x,y+dy,z) for dy in [-.016,.016] for x,y,z in vs];f=[(3,2,1,0),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)];o=obj('Vane',v,f,(0,0,0),'silver');bm=bmesh.new();bm.from_mesh(o.data);bmesh.ops.recalc_face_normals(bm,faces=bm.faces);bm.to_mesh(o.data);bm.free()
 beam('TailArm',(0,.1,4.7),(1.5,1.2,4.7),.06,'metal');box('TailFin',(1.5,1.2,4.75),(.55,.07,.55),'red')
asset(C1,'TR3_Farm_Windmill',windmill)
def fence(w=2.0):
 for x in [-w/2+.07,w/2-.07]:box('Post',(x,0,.55),(.14,.14,1.1),'timber');cyl('PostCap',(x,0,1.16),.10,.12,'bamboo',4,top=.025)
 for z in [.35,.8]:box('FenceRail',(0,0,z),(w,.075,.11),'bamboo')
for w in [2,4]:asset(C1,'TR3_Fence_Wood_'+str(w)+'m',lambda w=w:fence(w),'Modular nominal length '+str(w)+' m')
def gate(opened=False):
 for x in [-1.12,1.12]:pole(x,0,1.35,'timber')
 angle=math.radians(65) if opened else 0
 def P(x,z):return (-1.05+(x+1.05)*math.cos(angle),(x+1.05)*math.sin(angle),z)
 for z in [.30,1.05]:beam('GateRail',P(-1.05,z),P(1.05,z),.10,'bamboo')
 for x in [-1.05,1.05]:beam('GateSide',P(x,.3),P(x,1.05),.08,'timber')
 beam('GateBrace',P(-1,.34),P(1,1.01),.075,'timber')
for state in [False,True]:asset(C1,'TR3_Fence_Gate_'+('Open' if state else 'Closed'),lambda state=state:gate(state))
def crop(k,p=(0,0,0)):
 x,y,z=p
 if k=='Pechay':
  for a in range(5):
   t=a*math.tau/5;ico('PechayLeaf',(x+math.cos(t)*.08,y+math.sin(t)*.08,z+.14),(.11,.10,.17),'green')
 else:
  beam('Stem',(x,y,z),(x,y,z+.52),.025,'leaf')
  for side in [-1,1]:
   ico('Leaf',(x+side*.10,y,z+.35),(.15,.06,.06),'green');ico('Produce',(x+side*.10,y-.04,z+.20),(.08,.065,.075 if k=='Tomato' else .15),'red' if k=='Tomato' else 'purple')
def crop_row(k):
 box('SoilBed',(0,0,.06),(2,.65,.12),'soil')
 for i in range(5):crop(k,(-.8+i*.4,0,.1))
for k in ['Pechay','Tomato','Eggplant']:asset(C1,'TR3_CropRow_'+k,lambda k=k:crop_row(k))
def plowed():
 box('SoilBase',(0,0,.04),(4,4,.08),'soil')
 for i in range(8):box('FurrowRidge',(-1.75+i*.5,0,.13),(.25,4,.18),'earth')
asset(C1,'TR3_Plowed_Farmland_4x4',plowed,'4 x 4 m tile; no road layout')
def hay(round_bale=False):
 if round_bale:
  cyl('HayRoll',(0,0,.48),.48,1.05,'hay',12,(math.pi/2,0,0))
  for y in [-.32,.32]:cyl('Binding',(0,y,.48),.49,.045,'timber',12,(math.pi/2,0,0))
 else:
  box('Bale',(0,0,.25),(.9,.5,.5),'hay')
  for x in [-.27,.27]:box('StrapTop',(x,0,.505),(.04,.51,.015),'timber');box('StrapFront',(x,-.255,.25),(.04,.015,.50),'timber');box('StrapBack',(x,.255,.25),(.04,.015,.50),'timber')
for b in [False,True]:asset(C1,'TR3_HayBale_'+('Round' if b else 'Rectangular'),lambda b=b:hay(b))
def tool(k):
 cyl('WoodHandle',(0,0,.81),.026,1.5,'bamboo',8)
 if k=='Shovel':
  ico('Blade',(0,0,.15),(.17,.035,.24),'metal');beam('DGripLeft',(-.07,0,1.55),(-.07,0,1.73),.035,'timber');beam('DGripRight',(.07,0,1.55),(.07,0,1.73),.035,'timber');beam('DGripTop',(-.07,0,1.73),(.07,0,1.73),.035,'timber')
 elif k=='Rake':
  box('RakeHead',(0,0,.13),(.48,.06,.06),'metal')
  for i in range(7):box('Tine',(-.21+i*.07,-.07,.065),(.022,.15,.13),'metal')
 else:box('HoeBlade',(0,-.1,.11),(.24,.28,.035),'metal')
for k in ['Shovel','Rake','Hoe']:asset(C1,'TR3_Tool_'+k,lambda k=k:tool(k))
def barrel(ma):
 cyl('BarrelBody',(0,0,.47),.31,.94,ma,12)
 for z in [.10,.45,.83]:cyl('Hoop',(0,0,z),.319,.05,'metal',12)
 cyl('BarrelLid',(0,0,.95),.30,.025,ma,12);cyl('Cap',(.14,0,.975),.045,.025,'dark',8)
for ma in ['blue','green']:asset(C1,'TR3_WaterBarrel_'+ma,lambda ma=ma:barrel(ma))

def rice_cluster(p=(0,0,0),mature=False):
 x,y,z=p
 for i in range(5):
  a=i*math.tau/5;dx=math.cos(a)*.075;dy=math.sin(a)*.075;h=.62 if not mature else .78
  beam('RiceStem',(x+dx,y+dy,z),(x+dx*1.5,y+dy*1.5,z+h),.012,'rice')
  for s in [-1,1]:
   # slender solid leaf, two sides and a folded pointed profile
   triangle_prism('RiceLeaf',[(x+dx-.013,y+dy,z+.20),(x+dx+.013,y+dy,z+.20),(x+dx+s*.20,y+dy,z+h*.9)],.009,'rice')
  if mature:
   beam('SeedHead',(x+dx*1.5,y+dy*1.5,z+h),(x+dx*1.5+.10,y+dy*1.5,z+h-.09),.028,'grain')
for mature in [False,True]:asset(C2,'TR3_RiceCluster_'+('Mature' if mature else 'Young'),lambda mature=mature:rice_cluster(mature=mature))
def riceplot(w,d,flood=False,planted=True):
 box('PaddyBase',(0,0,.045),(w,d,.09),'mud')
 if flood:box('WaterSurface',(0,0,.105),(w-.30,d-.30,.045),'water')
 for x in [-w/2+.075,w/2-.075]:box('EarthBund',(x,0,.13),(.15,d,.26),'earth')
 for y in [-d/2+.075,d/2-.075]:box('EarthBund',(0,y,.13),(w-.30,.15,.26),'earth')
 if planted:
  nx=3 if w==4 else 6
  for i in range(nx):
   for j in range(3):rice_cluster((-w/2+.75+i*1.1,-d/2+.75+j*1.1,.12),False)
asset(C2,'TR3_RicePlot_Square_4x4',lambda:riceplot(4,4))
asset(C2,'TR3_RicePlot_Rectangular_8x4',lambda:riceplot(8,4))
asset(C2,'TR3_FloodedPaddy_Planted_4x4',lambda:riceplot(4,4,True))
asset(C2,'TR3_FloodedPaddy_Empty_4x4',lambda:riceplot(4,4,True,False))
asset(C2,'TR3_FieldBorder_Straight_4m',lambda:box('Bund',(0,0,.13),(4,.3,.26),'earth'))
def bordercorner():
 box('BundX',(.85,0,.13),(2,.3,.26),'earth');box('BundY',(0,.85,.13),(.3,2,.26),'earth')
asset(C2,'TR3_FieldBorder_Corner',bordercorner)
def ditch(water=True):
 box('DitchBed',(0,0,.025),(1,4,.05),'mud')
 for x in [-.42,.42]:box('Bank',(x,0,.22),(.16,4,.44),'earth')
 if water:box('Water',(0,0,.14),(.68,4,.06),'water')
for wet in [False,True]:asset(C2,'TR3_IrrigationDitch_'+('Wet' if wet else 'Dry')+'_4m',lambda wet=wet:ditch(wet))
asset(C2,'TR3_FarmPath_Narrow_4m',lambda:box('Path',(0,0,.06),(1,4,.12),'earth'))
def hut():
 for x in [-.85,.85]:
  for y in [-.70,.70]:pole(x,y,2.2)
 box('RaisedDeck',(0,0,.45),(2,1.7,.1),'bamboo')
 for y in [-.7,.7]:box('HalfWall',(0,y,.85),(1.8,.06,.72),'bamboo')
 roof(2.6,2.3,2.25,'hay',.75);box('Step',(0,-1.1,.20),(.7,.4,.4),'timber')
asset(C2,'TR3_RiceField_Hut',hut)
for shape in ['Square','Irregular']:
 def soil(shape=shape):
  if shape=='Square':box('SoilPatch',(0,0,.03),(2,2,.06),'soil')
  else:ico('SoilPatch',(0,0,.035),(1.2,.9,.07),'soil')
 asset(C2,'TR3_SoilPatch_'+shape,soil)

def excavator():
 for y in [-.81,.81]:
  box('TrackBelt',(0,y,.35),(2.8,.48,.60),'dark')
  for x in [-1.05,-.52,0,.52,1.05]:cyl('TrackWheel',(x,y-.25 if y<0 else y+.25,.35),.22,.035,'metal',10,(math.pi/2,0,0))
  for i in range(14):
   for z in [.055,.66]:box('TrackTread',(-1.3+i*.2,y,z),(.11,.51,.045),'metal')
 box('Chassis',(0,0,.72),(2,.9,.24),'metal');cyl('Turntable',(0,0,.89),.66,.20,'dark',12)
 box('UpperBody',(-.25,0,1.18),(1.9,1.45,.48),'yellow');box('Counterweight',(-1.13,0,1.25),(.43,1.55,.55),'yellow')
 box('CabFrame',(-.35,-.35,1.85),(1.1,.85,1.1),'yellow');box('SideGlass',(-.35,-.79,1.94),(.87,.035,.72),'glass');box('FrontGlass',(.22,-.35,1.95),(.035,.64,.70),'glass');box('CabRoof',(-.35,-.35,2.45),(1.25,.98,.12),'yellow')
 box('EngineGrille',(-.6,.74,1.28),(.75,.025,.27),'dark')
 for i in range(6):box('GrilleRib',(-.9+i*.12,.76,1.28),(.025,.025,.24),'metal')
 a=(.6,.4,1.35);b=(1.58,.4,3.20);c=(2.75,.4,1.65)
 beam('Boom',a,b,.32,'yellow');beam('Stick',b,c,.25,'yellow');beam('HydraulicCylinder',(.58,.32,1.5),(1.0,.32,2.45),.12,'metal');beam('Piston',(1,.32,2.45),(1.47,.32,2.95),.065,'silver')
 for p in [a,b,c]:cyl('JointPin',p,.19,.4,'metal',12,(math.pi/2,0,0))
 # Open digging bucket: back, floor, sides, lip and teeth.
 box('BucketBack',(2.95,.4,1.35),(.12,.85,.65),'metal',(0,-.3,0));box('BucketFloor',(3.17,.4,1.06),(.62,.85,.12),'metal')
 for y in [-.04,.84]:box('BucketSide',(3.15,y,1.27),(.57,.065,.50),'yellow')
 for y in [.06,.28,.50,.72]:box('BucketTooth',(3.58,y,1.04),(.32,.10,.10),'silver')
 cyl('Exhaust',(-.9,.46,1.91),.055,.65,'dark',8);panel('TODA WORKS',(-.53,-.81,1.35),1,.20,'yellow',.09)
asset(C3,'TR3_Excavator_Tracked',excavator,'Static prop; editable components, no rig or driving physics')
def barricade():
 for x in [-.9,.9]:
  for y in [-.35,.35]:beam('ALeg',(x,y,0),(x,0,1.15),.09,'timber')
 box('Board',(0,-.05,.86),(2.2,.13,.38),'white')
 for x in [-.8,-.3,.2,.7]:box('OrangeStripe',(x,-.122,.86),(.18,.012,.37),'orange',(0,0,0))
asset(C3,'TR3_Barricade_Striped',barricade)
def cone(h):
 box('ConeBase',(0,0,.035),(.48*h/.75,.48*h/.75,.07),'dark')
 for za,zb,ma in [(.07,.30,'orange'),(.30,.43,'white'),(.43,.57,'orange'),(.57,.66,'white'),(.66,.75,'orange')]:
  scale=h/.75;a=za*scale;b=zb*scale;r1=.20*(1-za/.85)*scale;r2=.20*(1-zb/.85)*scale;cyl('ConeBand',(0,0,(a+b)/2),r1,b-a,ma,10,top=r2)
for h in [.5,.75]:asset(C3,'TR3_TrafficCone_'+('Small' if h==.5 else 'Standard'),lambda h=h:cone(h))
def road_sign(kind):
 cyl('Post',(0,0,1.05),.035,2.1,'metal',8);box('WeightedFoot',(0,0,.055),(.48,.48,.11),'stone')
 if kind=='NoEntry':
  cyl('RoundSign',(0,-.03,1.9),.4,.065,'red',12,(math.pi/2,0,0));box('WhiteBar',(0,-.069,1.9),(.60,.018,.13),'white')
 else:
  label,w,h,ma,size={'RoadClosed':('ROAD CLOSED',1.6,.5,'red',.19),'Detour':('DETOUR >',1.4,.5,'yellow',.20),'ArrowLeft':('<',.8,.5,'yellow',.42),'ArrowRight':('>',.8,.5,'yellow',.42),'Warning':('INGAT!\nROAD WORK',1.3,.7,'orange',.18)}[kind]
  panel(label,(0,-.035,1.9),w,h,ma,size)
for k in ['RoadClosed','NoEntry','Detour','ArrowLeft','ArrowRight','Warning']:asset(C3,'TR3_Sign_'+k,lambda k=k:road_sign(k))
def warningboard():
 for x in [-.7,.7]:
  for y in [-.3,.3]:beam('Leg',(x,y,0),(x,0,1.25),.08,'timber')
 panel('DANGER\nCONSTRUCTION\nKEEP OUT',(0,-.04,.94),1.7,.80,'yellow',.16)
asset(C3,'TR3_Construction_WarningBoard',warningboard)
def workfence():
 for x in [-1,1]:pole(x,0,1.3,'metal');box('Foot',(x,0,.04),(.45,.48,.08),'dark')
 for z in [.35,.65,.95,1.23]:box('OrangeMeshRail',(0,0,z),(2,.035,.035),'orange')
 for x in [-.8,-.6,-.4,-.2,0,.2,.4,.6,.8]:box('OrangeMeshUpright',(x,0,.79),(.025,.035,.9),'orange')
asset(C3,'TR3_Roadwork_Fencing_2m',workfence)
for size in ['Small','Large']:
 def dirt(size=size):
  s=.7 if size=='Small' else 1.4;ico('DirtMound',(0,0,s*.3),(s,s*.8,s*.6),'earth')
  for x,y in [(-.4,-.3),(.3,.2),(.5,-.2)]:ico('DirtClod',(x*s,y*s,.10*s),(.20*s,.17*s,.17*s),'soil')
 asset(C3,'TR3_DirtPile_'+size,dirt)
for i,s in enumerate([(.22,.18,.17),(.45,.30,.28),(.75,.55,.5)]):asset(C3,'TR3_Rock_'+str(i+1),lambda s=s:ico('Rock',(0,0,s[2]),s,'stone'))
def debris():
 for i in range(7):ico('Rubble',((i%3)*.35-.35,(i//3)*.30-.30,.1),(.17,.13,.12),'stone')
 for y in [-.20,.20]:box('BrokenPlank',(0,y,.16),(1.3,.12,.045),'timber',(0,.08,y))
asset(C3,'TR3_Debris_Cluster',debris)
def barrier(ma):
 # solid tapered jersey barrier cross section
 v=[(x,y,z) for x in [-1,1] for y,z in [(-.30,0),(.30,0),(.30,.20),(.12,.8),(-.12,.8),(-.30,.2)]]
 f=[tuple(reversed(range(6))),tuple(range(6,12))]+[(i,(i+1)%6,(i+1)%6+6,i+6) for i in range(6)]
 o=obj('JerseyBarrier',v,f,(0,0,0),ma);bm=bmesh.new();bm.from_mesh(o.data);bmesh.ops.recalc_face_normals(bm,faces=bm.faces);bm.to_mesh(o.data);bm.free()
 for x in [-.65,0,.65]:box('Reflector',(x,-.208,.49),(.18,.02,.10),'white')
for ma in ['orange','stone']:asset(C3,'TR3_TemporaryBarrier_'+ma,lambda ma=ma:barrier(ma))
def warninglight(stand=False):
 if stand:
  for x,y in [(-.35,-.22),(.35,-.22),(0,.35)]:beam('Tripod',(x,y,0),(0,0,1.1),.04,'metal')
  z=1.15
 else:box('Base',(0,0,.045),(.22,.22,.09),'dark');z=.14
 cyl('BeaconBase',(0,0,z),.13,.12,'dark',10);cyl('AmberLens',(0,0,z+.17),.12,.23,'amber',10,top=.085);cyl('LensCap',(0,0,z+.30),.085,.025,'yellow',10)
for stand in [False,True]:asset(C3,'TR3_WarningLight_'+('Tripod' if stand else 'Portable'),lambda stand=stand:warninglight(stand),'Emissive lens material; no actual light object or blinking animation')

def cow(ma='white',graze=False):
 ico('CowTorso',(0,0,1.13),(.94,.39,.47),ma);ico('Shoulder',(.55,0,1.22),(.40,.34,.39),ma)
 for x in [-.58,.58]:
  for y in [-.24,.24]:
   beam('Leg',(x,y,.83),(x+.04,y,.17),.13,ma);box('Hoof',(x+.05,y,.09),(.20,.15,.18),'dark')
 headz=.56 if graze else 1.40;hx=1.02
 beam('Neck',(.58,0,1.3),(hx,0,headz+.12),.32,ma);ico('Head',(hx+.10,0,headz),(.36,.23,.25),ma);ico('Muzzle',(hx+.35,0,headz-.10),(.19,.24,.15),'pink')
 for side in [-1,1]:
  ico('Ear',(hx,side*.31,headz+.15),(.17,.15,.065),ma);ico('Eye',(hx+.17,side*.21,headz+.08),(.035,.025,.035),'dark')
  cyl('Horn',(hx-.03,side*.18,headz+.32),.055,.24,'cream',6,top=.012)
  for x,z in [(-.45,1.2),(.2,1.35)]:ico('CoatPatch',(x,side*.345,z),(.25,.06,.23),'black' if ma=='white' else 'cream')
 beam('Tail',(-.83,0,1.2),(-1.02,0,.55),.045,ma);ico('TailTuft',(-1.02,0,.51),(.07,.07,.13),'dark');ico('Udder',(-.37,0,.79),(.20,.16,.13),'pink')
asset(C4,'TR3_Cow_BlackWhite',cow,'Static unrigged animal; forward +X')
asset(C4,'TR3_Cow_Brown_Grazing',lambda:cow('brown',True),'Static grazing pose; no animation')
def goat(ma):
 ico('Torso',(0,0,.64),(.55,.22,.28),ma)
 for x in [-.35,.35]:
  for y in [-.15,.15]:beam('Leg',(x,y,.55),(x+.025,y,.08),.065,ma);box('Hoof',(x+.025,y,.055),(.10,.08,.11),'dark')
 beam('Neck',(.35,0,.72),(.55,0,1.03),.15,ma);ico('Head',(.65,0,1.03),(.22,.14,.18),ma);ico('Muzzle',(.80,0,.97),(.13,.12,.09),'cream')
 for side in [-1,1]:
  ico('Ear',(.58,side*.21,1.06),(.16,.12,.04),ma);ico('Eye',(.72,side*.13,1.08),(.018,)*3,'black');beam('Horn',(.58,side*.09,1.16),(.44,side*.11,1.42),.045,'timber')
 cyl('Beard',(.72,0,.79),.055,.22,'cream',6,top=.025);beam('Tail',(-.5,0,.75),(-.64,0,.94),.05,ma)
for ma in ['cream','brown']:asset(C4,'TR3_Goat_'+ma,lambda ma=ma:goat(ma),'Static unrigged animal; forward +X')
def chicken(ma):
 ico('Body',(0,0,.28),(.27,.18,.23),ma)
 for side in [-1,1]:
  ico('Wing',(-.025,side*.16,.28),(.20,.055,.13),ma);beam('Leg',(0,side*.09,.18),(.025,side*.09,.035),.022,'yellow')
  for dy in [-.035,.035]:beam('Toe',(.025,side*.09,.025),(.15,side*.09+dy,.02),.018,'yellow')
 ico('Neck',(.19,0,.47),(.10,.10,.18),ma);ico('Head',(.24,0,.59),(.12,.105,.12),ma)
 for side in [-1,1]:ico('Eye',(.29,side*.09,.62),(.018,)*3,'black')
 ico('Beak',(.38,0,.58),(.10,.05,.045),'yellow')
 for x in [.17,.23,.29]:ico('Comb',(x,0,.72),(.045,.04,.065),'red')
 ico('Wattle',(.30,0,.48),(.045,.04,.065),'red')
 for side in [-1,0,1]:ico('TailFeather',(-.25,side*.06,.45),(.12,.045,.22),'dark' if ma=='brown' else 'cream')
for ma in ['white','brown']:asset(C4,'TR3_Chicken_'+ma,lambda ma=ma:chicken(ma),'Static unrigged animal; forward +X')
def trough():
 box('TroughBase',(0,0,.30),(1.5,.5,.08),'timber')
 for y in [-.25,.25]:box('Wall',(0,y,.46),(1.5,.07,.35),'bamboo')
 for x in [-.72,.72]:box('End',(x,0,.46),(.07,.5,.35),'bamboo');box('Leg',(x,0,.15),(.12,.32,.30),'timber')
 box('Feed',(0,0,.48),(1.30,.36,.035),'hay')
asset(C4,'TR3_Animal_FeedingTrough',trough)
def crate():
 box('Base',(0,0,.03),(.65,.45,.06),'bamboo')
 for x in [-.30,.30]:
  for y in [-.20,.20]:box('Corner',(x,y,.22),(.055,.055,.44),'timber')
 for z in [.12,.26,.40]:
  for y in [-.225,.225]:box('Slat',(0,y,z),(.65,.035,.075),'bamboo')
  for x in [-.325,.325]:box('Slat',(x,0,z),(.035,.45,.075),'bamboo')
asset(C4,'TR3_Crate_Wooden',crate)
def basket():
 cyl('Base',(0,0,.025),.22,.05,'bamboo',12)
 for i in range(12):
  a=i*math.tau/12;beam('Upright',(.22*math.cos(a),.22*math.sin(a),.02),(.32*math.cos(a),.32*math.sin(a),.37),.025,'bamboo')
 for z in [.1,.2,.3,.38]:
  r=.22+.10*z/.38
  for i in range(12):
   a=i*math.tau/12;b=(i+1)*math.tau/12;beam('WovenBand',(r*math.cos(a),r*math.sin(a),z),(r*math.cos(b),r*math.sin(b),z),.023,'timber')
asset(C4,'TR3_Basket_Woven',basket)
def sack():
 ico('Sack',(0,0,.37),(.29,.23,.39),'cream');cyl('TiedMouth',(0,0,.76),.065,.12,'bamboo',8);panel('BIGAS',(0,-.219,.40),.35,.16,'green',.075)
asset(C4,'TR3_RiceSack_Upright',sack)
def sacks():
 sack();start=len(parts);sack()
 for o in parts[start:]:o.location.x+=.58
 start=len(parts);sack()
 for o in parts[start:]:o.location+=Vector((.29,0,.70))
asset(C4,'TR3_RiceSacks_Stack',sacks)
for k in ['Tomato','Eggplant','Pechay']:
 def filled(k=k):
  basket()
  for x,y in [(-.12,-.10),(.10,-.12),(-.1,.1),(.12,.1),(0,0)]:
   if k=='Pechay':ico('Pechay',(x,y,.35),(.10,.09,.15),'green')
   else:ico('Produce',(x,y,.35),(.085,.07,.08 if k=='Tomato' else .14),'red' if k=='Tomato' else 'purple')
 asset(C4,'TR3_ProduceBasket_'+k,filled)
def cart():
 box('CartFloor',(0,0,.55),(1.7,.95,.1),'bamboo')
 for y in [-.48,.48]:
  for z in [.72,.95]:box('SideRail',(0,y,z),(1.8,.07,.10),'bamboo')
  for x in [-.8,.8]:box('CornerPost',(x,y,.80),(.08,.08,.6),'timber')
 for x in [-.83,.83]:
  for z in [.72,.95]:box('EndRail',(x,0,z),(.07,.95,.10),'bamboo')
 beam('Axle',(-.15,-.75,.40),(-.15,.75,.40),.09,'metal')
 for y in [-.64,.64]:
  cyl('Wheel',(-.15,y,.4),.39,.10,'timber',12,(math.pi/2,0,0));cyl('Hub',(-.15,y+(.06 if y>0 else -.06),.4),.11,.04,'metal',10,(math.pi/2,0,0))
 for y in [-.32,.32]:beam('PullHandle',(.8,y,.50),(2.3,y,.8),.075,'timber')
 box('RestLeg',(1,0,.26),(.10,.10,.52),'timber')
asset(C4,'TR3_Cart_Wooden_Handcart',cart)
def container(k):
 if k=='JerryCan':
  box('CanBody',(0,0,.30),(.34,.20,.6),'blue');box('Shoulder',(0,0,.62),(.30,.20,.07),'blue');box('GripTop',(0,0,.76),(.22,.055,.045),'dark')
  for x in [-.09,.09]:box('GripSide',(x,0,.70),(.04,.055,.14),'dark')
  cyl('Cap',(.10,0,.68),.045,.055,'dark',8)
 else:
  cyl('Jug',(0,0,.29),.18,.52,'water',12,top=.16);cyl('Shoulder',(0,0,.59),.16,.10,'water',12,top=.065);cyl('Neck',(0,0,.68),.065,.08,'water',10);cyl('Cap',(0,0,.73),.075,.035,'blue',10)
for k in ['JerryCan','WaterJug']:asset(C4,'TR3_WaterContainer_'+k,lambda k=k:container(k))

for col in bpy.data.collections:
 if col.name.startswith('TR3_') and col.name not in bpy.context.scene.collection.children:bpy.context.scene.collection.children.link(col)
bpy.context.scene['description']='Level 3 reusable asset inspection catalog ONLY; no map or road layout.'
bpy.context.scene['asset_count']=len(records)
with open(R/'asset_manifest.json','w') as f:json.dump({'units':'meters','dimensions':'Blender X width / Y depth / Z height; GLB Y-up','origin':'ground center of bounding box','assets':records},f,indent=2)
with open(R/'asset_manifest.csv','w',newline='') as f:w=csv.DictWriter(f,fieldnames=list(records[0]));w.writeheader();w.writerows(records)
bpy.ops.object.select_all(action='DESELECT');bpy.ops.outliner.orphans_purge(do_recursive=True)
bpy.ops.wm.save_as_mainfile(filepath=str(R/'TODA_RUSH_Level3_Bukid_Assets.blend'))
print('COMPLETE',len(records),sum(a['triangles'] for a in records),flush=True)
