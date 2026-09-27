# v3.5 캐릭터 특수 능력 이펙트
#   fx_card      도적 도박 카드 (빙글 도는 카드 2장면)
#   fx_caltrop   도적 마름쇠
#   fx_net       궁수 그물 (펼쳐지는 3장면)
#   fx_falcon    궁수 매 (날갯짓 2장면)
#   fx_bigsword  검사 심판의 대검 (칼끝이 아래)
# 실행: python tools/pixelart/make_fx4.py
import math, os, sys
sys.path.insert(0, os.path.dirname(__file__))
from make_fx import disc, ring, line
from make_fx2 import save_sheet
from png import Img

W = (245, 245, 250, 255); K = (30, 25, 35, 255); R = (220, 50, 60, 255); Y = (240, 200, 80, 255)
G = (170, 175, 190, 255); D = (90, 90, 105, 255); B = (120, 80, 50, 255); T = (215, 195, 140, 255)


def card():
    frames = []
    for f, w in enumerate((6, 3)):          # 앞면 · 옆으로 돈 모습
        img = Img(16, 16)
        x0 = 8 - w
        for y in range(2, 14):
            for x in range(x0, 8 + w):
                img.set(x, y, W)
        for y in range(2, 14):
            img.set(x0, y, K); img.set(8 + w - 1, y, K)
        for x in range(x0, 8 + w):
            img.set(x, 2, K); img.set(x, 13, K)
        if w > 4:
            disc(img, 8, 8, 2, R)                # 하트 비슷한 붉은 무늬
            img.set(x0 + 1, 3, R); img.set(8 + w - 2, 12, R)
        frames.append(img)
    return frames


def caltrop():
    img = Img(12, 12)
    for a in (0, 120, 240):
        r = math.radians(a - 90)
        line(img, 6, 6, 6 + math.cos(r) * 5, 6 + math.sin(r) * 5, G, 1.3)
    disc(img, 6, 6, 1.6, D)
    img.set(6, 6, W)
    return [img]


def net():
    frames = []
    for f in range(3):
        img = Img(32, 32)
        r = 7 + f * 4.5
        for k in range(8):                      # 방사줄
            a = k * math.pi / 4
            line(img, 16, 16, 16 + math.cos(a) * r, 16 + math.sin(a) * r, T, 1)
        for rr in (r * 0.35, r * 0.7, r):        # 동심 고리
            ring(img, 16, 16, rr, 1, T)
        frames.append(img)
    return frames


def falcon():
    frames = []
    for up in (True, False):
        img = Img(24, 16)
        # 몸통 (오른쪽을 봄)
        for x in range(6, 18):
            h = 2.2 * math.sin((x - 6) / 12 * math.pi)
            for y in range(int(9 - h), int(9 + h) + 1):
                img.set(x, y, B)
        disc(img, 18, 8, 2.2, B)
        img.set(20, 8, Y); img.set(21, 9, Y)       # 부리
        img.set(18, 7, K)                          # 눈
        line(img, 3, 9, 7, 9, B, 1.5)              # 꼬리
        # 날개
        if up:
            line(img, 10, 8, 6, 1, (150, 110, 70, 255), 1.8); line(img, 12, 8, 11, 2, (150, 110, 70, 255), 1.5)
        else:
            line(img, 10, 10, 6, 15, (150, 110, 70, 255), 1.8); line(img, 12, 10, 11, 14, (150, 110, 70, 255), 1.5)
        frames.append(img)
    return frames


def bigsword():
    img = Img(16, 48)
    for y in range(12, 46):                        # 칼날 (아래로 갈수록 뾰족)
        w = 3 if y < 40 else max(0, 3 - (y - 40) // 2)
        for x in range(8 - w, 8 + w):
            img.set(x, y, W if x < 8 else G)
    line(img, 8, 13, 8, 42, (200, 220, 255, 255), 0.6)
    for x in range(1, 15):                         # 가드
        img.set(x, 10, Y); img.set(x, 11, Y)
    for y in range(2, 10):                         # 손잡이
        img.set(7, y, B); img.set(8, y, B)
    disc(img, 8, 2, 1.6, Y)
    return [img]


if __name__ == '__main__':
    save_sheet('fx_card', card())
    save_sheet('fx_caltrop', caltrop())
    save_sheet('fx_net', net())
    save_sheet('fx_falcon', falcon())
    save_sheet('fx_bigsword', bigsword())
