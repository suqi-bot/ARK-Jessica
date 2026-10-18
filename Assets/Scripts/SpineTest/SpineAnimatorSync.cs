using Spine.Unity;
using UnityEngine;

/// <summary>
/// 让 Animator 驱动的骨骼 Transform 在同一帧内写回 Spine 骨架。
///
/// 为什么需要它：
/// SkeletonAnimation 在 Update() 里会触发 UpdateLocal 事件做 Override 回写，
/// 而 Unity 的 Animator 求值发生在 Update() 之后、LateUpdate() 之前，
/// 所以默认链路会读到"上一帧"的 Transform，表现为动作慢半帧。
/// 这里用 -1000 的执行顺序在 LateUpdate 里再写回一次，保证当帧就生效，
/// 并且早于 SkeletonRenderer.LateUpdate（顺序 0）生成网格。
/// </summary>
[DefaultExecutionOrder(-1000)]
[RequireComponent(typeof(SkeletonAnimation))]
public class SpineAnimatorSync : MonoBehaviour
{
    SkeletonAnimation skeletonAnimation;
    SkeletonUtilityBone[] boneComponents;

    void Awake()
    {
        skeletonAnimation = GetComponent<SkeletonAnimation>();
        boneComponents = GetComponentsInChildren<SkeletonUtilityBone>(true);
    }

    void Start()
    {
        // 骨架实例化后 bone 引用是空的（[NonSerialized]），先让 SkeletonUtility 收集一遍
        var util = GetComponent<SkeletonUtility>();
        if (util != null) util.CollectBones();
    }

    void LateUpdate()
    {
        if (skeletonAnimation == null || skeletonAnimation.Skeleton == null)
            return;

        for (int i = 0; i < boneComponents.Length; i++)
        {
            var b = boneComponents[i];
            if (b == null || !b.isActiveAndEnabled)
                continue;
            // 注意：这里不能因为 b.bone == null 就跳过，
            // DoUpdate 内部会用 boneName 去 skeleton 里找骨骼，跳过就永远找不到
            b.transformLerpComplete = false;
            b.DoUpdate(SkeletonUtilityBone.UpdatePhase.Local);
        }

        skeletonAnimation.Skeleton.UpdateWorldTransform();
    }
}
