// 2D 角色逐帧法线绑定器
//
// 为什么需要：SpriteRenderer 在绘制时只会把当前帧的 sprite 贴图自动绑到 _MainTex，
// 不会绑 _BumpMap。帧动画每帧换 sprite，法线也必须跟着换，否则光照体积会"错帧"。
//
// 做法：监听 sprite 变化 → 按贴图名去 Resources/ARK_Normals/ 找同名 _N 贴图 →
//       用 MaterialPropertyBlock 逐渲染器设置 _BumpMap 和 _UseNormalMap。
//       找不到烘焙法线的帧会自动退回 shader 里的"运行时推导"，不会出错。
//
// 用法：挂在角色的 SpriteRenderer 同一物体上即可，无需手工拖图。
// 质量档位：ARK_SpriteNormalBinder.GlobalQuality（3 高 / 2 中 / 1 低 / 0 关闭）

using System.Collections.Generic;
using UnityEngine;

namespace ARK
{
    [RequireComponent(typeof(SpriteRenderer))]
    [DisallowMultipleComponent]
    [AddComponentMenu("ARK/Sprite Normal Binder")]
    public class ARK_SpriteNormalBinder : MonoBehaviour
    {
        public const string ResourceFolder = "ARK_Normals/";

        /// <summary>全局质量档位：3=高(烘焙法线,强度1.0) 2=中(烘焙法线,强度0.85) 1=低(退回运行时推导) 0=关闭(等同 1)</summary>
        public static int GlobalQuality = 2;

        static readonly Dictionary<string, Texture2D> s_cache = new Dictionary<string, Texture2D>();
        static readonly Dictionary<string, bool> s_missing = new Dictionary<string, bool>();
        static readonly List<ARK_SpriteNormalBinder> s_active = new List<ARK_SpriteNormalBinder>();

        SpriteRenderer _sr;
        MaterialPropertyBlock _mpb;
        Sprite _lastSprite;
        int _lastQuality = -1;
        bool _blockCleared;

        void OnEnable()
        {
            _sr = GetComponent<SpriteRenderer>();
            _mpb = new MaterialPropertyBlock();
            if (!s_active.Contains(this)) s_active.Add(this);
            _lastSprite = null;
            Apply();
        }

        void OnDisable()
        {
            s_active.Remove(this);
        }

        void LateUpdate()
        {
            if (_sr == null) return;
            if (_sr.sprite != _lastSprite || GlobalQuality != _lastQuality) Apply();
        }

        void Apply()
        {
            if (_sr == null) _sr = GetComponent<SpriteRenderer>();
            var sp = _sr.sprite;
            _lastSprite = sp;
            _lastQuality = GlobalQuality;
            if (sp == null) return;

            var tex = sp.texture;
            if (tex == null) return;

            // 材质里没有 _BumpMap（例如换成基础光照 shader）时，这套法线绑定就没有意义了。
            // 直接让开，交给 SpriteRenderer 自己绑 _MainTex，避免每帧写 PropertyBlock。
            var mat = _sr.sharedMaterial;
            if (mat == null || !mat.HasProperty("_BumpMap"))
            {
                if (!_blockCleared)
                {
                    _sr.SetPropertyBlock(null);
                    _blockCleared = true;
                }
                return;
            }
            _blockCleared = false;

            Texture2D nrm = GlobalQuality >= 2 ? Lookup(tex.name) : null;

            // SpriteRenderer 的 sprite 贴图绑定会被自定义 PropertyBlock 抢掉，
            // 所以 _MainTex 也要一起写回去。
            _mpb.Clear();
            _mpb.SetTexture("_MainTex", tex);

            if (nrm != null)
            {
                _mpb.SetTexture("_BumpMap", nrm);
                _mpb.SetFloat("_UseNormalMap", 1f);
                _mpb.SetFloat("_BumpScale", GlobalQuality >= 3 ? 1.0f : 0.85f);
            }
            else
            {
                _mpb.SetFloat("_UseNormalMap", 0f);
            }

            _sr.SetPropertyBlock(_mpb);
        }

        static Texture2D Lookup(string textureName)
        {
            if (string.IsNullOrEmpty(textureName)) return null;
            Texture2D hit;
            if (s_cache.TryGetValue(textureName, out hit)) return hit;
            if (s_missing.ContainsKey(textureName)) return null;

            hit = Resources.Load<Texture2D>(ResourceFolder + textureName + "_N");
            if (hit != null)
            {
                s_cache[textureName] = hit;
                return hit;
            }
            s_missing[textureName] = true;
            return null;
        }

        /// <summary>切换全局质量档位并刷新所有已注册的角色</summary>
        public static void SetGlobalQuality(int q)
        {
            GlobalQuality = Mathf.Clamp(q, 0, 3);
            for (int i = 0; i < s_active.Count; i++)
            {
                if (s_active[i] != null) s_active[i].Apply();
            }
        }

        /// <summary>清空缓存（换烘焙分辨率后需要调用，否则会拿到旧引用）</summary>
        public static void ClearCache()
        {
            s_cache.Clear();
            s_missing.Clear();
        }
    }
}
