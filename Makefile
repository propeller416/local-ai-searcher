.PHONY: build-win build-mac build-linux clean

PACK_ID = LocalAiSearcher
PACK_VERSION = 1.0.0
PUBLISH_DIR = ./publish_output

# Указываем полный путь к vpk и разрешаем запуск на .NET 10 (roll forward)
VPK = DOTNET_ROLL_FORWARD=Major ~/.dotnet/tools/vpk

build-win:
	@echo "Publishing the application for win-x64..."
	dotnet publish src/DesktopApp/DesktopApp.csproj -c Release -r win-x64 --self-contained true -o $(PUBLISH_DIR)
	@echo "Packing the application with Velopack for Windows..."
	$(VPK) pack --packId $(PACK_ID) --packVersion $(PACK_VERSION) --packDir $(PUBLISH_DIR) --mainExe $(PACK_ID).exe

build-mac:
	@echo "Publishing the application for osx-arm64..."
	dotnet publish src/DesktopApp/DesktopApp.csproj -c Release -r osx-arm64 --self-contained true -o $(PUBLISH_DIR)
	@echo "Packing the application with Velopack for macOS..."
	$(VPK) pack --packId $(PACK_ID) --packVersion $(PACK_VERSION) --packDir $(PUBLISH_DIR) --mainExe $(PACK_ID)

build-linux:
	@echo "Publishing the application for linux-x64..."
	dotnet publish src/DesktopApp/DesktopApp.csproj -c Release -r linux-x64 --self-contained true -o $(PUBLISH_DIR)
	@echo "Packing the application with Velopack for Linux..."
	$(VPK) pack --packId $(PACK_ID) --packVersion $(PACK_VERSION) --packDir $(PUBLISH_DIR) --mainExe $(PACK_ID)

clean:
	@echo "Cleaning output directories..."
	rm -rf $(PUBLISH_DIR)
	rm -rf ./Releases

run:
	dotnet run --project src/DesktopApp/DesktopApp.csproj