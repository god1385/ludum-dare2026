using UnityEngine;

namespace LudumDare2026.Core.Shop
{
    [CreateAssetMenu(menuName = "LudumDare2026/Shop/Cursor Item", fileName = "CursorItem")]
    public sealed class ShopCursorItemDefinition : ScriptableObject
    {
        [SerializeField] private string _id;
        [SerializeField] private Sprite _defaultCursorIcon;
        [SerializeField] private Sprite _clickCursorIcon;
        [SerializeField] private int _price = 1;
        [SerializeField] private AudioClip _clickSound;

        public string Id => string.IsNullOrEmpty(_id) ? name : _id;
        public Sprite DefaultCursorIcon => _defaultCursorIcon;
        public Sprite ClickCursorIcon => _clickCursorIcon;
        public int Price => _price;
        public AudioClip ClickSound => _clickSound;
    }
}
