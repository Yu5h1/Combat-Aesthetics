# WhiteRoom：網路方案評估

> 本文件為依據 [WhiteRoom 實作計畫](../../Samples~/WhiteRoom/Implementation-Plan.md)〈並行交辦〉「網路方案評估」第 1～4 項產出之技術評估與候選比較文件。
> 本文件僅進行技術調查與架構分析，不代表戰鬥驗證已恢復，亦不代表已正式選定或安裝任何網路套件。

## 系統位置與關係

```text
CombatAesthetic Package (Unity 6000.6.0f1 / URP)
├─ Samples~/WhiteRoom
│  ├─ Runtime
│  │  ├─ CombatEntity（由 ICombatInputSource 驅動，接收 CombatActionIntent）
│  │  ├─ CombatActionIntent（統一操作意圖結構體：移動、跳躍、防禦、互動等）
│  │  ├─ CombatNetworkMessage（序列號、發送者 ID、時間戳、意圖封包）
│  │  └─ SimulatedNetworkChannel（進程內延遲／抖動／丟包模擬隊列）
│  ├─ Handshake
│  │  ├─ HandshakeArbitrator（主機權威判定元件：裁定碰到、成功、惡作劇、抓住與掙脫）
│  │  └─ HandshakeParticipant（本機操作與表現端，負責採集意圖、本地播放、人形 IK 對準）
│  └─ Combat
│     └─ 戰鬥場景（未來攻防判定，沿用相同意圖與判定底層）
│
└─ Documentation/plans
   ├─ Multiplayer-Action-Network-Interaction-Technology-Discussion.md（既有討論記錄）
   └─ WhiteRoom-Network-Evaluation.md（本評估文件）
        │
        ├── 依據已確認方向：Host/Listen Server、主機裁定正式結果、本機立即回應、有上限延遲補償、LAN 直連
        ├── 候選方案評估：FishNet、Netcode for GameObjects (NGO)、Mirror、LiteNetLib
        ├── Yu5h1Lib 既有能力盤點與銜接架構（CombatActionIntent、SimulatedNetworkChannel 等）
        ├── 最小連線驗證案例矩陣（對應連線基線案例）
        └── 待決策項目選項（延遲補償上限、抖動容限、修正平滑、攻守公平性）
```

---

## 一、既有討論承接與過時標記

在評估新方案前，先檢視既有技術筆記 [多人動作遊戲網路互動技術討論](Multiplayer-Action-Network-Interaction-Technology-Discussion.md) 的結論：

### 1. 依然成立的結論

* **三層架構分離**：
  1. 遊戲客戶端與模擬（Unity 負責畫面、輸入、角色、動畫、物理、IK 與本地邏輯）。
  2. 網路同步技術（封包處理、Frame/Tick 同步、輸入傳送、本機預測、狀態校正與歷史回溯查核）。
  3. 網路基礎設施（Lobby、Relay、Dedicated Server、Matchmaking）。
  三層各自獨立，不可因選定某種傳輸方式而將遊戲邏輯與特定網路 SDK 深度綁定。
* **UDP 是即時動作遊戲的核心**：
  TCP/WebSocket 的 Head-of-line blocking（循序重傳阻塞）對 60fps 動作遊戲具有致命影響；高頻、時效性敏感的操作意圖與狀態必須運行在 UDP 基礎之上。
* **Host/Listen Server 與 Dedicated Server 的定位差異**：
  Host 模式開房快、零伺服器成本，非常適合作為內部原型與前期驗證；Dedicated Server 具備最佳防作弊與裁定公平性，適合未來商業營運。判定邏輯與呈現端徹底分離，即可在未來平滑將判定端遷移至 Dedicated Server。
* **介面隔離（Adapter）原則**：
  不可將第三方網路 API 散布於 `CombatEntity` 或 `HandshakeArbitrator` 中，必須透過抽象層（如 `ICombatInputSource` 或 Transport Adapter）傳遞 `CombatActionIntent`。

### 2. 已過時或本輪調整的部分

* **調整驗證起點（外網 Tunnel 延後，優先 LAN 直連）**：
  舊討論將「以 SignalR / Cloudflare Quick Tunnel 進行外網概念驗證」列為第一步。然而，WhiteRoom 的最新實作目標是**檢驗真實 3D 角色動作、雙人接觸時間窗與攻防判定**。因此，連線路線已明確調整為「**第 1 階段聚焦於同機雙執行個體（127.0.0.1）與區域網路（LAN Direct Connect UDP）**」，先排除 NAT 穿透與外網中繼干擾，專注於物理接觸、時間戳判定與延遲補償。
* **大幅簡化雙人互動的同步複雜度（非全狀態 Rollback）**：
  舊討論花費大量篇幅探討格鬥遊戲高成本的「全狀態 60fps 決定論回滾（Full Rollback）」。
  依使用者 2026-10-31 明確確認的方向：**雙人互動的網路傳輸量非常小**。它只傳輸移動方向、按住與放開（狀態＋序號）、連按次數與主機裁定的正式結果；動作片段與人形 IK 全部由各端本機計算，不上網路。
  因此，不需要導入龐大複雜的決定論物理/全狀態重播系統，只需採用「**客戶端帶時間戳的意圖傳送＋主機端有上限的歷史時間查核（Lag Compensation）＋客戶端平滑修正**」即可精準達成驗收要求。

---

## 二、〈連線驗證路線〉已確認的規格基線

依據 [WhiteRoom 實作計畫](../../Samples~/WhiteRoom/Implementation-Plan.md)〈連線驗證路線〉，本輪評估方案必須嚴格滿足下列規範：

1. **架構模式**：
   * 採**玩家開房兼任主機（Host / Listen Server）**模式，其他客戶端直接連入。
   * 主機與客戶端、AI 在同一套規則下運行，主機玩家並不享有特權繞過合法性檢查。
2. **正式結果裁定權**：
   * **主機裁定所有正式結果**（命中、防禦成立、受擊、硬直、握手碰到、握緊成功、惡作劇成立、勝負、抓住與掙脫）。
   * 客戶端不自行認定正式結果，僅呈現本機預測與接收主機裁定。
3. **本機立即回應與結果修正**：
   * 本機移動與按鍵操作立即在畫面呈現（本地預測），不等待網路 RTT。
   * 當主機裁定結果（或遠端位置）抵達時，若與本機預測有偏差，本機必須具備平滑接回（Smoothing / Interpolation）能力，不可突兀瞬移。
4. **有上限的延遲補償（Lag Compensation）**：
   * 遠端操作抵達主機時，主機依封包夾帶的採集時間戳，在有限範圍內回溯查核歷史狀態（如判定防禦時機或握手放開時機）。
   * 設有補償上限，不盲目信任遠端時間宣告，維持攻守公平性。
5. **區域網路直連與主機顯示**：
   * 主機開房後顯示 LAN IP 與 Port；客戶端直接輸入 IP 與 Port 加入。
   * 首輪支援同機雙開（127.0.0.1）與跨電腦 LAN 直連，不依賴外部 Lobby/Matchmaking 伺服器。
6. **生命週期與主機中斷處理**：
   * 主機中斷或離開時，直接結束本場測試，客戶端顯示連線中斷原因，返回主選單；重新開房為全新場次，舊連線殘留訊息不得污染新場次。
7. **首輪驗證規模**：
   * 最多 **2 名玩家與 2 名 AI**（涵蓋 1v1 對戰、2 人合作抗敵、2v2 多方威脅）。
8. **核心聚焦：雙人互動的時機**：
   * **握緊時間窗內放開**：以玩家按下／放開**當下的時間戳**判定，不能以封包抵達主機的延遲時間判定。
   * **網路抖動（Jitter）防護**：連按時的短暫放開容許時間（暫定 0.3 秒），不能因網路抖動拆成獨立封包而誤判為「放開超過時間」，避免誤結束握住或誤進入抓住。
   * **碰觸與掙脫距離**：碰觸（手部碰撞體）與掙脫（身體距離）以主機判定為準，本機若有畫面差異，需透過平滑內插拉回。
   * **附著關係一致性**：抓住、承載或牽著走時的兩者階層／附著關係，兩端必須一致。

---

## 三、Yu5h1Lib 既有相關能力盤點

盤點當前儲存庫中與網路、輸入及判定相關的既有能力：

| 模組／檔案 | 既有能力與實作狀態 | 適用邊界與評估 | 缺口與需擴充項目 |
|---|---|---|---|
| `CombatActionIntent.cs`<br>([連結](../../Samples~/WhiteRoom/Runtime/CombatActionIntent.cs)) | 統一的意圖結構體：含 `MoveInput`、`Jump`、`Attack`、`GuardHold`、`ParryTap`、`InteractHold`、`InteractDown`、`InteractUp`、`Timestamp` 等。具備 `ICombatInputSource` 介面。 | **完全相容**。已達成硬體輸入、AI 與本機模擬的徹底解耦，天然適合序列化後在網路上傳輸。 | 需加上網路序列化方法（如 Unity 的 `INetworkSerializable` 或二進位讀寫器）。 |
| `WhiteRoomNetwork.cs`<br>([連結](../../Samples~/WhiteRoom/Runtime/WhiteRoomNetwork.cs)) | 1. `CombatNetworkMessage`：包含 SequenceId、SenderId、Timestamp、CombatActionIntent。<br>2. `SimulatedNetworkChannel`：記憶體內模擬隊列，支援 LatencyMs、JitterMs、PacketLossRate 與時間戳排程遞送。 | **部分相容**。已在進程內成功驗證了帶時間戳的序列封包處理與弱網模擬邏輯。 | 僅為進程內記憶體模擬，缺乏跨進程與跨機器的真實 Socket / UDP Transport。 |
| `HandshakeArbitrator.cs`<br>([連結](../../Samples~/WhiteRoom/Handshake/HandshakeArbitrator.cs)) | 單例判定元件，由碰撞 Trigger 觸發，仲裁握手各階段（碰觸、握緊成功、惡作劇、握住、比力氣、抓住與掙脫）。輸出清晰的狀態與結果事件。 | **完全相容於主機端**。判定責任與動畫／IK 呈現已完全分開，只讀取意圖與碰撞，天然適合在 Host 端執行裁定。 | 目前直接呼叫本地單元，需改為接收網路廣播指令；客戶端需只呈現狀態，不重複執行仲裁。 |
| `CombatEntity.cs`<br>([連結](../../Samples~/WhiteRoom/Runtime/CombatEntity.cs)) | 提供 `StepCombat(CombatActionIntent, Transform)`，支援移動、跳躍、防禦、受擊與狀態切換。 | **高度相容**。每一 Frame/Tick 接收一份意圖並推進狀態，與 Client-side prediction 結構高度契合。 | 缺乏狀態快照存取（用於狀態校正與回溯）與平滑位移內插器。 |
| `RemotePointer.Networking`<br>(`WPF/Remote Pointer`) | 基於 SignalR / WebSocket 與 Cloudflare Quick Tunnel 實現雙向指令傳輸與 Ping 監控。 | **僅供架構參考**。驗證了 HTTP/WebSocket 與雲端穿透，但不適用於動作遊戲 60fps UDP 戰鬥。 | 無法用於低延遲動作遊戲，戰鬥必須走 UDP。 |

---

## 四、Unity `6000.6.0f1` 可用連線方案深度對比

針對 Unity 6 平台，挑選四個具代表性且相容的網路方案進行對比評估：

### 1. Fish-Networking (FishNet)
* **定位**：專為現代 Unity 開發設計之高效能開源 Netcode（MIT 授權），在動作與預測領域備受推崇。
* **主機權威**：原生嚴格支援 Server / Host 權威架構，明確劃分 Server 與 Client 權限。
* **輸入與時間戳**：
  * 原生支援 Tick 驅動系統與精確的 `NetworkTime`。
  * 提供 `[Replicate]` 機制：客戶端每 Tick 將自訂輸入結構打包並發送給伺服器，結構內可直接包裹 `CombatActionIntent`。
* **延遲補償與回溯判定**：
  * **內建強大的 `RollbackManager`**：伺服器收到帶有遠端時間戳的動作時，可自動將指定物件的 Collider 回溯至當時的物理變換（Transform），檢驗碰觸／命中後再復原。與 WhiteRoom 的時間窗判定極度契合。
* **本機預測與修正**：
  * 原生支援 `[Reconcile]` 機制：客戶端本地預先執行移動與出招；當伺服器回傳權威狀態時，若發生預測分歧，FishNet 會自動將角色重設回伺服器狀態，並在該幀重播未確認的本機輸入，平滑接回正確位置。
* **網路條件模擬**：底層 Tugboat Transport 內建延遲、抖動與封包遺失模擬。
* **區域網路直連**：原生純 UDP 直連，直接輸入 IP 與 Port 即可開房／加入。
* **授權與維護狀況**：核心開源（MIT License），社群極度活躍，完全相容 Unity 2021～Unity 6。
* **與現有架構銜接**：
  * `CombatActionIntent` 可直接宣告為 `[Replicate]` 的自訂輸入資料結構。
  * `CombatEntity` 的移動邏輯移至 `[Replicate]` 方法中執行，自動獲得客戶端預測與伺服器回滾重播。
  * `HandshakeArbitrator` 掛載為 NetworkBehaviour，在 Host 端執行裁定，透過 ObserversRpc 廣播正式結果。

### 2. Netcode for GameObjects (NGO) + Unity Transport (UTP)
* **定位**：Unity 官方在 Unity 6 主推的第一方 GameObject 連線框架（`com.unity.netcode.gameobjects` v2.x）。
* **主機權威**：標準 Server / Host 權威架構，內建 `NetworkManager`、`NetworkBehaviour`、`NetworkVariable` 與 RPC。
* **輸入與時間戳**：
  * 具備 `NetworkTickSystem` 與 `NetworkTimeSystem`，支援以固定 Tick 處理網路訊息。
  * 支援將自訂結構（實作 `INetworkSerializable`）透過 ServerRpc 由客戶端送往 Host。
* **延遲補償與回溯判定**：
  * **無開箱即用的 Collider Rollback**：NGO 本身不內建專屬的物理延遲補償回溯器（該能力在 DOTS Netcode for Entities 較完整）。若在 NGO 中進行時間回溯，需手動撰寫歷史環形緩衝區（RingBuffer）儲存過去 Transform，並在判定時手動回滾。
  * 但針對雙人互動（如握手放開時機、連按計數），因只涉及時間戳比較與純數值計算，不涉及複雜物理碰撞回溯，NGO 完全足以勝任。
* **本機預測與修正**：
  * NGO 2.x 提供 `NetworkTransform` 的本地預測（ClientNetworkTransform）與內插平滑，但通用狀態預測與 Reconciliation 需自行撰寫或使用官方實驗性預測擴充。
* **網路條件模擬**：搭配官方 `com.unity.multiplayer.tools` 的 Network Simulator，可精確模擬 Latency, Jitter, Loss。
* **區域網路直連**：Unity Transport 原生支援純 UDP 直連，輸入 IP 與 Port 即可。
* **授權與維護狀況**：Unity 官方核心套件（Unity Companion License），長期維護與 Unity 6 API 緊密同步。
* **與現有架構銜接**：
  * `CombatActionIntent` 實作 `INetworkSerializable`，透過每 Tick 的 ServerRpc 上傳給 Host。
  * `HandshakeArbitrator` 僅在 Host 端運行，透過 ClientRpc 同步結果。

### 3. Mirror Networking
* **定位**：Unity 開源社群歷史最悠久、最成熟的 High-level 網路函式庫（MIT 授權）。
* **主機權威**：以 UNet 為基礎重構之標準 Host/Server 權威。
* **輸入與時間戳**：具備 `NetworkTime`，透過 `[Command]` 上傳輸入。
* **延遲補償與回溯判定**：內建 Snapshot Interpolation（快照內插平滑），但無官方內建之通用 Collider 回溯工具，需依賴第三方外掛或手寫。
* **本機預測與修正**：內建預測能力較弱，多數依靠伺服器權威位置同步搭配客戶端內插平滑（Client-side interpolation）。若要實現嚴格的 Client-side prediction，需自行寫預測與校正代碼。
* **網路條件模擬**：預設 KCP Transport 支援模擬延遲與丟包。
* **區域網路直連**：原生純 UDP 直連，開箱即用。
* **授權與維護狀況**：MIT 授權，社群維護積極，相容 Unity 6。
* **與現有架構銜接**：成熟但缺乏現代動作遊戲急需的「預測／校正（Prediction/Reconciliation）」原生語法糖，需額外撰寫較多膠水代碼。

### 4. LiteNetLib（純輕量 UDP 傳輸層）
* **定位**：純 C# 撰寫的高效能 UDP 通訊庫（MIT 授權，零 GC 垃圾回收）。
* **主機權威**：純傳輸層，無預設主機權威概念，所有權威與狀態邏輯需 100% 自行建構。
* **輸入與時間戳**：提供可靠／非可靠有序通道，封包帶時間戳需手動序列化。
* **延遲補償與本機預測**：完全沒有。所有 Tick 同步、快照快取、內插平滑、預測與校正都必須從零寫起。
* **網路條件模擬**：內建 `SimulateLatency` 與 `SimulatePacketLoss`。
* **區域網路直連**：原生 UDP Socket 直連。
* **授權與維護狀況**：MIT 授權，穩定可靠。
* **與現有架構銜接**：雖然最自由且無 Unity 框架負擔，但開發者必須自行打造整套 RPC、物件同步與預測系統，對當前 WhiteRoom 驗證而言成本過高。

---

### 方案綜合對比矩陣

| 評估維度 | FishNet | Netcode for GameObjects (NGO) | Mirror | LiteNetLib |
|---|---|---|---|---|
| **Unity 6 相容性** | 高（社群相容 Unity 6） | **原生最高（Unity 官方第一方）** | 高（社群相容 Unity 6） | 高（純 C# / .NET） |
| **開源授權** | MIT | Unity Companion License | MIT | MIT |
| **主機權威架構** | 完善 | 完善 | 完善 | 需手寫 |
| **動作遊戲預測 (Prediction)** | **原生內建 (`[Replicate]`)** | 需部分手寫或搭配擴充 | 需手寫 | 需手寫 |
| **狀態校正 (Reconciliation)** | **原生內建 (`[Reconcile]`)** | 需手寫 | 需手寫 | 需手寫 |
| **延遲回溯 (Lag Compensation)** | **內建 `RollbackManager`** | 數值判定易，Collider 回溯需手寫 | 需手寫 | 需手寫 |
| **雙人互動（握手）適配度** | **極高**（天然對齊 Intent 與時間戳） | **高**（時間戳 RPC + 狀態同步） | 中 | 低（需寫整套同步） |
| **外部依賴性** | 第三方 Git/UPM 套件 | **零外部依賴（官方 Package）** | 第三方 Git/UPM 套件 | 純 DLL/原始碼 |
| **弱網模擬工具** | Transport 內建 | 官方 Network Simulator | Transport 內建 | 內建 Socket 模擬 |
| **開箱即用與開發成本** | **最低（動作機制最齊全）** | 中等（需補足預測與回溯細節） | 中高 | 最高（全手寫） |

---

## 五、方案推薦與深入理由

### 推薦首選：**Fish-Networking (FishNet)**

#### 推薦理由：
1. **與 WhiteRoom「動作意圖（`CombatActionIntent`）」天然契合**：
   WhiteRoom 的核心設計為每一格採集意圖送入 `StepCombat`。FishNet 的 `[Replicate]` 語法允許每一 Tick 直接將該結構當作輸入參數，在客戶端進行無延遲預測移動，並同時打包帶時間戳上傳給 Host；若主機裁定有偏差，FishNet 的 `[Reconcile]` 自動處理回滾校正與平滑重播，幾乎完全不用重寫角色移動狀態機。
2. **內建開源的 `RollbackManager`**：
   動作對戰與雙人互動中，最棘手的是「遠端客戶端按下了防禦或抽手，但訊息到達 Host 時已經過期」。FishNet 內建了時間戳回溯機制，能將物理 Collider 回退至客戶端輸入當下的時間進行檢測，完美契合「握緊時間窗內放開」、「精準防禦抓時機」的判定要求。
3. **完全開源與跨平台自足**：
   MIT 授權，不綁定任何商業雲端或付費 CCU 限制，純 UDP 直連極度輕快，非常適合本機雙開與 LAN 驗證。

### 次選備案：**Netcode for GameObjects (NGO)**

#### 推薦理由：
1. **Unity 6 官方第一方保證**：
   由 Unity 官方直接維護，能無縫整合 Unity 6 的 Profiler、Network Simulator 以及未來的引擎更新，免去第三方套件維護脫節的風險。
2. **雙人互動的特性降低了手寫回溯的難度**：
   若考量到盡量不引入外部第三方框架，由於 WhiteRoom 首輪的「雙人互動（握手）」判定核心主要是**狀態時間戳比較**（放開時間戳是否在握緊片段結束前、放開間隔是否大於 0.3 秒）與**身體 Transform 距離判定**，而非高頻物理射線檢測，因此在 NGO 中由 Host 端維持一個微型歷史時間戳緩衝區，即可在不依賴複雜物理回溯引擎的前提下完成驗證。

---

## 六、對應連線基線案例的最小驗證案例矩陣

依據 [WhiteRoom 實作計畫](../../Samples~/WhiteRoom/Implementation-Plan.md) 的 8 項連線基線案例，規劃最小驗證案例清單：

| 基線案例 | 最小驗證設計與步驟 | 輸入／觀察證據方式 | 判定成功標準 |
|---|---|---|---|
| **1. 開房與直接加入** | 1. 端 A 點選「Host 開房」，HUD 顯示 LAN IP 與 Port（例如 `127.0.0.1:7777`）。<br>2. 端 B 於輸入框填入 IP/Port 點選「Join 加入」。<br>3. 分別在同機雙開與兩台 LAN 電腦執行。 | HUD 顯示連線狀態與連線 RTT；記錄雙端 Log 之連線成功事件。 | 雙端進入同一白色測試場，兩端各有獨立鏡頭與被授權角色；連線失敗時顯示清楚錯誤訊息。 |
| **2. 控制與共享移動** | 1. 端 A 操作 P1 移動與跳躍，觀察端 B 畫面。<br>2. 端 B 操作 P2 移動與跳躍，觀察端 A 畫面。 | 匯出兩端位置座標日誌，比對每 0.5 秒的位置偏差。 | 每端僅能操控自己角色；遠端角色移動順暢無抖動，位置偏差在可接受閾值內，跳躍落地動作一致。 |
| **3. 雙人互動的時機**<br>（核心指標） | 1. 雙方靠近，P1 按住 E 伸手，P2 走近按住 E 伸手，右手接觸。<br>2. 握緊期間，P2 在時間窗內放開 E。<br>3. 握住期間，P1 連按 E，測試抖動條件下 0.3 秒容許窗口。<br>4. 抓住期間，被抓方往後拉開身體距離。 | HUD 顯示「判定狀態」、「雙方放開時間戳」、「連按次數」；截取雙端事件日誌。 | 1. 放開判定以按鍵當下之時間戳為準，非到達主機時刻。<br>2. 連按抖動不被誤判為斷開。<br>3. 碰觸與掙脫由主機裁定，本機平滑拉回，雙方結果完全一致。 |
| **4. 攻防一致性** | 1. P1 發起攻擊，P2 執行普通防禦或精準防禦。<br>2. 雙方同時發起攻擊。<br>3. 觀察命中、格擋成功或受擊中斷。 | 比對雙端 `OnHealthChanged` 與 `OnStateChanged` 事件序列與時間戳。 | 同一出招不重複計傷；格擋或受擊結果在兩端完全同步，無單邊判定幽靈受擊。 |
| **5. 玩家／AI 共用規則** | 1. Host 場上配置 1 名玩家與 1 名 AI（或 2 玩家 2 AI）。<br>2. AI 依內部決策發送 `CombatActionIntent`。 | 檢查 AI 與玩家呼叫的判定元件方法是否完全一致。 | AI 與玩家均遵守相同動作區間、硬直限制與碰觸判定，Host 端無特權後門。 |
| **6. 情境切換與全場重設** | 1. Host 點選「全場快速重設」或切換主題。<br>2. 檢查遠端客戶端狀態。 | 檢查雙端所有角色位置、血量、互動計時器與訊息隊列。 | 兩端角色瞬間回到初始位置與姿態；清除所有殘留封包與未處理意圖，舊資料不污染新場次。 |
| **7. 主機中斷與重新開房** | 1. 進行中強制關閉 Host 端或拔除網路線。<br>2. 客戶端觀察反應後點選返回。<br>3. Host 重啟新房，客戶端重新加入。 | 客戶端 HUD 顯示之斷線提示與 Log 記錄。 | 客戶端正確捕獲中斷，顯示明確原因而非崩潰卡死；新開場次無舊連線殘留。 |
| **8. 不良網路與修正** | 1. 開啟弱網模擬：延遲 100ms、抖動 30ms、丟包 5%。<br>2. 重新測試「握緊時間窗內放開」與「精準防禦時機」。 | 比對發送端時間戳、主機收到時間、回溯查核時間與最終修正記錄。 | 在設定之補償上限內，依時間戳正確判定成功／失敗；偏差時客戶端以平滑內插拉回，無突兀瞬移。 |

---

## 七、〈尚未決定〉第 4 項數值與政策選項清單

依據實作計畫，連線判定與同步細節不應由 Agent 擅自預設或代答，以下列出需由使用者裁決之決策項目與各選項分析：

### 決策 A：延遲補償（Lag Compensation）歷史查核時間上限

* **情境**：遠端玩家因網路延遲，其操作封包較晚到達主機。主機查核過去歷史時，最多願意回溯多久？
* **選項與影響**：
  * **選項 1（嚴格短窗：80ms ～ 100ms）**：
    * *優點*：攻守雙方體感高度即時，不會出現「明明已經躲開了卻在 0.2 秒後被抓到」的嚴重延遲感。
    * *缺點*：在 LAN 以外的稍高延遲環境（或網路偶發抖動），遠端玩家的精準防禦或抽手極易被判為超限作廢。
  * **選項 2（標準折衷：150ms ～ 200ms，建議起點）**：
    * *優點*：能良好吸收區域網路乃至一般家用寬頻的延遲與抖動，雙人互動手感寬容度適中。
    * *缺點*：極端抖動時，畫面稍微有一小段時間差需靠平滑拉回。
  * **選項 3（寬鬆長窗：250ms ～ 300ms）**：
    * *優點*：高延遲客戶端依然能成功觸發時機判定。
    * *缺點*：低延遲玩家（尤其是 Host 本機）會明顯感受到「被過去的幽靈抓到」，嚴重損害防守公平性。

### 決策 B：抖動過濾與連按容許窗口（當前暫定 0.3 秒）

* **情境**：在握住比力氣或維持握住時，玩家連按按鍵。兩次按下之間的短暫放開，若因網路抖動導致兩包間距忽長忽短，該如何對待？
* **選項與影響**：
  * **選項 1（固定容許時間 0.3 秒）**：
    * *優點*：規則單純固定，程式邏輯清晰，與單機體驗完全一致。
    * *缺點*：若遭遇較大網路抖動（Jitter > 100ms），部分連按可能被累計延遲拉長超過 0.3 秒而被誤判為放開。
  * **選項 2（動態容許時間：`0.3s + RTT 抖動餘裕`）**：
    * *優點*：根據當前網路量測的 Jitter 動態微調容許值，抗弱網能力極強。
    * *缺點*：高延遲下玩家會覺得放開反應變遲鈍，且需持續維護可靠的 RTT 統計。

### 決策 C：預測分歧時的客戶端位置修正政策

* **情境**：當客戶端本地預測移動的位置，與主機裁定的權威位置不符（例如被對方卡位阻擋或掙脫判定成立）時，畫面如何校正？
* **選項與影響**：
  * **選項 1（瞬移修正 Snap）**：
    * *優點*：邏輯最簡單，絕對保證畫面與伺服器邏輯瞬間對齊。
    * *缺點*：畫面有肉眼可見的頓挫跳躍，極度破壞視覺體驗。
  * **選項 2（指數平滑內插 Exponential Smoothing / Lerp，建議起點）**：
    * *優點*：在數個幀內平滑過渡至伺服器權威位置，無跳動感。
    * *缺點*：在修正期間角色與真實碰撞體存在微小像素級偏差。

### 決策 D：攻守雙方時間戳分歧裁定政策（公平性傾向）

* **情境**：A 宣告於 `t=1.00s` 出手抓人，B 宣告於 `t=1.02s` 抽手閃避。主機收到兩包時已是 `t=1.10s`。
* **選項與影響**：
  * **選項 1（傾向防守方 Favor Defender，動作／格鬥常見）**：
    * *說明*：只要防守方在其本地畫面的時間窗內成功做出防禦或抽手操作，判定防禦／閃避成功。給予防守者操作回饋最大的安全感。
  * **選項 2（傾向進攻方 Favor Attacker，射擊遊戲常見）**：
    * *說明*：只要進攻方出招時判定範圍內有目標即成立，防守方可能會有「明明按了防禦卻破防」的負面感受。
  * **選項 3（主機絕對時間 Server Authoritative Window）**：
    * *說明*：以主機當下狀態推進，僅允許極小時間戳修正。對 Host 最公平，但對 Client 延遲懲罰最大。

---

## 八、參考資料與引用文件

* 內部計畫與規範：
  * [WhiteRoom 實作計畫](../../Samples~/WhiteRoom/Implementation-Plan.md)
  * [既有能力與重用依據](../../Samples~/WhiteRoom/Existing-Capabilities.md)
  * [多人動作遊戲網路互動技術討論](Multiplayer-Action-Network-Interaction-Technology-Discussion.md)
* 官方與開源網路技術文件：
  * [Unity Netcode for GameObjects (NGO) 官方文件](https://docs.unity.com/en-us/packages/com.unity.netcode.gameobjects/current)
  * [Unity Transport (UTP) 說明](https://docs.unity.com/en-us/packages/com.unity.transport/current)
  * [Unity Network Simulator 工具文件](https://docs.unity3d.com/Packages/com.unity.multiplayer.tools@2.2/manual/network-simulator.html)
  * [Unity Dealing with Latency 指南](https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects@2.4/manual/learn/dealing-with-latency.html)
  * [Fish-Networking 官方文件與 Prediction 架構](https://fish-networking.gitbook.io/docs/)
  * [Mirror Networking 官方文件](https://mirror-networking.gitbook.io/docs/)
  * [LiteNetLib 開源專案](https://github.com/RevenantX/LiteNetLib)
