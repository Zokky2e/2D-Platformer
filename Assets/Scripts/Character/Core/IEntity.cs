
namespace Assets.Scripts
{
    public interface IEntity
    {
        public bool IsBlocking();
        public void TakeDamage();
        public void Die();
    }
}
