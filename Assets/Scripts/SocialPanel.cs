using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Tabbed social panel: Friends, Leaderboard, Gifts, Community.
/// Attach to a panel under Canvas.
/// </summary>
public class SocialPanel : MonoBehaviour, IPanel
{
    [Header("Panel Controls")]
    [SerializeField] Button openButton;
    [SerializeField] Button closeButton;

    [Header("Tabs")]
    [SerializeField] Button friendsTabButton;
    [SerializeField] Button leaderboardTabButton;
    [SerializeField] Button giftsTabButton;
    [SerializeField] Button communityTabButton;

    [Header("Content Containers")]
    [SerializeField] GameObject friendsContent;
    [SerializeField] GameObject leaderboardContent;
    [SerializeField] GameObject giftsContent;
    [SerializeField] GameObject communityContent;

    [Header("List Containers (inside each content)")]
    [SerializeField] Transform friendsListContainer;
    [SerializeField] Transform leaderboardListContainer;
    [SerializeField] Transform giftsListContainer;
    [SerializeField] Transform communityListContainer;

    [Header("Prefabs")]
    [SerializeField] GameObject friendEntryPrefab;
    [SerializeField] GameObject leaderboardEntryPrefab;
    [SerializeField] GameObject giftEntryPrefab;
    [SerializeField] GameObject activityEntryPrefab;

    [Header("Gift Notification")]
    [SerializeField] GameObject giftNotificationBadge;

    readonly List<GameObject> spawnedItems = new();
    ISocialService social;

    void Awake()
    {
        Services.Register(this);

        if (openButton != null) openButton.onClick.AddListener(Open);
        if (closeButton != null) closeButton.onClick.AddListener(Close);

        if (friendsTabButton != null) friendsTabButton.onClick.AddListener(() => ShowTab("friends"));
        if (leaderboardTabButton != null) leaderboardTabButton.onClick.AddListener(() => ShowTab("leaderboard"));
        if (giftsTabButton != null) giftsTabButton.onClick.AddListener(() => ShowTab("gifts"));
        if (communityTabButton != null) communityTabButton.onClick.AddListener(() => ShowTab("community"));

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

        social = Services.Get<ISocialService>();
        gameObject.SetActive(true);
        ShowTab("friends");

        UpdateGiftBadge();
    }

    public void Close()
    {
        ClearSpawned();
        gameObject.SetActive(false);
    }

    void ShowTab(string tab)
    {
        ClearSpawned();

        if (friendsContent != null) friendsContent.SetActive(tab == "friends");
        if (leaderboardContent != null) leaderboardContent.SetActive(tab == "leaderboard");
        if (giftsContent != null) giftsContent.SetActive(tab == "gifts");
        if (communityContent != null) communityContent.SetActive(tab == "community");

        switch (tab)
        {
            case "friends": PopulateFriends(); break;
            case "leaderboard": PopulateLeaderboard(); break;
            case "gifts": PopulateGifts(); break;
            case "community": PopulateCommunity(); break;
        }
    }

    void PopulateFriends()
    {
        social.GetFriendList(friends =>
        {
            foreach (var friend in friends)
            {
                var obj = Instantiate(friendEntryPrefab, friendsListContainer);
                spawnedItems.Add(obj);

                var nameText = obj.transform.Find("NameText")?.GetComponent<TMP_Text>();
                var statusText = obj.transform.Find("StatusText")?.GetComponent<TMP_Text>();
                var levelText = obj.transform.Find("LevelText")?.GetComponent<TMP_Text>();
                var visitButton = obj.transform.Find("VisitButton")?.GetComponent<Button>();
                var giftButton = obj.transform.Find("GiftButton")?.GetComponent<Button>();

                if (nameText != null) nameText.text = friend.displayName;
                if (levelText != null) levelText.text = $"Lv.{friend.level}";
                if (statusText != null)
                {
                    statusText.text = friend.isOnline ? "Online" : FormatLastActive(friend.lastActive);
                    statusText.color = friend.isOnline ? Color.green : Color.gray;
                }

                if (visitButton != null)
                {
                    string id = friend.friendId;
                    visitButton.onClick.AddListener(() => VisitFriend(id));
                }

                if (giftButton != null)
                {
                    if (friend.canGiftToday)
                    {
                        string id = friend.friendId;
                        giftButton.onClick.AddListener(() => SendGiftTo(id));
                    }
                    else
                    {
                        giftButton.interactable = false;
                        var btnText = giftButton.GetComponentInChildren<TMP_Text>();
                        if (btnText != null) btnText.text = "Sent";
                    }
                }
            }
        });
    }

    void PopulateLeaderboard()
    {
        social.GetLeaderboard(entries =>
        {
            foreach (var entry in entries)
            {
                var obj = Instantiate(leaderboardEntryPrefab, leaderboardListContainer);
                spawnedItems.Add(obj);

                var rankText = obj.transform.Find("RankText")?.GetComponent<TMP_Text>();
                var nameText = obj.transform.Find("NameText")?.GetComponent<TMP_Text>();
                var scoreText = obj.transform.Find("ScoreText")?.GetComponent<TMP_Text>();

                if (rankText != null) rankText.text = $"#{entry.rank}";
                if (nameText != null)
                {
                    nameText.text = entry.displayName;
                    if (entry.isPlayer) nameText.color = new Color(1f, 0.85f, 0.3f);
                }
                if (scoreText != null) scoreText.text = $"{entry.renown:F0}";
            }
        });
    }

    void PopulateGifts()
    {
        social.CollectGifts(gifts =>
        {
            if (gifts.Count == 0)
            {
                var obj = Instantiate(giftEntryPrefab, giftsListContainer);
                spawnedItems.Add(obj);
                var nameText = obj.transform.Find("NameText")?.GetComponent<TMP_Text>();
                if (nameText != null) nameText.text = "No gifts right now. Check back later!";
                return;
            }

            foreach (var gift in gifts)
            {
                var obj = Instantiate(giftEntryPrefab, giftsListContainer);
                spawnedItems.Add(obj);

                var nameText = obj.transform.Find("NameText")?.GetComponent<TMP_Text>();
                var detailText = obj.transform.Find("DetailText")?.GetComponent<TMP_Text>();

                if (nameText != null) nameText.text = $"From {gift.fromName}";
                if (detailText != null) detailText.text = $"{gift.flowerName} seed";
            }

            if (giftNotificationBadge != null)
                giftNotificationBadge.SetActive(false);
        });
    }

    void PopulateCommunity()
    {
        social.GetActivityFeed(feed =>
        {
            foreach (var entry in feed)
            {
                var obj = Instantiate(activityEntryPrefab, communityListContainer);
                spawnedItems.Add(obj);

                var messageText = obj.transform.Find("MessageText")?.GetComponent<TMP_Text>();
                var timeText = obj.transform.Find("TimeText")?.GetComponent<TMP_Text>();

                if (messageText != null) messageText.text = entry.message;
                if (timeText != null) timeText.text = FormatTimeAgo(entry.timestamp);
            }
        });
    }

    void VisitFriend(string friendId)
    {
        social.GetFriendGarden(friendId, garden =>
        {
            if (garden == null) return;
            Debug.Log($"[Social] Visiting {garden.displayName}'s garden ({garden.plots.Count} plots)");
        });
    }

    void SendGiftTo(string friendId)
    {
        var gardenManager = Services.Get<GardenManager>();
        if (gardenManager == null || gardenManager.AvailableFlowers.Count == 0) return;

        string flowerName = gardenManager.AvailableFlowers[0].displayName;

        social.SendGift(friendId, flowerName, success =>
        {
            if (success)
            {
                Debug.Log($"[Social] Gift sent to {friendId}");
                ShowTab("friends");
            }
        });
    }

    void UpdateGiftBadge()
    {
        if (giftNotificationBadge != null)
            giftNotificationBadge.SetActive(true);
    }

    void ClearSpawned()
    {
        foreach (var obj in spawnedItems)
            Destroy(obj);
        spawnedItems.Clear();
    }

    string FormatLastActive(DateTime time)
    {
        var diff = DateTime.UtcNow - time;
        if (diff.TotalMinutes < 60) return $"{diff.TotalMinutes:F0}m ago";
        if (diff.TotalHours < 24) return $"{diff.TotalHours:F0}h ago";
        return $"{diff.TotalDays:F0}d ago";
    }

    string FormatTimeAgo(DateTime time)
    {
        var diff = DateTime.UtcNow - time;
        if (diff.TotalMinutes < 5) return "Just now";
        if (diff.TotalMinutes < 60) return $"{diff.TotalMinutes:F0} min ago";
        if (diff.TotalHours < 24) return $"{diff.TotalHours:F0} hours ago";
        return $"{diff.TotalDays:F0} days ago";
    }
}