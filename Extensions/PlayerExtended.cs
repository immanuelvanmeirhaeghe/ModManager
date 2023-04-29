using ModManager.Managers;
using UnityEngine;

namespace ModManager.Extensions
{
    class PlayerExtended : Player
    {
        protected override void Start()
        {
            base.Start();
            new GameObject($"__{nameof(ModManager)}__").AddComponent<ModManager>();
            new GameObject($"__{nameof(StylingManager)}__").AddComponent<StylingManager>();
            new GameObject($"__{nameof(ModdingManager)}__").AddComponent<ModdingManager>();
            new GameObject($"__{nameof(MultiplayerManager)}__").AddComponent<MultiplayerManager>();
            new GameObject($"__{nameof(MenuLogScreen)}__").AddComponent<MenuLogScreen>();
        }
    }
}
