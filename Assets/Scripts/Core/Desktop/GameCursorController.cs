using System.Collections.Generic;
using LudumDare2026.Core.Shop;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Zenject;

namespace LudumDare2026.Core.Desktop
{
    /// <summary>
    /// Software cursor + hover sprite over common UI click targets + click sound from equipped skin.
    /// Place one instance in the scene; assign default sprites and optional default click sound.
    /// </summary>
    public sealed class GameCursorController : MonoBehaviour
    {
        [SerializeField] private Sprite _defaultCursor;
        [SerializeField] private Sprite _defaultCursorClick;
        [SerializeField] private AudioClip _defaultClickSound;
        [Tooltip("Optional: assign a dedicated AudioSource on the scene for UI clicks. If empty, one is added on this object.")]
        [SerializeField] private AudioSource _clickAudioSource;

        private PlayerCursorInventory _inventory;
        private AudioSource _audio;
        private readonly List<RaycastResult> _raycastResults = new List<RaycastResult>(32);
        private Sprite _lastAppliedSprite;
        private ShopCursorItemDefinition _equippedSkin;

        [Inject]
        private void Construct(PlayerCursorInventory inventory)
        {
            _inventory = inventory;
        }

        private void Awake()
        {
            _audio = _clickAudioSource != null ? _clickAudioSource : GetComponent<AudioSource>();
            if (_audio == null)
                _audio = gameObject.AddComponent<AudioSource>();

            _audio.playOnAwake = false;
        }

        private void Start()
        {
            _inventory.Equipped.Subscribe(OnEquippedChanged).AddTo(this);
        }

        private void OnEquippedChanged(ShopCursorItemDefinition skin)
        {
            _equippedSkin = skin;
            _lastAppliedSprite = null;
        }

        private void LateUpdate()
        {
            if (WasPrimaryClickPressedThisFrame())
            {
                var clip = _equippedSkin != null && _equippedSkin.ClickSound != null
                    ? _equippedSkin.ClickSound
                    : _defaultClickSound;

                if (clip != null)
                    _audio.PlayOneShot(clip);
            }

            var overClickable = IsPointerOverClickableUi();
            var normal = _equippedSkin != null ? _equippedSkin.DefaultCursorIcon : _defaultCursor;
            var hover = _equippedSkin != null ? _equippedSkin.ClickCursorIcon : _defaultCursorClick;
            var target = overClickable ? hover : normal;
            if (target == _lastAppliedSprite)
                return;

            ApplySoftwareCursor(target);
            _lastAppliedSprite = target;
        }

        private static bool WasPrimaryClickPressedThisFrame()
        {
            var mouse = Mouse.current;
            return mouse != null && mouse.leftButton.wasPressedThisFrame;
        }

        private bool IsPointerOverClickableUi()
        {
            var es = EventSystem.current;
            if (es == null)
                return false;

            var mouse = Mouse.current;
            if (mouse == null)
                return false;

            _raycastResults.Clear();
            var screenPos = mouse.position.ReadValue();
            var data = new PointerEventData(es) { position = screenPos };
            es.RaycastAll(data, _raycastResults);
            if (_raycastResults.Count == 0)
                return false;

            for (var i = 0; i < _raycastResults.Count; i++)
            {
                var go = _raycastResults[i].gameObject;
                if (go == null)
                    continue;

                if (IsClickableUiObject(go))
                    return true;
            }

            return false;
        }

        private static bool IsClickableUiObject(GameObject go)
        {
            if (go.GetComponentInParent<TaskbarWindowButton>(true) != null)
                return true;
            if (go.GetComponentInParent<DesktopIcon>(true) != null)
                return true;
            if (go.GetComponentInParent<TMP_InputField>(true) != null)
                return true;
            if (go.GetComponentInParent<Button>(true) != null)
                return true;

            return false;
        }

        private static void ApplySoftwareCursor(Sprite sprite)
        {
            if (sprite == null)
            {
                global::UnityEngine.Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
                return;
            }

            var hotspot = new Vector2Int(
                Mathf.RoundToInt(sprite.pivot.x),
                Mathf.RoundToInt(sprite.rect.height - sprite.pivot.y));

            global::UnityEngine.Cursor.SetCursor(sprite.texture, hotspot, CursorMode.Auto);
        }
    }
}
