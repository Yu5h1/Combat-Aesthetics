# WhiteRoom：3D 測試場 Sample（握手互動與徒手對戰）

> 套件名稱：`com.yu5h1.combataesthetic`  
> 驗證宿主：Unity `6000.6.0f1` URP（`W:/UnityProject/Yu5h1LibTest` 實測 0 編譯錯誤、PlayMode 測試組件 8/8 通過、握手互動案例 21/21 全數通過、Windows Standalone 執行檔建置成功）  
> 規範文件：[WhiteRoom 實作計畫](Implementation-Plan.md) ｜ [既有能力對照](Existing-Capabilities.md) ｜ [WhiteRoom 靜態與動態驗收紀錄](../../Documentation/WhiteRoom-Acceptance.md)

---

## 1. 簡介與定位

WhiteRoom 是 CombatAesthetic 套件的官方 3D 最小測試場範例（Sample）。包含兩個獨立場景，並共用同一套 `Samples~/WhiteRoom/Runtime/`：
1. **第一個交付：移動＋握手互動場景 (`Handshake/Handshake.unity`)**：
   - 依照 Stage 2b「第 3 批交辦：比力氣」、「第 3 批第二輪修正交辦」與「第 3 批第三輪修正交辦」規範，完整實作握手互動系統：發起伸手、中途取消收手、走近回應、碰觸握緊、握手成功、惡作劇（單方放開）、同時收手（雙方放開）、握住、比力氣對抗、抓住與掙脫。
   - **判定與表現嚴格分離**：
     - **碰觸判定與標準距離**：雙方右手碰撞體（`RightHandContactCollider`，`SphereCollider` trigger 半徑 `0.18m`，符合自然手部大小 <= `0.25m`）發生真實物理接觸時判定「碰到」，完全無距離捷徑。角色預製物件之 `CharacterController.radius` 設為 `0.30m`，使雙方能自然走近至骨盆中心相距 `0.80m` 的標準動作距離；戰鬥場景角色獨立維持 `0.50m` 原生半徑不受影響。
     - **握緊裁決與握住過渡**：握緊（`0.50s`）期間有人放開互動鍵時，放開方立刻播收手抽回；握緊結束時雙方均放開裁定同時收手 (`BothWithdraw`)、單方放開裁定惡作劇 (`Prank`) 並由動畫具名區間鎖定放開者在反應結束前不可重新發起；雙方均按住則成功進入握住 (`Holding`)。
     - **比力氣對抗**：握住期間連按 E 計算按下次數；連按時短暫放開容許時間暫定 `0.30s`（可調）；雙方均放開時判定元件比次數：差距超過門檻（預設 1 次）為勝負已分 (`StrengthContestWin`)、門檻內且有人按過為平手慢退 (`StrengthContestTie`)、無人按為 0 比 0 搖手收手 (`StrengthContestZeroZero`)。
     - **抓住與動作驅動根位移**：一方放開超過容許時間、另一方按住時記為抓住 (`Caught`/`Catching`)；被抓方抽手、抓人方跟隨、被抓方跳開全面由 17 段含手指骨骼之動作片段（`handshake-caught`、`handshake-dragged`、`handshake-leap`）之**真實曲線根位移**（`applyRootMotion = true`）驅動，移動不再由程式速度暫代；起跳由真實 `Jump()` 觸發並於空中暫停落地重力以完整呈現起跳拋物線根位移；雙方距離超過骨骼手臂純長度量測門檻（雙人完全伸直 `1.06m`）裁定掙脫 (`Escaped`)，量不到手臂骨骼時嚴格記錯誤（無寫死 fallback）；被抓方於容許時間內重按互動鍵立即重回雙方握住且保留連按次數（+1）。
     - **時間來源與動態片段綁定**：動作片段時長直接取自 `AnimationClip.length`；`HandshakeVisualFeedback` 透過動態 `AnimatorOverrideController` 將 `HandshakeSettings.asset` 中指派的 17 段動作片段映射至 Controller 內具名狀態節點；操作許可由動畫具名區間（`ActionInterval` / `ClipIntervalBinding`，源自片段檔 `intervals` 標籤）嚴格決定，徹底移除固定秒數計時器與倒數。
     - **Runtime Humanoid IK 與 Pre-IK 碰撞球擺放**：在握緊、搖手、握住期間以權重 0.85 對準雙手中點；被抓住期間以權重 0.95 對準抓人方右手表現手臂拉直。碰撞球在 `OnAnimatorIK` 開頭以純動畫手腕延伸姿態擺放，並於測試案例驗證在鄰近目標下漂移 < 0.05m，100% 不受 IK 牽引影響判定。
   - **重新按下才發起**：所有結果（成功、惡作劇、同時收手、比力氣勝負/平手/0比0、掙脫）裁定後若持續按住互動鍵，維持站姿不自動伸手；必須先放開互動鍵再重新按下才發起。
   - **預製物件化與手工編輯 DX**：角色、AI 與鏡頭全面改為 Prefab（`WhiteRoom_Player.prefab`、`WhiteRoom_AI.prefab`、`WhiteRoom_Camera.prefab` 位於 `Samples~/WhiteRoom/Prefabs/`），場景直接引用且可在 Editor 直接調校；`HandshakeSettings` ScriptableObject 提供集中參數管理，Play Mode 調整值可即時保存。
   - **Windows 獨立執行檔交付與證據**：建置輸出於 `W:\UnityProject\Yu5h1LibTest-Build\WhiteRoom_Handshake\WhiteRoom_Handshake.exe`（repo 外部），指定場景建置不修改宿主建置設定；結構化事件日誌 `Documentation/WhiteRoom_Handshake_Logs.json`（218 筆真實條目）。依指令全面廢止 agent 截圖，以執行檔與日誌為準。
   - **重用共用 `Runtime/`**：重用 `CombatEntity`（移動、跳躍、重力與全場重設）、`WhiteRoomCharacter`（P1/P2 輸入採集與改鍵）、`WhiteRoomHUD`（IMGUI 面板與 JSON 日誌匯出）及 `WhiteRoomSceneBuilder`（場地、燈光、鏡頭）。戰鬥場景鏡頭維持原生 `LookAt` 跟拍，握手場景使用四分之三側視角構圖避免背影遮擋。
   - **Sample 場景僅含可操作內容**：`Handshake.unity` 不掛載自動測試腳本，進入 Play Mode 後即可由玩家直接操作；輔助自動化測試器位於 `Samples~/WhiteRoom/Tests/HandshakeVerificationRunner.cs` 與 `HandshakePlayModeTests.cs`。
2. **戰鬥驗證場景 (`Combat/WhiteRoom.unity`)**（目前暫停，保留至戰鬥驗證重啟）：
   - 驗證第三人稱徒手攻防：八方向移動、跳躍落地恢復、原地/方向出招、警戒慢步、普通防禦、精準彈刀反制、受擊反應與受力因果。
   - 本機玩家、AI 傀儡與遠端連線角色共用同一套 `CombatEntity` 與 `CombatActionIntent` 合法性規則。身體半徑維持 Unity 預設之 `0.5m`，攻防距離不受握手影響。

> **【網路狀態聲明】**：目前 HUD 內建之「不良網路模擬器」為單一程序內的記憶體模擬佇列（用於驗證延遲/丟包下的時序參數與日誌格式）。尚未接入真實網路傳輸層（Host/Client 或 LAN Socket），面板明確標示真實傳輸層為不可用 (N/A)，不將心跳模擬器宣稱為 LAN 或連線對戰已完成。

---

## 2. 目錄架構與 Assembly 邊界

WhiteRoom 遵循獨立 Assembly 邊界原則，嚴格隔離宿主專案腳本（不依賴 `Assembly-CSharp`），避免範例邏輯污染套件核心 Runtime：

```text
Samples~/WhiteRoom/
├─ README.md                  本文件：匯入、操作、重設與驗收紀錄
├─ Implementation-Plan.md     實作計畫與第 1 批修正交辦規範
├─ Existing-Capabilities.md   既有能力與模組重用分析
├─ Preparation/               階段 0 盤點報告
├─ Art/
│  └─ Handshake/              人形模型 (punch-character.glb)、Avatar、AnimatorController 與 5 支 LIMB-ANIM-1 人形動畫片段
├─ Combat/                    WhiteRoom.unity（徒手對戰測試場景，暫停到戰鬥驗證）
├─ Handshake/                 Handshake.unity（移動＋握手互動場景，第一個交付）
│  ├─ HandContactBridge.cs    右手 SphereCollider 觸發器橋接（驗證實際接觸的兩隻右手）
│  ├─ HandshakeAiController.cs 可設定之 AI 握手控制器（伸手延遲、按住時長、握緊放開時序、是否走近、停止距離）
│  ├─ HandshakeArbitrator.cs  權威握手判定核心（右手碰撞接觸、握緊、搖手、惡作劇與同時收手裁決）
│  ├─ HandshakeParticipant.cs 握手參與者狀態機（組合共用 CombatEntity 與 AnimationClip 時長）
│  ├─ HandshakeSceneBuilder.cs 握手場景建置器（重用 WhiteRoomSceneBuilder 共用場地/鏡頭/HUD）
│  ├─ HandshakeSettings.cs    握手集中參數資產定義（時限、門檻、動作區間與動作片段）
│  ├─ HandshakeSettings.asset 握手集中參數資產實體（Play Mode 調整值可持久化保存）
│  ├─ HandshakeState.cs       握手狀態機與正式裁決結果列舉
│  ├─ HandshakeVisualFeedback.cs 人形 Animator 驅動、Runtime Humanoid IK 與視鏡顏色表現層
│  └─ Yu5h1Lib.CombatAesthetic.Samples.WhiteRoom.Handshake.asmdef
├─ Prefabs/                   握手預製物件（直接置於場景，支援 Editor 調整與持久化）
│  ├─ WhiteRoom_Player.prefab 玩家角色預製物件（CharacterController、WhiteRoomCharacter、HandshakeParticipant）
│  ├─ WhiteRoom_AI.prefab     AI 角色預製物件（CharacterController、HandshakeAiController、HandshakeParticipant）
│  └─ WhiteRoom_Camera.prefab 第三人稱攝影機預製物件（WhiteRoomCamera）
├─ Runtime/                   共用 Sample Runtime assembly（Handshake 與 Combat 共用）
│  ├─ CombatActionIntent.cs   抽象操作意圖（含移動、跳躍、出招、防禦、InteractHold/Down/Up）
│  ├─ CombatEntity.cs         共用角色運動學（移動、跳躍、重力、全場重設）與戰鬥狀態機
│  ├─ WhiteRoomCharacter.cs   本機玩家輸入控制器（支援 P1/P2 可改鍵互動命令與意圖覆寫）
│  ├─ WhiteRoomDummy.cs       自主 AI 戰鬥傀儡
│  ├─ WhiteRoomVisuals.cs     程序化機械人形與姿態/時機顏色反饋
│  ├─ WhiteRoomCamera.cs      第三人稱軌道鏡頭（支援 UI 焦點隔離）
│  ├─ WhiteRoomHUD.cs         共用 IMGUI 狀態、P1/P2 互動改鍵、AI 參數設定、診斷與 JSON 匯出
│  ├─ WhiteRoomNetwork.cs     模擬不良網路通道（延遲/抖動/丟包佇列）
│  └─ WhiteRoomSceneBuilder.cs 共用場地環境、燈光、鏡頭與戰鬥情境生成器
└─ Tests/                     獨立測試 assembly（不掛載於 Sample 場景）
   ├─ HandshakeVerificationRunner.cs 握手第 2 批 10 項輔助自動化驗證腳本
   ├─ HandshakePlayModeTests.cs 握手與戰鬥鏡頭 PlayMode 測試案例組
   └─ Yu5h1Lib.CombatAesthetic.Samples.WhiteRoom.Tests.asmdef
```

* **Runtime Assembly**：`Yu5h1Lib.CombatAesthetic.Samples.WhiteRoom.Runtime`
  * 參照：`Yu5h1Lib.CombatAesthetic`、`Yu5h1Lib.Common`、`Yu5h1Lib.Library`、`Yu5h1Lib.InputHandler`、`Unity.InputSystem`、`Unity.RenderPipelines.Universal.Runtime`。
* **Handshake Assembly**：`Yu5h1Lib.CombatAesthetic.Samples.WhiteRoom.Handshake`
  * 參照：`Yu5h1Lib.CombatAesthetic.Samples.WhiteRoom.Runtime`、`Yu5h1Lib.CombatAesthetic`、`Yu5h1Lib.Common`、`Yu5h1Lib.Library`、`Yu5h1Lib.InputHandler`、`Unity.InputSystem`、`Unity.RenderPipelines.Universal.Runtime`。
* **Tests Assembly**：`Yu5h1Lib.CombatAesthetic.Samples.WhiteRoom.Tests`
  * 參照：`Yu5h1Lib.CombatAesthetic.Samples.WhiteRoom.Runtime`、`Yu5h1Lib.CombatAesthetic.Samples.WhiteRoom.Handshake`、`Yu5h1Lib.CombatAesthetic`、`Yu5h1Lib.Common`、`Yu5h1Lib.Library`、`Yu5h1Lib.InputHandler`、`Unity.InputSystem`。

---

## 3. 宿主相容性與依賴盤點

### 3.1 宿主環境
* **Unity 版本**：`6000.6.0f1`（宿主 `W:\UnityProject\Yu5h1LibTest` 實機編譯 0 錯誤）
* **Render Pipeline**：Universal Render Pipeline (URP 17.6.0)
* **輸入與搜尋封裝**：全數透過 `Yu5h1Lib.InputHandler` 讀取輸入，並使用 `Yu5h1Lib.ObjectUtility.FindObject<T>` / `FindObjects<T>` 取代原生 `Object.FindAnyObjectByType` / `FindObjectsByType`。

### 3.2 來源與依賴隔離盤點
* **共用層**：引用 `Yu5h1Lib.Common`、`Yu5h1Lib.Library` 與 `com.yu5h1.animation`（Editor `HumanoidImporter` 匯入 `LIMB-ANIM-1` 人形資產）。
* **套件邊界**：完全隔離於舊 2D `CharacterController2D`、`AttributeBehaviour` 及舊 `GameManager`，不依賴任何 2D 專案元件。

---

## 4. 操作與控制指南

### 4.1 握手互動操作（場景：`Handshake/Handshake.unity`）

| 操作輸入 | 動作行為 | 可觀察標準與反饋 |
|---|---|---|
| **互動鍵 Hold（P1 預設 `E`、P2 預設 `Keypad3`，可於 Inspector 或 HUD 改鍵）** | 發起伸手與維持等待 | 站立常態（`None`）下按住互動鍵：播放 `handshake-reach`（`0.50s`），視鏡轉黃色；伸手完成後進入 `Waiting` 並循環播放 `handshake-wait`（`2.00s`）。 |
| **互動鍵 Release** | 收手取消 | 在 `Reaching` 或 `Waiting` 期間放開互動鍵：播放 `handshake-withdraw`（`0.50s`），完成後回到站立常態（`None`，視鏡還原藍色）。 |
| **W / A / S / D + 互動鍵 Hold** | 走近回應與右手碰撞接觸 | 對方伸手時，按住互動鍵並操作 WASD 走近（伸手期間慢步 `2.2 m/s`）。當雙方右手碰撞球（`RightHandContactCollider`，半徑 `0.25m`）實際接觸時，`HandshakeArbitrator` 判定「碰到」，雙方進入 `Grip` 並播放 `handshake-grip`（`0.50s`，視鏡轉紫色）。 |
| **握緊期間持續按住互動鍵** | 握手成功與搖一下 | `Grip`（`0.50s`）結束時雙方仍按住互動鍵：裁定**握手成功 (`Success`)**，雙方進入 `Shaking` 並播放 `handshake-shake`（`0.50s`，視鏡亮綠色），結束後自動經 `Withdrawing`（`0.50s`）收手回站立。 |
| **握緊期間單方放開互動鍵** | 惡作劇 (Prank) | `Grip` 期間僅一方放開互動鍵：放開方立刻播收手將手抽回，另一方保持握緊姿態；握緊時長結束時裁定**惡作劇 (`Prank`)** 並標註放開者姓名，視鏡與 HUD 呈現結果，雙方平滑收手回站立。 |
| **握緊期間雙方放開互動鍵** | 同時收手 (BothWithdraw) | `Grip` 期間雙方均於握緊結束前放開互動鍵：雙方放開時各自抽手收回；握緊時長結束時裁定**同時收手 (`BothWithdraw`)**，視鏡與 HUD 呈現結果，雙方收手回站立。 |
| **結束後仍按住互動鍵** | 要求先放開再按 | 成功、惡作劇、同時收手或取消收手回到 `None` 時若仍按住互動鍵，HUD 顯示 `(需先放開再按)`，角色維持站立不重新伸手；放開後再次按下才發起新一次伸手。 |
| **R 鍵 / HUD 按鈕** | 全場統一快速重設 | 呼叫 `CombatEntity.ResetAllArenaEntities()`，將玩家與 AI 重設回初始面對面位置（相距 `2.4m`）、清除 AI 計時與判定會話殘留。 |

### 4.2 徒手對戰操作（場景：`Combat/WhiteRoom.unity`，暫停中）

| 操作輸入 | 動作行為 | 可觀察標準與反饋 |
|---|---|---|
| **W / A / S / D** | 視角 8 方向移動 | 角色以鏡頭水平面為基準轉向輸入方向並前進。 |
| **Space** | 起跳與空中位移 | 垂直起跳（高度 `1.5m`，重力 `-20m/s²`）；著地後立即恢復移動與出招。 |
| **Left Ctrl (Hold) / C (Toggle)** | 蹲下維持／切換 | 蹲姿碰撞高度減半；放開或再按恢復站立。 |
| **滑鼠左鍵 (Click)** | 原地／方向出招 | 歷經 Startup (`0.20s`) ➔ Active (`0.15s`) ➔ Recovery (`0.25s`)。 |
| **滑鼠右鍵 (Hold)** | 普通防禦與警戒慢步 | 正面 120 度錐形減傷 80%，防禦中可以 `2.2 m/s` 慢步移動。 |
| **滑鼠右鍵 (Tap <=0.22s)** | 精準防禦 (Parry) | 時窗 `0.18s` 內正面受擊免疫傷害，並打斷攻擊者使其陷入 `1.0s` 暈眩。 |
| **R 鍵 / HUD 按鈕** | 全場統一重設 | 重設所有角色位置、生命與 AI 狀態。 |

---

## 5. 人形動作資產與 Runtime Humanoid IK

`Samples~/WhiteRoom/Art/Handshake/` 包含由動畫線（`W:\Yu5h1Lib\AnimationEditor\samples\handshake\*.anim.json` 搭配 `punch-puppet.limb.json`）匯出並透過 `com.yu5h1.animation` 之 `HumanoidImporter`（`LIMB-ANIM-1`）建立的人形資產：
* **角色模型與 Avatar**：`punch-character.glb` ＋ `punch-character.humanoid.asset`（`punch-characterAvatar`，`isHuman = true`）。
* **5 個第 1 批人形動作片段**：
  1. `handshake-reach.humanoid.asset`（長度 `0.500s`，`isHumanMotion = true`，單次播放）
  2. `handshake-wait.humanoid.asset`（長度 `2.000s`，`isHumanMotion = true`，循環播放）
  3. `handshake-withdraw.humanoid.asset`（長度 `0.500s`，`isHumanMotion = true`，單次播放）
  4. `handshake-grip.humanoid.asset`（長度 `0.500s`，`isHumanMotion = true`，單次播放）
  5. `handshake-shake.humanoid.asset`（長度 `0.500s`，`isHumanMotion = true`，單次播放）
* **Animator Controller**：`HandshakeAnimator.controller`（Base Layer 啟用 `iKPass = true`，包含 `Idle`、`Reach`、`Wait`、`Grip`、`Shake`、`Withdraw` 六個狀態）。
* **Runtime Humanoid IK (`HandshakeVisualFeedback.OnAnimatorIK`)**：
  * 在 `Reaching`、`Waiting`、`Grip`、`Shaking` 狀態下，以 `Animator.SetIKPositionWeight(AvatarIKGoal.RightHand, weight)` 與 `SetIKPosition` 將右手對準雙方手部中點目標。
  * 右手碰撞體（`RightHandContactCollider`）每幀於 `OnAnimatorIK` 套用 IK 前先同步至純動畫手腕延伸位置，確保 IK 只服務視覺對準，不影響物理觸發判定。

---

## 6. 驗證紀錄與證據分界

> **重要說明**：實體鍵盤與滑鼠的親手操作手感驗收由使用者本人執行；依據使用者交辦，**不再要求 agent 截圖，改交事件紀錄、測試結果與 Windows 執行檔**。下列 PlayMode 測試組件與結構化日誌為**輔助驗證**，不冒充使用者本人的手動鍵盤操作。

| 驗證項目 | 執行方式 | 結果與證據 |
|---|---|---|
| **套件與 Sample 編譯** | Unity `6000.6.0f1` 宿主 (`Yu5h1LibTest`) | **0 錯誤、0 警告**（`Runtime`、`Handshake`、`Tests` 三個 asmdef 均編譯通過）。 |
| **場景直接可操作性檢查** | 開啟 `Handshake/Handshake.unity` 進入 Play Mode | **通過**。場景無掛載 `HandshakeVerificationRunner`，直接引用預製物件（`WhiteRoom_Player`、`WhiteRoom_AI`、`WhiteRoom_Camera`）；進入 Play Mode 後 `playerOverride = False`，AI 預設為 `AiInitiator` 伸手等待，玩家可直接控制角色。 |
| **輔助案例 1～21 全套情境** | `Tests/HandshakeVerificationRunner.cs` | **PASS (21/21 全數通過)**。包含取消、回應、握住、握手防重發、惡作劇（雙向）、同時收手（記錄時間戳與先後順序）、握緊後放開時間戳、比力氣（AI勝、玩家勝、平手、0比0）、抓住、被抓動作驅動根位移跳開與骨骼臂長門檻（1.06m）掙脫、被抓重按累計次數保留、抓方放開比力氣、追上防掙脫、原地起跳未超門檻維持被抓、全場重設一致性。 |
| **PlayMode 驗證項目 1：控制區間真的決定操作** | `Tests/HandshakePlayModeTests.cs` | **PASS**。`VerifyItem1_ControlIntervalsGovernRePress` 驗證不可控制區間重按無效、可控制區間重按立即伸手。 |
| **PlayMode 驗證項目 2：片段真的播得出來** | `Tests/HandshakePlayModeTests.cs` | **PASS**。`VerifyItem2_ClipsActuallyPlayInAnimator` 遍歷所有 17 個狀態確認 Animator 正在播放片段與設定資產指派 100% 一致。 |
| **PlayMode 驗證項目 3：動作驅動根位移** | `Tests/HandshakePlayModeTests.cs` | **PASS**。`VerifyItem3_RootMotionDisplacementInCaughtCatchingLeap` 實測被抓抽手水平位移 > 0.02m、跳開垂直位移 > 0.04m 與水平位移 > 0.03m。 |
| **PlayMode 驗證項目 4：標準距離走進與自然手球** | `Tests/HandshakePlayModeTests.cs` | **PASS**。`VerifyItem4_StandardDistanceWalkingAndNaturalHandSphere` 驗證膠囊體不阻擋骨盆中心相距 0.80m（抵達間距 <= 0.85m）且手部碰撞球半徑 <= 0.25m（0.18m）自然接觸觸發握手。 |
| **Pre-IK 碰撞代理球檢查** | `Tests/HandshakePlayModeTests.cs` | **PASS**。`VerifyPreIkContactSpherePosition` 實測手部接觸代理球在手旁 0.20m IK 目標下漂移量 < 0.05m，100% 免疫於 IK。 |
| **戰鬥場景回歸檢查** | `Tests/HandshakePlayModeTests.cs` | **PASS**。戰鬥場景跳躍、空中不可出招、著地還原、著地後出招許可、邊緣墜落與事件紀錄（`VerifyCombatSceneJumpLandingAndAttackPermissions`）及鏡頭 LookAt 跟拍（`VerifyCombatCameraLookAtTracking`）全數通過。 |
| **Windows 獨立執行檔交付** | Unity Editor 批次建置（指定場景、不改宿主建置設定） | **通過**。建置輸出於 `W:\UnityProject\Yu5h1LibTest-Build\WhiteRoom_Handshake\WhiteRoom_Handshake.exe`（repo 外部），大小約 115 MB，包含 17 段動畫與專屬場景，可於 Windows 64-bit 獨立直接運行。 |
| **結構化日誌匯出** | `WhiteRoomHUD.ExportStructuredLogs()` | **通過**。匯出 `Documentation/WhiteRoom_Handshake_Logs.json`，共 **218 筆**有效結構化紀錄，完整涵蓋全部 21 項輔助案例之狀態、次數、拉扯距離與裁決轉換。 |
| **待使用者本人親手驗收項目** | 實體鍵盤 WASD + E / R 與 HUD 操作 | **待使用者驗收**（交付 Windows 獨立執行檔供實機體驗實體連按手感、抓住與跳躍彈開手感、情境按鈕測試）。 |

---

## 7. 後續階段規劃

1. **握手第 1 批**：發起、取消、走近與碰觸握手成功（**已完成**）。
2. **握手第 2 批**：惡作劇與同時收手（**已完成**：三向裁決、抽手表現、反應動作槽位、持續按住防重發、可配置 AI 時序）。
3. **握手第 3 批（含第二、三輪修正）**：比力氣與拉扯（**已完成**：控制區間由動作標記決定、17 段含手指片段動態綁定且 Animator 真實播放、被抓/抓住/跳開動作驅動根位移、骨骼臂長測量門檻 1.06m 無寫死 fallback、角色膠囊半徑 0.30m 與自然手球 0.18m 達標標準距離 0.80m、Pre-IK 碰撞代理隔離、PlayMode 8/8 組件全數通過、21/21 握手情境全數 PASS、Windows 執行檔交付）。
4. **戰鬥驗證重啟**：依據計畫時程，銜接至真實網路傳輸層整合。
