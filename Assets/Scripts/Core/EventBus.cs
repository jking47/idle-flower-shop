using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Type-based event bus for decoupled cross-system communication.
/// Wraps subscriber invocations in try/catch so one broken subscriber
/// cannot prevent other subscribers from receiving the event.
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
        if (!listeners.TryGetValue(type, out var existing)) return;

        // Invoke each subscriber individually so one exception
        // doesn't kill delivery to remaining subscribers
        var invocationList = existing.GetInvocationList();
        foreach (var del in invocationList)
        {
            try
            {
                ((Action<T>)del).Invoke(evt);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }

    public static void Clear()
    {
        listeners.Clear();
    }
}