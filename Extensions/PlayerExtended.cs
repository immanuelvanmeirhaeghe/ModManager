using UnityEngine;

namespace ModManager
{
    class PlayerExtended : Player
    {
        protected override void Start()
        {
            base.Start();
            new GameObject($"__{nameof(ModManager)}__").AddComponent<ModManager>();
        }
    }
}
