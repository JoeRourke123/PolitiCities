# PolitiCities — design doc

Six features, built on the vanilla systems CS1 exposes. Verified against the reference DLLs in `lib/` (member names confirmed via metadata strings; exact signatures to be confirmed in dnSpy before writing Harmony patches against them).

## Systems map

| Feature | Vanilla system | Mod hook |
|---|---|---|
| Council with party seats | `PolicyManager` (`IsCityPolicySet`, `SetCityPolicy`, `TogglePolicy`, `m_policies`), approval/happiness via `StatisticsManager` (`CityHappiness`) | Harmony patch on policy enactment + own council model in sim tick |
| Coalition ministries | none (new) | own model; ministers affect service budgets via patches later |
| City-wide campaigns | `EconomyManager` cash flow | spend from budget → district poll boost, decays |
| Implementation lag | policy activation in `PolicyManager` | custom policies enter "implementing" state; effects gated on progress |
| Policy sliders | vanilla policies are binary | custom policy groups with float 0..1, mapped to effects |
| Pilot districts | `DistrictManager` (`m_districts`, `GetDistrictName`, `SetDistrictPolicy`, `DistrictPolicies` flags) | pilot applies policy in one district first |

Extension points used: `IUserMod` (entry), `LoadingExtensionBase` (lifecycle + hotkey object), `ThreadingExtensionBase.OnAfterSimulationTick` (simulation), `SerializableDataExtensionBase` (`OnCreated`/`OnLoadData`/`OnSaveData`/`OnReleased` for persistence).

## 1. City council with party seats

- 6 fixed parties (`Politics/Party.cs`), each with an ideology vector (economy / green / order, -1..+1) and a color.
- Vote share per party is state (`PoliticalState.VoteShare`), drifted monthly by a seeded random walk plus a leader-approval term (approval wiring to `StatisticsManager` happiness comes with the first Harmony patch).
- Every `MonthsPerElection` (48) in-game months, a city election recomputes the council: 81 seats allocated by largest-remainder from vote shares.
- Enacting a vanilla or custom policy requires a council majority in our model; if the player's party lacks it, the enact is queued for a council vote (UI shows pending votes; vote outcome = seat-weighted ideology fit + randomness). (Patch target: `PolicyManager.TogglePolicy` / `SetCityPolicy`.)

## 2. Coalition ministries

- If no party holds > 40.5 seats, the largest party forms a coalition by adding partners (largest first) until a majority; coalition membership stored in `Council`.
- 5 ministries (Finance, Infrastructure, Environment, Security, Social Affairs) are assigned to coalition parties in proportion to their coalition seats.
- Each ministry has a minister (competence 0..1, integrity 0..1) — later these modify the related service budget effectiveness and scandal risk. Scaffold stores the mapping now.

## 3. City-wide campaigns

- `PoliticalState.CampaignBudget` (city funds allocated to campaigning, set in options panel).
- `SpendOnDistrict(districtId, amount)` converts money into `CampaignBoost[districtId]`, which lifts that district's vote share for the player's party; boost decays 10% per month.
- Later: district-level elections consume these boosts; rallies/debates are Chirper events.

## 4. Implementation lag

- Enacting a custom policy puts it in `PolicyProgress[policyId] = 0` instead of active.
- Progress advances each sim tick by `rate / AdminCapacity`; effects (Harmony patches) only apply when progress reaches 1. Default duration ~3 in-game months.
- `AdminCapacity` (state float) represents bureaucracy; upgrading city hall raises it (later: building-based).

## 5. Policy sliders instead of binaries

- Custom policy groups (e.g. `policeFunding`, `welfare`, `greenInvestment`) are floats 0..1 in `PoliticalState.PolicySliders`.
- UI shows a `UISlider` per group; the sim maps slider → continuous effects (e.g. policeFunding 0..1 → crime rate factor and civil-liberties meter), instead of vanilla's on/off.
- Mutual exclusivity within a group is inherent (one slider per group).

## 6. Pilot districts

- Any custom policy can be piloted: `PilotDistricts[policyId] = districtId` (district ids are indices into `DistrictManager.m_districts`).
- Effects apply only in the pilot district until `RolloutAll()` promotes the policy city-wide (progress applies as in feature 4).
- UI lists pilots with district names (`DistrictManager.GetDistrictName`).

## Persistence

`PoliticalData : SerializableDataExtensionBase` currently writes the whole `PoliticalState` to `PolitiCities.dat` under `DataLocation.localApplicationData` in `OnSaveData`/`OnLoadData` (binary, versioned). Next iteration: move to an `ISerializableDataManager` with `IDataContainer` so data is embedded in the savegame and keyed per save — verify exact signatures in dnSpy first.

## UI

- `CouncilPanel` (ColossalFramework.UI): council seat bars per party, coalition + ministries, policy sliders, pilot list, campaign budget.
- Opened with the `P` key via `HotkeyHandler` (a `MonoBehaviour` created in `Loading.OnLevelLoaded`; main-thread `Input` only).
- Mod options via `OnSettingsUI` (`Settings.cs`).

## Build order / status

1. Scaffold: model, persistence, sim tick, panel, hotkey — done.
2. First live patch — done: `Patches/EconomyPatches.cs` (+10% private income while `stimulusPackage` is fully implemented); approval wired to `StatisticsManager.Get(StatisticType.CityHappiness).GetLatestInt32()`.
3. Council vote enforcement — done: `Patches/CouncilPatches.cs` prefix on `DistrictManager.SetCityPolicy(Policies)`, off by default (`Settings.EnforceCouncilVotes`), votes per `PoliticalEngine.CouncilVote` (governing-party platform auto-approves, otherwise coalition majority).
4. District elections — done in engine + adapter: every `MonthsPerDistrictElection` (12) months per district, winner from seeded vote + district happiness/crime (`District.m_finalHappiness`/`m_finalCrimeRate`), winner's `Party.Platform` policies applied via `DistrictManager.SetDistrictPolicy(Policies, uint8)`.
5. Sliders — engine-level effects on approval done (`PoliticalEngine.ApplySliderEffects`); panel +/- controls; continuous game-effect patches still open.
6. Campaigns/ministries affecting the real budget — open.

## Tests

`tests/PolitiCities.Tests.csproj` (net8.0, xUnit) compiles the pure logic files (`Party`, `Council`, `PoliticalState`, `PoliticalEngine`) directly and runs in the `Test` workflow on every push/PR. The mod csproj excludes `tests/**`. Keep all game-API-free logic in `PoliticalEngine` so it stays testable.

## Verified signatures (read from `lib/` DLLs via metadata, 2026-11)

- Policies live on `DistrictManager` (no `PolicyManager` type): `bool IsCityPolicySet(Policies)`, `void SetCityPolicy(Policies)`, `bool IsDistrictPolicySet(Policies, uint8)`, `void SetDistrictPolicy(Policies, uint8)`, `string GetDistrictName(int)`, `uint8 GetDistrict(Vector3)`, fields `int m_districtCount`, `Array8<District> m_districts`. Policy flags enum is `Policies`.
- Approval/happiness: `Singleton<StatisticsManager>.instance.Get(StatisticType.CityHappiness)` returns `StatisticBase`; value via `GetLatestInt32()` (0-100). Wired into `PoliticalSimulation.ReadRealApproval`.
- Economy effect hook: `EconomyManager.AddPrivateIncome(int amount, Service, SubService, Level, int taxRate)` — patched with a `ref int amount` prefix (`Patches/EconomyPatches.cs`), +10% private income while `stimulusPackage` is fully implemented.
- `District` struct: per-zone `DistrictPrivateData` fields (m_residentialData…), `uint8 m_finalHappiness`, `uint8 m_finalCrimeRate`, policy flag fields per category.
- ICities confirmed: `ISerializableDataExtension` = `OnCreated(ISerializableData)`/`OnReleased()`/`OnLoadData()`/`OnSaveData()`; `ThreadingExtensionBase.OnAfterSimulationTick()`; `Singleton<T>.instance`; `DataLocation.localApplicationData`; `UIView.GetAView()` + `AddUIComponent(Type)`.

## Open questions (verify in dnSpy against `lib/` DLLs)

- `ISerializableDataManager` / `IDataContainer` member signatures for savegame-embedded persistence (currently file-based in `PoliticalData`).
- How vanilla policy upkeep is charged (`EconomyManager.CalculatePolicyExpenses`) for custom policy costs.
