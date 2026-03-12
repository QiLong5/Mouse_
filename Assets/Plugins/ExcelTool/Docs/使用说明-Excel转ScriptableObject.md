# Excel转ScriptableObject工具使用说明

## 🎯 功能概述

这个工具可以将Excel表格自动转换为Unity的ScriptableObject配置资源，大幅提升配置数据的管理效率。

### 主要特性

✅ 自动生成C#类（基于Excel表头）
✅ 自动创建ScriptableObject资源（基于Excel数据）
✅ 支持多种数据类型（int, float, bool, string, Vector2, Vector3, Color, **List<T>**等）
✅ 中文注释自动添加到字段
✅ 多表合并模式（多个工作表合并到一个ScriptableObject）
✅ **两种表格格式：参数表（横向配置） + 结构列表（List数据）**
✅ 命名空间：ExcelTool
✅ **智能两步操作：快速更新数据 / 重新生成**
✅ **配置自动保存：Excel路径和工作表配置保存在资源中**

## 📋 Excel表格格式要求

工具支持两种表格格式，通过**A1单元格**标识表格类型：

---

### 格式1：参数表（横向配置）

**标识：** A1单元格 = `参数表`

**用途：** 存储单个配置参数，如玩家速度、最大生命值等

**格式：**

| A列（第1列） | B列（第2列） | C列（第3列） | D列（第4列） |
|-------------|-------------|-------------|-------------|
| **参数表** | | | |
| 中文说明 | 字段名 | 值 | 类型 |
| 最大生命值 | maxHealth | 100 | int |
| 移动速度 | moveSpeed | 6.0 | float |
| 攻击力列表 | attackValues | (10,20,30) | List<int> |

**说明：**
- 第1行第1列（A1）= "参数表"（标识表格类型）
- 第2行 = 表头
- 第3行开始 = 数据行
- 每一行代表一个字段/变量
- **支持List类型**：如 `List<int>`, `List<float>`, `List<string>`，值格式为 `(1,2,3)`

---

### 格式2：结构列表（List数据）

**标识：** A1单元格 = `结构列表`

**用途：** 存储多行结构化数据，如敌人列表、道具列表等

**格式：**

| A列 | B列 | C列 | D列 |
|-----|-----|-----|-----|
| **结构列表** | int | float | string |
| 列表说明 | 敌人ID | 移动速度 | 敌人名称 |
| enemyList | enemyId | enemySpeed | enemyName |
| 1 | 101 | 5.5 | 小怪A |
| 2 | 102 | 6.0 | 小怪B |
| 3 | 103 | 4.8 | 小怪C |

**说明：**
- 第1行第1列（A1）= "结构列表"（标识表格类型）
- 第1行，B列开始 = 参数类型（int, float, string等）
- 第2行 = 表头说明
- 第3行：A3 = 列表名，B3 = 参数1名，C3 = 参数2名...
- 第4行开始 = 数据行
  - A列 = 序号/索引
  - B列开始 = 各参数的值
- 可以继续添加更多列（参数4、参数5...）

---

### 示例：混合使用两种格式

**Excel文件：** `游戏配置.xlsx`

**工作表1：玩家数据（参数表）**

| A列 | B列 | C列 | D列 |
|-----|-----|-----|-----|
| **参数表** | | | |
| 中文说明 | 字段名 | 值 | 类型 |
| 最大生命值 | maxHealth | 100 | int |
| 移动速度 | moveSpeed | 6.0 | float |

**工作表2：敌人列表（结构列表）**

| A列 | B列 | C列 | D列 |
|-----|-----|-----|-----|
| **结构列表** | int | float | string |
| 列表说明 | 敌人ID | 移动速度 | 敌人名称 |
| enemies | enemyId | speed | name |
| 1 | 101 | 5.5 | 小怪A |
| 2 | 102 | 6.0 | 小怪B |

**生成的C#类：**

```csharp
using System.Collections.Generic;
using UnityEngine;

namespace ExcelTool
{
    [CreateAssetMenu(fileName = "GameConfig", menuName = "ExcelTool/GameConfig")]
    public class GameConfig : ScriptableObject
    {
        // 参数表生成的字段
        public 玩家数据Data 玩家数据 = new 玩家数据Data();

        // 结构列表生成的List
        public List<EnemiesItem> enemies = new List<EnemiesItem>();

        [System.Serializable]
        public class 玩家数据Data
        {
            public int maxHealth = 100;
            public float moveSpeed = 6.0f;
        }

        [System.Serializable]
        public class EnemiesItem
        {
            public int enemyId;
            public float speed;
            public string name;
        }
    }
}
```

---

## 🚀 使用步骤（简化流程）

### 工作流程概述

工具会自动检测是否存在 `GameConfig.asset` 资源：

**情况1：已存在资源**
- 自动从资源中读取Excel路径和工作表配置
- 显示当前配置的工作表信息
- 提供三个操作：快速更新数据、重新生成、重新选择工作表

**情况2：不存在资源（首次使用）**
- 输入Excel文件路径
- 选择要合并的工作表
- 创建新的ScriptableObject资源
- 配置信息会自动保存到资源中

### 1. 打开工具窗口

在Unity菜单栏：`Tools > ExcelTool > Excel转ScriptableObject`

### 2. 首次使用 - 创建配置

### 2. 首次使用 - 创建配置

如果还没有 `GameConfig.asset` 资源：

**选择Excel文件：**
- 点击「浏览」按钮选择Excel文件
- 或直接输入文件路径（默认：`Assets/Scripts/GameDataConfig/老鼠榨汁.xlsx`）
- 工作表列表会**自动加载**

**选择要合并的工作表：**
- 勾选想要合并到一个ScriptableObject中的工作表
- 可以选择多个工作表，每个工作表会成为一个嵌套类
- 点击「确认工作表选择」按钮

**创建ScriptableObject：**
- 点击「重新生成」按钮创建资源
- Excel路径和工作表配置会**自动保存到资源中**
- 下次打开工具时会自动读取这些配置

### 3. 日常使用 - 更新数据

如果已经存在 `GameConfig.asset` 资源：

**自动加载配置：**
- 工具会自动从资源中读取Excel路径和工作表配置
- 显示当前使用的工作表列表
- 无需重新输入路径或选择工作表

**选择操作类型：**
根据你的修改类型选择：

#### 🔵 快速更新数据（改数值时用）

**使用场景：**
- ✅ 只修改了Excel中的**数值**（例如：生命值从100改成150）
- ✅ 没有新增或删除字段
- ✅ 没有修改字段的类型

**特点：**
- ⚡ 速度快，不重新编译脚本
- 🎯 直接更新现有ScriptableObject资源的值
- 🚀 适合频繁调整数值的策划人员

#### 🟠 重新生成（改字段时用）

**使用场景：**
- ✅ 新增了字段（例如：添加了"防御力"字段）
- ✅ 删除了字段
- ✅ 修改了字段的类型（例如：int改成float）
- ✅ 修改了字段名
- ✅ 第一次创建配置

**特点：**
- 🔄 重新生成C#脚本并编译
- 🆕 创建新资源或更新现有资源的结构
- 🛠️ 完整的重新生成流程

#### 🔄 重新选择工作表（需要修改工作表配置时用）

**使用场景：**
- ✅ 需要添加或删除工作表
- ✅ 需要更改合并哪些工作表

**操作步骤：**
- 点击「重新选择工作表」按钮
- 重新勾选想要的工作表
- 点击「确认工作表选择」
- 点击「重新生成」应用更改

### 4. 查看操作日志

窗口底部会显示操作日志，包括：
- ✓ 成功消息（绿色勾号）
- ❌ 错误消息（红色叉号）
- 详细的操作步骤和结果

## 📝 生成示例

### 输入Excel

**工作表1：Player（玩家配置）**
| 中文说明 | 字段名 | 值 | 类型 |
|----------|--------|-----|------|
| 最大生命值 | maxHealth | 100 | int |
| 移动速度 | moveSpeed | 6.0 | float |

**工作表2：Enemy（敌人配置）**
| 中文说明 | 字段名 | 值 | 类型 |
|----------|--------|-----|------|
| 生命值 | health | 50 | int |
| 攻击力 | attack | 15 | int |

### 输出：生成的C#类（GameConfig.cs）

```csharp
using UnityEngine;

namespace ExcelTool
{
    /// <summary>
    /// GameConfig 配置数据
    /// 自动生成，请勿手动修改
    /// </summary>
    [CreateAssetMenu(fileName = "GameConfig", menuName = "ExcelTool/GameConfig")]
    public class GameConfig : ScriptableObject
    {
        public PlayerData player = new PlayerData();
        public EnemyData enemy = new EnemyData();

        /// <summary>
        /// Player 配置数据
        /// </summary>
        [System.Serializable]
        public class PlayerData
        {
            /// <summary>
            /// 最大生命值
            /// </summary>
            public int maxHealth = 100;

            /// <summary>
            /// 移动速度
            /// </summary>
            public float moveSpeed = 6.0f;
        }

        /// <summary>
        /// Enemy 配置数据
        /// </summary>
        [System.Serializable]
        public class EnemyData
        {
            /// <summary>
            /// 生命值
            /// </summary>
            public int health = 50;

            /// <summary>
            /// 攻击力
            /// </summary>
            public int attack = 15;
        }
    }
}
```

### 输出：生成的ScriptableObject资源

在 `Assets/Scripts/GameDataConfig/` 下会生成：
- `GameConfig.asset` - 包含所有工作表数据的ScriptableObject

## 🔧 支持的数据类型

| Excel类型 | C#类型 | 示例值 | 说明 |
|-----------|--------|--------|------|
| int / int32 | int | 100 | 整数 |
| float / single | float | 6.0 | 浮点数 |
| double | double | 3.14159 | 双精度浮点数 |
| bool / boolean | bool | true | 布尔值 |
| string / text | string | "文本" | 字符串 |
| long / int64 | long | 999999999 | 长整型 |
| vector2 | Vector2 | (1,3) 或 1,3 | Unity Vector2 |
| vector3 | Vector3 | (1,2,3) 或 1,2,3 | Unity Vector3 |
| color | Color | (1,0,0,1) 或 1,0,0,1 | Unity Color (RGBA) |
| **List\<int\>** | **List\<int\>** | **(10,20,30)** | **整数列表（仅参数表）** |
| **List\<float\>** | **List\<float\>** | **(1.5,2.3,4.8)** | **浮点数列表（仅参数表）** |
| **List\<string\>** | **List\<string\>** | **(苹果,香蕉,橙子)** | **字符串列表（仅参数表）** |

**注意：** List类型仅在**参数表**中使用，值格式为 `(值1,值2,值3)`。结构列表本身就是List结构，不需要额外指定。

## 💡 使用生成的ScriptableObject

### 方式1：在Inspector中引用（参数表）

```csharp
using UnityEngine;
using ExcelTool;

public class GameManager : MonoBehaviour
{
    [Header("游戏配置")]
    public GameConfig gameConfig;

    void Start()
    {
        // 访问参数表数据
        Debug.Log($"玩家生命值: {gameConfig.玩家数据.maxHealth}");
        Debug.Log($"玩家速度: {gameConfig.玩家数据.moveSpeed}");

        // 访问List类型数据
        foreach(int value in gameConfig.玩家数据.attackValues)
        {
            Debug.Log($"攻击力: {value}");
        }
    }
}
```

### 方式2：访问结构列表数据

```csharp
using UnityEngine;
using ExcelTool;

public class EnemyManager : MonoBehaviour
{
    [Header("游戏配置")]
    public GameConfig gameConfig;

    void Start()
    {
        // 遍历结构列表
        foreach(var enemy in gameConfig.enemies)
        {
            Debug.Log($"敌人 {enemy.enemyId}: {enemy.name}, 速度={enemy.speed}");
        }

        // 根据ID查找敌人
        var enemy101 = gameConfig.enemies.Find(e => e.enemyId == 101);
        if (enemy101 != null)
        {
            Debug.Log($"找到敌人: {enemy101.name}");
        }
    }
}
```

### 方式3：通过Resources加载
}
```

### 方式2：通过Resources加载

将生成的资源放在 `Resources` 文件夹中：

```csharp
using UnityEngine;
using ExcelTool;

public class ConfigLoader : MonoBehaviour
{
    void Start()
    {
        // 加载配置
        GameConfig config = Resources.Load<GameConfig>("GameConfig");

        // 使用数据
        float speed = config.player.moveSpeed;
        Debug.Log($"玩家速度: {speed}");
    }
}
```

## ⚠️ 注意事项

1. **Excel格式必须严格遵守**
   - **参数表格式**：A1="参数表"，第2行=表头，第3行开始：第1列=中文注释，第2列=字段名，第3列=值，第4列=类型
   - **结构列表格式**：A1="结构列表"，第1行B列开始=参数类型，第2行=表头说明，第3行=字段名，第4行开始=数据
   - **旧格式兼容**：如果A1单元格不是"参数表"或"结构列表"，会自动使用旧的参数表格式（第1行=表头，第2行开始=数据）
   - 字段名不能为空

2. **表格类型标识**
   - **必须在A1单元格**标识表格类型："参数表" 或 "结构列表"
   - 同一个Excel文件中可以混合使用两种格式（不同工作表使用不同格式）
   - 工具会自动检测每个工作表的类型并正确处理

3. **List类型使用规则**
   - **参数表**中可以使用 `List<int>`, `List<float>`, `List<string>` 等
   - 值格式：`(值1,值2,值3)`，例如：`(10,20,30)` 或 `(苹果,香蕉,橙子)`
   - **结构列表**本身就是List结构，不需要在字段类型中使用List

4. **生成的脚本会被覆盖**
   - 每次「重新生成」都会覆盖同名脚本
   - 请勿手动修改生成的脚本

5. **数据类型要准确**
   - 确保第4列（参数表）或第1行B列开始（结构列表）的类型与实际数据匹配
   - 类型转换失败会使用默认值

6. **编译延迟**
   - 「重新生成」后Unity需要编译
   - 资源会在编译完成后自动创建
   - 如果资源未自动创建，请重新执行生成

7. **操作选择**
   - **仅改数值** → 使用「快速更新数据」
   - **改字段/首次生成** → 使用「重新生成」

6. **字段名规范**
   - 第2列的英文字段名要符合C#命名规范
   - 建议使用驼峰命名：`playerSpeed`, `maxHealth`

## 🔍 常见问题

**Q: 我只是改了一个数值，用哪个按钮？**

A: 使用「快速更新数据」按钮，速度更快，不需要等待编译。

**Q: 我新增了一个字段，用哪个按钮？**

A: 使用「重新生成」按钮，因为需要重新生成脚本和类结构。

**Q: 生成后看不到资源文件？**

A: Unity正在编译，等待几秒后刷新Project窗口。如果仍未出现，请重新点击生成按钮。

**Q: 如何修改已生成的数据？**

A:
1. 直接在Unity Inspector中修改ScriptableObject资源（临时修改）
2. 或修改Excel后使用「快速更新数据」（永久修改）

**Q: Vector2、Vector3、Color的格式是什么？**

A:
- Vector2: `(1,3)` 或 `1,3`
- Vector3: `(1,2,3)` 或 `1,2,3`
- Color: `(1,0,0,1)` 或 `1,0,0,1` (RGBA格式)

**Q: 我不知道该用哪个按钮？**

A: 如果不确定，使用「重新生成」按钮总是安全的，只是可能需要等待编译。

**Q: 我换了Excel文件的位置，怎么更新路径？**

A: 直接在工具窗口中修改Excel文件路径，然后点击「快速更新数据」或「重新生成」即可。新路径会自动保存到资源中。

**Q: 配置信息保存在哪里？**

A: Excel路径和工作表配置保存在 `GameConfig.asset` 资源中（作为隐藏字段），随资源一起版本控制。

## 🎯 最佳实践

1. **统一Excel模板**：团队使用统一的横向配置格式
2. **命名规范**：使用有意义的英文字段名
3. **数据验证**：生成后检查日志确认数据是否正确
4. **版本控制**：将生成的资源文件加入版本控制
5. **只读访问**：运行时不要修改ScriptableObject数据
6. **使用快速更新**：日常调数值时优先使用「快速更新数据」按钮
7. **查看日志**：遇到问题时查看窗口底部的操作日志
8. **配置随资源走**：Excel路径和工作表配置自动保存在资源中，无需额外配置

## 📁 相关文件

- 核心工具：[ExcelToScriptableObject.cs](../Scripts/ExcelToScriptableObject.cs)
- 编辑器窗口：[ExcelToScriptableObjectEditor.cs](../ToolEditor/ExcelToScriptableObjectEditor.cs)
- Excel读取器：[ExcelReader.cs](../Scripts/ExcelReader.cs)
- 生成的脚本：`Assets/Plugins/ExcelTool/Scripts/Generated/`
- 生成的资源：`Assets/Scripts/GameDataConfig/`
