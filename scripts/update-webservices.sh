#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
WEBSERVICES_DIR="$SCRIPT_DIR/../src/TracesNT/WebServices"
PROJECT_FILE="$SCRIPT_DIR/../src/TracesNT/TracesNT.csproj"
GENERATED_FILE="$SCRIPT_DIR/TracesClients.cs"
STAGING_DIR="$(mktemp -d "$SCRIPT_DIR/tracesnt-webservices.XXXXXX")"

cleanup() {
    rm -rf "$STAGING_DIR"
    rm -f "$GENERATED_FILE"
}

trap cleanup EXIT

if [[ ! -f "$SCRIPT_DIR/FileSplitter.csx" ]]; then
    echo "Required file not found: $SCRIPT_DIR/FileSplitter.csx" >&2
    exit 1
fi

if [[ ! -f "$SCRIPT_DIR/master.wsdl" ]]; then
    echo "Required file not found: $SCRIPT_DIR/master.wsdl" >&2
    exit 1
fi

if [[ ! -f "$SCRIPT_DIR/dotnet-svcutil.params.json" ]]; then
    echo "Required file not found: $SCRIPT_DIR/dotnet-svcutil.params.json" >&2
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

echo "Regenerating WCF WebServices from $SCRIPT_DIR/dotnet-svcutil.params.json..."
set +e
SVCUTIL_OUTPUT="$(
    dotnet-svcutil -u "$SCRIPT_DIR" --projectFile "$PROJECT_FILE" 2>&1
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

if [[ ! -f "$GENERATED_FILE" ]]; then
    echo "Expected generated file not found: $GENERATED_FILE" >&2
    exit 1
fi

echo "Splitting $GENERATED_FILE into staged files..."
dotnet tool run dotnet-script "$SCRIPT_DIR/FileSplitter.csx" -- \
    "$GENERATED_FILE" "$STAGING_DIR"

if [[ -z "$(find "$STAGING_DIR" -name "*.g.cs" -print -quit)" ]]; then
    echo "FileSplitter did not produce any generated files in $STAGING_DIR" >&2
    exit 1
fi

echo "Removing previously generated files from $WEBSERVICES_DIR..."
find "$WEBSERVICES_DIR" -maxdepth 1 -name "*.g.cs" -delete
find "$WEBSERVICES_DIR" -mindepth 1 -maxdepth 1 -type d -exec rm -rf {} +

echo "Installing newly generated files into $WEBSERVICES_DIR..."
cp -R "$STAGING_DIR"/. "$WEBSERVICES_DIR"/

echo "Done! WebServices types have been regenerated and replaced."
