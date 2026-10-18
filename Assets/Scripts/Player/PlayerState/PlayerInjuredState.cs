using UnityEngine;

public class PlayerInjuredState : IState
{
    private CharacterFSM fsm;
    private PlayerBlackboard blackboard;

    public PlayerInjuredState()
    {
        fsm = PlayerStateMgr.Instance.getFsm();
        blackboard = fsm.blackBoard as PlayerBlackboard;
    }
    public void OnEnter()
    {
        blackboard.player.nowState = Player_State_Type.Injured;
        blackboard.player.animator.Play("Player_Injured");
    }

    public void OnExit()
    {
        
        
    }
    bool isFinish;
    public void OnUpdate()
    {
        isFinish = blackboard.player.shootEvent.isFinsh;
        Debug.Log(" ‹…À◊¥Ã¨"+isFinish);
        if (isFinish)
        {
            blackboard.player.shootEvent.isFinsh = false;
            fsm.SwitchState(Player_State_Type.Idle);
        }
       

       
        
    }

   
}
