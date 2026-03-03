using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Type-based event bus for decoupled cross-system communication.
/// 
/// Usage:
///   // Define an event struct
///   public struct CurrencyChangedEvent { public CurrencyType type; public double amount; }
///
///   // Subscribe (typically in OnEnable)
///   EventBus.Subscribe<CurrencyChangedEvent>(OnCurrencyChanged);
///
///   // Unsubscribe (typically in OnDisable)
///   EventBus.Unsubscribe<CurrencyChangedEvent>(OnCurrencyChanged);
///
///   // Publish from anywhere
///   EventBus.Publish(new CurrencyChangedEvent { type = CurrencyType.Petals, amount = 150 });
/// </summary>
public static class EventBus
{
    static readonly Dictionary<Type, Delegate> listeners = new();

    public static void Subscribe<T>(Action<T> callback) where T : struct
    {
        var type = typeof(T);
        if (listeners.TryGetValue(type, out var existing))
        {
            listeners[type] = Delegate.Combine(existing, callback);
        }
        else
        {
            listeners[type] = callback;
        }
    }

    public static void Unsubscribe<T>(Action<T> callback) where T : struct
    {
        var type = typeof(T);
        if (listeners.TryGetValue(type, out var existing))
        {
            var result = Delegate.Remove(existing, callback);
            if (result == null)
                listeners.Remove(type);
            else
                listeners[type] = result;
        }
    }

    public static void Publish<T>(T evt) where T : struct
    {
        var type = typeof(T);
        if (listeners.TryGetValue(type, out var existing))
        {
            ((Action<T>)existing)?.Invoke(evt);
        }
    }

    public static void Clear()
    {
        listeners.Clear();
    }
}
