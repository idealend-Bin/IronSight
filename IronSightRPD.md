IronSight: Ethereal - 旗舰版产品设计文档

1. 产品愿景

通过 Fluent Design 体系打造极致的视觉美感，将“硬核”的 C++ 性能监控与“优雅”的 C# 交互逻辑融合。它不仅是工具，更是系统的“神经中枢”。

2. 核心模块设计

A. 全新 Fluent 视觉框架 (The Shell)

材质系统：全面集成 Mica (云母) 或 Acrylic (亚克力) 效果。即使在 Win7 下，也通过 Hook 手段模拟 Aero 透明感。

导航模式：左侧汉堡菜单导航，支持响应式布局（窄窗口自动折叠）。

动效库：使用 Composition API 实现卡片入场动画和进度条平滑过渡。

B. 系统监视器 (System Sentinel) - 深度增强

多维监控：

CPU 逻辑核心视图：利用原生层获取每个逻辑核心的负载。

IO 吞吐监控：实时显示磁盘读写曲线。

异常捕获：当某个进程 CPU 占用持续 10 秒超过 90% 时，触发“高温预警”并提供一键挂起功能。

C. 内存优化器 (Memory Alchemist) - 智能进化

阶梯式优化：

标准模式：调用 EmptyWorkingSet。

极智模式 (Smart Flush)：分析进程优先级，优先压缩高占用、低活性的后台应用（基于 UI 焦点状态判断）。

白名单持久化：支持用户右键将进程“永久豁免”，数据保存至本地 JSON 配置文件。

D. 高级剪贴板 (Ethereal Clipboard) - 新增核心

历史快照：支持记录最近 50 条剪贴板内容（文本、富文本、小图像）。

分类收藏：支持将常用文本（如代码片段、联系方式）钉选到侧边栏。

隐私模式：检测到密码或信用卡号格式时，自动隐藏预览。

跨语言同步思路：利用 C++ 监听 WM_CLIPBOARDUPDATE 消息，通过我们已实现的 DebugPrintEx 回调机制将更新实时推送到 WPF 界面。

3. 关键技术接口 (针对 AI 编码提示)

模块

原生函数 (C++)

托管封装 (C#)

监视器

GetSystemTimes / PdhCollectQueryData

PerformanceCounter (混合模式)

内存

SetProcessWorkingSetSize (深度提权版)

MemoryMethods.DeepCleanup()

剪贴板

AddClipboardFormatListener

ClipboardManager.OnClipboardChanged

4. UI/UX 详细规范

颜色体系 (Fluent Palette)

Accent: SystemAccentColor (动态获取系统主题色)。

Card Background: 0.75 透明度白色/深灰，配合 1px 的半透明描边 (Border)。

Shadows: 深度为 8-16 的软阴影，营造悬浮感。

高级剪贴板布局

瀑布流/卡片视图：点击 Ctrl + Win + V 呼出轻量级 Fluent 窗口。

一键粘贴：点击历史项自动模拟 Ctrl+V 操作。

5. PM 寄语与 AI 提示词建议

PM 寄语：
“我们不只是在堆砌功能，而是在创造一种交互。剪贴板功能要像呼吸一样自然，内存优化要像春雨一样安静。在 Win7 环境下，这些功能必须确保稳定性第一。”

推荐给 AI 的提示词 (Prompt Prefix)：

"你现在是 IronSight 的核心架构师，请基于 C# WPF 和 C++ 原生 DLL 开发 [XX功能]。要求：1. 遵守 Fluent Design 设计规范；2. 必须处理跨语言内存管理；3. 考虑 Win7 的兼容性回退机制；4. 日志必须通过我们已有的 Utils::DebugPrintEx 输出。"

6. 待办事项 (To-Do)

[ ] 实现 C++ 侧的剪贴板监听器。

[ ] 在 WPF 中创建 ClipboardItemViewModel。

[ ] 优化实时性能图表的渲染，防止高频率刷新导致 UI 卡顿。