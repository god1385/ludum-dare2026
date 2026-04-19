using UnityEngine;
using UnityEngine.UI;

namespace LudumDare2026.Core.Shop
{
    /// <summary>
    /// One shop row: cursor preview icon + price tag <see cref="Button"/> (art from SO includes the price).
    /// </summary>
    public class CursorItemView : MonoBehaviour
    {
        private enum RowAction
        {
            None,
            Buy,
            Equip,
        }

        [SerializeField] private Image _icon;
        [SerializeField] private Button _priceObj;

        private ShopCursorItemDefinition _definition;
        private RowAction _rowAction;
        private System.Action<CursorItemView> _onBuy;
        private System.Action<CursorItemView> _onEquip;

        private void Awake()
        {
            if (_priceObj != null)
                _priceObj.onClick.AddListener(OnPriceClicked);
        }

        private void OnDestroy()
        {
            if (_priceObj != null)
                _priceObj.onClick.RemoveListener(OnPriceClicked);
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
                _icon.sprite = definition != null ? definition.ShopListIconForUi : null;

            if (_priceObj == null)
                return;

            if (definition == null)
            {
                _rowAction = RowAction.None;
                _priceObj.interactable = false;
                ApplyPriceTagSprites(null, null);
                return;
            }

            _priceObj.transition = Selectable.Transition.SpriteSwap;
            ApplyPriceTagSprites(definition.PriceTagDefaultSprite, definition.PriceTagPressedSprite);

            if (!owned)
            {
                _rowAction = RowAction.Buy;
                _priceObj.interactable = balance >= definition.Price;
            }
            else if (!equipped)
            {
                _rowAction = RowAction.Equip;
                _priceObj.interactable = true;
            }
            else
            {
                _rowAction = RowAction.None;
                _priceObj.interactable = false;
            }
        }

        private void ApplyPriceTagSprites(Sprite defaultSprite, Sprite pressedSprite)
        {
            var graphic = _priceObj.targetGraphic as Image;
            if (graphic != null)
                graphic.sprite = defaultSprite;

            var state = _priceObj.spriteState;
            var normal = defaultSprite;
            state.highlightedSprite = normal;
            state.pressedSprite = pressedSprite != null ? pressedSprite : normal;
            state.selectedSprite = normal;
            state.disabledSprite = normal;
            _priceObj.spriteState = state;
        }

        private void OnPriceClicked()
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
