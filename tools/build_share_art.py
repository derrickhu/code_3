#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
微信分享图。场景是生图（不带字），书法标题用启动页那张 home_logo，避免生字写错。

发给朋友的卡片是 5:4，朋友圈是 1:1。导出时 wechat_loading_cover.py 把这两张拷进代码包 images/。
"""

from pathlib import Path

from PIL import Image, ImageFilter

ROOT = Path(__file__).resolve().parents[1]
SCENE = ROOT.parent / "game_assets/code_3/assets/raw/share_scene.jpg"
LOGO = ROOT / "Assets/Resources/Art/home_logo.png"
FINAL = ROOT.parent / "game_assets/code_3/assets/final"
OUT = ROOT / "tools/share"


def extend(strip, size, blur):
    im = strip.resize(size, Image.Resampling.BILINEAR)
    return im.filter(ImageFilter.GaussianBlur(radius=blur))


def place(scene, cw, ch, top):
    """场景按宽铺满、顶在 top。上面用天空拉齐，下面用地面拉齐。"""
    scale = cw / scene.width
    nh = round(scene.height * scale)
    scaled = scene.resize((cw, nh), Image.Resampling.LANCZOS)
    sky_c = scaled.getpixel((cw // 2, 4))
    canvas = Image.new("RGB", (cw, ch), sky_c)
    if top > 0:
        canvas.paste(extend(scaled.crop((0, 0, cw, 28)), (cw, top + 28), 10), (0, 0))
    visible = min(nh, ch - top)
    canvas.paste(scaled.crop((0, 0, cw, visible)), (0, top))
    bottom = top + visible
    if bottom < ch:
        canvas.paste(extend(scaled.crop((0, nh - 20, cw, nh)), (cw, ch - bottom + 8), 4), (0, bottom - 8))
    return canvas.convert("RGBA")


def stamp(canvas, logo, width, top):
    h = round(logo.height * width / logo.width)
    lg = logo.resize((width, h), Image.Resampling.LANCZOS)
    canvas.alpha_composite(lg, ((canvas.width - width) // 2, top))
    return h


def save(im, path):
    path.parent.mkdir(parents=True, exist_ok=True)
    im.convert("RGB").save(path, "JPEG", quality=86, optimize=True, subsampling=0)
    print(f"{path.name:16s} {im.width}x{im.height}  {path.stat().st_size // 1024}KB")


def main():
    scene = Image.open(SCENE).convert("RGB")
    logo = Image.open(LOGO).convert("RGBA")

    # 5:4。场景贴底，标题落在头顶那条空天上，脚下不另接模糊地面。
    friend_h = round(scene.height * 1000 / scene.width)
    friend = place(scene, 1000, 800, 800 - friend_h)
    stamp(friend, logo, 740, 6)

    # 1:1。同样贴底，人完整，多出来的是天空，标题叠在空天上。
    moment_h = round(scene.height * 800 / scene.width)
    moment = place(scene, 800, 800, 800 - moment_h)
    stamp(moment, logo, 700, 12)

    save(friend, FINAL / "share_5x4.jpg")
    save(moment, FINAL / "share_1x1.jpg")
    save(friend, OUT / "share.jpg")
    save(moment, OUT / "share_sq.jpg")


if __name__ == "__main__":
    main()
