# 무한 모드 "불타는 사막" 도트 만들기
#   - 지옥 타일셋 · 소품을 모래색으로 다시 칠함 (배치 · 조각 위치는 그대로, 불꽃 · 용암의 밝은 주황은 살림)
#   - 사막 소품(선인장 · 뼈 · 화로 · 바위 등)을 새로 그림
# 결과: Assets/Resources/Desert/*.png (+ .meta)     실행: python tools/pixelart/make_desert.py
import os, re, sys, uuid, random
sys.path.insert(0, os.path.dirname(__file__))
from png import Img, read, write

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
SPR = os.path.join(ROOT, 'Assets', 'Sprites')
OUT = os.path.join(ROOT, 'Assets', 'Resources', 'Desert')

# 어두운 곳 → 밝은 곳 모래 색 띠
RAMP = [(0.00, (40, 22, 16)), (0.14, (82, 46, 28)), (0.30, (138, 84, 46)), (0.46, (186, 128, 72)),
        (0.62, (218, 168, 102)), (0.80, (240, 206, 142)), (1.00, (252, 238, 200))]


def lum(c):
    return (0.3 * c[0] + 0.59 * c[1] + 0.11 * c[2]) / 255.0


def hot(c):
    # 불꽃 · 용암 · 불티 (밝고 주황 · 노랑)
    r, g, b = c[0], c[1], c[2]
    return r > 170 and g > 70 and r - b > 110 and lum(c) > 0.42


def ramp(t):
    t = max(0.0, min(1.0, t))
    for (t0, c0), (t1, c1) in zip(RAMP, RAMP[1:]):
        if t <= t1:
            k = (t - t0) / (t1 - t0)
            return tuple(round(a + (b - a) * k) for a, b in zip(c0, c1))
    return RAMP[-1][1]


def recolor(img, lo_out=0.06, hi_out=0.9):
    # 이 그림의 명암 범위를 모래 띠 전체로 늘려서 칠함
    ls = sorted(lum(c) for row in img.px for c in row if c[3] > 0 and not hot(c))
    lo, hi = ls[int(len(ls) * 0.02)], ls[int(len(ls) * 0.98)]
    out = Img(img.w, img.h)
    for y in range(img.h):
        for x in range(img.w):
            c = img.px[y][x]
            if c[3] == 0:
                continue
            if hot(c):
                # 불꽃은 조금 더 노랗고 밝게
                out.px[y][x] = (min(255, c[0] + 10), min(255, c[1] + 30), c[2], c[3])
                continue
            t = (lum(c) - lo) / max(1e-4, hi - lo)
            r, g, b = ramp(lo_out + (hi_out - lo_out) * t)
            out.px[y][x] = (r, g, b, c[3])
    return out


def new_guid():
    return uuid.uuid4().hex


def copy_meta(src_meta, dst_meta, old, new):
    s = open(src_meta, encoding='utf-8').read()
    s = re.sub(r'guid: [0-9a-f]{32}', 'guid: ' + new_guid(), s, count=1)
    s = s.replace(old, new)
    open(dst_meta, 'w', encoding='utf-8', newline='\n').write(s)


# ------------------------------------------------------------------ 새 소품 (16 px = 1칸)
P = {
    'k': (36, 22, 16), 'g': (58, 120, 62), 'G': (96, 168, 84), 'l': (148, 206, 110), 's': (228, 214, 170),
    'S': (250, 242, 212), 'b': (166, 150, 118), 'r': (120, 78, 48), 'R': (170, 112, 64), 'o': (255, 150, 40),
    'y': (255, 222, 90), 'w': (255, 250, 220), 'd': (86, 56, 34), 'p': (224, 92, 120), 'D': (196, 150, 92),
}

CACTUS = [
    '......kkkk......',
    '.....kGGlGk.....',
    '.....kGgGlk.....',
    '.....kGgGlk.....',
    '.kk..kGgGlk.....',
    'kGlk.kGgGlk..kk.',
    'kGlk.kGgGlk.kGlk',
    'kGgk.kGgGlk.kGgk',
    'kGgGkkGgGlk.kGgk',
    '.kGgGGGgGlkkGGgk',
    '..kkGGGgGlGGgGk.',
    '....kGGgGlkkkk..',
    '.....kGgGlk.....',
    '.....kGgGlk.....',
    '.....kGgGlk.....',
    '.....kGgGlk.....',
    '....DkGgGlkD....',
    '...DDDkkkkDDD...',
]
CACTUS_SMALL = [
    '....kkk.....',
    '...kGlGk....',
    '...kGgGk.kk.',
    '.k.kGgGk.kGk',
    'kGkkGgGk.kGk',
    'kGgGGgGkkGgk',
    '.kkkGgGGGgk.',
    '...kGgGkkk..',
    '...kGgGk....',
    '..DkGgGkD...',
    '.DDDkkkDDD..',
]
SKULL = [  # 햇볕에 바랜 소 두개골
    'k..............k',
    'Sk............kS',
    'bSk..kkkkkk..kSb',
    '.bSkkSSSSSSkkSb.',
    '..kSSSSSSSSSSk..',
    '...kSkkSSkkSk...',
    '...kSkkSSkkSk...',
    '...kSSSSSSSSk...',
    '....kSSbbSSk....',
    '....kSSSSSSk....',
    '.....kSkkSk.....',
    '......kkkk......',
]
RIBS = [
    '..k.k.k.k.k.....',
    '.kSkSkSkSkSk....',
    '.kS.S.S.S.Sk....',
    'kSk.S.S.S.S.k...',
    'kS..S.S.S.S..kk.',
    'kSkkSkSkSkSkkSSk',
    '.kSSSSSSSSSSSSk.',
    '..kkkkkkkkkkkk..',
]
BRAZIER = [
    '......y.y.......',
    '.....yoy.y......',
    '....yoooyoy.....',
    '...yowwoooy.....',
    '...yowwwooy.....',
    '..kkoowwookk....',
    '..kRRRRRRRRk....',
    '...kRrrrrRk.....',
    '....kRrrRk......',
    '.....kRRk.......',
    '.....kRrk.......',
    '....kRrrRk......',
    '...kkkkkkkk.....',
]
ROCKS = [
    '.....kkkk.......',
    '...kkRRDRk......',
    '..kRDDDRRk..kk..',
    '.kRDDRRRrrkkRDk.',
    '.kRRRrrrrkRDDRk.',
    'kkrrrrrrkkRRrrk.',
    'kdddddddkdddddk.',
]
BUSH = [  # 말라 죽은 덤불
    '..k....k...k....',
    '.krk..krk.kr....',
    '..krk.kr.krk..k.',
    '.k.krkrkkr..krk.',
    'krk.krrrk..krk..',
    '.krkkrrrkkkrk...',
    '...krrrrrrk.....',
    '..DDkkkkkkDD....',
]
FLAME_PILLAR = [  # 땅에서 솟는 불기둥 (장식)
    '.....y..........',
    '....yoy..y......',
    '...yoooyyoy.....',
    '..yoowwoooy.....',
    '..yowwwwooy.....',
    '..oowwwwwoo.....',
    '...oowwwoo......',
    '...ooowooo......',
    '....ooooo.......',
    '...kddddk.......',
    '..kdrrrrdk......',
]


def props():
    names = ['cactus', 'cactus_small', 'skull', 'ribs', 'brazier', 'rocks', 'bush', 'flame_pillar']
    rows = [CACTUS, CACTUS_SMALL, SKULL, RIBS, BRAZIER, ROCKS, BUSH, FLAME_PILLAR]
    return [(n, Img.from_rows(r, P)) for n, r in zip(names, rows)]


def single_meta(dst_meta, ppu):
    tpl = open(os.path.join(SPR, 'Items', 'hp_potion.png.meta'), encoding='utf-8').read()
    tpl = re.sub(r'guid: [0-9a-f]{32}', 'guid: ' + new_guid(), tpl, count=1)
    tpl = re.sub(r'spritePixelsToUnits: \d+', 'spritePixelsToUnits: %d' % ppu, tpl)
    tpl = re.sub(r'spritePivot: \{[^}]*\}', 'spritePivot: {x: 0.5, y: 0}', tpl)
    tpl = re.sub(r'alignment: \d+', 'alignment: 7', tpl)     # 아래 가운데 기준
    open(dst_meta, 'w', encoding='utf-8', newline='\n').write(tpl)


def main():
    os.makedirs(OUT, exist_ok=True)
    for src, dst in [('tileset_hell_32', 'tileset_desert_32'), ('objects_hell', 'objects_desert')]:
        img = recolor(read(os.path.join(SPR, src + '.png')))
        out = os.path.join(OUT, dst + '.png')
        write(img, out)
        if not os.path.exists(out + '.meta'):
            copy_meta(os.path.join(SPR, src + '.png.meta'), out + '.meta', src, dst)
        print('wrote', dst)
    for name, img in props():
        out = os.path.join(OUT, 'prop_' + name + '.png')
        write(img, out)
        if not os.path.exists(out + '.meta'):
            single_meta(out + '.meta', 16)
        print('wrote prop', name, img.w, img.h)


if __name__ == '__main__':
    main()
