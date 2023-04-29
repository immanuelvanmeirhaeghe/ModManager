using ModAPI;
using ModManager.Data.Enums;
using ModManager.Data.Interfaces;
using ModManager.Data.Modding;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using UnityEngine;

namespace ModManager.Managers
{
    public class ModdingManager : MonoBehaviour
    {
        private static ModdingManager Instance;
        private static readonly string ModuleName = nameof(ModdingManager);
        private static readonly string RuntimeConfiguration = Path.Combine(Application.dataPath.Replace("GH_Data", "Mods"), $"{nameof(RuntimeConfiguration)}.xml");

        public bool IsModEnabled { get; set; } = true;
        
        public int SelectedModIDIndex { get; set; } = 0;
        public string SelectedModID { get; set; } = string.Empty;
        public IConfigurableMod SelectedMod { get; set; } = default;

        public List<IConfigurableMod> ModList { get; set; } = default;
        public string[] ModNamesList { get; set; } = default;
        /// <summary>
        /// Key: (<see cref="IConfigurableMod.ID"/>, <see cref="IConfigurableModButton.ID"/>)
        /// Value: <see cref="IConfigurableModButton.KeyBinding"/>
        /// </summary>
        public Dictionary<(string, string), string> ModListConflicts { get; set; } = default;
        /// <summary>
        /// Key: <see cref="IConfigurableMod.ID"/>
        /// Value: <see cref="Type"/> of mod
        /// </summary>
        public Dictionary<string, Type> ModListLookUp { get; set; } = default;

        public ModdingManager()
        {
            useGUILayout = true;
            ModList = new List<IConfigurableMod>();
            ModListConflicts = new Dictionary<(string, string), string>();
            ModListLookUp  = new Dictionary<string, Type>();
            Instance = this;
        }

        public static ModdingManager Get() => Instance;

        private void HandleException(Exception exc, string methodName)
        {
            string info = $"[{ModuleName}:{methodName}] throws exception -  {exc.TargetSite?.Name}:\n{exc.Message}\n{exc.InnerException}\n{exc.Source}\n{exc.StackTrace}";
            ModAPI.Log.Write(info);
            Debug.Log(info);
        }

        protected virtual void Awake()
        {
            Instance = this;
        }

        protected virtual void OnDestroy()
        {
            Instance = null;
        }

        protected virtual void Start()
        {
            InitData();
        }

        protected virtual void Update()
        {
            if (IsModEnabled)
            {
                InitData();
            }
        }

        protected virtual void InitData()
        {
            ModList = GetModList();
            ModNamesList = GetModNamesList(false);
        }

        public List<IConfigurableMod> GetModList()
        {
            List<IConfigurableMod> modList = new List<IConfigurableMod>();
            try
            {
                if (File.Exists(RuntimeConfiguration))
                {
                    using (XmlReader configFileReader = XmlReader.Create(new StreamReader(RuntimeConfiguration)))
                    {
                        while (configFileReader.Read())
                        {
                            configFileReader.ReadToFollowing("Mod");
                            do
                            {
                                string gameID = GameID.GreenHell.ToString();
                                string modID = configFileReader.GetAttribute(nameof(ConfigurableMod.ID));
                                string uniqueID = configFileReader.GetAttribute(nameof(ConfigurableMod.UniqueID));
                                string version = configFileReader.GetAttribute(nameof(ConfigurableMod.Version));

                                var configurableMod = new ConfigurableMod(gameID, modID, uniqueID, version);

                                configFileReader.ReadToDescendant("Button");
                                do
                                {
                                    string buttonID = configFileReader.GetAttribute(nameof(ConfigurableModButton.ID));
                                    string buttonKeyBinding = configFileReader.ReadElementContentAsString();

                                    configurableMod.AddConfigurableModButton(buttonID, buttonKeyBinding);

                                } while (configFileReader.ReadToNextSibling("Button"));

                                if (!modList.Contains(configurableMod))
                                {
                                    modList.Add(configurableMod);
                                }

                            } while (configFileReader.ReadToNextSibling("Mod"));
                        }
                    }
                }
                return modList;
            }
            catch (Exception exc)
            {
                HandleException(exc, nameof(GetModList));
                modList = new List<IConfigurableMod>();
                return modList;
            }
        }

        public string[] GetModNamesList(bool refresh = false)
        {
            string[] modListNames = default;
            try
            {
                if (refresh)
                {
                    ModList = GetModList();
                    modListNames = new string[ModList.Count];
                }
               
                int modIDIdx = 0;
                foreach (var configurableMod in ModList)
                {
                    modListNames[modIDIdx] = configurableMod.ID;
                    modIDIdx++;
                }
                return modListNames;
            }
            catch (Exception exc)
            {
                HandleException(exc, nameof(GetModList));                
                return modListNames;
            }
        }

        public IConfigurableMod GetSelectedMod(string modID)
        {            
            return ModList.Find(cfgMod => cfgMod.ID == modID);            
        }

        public bool HasConflicts()
        {
            ModListConflicts.Clear();

            var allbindings = new Dictionary<(string, string), string>();
            if (ModList != null)
            {            
                foreach (var mod in ModList)
                {
                    for (int i = 0; i < mod.ConfigurableModButtons.Count; i++)
                    {
                        var btn = mod.ConfigurableModButtons[i];
                        if (allbindings.Values.Contains(btn.KeyBinding))
                        {
                            allbindings.Add((mod.ID, btn.ID), btn.KeyBinding);
                            ModListConflicts.Add((mod.ID, btn.ID), btn.KeyBinding);                           
                        }
                    }                   
                }
            }
            return ModListConflicts.Count > 0;
        }

        public KeyCode GetShortcutKey(string modID, string buttonID)
        {
            return (KeyCode)(ModList?.Find(cfgMod => cfgMod.ID == modID)?.ConfigurableModButtons?.Find(cfgButton => cfgButton.ID == buttonID)?.ShortcutKey);
        }
    }

}
