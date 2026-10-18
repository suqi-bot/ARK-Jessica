using UnityEngine;

public abstract class SkillBase 
{

    [Header("¼¼ÄÜCd")]
    protected float skillCd;
    protected float skillDuration;
    private CharacterFSM fsm;
    private PlayerBlackboard blackboard;
    protected float tempSkillCd;
    protected Player player;

    public virtual void InitSkill(Skill_Type type)
    {
        fsm = PlayerStateMgr.Instance.getFsm();
        blackboard = fsm.blackBoard as PlayerBlackboard;
        player = blackboard.player;
        Init();
        player.animator.Play(type + "_Skill");
        Debug.Log(type + "_Skill");
    }

    public abstract void Init();

    public abstract void DoSkill();

    public abstract void EndSkill();

    public virtual void UpdateSkillTime()
    {
        tempSkillCd -= Time.deltaTime;
    }


    public virtual float GetSkillDuration()
    {
        return skillDuration;
    }

    public virtual bool CheckSkillCd()
    {
        if (tempSkillCd < 0)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public float GetSkillTime()
    {
        return tempSkillCd;
    }
    
}
