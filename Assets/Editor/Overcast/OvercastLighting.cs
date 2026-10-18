using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace ARK.EditorTools
{
    /// <summary>
    /// 一键把当前场景调成「阴天」。
    ///
    /// 阴天不是把灯调暗，而是四件事一起做：
    ///   1. 天空：灰白的厚云层，不能有太阳光斑；
    ///   2. 光照：几乎没有方向性 —— 亮度主要来自环境光（天空是一个大面光源），主光很弱；
    ///   3. 色调：整体偏冷、偏灰，饱和度低；
    ///   4. 空气透视：加雾，远处发灰，画面才有纵深。
    ///
    /// 之前的场景黑，就是因为环境光模式是 Skybox，而天空盒材质 FS003_Rainy 的 _Tint 只有 0.5，
    /// 等于把整片天空压暗一半 —— 环境光采样它，自然全黑。
    /// </summary>
    public static class OvercastLighting
    {
        const string SourceSkyMat = "Assets/Fantasy Skybox FREE/Panoramics/FS003/FS003_Rainy.mat";
        const string SkyFolder = "Assets/ArtRes/Sky";
        const string OvercastSkyMat = SkyFolder + "/ARK_OvercastSky.mat";

        [MenuItem("Tools/ARK/场景光照/应用阴天 (Overcast)", false, 10)]
        public static void ApplyToActiveScene()
        {
            var scene = SceneManager.GetActiveScene();
            ApplyToScene(scene);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[ARK] 阴天光照已应用并保存：" + scene.name);
        }

        /// <summary>把阴天配置套到指定场景（当前打开的场景）。</summary>
        public static void ApplyToScene(Scene scene)
        {
            var sky = EnsureSkyMaterial();
            if (sky != null) RenderSettings.skybox = sky;

            // 环境光：三色模式才有意义（Skybox 模式会用天空盒球谐，天空一暗全场景跟着黑）
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.40f, 0.45f, 0.52f);
            RenderSettings.ambientEquatorColor = new Color(0.31f, 0.34f, 0.39f);
            RenderSettings.ambientGroundColor = new Color(0.18f, 0.18f, 0.19f);
            RenderSettings.ambientIntensity = 1f;

            // 雾：阴天的空气透视
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.006f;
            RenderSettings.fogColor = new Color(0.62f, 0.66f, 0.71f);

            // 主光：冷白、弱直射、无硬影
            var lights = Resources.FindObjectsOfTypeAll<Light>();
            for (int i = 0; i < lights.Length; i++)
            {
                var light = lights[i];
                if (light == null || !light.gameObject.scene.IsValid()) continue;
                if (light.type != LightType.Directional) continue;

                if (light.name == "SceneLight" && light.color.grayscale > 0.5f)
                {
                    light.color = new Color(0.86f, 0.90f, 0.96f);
                    light.intensity = 1.15f;
                    light.shadows = LightShadows.None;
                    EditorUtility.SetDirty(light);
                }
                else if (light.name == "UniStorm Sun")
                {
                    light.shadowStrength = 0.35f;   // 阴天的影子非常淡
                    light.shadows = LightShadows.Soft;
                    EditorUtility.SetDirty(light);
                }
            }

            ApplyUniStormWeather();
            EditorSceneManager.MarkSceneDirty(scene);
        }

        /// <summary>UniStorm：切到 Cloudy（无降水 → 用非 stormy 的天空/环境光渐变）、时间调到正午。</summary>
        static void ApplyUniStormWeather()
        {
            var systems = Resources.FindObjectsOfTypeAll<UniStormSystem>();
            for (int i = 0; i < systems.Length; i++)
            {
                var sys = systems[i];
                if (sys == null || !sys.gameObject.scene.IsValid()) continue;

                if (sys.AllWeatherTypes != null)
                {
                    for (int w = 0; w < sys.AllWeatherTypes.Count; w++)
                    {
                        var type = sys.AllWeatherTypes[w];
                        // 注意：Overcast 在 UniStorm 里被标成了“有降水”，会切到更暗的 stormy 渐变；
                        // 想要明亮的阴天，用 Cloudy（云量 Cloudy、无降水）更合适。
                        if (type != null && type.WeatherTypeName == "Cloudy")
                        {
                            sys.CurrentWeatherType = type;
                            break;
                        }
                    }
                }

                sys.Hour = 12;
                sys.StartingHour = 12;
                EditorUtility.SetDirty(sys);
            }
        }

        /// <summary>
        /// 天空盒：复制一份插件材质（不动插件资源），换成阴雨全景并修掉 _Tint 0.5 的压暗。
        /// </summary>
        static Material EnsureSkyMaterial()
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(OvercastSkyMat);
            if (mat == null)
            {
                if (!AssetDatabase.IsValidFolder(SkyFolder))
                    AssetDatabase.CreateFolder("Assets/ArtRes", "Sky");
                if (!AssetDatabase.CopyAsset(SourceSkyMat, OvercastSkyMat))
                {
                    Debug.LogWarning("[ARK] 找不到天空盒源材质：" + SourceSkyMat);
                    return null;
                }
                mat = AssetDatabase.LoadAssetAtPath<Material>(OvercastSkyMat);
            }

            if (mat == null) return null;

            if (mat.HasProperty("_Tint")) mat.SetColor("_Tint", new Color(1f, 1f, 1f, 1f));
            if (mat.HasProperty("_Exposure")) mat.SetFloat("_Exposure", 1.2f);
            mat.EnableKeyword("_MAPPING_LATITUDE_LONGITUDE_LAYOUT");
            EditorUtility.SetDirty(mat);
            return mat;
        }
    }
}
