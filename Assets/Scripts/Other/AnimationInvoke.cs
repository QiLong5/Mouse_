using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationInvoke : MonoBehaviour
{
    public Enemy enemy;

    public void PlayerAttack()
    {
        Player.instance.IsAttacking=true;
        Player.instance.Attack();
    }
    public void PlayerAttackFinsh()
    {
        Player.instance.IsAttacking =false;
        Player.instance.IsAttack=false;
    }

    public void EnemyAttack()
    {
        if (enemy!=null)
        {
            enemy.AttackPlayer();
        }
    }
}
