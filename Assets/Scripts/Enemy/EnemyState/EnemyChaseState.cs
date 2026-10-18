
using UnityEngine;
/// <summary>
/// 敌人追逐状态
/// </summary>
public class EnemyChaseState : IState
{
    private CharacterFSM fsm;
    private EnemyBlackboard blackboard;
    private float chaseDuration;
    private float chaseTime;
    private Vector2 otherPos;
    private float attackCd;
    public EnemyChaseState(CharacterFSM fsm)
    {
        this.fsm = fsm;
        blackboard = fsm.blackBoard as EnemyBlackboard;
    }

    public void OnEnter()
    {
        chaseTime = 10;
        blackboard.enemy.nowState = Enemy_State_Type.Chase;
        blackboard.enemy.groundCheck.MoveAudio(true, 0.6f);
        chaseDuration = blackboard.enemy.chaseDuration;
        attackCd = blackboard.enemy.attackCd;
        blackboard.enemy.ChaseTarget();
        blackboard.enemy.animator.Play("Enemy_Move");
    }

    public void OnExit()
    {
        blackboard.enemy.groundCheck.MoveAudio(false, 0.6f);
    }

    public void OnUpdate()
    {
        chaseTime -= Time.deltaTime;
        attackCd -= Time.deltaTime;
        otherPos = blackboard.enemy.ChasePos();

        if (blackboard.enemy.attackRaycastHit2D && attackCd < 0)
        {
            fsm.SwitchState(Enemy_State_Type.Attack);
        }

        if (Vector2.Distance(blackboard.enemy.transform.position, otherPos) >= chaseDuration)
        {
            blackboard.enemy.Move(true);

        }
        else
        {
            blackboard.enemy.Move(false);
        }
  
        


        if (chaseTime < 0)
        {
            fsm.SwitchState(Enemy_State_Type.Idle);
        }


    }

   
}
