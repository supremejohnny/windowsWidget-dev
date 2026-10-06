# 环境兼容、分发和占用研究

日期：2026-10-06。实测主机是 **Windows 10 Pro 22H2，10.0.19045，x64**，安装的 .NET Framework 为 **4.8.1**（Release 533325）。这不是 Windows 11 实测。

## Windows 兼容矩阵

| 环境 / 场景 | 当前结论 | 证据或下一步 |
| --- | --- | --- |
| 本机 Windows 10 x64 普通桌面窗口 | 已验证 | 用户接受当前效果；现有覆盖测试检查固定锚点、四角、遮挡、点击穿透与收起 |
| Windows 11 x64 | 预计可运行，未实测 | 所用 Win32/WinForms API 可用，系统带 .NET Framework；需要干净机器验证 |
| 32 位 CLR | 图片与资源加载已验证 | 同一 AnyCPU EXE 在本机 x86 CLR 通过无外部文件测试；不等于在 32 位 Windows 验证完整 GUI |
| Windows 10 x86 | 未实测 | 预计可使用 AnyCPU，需验证全部 HWND、绘制和事件行为 |
| Windows 11 ARM64 | 可行，未实测 | .NET Framework 4.8.1 有原生 ARM64 支持；需确认 AnyCPU 启动和所有 P/Invoke、GDI 行为 |
| 不同位数的目标应用 | 设计支持 | HWND 用 IntPtr，WinEvent 进程外通知；需补真实 x86/x64 应用组合验证 |
| 系统 DPI 单屏 | 当前实现 | SetProcessDPIAware 为系统 DPI 感知；尚无 100/125/150/200% 全矩阵证据 |
| 多屏相同 DPI、负坐标 | 坐标设计可支持，未实测 | 使用有符号屏幕坐标，不限制为主屏；需要左右/上下排列验证 |
| 多屏混合 DPI / 运行中更改缩放 | 当前重点缺口 | 没有 PerMonitorV2 和完整 WM_DPICHANGED 处理，固定像素绘制与点击区域可能不一致 |
| 边框不同、无边框、最大化、贴靠 | 尚待系统化验证 | 取 GetWindowRect，可能包含不可见边框；应评估 DWM 可见边界且维持坐标一致 |
| 普通 GPU 加速应用 | 不依赖目标的渲染引擎 | 只是目标窗口外的桌面叠层；具体应用仍需验证 |
| TopMost、管理员窗口、UWP/宿主窗口 | 未承诺支持 | 检查权限、窗口筛选、事件与 Z 顺序；不默认要求管理员启动 |
| Windows 虚拟桌面 | 存在未处理状态 | 未查询 DWMWA_CLOAKED；可见 HWND 不一定在当前桌面显示 |
| 独占全屏、UAC 安全桌面 | 首版不作为支持范围 | 普通桌面叠层不能保证进入这些表面 |
| 睡眠恢复、锁屏、远程桌面、显示器断开 | 未实测 | 验证时间跳变、目标消失、显示器变化和图形资源恢复 |
| Windows 7/8、精简系统、Server Core | 首版不承诺 | 不以旧系统/被移除框架的系统作为 0.0.1 目标 |

[微软系统要求](https://learn.microsoft.com/en-us/dotnet/framework/get-started/system-requirements)列出 Windows 10 22H2 与 Windows 11 内置 .NET Framework 4.8/4.8.1。精简或损坏系统不能据此保证运行。[ARM64 支持公告](https://devblogs.microsoft.com/dotnet/announcing-dotnet-framework-481/)说明 4.8.1 在 Windows 11+ 支持原生 ARM64。

高 DPI 应先统一坐标、绘制面和点击区域，随后配置 PerMonitorV2 并测试跨屏；只加一个 DPI 标志不足以完成适配。[WinForms 高 DPI 文档](https://learn.microsoft.com/en-us/dotnet/desktop/winforms/high-dpi-support-in-windows-forms)指出增强支持需要显式启用和运行条件。[GetWindowRect](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getwindowrect)还涉及 DPI 虚拟化和不可见边框。[DWMWA_CLOAKED](https://learn.microsoft.com/en-us/windows/win32/api/dwmapi/ne-dwmapi-dwmwindowattribute)可检测被 DWM 隐藏的窗口。

## 一个 EXE 的选择

| 方案 | 用户体验 | 代价 / 结论 |
| --- | --- | --- |
| 当前 .NET Framework，AnyCPU，内嵌图片 | 普通 Windows 10/11 直接双击一个 EXE，无 assets 或配置目录 | 使用系统 CLR；首版推荐。已生成并验证候选 |
| 现代 .NET，自包含单文件 | 不依赖预装对应 .NET 运行时 | 携带运行时增加包体，按 OS/CPU 分别发布；不能据此保证更低常驻内存 |
| 原生 Win32 C++/Rust | 可进一步减少运行时依赖 | 需要重写 GUI/渲染与移植物理，先在后续原型实际比较，再决定是否值得 |

现代 .NET 的[单文件发布](https://learn.microsoft.com/en-us/dotnet/core/deploying/single-file/overview)仍按 OS 和架构区分，自包含包包含运行时。官方目前列出 [WinForms 不支持 trimming](https://learn.microsoft.com/en-us/dotnet/core/deploying/trimming/incompatibilities)，不应把开启裁剪或 AOT 当作现成的小体积方案。

当前候选把原始 PNG 作为程序集资源，保持同样的高质量缩放路径；它不包含 Node、测试 fixture 或完整第三方 JS。版本、Windows manifest、MIT 声明也内嵌。About 可查看声明。下载一个 EXE 即可；不需要管理员安装。它仍是 Windows 应用，不能在 macOS/Linux 上原生启动。

## macOS 可行性

**可以实现同类产品，但当前 Windows EXE 不可直接转为 Mac 版。建议轻量原生 AppKit 外壳。**

| Windows 部分 | Mac 方案 | 需要验证的内容 |
| --- | --- | --- |
| HWND / EnumWindows | AXUIElement 获取应用及窗口；必要时用 CGWindowList 窗口信息辅助 | AX 窗口与 Window Server window ID 的匹配、同标题窗口、非标准窗口 |
| WinEventHook | AXObserver 订阅移动、缩放、最小化等通知，低频查询补漏 | 各目标应用是否提供可靠通知；位置变更延迟 |
| 透明 layered HWND | 透明无边框 NSPanel/NSWindow，Core Graphics 绘制 | Retina backingScale、多屏坐标转换、底部原点与物理顶部原点转换 |
| 绘制窗口穿透 + 小输入窗 | ignoresMouseEvents 绘制窗 + 两个不抢焦点的输入 panel | 点击 region、焦点行为和被其他应用覆盖后的点击 |
| SetWindowPos 相对顺序 | NSWindow.order(relativeTo:) 与窗口等级/状态观察 | 外部应用遮挡、失焦后排序、关闭/最小化及 Spaces；不能假定只设 floating level 就正确 |
| AnyCPU EXE | Universal `.app`（arm64 + x86_64） | 在两种 Mac 上验证；App 包是 Finder 中一个可双击的应用，内部为 bundle |

Apple 的 [AXObserver](https://developer.apple.com/documentation/applicationservices/1460133-axobservercreate)用于接收应用通知。观察其他应用的辅助功能接口需要用户在系统设置授权；见 [辅助功能访问权限](https://support.apple.com/guide/mac-help/mh43185/mac)。首次使用流程必须说明用途、检测授权并处理撤销。只跟随窗口几何不需要捕获窗口画面；如果后续使用屏幕捕获接口，则另行评估其权限。

AppKit 有 [ignoresMouseEvents](https://developer.apple.com/documentation/appkit/nswindow/ignoresmouseevents) 和 [相对窗口排序](https://developer.apple.com/documentation/appkit/nswindow/order(_:relativeto:))。这些是实现基础，不能证明与 Windows 完全相同的第三方窗口遮挡行为。Spaces、全屏和 Stage Manager 是 Mac 原型的首要验证项。

[Universal Binary](https://developer.apple.com/documentation/apple-silicon/building-a-universal-macos-binary)在一个应用中包含两种架构。直接分发建议 Developer ID 签名、公证，并以 `.zip` 或 `.dmg` 提供 `.app`；见 [Apple 分发说明](https://developer.apple.com/macos/distribution/)。不把 Wine 或 Rosetta 当成 Windows UI 的兼容层。

为了保持低占用，建议先将现有小型 solver 与挂件角运动移植为 Swift，复用现有 450 个数值 fixture；保持两端更新时跑一致性测试。若双平台持续维护，再考虑共享 C/C++ 数学核和小型 C ABI，Windows 用 P/Invoke，Mac 用 Swift 桥接。不要在没有 Mac 验证之前重写已接受的 Windows 核心。Mac 的实际内存、CPU、系统版本下限均未知，需 Mac/Xcode 和真机测试。

## 内存与 CPU

### 指标口径

- **PrivateWorkingSet**：当前驻留的进程私有页面，最接近用户看到的约 14 MB。
- **WorkingSet**：包含共享 DLL 等页面的总驻留内存；不能与其他进程工作集简单相加。
- **PrivateBytes**：私有提交，包括尚未驻留的页面和 CLR/原生分配。评估长期增长需要同时观察它。
- **GDI/USER/Handle 数量**：图形和窗口资源泄漏的直接线索；不能只看托管堆。
- **CPU**：记录累计 CPU 时间，再计算区间增量；短区间为 0 不意味着绝对零指令或长期零耗电。

### 本轮被动采样

在用户仍运行的原进程 PID 30884 上采样，没有关闭、重启或调参。时间为 2026-10-06 20:43:00–20:43:10（北京时间），5 个样本，约 10 秒有效窗口：

| 指标 | 结果 |
| --- | --- |
| 私有工作集 | 13.953–13.961 MiB |
| 总工作集 | 42.621–42.629 MiB |
| 私有提交 | 32.457 MiB，样本内不变 |
| GDI / USER | 46 / 53，样本内不变 |
| 总 handles | 306，样本内不变 |
| CPU 区间增量 | 采样分辨率内为 0 |

这只验证安静时短时间指标，不替代用户的一小时结果，也不是持续移动、反复挂载的压力测试。先前 300 帧绘制测试未见 GDI 增长；数学求解器单独测得约 0.141 ms/移动帧，不能将它当作整个进程的 CPU 占用。

### 一小时复现方法

```powershell
.\measure-usage.ps1 -ProcessId 30884 -DurationMinutes 60 -IntervalSeconds 10
```

PID 请改为当前进程。只有一个 WindowPet 时可省略 ProcessId。脚本在独立 PowerShell 进程内取样，不往挂件进程加入计时器；CSV 写入 `.test-output`。首个 CPU 样本为空，私有工作集读取失败时为空，均不伪装为零。`CpuOneCorePercent` 以一颗逻辑 CPU 为 100%，`CpuMachinePercent` 按整机归一化。

一小时观察建议分辨静止、拖动/戳动、收起、反复 Attach/Detach 四种状态。先看预热后的 PrivateBytes 和句柄是否形成平台；周期性 GC 波动或工作集变化不能单独判为泄漏。如果正常静止时 PrivateBytes 或 GDI/USER 持续阶梯增长，再根据相应资源排查。采样工具不会自动启动，也没有后台监控任务。

## 发布顺序建议

1. 0.0.1-pre 定位为 Windows 预览版，保留当前效果，公开实测范围和已知边界。
2. 纳入用户一小时结果；在至少一台干净 Windows 11 上验证单 EXE，优先补混合 DPI 和虚拟桌面行为。
3. 独立 Mac 窗口原型先验证跟随、点击和遮挡，再移植物理；以相同 fixture 保持效果一致。
4. 是否原生化 Windows 由完整进程测量决定，不仅比较 EXE 字节数。
