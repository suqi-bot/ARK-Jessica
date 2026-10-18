using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StartGamePanel : MonoBehaviour
{
    
    void Start()
    {
        UIManager.Instance.ShowPanel<GameScenePanel>();
    }

   
}
