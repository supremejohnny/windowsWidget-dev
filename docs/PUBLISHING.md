# GitHub 二进制发布记录

用户要求：核心源码在私有开发仓库维护，公开仓库只发布程序和简单 README。

## 开发仓库

- 私有仓库：https://github.com/supremejohnny/windowsWidget-dev
- 本地工作目录：`windowsWidget-dev/`。
- 本地源码仓库的 origin 只能指向上述私有仓库；上传源码前核对仓库 private=true。
- 开发仓库保留完整 Git 历史。不要将该仓库设为 public，也不要把它的历史推送到公开发布仓库。

## 公开发布仓库

- 仓库：https://github.com/supremejohnny/windowWidget
- 正式 Release：https://github.com/supremejohnny/windowWidget/releases/tag/v0.0.1
- 公共 Git 仓库独立位于本地 `dist/public-repo/`；其历史只有 README 与 THIRD_PARTY_NOTICES.txt。
- 公开仓库与私有开发仓库是两个独立 Git 仓库，禁止向公开仓库推送开发历史。
- 后续文档更新只能从 `dist/public-repo/` 推送；后续程序以 Release asset 上传。
- 0.0.1 上传文件仅为 `WindowPet-0.0.1.exe` 和 `SHA256SUMS.txt`。GitHub 自动 source archive 也只包含两份说明文件。
- 已核对 GitHub EXE digest 与本地产物 SHA-256 一致：`19a5d6089f14eee3757d86a0284eaa98baa0f101d155bd254d06d143db8125a1`。

用户反馈约 2.5 小时运行占用正常、无卡顿，已写入公共 README 与 Release 说明。此次构建没有关闭或覆盖原运行进程。

当前 EXE 是托管 .NET 程序，源码不公开不等于二进制不可反编译；本版未增加代码混淆或其他反向工程措施。

## macOS v0.0.2（2026-10-07）

- 发布 URL：https://github.com/supremejohnny/windowWidget/releases/tag/v0.0.2 。
- 私有源码基于 `2726e80`，先保留用户认可的 6 段基准（`935d742`），再增加到 10 段。源码只推送 `supremejohnny/windowsWidget-dev`；推送前再次核对 origin 与 private=true。
- 构建命令：`./macos/build-release.sh`。输出 `dist/macos-release/v0.0.2/WindowPet-0.0.2-macos-arm64.zip` 与 `SHA256SUMS.txt`。
- 平台：Apple Silicon（arm64），macOS 12 及以上。ad-hoc 签名，无 Developer ID / 公证；不支持 Intel Mac。
- ZIP SHA-256：`4e0beac64a2cfcd554a72b526b69b1372352b4d3ef029652d2797a8369df7981`。
- 已检查 ZIP 解压后的签名、版本、架构和可执行文件字节与受测构建一致。包内仅有 `.app`、运行资源与许可证，无开发源码或测试。
- 完整测试：`./macos/test.sh --ui`；最终发布构建再次独立窗口检查。20 组用户认可基准的完整轨迹比较通过；无行为变化的优化检查了 158,400 个节点位置与休眠决定，逐位一致。
- 公共 Git checkout 只更新 README 与 THIRD_PARTY_NOTICES.txt；Release 上传 ZIP 与 checksum。Windows v0.0.1 的程序、Release 与校验文件保留。
- 已发布为正式 Release（draft=false），GitHub published_at：`2026-10-07T08:59:27Z`；已确认是公开仓库 latest Release。
- GitHub ZIP 与 checksum 资产 digest 均与本地 SHA-256 一致；另从正式 Release 下载 ZIP 和 checksum，重新校验通过。
- 私有功能提交：`74928ec211feac589095d15b03949df4e0415cea`，已推送 master。公开文档提交：`2a69dbb56daf0fe32c5e816deea367c2d59757ee`；公开 tag 仅引用这份文档历史，不含开发源码。

## macOS v0.0.3（2026-10-07）

- 发布 URL：https://github.com/supremejohnny/windowWidget/releases/tag/v0.0.3 。
- 修复所有桌面的窗口发现、台前调度分组 / 全屏资格、反复跨应用置前，以及窗口切换时的动态暂停 / 恢复。采用前台目标策略，后台不显示挂件；目标关闭需重新选择。
- `MacRopePhysics.swift` 与 v0.0.2 无差异；10 段绳动态参数不变。身体只新增切换后的支点重新采样，不改变正常拖动方程。
- 完整测试 `./macos/test.sh --ui` 通过，报告 `.test-output/macos/switching-release-validation.txt`。实际窗口微动阈值增加 0.1 点调度余量；相同环境的上一版也超出旧阈值（1.016 点），本版 1.007–1.023 点。确定性基准和动态下限保持原阈值。
- 台前调度开启时，独立 AppKit 应用验证前后台切换、最小化恢复、全屏进出与同应用多窗口焦点。全屏面板 `isOnActiveSpace` 与 Window Server `onScreen` 均为 true。未直接操作 Codex 侧边缩略图；该路径、多屏和长期占用仍待继续实测。
- 平台仍为 Apple Silicon、macOS 12+，台前调度接口要求 macOS 13+；ad-hoc 签名，未进行 Developer ID 签名 / 公证。
- ZIP SHA-256：`e1b8362d4b339666d8de682fbbfb1d9b8a6e53431cf199a5d3497ab6b4ff6779`。打包后重新解压，签名检查通过，可执行文件与发布构建字节一致；包内无源码或测试。
- 上传源码前确认 origin 为 `supremejohnny/windowsWidget-dev` 且 GitHub `private=true`；公共 checkout 只修改 README，上传 ZIP 和 checksum 作为 Release assets。
- 已发布正式 Release（draft=false，prerelease=false），published_at=`2026-10-07T09:41:05Z`，已核对 latest 为 v0.0.3。GitHub 资产 digest 与本地一致；从正式 Release 下载 ZIP 与 checksum，重新校验通过。
- 私有功能提交：`bd549bb`，已推送 master。公共文档提交 / Release 目标：`987d25b266d036c22ce130bffd0beb22c486a11e`；GitHub tree 只有 README 与 THIRD_PARTY_NOTICES.txt。
- 关闭临时测试应用后打开正式修复版，控制面板实际列表中显示 `Codex — Window #4250（后台 / 其他桌面）`。
