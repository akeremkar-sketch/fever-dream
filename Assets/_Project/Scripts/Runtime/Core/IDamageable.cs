namespace FeverDream.Core
{
    public interface IDamageable
    {
        DamageResult TakeDamage(in DamageInfo info);
    }
}
