# 스팀 소형 캡슐 (462×174): 검색 결과 · 목록에서 가장 작게 보이는 이미지
# 글자만 있던 캡슐 대신, 총을 쏘는 거너 vs 몰려오는 괴물 떼 + 빛나는 로고로 "어떤 게임인지"가 한눈에 보이게
# Pillow 없이 tools/pixelart/png.py 로 합성
#
# 사용법: python tools/steam_art/make_small_capsule.py
# 결과:   docs/steam/art/capsules/small_capsule.png
import math
import os
import random
import sys

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
sys.path.insert(0, os.path.join(ROOT, "tools", "pixelart"))
import png  # noqa: E402

W, H = 462, 174
CELL = 32
SPR = os.path.join(ROOT, "Assets", "Sprites")
OUT = os.path.join(ROOT, "docs", "steam", "art", "capsules", "small_capsule.png")
random.seed(7)


# ---------------------------------------------------------------- 기본 도구
def load(path):
    return png.read(path)


def cell(img, row, col):
    out = png.Img(CELL, CELL)
    for y in range(CELL):
        for x in range(CELL):
            out.px[y][x] = img.get(col * CELL + x, row * CELL + y)
    return out


def scale_nearest(img, s, flip=False):
    out = png.Img(img.w * s, img.h * s)
    for y in range(out.h):
        for x in range(out.w):
            sx = x // s
            out.px[y][x] = img.px[y // s][img.w - 1 - sx if flip else sx]
    return out


def scale_box(img, w, h):
    # 큰 그림을 부드럽게 줄임 (영역 평균, 알파 가중)
    out = png.Img(w, h)
    for y in range(h):
        y0, y1 = int(y * img.h / h), max(int(y * img.h / h) + 1, int((y + 1) * img.h / h))
        for x in range(w):
            x0, x1 = int(x * img.w / w), max(int(x * img.w / w) + 1, int((x + 1) * img.w / w))
            r = g = b = a = n = 0
            for yy in range(y0, y1):
                row = img.px[yy]
                for xx in range(x0, x1):
                    pr, pg, pb, pa = row[xx]
                    r += pr * pa; g += pg * pa; b += pb * pa; a += pa; n += 1
            out.px[y][x] = (round(r / a), round(g / a), round(b / a), round(a / n)) if a else (0, 0, 0, 0)
    return out


def crop_alpha(img):
    xs = [x for y in range(img.h) for x in range(img.w) if img.px[y][x][3] > 10]
    ys = [y for y in range(img.h) for x in range(img.w) if img.px[y][x][3] > 10]
    x0, x1, y0, y1 = min(xs), max(xs) + 1, min(ys), max(ys) + 1
    out = png.Img(x1 - x0, y1 - y0)
    for y in range(out.h):
        out.px[y] = img.px[y0 + y][x0:x1]
    return out


def tint(img, k, add=(0, 0, 0)):
    out = png.Img(img.w, img.h)
    for y in range(img.h):
        for x in range(img.w):
            r, g, b, a = img.px[y][x]
            out.px[y][x] = (min(255, int(r * k + add[0])), min(255, int(g * k + add[1])), min(255, int(b * k + add[2])), a)
    return out


def paste(canvas, img, ox, oy, alpha=1.0):
    for y in range(img.h):
        cy = oy + y
        if not 0 <= cy < canvas.h:
            continue
        for x in range(img.w):
            p = img.px[y][x]
            if p[3]:
                canvas.blend(ox + x, cy, (p[0], p[1], p[2], int(p[3] * alpha)))


def add_light(canvas, cx, cy, radius, color, strength):
    # 더하기 빛 (원형, 가장자리로 갈수록 약하게)
    for y in range(max(0, int(cy - radius)), min(canvas.h, int(cy + radius) + 1)):
        for x in range(max(0, int(cx - radius)), min(canvas.w, int(cx + radius) + 1)):
            d = math.hypot(x - cx, y - cy) / radius
            if d >= 1:
                continue
            k = strength * (1 - d) ** 2
            r, g, b, a = canvas.px[y][x]
            canvas.px[y][x] = (min(255, int(r + color[0] * k)), min(255, int(g + color[1] * k)), min(255, int(b + color[2] * k)), a)


def outline(img, color=(10, 4, 8, 255)):
    # 1칸 짙은 윤곽선 (배경과 분리)
    out = png.Img(img.w + 2, img.h + 2)
    for y in range(img.h):
        for x in range(img.w):
            if img.px[y][x][3] > 40:
                for dx, dy in ((0, 1), (2, 1), (1, 0), (1, 2)):
                    out.px[y + dy][x + dx] = color
    for y in range(img.h):
        for x in range(img.w):
            if img.px[y][x][3]:
                out.px[y + 1][x + 1] = img.px[y][x]
    return out


# ---------------------------------------------------------------- 배경
canvas = png.Img(W, H)
for y in range(H):
    t = y / (H - 1)
    for x in range(W):
        # 위는 짙은 보라 밤하늘, 아래로 갈수록 지옥불 빛
        r = int(18 + 70 * t ** 1.6)
        g = int(8 + 14 * t ** 2)
        b = int(26 - 12 * t)
        canvas.px[y][x] = (r, g, b, 255)
# 바닥: 지옥 타일 한 줄
tiles = load(os.path.join(SPR, "tileset_hell_32.png"))
floor = tint(scale_nearest(cell(tiles, 0, 1), 1), 0.55)
for tx in range(0, W, CELL):
    paste(canvas, floor, tx, H - 22)
# 오른쪽 아래 불길 · 왼쪽 위 영혼 빛
add_light(canvas, W * 0.78, H + 10, 190, (255, 80, 20), 0.75)
add_light(canvas, W * 0.12, H * 0.35, 120, (90, 160, 255), 0.35)

# ---------------------------------------------------------------- 괴물 떼 (오른쪽, 왼쪽을 바라봄)
def enemy(path, scale, dark):
    s = scale_nearest(cell(load(os.path.join(SPR, "enemy", path)), 0, 0), scale, flip=True)
    s = crop_alpha(s)
    return outline(tint(s, dark, (int(30 * (1 - dark)), 0, 0)))


def silhouette(path, scale):
    # 뒷줄 괴물 떼: 붉게 물든 그림자
    s = crop_alpha(scale_nearest(cell(load(os.path.join(SPR, "enemy", path)), 0, 0), scale, flip=True))
    out = png.Img(s.w, s.h)
    for y in range(s.h):
        for x in range(s.w):
            r, g, b, a = s.px[y][x]
            lum = (r + g + b) / 765
            out.px[y][x] = (int(70 + 60 * lum), int(14 + 10 * lum), int(16 + 8 * lum), a)
    return out


def rim(e, color=(255, 120, 40)):
    # 오른쪽 아래 불길에 비친 테두리 빛 (오른쪽 · 아래 가장자리)
    out = png.Img(e.w, e.h)
    for y in range(e.h):
        out.px[y] = list(e.px[y])
    for y in range(e.h):
        for x in range(e.w):
            r, g, b, a = e.px[y][x]
            if a and (x + 1 >= e.w or e.px[y][x + 1][3] == 0 or y + 1 >= e.h or e.px[y + 1][x][3] == 0):
                out.px[y][x] = (min(255, r + color[0] // 2), min(255, g + color[1] // 2), min(255, b + color[2] // 3), a)
    return out


# 뒷줄: 겹겹이 몰려오는 실루엣 (멀수록 작고 위쪽)
crowd = ["hell/hell_imp.png", "hell/hell_flameskull.png", "hell/hell_hound.png", "eneymy3.png", "hell/hell_golem.png", "hell/hell_knight.png"]
for i in range(14):
    sc = 2 if i % 3 else 3
    sil = silhouette(crowd[i % len(crowd)], sc)
    x = 210 + (i * 37) % 260 + random.randint(-8, 8)
    y = 74 + (i * 17) % 38                      # 로고 아래로 (글자 주변이 지저분하지 않게)
    paste(canvas, sil, x - sil.w // 2, y, 0.75)
    # 빛나는 두 눈
    add_light(canvas, x - sil.w * 0.12, y + sil.h * 0.3, 2.2, (255, 60, 30), 1.2)
    add_light(canvas, x + sil.w * 0.05, y + sil.h * 0.3, 2.2, (255, 60, 30), 1.2)

# 앞줄: 크고 밝게, 불빛 테두리
front = [("hell/hell_imp.png", 4, 1.05, 244, 98), ("hell/hell_hound.png", 4, 1.35, 312, 104),
         ("boss.png", 4, 1.1, 386, 64), ("hell/hell_knight.png", 4, 1.0, 444, 92)]
for path, sc, dk, x, y in front:
    e = rim(enemy(path, sc, dk))
    paste(canvas, e, x - e.w // 2, min(y, H - 12 - e.h))

# 붉은 눈빛 · 불씨
for _ in range(60):
    ex, ey = random.uniform(200, W), random.uniform(20, H)
    add_light(canvas, ex, ey, random.uniform(1.5, 3.2), (255, random.randint(90, 170), 40), 1.0)

# ---------------------------------------------------------------- 거너 (왼쪽, 오른쪽으로 사격)
hero_sheet = load(os.path.join(SPR, "Players", "players blue x1.png"))
hero = outline(tint(crop_alpha(scale_nearest(cell(hero_sheet, 4, 0), 5)), 1.15, (10, 10, 18)), (6, 2, 14, 255))
hx, hy = 4, H - 8 - hero.h
add_light(canvas, hx + hero.w * 0.5, hy + hero.h * 0.55, 85, (120, 190, 255), 0.6)    # 영웅 뒤 푸른 영혼 빛
paste(canvas, hero, hx, hy)

# 총알 궤적: 총구 → 괴물들 (흰 심지 + 금빛 번짐)
mx, my = hx + hero.w - 2, hy + int(hero.h * 0.52)
burst = load(os.path.join(ROOT, "Assets", "Resources", "Fx", "fx_deathburst.png"))
bframe = png.Img(24, 24)
for y in range(24):
    bframe.px[y] = burst.px[y][48:72]
bframe = tint(scale_nearest(bframe, 2), 1.0, (60, 30, 0))
for (tx, ty) in ((250, 124), (318, 136), (384, 112)):
    steps = int(math.hypot(tx - mx, ty - my))
    for i in range(0, steps, 2):
        t = i / steps
        px, py = mx + (tx - mx) * t, my + (ty - my) * t
        fade = 0.35 + 0.65 * t
        add_light(canvas, px, py, 3.2, (255, 190, 60), 0.9 * fade)
        canvas.blend(int(px), int(py), (255, 250, 220, int(220 * fade)))
    add_light(canvas, tx, ty, 16, (255, 170, 60), 1.0)                                   # 명중 불꽃
    add_light(canvas, tx, ty, 6, (255, 255, 230), 1.2)
    paste(canvas, bframe, tx - bframe.w // 2, ty - bframe.h // 2, 0.9)
# 총구 불꽃
muzzle = load(os.path.join(ROOT, "Assets", "Resources", "Fx", "fx_muzzle.png"))
flash = png.Img(16, 16)
for y in range(16):
    flash.px[y] = muzzle.px[y][16:32]
flash = scale_nearest(flash, 2)
add_light(canvas, mx + 8, my, 22, (255, 200, 90), 1.1)
paste(canvas, flash, mx - 4, my - flash.h // 2)

# ---------------------------------------------------------------- 로고 (가운데 위, 크게)
logo = crop_alpha(load(os.path.join(ROOT, "docs", "steam", "art", "capsules", "library_logo.png")))
lw = 300
lh = round(logo.h * lw / logo.w)
logo = scale_box(logo, lw, lh)
lx, ly = (W - lw) // 2 + 34, 10
# 글자 뒤 짙은 그림자 띠 (어디에 놓여도 읽히게)
for y in range(ly - 8, ly + lh + 10):
    for x in range(lx - 24, lx + lw + 24):
        if 0 <= x < W and 0 <= y < H:
            dx = max(0, abs(x - (lx + lw / 2)) - lw / 2) / 24
            dy = max(0, abs(y - (ly + lh / 2)) - lh / 2) / 10
            k = max(0.0, 1 - math.hypot(dx, dy)) * 0.6
            r, g, b, a = canvas.px[y][x]
            canvas.px[y][x] = (int(r * (1 - k)), int(g * (1 - k)), int(b * (1 - k)), a)
add_light(canvas, lx + lw / 2, ly + lh / 2, 170, (255, 150, 60), 0.18)
paste(canvas, logo, lx, ly)

# 가장자리 비네트
for y in range(H):
    for x in range(W):
        d = math.hypot((x - W / 2) / (W * 0.62), (y - H / 2) / (H * 0.75))
        k = min(1.0, max(0.0, d - 0.55) * 1.4) * 0.55
        r, g, b, a = canvas.px[y][x]
        canvas.px[y][x] = (int(r * (1 - k)), int(g * (1 - k)), int(b * (1 - k)), 255)

png.write(canvas, OUT)
print("저장:", OUT)
