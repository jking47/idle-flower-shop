using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tracks player milestones and awards Renown on completion.
/// Self-contained — subscribe to game events and fire achievement toasts.
/// Attach to GameManager object.
/// </summary>
public class AchievementManager : MonoBehaviour
{
    // --- Milestone Definitions ---

    public enum MilestoneId
    {
        FirstBloom        = 0,
        FirstHarvest      = 1,
        GreenThumb        = 2,   // 10 harvests
        ProlifcGrower    = 3,   // 50 harvests
        FirstOrder        = 4,
        RegularSupplier   = 5,   // 5 orders
        BusinessBlooming  = 6,   // 20 orders
        PetalCollector    = 7,   // 100 petals earned
        PetalHoarder      = 8,   // 500 petals earned
        GardenUnlocked    = 9,
        ShopOpened        = 10,
        BusinessTycoon    = 11,
        RoseUnlocked      = 12,
        SunflowerUnlocked = 13,
        OrchidUnlocked    = 14,
        LilyUnlocked      = 15,
    }

    public readonly struct Milestone
    {
        public readonly MilestoneId Id;
        public readonly string Title;
        public readonly string Detail;
        public readonly double Renown;

        public Milestone(MilestoneId id, string title, string detail, double renown)
        {
            Id = id; Title = title; Detail = detail; Renown = renown;
        }
    }

    public static IReadOnlyList<Milestone> AllMilestones => All;

    static readonly Milestone[] All = new[]
    {
        new Milestone(MilestoneId.FirstBloom,        "First Bloom!",          "A flower bloomed in your garden.",              5),
        new Milestone(MilestoneId.FirstHarvest,      "First Harvest!",        "You harvested your first flower.",              5),
        new Milestone(MilestoneId.GreenThumb,        "Green Thumb",           "Harvested 10 flowers.",                        10),
        new Milestone(MilestoneId.ProlifcGrower,    "Prolific Grower",       "Harvested 50 flowers.",                        25),
        new Milestone(MilestoneId.FirstOrder,        "Open for Business",     "Filled your first customer order.",            10),
        new Milestone(MilestoneId.RegularSupplier,   "Regular Supplier",      "Filled 5 customer orders.",                    20),
        new Milestone(MilestoneId.BusinessBlooming,  "Business is Blooming",  "Filled 20 customer orders.",                   50),
        new Milestone(MilestoneId.PetalCollector,    "Petal Collector",       "Earned 100 petals in total.",                  15),
        new Milestone(MilestoneId.PetalHoarder,      "Petal Hoarder",         "Earned 500 petals in total.",                  25),
        new Milestone(MilestoneId.GardenUnlocked,    "The Garden Awaits",     "Expanded from Patch to Garden.",               10),
        new Milestone(MilestoneId.ShopOpened,        "Grand Opening!",        "Opened your flower shop.",                     25),
        new Milestone(MilestoneId.BusinessTycoon,    "Business Tycoon",       "Built a real flower business.",                50),
        new Milestone(MilestoneId.RoseUnlocked,      "Rosy Outlook",          "Unlocked the Rose — a fine coin earner.",       8),
        new Milestone(MilestoneId.SunflowerUnlocked, "Facing the Sun",        "Unlocked the Sunflower's bulk yield.",          8),
        new Milestone(MilestoneId.OrchidUnlocked,    "Orchid Aficionado",     "Unlocked the premium Orchid.",                 12),
        new Milestone(MilestoneId.LilyUnlocked,      "Master Gardener",       "Unlocked the Lily — the pinnacle of petals.",  20),
    };

    // --- State ---

    readonly HashSet<int> completed = new();

    int    totalHarvests;
    int    totalOrdersFilled;
    double totalPetalsEarned;

    public int    TotalHarvests     => totalHarvests;
    public int    TotalOrdersFilled => totalOrdersFilled;
    public double TotalPetalsEarned => totalPetalsEarned;

    void Awake()
    {
        Services.Register(this);
    }

    void OnEnable()
    {
        EventBus.Subscribe<FlowerBloomedEvent>(OnBloomed);
        EventBus.Subscribe<FlowerHarvestedEvent>(OnHarvested);
        EventBus.Subscribe<OrderFilledEvent>(OnOrderFilled);
        EventBus.Subscribe<PhaseUnlockedEvent>(OnPhaseUnlocked);
        EventBus.Subscribe<FlowerTypeUnlockedEvent>(OnFlowerTypeUnlocked);
    }

    void OnDisable()
    {
        EventBus.Unsubscribe<FlowerBloomedEvent>(OnBloomed);
        EventBus.Unsubscribe<FlowerHarvestedEvent>(OnHarvested);
        EventBus.Unsubscribe<OrderFilledEvent>(OnOrderFilled);
        EventBus.Unsubscribe<PhaseUnlockedEvent>(OnPhaseUnlocked);
        EventBus.Unsubscribe<FlowerTypeUnlockedEvent>(OnFlowerTypeUnlocked);
    }

    void OnBloomed(FlowerBloomedEvent e)
    {
        TryComplete(MilestoneId.FirstBloom);
    }

    void OnHarvested(FlowerHarvestedEvent e)
    {
        totalHarvests++;
        totalPetalsEarned += e.yield;

        TryComplete(MilestoneId.FirstHarvest);
        if (totalHarvests >= 10)  TryComplete(MilestoneId.GreenThumb);
        if (totalHarvests >= 50)  TryComplete(MilestoneId.ProlifcGrower);
        if (totalPetalsEarned >= 100)  TryComplete(MilestoneId.PetalCollector);
        if (totalPetalsEarned >= 500)  TryComplete(MilestoneId.PetalHoarder);
    }

    void OnOrderFilled(OrderFilledEvent e)
    {
        totalOrdersFilled++;

        TryComplete(MilestoneId.FirstOrder);
        if (totalOrdersFilled >= 5)  TryComplete(MilestoneId.RegularSupplier);
        if (totalOrdersFilled >= 20) TryComplete(MilestoneId.BusinessBlooming);
    }

    void OnPhaseUnlocked(PhaseUnlockedEvent e)
    {
        switch (e.phase)
        {
            case GamePhase.Garden:   TryComplete(MilestoneId.GardenUnlocked); break;
            case GamePhase.Shop:     TryComplete(MilestoneId.ShopOpened);     break;
            case GamePhase.Business: TryComplete(MilestoneId.BusinessTycoon); break;
        }
    }

    void OnFlowerTypeUnlocked(FlowerTypeUnlockedEvent e)
    {
        switch (e.flowerName)
        {
            case "Flower_3_Rose":      TryComplete(MilestoneId.RoseUnlocked);      break;
            case "Flower_4_Sunflower": TryComplete(MilestoneId.SunflowerUnlocked); break;
            case "Flower_5_Orchid":    TryComplete(MilestoneId.OrchidUnlocked);    break;
            case "Flower_6_Lily":      TryComplete(MilestoneId.LilyUnlocked);      break;
        }
    }

    void TryComplete(MilestoneId id)
    {
        int key = (int)id;
        if (completed.Contains(key)) return;
        completed.Add(key);

        var milestone = Array.Find(All, m => m.Id == id);
        Debug.Log($"[Achievement] {milestone.Title} — +{milestone.Renown} renown");

        if (milestone.Renown > 0)
            Services.Get<CurrencyManager>()?.Add(CurrencyType.Renown, milestone.Renown);

        AchievementToast.Show(milestone.Title, milestone.Detail, milestone.Renown);
    }

    // --- Save / Load ---

    public List<int> GetSaveData() => new(completed);

    public void LoadSaveData(List<int> data, int harvests, int orders, double petalsEarned)
    {
        if (data != null)
            foreach (int id in data) completed.Add(id);

        totalHarvests      = harvests;
        totalOrdersFilled  = orders;
        totalPetalsEarned  = petalsEarned;
    }
}
