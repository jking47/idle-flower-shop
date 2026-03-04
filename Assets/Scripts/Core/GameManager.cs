using UnityEngine;

/// <summary>
/// Bootstrap and top-level game state. Attach to a persistent GameObject.
/// All managers are components on this same GameObject (not children).
/// </summary>
public class GameManager : MonoBehaviour
{
    [Header("Game State")]
    [SerializeField] GamePhase currentPhase = GamePhase.Patch;

    [Header("Starting Resources")]
    [SerializeField] double startingPetals = 10;

    [Header("Phase Unlock Thresholds")]
    [SerializeField] double gardenUnlockPetals = 50;
    [SerializeField] double shopUnlockPetals = 500;
    [SerializeField] double businessUnlockCoins = 1000;

    public GamePhase CurrentPhase => currentPhase;

    bool suppressPhaseEvents;

    /// <summary>
    /// Used by SaveSystem to restore phase. Does not fire events.
    /// </summary>
    public void SetPhase(GamePhase phase)
    {
        currentPhase = phase;
    }

    public static GameManager Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        Services.Register(this);
    }

    void Start()
    {
        suppressPhaseEvents = true;

        var save = Services.Get<SaveSystem>();
        bool loaded = save != null && save.Load();

        if (!loaded)
        {
            var currency = Services.Get<CurrencyManager>();
            currency.Add(CurrencyType.Petals, startingPetals);
        }

        suppressPhaseEvents = false;
    }

    void OnEnable()
    {
        EventBus.Subscribe<CurrencyChangedEvent>(OnCurrencyChanged);
    }

    void OnDisable()
    {
        EventBus.Unsubscribe<CurrencyChangedEvent>(OnCurrencyChanged);
    }

    void OnCurrencyChanged(CurrencyChangedEvent evt)
    {
        CheckPhaseProgression();
    }

    void CheckPhaseProgression()
    {
        if (suppressPhaseEvents) return;

        var currency = Services.Get<CurrencyManager>();
        if (currency == null) return;

        // Loop to handle multi-phase jumps (e.g. debug adding 500 petals
        // should advance Patch → Garden → Shop in one pass)
        bool advanced = true;
        while (advanced)
        {
            advanced = false;
            GamePhase newPhase = currentPhase;

            switch (currentPhase)
            {
                case GamePhase.Patch:
                    if (currency.GetBalance(CurrencyType.Petals) >= gardenUnlockPetals)
                        newPhase = GamePhase.Garden;
                    break;

                case GamePhase.Garden:
                    if (currency.GetBalance(CurrencyType.Petals) >= shopUnlockPetals)
                        newPhase = GamePhase.Shop;
                    break;

                case GamePhase.Shop:
                    if (currency.GetBalance(CurrencyType.Coins) >= businessUnlockCoins)
                        newPhase = GamePhase.Business;
                    break;
            }

            if (newPhase != currentPhase)
            {
                currentPhase = newPhase;
                advanced = true;
                Debug.Log($"[GameManager] Phase unlocked: {currentPhase}");
                EventBus.Publish(new PhaseUnlockedEvent { phase = currentPhase });
            }
        }
    }

    /// <summary>
    /// Called by DebugPanel to do a clean reset. Destroys this DDOL object
    /// so the scene reload creates a fresh one.
    /// </summary>
    public void PrepareForReset()
    {
        Instance = null;
        Destroy(gameObject);
    }
}