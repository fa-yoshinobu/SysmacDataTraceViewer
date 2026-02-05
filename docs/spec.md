# SysmacDataTraceViewer Specification
Version date: 2026-02-05

---

## English

### Scope
- Single-window WPF app.
- Windows only.
- Single CSV loaded at a time.
- No multi-CSV overlay.

### Purpose
- Visualize Sysmac Data Trace CSV on Windows.
- BOOL signals are rendered as timeline lanes; other typed signals are values at cursor.
- Usability is optimized for BOOL inspection by dedicating the main chart to ON/OFF timelines.

### High-Level Behavior
- On CSV load: parse -> build TraceData -> initialize BOOL/Value rows -> apply comments (if found) -> draw chart.
- On cursor move: update cursor position, update value panel, update delta/range band text.
- On visibility or ordering changes: redraw chart and update status text.
- Initial status text (before load): `Load a CSV file to display BOOL timeline and value variables.`

### CSV Input
- Header row starts with `"Index","Date","ClockTime"...`.
- Columns are `name:type` (example: `test1:BOOL`).
- Supported types:
  - Timeline chart: `:BOOL`
  - Value panel: `:INT`, `:BYTE`, `:WORD`, `:DWORD`, `:LWORD`, `:SINT`, `:DINT`, `:LINT`, `:USINT`, `:UINT`, `:UDINT`, `:ULINT`, `:REAL`, `:LREAL`, `:TIME`, `:DATE`, `:TIME_OF_DAY`, `:DATE_AND_TIME`
- Elapsed time is derived from `ClockTime` (midnight rollover handled).
- Empty or invalid value cells are treated as missing (`-` in UI).
- `Date` is optional; when missing, original time display uses `ClockTime` only.
- `ClockTime` parsing accepts `HH:mm:ss.fff` and `mm:ss.fff` formats.

### Value Parsing
- Values are stored as strings; `REAL/LREAL` are normalized using invariant culture.
- Non-numeric or empty cells are treated as missing.
- BOOL values accept `0/1` and `true/false`.

### Visualization
- BOOL chart: multi-lane ON/OFF step plot.
- Left name lane: BOOL labels (variable name or comment).
- Right panel: value variables at cursor.
- Bottom panel: variable visibility, color, comments, ordering.
- Hovering a BOOL lane shows ON/OFF state and duration for the contiguous segment.
- Chart axes: pan/zoom disabled; time window is controlled by the horizontal scrollbar.

### Screen Layout
- Top: menu bar and toolbar buttons (open/export/toggles, cursor swap, change jump).
- Top status bar: current load status text.
- Main area (center):
  - Left: BOOL name lane with horizontal scrollbar.
  - Center: time chart (OxyPlot).
  - Right: value panel (values at cursor).
- Bottom: Variable Settings panel (BOOL/Value visibility, color, comment, order).
- Splitters:
  - Between name lane and chart.
  - Between chart and right value panel.
  - Between main area and bottom panel.

### Data Model
- `TraceData`
  - `ElapsedSeconds[]`, `DateTexts[]`, `ClockTimeTexts[]`
  - `BoolSignals[]`: name, bool? values, hasChange
  - `ValueSignals[]`: name, string? values, hasChange
- `BoolSignalRow` / `ValueSignalRow`
  - Display label, comment, visibility, and ordering state.

### Interaction
- Cursor:
  - Left drag: primary cursor (value display).
  - Right drag: delta cursor.
  - Range band between cursors (toggleable).
  - Delta time display.
- Change jump:
  - Previous/Next change.
  - Scope: Visible BOOL variables or Selected BOOL variable.
- Label mode:
  - `Variable Name` / `Comment`.
  - BOOL and value labels follow the same mode.
- Name lane selection syncs to BOOL list selection.

### Mouse Controls
- Chart:
  - Left drag: move primary cursor.
  - Right drag: move delta cursor.
  - Hover over BOOL lane: show ON/OFF state and segment duration.
  - Leaving chart clears hover state.
- Name lane:
  - Left click a label: select corresponding BOOL variable.
- Lists:
  - Drag and drop BOOL or Value rows to reorder.

### UI Controls
- Toggles:
  - Right panel show/hide.
  - Variable Settings panel show/hide.
- Buttons:
  - Swap Red/Blue Cursors.
  - Previous/Next Change.
  - BOOL/Value: Select All, Clear All, Hide No Change.
- Reorder: BOOL/Value lists via drag-and-drop.
- Splitters: resizable name lane, panels.
- Horizontal scroll bars for time and name-lane overflow.

### Comments CSV
- File: `*_comments.csv`
- Fields: Type, Name, Comment, IsVisible, ColorHex, Order
- Types: `BOOL` and `VALUE`
- `Order` controls list ordering; missing/invalid order falls back to append.
- Load order:
  - BOOL and VALUE rows are matched by exact name.
  - Missing entries keep defaults.

### PNG Export
- Visible range export.
- Full range export.
- Includes left labels.
- Default file name:
  - Visible range: `trace_visible.png`
  - Full range: `trace_full.png`
- Export size: 1800 x 900.
- Left label margin is expanded to at least 280 px for export.
- Full range export temporarily zooms to the entire data range, then restores view.
- Output format: PNG.

### Menu
- File: Open Trace CSV, Export PNG (visible/full), Load/Save Comments, Exit
- View: Show Type Suffix, Show Range Band
- Help: About

### Shortcuts
- `Ctrl+O`: Open Trace CSV
- `Ctrl+E`: Export Visible Range PNG
- `Shift+Left`: Previous Change
- `Shift+Right`: Next Change

### About Dialog
- Shows app name, version, author, and GitHub link.
- Lists libraries and licenses (app, OxyPlot.Core, OxyPlot.Wpf, .NET Runtime).
- Displays embedded OSS license text from `LICENSE`.

### Color Handling
- BOOL default palette uses a fixed 10-color cycle.
- Color input is `#RRGGBB`; invalid values are ignored.

### View Options
- `Show Type Suffix`: toggles showing the `:TYPE` suffix in labels.
- `Show Range Band`: toggles the cursor range band between primary/delta cursors.

### Status Text
- Shows file name, sample count, BOOL count (and visible count), value count (and visible count).

### Versioning
- `AssemblyInformationalVersion` is fixed (no hash).

### Limits
- Single CSV loaded at a time.
- Multi-CSV overlay is not supported.

### Error Handling
- CSV load error: modal dialog with error text.
- PNG export error: modal dialog with error text.
- Load/Save comments without CSV loaded: informational dialog.

### Screen Transitions
- Main window:
  - File > Open Trace CSV... opens file dialog; on success, updates chart and panels.
  - File > Export Chart PNG > Visible/Full opens save dialog; on success, writes PNG.
  - File > Load Comments... opens file dialog; on success, applies comments.
  - File > Save Comments... opens save dialog; on success, writes comment CSV.
  - Help > About opens About dialog (modal).
- About dialog:
  - Close button closes dialog.

### State Transitions
- Initial (no CSV loaded):
  - Chart shows empty axes.
  - Value panel shows no values.
  - Save/Load Comments warns and exits.
- CSV loaded:
  - TraceData populated and cached.
  - BOOL rows initialized (default palette + comments).
  - Value rows initialized (comments + visibility).
  - Chart drawn for visible BOOLs.
  - Status text updated.
- Comments loaded:
  - BOOL/Value rows updated by matching name.
  - Order and visibility updated.
  - Labels refreshed; chart redrawn.
- Visibility change (BOOL):
  - Visible list recalculated.
  - Chart redrawn; status updated.
- Visibility/order change (Value):
  - Visible value list recalculated.
  - Status updated.

---

## 日本語

### スコープ
- 単一ウィンドウのWPFアプリ。
- Windows専用。
- 1回に1つのCSVを読み込み。
- 複数CSVの重ね表示は非対応。

### 目的
- Sysmac Data Trace CSVをWindows上で可視化する。
- BOOL信号はタイムラインとして表示し、その他の型はカーソル位置の値として表示する。
- BOOLのON/OFF解析に特化し、操作性を高める。

### 高レベル動作
- CSV読み込み: パース -> TraceData生成 -> BOOL/Value行初期化 -> コメント適用(あれば) -> チャート描画。
- カーソル移動: カーソル位置更新 -> 値パネル更新 -> 2カーソル差分表示更新。
- 表示/並び変更: チャート再描画、ステータス更新。
- 初期ステータス: `Load a CSV file to display BOOL timeline and value variables.`

### CSV入力
- ヘッダー行は `"Index","Date","ClockTime"...` で始まる。
- 列名は `name:type` 形式（例: `test1:BOOL`）。
- 対応型:
  - タイムライン表示: `:BOOL`
  - 値表示: `:INT`, `:BYTE`, `:WORD`, `:DWORD`, `:LWORD`, `:SINT`, `:DINT`, `:LINT`, `:USINT`, `:UINT`, `:UDINT`, `:ULINT`, `:REAL`, `:LREAL`, `:TIME`, `:DATE`, `:TIME_OF_DAY`, `:DATE_AND_TIME`
- 経過時間は `ClockTime` から算出（0時跨ぎ対応）。
- 空欄/不正値は欠損扱い（UIは `-`）。
- `Date` が無い場合は `ClockTime` のみ表示。
- `ClockTime` は `HH:mm:ss.fff` と `mm:ss.fff` を許容。

### 値の扱い
- 値は文字列として保持。REAL/LREALはInvariantCultureで正規化。
- 数値不正/空欄は欠損。
- BOOLは `0/1` と `true/false` を許容。

### 可視化
- BOOLチャート: ON/OFFのステッププロット。
- 左: BOOLラベル（変数名/コメント切替）。
- 右: カーソル位置の値表示。
- 下: 変数設定（表示/色/コメント/順序）。
- BOOLレーンにホバーすると、ON/OFF状態と区間時間を表示。
- チャートのパン/ズームは無効。横スクロールで時間を移動。

### 画面レイアウト
- 上: メニューバーとツールボタン。
- その下: ステータス表示。
- 中央:
  - 左: BOOL名レーン（横スクロール対応）
  - 中: 時間チャート（OxyPlot）
  - 右: Valueパネル
- 下: Variable Settingsパネル（BOOL/Valueの表示/色/コメント/順序）
- スプリッター:
  - 名前レーンとチャート間
  - チャートと値パネル間
  - 中央領域と下パネル間

### データモデル
- `TraceData`
  - `ElapsedSeconds[]`, `DateTexts[]`, `ClockTimeTexts[]`
  - `BoolSignals[]`: name, bool? values, hasChange
  - `ValueSignals[]`: name, string? values, hasChange
- `BoolSignalRow` / `ValueSignalRow`
  - 表示名、コメント、可視性、順序情報を保持

### 操作
- カーソル:
  - 左ドラッグ: 主カーソル
  - 右ドラッグ: 差分カーソル
  - 2カーソル間帯（表示切替可）
  - 経過時間表示
- 変化点ジャンプ:
  - Previous/Next Change
  - 対象範囲: Visible BOOL / Selected BOOL
- ラベルモード:
  - `Variable Name` / `Comment`
  - BOOLとValueで共通
- Name lane選択はBOOLリストと同期

### マウス操作
- チャート:
  - 左ドラッグ: 主カーソル
  - 右ドラッグ: 差分カーソル
  - BOOLレーンのホバー: ON/OFF区間と継続時間表示
  - チャート外に出るとホバー解除
- Name lane:
  - クリックでBOOL選択
- リスト:
  - BOOL/ValueのD&D並び替え

### UI操作
- 表示切替:
  - 右パネル表示/非表示
  - Variable Settings表示/非表示
- ボタン:
  - カーソル入替
  - Previous/Next Change
  - BOOL/Value: Select All / Clear All / Hide No Change
- 横スクロール:
  - 時間軸
  - 名前レーン

### コメントCSV
- ファイル: `*_comments.csv`
- フィールド: Type, Name, Comment, IsVisible, ColorHex, Order
- Type: `BOOL` / `VALUE`
- `Order` で並び順を保持（不正/欠損は末尾へ）。
- 読み込み:
  - Name一致で反映
  - 未一致は既定値保持

### PNG出力
- 表示範囲と全範囲の2種類。
- 左ラベル込みで出力。
- 既定ファイル名:
  - 表示範囲: `trace_visible.png`
  - 全範囲: `trace_full.png`
- 出力サイズ: 1800 x 900。
- 左ラベル幅は最低280pxに拡張。
- 全範囲出力は一時的に全体表示へズームし、復帰する。
- 形式: PNG。

### メニュー
- File: Open Trace CSV, Export PNG (visible/full), Load/Save Comments, Exit
- View: Show Type Suffix, Show Range Band
- Help: About

### ショートカット
- `Ctrl+O`: Open Trace CSV
- `Ctrl+E`: Export Visible Range PNG
- `Shift+Left`: Previous Change
- `Shift+Right`: Next Change

### Aboutダイアログ
- アプリ名、バージョン、作者、GitHubリンクを表示。
- OSSライセンス一覧（本体、OxyPlot.Core/Wpf、.NET Runtime）。
- `LICENSE` を埋め込み表示。

### 色
- BOOLの既定色は10色パレットの循環。
- 色入力は `#RRGGBB`。不正値は無視。

### 表示オプション
- `Show Type Suffix`: ラベルに `:TYPE` を表示/非表示。
- `Show Range Band`: 2カーソル間の帯を表示/非表示。

### ステータス表示
- ファイル名、サンプル数、BOOL数(表示数)、Value数(表示数)を表示。

### バージョン
- `AssemblyInformationalVersion` は固定値（ハッシュ無し）。

### 制限
- CSVは1つのみ。
- 複数CSVの重ね表示は非対応。

### エラー処理
- CSV読み込み失敗: エラーダイアログ。
- PNG出力失敗: エラーダイアログ。
- CSV未読み込み時のコメント操作: 情報ダイアログ。

### 画面遷移
- Main:
  - Open Trace CSV... で読み込み → 表示更新
  - Export PNG... で保存
  - Load/Save Comments... でコメント反映/保存
  - About でダイアログ表示
- About:
  - Closeで閉じる

### 状態遷移
- 初期(未読み込み):
  - 空のチャート
  - 値パネル空
  - コメント操作は警告
- CSV読み込み:
  - TraceData生成
  - BOOL/Value行初期化
  - チャート描画
  - ステータス更新
- コメント読み込み:
  - Name一致で反映
  - 並び/可視性更新
  - ラベル/チャート更新
- 表示変更(BOOL):
  - 再描画 + ステータス更新
- 表示/並び変更(Value):
  - 値リスト更新 + ステータス更新
