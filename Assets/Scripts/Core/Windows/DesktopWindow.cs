using System;
using DG.Tweening;
using LudumDare2026.Core.Desktop;
using LudumDare2026.Utilities;
using UnityEngine;
using Zenject;

namespace LudumDare2026.Core.Windows
{
    [Serializable]
    public class DesktopWindowSlot
    {
        [SerializeField] private DesktopAppKind _appKind;
        [SerializeField] private GameObject _chromePrefab;
        [SerializeField] private string _taskbarCaption;
        [SerializeField] private bool _startClosed;

        public DesktopAppKind AppKind => _appKind;
        public GameObject ChromePrefab => _chromePrefab;
        public string TaskbarCaption => _taskbarCaption;
        public bool StartClosed => _startClosed;
    }

    public class DesktopWindow : MonoBehaviour
    {
        [SerializeField] private DesktopWindowSlot[] _slots;
        [SerializeField] private DesktopTaskbarController _taskbar;
        [SerializeField] private Transform _spawnParent;
        [SerializeField] private float _windowTweenDuration = 0.28f;
        [SerializeField] private float _minimizeEndScale = 0.12f;

        [SerializeField] private DesktopClockWidget _cornerClockWidget;

        private SlotState[] _runtime;
        private IWindowChromeTarget[] _chromeTargets;
        private DiContainer _container;

        [Inject]
        private void Construct([InjectOptional] DiContainer container) => _container = container;

        private void Awake()
        {
            if (_slots == null || _slots.Length == 0)
                return;

            _runtime = new SlotState[_slots.Length];
            _chromeTargets = new IWindowChromeTarget[_slots.Length];

            for (var i = 0; i < _slots.Length; i++)
            {
                _runtime[i] = new SlotState();
                if (!IsSlotConfigured(i))
                    continue;

                _chromeTargets[i] = new SlotCommandTarget(this, i);
            }
        }

        private void Start()
        {
            if (_slots == null)
                return;

            for (var i = 0; i < _slots.Length; i++)
            {
                if (!IsSlotConfigured(i))
                    continue;

                if (_slots[i].StartClosed)
                    continue;

                EnsureSpawned(i);
                ChromeRoot(i).SetActive(true);
                BindTaskbarButton(i);
            }
        }

        private void OnDestroy()
        {
            if (_runtime == null)
                return;

            for (var i = 0; i < _runtime.Length; i++)
            {
                var chrome = _runtime[i].SpawnedChrome;
                if (chrome == null)
                    continue;

                chrome.Unbind();
                chrome.LayoutRoot.DOKill();
                Destroy(chrome.gameObject);
            }
        }

        public void OpenOrRestoreFromIcon(DesktopAppKind appKind)
        {
            var windowIndex = FindSlotIndexForAppKind(appKind);
            if (windowIndex < 0)
            {
                Debug.LogWarning($"{nameof(DesktopWindow)}: no slot with {nameof(DesktopAppKind)}.{appKind}.", this);
                return;
            }

            OpenOrRestoreFromIcon(windowIndex);
        }

        public void OpenOrRestoreFromIcon(int windowIndex)
        {
            if (!IsValidIndex(windowIndex))
                return;

            EnsureSpawned(windowIndex);
            ChromeRoot(windowIndex).SetActive(true);
            CaptureSnapshotIfNeeded(windowIndex);
            BindTaskbarButton(windowIndex);
            PlayOpenTween(windowIndex, default(Vector3?));
        }

        /// <summary>
        /// Taskbar click: hide if the window is open (same as minimize), otherwise restore from taskbar.
        /// </summary>
        public void OnTaskbarButtonClicked(int windowIndex)
        {
            if (!IsValidIndex(windowIndex))
                return;

            EnsureSpawned(windowIndex);
            if (ChromeRoot(windowIndex).activeInHierarchy)
                RequestMinimize(windowIndex);
            else
                RestoreFromTaskbar(windowIndex);
        }

        public void RestoreFromTaskbar(int windowIndex)
        {
            if (!IsValidIndex(windowIndex))
                return;

            EnsureSpawned(windowIndex);

            Vector3? openFromWorld = _taskbar.TryGetTaskbarButtonWorldCenter(windowIndex, out var worldCenter)
                ? worldCenter
                : (Vector3?)null;

            ChromeRoot(windowIndex).SetActive(true);
            CaptureSnapshotIfNeeded(windowIndex);
            PlayOpenTween(windowIndex, openFromWorld);
        }

        private void RequestMinimize(int windowIndex)
        {
            if (!IsValidIndex(windowIndex))
                return;

            EnsureSpawned(windowIndex);
            Chrome(windowIndex).SetInteractable(false);
            var dock = BindTaskbarButton(windowIndex);
            PlayMinimizeTween(windowIndex, dock, () =>
            {
                ChromeRoot(windowIndex).SetActive(false);
                Chrome(windowIndex).SetInteractable(true);
            });
        }

        private void RequestClose(int windowIndex)
        {
            if (!IsValidIndex(windowIndex))
                return;

            EnsureSpawned(windowIndex);
            Chrome(windowIndex).SetInteractable(false);
            PlayMinimizeTween(windowIndex, null, () =>
            {
                ChromeRoot(windowIndex).SetActive(false);
                Chrome(windowIndex).SetInteractable(true);
                _taskbar.Unregister(windowIndex);
                _runtime[windowIndex].HasPreMinimizeLayout = false;
            });
        }

        private bool IsSlotConfigured(int windowIndex) =>
            _slots[windowIndex].ChromePrefab != null;

        private bool IsValidIndex(int windowIndex) =>
            _slots != null && windowIndex >= 0 && windowIndex < _slots.Length && IsSlotConfigured(windowIndex);

        private int FindSlotIndexForAppKind(DesktopAppKind appKind)
        {
            if (_slots == null)
                return -1;

            for (var i = 0; i < _slots.Length; i++)
            {
                if (!IsSlotConfigured(i))
                    continue;
                if (_slots[i].AppKind == appKind)
                    return i;
            }

            return -1;
        }

        /// <summary>
        /// True if this app&apos;s chrome was spawned and its layout root is active (window open on screen).
        /// </summary>
        public bool IsWindowChromeActiveInHierarchy(DesktopAppKind appKind)
        {
            var idx = FindSlotIndexForAppKind(appKind);
            if (idx < 0 || !IsValidIndex(idx))
                return false;

            if (_runtime[idx].SpawnedChrome == null)
                return false;

            return ChromeRoot(idx).activeInHierarchy;
        }

        /// <summary>Disable minimize / close / drag for one app (e.g. terminal during ending).</summary>
        public void SetAppWindowCommandsInteractable(DesktopAppKind appKind, bool interactable)
        {
            var idx = FindSlotIndexForAppKind(appKind);
            if (idx < 0 || !IsValidIndex(idx))
                return;

            if (_runtime[idx].SpawnedChrome == null)
                return;

            _runtime[idx].SpawnedChrome.SetWindowCommandsInteractable(interactable);
        }

        private void EnsureSpawned(int windowIndex)
        {
            var state = _runtime[windowIndex];
            if (state.SpawnedChrome != null)
                return;

            var slot = _slots[windowIndex];
            var instance = Instantiate(slot.ChromePrefab, _spawnParent);
            _container?.InjectGameObject(instance);
            state.SpawnedChrome = instance.GetComponent<DesktopWindowChrome>()
                ?? instance.GetComponentInChildren<DesktopWindowChrome>(true);

            var options = new DesktopWindowChromeOptions(slot.TaskbarCaption);
            state.SpawnedChrome.Bind(_chromeTargets[windowIndex], options);
            state.SnapshotCaptured = false;
            CaptureSnapshotIfNeeded(windowIndex);

            if (slot.AppKind == DesktopAppKind.Terminal)
            {
                var terminalView = instance.GetComponentInChildren<TerminalView>(true);
                terminalView?.BindDesktopServices(this, _cornerClockWidget);
            }
        }

        private RectTransform BindTaskbarButton(int windowIndex)
        {
            var chrome = _runtime[windowIndex].SpawnedChrome;
            var src = chrome != null
                ? chrome.GetComponentInChildren<IWindowTaskbarIconSource>(true)
                : null;

            return _taskbar.GetOrCreateTaskbarDock(
                windowIndex,
                _slots[windowIndex].TaskbarCaption,
                this,
                src?.TaskbarIcon,
                src?.TaskbarHighlightSprite,
                src?.TaskbarPressedSprite);
        }

        private DesktopWindowChrome Chrome(int i) => _runtime[i].SpawnedChrome;

        private RectTransform LayoutRoot(int i) => Chrome(i).LayoutRoot;

        private GameObject ChromeRoot(int i) => LayoutRoot(i).gameObject;

        private void ApplyEndLayoutBeforeOpenTween(int windowIndex)
        {
            var state = _runtime[windowIndex];
            if (state.HasPreMinimizeLayout)
                state.PreMinimizeLayout.Apply(LayoutRoot(windowIndex));
            else
                state.RestoredSnapshot.Apply(LayoutRoot(windowIndex));
        }

        private void PlayMinimizeTween(int windowIndex, RectTransform dockTarget, Action onComplete)
        {
            var root = LayoutRoot(windowIndex);
            root.DOKill();
            var state = _runtime[windowIndex];
            state.PreMinimizeLayout = WindowLayoutSnapshot.Capture(root);
            state.HasPreMinimizeLayout = true;

            var endAnchored = ResolveEndAnchoredFromDockOrFallback(windowIndex, dockTarget, root.anchoredPosition3D);
            var endScale = Vector3.one * _minimizeEndScale;

            var sequence = DOTween.Sequence();
            sequence.SetUpdate(true);
            sequence.Join(root.DOAnchorPos3D(endAnchored, _windowTweenDuration).SetEase(Ease.InCubic));
            sequence.Join(root.DOScale(endScale, _windowTweenDuration).SetEase(Ease.InCubic));
            sequence.OnComplete(() => onComplete?.Invoke());
        }

        private void PlayOpenTween(int windowIndex, Vector3? openFromWorld)
        {
            var root = LayoutRoot(windowIndex);
            root.DOKill();
            ApplyEndLayoutBeforeOpenTween(windowIndex);

            var targetAnchored = root.anchoredPosition3D;
            var targetScale = root.localScale;

            var startAnchored = openFromWorld.HasValue
                ? AnchoredFromWorldPoint(windowIndex, openFromWorld.Value)
                : targetAnchored + new Vector3(0f, -120f, 0f);

            root.anchoredPosition3D = startAnchored;
            root.localScale = Vector3.one * _minimizeEndScale;

            Chrome(windowIndex).SetInteractable(false);
            var sequence = DOTween.Sequence();
            sequence.SetUpdate(true);
            sequence.Join(root.DOAnchorPos3D(targetAnchored, _windowTweenDuration).SetEase(Ease.OutCubic));
            sequence.Join(root.DOScale(targetScale, _windowTweenDuration).SetEase(Ease.OutCubic));
            sequence.OnComplete(() => Chrome(windowIndex).SetInteractable(true));
        }

        private Vector3 ResolveEndAnchoredFromDockOrFallback(int windowIndex, RectTransform dockTarget, Vector3 fallbackSource)
        {
            if (dockTarget != null)
                return DockAnchoredFromDockRect(windowIndex, dockTarget);

            return fallbackSource + new Vector3(0f, -120f, 0f);
        }

        private Vector3 DockAnchoredFromDockRect(int windowIndex, RectTransform dock)
        {
            var root = LayoutRoot(windowIndex);
            var parent = root.parent as RectTransform;
            if (parent == null)
                return root.anchoredPosition3D;

            var canvas = root.GetComponentInParent<Canvas>();
            var cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            var world = dock.TransformPoint(dock.rect.center);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parent,
                RectTransformUtility.WorldToScreenPoint(cam, world),
                cam,
                out var local);
            return new Vector3(local.x, local.y, 0f);
        }

        private Vector3 AnchoredFromWorldPoint(int windowIndex, Vector3 worldPoint)
        {
            var root = LayoutRoot(windowIndex);
            var parent = root.parent as RectTransform;
            if (parent == null)
                return root.anchoredPosition3D;

            var canvas = root.GetComponentInParent<Canvas>();
            var cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parent,
                RectTransformUtility.WorldToScreenPoint(cam, worldPoint),
                cam,
                out var local);
            return new Vector3(local.x, local.y, 0f);
        }

        private void CaptureSnapshotIfNeeded(int windowIndex)
        {
            var state = _runtime[windowIndex];
            if (state.SnapshotCaptured)
                return;

            state.RestoredSnapshot = WindowLayoutSnapshot.Capture(LayoutRoot(windowIndex));
            state.SnapshotCaptured = true;
        }

        private class SlotState
        {
            public DesktopWindowChrome SpawnedChrome;
            public WindowLayoutSnapshot RestoredSnapshot;
            public bool SnapshotCaptured;
            public WindowLayoutSnapshot PreMinimizeLayout;
            public bool HasPreMinimizeLayout;
        }

        private class SlotCommandTarget : IWindowChromeTarget
        {
            private readonly DesktopWindow _owner;
            private readonly int _index;

            public SlotCommandTarget(DesktopWindow owner, int index)
            {
                _owner = owner;
                _index = index;
            }

            public void RequestMinimize() => _owner.RequestMinimize(_index);

            public void RequestClose() => _owner.RequestClose(_index);
        }
    }
}
