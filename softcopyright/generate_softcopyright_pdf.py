#!/usr/bin/env python3
"""
软件著作权登记 - 源程序鉴别材料 PDF 生成脚本
软件全称：深圳幸运呱科技有限公司营救宋江小游戏软件
版本号：V1.0.0

排版规范：
  1. A4 纵向，页眉左为「软件全称 + 版本号」，页眉右为连续阿拉伯页码 1..60，页脚居中为著作权人名称
  2. 剔除纯空行与仅在开发工具生效的 HomeGm.cs 调试脚本
  3. 超长代码行按版心宽度自动折行，不以省略号截断任何源码字符
  4. 每页固定 52 行（满足不少于 50 行要求），超过 60 页取前 30 页 + 后 30 页，第 60 页末行为完整模块闭合
"""

import sys
import warnings
from math import ceil
from pathlib import Path

from fpdf import FPDF

warnings.filterwarnings("ignore", category=DeprecationWarning)

SCRIPT_DIR = Path(__file__).resolve().parent
PROJECT_ROOT = SCRIPT_DIR.parent
OUTPUT = SCRIPT_DIR / "软著源程序-营救宋江-V1.0.0.pdf"

SOFTWARE_FULL_NAME = "深圳幸运呱科技有限公司营救宋江小游戏软件"
SOFTWARE_VERSION = "V1.0.0"
APPLICANT_NAME = "深圳幸运呱科技有限公司"

LINES_PER_PAGE = 52
FRONT_PAGES = 30
BACK_PAGES = 30

SONGTI_TTC = "/System/Library/Fonts/Supplemental/Songti.ttc"
SONGTI_FACE = 6  # Songti SC Regular

CODE_FONT_SIZE = 8.5
HEADER_FONT_SIZE = 9.5
FOOTER_FONT_SIZE = 9.0
LINENO_FONT_SIZE = 7.2

LINE_HEIGHT = 4.55
LEFT_MARGIN = 18.0
RIGHT_MARGIN = 15.0
TOP_MARGIN = 14.0
BOTTOM_MARGIN = 14.0
LINENO_COL_W = 13.0
CODE_MAX_W = 210.0 - LEFT_MARGIN - RIGHT_MARGIN - LINENO_COL_W - 2.0

HEADER_TEXT = f"{SOFTWARE_FULL_NAME} {SOFTWARE_VERSION}"


def songti_file() -> str:
    out = Path("/tmp/songti-sc-regular.ttf")
    src = Path(SONGTI_TTC)
    if not out.exists() or out.stat().st_mtime < src.stat().st_mtime:
        from fontTools.ttLib import TTFont
        TTFont(str(src), fontNumber=SONGTI_FACE).save(str(out))
    return str(out)


class SourcePDF(FPDF):
    def __init__(self):
        super().__init__(orientation="P", unit="mm", format="A4")
        self.set_auto_page_break(auto=False)

    def header(self):
        self.set_font("Songti", "", HEADER_FONT_SIZE)
        self.set_text_color(0, 0, 0)
        self.set_xy(LEFT_MARGIN, TOP_MARGIN)
        self.cell(0, 5.5, HEADER_TEXT, new_x="LEFT", new_y="TOP")

        page_str = str(self.page_no())
        tw = self.get_string_width(page_str)
        self.set_xy(210.0 - RIGHT_MARGIN - tw, TOP_MARGIN)
        self.cell(tw, 5.5, page_str, new_x="LEFT", new_y="TOP")

        line_y = TOP_MARGIN + 6.5
        self.set_draw_color(40, 40, 40)
        self.set_line_width(0.35)
        self.line(LEFT_MARGIN, line_y, 210.0 - RIGHT_MARGIN, line_y)

    def footer(self):
        footer_y = 297.0 - BOTTOM_MARGIN
        self.set_xy(LEFT_MARGIN, footer_y)
        self.set_font("Songti", "", FOOTER_FONT_SIZE)
        self.set_text_color(40, 40, 40)
        self.cell(210.0 - LEFT_MARGIN - RIGHT_MARGIN, 5.0, APPLICANT_NAME, align="C")


def collect_source_files(root: Path):
    """收集客户端运行时 C# 与云函数 JS，排除仅在开发工具出现的 HomeGm.cs。"""
    scripts = root / "Assets" / "Scripts"
    excluded = {scripts / "Game" / "HomeGm.cs"}

    front_priority = [
        scripts / "Game" / "LoadingScreen.cs",
        scripts / "Game" / "HomeScreen.cs",
        scripts / "Game" / "HomeDaily.cs",
        scripts / "Game" / "HomeShop.cs",
        scripts / "Sim" / "Defs.cs",
        scripts / "Sim" / "Match.cs",
        scripts / "Sim" / "Board.cs",
    ]
    tail_cs = [
        scripts / "Sim" / "Weapons.cs",
        scripts / "Game" / "JoinPanel.cs",
        scripts / "Game" / "ResultPanel.cs",
    ]

    front = [p for p in front_priority if p.exists() and p not in excluded]
    tail = [p for p in tail_cs if p.exists() and p not in excluded]
    reserved = set(front) | set(tail) | excluded

    middle = [p for p in sorted(scripts.rglob("*.cs")) if p not in reserved]

    cloud = root / "cloudfunctions" / "songjiang-api"
    js_order = [
        cloud / "index.js",
        cloud / "lib" / "config.js",
        cloud / "lib" / "http.js",
        cloud / "lib" / "db.js",
        cloud / "lib" / "geo.js",
        cloud / "lib" / "auth.js",
        cloud / "lib" / "live.js",
        cloud / "lib" / "save.js",
    ]
    js_files = [p for p in js_order if p.exists()]
    for p in sorted(cloud.rglob("*.js")):
        if "node_modules" not in p.parts and p not in js_files:
            js_files.append(p)

    return front + middle + tail + js_files


def clean_bmp(text: str) -> str:
    text = text.replace("\t", "    ").replace("◀", "<").replace("▶", ">")
    return "".join(c if ord(c) <= 0xFFFF else "?" for c in text)


def wrap_code_lines(files, pdf_measurer: SourcePDF):
    """返回 (视觉折行后的行列表, 原始非空源码总行数)。"""
    pdf_measurer.set_font("Songti", "", CODE_FONT_SIZE)
    visual_lines = []
    raw_count = 0

    def split_width(s: str):
        if pdf_measurer.get_string_width(s) <= CODE_MAX_W:
            return [s]
        indent = len(s) - len(s.lstrip(" "))
        cont_prefix = " " * min(indent + 4, 16)
        parts = []
        cur = ""
        for ch in s:
            if pdf_measurer.get_string_width(cur + ch) <= CODE_MAX_W:
                cur += ch
            else:
                parts.append(cur)
                cur = cont_prefix + ch
        if cur:
            parts.append(cur)
        return parts

    for fp in files:
        try:
            text = fp.read_text(encoding="utf-8")
        except UnicodeDecodeError:
            text = fp.read_text(encoding="utf-8-sig")

        rel = fp.relative_to(PROJECT_ROOT).as_posix()
        visual_lines.append((None, f"// 文件: {rel}"))

        for raw in text.splitlines():
            line = raw.rstrip("\r\n")
            if not line.strip():
                continue
            raw_count += 1
            safe = clean_bmp(line)
            chunks = split_width(safe)
            for idx, chunk in enumerate(chunks):
                visual_lines.append((raw_count if idx == 0 else None, chunk))

    return visual_lines, raw_count


def select_visual_lines(visual_lines):
    cap = (FRONT_PAGES + BACK_PAGES) * LINES_PER_PAGE
    if len(visual_lines) <= cap:
        return visual_lines
    head = visual_lines[: FRONT_PAGES * LINES_PER_PAGE]
    tail = visual_lines[-BACK_PAGES * LINES_PER_PAGE :]
    return head + tail


def build_pdf(selected):
    total_pages = max(1, ceil(len(selected) / LINES_PER_PAGE))
    pdf = SourcePDF()
    pdf.add_font("Songti", "", songti_file())

    start_y = TOP_MARGIN + 9.5
    seq = 1
    for page_idx in range(total_pages):
        pdf.add_page()
        chunk = selected[page_idx * LINES_PER_PAGE : (page_idx + 1) * LINES_PER_PAGE]
        for row_i, (_, code) in enumerate(chunk):
            y = start_y + row_i * LINE_HEIGHT
            pdf.set_font("Songti", "", LINENO_FONT_SIZE)
            pdf.set_text_color(135, 135, 135)
            num_s = str(seq)
            seq += 1
            lw = pdf.get_string_width(num_s)
            pdf.set_xy(LEFT_MARGIN + LINENO_COL_W - lw - 1.2, y)
            pdf.cell(lw, LINE_HEIGHT, num_s)

            pdf.set_font("Songti", "", CODE_FONT_SIZE)
            pdf.set_text_color(15, 15, 15)
            pdf.set_xy(LEFT_MARGIN + LINENO_COL_W + 0.8, y)
            pdf.cell(CODE_MAX_W, LINE_HEIGHT, code)

    pdf.output(str(OUTPUT))
    return total_pages


def main():
    files = collect_source_files(PROJECT_ROOT)
    if not files:
        print(f"未找到源码文件：{PROJECT_ROOT}")
        sys.exit(1)

    measurer = SourcePDF()
    measurer.add_font("Songti", "", songti_file())
    visual_lines, raw_count = wrap_code_lines(files, measurer)
    selected = select_visual_lines(visual_lines)
    pages = build_pdf(selected)

    from pypdf import PdfReader
    verified_pages = len(PdfReader(str(OUTPUT)).pages)
    total_visual_pages = ceil(len(visual_lines) / LINES_PER_PAGE)

    print("=" * 62)
    print("  营救宋江软著源程序 PDF 生成报告")
    print("=" * 62)
    print(f"  软件全称:       {SOFTWARE_FULL_NAME}")
    print(f"  版本号:         {SOFTWARE_VERSION}")
    print(f"  著作权人:       {APPLICANT_NAME}")
    print(f"  纳入源码文件:   {len(files)} 个")
    print(f"  非空源码行数:   {raw_count} 行（填报「源程序量」用此数）")
    print(f"  折行后总页数:   {total_visual_pages} 页（每页 {LINES_PER_PAGE} 行）")
    print(f"  提交页数:       {verified_pages} 页（前 {FRONT_PAGES} 页 + 后 {BACK_PAGES} 页）")
    print(f"  第 1 页首行:    {selected[0][1]}")
    print(f"  第 30 页末行:   {selected[FRONT_PAGES * LINES_PER_PAGE - 1][1]}")
    print(f"  第 31 页首行:   {selected[FRONT_PAGES * LINES_PER_PAGE][1]}")
    print(f"  第 60 页末行:   {selected[-1][1]}")
    print(f"  输出路径:       {OUTPUT}")
    print("=" * 62)
    if verified_pages != pages:
        sys.exit(1)


if __name__ == "__main__":
    main()
