using System.Collections;
using DG.Tweening;
using LudumDare2026.Core.GameFlow;
using TMPro;
using UniRx;
using UnityEngine;
using Zenject;

namespace LudumDare2026.Core.Desktop
{
    public class DesktopClockWidget : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _label;

        private IGameFlowPresentationModel _presentation;
        private Tween _clockTween;

        [Inject]
        private void Construct(IGameFlowPresentationModel presentation) => _presentation = presentation;

        private void Start()
        {
            if (_label == null)
                return;

            _presentation.ClockDisplayText.Subscribe(t => _label.text = t).AddTo(this);
        }

        private void OnDestroy() => _clockTween?.Kill();

        public IEnumerator AnimateToNinetyNine(float durationSeconds)
        {
            if (_label == null)
                yield break;

            var start = ParseComparable(_label.text);
            var end = 99 * 100 + 99;
            if (start >= end)
                yield break;

            var done = false;
            _clockTween?.Kill();
            _clockTween = DOTween
                .To(() => (float)start, v =>
                {
                    var i = Mathf.RoundToInt(v);
                    _label.text = FormatComparable(i);
                }, end, durationSeconds)
                .SetEase(Ease.Linear)
                .SetUpdate(true)
                .OnComplete(() => done = true);

            while (!done)
                yield return null;

            _clockTween = null;
        }

        private static int ParseComparable(string s)
        {
            if (string.IsNullOrEmpty(s))
                return 0;

            var parts = s.Split(':');
            if (parts.Length < 2)
                return 0;

            var m = int.TryParse(parts[0].Trim(), out var mm) ? mm : 0;
            var sec = int.TryParse(parts[1].Trim(), out var ss) ? ss : 0;
            m = Mathf.Clamp(m, 0, 99);
            sec = Mathf.Clamp(sec, 0, 99);
            return m * 100 + sec;
        }

        private static string FormatComparable(int v)
        {
            var m = v / 100;
            var s = v % 100;
            return $"{m:00}:{s:00}";
        }
    }
}
