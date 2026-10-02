using UnityEngine;
namespace PharmaBrawl
{
    // Ability presentation is handled by the character views; no aiming overlays.
    public sealed class AbilityEffects:MonoBehaviour
    {
        public void Initialize(ArenaSimulation sim,int local=0){}
        public void Emit(ArenaSimulation.CombatEvent e){}
        public void Clear(){enabled=false;}
    }
}
