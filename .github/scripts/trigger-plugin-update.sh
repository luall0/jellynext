#!/bin/bash
set -e

# Script to trigger plugin repository update via GitHub API
# This handles proper JSON escaping for the changelog

GUID="$1"
CHECKSUM="$2"
CHANGELOG="$3"
TARGET_ABI="$4"
SOURCE_URL="$5"
VERSION="$6"
PAT_TOKEN="$7"

# Escape the changelog for JSON:
# - Escape backslashes first
# - Escape double quotes
# - Escape newlines
# - Escape tabs
# - Remove carriage returns
ESCAPED_CHANGELOG=$(echo "$CHANGELOG" | \
  sed 's/\\/\\\\/g' | \
  sed 's/"/\\"/g' | \
  sed ':a;N;$!ba;s/\n/\\n/g' | \
  sed 's/\t/\\t/g' | \
  tr -d '\r')

# Create the JSON payload
JSON_PAYLOAD=$(cat <<EOF
{
  "event_type": "external_trigger",
  "client_payload": {
    "guid": "${GUID}",
    "checksum": "${CHECKSUM}",
    "changelog": "${ESCAPED_CHANGELOG}",
    "targetAbi": "${TARGET_ABI}",
    "sourceUrl": "${SOURCE_URL}",
    "version": "${VERSION}"
  }
}
EOF
)

# Send the API request
curl -X POST \
  -H "Accept: application/vnd.github+json" \
  -H "Authorization: Bearer ${PAT_TOKEN}" \
  -H "X-GitHub-Api-Version: 2022-11-28" \
  https://api.github.com/repos/luall0/jellyfin-luall0-plugins/dispatches \
  -d "${JSON_PAYLOAD}"

echo "Plugin repository update triggered successfully"
