using UnityEngine;
using UnityEngine.UI;

public class EventThings : MonoBehaviour
{
    public Button eventButton; //事件触发按钮
    public Text eventText; //事件文本
    public Button closeBtuuon; //事件结束按钮
    private EndThings_Type endType = EndThings_Type.Closing; //事件结束类型

    void Start()
    {
        eventButton.onClick.AddListener(ClickEvent);

        closeBtuuon.onClick.AddListener(() =>
        {
            UIManager.Instance.HidePanel<GameTextPanel>();
        });
        this.gameObject.SetActive(false);
    }

    /// <summary>
    /// 设置触发的事件类型
    /// </summary>
    /// <param name="text"></param>
    /// <param name="endType"></param>
    public void ShowEvent(string text, EndThings_Type endType)
    {
        eventText.text = text;
        this.endType = endType;
        this.gameObject.SetActive(true);
    }

    /// <summary>
    /// 点击触发事件
    /// </summary>
    public void ClickEvent()
    {
        switch (endType)
        {
            case EndThings_Type.Shopping:
                // 触发购物事件
                UIManager.Instance.ShowPanel<StorePanel>();
                Debug.Log("触发购物事件");
            break;
            case EndThings_Type.Tasking:
                Debug.Log("触发任务事件");
                break;
        }
    }
}
