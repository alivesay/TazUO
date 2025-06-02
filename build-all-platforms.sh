#!/bin/bash

CLIENT="src/ClassicUO.Client/"
OUT="bin/publish"

# Array of runtime IDs
RIDS=("win-x64" "linux-x64" "osx-x64" "osx-arm64")

for RID in "${RIDS[@]}"; do
  echo "Publishing for $RID..."
  dotnet publish "$CLIENT" -o "${OUT}/${RID}" -c Release -r "$RID"

  ZIPFILE="${OUT}/${RID}.zip"
  echo "Zipping contents of ${OUT}/${RID} to $ZIPFILE ..."

  [ -f "$ZIPFILE" ] && rm "$ZIPFILE"

  (
    cd "${OUT}/${RID}" || exit 1
    zip -r -q "../${RID}.zip" ./*
  )
done

echo "All done!"
