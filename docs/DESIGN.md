# MC附魔台 — 设计文档

## 1. 项目定位

Mod 名称：

**MC附魔台**

内部项目 ID：

`MCEnchantingTable`

本 Mod 为《杀戮尖塔2》加入一套受到 Minecraft 附魔机制启发的局内卡牌成长系统。

核心玩法循环：

**战斗 → 获取书 → 提升附魔能力 → 在先古之民、火堆或特殊事件处附魔卡牌 → 随着书籍积累逐渐获得更高级的附魔**

本 Mod 的重点不是简单增加更多附魔，而是重新设计附魔的获取、成长和路线规划机制。

---

# 2. 核心遗物：奇异的书

书籍系统通过一个特殊遗物表现。

正式设计决定：

- “奇异的书”是本 Mod 的系统载体遗物，不进入普通遗物池。
- 每局开始时，每名玩家自动拥有各自独立的一份“奇异的书”。
- 初始 `BookCount = 0`，书籍数量没有上限。
- `BookCount` 绑定遗物实例及其玩家，不使用无法区分玩家的全局静态状态。

遗物名称：

**奇异的书**

视觉主体：

一本受到 Minecraft 附魔书视觉概念启发、但使用原创素材重新设计的奇异书籍。

## 书本总数

书籍数量通过遗物右下角的数字角标显示。

表现方式参考钢笔尖等具有数字计数的原版遗物。

例如：

`8`

代表当前拥有：

**8本书**

书籍数量：

**没有上限。**

15本不是资源上限。

15本只是当前版本中：

**附魔台达到完整状态的阈值。**

因此：

`15`

`18`

`30`

等数量全部属于合法状态，并必须保存真实值。

---

# 3. 普通战斗进度显示

从第二幕开始，一场普通战斗不再必定直接获得一本书。

因此《奇异的书》的封面承担普通战斗进度显示功能。

这个进度必须和书本右下角总数区分。

例如第二幕：

封面：

`1/2`

右下角：

`8`

表示：

当前拥有8本书，并且已经完成了下一本书所需的2场普通战斗中的1场。

## 第一幕

每1场普通战获得1本书。

因此第一幕不需要在书封显示：

`0/1`

只使用基础书本插图。

## 第二幕

书封可能显示：

`0/2`

`1/2`

完成第二场普通战后：

- 书本数量 +1
- 普通战进度归零
- 封面重新显示 `0/2`

## 第三幕

书封可能显示：

`0/3`

`1/3`

`2/3`

完成第三场普通战后：

- 书本数量 +1
- 普通战进度归零
- 封面重新显示 `0/3`

数字应尽量在视觉上与书皮颜色、纹理和材质融合，而不是表现为一个突兀的调试文字覆盖层。

实现上优先考虑：

**基础书本图像 + 动态进度文字**

而不是为每种状态制作一张完整图片。

---

# 4. 战斗获取书籍规则

战斗胜利时先计算本场书籍数量，并在战斗奖励界面生成一个可主动领取的 `BookReward`。生成奖励时不直接修改 `BookCount`；只有玩家点击领取后才一次性增加对应数量。多本书合并在同一个奖励项中，允许像其他可跳过战斗奖励一样被放弃。

只有自然地图战斗参与书籍规则：普通 Monster 节点、Unknown 节点自然解析出的 Monster、自然 Elite 和 Boss。通过事件文本选项进入的战斗不推进普通战进度，也不生成 `BookReward`。

## 第一幕

普通战：

**每1场 → +1本书**

精英战：

**每1场 → +1本书**

Boss战：

**+2本书**

---

## 第二幕

普通战：

**每2场 → +1本书**

精英战：

**每1场 → +1本书**

Boss战：

**+2本书**

---

## 第三幕

普通战：

**每3场 → +1本书**

精英战：

**每1场 → +1本书**

Boss战：

**+2本书**

---

# 5. 普通战计数规则

普通战进度：

**只由普通战推进。**

精英战和Boss战：

- 可以直接获得书
- 不推进普通战计数器

例如第二幕：

普通战：

`1/2`

然后完成精英：

`+1本书`

但普通战进度仍然：

`1/2`

---

# 6. 幕末余数结算

每一幕结束时进行统一的普通战进度结算。

如果：

`普通战进度 > 0`

则：

**额外获得1本书**

随后：

**普通战进度归零**

余数补偿在 Boss 胜利时与 Boss 基础 2 本合并为同一个 `BookReward`。例如 Boss 基础 2 本且存在余数时，奖励界面显示一次可领取的“书 ×3”，不再在进入下一幕后后台到账。

例如第二幕结束：

`1/2`

则：

`+1本书`

进入第三幕后：

`0/3`

第三幕同样执行这一规则。

例如第三幕Boss结束：

`2/3`

仍然：

`+1本书`

然后计数归零。

该规则必须支持未来：

- 第四幕
- 新增幕
- 无尽模式

不要针对第二幕或第三幕写一次性特殊代码。

---

# 7. 附魔等级

附魔分为：

**I级**

**II级**

**III级**

数值型附魔可以根据等级提高数值。

例如某个获得格挡的附魔可以采用类似：

I：

获得2点格挡

II：

获得5点格挡

III：

获得7点格挡

具体数值后续统一平衡。

---

# 8. 机制型附魔

直接改变卡牌运行机制的附魔默认属于：

**III级**

包括但不限于：

- 保留
- 重放
- 失去消耗
- 自动打出
- 其他明显改变卡牌行为方式的附魔

这类附魔不强制设计 I / II / III 三个弱化版本。

其默认最低等级：

**III**

---

# 9. 附魔台阶段

## 低书架：0～4本

生成：

**1个槽位**

槽位1：

**100% I级**

---

## 中书架：5～10本

生成：

**2个槽位**

槽位1：

**100% I级**

槽位2：

**100% II级**

---

## 高书架：11～14本

生成：

**3个槽位**

槽位1：

**100% I级**

槽位2：

**100% II级**

槽位3：

- 70% II级
- 30% III级

---

## 满书架：≥15本

生成：

**3个槽位**

槽位1：

**100% I级**

槽位2：

- 70% II级
- 30% III级

槽位3：

- 30% II级
- 70% III级

---

# 10. 附魔流程

附魔必须按照：

**先选择卡牌，再生成附魔**

的顺序进行。

流程：

选择附魔

↓

选择一张合法卡牌

↓

分析该卡牌属性

↓

确定当前书架阶段

↓

确定槽位数量

↓

Roll 每个槽位的附魔等级

↓

针对“目标卡牌 + 附魔等级”建立合法附魔池

↓

生成附魔选项

↓

玩家选择一个

↓

永久赋予该卡牌

---

# 11. 不同卡牌拥有不同附魔池

不同卡牌进行附魔时：

**可出现的附魔选项应该不同。**

合法性判断至少需要考虑：

- 卡牌类型
- 是否造成伤害
- 是否产生格挡
- 是否抽牌
- 是否消耗
- 是否虚无
- 是否固有
- 卡牌费用
- X费用
- 是否生成其他卡牌
- 卡牌已有机制
- 游戏原版附魔适用性规则

优先复用游戏原版的附魔合法性判定。

不得简单从所有附魔中无条件随机。

---

# 12. 同一次附魔的重复规则

同一次附魔中：

**允许同一种附魔出现多个不同等级。**

例如允许：

`Sharp I`

`Sharp II`

`Replay III`

也允许极低概率出现：

`Sharp I`

`Sharp II`

`Sharp III`

但是不允许完全相同的结果重复。

例如：

`Sharp II`

`Sharp II`

属于非法结果。

---

# 13. 同名附魔降权

同一个附魔在本次附魔中首次出现：

**100%基础权重**

已经出现一次：

后续槽位权重：

**×0.25**

已经出现两次：

后续槽位权重：

**×0.05**

因此同名跨等级结果允许出现，但概率明显降低。

所有这些数字必须可配置。

---

# 14. 普通附魔的数量限制

v1.0中：

**一张卡牌最多拥有一个附魔。**

已经存在附魔的卡牌：

不能通过普通附魔流程再次获得第二个附魔。

多重附魔留给未来的：

**铁砧系统**

处理。

---

# 15. 先古之民

每一次先古之民 encounter 为每名玩家分别提供：

**一次附魔机会**

该机会不是“每幕一次”。同一幕如果通过自定义地图或其他机制出现多个 Ancient encounter，每一个 encounter 都提供新的独立机会；第四幕、Endless 和自定义幕同样按 encounter 处理。

机会状态保存在每名玩家自己的“奇异的书”实例上，不使用 `static`、UI 节点状态或仅记录幕编号的 `LastAncientEnchantActIndex`。

附魔等级完全取决于玩家当时实际拥有的书籍数量。

---

# 16. 火堆附魔

火堆增加新的操作：

**附魔**

选择附魔：

- 消耗本次火堆行动
- 恢复最大生命值的10%
- 进行一次附魔

---

# 17. 火堆附魔恢复是独立效果

火堆附魔固定恢复 **10% 最大生命**。这不是原版 Rest/Heal 行为，不进入 `HealRestSiteOption`、`ModifyRestSiteHealAmount`、`AfterRestSiteHeal` 或其他休息恢复 Modifier，因此皇家枕头和其他 Rest 专属遗物不会改变该数值。

未来如果需要让遗物影响附魔恢复，应建立语义独立的 `EnchantHealModifier`，不得重新借用原版 Rest heal hook。

---

# 18. 问号事件

v1.0需要加入附魔主题问号事件。

事件可以通过以下资源交换获得附魔相关收益：

- 金币
- 当前生命
- 最大生命
- 诅咒
- 其他机会成本

奖励可以包括：

- +1本书
- +2本书
- +3本书
- 特殊固定等级附魔
- 特殊随机附魔机会

v1.0暂时不加入：

- 附魔书物品
- 铁砧
- 多重附魔

---

# 19. 多人游戏

v1.0必须支持多人模式加载。

多人支持不是后期补丁，而是核心架构要求。

至少需要正确同步：

- 玩家书本总数
- 当前普通战进度
- 幕末余数结算
- 卡牌附魔状态
- 附魔选项随机结果
- 最终附魔选择
- 火堆附魔
- 先古之民附魔
- 问号事件产生的相关状态

不要使用不可同步的本地随机数。

如果游戏采用权威端机制：

随机附魔结果应由权威端产生并同步。

不得出现不同客户端看到不同附魔结果的情况。

书籍状态必须绑定具体玩家，而不是使用无法区分玩家的全局静态整数。

---

# 20. v1.0范围

v1.0包含：

- 奇异的书遗物
- 书本计数
- 普通战斗进度
- 动态遗物显示
- 三幕书籍增长规则
- 幕末余数结算
- I / II / III附魔
- 卡牌专属合法附魔池
- 同名附魔低概率跨级重复
- 机制型III级附魔
- 先古之民附魔
- 火堆附魔
- 火堆10%最大生命恢复
- 独立的火堆附魔恢复（不受皇家枕头影响）
- 附魔主题问号事件
- 单机存档
- 多人同步

---

# 21. 暂不实现

以下属于未来版本：

- 附魔书
- 铁砧
- 双重/多重附魔
- 两本附魔书合成
- 铁砧升级附魔
- 附魔冲突体系

未来设计方向为：

**书架 → 附魔台 → 附魔书 → 铁砧**

但v1.0只完成：

**书架 + 附魔台 + 附魔事件**

---

# 22. 设计原则

本 Mod 的核心体验应是：

> 玩家通过路线和战斗逐渐建设自己的附魔能力，并利用有限的附魔机会，将少量关键卡牌培养成本局重要组成部分。

系统不应该退化为：

> 战斗越多就无条件获得大量免费强化。

强度必须受到：

- 路线选择
- 战斗数量
- 书籍积累
- 火堆机会成本
- 随机附魔结果
- 卡牌合法附魔池

共同约束。

---

# 23. Phase 2A.5 书籍规则配置

> 2026-08-31 更新：本节为旧版历史。当前 Unified Settings v2 以 `config/MCEnchantingConfig.json` 为唯一默认值来源，继续使用同一个 BaseLib 配置文件和设置页。单人后续奖励即时读取新设置；多人书本规则仍使用 Host 开局快照。完整 schema、迁移、22 附魔 Amount 语义、默认值及测试边界见 [UNIFIED_SETTINGS_V2.md](UNIFIED_SETTINGS_V2.md)。已有附魔和已缓存候选不追溯修改。Ancient 默认治疗为 0%，火堆为 10%。

六项掉书规则由 BaseLib Mod Config 管理，默认值保持 `1 / 2 / 3 / 1 / 1 / 2`。普通战阈值范围为 `1–20`，奖励数量范围为 `0–20`。

每个新 Run 创建时冻结一次规则快照；当前 Run 不受之后全局配置修改影响。快照随每名玩家的“奇异的书”遗物实例保存。多人新局以 Host 配置为准，并在原版开局消息中一次性同步快照；战斗期间不重复发送配置。

幕末有普通战余数时，补偿数量使用该 Run 快照中的普通战奖励数量，而不是固定一本。

## 正式资源名称

“奇异的书”既是遗物栏中系统载体的正式名称，也是战斗结算中书籍资源的正式名称：

- 简体中文：`奇异的书`
- English：`Strange Book`

战斗奖励不得使用泛称“书 / Book”。数量由 `BookReward.Amount` 表达，例如“奇异的书 ×2 / Strange Book ×2”；遗物栏右下角的 `BookCount` 表示当前累计拥有数量。

---

# 24. Phase 2B 遗物进度显示

`StrangeBook` 按原版遗物资源契约分离两套 Icon：

- PackedIcon：`res://MCEnchantingTable/images/relics/strange_book_packed.png`，用于遗物栏及 `NRelicInventoryHolder.DoFlash()` 粒子 Texture；
- BigIcon：`res://MCEnchantingTable/images/relics/strange_book.png`，256×256，用于 tooltip、大图展示与 `BookReward` 奖励栏。

两者由 `MCEnchantingTableAssets.RelicAssets.StrangeBookPackedIconPath` 与 `StrangeBookBigIconPath` 统一管理。战斗奖励始终使用 BigIcon。普通战进度通过 BaseLib 3.4.5 `ICustomUiModel.CreateCustomUi(Control)` 仅挂载于本地遗物栏 `StrangeBook` 的 `NRelicInventoryHolder`，不修改或替换共享 Texture。

进度格式为 `current/required`，其中 `required` 来自当前 Run 的规则快照；当 `required <= 1` 时隐藏。进度 Label 与原版右下角 `BookCount` Counter 相互独立。

---

# 25. Phase 2C Ancient 独立附魔入口

Ancient 附魔入口是 `NAncientEventLayout` 上的独立 side action，不加入 `AncientEventModel.CurrentOptions`，不占用 `EventOption` index，不调用 `EventSynchronizer.ChooseOption`，也不改变 Ancient 的 `IsFinished` 或 Event History。玩家使用附魔入口后，原版 Ancient 选项仍保留并可正常选择。

按钮是独立图片按钮，当前节点结构为 `AncientEnchantOption : Button -> Control("Visuals") -> TextureRect("Icon")`。Button 自身使用透明空 StyleBox，PNG 提供完整外观和 alpha；不使用 Panel、ColorRect、Label 或 C# 绘制按钮背景。按钮不显示文字，也不创建 Hover Tooltip；“附魔 / Enchant”本地化仍保留供其他界面或未来用途。按钮随 Ancient layout 创建，但在对话到达最后一句以前保持隐藏和禁用；原版 `NAncientEventLayout.SetDialogueLineAndAnimate` 进入最后一句时才开放。

按钮直接挂在全屏 `NAncientEventLayout`，当前使用固定相对 Anchor：中心位于 layout 的 `(0.82, 0.50)`，逻辑点击区域为 132×132，`Visuals` 子节点使用 1.2 基础 Scale。当前稳定方案不读取 `NEventOptionButton`、`OptionsContainer`、GlobalPosition 或 GlobalRect，不跟随原版选项 Hover/Press，也不使用 `_Process`、Timer、排序或动态重定位。后续若重新设计自动对齐，必须作为独立 UI 任务重新验证。

Hover/Press 仅作用于 `Visuals` 子节点，不改变点击区域或 Anchor。交互参数参考原版 Event Option：Hover Scale 1.01、Press Scale 0.99，并复用 `event:/sfx/ui/clicks/ui_hover` 与 `event:/sfx/ui/clicks/ui_click`。

每名玩家自己的 `StrangeBook.LastAncientEnchantEncounterKey` 使用 `[SavedProperty]` 保存最近已消费的 Ancient encounter。当前 Beta 没有可跨 Save/Load 稳定使用的 room instance ID；`AbstractRoom.Id` 明确不稳定。因此 encounter key 由当前幕、地图坐标、总楼层和 Ancient `ModelId` 组合。不同幕、不同 Ancient 节点或同幕额外 Ancient 都生成不同 key；Continue 当前房间时生成相同 key。

点击按钮目前只验证机会、写入 encounter key、同步玩家所属状态并保存 Run，不执行选牌、生成候选附魔或应用 `EnchantmentModel`。未来 Ancient 与 Rest Site 应共同进入 `AncientEnchantController.BeginEnchant()` 后续扩展的统一附魔流程。

多人模式中，状态仍绑定每名玩家自己的 StrangeBook。点击者本地先消费机会，再通过 BaseLib 的位置定向自定义消息把同一状态变化应用到其他 peer 的该玩家遗物实例；消息以真实 `senderId` 识别玩家，并由当前 `RunLocation` 限定。玩家 A 使用不会修改玩家 B 的 encounter key 或按钮可用性。

玩家可见图片路径统一收口到 `MCEnchantingTableAssets`。当前分类登记 `RelicAssets.StrangeBookPackedIconPath`、`RelicAssets.StrangeBookBigIconPath`、`AncientAssets.EnchantButtonPath` 与 `RestSiteAssets.EnchantButtonPath`；未来事件背景和附魔 UI 图片也必须加入同一资源目录表，禁止在各业务类中散落 `res://` 字符串。遗物名称、描述、效果说明和风味文本继续由 `zhs/eng` localization 文件提供，C# 模型不得保存玩家可见文案。

---

# 26. Phase 2D Rest Site 附魔入口

火堆附魔属于原版 `RestSiteOption`，不是 Ancient 风格的悬浮图片按钮。`EnchantRestSiteOption` 通过 `RestSiteOption.Generate` Postfix 加入每名玩家自己的 option 列表，并由原版 `NRestSiteButton`、布局、动画和 `RestSiteSynchronizer` 完成显示与选择。

选项 ID 为 `MCENCHANTINGTABLE_ENCHANT`，标题及描述来自 `rest_site_ui` 本地化。Phase 2D 暂时只恢复 10% 最大生命，不选择卡牌、不生成或应用附魔。Ancient 与 Rest Site 的后续业务入口统一收口到 `EnchantController`，避免未来发展成两套附魔流程。

10% 是附魔动作自身的最终基础效果，只通过通用 `CreatureCmd.Heal` 执行生命变化，不调用 `HealRestSiteOption.ExecuteRestSiteHeal()` 或任何 Rest heal hook。皇家枕头及其他休息恢复 Modifier 不参与计算。

本阶段通过 `RestSiteOption.Icon` getter Patch 仅为 `EnchantRestSiteOption` 返回 `MCEnchantingTableAssets.RestSiteAssets.EnchantButtonPath`，实际资源为 `res://MCEnchantingTable/images/rest_site/campfire_enchant_button.png`。图片遵循原版横向 Rest Site Option 资源结构，由原版 `NRestSiteButton` 提供 Outline、Hover 与 Press 动画，不额外绘制 Hover 描边。选项选择由原版 option index 消息按玩家同步，不新增自定义网络协议或 `[SavedProperty]`。

`BookReward` 领取时创建一个临时的 `NRelicInventoryHolder` 视觉实例，并复用其原版 `PlayNewlyAcquiredAnimation()`。真实 StrangeBook holder 始终停留并显示在遗物栏；临时实例使用真实 holder 的 global position 与实际 Control size 作为落点。动画开始后隐藏完整的 BookReward RewardButton，使其立即退出奖励栏视觉布局；不手动删除按钮。动画和落点闪光完成后才执行 `StrangeBook.AddBooks(Amount)`，`OnSelect()` 返回成功后仍由原版 `RewardClaimed -> NRewardsScreen.RewardCollectedFrom() -> RemoveButton()` 完成权威清理。动画只在领取者本地 UI 播放，远端 RunState 镜像不创建本地飞行动画。

BookReward 奖励栏使用 `StrangeBookBigIconPath`；临时 holder 的 `DoFlash()` 使用模型的 `PackedIcon`，即 `StrangeBookPackedIconPath`。当前不生成运行时缩放纹理，不 Patch `DoFlash()`，不修改 Ripple scene、Scale、粒子、Curve、Alpha 或 Duration。旧 1254×1254 兼容归一化和 BookReward 专用 Ripple Scale 均已回退并不存在于当前代码。

---

# 27. Text Localization & Keyword Highlight Rules

所有玩家可见文本必须由 Localization 提供，并同时维护简体中文 `zhs` 与 English `eng`。适用范围包括附魔按钮名称、Tooltip、附魔说明、遗物名称与描述、事件正文与选项、Rest Site 与 Ancient 文本、附魔界面提示以及未来附魔词条。C# 业务代码不得硬编码“附魔 / Enchant”“升级 / Upgrade”“牌组 / Deck”等玩家可见文本。

颜色必须由原版 LocString / Keyword Highlight 标签产生，禁止通过 Label Modulate、字体颜色覆盖或 C# 字符串拼接制造高亮。当前统一规则为：

- 普通数量使用原版数字高亮 `[blue]`；
- 游戏动作关键词（例如“升级 / Upgrade”）使用原版动作高亮 `[gold]`；
- MCEnchantingTable 系统特殊关键词“附魔 / Enchant”使用原版特殊关键词高亮 `[purple]`。

同一关键词在卡牌选择提示、Rest Site Tooltip、事件说明和后续附魔文本中必须保持同一语义颜色。新增关键词不得自行指定颜色，必须先检查当前 Beta 原版 Localization 的既有颜色规则。

Ancient 的当前图片按钮按正式设计不创建 Hover Tooltip，因此不存在按钮悬浮文字；其保留的本地化名称供未来界面使用。EnchantScreen 的选择提示直接复用原版 `card_selection/TO_ENCHANT`，Rest Site Tooltip 使用本 Mod 的 `rest_site_ui/OPTION_MCENCHANTINGTABLE_ENCHANT.description`，两者均以 `[purple]` 标记“附魔 / Enchant”。

---

# 28. Phase 3B 附魔候选框架

MCEnchantingTable 不重写原版附魔数据、合法性、应用或保存结构。候选池中的每一项都引用原版 `EnchantmentModel` 的 `ModelId`；卡牌合法性只调用对应模型的 `CanEnchant(CardModel)`。未来真正确认应用时必须调用 `CardCmd.Enchant()`，由原版创建可变 `EnchantmentModel` 并继续使用 `SerializableEnchantment` 保存。本阶段只生成、显示和记录 UI 选择，不调用 `CardCmd.Enchant()`，也不改变卡牌。

`MCEnchantmentCandidate` 保存原版模型 ID、MC 等级、原版 `Amount`、原版本地化 key 和原版 Icon 路径。MC 等级是本 Mod 的候选稀有度/强度层级，`Amount` 是原版模型实际使用并写入存档的数值，两者不得等同处理。各等级对应的 `Amount` 由 `res://config/MCEnchantingConfig.json` 配置，`MCEnchantmentConfig` 负责读取、schema 校验和解析原版模型，UI 不保存数值映射。

JSON 当前登记 22 个原版附魔。登记代表它们可以由统一候选框架解析，并不代表每个 MC 等级都能改变原版效果；不读取 `Amount` 的机制型附魔必须使用 1 作为存档占位。加载器会检查 schema、书数区间连续性、等级集合、正权重、原版 `ModelId` 和 Amount 语义。已确认 `Spiral` 的原版重放次数固定来自 `DynamicVar("Times", 1)`，不会读取 `Amount`；配置 III→2 只会触发警告，不会使其重放两次，若要实现该效果必须另行设计而不能假定 Amount 生效。

选牌列表遍历当前配置池：只要至少一个原版模型对该牌的 `CanEnchant` 返回 true，该牌即可显示。选择卡牌后，`EnchantCandidateGenerator` 按 `BookCount` 生成槽位等级：

- 0–4：1 槽，I；
- 5–10：2 槽，I / II；
- 11–14：3 槽，I / II /（70% II，30% III）；
- 15+：3 槽，I /（70% II，30% III）/（30% II，70% III）。

每个槽位再从“支持该等级且 `CanEnchant` 为 true”的配置项中抽取。相同“附魔 ID + 等级”不重复；同名附魔允许跨等级再次出现，但第一次重复降至 25% 基础权重，后续重复降至 5%。候选 UI 使用原版 Icon、`enchantments/{ID}.title` 与动态 description，仅记录当前选中的 `MCEnchantmentCandidate`。

当前生成器显式接收 `Rng`，界面暂接 `player.RunState.Rng.Niche`，不使用 `System.Random` 或 Godot 非确定随机数。Phase 3B-Framework 尚未实现多人权威端候选消息；在进入可实际应用的阶段前，必须由权威端生成并同步候选，不能依赖各 peer 独立打开 UI 得到相同结果。
