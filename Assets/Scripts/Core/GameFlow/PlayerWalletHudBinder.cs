using TMPro;
using UniRx;
using UnityEngine;
using Zenject;

namespace LudumDare2026.Core.GameFlow
{
    /// <summary>
    /// Binds <see cref="IPlayerWallet.Dollars"/> to a TMP label (e.g. shop HUD or taskbar counter).
    /// </summary>
    public class PlayerWalletHudBinder : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _dollarsLabel;
        [SerializeField] private string _format = "{0}";

        private CompositeDisposable _disposables;

        [Inject]
        private void Construct(IPlayerWallet wallet)
        {
            if (_dollarsLabel == null)
            {
                Debug.LogError($"{nameof(PlayerWalletHudBinder)}: assign Dollars Label.", this);
                return;
            }

            _disposables = new CompositeDisposable();
            wallet.Dollars.Subscribe(v => _dollarsLabel.text = string.Format(_format, v)).AddTo(_disposables);
        }

        private void OnDestroy() => _disposables?.Dispose();
    }
}
