using System.Collections;
using UnityEngine;

namespace ARK.Glass
{
    /// <summary>
    /// 演示/自测用：按节奏朝玻璃开枪，便于观察裂纹累积与碎裂效果。
    /// 正式项目里不需要它，真实伤害由子弹、技能、爆炸调用 GlassPanel.Hit 即可。
    /// </summary>
    [AddComponentMenu("ARK/Glass Demo Driver")]
    public class GlassDemoDriver : MonoBehaviour
    {
        public GlassPanel[] targets;
        public float startDelay = 0.9f;
        public float interval = 0.42f;
        public float damage = 34f;
        public bool loop = true;
        public bool drawBulletLine = true;

        private LineRenderer line;
        private Coroutine lineRoutine;

        private void Start()
        {
            if (targets == null || targets.Length == 0)
                targets = FindObjectsOfType<GlassPanel>(true);

            if (drawBulletLine)
            {
                line = gameObject.AddComponent<LineRenderer>();
                line.positionCount = 2;
                line.useWorldSpace = true;
                line.startWidth = 0.045f;
                line.endWidth = 0.012f;
                line.numCapVertices = 2;
                line.material = new Material(Shader.Find("Sprites/Default"));
                line.startColor = new Color(1f, 0.95f, 0.6f, 1f);
                line.endColor = new Color(1f, 0.8f, 0.3f, 0f);
                line.enabled = false;
            }

            StartCoroutine(Run());
        }

        private IEnumerator Run()
        {
            yield return new WaitForSeconds(startDelay);

            int guard = 0;
            while (guard++ < 200)
            {
                bool anyAlive = false;
                for (int i = 0; i < targets.Length; i++)
                {
                    var t = targets[i];
                    if (t == null || t.IsBroken) continue;
                    anyAlive = true;

                    for (int shot = 0; shot < 3 && t != null && !t.IsBroken; shot++)
                    {
                        FireAt(t);
                        yield return new WaitForSeconds(0.16f);
                    }
                    yield return new WaitForSeconds(interval);
                }

                if (!anyAlive) break;
                if (!loop) break;
                yield return new WaitForSeconds(0.5f);
            }

            // 兜底：确保所有玻璃都被打碎（截图时总有碎裂画面）
            for (int i = 0; i < targets.Length; i++)
            {
                if (targets[i] != null && !targets[i].IsBroken)
                    targets[i].Hit(targets[i].transform.position, Vector2.up, 999f);
            }
        }

        private void FireAt(GlassPanel panel)
        {
            if (panel == null) return;

            // 从相机方向偏下一点打过去，命中点带随机性
            Vector3 center = panel.transform.position;
            Vector2 hit = (Vector2)center + Random.insideUnitCircle * 0.32f;
            Vector2 origin = new Vector2(center.x - 4.2f, center.y + Random.Range(-0.35f, 0.35f));
            Vector2 dir = (hit - origin).normalized;

            panel.Hit(hit, dir, damage);

            if (line != null && drawBulletLine)
            {
                // 注意：不能用 StopAllCoroutines，那会停掉射击节奏协程
                if (lineRoutine != null) StopCoroutine(lineRoutine);
                lineRoutine = StartCoroutine(ShowLine(origin, hit));
            }
        }

        private IEnumerator ShowLine(Vector2 from, Vector2 to)
        {
            line.enabled = true;
            line.SetPosition(0, new Vector3(from.x, from.y, -0.3f));
            line.SetPosition(1, new Vector3(to.x, to.y, -0.3f));
            yield return new WaitForSeconds(0.1f);
            line.enabled = false;
        }
    }
}
