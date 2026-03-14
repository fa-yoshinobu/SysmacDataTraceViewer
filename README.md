# SysmacDataTraceViewer

[![release](https://img.shields.io/github/v/tag/fa-yoshinobu/SysmacDataTraceViewer?label=release&color=orange)](https://github.com/fa-yoshinobu/SysmacDataTraceViewer/tags)
[![CI](https://github.com/fa-yoshinobu/SysmacDataTraceViewer/actions/workflows/ci.yml/badge.svg)](https://github.com/fa-yoshinobu/SysmacDataTraceViewer/actions/workflows/ci.yml)
[![release-exe](https://github.com/fa-yoshinobu/SysmacDataTraceViewer/actions/workflows/release-single-exe.yml/badge.svg)](https://github.com/fa-yoshinobu/SysmacDataTraceViewer/actions/workflows/release-single-exe.yml)
[![license](https://img.shields.io/badge/license-MIT-yellowgreen)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-8.0--windows-512BD4)](src/SysmacDataTraceViewer/SysmacDataTraceViewer.csproj)

Sysmac Studio Data Trace CSV viewer for Windows (.NET WPF).

---

## English

### Overview

SysmacDataTraceViewer visualizes Sysmac Studio trace CSV files on Windows.
It plots `:BOOL` signals as ON/OFF timeline lanes and shows typed value signals at the cursor position.

![Sysmac Data Trace Viewer](Sysmac%20Data%20Trace%20Viewer.png)

### Current Version

- `1.0.2`

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

### Recent Updates

- BOOL Name Lane `Value` column shows `0/1` at the current cursor sample.
- Hovering a BOOL signal in the time chart now shows signal name with state (for example `Signal_X: ON`).
- Version updated to `1.0.2`.

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
.\make.bat win-x64 .\dist\win-x64
```

Or run the publish command directly:

```powershell
dotnet publish .\src\SysmacDataTraceViewer\SysmacDataTraceViewer.csproj `
  -c Release -r win-x64 `
  -p:PublishSingleFile=true `
  -p:SelfContained=true `
  -o .\dist\win-x64
```

### Automated CI / Release

- `CI`: runs on push to `main` and on pull requests. It restores, builds with analyzers enabled, verifies `dotnet format`, and smoke-tests Single EXE publish.
- `Release Single EXE`: push a tag like `Ver.1.0.2` that matches the project `<Version>` to build the `win-x64` self-contained Single EXE and create a GitHub Release automatically.
- `VirusTotal scan on release`: runs after the GitHub Release is published and appends the VirusTotal result to the release page.

### Repository

- GitHub: https://github.com/fa-yoshinobu/SysmacDataTraceViewer

### Spec

- [docs/spec.md](docs/spec.md)

### License

MIT License. See `LICENSE`.

---

## 日本語

### 概要

SysmacDataTraceViewer は、Windows 上で Sysmac Studio の Data Trace CSV を可視化するツールです。  
`:BOOL` 信号は ON/OFF のタイムラインとして表示し、その他の型はカーソル位置の値として表示します。

![Sysmac Data Trace Viewer](Sysmac%20Data%20Trace%20Viewer.png)

### 現在のバージョン

- `1.0.2`

### 主な機能

- BOOL タイムライン（複数レーンの ON/OFF 表示）
- 主カーソル + 差分カーソル
- BOOL 変数 / 値変数の表示切り替え
- 変化なし変数の非表示（`[No Change]`）
- BOOL/値リストのドラッグ&ドロップ並び替え
- ラベル表示切り替え（`Variable Name` / `Comment`）
- BOOL ごとの色設定（`#RRGGBB`）
- PNG 出力（表示範囲 / 全範囲）
- コメント CSV の読み込み/保存（コメント・表示・色・順序）

### 最近の更新

- BOOL 名レーンの `Value` 列に、カーソル位置の `0/1` を表示。
- 時間チャート上で BOOL 信号にホバーした際、信号名つき状態表示（例: `Signal_X: ON`）に対応。
- バージョンを `1.0.2` に更新。

### 対応 CSV 信号型

- タイムライン: `:BOOL`
- 値パネル: `:INT`, `:BYTE`, `:WORD`, `:DWORD`, `:LWORD`, `:SINT`, `:DINT`, `:LINT`, `:USINT`, `:UINT`, `:UDINT`, `:ULINT`, `:REAL`, `:LREAL`, `:TIME`, `:DATE`, `:TIME_OF_DAY`, `:DATE_AND_TIME`

### ビルド

```powershell
dotnet restore
dotnet build .\SysmacDataTraceViewer.sln -c Release
dotnet run --project .\src\SysmacDataTraceViewer\SysmacDataTraceViewer.csproj
```

### 配布（単一 EXE）

```powershell
dotnet publish .\src\SysmacDataTraceViewer\SysmacDataTraceViewer.csproj `
  -c Release -r win-x64 `
  -p:PublishSingleFile=true `
  -p:SelfContained=true `
  -o .\dist\win-x64
```

### リポジトリ

- GitHub: https://github.com/fa-yoshinobu/SysmacDataTraceViewer

### 仕様書

- [docs/spec.md](docs/spec.md)

### ライセンス

MIT License。`LICENSE` を参照。
