using LitJson;
using UnityEngine;
using UnityEngine.UI;

public class TaskItem : MonoBehaviour
{
    //进度条
    public ProgressBar progressBar;
    public Button clickTaskButton;
    public Button clickTextButton;
    //任务图片
    public Image iconImage;
    // 背景图片
    public Image backgroudImage;
    //任务标题
    public Text titleText;
    //任务链id
    public int task_chain_id;
    //子任务id
    public int task_sub_id;
    //任务奖励
    public Text rewardText;
    //rect位置
    public RectTransform rectTransform;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    private void Start()
    {
        clickTaskButton.onClick.AddListener(()=>
        {
            UIManager.Instance.GetPanel<TaskPanel>().SetTaskInfo(task_chain_id, task_sub_id);
        });
        clickTextButton.onClick.AddListener(() =>
        {

        });
    }

    /// <summary>
    /// 设置任务项
    /// </summary>
    public void SetTaskItem(TaskDataItem taskDataItem)
    {
        TaskCfgItem taskCfgItem = GameDataMgr.Instance.taskCfg.GetCfgItem(taskDataItem.task_chain_id, taskDataItem.task_sub_id);
        iconImage.sprite = Resources.Load<Sprite>("Icon/"+taskCfgItem.icon+"_icon");
        titleText.text = taskCfgItem.desc;
        rewardText.text = "奖励：" + JsonMapper.ToObject(taskCfgItem.award)["money"].ToString()+"龙门币";
        task_chain_id = taskCfgItem.task_chain_id;
        task_sub_id = taskCfgItem.task_sub_id;
        int progress = GameDataMgr.Instance.taskData.SearchItem(task_chain_id, task_sub_id).progress;
        backgroudImage.color = progress >= taskCfgItem.target_amount ? new Color32(255, 244, 0, 255) : new Color32(209, 209, 209, 255);
        progressBar.SetProgress(progress, taskCfgItem.target_amount); 

    }

    
   

}
