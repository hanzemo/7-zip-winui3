#!/bin/bash
# 快速构建和测试脚本
echo "=== SevenZip WinUI3 构建脚本 ==="
cd /workspace/SevenZipWinUI
dotnet build SevenZipApp.sln -c Release
echo "构建完成！请检查输出目录运行"
