#!/bin/bash

# Cube Server 工具类示例程序运行脚本

clear
echo "========================================"
echo "Cube Server 工具类示例程序"
echo "========================================"
echo

cd "$(dirname "$0")/examples/ConsoleExample"

echo "正在构建示例程序..."
dotnet build --no-restore
if [ $? -ne 0 ]; then
    echo "构建失败，请检查错误信息"
    exit 1
fi

echo
echo "正在运行示例程序..."
echo
dotnet run