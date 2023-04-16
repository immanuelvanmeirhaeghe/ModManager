using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModManager
{
    /// <summary>
    /// Represents  a mod created using ModAPI
    /// </summary>
    public class ConfigurableMod
    {

        /// <summary>
        /// The game for which the mod was created.
        /// </summary>
        public string GameID { get; set; } = string.Empty;

        /// <summary>
        /// xml node Mod, attribute ID
        /// </summary>
        public string ID { get; set; } = string.Empty;

        /// <summary>
        /// xml node Mod, attribute UniqueID
        /// </summary>
        public string UniqueID { get; set; } = string.Empty;

        /// <summary>
        /// xml node Mod, attribute Version
        /// </summary>
        public string Version { get; set; } = string.Empty;
        
        /// <summary>
        /// List of mod buttons whcih can be configured in ModAPI.
        /// </summary>
        public List<ConfigurableModButton> ConfigurableModButtons { get; set; }

        public ConfigurableMod()
        {
            ConfigurableModButtons = new List<ConfigurableModButton>();
        }

        public ConfigurableMod(string gameId, string id, string uniqueID, string version)
            : this()
        {
            GameID = gameId;
            ID = id;
            UniqueID = uniqueID;
            Version = version;
        }

        public void AddConfigurableModButton(string buttonId, string keyBinding)
        {
            var button = new ConfigurableModButton(buttonId, keyBinding);
           if (button != null)
            {
                AddConfigurableModButton(button);
            }
        }

        public void AddConfigurableModButton(ConfigurableModButton button)
        {
            if (button != null && !ConfigurableModButtons.Contains(button))
            {
                ConfigurableModButtons.Add(button);
            }
        }
    }
}
