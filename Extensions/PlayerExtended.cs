using UnityEngine;

namespace ModManager.Extensions
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
