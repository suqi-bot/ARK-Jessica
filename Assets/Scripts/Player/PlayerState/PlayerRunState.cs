using UnityEngine;

public class PlayerRunState : IState
{
    private CharacterFSM fsm;
    private PlayerBlackboard blackboard;
    
    public PlayerRunState()
    {
        fsm = PlayerStateMgr.Instance.getFsm();
        blackboard = fsm.blackBoard as PlayerBlackboard;
    }
    public void OnEnter()
    {
        blackboard.player.nowState = Player_State_Type.Run;
        blackboard.player.groundCheck.MoveAudio(true, 1.2f);
        blackboard.player.animator.Play("Player_Run");
    }

    public void OnExit()
    {
        blackboard.player.groundCheck.MoveAudio(false, 1.2f);
    }

    public void OnUpdate()
    {
        blackboard.player.DoRun();
        if (Input.GetAxis(blackboard.player.horizontalAxis) == 0)
        {
            fsm.SwitchState(Player_State_Type.Idle);
        }
        else if (Input.GetButtonUp(blackboard.player.runButton))
        {
            fsm.SwitchState(Player_State_Type.Move);
        }

        if (Input.GetButtonDown(blackboard.player.skillButton) && blackboard.player.isGrounded)
        {
            // 选择技能
            SkillMgr.Instance.ChooseSkill(blackboard.player.skill);
            if (SkillMgr.Instance.CheckSkillCd())
            {
                fsm.SwitchState(Player_State_Type.Skill);
            }
            else
            {
                Debug.Log("技能正在冷却中");
            }
        }


        // 跳跃
        if (Input.GetButtonDown(blackboard.player.jumpButton) && blackboard.player.isGrounded)
        {
            if (blackboard.player.raycastHit2D && blackboard.player.isGrounded)
            {
                fsm.SwitchState(Player_State_Type.CrossObstacle);
            }
            else
            {
                fsm.SwitchState(Player_State_Type.Jump);
            }
        }
    }

   
}
