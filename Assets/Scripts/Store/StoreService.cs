using System;
using System.Collections.Generic;

/// <summary>
/// Interface for IAP operations. Mirrors real store SDK patterns.
/// </summary>
public interface IStoreService
{
    void GetProducts(Action<List<StoreProduct>> callback);
    void Purchase(string productId, Action<bool, string> callback);
    void WatchAd(string placement, Action<bool> callback);
}

[Serializable]
public class StoreProduct
{
    public string productId;
    public string displayName;
    public string priceString;
    public int gemAmount;
    public string bonusLabel; // e.g., "Best Value!", "Most Popular"
}
