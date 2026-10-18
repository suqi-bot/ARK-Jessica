using UnityEngine;

/// <summary>
/// 序列帧版 Jessica 的测试驱动。使用 Player_anim.controller 里现成的状态。
/// 键位：1 = 待机   2 = 走   3 = 跑   Space = 跳   A/D 或 ←/→ 移动   R = 复位
/// </summary>
public class FrameTestDriver : MonoBehaviour
{
    [Header("引用")]
    public Animator animator;

    [Header("参数")]
    public float walkSpeed = 2.5f;
    public float runSpeed = 6f;

    enum Locomotion { Idle, Move, Run }
    Locomotion current = Locomotion.Idle;

    Vector3 spawnPos;
    float facing = 1f;

    void Awake()
    {
        spawnPos = transform.position;
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (animator != null)
        {
            // 逐帧动画对时间很敏感，保证动画器一直在算
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.applyRootMotion = false;
        }
    }

    void Start()
    {
        Play("Player_Idle");
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) { current = Locomotion.Idle; Play("Player_Idle"); }
        if (Input.GetKeyDown(KeyCode.Alpha2)) { current = Locomotion.Move; Play("Player_Move"); }
        if (Input.GetKeyDown(KeyCode.Alpha3)) { current = Locomotion.Run; Play("Player_Run"); }
        if (Input.GetKeyDown(KeyCode.Space)) Play("Player_Jump");

        float h = 0f;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) h -= 1f;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) h += 1f;

        if (Mathf.Abs(h) > 0.01f)
        {
            float speed = current == Locomotion.Run ? runSpeed : walkSpeed;
            transform.position += new Vector3(h * speed * Time.deltaTime, 0f, 0f);

            facing = Mathf.Sign(h);
            var s = transform.localScale;
            if (Mathf.Sign(s.x) != facing)
                transform.localScale = new Vector3(Mathf.Abs(s.x) * facing, s.y, s.z);

            var want = speed > walkSpeed + 0.01f ? Locomotion.Run : Locomotion.Move;
            if (want != current) { current = want; Play(current == Locomotion.Run ? "Player_Run" : "Player_Move"); }
        }
        else if (current != Locomotion.Idle && !Input.GetKey(KeyCode.Space))
        {
            current = Locomotion.Idle;
            Play("Player_Idle");
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            transform.position = spawnPos;
            current = Locomotion.Idle;
            Play("Player_Idle");
        }
    }

    void Play(string state)
    {
        if (animator == null || animator.runtimeAnimatorController == null) return;
        animator.Play(state, 0, 0f);
    }

    void OnGUI()
    {
        GUI.color = Color.white;
        GUI.Label(new Rect(16, 12, 700, 22), "<b>序列帧测试场景</b>");
        GUI.Label(new Rect(16, 34, 700, 22),
            "1 = 待机    2 = 走    3 = 跑    Space = 跳    A/D 或 ←/→ 移动    R = 复位");
        GUI.Label(new Rect(16, 56, 700, 22), "当前状态：" + current);
    }
}
