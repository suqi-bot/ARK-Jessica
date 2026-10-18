using UnityEngine;
using UnityEngine.Pool;

public class enemy_1546_cliff : Enemy_Base
{
    private ObjectPool<GameObject> bulletPool; // 子弹对象池
    private int nowSkill;
    private AudioSource hitSource;
    private Bullet nowBullet;
    private bool isDeadTwice = false;
    [SerializeField] private GameObject win;
    public override bool Attack()
    {
        if (enemyEvent.isAttack)
        {
            if (nowState == Enemy_State_Type.Revive)
                nowSkill = 3;
            AttackSkill(nowSkill);
            enemyEvent.isAttack = false;
            return true;
        }
        return false;
    }

    public override bool AttackOver()
    {
        if (enemyEvent.isOver)
        {
            enemyEvent.isOver = false;
            return true;
        }
        return false;

    }

    public override void EnemyInit()
    {
        nowBullet = bullets[0].GetComponent<Bullet>();
        chaseDuration = 10f;
        BasePool.Instance.SetItem(bullets[0], firePos[0]);
        bulletPool = BasePool.Instance.GetPool(bullets[0].name);

        maxHp = 220;
        currentHealth = maxHp;
        moveSpeed = 5f;
        // 更新血量面板
        panel.UpdateHp(maxHp, currentHealth);
        
    }
    /// <summary>
    /// 音量设置
    /// </summary>
    public override void PlayHitSource()
    {
        nowBullet.Speed = 40;
        nowBullet.owner = this.gameObject;
        hitSource = firePos[0].GetComponent<AudioSource>();
        hitSource.mute = !GameDataMgr.Instance.audioData.soundOpen;
        hitSource.volume = GameDataMgr.Instance.audioData.soundValue;
        hitSource.Play();
    }

    public override void Jump()
    {
        throw new System.NotImplementedException();
    }

    public void AttackSkill(int skillLevel)
    {
        BasePool.Instance.SetItem(bullets[0], firePos[skillLevel]);
        if(skillLevel == 2)
            FlipTarget(enemyPos.transform.position);
        PlayHitSource();
        bulletPool.Get();
        GameObject fire = Resources.Load<GameObject>("Bullet/BulletFx/FireFx");
        BasePool.Instance.SetItem(fire, firePos[skillLevel]);
        BasePool.Instance.GetPool(fire.name).Get();
    }

    public override void Die()
    {
        
        Debug.Log("敌人死亡，当前血量：" + isDeadTwice);
        if (isDeadTwice)
        {
            Debug.Log("敌人已死亡");
            win.SetActive(true);
            base.Die();    
        }
        else
        {
            currentHealth = 1;
            Debug.Log("敌人死亡，进入复活状态");
            isDeadTwice = true;
            BKMusic.Instance.PlayTime(144f);
            fsmMgr.GetFsm().SwitchState(Enemy_State_Type.Revive);
        }
        
        
    }

    public override int Skill()
    {
        if (Random.Range(0, 100) >= 70)
        {
            if (isDeadTwice)
            {
                AttackDisplacement();
                nowSkill = 2;
            }
            else
            {
                AttackDisplacement();
                nowSkill = 1;
            }
        }
        else
        {
            nowSkill = 0;
        }
        
        return nowSkill;
    }

    public override bool RecoverHp()
    {
        currentHealth += maxHp / 12f * Time.deltaTime;
        panel.UpdateHp(maxHp, currentHealth);
        if (currentHealth >= maxHp)
            return true;
        return false;
    }
    Vector3 displace;
    int randomDirection;
    public void AttackDisplacement()
    {
        randomDirection = Random.Range(0,2);
        displace = new Vector2(Mathf.Sign(this.enemyPos.transform.position.x) * 
            ((isDeadTwice? 15 : 10) * (randomDirection == 0 ? -1 : 1)) + 
            Mathf.Abs(this.enemyPos.transform.position.x), this.transform.position.y);
        if (displace.x < patrolPoint[0].x)
        {
            randomDirection = 1;
        }
        else if (displace.x > patrolPoint[1].x)
        {
            randomDirection = 0;
        }
        displace = new Vector2(Mathf.Sign(this.enemyPos.transform.position.x) *
            ((isDeadTwice ? 15 : 10) * (randomDirection == 0 ? -1 : 1)) +
            Mathf.Abs(this.enemyPos.transform.position.x), this.transform.position.y);
        this.transform.position = displace;
        // 面向目标
        FlipTarget(enemyPos.transform.position);
    }
}
