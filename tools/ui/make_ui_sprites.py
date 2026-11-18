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
sprites["Icon_001_Songi"]=outline(im,(52,34,24,255))

# 구름버섯(Gureum)
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
sprites["Icon_002_Gureum"]=outline(im,(48,40,40,255))

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
sprites["Icon_003_Malbul"]=outline(im,(70,58,44,255))

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

# 노루궁뎅이: 하얀 털뭉치 + 아래로 늘어진 가시
im=new(24,24); px=im.load()
nb=(240,232,208,255); nl=(252,248,236,255); nd=(214,198,160,255)
for y in range(18,22):
    for x in range(10,14): px[x,y]=nd
for y in range(24):
    for x in range(24):
        if ell(12,10.5,9,7,x,y)<=1:
            u=(x+0.5-12)/9; v=(y+0.5-10.5)/7
            px[x,y]= nl if u+v<-0.8 else (nd if u+v>0.8 else nb)
for x in range(5,20,2):
    for d in range(1,3+(x*7)%3):
        if px[x,17+d-1][3]==0 or d>1: px[x,16+d]= nl if x%4==1 else nb
for p in ((8,6),(14,5),(11,9),(16,9),(7,11),(13,12)): px[p]=nl
sprites["Icon_004_Noru"]=outline(im,(110,96,72,255))

# 표고: 납작한 갈색 갓 + 흰 갈라짐
im=new(24,24); px=im.load()
pc=(120,74,46,255); pd=(88,52,32,255); pk=(226,206,176,255); ps=(232,220,196,255); psd=(196,180,150,255)
for y in range(12,21):
    for x in range(10,14): px[x,y]= psd if x==13 else ps
for y in range(24):
    for x in range(24):
        if ell(12,12,10,7,x,y)<=1 and y<=11:
            px[x,y]= pd if ell(12,12,10,7,x,y)>0.7 else pc
for x in range(4,21): px[x,12]=psd
for p in ((8,7),(9,7),(12,6),(13,7),(16,8),(15,8),(10,9),(6,10),(18,10),(11,5)): px[p]=pk
sprites["Icon_005_Pyogo"]=outline(im,(56,34,20,255))

# 팽이: 가는 흰 줄기 다발 + 작은 갓
im=new(24,24); px=im.load()
pw=(246,242,226,255); pcap=(214,186,118,255); pb=(190,160,110,255)
stems=((7,9),(9,4),(11,7),(13,2),(15,6),(17,9))
for sx,top in stems:
    for y in range(top+2,20): px[sx,y]=pw
    for dx in (-1,0,1): px[sx+dx,top+1]=pcap
    px[sx,top]=pcap
# 줄기 사이를 옅은 그늘로 메워 한 다발로 보이게
for y in range(5,20):
    xs=[x for x in range(24) if px[x,y]==pw]
    if len(xs)<2: continue
    for x in range(xs[0],xs[-1]+1):
        if px[x,y][3]==0: px[x,y]=(214,206,182,255)
for y in range(19,22):
    for x in range(6,19): px[x,y]=pb
sprites["Icon_006_Paengi"]=outline(im,(90,76,52,255))

# 광대버섯: 빨간 갓 + 흰 점, 흰 줄기와 턱받이
im=new(24,24); px=im.load()
gr=(214,52,40,255); gd=(170,36,30,255); gw=(248,244,232,255); gs=(244,238,220,255); gsd=(206,198,176,255)
for y in range(10,21):
    for x in range(10,14): px[x,y]= gsd if x==13 else gs
for x in range(8,16): px[x,20]=gs; px[x,21]=gsd
for x in range(9,15): px[x,13]=gw
for y in range(24):
    for x in range(24):
        if ell(12,11,9.5,8,x,y)<=1 and y<=10:
            px[x,y]= gd if y>=10 else gr
for p in ((8,6),(9,6),(13,4),(14,4),(16,7),(11,8),(6,9),(17,9),(12,6)): px[p]=gw
sprites["Icon_007_Gwangdae"]=outline(im,(80,20,16,255))

# 서리버섯: 옅은 푸른 고깔 갓 + 고드름
im=new(24,24); px=im.load()
sb=(176,214,236,255); sl=(226,242,252,255); sd=(120,166,204,255); ss=(214,230,240,255)
for y in range(12,21):
    for x in range(10,14): px[x,y]= sd if x==13 else ss
for y in range(2,13):
    hw=max(1,round(9*math.sqrt((y-1)/11)))
    for x in range(12-hw,12+hw):
        px[x,y]= sl if y<5 or x<12-hw+2 else (sd if x>=12+hw-2 else sb)
for x,l in ((5,2),(8,1),(15,1),(18,2)):
    for d in range(1,l+1): px[x,12+d]=sl
sprites["Icon_008_Seori"]=outline(im,(60,96,130,255))

# 썩은 버섯 덮개: 아이콘 위에 겹치는 곰팡이 얼룩(반투명)
im=new(24,24); px=im.load()
mold=(92,110,70,170); moldD=(58,70,44,210); fuzz=(196,206,170,200)
for cx,cy,r in ((9,9,2.8),(14,13,3.2),(11,16,2.2),(15,7,2.0)):
    for y in range(24):
        for x in range(24):
            if ell(cx,cy,r,r,x,y)<=1: px[x,y]= moldD if ell(cx,cy,r,r,x,y)<0.35 else mold
for p in ((8,6),(14,10),(17,13),(11,18),(12,8),(13,15)): px[p]=fuzz
sprites["Icon_Rotten"]=im

# 변종 표시: 8x8 별
im=new(8,8); px=im.load()
star=["...##...","...##...","########",".######.","..####..",".##..##.",".#....#.","........"]
for y,row in enumerate(star):
    for x,ch in enumerate(row):
        if ch=="#": px[x,y]=(255,214,92,255)
px[3,2]=(255,246,200,255)
sprites["Icon_Variant"]=outline(im,(110,70,10,255))

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

# 전투 HUD: 조이스틱(흰색 바탕, 코드에서 색·투명도 입힘)
im=new(48,48); px=im.load()
for y in range(48):
    for x in range(48):
        e=ell(24,24,23.5,23.5,x,y)
        if e>1: continue
        if e>0.86: px[x,y]=(255,255,255,255)                 # 바깥 테
        elif e>0.78: px[x,y]=(160,160,160,255)               # 테 안쪽 그늘
        else: px[x,y]=(255,255,255,90)                        # 반투명 바닥
# 상하좌우 방향 눈금
for (x,y) in ((23,6),(24,6),(22,7),(25,7),(23,41),(24,41),(22,40),(25,40),(6,23),(6,24),(7,22),(7,25),(41,23),(41,24),(40,22),(40,25)):
    px[x,y]=(255,255,255,200)
sprites["HUD_JoystickBase"]=im

im=new(24,24); px=im.load()
for y in range(24):
    for x in range(24):
        e=ell(12,12,11,11,x,y)
        if e>1: continue
        u=(x+0.5-12)/11; v=(y+0.5-12)/11
        c=W
        if u+v>0.55 and e>0.45: c=S
        if u+v<-0.75 and e>0.35: c=(255,255,255,255)
        px[x,y]=c
sprites["HUD_JoystickKnob"]=outline(im,K)

# 공격: 칼 + 휘두른 궤적
im=new(24,24); px=im.load()
line(px,7,15,17,5,3)                                          # 칼날
px[19,3]=W; px[18,4]=W; px[19,4]=W
line(px,4,12,9,17,2)                                          # 가드
line(px,3,19,6,16,2)                                          # 손잡이
for t in range(-80,15,3):                                     # 휘두른 궤적
    r=math.radians(t)
    for rr in (15.5,16.5):
        x=round(7+rr*math.cos(r)); y=round(17+rr*math.sin(r))
        if 0<=x<24 and 0<=y<24 and px[x,y][3]==0: px[x,y]=(255,255,255,200)
sprites["HUD_Attack"]=outline(im,O)

# 대시: 앞으로 쏠린 화살표 + 속도선
im=new(24,24); px=im.load()
for y in range(6,18):
    hw=6-abs(y-11.5)
    for x in range(13,13+int(hw*1.4)+1): px[x,y]=W
rect(px,8,10,13,13)
rect(px,2,7,7,7); rect(px,4,11,6,12); rect(px,2,16,7,16)
sprites["HUD_Dash"]=outline(im,O)

# 시장 UI: 문자 격자를 팔레트로 칠한다('.'은 투명)
def grid(rows,pal):
    im=new(len(rows[0]),len(rows)); px=im.load()
    for y,row in enumerate(rows):
        for x,ch in enumerate(row):
            if ch!=".": px[x,y]=pal[ch]
    return im

# 자물쇠: 잠긴 지역 위에 올림
sprites["Icon_Lock"]=outline(grid([
"................",
".....ssssss.....",
"....ss....ss....",
"....s......s....",
"....s......s....",
"....s......s....",
"..hhhhhhhhhhhh..",
"..hbbbbbbbbbbd..",
"..bbbbbkkbbbbd..",
"..bbbbbkkbbbbd..",
"..bbbbbkkbbbbd..",
"..bbbbbbbbbbbd..",
"..bbbbbbbbbbbd..",
"..dddddddddddd..",
"................",
"................"],{"s":(186,190,200,255),"h":(255,240,170,255),"b":(242,193,78,255),"d":(204,146,44,255),"k":(92,60,22,255)}),(92,60,22,255))

# 말풍선(9-slice): 꼬리를 왼쪽 아래 모서리 칸(테두리 7x6) 안에 둬서 늘려도 꼬리는 그대로
sprites["UI_SpeechBubble"]=grid([
".KKKKKKKKKKKKKK.",
"KWWWWWWWWWWWWWWK",
"KWWWWWWWWWWWWWWK",
"KWWWWWWWWWWWWWWK",
"KWWWWWWWWWWWWWWK",
"KWWWWWWWWWWWWWWK",
"KWWWWWWWWWWWWWWK",
"KWWWWWWWWWWWWWWK",
"KWWWWWWWWWWWWWWK",
"KWWWWWWWWWWWWWWK",
"KSSSSSSSSSSSSSSK",
".KKSSSKKKKKKKKK.",
"..KSSK..........",
"..KSK...........",
"..KK............",
"................"],{"K":K,"W":W,"S":S})

# 경쟁: 교차한 두 칼
im=new(16,16); px=im.load()
bl=(230,232,240,255); bd=(150,156,170,255); hg=(242,193,78,255); hb=(139,94,60,255)
for m in (lambda x:x, lambda x:15-x):                    # 두 번째 칼은 좌우 반전
    for i in range(2,11): px[m(i),i]=bl; px[m(i+1),i]=bd
    for x,y in ((8,12),(9,11),(11,9),(12,8)): px[m(x),y]=hg  # 가드
    for i in (11,12,13): px[m(i),i]=hb; px[m(i+1),i]=hb    # 손잡이
    px[m(14),14]=hg
sprites["Icon_Compete"]=outline(im,(48,48,60,255))

# 담합: 맞물린 사슬 고리 두 개
im=new(16,16); px=im.load()
cl=(230,232,240,255); cm=(186,190,200,255); cd=(120,124,136,255)
def link(cx,cy,half):
    for y in range(16):
        for x in range(16):
            e=ell(cx,cy,4,4,x,y)
            if not 0.5<=e<=1: continue
            if half and not half(x,y): continue
            u=(x+0.5-cx)+(y+0.5-cy)
            px[x,y]= cl if u<-2 else (cd if u>2 else cm)
link(10,10,None); link(6,6,None)                          # 왼 위 고리가 위
link(10,10,lambda x,y:x<y)                                # 반대쪽 교차는 오른 아래 고리가 위 → 맞물림
sprites["Icon_Collude"]=outline(im,(48,48,60,255))

# 기다리기: 모래시계
sprites["Icon_Wait"]=outline(grid([
"................",
"..wwwwwwwwwwww..",
"..dddddddddddd..",
"...gggggggggg...",
"...gssssssssg...",
"....gssssssg....",
".....gssssg.....",
"......gssg......",
"......gsgg......",
".....ggsggg.....",
"....gggsgggg....",
"...gggssssggg...",
"...gssssssssg...",
"..wwwwwwwwwwww..",
"..dddddddddddd..",
"................"],{"w":(176,120,72,255),"d":(124,80,44,255),"g":(214,236,246,255),"s":(232,196,120,255)}),(70,48,30,255))

for n,i in sprites.items(): i.save(f"{OUT}/{n}.png")

# 미리보기: 6배, 틀류는 색 입혀서
tint={"UI_Panel":(44,59,48),"UI_Button":(217,87,59),"UI_Button_Pressed":(217,87,59),"UI_Slot":(63,82,68),"UI_GaugeFrame":(243,233,210),"UI_GaugeFill":(123,201,80),"UI_SpeechBubble":(243,233,210)}
for k in ("Tab_Gear","Tab_Hunt","Tab_Breed","Tab_Dispatch","HUD_JoystickBase","HUD_JoystickKnob","HUD_Attack","HUD_Dash"): tint[k]=(243,233,210)
names=list(sprites); sc=6; cell=24*sc+40
rows=(len(sprites)+6)//7
pv=Image.new("RGBA",(cell*7,cell*rows+10),(30,42,34,255))
for i,n in enumerate(names):
    im=sprites[n]
    if n in tint:
        t=tint[n]; im=im.copy(); p=im.load()
        for y in range(im.size[1]):
            for x in range(im.size[0]):
                r,g_,b_,a=p[x,y]; p[x,y]=(r*t[0]//255,g_*t[1]//255,b_*t[2]//255,a)
    s_=sc if max(im.size)<=24 else 24*sc//max(im.size)  # 큰 스프라이트는 칸에 맞게 축소
    big=im.resize((im.size[0]*s_,im.size[1]*s_),Image.NEAREST)
    pv.alpha_composite(big,((i%7)*cell+20,(i//7)*cell+20))
pv.save(sys.argv[2])
print(names)
