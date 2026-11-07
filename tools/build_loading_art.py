#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
启动页合成：主视觉 + 书法标题 + 底部暗场 + 进度槽 + 健康游戏忠告 + 12+ 适龄标，压成一张 1080x1920。
微信封面和 Mac 上的游戏内启动页用的都是这一张，进度条填充和状态字由各自叠在槽里。

图层源文件在美术仓 game_assets/code_3/assets/final/loading_key.png、loading_logo.png。
排版以 720x1280 设计尺寸写（和微信 loadingPageConfig 同一套坐标），合成时乘 1.5。

微信封面用 noBorder 等比铺满：长屏左右各裁掉约 72 设计像素，所以要紧的东西都放在中间 560 宽里。
"""

from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter, ImageFont

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT.parent / "game_assets/code_3/assets/final"
KEY = SRC / "loading_key.png"
LOGO = SRC / "loading_logo.png"
OUT = ROOT / "Assets/Resources/Art/loading.jpg"
BRUSH = ROOT / "Assets/Resources/Fonts/Brush.ttf"
KAI = ROOT / "Assets/Resources/Fonts/Kai.ttf"

# 720x1280 设计坐标（离底边的距离），和 wechat_loading_cover.py / LoadingScreen.cs 共用。
DW, DH = 720, 1280
S = 1.5
W, H = round(DW * S), round(DH * S)
BAR_W, BAR_H, BAR_BOTTOM = 400, 10, 214
TEXT_BOTTOM, TEXT_H = 232, 20
ADVICE_TITLE_Y = 162
ADVICE_BOTTOM, ADVICE_STEP = 36, 26
LOGO_W, LOGO_CY = 560, 300
BADGE_X, BADGE_Y, BADGE_SIZE = 92, 100, 58
# 印章在 loading_logo.png 里的内框（像素）。
SEAL_BOX = (2187, 624, 2288, 735)

GOLD = (222, 186, 112)
GOLD_DEEP = (176, 132, 62)
PAPER = (236, 225, 202)
NIGHT = (20, 13, 9)

ADVICE_TITLE = "健康游戏忠告"
ADVICE = [
    "抵制不良游戏，拒绝盗版游戏。",
    "注意自我保护，谨防受骗上当。",
    "适度游戏益脑，沉迷游戏伤身。",
    "合理安排时间，享受健康生活。",
]


def px(v):
    return round(v * S)


def from_bottom(v):
    return H - px(v)


def text_center(draw, text, face, cx, cy, fill, **kw):
    box = draw.textbbox((0, 0), text, font=face, **{k: v for k, v in kw.items() if k == "stroke_width"})
    tw, th = box[2] - box[0], box[3] - box[1]
    draw.text((cx - tw / 2 - box[0], cy - th / 2 - box[1]), text, font=face, fill=fill, **kw)


def key_art():
    im = Image.open(KEY).convert("RGB")
    sw, sh = im.size
    scale = max(W / sw, H / sh)
    im = im.resize((round(sw * scale), round(sh * scale)), Image.Resampling.LANCZOS)
    x, y = (im.width - W) // 2, (im.height - H) // 2
    return im.crop((x, y, x + W, y + H)).convert("RGBA")


def bottom_shade(base):
    """底部压一层墨色暗场，忠告和进度条在上面读得清；顶端从透明开始，过渡要缓。"""
    shade = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(shade)
    top = from_bottom(470)
    for y in range(top, H):
        t = (y - top) / (H - top)
        a = (t * t * (3 - 2 * t)) ** 0.9 * 0.9
        d.line([(0, y), (W, y)], fill=NIGHT + (round(a * 255),))
    base.alpha_composite(shade)
    vig = Image.new("L", (W, H), 0)
    ImageDraw.Draw(vig).ellipse((-W * 0.25, -H * 0.12, W * 1.25, H * 1.08), fill=255)
    vig = vig.filter(ImageFilter.GaussianBlur(px(70)))
    dark = Image.new("RGBA", (W, H), NIGHT + (255,))
    dark.putalpha(vig.point(lambda v: round((255 - v) * 0.35)))
    base.alpha_composite(dark)


def logo(base):
    lg = Image.open(LOGO).convert("RGBA")
    seal(lg)
    w = px(LOGO_W)
    h = round(lg.height * w / lg.width)
    lg = lg.resize((w, h), Image.Resampling.LANCZOS)
    cx, cy = W // 2, px(LOGO_CY)
    glow = Image.new("L", (W, H), 0)
    ImageDraw.Draw(glow).ellipse((cx - w * 0.62, cy - h * 0.75, cx + w * 0.62, cy + h * 0.75), fill=255)
    glow = glow.filter(ImageFilter.GaussianBlur(px(46)))
    light = Image.new("RGBA", (W, H), (255, 246, 226, 255))
    light.putalpha(glow.point(lambda v: round(v * 0.55)))
    base.alpha_composite(light)
    shadow = Image.new("RGBA", lg.size, NIGHT + (0,))
    shadow.putalpha(lg.getchannel("A").point(lambda v: round(v * 0.45)))
    shadow = shadow.filter(ImageFilter.GaussianBlur(px(6)))
    base.alpha_composite(shadow, (cx - w // 2, cy - h // 2 + px(6)))
    base.alpha_composite(lg, (cx - w // 2, cy - h // 2))


def seal(lg):
    """生图的印章是空的，补上「梁山」两字，竖排白文。"""
    x0, y0, x1, y1 = SEAL_BOX
    d = ImageDraw.Draw(lg)
    size = round((y1 - y0) * 0.44)
    face = ImageFont.truetype(str(BRUSH), size)
    cx = (x0 + x1) / 2
    text_center(d, "梁", face, cx, y0 + (y1 - y0) * 0.27, (250, 236, 214, 255))
    text_center(d, "山", face, cx, y0 + (y1 - y0) * 0.73, (250, 236, 214, 255))


def diamond(d, cx, cy, r, fill):
    d.polygon([(cx, cy - r), (cx + r, cy), (cx, cy + r), (cx - r, cy)], fill=fill)


def bar_slot(base):
    """进度槽：黑漆胶囊、金边，两头一颗金菱。插件的进度条正好落在里面。"""
    layer = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)
    cy = from_bottom(BAR_BOTTOM + BAR_H / 2)
    half_w = px(BAR_W / 2 + 7)
    half_h = px(BAR_H / 2 + 6)
    cx = W // 2
    box = (cx - half_w, cy - half_h, cx + half_w, cy + half_h)
    d.rounded_rectangle(box, radius=half_h, fill=(10, 7, 5, 235), outline=GOLD_DEEP + (255,), width=px(1.6))
    inner = (box[0] + px(2.2), box[1] + px(2.2), box[2] - px(2.2), box[3] - px(2.2))
    d.rounded_rectangle(inner, radius=half_h - px(2.2), outline=(255, 226, 160, 70), width=1)
    for sgn in (-1, 1):
        ex = cx + sgn * (half_w + px(10))
        diamond(d, ex, cy, px(5.5), GOLD + (255,))
        diamond(d, ex, cy, px(2.4), NIGHT + (255,))
        d.line([(ex + sgn * px(7), cy), (ex + sgn * px(30), cy)], fill=GOLD_DEEP + (200,), width=px(1.2))
    base.alpha_composite(layer)


def advice(base):
    d = ImageDraw.Draw(base)
    brush = ImageFont.truetype(str(BRUSH), px(21))
    kai = ImageFont.truetype(str(KAI), px(16.5))
    cy = from_bottom(ADVICE_TITLE_Y)
    text_center(d, ADVICE_TITLE, brush, W / 2, cy, GOLD + (255,))
    box = d.textbbox((0, 0), ADVICE_TITLE, font=brush)
    half = (box[2] - box[0]) / 2 + px(14)
    for sgn in (-1, 1):
        x0 = W / 2 + sgn * half
        x1 = W / 2 + sgn * (half + px(78))
        d.line([(x0, cy), (x1, cy)], fill=GOLD_DEEP + (220,), width=px(1))
        diamond(d, x1 + sgn * px(5), cy, px(3.2), GOLD + (255,))
    n = len(ADVICE)
    for i, line in enumerate(ADVICE):
        y = from_bottom(ADVICE_BOTTOM + ADVICE_STEP * (n - 1 - i) + ADVICE_STEP / 2)
        text_center(d, line, kai, W / 2, y, PAPER + (235,))


def age_badge(base):
    """CADPA 适龄提示：蓝底 12+，下面一行 CADPA，和后台填的适龄一致。"""
    size = px(BADGE_SIZE)
    k = 4
    big = size * k
    im = Image.new("RGBA", (big, big + 6 * k), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    r = round(big * 0.2)
    d.rounded_rectangle((0, 5 * k, big, big + 5 * k), radius=r, fill=(0, 0, 0, 70))
    d.rounded_rectangle((0, 0, big, big), radius=r, fill=(255, 255, 255, 255))
    pad = round(big * 0.06)
    d.rounded_rectangle((pad, pad, big - pad, big - pad), radius=r - pad, fill=(23, 105, 210, 255))
    num = ImageFont.truetype("/System/Library/Fonts/HelveticaNeue.ttc", round(big * 0.46), index=1)
    tag = ImageFont.truetype("/System/Library/Fonts/HelveticaNeue.ttc", round(big * 0.15), index=1)
    text_center(d, "12+", num, big / 2, big * 0.43, (255, 255, 255, 255))
    text_center(d, "CADPA", tag, big / 2, big * 0.78, (255, 255, 255, 235))
    im = im.resize((size, size + 6), Image.Resampling.LANCZOS)
    base.alpha_composite(im, (px(BADGE_X) - size // 2, px(BADGE_Y) - size // 2))


def main():
    for f in (KEY, LOGO, BRUSH, KAI):
        if not f.exists():
            raise SystemExit(f"缺少 {f}")
    im = key_art()
    bottom_shade(im)
    logo(im)
    bar_slot(im)
    advice(im)
    age_badge(im)
    OUT.parent.mkdir(parents=True, exist_ok=True)
    im.convert("RGB").save(OUT, "JPEG", quality=92, optimize=True, subsampling=0)
    print(f"启动页 {OUT}  {W}x{H}  {OUT.stat().st_size // 1024}KB")


if __name__ == "__main__":
    main()
