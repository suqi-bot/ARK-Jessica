using UnityEngine;

public class TestTalkSystem : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        //UIManager.Instance.ShowPanel<TaskPanel>();
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            TaskLogic.Instance.AddProgress(1, 1, 1, (i, b) =>
            {

            });
        }
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            UIManager.Instance.ShowPanel<TaskPanel>();
        }

        if (Input.GetKeyDown(KeyCode.V))
        {
            GameDataMgr.Instance.playerItemData.money = 100;
            UIManager.Instance.ShowPanel<StorePanel>();
        }
    }
}
