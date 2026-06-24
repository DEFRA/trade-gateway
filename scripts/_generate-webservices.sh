#!/usr/bin/env bash
set -euo pipefail

# Minimal shared helper to generate webservice client classes using dotnet-svcutil.
# Usage: _generate-webservices.sh <wsdl-path> <params-dir> <output-dir>
# This intentionally keeps behaviour minimal and matches existing scripts.

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
WSDL_SRC="${1:-}"
PARAMS_DIR="${2:-$SCRIPT_DIR}"
WEBSERVICES_DIR="${3:-$REPO_ROOT/src/TracesNT/WebServices}"
PROJECT_FILE="$REPO_ROOT/src/TracesNT/TracesNT.csproj"

if [[ -z "$WSDL_SRC" ]]; then
    echo "Usage: $0 <wsdl-path> <params-dir> <output-dir>" >&2
    exit 2
fi

if [[ ! -f "$PARAMS_DIR/dotnet-svcutil.params.json" ]]; then
    echo "Required params file not found: $PARAMS_DIR/dotnet-svcutil.params.json" >&2
    exit 1
fi

if [[ ! -f "$PROJECT_FILE" ]]; then
    echo "Required file not found: $PROJECT_FILE" >&2
    exit 1
fi

if [[ ! -d "$WEBSERVICES_DIR" ]]; then
    echo "Required directory not found: $WEBSERVICES_DIR" >&2
    exit 1
fi

STAGING_DIR="$(mktemp -d "$SCRIPT_DIR/tracesnt-webservices.XXXXXX")"
GENERATED_FILE="$PARAMS_DIR/TracesClients.cs"

cleanup() {
    rm -rf "$STAGING_DIR"
}
trap cleanup EXIT

# Read expected input filename from params.json (first inputs entry)
INPUT_NAME=$(sed -n 's/.*"inputs"[[:space:]]*:[[:space:]]*\[[[:space:]]*"\([^\"]*\)".*/\1/p' "$PARAMS_DIR/dotnet-svcutil.params.json" || true)
if [[ -z "$INPUT_NAME" ]]; then
    INPUT_NAME="$(basename "$WSDL_SRC")"
fi

# Ensure the WSDL exists either as an absolute path or inside the params dir
if [[ -f "$WSDL_SRC" ]]; then
    :
elif [[ -f "$PARAMS_DIR/$WSDL_SRC" ]]; then
    :
else
    echo "WSDL file not found: $WSDL_SRC" >&2
    exit 1
fi

# Run dotnet-svcutil using the params directory directly (do not copy params.json)
echo "Regenerating WCF WebServices from $PARAMS_DIR/dotnet-svcutil.params.json..."
set +e
SVCUTIL_OUTPUT="$(
    dotnet dotnet-svcutil -u "$PARAMS_DIR" --projectFile "$PROJECT_FILE" 2>&1
)"
SVCUTIL_EXIT=$?
set -e

if [[ -n "$SVCUTIL_OUTPUT" ]]; then
    printf '%s\n' "$SVCUTIL_OUTPUT"
fi

if [[ "$SVCUTIL_EXIT" -ne 0 && ! -f "$GENERATED_FILE" ]]; then
    echo "dotnet-svcutil failed before producing $GENERATED_FILE" >&2
    exit "$SVCUTIL_EXIT"
fi

# Split generated file
dotnet tool run dotnet-script "$SCRIPT_DIR/FileSplitter.csx" -- "$GENERATED_FILE" "$STAGING_DIR"

if [[ -z "$(find "$STAGING_DIR" -name "*.g.cs" -print -quit)" ]]; then
    echo "FileSplitter did not produce any generated files in $STAGING_DIR" >&2
    exit 1
fi

# Replace generated files at destination
find "$WEBSERVICES_DIR" -maxdepth 1 -name "*.g.cs" -delete
find "$WEBSERVICES_DIR" -mindepth 1 -maxdepth 1 -type d -exec rm -rf {} +
cp -R "$STAGING_DIR"/. "$WEBSERVICES_DIR"/

# Remove the intermediate combined generated file if present (cleanup after successful run)
if [[ -f "$GENERATED_FILE" ]]; then
    rm -f "$GENERATED_FILE" && echo "Removed generated file $GENERATED_FILE"
fi

echo "Done! WebServices types have been regenerated and replaced in $WEBSERVICES_DIR."
