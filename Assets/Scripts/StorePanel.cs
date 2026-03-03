using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Mock IAP store panel. Shows gem bundles and ad-for-reward options.
/// Attach to a panel under Canvas.
/// </summary>
public class StorePanel : MonoBehaviour, IPanel
{
    [Header("Panel Controls")]
    [SerializeField] Button openButton;
    [SerializeField] Button closeButton;

    [Header("Product List")]
    [SerializeField] Transform productContainer;
    [SerializeField] GameObject productPrefab;

    [Header("Ad Rewards")]
    [SerializeField] Button watchAdButton;
    [SerializeField] TMP_Text adStatusText;
    [SerializeField] double adRewardPetals = 50;

    [Header("Purchase Feedback")]
    [SerializeField] TMP_Text feedbackText;

    readonly List<GameObject> spawnedItems = new();
    IStoreService store;
    bool adInProgress;

    void Awake()
    {
        Services.Register(this);

        if (openButton != null) openButton.onClick.AddListener(Open);
        if (closeButton != null) closeButton.onClick.AddListener(Close);
        if (watchAdButton != null) watchAdButton.onClick.AddListener(OnWatchAd);

        gameObject.SetActive(false);
    }

    void Start()
    {
        if (Services.TryGet<PanelManager>(out var pm))
            pm.Register(this);
    }

    public void Open()
    {
        if (Services.TryGet<PanelManager>(out var pm))
            pm.Open(this);

        store = Services.Get<IStoreService>();
        Debug.Log($"[StorePanel] Open called. store is {(store != null ? "valid" : "NULL")}");

        gameObject.SetActive(true);

        if (feedbackText != null) feedbackText.text = "";
        if (adStatusText != null) adStatusText.text = "Watch ad for free petals!";

        BuildProducts();
    }

    public void Close()
    {
        ClearSpawned();
        gameObject.SetActive(false);
    }

    void BuildProducts()
    {
        ClearSpawned();

        if (store == null)
        {
            Debug.LogError("[StorePanel] Cannot build products — store service is null");
            return;
        }

        Debug.Log($"[StorePanel] productContainer: {(productContainer != null ? productContainer.name : "NULL")}");
        Debug.Log($"[StorePanel] productPrefab: {(productPrefab != null ? productPrefab.name : "NULL")}");

        store.GetProducts(products =>
        {
            Debug.Log($"[StorePanel] Got {products.Count} products");

            foreach (var product in products)
            {
                var obj = Instantiate(productPrefab, productContainer);
                spawnedItems.Add(obj);

                Debug.Log($"[StorePanel] Spawned product: {product.displayName}");

                var nameText = obj.transform.Find("NameText")?.GetComponent<TMP_Text>();
                var priceText = obj.transform.Find("PriceText")?.GetComponent<TMP_Text>();
                var amountText = obj.transform.Find("AmountText")?.GetComponent<TMP_Text>();
                var bonusText = obj.transform.Find("BonusText")?.GetComponent<TMP_Text>();
                var buyButton = obj.GetComponent<Button>();

                if (nameText != null) nameText.text = product.displayName;
                if (priceText != null) priceText.text = product.priceString;
                if (amountText != null) amountText.text = $"{product.gemAmount} gems";

                if (bonusText != null)
                {
                    bonusText.text = product.bonusLabel;
                    bonusText.gameObject.SetActive(!string.IsNullOrEmpty(product.bonusLabel));
                }

                string id = product.productId;
                if (buyButton != null)
                {
                    buyButton.onClick.AddListener(() => OnPurchase(id));
                }
            }
        });
    }

    void OnPurchase(string productId)
    {
        if (feedbackText != null) feedbackText.text = "Processing...";

        store.Purchase(productId, (success, message) =>
        {
            if (feedbackText != null)
                feedbackText.text = success ? "Purchase successful!" : $"Failed: {message}";

            if (success) BuildProducts();
        });
    }

    void OnWatchAd()
    {
        if (adInProgress) return;
        adInProgress = true;

        if (adStatusText != null) adStatusText.text = "Watching ad...";
        if (watchAdButton != null) watchAdButton.interactable = false;

        store.WatchAd("reward_petals", success =>
        {
            adInProgress = false;

            if (success)
            {
                var currency = Services.Get<CurrencyManager>();
                currency?.Add(CurrencyType.Petals, adRewardPetals);

                if (adStatusText != null) adStatusText.text = $"+{adRewardPetals:F0} petals! Watch again?";
            }
            else
            {
                if (adStatusText != null) adStatusText.text = "Ad failed. Try again.";
            }

            if (watchAdButton != null) watchAdButton.interactable = true;
        });
    }

    void ClearSpawned()
    {
        foreach (var obj in spawnedItems)
            Destroy(obj);
        spawnedItems.Clear();
    }
}