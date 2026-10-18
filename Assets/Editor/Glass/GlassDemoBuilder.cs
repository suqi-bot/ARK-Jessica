using System.Collections.Generic;
using ARK.Glass;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ARK.Glass.EditorTools
{
    /// <summary>
    /// 玻璃演示场景与示例物体构建器。
    /// 菜单：Tools/ARK/玻璃/创建演示场景
    /// </summary>
    public static class GlassDemoBuilder
    {
        const string MatRoot = "Assets/ArtRes/Glass";

        [MenuItem("Tools/ARK/玻璃/创建演示场景", false, 21)]
        public static void BuildDemoScene()
        {
            BuildDemoSceneInternal(true);
        }

        /// <summary>静默重建（自动化验收用，不弹保存对话框）。</summary>
        public static void BuildDemoSceneSilent()
        {
            BuildDemoSceneInternal(false);
        }

        private static void BuildDemoSceneInternal(bool interactive)
        {
            if (interactive && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // ── 环境 ──
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            camGo.AddComponent<AudioListener>();
            camGo.transform.position = new Vector3(0f, 2.2f, -8f);
            cam.fieldOfView = 55f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.12f, 0.14f, 0.18f);

            var lightGo = new GameObject("Directional Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.15f;
            light.color = new Color(1f, 0.97f, 0.9f);
            lightGo.transform.rotation = Quaternion.Euler(42f, -28f, 0f);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.30f, 0.34f, 0.40f);

            // ── 地面（3D 视觉 + 2D 物理，和项目一致） ──
            var groundMat = GetOrCreateMaterial(MatRoot + "/Demo_Ground.mat", "Legacy Shaders/Diffuse",
                new Color(0.30f, 0.31f, 0.32f, 1f));
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Ground";
            ground.transform.position = new Vector3(0f, -0.2f, 0f);
            ground.transform.localScale = new Vector3(22f, 0.4f, 4f);
            ground.GetComponent<MeshRenderer>().sharedMaterial = groundMat;
            Object.DestroyImmediate(ground.GetComponent<BoxCollider>());
            var gcol = ground.AddComponent<BoxCollider2D>();
            gcol.size = new Vector2(1f, 1f);

            // ── 背景物体：用来验证玻璃的透视/半透明效果 ──
            CreateBackdrop("Backdrop_A", new Vector3(-2.6f, 1.6f, 5.5f), new Vector3(2.6f, 3.4f, 0.7f),
                new Color(0.86f, 0.44f, 0.26f));
            CreateBackdrop("Backdrop_B", new Vector3(-0.1f, 1.2f, 8f), new Vector3(2.2f, 2.6f, 0.7f),
                new Color(0.23f, 0.62f, 0.68f));
            CreateBackdrop("Backdrop_C", new Vector3(2.6f, 2.0f, 10f), new Vector3(2.8f, 3.8f, 0.7f),
                new Color(0.52f, 0.40f, 0.72f));

            // ── 玻璃窗（带金属框） ──
            var windowMat = AssetDatabase.LoadAssetAtPath<Material>(MatRoot + "/Glass_Panel.mat");
            var frameMat = GetOrCreateMaterial(MatRoot + "/Demo_Frame.mat", "Legacy Shaders/Diffuse",
                new Color(0.20f, 0.21f, 0.24f, 1f));

            CreateGlassWindow("GlassWindow_A", new Vector3(-2.4f, 2.2f, 0f), new Vector2(1.6f, 2.0f),
                windowMat, frameMat, 0.1f);

            CreateGlassWindow("GlassWindow_B", new Vector3(-0.2f, 2.2f, 0.6f), new Vector2(1.8f, 2.0f),
                windowMat, frameMat, 0.25f);

            CreateGlassWindow("GlassWindow_C", new Vector3(2.4f, 2.2f, 1.2f), new Vector2(1.6f, 2.0f),
                windowMat, frameMat, 0.55f);

            // ── 玻璃物件（箱子 / 瓶子） ──
            var bottlePanel = CreateGlassObject("GlassBox", new Vector3(1.0f, 0.55f, -0.4f),
                GlassPanel.ShapeType.RoundedBox, new Vector2(0.75f, 0.95f), windowMat, 0.18f);
            bottlePanel.fragmentCount = 26;

            var jar = CreateGlassObject("GlassJar", new Vector3(2.0f, 0.5f, -0.4f),
                GlassPanel.ShapeType.Ellipse, new Vector2(0.8f, 1.0f), windowMat, 0.32f);
            jar.ellipseSegments = 20;

            // ── 自动演示 ──
            var driverGo = new GameObject("GlassDemoDriver");
            var driver = driverGo.AddComponent<GlassDemoDriver>();
            // 固定攻击顺序：先打窗户（面积大、裂纹更明显），最后打小物件
            var ordered = new List<GlassPanel>();
            string[] names = { "GlassWindow_A", "GlassWindow_B", "GlassWindow_C", "GlassBox", "GlassJar" };
            foreach (var n in names)
            {
                var go = GameObject.Find(n);
                if (go == null) continue;
                var panel = go.GetComponentInChildren<GlassPanel>();
                if (panel != null) ordered.Add(panel);
            }
            if (ordered.Count == 0) ordered.AddRange(Object.FindObjectsOfType<GlassPanel>(true));
            driver.targets = ordered.ToArray();
            driver.startDelay = 0.9f;
            driver.interval = 0.42f;
            driver.damage = 34f;
            driver.enabled = true;

            string dir = "Assets/Scenes";
            if (!AssetDatabase.IsValidFolder(dir)) AssetDatabase.CreateFolder("Assets", "Scenes");
            EditorSceneManager.SaveScene(scene, dir + "/GlassTestScene.unity");
            AssetDatabase.Refresh();
            Debug.Log("[Glass] 演示场景已创建：Assets/Scenes/GlassTestScene.unity");
        }

        // ───────────────────────────── 场景内快速放置 ─────────────────────────────

        [MenuItem("Tools/ARK/玻璃/放置玻璃窗（场景视图中心）", false, 31)]
        public static void PlaceGlassWindowAtView()
        {
            PlaceGlass(GlassPanel.ShapeType.Box, new Vector2(1.6f, 2.0f), true, "GlassWindow");
        }

        [MenuItem("Tools/ARK/玻璃/放置玻璃箱（场景视图中心）", false, 32)]
        public static void PlaceGlassBoxAtView()
        {
            PlaceGlass(GlassPanel.ShapeType.RoundedBox, new Vector2(0.8f, 1.0f), false, "GlassBox");
        }

        [MenuItem("Tools/ARK/玻璃/放置玻璃罐（场景视图中心）", false, 33)]
        public static void PlaceGlassJarAtView()
        {
            PlaceGlass(GlassPanel.ShapeType.Ellipse, new Vector2(0.8f, 1.1f), false, "GlassJar");
        }

        private static void PlaceGlass(GlassPanel.ShapeType shape, Vector2 size, bool withFrame, string defaultName)
        {
            var glassMat = AssetDatabase.LoadAssetAtPath<Material>(MatRoot + "/Glass_Panel.mat");
            if (glassMat == null)
            {
                Debug.LogWarning("[Glass] 缺少材质，先执行 Tools/ARK/玻璃/创建默认材质");
                GlassAssetGenerator.CreateMaterials();
                glassMat = AssetDatabase.LoadAssetAtPath<Material>(MatRoot + "/Glass_Panel.mat");
            }
            var frameMat = AssetDatabase.LoadAssetAtPath<Material>(MatRoot + "/Demo_Frame.mat");
            if (frameMat == null && withFrame)
            {
                var shader = Shader.Find("Legacy Shaders/Diffuse");
                if (shader == null) shader = Shader.Find("Standard");
                frameMat = new Material(shader) { name = "Demo_Frame" };
                if (frameMat.HasProperty("_Color")) frameMat.SetColor("_Color", new Color(0.2f, 0.21f, 0.24f, 1f));
                AssetDatabase.CreateAsset(frameMat, MatRoot + "/Demo_Frame.mat");
            }

            Vector3 pos = Vector3.zero;
            var view = SceneView.lastActiveSceneView;
            if (view != null) pos = view.pivot;
            pos.z = 0f;

            GameObject created;
            if (withFrame)
            {
                created = CreateGlassWindow(defaultName, pos, size, glassMat, frameMat, UnityEngine.Random.value * 6.28f);
                Undo.RegisterCreatedObjectUndo(created, "Create Glass Window");
            }
            else
            {
                var panel = CreateGlassObject(defaultName, pos, shape, size, glassMat, UnityEngine.Random.value * 6.28f);
                created = panel.gameObject;
                Undo.RegisterCreatedObjectUndo(created, "Create Glass Object");
            }

            Selection.activeGameObject = created;
            EditorSceneManager.MarkSceneDirty(created.scene);
            Debug.Log("[Glass] 已创建 " + created.name + " @ " + pos);
        }

        // ───────────────────────────── 构件 ─────────────────────────────

        public static GameObject CreateGlassWindow(string name, Vector3 worldPos, Vector2 size,
                                                   Material glassMat, Material frameMat, float bandOffset)
        {
            var root = new GameObject(name);
            root.transform.position = worldPos;
            root.tag = "Glass";

            var glassGo = new GameObject("Glass");
            glassGo.transform.SetParent(root.transform, false);
            glassGo.tag = "Glass";
            var panel = glassGo.AddComponent<GlassPanel>();
            panel.shape = GlassPanel.ShapeType.Box;
            panel.size = size;
            panel.thickness = 0.06f;
            panel.glassMaterial = glassMat;
            panel.bandOffset = bandOffset;
            panel.maxHealth = 100f;
            panel.crackLayers = 3;
            panel.explosionSpeed = 4.8f;
            panel.fragmentLife = 4.5f;
            AssignSharedAssets(panel);
            panel.Rebuild();

            // 金属框：上下左右四根
            if (frameMat != null)
            {
                float t = 0.09f;
                float depth = 0.16f;
                CreateBar(root.transform, "Frame_Top", new Vector3(0f, size.y * 0.5f + t * 0.5f, 0f),
                    new Vector3(size.x + t * 2f, t, depth), frameMat);
                CreateBar(root.transform, "Frame_Bottom", new Vector3(0f, -size.y * 0.5f - t * 0.5f, 0f),
                    new Vector3(size.x + t * 2f, t, depth), frameMat);
                CreateBar(root.transform, "Frame_Left", new Vector3(-size.x * 0.5f - t * 0.5f, 0f, 0f),
                    new Vector3(t, size.y, depth), frameMat);
                CreateBar(root.transform, "Frame_Right", new Vector3(size.x * 0.5f + t * 0.5f, 0f, 0f),
                    new Vector3(t, size.y, depth), frameMat);
            }

            return root;
        }

        public static GlassPanel CreateGlassObject(string name, Vector3 worldPos, GlassPanel.ShapeType shape,
                                                   Vector2 size, Material glassMat, float bandOffset)
        {
            var go = new GameObject(name);
            go.transform.position = worldPos;
            go.tag = "Glass";
            var panel = go.AddComponent<GlassPanel>();
            panel.shape = shape;
            panel.size = size;
            panel.thickness = 0.1f;
            panel.glassMaterial = glassMat;
            panel.bandOffset = bandOffset;
            panel.maxHealth = 60f;
            panel.crackLayers = 2;
            panel.explosionSpeed = 3.6f;
            panel.fragmentCount = 22;
            panel.size = size;
            AssignSharedAssets(panel);
            panel.Rebuild();
            return panel;
        }

        private static void AssignSharedAssets(GlassPanel panel)
        {
            var fragMat = AssetDatabase.LoadAssetAtPath<Material>(MatRoot + "/Glass_Fragment.mat");
            var crackMat = AssetDatabase.LoadAssetAtPath<Material>(MatRoot + "/Glass_Crack.mat");
            panel.fragmentMaterial = fragMat;
            panel.crackMaterial = crackMat;

            var cracks = new List<Texture2D>();
            for (int i = 1; i <= 4; i++)
            {
                var t = AssetDatabase.LoadAssetAtPath<Texture2D>(string.Format("{0}/Cracks/Crack_{1:00}.png", MatRoot, i));
                if (t != null) cracks.Add(t);
            }
            panel.crackTextures = cracks.ToArray();

#if UNITY_EDITOR
            panel.breakClip = AssetDatabase.LoadAssetAtPath<AudioClip>(MatRoot + "/Audio/Glass_Break.wav");
            panel.hitClip = AssetDatabase.LoadAssetAtPath<AudioClip>(MatRoot + "/Audio/Glass_Hit.wav");
#endif
        }

        private static void CreateBackdrop(string name, Vector3 pos, Vector3 scale, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.position = pos;
            go.transform.localScale = scale;
            go.transform.rotation = Quaternion.Euler(0f, -18f, 0f);
            var shader = Shader.Find("Legacy Shaders/Diffuse");
            if (shader == null) shader = Shader.Find("Standard");
            var mat = new Material(shader) { name = name + "_Mat" };
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            Object.DestroyImmediate(go.GetComponent<BoxCollider>());
        }

        private static void CreateBar(Transform parent, string name, Vector3 localPos, Vector3 scale, Material mat)
        {
            var bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bar.name = name;
            bar.transform.SetParent(parent, false);
            bar.transform.localPosition = localPos;
            bar.transform.localScale = scale;
            bar.GetComponent<MeshRenderer>().sharedMaterial = mat;
            Object.DestroyImmediate(bar.GetComponent<BoxCollider>());
        }

        private static Material GetOrCreateMaterial(string path, string shaderName, Color color)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                var shader = Shader.Find(shaderName);
                if (shader == null) shader = Shader.Find("Standard");
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
            EditorUtility.SetDirty(mat);
            return mat;
        }
    }
}
