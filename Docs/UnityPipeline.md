# Unity CLI / Pipeline

起動中の Unity Editor をターミナルから操作するための手順。`com.unity.pipeline`（`Packages/manifest.json` に
入っている）が Editor 内に HTTP サーバーを立てており、`unity` CLI がそこへコマンドを投げる。
Editor を開いたままコンパイル・テスト・シーン操作ができるので、Unity の GUI を触りに行かずに検証を回せる。

## セットアップ

CLI はリポジトリの外（ユーザーのホーム）に入る。各自が一度だけ実行する。

```bash
curl -fsSL https://public-cdn.cloud.unity3d.com/hub/prod/cli/install.sh | UNITY_CLI_CHANNEL=beta bash
```

インストール先は `~/.unity/bin/unity`。インストーラが `~/.zshrc` に `. "$HOME/.unity/env"` を追記するので、
シェルを開き直せば `unity` が PATH に乗る。開き直さずに使うときはリポジトリの `Tools/unity-cli.sh` を通す。

```bash
./Tools/unity-cli.sh command editor_status
```

このラッパーは `command` サブコマンドに `--project-path`（このリポジトリ）を自動で付ける。
別プロジェクトの Editor が同時に起動していると CLI が対象を取り違えるので、素の `unity command` を使う場合は
自分で `--project-path` を付けること。

## 接続確認

```bash
unity pipeline list        # 起動中の Editor と Pipeline サーバーの疎通
unity command              # その Editor が公開しているコマンド一覧
unity command editor_status
```

`Server Reachable` が空なら Editor が落ちているか Safe Mode。Safe Mode は C# のコンパイルエラーで入るため、
まずエラーを直して Unity を再起動する。

## 編集 → コンパイル → テストのループ

```bash
# 1. 非アクティブでも Editor を回し続ける（これを忘れるとコンパイルが止まる）
unity command set_autotick --enable true

# 2. C# をいつも通り編集する

# 3. コンパイル（非同期。完了までポーリングする）
unity command recompile
unity command recompile_status        # "completed" / "up_to_date" になるまで繰り返す

# 4. テスト（フィルタを効かせる。52 件フルで回すと時間がかかる）
unity command list_tests --mode editor
unity command run_tests --mode editor --filter MixVerse.Game.Model.Tests.RhythmRulesTests --filter_type testName
```

`--filter_type` は `testName` / `assembly` / `category`。アセンブリ単位なら次の 3 つ。

| アセンブリ | 中身 |
| --- | --- |
| `MixVerse.Game.Model.Tests` | 譜面編集・リズム判定・カートレースのルール |
| `MixVerse.Fracture.Editor.Tests` | 破壊メッシュ生成 |
| `MixVerse.Seal.Editor.Tests` | シール剥がしシェーダー |

長いテストは `--async_tests true` にして `unity command test_status` でポーリングし、
`unity command cancel_tests` で止める。

## そのほかよく使うもの

```bash
unity command eval 'return UnityEditor.EditorApplication.isPlaying;'
unity command run_script --file AgentScripts/Build.cs --entry Build.All   # まとまった構築処理
unity command editor_play / editor_stop
unity command capture_game_view
unity command console                 # Editor のコンソールログ
```

`run_script` に渡すファイルは `Assets/` の外に置く。`Assets/` 配下に書くとアセットインポートとドメインリロードが
走ってしまい、速さの意味がなくなる。

## 終了コード

| 終了コード | 意味 | 対応 |
| --- | --- | --- |
| `2` | 引数が不正。何も実行されていない | コマンドラインを直して再実行する |
| `6` | 実行されたが失敗した、または Editor が処理できなかった | エラーを読む。同じコマンドの再実行は無意味 |

## うまくいかないとき

- **コマンドが返ってこない**: Editor でモーダルダイアログが開いている可能性がある。`unity command editor_status` は
  ブロック中でも即答するので、`status: "blocked_by_dialog"` なら CLI 側では押せない。Unity 側で閉じる。
- **`AMBIGUOUS_EDITOR`**: Editor が複数起動している。`--project-path` を付けるか `Tools/unity-cli.sh` を使う。
- **接続できない**: `Library/Pipeline/.unity-pipeline-port` があるか確認する。無ければ Editor 側でサーバーが
  起動していない（Safe Mode か、パッケージが解決できていない）。

## エージェント向けスキル

`.claude/skills/unity-cli` と `.claude/skills/unity-pipeline` を追跡している。`unity skill install claude-code --local`
が生成したもので、CLI やパッケージを更新したときは同じコマンドで貼り直す。
