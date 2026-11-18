# 전투·시장 HUD 추가 스프라이트. 사용: python tools/ui/make_hud_icons.py <출력 폴더> <미리보기.png>
# 기존 HUD처럼 흰 바탕(코드에서 색 입힘) + 어두운 외곽선.
# 밤 시계: Fill(흰 원, Image Filled Radial360으로 남은 밤만큼 줄임) 위에 Frame(테·눈금·달)을 겹친다
import math, os, sys
from PIL import Image
OUT = sys.argv[1]; os.makedirs(OUT, exist_ok=True)
T = (0, 0, 0, 0); W = (255, 255, 255, 255); S = (186, 186, 186, 255); O = (30, 30, 30, 255)

def new(n): return Image.new("RGBA", (n, n), T)
def d(x, y, c): return math.hypot(x + 0.5 - c, y + 0.5 - c)

# 밤 시계 테(48): 외곽선 - 흰 테 3px - 안쪽 선, 가운데는 비워 Fill이 보이게
frame = new(48); px = frame.load(); C = 24
for y in range(48):
    for x in range(48):
        r = d(x, y, C)
        if r > 23.5: continue
        if r > 22.5 or 18.5 < r <= 19.5: px[x, y] = O
        elif r > 19.5: px[x, y] = S if y > 30 else W            # 아래쪽 그늘
for k in range(12):                                             # 눈금: 3·6·9·12시는 길게
    a = math.radians(k * 30); L = 4 if k % 3 == 0 else 2
    for t in range(L):
        rr = 17.5 - t
        px[round(C - 0.5 + rr * math.sin(a)), round(C - 0.5 - rr * math.cos(a))] = W
for y in range(20, 28):                                         # 가운데 축
    for x in range(20, 28):
        r = d(x, y, C)
        if r <= 2.5: px[x, y] = W
        elif r <= 3.5: px[x, y] = O
for y in range(0, 12):                                          # 12시 위 초승달(테에 걸침)
    for x in range(18, 30):
        a, b = math.hypot(x + 0.5 - 24, y + 0.5 - 5), math.hypot(x + 0.5 - 26.5, y + 0.5 - 3.5)
        if a <= 4.5 and b > 3.5: px[x, y] = (255, 236, 150, 255)
        elif a <= 5.5 and (b > 2.5 or a > 4.5) and px[x, y][3] == 0: px[x, y] = O

# 밤 시계 채움(48): 테 안쪽 구멍보다 1px 큰 흰 원
fill = new(48); fp = fill.load()
for y in range(48):
    for x in range(48):
        if d(x, y, C) <= 19.5: fp[x, y] = W

# 토벌 종료(24): 깃대 + 펄럭이는 깃발(버섯 무늬)
flag = new(24); fl = flag.load()
for y in range(3, 22): fl[5, y] = W; fl[6, y] = S                 # 깃대
for x in range(4, 9): fl[x, 21] = W                               # 받침
fl[5, 2] = W; fl[6, 2] = W
for x in range(7, 21):                                            # 물결 깃발
    off = round(1.5 * math.sin((x - 7) / 13 * math.pi * 1.5))
    for y in range(4 + off, 14 + off): fl[x, y] = S if y >= 12 + off else W
for y, x0, x1 in ((6, 12, 14), (7, 11, 15), (8, 10, 16), (9, 12, 14), (10, 12, 14)):  # 버섯 무늬(어두운 칠, 갓 2줄 + 대)
    for x in range(x0, x1 + 1): fl[x, y + round(1.5 * math.sin((x - 7) / 13 * math.pi * 1.5))] = O
def outline(img):
    p = img.load(); n = img.size[0]
    add = [(x, y) for y in range(n) for x in range(n) if not p[x, y][3] and any(
        0 <= x + a < n and 0 <= y + b < n and p[x + a, y + b][3] and p[x + a, y + b] != O for a, b in ((1, 0), (-1, 0), (0, 1), (0, -1)))]
    for q in add: p[q] = O
    return img
outline(flag)

sprites = {"HUD_NightClock_Frame": frame, "HUD_NightClock_Fill": fill, "HUD_EndHunt": flag}
for n, i in sprites.items(): i.save(f"{OUT}/{n}.png")

# 미리보기: 색 입힌 시계(남은 밤 100%·60%·15%)와 깃발
def tint(img, c):
    img = img.copy(); p = img.load()
    for y in range(img.size[1]):
        for x in range(img.size[0]):
            r, g, b, a = p[x, y]; p[x, y] = (r * c[0] // 255, g * c[1] // 255, b * c[2] // 255, a)
    return img
def radial(img, amt):  # Unity Radial360(위에서 시계 방향으로 비움) 흉내
    img = img.copy(); p = img.load()
    for y in range(48):
        for x in range(48):
            ang = (math.degrees(math.atan2(x + 0.5 - C, -(y + 0.5 - C))) + 360) % 360
            if ang < 360 * (1 - amt): p[x, y] = T
    return img
sc = 5; pv = Image.new("RGBA", (4 * (48 * sc + 20) + 20, 48 * sc + 40), (30, 42, 34, 255))
for k, amt in enumerate((1.0, 0.6, 0.15)):
    col = (70, 80, 160) if amt > 0.2 else (200, 70, 60)
    im = Image.new("RGBA", (48, 48), T); im.alpha_composite(tint(radial(fill, amt), col)); im.alpha_composite(tint(frame, (243, 233, 210)))
    pv.alpha_composite(im.resize((48 * sc, 48 * sc), Image.NEAREST), (20 + k * (48 * sc + 20), 20))
f = tint(flag, (243, 233, 210)).resize((24 * sc * 2 // 2 * 2, 24 * sc * 2 // 2 * 2), Image.NEAREST)
pv.alpha_composite(f, (20 + 3 * (48 * sc + 20), 20))
pv.save(sys.argv[2])
print(list(sprites))
