# 마우스 커서 (1.8.7~): 48x48 도트, 메뉴용 화살표(끝 = 3,3) · 전투용 조준점(가운데 = 24,24) · 검은 테두리 2겹 + 밝은 테두리
#   → Assets/Resources/Cursors/<id>_arrow.png, <id>_aim.png (Unity 텍스처 종류 = 커서)
#   → Assets/Resources/Icons/<id>.png (스킨 상점 미리보기 · 스프라이트)
#   전설(cursor_gold)은 반짝임이 도는 4장: <id>_aim_0~3.png, <id>_arrow_0~3.png
# 실행: python tools/pixelart/make_cursors.py
import math, os, re, sys, uuid, time
sys.path.insert(0, os.path.dirname(__file__))
from png import Img, write
from make_fx import ROOT

CUR = os.path.join(ROOT, 'Assets', 'Resources', 'Cursors')
ICO = os.path.join(ROOT, 'Assets', 'Resources', 'Icons')
K = (18, 14, 24, 255)

# id: (채움, 밝은 면, 어두운 면, 조준 가운데)
SKINS = {
    'cursor_default': ((240, 232, 214, 255), (255, 255, 255, 255), (190, 170, 140, 255), (245, 205, 110, 255)),
    'cursor_bone':    ((214, 208, 190, 255), (245, 240, 225, 255), (150, 140, 120, 255), (120, 110, 95, 255)),
    'cursor_ember':   ((235, 110, 45, 255), (255, 210, 90, 255), (160, 45, 30, 255), (255, 235, 150, 255)),
    'cursor_soul':    ((150, 95, 235, 255), (140, 235, 255, 255), (80, 45, 150, 255), (170, 245, 255, 255)),
    'cursor_gold':    ((245, 200, 80, 255), (255, 250, 210, 255), (175, 120, 35, 255), (255, 255, 255, 255)),
}
ANIM = {'cursor_gold'}

N = 48  # 커서 크기 (1.8.7: 잘 보이게 48, 게임이 직접 그리는 소프트웨어 커서)

# 화살표 모양 (끝이 왼쪽 위): 줄마다 칠할 칸 [시작, 끝)
ARROW = [(0, n) for n in range(1, 13)] + [(0, 7), (0, 7), (4, 7), (4, 7), (5, 8), (5, 8), (6, 8)]


def arrow(fill, hi, lo, frame=0, sparkle=False):
    img = Img(N, N)
    k = 2  # 도트 한 칸 = 2픽셀
    for y, (a, b) in enumerate(ARROW):
        for x in range(a, b):
            img.rect(3 + x * k, 3 + y * k, k, k, fill)
    shade(img, hi, lo)
    border(img)
    if sparkle: star(img, 27 + [0, 3, 6, 3][frame], 10 + [0, 2, 0, -2][frame], frame)
    return img


def aim(fill, hi, lo, core, frame=0, sparkle=False):
    img = Img(N, N)
    c = N / 2
    for y in range(N):
        for x in range(N):
            dx, dy = x + 0.5 - c, y + 0.5 - c
            d = math.hypot(dx, dy)
            if 11.5 <= d <= 14.5:                                  # 두꺼운 고리
                img.set(x, y, hi if dy < -4 else lo if dy > 5 else fill)
            elif (abs(dx) < 1.6 and 5 <= abs(dy) <= 20) or (abs(dy) < 1.6 and 5 <= abs(dx) <= 20):
                img.set(x, y, fill)                                # 끊기지 않는 십자선
            elif d <= 2.6:
                img.set(x, y, core)                                # 가운데 점
    border(img)
    if sparkle:
        a = frame * math.pi / 2 + math.pi / 4
        star(img, int(c + math.cos(a) * 17), int(c + math.sin(a) * 17), frame)
    return img


def shade(img, hi, lo):
    # 밝은 왼쪽 모서리 · 어두운 오른쪽 모서리 (2픽셀)
    for y in range(N):
        xs = [x for x in range(N) if img.get(x, y)[3] > 0]
        if not xs: continue
        for x in xs[:2]: img.set(x, y, hi)
        if len(xs) > 4:
            for x in xs[-2:]: img.set(x, y, lo)


def grow(img, color, cond):
    src = [[img.get(x, y) for x in range(N)] for y in range(N)]
    for y in range(N):
        for x in range(N):
            if src[y][x][3] > 0: continue
            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1), (1, 1), (-1, -1), (1, -1), (-1, 1)):
                xx, yy = x + dx, y + dy
                if 0 <= xx < N and 0 <= yy < N and cond(src[yy][xx]):
                    img.set(x, y, color)
                    break


def border(img):
    # 검은 테두리 2겹 + 바깥에 밝은 테두리 1겹 (어두운 맵 · 밝은 맵 모두에서 또렷하게)
    grow(img, K, lambda c: c[3] > 0)
    grow(img, K, lambda c: c[3] > 0)
    grow(img, (255, 250, 235, 230), lambda c: c[:3] == K[:3] and c[3] > 0)


def star(img, x, y, frame):
    w = (255, 255, 255, 255)
    img.set(x, y, w)
    for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
        if 0 <= x + dx < N and 0 <= y + dy < N: img.set(x + dx, y + dy, (255, 240, 170, 255))
    if frame % 2 == 0:
        for dx, dy in ((2, 0), (-2, 0), (0, 2), (0, -2)):
            if 0 <= x + dx < N and 0 <= y + dy < N: img.set(x + dx, y + dy, (255, 225, 120, 200))


def retry(fn):
    for _ in range(20):
        try:
            return fn()
        except PermissionError:
            time.sleep(0.5)
    return fn()


def save(img, path, meta):
    if not os.path.exists(path + '.meta'):
        m = re.sub(r'guid: [0-9a-f]{32}', 'guid: ' + uuid.uuid4().hex, meta, count=1)
        retry(lambda: open(path + '.meta', 'w', encoding='utf-8', newline='\n').write(m))
    retry(lambda: write(img, path))


def main():
    os.makedirs(CUR, exist_ok=True)
    sprite_meta = open(os.path.join(ICO, 'ability_85.png.meta'), encoding='utf-8').read()
    # 커서 텍스처: 종류 = 커서(7), 스프라이트 아님, 읽기 가능
    cursor_meta = sprite_meta.replace('textureType: 8', 'textureType: 7').replace('spriteMode: 1', 'spriteMode: 0').replace('isReadable: 0', 'isReadable: 1')
    for sid, (fill, hi, lo, core) in SKINS.items():
        frames = 4 if sid in ANIM else 1
        for f in range(frames):
            suffix = '_%d' % f if frames > 1 else ''
            save(arrow(fill, hi, lo, f, sid in ANIM), os.path.join(CUR, '%s_arrow%s.png' % (sid, suffix)), cursor_meta)
            save(aim(fill, hi, lo, core, f, sid in ANIM), os.path.join(CUR, '%s_aim%s.png' % (sid, suffix)), cursor_meta)
        # 상점 미리보기: 조준점 옆에 화살표
        icon = Img(80, 48)
        icon.paste(aim(fill, hi, lo, core, 0, sid in ANIM), 0, 0)
        icon.paste(arrow(fill, hi, lo, 0, sid in ANIM), 40, 4)
        save(icon, os.path.join(ICO, sid + '.png'), sprite_meta)
        print('wrote', sid, frames)


if __name__ == '__main__':
    main()
