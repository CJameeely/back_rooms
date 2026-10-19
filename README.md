# The Last Light — Complete Playable Prototype

玩家凌晨 3:07 在校园实验室意外穿墙，进入 LEVEL 4.7 THE CLEAN ROOMS。R. Shen 留下的物件、纸条和四段磁带引导玩家发现光照与空间的规则。

当前版本（2026-10-19）：

- 四个可玩的挑战：符号密码、凳子与校准卡、检查灯与侧光断路器、镜面顺序与旋钮锁。
- 九个已有空间串成完整路线，后接出口走廊。普通结局返回循环，真结局回到校园实验室。
- 每房间独立计时、阅读暂停、错误扣时、隐藏线索加时、重试恢复位置与物品、已解房间保留。
- 清洁工、字幕磁带、可跳过开场、循环记录与上一轮痕迹已接入。角色和部分环境仍为原型级造型，磁带暂未配音。
- WASD 移动，鼠标观察，E 互动，F 检查灯，R 旋转校准卡，Tab 重看目标，Esc 关闭界面。

前两间由编辑器生成并保存到场景；`StoryExperience` 与 `FullCampaign` 在关卡加载时安装其余叙事和机关。后半段修改应改对应运行时构建方法。

构建：`WhiteBoxKit.PlaytestBuild.BuildMac`。专用录像构建：`BuildRecording`，启动参数 `--record-playthrough <absolute.mp4>`。录像按真实键鼠输入通关，读取已显示的线索。`--chapter-check` 仅供开发时设置后半段测试状态，带有明显开发标记，不可作为作业通关录像；`--normal-ending` 在完整路线最后选择普通结局。

以下保留基础编辑器生成流程的技术说明；旧的第一间单房间演示不是当前完整游戏的边界。

## 技术栈

| 类别 | 选型 | 版本 |
|---|---|---|
| 引擎 | Unity (Unity 6 / Tuanjie) | `6000.6.0f1` |
| 渲染管线 | Universal Render Pipeline (URP) | `com.unity.render-pipelines.universal` 17.6.0 |
| 输入 | Unity Input System(**仅新版**,Active Input Handling = Input System Package) | `com.unity.inputsystem` 1.20.0 |
| UI / 文字 | TextMeshPro(世界空间标签 + 屏幕 UI 两种都用到) | 内置于 `com.unity.ugui` 2.6.0 |
| 测试 | Unity Test Framework(NUnit,PlayMode) | `com.unity.test-framework` 1.8.0 |
| 导航(预留,未使用) | AI Navigation | `com.unity.ai.navigation` 2.0.14 |
| 版本控制 | Git + GitHub,场景/预制体用 **Force Text** 序列化 | — |
| 编辑器自动化 | 自研 Editor 脚本(`WhiteBoxKit` 命名空间)+ [Unity CLI](https://github.com/cli/cli) `unity` 命令做无头批处理生成/截图/测试 | — |

> 引擎版本必须完全一致(`6000.6.0f1`)。用 Unity Hub 打开项目时如果没有这个版本,会提示安装——直接装,不要用相近版本代替,URP/Input System 的包版本是按这个 Editor 版本锁定的。
>
> Input System 是"仅新版"模式(不是 Both),所以**不能用旧版 `Input.GetAxis`/`Input.GetKey`**,新脚本一律用 `Keyboard.current`/`Mouse.current`,参考 `FirstPersonController.cs`。

## 目录结构

```
Assets/
  Editor/WhiteBox/            # 关卡/玩法生成器(仅编辑器,不会打进构建包)
    WhiteBoxModules.cs          # 材质(含 Backrooms 配色) + 15 个基础模块 Prefab + Room_6x6 房间壳
    WhiteBoxLevelBuilder.cs     # 房间关系图 + 每个房间的具体布置 + 相机导览路径生成
    GameplayBuilder.cs          # Step 3:在房间1上叠加玩法(玩家/线索/密码门/计时器)
    SceneFlowBuilder.cs         # Step 4:生成 Title/End 场景 + 修正 Build Settings
    UIBuilder.cs                 # 纯编辑器用的 UI 搭建工具函数,Step3/4 共用
    WhiteBoxScreenshotTool.cs   # 批量截图工具
    WhiteBoxDiagnostics.cs      # 杂项诊断/一次性维护用菜单项
  Scripts/
    CameraTourDriver.cs          # 自动运镜相机(录屏演示用),独立 asmdef: WhiteBox.Runtime
    Gameplay/                    # 房间1玩法的运行时脚本,同样在 WhiteBox.Runtime asmdef 里
      IInteractable.cs / CodeManager.cs / FirstPersonController.cs / PlayerInteractor.cs
      ClueMarker.cs / ClueTracker.cs / Billboard.cs / SlideMover.cs
      MovableCabinetClue.cs / DrawerClue.cs / PaperCloseupClue.cs / PaperCloseupUI.cs
      LightSwitchClue.cs / KeypadController.cs / KeypadUI.cs / KeypadDigitButton.cs
      WinTrigger.cs / LightsOutTimer.cs / GameResult.cs / TitleMenu.cs / EndMenu.cs
  Scenes/
    Title.unity                  # Build index 0,由 Step 4 生成
    WhiteBox_LastLight.unity     # Build index 1,由 Step 2(几何)+ Step 3(房间1玩法)依次生成
    End.unity                    # Build index 2,由 Step 4 生成
  StudentTools/FlyCamera/      # 课程提供的飞行相机工具,禁止修改其脚本
  WhiteBox/
    Materials/                   # Mat_Wall/Mat_Floor/Mat_Ceiling(Backrooms配色)+ Mat_Structure(家具)
                                  # + Mat_Interactive(橙,可交互)/ Mat_Clue(青,线索)/ Mat_LightPanel(自发光)
    Prefabs/                      # Step 1 生成的所有模块 Prefab + Room_6x6
  Tests/PlayMode/               # 独立 asmdef: WhiteBox.PlayMode
    WhiteBoxSmokeTest.cs           # 关卡场景能正常跑几秒不报错
    CameraTourSmokeTest.cs         # 运镜相机能走完全部 waypoint
    Room1GameplaySmokeTest.cs      # 完整模拟一遍通关流程 + 一遍超时判负流程
    UIInitialStateTest.cs          # 确认 Keypad/Paper 两个 UI 面板默认是关闭的
```

`Screenshots/` 由截图工具生成,已 `.gitignore` 掉(是构建产物,不是源内容)。

## 如何修改关卡 / 玩法(重要,先读这个)

**不要在 Scene 视图里手动拖东西摆房间或摆玩法机关。** 场景是由代码生成的,任何手动改动会在下次跑生成器时被整体覆盖。真正的"源文件"是:

- `Assets/Editor/WhiteBox/WhiteBoxLevelBuilder.cs` 里的 `BuildGraph()`(房间图:顺序、方向、连接)和各个 `Populate_*` 方法(每个房间放什么家具/贴花/标签)。
- `Assets/Editor/WhiteBox/GameplayBuilder.cs`(房间1的四条线索、键盘、门、计时器怎么摆、怎么互相引用)。
- `Assets/Editor/WhiteBox/WhiteBoxModules.cs` 里的模块定义(新增一种基础模块,比如新的家具形状)。

改完代码后,在 Unity 菜单里按顺序跑(每一步都依赖前一步的产物):

1. `Tools > WhiteBox > Step 1 - Import FlyCam + Generate Prefabs`(只有新增/改了模块或材质时才需要重跑)
2. `Tools > WhiteBox > Step 2 - Build Level Scene`(**改了房间图/房间内容都要重跑**——注意这一步是整场景重建,会把房间1的玩法也一起冲掉)
3. `Tools > WhiteBox > Step 3 - Build Room1 Gameplay`(**只要跑过 Step 2,就必须接着跑这一步**,把玩家/线索/密码门/计时器重新叠加回房间1;可以反复重跑,自己会先清掉上次生成的玩法物体再重建,不会越堆越多)
4. `Tools > WhiteBox > Step 4 - Build Title & End Scenes`(只有改了标题页/结束页的文案或按钮逻辑时才需要重跑;会顺带重新整理 Build Settings)
5. `Tools > WhiteBox > Capture Screenshots`(可选,更新参考截图)

也可以不开界面,用命令行无头批处理(CI 或远程机器上常用),方法名对应上面四步:

```bash
unity run "<项目路径>" --editor-version 6000.6.0f1 -- -executeMethod WhiteBoxKit.WhiteBoxModules.Step1_ImportAndGeneratePrefabs
unity run "<项目路径>" --editor-version 6000.6.0f1 -- -executeMethod WhiteBoxKit.WhiteBoxLevelBuilder.Step2_BuildLevel
unity run "<项目路径>" --editor-version 6000.6.0f1 -- -executeMethod WhiteBoxKit.GameplayBuilder.Step3_BuildRoom1Gameplay
unity run "<项目路径>" --editor-version 6000.6.0f1 -- -executeMethod WhiteBoxKit.SceneFlowBuilder.Step4_BuildTitleAndEndScenes
```

### 房间图怎么组织的

`WhiteBoxLevelBuilder.BuildGraph()` 用一串 `Step(kind, direction, label)` 描述一条线性路径,从 `Start` 出发,每一步往 东/南/西/北 挪一格(1 格 = 8m,6m 房间 + 2m 走廊)。`ComputeDoors()` 会根据这条路径自动算出每个房间该在哪面墙开门,`InstantiateRooms()`/`InstantiateCorridors()` 负责真正摆 Prefab。新增房间只需要在 `main` 列表里插一行 `Step`,不用手动算坐标或摆墙。

九个房间当前顺序:`Start → R1 Displaced Object(可玩)→ R2 Locked Counter → E1 Repeat(藏钥匙)→ R3 Pattern+Empty Slot → R4 Portable Light → R5 Aftermath → R6 Mirror+False Centre → R7 Final Alignment`。

### 白盒美术规则(别破坏)

- 结构类用 Backrooms 配色三件套:`Mat_Wall`(脏黄墙)/`Mat_Floor`(棕黄地毯)/`Mat_Ceiling`(米白天花板);家具用 `Mat_Structure`(做旧灰褐);天花板发光面板用 `Mat_LightPanel`。
- 可交互物件(柜子、抽屉、纸、开关、键盘、门)统一用 `Mat_Interactive`(橙);关键痕迹/线索文字用 `Mat_Clue`(青)。
- 所有位置对齐 0.5m 网格。
- 只用简单几何体组合(长方体/圆柱体)表达形状,不导入模型/贴图——想让一个物件更好认,靠"换个更有辨识度的几何体组合"(比如键盘=底板+3x3按钮网格),不是靠贴一行解释性文字上去。房间1之前堆过"CLEAN PATCH"/"DRAG MARK"这类纯装饰性文字标签,实测在真实游玩视角下很容易显得杂乱,现已移除——**白盒阶段的说明性标签和房间1这种会被真人玩的空间,文字密度不是一回事**,后者要克制。

## 房间1 玩法(Step 3 / `GameplayBuilder.cs`)

开局随机生成一个 4 位密码,由 `CodeManager` 统一管理。密码门旁键盘面板上 4 个输入格,从左到右固定对应 ● 圆形 / ▲ 三角 / ■ 方形 / | 竖线,房间里 4 条线索各自展示"一个固定符号 + 当局随机的一位数字":

1. **●(圆形)移动的柜子**:看向柜子按 E,柜子滑回墙角的清洁印子位置,露出原本被挡住的墙面文字。
2. **▲(三角)抽屉里的纸**:按 E 拉开抽屉,再看向纸按 E,弹出全屏特写面板(`PaperCloseupUI`)。
3. **■(方形)柱子背面**:纯被动线索,没有交互提示,必须绕到柱子背对入口/房间中心的那一面才能看到——没有任何实体改变状态,看不看得到完全取决于柱子本身挡不挡视线。
4. **|(竖线)关掉一盏灯**:按 E 切换离开关最近的一盏顶灯,文字透明度随灯光渐变(亮着时几乎看不见)。

键盘(`KeypadController`/`KeypadUI`)按 E 打开,错误会左右抖动并清空,正确则密码门(`SlideMover` 驱动)滑开,门后触发区(`WinTrigger`)判定胜利。`LightsOutTimer` 独立计时(默认 180s,可在 Inspector 关闭或调参),时间耗尽前依次闪烁/熄灯/黑屏,黑屏 2s 后判负——全程不显示倒计时数字。`GameResult`(静态类)把输赢结果和用时带去 `End.unity` 显示。

四条线索的世界空间文字(柜子揭示文字、柱子背面文字、开关旁文字)都挂了 `Billboard.cs`,会一直转向摄像机,不会出现"从某些角度看文字是反的"的问题——这不影响柱子那条线索的"必须绕背面"机制,因为挡不挡得住看是由柱子的实体碰撞体决定的,跟文字朝哪转无关。

## 相机 / 玩家

场景里有三套可控对象,**默认只有玩家是激活的**:

- **PlayerController**(`FirstPersonController` + `PlayerInteractor`):CharacterController 驱动,WASD 移动 + 鼠标视角 + 重力,**默认激活**,出生在房间1南侧入口附近面朝房间中心。交互统一按 E,射线检测 2m 内的 `IInteractable`。
- **FlyCamera**(`Assets/StudentTools/FlyCamera/`,课程发的工具,脚本禁止改):**默认禁用**(以前是默认开的,房间1接入真实玩法后改成了默认关,避免和 PlayerController 抢控制权)。需要自由飞行走查整个白盒时,手动在 Hierarchy 里把它勾选为激活、同时关掉 PlayerController。默认速度 8 m/s,无碰撞,穿墙是设计如此。
- **TourCamera**(`CameraTour/TourCamera`,`CameraTourDriver.cs` 驱动):默认未激活。勾选激活后会在自己的 `Awake()` 里自动关掉 FlyCamera(但不会管 PlayerController,建议手动也关掉),按房间生成顺序自动飞过全部房间,适合录屏演示整体白盒。参数都在 Inspector 里。

## 场景流程

`Title.unity`(Start 按钮)→ `WhiteBox_LastLight.unity`(房间1游玩)→ `End.unity`(按 `GameResult.Won` 显示胜利/失败文案,Replay 回到关卡 / Title 回标题页)。Build Settings 顺序已固定为 Title(0)/关卡(1)/End(2),由 `SceneFlowBuilder` 维护,不要手动改。

## 测试

`Assets/Tests/PlayMode/` 下四个 PlayMode 测试,核心逻辑改动后建议都跑一遍:

- `WhiteBoxSmokeTest`:加载关卡场景跑几秒,确认没有运行时报错。
- `CameraTourSmokeTest`:激活 TourCamera,确认能在合理时间内走完全部 9 个 waypoint,且正确禁用 FlyCamera。
- `Room1GameplaySmokeTest`:**完整模拟一次通关**(逐个触发四条线索、读取真实密码、键盘输入、走过胜利触发区,断言跳转到 End 且 `GameResult.Won=true`)和**一次超时判负**(把 `LightsOutTimer` 时长压缩到 1 秒,断言黑屏后跳转 End 且 `GameResult.Won=false`)。
- `UIInitialStateTest`:断言 KeypadUI/PaperCloseupUI 两个面板刚加载时是关闭的。

跑测试:Unity 里 `Window > General > Test Runner`,或命令行:

```bash
unity test "<项目路径>" --editor-version 6000.6.0f1 --mode PlayMode --output results.xml
```

## Git 协作须知

- **不要提交** `Library/`、`Temp/`、`Logs/`、`UserSettings/`、`*.csproj`、`*.sln(x)`——都已在 `.gitignore` 里,是本地生成的缓存,克隆后 Unity 打开项目会自动重新生成。
- 场景/预制体序列化模式已经设成 **Force Text**(`ProjectSettings/EditorSettings.asset`),千万别改回 Mixed/Binary,不然 `.unity`/`.prefab` 就没法在 Git 里正常 diff 了。
- 想让 `.unity`/`.prefab` 冲突时有像样的三方合并(而不是纯文本冲突标记),在自己本机配置一次 Unity 自带的 `UnityYAMLMerge`:
  ```bash
  git config --global merge.unityyamlmerge.name "Unity YAML Merge"
  git config --global merge.unityyamlmerge.driver "'/Applications/Unity/Hub/Editor/<你的版本>/Unity.app/Contents/Resources/UnityYAMLMerge' merge -p %O %B %A %A"
  ```
  `.gitattributes` 里已经把这些文件类型标了 `merge=unityyamlmerge`,配好本地 driver 才会生效,没配也不影响正常使用(会退回普通合并)。
- `WhiteBox_LastLight.unity` 冲突的正确解法通常不是手动合并 YAML,而是**重新跑一遍 Step 2 + Step 3**——场景是生成出来的,冲突往往意味着两人同时改了 `WhiteBoxLevelBuilder.cs`/`GameplayBuilder.cs` 里不同的部分,先合好那两份 C# 代码,再重新生成场景覆盖掉冲突的 `.unity` 文件。`Title.unity`/`End.unity` 同理对应 `SceneFlowBuilder.cs`。
- 新增运行时脚本放 `Assets/Scripts/` 或其子目录,会自动归入 `WhiteBox.Runtime` asmdef;新增 PlayMode 测试放 `Assets/Tests/PlayMode/`,归入 `WhiteBox.PlayMode` asmdef。两个 asmdef 都显式列了 `Unity.TextMeshPro`/`Unity.InputSystem` 引用——如果新脚本报 "TMPro/InputSystem 命名空间找不到",先检查对应 asmdef 的 `references` 而不是怀疑包没装。
- 建议分支模型:`main` 保持随时可运行,日常开发用 `feature/<你的内容>` 分支,提 PR 合并。

## 已知局限 / 后续可以做的

- FlyCamera 无碰撞,是工具本身的设计,不是关卡的问题。
- 其余 8 个非房间1的房间里,装饰性/说明性文字标签仍然是旧的"背对房间中心"朝向启发式,极少数从非常规角度看会显示镜像文字——房间1的四条线索文字已经用 `Billboard.cs` 解决了这个问题,其余房间因为不涉及实际游玩,暂未处理,如果要修可以把 `Billboard` 组件也挂到对应标签上。
- 目前只保留了一个"近乎相同的重复房间"(E1);18→9 精简房间数量、以及后来给每个房间塞更多细节时,牺牲了原设计里"多个重复房间考验玩家能否察觉细微差异"的测试强度,如果后续要恢复,在 `BuildGraph()` 里加几个 `RoomKind.Repeat` 步骤,并在 `Populate_Repeat` 里给每个实例传不同的道具摆放变体就行。
- 房间1之外的 8 个房间还没有玩法,只是走查用的白盒——如果要扩展成多房间关卡,`GameplayBuilder.cs` 目前的写法是硬编码找 `R1_Displaced` 房间,扩展到其他房间需要先把里面「找房间 → 叠加玩法」的逻辑抽成可复用的参数化方法。

## 本周课堂试玩版本（CJ Evan）

- 从 `Assets/Scenes/Title.unity` 开始运行：标题、作者、目标、操作说明 → Start → 四线索密室 → You Win / Game Over → Replay / Title。
- WASD 移动，鼠标观察，E 交互。纸条用 E / Esc 关闭；密码面板可点击数字或输入主键盘 / 小键盘数字，Enter 确认，Backspace / Delete 清空，Esc / Close 关闭。
- 限时 180 秒；灯光是时间提示，不显示数字倒计时。找到四个符号对应的数字，输入正确密码，再走过出口即获胜。到时后无法抢跑触发胜利。
- 本次修复：结束页释放鼠标以便重玩；阅读面板打开当帧不会被同一个 E 立即关掉；面板打开时阻止场景交互；错误密码动画可安全关闭并重试；直接运行关卡也正确重置用时；重建机关时不重复添加柜子组件；密码键盘调整到室内可交互高度，门板完整填满门洞；线索 HUD 四个槽位不再被屏幕裁掉。
- macOS 本机试玩构建菜单：`Tools > WhiteBox > Build macOS Playtest`，输出 `Builds/The Last Light.app`。

### 提交录屏建议

从标题和作者信息开始，点击 Start；依次展示移柜、开抽屉读纸、绕柱子、关灯找线索；按符号顺序输入当局密码；走出门，停留在 You Win 页面，然后点击 Replay 展示重开。另录一次等待熄灯失败的过程，展示 Game Over 和重玩按钮。密码每局随机，不要照抄上一次的数字。

Mac 可用 Shift + Command + 5 选择游戏窗口所在区域录制。建议 1280×720 或更高，保留完整 HUD 和结束页。提交真实游戏画面；现有自动运镜仅展示空间，不能替代通关录像。

### 本机验证

`Deliverables/playmode-results.xml` 保存本次七项 PlayMode 测试结果（全部通过，涵盖通关、失败、重玩、返回标题、错误密码重试、UI 初始状态、密码键盘可达性和相机巡游）。本机 Unity 6000.6.0f1 的 Burst 原生链接报错，验证时使用 `-burst-disable-compilation` 关闭该编译优化，未屏蔽游戏异常。


## Tripo 模型接入（2026-10-18）

18种用户提供的GLB转换为FBX，保留贴图，放在 `Assets/TripoProps/`。文件名可能与实际物件不一致：`metal_cabinet` 是门板，`metal_storage_box` 是灯盖，`space_station_tile` 是校准卡。

使用 `Tools > WhiteBox > Rebuild Level With Tripo Art` 完整重建；单独运行 Step 2 / Step 3 / Step 3b 后，最后运行 `Step 5 - Apply Tripo Art`。新增的 `TripoArtPass.cs` 保留原交互对象与碰撞，只替换视觉子物体。场景重新生成后仍可恢复美术。

`Tools/prepare_tripo.py` 在 Blender 中执行，用于从原GLB生成游戏模型。多数物件约14k三角面；Room 2 的工具箱、托盘、文件夹、凳子和显示器保留原始网格，以避免近景贴图碎裂。原始文件路径见 `Assets/TripoProps/manifest.json`。URP材质使用提取的颜色与法线图，MetalSmooth贴图按glTF金属度/粗糙度通道转换。源GLB没有改写。

本轮完整重建和11项PlayMode测试通过。后续白盒房间的钥匙、灯、镜面和转盘仍是陈设，不代表新增了对应谜题。部分AI网格有近看可见的碎面；实时镜面反射、模型精修和整体光照调整未包含在本轮。

### Room 2 风格预览（2026-10-18）

`Room2StylePass` 为 Room 2 单独使用暖灰墙面、灰绿护墙、磨旧地面和柔和暖光；保留倒计时灯、交互对象及碰撞引用。Step 5 会自动应用，也可运行 `Tools > WhiteBox > Room 2 Style and Preview`。预览输出到项目上一级的 `output/room2_style_20261018/preview.html`。Unity 批处理拍摄先预热渲染，避免首次材质缓冲尚未更新造成的错误贴图。Room 2 通关和 Tripo 资产检查 3 项通过。

### 全关卡风格与通路（2026-10-18）

9 个空间及 8 段走廊已统一应用材质、暖色灯光和低反光设备材质。运行 `Tools > WhiteBox > Style Entire Level and Preview` 可重应用并输出 18 张实际渲染图，位于上一级目录 `output/level_style_20261018/preview.html`。Step 5 及完整重建也会应用全关卡风格。

修复 Room 6 镜子占据入口、Room 7 封闭出口占据西侧入口的问题；最终出口改在北墙。修正桌上道具高度和数字标记被墙面遮挡的问题，隐藏生成器的悬浮文字说明。

12 项 PlayMode 测试通过，包括 Room 1/2 解谜、UI、资产、镜头游览，以及解锁门后按站立胶囊尺寸检查 9 个房间的连接。通路测试验证空间可达，不代表 Room 3–7 的谜题已实现；后续房间与最终出口仍包含待开发内容。

### 氛围 Demo 与声音（2026-10-18）

`output/demo_20261018/The_Last_Light_Atmosphere_Demo.mp4`（仓库上一级）是 66 秒、720p/24fps 的 Unity 引擎场景镜头展示，不是玩家操作或完整通关录像。视频使用同一原创房间环境音；脚步与按钮音已接入游戏，视频镜头没有模拟玩家脚步。

`GameSoundscape` 在进入场景时自动加载环境声、脚步和按钮反馈；所在房间熄灯时环境音减弱。`Tools/make_audio.py` 可重生成三个原创 WAV，无外部音乐授权依赖。13 项 PlayMode 测试通过，包含声音加载、无重复实例及熄灯衰减。Mac 可运行版本同步更新。

### 当前作业 Playthrough（2026-10-18）

最终视频：仓库上一级 `output/playthrough_20261018/The_Last_Light_CJ_Evan_Playthrough.mp4`，161.56 秒，1280×720 / 30 fps，原生游戏音频。标题 → Room 1 四线索与密码 → 实际穿门 → Room 2 搬凳、登高取卡、刷卡、校准、密码 → 实际穿门。末尾明确标注后续谜题仍在开发。

录制专用构建通过 `PlaytestBuild.BuildRecording` 生成，普通构建不启用 `RECORDING_CAPTURE`。录制脚本回放键鼠，依赖真实碰撞、射线、交互和已显示的线索；不传送、不修改密码、不强制胜利。屏幕字幕标明 Automated Input。Unity AudioRenderer 同步记录原生音频，再与帧缓冲视频封装。`verified_capture.mp4.json` 保存完成状态。修复了卡片方向标记字体缺字（改用现有字体支持的三角形）。
