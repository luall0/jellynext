#!/bin/bash
set -e

# Script to trigger plugin repository update via GitHub API
# Uses jq for proper JSON escaping of all special characters
# Changelog is passed via CHANGELOG_CONTENT environment variable to avoid shell argument parsing issues

GUID="$1"
CHECKSUM="$2"
TARGET_ABI="$3"
CHANGELOG=$4
SOURCE_URL="$5"
VERSION="$6"
PAT_TOKEN="$7"

# Changelog comes from environment variable to avoid shell escaping issues

JSON_PAYLOAD=$(cat <<EOF
{
  "event_type": "external_trigger",
  "client_payload": {
    "guid": "${GUID}",
    "checksum": "${CHECKSUM}",
    "changelog": ${CHANGELOG},
    "targetAbi": "${TARGET_ABI}",
    "sourceUrl": "${SOURCE_URL}",
    "version": "${VERSION}"
  }
}
EOF
)

echo $JSON_PAYLOAD

# Send the API request
curl -X POST \
  -H "Accept: application/vnd.github+json" \
  -H "Authorization: Bearer ${PAT_TOKEN}" \
  -H "X-GitHub-Api-Version: 2022-11-28" \
  https://api.github.com/repos/luall0/jellyfin-luall0-plugins/dispatches \
  -d "${JSON_PAYLOAD}"

echo "Plugin repository update triggered successfully"
