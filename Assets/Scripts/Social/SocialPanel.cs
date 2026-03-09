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

    // Achievements tab — built programmatically; no extra inspector fields needed
    GameObject achievementsContent;
    Button achievementsTabButton;

    void Awake()
    {
        Services.Register(this);

        if (openButton != null) openButton.onClick.AddListener(Open);
        if (closeButton != null) closeButton.onClick.AddListener(Close);

        gameObject.AddComponent<PanelTransition>();

        if (friendsTabButton != null) friendsTabButton.onClick.AddListener(() => ShowTab("friends"));
        if (leaderboardTabButton != null) leaderboardTabButton.onClick.AddListener(() => ShowTab("leaderboard"));
        if (giftsTabButton != null) giftsTabButton.onClick.AddListener(() => ShowTab("gifts"));
        if (communityTabButton != null) communityTabButton.onClick.AddListener(() => ShowTab("community"));

        BuildAchievementsTab();

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

        if (friendsContent != null)      friendsContent.SetActive(tab == "friends");
        if (leaderboardContent != null)  leaderboardContent.SetActive(tab == "leaderboard");
        if (giftsContent != null)        giftsContent.SetActive(tab == "gifts");
        if (communityContent != null)    communityContent.SetActive(tab == "community");
        if (achievementsContent != null) achievementsContent.SetActive(tab == "achievements");

        switch (tab)
        {
            case "friends":      PopulateFriends();      break;
            case "leaderboard":  PopulateLeaderboard();  break;
            case "gifts":        PopulateGifts();        break;
            case "community":    PopulateCommunity();    break;
            case "achievements": PopulateAchievements(); break;
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

            // Look up flowers once for matching by displayName
            List<FlowerData> availableFlowers = null;
            if (Services.TryGet<GardenManager>(out var garden))
                availableFlowers = new List<FlowerData>(garden.AvailableFlowers);

            foreach (var gift in gifts)
            {
                var obj = Instantiate(giftEntryPrefab, giftsListContainer);
                spawnedItems.Add(obj);

                var nameText = obj.transform.Find("NameText")?.GetComponent<TMP_Text>();
                var detailText = obj.transform.Find("DetailText")?.GetComponent<TMP_Text>();

                if (nameText != null) nameText.text = $"From {gift.fromName}";
                if (detailText != null) detailText.text = $"{gift.flowerName} seed";

                // Add gifted flower to inventory
                if (availableFlowers != null && Services.TryGet<InventoryManager>(out var inv))
                {
                    var flower = availableFlowers.Find(
                        f => f.displayName == gift.flowerName || f.name == gift.flowerName);
                    if (flower != null)
                        inv.Add(flower.name, 1);
                }
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

    // --- Achievements Tab ---

    void BuildAchievementsTab()
    {
        // --- Tab button: clone community button for matching style, relabel it ---
        if (communityTabButton == null) return;

        achievementsTabButton = Instantiate(communityTabButton, communityTabButton.transform.parent);
        achievementsTabButton.name = "AchievementsTabButton";
        var label = achievementsTabButton.GetComponentInChildren<TMP_Text>();
        if (label != null) label.text = "Medals";
        achievementsTabButton.onClick.RemoveAllListeners();
        achievementsTabButton.onClick.AddListener(() => ShowTab("achievements"));

        // --- Content panel: fresh GameObject, same parent/size as communityContent ---
        if (communityContent == null) return;

        var panelGo = new GameObject("AchievementsContent");
        var panelRt = panelGo.AddComponent<RectTransform>();
        panelRt.SetParent(communityContent.transform.parent, false);

        // Match community content's rect exactly
        var srcRt = communityContent.GetComponent<RectTransform>();
        panelRt.anchorMin        = srcRt.anchorMin;
        panelRt.anchorMax        = srcRt.anchorMax;
        panelRt.anchoredPosition = srcRt.anchoredPosition;
        panelRt.sizeDelta        = srcRt.sizeDelta;
        panelRt.pivot            = srcRt.pivot;

        achievementsContent = panelGo;

        // --- Scroll view inside the panel ---
        var scrollRt = new GameObject("Scroll").AddComponent<RectTransform>();
        scrollRt.SetParent(panelRt, false);
        scrollRt.anchorMin = Vector2.zero;
        scrollRt.anchorMax = Vector2.one;
        scrollRt.offsetMin = Vector2.zero;
        scrollRt.offsetMax = Vector2.zero;

        var sv = scrollRt.gameObject.AddComponent<ScrollRect>();
        sv.horizontal = false;

        var viewportRt = new GameObject("Viewport").AddComponent<RectTransform>();
        viewportRt.SetParent(scrollRt, false);
        viewportRt.anchorMin = Vector2.zero;
        viewportRt.anchorMax = Vector2.one;
        viewportRt.offsetMin = Vector2.zero;
        viewportRt.offsetMax = Vector2.zero;
        viewportRt.gameObject.AddComponent<Image>().color = Color.white;
        viewportRt.gameObject.AddComponent<Mask>().showMaskGraphic = false;
        sv.viewport = viewportRt;

        var contentRt = new GameObject("Content").AddComponent<RectTransform>();
        contentRt.SetParent(viewportRt, false);
        contentRt.anchorMin = new Vector2(0f, 1f);
        contentRt.anchorMax = new Vector2(1f, 1f);
        contentRt.pivot     = new Vector2(0.5f, 1f);
        contentRt.sizeDelta = Vector2.zero;
        sv.content = contentRt;

        var vlg = contentRt.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.spacing              = 6f;
        vlg.padding              = new RectOffset(8, 8, 8, 8);
        vlg.childControlWidth    = true;
        vlg.childControlHeight   = false;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;

        contentRt.gameObject.AddComponent<ContentSizeFitter>().verticalFit =
            ContentSizeFitter.FitMode.PreferredSize;

        achievementsContent.SetActive(false);
    }

    void PopulateAchievements()
    {
        var scroll = achievementsContent.GetComponentInChildren<ScrollRect>();
        Transform container = scroll != null ? (Transform)scroll.content : achievementsContent.transform;

        // Works even if AchievementManager isn't attached yet — just shows all as locked
        var completed = Services.TryGet<AchievementManager>(out var mgr)
            ? new HashSet<int>(mgr.GetSaveData())
            : new HashSet<int>();

        foreach (var milestone in AchievementManager.AllMilestones)
        {
            bool done = completed.Contains((int)milestone.Id);

            const float rowH = 90f;

            var row = new GameObject("AchRow");
            var rowRt = row.AddComponent<RectTransform>();
            rowRt.SetParent(container, false);
            rowRt.sizeDelta = new Vector2(0f, rowH);
            spawnedItems.Add(row);

            var le = row.AddComponent<LayoutElement>();
            le.preferredHeight = rowH;
            le.flexibleWidth   = 1f;

            var bg = row.AddComponent<Image>();
            bg.color = done
                ? new Color(0.18f, 0.28f, 0.18f, 0.9f)
                : new Color(0.18f, 0.20f, 0.26f, 0.7f);

            var outline = row.AddComponent<Outline>();
            outline.effectColor = done
                ? new Color(0.4f, 0.75f, 0.4f, 0.6f)
                : new Color(0.3f, 0.35f, 0.45f, 0.5f);
            outline.effectDistance = new Vector2(1, -1);

            // Check / lock icon
            var iconGo = new GameObject("Icon");
            var iconRt = iconGo.AddComponent<RectTransform>();
            iconRt.SetParent(rowRt, false);
            iconRt.anchorMin        = new Vector2(0f, 0.5f);
            iconRt.anchorMax        = new Vector2(0f, 0.5f);
            iconRt.pivot            = new Vector2(0f, 0.5f);
            iconRt.anchoredPosition = new Vector2(10f, 0f);
            iconRt.sizeDelta        = new Vector2(44f, 44f);
            var iconText = iconGo.AddComponent<TextMeshProUGUI>();
            iconText.text      = done ? "✓" : "○";
            iconText.fontSize  = 30;
            iconText.color     = done ? new Color(0.4f, 0.9f, 0.4f) : new Color(0.4f, 0.45f, 0.55f);
            iconText.alignment = TextAlignmentOptions.Center;
            iconText.raycastTarget = false;

            // Title
            var titleGo = new GameObject("Title");
            var titleRt = titleGo.AddComponent<RectTransform>();
            titleRt.SetParent(rowRt, false);
            titleRt.anchorMin = new Vector2(0f, 0.5f);
            titleRt.anchorMax = new Vector2(0.72f, 1f);
            titleRt.offsetMin = new Vector2(60f, 2f);
            titleRt.offsetMax = new Vector2(-4f, -6f);
            var titleTmp = titleGo.AddComponent<TextMeshProUGUI>();
            titleTmp.text              = milestone.Title;
            titleTmp.fontSize          = 24;
            titleTmp.color             = done ? Color.white : new Color(0.55f, 0.6f, 0.65f);
            titleTmp.enableWordWrapping = false;
            titleTmp.overflowMode      = TextOverflowModes.Ellipsis;
            titleTmp.alignment         = TextAlignmentOptions.BottomLeft;
            titleTmp.raycastTarget     = false;

            // Detail
            var detailGo = new GameObject("Detail");
            var detailRt = detailGo.AddComponent<RectTransform>();
            detailRt.SetParent(rowRt, false);
            detailRt.anchorMin = new Vector2(0f, 0f);
            detailRt.anchorMax = new Vector2(0.72f, 0.5f);
            detailRt.offsetMin = new Vector2(60f, 6f);
            detailRt.offsetMax = new Vector2(-4f, 0f);
            var detailTmp = detailGo.AddComponent<TextMeshProUGUI>();
            detailTmp.text              = milestone.Detail;
            detailTmp.fontSize          = 19;
            detailTmp.color             = new Color(0.6f, 0.65f, 0.7f);
            detailTmp.enableWordWrapping = false;
            detailTmp.overflowMode      = TextOverflowModes.Ellipsis;
            detailTmp.alignment         = TextAlignmentOptions.TopLeft;
            detailTmp.raycastTarget     = false;

            // Renown badge
            var badgeGo = new GameObject("Renown");
            var badgeRt = badgeGo.AddComponent<RectTransform>();
            badgeRt.SetParent(rowRt, false);
            badgeRt.anchorMin = new Vector2(0.72f, 0f);
            badgeRt.anchorMax = new Vector2(1f, 1f);
            badgeRt.offsetMin = new Vector2(4f, 10f);
            badgeRt.offsetMax = new Vector2(-10f, -10f);
            var badgeTmp = badgeGo.AddComponent<TextMeshProUGUI>();
            badgeTmp.text      = $"+{milestone.Renown:F0}\n<size=16>renown</size>";
            badgeTmp.fontSize  = 22;
            badgeTmp.color     = done ? new Color(0.75f, 0.85f, 1f) : new Color(0.4f, 0.45f, 0.5f);
            badgeTmp.alignment = TextAlignmentOptions.Center;
            badgeTmp.raycastTarget = false;
        }
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