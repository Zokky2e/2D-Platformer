using UnityEngine;

// Cached animator parameter ids for the hero and enemy controllers, so Set* calls don't hash strings every frame.
// The names must match the parameters in the Animator Controller assets.
public static class AnimatorParams
{
    public static readonly int AnimState = Animator.StringToHash("AnimState");
    public static readonly int Grounded = Animator.StringToHash("Grounded");
    public static readonly int AirSpeedY = Animator.StringToHash("AirSpeedY");
    public static readonly int WallSlide = Animator.StringToHash("WallSlide");
    public static readonly int LedgeGrab = Animator.StringToHash("LedgeGrab");
    public static readonly int Jump = Animator.StringToHash("Jump");
    public static readonly int Roll = Animator.StringToHash("Roll");
    public static readonly int Attack = Animator.StringToHash("Attack"); // Enemies
    public static readonly int Block = Animator.StringToHash("Block");
    public static readonly int IdleBlock = Animator.StringToHash("IdleBlock");
    public static readonly int Hurt = Animator.StringToHash("Hurt");
    public static readonly int Death = Animator.StringToHash("Death");
    public static readonly int Revive = Animator.StringToHash("Revive");
    public static readonly int NoBlood = Animator.StringToHash("noBlood");

    // Hero combo triggers "Attack1".."Attack3"
    private static readonly int[] heroAttacks =
    {
        Animator.StringToHash("Attack1"), Animator.StringToHash("Attack2"), Animator.StringToHash("Attack3")
    };

    public static int HeroAttack(int comboStep) => heroAttacks[comboStep - 1];
}
