# 지역(맵) 아이콘 도트 스티커 생성. 사용: python tools/ui/make_map_icons.py <출력 폴더> <미리보기.png>
# 72x72(그림 64px + 테두리 여백 4px, 배치 좌표는 48 기준, Z배): 그림(나무·버섯·몬스터)마다 외곽선 → 전체 실루엣을 따라 칠한 스티커 테두리 → 바깥 외곽선
import os, sys
from PIL import Image, ImageDraw, ImageFilter
OUT = sys.argv[1]; os.makedirs(OUT, exist_ok=True)
Z = 4 / 3; S = 72; O = 4; T = (0, 0, 0, 0); K = (34, 26, 26, 255)

def rgb(h): return (int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16), 255)

class Pic:
    def __init__(self): self.im = Image.new("RGBA", (S, S), T); self.px = self.im.load()
    def put(self, x, y, c):  # 그림 좌표 → 여백 O만큼 밀어 찍음
        x, y = x + O, y + O
        if 0 <= x < S and 0 <= y < S: self.px[x, y] = c
    def ell(self, cx, cy, rx, ry, c, hi=None):  # hi: 왼쪽 위 밝은 면
        cx, cy, rx, ry = cx * Z, cy * Z, rx * Z, ry * Z
        for y in range(S):
            for x in range(S):
                dx, dy = (x + 0.5 - cx) / rx, (y + 0.5 - cy) / ry
                if dx * dx + dy * dy <= 1: self.put(x, y, hi if hi and dx + dy < -0.6 else c)
    def rect(self, x0, y0, x1, y1, c):
        x0, y0, x1, y1 = round(x0 * Z), round(y0 * Z), round((x1 + 1) * Z) - 1, round((y1 + 1) * Z) - 1
        for y in range(y0, y1 + 1):
            for x in range(x0, x1 + 1): self.put(x, y, c)
    def tri(self, cx, top, base, half, c, cR=None):  # 위가 뾰족한 삼각형, cR: 오른쪽 면
        cx, top, base, half = cx * Z, round(top * Z), round(base * Z), half * Z
        for y in range(top, base + 1):
            w = (y - top) * half / max(1, base - top)
            for x in range(int(cx - w), int(cx + w) + 1): self.put(x, y, cR if cR and x > cx else c)
    def outline(self):  # 지금까지 그린 것에 1px 외곽선(새 그림을 위에 얹기 전에 호출)
        add = [(x, y) for y in range(S) for x in range(S) if not self.px[x, y][3] and any(
            0 <= x + dx < S and 0 <= y + dy < S and self.px[x + dx, y + dy][3] and self.px[x + dx, y + dy] != K
            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)))]
        for p in add: self.px[p] = K
    def layer(self, draw):  # 겹치는 그림도 각자 외곽선이 보이게 따로 그려 얹음
        p = Pic(); draw(p); p.outline(); self.im.alpha_composite(p.im)
    def sparkle(self, x, y, c=rgb("fff6c0")):
        x, y = round(x * Z), round(y * Z)
        for d in range(-3, 4): self.put(x + d, y, c); self.put(x, y + d, c)
        for d in (-1, 1): self.put(x + d, y + d, c); self.put(x + d, y - d, c)
    def pix(self, x, y, rows, pal):  # 손으로 찍은 도트(출력 픽셀 그대로). x, y: 48 기준 왼쪽 위
        x, y = round(x * Z), round(y * Z)
        for j, row in enumerate(rows):
            assert len(row) == len(rows[0]), row
            for i, ch in enumerate(row):
                if ch != ".": self.put(x + i, y + j, pal[ch])
    def sticker(self, bg, band, line):  # 안쪽 빈틈은 배경색 → 2px 테두리 → 색 선 → 외곽선
        P = 12  # 가장자리에서 닫기 연산이 잘리지 않게 여백
        m = Image.new("L", (S + 2 * P, S + 2 * P), 0); m.paste(self.im.getchannel("A").point(lambda v: 255 if v else 0), (P, P))
        m = m.filter(ImageFilter.MaxFilter(13)).filter(ImageFilter.MinFilter(13))  # 6px 이하 오목한 틈 메우기
        ImageDraw.floodfill(m, (0, 0), 128)  # 바깥만 128 → 나머지(구멍 포함)는 안쪽
        inside = [[m.getpixel((x + P, y + P)) != 128 for x in range(S)] for y in range(S)]
        out = Image.new("RGBA", (S, S), T); op = out.load()
        for y in range(S):
            for x in range(S):
                if inside[y][x]: op[x, y] = bg; continue
                d = min((abs(x - u) + abs(y - v) for v in range(max(0, y - 4), min(S, y + 5))
                         for u in range(max(0, x - 4), min(S, x + 5)) if inside[v][u]), default=99)
                if d <= 2: op[x, y] = band
                elif d == 3: op[x, y] = line
                elif d == 4: op[x, y] = K
        out.alpha_composite(self.im); return out

def face(p, cx, cy):  # 웃는 얼굴: 눈 두 점 + 입
    p.rect(cx - 2, cy, cx - 2, cy, K); p.rect(cx + 2, cy, cx + 2, cy, K); p.rect(cx - 1, cy + 2, cx + 1, cy + 2, K)

def songi(p, x, y, s=1):  # 송이 버섯(x: 중심, y: 갓 위)
    p.layer(lambda q: q.rect(x - 2 * s, y + 3 * s, x + 1 * s, y + 10 * s, rgb("ece0c4")))
    p.layer(lambda q: q.ell(x, y + 3 * s, 5 * s, 3 * s, rgb("8b5e3c"), rgb("b8885c")))

# 버끼(토끼) 15x15, 버귄(펭귄) 15x17. o 외곽선, e 눈, h 눈 하이라이트
BUKKI = [
    "..ooo.....ooo..",
    ".owpwo...owpwo.",
    ".owpwo...owpwo.",
    ".owpwo...owpwo.",
    ".owwwoooooowwo.",
    "owwwwwwwwwwwwwo",
    "owwwwwwwwwwwwwo",
    "owwhewwwwwhewwo",
    "owweewwwwweewwo",
    "owpwwwmwmwwwpwo",
    "owwwwwwmwwwwwwo",
    ".owwwwwwwwwwso.",
    ".osswwwwwwwsso.",
    "..owwooooowwo..",
    "..oooo...oooo..",
]
PAL_BUKKI = {"o": K, "w": rgb("fff4e6"), "s": rgb("f0d8c0"), "p": rgb("f4a0b0"), "e": K, "h": rgb("ffffff"), "m": rgb("8a4a3a")}
PENGU = [
    ".....ooooo.....",
    "...oocccccoo...",
    "..occdcccdcco..",
    "..oCCCCCCCCCo..",
    "..onnnnnnnnno..",
    ".onNnnnnnnnnno.",
    ".onnwwwwwwwnno.",
    ".onwhewwwhewno.",
    ".onweewwweewno.",
    ".onpwwyyywwpno.",
    "onnwwwwywwwwnno",
    "onnwwwwwwwwwnno",
    "onnwwwwwwwwwnno",
    ".onngggggggno..",
    "..oonnnnnnoo...",
    "..oyyyo.oyyyo..",
    "..ooooo.ooooo..",
]
PAL_PENGU = {"o": K, "c": rgb("dfe8f2"), "C": rgb("b8c8da"), "d": rgb("ffffff"), "n": rgb("2e3a52"), "N": rgb("4a5c80"),
             "w": rgb("f8f8f8"), "g": rgb("dde4ee"), "y": rgb("f0a030"), "p": rgb("f4a0b0"), "e": K, "h": rgb("ffffff")}

# 001 평원: 보스 나무, 송이, 버끼, 반짝이
def field(p):
    def island(q):  # 떠 있는 풀밭 섬: 흙 단면 + 풀 윗면
        q.ell(24, 39, 21, 7, rgb("8a5a34"), rgb("a87040"))
        q.ell(24, 34, 22, 6, rgb("5a9a3c"), rgb("7cbc52"))
    p.layer(island)
    for x, y in ((10, 41), (19, 43), (30, 42), (37, 40)): p.rect(x, y, x + 1, y, rgb("6e4628"))  # 흙 결
    p.layer(lambda q: q.ell(13, 43, 2, 1.5, rgb("9a9a9a"), rgb("c0c0c0")) or q.ell(33, 44, 1.5, 1, rgb("9a9a9a")))  # 돌
    p.layer(lambda q: q.ell(31, 35, 7, 2, rgb("d8c08a")))  # 흙길
    def fence(q):
        for x in (34, 38, 42): q.rect(x, 24, x, 31, rgb("c09060"))
        q.rect(33, 26, 43, 26, rgb("d8a878")); q.rect(33, 29, 43, 29, rgb("d8a878"))
    p.layer(fence)
    p.layer(lambda q: q.ell(4, 31, 3, 2.5, rgb("2f6b34"), rgb("4a9a46")) or q.ell(45, 31, 2.5, 2.5, rgb("2f6b34"), rgb("4a9a46")))  # 덤불
    def trunk(q):  # 보스 나무 줄기(얼굴·뿌리)
        q.rect(9, 17, 16, 33, rgb("7a5432")); q.rect(13, 17, 16, 33, rgb("94683e"))
        q.rect(6, 31, 8, 33, rgb("7a5432")); q.rect(17, 31, 19, 33, rgb("94683e"))
    p.layer(trunk)
    p.rect(10, 22, 11, 23, K); p.rect(14, 22, 15, 23, K); p.rect(11, 27, 14, 27, rgb("4a3020"))
    def canopy(q):
        for cx, cy, r in ((6, 12, 5), (13, 7, 7), (20, 11, 5), (12, 15, 6), (19, 16, 4)): q.ell(cx, cy, r, r, rgb("3f8442"), rgb("5aa84e"))
    p.layer(canopy)
    for x, y in ((9, 9), (16, 5), (17, 13), (6, 15)): p.rect(x, y, x, y, rgb("78c060"))  # 잎 하이라이트
    p.layer(lambda q: q.ell(20, 6, 3, 2, rgb("8b5e3c"), rgb("b8885c")) or q.ell(4, 8, 2, 1.5, rgb("c0603a"), rgb("e08858")))  # 나무 위 버섯
    p.pix(20, 23, BUKKI, PAL_BUKKI)
    def shrooms(q):  # 송이 무리
        for cx, cy, s in ((37, 33, 1.0), (41, 34, 0.8), (34, 36, 0.6)):
            q.rect(cx - s, cy, cx + s, cy + 4 * s, rgb("ece0c4")); q.ell(cx, cy, 3 * s, 2 * s, rgb("8b5e3c"), rgb("b8885c"))
    p.layer(shrooms)
    for x, y, c in ((7, 36, "f6e070"), (16, 37, "f0a0c0"), (20, 35, "ffffff"), (44, 36, "f6e070"), (29, 38, "f0a0c0")): p.rect(x, y, x, y, rgb(c))  # 꽃
    p.layer(lambda q: q.ell(35, 9, 5, 3, rgb("ffffff")) or q.ell(40, 8, 4, 3.5, rgb("ffffff")) or q.ell(43, 10, 3, 2, rgb("ffffff")))  # 구름
    p.sparkle(29, 15); p.sparkle(41, 19)

# 002 얼음: 설산, 버귄, 눈송이
def ice(p):
    p.layer(lambda q: q.tri(22, 4, 30, 15, rgb("7f9bb8"), rgb("9ab4cf")))
    p.layer(lambda q: q.tri(22, 4, 10, 4, rgb("ffffff")))
    p.layer(lambda q: q.tri(10, 14, 30, 8, rgb("6f8ba8"), rgb("8aa6c2")))
    p.layer(lambda q: q.tri(10, 14, 18, 2, rgb("ffffff")))
    p.layer(lambda q: q.ell(24, 40, 21, 3.5, rgb("e6f2fb"), rgb("ffffff")))  # 눈 바닥
    for x, y in ((8, 40), (36, 41), (17, 42)): p.rect(x, y, x + 2, y, rgb("bcd4ea"))  # 눈 결
    def pines(q):  # 눈 쌓인 소나무
        for cx, top, h in ((5, 26, 13), (43, 28, 11), (38, 33, 7)):
            q.rect(cx, top + h - 1, cx, top + h + 1, rgb("6b4a2c")); q.tri(cx, top, top + h, h * 0.4, rgb("2f6b4a"), rgb("3f8a5e"))
    p.layer(pines)
    for cx, top in ((5, 26), (43, 28), (38, 33)): p.rect(cx - 1, top + 2, cx + 1, top + 2, rgb("ffffff")); p.rect(cx, top, cx, top + 1, rgb("ffffff"))
    p.layer(lambda q: [q.tri(cx, top, 37, w, rgb("9fe0f4"), rgb("cff2fb")) for cx, top, w in ((12, 30, 2), (15, 33, 1.5), (9, 34, 1.2))])  # 얼음 결정
    def snowshroom(q):  # 구름버섯 무리
        for cx, cy, s in ((20, 37, 1.0), (23, 39, 0.7)):
            q.rect(cx - s, cy, cx + s, cy + 3 * s, rgb("ece0c4")); q.ell(cx, cy, 3 * s, 2 * s, rgb("dfe8f2"), rgb("ffffff"))
    p.layer(snowshroom)
    p.pix(23, 26, PENGU, PAL_PENGU)
    p.sparkle(35, 19, rgb("ffffff")); p.sparkle(8, 22, rgb("ffffff"))


# 003 공룡맵: 화산, 공룡(버우르스), 고사리
def dino(p):
    p.layer(lambda q: q.ell(25, 41, 21, 3, rgb("c49a5c"), rgb("dab478")))  # 모래 바닥
    for x, y in ((9, 41), (40, 42), (22, 43)): p.rect(x, y, x + 2, y, rgb("a8804a"))
    p.layer(lambda q: q.tri(17, 8, 30, 14, rgb("6b4a3a"), rgb("85604a")))
    p.layer(lambda q: q.rect(14, 7, 20, 9, rgb("ff8a3a")))
    p.layer(lambda q: [q.ell(cx, cy, 2, 2, rgb("9a908a")) for cx, cy in ((18, 4), (21, 2))])
    for x0, y0, y1 in ((16, 10, 16), (19, 11, 19), (14, 11, 13)): p.rect(x0, y0, x0, y1, rgb("ff8a3a"))  # 용암 줄기
    p.layer(lambda q: [q.ell(cx, cy, r, r * 0.7, rgb("8a8078"), rgb("a8a098")) for cx, cy, r in ((44, 39, 2), (12, 42, 1.5))])  # 돌
    def egg(q):  # 공룡 알
        q.ell(15, 38, 3, 4, rgb("f4ecd4"), rgb("fffaec"))
    p.layer(egg); p.rect(14, 36, 14, 36, rgb("8fcc78")); p.rect(16, 39, 16, 39, rgb("8fcc78")); p.rect(13, 39, 13, 39, rgb("8fcc78"))
    def fern2(q):
        for i in range(4): q.rect(43 - i // 2, 38 - i * 3, 46 + i // 2, 38 - i * 3, rgb("4a9a3a"))
        q.rect(44, 28, 44, 40, rgb("2f6b2c"))
    p.layer(fern2)
    def fern(q):
        for i in range(6): q.rect(5 - i // 2, 38 - i * 3, 8 + i // 2, 39 - i * 3, rgb("3f8a3a"))
        q.rect(6, 22, 7, 42, rgb("2f6b2c"))
    p.layer(fern)
    def saur(q):  # 버우르스: 둥근 공룡 + 등 가시
        q.ell(30, 37, 10, 7, rgb("6fb05a"), rgb("8fcc78")); q.ell(38, 28, 5, 5, rgb("6fb05a"), rgb("8fcc78"))
        q.rect(24, 42, 26, 45, rgb("5a9a48")); q.rect(33, 42, 35, 45, rgb("5a9a48"))
        for x in (24, 28, 32): q.tri(x, 27, 31, 2, rgb("f0c040"))
    p.layer(saur); p.rect(37, 27, 37, 27, K); p.rect(40, 27, 40, 27, K); p.rect(38, 30, 39, 30, K)
    p.layer(lambda q: q.ell(38, 22, 4, 2, rgb("c0603a"), rgb("e08858")))  # 머리 버섯
    p.sparkle(31, 17)

icons = {}
for name, draw, bg, band, line in (("Icon_Map_001_Field", field, rgb("cdeef8"), rgb("f6eeda"), rgb("5a9a3c")),
                                   ("Icon_Map_002_Ice", ice, rgb("bcdcf2"), rgb("f2f8fd"), rgb("5a88b8")),
                                   ("Icon_Map_003_Dino", dino, rgb("f8d49a"), rgb("fbe6d2"), rgb("c0603a"))):
    p = Pic(); draw(p); icons[name] = p.sticker(bg, band, line)
for n, i in icons.items(): i.save(f"{OUT}/{n}.png")

sc = 5; pv = Image.new("RGBA", ((S * sc + 24) * len(icons) + 24, S * sc + 48), (30, 42, 34, 255))
for k, i in enumerate(icons.values()):
    pv.alpha_composite(i.resize((S * sc, S * sc), Image.NEAREST), (24 + k * (S * sc + 24), 24))
pv.save(sys.argv[2])
print(list(icons))
