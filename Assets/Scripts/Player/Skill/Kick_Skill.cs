
using UnityEngine;

public class Kick_Skill : SkillBase
{
    // 获取所有碰撞体
    private Collider2D[] all;
    // 位移距离
    private float dir;
    public override void DoSkill()
    {
        player.DoAttackMove(dir);
    }

    public override void EndSkill()
    {
        
    }

    public override void Init()
    {
        all = player.kickBox.GetDetectedColliders();
        dir = 5;
        skillCd = .4f;
        skillDuration = .4f;
        if (all != null)
        {

            foreach (var item in all)
            {
                if (item.CompareTag("Door"))
                {
                    item.GetComponentInParent<Door>().KickDoor(player.transform);
                    
                }
                if (item.CompareTag("NPC"))
                {
                    item.GetComponent<NPCBase>().EnterTalkEvent();
                }
                return;
            }
        }
            
        
    }

    
}
