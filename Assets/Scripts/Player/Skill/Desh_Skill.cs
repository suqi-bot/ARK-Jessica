using UnityEngine;

public class Desh_Skill : SkillBase
{
    private float deshSpeed = 14f;

    public override void Init()
    {
        skillCd = .8f;
        tempSkillCd = skillCd;
        skillDuration = .3f;
    }
    public override void DoSkill()
    {
        player.isInvincible = true;
        player.GetRigidbody().velocity = new Vector2(deshSpeed * (player.isFacingRight ? 1 : -1), player.GetRigidbody().velocity.y);
    }

    public override void EndSkill()
    {
        player.isInvincible = false;
        player.GetRigidbody().velocity = Vector2.zero;
    }

    
}
