# 메인 화면 아이콘 (1.8.5~): 32x32, 배경 없이 윤곽선 있는 도트
#   → Assets/Resources/Icons/menu_<이름>.png
# 실행: python tools/pixelart/make_menu_icons.py
import math, os, re, sys, uuid, time
sys.path.insert(0, os.path.dirname(__file__))
from png import Img, write
from make_fx import disc, ring, line, ROOT

OUT = os.path.join(ROOT, 'Assets', 'Resources', 'Icons')
K = (20, 16, 26, 255); W = (245, 240, 230, 255); G = (245, 205, 110, 255); GD = (190, 140, 60, 255)
R = (220, 70, 70, 255); B = (110, 170, 240, 255); P = (190, 120, 240, 255); GR = (120, 220, 130, 255); S = (170, 175, 190, 255)


def outline(img):
    # 칠한 칸 둘레에 1칸 윤곽선 (밝은 배경에서도 또렷하게)
    src = [[img.px[y][x] for x in range(img.w)] for y in range(img.h)]
    for y in range(img.h):
        for x in range(img.w):
            if src[y][x][3] > 0: continue
            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                xx, yy = x + dx, y + dy
                if 0 <= xx < img.w and 0 <= yy < img.h and src[yy][xx][3] > 0 and src[yy][xx][:3] != K[:3]:
                    img.px[y][x] = K
                    break


def play(img):
    for y in range(7, 26):
        w = 13 - abs(y - 16) * 13 // 9
        for x in range(10, 10 + max(0, w) * 1):
            img.set(x, y, G)
        for x in range(10, 10 + max(0, w)):
            pass
    line(img, 11, 9, 11, 23, (255, 235, 160, 255), 1)


def character(img):
    disc(img, 16, 10, 5, (232, 195, 158, 255))
    for y in range(9, 12): img.set(12, y, K)
    img.rect(9, 17, 14, 11, B)
    img.rect(9, 17, 14, 2, (150, 200, 250, 255))


def skin(img):
    # 옷걸이 + 옷
    line(img, 16, 5, 16, 9, S, 1.4); ring(img, 16, 5, 2, 1, S)
    line(img, 16, 9, 6, 15, S, 1.4); line(img, 16, 9, 26, 15, S, 1.4)
    img.rect(8, 15, 16, 12, P)
    img.rect(8, 15, 16, 2, (225, 170, 255, 255))
    disc(img, 16, 21, 2, G)


def codex(img):
    img.rect(6, 7, 20, 19, (150, 90, 50, 255))
    img.rect(8, 8, 16, 17, (240, 230, 205, 255))
    line(img, 16, 8, 16, 24, (150, 90, 50, 255), 1)
    for y in (11, 14, 17, 20): line(img, 10, y, 14, y, (150, 140, 120, 255), 1); line(img, 18, y, 22, y, (150, 140, 120, 255), 1)


def tutorial(img):
    ring(img, 16, 12, 6, 2.2, GR, gaps=0)
    for x in range(8, 16): img.set(x, 13, (0, 0, 0, 0)); img.set(x, 14, (0, 0, 0, 0)); img.set(x, 15, (0, 0, 0, 0))
    line(img, 16, 18, 16, 21, GR, 2.2)
    disc(img, 16, 26, 1.8, GR)


def settings(img):
    for i in range(8):
        a = i * math.pi / 4
        disc(img, 16 + math.cos(a) * 10, 16 + math.sin(a) * 10, 2.6, S)
    disc(img, 16, 16, 8.5, S)
    disc(img, 16, 16, 3.5, (0, 0, 0, 0))


def exit_(img):
    img.rect(7, 5, 13, 23, (120, 80, 60, 255))
    img.rect(9, 7, 9, 19, (70, 45, 35, 255))
    disc(img, 16, 17, 1.2, G)
    line(img, 20, 16, 28, 16, R, 2)
    line(img, 28, 16, 24, 12, R, 2); line(img, 28, 16, 24, 20, R, 2)


def coin(img):
    disc(img, 16, 16, 11, G)
    ring(img, 16, 16, 11, 1.4, GD)
    disc(img, 16, 16, 6, (255, 230, 150, 255))
    line(img, 16, 11, 16, 21, GD, 2)


ICONS = {'play': play, 'character': character, 'skin': skin, 'codex': codex, 'tutorial': tutorial,
         'settings': settings, 'exit': exit_, 'coin': coin}


def retry(fn):
    for i in range(20):
        try:
            return fn()
        except PermissionError:
            time.sleep(0.5)
    return fn()


def main():
    tpl = open(os.path.join(OUT, 'ability_85.png.meta'), encoding='utf-8').read()
    for name, draw in ICONS.items():
        img = Img(32, 32)
        draw(img)
        outline(img)
        path = os.path.join(OUT, 'menu_%s.png' % name)
        meta = re.sub(r'guid: [0-9a-f]{32}', 'guid: ' + uuid.uuid4().hex, tpl, count=1)
        # 메타를 먼저 (편집기가 그림을 먼저 보면 기본 설정 메타를 만들어 버림)
        if not os.path.exists(path + '.meta'):
            retry(lambda: open(path + '.meta', 'w', encoding='utf-8', newline='\n').write(meta))
        retry(lambda: write(img, path))
        print('wrote', name)


if __name__ == '__main__':
    main()
