using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// 敌人移动状态
/// </summary>
public class EnemyMoveState : IState
{
    private CharacterFSM fsm;
    private EnemyBlackboard blackboard;
    

    public EnemyMoveState(CharacterFSM fsm)
    {
        this.fsm = fsm;
        blackboard = fsm.blackBoard as EnemyBlackboard;
    }
    
    public void OnEnter()
    {
        blackboard.enemy.nowState = Enemy_State_Type.Patol;
        blackboard.enemy.groundCheck.MoveAudio(true, 0.6f);
        blackboard.enemy.animator.Play("Enemy_Move");
        blackboard.enemy.RandomPatrol();
    }

    public void OnExit()
    {
        blackboard.enemy.groundCheck.MoveAudio(false, 0.6f);
        blackboard.enemy.Idle();
    }

    public void OnUpdate()
    {
       
        blackboard.enemy.Move(true);

        if (blackboard.enemy.chaseRaycastHit2D)
        {
            fsm.SwitchState(Enemy_State_Type.Chase);
        }

        if (blackboard.enemy.UpdatePatrol())
        {
            fsm.SwitchState(Enemy_State_Type.Idle);
        }
        

        
    }

    
}
