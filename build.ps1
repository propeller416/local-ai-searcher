# Шаг 1: Создание финального .exe (Publish)
Write-Host "Publishing the application..."
dotnet publish src/DesktopApp/DesktopApp.csproj -c Release -r win-x64 --self-contained true -o ./publish_output

# Шаг 2: Упаковка в инсталлятор (Pack)
Write-Host "Packing the application with Velopack..."
vpk pack --packId LocalAiSearcher --packVersion 1.0.0 --packDir ./publish_output --mainExe LocalAiSearcher.exe

Write-Host "Done!"