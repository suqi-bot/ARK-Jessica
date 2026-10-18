using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameLogPanel : BasePanel
{
    public Button clickBtn;//继续按钮
    private static List<string> contextList = new List<string>();//当前对话文本
    public Transform fatherContext; // 文本父对象
   
    public override void Init()
    {
        clickBtn.onClick.AddListener(() =>
        {
            UIManager.Instance.HidePanel<GameLogPanel>();
        });
        RemoveLastLog();
        // 清除之前保存的日志内容，避免重复显示之前的条目
        ShowLogList();
    }

    /// <summary>
    /// 添加已阅读的文本
    /// </summary>
    /// <param name="name"></param>
    /// <param name="contect"></param>
    /// <param name="iscontinue"></param>
    public void AddLogList(string name, string contect, bool iscontinue)
    {
        if (iscontinue)
            contextList.Add(contect);
        else
            contextList.Add(name+","+ contect);
        
        
    }

    string[] split;
    /// <summary>
    /// 展示所有已阅读的文本
    /// </summary>
    public void ShowLogList()
    {
        if(contextList == null)
        {
            Debug.Log("内容为空");
            return;
        }

        foreach (var item in contextList)
        {
            split = item.Split(",");
            GameObject temp = Instantiate(Resources.Load<GameObject>("Text/TextGrip"), fatherContext);
            
            if (split.Length > 1)
                temp.GetComponent<TextGrip>().Input(split[1], split[0]);
            else
                temp.GetComponent<TextGrip>().Input(split[0]);
        }

        
    }
    /// <summary>
    /// 清除上一次文本
    /// </summary>
    public void RemoveLastLog()
    {
        for (int i = fatherContext.transform.childCount - 1; i >= 0; i--)
        {
            GameObject.Destroy(fatherContext.transform.GetChild(i).gameObject);
        }
    }
    /// <summary>
    /// 清理列表内容
    /// </summary>
    public void ClearLogList()
    {
        contextList.Clear();
    }
}
