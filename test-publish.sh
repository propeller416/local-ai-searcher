dotnet publish src/DesktopApp/DesktopApp.csproj -c Release -r osx-arm64 --self-contained true -o ./test_publish
cd test_publish
codesign -s - LocalAiSearcher || true
./LocalAiSearcher
