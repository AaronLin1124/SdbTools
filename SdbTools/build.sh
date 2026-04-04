#!/bin/bash
echo "========================================"
echo "SdbTools Cross-Platform Build Script"
echo "========================================"
echo ""

echo "Cleaning bin folder..."
rm -rf bin
mkdir bin

echo ""
echo "Building for Windows x64..."
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:AssemblyName=SdbTools-win -o bin/SdbTools-win

echo ""
echo "Building for Linux x64..."
dotnet publish -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:AssemblyName=SdbTools-linux -o bin/SdbTools-linux

echo ""
echo "Building for macOS x64 (Intel)..."
dotnet publish -c Release -r osx-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:AssemblyName=SdbTools-mac-intel -o bin/SdbTools-mac-intel

echo ""
echo "Building for macOS ARM64 (Apple Silicon)..."
dotnet publish -c Release -r osx-arm64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:AssemblyName=SdbTools-mac-arm -o bin/SdbTools-mac-arm

echo.
echo ========================================
echo Build complete!
echo ========================================
echo.
echo Output files:
echo   - bin\SdbTools-win\SdbTools-win.exe
echo   - bin\SdbTools-linux\SdbTools-linux
echo   - bin\SdbTools-mac-intel\SdbTools-mac-intel
echo   - bin\SdbTools-mac-arm\SdbTools-mac-arm
echo.

pause
