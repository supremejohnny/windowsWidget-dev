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
