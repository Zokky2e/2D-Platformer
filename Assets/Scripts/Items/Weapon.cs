using System;
using UnityEngine;
using UnityEngine.InputSystem;

class Weapon : MonoBehaviour
{
    public float damage;
    public bool isRanged = false;

    private Collider2D enemyInRange;
    private Collider2D weaponSensor;
    private Hero player;
    private bool isEnemyHit = false;
    public void Awake()
    {
        player = this.GetComponent<Hero>();
        weaponSensor = transform.Find("WeaponSensor")?.GetComponent<Collider2D>();
        //player has WeaponSensor as a child game object i need to fetch its collider component
        //this will be used to check if enemy is hit by the collider
    }
    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.tag == "Enemy" && damage > 0)
        {
            Debug.Log("Enemy In Range!");
            if (player.GetCurrentHeroState() == HeroStates.Attack)
            {
                if (!isEnemyHit)
                {
                    collision.GetComponent<Health>()?.TakeDamage(damage);
                    isEnemyHit = true;
                    Debug.Log("Enemy Hit!");
                }
            }
            isEnemyHit = false;
        }
    }
}
