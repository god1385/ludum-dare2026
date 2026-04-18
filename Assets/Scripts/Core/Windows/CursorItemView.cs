using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LudumDare2026.Core.Shop
{
    /// <summary>
    /// One row in the shop: icon from the cursor definition, price label, buy / equip / owned states.
    /// </summary>
    public sealed class CursorItemView : MonoBehaviour
    {
        private enum RowAction
        {
            None,
            Buy,
            Equip,
        }

        [SerializeField] private Image _icon;
        [SerializeField] private TMP_Text _priceLabel;
        [SerializeField] private Button _actionButton;
        [SerializeField] private TMP_Text _actionLabel;

        private ShopCursorItemDefinition _definition;
        private RowAction _rowAction;
        private System.Action<CursorItemView> _onBuy;
        private System.Action<CursorItemView> _onEquip;

        private void Awake()
        {
            if (_actionButton != null)
                _actionButton.onClick.AddListener(OnActionClicked);
        }

        private void OnDestroy()
        {
            if (_actionButton != null)
                _actionButton.onClick.RemoveListener(OnActionClicked);
        }

        public ShopCursorItemDefinition Definition => _definition;

        public void Bind(
            ShopCursorItemDefinition definition,
            int balance,
            bool owned,
            bool equipped,
            System.Action<CursorItemView> onBuy,
            System.Action<CursorItemView> onEquip)
        {
            _definition = definition;
            _onBuy = onBuy;
            _onEquip = onEquip;

            if (_icon != null)
                _icon.sprite = definition != null ? definition.DefaultCursorIcon : null;

            if (_priceLabel != null)
                _priceLabel.text = definition != null ? $"${definition.Price}" : string.Empty;

            if (_actionButton == null || _actionLabel == null)
                return;

            if (definition == null)
            {
                _rowAction = RowAction.None;
                _actionButton.interactable = false;
                _actionLabel.text = string.Empty;
                return;
            }

            if (!owned)
            {
                _rowAction = RowAction.Buy;
                _actionLabel.text = "Buy";
                _actionButton.interactable = balance >= definition.Price;
            }
            else if (!equipped)
            {
                _rowAction = RowAction.Equip;
                _actionLabel.text = "Equip";
                _actionButton.interactable = true;
            }
            else
            {
                _rowAction = RowAction.None;
                _actionLabel.text = "Equipped";
                _actionButton.interactable = false;
            }
        }

        private void OnActionClicked()
        {
            switch (_rowAction)
            {
                case RowAction.Buy:
                    _onBuy?.Invoke(this);
                    break;
                case RowAction.Equip:
                    _onEquip?.Invoke(this);
                    break;
            }
        }
    }
}
