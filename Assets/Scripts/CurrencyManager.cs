using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tracks all currency balances. Uses double to handle large idle numbers
/// without floating point breakdown at lower values.
/// </summary>
public class CurrencyManager : MonoBehaviour
{
    readonly Dictionary<CurrencyType, double> balances = new();

    void Awake()
    {
        Services.Register(this);

        // Initialize all currencies to zero
        foreach (CurrencyType type in Enum.GetValues(typeof(CurrencyType)))
        {
            balances[type] = 0;
        }
    }

    public double GetBalance(CurrencyType type) => balances[type];

    public bool CanAfford(CurrencyType type, double cost) => balances[type] >= cost - 0.001;

    /// <summary>
    /// Add currency. Returns new balance. Use for harvests, sales, rewards.
    /// </summary>
    public double Add(CurrencyType type, double amount)
    {
        if (amount <= 0)
        {
            Debug.LogWarning($"[Currency] Tried to add non-positive amount {amount} to {type}");
            return balances[type];
        }

        double prev = balances[type];
        balances[type] += amount;

        EventBus.Publish(new CurrencyChangedEvent
        {
            currencyType = type,
            previousAmount = prev,
            newAmount = balances[type]
        });

        return balances[type];
    }

    /// <summary>
    /// Spend currency. Returns true if successful, false if insufficient funds.
    /// </summary>
    public bool Spend(CurrencyType type, double cost)
    {
        if (cost <= 0)
        {
            Debug.LogWarning($"[Currency] Tried to spend non-positive amount {cost} of {type}");
            return false;
        }

        if (!CanAfford(type, cost)) return false;

        double prev = balances[type];
        balances[type] -= cost;

        EventBus.Publish(new CurrencyChangedEvent
        {
            currencyType = type,
            previousAmount = prev,
            newAmount = balances[type]
        });

        return true;
    }

    /// <summary>
    /// Set balance directly. Used by save/load system.
    /// </summary>
    public void SetBalance(CurrencyType type, double amount)
    {
        double prev = balances[type];
        balances[type] = Math.Max(0, amount);

        EventBus.Publish(new CurrencyChangedEvent
        {
            currencyType = type,
            previousAmount = prev,
            newAmount = balances[type]
        });
    }
}