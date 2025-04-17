using System.Collections;
using System.Collections.Generic;
using FishNet.Object;
using FishNet.Connection;
using FishNet.Object.Synchronizing;
using FishNet.Component.Transforming;

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

    [SerializeField] private NetworkTransform networkTransform;
    private Vector3 targetDragPosition;
    private bool isDragging;

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

        if (!IsOwner)
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
        if (!alive) return;

        RequestTransferOwnershipForDragServerRpc();
        DisableMovement();
        isDragging = true;
        rb.gravityScale = 0f; // Disable gravity while dragging
    }

    public void UpdateDragPosition(Vector3 newPosition)
    {
        if (!isDragging) return;

        // Direct position update for more responsive dragging
        transform.position = newPosition;

    }

    public void EndDrag(Vector3 dragEndVelocity)
    {
        isDragging = false;
        rb.gravityScale = 1f; // Restore gravity
        RequestReturnOwnershipServerRpc(dragEndVelocity);
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestTransferOwnershipForDragServerRpc()
    {
        // Store original owner and transfer ownership to dragging client
        _originalOwner = Owner;
        NetworkObject.GiveOwnership(SteamPlayerManager.Instance.GetNetworkConnection(SteamLobbyManager.Instance.GetArtist())); // Give to host/artist
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestReturnOwnershipServerRpc(Vector3 dragEndVelocity)
    {
        if (_originalOwner == null) return;

        NetworkObject.GiveOwnership(_originalOwner);
        ApplyThrowVelocityObserversRpc(dragEndVelocity);
    }

    [ObserversRpc]
    private void ApplyThrowVelocityObserversRpc(Vector3 velocity)
    {
        EnableMovement(true);
        if (IsOwner)
        {
            rb.AddForce(velocity, ForceMode2D.Impulse);
        }
    }
}
