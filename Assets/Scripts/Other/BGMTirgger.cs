using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BGMTirgger : MonoBehaviour
{
    public AudioClip clip;
    public bool isTriggle = false;
    public bool isAttackTwo = false;
    public Enemy_Base enemy;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
        
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            if (!isTriggle)
            {
                isTriggle = true;
                BKMusic.Instance.PlayMusic(clip);
            }
        }
    }
}
