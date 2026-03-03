#!/usr/bin/env bash
set -euo pipefail

chmod +x install_backend.sh install_frontend.sh

./install_backend.sh
./install_frontend.sh

echo "✅ Backend and frontend setup scripts executed."
