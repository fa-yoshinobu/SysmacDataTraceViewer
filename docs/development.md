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
