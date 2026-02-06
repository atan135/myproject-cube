@echo off
cls
echo ========================================
echo Cube Server 工具类示例程序
echo ========================================
echo.

cd /d "%~dp0..\examples\ConsoleExample"

echo 正在构建示例程序...
dotnet build --no-restore
if %errorlevel% neq 0 (
    echo 构建失败，请检查错误信息
    pause
    exit /b %errorlevel%
)

echo.
echo 正在运行示例程序...
echo.
dotnet run

pause