using UnityEngine;


public class enemy_1007_slime : Enemy_Base
{
    Collider2D[] coll;
    
    public override bool Attack()
    {
        coll = attackBox.GetDetectedColliders();
      
        if (enemyEvent.isAttack)
        {
            CharacterBaseClass character;

            foreach (var item in coll)
            {
                character = item.GetComponent<CharacterBaseClass>();
                if (!character.isInvincible && !character.isDodge)
                {
                    character.Injure(atk);
                   
                }
                enemyEvent.isAttack = false;
            }
            return true;
        }
        return false;
    }

    public override bool AttackOver()
    {
        if (enemyEvent.isOver)
        {
            coll = null;
            enemyEvent.isOver = false;
            return true;

        }
        return false;
    }

    public override void Die()
    {
        base.Die();
        if (!isTarget)
        {
            GameDataMgr.Instance.AddOrUpdateTargetProgress(this.name, 1);
            isTarget = true;
        }
    }

    public override void EnemyInit()
    {
        maxHp = 25;
        currentHealth = maxHp;
        moveSpeed = 2f;
        attackCd = 1f;
        isTarget = false;
        // 更新血量面板
        panel.UpdateHp(maxHp, currentHealth);
    }

    public override void Jump()
    {
        
    }

    public override bool RecoverHp()
    {
        throw new System.NotImplementedException();
    }

    public override int Skill()
    {
        return 0;
    }
}
