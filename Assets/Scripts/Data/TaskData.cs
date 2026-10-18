using System.Collections.Generic;
using UnityEngine;

public class TaskData : MonoBehaviour
{

    private List<TaskDataItem> taskDataItems;

    public TaskData()
    {
        taskDataItems = new List<TaskDataItem>();
    }

    /// <summary>
    /// 添加数据
    /// </summary>
    /// <param name="dataItem"></param>
    public void AddDataOrUpdate(TaskDataItem dataItem)
    {
        bool isUpdate = false;
        for (int i = 0; i < taskDataItems.Count; i++)
        {
            TaskDataItem item = taskDataItems[i];
            if (item.task_chain_id == dataItem.task_chain_id && item.task_sub_id == dataItem.task_sub_id)
            {
                isUpdate = true;
                taskDataItems[i] = dataItem;
                break;
            }

        }
        if (!isUpdate)
            taskDataItems.Add(dataItem);
        // 任务排序
        taskDataItems.Sort((a, b) =>
        {
            return a.task_chain_id.CompareTo(b.task_chain_id);
        });
    }

    /// <summary>
    /// 查询搜索项
    /// </summary>
    /// <returns></returns>
    public TaskDataItem SearchItem(int task_chain_id, int task_sub_id)
    {
        foreach (var item in taskDataItems)
        {
            if (item.task_chain_id == task_chain_id && item.task_sub_id == task_sub_id)
                return item;
        }
        return null;
    }

    public List<TaskDataItem> SearchItemsByTaskname(string task_name)
    {
        List<TaskDataItem> result = new List<TaskDataItem>();
        foreach (var item in taskDataItems)
        {
            if (item.task_target == task_name)
                result.Add(item);
        }
        return result;
    }

    /// <summary>
    /// 移除任务项
    /// </summary>
    /// <param name="task_chain_id"></param>
    /// <param name="task_sub_id"></param>
    public void RemoveData(int task_chain_id, int task_sub_id)
    {
        TaskDataItem temp = SearchItem(task_chain_id, task_sub_id);
        if (temp != null)
            taskDataItems.Remove(temp);
    }

    public List<TaskDataItem> GetTaskData()
    {
        return taskDataItems;
    }

}
/// <summary>
/// 任务数据
/// </summary>
public class TaskDataItem
{
    // 链id
    public int task_chain_id;
    // 任务子id
    public int task_sub_id;
    // 进度
    public int progress;
    // 目标名称
    public string task_target;
    // 奖励是否被领取了，0：未被领取，1：已被领取
    public short award_is_get;
}
