using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Local mock implementation of ISocialService.
/// Generates procedural friends and simulates social features.
/// Attach to GameManager object.
/// </summary>
public class MockSocialService : MonoBehaviour, ISocialService
{
    [Header("Mock Settings")]
    [SerializeField] int friendCount = 8;
    [SerializeField] int leaderboardSize = 15;
    [SerializeField] List<FlowerData> allFlowers = new();

    List<FriendData> friends;
    List<GiftData> pendingGifts;
    List<CoopOrderData> coopOrders;
    HashSet<string> giftedToday = new();
    bool giftsCollectedThisSession;

    // Seed for deterministic procedural generation
    System.Random rng;

    static readonly string[] firstNames = {
        "Lily", "Rose", "Jasmine", "Dahlia", "Ivy",
        "Sage", "Basil", "Hazel", "Violet", "Poppy",
        "Clover", "Aster", "Briar", "Fern", "Laurel"
    };

    static readonly string[] activityTemplates = {
        "{0} grew their first {1}!",
        "{0} just opened their flower shop!",
        "{0} harvested a rare {1}!",
        "{0} completed a bulk order!",
        "{0} reached Season {1}!",
        "{0}'s garden is thriving with {1} flowers!",
        "{0} sent you a gift!",
        "{0} unlocked a new plot!"
    };

    void Awake()
    {
        rng = new System.Random(42); // Fixed seed for consistent mock data
        Services.Register<ISocialService>(this);
        GenerateFriends();
        GeneratePendingGifts();
        GenerateCoopOrders();
    }

    void GenerateFriends()
    {
        friends = new List<FriendData>();

        for (int i = 0; i < friendCount; i++)
        {
            friends.Add(new FriendData
            {
                friendId = $"friend_{i}",
                displayName = firstNames[i % firstNames.Length],
                level = rng.Next(1, 25),
                totalRenown = rng.Next(50, 5000),
                isOnline = rng.NextDouble() > 0.6,
                lastActive = DateTime.UtcNow.AddMinutes(-rng.Next(0, 1440)),
                canGiftToday = true
            });
        }
    }

    void GeneratePendingGifts()
    {
        pendingGifts = new List<GiftData>();

        // 1-3 random gifts waiting
        int count = rng.Next(1, 4);
        for (int i = 0; i < count && i < friends.Count; i++)
        {
            if (allFlowers.Count == 0) break;
            pendingGifts.Add(new GiftData
            {
                fromFriendId = friends[i].friendId,
                fromName = friends[i].displayName,
                flowerName = allFlowers[rng.Next(allFlowers.Count)].displayName,
                sentTime = DateTime.UtcNow.AddHours(-rng.Next(1, 12))
            });
        }
    }

    void GenerateCoopOrders()
    {
        coopOrders = new List<CoopOrderData>();

        if (allFlowers.Count == 0) return;

        coopOrders.Add(new CoopOrderData
        {
            orderId = "coop_1",
            description = "Town Festival Decoration",
            requiredFlower = allFlowers[0].displayName,
            requiredAmount = 50,
            currentAmount = rng.Next(15, 40),
            playerContribution = 0,
            rewardCoins = 200,
            expiresAt = DateTime.UtcNow.AddHours(rng.Next(8, 24))
        });

        if (allFlowers.Count > 1)
        {
            coopOrders.Add(new CoopOrderData
            {
                orderId = "coop_2",
                description = "Wedding Arrangements",
                requiredFlower = allFlowers[1].displayName,
                requiredAmount = 30,
                currentAmount = rng.Next(5, 20),
                playerContribution = 0,
                rewardCoins = 350,
                expiresAt = DateTime.UtcNow.AddHours(rng.Next(12, 48))
            });
        }
    }

    // --- ISocialService Implementation ---

    public void GetFriendList(Action<List<FriendData>> callback)
    {
        // Update gift eligibility based on today's gifts
        foreach (var friend in friends)
        {
            friend.canGiftToday = !giftedToday.Contains(friend.friendId);
        }
        callback?.Invoke(friends);
    }

    public void GetFriendGarden(string friendId, Action<FriendGardenData> callback)
    {
        var friend = friends.Find(f => f.friendId == friendId);
        if (friend == null)
        {
            callback?.Invoke(null);
            return;
        }

        // Procedurally generate a garden state for this friend
        var gardenRng = new System.Random(friendId.GetHashCode());
        var plots = new List<FriendPlotInfo>();

        int plotCount = Mathf.Clamp(friend.level / 3 + 3, 3, 9);
        for (int i = 0; i < plotCount; i++)
        {
            if (allFlowers.Count > 0 && gardenRng.NextDouble() > 0.2)
            {
                plots.Add(new FriendPlotInfo
                {
                    flowerName = allFlowers[gardenRng.Next(allFlowers.Count)].displayName,
                    state = gardenRng.NextDouble() > 0.4 ? (int)PlotState.Bloomed : (int)PlotState.Growing
                });
            }
            else
            {
                plots.Add(new FriendPlotInfo { flowerName = "", state = (int)PlotState.Empty });
            }
        }

        callback?.Invoke(new FriendGardenData
        {
            friendId = friendId,
            displayName = friend.displayName,
            plots = plots
        });
    }

    public void SendGift(string friendId, string flowerName, Action<bool> callback)
    {
        if (giftedToday.Contains(friendId))
        {
            callback?.Invoke(false);
            return;
        }

        giftedToday.Add(friendId);
        Debug.Log($"[Social] Sent {flowerName} to {friendId}");
        callback?.Invoke(true);
    }

    public void CollectGifts(Action<List<GiftData>> callback)
    {
        // Return pending gifts only once per session to prevent duplicate inventory grants
        if (giftsCollectedThisSession)
        {
            callback?.Invoke(new List<GiftData>());
            return;
        }
        giftsCollectedThisSession = true;
        var collected = new List<GiftData>(pendingGifts);
        pendingGifts.Clear();
        callback?.Invoke(collected);
    }

    public void GetLeaderboard(Action<List<LeaderboardEntry>> callback)
    {
        var entries = new List<LeaderboardEntry>();

        // Add player
        var currency = Services.Get<CurrencyManager>();
        double playerRenown = currency != null ? currency.GetBalance(CurrencyType.Renown) : 0;

        entries.Add(new LeaderboardEntry
        {
            displayName = "You",
            renown = playerRenown,
            isPlayer = true
        });

        // Add friends
        foreach (var friend in friends)
        {
            entries.Add(new LeaderboardEntry
            {
                displayName = friend.displayName,
                renown = friend.totalRenown,
                isPlayer = false
            });
        }

        // Fill remaining slots with generated names
        for (int i = entries.Count; i < leaderboardSize; i++)
        {
            entries.Add(new LeaderboardEntry
            {
                displayName = $"Player_{rng.Next(1000, 9999)}",
                renown = rng.Next(10, 3000),
                isPlayer = false
            });
        }

        // Sort by renown descending, assign ranks
        entries.Sort((a, b) => b.renown.CompareTo(a.renown));
        for (int i = 0; i < entries.Count; i++)
        {
            entries[i].rank = i + 1;
        }

        callback?.Invoke(entries);
    }

    public void GetActivityFeed(Action<List<ActivityFeedEntry>> callback)
    {
        var feed = new List<ActivityFeedEntry>();
        var feedRng = new System.Random(DateTime.UtcNow.DayOfYear);

        int count = Mathf.Min(6, friends.Count);
        for (int i = 0; i < count; i++)
        {
            string template = activityTemplates[feedRng.Next(activityTemplates.Length)];
            string flowerName = allFlowers.Count > 0
                ? allFlowers[feedRng.Next(allFlowers.Count)].displayName
                : "flower";
            string detail = feedRng.Next(2, 15).ToString();

            // Pick either flower name or number depending on template
            string param = template.Contains("grew") || template.Contains("harvested")
                ? flowerName : detail;

            feed.Add(new ActivityFeedEntry
            {
                friendName = friends[i].displayName,
                message = string.Format(template, friends[i].displayName, param),
                timestamp = DateTime.UtcNow.AddMinutes(-feedRng.Next(5, 720))
            });
        }

        feed.Sort((a, b) => b.timestamp.CompareTo(a.timestamp));
        callback?.Invoke(feed);
    }

    public void ContributeToCoopOrder(string orderId, string flowerName, int amount, Action<bool> callback)
    {
        var order = coopOrders.Find(o => o.orderId == orderId);
        if (order == null || order.currentAmount >= order.requiredAmount)
        {
            callback?.Invoke(false);
            return;
        }

        int actual = Mathf.Min(amount, order.requiredAmount - order.currentAmount);
        order.currentAmount += actual;
        order.playerContribution += actual;

        Debug.Log($"[Social] Contributed {actual} {flowerName} to {order.description}");

        // Check if order completed
        if (order.currentAmount >= order.requiredAmount)
        {
            var currency = Services.Get<CurrencyManager>();
            if (currency != null)
            {
                double reward = order.rewardCoins * ((double)order.playerContribution / order.requiredAmount);
                currency.Add(CurrencyType.Coins, reward);
                Debug.Log($"[Social] Coop order complete! Earned {reward:F0} coins");
            }
        }

        callback?.Invoke(true);
    }

    public void GetCoopOrders(Action<List<CoopOrderData>> callback)
    {
        // Simulate friends contributing over time
        foreach (var order in coopOrders)
        {
            if (order.currentAmount < order.requiredAmount && rng.NextDouble() > 0.7)
            {
                order.currentAmount = Mathf.Min(order.currentAmount + rng.Next(1, 4), order.requiredAmount);
            }
        }
        callback?.Invoke(coopOrders);
    }
}
