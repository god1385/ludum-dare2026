using LudumDare2026.Core.GameFlow;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace LudumDare2026.Core.Desktop
{
    public class GameExitFlowController : MonoBehaviour
    {
        [SerializeField] private Button _exitFlyButton;
        [SerializeField] private GameObject _quitConfirmRoot;
        [SerializeField] private Button _quitYesButton;
        [SerializeField] private Button _quitNoButton;
        [SerializeField] private GameObject _endGamePanelRoot;
        [SerializeField] private CanvasGroup _endGameCanvasGroup;
        [SerializeField] private TMP_Text _endingBodyText;
        [SerializeField] private Button _endExitButton;
        [SerializeField] private EndingTextsConfig _endingTexts;
        [SerializeField] private GameObject _neutralQuitStoryRoot;

        private IGameFlowPresentationModel _presentation;

        [Inject]
        private void Construct([InjectOptional] IGameFlowPresentationModel presentation) => _presentation = presentation;

        private void Awake()
        {
            _quitConfirmRoot?.SetActive(false);
            _endGamePanelRoot?.SetActive(false);
            ResetCanvasGroupHidden(_endGameCanvasGroup);
            _exitFlyButton?.onClick.AddListener(OnExitFlyClicked);
            _quitNoButton?.onClick.AddListener(OnQuitNoClicked);
            _quitYesButton?.onClick.AddListener(OnQuitYesClicked);
            _endExitButton?.onClick.AddListener(QuitApplication);
        }

        private void OnDestroy()
        {
            _exitFlyButton?.onClick.RemoveListener(OnExitFlyClicked);
            _quitNoButton?.onClick.RemoveListener(OnQuitNoClicked);
            _quitYesButton?.onClick.RemoveListener(OnQuitYesClicked);
            _endExitButton?.onClick.RemoveListener(QuitApplication);
        }

        public void ShowEndGame(EndingKind kind)
        {
            ApplyEndingText(kind);
            _endGamePanelRoot?.SetActive(true);
            ApplyEndGameCanvasGroupVisible();
        }

        private void OnExitFlyClicked() => _quitConfirmRoot?.SetActive(true);

        private void OnQuitNoClicked() => _quitConfirmRoot?.SetActive(false);

        private void OnQuitYesClicked()
        {
            _quitConfirmRoot?.SetActive(false);

            if (_presentation != null && _presentation.SuppressQuitEndingStoryText.Value)
                _neutralQuitStoryRoot?.SetActive(false);

            ShowEndGame(EndingKind.Neutral);
        }

        private void ApplyEndingText(EndingKind kind)
        {
            if (_endingBodyText == null)
                return;

            if (_endingTexts == null)
            {
                _endingBodyText.text = string.Empty;
                return;
            }

            if (kind == EndingKind.Neutral && _presentation != null && _presentation.SuppressQuitEndingStoryText.Value)
            {
                _endingBodyText.text = string.Empty;
                return;
            }

            _endingBodyText.text = _endingTexts.GetText(kind);
        }

        private void ApplyEndGameCanvasGroupVisible()
        {
            if (_endGameCanvasGroup == null)
                return;

            _endGameCanvasGroup.alpha = 1f;
            _endGameCanvasGroup.interactable = true;
            _endGameCanvasGroup.blocksRaycasts = true;
        }

        private static void ResetCanvasGroupHidden(CanvasGroup group)
        {
            if (group == null)
                return;

            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
        }

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
