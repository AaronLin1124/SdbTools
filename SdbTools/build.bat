@echo off
echo ========================================
echo DbcTools Cross-Platform Build Script
echo ========================================
echo.

echo Cleaning bin folder...
if exist bin rmdir /s /q bin
mkdir bin

echo.
echo Building for Windows x64...
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:AssemblyName=DbcTools-win -o bin\DbcTools-win

echo.
echo Building for Linux x64...
dotnet publish -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:AssemblyName=DbcTools-linux -o bin\DbcTools-linux

echo.
echo Building for macOS x64 (Intel)...
dotnet publish -c Release -r osx-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:AssemblyName=DbcTools-mac-intel -o bin\DbcTools-mac-intel

echo.
echo Building for macOS ARM64 (Apple Silicon)...
dotnet publish -c Release -r osx-arm64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:AssemblyName=DbcTools-mac-arm -o bin\DbcTools-mac-arm

echo.
echo ========================================
echo Build complete!
echo ========================================
echo.
echo Output files:
echo   - bin\DbcTools-win\DbcTools-win.exe
echo   - bin\DbcTools-linux\DbcTools-linux
echo   - bin\DbcTools-mac-intel\DbcTools-mac-intel
echo   - bin\DbcTools-mac-arm\DbcTools-mac-arm
echo.

pause
