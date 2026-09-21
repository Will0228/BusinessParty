# セットアップ手順

clone 直後のプロジェクトを、他のメンバーと同じ状態で開くための手順をまとめる。

## 必要なもの

| 項目 | バージョン / 条件 |
| --- | --- |
| Unity | 6000.3.23f1（`ProjectSettings/ProjectVersion.txt` と同じもの） |
| git | PATH に通っていること。Package Manager が git URL のパッケージを取得するために使う |
| GitHub への接続 | R3 / UniTask / VContainer / NuGetForUnity / Minis / rtmidi を git URL から取得する |

## 手順

1. リポジトリを clone する。
2. Unity Hub から 6000.3.23f1 で開く。初回は git URL パッケージの取得とインポートで時間がかかる。
3. `Window > TextMeshPro > Import TMP Essential Resources` を実行する。
4. Asset Store のアセットを自分のアカウントから import する（[手動 import が必要なもの](#手動-import-が必要なもの)）。
5. `MixVerse > Setup > セットアップ状況を確認` で不足がないか確認する。

## git だけで揃うもの（作業不要）

- **UPM パッケージ**: `Packages/manifest.json` + `Packages/packages-lock.json` で解決する。registry のものも git URL のものもバージョンは固定済み。
- **NuGet パッケージ**: `Assets/packages.config` に対応する DLL を `Assets/Packages/` ごと追跡している。
  NuGetForUnity の復元はコンパイルより後に走るため、追跡していないと clone 直後の初回インポートで
  `Packages/com.cysharp.r3` が `Observable<>` / `Subject<>` を見つけられず CS0246 で大量に失敗する。
  パッケージを増減させたときは `Assets/Packages/` の差分も一緒にコミットすること。

## 手動 import が必要なもの

### TextMeshPro エッセンシャルリソース

`Assets/TextMesh Pro/` は追跡対象外。`Window > TextMeshPro > Import TMP Essential Resources` で復元する。

未 import だと次の参照が切れる。

- `Assets/Scenes/SampleScene.unity` の TextMeshPro テキスト（LiberationSans SDF）
- `Assets/Fonts/BusinessPartyJapanese.asset`（TMP_SDF-Mobile シェーダー）
- `Assets/Shaders/TMP/TextMeshPro_URP_GlitchShader.mat`

### Asset Store のアセット

サイズとライセンスの都合で追跡していない。**各自が自分のアカウントから同じ版を import する。**

| import 先フォルダ | 未 import で起きること |
| --- | --- |
| `Assets/HIVEMIND` | `Assets/Materials/Room/*.mat` のシェーダーが見つからず、ステージのマテリアルがピンクになる |
| `Assets/Monster` | `Assets/Prefabs/CPUs.prefab` のネスト Prefab（Giant / Monster Skin1）が欠落する |
| `Assets/MonsterMutant 7` | CPU まわりのモデルが欠落する |
| `Assets/Rallba` | CPU まわりのモデルが欠落する |

import 先のフォルダ名は上記から変えないこと。変えると GUID は保たれても .meta のパスが変わり、
他のメンバーの環境と差分が出る。

## 既知の未解決参照（対応不要）

以下は clone の失敗ではなく、元アセットやベイク結果が最初から含まれていないもの。警告が出ても無視してよい。

- `Assets/SimpleHands/Prefabs/*.prefab` の AnimatorController
- `Assets/Ishikawa1116/.../DemoScene_sample.unity` の LightingDataAsset

## うまくいかないとき

### R3 まわりで CS0246 が大量に出る

`Assets/Packages/R3.1.3.1/` があるか確認する。無ければ
`NuGet > Restore Packages`（NuGetForUnity）を実行してから Unity を再起動する。

### パッケージの取得に失敗する

git URL の解決に失敗している。ターミナルで `git ls-remote https://github.com/Cysharp/R3.git` が通るか確認し、
通るなら `Packages/packages-lock.json` を残したまま Unity を再起動する。

### インポート結果がおかしい

`Library/` を削除して Unity を開き直すとインポートをやり直せる。`Library/` は追跡対象外なので消して問題ない。
