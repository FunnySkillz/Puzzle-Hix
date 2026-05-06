using System;
using System.Collections.Generic;
using System.Globalization;
using PuzzleDungeon.Gameplay.Match3;
using UnityEngine;

namespace PuzzleDungeon.Production
{
    public static class ProductionPolicy
    {
        public const string ProductDirection = "Android-first hybrid-casual cozy dungeon match-3";
        public const string BudgetRule = "0 EUR until soft-launch readiness";
        public const bool PaidRandomRewardsAllowed = false;
        public const bool HardLivesEnabledAtLaunch = false;
        public const bool InterstitialAdsEnabledAtLaunch = false;
    }

    public sealed class AnalyticsEvent
    {
        private readonly Dictionary<string, string> properties;

        public AnalyticsEvent(string name, IReadOnlyDictionary<string, string> eventProperties = null)
        {
            Name = string.IsNullOrWhiteSpace(name) ? "unknown_event" : name.Trim();
            properties = eventProperties != null
                ? new Dictionary<string, string>(eventProperties)
                : new Dictionary<string, string>();
        }

        public string Name { get; }
        public IReadOnlyDictionary<string, string> Properties => properties;
    }

    public interface IAnalyticsService
    {
        void TrackEvent(AnalyticsEvent analyticsEvent);
        void TrackEvent(string eventName, IReadOnlyDictionary<string, string> properties = null);
    }

    public sealed class NoOpAnalyticsService : IAnalyticsService
    {
        public void TrackEvent(AnalyticsEvent analyticsEvent)
        {
        }

        public void TrackEvent(string eventName, IReadOnlyDictionary<string, string> properties = null)
        {
        }
    }

    public enum AdPlacementId
    {
        FailExtraMoves,
        DailyRewardDouble,
        FreeBooster,
        BonusCoins
    }

    public enum AdShowResult
    {
        NotAvailable,
        Completed,
        Skipped,
        Failed
    }

    public interface IAdService
    {
        bool InterstitialsEnabled { get; }
        bool IsRewardedAvailable(AdPlacementId placementId);
        AdShowResult ShowRewarded(AdPlacementId placementId);
        AdShowResult ShowInterstitial(string placementId);
    }

    public sealed class MockAdService : IAdService
    {
        private readonly HashSet<AdPlacementId> disabledRewardedPlacements = new HashSet<AdPlacementId>();

        public MockAdService(bool rewardedAdsAvailable = true, bool interstitialsEnabled = false)
        {
            RewardedAdsAvailable = rewardedAdsAvailable;
            InterstitialsEnabled = interstitialsEnabled;
        }

        public bool RewardedAdsAvailable { get; set; }
        public bool InterstitialsEnabled { get; set; }
        public AdShowResult NextRewardedResult { get; set; } = AdShowResult.Completed;

        public void SetRewardedPlacementEnabled(AdPlacementId placementId, bool enabled)
        {
            if (enabled)
            {
                disabledRewardedPlacements.Remove(placementId);
                return;
            }

            disabledRewardedPlacements.Add(placementId);
        }

        public bool IsRewardedAvailable(AdPlacementId placementId)
        {
            return RewardedAdsAvailable && !disabledRewardedPlacements.Contains(placementId);
        }

        public AdShowResult ShowRewarded(AdPlacementId placementId)
        {
            return IsRewardedAvailable(placementId) ? NextRewardedResult : AdShowResult.NotAvailable;
        }

        public AdShowResult ShowInterstitial(string placementId)
        {
            return InterstitialsEnabled ? AdShowResult.Completed : AdShowResult.NotAvailable;
        }
    }

    public enum IapProductId
    {
        RemoveAds,
        StarterPack,
        SmallCoinPack,
        MediumCoinPack,
        LargeCoinPack,
        BoosterBundle,
        CosmeticBundle
    }

    public enum PurchaseStatus
    {
        Succeeded,
        Cancelled,
        Failed,
        Unavailable
    }

    public sealed class PurchaseResult
    {
        private PurchaseResult(IapProductId productId, PurchaseStatus status, string message)
        {
            ProductId = productId;
            Status = status;
            Message = message ?? string.Empty;
        }

        public IapProductId ProductId { get; }
        public PurchaseStatus Status { get; }
        public string Message { get; }
        public bool WasSuccessful => Status == PurchaseStatus.Succeeded;

        public static PurchaseResult Succeeded(IapProductId productId)
        {
            return new PurchaseResult(productId, PurchaseStatus.Succeeded, string.Empty);
        }

        public static PurchaseResult Cancelled(IapProductId productId)
        {
            return new PurchaseResult(productId, PurchaseStatus.Cancelled, "Purchase cancelled.");
        }

        public static PurchaseResult Failed(IapProductId productId, string message)
        {
            return new PurchaseResult(productId, PurchaseStatus.Failed, message);
        }

        public static PurchaseResult Unavailable(IapProductId productId)
        {
            return new PurchaseResult(productId, PurchaseStatus.Unavailable, "Purchases are not available.");
        }
    }

    public interface IIapService
    {
        bool IsReady { get; }
        bool IsOwned(IapProductId productId);
        PurchaseResult Purchase(IapProductId productId);
        void RestorePurchases();
    }

    public sealed class MockIapService : IIapService
    {
        private readonly HashSet<IapProductId> ownedProducts = new HashSet<IapProductId>();

        public MockIapService(bool isReady = true)
        {
            IsReady = isReady;
        }

        public bool IsReady { get; set; }
        public PurchaseStatus NextPurchaseStatus { get; set; } = PurchaseStatus.Succeeded;

        public bool IsOwned(IapProductId productId)
        {
            return ownedProducts.Contains(productId);
        }

        public PurchaseResult Purchase(IapProductId productId)
        {
            if (!IsReady)
            {
                return PurchaseResult.Unavailable(productId);
            }

            if (NextPurchaseStatus == PurchaseStatus.Cancelled)
            {
                return PurchaseResult.Cancelled(productId);
            }

            if (NextPurchaseStatus == PurchaseStatus.Failed)
            {
                return PurchaseResult.Failed(productId, "Mock purchase failed.");
            }

            if (IsNonConsumable(productId))
            {
                ownedProducts.Add(productId);
            }

            return PurchaseResult.Succeeded(productId);
        }

        public void RestorePurchases()
        {
        }

        private static bool IsNonConsumable(IapProductId productId)
        {
            return productId == IapProductId.RemoveAds || productId == IapProductId.CosmeticBundle;
        }
    }

    public sealed class ConsentState
    {
        public ConsentState(bool analyticsAllowed, bool personalizedAdsAllowed)
        {
            AnalyticsAllowed = analyticsAllowed;
            PersonalizedAdsAllowed = personalizedAdsAllowed;
        }

        public bool AnalyticsAllowed { get; }
        public bool PersonalizedAdsAllowed { get; }
    }

    public interface IConsentService
    {
        ConsentState Current { get; }
        void SetConsent(bool analyticsAllowed, bool personalizedAdsAllowed);
    }

    public sealed class LocalConsentService : IConsentService
    {
        public LocalConsentService()
        {
            Current = new ConsentState(false, false);
        }

        public ConsentState Current { get; private set; }

        public void SetConsent(bool analyticsAllowed, bool personalizedAdsAllowed)
        {
            Current = new ConsentState(analyticsAllowed, personalizedAdsAllowed);
        }
    }

    public interface IRemoteConfigService
    {
        bool GetBool(string key, bool defaultValue);
        int GetInt(string key, int defaultValue);
        float GetFloat(string key, float defaultValue);
        string GetString(string key, string defaultValue);
    }

    public sealed class LocalRemoteConfigService : IRemoteConfigService
    {
        private readonly Dictionary<string, string> values = new Dictionary<string, string>();

        public void SetBool(string key, bool value)
        {
            SetValue(key, value ? "true" : "false");
        }

        public void SetInt(string key, int value)
        {
            SetValue(key, value.ToString(CultureInfo.InvariantCulture));
        }

        public void SetFloat(string key, float value)
        {
            SetValue(key, value.ToString(CultureInfo.InvariantCulture));
        }

        public void SetString(string key, string value)
        {
            SetValue(key, value);
        }

        public bool GetBool(string key, bool defaultValue)
        {
            return values.TryGetValue(SanitizeKey(key), out string value) && bool.TryParse(value, out bool parsed)
                ? parsed
                : defaultValue;
        }

        public int GetInt(string key, int defaultValue)
        {
            return values.TryGetValue(SanitizeKey(key), out string value) && int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed)
                ? parsed
                : defaultValue;
        }

        public float GetFloat(string key, float defaultValue)
        {
            return values.TryGetValue(SanitizeKey(key), out string value) && float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed)
                ? parsed
                : defaultValue;
        }

        public string GetString(string key, string defaultValue)
        {
            return values.TryGetValue(SanitizeKey(key), out string value) ? value : defaultValue;
        }

        private void SetValue(string key, string value)
        {
            values[SanitizeKey(key)] = value ?? string.Empty;
        }

        private static string SanitizeKey(string key)
        {
            return string.IsNullOrWhiteSpace(key) ? "missing_key" : key.Trim();
        }
    }

    public enum CurrencyType
    {
        Coins
    }

    public enum EconomyTransactionReason
    {
        LevelReward,
        DailyReward,
        AdReward,
        Purchase,
        BoosterSpend,
        DebugGrant
    }

    public sealed class EconomyService
    {
        public int Coins { get; private set; }

        public bool GrantCoins(int amount, EconomyTransactionReason reason)
        {
            if (amount <= 0)
            {
                return false;
            }

            Coins += amount;
            return true;
        }

        public bool TrySpendCoins(int amount, EconomyTransactionReason reason)
        {
            if (amount <= 0 || amount > Coins)
            {
                return false;
            }

            Coins -= amount;
            return true;
        }

        public void Reset()
        {
            Coins = 0;
        }
    }

    public enum BoosterType
    {
        Hammer,
        Shuffle,
        LineBlast,
        ColorBlast,
        ExtraMoves
    }

    public enum CosmeticSlot
    {
        PieceSkin,
        BoardFrame,
        MapTheme,
        WinBadge,
        ProfileFrame
    }

    public sealed class InventoryService
    {
        private readonly Dictionary<BoosterType, int> boosterCounts = new Dictionary<BoosterType, int>();
        private readonly HashSet<string> ownedCosmetics = new HashSet<string>();

        public int GetBoosterCount(BoosterType boosterType)
        {
            return boosterCounts.TryGetValue(boosterType, out int count) ? Mathf.Max(0, count) : 0;
        }

        public void AddBooster(BoosterType boosterType, int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            boosterCounts[boosterType] = GetBoosterCount(boosterType) + amount;
        }

        public bool TryConsumeBooster(BoosterType boosterType, int amount = 1)
        {
            if (amount <= 0 || GetBoosterCount(boosterType) < amount)
            {
                return false;
            }

            boosterCounts[boosterType] = GetBoosterCount(boosterType) - amount;
            return true;
        }

        public bool UnlockCosmetic(string cosmeticId)
        {
            if (string.IsNullOrWhiteSpace(cosmeticId))
            {
                return false;
            }

            return ownedCosmetics.Add(cosmeticId.Trim());
        }

        public bool OwnsCosmetic(string cosmeticId)
        {
            return !string.IsNullOrWhiteSpace(cosmeticId) && ownedCosmetics.Contains(cosmeticId.Trim());
        }
    }

    public sealed class BoosterService
    {
        private readonly InventoryService inventoryService;

        public BoosterService(InventoryService inventory)
        {
            inventoryService = inventory ?? new InventoryService();
        }

        public bool CanUseBooster(BoosterType boosterType)
        {
            return inventoryService.GetBoosterCount(boosterType) > 0;
        }

        public bool TryUseBooster(BoosterType boosterType)
        {
            return inventoryService.TryConsumeBooster(boosterType);
        }
    }

    public sealed class CosmeticService
    {
        public const string DefaultCosmeticId = "default";

        private readonly InventoryService inventoryService;
        private readonly Dictionary<CosmeticSlot, string> equippedCosmetics = new Dictionary<CosmeticSlot, string>();

        public CosmeticService(InventoryService inventory)
        {
            inventoryService = inventory ?? new InventoryService();
        }

        public string GetEquippedCosmetic(CosmeticSlot slot)
        {
            return equippedCosmetics.TryGetValue(slot, out string cosmeticId) ? cosmeticId : DefaultCosmeticId;
        }

        public bool TryEquipCosmetic(CosmeticSlot slot, string cosmeticId)
        {
            if (string.IsNullOrWhiteSpace(cosmeticId) || cosmeticId.Trim() == DefaultCosmeticId)
            {
                equippedCosmetics[slot] = DefaultCosmeticId;
                return true;
            }

            if (!inventoryService.OwnsCosmetic(cosmeticId))
            {
                return false;
            }

            equippedCosmetics[slot] = cosmeticId.Trim();
            return true;
        }
    }

    public sealed class LifeService
    {
        public bool IsEnabled => ProductionPolicy.HardLivesEnabledAtLaunch;
        public int Lives => int.MaxValue;
        public bool CanStartLevel => true;

        public bool TryConsumeLife()
        {
            return true;
        }
    }

    public enum StoreProductCategory
    {
        RemoveAds,
        Coins,
        BoosterBundle,
        StarterPack,
        Cosmetic
    }

    public sealed class StoreProductDefinition
    {
        public StoreProductDefinition(IapProductId productId, StoreProductCategory category, string storeId, string displayName, bool isConsumable)
        {
            ProductId = productId;
            Category = category;
            StoreId = string.IsNullOrWhiteSpace(storeId) ? productId.ToString() : storeId.Trim();
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? productId.ToString() : displayName.Trim();
            IsConsumable = isConsumable;
        }

        public IapProductId ProductId { get; }
        public StoreProductCategory Category { get; }
        public string StoreId { get; }
        public string DisplayName { get; }
        public bool IsConsumable { get; }
        public bool IsRandomizedReward => false;
    }

    public static class ProductionCatalogDefaults
    {
        public static StoreProductDefinition[] CreateStoreProducts()
        {
            return new[]
            {
                new StoreProductDefinition(IapProductId.RemoveAds, StoreProductCategory.RemoveAds, "remove_ads", "Remove Ads", false),
                new StoreProductDefinition(IapProductId.StarterPack, StoreProductCategory.StarterPack, "starter_pack", "Starter Pack", true),
                new StoreProductDefinition(IapProductId.SmallCoinPack, StoreProductCategory.Coins, "small_coin_pack", "Small Coin Pack", true),
                new StoreProductDefinition(IapProductId.MediumCoinPack, StoreProductCategory.Coins, "medium_coin_pack", "Medium Coin Pack", true),
                new StoreProductDefinition(IapProductId.LargeCoinPack, StoreProductCategory.Coins, "large_coin_pack", "Large Coin Pack", true),
                new StoreProductDefinition(IapProductId.BoosterBundle, StoreProductCategory.BoosterBundle, "booster_bundle", "Booster Bundle", true),
                new StoreProductDefinition(IapProductId.CosmeticBundle, StoreProductCategory.Cosmetic, "cosmetic_bundle", "Cosmetic Bundle", false)
            };
        }
    }

    [CreateAssetMenu(fileName = "EconomyCatalog", menuName = "PuzzleDungeon/Production/Economy Catalog")]
    public sealed class EconomyCatalog : ScriptableObject
    {
        [SerializeField] private int levelWinCoins = 25;
        [SerializeField] private int threeStarBonusCoins = 15;
        [SerializeField] private int dailyRewardCoins = 50;
        [SerializeField] private int rewardedAdBonusCoins = 30;

        public int LevelWinCoins => Mathf.Max(0, levelWinCoins);
        public int ThreeStarBonusCoins => Mathf.Max(0, threeStarBonusCoins);
        public int DailyRewardCoins => Mathf.Max(0, dailyRewardCoins);
        public int RewardedAdBonusCoins => Mathf.Max(0, rewardedAdBonusCoins);
    }

    [CreateAssetMenu(fileName = "BoosterData", menuName = "PuzzleDungeon/Production/Booster Data")]
    public sealed class BoosterData : ScriptableObject
    {
        [SerializeField] private BoosterType boosterType = BoosterType.Hammer;
        [SerializeField] private string displayName = "Hammer";
        [SerializeField] private int coinCost = 120;
        [SerializeField] private int startingGrant;

        public BoosterType BoosterType => boosterType;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? boosterType.ToString() : displayName;
        public int CoinCost => Mathf.Max(0, coinCost);
        public int StartingGrant => Mathf.Max(0, startingGrant);
    }

    [CreateAssetMenu(fileName = "CosmeticSkinData", menuName = "PuzzleDungeon/Production/Cosmetic Skin Data")]
    public sealed class CosmeticSkinData : ScriptableObject
    {
        [SerializeField] private string cosmeticId = "default";
        [SerializeField] private CosmeticSlot slot = CosmeticSlot.PieceSkin;
        [SerializeField] private string displayName = "Default";
        [SerializeField] private bool isEarnable = true;
        [SerializeField] private bool isPurchasable;

        public string CosmeticId => string.IsNullOrWhiteSpace(cosmeticId) ? CosmeticService.DefaultCosmeticId : cosmeticId.Trim();
        public CosmeticSlot Slot => slot;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? CosmeticId : displayName;
        public bool IsEarnable => isEarnable;
        public bool IsPurchasable => isPurchasable;
    }

    [CreateAssetMenu(fileName = "StoreProductData", menuName = "PuzzleDungeon/Production/Store Product Data")]
    public sealed class StoreProductData : ScriptableObject
    {
        [SerializeField] private IapProductId productId = IapProductId.RemoveAds;
        [SerializeField] private StoreProductCategory category = StoreProductCategory.RemoveAds;
        [SerializeField] private string storeId = "remove_ads";
        [SerializeField] private string displayName = "Remove Ads";
        [SerializeField] private bool isConsumable;

        public IapProductId ProductId => productId;
        public StoreProductCategory Category => category;
        public string StoreId => string.IsNullOrWhiteSpace(storeId) ? productId.ToString() : storeId.Trim();
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? productId.ToString() : displayName;
        public bool IsConsumable => isConsumable;
        public bool IsRandomizedReward => false;
    }

    [CreateAssetMenu(fileName = "AdPlacementConfig", menuName = "PuzzleDungeon/Production/Ad Placement Config")]
    public sealed class AdPlacementConfig : ScriptableObject
    {
        [SerializeField] private AdPlacementId placementId = AdPlacementId.FailExtraMoves;
        [SerializeField] private bool rewardedEnabled = true;
        [SerializeField] private int rewardAmount = 5;
        [SerializeField] private int cooldownSeconds = 30;

        public AdPlacementId PlacementId => placementId;
        public bool RewardedEnabled => rewardedEnabled;
        public int RewardAmount => Mathf.Max(0, rewardAmount);
        public int CooldownSeconds => Mathf.Max(0, cooldownSeconds);
    }

    [CreateAssetMenu(fileName = "RemoteTuningConfig", menuName = "PuzzleDungeon/Production/Remote Tuning Config")]
    public sealed class RemoteTuningConfig : ScriptableObject
    {
        [SerializeField] private bool rewardedAdsEnabled;
        [SerializeField] private bool interstitialAdsEnabled;
        [SerializeField] private int failExtraMovesReward = 5;
        [SerializeField] private int maxInterstitialsPerSession;
        [SerializeField] private int dailyRewardCoins = 50;

        public bool RewardedAdsEnabled => rewardedAdsEnabled;
        public bool InterstitialAdsEnabled => interstitialAdsEnabled && ProductionPolicy.InterstitialAdsEnabledAtLaunch;
        public int FailExtraMovesReward => Mathf.Max(1, failExtraMovesReward);
        public int MaxInterstitialsPerSession => Mathf.Max(0, maxInterstitialsPerSession);
        public int DailyRewardCoins => Mathf.Max(0, dailyRewardCoins);
    }

    [CreateAssetMenu(fileName = "DailyRewardCalendar", menuName = "PuzzleDungeon/Production/Daily Reward Calendar")]
    public sealed class DailyRewardCalendar : ScriptableObject
    {
        [SerializeField] private int[] coinRewards = { 25, 30, 35, 40, 50, 60, 100 };

        public int RewardCount => coinRewards != null ? coinRewards.Length : 0;

        public int GetCoinReward(int dayIndex)
        {
            if (coinRewards == null || coinRewards.Length == 0)
            {
                return 0;
            }

            int clampedIndex = Mathf.Clamp(dayIndex, 0, coinRewards.Length - 1);
            return Mathf.Max(0, coinRewards[clampedIndex]);
        }
    }

    public enum MissionType
    {
        CompleteLevels,
        EarnStars,
        ClearPieces,
        CreateSpecialPieces,
        WatchRewardedAd
    }

    [CreateAssetMenu(fileName = "MissionData", menuName = "PuzzleDungeon/Production/Mission Data")]
    public sealed class MissionData : ScriptableObject
    {
        [SerializeField] private MissionType missionType = MissionType.CompleteLevels;
        [SerializeField] private int targetAmount = 3;
        [SerializeField] private int coinReward = 50;

        public MissionType MissionType => missionType;
        public int TargetAmount => Mathf.Max(1, targetAmount);
        public int CoinReward => Mathf.Max(0, coinReward);
    }

    [CreateAssetMenu(fileName = "LevelPackCatalog", menuName = "PuzzleDungeon/Production/Level Pack Catalog")]
    public sealed class LevelPackCatalog : ScriptableObject
    {
        [SerializeField] private string packId = "main_001";
        [SerializeField] private string displayName = "Dungeon Path";
        [SerializeField] private Match3LevelCatalog levelCatalog;
        [SerializeField] private int firstLevelNumber = 1;
        [SerializeField] private int lastLevelNumber = 60;

        public string PackId => string.IsNullOrWhiteSpace(packId) ? "main_001" : packId.Trim();
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? PackId : displayName;
        public Match3LevelCatalog LevelCatalog => levelCatalog;
        public int FirstLevelNumber => Mathf.Max(1, firstLevelNumber);
        public int LastLevelNumber => Mathf.Max(FirstLevelNumber, lastLevelNumber);
    }
}
