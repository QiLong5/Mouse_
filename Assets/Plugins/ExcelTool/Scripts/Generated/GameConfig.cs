using System.Collections.Generic;
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
        [Header("=== 工具元数据（请勿手动修改） ===")]
        /// <summary>
        /// 元数据：Excel文件路径
        /// </summary>
        [Tooltip("此文件由工具自动管理，请勿手动修改")]
        public string _excelFilePath = "";

        /// <summary>
        /// 元数据：使用的工作表索引（逗号分隔）
        /// </summary>
        [Tooltip("此文件由工具自动管理，请勿手动修改")]
        public string _sheetIndices = "";

        [Space(20)]
        /// <summary>
        /// 玩家数据
        /// </summary>
        public 玩家数据Data 玩家数据 = new 玩家数据Data();

        /// <summary>
        /// 老鼠数据
        /// </summary>
        public 老鼠数据Data 老鼠数据 = new 老鼠数据Data();

        /// <summary>
        /// 生产交易相关
        /// </summary>
        public 生产交易相关Data 生产交易相关 = new 生产交易相关Data();

        /// <summary>
        /// 顾客配置 - Customer
        /// </summary>
        public List<CustomerItem> customer = new List<CustomerItem>();

        /// <summary>
        /// 玩家数据 数据
        /// </summary>
        [System.Serializable]
        public class 玩家数据Data
        {
            /// <summary>
            /// 玩家移动速度
            /// </summary>
            public float playerSpeed = 6.0f;

            /// <summary>
            /// 玩家捕捉上限初始
            /// </summary>
            public int playerCatchMax = 5;

            /// <summary>
            /// 玩家捕捉上限二级
            /// </summary>
            public int playerCatchMaxLevel2 = 20;

            /// <summary>
            /// 玩家攻击半径初始
            /// </summary>
            public float playerAttackRadius = 3.0f;

            /// <summary>
            /// 玩家攻击半径二级
            /// </summary>
            public float playerAttackRadiusLevel2 = 5.0f;

            /// <summary>
            /// 玩家初始携带老鼠数量
            /// </summary>
            public int playerinitRatCount = 10;

        }

        /// <summary>
        /// 老鼠数据 数据
        /// </summary>
        [System.Serializable]
        public class 老鼠数据Data
        {
            /// <summary>
            /// 老鼠数量上限
            /// </summary>
            public int enemyCount = 10;

            /// <summary>
            /// 老鼠移动速度
            /// </summary>
            public float enemySpeed = 8.0f;

            /// <summary>
            /// 老鼠漫游范围
            /// </summary>
            public Vector2 enemyPatrolRadiusIn = new Vector2(1.0f, 3.0f);

            /// <summary>
            /// 玩家进入后老鼠漫游范围
            /// </summary>
            public Vector2 enemyPatrolRadiusOut = new Vector2(2.0f, 5.0f);

        }

        /// <summary>
        /// 生产交易相关 数据
        /// </summary>
        [System.Serializable]
        public class 生产交易相关Data
        {
            /// <summary>
            /// 老鼠洞上限
            /// </summary>
            public int materialCount_Rat = 5;

            /// <summary>
            /// 一个老鼠榨出汁液数量
            /// </summary>
            public float productCount_Juice = 2.0f;

            /// <summary>
            /// 一瓶老鼠汁熬出药瓶数量
            /// </summary>
            public float materialCount_Juice = 1.0f;

        }

        /// <summary>
        /// 顾客配置 - Customer 数据项
        /// </summary>
        [System.Serializable]
        public class CustomerItem
        {
            /// <summary>
            /// 需要的商品数量
            /// </summary>
            public int needCount;

            /// <summary>
            /// 提供的金币
            /// </summary>
            public int giveGoin;

        }

    }
}
