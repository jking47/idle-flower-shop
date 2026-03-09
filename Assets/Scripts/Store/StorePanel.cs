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
    [SerializeField] int adRewardGems = 3;

    [Header("Purchase Feedback")]
    [SerializeField] TMP_Text feedbackText;

    readonly List<GameObject> spawnedItems = new();
    IStoreService store;
    bool adInProgress;
    bool gemAdInProgress;
    Button gemAdButton;
    TMP_Text gemAdStatusText;

    void Awake()
    {
        Services.Register(this);

        if (openButton != null) openButton.onClick.AddListener(Open);
        if (closeButton != null) closeButton.onClick.AddListener(Close);
        if (watchAdButton != null) watchAdButton.onClick.AddListener(OnWatchAd);

        gameObject.AddComponent<PanelTransition>();
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
        if (adStatusText != null) adStatusText.text = "Watch ad → free petals!";

        EnsureGemAdButton();
        if (gemAdStatusText != null) gemAdStatusText.text = $"Watch ad → {adRewardGems} free gems!";

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

    void OnWatchAdForGems()
    {
        if (gemAdInProgress || store == null) return;
        gemAdInProgress = true;

        if (gemAdStatusText != null) gemAdStatusText.text = "Watching ad...";
        if (gemAdButton != null) gemAdButton.interactable = false;

        store.WatchAd("reward_gems", success =>
        {
            gemAdInProgress = false;

            if (success)
            {
                var currency = Services.Get<CurrencyManager>();
                currency?.Add(CurrencyType.Gems, adRewardGems);

                if (gemAdStatusText != null) gemAdStatusText.text = $"+{adRewardGems} gems! Watch again?";
            }
            else
            {
                if (gemAdStatusText != null) gemAdStatusText.text = "Ad failed. Try again.";
            }

            if (gemAdButton != null) gemAdButton.interactable = true;
        });
    }

    /// <summary>
    /// Builds the gem-ad button below the petal-ad button on first Open.
    /// Uses the watchAdButton's parent as the container so it fits the existing layout.
    /// </summary>
    void EnsureGemAdButton()
    {
        if (gemAdButton != null) return;
        if (watchAdButton == null) return;

        var parent = watchAdButton.transform.parent;
        if (parent == null) return;

        // Row container
        var rowGo = new GameObject("GemAdRow");
        var rowRt = rowGo.AddComponent<RectTransform>();
        rowRt.SetParent(parent, false);

        var rowLayout = rowGo.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();
        rowLayout.spacing = 8f;
        rowLayout.childControlWidth = true;
        rowLayout.childForceExpandWidth = false;
        rowLayout.childControlHeight = true;
        rowLayout.childForceExpandHeight = false;

        var rowElem = rowGo.AddComponent<UnityEngine.UI.LayoutElement>();
        rowElem.minHeight = 36f;

        // Button
        var btnGo = new GameObject("WatchAdGemsButton");
        var btnRt = btnGo.AddComponent<RectTransform>();
        btnRt.SetParent(rowGo.transform, false);

        var btnImg = btnGo.AddComponent<UnityEngine.UI.Image>();
        btnImg.color = new Color(0.2f, 0.6f, 0.9f);

        gemAdButton = btnGo.AddComponent<Button>();
        gemAdButton.targetGraphic = btnImg;
        gemAdButton.onClick.AddListener(OnWatchAdForGems);

        var btnElem = btnGo.AddComponent<UnityEngine.UI.LayoutElement>();
        btnElem.minWidth = 180f;
        btnElem.minHeight = 36f;

        var btnText = new GameObject("Text").AddComponent<TMPro.TextMeshProUGUI>();
        btnText.transform.SetParent(btnGo.transform, false);
        var btnTextRt = btnText.GetComponent<RectTransform>();
        btnTextRt.anchorMin = Vector2.zero;
        btnTextRt.anchorMax = Vector2.one;
        btnTextRt.offsetMin = btnTextRt.offsetMax = Vector2.zero;
        btnText.text = "Watch Ad";
        btnText.fontSize = 16f;
        btnText.alignment = TMPro.TextAlignmentOptions.Center;
        btnText.raycastTarget = false;

        // Status text
        var statusGo = new GameObject("GemAdStatus");
        var statusRt = statusGo.AddComponent<RectTransform>();
        statusRt.SetParent(rowGo.transform, false);

        gemAdStatusText = statusGo.AddComponent<TMPro.TextMeshProUGUI>();
        gemAdStatusText.text = $"Watch ad → {adRewardGems} free gems!";
        gemAdStatusText.fontSize = 14f;
        gemAdStatusText.alignment = TMPro.TextAlignmentOptions.MidlineLeft;
        gemAdStatusText.raycastTarget = false;
        gemAdStatusText.color = new Color(0.8f, 0.9f, 1f);

        var statusElem = statusGo.AddComponent<UnityEngine.UI.LayoutElement>();
        statusElem.flexibleWidth = 1f;
        statusElem.minHeight = 36f;
    }

    void ClearSpawned()
    {
        foreach (var obj in spawnedItems)
            Destroy(obj);
        spawnedItems.Clear();
    }
}