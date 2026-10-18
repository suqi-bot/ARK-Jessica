using System;
using System.IO;
using ARK.Glass;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ARK.Glass.EditorTools
{
    /// <summary>
    /// 自动验收流程（仅开发期）：检测到 flag 文件后，自动打开演示场景、进入播放，
    /// 由 GlassCaptureAgent 截图并退出播放，全程写日志。
    ///
    /// 两种触发方式：
    ///   1. 编辑模式下轮询 .workbuddy/tmp/verify.flag（改代码后放个 flag 即可）
    ///   2. 命令行：Unity.exe -projectPath ... -executeMethod ARK.Glass.EditorTools.GlassAutoVerify.RunFromCommandLine
    /// </summary>
    [InitializeOnLoad]
    public static class GlassAutoVerify
    {
        const string Root = "E:/unity/ARK-Jessica/.workbuddy/tmp";
        const string FlagPath = Root + "/verify.flag";
        const string LogPath = Root + "/verify.log";
        const string DiagPath = Root + "/verify_diag.txt";
        const string ScenePath = "Assets/Scenes/GlassTestScene.unity";

        static bool running;
        static bool refreshedOnce;
        static double lastPollTime;

        static GlassAutoVerify()
        {
            EditorApplication.update += Poll;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        static void Log(string msg)
        {
            try
            {
                Directory.CreateDirectory(Root);
                File.AppendAllText(LogPath, DateTime.Now.ToString("HH:mm:ss.fff") + "  " + msg + "\n");
            }
            catch { }
        }

        /// <summary>命令行入口。Unity 启动时会先编译再执行这里。</summary>
        public static void RunFromCommandLine()
        {
            EditorApplication.delayCall += () => Begin("commandline");
        }

        static void Poll()
        {
            double now = EditorApplication.timeSinceStartup;
            if (now - lastPollTime < 0.5) return;
            lastPollTime = now;

            if (EditorApplication.isPlaying || EditorApplication.isCompiling) return;
            if (!File.Exists(FlagPath)) return;

            // 项目关闭了自动刷新，这里手动刷新一次，确保跑的是最新脚本。
            // 如果刷新触发了编译，会 domain reload，refreshedOnce 复位后下一轮再执行。
            if (!refreshedOnce)
            {
                refreshedOnce = true;
                Log("refresh assets");
                AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
                return;
            }

            Begin("flag");
        }

        static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                Log("entered edit mode");
                if (running)
                {
                    Log("verify finished");
                    running = false;
                }
            }
        }

        static void Begin(string source)
        {
            if (running)
            {
                Log("already running, skip (" + source + ")");
                return;
            }
            running = true;
            try
            {
                string flagContent = "";
                try
                {
                    if (File.Exists(FlagPath)) flagContent = File.ReadAllText(FlagPath).Trim();
                    File.Delete(FlagPath);
                }
                catch { }
                File.WriteAllText(LogPath, "");
                Log("begin via " + source + " flag=" + flagContent);

                if (EditorApplication.isPlaying)
                {
                    Log("stop existing play mode");
                    EditorApplication.isPlaying = false;
                    return;
                }

                if (flagContent.Contains("rebuild"))
                {
                    Log("rebuild materials + demo scene");
                    GlassAssetGenerator.CreateMaterials();
                    GlassDemoBuilder.BuildDemoSceneSilent();
                }

                var scene = EditorSceneManager.GetActiveScene();
                if (scene.path != ScenePath)
                {
                    Log("open scene");
                    scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                }

                var panels = UnityEngine.Object.FindObjectsOfType<GlassPanel>(true);
                Log("glass panels=" + panels.Length);
                if (panels.Length == 0)
                {
                    Log("ERROR no glass panels, abort");
                    running = false;
                    return;
                }

                var agent = UnityEngine.Object.FindObjectOfType<GlassCaptureAgent>();
                if (agent == null)
                {
                    var go = new GameObject("GlassCaptureAgent");
                    agent = go.AddComponent<GlassCaptureAgent>();
                    Log("capture agent added");
                }
                agent.stopPlayModeWhenDone = true;
                agent.diagPath = DiagPath;

                EditorApplication.isPlaying = true;
                Log("play started");
            }
            catch (Exception e)
            {
                Log("ERROR " + e);
                running = false;
            }
        }
    }
}
