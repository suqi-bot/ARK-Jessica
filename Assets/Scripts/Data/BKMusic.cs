using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;

public class BKMusic : MonoBehaviour
{
    private static BKMusic instance;
    public static BKMusic Instance => instance;
    private AudioSource audioSource;
    private void Awake()
    {
        instance = this;
        audioSource = this.gameObject.GetComponent<AudioSource>();

        //获取音量数据
        AudioData data = GameDataMgr.Instance.audioData;
        SetIsOpen(data.musicOpen);
        ChangeValue(data.musicValue);

        Invoke("StormInit", 0.1f);
    }
   
    // 开关音乐方法
    public void SetIsOpen(bool isOpen)
    {
        audioSource.mute = !isOpen;
    }
    // 调整音乐大小方法
    public void ChangeValue(float v)
    {
        audioSource.volume = v;
    }

    public void PlayMusic(AudioClip clip)
    {
        audioSource.clip = clip;
        audioSource.Play();
    }
    public void PlayTime(float time)
    {
        audioSource.time = time;
    }

    public void StormInit()
    {
        AudioData data = GameDataMgr.Instance.audioData;
        this.isStorm = data.soundOpen;
        this.stormValue = data.soundValue;
        SetStorm(data.soundValue, data.soundOpen);



    }
    bool isStorm;
    float stormValue;
    public void SetStormValue(float v)
    {
        this.stormValue = v;
        SetStorm(v, isStorm);
    }

    private void SetStorm(float v, bool isopen)
    {
        if (UniStormManager.Instance == null)
        {
            return;
        }
        if (isopen)
        {
            //2.设置UniStorm的环境音量值从0(完全静音)到1(全音量)。  
            UniStormManager.Instance.SetAmbienceVolume(v);
            //3.将UniStorm的天气音量设置为0(全静音)到1(全音量)。  
            UniStormManager.Instance.SetWeatherVolume(v);
        }
        else
        {
            //2.设置UniStorm的环境音量值从0(完全静音)到1(全音量)。  
            UniStormManager.Instance.SetAmbienceVolume(0);
            //3.将UniStorm的天气音量设置为0(全静音)到1(全音量)。  
            UniStormManager.Instance.SetWeatherVolume(0);
        }
    }

    public void SetStormIsOpen(bool isOpen)
    {
        this.isStorm = isOpen;
        SetStorm(stormValue, this.isStorm);
    }

}
