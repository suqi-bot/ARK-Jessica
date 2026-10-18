using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerShootEvent : MonoBehaviour
{
    PlayerBlackboard blackboard;
    public bool isShoot {  get; set; }
    public bool isFinsh { get; set; }
    private void Start()
    {
        blackboard = PlayerStateMgr.Instance.getFsm().blackBoard as PlayerBlackboard;
        
    }


    public void Attack()
    {
        isShoot = true;
    }

    public void FinishAttack()
    {
        isFinsh = true;
    }
}
