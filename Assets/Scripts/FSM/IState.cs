using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ЧґМ¬№ЬАн
/// </summary>
public interface IState
{
    void OnEnter();
    void OnUpdate();
    void OnExit();

}
