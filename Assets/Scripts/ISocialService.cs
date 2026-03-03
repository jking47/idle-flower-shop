using System;
using System.Collections.Generic;

/// <summary>
/// Interface for all social operations. Backed by MockSocialService locally,
/// designed to be swappable to a real server implementation.
/// All methods use callbacks to mirror async server patterns.
/// </summary>
public interface ISocialService
{
    void GetFriendList(Action<List<FriendData>> callback);
    void GetFriendGarden(string friendId, Action<FriendGardenData> callback);
    void SendGift(string friendId, string flowerName, Action<bool> callback);
    void CollectGifts(Action<List<GiftData>> callback);
    void GetLeaderboard(Action<List<LeaderboardEntry>> callback);
    void GetActivityFeed(Action<List<ActivityFeedEntry>> callback);
    void ContributeToCoopOrder(string orderId, string flowerName, int amount, Action<bool> callback);
    void GetCoopOrders(Action<List<CoopOrderData>> callback);
}

[Serializable]
public class FriendData
{
    public string friendId;
    public string displayName;
    public int level;
    public double totalRenown;
    public bool isOnline;
    public DateTime lastActive;
    public bool canGiftToday;
}

[Serializable]
public class FriendGardenData
{
    public string friendId;
    public string displayName;
    public List<FriendPlotInfo> plots;
}

[Serializable]
public class FriendPlotInfo
{
    public string flowerName;
    public int state; // maps to PlotState
}

[Serializable]
public class GiftData
{
    public string fromFriendId;
    public string fromName;
    public string flowerName;
    public DateTime sentTime;
}

[Serializable]
public class LeaderboardEntry
{
    public string displayName;
    public double renown;
    public int rank;
    public bool isPlayer;
}

[Serializable]
public class ActivityFeedEntry
{
    public string friendName;
    public string message;
    public DateTime timestamp;
}

[Serializable]
public class CoopOrderData
{
    public string orderId;
    public string description;
    public string requiredFlower;
    public int requiredAmount;
    public int currentAmount;
    public int playerContribution;
    public double rewardCoins;
    public DateTime expiresAt;
}
