# MushTaker UI 도트 스프라이트 생성. 사용: python tools/ui/make_ui_sprites.py <출력 폴더> <미리보기.png>
import math, os, sys
from PIL import Image
OUT = sys.argv[1]; os.makedirs(OUT, exist_ok=True)
K=(34,34,34,255); W=(255,255,255,255); S=(186,186,186,255); T=(0,0,0,0)

def new(w,h): return Image.new("RGBA",(w,h),T)
def outline(img, col):
    w,h=img.size; px=img.load(); add=[]
    for y in range(h):
        for x in range(w):
            if px[x,y][3]: continue
            for dx,dy in ((1,0),(-1,0),(0,1),(0,-1)):
                nx,ny=x+dx,y+dy
                if 0<=nx<w and 0<=ny<h and px[nx,ny][3] and px[nx,ny]!=col:
                    add.append((x,y)); break
    for p in add: px[p]=col
    return img

def frame(top=0, bottom_shade=0, inset=False, ring=False):
    im=new(16,16); px=im.load()
    for y in range(top,16):
        for x in range(16):
            edge = x in (0,15) or y in (top,15)
            corner = (x in (0,15)) and (y in (top,15))
            if corner: continue
            if edge: px[x,y]=K; continue
            if ring:
                px[x,y]= W if (x in (1,14) or y in (top+1,14)) else T; continue
            c=W
            if bottom_shade and y>=15-bottom_shade: c=S
            if inset and (x==1 or y==top+1): c=S
            px[x,y]=c
    return im

sprites={}
sprites["UI_Panel"]=frame(bottom_shade=2)
sprites["UI_Button"]=frame(bottom_shade=3)
sprites["UI_Button_Pressed"]=frame(top=2,bottom_shade=1)
sprites["UI_Slot"]=frame(inset=True)
sprites["UI_GaugeFrame"]=frame(ring=True)
g=new(16,16); gp=g.load()
for y in range(16):
    for x in range(16): gp[x,y]= S if y>=14 else W
sprites["UI_GaugeFill"]=g

def ell(cx,cy,rx,ry,x,y): return ((x+0.5-cx)/rx)**2+((y+0.5-cy)/ry)**2

# 송이
im=new(24,24); px=im.load()
cap=(139,94,60,255); capD=(104,66,40,255); capH=(184,136,92,255)
st=(236,224,196,255); stD=(198,180,146,255)
for y in range(9,22):
    w0,w1=(9,14) if y<19 else (8,15)
    for x in range(w0,w1+1): px[x,y]= stD if x>=w1-1 else st
for y in range(2,12):
    for x in range(24):
        if ell(12,9,8,6.5,x,y)<=1 and y<=10:
            px[x,y]= capD if y>=10 else (capH if (x+0.5-12)/8+(y+0.5-9)/6.5<-0.85 else cap)
sprites["Icon_Songi"]=outline(im,(52,34,24,255))

# 구름버섯
im=new(24,24); px=im.load()
bands=[(0.82,(232,222,202,255)),(0.62,(122,126,144,255)),(0.42,(134,98,72,255)),(0,(86,64,52,255))]
for cx,cy,rx,ry in ((14,9,7,5),(10,14,8,5.5),(12,19,10,6)):
    for y in range(24):
        for x in range(24):
            if y+0.5>cy: continue
            r=math.sqrt(ell(cx,cy,rx,ry,x,y))
            if r>1: continue
            for th,c in bands:
                if r>=th: px[x,y]=c; break
sprites["Icon_Cloud"]=outline(im,(48,40,40,255))

# 말불버섯
im=new(24,24); px=im.load()
b=(232,222,196,255); bD=(198,184,152,255); bH=(250,246,232,255); dot=(176,160,128,255)
for y in range(18,22):
    for x in range(9,15): px[x,y]= bD if x>=13 else b
for y in range(24):
    for x in range(24):
        e=ell(12,12.5,8.5,7.5,x,y)
        if e<=1:
            u=(x+0.5-12)/8.5; v=(y+0.5-12.5)/7.5
            px[x,y]= bH if u+v<-0.9 else (bD if u+v>0.7 else b)
for p in ((9,9),(14,8),(16,12),(10,14),(13,15),(7,12)): px[p]=dot
px[12,5]=(120,104,84,255); px[11,5]=(120,104,84,255)
sprites["Icon_Malbul"]=outline(im,(70,58,44,255))

# 골드
im=new(16,16); px=im.load()
for y in range(16):
    for x in range(16):
        e=ell(8,8,6.5,6.5,x,y)
        if e<=1:
            c=(242,193,78,255)
            if 0.45<=e<=0.62: c=(204,146,44,255)
            if (x+0.5-8)+(y+0.5-8)<-6 and e>0.3: c=(255,240,170,255)
            px[x,y]=c
for y in range(6,10): px[8,y]=(204,146,44,255)
sprites["Icon_Gold"]=outline(im,(92,60,22,255))

# 탭 아이콘(흰색, 코드에서 색 입힘)
O=(30,30,30,255)
def line(px,x0,y0,x1,y1,th=2):
    n=max(abs(x1-x0),abs(y1-y0))
    for i in range(n+1):
        x=round(x0+(x1-x0)*i/n); y=round(y0+(y1-y0)*i/n)
        for dx in range(th):
            if 0<=x+dx<24: px[x+dx,y]=W
def rect(px,x0,y0,x1,y1):
    for y in range(y0,y1+1):
        for x in range(x0,x1+1): px[x,y]=W

im=new(24,24); px=im.load()            # 정비: 망치
rect(px,6,3,17,8); rect(px,11,9,12,21); rect(px,4,4,5,7)
sprites["Tab_Gear"]=outline(im,O)

im=new(24,24); px=im.load()            # 토벌: 칼
rect(px,10,3,13,14); px[11,2]=W; px[12,2]=W
rect(px,6,15,17,16); rect(px,11,17,12,20); rect(px,10,21,13,21)
for y in range(4,14): px[12,y]=(200,200,200,255)
sprites["Tab_Hunt"]=outline(im,O)

im=new(24,24); px=im.load()            # 개량: 플라스크
rect(px,10,2,13,8); rect(px,9,2,14,3)
for y in range(9,21):
    hw=2+(y-9)*6//11
    for x in range(12-hw,12+hw): px[x,y]=W
for p in ((10,16),(13,14),(11,18),(14,18)): px[p]=O
sprites["Tab_Breed"]=outline(im,O)

im=new(24,24); px=im.load()            # 파견: 깃발
rect(px,6,3,7,20); rect(px,4,20,10,21)
for y in range(4,12):
    for x in range(8,19):
        if x>=16 and abs(y-7.5)<(x-15): continue
        px[x,y]=W
sprites["Tab_Dispatch"]=outline(im,O)

for n,i in sprites.items(): i.save(f"{OUT}/{n}.png")

# 미리보기: 6배, 틀류는 색 입혀서
tint={"UI_Panel":(44,59,48),"UI_Button":(217,87,59),"UI_Button_Pressed":(217,87,59),"UI_Slot":(63,82,68),"UI_GaugeFrame":(243,233,210),"UI_GaugeFill":(123,201,80)}
for k in ("Tab_Gear","Tab_Hunt","Tab_Breed","Tab_Dispatch"): tint[k]=(243,233,210)
names=list(sprites); sc=6; cell=24*sc+40
pv=Image.new("RGBA",(cell*7,cell*2+10),(30,42,34,255))
for i,n in enumerate(names):
    im=sprites[n]
    if n in tint:
        t=tint[n]; im=im.copy(); p=im.load()
        for y in range(im.size[1]):
            for x in range(im.size[0]):
                r,g_,b_,a=p[x,y]; p[x,y]=(r*t[0]//255,g_*t[1]//255,b_*t[2]//255,a)
    big=im.resize((im.size[0]*sc,im.size[1]*sc),Image.NEAREST)
    pv.alpha_composite(big,((i%7)*cell+20,(i//7)*cell+20))
pv.save(sys.argv[2])
print(names)
