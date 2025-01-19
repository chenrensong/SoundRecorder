# Voice Recorder with Real-time Transcription

一个基于 Avalonia 的跨平台语音录制和实时转写应用。支持 Windows 和 macOS。

## 功能特点

- 🎙️ 音频录制
- 📝 实时语音转文字
- 📊 实时波形显示
- 💾 录音文件管理
- 🔄 历史记录保存
- 🌐 离线运行，无需联网
- 🖥️ 跨平台支持 (Windows/macOS)

## 技术栈

- Avalonia UI - 跨平台 UI 框架
- Vosk - 离线语音识别引擎
- NAudio - 音频处理库
- .NET 6.0+

## 系统要求

- Windows 10+ 或 macOS 10.14+
- .NET 6.0 Runtime
- 2GB+ RAM
- 200MB 磁盘空间（包含语音模型）

## 安装说明

1. 下载最新发布版本
2. 解压到任意目录
3. 运行程序，首次运行会自动下载语音识别模型（约50MB）

## 开发环境设置
bash
克隆仓库
git clone https://github.com/chenrensong/SoundRecorder.git
进入项目目录
cd SoundRecorder
还原依赖
dotnet restore
运行项目
dotnet run

## 依赖项

<PackageReference Include="Avalonia" Version="11.0.0" />
<PackageReference Include="NAudio" Version="2.1.0" />
<PackageReference Include="Vosk" Version="0.3.38" />

## 使用说明

1. 点击录音按钮开始录音
2. 说话时会显示实时波形和转写文本
3. 点击完成按钮结束录音
4. 录音文件会自动保存并进行完整转写
5. 可以在列表中查看和管理历史录音

## 项目结构
```
SoundRecorder/
├── Services/
│ ├── ISpeechRecognitionService.cs # 语音识别接口
│ └── VoskSpeechRecognitionService.cs # Vosk实现
├── ViewModels/
│ └── MainViewModel.cs # 主视图模型
├── Views/
│ └── MainView.axaml # 主视图
└── Models/
└── RecordingItem.cs # 录音项模型
```

## 贡献指南

1. Fork 项目
2. 创建特性分支 (`git checkout -b feature/AmazingFeature`)
3. 提交更改 (`git commit -m 'Add some AmazingFeature'`)
4. 推送到分支 (`git push origin feature/AmazingFeature`)
5. 打开 Pull Request

## 许可证

本项目采用 MIT 许可证

## 致谢

- [Vosk](https://alphacephei.com/vosk/) - 开源语音识别引擎
- [Avalonia](https://avaloniaui.net/) - 跨平台 UI 框架
- [NAudio](https://github.com/naudio/NAudio) - .NET 音频库
