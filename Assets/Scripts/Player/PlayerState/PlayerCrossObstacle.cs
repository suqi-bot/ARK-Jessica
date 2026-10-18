using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCrossObstacle : IState
{
    private CharacterFSM fsm;
    private PlayerBlackboard blackboard;
    private bool isDone;
    private float mixTime = 1.5f;
    public PlayerCrossObstacle()
    {
        fsm = PlayerStateMgr.Instance.getFsm();
        blackboard = fsm.blackBoard as PlayerBlackboard;
    }
    public void OnEnter()
    {
        blackboard.player.CheckJumpOb();
        blackboard.player.animator.Play("Player_Climb");
        blackboard.player.groundCheck.JumpUpAudio();
        blackboard.player.nowState = Player_State_Type.CrossObstacle;
        blackboard.player.isInvincible = true;
    }

    public void OnExit()
    {
        blackboard.player.groundCheck.JumpDownAudio();
        blackboard.player.isInvincible = false;
    }

    public void OnUpdate()
    {
        mixTime -= Time.deltaTime;
        isDone = blackboard.player.DoJumpOb();
        if (isDone || mixTime <= 0) {
            fsm.SwitchState(Player_State_Type.Idle);
            mixTime = 1.5f;
        }
        
    }
}
