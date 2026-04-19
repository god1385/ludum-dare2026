using UnityEngine;

namespace LudumDare2026.Utilities
{
    /// <summary>
    /// <see cref="UnityEngine.Cursor.SetCursor"/> needs a CPU-readable <see cref="Texture2D"/>.
    /// Atlas sprites often point at GPU-only textures — copy into a small RGBA32 texture when needed.
    /// </summary>
    public static class CursorTextureUtility
    {
        /// <summary>
        /// Copies sprite pixels into a new RGBA32 texture. Source atlas must be Read/Write enabled in import settings.
        /// </summary>
        public static Texture2D SpriteToValidCursorTexture(Sprite sprite)
        {
            if (sprite == null || sprite.texture == null)
                return null;

            var rect = sprite.textureRect;
            var w = Mathf.RoundToInt(rect.width);
            var h = Mathf.RoundToInt(rect.height);
            if (w <= 0 || h <= 0)
                return null;

            var texture = new Texture2D(w, h, TextureFormat.RGBA32, false, false)
            {
                name = sprite.name + "_CursorCopy",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave
            };

            try
            {
                var pixels = sprite.texture.GetPixels(
                    Mathf.RoundToInt(rect.x),
                    Mathf.RoundToInt(rect.y),
                    w,
                    h);

                texture.SetPixels(pixels);
            }
            catch (UnityException)
            {
                Object.Destroy(texture);
                Debug.LogWarning(
                    $"CursorTextureUtility: cannot read pixels from '{sprite.texture.name}'. Enable Read/Write on the texture import, or assign a standalone Texture2D for the cursor.",
                    sprite);
                return null;
            }

            // keep CPU copy: Cursor.SetCursor needs a readable texture; do not pass makeNoLongerReadable: true
            texture.Apply(false, false);
            return texture;
        }
    }
}
