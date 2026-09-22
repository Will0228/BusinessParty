# AGENTS.md

## 実装方針

- 実装はMVPパターンをメインに実装する
  - Presenterはインタフェースを継承する
  - Viewはインタフェースを継承しない
- 無駄なstaticメソッドは作成しない
  - Helperクラスなどのstaticメソッドは許可する
  - ただし基本的にはDIしてUtilityクラスを作るようにする
- 実装したらコミットまで対応する
- コメント入力は基本不要
  - 中身の処理や関数を見て客観的に分からないと判断した場合はコメントを記載してもよい
- 購読処理を行う場合はSetEvent, Bindメソッド内で行う
  - SetEvent：その関数の中で完結する処理（同クラス内のメソッド呼び出しなど）
  - Bind：他クラスのメソッドなどを呼ぶ場合

## 検証

- 起動中の Unity Editor は `unity` CLI から操作する（`./Tools/unity-cli.sh command ...`）
- C# を編集したら `command recompile` → `command recompile_status` → `command run_tests` で確認する
- 手順とコマンドは [Docs/UnityPipeline.md](Docs/UnityPipeline.md) を参照
