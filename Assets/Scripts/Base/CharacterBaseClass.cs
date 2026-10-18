using System;
using UnityEngine;

public abstract class CharacterBaseClass : MonoBehaviour
{
    [Header("基础属性")]
    protected float maxHp;
    protected float moveSpeed;
    protected float shootCoolDown = 0.5f;
    [Header("组件引用")]
    [SerializeField] protected Rigidbody2D rb;
    [SerializeField] protected Collider2D col;
    public Animator animator;
    [SerializeField] protected SpriteRenderer spriteRenderer;
    [Header("状态")]
    [SerializeField] protected float currentHealth;
    [SerializeField] protected bool isDead = false;
    [SerializeField] protected Transform obstaclePoint;
    public bool isFacingRight = true;
    public GroundCheck groundCheck;
    [SerializeField] protected float obstacleCheckLine;
    public GameObject chatacterRenderer;
    public bool isInvincible = false;
    public bool isDodge = false;
    [Header("基础数值")]
    public float atk;
    public float atkSpeed;
    public event Action<float> OnHealthChanged;
    // public event Action OnDeath;


    protected virtual void Awake()
    {
        // 自动获取组件
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (col == null) col = GetComponent<Collider2D>();
        // if (animator == null) animator = GetComponent<Animator>();
        if (spriteRenderer == null) 
            spriteRenderer = GetComponent<SpriteRenderer>();
        if(animator == null)
            animator = chatacterRenderer.GetComponent<Animator>();
       
    }

    void Start()
    {
        Init();
        currentHealth = maxHp;
    }
    private void Update()
    {
        if (isDead) return;
        //HandleMovement();
        //HandleAttack();
        HandleUpdate();
    }

    // 角色初始化
    public abstract void Init();
    public abstract void HandleUpdate();
    //public abstract void HandleAttack();

    //public abstract void HandleMovement();

    //private void OnTriggerEnter2D(Collider2D collision)
    //{
    //    if (collision.CompareTag("Bullet") && !isInvincible)
    //    {
            
    //    }
    //}

    public void Injure(float hit)
    {
        OnHealthChanged?.Invoke(hit);
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public virtual void Die()
    {

    }

    /// <summary>
    /// 障碍射线监测
    /// </summary>
#if UNITY_EDITOR
    protected virtual void OnDrawGizmosSelected()
    {
        if (obstaclePoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(obstaclePoint.position, Vector3.right * obstacleCheckLine * (isFacingRight ? 1 : -1) + obstaclePoint.position);
        }
    }
#endif

    /// <summary>
    /// 角色翻转
    /// </summary>
    public void Flip()
    {
        isFacingRight = !isFacingRight;
        this.transform.rotation *= Quaternion.AngleAxis(180, Vector3.up);
    }

    public Rigidbody2D GetRigidbody()
    {
        return rb;
    }

    public bool GetHealthIsFull()
    {
        return currentHealth == maxHp;
    }
}
