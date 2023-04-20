using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ModManager.Data.Interfaces
{
    /// <summary>
    /// Represents a configurable mod
    /// created using ModAPI.
    /// </summary>
    public interface IConfigurableMod
    {
        /// <summary>
        /// The game for which the mod was created.
        /// This should be a name
        /// from enumeration type <see cref="Enums.GameID"/>
        /// </summary>
        string GameID { get; set; }

        /// <summary>
        /// This should be the same as the given type ModName.
        /// This can be found within
        /// the ModAPI runtime configuration file,
        /// xml node Mod, attribute ID.
        /// </summary>
        string ID { get; set; }

        /// <summary>
        /// This can be found within
        /// the ModAPI runtime configuration file,
        /// xml node Mod, attribute UniqueID.
        /// </summary>
        string UniqueID { get; set; }

        /// <summary>
        /// This can be found within
        /// the ModAPI runtime configuration file,
        /// xml node Mod, attribute Version.
        /// </summary>
        string Version { get; set; }

        /// <summary>
        /// List of all of this mod's buttons,
        /// which can be configured in ModAPI.
        /// </summary>
        List<IConfigurableModButton> ConfigurableModButtons { get; set; }

        /// <summary>
        /// Adds a configurable mod button to this mod's <see cref="ConfigurableModButtons"/>.
        /// </summary>
        /// <param name="button"><see cref="IConfigurableModButton"/></param>
        void AddConfigurableModButton(IConfigurableModButton button);

        /// <summary>
        /// Adds a configurable mod button to this mod's <see cref="ConfigurableModButtons"/>.
        /// </summary>
        /// <param name="buttonID"><see cref="IConfigurableModButton.ID"/></param>
        /// <param name="keyBinding"><see cref="IConfigurableModButton.KeyBinding"/></param>
        void AddConfigurableModButton(string buttonID, string keyBinding);

    }
}
