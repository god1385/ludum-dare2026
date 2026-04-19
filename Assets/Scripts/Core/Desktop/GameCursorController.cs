using System.Collections.Generic;
using LudumDare2026.Core.GameFlow;
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
    public class GameCursorController : MonoBehaviour
    {
        [Header("Default cursor (when no shop skin is equipped)")]
        [SerializeField] private Texture2D _defaultCursorTexture;
        [SerializeField] private Vector2Int _defaultCursorHotspot;
        [SerializeField] private Texture2D _defaultCursorClickTexture;
        [SerializeField] private Vector2Int _defaultCursorClickHotspot;

        [SerializeField] private AudioClip _defaultClickSound;
        [Tooltip("Optional: assign a dedicated AudioSource on the scene for UI clicks. If empty, one is added on this object.")]
        [SerializeField] private AudioSource _clickAudioSource;

        private PlayerCursorInventory _inventory;
        private IGameFlowPresentationModel _presentation;
        private AudioSource _audio;
        private readonly List<RaycastResult> _raycastResults = new List<RaycastResult>(32);
        private ShopCursorItemDefinition _equippedSkin;

        private Texture2D _lastTexture;
        private Vector2Int _lastHotspot;
        private bool _lastOverClickable;

        [Inject]
        private void Construct(PlayerCursorInventory inventory, [InjectOptional] IGameFlowPresentationModel presentation)
        {
            _inventory = inventory;
            _presentation = presentation;
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
            _lastTexture = null;
            _lastHotspot = default;
            _lastOverClickable = false;
        }

        private void LateUpdate()
        {
            if (_presentation != null && _presentation.BlockAllPlayerInput.Value)
                return;

            if (WasPrimaryClickPressedThisFrame())
            {
                var clip = _equippedSkin != null && _equippedSkin.ClickSound != null
                    ? _equippedSkin.ClickSound
                    : _defaultClickSound;

                if (clip != null)
                    _audio.PlayOneShot(clip);
            }

            var overClickable = IsPointerOverClickableUi();

            Texture2D tex;
            Vector2Int hotspot;

            if (_equippedSkin != null)
            {
                if (overClickable && _equippedSkin.ClickCursorTexture != null)
                {
                    tex = _equippedSkin.ClickCursorTexture;
                    hotspot = _equippedSkin.ClickHotspot;
                }
                else
                {
                    tex = _equippedSkin.DefaultCursorTexture;
                    hotspot = _equippedSkin.DefaultHotspot;
                }
            }
            else
            {
                if (overClickable && _defaultCursorClickTexture != null)
                {
                    tex = _defaultCursorClickTexture;
                    hotspot = _defaultCursorClickHotspot;
                }
                else
                {
                    tex = _defaultCursorTexture;
                    hotspot = _defaultCursorHotspot;
                }
            }

            if (tex == _lastTexture && hotspot == _lastHotspot && overClickable == _lastOverClickable)
                return;

            ApplySoftwareCursor(tex, hotspot);
            _lastTexture = tex;
            _lastHotspot = hotspot;
            _lastOverClickable = overClickable;
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

        private static void ApplySoftwareCursor(Texture2D texture, Vector2Int hotspot)
        {
            if (texture == null)
            {
                global::UnityEngine.Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
                return;
            }

            global::UnityEngine.Cursor.SetCursor(texture, (Vector2)hotspot, CursorMode.Auto);
        }
    }
}
