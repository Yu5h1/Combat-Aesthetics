# Combat Aesthetics 對 Animation 的需求

目標：動畫編輯器線（`W:\Yu5h1Lib\AnimationEditor`，格式規範在 data-driven/animation；[握手動作片段](file:///W:/Yu5h1Lib/AnimationEditor/Documentation/Handshake-Clips.md)）。提出端：Combat-Aesthetics 的 WhiteRoom Sample。接用方式：動畫線以動畫編輯器製作握手片段，經角色建造器匯出動作 GLB，再由 UnityExtension 的人形匯入器（LIMB-ANIM-1）轉成人形片段給 WhiteRoom 播放；互動規則由 [WhiteRoom 計畫〈第一個交付的目標〉](../Samples~/WhiteRoom/Implementation-Plan.md#第一個交付的目標) 擁有。提出日期：2026-10-31。未結需求：2。

## 比力氣的拚力後慢退（COMBAT-ANIMATION-1）

| 欄位 | 內容 |
|---|---|
| Status | Implemented，2026-10-31（WhiteRoom 匯入 Combat-Aesthetics `c9b215c`）。提出 2026-10-31。2026-10-31 驗收交付（data-driven `c51cee3`、AnimationEditor `d7bbf16`）：第 1 項通過，清單第 3 批列出「拚力後慢退」約 1.5 秒（`slow-withdraw.anim.json` 45 影格），起點和用力握相同、結束和收手起點相同（`handshake.test.mjs` 銜接測試通過）。第 2 項未通過：片段以沒有手指的角色製作，「慢慢鬆開」只靠 `fist_r` 的縮放表現，人形匯入只寫肌肉姿勢、不帶骨骼縮放，到 WhiteRoom 會消失，也不符合清單「角色要有手指」的製作規則。待動畫線在有手指的角色（ANIMATION-LIMB-6）提交後，以手指重做這段的放鬆。2026-10-31 驗收重做（AnimationEditor `7912a50`、data-driven `b1db727`）：第 1 項維持通過；`slow-withdraw` 改用有手指的成人角色，放鬆由手指關節旋轉表現，沒有縮放軌道。第 2 項待 WhiteRoom 匯入後驗證（[第 3 批第三輪修正交辦](../Samples~/WhiteRoom/Implementation-Plan.md#第-3-批第三輪修正交辦) 第 3 項）。2026-10-31 驗收匯入（Combat-Aesthetics `c9b215c`）：第 2 項通過，人形片段 `handshake-slow-withdraw` 帶手指肌肉曲線，右手食指由 −0.04 到 0.67 慢慢鬆開，設定資產指定為 `ContestTie` 狀態的片段（[握手第 3 批第三輪修正驗收](WhiteRoom-Acceptance.md#握手第-3-批第三輪修正驗收)）。 |
| Requirement | 握手片段第 3 批加一段「拚力後慢退」：比力氣的次數差距在門檻內且有人按過時，雙方從用力握的姿勢慢慢鬆開、退開，再接收手，表現用力後的解脫。 |
| Evidence | 使用者 2026-10-31 定下比力氣的規則（WhiteRoom 計畫〈第一個交付的目標〉的「比力氣」列）：每次握緊成功都進入握住，還有一方按著就持續、雙方都放開才結束，期間只算按下次數；差距超過門檻時少的一方痛苦、多的一方得意；差距在門檻內且有人按過時拚力後慢退再收手；0 比 0 時搖一下再收手。目前片段清單第 3 批只有用力握、痛苦、得意。 |
| Acceptance | 1. 片段清單第 3 批列出這段，長度依製作規則訂定，從用力握接上，結束的姿勢能接收手。2. 片段做出後，可經人形匯入給 WhiteRoom 使用。 |
| Impact | WhiteRoom 第 3 批（[第 3 批交辦](../Samples~/WhiteRoom/Implementation-Plan.md#第-3-批交辦比力氣)）播放這段；它和第 3 批其他片段一樣，要等有手指的角色（ANIMATION-LIMB-6）。 |
| Workaround | 片段到之前，WhiteRoom 只在 HUD 與事件紀錄顯示這個結果，雙方用收手回到站姿。 |

## 大笑的用途改為放開的一方（COMBAT-ANIMATION-2）

| 欄位 | 內容 |
|---|---|
| Status | Implemented，2026-10-31（data-driven `c51cee3`）。提出 2026-10-31。依據：片段清單「大笑」的用途改為「惡作劇時放開的一方」，與 WhiteRoom 計畫「惡作劇」列一致；片段未改。 |
| Requirement | 片段清單中「大笑」的用途由「惡作劇的發起方」改為「惡作劇時放開的一方」。 |
| Evidence | 使用者 2026-10-31 決定惡作劇不分發起方或回應方，誰單獨放開就是誰惡作劇（WhiteRoom 計畫〈第一個交付的目標〉的「惡作劇」列）；片段清單仍寫「惡作劇的發起方」。 |
| Acceptance | 片段清單「大笑」的用途欄與新規則一致；片段本身不用改。 |
| Impact | 無；發起方與回應方本來就共用同一組片段。 |
| Workaround | 以 WhiteRoom 計畫的規則為準。 |

## 抓住與掙脫的片段（COMBAT-ANIMATION-3）

| 欄位 | 內容 |
|---|---|
| Status | Implemented，2026-10-31（WhiteRoom 匯入 Combat-Aesthetics `c9b215c`）。提出 2026-10-31。2026-10-31 驗收交付（data-driven `c51cee3`、AnimationEditor `d7bbf16`）：第 1 項通過，清單第 3 批列出想抽手（1 秒循環，`caught` 30 影格）、掙脫（約 1 秒，`stumble` 30 影格）、抓空（約 1 秒，`grasp-empty` 30 影格），掙脫與抓空結束和收手起點相同（銜接測試通過）。第 2 項通過：`caught` 只有身體與手臂軌道，沒有手的接觸約束；清單製作規則也寫明手由執行時 IK 處理。第 3 項未通過：片段以沒有手指的角色製作，「抓空」的抓手只靠 `fist_r` 的縮放表現，人形匯入不帶骨骼縮放，到 WhiteRoom 會消失，也不符合清單「角色要有手指」的製作規則。待動畫線在有手指的角色（ANIMATION-LIMB-6）提交後，以手指重做抓空。2026-10-31 驗收重做與補充（AnimationEditor `7912a50`、data-driven `b1db727`）：第 1 項通過，`grasp-empty` 改由手指表現；補充的三段帶根位移：想抽手 `caught` 往後 0.2 m／循環，被拖著跟上 `dragged` 往前 0.2 m／循環且手指維持握住，跳開 `leap` 往後 0.5 m，滯空的影格 1～28 解除地板約束、落地重新釘住。第 2 項維持通過。第 3 項待 WhiteRoom 匯入後驗證（第三輪修正交辦第 3 項）。2026-10-31 驗收匯入（Combat-Aesthetics `c9b215c`）：第 3 項通過，五段都匯入並指定到狀態；抓空的食指由握到張（−0.74 到 0.67），根位移 `RootT.z` 為想抽手 −0.20 m／循環、被拖著跟上 +0.20 m／循環、跳開 −0.42 m 且最高點升起 0.33 m，三個狀態由根位移移動角色（[握手第 3 批第三輪修正驗收](WhiteRoom-Acceptance.md#握手第-3-批第三輪修正驗收)）。 |
| Requirement | 握手片段第 3 批加三段：「想抽手」（循環，被抓住的一方身體往後拉、想把手抽回）、「掙脫」（掙脫的一方往後踉蹌、甩甩手，結束能接收手）、「抓空」（抓的一方手抓空，結束能接收手）。 2026-10-31 補充：「想抽手」要帶往後的根位移（被抓的一方往後拉時的移動由它決定）；另加「被拖著跟上」（抓的一方被拉著往前跟的循環，帶根位移）與「跳開」（被抓的一方跳起往後彈開，帶根位移）。 |
| Evidence | 使用者 2026-10-31 定下抓住與掙脫的規則（WhiteRoom 計畫〈第一個交付的目標〉的「抓住與掙脫」列）：握住期間一方放開、另一方還按著，就是抓住不放；被抓住期間雙方都能慢步移動，身體拉開超過門檻就掙脫；掙脫的一方往後踉蹌、甩甩手，抓的一方抓空，再各自收手。目前片段清單沒有這三段。 使用者 2026-10-31 另定「動作驅動位移」：位移由專屬動畫的根位移決定，不另設速度與距離參數（WhiteRoom 計畫〈第一個交付的目標〉的「動作驅動位移」列）。 |
| Acceptance | 1. 片段清單第 3 批列出這三段，長度依製作規則訂定。2. 「想抽手」只做身體，手的位置由 WhiteRoom 在執行時以人形 IK 黏在對方手上，片段不必對準對方的手。3. 片段做出後，可經人形匯入給 WhiteRoom 使用。 |
| Impact | WhiteRoom 第 3 批（[第 3 批交辦](../Samples~/WhiteRoom/Implementation-Plan.md#第-3-批交辦比力氣)）播放這三段；和第 3 批其他片段一樣，要等有手指的角色（ANIMATION-LIMB-6）。 |
| Workaround | 片段到之前，WhiteRoom 只在 HUD 與事件紀錄顯示抓住與掙脫，手的 IK 照常表現被拉住，雙方用收手回到站姿。 |

## 片段標出控制區間（COMBAT-ANIMATION-4）

| 欄位 | 內容 |
|---|---|
| Status | Outstanding，提出 2026-10-31。2026-10-31 驗收交付（Web `2e1b4ab`、AnimationEditor `7912a50`、data-driven `b1db727`）：第 3 項通過，收手、大笑、握拳生氣、拍手大笑以及痛苦、得意、拚力後慢退、掙脫、抓空、跳開、想抽手、被拖著跟上、用力握都標了「不可控制」「可控制」區間。第 1 項未通過：動畫編輯器只在時間軸畫出區間，不能新增、移動、刪除或改名；格式在現行 `0.4.0` 直接加入 `intervals`，沒有依規範〈版本與相容〉的規則（加入選用欄位要提高 minor，如 `0.3.0`）升版；`animation.schema.json` 定義了 `$defs/intervals` 卻沒有在任何版本引用，`0.4.0` 分支不允許額外欄位，帶區間的片段會被 schema 拒絕；規範的範例表沒有區間的案例。第 2 項未開始：動作 GLB 匯出與人形匯入讀取區間，動畫線的 handoff 寫「待 CharacterBuilder 與 UnityExtension 接入」，但沒有向兩者提需求。2026-10-31 驗收修正（data-driven `04638ab`、Web `60dfa47`、AnimationEditor `e1e60ff`）：第 1 項通過：格式升為 `0.5.0`，`0.4.0` 照讀且帶 `intervals` 時拒絕；schema 在 `0.5.0` 引用區間，3 個新案例與 17 段握手片段都通過 schema 檢查；規範補了區間的範例與錯誤案例；動畫編輯器能新增（`Alt+I`）、拖曳移動與調整起訖、改名（`F2`）、刪除區間，都是快捷鍵命令；Web 57 項、AnimationEditor 85 項測試通過。第 3 項維持通過。第 2 項已開需求、待下游實作：角色建造器輸出動作 GLB 帶區間（AnimationEditor `Documentation/Requirements.CharacterBuilder.md` 的 ANIMATION-LIMB-8），UnityExtension 人形匯入器讀取區間（AnimationEditor `Documentation/Requirements.UnityExtension.md` 的 ANIMATION-UNITYEXTENSION-1）。附帶問題：這次改動把規範約束表 `node` 列正規式裡的 `\|` 改成 `|`，GitHub 表格會在那裡斷欄，也拿掉了約束節點的 `maxLength: 64`；兩處都已在 data-driven `f6eaf9f` 修回。WhiteRoom 目前照片段檔手動填寫區間（Combat-Aesthetics `c9b215c`），第 2 項接通後改由匯入讀取。2026-11-05 第 2 項通過：17 段握手 GLB 經角色建造器重新匯出（13 段帶區間，與 `.anim.json` 逐檔一致；伸手、等待、握住、搖手沒有），取代套件與宿主的舊檔，片段 fileID 沒變；`HandshakeSettings` 刪掉手填區間改讀匯入的 `MotionIntervals`，3 項測試 3／3 通過（[驗收紀錄](WhiteRoom-Acceptance.md#2026-11-05-區間改由匯入讀取)）。待處理：樣本 `.anim.json` 與 GLB 內的區間名稱是亂碼。 |
| Requirement | 握手片段能帶具名的控制區間標記（例如可取消、不可控制、可控制），隨片段交付給 WhiteRoom；標記的格式由動畫線訂定。 |
| Evidence | 使用者 2026-10-31 定下「動作合理性約束操控」：每個狀態綁定一段參考動畫，由動畫時間軸上標出的具名區間決定接不接受操作，區間順序依動作而定，調整分界就是調整操控手感（WhiteRoom 計畫〈第一個交付的目標〉的同名列）。目前片段只有姿勢與長度，沒有區間資料。 |
| Acceptance | 1. 片段格式能標出具名區間，在動畫編輯器裡可以看到並修改。2. 區間標記隨動作 GLB 交付，人形匯入後 WhiteRoom 讀得到。3. 至少收手、大笑、握拳生氣、拍手大笑標好區間。 |
| Impact | WhiteRoom 依區間決定放開的一方何時能再伸手；之後的出招、受擊也用同一套區間。 |
| Workaround | 標記到之前，WhiteRoom 在 Unity 端為片段設定區間，片段帶標記後改讀片段的資料。 |

## 移動用的走路、後退、側走片段（COMBAT-ANIMATION-5）

| 欄位 | 內容 |
|---|---|
| Status | Outstanding，提出 2026-11-03。2026-11-05 驗收交付（AnimationEditor `37a32f0`，清單 `Documentation/Locomotion-Clips.md`）：第 1 項通過：`samples/` 有往前走 `walk`、往後退 `walk-back`、往左 `strafe-left`、往右 `strafe-right` 四段循環，用 `punch-character.glb` 製作，和握手片段同一個角色；手指沒有動畫軌道，和握手的 `withdraw` 一樣維持靜止姿勢。第 2 項通過：四段都是 30 fps、32 影格，第 0 影格的支撐腳相同（左腳落地），根位移方向正確（前 1.12 m、後 0.8 m、左右各 0.56 m／循環），速度只由根位移決定；`locomotion.test.mjs` 28／28 在本機重跑通過。第 3 項部分通過：另交的待機 `idle`（60 影格，沒有根位移）第 0 影格等於收手最後一格，可以取代目前 Idle 用的收手最後一格；四段移動片段的頭尾不是站姿，要在 Unity 用過渡接上站姿，這在 WhiteRoom 組 blend tree 時驗證。第 4 項未開始：還沒經角色建造器匯出動作 GLB 與人形匯入。清單外另交了警戒 `alert`（60 影格，出拳的護架姿勢），留給戰鬥驗證使用。即時播放與 Unity 中的混合都還沒驗證。2026-11-05 WhiteRoom 匯入並組成混合：第 3 項通過：`idle` 已取代收手最後一格當 Idle，六段 GLB 經人形匯入（根位移方向與長度和清單一致），Move 混合樹與 0.15 s 過渡接上站姿，測試 4／4 通過（[驗收紀錄](WhiteRoom-Acceptance.md#2026-11-05-移動片段與方向混合)）。第 4 項通過：動作 GLB 已匯入並給 WhiteRoom 使用。視覺（腳步是否打架、過渡外觀）尚待使用者試玩。 |
| Requirement | 提供一組人形移動片段：往前走、往後退、往左側走、往右側走（循環），給 WhiteRoom 在 Unity 組成方向混合（blend tree），讓角色依移動方向播放對應的步伐。 |
| Evidence | 使用者 2026-11-03 試玩握手 demo：角色移動時沒有步伐動作，WhiteRoom 計畫〈第一個交付的目標〉的「動作驅動位移」要求移動由專屬動畫的根位移決定，片段沒到之前只能暫代。握手時會邊伸手邊走近、被抓時往後拉、抓的一方跟上，都需要前進、後退與側移。 |
| Acceptance | 1. 片段清單列出四段循環：往前走、往後退、往左側走、往右側走，用有手指的成人角色製作，和握手片段同一個角色。2. 四段可以互相混合：循環長度相同、起步的腳與步伐相位一致，混合時腳不打架；每段帶根位移（前進、後退、側移的方向正確），速度由根位移決定，不另訂數值。3. 起點和終點姿勢能接站姿（握手片段 Idle 用的收手最後一格）。4. 經角色建造器匯出動作 GLB、人形匯入後可給 WhiteRoom 使用。 |
| Impact | WhiteRoom 用這四段在 Unity 組 2D 方向混合（blend tree 由 WhiteRoom 組裝，不是動畫線的交付物），取代目前移動時的暫代；之後戰鬥場景沿用。 |
| Workaround | 片段到之前，角色移動時維持站姿平移，事件紀錄標明暫代。 |

