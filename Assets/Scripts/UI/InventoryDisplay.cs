using System.Text;
using TMPro;
using UnityEngine;

/// <summary>
/// Shows current flower inventory on the HUD.
/// Attach to a TMP_Text object under HUD.
/// </summary>
public class InventoryDisplay : MonoBehaviour
{
    [SerializeField] TMP_Text inventoryText;

    readonly StringBuilder sb = new();

    void Awake()
    {
        if (inventoryText == null)
            inventoryText = GetComponent<TMP_Text>();
    }

    void OnEnable()
    {
        EventBus.Subscribe<InventoryChangedEvent>(OnInventoryChanged);
        Refresh();
    }

    void OnDisable()
    {
        EventBus.Unsubscribe<InventoryChangedEvent>(OnInventoryChanged);
    }

    void OnInventoryChanged(InventoryChangedEvent evt) => Refresh();

    void Refresh()
    {
        if (inventoryText == null) return;
        if (!Services.TryGet<InventoryManager>(out var inv))
        {
            inventoryText.text = "";
            return;
        }

        var stock = inv.GetAllStock();
        if (stock.Count == 0)
        {
            inventoryText.text = "No flowers";
            return;
        }

        sb.Clear();
        foreach (var kvp in stock)
        {
            if (sb.Length > 0) sb.Append("  |  ");
            sb.Append(kvp.Key).Append(": ").Append(kvp.Value);
        }
        inventoryText.text = sb.ToString();
    }
}
