using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameDataEditor :MonoSingleton<GameDataEditor>
{
    [Header("玩家相关")]
    [Tooltip("玩家移动速度")] public float playerSpeed = 9f;
    [Tooltip("玩家旋转平滑系数")] public float playerTurnSmoothTime = 0.1f;
    [Tooltip("玩家最大血量")] public  float playerMaxHp = 100f;
    [Tooltip("玩家攻击伤害")] public int playerAamage = 50;
    [Tooltip("玩家攻击间隔")] public float playerAttackInterval = 1f;
    [Tooltip("玩家攻击半径")] public  float playerAttackRadius = 3f;
    [Range(-90, 90)][Tooltip("攻击范围起始角度")] public float startAngle = 0f;
    [Range(90, 270)][Tooltip("攻击范围结束角度")] public float endAngle = 90f;

    [Header("物品抛物线最高点")]
    public float itemHeightY = 10f;

    [Header("敌人相关")]
    [Tooltip("敌人数量")] public int enemyCount = 100;
    [Tooltip("敌人移动速度")] public float enemySpeed = 4.5f;
    [Tooltip("敌人击退力度")] public float enemyKnockbackForc =10f;
    [Tooltip("敌人最大血量")] public float enemyMaxHp = 3f;
    [Tooltip("敌人警戒半径")] public float enemyAlertRadius = 6f;
    [Tooltip("敌人攻击半径")] public float enemyAttackRadius = 3f;
    [Tooltip("敌人攻击间隔")] public float enemyAttackInterval = 1f;

    [Tooltip("敌人限定范围起点(左下)")] public Transform patrolAreaMin;
    [Tooltip("敌人限定范围终点(右上)")] public Transform patrolAreaMax;
    [Tooltip("敌人受伤闪白材质")] public Material enemyHurtMat;

    [Header("交易相关")]
    [Tooltip("每个顾客需要物品数量")] public int customerNeedCount = 3;
    [Tooltip("每个顾客给的金币")] public int customerGiveGoin = 3;
    [Tooltip("物品交易间隔")] public float itemSpawnInterval = 0.1f;
}