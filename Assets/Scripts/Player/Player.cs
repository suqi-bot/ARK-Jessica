using System.Collections.Generic;
using UnityEngine;


public class Player : CharacterBaseClass
{
    [Header("玩家属性")]
    public float jumpForce = 10f;
    public float deshSpeed = 10;
    public float runSpeed = 10;
    private float moveTime = .25f;
    public LayerMask groundLayer;
    public LayerMask obstacleLayer;
    [SerializeField] public Player_State_Type nowState; // 当前状态
    [Header("射击")]
    //public Dictionary<Weapon_Type, List<Transform>> firePos;\
    // 射击点位
    public List<Transform> firePos;
    // 武器
    public Weapon_Base weapon;
    // 射击事件
    public PlayerShootEvent shootEvent;
    // 
    public float projectileSpeed = 20f;
    // 攻击连段Cd
    public float fireNumCd;
    // 攻击盒子
    public AttackBox kickBox;
    [Header("技能")]
    public Skill_Type skill;
    public Skill_Type kick_skill;
    public Skill_Type uniqueSkill;
    [Header("输入设置")]
    public string horizontalAxis = "Horizontal";
    public string verticalAxis = "Vertical";
    public string jumpButton = "Jump";
    public string fireButton = "Fire1";
    public string interactiveButton = "Interactive";
    public string kickButton = "Kick";
    public string skillButton = "Skill";
    public string uniqueSkillButton = "Unique Skill";
    public string runButton = "Run";
    public string reLoadButton = "ReLoad";
    public string BagButton = "Bag";

    public RaycastHit2D raycastHit2D;
    public bool isGrounded;

    /// <summary>
    /// 初始化
    /// </summary>
    public override void Init()
    {
        // 初始化武器
        weapon = Resources.Load<Weapon_Base>("Weapon/Weapon_M9");
        // 设置默认武器
        weapon.Init(this.gameObject, firePos);
        UIManager.Instance.ShowPanel<PlayerHpPanel>().SetOwner(this.gameObject);
        UIManager.Instance.ShowPanel<GameScenePanel>();
        //UIManager.Instance.ShowPanel<GameScenePanel>();
        #region 初始化面板相关
        // 受伤代码
        OnHealthChanged += (float hit) =>
        {
            currentHealth -= hit;
            if (hit > 0)
            {
                PlayerStateMgr.Instance.getFsm().SwitchState(Player_State_Type.Injured);
                Debug.Log(hit);
            }
            else if(hit < 0)
            {
               currentHealth = currentHealth - hit > maxHp ? maxHp : currentHealth - hit;
            }
            UIManager.Instance.GetPanel<PlayerHpPanel>().UpdateHp(maxHp, currentHealth);
        };
        maxHp = 200;
        currentHealth = maxHp;
        moveSpeed = 5f;
        // 初始化血量
        UIManager.Instance.GetPanel<PlayerHpPanel>().UpdateHp(maxHp, currentHealth);
        // 初始化弹药
        UIManager.Instance.GetPanel<PlayerHpPanel>().UpdateAmmo(weapon.maxClipAmmoNum, weapon.nowClipAmmoNum);
        // 初始化弹药线
        UIManager.Instance.GetPanel<PlayerHpPanel>().DrawAmmoLine(weapon.maxClipAmmoNum);
        #endregion
        
        
    }

    public override void HandleUpdate()
    {
        // 检查是否在地面
        isGrounded = groundCheck.isGrounded;
        // 检查前方是否有障碍
        raycastHit2D = Physics2D.Raycast(obstaclePoint.position, Vector3.right * (isFacingRight ? 1: -1), obstacleCheckLine, obstacleLayer);
        // 攻击连携cd
        fireNumCd -= Time.deltaTime;
        // 技能冷却时间
        SkillMgr.Instance.UpdateSkillTime();
        OpenBag();
        OpenTask();
        // ChangeWeapon();
    }

    /// <summary>
    /// 死亡代码
    /// </summary>
    public override void Die()
    {
        base.Die();
        isInvincible = true;
        PlayerStateMgr.Instance.getFsm().SwitchState(Player_State_Type.Die);
        UIManager.Instance.ShowPanel<GameOverPanel>();
        UIManager.Instance.HidePanel<PlayerHpPanel>();
    }


    /// <summary>
    /// 射击
    /// </summary>
    public void Attack()
    {
        fireNumCd = .5f;
        if (weapon.isContinuous)
        {
            if (weapon.nowClipAmmoNum != 0)
            {
                weapon.Fire();
                UIManager.Instance.GetPanel<PlayerHpPanel>().UpdateAmmo(weapon.maxClipAmmoNum, weapon.nowClipAmmoNum);
            }
        }
        else
        {
            if (weapon.nowClipAmmoNum != 0)
            {
                weapon.Fire();
                UIManager.Instance.GetPanel<PlayerHpPanel>().UpdateAmmo(weapon.maxClipAmmoNum, weapon.nowClipAmmoNum);
            }
        }

        
    }
   

    /// <summary>
    /// 水平移动
    /// </summary>
    public void XMove()
    {
        rb.velocity = new Vector2(moveSpeed * Input.GetAxis(horizontalAxis), rb.velocity.y);

        if (!isFacingRight && Input.GetAxis(horizontalAxis) > 0)
            Flip();
        if (isFacingRight && Input.GetAxis(horizontalAxis) < 0)
            Flip();

    }
    /// <summary>
    /// 换弹移动
    /// </summary>
    public void ReLoadMove()
    {
        rb.velocity = new Vector2(moveSpeed * Input.GetAxis(horizontalAxis) * 0.6f, rb.velocity.y);

        if (!isFacingRight && Input.GetAxis(horizontalAxis) > 0)
            Flip();
        if (isFacingRight && Input.GetAxis(horizontalAxis) < 0)
            Flip();
    }

    /// <summary>
    /// 跳跃
    /// </summary>
    public void Jump()
    {
       rb.AddForce(Vector2.up * jumpForce * 50); 
    }

    
    /// <summary>
    /// 测算障碍距离
    /// </summary>
    Vector2 goalPoint;
    Vector2 frontPoint;
    float obTime;
    
    public void CheckJumpOb()
    {
        frontPoint = this.transform.position;
        // 计算翻越过去的点位
        goalPoint = new Vector2(raycastHit2D.transform.position.x + 
            (raycastHit2D.collider.bounds.size.x / 2 + this.GetComponent<Collider2D>().bounds.size.x )
            * (isFacingRight ? 1: -1), this.transform.position.y);
        
    }

    /// <summary>
    /// 越过障碍
    /// </summary>
    /// <returns></returns>
    public bool DoJumpOb()
    {
        obTime += Time.deltaTime;
        this.transform.position = Vector2.Lerp(frontPoint, goalPoint, obTime);
        
        if(this.transform.position.x == goalPoint.x)
        {
            obTime = 0;
            return true;
        }
        return false;
    }
   
    /// <summary>
    /// 射击位移
    /// </summary>
    /// <param name="dic">位移距离</param>
    /// <returns></returns>
    public bool DoAttackMove(float dic)
    {
        moveTime -= Time.deltaTime;
        if (moveTime >= 0)
        {
            rb.velocity = new Vector2(dic * moveTime * (isFacingRight ? 1 : -1), rb.velocity.y);
            moveTime = .25f;
            return true;
        }
        return false;
    }

    /// <summary>
    /// 跑步
    /// </summary>
    public void DoRun()
    {
        rb.velocity = new Vector2(runSpeed * Input.GetAxis(horizontalAxis), rb.velocity.y);

        if (!isFacingRight && Input.GetAxis(horizontalAxis) > 0)
            Flip();
        if (isFacingRight && Input.GetAxis(horizontalAxis) < 0)
            Flip();
    }

    /// <summary>
    /// 换弹
    /// </summary>
    /// <returns></returns>
    public void ReLoad()
    {
        weapon.ReLoad();
        UIManager.Instance.GetPanel<PlayerHpPanel>().UpdateAmmo(weapon.maxClipAmmoNum, weapon.nowClipAmmoNum);
    }
    /// <summary>
    /// 打开背包
    /// </summary>
    public void OpenBag()
    {
        if (Input.GetButtonDown(BagButton))
        {
            UIManager.Instance.ShowPanel<BagPanel>();
        }
    }
    /// <summary>
    /// 打开任务面板
    /// </summary>
    public void OpenTask()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
            {
                UIManager.Instance.ShowPanel<TaskPanel>();
        }
    }

    /// <summary>
    /// 改变装备
    /// </summary>
    public void ChangeWeapon()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            // 初始化武器
            weapon = Resources.Load<Weapon_Base>("Weapon/Weapon_Glock_34");
            // 设置默认武器
            weapon.Init(this.gameObject, firePos);
            // 初始化弹药
            UIManager.Instance.GetPanel<PlayerHpPanel>().UpdateAmmo(weapon.maxClipAmmoNum, weapon.nowClipAmmoNum);
            // 初始化弹药线
            UIManager.Instance.GetPanel<PlayerHpPanel>().DrawAmmoLine(weapon.maxClipAmmoNum);
            // 更换图片
            chatacterRenderer.GetComponent<SpriteRenderer>().sprite = Resources.Load<SpriteRenderer>("Player/Jessica_G").sprite;
            chatacterRenderer.transform.localScale = Vector3.one * 2.3f;
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            // 初始化武器
            weapon = Resources.Load<Weapon_Base>("Weapon/Weapon_TTI_MPX");
            // 设置武器发射点
            //
            // 设置默认武器
            weapon.Init(this.gameObject, firePos);
            // 初始化弹药
            UIManager.Instance.GetPanel<PlayerHpPanel>().UpdateAmmo(weapon.maxClipAmmoNum, weapon.nowClipAmmoNum);
            // 初始化弹药线
            UIManager.Instance.GetPanel<PlayerHpPanel>().DrawAmmoLine(weapon.maxClipAmmoNum);
            // 更换图片
            chatacterRenderer.GetComponent<SpriteRenderer>().sprite = Resources.Load<SpriteRenderer>("Player/Jessica_MPX").sprite;
            chatacterRenderer.transform.localScale = Vector3.one * 1.24f;
        }


    }


    
   
}
