# 캐릭터 고유 레벨업 스킬 아이콘 (ID 120~169) · 스킬 진화 아이콘 (ID 170~206) · 메인 메뉴 「스킬 진화」
#   → Assets/Resources/Icons/ability_<id>.png , menu_evolution.png  (32x32)
# 고유 스킬: 캐릭터 색 둥근 판 위에 문양 (make_ability_icons 와 같은 모양)
# 진화: 보라 · 금빛 판 + 금빛 테두리 + 오른쪽 위 별, 문양은 그 진화의 대표 스킬 문양을 금빛으로
# 실행: python tools/pixelart/make_skill_icons.py
import math, os, re, sys, uuid
sys.path.insert(0, os.path.dirname(__file__))
from png import Img, write
from make_fx import disc, ring, line, ROOT
from make_ability_icons import sword, arrow, star, flask, BG, meta, W, K, G, Y, R, B, P, C, GR, O, S, T

OUT = os.path.join(ROOT, 'Assets', 'Resources', 'Icons')
GOLD = (255, 215, 110, 255)
ICE = (170, 230, 255, 255)
GREY = (150, 145, 165, 255)


def flame(img, cx, cy, s=1.0):
    disc(img, cx, cy + 2 * s, 4 * s, O); disc(img, cx, cy - 1 * s, 3 * s, (255, 120, 40, 255)); disc(img, cx, cy + 2 * s, 2 * s, Y)


def shuriken(img, cx, cy, r=5, col=G):
    star(img, cx, cy, r, col, 4, 0.8)


def drop(img, cx, cy, col=R):
    disc(img, cx, cy + 2, 3.5, col); line(img, cx, cy - 4, cx, cy + 1, col, 2)


def plus(img, cx, cy, col=GR):
    line(img, cx - 3, cy, cx + 3, cy, col, 2); line(img, cx, cy - 3, cx, cy + 3, col, 2)


def glyph(img, g):
    # ---------------- 거너
    if g == 'g.mine':
        for x, y in [(9, 20), (16, 23), (23, 19)]: img.rect(x - 1, y - 3, 3, 6, Y)
        star(img, 16, 11, 7, O, 8, 0.2); disc(img, 16, 11, 2.5, W)
    elif g == 'g.shrapnel':
        disc(img, 16, 16, 4, K)
        for k in range(8):
            a = k * math.pi / 4
            line(img, 16 + math.cos(a) * 6, 16 + math.sin(a) * 6, 16 + math.cos(a) * 12, 16 + math.sin(a) * 12, Y, 1.4)
    elif g == 'g.heat':
        line(img, 5, 22, 22, 13, S, 3.2); line(img, 5, 22, 22, 13, W, 1)
        flame(img, 24, 10, 1.1)
    elif g == 'g.soul':
        ring(img, 16, 16, 11, 1.4, C); line(img, 9, 20, 21, 12, ICE, 3); disc(img, 22, 11, 2.4, W)
    elif g == 'g.flash':
        star(img, 16, 16, 12, W, 8, 0.0); disc(img, 16, 16, 4, Y); ring(img, 16, 16, 13, 1, Y)
    elif g == 'g.counter':
        disc(img, 12, 18, 4, R)
        for i in range(3): line(img, 16, 18 - i * 4, 27, 12 - i * 4, Y, 1.2)
        line(img, 5, 9, 11, 13, W, 1.6)
    elif g == 'g.quick':
        for i in range(3): img.rect(6 + i * 7, 13, 5, 6, Y); img.rect(10 + i * 7, 14, 2, 4, GOLD)
        for i in range(3): line(img, 5, 24 + i * 0, 27, 24, C, 1)
    elif g == 'g.runreload':
        for i in range(3): line(img, 4, 10 + i * 5, 11, 10 + i * 5, C, 1)
        img.rect(15, 9, 6, 13, T); img.rect(15, 9, 6, 3, Y); line(img, 22, 22, 27, 26, W, 1.6)
    elif g == 'g.scavenge':
        img.rect(10, 9, 6, 14, T); img.rect(10, 9, 6, 3, Y); plus(img, 22, 20)
    elif g == 'g.threat':
        disc(img, 16, 14, 7, W); disc(img, 13, 13, 1.8, K); disc(img, 19, 13, 1.8, K); img.rect(13, 19, 7, 3, W)
        ring(img, 16, 16, 13, 1.4, R, gaps=4)
    # ---------------- 검사
    elif g == 's.riposte':
        sword(img, 8, 24, 22, 10); ring(img, 16, 16, 12, 1.4, Y, gaps=1, rot=2.2); line(img, 25, 22, 27, 17, Y, 1.4)
    elif g == 's.trail':
        for i, a in enumerate([70, 140, 255]): sword(img, 6 + i * 4, 24, 18 + i * 4, 10, (200, 225, 255, a))
    elif g == 's.bloodguard':
        for y in range(7, 26):
            w = 9 if y < 17 else 9 - (y - 17)
            for x in range(16 - w, 16 + w): img.blend(x, y, R)
        drop(img, 16, 14, (255, 200, 200, 255))
    elif g == 's.whirl':
        for i in range(24):
            a = i * 0.45; r = 2 + i * 0.45
            img.blend(int(16 + math.cos(a) * r), int(16 + math.sin(a) * r), C)
        sword(img, 10, 22, 22, 10)
    elif g == 's.will':
        ring(img, 16, 16, 12, 1.4, G)
        for k in range(4):
            a = k * math.pi / 2 + 0.785
            line(img, 16 + math.cos(a) * 11, 16 + math.sin(a) * 11, 16 + math.cos(a) * 5, 16 + math.sin(a) * 5, Y, 1.4)
        disc(img, 16, 16, 3, W)
    elif g == 's.dance':
        sword(img, 12, 24, 25, 9)
        for i in range(3): line(img, 3, 12 + i * 5, 10, 12 + i * 5, C, 1)
    elif g == 's.crack':
        pts = [(16, 4), (13, 11), (18, 16), (12, 22), (15, 28)]
        for (a, b), (c, d) in zip(pts, pts[1:]): line(img, a, b, c, d, O, 2)
        line(img, 18, 16, 25, 14, O, 1.4); line(img, 13, 11, 6, 9, O, 1.4)
    elif g == 's.giant':
        disc(img, 20, 10, 4, S); img.rect(15, 14, 10, 12, S); sword(img, 5, 26, 14, 15, W)
    elif g == 's.echo':
        ring(img, 16, 16, 6, 1.6, W); ring(img, 16, 16, 11, 1.2, (200, 225, 255, 160)); disc(img, 16, 16, 2, C)
    elif g == 's.soul':
        sword(img, 8, 24, 22, 10)
        for x, y in [(23, 21), (26, 16), (20, 25)]: disc(img, x, y, 2, C)
    # ---------------- 도적
    elif g == 'r.smoke':
        for x, y, r in [(11, 18, 6), (18, 15, 7), (22, 21, 5)]: disc(img, x, y, r, GREY)
        disc(img, 15, 15, 2, (200, 195, 215, 255))
    elif g == 'r.feast':
        drop(img, 13, 14); plus(img, 22, 20)
    elif g == 'r.burst':
        star(img, 16, 16, 11, R, 8, 0.2); disc(img, 16, 16, 4, (255, 120, 120, 255))
    elif g == 'r.ambush':
        shuriken(img, 13, 18, 7); line(img, 24, 6, 24, 15, Y, 2.2); disc(img, 24, 19, 1.4, Y)
    elif g == 'r.boomerang':
        shuriken(img, 11, 12, 5); ring(img, 16, 16, 11, 1.4, C, gaps=1, rot=3.6); line(img, 7, 24, 11, 27, C, 1.4)
    elif g == 'r.path':
        for i in range(4): line(img, 4 + i * 6, 25 - i * 5, 8 + i * 6, 23 - i * 5, P, 1.6)
        shuriken(img, 24, 9, 4)
    elif g == 'r.practice':
        ring(img, 16, 16, 11, 1.4, W); ring(img, 16, 16, 6, 1.4, R); shuriken(img, 16, 16, 4)
    elif g == 'r.overflow':
        for k in range(6):
            a = k * math.pi / 3
            shuriken(img, 16 + math.cos(a) * 10, 16 + math.sin(a) * 10, 3)
        disc(img, 16, 16, 3, Y)
    elif g == 'r.knot':
        pts = [(8, 10), (24, 12), (15, 24)]
        for i in range(3):
            a, b = pts[i]; c, d = pts[(i + 1) % 3]
            line(img, a, b, c, d, P, 1.4)
        for x, y in pts: disc(img, x, y, 3, (90, 60, 130, 255)); disc(img, x, y, 1.4, W)
    elif g == 'r.rain':
        for x, y in [(8, 8), (16, 13), (24, 7), (12, 22), (22, 22)]:
            shuriken(img, x, y, 3); line(img, x - 2, y - 6, x, y - 3, S, 1)
    # ---------------- 궁수
    elif g == 'a.wind':
        arrow(img, 5, 20, 26, 12, C)
        for i in range(2):
            for x in range(4, 26): img.blend(x, int(25 + i * 3 + 1.5 * math.sin(x * 0.5)), C)
    elif g == 'a.breath':
        ring(img, 16, 16, 11, 1.2, GR); arrow(img, 6, 16, 26, 16)
        for i in range(3): disc(img, 10 + i * 6, 25, 1.4, Y)
    elif g == 'a.seed':
        disc(img, 16, 24, 3, B)
        for a in (-0.6, 0, 0.6): line(img, 16, 23, 16 + math.sin(a) * 10, 23 - math.cos(a) * 12, GR, 1.6)
        for x, y in [(10, 13), (22, 13), (16, 10)]: img.set(x, y, W)
    elif g == 'a.ring':
        arrow(img, 4, 26, 16, 14); ring(img, 21, 11, 4, 1, Y); ring(img, 21, 11, 8, 1, (255, 230, 150, 150))
    elif g == 'a.spread':
        ring(img, 12, 16, 6, 1.4, R); disc(img, 12, 16, 1.6, R)
        for x, y in [(24, 9), (25, 22)]: ring(img, x, y, 3.2, 1, R)
        line(img, 17, 14, 21, 11, W, 1); line(img, 17, 18, 22, 21, W, 1)
    elif g == 'a.pierce':
        arrow(img, 3, 16, 29, 16)
        for x in (10, 16, 22): disc(img, x, 16, 2.6, R)
    elif g == 'a.retreat':
        arrow(img, 14, 12, 28, 12); disc(img, 9, 20, 3, P); line(img, 9, 23, 9, 27, P, 1.6)
        for i in range(2): line(img, 2, 18 + i * 4, 5, 18 + i * 4, C, 1)
    elif g == 'a.ambush':
        for k in range(5):
            a = math.pi + k * math.pi / 4
            line(img, 16, 18, 16 + math.cos(a) * 10, 18 + math.sin(a) * 10, S, 1.4)
        line(img, 5, 18, 27, 18, T, 2); disc(img, 16, 18, 2, K)
    elif g == 'a.high':
        for y in range(16, 28):
            w = (y - 14)
            for x in range(16 - w, 16 + w): img.blend(x, y, (110, 120, 100, 255))
        for x in (8, 16, 24): line(img, x, 3, x, 11, W, 1.2); img.set(x, 12, Y)
    elif g == 'a.meteor':
        for i in range(6): line(img, 4 + i, 4 + i * 2, 14 + i, 14, (255, 200, 120, 120 + i * 20), 1)
        disc(img, 19, 18, 6, O); disc(img, 18, 17, 3, Y)
    # ---------------- 연금술사
    elif g == 'l.ignite':
        for x in range(5, 28):
            for y in range(20, 27):
                if ((x - 16) / 11) ** 2 + ((y - 23) / 3.5) ** 2 < 1: img.blend(x, y, GR)
        flame(img, 16, 12, 1.4)
    elif g == 'l.frost':
        for k in range(6):
            a = k * math.pi / 3
            line(img, 16, 16, 16 + math.cos(a) * 11, 16 + math.sin(a) * 11, ICE, 1.6)
        disc(img, 16, 16, 3, W)
    elif g == 'l.catalyst':
        disc(img, 16, 9, 4.5, O); disc(img, 9, 22, 4.5, C); disc(img, 23, 22, 4.5, GR)
        line(img, 16, 9, 9, 22, W, 0.8); line(img, 9, 22, 23, 22, W, 0.8); line(img, 23, 22, 16, 9, W, 0.8)
    elif g == 'l.amplify':
        flask(img, P); line(img, 25, 14, 25, 5, Y, 1.8); line(img, 22, 8, 25, 5, Y, 1.6); line(img, 28, 8, 25, 5, Y, 1.6)
    elif g == 'l.tinker':
        disc(img, 13, 18, 6, GR); disc(img, 11, 16, 1.3, K); disc(img, 15, 16, 1.3, K)
        line(img, 21, 8, 27, 14, S, 2); ring(img, 21, 8, 2.5, 1.2, S)
    elif g == 'l.glassrain':
        for x, y in [(8, 8), (18, 6), (13, 17), (24, 15), (9, 26), (21, 25)]:
            img.rect(x - 1, y - 2, 3, 4, ICE); img.set(x, y - 3, W)
    elif g == 'l.salve':
        for x in range(5, 28):
            for y in range(20, 27):
                if ((x - 16) / 11) ** 2 + ((y - 23) / 3.5) ** 2 < 1: img.blend(x, y, GR)
        plus(img, 16, 11, (255, 120, 140, 255))
    elif g == 'l.refine':
        flask(img, P); plus(img, 24, 8, (120, 255, 140, 255))
    elif g == 'l.embers':
        for i, (x, y) in enumerate([(7, 25), (13, 20), (19, 15), (25, 10)]):
            disc(img, x, y, 2.4, (120, 90, 60, 255)); flame(img, x, y - 4, 0.5)
    elif g == 'l.concentrate':
        flask(img, O); ring(img, 16, 18, 13, 1.4, Y, gaps=3, rot=0.3); star(img, 24, 7, 4, W, 4, 0.8)
    # ---------------- 공통 진화
    elif g == 'ce.gold':
        disc(img, 13, 18, 7, Y); ring(img, 13, 18, 7, 1, K); disc(img, 13, 18, 2.4, (255, 240, 170, 255))
        ring(img, 16, 16, 13, 1.2, GOLD, gaps=3)
    elif g == 'ce.body':
        disc(img, 12, 14, 5, R); disc(img, 20, 14, 5, R)
        for y in range(14, 26):
            w = max(0, 10 - (y - 14))
            for x in range(16 - w, 16 + w): img.blend(x, y, R)
        ring(img, 16, 17, 13, 1.2, (255, 160, 170, 255), gaps=2)


SIG = {
    'gn': ['g.mine', 'g.shrapnel', 'g.heat', 'g.soul', 'g.flash', 'g.counter', 'g.quick', 'g.runreload', 'g.scavenge', 'g.threat'],
    'sw': ['s.riposte', 's.trail', 's.bloodguard', 's.whirl', 's.will', 's.dance', 's.crack', 's.giant', 's.echo', 's.soul'],
    'rg': ['r.smoke', 'r.feast', 'r.burst', 'r.ambush', 'r.boomerang', 'r.path', 'r.practice', 'r.overflow', 'r.knot', 'r.rain'],
    'ar': ['a.wind', 'a.breath', 'a.seed', 'a.ring', 'a.spread', 'a.pierce', 'a.retreat', 'a.ambush', 'a.high', 'a.meteor'],
    'al': ['l.ignite', 'l.frost', 'l.catalyst', 'l.amplify', 'l.tinker', 'l.glassrain', 'l.salve', 'l.refine', 'l.embers', 'l.concentrate'],
}
ORDER = ['gn', 'sw', 'rg', 'ar', 'al']
# 진화 아이콘의 대표 문양 (LevelShop.Signature 의 CharEvos 순서 · 170 ~ 204), 공통 205 · 206
EVO = [
    'g.flash', 'g.heat', 'g.shrapnel', 'g.soul', 'g.counter', 'g.quick', 'g.threat',
    's.riposte', 's.trail', 's.bloodguard', 's.whirl', 's.dance', 's.crack', 's.soul',
    'r.smoke', 'r.burst', 'r.knot', 'r.boomerang', 'r.path', 'r.practice', 'r.overflow',
    'a.wind', 'a.breath', 'a.seed', 'a.ring', 'a.spread', 'a.pierce', 'a.meteor',
    'l.ignite', 'l.frost', 'l.catalyst', 'l.amplify', 'l.tinker', 'l.glassrain', 'l.concentrate',
    'ce.gold', 'ce.body',
]


def save(img, path):
    if not os.path.exists(path + '.meta'):
        meta(path + '.meta')
    write(img, path)


def base(img, dark, light):
    disc(img, 16, 16, 15, dark + (255,))
    disc(img, 14, 13, 11, light + (90,))


def main():
    n = 0
    for ci, who in enumerate(ORDER):
        for k, g in enumerate(SIG[who]):
            img = Img(32, 32)
            dark, light = BG[who]
            base(img, dark, light)
            ring(img, 16, 16, 15, 1.5, (20, 15, 25, 255))
            glyph(img, g)
            save(img, os.path.join(OUT, 'ability_%d.png' % (120 + ci * 10 + k)))
            n += 1
    for i, g in enumerate(EVO):
        img = Img(32, 32)
        base(img, (70, 35, 95), (190, 140, 230))
        glyph(img, g)
        ring(img, 16, 16, 15, 1.6, GOLD)
        ring(img, 16, 16, 13.4, 0.8, (120, 70, 160, 255))
        star(img, 26, 6, 4, GOLD, 4, 0.0)
        disc(img, 26, 6, 1.4, W)
        save(img, os.path.join(OUT, 'ability_%d.png' % (170 + i)))
        n += 1
    # 메인 메뉴 「스킬 진화」: 두 조각이 하나로 합쳐지는 모양 (배경 없음, 윤곽선)
    from make_menu_icons import outline
    img = Img(32, 32)
    disc(img, 9, 11, 5, (120, 200, 255, 255)); disc(img, 23, 11, 5, (255, 170, 90, 255))
    line(img, 11, 15, 15, 21, W, 1.6); line(img, 21, 15, 17, 21, W, 1.6)
    star(img, 16, 24, 6, GOLD, 8, 0.2); disc(img, 16, 24, 2.2, W)
    outline(img)
    save(img, os.path.join(OUT, 'menu_evolution.png'))
    print('wrote', n + 1, 'icons')


if __name__ == '__main__':
    main()
