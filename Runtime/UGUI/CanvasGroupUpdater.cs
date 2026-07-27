using UnityEngine;

namespace MagicUtilities
{
    public class CanvasGroupUpdater : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private float _lerpSpeed;

        private bool _value;

        private void Start()
        {
            _canvasGroup.alpha = GetAlphaTarget();
        }

        private void Update()
        {
            UpdateAlpha();
        }

        private void UpdateAlpha()
        {
            _canvasGroup.alpha = Mathf.Lerp(_canvasGroup.alpha, GetAlphaTarget(), Time.unscaledDeltaTime * _lerpSpeed);
            _canvasGroup.interactable = _value;
            _canvasGroup.blocksRaycasts = _value;
        }

        private float GetAlphaTarget()
        {
            return _value ? 1f : 0f;
        }

        public void UpdateValue(bool v)
        {
            _value = v;
        }
    }
}