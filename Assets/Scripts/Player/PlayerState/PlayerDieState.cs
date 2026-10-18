public class PlayerDieState : IState
{
    private CharacterFSM fsm;
    private PlayerBlackboard blackboard;

    public PlayerDieState()
    {
        fsm = PlayerStateMgr.Instance.getFsm();
        blackboard = fsm.blackBoard as PlayerBlackboard;
    }
    public void OnEnter()
    {
        blackboard.player.animator.Play("Player_Did");
        blackboard.player.groundCheck.DieAudio();
        blackboard.player.nowState = Player_State_Type.Die;
    }

    public void OnExit()
    {
        
    }

    public void OnUpdate()
    {
        
    }

}
