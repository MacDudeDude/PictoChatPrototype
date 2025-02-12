using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour, IKillable
{
    public Animator animator;
    public Transform spritesHolder;
    public int startingState;
    public PlayerState[] PlayerStates;
    public PlayerStateMachine StateMachine { get; set; }

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
        StateMachine.Initialize(PlayerStates[startingState]);
    }

    void Update()
    {
        StateMachine.CurrentPlayerState.FrameUpdate();
    }

    private void FixedUpdate()
    {
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
        transform.position = new Vector3(10, 10, 0);
    }
}
