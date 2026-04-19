using System.Collections.Generic;
using LudumDare2026.Core.Desktop;
using LudumDare2026.Core.GameFlow;
using LudumDare2026.Core.Windows;
using UniRx;
using UnityEngine;
using Zenject;

namespace LudumDare2026.Core.Shop
{
    public class ShopView : MonoBehaviour, IWindowTaskbarIconSource
    {
        [SerializeField] private Transform _itemRoot;
        [SerializeField] private CursorItemView _itemPrefab;
        [SerializeField] private ShopCursorItemDefinition[] _cursorItems;

        [SerializeField] private Sprite _taskbarIcon;
        [SerializeField] private Sprite _taskbarHighlightSprite;
        [SerializeField] private Sprite _taskbarPressedSprite;
        [SerializeField] private AudioSource _purchaseAudioSource;
        [SerializeField] private AudioClip _purchaseSuccessClip;

        public Sprite TaskbarIcon => _taskbarIcon;
        public Sprite TaskbarHighlightSprite => _taskbarHighlightSprite;
        public Sprite TaskbarPressedSprite => _taskbarPressedSprite;

        private IPlayerWallet _wallet;
        private PlayerCursorInventory _inventory;
        private readonly CompositeDisposable _disposables = new CompositeDisposable();
        private readonly List<CursorItemView> _spawned = new List<CursorItemView>();

        [Inject]
        private void Construct(IPlayerWallet wallet, PlayerCursorInventory inventory)
        {
            _wallet = wallet;
            _inventory = inventory;
            BindReactiveStreamsIfReady();
        }

        private void OnEnable() => BindReactiveStreamsIfReady();

        private void OnDisable() => _disposables.Clear();

        private void BindReactiveStreamsIfReady()
        {
            if (_wallet == null || _inventory == null)
                return;

            _disposables.Clear();
            _wallet.Dollars.Subscribe(_ => Rebuild()).AddTo(_disposables);
            _inventory.Equipped.Subscribe(_ => Rebuild()).AddTo(_disposables);
            Rebuild();
        }

        private void OnDestroy() => _disposables.Dispose();

        private void Rebuild()
        {
            if (_wallet == null || _inventory == null)
                return;

            if (_itemRoot == null || _itemPrefab == null)
                return;

            for (var i = 0; i < _spawned.Count; i++)
            {
                if (_spawned[i] != null)
                    Destroy(_spawned[i].gameObject);
            }

            _spawned.Clear();

            var items = _cursorItems;
            if (items == null || items.Length == 0)
                return;

            var balance = _wallet.Dollars.Value;
            var equipped = _inventory.Equipped.Value;

            for (var i = 0; i < items.Length; i++)
            {
                var def = items[i];
                if (def == null)
                    continue;

                var row = Instantiate(_itemPrefab, _itemRoot);
                row.Bind(
                    def,
                    balance,
                    _inventory.IsOwned(def),
                    equipped == def,
                    OnBuyClicked,
                    OnEquipClicked);
                _spawned.Add(row);
            }
        }

        private void OnBuyClicked(CursorItemView row)
        {
            var def = row.Definition;
            if (def == null || _inventory.IsOwned(def))
                return;

            if (!_wallet.TrySpend(def.Price))
                return;

            _inventory.MarkOwned(def);
            _inventory.TryEquip(def);
            PlayPurchaseSuccess();
            Rebuild();
        }

        private void PlayPurchaseSuccess()
        {
            if (_purchaseAudioSource == null || _purchaseSuccessClip == null)
                return;

            _purchaseAudioSource.PlayOneShot(_purchaseSuccessClip);
        }

        private void OnEquipClicked(CursorItemView row)
        {
            var def = row.Definition;
            if (def == null || !_inventory.IsOwned(def))
                return;

            _inventory.TryEquip(def);
            Rebuild();
        }
    }
}
