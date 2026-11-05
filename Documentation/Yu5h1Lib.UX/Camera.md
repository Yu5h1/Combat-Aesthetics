# Camera — Combat-Aesthetics WhiteRoom

WhiteRoom 的第三人稱鏡頭是 Yu5h1Lib.UX Camera 目標 1「繞焦點的轉盤式環繞」的來源之一：滑鼠改方位與仰角，仰角限制在一段不對稱的範圍，鏡頭繞角色上的焦點放置。這份紀錄擁有 WhiteRoom 的對照與來源基線；規則以規範為準，這裡不重寫。

```text
Yu5h1Lib.UX Camera 0.1.0-draft（案例 0.1.0）
└─ Samples~/WhiteRoom/Runtime/WhiteRoomCamera.cs：宣告方位不限、仰角 −25° 到 +65°
   ├─ LateUpdate：讀滑鼠位移，改 _yaw、_pitch，再依焦點與距離擺鏡頭
   └─ 規範外：焦點跟隨角色、避障拉近、游標鎖定（宿主行為）
```

## 第 1 輪：來源基線（0.1.0-draft）

| 項目 | 內容 |
|---|---|
| 規範 | [Camera](../../../../UX/Documentation/ModuleSpec/Foundation/Camera.md) 0.1.0-draft，UX `25f5b13` |
| SHA-256 | `Foundation/Camera.md` 2e4b66a43c0ff0f5847de31640998914e4c9a6f3c91437ea3e286b437d5830b2；`Foundation/Camera/camera-cases.json`（0.1.0）92ea5c5201728a7382d1c1f90b10e7e45b46fe2de36d490e06284cbf3f907d6e |
| 實作 | Combat-Aesthetics `e7acdb1`（`WhiteRoomCamera.cs` 最後變更 `3411ad3`） |
| 範圍宣告 | 方位不限；仰角 −25° 到 +65°（`_minPitch`、`_maxPitch` 的預設值，換到規範的仰角：鏡頭在焦點上方為正） |
| DX | 不適用 |

## 第 1 輪：實作對照（讀程式）

| 行為 | 位置 | 做法 |
|---|---|---|
| CAM-POSE | `LateUpdate`：`pivot − Quaternion.Euler(_pitch, _yaw, 0) × forward × _distance`，`LookAt(pivot)` | 左手座標；`_pitch` 為正時鏡頭在焦點上方往下看，等於規範的仰角；`_yaw` 增加時鏡頭往自己的左方繞，等於規範的方位減少 |
| CAM-ORBIT、CAM-RANGE | `_yaw += 滑鼠 x × 靈敏度`；`_pitch −= 滑鼠 y × 靈敏度` 後夾在 `_minPitch`～`_maxPitch` | 方位不限（不換回 −π 到 π，比較時換算，見規範〈允許差異〉）；仰角超出的量丟掉 |
| CAM-OVERHEAD | — | 仰角最多 +65°，到不了正上方，不適用 |
| CAM-DIRECTION | 同上 | 滑鼠往右，朝向往右轉；滑鼠往下（y 為負），`_pitch` 增加、鏡頭升高往下看，與規範相同 |

讀程式時看到的規範外行為：從焦點往鏡頭打射線，撞到東西時把鏡頭拉到撞點前面，所以距離會變；這是避障，不在目標 1，不算偏離。

## 第 1 輪：結果

| 行為 | 案例或檢查 | 結果 | 原因 |
|---|---|---|---|
| CAM-ORBIT、CAM-RANGE | uneven-elevation-range | 未執行 | 環繞的計算寫在 `LateUpdate` 裡、直接讀滑鼠，沒有能送入角度變化的公開入口；要執行需要在 Unity 的 Play 模式模擬滑鼠輸入，並讓場景裡沒有會觸發避障的物體 |
| CAM-POSE | orbit-keeps-focus-and-distance、free-azimuth-turns-past-half-a-turn | 未執行 | 同上 |
| 其他 | limited-range-clamps-and-turns-back-at-once、overhead-stops-without-flipping | 不適用 | 範圍不是 WhiteRoom 宣告的方位不限、仰角 −25° 到 +65° |
| CAM-DIRECTION | CA01 方向與正上方 | 未執行 | 需要使用者在 Play 模式實際移動滑鼠；正上方不適用 |

上面的對照只是讀程式，不是執行結果，不能當作通過。

這份來源基線只用來判斷 Camera 規範與 WhiteRoom 鏡頭是否一致，不代表任何其他實作，也不代表其他消費端需要每一條規則。

## 輪次

| 規範版本 | 來源 commit | DX | 實作 commit | checklist | 結論 |
|---|---|---|---|---|---|
| 0.1.0-draft | UX `25f5b13` | 不適用 | Combat-Aesthetics `e7acdb1` | 無（結果在本頁） | 未執行：沒有可送入角度的公開入口；讀程式沒有看到與規範衝突的地方 |

## 2026-11-05：改用共用 CameraController

`WhiteRoomCamera.cs` 已刪除。`WhiteRoomSceneBuilder.EnsureCamera` 改在 Main Camera 上掛 `Yu5h1Lib.CameraControl.CameraController` 與 `MouseKeyboardCameraInput`（com.yu5h1.common）：目標偏移 (0, 1.4, 0)、距離 4、仰角 −25°～65°、每像素 0.2°（等於原本 Input System 位移 ×0.1×2）、樞軸平滑 15。游標鎖定、Alt／Esc 切換與 UI 焦點讓出由 `MouseKeyboardCameraInput` 負責，`WhiteRoomCharacter.IsUIFocused` 當作 blocker。上面的對照是舊實作的紀錄，不再對應程式。
手感差異：原本平滑相機位置、旋轉每影格 `LookAt`；現在平滑樞軸、相機剛性接在樞軸後方，轉動沒有拖尾，平滑與幀率無關。
