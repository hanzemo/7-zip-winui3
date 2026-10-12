# Drift Zip

Windows 压缩工具，WinUI 3 + MSIX 打包。

## 下载

[Actions](https://github.com/hanzemo/Drift-Zip/actions) → 最新 Build → Artifacts → MSIX Package - x64

## 功能

  压缩 / 解压：7z、zip、rar、tar、gz、bz2、xz
  Windows 右键菜单：解压到… / 压缩到… / 解压到此目录 / 压缩到此目录
  文件关联：双击 .7z / .zip / .rar 打开
  内建文件浏览器：盘符 / 目录 / 前进后退 / 面包屑 / 搜索
  压缩包预览：逐级进入 + 拖出解压
  视图切换：详细信息 / 大图标 / 小图标
  多选拖出（Ctrl / Shift）
  自定义标题栏
  压缩 / 解压进度条

## 安装

1. 解压 zip
2. 双击 `DriftZip.cer` → 本地计算机 → 受信任的根证书颁发机构 → 完成 → 是
3. 再双击 `*.cer` （*指全部）→ 本地计算机 → 受信任的发布者 → 完成 → 是
4. 双击 `DriftZipApp_*.msix` → 安装

## 使用

### 右键菜单

安装后，在文件资源管理器里：

  右键文件 → Drift Zip → 解压到…
  右键文件 → Drift Zip → 压缩到…
  右键文件 → Drift Zip → 解压到此目录
  右键文件 → Drift Zip → 压缩到此目录

### 主程序

打开 Drift Zip 后：

  扫描压缩包：自动列出桌面 / 文档 / 下载里的压缩包
  浏览此电脑：像资源管理器一样浏览磁盘
  压缩页：选择文件 → 指定输出 → 压缩
  解压页：选择压缩包 → 指定目录 → 解压
  设置页：日志、选项

### 视图切换

右键空白处 → 视图 → 详细信息 / 大图标 / 小图标

## 技术栈

| 部分 | 技术 |
|---|---|
| 主程序 | WinUI 3（.NET 8, net8.0-windows10.0.22621.0） |
| 压缩引擎 | SharpSevenZip 2.0.128 |
| 右键菜单 | C++ WRL（IExplorerCommand）+ MSIX manifest |
| 打包 | MSIX（self-contained） |
| CI | GitHub Actions（windows-latest, msbuild） |
| 签名 | 自签名证书（New-SelfSignedCertificate） |

## 项目结构

```
DriftZipApp.sln
├── DriftZipApp/                    # WinUI 3 主项目
│   ├── DriftZipApp.csproj
│   ├── Package.appxmanifest
│   ├── app.ico
│   ├── Assets/                     # 图标（256/512/1240 大尺寸）
│   ├── Pages/
│   │   ├── HomePage.xaml(.cs)      # 主页
│   │   ├── CompressPage.xaml(.cs)  # 压缩页
│   │   ├── ExtractPage.xaml(.cs)   # 解压页
│   │   ├── ArchivePage.xaml(.cs)
│   │   ├── ArchivePreviewPage.xaml(.cs)
│   │   ├── FilesPage.xaml(.cs)
│   │   └── SettingsPage.xaml(.cs)
│   ├── Services/
│   │   ├── DriftZipEngine.cs       # 压缩/解压核心
│   │   ├── ArchiveItem.cs
│   │   ├── CompressionOptions.cs
│   │   ├── Logger.cs
│   │   └── ViewMode.cs
│   └── Views/MainWindow.xaml(.cs)
└── DriftZipShellExt/               # C++ WRL shell 扩展
    ├── DriftZipShellExt.vcxproj
    ├── dllmain.cpp
    ├── guids.h                     # CLSID 定义
    ├── ClassFactory.h
    ├── EnumExplorerCommand.h
    ├── SubCommands.h               # 4 个子命令
    ├── SevenZipParent.h
    ├── app.ico
    └── DriftZipShellExt.rc
```

## 构建

推送到 main 自动触发 GitHub Actions。

### 本地构建

1. Visual Studio 2022 + Windows App SDK
2. 打开 `DriftZipApp.sln`
3. 生成 → 生成解决方案

### 要求

  Windows 10 1809+ (17763)
  x64

## 卸载

设置 → 应用 → Drift Zip → 卸载

证书可留可删，不影响。

## 已知问题

  自签名证书，Windows SmartScreen 会警告
  仅 x64

## 许可

GPL-2.0-or-later
