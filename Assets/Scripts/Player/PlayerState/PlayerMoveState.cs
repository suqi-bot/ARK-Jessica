using UnityEngine;

public class PlayerMoveState : IState
{
    private CharacterFSM fsm;
    private PlayerBlackboard blackboard;

    public PlayerMoveState()
    {
        fsm = PlayerStateMgr.Instance.getFsm();
        blackboard = fsm.blackBoard as PlayerBlackboard;
    }
    public void OnEnter()
    {
        blackboard.player.groundCheck.MoveAudio(true, 0.8f);
        blackboard.player.animator.Play("Player_Move");
        blackboard.player.nowState = Player_State_Type.Move;
    }

    public void OnExit()
    {
        blackboard.player.groundCheck.MoveAudio(false, 0.8f);
        
    }

    public void OnUpdate()
    {
        
        blackboard.player.XMove();
        if (Input.GetButtonDown(blackboard.player.fireButton) && blackboard.player.weapon.nowClipAmmoNum != 0)
        {
            fsm.SwitchState(Player_State_Type.Attack);
        }
        if (Input.GetAxis(blackboard.player.horizontalAxis) != 0 
            && blackboard.player.isGrounded && Input.GetButton(blackboard.player.runButton))
        {
            fsm.SwitchState(Player_State_Type.Run);
        }


        if (Input.GetAxis(blackboard.player.horizontalAxis) == 0 && blackboard.player.isGrounded)
        {
            fsm.SwitchState(Player_State_Type.Idle);
        }

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

        // 释放踢技
        if (Input.GetButtonDown(blackboard.player.kickButton) && blackboard.player.isGrounded)
        {
            SkillMgr.Instance.ChooseSkill(blackboard.player.kick_skill);
            if (SkillMgr.Instance.CheckSkillCd())
            {
                fsm.SwitchState(Player_State_Type.Skill);
            }
            else
            {
                Debug.Log(blackboard.player.kick_skill.ToString() + "技能正在冷却中");
            }
        }

        if (Input.GetButtonDown(blackboard.player.uniqueSkillButton) && blackboard.player.isGrounded)
        {
            // 选择技能
            SkillMgr.Instance.ChooseSkill(blackboard.player.uniqueSkill);
            if (SkillMgr.Instance.CheckSkillCd())
            {
                fsm.SwitchState(Player_State_Type.Skill);
            }
            else
            {
                Debug.Log(blackboard.player.uniqueSkill.ToString() + "技能正在冷却中");
            }
        }


        if (Input.GetButtonDown(blackboard.player.reLoadButton) && blackboard.player.isGrounded)
        {
            if (blackboard.player.weapon.CanReLoad())
            {
                fsm.SwitchState(Player_State_Type.ReLoad);
            }
            else
            {
                Debug.Log("无法换弹");
            }
        }

       
        
    }

   
}
