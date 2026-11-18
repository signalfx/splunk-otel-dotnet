#!/usr/bin/env bash

bash --version 2>&1 | head -n 1

set -eo pipefail
SCRIPT_DIR=$(cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd)
cd "$SCRIPT_DIR"

###########################################################################
# CONFIGURATION
###########################################################################

TEMP_DIRECTORY="$SCRIPT_DIR/.fallout/temp"

DOTNET_GLOBAL_FILE="$SCRIPT_DIR/global.json"
DOTNET_INSTALL_FILE_DEFAULT="$SCRIPT_DIR/scripts/dotnet-install.sh"
DOTNET_INSTALL_URL="https://dot.net/v1/dotnet-install.sh"
DOTNET_CHANNEL="STS"

export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_NOLOGO=1

###########################################################################
# EXECUTION
###########################################################################

function FirstJsonValue {
    perl -0777 -ne 'print $1 if m{"'"$1"'"\s*:\s*"([^"]+)"}' <<< "${@:2}"
}

# If dotnet CLI is installed globally, use it; otherwise provision a local copy under .fallout/temp.
if [ -x "$(command -v dotnet)" ] && dotnet --version &>/dev/null; then
    DOTNET_EXE="$(command -v dotnet)"
    export DOTNET_EXE
else
    # Use local install script (preferred) or explicit override
    DOTNET_INSTALL_FILE="${DOTNET_INSTALL_FILE:-$DOTNET_INSTALL_FILE_DEFAULT}"
    if [[ ! -f "$DOTNET_INSTALL_FILE" ]]; then
        echo "dotnet installer script not found: $DOTNET_INSTALL_FILE" >&2
        echo "Place a trusted copy at scripts/dotnet-install.sh (or set DOTNET_INSTALL_FILE)." >&2
        echo "Reference URL for obtaining the script: $DOTNET_INSTALL_URL" >&2
        exit 1
    fi
    if [[ -f "$DOTNET_GLOBAL_FILE" ]]; then
        DOTNET_VERSION=$(FirstJsonValue "version" "$(cat "$DOTNET_GLOBAL_FILE")")
        if [[ "$DOTNET_VERSION" == ""  ]]; then
            unset DOTNET_VERSION
        fi
    fi

    DOTNET_DIRECTORY="$TEMP_DIRECTORY/dotnet-unix"
    if [[ -z ${DOTNET_VERSION+x} ]]; then
        bash "$DOTNET_INSTALL_FILE" --install-dir "$DOTNET_DIRECTORY" --channel "$DOTNET_CHANNEL" --no-path
    else
        bash "$DOTNET_INSTALL_FILE" --install-dir "$DOTNET_DIRECTORY" --version "$DOTNET_VERSION" --no-path
    fi
    export DOTNET_EXE="$DOTNET_DIRECTORY/dotnet"
    export DOTNET_ROOT="$DOTNET_DIRECTORY"
    export PATH="$DOTNET_DIRECTORY:$PATH"
fi

echo "Microsoft (R) .NET SDK version $("$DOTNET_EXE" --version)"

"$DOTNET_EXE" tool restore
exec "$DOTNET_EXE" fallout "$@"
