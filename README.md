# PathSnap

PathSnap 是一个基于 .NET 8 WinForms 的截图路径工具：
按下全局快捷键后，将剪贴板中的图片保存到本地，并把带引号的文件路径复制回剪贴板，便于直接粘贴到命令行或聊天工具。

## 功能特性

- 全局快捷键触发（支持键盘与鼠标按键组合）
- 自动保存剪贴板图片到指定目录
- 自动复制带引号的图片路径
- 托盘常驻运行，支持快速打开设置和保存目录
- 现代化设置面板，可直接选择保存目录

## 运行环境

- Windows
- .NET 8 SDK（开发/构建）

## 本地开发

```bash
dotnet restore
dotnet build PathSnap.csproj -c Debug
dotnet run --project PathSnap.csproj
```

## 发布

```bash
dotnet publish PathSnap.csproj -c Release -r win-x64 --self-contained false
```

## 手动验证清单

- 快捷键可触发保存（默认 `Ctrl+Shift+V`）
- 剪贴板为空图片时给出提示
- 目录不存在时可自动创建
- 保存后路径会以双引号形式复制
- 托盘菜单操作正常

## 许可证

本项目采用 [MIT License](./LICENSE) 开源。
