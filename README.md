# back_rooms — *The Last Light* White-Box

第一人称环境解谜游戏《The Last Light》的白盒(white-box)原型。玩家在一个无限重复、灯光均匀过亮的研究室阈限空间(Backrooms 式)里,通过移动光源、读取人类痕迹,重建一位研究者离开前的最后步骤。

这个仓库目前只包含**白盒关卡**:验证尺度、动线和可读性用的灰模场景,不含美术、音效或最终玩法脚本。整个关卡(房间、家具、贴花、文字标签)是**用编辑器脚本程序化生成的**,不是手工在 Scene 里摆出来的——这一点直接决定了你该怎么改关卡,见下面「如何修改关卡」。

## 技术栈

| 类别 | 选型 | 版本 |
|---|---|---|
| 引擎 | Unity (Unity 6 / Tuanjie) | `6000.6.0f1` |
| 渲染管线 | Universal Render Pipeline (URP) | `com.unity.render-pipelines.universal` 17.6.0 |
| 输入 | Unity Input System | `com.unity.inputsystem` 1.20.0 |
| UI / 文字 | TextMeshPro(内置于 `com.unity.ugui`) | 2.6.0 |
| 测试 | Unity Test Framework(NUnit,PlayMode) | `com.unity.test-framework` 1.8.0 |
| 导航(预留,未使用) | AI Navigation | `com.unity.ai.navigation` 2.0.14 |
| 版本控制 | Git + GitHub,场景/预制体用 **Force Text** 序列化 | — |
| 编辑器自动化 | 自研 Editor 脚本(`WhiteBoxKit` 命名空间)+ [Unity CLI](https://github.com/cli/cli) `unity` 命令做无头批处理生成/截图/测试 | — |

> 引擎版本必须完全一致(`6000.6.0f1`)。用 Unity Hub 打开项目时如果没有这个版本,会提示安装——直接装,不要用相近版本代替,URP/Input System 的包版本是按这个 Editor 版本锁定的。

## 目录结构

```
Assets/
  Editor/WhiteBox/          # 关卡生成器(仅编辑器,不会打进构建包)
    WhiteBoxModules.cs        # 材质 + 15 个基础模块 Prefab + Room_6x6 通用房间壳
    WhiteBoxLevelBuilder.cs   # 房间关系图 + 每个房间的具体布置 + 相机导览路径生成
    WhiteBoxScreenshotTool.cs # 批量截图工具
    WhiteBoxDiagnostics.cs    # 杂项诊断/一次性维护用菜单项
  Scripts/                   # 运行时脚本(会打进构建包),独立 asmdef: WhiteBox.Runtime
    CameraTourDriver.cs        # 自动运镜相机(录屏演示用)
  Scenes/
    WhiteBox_LastLight.unity  # 唯一关卡场景,由 Step 2 全量重新生成
  StudentTools/FlyCamera/    # 课程提供的飞行相机工具,禁止修改其脚本
  WhiteBox/
    Materials/                 # Mat_Structure(灰)/ Mat_Interactive(橙)/ Mat_Clue(青)/ Mat_LightPanel(自发光白)
    Prefabs/                    # Step 1 生成的所有模块 Prefab + Room_6x6
  Tests/PlayMode/            # 独立 asmdef: WhiteBox.PlayMode
    WhiteBoxSmokeTest.cs       # 关卡场景能正常跑几秒不报错
    CameraTourSmokeTest.cs     # 运镜相机能走完全部 waypoint
```

`Screenshots/` 由截图工具生成,已 `.gitignore` 掉(是构建产物,不是源内容)。

## 如何修改关卡(重要,先读这个)

**不要在 Scene 视图里手动拖东西摆房间。** 场景是由代码生成的,任何手动改动会在下次跑生成器时被整体覆盖。真正的"源文件"是:

- `Assets/Editor/WhiteBox/WhiteBoxLevelBuilder.cs` 里的 `BuildGraph()`(房间图:顺序、方向、连接)和各个 `Populate_*` 方法(每个房间放什么家具/贴花/标签)。
- `Assets/Editor/WhiteBox/WhiteBoxModules.cs` 里的模块定义(如果要新增一种基础模块,比如新的家具形状)。

改完代码后,在 Unity 菜单里跑:

1. `Tools > WhiteBox > Step 1 - Import FlyCam + Generate Prefabs`(只有新增/改了模块或材质时才需要重跑)
2. `Tools > WhiteBox > Step 2 - Build Level Scene`(**每次改了房间图或房间内容都要重跑**,会整体重建并覆盖保存 `WhiteBox_LastLight.unity`)
3. `Tools > WhiteBox > Capture Screenshots`(可选,更新参考截图)

也可以不开界面,用命令行无头批处理(CI 或远程机器上常用):

```bash
unity run "<项目路径>" --editor-version 6000.6.0f1 \
  -- -executeMethod WhiteBoxKit.WhiteBoxModules.Step1_ImportAndGeneratePrefabs

unity run "<项目路径>" --editor-version 6000.6.0f1 \
  -- -executeMethod WhiteBoxKit.WhiteBoxLevelBuilder.Step2_BuildLevel

unity run "<项目路径>" --editor-version 6000.6.0f1 \
  -- -executeMethod WhiteBoxKit.WhiteBoxScreenshotTool.CaptureScreenshots
```

### 房间图怎么组织的

`WhiteBoxLevelBuilder.BuildGraph()` 用一串 `Step(kind, direction, label)` 描述一条线性路径,从 `Start` 出发,每一步往 东/南/西/北 挪一格(1 格 = 8m,6m 房间 + 2m 走廊)。`ComputeDoors()` 会根据这条路径自动算出每个房间该在哪面墙开门,`InstantiateRooms()`/`InstantiateCorridors()` 负责真正摆 Prefab。新增房间只需要在 `main` 列表里插一行 `Step`,不用手动算坐标或摆墙。

九个房间当前顺序:`Start → R1 Displaced Object → R2 Locked Counter → E1 Repeat(藏钥匙) → R3 Pattern+Empty Slot → R4 Portable Light → R5 Aftermath → R6 Mirror+False Centre → R7 Final Alignment`。

### 白盒美术规则(别破坏)

- 只用三种材质区分含义:结构(灰 `Mat_Structure`)/ 可交互谜题物件(橙 `Mat_Interactive`)/ 关键痕迹线索(青 `Mat_Clue`)。天花板发光面板用第四种纯功能性材质 `Mat_LightPanel`,不算在规则内。
- 所有位置对齐 0.5m 网格。
- 每个谜题物件上方用 `WhiteBoxModules.CreateLabel(...)` 生成一个 3D TextMeshPro 标签写名字。

## 相机

场景里有两套相机,**默认只有一个是激活的**:

- **FlyCamera**(`Assets/StudentTools/FlyCamera/`,课程发的工具,脚本禁止改):默认激活,自由飞行探索用。默认速度 8 m/s,可用鼠标滚轮实时调(建议 6m 房间用 2~3 更好判断尺度),它本身**没有碰撞**,穿墙是设计如此,不是 bug。
- **TourCamera**(`CameraTour/TourCamera`,`CameraScripts/CameraTourDriver.cs` 驱动):默认**未激活**。勾选激活后会在自己的 `Awake()` 里自动关掉 FlyCamera,然后按房间生成顺序自动飞过全部房间,每间停留 ~2.5s 并左右扫视,适合录屏演示用。参数(速度/停留时间/扫视角度/是否循环)都在 Inspector 里,不用改代码。

## 测试

`Assets/Tests/PlayMode/` 下两个 PlayMode 冒烟测试:

- `WhiteBoxSmokeTest`:加载关卡场景跑几秒,确认没有运行时报错,`Rooms`/`Player` 分组都存在。
- `CameraTourSmokeTest`:激活 TourCamera,确认它能在合理时间内走完全部 9 个 waypoint,并且正确禁用了 FlyCamera。

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
- 关卡场景冲突的正确解法通常不是手动合并 YAML,而是**两边都重新跑一遍 `Step 2`**——因为场景是生成出来的,冲突往往意味着两人同时改了 `WhiteBoxLevelBuilder.cs` 里不同的房间,先合好那份 C# 代码,再重新生成场景覆盖掉冲突的 `.unity` 文件。
- 建议分支模型:`main` 保持随时可运行,日常开发用 `feature/<你的内容>` 分支,提 PR 合并。

## 已知局限 / 后续可以做的

- FlyCamera 无碰撞,是工具本身的设计,不是关卡的问题;如果之后需要"不能穿墙"的效果,得另外做一个带 `CharacterController` 的步行控制器,而不是改 FlyCamera。
- 3D 文字标签的朝向是按"背对房间中心"的启发式算出来的,极少数从非常规角度(比如隔着门看邻近房间)看会显示镜像文字,不影响正常近距离游玩。
- 目前只保留了一个"近乎相同的重复房间"(E1),18→9 精简房间数量时为了给每个房间塞更多细节,牺牲了原设计里"多个重复房间考验玩家能否察觉细微差异"的测试强度,如果后续要恢复这个机制,在 `BuildGraph()` 里加几个 `RoomKind.Repeat` 步骤,并在 `Populate_Repeat` 里给每个实例传不同的道具摆放变体就行。
