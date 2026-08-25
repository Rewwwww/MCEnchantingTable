# Reference Mods

本文是 `D:\AAAPKBC\Files\Mods\StS2\References` 下成熟 Mod 的只读索引。它们是社区实现案例，不是当前 Beta API 的权威定义，也不是可以直接复制的源码模板。

## 1. 调查方法与证据边界

- `ModA`、当前 `ModB`、`ModC` 的发布目录是 binary-only；`ModA_Source`、`ModC_Source`、`ModD_Source` 提供可读源码参考。目录名只是索引，必须以当前 manifest 重新识别内容。
- 本轮使用 ILSpy 9.1 命令行版对 DLL 做只读反编译。反编译输出位于系统临时目录，没有写回 Reference Mod 原目录。
- 反编译结果仅用于确认类型、方法、调用关系和资源路径；它不是原作者源码，不应复制大段反编译代码。
- 对带版本变体的 Mod，优先分析 `lib\0.111.0`，因为当前主要目标是 Beta `0.111.x`。
- GDRE Tools 在尝试进入 Reference 资源调查时发生 native access violation（错误地址附近为 `0x0000000000000050`）。本轮没有完成任何具体 PCK 的提取，因而无法诚实归因到某一个 PCK；记录为 **PCK extraction unavailable / GDRE crash**，并停止重试。
- PCK 未解包不影响 DLL/API 调查。Localization 文件内容、Godot scene 节点树、纹理实际布局等只能标记为未验证；不能根据 DLL 中的路径字符串假装已经检查过资源。

证据优先级固定为：

1. 当前 Beta 反编译源码 `StS2_Decompiled`；
2. 当前实际使用的 BaseLib 3.4.5；
3. 本文列出的 Reference Mod 实现；
4. 新设计的兼容层。

## 2. 快速索引

| 目录 | Mod | 版本 / 目标 | BaseLib | 当前 Beta 兼容性 | 最有价值的参考 |
| --- | --- | --- | --- | --- | --- |
| `References\ModA` | 猪猪 (`YuWanCard`) | `v0.5.12`; 有 `0.111.0` DLL | manifest 未声明 | High（版本接近，但自带大型框架） | Ancient/Event、SavedProperty 扩展、复杂目录组织 |
| `References\ModB` | CampfireTrade (`CampfireTrade`) | `v0.1.1`; min `0.107.0` | `>= 3.3.8` | Medium | 精简 RestSiteOption、原版选牌 Screen、PlayerChoiceSynchronizer |
| `References\ModC` | 海克斯符文 (`HextechRunes`) | `0.9.1`; 有 `0.111.0` DLL | manifest 未声明 | High（版本接近，但主要用自有 Harmony 框架） | 多选 Screen、Reward 保存恢复、SavedProperty、多人数值选择 |
| `References\ModD_Source` | 你画瓦猜 (`DrawAndGuessMod`) | `0.9.0`; min `0.111.0` | 不使用 BaseLib；依赖 RitsuLib `0.5.12` | High（源码/0.111） | Neow 独立 Side Button、自定义 Screen、复杂多人绘图 |

“High”只表示该包提供了与当前 Beta 接近的二进制变体，不表示其私有框架或反射 Patch 可以直接移植。

## 3. ModA — 猪猪 / YuWanCard

### 基础信息

- 路径：`References\ModA`
- ID：`YuWanCard`
- 名称：`猪猪`
- 作者：`一条鱼丸_`
- 版本：`v0.5.12`
- `min_game_version`：`0.107.1`
- 二进制：入口 `YuWanCard.dll`，内容程序集 `lib\0.107.1\YuWanCard.Content.dll` 与 `lib\0.111.0\YuWanCard.Content.dll`
- BaseLib：manifest 和已反编译的 `0.111.0` Content 程序集均未显示 BaseLib 依赖；它使用自己的注册、保存、配置和 Patch 框架。
- 评级：**High（设计参考）**。存在 `0.111.0` 目标，但大量功能依赖 `YuWanCard.Core.*`、反射和自定义兼容层，具体 API 必须重新核对。

### 结构与初始化

这是三个包中规模最大、分层最完整的一个。反编译命名空间包括 `YuWanCard.Cards`、`Relics`、`Enchantments`、`Events`、`Ancients`、`RestSite`、`UI`、`Multiplayer`、`Config`、`Core.Patches`、`Core.Persistence`、`Core.Registration` 等。

值得回看的入口：

- `YuWanCard.MainFile.Initialize()`：Mod 入口。
- `YuWanCard.Core.Patching.ModPatcher`：集中安装 Harmony Patch。
- `YuWanCard.Core.Registration.SavedPropertyRegistration.RegisterAssembly()`：扫描带 `[SavedProperty]` 的模型类型并注册。
- `YuWanCard.Core.Persistence.SavedAttachedStateRegistry`：把无法直接放在模型属性上的附加状态导出/导入 `SavedProperties`。

这套 `Core` 是该 Mod 私有基础设施，不应在 MCEnchantingTable 中另建一套平行框架。

### Localization 与 Config

- DLL 中广泛使用 `LocString`，例如 Ancient 选项使用 `relics` 表和 `YUWANCARD-*` key。
- DLL 可见资源根为 `res://YuWanCard/...`，并可见自定义 Ancient scene 路径。
- 因 PCK 未解包，具体 `zhs`/`eng` 文件、fallback 和资源导入结果未验证。
- 配置入口可见于 `YuWanCard.RitsuConfigRuntimeBridge`、`YuWanCard.Config.YuWanContentSettingsSnapshot` 等；这是 RitsuLib/私有桥接方式，不是 BaseLib 3.4.5 配置范例。

### Ancient / Event / Rest Site

- `YuWanCard.Patches.StartingAncientOptionsPatch.ModifyInitialOptions()` Patch `AncientEventModel.GenerateInitialOptionsWrapper`，保存原始 description/options 后替换为自己的 `EventOption` 集合。
- `StartingAncientOptionsPatch.SetEventState()` 通过反射调用 EventModel 私有 `SetEventState`；完成后再恢复原选项。
- `YuWanCard.Patches.StartingAncientSetEventStatePatch` 与 `YuWanCard.Core.Patches.CustomAncientRegistry` 也是相关入口。
- `YuWanCard.Ancients.PigPig` 是自定义 Ancient 内容。
- `YuWanCard.RestSite.RoastPorkRestSiteOption.OnSelect()` 是自定义 `RestSiteOption`；`RestSiteOptionIconPatch` 和 `SmithRestSiteOptionPatch` 处理图标/原版选项。

重要限制：这里展示的是 **替换/插入 EventOption** 的做法，没有确认到独立于 `EventOption` 的 Ancient 额外按钮。因此它不能直接决定 MCEnchantingTable 未来独立“附魔”按钮的 UI 位置或每玩家状态模型。

### Save 与 Multiplayer

- 大量模型使用 `[SavedProperty]`。
- `SavedPropertyRegistration` 将自定义模型类型加入缓存；`SavedAttachedStateRegistry` 支持 `int`、`bool`、`string`、`ModelId`、数组和 `SerializableCard` 等附加状态。
- `YuWanCard.Core.Patches.ManagedActionNetPatchHelpers`、`YuWanCard.Core.Multiplayer.*` 以及 `TeammatePayGoldQueryMessage` / `ResponseMessage` 展示了自定义网络动作和消息。
- 若只需要 MCEnchantingTable 已有的模型实例 `[SavedProperty]`，不应引入这套附加状态框架。

### Relic UI / Custom Screen

- 可见 `HextechRelicInventorySafetyPatch`、`RightClickRelicPatch`、遗物图鉴/图标路径 Patch，以及 `ShoppingCartPopup`、`TeammateSelectionPopup` 等自定义窗口。
- 没有发现给某个 `NRelic` 增加动态 Label/进度 Overlay 的明确实现。
- `NCardAncientVisualOverlayPatch` 是卡牌 Overlay，不是遗物 Overlay。

### 适合与不适合参考

适合：

- Ancient/Event 状态暂存与恢复的风险点；
- 复杂 Mod 的模块分层；
- 自定义保存注册、附加状态和网络动作的架构思路；
- Rest Site 和复杂 Popup 生命周期。

不适合直接参考：

- BaseLib 注册或 BaseLib Config；
- StrangeBook Progress Overlay；
- Ancient 独立按钮；
- 将私有 `YuWanCard.Core` 框架复制进本项目。

## 4. ModB — CampfireTrade（已替换）

> `References\ModB` 已在 2026-08-23 被替换。此前记录的 `STS2Trade v1.0.1rs`、`TradeSynchronizer`、`NTradeScreen` 和自定义 Trade Message 等结论全部失效，不得再以旧 ModB 路径引用。

### 基础信息

- 路径：`References\ModB`
- ID / 名称：`CampfireTrade`
- 作者：`Sterdo`
- 版本：`v0.1.1`
- `min_game_version`：`0.107.0`
- BaseLib：manifest 明确依赖 `BaseLib >= 3.3.8`
- Source：当前没有对应 `ModB_Source`，仍是 DLL/PCK/manifest 的 Binary-only reference。
- 评级：**Medium**。仍以 `0.107.0` 为最低游戏版本，但 BaseLib 版本和代码结构比旧 ModB 更接近当前项目；精确 API 仍须核对 Beta 0.111/BaseLib 3.4.5。

### 初始化、Config 与 Localization

- `CampfireTrade.CampfireTradeCode.MainFile.Initialize()`：`[ModInitializer]` 入口，注册 `CampfireTradeConfig : SimpleModConfig` 并执行 Harmony Patch。
- Config 使用 BaseLib section、conditional visibility 和 debug settings；没有 `[SavedProperty]`。
- DLL 使用 `LocString("rest_site_ui", "OPTION_TRADE.*")`。PCK 未解包，语言文件和 fallback 未验证。

### Rest Site 与 Custom Screen

- `CampfireTrade.CampfireTradeCode.Patches.AddTradeRestSiteOptionPatch.Postfix()` Patch `RestSiteOption.Generate`，在多人或 debug solo popup 设置开启时追加 `TradeRestSiteOption`，并按 `OptionId` 防重复。
- `TradeRestSiteOption : RestSiteOption`，`OptionId => "TRADE"`；`OnSelect()` 为每名玩家向 `PlayerChoiceSynchronizer` 预留 choice id，并区分本地和远端流程。
- `CampfireTrade.CampfireTradeCode.Trading.TradeFlow` 使用 `PlayerChoiceSynchronizer` 同步目标玩家、卡牌 index 列表和接受/拒绝结果。
- 选牌直接复用原版 `NDeckCardSelectScreen` / `NOverlayStack`；交易请求使用 `NTradeRequestPopup` / `TradeRequestPopupController` 和原版 generic popup 资产。

这是三个参考中 **Rest Site 自定义按钮和多人自定义 Screen 最直接的案例**。移植前仍须用当前 `StS2_Decompiled` 核对 `RestSiteOption.Generate`、`NTargetManager`、overlay parent 和 choice 同步签名。

### Multiplayer

- 当前 ModB 没有发现自定义 `INetMessage`；核心流程以连续的 `PlayerChoiceSynchronizer.ReserveChoiceId`、`SyncLocalChoice`、`WaitForRemoteChoice` 完成。
- 与旧 ModB 的“自定义 TradeSynchronizer + 多种 Message”相比，当前版本更精简，更适合参考一次性目标/卡牌/确认选择；旧版本更适合长生命周期报价会话，但它已不再位于 References，不能继续作为当前 ModB 证据。

Phase 2D 采用当前 ModB 的 `AddTradeRestSiteOptionPatch` 最小模式：Postfix `RestSiteOption.Generate(Player)`，按唯一 `OptionId` 防重复并追加自定义 `RestSiteOption`。没有复制其交易弹窗、选牌或业务逻辑；火堆选择继续由原版 `RestSiteSynchronizer` 处理。

### Reward / Save / Relic UI / Ancient

- 未发现 Custom Reward、`[SavedProperty]`、ExtendedSave、save migration 或长期交易状态保存。
- 未发现 `NRelicInventoryHolder` Overlay、自定义 Ancient/Event。
- 它主要处理即时交易会话，不是长期 Run 状态或 Missing Mod 兼容示例。

### 适合与不适合参考

适合：

- Rest Site 选项注入；
- 原版选牌 Screen 与 Popup 的组合；
- 纯 `PlayerChoiceSynchronizer` 的多阶段玩家交互；
- BaseLib 3.3.8 `SimpleModConfig` 的社区用法。

不适合直接参考：

- 当前 BaseLib 3.4.5 精确 API、长期会话状态；
- Reward、Save migration、Ancient；
- StrangeBook Progress Overlay；
- 当前 Beta 的权威端/RNG 结论。

## 5. ModC — 海克斯符文 / HextechRunes

### 基础信息

- 路径：`References\ModC`
- ID / 名称：`HextechRunes` / `海克斯符文`
- 作者：`Natsuki`
- 版本：`0.9.1`
- `min_game_version`：`0.107.1`
- 二进制变体：`0.107.1`、`0.110.0`、`0.111.0`
- BaseLib：manifest 和已反编译程序集未显示 BaseLib 依赖；主要使用 Harmony 和自己的 hooks/coordinators。
- 评级：**High（设计参考）**。有当前 `0.111.0` 变体，但其兼容性保护、反射和保存注册规模很大，不能当作最小实现模板。

### 初始化、Localization 与 Config

- `HextechRunes.ModEntry.Initialize()` 是入口，按 hook group 安装功能。
- `HextechContentRegistry` 负责内容注册；`HextechRunLifecycleHooks` 处理 Run/幕生命周期。
- `HextechRuneConfigMenuHooks` 与 `HextechRelicVisibilityHooks.ModUiConfig` 实现自定义设置/JSON 配置；后者显式读写 `ui_config.json`，不是 BaseLib Config。
- DLL 广泛使用 `LocString`；manifest 明确声称中英文文本。由于 PCK 未解包，具体 `zhs`/`eng` 文件和 fallback 未验证。

### Custom Reward 与 Save

- `HextechForgeChoiceReward : Reward` 提供 `RewardType`、`RewardsSetIndex`、`Description`、`IconPath`、`Populate()`、`OnSelect()`、`ToSerializable()` 和 `TryFromSavedReward()`。
- 它是“多个遗物候选项 + 保存/恢复”的实际案例，适合研究 Reward 生命周期；但它不是 BaseLib `CustomReward`，并用了自有恢复 hooks，不能替换本项目已经稳定的 BookReward/BaseLib 架构。
- 大量模型带 `[SavedProperty]`。
- `HextechSavedPropertyBootstrap` 负责让属性载体进入保存类型缓存；`HextechSavedPropertyNetIdHooks` 审核/规范化属性 net id；`HextechMultiplayerCompatibilityHooks` 对未知属性名/net id 做兼容诊断。
- 这些是复杂兼容保护范例，不等于 Missing Mod 预验证，也不应无需要地移植。

### Custom Screen 与 Multiplayer Selection

- `HextechRuneSelectionScreen` 是多个符文候选项、reroll、确认和输入状态的自定义界面。
- `HextechRuneSelectionCoordinator.HandleActSelection()` / `HandleStageSelection()` 组织幕开始选择。
- `SelectRuneMultiplayer()`、`CreateRuneSelectionScreenAsync()`、`CreateRuneChoiceResult()`、`ResolveRemoteRuneChoice()` 将本地 Screen 与 `PlayerChoiceSynchronizer` 连接。
- Coordinator 会区分本地选择与远端等待，并以 `ModelId`/choice result 恢复候选与结果。
- 多人确定性选择相关方法包括 `RollStableRarity()`、`BuildStableSelectableRunesForRarity()`、`GetMultiplayerRerollIndex()`；它显示“同步输入/稳定种子派生”思路，但必须在当前游戏 RNG API 上重新设计，不能复制内部算法。

这是三个参考中 **未来附魔候选界面与多人选择协调最值得回看的实现**。

### Rest Site / Ancient / Relic UI

- `StokeRestSiteOption : RestSiteOption`，`OnSelect()` 使用 `CardSelectCmd.FromDeckForRemoval` 打开选牌并返回完成状态。
- `HextechRunLifecycleHooks` 能接触 Ancient 生命周期，但未发现独立 Ancient UI 按钮或完整 EventLayout 扩展示例。
- `HextechRelicVisibilityHooks` 会在全局 UI 中动态创建隐藏遗物开关，并接触 `NRelicInventoryHolder`；它用于整体遗物可见性/折叠，不是给单个 `NRelic` 添加进度 Label。
- `HextechUiSafetyHooks` 处理部分遗物 UI 安全性，同样没有形成可直接复用的单遗物 Overlay。

### 适合与不适合参考

适合：

- 多候选 Custom Screen；
- 本地 UI 与远端选择等待的协调；
- Reward 的保存/恢复生命周期；
- 大量 `[SavedProperty]` 与 multiplayer net-id 兼容诊断；
- 选牌型 RestSiteOption。

不适合直接参考：

- BaseLib CustomReward/Config 注册；
- StrangeBook 单遗物 Overlay；
- Ancient 独立按钮；
- 将复杂反射、缓存注入或 RNG 算法原样迁移。

## 6. ModD — 你画瓦猜 / DrawAndGuessMod

### 基础信息

- 路径：`References\ModD`；源码参考为 `References\ModD_Source`
- ID：`DrawAndGuessMod`
- 名称：`你画瓦猜 / Draw & Guess`
- 作者：`QingTian`
- 版本：`0.9.0`
- `min_game_version`：`0.111.0`
- 依赖：`STS2-RitsuLib >= 0.5.12`，不使用 BaseLib
- 评级：**High（源码设计参考）**。版本与当前 Beta 对齐且有源码，但其 RitsuLib 状态框架、ONNX 运行时和网络系统不属于 MCEnchantingTable 架构。

### Neow Side Button Pattern

- `Scripts.Patches.NeowRunSettingsPatch` Patch `NEventLayout.SetEvent`。
- Postfix 只接受 `Neow + NAncientEventLayout + RunState`，以固定节点名防止重复创建。
- 它通过 `%OptionsContainer` 获得布局参照，调用 `NeowSettingsBadge.Create(optionsContainer, runState)`，再把按钮直接挂到 `NAncientEventLayout`。
- `Scripts.Ui.NeowSettingsBadge : Button` 自己处理 tooltip、focus、hover、pressed scale、locale font 和子节点 `MouseFilter=Ignore`；点击后打开 `NeowRunSettingsScreen`，没有进入 EventOption 系统。
- `NeowRunSettingsScreen.Open()` 使用 `NModalContainer`，并实现 `IScreenContext`、默认焦点、Escape 关闭及 `_ExitTree` 事件解绑。
- Badge 随 layout tree 销毁，不需要全局 UI 节点 Dictionary。

该模式证明 `NEventLayout.SetEvent` 是当前 0.111 可用的 side-action 创建时机，但不能原样复制：

- 原按钮是圆形，本项目要求圆角方形；
- 原按钮用 `_Process()` 每帧读取 `%OptionsContainer.GlobalPosition/Size`，本项目禁止对话/UI 轮询；
- 原按钮打开的是 Neow Run Settings，并包含 host-only 全局规则，不是每玩家 Ancient encounter 状态；
- 原实现创建后直接参与显示定位，不负责等待 Ancient 最后一行 dialogue。

Phase 2C 采用其“Patch `SetEvent` + layout 独立子按钮 + 不进入 EventOption”的架构，但改用相对 Anchor 定位，并用 Beta `NAncientEventLayout.SetDialogueLineAndAnimate` 的确定性生命周期开放按钮。

### 其他参考价值

- `DrawingNetSync` 展示复杂位置定向消息与 UI 会话；规模远超 Phase 2C。
- `DrawingRunRules`、各 `State/*Store` 展示 RitsuLib 保存状态，但不应替换 StrangeBook `[SavedProperty]`。
- `DeathNoteRestSiteOption` 可作为后续 Rest Site 调研入口。
- Localization 源码包含 `eng`、`zhs`、`zht`、`deu`、`fra`、`jpn`、`kor`，资源工程清晰可读。

## 7. Phase 2B — StrangeBook Progress Overlay 结论

三个 Reference Mod 中 **没有发现与目标完全对应的实现**：即给特定 `CustomRelicModel` 的 `NRelic` 增加动态普通战进度 Label，并在遗物实例状态变化时刷新。

最接近但仍不应照搬的是：

- `HextechRelicVisibilityHooks`：展示如何在现有 Godot UI 下动态创建 `Control`/按钮、定位、刷新和清理，但作用域是全局遗物可见性。
- `YuWanCard` 的遗物 UI safety/icon patches：展示 `NRelicInventoryHolder` 生命周期风险，但没有具体的单遗物进度 Overlay。

因此 Phase 2B 仍应以当前项目已验证的 BaseLib 3.4.5 `ICustomUiModel.CreateCustomUi(Control)` 路线为主，以当前 Beta 的 `NRelic` / `NRelicInventoryHolder` 为生命周期依据。Reference Mod 只能补充动态节点和销毁方面的注意事项。**本轮不实施或修改该 Overlay。**

## 8. Future Enchant UI / Ancient / Rest Site 索引

- 选一张卡：`HextechRunes.StokeRestSiteOption.OnSelect()`；当前 ModB 的 `TradeFlow` 直接复用 `NDeckCardSelectScreen`。
- 多候选 Screen：`HextechRuneSelectionScreen`。
- 多人 UI 选择同步：`HextechRuneSelectionCoordinator.SelectRuneMultiplayer()`；较小的多阶段协议参考当前 ModB `TradeFlow`。
- Ancient 独立 side button：ModD `NeowRunSettingsPatch` + `NeowSettingsBadge`。
- UI 挂载/销毁：ModD `NeowRunSettingsScreen` / `NModalContainer`，以及当前 ModB 的 popup controller。
- Ancient/EventOption：`YuWanCard.Patches.StartingAncientOptionsPatch`。
- Ancient 独立按钮：ModD 已提供 Neow side-action 案例，但开放时机仍以当前 Beta `NAncientEventLayout` 调查为准。
- Rest Site 注入：ModB `AddTradeOptionPatch` 最直接；卡牌选择动作参考 ModC `StokeRestSiteOption`。

## 9. Missing Mod / Save Dependency 结论

当前参考集合中没有发现能真正解决以下问题的机制：**当该 Mod 自身完全未加载时，在 Continue/FadeOut 前识别旧 Run 对自己的依赖并阻止加载。**

- 搜索未发现 `RequiredMod`、`MissingMod`、完整的 save dependency metadata 或 safe-uninstall 入口。
- ModA/ModC 的保存缓存和未知 SavedProperty 兼容代码只有在它们自己的程序集已加载时才能运行。
- ModC 的 reward restore/safety hooks 同样不能在 Mod 缺失时保护自己的自定义 Reward。
- 因此这些实现不能解决 MCEnchantingTable 当前的根本问题；正确方向仍是 Beta/BaseLib 通用 Required Mods 预验证，或未来在 Mod 仍启用时执行完整 Safe Uninstall。

## 10. 后续使用规则

- 永远只读使用 `References`，反编译输出只能写到临时目录或 `References\_Decompiled\_Temp`，不得覆盖原包。
- DLL 优先；PCK 仅用于 scene、UI、localization、texture 和资源树，解包失败时记录并跳过。
- 不再为完成 API 调查强行运行 GDRE。
- 每次采用参考模式前，核对当前 Beta API、BaseLib 3.4.5 行为、生命周期、保存、多人与许可证。
- 不复制大段反编译代码；只重新实现符合 MCEnchantingTable 需求的最小版本。
- 新发现的关键类和兼容性变化继续更新本文。
