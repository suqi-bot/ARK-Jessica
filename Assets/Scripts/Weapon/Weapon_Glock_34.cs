using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Weapon_Glock_34 : Weapon_Base
{
    public override void Fire()
    {
        base.Fire();
        Instantiate(nowBullet.gameObject, nowFirePos.transform.position, nowFirePos.transform.rotation);
        nowClipAmmoNum--;
    }

    public override void Init(GameObject user, List<Transform> firePos)
    {
        base.Init(user, firePos);
        // 设置子弹拥有者
        owner = user;
        nowBullet.owner = user;
        // 设置子弹攻击力
        owner.GetComponent<CharacterBaseClass>().atk = atk;
        // 设置发射点位置
        this.firePosList = firePos;
        // 设置子弹口径
        this.bulletCaliber = "9x19mm";
        // 是否可以持续射击
        this.isContinuous = false;
    }

    public override void PrepareFire(int index)
    {
        base.PrepareFire(index);
    }
}
