using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EnemyPanel : MonoBehaviour 
{

    public GameObject enemy;
    public Image hpFG;
    public Image ammoBG;
    public Image ammoFG;
    public GameObject Ammo_Line;
    public Transform statusBar;
    public float loadCd = 0.05f;
    private Vector3 lastPos;
    private Vector3 enemyPos;
    private float hpCapacity;

    private float screenOffest;
    // Start is called before the first frame update

    private void Awake()
    {
        screenOffest = -Screen.height / 5.6f;
        hpCapacity = ammoBG.rectTransform.sizeDelta.x;
    }

    // Update is called once per frame
    void Update()
    {

    }

    private void LateUpdate()
    {

        enemyPos = Camera.main.WorldToScreenPoint(enemy.transform.position);
        lastPos = statusBar.position;
        if (Mathf.Abs(statusBar.position.x - enemyPos.x) > Mathf.Abs(screenOffest))
        {
            statusBar.transform.position = enemyPos + Vector3.up * screenOffest;

        }
        else
        {
            statusBar.transform.position = Vector3.Lerp(lastPos, enemyPos + Vector3.up * screenOffest, Time.deltaTime * 30);


        }


    }

    public void UpdateHp(float maxHp, float curretHp)
    {
        if(curretHp > maxHp)
            curretHp = maxHp;
        hpFG.rectTransform.sizeDelta = new Vector2(Mathf.Max(0, hpCapacity * (curretHp / maxHp)), hpFG.rectTransform.sizeDelta.y);
    }

    public void UpdateAmmo(int maxAmmo, int nowAmmo)
    {
        ammoFG.rectTransform.sizeDelta = new Vector2(Mathf.Max(0, hpCapacity * ((float)nowAmmo / maxAmmo)), ammoFG.rectTransform.sizeDelta.y);
    }
    GameObject g;
    List<GameObject> list;
    public void DrawAmmoLine(int ammoNum)
    {
        if (list != null)
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

    public void HideSelf()
    {
        this.gameObject.SetActive(false);
    }
    
    public void ShowSelf()
    {
        this.gameObject.SetActive(true);
    }
}
