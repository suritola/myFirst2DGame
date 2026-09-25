# 새 캐릭터 특수 능력 아이콘 (ID 20~51) → Assets/Resources/Icons/ability_<id>.png  (32x32)
# 캐릭터 색 둥근 판 위에 능력마다 다른 문양
# 실행: python tools/pixelart/make_ability_icons.py
import math, os, re, sys, uuid
sys.path.insert(0, os.path.dirname(__file__))
from png import Img, write
from make_fx import disc, ring, line, ROOT

OUT = os.path.join(ROOT, 'Assets', 'Resources', 'Icons')
W = (240, 240, 248, 255); K = (25, 20, 30, 255); G = (175, 180, 195, 255); Y = (240, 200, 80, 255)
R = (220, 60, 60, 255); B = (120, 80, 50, 255); P = (170, 110, 230, 255); C = (120, 230, 255, 255)
GR = (110, 230, 110, 255); O = (255, 150, 50, 255); S = (150, 150, 165, 255)


def sword(img, x0=9, y0=23, x1=23, y1=9, blade=W):
    line(img, x0, y0, x1, y1, blade, 2.2)
    line(img, x0 - 2, y0 - 2 + 0, x0 + 2, y0 + 2, Y, 1.6)       # 손잡이 가드
    line(img, x0 - 1, y0 + 1, x0 - 4, y0 + 4, B, 1.8)


def arrow(img, x0, y0, x1, y1, col=W):
    line(img, x0, y0, x1, y1, B, 1.2)
    ang = math.atan2(y1 - y0, x1 - x0)
    for s in (-0.6, 0.6):
        line(img, x1, y1, x1 - math.cos(ang + s) * 4, y1 - math.sin(ang + s) * 4, col, 1.3)
    for s in (-0.8, 0.8):
        line(img, x0, y0, x0 - math.cos(ang + s) * 3, y0 - math.sin(ang + s) * 3, W, 1)


def star(img, cx, cy, r, col, n=4, rot=0.3):
    for k in range(n):
        a = rot + k * 2 * math.pi / n
        line(img, cx, cy, cx + math.cos(a) * r, cy + math.sin(a) * r, col, 2.2)
    disc(img, cx, cy, 2, K)


def flask(img, liquid):
    img.rect(14, 7, 4, 5, W)
    for y in range(12, 26):
        w = min(9, 2 + (y - 12))
        for x in range(16 - w, 16 + w):
            img.blend(x, y, liquid if y > 16 else W)
    for x in range(8, 24):
        img.blend(x, 26, K)


def glyph(img, gid):
    g = gid
    if g == 'greatsword': sword(img, 8, 25, 24, 7); line(img, 12, 21, 21, 12, G, 3.5)
    elif g == 'twin': sword(img, 8, 24, 20, 8); sword(img, 24, 24, 12, 8)
    elif g == 'spear': line(img, 6, 26, 24, 8, B, 1.6); line(img, 21, 11, 26, 6, W, 3); disc(img, 20, 12, 1.6, R)
    elif g == 'dashslash': sword(img, 12, 22, 25, 9); [line(img, 4, 12 + i * 4, 12, 12 + i * 4, C, 1) for i in range(3)]
    elif g == 'whirl': ring(img, 16, 16, 10, 2, W); ring(img, 16, 16, 6, 1.5, C, gaps=3); disc(img, 16, 16, 2, Y)
    elif g == 'wave':
        for x in range(32):
            for y in range(32):
                d1 = math.hypot(x - 10, y - 16); d2 = math.hypot(x - 6, y - 16)
                if d1 < 13 and d2 > 12: img.blend(x, y, C)
    elif g == 'shield':
        for y in range(7, 26):
            w = 9 if y < 18 else 9 - (y - 18)
            for x in range(16 - w, 16 + w): img.blend(x, y, G)
        line(img, 16, 9, 16, 22, Y, 2); line(img, 11, 14, 21, 14, Y, 2)
    elif g == 'bloodblade': sword(img, 9, 23, 23, 9, R); disc(img, 22, 22, 3, R); disc(img, 22, 20, 1.5, R)
    elif g == 'knifefan':
        for k in range(5):
            a = math.radians(-60 + k * 30)
            line(img, 16, 26, 16 + math.sin(a) * 14, 26 - math.cos(a) * 14, W, 1.6)
    elif g == 'blazestar': star(img, 16, 16, 10, G); disc(img, 22, 10, 3.5, O); disc(img, 22, 9, 1.8, Y)
    elif g == 'chakram': ring(img, 16, 16, 9, 3, C); ring(img, 16, 16, 9, 1, W, gaps=6); disc(img, 16, 16, 3, K)
    elif g == 'shadowstep':
        disc(img, 10, 18, 5, (80, 60, 120, 200)); disc(img, 22, 13, 5, P); line(img, 12, 17, 20, 14, W, 1)
    elif g == 'smoke':
        for x, y, r in [(12, 18, 6), (19, 16, 6), (16, 12, 5), (21, 21, 4)]: disc(img, x, y, r, (140, 130, 160, 255))
    elif g == 'assassin': line(img, 8, 24, 22, 10, W, 2); ring(img, 20, 12, 6, 1.5, R); disc(img, 20, 12, 1.5, R)
    elif g == 'crit': star(img, 16, 16, 11, Y, 8, 0.2); disc(img, 16, 16, 3, R)
    elif g == 'afterimage':
        for i, a in enumerate([90, 160, 255]): disc(img, 10 + i * 5, 16, 5, (170, 120, 230, a))
    elif g == 'longbow':
        for y in range(5, 28): img.blend(int(9 + 7 * math.sin((y - 5) / 23 * math.pi)), y, B)
        line(img, 9, 5, 9, 27, W, 1); arrow(img, 6, 16, 27, 16)
    elif g == 'repeater':
        for i in range(3): arrow(img, 5, 10 + i * 6, 25, 10 + i * 6)
    elif g == 'blastarrow': arrow(img, 5, 25, 20, 10); disc(img, 23, 8, 5, O); disc(img, 23, 8, 2.5, Y)
    elif g == 'backstep': arrow(img, 26, 16, 10, 16); line(img, 6, 22, 12, 22, C, 1); line(img, 6, 25, 14, 25, C, 1)
    elif g == 'trap':
        ring(img, 16, 18, 8, 2, S)
        for k in range(8):
            a = k * math.pi / 4
            line(img, 16 + math.cos(a) * 8, 18 + math.sin(a) * 8, 16 + math.cos(a) * 5, 18 + math.sin(a) * 5, W, 1)
    elif g == 'triplearrow':
        for i in range(3): arrow(img, 4 + i * 3, 26 - i * 3, 22 + i * 3, 8 - i * 0 + 0 * i)
    elif g == 'eye':
        for x in range(5, 28):
            h = int(6 * math.sin((x - 5) / 22 * math.pi))
            img.blend(x, 16 - h, W); img.blend(x, 16 + h, W)
        disc(img, 16, 16, 4, GR); disc(img, 16, 16, 1.8, K)
    elif g == 'wind':
        for i in range(3):
            for x in range(6, 26): img.blend(x, int(10 + i * 6 + 2 * math.sin(x * 0.5 + i)), C)
    elif g == 'fireflask': flask(img, O); disc(img, 16, 20, 2, Y)
    elif g == 'iceflask': flask(img, C); star(img, 16, 20, 3, W, 6, 0)
    elif g == 'shockflask': flask(img, (255, 235, 90, 255)); line(img, 13, 16, 18, 20, K, 1); line(img, 18, 20, 14, 24, K, 1)
    elif g == 'heal': flask(img, R); line(img, 16, 16, 16, 24, W, 2); line(img, 12, 20, 20, 20, W, 2)
    elif g == 'transmute': disc(img, 16, 16, 9, Y); disc(img, 16, 16, 6, (255, 225, 120, 255)); ring(img, 16, 16, 9, 1, K); line(img, 13, 16, 19, 16, K, 1)
    elif g == 'acidrain':
        for x, y in [(9, 8), (16, 12), (23, 7), (12, 19), (21, 18)]:
            disc(img, x, y + 2, 2.3, GR); img.set(int(x), int(y - 1), GR)
        for x in range(5, 27): img.blend(x, 27, GR)
    elif g == 'catalyst':
        for x, y, r in [(12, 20, 4), (19, 15, 3), (15, 10, 2.5), (22, 22, 2)]: ring(img, x, y, r, 1.3, GR)
    elif g == 'goldtouch':
        disc(img, 12, 18, 6, Y); ring(img, 12, 18, 6, 1, K); disc(img, 21, 12, 5, Y); ring(img, 21, 12, 5, 1, K)


ICONS = {
    20: ('greatsword', 'sw'), 21: ('twin', 'sw'), 22: ('spear', 'sw'), 23: ('dashslash', 'sw'), 24: ('whirl', 'sw'), 25: ('wave', 'sw'), 26: ('shield', 'sw'), 27: ('bloodblade', 'sw'),
    28: ('knifefan', 'rg'), 29: ('blazestar', 'rg'), 30: ('chakram', 'rg'), 31: ('shadowstep', 'rg'), 32: ('smoke', 'rg'), 33: ('assassin', 'rg'), 34: ('crit', 'rg'), 35: ('afterimage', 'rg'),
    36: ('longbow', 'ar'), 37: ('repeater', 'ar'), 38: ('blastarrow', 'ar'), 39: ('backstep', 'ar'), 40: ('trap', 'ar'), 41: ('triplearrow', 'ar'), 42: ('eye', 'ar'), 43: ('wind', 'ar'),
    44: ('fireflask', 'al'), 45: ('iceflask', 'al'), 46: ('shockflask', 'al'), 47: ('heal', 'al'), 48: ('transmute', 'al'), 49: ('acidrain', 'al'), 50: ('catalyst', 'al'), 51: ('goldtouch', 'al'),
}
BG = {'sw': ((40, 60, 120), (80, 110, 190)), 'rg': ((45, 30, 70), (95, 65, 140)), 'ar': ((35, 70, 35), (75, 130, 65)), 'al': ((60, 30, 85), (120, 70, 170))}


def meta(path):
    tpl = open(os.path.join(ROOT, 'Assets', 'Sprites', 'Items', 'hp_potion.png.meta'), encoding='utf-8').read()
    tpl = re.sub(r'guid: [0-9a-f]{32}', 'guid: ' + uuid.uuid4().hex, tpl, count=1)
    tpl = re.sub(r'spritePixelsToUnits: \d+', 'spritePixelsToUnits: 32', tpl)
    open(path, 'w', encoding='utf-8', newline='\n').write(tpl)


def main():
    os.makedirs(OUT, exist_ok=True)
    for aid, (g, who) in ICONS.items():
        img = Img(32, 32)
        dark, light = BG[who]
        disc(img, 16, 16, 15, dark + (255,))
        disc(img, 14, 13, 11, light + (90,))
        ring(img, 16, 16, 15, 1.5, (20, 15, 25, 255))
        glyph(img, g)
        out = os.path.join(OUT, 'ability_%d.png' % aid)
        write(img, out)
        if not os.path.exists(out + '.meta'):
            meta(out + '.meta')
    print('wrote', len(ICONS), 'icons')


if __name__ == '__main__':
    main()
