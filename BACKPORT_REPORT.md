# AutoDuty Back-porting 研究報告

> 基準：tc 分支（tag `0.0.0.228` / commit `0986123`，API12，Microsoft.NET.Sdk + net9.0）
> 目標來源：erdelf/AutoDuty master（commit `b7bc07d7`，API15，Dalamud.NET.Sdk/15 + net10.0）
> 硬限制：The Calamity Dalamud = **12.0.1.5-tc.18（API12）**，最終產物必須在 API12 編譯執行
> 產出日期：2026-07-05

---

## 一、整體差距

| 指標 | 數值 |
|---|---|
| 時間跨度 | 2025-07-28 → 2026-07-04（約 11 個月） |
| 總 commit 數 | 1,076（核心程式碼約 335 個非 merge） |
| 總 diff | 467 檔，+110,595 / −60,766 |
| 核心程式碼（排除 Paths/Localization） | 86 檔，+11,622 / −6,707 |
| Paths 路徑資料 | 306 檔（新增 71 個副本目錄，含 7.2/7.3 內容） |

**主幹檔案幾乎重寫**：`AutoDuty.cs` ±2,725 行、`Config.cs` ±1,952 行、`ActionsManager.cs` ±978 行。

### 建置系統斷層（master 無法直接降到 API12 的原因）

- SDK：`Microsoft.NET.Sdk` → `Dalamud.NET.Sdk/15.0.0`；TFM：net9.0 → **net10.0**
- ImGui binding 全面更換：`ImGuiNET` → `Dalamud.Bindings.ImGui`（API13 破壞性變更，逐檔滲透）
- `IGameObject.DataId` → `.BaseId`；`LegacyTaskManager` → `NeoTaskManager`
- 依賴樹整體 net10/API15 生態：新增 submodule `ECommons.IPC`（API12 時代不存在）、`NightmareUI`；Pictomancy 改 NuGet 1.0.10；新增 `WrathCombo.API`
- C# 14 語言特性（`field` keyword、null-conditional assignment）散佈各檔

### master 主要新功能

1. **Multibox 多開協同**（詳見第三節）
2. **Playlist 副本播放清單**（深度耦合 MainTab/Config/QueueHelper）
3. **完整 Localization 系統 + 官方 zh-TW**（詳見第二節）
4. **多設定檔 Config Profiles**（per-character）
5. **StatsTab 統計頁**（耦合低，新檔為主）
6. Armoire 衣櫃收納 / BLU 青魔輔助 / Novice Hall
7. Trust/Squadron 大幅改進、Wrath Combo 深度整合
8. **路徑/動作系統重構**：新路徑 JSON 用新 action 語彙，與舊 ActionsManager 不完全相容 —— 想吃新副本路徑需連帶移植此重構

---

## 二、官方 Localization 系統（重大發現）

**master 已內建 zh-TW 繁中翻譯**，2026-01-11 由社群貢獻者 lianghong 提交（"i18n: Add zh-TW lang"）。

### 架構

- `Managers/LocalizationManager.cs`（`Loc.Get(key)` alias）：巢狀 JSON 攤平成 `"MainWindow.Goto.Barracks"` 點分隔 key
- 三層 fallback：目前語言 → en-US → 回傳 key 字串（缺翻譯不會壞）
- 支援 `string.Format` 參數化；語言在 Config 頁手動切換（無自動偵測）
- **只依賴 Newtonsoft.Json + Svc.Log，無 API15 相依 → 可原樣搬到 API12**

### zh-TW 品質評估

| 項目 | 結果 |
|---|---|
| 條目數 | 544（en-US 537） |
| 有效覆蓋率 | ~86%（缺 75 個 2026 上半年新功能 key，fallback 顯示英文） |
| 簡體字殘留 | **0** |
| 性質 | 真人繁中在地化，非簡轉繁（「開啟檔案」非「打開文件」、「設定檔」非「配置」） |
| 小瑕疵 | 1 處「設置」、2 處「自定義」（台灣慣用「自訂」）；82 個 stale key |

### 與 tc 分支 ini 方案的比較結論

**採官方 JSON 系統，tc 的 331 條 ini 翻譯降級為術語校訂語料。**

關鍵理由：ECommons `.Loc()` 用「英文原文當 key」，上游任何英文措辭一改，ini 條目整條失效；官方 key-based JSON 與上游同構，之後同步只需 cherry-pick json diff。

---

## 三、Multibox 模組

### 架構

```
MultiboxUtility (746 行核心)
├── MultiboxConfiguration（掛在 ConfigurationMain.multibox）
├── Server（Host，最多 3 client = 4 人小隊）
├── Client（單連線 + 10s KeepAlive）
└── StreamString（2-byte 長度前綴 + UTF-16 編碼）
        ↓ ITransport
NamedPipeTransport │ TcpTransport（預設 port 1716）
```

### 運作模式

- **Host**：強制 Regular 副本模式；自動組隊邀請（`InfoProxyPartyInvite`）；廣播排本（`DUTY_QUEUE`）、退本（`DUTY_EXIT`）、path 同步（`PATH_STEPS` JSON）、step 推進（`STEP_START|index`）
- **Client**：被動接收；自動接受邀請/排本確認；進本自動開跑；UI 鎖 Loop 設定
- **Lock-step 步進同步**：每 step 開始前 block，client 回報 `STEP_COMPLETED`，Host 等全員到齊才廣播下一步（boss 戰與寶箱有特例）

### 整合點（back-port 時要動的地方）

- `AutoDuty.cs`：~10 處（Stage 轉換 blocking、LoadPath、TerritoryChanged、Loop 續跑條件、Dispose）
- `QueueHelper.cs`：2 處（Host 廣播、client `InvokeAcceptOnly()`）
- `DeathHelper.cs`：1 處（死亡同步）
- `Config.cs`：Multiboxing 設定區段（~230 行 UI）
- `MainWindow.cs`：1 處（client 鎖 Loop UI）

### API12 相容性評估：**中等偏低風險**

有利因素：
- 傳輸層純 .NET BCL（Pipes/Sockets），net9.0 全有
- **初版 multibox（`cc7b6bbb`，2025-08-05）本來就誕生在 API12 環境**，可作參考實作
- `InfoProxyPartyInvite` 等 FFXIVClientStructs API 在 API12 已存在（初版實證）

必須處理的缺口：
| 項目 | 處理方式 |
|---|---|
| C# 14 `field` keyword（MultiboxUtility 2 處 + AutoDuty.cs 多處） | 改明確 backing field |
| `Config?.MultiBox = false`（C# 14 null-conditional assignment） | 改 if 判斷 |
| `Player.CurrentWorld.RowId` | 舊 ECommons 用 `Player.CurrentWorldId` |
| `Chat.ExecuteCommand`（static） | 舊版 `Chat.Instance.ExecuteCommand` |
| `ConfigurationMain.JsonSerializerSettings` | 一併移植或用預設 settings |
| UI：`Dalamud.Bindings.ImGui` → `ImGuiNET`、`ImRaii.DisabledDisposable` → `IEndObject` | 改寫 UI 區段 |
| `Loc.Get(...)` | 先 back-port Localization 系統即可沿用 |
| NightmareUI `Censor` 名字打碼 | 移除此功能（tag 無此 submodule） |
| `PartyHelper.cs`（tag 缺） | 初版 `cc7b6bbb` 的 122 行版本即 API12 相容，直接取用 |
| `QueueHelper.InvokeAcceptOnly()`（tag 缺） | ~7 行，容易補 |
| 命名差異：`Indexer/Action`（tag 大寫）vs `indexer/action`；`TerritoryChanged(ushort)` vs `(uint)` | 逐一對應 |

順手可修的上游 bug：
- `MultiboxUtility.cs:114` `IsDead()` 條件反了（`if (Config.MultiBox) return;` 應為 `!`），死亡同步實際失效
- 明文認證、TCP 無加密（遠端連線信任網路環境）
- 未連線 slot 的訊息佇列堆積（舊訊息重放隱患）

---

## 四、策略結論

| 方案 | 評估 |
|---|---|
| A. 從 master cherry-pick 回 tc | ❌ 不現實 —— 主幹檔案已重寫，幾乎必然全衝突 |
| B. master 整體降級到 API12 | ❌ 不划算 —— 依賴樹（ECommons.IPC/NightmareUI/Pictomancy/WrathCombo.API）整體是 net10/API15 生態 |
| **C. 分層取用（採用）** | ✅ 以 tc 為基，參照 master 實作手寫 API12 版 |

### 方案 C 執行順序

1. **Back-port 官方 Localization 系統**（小工作量、高對齊價值）
   - 搬 `LocalizationManager.cs` + `Localization/` 目錄 + csproj Content 規則
   - 把 tc 現有 331 條 ini 的術語校訂合併進 zh-TW json（「設置→設定」「自定義→自訂」），補缺失 key
   - 把 tc 已包的 `.Loc()` 呼叫改為 `Loc.Get(key)`，key 命名對齊 master（未來同步的減震器）
   - 移除 ECommons ini 方案
2. **Back-port Multibox**（中等偏低風險，參照初版 `cc7b6bbb` + 現版架構）
   - 傳輸層 3 檔照搬 → MultiboxUtility 處理 C#14/ECommons 差異 → 整合點逐一接上 → UI 改 ImGuiNET
   - 順手修 `IsDead()` bug
3. （可選）按價值/耦合比追加：StatsTab → Armoire/BLU Helper → Playlist
4. **路徑資料**：舊格式路徑可直接更新；新副本路徑需先評估 ActionsManager 重構的 back-port

### 長期注意

上游一年 1,076 commits，凍結在 API12 的分支會加速腐化。若 The Calamity 未來升 API13+，ImGui binding 那刀無可避免 —— 方案 C 累積的「與上游 key/結構對齊」會顯著降低屆時的換軌成本。
