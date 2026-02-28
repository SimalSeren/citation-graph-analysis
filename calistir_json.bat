@echo off
chcp 65001 > nul
echo ╔════════════════════════════════════════════╗
echo ║     Graf Analizi - JSON Yükleyici          ║
echo ╚════════════════════════════════════════════╝
echo.

if "%~1"=="" (
    echo ❌ Kullanım: JSON dosyasını bu .bat dosyasına sürükleyin
    echo.
    echo Örnek: data.json dosyasını bu dosyanın üzerine sürükleyin
    echo.
    pause
    exit /b 1
)

if not exist "%~1" (
    echo ❌ Dosya bulunamadı: %~1
    echo.
    pause
    exit /b 1
)

echo 📁 JSON Dosyası: %~1
echo 🚀 Sunucu başlatılıyor...
echo.

cd /d "%~dp0"
dotnet run -- "%~1"

pause
