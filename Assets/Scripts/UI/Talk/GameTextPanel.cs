using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 对话结束事件枚举
/// </summary>
public enum EndThings_Type
{
    Closing,
    Shopping,
    Tasking,
}


public class GameTextPanel : BasePanel
{
    public Button skipBtn;//跳过按钮
    public Button logBtn;//日志按钮
    public Button clickBtn;//继续按钮
    public Image charactor1Img;//角色1图片
    public Image charactor2Img;//角色2图片
    public Text context;//对话文本
    public Text textName;//角色名字
    public TextAsset textAsset;//对话文本资源
    private string[] contextList;//当前提取的文本

    public EndThings_Type endType; // 结束事件

    public EventThings eventThings; // 事件触发面板

    public List<Sprite> images = new List<Sprite>(); // 存储图片资源的列表

    private Dictionary<string, Sprite> imageDict = new Dictionary<string, Sprite>(); // 名称对应的图片字典

    public GameLogPanel logPanel;

    protected override void Awake()
    {
        base.Awake();
        images = Resources.LoadAll<Sprite>("Charactor").ToList();
        foreach (var item in images)
        {
            imageDict.Add(item.name, item);
        }

    }

    public override void Init()
    {
        charactor1Img.gameObject.SetActive(false);
        charactor2Img.gameObject.SetActive(false);
        skipBtn.onClick.AddListener(() =>
        {
            EndThings(endType);
        });

        logBtn.onClick.AddListener(() =>
        {
            UIManager.Instance.ShowPanel<GameLogPanel>();
        });
        clickBtn.onClick.AddListener(() =>
        {
            ShowDialogRow();
        });
        logPanel = Resources.Load<GameLogPanel>("UI/GameLogPanel");
        ReadText(textAsset);
        ShowDialogRow();
    }

    /// <summary>
    /// 更新对话文本
    /// </summary>
    /// <param name="textName"></param>
    /// <param name="context"></param>
    public void UpdateText(string textName, string context)
    {
        this.textName.text = textName;
        this.context.text = context;
    }

    Color color1;
    Color color2;
    /// <summary>
    /// 更新角色图片，并根据是否是当前说话角色调整透明度
    /// </summary>
    /// <param name="textName"></param>
    /// <param name="isRight"></param>
    public void UpdateIamge(string textName, bool isRight)
    {

        if (isRight)
        {
            
            charactor2Img.gameObject.SetActive(true);
            charactor2Img.sprite = imageDict[textName];
            color2 = charactor2Img.color;
            color2.a = 1f; // 将角色2的透明度设置为100% 
            charactor2Img.color = color2;
            color1 = charactor1Img.color;
            color1.a = 0.5f; // 将角色1的透明度设置为50%
            charactor1Img.color = color1;
        }
        else
        {
            charactor1Img.gameObject.SetActive(true);
            charactor1Img.sprite = imageDict[textName];
            color1 = charactor1Img.color;
            color1.a = 1f; // 将角色1的透明度设置为100%
            charactor1Img.color = color1;
            color2 = charactor2Img.color;
            color2.a = 0.5f; // 将角色2的透明度设置为50%
            charactor2Img.color = color2;
        }
    }

    /// <summary>
    /// 装载文本
    /// </summary>
    /// <param name="textAsset"></param>
    public void ReadText(TextAsset textAsset)
    {
        contextList = textAsset.text.Split('\n');
        if (contextList.Length != 0)
        {
            Debug.Log("文本读取成功");
        }
        else
        {
            Debug.Log("文本读取失败");
        }
    }

    int dialogRows = 0;
    /// <summary>
    /// 文本解析
    /// </summary>
    public void ShowDialogRow()
    {
        string[] cells;
        foreach (var item in contextList)
        {
            cells = item.Split(",");
            if (cells[0] == "#" && int.Parse(cells[1]) == dialogRows)
            {
                UpdateText(cells[2], cells[3]);
                UpdateIamge(cells[2], cells[4] == "左" ? false : true);
                dialogRows = int.Parse(cells[5]);
                logPanel.AddLogList(cells[2], cells[3], false);
                break;
            }
            else if ( cells[0] == "&" && int.Parse(cells[1]) == dialogRows )
            {
                UpdateText(cells[2], cells[3]);
                dialogRows = int.Parse(cells[5]);
                logPanel.AddLogList(cells[2], cells[3], true);
                break;
            }
            else if (cells[0] == "end")
            {
                EndThings(endType);
            }
        }


    }

    /// <summary>
    /// 结束点击事件，根据不同的结束事件类型执行相应的操作
    /// </summary>
    /// <param name="type"></param>
    public void EndThings(EndThings_Type type)
    {
        switch (type)
        {
            case EndThings_Type.Closing:
                logPanel.ClearLogList();
                UIManager.Instance.HidePanel<GameTextPanel>();
                break;
            case EndThings_Type.Shopping:
                eventThings.ShowEvent("商店", EndThings_Type.Shopping);
                break;
            case EndThings_Type.Tasking:
                eventThings.ShowEvent("接取任务", EndThings_Type.Tasking);
                break;

        }
    }

}
