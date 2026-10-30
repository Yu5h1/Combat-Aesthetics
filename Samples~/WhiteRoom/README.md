# WhiteRoom：3D 測試場 Sample（握手互動與徒手對戰）

> 套件名稱：`com.yu5h1.combataesthetic`  
> 驗證宿主：Unity `6000.6.0f1` URP（`W:/UnityProject/Yu5h1LibTest` 實測 0 編譯錯誤、PlayMode 畫面與互動檢查通過、輔助自動案例 7/7 通過）  
> 規範文件：[WhiteRoom 實作計畫](Implementation-Plan.md) ｜ [既有能力對照](Existing-Capabilities.md) ｜ [WhiteRoom 靜態與動態驗收紀錄](../../Documentation/WhiteRoom-Acceptance.md)

---

## 1. 簡介與定位

WhiteRoom 是 CombatAesthetic 套件的官方 3D 最小測試場範例（Sample）。包含兩個獨立場景，並共用同一套 `Samples~/WhiteRoom/Runtime/`：
1. **第一個交付：移動＋握手互動場景 (`Handshake/Handshake.unity`)**：
   - 依照 Stage 2b 與「第 1 批修正交辦」規範，實作第 1 批握手互動：發起伸手、中途取消收手、走近回應、碰觸握緊至成功搖手，以及握緊中途放開直接收手（標明第 2 批情況）。
   - **重用共用 `Runtime/`**：直接重用 `CombatEntity`（移動、跳躍、重力與全場重設）、`WhiteRoomCharacter`（P1/P2 輸入採集與改鍵）、`WhiteRoomHUD`（共用 IMGUI 面板與 JSON 日誌匯出）及 `WhiteRoomSceneBuilder`（共用場地、燈光、相機與 HUD 建置），不複製第二份角色移動或 HUD 程式。
   - **嚴格落實判定與表現分離**：
     - **碰觸判定**：僅當雙方右手碰撞體（`RightHandContactCollider`，`SphereCollider` trigger 半徑 `0.22m`）發生真實物理接觸並對應到該兩隻右手（`HandContactBridge`）時才算「碰到」，移除任何手部或身體距離門檻捷徑。
     - **時間來源**：伸手（`0.50s`）、等待（`2.00s` 循環）、收手（`0.50s`）、握緊（`0.50s`）、搖一下（`0.50s`）時長直接讀取自 `LIMB-ANIM-1` 匯入之人形 `AnimationClip.length`，不在 C# 寫死秒數。
     - **人形動畫與 Runtime Humanoid IK**：採用 `punch-character.glb` 人形骨架與 `HandshakeAnimator.controller` 播放 5 個第 1 批人形動作片段；`HandshakeVisualFeedback.OnAnimatorIK` 在 `Reach` / `Wait` / `Grip` / `Shaking` 期間以 `AvatarIKGoal.RightHand` 將右手對準對方右手位置，且碰撞體錨定於 IK 計算前的手腕姿勢，確保 IK 僅影響畫面呈現而不干擾接觸判定。
   - **重新按下才發起**：一次握手結束或收手回到站立（`None`）後，若玩家或 AI 仍持續按住互動鍵，不會再次伸手；必須先放開互動鍵再重新按下才會發起下一次伸手。
   - **Sample 場景僅含可操作內容**：`Handshake.unity` 不掛載自動測試腳本，進入 Play Mode 後即可由玩家直接操作；輔助自動化測試器移至 `Samples~/WhiteRoom/Tests/HandshakeVerificationRunner.cs`。
2. **戰鬥驗證場景 (`Combat/WhiteRoom.unity`)**（目前暫停，保留至戰鬥驗證重啟）：
   - 驗證第三人稱徒手攻防：八方向移動、跳躍落地恢復、原地/方向出招、警戒慢步、普通防禦、精準彈刀反制、受擊反應與受力因果。
   - 本機玩家、AI 傀儡與遠端連線角色共用同一套 `CombatEntity` 與 `CombatActionIntent` 合法性規則。

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
│  ├─ HandshakeAiController.cs 可設定之 AI 握手控制器（伸手延遲、按住時長、是否走近、停止距離）
│  ├─ HandshakeArbitrator.cs  權威握手判定核心（右手碰撞接觸、握緊、搖手、收手裁決）
│  ├─ HandshakeParticipant.cs 握手參與者狀態機（組合共用 CombatEntity 與 AnimationClip 時長）
│  ├─ HandshakeSceneBuilder.cs 握手場景建置器（重用 WhiteRoomSceneBuilder 共用場地/鏡頭/HUD）
│  ├─ HandshakeState.cs       握手狀態機與正式裁決結果列舉
│  ├─ HandshakeVisualFeedback.cs 人形 Animator 驅動、Runtime Humanoid IK 與視鏡顏色表現層
│  └─ Yu5h1Lib.CombatAesthetic.Samples.WhiteRoom.Handshake.asmdef
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
   ├─ HandshakeVerificationRunner.cs 握手第 1 批 7 項輔助自動化驗證腳本
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
| **W / A / S / D + 互動鍵 Hold** | 走近回應與右手碰撞接觸 | 對方伸手時，按住互動鍵並操作 WASD 走近（伸手期間慢步 `2.2 m/s`）。當雙方右手碰撞球（`RightHandContactCollider`，半徑 `0.22m`）實際接觸時，`HandshakeArbitrator` 判定「碰到」，雙方進入 `Grip` 並播放 `handshake-grip`（`0.50s`，視鏡轉紫色）。 |
| **握緊期間持續按住互動鍵** | 握手成功與搖一下 | `Grip`（`0.50s`）結束時雙方仍按住互動鍵：裁定**握手成功 (`Success`)**，雙方進入 `Shaking` 並播放 `handshake-shake`（`0.50s`，視鏡亮綠色），結束後自動經 `Withdrawing`（`0.50s`）收手回站立。 |
| **握緊期間放開互動鍵** | 中途放開直接收手 | `Grip` 期間任一方提前放開互動鍵：裁定 `EarlyReleaseDuringGrip`（標明第 2 批情況），雙方立即進入 `Withdrawing` 收手回站立，視鏡轉灰。 |
| **結束後仍按住互動鍵** | 要求先放開再按 | 握手完成或收手回到 `None` 時若仍按住互動鍵，HUD 顯示 `(需先放開再按)`，角色維持站立不重新伸手；放開後再次按下才發起新一次伸手。 |
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

> **重要說明**：實體鍵盤與滑鼠的親手操作手感驗收由使用者本人執行；下列 PlayMode 畫面擷取與 `HandshakeVerificationRunner` 測試案例為**輔助驗證**，不冒充使用者本人的手動鍵盤操作。

| 驗證項目 | 執行方式 | 結果與證據 |
|---|---|---|
| **套件與 Sample 編譯** | Unity `6000.6.0f1` 宿主 (`Yu5h1LibTest`) | **0 錯誤、0 警告**（`Runtime`、`Handshake`、`Tests` 三個 asmdef 均編譯通過）。 |
| **場景直接可操作性檢查** | 開啟 `Handshake/Handshake.unity` 進入 Play Mode | **通過**。場景無掛載 `HandshakeVerificationRunner`；進入 Play Mode 後 `playerOverride = False`，AI 預設為 `AiInitiator` 伸手等待，玩家可直接控制角色（見截圖 `Documentation/Screenshots/Handshake_PlayMode_Initial.png`）。 |
| **輔助案例 1：玩家發起後放開取消** | `Tests/HandshakeVerificationRunner.cs` | **PASS**。按住 E 進入 `Reaching`，放開 E 經 `Withdrawing`（`0.50s`）返回 `None`。 |
| **輔助案例 2：AI 發起、玩家走近回應成功** | `Tests/HandshakeVerificationRunner.cs` | **PASS**。AI 伸手等待，玩家按住 E 走近至右手 `SphereCollider`（半徑 `0.22m`）實際接觸，歷經 `Grip`（`0.50s`）與 `Shaking`（`0.50s`），裁定 `Success`（見截圖 `Documentation/Screenshots/Handshake_PlayMode_Success.png`）。 |
| **輔助案例 3：未放開 E 不重新發起（第 10 項）** | `Tests/HandshakeVerificationRunner.cs` | **PASS**。握手結束回到 `None` 後持續按住 E 達 `0.35s`，狀態維持 `None` 不再伸手；放開 `0.10s` 後再按下才重新進入 `Reaching`。 |
| **輔助案例 4：玩家發起、AI 走近回應成功** | `Tests/HandshakeVerificationRunner.cs` | **PASS**。玩家原地伸手，AI 走近使右手碰撞體接觸，完成 `Grip` ➔ `Shaking` ➔ `Success`。 |
| **輔助案例 5：握緊期間提前放開收手** | `Tests/HandshakeVerificationRunner.cs` | **PASS**。`Grip` 期間 AI 於 `0.18s` 提前放開，裁定 `EarlyReleaseDuringGrip`（第 2 批情況），雙方立即收手。 |
| **輔助案例 6：AI 發起後依設定時長取消** | `Tests/HandshakeVerificationRunner.cs` | **PASS**。AI 伸手後依可設定之 `_cancelHoldDuration`（測試設為 `0.60s`）放開取消，經 `Withdrawing` 返回 `None`。 |
| **輔助案例 7：全場統一快速重設** | `Tests/HandshakeVerificationRunner.cs` | **PASS**。呼叫 `CombatEntity.ResetAllArenaEntities()`，雙方回到 `(0, 0, -1.2)` 與 `(0, 0, 1.2)`、狀態 `None`、判定會話無殘留。 |
| **結構化日誌匯出** | `WhiteRoomHUD.ExportStructuredLogs()` | **通過**。匯出 `Documentation/WhiteRoom_Handshake_Logs.json`，共 **43 筆**結構化紀錄，涵蓋全部 7 項輔助案例之狀態與裁決轉換。 |
| **戰鬥場景相容性檢查** | 開啟 `Combat/WhiteRoom.unity` 進入 Play Mode | **通過**。共用 `Runtime/` 修改後，戰鬥場景正常生成玩家與 AI（`OnePlayerVsOneAI`），無編譯或執行期錯誤。 |
| **待使用者本人親手驗收項目** | 實體鍵盤 WASD + E / R 與 HUD 操作 | **待使用者驗收**（包含實體按鍵手感、改鍵操作、攝影機視角與畫面微調感受）。 |

---

## 7. 後續階段規劃

1. **握手第 2 批**：惡作劇收手（對方握空反應）、同時收手、進階姿勢過渡。
2. **握手第 3 批**：握手比力氣（Strength Contest 力量對抗機制）。
3. **戰鬥驗證重啟**：依據計畫時程，銜接至真實網路傳輸層整合。
