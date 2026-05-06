using System.Linq;
using NUnit.Framework;
using PuzzleDungeon.Production;
using UnityEngine;

namespace PuzzleDungeon.Tests.EditMode
{
    public class ProductionFoundationTests
    {
        [Test]
        public void ProductionPolicy_LocksZeroCostFairMonetizationDefaults()
        {
            Assert.That(ProductionPolicy.BudgetRule, Does.Contain("0 EUR"));
            Assert.That(ProductionPolicy.PaidRandomRewardsAllowed, Is.False);
            Assert.That(ProductionPolicy.HardLivesEnabledAtLaunch, Is.False);
            Assert.That(ProductionPolicy.InterstitialAdsEnabledAtLaunch, Is.False);
        }

        [Test]
        public void DefaultStoreProducts_AreDirectPurchasesWithoutRandomizedRewards()
        {
            StoreProductDefinition[] products = ProductionCatalogDefaults.CreateStoreProducts();

            Assert.That(products.Select(product => product.ProductId).Distinct().Count(), Is.EqualTo(products.Length));
            Assert.That(products.Any(product => product.ProductId == IapProductId.RemoveAds), Is.True);
            Assert.That(products.Any(product => product.ProductId == IapProductId.BoosterBundle), Is.True);
            Assert.That(products.Any(product => product.ProductId == IapProductId.CosmeticBundle), Is.True);
            Assert.That(products.All(product => !product.IsRandomizedReward), Is.True);
        }

        [Test]
        public void MockAdService_UsesOptionalRewardedAdsAndKeepsInterstitialsOffByDefault()
        {
            MockAdService ads = new MockAdService();

            Assert.That(ads.InterstitialsEnabled, Is.False);
            Assert.That(ads.IsRewardedAvailable(AdPlacementId.FailExtraMoves), Is.True);
            Assert.That(ads.ShowRewarded(AdPlacementId.FailExtraMoves), Is.EqualTo(AdShowResult.Completed));
            Assert.That(ads.ShowInterstitial("level_end"), Is.EqualTo(AdShowResult.NotAvailable));

            ads.SetRewardedPlacementEnabled(AdPlacementId.FailExtraMoves, false);

            Assert.That(ads.IsRewardedAvailable(AdPlacementId.FailExtraMoves), Is.False);
            Assert.That(ads.ShowRewarded(AdPlacementId.FailExtraMoves), Is.EqualTo(AdShowResult.NotAvailable));
        }

        [Test]
        public void MockIapService_HandlesSuccessCancelUnavailableAndOwnership()
        {
            MockIapService iap = new MockIapService();

            PurchaseResult removeAds = iap.Purchase(IapProductId.RemoveAds);
            PurchaseResult coins = iap.Purchase(IapProductId.SmallCoinPack);

            Assert.That(removeAds.WasSuccessful, Is.True);
            Assert.That(iap.IsOwned(IapProductId.RemoveAds), Is.True);
            Assert.That(coins.WasSuccessful, Is.True);
            Assert.That(iap.IsOwned(IapProductId.SmallCoinPack), Is.False);

            iap.NextPurchaseStatus = PurchaseStatus.Cancelled;
            Assert.That(iap.Purchase(IapProductId.BoosterBundle).Status, Is.EqualTo(PurchaseStatus.Cancelled));

            iap.IsReady = false;
            Assert.That(iap.Purchase(IapProductId.CosmeticBundle).Status, Is.EqualTo(PurchaseStatus.Unavailable));
        }

        [Test]
        public void EconomyInventoryBoosterAndCosmeticServices_HandleRewardsAndSpends()
        {
            EconomyService economy = new EconomyService();
            InventoryService inventory = new InventoryService();
            BoosterService boosters = new BoosterService(inventory);
            CosmeticService cosmetics = new CosmeticService(inventory);

            Assert.That(economy.GrantCoins(100, EconomyTransactionReason.LevelReward), Is.True);
            Assert.That(economy.TrySpendCoins(40, EconomyTransactionReason.BoosterSpend), Is.True);
            Assert.That(economy.Coins, Is.EqualTo(60));
            Assert.That(economy.TrySpendCoins(200, EconomyTransactionReason.BoosterSpend), Is.False);

            inventory.AddBooster(BoosterType.Hammer, 2);
            Assert.That(boosters.CanUseBooster(BoosterType.Hammer), Is.True);
            Assert.That(boosters.TryUseBooster(BoosterType.Hammer), Is.True);
            Assert.That(inventory.GetBoosterCount(BoosterType.Hammer), Is.EqualTo(1));

            Assert.That(cosmetics.TryEquipCosmetic(CosmeticSlot.BoardFrame, "relic_frame"), Is.False);
            Assert.That(inventory.UnlockCosmetic("relic_frame"), Is.True);
            Assert.That(cosmetics.TryEquipCosmetic(CosmeticSlot.BoardFrame, "relic_frame"), Is.True);
            Assert.That(cosmetics.GetEquippedCosmetic(CosmeticSlot.BoardFrame), Is.EqualTo("relic_frame"));
        }

        [Test]
        public void ConsentAnalyticsAndRemoteConfig_DefaultToSafeNoOpBehavior()
        {
            NoOpAnalyticsService analytics = new NoOpAnalyticsService();
            LocalConsentService consent = new LocalConsentService();
            LocalRemoteConfigService remoteConfig = new LocalRemoteConfigService();

            Assert.DoesNotThrow(() => analytics.TrackEvent("session_start"));
            Assert.That(consent.Current.AnalyticsAllowed, Is.False);
            Assert.That(consent.Current.PersonalizedAdsAllowed, Is.False);
            Assert.That(remoteConfig.GetBool("rewarded_ads_enabled", false), Is.False);
            Assert.That(remoteConfig.GetInt("fail_extra_moves", 5), Is.EqualTo(5));

            consent.SetConsent(true, false);
            remoteConfig.SetBool("rewarded_ads_enabled", true);
            remoteConfig.SetInt("fail_extra_moves", 7);

            Assert.That(consent.Current.AnalyticsAllowed, Is.True);
            Assert.That(consent.Current.PersonalizedAdsAllowed, Is.False);
            Assert.That(remoteConfig.GetBool("rewarded_ads_enabled", false), Is.True);
            Assert.That(remoteConfig.GetInt("fail_extra_moves", 5), Is.EqualTo(7));
        }

        [Test]
        public void ProductionDataAssets_HaveSafeDefaults()
        {
            EconomyCatalog economy = ScriptableObject.CreateInstance<EconomyCatalog>();
            BoosterData booster = ScriptableObject.CreateInstance<BoosterData>();
            CosmeticSkinData cosmetic = ScriptableObject.CreateInstance<CosmeticSkinData>();
            StoreProductData storeProduct = ScriptableObject.CreateInstance<StoreProductData>();
            RemoteTuningConfig tuning = ScriptableObject.CreateInstance<RemoteTuningConfig>();
            DailyRewardCalendar rewards = ScriptableObject.CreateInstance<DailyRewardCalendar>();
            MissionData mission = ScriptableObject.CreateInstance<MissionData>();
            LevelPackCatalog levelPack = ScriptableObject.CreateInstance<LevelPackCatalog>();

            Assert.That(economy.LevelWinCoins, Is.GreaterThanOrEqualTo(0));
            Assert.That(booster.CoinCost, Is.GreaterThanOrEqualTo(0));
            Assert.That(cosmetic.CosmeticId, Is.EqualTo(CosmeticService.DefaultCosmeticId));
            Assert.That(storeProduct.IsRandomizedReward, Is.False);
            Assert.That(tuning.InterstitialAdsEnabled, Is.False);
            Assert.That(rewards.GetCoinReward(0), Is.GreaterThanOrEqualTo(0));
            Assert.That(mission.TargetAmount, Is.GreaterThan(0));
            Assert.That(levelPack.FirstLevelNumber, Is.EqualTo(1));
            Assert.That(levelPack.LastLevelNumber, Is.GreaterThanOrEqualTo(levelPack.FirstLevelNumber));

            Object.DestroyImmediate(economy);
            Object.DestroyImmediate(booster);
            Object.DestroyImmediate(cosmetic);
            Object.DestroyImmediate(storeProduct);
            Object.DestroyImmediate(tuning);
            Object.DestroyImmediate(rewards);
            Object.DestroyImmediate(mission);
            Object.DestroyImmediate(levelPack);
        }
    }
}
