# 山頂から麓へ下るインゲームコース

雨、シアン／マゼンタのネオン、山腹と麓の街並みを残した下りコース。
`KartCourseLayout` が走行距離に対する標高・旋回角・トンネル位置を定義する。
標準の全長1,850mでは落差約350m。全長を変更しても下り80%／平坦20%の比率を維持する。

| コース内の位置 | 地形 |
| --- | --- |
| 0–4% | 山頂の平坦なスタート |
| 4–29% | 山腹の下りとカーブ |
| 29–35% | ジャンプ台周辺の平坦区間 |
| 35–46% | ギャラリーを通る下り |
| 46–50% | 山腹の平坦区間 |
| 50–94% | 長いカーブ、短いトンネル、左右180度ヘアピンを含む下り |
| 94–100% | 麓の平坦なゴール前 |

勾配の切り替えは全長の1%を使って滑らかにつなぐ。ジャンプ台そのものは既存の上向き踏み切りを維持する。
ロングカーブは50–61.25%、トンネルは54–59%。ヘアピンは68.33–71.67%と75–78.33%に配置する。
山道の外側はガードレールと山肌／谷、街並み区間は従来の建物と看板で構成する。
CPUの区間別速度やレース判定は従来の設定を維持する。

## Unityでの確認

通常のレース開始時にコースを生成するため、シーンの再保存は不要。
任意の地点だけを確認する場合は、通常のレースを開始していないPlay Modeで以下を実行する。

```bash
./Tools/unity-cli.sh command editor_play
./Tools/unity-cli.sh command run_script --file AgentScripts/InspectDownhillCourse.cs --entry InspectDownhillCourse.Preview --args '[0.69]'
./Tools/unity-cli.sh command capture_game_view --camera RaceCamera --width 1280 --height 720 --save_path .utmp/downhill-preview.png
./Tools/unity-cli.sh command editor_stop
```

`Preview` の引数はコース進捗（0–1）。0.12で山腹、0.515で長いカーブ、0.69と0.765で左右のヘアピン、0.97で麓を確認できる。
検証用ステージはPlay Modeを終了すると破棄される。キャプチャはPipelineにより `Assets/.utmp/` へ保存される。

```bash
./Tools/unity-cli.sh command recompile
./Tools/unity-cli.sh command recompile_status
./Tools/unity-cli.sh command run_tests --mode editor --filter MixVerse.Game.Model.Tests.KartCourseLayoutTests --filter_type testName
```

テストでは下り比率、上りの不在、標高の連続性、ジャンプ・ゴール前の平坦さ、下りヘアピン、ロングカーブの旋回率を確認する。
