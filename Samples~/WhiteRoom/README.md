# WhiteRoom：3D 測試場 Sample（握手互動與徒手對戰）

> 套件名稱：`com.yu5h1.combataesthetic`  
> 驗證宿主：Unity `6000.6.0f1` URP（`W:/UnityProject/Yu5h1LibTest` 實測 0 編譯錯誤、PlayMode 測試組件 4/4 通過、握手互動案例 21/21 全數通過、Windows Standalone 執行檔建置成功）  
> 規範文件：[WhiteRoom 實作計畫](Implementation-Plan.md) ｜ [既有能力對照](Existing-Capabilities.md) ｜ [WhiteRoom 靜態與動態驗收紀錄](../../Documentation/WhiteRoom-Acceptance.md)

---

## 1. 簡介與定位

WhiteRoom 是 CombatAesthetic 套件的官方 3D 最小測試場範例（Sample）。包含兩個獨立場景，並共用同一套 `Samples~/WhiteRoom/Runtime/`：
1. **第一個交付：移動＋握手互動場景 (`Handshake/Handshake.unity`)**：
   - 依照 Stage 2b「第 3 批交辦：比力氣」及「第 3 批第二輪修正交辦」規範，完整實作握手互動系統：發起伸手、中途取消收手、走近回應、碰觸握緊、握手成功、惡作劇（單方放開）、同時收手（雙方放開）、握住、比力氣對抗、抓住與掙脫。
   - **判定與表現嚴格分離**：
     - **碰觸判定**：僅當雙方右手碰撞體（`RightHandContactCollider`，`SphereCollider` trigger 半徑 `0.40m`，計入 lossyScale 縮放）發生真實物理接觸並對應到該兩隻右手（`HandContactBridge`）時才算「碰到」，移除任何手部或身體距離門檻捷徑。
     - **握緊裁決與握住過渡**：握緊（`0.50s`）期間有人放開互動鍵時，放開方立刻播收手抽回；握緊結束時雙方均放開裁定同時收手 (`BothWithdraw`)、單方放開裁定惡作劇 (`Prank`) 並由動畫具名區間鎖定放開者在反應結束前不可重新發起；雙方均按住則成功進入握住 (`Holding`)。
     - **比力氣對抗**：握住期間連按 E 計算按下次數；連按時短暫放開容許時間暫定 `0.30s`（可調）；雙方均放開時判定元件比次數：差距超過門檻（預設 1 次）為勝負已分 (`StrengthContestWin`)、門檻內且有人按過為平手慢退 (`StrengthContestTie`)、無人按為 0 比 0 搖手收手 (`StrengthContestZeroZero`)。
     - **抓住與掙脫**：一方放開超過容許時間、另一方按住時記為抓住 (`Caught`/`Catching`)；被抓方以角色走路速度移動（`5.0 m/s`，`[暫代]`）、抓人方以慢步移動（`2.2 m/s`，`[暫代]`）；雙方皆可跳躍，被抓方跳起時往後彈開；身體距離超過骨骼手臂量測門檻加上補償值 `EscapeDistanceCompensation`（移除 `Mathf.Max` 覆寫）裁定掙脫 (`Escaped`)，不比次數；被抓方於被抓同一格或容許時間內重按互動鍵立即重回雙方握住且保留連按次數（+1）。
     - **時間來源與動態片段綁定**：動作片段時長直接取自 `AnimationClip.length`；`HandshakeVisualFeedback` 透過動態 `AnimatorOverrideController` 將 `HandshakeSettings.asset` 中指派的動作片段（含 `Caught` 與 `Catching`）映射至 Animator Controller 之所有狀態節點；操作許可由動畫具名區間標記（`ActionInterval` / `ClipIntervalBinding`）決定，徹底移除固定計時器。
     - **Runtime Humanoid IK 與 Pre-IK 碰撞球擺放**：在握緊、搖手、握住期間以權重 0.85 對準雙手中點；被抓住期間以權重 0.95 對準抓人方右手表現手臂拉直。碰撞球在 `OnAnimatorIK` 開頭以純動畫姿態擺放，100% 不受 IK 牽引影響判定。
   - **重新按下才發起**：所有結果（成功、惡作劇、同時收手、比力氣勝負/平手/0比0、掙脫）裁定後若持續按住互動鍵，維持站姿不自動伸手；必須先放開互動鍵再重新按下才發起。
   - **預製物件化與手工編輯 DX**：角色、AI 與鏡頭全面改為 Prefab（`WhiteRoom_Player.prefab`、`WhiteRoom_AI.prefab`、`WhiteRoom_Camera.prefab` 位於 `Samples~/WhiteRoom/Prefabs/`），場景直接引用且可在 Editor 直接調校；`HandshakeSettings` ScriptableObject 提供集中參數管理，Play Mode 調整值可即時保存。
   - **Windows 獨立執行檔交付與證據**：建置輸出於 `W:\UnityProject\Yu5h1LibTest-Build\WhiteRoom_Handshake\WhiteRoom_Handshake.exe`（repo 外部），指定場景建置不修改宿主建置設定；結構化事件日誌 `Documentation/WhiteRoom_Handshake_Logs.json`（175 筆）。依指令全面廢止 agent 截圖，以執行檔與日誌為準。
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
| **輔助案例 1：玩家發起後取消** | `Tests/HandshakeVerificationRunner.cs` | **PASS**。按住 E 進入 `Reaching`，放開 E 經 `Withdrawing`（`0.50s`）返回 `None`。 |
| **輔助案例 2：AI 發起後取消** | `Tests/HandshakeVerificationRunner.cs` | **PASS**。AI 發起伸手等待後放開取消收手回到 `None`。 |
| **輔助案例 3：AI 發起、玩家走近回應成功進入握住** | `Tests/HandshakeVerificationRunner.cs` | **PASS**。AI 伸手等待，玩家按住 E 走近至右手 `SphereCollider`（半徑 `0.40m`）實際接觸，歷經 `Grip`（`0.50s`），裁定正式結果 `Success` 並進入 `Holding` 維持緊握姿勢不搖手。 |
| **輔助案例 4：成功後持續按住維持站立防重發** | `Tests/HandshakeVerificationRunner.cs` | **PASS**。握手成功後持續按住 E 維持站立，放開再按下才重新發起伸手。 |
| **輔助案例 5：玩家發起、AI 走近回應成功進入握住** | `Tests/HandshakeVerificationRunner.cs` | **PASS**。玩家原地伸手，AI 走近接觸，完成 `Grip` ➔ `Holding`。 |
| **輔助案例 6：玩家握緊中途放開（惡作劇）** | `Tests/HandshakeVerificationRunner.cs` | **PASS**。`Grip` 期間玩家提前放開 E 立即抽手，握緊結束裁定 `Prank`，標註放開者為 Player1。 |
| **輔助案例 7：惡作劇後持續按住維持站立防重發** | `Tests/HandshakeVerificationRunner.cs` | **PASS**。惡作劇後持續按住 E 維持站立，放開清除 latch。 |
| **輔助案例 8：AI 握緊中途放開（惡作劇）** | `Tests/HandshakeVerificationRunner.cs` | **PASS**。`Grip` 期間 AI 提前放開，AI 立即抽手，握緊結束裁定 `Prank`，標註放開者為 AI_Opponent。 |
| **輔助案例 9：雙方握緊放開（同時收手）附時間戳順序** | `Tests/HandshakeVerificationRunner.cs` | **PASS**。雙方均在握緊結束前放開，各自抽手，握緊計時結束時裁定 `BothWithdraw`，精確記錄雙方真實放開時間戳與先後順序（`pTime < aiTime`）。 |
| **輔助案例 10：同時收手後持續按住維持站立防重發** | `Tests/HandshakeVerificationRunner.cs` | **PASS**。同時收手後持續按住 E 維持站立，放開再按才重新發起。 |
| **輔助案例 11：握緊結束後才放開記錄放開時間戳** | `Tests/HandshakeVerificationRunner.cs` | **PASS**。握緊結束完成握住後才放開，記錄放開時間戳（`16.73s`）。 |
| **輔助案例 12：比力氣 AI 勝（次數差超過門檻）** | `Tests/HandshakeVerificationRunner.cs` | **PASS**。握住期間玩家按 0 次、AI 連按 5 次，雙方放開後裁定 `StrengthContestWin`，勝方為 AI。 |
| **輔助案例 13：比力氣玩家勝（次數差超過門檻）** | `Tests/HandshakeVerificationRunner.cs` | **PASS**。握住期間玩家按 3 次、AI 按 0 次，雙方放開後裁定 `StrengthContestWin`，勝方為玩家。 |
| **輔助案例 14：比力氣平手（門檻內且有人按過）** | `Tests/HandshakeVerificationRunner.cs` | **PASS**。握住期間雙方各按 2 次，雙方放開後裁定 `StrengthContestTie`。 |
| **輔助案例 15：比力氣 0 比 0（無人連按搖一下）** | `Tests/HandshakeVerificationRunner.cs` | **PASS**。雙方均未連按，放開後裁定 `StrengthContestZeroZero`，搖一下各自收手。 |
| **輔助案例 16：抓住與身體距離超標掙脫** | `Tests/HandshakeVerificationRunner.cs` | **PASS**。一方放開超時被抓住（`Caught`/`Catching`），拉扯移動與跳躍使身體距離超過骨骼手臂量測門檻加上補償值（`1.80m`），裁定 `Escaped`，不比次數。 |
| **輔助案例 17：被抓中重按互動鍵返回握住（累計不歸零）** | `Tests/HandshakeVerificationRunner.cs` | **PASS**。被抓方重新按住 E 立即回到雙方握住（`Holding`），連按累計次數（由 2 次累計至 3 次）完整保留。 |
| **輔助案例 18：抓方亦放開結束握住照次數比力氣** | `Tests/HandshakeVerificationRunner.cs` | **PASS**。抓方亦放開視同雙方放開，結束握住並照次數裁定比力氣（`StrengthContestZeroZero`）。 |
| **輔助案例 19：抓方慢步追上防超標掙脫** | `Tests/HandshakeVerificationRunner.cs` | **PASS**。被抓方後退，抓方慢步跟隨（`2.2 m/s`），雙方身體距離維持在門檻內，未觸發掙脫。 |
| **輔助案例 20：被抓方起跳未超門檻維持被抓** | `Tests/HandshakeVerificationRunner.cs` | **PASS**。被抓方起跳真實離地（`!isGrounded`），空中套用水平位移，身體距離維持在門檻內，維持被抓狀態。 |
| **輔助案例 21：全場快速重設一致性** | `Tests/HandshakeVerificationRunner.cs` | **PASS**。呼叫 `CombatEntity.ResetAllArenaEntities()`，雙方乾淨回到初始點、狀態 `None`、判定會話無殘留。 |
| **Windows 獨立執行檔交付** | Unity Editor 批次建置（指定場景、不改宿主建置設定） | **通過**。建置輸出於 `W:\UnityProject\Yu5h1LibTest-Build\WhiteRoom_Handshake\WhiteRoom_Handshake.exe`（repo 外部），大小約 667 KB，附帶 UnityPlayer.dll 與 Data 目錄，可於 Windows 64-bit 獨立直接運行。 |
| **結構化日誌匯出** | `WhiteRoomHUD.ExportStructuredLogs()` | **通過**。匯出 `Documentation/WhiteRoom_Handshake_Logs.json`，共 **175 筆**有效結構化紀錄，完整涵蓋全部 21 項輔助案例之狀態、次數、拉扯距離與裁決轉換。 |
| **戰鬥場景回歸檢查** | `Tests/HandshakePlayModeTests.cs` | **通過**。戰鬥場景跳躍、空中不可出招、著地還原、著地後出招許可、邊緣墜落與事件紀錄（`VerifyCombatSceneJumpLandingAndAttackPermissions`）及鏡頭 LookAt 跟拍（`VerifyCombatCameraLookAtTracking`）全數 PASS。 |
| **Pre-IK 碰撞代理球檢查** | `Tests/HandshakePlayModeTests.cs` | **通過**。`VerifyPreIkContactSpherePosition` 實測碰撞代理球位置於套用 IK 前錨定至骨骼手腕延伸位置，漂移量 < 0.35m 通過。 |
| **待使用者本人親手驗收項目** | 實體鍵盤 WASD + E / R 與 HUD 操作 | **待使用者驗收**（交付 Windows 獨立執行檔供實機體驗實體連按手感、抓住與跳躍彈開手感、情境按鈕測試）。 |

---

## 7. 後續階段規劃

1. **握手第 1 批**：發起、取消、走近與碰觸握手成功（**已完成**）。
2. **握手第 2 批**：惡作劇與同時收手（**已完成**：三向裁決、抽手表現、反應動作槽位、持續按住防重發、可配置 AI 時序）。
3. **握手第 3 批（含第二輪修正）**：比力氣與修正（**已完成**：控制區間綁定動畫、AnimatorOverrideController 動態片段綁定、走路/慢步 [暫代] 根位移、骨架測量門檻加補償、抓住真跳躍、被抓重按累計次數保留、場景預製物件化、Pre-IK 碰撞球位置隔離、PlayMode 4/4 組件全數通過、21/21 握手情境全數 PASS、Windows 執行檔交付）。
4. **戰鬥驗證重啟**：依據計畫時程，銜接至真實網路傳輸層整合。
