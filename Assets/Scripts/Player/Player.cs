using System.Collections;
using System.Collections.Generic;
using FishNet.Object;
using FishNet.Connection;
using FishNet.Object.Synchronizing;

using UnityEngine;
using Steamworks;

public class Player : NetworkBehaviour, IKillable, IDraggable
{

    public Animator animator;
    public Transform spritesHolder;
    public int startingState;
    public PlayerState[] PlayerStates;
    public Rigidbody2D rb;
    public PlayerStateMachine StateMachine { get; set; }


    private float killTimer;
    private bool alive = true;
    private bool movementEnabled;

    // Field to store the original owner's clientId.
    private NetworkConnection _originalOwner = null;

    public override void OnStartServer()
    {
        base.OnStartServer();
        _originalOwner = Owner;
    }
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
        if (!IsOwner)
            return;

        if (movementEnabled)
            StateMachine.CurrentPlayerState.FrameUpdate();

        HandleSelfDestruct();
    }

    private void FixedUpdate()
    {
        if (!IsOwner)
            return;

        if (movementEnabled)
            StateMachine.CurrentPlayerState.PhysicsUpdate();
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.contactCount > 20)
        {
            Kill();
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Kill();
    }

    private void HandleSelfDestruct()
    {
        if (alive && Input.GetKey(KeyCode.R))
        {
            killTimer += Time.deltaTime;
        }
        else
        {
            killTimer -= Time.deltaTime;
        }

        if (killTimer >= 2)
            Kill();

        killTimer = Mathf.Clamp(killTimer, 0, 2);
    }

    public void Kill()
    {
        if (!alive)
            return;

        alive = false;
        if (RespawnPoint.Instance != null)
            RespawnPoint.Instance.QueRespawn(this);
    }

    public void DisableMovement()
    {
        rb.velocity = Vector2.zero;
        movementEnabled = false;
        rb.simulated = false;
    }

    public void EnableMovement(bool onlyIfAlive)
    {
        if (onlyIfAlive && !alive)
            return;

        StateMachine.CurrentPlayerState.EnterState();
        movementEnabled = true;
        rb.simulated = true;
        alive = true;
    }

    public bool CanDrag()
    {
        return alive;
    }

    public void BeginDrag()
    {
        DisableMovement();
        RequestTransferOwnershipForDragServerRpc();
    }

    public void EndDrag(Vector3 dragEndVelocity)
    {
        rb.velocity = dragEndVelocity;
        EnableMovement(true);
        //RequestReturnOwnershipServerRpc();
    }
    /// <summary>
    /// Transfers ownership of this Player to a new owner.
    /// This method can be called from a non-owner via RPC.
    /// </summary>
    /// <param name="newOwner">The clientId of the new owner (dragging client).</param>
    [ServerRpc(RequireOwnership = false)]
    public void RequestTransferOwnershipForDragServerRpc()
    {
        Debug.Log("[Player] Request Transfer Ownership For Drag ServerRpc");
        _originalOwner = Owner;
        Debug.Log("[Player] Original Owner: " + _originalOwner);
        NetworkObject.RemoveOwnership();
    }

    /// <summary>
    /// Returns ownership of this Player to the original owner.
    /// This method can be called from a non-owner via RPC.
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void RequestReturnOwnershipServerRpc()
    {
        if (_originalOwner != null)
        {
            NetworkObject.GiveOwnership(_originalOwner);
            Debug.Log("[Player] Giving back ownership to original Owner: " + _originalOwner);
        }
    }
    [ServerRpc]
    public void DragUpdateServerRpc(Vector3 newPosition, float deltaTime)
    {
        // Set the new position on the server.
        transform.position = newPosition;

    }

}
