#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
微信启动封面：直接用 Assets/Resources/Art/loading.jpg（tools/build_loading_art.py 合成，标题、进度槽、忠告、适龄标都在图上）。
插件只在图上的进度槽里画金色填充和一行状态字，坐标和合成脚本同一套 720x1280 设计尺寸。
"""

import shutil
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
from build_loading_art import BAR_BOTTOM, BAR_H, BAR_W, DH, DW, TEXT_BOTTOM, TEXT_H  # noqa: E402

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "Assets/Resources/Art/loading.jpg"
OUT = ROOT / "wechat-minigame/minigame/images/background.jpg"
GAME_JS = ROOT / "wechat-minigame/minigame/game.js"

W, H = DW, DH
BAR_FILL = "#E6B54A"
TEXT_COLOR = "#EBDDBE"


def compose():
    OUT.parent.mkdir(parents=True, exist_ok=True)
    shutil.copyfile(SRC, OUT)
    print(f"封面 {OUT}  {OUT.stat().st_size // 1024}KB")


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
                // 720x1280 等比铺满不拉伸，长屏裁左右。进度条落在图上画好的金边槽里。
                designWidth: {W},
                designHeight: {H},
                scaleMode: scaleMode.noBorder,
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
                        width: {BAR_W},
                        lineHeight: {TEXT_H},
                        color: '{TEXT_COLOR}',
                        fontSize: 13,
                    }},
                }},
                barConfig: {{
                    style: {{
                        width: {BAR_W},
                        height: {BAR_H},
                        padding: 0,
                        bottom: {BAR_BOTTOM},
                        backgroundColor: '{BAR_FILL}',
                        borderRadius: {BAR_H // 2},
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
    print("已改 game.js：封面等比铺满，金色进度条落在图上的槽里，留到主界面出来再揭")


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


def copy_share():
    """分享图不进首包资源，放在代码包 images/ 里，imageUrl 用相对路径。"""
    src_dir = ROOT / "tools/share"
    dst_dir = ROOT / "wechat-minigame/minigame/images"
    dst_dir.mkdir(parents=True, exist_ok=True)
    for name in ("share.jpg", "share_sq.jpg"):
        src = src_dir / name
        if not src.exists():
            raise SystemExit(f"缺少 {src}，先跑 tools/build_share_art.py")
        dst = dst_dir / name
        shutil.copyfile(src, dst)
        print(f"分享图 {dst.name}  {dst.stat().st_size // 1024}KB")


def main():
    if not SRC.exists():
        raise SystemExit(f"缺少 {SRC}，先跑 tools/build_loading_art.py")
    compose()
    copy_share()
    patch_game_js()
    patch_request_transport()
    patch_privacy_check()


if __name__ == "__main__":
    main()
