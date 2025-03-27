using System;
using UnityEngine;

namespace EverdrivenDays
{
    [Serializable]
    public class PlayerAttackData
    {
        [field: SerializeField][field: Range(1f, 3f)] public float SpeedModifier { get; private set; } = 0f;
        [field: SerializeField] public PlayerRotationData RotationData { get; private set; }
        [field: SerializeField][field: Range(0f, 2f)] public float TimeToBeConsideredConsecutive { get; private set; } = 1f;
        [field: SerializeField][field: Range(1, 10)] public int ConsecutiveAttacksLimitAmount { get; private set; } = 2;
        [field: SerializeField][field: Range(0f, 5f)] public float AttackLimitReachedCooldown { get; private set; } = 1.75f;
    }
}