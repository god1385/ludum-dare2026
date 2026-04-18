namespace LudumDare2026.Core.Desktop
{
    /// <summary>
    /// Matches a desktop icon to a <see cref="Windows.DesktopWindowSlot"/> on <see cref="Windows.DesktopWindow"/>.
    /// </summary>
    public enum DesktopAppKind
    {
        Terminal = 0,
        Decoder = 1,
        Hint = 2,
        Notebook = 3,
        Shop = 4,
    }
}
