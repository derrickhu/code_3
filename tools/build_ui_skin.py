#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
主界面和各浮层共用的一套皮：黑漆、金边、金菱、宣纸，和启动页的进度槽同一套配色。
输出到 Assets/Resources/Art/skin_*.png，运行时由 Art.Skin 按九宫格切开。

贴图按界面尺寸的 2 倍画（Ui.Skin 里 pixelsPerUnitMultiplier = 2），手机上边线不糊。
另外把启动页的书法标题补好印章，存成 home_logo.png；主界面背景 home_bg.jpg 也从美术仓拷进来。
"""

from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[1]
ART = ROOT / "Assets/Resources/Art"
SRC = ROOT.parent / "game_assets/code_3/assets/final"

GOLD_DARK = np.array([112, 76, 32], float)
GOLD = np.array([206, 160, 84], float)
GOLD_LIGHT = np.array([252, 230, 168], float)
EDGE = np.array([18, 11, 7], float)
LACQUER_TOP = np.array([46, 30, 20], float)
LACQUER_DEEP = np.array([22, 14, 9], float)


def grid(w, h):
    y, x = np.mgrid[0:h, 0:w].astype(float)
    return x + 0.5 - w / 2, y + 0.5 - h / 2


def rrect(w, h, r):
    """圆角矩形的有向距离（里面为负）和朝外法线的竖直分量（朝上为正）。"""
    x, y = grid(w, h)
    qx = np.abs(x) - (w / 2 - r)
    qy = np.abs(y) - (h / 2 - r)
    ox, oy = np.maximum(qx, 0), np.maximum(qy, 0)
    d = np.hypot(ox, oy) + np.minimum(np.maximum(qx, qy), 0) - r
    gy, _ = np.gradient(d)
    return d, -gy


def disc(n, r):
    x, y = grid(n, n)
    d = np.hypot(x, y) - r
    up = -y / np.maximum(np.hypot(x, y), 1e-6)
    return d, up


def band(e, a, b):
    """深度 e 落在 [a, b) 里的覆盖率，两端各留 1px 抗锯齿。"""
    return np.clip(e - a + 0.5, 0, 1) * np.clip(b - e + 0.5, 0, 1)


def gold(e, a, b, up):
    """金边：截面中间偏上最亮、两边压暗，朝上的边整体再提亮。"""
    t = np.clip((e - a) / max(b - a, 1e-6), 0, 1)
    k = np.clip(1 - np.abs(t - 0.38) / 0.62, 0, 1) ** 1.3
    lit = np.clip(0.55 + 0.45 * up, 0, 1)[..., None]
    base = GOLD_DARK + (GOLD - GOLD_DARK) * np.clip(k * 1.6, 0, 1)[..., None]
    hi = np.clip((k - 0.55) / 0.45, 0, 1)[..., None] * lit
    c = base + (GOLD_LIGHT - base) * hi
    return c * (0.78 + 0.22 * lit)


def compose(layers, shape):
    """layers：[(覆盖率, 颜色数组或三元组, 透明度)] 由底到顶叠。"""
    rgb = np.zeros(shape + (3,))
    alpha = np.zeros(shape)
    for cov, col, a in layers:
        col = np.broadcast_to(np.asarray(col, float), shape + (3,)) if np.ndim(col) <= 1 else col
        k = cov * a
        rgb = rgb * (1 - k[..., None]) + col * k[..., None]
        alpha = alpha * (1 - k) + k
    out = np.zeros(shape + (4,))
    safe = np.maximum(alpha, 1e-6)[..., None]
    out[..., :3] = np.where(alpha[..., None] > 1e-6, rgb / safe, 0)
    out[..., 3] = alpha * 255
    return Image.fromarray(np.clip(out, 0, 255).astype(np.uint8))


def lacquer(e, a, b, up):
    t = np.clip((e - a) / max(b - a, 1e-6), 0, 1)[..., None]
    c = LACQUER_TOP + (LACQUER_DEEP - LACQUER_TOP) * t
    sheen = np.clip(up, 0, 1)[..., None] * (1 - t) * 18
    return c + sheen


def diamond(d, cx, cy, r, fill, hole=None):
    d.polygon([(cx, cy - r), (cx + r, cy), (cx, cy + r), (cx - r, cy)], fill=fill)
    if hole:
        r2 = r * 0.42
        d.polygon([(cx, cy - r2), (cx + r2, cy), (cx, cy + r2), (cx - r2, cy)], fill=hole)


def panel():
    """面板外框：黑漆宽边、外金边、内金线，四角各一颗金菱。中间透明，宣纸另铺。"""
    B, R, n = 68, 56, 176
    d, up = rrect(n, n, R)
    e = -d
    im = compose([
        (band(e, 0, B), EDGE, 1),
        (band(e, 4, 15), gold(e, 4, 15, up), 1),
        (band(e, 15, 56), lacquer(e, 15, 56, up), 1),
        (band(e, 56, 62), gold(e, 56, 62, up), 1),
        (band(e, 62, B), EDGE, 0.9),
    ], (n, n))
    dr = ImageDraw.Draw(im)
    for cx in (35, n - 35):
        for cy in (35, n - 35):
            diamond(dr, cx, cy, 11, (222, 186, 112, 255), (30, 19, 12, 255))
    return im, B


def paper():
    """宣纸内衬：近白（运行时按面色染），靠边一圈压暗，带一点纤维纹。"""
    n, B, R = 128, 40, 18
    d, _ = rrect(n, n, R)
    e = -d
    rng = np.random.default_rng(7)
    fib = rng.normal(0, 1, (n, n))
    fib = (fib + np.roll(fib, 1, 1) + np.roll(fib, 2, 1) + np.roll(fib, -1, 0)) / 4
    shade = 0.86 + 0.14 * np.clip(e / 22, 0, 1) ** 0.6
    v = (shade * 255 + fib * 3)[..., None] * np.ones(3)
    return compose([(band(e, 0, 999), v, 1)], (n, n)), B


def button():
    """按钮面：灰度，上亮下暗，顶边一道高光、底边一道暗线；运行时按面色染。"""
    w, h, B, R = 160, 128, 44, 34
    d, up = rrect(w, h, R)
    e = -d
    _, y = grid(w, h)
    t = (y / h + 0.5)
    v = 255 * (1.0 - 0.2 * t)
    hi = np.clip(1 - np.abs(e - 15) / 4, 0, 1) * np.clip(up, 0, 1) * 0.35
    lo = np.clip(1 - np.abs(e - 15) / 5, 0, 1) * np.clip(-up, 0, 1) * 0.2
    v = v * (1 - lo) + 255 * hi
    return compose([(band(e, 0, 999), v[..., None] * np.ones(3), 1)], (h, w)), B


def button_rim():
    """按钮金边：叠在染色的按钮面上，自己不染。"""
    w, h, B, R = 160, 128, 44, 34
    d, up = rrect(w, h, R)
    e = -d
    return compose([
        (band(e, 0, 4), EDGE, 1),
        (band(e, 4, 11), gold(e, 4, 11, up), 1),
        (band(e, 11, 13.5), EDGE, 0.55),
    ], (h, w)), B


def slot():
    """黑漆槽：和启动页进度槽同款，外金边、内一道暗线，顶上压一点内阴影。"""
    w, h, B, R = 128, 96, 46, 46
    d, up = rrect(w, h, R)
    e = -d
    _, y = grid(w, h)
    t = np.clip(y / h + 0.5, 0, 1)[..., None]
    face = np.array([30, 20, 14], float) * (1 - t) + np.array([12, 8, 6], float) * t
    inner = np.clip(1 - (e - 9) / 14, 0, 1) * np.clip(up, 0, 1)
    face = face * (1 - 0.5 * inner[..., None])
    return compose([
        (band(e, 0, 3), EDGE, 1),
        (band(e, 3, 8), gold(e, 3, 8, up), 1),
        (band(e, 8, 9.5), EDGE, 1),
        (band(e, 9.5, 999), face, 0.94),
        (band(e, 10, 11.2), np.array([255, 226, 160], float), 0.14),
    ], (h, w)), B


def medallion(n=256):
    """圆章：外金圈、黑漆章心，内一圈细金线。"""
    R = n / 2 - 1
    d, up = disc(n, R)
    e = -d * 256 / n
    x, y = grid(n, n)
    rr = np.clip(np.hypot(x, y - n * 0.12) / R, 0, 1)[..., None]
    face = np.array([70, 44, 28], float) * (1 - rr) + LACQUER_DEEP * rr
    return compose([
        (band(e, 0, 999), EDGE, 1),
        (band(e, 5, 22), gold(e, 5, 22, up), 1),
        (band(e, 22, 26), EDGE, 1),
        (band(e, 26, 999), face, 1),
        (band(e, 36, 38.5), gold(e, 36, 38.5, up), 0.75),
    ], (n, n))


def close_btn():
    n = 160
    big = medallion(n * 4)
    dr = ImageDraw.Draw(big)
    c, a = n * 2, n * 0.62
    for sx in (-1, 1):
        dr.line([(c - a, c - a * sx), (c + a, c + a * sx)], fill=(18, 11, 7, 255), width=round(n * 0.34))
    for sx in (-1, 1):
        dr.line([(c - a, c - a * sx), (c + a, c + a * sx)], fill=(232, 196, 120, 255), width=round(n * 0.2))
    return big.resize((n, n), Image.Resampling.LANCZOS)


def title_banner():
    """标题牌：横向黑漆牌，两头削尖各镶一颗金菱，上下金边。左右九宫格拉长。"""
    w, h, k = 360, 132, 4
    W, H = w * k, h * k
    im = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    dr = ImageDraw.Draw(im)
    tip = 54 * k

    def hexa(inset):
        i = inset * k
        return [(i * 1.6, H / 2), (tip, i), (W - tip, i), (W - i * 1.6, H / 2), (W - tip, H - i), (tip, H - i)]

    dr.polygon(hexa(0), fill=(18, 11, 7, 255))
    dr.polygon(hexa(4), fill=(214, 172, 96, 255))
    dr.polygon(hexa(6), fill=(140, 98, 44, 255))
    dr.polygon(hexa(10), fill=(18, 11, 7, 255))
    dr.polygon(hexa(12), fill=(44, 28, 19, 255))
    sheen = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    sd = ImageDraw.Draw(sheen)
    for i in range(int(H * 0.4)):
        a = round(22 * (1 - i / (H * 0.4)))
        sd.line([(tip, 12 * k + i), (W - tip, 12 * k + i)], fill=(255, 220, 160, a))
    im.alpha_composite(sheen)
    dr.line([(tip + 6 * k, 22 * k), (W - tip - 6 * k, 22 * k)], fill=(222, 186, 112, 150), width=2 * k)
    dr.line([(tip + 6 * k, H - 22 * k), (W - tip - 6 * k, H - 22 * k)], fill=(222, 186, 112, 150), width=2 * k)
    for cx in (tip * 0.62, W - tip * 0.62):
        diamond(dr, cx, H / 2, 15 * k, (232, 196, 120, 255), (30, 19, 12, 255))
    return im.resize((w, h), Image.Resampling.LANCZOS), 110


def logo():
    from build_loading_art import LOGO, seal
    lg = Image.open(LOGO).convert("RGBA")
    seal(lg)
    w = 1600
    return lg.resize((w, round(lg.height * w / lg.width)), Image.Resampling.LANCZOS)


def home_bg():
    im = Image.open(SRC / "home_bg_v4.png").convert("RGB")
    return im.resize((1080, 1920), Image.Resampling.LANCZOS)


def main():
    out = {
        "skin_panel": panel()[0],
        "skin_paper": paper()[0],
        "skin_btn": button()[0],
        "skin_btn_rim": button_rim()[0],
        "skin_slot": slot()[0],
        "skin_disc": medallion(),
        "skin_close": close_btn(),
        "skin_title": title_banner()[0],
        "home_logo": logo(),
    }
    for name, im in out.items():
        p = ART / f"{name}.png"
        im.save(p, optimize=True)
        print(f"{p.name:20s} {im.size[0]}x{im.size[1]}  {p.stat().st_size // 1024}KB")
    bg = ART / "home_bg.jpg"
    home_bg().save(bg, "JPEG", quality=92, optimize=True, subsampling=0)
    print(f"{bg.name:20s} 1080x1920  {bg.stat().st_size // 1024}KB")


if __name__ == "__main__":
    import sys
    sys.path.insert(0, str(Path(__file__).parent))
    main()
