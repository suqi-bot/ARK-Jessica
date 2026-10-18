using UnityEngine;
/// <summary>
/// 敌人死亡状态
/// </summary>
public class EnemyDieState : IState
{
    private CharacterFSM fsm;
    private EnemyBlackboard blackboard;
    private bool attackOver;
    public EnemyDieState(CharacterFSM fsm)
    {
        this.fsm = fsm;
        blackboard = fsm.blackBoard as EnemyBlackboard;

    }
    public void OnEnter()
    {
        blackboard.enemy.animator.Play("Enemy_Die");
        blackboard.enemy.panel.HideSelf();
        blackboard.enemy.nowState = Enemy_State_Type.Die;
    }

    public void OnExit()
    {
        
    }
    public void OnUpdate()
    {
        //attackCd -= Time.deltaTime;
        attackOver = blackboard.enemy.AttackOver();
        
        if (attackOver)
        {
            blackboard.enemy.EnemyInit();
            blackboard.enemy.enemypool.Release(blackboard.enemy.gameObject);
        }
    }

    
}
