# 開発・ビルド・リリース

## 必要環境

Windowsと.NET 10 SDKを使用します。製品バージョンは`src/SysmacDataTraceViewer/SysmacDataTraceViewer.csproj`の`Version`で管理し、AssemblyVersion・FileVersion・製品バージョンはSDKが導出します。コミットIDは製品バージョンに付加しません。設定形式や依存パッケージの版とは別に管理します。

## 共通検証

```powershell
.\ci.bat
python -B -m unittest discover -s tests/release
```

`ci.bat`は復元、警告をエラーにしたReleaseビルド、単体テスト、`dotnet format`の書式・スタイル・解析確認、単体EXE発行確認を順に行います。mainへのpushとPRでも同じバッチを実行します。

## 単体EXEの発行

```powershell
.\build.bat
.\build.bat win-x64 artifacts/publish
```

既定の出力先は`artifacts/publish/SysmacDataTraceViewer.exe`です。ランタイム同梱のWindows x64用です。大容量CSVの計測手順は[計測記録](large-csv-benchmark.md)を参照してください。

## リリース

1. 共通検証を行い、CSV表示・カーソル・コメント保存/読込み・PNG出力を確認する。
2. csprojの`Version`と`CHANGELOG.md`を更新し、該当変更から`.github/release-notes.md`の英語Changes節を作成する。
3. 変更をコミット・pushし、同じ版の`vMAJOR.MINOR.PATCH`形式のタグをpushする。旧形式の既存タグは変更しない。
4. `Release Single EXE`がビルド・単体テストに成功してから、固定名ZIPとZIPのSHA-256を公開する。
5. 公開ZIPの内容・起動・EXEバージョン・チェックサム一致を確認する。公開物を検査する`Scan release with VirusTotal`の解析完了と本文への結果リンク掲載も確認する。

未完了のVirusTotal検査は、`Scan release with VirusTotal`へ公開タグを指定して再実行できます。旧タグ向けの互換入口`Rescan release with VirusTotal`も維持しています。既存リリースの配布物を上書きして修正せず、新しい版を発行します。

リリース本文とVirusTotal CIの表示は英語、READMEやCHANGELOGは既存の言語方針を維持します。詳細は[共通リリース仕様](https://github.com/fa-yoshinobu/release_standard)を参照してください。

## Phase 4 実施状況（2026-11-12）

既存の `docs/phase4-refactor-plan.md` は作業開始時から削除されていたため、復元せず実施状況だけをここに記録する。

| 手順 | 状態 |
| --- | --- |
| PlotModel構築の分離 | 済み（TracePlotModelBuilder） |
| ダイアログの差し替え | 済み（IDialogService、TraceFileOperations） |
| カーソル状態の分離 | 済み（CursorState）。画面座標と注釈の接続はMainWindowに残す |
| 操作のコマンド化 | 済み。読込み・コメント・PNGの段取りはサービス、信号の状態・操作はViewModelへ移動 |
| XAMLの状態バインド | ステータス・読込み状態・表示行・操作コマンドは済み。GridのサイズとOxyPlotの操作接続は画面側に残す |

名前レーンの表示処理は専用の `Views/NameLanePresenter` にまとめ、信号選択をMainWindowへ通知する。画面のレイアウトは変えない。

## 同梱ランタイムとPNG

同梱.NETランタイムはcsprojのRuntimeFrameworkVersionで固定する。`python .github/scripts/check_runtime.py` がMicrosoftの公式リリース情報を照合し、新しいパッチがあれば失敗する。週次と手動のGitHub Actionsでも実行する。更新は固定値の変更・共通検証・新しいアプリの公開の順で行い、自動マージや自動リリースは行わない。

PNGは1800×900を維持する。既存の保存結果を再現でき、表示範囲と全範囲の比較にも使うため、今回はサイズ指定の設定を追加しない。

コメントCSVは同じディレクトリの一時ファイルを書き終えてから置き換える。引用符内のCR/LFを保持し、複数行のコメントも読み戻す。
