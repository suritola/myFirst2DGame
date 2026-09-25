# 거너 스프라이트에서 손에 든 권총(과 총구 섬광)을 지운 시트를 만든다
# → Assets/Resources/Characters/gunner_nogun.png (+ .meta: 원본과 같은 조각 · 같은 이름)
# 게임에서는 PlayerLook 이 애니메이션이 고른 프레임을 같은 이름의 이 프레임으로 바꿔 끼우고,
# 권총은 따로 그린 weapon_pistol 을 손에 들어 마우스 방향으로 겨눈다.
# 실행: python tools/pixelart/make_gunner_nogun.py
import os, re, sys, uuid
sys.path.insert(0, os.path.dirname(__file__))
from png import Img, read, write

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
SRC = os.path.join(ROOT, 'Assets', 'Sprites', 'Players', 'players blue x1.png')
OUT_DIR = os.path.join(ROOT, 'Assets', 'Resources', 'Characters')
OUT = os.path.join(OUT_DIR, 'gunner_nogun.png')

# 총 · 섬광 색
GUN = {(149, 147, 150), (159, 156, 163), (160, 157, 162), (153, 151, 155), (169, 165, 173), (217, 211, 220),
       (138, 123, 147), (188, 173, 161), (255, 154, 60), (242, 214, 126), (255, 246, 200)}
# 윤곽선 색 (총 둘레에만 붙어 있으면 함께 지움)
OUTLINE = {(20, 17, 23), (29, 25, 33), (43, 36, 51), (44, 38, 50)}


def main():
    im = read(SRC)
    meta = open(SRC + '.meta', encoding='utf-8').read()
    rects = re.findall(r'name: (players blue x1_\d+)\n\s+rect:\n\s+serializedVersion: 2\n\s+x: (\d+)\n\s+y: (\d+)\n\s+width: (\d+)\n\s+height: (\d+)', meta)
    out = Img(im.w, im.h)
    for y in range(im.h):
        for x in range(im.w):
            out.px[y][x] = im.px[y][x]

    removed_total = 0
    for name, x, y, w, h in rects:
        x, y, w, h = map(int, (x, y, w, h))
        top = im.h - y - h
        cells = [(xx, yy) for yy in range(top, top + h) for xx in range(x, x + w)]
        gun = {(xx, yy) for xx, yy in cells if im.px[yy][xx][3] and im.px[yy][xx][:3] in GUN}
        if len(gun) > 40:          # 온몸이 회색인 쓰러짐 프레임은 건드리지 않음
            continue
        body = {(xx, yy) for xx, yy in cells if im.px[yy][xx][3] and (xx, yy) not in gun and im.px[yy][xx][:3] not in OUTLINE}
        drop = set(gun)
        # 총에 붙은 윤곽선 중 몸(색 있는 픽셀)에 닿지 않는 것
        for xx, yy in cells:
            c = im.px[yy][xx]
            if not c[3] or c[:3] not in OUTLINE:
                continue
            near = [(xx + dx, yy + dy) for dx in (-1, 0, 1) for dy in (-1, 0, 1) if dx or dy]
            if any(p in gun for p in near) and not any(p in body for p in near):
                drop.add((xx, yy))
        for xx, yy in drop:
            out.px[yy][xx] = (0, 0, 0, 0)
        removed_total += len(drop)

    os.makedirs(OUT_DIR, exist_ok=True)
    write(out, OUT)
    if not os.path.exists(OUT + '.meta'):
        m = re.sub(r'guid: [0-9a-f]{32}', 'guid: ' + uuid.uuid4().hex, meta, count=1)
        open(OUT + '.meta', 'w', encoding='utf-8', newline='\n').write(m)
    print('removed', removed_total, 'pixels from', len(rects), 'frames')


if __name__ == '__main__':
    main()
