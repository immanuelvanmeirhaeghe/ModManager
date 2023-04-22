using ModManager.Data.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ModManager.Data.Modding
{
    /// <summary>
    /// Represents  a base class for a  mod 
    /// created using ModAPI
    /// </summary>
    public class ConfigurableMod : IConfigurableMod
    {
        public string GameID { get; set; } = string.Empty;
                
        public string ID { get; set; } = string.Empty;
                
        public string UniqueID { get; set; } = string.Empty;
                
        public string Version { get; set; } = string.Empty;
                
        public List<IConfigurableModButton> ConfigurableModButtons { get; set; }
        
        public ConfigurableMod()
        {
            ConfigurableModButtons = new List<IConfigurableModButton>();
        }

        public ConfigurableMod(string gameID, string modID, string uniqueID, string version)
            : this()
        {
            GameID = gameID;
            ID = modID;
            UniqueID = uniqueID;
            Version = version;
        }

        public void AddConfigurableModButton(string buttonID, string keyBinding)
        {
            var button = new ConfigurableModButton(buttonID, keyBinding);
           if (button != null)
            {
                AddConfigurableModButton(button);
            }
        }

        public void AddConfigurableModButton(IConfigurableModButton button)
        {
            if (button != null && ConfigurableModButtons != null && !ConfigurableModButtons.Contains(button))
            {
                ConfigurableModButtons.Add(button);
            }
        }
    }
}
