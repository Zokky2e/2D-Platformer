
namespace Assets.Scripts
{
    public interface IEntity
    {
        public bool IsBlocking();
        public float TakeDamage(float _damage);
        public void Die();
    }
}
