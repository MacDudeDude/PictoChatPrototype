using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RespawnPoint : MonoBehaviour
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
        player.EnableMovement();
    }
}
