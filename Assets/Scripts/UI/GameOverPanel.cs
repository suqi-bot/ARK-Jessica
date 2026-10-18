using UnityEngine.SceneManagement;
using UnityEngine.UI;
/// <summary>
/// 游戏结束面板
/// </summary>
public class GameOverPanel : BasePanel
{
    // 返回主菜单
    public Button Goback;
    // 重新开始
    public Button ReStart;
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
            UIManager.Instance.HidePanel<GameOverPanel>();
            UIManager.Instance.HidePanel<GameScenePanel>();
        });
        // 重新加载当前场景
        ReStart.onClick.AddListener(() =>
        {
            SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().name);
            // 隐藏自己
            UIManager.Instance.HidePanel<GameOverPanel>();
            //UIManager.Instance.ShowPanel<GameScenePanel>();
        });
    }

   
}
