using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace MagicUtilities
{
    public static class UIToolkitUtilities
    {
        // Shows or hides an element
        public static void SetVisible(VisualElement ve, bool visible) =>
            ve.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;

        // Places a UI element over a world position
        public static void SetElementToWorldPosition(VisualElement element, Vector3 worldPos, Camera cam, VisualElement root, Vector2 offset)
        {
            Vector2 panelPos = RuntimePanelUtils.CameraTransformWorldToPanel(root.panel, worldPos, cam);
            element.style.left = panelPos.x - offset.x;
            element.style.top = panelPos.y - offset.y;
        }

        // Waits until the element has a real size (layout done)
        public static IEnumerator WaitForGeometry(VisualElement ve)
        {
            bool ready = false;
            void OnGeom(GeometryChangedEvent e)
            {
                if (ve.resolvedStyle.width > 0f && ve.resolvedStyle.height > 0f)
                {
                    ready = true;
                    ve.UnregisterCallback<GeometryChangedEvent>(OnGeom);
                }
            }
            ve.RegisterCallback<GeometryChangedEvent>(OnGeom);
            yield return new WaitUntil(() => ready || ve.panel == null);
        }

        // Scrolls the parent ScrollView so the element becomes visible
        public static void EnsureVisible(VisualElement ve)
        {
            var sv = ve.GetFirstAncestorOfType<ScrollView>();
            if (sv == null) return;

            sv.schedule.Execute(() =>
            {
                if (ve != null && ve.panel != null && sv.contentContainer.Contains(ve))
                    sv.ScrollTo(ve);
            });
        }

        // Finds the best element in a direction (for gamepad/keyboard navigation)
        public static VisualElement FindNext(
            VisualElement current,
            Vector2 inputDir,
            IList<VisualElement> candidates,
            float maxAngleDeg = 60f,
            float minForwardPx = 4f)
        {
            if (current == null || candidates == null || candidates.Count == 0) return null;

            // Input System down is -1, but UI y+ goes down
            Vector2 dir = inputDir;
            if (dir.sqrMagnitude < 1e-6f) return null;
            dir.y = -dir.y;
            dir.Normalize();

            Vector2 fwd = dir;
            Vector2 right = new(dir.y, -dir.x);
            float cosMin = Mathf.Cos(maxAngleDeg * Mathf.Deg2Rad);

            Vector2 start = Center(current.worldBound);

            VisualElement best = null;
            float bestF = float.MaxValue; // forward distance
            float bestL = float.MaxValue; // lateral distance
            float bestD = float.MaxValue; // total distance

            for (int i = 0; i < candidates.Count; i++)
            {
                var c = candidates[i];
                if (c == null || c == current || !IsVisibleEnabled(c)) continue;

                Vector2 delta = (Vector2)(Center(c.worldBound) - start);
                float f = Vector2.Dot(fwd, delta);
                if (f < minForwardPx) continue; // must be in front

                float d = delta.magnitude;
                if (d < 1e-4f) continue;

                float cos = f / d;
                if (cos < cosMin) continue; // outside the cone

                float l = Mathf.Abs(Vector2.Dot(right, delta));

                // Sort by forward, then lateral, then total distance
                if (f < bestF - 0.0001f ||
                   (Mathf.Approximately(f, bestF) && (l < bestL - 0.0001f ||
                   (Mathf.Approximately(l, bestL) && d < bestD))))
                {
                    best = c; bestF = f; bestL = l; bestD = d;
                }
            }

            return best;
        }

        // Sets all four border colors at once
        public static void SetBorderColor(this VisualElement ve, Color c)
        {
            var sc = new StyleColor(c);
            ve.style.borderTopColor    = sc;
            ve.style.borderRightColor  = sc;
            ve.style.borderBottomColor = sc;
            ve.style.borderLeftColor   = sc;
        }

        // Clears the four border colors back to the stylesheet value
        public static void ResetBorderColor(this VisualElement ve)
        {
            ve.style.borderTopColor    = StyleKeyword.Null;
            ve.style.borderRightColor  = StyleKeyword.Null;
            ve.style.borderBottomColor = StyleKeyword.Null;
            ve.style.borderLeftColor   = StyleKeyword.Null;
        }

        // Highlights the border on hover, restores it on leave
        public static void ApplyBorderColorOnHover(this VisualElement ve, Color hover)
        {
            var c = ve.resolvedStyle.borderTopColor;
            ve.RegisterCallback<PointerEnterEvent>(_ => ve.SetBorderColor(hover));
            ve.RegisterCallback<PointerLeaveEvent>(_ => ve.SetBorderColor(c));
        }

        // Returns the center of a rect
        public static Vector2 Center(Rect r) => new(r.xMin + r.width * 0.5f, r.yMin + r.height * 0.5f);

        // True if the element is on screen and interactable
        static bool IsVisibleEnabled(VisualElement ve)
            => ve.panel != null && ve.enabledInHierarchy && ve.visible &&
               ve.resolvedStyle.display != DisplayStyle.None &&
               ve.worldBound.width > 0.5f && ve.worldBound.height > 0.5f;
    }
}