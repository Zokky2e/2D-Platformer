using System;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;
using UnityEngine.InputSystem;

class Armor : MonoBehaviour
{
    public float armor;

    private Hero player;

    public void Awake()
    {
        player = this.GetComponent<Hero>();
    }
}
