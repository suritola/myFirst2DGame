# 날아가는 검기 그림 (1.0.5): 예전엔 가늘고 흐린 초승달이라 작은 빛 덩어리(총알)처럼 보였음
# → 두껍고 선명한 초승달 칼날 (앞쪽 흰 날 · 하늘색 테 · 뒤로 흩날리는 바람결), 4프레임 반짝임
# 프레임 크기 24x32 · 4장 (fx_swordwave.png.meta 의 자르기와 같게), 오른쪽으로 날아감
# 실행: py tools/pixelart/make_swordwave.py
import math, os, sys
sys.path.insert(0, os.path.dirname(__file__))
from png import Img, write
from make_fx import ROOT

OUT = os.path.join(ROOT, 'Assets', 'Resources', 'FX', 'fx_swordwave.png')


def frame(f):
    img = Img(24, 32)
    pulse = [1.0, 0.92, 1.0, 0.85][f]
    for y in range(32):
        for x in range(24):
            dx, dy = x + 0.5, y + 0.5 - 16
            d_out = math.hypot(dx - 4, dy)        # 바깥 원 (앞쪽 날)
            d_in = math.hypot(dx + 4, dy * 1.05)   # 안쪽 원 (깎아 낸 부분)
            if d_out > 15.5 or d_in < 16.5: continue
            edge = 15.5 - d_out                    # 앞쪽 날에서 얼마나 안쪽인지
            tip = 1 - min(1.0, abs(dy) / 16)       # 끝으로 갈수록 옅게
            if edge < 1.6: c = (255, 255, 255)
            elif edge < 3.4: c = (190, 235, 255)
            else: c = (110, 170, 245)
            a = int(255 * max(0.0, min(1.0, 0.35 + tip)) * pulse)
            img.blend(x, y, c + (a,))
    # 뒤로 흩날리는 바람결
    for i, (y, l) in enumerate(((9, 6), (14, 9), (18, 8), (23, 5))):
        off = (f + i) % 3
        for x in range(off, off + l):
            a = int(160 * (x - off) / max(1, l))
            img.blend(x, y, (200, 235, 255, a))
    return img


def main():
    sheet = Img(96, 32)
    for f in range(4):
        fr = frame(f)
        for y in range(32):
            for x in range(24):
                c = fr.get(x, y)
                if c[3]: sheet.set(f * 24 + x, y, c)
    write(sheet, OUT)
    print('검기 그림 저장:', OUT)


if __name__ == '__main__':
    main()
