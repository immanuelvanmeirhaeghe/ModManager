using UnityEngine;

namespace ModManager.Data.Interfaces
{
    /// <summary>
    /// Represents a configurable mod button
    /// created using ModAPI.
    /// </summary>
    public interface IConfigurableModButton
    {
        /// <summary>
        /// This button's identifier,
        ///  found within xml node's Button attribute ID.
        /// </summary>
        string ID { get; set; }

        /// <summary>
        /// This button's input keybinding,
        /// found within xml node'd Button text content.
        /// </summary>
        string KeyBinding { get; set; }

        /// <summary>
        /// The input key bound to the mod
        /// based on <see cref="KeyBinding"/>
        /// </summary>
        KeyCode ShortcutKey { get; set; }

        /// <summary>
        /// Button description
        /// </summary>
        string Description { get; set; }

        /// <summary>
        /// Get the <see cref="UnitEngine.KeyCode"/>
        /// for a given <paramref name="keyBinding"/>
        /// </summary>
        /// <param name="keyBinding"></param>
        /// <returns></returns>
        KeyCode GetKeyCode(string keyBinding);

    }
}