#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""把启动封面换成和游戏内同一张水墨页：营救宋江，底下印健康游戏忠告，进度条让在忠告上面。"""

from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "Assets/Resources/Art/loading.jpg"
OUT = ROOT / "wechat-minigame/minigame/images/background.jpg"
GAME_JS = ROOT / "wechat-minigame/minigame/game.js"

W, H = 720, 1280
# 游戏里忠告贴在 1080x1920 画布底边。封面按同样的比例印，进度条放在标题上方的留白里。
SY = H / 1920
TITLE_BOTTOM = round(248 * SY)
TITLE_H = round(52 * SY)
BODY_BOTTOM = round(52 * SY)
BODY_H = round(176 * SY)
TITLE_PX = round(36 * SY)
BODY_PX = round(30 * SY)
ADVICE_TOP = TITLE_BOTTOM + TITLE_H
BAR_BOTTOM = ADVICE_TOP + 18
BAR_H = 8
TEXT_BOTTOM = BAR_BOTTOM + BAR_H + 8
TEXT_H = 22

INK = (30, 26, 22)
INK_SOFT = (74, 66, 56)
BAR = "#8C3A32"

TITLE = "健康游戏忠告"
LINES = [
    "抵制不良游戏，拒绝盗版游戏。",
    "注意自我保护，谨防受骗上当。",
    "适度游戏益脑，沉迷游戏伤身。",
    "合理安排时间，享受健康生活。",
]


def font(path, size):
    return ImageFont.truetype(str(path), size)


def draw_center(draw, text, face, bottom, height, fill):
    box = draw.textbbox((0, 0), text, font=face)
    tw, th = box[2] - box[0], box[3] - box[1]
    x = (W - tw) / 2 - box[0]
    band_top = H - bottom - height
    y = band_top + (height - th) / 2 - box[1]
    draw.text((x, y), text, font=face, fill=fill)


def compose():
    src = Image.open(SRC).convert("RGB")
    im = src.resize((W, H), Image.Resampling.LANCZOS)
    draw = ImageDraw.Draw(im)
    brush = font(ROOT / "Assets/Resources/Fonts/Brush.ttf", TITLE_PX)
    kai = font(ROOT / "Assets/Resources/Fonts/Kai.ttf", BODY_PX)
    draw_center(draw, TITLE, brush, TITLE_BOTTOM, TITLE_H, INK)
    step = BODY_PX * 1.08
    band_top = H - BODY_BOTTOM - BODY_H
    y = band_top + 2
    for line in LINES:
        box = draw.textbbox((0, 0), line, font=kai)
        tw = box[2] - box[0]
        x = (W - tw) / 2 - box[0]
        draw.text((x, y - box[1]), line, font=kai, fill=INK)
        y += step
    OUT.parent.mkdir(parents=True, exist_ok=True)
    im.save(OUT, "JPEG", quality=86, optimize=True)
    print(f"封面 {OUT}  {OUT.stat().st_size // 1024}KB  忠告顶边距底 {ADVICE_TOP}px")


def patch_game_js():
    if not GAME_JS.exists():
        print("没有 game.js，跳过加载页配置")
        return
    js = GAME_JS.read_text(encoding="utf-8")
    start = js.find("loadingPageConfig:")
    if start < 0:
        raise SystemExit("game.js 里没有 loadingPageConfig")
    brace = js.find("{", start)
    depth = 0
    end = None
    for i in range(brace, len(js)):
        if js[i] == "{":
            depth += 1
        elif js[i] == "}":
            depth -= 1
            if depth == 0:
                end = i + 1
                break
    if end is None:
        raise SystemExit("loadingPageConfig 括号不配对")
    block = f"""loadingPageConfig: {{
                totalLaunchTime: 7000,
                animationDuration: 100,
                // 720x1280，和游戏内启动页同一张图。进度条停在健康忠告上面。
                designWidth: {W},
                designHeight: {H},
                scaleMode: scaleMode.exactFit,
                textConfig: {{
                    firstStartText: '首次加载请耐心等待',
                    downloadingText: ['正在加载资源'],
                    compilingText: '编译中',
                    initText: '初始化中',
                    completeText: '开始游戏',
                    textDuration: 1500,
                    style: {{
                        bottom: {TEXT_BOTTOM},
                        height: {TEXT_H},
                        width: 360,
                        lineHeight: {TEXT_H},
                        color: '#4A4238',
                        fontSize: 15,
                    }},
                }},
                barConfig: {{
                    style: {{
                        width: 280,
                        height: {BAR_H},
                        padding: 1,
                        bottom: {BAR_BOTTOM},
                        backgroundColor: '{BAR}',
                    }},
                }},
                iconConfig: {{
                    visible: false,
                    style: {{
                        width: 64,
                        height: 23,
                        bottom: 20,
                    }},
                }},
                materialConfig: {{
                    backgroundImage: 'images/background.jpg',
                    backgroundVideo: '',
                    iconImage: 'images/unity_logo.png',
                }},
            }}"""
    GAME_JS.write_text(js[:start] + block + js[end:], encoding="utf-8")
    print("已改 game.js：封面与健康忠告同一页，进度条在忠告上方")


def main():
    if not SRC.exists():
        raise SystemExit(f"缺少 {SRC}")
    compose()
    patch_game_js()


if __name__ == "__main__":
    main()
