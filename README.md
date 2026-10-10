# 7-Zip WinUI 3

Windows 7-Zip 客户端，WinUI 3 + MSIX 打包。

## 下载

[Actions](https://github.com/hanzemo/7-zip-winui3/actions) → 最新 Build → Artifacts → MSIX Package - x64

## 安装

1. 解压 zip
2. 双击 `SevenZip.cer` → 本地计算机 → 受信任的根证书颁发机构 → 完成 → 是
3. 再双击 `SevenZip.cer` → 本地计算机 → 受信任的发布者 → 完成 → 是
4. 双击 `SevenZipApp_*.msix` → 安装

## 构建

推送到 main 自动触发 GitHub Actions。

## 要求

- Windows 10 1809+ (17763)
- x64
