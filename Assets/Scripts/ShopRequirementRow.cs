using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One row in an order card: flower icon, name, have/need count, demand badge.
/// Count turns green when requirement is met.
/// Prefab layout: [Icon Image] [Name TMP_Text] [Count TMP_Text] [Demand TMP_Text]
/// </summary>
public class ShopRequirementRow : MonoBehaviour
{
    [SerializeField] Image flowerIcon;
    [SerializeField] TMP_Text flowerNameText;
    [SerializeField] TMP_Text countText;

    [Header("Market Demand")]
    [Tooltip("Label showing HOT!/High/Normal/Low with demand color. Can be null if not needed.")]
    [SerializeField] TMP_Text demandText;

    FlowerData flower;
    int required;

    public void Set(FlowerData f, int count)
    {
        flower = f;
        required = count;
        if (flowerIcon)     flowerIcon.sprite = f.icon;
        if (flowerNameText) flowerNameText.text = f.displayName;
        Refresh();
    }

    /// <summary>
    /// Update count, color, and demand badge. Called from ShopOrderUI.RefreshFillButton()
    /// and whenever market updates.
    /// </summary>
    public void Refresh()
    {
        if (flower == null) return;

        // Stock count
        if (countText != null)
        {
            int have = Services.Get<InventoryManager>()?.GetCount(flower.name) ?? 0;
            countText.text = $"{have}/{required}";
            countText.color = have >= required
                ? new Color(0.2f, 0.8f, 0.2f)
                : new Color(0.9f, 0.3f, 0.3f);
        }

        // Demand badge
        if (demandText != null)
        {
            var market = Services.Get<MarketManager>();
            if (market != null)
            {
                demandText.text = market.GetDemandLabel(flower.name);
                demandText.color = market.GetDemandColor(flower.name);
            }
            else
            {
                demandText.text = string.Empty;
            }
        }
    }
}