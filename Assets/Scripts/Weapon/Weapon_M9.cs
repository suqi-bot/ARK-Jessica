using System.Collections.Generic;
using UnityEngine;


public class Weapon_M9 : Weapon_Base
{
    

    public override void Fire()
    {
        base.Fire();

        nowFirePos = firePosList[hitTime >= firePosList.Count ? firePosList.Count - 1 : hitTime];
        nowBullet = bulletList[hitTime >= bulletList.Count ? bulletList.Count - 1 : hitTime];
        if (hitTime == 2)
        {
            owner.GetComponent<CharacterBaseClass>().atk = atk * 2f;
        }
        BasePool.Instance.SetItem(nowBullet.gameObject, nowFirePos.transform);
        BasePool.Instance.GetPool(nowBullet.gameObject.name).Get();
        GameObject fire = Resources.Load<GameObject>("Bullet/BulletFx/FireFx");
        BasePool.Instance.SetItem(fire, nowFirePos.transform);
        BasePool.Instance.GetPool(fire.name).Get();
        //Instantiate(nowBullet.gameObject, nowFirePos.transform.position, nowFirePos.transform.rotation);

        nowClipAmmoNum--;
    }

    public override void Init(GameObject user, List<Transform> firePos)
    {
        base.Init(user, firePos);
        nowClipAmmoNum = maxClipAmmoNum;
        this.fireVector = new List<float>
        {
            0,
            5,
            -8
        };
    }

    

    public override void PrepareFire(int index)
    {

        base.PrepareFire(index);
        
        if (hitTime == 2)
        {
            audioSource.PlayOneShot(fireClip[1]);
        }
        else
        {
            audioSource.PlayOneShot(fireClip[0]);
        }

        

    }
}
