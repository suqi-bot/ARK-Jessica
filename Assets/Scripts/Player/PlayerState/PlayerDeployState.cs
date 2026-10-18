using UnityEngine;

public class PlayerDeployState : IState
{
    private CharacterFSM fsm;
    private PlayerBlackboard blackboard;
    private float time;
    private AudioSource audioSource;
    public PlayerDeployState()
    {
        fsm = PlayerStateMgr.Instance.getFsm();
        blackboard = fsm.blackBoard as PlayerBlackboard;
    }

    public void OnEnter()
    {
        blackboard.player.isInvincible = true;
        time = 0.9f;
        blackboard.player.groundCheck.DeployAudio();
        blackboard.player.animator.Play("Player_Start");
        blackboard.player.nowState = Player_State_Type.Deploy;
    }

    public void OnExit()
    {
        blackboard.player.isInvincible = false;
    }

    public void OnUpdate()
    {
        time -= Time.deltaTime;
        if (time < 0)
        {
            fsm.SwitchState(Player_State_Type.Idle);
        }
    }

    
}
