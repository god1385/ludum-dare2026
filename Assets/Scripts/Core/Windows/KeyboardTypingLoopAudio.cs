using System.Collections;
using TMPro;
using UnityEngine;

namespace LudumDare2026.Core.Windows
{
    /// <summary>
    /// Plays a long typing ambience clip while the player edits a <see cref="TMP_InputField"/>;
    /// stops playback after <see cref="_idleStopSeconds"/> without value changes (and on deselect / end edit).
    /// </summary>
    public class KeyboardTypingLoopAudio : MonoBehaviour
    {
        [SerializeField] private TMP_InputField _field;
        [SerializeField] private AudioClip _clip;
        [SerializeField] private float _idleStopSeconds = 0.5f;
        [SerializeField] private AudioSource _audioSource;

        private TMP_InputField _wiredField;
        private Coroutine _idleRoutine;

        public static void Ensure(TMP_InputField field, AudioClip clip, float idleStopSeconds = 0.5f, AudioSource audioSource = null)
        {
            if (field == null || clip == null)
                return;

            var c = field.GetComponent<KeyboardTypingLoopAudio>();
            if (c == null)
                c = field.gameObject.AddComponent<KeyboardTypingLoopAudio>();

            c.Configure(field, clip, idleStopSeconds, audioSource);
        }

        public void Configure(TMP_InputField field, AudioClip clip, float idleStopSeconds = 0.5f, AudioSource audioSource = null)
        {
            Unwire();
            _field = field;
            _clip = clip;
            _idleStopSeconds = idleStopSeconds;
            _audioSource = audioSource;
            WireIfReady();
        }

        private void Awake()
        {
            if (_field != null && _clip != null)
                WireIfReady();
        }

        private void OnDestroy() => Unwire();

        private void WireIfReady()
        {
            if (_field == null || _clip == null || _wiredField != null)
                return;

            _wiredField = _field;
            EnsureSource();
            _wiredField.onValueChanged.AddListener(OnValueChanged);
            _wiredField.onDeselect.AddListener(OnDeselect);
            _wiredField.onEndEdit.AddListener(OnEndEdit);
        }

        private void Unwire()
        {
            StopIdleRoutine();
            StopPlayback();

            if (_wiredField == null)
                return;

            _wiredField.onValueChanged.RemoveListener(OnValueChanged);
            _wiredField.onDeselect.RemoveListener(OnDeselect);
            _wiredField.onEndEdit.RemoveListener(OnEndEdit);
            _wiredField = null;
        }

        private void EnsureSource()
        {
            if (_audioSource == null)
            {
                _audioSource = GetComponent<AudioSource>();
                if (_audioSource == null)
                    _audioSource = gameObject.AddComponent<AudioSource>();
            }

            _audioSource.playOnAwake = false;
            _audioSource.loop = false;
            _audioSource.clip = _clip;
        }

        private void OnValueChanged(string _)
        {
            if (_field == null || _clip == null || !_field.isFocused)
                return;

            RestartIdleStop();

            if (_audioSource != null && !_audioSource.isPlaying)
                _audioSource.Play();
        }

        private void OnDeselect(string _) => StopAll();

        private void OnEndEdit(string _) => StopAll();

        private void StopAll()
        {
            StopIdleRoutine();
            StopPlayback();
        }

        private void StopPlayback()
        {
            if (_audioSource != null && _audioSource.isPlaying)
                _audioSource.Stop();
        }

        private void RestartIdleStop()
        {
            StopIdleRoutine();
            if (isActiveAndEnabled)
                _idleRoutine = StartCoroutine(IdleStopRoutine());
        }

        private void StopIdleRoutine()
        {
            if (_idleRoutine == null)
                return;

            StopCoroutine(_idleRoutine);
            _idleRoutine = null;
        }

        private IEnumerator IdleStopRoutine()
        {
            yield return new WaitForSecondsRealtime(_idleStopSeconds);
            _idleRoutine = null;
            StopPlayback();
        }
    }
}
