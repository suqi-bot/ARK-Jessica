using System.Collections.Generic;
using UnityEngine;

namespace ARK.Glass
{
    /// <summary>
    /// 单块玻璃碎片：由 GlassPanel 在碎裂时创建，使用 2D 刚体在 XY 平面飞散，
    /// 用棱柱网格表现厚度，寿命结束后淡出回池。
    /// </summary>
    [DisallowMultipleComponent]
    public class GlassFragment : MonoBehaviour
    {
        static readonly int AlphaScaleId = Shader.PropertyToID("_AlphaScale");
        static readonly int FlashId = Shader.PropertyToID("_Flash");
        static readonly int BandOffsetId = Shader.PropertyToID("_BandOffset");

        [Header("寿命")]
        public float lifeTime = 3.5f;
        public float fadeDuration = 0.9f;
        /// <summary>落地并静止多久后开始淡出（0 表示不提前）</summary>
        public float restFadeDelay = 1.2f;

        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;
        private PolygonCollider2D polygonCollider;
        private Rigidbody2D body;
        private MaterialPropertyBlock propertyBlock;

        private float age;
        private float restTimer;
        private bool fading;
        private float fadeT;
        private Vector3 baseScale = Vector3.one;
        private Color baseTint = Color.white;
        private bool active;

        public Rigidbody2D Body => body;

        private void Awake()
        {
            meshFilter = GetComponent<MeshFilter>();
            if (meshFilter == null) meshFilter = gameObject.AddComponent<MeshFilter>();
            meshRenderer = GetComponent<MeshRenderer>();
            if (meshRenderer == null) meshRenderer = gameObject.AddComponent<MeshRenderer>();
            polygonCollider = GetComponent<PolygonCollider2D>();
            if (polygonCollider == null) polygonCollider = gameObject.AddComponent<PolygonCollider2D>();
            body = GetComponent<Rigidbody2D>();
            if (body == null) body = gameObject.AddComponent<Rigidbody2D>();
            propertyBlock = new MaterialPropertyBlock();
        }

        /// <summary>从池中取出时初始化。</summary>
        public void Launch(Mesh mesh, Vector2[] path, Material material, Vector3 position, float zRotation,
                           Vector2 velocity, float angularVelocity, Color tint, float scale,
                           float life, float bandOffset)
        {
            active = true;
            age = 0f;
            restTimer = 0f;
            fading = false;
            fadeT = 0f;

            meshFilter.sharedMesh = mesh;

            if (path != null && path.Length >= 3)
            {
                polygonCollider.pathCount = 1;
                polygonCollider.SetPath(0, path);
            }
            polygonCollider.enabled = true;

            if (material != null && meshRenderer.sharedMaterial != material)
                meshRenderer.sharedMaterial = material;

            baseTint = tint;
            transform.position = position;
            transform.rotation = Quaternion.Euler(0f, 0f, zRotation);
            baseScale = Vector3.one * scale;
            transform.localScale = baseScale;

            body.simulated = true;
            body.velocity = velocity;
            body.angularVelocity = angularVelocity;
            body.drag = 0.06f;
            body.angularDrag = 0.9f;
            body.gravityScale = 1.35f;

            lifeTime = life;
            ApplyMaterialProps(1f, 0f, bandOffset);
        }

        private void Update()
        {
            if (!active) return;

            age += Time.deltaTime;

            // 落地静止后加速淡出，避免一地碎片长时间堆着
            if (!fading)
            {
                if (body.velocity.sqrMagnitude < 0.35f && Mathf.Abs(body.angularVelocity) < 45f)
                {
                    restTimer += Time.deltaTime;
                    if (restTimer >= restFadeDelay) StartFade();
                }
                else
                {
                    restTimer = 0f;
                }

                if (age >= lifeTime) StartFade();
            }
            else
            {
                fadeT += Time.deltaTime / Mathf.Max(0.05f, fadeDuration);
                float a = Mathf.Clamp01(1f - fadeT);
                ApplyMaterialProps(a * a, 0f, 0f);
                transform.localScale = baseScale * Mathf.Lerp(1f, 0.82f, fadeT);

                if (fadeT >= 1f) Release();
            }
        }

        private void StartFade()
        {
            if (fading) return;
            fading = true;
            fadeT = 0f;
            // 淡出时不再参与物理，省开销也避免抖
            body.simulated = false;
            polygonCollider.enabled = false;
        }

        private void ApplyMaterialProps(float alpha, float flash, float bandOffset)
        {
            propertyBlock.SetFloat(AlphaScaleId, alpha);
            propertyBlock.SetFloat(FlashId, flash);
            propertyBlock.SetFloat(BandOffsetId, bandOffset);
            propertyBlock.SetColor("_Tint", baseTint);
            meshRenderer.SetPropertyBlock(propertyBlock);
        }

        private void Release()
        {
            if (!active) return;
            active = false;
            GlassFragmentPool.Release(this);
        }

        private void OnDisable()
        {
            // 回收时销毁独立网格，防止泄漏
            if (meshFilter != null && meshFilter.sharedMesh != null)
            {
                if (Application.isPlaying) Destroy(meshFilter.sharedMesh);
                else DestroyImmediate(meshFilter.sharedMesh);
                meshFilter.sharedMesh = null;
            }
        }
    }
}
