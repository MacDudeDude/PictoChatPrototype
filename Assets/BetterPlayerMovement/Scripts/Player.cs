using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
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

        if(Input.GetKeyDown(KeyCode.Z))
        {
            StateMachine.ChangeState(PlayerStates[Random.Range(0, 2)]);
        }
    }

    private void FixedUpdate()
    {
        StateMachine.CurrentPlayerState.PhysicsUpdate();
    }
}
