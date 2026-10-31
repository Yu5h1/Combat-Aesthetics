# Combat Aesthetics Handoff

GitHub repository: [Yu5h1/Combat-Aesthetics](https://github.com/Yu5h1/Combat-Aesthetics). Local folder is now `Unity/Combat-Aesthetics`, renamed by the user; origin uses the corresponding GitHub URL. Unity package identifier and code type names retain their existing names. TaskProgress scope remains `combat-aesthetic` and points to this new local folder.

## Local checkout path migration

The user subsequently completed the local rename to `Unity/Combat-Aesthetics`. The old `Unity/CombatAesthetic` path is absent. TaskProgress registration and the workspace overlay's handoff reference are synchronized to the new path; no compatibility junction was created by this chat.

The user subsequently asked to finish the reference repair. KFC's `Packages/manifest.json` and `Packages/packages-lock.json` now point to `file:C:/Users/Yu5h1/Dev/VSProjects/Yu5h1Lib/Unity/Combat-Aesthetics`; each changed one path only, with all other bytes preserved and package identifier unchanged. KFC files have not been committed by this chat. Yu5h1LibTest was inspected read-only: no CombatAesthetic reference was found in Assets, Packages or asset junction targets; do not add a dependency merely to perform this rename. No Unity instance was launched or package resolution run.

## WhiteRoom sample planning

First delivery reordered by the user 2026-10-30: movement plus handshake (hold E to reach and wait, release to withdraw; the other side holds E and walks in; success, prank, both-withdraw and grip contest outcomes, built in three batches) comes first as the interaction system; unarmed PvE attack and defence become the second delivery and reuse it ([第一個交付的目標](Samples~/WhiteRoom/Implementation-Plan.md#第一個交付的目標), stage 2b). Clips come from the animation line; hand alignment is runtime humanoid IK, presentation only. On 2026-10-31 the user split the scenes: the playable combat version moves to Samples~/WhiteRoom/Combat/ and pauses until combat validation starts; the handshake goes in Samples~/WhiteRoom/Handshake/.

Handshake Batch 1 and the scene split were implemented in `4cba70b`; its acceptance failed. Item-by-item results are in [握手第 1 批交辦驗收](Documentation/WhiteRoom-Acceptance.md#握手第-1-批交辦驗收); its "6/6 PASS" is scripted intent injection, not user operation. The fix brief [第 1 批修正交辦](Samples~/WhiteRoom/Implementation-Plan.md#第-1-批修正交辦) includes the user's re-press rule (item 10).

The fixes landed in `bf8544d` ([握手第 1 批修正驗收](Documentation/WhiteRoom-Acceptance.md#握手第-1-批修正驗收)) and the second round in `09ed127` ([握手第 1 批第二輪修正驗收](Documentation/WhiteRoom-Acceptance.md#握手第-1-批第二輪修正驗收)).

Handshake batch 2 landed in `0d29bfb`; acceptance: [握手第 2 批驗收](Documentation/WhiteRoom-Acceptance.md#握手第-2-批驗收) — arbitration is correct, but screenshots miss the HUD, reaction clip slots are not usable from the scene, and an undocumented `CombatEntity` grounding change may alter the combat scene.

Handshake batch 3 and its first fixes landed together in `f95b8f8`; acceptance: [握手第 3 批驗收](Documentation/WhiteRoom-Acceptance.md#握手第-3-批驗收) and [握手第 3 批修正驗收](Documentation/WhiteRoom-Acceptance.md#握手第-3-批修正驗收) — both not passed (no Animator clip playback, timer-based control lock, no real jump while caught, no prefabs; agent screenshots are blank, likely because batch-mode tests do not render). On 2026-10-31 the user decided: control intervals are named segments on each state's reference animation; movement comes from dedicated clips' root motion instead of tunable speeds; the escape threshold is arm reach from the skeleton plus an adjustable offset; agents deliver logs, test results and a Windows build instead of screenshots. The second round landed in `c696749`; acceptance: [握手第 3 批第二輪修正驗收](Documentation/WhiteRoom-Acceptance.md#握手第-3-批第二輪修正驗收) — not passed: prefabs, pre-IK sphere, tests and the Windows build (`W:\UnityProject\Yu5h1LibTest-Build\WhiteRoom_Handshake\`) are done, but the interval checks are never called, timers still drive states, overrides by state name leave batch 2/3 states silent, and the hand sphere is forced to 0.40 m because the 0.5 m capsules keep the two bodies from the 0.8 m standard distance. The animation line remade all 17 clips with fingers and root motion (AnimationEditor `7912a50`, data-driven `b1db727`, Web `2e1b4ab`); COMBAT-ANIMATION-1/-3 wait only on WhiteRoom import, COMBAT-ANIMATION-4 fails on editing, the unbumped format version, an unreferenced schema definition and no downstream requests (statuses in [Requirements.Animation.md](Documentation/Requirements.Animation.md)). The third round landed in `c9b215c`; acceptance: [握手第 3 批第三輪修正驗收](Documentation/WhiteRoom-Acceptance.md#握手第-3-批第三輪修正驗收) — passed: intervals copied from the clip files gate re-press, every state plays its own clip through overrides keyed by the original clip, all 17 fingered clips are imported with root motion driving caught, catching and leap, the 0.18 m hand sphere and 0.30 m capsules reach 0.82 m, and the build data was rebuilt at 18:46. COMBAT-ANIMATION-1 and -3 are Implemented; COMBAT-ANIMATION-4 passes items 1 and 3 and waits on ANIMATION-LIMB-8 and ANIMATION-UNITYEXTENSION-1 for GLB delivery, after which WhiteRoom reads intervals from the import instead of the hand-copied values. The animation spec table pipes and the constraint node length limit were restored in data-driven `f6eaf9f`. Next: an implementation agent runs [第 3 批收尾小修交辦](Samples~/WhiteRoom/Implementation-Plan.md#第-3-批收尾小修交辦) (GLB `.meta` files, remaining hard-coded fallbacks, the capsule radius hard-coded in code, an override-distinguishing test). Keyboard acceptance by the user with the Windows build is still open for batches 1 and 3.

When combat validation resumes, continue through 〈戰鬥驗證交辦範圍（暫停）〉 in the same plan, based on [交辦前來源補查](Documentation/WhiteRoom-Acceptance.md#交辦前來源補查). The user accepted the existing Yu5h1LibTest / Unity 6000.6.0f1 host baseline. Runtime sources match the package, but scene hashes differ; inspect via Unity before overwriting. Fix and verify the existing local loop and diagnostics first, then assess network batch C; do not rebuild the common pipeline or claim the heartbeat queue tests combat networking. Online combat has no personal pause or protection while using panels, switching windows, or leaving the computer; follow the plan and implementation brief. The held-WASD question was withdrawn because it over-focused this interview on input details instead of the online-combat requirement; do not infer an accepted WASD policy. Continue only material planning decisions in [尚未決定](Samples~/WhiteRoom/Implementation-Plan.md#尚未決定), one at a time. This chat remains documentation-only; the user will pass the implementation brief to another agent.

The active scope is same-repo isolation of the new combat foundation and WhiteRoom validation. Follow [已確認的演進與遷移方向](Documentation/plans/UPM-Package-Split-Plan.md#已確認的演進與遷移方向). The user will handle KFC migration in another session: do not plan its batches, ask migration questions, or make it a WhiteRoom prerequisite. Preserve existing consumers while defining the new boundary. The earlier migration procedure was removed because it expanded this session beyond the user’s intended scope. Inspect the latest implementation before applying older review findings.

For the user's return to gameplay acceptance, start with [WhiteRoom 靜態驗收紀錄](Documentation/WhiteRoom-Acceptance.md). It pins the inspected host copy and implementation and separates source findings from unexecuted runtime checks. The user accepted the updated [實作階段](Samples~/WhiteRoom/Implementation-Plan.md#實作階段) and [連線驗證路線](Samples~/WhiteRoom/Implementation-Plan.md#連線驗證路線): design for online synchronization, validate minimal combat on localhost/LAN with simulated adverse network conditions in stage 1, then expand gameplay. This turn updates documentation only; it does not authorize implementation or select a network stack. Older planning-only observations below are historical context.

Current user direction: keep WhiteRoom in this existing package's Sample scope. The canonical implementation plan is [Samples~/WhiteRoom/Implementation-Plan.md](Samples~/WhiteRoom/Implementation-Plan.md). It supersedes the independent product-host proposal from the UX discussion. This request authorizes planning, commit and push only; no demo implementation, Sample registration, dependency change or asset acquisition.

First delivery is a 3D PvE loop: movement/jump, stationary/directional attack, ordinary/precise defense, hit response/recovery, third-person camera and reset. Follow the plan for stages, acceptance and [美術與角色方向](Samples~/WhiteRoom/Implementation-Plan.md#美術與角色方向); do not resume per-reaction interviews before resolving delivery prerequisites. Broader package extraction discussions below are separate work.

Validation for this documentation change is static and scoped; Unity import, gameplay and Windows builds have not been executed.

Preparation work is tracked by this package's Report card `white-room-stage0`, scope `combat-aesthetic` (canonical entry: [WhiteRoom plan](Samples~/WhiteRoom/Implementation-Plan.md)). The user dispatches other agents. Follow [可先交辦的準備工作](Samples~/WhiteRoom/Implementation-Plan.md#可先交辦的準備工作) for the read-only scope and isolated outputs. Individual instructions and progress belong to Report items. The shared-operation flow is now agreed; preparation agents must not decide the remaining tool/platform/data questions.

Networking discovery continues through the plan's [連線驗證路線](Samples~/WhiteRoom/Implementation-Plan.md#連線驗證路線). The user accepted the first LAN scenario: two independent WhiteRoom clients, each controlling a character in the same fight. The plan owns the route and remaining networking choices; the scenario is not authorization to install networking packages or launch a server. Keep the independent Animation authoring case separate from connected combat acceptance.

The reusable source systems are consolidated in `Yu5h1Lib/data-driven`, with `animation` and `limb` modules. Continue through its [handoff](../../data-driven/handoff.md) and [root plan](../../data-driven/plan.md). This repo owns Combat Aesthetics integration and WhiteRoom consumer tests. Preserve the consumer requirements in [WhiteRoom plan](Samples~/WhiteRoom/Implementation-Plan.md); do not duplicate the source systems' pending questions here. The integration changes repositories/documents, not runtime implementations.

Source-system progress belongs to scope `data-driven`, animation card `character-animation-foundation` and limb card `limb-system-foundation`, in [its Report](../../data-driven/report.json). This report carries only the `data-driven` pointer card. Scope/pointer migration used the locked direct-edit exception because the CLI lacks these operations; other Report updates used its CLI. Name that exception when committing this integration.

The user requested Chinese Report display text and scope `combat-aesthetic`. Report/task/item IDs, statuses, priorities, ordering and recorded history are preserved; the stable report_id remains unchanged. Text edits use Report operations. Root scope_id is not supported by report.update, so its rename is the scoped direct-edit exception, under revision lock and followed by Report validation; mention this exception if committing the rename. Use the new scope for links and local registration.

The [existing-capability inventory](Samples~/WhiteRoom/Existing-Capabilities.md) still supplies reuse evidence, but its Editor/Clip writer is no longer the required pipeline. Reassess relevant pieces against the new source/runtime boundary rather than discarding all Yu5h1Lib capabilities. The current host and version are defined by the WhiteRoom plan. The earlier 6000.3.9f1 first-host choice was superseded by the user’s existing Yu5h1LibTest / 6000.6.0f1 selection; this is not a removal of shared-library compatibility obligations.

Ruled out: selecting KayKit or another ready-made animation library as the first source. The user instead wants agents to build an accumulating animation library of their own; the earlier ready-made-animation direction is superseded. This does not authorize a new animation framework or the separate limb-generation system.

Superseded: .anim as the required first delivery format and an Editor/Clip-centered production workflow. Preserve readable source data and physical causality; do not silently require Clip generation. The former LimbSystem is now data-driven's limb module; its source responsibility stays separate from Combat Aesthetics, which remains the consuming integration.

## Current User Direction

User clarified that CombatAesthetic is about the aesthetics of action/fighting gameplay: game feel, action combat, duel readability, hit response, animation/FX/audio timing, and presentation quality.

Systems that are generally reusable outside this action-combat aesthetic layer should be extracted into Yu5h1Lib packages. CombatAesthetic should become the integration/product layer that composes those packages for action-combat gameplay, not the place where every generic system lives.

## Coordination Notes

- Follows UnityExtension House Conventions in [Unity/UnityExtension/.agents/introduction.md](../UnityExtension/.agents/introduction.md): never read UnityEngine.Input directly, always use Yu5h1Lib.InputHandler.

- No active `handoff.md` existed before this file.
- Do not implement package moves yet unless the user explicitly asks.
- Existing code has not been moved or refactored in this planning pass.
- A discussion plan exists at `Documentation/.plan/UPM-Package-Split-Plan.md`.
- `plan.md` is the human-facing project plan and should keep the user's language/context.

## Architectural Decision Snapshot

CombatAesthetic should keep:

- Action combat feel and timing.
- Character combat composition.
- Hit/hurt/cancel/reaction aesthetics.
- Animator/FX/audio/UI presentation coordination for combat.
- Samples, presets, and integration behaviours that express the combat aesthetic.

Move or create Yu5h1Lib packages for:

- Generic gameplay data and attributes.
- Generic 2D physics/detection helpers.
- Generic input command adapters.
- Generic AI route/patrol tools.
- Generic UI controls/stat views.
- Generic animation/Timeline/SMB helpers.
- Generic pooling, event, resource, and editor utilities.
- Future network/channel/snapshot abstractions.

## Recommended Package Extraction Direction

Start with broad package boundaries instead of many tiny packages:

1. `GameplayCore`
2. `Gameplay2D`
3. `CombatCore`
4. `Combat2D`
5. `CombatAnimation`

Later split adapters:

- `CombatInput`
- `CombatAI`
- `CombatUI`
- `CombatFX`
- `Network`

## Known Coupling Risks

- `CharacterController2D` currently mixes movement, attribute damage, input host, detector, scanner, caster, teleport, PlayerPrefs tuning, invincibility visuals, and GameManager pause state.
- `AnimatorCharacterController2D` currently mixes Animator root motion, skill selection, FX cast, audio playback, UI/cursor operations, and combat state.
- `AttributeBehaviour` owns UI references, which blocks clean extraction into model/core.
- `HostData2D` is a good command-source concept, but it directly depends on `CharacterController2D`.
- Several 2D event/physics utilities depend on `GameManager` or `SceneController`, which prevents them from being low-level 2D package code.

## Next Safe Steps

- For progression design, resolve [尚未決定](Documentation/plans/Progression-and-Ascension-Design.md#尚未決定) before implementing the [suggested prototype](Documentation/plans/Progression-and-Ascension-Design.md#建議初版與案例). The design is documented; gameplay behavior has not been implemented or tested.

1. Produce a class-to-package mapping table before moving files.
2. Mark generic classes that should be promoted to Yu5h1Lib package candidates.
3. Define command-source interfaces before touching input/AI/network.
4. Define presentation events before splitting UI/FX/audio out of combat behaviours.
5. Only then create package branches and move files in small batches.

## Open decisions

- New combat boundaries: [下一步討論題](Documentation/plans/UPM-Package-Split-Plan.md#下一步討論題). Same-repo isolation and WhiteRoom-first validation are settled; package/API shapes remain open. KFC migration decisions belong to the separate session.

- WhiteRoom Sample: [尚未決定](Samples~/WhiteRoom/Implementation-Plan.md#尚未決定) owns this consumer's integration, networking, precise-defense and import/assembly questions. The separate system's [尚未決定](../../data-driven/animation/plan.md#尚未決定) is owned by data-driven/animation and routed by its handoff; do not decide or duplicate those answers here. Repo name/location, the split, both test approaches and shared-operation flow are agreed alongside the current host baseline in the plan.
- Interaction system design draft: [尚未決定](Documentation/plans/Interaction-System-Design.md#尚未決定) (whether combat moves share the definition format, whether cancel is a formal result, whether the shared layer stays in the Sample or moves to the package Runtime, how the player picks the current interaction). The network option list is routed from the plan's item 4 to [WhiteRoom-Network-Evaluation](Documentation/plans/WhiteRoom-Network-Evaluation.md#需要使用者決定的選項).
- Progression and ascension: [尚未決定](Documentation/plans/Progression-and-Ascension-Design.md#尚未決定). This is a discussion draft, not an approved implementation specification.
- Question: Should CombatAesthetic's game core adopt physical aesthetics as a base rule?
  - Options: enforce no process-free teleportation and no perfect cloning; allow these only as explicit authored exceptions; leave them unrestricted for convenience.
  - Recommendation: enforce the principle by default, because combat feel and world consistency depend on visible/physical process, source, distance, and material continuity.
- Question: How do players enter or get born into the world?
  - Options: enter from outside the arena; spawn from a field/device; assemble from existing material; be summoned/dropped by another system; defer until the first playable prototype.
  - Recommendation: decide before implementing spawn/respawn/network authority, because it affects rules, animation, synchronization, fairness, and anti-cheat.
- Directional input UX: [未決策事項](Documentation/plans/Directional-Input-UX-Design.md#31-觸控螢幕操控-mobile-touch-ux). Options include dynamic floating joystick vs. fixed octagonal D-pad, and double-tap dash vs. dedicated evade button.

## Current Decision Report

- User wants a priority task and decision checklist before implementation.
- Human-facing report added at `Documentation/plans/Priority-Decision-Report.md`.
- TaskProgress report is `report.json`, scope `combat-aesthetic` (includes task `directional-input-ux-design`).
- TaskProgress now includes `physical-aesthetic-core` to track no perfect cloning, no process-free teleportation, and player spawn/birth decisions.
- Local Viewer route: `http://127.0.0.1:8001/?scope=combat-aesthetic`.
- The View/ViewNavigator decision list (items 1–18) is settled; see the report's 〈已決定〉. The shared `Viewer` was renamed `ViewNavigator` because Yu5h1Lib.UX already defines Viewer as the camera viewport. Implementation and migration are queued as Report items: `unity-extension › view-navigator › view-navigator-core`, then `virtual-chat-partner › view-navigator-adoption › vcp-page-migration` (waits on that item and on VCP's in-progress `streaming-assets` items). CombatAesthetic/KFC is the second round, not planned here.
- The physics-aesthetic items (decision list 0.x) remain open.

## Refactor Guidance Added

User asked for clearer refactoring direction, not only package splitting. A guide was added at `Documentation/.plan/Refactor-Direction-Guide.md`.

Current recommended starting point:

1. Clarify `AttributeBehaviour` first because it couples gameplay data, Unity lifecycle, UI refresh, depletion rules, and skill cost validation.
2. Clarify `HostData2D` into a command-source concept because Player, AI, Network, and Replay should all feed the same role.
3. Slim `CharacterController2D` into a command-consuming character executor before trying to make it pure or package-ready.
4. Treat `AnimatorCharacterController2D` as a presentation bridge, not core combat logic.

Architecture stance:

- Do not introduce ECS or a DI container as the first step.
- Use Yu5h1Lib AdapterFactory-style injection only where there is one purpose with multiple implementations.
- Focus first on interfaces, events, and dependency direction.
- ScriptableObject remains useful for authoring data/settings, but runtime state and future rollback snapshots should not live primarily in SO assets.

## Scope Correction

User clarified later that the broader refactor discussion is not CombatAesthetic-specific. CombatAesthetic remains an action-combat aesthetics application/use case. The general data-driven composition/capability-resolution architecture belongs to Yu5h1Lib UnityExtension.

Canonical current plan location:

- `C:\Users\Yu5h1\Dev\VSProjects\Yu5h1Lib\Unity\UnityExtension\plan.md`
- `C:\Users\Yu5h1\Dev\VSProjects\Yu5h1Lib\Unity\UnityExtension\handoff.md`

Do not continue treating CombatAesthetic as the owner of that general architecture. Use it only as a validation case.
