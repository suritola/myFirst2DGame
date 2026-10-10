# 고르기 창 그림 (1.0.5~, ChoiceUI · TreasureChest): 32x32, 배경 없이 윤곽선 있는 도트
#   → Assets/Resources/Icons/choice_<이름>.png
#   보물 상자: gold 코인 더미 · soul 영혼 수정 · life 하트 / 보스 보상: might 검 · shards 수정 다발 · essence 붉은 물약
#   저주 제단: curse 보랏빛 눈의 해골 · refuse 방패 / chest 보물 상자 (게임 안 상자 그림으로도)
# 실행: py tools/pixelart/make_choice_icons.py
import os, re, sys, uuid
sys.path.insert(0, os.path.dirname(__file__))
from png import Img, write
from make_fx import disc, ring, line
from make_menu_icons import outline, retry, OUT, K, W, G, GD, R, B, P, S

BL = (170, 225, 255, 255)       # 밝은 영혼 파랑
BD = (60, 110, 200, 255)        # 어두운 영혼 파랑
RD = (150, 35, 45, 255)         # 어두운 빨강
BR = (130, 80, 45, 255)         # 나무
BRD = (90, 55, 30, 255)
BONE = (232, 225, 240, 255)
OR = (255, 150, 60, 255)


def crystal(img, cx, top, h, w, light=BL, mid=B, dark=BD):
    # 위아래로 뾰족한 수정 (왼쪽 면은 밝게)
    mid_y = top + h * 0.4
    for y in range(int(top), int(top + h) + 1):
        if y <= mid_y: half = (y - top) / max(1, mid_y - top) * w
        else: half = (top + h - y) / max(1, top + h - mid_y) * w
        for x in range(int(round(cx - half)), int(round(cx + half)) + 1):
            img.set(x, y, light if x < cx - 0.5 else mid if x < cx + half * 0.5 else dark)


def coin_at(img, cx, cy, r):
    disc(img, cx, cy, r, G)
    ring(img, cx, cy, r, 1.2, GD)
    disc(img, cx - r * 0.3, cy - r * 0.3, r * 0.35, (255, 235, 170, 255))


def gold(img):
    coin_at(img, 10, 21, 6.5)
    coin_at(img, 22, 21, 6.5)
    coin_at(img, 16, 12, 6.5)


def soul(img):
    crystal(img, 16, 3, 26, 8)


def heart(img, color, dark):
    disc(img, 11, 12, 6, color)
    disc(img, 21, 12, 6, color)
    for y in range(12, 28):
        half = max(0, 11.5 - (y - 12) * 0.75)
        for x in range(int(16 - half), int(16 + half) + 1):
            img.set(x, y, color)
    for y in range(14, 27):
        half = max(0, 11.5 - (y - 12) * 0.75)
        img.set(int(16 + half), y, dark)
    disc(img, 10, 10, 2, (255, 170, 170, 255))


def life(img):
    heart(img, R, RD)
    line(img, 16, 14, 16, 21, W, 1.6)
    line(img, 12.5, 17.5, 19.5, 17.5, W, 1.6)


def might(img):
    line(img, 9, 23, 26, 6, S, 3)                      # 칼날
    line(img, 10, 21, 25, 6, W, 1)                     # 날 빛
    line(img, 6, 19, 13, 26, G, 2.2)                   # 손잡이 가드
    line(img, 4, 28, 9, 23, BR, 2.4)                   # 손잡이
    disc(img, 4, 28, 1.8, G)
    for x, y in ((24, 15), (14, 5), (27, 11), (19, 3)): disc(img, x, y, 1.2, OR)     # 불꽃


def shards(img):
    crystal(img, 9, 9, 18, 5)
    crystal(img, 23, 9, 18, 5)
    crystal(img, 16, 3, 24, 6.5)


def essence(img):
    disc(img, 16, 20, 8.5, RD)
    disc(img, 16, 20, 7.5, R)
    disc(img, 13, 17, 2.2, (255, 170, 170, 255))
    img.rect(13, 7, 6, 6, (200, 220, 235, 255))       # 병목
    img.rect(12, 4, 8, 3, BR)                         # 마개
    for x in range(10, 23): img.set(x, 13, (200, 220, 235, 255))


def curse(img):
    disc(img, 16, 13, 8, BONE)
    img.rect(11, 18, 10, 7, BONE)
    disc(img, 12.5, 13, 2.6, K)
    disc(img, 19.5, 13, 2.6, K)
    disc(img, 12.5, 13, 1.3, P)
    disc(img, 19.5, 13, 1.3, P)
    img.set(16, 17, K); img.set(15, 18, K); img.set(17, 18, K)
    for x in (13, 16, 19): line(img, x, 21, x, 24, (150, 140, 160, 255), 1)


def refuse(img):
    for y in range(5, 28):
        half = 10 if y < 17 else max(0, 10 - (y - 17) * 0.95)
        for x in range(int(16 - half), int(16 + half) + 1):
            img.set(x, y, (120, 150, 190, 255) if x < 16 else (90, 115, 155, 255))
    for y in range(7, 25):
        half = 7 if y < 17 else max(0, 7 - (y - 17) * 0.95)
        for x in range(int(16 - half), int(16 + half) + 1):
            img.set(x, y, (160, 190, 225, 255))
    line(img, 16, 8, 16, 24, S, 1.4)
    line(img, 10, 14, 22, 14, S, 1.4)


def chest(img):
    img.rect(4, 14, 24, 13, BR)                       # 몸통
    img.rect(4, 14, 24, 2, BRD)
    for y in range(7, 14):                            # 둥근 뚜껑
        inset = max(0, (10 - y) // 2) if y < 10 else 0
        for x in range(4 + inset, 28 - inset):
            img.set(x, y, (155, 95, 55, 255))
    for x in (7, 24):                                 # 금빛 띠
        line(img, x, 7, x, 26, G, 1.6)
    img.rect(13, 15, 6, 6, G)                         # 자물쇠
    img.rect(15, 17, 2, 3, K)
    disc(img, 26, 6, 1.6, (255, 245, 200, 255))      # 반짝


ICONS = {'gold': gold, 'soul': soul, 'life': life, 'might': might, 'shards': shards,
         'essence': essence, 'curse': curse, 'refuse': refuse, 'chest': chest}


def main():
    tpl = open(os.path.join(OUT, 'ability_85.png.meta'), encoding='utf-8').read()
    for name, draw in ICONS.items():
        img = Img(32, 32)
        draw(img)
        outline(img)
        path = os.path.join(OUT, 'choice_%s.png' % name)
        meta = re.sub(r'guid: [0-9a-f]{32}', 'guid: ' + uuid.uuid4().hex, tpl, count=1)
        if not os.path.exists(path + '.meta'):
            retry(lambda: open(path + '.meta', 'w', encoding='utf-8', newline='\n').write(meta))
        retry(lambda: write(img, path))
        print('그림 저장:', name)


if __name__ == '__main__':
    main()
