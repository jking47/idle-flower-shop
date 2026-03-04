# Idle Flower Shop

A systems-driven idle game built in Unity as a game design portfolio piece.

## Play It
[https://jking47.github.io/idle-flower-shop/](https://jking47.github.io/idle-flower-shop/)

## Systems
- **Phase Progression** — Patch → Garden → Shop → Business, each introducing new mechanics
- **Dynamic Market** — Supply/demand pricing with sell pressure and periodic demand shifts
- **Order Fulfillment** — Timed customer orders with market-adjusted rewards
- **Upgrade System** — Data-driven ScriptableObject upgrades with easily customizable cost curves
- **Idle + Active Hybrid** — Auto-harvest idle income with active watering/harvesting for 2-3x efficiency
- **Save System** — JSON + PlayerPrefs with offline progress calculation
- **Mock Monetization** — Simulated IAP store and ad-for-reward using interface-based services (IStoreService)
- **Social System** — Mock friend list, gifting, leaderboard via ISocialService for real backend swap

## Architecture
- ServiceLocator pattern (`Services.Get<T>()`) over raw singletons
- EventBus for decoupled cross-system communication
- ScriptableObjects for all static data (flowers, upgrades, orders)
- Interface-based services with mock implementations (IStoreService, ISocialService)

## Built With
Unity 6, C#, WebGL
