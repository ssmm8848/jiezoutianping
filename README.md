# 引力节拍 · Gravity Beat

用 Unity 与 C# 开发的四轨节奏游戏原型，将音乐判定与动态重力机制结合。玩家不仅需要准确击中音符，还要管理判定线偏移、稳定度和校准能量，在节奏中维持平衡。

本文依据 2026-10-13 本地工作目录中的最新代码整理，包含尚未提交到原有版本控制的修改。

## 核心体验

- **3D 重力模式**：普通、重、轻、稳定四种重量音符。重音符推动判定线下沉，轻音符推动其上浮，稳定音符帮助回正；判定线通过弹簧与阻尼产生反馈。
- **配平机制**：在有效窗口内以 Perfect / Good 命中相反重量音符，可触发配平，收敛偏移与速度，并获得额外分数、稳定度与能量。稳定音符也参与相关配平处理。
- **主动校准**：消耗能量使用 Space 校准判定线，并短暂锁定重力变化。
- **稳定度系统**：Bad、Miss 及过大的判定线偏移会扣除稳定度；稳定度耗尽时失败。
- **独立 2D 模式**：提供固定判定线的四轨演奏界面，复用歌曲、谱面和计分数据；当前 2D 控制器未接入 3D 的重量、稳定度与配平玩法。
- **选曲与难度**：SongConfig 组织歌曲、封面、试听片段以及 Easy / Normal / Hard 谱面；缺失谱面的难度会在界面提示。
- **完整演奏流程**：开场倒计时、重力教程、暂停与恢复倒计时、重试、结算、返回选曲。
- **本地设置与记录**：音量、音符流速、输入偏移及歌曲/难度对应的最佳分数和连击通过 PlayerPrefs 保存。

## 操作

| 按键 | 功能 |
|---|---|
| S / D / J / K | 从左到右四条轨道 |
| Space | 3D 模式主动校准，需足够能量 |
| Esc | 暂停 / 恢复 |
| 鼠标 | 菜单、选曲、难度与设置 |

以上轨道按键已核对 `RhythmInput.cs`、3D 场景序列化配置与 `Rhythm2DGameManager.cs`。3D 控件可在 Inspector 中重新配置。

## 环境与运行

- Unity Editor：**2021.3.45f2c1**，以 `ProjectSettings/ProjectVersion.txt` 为准。
- 主要 UI 依赖：TextMesh Pro **3.0.6**、UGUI **1.0.0**；完整依赖见 `Packages/manifest.json` 与锁文件。

1. 克隆仓库，通过 Unity Hub 的 Add / 添加选择仓库根目录。
2. 使用对应 Unity 版本打开，等待包解析与资源导入。
3. 打开 `Assets/Scenes/MainMenuScene.unity`，点击 Play。
4. 进入选曲，选择歌曲和难度，再进入 3D 或 2D 模式。

Build Settings 已启用以下场景：`MainMenuScene`、`SongSelectScene`、`RhythmTestScene`、`Rhythm2DScene`。构建桌面程序时使用 Unity 的 File → Build Settings，并保持这些场景启用。

## 谱面与扩展

`SongConfig` 是 ScriptableObject，可通过 **Create → Gravity Beat → Song Config** 创建，配置 AudioClip、封面、试听起点/时长以及三个难度的 JSON TextAsset。

谱面由 `ChartJsonData` / `NoteData` 描述，包含歌曲元信息、BPM、offset、提前生成时间与音符列表。音符使用秒级 `time`、0–3 的 `lane`、Tap 类型和 `weightType`；重量值支持 `Normal`、`Heavy`、`Light`、`Stabilize`。新增歌曲还需接入选曲场景实际使用的歌曲列表。

判定依据音乐时间与输入偏移计算。脚本默认时间窗为 Perfect 50 ms、Good 100 ms、Bad 150 ms；3D 中序列化参数可由场景或 Inspector 覆盖。延迟校准范围为 -200 至 +200 ms。

## 代码结构

| 路径 / 模块 | 职责 |
|---|---|
| `Assets/Scripts/GameManager.cs` | 3D 状态、音乐计时、暂停与结算流程 |
| `ChartData.cs`、`SongConfig.cs`、`SelectedSongData.cs` | 谱面解析、歌曲配置与跨场景选曲状态 |
| `NoteSpawner.cs`、`Note.cs`、`RhythmInput.cs` | 音符生成、移动与输入 |
| `JudgeSystem.cs`、`ScoreManager.cs` | 时间判定、计分、配平奖励与本地最佳记录 |
| `JudgeLineController.cs`、`StabilitySystem.cs` | 重力偏移、校准能量、配平与失败条件 |
| `Rhythm2DGameManager.cs` | 独立的 2D 演奏流程及运行时 UI |
| `GravityHUD.cs`、`GravityTutorialUI.cs`、`HitEffectSystem.cs` | 重力提示、教程与反馈 |
| `Assets/Audio`、`Assets/Charts`、`Assets/SongConfigs` | 音乐、谱面与歌曲配置 |
| `Assets/Scenes`、`Assets/Prefabs` | 场景与预制体 |

## 当前范围

这是持续迭代的本地单人游戏原型，README 描述源码已有功能，不代表已完成本次 Unity 构建或逐项运行验收。运行结果以对应版本 Unity 中的实际场景为准。

仓库保留 Unity `.meta`、Assets、Packages 和 ProjectSettings，排除 Library、Temp、Logs、UserSettings、构建产物和旧 ZIP。首次打开会重新生成缓存。第三方音乐、字体与素材按各自权利和许可使用；仓库未为这些资源授予额外的再分发许可。
