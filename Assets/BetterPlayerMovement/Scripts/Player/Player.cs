using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour, IKillable
{
    public Animator animator;
    public Transform spritesHolder;
    public int startingState;
    public PlayerState[] PlayerStates;
    public Rigidbody2D rb;
    public PlayerStateMachine StateMachine { get; set; }

    private bool movementEnabled;

    private void Awake()
    {
        StateMachine = new PlayerStateMachine();
        for (int i = 0; i < PlayerStates.Length; i++)
        {
            PlayerStates[i].Init(this, StateMachine);
        }
    }

    private void Start()
    {
        Kill();
        StateMachine.Initialize(PlayerStates[startingState]);
    }

    void Update()
    {
        if(movementEnabled)
            StateMachine.CurrentPlayerState.FrameUpdate();
    }

    private void FixedUpdate()
    {
        if(movementEnabled)
            StateMachine.CurrentPlayerState.PhysicsUpdate();
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if(collision.contactCount > 20)
        {
            Kill();
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Kill();
    }

    public void Kill()
    {
        if(RespawnPoint.Instance != null)
            RespawnPoint.Instance.QueRespawn(this);
    }

    public void DisableMovement()
    {
        movementEnabled = false;
        rb.simulated = false;
    }

    public void EnableMovement()
    {
        movementEnabled = true;
        rb.simulated = true;
    }
}
