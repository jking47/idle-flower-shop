using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Lightweight service locator for decoupled system access.
/// Managers register themselves on Awake, other systems retrieve via Services.Get<T>().
/// </summary>
public static class Services
{
    static readonly Dictionary<Type, object> registry = new();

    public static void Register<T>(T service) where T : class
    {
        var type = typeof(T);
        if (registry.ContainsKey(type))
        {
            Debug.LogWarning($"[Services] Overwriting existing registration for {type.Name}");
        }
        registry[type] = service;
    }

    public static T Get<T>() where T : class
    {
        var type = typeof(T);
        if (registry.TryGetValue(type, out var service))
        {
            return (T)service;
        }
        Debug.LogError($"[Services] No service registered for {type.Name}");
        return null;
    }

    public static bool TryGet<T>(out T service) where T : class
    {
        var type = typeof(T);
        if (registry.TryGetValue(type, out var obj))
        {
            service = (T)obj;
            return true;
        }
        service = null;
        return false;
    }

    /// <summary>
    /// Call on scene teardown or when resetting game state.
    /// </summary>
    public static void Clear()
    {
        registry.Clear();
    }
}
