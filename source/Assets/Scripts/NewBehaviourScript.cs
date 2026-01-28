using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Runtime.InteropServices;
using UnityOrbisBridge;

public class test : MonoBehaviour
{
    public Text Prueba1;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Joystick1Button3) ||
            Input.GetKeyDown(KeyCode.Keypad8))
        {
            Prueba1.text = "funcionando";
            UOB.TextNotify(1, "Todo bien!");
        }
    }
}