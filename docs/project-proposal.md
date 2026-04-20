# Project Proposal

## Development of a Mobile Hybrid-Casual Puzzle Game

**Project Title:**  
Development and release of a mobile hybrid-casual puzzle game for Android and iOS

**Project Lead:**  
Sebastijan Bogdan

**Project Type:**  
Software development project / Mobile app / Game development

**Planned Project Start:**  
[To be filled in]

**Planned Project End:**  
[To be filled in]

---

## 1. Initial Situation

The mobile gaming market continues to show strong demand, especially in the area of simple, easy-to-understand casual and puzzle games. For solo developers and small teams, hybrid-casual puzzle games are a realistic entry point because they can be built, tested, and iterated on with comparatively manageable development effort.

The goal is to develop a simple but marketable mobile puzzle game with a clear gameplay loop, low technical complexity, and scalable monetization. The project is intended to start with a lean MVP so that the market can be validated as early as possible and the product can later be improved based on real player data.

---

## 2. Project Goal

The goal of the project is the design, development, and release of a mobile puzzle game built around accessibility, short play sessions, and strong replayability.

The product should:

- be easy to understand within seconds,
- be developed in a technically clean and maintainable way,
- be feasible with a limited budget,
- be validated on Android first,
- be prepared for later release on iOS after successful validation,
- be monetized through ads and in-app purchases.

---

## 3. Project Description

The project is planned as a 2D hybrid-casual puzzle game. It is based on a simple and clearly readable game mechanic with short levels, fast feedback, and a low barrier to entry. The focus is on gameplay that can be understood within seconds while still offering enough motivation for repeated play.

The product will initially be implemented as an MVP. The MVP will include only the features required for a first market release and validation. More complex systems such as multiplayer, cloud synchronization, user accounts, extensive live events, or story-heavy content are explicitly excluded from the first phase.

---

## 4. Target Group

The primary target audience consists of:

- mobile players with short gaming sessions,
- casual gamers,
- users who prefer simple and easy-to-understand puzzle games,
- people roughly between the ages of 16 and 45,
- Android users in the first release phase.

As a secondary target group, the game may later be made available to iOS users after successful optimization and validation.

---

## 5. Value and Benefits

The project provides economic, technical, and strategic value.

**Economic Value:**

- creation of a monetizable digital product,
- opportunity to generate recurring revenue through ads and in-app purchases,
- creation of a scalable basis for future game projects.

**Technical Value:**

- establishment of a clean and reusable architecture for mobile games,
- use of modern development tools and AI-assisted workflows,
- reuse of components for future game projects.

**Strategic Value:**

- entry into the mobile games market,
- development of know-how in monetization, user behavior, and retention,
- opportunity to create future variants or spin-offs based on the MVP.

---

## 6. Project Scope

### In Scope

The following items are part of the project:

- development of a 2D puzzle game,
- implementation of a clear core gameplay loop,
- level system with defined progression,
- main menu and game screen,
- local save system for player progress,
- integration of rewarded ads,
- implementation of a "Remove Ads" purchase,
- basic analytics for evaluating usage and player behavior,
- Android release as the first target platform,
- preparation for a later iOS release.

### Out of Scope

The following items are not part of the first phase:

- multiplayer,
- online accounts or login,
- cloud save,
- PvP or leaderboards,
- story mode,
- LiveOps systems,
- complex shop systems,
- advanced backend infrastructure.

---

## 7. Technical Approach

The project is planned with a focus on fast execution, maintainability, and extensibility.

**Technology Stack:**

- **Engine:** Unity, currently based on `2022.3.15f1`; a later upgrade to a newer Unity version, including Unity 6, may be evaluated if it provides practical benefits
- **Programming Language:** C#
- **Platforms:** Android first, iOS in a later phase
- **Version Control:** Git / GitHub
- **AI Support:** Codex for structuring, implementation support, and refactoring
- **Data Storage:** local on the device
- **Monetization:** rewarded ads, "Remove Ads" in-app purchase

**Architecture Principles:**

- separation of gameplay logic and presentation,
- modular services for ads, audio, IAP, and saving,
- maintainable project structure,
- MVP-first approach,
- no unnecessary backend complexity in phase 1.

---

## 8. System Architecture

The application is intended to be divided into four logical layers:

### 1. Core Layer

Contains the pure gameplay logic:

- board state,
- rules,
- move validation,
- win and loss conditions,
- scoring,
- hint and undo logic.

### 2. Presentation Layer

Contains all visual and interactive elements:

- UI,
- animations,
- input handling,
- effects,
- transitions.

### 3. Service Layer

Contains platform-related and technical services:

- SaveService,
- AdService,
- IapService,
- AudioService,
- AnalyticsService.

### 4. Content Layer

Contains configurable content:

- level data,
- difficulty configuration,
- themes,
- gameplay parameters.

This structure supports a clean separation of responsibilities and makes later extensions easier to implement.

---

## 9. Project Execution Approach

The project will be implemented iteratively.

### Phase 1 - Concept and Prototype

- definition of the gameplay loop,
- selection of the core puzzle mechanic,
- creation of the basic architecture,
- prototype of the game board.

### Phase 2 - MVP Development

- implementation of gameplay logic,
- UI and navigation,
- level structure,
- save system,
- first playable version.

### Phase 3 - Monetization and Polish

- rewarded ads,
- "Remove Ads" IAP,
- sound and visual improvements,
- balancing and bug fixing.

### Phase 4 - Testing and Soft Launch

- internal testing,
- closed test or soft launch on Android,
- evaluation of initial metrics,
- optimizations.

### Phase 5 - iOS Preparation

- porting and platform adjustments,
- build and store preparation,
- App Store release after successful Android validation.

---

## 10. Schedule

Example of a rough high-level schedule:

- **Week 1:** concept, scope definition, project setup
- **Week 2-3:** core gameplay and game logic
- **Week 4:** UI, navigation, level data
- **Week 5:** save system, audio, polishing
- **Week 6:** ads, IAP, analytics
- **Week 7:** testing, bug fixing, Android build
- **Week 8:** soft launch and first optimizations

Estimated MVP duration: approximately 6 to 8 weeks

---

## 11. Risks

The following risks have been identified:

- insufficient player retention,
- low monetization conversion,
- limited store visibility,
- scope becoming too complex too early,
- time loss caused by unnecessary extra features.

**Mitigation Measures:**

- strict MVP focus,
- early testability,
- Android-first validation,
- data-driven iteration,
- deliberate limitation of feature scope.

---

## 12. Monetization Concept

The game is intended to use a lean hybrid monetization model at first:

- **Rewarded Ads** for extra attempts, hints, or small advantages,
- **Remove Ads** as a one-time in-app purchase,
- optional later additions:
  - hint packs,
  - undo packs,
  - small convenience features.

The monetization concept is deliberately kept simple so that the MVP does not overload the user experience.

---

## 13. Success Metrics

The following KPIs will be used to evaluate project success:

- number of installs,
- Day 1 and Day 7 retention,
- average session length,
- number of levels played per user,
- ad watch rate,
- conversion rate for "Remove Ads",
- crash-free sessions,
- player feedback during soft launch.

---

## 14. Expected Result

At the end of the project, the intended result is a released, functional, and marketable mobile puzzle game MVP that:

- is technically stable,
- has been released on Android,
- provides real usage data,
- can be monetized,
- serves as a basis for later extensions or future game projects.

---

## 15. Current Development Status

At the time of this proposal, the repository reflects a very early project stage:

- a Unity project foundation is already in place,
- the current Unity editor version is `2022.3.15f1`,
- the project still uses the temporary Unity product name `New Unity Project`,
- there are currently no scenes configured in build settings,
- the `Assets` folder does not yet contain meaningful gameplay implementation.

This means the proposal describes the intended product and delivery roadmap, while the current repository primarily represents the initial technical setup for future implementation.

---

## 16. Application

The project "Development and Release of a Mobile Hybrid-Casual Puzzle Game for Android and iOS" is hereby submitted for approval.

The project is professionally meaningful, technically realistic, and economically suitable for creating a marketable digital product with manageable effort. The deliberate limitation to an MVP, the Android-first approach, and the focus on a clean technical architecture are intended to ensure efficient and goal-oriented implementation.

**Approval is requested for the implementation of the project described above.**
