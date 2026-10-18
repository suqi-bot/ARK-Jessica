using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// 敌人复活状态
/// </summary>
public class EnemyReviveState : IState
{
    private CharacterFSM fsm;
    private EnemyBlackboard blackboard;
    private float attackCd;
    private int skills;
    private bool OnAttack;
    private bool attackOver;
    public EnemyReviveState(CharacterFSM fsm)
    {
        this.fsm = fsm;
        blackboard = fsm.blackBoard as EnemyBlackboard;

    }
    public void OnEnter()
    {

        blackboard.enemy.animator.Play("Enemy_Revive");
        blackboard.enemy.isInvincible = true;
        blackboard.enemy.nowState = Enemy_State_Type.Revive;
    }

    public void OnExit()
    {
        blackboard.enemy.isInvincible = false;
    }
    bool isRecover;
    public void OnUpdate()
    {
        //attackCd -= Time.deltaTime;
        attackOver = blackboard.enemy.AttackOver();
        OnAttack = blackboard.enemy.Attack();
        isRecover = blackboard.enemy.RecoverHp();
        //Debug.Log("attackOver:"+attackOver+"isRecover"+ isRecover);
        if(OnAttack)
        {
            blackboard.enemy.PlayHitSource();
        }

        if (isRecover)
        {
            attackOver = false;
            fsm.SwitchState(Enemy_State_Type.Chase);
        }
    }

    
}
