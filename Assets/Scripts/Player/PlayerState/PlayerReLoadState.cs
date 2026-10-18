using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerReLoadState : IState
{
    private CharacterFSM fsm;
    private PlayerBlackboard blackboard;
    private float reLoadCd;
    public PlayerReLoadState()
    {
        fsm = PlayerStateMgr.Instance.getFsm();
        blackboard = fsm.blackBoard as PlayerBlackboard;
        
    }

    public void OnEnter()
    {
        reLoadCd = blackboard.player.weapon.reLoadCd;
        blackboard.player.animator.Play("Player_Reload");
        blackboard.player.weapon.ReLoadAudio();
        blackboard.player.nowState = Player_State_Type.ReLoad;
    }

    public void OnExit()
    {
        
    }

    public void OnUpdate()
    {
        reLoadCd -= Time.deltaTime;
        // blackboard.player.ReLoadMove();
        if(reLoadCd <= 0)
        {
            blackboard.player.ReLoad();
            if (Input.GetAxis(blackboard.player.horizontalAxis) != 0)
            {
                fsm.SwitchState(Player_State_Type.Move);
            }
            else
            {
                fsm.SwitchState(Player_State_Type.Idle);
            }
        }
    }
}
