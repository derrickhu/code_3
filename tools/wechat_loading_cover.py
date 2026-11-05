#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""微信启动封面：和游戏内同一张水墨页。健康忠告印在图上，进度条由插件画成细的一条，不在图上再挖槽。"""

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
BAR_W = 320
BAR_H = 16
BAR_PAD = 0
BAR_BOTTOM = ADVICE_TOP + 36
TEXT_BOTTOM = BAR_BOTTOM + BAR_H + 12
TEXT_H = 24
BADGE = ROOT / "Assets/Resources/Art/age12.png"
BADGE_SIZE = 72
BADGE_XY = (24, 88)

INK = (30, 26, 22)
BAR = "#8E3A30"

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


def system_font(size):
    for path in (
        "/System/Library/Fonts/PingFang.ttc",
        "/System/Library/Fonts/STHeiti Medium.ttc",
        "/Library/Fonts/Arial Unicode.ttf",
    ):
        if Path(path).exists():
            return ImageFont.truetype(path, size)
    return font(ROOT / "Assets/Resources/Fonts/Kai.ttf", size)


def write_badge():
    size = 256
    im = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    draw = ImageDraw.Draw(im)
    draw.rounded_rectangle((14, 18, 246, 250), radius=56, fill=(30, 26, 22, 80))
    draw.rounded_rectangle((8, 8, 244, 244), radius=56, fill=(31, 95, 214, 255))
    draw.rounded_rectangle((24, 24, 228, 228), radius=44, outline=(255, 255, 255, 230), width=8)
    big = system_font(118)
    small = system_font(40)
    box = draw.textbbox((0, 0), "12+", font=big)
    tw, th = box[2] - box[0], box[3] - box[1]
    draw.text(((size - tw) / 2 - box[0], 58 - box[1]), "12+", font=big, fill=(255, 255, 255, 255))
    box = draw.textbbox((0, 0), "适龄", font=small)
    tw = box[2] - box[0]
    draw.text(((size - tw) / 2 - box[0], 168 - box[1]), "适龄", font=small, fill=(255, 255, 255, 255))
    BADGE.parent.mkdir(parents=True, exist_ok=True)
    im.save(BADGE)
    print(f"适龄标 {BADGE}")


def paste_badge(im):
    if not BADGE.exists():
        return im
    badge = Image.open(BADGE).convert("RGBA").resize((BADGE_SIZE, BADGE_SIZE), Image.Resampling.LANCZOS)
    base = im.convert("RGBA")
    base.paste(badge, BADGE_XY, badge)
    return base.convert("RGB")


def compose():
    src = Image.open(SRC).convert("RGB")
    im = src.resize((W, H), Image.Resampling.LANCZOS)
    im = paste_badge(im)
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
                // 720x1280。细进度条停在健康忠告上面，图上不再画槽。
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
                        color: '#5A4C40',
                        fontSize: 16,
                    }},
                }},
                barConfig: {{
                    style: {{
                        width: {BAR_W},
                        height: {BAR_H},
                        padding: {BAR_PAD},
                        bottom: {BAR_BOTTOM},
                        backgroundColor: '{BAR}',
                        borderRadius: 8,
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
    js = js[:start] + block + js[end:]
    js = js.replace("hideAfterCallmain: true", "hideAfterCallmain: false")
    js = js.replace("callmain结束后立即隐藏封面视频", "封面留到主界面画出来再揭")
    GAME_JS.write_text(js, encoding="utf-8")
    print("已改 game.js：一张封面留到主界面出来，细进度条在忠告上方")


TRANSPORT_HOOK = """// songjiang: Unity 的请求走插件，不经过 weapp-adapter。真机关掉 QUIC 和高性能模式，否则排行榜握手失败。
function lockSongjiangTransport() {
    if (typeof wx === 'undefined' || !wx.request || wx.request.__songjiangLocked) return;
    const raw = wx.request.bind(wx);
    function locked(opt) {
        if (opt && typeof opt === 'object') {
            opt.enableQuic = false;
            opt.enableHttp2 = false;
            opt.useHighPerformanceMode = false;
        }
        return raw(opt);
    }
    locked.__songjiangLocked = true;
    wx.request = locked;
}
lockSongjiangTransport();
"""


def patch_request_transport():
    """导出的适配器默认开 QUIC。真机握手失败，开调试才通，排行榜就显示连不上。
    UnityWebRequest 实际走插件，不经过这份适配器，所以 game.js 里还要包一层 wx.request。"""
    path = ROOT / "wechat-minigame/minigame/weapp-adapter.js"
    if path.exists():
        js = path.read_text(encoding="utf-8")
        old = "enableHttp2: true,\n                                enableQuic: true,"
        new = "enableHttp2: false,\n                                enableQuic: false,"
        if old in js:
            path.write_text(js.replace(old, new, 1), encoding="utf-8")
            print("已关 weapp-adapter 的 QUIC/HTTP2")
    game = ROOT / "wechat-minigame/minigame/game.js"
    if not game.exists():
        return
    src = game.read_text(encoding="utf-8")
    if "lockSongjiangTransport" not in src:
        needle = "const managerConfig = {"
        if needle not in src:
            print("game.js 没有 managerConfig，请求钩子没插上")
        else:
            src = src.replace(needle, TRANSPORT_HOOK + needle, 1)
    if "requirePlugin('UnityPlugin'" in src and src.count("lockSongjiangTransport()") < 2:
        src = src.replace("}).default;\n", "}).default;\n            lockSongjiangTransport();\n", 1)
    game.write_text(src, encoding="utf-8")
    print("已让 Unity 的请求关掉 QUIC")


def patch_privacy_check():
    """开发版、体验版不走隐私接口校验。没勾用户信息时，点授权会被微信打成 112。"""
    path = ROOT / "wechat-minigame/minigame/game.json"
    if not path.exists():
        return
    js = path.read_text(encoding="utf-8")
    if "__usePrivacyCheck__" in js:
        return
    js = js.replace("{", '{\n  "__usePrivacyCheck__": false,', 1)
    path.write_text(js, encoding="utf-8")
    print("已关开发版隐私接口校验")


def main():
    if not SRC.exists():
        raise SystemExit(f"缺少 {SRC}")
    if not BADGE.exists():
        write_badge()
    compose()
    patch_game_js()
    patch_request_transport()
    patch_privacy_check()


if __name__ == "__main__":
    main()
