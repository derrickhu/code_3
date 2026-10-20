#!/usr/bin/env python3
"""
软件著作权登记 - 源程序代码 PDF 生成工具
项目: 营救宋江小游戏软件

生成规则:
  1. A4 纸张, 纵向, 单面
  2. 页眉左侧: 软件全称 + 版本号
  3. 页眉右侧: 阿拉伯数字连续页码
  4. 页脚: 申请人名称
  5. 每页不少于 50 行 (最后一页除外), 纯空白行不计入
  6. 代码不足 60 页全部提交, 超过 60 页取前 30 页 + 后 30 页
"""

import sys
import warnings
from math import ceil
from pathlib import Path

from fpdf import FPDF

warnings.filterwarnings("ignore", category=DeprecationWarning)


SCRIPT_DIR = Path(__file__).resolve().parent
PROJECT_ROOT = Path('/Users/huyi/dk_proj/code_3')
OUTPUT = SCRIPT_DIR / '软著源程序-营救宋江-V1.0.0.pdf'

SOFTWARE_FULL_NAME = '深圳幸运呱科技有限公司营救宋江小游戏软件'
SOFTWARE_VERSION = 'V1.0.0'
APPLICANT_NAME = '深圳幸运呱科技有限公司'

LINES_PER_PAGE = 50
FRONT_PAGES = 30
BACK_PAGES = 30

SONGTI_TTC = '/System/Library/Fonts/Supplemental/Songti.ttc'
SONGTI_FACE = 1  # Songti SC 常规，比默认的黑体子集多「珪」等字

CODE_FONT_SIZE = 9
HEADER_FONT_SIZE = 10.5
FOOTER_FONT_SIZE = 9
LINENO_FONT_SIZE = 7.5

LINE_HEIGHT = 4.6
LEFT_MARGIN = 20
RIGHT_MARGIN = 15
TOP_MARGIN = 15
BOTTOM_MARGIN = 15

HEADER_TEXT = f'{SOFTWARE_FULL_NAME} {SOFTWARE_VERSION} 源程序'
MAX_CODE_CHARS = 100


class SoftCopyrightPDF(FPDF):
    def __init__(self):
        super().__init__(orientation='P', unit='mm', format='A4')
        self.set_auto_page_break(auto=False)

    def header(self):
        self.set_font('Songti', '', HEADER_FONT_SIZE)
        self.set_text_color(0, 0, 0)
        self.set_xy(LEFT_MARGIN, TOP_MARGIN)
        self.cell(0, 6, HEADER_TEXT, new_x='LEFT', new_y='TOP')

        page_str = str(self.page_no())
        tw = self.get_string_width(page_str)
        self.set_xy(210 - RIGHT_MARGIN - tw, TOP_MARGIN)
        self.cell(tw, 6, page_str, new_x='LEFT', new_y='TOP')

        line_y = TOP_MARGIN + 7
        self.set_draw_color(0, 0, 0)
        self.set_line_width(0.4)
        self.line(LEFT_MARGIN, line_y, 210 - RIGHT_MARGIN, line_y)

    def footer(self):
        footer_y = 297 - BOTTOM_MARGIN
        self.set_xy(LEFT_MARGIN, footer_y)
        self.set_font('Songti', '', FOOTER_FONT_SIZE)
        self.set_text_color(0, 0, 0)
        self.cell(210 - LEFT_MARGIN - RIGHT_MARGIN, 5, APPLICANT_NAME, align='C')


def songti_file():
    """fpdf 打开 ttc 只会用第 0 个字重，那个子集缺字。抽出完整宋体再嵌入。"""
    out = Path('/tmp/songti-sc-face1.ttf')
    src = Path(SONGTI_TTC)
    if not out.exists() or out.stat().st_mtime < src.stat().st_mtime:
        from fontTools.ttLib import TTFont
        TTFont(str(src), fontNumber=SONGTI_FACE).save(str(out))
    return str(out)


def collect_source_files(root: Path):
    """运行时源程序。入口在前，编辑器工具和测试不纳入。"""
    files = []
    scripts = root / 'Assets' / 'Scripts'
    first = [
        scripts / 'Game' / 'GameFlow.cs',
        scripts / 'Game' / 'LoadingScreen.cs',
        scripts / 'Game' / 'BattleScreen.cs',
        scripts / 'Game' / 'BattleView.cs',
        scripts / 'Sim' / 'Match.cs',
        scripts / 'Sim' / 'Side.cs',
        scripts / 'Sim' / 'Board.cs',
        scripts / 'Sim' / 'Heroes.cs',
        scripts / 'Sim' / 'Defs.cs',
    ]
    for fp in first:
        if fp.exists():
            files.append(fp)

    if scripts.exists():
        for fp in sorted(scripts.rglob('*.cs')):
            if fp not in files:
                files.append(fp)

    cloud = root / 'cloudfunctions' / 'songjiang-api'
    if cloud.exists():
        for fp in sorted(cloud.rglob('*.js')):
            if 'node_modules' not in fp.parts:
                files.append(fp)
    return files


def read_all_lines(files):
    all_lines = []
    line_no = 1
    for fp in files:
        try:
            text = fp.read_text(encoding='utf-8')
        except UnicodeDecodeError:
            text = fp.read_text(encoding='utf-8-sig')

        rel = fp.relative_to(PROJECT_ROOT) if fp.is_relative_to(PROJECT_ROOT) else fp.name
        all_lines.append((line_no, f'// ===== FILE: {rel} ====='))
        line_no += 1

        for raw_line in text.splitlines():
            clean = raw_line.rstrip('\n\r')
            if clean.strip() == '':
                continue
            all_lines.append((line_no, clean))
            line_no += 1
    return all_lines


def select_lines(all_lines):
    max_lines = (FRONT_PAGES + BACK_PAGES) * LINES_PER_PAGE
    if len(all_lines) <= max_lines:
        return all_lines
    head = all_lines[:FRONT_PAGES * LINES_PER_PAGE]
    tail = all_lines[-BACK_PAGES * LINES_PER_PAGE:]
    return head + tail


def truncate_code(text):
    safe = text.replace('\t', '    ')
    safe = ''.join(c if ord(c) < 0x10000 else '?' for c in safe)
    if len(safe) <= MAX_CODE_CHARS:
        return safe
    return safe[:MAX_CODE_CHARS - 3] + '...'


def generate_pdf(selected_lines):
    total_pages = max(1, ceil(len(selected_lines) / LINES_PER_PAGE))
    pdf = SoftCopyrightPDF()
    pdf.add_font('Songti', '', songti_file())

    code_start_y = TOP_MARGIN + 10
    lineno_col_w = 14

    for page_idx in range(total_pages):
        pdf.add_page()
        start = page_idx * LINES_PER_PAGE
        end = min(start + LINES_PER_PAGE, len(selected_lines))
        page_items = selected_lines[start:end]

        for i, (line_no, code) in enumerate(page_items):
            y = code_start_y + i * LINE_HEIGHT

            pdf.set_font('Songti', '', LINENO_FONT_SIZE)
            pdf.set_text_color(140, 140, 140)
            lineno_str = str(line_no)
            lw = pdf.get_string_width(lineno_str)
            pdf.set_xy(LEFT_MARGIN + lineno_col_w - lw - 1, y)
            pdf.cell(lw, LINE_HEIGHT, lineno_str)

            pdf.set_font('Songti', '', CODE_FONT_SIZE)
            pdf.set_text_color(0, 0, 0)
            pdf.set_xy(LEFT_MARGIN + lineno_col_w + 1, y)
            pdf.cell(0, LINE_HEIGHT, truncate_code(code))

    pdf.output(str(OUTPUT))
    return total_pages


def validate_pdf():
    from pypdf import PdfReader
    reader = PdfReader(str(OUTPUT))
    return len(reader.pages)


def main():
    files = collect_source_files(PROJECT_ROOT)
    if not files:
        print(f'错误: 未找到源码文件, 请检查 PROJECT_ROOT={PROJECT_ROOT}')
        sys.exit(1)

    all_lines = read_all_lines(files)
    selected = select_lines(all_lines)
    total_pages = generate_pdf(selected)
    validated = validate_pdf()

    total_source = len(all_lines)
    total_source_pages = ceil(total_source / LINES_PER_PAGE)

    print('=' * 60)
    print('  营救宋江软著源程序 PDF 生成报告')
    print('=' * 60)
    print(f'  软件名称:     {SOFTWARE_FULL_NAME} {SOFTWARE_VERSION}')
    print(f'  申请人:       {APPLICANT_NAME}')
    print(f'  项目路径:     {PROJECT_ROOT}')
    print(f'  源码文件数:   {len(files)} 个')
    print(f'  源码总行数:   {total_source} 行')
    print(f'  源码总页数:   {total_source_pages} 页')
    print(f'  提取方式:     {"前30页+后30页" if total_source_pages > 60 else "全部提交"}')
    print(f'  选取行数:     {len(selected)} 行')
    print(f'  生成页数:     {total_pages} 页')
    print(f'  PDF验证页数:  {validated} 页')
    print(f'  输出文件:     {OUTPUT}')
    print('=' * 60)
    print(f'  第1行: {selected[0][1][:80] if selected else ""}')
    print(f'  末行:  {selected[-1][1][:80] if selected else ""}')
    print('  生成验证:     ' + ('通过' if validated == total_pages else '页数不一致, 请检查'))


if __name__ == '__main__':
    main()
