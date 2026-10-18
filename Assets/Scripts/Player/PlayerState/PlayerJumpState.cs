using UnityEngine;

public class PlayerJumpState : IState
{
    private CharacterFSM fsm;
    private PlayerBlackboard blackboard;
    private float lateUpdate;
    public PlayerJumpState()
    {
        fsm = PlayerStateMgr.Instance.getFsm();
        blackboard = fsm.blackBoard as PlayerBlackboard;
    }

    public void OnEnter()
    {
        lateUpdate = .2f;
        blackboard.player.Jump();
        blackboard.player.animator.Play("Player_Jump", 0, 0f);
        blackboard.player.groundCheck.JumpUpAudio();

        blackboard.player.nowState = Player_State_Type.Jump;
    }

    public void OnExit()
    {
        blackboard.player.groundCheck.JumpDownAudio();
    }
    bool isGrounded;
    float wolfTime;
    public void OnUpdate()
    {
        lateUpdate -= Time.deltaTime;
        wolfTime -= Time.deltaTime;
        if (lateUpdate < 0)
        {
            isGrounded = blackboard.player.isGrounded;
            blackboard.player.XMove();
            if (Input.GetButtonDown(blackboard.player.jumpButton))
                wolfTime = .2f;
            if (isGrounded && wolfTime > 0)
            {
                wolfTime = 0;
                fsm.SwitchState(Player_State_Type.Jump);
            }
            if (Input.GetAxis(blackboard.player.horizontalAxis) == 0 && isGrounded && wolfTime < 0)
                fsm.SwitchState(Player_State_Type.Idle);
            else if (Input.GetAxis(blackboard.player.horizontalAxis) != 0 && isGrounded && wolfTime < 0)
                fsm.SwitchState(Player_State_Type.Move);
        }
    }
}
