using UnityEngine;

namespace EverdrivenDays
{
    [CreateAssetMenu(fileName = "PlayerStrongAttackData", menuName = "Custom/Player/Grounded/Strong Attack Data")]
    public class PlayerStrongAttackData : ScriptableObject
    {
        [field: SerializeField] [field: Range(0f, 1f)] public float SpeedModifier { get; private set; } = 0.5f;
        [field: SerializeField] public PlayerRotationData RotationData { get; private set; }
        [field: SerializeField] [field: Range(0, 10)] public int ConsecutiveStrongAttacksLimitAmount { get; private set; } = 3;
        [field: SerializeField] [field: Range(0f, 5f)] public float StrongAttackLimitReachedCooldown { get; private set; } = 1.5f;
        [field: SerializeField] [field: Range(0f, 2f)] public float TimeToBeConsideredConsecutive { get; private set; } = 1f;
    }
}