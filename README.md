# SysmacDataTraceViewer

Sysmac Studio Data Trace CSV viewer for Windows (.NET WPF).

---

## English

### Overview

SysmacDataTraceViewer visualizes Sysmac Studio trace CSV files on Windows.
It plots `:BOOL` signals as ON/OFF timeline lanes and shows typed value signals at the cursor position.

![Sysmac Data Trace Viewer](Sysmac%20Data%20Trace%20Viewer.png)

### Features

- BOOL timeline chart (multi-lane ON/OFF visualization)
- Primary cursor + delta cursor
- Show/hide BOOL variables and value variables
- Hide unchanged variables (`[No Change]`)
- Drag and drop reorder for BOOL/value lists
- Label mode switch (`Variable Name` / `Comment`)
- Per-BOOL color setting (`#RRGGBB`)
- PNG export (visible range / full range)
- Comment CSV load/save (comment, visibility, color, order)

### Supported CSV Signal Types

- Timeline: `:BOOL`
- Value panel: `:INT`, `:BYTE`, `:WORD`, `:DWORD`, `:LWORD`, `:SINT`, `:DINT`, `:LINT`, `:USINT`, `:UINT`, `:UDINT`, `:ULINT`, `:REAL`, `:LREAL`, `:TIME`, `:DATE`, `:TIME_OF_DAY`, `:DATE_AND_TIME`

### Build

```powershell
dotnet restore
dotnet build .\SysmacDataTraceViewer.sln -c Release
dotnet run --project .\src\SysmacDataTraceViewer\SysmacDataTraceViewer.csproj
```

### Publish (Single EXE)

```powershell
dotnet publish .\src\SysmacDataTraceViewer\SysmacDataTraceViewer.csproj `
  -c Release -r win-x64 `
  -p:PublishSingleFile=true `
  -p:SelfContained=true `
  -o .\dist\win-x64
```

### Repository

- GitHub: https://github.com/fa-yoshinobu/SysmacDataTraceViewer

### Spec

- `docs/spec.md`

### License

MIT License. See `LICENSE`.

---

## 日本語

### 概要

SysmacDataTraceViewer は、Windows 上で Sysmac Studio Data Trace CSV を可視化するビューアです。  
`:BOOL` は ON/OFF のタイムチャートとして表示し、その他の型付き信号はカーソル位置の値として表示します。

![Sysmac Data Trace Viewer](Sysmac%20Data%20Trace%20Viewer.png)

### 主な機能

- BOOL タイムチャート（複数レーン）
- 主カーソル + 差分カーソル
- BOOL 変数と値変数の表示/非表示
- 変化なし変数の識別（`[No Change]`）
- BOOL/値リストのドラッグ&ドロップ並び替え
- ラベル表示切替（`Variable Name` / `Comment`）
- BOOL ごとの色設定（`#RRGGBB`）
- PNG 出力（表示範囲 / 全範囲）
- コメント CSV の読込/保存（コメント、表示状態、色、並び順）

### 対応CSV信号型

- タイムチャート: `:BOOL`
- 値パネル: `:INT`, `:BYTE`, `:WORD`, `:DWORD`, `:LWORD`, `:SINT`, `:DINT`, `:LINT`, `:USINT`, `:UINT`, `:UDINT`, `:ULINT`, `:REAL`, `:LREAL`, `:TIME`, `:DATE`, `:TIME_OF_DAY`, `:DATE_AND_TIME`

### ビルド

```powershell
dotnet restore
dotnet build .\SysmacDataTraceViewer.sln -c Release
dotnet run --project .\src\SysmacDataTraceViewer\SysmacDataTraceViewer.csproj
```

### 配布用ビルド（単一EXE）

```powershell
dotnet publish .\src\SysmacDataTraceViewer\SysmacDataTraceViewer.csproj `
  -c Release -r win-x64 `
  -p:PublishSingleFile=true `
  -p:SelfContained=true `
  -o .\dist\win-x64
```

### リポジトリ

- GitHub: https://github.com/fa-yoshinobu/SysmacDataTraceViewer

### 仕様

- `docs/spec.md`

### ライセンス

MIT License（詳細は `LICENSE` を参照）。
