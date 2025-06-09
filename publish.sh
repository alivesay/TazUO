#!/bin/bash

BOOTSTRAP="src/ClassicUO.Bootstrap/src/ClassicUO.Bootstrap.csproj"
CLIENT="src/ClassicUO.Client/"
OUT="bin/publish"
FINAL_OUT="bin/publish/combined"
ZIPFILE="TazUO.zip"

GREEN='\033[0;32m'
NC='\033[0m' # No Color

mkdir -p "$FINAL_OUT"


printf '\n%.0s' {1..5}
echo -e "${GREEN}||    Publishing...    ||${NC}"  
dotnet publish "$CLIENT" -o "${OUT}/client" -c Release


printf '\n%.0s' {1..5}
echo -e "${GREEN}||    Building bootstrapper...    ||${NC}"
dotnet publish "$CLIENT" -o "${OUT}/bootlib" -c Release -p:OutputType=Library
dotnet publish "$BOOTSTRAP" -c Release -o "${OUT}/bootstrap"

cp -r "${OUT}/bootlib/." "$FINAL_OUT"

cp -r "${OUT}/bootstrap/." "$FINAL_OUT"

cp -r "${OUT}/client/." "$FINAL_OUT"

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

echo "All done! -> ${FINAL_OUT}"
