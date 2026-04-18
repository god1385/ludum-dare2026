using System.Collections.Generic;
using LudumDare2026.Core.Windows;
using UnityEngine;
using UnityEngine.UI;

namespace LudumDare2026.Core.Desktop
{
    public class DesktopTaskbarController : MonoBehaviour
    {
        [SerializeField] private RectTransform _buttonContainer;
        [SerializeField] private TaskbarWindowButton _buttonPrefab;

        private readonly Dictionary<int, TaskbarWindowButton> _buttons = new Dictionary<int, TaskbarWindowButton>();

        public RectTransform GetOrCreateTaskbarDock(
            int windowSlotIndex,
            string taskbarCaption,
            DesktopWindow windowController,
            Sprite taskbarIcon,
            Sprite taskbarHighlightSprite,
            Sprite taskbarPressedSprite)
        {
            if (_buttons.TryGetValue(windowSlotIndex, out var existing))
            {
                existing.Bind(windowController, windowSlotIndex, taskbarCaption, taskbarIcon, taskbarHighlightSprite, taskbarPressedSprite);
                return (RectTransform)existing.transform;
            }

            var instance = Instantiate(_buttonPrefab, _buttonContainer);
            instance.Bind(windowController, windowSlotIndex, taskbarCaption, taskbarIcon, taskbarHighlightSprite, taskbarPressedSprite);
            _buttons[windowSlotIndex] = instance;
            LayoutRebuilder.ForceRebuildLayoutImmediate(_buttonContainer);
            Canvas.ForceUpdateCanvases();
            return (RectTransform)instance.transform;
        }

        public bool TryGetTaskbarButtonWorldCenter(int windowSlotIndex, out Vector3 worldCenter)
        {
            if (!_buttons.TryGetValue(windowSlotIndex, out var button))
            {
                worldCenter = default;
                return false;
            }

            var rect = (RectTransform)button.transform;
            worldCenter = rect.TransformPoint(rect.rect.center);
            return true;
        }

        public void Unregister(int windowSlotIndex)
        {
            if (!_buttons.TryGetValue(windowSlotIndex, out var button))
                return;

            _buttons.Remove(windowSlotIndex);
            Destroy(button.gameObject);
        }
    }
}
