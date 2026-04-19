using UnityEngine;

namespace LudumDare2026.Core.Shop
{
    [CreateAssetMenu(menuName = "LudumDare2026/Shop/Cursor Item", fileName = "CursorItem")]
    public class ShopCursorItemDefinition : ScriptableObject
    {
        [SerializeField] private string _id;
        [Header("Cursor (Texture2D + hotspot — set import: Cursor or Default, Read/Write if needed)")]
        [SerializeField] private Texture2D _defaultCursorTexture;
        [SerializeField] private Vector2Int _defaultHotspot;
        [SerializeField] private Texture2D _clickCursorTexture;
        [SerializeField] private Vector2Int _clickHotspot;
        [Header("Shop row")]
        [Tooltip("Optional small icon in the list. If empty, a sprite is built once from Default Cursor Texture.")]
        [SerializeField] private Sprite _shopListIcon;
        [Header("Shop price tag (pre-made art; no text field in UI)")]
        [SerializeField] private Sprite _priceTagDefaultSprite;
        [SerializeField] private Sprite _priceTagPressedSprite;
        [SerializeField] private int _price = 1;
        [SerializeField] private AudioClip _clickSound;

        [System.NonSerialized]
        private Sprite _cachedShopListSprite;

        public string Id => string.IsNullOrEmpty(_id) ? name : _id;

        public Texture2D DefaultCursorTexture => _defaultCursorTexture;
        public Vector2Int DefaultHotspot => _defaultHotspot;

        public Texture2D ClickCursorTexture => _clickCursorTexture;
        public Vector2Int ClickHotspot => _clickHotspot;

        public int Price => _price;
        public AudioClip ClickSound => _clickSound;

        public Sprite PriceTagDefaultSprite => _priceTagDefaultSprite;
        public Sprite PriceTagPressedSprite => _priceTagPressedSprite;

        /// <summary>Icon for the shop row <see cref="UnityEngine.UI.Image"/>.</summary>
        public Sprite ShopListIconForUi
        {
            get
            {
                if (_shopListIcon != null)
                    return _shopListIcon;

                if (_defaultCursorTexture == null)
                    return null;

                if (_cachedShopListSprite == null)
                {
                    _cachedShopListSprite = Sprite.Create(
                        _defaultCursorTexture,
                        new Rect(0, 0, _defaultCursorTexture.width, _defaultCursorTexture.height),
                        new Vector2(0.5f, 0.5f),
                        100f);
                    _cachedShopListSprite.name = name + "_ShopList";
                }

                return _cachedShopListSprite;
            }
        }

        private void OnValidate()
        {
            _cachedShopListSprite = null;
        }
    }
}
