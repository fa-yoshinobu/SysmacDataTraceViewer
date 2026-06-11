# refactor-instructions.md — SysmacDataTraceViewer

作成日: 2026-06-11。根拠: ソース・README.md・docs/spec.md・ci.bat・git履歴の実読。
本リポジトリ単体で完結する指示書。本ファイル自体は untracked のままにし、コミットに含めない。

---

## 1. Objective

このリポジトリは5プロジェクト中**唯一テストプロジェクトが存在しない**。最優先は、UI非依存で純粋な
`Services/` パーサ群に `docs/spec.md` を根拠とした特性テストの安全網を張ること。
次点で、code-behind（計1300行超 + partial 3本）から**純粋計算部分のみ**を限定的に分離する。
完全MVVM化・描画系の変更はリスクが大きいため提案に留める。

## 2. Project Understanding

- Windows WPF アプリ (net8.0-windows) + OxyPlot 2.1.2。Sysmac Studio の Data Trace CSV を可視化し、
  BOOL信号をON/OFFタイムライン、その他の型はカーソル位置の値として表示する。MIT License。
- **`docs/spec.md` に英日併記の詳細仕様あり。これがテストの受け入れ基準。**
- 構成（`src/SysmacDataTraceViewer/`）:
  - `Services/`（すべて internal static で UI 非依存・テスト容易）:
    `CsvTraceParser`（ヘッダー検出 `"Index"..."ClockTime"`、`name:type` 列振り分け、0時跨ぎの経過時間算出、
    `HH:mm:ss.fff`/`mm:ss.fff` 許容、欠損値処理）、`CsvLineParser`（引用符付きCSV1行パース）、
    `CommentCsvService`（`*_comments.csv`: Type,Name,Comment,IsVisible,ColorHex,Order の読み書き）、
    `TraceNavigationService`（変化点ジャンプ計算）、`SignalColorService`（10色パレット・`#RRGGBB`検証）、
    `UiFormattingService`（時間・状態文字列整形）。
  - `ViewModels/MainViewModel.cs`: 表示文字列と行コレクション（`BoolSignalRow`/`ValueSignalRow`）のみの薄いVM。
  - `MainWindow.xaml.cs`（1323行）+ `MainWindow.IO.cs` / `MainWindow.CursorHover.cs` / `MainWindow.NameLane.cs`:
    描画（OxyPlot系列構築）、カーソル/差分カーソル/ホバー、変化点ジャンプ、D&D並び替え、表示切替、
    PNG出力、コメントCSV適用、約30個の private 状態フィールド。**全ロジックが code-behind**。
- 依存: OxyPlot.Core / OxyPlot.Wpf。`Directory.Build.props` で `AnalysisMode=AllEnabledByDefault` +
  `EnforceCodeStyleInBuild`（解析は厳格）。
- 検証: `ci.bat` = restore → build(`-warnaserror`) → `dotnet format`(whitespace/style/analyzers 検証) →
  publish smoke test。**test 工程なし（テストが無いため）**。GitHub Actions（ci / release-single-exe / VirusTotal）。

## 3. Behaviors To Preserve

`docs/spec.md` 記載の全仕様。特に:

1. CSVヘッダー判定（`Index` で始まり `ClockTime` を含む行を探す）、対応型一覧（BOOL はタイムライン、
   INT/REAL/TIME 等18種は値パネル）、`:BOOL` を含む列の振り分け規則。
2. `ClockTime` からの経過時間算出（0時跨ぎ対応）、`Date` 省略可、`mm:ss.fff` 許容、
   空欄/不正値は欠損（UI表示 `-`）、REAL/LREAL の InvariantCulture 正規化、BOOL の `0/1`/`true/false` 許容。
3. コメントCSV仕様: フィールド構成、Name 完全一致で反映、未一致は既定値保持、Order 欠損/不正は末尾fallback。
4. PNG出力: 1800x900、左ラベル最低280px、visible/full の2種、既定ファイル名 `trace_visible.png`/`trace_full.png`、
   full は一時ズーム→復帰。
5. カーソル操作（左ドラッグ=主、右ドラッグ=差分）、変化点ジャンプ（Visible/Selected スコープ）、
   ショートカット（Ctrl+O / Ctrl+E / Shift+←→）、D&D並び替え、ラベルモード切替、`[No Change]` サフィックス。
6. エラー処理: CSV読み込み失敗・PNG出力失敗はモーダルダイアログ、CSV未読み込み時のコメント操作は情報ダイアログ。
7. 初期ステータス文言 `Load a CSV file to display BOOL timeline and value variables.` 等の固定文言。
8. csproj の `<Version>`（リリースタグと一致運用）、`.github/workflows/`、`Directory.Build.props`。

## 4. Non-Negotiables

- 開始前に `git status` 確認。本ファイル以外の差分があれば停止・報告。
- 編集前に `ci.bat` を実行し baseline を記録。失敗なら作業中止・報告。
- 1論点=1コミット。`-warnaserror` + format 検証が厳しいため、コミット前に必ず `ci.bat` の format 工程まで通す。
- NuGet 追加はテストプロジェクト新設に必要な xUnit 系のみ
  （他リポジトリ実績: xunit 2.9.2 / xunit.runner.visualstudio 2.8.2 / Microsoft.NET.Test.Sdk 17.12.0 / coverlet.collector 6.0.2）。
- 描画・イベントハンドラ・OxyPlot 操作のコードは移動も変更もしない（Phase 3 の指定範囲を除く）。

## 5. Stop And Ask Conditions

1. テストを書いたら現実装と `docs/spec.md` が矛盾した場合（どちらが正か決めない。spec が古い可能性もある）。
2. 変更がコメントCSV・PNG出力・CSVパース結果に影響しうる場合。
3. csproj / `Directory.Build.props` / `.github/workflows/` に触れたくなった場合（sln へのテストプロジェクト追加と
   csproj への `InternalsVisibleTo` 追加は除く）。
4. code-behind の分離で挙動同一性を差分レビューだけで担保できないと感じた場合（やめて報告）。

## 6. Baseline Commands

```bat
cd /d D:\refactor\SysmacDataTraceViewer
git status
ci.bat
```

## 7. Debt Map

凡例: ✅=実装可 / ⚠️=条件付き(指定フェーズ厳守) / ❌=提案・報告のみ

| # | 負債 | 根拠 | 改善案 | 可否 |
|---|---|---|---|---|
| S1 | **テストプロジェクトが存在しない** | sln 実読 | `src/SysmacDataTraceViewer.Tests`（xUnit, net8.0-windows）新設・sln 追加。`Services/` は internal static のため csproj に `InternalsVisibleTo` を追加してテスト。spec.md を根拠に特性テストを書く | ✅ |
| S2 | code-behind 1323行+partial 3本に描画・カーソル・ジャンプ・D&D・IO の全状態が同居。VM は表示文字列のみ | `MainWindow*.cs` 実読 | 完全MVVM化は**提案のみ**。限定分離のみ可: `JumpPrevChangeCore`/`JumpNextChangeCore` 内の探索計算、`ReorderBoolRows`/`ReorderValueRows` の並び替え計算など**WPF型に依存しない純粋部分**を `Services/` へ移動しテスト | ⚠️ |
| S3 | ci.bat に test 工程が無い | `ci.bat` 実読 | S1 完了後、build 工程の後に `dotnet test SysmacDataTraceViewer.sln -c Release --no-build` 相当を追加（既存工程の順序・内容は不変） | ⚠️ |
| S4 | UI文言定数（`MainWindow.xaml.cs:25-35`）と spec.md の文言が二重管理 | 同上 | 現状維持。記録のみ | ❌ |
| S5 | `MainWindow.xaml.cs` の状態フィールド約30個（`_suppress*`/`_suspend*` フラグ多数）による暗黙の状態機械 | `MainWindow.xaml.cs:38-75` | 変更リスク大。構造案（ChartController 抽出等）を提案として報告のみ | ❌ |

## 8. Implementation Phases

1. **Phase 0 — baseline**: `git status` / `ci.bat` 実行・記録。失敗なら停止。
2. **Phase 1 — テストプロジェクト新設 (S1)**: Tests プロジェクト作成・sln 追加・`InternalsVisibleTo` 追加。
   特性テスト対象（spec.md の節を根拠に）:
   - `CsvLineParser`: 引用符・エスケープ・空フィールド
   - `CsvTraceParser`: ヘッダー検出、型振り分け、0時跨ぎ、`mm:ss.fff`、Date無し、欠損値、
     BOOL `0/1`/`true/false`、ヘッダー不在/列不在時の `InvalidDataException`
   - `CommentCsvService`: round-trip、Order 欠損/不正の末尾fallback、未知Nameの無視
   - `TraceNavigationService`: 変化点の前後ジャンプ境界
   - `UiFormattingService` / `SignalColorService`: 整形・色検証・10色循環
   テストは**変更前のコードで通る**こと。spec.md と矛盾したら §5-1 で停止。
3. **Phase 2 — ci.bat へ test 追加 (S3)**。
4. **Phase 3 — 限定分離 (S2)**: 純粋計算部分のみ `Services/` へ移動（移動のみ・ロジック不変）し、
   テストを追加。1移動=1コミット。移動後にアプリを起動しメイン画面表示を確認。
   サンプルのトレースCSVが入手できる場合は読み込み→カーソル操作→ジャンプ→PNG出力を手動確認。
   入手不可なら起動確認のみとし、報告に明記。
5. **Phase 4 — 提案のみ (S2完全版, S5)**: MVVM化・状態整理の設計案を最終レポートに記載。実装禁止。

## 9. Verification Requirements

- 各フェーズで `ci.bat` 完走（`-warnaserror` build / format 3種 / publish smoke test、Phase 2 以降は test も）。
- Phase 3 の移動前後で `dotnet test` の件数・成否が同一であること。
- 手動確認の範囲と省略理由を必ず報告に書く。

## 10. Reporting Format

1. 実施フェーズ / 追加・変更ファイル / コミット一覧
2. baseline と最終の `ci.bat` 結果対比（テスト件数 0 → after）
3. 最後に実行した検証コマンドと生出力
4. Stop And Ask 該当事項一覧（spec.md と実装の矛盾を含む）
5. Phase 4 の設計提案（実装していないことを明記）
6. スキップした確認とその理由（サンプルCSV不在など）

## 11. Out-of-scope

- 完全MVVM化、描画・カーソル・イベントハンドラのコード変更
- `.github/workflows/`、バージョン番号、`Directory.Build.props`、publish 設定の変更
- UI/XAML・文言・spec.md の変更
- OxyPlot のバージョン更新、新機能追加、網羅的整形

---

## 12. Implementation Status

実装済み（2026-06-12）。

実施済みフェーズ:

- Phase 0: baseline 確認済み。
- Phase 1: `src/SysmacDataTraceViewer.Tests` を追加し、parser/comment/navigation/formatting/color 周辺の特性テストを追加済み。
- Phase 2: `ci.bat` に test 工程を追加済み。
- Phase 3: `TraceNavigationService` と `SignalOrderingService` へ限定分離済み。
- 追加 Phase 4: `docs/phase4-refactor-plan.md` に基づき、`TracePlotModelBuilder`、`IDialogService`、`CursorState`、ViewModel command 化、XAML binding 拡張を実装済み。

実装コミット:

- `e46abbb` Add service characterization tests
- `457b2ef` Run tests in local CI
- `3d84121` Extract change point target lookup
- `e6ae1d4` Extract signal row ordering
- `7d2262f` Implement phase 4 refactor

検証:

- `ci.bat` 完走。
- 最終テスト結果: 22 passed。
- publish smoke test passed。

補足:

- サンプルのトレース CSV が無いため、読み込み・カーソル操作・PNG 出力の手動確認は未実施。起動と CI/publish smoke test で確認済み。
