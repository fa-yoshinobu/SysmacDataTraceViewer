# SysmacDataTraceViewer

[![Release](https://img.shields.io/github/v/release/fa-yoshinobu/SysmacDataTraceViewer?display_name=tag)](https://github.com/fa-yoshinobu/SysmacDataTraceViewer/releases/latest)
[![CI](https://img.shields.io/github/actions/workflow/status/fa-yoshinobu/SysmacDataTraceViewer/ci.yml?branch=main&label=CI)](https://github.com/fa-yoshinobu/SysmacDataTraceViewer/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/download/dotnet/10.0)

Windows上でSysmac StudioのData Trace CSVを確認するWPFアプリです。BOOL信号をON/OFFのタイムラインとして表示し、その他の信号はカーソル位置の値を表示します。

![Sysmac Data Trace Viewer](Sysmac%20Data%20Trace%20Viewer.png)

## ダウンロード / Download

[最新版 Windows x64 ZIP](https://github.com/fa-yoshinobu/SysmacDataTraceViewer/releases/latest/download/SysmacDataTraceViewer-win-x64.zip) · [SHA-256](https://github.com/fa-yoshinobu/SysmacDataTraceViewer/releases/latest/download/SysmacDataTraceViewer-win-x64.zip.sha256)

ZIPを展開して`SysmacDataTraceViewer.exe`を起動します。.NETランタイムは同梱しています。最新の公開版は冒頭のReleaseバッジ、変更内容は[変更履歴](CHANGELOG.md)を参照してください。

## 主な機能

- BOOL信号の複数レーン表示、主カーソルと差分カーソル、変化点への移動
- カーソル位置のBOOL値・数値・日付・時刻などの表示と、BOOL状態・継続時間のホバー表示
- 信号の表示切替、変化しない信号の非表示、リストのドラッグによる並び替え
- 変数名／コメントのラベル切替と、BOOLごとの色設定
- 表示範囲／全範囲のPNG出力
- コメント・表示・色・順序のコメントCSV保存と読込み

対応する信号型、操作、ショートカットは[仕様書](docs/spec.md)を参照してください。

## 開発・保守

[開発・ビルド・CI・リリース手順](docs/development.md) · [大容量CSVの計測](docs/large-csv-benchmark.md) · [TODO](TODO.md)

## English

A Windows WPF viewer for Sysmac Studio Data Trace CSV files. BOOL signals appear as ON/OFF timeline lanes; other typed signals are shown at the cursor. It supports primary/delta cursors, signal ordering, visibility, colors, comment CSV files, and PNG export.

Use the ZIP and checksum links above, extract the ZIP, and run `SysmacDataTraceViewer.exe`. The .NET runtime is included. See the [specification](docs/spec.md) for supported types and shortcuts, and the [changelog](CHANGELOG.md) for updates.

## ライセンス / License

[MIT License](LICENSE)
