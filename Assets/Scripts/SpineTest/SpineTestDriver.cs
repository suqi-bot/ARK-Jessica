using Spine.Unity;
using UnityEngine;

/// <summary>
/// Spine 测试场景的驱动脚本。只做最小功能：切状态、左右移动、翻转、跳跃。
/// 键位：
///   1 = Stand   2 = Move   3 = Run
///   Space = Jump（播完自动回到当前移动状态）
///   A / ←  左移     D / →  右移
///   R = 回到出生点
/// </summary>
public class SpineTestDriver : MonoBehaviour
{
    [Header("引用")]
    public Animator animator;
    public SkeletonAnimation skeletonAnimation;

    [Header("参数")]
    // 这两个值是照着动画步幅算出来的，保证脚不打滑：
    //   走：脚前后各 0.33，一个完整步 ≈ 0.66，1.3s 两步 → ≈1.0 单位/秒
    //   跑：脚前后各 0.45，0.75s 两步 → ≈2.4 单位/秒
    public float walkSpeed = 1.0f;
    public float runSpeed = 2.4f;
    public float jumpClipLength = 1.2f;

    enum Locomotion { Stand, Move, Run }
    Locomotion current = Locomotion.Stand;

    Vector3 spawnPos;
    float facing = 1f;
    bool jumping;
    float jumpTimer;

    void Awake()
    {
        spawnPos = transform.position;
        if (animator != null)
        {
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        }
    }

    void Start()
    {
        Play("Stand");
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) { current = Locomotion.Stand; Play("Stand"); }
        if (Input.GetKeyDown(KeyCode.Alpha2)) { current = Locomotion.Move; Play("Move"); }
        if (Input.GetKeyDown(KeyCode.Alpha3)) { current = Locomotion.Run; Play("Run"); }

        if (Input.GetKeyDown(KeyCode.Space) && !jumping)
        {
            jumping = true;
            jumpTimer = 0f;
            Play("Jump");
        }

        if (jumping)
        {
            jumpTimer += Time.deltaTime;
            if (jumpTimer >= jumpClipLength)
            {
                jumping = false;
                Play(current.ToString());
            }
        }

        float h = 0f;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) h -= 1f;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) h += 1f;

        if (Mathf.Abs(h) > 0.01f)
        {
            float speed = current == Locomotion.Run ? runSpeed : walkSpeed;
            transform.position += new Vector3(h * speed * Time.deltaTime, 0f, 0f);
            facing = Mathf.Sign(h);
            var s = transform.localScale;
            transform.localScale = new Vector3(Mathf.Abs(s.x) * facing, s.y, s.z);

            // 用键盘移动时自动切到对应状态，方便看动画
            if (!jumping)
            {
                var want = speed > walkSpeed + 0.01f ? Locomotion.Run : Locomotion.Move;
                if (want != current) { current = want; Play(current.ToString()); }
            }
        }
        else if (!jumping && current != Locomotion.Stand)
        {
            current = Locomotion.Stand;
            Play("Stand");
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            transform.position = spawnPos;
            current = Locomotion.Stand;
            jumping = false;
            Play("Stand");
        }
    }

    void Play(string stateName)
    {
        if (animator == null || animator.runtimeAnimatorController == null) return;
        animator.Play(stateName, 0, 0f);
    }

    void OnGUI()
    {
        GUI.color = Color.white;
        GUI.Label(new Rect(16, 12, 700, 22), "<b>Spine 测试场景</b>");
        GUI.Label(new Rect(16, 34, 700, 22),
            "1 = Stand    2 = Move    3 = Run    Space = Jump    A/D 或 ←/→ 移动    R = 复位");
        GUI.Label(new Rect(16, 56, 700, 22),
            "当前状态：" + (jumping ? "Jump" : current.ToString()) + "    朝向：" + (facing > 0 ? "右" : "左"));
    }
}
