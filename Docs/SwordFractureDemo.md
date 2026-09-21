# 剣の衝撃による破片分解デモ

`Assets/Scenes/SampleScene.unity` を開いて Play するとデモが表示されます。

- ゲージ `IMPACT` をドラッグ：0 は接触前、1 は全破片の飛散完了。途中停止・巻き戻しが可能です。
- `Play slash`：0 に戻して約 2.5 秒で剣の攻撃を再生します。
- `Reset / 0`：形状と剣を初期状態に戻します。
- `Close demo`：既存シーンの表示に戻ります。右上の `Open sword fracture demo` で再表示します。
- Play 中の `SwordFractureDemo` の Inspector の Progress でも操作できます。

## 実装

Shader 方式を採用しています。Naive Surface Nets は使用していません。
Cube と Sphere はそれぞれ 6 × 6 × 6 = 216 個の閉じた破片からなるメッシュです。
Sphere は格子を球状へ写像して生成し、表面の法線を補間します。断面はオレンジ色です。
頂点の UV1 に破片の中心を持たせ、Shader が破片単位で移動・回転します。
剣の斜めの移動に合わせて、左上の破片から順に分解が始まります。
固定の乱数と進捗のみで位置を決めるため、ゲージを戻すと同じ形に復元できます。

`FractureDemoPresenter : IFractureDemoPresenter` が進捗と再生状態を管理し、
`FractureDemoView` がUI・剣・プレビューを、`FractureObjectView` がオブジェクトの描画を担当します。
View はインタフェースを継承しません。

独立したプレビューカメラと RenderTexture を使い、既存シーンのカメラ設定は変更しません。
デモのオブジェクトは座標 (1000, 1000, 1000) 付近のレイヤー30に配置します。
デモを再配置する場合は `MixVerse > Setup > Create Sword Fracture Demo` を使います。

## ゲームへの組み込み

`FractureObjectView.SetProgress(float)` に 0〜1 の値を渡すと分解状態を設定できます。
実際の剣のヒット判定から、時間に応じてこの値を更新してください。
`SwordFractureURP` のマテリアルと、このコンポーネントが生成する専用メッシュを組み合わせて使用します。
通常の Cube/Sphere メッシュにマテリアルを貼るだけでは破片の中心データがないため分解しません。

これは描画上の破壊表現です。破片の Rigidbody、物理衝突、任意の切断面生成は実装していません。
Shader は固定の照明で断面を見やすく表示し、影の投射は行いません。
Shader 変形を考慮してメッシュの描画境界を広げています。

## 検証

Test Runner の EditMode で `MixVerse.FractureTests` を実行します。
Cube/Sphere の破片の閉鎖性、面の向き、球表面の半径、および Shader のコンパイルを検証します。
