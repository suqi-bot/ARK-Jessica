using UnityEngine;

public class PlayerIdleState : IState
{
    private CharacterFSM fsm;
    private PlayerBlackboard blackboard;

    public PlayerIdleState(){
        fsm = PlayerStateMgr.Instance.getFsm();
        blackboard = fsm.blackBoard as PlayerBlackboard;
    }
    public void OnEnter()
    {
        blackboard.player.nowState = Player_State_Type.Idle;
        blackboard.player.animator.Play("Player_Idle");
    }

    public void OnExit()
    {
        
    }

    public void OnUpdate()
    {
        // 部署
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            fsm.SwitchState(Player_State_Type.Deploy);
        }

        

        // 攻击
        if (Input.GetButtonDown(blackboard.player.fireButton) && blackboard.player.weapon.nowClipAmmoNum != 0)
        {
            fsm.SwitchState(Player_State_Type.Attack);
        }
        
        // 移动
        if (Input.GetAxis(blackboard.player.horizontalAxis) != 0)
        {
            
            if(Input.GetButton(blackboard.player.runButton) && blackboard.player.isGrounded)
            {
                fsm.SwitchState(Player_State_Type.Run);
            }
            else
            {
                fsm.SwitchState(Player_State_Type.Move);
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

        // 交互键
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
        // 释放技能
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
                Debug.Log(blackboard.player.skill.ToString() + "技能正在冷却中");
            }
        }

        // 释放终结技
        if (Input.GetButtonDown(blackboard.player.uniqueSkillButton) && blackboard.player.isGrounded)
        {
            // 选择技能
            SkillMgr.Instance.ChooseSkill(blackboard.player.uniqueSkill);
            if (SkillMgr.Instance.CheckSkillCd())
            {
                Debug.Log("释放终结技");
                fsm.SwitchState(Player_State_Type.Skill);
            }
            else
            {
                Debug.Log(blackboard.player.uniqueSkill.ToString() + "技能正在冷却中");
            }
        }

        // 换弹
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
    

