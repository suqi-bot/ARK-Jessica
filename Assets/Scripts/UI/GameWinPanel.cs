using UnityEngine.SceneManagement;
using UnityEngine.UI;
/// <summary>
/// 游戏结束面板
/// </summary>
public class GameWinPanel : BasePanel
{
    // 返回主菜单
    public Button Goback;
    // 信息
    public Text info;
    public override void Init()
    {
        UIManager.Instance.HidePanel<GameScenePanel>();
        // 返回主菜单
        Goback.onClick.AddListener(() =>
        {
            SceneManager.LoadSceneAsync("BeginScene");
            // 隐藏自己
            UIManager.Instance.HidePanel<GameWinPanel>();
            UIManager.Instance.HidePanel<GameScenePanel>();
            UIManager.Instance.HidePanel<PlayerHpPanel>();
        });
       
    }

   
}
