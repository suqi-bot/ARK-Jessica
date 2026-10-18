using UnityEngine;

public class PlayerSkillState : IState
{
    private CharacterFSM fsm;
    private PlayerBlackboard blackboard;
    private float durationTime;
    public PlayerSkillState()
    {
        fsm = PlayerStateMgr.Instance.getFsm();
        blackboard = fsm.blackBoard as PlayerBlackboard;
    }

    public void OnEnter()
    {
        SkillMgr.Instance.InitSkill();
        durationTime = SkillMgr.Instance.GetSkillDuration();
        
        blackboard.player.nowState = Player_State_Type.Skill;
    }

    public void OnExit()
    {
        SkillMgr.Instance.EndSkill();
    }

    public void OnUpdate()
    {
        SkillMgr.Instance.DoSkill();
        durationTime -= Time.deltaTime;
        
        if (durationTime <= 0)
        {
            fsm.SwitchState(Player_State_Type.Idle);
        }
    }
}
