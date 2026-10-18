using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ARK.Glass
{
    /// <summary>
    /// 可破坏玻璃。挂在任意物体上即可：自动按多边形生成带厚度的棱柱网格 + 2D 碰撞体，
    /// 受到攻击时先出现裂纹与闪光，血量归零后按 Voronoi 剖分成碎块并抛飞。
    ///
    /// 场景是 2.5D（3D 布景 + 2D 物理），所以玻璃本体是 3D 棱柱，碰撞和碎片走 2D 物理，
    /// 保证和玩家、子弹、地面都在同一个 z=0 平面里交互。
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public class GlassPanel : MonoBehaviour
    {
        public enum ShapeType { Box, RoundedBox, Ellipse, Custom }

        // ───────────────────────────── 形状 ─────────────────────────────
        [Header("形状（局部 XY 平面，Z 为厚度方向）")]
        public ShapeType shape = ShapeType.Box;
        public Vector2 size = new Vector2(1.8f, 1.8f);
        [Range(0f, 0.5f)] public float cornerRadiusRatio = 0.08f;
        [Range(6, 64)] public int ellipseSegments = 24;
        public Vector2[] customPoints;

        [Tooltip("玻璃厚度（沿 Z），太薄会看不到侧面高光")]
        [Range(0.005f, 0.4f)] public float thickness = 0.07f;

        // ───────────────────────────── 外观 ─────────────────────────────
        [Header("外观")]
        public Material glassMaterial;
        public Color tint = new Color(0.56f, 0.80f, 0.92f, 0.14f);
        public Color edgeColor = new Color(0.75f, 0.95f, 1f, 0.75f);
        [Tooltip("每块玻璃的反光带偏移，避免整排玻璃反光完全一致")]
        public float bandOffset;
        public bool randomizeBandOffset = true;

        // ───────────────────────────── 耐久 ─────────────────────────────
        [Header("耐久")]
        public float maxHealth = 100f;
        [Tooltip("裂纹层数：血量越低叠加的裂纹越多")]
        [Range(1, 8)] public int crackLayers = 3;
        [Tooltip("最低伤害，避免 0 伤害的子弹打不裂")]
        public float minDamage = 1f;

        // ───────────────────────────── 碎裂 ─────────────────────────────
        [Header("碎裂")]
        [Tooltip("碎片数量，0 = 按面积自动")]
        public int fragmentCount = 0;
        [Tooltip("命中点的碎片密度（越大，击中处越碎）")]
        [Range(0f, 1f)] public float impactDensity = 0.62f;
        [Tooltip("碎片抛飞速度")]
        public float explosionSpeed = 4.5f;
        [Tooltip("碎片存活秒数")]
        public float fragmentLife = 4f;
        public Material fragmentMaterial;
        [Tooltip("小于该面积的碎片丢弃，防止碎成粉末")]
        public float minFragmentArea = 0.0025f;

        // ───────────────────────────── 反馈 ─────────────────────────────
        [Header("反馈")]
        public Texture2D[] crackTextures;
        public Material crackMaterial;
        public GameObject hitEffect;
        public GameObject breakEffect;
        public AudioClip hitClip;
        public AudioClip breakClip;
        [Range(0f, 1f)] public float hitStopScale = 0.9f;
        public float hitStopDuration = 0.045f;
        [Range(0f, 1f)] public float breakShake = 0.35f;

        // ───────────────────────────── 行为 ─────────────────────────────
        [Header("行为")]
        [Tooltip("碎裂后是否销毁物体（false 则只是隐藏，适合需要复活/重置的场景）")]
        public bool destroyOnBreak = false;
        [Tooltip("碎裂后是否掉落物品")]
        public GameObject dropPrefab;
        public int dropCount = 1;
        public float dropScatter = 0.4f;

        // ───────────────────────────── 运行时状态 ─────────────────────────────
        private List<Vector2> polygon = new List<Vector2>();
        private Vector2 halfExtents = Vector2.one;
        private List<Vector2> seededPolygon = new List<Vector2>();
        private Mesh runtimeMesh;
        private MeshRenderer meshRenderer;
        private MeshFilter meshFilter;
        private MaterialPropertyBlock propertyBlock;
        private Collider2D[] ownColliders;

        private float health;
        private bool broken;
        private int crackIndex;
        private Coroutine flashRoutine;

        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int EdgeColorId = Shader.PropertyToID("_EdgeColor");
        private static readonly int BandOffsetId = Shader.PropertyToID("_BandOffset");
        private static readonly int AlphaScaleId = Shader.PropertyToID("_AlphaScale");
        private static readonly int FlashId = Shader.PropertyToID("_Flash");
        private static readonly int HalfSizeId = Shader.PropertyToID("_HalfSize");
        private static float lastHitStopTime;

        private static Material sharedDefaultGlass;
        private static Material sharedDefaultFragment;
        private static Material sharedDefaultCrack;
        private static Mesh sharedQuadMesh;

        public bool IsBroken => broken;
        public float Health => health;
        public float HealthNormalized => maxHealth <= 0f ? 0f : Mathf.Clamp01(health / maxHealth);
        public IReadOnlyList<Vector2> Polygon => polygon;

        // ───────────────────────────── 生命周期 ─────────────────────────────

        private void OnEnable()
        {
            if (!Application.isPlaying) health = maxHealth;
            Rebuild();
            if (Application.isPlaying)
            {
                health = maxHealth;
                broken = false;
                crackIndex = 0;
                SetCollidersEnabled(true);
                ApplyAppearance(1f, 0f);
            }
        }

        private void OnDisable()
        {
            if (runtimeMesh != null)
            {
                if (Application.isPlaying) Destroy(runtimeMesh);
                else DestroyImmediate(runtimeMesh);
                runtimeMesh = null;
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!isActiveAndEnabled) return;
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this != null && isActiveAndEnabled) Rebuild();
            };
        }
#endif

        // ───────────────────────────── 构建 ─────────────────────────────

        /// <summary>按当前参数重建多边形、网格和碰撞体。</summary>
        public void Rebuild()
        {
            if (meshFilter == null) meshFilter = GetComponent<MeshFilter>();
            if (meshRenderer == null) meshRenderer = GetComponent<MeshRenderer>();
            if (propertyBlock == null) propertyBlock = new MaterialPropertyBlock();

            polygon = BuildPolygon();
            if (polygon.Count < 3) return;

            if (randomizeBandOffset && !Application.isPlaying && Mathf.Approximately(bandOffset, 0f))
                bandOffset = Random.Range(0f, 6.28f);

            if (runtimeMesh == null)
            {
                runtimeMesh = new Mesh();
                runtimeMesh.name = "GlassPrism";
            }
            GlassGeometry.BuildPrism(polygon, thickness, Vector2.zero, runtimeMesh);

            // 网格必须在 XY 平面朝向相机，绕 Z 旋转由 Transform 负责
            meshFilter.sharedMesh = runtimeMesh;

            if (meshRenderer.sharedMaterial == null || glassMaterial != null)
            {
                var target = ResolveGlassMaterial();
                if (meshRenderer.sharedMaterial != target)
                    meshRenderer.sharedMaterial = target;
            }

            BuildColliders();
            ApplyAppearance(1f, 0f);
        }

        private List<Vector2> BuildPolygon()
        {
            List<Vector2> result;
            switch (shape)
            {
                case ShapeType.RoundedBox:
                    result = GlassGeometry.MakeRoundedBox(size, Mathf.Min(size.x, size.y) * cornerRadiusRatio, 4);
                    break;
                case ShapeType.Ellipse:
                    result = GlassGeometry.MakeEllipse(size * 0.5f, ellipseSegments);
                    break;
                case ShapeType.Custom:
                    result = customPoints != null && customPoints.Length >= 3
                        ? new List<Vector2>(customPoints)
                        : GlassGeometry.MakeBox(size);
                    break;
                default:
                    result = GlassGeometry.MakeBox(size);
                    break;
            }
            GlassGeometry.EnsureCounterClockwise(result);
            UpdateHalfExtents(result);
            return result;
        }

        private void UpdateHalfExtents(List<Vector2> poly)
        {
            if (poly.Count == 0) { halfExtents = Vector2.one; return; }
            float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
            for (int i = 0; i < poly.Count; i++)
            {
                Vector2 v = poly[i];
                if (v.x < minX) minX = v.x;
                if (v.y < minY) minY = v.y;
                if (v.x > maxX) maxX = v.x;
                if (v.y > maxY) maxY = v.y;
            }
            halfExtents = new Vector2(Mathf.Max(0.001f, (maxX - minX) * 0.5f),
                                      Mathf.Max(0.001f, (maxY - minY) * 0.5f));
        }

        private void BuildColliders()
        {
            // 保留用户手动加的碰撞体（比如额外的触发器），这里只维护主碰撞体
            var polyCol = GetComponent<PolygonCollider2D>();
            if (polyCol == null) polyCol = gameObject.AddComponent<PolygonCollider2D>();
            polyCol.pathCount = 1;
            polyCol.SetPath(0, polygon.ToArray());
            polyCol.isTrigger = false;

            // 去掉自动加的多余 BoxCollider2D，避免重复
            var box = GetComponent<BoxCollider2D>();
            if (box != null && Application.isPlaying == false)
            {
                // 只在编辑器里清理，运行时不 Destroy 用户组件
            }

            ownColliders = GetComponents<Collider2D>();
        }

        private void SetCollidersEnabled(bool on)
        {
            if (ownColliders == null) ownColliders = GetComponents<Collider2D>();
            for (int i = 0; i < ownColliders.Length; i++)
                if (ownColliders[i] != null) ownColliders[i].enabled = on;
        }

        // ───────────────────────────── 外观 ─────────────────────────────

        private void ApplyAppearance(float alphaScale, float flash)
        {
            if (meshRenderer == null) return;
            if (propertyBlock == null) propertyBlock = new MaterialPropertyBlock();

            meshRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor(ColorId, tint);
            propertyBlock.SetColor(EdgeColorId, edgeColor);
            propertyBlock.SetFloat(BandOffsetId, bandOffset);
            propertyBlock.SetFloat(AlphaScaleId, alphaScale);
            propertyBlock.SetFloat(FlashId, flash);
            propertyBlock.SetVector(HalfSizeId, new Vector4(halfExtents.x, halfExtents.y, 0f, 0f));
            meshRenderer.SetPropertyBlock(propertyBlock);
        }

        private Material ResolveGlassMaterial()
        {
            if (glassMaterial != null) return glassMaterial;
            if (sharedDefaultGlass == null)
            {
                var shader = Shader.Find("ARK/Glass");
                if (shader == null) shader = Shader.Find("Sprites/Default");
                sharedDefaultGlass = new Material(shader) { name = "GlassDefault(Runtime)" };
            }
            return sharedDefaultGlass;
        }

        private Material ResolveFragmentMaterial()
        {
            if (fragmentMaterial != null) return fragmentMaterial;
            if (sharedDefaultFragment == null)
            {
                var shader = Shader.Find("ARK/Glass");
                if (shader == null) shader = Shader.Find("Sprites/Default");
                sharedDefaultFragment = new Material(shader) { name = "GlassFragment(Runtime)" };
                sharedDefaultFragment.SetColor("_Color", new Color(0.62f, 0.86f, 0.95f, 0.55f));
                sharedDefaultFragment.SetColor("_EdgeColor", new Color(0.9f, 0.98f, 1f, 0.95f));
                sharedDefaultFragment.SetFloat("_BandStrength", 0.15f);
            }
            return sharedDefaultFragment;
        }

        private Material ResolveCrackMaterial()
        {
            if (crackMaterial != null) return crackMaterial;
            if (sharedDefaultCrack == null)
            {
                var shader = Shader.Find("ARK/GlassCrack");
                if (shader == null) shader = Shader.Find("Sprites/Default");
                sharedDefaultCrack = new Material(shader) { name = "GlassCrack(Runtime)" };
            }
            return sharedDefaultCrack;
        }

        private static Mesh QuadMesh()
        {
            if (sharedQuadMesh != null) return sharedQuadMesh;
            var mesh = new Mesh { name = "GlassQuad" };
            mesh.SetVertices(new List<Vector3>
            {
                new Vector3(-0.5f, -0.5f, 0f),
                new Vector3( 0.5f, -0.5f, 0f),
                new Vector3( 0.5f,  0.5f, 0f),
                new Vector3(-0.5f,  0.5f, 0f),
            });
            mesh.SetUVs(0, new List<Vector2>
            {
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f)
            });
            mesh.SetTriangles(new[] { 0, 1, 2, 0, 2, 3 }, 0);
            mesh.RecalculateBounds();
            sharedQuadMesh = mesh;
            return mesh;
        }

        // ───────────────────────────── 命中 ─────────────────────────────

        /// <summary>
        /// 受到攻击。worldPoint 为命中点（世界坐标），worldDirection 为攻击方向。
        /// 返回 true 表示这一下把玻璃打碎了。
        /// </summary>
        public bool Hit(Vector2 worldPoint, Vector2 worldDirection, float damage)
        {
            if (broken) return false;

            damage = Mathf.Max(damage, minDamage);

            Vector2 local = transform.InverseTransformPoint(worldPoint);
            if (!GlassGeometry.PointInPolygon(polygon, local))
                local = ClosestInsidePoint(local);

            Vector2 localDir = Vector2.zero;
            if (worldDirection.sqrMagnitude > 1e-6f)
            {
                Vector3 d3 = transform.InverseTransformDirection(worldDirection.normalized);
                localDir = new Vector2(d3.x, d3.y);
            }

            health -= damage;
            float progress = 1f - Mathf.Clamp01(health / maxHealth);

            StartFlash();
            SpawnChip(worldPoint, worldDirection);
            PlayClip(hitClip, worldPoint, 0.5f);
            TriggerHitStop(false);

            if (health <= 0f)
            {
                Shatter(local, localDir, worldPoint);
                return true;
            }

            // 按血量进度补裂纹
            int targetCracks = Mathf.FloorToInt(progress * crackLayers + 0.0001f);
            while (crackIndex < targetCracks && crackIndex < crackLayers * 2)
            {
                SpawnCrack(local, 1f);
                crackIndex++;
            }
            ApplyAppearance(1f, 0f);
            return false;
        }

        /// <summary>范围伤害（爆炸、子弹链式、玩家冲刺撞击都用这个）。</summary>
        public bool HitRadius(Vector2 worldCenter, float radius, float damage, Vector2 direction)
        {
            if (broken) return false;
            Vector2 local = transform.InverseTransformPoint(worldCenter);
            float dist = GlassGeometry.DistanceToPolygonEdge(polygon, local);
            if (!GlassGeometry.PointInPolygon(polygon, local) && dist > radius) return false;
            if (dist > radius) return false;

            Vector2 point = dist > 1e-4f ? transform.TransformPoint(ClosestPointOnPolygon(local)) : (Vector3)worldCenter;
            float falloff = Mathf.Clamp01(1f - dist / Mathf.Max(radius, 0.001f));
            return Hit(point, direction, damage * Mathf.Lerp(0.4f, 1f, falloff));
        }

        private Vector2 ClosestInsidePoint(Vector2 p)
        {
            // 简单做法：先找边界最近点，再朝质心挪一点
            Vector2 edge = ClosestPointOnPolygon(p);
            Vector2 centroid = GlassGeometry.Centroid(polygon);
            return Vector2.Lerp(edge, centroid, 0.08f);
        }

        private Vector2 ClosestPointOnPolygon(Vector2 p)
        {
            Vector2 best = p;
            float bestDist = float.MaxValue;
            for (int i = 0; i < polygon.Count; i++)
            {
                Vector2 a = polygon[i];
                Vector2 b = polygon[(i + 1) % polygon.Count];
                Vector2 ab = b - a;
                float len2 = ab.sqrMagnitude;
                float t = len2 < 1e-9f ? 0f : Mathf.Clamp01(Vector2.Dot(p - a, ab) / len2);
                Vector2 proj = a + ab * t;
                float d = (p - proj).sqrMagnitude;
                if (d < bestDist) { bestDist = d; best = proj; }
            }
            return best;
        }

        private void StartFlash()
        {
            if (!Application.isPlaying) return;
            if (flashRoutine != null) StopCoroutine(flashRoutine);
            flashRoutine = StartCoroutine(FlashRoutine());
        }

        private IEnumerator FlashRoutine()
        {
            float t = 0f;
            const float dur = 0.16f;
            while (t < dur)
            {
                t += Time.deltaTime;
                float k = 1f - t / dur;
                ApplyAppearance(1f, k * k * 0.9f);
                yield return null;
            }
            ApplyAppearance(1f, 0f);
            flashRoutine = null;
        }

        private void SpawnCrack(Vector2 localPoint, float strength)
        {
            if (crackTextures == null || crackTextures.Length == 0) return;

            var go = new GameObject("Crack");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(localPoint.x, localPoint.y, -thickness * 0.5f - 0.006f);
            go.transform.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));

            // 裂纹要盖住大半个玻璃，否则第一枪看起来只是"扎了个点"
            float scale = Mathf.Max(size.x, size.y) * Random.Range(0.85f, 1.3f);
            go.transform.localScale = new Vector3(scale, scale, 1f);

            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = QuadMesh();
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = ResolveCrackMaterial();
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;

            var tex = crackTextures[Random.Range(0, crackTextures.Length)];
            var mpb = new MaterialPropertyBlock();
            mpb.SetTexture("_MainTex", tex);
            mpb.SetFloat("_Opacity", Mathf.Clamp01(strength) * Random.Range(0.75f, 1f));
            mpb.SetColor("_Color", new Color(1f, 1f, 1f, 0.95f));
            mr.SetPropertyBlock(mpb);

            if (Application.isPlaying) Destroy(go, 60f);
        }

        private void SpawnChip(Vector2 worldPoint, Vector2 worldDirection)
        {
            if (hitEffect != null)
            {
                var fx = Instantiate(hitEffect, worldPoint, Quaternion.identity);
                if (worldDirection.sqrMagnitude > 1e-4f)
                {
                    float ang = Mathf.Atan2(worldDirection.y, worldDirection.x) * Mathf.Rad2Deg;
                    fx.transform.rotation = Quaternion.Euler(0f, 0f, ang);
                }
                Destroy(fx, 4f);
            }
        }

        private void PlayClip(AudioClip clip, Vector2 worldPoint, float volume)
        {
            if (clip == null) return;
            // PlayClipAtPoint 会自己创建一个临时 AudioSource 并在播放结束后销毁
            AudioSource.PlayClipAtPoint(clip, worldPoint, volume);
        }

        // ───────────────────────────── 碎裂 ─────────────────────────────

        private void Shatter(Vector2 impactLocal, Vector2 impactDirLocal, Vector2 impactWorld)
        {
            broken = true;
            SetCollidersEnabled(false);

            Vector2 impactWorldPos = transform.TransformPoint(new Vector3(impactLocal.x, impactLocal.y, 0f));
            Vector2 impactDirWorld = impactDirLocal.sqrMagnitude > 1e-6f
                ? transform.TransformDirection(new Vector3(impactDirLocal.x, impactDirLocal.y, 0f))
                : Vector2.up;

            BuildAndLaunchFragments(impactLocal, impactWorldPos, impactDirWorld);

            // 爆裂特效
            if (breakEffect != null)
            {
                var fx = Instantiate(breakEffect, impactWorldPos, Quaternion.identity);
                Destroy(fx, 5f);
            }
            PlayClip(breakClip, impactWorldPos, 0.85f);
            TriggerHitStop(true);
            SpawnDrops(impactWorldPos);
            StartCoroutine(NotifyAndHide());
        }

        private void BuildAndLaunchFragments(Vector2 impactLocal, Vector2 impactWorldPos, Vector2 impactDirWorld)
        {
            float area = Mathf.Abs(GlassGeometry.SignedArea(polygon));
            int count = fragmentCount > 0
                ? fragmentCount
                : Mathf.Clamp(Mathf.RoundToInt(area / 0.026f), 16, 60);

            var rnd = new System.Random(GetInstanceID() ^ (int)(Time.realtimeSinceStartup * 1000f));
            var seeds = GlassGeometry.ScatterSeeds(polygon, impactLocal, count, impactDensity, 2.1f, rnd);
            var cells = GlassGeometry.VoronoiCells(polygon, seeds, minFragmentArea);
            if (cells.Count == 0)
            {
                // 极端情况兜底：退回矩形剖分
                cells = GlassGeometry.VoronoiCells(polygon, seeds, minFragmentArea * 0.1f);
            }

            // 求最远距离用于速度衰减
            float maxDist = 0.001f;
            for (int i = 0; i < cells.Count; i++)
            {
                Vector2 c = GlassGeometry.Centroid(cells[i]);
                float d = (c - impactLocal).magnitude;
                if (d > maxDist) maxDist = d;
            }

            var fragMaterial = ResolveFragmentMaterial();
            float zAngle = transform.eulerAngles.z;
            Color fragTint = Color.Lerp(tint, Color.white, 0.35f);
            fragTint.a = Mathf.Clamp01(0.45f + tint.a);

            for (int i = 0; i < cells.Count; i++)
            {
                var cell = cells[i];
                if (cell.Count < 3) continue;

                Vector2 center = GlassGeometry.Centroid(cell);
                var mesh = GlassGeometry.BuildPrism(cell, thickness, center);
                if (mesh == null) continue;

                var path = new Vector2[cell.Count];
                for (int k = 0; k < cell.Count; k++) path[k] = cell[k] - center;

                Vector3 worldPos = transform.TransformPoint(new Vector3(center.x, center.y, 0f));
                worldPos.z += Random.Range(-0.02f, 0.02f);

                Vector2 toFrag = (Vector2)worldPos - impactWorldPos;
                float dist = toFrag.magnitude;
                Vector2 radial = dist > 0.02f ? toFrag / dist : Random.insideUnitCircle.normalized;
                float falloff = Mathf.Lerp(1f, 0.42f, Mathf.Clamp01(dist / maxDist));

                Vector2 velocity = radial * (explosionSpeed * falloff)
                                 + impactDirWorld.normalized * (explosionSpeed * 0.28f)
                                 + new Vector2(Random.Range(-0.35f, 0.35f), Random.Range(0.25f, 0.9f)) * explosionSpeed * 0.3f;

                float angular = Random.Range(-560f, 560f) * falloff;

                var fragment = GlassFragmentPool.Get();
                fragment.Launch(
                    mesh,
                    path,
                    fragMaterial,
                    worldPos,
                    zAngle + Random.Range(-14f, 14f),
                    velocity,
                    angular,
                    fragTint,
                    Random.Range(0.94f, 1.06f),
                    fragmentLife * Random.Range(0.85f, 1.15f),
                    bandOffset + Random.Range(-0.6f, 0.6f));
            }
        }

        private void SpawnDrops(Vector2 worldPos)
        {
            if (dropPrefab == null || dropCount <= 0) return;
            for (int i = 0; i < dropCount; i++)
            {
                Vector3 offset = new Vector3(Random.Range(-dropScatter, dropScatter), Random.Range(0f, dropScatter), 0f);
                Instantiate(dropPrefab, (Vector3)worldPos + offset, Quaternion.identity);
            }
        }

        private IEnumerator NotifyAndHide()
        {
            // 留给特效/音效一点点时间再隐藏本体
            yield return new WaitForSeconds(0.02f);

            if (destroyOnBreak)
            {
                Destroy(gameObject);
                yield break;
            }

            // 只关掉渲染和碰撞，物体本身保留（方便重置/复活）
            if (meshRenderer != null) meshRenderer.enabled = false;
            for (int i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i);
                if (child.name == "Crack") child.gameObject.SetActive(false);
            }
        }

        private void TriggerHitStop(bool strong)
        {
            if (!Application.isPlaying) return;
            if (Time.unscaledTime - lastHitStopTime < 0.08f) return;
            lastHitStopTime = Time.unscaledTime;

            float scale = strong ? hitStopScale : Mathf.Lerp(1f, hitStopScale, 0.5f);
            float dur = strong ? hitStopDuration * 1.6f : hitStopDuration;
            if (scale >= 0.999f) return;
            StartCoroutine(HitStopRoutine(scale, dur));
        }

        // 顿挫必须用引用计数 + 基准值，否则连续命中时 timeScale 会反复相乘，
        // 表现为"打碎几块玻璃后整个游戏越来越慢"
        private static float hitStopBaseScale = 1f;
        private static int hitStopDepth;

        private IEnumerator HitStopRoutine(float scale, float duration)
        {
            if (hitStopDepth <= 0)
            {
                hitStopBaseScale = Time.timeScale;
                if (hitStopBaseScale <= 0.001f) hitStopBaseScale = 1f;
            }
            hitStopDepth++;
            Time.timeScale = hitStopBaseScale * scale;

            yield return new WaitForSecondsRealtime(duration);

            hitStopDepth = Mathf.Max(0, hitStopDepth - 1);
            if (hitStopDepth == 0) Time.timeScale = hitStopBaseScale;
        }

        // ───────────────────────────── 外部工具 ─────────────────────────────

        /// <summary>复原玻璃（重开、关卡重置时调用）。</summary>
        public void ResetGlass()
        {
            StopAllCoroutines();
            broken = false;
            health = maxHealth;
            crackIndex = 0;
            flashRoutine = null;

            if (meshRenderer != null) meshRenderer.enabled = true;
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i);
                if (child.name != "Crack") continue;
                if (Application.isPlaying) Destroy(child.gameObject);
                else DestroyImmediate(child.gameObject);
            }
            SetCollidersEnabled(true);
            Time.timeScale = 1f;
            ApplyAppearance(1f, 0f);
        }

        /// <summary>把世界坐标命中点转换成玻璃局部坐标（调试用）。</summary>
        public Vector2 WorldToLocal2D(Vector2 worldPoint)
        {
            return transform.InverseTransformPoint(worldPoint);
        }
    }
}
