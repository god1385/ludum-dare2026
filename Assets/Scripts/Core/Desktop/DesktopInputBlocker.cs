using LudumDare2026.Core.GameFlow;
using UniRx;
using UnityEngine;
using Zenject;

namespace LudumDare2026.Core.Desktop
{
    public class DesktopInputBlocker : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _canvasGroup;

        private IGameFlowPresentationModel _presentation;

        [Inject]
        private void Construct(IGameFlowPresentationModel presentation) => _presentation = presentation;

        private void Start()
        {
            if (_canvasGroup == null)
                return;

            _presentation.BlockAllPlayerInput.Subscribe(OnBlocked).AddTo(this);
        }

        private void OnBlocked(bool blocked)
        {
            _canvasGroup.interactable = !blocked;
            _canvasGroup.blocksRaycasts = !blocked;
        }
    }
}
