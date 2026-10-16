"""把 OFL 字体裁成游戏内实际用到的字（扫描 Assets/Scripts 里的中文 + ASCII），输出到 Resources/Fonts。

用法：python3 tools/build_font.py
依赖：fontTools
"""
import pathlib
import re
import shutil

from fontTools import subset
from fontTools.pens.boundsPen import BoundsPen
from fontTools.pens.recordingPen import RecordingPen
from fontTools.pens.transformPen import TransformPen
from fontTools.pens.ttGlyphPen import TTGlyphPen
from fontTools.ttLib import TTFont

ROOT = pathlib.Path(__file__).resolve().parent.parent
SRC = ROOT.parent / "game_assets" / "code_3" / "fonts"
OUT = ROOT / "Assets" / "Resources" / "Fonts"
LICENSES = ROOT / "LICENSES"

FONTS = [
    # (源文件, 输出名, 改名后的家族名；OFL 保留字体名的必须改名)
    ("MaShanZheng-Regular.ttf", "Brush.ttf", None, "OFL-MaShanZheng.txt"),
    ("LXGWWenKai-Medium.ttf", "Kai.ttf", "Shuihu Kai", "OFL-LXGWWenKai.txt"),
]

EXTRA = "，。！？、：；「」『』（）《》—…·★☆↓→←↑×％+-/0123456789"

# 马善政缺的左右结构字：从 donor 取 x 小于 split 的偏旁轮廓，再把 right 整字缩放进 donor 右半部件的框里。
SYNTH = {
    "Brush.ttf": {
        "啰": ("喽", 400, "罗"),
        "玘": ("玩", 400, "己"),
        "珪": ("玩", 400, "圭"),
    },
}


def collect_chars() -> str:
    chars = set(chr(c) for c in range(0x20, 0x7F))
    chars.update(EXTRA)
    for p in (ROOT / "Assets" / "Scripts").rglob("*.cs"):
        text = p.read_text(encoding="utf-8")
        for m in re.finditer(r"[^\x00-\x7F]", text):
            chars.add(m.group(0))
    return "".join(sorted(chars))


def rename(font: TTFont, family: str) -> None:
    ps = family.replace(" ", "")
    for rec in font["name"].names:
        if rec.nameID in (1, 4, 16, 21):
            rec.string = family
        elif rec.nameID in (3, 6, 20):
            rec.string = ps
        elif rec.nameID == 17 or rec.nameID == 22:
            rec.string = "Regular"


def _contours(glyphs, name):
    rec = RecordingPen()
    glyphs[name].draw(rec)
    out, cur = [], []
    for op, args in rec.value:
        cur.append((op, args))
        if op in ("closePath", "endPath"):
            b = BoundsPen(glyphs)
            for o, a in cur:
                getattr(b, o)(*a)
            out.append((cur, b.bounds))
            cur = []
    return out


def _union(boxes):
    return (min(b[0] for b in boxes), min(b[1] for b in boxes), max(b[2] for b in boxes), max(b[3] for b in boxes))


def synthesize(font: TTFont, table: dict) -> str:
    glyphs = font.getGlyphSet()
    cmap = font.getBestCmap()
    made = []
    for ch, (donor, split, right) in table.items():
        if ord(ch) in cmap or ord(donor) not in cmap or ord(right) not in cmap:
            continue
        parts = _contours(glyphs, cmap[ord(donor)])
        left = [c for c, b in parts if b[2] < split]
        box = _union([b for c, b in parts if b[2] >= split])
        rb = BoundsPen(glyphs)
        glyphs[cmap[ord(right)]].draw(rb)
        r = rb.bounds
        sx, sy = (box[2] - box[0]) / (r[2] - r[0]), (box[3] - box[1]) / (r[3] - r[1])
        pen = TTGlyphPen(glyphs)
        for contour in left:
            for op, args in contour:
                getattr(pen, op)(*args)
        glyphs[cmap[ord(right)]].draw(TransformPen(pen, (sx, 0, 0, sy, box[0] - r[0] * sx, box[1] - r[1] * sy)))
        name = f"uni{ord(ch):04X}"
        glyf = font["glyf"]
        glyf[name] = pen.glyph()
        font["hmtx"][name] = font["hmtx"][cmap[ord(donor)]]
        order = font.getGlyphOrder()
        if name not in order:
            font.setGlyphOrder(order + [name])
        glyf.glyphOrder = font.getGlyphOrder()
        for t in font["cmap"].tables:
            if t.isUnicode():
                t.cmap[ord(ch)] = name
        made.append(ch)
    return "".join(made)


def main() -> None:
    OUT.mkdir(parents=True, exist_ok=True)
    LICENSES.mkdir(exist_ok=True)
    text = collect_chars()
    for src, dst, family, lic in FONTS:
        font = TTFont(SRC / src)
        made = synthesize(font, SYNTH.get(dst, {}))
        if made:
            print(f"{dst}: 拼字 {made}")
        cmap = font.getBestCmap()
        missing = [c for c in text if ord(c) not in cmap and not c.isspace()]
        opts = subset.Options()
        opts.layout_features = ["*"]
        opts.name_IDs = ["*"]
        opts.name_languages = ["*"]
        opts.notdef_outline = True
        sub = subset.Subsetter(opts)
        sub.populate(text=text)
        sub.subset(font)
        if family:
            rename(font, family)
        font.save(OUT / dst)
        shutil.copy(SRC / lic, LICENSES / lic)
        size = (OUT / dst).stat().st_size // 1024
        print(f"{dst}: {len(text)} 字, {size} KB, 缺字 {''.join(missing) or '无'}")


if __name__ == "__main__":
    main()
