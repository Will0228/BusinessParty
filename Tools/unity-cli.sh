#!/usr/bin/env bash
# unity CLI を PATH に載せてから引数をそのまま渡す。
# シェルを開き直さずに使いたい場合や、PATH が通っていない CI から呼ぶための入口。
# command サブコマンドにはこのリポジトリを --project-path として補う。
set -euo pipefail

UNITY_CLI_BIN="${UNITY_CLI_HOME:-$HOME/.unity}/bin/unity"

if [ ! -x "$UNITY_CLI_BIN" ]; then
  if command -v unity >/dev/null 2>&1; then
    UNITY_CLI_BIN="$(command -v unity)"
  else
    cat >&2 <<'MSG'
unity CLI が見つからない。次のコマンドでインストールする。

  curl -fsSL https://public-cdn.cloud.unity3d.com/hub/prod/cli/install.sh | UNITY_CLI_CHANNEL=beta bash

詳細は Docs/UnityPipeline.md を参照。
MSG
    exit 127
  fi
fi

PROJECT_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

if [ $# -gt 0 ] && [ "$1" = "command" ]; then
  shift
  exec "$UNITY_CLI_BIN" command --project-path "$PROJECT_ROOT" "$@"
fi

exec "$UNITY_CLI_BIN" "$@"
