#!/bin/bash

CLIENT="src/ClassicUO.Client/"
OUT="bin/publish/"

dotnet publish "$CLIENT" -o "${OUT}win-x64" -c Release -r win-x64 /p:PublishSingleFile=true
dotnet publish "$CLIENT" -o "${OUT}linux-x64" -c Release -r linux-x64 /p:PublishSingleFile=true
dotnet publish "$CLIENT" -o "${OUT}osx-x64" -c Release -r osx-x64 /p:PublishSingleFile=true
dotnet publish "$CLIENT" -o "${OUT}osx-arm64" -c Release -r osx-arm64 /p:PublishSingleFile=true
