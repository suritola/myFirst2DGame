# 검사 평타 · 도적 우클릭 이펙트
#   fx_swordswing  검을 크게 휘두른 초승달 궤적 (가운데 = 플레이어, 오른쪽을 향함, 위 → 아래로 쓸어내림)
#   fx_bleed       출혈: 몸에서 떨어지는 핏방울
#   fx_shadowdash  그림자 돌진: 보라 그림자 줄기와 붉은 칼끝
# 실행: python tools/pixelart/make_fx3.py
import math, os, random, sys
sys.path.insert(0, os.path.dirname(__file__))
from make_fx import disc, ring, line
from make_fx2 import save_sheet
from png import Img


def sword_swing():
    # 64x64, 반지름 10~31 부채꼴, 각도 +75° → -75° 로 쓸어내리며 뒤쪽은 옅어짐
    frames = []
    N = 6
    for f in range(N):
        img = Img(64, 64)
        head = 75 - 150 * min(1.0, (f + 1) / 4)       # 칼끝이 지나간 각도 (4프레임 만에 끝까지)
        fade = 1.0 if f < 4 else (1.0 - (f - 3) / 3)   # 다 휘두른 뒤 사라짐
        for y in range(64):
            for x in range(64):
                dx, dy = x + 0.5 - 32, 32 - (y + 0.5)
                r = math.hypot(dx, dy)
                if r < 9 or r > 31.5:
                    continue
                ang = math.degrees(math.atan2(dy, dx))
                if ang > 75 or ang < head:
                    continue
                # 칼끝(head)에 가까울수록 진하고, 바깥 테두리는 밝은 칼날 궤적 · 안쪽은 옅은 바람
                behind = (ang - head) / 150
                a = max(0.0, 1 - behind * 1.4) * fade
                edge = (r - 9) / 22.5
                if edge > 0.55:
                    a *= 0.55 + 0.45 * (edge - 0.55) / 0.45
                else:
                    a *= 0.18 * edge / 0.55
                if a <= 0.02:
                    continue
                if edge > 0.9:
                    c = (255, 255, 255)
                elif edge > 0.75:
                    c = (220, 238, 255)
                elif edge > 0.55:
                    c = (150, 195, 255)
                else:
                    c = (170, 200, 255)
                img.blend(x, y, c + (int(255 * min(1.0, a)),))
        # 칼끝 섬광
        if f < 4:
            ha = math.radians(head)
            for rr in range(14, 32, 3):
                disc(img, 32 + math.cos(ha) * rr, 32 - math.sin(ha) * rr, 1.3, (255, 255, 255, 230))
        frames.append(img)
    return frames


def bleed():
    # 20x20, 붉은 핏방울이 튀었다가 떨어짐
    random.seed(11)
    drops = [(random.uniform(-4, 4), random.uniform(-3, 2), random.uniform(1.0, 1.8)) for _ in range(6)]
    frames = []
    for f in range(6):
        img = Img(20, 20)
        t = f / 5
        for vx, vy, r in drops:
            x = 10 + vx * t * 1.4
            y = 7 + vy * t + 11 * t * t          # 아래로 떨어짐 (이미지 좌표 y 아래)
            a = int(255 * (1 - t * 0.7))
            disc(img, x, y, r * (1 - t * 0.3), (170, 20, 30, a))
            disc(img, x - 0.4, y - 0.4, r * 0.4, (255, 90, 90, a))
        if f < 2:
            ring(img, 10, 7, 3 + f * 2, 1, (220, 40, 50, 200 - f * 80))
        frames.append(img)
    return frames


def shadow_dash():
    # 48x16, 오른쪽으로 달리는 그림자 줄기 (가운데 = 줄기 중앙)
    frames = []
    for f in range(5):
        img = Img(48, 16)
        k = 1 - f / 5
        for x in range(48):
            t = x / 47                         # 0 = 꼬리, 1 = 머리
            w = (1.5 + 5 * t) * k
            for y in range(16):
                d = abs(y + 0.5 - 8)
                if d > w:
                    continue
                a = int(255 * t * (1 - d / (w + 0.01)) * k)
                c = (60, 30, 90) if d > w * 0.5 else (150, 90, 210)
                img.blend(x, y, c + (a,))
        # 머리의 붉은 칼날 선
        line(img, 30, 8 - 3 * k, 47, 8, (255, 70, 80, int(255 * k)), 1.0)
        line(img, 30, 8 + 3 * k, 47, 8, (255, 70, 80, int(255 * k)), 1.0)
        frames.append(img)
    return frames


if __name__ == '__main__':
    save_sheet('fx_swordswing', sword_swing())
    save_sheet('fx_bleed', bleed())
    save_sheet('fx_shadowdash', shadow_dash())
