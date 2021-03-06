﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Health : MonoBehaviour
{
    [SerializeField] private int maxHealth = 10;
    public int CurHP { get; private set; }

    private void Start()
    {
        CurHP = maxHealth;
    }

    public void TakeDamage(int amount, Vector3 dmgSrc)
    {
        if (amount < 0)
        {
            Debug.LogError("ERROR - Health damage amount was < 0. All damage must be positive to be applied.");
            return;
        }
        if (amount > 0)
        {
            Debug.Log(gameObject.name + " was damaged! (" + amount + "). Remainder " + (CurHP - amount) + ".");
        }

        CurHP -= amount;

        if (CurHP <= 0) Destroy(gameObject);
    }
}