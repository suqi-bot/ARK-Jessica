
using UnityEngine;
/// <summary>
/// 敌人待机状态
/// </summary>
public class EnemyIdleState : IState
{
    public CharacterFSM fsm;
    public EnemyBlackboard blackboard;
    private float switchTime;
    public EnemyIdleState(CharacterFSM fsm) 
    {
        this.fsm = fsm;
        blackboard = fsm.blackBoard as EnemyBlackboard;
        
    }

    public void OnEnter()
    {
        blackboard.enemy.Idle();
        switchTime = Random.Range(1, 3);
        blackboard.enemy.animator.Play("Enemy_Idle");
        blackboard.enemy.nowState = Enemy_State_Type.Idle;
    }

    public void OnExit()
    {
        
    }

    public void OnUpdate()
    {
        switchTime-=Time.deltaTime;
        if (blackboard.enemy.chaseRaycastHit2D)
        {
            fsm.SwitchState(Enemy_State_Type.Chase);
        }

        if (switchTime < 0) 
        {
            fsm.SwitchState(Enemy_State_Type.Patol);
        }
    }

    
}
