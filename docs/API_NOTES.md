# API_NOTES — Bannerlord APIs this design assumes

Every entry started as an assumption written from memory. Before writing code against an API, decompile the user's installed game version, confirm the real name and signature, and update the row.

Status values: `UNVERIFIED` · `VERIFIED` (signature confirmed, note the version) · `CHANGED` (exists but differs — write the real form) · `MISSING` (does not exist — write the alternative used, and log a decision in DECISIONS.md if it affects design) · `DEFERRED` (later phase; existence checked only)

Game version verified against: **v1.4.8** (Steam, no DLC), decompiled with ilspycmd 11.1 on 2026-10-18.

General notes for v1.4.8:
- All TaleWorlds assemblies target **netstandard2.0**; the game runs on .NET Framework (`Bannerlord.exe`). Our module targets net472 (BUTR template default).
- Events are exposed as `IMbEvent<...>` (interface), not `MbEvent<...>`. Subscribe with `CampaignEvents.X.AddNonSerializedListener(this, handler)`.

---

## 1. Module bootstrap

| API | Purpose | Real form (v1.4.8) | Status | Notes |
|---|---|---|---|---|
| `MBSubModuleBase` | Module entry point | `protected internal virtual void OnSubModuleLoad()`, `protected internal virtual void OnGameStart(Game game, IGameStarter gameStarterObject)`, `public virtual void OnGameEnd(Game game)` | VERIFIED | `TaleWorlds.MountAndBlade` |
| `CampaignGameStarter.AddBehavior` | Register our behavior | `public void AddBehavior(CampaignBehaviorBase campaignBehavior)` | VERIFIED | Cast `IGameStarter` when `game.GameType is Campaign` |
| `CampaignBehaviorBase` | Events + save | `public abstract void RegisterEvents()`, `public abstract void SyncData(IDataStore dataStore)` | VERIFIED | |
| `IDataStore.SyncData` | Persist a string | `bool SyncData<T>(string key, ref T data)` | VERIFIED | `IDataStore.IsSaving/IsLoading` also exist |
| `CampaignEvents.OnSessionLaunchedEvent` | Add menus once campaign is ready | `IMbEvent<CampaignGameStarter>` | VERIFIED | Vanilla adds menus here too |
| `CampaignEvents.OnGameLoadFinishedEvent` | Post-load hook | `IMbEvent` | VERIFIED | |
| JSON serializer | Persistence | Game ships `Newtonsoft.Json.dll` assembly 13.0.0.0 (file 13.0.1.25517) | VERIFIED | Used by TaleWorlds.Library. See ADR-012 |

## 2. Time

| API | Purpose | Real form | Status | Notes |
|---|---|---|---|---|
| `CampaignEvents.DailyTickEvent` | Price update | `IMbEvent` | VERIFIED | `WeeklyTickEvent` (`IMbEvent`) also exists |
| `CampaignTime.Now` | Current time | `static CampaignTime Now` | VERIFIED | `ToDays` is absolute (≈91077 at campaign start, year 1084). `DailyTickEvent` is a periodic event aligned to `CampaignTimeModel.CampaignStartTime`, which includes +9 hours, so the tick fires at 09:00, **not** at midnight; `floor(ToDays)` changes 9 hours before the tick (seen in game test 2026-10-18). Phase 1: Core day = our own tick counter / days since `CampaignStartTime`, never `floor(ToDays)` |
| `CampaignTime` getters | Day/season | `double ToDays`, `int GetDayOfYear`, `int GetDayOfSeason`, `int GetYear`, `Seasons GetSeasonOfYear` (properties, not methods) | VERIFIED | `CampaignTime.Seasons { Spring, Summer, Autumn, Winter }` |
| Year length | Calendar | `static int CampaignTime.DaysInWeek`, `WeeksInSeason`, `SeasonsInYear`; `static int DaysInSeason`, `DaysInYear` | CHANGED | Not constant: normal 7/21/84 days, FastMode 3/6/24. ADR-008: read at runtime |

## 3. World state readers

| API | Purpose | Real form | Status | Notes |
|---|---|---|---|---|
| `Town.AllTowns` | Iterate towns | `static MBReadOnlyList<Town> AllTowns` | VERIFIED | Castles are `Town` too: filter with `town.IsTown` / `Settlement.IsTown` |
| `Town.Prosperity` | Weighting | `float Prosperity` | VERIFIED | |
| `Town.FoodStocks` | Famine detection | `float FoodStocks { get; set; }` (on base `Fief`) | VERIFIED | |
| `Town.Security`, `Town.Loyalty` | Bond risk | `float` | VERIFIED | |
| `Town.MarketData` | Prices | `TownMarketData MarketData`; `int GetPrice(ItemObject item, MobileParty tradingParty = null, bool isSelling = false, PartyBase merchantParty = null)`; `float GetPriceFactor(ItemCategory)` | VERIFIED | Prices are **per ItemCategory** (supply/demand/inStore per category). `GetPrice` with null party is safe but adds a flat 6% trade penalty. ADR-009: use `item.Value × GetPriceFactor(category)`. Also `Town.GetItemCategoryPriceIndex(ItemCategory)` = same factor |
| `TownMarketData.GetSupply/GetDemand` | Supply/demand signal | `float GetSupply(ItemCategory)`, `float GetDemand(ItemCategory)`, `int GetItemCountOfCategory(ItemCategory)` | VERIFIED | |
| Siege state | Exclude besieged towns | `bool Town.IsUnderSiege`, `bool Settlement.IsUnderSiege`, `SiegeEvent Settlement.SiegeEvent` | VERIFIED | |
| Kingdom membership | | `IFaction Town.MapFaction`, `Clan Settlement.OwnerClan`, `Clan.Kingdom` | VERIFIED | |
| `Village.All` | Villages | `static MBReadOnlyList<Village> All` | VERIFIED | |
| `Village.VillageType.PrimaryProduction` | Main product | `VillageType VillageType` (field); `ItemObject PrimaryProduction` (highest value×rate); `Productions` list | VERIFIED | |
| `Village.VillageState` | State | `VillageStates { Normal, BeingRaided, ForcedForVolunteers, ForcedForSupplies, Looted }` | VERIFIED | |
| `Village.Bound` / `TradeBound` | Parent town | `Settlement Bound`, `Settlement TradeBound` | VERIFIED | |
| `Town.Workshops` | Guild shares | `Workshop[] Workshops` | VERIFIED | |
| `Workshop.WorkshopType` | Guild type | `WorkshopType WorkshopType` (`.StringId`, `.IsHidden`); null for an empty slot | VERIFIED | Hidden type `artisans` skipped via `IsHidden` |
| Workshop profit | Guild dividends | `int Workshop.ProfitMade` (= max(Capital−InitialCapital,0)), `int Capital`, `int Expense`; `Campaign.Current.Models.ClanFinanceModel.CalculateOwnerIncomeFromWorkshop(Workshop)` | VERIFIED | ADR-010: daily owner income from the finance model |
| `Kingdom.All`, `IsEliminated` | Kingdom indices | `static MBReadOnlyList<Kingdom> All`, `bool IsEliminated`, `MBReadOnlyList<Town> Fiefs`, `MBReadOnlyList<Settlement> Settlements` | VERIFIED | |
| `FactionManager.IsAtWarAgainstFaction` | War state | `static bool IsAtWarAgainstFaction(IFaction, IFaction)` | VERIFIED | `IFaction.IsAtWarWith` also used by vanilla |
| `Clan.PlayerClan.Tier` | Position limits | `static Clan PlayerClan`, `int Tier` | VERIFIED | |
| `Hero.GetSkillValue` | Fee discount | `int GetSkillValue(SkillObject)`; `DefaultSkills.Trade` | VERIFIED | |
| Caravans | Commenda, insurance | `MobileParty.All`, `MobileParty.AllCaravanParties`, `bool IsCaravan` | VERIFIED | Phase 3 |
| `MobileParty.PartyTradeGold` | Commenda return | `int PartyTradeGold` | VERIFIED | Phase 3 |
| Bandits | Region danger | `MobileParty.AllBanditParties`, `bool IsBandit` | VERIFIED | Phase 3 |
| Map distance | News delay | `MapDistanceModel.GetDistance(Settlement from, Settlement to, bool isFromPort, bool isTargetingPort, MobileParty.NavigationType navigationCapability)` | CHANGED | Extra port/navigation args since naval update. Phase 4 |

## 4. Events

All are `static IMbEvent<...>` properties on `CampaignEvents`.

| API | Purpose | Real form | Status | Notes |
|---|---|---|---|---|
| `VillageLooted` | Raid shock | `IMbEvent<Village>` | VERIFIED | Fired from `Village.VillageState` setter. Also `VillageBeingRaided` (`IMbEvent<Village>`), `VillageStateChanged` (`IMbEvent<Village, VillageStates, VillageStates, MobileParty>`), `RaidCompletedEvent` (`IMbEvent<BattleSideEnum, RaidEventComponent>`) |
| `OnSiegeEventStartedEvent` | Siege shock | `IMbEvent<SiegeEvent>` | VERIFIED | |
| `OnSiegeEventEndedEvent` | Siege end | `IMbEvent<SiegeEvent>` | VERIFIED | |
| `OnSettlementOwnerChangedEvent` | Town captured | `IMbEvent<Settlement, bool, Hero, Hero, Hero, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail>` | VERIFIED | |
| `WarDeclared` | War shock | `IMbEvent<IFaction, IFaction, DeclareWarAction.DeclareWarDetail>` | VERIFIED | |
| `MakePeace` | Peace shock | `IMbEvent<IFaction, IFaction, MakePeaceAction.MakePeaceDetail>` | VERIFIED | |
| `KingdomDestroyedEvent` | Close kingdom index | `IMbEvent<Kingdom>` | VERIFIED | |
| `MobilePartyDestroyed` | Caravan loss | `IMbEvent<MobileParty, PartyBase>` | VERIFIED | Phase 3 |
| `HeroKilledEvent` | Lord bond default | `IMbEvent<Hero, Hero, KillCharacterAction.KillCharacterActionDetail, bool>` | VERIFIED | Phase 3. Fires **after** `ChangeClanLeaderAction` and the ruling clan change, so the victim is no longer leader there |
| `BeforeHeroKilledEvent` | Ruler / clan leader death (ADR-032) | `IMbEvent<Hero, Hero, KillCharacterAction.KillCharacterActionDetail, bool>` (victim, killer, detail, notify) | VERIFIED | Fires before the leader change, so `Clan.Leader` / `Kingdom.Leader` still name the victim. Detail: `Murdered, DiedInLabor, DiedOfOldAge, DiedInBattle, WoundedInBattle, Executed, ExecutionAfterMapEvent, Lost` |
| `MapEventEnded` | Battle results (ADR-032) | `IMbEvent<MapEvent>` | VERIFIED | Fired at the end of `FinalizeEventAux`, after `BattleState` is set. `WinningSide` / `DefeatedSide` (`BattleSideEnum` in TaleWorlds.Core, `None` without winner), `GetMapEventSide(side)`, `MapEventSide.MapFaction`, `.TroopCasualties`, `IsRaid`, `MapEventSettlement` |
| `OnClanChangedKingdomEvent` | Clan defection / rebellion (ADR-032) | `IMbEvent<Clan, Kingdom, Kingdom, ChangeKingdomAction.ChangeKingdomActionDetail, bool>` (clan, old, new, detail, notify) | VERIFIED | Detail: `JoinAsMercenary, JoinKingdom, JoinKingdomByDefection, LeaveKingdom, LeaveWithRebellion, LeaveAsMercenary, LeaveByClanDestruction, CreateKingdom, LeaveByKingdomDestruction` |
| Peace terms | War winner (ADR-032) | `IFaction.GetStanceWith(other)` → `StanceLink.GetDailyTributeToPay(faction)` (> 0 = that faction pays) | VERIFIED | `MakePeaceAction` sets the tribute before firing `MakePeace`. StanceLink also has per-side `GetCasualties`, `GetSuccessfulSieges`, `GetSuccessfulRaids` (not used yet) |
| Kingdom readers | Index parts (ADR-032) | `Kingdom.Villages` (`MBReadOnlyList<Village>`), `Village.Hearth` (float), `Village.VillageState` (`Normal, BeingRaided, ForcedForVolunteers, ForcedForSupplies, Looted`), `Kingdom.CurrentTotalStrength` (float, sum of clans), `Kingdom.Clans`, `Clan.IsUnderMercenaryService`, `Clan.IsEliminated`, `Settlement.IsCastle` | VERIFIED | |
| `HeroPrisonerTaken` | Lord bond freeze; "captive" news (ADR-031) | `IMbEvent<PartyBase, Hero>` (capturer, prisoner) | VERIFIED | News filter: `Hero.IsLord` (`Occupation == Lord`) and `MapFaction.IsKingdomFaction`. Captor = `PartyBase.LeaderHero` (null for garrisons), `PartyBase.MapFaction`. `Hero.Find(stringId)` looks up via `CampaignObjectManager` |
| `PlayerInventoryExchangeEvent` | Player market impact | `IMbEvent<List<(ItemRosterElement, int)>, List<(ItemRosterElement, int)>, bool>` | VERIFIED | Phase 4: total vs unit price still to check in `InventoryLogic` |
| Season change | Seasonal shock | — | MISSING | No event; detect `GetSeasonOfYear` change in daily tick (as planned) |
| Famine | Food stocks hit zero | — | MISSING | No event; detect from `FoodStocks <= 0` in daily tick (as planned) |

## 5. Menus and UI

| API | Purpose | Real form | Status | Notes |
|---|---|---|---|---|
| `CampaignGameStarter.AddGameMenuOption` | Town menu option | `void AddGameMenuOption(string menuId, string optionId, string optionText, GameMenuOption.OnConditionDelegate condition, GameMenuOption.OnConsequenceDelegate consequence, bool isLeave = false, int index = -1, bool isRepeatable = false, object relatedObject = null)` | VERIFIED | Town menu id is `"town"` (`PlayerTownVisitCampaignBehavior`) |
| `CampaignGameStarter.AddGameMenu` | Exchange submenu | `void AddGameMenu(string menuId, string menuText, OnInitDelegate initDelegate, GameMenu.MenuOverlayType overlay = None, GameMenu.MenuFlags menuFlags = None, object relatedObject = null)` | VERIFIED | Town uses overlay `SettlementWithBoth` |
| `GameMenu.SwitchToMenu` | Navigate | `static void SwitchToMenu(string menuId)` | VERIFIED | Also `ActivateGameMenu`, `ExitToLast` |
| Delegates | | `bool OnConditionDelegate(MenuCallbackArgs)`, `void OnConsequenceDelegate(MenuCallbackArgs)`; `args.optionLeaveType`, `args.IsEnabled`, `args.Tooltip` | VERIFIED | |
| `GameMenuOption.LeaveType` | Option icons | enum incl. `Trade`, `Submenu`, `Leave`, `Manage`, `Warehouse` | VERIFIED | Use `Trade` |
| `InformationManager.ShowInquiry` | Confirmations | `static void ShowInquiry(InquiryData data, bool pauseGameActiveState = false, bool prioritize = false)`; ctor `InquiryData(string titleText, string text, bool isAffirmativeOptionShown, bool isNegativeOptionShown, string affirmativeText, string negativeText, Action affirmativeAction, Action negativeAction, ...)` | VERIFIED | `TaleWorlds.Library` |
| `MBInformationManager.ShowMultiSelectionInquiry` | Pick instrument | `static void ShowMultiSelectionInquiry(MultiSelectionInquiryData data, bool pauseGameActiveState = false, bool prioritize = false)`; `InquiryElement(object identifier, string title, ImageIdentifier imageIdentifier[, bool isEnabled, string hint])` | VERIFIED | Namespace `TaleWorlds.Core` |
| `InformationManager.ShowTextInquiry` | Quantity input | `static void ShowTextInquiry(TextInquiryData textData, bool pause = false, bool prioritize = false)`; ctor has `Func<string, Tuple<bool,string>> textCondition` | VERIFIED | |
| `InformationManager.DisplayMessage` | Notifications | `static void DisplayMessage(InformationMessage message)`; `InformationMessage(string)`, `(string, Color)` | VERIFIED | |
| `TextObject` | Localization | `TextObject(string value, Dictionary<string, object> attributes = null)`, `SetTextVariable(string, string/int/float)` | VERIFIED | |
| Gauntlet | Phase 2 screen | `TaleWorlds.GauntletUI*.dll`, `TaleWorlds.Engine.GauntletUI.dll`, `TaleWorlds.ScreenSystem.dll` present | DEFERRED | Verify in Phase 2.6 spike |
| Gauntlet data binding of layout values | Chart slices (ADR-031) | `WidgetExtensions.SetWidgetAttribute` → `ConvertObject` only converts **strings** (to Sprite, Brush, int, Color); other values are set as-is by reflection | VERIFIED | So a VM property bound to `MarginTop` / `SuggestedHeight` / `PositionYOffset` must be `float` exactly. Vanilla binds `PositionYOffset="@CursorPositionY"` |
| `DimensionSyncWidget` | Popup that sizes to its content | `DimensionToSync="Vertical" WidgetToCopyHeightFrom="<child id>"` | VERIFIED | Used by vanilla `SingleQueryPopup.xml`; our `CxConfirmPopup.xml` |
| Search box | Commodity market search | `BrushWidget Brush="SaveLoad.Search.Button"` + `EditableTextWidget Brush="SaveLoad.Search.InputText" DefaultSearchText="@..." Text="@SearchText"` | VERIFIED | Same markup as vanilla `MultiSelectionQueryPopup.xml`; brushes are in SandBox (loaded in campaign) |

## 6. Money and skills

| API | Purpose | Real form | Status | Notes |
|---|---|---|---|---|
| `Hero.MainHero.Gold` | Read gold | `static Hero MainHero`, `int Gold` | VERIFIED | |
| `GiveGoldAction.ApplyBetweenCharacters` | Pay / receive | `static void ApplyBetweenCharacters(Hero giverHero, Hero recipientHero, int amount, bool disableNotification = false)` | VERIFIED | Also `Hero.ChangeHeroGold(int)` |
| `Hero.AddSkillXp` | Trade XP | `void AddSkillXp(SkillObject skill, float xpAmount)` | VERIFIED | Phase 4 |

## 7. Console commands

| API | Purpose | Real form | Status | Notes |
|---|---|---|---|---|
| `CommandLineFunctionality.CommandLineArgumentFunction` | Debug commands | `[CommandLineFunctionality.CommandLineArgumentFunction(string name, string groupname)]` on `public static string M(List<string> args)` | VERIFIED | Discovered by scanning every loaded assembly that references TaleWorlds.Library. Invoked as `groupname.name` |

## 8. Game data IDs (verified v1.4.8)

Sources: `Modules\SandBoxCore\ModuleData\items\horses_and_others.xml`, `TaleWorlds.CampaignSystem.DefaultItems` (code-defined items), `TaleWorlds.Core.DefaultItemCategories`, `Modules\SandBox\ModuleData\spworkshops.xml`.

**Trade goods (item id = category id unless noted):** `grain`, `fish`, `meat`, `cheese`, `butter`, `olives`, `date_fruit`, `grape`, `wine`, `beer`, `oil`, `salt`, `iron` (Iron Ore; category `iron` also holds ingots), `tools`, `clay`, `pottery`, `flax`, `linen`, `cotton` (shown as "Raw Silk"), `wool`, `felt`, `velvet`, `hides`, `leather`, `fur`, `silver` (Silver Ore), `jewelry`, `hardwood` (category id `hardwood`), `planks`. `spice` exists but has no category (smuggling item) — not used. **`cloth` is not a trade good** (ADR-011).

**Military item categories:** `melee_weapons`, `melee_weapons_2..5`, `ranged_weapons`, `ranged_weapons_2..5`, `arrows`, `shield`, `shield_2..5`, `garment`, `light_armor`, `medium_armor`, `heavy_armor`, `ultra_armor`, `horse_equipment`, `horse_equipment_2..5`, `horse`, `war_horse`, `noble_horse`, `sumpter_horse`.

**Workshop types:** `brewery`, `velvet_weavery`, `linen_weavery`, `wine_press`, `pottery_shop`, `olive_press`, `wool_weavery`, `tannery`, `wood_WorkshopType` (sic), `smithy`, `silversmithy`. Hidden: `artisans` (skip). `stable` is commented out in the XML — not in the game.

## 8b. Additional APIs used by the Phase 1C game layer (verified v1.4.8)

| API | Real form | Status | Notes |
|---|---|---|---|
| `MBObjectManager.Instance.GetObject<T>(string)` | `T GetObject<T>(string objectName) where T : MBObjectBase` | VERIFIED | Item and item category lookup by StringId |
| `ItemObject.Value`, `ItemObject.ItemCategory`, `ItemObject.Name` | `int`, `ItemCategory`, `TextObject` | VERIFIED | |
| `TownMarketData.GetCategoryData(ItemCategory)` | returns `ItemData` struct (`Supply`, `Demand`, `InStore`, `InStoreValue`) | VERIFIED | Missing category = default struct; its price factor is the 0.1 floor, so WorldReader skips towns without data |
| `Village.Settlement`, `Village.Bound`, `Village.VillageType` | `Settlement`, `Settlement`, field `VillageType` | VERIFIED | |
| `SiegeEvent.BesiegedSettlement` | `Settlement` | VERIFIED | Castles filtered out with `Settlement.IsTown` |
| `Hero.MapFaction`, `IFaction.IsKingdomFaction`, `IFaction.StringId` | | VERIFIED | Wars with bandits/minor factions ignored |
| `GiveGoldAction.ApplyBetweenCharacters(giver, null, ...)` | null recipient/giver = gold leaves/enters the world | VERIFIED | Amount is clamped to the giver's gold |
| `InquiryElement(object, string, ImageIdentifier, bool, string)` | null image is allowed (`GenericImageIdentifierVM` uses `EmptyImageIdentifier`) | VERIFIED | Commodity rows use `ItemImageIdentifier(item)` (`TaleWorlds.Core.ImageIdentifiers`) |
| `MBTextManager.SetTextVariable(string, string)` | | VERIFIED | Menu text `{=!}{CX_EXCHANGE_TEXT}` |
| `Campaign.Current.CurrentMenuContext.GameMenu.StringId` | | VERIFIED | Used to refresh the exchange menu after a trade |
| `Colors.Green/Red/Yellow` | `static readonly Color` | VERIFIED | |
| `MenuContext.SetBackgroundMeshName(string)` | name is a Gauntlet **sprite** name (`GameMenuWidget Sprite="@Background"`, `GameMenuVM.Background`) | VERIFIED | Exchange menu background `cx_exchange_bg` |
| `UIResourceManager.SpriteData.Sprites` | `Dictionary<string, Sprite>`, looked up by `SpriteData.GetSprite(name)` | VERIFIED | We add our runtime sprite here |
| `UIResourceManager.BrushFactory.GetBrush(name)`, `BrushLayer.Sprite/Color`, `Style.GetLayer(name)` → `StyleLayer.Sprite/Color/ColorFactor` | settable at runtime; `StyleLayer` falls back to its brush layer unless set | VERIFIED (signature) | `UI/ButtonSkins` |
| `ChangeRelationAction.ApplyPlayerRelation(Hero, int, bool affectRelatives = true, bool showQuickNotification = true)` | | VERIFIED (signature) | Guild debt penalty (ADR-027) |
| `MBInformationManager.AddQuickInformation(TextObject, int extraTimeInMs = 0, BasicCharacterObject announcer = null, Equipment = null, string sound = "")` | top-of-screen panel with portrait | VERIFIED (signature) | Debt penalty warning |
| Save strings (`SaveContext.SaveStringTo`) | entry size written as `short` (4 + UTF-8 bytes) | VERIFIED | **A string > 32767 bytes corrupts the save.** State goes through `SaveChunks` (ADR-028) |
| `IDataStore.SyncData<T>(key, ref data)` on load | missing key returns false, leaves `data` | VERIFIED | Chunk keys `cx_state_v1_N` |
| `abstract Sprite(string, int, int, SpriteNinePatchParameters)` + `Texture`, `GetMinUvs`, `GetMaxUvs` | subclassable | VERIFIED | `UI/TextureSprite` |
| `TaleWorlds.Engine.Texture.LoadTextureFromPath(fileName, folder)`, `CreateFromMemory(byte[])` | native; no vanilla caller | VERIFIED (signature) | PNG loads from path. **24-bit RGB PNGs show red/blue swapped; 32-bit RGBA PNGs are correct** (in-game test). Deploy writes 32-bit RGBA |

## 9. After every game update

1. Re-run the decompile.
2. Re-check every `VERIFIED` row used by shipped features; diff signatures.
3. Run Core tests and the simulator.
4. Smoke test in game: open exchange, buy, sell, save, reload, advance 10 days.
