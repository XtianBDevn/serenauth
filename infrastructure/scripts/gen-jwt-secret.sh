#!/usr/bin/env bash
# Generates a 64-byte base64 secret suitable for Jwt__SigningKey.
# Pipe directly into your secret manager — never commit the output.
set -euo pipefail
openssl rand -base64 64 | tr -d '\n'
