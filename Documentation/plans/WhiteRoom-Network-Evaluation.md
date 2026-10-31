# WhiteRoom：網路方案評估

> 依 [WhiteRoom 實作計畫](../../Samples~/WhiteRoom/Implementation-Plan.md)〈並行交辦〉「網路方案評估」第 1～4 項撰寫。本文比較候選方案並提出推薦，供使用者選定；不代表已選定或安裝套件，也不代表戰鬥驗證恢復。
>
> 程式現況以 commit `ed45fe9` 為準；之後第 3 批「比力氣」修改的握手程式不在本文範圍。外部資料於 2026-10-31 查證，出處列在各節與〈參考資料〉；查不到出處的寫明「未查證」。

## 位置與關係

```text
WhiteRoom（Combat Aesthetics Sample）
├─ 操作來源：玩家輸入、AI、（之後）遠端玩家
│    └─ 都產生 CombatActionIntent → CombatEntity.StepCombat
├─ 判定：HandshakeArbitrator（握手）；之後的戰鬥判定沿用同一底層
│    └─ 正式結果 → 參與者狀態 → 動畫片段與 IK（各端本機計算）
└─ 連線層（尚未選定，本文評估）
     ├─ 傳輸：同機 127.0.0.1／LAN 直接輸入位址與連接埠
     ├─ 共同時間：讓主機能比較各端的操作時間
     ├─ 操作上傳：意圖＋序號＋採集時間
     ├─ 正式結果與狀態下傳：位置、狀態、判定結果
     └─ 網路條件模擬：延遲、抖動、丟包
```

連線層只負責傳送與時間；判定規則仍在 WhiteRoom 的判定元件，畫面表演接收判定結果。

## 已確認方向對連線方案的要求

方向的原文與日期以計畫〈連線驗證路線〉為準，這裡只列每項方向要求連線方案提供什麼。

| 已確認方向 | 對連線方案的要求 |
|---|---|
| 玩家開房兼任主機（Host／Listen Server） | 支援主機同時是伺服器與玩家；主機自己的操作也走同一套合法性規則。 |
| 主機裁定正式結果 | 判定元件只在主機執行；客戶端只送意圖、收結果。日後判定端可移到專用伺服器。 |
| 本機立即回應，再依主機結果修正 | 客戶端能先在本機推進，再接回主機的正式狀態；修正方法與可接受的差異仍待驗證（〈尚未決定〉第 4 項）。 |
| 有上限的延遲補償 | 主機能依操作的採集時間，在上限內查核過去的狀態；不能把客戶端宣稱的時間直接當事實。上限留待 Demo 實測。 |
| 直接輸入位址與連接埠 | 不經大廳或中繼即可連線；主機顯示 LAN IP 與連接埠。 |
| 主機斷線就結束本場 | 客戶端能分辨「主機主動離開」與「失去連線」；無法判明時顯示連線中斷，不把逾時當成主機離開。新場次不接收舊場次的訊息。 |
| 首輪最多 2 名玩家加 2 名 AI | 4 個角色的同步量；AI 在主機上經共同規則處理。 |
| 開房者統一切換情境與全場重設 | 主機能廣播情境與重設，並讓舊情境的延遲訊息失效。 |
| 雙人互動的時機 | 握緊時間窗內的放開以按鍵當下的時間判定，不以訊息到達主機的時間；抖動不能讓按住的鍵被誤判為放開超過容許時間；碰到與掙脫的距離以主機為準；承載與牽著走的附著關係兩端一致。 |

雙人互動的網路量已確認很小：只傳移動方向、按住與放開（狀態加序號）、連按與正式結果；動作片段與 IK 不上網路。

## 既有討論的承接

先讀了 [多人動作遊戲網路互動技術討論](Multiplayer-Action-Network-Interaction-Technology-Discussion.md)。

**仍成立的結論**

- 遊戲模擬、網路同步技術、網路基礎設施三層分開；遊戲邏輯不直接散布供應商 API，經少數接口（例如原文的 `IGameTransport`）隔離。
- 高頻、時效性高的戰鬥輸入適合 UDP；TCP／WebSocket 在丟包時會等重傳，可能造成短暫停頓。房間、通知等低頻訊息不受此限。
- Client-hosted 的限制：主機可能有優勢、主機離線要處理、防作弊較弱。這和計畫的取捨一致：首輪接受這些限制，保留日後移到專用伺服器的空間。
- Rollback 的成本主要在工程複雜度；要重播的模擬必須可重現。

**已過時的部分**

- 「房間與外網概念驗證」以 SignalR／Cloudflare Quick Tunnel 為第一步：計畫已改為先同機雙執行個體、再區域網路直連；外網連線建立（中繼、穿透、專用伺服器）留到網際網路階段。
- 「小型可玩原型」先用 Unity Lobby＋Relay：首輪改為直接輸入位址與連接埠，不經大廳或中繼。
- 「對戰先採簡單 Input Delay，再逐步評估 Rollback」：計畫已確認本機立即回應再修正，並採有上限的延遲補償；首輪不要求完整回滾系統，但預測、遠端插值、結果修正與延遲補償都要納入第一階段的設計與實驗，不能只用輸入延遲。

## Yu5h1Lib 既有能力盤點

| 能力 | 現況（commit `ed45fe9`） | 接上網路時的缺口 |
|---|---|---|
| [`CombatActionIntent`](../../Samples~/WhiteRoom/Runtime/CombatActionIntent.cs) 與 `ICombatInputSource` | 玩家（`WhiteRoomCharacter`）、AI（`HandshakeAiController`、`WhiteRoomDummy`）都產生同一種意圖，再呼叫 `CombatEntity.StepCombat`。欄位含 `MoveInput`、`Jump`、`Attack`、`GuardHold`、`ParryTap`、`InteractHold`、`InteractDown`、`InteractUp`、`GlobalReset`、`Timestamp`。 | `Timestamp` 是各程序自己的 `Time.time`，兩端不可比較，需要共同時間（例如網路 tick）。`InteractDown`／`InteractUp` 是單格的邊緣旗標，封包遺失就會漏掉；計畫要求的「狀態加序號」還沒有。沒有序列化。 |
| [`WhiteRoomNetwork.cs`](../../Samples~/WhiteRoom/Runtime/WhiteRoomNetwork.cs)：`CombatNetworkMessage`、`SimulatedNetworkChannel` | 程序內的佇列，可設延遲、抖動、丟包；訊息有序號、送出者與時間。只被 `WhiteRoomHUD` 的「發送心跳封包」按鈕使用，沒有接到戰鬥或握手，也沒有測試。 | 不能跨程序。訊息時間是送出當下的 `Time.time`，不是意圖的採集時間；同一格到期的訊息以倒序遞送。計畫明定不把手動心跳佇列當成戰鬥同步，所以它只能當作模擬參數與紀錄格式的參考。 |
| [`HandshakeArbitrator`](../../Samples~/WhiteRoom/Handshake/HandshakeArbitrator.cs) | 單一實例的判定元件；碰到由兩隻右手碰撞體的 trigger 事件決定（`HandContactBridge.OnTriggerContact`）；判定成功、惡作劇、同時收手、取消。比力氣、抓住與掙脫是第 3 批，尚未提交。 | 每格讀參與者目前的 `InteractHold`，並用 `Time.deltaTime` 倒數握緊時間：放開的時間等於「這格才看到」的時間。放到主機上執行時，遠端放開會以訊息到達的時間判定，正好違反「雙人互動的時機」。需要改成依採集時間（共同時間）判定。它也直接呼叫參與者的 `SetState`，客戶端要改為接收正式結果，不自己判定。 |
| [`CombatEntity`](../../Samples~/WhiteRoom/Runtime/CombatEntity.cs) | `StepCombat(CombatActionIntent, Transform)` 每格依意圖推進移動、跳躍、防禦與狀態；有 `OnHealthChanged`、`OnStateChanged`、`OnIntentStepped` 事件。 | 沒有狀態快照與還原，無法做預測修正或歷史查核；移動用 `CharacterController`，重播需要能重跑同一段意圖。 |
| `HandshakeVisualFeedback` | 動畫片段與右手 IK（`OnAnimatorIK`）在本機計算。 | 已符合「表現不上網路」，只要讓它讀同步後的狀態。 |
| [Remote Pointer](../../../../WPF/Remote%20Pointer/README.md) 的 `RemotePointer.Networking` | WPF／.NET 8 工具，SignalR 用戶端、Cloudflare Tunnel 管理；共用協定有序號與房間生命週期。 | 不是 Unity 程式，也不走 UDP；只參考房間生命週期與序號驗證的做法。 |

Yu5h1Lib 目前沒有 Unity 端的網路傳輸、共同時間或狀態同步能力；這些要由選定的套件提供，或另行新增。新增的通用部分放在 Sample 還是共用套件，是〈尚未決定〉第 4 項的「通用能力歸屬」。

## 候選方案比較

候選：Fish-Networking（FishNet）、Netcode for GameObjects（NGO）＋Unity Transport、Mirror、LiteNetLib。四者都能以直接輸入位址的方式在 Windows 上連線，涵蓋「功能最齊的社群框架」、「官方框架」、「歷史最久的社群框架」、「只有傳輸層」四種取捨。

| 面向 | FishNet | NGO＋Unity Transport | Mirror | LiteNetLib |
|---|---|---|---|---|
| 查證版本 | 4.7.4R（GitHub releases） | 3.1.0（官方手冊） | 未查證版本號 | 2.x（README 標題「LiteNetLib 2」） |
| 與 Unity `6000.6.0f1` | release notes 寫 4.7.4R 在 Unity 6.6 編譯無誤；未在宿主實測 | 手冊寫支援 Unity 6.0 以上；6.6 未查證、未實測 | README 列到 6000.1；6.6 未查證、未實測 | README 寫最低 2021.2，純 C#；未實測 |
| 主機權威 | 伺服器／主機權威，主機可兼玩家 | 伺服器／主機權威，主機可兼玩家 | 伺服器／主機權威，主機可兼玩家 | 只有傳輸，權威要自己寫 |
| 輸入與時間戳 | 以 tick 為共同時間；`[Replicate]` 每 tick 上傳輸入結構 | 有網路時間（server time）與 tick；輸入要自己用 RPC 上傳 | 有 `NetworkTime`；輸入用 `[Command]` 上傳 | 有 NTP 時間請求；序號與時間要自己寫 |
| 延遲補償與回溯判定 | 碰撞體回溯（`RollbackManager`、`ColliderRollback`）是 **Pro 付費功能**；免費版可用 tick 自寫時間查核 | 官方明寫「沒有伺服器端回溯」，要自己寫 | 內建 `LagCompensator`（README 標 Beta），可記錄碰撞體歷史並查核 | 沒有，要自己寫 |
| 本機預測與修正 | `[Replicate]`／`[Reconcile]`：本機先推進，收到伺服器狀態後重設並重播未確認輸入。Pro 頁未把預測列為付費功能（未逐項確認） | 沒有完整預測；官方提供 `AnticipatedNetworkVariable`、`AnticipatedNetworkTransform` 作為自建預測的基礎；`NetworkTransform` 有插值 | README 把預測列為研究中；目前以伺服器狀態加客戶端插值為主 | 沒有，要自己寫 |
| 網路條件模擬 | `TransportManager` 的 Latency Simulator：延遲、丟包、亂序；抖動要用外部工具（例如 Clumsy） | Multiplayer Tools 的 Network Simulator：延遲、抖動、丟包（需 Unity Transport 2.0 以上） | Latency Simulation 元件：延遲、丟包、抖動 | 內建延遲與丟包模擬 |
| 區域網路直連 | 預設傳輸 Tugboat（以 LiteNetLib 為底層）設定位址與連接埠 | `UnityTransport.SetConnectionData(位址, 連接埠)` 後啟動客戶端 | 預設 KCP（可靠 UDP），輸入位址連線 | UDP socket 直連 |
| 授權 | FishNet 自訂授權，**不是 MIT**：禁止其他網路方案使用或實作本軟體；Pro 另購 | Unity Companion License | MIT | MIT |
| 維護狀況 | 持續發版，已跟進 Unity 6.5／6.6 API | Unity 官方套件，持續發版 | 社群維護，README 列出的支援版本只到 6000.1 | 持續維護 |
| 接上 `CombatActionIntent` 與判定元件 | 把意圖（換成共同 tick 與按住狀態＋序號）當成 `[Replicate]` 輸入，`CombatEntity` 的推進放進 replicate；`HandshakeArbitrator` 只在伺服器執行，結果用 `ObserversRpc` 或同步狀態下傳 | 意圖實作 `INetworkSerializable`，以 RPC 上傳；`HandshakeArbitrator` 只在主機執行，結果用 RPC 或 `NetworkVariable` 下傳；預測與時間查核自建 | 意圖以 `[Command]` 上傳，結果用 `[ClientRpc]` 或 SyncVar 下傳；預測自建，時間查核可用 `LagCompensator` | 序列化、連線管理、物件同步、RPC 全部自建，再接到判定元件 |

**共同的改動**：不論選哪個方案，握手的時間判定都要改成依共同時間的採集時間比較（見上節 `HandshakeArbitrator` 的缺口）。這種判定比的是時間與身體距離，不需要碰撞體回溯，所以 FishNet 的 Pro 回溯或 Mirror 的 `LagCompensator` 只在之後戰鬥的命中查核才有差別。

**沒有列入比較**：Netcode for Entities 以 Entities（ECS）為前提，WhiteRoom 目前是 GameObject 架構，不列入。商業雲端方案（例如 Photon）以供應商服務為主，未調查是否符合直接輸入位址與授權需求。

## 推薦與理由

推薦先以 **FishNet 免費版**做最小驗證，NGO 為備案。選定套件仍由使用者決定（〈尚未決定〉第 4 項的「同步套件」）。

理由：

1. 計畫要求本機立即回應再修正，FishNet 免費版有現成的預測與修正（`[Replicate]`／`[Reconcile]`）和共同 tick，其他三個方案都要自己寫。WhiteRoom 的意圖已是「每格一份意圖推進角色」，對得上 replicate 的寫法。
2. 握手的時間判定以共同 tick 比較採集時間即可，不需要 Pro 的碰撞體回溯；首輪不必付費。
3. 已有 Unity 6.6 編譯無誤的發版紀錄，四者中只有它查得到 6.6 的相容說明。

風險與備案：

- **授權**：FishNet 是自訂授權，禁止其他網路方案使用或實作它。若之後要在 Yu5h1Lib 發展通用網路層，要先確認這條是否影響；選定前應由使用者看過授權。
- **抖動模擬**：FishNet 內建模擬沒有抖動，要搭配外部工具；NGO 的 Network Simulator 內建抖動，且計畫引用的 Unity 延遲說明就是 NGO 文件。
- **NGO 備案的代價**：官方、授權單純、模擬工具齊全，但預測修正與時間查核都要自建，工作量較大。若授權確認不能接受，或 FishNet 在宿主實測不相容，改用 NGO。
- 兩者都要先在 `W:/UnityProject/Yu5h1LibTest` 實測編譯；取得依賴依工作區授權流程進行。

## 最小驗證案例

對應計畫〈連線驗證路線〉的連線基線案例。每輪先記錄延遲、抖動、丟包設定、時間單位、延遲是單程或 RTT，並保留無額外延遲的基準；具體數值不在此預設。

| 連線基線案例 | 最小驗證 | 證據 | 通過條件 |
|---|---|---|---|
| 開房與直接加入 | A 開房，畫面顯示位址與連接埠；B 輸入後加入。同機（127.0.0.1）與兩台 LAN 電腦各做一次；再用錯誤位址測失敗。 | 兩端畫面與連線紀錄 | 兩端進入同一場測試；失敗時顯示連線未成功。 |
| 控制與共享移動 | A、B 各自移動與跳躍，互看對方。 | 兩端位置與狀態紀錄 | 每端只控制被授權角色；記錄正式狀態與呈現偏差（容許值見〈需要使用者決定的選項〉）。 |
| 雙人互動的時機 | 在相同網路條件下握手：(1) 握緊時間窗內一方放開；(2) 握住期間連按，加入抖動；(3) 抓住後拉開距離到掙脫；(4) 承載與牽著走（實作後）。主機與遠端角色輪換。 | 每次按下／放開的序號與採集時間、主機收到時間、判定時間與結果，兩端畫面 | (1) 以採集時間判定，不以到達時間；(2) 抖動不使按住被判為放開超過容許時間，不錯誤結束握住或變成抓住；(3) 碰到與掙脫以主機距離為準，本機畫面接回修正；(4) 附著關係兩端一致。 |
| 攻防一致性 | 普通格擋、精準防禦、受擊、攻擊中止、同時出手。 | 兩端 `OnHealthChanged`／`OnStateChanged` 與出招識別 | 同一出招不重複計傷；正式結果兩端一致。 |
| 玩家／AI 共用規則 | 玩家互打，以及共同面對 AI（最多 2 玩家＋2 AI）；重設後重用同一 AI 行為設定。 | 主機判定紀錄 | 玩家與 AI 經相同出招、命中、防禦與恢復限制；不以固定腳本強制成功。 |
| 情境切換與全場重設 | 開房者切換情境或重設；客戶端嘗試改設定。 | 兩端角色、生命、計時與出招紀錄 | 兩端回到同一情境基準；客戶端不能改全場設定；舊情境的延遲訊息不影響新情境。 |
| 主機中斷與重新開房 | 分別測主機主動離開與失去連線，再重新開房加入。 | 客戶端顯示的原因與紀錄 | 客戶端結束該場並顯示原因，無法判明時顯示連線中斷；新場次不接收舊場次結果。 |
| 除錯觀察與匯出 | 開關面板、操作面板、切換視窗，匯出兩端紀錄。 | 匯出的紀錄 | 操作面板時不觸發戰鬥輸入與鏡頭，戰鬥持續且仍可受擊；返回後不補發；兩端可依場次與出招識別對照；實際延遲與模擬設定分開記錄。 |
| 不良網路與修正 | 加入延遲、抖動、丟包，重測握緊時間窗內放開、精準防禦邊界與同時出手。 | 採集時間、主機收到時間、歷史查核時間、正式結果與兩端修正 | 能重現並說明差異；結果在補償上限內依採集時間判定，超限依使用者選定的處理方式。 |

## 需要使用者決定的選項

以下屬〈尚未決定〉第 4 項。只列選項與影響，不代答；數值留待 Demo 實測。

**1. 延遲補償上限**：主機最多回頭查核多久以前的操作。

- 較短：守方看到的結果較即時，「已經躲開卻被判中」較少；延遲較高的一方較常超限。
- 較長：高延遲的一方較容易成立；低延遲的一方（包括主機玩家）較常感到「被過去的動作判中」。
- 做法：先用無額外延遲與幾組模擬延遲實測，再定數值。

**2. 時間查核**：主機如何對待客戶端送來的採集時間。

- 採用客戶端的時間，但限制在補償上限內，並要求同一角色的時間只增不減。
- 主機以收到時間減去量測的單程延遲估算，不採用客戶端時間。
- 影響：前者對「按鍵當下」最忠實，但較依賴查核；後者較難作假，但抖動會直接影響握緊時間窗與容許時間的判定。

**3. 超限處理**：操作晚於補償上限才到時。

- 丟棄這個操作。
- 改用主機收到的時間判定。
- 對持續狀態（按住、放開）和一次性操作（出招、跳躍）分別處理。

**4. 攻守公平性**：攻方出招與守方防禦或抽手的時間只差一點、兩者都在補償範圍內時。

- 傾向守方：守方在自己畫面內及時操作就成立。
- 傾向攻方：攻方出招當下判定範圍內有目標就成立。
- 以主機當下狀態為主，只允許很小的時間修正：對主機最單純，遠端延遲的影響最大。

**5. 可接受的修正幅度**：本機預測和主機結果不同時，多大的差距可以接受、如何接回。

- 差距在門檻內就平滑接回，超過門檻就直接對齊。
- 一律平滑接回。
- 一律直接對齊。
- 影響：平滑接回的期間，畫面和主機用來判定的位置會不同；直接對齊會跳動，但畫面與判定一致。

**6. 同步套件**：依上節推薦選定，或指定其他方案；含是否接受 FishNet 的自訂授權。

**7. 通用能力歸屬**：共同時間、意圖序列化、狀態快照等能力放在 WhiteRoom Sample，或放進 Yu5h1Lib 的共用套件供其他專案使用。

## 參考資料

計畫與既有文件：

- [WhiteRoom 實作計畫](../../Samples~/WhiteRoom/Implementation-Plan.md)
- [既有能力與重用依據](../../Samples~/WhiteRoom/Existing-Capabilities.md)
- [多人動作遊戲網路互動技術討論](Multiplayer-Action-Network-Interaction-Technology-Discussion.md)

FishNet：

- [授權](https://github.com/FirstGearGames/FishNet/blob/main/LICENSE.md)
- [Releases（4.7.4R、Unity 6.6）](https://github.com/FirstGearGames/FishNet/releases)
- [Prediction](https://fish-networking.gitbook.io/docs/guides/features/prediction)
- [RollbackManager（Pro）](https://fish-networking.gitbook.io/docs/fishnet-building-blocks/components/managers/rollbackmanager-pro-feature)
- [Pro、Projects 與支援](https://fish-networking.gitbook.io/docs/overview/readme/pro-projects-and-support)
- [模擬不良網路](https://fish-networking.gitbook.io/docs/tutorials/simple/simulating-bad-network-connections)
- [Tugboat](https://fish-networking.gitbook.io/docs/fishnet-building-blocks/transports/tugboat)

Netcode for GameObjects：

- [手冊 3.1](https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects@3.1/manual/index.html)
- [授權（Unity Companion License）](https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects@3.1/license/LICENSE.html)
- [處理延遲（3.1）](https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects@3.1/manual/learn/dealing-with-latency.html)
- [NetworkManager 與直接連線](https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects@3.1/manual/components/core/networkmanager.html)
- [Network Simulator](https://docs.unity3d.com/Packages/com.unity.multiplayer.tools@2.2/manual/network-simulator.html)

Mirror 與 LiteNetLib：

- [Mirror（README：授權、支援版本、功能狀態）](https://github.com/MirrorNetworking/Mirror)
- [Mirror Lag Compensation](https://mirror-networking.gitbook.io/docs/manual/general/lag-compensation)
- [LiteNetLib](https://github.com/RevenantX/LiteNetLib)
