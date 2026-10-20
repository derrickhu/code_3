#!/usr/bin/env python3
"""
软件著作权登记 - 文档鉴别材料（设计说明书）PDF 生成工具
项目: 营救宋江小游戏软件

截图若未准备，会在 PDF 中生成“待补截图”占位框，并在运行报告中输出缺图清单。
"""

import warnings
from pathlib import Path

from fpdf import FPDF
from fpdf.enums import WrapMode
from PIL import Image

warnings.filterwarnings("ignore", category=DeprecationWarning)


SCRIPT_DIR = Path(__file__).resolve().parent
PROJECT_ROOT = Path('/Users/huyi/dk_proj/code_3')
OUTPUT = SCRIPT_DIR / '软著文档-营救宋江-V1.0.0.pdf'

SOFTWARE_FULL_NAME = '深圳幸运呱科技有限公司营救宋江小游戏软件'
SOFTWARE_VERSION = 'V1.0.0'
APPLICANT_NAME = '深圳幸运呱科技有限公司'

SONGTI_TTC = '/System/Library/Fonts/Supplemental/Songti.ttc'
SONGTI_FACE = 1

BODY_FONT_SIZE = 10.5
H1_FONT_SIZE = 16
H2_FONT_SIZE = 14
H3_FONT_SIZE = 12
CODE_FONT_SIZE = 9
HEADER_FONT_SIZE = 10
FOOTER_FONT_SIZE = 9

LINE_HEIGHT = 6.5
CODE_LINE_HEIGHT = 5.0
H1_LINE_HEIGHT = 10
H2_LINE_HEIGHT = 8.5
H3_LINE_HEIGHT = 7.5

LEFT_MARGIN = 25
RIGHT_MARGIN = 20
TOP_MARGIN = 15
BOTTOM_MARGIN = 15

PAGE_W = 210
PAGE_H = 297
CONTENT_W = PAGE_W - LEFT_MARGIN - RIGHT_MARGIN
CONTENT_TOP = TOP_MARGIN + 10

HEADER_TEXT = f'{SOFTWARE_FULL_NAME} {SOFTWARE_VERSION} 设计说明书'
PICS_DIR = SCRIPT_DIR / 'pics'


def songti_file():
    out = Path('/tmp/songti-sc-face1.ttf')
    src = Path(SONGTI_TTC)
    if not out.exists() or out.stat().st_mtime < src.stat().st_mtime:
        from fontTools.ttLib import TTFont
        TTFont(str(src), fontNumber=SONGTI_FACE).save(str(out))
    return str(out)


def _pic(*names):
    for name in names:
        path = PICS_DIR / name
        if path.exists():
            return path
    return PICS_DIR / names[0]


SCREENSHOTS = {
    'loading': [(_pic('songjiang_01_loading.png', 'songjiang_01_loading.jpg'),
                 '图1  加载页 - 游戏名称、进度与健康游戏忠告')],
    'home': [(_pic('songjiang_02_home.png', 'songjiang_02_home.jpg'),
              '图2  山寨主界面 - 座次、铜钱、体力、出征与功能入口')],
    'story': [(_pic('songjiang_03_story.png', 'songjiang_03_story.jpg'),
               '图3  回目界面 - 章节名、剧情与出征')],
    'scout': [(_pic('songjiang_04_scout.png', 'songjiang_04_scout.jpg'),
               '图4  备战界面 - 官军探马、双方棋盘与宋江')],
    'battle': [(_pic('songjiang_05_battle.png', 'songjiang_05_battle.jpg'),
                '图5  对局界面 - 棋盘、山寨、聚义与双方宋江')],
    'range': [(_pic('songjiang_06_range.png', 'songjiang_06_range.jpg'),
               '图6  攻击范围 - 拖动兵种时的射程圈')],
    'hero': [(_pic('songjiang_07_hero.png', 'songjiang_07_hero.jpg'),
              '图7  凑成好汉 - 名字金框与绰号')],
    'attack': [(_pic('songjiang_08_attack.png', 'songjiang_08_attack.jpg'),
                '图8  好汉普攻 - 兵器从名字挥向敌人')],
    'item_drag': [(_pic('songjiang_09_item_drag.png', 'songjiang_09_item_drag.jpg'),
                   '图9  主动道具 - 拖上战场后松手放出')],
    'item_tip': [(_pic('songjiang_10_item_tip.png', 'songjiang_10_item_tip.jpg'),
                  '图10  道具说明 - 气泡不挡住道具一排')],
    'shop': [(_pic('songjiang_11_shop.png', 'songjiang_11_shop.jpg'),
              '图11  十字坡酒店 - 当日货架与购买')],
    'lottery': [(_pic('songjiang_12_lottery.png', 'songjiang_12_lottery.jpg'),
                 '图12  抽奖界面 - 九宫格与奖品')],
    'codex': [(_pic('songjiang_13_codex.png', 'songjiang_13_codex.jpg'),
               '图13  好汉谱 - 一百单八将与羁绊')],
    'weapon': [(_pic('songjiang_14_weapon.png', 'songjiang_14_weapon.jpg'),
                '图14  兵器界面 - 品级、特性与装备')],
    'result': [(_pic('songjiang_15_result.png', 'songjiang_15_result.jpg'),
                '图15  结算界面 - 胜负、波次、斩敌与及时雨')],
}


class DocPDF(FPDF):
    def __init__(self):
        super().__init__(orientation='P', unit='mm', format='A4')
        self.set_left_margin(LEFT_MARGIN)
        self.set_right_margin(RIGHT_MARGIN)
        self.set_top_margin(CONTENT_TOP)
        self.set_auto_page_break(auto=True, margin=BOTTOM_MARGIN + 10)
        self.missing_images = []

    def header(self):
        self.set_font('Songti', '', HEADER_FONT_SIZE)
        self.set_text_color(0, 0, 0)
        self.set_xy(LEFT_MARGIN, TOP_MARGIN)
        self.cell(0, 6, HEADER_TEXT, new_x='LEFT', new_y='TOP')
        page_str = str(self.page_no())
        tw = self.get_string_width(page_str)
        self.set_xy(PAGE_W - RIGHT_MARGIN - tw, TOP_MARGIN)
        self.cell(tw, 6, page_str, new_x='LEFT', new_y='TOP')
        line_y = TOP_MARGIN + 7
        self.set_draw_color(0, 0, 0)
        self.set_line_width(0.4)
        self.line(LEFT_MARGIN, line_y, PAGE_W - RIGHT_MARGIN, line_y)
        self.set_y(CONTENT_TOP)

    def footer(self):
        footer_y = PAGE_H - BOTTOM_MARGIN
        self.set_xy(LEFT_MARGIN, footer_y)
        self.set_font('Songti', '', FOOTER_FONT_SIZE)
        self.set_text_color(0, 0, 0)
        self.cell(CONTENT_W, 5, APPLICANT_NAME, align='C')

    def check_page_break(self, h):
        if self.get_y() + h > PAGE_H - BOTTOM_MARGIN - 10:
            self.add_page()

    def write_h1(self, text):
        self.check_page_break(H1_LINE_HEIGHT + 5)
        self.ln(4)
        self.set_font('Songti', '', H1_FONT_SIZE)
        self.set_text_color(0, 0, 0)
        self.set_x(LEFT_MARGIN)
        self.cell(CONTENT_W, H1_LINE_HEIGHT, _safe_text(text), new_x='LMARGIN', new_y='NEXT')
        self.ln(2)

    def write_h2(self, text):
        self.check_page_break(H2_LINE_HEIGHT + 4)
        self.ln(3)
        self.set_font('Songti', '', H2_FONT_SIZE)
        self.set_text_color(0, 0, 0)
        self.set_x(LEFT_MARGIN)
        self.cell(CONTENT_W, H2_LINE_HEIGHT, _safe_text(text), new_x='LMARGIN', new_y='NEXT')
        self.ln(1.5)

    def write_h3(self, text):
        self.check_page_break(H3_LINE_HEIGHT + 3)
        self.ln(2)
        self.set_font('Songti', '', H3_FONT_SIZE)
        self.set_text_color(0, 0, 0)
        self.set_x(LEFT_MARGIN)
        self.cell(CONTENT_W, H3_LINE_HEIGHT, _safe_text(text), new_x='LMARGIN', new_y='NEXT')
        self.ln(1)

    def write_body(self, text, indent=0):
        self.set_font('Songti', '', BODY_FONT_SIZE)
        self.set_text_color(30, 30, 30)
        self.set_x(LEFT_MARGIN + indent)
        self.multi_cell(CONTENT_W - indent, LINE_HEIGHT, _safe_text(text),
                        new_x='LMARGIN', new_y='NEXT', wrapmode=WrapMode.CHAR)

    def write_bullet(self, text, level=0):
        indent = 4 + level * 4
        bullet = '  ' * level + ('- ' if level > 0 else '* ')
        self.write_body(bullet + text, indent=indent)

    def write_code_block(self, lines):
        self.ln(1)
        self.set_font('Songti', '', CODE_FONT_SIZE)
        self.set_text_color(40, 40, 40)
        for line in lines:
            self.check_page_break(CODE_LINE_HEIGHT)
            self.set_fill_color(245, 245, 245)
            self.set_x(LEFT_MARGIN + 4)
            self.cell(CONTENT_W - 4, CODE_LINE_HEIGHT, _safe_text(line.replace('\t', '    ')),
                      fill=True, new_x='LMARGIN', new_y='NEXT')
        self.ln(1)

    def write_table(self, headers, rows, col_widths=None):
        self.ln(1)
        if col_widths is None:
            col_widths = [CONTENT_W / len(headers)] * len(headers)
        row_line_h = 5.6
        pad_x = 1.5
        pad_y = 1.5

        def wrap_cell(text, width):
            text = _safe_text(str(text))
            lines = []
            for paragraph in text.split('\n'):
                current = ''
                for ch in paragraph:
                    if self.get_string_width(current + ch) <= width:
                        current += ch
                    else:
                        if current:
                            lines.append(current)
                        current = ch
                lines.append(current)
            return lines or ['']

        def draw_row(cells, fill):
            self.set_font('Songti', '', BODY_FONT_SIZE)
            wrapped = [wrap_cell(c, col_widths[i] - pad_x * 2) for i, c in enumerate(cells)]
            row_h = max(len(lines) for lines in wrapped) * row_line_h + pad_y * 2
            self.check_page_break(row_h)
            y0 = self.get_y()
            x = LEFT_MARGIN
            self.set_fill_color(*fill)
            self.set_draw_color(0, 0, 0)
            for i, lines in enumerate(wrapped):
                self.rect(x, y0, col_widths[i], row_h, style='DF')
                self.set_xy(x + pad_x, y0 + pad_y)
                for line in lines:
                    self.cell(col_widths[i] - pad_x * 2, row_line_h, line,
                              new_x='LEFT', new_y='NEXT')
                    self.set_x(x + pad_x)
                x += col_widths[i]
            self.set_y(y0 + row_h)

        self.set_font('Songti', '', BODY_FONT_SIZE)
        self.set_text_color(0, 0, 0)
        draw_row(headers, (230, 230, 230))
        for row in rows:
            self.set_text_color(30, 30, 30)
            draw_row(row, (255, 255, 255))
        self.ln(1)

    def write_image(self, img_path, caption='', max_h=90):
        img_path = Path(img_path)
        if not img_path.exists():
            self._write_image_placeholder(img_path, caption)
            return
        img = Image.open(img_path)
        iw, ih = img.size
        max_w = CONTENT_W * 0.48
        ratio = min(max_w / iw, max_h / ih)
        draw_w = iw * ratio
        draw_h = ih * ratio
        total_h = draw_h + 18
        self.check_page_break(total_h)
        self.ln(3)
        x = LEFT_MARGIN + (CONTENT_W - draw_w) / 2
        self.image(str(img_path), x=x, y=self.get_y(), w=draw_w, h=draw_h)
        self.set_y(self.get_y() + draw_h + 2)
        if caption:
            self.set_font('Songti', '', 9)
            self.set_text_color(100, 100, 100)
            self.set_x(LEFT_MARGIN)
            self.cell(CONTENT_W, 5, _safe_text(caption), align='C', new_x='LMARGIN', new_y='NEXT')
            self.set_text_color(30, 30, 30)
        self.ln(3)

    def _write_image_placeholder(self, img_path, caption):
        self.missing_images.append((str(img_path), caption))
        box_h = 58
        self.check_page_break(box_h + 16)
        self.ln(3)
        x = LEFT_MARGIN + CONTENT_W * 0.18
        w = CONTENT_W * 0.64
        y = self.get_y()
        self.set_draw_color(120, 120, 120)
        self.set_fill_color(248, 248, 248)
        self.rect(x, y, w, box_h, style='DF')
        self.set_font('Songti', '', 11)
        self.set_text_color(120, 120, 120)
        self.set_xy(x, y + 18)
        self.cell(w, 7, '待补游戏截图', align='C', new_x='LEFT', new_y='NEXT')
        self.set_xy(x, y + 28)
        self.cell(w, 7, img_path.name, align='C', new_x='LEFT', new_y='NEXT')
        self.set_y(y + box_h + 2)
        self.set_font('Songti', '', 9)
        self.set_text_color(100, 100, 100)
        self.set_x(LEFT_MARGIN)
        self.cell(CONTENT_W, 5, _safe_text(caption + '（截图占位）'), align='C',
                  new_x='LMARGIN', new_y='NEXT')
        self.set_text_color(30, 30, 30)
        self.ln(3)


def _safe_text(text):
    replacements = {
        '→': '->', '←': '<-', '↑': '^', '↓': 'v', '★': '*',
        '“': '"', '”': '"', '‘': "'", '’': "'",
        '—': '-', '·': '.', '：': ':', '（': '(', '）': ')',
    }
    for old, new in replacements.items():
        text = text.replace(old, new)
    return ''.join(c if ord(c) <= 0xFFFF else '?' for c in text)


def img(key):
    return SCREENSHOTS[key][0]


def write_document(pdf):
    pdf.add_page()
    pdf.write_h1('目  录')
    for item in [
        '一、引言',
        '    1.1 编写目的',
        '    1.2 软件概述',
        '    1.3 运行环境',
        '    1.4 术语与缩略语',
        '    1.5 水浒题材如何落到规则里',
        '二、软件总体设计',
        '    2.1 软件需求概括',
        '    2.2 总体架构设计',
        '    2.3 模块划分与关系',
        '    2.4 场景与界面系统设计',
        '    2.5 主循环与画面适配',
        '    2.6 玩法流程',
        '三、核心模块详细设计',
        '    3.1 启动、加载与平台适配',
        '    3.2 山寨主界面与回目',
        '    3.3 对局棋盘、征兵与合成',
        '    3.4 兵种射程与普攻表现',
        '    3.5 组字成将与好汉出手',
        '    3.6 波次、漏兵与及时雨',
        '    3.7 主动道具与被动道具',
        '    3.8 十字坡酒店与抽奖',
        '    3.9 好汉谱、兵器与座次',
        '    3.10 结算与剧情回放',
        '    3.11 存档与云同步',
        '四、数据结构设计',
        '五、数据接口设计',
        '六、出错处理设计',
        '七、性能优化设计',
        '八、结论',
    ]:
        pdf.write_body(item)

    pdf.add_page()
    pdf.write_h1('一、引言')
    pdf.write_h2('1.1 编写目的')
    pdf.write_body(
        f'编写本设计说明书是{SOFTWARE_FULL_NAME} {SOFTWARE_VERSION}软件著作权登记材料的一部分。'
        '本文档用于说明本软件的功能范围、总体架构、核心模块、数据结构、接口设计、异常处理和性能优化方案，'
        '证明本软件为独立开发完成的原创游戏软件。'
    )
    pdf.write_body(
        '本文档面向软件著作权审查人员及后续维护人员，重点描述软件技术实现，不包含运营数据、用户隐私数据和商业敏感策略。'
    )

    pdf.write_h2('1.2 软件概述')
    pdf.write_body(
        '营救宋江是一款按《水浒传》回目推进的竖屏对战游戏。玩家守住棋盘一端的宋江，'
        '用炊饼聚义，把抽出的字按好汉姓名排上棋盘。排成之后，这名好汉用原著里的那件兵器自动出手，拦住沿路赶来的官兵。'
        '官兵走到宋江面前会扣心。宋江绰号及时雨，每掉一颗仍活着的心，就撒下一笔炊饼。'
        '上下半区各守一名宋江，对手用同一套组字和兵器规则，由脚本代为布阵。'
    )
    pdf.write_body(
        '本软件的关卡、角色、兵器、道具和资源都绑在水浒情节上，不是把一套通用组字对战换上古风名字。'
        '五十七回使用原著回目做标题。一百单八将各自有绰号、天罡或地煞星号、忠义堂座次和原著兵器。'
        '羁绊、头目克制、生辰纲、蒙汗药、石碣和十字坡，都按原著关系写成规则。'
    )
    pdf.write_body('本软件的主要功能包括:')
    for text in [
        '加载与健康游戏忠告: 启动时展示游戏名称、加载进度和健康游戏忠告。',
        '山寨主界面: 展示玩家座次、铜钱、体力、当前回目，并进入出征、好汉谱和十字坡酒店。',
        '水浒回目: 五十七回按原著顺序解锁。开局有引子，胜利后展示典故，这一回的好汉入伙。',
        '对局棋盘: 每边 8 列 5 行。用炊饼聚义、拖放、同级合成，白地摆兵，草地可用方便铲开垦。',
        '兵种与射程: 刀、枪、弓、骑各有冷却、伤害和攻击范围。刀在本格以外再盖一格，骑再向外一格半。',
        '组字成将: 按好汉姓名排字后亮框，显示绰号，并用该将的原著兵器自动普攻，大招自动施放。',
        '波次与及时雨: 官兵分波出现。宋江每掉一颗仍活着的心，补 10 个炊饼。',
        '原著道具: 主动道具拖上战场才放出；被动道具开局生效。名称和效果对应生辰纲、蒙汗药、石碣等情节。',
        '局外养成: 孙二娘的十字坡酒店购买或抽奖。好汉谱收录一百单八将，兵器分品级，羁绊按原著渊源触发。',
        '存档同步: 进度先写本地。微信登录后与腾讯云 CloudBase 按更新时间对齐。',
    ]:
        pdf.write_bullet(text)
    p, c = img('home')
    pdf.write_image(p, c)

    pdf.write_h2('1.3 运行环境')
    pdf.write_table(
        ['环境项', '要求说明'],
        [
            ['客户端平台', '微信小游戏。编辑器内可直接运行同一套对局逻辑'],
            ['开发语言', 'C#、JavaScript'],
            ['引擎', '团结引擎（Unity 2022.3 内核）'],
            ['画面', '竖屏。宽度锁成 8 格，格子保持正方形，长屏把多出的高度补在两军之间'],
            ['后端平台', '腾讯云 CloudBase 云函数 songjiang-api'],
            ['数据存储', '本地存档；云端集合 songjiang_playerData'],
        ],
        [36, 129],
    )

    pdf.write_h2('1.4 术语与缩略语')
    pdf.write_table(
        ['术语', '含义说明'],
        [
            ['炊饼', '局内货币。开局 20 个，聚义消耗，杀敌和及时雨都会增加'],
            ['聚义', '花炊饼从山寨刷出一枚棋子。价格随次数递增'],
            ['山寨', '棋盘下方的 5 个待摆格子'],
            ['好汉', '由姓名用字在棋盘上排成的武将，占多格，自动出手'],
            ['及时雨', '宋江每掉一颗仍活着的心，立刻补 10 个炊饼'],
            ['座次', '局外成长阶位，从喽啰直到天魁星'],
            ['GAME_KEY', '游戏代号 songjiang，用于云存档和登录隔离'],
        ],
        [36, 129],
    )

    pdf.write_h2('1.5 水浒题材如何落到规则里')
    pdf.write_body(
        '审查和阅读本软件时，应把下面这些对应关系看成规则，而不是界面文案。'
        '换掉其中任何一项，关卡解锁、组字判定、出手表现或资源结算都会改变。'
    )
    pdf.write_table(
        ['原著元素', '在本软件中的规则'],
        [
            ['宋江 / 及时雨', '上下各守一名宋江。每掉一颗仍活着的心，补 10 个炊饼'],
            ['回目', '五十七回用原著标题。未通上一回不能进下一回，通关后播放典故'],
            ['一百单八将', '按姓名组字。每人有绰号、星号、忠义堂座次、职司和原著兵器'],
            ['天罡地煞', '座次 1 到 36 为天罡，37 到 108 为地煞。武大郎可出场，但不占这 108 席'],
            ['原著兵器与招式', '林冲丈八蛇矛、李逵双板斧、武松戒刀。大招如风雪山神庙、醉打，自动施放'],
            ['羁绊', '阮氏三雄、七星聚义等有渊源的好汉同时上场，伤害提高'],
            ['头目克制', '带抵抗的头目只受一半伤害；原著克星出手时伤害改为五倍'],
            ['玩家座次', '喽啰直到天魁星，是玩家自己的成长，与好汉的星号座次分开'],
            ['情节道具', '生辰纲、蒙汗药、石碣、替天行道旗、孙二娘包子、方便铲'],
        ],
        [42, 123],
    )
    pdf.write_body(
        '五十七回打通蓼儿洼之后，才开放聚义厅。玩家从已入伙的好汉里自选五员，进行一阵强过一阵的车轮战。'
        '一百单八将没有聚齐时，这个入口不出现。'
    )

    pdf.write_h1('二、软件总体设计')
    pdf.write_h2('2.1 软件需求概括')
    pdf.write_body(
        '本软件把对局规则和画面分开。规则层用纯 C# 描述棋盘、波次、索敌、伤害和胜负，不依赖场景，便于单独验证。'
        '画面层读取规则状态来摆格子、播攻击和接玩家拖拽。后端只负责登录和存档，不参与单局演算。'
    )
    for text in [
        '上下半区使用同一套规则。玩家在下，对手在上，对手由收入、操作节奏和阵容脚本驱动。',
        '一局要能看清征兵、合成、组字、射程和出手，不能只在结算时才知道发生了什么。',
        '长屏手机不能把棋盘拉变形，也不能在聚义按钮下面留一大块空白。',
        '退出、断网和换设备后，回目进度、座次、铜钱和已购道具要能恢复。',
    ]:
        pdf.write_bullet(text)

    pdf.write_h2('2.2 总体架构设计')
    pdf.write_body(
        '软件划分为启动层、规则层、表现层、局外界面层、存档层和后端服务层。'
        '表现层只调用规则层的公开操作，例如征兵、摆放、使用道具，不直接改敌人血量。'
    )
    pdf.write_code_block([
        'GameFlow                         启动、相机、主界面与对局切换',
        '  +-- LoadingScreen              加载页与健康游戏忠告',
        '  +-- HomeScreen / HomeShop      山寨、回目、酒店、抽奖',
        '  +-- HomePanels / HomeStory     好汉谱、兵器、座次、剧情',
        '  +-- BattleScreen               一局的流程、横幅、结算入口',
        '  +-- BattleView / BattleFx      棋盘表现、拖拽、普攻兵器',
        '  +-- Sim.Match / Side / Board   对局规则',
        '  +-- Save / CloudSync / Backend 本地存档与云同步',
        '  +-- songjiang-api              登录、拉取存档、上传存档',
    ])

    pdf.write_h2('2.3 模块划分与关系')
    pdf.write_table(
        ['模块层', '代表文件', '功能简述'],
        [
            ['启动层', 'GameFlow / LoadingScreen', '创建相机、读存档、切主界面和对局'],
            ['规则层', 'Match / Side / Board / Heroes', '波次、索敌、合成、组字、胜负'],
            ['表现层', 'BattleView / BattleFx / BattleMap', '格子、拖拽、射程圈、攻击兵器'],
            ['局外层', 'HomeScreen / HomeShop / HomePanels', '座次、回目、酒店、图鉴、兵器'],
            ['存档层', 'Save / CloudSync / Backend', '本地读写、登录、按时间对齐云端'],
            ['后端层', 'songjiang-api', '微信登录、JWT、存档 pull / push'],
        ],
        [28, 62, 75],
    )
    pdf.write_body(
        '玩家点聚义时，表现层调用规则层扣炊饼并生成棋子，再根据返回的事件播放音效和刷新山寨。'
        '规则层发出攻击事件后，表现层才画兵器和飘字，因此改画面不会改伤害。'
    )

    pdf.write_h2('2.4 场景与界面系统设计')
    pdf.write_body(
        '软件没有多场景切换。启动后停在同一个运行环境里，用界面对象替换主界面和对局。'
        '主界面是界面控件；对局棋盘、宋江、山寨和聚义是世界坐标里的精灵，方便和格子对齐。'
    )
    p, c = img('loading')
    pdf.write_image(p, c)
    pdf.write_body(
        '加载页先出现，展示名称、进度和健康游戏忠告，最短停留一段时间后再进入山寨，避免一闪而过。'
    )

    pdf.write_h2('2.5 主循环与画面适配')
    pdf.write_body(
        '对局按固定小步推进，每步约六十分之一秒，避免帧率波动改变出手和走位结果。'
        '画面宽度永远对应 8 个格子。9:16 窗口里，山寨和聚义贴近底边。'
        '更高的手机把多出来的高度补在两军之间，自己的棋盘、山寨和聚义一起下移，底下只留底部安全区。'
    )
    pdf.write_code_block([
        '可见宽度 = 8 格',
        '9:16 世界高度 = 8 * 16 / 9',
        '多出的高度 = 实际世界高度 - 9:16 世界高度 - 底部安全区',
        '下半场下移 = 多出的高度',
    ])

    pdf.write_h2('2.6 玩法流程')
    pdf.write_body(
        '玩家一次只打一回。从打开游戏到领完结算，顺序是固定的：加载、山寨、选回目、扣体力、匹配、备战、聚义布阵、'
        '官兵分波来攻、胜负结算，再回到山寨。对局中途没有手动释放大招这一步，好汉排成之后自己出手。'
    )
    pdf.write_code_block([
        '加载页(游戏名 + 进度 + 健康游戏忠告)',
        '  -> 山寨主界面',
        '  -> 选一回(未通上一回则锁住) -> 扣 5 点体力',
        '  -> 匹配完成(约 1.8 秒)',
        '  -> 备战约 12 秒: 探马沿路走，玩家聚义摆子',
        '  -> 官兵分波出现，双方同时守自己的宋江',
        '  -> 一方宋江心数归零 -> 结算',
        '  -> 入伙、升星、发铜钱 -> 回到山寨，可进十字坡',
    ])

    pdf.write_h3('2.6.1 加载与进入山寨')
    pdf.write_body(
        '启动后先停在加载页。这一页同时是健康游戏忠告：画面是营救宋江的水墨封面，底部印着忠告全文，进度条停在忠告上方。'
        '封面至少停留约 2.6 秒，并且要等云存档拉取结束，才淡出进入山寨。登录或拉云失败时仍进山寨，只使用本地进度。'
    )
    pdf.write_body(
        '山寨上方是座次牌、铜钱和体力。体力上限 30 点，每 5 分钟恢复 1 点，不满时旁边显示下一点的倒计时。'
        '中间是当前回目，左右箭头可以换已经解锁的回。下方红色按钮是开始游戏，标着本次消耗的体力。'
        '左侧进入公告、好汉谱和回目卷，右上是十字坡的孙二娘，底部是排行榜和兵器背包。'
    )
    p, c = img('home')
    pdf.write_image(p, c)

    pdf.write_h3('2.6.2 选回目并出征')
    pdf.write_body(
        '回目卷按篇章列出五十七回，例如引首、鲁达篇、林冲篇。每一行写着回目名、原著标题和这一回会入伙的人数。'
        '没打过上一回的行是锁住的，点开会提示先打通上一回。点中一行后回到山寨，中央回目改成这一回。'
    )
    p, c = img('story')
    pdf.write_image(p, c)
    pdf.write_body(
        '点开始游戏时先扣 5 点体力。体力不够就不进对局，改为询问是否看一段激励视频，看完补 5 点。'
        '扣成功后先播匹配：上方是玩家自己的座次和胜率，下方是这一回的对手名字、座次和胜率，底部写第几回。'
        '匹配约 1.8 秒，只做开场，不改棋盘。'
    )

    pdf.write_h3('2.6.3 备战')
    pdf.write_body(
        '进入棋盘后，第一波官兵要到第 12 秒才出现。开局约 0.8 秒，双方各有一名探马沿出兵路线走一圈，嘴里提示宋江在哪一头，大约走 7.5 秒后离开。'
        '探马不扣心、不受攻击，只让玩家看清路。这段时间横幅写着备战和对手名字，玩家已经可以聚义、拖子、合成。'
        '画面下半是玩家，上半是对手。两边都是 8 列 5 行，各有一名宋江和心数。玩家的山寨五格和聚义按钮在最下方。'
    )
    p, c = img('scout')
    pdf.write_image(p, c)

    pdf.write_h3('2.6.4 聚义、摆放与合成')
    pdf.write_body(
        '开局 20 个炊饼。点聚义花炊饼，从山寨空格里刷出一枚棋子。第一次 10 个，之后每次贵 2 个。山寨满了或炊饼不够时，这一下不生效。'
        '刷出的多数是刀、枪、弓、骑。大约两成四是好汉姓名里的一个字，大约一成二是铲子。连续五次没出字时，下一次必出字。'
        '白地没了但还有草地可开时，会优先给出铲子。'
    )
    pdf.write_body(
        '棋子先待在山寨。玩家把它拖到自己棋盘的白地上才参战。拖动时画出这枚棋子的攻击范围，松手在不能放的格子上会回到原处。'
        '草地和沼泽不能直接摆，要先把铲子拖到紧挨白地的那一格，开成白地。两枚相同兵种、相同等级的棋子叠在一起合成高一级，最高 5 级。'
        '不想要的棋子拖回聚义按钮上遣散：铲子退 1 个炊饼，兵按等级退回两倍。'
    )
    p, c = img('battle')
    pdf.write_image(p, c)
    p, c = img('range')
    pdf.write_image(p, c)
    pdf.write_body(
        '刀从本格中心量到外沿 1.5，也就是本格以外再盖满相邻一格。骑外沿 2，本格以外再向外一格半。枪约 2.15，弓约 2.65。'
        '小兵按冷却自动打射程内的官兵：刀劈、枪刺、弓射、骑冲。对手半区由脚本按收入和节奏自己聚义、摆放，玩家不能替它操作。'
    )

    pdf.write_h3('2.6.5 组字成将')
    pdf.write_body(
        '金色的字按从左到右、从上到下读。横着或竖着读出某一员好汉的姓名后，这些格子合成这名好汉，外框亮起，并显示绰号。'
        '例如排成「鲁智深」，得到的是花和尚，不是一个通用斧兵。排成的瞬间好汉就在场上，不需要再点一次确认。'
    )
    p, c = img('hero')
    pdf.write_image(p, c)
    pdf.write_body(
        '好汉没有手动按钮。普攻冷却好了就自动打，大招冷却好了也自动放。普攻从名字上挥出这员好汉自己的兵器，打向目标。'
        '鲁智深挥方便铲，林冲刺丈八蛇矛，李逵砍双板斧，武松用戒刀。没有兵器图的，例如蒙汗药，改为一团药尘飞向目标。'
        '这一下只改变看得见的出手，不改伤害和冷却。'
    )
    p, c = img('attack')
    pdf.write_image(p, c)

    pdf.write_h3('2.6.6 波次、漏兵与及时雨')
    pdf.write_body(
        '第 12 秒起出第一波。同一波里官兵一个接一个从路口走出，前两波间隔约 1 秒出头，往后更密。'
        '一波走完后停一停再出下一波：前两波停 8 秒，第三到第五波停 7 秒，再往后停 6 秒。人数随波次增加，到 44 人封顶，单兵血量按波次变厚。'
        '官兵沿路径往宋江走。走出草堆之前看不见，也不能被打。走到终点且没有护盾时，宋江扣心。小卒一颗，捕快和头目可以更多。'
    )
    pdf.write_body(
        '扣心之后宋江还活着，就按掉的心数触发及时雨：每颗心立刻补 10 个炊饼，并在画面上撒钱，提示马上再聚义。'
        '一次漏两颗心就补 20 个。宋江被这一下打死，不再补。护盾挡住、没有扣到心，也不触发。'
        '上下两名宋江用同一套规则。玩家守下边，对手守上边。'
    )

    pdf.write_h3('2.6.7 局内道具')
    pdf.write_body(
        '带进这一局的主动道具排在聚义左边，一局最多四种，包括蒙汗药、包子、天书、攻速符和陷阱。'
        '按住拖到战场上松手才放出，拖回按钮下方则取消。放完要转完冷却才能再放。长按约 0.32 秒看说明，气泡停在山寨上方，不挡住道具那一排。'
        '被动道具排在聚义下面，本局一直生效，点一下只看说明，不会被消耗。'
    )
    p, c = img('item_drag')
    pdf.write_image(p, c)
    p, c = img('item_tip')
    pdf.write_image(p, c)

    pdf.write_h3('2.6.8 胜负、结算与回到山寨')
    pdf.write_body(
        '哪一方宋江的心先归零，哪一方输。若同一小步里两边一起归零，斩敌多的一方赢。'
        '结算展示胜负、撑到的波次、斩敌数和及时雨次数。胜利推进回目解锁，把这一回入伙的好汉写入好汉谱，发放铜钱，并按胜负加减座次的星。'
        '中途撤回不记座次进度。领完之后回到山寨，并打开十字坡，方便用刚得到的铜钱买下一局要用的道具。'
    )
    p, c = img('result')
    pdf.write_image(p, c)
    pdf.write_body(
        '五十七回全部打通、到达蓼儿洼之后，回目卷末尾的聚义厅才打开。玩家从已入伙的好汉里自选五员，进行一阵强过一阵的车轮战。'
        '每一阵结束可以换将。阵容里有渊源的好汉同时上场，触发羁绊，伤害更高。一百单八将没有聚齐时，这个入口不出现。'
    )

    pdf.write_h1('三、核心模块详细设计')
    pdf.write_h2('3.1 启动、加载与平台适配')
    pdf.write_body(
        'GameFlow 在场景加载后自动创建。它设置帧率、读取本地存档，并按屏幕宽高比调整相机。'
        '微信环境读取窗口安全区和右上角胶囊，标题避开胶囊。编辑器里没有该接口时，按屏幕安全区处理。'
    )
    pdf.write_body(
        '云同步在启动时先登录再拉云。云端较新就覆盖本地，本地较新才上传。登录或拉取失败时只使用本地，禁止上传，避免空档盖掉云端。'
    )

    pdf.write_h2('3.2 山寨主界面与回目')
    pdf.write_body(
        '主界面上方是玩家座次牌、铜钱和体力，中间是当前回目，下方和侧边进入出征、好汉谱、十字坡酒店。'
        '回目卷按引首、鲁达篇、林冲篇等篇章列出五十七回，标题用原著回目，例如「林教头风雪山神庙」。'
        '未打过上一回不能进入下一回。打通蓼儿洼后，聚义厅才按玩家自选的五员好汉进行车轮战。'
    )
    p, c = img('story')
    pdf.write_image(p, c)
    pdf.write_body(
        '点出征后先播一段回目引子，再进入战场。赢下后展示这一回的水浒典故，并把入伙好汉写入存档。'
    )

    pdf.write_h2('3.3 对局棋盘、征兵与合成')
    pdf.write_body(
        '每边棋盘 8 列 5 行。路径格走兵，白地可以摆子，草地和沼泽要先用铲子开成白地。'
        '山寨有 5 格。聚义花费炊饼，价格从 10 开始，每次增加 2。开局 20 个炊饼，第一轮摆完后还留得住下一次。'
    )
    p, c = img('scout')
    pdf.write_image(p, c)
    pdf.write_body(
        '开局有一段备战。官军探马沿出兵路线走一圈，提示玩家先布阵。探马不影响规则，只做引导。'
    )
    p, c = img('battle')
    pdf.write_image(p, c)
    pdf.write_body(
        '两枚相同兵种、相同等级的棋子叠在一起会合成高一级。等级提高伤害和攻速，最高 5 级。'
        '山寨里的棋子拖到白地上才参战。拖动时显示攻击范围，松手在非法格子上会回到原位。'
    )

    pdf.write_h2('3.4 兵种射程与普攻表现')
    pdf.write_body(
        '射程从格子中心量到圈的外沿。本格边缘在中心以外半格。刀再向外盖满一格，外沿 1.5；'
        '骑再向外一格半，外沿 2。枪约 2.15，弓约 2.65。御敌千里只把超过两格的远程射程翻倍，骑收到正好两格，不翻倍。'
    )
    pdf.write_table(
        ['兵种', '作用', '射程取法'],
        [
            ['刀', '近战劈砍，带小范围溅射', '本格以外再一格'],
            ['枪', '直线突刺，穿透三人', '约两格出头'],
            ['弓', '远程放箭', '远于枪'],
            ['骑', '攻速快，击退', '本格以外再一格半'],
        ],
        [28, 79, 58],
    )
    p, c = img('range')
    pdf.write_image(p, c)
    pdf.write_body(
        '小兵出手时，格子上的字会变成对应兵器。刀画弧，枪前刺，弓拉开，骑向前冲。'
        '攻击范围圈的直径和数值一致，玩家拖动时看到的圈就是实际能打到的距离。'
    )

    pdf.write_h2('3.5 组字成将与好汉出手')
    pdf.write_body(
        '棋盘上的金色字按从左到右、从上到下读成姓名。对上某一员好汉的名字后，这些格子合成这名好汉，外框亮起，并显示原著绰号。'
        '排成「林冲」得到的是豹子头，不是一个通用枪兵。对手半区旋转了 180 度，读字方向在它自己的坐标里反过来，保证双方规则一致。'
    )
    p, c = img('hero')
    pdf.write_image(p, c)
    pdf.write_body(
        '好汉没有手动技能按钮。普攻按冷却自动打出，大招冷却好了也自动放。'
        '普攻会从好汉名字上挥出他自己的兵器：刀剑斧棍画弧，枪叉直刺，弓在身边拉开，石子抛出，鞭和绳甩到目标上。'
        '没有兵器图的，例如蒙汗药、酒，改为一团药尘飞向目标。伤害、射程和冷却不因这套表现而改变。'
    )
    p, c = img('attack')
    pdf.write_image(p, c)
    pdf.write_body(
        '好汉数据按一百二十回本记录姓名、绰号、星号、职司、事迹、原著兵器和大招名。'
        '座次 1 到 36 是天罡，37 到 108 是地煞。武大郎可以组出来，星号为空，不占一百单八将的席位。'
        '没有单独调过数值的好汉，按出手样子套用模板，再按忠义堂座次微调伤害。'
        '带抵抗的头目平常只受一半伤害；数据里写明的克星出手时，伤害改为五倍。例如棒打洪教头一回，洪教头只受一半伤害，林冲出手则改为五倍。'
    )

    pdf.write_h2('3.6 波次、漏兵与及时雨')
    pdf.write_body(
        '每一回由多波官兵组成。两波之间有间隔，越往后间隔略缩短，单兵血量按波次增长。'
        '普通卒、快差、厚甲、精英捕快和头目的血量、速度、漏心和赏金不同。捕快和头目一次可扣更多心。'
    )
    pdf.write_body(
        '官兵走到终点且没有被护盾挡住时，宋江扣心。若扣心后宋江还活着，按掉心数量触发及时雨：每颗心补 10 个炊饼。'
        '一次漏掉两颗心就补 20 个。宋江被打死的那一下不再补。护盾挡住、没有扣到心，也不触发。'
        '画面播放钱雨，并提示立刻聚义反推。'
    )

    pdf.write_h2('3.7 主动道具与被动道具')
    pdf.write_body(
        '主动道具放在聚义左边，包括蒙汗药、包子、天书、攻速符和陷阱，一局最多带四种。按住拖到战场上松手才放出，拖回按钮下方则取消。'
        '长按约 0.32 秒弹出说明。说明气泡停在山寨上方，不盖住道具和聚义。被动道具排在聚义下面，点一下只看说明，本局一直生效，不会被消耗。'
    )
    p, c = img('item_drag')
    pdf.write_image(p, c)
    p, c = img('item_tip')
    pdf.write_image(p, c)
    pdf.write_table(
        ['道具', '类型', '作用'],
        [
            ['蒙汗药', '主动', '全场官兵昏睡 3 秒'],
            ['包子', '主动', '五成半回一颗心，否则掉一颗'],
            ['天书', '主动', '场上最低级的兵升一级'],
            ['攻速符', '主动', '全军攻速提高，持续十二秒'],
            ['陷阱', '主动', '官兵跌回并短暂爬不上来'],
            ['生辰纲', '被动', '开局炊饼增加'],
            ['替天行道旗', '被动', '全军伤害提高'],
            ['水泊', '被动', '官兵移速降低'],
            ['石碣', '被动', '官兵靠近宋江时天降石碣'],
            ['续命丹', '被动', '宋江额外多八颗心'],
            ['方便铲', '被动', '开局多两把铲子'],
            ['御敌千里', '被动', '超过两格的远程射程翻倍'],
        ],
        [36, 28, 101],
    )

    pdf.write_h2('3.8 十字坡酒店与抽奖')
    pdf.write_body(
        '十字坡酒店由孙二娘经营。商店每天刷新三件货，其中较贵的一件可以看激励视频领取。'
        '当天买下的道具可以退回。主动道具进背包，带进下一局；被动道具购买后在后续对局中生效。'
    )
    p, c = img('shop')
    pdf.write_image(p, c)
    pdf.write_body(
        '抽奖是九宫格外圈八件，每天换一批。稀有度越高，权重越低。抽出的道具写入与商店相同的存档字段。'
    )
    p, c = img('lottery')
    pdf.write_image(p, c)

    pdf.write_h2('3.9 好汉谱、兵器与座次')
    pdf.write_body(
        '好汉谱收录一百单八将。已入伙的好汉可以查看绰号、星号、原著兵器、普攻说明和以原著情节命名的大招。'
        '有渊源的好汉同时上场时触发羁绊，例如阮氏三雄、七星聚义、十字坡夫妻。羁绊提高伤害，并在说明里写上对应情节。'
        '兵器分凡、利、宝、神四品，缴获后可以装备给对应好汉，提高伤害并附带特性。'
    )
    p, c = img('codex')
    pdf.write_image(p, c)
    p, c = img('weapon')
    pdf.write_image(p, c)
    pdf.write_body(
        '玩家自己的座次从喽啰、小校、都头、提辖，升到步军、水军、马军头领，再到八骠骑、五虎将、地煞星、天罡星，顶上是天魁星。'
        '每一大阶分叁、贰、壹。这是玩家的成长阶位，与好汉在忠义堂里的第几位、天罡或地煞星号不是同一套数字。'
        '座次不在单局中途改写已经生成的敌人。座次牌可打开排行，排行只展示名次和座次，不在客户端保存他人存档。'
    )

    pdf.write_h2('3.10 结算与剧情回放')
    pdf.write_body(
        '一方宋江心数归零则对局结束。结算展示胜负、撑到的波次、斩敌数和及时雨次数。'
        '胜利推进回目解锁，发放铜钱，并播放这一回的典故。中途撤回不记座次进度。'
    )
    p, c = img('result')
    pdf.write_image(p, c)

    pdf.write_h2('3.11 存档与云同步')
    pdf.write_body(
        '本地存档包括回目进度、座次、铜钱、体力、已入伙好汉、兵器、道具和引导标记。'
        '对局中的棋盘不写入长期存档，退出对局后只保留局外结果。'
    )
    pdf.write_body(
        '云同步照固定顺序进行：启动登录，拉取云端，比较更新时间。云端更新覆盖本地；本地更新则延迟上传。'
        '服务端返回版本冲突时改为再拉一次，不用本地空数据覆盖。观看激励视频的结果只增加对应道具或体力，不上传广告凭证以外的隐私内容。'
    )
    pdf.write_body('云同步没有单独的游戏截图。上述流程由存档层和后端完成，不在对局画面中展示。')

    pdf.write_h1('四、数据结构设计')
    pdf.write_h2('4.1 棋子与格子')
    pdf.write_body(
        '格子类型包括草地、路径、水面、白地、岩石和沼泽。棋子类型包括兵种、好汉用字和铲子。'
        '兵种记录等级。好汉实例记录所占格子、等级、普攻冷却、大招冷却和经验。'
    )
    pdf.write_code_block([
        'Piece: Id, Type, Kind, Char, Level',
        'Cell: Grass, Path, Water, Tile, Rock, Marsh',
        'HeroInstance: Id, Def, Cells, Level, Cd, UltCd, Xp',
    ])

    pdf.write_h2('4.2 对局与波次')
    pdf.write_body(
        '一局 Match 持有两名 Side、当前时间和波次。Side 持有棋盘、山寨、炊饼、心数、道具冷却和已触发的及时雨次数。'
        '敌人记录种类、路径进度、生命、眩晕和加速剩余时间。'
    )
    pdf.write_table(
        ['数据', '主要字段', '说明'],
        [
            ['Match', 'Time, Wave, Over, Winner', '一局的公共进程'],
            ['Side', 'Buns, Hp, Hearts, Rains', '一方资源与宋江状态'],
            ['Enemy', 'Kind, Progress, Hp, StunT', '路径上的官兵'],
            ['Chapter', 'Name, Hui, Map, Pool, Boss', '回目、地图和好汉池'],
            ['Loadout', '主动数量、被动开关', '带入一局的道具'],
        ],
        [32, 62, 71],
    )

    pdf.write_h2('4.3 局外存档')
    pdf.write_body(
        '局外存档用一个可序列化对象保存。好汉用编号列表，兵器用品级和数量，道具用数量或是否拥有。'
        '云端保存同一份存档正文，另加更新时间，用于和本地比较先后。'
    )

    pdf.write_h1('五、数据接口设计')
    pdf.write_h2('5.1 客户端到规则层')
    pdf.write_body('表现层只通过 Side 的公开方法改变对局，例如征兵、把山寨棋子放到格子、合成、使用主动道具、开垦。规则层用事件把结果交还表现层。')
    pdf.write_table(
        ['事件', '含义'],
        [
            ['Recruit', '聚义生成一枚棋子'],
            ['Merge', '两枚棋子合成'],
            ['HeroFormed', '名字排成好汉'],
            ['Attack / HeroAttack', '小兵或好汉出手'],
            ['Leak', '官兵走到宋江面前'],
            ['Rain', '及时雨，补炊饼'],
            ['Ult', '好汉大招'],
        ],
        [48, 117],
    )

    pdf.write_h2('5.2 客户端到后端')
    pdf.write_body('后端是 CloudBase HTTP 云函数。客户端用平台登录凭证换取令牌，之后在拉取和上传时携带令牌。')
    pdf.write_table(
        ['接口', '作用'],
        [
            ['POST /login', '校验微信登录凭证，签发令牌'],
            ['POST /save/pull', '按用户读取云端存档和更新时间'],
            ['POST /save/push', '上传存档。版本落后时拒绝并要求重拉'],
            ['GET 或 POST /health', '服务健康检查'],
        ],
        [48, 117],
    )
    pdf.write_body('令牌密钥、微信密钥只放在云函数环境变量中，不写入客户端包，也不写入本文档。')

    pdf.write_h1('六、出错处理设计')
    for text in [
        '拖到不能放置的格子：棋子回到山寨或原格，不扣第二次炊饼。',
        '炊饼不足或山寨已满：聚义不生效，并给出文字提示。',
        '主动道具尚在冷却：拖到战场上会提示剩余时间，松手不生效。',
        '登录或拉云失败：进入只读本地模式，禁止上传。',
        '上传时云端版本更新：改为重新拉取，不用本地覆盖。',
        '激励视频未看完：不发放道具。',
        '宋江心数归零：停止继续漏兵和及时雨，进入结算。',
    ]:
        pdf.write_bullet(text)

    pdf.write_h1('七、性能优化设计')
    pdf.write_body(
        '对局逻辑按固定步长推进，表现可以掉帧，但伤害和走位不随帧率改变。攻击兵器、钱雨和飘字使用短生命周期对象，播完即销毁。'
    )
    pdf.write_body(
        '画面宽度固定为 8 格，不为了塞下高度而缩小整张棋盘，避免左右露出空地后又把格子拉成长方形。'
        '长屏只平移下半场并补草地，不增加棋盘格数。微信导出时首包资源走分包加载，不把数据包压成额外的压缩包格式，以符合平台对首包体积的要求。'
    )
    pdf.write_body(
        '云同步合并短时间内的多次存档改动，退出或切到后台时再立即上传一次。失败按间隔重试，连续失败不阻塞对局。'
    )

    pdf.write_h1('八、结论')
    pdf.write_body(
        f'{SOFTWARE_FULL_NAME} {SOFTWARE_VERSION}的规则、角色数据和界面都围绕营救宋江展开。'
        '回目标题、一百单八将的姓名与兵器、及时雨、炊饼、羁绊和头目克制，是关卡、组字、出手和资源结算的数据结构，不是贴在通用对战上的名称。'
        '规则与画面分离，上下半区共用同一套模拟；进度保存在本地，并在登录后与云端按时间对齐。'
        '上述结构、数据和接口能够说明本软件的技术实现，满足软件著作权登记文档鉴别材料的要求。'
    )


def validate_pdf():
    from pypdf import PdfReader
    return len(PdfReader(str(OUTPUT)).pages)


def main():
    pdf = DocPDF()
    pdf.add_font('Songti', '', songti_file())
    write_document(pdf)
    pdf.output(str(OUTPUT))
    pages = validate_pdf()
    print('=' * 60)
    print('  营救宋江软著文档鉴别材料 PDF 生成报告')
    print('=' * 60)
    print(f'  软件名称:     {SOFTWARE_FULL_NAME} {SOFTWARE_VERSION}')
    print(f'  申请人:       {APPLICANT_NAME}')
    print(f'  文档类型:     设计说明书')
    print(f'  生成页数:     {pages} 页')
    print(f'  输出文件:     {OUTPUT}')
    if pdf.missing_images:
        print(f'  缺少截图:     {len(pdf.missing_images)} 张，PDF 中为占位框')
        for path, caption in pdf.missing_images:
            print(f'    - {Path(path).name}: {caption}')
    else:
        print('  截图检查:     已找到全部截图')
    print('=' * 60)


if __name__ == '__main__':
    main()
