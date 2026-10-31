# 通用互動系統設計稿

> 狀態：討論稿
>
> 與其他文件的關係：本文作為「通用互動系統」的架構討論稿，上承 [WhiteRoom 實作計畫](../../Samples~/WhiteRoom/Implementation-Plan.md) 之〈後續發展邊界〉「共用的雙人互動系統」與〈第一個交付的目標〉各列；為後續將握手原型抽換、重構為可資料驅動之通用互動系統時的參考設計。實作進度仍由 WhiteRoom [report.json](../../report.json) 與 [Implementation-Plan.md](../../Samples~/WhiteRoom/Implementation-Plan.md) 擁有。
>
> 握手程式讀取基準：commit `41f61db34d8978d589e58d7ee3b602dbf0f2864d`（`WhiteRoom: network evaluation brief starts from the existing network discussion`，最後握手程式提交為 `0d29bfb` 第 2 批惡作劇與同時收手；第 3 批程式正由另一 agent 並行修改中，本文只讀取 HEAD 基準）。

## 系統階層與關係

```text
CombatAesthetic (Package)
├─ Runtime / Combat (核心能力與既有戰鬥基礎)
│  ├─ CombatActionIntent (統一動作意圖結構：移動、跳躍、按住/按下互動、攻防標記)
│  └─ CombatEntity (角色的移動力學、物理碰撞體、接地、姿態限制與重設生命週期)
└─ Samples~/WhiteRoom
   ├─ 通用互動系統 (目標通用層，由握手經驗抽象化)
   │  ├─ 意圖接收與門禁 (Intent & Gatekeeping：重新按下判定、移動限制)
   │  ├─ 接觸偵測 (ContactBridge：多部位碰撞體/Trigger、非 IK 原生骨骼跟隨)
   │  ├─ 時間窗與仲裁器 (InteractionArbitrator：集中判定、片段長度驅動、超時處置)
   │  ├─ 正式結果與事件 (Authoritative Outcome & Event Log：單一事實來源、主機裁決)
   │  └─ 視覺與表現 (VisualFeedback & IK：單向接收結果、人形 IK 對準、表情與顏色反饋)
   ├─ 互動定義模型 (InteractionDefinition：資料化描述部位、階段、片段、IK、規則模組)
   ├─ 具體雙人社交互動應用 (現有 WhiteRoom/Handshake 驗證，後續擴充碰拳、擊掌、擁抱...)
   └─ 戰鬥交手應用 (WhiteRoom/Combat：單向發起、多人多對象、攻擊/防禦/受擊/擒拿)
```

**依賴與資料流向說明**：
- **控制與意圖流（由上而下）**：玩家輸入（`ICombatInputSource`）、AI 控制器或網路鏡像產生統一的 `CombatActionIntent`，注入 `CombatEntity`。通用互動系統監聽意圖，透過門禁規則（例如：必須釋放後重新按下）決定是否啟動或推進互動狀態。
- **物理與接觸流（橫向進入仲裁）**：角色骨骼上的 `ContactBridge` 在實體碰撞體接觸時回報給中央 `InteractionArbitrator`。為避免循環反饋，接觸偵測一律掛載在未受 IK 影響的動畫骨骼階層上。
- **裁定與同步（中央裁定，單向分發）**：`InteractionArbitrator` 依據雙方意圖、接觸時機與動畫時間窗產出正式結果（Authoritative Outcome）。聯網環境下由主機（Host）擁有此仲裁器，遠端僅作為輸入來源與結果接收端。
- **表現呈現（單向接收，無逆向回饋）**：視覺與 IK 元件（`VisualFeedback`）僅單向監聽狀態與結果，驅動 Animator 播放片段，並在 `OnAnimatorIK` 中拉動手腳對準目標。IK 的拉伸與偏移絕不逆向干擾碰撞偵測或仲裁判定。
- **戰鬥與雙人互動的關係**：戰鬥交手（Attack / Guard / Parry / Grapple）與雙人互動共用上述「意圖 → 接觸與時間窗 → 仲裁器裁定正式結果 → 表現呈現」之共同底層，但戰鬥具有「非對等／單向發起」、「可多人同時對一人」以及「狀態持續（硬直、擊飛、生命值不歸零）」等特性，屬於建立於同一底層上的平行應用分支，而非雙人社交互動的子類別。

---

## 摘要

通用互動系統旨在將 WhiteRoom 在「移動＋握手」第一交付中所驗證成熟的雙人互動機制（發起、回應、接觸判定、時間窗、取消、惡作劇、比力氣、抓住與掙脫），提升為可重用、可擴充的資料驅動框架。

本設計稿界定出純粹的**共同底層**（意圖、接觸、時間窗、判定、正式結果、表現），並規劃出彈性的**互動定義模型**（`InteractionDefinition`）。透過將具體動作的接觸部位、狀態階段、動畫片段、IK 錨點與行為規則（落空、握住、比力氣、拉扯、承載）模組化，讓未來新增碰拳、擊掌、擁抱、牽手乃至戰鬥擒拿時，只需編寫或配置定義，無需為每個動作複製整套仲裁邏輯。

---

## 問題與目標

### 要解決的問題
1. **握手程式高度特化**：HEAD commit `41f61db`（實作至第 2 批 `0d29bfb`）的握手架構中，狀態列舉（`HandshakeState`）、仲裁器（`HandshakeArbitrator`）、回饋（`HandshakeVisualFeedback`）均寫死為右手、雙人對稱、握緊、搖手與專屬的惡作劇大笑邏輯。若直接以此複製出碰拳或擁抱，會造成大量重複的狀態機與維護負擔。
2. **多互動難以平滑切換**：未來角色在場景中需依情境切換不同互動（HUD 選擇或距離接近觸發），若無統一的互動定義與通用參與者元件，角色身上將掛滿互不相干的特化腳本。
3. **戰鬥與社交互動界線不清**：戰鬥中的接拳、擒拿、格擋與社交握手、拉扯在力學與判定上本質相通，但過往常被切割為兩套獨立系統，導致受擊、位移約束與 IK 對準無法共用。

### 設計目標
1. **判定與表現嚴格分離**：正式結果（命中、接觸、成功、惡作劇、勝負、掙脫）由仲裁器唯一決定；動畫播放與人形 IK 僅負責畫面呈現，兩者解耦。
2. **規則可組裝（Modular Rules）**：將時間窗惡作劇、持續握住、連按比力氣、抓住與拉扯、角色承載等規則獨立為可選行為模組，依互動定義自由勾選或組合。
3. **資料驅動（Data-driven Configuration）**：互動參數（部位、時間窗、IK 權重、速度限制、動畫片段）抽離為獨立定義，新增社交動作無需重寫 C# 邏輯。
4. **支援雙人社交與戰鬥交手**：同一套底層可承載雙向同意的社交互動（握手、碰拳、擁抱），亦能無縫映射到單向強制、多人參與的戰鬥攻防（普通防禦、精準格擋、接拳、摔投）。

### 明確非目標
1. 本文**不修改現有程式碼**，不更動正由其他 agent 進行中的第 3 批握手實作檔案。
2. 本文**不擅自替使用者定案**「互動定義採用 ScriptableObject 或跨平台資料檔（尚未決定第 6 項）」，僅客觀分析兩種格式對設計的影響。
3. 本文**不包含大規模網路同步實作**，僅確保狀態機架構完全符合 Host 權威與時間戳校驗之聯網相容原則。

---

## 共同底層架構

通用互動系統的核心底層由 6 個正交職責構成：

```text
[玩家 / AI / 網路意圖] 
        │ CombatActionIntent (InteractHold, InteractDown, MoveInput, Timestamp)
        ▼
   ┌──────────┐      觸發實體接觸 (非 IK 骨骼)
   │ 意圖門禁 │ ────────────────────────────────┐
   └──────────┘                                 │
        │                                       ▼
        │ 狀態轉移 (Reaching / Waiting)     ┌──────────┐
        └─────────────────────────────────> │ 接觸偵測 │ (ContactBridge)
                                            └──────────┘
                                                │ ContactEvent (雙方部位、時間戳)
                                                ▼
                                     ┌──────────────────────┐
                                     │  中央仲裁器          │ ◄─── 動畫長度驅動時間窗
                                     │ (InteractionArbitrator)    (AnimationClip.length)
                                     └──────────────────────┘
                                                │
                                                │ 裁決正式結果 (Authoritative Outcome)
                        ┌───────────────────────┴───────────────────────┐
                        ▼                                               ▼
             ┌─────────────────────┐                         ┌─────────────────────┐
             │ 正式事件與主機狀態  │                         │ 視覺表現與人形 IK   │
             │ (Event Log / Sync)  │                         │ (VisualFeedback)    │
             └─────────────────────┘                         └─────────────────────┘
```

### 1. 意圖（Intent）
- **統一介面**：使用 `CombatActionIntent`。雙人社交互動主要消費 `InteractHold`、`InteractDown`、`InteractUp`、`MoveInput`、`Jump` 與 `Timestamp`；戰鬥則進一步消費 `Attack`、`GuardHold`、`ParryTap`。
- **重新按下約束（Re-press Enforcement）**：延續握手實作已確認的行為契約——一次互動結束回到站姿時，若互動鍵仍按著，角色維持站姿，必須放開再按下才能發起下一次互動。此門禁邏輯收斂於通用意圖轉譯層中，避免重複觸發。

### 2. 接觸（Contact）
- **骨骼實體碰撞**：由掛載於具體身體部位的 `ContactBridge`（包含 `Collider.isTrigger` 與 Kinematic `Rigidbody`）偵測實際接觸。
- **IK 隔離**：接觸碰撞體跟隨動畫原生骨骼（Raw Animation Transform），絕不受即時人形 IK（`OnAnimatorIK`）或 Procedural 修正的位置偏移干擾，杜絕「IK 引起碰撞，碰撞又改變 IK」的正回授問題。
- **事件簽章**：回報 `(ParticipantA, BoneA, ParticipantB, BoneB, ContactPoint, Timestamp)`，允許仲裁器核對是否為定義所要求的有效接觸部位。

### 3. 時間窗（Time Window）
- **片段長度驅動（Clip-driven Timing）**：時間窗長度原則上直接取自對應動畫片段的長度（`AnimationClip.length`），如「握緊結束」即為握緊片段長度，不額外發明硬編碼數值。
- **彈性容許時間（Tolerances）**：針對按鍵抖動與連按操作，提供微小容許時間窗（例如連按判定容許放開 0.3 秒內不算真正放開）。
- **時間戳驗證（Timestamped Window）**：在網路環境下，放開或按下的時機依據意圖發生的客戶端時間戳比對主機時間窗，避免因傳輸抖動造成誤判。

### 4. 判定（Arbitration）
- **集中權威判定**：場景中僅存在一個活動的仲裁器實例（聯網時由 Host 執行）。
- **參與者無自主權**：參與角色（`InteractionParticipant`）僅上報狀態與意圖，不得自行決定「握手成功」或「惡作劇成立」；所有狀態分支均由仲裁器統一評估雙方條件後回寫。

### 5. 正式結果（Authoritative Outcome）
- **明確的結果結構**：判定結果帶有完整上下文，例如：
  - `Cancelled`（單方或雙方於接觸前收手）
  - `Success`（接觸且完成預期動作）
  - `Miss / Prank`（帶出放開者與被惡作劇者）
  - `BothWithdraw`（雙方同時放開）
  - `ContestResolved`（比力氣結束，帶出勝方、次數差值與平手標記）
  - `Grabbed` / `Escaped`（抓住與掙脫）
  - `CarriageStateChanged`（承載建立或脫離）
- **單一事實來源**：結果觸發結構化事件 `OnInteractionFinished(InteractionOutcome)`，同時寫入 HUD 動作日誌與除錯紀錄。

### 6. 表現（Presentation）
- **單向消費**：視覺表現層接收到狀態切換與結果時，負責：
  - 切換 Animator State（播放對應片段）。
  - 在 `OnAnimatorIK` 中計算動態對準（例如將手掌移向對方的接觸手，或調整頭部 LookAt）。
  - 調整角色外殼材質顏色（Visual State Feedback）。
- **失敗降級（Graceful Fallback）**：當專屬動畫片段（如生氣、拍手大笑）尚未製作完成時，系統降級使用標準收手動作（Withdraw）回到站姿，並僅在 HUD 與事件紀錄顯示判定結果，不阻斷遊戲狀態機。

---

## 互動定義模型（Interaction Definition）

為實現「新增互動變化型只需新增定義，無需改動程式碼」，定義資產必須涵蓋以下維度：

```text
InteractionDefinition
├─ Identification (Id, DisplayName, Priority, Category)
├─ Initiation & Contact
│  ├─ InitiationType (Consensual 雙向同意 / Unilateral 單向強行 / Reactive 意外反應)
│  ├─ InitiatorContactBone (RightHand, LeftHand, Torso, Hips...)
│  └─ ResponderContactBone (RightHand, LeftHand, Torso, Any...)
├─ Phases & Clips Mapping
│  ├─ Reaching (Clips for Initiator / Responder, LocomotionRule)
│  ├─ Waiting (Clips, DistanceCheck)
│  ├─ Engaging (Clips: Grip, Hug, Bump; drives TimeWindow)
│  └─ Reactions (Success, PrankInitiator, PrankResponder, BothWithdraw...)
├─ Locomotion & IK Constraints
│  ├─ SpeedOverridePerPhase (Locked = 0, GuardSpeed, Normal, Custom)
│  ├─ IKGoal (RightHand, LeftHand, BothHands, None)
│  ├─ IKTargetStrategy (Midpoint, TargetBoneOffset, Socket)
│  └─ IKWeightCurve (Blend In / Blend Out)
└─ Rule Modules (模組化規則開關與參數)
   ├─ Miss / Prank Rule (時間窗內提早放開處置)
   ├─ Hold Rule (持續按住、放開容許緩衝時間)
   ├─ Contest Rule (連按計數、門檻差距比勝負)
   ├─ Grab & Break-free Rule (抽手轉抓住、移動速度差、身軀距離掙脫門檻)
   └─ Carriage Rule (主客附著、移動權限轉移、脫離條件)
```

### 1. 接觸部位（Contact Points）
- 定義發起方（Initiator）與回應方（Responder）分別以哪一個骨骼碰撞體接觸算作有效。
- 支援單一部位（右手對右手）、跨部位（掌心對拳頭、手臂對軀幹），以及「無物理接觸」（如猜拳，僅依賴身軀距離與視野朝向）。

### 2. 階段劃分（Phases）
互動生命週期切分為標準階段：
1. `Initiating`（發起動作中，如伸手）：受不可控制區間保護，播完轉入等待。
2. `Waiting`（等待回應中）：循環播放待機，可由放開按鍵隨時取消。
3. `Engaging`（接觸成立，執行主要動作）：如握緊、碰拳、抱住；此區間長度構成核心時間窗。
4. `Sustaining`（持續中，若有開啟 Hold Rule）：雙方或單方維持按住。
5. `Resolving`（結果裁定與反應表現）：播放成功、惡作劇、失敗或勝負動作。
6. `Withdrawing`（收手或脫離，回到站姿）：受不可控制區間保護，結束後清空互動狀態。

### 3. 各階段動畫片段（Clips）
- 每個階段可為發起方與回應方配置對稱或非對稱的 `AnimationClip`。
- 階段的耗時直接以片段長度作為權威時間，維持動畫長度即判定時間的原則。

### 4. IK 對準的位置與姿勢約束（IK & Locomotion Constraints）
- **IK 錨點目標**：
  - `Midpoint`：雙方接觸骨骼的中間點（如握手、碰拳）。
  - `TargetBone`：對準對方的特定骨骼加上固定 Offset（如牽手、摸頭、架住手腕）。
  - `Socket`：完全吸附至對方的掛載點（如背抱、騎頭上）。
- **移動限制（Locomotion Rules）**：
  - `Locked`（完全鎖定移動與轉向）：如握緊、比力氣中、擁抱。
  - `GuardSpeed`（限制為警戒慢步）：如伸手等待走近時。
  - `AsymmetricLocomotion`（非對稱移動）：如抓住與掙脫時，被抓者一般走路速度、抓人者慢步；承載時由下方者完全控制移動。

### 5. 套用的規則模組（Modular Rules）
互動定義依需要勾選以下規則模組，未勾選者不啟動對應邏輯：
- **落空模組（Miss / Prank）**：在主要動作片段播完前，若有人放開互動鍵或脫離接觸，判斷為落空或惡作劇。可設定雙方放開時的特殊反應（如拍手大笑）。
- **握住模組（Hold）**：主要動作成功後不直接退出，只要仍有人按著即維持持續狀態。提供 `ReleaseTolerance`（如 0.3 秒），短暫放開或連按不判定為中斷。
- **比力氣模組（Contest / Mash）**：在 Sustaining 期間記錄玩家與 AI 的按下次數。退出時計算次數差值，依門檻判定大勝、微幅差距、平手。
- **抓住與掙脫模組（Grab & Break-free）**：若一方放開超過容許時間而另一方仍按著，轉入抓住狀態。放開者手腕被 IK 鎖定於抓人者手上；被抓者往後退或起跳彈開，身軀距離超過掙脫門檻（如雙臂伸直距離）時強制判定掙脫。
- **承載模組（Carriage）**：一方物理層級附著於另一方，被承載者的移動輸入被抑制或轉化為掙脫意圖，全由承載者驅動位移。

---

## 優先順序前 8 個互動之表達能力檢驗

依據計畫〈後續發展邊界〉建議之順序，逐一驗證上述通用定義能否完整表達：

| 順序 | 互動名稱 | 新引進的能力驗證 | 定義表達方式與配置檢驗 |
|:---:|:---|:---|:---|
| **1** | **碰拳** (Fist Bump) | 接觸部位變更為拳頭；無握住階段；驗證接觸部位可配置性。 | **可完整表達**。<br>• 接觸部位：`RightFist` 對 `RightFist`。<br>• 規則模組：開啟「落空模組」，關閉「握住模組」。<br>• 流程：伸手 → 接觸 → 碰拳片段 → 直接收手；若時間窗內抽手則觸發落空尷尬。 |
| **2** | **擊掌** (High Five) | 高位接觸；擊中後的短暫抓手時間窗。 | **可完整表達**。<br>• 接觸部位：高位 `RightPalm` 對 `RightPalm`。<br>• 規則模組：開啟「落空」與條件式「抓住模組」。<br>• 特徵：擊中判定成功後，開放極短時間窗（如 0.2 秒），若一方快速改按住可轉入抓住拉扯。 |
| **3** | **猜拳** (RPS) | 無物理碰撞體接觸；雙向同時發起；時間窗判定慢出。 | **可完整表達**。<br>• 接觸部位：`None`（由身軀距離與面向檢核取代觸碰）。<br>• 規則模組：啟動「同步時間窗模組」。<br>• 特徵：雙方意圖在時間窗內抵達時，比對雙方隨機/選定之出拳結果；超時未出者判為慢出落敗。 |
| **4** | **擁抱** (Hug) | 多部位接觸（雙臂對軀幹）；身軀貼近；雙臂環抱 IK。 | **可完整表達**。<br>• 接觸部位：`Chest` 與 `BothArms` 複合 Trigger。<br>• IK 策略：雙手目標分別繞至對方後背骨骼。<br>• 規則模組：開啟「握住模組」（按多久抱多久）與「掙脫模組」。 |
| **5** | **牽手與牽著走** (Hand Holding) | 連接狀態下雙方均允許移動；身軀距離動態中斷。 | **可完整表達**。<br>• 接觸部位：`RightHand` 對 `LeftHand`。<br>• 移動規則：雙方均為 `NormalSpeed`。<br>• 規則模組：開啟「握住模組」，並以身軀距離即時監控掙脫/脫手門檻。 |
| **6** | **拉人起身、攙扶** (Help Up / Support) | 雙方狀態非對等（一方倒地或受傷）；狀態機跨實體影響。 | **可完整表達**。<br>• 前置條件檢查：回應方必須處於 `KnockedDown` 或 `Injured` 狀態。<br>• 接觸部位：一手抓手腕 / 手臂搭肩膀。<br>• 成果回寫：互動成功時將對方的 `CombatEntity` 狀態改寫為站立或恢復行動。 |
| **7** | **背與其他承載** (Piggyback) | 實體階層附著（Mounting）；單方控制位移。 | **可完整表達**。<br>• 規則模組：開啟「承載模組」。<br>• IK 與 Transform：被背者 Transform 父級或 Constraint 附著於背者背部 Socket。<br>• 移動規則：背者承擔移動（速度可受承載修正），被背者移動輸入轉為按鍵掙脫。 |
| **8** | **接住跌倒的人** (Catch Falling Ally) | 非同意式意外反應；單向觸發的反應時間窗。 | **可完整表達**。<br>• 發起模式：`Reactive`（由隊友跌倒事件直接觸發救援時間窗）。<br>• 接觸部位：雙臂截擊隊友軀幹。<br>• 判定：落地前接觸成功轉為抱住/攙扶；超時未接住則隊友著地受創。 |

**檢驗結論**：通用定義模型透過「部位配置」、「階段映射」與「獨立規則模組開關」，能完全涵蓋前 8 項社交互動所需的全部機制，且具備向下延伸至複雜戰鬥與救援的能力。

---

## 戰鬥交手在同一底層上的構建方式

計畫中明確指出：**「戰鬥與雙人互動共用底層，但戰鬥不是雙人互動的一種」**。

### 1. 共同底層的沿用
兩者完全共用相同的技術管線：
```text
意圖 (CombatActionIntent) 
  → 碰撞體接觸與時間窗 (Hitbox / Hurtbox / Trigger & Timing Window) 
  → 仲裁元件裁定正式結果 (CombatArbitrator / Host Authority) 
  → 表現呈現 (Animation, Hit Reactions, Directional IK, Sound/VFX)
```
玩家與 AI 均受相同的角色物理與動作區間（可取消、不可控制、接續）約束。

### 2. 戰鬥交手的對應關係映射
戰鬥行為可直接對應到底層互動概念：
- **攻擊（Attack）**：不需對方同意的單向發起（Unilateral Initiation），在攻擊片段的有效幀（Active Frames）開啟 Hitbox 碰撞偵測。
- **普通防禦（Normal Guard）**：寬時間窗內的回應。只要在被擊中前處於防禦姿態且面向來襲方向，仲裁器裁定為普通格擋（削減傷害、保留進攻節奏）。
- **精準防禦（Parry）**：極窄時間窗內的回應操作（在受擊前數幀內按下並放開）。仲裁器裁定為精準彈開，強制中斷對方的連續進攻狀態（進入硬直）。
- **接拳（Catch Punch）**：攻擊者的拳頭碰撞體與防守者肩膀前方張開的雙手碰撞體接觸，且防守者處於接拳時間窗，裁定為接拳成功，直接銜接抓住與拉扯模組。
- **擒拿與摔投（Grapple / Throw）**：破防後接觸身軀，進入抓住狀態，並在時間窗內發動摔投判定（類似承載與脫離的力學延伸）。

### 3. 戰鬥專屬特徵與分層差異
戰鬥系統在共用底層之上，構建為獨立的特化層，不與社交互動混淆：
1. **非對等與非同意性**：攻擊方無需等待受擊方回應，碰觸即觸發仲裁。
2. **多對一支援（Concurrency）**：一名角色可能同時被多個敵人攻擊。戰鬥仲裁器維護的是「多個並行碰撞檢驗票據（Combat Exchange Tickets）」，而非社交互動的一對一配對。
3. **持續狀態存在（Persistent State）**：社交互動結束時各自身體狀態回到基準；戰鬥交手則會持續累積生命值扣減、體勢值消耗、擊飛力道與倒地狀態，受力因果不可隨動作切換而清除。

---

## 現有握手程式對照與重構邊界

對照開始時 HEAD commit `41f61db34d8978d589e58d7ee3b602dbf0f2864d`（包含第 2 批最新成果 `0d29bfb`）的握手實作檔案，界定未來重構時的資產與程式分工：

### 1. 程式組件對照分析表

| 現有握手檔案 (HEAD) | 現行責任與寫死內容 | 未來通用層重用與抽象化方向 |
|:---|:---|:---|
| `HandshakeState.cs` | • 定義 `HandshakeState` (Reaching, Grip, Shaking...)<br>• 定義 `HandshakeOutcome` (Cancelled, Success, Prank...) | **抽象化為通用模型**：<br>• 提煉為通用階段狀態機（`InteractionPhase`）與通用結果結構（`InteractionOutcome`）。<br>• 握手特有的枚舉保留為具體實作或作為通用結果的標籤。 |
| `HandshakeArbitrator.cs` | • 單例仲裁器<br>• 寫死雙人（`_participantA`, `_participantB`）<br>• 寫死 5 段握手片段與 3 段惡作劇片段<br>• 寫死 Grip 放開追蹤與搖手計時 | **重構為通用仲裁器**：<br>• 重命名或演進為 `InteractionArbitrator`。<br>• 透過 `InteractionDefinition` 載入片段與時間窗，不再將 AnimationClip 序列化在仲裁器本身。<br>• 核心狀態機改由「規則模組」驅動（將惡作劇判定委派給 `PrankRule`，搖手委派給 `ActionDurationRule`）。 |
| `HandshakeParticipant.cs` | • 依賴 `CombatEntity`<br>• 寫死 `0.25f` 碰撞半徑與速度約束<br>• 重新按下門禁（`_requireInteractRelease`）<br>• 寫死按住/放開切換 Reaching/Waiting/Withdrawing | **提煉為通用參與者**：<br>• 提煉為 `InteractionParticipant`。<br>• 保留成熟的 `CombatActionIntent` 監聽、重新按下門禁與 `CombatEntity` 重設對接。<br>• 速度約束（`SpeedOverride`）與狀態轉移條件改由當前執行的 `InteractionDefinition` 參數驅動。 |
| `HandContactBridge.cs` | • 右手 Trigger 偵測<br>• `OnTriggerEnter`/`Stay` 搭配 `Physics.OverlapSphere`<br>• 僅辨識對手的 `HandContactBridge` | **擴充為多部位通用 Bridge**：<br>• 演進為 `BodyContactBridge`，支援指定骨骼標籤（`BoneTag`：LeftHand, RightHand, Torso 等）。<br>• 保留其「Kinematic Rigidbody + Trigger」與「OverlapSphere 保底」的成熟接觸回報機制。 |
| `HandshakeVisualFeedback.cs` | • 監聽狀態切換 Animator 片段<br>• 寫死 5+3 組 State 名稱字串<br>• `OnAnimatorIK` 寫死右手向中點對準<br>• 寫死材質顏色漸層反饋 | **重構為通用視覺表現**：<br>• 演進為 `InteractionVisualFeedback`。<br>• Animator State 名稱由互動定義之對應表查詢，不寫死字串。<br>• IK 目標點依定義之 `IKTargetStrategy` 動態綁定目標骨骼或中點，權重曲線由設定驅動。 |

### 2. 重構過渡路線建議
在第 3 批握手實作完成且通過驗收後，建議採取「提取共用核心，握手作為第一個定義實例」的過渡方式：
1. **第一步（下放通訊契約）**：將 `CombatActionIntent` 與重新按下門禁邏輯確認為不變底層。
2. **第二步（抽象化接觸與仲裁）**：建立 `InteractionArbitrator` 與 `InteractionDefinition` 結構，讓 `HandshakeArbitrator` 繼承或包裝通用仲裁器，先驗證握手能經由資料驅動執行且手感無退化。
3. **第三步（實作第二個互動）**：配置「碰拳（Fist Bump）」定義資產，驗證系統在不改動 C# 仲裁核心的情況下，僅憑部位變更與規則開關即可成功運行。

---

## 互動定義格式分析（尚未決定第 6 項）

依據計畫〈尚未決定〉第 6 項：
> 「互動定義的格式：存成 Unity 的設定資產（Inspector 編輯最方便，只有 Unity 能用），或跨平台資料檔（沿 data-driven 方向，Web 工具也能編輯、其他平台可讀，但 Unity 內要另做編輯介面或改用 Web 工具）。計畫方向是跨平台累積，agent 傾向跨平台資料檔；做通用互動系統前決定。」

本討論稿不代為決定，客觀列出兩種格式各自對系統架構與實作之影響：

### 方案 A：Unity 設定資產（ScriptableObject / `.asset`）
- **架構特徵**：
  - 定義模型以繼承 `ScriptableObject` 的 C# 類別實作（如 `InteractionDefinitionSO`）。
  - 各階段動畫直接參照 `UnityEngine.AnimationClip` 物件。
  - 骨骼部位直接使用 `Transform` 引用或 Unity 的 `AvatarIKGoal` 枚舉。
- **對設計與開發的影響**：
  - **優勢**：
    1. **極佳的 Unity 編輯體驗**：在 Unity Inspector 中能直接拖拉 AnimationClip、曲線與數值，支援 Undo/Redo、Prefab Overrides 與物件驗證。
    2. **零轉換成本**：執行時無需任何 JSON 反序列化或字串尋找，效能最佳且無載入延遲。
    3. **符合第 3 批手工調整需求**：計畫中提到的「在 Play 模式調整參數並保存」，Unity 原生資產具備最直覺的支援。
  - **代價**：
    1. **綁定 Unity 引擎**：檔案為 Unity YAML/二進位，非 Unity 環境（如 Web 動作編輯器、其他後端或獨立模擬工具）無法直接讀取或修改。
    2. **跨平台資產累積斷層**：無法直接融入 `Yu5h1Lib/data-driven` 的跨平台動作資料管線。

### 方案 B：跨平台資料檔（JSON / Schema-driven）
- **架構特徵**：
  - 定義模型以純資料結構（POCO / Schema）定義，儲存為獨立的 `.json` 檔案。
  - 動畫片段以全域識別碼或相對路徑（Clip ID / Relative URI）參照。
  - 部位與規則以字串鍵值（String Key / Enum String）描述。
- **對設計與開發的影響**：
  - **優勢**：
    1. **高度符合長遠累積方向**：與 `Yu5h1Lib/data-driven`（Limb 與 Animation 模組）完全一致，Web 動畫編輯工具可直接生成與編輯互動定義。
    2. **易於版本控制與審查**：JSON 結構在 Git 中具備高可讀性，方便比對差異與進行自動化驗證。
    3. **引擎無關（Engine-agnostic）**：未來若移植至其他渲染端或作為專用伺服器（Dedicated Server）之判定依據，無需依賴 Unity 函式庫。
  - **代價**：
    1. **需要轉譯配對層（Adapter Layer）**：Unity 端必須額外撰寫載入器（Importer / Resolver），負責將 JSON 中的 Clip ID 解析為專案內的 `AnimationClip` 資產，若命名不一致容易失效。
    2. **Unity 內部編輯介面成本**：若要在 Unity Inspector 中提供友善的手工調整介面，需要客製化 Editor 視窗或自訂 PropertyDrawer，增加開發工作量。

### 設計收斂建議（轉譯隔離設計）
無論最終選擇何者，通用互動系統的**執行時核心（Runtime Core）**均應操作純 C# 介面或純資料類別（如 `IInteractionDefinition` / `InteractionDefinitionData`）。
若採方案 A，以 ScriptableObject 實作該介面；若採方案 B，由 JSON Deserializer 產出該類別實例。如此可確保仲裁邏輯與資料儲存格式完全解耦，不因格式決策而影響底層機制之建立。

---

## 決策理由與替代方案

### 1. 為何維持「動作片段長度即時間窗」而非「手動填寫時間秒數」？
- **已選方向**：時間窗直接讀取 `AnimationClip.length`。
- **排除方案**：在設定檔中為每個互動手動指定「握緊時間窗 = 0.6 秒」。
- **理由**：動畫修改後若需手動校正秒數，極易產生畫面與判定脫節；以片段長度為單一事實來源，能確保美術/動作調整時，判定窗自動同步對齊，維持「畫面可讀即判定真相」的原則。

### 2. 為何堅持「IK 不反饋至接觸碰撞」？
- **已選方向**：接觸判定由非 IK 骨骼的原生 Trigger 判定。
- **排除方案**：讓碰撞體掛在受 `OnAnimatorIK` 控制的最終手掌位置上。
- **理由**：IK 是為了解決模型骨骼尺寸差異而產生的視覺修飾；若 IK 影響碰撞，會造成「因為靠得很近產生 IK 吸引，進而誤觸發碰撞」的邏輯死結與非對稱抖動。

---

## 驗收與遷移

通用互動系統實作完成後的驗收準則：
1. **握手回歸一致性**：握手以通用定義呈現後，在相同 WhiteRoom 測試場景中，能完整重現發起、取消、回應、成功、惡作劇、同時收手與比力氣的所有判定與畫面，且與既有第 1～3 批握手實作行為無差異。
2. **碰拳零程式碼擴充**：僅透過建立碰拳定義資產（配置右手拳頭 Trigger 與無握住規則），即可在場景中操作出完整的碰拳與落空反應，無需撰寫或修改任何一行 C# 判定程式。
3. **戰鬥場景隔離保證**：通用互動系統之任何變更，均不得影響 `Combat/WhiteRoom.unity` 既有移動、跳躍、攻防與鏡頭行為。

---

## 尚未決定

1. **互動定義的儲存格式（尚未決定第 6 項）**：待使用者於進入通用系統實作前，在「Unity 原生 ScriptableObject」與「跨平台 JSON 資料檔」之間做出方向決策。
2. **多互動切換之選擇介面 UX**：玩家在遊戲中如何切換目前預設互動（HUD 輪盤、快捷鍵、或走近特定目標時依情境自動推薦），尚未納入首輪實作範圍。
3. **戰鬥擒拿與社交抓住的數值共用幅度**：擒拿掙脫是否消耗體力或耐力，以及與社交比力氣連按門檻之數值天平，留待戰鬥驗證恢復後進一步調校。
