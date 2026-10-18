using Spine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameScenePanel : BasePanel                       
{
    
    public Button BtnSetting;// 设置按钮
    public Button BtnBag;// 背包按钮
    public Text dash;
    public Text smoke;
    public override void Init()
    {
        Time.timeScale = 1;
        // 打开设置面板
        BtnSetting.onClick.AddListener(() =>
        {
            UIManager.Instance.ShowPanel<SettingPanel>();
        });

        BtnBag.onClick.AddListener(() =>
        {
            UIManager.Instance.ShowPanel<BagPanel>();
        });
    }
    float dashTime;
    float smokeTime;
    string formatI = "0";
    string formatF = "0";
    protected override void Update()
    {
        base.Update();
        dashTime = SkillMgr.Instance.GetSkillTime(Skill_Type.Desh);
        if(dashTime < 0)
        {
            dashTime = 0;
            formatI = "0";
        }
        else
        {
            formatI = "0.00";
        }

        dash.text = "冲刺："+ dashTime.ToString(formatI);

        smokeTime = SkillMgr.Instance.GetSkillTime(Skill_Type.Smoke);
        if (smokeTime < 0)
        {
            formatF = "0";
            smokeTime = 0;
        }else
        {
            formatF = "0.00";
        }
        smoke.text = "烟雾弹：" + smokeTime.ToString(formatF);
    }

}
