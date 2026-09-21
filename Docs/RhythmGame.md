# インゲーム（リズムゲーム）

接待ゲーム（先輩・後輩のトークに DJ コントローラーで応える形式）を廃止し、
インゲームを 3D のリズムゲームに差し替えた。

## 画面

俯瞰視点で、画面の下の方にプレイヤー、その左右に CPU が並ぶ。
キャラクターは一旦 Capsule で、ノーツは画面の奥（上）から手前の判定ラインへ流れてくる。

```
        ┌──────── 奥（ノーツの発生位置） ────────┐
        │              ▮  ← ノーツ               │
        │              ▮                         │
        │              ▮                         │
        ├────────── 判定ライン ──────────────────┤
        │   ◍ CPU      ◍ Player      ◍ CPU       │
        └──────── 手前（カメラ側） ──────────────┘
```

- BPM 130 の 4 分（1 拍に 1 つ）で途切れずノーツが流れる
- ノーツは `noteLeadBeats` 拍ぶんかけて発生位置から判定ラインへ着く
- 拍ごとにメトロノームが鳴り、左右の CPU が跳ねる

## 操作

| キー | 動作 |
| --- | --- |
| Space / F / J | ノーツを叩く |
| Esc | ホームへ戻る |

判定は Perfect / Good / Miss の 3 段階。叩く時刻とノーツの到達時刻のズレで決まる。

## 構成

MVP で、拍・譜面・判定・スコアの規則は Unity に依存しない純粋な C#（`MixVerse.Game.Model`）に置く。

| 層 | クラス | 役割 |
| --- | --- | --- |
| Controller | `GameController` | 曲の進行ループと画面遷移 |
| Presenter | `GamePresenter` | ノーツの発生・判定・スコアの取りまとめ |
| View | `GameView` | 入力の受け取りとステージの生成・破棄 |
| View | `RhythmStageFactory` | 俯瞰ステージ（カメラ・Capsule・レーン・HUD）を実行時に組み立てる |
| Model | `BeatClock` / `NoteChart` / `JudgementTable` / `ScoreBoard` | BPM・譜面・判定・スコア |

ステージは実行時に生成するので、シーンに置くのは `GameView` を付けた `GameScreen` だけでよい。
部屋のオブジェクトと重ならないよう、ステージは `stageOrigin`（既定で y = 1000）に建て、
プレイ中は部屋側のカメラ・AudioListener・各 Tester を止める。

## 調整値

`GameScreen` の `GameView` の Inspector で触る。BPM・判定幅・レーンの長さ・カメラの高さと角度など。

## clone 後 / 差し替え直後の手順

`MixVerse > Setup > Rebuild Rhythm Game Screen` を一度実行すると、
古い接待ゲームの `GameScreen` をシーンから取り除き、リズムゲーム用の `GameScreen` を置き直して
`UndisposableLifetimeScope` に繋ぎ直す。実行後はシーンが保存される。
