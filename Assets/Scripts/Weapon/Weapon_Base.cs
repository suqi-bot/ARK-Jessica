using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public enum Weapon_Type
{
    M9_Pistol,
    Glock34,
    TTI_MPX
}


public abstract class Weapon_Base : MonoBehaviour
{
    protected ObjectPool<GameObject> pool;
    // 子弹发射位置
    [SerializeField] protected List<Transform> firePosList;
    // 子弹类型
    [SerializeField] protected List<Bullet> bulletList;
    // 当前子弹
    protected Transform nowFirePos;
    protected Bullet nowBullet;
    // 当前射击位置

    [Header("枪械属性")]
    [SerializeField] protected GameObject owner;
    // 子弹口径
    [SerializeField] protected string bulletCaliber;
    // 当前弹夹最大子弹数
    [SerializeField] public int maxClipAmmoNum;
    // 当前弹夹中子弹的数量
    [SerializeField] public int nowClipAmmoNum;
    // 最大后备子弹数
    [SerializeField] protected int maxBackAmmoNum;
    // 目前子弹的数量
    [SerializeField] protected int nowBackAmmoNum;
    // 攻击力
    [SerializeField] public float atk;
    // 射击Cd
    [SerializeField] public float atkCd;
    // 换弹时间
    [SerializeField] public float reLoadCd;
    // 射击次数
    protected int hitTime;
    // 是否可以持续射击
    [SerializeField] public bool isContinuous;
    // 开火音效
    [SerializeField] protected AudioClip[] fireClip;
    // 换弹音效
    [SerializeField] protected AudioClip reloadClip;
    // 射击位移
    public List<float> fireVector;
    // 音效管理
    [SerializeField] protected AudioSource audioSource;
    

    public virtual void Init(GameObject user, List<Transform> firePos)
    {
        // 设置射击位置
        firePosList = firePos;
        nowFirePos = firePosList[0];
        nowBullet = bulletList[0];
        nowBackAmmoNum = maxBackAmmoNum;
        // 设置子弹拥有者
        owner = user;
        nowBullet.owner = user;
        BasePool.Instance.SetItem(nowBullet.gameObject, nowFirePos.transform);
        // 设置对象池
        pool = BasePool.Instance.GetPool(nowBullet.gameObject.name);
        // 设置音效资源
        audioSource = firePos[0].GetComponent<AudioSource>();
       
        
    }
    // 开火
    public virtual void Fire() 
    {
        owner.GetComponent<CharacterBaseClass>().atk = atk;
    }

    // 是否可以换弹
    public bool CanReLoad()
    {
        if (maxClipAmmoNum == nowClipAmmoNum || nowBackAmmoNum == 0)
        {
            return false;
        }

        return true;
    }
    // 换弹
    public virtual void ReLoad()
    {
        

        if (maxClipAmmoNum > nowBackAmmoNum + nowClipAmmoNum)
        {
            nowClipAmmoNum = nowBackAmmoNum;
            nowBackAmmoNum = 0;
            
        }
        else
        {
            nowBackAmmoNum += nowClipAmmoNum - maxClipAmmoNum;
            nowClipAmmoNum = maxClipAmmoNum;
            
        }
        
    }

    public void ReLoadAudio()
    {
        if (audioSource != null && reloadClip != null)
        {
            audioSource.clip = reloadClip;
            audioSource.volume = GameDataMgr.Instance.audioData.soundValue;
            audioSource.mute = !GameDataMgr.Instance.audioData.soundOpen;
            audioSource.Play();

        }
    }

    public List<Transform> GetFirePos()
    {
        return firePosList;
    }

    public List<Bullet> GetBullets()
    {
        return bulletList;
    }

    public virtual void PrepareFire(int index)
    {
        audioSource.mute = !GameDataMgr.Instance.audioData.soundOpen;
        audioSource.volume = GameDataMgr.Instance.audioData.soundValue;
        hitTime = index;
    }

    


}
