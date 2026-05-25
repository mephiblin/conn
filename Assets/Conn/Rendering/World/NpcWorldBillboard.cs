using UnityEngine;

namespace Conn.Rendering.World
{
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public sealed class NpcWorldBillboard : MonoBehaviour
    {
        [SerializeField] private Texture2D texture;
        [SerializeField] private Color fallbackColor = new Color(0.78f, 0.68f, 0.52f, 1f);
        [SerializeField] private float height = 1.7f;
        [SerializeField] private bool faceCamera = true;
        [SerializeField] private float maxYawDegrees = 55f;
        [SerializeField] private float rotationSpeedDegrees = 180f;
        [SerializeField] private float recoverySpeedDegrees = 120f;

        private static Mesh quadMesh;
        private static Material sharedTransparentMaterial;

        private MeshRenderer meshRenderer;
        private MeshFilter meshFilter;
        private Texture2D generatedFallbackTexture;
        private MaterialPropertyBlock propertyBlock;
        private Quaternion restLocalRotation;

        public Texture2D Texture
        {
            get => texture;
            set
            {
                texture = value;
                ApplyVisual();
            }
        }

        public Color FallbackColor
        {
            get => fallbackColor;
            set
            {
                fallbackColor = value;
                generatedFallbackTexture = null;
                ApplyVisual();
            }
        }

        public float Height
        {
            get => height;
            set
            {
                height = value;
                ApplySize();
            }
        }

        public bool FaceCamera
        {
            get => faceCamera;
            set => faceCamera = value;
        }

        public float MaxYawDegrees
        {
            get => maxYawDegrees;
            set => maxYawDegrees = Mathf.Max(0f, value);
        }

        public float RotationSpeedDegrees
        {
            get => rotationSpeedDegrees;
            set => rotationSpeedDegrees = Mathf.Max(0f, value);
        }

        public float RecoverySpeedDegrees
        {
            get => recoverySpeedDegrees;
            set => recoverySpeedDegrees = Mathf.Max(0f, value);
        }

        private void Awake()
        {
            restLocalRotation = transform.localRotation;
            EnsureComponents();
            ApplyVisual();
            ApplySize();
        }

        private void OnValidate()
        {
            EnsureComponents();
            ApplyVisual();
            ApplySize();
        }

        private void LateUpdate()
        {
            if (!faceCamera)
            {
                RecoverToRestRotation();
                return;
            }

            var camera = Camera.main;
            if (camera == null)
            {
                RecoverToRestRotation();
                return;
            }

            var parent = transform.parent;
            var localCamera = parent != null
                ? parent.InverseTransformPoint(camera.transform.position)
                : camera.transform.position;
            localCamera.y = 0f;
            if (localCamera.sqrMagnitude <= 0.0001f)
            {
                RecoverToRestRotation();
                return;
            }

            var desiredYaw = Mathf.Atan2(-localCamera.x, -localCamera.z) * Mathf.Rad2Deg;
            if (Mathf.Abs(Mathf.DeltaAngle(0f, desiredYaw)) > maxYawDegrees)
            {
                RecoverToRestRotation();
                return;
            }

            var target = restLocalRotation * Quaternion.Euler(0f, desiredYaw, 0f);
            transform.localRotation = Quaternion.RotateTowards(
                transform.localRotation,
                target,
                Mathf.Max(0f, rotationSpeedDegrees) * Time.deltaTime);
        }

        private void RecoverToRestRotation()
        {
            transform.localRotation = Quaternion.RotateTowards(
                transform.localRotation,
                restLocalRotation,
                Mathf.Max(0f, recoverySpeedDegrees) * Time.deltaTime);
        }

        private void EnsureComponents()
        {
            if (meshFilter == null)
            {
                meshFilter = GetComponent<MeshFilter>();
            }

            if (meshRenderer == null)
            {
                meshRenderer = GetComponent<MeshRenderer>();
            }

            if (meshFilter != null && meshFilter.sharedMesh == null)
            {
                meshFilter.sharedMesh = QuadMesh();
            }

            if (meshRenderer != null)
            {
                if (meshRenderer.sharedMaterial == null)
                {
                    meshRenderer.sharedMaterial = TransparentMaterial();
                }

                meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                meshRenderer.receiveShadows = false;
            }
        }

        private void ApplyVisual()
        {
            EnsureComponents();
            if (meshRenderer == null)
            {
                return;
            }

            propertyBlock ??= new MaterialPropertyBlock();
            meshRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetTexture("_MainTex", texture != null ? texture : FallbackTexture());
            propertyBlock.SetColor("_Color", Color.white);
            meshRenderer.SetPropertyBlock(propertyBlock);
            ApplySize();
        }

        private void ApplySize()
        {
            var source = texture != null ? texture : FallbackTexture();
            var aspect = source != null && source.height > 0 ? (float)source.width / source.height : 1f;
            var worldHeight = Mathf.Max(0.01f, height);
            var worldWidth = worldHeight * aspect;
            var parentScale = transform.parent != null ? transform.parent.lossyScale : Vector3.one;

            transform.localScale = new Vector3(
                worldWidth / Mathf.Max(0.01f, Mathf.Abs(parentScale.x)),
                worldHeight / Mathf.Max(0.01f, Mathf.Abs(parentScale.y)),
                1f / Mathf.Max(0.01f, Mathf.Abs(parentScale.z)));
        }

        private Texture2D FallbackTexture()
        {
            if (generatedFallbackTexture != null)
            {
                return generatedFallbackTexture;
            }

            generatedFallbackTexture = new Texture2D(16, 24, TextureFormat.RGBA32, false)
            {
                name = $"{name}_FallbackNpcTexture",
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
            generatedFallbackTexture.SetPixels(pixels);
            generatedFallbackTexture.Apply();
            return generatedFallbackTexture;
        }

        private static Mesh QuadMesh()
        {
            if (quadMesh != null)
            {
                return quadMesh;
            }

            quadMesh = new Mesh
            {
                name = "NpcWorldBillboardQuad",
                vertices = new[]
                {
                    new Vector3(-0.5f, 0f, 0f),
                    new Vector3(0.5f, 0f, 0f),
                    new Vector3(-0.5f, 1f, 0f),
                    new Vector3(0.5f, 1f, 0f)
                },
                uv = new[]
                {
                    new Vector2(0f, 0f),
                    new Vector2(1f, 0f),
                    new Vector2(0f, 1f),
                    new Vector2(1f, 1f)
                },
                triangles = new[] { 0, 2, 1, 2, 3, 1 }
            };
            quadMesh.RecalculateBounds();
            return quadMesh;
        }

        private static Material TransparentMaterial()
        {
            if (sharedTransparentMaterial != null)
            {
                return sharedTransparentMaterial;
            }

            var shader = Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Transparent");
            sharedTransparentMaterial = new Material(shader)
            {
                name = "NpcWorldBillboardTransparent"
            };
            return sharedTransparentMaterial;
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
