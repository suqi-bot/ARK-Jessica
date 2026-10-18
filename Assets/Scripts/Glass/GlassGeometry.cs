using System.Collections.Generic;
using UnityEngine;

namespace ARK.Glass
{
    /// <summary>
    /// 玻璃几何工具：多边形运算、Voronoi 剖分、棱柱网格构建。
    /// 所有多边形都在局部 XY 平面，顶点按逆时针排列（从 +Z 看）。
    /// </summary>
    public static class GlassGeometry
    {
        public const float Eps = 1e-5f;

        // ───────────────────────────── 基础多边形 ─────────────────────────────

        /// <summary>有符号面积，逆时针为正。</summary>
        public static float SignedArea(IList<Vector2> poly)
        {
            float s = 0f;
            int n = poly.Count;
            for (int i = 0; i < n; i++)
            {
                Vector2 a = poly[i];
                Vector2 b = poly[(i + 1) % n];
                s += a.x * b.y - b.x * a.y;
            }
            return s * 0.5f;
        }

        /// <summary>多边形质心（面积加权）。退化时退回到顶点平均。</summary>
        public static Vector2 Centroid(IList<Vector2> poly)
        {
            int n = poly.Count;
            if (n == 0) return Vector2.zero;
            if (n < 3) return Average(poly);

            float area = 0f;
            Vector2 c = Vector2.zero;
            for (int i = 0; i < n; i++)
            {
                Vector2 a = poly[i];
                Vector2 b = poly[(i + 1) % n];
                float cross = a.x * b.y - b.x * a.y;
                area += cross;
                c += (a + b) * cross;
            }
            area *= 0.5f;
            if (Mathf.Abs(area) < Eps) return Average(poly);
            c /= (6f * area);
            return c;
        }

        public static Vector2 Average(IList<Vector2> poly)
        {
            if (poly.Count == 0) return Vector2.zero;
            Vector2 sum = Vector2.zero;
            for (int i = 0; i < poly.Count; i++) sum += poly[i];
            return sum / poly.Count;
        }

        public static bool PointInPolygon(IList<Vector2> poly, Vector2 p)
        {
            bool inside = false;
            int n = poly.Count;
            for (int i = 0, j = n - 1; i < n; j = i++)
            {
                Vector2 a = poly[i];
                Vector2 b = poly[j];
                if (((a.y > p.y) != (b.y > p.y)) &&
                    (p.x < (b.x - a.x) * (p.y - a.y) / (b.y - a.y + 1e-12f) + a.x))
                {
                    inside = !inside;
                }
            }
            return inside;
        }

        public static bool EnsureCounterClockwise(List<Vector2> poly)
        {
            if (SignedArea(poly) < 0f)
            {
                poly.Reverse();
                return true;
            }
            return false;
        }

        public static void Translate(List<Vector2> poly, Vector2 offset)
        {
            for (int i = 0; i < poly.Count; i++) poly[i] += offset;
        }

        // ───────────────────────────── 形状生成 ─────────────────────────────

        public static List<Vector2> MakeBox(Vector2 size)
        {
            Vector2 h = size * 0.5f;
            return new List<Vector2>
            {
                new Vector2(-h.x, -h.y),
                new Vector2( h.x, -h.y),
                new Vector2( h.x,  h.y),
                new Vector2(-h.x,  h.y),
            };
        }

        public static List<Vector2> MakeEllipse(Vector2 radius, int segments)
        {
            segments = Mathf.Max(5, segments);
            var poly = new List<Vector2>(segments);
            for (int i = 0; i < segments; i++)
            {
                float t = (float)i / segments * Mathf.PI * 2f;
                poly.Add(new Vector2(Mathf.Cos(t) * radius.x, Mathf.Sin(t) * radius.y));
            }
            return poly;
        }

        /// <summary>圆角矩形（做玻璃窗/玻璃箱很实用）。</summary>
        public static List<Vector2> MakeRoundedBox(Vector2 size, float radius, int cornerSegments)
        {
            Vector2 h = size * 0.5f;
            radius = Mathf.Clamp(radius, 0f, Mathf.Min(h.x, h.y) * 0.999f);
            cornerSegments = Mathf.Max(1, cornerSegments);
            var poly = new List<Vector2>();

            Vector2[] corners =
            {
                new Vector2( h.x - radius, -h.y + radius),
                new Vector2( h.x - radius,  h.y - radius),
                new Vector2(-h.x + radius,  h.y - radius),
                new Vector2(-h.x + radius, -h.y + radius),
            };
            float[] startAngles = { -90f, 0f, 90f, 180f };

            for (int c = 0; c < 4; c++)
            {
                for (int s = 0; s <= cornerSegments; s++)
                {
                    float ang = (startAngles[c] + 90f * s / cornerSegments) * Mathf.Deg2Rad;
                    poly.Add(corners[c] + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * radius);
                }
            }
            return poly;
        }

        // ───────────────────────────── 半平面裁剪 ─────────────────────────────

        /// <summary>
        /// Sutherland–Hodgman：保留 dot(x - p, n) &lt;= 0 的一侧。
        /// </summary>
        public static List<Vector2> ClipHalfPlane(IList<Vector2> poly, Vector2 p, Vector2 n)
        {
            var result = new List<Vector2>(poly.Count + 2);
            int count = poly.Count;
            if (count == 0) return result;

            for (int i = 0; i < count; i++)
            {
                Vector2 cur = poly[i];
                Vector2 nxt = poly[(i + 1) % count];

                float dCur = (cur.x - p.x) * n.x + (cur.y - p.y) * n.y;
                float dNxt = (nxt.x - p.x) * n.x + (nxt.y - p.y) * n.y;

                bool curIn = dCur <= Eps;
                bool nxtIn = dNxt <= Eps;

                if (curIn) result.Add(cur);

                if (curIn != nxtIn)
                {
                    float denom = dCur - dNxt;
                    if (Mathf.Abs(denom) > 1e-9f)
                    {
                        float t = dCur / denom;
                        result.Add(Vector2.Lerp(cur, nxt, Mathf.Clamp01(t)));
                    }
                }
            }
            return result;
        }

        // ───────────────────────────── Voronoi 剖分 ─────────────────────────────

        /// <summary>
        /// 对多边形做 Voronoi 剖分（半平面裁剪实现，碎片数几十个时性能完全够用）。
        /// 返回每个单元的多边形列表，已剔除退化单元。
        /// </summary>
        public static List<List<Vector2>> VoronoiCells(IList<Vector2> poly, IList<Vector2> seeds, float minArea)
        {
            var cells = new List<List<Vector2>>(seeds.Count);
            int n = seeds.Count;
            if (n == 0) return cells;

            for (int i = 0; i < n; i++)
            {
                Vector2 pi = seeds[i];
                var cell = new List<Vector2>(poly);

                for (int j = 0; j < n; j++)
                {
                    if (i == j) continue;
                    Vector2 pj = seeds[j];
                    Vector2 mid = (pi + pj) * 0.5f;
                    Vector2 dir = pj - pi;
                    float len = dir.magnitude;
                    if (len < 1e-6f) continue;
                    Vector2 normal = dir / len; // 指向 pj，因此保留的是靠 pi 的一侧
                    cell = ClipHalfPlane(cell, mid, normal);
                    if (cell.Count < 3) break;
                }

                if (cell.Count >= 3 && Mathf.Abs(SignedArea(cell)) >= minArea)
                    cells.Add(cell);
            }
            return cells;
        }

        /// <summary>
        /// 撒种子点：以命中点为中心做密度加权（命中处碎片细密，远处大块）。
        /// impactRatio: 受命中点影响的点比例   shrinkPow: 向命中点收缩的力度
        /// </summary>
        public static List<Vector2> ScatterSeeds(IList<Vector2> poly, Vector2 impact, int count,
                                                 float impactRatio, float shrinkPow, System.Random rnd)
        {
            var seeds = new List<Vector2>(count);
            Vector2 c = Centroid(poly);

            // 命中点本身一定是一个种子：保证碎片以击中点为放射中心
            seeds.Add(impact);

            // 包围盒，用于拒绝采样
            float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
            for (int i = 0; i < poly.Count; i++)
            {
                Vector2 v = poly[i];
                if (v.x < minX) minX = v.x;
                if (v.y < minY) minY = v.y;
                if (v.x > maxX) maxX = v.x;
                if (v.y > maxY) maxY = v.y;
            }

            int guard = 0;
            while (seeds.Count < count && guard < count * 80)
            {
                guard++;
                float x = Mathf.Lerp(minX, maxX, (float)rnd.NextDouble());
                float y = Mathf.Lerp(minY, maxY, (float)rnd.NextDouble());
                var p = new Vector2(x, y);
                if (!PointInPolygon(poly, p)) continue;

                // 一定比例的点向命中点收缩 → 中心碎片更碎
                if (rnd.NextDouble() < impactRatio * rnd.NextDouble())
                {
                    float k = Mathf.Pow((float)rnd.NextDouble(), shrinkPow);
                    p = Vector2.Lerp(impact, p, k);
                }
                seeds.Add(p);
            }

            // 兜底：极端形状下采样不足时补质心附近点
            while (seeds.Count < 4)
                seeds.Add(Vector2.Lerp(c, impact, (float)rnd.NextDouble()));

            return seeds;
        }

        // ───────────────────────────── 棱柱网格 ─────────────────────────────

        /// <summary>
        /// 把二维多边形挤出成有厚度的棱柱网格（正面朝 -Z，即朝向 z=-10 的主相机）。
        /// </summary>
        public static Mesh BuildPrism(IList<Vector2> poly, float thickness, Vector2 center, Mesh reuse = null)
        {
            int n = poly.Count;
            if (n < 3) return null;

            float halfT = Mathf.Max(0.0005f, thickness * 0.5f);
            var verts = new List<Vector3>(n * 2 + n * 4);
            var normals = new List<Vector3>(n * 2 + n * 4);
            var uvs = new List<Vector2>(n * 2 + n * 4);
            var tris = new List<int>(n * 12);

            // 顶点：0..n-1 正面(-Z)，n..2n-1 背面(+Z)
            for (int i = 0; i < n; i++)
            {
                Vector2 v = poly[i] - center;
                verts.Add(new Vector3(v.x, v.y, -halfT));
                normals.Add(new Vector3(0f, 0f, -1f));
                uvs.Add(new Vector2(v.x, v.y));
            }
            for (int i = 0; i < n; i++)
            {
                Vector2 v = poly[i] - center;
                verts.Add(new Vector3(v.x, v.y, halfT));
                normals.Add(new Vector3(0f, 0f, 1f));
                uvs.Add(new Vector2(v.x, v.y));
            }

            // 正面：逆时针多边形从 -Z 看是顺时针 → Unity 正面
            for (int i = 1; i < n - 1; i++)
            {
                tris.Add(0);
                tris.Add(i);
                tris.Add(i + 1);
            }
            // 背面：反向
            for (int i = 1; i < n - 1; i++)
            {
                tris.Add(n);
                tris.Add(n + i + 1);
                tris.Add(n + i);
            }

            // 侧面
            for (int i = 0; i < n; i++)
            {
                int next = (i + 1) % n;
                Vector2 a = poly[i] - center;
                Vector2 b = poly[next] - center;
                Vector2 edge = b - a;
                Vector3 sideNormal = new Vector3(edge.y, -edge.x, 0f).normalized;

                int baseIdx = verts.Count;
                verts.Add(new Vector3(a.x, a.y, -halfT)); // 0 正面 a
                verts.Add(new Vector3(b.x, b.y, -halfT)); // 1 正面 b
                verts.Add(new Vector3(b.x, b.y, halfT));  // 2 背面 b
                verts.Add(new Vector3(a.x, a.y, halfT));  // 3 背面 a
                for (int k = 0; k < 4; k++) normals.Add(sideNormal);
                uvs.Add(new Vector2(0f, 0f));
                uvs.Add(new Vector2(1f, 0f));
                uvs.Add(new Vector2(1f, 1f));
                uvs.Add(new Vector2(0f, 1f));

                tris.Add(baseIdx + 0);
                tris.Add(baseIdx + 1);
                tris.Add(baseIdx + 2);
                tris.Add(baseIdx + 0);
                tris.Add(baseIdx + 2);
                tris.Add(baseIdx + 3);
            }

            var mesh = reuse != null ? reuse : new Mesh();
            mesh.Clear();
            mesh.name = "GlassPrism";
            if (verts.Count > 65000) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(verts);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>取多边形所有边的法线，用于简单 OBB 判定（判断命中点是否在玻璃正面）。</summary>
        public static float DistanceToPolygonEdge(IList<Vector2> poly, Vector2 p)
        {
            float best = float.MaxValue;
            int n = poly.Count;
            for (int i = 0; i < n; i++)
            {
                Vector2 a = poly[i];
                Vector2 b = poly[(i + 1) % n];
                Vector2 ab = b - a;
                float len2 = ab.sqrMagnitude;
                float t = len2 < 1e-9f ? 0f : Mathf.Clamp01(Vector2.Dot(p - a, ab) / len2);
                Vector2 proj = a + ab * t;
                float d = (p - proj).sqrMagnitude;
                if (d < best) best = d;
            }
            return Mathf.Sqrt(best);
        }
    }
}
