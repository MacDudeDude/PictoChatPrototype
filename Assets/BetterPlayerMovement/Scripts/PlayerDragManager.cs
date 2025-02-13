using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerDragManager : MonoBehaviour
{
    public Camera cam;
    public MouseManager grabber;
    public Player grabbedPlayer;
    public bool canDrag;

    private Vector3 lastPos;

    private void Awake()
    {
        cam = Camera.main;
    }

    private void Update()
    {
        if(Input.GetMouseButtonDown(0))
        {
            GameObject clickedOn = grabber.GetHoveredObject();
            if(clickedOn != null)
            {
                if(clickedOn.layer == 10 || clickedOn.layer == 11)
                {
                    grabbedPlayer = clickedOn.GetComponent<Player>();
                    grabbedPlayer.DisableMovement();
                    lastPos = grabbedPlayer.transform.position;
                }
            }
        }

        if(grabbedPlayer != null)
        {
            if(!grabbedPlayer.IsAlive())
            {
                grabbedPlayer = null;
            }else
            {
                Vector2 newPos = cam.ScreenToWorldPoint(Input.mousePosition);
                grabbedPlayer.transform.position = Vector3.Lerp(grabbedPlayer.transform.position, newPos, Time.deltaTime * 30);
            }
        }

        if(Input.GetMouseButtonUp(0))
        {
            if(grabbedPlayer != null)
            {
                grabbedPlayer.rb.velocity = (grabbedPlayer.transform.position - lastPos) / Time.deltaTime;
                grabbedPlayer.EnableMovement(true);
                grabbedPlayer = null;
            }
        }

        if(grabbedPlayer != null)
            lastPos = grabbedPlayer.transform.position;
    }
}
