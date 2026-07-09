using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UI;

namespace MagicUtilities
{
    public static class UIAnimationUtilities
    {
        static string ScaleId(VisualElement ve)     => "UI_SCALE_"     + ve.GetHashCode();
        static string OpacityId(VisualElement ve)   => "UI_OPACITY_"   + ve.GetHashCode();
        static string TranslateId(VisualElement ve) => "UI_TRANSLATE_" + ve.GetHashCode();

        // -------- UI Toolkit --------

        // Scales an element from one value to another
        public static void Pop(VisualElement ve, Ease ease, float duration, float from, float to, bool ignoreTimeScale = true)
        {
            if (ve == null) return;
            ve.style.transformOrigin = new TransformOrigin(Length.Percent(50), Length.Percent(50), 0);

            var id = ScaleId(ve);
            DOTween.Kill(id);

            ve.style.scale = new Scale(new Vector3(from, from, 1f));
            DOVirtual.Float(from, to, duration, v => ve.style.scale = new Scale(new Vector3(v, v, 1f)))
                .SetEase(ease).SetUpdate(ignoreTimeScale).SetId(id);
        }

        // Moves an element into place from the screen center
        public static void PopFromCenter(VisualElement ve, Ease ease, float duration, bool ignoreTimeScale = true)
            => PopFromScreenPoint(ve, new Vector2(0.5f, 0.5f), ease, duration, ignoreTimeScale);

        // Moves an element into place from a screen point (0..1 on each axis)
        public static void PopFromScreenPoint(VisualElement ve, Vector2 originPercent, Ease ease, float duration, bool ignoreTimeScale = true)
        {
            if (ve == null) return;

            var screenRect  = ve.panel.visualTree.worldBound;
            var originX     = screenRect.x + screenRect.width  * originPercent.x;
            var originY     = screenRect.y + screenRect.height * originPercent.y;
            var panelCenter = ve.parent.LocalToWorld(ve.layout.center);

            var offsetX = originX - panelCenter.x;
            var offsetY = originY - panelCenter.y;

            var tid = TranslateId(ve);
            DOTween.Kill(tid);

            ve.style.translate = new StyleTranslate(new Translate(offsetX, offsetY));
            DOVirtual.Float(1f, 0f, duration, t =>
                    ve.style.translate = new StyleTranslate(new Translate(offsetX * t, offsetY * t)))
                .SetEase(ease).SetUpdate(ignoreTimeScale).SetId(tid);
        }

        // Fades an element's opacity from one value to another
        public static void Fade(VisualElement ve, Ease ease, float duration, float from, float to, bool ignoreTimeScale = true)
        {
            if (ve == null) return;

            var id = OpacityId(ve);
            DOTween.Kill(id);

            ve.style.opacity = from;
            DOVirtual.Float(from, to, duration, v => ve.style.opacity = v)
                .SetEase(ease).SetUpdate(ignoreTimeScale).SetId(id);
        }
    }
}