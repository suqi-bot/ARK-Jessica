using System;
using UnityEngine;

public enum Enemy_State_Type
{
    Idle,// ´ý»ú
    Patol, // Ñ²Âß
    Chase, //×·×Ù
    React, // ·´Ó¦
    Attack, // ¹¥»÷
    Revive, // ¸´»î
    Die, // ËÀÍö

}
[Serializable]
public class EnemyBlackboard : BlackBoard
{
    public Enemy_Base enemy;


}


public class EnemyStateMgr : MonoBehaviour
{
    
    public EnemyBlackboard blackBoard;
    private CharacterFSM fsm;

    private void Awake()
    {

        fsm = new CharacterFSM(blackBoard);
        fsm.AddState(Enemy_State_Type.Idle, new EnemyIdleState(fsm));
        fsm.AddState(Enemy_State_Type.Patol, new EnemyMoveState(fsm));
        fsm.AddState(Enemy_State_Type.Attack, new EnemyAttackState(fsm));
        fsm.AddState(Enemy_State_Type.Chase, new EnemyChaseState(fsm));
        fsm.AddState(Enemy_State_Type.Revive, new EnemyReviveState(fsm));
        fsm.AddState(Enemy_State_Type.Die, new EnemyDieState(fsm));
        fsm.SwitchState(Enemy_State_Type.Idle);
    }

    private void OnEnable()
    {
        fsm.SwitchState(Enemy_State_Type.Idle);
    }

    void Start()
    {
       
    }

    // Update is called once per frame
    void Update()
    {
        fsm.OnUpdate();
    }

    public CharacterFSM GetFsm()
    {
        return fsm;
    }
}
