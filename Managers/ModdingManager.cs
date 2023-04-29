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
        public Vector2 ModListScrollViewPosition { get; set; } = default;
        public int SelectedModIDIndex { get; set; } = 0;
        public string SelectedModID { get; set; } = string.Empty;
        public IConfigurableMod SelectedMod { get; set; } = default;
        public List<IConfigurableMod> ConfigurableModList { get; set; } = default;
        public string[] ModListNames { get; set; } = default;
        /// <summary>
        /// Key: (<see cref="IConfigurableMod.ID"/>, <see cref="IConfigurableModButton.ID"/>)
        /// Value: <see cref="IConfigurableModButton.KeyBinding"/>
        /// </summary>
        public Dictionary<(string, string), string> ModConflictList  = new Dictionary<(string, string), string>();

        public ModdingManager()
        {
            useGUILayout = true;
            ConfigurableModList = new List<IConfigurableMod>();
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

        private void OnDestroy()
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
            ConfigurableModList = GetModList();
            ModListNames = GetModListNames();
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

        public string[] GetModListNames(bool refresh = false)
        {
            if (ModListNames == null || ConfigurableModList == null || refresh)
            {
                ConfigurableModList = ConfigurableModList ?? new List<IConfigurableMod>();
                ModListNames = new string[ConfigurableModList.Count];
                int modIDIdx = 0;
                foreach (var configurableMod in ConfigurableModList)
                {
                    ModListNames[modIDIdx] = configurableMod.ID;
                    modIDIdx++;
                }
            }
            return ModListNames;
        }

        public IConfigurableMod GetSelectedMod(string modID)
        {
            SelectedMod = ConfigurableModList.Find(cfgMod => cfgMod.ID == modID);
            return SelectedMod;
        }

        public bool HasConflicts()
        {
            ModConflictList.Clear();

            var allbindings = new Dictionary<(string, string), string>();
            if (ConfigurableModList != null)
            {            
                foreach (var mod in ConfigurableModList)
                {
                    for (int i = 0; i < mod.ConfigurableModButtons.Count; i++)
                    {
                        var btn = mod.ConfigurableModButtons[i];
                        if (allbindings.Values.Contains(btn.KeyBinding))
                        {
                            allbindings.Add((mod.ID, btn.ID), btn.KeyBinding);
                            ModConflictList.Add((mod.ID, btn.ID), btn.KeyBinding);                           
                        }
                    }                   
                }
            }
            return ModConflictList.Count > 0;
        } 

    }
}
