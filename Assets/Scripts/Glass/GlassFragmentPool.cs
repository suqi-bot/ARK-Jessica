using System.Collections.Generic;
using UnityEngine;

namespace ARK.Glass
{
    /// <summary>
    /// 玻璃碎片对象池。碎片数量可能瞬间到达几十个，必须复用避免 GC 抖动。
    /// </summary>
    public class GlassFragmentPool : MonoBehaviour
    {
        public const string LayerName = "GlassFragment";

        private static readonly Stack<GlassFragment> Pool = new Stack<GlassFragment>();
        private static Transform poolRoot;
        private static Transform activeRoot;
        private static int glassLayer = -1;
        private static bool collisionsConfigured;
        private static PhysicsMaterial2D fragmentPhysics;
        private static int totalCreated;

        /// <summary>当前活跃碎片数（调试用）。</summary>
        public static int ActiveCount { get; private set; }

        public static Transform ActiveRoot
        {
            get
            {
                EnsureRoots();
                return activeRoot;
            }
        }

        private static void EnsureRoots()
        {
            if (poolRoot != null) return;

            var rootGo = new GameObject("[GlassFragments]");
            DontDestroyOnLoad(rootGo);
            poolRoot = rootGo.transform;

            var activeGo = new GameObject("[GlassFragments_Active]");
            activeGo.transform.SetParent(poolRoot, false);
            activeRoot = activeGo.transform;

            glassLayer = LayerMask.NameToLayer(LayerName);
            if (glassLayer < 0) glassLayer = 0;

            fragmentPhysics = new PhysicsMaterial2D("GlassFragmentMat")
            {
                friction = 0.55f,
                bounciness = 0.03f
            };

            ConfigureCollisions();
        }

        /// <summary>
        /// 碎片只和地面/障碍/墙交互，不挡玩家和敌人，否则碎完会卡住角色。
        /// </summary>
        private static void ConfigureCollisions()
        {
            if (collisionsConfigured) return;
            collisionsConfigured = true;
            if (glassLayer <= 0) return;

            int[] ignore =
            {
                LayerMask.NameToLayer("Player"),
                LayerMask.NameToLayer("Enemy"),
                LayerMask.NameToLayer("NPC"),
                LayerMask.NameToLayer("Door"),
                LayerMask.NameToLayer("AirWall"),
            };
            for (int i = 0; i < ignore.Length; i++)
            {
                if (ignore[i] > 0) Physics2D.IgnoreLayerCollision(glassLayer, ignore[i], true);
            }
            // 碎片之间互相碰撞开销大且视觉杂乱，关掉
            Physics2D.IgnoreLayerCollision(glassLayer, glassLayer, true);
        }

        public static GlassFragment Get()
        {
            EnsureRoots();
            ActiveCount++;

            GlassFragment fragment;
            if (Pool.Count > 0)
            {
                fragment = Pool.Pop();
            }
            else
            {
                var go = new GameObject("GlassFragment");
                go.layer = glassLayer;
                fragment = go.AddComponent<GlassFragment>();
                totalCreated++;
            }

            fragment.transform.SetParent(activeRoot, false);
            fragment.gameObject.SetActive(true);

            var col = fragment.GetComponent<PolygonCollider2D>();
            if (col != null && fragmentPhysics != null) col.sharedMaterial = fragmentPhysics;

            return fragment;
        }

        public static void Release(GlassFragment fragment)
        {
            if (fragment == null) return;
            ActiveCount = Mathf.Max(0, ActiveCount - 1);
            fragment.gameObject.SetActive(false);
            if (poolRoot != null) fragment.transform.SetParent(poolRoot, false);
            Pool.Push(fragment);
        }

        /// <summary>场景切换/重开时清池。</summary>
        public static void Clear()
        {
            Pool.Clear();
            ActiveCount = 0;
            if (poolRoot != null)
            {
                Destroy(poolRoot.gameObject);
                poolRoot = null;
                activeRoot = null;
                collisionsConfigured = false;
            }
        }

        public static string DebugInfo()
        {
            return string.Format("fragments pool={0} active={1} created={2}", Pool.Count, ActiveCount, totalCreated);
        }
    }
}
