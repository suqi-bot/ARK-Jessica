using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ARK.Glass.EditorTools
{
    /// <summary>
    /// 玻璃系统的程序化资源生成器：
    ///   1. 裂纹贴图（4 张不同 seed 的放射裂纹，白色，带圆形软蒙版）
    ///   2. 玻璃碎裂/受击音效（合成 WAV，不需要外部音频素材）
    ///   3. 默认材质
    /// 菜单：Tools/ARK/玻璃
    /// </summary>
    public static class GlassAssetGenerator
    {
        public const string GlassRoot = "Assets/ArtRes/Glass";
        public const string CrackDir = GlassRoot + "/Cracks";
        public const string AudioDir = GlassRoot + "/Audio";
        const int CrackSize = 256;

        // ───────────────────────────── 菜单 ─────────────────────────────

        [MenuItem("Tools/ARK/玻璃/1. 生成全部资源 (裂纹+音效+材质)", false, 1)]
        public static void GenerateAll()
        {
            EnsureFolders();
            GenerateCrackTextures();
            GenerateAudio();
            CreateMaterials();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Glass] 资源生成完成：" + GlassRoot);
        }

        [MenuItem("Tools/ARK/玻璃/生成裂纹贴图", false, 11)]
        public static void GenerateCrackTextures()
        {
            EnsureFolders();
            for (int i = 0; i < 4; i++)
            {
                var tex = BuildCrackTexture(20250901 + i * 977);
                string path = string.Format("{0}/Crack_{1:00}.png", CrackDir, i + 1);
                File.WriteAllBytes(path, tex.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(tex);
            }
            AssetDatabase.Refresh();
            ConfigureCrackImporter();
            Debug.Log("[Glass] 裂纹贴图已生成 4 张 → " + CrackDir);
        }

        [MenuItem("Tools/ARK/玻璃/生成碎裂音效", false, 12)]
        public static void GenerateAudio()
        {
            EnsureFolders();
            WriteWav(AudioDir + "/Glass_Break.wav", BuildGlassBreak(0.95f, 12345));
            WriteWav(AudioDir + "/Glass_Hit.wav", BuildGlassHit(0.22f, 999));
            AssetDatabase.Refresh();
            Debug.Log("[Glass] 音效已生成 → " + AudioDir);
        }

        [MenuItem("Tools/ARK/玻璃/创建默认材质", false, 13)]
        public static void CreateMaterials()
        {
            EnsureFolders();

            CreateOrUpdateMaterial(GlassRoot + "/Glass_Panel.mat", "ARK/Glass", m =>
            {
                m.SetColor("_Color", new Color(0.52f, 0.80f, 0.92f, 0.12f));
                m.SetColor("_EdgeColor", new Color(0.74f, 0.95f, 1.00f, 0.80f));
                m.SetFloat("_FresnelPower", 3.1f);
                m.SetFloat("_FresnelStrength", 0.9f);
                m.SetFloat("_Reflection", 0.40f);
                m.SetFloat("_BandStrength", 0.55f);
                m.SetFloat("_BandFrequency", 0.32f);
                m.SetFloat("_BandSharpness", 3.2f);
                m.SetFloat("_SpecIntensity", 0.9f);
                m.SetFloat("_EdgeWidth", 0.11f);
                m.SetFloat("_EdgePower", 1.0f);
                m.SetFloat("_AmbientBoost", 0.62f);
            });

            CreateOrUpdateMaterial(GlassRoot + "/Glass_Fragment.mat", "ARK/Glass", m =>
            {
                m.SetColor("_Color", new Color(0.62f, 0.87f, 0.96f, 0.42f));
                m.SetColor("_EdgeColor", new Color(0.90f, 0.99f, 1.00f, 0.85f));
                m.SetFloat("_FresnelPower", 2.6f);
                // 碎片是小块，侧面占屏幕比例大，菲涅尔必须压低，否则整块发白不透
                m.SetFloat("_FresnelStrength", 0.45f);
                m.SetFloat("_Reflection", 0.40f);
                m.SetFloat("_BandStrength", 0.08f);
                m.SetFloat("_AmbientBoost", 0.40f);
            });

            CreateOrUpdateMaterial(GlassRoot + "/Glass_Crack.mat", "ARK/GlassCrack", m =>
            {
                var crack = AssetDatabase.LoadAssetAtPath<Texture2D>(CrackDir + "/Crack_01.png");
                if (crack != null) m.SetTexture("_MainTex", crack);
                m.SetColor("_Color", new Color(1f, 1f, 1f, 0.95f));
            });

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Glass] 材质已生成 → " + GlassRoot);
        }

        private static void CreateOrUpdateMaterial(string path, string shaderName, Action<Material> setup)
        {
            var shader = Shader.Find(shaderName);
            if (shader == null)
            {
                Debug.LogError("[Glass] 找不到 Shader: " + shaderName);
                return;
            }
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            else if (mat.shader != shader)
            {
                mat.shader = shader;
            }
            setup?.Invoke(mat);
            EditorUtility.SetDirty(mat);
        }

        private static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder(GlassRoot)) AssetDatabase.CreateFolder("Assets/ArtRes", "Glass");
            if (!AssetDatabase.IsValidFolder(CrackDir)) AssetDatabase.CreateFolder(GlassRoot, "Cracks");
            if (!AssetDatabase.IsValidFolder(AudioDir)) AssetDatabase.CreateFolder(GlassRoot, "Audio");
        }

        private static void ConfigureCrackImporter()
        {
            for (int i = 1; i <= 4; i++)
            {
                string path = string.Format("{0}/Crack_{1:00}.png", CrackDir, i);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) continue;
                importer.textureType = TextureImporterType.Default;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = true;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.filterMode = FilterMode.Bilinear;
                importer.SaveAndReimport();
            }
        }

        // ───────────────────────────── 裂纹贴图 ─────────────────────────────

        private static Texture2D BuildCrackTexture(int seed)
        {
            var rnd = new System.Random(seed);
            int w = CrackSize, h = CrackSize;
            var field = new float[w * h];

            Vector2 center = new Vector2(w * 0.5f, h * 0.5f);
            int mainCount = 11 + rnd.Next(0, 5);
            float baseAngle = (float)rnd.NextDouble() * Mathf.PI * 2f;

            for (int i = 0; i < mainCount; i++)
            {
                float ang = baseAngle + (Mathf.PI * 2f / mainCount) * i + Range(rnd, -0.16f, 0.16f);
                float maxLen = Range(rnd, 78f, h * 0.5f - 6f);
                DrawBranch(field, w, h, center, ang, maxLen, Range(rnd, 2.4f, 3.6f), 0, rnd);
            }

            // 同心环：让中心有一圈碎裂环
            int rings = 2 + rnd.Next(0, 2);
            for (int r = 0; r < rings; r++)
            {
                float radius = Range(rnd, 12f, 46f);
                int seg = 10 + rnd.Next(0, 8);
                Vector2 prev = center + new Vector2(1f, 0f) * radius;
                for (int s = 1; s <= seg; s++)
                {
                    float a = (float)s / seg * Mathf.PI * 2f;
                    float jitter = 1f + Range(rnd, -0.18f, 0.18f);
                    Vector2 p = center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius * jitter;
                    DrawSegment(field, w, h, prev, p, Range(rnd, 0.8f, 1.6f));
                    prev = p;
                }
            }

            // 中心撞击白点
            Stamp(field, w, h, center.x, center.y, Range(rnd, 4f, 7f), 1f);
            Stamp(field, w, h, center.x, center.y, Range(rnd, 9f, 13f), 0.35f);

            // 圆形软蒙版 + 转纹理
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var pixels = new Color32[w * h];
            float radiusOuter = w * 0.5f;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int idx = y * w + x;
                    float dx = x + 0.5f - center.x;
                    float dy = y + 0.5f - center.y;
                    float d = Mathf.Sqrt(dx * dx + dy * dy) / radiusOuter;
                    float mask = Mathf.Clamp01(1f - Mathf.Pow(d, 2.6f));
                    float a = Mathf.Clamp01(field[idx]) * mask;
                    byte alpha = (byte)(a * 255f);
                    byte v = 255;
                    pixels[idx] = new Color32(v, v, v, alpha);
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply(false, false);
            return tex;
        }

        private static void DrawBranch(float[] field, int w, int h, Vector2 start, float angle,
                                       float length, float width, int depth, System.Random rnd)
        {
            Vector2 p = start;
            float travelled = 0f;
            float ang = angle;
            int guard = 0;
            while (travelled < length && guard++ < 400)
            {
                float step = Range(rnd, 5f, 12f);
                ang += Range(rnd, -0.22f, 0.22f);
                Vector2 next = p + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * step;

                float t = Mathf.Clamp01(travelled / Mathf.Max(length, 0.001f));
                float wNow = Mathf.Lerp(width, width * 0.25f, t);
                DrawSegment(field, w, h, p, next, wNow);

                // 分岔
                if (depth < 2 && rnd.NextDouble() < 0.28)
                {
                    float branchAng = ang + (rnd.NextDouble() < 0.5 ? -1f : 1f) * Range(rnd, 0.45f, 1.0f);
                    float branchLen = (length - travelled) * Range(rnd, 0.3f, 0.65f);
                    if (branchLen > 6f)
                        DrawBranch(field, w, h, next, branchAng, branchLen, wNow * 0.75f, depth + 1, rnd);
                }

                p = next;
                travelled += step;
                if (p.x < 2 || p.x > w - 2 || p.y < 2 || p.y > h - 2) break;
            }
        }

        private static void DrawSegment(float[] field, int w, int h, Vector2 a, Vector2 b, float width)
        {
            float len = Vector2.Distance(a, b);
            int steps = Mathf.Max(1, Mathf.CeilToInt(len * 1.4f));
            for (int i = 0; i <= steps; i++)
            {
                Vector2 p = Vector2.Lerp(a, b, (float)i / steps);
                Stamp(field, w, h, p.x, p.y, width, 1f);
            }
        }

        private static void Stamp(float[] field, int w, int h, float cx, float cy, float radius, float strength)
        {
            if (radius <= 0f) return;
            int x0 = Mathf.Max(0, Mathf.FloorToInt(cx - radius - 1f));
            int x1 = Mathf.Min(w - 1, Mathf.CeilToInt(cx + radius + 1f));
            int y0 = Mathf.Max(0, Mathf.FloorToInt(cy - radius - 1f));
            int y1 = Mathf.Min(h - 1, Mathf.CeilToInt(cy + radius + 1f));

            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    float dx = x + 0.5f - cx;
                    float dy = y + 0.5f - cy;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float v = Mathf.Clamp01(radius - d + 0.5f) * strength; // 1px 软边
                    if (v <= 0f) continue;
                    int idx = y * w + x;
                    float old = field[idx];
                    // 叠加强度用 1-(1-a)(1-b)，重叠处略亮但不爆白
                    field[idx] = 1f - (1f - old) * (1f - v);
                }
            }
        }

        private static float Range(System.Random rnd, float min, float max)
        {
            return min + (float)rnd.NextDouble() * (max - min);
        }

        // ───────────────────────────── 音效合成 ─────────────────────────────

        /// <summary>玻璃碎：撞击闷响 + 大量高频碎片叮当。</summary>
        private static AudioClip BuildGlassBreak(float duration, int seed)
        {
            const int sampleRate = 44100;
            int total = Mathf.CeilToInt(duration * sampleRate);
            var data = new float[total];
            var rnd = new System.Random(seed);

            // 1) 低频撞击
            for (int i = 0; i < total; i++)
            {
                float t = (float)i / sampleRate;
                float env = Mathf.Exp(-t * 26f);
                float f = Mathf.Lerp(160f, 70f, Mathf.Clamp01(t * 8f));
                data[i] += Mathf.Sin(2f * Mathf.PI * f * t) * env * 0.55f;
            }

            // 2) 碎裂宽带噪声（前 80ms）
            float lp = 0f;
            for (int i = 0; i < total; i++)
            {
                float t = (float)i / sampleRate;
                float n = (float)rnd.NextDouble() * 2f - 1f;
                lp = Mathf.Lerp(lp, n, 0.55f);
                float env = Mathf.Exp(-t * 34f);
                data[i] += (n - lp * 0.55f) * env * 0.35f;
            }

            // 3) 碎片叮当：随机时间、随机高频
            int shards = 26 + rnd.Next(0, 14);
            for (int s = 0; s < shards; s++)
            {
                float startT = (float)rnd.NextDouble() * duration * 0.55f;
                float freq = 1400f + (float)rnd.NextDouble() * 6200f;
                float decay = 12f + (float)rnd.NextDouble() * 34f;
                float amp = 0.10f + (float)rnd.NextDouble() * 0.26f;
                int startSample = Mathf.Clamp(Mathf.RoundToInt(startT * sampleRate), 0, total - 1);
                int len = Mathf.Min(total - startSample, Mathf.CeilToInt(0.55f * sampleRate));
                for (int i = 0; i < len; i++)
                {
                    float t = (float)i / sampleRate;
                    float env = Mathf.Exp(-t * decay);
                    if (env < 0.0008f) break;
                    // 非整数倍泛音，听起来更"玻璃"
                    float v = Mathf.Sin(2f * Mathf.PI * freq * t)
                            + 0.45f * Mathf.Sin(2f * Mathf.PI * freq * 2.71f * t)
                            + 0.2f * Mathf.Sin(2f * Mathf.PI * freq * 4.13f * t);
                    data[startSample + i] += v * env * amp * 0.4f;
                }
            }

            Normalize(data, 0.92f);
            return BuildClip("Glass_Break", data, sampleRate);
        }

        /// <summary>玻璃受击：短促的"叮"。</summary>
        private static AudioClip BuildGlassHit(float duration, int seed)
        {
            const int sampleRate = 44100;
            int total = Mathf.CeilToInt(duration * sampleRate);
            var data = new float[total];
            var rnd = new System.Random(seed);

            float freq = 2600f + (float)rnd.NextDouble() * 1400f;
            for (int i = 0; i < total; i++)
            {
                float t = (float)i / sampleRate;
                float env = Mathf.Exp(-t * 42f);
                float v = Mathf.Sin(2f * Mathf.PI * freq * t)
                        + 0.5f * Mathf.Sin(2f * Mathf.PI * freq * 2.4f * t)
                        + 0.22f * Mathf.Sin(2f * Mathf.PI * freq * 3.9f * t);
                data[i] += v * env * 0.5f;
            }

            // 极短的擦击噪声
            float lp = 0f;
            for (int i = 0; i < Mathf.Min(total, sampleRate / 20); i++)
            {
                float t = (float)i / sampleRate;
                float n = (float)rnd.NextDouble() * 2f - 1f;
                lp = Mathf.Lerp(lp, n, 0.4f);
                data[i] += (n - lp) * Mathf.Exp(-t * 90f) * 0.35f;
            }

            Normalize(data, 0.85f);
            return BuildClip("Glass_Hit", data, sampleRate);
        }

        private static AudioClip BuildClip(string name, float[] data, int sampleRate)
        {
            var clip = AudioClip.Create(name, data.Length, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static void Normalize(float[] data, float peak)
        {
            float max = 0f;
            for (int i = 0; i < data.Length; i++)
                max = Mathf.Max(max, Mathf.Abs(data[i]));
            if (max < 1e-5f) return;
            float k = peak / max;
            for (int i = 0; i < data.Length; i++)
                data[i] = Mathf.Clamp(data[i] * k, -1f, 1f);
        }

        private static void WriteWav(string path, AudioClip clip)
        {
            int sampleCount = clip.samples * clip.channels;
            var samples = new float[sampleCount];
            clip.GetData(samples, 0);

            using (var fs = new FileStream(path, FileMode.Create))
            using (var bw = new BinaryWriter(fs))
            {
                int byteRate = clip.frequency * 2 * clip.channels;
                bw.Write(new[] { 'R', 'I', 'F', 'F' });
                bw.Write(36 + sampleCount * 2);
                bw.Write(new[] { 'W', 'A', 'V', 'E' });
                bw.Write(new[] { 'f', 'm', 't', ' ' });
                bw.Write(16);
                bw.Write((short)1);
                bw.Write((short)clip.channels);
                bw.Write(clip.frequency);
                bw.Write(byteRate);
                bw.Write((short)(clip.channels * 2));
                bw.Write((short)16);
                bw.Write(new[] { 'd', 'a', 't', 'a' });
                bw.Write(sampleCount * 2);
                for (int i = 0; i < sampleCount; i++)
                {
                    short s = (short)Mathf.Clamp(Mathf.RoundToInt(samples[i] * 32767f), -32768, 32767);
                    bw.Write(s);
                }
            }
            UnityEngine.Object.DestroyImmediate(clip);
        }
    }
}
