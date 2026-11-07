#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""角色动作帧：抠图、切帧、按脚底对齐到同一块方画布，输出到 Assets/ArtCdn（打成 CDN 图包）。

  python3 tools/unit_frames.py            全部
  python3 tools/unit_frames.py units      只做兵
  python3 tools/unit_frames.py heroes     只做好汉（可再跟 id：heroes LiKui WuSong）
  python3 tools/unit_frames.py enemies    只做官兵

兵：pc_<兵种>_<等级>_<帧>.png，等级 1~5，帧 0 站 / 1 蓄力 / 2 出手。原图 raw/units_v2/，提示词 docs/prompt/units_v2_prompt.txt。
好汉：hr_<id小写>_<帧>.png，帧同上。原图 raw/heroes/，清单 docs/prompt/heroes_v1.tsv。
官兵：en_<名>_walk_<0~3>.png。
同一组画布一样大，脚底在同一高度、水平居中，游戏里换帧不跳。
"""

import csv
import json
import sys
from pathlib import Path

import numpy as np
from PIL import Image
from rembg import new_session, remove

ROOT = Path(__file__).resolve().parents[1]
ASSETS = Path("/Users/huyi/dk_proj/game_assets/code_3/assets")
RAW = ASSETS / "raw"
NOBG = ASSETS / "nobg"
FINAL = ASSETS / "final" / "units_v2"
OUT = ROOT / "Assets/ArtCdn"

# 站立帧身子高度（像素，不算举过头的兵器）。等级越高个子越大一点，远看也分得出。骑兵连马再高一截。
FIG_H = 330
LEVEL_SCALE = {1: 1.0, 2: 1.03, 3: 1.06, 4: 1.09, 5: 1.12}
# 骑兵原图的枪长、马铠差别大，量身高会偏；按人马整体宽度校过一遍，保证等级越高越大。
SCALE_FIX = {"pc_qi_2": 0.95, "pc_qi_4": 0.88, "pc_qi_5": 1.05}
# 官兵整张图（连兵器）的高度，和原来那张一样按长边算。
FIG_EN = 340
CANVAS = {"pc": 640, "en": 512, "hr": 640}
BASE = 0.86

# (输出前缀, 原图, 取法, 参数)  grid: 3x3 表里的第 r 行；strip: 一行 n 个
UNITS = [
    ("pc_dao_1", "units_v2_dao.jpg", "grid", 0),
    ("pc_dao_2", "units_v4_dao_l2.jpg", "strip", 3),
    ("pc_dao_3", "units_v4_dao_l3.jpg", "strip", 3),
    ("pc_dao_4", "units_v3_dao_l4.jpg", "strip", 3),
    ("pc_dao_5", "units_v2_dao_t3b.jpg", "strip", 3),
    ("pc_qiang_1", "units_v2_qiang_t1.jpg", "strip", 3),
    ("pc_qiang_2", "units_v4_qiang_l2.jpg", "strip", 3),
    ("pc_qiang_3", "units_v4_qiang_l3.jpg", "strip", 3),
    ("pc_qiang_4", "units_v3_qiang_l4.jpg", "strip", 3),
    ("pc_qiang_5", "units_v2_qiang.jpg", "grid", 2),
    ("pc_gong_1", "units_v2_gong.jpg", "grid", 0),
    ("pc_gong_2", "units_v4_gong_l2.jpg", "strip", 3),
    ("pc_gong_3", "units_v4_gong_l3.jpg", "strip", 3),
    ("pc_gong_4", "units_v3_gong_l4.jpg", "strip", 3),
    ("pc_gong_5", "units_v2_gong_t3.jpg", "strip", 3),
    ("pc_qi_1", "units_v2_qi_t1b.jpg", "strip", 3),
    ("pc_qi_2", "units_v3_qi_l2.jpg", "strip", 3),
    ("pc_qi_3", "units_v2_qi_t2.jpg", "strip", 3),
    ("pc_qi_4", "units_v3_qi_l4.jpg", "strip", 3),
    ("pc_qi_5", "units_v2_qi_t3.jpg", "strip", 3),
]
ENEMIES = ["zu", "zu2", "chai", "jia", "bu", "xianwei"]

_session = None


def nobg(folder, name):
    """整张图抠一次，结果缓存在 nobg/<folder>/。"""
    global _session
    out = NOBG / folder / (Path(name).stem + ".png")
    src_path = RAW / folder / name
    if out.exists() and out.stat().st_mtime >= src_path.stat().st_mtime:
        return Image.open(out).convert("RGBA")
    if _session is None:
        _session = new_session("isnet-anime")
    src = Image.open(src_path).convert("RGB")
    im = remove(src, session=_session, post_process_mask=True)
    a = np.array(im)
    a[..., 3][a[..., 3] < 40] = 0
    im = Image.fromarray(a)
    out.parent.mkdir(parents=True, exist_ok=True)
    im.save(out)
    return im


def columns(alpha, n):
    """按透明竖条把一行切成 n 段；切不准时平分。"""
    occ = (alpha > 0).sum(axis=0) > 2
    runs, start = [], None
    for x, v in enumerate(occ):
        if v and start is None:
            start = x
        elif not v and start is not None:
            runs.append([start, x])
            start = None
    if start is not None:
        runs.append([start, len(occ)])
    runs = [r for r in runs if r[1] - r[0] > 6]
    w = alpha.shape[1]
    # 甩出去的暗器这种小碎块并到最近的人身上，不单算一格。
    while len(runs) > 1:
        small = [i for i, r in enumerate(runs) if r[1] - r[0] < w * 0.07]
        if not small:
            break
        i = small[0]
        j = i - 1 if i == len(runs) - 1 or (i > 0 and runs[i][0] - runs[i - 1][1] < runs[i + 1][0] - runs[i][1]) else i + 1
        a, b = sorted((i, j))
        runs[a] = [runs[a][0], runs[b][1]]
        del runs[b]
    # 相邻两人伸手碰到一起：在最宽那段中间找最细的一列劈开。
    count = (alpha > 0).sum(axis=0)
    while len(runs) < n:
        i = int(np.argmax([r[1] - r[0] for r in runs]))
        x0, x1 = runs[i]
        if x1 - x0 < w / n * 1.4:
            break
        lo, hi = x0 + (x1 - x0) // 4, x1 - (x1 - x0) // 4
        cut = lo + int(np.argmin(count[lo:hi]))
        runs[i:i + 1] = [[x0, cut], [cut, x1]]
    while len(runs) > n:
        gaps = [runs[i + 1][0] - runs[i][1] for i in range(len(runs) - 1)]
        i = int(np.argmin(gaps))
        runs[i] = [runs[i][0], runs[i + 1][1]]
        del runs[i + 1]
    if len(runs) != n:
        w = alpha.shape[1]
        return [(i * w // n, (i + 1) * w // n) for i in range(n)]
    cuts = [0] + [(runs[i][1] + runs[i + 1][0]) // 2 for i in range(n - 1)] + [alpha.shape[1]]
    return [(cuts[i], cuts[i + 1]) for i in range(n)]


def biggest(im, loose=False):
    """格子里只留最大的一团（连同贴着它的武器），去掉旁边格子漏进来的边角。
    loose：好汉甩出去的暗器、套索和身子不连着，不贴格子左右边的小团也留下。"""
    a = np.array(im)
    mask = a[..., 3] > 0
    if not mask.any():
        return im
    from scipy import ndimage

    lab, n = ndimage.label(ndimage.binary_dilation(mask, iterations=6))
    if n > 1:
        sizes = ndimage.sum(mask, lab, range(1, n + 1))
        main = int(np.argmax(sizes)) + 1
        keep = lab == main
        if loose:
            w = mask.shape[1]
            for k in range(1, n + 1):
                if k == main or sizes[k - 1] < sizes[main - 1] * 0.004:
                    continue
                xs = np.where((lab == k).any(axis=0))[0]
                if xs[0] > 2 and xs[-1] < w - 3:
                    keep |= lab == k
        a[..., 3][~keep] = 0
    return Image.fromarray(a)


def frames_of(folder, name, mode, arg, loose=False):
    im = nobg(folder, name)
    w, h = im.size
    if mode == "grid":
        r = arg
        row = im.crop((0, r * h // 3, w, (r + 1) * h // 3))
        cells = [row.crop((c * w // 3, 0, (c + 1) * w // 3, row.size[1])) for c in range(3)]
    else:
        a = np.array(im)[..., 3]
        cells = [im.crop((x0, 0, x1, h)) for x0, x1 in columns(a, arg)]
    out = []
    for c in cells:
        c = biggest(c, loose)
        box = c.getbbox()
        out.append(c.crop(box) if box else c)
    return out


def foot_x(im):
    """脚底那一截的重心，当水平锚点。"""
    a = np.array(im)[..., 3].astype(float)
    h = a.shape[0]
    low = a[int(h * 0.78):]
    xs = np.arange(a.shape[1])
    s = low.sum()
    return float((low.sum(axis=0) * xs).sum() / s) if s > 0 else a.shape[1] / 2


def body_h(im):
    """身子的高度：先腐蚀掉枪杆、弓弦这类细线，再量最大一团。"""
    from scipy import ndimage

    mask = np.array(im)[..., 3] > 0
    core = ndimage.binary_erosion(mask, iterations=9)
    lab, n = ndimage.label(core)
    if n == 0:
        return im.size[1]
    sizes = ndimage.sum(core, lab, range(1, n + 1))
    ys = np.where((lab == int(np.argmax(sizes)) + 1).any(axis=1))[0]
    return int(ys[-1] - ys[0] + 18)


def place(frames, scale, canvas, prefix, report):
    for i, f in enumerate(frames):
        f = f.resize((max(1, round(f.size[0] * scale)), max(1, round(f.size[1] * scale))), Image.Resampling.LANCZOS)
        top = canvas * BASE
        if f.size[1] > top:
            k = top / f.size[1]
            f = f.resize((max(1, round(f.size[0] * k)), round(top)), Image.Resampling.LANCZOS)
            report.append(f"{prefix}_{i} 顶出画布，这一帧缩到 {k:.2f}")
        fx = foot_x(f)
        x = round(canvas / 2 - fx)
        y = round(top - f.size[1])
        if x < 0 or x + f.size[0] > canvas:
            shift = -x if x < 0 else canvas - f.size[0] - x
            x = max(0, min(canvas - f.size[0], x))
            report.append(f"{prefix}_{i} 横向出画布，整帧挪 {shift}px")
        cv = Image.new("RGBA", (canvas, canvas), (0, 0, 0, 0))
        cv.alpha_composite(f, (max(0, x), max(0, y)), (max(0, -x), max(0, -y)))
        name = f"{prefix}_{i}.png"
        FINAL.mkdir(parents=True, exist_ok=True)
        cv.save(FINAL / name, optimize=True)
        cv.save(OUT / name, optimize=True)


def do_units(report, meta):
    for prefix, name, mode, arg in UNITS:
        frames = frames_of("units_v2", name, mode, arg)
        level = int(prefix[-1])
        idle_h = body_h(frames[0])
        target = FIG_H * (1.12 if prefix.startswith("pc_qi_") else 1.0)
        scale = target * LEVEL_SCALE[level] / idle_h * SCALE_FIX.get(prefix, 1.0)
        place(frames, scale, CANVAS["pc"], prefix, report)
        meta[prefix] = round(FIG_H * LEVEL_SCALE[level])
        print(prefix, [f.size for f in frames], f"x{scale:.2f}")


def do_enemies(report, meta):
    for e in ENEMIES:
        frames = frames_of("units_v2", f"en_{e}_walk2.jpg", "strip", 4)
        scale = FIG_EN / float(np.median([f.size[1] for f in frames]))
        place(frames, scale, CANVAS["en"], f"en_{e}_walk", report)
        print(f"en_{e}_walk", [f.size for f in frames], f"x{scale:.2f}")


def hero_ids():
    with open(ROOT / "docs/prompt/heroes_v1.tsv", encoding="utf-8") as f:
        return [r["id"] for r in csv.DictReader(f, delimiter="\t")]


def do_heroes(report, meta, only=None):
    for hid in only or hero_ids():
        key = hid.lower()
        src = RAW / "heroes" / f"hero_{key}.jpg"
        if not src.exists():
            report.append(f"hr_{key} 还没有原图")
            continue
        frames = frames_of("heroes", src.name, "strip", 3, loose=True)
        idle_h = body_h(frames[0])
        scale = FIG_H / idle_h
        place(frames, scale, CANVAS["hr"], f"hr_{key}", report)
        meta[f"hr_{key}"] = FIG_H
        print(f"hr_{key}", [f.size for f in frames], f"x{scale:.2f}")


def main():
    args = sys.argv[1:]
    what = args[0] if args else "all"
    OUT.mkdir(parents=True, exist_ok=True)
    report = []
    meta_path = FINAL / "meta.json"
    meta = json.loads(meta_path.read_text()) if meta_path.exists() else {}
    if what in ("all", "units"):
        do_units(report, meta)
    if what in ("all", "enemies"):
        do_enemies(report, meta)
    if what in ("all", "heroes"):
        do_heroes(report, meta, args[1:] or None)
    print("画布占比", FIG_H / CANVAS["pc"], "脚底", BASE)
    for r in report:
        print("警告", r)
    FINAL.mkdir(parents=True, exist_ok=True)
    meta_path.write_text(json.dumps(meta, ensure_ascii=False, indent=1))
    contact(what)


def contact(what):
    """联系表：每行一组，灰底上画脚底线，看对齐和大小。"""
    if what == "heroes":
        rows = [f"hr_{h.lower()}" for h in hero_ids()]
    else:
        rows = [p for p, *_ in UNITS] + [f"en_{e}_walk" for e in ENEMIES]
    rows = [p for p in rows if (FINAL / f"{p}_0.png").exists()]
    if not rows:
        return
    cell = 160
    per = 4
    cols = 3 if what == "heroes" else 4
    groups = 3 if what == "heroes" else 1
    lines = (len(rows) + groups - 1) // groups
    sheet = Image.new("RGBA", (cell * cols * groups, cell * lines), (205, 196, 176, 255))
    for i, p in enumerate(rows):
        gx, r = (i // lines) * cols * cell, i % lines
        files = sorted(FINAL.glob(f"{p}_*.png"))[:per]
        for c, f in enumerate(files):
            im = Image.open(f).resize((cell, cell), Image.Resampling.LANCZOS)
            sheet.alpha_composite(im, (gx + c * cell, r * cell))
        y = r * cell + int(cell * BASE)
        sheet.paste((120, 60, 40, 255), (gx, y, gx + cols * cell, y + 1))
    out = FINAL / f"contact_{what}.jpg"
    sheet.convert("RGB").save(out, quality=85)
    print("联系表", out)


if __name__ == "__main__":
    main()
