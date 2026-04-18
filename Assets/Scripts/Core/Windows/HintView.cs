using UnityEngine;

namespace LudumDare2026.Core.Windows
{
    /// <summary>Hint copy is authored on the prefab; game flow does not drive hint text.</summary>
    public class HintView : MonoBehaviour, IWindowTaskbarIconSource
    {
        [SerializeField] private Sprite _taskbarIcon;
        [SerializeField] private Sprite _taskbarHighlightSprite;
        [SerializeField] private Sprite _taskbarPressedSprite;

        public Sprite TaskbarIcon => _taskbarIcon;
        public Sprite TaskbarHighlightSprite => _taskbarHighlightSprite;
        public Sprite TaskbarPressedSprite => _taskbarPressedSprite;
    }
}
