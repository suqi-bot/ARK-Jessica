using UnityEngine;

public class NPCBase : MonoBehaviour
{
    public TextAsset[] assets; //对话文本集
    GameTextPanel panel;
    public EndThings_Type endType; //对话结束后事件类型

    /// <summary>
    /// 进入对话事件
    /// </summary>
    public void EnterTalkEvent()
    {
        panel = Resources.Load<GameTextPanel>("UI/GameTextPanel");
        panel.endType = endType;
        panel.textAsset = assets[0];
        UIManager.Instance.ShowPanel<GameTextPanel>();
        
    }
}
