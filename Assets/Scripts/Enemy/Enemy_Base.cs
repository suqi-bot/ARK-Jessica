using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;



public abstract class Enemy_Base : CharacterBaseClass
{

    [SerializeField] protected Transform flipCheck;
    [SerializeField] protected Transform enemyCheck;
    [SerializeField] protected float flipCheckLine;
    [SerializeField] protected float enemyCheckLine;
    [SerializeField] protected float attackCheckLine;
    [SerializeField] protected Transform enemyPos;
    [SerializeField] public float chaseDuration;
    public RaycastHit2D obRaycastHit2D;
    public RaycastHit2D groundRaycastHit2D;
    public RaycastHit2D chaseRaycastHit2D;
    public RaycastHit2D attackRaycastHit2D;

    [SerializeField] protected LayerMask obstacleLayer;
    [SerializeField] protected LayerMask groundLayer;
    [SerializeField] protected LayerMask enemyLayer;

    [Header("基础属性")]
    public float attackCd;// 攻击间隔
    public EnemyPanel panel;// 获取血量面板
    [SerializeField] protected GameObject[] bullets;
    [SerializeField] protected Transform[] firePos;
    [SerializeField] protected List<Vector2> patrolPoint;
    [SerializeField] protected AttackBox attackBox;
    [SerializeField] protected EnemyEvent enemyEvent;
    [SerializeField] public Enemy_State_Type nowState;
    [SerializeField] protected EnemyStateMgr fsmMgr;
    // 获取对象池
    public ObjectPool<GameObject> enemypool;
    private int patrolIndex;
    public bool isTarget;
    public override void HandleUpdate()
    {
        // 检查前方是否有障碍
        obRaycastHit2D = Physics2D.Raycast(obstaclePoint.position, Vector3.right * (isFacingRight ? 1 : -1), obstacleCheckLine, obstacleLayer);
        groundRaycastHit2D = Physics2D.Raycast(flipCheck.position, Vector3.down, flipCheckLine, groundLayer);
        chaseRaycastHit2D = Physics2D.Raycast(enemyCheck.position, Vector3.right * (isFacingRight ? 1 : -1), enemyCheckLine, enemyLayer);
        attackRaycastHit2D = Physics2D.Raycast(enemyCheck.position, Vector3.right * (isFacingRight ? 1 : -1), attackCheckLine, enemyLayer);

        if (!groundRaycastHit2D)
        {
            Flip();
        }
    }

    public override void Init()
    {
        BasePool.Instance.SetItem(this.gameObject, this.transform);
        enemypool = BasePool.Instance.GetPool(this.gameObject.name);
        fsmMgr = this.GetComponent<EnemyStateMgr>();

        panel.ShowSelf();
        if (patrolPoint.Count == 0)
        {
            patrolPoint.Add(new Vector3 (this.transform.position.x - 10, 0, 0));
            patrolPoint.Add(new Vector3 (this.transform.position.x + 10, 0, 0));
        }

        if(bullets != null)
        {
            foreach (var item in bullets)
            {
                item.GetComponent<Bullet>().owner = this.gameObject;
            }
        }


        OnHealthChanged += (float hit) =>
        {
            currentHealth -= hit;
            panel.UpdateHp(maxHp, currentHealth);
            if (currentHealth <= 0)
            {
                currentHealth = 0;
                Die();
            }
        };
        EnemyInit();
        
    }

    private void OnEnable()
    {
        
        panel.ShowSelf();
    }

    public abstract void EnemyInit();

    public Vector2 ChasePos()
    {
        Vector2 otherPos = enemyPos.transform.position;
        FlipTarget(otherPos);

        return otherPos;
    }


    public override void Die()
    {
        fsmMgr.GetFsm().SwitchState(Enemy_State_Type.Die);
    }

    public void DieRelease()
    {
    }

    public void Idle()
    {
        rb.velocity = new Vector2(0 ,rb.velocity.y);
    }
    
    public void Move(bool far)
    {
        rb.velocity = new Vector2(moveSpeed * (isFacingRight? 1:-1) * (far? 1: -1), rb.velocity.y);
    }

    public virtual void PlayHitSource()
    {

    }

    public virtual void AttackHatred(Transform hatrePos)
    {
        enemyPos = hatrePos;
    }
    public void RandomPatrol()
    {
        patrolIndex = Random.Range(0, patrolPoint.Count);
    }

    public bool UpdatePatrol()
    {
        FlipTarget(patrolPoint[patrolIndex]);
        if (Mathf.Abs(this.transform.position.x - patrolPoint[patrolIndex].x) < 1)
        {
            return true;
        }
        return false;
    }

    public void ChaseTarget()
    {
        if (chaseRaycastHit2D)
        {
            this.enemyPos = chaseRaycastHit2D.collider.transform;
        }
    }


    public abstract void Jump();

    public abstract bool Attack();

    public abstract bool AttackOver();

    public abstract int Skill();

    public abstract bool RecoverHp();
    public void FlipTarget(Vector2 target)
    {
        if (target.x > this.transform.position.x && isFacingRight == false)
        {
            Flip();
        }
        else if (target.x < this.transform.position.x && isFacingRight == true)
        {
            Flip();
        }
    }
#if UNITY_EDITOR
    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();
        if (flipCheck != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(flipCheck.position, Vector3.down * flipCheckLine + flipCheck.position);
        }
        if(enemyCheck != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(enemyCheck.position, Vector3.right * (isFacingRight ? 1 : -1) * enemyCheckLine + enemyCheck.position);
            Gizmos.color = Color.green;
            Gizmos.DrawLine(enemyCheck.position, Vector3.right * (isFacingRight ? 1 : -1) * attackCheckLine + enemyCheck.position);
        }
    }
#endif
    protected void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Bullet") && enemyPos == null)
        {
            this.enemyPos = collision.GetComponent<Bullet>().owner.transform;
            fsmMgr.GetFsm().SwitchState(Enemy_State_Type.Chase);
        }
    }

}
