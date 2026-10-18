using UnityEngine;

public class Smoke : MonoBehaviour
{
    // 闪避概率 1 ~ 100
    public float dodge = 100;
    // 技能持续时间
    public float duration = 3;
    // 是否处于范围中
    public bool isStay;
    private Player player;
    private AudioSource audioSource;
    /// <summary>
    /// 初始技能
    /// </summary>
    public void Init(Player player , float dodge, float duration)
    {
        this.dodge = dodge;
        this.duration = duration;
        this.player = player;
        // 初始化音效
        audioSource = GetComponent<AudioSource>();
        AudioData data = GameDataMgr.Instance.audioData;
        audioSource.mute = !data.soundOpen;
        audioSource.volume = data.soundValue;
        audioSource.Play();
    }

    public void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            isStay = true;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            isStay = false;
        }
    }

    private void Update()
    {
        duration -= Time.deltaTime;
        if (duration <= 0)
        {
            player.isDodge = false;
            Destroy(this.gameObject);
        }
        else if(isStay)
        {
            
            player.isDodge = Random.Range(1, 101) <= dodge? true : false;
        }
        else if(!isStay)
        {
            player.isDodge = false;
        }
        
    }
}
