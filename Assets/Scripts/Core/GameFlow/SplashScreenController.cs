using System;
using UnityEngine;
using UnityEngine.UI;

namespace LudumDare2026.Core.GameFlow
{
    /// <summary>
    /// Full-screen start UI: plays the startup clip on load, then on confirm hides this root and notifies <see cref="GameFlowController"/>.
    /// </summary>
    public class SplashScreenController : MonoBehaviour
    {
        [SerializeField] private Button _startButton;
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private AudioClip _startupClip;

        private bool _dismissed;

        /// <summary>Fired once after the player confirms the start button; subscribers should start the main game loop.</summary>
        public event Action Dismissed;

        private void Awake()
        {
            if (_startButton != null)
                _startButton.onClick.AddListener(OnStartButtonClicked);
        }

        private void Start()
        {
            if (_audioSource != null && _startupClip != null)
                _audioSource.PlayOneShot(_startupClip);
        }

        private void OnDestroy()
        {
            if (_startButton != null)
                _startButton.onClick.RemoveListener(OnStartButtonClicked);
        }

        /// <summary>
        /// Call from the UI button OnClick, or leave <see cref="_startButton"/> assigned and wiring is automatic in <see cref="Awake"/>.
        /// </summary>
        public void OnStartButtonClicked() => TryDismiss();

        private void TryDismiss()
        {
            if (_dismissed)
                return;

            _dismissed = true;

            Dismissed?.Invoke();
            gameObject.SetActive(false);
        }
    }
}

