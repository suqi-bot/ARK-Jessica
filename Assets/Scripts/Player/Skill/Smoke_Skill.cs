using UnityEngine;

public class Smoke_Skill : SkillBase
{
    Smoke smokeSkill;
    private float dodge = 100f;
    public override void Init()
    {
        skillCd = 5;
        skillDuration = .5f;
        tempSkillCd = skillCd;
        GameObject obj = GameObject.Instantiate(Resources.Load<GameObject>("Skill/Smoke"),player.transform.position, player.transform.rotation);
        smokeSkill = obj.GetComponent<Smoke>();
        smokeSkill.Init(player, dodge, 3);
    }
    
    public override void DoSkill()
    {
        
    }

    public override void EndSkill()
    {
        
    }

    
}
