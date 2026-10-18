using System;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// 任务逻辑
/// </summary>
public class TaskLogic
{
    
    private TaskData taskData;
    private static TaskLogic instance;
    public static TaskLogic Instance
    {
        get {
            if(instance == null)
                instance = new TaskLogic();
            return instance; 
        }
    }


    public TaskLogic()
    {
        taskData = GameDataMgr.Instance.taskData;
    }

    /// <summary>
    /// 获取任务列表
    /// </summary>
    public List<TaskDataItem> taskDatas
    {
        get { return taskData.GetTaskData(); }
    }

    /// <summary>
    /// 更新任务进度
    /// </summary>
    /// <param name="task_chain_id"></param>
    /// <param name="task_sub_id"></param>
    /// <param name="progress"></param>
    /// <param name="action"></param>
    public void AddProgress(int task_chain_id, int task_sub_id, int progress, Action<int, bool> action)
    {
        TaskDataItem item = taskData.SearchItem(task_chain_id, task_sub_id);
        if (item != null)
        {
            item.progress = progress;
            taskData.AddDataOrUpdate(item);
            var cfg = GameDataMgr.Instance.taskCfg.GetCfgItem(task_chain_id, task_sub_id);
            if (cfg != null)
                action(0, item.progress >= cfg.target_amount);
            else
                action(-1, false);

        }
        else
        {
            action(-1, false);
        }
    }

    /// <summary>
    /// 领取任务奖励
    /// </summary>
    /// <param name="task_chain_id"></param>
    /// <param name="task_sub_id"></param>
    public void GetAward(int task_chain_id, int task_sub_id, Action<int, string> action)
    {
        TaskDataItem item = taskData.SearchItem(task_chain_id, task_sub_id);
        TaskCfgItem cfg = GameDataMgr.Instance.taskCfg.GetCfgItem(task_chain_id, task_sub_id);
        if (item == null)
        {
            action(-1, "{}");
            return;
        }
        if (item.award_is_get == 0 && item.progress >= cfg.target_amount)
        {
            item.award_is_get = 1;
            taskData.AddDataOrUpdate(item);
            GoNext(task_chain_id, task_sub_id);
            action(0, cfg.award);
        }
        else
        {
            action(-1, "{}");
        }
    }

    /// <summary>
    /// 开启下一个任务（含支线）
    /// </summary>
    /// <param name="chainId">链id</param>
    /// <param name="subId">任务子id</param>
    private void GoNext(int chainId, int subId)
    {
        var data = taskData.SearchItem(chainId, subId);
        var cfg = GameDataMgr.Instance.taskCfg.GetCfgItem(data.task_chain_id, data.task_sub_id);
        var nextCfg = GameDataMgr.Instance.taskCfg.GetCfgItem(data.task_chain_id, data.task_sub_id + 1);

        if (data.award_is_get == 1)
        {
            // 移除掉已领奖的任务
            taskData.RemoveData(chainId, subId);

            // 开启下一个任务
            if (nextCfg != null)
            {
                TaskDataItem dataItem = new TaskDataItem();
                dataItem.task_chain_id = nextCfg.task_chain_id;
                dataItem.task_sub_id = nextCfg.task_sub_id;
                dataItem.task_target = nextCfg.task_target;
                dataItem.progress = 0;
                dataItem.award_is_get = 0;
                taskData.AddDataOrUpdate(dataItem);
            }
            Debug.Log(taskData);
            // 开启支线任务
            if (!string.IsNullOrEmpty(cfg.open_chain))
            {
                // 开启新分支
                var chains = cfg.open_chain.Split(',');
                for (int i = 0, len = chains.Length; i < len; ++i)
                {
                    var task = chains[i].Split('|');
                    TaskDataItem subChainDataItem = new TaskDataItem();
                    subChainDataItem.task_chain_id = int.Parse(task[0]);
                    subChainDataItem.task_sub_id = int.Parse(task[1]);
                    subChainDataItem.task_target = GameDataMgr.Instance.taskCfg.GetCfgItem(int.Parse(task[0]), int.Parse(task[1])).task_target;
                    
                    subChainDataItem.progress = GameDataMgr.Instance.GetTargetProgress(subChainDataItem.task_target);
                    subChainDataItem.award_is_get = 0;
                    taskData.AddDataOrUpdate(subChainDataItem);
                }
            }
        }
    }


}
