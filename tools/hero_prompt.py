#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""按 docs/prompt/heroes_v1.tsv 拼一位好汉的出图提示词。

  python3 tools/hero_prompt.py LiKui        打印提示词，第一行是参考图路径
  python3 tools/hero_prompt.py --missing    列出 raw/heroes 里还没有图的好汉
"""

import csv
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
DOC = ROOT / "docs/prompt"
RAW = Path("/Users/huyi/dk_proj/game_assets/code_3/assets/raw/heroes")
ART = ROOT / "Assets/ArtCdn"


def rows():
    with open(DOC / "heroes_v1.tsv", encoding="utf-8") as f:
        return list(csv.DictReader(f, delimiter="\t"))


def poses():
    out = {}
    for line in (DOC / "heroes_v1.txt").read_text(encoding="utf-8").splitlines():
        parts = line.split(None, 1)
        if len(parts) == 2 and "|" in parts[1] and parts[0].isalpha() and parts[0].islower():
            out[parts[0]] = [p.strip() for p in parts[1].split("|")]
    return out


def reference(hid):
    own = ART / f"hr_{hid.lower()}_0.png"
    return own if own.exists() else ART / "hr_shijin_0.png"


def prompt(r):
    p = poses()[r["pose"]]
    w = r["weapon"]
    p = [s.split(" ", 1)[1].replace("{w}", w) for s in p]
    return f"""Game sprite strip: exactly 3 poses of the SAME character side by side in one row, on a plain flat light grey background (#D9D9D9), wide empty space between the poses, poses never touch or overlap.
Character: {r['name_en']} from Water Margin — {r['look']}, wielding {w}.
Style: same as the reference image — cute chibi Water Margin (Song dynasty) hero, big head small body, thick dark brown outline, clean cel shading, bright saturated colors, mobile game sprite. Identical outfit, colors, hair and weapon in all three poses.
All three in 3/4 side view facing RIGHT, full body visible, feet on the same ground line, same scale.
Pose 1 (left): {p[0]}
Pose 2 (middle): {p[1]}
Pose 3 (right): {p[2]}
NO TEXT, no letters, no calligraphy, no labels, no numbers, no ground shadows, no borders, no grid lines."""


def main():
    if sys.argv[1:] == ["--missing"]:
        for r in rows():
            if not (RAW / f"hero_{r['id'].lower()}.jpg").exists():
                print(r["id"])
        return
    by = {r["id"]: r for r in rows()}
    r = by[sys.argv[1]]
    print(reference(r["id"]))
    print(prompt(r))


if __name__ == "__main__":
    main()
