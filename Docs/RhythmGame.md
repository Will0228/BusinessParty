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

## 譜面エディタ

`MixVerse > Rhythm > 譜面エディタ` で開く。Play は不要で、編集結果は `NoteChartAsset`
（`Assets/Create > MixVerse > Note Chart` でも作れる ScriptableObject）に保存する。

```
        ┌─── 左（L） ───┬─── 右（R） ───┐
   1    │       ▬       │               │  ← 小節の頭は黄色い線
        │               │       ▬       │
        │       ▬       │               │
   2    │               │               │
        ：              ：              ：
   4    │               │       ▬       │
        └───────────────┴───────────────┘
              ▲ 前の 4 小節 / ▼ 次の 4 小節
```

- 小節は縦に並べ、4 小節を 1 セットとして 1 画面に収める。行の高さは画面に合わせて詰める
- セットの切り替えは矢印ボタン。`3 - 4 セット` のように今どこを見ているかを出す
- 列は 2 つだけで、左右それぞれ別の要素として扱う。セルをクリックで置く／消す
- 分解能は 4 分 〜 32 分。変えても鳴る位置が動かないよう、ステップを割り当て直す
- 小節数を減らすと、外へ出たノーツは捨てる
- `下が先（落ちてくる向き）` を押すと、サウンドボルテックスのように下から上へ読む並びになる

| 層 | クラス | 役割 |
| --- | --- | --- |
| Presenter | `ChartEditorPresenter` | 編集中の譜面・ページ・保存状態の取りまとめ |
| View | `ChartEditorWindow` | ウィンドウの組み立てと入力の受け取り |
| View | `ChartGridDrawer` | 1 セットぶんの格子とノーツの描画、クリックされたセルの解決 |
| Utility | `ChartAssetRepository` | 譜面アセットの作成と保存 |
| Model | `ChartGrid` / `ChartPager` / `EditableNoteChart` | 格子の数え方・ページ・ノーツの置き方 |
| Asset | `NoteChartAsset` | BPM・拍子・分解能・小節数とノーツの保存先 |

ノーツは秒ではなくステップ（1 拍を分解能で割った長さの通し番号）で持ち、
秒が要るときは `ChartGrid.TimeOfStep(BeatClock, int)` で求める。

インゲームはまだ `NoteChart` の 4 分の自動生成を使っているので、
この譜面アセットを読んで流すには 2 列ぶんの入力と判定を足す必要がある。

## 検証

Test Runner の EditMode で `MixVerse.Game.Model.Tests` を実行する。
拍・判定・スコアの規則に加えて、譜面の格子・ページ送り・ノーツの置き方を検証する。
