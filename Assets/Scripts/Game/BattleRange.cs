using Rescue.Sim;
using UnityEngine;

namespace Rescue
{
    /// <summary>
    /// 射程圈：淡色圆面 + 细亮边 + 圈外一窄条投影，裁在自家棋盘内；
    /// 普通字白边、天罡金边、地煞紫边；打开时弹出，圈内有一道向外扩散的波纹，圈里的敌人脚下标红。
    /// </summary>
    public sealed partial class BattleView
    {
        enum RangeStyle { Unit, Hero, Disha }

        const int RangeMarkMax = 16;
        const float RangeOpen = 0.24f;
        const float RangePulse = 1.8f;

        SpriteRenderer _rangeFill, _rangeRim, _rangeWave;
        readonly SpriteRenderer[] _rangeMarks = new SpriteRenderer[RangeMarkMax];
        Vector2 _rangeCenter;
        float _rangeR, _rangeK, _rangeT, _rangeBuiltR;
        bool _rangeAll, _rangeFromDrag;
        RangeStyle _rangeStyle;

        static Color RimColor(RangeStyle s) =>
            s == RangeStyle.Hero ? Pal.Hex("FFE08A") : s == RangeStyle.Disha ? Pal.Hex("D9B8FF") : new Color(1f, 1f, 1f);

        static Color FillColor(RangeStyle s) =>
            s == RangeStyle.Hero ? new Color(1f, 0.9f, 0.55f, 0.26f)
            : s == RangeStyle.Disha ? new Color(0.86f, 0.76f, 1f, 0.26f)
            : new Color(1f, 0.98f, 0.92f, 0.28f);

        void BuildRange()
        {
            var mask = new GameObject("rangeMask").AddComponent<SpriteMask>();
            mask.transform.SetParent(_root, false);
            mask.sprite = Art.White;
            mask.transform.localPosition = new Vector2(0f, MyBottom + Board.Rows * 0.5f);
            mask.transform.localScale = new Vector3(Board.Cols, Board.Rows, 1f);

            _rangeFill = Gfx.Sprite(_root, Art.RangeFill, Color.white, Vector2.zero, Vector2.one, 15, "rangeFill");
            _rangeWave = Gfx.Sprite(_root, Art.RangeRim(1f), Color.white, Vector2.zero, Vector2.one, 16, "rangeWave");
            _rangeRim = Gfx.Sprite(_root, Art.RangeRim(1f), Color.white, Vector2.zero, Vector2.one, 17, "rangeRim");
            foreach (var sr in new[] { _rangeFill, _rangeWave, _rangeRim })
            {
                sr.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
                sr.enabled = false;
            }
            for (int i = 0; i < RangeMarkMax; i++)
            {
                _rangeMarks[i] = Gfx.Sprite(_root, Art.Ring, Pal.Red, Vector2.zero, new Vector2(0.62f, 0.26f), 28, "rangeMark");
                _rangeMarks[i].enabled = false;
            }
        }

        /// <summary>射程 ≥ 20 视为全场（花荣），只铺满自家棋盘，不画边。</summary>
        void ShowRange(Vector2 center, float r, RangeStyle style, bool fromDrag)
        {
            if (r <= 0f) { HideRange(); return; }
            bool all = r >= 20f;
            bool reopen = _rangeR <= 0f || style != _rangeStyle || all != _rangeAll;
            bool moved = (center - _rangeCenter).sqrMagnitude > 0.01f || Mathf.Abs(r - _rangeR) > 0.01f;
            if (reopen) _rangeK = 0f;
            else if (moved) _rangeK = Mathf.Min(_rangeK, 0.45f);
            if (reopen || moved) _rangeT = 0f;
            _rangeCenter = center;
            _rangeR = r;
            _rangeAll = all;
            _rangeStyle = style;
            _rangeFromDrag = fromDrag;
            if (!all && Mathf.Abs(_rangeBuiltR - r) > 0.001f)
            {
                _rangeRim.sprite = _rangeWave.sprite = Art.RangeRim(r);
                _rangeBuiltR = r;
            }
            PlaceRange();
        }

        void HideRange()
        {
            _rangeR = 0f;
            _rangeFromDrag = false;
            PlaceRange();
        }

        void TickRange(float dt)
        {
            if (_rangeR <= 0f) return;
            _rangeK = Mathf.Min(1f, _rangeK + dt / RangeOpen);
            _rangeT += dt;
            PlaceRange();
        }

        static float OutBack(float k)
        {
            const float s = 1.6f;
            k -= 1f;
            return 1f + k * k * ((s + 1f) * k + s);
        }

        void PlaceRange()
        {
            bool on = _rangeR > 0f;
            _rangeFill.enabled = on;
            _rangeRim.enabled = on && !_rangeAll;
            _rangeWave.enabled = on && !_rangeAll;
            if (!on)
            {
                for (int i = 0; i < RangeMarkMax; i++) _rangeMarks[i].enabled = false;
                return;
            }

            float fade = Mathf.Clamp01(_rangeK * 1.6f);
            if (_rangeAll)
            {
                _rangeFill.transform.localPosition = new Vector2(0f, MyBottom + Board.Rows * 0.5f);
                _rangeFill.transform.localScale = Vector3.one * (Board.Cols + Board.Rows) * 1.2f;
                var fc = FillColor(_rangeStyle);
                _rangeFill.color = fc.A(fc.a * fade * (0.85f + 0.15f * Mathf.Sin(_rangeT * 3f)));
            }
            else
            {
                float s = Mathf.LerpUnclamped(0.72f, 1f, OutBack(_rangeK));
                float dia = _rangeR * 2f * s;
                _rangeFill.transform.localPosition = _rangeCenter;
                _rangeFill.transform.localScale = new Vector3(dia, dia, 1f);
                _rangeFill.color = FillColor(_rangeStyle).A(FillColor(_rangeStyle).a * fade);

                float rimDia = (_rangeR + Art.RangeRimPad) * 2f * s;
                var rc = RimColor(_rangeStyle);
                _rangeRim.transform.localPosition = _rangeCenter;
                _rangeRim.transform.localScale = new Vector3(rimDia, rimDia, 1f);
                _rangeRim.color = rc.A(fade * (0.92f + 0.08f * Mathf.Sin(_rangeT * 4f)));

                float w = Mathf.Repeat(_rangeT, RangePulse) / RangePulse;
                float wk = 1f - (1f - w) * (1f - w);
                _rangeWave.transform.localPosition = _rangeCenter;
                _rangeWave.transform.localScale = Vector3.one * rimDia * Mathf.Lerp(0.25f, 1f, wk);
                _rangeWave.color = rc.A(fade * 0.38f * Mathf.Sin(w * Mathf.PI) * (_rangeK >= 1f ? 1f : 0f));
            }
            MarkEnemiesInRange(fade);
        }

        void MarkEnemiesInRange(float fade)
        {
            float top = MyBottom + Board.Rows + 0.4f;
            float r2 = _rangeR * _rangeR;
            int n = 0;
            float bob = 0.9f + 0.1f * Mathf.Sin(_rangeT * 6f);
            foreach (var ev in _enemies.Values)
            {
                if (n >= RangeMarkMax) break;
                Vector2 p = ev.Root.localPosition;
                if (p.y > top || p.y < MyBottom - 0.4f) continue;
                if (!_rangeAll && (p - _rangeCenter).sqrMagnitude > r2) continue;
                var m = _rangeMarks[n++];
                m.enabled = true;
                float h = ev.GlyphHeight;
                m.transform.localPosition = p + new Vector2(0f, -h * 0.45f);
                m.transform.localScale = new Vector3(h * 1.5f * bob, h * 0.6f * bob, 1f);
                m.color = Pal.Red.A(0.85f * fade);
            }
            for (int i = n; i < RangeMarkMax; i++) _rangeMarks[i].enabled = false;
        }
    }
}
