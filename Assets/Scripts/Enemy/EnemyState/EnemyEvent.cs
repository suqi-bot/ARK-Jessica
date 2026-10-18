using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyEvent : MonoBehaviour
{
    public bool isAttack;
    public bool isOver;

    public void OnAttack()
    {
        isAttack = true;
    }

    public void OnAttackOver()
    {
        isOver = true;
    }
}
