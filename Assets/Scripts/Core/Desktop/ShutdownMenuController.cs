using UnityEngine;
using UnityEngine.UI;

namespace LudumDare2026.Core.Desktop
{
    public class ShutdownMenuController : MonoBehaviour
    {
        [SerializeField] private GameObject _menuRoot;
        [SerializeField] private Button _startButton;
        [SerializeField] private Button _shutdownButton;

        private void Awake()
        {
            _menuRoot.SetActive(false);
            _startButton.onClick.AddListener(ToggleMenu);
            _shutdownButton.onClick.AddListener(QuitApplication);
        }

        private void ToggleMenu() => _menuRoot.SetActive(!_menuRoot.activeSelf);

        private static void QuitApplication()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
