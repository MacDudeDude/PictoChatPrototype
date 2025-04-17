using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class ButtonToggler : MonoBehaviour
{
    public UnityEvent enabledToggleEvent;
    public UnityEvent disabledToggleEvent;
    public bool toggled;

    public void Toggle()
    {
        toggled = !toggled;

        if (toggled)
            enabledToggleEvent.Invoke();
        else
            disabledToggleEvent.Invoke();
    }
}
