using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class OverUI : MonoBehaviour
{
    private static OverUI _instance;

    public static OverUI Instance { get { return _instance; } }
    private int frameCount;
    private int frameChecked;
    private bool frameCheckedResult;

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

    int UILayer;

    private void Start()
    {
        UILayer = LayerMask.NameToLayer("UI");
        frameCount = 0;
    }

    private void Update()
    {
        frameCount++;
    }

    public bool IsPointerOverUIElement()
    {
        if (frameCount != frameChecked) // In case two things want to check it on the same frame we can just cache it for the current frame
        {
            frameChecked = frameCount;
            frameCheckedResult = IsPointerOverUIElement(GetEventSystemRaycastResults(Input.mousePosition));
        }

        return frameCheckedResult;
    }

    public bool IsPointOverElement(Vector3 screenPosition)
    {
        return IsPointerOverUIElement(GetEventSystemRaycastResults(screenPosition));
    }


    //Returns 'true' if we touched or hovering on Unity UI element.
    private bool IsPointerOverUIElement(List<RaycastResult> eventSystemRaysastResults)
    {
        for (int index = 0; index < eventSystemRaysastResults.Count; index++)
        {
            RaycastResult curRaysastResult = eventSystemRaysastResults[index];
            if (curRaysastResult.gameObject.layer == UILayer)
                return true;
        }
        return false;
    }


    //Gets all event system raycast results of current mouse or touch position.
    static List<RaycastResult> GetEventSystemRaycastResults(Vector3 screenPosition)
    {
        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = screenPosition;
        List<RaycastResult> raysastResults = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, raysastResults);
        return raysastResults;
    }
}