using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Mock store that simulates IAP with fake delays.
/// Attach to GameManager object.
/// </summary>
public class MockStoreService : MonoBehaviour, IStoreService
{
    [Header("Products")]
    [SerializeField] float purchaseDelay = 1.0f;
    [SerializeField] float adDuration = 7.0f;

    readonly List<StoreProduct> products = new()
    {
        new StoreProduct
        {
            productId = "gems_small",
            displayName = "Handful of Gems",
            priceString = "$0.99",
            gemAmount = 50,
            bonusLabel = ""
        },
        new StoreProduct
        {
            productId = "gems_medium",
            displayName = "Pouch of Gems",
            priceString = "$4.99",
            gemAmount = 300,
            bonusLabel = "Most Popular"
        },
        new StoreProduct
        {
            productId = "gems_large",
            displayName = "Chest of Gems",
            priceString = "$9.99",
            gemAmount = 700,
            bonusLabel = "Best Value!"
        },
        new StoreProduct
        {
            productId = "gems_mega",
            displayName = "Vault of Gems",
            priceString = "$19.99",
            gemAmount = 1600,
            bonusLabel = "2x Bonus!"
        }
    };

    void Awake()
    {
        Services.Register<IStoreService>(this);
    }

    public void GetProducts(Action<List<StoreProduct>> callback)
    {
        callback?.Invoke(products);
    }

    public void Purchase(string productId, Action<bool, string> callback)
    {
        StartCoroutine(SimulatePurchase(productId, callback));
    }

    public void WatchAd(string placement, Action<bool> callback)
    {
        // Show the fake ad overlay instead of a silent wait
        AdSimulator.Show(adDuration, () =>
        {
            Debug.Log($"[Store] Ad complete for: {placement}");
            callback?.Invoke(true);
        });
    }

    IEnumerator SimulatePurchase(string productId, Action<bool, string> callback)
    {
        yield return new WaitForSeconds(purchaseDelay);

        var product = products.Find(p => p.productId == productId);
        if (product == null)
        {
            callback?.Invoke(false, "Product not found");
            yield break;
        }

        var currency = Services.Get<CurrencyManager>();
        if (currency != null)
        {
            currency.Add(CurrencyType.Gems, product.gemAmount);
        }

        Debug.Log($"[Store] Mock purchase: {product.displayName} ({product.priceString}) → +{product.gemAmount} gems");
        callback?.Invoke(true, "Purchase successful!");
    }
}