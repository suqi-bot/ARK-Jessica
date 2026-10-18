using System;
using System.Collections.Generic;
using UnityEngine;



[Serializable]
public class BlackBoard
{
    
}

public class CharacterFSM
{
    public IState curState;
    public Dictionary<Enum, IState> states;
    public BlackBoard blackBoard;
    public CharacterFSM(BlackBoard blackBoard)
    {
        this.states = new Dictionary<Enum, IState>();
        this.blackBoard = blackBoard;
    }

    public void AddState(Enum stateType, IState state)
    {
        if (states.ContainsKey(stateType))
        {
            Debug.Log("状态无需重复添加");
            return;
        }
        states.Add(stateType, state);
    }

    public void SwitchState(Enum stateType) 
    {
        if (!states.ContainsKey(stateType))
        {
            Debug.Log("未知状态："+ stateType);
            return;
        }
        if (curState != null)
        {
            curState.OnExit();
        }
        curState = states[stateType];
        curState.OnEnter();
    }

    public void OnUpdate()
    {
        curState.OnUpdate();
    }



}
