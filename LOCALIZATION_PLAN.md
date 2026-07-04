# AutoDuty 繁體中文化計畫 (tc 分支 / API12)

## 目標
將 AutoDuty UI 繁體化，且採用對「back-porting 官方新功能」最友善的架構。

## 架構決策：使用 ECommons 內建 `.Loc()` 框架

ECommons (`ECommons.LanguageHelpers.Localization`) 已內建在地化：
- **以英文原字串為 key**：`"Start".Loc()` → 有翻譯回中文，無則 fallback 回 `"Start"`
- 翻譯存於 plugin 目錄的 `Language<名稱>.ini`，格式 `英文==中文`（每行一條，`\n` 表換行）
- `Localization.Logging = true` 時會自動把所有經過 `.Loc()` 但缺翻譯的 key 收集起來 → 可一鍵匯出待翻譯清單

**為何選這個路線（而非直接替換字面量）：**
- 原始碼結構幾乎不變，只是 `"文字"` → `"文字".Loc()`，英文 key 留在原地
- merge 官方更新衝突極小；官方改字串 = 只需在 `.ini` 補一條
- 翻譯與程式碼分離，可隨時切換語言 / 對照校對

## 前提驗證（go/no-go）
1. **CJK 字型**：Dalamud 預設字型須含中文字符，否則顯示豆腐字 □□□。
   - The Calamity 為中文服 (`language: zh-TW`)，理論上已載入中文字型 → 需實機驗證。
   - 若不行，需在 UiBuilder 掛自訂字型 + CJK GlyphRange（額外工項）。

## 實作步驟

### Phase 0：基礎建設
- [ ] 在 `AutoDuty()` 建構子（`ECommonsMain.Init` 之後）呼叫 `Localization.Init("ChineseTraditional")`
- [ ] 建立空的 `LanguageChineseTraditional.ini`，並設定 csproj 將其複製到輸出目錄
- [ ] 加一個 dev 開關可切 `Localization.Logging` 收集 key

### Phase 1：字串收集（半自動）
- [ ] 開 Logging 模式，跑過所有 UI 分頁，收集全部 key
- [ ] 或用腳本掃 `Windows/*.cs` 擷取 ImGui 字面量 → 產生英中對照草稿
- [ ] 產出 `英文==` 空白對照表

### Phase 2：包裹 `.Loc()`（分檔進行）
依字串量排序，逐檔把使用者可見字串包上 `.Loc()`：
- [ ] `Config.cs`（195 處，最大宗）
- [ ] `MainWindow.cs`（35）
- [ ] `BuildTab.cs`（30）
- [ ] `LogTab.cs`（26）
- [ ] `MainTab.cs`（25）
- [ ] `PathsTab.cs`（9）、`Overlay.cs`（5）、`InfoTab.cs`（4）
- **注意**：ImGui 的 `##id` 部分不可翻（`"Label##id"` → 只翻 Label，或用 `.Loc()` 後仍保留 id）。含 `##` 者需個別處理。
- **不翻**：duty path 動作參數、內部識別字串、config key 名。

### Phase 3：翻譯
- [ ] 填寫 `.ini` 繁中翻譯（FF14 台服術語對齊：如「副本」「隨機任務」「盾職」等）

### Phase 4：驗證
- [ ] build（用 `-p:DalamudLibPath=%APPDATA%/XIVThecalamity/Dalamud/Hooks/dev/`）
- [ ] 實機載入，逐頁檢查中文顯示正常、無豆腐字、無破版

## 風險
- 字型豆腐字（見前提）
- `##id` 誤翻導致 ImGui 控件 ID 衝突／狀態錯亂
- 字串串接（`$"..."` 插值）較難純 key 化，需個案處理
