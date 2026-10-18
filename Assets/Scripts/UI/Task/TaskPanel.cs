using LitJson;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
/// <summary>
/// 任务面板
/// </summary>
public class TaskPanel : BasePanel
{
    public Button sureBtn; //确认按钮
    public Button backBtn; //返回按钮
    public Transform taskListContent; //任务列表父物体
    public int task_chain_id; //任务链id
    public int task_sub_id; //子任务id
    private InfiniteScroll infiniteScroll; //无限滚动组件
    public override void Init()
    {
        infiniteScroll = GetComponentInChildren<InfiniteScroll>();
        sureBtn.onClick.AddListener(() =>
        {
            TaskLogic.Instance.GetAward(task_chain_id, task_sub_id, (code, award) =>
            {
                if (code == 0)
                {
                    UIManager.Instance.ShowPanel<InfoPanel>();
                    InfoPanel info = UIManager.Instance.GetPanel<InfoPanel>();
                    var awardJd = JsonMapper.ToObject(award);
                    GameDataMgr.Instance.playerItemData.money += int.Parse(awardJd["money"].ToString());
                    Debug.Log(GameDataMgr.Instance.playerItemData.money);
                    info.SetInfoText("领取成功");
                    infiniteScroll.RefreshVisibleItems();
                    // UpdateTaskList();
                }
                else if (code == -1)
                {
                    UIManager.Instance.ShowPanel<InfoPanel>();
                    InfoPanel info = UIManager.Instance.GetPanel<InfoPanel>();
                    info.SetInfoText("领取失败");
                }
               
            });
        });
        //UpdateTaskList();
        backBtn.onClick.AddListener(() =>
        {
            UIManager.Instance.HidePanel<TaskPanel>();
        });
        List<TaskDataItem> taskDataItems = GameDataMgr.Instance.taskData.GetTaskData();
        GetComponentInChildren<InfiniteScroll>().InitData(taskDataItems);
    }

    /// <summary>
    /// 设置当前任务对象
    /// </summary>
    /// <param name="task_chain_id"></param>
    /// <param name="task_sub_id"></param>
    public void SetTaskInfo(int task_chain_id, int task_sub_id)
    {
        this.task_chain_id = task_chain_id;
        this.task_sub_id = task_sub_id;
    }

    /// <summary>
    /// 更新任务列表
    /// </summary>
    //public void UpdateTaskList()
    //{
    //    List<TaskDataItem> taskDataItems = GameDataMgr.Instance.taskData.GetTaskData();
    //    foreach (var item in taskDataItems)
    //    {
    //        TaskItem task = Instantiate(Resources.Load<TaskItem>("item/TaskItem"), taskListContent);
    //        task.SetTaskItem(item);
    //    }
    //}

}
