using UnityEngine;

namespace Conn.Rendering.World
{
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class NpcWorldBillboard : MonoBehaviour
    {
        [SerializeField] private Sprite sprite;
        [SerializeField] private Color fallbackColor = new Color(0.78f, 0.68f, 0.52f, 1f);
        [SerializeField] private Vector2 size = new Vector2(1.25f, 1.8f);

        private SpriteRenderer spriteRenderer;
        private Sprite generatedFallbackSprite;

        public Sprite Sprite
        {
            get => sprite;
            set
            {
                sprite = value;
                ApplySprite();
            }
        }

        public Color FallbackColor
        {
            get => fallbackColor;
            set
            {
                fallbackColor = value;
                generatedFallbackSprite = null;
                ApplySprite();
            }
        }

        public Vector2 Size
        {
            get => size;
            set
            {
                size = value;
                ApplySize();
            }
        }

        private void Awake()
        {
            EnsureRenderer();
            ApplySprite();
            ApplySize();
        }

        private void OnValidate()
        {
            EnsureRenderer();
            ApplySprite();
            ApplySize();
        }

        private void LateUpdate()
        {
            var camera = Camera.main;
            if (camera == null)
            {
                return;
            }

            var toCamera = transform.position - camera.transform.position;
            toCamera.y = 0f;
            if (toCamera.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            transform.rotation = Quaternion.LookRotation(toCamera.normalized, Vector3.up);
        }

        private void EnsureRenderer()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }
        }

        private void ApplySprite()
        {
            EnsureRenderer();
            if (spriteRenderer == null)
            {
                return;
            }

            spriteRenderer.sprite = sprite != null ? sprite : FallbackSprite();
            spriteRenderer.sortingOrder = 0;
        }

        private void ApplySize()
        {
            transform.localScale = new Vector3(Mathf.Max(0.01f, size.x), Mathf.Max(0.01f, size.y), 1f);
        }

        private Sprite FallbackSprite()
        {
            if (generatedFallbackSprite != null)
            {
                return generatedFallbackSprite;
            }

            var texture = new Texture2D(16, 24, TextureFormat.RGBA32, false)
            {
                name = $"{name}_FallbackNpcSprite",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            var clear = new Color(0f, 0f, 0f, 0f);
            var pixels = new Color[16 * 24];
            for (var i = 0; i < pixels.Length; i++)
            {
                pixels[i] = clear;
            }

            FillRect(pixels, 16, 5, 2, 6, 6, fallbackColor);
            FillRect(pixels, 16, 3, 8, 10, 12, fallbackColor);
            FillRect(pixels, 16, 1, 10, 14, 3, new Color(0.12f, 0.1f, 0.09f, 1f));
            FillRect(pixels, 16, 5, 20, 6, 3, new Color(0.08f, 0.06f, 0.05f, 1f));
            texture.SetPixels(pixels);
            texture.Apply();

            generatedFallbackSprite = Sprite.Create(texture, new Rect(0f, 0f, 16f, 24f), new Vector2(0.5f, 0f), 24f);
            generatedFallbackSprite.name = $"{name}_FallbackNpcSprite";
            return generatedFallbackSprite;
        }

        private static void FillRect(Color[] pixels, int width, int x, int y, int w, int h, Color color)
        {
            for (var row = y; row < y + h; row++)
            {
                for (var column = x; column < x + w; column++)
                {
                    if (row < 0 || column < 0 || column >= width || row * width + column >= pixels.Length)
                    {
                        continue;
                    }

                    pixels[row * width + column] = color;
                }
            }
        }
    }
}
