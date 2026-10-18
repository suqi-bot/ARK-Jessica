using System.Collections.Generic;
using UnityEngine;


public enum Skill_Type
{
    Desh,
    Smoke,
    Kick
}

public class SkillMgr
{
    private static SkillMgr instance = new SkillMgr();

    public static SkillMgr Instance => instance;

    private Dictionary<Skill_Type, SkillBase> skillDic = new Dictionary<Skill_Type, SkillBase>();

    private Skill_Type nowType;

    private SkillMgr()
    {
        skillDic.Add(Skill_Type.Desh, new Desh_Skill());
        skillDic.Add(Skill_Type.Smoke, new Smoke_Skill());
        skillDic.Add(Skill_Type.Kick, new Kick_Skill());
    }

    public void ChooseSkill(Skill_Type type)
    {
        if (skillDic.ContainsKey(type))
        {
            nowType = type;
        }
        else
        {
            Debug.Log("¸Ã¼¼ÄÜÎ´×¢²á");
        }
    }

    public void InitSkill()
    {
        skillDic[nowType].InitSkill(nowType);
    }

    public void DoSkill()
    {
        skillDic[nowType].DoSkill();
    }

    public void EndSkill()
    {
        skillDic[nowType].EndSkill();
    }
    
    public bool CheckSkillCd()
    {
        return skillDic[nowType].CheckSkillCd();
    }

    public float GetSkillTime(Skill_Type type)
    {
        return skillDic[type].GetSkillTime();
    }
    

    public void UpdateSkillTime()
    {
        foreach (var item in skillDic)
        {
            item.Value.UpdateSkillTime();
        }
    }

    public float GetSkillDuration()
    {
        return skillDic[nowType].GetSkillDuration();
    }

    
}
