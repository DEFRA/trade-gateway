#!/usr/bin/env bash
set -euo pipefail

# Thin wrapper for certex webservices generation using shared helper
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
WSDL_FILE="$SCRIPT_DIR/master.wsdl"
PARAMS_DIR="$SCRIPT_DIR"
WEBSERVICES_DIR="$REPO_ROOT/src/TracesNT/CertexServices"

bash "$REPO_ROOT/scripts/_generate-webservices.sh" "$WSDL_FILE" "$PARAMS_DIR" "$WEBSERVICES_DIR"




