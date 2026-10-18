using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InitGamePanel : MonoBehaviour
{
    // Start is called before the first frame update
    void Awake()
    {
        Time.timeScale = 1;
        UIManager.Instance.ShowPanel<PlayerHpPanel>().SetOwner(this.gameObject);
        UIManager.Instance.ShowPanel<GameScenePanel>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
