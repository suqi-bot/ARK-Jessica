using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class PlayerHpPanel : BasePanel
{

    private GameObject player;
    public Image hpFG;
    public Image ammoBG;
    public Image ammoFG;
    public GameObject Ammo_Line;
    public Transform statusBar;
    public float loadCd = 0.05f;
    private Vector3 lastPos;
    private Vector3 playerPos;

    private float hpCapacity;

    private float screenOffest;
    // Start is called before the first frame update
    public override void Init()
    {
        screenOffest = -Screen.height / 5.6f;
        
       
    }

    

    private void LateUpdate()
    {
        
        playerPos = Camera.main.WorldToScreenPoint(player.transform.position);
        lastPos = statusBar.position;
        if (Mathf.Abs (statusBar.position.x - playerPos.x) > Mathf.Abs(screenOffest))
        {
            statusBar.transform.position = playerPos + Vector3.up * screenOffest;
          
        }
        else
        {
            statusBar.transform.position = Vector3.Lerp(lastPos, playerPos + Vector3.up * screenOffest, Time.deltaTime * 30);
            

        }
        

    }

    public void SetOwner(GameObject owner)
    {
        player = owner;
        hpCapacity = hpFG.rectTransform.sizeDelta.x;
    }

    public void UpdateHp(float maxHp, float curretHp)
    {
        hpFG.rectTransform.sizeDelta = new Vector2(Mathf.Max(0, hpCapacity * (curretHp / maxHp)) , hpFG.rectTransform.sizeDelta.y);
    }

    public void UpdateAmmo(int maxAmmo, int nowAmmo)
    {
        ammoFG.rectTransform.sizeDelta = new Vector2(Mathf.Max(0, hpCapacity * ((float)nowAmmo / maxAmmo)), ammoFG.rectTransform.sizeDelta.y);
    }
    GameObject g;
    List<GameObject> list;
    /// <summary>
    /// 弹药画线
    /// </summary>
    /// <param name="ammoNum"></param>
    public void DrawAmmoLine(int ammoNum)
    {
        if(list != null)
        {
            for (int i = 0; i < list.Count; i++)
            {
                Destroy(list[i]);
            }
            list = null;
        }
            
        float size = ammoBG.rectTransform.sizeDelta.x / ammoNum;
        list = new List<GameObject>();
        for (int i = 1; i < ammoNum; i++)
        {
            g = Instantiate(Ammo_Line);
            g.transform.SetParent(ammoBG.transform);
            g.transform.localPosition = new Vector2(-107.5f + size * i, g.transform.position.y);
            list.Add(g);
        }
    }
}
