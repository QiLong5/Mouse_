using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StateManager
{
    
}

/// <summary>
/// Npc状态
/// </summary>
public enum NpcState
{
    Idle,
    Run,
    Attack,
    RunAttack,
    Hit,
    Work,
    Die
}
/// <summary>
/// 敌人状态
/// </summary>
public enum EnemyState
{
    Idle,
    Run,
    Patrol,//警戒
    Chase,//追击
    Attack,//攻击
    Hit,//受伤
    Die//死亡
}



