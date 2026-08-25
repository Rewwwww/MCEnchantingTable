# MC附魔台 — 开发环境与工程说明

## 1. 项目基本信息

项目：

**MCEnchantingTable**

玩家可见名称：

**MC附魔台**

游戏：

**Slay the Spire 2**

目标框架：

**.NET 9**

当前项目由 Alchyr StS2 Mod Template 创建。

当前模板已经成功完成：

`dotnet build`

并且已经经过实际游戏加载验证。

因此：

**当前工程属于已知可编译、可加载的正常基线。**

不要无理由重构模板基础结构。

---

# 2. 本地路径

## Mod源码

```text
D:\AAAPKBC\Files\Mods\StS2\MCEnchantingTable
```

所有正式源码修改都应发生在这个目录中。

---

## 游戏本体

```text
D:\AAAPKBC\Apps\Steam\steamapps\common\Slay the Spire 2
```

该目录主要用于：

- 引用游戏程序集
- 本地测试Mod
- 运行游戏

不要修改原版游戏文件。

---

## 反编译游戏源码

```text
D:\AAAPKBC\Files\Mods\StS2\StS2_Decompiled
```

这是通过 GDRE Tools 得到的原版游戏反编译参考。

当需要查找：

- 原版遗物
- 战斗结算
- 幕切换
- 火堆
- Ancient
- 附魔
- UI
- 存档
- 多人同步
- RNG

等实现时，优先搜索这里。

不要把整个反编译工程复制进Mod项目。

---

# 3. 当前开发工具

已安装：

- .NET 9 SDK
- Godot 4.5.1 Stable Mono
- GDRE Tools

游戏已经成功被反编译用于源码参考。

---

# 4. 构建命令

在项目根目录执行：

```powershell
dotnet build
```

当前已验证该命令成功。

已知存在一个非阻塞警告：

```text
STS002: Localization files must be added as additional files for analysis
```

该警告目前不会阻止构建或游戏加载。

未来开始正式加入本地化文件时再正确处理。

不要为了消除此警告破坏模板分析器配置。

当前带 PCK 的有效发布命令为：

```powershell
dotnet publish .\MCEnchantingTable.csproj -c ExportRelease
```

项目根目录存在用于 Godot C# 工程检查的 `.sln`，但该 solution 没有 `ExportRelease` 配置。因此发布时必须明确指定 `.csproj`，不要删除 `.sln`，也不要为此改造整个解决方案构建体系。

---

# 5. 构建产物原则

项目根目录中的文件属于：

**源码**

游戏目录中由构建过程复制出来的Mod文件属于：

**测试构建产物**

不要直接编辑游戏Mod目录里的：

- DLL
- JSON
- PCK
- 其他部署文件

因为下一次：

```powershell
dotnet build
```

可能覆盖这些文件。

例如 Manifest 应修改：

```text
D:\AAAPKBC\Files\Mods\StS2\MCEnchantingTable\MCEnchantingTable.json
```

而不是修改游戏测试目录里的副本。

---

# 6. Steam Workshop

项目最终目标：

**发布到 Steam 创意工坊。**

源码工程和创意工坊发布包应保持分离。

未来应设计：

开发源码

↓

构建

↓

本地测试部署

或

↓

Workshop发布包

不要把Steam Workshop内容目录作为主要源码目录。

---

# 7. Codex工作原则

开始任何新功能前：

1. 阅读 `docs/DESIGN.md`
2. 阅读本文件
3. 检查现有源码
4. 查找反编译原版实现
5. 确认真正存在的API、类和Hook
6. 再开始修改

不要根据名称猜测API。

不要虚构不存在的：

- 类
- Hook
- 网络接口
- 存档接口

如果不确定：

先搜索：

```text
D:\AAAPKBC\Files\Mods\StS2\StS2_Decompiled
```

以及项目当前依赖。

---

# 8. 原版实现优先

如果游戏原版已经存在某项能力，例如：

- 卡牌附魔
- 附魔合法性检查
- 遗物数字计数
- Run RNG
- 存档序列化
- 多人状态同步

优先复用原版机制。

不要无必要重新实现一套平行系统。

---

# 9. 《奇异的书》参考目标

需要研究原版具有数字计数的遗物实现。

重点参考类似：

**钢笔尖**

的数字角标机制。

《奇异的书》的：

`bookCount`

应显示为遗物右下角数字角标。

普通战斗进度：

`normalCombatProgress / normalCombatsRequiredPerBook`

应表现于书本封面。

---

# 10. 状态设计

不要把玩家状态简单保存为无法区分玩家的全局静态字段，例如：

```csharp
static int BookCount;
```

核心状态至少包括：

```text
bookCount
normalCombatProgress
```

这些状态必须：

- 能存档
- 能读档
- 能绑定正确玩家
- 能适配多人
- 能在重连时恢复

---

# 11. 书籍不能截断

禁止：

```csharp
bookCount = Math.Min(bookCount, 15);
```

15只是当前附魔台最高基础阶段阈值。

真实书籍数量必须继续增长。

所有判断应采用类似：

```text
bookCount >= 15
```

而不是把真实数据改成15。

---

# 12. 幕规则应数据驱动

避免把所有逻辑散落为：

```text
if Act == 1
else if Act == 2
else if Act == 3
```

优先设计可扩展的规则表。

当前概念：

```text
Act 1:
NormalCombatsPerBook = 1

Act 2:
NormalCombatsPerBook = 2

Act 3:
NormalCombatsPerBook = 3
```

精英：

```text
BooksPerElite = 1
```

Boss：

```text
BooksPerBoss = 2
```

未来增加第四幕或无尽模式时，应尽可能只新增规则配置，而不是重写书籍系统。

---

# 13. 所有平衡参数集中配置

至少应集中管理：

```text
0–4
5–10
11–14
>=15
```

四个书架阶段。

以及：

```text
High Slot 3:
II = 70%
III = 30%

Full Slot 2:
II = 70%
III = 30%

Full Slot 3:
II = 30%
III = 70%
```

同名附魔权重：

```text
首次：1.00
第二次：0.25
第三次：0.05
```

火堆附魔恢复：

```text
10% Max HP
```

不要把这些数字散落在UI、Patch和业务代码中。

---

# 14. RNG要求

随机附魔必须优先使用游戏已有：

**Run RNG / 同步随机系统**

不要使用：

- 当前系统时间
- 当前帧
- 本地客户端时间戳
- 未同步的 `Random`

作为附魔随机来源。

必须考虑：

- Seed可复现
- Save/Load
- 多人同步

---

# 15. 多人游戏

多人兼容属于v1.0要求。

实现相关逻辑前必须先调查原版多人架构。

必须明确：

- 哪一端拥有状态权威
- 战斗结束Hook在哪些客户端执行
- 如何同步玩家自定义数据
- 如何同步卡牌永久变化
- 如何同步随机奖励
- 如何同步事件选择
- 如何处理重连

不要自行发明一套与原版多人系统无关的网络层。

---

# 16. 防止重复结算

战斗书籍奖励：

**只能结算一次。**

幕末余数：

**只能结算一次。**

多人环境中必须防止：

Host结算一次

+

Client再次结算一次

导致重复奖励。

如果Hook在所有客户端执行：

实际状态修改必须遵循原版多人权威模型。

---

# 17. 附魔随机结果同步

多人游戏不能出现：

玩家A看到：

`Sharp II`

玩家B看到：

`Swift II`

的情况。

附魔选项必须使用：

- 原版同步RNG

或：

- 权威端生成后同步确定结果

最终所有相关客户端必须保持一致。

---

# 18. 开发顺序

不要一次实现整个Mod。

## Phase 0

已完成：

- 模板生成
- dotnet build成功
- 游戏成功加载MCEnchantingTable

这是当前已知正常基线。

---

## Phase 1

只实现：

**奇异的书**

目标：

- 注册遗物
- 显示遗物
- 显示基础图标
- 支持右下角数字计数
- 使用 BaseLib 3.4.5 的 `CustomRelicModel` 和既有自动发现架构，不建立平行注册系统
- 每名新建玩家自动获得独立的“奇异的书”实例
- 初始 `BookCount = 0`，且不设置数量上限
- `BookCount` 使用 `[SavedProperty]` 保存，并通过原版 `ShowCounter` / `DisplayAmount` 角标显示
- Phase 1 只实现 `BookCount`，不实现普通战进度

暂时不要接附魔。

本阶段也不实现 Ancient 附魔按钮、战斗掉书、幕切换、火堆、附魔槽、问号事件或自定义多人消息。

后续 Ancient 附魔采用独立于 `EventOption` 的按钮；语义为“每次 Ancient encounter，每名玩家一次机会”。当前不得将状态设计锁定为 `LastAncientEnchantActIndex` 或固定“每幕一次”。

---

## Phase 2

增加：

- 普通战斗计数
- 第一幕书籍获得
- 第二幕 `0/2、1/2`
- 第三幕 `0/3、1/3、2/3`
- 精英奖励
- Boss奖励
- 幕末余数
- 存档

### Phase 2A 奖励交付重构

- 战斗胜利只计算本场待领取书籍数量，不直接增加 `BookCount`。
- 待领取书籍使用 BaseLib 3.4.5 的 `CustomReward`，通过原版 `CombatRoom.ExtraRewards`、`RewardsSet` 和 `RewardsSetSynchronizer` 展示、保存及同步领取。
- 多本书合并为一个 `BookReward(Amount)`；只有领取时才调用 `StrangeBook.AddBooks(Amount)`。
- Boss 的基础 2 本与未完成普通战余数合并为同一个 `BookReward`，并在 Boss 胜利时清空余数；不再在 `EnterNextAct` 后台到账。
- 普通 Monster 地图节点和 Unknown 节点自然解析出的 Monster 战计数。事件经 `EventCombatSynchronizer` 启动的战斗具有 `CombatRoom.ParentEventId`，一律不计数、不生成书籍奖励。
- 当前来源资格还要求 `CurrentMapPointHistoryEntry.MapPointType` 与自然 Monster/Elite/Boss 路线匹配，避免把调试或其他非地图自然战斗误计入。
- Phase 2A 当时奖励图标使用基础图、普通战进度仅存于遗物实例；后续 Phase 2B 已补充 StrangeBook 专属 Progress Overlay，并将 PackedIcon / BigIcon 分离。此条保留为阶段历史，不代表当前 UI 状态。

### Phase 2B–2D 当前完成状态

- Phase 2B 已完成：`StrangeBook` 实现 BaseLib 3.4.5 `ICustomUiModel`，遗物栏显示独立普通战进度；PackedIcon 与 BigIcon 已分离，BookReward 使用 BigIcon，遗物栏与 DoFlash 使用 PackedIcon。
- Phase 2C 已完成入口框架：`AncientEnchantOption` 是 `NAncientEventLayout` 内的独立图片按钮，保留点击、机会保存、多人消息、对话末行开放、ClearDialogue 隐藏、Hover/Press 与原版音效。当前为固定 Anchor 稳定布局，不实现选牌或真实附魔。
- Phase 2D 已完成入口与领取反馈：`EnchantRestSiteOption` 使用原版 Rest Site Option 生命周期和自定义横向 PNG，当前效果仅为独立 10% 最大生命恢复；BookReward 使用临时 `NRelicInventoryHolder` 和原版 `PlayNewlyAcquiredAnimation()`，动画结束后增加 BookCount，并由原版 RewardClaimed 流程清理按钮。
- 尚未实现：卡牌选择、`EnchantmentModel`、`CardCmd.Enchant`、附魔等级/槽位、问号事件及其他 Phase 3 以后内容。

---

## Phase 3

研究并实现：

- 多人书籍同步
- 普通战进度同步
- 多人读档/重连

在继续复杂附魔系统前，优先证明核心成长数据可以多人同步。

---

## Phase 4

实现：

- I / II / III
- 书架阶段
- 附魔槽概率
- 卡牌合法附魔池
- 同名附魔降权

---

## Phase 5

实现：

- 先古之民附魔
- 火堆附魔
- 10%最大生命恢复
- 火堆附魔独立恢复（不受皇家枕头影响）

---

## Phase 6

实现：

- 附魔主题问号事件
- UI完善
- 本地化
- 音效/动画等表现

---

# 19. 每个Phase完成要求

每完成一个阶段：

1. 运行：

```powershell
dotnet build
```

2. 修复所有新增编译错误。
3. 不删除设计需求来绕过错误。
4. 列出修改文件。
5. 简述每个文件职责。
6. 列出新增Patch/Hook。
7. 说明实际参考的原版类。
8. 说明仍存在的风险。
9. 等待实机测试结果再继续下一阶段。

---

# 20. Git建议

当前工程已经成功编译并成功被游戏加载。

建议立即创建一个Git基线提交。

例如：

```text
initial-working-template
```

以后每一个大型Phase单独提交。

如果某次Codex修改导致Mod完全无法加载：

应优先通过Git比较和回退，而不是在未知状态下继续叠加修复。

---

# 21. 当前最重要的原则

**先保证可运行，再增加功能。**

当前模板是已知可运行状态。

任何大规模修改都必须有明确理由。

在没有验证前：

不要同时修改：

- 存档
- 多人
- RNG
- 火堆
- 附魔
- UI

多个核心系统。

每次只建立一个可测试的最小闭环。

当前开发、编译、反编译参考和实机测试环境均为《Slay the Spire 2》Beta 分支。
当前反编译源码 StS2_Decompiled 对应当前安装的 Beta 游戏版本。
当前 Mod 首要开发目标为 Beta 分支。
正式版兼容属于发布前兼容目标；在进行正式版兼容时，应单独获取对应正式版程序集与反编译源码，不应假设 Beta API 与 Stable API 完全一致。

---

# 22. 玩家可见文本的双语完成标准

任何新增玩家可见内容必须同时提供 `zhs` 与 `eng` 本地化，包括遗物、奖励、进度提示、火堆与 Ancient 选项、附魔名称与说明、问号事件与 `EventOption`、配置界面、错误及无合法卡牌提示，以及未来的附魔书和铁砧文本。

玩家可见文本必须进入对应 Localization 文件，不得在 C# 业务代码中硬编码。缺少任一语言时，该功能不视为完成。

## Text Localization & Keyword Highlight Rules

Localization 负责玩家可见内容，原版 LocString / Keyword Highlight 负责语义颜色。禁止在 C# 中硬编码“附魔 / Enchant”“升级 / Upgrade”“牌组 / Deck”等文本，也禁止通过 Label Modulate、字体颜色覆盖或运行时拼接 RichText 标签实现高亮。

当前关键词约定：普通数量使用 `[blue]`；“升级 / Upgrade”等游戏动作关键词使用 `[gold]`；MCEnchantingTable 核心系统关键词“附魔 / Enchant”使用 `[purple]`。未来新增关键词必须先核对当前 Beta 原版 Localization 的既有标签与颜色语义，不得仅凭视觉偏好决定。

当前检查结果：`EnchantScreen` 直接复用原版 `card_selection/TO_ENCHANT`，其中“附魔 / Enchant”为 `[purple]`；Rest Site Tooltip 的 `OPTION_MCENCHANTINGTABLE_ENCHANT.description` 同样使用 `[purple]`；`AncientEnchantOption` 当前不创建 Tooltip 或 Hover 文字，其本地化名称仅保留供其他界面或未来功能使用。

“奇异的书”是遗物与战斗书籍资源共用的正式名称：简体中文为“奇异的书”，English 为“Strange Book”。`BookReward` 必须通过独立的 CustomReward localization key 显示同一名称，禁止退回“书 / Book”；奖励数量由 `BookReward.Amount` 的本地化参数表达，遗物角标 `BookCount` 表示累计持有量。

Phase 2B 使用 BaseLib 3.4.5 的 `ICustomUiModel.CreateCustomUi(Control)` 为 `StrangeBook` 创建临时遗物 UI。Overlay 只在其祖先为 `NRelicInventoryHolder` 时显示；BaseLib 在 `NRelic.Reload` 时负责移除并重建临时 UI。进度刷新由遗物实例事件与原版 `RunManager.ActEntered` 驱动，不得把刷新绑定到 `BookCount`，也不得向多人网络层发送 UI 消息。

---

# 23. Known Limitation — Disabling the Mod Mid-Run

使用 MCEnchantingTable 创建并保存的 Run 属于 **MCEnchantingTable-dependent Run**。当前 Beta / BaseLib 3.4.5 没有完整的 Required Mods、Missing Mods 或 Save Dependency Validation 机制，不能在 Continue 前可靠阻止缺少该 Mod 的旧 Run 被加载。

当前已确认的风险如下：

1. 每名玩家开局即获得 StrangeBook，因此第一次保存后 Run 已经在语义上依赖 MCEnchantingTable。
2. 如果存档中有尚未领取的 BookReward，`SerializableReward` 会保存 MCEnchantingTable 自定义 `RewardType`，并用 `GoldAmount` 保存 `BookReward.Amount`。
3. Mod 未加载时，`BookReward.Initialize()` 不会执行，BaseLib 注册表中没有该 RewardType。BaseLib warning 后回退到原版 `Reward.FromSerializable()`，原版对未知 RewardType 抛出 `NotImplementedException`。
4. 已观察到的调用链为：`NMainMenu.OnContinueButtonPressedAsync` → `RunState.FromSerializable` → `FadeOut` → `NGame.LoadRun` → `AbstractRoom.FromSerializable` → `CombatRoom.FromSerializable` → `Reward.FromSerializable` → `NotImplementedException`。异常发生在 FadeOut 后且正常 FadeIn 未执行，玩家看到黑屏。
5. 即使没有 pending BookReward，未知 StrangeBook 也可能经 `SaveUtil.RelicOrDeprecated` 降级为 `DeprecatedRelic`；其 `BookCount`、`NormalCombatProgress` 和当前 Run rules snapshot 可能被忽略或丢失。若随后再次保存，原本可恢复的 Mod 状态可能被永久覆盖。

正式支持策略：

- 不支持在 Run 进行途中禁用或卸载 MCEnchantingTable。
- 如果玩家误关闭，必须在按 Continue 前重新启用 MCEnchantingTable。
- 这是一项 **Known Limitation / Framework Limitation**，不是已经修复的程序 Bug。
- 不通过伪装原版 Reward/Relic、取消序列化、静默丢弃未知数据或直接修改 Save JSON 来规避；这些方案会造成状态丢失和多人 divergence。

## Future TODO — Missing Mods 与 Safe Uninstall

优先等待或推动 Beta/BaseLib 提供通用的 Required Mod metadata 和 Continue pre-validation：保存时记录所需 Mod ID，在 FadeOut 和 `NGame.LoadRun` 前比较存档依赖与当前 loaded mods；缺少 `MCEnchantingTable` 时直接显示提示。MCEnchantingTable 自己无法在“自身完全未加载”时可靠执行该检查，consumer 必须位于 Beta、BaseLib 或其他始终加载的通用框架。

后期可以考虑只在 MCEnchantingTable 仍启用时主动执行 Safe Uninstall。它必须完整移除 StrangeBook、BookCount、NormalCombatProgress、rules snapshot、pending BookReward、自定义 Event/Ancient/Rest Site 状态、卡牌附魔、附魔书、铁砧、多重附魔及所有其他 `MCENCHANTINGTABLE` Model/Reward/Saved data；保存后最好重新读取 `SerializableRun`，确认无任何残留后才能提示可以安全禁用。附魔系统未完成前不实现该功能。

---

# 24. Reference Mods 只读规则

成熟参考 Mod 位于：

`D:\AAAPKBC\Files\Mods\StS2\References`

详细版本、兼容性、关键类与适用模块记录在 [REFERENCE_MODS.md](REFERENCE_MODS.md)。

固定规则：

- Reference Mod 始终只读，不编辑、格式化、覆盖、清理或在原目录生成反编译结果。
- 当前三个包均为 Binary-only reference；DLL 反编译结果不是原作者源码，只供 API/架构调查，不复制大段代码。
- DLL 优先使用非交互式 .NET metadata/decompiler；PCK 只用于 scene、UI、localization、texture 和资源目录结构。
- PCK/GDRE 失败不阻断调查。本轮 GDRE Tools 发生 native access violation 后已停止，不把它归类为 MCEnchantingTable build failure，也不据此推断 Godot 4.5.1 或本项目 PCK Export 失效。
- Reference Mod 是实现案例，不是 API authority。所有 API 先对照当前 `StS2_Decompiled`，所有 BaseLib 行为先对照当前实际使用的 BaseLib 3.4.5。
- 旧 Beta、旧 BaseLib、私有 framework、反射 Patch 和兼容 fork 只能参考设计模式，不能直接复制签名。
- 采用任何非平凡实现前必须检查生命周期、保存格式、多人权威端/RNG、许可证和当前项目是否已有更小的 BaseLib 路线。
- 有价值的类和调查限制持续记录到 `docs/REFERENCE_MODS.md`。

当前 Phase 2B 结论：三个 Reference Mod 都没有发现可直接复用的单个遗物动态 Progress Overlay；当前实现以 BaseLib `ICustomUiModel.CreateCustomUi(Control)` 为主。此前 Reference Mods 调查轮次本身没有修改 StrangeBook UI 或 Gameplay 代码；其后的 Phase 2B 实现已经完成并实机验证。

---

# 25. Phase 2C Ancient Side Action 规则

Ancient 的“附魔”入口禁止实现成 `EventOption`。`EventOption` 属于原版 Ancient 叙事选择，会进入 `CurrentOptions`、option index 同步、`EventSynchronizer.ChooseOption`、Event History 和完成状态；附魔入口则是房间内的独立 side action，使用后仍必须允许玩家选择原版 Ancient 选项。

当前 Beta 的可靠开放时机是 `NAncientEventLayout.SetDialogueLineAndAnimate(int lineIndex)`：当 `lineIndex` 到达 `_dialogue.Count - 1` 时，原版隐藏 next button、关闭 dialogue hitbox 并启用 option buttons。本项目只在该方法完成后开放附魔按钮，不轮询文字、不使用 `_Process` 检查对话。

按钮由 `NEventLayout.SetEvent` Postfix 创建并直接挂在 `NAncientEventLayout`，使用相对 Anchor 定位。layout 离开树时按钮自动销毁；节点名检查防止同一 layout 重复创建。Ancient 状态变化触发 `ClearDialogue()` 后按钮立即隐藏，且不修改原版选项。

机会语义为“每个 Ancient encounter、每名玩家一次”，不是“每幕一次”。状态使用 StrangeBook 实例上的 `[SavedProperty] LastAncientEnchantEncounterKey`。key 由 act index、当前 map coord、total floor 与完整 Ancient `ModelId` 组成；不使用跨存档不稳定的 `AbstractRoom.Id`。

本功能的按钮点击是新的玩家操作，不能依赖 `[SavedProperty]` 自动实时广播。多人使用一条必要的 BaseLib `ICustomTargetedMessage`，按当前 `RunLocation` 路由；接收端只用网络层提供的 `senderId` 找到该玩家的 Ancient 和 StrangeBook，并重新校验 Ancient ID 与 encounter key。没有 UI static、全局玩家状态 Dictionary 或每帧同步。

## Phase 2C UI 实机修正与当前稳定状态

历史记录：初版按钮直接使用百分比 Anchor、纯色 `StyleBoxFlat` 和 Button.Text。实机证明该视觉不符合目标，且资源包未包含新 localization 时会把 key 直接显示在按钮上。后续版本改为图片按钮，并经历 ContentContainer、EventOption 索引、可见项筛选和屏幕坐标排序等自动定位尝试；这些自动定位方案现已全部回退，不是当前实现。

当前类名与节点结构是 `AncientEnchantOption : Button -> Control("Visuals") -> TextureRect("Icon")`。TextureRect 从 `MCEnchantingTableAssets.AncientAssets.EnchantButtonPath` 加载 `res://MCEnchantingTable/images/ancient/ancient_enchant_button.png`，使用 `KeepAspectCentered`，子节点忽略鼠标；Button 的 normal、hover、pressed、focus 和 disabled StyleBox 均为空。不使用 `ColorRect`、Panel、Label 或 C# 绘制背景。

按钮父节点仍是全屏 `NAncientEventLayout`。当前稳定位置完全来自 layout 相对 Anchor：`FixedAnchorX = 0.82`、`FixedAnchorY = 0.50`，点击区域 132×132；不依赖 `%ContentContainer`、`OptionsContainer`、`NEventOptionButton`、节点索引、GlobalRect、GlobalPosition、排序、clamp、Resized 或 deferred reposition，也没有 `_Process` 轮询。`Visuals` 基础 Scale 为 1.2；Hover/Press 分别使用原版 Event Option 的 1.01/0.99 比例和 `ui_hover`/`ui_click` 音效，缩放不改变 Anchor 或点击区域。

按钮不设置可见 `Text`，也不创建 Tooltip/Hover 文字。`gameplay_ui/MCENCHANTINGTABLE-ANCIENT_ENCHANT_BUTTON` 的 `zhs = 附魔`、`eng = Enchant` 仍保留，供其他界面或未来用途。发布测试必须使用新生成的 PCK，因为单独 `dotnet build` 只部署 DLL、manifest 和 PDB，不会刷新图片及 localization。

图片资源统一由 `MCEnchantingTableAssets` 分类管理：`RelicAssets.StrangeBookPackedIconPath`、`RelicAssets.StrangeBookBigIconPath`、`AncientAssets.EnchantButtonPath`、`RestSiteAssets.EnchantButtonPath`。业务代码不保存旧路径字符串。

StrangeBook BigIcon 为 256×256 透明 PNG，PackedIcon 为独立的约 85×85 遗物栏资源。`StrangeBook.CustomRelicModel` 的 `PackedIconPath`/outline 指向 packed 文件，`BigIconPath` 与 `BookReward.IconPath` 指向 big 文件。Phase 2B Progress Overlay 继续使用遗物节点内部坐标，不新增另一套 overlay 系统。

---

# 26. Phase 2D Rest Site Option

当前 Beta `RestSiteOption.Generate(Player)` 固定生成 Heal、Smith，多人时再生成 Mend，随后调用 `Hook.ModifyRestSiteOptions`。Phase 2D 使用 Generate Postfix 追加 `EnchantRestSiteOption`，并按 `OptionId` 防重复。当前 ModB `CampfireTrade.AddTradeRestSiteOptionPatch` 使用相同的 Generate Postfix 模式，是本轮 Reference Mod 参考；实际 API 与同步结论以 Beta 反编译源码为准。

`NRestSiteRoom.UpdateRestSiteOptions()` 会为列表中的每个 option 调用 `NRestSiteButton.Create(option)`，因此自定义选项自动获得原版按钮场景、焦点导航、hover description、动画和 controller 支持。禁止为 Phase 2D 修改 `NRestSiteRoom` 场景或创建独立 TextureRect 按钮。

`RestSiteOption.Icon` 并非 virtual，而且根据 `OptionId` 自动构造 `ui/rest_site/option_<id>.png`。当前 getter Patch 仅对 `EnchantRestSiteOption` 返回 `MCEnchantingTableAssets.RestSiteAssets.EnchantButtonPath`，实际文件为 `res://MCEnchantingTable/images/rest_site/campfire_enchant_button.png`；`AssetPaths` 同时声明该图片和休息 VFX。按钮继续由原版 `NRestSiteButton` 提供 Outline、Hover、Press、焦点和 controller 行为，不额外绘制 Hover 描边。

不能调用 `HealRestSiteOption.ExecuteRestSiteHeal()`，因为附魔不是 Rest/Heal 行为。`EnchantController.ExecuteRestSiteEnchant()` 只计算 `MaxHp * 0.10m` 并调用通用 `CreatureCmd.Heal`；不调用 `Hook.ModifyRestSiteHealAmount`、`Hook.AfterRestSiteHeal`、`Hook.ModifyRestSiteHealRewards` 或 `RewardsCmd.OfferCustom`。因此 `RegalPillow` 和其他 Rest heal Modifier 不会参与。local VFX 仍复用休息音效、`NRestSmokeVfx` 和 `NDesaturateTransitionVfx`，这些只是表现，不改变效果语义。

`RestSiteSynchronizer.BeginRestSite()` 为每个玩家分别调用 Generate，`ChooseLocalOption(index)` 发送 `OptionIndexChosenMessage` 后只对 LocalPlayer 执行，远端收到消息后按 sender 对应 Player 执行相同 index。只要所有 peer 确定性追加相同 OptionId/顺序，玩家 A 选择附魔、玩家 B 选择休息就是原生支持的，不需要自定义消息。成功后原版写入 `RestSiteChoices` 并按 `ShouldDisableRemainingRestSiteOptions` 完成火堆行动。

本阶段不新增保存字段。Continue 后 Rest Site options 会从当前玩家及 RunState 重新 Generate；已经成功完成的操作依赖原版 room/history/save 流程。

## BookReward 原版遗物飞行动画

原版 `RelicReward.OnSelect()` 只通过 `RelicCmd.Obtain()` 增加遗物；带奖励栏起点的飞行动画实际发生在 `NRewardButton.GetReward()` 选择成功之后：`NRelicInventory.AnimateRelic(claimedRelic, _iconContainer.GlobalPosition)` → `NRelicInventoryHolder.PlayNewlyAcquiredAnimation()`。

BookReward 不能调用 `RelicCmd.Obtain`，因为 StrangeBook 已存在且书籍是资源。`BookRewardAnimationPatch` 在 `NRewardButton.GetReward()` Prefix 同时捕获当前 `NRewardButton` 与其 `_iconContainer`，写入对应 BookReward 的瞬时 UI 引用，不参与序列化。

不能直接对现有 StrangeBook holder 调用 `PlayNewlyAcquiredAnimation()`：该方法会移动 holder 内真实 `_relic.Icon`，正是第一版动画期间遗物栏图标消失的原因。当前实现创建临时 `NRelicInventoryHolder`，将其 global position 和 Control size 对齐真实 holder，隐藏临时 amount/progress overlay，只让临时 Icon 从奖励位置飞回；真实 holder 从不移动或隐藏。飞行动画开始后隐藏完整的 BookReward RewardButton，使其退出 Container 视觉布局；不手动删除按钮，也不使用 Timer 延迟清理。

原版 `NRelicInventory.AnimateRelic()` 是 fire-and-forget，无法保证 `BookCount` 在动画后更新。因此 `BookRewardRelicAnimation` 直接 await 临时 holder 的同一个 public animation，并等待其内部 `_obtainedTween` 的 `Finished` 信号；没有创建新 Tween。完成后才调用 `StrangeBook.AddBooks(Amount)`，随后播放一次原版 relic pickup sound。`OnSelect()` 返回成功后，仍由原版 `NRewardButton` 发出 `RewardClaimed`，并由 `NRewardsScreen.RewardCollectedFrom()` / `RemoveButton()` 完成正式清理。远端 peer 没有本地奖励按钮起点，不播放动画或声音，但仍由现有 `RewardsSetSynchronizer` 执行相同奖励状态变化。

历史记录：旧版为兼容 1254×1254 BigIcon 曾加入运行时 `ImageTexture` 归一化、临时-holder metadata，并短暂尝试 BookReward 专用 Ripple Scale。上述代码以及 `NRelicInventoryHolder.DoFlash()` Prefix 均已全部移除。当前 BigIcon 使用 `RelicAssets.StrangeBookBigIconPath`，奖励栏显示该 256×256 图片；临时 holder 的原版 DoFlash 使用模型的 `PackedIcon`，即 `RelicAssets.StrangeBookPackedIconPath`。当前不 Patch DoFlash，也不修改 Ripple scene、Scale、粒子数量、Curve、Alpha、Duration 或动画速度。

---

# 27. Known Limitation — Godot Headless Export 类型扫描日志

Godot Headless Export 执行 `ScriptManagerBridge` 程序集扫描时，可能针对 `MCEnchantingTable.dll` 输出以下 `TypeLoadException`：

```text
Could not resolve type
MCEnchantingTable.MCEnchantingTableCode.UI.Enchant.EnchantScreen
```

该问题来自 Headless Export 环境不包含完整的 StS2 游戏程序集上下文。`EnchantScreen` 依赖游戏程序集中的 UI、Overlay 与卡牌选择类型，因此独立 Headless 扫描可能无法完整解析该类型。

当前确认的影响范围：

- 不影响 `dotnet publish` 完成；
- 不影响 PCK 生成，导出日志仍会完成 `savepack DONE`；
- 不代表游戏运行时 Mod 加载失败。

验证方式是实机启动 MCEnchantingTable，并正常进入 `EnchantScreen`。游戏运行时拥有完整的 StS2 与 Mod 依赖程序集上下文，与独立 Headless Export 环境不同。

本限制当前只记录，不修改 `MCEnchantingTable.csproj`、`Private=false` 引用或程序集复制策略。
