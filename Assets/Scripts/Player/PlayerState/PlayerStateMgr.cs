using System;
using UnityEngine;



public enum Player_State_Type
{
    Deploy,
    Idle,
    Move,
    Attack,
    Injured,
    Jump,
    CrossObstacle,
    Skill,
    Run,
    ReLoad,
    Die,
    Success
}


public class PlayerStateMgr : MonoBehaviour
{
    private static PlayerStateMgr instance;
    public static PlayerStateMgr Instance => instance;

    public PlayerBlackboard playerBlackboard;

    private CharacterFSM fsm;

    private void Awake()
    {
        instance = this;
        fsm = new CharacterFSM(playerBlackboard);
        
    }

    void Start()
    {
        fsm.AddState(Player_State_Type.Deploy, new PlayerDeployState());
        fsm.AddState(Player_State_Type.Idle, new PlayerIdleState());
        fsm.AddState(Player_State_Type.Move, new PlayerMoveState());
        fsm.AddState(Player_State_Type.Jump, new PlayerJumpState());
        fsm.AddState(Player_State_Type.CrossObstacle, new PlayerCrossObstacle());
        fsm.AddState(Player_State_Type.Skill, new PlayerSkillState());
        fsm.AddState(Player_State_Type.Run, new PlayerRunState());
        fsm.AddState(Player_State_Type.ReLoad, new PlayerReLoadState());
        fsm.AddState(Player_State_Type.Attack, new PlayerAttackState());
        fsm.AddState(Player_State_Type.Die, new PlayerDieState());
        fsm.AddState(Player_State_Type.Injured, new PlayerInjuredState());
        fsm.SwitchState(Player_State_Type.Deploy);
    }

    // Update is called once per frame
    void Update()
    {
        fsm.OnUpdate();
    }
    public CharacterFSM getFsm()
    {
        return fsm;
    }


}
[Serializable]
public class PlayerBlackboard : BlackBoard
{
    public Player player;


}
