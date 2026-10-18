// ARK 2D 角色法线贴图烘焙器
//
// 为什么需要烘焙：SpriteRenderer 是逐帧换贴图的，法线也必须逐帧对应。
// 纯 per-pixel Sobel 只能得到描边一圈的倒角，角色内部永远是平面，
// 光照方向一变几乎没反应。要得到"有体积"的法线，必须知道每个像素到轮廓的距离
// ——这是一个全局运算（距离变换），只能在 CPU 上对整张图算一次并烘成贴图。
//
// 算法（对每个连通部件自适应）：
//   1. alpha > 0.5 视为实体，做倒角距离变换得到每个像素到轮廓的距离 dist。
//   2. BFS 标记连通部件，取每个部件的最大内切半径 R。
//   3. 高度场 h = 圆顶( dist / R ) + 亮度高频浮雕。
//      用 R 归一化是关键：不管部件大小（头 / 手 / 枪），都鼓成一个完整的圆顶。
//   4. 对 h 做 Sobel 求梯度 → 切线空间法线。
//
// 用法：菜单 ARK/角色光照/烘焙法线贴图（当前选中）或（整目录）

using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ARK.EditorTools
{
    public static class ARK_SpriteNormalBaker
    {
        // ---------- 可调参数 ----------
        public static float DomeGain = 1.0f;       // 圆顶强度
        public static float DetailGain = 0.25f;    // 亮度浮雕强度（太高会把画面原有明暗放大成条纹）
        public static float NormalStrength = 1.05f;// 整体梯度放大
        public static float MinRadius = 2.5f;      // 极小部件的半径下限（防过鼓）
        public static int MaxSize = 512;           // 输出法线最大边长（未指定时的默认值）
        public static bool Overwrite = false;

        /// <summary>画质档位：决定法线分辨率。档位越高越占显存，画面体积感越细。</summary>
        public enum QualityTier { High = 0, Medium = 1, Low = 2 }

        // 档位 → (主角, 杂兵) 法线最大边长。
        // 主角在屏幕上约 1000px 高，杂兵小得多；法线是低频信息，不必与 albedo 同分辨率。
        static readonly int[] TierHeroSize = { 512, 384, 256 };
        static readonly int[] TierMobSize = { 320, 256, 160 };

        public static QualityTier Tier = QualityTier.Medium;

        /// <summary>法线统一输出到 Resources，保证打包后能用 Resources.Load 找到。</summary>
        public const string OutputRoot = "Assets/Resources/ARK_Normals";

        public static int SizeForPath(string spritePath, QualityTier tier)
        {
            bool hero = spritePath.Replace('\\', '/').Contains("/Jessica/");
            return hero ? TierHeroSize[(int)tier] : TierMobSize[(int)tier];
        }

        static string OutPathFor(string spritePath)
        {
            if (string.IsNullOrEmpty(OutputRoot)) return Path.ChangeExtension(spritePath, null) + "_N.png";
            if (!Directory.Exists(OutputRoot)) Directory.CreateDirectory(OutputRoot);
            // 扁平输出、按文件名索引：运行时只能拿到 sprite.texture.name，拿不到工程路径。
            return OutputRoot + "/" + Path.GetFileNameWithoutExtension(spritePath) + "_N.png";
        }

        /// <summary>烘焙单张精灵图（默认分辨率，输出到 OutputRoot 或源目录旁）</summary>
        public static string BakeOne(string spritePath)
        {
            return BakeOne(spritePath, Mathf.Max(MaxSize, SizeForPath(spritePath, Tier)));
        }

        /// <summary>烘焙单张精灵图，指定输出最大边长</summary>
        public static string BakeOne(string spritePath, int maxSize)
        {
            var bytes = File.ReadAllBytes(spritePath);
            var src = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!src.LoadImage(bytes))
            {
                Object.DestroyImmediate(src);
                return "读取失败: " + spritePath;
            }

            Color[] srcPx = src.GetPixels();
            int sw = src.width, sh = src.height;
            Object.DestroyImmediate(src);

            // ---- 降采样到 maxSize 以内（法线是低频信息，不需要原始 2K）----
            // 边长必须对齐到 4 的倍数：否则 DXT/BC 块压缩无法工作，Unity 会静默退回
            // RGBA32 未压缩，显存直接翻 4 倍（303x384 就是踩了这个坑）。
            int tw = sw, th = sh;
            float scale = 1f;
            if (Mathf.Max(sw, sh) > maxSize)
            {
                scale = (float)maxSize / Mathf.Max(sw, sh);
            }
            tw = Mathf.Max(4, (Mathf.FloorToInt(sw * scale) / 4) * 4);
            th = Mathf.Max(4, (Mathf.FloorToInt(sh * scale) / 4) * 4);

            var px = new Color[tw * th];
            for (int y = 0; y < th; y++)
            {
                int sy0 = Mathf.FloorToInt(y / scale);
                int sy1 = Mathf.Max(sy0 + 1, Mathf.FloorToInt((y + 1) / scale));
                for (int x = 0; x < tw; x++)
                {
                    int sx0 = Mathf.FloorToInt(x / scale);
                    int sx1 = Mathf.Max(sx0 + 1, Mathf.FloorToInt((x + 1) / scale));
                    float r = 0, g = 0, b = 0, a = 0;
                    int cnt = 0;
                    for (int sy = sy0; sy < sy1 && sy < sh; sy++)
                    {
                        for (int sx = sx0; sx < sx1 && sx < sw; sx++)
                        {
                            var c = srcPx[sy * sw + sx];
                            r += c.r; g += c.g; b += c.b; a += c.a;
                            cnt++;
                        }
                    }
                    if (cnt == 0) cnt = 1;
                    px[y * tw + x] = new Color(r / cnt, g / cnt, b / cnt, a / cnt);
                }
            }

            int n = tw * th;

            // ---- 1) 倒角距离变换 ----
            const float BIG = 1e6f;
            var dist = new float[n];
            for (int i = 0; i < n; i++) dist[i] = px[i].a > 0.5f ? BIG : 0f;

            for (int y = 0; y < th; y++)
            {
                for (int x = 0; x < tw; x++)
                {
                    int i = y * tw + x;
                    if (dist[i] <= 0f) continue;
                    float d = dist[i];
                    if (x > 0) d = Mathf.Min(d, dist[i - 1] + 1f);
                    if (y > 0)
                    {
                        d = Mathf.Min(d, dist[i - tw] + 1f);
                        if (x > 0) d = Mathf.Min(d, dist[i - tw - 1] + 1.4142f);
                        if (x < tw - 1) d = Mathf.Min(d, dist[i - tw + 1] + 1.4142f);
                    }
                    dist[i] = d;
                }
            }
            for (int y = th - 1; y >= 0; y--)
            {
                for (int x = tw - 1; x >= 0; x--)
                {
                    int i = y * tw + x;
                    if (dist[i] <= 0f) continue;
                    float d = dist[i];
                    if (x < tw - 1) d = Mathf.Min(d, dist[i + 1] + 1f);
                    if (y < th - 1)
                    {
                        d = Mathf.Min(d, dist[i + tw] + 1f);
                        if (x < tw - 1) d = Mathf.Min(d, dist[i + tw + 1] + 1.4142f);
                        if (x > 0) d = Mathf.Min(d, dist[i + tw - 1] + 1.4142f);
                    }
                    dist[i] = d;
                }
            }

            // ---- 2) 连通部件 + 各自最大内切半径 ----
            var label = new int[n];
            var queue = new int[n];
            int labelCount = 0;
            var compRadius = new List<float> { 1f };
            for (int start = 0; start < n; start++)
            {
                if (px[start].a <= 0.5f || label[start] != 0) continue;
                labelCount++;
                int head = 0, tail = 0;
                queue[tail++] = start;
                label[start] = labelCount;
                float maxD = 0f;
                while (head < tail)
                {
                    int i = queue[head++];
                    if (dist[i] > maxD) maxD = dist[i];
                    int cx = i % tw, cy = i / tw;
                    if (cx > 0) Push(queue, ref tail, label, px, i - 1, labelCount);
                    if (cx < tw - 1) Push(queue, ref tail, label, px, i + 1, labelCount);
                    if (cy > 0) Push(queue, ref tail, label, px, i - tw, labelCount);
                    if (cy < th - 1) Push(queue, ref tail, label, px, i + tw, labelCount);
                }
                compRadius.Add(Mathf.Max(maxD, MinRadius));
            }

            // ---- 3) 高度场：圆顶 + 亮度浮雕 ----
            var lum = new float[n];
            for (int i = 0; i < n; i++)
            {
                float a = px[i].a;
                lum[i] = Mathf.Clamp01(0.299f * px[i].r + 0.587f * px[i].g + 0.114f * px[i].b) * Mathf.Clamp01(a * 3f);
            }

            var lumBlur = new float[n];
            for (int y = 0; y < th; y++)
            {
                for (int x = 0; x < tw; x++)
                {
                    int i = y * tw + x;
                    if (px[i].a <= 0.02f) { lumBlur[i] = 0f; continue; }
                    float sum = 0f, wsum = 0f;
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            int nx2 = x + dx, ny2 = y + dy;
                            if (nx2 < 0 || ny2 < 0 || nx2 >= tw || ny2 >= th) continue;
                            int j = ny2 * tw + nx2;
                            if (px[j].a <= 0.02f) continue;
                            float wgt = (dx == 0 && dy == 0) ? 4f : ((dx == 0 || dy == 0) ? 2f : 1f);
                            sum += lum[j] * wgt;
                            wsum += wgt;
                        }
                    }
                    lumBlur[i] = wsum > 0f ? sum / wsum : lum[i];
                }
            }

            var hgt = new float[n];
            for (int i = 0; i < n; i++)
            {
                if (px[i].a <= 0.02f) { hgt[i] = 0f; continue; }
                float R = compRadius[label[i]];
                float d = dist[i];
                // 高度必须以"像素"为单位：Sobel 是按像素求导的，若把圆顶归一化到 0..1，
                // 梯度只有 1/R 量级（R≈30 时约 0.03），法线会平得像没做——这是最容易踩的坑。
                // 多尺度圆顶：大尺度给整体体积，小尺度给边缘圆润。
                // 整张角色通常只有一个连通域，靠分尺度补偿缺失的"部件级"起伏。
                float dome = DomeHeight(d, R, 1.0f)
                           + DomeHeight(d, R / 2.5f, 0.45f)
                           + DomeHeight(d, R / 6.0f, 0.22f);
                dome *= DomeGain;
                float detail = Mathf.Clamp((lum[i] - lumBlur[i]) * 2.5f, -0.35f, 0.35f) * DetailGain;
                hgt[i] = dome + detail;
            }

            // ---- 4) Sobel → 法线 ----
            var outPx = new Color[n];
            for (int y = 0; y < th; y++)
            {
                for (int x = 0; x < tw; x++)
                {
                    int i = y * tw + x;
                    if (px[i].a <= 0.02f) { outPx[i] = new Color(0.5f, 0.5f, 1f, 1f); continue; }

                    int xm = Mathf.Max(x - 1, 0), xp = Mathf.Min(x + 1, tw - 1);
                    int ym = Mathf.Max(y - 1, 0), yp = Mathf.Min(y + 1, th - 1);

                    float h00 = hgt[ym * tw + xm], h10 = hgt[ym * tw + x], h20 = hgt[ym * tw + xp];
                    float h01 = hgt[y * tw + xm], h21 = hgt[y * tw + xp];
                    float h02 = hgt[yp * tw + xm], h12 = hgt[yp * tw + x], h22 = hgt[yp * tw + xp];

                    float gx = (h20 + 2f * h21 + h22) - (h00 + 2f * h01 + h02);
                    float gy = (h02 + 2f * h12 + h22) - (h00 + 2f * h10 + h20);

                    var nrm = new Vector3(-gx * NormalStrength / 8f, -gy * NormalStrength / 8f, 1f);
                    // 钳制极端梯度，避免轮廓处出现硬边/死黑
                    nrm.x = Mathf.Clamp(nrm.x, -2.5f, 2.5f);
                    nrm.y = Mathf.Clamp(nrm.y, -2.5f, 2.5f);
                    nrm.Normalize();
                    outPx[i] = new Color(nrm.x * 0.5f + 0.5f, nrm.y * 0.5f + 0.5f, nrm.z * 0.5f + 0.5f, 1f);
                }
            }

            // ---- 5) 写出 + 导入设置 ----
            var nt = new Texture2D(tw, th, TextureFormat.RGBA32, false);
            nt.SetPixels(outPx);
            byte[] png = nt.EncodeToPNG();
            Object.DestroyImmediate(nt);

            string outPath = OutPathFor(spritePath);
            File.WriteAllBytes(outPath, png);
            AssetDatabase.ImportAsset(outPath, ImportAssetOptions.ForceUpdate);

            var ti = AssetImporter.GetAtPath(outPath) as TextureImporter;
            if (ti != null)
            {
                ti.textureType = TextureImporterType.NormalMap;
                ti.sRGBTexture = false;
                ti.alphaSource = TextureImporterAlphaSource.None;
                ti.mipmapEnabled = false;
                ti.wrapMode = TextureWrapMode.Clamp;
                ti.filterMode = FilterMode.Bilinear;
                ti.maxTextureSize = Mathf.Max(maxSize, 32);
                ti.textureCompression = TextureImporterCompression.Compressed;
                ti.SaveAndReimport();
            }

            return string.Format("{0} -> {1}x{2}, 部件={3}", Path.GetFileName(spritePath), tw, th, labelCount);
        }

        /// <summary>
        /// 圆顶截面高度（像素单位）。dist：到轮廓的距离；r：圆顶半径；w：权重。
        /// 用 smoothstep 而不是半球 sqrt：半球在轮廓处斜率发散，会让角色边缘出现一圈死黑；
        /// smoothstep 两端斜率都为 0，得到"两侧都圆润"的软倒角。
        /// </summary>
        static float DomeHeight(float dist, float r, float w)
        {
            if (r <= 0.0001f) return 0f;
            float t = Mathf.Clamp01(dist / r);
            float s = t * t * (3f - 2f * t);
            return w * r * s;
        }

        static void Push(int[] queue, ref int tail, int[] label, Color[] px, int idx, int lbl)
        {
            if (px[idx].a <= 0.5f || label[idx] != 0) return;
            label[idx] = lbl;
            queue[tail++] = idx;
        }

        // ---------- 菜单入口 ----------

        [MenuItem("ARK/角色光照/烘焙选中贴图的法线")]
        static void BakeSelected()
        {
            int ok = 0;
            foreach (var obj in Selection.objects)
            {
                string path = AssetDatabase.GetAssetPath(obj);
                if (string.IsNullOrEmpty(path)) continue;
                if (!path.EndsWith(".png") && !path.EndsWith(".tga") && !path.EndsWith(".psd")) continue;
                if (path.EndsWith("_N.png")) continue;
                Debug.Log("[ARK] 烘焙法线 " + BakeOne(path));
                ok++;
            }
            if (ok == 0) Debug.LogWarning("[ARK] 请先选中若干张精灵图（.png）。");
            else AssetDatabase.Refresh();
        }

        [MenuItem("ARK/角色光照/烘焙目录下的法线（含子目录）")]
        static void BakeFolder()
        {
            var folder = EditorUtility.OpenFolderPanel("选择要烘焙的贴图目录", Application.dataPath, "");
            if (string.IsNullOrEmpty(folder)) return;

            string projectRoot = Directory.GetParent(Application.dataPath).FullName.Replace('\\', '/');
            folder = folder.Replace('\\', '/');
            if (!folder.StartsWith(projectRoot))
            {
                Debug.LogError("[ARK] 目录必须在工程内: " + folder);
                return;
            }
            string rel = folder.Substring(projectRoot.Length).TrimStart('/');

            var files = Directory.GetFiles(folder, "*.png", SearchOption.AllDirectories);
            int count = 0;
            try
            {
                for (int i = 0; i < files.Length; i++)
                {
                    string f = files[i].Replace('\\', '/');
                    if (f.EndsWith("_N.png")) continue;
                    string outPath = Path.ChangeExtension(f, null) + "_N.png";
                    if (!Overwrite && File.Exists(outPath)) continue;

                    EditorUtility.DisplayProgressBar("ARK 烘焙法线", Path.GetFileName(f), (float)i / files.Length);
                    Debug.Log("[ARK] " + BakeOne(f));
                    count++;
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                AssetDatabase.Refresh();
            }
            Debug.Log("[ARK] 完成，共烘焙 " + count + " 张 -> " + rel);
        }

        // ================= 全量烘焙（分批执行，供外部循环调用） =================

        /// <summary>要全量烘焙的角色美术目录（相对 Assets）。</summary>
        public static readonly string[] CharacterRoots =
        {
            "Assets/ArtRes/Jessica",
            "Assets/ArtRes/Enemy",
            "Assets/ArtRes/Npc"
        };

        /// <summary>按固定顺序列出所有待烘焙的源图（跳过已生成的 _N.png）。</summary>
        public static string[] CollectSources()
        {
            var list = new List<string>();
            foreach (var root in CharacterRoots)
            {
                if (!Directory.Exists(root)) continue;
                var files = Directory.GetFiles(root, "*.png", SearchOption.AllDirectories);
                System.Array.Sort(files, System.StringComparer.Ordinal);
                foreach (var f in files)
                {
                    string p = f.Replace('\\', '/');
                    if (p.EndsWith("_N.png")) continue;
                    list.Add(p);
                }
            }
            return list.ToArray();
        }

        static string BakeStatePath
        {
            get { return Path.Combine(Path.GetDirectoryName(Application.dataPath), "Temp/ark_bake_state.txt"); }
        }

        /// <summary>清零烘焙进度（重新全量烘焙前调用）</summary>
        public static void ResetBakeState()
        {
            try { if (File.Exists(BakeStatePath)) File.Delete(BakeStatePath); } catch { }
        }

        /// <summary>
        /// 烘焙 [start, ...) 区间，直到超过 budgetSeconds 秒。
        /// start 传 -1 表示从磁盘上的进度文件续跑 —— 这是关键：单次 execute_code 可能失败，
        /// 若进度只在调用方内存里，失败一次就会把整段重做一遍（Overwrite 模式下尤其糟糕）。
        /// 返回 "total=.. done=.. next=.. elapsed=.. size=.. last=.."
        /// </summary>
        public static string BakeRange(int tier, int start, int budgetSeconds)
        {
            Tier = (QualityTier)Mathf.Clamp(tier, 0, 2);
            var all = CollectSources();
            int total = all.Length;

            if (start < 0)
            {
                int s = 0;
                try { if (File.Exists(BakeStatePath)) int.TryParse(File.ReadAllText(BakeStatePath).Trim(), out s); }
                catch { s = 0; }
                start = Mathf.Clamp(s, 0, total);
            }

            int i = start;
            int done = 0;
            string last = "-";
            var sw = System.Diagnostics.Stopwatch.StartNew();

            while (i < total && sw.Elapsed.TotalSeconds < budgetSeconds)
            {
                string src = all[i];
                int size = SizeForPath(src, Tier);
                string outPath = OutPathFor(src);
                if (!Overwrite && File.Exists(outPath))
                {
                    i++;
                    done++;
                    SaveState(i);
                    continue;
                }
                try
                {
                    last = BakeOne(src, size);
                }
                catch (System.Exception e)
                {
                    last = "FAILED " + src + " : " + e.Message;
                    Debug.LogWarning("[ARK] " + last);
                }
                i++;
                done++;
                SaveState(i);
            }

            return string.Format("total={0} done={1} next={2} elapsed={3:0.0} size={4} last={5}",
                total, done, i, sw.Elapsed.TotalSeconds,
                (int)Tier, last);
        }

        static void SaveState(int index)
        {
            try { File.WriteAllText(BakeStatePath, index.ToString()); } catch { }
        }

        [MenuItem("ARK/角色光照/全量烘焙 - 高画质（主角512 / 杂兵320）")]
        static void BakeAllHigh() { BakeAll(QualityTier.High); }

        [MenuItem("ARK/角色光照/全量烘焙 - 中画质（主角384 / 杂兵256）")]
        static void BakeAllMedium() { BakeAll(QualityTier.Medium); }

        [MenuItem("ARK/角色光照/全量烘焙 - 低画质（主角256 / 杂兵160）")]
        static void BakeAllLow() { BakeAll(QualityTier.Low); }

        static void BakeAll(QualityTier tier)
        {
            var all = CollectSources();
            if (!EditorUtility.DisplayDialog("ARK 烘焙法线",
                "将烘焙 " + all.Length + " 张精灵图到 " + OutputRoot + "\n档位：" + tier +
                "（主角 " + TierHeroSize[(int)tier] + " / 杂兵 " + TierMobSize[(int)tier] + "）",
                "开始", "取消")) return;

            Tier = tier;
            int i = 0;
            int guard = 0;
            while (i < all.Length && guard++ < 100000)
            {
                EditorUtility.DisplayProgressBar("ARK 烘焙法线", "剩余 " + (all.Length - i), (float)i / all.Length);
                string r = BakeRange((int)tier, i, 5);
                int next;
                int p = r.IndexOf("next=");
                int q = r.IndexOf(' ', p);
                if (p < 0) break;
                next = int.Parse(r.Substring(p + 5, (q < 0 ? r.Length : q) - p - 5));
                if (next <= i) break;
                i = next;
            }
            EditorUtility.ClearProgressBar();
            AssetDatabase.Refresh();
            Debug.Log("[ARK] 全量烘焙完成，档位=" + tier);
        }
    }
}
