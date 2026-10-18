#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using Spine.Unity;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 在 Jessica 的 Spine 骨架上烘焙 Move / Run / Jump 三段动画。
/// 原理：SkeletonUtilityBone 处于 Override 模式，把骨骼 GameObject 的 Transform 反向写回骨架，
/// 所以这里用 AnimationClip 直接 K 骨骼 Transform 的旋转和位移即可。
///
/// 坐标系约定（这套 rig 特有，实测得出）：
///   - 骨骼 GameObject 的 localPosition 与 Spine bone.X/Y 单位一致（positionScale = 1）
///   - localEulerAngles.z 与 bone.Rotation 一一对应
///   - 腿是 IK 驱动：IK 约束的 target 就是 Ik_F_L_Leg / Ik_F_L_Foot 这些骨骼本身，
///     它们挂在 F_Hip 下面，所以「抬脚」= 把 IK target 的 X 增大（X 正方向朝上）
///   - 身体下沉但脚要踩地 = F_Hip 减 X，同时 IK target 加同样的量
/// </summary>
public static class SpineLocomotionBaker
{
    const string PrefabPath = "Assets/Prefabs/Player/Jessica_Spine.prefab";
    const string SdaPath = "Assets/ArtRes/Jessica/Spine/char_235_jesica_SkeletonData.asset";
    const string ClipDir = "Assets/ArtRes/Jessica/Spine/Clips";
    const string RootPath = "SkeletonUtility-SkeletonRoot/";

    // ---------- 数据 ----------

    class BK
    {
        public string bone;
        public float rot;
        public float dx;
        public float dy;
        public BK(string bone, float rot, float dx = 0f, float dy = 0f)
        {
            this.bone = bone; this.rot = rot; this.dx = dx; this.dy = dy;
        }
    }

    class KF
    {
        public float t;
        public BK[] keys;
        public KF(float t, params BK[] keys) { this.t = t; this.keys = keys; }
    }

    // 腿 IK target 的静止位置（Idle 动画 t=0.6 处采样，单位=bones）
    const float LLegX = -1.3045f, LLegY = -0.5177f;
    const float LFootX = -1.5284f, LFootY = -0.5382f;
    const float RLegX = -1.2775f, RLegY = 1.1805f;
    const float RFootX = -1.5052f, RFootY = 1.1631f;

    static BK LLeg(float lift, float side = 0f) { return new BK("Ik_F_L_Leg", -90f, LLegX + lift, LLegY + side); }
    static BK LFoot(float lift, float side = 0f) { return new BK("Ik_F_L_Foot", -90f, LFootX + lift, LFootY + side); }
    static BK RLeg(float lift, float side = 0f) { return new BK("Ik_F_R_Leg", -90f, RLegX + lift, RLegY + side); }
    static BK RFoot(float lift, float side = 0f) { return new BK("Ik_F_R_Foot", -90f, RFootX + lift, RFootY + side); }

    // ---------- 入口 ----------

    [MenuItem("Tools/Spine/生成 Move·Run·Jump 动画", false, 1)]
    public static void BuildAll()
    {
        if (!Directory.Exists(ClipDir)) Directory.CreateDirectory(ClipDir);

        BuildClip("Jessica_Default", BuildDefault(), true);
        BuildClip("Jessica_Move", BuildMove(), true);
        BuildClip("Jessica_Run", BuildRun(), true);
        BuildClip("Jessica_Jump", BuildJump(), false);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[SpineLocomotionBaker] 已生成 Move / Run / Jump 三段动画到 " + ClipDir);
    }

    // ---------- Stand：静止基准姿势（用于 Stand 状态，把姿势锁死） ----------

    static KF[] BuildDefault()
    {
        var a = BaseKey(0.00f);
        var b = BaseKey(0.50f);
        return new KF[] { a, b };
    }

    // ---------- 走 / 跑：按参考视频（角色移动动画制作.mp4）重做 ----------
    //
    // 两个实测结论决定了坐标怎么用：
    //   1) IK target 的 localPosition.Y → 屏幕水平方向，也就是前后跨步
    //   2) IK target 的 localPosition.X → 屏幕垂直方向，越大越靠上，也就是抬脚
    // 参考视频 5.06s / 24fps，自相关分析出一个完整步幅 ≈ 31 帧 ≈ 1.29s；
    // 视频里两只手一直扶着枪没有摆动，所以手臂给固定值，只有腿和尾巴在动。

    // 基准姿势：全部取自 Idle 动画 t=0.6 处的采样值。
    // 注意不能用 setup pose（-150 / 142.1 那套）——那是没摆过的原始姿势，手臂是垂下来的。
    const float WaistBase = -8.49f, ChestBase = -9.69f, HeadBase = 6.48f;
    const float LArm = -82.03f, LForearm = 14.76f, LHand = 19.84f;
    const float RArm = 272.86f, RForearm = 19.96f, RHand = -1.14f;
    const float TailBase = 139.39f, Tail2Base = -25.00f, Tail3Base = -61.52f;
    const float BraidBase = 74.28f, Braid2Base = 60.06f;

    static KF[] BuildMove()
    {
        return BuildLocomotion(
            cycle: 1.30f, samples: 8,
            stride: 0.33f, lift: 0.34f, bob: 0.032f,
            lean: 0f, tailSway: 5f, braidSway: 2f);
    }

    static KF[] BuildRun()
    {
        return BuildLocomotion(
            cycle: 0.75f, samples: 8,
            stride: 0.45f, lift: 0.50f, bob: 0.06f,
            lean: -5f, tailSway: 10f, braidSway: 5f);
    }

    /// <summary>
    /// 程序化生成原地循环步态：
    ///   跨步 = sin(相位)，抬脚 = cos 正半周（前摆时抬起），
    ///   身体每步下沉一次，脚做反向补偿保证不陷进地面。
    /// </summary>
    static KF[] BuildLocomotion(float cycle, int samples, float stride, float lift, float bob,
                                float lean, float tailSway, float braidSway)
    {
        var frames = new KF[samples + 1];
        for (int i = 0; i <= samples; i++)
        {
            float p = (float)i / samples;
            float a = Mathf.PI * 2f * p;

            float sL = Mathf.Sin(a);
            float sR = Mathf.Sin(a + Mathf.PI);
            float liftL = Mathf.Max(0f, Mathf.Cos(a));
            float liftR = Mathf.Max(0f, Mathf.Cos(a + Mathf.PI));

            float bodyDown = -(0.5f + 0.5f * Mathf.Cos(2f * a)) * bob;
            float footComp = -bodyDown;
            float swing = Mathf.Sin(a);

            frames[i] = new KF(cycle * p,
                new BK("Ik_F_L_Leg", -90f, LLegX + liftL * lift + footComp, LLegY + sL * stride),
                new BK("Ik_F_L_Foot", -90f, LFootX + liftL * lift + footComp, LFootY + sL * stride),
                new BK("Ik_F_R_Leg", -90f, RLegX + liftR * lift + footComp, RLegY + sR * stride),
                new BK("Ik_F_R_Foot", -90f, RFootX + liftR * lift + footComp, RFootY + sR * stride),
                new BK("F_Hip", 0f, bodyDown, 0f),
                new BK("F_Waist", WaistBase + lean + swing * 1.2f),
                new BK("F_Chest", ChestBase + swing * 1.0f),
                new BK("F_Head", HeadBase - swing * 0.8f),
                new BK("F_L_Arm", LArm), new BK("F_L_Forearm", LForearm), new BK("F_L_Hand", LHand),
                new BK("F_R_Arm", RArm), new BK("F_R_Forearm", RForearm), new BK("F_R_Hand", RHand),
                new BK("F_Tail", TailBase + swing * tailSway),
                new BK("F_Tail_2", Tail2Base + swing * tailSway * 0.7f),
                new BK("F_Tail_3", Tail3Base + swing * tailSway * 0.5f),
                new BK("F_Braid", BraidBase + swing * braidSway),
                new BK("F_Braid_2", Braid2Base + swing * braidSway * 0.8f));
        }
        return frames;
    }

    // ---------- Jump：1.2s 单次（蹲→起→空中收腿→落地缓冲） ----------

    /// <summary>默认站姿的一组关键帧数据（Idle 基准姿势）。</summary>
    static KF BaseKey(float t)
    {
        return new KF(t,
            LLeg(0f), LFoot(0f), RLeg(0f), RFoot(0f),
            new BK("F_Hip", 0f, 0f, 0f),
            new BK("F_Waist", WaistBase), new BK("F_Chest", ChestBase), new BK("F_Head", HeadBase),
            new BK("F_L_Arm", LArm), new BK("F_L_Forearm", LForearm), new BK("F_L_Hand", LHand),
            new BK("F_R_Arm", RArm), new BK("F_R_Forearm", RForearm), new BK("F_R_Hand", RHand),
            new BK("F_Tail", TailBase), new BK("F_Tail_2", Tail2Base), new BK("F_Tail_3", Tail3Base),
            new BK("F_Braid", BraidBase), new BK("F_Braid_2", Braid2Base));
    }

    static KF[] BuildJump()
    {
        // 蹲下（hip 下沉 + 腿压缩）→ 蹬起 → 空中收腿 → 下落 → 落地缓冲 → 回站姿
        return new KF[]
        {
            BaseKey(0.00f),
            new KF(0.15f,
                LLeg(0.13f), LFoot(0.13f), RLeg(0.13f), RFoot(0.13f),
                new BK("F_Hip", 0f, -0.16f, 0f),
                new BK("F_Waist", WaistBase + 7f), new BK("F_Chest", ChestBase + 2f), new BK("F_Head", HeadBase + 3f),
                new BK("F_L_Arm", LArm), new BK("F_L_Forearm", LForearm), new BK("F_L_Hand", LHand),
                new BK("F_R_Arm", RArm), new BK("F_R_Forearm", RForearm), new BK("F_R_Hand", RHand),
                new BK("F_Tail", TailBase - 5f)),
            new KF(0.30f,
                LLeg(-0.03f), LFoot(-0.03f), RLeg(-0.03f), RFoot(-0.03f),
                new BK("F_Hip", 0f, 0.07f, 0f),
                new BK("F_Waist", WaistBase - 3f), new BK("F_Chest", ChestBase + 4f), new BK("F_Head", HeadBase - 4f),
                new BK("F_L_Arm", LArm - 6f), new BK("F_L_Forearm", LForearm), new BK("F_L_Hand", LHand),
                new BK("F_R_Arm", RArm + 6f), new BK("F_R_Forearm", RForearm), new BK("F_R_Hand", RHand),
                new BK("F_Tail", TailBase + 6f)),
            new KF(0.55f,
                LLeg(0.20f), LFoot(0.20f), RLeg(0.22f), RFoot(0.22f),
                new BK("F_Hip", 0f, 0.05f, 0f),
                new BK("F_Waist", WaistBase - 1f), new BK("F_Chest", ChestBase), new BK("F_Head", HeadBase - 2f),
                new BK("F_L_Arm", LArm - 4f), new BK("F_L_Forearm", LForearm), new BK("F_L_Hand", LHand),
                new BK("F_R_Arm", RArm + 4f), new BK("F_R_Forearm", RForearm), new BK("F_R_Hand", RHand),
                new BK("F_Tail", TailBase + 10f), new BK("F_Tail_2", Tail2Base + 6f)),
            new KF(0.80f,
                LLeg(0.04f), LFoot(0.04f), RLeg(0.04f), RFoot(0.04f),
                new BK("F_Hip", 0f, 0.01f, 0f),
                new BK("F_Waist", WaistBase + 1f), new BK("F_Chest", ChestBase), new BK("F_Head", HeadBase),
                new BK("F_L_Arm", LArm), new BK("F_L_Forearm", LForearm), new BK("F_L_Hand", LHand),
                new BK("F_R_Arm", RArm), new BK("F_R_Forearm", RForearm), new BK("F_R_Hand", RHand),
                new BK("F_Tail", TailBase - 4f)),
            new KF(0.95f,
                LLeg(0.15f), LFoot(0.15f), RLeg(0.15f), RFoot(0.15f),
                new BK("F_Hip", 0f, -0.14f, 0f),
                new BK("F_Waist", WaistBase + 6f), new BK("F_Chest", ChestBase + 2f), new BK("F_Head", HeadBase + 3f),
                new BK("F_L_Arm", LArm), new BK("F_L_Forearm", LForearm), new BK("F_L_Hand", LHand),
                new BK("F_R_Arm", RArm), new BK("F_R_Forearm", RForearm), new BK("F_R_Hand", RHand),
                new BK("F_Tail", TailBase - 6f)),
            BaseKey(1.20f),
        };
    }

    // ---------- 烘焙 ----------

    static Dictionary<string, string> BuildPaths()
    {
        var sda = AssetDatabase.LoadAssetAtPath<SkeletonDataAsset>(SdaPath);
        var data = sda.GetSkeletonData(false);
        var map = new Dictionary<string, string>();
        foreach (var b in data.Bones)
        {
            string p = b.Name;
            var cur = b;
            while (cur.Parent != null) { p = cur.Parent.Name + "/" + p; cur = cur.Parent; }
            map[b.Name] = RootPath + p;
        }
        return map;
    }

    static void BuildClip(string name, KF[] frames, bool loop)
    {
        var paths = BuildPaths();
        var clip = new AnimationClip { name = name, frameRate = 30f };

        // 收集每根骨骼的关键帧
        var boneTimes = new Dictionary<string, List<float>>();
        var boneRot = new Dictionary<string, List<float>>();
        var boneX = new Dictionary<string, List<float>>();
        var boneY = new Dictionary<string, List<float>>();
        var hasPos = new Dictionary<string, bool>();

        foreach (var f in frames)
        {
            foreach (var k in f.keys)
            {
                if (!paths.ContainsKey(k.bone))
                {
                    Debug.LogWarning("[SpineLocomotionBaker] 骨骼不存在: " + k.bone);
                    continue;
                }
                if (!boneTimes.ContainsKey(k.bone))
                {
                    boneTimes[k.bone] = new List<float>();
                    boneRot[k.bone] = new List<float>();
                    boneX[k.bone] = new List<float>();
                    boneY[k.bone] = new List<float>();
                    hasPos[k.bone] = false;
                }
                boneTimes[k.bone].Add(f.t);
                boneRot[k.bone].Add(k.rot);
                boneX[k.bone].Add(k.dx);
                boneY[k.bone].Add(k.dy);
                if (Mathf.Abs(k.dx) > 0.0001f || Mathf.Abs(k.dy) > 0.0001f) hasPos[k.bone] = true;
            }
        }

        foreach (var pair in boneTimes)
        {
            string bone = pair.Key;
            var ts = pair.Value;
            var rs = boneRot[bone];

            var rotCurve = new AnimationCurve();
            for (int i = 0; i < ts.Count; i++) rotCurve.AddKey(new Keyframe(ts[i], rs[i]));
            SmoothTangents(rotCurve);
            clip.SetCurve(paths[bone], typeof(Transform), "localEulerAnglesRaw.z", rotCurve);

            if (hasPos[bone])
            {
                var cx = new AnimationCurve();
                var cy = new AnimationCurve();
                for (int i = 0; i < ts.Count; i++)
                {
                    cx.AddKey(new Keyframe(ts[i], boneX[bone][i]));
                    cy.AddKey(new Keyframe(ts[i], boneY[bone][i]));
                }
                SmoothTangents(cx);
                SmoothTangents(cy);
                clip.SetCurve(paths[bone], typeof(Transform), "localPosition.x", cx);
                clip.SetCurve(paths[bone], typeof(Transform), "localPosition.y", cy);
            }
        }

        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        string path = ClipDir + "/" + name + ".anim";
        AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(clip, path);
    }

    /// <summary>用 Catmull-Rom 近似算切线，让循环动画首尾过渡自然；首尾关键帧切线置 0 避免越界。</summary>
    static void SmoothTangents(AnimationCurve curve)
    {
        var keys = curve.keys;
        int n = keys.Length;
        if (n < 2) return;
        for (int i = 0; i < n; i++)
        {
            float slope = 0f;
            if (i > 0 && i < n - 1)
            {
                float dt = keys[i + 1].time - keys[i - 1].time;
                if (dt > 0f) slope = (keys[i + 1].value - keys[i - 1].value) / dt;
            }
            keys[i].inTangent = slope;
            keys[i].outTangent = slope;
        }
        curve.keys = keys;
    }

    // ---------- 预览 ----------

    /// <summary>
    /// 应用某一时刻的姿势：采样曲线 → 手动跑一遍 Override 回写 → 更新骨架与网格。
    /// 注意：编辑模式下 SkeletonAnimation.Update 不会被调用，ISkeletonAnimation 的事件链不会触发，
    /// 所以这里必须手动 DoUpdate，否则 Transform 动了但骨骼不动。
    /// </summary>
    static void ApplyPose(GameObject go, SkeletonAnimation sa, AnimationClip clip, float t)
    {
        clip.SampleAnimation(go, t);
        var boneComps = go.GetComponentsInChildren<SkeletonUtilityBone>(true);
        for (int i = 0; i < boneComps.Length; i++)
        {
            boneComps[i].transformLerpComplete = false;
            boneComps[i].DoUpdate(SkeletonUtilityBone.UpdatePhase.Local);
        }
        sa.Skeleton.UpdateWorldTransform();
        sa.LateUpdate();
    }

    [MenuItem("Tools/Spine/渲染动画预览图", false, 2)]
    public static void RenderPreview()
    {
        var clipNames = new[] { "Jessica_Move", "Jessica_Run", "Jessica_Jump" };
        const int cols = 6;
        const int cw = 240, ch = 300;

        var sheet = new Texture2D(cols * cw, clipNames.Length * ch, TextureFormat.ARGB32, false);

        var scene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(
            UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,
            UnityEditor.SceneManagement.NewSceneMode.Additive);
        var hidden = new List<GameObject>();
        try
        {
            // 把其它场景的根对象临时藏起来，否则 Overlay Canvas 会盖住预览
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
            {
                var s = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
                if (s == scene) continue;
                foreach (var r in s.GetRootGameObjects())
                {
                    if (r.activeSelf) { hidden.Add(r); r.SetActive(false); }
                }
            }
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            var sa = go.GetComponent<SkeletonAnimation>();
            if (sa.Skeleton == null) sa.Initialize(true);
            sa.AnimationState.ClearTracks();

            var camGo = new GameObject("PreviewCam");
            UnityEditor.SceneManagement.EditorSceneManager.MoveGameObjectToScene(camGo, scene);
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.aspect = (float)cw / ch;
            cam.orthographicSize = 2.30f;
            cam.transform.position = new Vector3(-0.20f, 1.90f, -10f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.10f, 0.11f, 0.13f, 1f);

            var rt = new RenderTexture(cw, ch, 24, RenderTextureFormat.ARGB32);

            var clips = new AnimationClip[clipNames.Length];
            for (int i = 0; i < clipNames.Length; i++)
                clips[i] = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipDir + "/" + clipNames[i] + ".anim");

            // 第一遍：扫出所有姿势的并集包围盒，镜头按它自动取景
            var mr = go.GetComponent<MeshRenderer>();
            bool hasBounds = false;
            var union = new Bounds();
            for (int row = 0; row < clips.Length; row++)
            {
                if (clips[row] == null) continue;
                for (int c = 0; c < cols; c++)
                {
                    ApplyPose(go, sa, clips[row], clips[row].length * (c / (float)cols));
                    var bb = mr.bounds;
                    if (!hasBounds) { union = bb; hasBounds = true; }
                    else union.Encapsulate(bb);
                }
            }
            if (hasBounds)
            {
                cam.orthographicSize = Mathf.Max(union.extents.y, union.extents.x / cam.aspect) * 1.22f;
                cam.transform.position = new Vector3(union.center.x, union.center.y, -10f);
            }
            cam.targetTexture = rt;

            // 第二遍：逐帧渲染拼图
            for (int row = 0; row < clips.Length; row++)
            {
                if (clips[row] == null) continue;
                for (int c = 0; c < cols; c++)
                {
                    ApplyPose(go, sa, clips[row], clips[row].length * (c / (float)cols));
                    cam.Render();

                    RenderTexture.active = rt;
                    var tmp = new Texture2D(cw, ch, TextureFormat.ARGB32, false);
                    tmp.ReadPixels(new Rect(0, 0, cw, ch), 0, 0);
                    tmp.Apply();
                    RenderTexture.active = null;

                    int px = c * cw;
                    int py = (clipNames.Length - 1 - row) * ch;
                    sheet.SetPixels(px, py, cw, ch, tmp.GetPixels());
                    Object.DestroyImmediate(tmp);
                }
            }
            cam.targetTexture = null;
            sheet.Apply();
            File.WriteAllBytes("E:/unity/ARK-Jessica/_preview_locomotion.png", sheet.EncodeToPNG());
        }
        finally
        {
            for (int i = 0; i < hidden.Count; i++)
                if (hidden[i] != null) hidden[i].SetActive(true);
            UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene, true);
        }
        AssetDatabase.Refresh();
    }

    // ---------- 顺带生成 AnimatorController ----------

    [MenuItem("Tools/Spine/生成动画控制器", false, 3)]
    public static void BuildController()
    {
        string path = ClipDir + "/Jessica_Locomotion.controller";
        AssetDatabase.DeleteAsset(path);
        var controller = UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath(path);

        var stateMachine = controller.layers[0].stateMachine;
        var stand = stateMachine.AddState("Stand");
        stand.motion = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipDir + "/Jessica_Default.anim");
        var move = stateMachine.AddState("Move");
        move.motion = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipDir + "/Jessica_Move.anim");
        var run = stateMachine.AddState("Run");
        run.motion = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipDir + "/Jessica_Run.anim");
        var jump = stateMachine.AddState("Jump");
        jump.motion = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipDir + "/Jessica_Jump.anim");

        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("Jump", AnimatorControllerParameterType.Trigger);

        stateMachine.defaultState = stand;
        AssetDatabase.SaveAssets();
        Debug.Log("[SpineLocomotionBaker] 已生成 " + path + "（Stand/Move/Run/Jump 四个状态，Speed 浮点 + Jump 触发器）");
    }
}
#endif
