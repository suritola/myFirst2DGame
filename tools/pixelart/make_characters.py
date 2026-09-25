# 캐릭터 도트: 몸(대기 2 + 달리기 4 프레임) · 손에 드는 기본 무기 · 공격/스킬 이펙트
#   Assets/Resources/Characters/<id>.png   (32x32 프레임 6장 가로, 8px = 1칸, 거너와 같은 크기)
#   Assets/Resources/Weapons/weapon_<name>.png
#   Assets/Resources/FX/fx_<name>.png
# 실행: python tools/pixelart/make_characters.py
import math, os, random, re, sys, uuid
sys.path.insert(0, os.path.dirname(__file__))
from png import Img, write
from make_fx import disc, ring, line, sheet_meta, FX, ROOT
from make_fx2 import save_sheet, weapon_meta, WEAPONS

CHARS = os.path.join(ROOT, 'Assets', 'Resources', 'Characters')

COMMON = {'k': (20, 17, 23), 's': (232, 195, 158), 'S': (185, 138, 106), 'w': (240, 240, 245)}

# ------------------------------------------------------------------ 몸 (머리 · 몸통 13줄 + 다리 4줄)
BODIES = {
    'swordsman': ({
        'G': (175, 180, 195), 'g': (105, 110, 125), 'r': (205, 50, 50), 'b': (55, 85, 165), 'y': (235, 195, 75),
        'c': (150, 35, 40), 'm': (130, 135, 150), 'L': (90, 95, 110), 'B': (95, 62, 40)}, [
        'rrr',
        'rrrr',
        'kkkkkk',
        'kGGGGGGk',
        'kGwGGGGk',
        'kkkkkkkk',
        'kGssssGk',
        'kgsSSsgk',
        'ckbbbbbbkc',
        'ckbbbyybbbkc',
        'ckbbbyybbbkc',
        'cksbbbbbbskc',
        'cckmmmmmmkcc',
    ]),
    'rogue': ({
        'h': (60, 45, 82), 'H': (95, 72, 120), 'm': (30, 28, 36), 'e': (255, 220, 90), 'r': (175, 40, 52),
        'l': (98, 72, 52), 'd': (160, 160, 172), 'L': (52, 46, 62), 'B': (40, 35, 46)}, [
        '',
        'kkkk',
        'kHhhhk',
        'kHhhhhhk',
        'khmmmmhk',
        'khmemehk',
        'khmmmmhk',
        'krrrrrrrrk',
        'khrllllrhk',
        'khhllddllhhk',
        'khhllllllhhk',
        'khsllllllshk',
        'kkLLLLLLkk',
    ]),
    'archer': ({
        'n': (62, 122, 62), 'N': (105, 165, 85), 't': (122, 86, 52), 'T': (152, 112, 66), 'q': (112, 72, 40),
        'f': (235, 235, 235), 'y': (225, 190, 80), 'L': (72, 92, 60), 'B': (82, 56, 36)}, [
        'f',
        'kkkkkf',
        'kNnnnnk',
        'kNnnnnnnk',
        'knssssnk',
        'knsksknk',
        'kkssSskk',
        'kqtttttttk',
        'kqqtTTTTttk',
        'kqqtyyyyttk',
        'kqstttttttsk',
        'kqtttttttk',
        'kkLLLLLLkk',
    ]),
    'alchemist': ({
        'p': (110, 60, 150), 'P': (150, 96, 192), 'o': (95, 225, 235), 'O': (205, 162, 72), 'a': (225, 225, 230),
        'v': (100, 232, 122), 'V': (232, 92, 92), 'L': (80, 50, 110), 'B': (60, 45, 50)}, [
        'a.a..a',
        'aaaaaaa',
        'kaaaaaaak',
        'kOoOOoOk',
        'kssssssk',
        'ksksskskk'[:8],
        'kkssSskk',
        'kpPppppppk',
        'kpPvVvvPppk',
        'kpPppppppPpk',
        'kspPppppPpsk',
        'kpPppppppPpk',
        'kkpPppppPpkk',
    ]),
    # 아직 만들지 않은 캐릭터 자리 (카드에서는 검은 실루엣으로만 보임)
    'mystery': ({'h': (70, 70, 80), 'H': (100, 100, 110), 'L': (60, 60, 70), 'B': (50, 50, 60)}, [
        '',
        'kkkk',
        'kHhhhk',
        'kHhhhhhk',
        'khhhhhhk',
        'khhhhhhk',
        'khhhhhhk',
        'khhhhhhhhk',
        'khhhhhhhhhhk',
        'khhhhhhhhhhk',
        'khhhhhhhhhhk',
        'khhhhhhhhhhk',
        'kkhhhhhhhhkk',
    ]),
}

LEGS = {
    'idle': ['kLLkkLLk', 'kLLkkLLk', 'kBBkkBBk', 'kBBBkkBBBk'],
    'run1': ['kLLkkLLk', 'kLLk..kLLk', 'kBBk....kBBk', 'kkk......kkk'],
    'run2': ['kLLkkLLk', 'kLLkkLLk', 'kBBkkBBk', 'kBBBkkBBBk'],
    'run3': ['kLLkkLLk', 'kLkkLLk', 'kBBkBBk.', 'kkkBBBk.'],
}


def center(row, width=16):
    row = row.rstrip('.')
    pad = (width - len(row)) // 2
    return '.' * pad + row + '.' * (width - len(row) - pad)


def body_frame(pal, upper, legs, bob=0):
    img = Img(32, 32)
    rows = [center(r) for r in upper] + [center(r) for r in LEGS[legs]]
    top = 32 - len(rows)
    for y, row in enumerate(rows):
        yy = top + y + (bob if y < len(upper) else 0)
        for x, ch in enumerate(row):
            c = pal.get(ch) or COMMON.get(ch)
            if c:
                img.set(8 + x, yy, c)
    return img


def meta_frames(name, n, path):
    # 32x32 프레임 n장, 8px = 1칸, 가운데 기준 (거너와 같게)
    tpl = open(os.path.join(FX, 'fx_orb.png.meta'), encoding='utf-8').read()
    tpl = re.sub(r'spritePixelsToUnits: \d+', 'spritePixelsToUnits: 8', tpl)
    open(path + '.tmp', 'w', encoding='utf-8', newline='\n').write(tpl)
    # sheet_meta 는 fx_orb 메타를 틀로 쓰므로 한 번 쓴 뒤 PPU 만 바꿈
    sheet_meta(name, 32, 32, n, path)
    s = open(path, encoding='utf-8').read()
    s = re.sub(r'spritePixelsToUnits: \d+', 'spritePixelsToUnits: 8', s)
    open(path, 'w', encoding='utf-8', newline='\n').write(s)
    os.remove(path + '.tmp')


def bodies():
    os.makedirs(CHARS, exist_ok=True)
    for name, (pal, upper) in BODIES.items():
        frames = [body_frame(pal, upper, 'idle'), body_frame(pal, upper, 'idle', 1),
                  body_frame(pal, upper, 'run1'), body_frame(pal, upper, 'run2', -1),
                  body_frame(pal, upper, 'run3'), body_frame(pal, upper, 'run2', -1)]
        sheet = Img(32 * len(frames), 32)
        for i, f in enumerate(frames):
            sheet.paste(f, i * 32, 0)
        out = os.path.join(CHARS, name + '.png')
        write(sheet, out)
        if not os.path.exists(out + '.meta'):
            meta_frames(name, len(frames), out + '.meta')
        print('wrote body', name)


# ------------------------------------------------------------------ 손에 드는 기본 무기
HELD = {
    'sword': [   # 장검
        '..kk...................',
        '.kyyk.kkkkkkkkkkkkkkkk.',
        'kbyyykwwwwwwwwwwwwwwwwk',
        'kbyyykGGGGGGGGGGGGGGGk.',
        '.kyyk.kkkkkkkkkkkkkkk..',
        '..kk...................',
    ],
    'shuriken': [  # 표창 (손에 쥔 것)
        '...kk...',
        '..kGGk..',
        'kkkGwkkk',
        'kGwkkwGk',
        'kkkwGkkk',
        '..kGGk..',
        '...kk...',
    ],
    'bow': [      # 활 (옆에서 본 모습, 시위는 흰 줄)
        'kk.......',
        'kbk......',
        '.kbk.....',
        '..kbk....',
        '..kbw....',
        '..kbw....',
        '..kbw....',
        '..kbk....',
        '.kbk.....',
        'kbk......',
        'kk.......',
    ],
    'flask': [    # 플라스크
        '..kkk..',
        '..kwk..',
        '..kok..',
        '.kvvvk.',
        'kvVvvvk',
        'kvvvVvk',
        '.kkkkk.',
    ],
}
HPAL = {'k': (25, 20, 28), 'y': (225, 185, 70), 'b': (110, 70, 40), 'w': (235, 235, 245), 'G': (160, 165, 180),
        'o': (200, 160, 70), 'v': (110, 235, 130), 'V': (200, 255, 200)}


def held():
    os.makedirs(WEAPONS, exist_ok=True)
    for name, rows in HELD.items():
        width = max(len(r) for r in rows)
        rows = [r.ljust(width, '.') for r in rows]
        img = Img.from_rows(rows, HPAL)
        out = os.path.join(WEAPONS, 'weapon_%s.png' % name)
        write(img, out)
        if not os.path.exists(out + '.meta'):
            weapon_meta(out + '.meta', 16, img.w, img.h)
        print('wrote held', name)


# ------------------------------------------------------------------ 이펙트
def sword_wave():
    # 초승달 검기 (오른쪽으로 날아감)
    frames = []
    for f in range(4):
        img = Img(24, 32)
        for y in range(32):
            for x in range(24):
                dx, dy = x + 0.5 - 6, y + 0.5 - 16
                d1 = math.hypot(dx, dy)
                d2 = math.hypot(dx + 5, dy)
                if d1 < 15 and d2 > 15.5 - f * 0.3:
                    t = (d1 - (15.5 - 5)) / 5
                    a = int(255 * max(0.0, min(1.0, 1 - abs(dy) / 16)))
                    c = (230, 245, 255) if t > 0.6 else (150, 200, 255) if t > 0.2 else (90, 140, 230)
                    img.blend(x, y, c + (a,))
        frames.append(img)
    return frames


def spin_slash():
    # 사방으로 휘두르는 칼날 고리
    frames = []
    for f in range(6):
        img = Img(48, 48)
        rot = f / 6 * 2 * math.pi
        for arm in range(4):
            for i in range(40):
                t = i / 40
                ang = rot + arm * math.pi / 2 + t * 1.3
                r = 12 + t * 10
                a = int(255 * (1 - t) * (1 - f / 8))
                disc(img, 24 + math.cos(ang) * r, 24 + math.sin(ang) * r, 2.2 - t, (220, 235, 255, a))
        ring(img, 24, 24, 21, 1.5, (160, 200, 255, int(200 * (1 - f / 6))))
        frames.append(img)
    return frames


def shuriken():
    frames = []
    for f in range(4):
        img = Img(12, 12)
        rot = f / 4 * math.pi / 2
        for k in range(4):
            a = rot + k * math.pi / 2
            for r in range(1, 6):
                x, y = 6 + math.cos(a) * r, 6 + math.sin(a) * r
                img.blend(int(x), int(y), (200, 205, 215, 255))
                x2, y2 = 6 + math.cos(a + 0.5) * (r - 1), 6 + math.sin(a + 0.5) * (r - 1)
                img.blend(int(x2), int(y2), (130, 135, 150, 255))
        disc(img, 6, 6, 1.4, (60, 60, 70, 255))
        frames.append(img)
    return frames


def stealth_smoke():
    frames = []
    random.seed(4)
    puffs = [(random.uniform(8, 24), random.uniform(10, 26), random.uniform(3, 6)) for _ in range(9)]
    for f in range(6):
        img = Img(32, 32)
        for x, y, r in puffs:
            rr = r * (0.6 + f * 0.18)
            a = int(200 * (1 - f / 6))
            disc(img, x, y - f * 0.8, rr, (95, 70, 130, a))
            disc(img, x - 1, y - f * 0.8 - 1, rr * 0.5, (150, 125, 185, a))
        frames.append(img)
    return frames


def arrow():
    frames = []
    for f in range(2):
        img = Img(16, 5)
        for x in range(2, 13):
            img.set(x, 2, (130, 90, 55))
        for x, y in [(13, 2), (12, 1), (12, 3), (14, 2), (11, 0), (11, 4)]:
            img.set(x, y, (210, 215, 225))
        for x, y in [(0, 1), (1, 1), (0, 3), (1, 3), (2, 1), (2, 3)]:
            img.set(x, y, (230, 230, 230) if f == 0 else (200, 90, 80))
        frames.append(img)
    return frames


def arrow_rain():
    # 하늘에서 떨어지는 화살 (아래를 향함) + 꽂히는 먼지
    frames = []
    for f in range(4):
        img = Img(10, 24)
        y0 = f * 5
        if f < 3:
            for y in range(y0, y0 + 12):
                img.set(5, y, (130, 90, 55))
            for dx, dy in [(0, 12), (-1, 11), (1, 11), (0, 13)]:
                img.set(5 + dx, y0 + dy, (215, 220, 230))
            img.set(4, y0, (235, 235, 235)); img.set(6, y0, (235, 235, 235))
        else:
            for y in range(12, 22):
                img.set(5, y, (130, 90, 55))
            for x in range(1, 9):
                img.blend(x, 22, (190, 170, 130, 200))
            disc(img, 5, 22, 2.5, (210, 190, 150, 150))
        frames.append(img)
    return frames


def flask_spin():
    frames = []
    base = Img.from_rows(HELD['flask'], HPAL)
    for f in range(4):
        img = Img(9, 9)
        for y in range(base.h):
            for x in range(base.w):
                sx, sy = x, y
                for _ in range(f):
                    sx, sy = sy, base.w - 1 - sx
                if 0 <= sx < base.w and 0 <= sy < base.h:
                    c = base.px[sy][sx]
                    if c[3]:
                        img.set(x + 1, y + 1, c)
        frames.append(img)
    return frames


def alchemy_blast():
    frames = []
    random.seed(12)
    bubbles = [(random.uniform(0, 2 * math.pi), random.uniform(0.5, 1)) for _ in range(12)]
    for f in range(6):
        img = Img(40, 40)
        t = (f + 1) / 6
        r = 6 + t * 13
        a = int(255 * (1 - t * 0.7))
        disc(img, 20, 20, r, (120, 230, 110, int(a * 0.55)))
        disc(img, 20, 20, r * 0.6, (210, 255, 170, int(a * 0.7)))
        ring(img, 20, 20, r, 2, (170, 90, 220, a))
        for ang, s in bubbles:
            d = r * s * 1.1
            disc(img, 20 + math.cos(ang) * d, 20 + math.sin(ang) * d, 1.8, (200, 150, 255, a))
        frames.append(img)
    return frames


if __name__ == '__main__':
    bodies()
    held()
    save_sheet('fx_swordwave', sword_wave())
    save_sheet('fx_spinslash', spin_slash())
    save_sheet('fx_shuriken', shuriken())
    save_sheet('fx_stealth', stealth_smoke())
    save_sheet('fx_arrow', arrow())
    save_sheet('fx_arrowrain', arrow_rain())
    save_sheet('fx_flask', flask_spin())
    save_sheet('fx_alchemyblast', alchemy_blast())
