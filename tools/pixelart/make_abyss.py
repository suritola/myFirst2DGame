# 4장 "영혼의 심연" 도트 만들기 (2.1.1~)
#   - 지옥 타일셋 · 소품을 검푸른 심연 색으로 다시 칠함 (배치 · 조각 위치는 그대로, 불꽃 · 용암은 영혼빛 하늘색으로)
#   - 심연 소품 (영혼 화로 · 부서진 석상 · 사슬 기둥 · 영혼 수정 · 묘비 · 뼈 무더기)
#   - 잡몹 5종 (떠도는 혼 · 사슬 망령 · 공허 눈알 · 영혼 수확자 · 심연 거상) 과 보스 거울의 군주: 움직임 4장씩
# 결과: Assets/Resources/Abyss/*.png (+ .meta)     실행: python tools/pixelart/make_abyss.py
import math, os, re, sys, uuid
sys.path.insert(0, os.path.dirname(__file__))
from png import Img, read, write
from make_fx import disc, ring, line
import make_desert as D

ROOT = D.ROOT
SPR = D.SPR
OUT = os.path.join(ROOT, 'Assets', 'Resources', 'Abyss')

# 어두운 곳 → 밝은 곳 심연 색 띠 (먹빛 남색 → 보라 → 옅은 청록)
RAMP = [(0.00, (10, 8, 20)), (0.16, (24, 20, 46)), (0.32, (42, 36, 80)), (0.48, (68, 56, 118)),
        (0.64, (98, 86, 156)), (0.80, (138, 136, 196)), (1.00, (196, 214, 236))]
SOUL = (120, 235, 255)


def recolor(img):
    D.RAMP = RAMP
    out = D.recolor(img)
    # 불꽃 · 용암(사막 변환이 노랗게 살린 밝은 주황)은 영혼빛으로
    for y in range(out.h):
        for x in range(out.w):
            c = img.px[y][x]
            if c[3] > 0 and D.hot(c):
                k = D.lum(c)
                out.px[y][x] = (int(SOUL[0] * k + 40), min(255, int(SOUL[1] * k + 60)), 255, c[3])
    return out


# ------------------------------------------------------------------ 색
K = (14, 10, 24, 255)          # 윤곽선
W = (240, 250, 255, 255)
C = (110, 230, 255, 255)       # 영혼 하늘색
CP = (200, 248, 255, 255)
V = (110, 70, 170, 255)        # 보라
VD = (62, 40, 104, 255)
VL = (168, 130, 225, 255)
G = (92, 100, 130, 255)        # 돌
GD = (58, 62, 88, 255)
GL = (140, 150, 180, 255)
S = (196, 200, 214, 255)       # 은 · 거울
SD = (120, 124, 150, 255)
R = (230, 70, 110, 255)
BONE = (226, 220, 200, 255)


def outline(img):
    src = [[img.px[y][x] for x in range(img.w)] for y in range(img.h)]
    for y in range(img.h):
        for x in range(img.w):
            if src[y][x][3] > 0:
                continue
            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                xx, yy = x + dx, y + dy
                if 0 <= xx < img.w and 0 <= yy < img.h and src[yy][xx][3] > 100 and src[yy][xx][:3] != K[:3]:
                    img.px[y][x] = K
                    break


def a(c, alpha):
    return (c[0], c[1], c[2], alpha)


# ------------------------------------------------------------------ 잡몹 (32 x 32, 가운데 기준 · 몸은 18 칸 안팎)
BOB = [0, -1, -2, -1]


def wisp(img, f):
    y = 17 + BOB[f]
    sway = [0, 1, 0, -1][f]
    for i in range(4):          # 꼬리 불꽃
        disc(img, 16 - sway * (i + 1) * 0.5, y + 6 + i * 1.6, 3.2 - i * 0.7, a(C, 200 - i * 40))
    disc(img, 16, y, 6.5, C)
    disc(img, 16 + sway, y - 5, 4.5, C)
    disc(img, 16 + sway * 2, y - 9, 2.4, C)
    disc(img, 16, y + 1, 4, CP)
    img.rect(13, y - 1, 2, 3, K)
    img.rect(18, y - 1, 2, 3, K)


def chainwraith(img, f):
    y0 = 8 + BOB[f]
    sway = [0, 1, 0, -1][f]
    for yy in range(y0 + 4, y0 + 21):          # 너풀거리는 옷자락
        w = 4 + (yy - y0 - 4) * 0.45
        for xx in range(int(16 - w + sway * (yy - y0) / 20), int(16 + w + 1 + sway * (yy - y0) / 20)):
            img.set(xx, yy, V if (xx + yy) % 5 else VD)
    for xx in range(9, 24, 3):                  # 찢어진 밑단
        img.set(xx + sway, y0 + 21, (0, 0, 0, 0))
    disc(img, 16, y0 + 5, 5, VD)                # 두건
    disc(img, 16, y0 + 6, 3.2, K)
    img.set(14, y0 + 6, C); img.set(18, y0 + 6, C)
    # 사슬 (양손에서 늘어짐)
    for side in (-1, 1):
        hx, hy = 16 + side * 6, y0 + 11
        for i in range(5):
            cx = hx + side * (i * 1.4) + (sway if side > 0 else -sway) * 0.4
            cy = hy + i * 2.2
            ring(img, cx, cy, 1.3, 0.7, S)


def voideye(img, f):
    y = 14 + BOB[f]
    look = [(0, 0), (1, 0), (0, 1), (-1, 0)][f]
    for i in range(5):                          # 촉수
        x0 = 9 + i * 3.5
        wave = math.sin(f * math.pi / 2 + i) * 1.5
        line(img, x0, y + 6, x0 + wave, y + 13, VD, 1.6)
        disc(img, x0 + wave, y + 13, 1, V)
    disc(img, 16, y, 9, (110, 50, 130, 255))
    disc(img, 16, y, 7, (236, 226, 240, 255))
    for k in range(6):                           # 핏줄
        ang = k * math.pi / 3
        line(img, 16 + math.cos(ang) * 4.5, y + math.sin(ang) * 4.5, 16 + math.cos(ang) * 6.5, y + math.sin(ang) * 6.5, (200, 120, 160, 255), 0.6)
    disc(img, 16 + look[0] * 1.5, y + look[1] * 1.5, 3.8, (160, 60, 230, 255))
    disc(img, 16 + look[0] * 1.8, y + look[1] * 1.8, 1.8, K)
    img.set(15 + look[0], y - 2 + look[1], W)


def reaper(img, f):
    y0 = 7 + BOB[f]
    sway = [0, 1, 0, -1][f]
    for yy in range(y0 + 5, y0 + 23):           # 검은 망토
        w = 3.5 + (yy - y0 - 5) * 0.38
        for xx in range(int(15 - w), int(15 + w + 1)):
            img.set(xx + (sway if yy > y0 + 15 else 0), yy, (36, 28, 56, 255) if (xx * 3 + yy) % 7 else (54, 44, 80, 255))
    disc(img, 15, y0 + 5, 4.6, (36, 28, 56, 255))
    disc(img, 15, y0 + 6, 3, BONE)              # 해골 얼굴
    img.set(14, y0 + 6, K); img.set(16, y0 + 6, K); img.set(15, y0 + 8, K)
    # 낫: 자루 + 영혼빛 날
    hx = 23 + sway
    line(img, hx, y0 + 2, hx - 2, y0 + 24, (110, 90, 70, 255), 1.4)
    for i in range(12):         # 자루 끝에서 바깥쪽(오른쪽 아래)으로 휘는 날
        t = i / 11
        ang = -math.pi * 0.5 + math.pi * 0.7 * t
        disc(img, hx + 0.5 + math.cos(ang) * 6.5, y0 + 8.5 + math.sin(ang) * 6.5, 1.2 - 0.6 * t, CP if i < 8 else C)
    img.set(10, y0 + 14, BONE); img.set(21, y0 + 14, BONE)      # 손


def colossus(img, f):
    y0 = 6 + [0, 0, -1, -1][f]
    arm = [0, 1, 2, 1][f]
    img.rect(8, y0 + 8, 16, 16, G)              # 몸통
    img.rect(8, y0 + 8, 16, 2, GL)
    img.rect(11, y0 + 2, 10, 7, G)              # 머리
    img.rect(11, y0 + 2, 10, 2, GL)
    img.rect(13, y0 + 5, 2, 2, C); img.rect(17, y0 + 5, 2, 2, C)
    img.rect(3, y0 + 9 + arm, 5, 12, GD)        # 팔
    img.rect(24, y0 + 9 + (2 - arm), 5, 12, GD)
    img.rect(2, y0 + 19 + arm, 7, 4, G)
    img.rect(23, y0 + 19 + (2 - arm), 7, 4, G)
    img.rect(10, y0 + 24, 5, 4, GD); img.rect(17, y0 + 24, 5, 4, GD)   # 다리
    # 영혼빛 균열
    for (x0, yy0, x1, yy1) in [(12, 11, 15, 16), (15, 16, 13, 21), (19, 12, 21, 18), (21, 18, 19, 22)]:
        line(img, x0, y0 + yy0, x1, y0 + yy1, C, 0.8)
    disc(img, 16, y0 + 15, 1.6, CP)


def mirrorlord(img, f):
    # 은빛 갑옷 · 거울 조각 망토 · 왕관, 얼굴 자리는 텅 빈 거울 (몸은 아래쪽 가운데: 보스 충돌체가 아래에 있음)
    y0 = 6 + BOB[f]
    shard = f
    for i in range(7):                          # 떠다니는 거울 조각
        ang = i / 7 * math.pi * 2 + shard * 0.4
        x, y = 16 + math.cos(ang) * 13, y0 + 13 + math.sin(ang) * 7
        img.set(int(x), int(y), CP); img.set(int(x) + 1, int(y), S)
    for yy in range(y0 + 10, y0 + 25):          # 망토
        w = 5 + (yy - y0 - 10) * 0.5
        for xx in range(int(16 - w), int(16 + w + 1)):
            img.set(xx, yy, VD if (xx + yy) % 4 else V)
    img.rect(11, y0 + 9, 10, 10, S)             # 갑옷
    img.rect(11, y0 + 9, 10, 2, W)
    img.rect(15, y0 + 11, 2, 7, SD)
    disc(img, 16, y0 + 5.5, 4.2, S)             # 둥근 투구
    disc(img, 16, y0 + 6, 2.8, (40, 46, 80, 255))           # 텅 빈 거울 얼굴
    line(img, 14.8, y0 + 4.8, 17, y0 + 7.4, CP, 0.6)        # 거울에 비친 빛
    for x, h in ((13, 2), (16, 3), (19, 2)):    # 뾰족한 영혼빛 왕관
        line(img, x, y0 + 1.5, x, y0 + 1.5 - h, C, 0.9)
    img.rect(8, y0 + 12, 3, 7, S); img.rect(21, y0 + 12, 3, 7, S)    # 팔
    line(img, 24, y0 + 18, 27, y0 + 5, CP, 1.2)  # 거울 검
    line(img, 25, y0 + 18, 28, y0 + 6, S, 0.6)


ENEMIES = {'wisp': wisp, 'chainwraith': chainwraith, 'voideye': voideye, 'reaper': reaper, 'colossus': colossus, 'mirrorlord': mirrorlord}


# ------------------------------------------------------------------ 소품 (16 px = 1칸)
def soul_brazier():
    img = Img(16, 20)
    img.rect(4, 12, 8, 3, GD); img.rect(5, 15, 6, 2, G); img.rect(6, 17, 4, 3, GD)
    disc(img, 8, 9, 3.8, C); disc(img, 8, 6, 2.4, C); disc(img, 8, 9, 2, CP); img.set(8, 2, C)
    return img


def statue():
    img = Img(16, 26)
    img.rect(3, 22, 10, 4, GD); img.rect(4, 20, 8, 2, G)
    img.rect(5, 9, 6, 11, G); img.rect(5, 9, 6, 1, GL)
    disc(img, 8, 6, 3, G)
    line(img, 6, 4, 10, 8, (0, 0, 0, 0), 1)    # 부서진 머리
    line(img, 7, 12, 9, 18, GD, 0.6)
    img.rect(11, 23, 3, 2, G)                   # 떨어진 조각
    return img


def chain_pillar():
    img = Img(12, 28)
    img.rect(3, 4, 6, 22, GD); img.rect(3, 4, 6, 1, GL); img.rect(2, 25, 8, 3, G); img.rect(2, 2, 8, 2, G)
    for i in range(6):
        ring(img, 6 + (1 if i % 2 else -1) * 0.8, 6 + i * 3.4, 1.4, 0.7, S)
    return img


def crystal():
    img = Img(14, 18)
    for (cx, h, w) in [(7, 15, 3), (3.5, 9, 2), (10.5, 10, 2)]:
        for yy in range(int(17 - h), 17):
            ww = w * (yy - (17 - h)) / h + 0.6
            for xx in range(int(cx - ww), int(cx + ww + 1)):
                img.set(xx, yy, CP if xx < cx else C)
    img.rect(2, 16, 10, 2, GD)
    return img


def grave():
    img = Img(12, 16)
    img.rect(2, 4, 8, 11, G); disc(img, 6, 4.5, 4, G); img.rect(2, 4, 1, 11, GL)
    img.rect(5, 6, 2, 6, GD); img.rect(3, 8, 6, 2, GD)
    img.rect(0, 14, 12, 2, VD)
    return img


def bones():
    img = Img(16, 8)
    disc(img, 4, 4, 2.5, BONE); img.set(3, 4, K); img.set(5, 4, K)
    line(img, 7, 6, 14, 3, BONE, 1); line(img, 8, 2, 13, 7, BONE, 1)
    return img


PROPS = {'soul_brazier': soul_brazier, 'statue': statue, 'chain_pillar': chain_pillar, 'crystal': crystal, 'grave': grave, 'bones': bones}


def single_meta(path, ppu, pivot_bottom):
    tpl = open(os.path.join(SPR, 'Items', 'hp_potion.png.meta'), encoding='utf-8').read()
    tpl = re.sub(r'guid: [0-9a-f]{32}', 'guid: ' + uuid.uuid4().hex, tpl, count=1)
    tpl = re.sub(r'spritePixelsToUnits: \d+', 'spritePixelsToUnits: %d' % ppu, tpl)
    if pivot_bottom:
        tpl = re.sub(r'spritePivot: \{[^}]*\}', 'spritePivot: {x: 0.5, y: 0}', tpl)
        tpl = re.sub(r'alignment: \d+', 'alignment: 7', tpl)
    open(path, 'w', encoding='utf-8', newline='\n').write(tpl)


def save(img, path, ppu, pivot_bottom=False):
    if not os.path.exists(path + '.meta'):
        single_meta(path + '.meta', ppu, pivot_bottom)
    write(img, path)


def main():
    os.makedirs(OUT, exist_ok=True)
    for src, dst in [('tileset_hell_32', 'tileset_abyss_32'), ('objects_hell', 'objects_abyss')]:
        img = recolor(read(os.path.join(SPR, src + '.png')))
        out = os.path.join(OUT, dst + '.png')
        write(img, out)
        if not os.path.exists(out + '.meta'):
            D.copy_meta(os.path.join(SPR, src + '.png.meta'), out + '.meta', src, dst)
            # 조각 이름도 _hell → _abyss (맵을 바꿀 때 이름으로 찾음)
            m = open(out + '.meta', encoding='utf-8').read().replace('_hell', '_abyss')
            open(out + '.meta', 'w', encoding='utf-8', newline='\n').write(m)
        print('wrote', dst)
    for name, draw in PROPS.items():
        save(draw(), os.path.join(OUT, 'prop_' + name + '.png'), 16, True)
    # 잡몹 · 보스: 32 x 32, 잡몹은 기존 적과 같은 8 px = 1칸, 보스는 32 px = 1칸 (프리팹 크기 20배)
    for name, draw in ENEMIES.items():
        for f in range(4):
            img = Img(32, 32)
            draw(img, f)
            outline(img)
            save(img, os.path.join(OUT, '%s_%d.png' % (name, f)), 32 if name == 'mirrorlord' else 8)
    print('wrote props', len(PROPS), 'enemies', len(ENEMIES))


if __name__ == '__main__':
    main()
