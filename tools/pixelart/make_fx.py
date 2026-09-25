# 새 도트 이펙트 시트 만들기 → Assets/Resources/FX/<이름>.png (+ .meta, 가로로 이어 붙인 프레임)
#   fx_scythe_ghost  부메랑 낫 조준: 회전하는 유령 낫 (보라)
#   fx_trail_dot     조준 경로 점선: 빛나는 화살촉 (흰색 → 코드에서 색 입힘)
#   fx_target_rune   운석 낙하 지점: 회전하는 룬 고리 (흰색)
#   fx_deathburst    적 처치: 퍼지는 영혼 고리와 불티 (흰색)
#   fx_levelup       레벨업: 솟아오르는 빛기둥 (금색)
#   fx_sparkle       코인 획득: 반짝이는 별 (흰색)
# 실행: python tools/pixelart/make_fx.py
import math, os, random, re, sys, uuid
sys.path.insert(0, os.path.dirname(__file__))
from png import Img, write

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
FX = os.path.join(ROOT, 'Assets', 'Resources', 'FX')


def shade(v, a=255):
    v = max(0, min(255, int(v)))
    return (v, v, v, a)


def disc(img, cx, cy, r, c):
    for y in range(int(cy - r - 1), int(cy + r + 2)):
        for x in range(int(cx - r - 1), int(cx + r + 2)):
            if (x + 0.5 - cx) ** 2 + (y + 0.5 - cy) ** 2 <= r * r:
                img.blend(x, y, c)


def ring(img, cx, cy, r, w, c, gaps=0, rot=0.0):
    for y in range(img.h):
        for x in range(img.w):
            dx, dy = x + 0.5 - cx, y + 0.5 - cy
            d = math.hypot(dx, dy)
            if abs(d - r) <= w / 2:
                if gaps:
                    a = (math.atan2(dy, dx) - rot) % (2 * math.pi / gaps)
                    if a > 2 * math.pi / gaps * 0.72:
                        continue
                img.blend(x, y, c)


def line(img, x0, y0, x1, y1, c, w=1.0):
    n = int(max(abs(x1 - x0), abs(y1 - y0)) * 2) + 1
    for i in range(n + 1):
        t = i / n
        disc(img, x0 + (x1 - x0) * t, y0 + (y1 - y0) * t, w / 2, c)


# ------------------------------------------------------------------ 이펙트별 프레임
def scythe_ghost():
    # 기본 모양 (오른쪽 위를 향한 낫), 90도씩 돌려 4프레임
    P = {'k': (40, 16, 60), 'p': (150, 90, 230), 'P': (205, 160, 255), 'w': (245, 230, 255), 'h': (110, 80, 60), 'H': (150, 115, 80)}
    rows = [
        '........................',
        '..........kkkkk.........',
        '........kkPPPPPkk.......',
        '......kkPPwwwwPPPk......',
        '.....kPPwwkkkkwwPPk.....',
        '....kPPwkk....kkwPPk....',
        '...kPPwk........kPPk....',
        '...kPwk..........kPk....',
        '..kPPk...........kPk....',
        '..kPk............kPk....',
        '..kPk...........kHk.....',
        '..kpk..........kHk......',
        '...kk.........kHk.......',
        '.............kHk........',
        '............kHk.........',
        '...........kHk..........',
        '..........khk...........',
        '.........khk............',
        '........khk.............',
        '.......khk..............',
        '......kpk...............',
        '......kPk...............',
        '.......k................',
        '........................',
    ]
    base = Img.from_rows(rows, P)
    frames = []
    for f in range(4):
        img = Img(24, 24)
        # 뒤에 옅은 보라 잔상
        disc(img, 12, 12, 11, (150, 90, 230, 40))
        for y in range(24):
            for x in range(24):
                # f 번 90도 회전
                sx, sy = x, y
                for _ in range(f):
                    sx, sy = sy, 23 - sx
                c = base.px[sy][sx]
                if c[3]:
                    img.blend(x, y, c)
        frames.append(img)
    return frames


def trail_dot():
    frames = []
    for f in range(4):
        img = Img(8, 8)
        glow = 90 + 50 * math.sin(f / 4 * 2 * math.pi)
        disc(img, 4, 4, 3.6, shade(255, int(glow * 0.5)))
        # 오른쪽을 가리키는 화살촉
        for i in range(4):
            img.set(2 + i, 1 + i, shade(255))
            img.set(2 + i, 6 - i, shade(255))
            img.set(1 + i, 1 + i, shade(200))
            img.set(1 + i, 6 - i, shade(200))
        img.set(5, 3, shade(255)); img.set(5, 4, shade(255))
        frames.append(img)
    return frames


def target_rune():
    frames = []
    for f in range(6):
        img = Img(32, 32)
        rot = f / 6 * (2 * math.pi / 3)
        ring(img, 16, 16, 14.2, 1.6, shade(255, 230))
        ring(img, 16, 16, 11.2, 1.0, shade(200, 190), gaps=6, rot=rot)
        # 도는 세모 셋
        for k in range(3):
            a = rot + k * 2 * math.pi / 3
            tx, ty = 16 + math.cos(a) * 14.2, 16 + math.sin(a) * 14.2
            disc(img, tx, ty, 2.2, shade(255))
        # 가운데 십자와 점
        for d in range(-5, 6):
            if abs(d) > 1:
                img.blend(16 + d, 16, shade(230, 170))
                img.blend(16, 16 + d, shade(230, 170))
        disc(img, 16, 16, 1.6, shade(255))
        disc(img, 16, 16, 9, shade(255, 20 + 12 * (f % 3)))
        frames.append(img)
    return frames


def death_burst():
    frames = []
    random.seed(5)
    sparks = [(random.uniform(0, 2 * math.pi), random.uniform(0.7, 1.0)) for _ in range(10)]
    for f in range(6):
        img = Img(24, 24)
        t = (f + 1) / 6
        fade = int(255 * (1 - t * 0.85))
        if f < 2:
            disc(img, 12, 12, 3 + f * 2, shade(255, 230 - f * 60))
        ring(img, 12, 12, 2 + t * 9, 2.2 - t * 1.2, shade(255, fade))
        for a, s in sparks:
            d = (3 + t * 9) * s
            x, y = 12 + math.cos(a) * d, 12 + math.sin(a) * d
            img.blend(int(x), int(y), shade(255, fade))
            if f < 4:
                img.blend(int(x - math.cos(a)), int(y - math.sin(a)), shade(200, fade // 2))
        frames.append(img)
    return frames


def level_up():
    frames = []
    random.seed(9)
    motes = [(random.uniform(4, 28), random.uniform(0, 48), random.uniform(0.6, 1.4)) for _ in range(14)]
    for f in range(8):
        img = Img(32, 48)
        t = f / 7
        width = 10 * (1 - abs(t - 0.35) * 1.2)
        alpha = 1 - max(0.0, t - 0.5) * 2
        for y in range(48):
            k = y / 47.0
            for x in range(32):
                d = abs(x + 0.5 - 16) / max(0.5, width)
                if d < 1:
                    a = min(1.0, (1 - d) ** 0.9 * alpha * (0.6 + 0.4 * k) * 1.3)
                    core = d < 0.35
                    img.blend(x, y, (255, 250, 210, int(255 * a)) if core else (255, 214, 100, int(255 * a)))
        for mx, my, sp in motes:
            y = int((my - f * 6 * sp) % 48)
            img.blend(int(mx), y, (255, 255, 220, int(230 * alpha)))
            img.blend(int(mx), y + 1, (255, 200, 90, int(140 * alpha)))
        # 발밑 고리
        ring(img, 16, 44, 4 + t * 10, 1.4, (255, 220, 120, int(220 * alpha)))
        frames.append(img)
    return frames


def sparkle():
    frames = []
    for f in range(4):
        img = Img(12, 12)
        L = [2, 5, 4, 1][f]
        for d in range(-L, L + 1):
            a = 255 - abs(d) * (200 // max(1, L))
            img.blend(6 + d, 6, shade(255, a))
            img.blend(6, 6 + d, shade(255, a))
        if f in (1, 2):
            for d in (-2, 2):
                img.blend(6 + d, 6 + d, shade(255, 150))
                img.blend(6 + d, 6 - d, shade(255, 150))
        disc(img, 6, 6, 1.2, shade(255))
        frames.append(img)
    return frames


# ------------------------------------------------------------------ 저장 (+ 메타)
def sheet_meta(name, fw, fh, n, path):
    tpl = open(os.path.join(FX, 'fx_orb.png.meta'), encoding='utf-8').read()
    head = tpl[:tpl.index('    sprites:\n') + len('    sprites:\n')]
    tail = tpl[tpl.index('    outline: []\n    physicsShape: []\n    bones: []\n    spriteID: \n'):]
    head = re.sub(r'guid: [0-9a-f]{32}', 'guid: ' + uuid.uuid4().hex, head, count=1)
    ids = [random.randint(-2 ** 31, 2 ** 31 - 1) for _ in range(n)]
    sprites = ''
    for i in range(n):
        sprites += ('    - serializedVersion: 2\n      name: %s_%d\n      rect:\n        serializedVersion: 2\n'
                    '        x: %d\n        y: 0\n        width: %d\n        height: %d\n      alignment: 0\n'
                    '      pivot: {x: 0.5, y: 0.5}\n      border: {x: 0, y: 0, z: 0, w: 0}\n      outline: []\n'
                    '      physicsShape: []\n      tessellationDetail: 0\n      bones: []\n      spriteID: %s\n'
                    '      internalID: %d\n      vertices: []\n      indices: \n      edges: []\n      weights: []\n'
                    % (name, i, i * fw, fw, fh, uuid.uuid4().hex, ids[i]))
    table = '    nameFileIdTable:\n' + ''.join('      %s_%d: %d\n' % (name, i, ids[i]) for i in range(n))
    tail = re.sub(r'    nameFileIdTable:\n(      .*\n)+', table, tail)
    open(path, 'w', encoding='utf-8', newline='\n').write(head + sprites + tail)


def save(name, frames):
    fw, fh = frames[0].w, frames[0].h
    sheet = Img(fw * len(frames), fh)
    for i, fr in enumerate(frames):
        sheet.paste(fr, i * fw, 0)
    out = os.path.join(FX, name + '.png')
    write(sheet, out)
    if not os.path.exists(out + '.meta'):
        sheet_meta(name, fw, fh, len(frames), out + '.meta')
    print('wrote', name, len(frames), 'frames', fw, 'x', fh)


if __name__ == '__main__':
    random.seed(1)
    save('fx_scythe_ghost', scythe_ghost())
    save('fx_trail_dot', trail_dot())
    save('fx_target_rune', target_rune())
    save('fx_deathburst', death_burst())
    save('fx_levelup', level_up())
    save('fx_sparkle', sparkle())
