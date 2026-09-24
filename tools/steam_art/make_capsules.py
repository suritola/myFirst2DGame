# Steam 스토어 그래픽 에셋(캡슐·라이브러리 이미지)을 게임 안 스프라이트로 만든다.
#
# 사용법: python3 tools/steam_art/make_capsules.py --title "Six Feet Under Fire"
#         python3 tools/steam_art/make_capsules.py --title "Gravegun" --out steam_assets
# 필요: pip install pillow
import argparse
import os

from PIL import Image, ImageDraw, ImageEnhance, ImageFilter, ImageFont

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
SPR = os.path.join(ROOT, "Assets", "Sprites")
FONT = os.path.join(os.path.dirname(__file__), "fonts", "PixelifySans.ttf")
CELL = 32  # 스프라이트 시트 한 칸 크기

# (파일, 줄, 칸) — 줄 0: 대기, 4: 사격
PLAYER = ("Players/players blue x1.png", 4, 0)
BOSSES = [
    ("enemy/meadow/meadow_slimeking.png", 0, 0),
    ("enemy/hell/hell_lord.png", 0, 0),
    ("enemy/boss.png", 0, 0),  # 리치 왕 (맨 앞)
]

# 이름: (가로, 세로, 제목 넣기)
SIZES = {
    "header_capsule": (920, 430, True),
    "small_capsule": (462, 174, True),
    "main_capsule": (1232, 706, True),
    "vertical_capsule": (748, 896, True),
    "library_capsule": (600, 900, True),
    "library_header": (920, 430, True),
    "library_hero": (3840, 1240, False),
    "page_background": (1438, 810, False),
}


def frame(path, row, col):
    sheet = Image.open(os.path.join(SPR, path)).convert("RGBA")
    return sheet.crop((col * CELL, row * CELL, (col + 1) * CELL, (row + 1) * CELL))


def pixel_scale(img, scale):
    return img.resize((img.width * scale, img.height * scale), Image.NEAREST)


def cover(img, w, h, focus_y=0.35):
    # 비율을 유지하며 w×h를 꽉 채우고 넘치는 부분을 자름
    s = max(w / img.width, h / img.height)
    img = img.resize((round(img.width * s), round(img.height * s)), Image.NEAREST)
    x = (img.width - w) // 2
    y = int((img.height - h) * focus_y)
    return img.crop((x, y, x + w, y + h))


def background(w, h):
    crypt = Image.open(os.path.join(SPR, "UI", "Theme", "menu_bg.png")).convert("RGBA")
    bg = cover(crypt, w, h)
    bg = ImageEnhance.Brightness(bg).enhance(0.55)

    # 아래쪽에서 올라오는 지옥불 빛, 가장자리 비네트
    glow = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    g = ImageDraw.Draw(glow)
    for i in range(h):
        t = i / h
        g.line([(0, i), (w, i)], fill=(255, 80, 20, int(110 * t ** 2.2)))
    bg = Image.alpha_composite(bg, glow)

    vig = Image.new("L", (w, h), 0)
    ImageDraw.Draw(vig).ellipse((-w * 0.15, -h * 0.25, w * 1.15, h * 1.25), fill=255)
    vig = vig.filter(ImageFilter.GaussianBlur(min(w, h) * 0.12))
    dark = Image.new("RGBA", (w, h), (8, 4, 12, 255))
    return Image.composite(bg, dark, vig)


def shadow(sprite, blur):
    a = sprite.split()[3]
    sh = Image.new("RGBA", sprite.size, (0, 0, 0, 0))
    sh.putalpha(a.point(lambda v: 160 if v else 0))
    return sh.filter(ImageFilter.GaussianBlur(blur))


def paste(canvas, sprite, x, y):
    canvas.alpha_composite(shadow(sprite, max(2, sprite.width // 40)), (x + sprite.width // 30, y + sprite.width // 30))
    canvas.alpha_composite(sprite, (x, y))


def characters(canvas, w, h, has_title):
    tall = h > w
    # 제목이 있으면 캐릭터를 아래쪽으로 몰아서 글자와 겹치지 않게 함
    unit = h * (0.42 if tall else 0.62) if has_title else h * 0.8
    scale = max(1, int(unit / CELL))
    base_y = h - int(scale * CELL * (0.93 if has_title else 1.05))

    player = pixel_scale(frame(*PLAYER), scale)
    boss_scale = max(1, int(scale * 0.8))
    bosses = [pixel_scale(frame(*b), boss_scale).transpose(Image.FLIP_LEFT_RIGHT) for b in BOSSES]

    if tall:
        px = int(w * 0.02)
        paste(canvas, player, px, base_y)
        bx = [int(w * 0.62), int(w * 0.42), int(w * 0.52)]
        by = [base_y - int(boss_scale * CELL * 0.55), base_y - int(boss_scale * CELL * 0.35), base_y + int(scale * 4)]
    else:
        px = int(w * (0.06 if has_title else 0.18))
        paste(canvas, player, px, base_y)
        right = w - int(w * (0.04 if has_title else 0.16))
        step = int(boss_scale * CELL * 0.62)
        bx = [right - boss_scale * CELL - step * 2, right - boss_scale * CELL - step, right - boss_scale * CELL]
        by = [base_y - int(boss_scale * 6), base_y - int(boss_scale * 3), base_y + int(scale * 2)]
    for s, x, y in zip(bosses, bx, by):
        paste(canvas, s, x, y)


def fit_font(text, max_w, max_h):
    size = int(max_h)
    while size > 8:
        f = ImageFont.truetype(FONT, size)
        f.set_variation_by_name("Bold")
        box = f.getbbox(text)
        if box[2] - box[0] <= max_w and box[3] - box[1] <= max_h:
            return f
        size -= 2
    return ImageFont.truetype(FONT, 8)


def split_title(title, tall):
    words = title.split()
    if len(words) < 2 or (not tall and len(title) <= 14):
        return [title]
    # 두 줄로 나눌 때 길이가 가장 비슷해지는 지점
    best = min(range(1, len(words)), key=lambda i: abs(len(" ".join(words[:i])) - len(" ".join(words[i:]))))
    return [" ".join(words[:best]), " ".join(words[best:])]


def draw_title(canvas, title, w, h, top, height, one_line=False):
    tall = h > w
    lines = [title] if one_line else split_title(title, tall or len(title) > 14)
    line_h = height / len(lines)
    d = ImageDraw.Draw(canvas)
    font = fit_font(max(lines, key=len), w * 0.9, line_h * 0.82)
    outline = max(2, font.size // 12)
    y = top
    for line in lines:
        box = d.textbbox((0, 0), line, font=font)
        x = (w - (box[2] - box[0])) // 2 - box[0]
        ty = int(y + (line_h - (box[3] - box[1])) / 2 - box[1])
        d.text((x + outline, ty + outline * 2), line, font=font, fill=(0, 0, 0, 200),
               stroke_width=outline, stroke_fill=(0, 0, 0, 200))
        d.text((x, ty), line, font=font, fill=(255, 214, 120), stroke_width=outline, stroke_fill=(70, 12, 8))
        y += line_h


def make(name, w, h, has_title, title):
    canvas = background(w, h)
    if h < 250:
        # 작은 캡슐은 제목이 읽히는 게 가장 중요해서 글자만 크게 넣음
        draw_title(canvas, title, w, h, h * 0.08, h * 0.84, one_line=True)
        return canvas.convert("RGB")
    characters(canvas, w, h, has_title)
    if has_title:
        if h > w:
            draw_title(canvas, title, w, h, h * 0.05, h * 0.34)
        else:
            draw_title(canvas, title, w, h, h * 0.04, h * 0.36)
    return canvas.convert("RGB")


def make_logo(title, w=1280, h=720):
    logo = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    draw_title(logo, title, w, h, h * 0.1, h * 0.8)
    return logo.crop(logo.getbbox())


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--title", required=True)
    ap.add_argument("--out", default=os.path.join(ROOT, "steam_assets"))
    args = ap.parse_args()
    os.makedirs(args.out, exist_ok=True)

    for name, (w, h, has_title) in SIZES.items():
        make(name, w, h, has_title, args.title).save(os.path.join(args.out, f"{name}_{w}x{h}.png"))
    make_logo(args.title).save(os.path.join(args.out, "library_logo.png"))
    print("저장 위치:", args.out)


if __name__ == "__main__":
    main()
