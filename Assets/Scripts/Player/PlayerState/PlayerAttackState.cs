using UnityEngine;

public class PlayerAttackState : IState
{
    private CharacterFSM fsm;
    private PlayerBlackboard blackboard;
    private bool isShoot;
    private bool isFinsh;
    private int hitTime;
    private float fireVector;
    public PlayerAttackState()
    {
        fsm = PlayerStateMgr.Instance.getFsm();
        blackboard = fsm.blackBoard as PlayerBlackboard;
    }
    public void OnEnter()
    {
        if (hitTime > 2 || blackboard.player.fireNumCd <= 0)
            hitTime = 0;
        blackboard.player.weapon.PrepareFire(hitTime);
        blackboard.player.animator.Play("Player_Attack" + (hitTime + 1),0, 0);
        blackboard.player.animator.speed = 1 + blackboard.player.weapon.atkCd * 0.1f;
        fireVector = blackboard.player.weapon.fireVector[hitTime];
        nextAttack = 0;
        hitTime++;

        blackboard.player.nowState = Player_State_Type.Attack;
    }
    public void OnExit()
    {
        blackboard.player.animator.speed = 1;
    }
    bool fireKey;
    float nextAttack;
    public void OnUpdate()
    {
        nextAttack -= Time.unscaledDeltaTime;
        isShoot = blackboard.player.shootEvent.isShoot;
        isFinsh = blackboard.player.shootEvent.isFinsh;
        fireKey = Input.GetButtonDown(blackboard.player.fireButton);
        if (!isShoot && fireKey)
        {
            nextAttack = .4f;
        }
        if (isShoot)
        {
            blackboard.player.Attack();
            blackboard.player.shootEvent.isShoot = false;
        }
        blackboard.player.DoAttackMove(fireVector);
        //Debug.Log(isShoot);
        
        
        if(blackboard.player && isFinsh)
        {
            if (nextAttack > 0 && blackboard.player.weapon.nowClipAmmoNum > 0)
                fsm.SwitchState(Player_State_Type.Attack);
            else
                fsm.SwitchState(Player_State_Type.Idle);
            blackboard.player.shootEvent.isFinsh = false;
        }
    }
}
