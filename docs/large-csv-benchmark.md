# 大容量CSVの計測

2026-11-12、AMD Ryzen AI MAX+ 395、.NET SDK 10.0.202のWindows環境・Releaseビルドで計測。設備データは使わず、20万サンプル・BOOL 50信号・値20信号（37,690,431 bytes）を生成した。各BOOLは約1,000サンプルごとに変化し、値は連続して同じ文字列になる。測定値はこの架空データの単回測定であり、すべてのCSVで同じ時間になる保証ではない。

## 再現手順

```powershell
python tools/generate_benchmark_csv.py "$env:TEMP\SysmacViewer-large-fictional.csv"
dotnet run --project tools/TraceBenchmark/TraceBenchmark.csproj -c Release -- "$env:TEMP\SysmacViewer-large-fictional.csv" "$env:TEMP\SysmacViewer-chart.png"
```

測定プログラムは解析・描画モデル作成・1800×900 PNG描画を別々に計測し、GC後の保持メモリとプロセスの最大ワーキングセットをJSONで出力する。

| 項目 | 変更前 | 変更後 |
| --- | ---: | ---: |
| 解析 | 1,489 ms | 941 ms |
| モデル作成 | 120 ms | 37 ms |
| 初回PNG描画 | 1,077 ms | 730 ms |
| 合計 | 2,686 ms | 1,708 ms |
| 解析後の保持メモリ | 200,886,536 bytes | 66,611,376 bytes |
| モデル作成後の保持メモリ | 410,635,240 bytes | 66,848,432 bytes |
| 最大ワーキングセット | 866,168,832 bytes | 245,526,528 bytes |
| 描画点 | 10,000,000 | 10,099 |

この環境での目標は、解析2秒以内、モデル作成と初回PNG描画の合計1秒以内、全体2.5秒以内、最大ワーキングセット350MiB以内とした。上記計測ではすべて満たした。

変更前後の1800×900 PNGは全ピクセルが一致した。実MainWindowによる小さなShift_JISデータの表示範囲・全範囲PNGも一致した。単体テストでは、欠損値を含む6サンプルの全729パターンで、全点の階段線と削減後の線が同じ値になることを確認する。全サンプルの配列は維持し、カーソル・ホバー・変化点移動の入力には削減点を使わない。

UI検証では100万サンプルを読み込みながらDispatcherタイマーが最大37ms間隔で応答し、300万サンプルの読込み途中で実際のCancelボタンから中止できた。中止時は前のデータと表示を維持し、操作が再び有効になった。大きい検証データも同じ生成スクリプトの`--samples`で再現できる。
