# SysmacDataTraceViewer Specification
Last reviewed / 最終確認: 2026-11-13

This document describes the current repository implementation. Released builds may differ. / この文書は現在のリポジトリの実装を対象とし、公開済みEXEとは異なる場合があります。

[README](../README.md) · [Development and maintenance](development.md) · [Changelog](../CHANGELOG.md)

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
- Open through File → Open Trace CSV, by dropping one CSV onto the window, or by passing the first startup argument: `SysmacDataTraceViewer.exe "trace.csv"`. All paths share the same asynchronous loader. Multiple files and non-CSV drops show a reason; an unreadable startup file shows an error and leaves an empty window. Signal-row reordering remains separate. No Windows file association is registered.
- Trace and comment CSV files use BOM-specified encoding when present. Without a BOM, the entire file is validated as strict UTF-8; decoding failure falls back to Shift_JIS (CP932). Invalid bytes are not silently replaced. The detected encoding is displayed in the status text. Comment CSV saving remains UTF-8.
- The parser searches for a header whose first column is `Index` and which contains `ClockTime` (case-insensitive). Blank lines and preceding metadata are skipped. `Date` is optional; `Index,ClockTime,...` is also accepted.
- Columns are `name:type` (example: `test1:BOOL`).
- BOOL columns are rendered as timelines. Other columns with a `:TYPE` suffix appear in the value panel; columns without a type suffix are ignored.
- Integer validation applies to `INT`, `BYTE`, `WORD`, `DWORD`, `LWORD`, `SINT`, `DINT`, `LINT`, `USINT`, `UINT`, `UDINT`, and `ULINT`. `REAL`/`LREAL` use floating-point parsing. Other types, including `STRING`, `TIME`, `DATE`, `TIME_OF_DAY`, and `DATE_AND_TIME`, remain text.
- Elapsed time is derived from `ClockTime` (midnight rollover handled).
- Empty or invalid value cells are treated as missing (`-` in UI).
- `Date` is optional; when missing, original time display uses `ClockTime` only.
- `ClockTime` accepts hours:minutes:seconds or minutes:seconds, with optional fractional seconds. The first valid sample starts at elapsed time 0. A remaining backwards time step is clamped to 0 and counted as a warning. Dates are used for display and rollover detection, not multi-day elapsed-time calculation.

### Value Parsing
- Values are stored as strings; `REAL/LREAL` are normalized using invariant culture.
- Empty cells are missing. Integer types accept decimal integer text, and unsigned types reject negative values; PLC bit-width ranges are not checked. Invalid numeric text is missing.
- Other types retain their trimmed text; they are not required to be numeric and are not converted to another unit or date format.
- BOOL values accept `0/1` and `true/false`.

### Visualization
- BOOL chart: multi-lane ON/OFF step plot.
- Left name lane: BOOL labels (variable name or comment).
- Right panel: value variables at cursor.
- Bottom panel: variable visibility, color, comments, ordering.
- Hovering a BOOL lane shows ON/OFF state and duration for the contiguous segment.
- Mouse-wheel zoom changes the time axis. Its scrollbar follows the visible interval and moves that interval horizontally; it sits below the chart only. Direct axis panning and vertical-axis zoom are disabled.
- Missing BOOL samples are skipped in the plotted line, connecting surrounding valid points. Cursor values still show missing samples as `-`.

### Screen Layout
- Window title: loaded file name followed by the application name.
- Top: menu bar, then a toolbar (panel toggles, cursor swap, change jump, label mode, jump scope) that wraps on narrow windows.
- Cursor readouts below the toolbar, grouped and color-coded: red data cursor (elapsed time, recorded time, sample index), blue delta cursor (cursor delta), and hover state/duration.
- Top status bar: current load status text. It turns amber with a warning mark and a Read Details button when the CSV had read issues, and shows a progress indicator and Cancel while loading.
- Bottom footer: mouse and keyboard hints.
- Before a trace is loaded, the chart shows how to open one (drop a CSV or File > Open Trace CSV).
- Every other BOOL lane is lightly striped in the chart and in the name lane; the name lane shows ON as a green `1`.
- Main area (center):
  - Left: BOOL name lane with ellipsized labels (full text in the tooltip) and values kept inside the visible width. Its rows align with the chart plotting area, including the plot padding.
  - Center: time chart (OxyPlot).
  - Right: value panel (values at cursor).
- Bottom: Variable Settings panel (BOOL/Value visibility, color, comment, order).
- Splitters:
  - Between name lane and chart.
  - Between chart and right value panel.
  - Between main area and bottom panel.

### Data Model
- `TraceData`
  - Elapsed seconds, original date/time text, BOOL/value signal lists, detected encoding, read diagnostics, and comment-source summary.
  - `BoolSignals[]`: name, bool? values, hasChange
  - `ValueSignals[]`: name, string? values, hasChange
- `BoolSignalRow` / `ValueSignalRow`
  - Display label, comment, visibility, and ordering state.
- Sample Index is the zero-based index of accepted samples, not the original CSV Index field or physical line number.

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
  - Moving over an empty lane, a missing value, or outside the plot data area clears the hover readout on the next chart mouse move. Merely leaving the window can leave the last readout visible.
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
  - BOOL: Auto Assign Colors applies the 10-color palette to visible rows in display order.
- Reorder: BOOL/Value lists via drag-and-drop.
- Splitters: resizable name lane, panels.
- The horizontal scrollbar moves the time window. Name labels are trimmed within their panel width and show their full text in a tooltip; BOOL values stay visible.

### Comments CSV
- File: `*_comments.csv`
- Fields: Type, Name, Comment, IsVisible, ColorHex, Order
- Types: `BOOL` and `VALUE`
- `Order` controls list ordering; missing/invalid order falls back to append.
- Load order:
  - BOOL and VALUE rows are matched by exact name.
  - Missing entries keep the current state (defaults immediately after loading a new trace).
- A sibling `<trace-name>_comments.csv` is loaded automatically after the trace; a failed automatic comment load does not block the trace. Manual loading reports errors.
- Saving uses BOM-marked UTF-8 and writes a temporary file in the destination directory before replacing the target. Quoted commas, quotes, and multiline comments round-trip.
- Save Comments opens a save dialog; an existing destination requires overwrite confirmation.

### Comments from smc2
- After opening a trace, choose File > Load Comments (CSV / smc2)... and select a Sysmac Studio project.
- Imports SLWD-format global and POU-local variables, array-element comments, and structure/FB members. HMI/Safety device tables are excluded. Type suffixes are removed when matching trace names, and POU scope is respected.
- The PLC with the most resolved trace signals is selected only when unique. No matches or an equal match count across PLCs produces an error without changing existing comments.
- Only matched, nonempty comments are imported. Missing or unresolved comments keep their current text. Visibility, colors, and ordering stay unchanged.
- Import runs in the background and supports Cancel or Escape. The status shows imported, comment-empty, and unmatched counts. The source project is read-only; no comment CSV is saved automatically. Save Comments can persist the result as CSV.

### PNG Export
- Visible range export.
- Full range export.
- Includes left labels, lane stripes, and current cursor/range/hover annotations. Time-axis view and temporary export formatting are restored afterwards.
- Default file name:
  - Visible range: `trace_visible.png`
  - Full range: `trace_full.png`
- Export size: 1800 x 900.
- Left label margin is expanded to at least 280 px for export.
- Full range export temporarily zooms to the entire data range, then restores view.
- Output format: PNG.

### Menu
- File: Open Trace CSV, Export Chart PNG (visible/full), Load Comments (CSV / smc2), Save Comments, Exit
- View: Show Type Suffix, Show Range Band
- Help: About

### Shortcuts
- `Ctrl+O`: Open Trace CSV
- `Ctrl+E`: Export Visible Range PNG
- `Shift+Left`: Previous Change
- `Shift+Right`: Next Change
- `Escape`: cancel an active CSV or smc2 load. Shift+Arrow remains text selection when a text editor has focus.

### About Dialog
- Shows app name, version, author, and GitHub link.
- Lists libraries and licenses (app, OxyPlot.Core, OxyPlot.Wpf, .NET Runtime).
- Displays embedded OSS license text from `LICENSE`.

### Color Handling
- BOOL default palette uses a fixed 10-color cycle.
- Color input is `#RRGGBB`; invalid values are ignored.

### View Options
- `Show Type Suffix`: toggles the `:TYPE` suffix when displaying variable names; imported comment text is unchanged.
- `Show Range Band`: toggles the cursor range band between primary/delta cursors.

### Status Text
- Shows file name, sample count, BOOL/value counts and visible counts, detected encoding, comment source/encoding, and any CSV warning counts. Long text has a tooltip.
- Read Details shows physical row numbers and reasons for the first 20 issues: skipped rows, invalid values, and backwards time steps. Blank value cells are missing data without an invalid-value warning.
- CSV parsing runs off the UI thread, reports progress, and supports cancellation. Editing controls are disabled during loading. Parse failure or cancellation preserves the previously loaded trace; an unreadable startup file leaves an empty window.

### Versioning
- The project `Version` property is the product-version input. Assembly/file/informational versions are derived from it; informational version excludes the source hash. Runtime pinning and release procedures are documented in [development.md](development.md).

### Limits
- Single CSV loaded at a time.
- Multi-CSV overlay and numeric-signal graphs are not supported.
- No PLC communication or Windows file-association registration is performed.
- Integer syntax validation does not enforce the value range of each PLC type.
- Trace CSV parsing expects one physical line per sample. Multiline fields are supported in comment CSV files only.

### Error Handling
- CSV load error: modal dialog with error text.
- PNG export error: modal dialog with error text.
- Load/Save comments without CSV loaded: informational dialog.
- Unexpected UI/process exceptions trigger best-effort notification and a bounded local log in `%LOCALAPPDATA%\SysmacDataTraceViewer\logs\`. Each log is at most 1 MiB, with one rotated file. It stores exception types, HResult and stacks, excluding exception messages that could contain CSV names or values. Fatal exceptions can still terminate the app.

### Screen Transitions
- Main window:
  - File > Open Trace CSV... opens file dialog; on success, updates chart and panels.
  - File > Export Chart PNG > Visible/Full opens save dialog; on success, writes PNG.
  - File > Load Comments (CSV / smc2)... opens a comment CSV or Sysmac Studio project. CSV applies saved display settings; smc2 imports only matching nonempty comments, preserving visibility, colors, and ordering.
  - File > Save Comments... opens save dialog; on success, writes comment CSV.
  - Help > About opens About dialog (modal).
- About dialog:
  - Close button closes dialog.

### State Transitions
- Initial (no CSV loaded):
  - Chart shows empty axes and an instruction card for opening or dropping a CSV.
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
  - CSV updates saved order/visibility/colors; smc2 changes comments only.
  - Labels refreshed; chart redrawn.
- Visibility change (BOOL):
  - Visible list recalculated.
  - Chart redrawn; status updated.
- Visibility/order change (Value):
  - Visible value list recalculated.
  - Status updated.

Labels on the chart and value panel follow Label Mode. BOOL settings rows keep variable names; Show Type Suffix controls the type suffix. Hide No Change considers valid samples only, skipping missing values.

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
- File → Open Trace CSV、ウィンドウへのCSV 1ファイルのドロップ、または起動時の第1引数（`SysmacDataTraceViewer.exe "trace.csv"`）で開く。全て同じ非同期処理を呼ぶ。複数ファイル・CSV以外のドロップは理由を表示して拒否する。信号の並び替えとは干渉せず、Windowsの関連付け登録は行わない。
- トレース・コメントCSVはBOMがあればその文字コードで読み込みます。BOMなしではファイル全体をUTF-8として厳密に検証し、復号できなければShift_JIS（CP932）へ切り替えます。不正なバイトを黙って置換しません。判定した文字コードをステータスに表示します。コメントCSVの保存は従来どおりUTF-8です。
- 先頭列が `Index` で、`ClockTime` 列を含むヘッダーを検索する（大文字・小文字を区別しない）。手前のメタデータや空行は読み飛ばす。`Date` は省略でき、`Index,ClockTime,...` も受け付ける。
- 列名は `name:type` 形式（例: `test1:BOOL`）。
- BOOL列はタイムライン、それ以外の `:TYPE` 付き列は値パネルへ表示する。型サフィックスのない列は表示対象にしない。
- 整数として検証する型: `INT`, `BYTE`, `WORD`, `DWORD`, `LWORD`, `SINT`, `DINT`, `LINT`, `USINT`, `UINT`, `UDINT`, `ULINT`。`REAL`/`LREAL` は浮動小数点として解釈する。それ以外（`STRING`, `TIME`, `DATE`, `TIME_OF_DAY`, `DATE_AND_TIME` など）は文字列として保持する。
- 経過時間は `ClockTime` から算出（0時跨ぎ対応）。
- 空欄/不正値は欠損扱い（UIは `-`）。
- `Date` が無い場合は `ClockTime` のみ表示。
- `ClockTime` は「時:分:秒」または「分:秒」を受け付け、秒の小数部は任意。最初の有効サンプルを経過時間0とする。0時跨ぎ補正後も逆行する時刻差は0に丸め、警告として数える。日付は表示と0時跨ぎの判定に使い、日付差による複数日の経過時間計算は行わない。

### 値の扱い
- 値は文字列として保持。REAL/LREALはInvariantCultureで正規化。
- 空欄は欠損。整数型は十進整数表記を受け付け、符号なし型の負数を拒否する。PLCの型ごとのビット幅・値域は検証しない。不正な数値は欠損扱い。
- その他の型は前後の空白を除いた文字列を保持し、数値であることを要求しない。時間単位や日付書式の変換もしない。
- BOOLは `0/1` と `true/false` を許容。

### 可視化
- BOOLチャート: ON/OFFのステッププロット。
- 左: BOOLラベル（変数名/コメント切替）。
- 右: カーソル位置の値表示。
- 下: 変数設定（表示/色/コメント/順序）。
- BOOLレーンにホバーすると、ON/OFF状態と区間時間を表示。
- マウスホイールで時間軸を拡大・縮小できる。グラフの下だけにある横バーが表示範囲へ追従し、拡大後の時間範囲を横移動できる。軸の直接パンと縦軸のズームは無効。
- BOOL描画は欠損点を飛ばして前後の有効点を接続する。欠損区間を独立した空白としては描画せず、カーソル位置の値は `-` で表示する。

### 画面レイアウト
- タイトルには読み込んだファイル名を表示する。
- 上: メニューバーと、狭い幅で折り返すツールバー。
- カーソル情報は赤の主カーソル、青の差分カーソル、ホバー状態・継続時間に分けて表示する。
- ステータスは読込み警告があると琥珀色になり、Read Detailsで詳細を開ける。読込み中は進捗とCancelを表示する。
- 未読込み時はCSVのドロップまたはメニューから開く案内を表示し、下部にはマウス・キー操作のヒントを表示する。
- 名前レーンとグラフは1行おきに背景色を付けて位置を揃え、BOOL値の `1` は緑色にする。
- 中央:
  - 左: BOOL名レーン。長い名前は省略し、ツールチップで全文を表示する。BOOL値を表示幅内に保ち、グラフの内側余白も含めて行位置を合わせる。
  - 中: 時間チャート（OxyPlot）
  - 右: Valueパネル
- 下: Variable Settingsパネル（BOOL/Valueの表示/色/コメント/順序）
- スプリッター:
  - 名前レーンとチャート間
  - チャートと値パネル間
  - 中央領域と下パネル間

### データモデル
- `TraceData`
  - 経過秒、元の日付・時刻文字列、BOOL/Value信号リスト、判定した文字コード、読込み診断、コメント取込み元の概要を保持する。
  - `BoolSignals[]`: name, bool? values, hasChange
  - `ValueSignals[]`: name, string? values, hasChange
- `BoolSignalRow` / `ValueSignalRow`
  - 表示名、コメント、可視性、順序情報を保持
- Sample Indexは読み込めたサンプルの0始まりの添字であり、CSVのIndex列やファイルの物理行番号ではない。

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
  - グラフのBOOLラベルと値パネルのラベルに適用する。BOOL設定リストは変数名を維持する。
- Name lane選択はBOOLリストと同期

### マウス操作
- チャート:
  - 左ドラッグ: 主カーソル
  - 右ドラッグ: 差分カーソル
  - BOOLレーンのホバー: ON/OFF区間と継続時間表示
  - 欠損値や空のレーン、描画領域外へ移動すると次のチャート内マウス移動でホバー表示を解除する。ウィンドウ外へ出るだけでは直前の表示が残る場合がある。
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
  - BOOL: Auto Assign Colorsは表示中の行へ、表示順に10色パレットを割り当てる。
- 横スクロールは時間軸のみ。名前レーンは幅内で省略表示する。
- Hide No Changeは欠損値を飛ばし、有効な値の変化がない信号を隠す。

### コメントCSV
- ファイル: `*_comments.csv`
- フィールド: Type, Name, Comment, IsVisible, ColorHex, Order
- Type: `BOOL` / `VALUE`
- `Order` で並び順を保持（不正/欠損は末尾へ）。
- 読み込み:
  - Name一致で反映
  - 未一致の信号は現在の状態を保持する（新しいトレースを開いた直後は既定値）。
- トレースと同じフォルダーの `<トレース名>_comments.csv` を自動読込みする。自動読込みの失敗はトレース表示を妨げず、手動読込みではエラーを表示する。
- BOM付きUTF-8で保存する。同じフォルダーの一時ファイルを書き終えてから保存先を置き換える。カンマ・引用符・改行を含むコメントを読み戻せる。
- 保存ダイアログで保存先を選び、既存ファイルには上書き確認を行う。

### smc2からコメントを直接取り込む

トレースCSVを開き、File → Load Comments (CSV / smc2)... でSysmac Studioプロジェクトを選択する。処理はバックグラウンドで行い、読込み中のCancelで取り消せる。元プロジェクトは変更せず、コメントCSVも自動保存しない。

対応対象はSLWD形式のグローバル変数・POU内変数、配列要素、構造体／FBのメンバーコメント。HMI・Safetyの変数テーブルは対象外とする。CSVの型サフィックスを除く名前とPOUを照合する。複数PLCのプロジェクトでは、CSVの信号名が最も多く解決できるPLCを一意に特定した場合に取り込む。同数の候補がある場合や一致する信号がない場合はエラーを表示し、既存コメントを保持する。

一致した空でないコメントだけを反映し、コメント未設定・未解決の信号は既存コメントを保持する。信号の表示・色・並び順は維持する。ステータスには取込み件数・コメント未設定件数・未解決件数を表示する。Label Mode → Commentでグラフに表示でき、Save Comments...で既存形式のコメントCSVへ保存できる。

### PNG出力
- 表示範囲と全範囲の2種類。
- 左ラベル、レーンの縞、現在のカーソル・範囲帯・ホバー注釈を含めて出力し、書出し後は時間軸の表示範囲と一時的な書式を復元する。
- 既定ファイル名:
  - 表示範囲: `trace_visible.png`
  - 全範囲: `trace_full.png`
- 出力サイズ: 1800 x 900。
- 左ラベル幅は最低280pxに拡張。
- 全範囲出力は一時的に全体表示へズームし、復帰する。
- 形式: PNG。

### メニュー
- File: Open Trace CSV, Export Chart PNG (visible/full), Load Comments (CSV / smc2), Save Comments, Exit
- View: Show Type Suffix, Show Range Band
- Help: About

### ショートカット
- `Ctrl+O`: Open Trace CSV
- `Ctrl+E`: Export Visible Range PNG
- `Shift+Left`: Previous Change
- `Shift+Right`: Next Change
- `Escape`: CSV・smc2読込み中の取消し。テキスト編集中のShift+矢印は文字列選択を優先する。

### Aboutダイアログ
- アプリ名、バージョン、作者、GitHubリンクを表示。
- OSSライセンス一覧（本体、OxyPlot.Core/Wpf、.NET Runtime）。
- `LICENSE` を埋め込み表示。

### 色
- BOOLの既定色は10色パレットの循環。
- 色入力は `#RRGGBB`。不正値は無視。

### 表示オプション
- `Show Type Suffix`: 変数名表示時の `:TYPE` を表示/非表示にする。コメント自体の文字列は変更しない。
- `Show Range Band`: 2カーソル間の帯を表示/非表示。

### ステータス表示
- ファイル名、サンプル数、BOOL/Value数と表示数、判定した文字コード、コメント取込み元・文字コード、CSVの警告件数を表示する。長い表示はツールチップで確認できる。
- 読み飛ばし行・不正値・時刻逆行を数え、Read Detailsで先頭20件の物理行番号と理由を表示する。値の空欄は欠損扱いだが、不正値の警告には数えない。
- CSV解析は画面スレッドの外で行い、進捗表示と取消しに対応する。読込み中は編集操作を無効にする。解析失敗・取消し時は前のトレースを維持し、読めない起動引数はエラー後に空の画面を残す。

### バージョン
- csprojの `Version` を入力とし、アセンブリ・ファイル・製品バージョンを導出する。製品バージョンにはソースのハッシュを付けない。ランタイムの固定とリリース手順は[開発・保守手順](development.md)を参照。

### 制限
- CSVは1つのみ。
- 複数CSVの重ね表示と数値信号のグラフ化は非対応。
- PLCとの通信やWindowsの拡張子関連付け登録は行わない。
- 整数の表記を検証するが、PLCの型ごとの値域は検証しない。
- トレースCSVは1サンプルを1物理行として読む。改行を含むフィールドへの対応はコメントCSVのみ。

### エラー処理
- CSV読み込み失敗: エラーダイアログ。
- PNG出力失敗: エラーダイアログ。
- CSV未読み込み時のコメント操作: 情報ダイアログ。
- 予期しないUI・プロセス例外は通知と `%LOCALAPPDATA%\SysmacDataTraceViewer\logs\` への記録を試みる。ログは1ファイル最大1MiB、退避1世代。例外の型・HResult・スタックを保存し、CSVの名前や値を含み得る例外Messageは保存しない。継続できない例外ではアプリが終了する場合がある。

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
  - 空のチャートとCSVを開く案内
  - 値パネル空
  - コメント操作は警告
- CSV読み込み:
  - TraceData生成
  - BOOL/Value行初期化
  - チャート描画
  - ステータス更新
- コメント読み込み:
  - Name一致で反映
  - CSVは並び・可視性・色を更新する。smc2はコメントだけを更新する。
  - ラベル/チャート更新
- 表示変更(BOOL):
  - 再描画 + ステータス更新
- 表示/並び変更(Value):
  - 値リスト更新 + ステータス更新
