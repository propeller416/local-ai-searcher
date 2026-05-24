#!/bin/bash
set -e

# Устанавливаем целевую платформу, по умолчанию win-x64, если не передана другая
RID=${1:-win-x64}

echo "Publishing the application for $RID..."
dotnet publish src/DesktopApp/DesktopApp.csproj -c Release -r $RID --self-contained true -o ./publish_output

echo "Packing the application with Velopack..."
# Указываем прямой путь к vpk и разрешаем запуск на .NET 10 (roll forward)
VPK="DOTNET_ROLL_FORWARD=Major $HOME/.dotnet/tools/vpk"

if [ "$RID" == "win-x64" ]; then
    eval "$VPK \"[win]\" pack -c win -r win-x64 --packId LocalAiSearcher --packVersion 1.0.0 --packDir ./publish_output --mainExe LocalAiSearcher.exe"
elif [[ "$RID" == osx* ]]; then
    eval "$VPK \"[osx]\" pack -c osx -r osx-arm64 --packId LocalAiSearcher --packVersion 1.0.0 --packDir ./publish_output --mainExe LocalAiSearcher"
elif [[ "$RID" == linux* ]]; then
    eval "$VPK \"[linux]\" pack -c linux -r linux-x64 --packId LocalAiSearcher --packVersion 1.0.0 --packDir ./publish_output --mainExe LocalAiSearcher"
else
    eval "$VPK pack --packId LocalAiSearcher --packVersion 1.0.0 --packDir ./publish_output --mainExe LocalAiSearcher"
fi

echo "Done!"