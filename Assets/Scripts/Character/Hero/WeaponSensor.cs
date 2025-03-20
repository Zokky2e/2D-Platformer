using UnityEngine;

class WeaponSensor : MonoBehaviour
{
    private Collider2D enemyInRange;
    private Hero player;
    private bool isEnemyHit = false;
    public void Awake()
    {
        player = Object.FindAnyObjectByType<Hero>();
        //player has WeaponSensor as a child game object i need to fetch its collider component
        //this will be used to check if enemy is hit by the collider
    }
    private void OnTriggerStay2D(Collider2D collision)
    {
        Debug.Log(collision.tag);
        if (collision.tag == "Enemy" && player.stats.TotalDamage > 0)
        {
            if (player.GetCurrentHeroState() == HeroStates.Attack && !isEnemyHit)
            {
                collision.GetComponent<Health>()?.TakeDamage(player.stats.TotalDamage);
                isEnemyHit = true;
            }
            if (player.GetCurrentHeroState() != HeroStates.Attack)
            {
                isEnemyHit = false;
            }
        }
    }

    public void Update()
    {
        float rotationAngle = player.FacingDirection == -1 ? 180f : 0f;
        transform.rotation = Quaternion.Euler(0, rotationAngle, 0);
    }
}
