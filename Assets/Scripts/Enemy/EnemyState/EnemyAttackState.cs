using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyAttackState : IState
{
    private CharacterFSM fsm;
    private EnemyBlackboard blackboard;
    private float attackCd;
    private int skills;
    private bool attackOver;
    public EnemyAttackState(CharacterFSM fsm)
    {
        this.fsm = fsm;
        blackboard = fsm.blackBoard as EnemyBlackboard;

    }
    public void OnEnter()
    {
        attackCd = blackboard.enemy.attackCd;
        skills = blackboard.enemy.Skill();
        blackboard.enemy.nowState = Enemy_State_Type.Attack;
        if (skills == 0)
        {
            blackboard.enemy.animator.Play("Enemy_Attack");
        }
        else
        {
            blackboard.enemy.animator.Play("Enemy_Attack_Skill"+skills);
        }
        
    }

    public void OnExit()
    {
        
    }

    public void OnUpdate()
    {
        //attackCd -= Time.deltaTime;
        attackOver = blackboard.enemy.AttackOver();
        blackboard.enemy.Attack();


        if (attackOver)
        {
            fsm.SwitchState(Enemy_State_Type.Chase);
        }
    }

    
}
