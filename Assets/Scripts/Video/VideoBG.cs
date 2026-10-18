using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VideoBG : MonoBehaviour
{
    
    void Start()
    {
        RectTransform re = this.GetComponent<RectTransform>();
        re.sizeDelta = new Vector2(Screen.width,Screen.height);
    }

   
}
