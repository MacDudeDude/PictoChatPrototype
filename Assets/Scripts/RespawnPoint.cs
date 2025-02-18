using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PrimeTween;

public class RespawnPoint : MonoBehaviour, IDraggable
{
    public float respawnDuration;
    public bool canRespawn;

    private static RespawnPoint _instance;
    public static RespawnPoint Instance { get { return _instance; } }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            _instance = this;
        }
    }

    public void QueRespawn(Player player, float extraDuration = 0)
    {
        StartCoroutine(RespawnPlayer(player, respawnDuration + extraDuration));
    }

    private IEnumerator RespawnPlayer(Player player, float duration)
    {
        bool positiveScale = player.transform.localScale.x >= 0;

        Tween.Scale(player.transform, 0, 0.1f);
        player.DisableMovement();

        while (duration > 0)
        {
            duration -= Time.deltaTime;
            yield return null;
        }

        while (!canRespawn)
        {
            yield return null;
        }

        player.transform.position = transform.position;
        player.transform.rotation = transform.rotation;

        yield return Tween.Scale(player.transform, new Vector3(positiveScale ? 0.6f : -0.6f, 0.6f, 0.6f), 1f).ToYieldInstruction();

        player.EnableMovement(false);

    }

    public bool CanDrag()
    {
        return canRespawn;
    }

    public void BeginDrag()
    {

    }

    public void EndDrag(Vector3 dragEndVelocity)
    {

    }
}
