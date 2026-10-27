"""Build original low-poly UI illustrations and bundle local licensed typography."""
from pathlib import Path
from PIL import Image, ImageDraw
import urllib.request, shutil
from fontTools.ttLib import TTFont
from fontTools.varLib.instancer import instantiateVariableFont
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'Shared/Design';OUT.mkdir(parents=True,exist_ok=True)
fonts={'LilitaOne.ttf':'https://raw.githubusercontent.com/google/fonts/main/ofl/lilitaone/LilitaOne-Regular.ttf',
 'NunitoSans-Variable.ttf':'https://raw.githubusercontent.com/google/fonts/main/ofl/nunitosans/NunitoSans%5BYTLC%2Copsz%2Cwdth%2Cwght%5D.ttf',
 'LICENSE-Lilita.txt':'https://raw.githubusercontent.com/google/fonts/main/ofl/lilitaone/OFL.txt',
 'LICENSE-Nunito.txt':'https://raw.githubusercontent.com/google/fonts/main/ofl/nunitosans/OFL.txt'}
for name,url in fonts.items():
 p=OUT/name
 if not p.exists():urllib.request.urlretrieve(url,p)
f=TTFont(OUT/'NunitoSans-Variable.ttf');axes={a.axisTag:a.defaultValue for a in f['fvar'].axes};axes['wght']=600
instantiateVariableFont(f,axes,inplace=True).save(OUT/'NunitoSans.ttf')
im=Image.new('RGB',(1080,420),'#102b25');d=ImageDraw.Draw(im)
d.polygon([(0,170),(240,100),(380,160),(540,90),(760,150),(1080,60),(1080,420),(0,420)],fill='#244e40')
d.polygon([(0,250),(270,200),(480,260),(760,140),(1080,210),(1080,420),(0,420)],fill='#3f9c9a')
d.polygon([(100,420),(230,270),(460,225),(640,260),(850,420)],fill='#d7c196')
d.polygon([(230,270),(460,225),(540,300),(100,420)],fill='#f4e6c4')
d.polygon([(460,225),(590,145),(690,235),(640,260)],fill='#477153')
d.polygon([(590,145),(690,235),(640,260),(620,210)],fill='#2f5c42')
# Lost sail, castaway and route: story-specific landmarks rather than stock jungle ornaments.
d.polygon([(270,302),(310,287),(350,304),(320,321)],fill='#6a4028');d.line([(310,300),(310,207)],fill='#e8b75f',width=7)
d.polygon([(301,212),(260,272),(301,267)],fill='#f4e6c4')
for x,y in [(365,329),(405,308),(444,301),(484,309),(529,325),(576,325)]:d.ellipse((x,y,x+8,y+8),fill='#6a4028')
d.ellipse((585,270,610,297),fill='#e8b75f');d.polygon([(582,298),(610,298),(616,334),(575,334)],fill='#102b25');d.line([(584,330),(580,351)],fill='#102b25',width=8);d.line([(607,330),(612,351)],fill='#102b25',width=8)
for x,y in [(80,190),(915,153)]:
 d.polygon([(x,y),(x+10,y),(x+15,y+160),(x-4,y+160)],fill='#6a4028')
 for pts in [[(x,y),(x-90,y+20),(x-40,y-32)],[(x,y),(x+95,y+5),(x+47,y-40)],[(x,y),(x+5,y-68),(x+40,y-17)]]:d.polygon(pts,fill='#477153')
im.save(OUT/'island-route.png')
im=Image.new('RGBA',(192,192));d=ImageDraw.Draw(im)
d.polygon([(46,12),(146,12),(180,48),(180,144),(145,180),(45,180),(12,143),(12,48)],fill='#e8b75f')
d.polygon([(52,96),(56,72),(69,69),(73,101),(75,40),(89,35),(94,93),(99,28),(112,30),(117,95),(124,44),(138,49),(135,112),(143,86),(155,92),(147,136),(121,155),(86,152),(67,130)],fill='#102b25')
im.save(OUT/'island-seal.png')
for target in [ROOT/'PWA/public/brand',ROOT/'Companion/assets/brand',ROOT/'Unity/Assets/_Project/Resources/Brand']:
 target.mkdir(parents=True,exist_ok=True)
 for name in ['LilitaOne.ttf','NunitoSans.ttf','LICENSE-Lilita.txt','LICENSE-Nunito.txt','island-route.png','island-seal.png']:shutil.copy2(OUT/name,target/name)
