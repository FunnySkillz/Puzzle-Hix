# PuzzleDungeon Production Roadmap

## Locked Direction

- Android-first hybrid-casual cozy dungeon match-3.
- iOS comes after Android validation.
- 0 EUR until soft-launch readiness.
- Retention-first monetization: optional rewarded ads, direct IAP, cosmetics, boosters, and remove ads.
- No paid random loot boxes in v1.

## Implemented Foundation Slice

- Product bible with theme, monetization, originality, and success rules.
- Asset/license register for the zero-cost production constraint.
- Production service interfaces and local/mock implementations for analytics, ads, IAP, consent, remote config, economy, inventory, boosters, cosmetics, and launch policy.
- Catalog data types for future boosters, skins, store products, ad placements, remote tuning, daily rewards, missions, and level packs.

## Next Sprint Order

1. Core architecture hardening: split match-3 board rules from UI-heavy board presentation.
2. Mobile UX pass: safe areas, portrait layout, TextMeshPro, level preview, and tutorial.
3. Game feel pass: polished animations, SFX, special-piece effects, settings toggles.
4. Gameplay depth: blockers, special combinations, and earned boosters.
5. Content expansion: validation tooling and 60 balanced levels.
6. Economy/progression: coins, inventory rewards, daily rewards, missions, star chests.
7. SDK pass: analytics, crash reporting, remote config, consent, then ads/IAP mocks to Android implementation.
8. Store readiness: signing, package id, icon, screenshots, privacy/data safety, closed test.

## Production Gates

- Do not add real ads or IAP before analytics, consent, and remote config are working.
- Do not add interstitial ads before retention data supports testing them.
- Do not expand beyond 60 levels until the first 20 are fun and understandable.
- Do not spend money before the project explicitly changes the budget rule.

## Market And Policy Anchors

- Hybrid-casual direction and ad-frequency caution: https://gamingamericas.com/press-releases/2025/04/24/111395/appodeals-2025-mobile-casual-benchmarks-report-shows-hybrid-casual-games-significantly-outperforming-hypercasual-when-it-comes-to-ad-based-monetization/
- Puzzle LiveOps and analytics foundation: https://naavik.co/digest/live-ops-trends-powering-mobile-puzzle/
- LiveOps as a retention and monetization layer: https://mobidictum.com/appmagic-liveops-report-2025/
- Google Play quality pillars: https://play.google.com/console/about/guides/featuring/
- Apple IAP and randomized reward disclosure rules: https://developer.apple.com/app-store/review/guidelines/
- Unity Gaming Services free-tier caution: https://docs.unity.com/services/pricing-and-billing
