using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FakePlayerSpawner : MonoBehaviour
{
    public GameObject fakePlayer;

    void Update()
    {
        if(Input.GetKeyDown(KeyCode.RightArrow))
        {
            StartCoroutine(spawner(Instantiate(fakePlayer)));
        }
    }

    IEnumerator spawner(GameObject ebnasdawd)
    {
        yield return new WaitForSeconds(1);

        ebnasdawd.SetActive(true);
    }
}
