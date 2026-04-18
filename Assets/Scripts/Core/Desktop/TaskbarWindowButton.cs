using LudumDare2026.Core.Windows;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LudumDare2026.Core.Desktop
{
    public class TaskbarWindowButton : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private TextMeshProUGUI _captionLabel;
        [SerializeField] private Image _iconImage;

        public void Bind(DesktopWindow windowController, int windowSlotIndex, string caption, Sprite icon, Sprite highlightSprite, Sprite pressedSprite)
        {
            _captionLabel.text = caption;
            if (_iconImage != null)
            {
                _iconImage.sprite = icon;
                _iconImage.enabled = icon != null || highlightSprite != null || pressedSprite != null;
            }

            var state = _button.spriteState;
            state.highlightedSprite = highlightSprite != null ? highlightSprite : icon;
            state.pressedSprite = pressedSprite != null ? pressedSprite : icon;
            _button.spriteState = state;

            _button.onClick.RemoveAllListeners();
            _button.onClick.AddListener(() => windowController.OnTaskbarButtonClicked(windowSlotIndex));
        }
    }
}
