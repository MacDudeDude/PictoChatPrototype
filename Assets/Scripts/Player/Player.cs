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
    public Transform grabBox;
    public Transform playerMessageHolder;
    public GameObject playerMessagePrefab;
    public float messagePopupDuration;

    private GameObject lastMessageObject;
    private Coroutine messageCoroutine;

    private float killTimer;
    private bool alive = true;
    private bool movementEnabled;


    // Field to store the original owner's clientId.
    private NetworkConnection _originalOwner = null;
    public PlayerCosmetics playerCosmetics;

    [SerializeField] private NetworkTransform networkTransform;
    private Vector3 targetDragPosition;
    private bool isDragging;

    public override void OnStartServer()
    {
        base.OnStartServer();
        _originalOwner = Owner;
        Debug.Log("[Player] OnStartServer - Original owner set: " + Owner?.ClientId);
    }
    private void Awake()
    {
        StateMachine = new PlayerStateMachine();
        for (int i = 0; i < PlayerStates.Length; i++)
        {
            PlayerStates[i].Init(this, StateMachine);
        }
        Debug.Log("[Player] Awake - State machine initialized with " + PlayerStates.Length + " states");
    }


    private void Start()
    {
        Kill();
        StateMachine.Initialize(PlayerStates[startingState]);
        networkTransform = GetComponent<NetworkTransform>();
        playerCosmetics = GetComponent<PlayerCosmetics>();

        ChatReciever.Instance.OnChatMessageReceived += OnChatReceived;
        playerCosmetics.ApplyCosmetics();
        Debug.Log("[Player] Start - Initialized with state: " + PlayerStates[startingState].GetType().Name);
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
            Debug.Log("[Player] OnCollisionStay2D - Too many contacts (" + collision.contactCount + "), killing player");
            Kill();
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Debug.Log("[Player] OnTriggerEnter2D - Triggered by " + collision.gameObject.name + ", killing player");
        Kill();
    }

    private void HandleSelfDestruct()
    {
        if (alive && Input.GetKey(KeyCode.R))
        {
            killTimer += Time.deltaTime;
            if (killTimer > 1.5f)
                Debug.Log("[Player] Self-destruct imminent: " + killTimer.ToString("F1") + "/2.0");
        }
        else
        {
            killTimer -= Time.deltaTime;
        }

        if (killTimer >= 2)
        {
            Debug.Log("[Player] Self-destruct activated");
            Kill();
        }

        killTimer = Mathf.Clamp(killTimer, 0, 2);
    }

    public void Kill()
    {
        if (!alive)
        {
            Debug.Log("[Player] Kill attempted but player already dead");
            return;
        }

        if (!IsOwner)
        {
            Debug.Log("[Player] Kill attempted but not owner");
            return;
        }

        Debug.Log("[Player] Player killed");
        alive = false;
        if (RespawnPoint.Instance != null)
            RespawnPoint.Instance.QueRespawn(this);
    }

    public void DisableMovement()
    {
        Debug.Log("[Player] Movement disabled");
        rb.velocity = Vector2.zero;
        movementEnabled = false;
        rb.simulated = false;
    }

    public void EnableMovement(bool onlyIfAlive)
    {
        if (onlyIfAlive && !alive)
        {
            Debug.Log("[Player] EnableMovement called but player not alive");
            return;
        }

        Debug.Log("[Player] Movement enabled");
        StateMachine.CurrentPlayerState.EnterState();
        movementEnabled = true;
        rb.simulated = true;
        alive = true;

        grabBox.position = transform.position;

    }

    public void OnChatReceived(Texture2D message, string textMessage, bool playerPopup, NetworkConnection connection)
    {
        if (connection == Owner)
        {
            if (playerPopup)
            {
                if (lastMessageObject != null)
                {
                    StopCoroutine(messageCoroutine);
                    lastMessageObject = null;
                }

                lastMessageObject = Instantiate(playerMessagePrefab, transform.position, Quaternion.identity, playerMessageHolder);

                TMPro.TMP_Text[] messageText = lastMessageObject.GetComponentsInChildren<TMPro.TMP_Text>();
                messageText[0].text = "";
                if (!string.IsNullOrEmpty(textMessage))
                {
                    messageText[1].text = textMessage;
                }
                lastMessageObject.GetComponentInChildren<UnityEngine.UI.RawImage>().texture = message;

                messageCoroutine = StartCoroutine(ChatMessageDestroyTimer());
            }
        }
    }

    private IEnumerator ChatMessageDestroyTimer()
    {
        yield return new WaitForSeconds(messagePopupDuration);

        Destroy(lastMessageObject);
        lastMessageObject = null;
    }

    public bool CanDrag()
    {
        Debug.Log("[Player] CanDrag check: " + alive);
        return alive;
    }

    public void BeginDrag()
    {
        if (!alive)
        {
            Debug.Log("[Player] BeginDrag called but player not alive");
            return;
        }

        Debug.Log("[Player] Beginning drag, transferring ownership");
        animator.SetBool("Dragging", true);
        TransferOwnerDragging();
        DisableMovement();
        TransferOwnerDragging();
        isDragging = true;
        rb.gravityScale = 0f; // Disable gravity while dragging
    }

    public void UpdateDragPosition(Vector3 newPosition)
    {
        if (!isDragging)
        {
            Debug.Log("[Player] UpdateDragPosition called but not dragging");
            return;
        }

        transform.position = newPosition;
        networkTransform.ForceSend();
    }

    public void EndDrag(Vector3 dragEndVelocity)
    {
        Debug.Log("[Player] Ending drag with velocity: " + dragEndVelocity);


        animator.SetBool("Dragging", false);
        isDragging = false;
        rb.gravityScale = 2f;
        rb.AddForce(dragEndVelocity, ForceMode2D.Impulse);
        StartCoroutine(WaitForEndVelocity());

    }

    private IEnumerator WaitForEndVelocity()
    {
        while (rb.velocity.magnitude > 0.1f)
        {
            networkTransform.ForceSend();
            yield return new WaitForSeconds(0.1f);
        }
        ReturnOwnership();
        EnableMovement(true);
    }


    [ServerRpc(RequireOwnership = false)]
    private void TransferOwnerDragging()
    {
        // Store original owner and transfer ownership to dragging client
        Debug.Log("[Player] Server: Transferring ownership from " + _originalOwner?.ClientId);
        NetworkObject.GiveOwnership(SteamPlayerManager.Instance.GetNetworkConnection(SteamLobbyManager.Instance.GetArtist()));
    }

    [ServerRpc(RequireOwnership = false)]
    private void ReturnOwnership()
    {
        if (_originalOwner == null)
        {
            Debug.Log("[Player] Server: Cannot return ownership, original owner is null");
            return;
        }

        Debug.Log("[Player] Server: Returning ownership to " + _originalOwner.ClientId);
        NetworkObject.GiveOwnership(_originalOwner);
    }


}
