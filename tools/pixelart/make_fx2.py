# 보스 신규 스킬 이펙트 + 플레이어가 드는 무기 도트
#   Assets/Resources/FX/        fx_soulchain · fx_bonehand · fx_firepillar · fx_fissure · fx_geyser · fx_vortex (가로 프레임 시트)
#   Assets/Resources/Weapons/   weapon_<id>.png (한 장, 손잡이 쪽이 기준점)
# 실행: python tools/pixelart/make_fx2.py
import math, os, random, re, sys, uuid
sys.path.insert(0, os.path.dirname(__file__))
from png import Img, write
from make_fx import disc, ring, line, sheet_meta, FX, ROOT

WEAPONS = os.path.join(ROOT, 'Assets', 'Resources', 'Weapons')


def save_sheet(name, frames):
    fw, fh = frames[0].w, frames[0].h
    sheet = Img(fw * len(frames), fh)
    for i, fr in enumerate(frames):
        sheet.paste(fr, i * fw, 0)
    out = os.path.join(FX, name + '.png')
    write(sheet, out)
    if not os.path.exists(out + '.meta'):
        sheet_meta(name, fw, fh, len(frames), out + '.meta')
    print('wrote', name, len(frames), fw, 'x', fh)


def lerp(a, b, t):
    return tuple(round(x + (y - x) * t) for x, y in zip(a, b))


# ------------------------------------------------------------------ 리치 왕: 영혼 사슬 (땅에서 솟는 사슬)
def soul_chain():
    frames = []
    for f in range(6):
        img = Img(16, 40)
        h = [10, 24, 38, 38, 30, 18][f]            # 솟았다가 가라앉음
        fade = [255, 255, 255, 220, 150, 80][f]
        base = 39
        # 바닥 균열 빛
        for x in range(2, 14):
            img.blend(x, base, (120, 230, 255, fade // 2))
        for i in range(0, h, 6):
            y = base - i
            # 고리 하나 (세로 타원) + 옆에서 본 고리 번갈아
            if (i // 6) % 2 == 0:
                for yy in range(y - 5, y + 1):
                    for xx in range(5, 11):
                        edge = xx in (5, 10) or yy in (y - 5, y)
                        if edge:
                            img.blend(xx, yy, (170, 245, 255, fade))
                img.blend(7, y - 3, (255, 255, 255, fade)); img.blend(8, y - 3, (255, 255, 255, fade))
            else:
                for yy in range(y - 5, y + 1):
                    img.blend(7, yy, (200, 250, 255, fade)); img.blend(8, yy, (120, 220, 255, fade))
        # 꼭대기 영혼 불꽃
        top = base - h
        disc(img, 8, top + 1, 2.6, (150, 240, 255, fade))
        disc(img, 8, top + 1, 1.3, (255, 255, 255, fade))
        frames.append(img)
    return frames


# ------------------------------------------------------------------ 리치 왕: 망자의 손아귀 (뼈 손이 솟아 움켜쥠)
def bone_hand():
    B, S, K = (240, 232, 205), (180, 170, 150), (60, 50, 60)
    frames = []
    for f in range(6):
        img = Img(24, 28)
        rise = [4, 12, 18, 18, 18, 10][f]
        close = [0, 0, 0, 0.5, 1, 1][f]
        base = 27
        # 흙 파편
        for x in range(4, 20):
            img.blend(x, base, (90, 70, 60, 220))
        for k in range(5):
            random.seed(f * 10 + k)
            img.blend(random.randint(2, 21), base - random.randint(1, 4), (120, 100, 80, 200))
        top = base - rise
        # 손목 · 손바닥
        img.rect(10, top + 8, 4, max(1, rise - 8), B)
        img.rect(8, top + 5, 8, 4, B)
        img.rect(8, top + 8, 8, 1, S)
        # 손가락 5개 (움켜쥘수록 안쪽으로 굽음)
        for i, fx in enumerate([8, 10, 12, 14, 15]):
            length = 5 if i != 4 else 3
            for j in range(length):
                bend = int(close * j * (1 if fx >= 12 else -1) * 0.8)
                x = fx + bend if i != 4 else fx + j
                y = top + 5 - j if i != 4 else top + 7 - j
                img.set(x, y, B)
                if j == length - 1:
                    img.set(x, y - 1, S)
        # 가운데 해골 눈빛 (저주)
        img.blend(11, top + 6, (150, 90, 255, 255)); img.blend(12, top + 6, (150, 90, 255, 255))
        frames.append(img)
    return frames


# ------------------------------------------------------------------ 지옥의 군주: 지옥불 기둥
def fire_pillar():
    frames = []
    random.seed(21)
    for f in range(6):
        img = Img(20, 48)
        h = [12, 30, 46, 46, 40, 24][f]
        a = [255, 255, 255, 235, 190, 110][f]
        for y in range(48 - h, 48):
            k = (48 - y) / max(1, h)                  # 0 아래 → 1 위
            w = 8 * (1 - k * 0.55) + math.sin(y * 0.6 + f * 1.7) * 1.4
            for x in range(20):
                d = abs(x + 0.5 - 10) / max(1.0, w)
                if d < 1:
                    if d < 0.35: c = (255, 250, 210)
                    elif d < 0.65: c = (255, 190, 70)
                    else: c = (230, 70, 30)
                    img.blend(x, y, c + (int(a * (1 - k * 0.3)),))
        for _ in range(8):
            x, y = random.randint(3, 16), random.randint(48 - h, 47)
            img.blend(x, y - 3, (255, 240, 180, a))
        # 바닥 불티 고리
        for x in range(1, 19):
            img.blend(x, 47, (255, 120, 40, a))
        frames.append(img)
    return frames


# ------------------------------------------------------------------ 지옥의 군주: 용암 균열 (땅이 갈라지며 용암이 치솟음)
def fissure():
    frames = []
    random.seed(7)
    path = [(0, 8)]
    for x in range(1, 32):
        path.append((x, max(3, min(12, path[-1][1] + random.choice([-1, 0, 0, 1])))))
    for f in range(6):
        img = Img(32, 16)
        glow = [80, 160, 255, 255, 200, 120][f]
        spurt = [0, 0, 4, 7, 5, 2][f]
        for x, y in path:
            img.blend(x, y, (40, 10, 10, 255))
            img.blend(x, y + 1, (70, 20, 10, 230))
            img.blend(x, y - 1, (255, 120, 30, glow))
            if f >= 1:
                img.blend(x, y, (255, 200, 80, glow))
            if spurt and x % 5 == 2:
                for s in range(spurt):
                    img.blend(x, y - 1 - s, (255, 220 - s * 20, 90, 255 - s * 25))
        frames.append(img)
    return frames


# ------------------------------------------------------------------ 킹 슬라임: 산성 간헐천
def geyser():
    frames = []
    random.seed(33)
    for f in range(6):
        img = Img(20, 44)
        h = [6, 22, 42, 38, 26, 10][f]
        a = [255, 255, 255, 230, 180, 120][f]
        for y in range(44 - h, 44):
            k = (44 - y) / max(1, h)
            w = 4 + k * 3 + math.sin(y * 0.8 + f) * 0.8
            for x in range(20):
                d = abs(x + 0.5 - 10) / w
                if d < 1:
                    c = (230, 255, 170) if d < 0.4 else (140, 240, 90) if d < 0.75 else (70, 170, 60)
                    img.blend(x, y, c + (a,))
        # 튀는 방울
        for _ in range(7):
            x = random.randint(1, 18); y = 44 - h + random.randint(-4, 6)
            disc(img, x, y, 1.1, (170, 255, 120, a))
        # 바닥 웅덩이
        for x in range(1, 19):
            img.blend(x, 43, (90, 200, 70, a))
        frames.append(img)
    return frames


# ------------------------------------------------------------------ 킹 슬라임: 끈적 소용돌이 (도는 슬라임 고리)
def vortex():
    frames = []
    for f in range(6):
        img = Img(40, 40)
        rot = f / 6 * 2 * math.pi
        for arm in range(3):
            for i in range(60):
                t = i / 60
                ang = rot + arm * 2 * math.pi / 3 + t * 4.2
                r = 3 + t * 16
                x, y = 20 + math.cos(ang) * r, 20 + math.sin(ang) * r * 0.85
                c = (220, 255, 170, int(255 * (1 - t * 0.6))) if t < 0.3 else (120, 230, 90, int(230 * (1 - t * 0.7)))
                disc(img, x, y, 1.6 - t * 0.6, c)
        disc(img, 20, 20, 3, (60, 150, 50, 220))
        disc(img, 20, 20, 1.5, (20, 60, 20, 255))
        frames.append(img)
    return frames


# ------------------------------------------------------------------ 플레이어가 드는 무기 (오른쪽을 향함, 손잡이 = 왼쪽 가운데)
WP = {
    'k': (30, 24, 30), 'g': (90, 88, 100), 'G': (150, 150, 165), 'w': (230, 230, 240), 'b': (110, 70, 40), 'B': (160, 105, 60),
    'o': (255, 140, 40), 'O': (255, 210, 90), 'r': (200, 50, 40), 'c': (90, 230, 255), 'C': (200, 250, 255), 'p': (160, 90, 230),
    'P': (215, 170, 255), 'y': (255, 225, 80), 'Y': (255, 250, 190), 'e': (70, 170, 255), 'l': (120, 90, 60),
}
WEAPON_ROWS = {
    0: [  # 지옥불 산탄총: 두꺼운 이중 총열 + 주황 룬
        '............kkkkkkkk.....',
        '.kkkkkkkkkkkgGGGGGGGk....',
        'kbBBBBBkggggGGoGGoGGGkk..',
        'kbBBBBBkggggggggggggggGk.',
        '.kbbbbkkkGGGGGGGGGGGGGk..',
        '..kbbk..kkkkkkkkkkkkkk...',
        '..kbbk...................',
        '...kk....................',
    ],
    1: [  # 영혼 저격총: 긴 총열 + 조준경 + 청록 영혼 코일
        '........kkkkk...................',
        '.......kGCCCGk..................',
        '.kkkkkkkkkkkkkkkkkkkkkkkkkkkk...',
        'kbBBBBkggcCcCcggggggggggggggGkk.',
        'kbBBBBkGGGGGGGGGGGGGGGGGGGGGGGCk',
        '.kbbbkkkkkkkkkkkkkkkkkkkkkkkkkk.',
        '..kbk...........................',
        '..kk............................',
    ],
    2: [  # 황금 쌍권총: 두 자루가 겹친 모양
        '....kkkkkkkkk....',
        '...kyYYYYYYYYk...',
        '..kyyyyyyyyyyyk..',
        '.kkkkkkkkkkkkkk..',
        'kyYYYYYYYYYk.....',
        'kyyyyyyyyyyyk....',
        '.kbBkkkkkkkk.....',
        '.kbBk............',
        '..kk.............',
    ],
    3: [  # 화염 방사기: 연료통 + 넓은 분사구
        '..kkkk....................',
        '.krrrrk..kkkkkkkkkkkkk....',
        '.krOOrkkkgggggggggggggkkk.',
        '.krrrrkgGGGGGGGGGGGGGgoOok',
        '.krrrrkkkggggggggggggkkk..',
        '..kkkk..kbBk.kk...........',
        '........kbBk..............',
        '.........kk...............',
    ],
    4: [  # 영혼 추적포: 영혼 구슬이 박힌 짧은 포
        '.........kkkkk.......',
        '.kkkkkkkkpPPPpkkkkk..',
        'kbBBBkggkPCwCPkgggGk.',
        'kbBBBkGGkpPPPpkGGGGGk',
        '.kbbkkkkkkkkkkkkkkkk.',
        '..kbk................',
        '..kk.................',
    ],
    5: [  # 번개 사슬총: 코일 세 개 + 번개 끝
        '..........k...k...k.....',
        '.kkkkkkkkkeykeykeyk..y..',
        'kbBBBkgggkeekeekeekk.yY.',
        'kbBBBkGGGGGGGGGGGGGGkYy.',
        '.kbbkkkkkkkkkkkkkkkk..y.',
        '..kbk...................',
        '..kk....................',
    ],
    6: [  # 부메랑 낫 (들고 있는 모습)
        '.........kkkkkk.....',
        '.......kkPPPPPPkk...',
        '.....kkPPwwwwwPPPk..',
        '....kPPwkk...kkwPPk.',
        '...kPwk.........kPk.',
        '..kPk.............k.',
        '.kll................',
        'klk.................',
        'kk..................',
    ],
    7: [  # 용암 유탄 발사기: 드럼 탄창 + 굵은 포신
        '.......kkkkk.............',
        '.kkkkkkrrrrrkkkkkkkkkkk..',
        'kbBBBkkrOoOrkgggggggggGk.',
        'kbBBBkkrOoOrkGGGGGGGGGGok',
        '.kbbkkkrrrrrkkkkkkkkkkkk.',
        '..kbk..kkkkk.............',
        '..kk.....................',
    ],
}


def weapon_meta(path, ppu, w, h):
    tpl = open(os.path.join(ROOT, 'Assets', 'Sprites', 'Items', 'hp_potion.png.meta'), encoding='utf-8').read()
    tpl = re.sub(r'guid: [0-9a-f]{32}', 'guid: ' + uuid.uuid4().hex, tpl, count=1)
    tpl = re.sub(r'spritePixelsToUnits: \d+', 'spritePixelsToUnits: %d' % ppu, tpl)
    # 손잡이(왼쪽 아래쪽 가운데)를 기준점으로
    tpl = re.sub(r'spritePivot: \{[^}]*\}', 'spritePivot: {x: %.3f, y: 0.5}' % (3.0 / w), tpl)
    tpl = re.sub(r'alignment: \d+', 'alignment: 9', tpl)
    open(path, 'w', encoding='utf-8', newline='\n').write(tpl)


def weapons():
    os.makedirs(WEAPONS, exist_ok=True)
    for wid, rows in WEAPON_ROWS.items():
        width = max(len(r) for r in rows)
        rows = [r.ljust(width, '.') for r in rows]
        img = Img.from_rows(rows, WP)
        out = os.path.join(WEAPONS, 'weapon_%d.png' % wid)
        write(img, out)
        if not os.path.exists(out + '.meta'):
            weapon_meta(out + '.meta', 16, img.w, img.h)
        print('wrote weapon', wid, img.w, 'x', img.h)


if __name__ == '__main__':
    save_sheet('fx_soulchain', soul_chain())
    save_sheet('fx_bonehand', bone_hand())
    save_sheet('fx_firepillar', fire_pillar())
    save_sheet('fx_fissure', fissure())
    save_sheet('fx_geyser', geyser())
    save_sheet('fx_vortex', vortex())
    weapons()
