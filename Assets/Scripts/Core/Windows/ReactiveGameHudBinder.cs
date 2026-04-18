using LudumDare2026.Core.GameFlow;
using TMPro;
using UniRx;
using UnityEngine;
using Zenject;

namespace LudumDare2026.Core.Windows
{
    public class ReactiveGameHudBinder : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _statusLabel;

        private CompositeDisposable _disposables;

        [Inject]
        private void Construct(IGameFlowPresentationModel presentation)
        {
            if (_statusLabel == null)
            {
                Debug.LogError($"{nameof(ReactiveGameHudBinder)}: assign Status Label.", this);
                return;
            }

            _disposables = new CompositeDisposable();
            presentation.Status.Subscribe(t => _statusLabel.text = t).AddTo(_disposables);
        }

        private void OnDestroy() => _disposables?.Dispose();
    }
}
