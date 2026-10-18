using System.Collections.Generic;
using UnityEngine;

public class UIManager 
{
    private static UIManager instance = new UIManager();
    public static UIManager Instance => instance;

    private Dictionary<string, BasePanel> panelDic = new Dictionary<string, BasePanel>();

    private Transform canvasTrans;

    private UIManager()
    {
        GameObject canvas = GameObject.Instantiate(Resources.Load<GameObject>("UI/Canvas"));
        canvasTrans = canvas.transform;

        // 不移除canvas对象
        GameObject.DontDestroyOnLoad(canvas);
        
        
    }
    // 显示面板
    public T ShowPanel<T>() where T : BasePanel
    {
        // 泛型和面板名字一样
        string panelName = typeof(T).Name;

        // 判断字典中 是否已经显示
        if (panelDic.ContainsKey(panelName) )
            return (T)panelDic[panelName];

        GameObject panelObj = GameObject.Instantiate(Resources.Load<GameObject>("UI/"+ panelName));
       
        // 放到场景中
        panelObj.transform.SetParent(canvasTrans, false);

        T panel = panelObj.GetComponent<T>();
        // 把面板存储到字典中
        panelDic.Add(panelName, panel);
        // 调用自己的显示逻辑
        panel.ShowMe();

        return panel;
    }
    /// <summary>
    /// 隐藏面板
    /// </summary>
    /// <typeparam name="T">面板类名</typeparam>
    /// <param name="isFade">是否淡出完毕后删除面板</param>

    public void HidePanel<T>(bool isFade = true) where T : BasePanel
    {
        string panelName = typeof(T).Name;
        if(panelDic.ContainsKey(panelName))
        {
           
            if (isFade)
            {
                // 执行淡出
                panelDic[panelName].HideMe(() =>
                {
                    // 删除对象
                    GameObject.Destroy(panelDic[panelName].gameObject);
                    // 删除字典里面的脚本
                    panelDic.Remove(panelName);
                });
            }
            else
            {
                // 删除对象
                GameObject.Destroy(panelDic[panelName].gameObject);
                // 删除字典里面的脚本
                panelDic.Remove(panelName);
            }

           
            
        }
    }
    // 得到面板
    public T GetPanel<T>() where T : BasePanel
    {
        string panelName = typeof(T).Name;
        if(panelDic.ContainsKey(panelName) )
            return panelDic[panelName] as T;
        // 如果没有对应面板就返回空
        return null;
    }
}
