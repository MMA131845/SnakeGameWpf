自由贪吃蛇 v7.0.0
<div align="center">
https://img.shields.io/badge/version-7.0.0-brightgreen?style=for-the-badge
https://img.shields.io/badge/.NET-10.0--windows-512BD4?style=for-the-badge&logo=dotnet
https://img.shields.io/badge/WPF-Desktop-0078D4?style=for-the-badge&logo=windows
https://img.shields.io/badge/license-MIT-blue?style=for-the-badge
https://img.shields.io/badge/platform-Windows-0078D4?style=for-the-badge&logo=windows

一款从 WinForms 全面迁移到 WPF 的现代化贪吃蛇游戏

五种游戏模式 | 局域网联机 | 主题系统 | 成就体系 | 全局自绘 UI

功能特性 | 游戏模式 | 快速开始 | 操作指南 | 项目结构 | 技术亮点

</div>
项目简介
自由贪吃蛇 v7.0.0 是一款使用 WPF (.NET 10) 完全重写的现代化贪吃蛇游戏。所有界面（主菜单、设置、排行榜、成就、联机大厅、更新日志）全部采用 GPU 加速的自绘渲染，告别传统控件的臃肿，带来丝滑的游戏体验。

相比 v6.x 的 WinForms 版本，v7.0.0 完成了一次彻底的架构革命：

完全自绘 UI：所有面板均通过 DrawingContext 绘制，统一风格、统一缩放、统一输入处理

性能飞跃：Brush / Pen / Typeface / FormattedText 全局缓存池

主题系统：Windows11 / 深色 / 浅色 / 纯白，实时切换影响所有界面

自定义背景图：支持 JPG / PNG / BMP / GIF，兼容中文路径

AES-256 加密存档：配置、成就、排行榜全部加密存储

功能特性
五种游戏模式
模式	标识色	玩法	核心特色
经典模式	绿色	自由移动，吃食物变长	鼠标操控、Ctrl 加速、撞墙即死
淘汰之王	橙色	49 名 AI 同场竞技	F 键决斗、等级成长、毒圈收缩
占领模式	蓝色	4v4 团队占领中央区域	蓝队协作、进度条对抗、边缘箭头导航
极限模式	红色	20 敌人 + 食物仅 60 个	加速减半、AI 更强、极限操作
搜打撤	金色	搜索容器，携带分数撤离	三张地图、燃烧弹、撤离解锁 Q 技能
核心亮点
<table> <tr> <td width="50%">
现代化界面

全局 WPF 自绘渲染

窗口几何自动保存恢复

分辨率 / 显示模式实时切换

抗锯齿 / 垂直同步 / 高帧率开关

深色 / 浅色 / 主题色动态切换

</td> <td width="50%">
游戏系统

五模式差异化 AI 行为树

实时排行榜（每 10 帧刷新）

成就系统（6 项成就 + 加密存档）

教程系统（首次进入弹窗引导）

更新日志内置查看

</td> </tr> <tr> <td width="50%">
局域网联机

基于 TCP 的自研游戏服务器

主机 / 加入双模式

JSON 状态同步（33ms 帧间隔）

支持 IPv4 手动配置

</td> <td width="50%">
技术架构

模式基类 GameModeBase 插件化

AES-256 存档加密 (SecureStorage)

Named Pipe IPC 状态上报

双 .csproj 兼容 SDK / 传统构建

</td> </tr> </table>
游戏模式
经典模式 (Classic)
最纯粹的贪吃蛇体验，鼠标指向即为前进方向。

食物数量：125

敌人数量：10（会随时间补充生成）

按 Ctrl 加速冲刺（5 秒上限）

撞敌人身体或世界边界即失败

淘汰之王 (Timed) - v7.0.0 重磅重做
接近《地平线 6》淘汰之王玩法。

49 名 AI + 你 = 50 人局

靠近 AI 后按 F 发起决斗，双方冲向目标点

先到者胜：淘汰对手并提升等级（最高 10 级）

对手先到：游戏结束

AI 行为：玩家等级越高越倾向躲避（75% 至 93%）

毒圈机制：每 5 分钟收缩一次，共 3 次；圈外 15 秒判负

实时排行、等级颜色标签、缩圈倒计时、决斗倒计时

占领模式 (Team 4v4)
与蓝队 AI 并肩作战，占领地图中央。

站在占领圈内累积进度（范围 -30 至 +30）

先到 +30 蓝队胜利，先到 -30 红队胜利

占领区离开屏幕时，边缘显示黄色旋转箭头 + 距离

顶部显示进度条 + 双方剩余人数

极限模式 (Extreme)
为高手准备的高压战场。

敌人数量：20

食物数量：60

加速持续时间减半（2.5 秒）

AI 追击范围扩大至 400px

搜打撤 (Extraction)
类《逃离塔科夫》玩法。

三张地图：

地图	尺寸	容器	敌人	敌人速度	敌人生命
废弃工厂	4000x3500	30	5	1.0x	1
边境森林	5000x4000	99	10	1.5x	1
航天基地	8000x6000	999	20	5.0x	3
靠近容器按 F 搜索，进度条完成后按 F 拾取

搜索耗时与当前分数相关（分数越高耗时越长）

撤离点停留 9 秒 成功撤离

航天基地有燃烧弹空袭（暴露 5 秒即死）

成功撤离解锁全局 Q 技能

快速开始
环境要求
操作系统：Windows 10 / 11

.NET SDK：.NET 10.0 SDK（或 .NET Framework 4.8）

IDE：Visual Studio 2022 / Rider / VS Code

编译运行
bash
# 克隆仓库
git clone https://github.com/your-username/SnakeGame.git
cd SnakeGame

# 编译（SDK 风格项目）
dotnet build SnakeGame.csproj -c Release

# 运行
dotnet run --project SnakeGame.csproj
或直接双击 SnakeGame.csproj 用 Visual Studio 打开后按 F5。

提示：项目同时提供传统 .csproj（SnakeGameWpf.csproj），兼容 Visual Studio 2019 与 .NET Framework 4.8 环境。

操作指南
键盘操作
按键	功能
鼠标移动	控制蛇头方向
Ctrl	加速冲刺（长按持续）
F	搜索容器 / 发起决斗
Q	释放已解锁的技能
Esc	暂停 / 返回上一级
Space	主菜单：开始游戏
S	主菜单：打开设置
H	主菜单：历史排行榜
J	主菜单：成就
L	主菜单：更新日志
O	主菜单：局域网联机
上 / 下 / Enter	菜单导航
联机流程
主机端操作：

点击「局域网联机」进入大厅

选择「创建房间」

确认端口（默认 8888）

自动启动 GameServer 并等待客户端连接

客户端操作：

点击「局域网联机」进入大厅

选择「加入房间」

输入主机 IP 与端口

点击确定发起连接

连接成功后双方进入游戏，服务器每 33ms 广播一次游戏状态。

技术亮点
1. 全自绘 UI 架构
传统 WPF 使用 XAML 控件树，本项目将所有 UI 绘制集中在 MainWindow.OnRender()：

csharp
protected override void OnRender(DrawingContext dc)
{
    // 统一背景（主题色 or 自定义图片）
    if (useCustomBg) dc.DrawImage(_customBg, screenRect);
    else dc.DrawRectangle(ThemeBackground, null, screenRect);

    // 优先级：名字输入 > 设置 > 排行榜 > ... > 游戏
    if (ShowNameInput)    { DrawNameInput(dc); return; }
    if (ShowSettings)     { DrawSettingsInline(dc); return; }
    if (ShowRankings)     { DrawRankingsInline(dc); return; }
    // ...
    if (!GameStarted)     { DrawStartMenu(dc); return; }
    DrawLocalGame(dc);
}
优势：单一渲染管线、跨面板一致的缩放、零控件开销。

2. 高性能渲染缓存
RenderResources.cs 提供全局线程安全缓存：

csharp
// Brush 缓存：uint 颜色值 -> Frozen Brush
public static SolidColorBrush Brush(byte a, byte r, byte g, byte b)
{
    uint key = ((uint)a << 24) | ((uint)r << 16) | ((uint)g << 8) | b;
    if (_brushCache.TryGetValue(key, out var cached)) return cached;
    var br = new SolidColorBrush(Color.FromArgb(a, r, g, b));
    br.Freeze(); // 冻结后可跨线程复用，大幅降低 GC
    _brushCache[key] = br;
    return br;
}
收益：

零重复分配：Frozen 对象可安全跨线程共享

GPU 友好：冻结对象避免 WPF 的依赖属性开销

GC 压力降低 80% 以上

3. AES-256 加密存档
SecureStorage 采用 AES-CBC 模式，文件格式：

text
[Magic "SG01" 4 字节][IV 16 字节][AES-256-CBC 密文]
自动兼容旧明文文件（无 magic 则按明文读）

首次保存自动加密，无需迁移

硬编码密钥防止普通用户直接编辑存档

4. 模式插件化
csharp
public abstract class GameModeBase
{
    public abstract string Name { get; }
    public abstract int FoodCount { get; }
    public abstract int EnemyCount { get; }
    public abstract float AiDifficulty { get; }

    public virtual void Initialize()       { }
    public virtual void UpdateBeforeMove() { }
    public virtual void UpdateAfterMove()  { }
    public virtual void UpdateAI()         { }
    public virtual void DrawUI(DrawingContext dc) { }
}
新增模式只需继承 GameModeBase，在 MainWindow.StartGame() 里注册，无需改动主循环。

5. 双模构建配置
文件	目标框架	适用场景
SnakeGame.csproj	net10.0-windows	现代 SDK 风格，跨平台 CLI 友好
SnakeGameWpf.csproj	v4.8	传统 Visual Studio 工程
更新日志摘要
v7.0.0（当前版本）
<details> <summary>点击展开完整更新内容</summary>
重大变更
从 WinForms 全面迁移到 WPF（.NET 10）

所有界面统一自绘：设置 / 排行榜 / 成就 / 联机 / 更新日志全部内嵌主窗口

淘汰之王模式重做：49 名 AI 同场竞技，接近《地平线 6》玩法

淘汰之王新特性
按 F 挑战 600 范围内最近的 AI，双方冲向同一目标点，先到者胜

击败对手提升等级（最高 10 级），速度随等级提升

AI 遇玩家 25% 发起挑战，75% 躲避；玩家等级越高 AI 越倾向躲避

毒圈每 5 分钟收缩一次，共 3 次；圈外停留 15 秒判负

新增实时排行、等级颜色标签、缩圈倒计时、决斗倒计时

占领模式
新增屏幕边缘黄色箭头，指向屏幕外的占领区并显示距离

顶部显示进度条与蓝红队剩余人数

设置界面
左侧 3 大类标签（个性化 / 画面 / 实验性），右侧内容自适应

支持分辨率、显示模式、最高帧数、抗锯齿、垂直同步、高帧率模式

主题系统实时影响所有界面背景与卡片色

自定义背景图支持 JPG / PNG / BMP / GIF（兼容中文路径）

Bug 修复
修复淘汰之王决斗胜利后未立即结束游戏

修复部分 UI 被其它 HUD 遮挡

修复自定义图片无法显示（改用 FileStream + Uri 双重加载）

修复多处 Collection was modified 异常

修复占领模式 AI 阵营逻辑错乱

修复模式选择选项卡尺寸过小

</details>
贡献指南
欢迎提交 Issue 和 Pull Request。

bash
# Fork 后克隆
git clone https://github.com/your-username/SnakeGame.git

# 创建功能分支
git checkout -b feature/amazing-feature

# 提交修改
git commit -m "feat: 添加新功能"

# 推送分支
git push origin feature/amazing-feature

# 在 GitHub 上打开 Pull Request
代码规范
使用 PascalCase 命名公开成员，_camelCase 命名私有字段

UI 绘制代码集中在 MainWindow.OnRender() 及其子方法

新增游戏模式请继承 GameModeBase

开源协议
本项目基于 MIT License 开源，详见 LICENSE 文件。

text
MIT License

Copyright (c) 2024 Your Name

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction...
致谢
Newtonsoft.Json：JSON 序列化

.NET Community：优秀的开发平台

所有为本项目提供反馈与建议的玩家

<div align="center">
如果这个项目对你有帮助，欢迎点一个 Star

Made with love by Your Name

https://visitor-badge.laobi.icu/badge?page_id=your-username.SnakeGame

</div>
