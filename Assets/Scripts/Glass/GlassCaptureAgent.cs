using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ARK.Glass
{
    /// <summary>
    /// 仅用于开发期验收：运行后按关键节点自动把游戏画面截图到项目根目录。
    /// 正式发布前可以移除，不影响玻璃系统本身。
    /// </summary>
    [AddComponentMenu("ARK/Glass Capture Agent")]
    public class GlassCaptureAgent : MonoBehaviour
    {
        public string outputPrefix = "E:/unity/ARK-Jessica/_glass";
        public int width = 1400;
        public int height = 800;
        public float totalDuration = 5.5f;
        [Tooltip("验收用：跑完自动退出播放模式")]
        public bool stopPlayModeWhenDone = false;
        public string diagPath = "";

        private readonly List<GlassPanel> panels = new List<GlassPanel>();
        private readonly HashSet<string> captured = new HashSet<string>();

        private IEnumerator Start()
        {
            panels.AddRange(FindObjectsOfType<GlassPanel>(true));
            WriteDiagnostics();
            yield return new WaitForSeconds(0.35f);
            Capture("01_intact");

            float t = 0f;
            bool sawCrack = false;
            bool sawBroken = false;
            float brokenAt = -1f;

            while (t < totalDuration)
            {
                // 用 unscaled 计时：命中顿挫会改 timeScale，否则验收流程会被拖长
                t += Time.unscaledDeltaTime;

                if (!sawCrack)
                {
                    for (int i = 0; i < panels.Count; i++)
                    {
                        var p = panels[i];
                        if (p != null && !p.IsBroken && p.HealthNormalized < 0.999f)
                        {
                            sawCrack = true;
                            Capture("02_crack");
                            break;
                        }
                    }
                }

                if (!sawBroken)
                {
                    for (int i = 0; i < panels.Count; i++)
                    {
                        var p = panels[i];
                        if (p != null && p.IsBroken)
                        {
                            sawBroken = true;
                            brokenAt = t;
                            Capture("03_break");
                            break;
                        }
                    }
                }
                else if (brokenAt > 0f && t - brokenAt > 0.22f)
                {
                    if (Capture("04_scatter")) brokenAt = -1f;
                }

                yield return null;
            }

            Capture("05_final");
            Debug.Log("[GlassCapture] 截图完成 → " + outputPrefix + "_*.png");

            if (stopPlayModeWhenDone)
            {
#if UNITY_EDITOR
                yield return new WaitForSeconds(0.2f);
                UnityEditor.EditorApplication.isPlaying = false;
#endif
            }
        }

        /// <summary>把网格/材质的关键信息写到文件，方便命令行验收时排查。</summary>
        private void WriteDiagnostics()
        {
            if (string.IsNullOrEmpty(diagPath)) return;
            try
            {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("panels=" + panels.Count);
                for (int i = 0; i < panels.Count && i < 3; i++)
                {
                    var p = panels[i];
                    if (p == null) continue;
                    var mf = p.GetComponent<MeshFilter>();
                    var mr = p.GetComponent<MeshRenderer>();
                    var mesh = mf != null ? mf.sharedMesh : null;
                    sb.AppendLine("[" + p.name + "] shader=" + (mr != null && mr.sharedMaterial != null ? mr.sharedMaterial.shader.name : "null")
                        + " mat=" + (mr != null && mr.sharedMaterial != null ? mr.sharedMaterial.name : "null")
                        + " verts=" + (mesh != null ? mesh.vertexCount : -1));
                    if (mesh != null)
                    {
                        var nrm = mesh.normals;
                        var tris = mesh.triangles;
                        sb.AppendLine("   n[0]=" + (nrm.Length > 0 ? nrm[0].ToString() : "-")
                            + " n[last]=" + (nrm.Length > 0 ? nrm[nrm.Length - 1].ToString() : "-"));
                        if (tris.Length >= 3)
                            sb.AppendLine("   tri0=" + tris[0] + "," + tris[1] + "," + tris[2]);
                        var v = mesh.vertices;
                        if (v.Length > 2)
                            sb.AppendLine("   v0=" + v[0] + " v1=" + v[1] + " v2=" + v[2]);
                    }
                }
                System.IO.File.WriteAllText(diagPath, sb.ToString());
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[GlassCapture] diag failed: " + e.Message);
            }
        }

        private bool Capture(string tag)
        {
            if (captured.Contains(tag)) return false;
            captured.Add(tag);

            var cam = Camera.main;
            if (cam == null) return true;

            var rt = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
            var prevTarget = cam.targetTexture;
            float prevAspect = cam.aspect;
            cam.targetTexture = rt;
            cam.aspect = (float)width / height;
            cam.Render();

            RenderTexture.active = rt;
            var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();
            RenderTexture.active = null;

            cam.targetTexture = prevTarget;
            cam.aspect = prevAspect;

            File.WriteAllBytes(outputPrefix + "_" + tag + ".png", tex.EncodeToPNG());
            Destroy(tex);
            RenderTexture.ReleaseTemporary(rt);
            return true;
        }
    }
}
