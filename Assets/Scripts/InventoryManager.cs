using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tracks harvested flower quantities by FlowerData asset name.
/// Attach to GameManager object.
/// 
/// Notifies via both:
///   - C# Action OnInventoryChanged (for direct subscribers e.g. UI components)
///   - EventBus InventoryChangedEvent (for decoupled systems e.g. ShopPanel)
/// </summary>
public class InventoryManager : MonoBehaviour
{
    readonly Dictionary<string, int> stock = new();

    public event Action OnInventoryChanged;

    void Awake() => Services.Register(this);

    void OnEnable() => EventBus.Subscribe<FlowerHarvestedEvent>(OnFlowerHarvested);
    void OnDisable() => EventBus.Unsubscribe<FlowerHarvestedEvent>(OnFlowerHarvested);

    void OnFlowerHarvested(FlowerHarvestedEvent evt)
    {
        if (evt.flowerData != null)
            Add(evt.flowerData.name, 1);
    }

    public void Add(string flowerName, int amount)
    {
        if (!stock.ContainsKey(flowerName)) stock[flowerName] = 0;
        stock[flowerName] += amount;
        Notify(flowerName);
    }

    public int GetCount(string flowerName) =>
        stock.TryGetValue(flowerName, out int n) ? n : 0;

    /// <summary>
    /// Remove flowers from inventory. Returns false if insufficient stock.
    /// </summary>
    public bool Remove(string flowerName, int amount)
    {
        if (GetCount(flowerName) < amount) return false;

        stock[flowerName] -= amount;
        if (stock[flowerName] <= 0) stock.Remove(flowerName);

        Notify(flowerName);
        return true;
    }

    public Dictionary<string, int> GetAllStock() => new(stock);

    // --- Internal ---

    void Notify(string flowerName)
    {
        OnInventoryChanged?.Invoke();

        // Resolve FlowerData for the EventBus payload.
        // GardenManager holds the canonical flower list — look up by asset name.
        FlowerData flower = null;
        Services.TryGet<GardenManager>(out var garden);
        if (garden != null)
        {
            foreach (var f in garden.AvailableFlowers)
            {
                if (f.name == flowerName) { flower = f; break; }
            }
        }

        EventBus.Publish(new InventoryChangedEvent
        {
            flower = flower,       // null if flower not found in garden list (safe — UI checks for null)
            newCount = stock.TryGetValue(flowerName, out int n) ? n : 0
        });
    }

    // --- Save/Load ---

    public List<InventorySaveEntry> GetSaveData()
    {
        var data = new List<InventorySaveEntry>();
        foreach (var kvp in stock)
            data.Add(new InventorySaveEntry { flowerName = kvp.Key, count = kvp.Value });
        return data;
    }

    public void LoadSaveData(List<InventorySaveEntry> data)
    {
        stock.Clear();
        if (data == null) return;
        foreach (var entry in data)
        {
            if (entry.count > 0)
                stock[entry.flowerName] = entry.count;
        }
        OnInventoryChanged?.Invoke();
        // No EventBus publish on load — no FlowerData refs available yet at load time.
        // ShopPanel does a full RefreshAllSlots() after load anyway.
    }
}

[Serializable]
public class InventorySaveEntry
{
    public string flowerName;
    public int count;
}
