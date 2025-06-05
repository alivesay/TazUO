#!/bin/bash

BOOTSTRAP="src/ClassicUO.Bootstrap/src/ClassicUO.Bootstrap.csproj"
CLIENT="src/ClassicUO.Client/"
OUT="bin/publish"
FINAL_OUT="bin/publish/combined"
ZIPFILE="TazUO.zip"

GREEN='\033[0;32m'
NC='\033[0m' # No Color

# Array of runtime IDs
RIDS=("win-x64" "linux-x64" "osx-x64" "osx-arm64")

mkdir -p "$FINAL_OUT"

# For each RID publish the client
for RID in "${RIDS[@]}"; do
  printf '\n%.0s' {1..5}
  echo -e "${GREEN}||    Publishing for $RID...    ||${NC}"  
  dotnet publish "$CLIENT" -o "${OUT}/${RID}" -c Release -r "$RID" -p:CustomAssemblyName=TazUO-$RID

  # For the windows RID build the bootstrapper for assistant support
  if [ "$RID" == "win-x64" ]; then
    printf '\n%.0s' {1..5}
    echo -e "${GREEN}||    Building Windows-specific bootstrapper...    ||${NC}"
    #dotnet publish "$CLIENT" -o "${OUT}/bootlib" -c Release -r "$RID" -p:OutputType=Library -p:NativeLib=Shared -p:CustomAssemblyName=TazUO
    dotnet publish "$BOOTSTRAP" -c Release -o "${OUT}/bootstrap"
    cp -r "${OUT}/bootlib/." "$FINAL_OUT"
    cp -r "${OUT}/bootstrap/." "$FINAL_OUT"
  fi

  cp -r "${OUT}/${RID}/." "$FINAL_OUT"


done

# Copy monokickstart for bootstrapper on unix
printf '\n%.0s' {1..5}
echo -e "${GREEN}||    Copying monokickstart...    ||${NC}"
cp -r "tools/monokickstart/." "$FINAL_OUT"

printf '\n%.0s' {1..5}
echo -e "${GREEN}||    Zipping contents of ${FINAL_OUT}/ to $ZIPFILE ...    ||${NC}"

[ -f "$ZIPFILE" ] && rm "$ZIPFILE"

(
  cd "${FINAL_OUT}" || exit 1

  chmod +x ./ClassicUO

  zip -r -q "../${ZIPFILE}" ./*
)

echo "All done!"
