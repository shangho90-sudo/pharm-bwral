using UnityEngine;

namespace PharmaBrawl
{
    public enum AttackKind { Capsule, Lightning, Arrow, Wave, Burst, Artillery, Assassin, Summoner, Fan, Poison }

    [CreateAssetMenu(menuName = "Pharma Brawl/Character")]
    public sealed class CharacterDefinition : ScriptableObject
    {
        public string displayName, role, voiceLine, attackDescription, skillDescription, ultimateDescription;
        public AttackKind kind;
        public Color color = Color.red;
        public float maxHp = 3000, speed = 5, damage = 400, attackInterval = .7f, range = 9;
        public float projectileSpeed = 15, skillCooldown = 6, ultimateRequirement = 2800;
        public AudioClip ultimateVoice;
        public GameObject characterPrefab, weaponPrefab;
        public Vector3 weaponGripPosition, weaponGripEuler;
        public float weaponLength = 1.3f;
    }
}
