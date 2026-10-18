using LitJson;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class TaskCfg
{

    // 任务配置，(链id : 子任务id : TaskCfgItem)
    private Dictionary<int, Dictionary<int, TaskCfgItem>> m_cfg;


    /// <summary>
    /// 读取配置
    /// </summary>
    public void LoadCfg()
    {
        m_cfg = new Dictionary<int, Dictionary<int, TaskCfgItem>>();
        var txt = Resources.Load<TextAsset>("Json/task_cfg").text;
        var jd = JsonMapper.ToObject<JsonData>(txt);


        for (int i = 0, cnt = jd.Count; i < cnt; ++i)
        {
            var itemJd = jd[i] as JsonData;
            TaskCfgItem cfgItem = JsonMapper.ToObject<TaskCfgItem>(itemJd.ToJson());

            if (!m_cfg.ContainsKey(cfgItem.task_chain_id))
            {
                m_cfg[cfgItem.task_chain_id] = new Dictionary<int, TaskCfgItem>();
            }
            m_cfg[cfgItem.task_chain_id].Add(cfgItem.task_sub_id, cfgItem);
        }

    }

    /// <summary>
    /// 获取配置项
    /// </summary>
    /// <param name="chainId">链id</param>
    /// <param name="taskSubId">任务子id</param>
    /// <returns></returns>
    public TaskCfgItem GetCfgItem(int chainId, int taskSubId)
    {
        if (m_cfg.ContainsKey(chainId) && m_cfg[chainId].ContainsKey(taskSubId))
            return m_cfg[chainId][taskSubId];
        return null;
    }

}

/// <summary>
/// 任务配置结构
/// </summary>
[System.Serializable]
public class TaskCfgItem
{
    public int task_chain_id;
    public int task_sub_id;
    public string icon;
    public string desc;
    public string task_target;
    public int target_amount;
    public string award;
    public string open_chain;
}




