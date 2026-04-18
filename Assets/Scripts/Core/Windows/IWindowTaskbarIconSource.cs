using UnityEngine;

namespace LudumDare2026.Core.Windows
{
    public interface IWindowTaskbarIconSource
    {
        Sprite TaskbarIcon { get; }
        Sprite TaskbarHighlightSprite { get; }
        Sprite TaskbarPressedSprite { get; }
    }
}
