# 仓库说明（已按当前实现同步）

## 项目结构与当前架构
`Ink Canvas Modern.sln` 是主解决方案。桌面端主程序位于 `Ink Canvas/`，目标框架是 `.NET 8 WPF`。

主入口：

- `Ink Canvas/App.xaml`
- `Ink Canvas/MainWindow.xaml`
- `Ink Canvas/MainWindow.xaml.cs`

当前项目采用“主窗口桥接 + ViewModel + Controller + Feature 协调 + InkEngine 抽象”的混合分层：

- `Ink Canvas/MainWindow/`
  `MainWindow` 的 partial 桥接按 `Events/`、`Hosts/`、`Lifecycle/`、`Utilities/` 拆分，负责窗口生命周期、控件桥接、事件路由和必须触控可视树/窗口句柄的逻辑。
- `Ink Canvas/ViewModels/`
  运行时状态与设置状态，基于 `CommunityToolkit.Mvvm`。
- `Ink Canvas/Controllers/`
  副作用与输入桥接（例如 `InkCanvasInteractionController`、PowerPoint/WPS 会话、自动化、热键等）。
- `Ink Canvas/Features/`
  按功能域组织。当前包含：
  - `Features/Automation/`
  - `Features/Ink/`
  - `Features/Presentation/`
  - `Features/Settings/`
  - `Features/Shell/`
- `Ink Canvas/Services/`
  设置持久化、日志、路径选择、系统集成。
- `Ink Canvas/Helpers/`
  低层通用工具（COM/Win32 封装、路径安全、日志辅助）。
- `Ink Canvas/Windows/`
  弹窗与次级窗口。
- `Ink Canvas/Resources/`、`Images/`
  资源、图标、样式与说明图片。
- `update_server/`
  Python 轻量更新服务。

## Ink 子系统当前事实
`Features/Ink` 目前已分为以下关键层：

- `Features/Ink/Engine/`
  新增书写引擎抽象与实现：
  - `IInkEngine` / `IInkSurfaceHost` / `InkInputSample` / `InkDocumentSnapshot`
  - `InkEngineCoordinator`（按配置路由后端）
  - `SkiaInkEngine`（当前默认后端标识 `SkiaV1`）
  - `LegacyInkAdapter`（回退后端标识 `Legacy`）
  - `InkDocumentModel`（运行时单一权威模型）
- `Features/Ink/Coordinators/`
  `InkInteractionCoordinator` 已支持识别引擎路由：
  - `InkRecognitionV2Engine`（默认）
  - `InkRecognitionV1Engine`（回退）
- `Features/Ink/Services/`
  关键新增：
  - `InkRuntimeSettingsResolver`（解析后端/识别/归档写入路由）
  - `InkStrokeV2Serializer`（`strokes.v2.bin` 读写）
  - `InkDocumentModelAdapter`（`StrokeCollection <-> InkDocumentModel` 适配）
  - `InkArchiveService` 已支持 v4 写入与 v3 兼容读取
  - `TimeMachine` 增加 `SessionReset` 审计事件轨迹

## 运行时路由（当前默认）
`Settings` 已存在以下路由字段：

- `Canvas.InkBackend`（默认 `SkiaV1`）
- `InkToShape.RecognizerVersion`（默认 `V2`）
- `Canvas.ArchiveWriteFormat`（默认 `V4`）
- `Advanced.InkEngineEmergencyFallback`（总闸）
- `Advanced.InkBackendOverride`
- `Advanced.RecognizerOverride`
- `Advanced.ArchiveWriteFormatOverride`

优先级：

- 总闸 `InkEngineEmergencyFallback` > 三个 override > 常规设置字段

总闸开启时强制：

- Backend=`Legacy`
- Recognizer=`V1`
- ArchiveWriteFormat=`V3`

## 归档现状
`InkArchiveService` 当前行为：

- 默认写 v4：
  - manifest 版本 `4`
  - 笔迹入口 `strokes.v2.bin`
  - 元素仍走 `elements.xaml`
- 兼容读取 v3/v4：
  - v3 继续支持 `strokes.icstk`
  - v4 读取 `strokes.v2.bin`
- `LoadStrokeData` 对旧调用方仍返回可构建 `StrokeCollection` 的字节流

## MainWindowViewModel 聚合

- `SettingsViewModel`
- `ShellViewModel`
- `InputStateViewModel`
- `PresentationSessionViewModel`
- `AutomationStateViewModel`
- `WorkspaceSessionViewModel`

## 设置链路当前形态

- `SettingsViewModel`：可绑定状态 + 持久化触发。
- `Features/Settings/Coordinators/SettingsApplicationCoordinator`：分发设置变更。
- `MainWindow/Hosts/SettingsApplicationHost.cs`：WPF UI bridge。
- `MainWindow/Lifecycle` 中在设置加载和设置变更时会调用 Ink 运行时路由应用（后端切换会触发 history reset + `SessionReset` 审计）。

## 构建、测试与开发命令
建议在 Windows 环境开发，并安装 .NET 8 SDK + Windows Desktop Workload。

- `dotnet restore "Ink Canvas Modern.sln"`
- `dotnet build "Ink Canvas Modern.sln" -c Debug`
- `dotnet build "Ink Canvas Modern.sln" -c Release -p:Platform="Any CPU"`
- `dotnet build "Ink Canvas Modern.sln" -c Release -p:Platform="x64"`
- `dotnet build "Ink Canvas Modern.sln" -c Release -p:Platform="ARM64"`
- `python update_server/server.py`

CI：`.github/workflows/dotnet-desktop.yml`（Any CPU / x64 / ARM64）。

## 代码放置与修改规则
遵循现有 C# / XAML 风格：

- 4 空格缩进
- 大括号独占一行
- PascalCase 命名

新增或修改时，优先遵循：

- 持久化设置：
  `SettingsViewModel` + `Resources/Configuration/Settings.cs` + `Services/Settings/`
- 输入采样归一化：
  放 `Controllers/Input`（产出 `InkInputSample`），不要把输入判定回灌到 `MainWindow` 大量事件中
- 书写后端/命中/快照逻辑：
  放 `Features/Ink/Engine/`
- 识别与交互业务编排：
  放 `Features/Ink/Coordinators/`
- 归档/序列化/兼容格式：
  放 `Features/Ink/Services/`
- UI 控件显隐、动画、可视树操作：
  放 `MainWindow/Hosts` 或 `MainWindow/Events` 的桥接层

## 与可靠性相关的强约束

- 路径拼接：
  统一通过 `PathSafetyHelper.ResolveRelativePath` / `NormalizeLeafName`，不要把外部字符串直接喂给 `Path.Combine`。
- Win32/COM：
  复用既有 helper，避免散落新互操作声明。
- 异常处理：
  不要引入裸 `catch {}` 或静默吞错。
- Ink 扩展属性：
  `Stroke.AddPropertyData` 仅写入 WPF Ink 支持的数据类型；不要直接写 `Guid` 对象（可用字符串形式）。

## 当前应避免回流的旧模式

- 不要把控件 `Visibility` 当业务状态源。
- 不要新增 `currentMode` 之类兼容字段绕开 ViewModel。
- 不要把设置改动写成“直接改 Settings + 直接改控件 + 顺手保存”的散点代码。
- 不要把新业务决策塞回 `MainWindow.xaml.cs`。

## 测试与验证要求
提交前至少执行：

- `Debug`
- `Release | Any CPU`
- `Release | x64`

若涉及平台/RID/打包，再补：

- `Release | ARM64`

手工冒烟至少覆盖：

- 启动与设置加载/保存
- 墨迹书写、工具切换、橡皮模式
- 后端切换与应急总闸（Skia/Legacy）
- 识别路由（V1/V2）基本行为
- 归档保存/加载（v4 写入、v3 读取兼容）
- 后端切换与归档加载后 `SessionReset` 行为是否符合预期
- 浮动工具栏与子面板
- 桌面模式/黑板模式切换
- PowerPoint/WPS 会话检测、翻页、导航显示
- 热键与自动化流程

如改动涉及路径/文件名/外部输入，补测中文、空格、非法字符、疑似绝对路径输入。

## 依赖注意事项

- Office 相关依赖保持现状：
  - `Microsoft.Office.Interop.PowerPoint`
  - `MSOfficeCore.Interop`（.NET 8 下 `NU1701` 为已知现状）
- Ink 引擎相关已新增：
  - `SkiaSharp`

## 提交与 PR 约定
提交信息短小、祈使式，建议前缀：

- `[fix]`
- `[add]`
- `[opt]`
- `[refactor]`

PR 建议包含：

- 改动摘要
- 关联 issue（如有）
- 构建与验证步骤
- UI 变更截图/录屏
- 环境前提说明（Office / WPS / 触控设备等）

