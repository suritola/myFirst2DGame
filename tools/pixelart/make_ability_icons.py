# 새 캐릭터 특수 능력 아이콘 (ID 20~59) · 캐릭터 전용 레벨업 카드 아이콘 (ID 60~91, 거너 86~91)
#   → Assets/Resources/Icons/ability_<id>.png  (32x32)
# 캐릭터 색 둥근 판 위에 능력마다 다른 문양
# 실행: python tools/pixelart/make_ability_icons.py
import math, os, re, sys, uuid
sys.path.insert(0, os.path.dirname(__file__))
from png import Img, write
from make_fx import disc, ring, line, ROOT

OUT = os.path.join(ROOT, 'Assets', 'Resources', 'Icons')
W = (240, 240, 248, 255); K = (25, 20, 30, 255); G = (175, 180, 195, 255); Y = (240, 200, 80, 255)
R = (220, 60, 60, 255); B = (120, 80, 50, 255); P = (170, 110, 230, 255); C = (120, 230, 255, 255)
GR = (110, 230, 110, 255); O = (255, 150, 50, 255); S = (150, 150, 165, 255); T = (215, 195, 140, 255)


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


def card(img, x, y, col=W, mark=R):
    img.rect(x, y, 8, 11, col)
    for i in range(8): img.set(x + i, y, K); img.set(x + i, y + 10, K)
    for j in range(11): img.set(x, y + j, K); img.set(x + 7, y + j, K)
    disc(img, x + 4, y + 5.5, 1.8, mark)


def bird(img, cx, cy):
    disc(img, cx, cy, 3.5, B); disc(img, cx + 4, cy - 2, 2.2, B)
    img.set(int(cx + 6), int(cy - 2), Y)
    line(img, cx - 1, cy - 1, cx - 7, cy - 8, (160, 120, 80, 255), 2)
    line(img, cx + 1, cy - 1, cx + 4, cy - 9, (160, 120, 80, 255), 1.6)


def glyph(img, g):
    # ---------------- 검사
    if g == 'hammer':
        line(img, 9, 25, 20, 12, B, 1.8)
        line(img, 16, 8, 25, 17, G, 5.5); line(img, 16, 8, 25, 17, W, 1.2)
        line(img, 5, 27, 12, 27, Y, 1)
    elif g == 'whip':
        pts = [(6, 24), (10, 18), (15, 17), (19, 21), (23, 18), (26, 10)]
        for (a, b), (c, d) in zip(pts, pts[1:]): line(img, a, b, c, d, C, 1.6)
        line(img, 4, 26, 7, 23, B, 2); disc(img, 26, 10, 1.8, Y)
    elif g == 'lance':
        line(img, 5, 27, 24, 8, B, 1.6)
        for k in range(6): line(img, 18 + k, 14 - k, 27, 5, W, 1.4 - k * 0.1)
        line(img, 6, 20, 12, 26, Y, 2)
    elif g == 'warcry':
        disc(img, 12, 16, 5, T); disc(img, 12, 14, 1, K); line(img, 11, 18, 14, 18, K, 1)
        for r in (7, 10, 13):
            for i in range(24):
                ang = -0.9 + i * 0.075
                img.blend(int(14 + math.cos(ang) * r), int(16 + math.sin(ang) * r), O)
    elif g == 'judgment':
        line(img, 16, 3, 16, 22, W, 3); line(img, 11, 7, 21, 7, Y, 2); disc(img, 16, 3, 1.5, Y)
        for x in range(8, 25): img.blend(x, 26, O)
        line(img, 10, 26, 7, 21, O, 1); line(img, 22, 26, 25, 21, O, 1)
    elif g == 'earthsplit':
        pts = [(5, 26), (10, 22), (13, 24), (17, 17), (21, 18), (26, 9)]
        for (a, b), (c, d) in zip(pts, pts[1:]): line(img, a, b, c, d, K, 2.2)
        for (a, b), (c, d) in zip(pts, pts[1:]): line(img, a, b - 1, c, d - 1, O, 0.8)
    elif g == 'unyielding':
        for y in range(8, 26):
            w = 8 if y < 18 else 8 - (y - 18)
            for x in range(16 - w, 16 + w): img.blend(x, y, G)
        line(img, 12, 10, 15, 16, K, 1); line(img, 15, 16, 13, 21, K, 1)
        disc(img, 21, 21, 3.5, R)
    elif g == 'fervor':
        for i, c in enumerate([O, Y, R]):
            line(img, 8 + i * 6, 26, 11 + i * 6, 8 + i * 2, c, 2)
        disc(img, 24, 8, 2.5, Y)
    # ---------------- 도적
    elif g == 'blowgun':
        line(img, 5, 24, 20, 12, B, 2.4); line(img, 21, 11, 27, 6, GR, 1.2)
        for x, y in [(22, 16), (25, 12)]: disc(img, x, y, 1.3, GR)
    elif g == 'cards':
        card(img, 7, 10, W, R); card(img, 12, 8, W, K); card(img, 17, 11, (255, 235, 150, 255), R)
    elif g == 'wire':
        pts = [(7, 9), (25, 12), (13, 25)]
        for i in range(3):
            a, b = pts[i]; c, d = pts[(i + 1) % 3]
            line(img, a, b, c, d, (230, 220, 255, 255), 1)
        for x, y in pts: disc(img, x, y, 2, P)
    elif g == 'deathmark':
        disc(img, 16, 14, 7, W); disc(img, 13, 13, 1.8, K); disc(img, 19, 13, 1.8, K)
        img.rect(13, 20, 7, 4, W); line(img, 16, 18, 16, 19, K, 1)
        ring(img, 16, 16, 12, 1.2, R)
    elif g == 'caltrops':
        for cx, cy in [(10, 12), (21, 11), (15, 21), (24, 22)]:
            for a in (0, 120, 240):
                r = math.radians(a - 90)
                line(img, cx, cy, cx + math.cos(r) * 4, cy + math.sin(r) * 4, G, 1.2)
            disc(img, cx, cy, 1.2, K)
    elif g == 'pickpocket':
        disc(img, 14, 19, 7, (130, 95, 60, 255)); line(img, 10, 13, 18, 13, Y, 1.5)
        disc(img, 23, 10, 3.2, Y); ring(img, 23, 10, 3.2, 1, K)
    elif g == 'evasion':
        for i, a in enumerate([70, 140, 255]): disc(img, 9 + i * 6, 16, 4.5, (190, 150, 240, a))
        line(img, 6, 8, 26, 24, W, 1)
    elif g == 'spree':
        for i in range(3): line(img, 6 + i * 3, 24 - i * 7, 14 + i * 3, 24 - i * 7, C, 1.2)
        star(img, 21, 15, 7, R, 4, 0.8)
    # ---------------- 궁수
    elif g == 'netbow':
        for k in range(6):
            a = k * math.pi / 3
            line(img, 16, 16, 16 + math.cos(a) * 11, 16 + math.sin(a) * 11, T, 1)
        for r in (4, 8, 11): ring(img, 16, 16, r, 1, T)
    elif g == 'javelin':
        line(img, 4, 27, 23, 8, B, 1.6); line(img, 21, 10, 27, 4, W, 2.6)
        for i in range(3): line(img, 5 + i * 4, 12 + i * 2, 9 + i * 4, 12 + i * 2, C, 1)
    elif g == 'burstbow':
        for i in range(3): arrow(img, 5 + i * 5, 24 - i * 2, 17 + i * 5, 12 - i * 2)
    elif g == 'falcon':
        bird(img, 15, 19)
        line(img, 22, 26, 27, 21, R, 1); line(img, 24, 27, 28, 23, R, 1)
    elif g == 'gale':
        arrow(img, 5, 16, 27, 16, C)
        for i in range(2):
            for x in range(6, 22): img.blend(x, int(10 + i * 12 + 1.5 * math.sin(x * 0.6)), C)
    elif g == 'focus':
        ring(img, 16, 16, 10, 1.4, W); ring(img, 16, 16, 5, 1.2, GR)
        line(img, 16, 3, 16, 9, W, 1); line(img, 16, 23, 16, 29, W, 1)
        line(img, 3, 16, 9, 16, W, 1); line(img, 23, 16, 29, 16, W, 1)
        disc(img, 16, 16, 1.5, R)
    elif g == 'trophy':
        disc(img, 16, 18, 7, T)
        line(img, 10, 13, 6, 5, T, 1.6); line(img, 22, 13, 26, 5, T, 1.6)
        disc(img, 13, 17, 1.4, K); disc(img, 19, 17, 1.4, K)
        disc(img, 24, 24, 3.2, R)
    elif g == 'distance':
        disc(img, 7, 22, 3, W); disc(img, 25, 10, 3, R)
        for x in range(10, 23, 3): img.blend(x, int(22 - (x - 7) * 12 / 18), Y)
    # ---------------- 연금술사
    elif g == 'quicksilver':
        disc(img, 12, 20, 5, (205, 210, 225, 255)); disc(img, 10, 18, 1.5, W)
        for x, y in [(19, 12), (24, 7)]: disc(img, x, y, 2, (205, 210, 225, 255))
        line(img, 16, 16, 26, 6, S, 0.7)
    elif g == 'magnet':
        for y in range(32):
            for x in range(32):
                d = math.hypot(x + 0.5 - 16, y + 0.5 - 14)
                if 5 <= d <= 10 and y >= 14: img.blend(x, y, R)
        img.rect(6, 6, 5, 8, R); img.rect(21, 6, 5, 8, R)
        img.rect(6, 6, 5, 3, W); img.rect(21, 6, 5, 3, W)
    elif g == 'firework':
        line(img, 10, 27, 15, 14, O, 1)
        for k in range(8):
            a = k * math.pi / 4
            line(img, 18, 11, 18 + math.cos(a) * 8, 11 + math.sin(a) * 8, [R, Y, GR, C][k % 4], 1.2)
        disc(img, 18, 11, 1.6, W)
    elif g == 'stone':
        pts = [(16, 5), (25, 14), (16, 27), (7, 14)]
        for y in range(5, 28):
            for x in range(7, 26):
                if abs(x - 16) / 9 + abs(y - 15) / 11 <= 1: img.blend(x, y, (220, 60, 110, 255))
        line(img, 12, 10, 16, 20, (255, 170, 200, 255), 1)
    elif g == 'rewind':
        ring(img, 16, 16, 10, 2, C)
        line(img, 16, 16, 16, 9, W, 1.5); line(img, 16, 16, 21, 18, W, 1.5)
        line(img, 5, 8, 7, 14, C, 1.6); line(img, 7, 14, 12, 12, C, 1.6)
    elif g == 'giant':
        flask(img, GR)
        line(img, 16, 18, 16, 24, W, 1.6); line(img, 13, 21, 16, 18, W, 1.6); line(img, 19, 21, 16, 18, W, 1.6)
    elif g == 'cycle':
        for k in range(3):
            a = k * 2 * math.pi / 3
            x0, y0 = 16 + math.cos(a) * 9, 16 + math.sin(a) * 9
            x1, y1 = 16 + math.cos(a + 1.6) * 9, 16 + math.sin(a + 1.6) * 9
            line(img, x0, y0, x1, y1, GR, 1.8)
            disc(img, x1, y1, 2, W)
    elif g == 'volatile':
        flask(img, (190, 90, 240, 255))
        star(img, 22, 9, 5, Y, 6, 0.1)
    # ---------------- 캐릭터 전용 레벨업 카드 (60~75)
    elif g == 'c_wave':
        for x in range(32):
            for y in range(32):
                d1 = math.hypot(x - 12, y - 16); d2 = math.hypot(x - 8, y - 16)
                if d1 < 12 and d2 > 11: img.blend(x, y, C)
        sword(img, 6, 24, 16, 12)
    elif g == 'c_vamp': sword(img, 9, 23, 23, 9, R); disc(img, 22, 22, 3, R)
    elif g == 'c_parry':
        sword(img, 8, 24, 22, 10); disc(img, 22, 21, 3, O); line(img, 18, 26, 26, 17, W, 1)
    elif g == 'c_storm':
        ring(img, 16, 16, 10, 2, W, gaps=4); ring(img, 16, 16, 6, 1.5, C, gaps=3); disc(img, 16, 16, 2, Y)
    elif g == 'c_ricochet':
        star(img, 9, 22, 5, G); line(img, 12, 19, 18, 9, S, 1); line(img, 18, 9, 25, 18, S, 1); star(img, 25, 19, 4, G)
    elif g == 'c_barb': star(img, 14, 14, 9, G); disc(img, 22, 23, 3, R); disc(img, 22, 21, 1.5, R)
    elif g == 'c_clone':
        disc(img, 11, 16, 6, (90, 60, 130, 180)); disc(img, 21, 16, 6, P); star(img, 21, 16, 4, W)
    elif g == 'c_starstorm':
        for k in range(6):
            a = k * math.pi / 3
            star(img, 16 + math.cos(a) * 9, 16 + math.sin(a) * 9, 3, G, 4, a)
    elif g == 'c_split':
        arrow(img, 4, 16, 16, 16)
        for dy in (-7, 0, 7): arrow(img, 17, 16, 27, 16 + dy)
    elif g == 'c_echo':
        arrow(img, 4, 20, 24, 20); arrow(img, 8, 12, 28, 12, (140, 210, 255, 255))
    elif g == 'c_windstep':
        arrow(img, 12, 16, 27, 16)
        for i in range(3): line(img, 4, 10 + i * 6, 10, 10 + i * 6, C, 1)
    elif g == 'c_thorn':
        for x in range(5, 28, 4): line(img, x, 26, x + 2, 16 + (x % 3) * 2, GR, 1.4)
        for x in range(5, 28, 4): img.set(x + 2, 15 + (x % 3) * 2, W)
    elif g == 'c_chain':
        disc(img, 11, 20, 5, O); disc(img, 11, 20, 2.5, Y); disc(img, 22, 11, 4, O); disc(img, 22, 11, 2, Y)
        line(img, 14, 17, 19, 13, W, 1)
    elif g == 'c_freeze': flask(img, C); star(img, 16, 20, 4, W, 6, 0); ring(img, 16, 16, 13, 1, W)
    elif g == 'c_homunculus':
        disc(img, 16, 18, 7, GR); disc(img, 13, 16, 1.5, K); disc(img, 19, 16, 1.5, K)
        line(img, 16, 11, 16, 6, GR, 1); disc(img, 16, 5, 1.6, Y)
    elif g == 'c_momentum':
        star(img, 12, 12, 7, G)
        img.rect(7, 22, 18, 4, K)
        for x in range(8, 22): 
            for y in range(23, 25): img.set(x, y, C)
        line(img, 20, 7, 26, 13, Y, 1.4); line(img, 26, 13, 24, 7, Y, 1.4)
    elif g == 'oath':
        sword(img, 9, 24, 23, 8); disc(img, 22, 22, 4.5, R); line(img, 20, 22, 24, 22, W, 1.2); line(img, 22, 20, 22, 24, W, 1.2)
    elif g == 'plate':
        for y in range(7, 26):
            w = 9 if y < 17 else 9 - (y - 17)
            for x in range(16 - w, 16 + w): img.blend(x, y, G)
        line(img, 16, 8, 16, 24, S, 1.5); line(img, 9, 13, 23, 13, S, 1.2)
    elif g == 'veil':
        for i, a in enumerate([60, 120, 200]): disc(img, 16, 16, 11 - i * 3, (120, 80, 180, a))
        disc(img, 13, 15, 1.5, (255, 230, 120, 255)); disc(img, 19, 15, 1.5, (255, 230, 120, 255))
    elif g == 'fugitive':
        for i in range(3): line(img, 5, 10 + i * 5, 12, 10 + i * 5, C, 1)
        disc(img, 20, 10, 3, P); line(img, 20, 13, 18, 20, P, 2); line(img, 18, 20, 14, 26, P, 1.6); line(img, 18, 20, 23, 25, P, 1.6)
    elif g == 'weakspot':
        ring(img, 16, 16, 11, 1.4, W); ring(img, 16, 16, 6, 1.4, R); disc(img, 16, 16, 2.4, R)
        line(img, 16, 2, 16, 8, W, 1); line(img, 16, 24, 16, 30, W, 1)
    elif g == 'instinct':
        arrow(img, 5, 26, 25, 6, Y); arrow(img, 5, 19, 20, 4, (255, 240, 170, 255))
        disc(img, 24, 23, 3.5, GR)
    elif g == 'emergency':
        flask(img, R); line(img, 16, 17, 16, 24, W, 2); line(img, 12, 20, 20, 20, W, 2)
        star(img, 24, 8, 4, Y, 4, 0.8)
    elif g == 'goldconvert':
        disc(img, 12, 19, 6, Y); ring(img, 12, 19, 6, 1, K); disc(img, 12, 19, 2, (255, 240, 170, 255))
        star(img, 22, 10, 5, Y, 8, 0.2)
    elif g == 'c_stance':
        sword(img, 16, 26, 16, 5); ring(img, 16, 16, 12, 1.4, C, gaps=4)
    elif g == 'c_combo':
        for i in range(3): line(img, 6 + i * 6, 25, 14 + i * 6, 7, W if i < 2 else Y, 1.8)
    elif g == 'c_vital':
        star(img, 13, 13, 8, G); disc(img, 22, 22, 4, R); ring(img, 22, 22, 6, 1, R)
    elif g == 'c_recall':
        star(img, 12, 16, 6, G); ring(img, 16, 16, 12, 1.4, C, gaps=1, rot=0.5)
        line(img, 25, 9, 27, 14, C, 1.4); line(img, 25, 9, 20, 9, C, 1.4)
    elif g == 'c_steady':
        ring(img, 16, 16, 9, 1.4, W); disc(img, 16, 16, 1.6, R)
        for x0, y0, x1, y1 in [(16, 3, 16, 9), (16, 23, 16, 29), (3, 16, 9, 16), (23, 16, 29, 16)]: line(img, x0, y0, x1, y1, GR, 1.4)
    elif g == 'c_mark':
        disc(img, 16, 18, 7, (150, 110, 80, 255)); ring(img, 16, 18, 10, 1.4, R); line(img, 16, 5, 16, 11, R, 1.4)
    elif g == 'c_fusion':
        disc(img, 11, 16, 5, O); disc(img, 21, 16, 5, C); disc(img, 16, 10, 4.5, (235, 235, 245, 230))
    elif g == 'c_sticky':
        for x in range(6, 27):
            for y in range(19, 27):
                if ((x - 16) / 10) ** 2 + ((y - 23) / 4) ** 2 < 1: img.blend(x, y, GR)
        for x in (10, 16, 22): line(img, x, 13, x, 20, GR, 1.4)
    elif g == 'c_supply':
        img.rect(8, 11, 16, 13, B); line(img, 8, 17, 24, 17, (80, 55, 35, 255), 1)
        line(img, 16, 12, 16, 23, R, 2); line(img, 11, 17, 21, 17, R, 2)
    elif g == 'g_ricochet':
        disc(img, 9, 22, 2.4, Y); line(img, 9, 22, 16, 12, W, 1.4); disc(img, 16, 12, 3, R)
        line(img, 16, 12, 25, 20, Y, 1.6); disc(img, 25, 20, 2.2, Y)
    elif g == 'g_explosive':
        star(img, 16, 16, 11, O, 8, 0.2); disc(img, 16, 16, 5, Y); disc(img, 16, 16, 2.2, W)
    elif g == 'g_incendiary':
        line(img, 6, 24, 16, 14, Y, 2); disc(img, 20, 11, 5, O); disc(img, 21, 9, 3, R); disc(img, 19, 13, 2, Y)
    elif g == 'g_homing':
        for i in range(8):
            t = i / 7
            img.blend(int(6 + t * 18), int(24 - math.sin(t * math.pi) * 12 + t * 2), P)
        ring(img, 24, 20, 4, 1.2, R); disc(img, 24, 20, 1.4, R)
    elif g == 'g_shock':
        line(img, 18, 5, 12, 16, C, 2); line(img, 12, 16, 20, 16, C, 2); line(img, 20, 16, 13, 27, C, 2)
        disc(img, 7, 9, 1.6, W); disc(img, 25, 23, 1.6, W)
    elif g == 'g_reloadwave':
        img.rect(13, 9, 6, 13, T); img.rect(13, 9, 6, 3, Y)
        ring(img, 16, 16, 12, 1.4, O, gaps=3)
    elif g == 'c_shrapnel':
        flask(img, GR)
        for x, y in [(5, 8), (26, 7), (6, 24), (26, 24)]: disc(img, x, y, 1.6, GR)


ICONS = {
    20: ('hammer', 'sw'), 21: ('whip', 'sw'), 22: ('lance', 'sw'), 23: ('warcry', 'sw'), 24: ('judgment', 'sw'), 25: ('earthsplit', 'sw'), 26: ('unyielding', 'sw'), 27: ('fervor', 'sw'),
    28: ('blowgun', 'rg'), 29: ('cards', 'rg'), 30: ('wire', 'rg'), 31: ('deathmark', 'rg'), 32: ('caltrops', 'rg'), 33: ('pickpocket', 'rg'), 34: ('evasion', 'rg'), 35: ('spree', 'rg'),
    36: ('netbow', 'ar'), 37: ('javelin', 'ar'), 38: ('burstbow', 'ar'), 39: ('falcon', 'ar'), 40: ('gale', 'ar'), 41: ('focus', 'ar'), 42: ('trophy', 'ar'), 43: ('distance', 'ar'),
    44: ('quicksilver', 'al'), 45: ('magnet', 'al'), 46: ('firework', 'al'), 47: ('stone', 'al'), 48: ('rewind', 'al'), 49: ('giant', 'al'), 50: ('cycle', 'al'), 51: ('volatile', 'al'),
    60: ('c_wave', 'sw'), 61: ('c_vamp', 'sw'), 62: ('c_parry', 'sw'), 63: ('c_storm', 'sw'),
    64: ('c_ricochet', 'rg'), 65: ('c_barb', 'rg'), 66: ('c_clone', 'rg'), 67: ('c_starstorm', 'rg'),
    68: ('c_split', 'ar'), 69: ('c_echo', 'ar'), 70: ('c_windstep', 'ar'), 71: ('c_thorn', 'ar'),
    72: ('c_chain', 'al'), 73: ('c_freeze', 'al'), 74: ('c_homunculus', 'al'), 75: ('c_shrapnel', 'al'), 76: ('c_momentum', 'rg'),
    52: ('oath', 'sw'), 53: ('plate', 'sw'), 54: ('veil', 'rg'), 55: ('fugitive', 'rg'), 56: ('weakspot', 'ar'), 57: ('instinct', 'ar'), 58: ('emergency', 'al'), 59: ('goldconvert', 'al'),
    77: ('c_stance', 'sw'), 78: ('c_combo', 'sw'), 79: ('c_vital', 'rg'), 80: ('c_recall', 'rg'), 81: ('c_steady', 'ar'), 82: ('c_mark', 'ar'), 83: ('c_fusion', 'al'), 84: ('c_sticky', 'al'), 85: ('c_supply', 'gn'),
    86: ('g_ricochet', 'gn'), 87: ('g_explosive', 'gn'), 88: ('g_incendiary', 'gn'), 89: ('g_homing', 'gn'), 90: ('g_shock', 'gn'), 91: ('g_reloadwave', 'gn'),
}
BG = {'gn': ((70, 55, 35), (140, 110, 70)), 'sw': ((40, 60, 120), (80, 110, 190)), 'rg': ((45, 30, 70), (95, 65, 140)), 'ar': ((35, 70, 35), (75, 130, 65)), 'al': ((60, 30, 85), (120, 70, 170))}


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
