/*
 * This code was inspired by Moritz. Thank you!
 * */
using Enums;
using ModManager.Enums;
using RootMotion.FinalIK;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using UnityEngine;
using UnityEngine.UI;

namespace ModManager
{
    /// <summary>
    /// ModManager is a mod for Green Hell, which aims to be a tool for players
    /// who would like to be able to use ModAPI mods in multiplayer when not being host.
    /// Press Alpha0 (default) or the key configurable in ModAPI to open the main mod screen.
    /// </summary>
    public class ModManager : MonoBehaviour
    {
        private static ModManager Instance;
        private static readonly string RuntimeConfigurationFile = Path.Combine(Application.dataPath.Replace("GH_Data", "Mods"), "RuntimeConfiguration.xml");
        private static KeyCode ModShortcutKey { get; set; } = KeyCode.Alpha0;

        private static readonly string ModName = nameof(ModManager);
        private static readonly float ModScreenTotalWidth = 500f;
        private static readonly float ModScreenTotalHeight = 150f;
        private static readonly float ModScreenMinWidth = 450f;
        private static readonly float ModScreenMaxWidth = 550f;
        private static readonly float ModScreenMinHeight = 50f;
        private static readonly float ModScreenMaxHeight = 200f;
        private static float ModScreenStartPositionX { get; set; } = Screen.width / 2f;
        private static float ModScreenStartPositionY { get; set; } = Screen.height / 2f;
        private static bool IsMinimized { get; set; } = false;

        private static CursorManager LocalCursorManager;
        private static HUDManager LocalHUDManager;
        private static Player LocalPlayer;
        private static SteamManager LocalSteamManager;
        private static MainMenuManager LocalMainMenuManager;

        private Color DefaultGuiColor = GUI.color;
        private bool ShowUI = false;
        private bool ShowPlayerList = false;

        public static Rect ModManagerScreen = new Rect(ModScreenStartPositionX, ModScreenStartPositionY, ModScreenTotalWidth, ModScreenTotalHeight);
        public static Rect ModManagePlayersScreen = new Rect(Screen.width / 2f, Screen.height / 2f, 450f, 100f);

        public static List<ConfigurableMod> ModList { get; set; } = new List<ConfigurableMod>();

        public static string ChatRequestId { get; set; } = string.Empty;
        public static string LocalHostDisplayName { get; set; } = string.Empty;
        public static GameMode GameModeAtStart { get; set; } = GameMode.None;
        public static string SteamAppId  => GreenHellGame.s_SteamAppId.m_AppId.ToString();
        public static int PlayerCount => P2PSession.Instance.GetRemotePeerCount();

        public delegate void OnPermissionValueChanged(bool optionValue);
        public static event OnPermissionValueChanged onPermissionValueChanged;
        public delegate void OnOptionToggled(bool optionValue, string optionText);
        public static event OnOptionToggled onOptionToggled;

        public static string TextChatMessage { get; set; } = string.Empty;

        public static Vector2 PlayerListScrollViewPosition { get; set; }
        public static string SelectedPlayerName { get; set; } = string.Empty;
        public static int SelectedPlayerIndex { get; set; } = 0;

        public static Vector2 ModListScrollViewPosition { get; set; }
        public static int SelectedModIDIndex { get; set; }
        public static string SelectedModID { get; set; }
        public static ConfigurableMod SelectedMod { get; set; }
        public static bool SwitchPlayerVersusMode { get; set; } = false;
        public static bool RequestInfoShown { get; set; } = false;
        public static int RequestsSendToHost { get; set; } = 0;
     
        public static bool AllowModsAndCheatsForMultiplayer { get; set; } = false;
        public bool IsModActiveForMultiplayer { get; private set; }
        public bool IsModActiveForSingleplayer => ReplTools.AmIMaster();

        public static bool IsHostManager
            => ReplTools.AmIMaster();
        public static bool IsHostInCoop
            => IsHostManager && ReplTools.IsCoopEnabled();
        public static bool IsHostWithPlayersInCoop
            => IsHostInCoop && !ReplTools.IsPlayingAlone();
        public static bool Disable { get; set; } = false;

        public static string GetClientCommandToUseMods()
            => "!requestMods";

        public static string GetHostCommandToAllowMods(string chatRequestId)
            => $"!allowMods{chatRequestId}";

        public static string GetClientPlayerName()
            => ReplTools.GetLocalPeer().GetDisplayName();

        public static string GetHostPlayerName()
            => P2PSession.Instance.GetSessionMaster().GetDisplayName();

        public static void SetNewChatRequestId()
        {
            ChatRequestId = P2PSession.Instance.GetSessionId();
        }

        public static string HostCommandToAllowModsWithRequestId()
            => GetHostCommandToAllowMods(ChatRequestId);

        public static string ClientSystemInfoChatMessage(string command, Color? color = null)
            => SystemInfoChatMessage(
                $"Send <b><color=#{(color.HasValue ? ColorUtility.ToHtmlStringRGBA(color.Value) : ColorUtility.ToHtmlStringRGBA(Color.cyan))}>{command}</color></b> to request permission to use mods.",
                color);

        public static string HostSystemInfoChatMessage(string command, Color? color = null, Color? subColor = null)
            => SystemInfoChatMessage(
                $"Hello <b><color=#{(color.HasValue ? ColorUtility.ToHtmlStringRGBA(color.Value) : ColorUtility.ToHtmlStringRGBA(Color.blue))}>{GetHostPlayerName()}</color></b>"
            + $"\nto enable the use of mods for {GetClientPlayerName()}, send <b><color=#{(subColor.HasValue ? ColorUtility.ToHtmlStringRGBA(subColor.Value) : ColorUtility.ToHtmlStringRGBA(Color.cyan))}>{command}</color></b>"
            + $"\n<color=#{(color.HasValue ? ColorUtility.ToHtmlStringRGBA(color.Value) : ColorUtility.ToHtmlStringRGBA(Color.yellow))}>Be aware that this can be used for griefing!</color>",
                color);

        public static string RequestWasSentMessage(Color? color = null)
            => SystemInfoChatMessage(
                $"<color=#{(color.HasValue ? ColorUtility.ToHtmlStringRGBA(color.Value) : ColorUtility.ToHtmlStringRGBA(Color.green))}>Request sent to host!</color>",
                color);

        public static string PermissionWasGrantedMessage()
            => $"Permission granted!";

        public static string PermissionWasRevokedMessage()
            => $"Permission  revoked!";

        public static string FlagStateChangedMessage(bool flagState, string content, Color? color = null)
            => SystemInfoChatMessage(
                $"<color=#{(color.HasValue ? ColorUtility.ToHtmlStringRGBA(color.Value) : ColorUtility.ToHtmlStringRGBA(Color.yellow))}>{content} {(flagState ? "enabled" : "disabled")}!</color>",
                color);

        public static string OnlyHostCanAllowMessage(Color? color = null)
            => SystemInfoChatMessage(
                $"<color=#{(color.HasValue ? ColorUtility.ToHtmlStringRGBA(color.Value) : ColorUtility.ToHtmlStringRGBA(Color.yellow))}>Only the host {GetHostPlayerName()} can grant permission!</color>",
                color);

        public static string PlayerWasKickedMessage(string playerNameToKick, Color? color = null)
            => SystemInfoChatMessage($"<color=#{(color.HasValue ? ColorUtility.ToHtmlStringRGBA(color.Value) : ColorUtility.ToHtmlStringRGBA(Color.yellow))}>{playerNameToKick} got kicked from the game!</color>");

        public static string MaximumRequestsSendMessage(int maxRequest, Color? color = null)
            => SystemInfoChatMessage(
                $"<color=#{(color.HasValue ? ColorUtility.ToHtmlStringRGBA(color.Value) : ColorUtility.ToHtmlStringRGBA(Color.yellow))}>Max. {maxRequest} requests can be send!</color>",
                color);

        public static string SystemInfoServerRestartMessage(Color? color = null)
            => SystemInfoChatMessage(
                $"<color=#{(color.HasValue ? ColorUtility.ToHtmlStringRGBA(color.Value) : ColorUtility.ToHtmlStringRGBA(Color.yellow))}><b>Attention all players!</b></color>"
             + $" \nGame host {GetHostPlayerName()} is restarting the server. \nYou will be automatically rejoining in a short while. Please hold.",
                color);

        public static string SystemInfoChatMessage(string content, Color? color = null)
            => $"<color=#{(color.HasValue ? ColorUtility.ToHtmlStringRGBA(color.Value) : ColorUtility.ToHtmlStringRGBA(Color.red))}>System</color>:\n{content}";

        public static string PermissionChangedMessage(string permission)
            => $"Permission to use mods and cheats in multiplayer was {permission}";

        public static string HUDBigInfoMessage(string message, MessageType messageType, Color? headcolor = null)
            => $"<color=#{(headcolor != null ? ColorUtility.ToHtmlStringRGBA(headcolor.Value) : ColorUtility.ToHtmlStringRGBA(Color.red))}>{messageType}</color>\n{message}";

        public ModManager()
        {
            useGUILayout = true;
            Instance = this;
        }

        public static ModManager Get()
        {
            return Instance;
        }

        public static void ShowHUDBigInfo(string text)
        {
            string header = $"{ModName} Info";
            string textureName = HUDInfoLogTextureType.Count.ToString();

            HUDBigInfo bigInfo = (HUDBigInfo)LocalHUDManager.GetHUD(typeof(HUDBigInfo));
            HUDBigInfoData.s_Duration = 3f;
            HUDBigInfoData bigInfoData = new HUDBigInfoData
            {
                m_Header = header,
                m_Text = text,
                m_TextureName = textureName,
                m_ShowTime = Time.time
            };
            bigInfo.AddInfo(bigInfoData);
            bigInfo.Show(true);
        }

        public void ShowHUDInfoLog(string ItemInfo, string localizedTextKey)
        {
            Localization localization = GreenHellGame.Instance.GetLocalization();
            ((HUDMessages)LocalHUDManager.GetHUD(typeof(HUDMessages))).AddMessage(localization.Get(localizedTextKey) + "  " + localization.Get(ItemInfo));
        }

        private void EnableCursor(bool blockPlayer = false)
        {
            LocalCursorManager.ShowCursor(blockPlayer);

            if (blockPlayer)
            {
                LocalPlayer.BlockMoves();
                LocalPlayer.BlockRotation();
                LocalPlayer.BlockInspection();
            }
            else
            {
                LocalPlayer.UnblockMoves();
                LocalPlayer.UnblockRotation();
                LocalPlayer.UnblockInspection();
            }
        }

        private void Start()
        {
            GameModeAtStart = GreenHellGame.Instance.m_GHGameMode;          
            onOptionToggled += ModManager_onOptionToggled;
            onPermissionValueChanged += ModManager_onPermissionValueChanged;

            ModList = GetModList();
            ModShortcutKey = ModList.Find(cfgMod => cfgMod.ID == ModName).ConfigurableModButtons.Find(cfgButton => cfgButton.ID == nameof(ModShortcutKey)).KeyCode;            
            ModAPI.Log.Write($"{nameof(ModShortcutKey)} = {ModShortcutKey}");
        }

        private List<ConfigurableMod> GetModList()
        {
            ModAPI.Log.Write($"{ModName}:{nameof(GetModList)}");
            List<ConfigurableMod> modList = new List<ConfigurableMod>();
            try
            {
                if (File.Exists(RuntimeConfigurationFile))
                {
                    using (XmlReader configFileReader = XmlReader.Create(new StreamReader(RuntimeConfigurationFile)))
                    {
                        while (configFileReader.Read())
                        {
                            configFileReader.ReadToFollowing("Mod");
                            do
                            {
                                string gameID = "GH";
                                string modID = configFileReader.GetAttribute("ID");
                                string uniqueID = configFileReader.GetAttribute("UniqueID");
                                string version = configFileReader.GetAttribute("Version");

                                ModAPI.Log.Write($"{nameof(ConfigurableMod.GameID)} = {gameID}");
                                ModAPI.Log.Write($"{nameof(ConfigurableMod.ID)} = {modID}");
                                ModAPI.Log.Write($"{nameof(ConfigurableMod.UniqueID)} = {uniqueID}");
                                ModAPI.Log.Write($"{nameof(ConfigurableMod.Version)} = {version}");

                                var configurableMod = new ConfigurableMod(gameID, modID, uniqueID, version);

                                configFileReader.ReadToDescendant("Button");
                                do
                                {
                                    string buttonID = configFileReader.GetAttribute("ID");
                                    string buttonKeyBinding = configFileReader.ReadElementContentAsString();

                                    ModAPI.Log.Write($"{nameof(buttonID)} = {buttonID}");
                                    ModAPI.Log.Write($"{nameof(buttonKeyBinding)} = {buttonKeyBinding}");

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
                HandleException(exc, "GetModList");
                modList = new List<ConfigurableMod>();
                return modList;
            }
        }

        private void ModManager_onPermissionValueChanged(bool optionValue)
        {
            // GreenHellGame.DEBUG = optionValue;
            IsModActiveForMultiplayer = optionValue;
            if (optionValue)
            {
                GreenHellGame.Instance.m_GHGameMode = GameMode.Debug;
                MainLevel.Instance.m_GameMode = GameMode.Debug;
            }
            else
            {
                GreenHellGame.Instance.m_GHGameMode = GameModeAtStart;
                MainLevel.Instance.m_GameMode = GameModeAtStart;
            }
        }

        private void ModManager_onOptionToggled(bool optionValue, string optionText)
        {
            ShowHUDBigInfo(
                FlagStateChangedMessage(optionValue, optionText));

            if (IsHostWithPlayersInCoop && PlayerCount > 0)
            {
                P2PSession.Instance.SendTextChatMessage(FlagStateChangedMessage(optionValue, optionText));
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(ModShortcutKey))
            {
                if (!ShowUI)
                {
                    InitData();
                    EnableCursor(blockPlayer: true);
                }
                ToggleShowUI(0);
                if (!ShowUI)
                {
                    EnableCursor(blockPlayer: false);
                }
            }
        }

        private void ToggleShowUI(int level)
        {
            switch (level)
            {
                case 0:
                    ShowUI = !ShowUI;
                    return;
                case 1:
                    ShowPlayerList = !ShowPlayerList;
                    return;              
            }
            ShowUI = !ShowUI;
            ShowPlayerList = !ShowPlayerList;
        }

        private void OnGUI()
        {
            if (ShowUI || ShowPlayerList)
            {
                InitData();
                InitSkinUI();
                InitWindow();
            }
        }

        private void InitWindow()
        {
            if (ShowUI)
            {
                ModManagerScreen = GUILayout.Window(
                                               GetHashCode(),
                                                ModManagerScreen,
                                                InitModManagerScreen,
                                                $"{ModName} by [Dragon Legion] Immaanuel#4300",
                                                GUI.skin.window,
                                                GUILayout.ExpandWidth(true),
                                                GUILayout.MinWidth(ModScreenMinWidth),
                                                GUILayout.MaxWidth(ModScreenMaxWidth),
                                                GUILayout.ExpandHeight(true),
                                                GUILayout.MinHeight(ModScreenMinHeight),
                                                GUILayout.MaxHeight(ModScreenMaxHeight));
            }

            if (ShowPlayerList)
            {
                ModManagePlayersScreen = GUILayout.Window(
                                                                                                    GetHashCode(),       
                                                                                                    ModManagePlayersScreen,
                                                                                                    InitModManagePlayersWindow,
                                                                                                    $"{ModName} - Manage players",
                                                                                                    GUI.skin.window,
                                                                                                    GUILayout.ExpandWidth(true),
                                                                                                    GUILayout.MinWidth(ModScreenMinWidth),
                                                                                                    GUILayout.MaxWidth(ModScreenMaxWidth),
                                                                                                    GUILayout.ExpandHeight(true),
                                                                                                    GUILayout.MinHeight(ModScreenMinHeight),
                                                                                                    GUILayout.MaxHeight(ModScreenMaxHeight));
            }
        }

        private static void InitData()
        {
            LocalHUDManager = HUDManager.Get();
            LocalPlayer = Player.Get();
            LocalCursorManager = CursorManager.Get();         
            LocalMainMenuManager = MainMenuManager.Get();
        }

        private static void InitSkinUI()
        {
            GUI.skin = ModAPI.Interface.Skin;
        }

        private void CloseWindow()
        {
            ShowUI = false;
            ShowPlayerList = false;
            EnableCursor(false);
        }

        private void InitModManagePlayersWindow(int windowID)
        {       
            using (var modplayersScope = new GUILayout.VerticalScope(GUI.skin.box))
            {
                ManagePlayersScrollViewBox();
            }
            GUI.DragWindow(new Rect(0f, 0f, 10000f, 10000f));
        }

        private void ManagePlayersScrollViewBox()
        {
            LocalHostDisplayName = GetHostPlayerName();

            GUI.color = Color.cyan;
            GUILayout.Label($"Selected player: {SelectedPlayerName}", GUI.skin.label);
          
            GUI.color = DefaultGuiColor;
            GUILayout.Label($"Type in your message to send to {SelectedPlayerName}:", GUI.skin.label);
            TextChatMessage = GUILayout.TextArea(TextChatMessage, GUI.skin.textArea);
            using (var pScrollViewScope = new GUILayout.VerticalScope(GUI.skin.box))
            {
                PlayerListScrollView();
                using (var actionScope = new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    if (GUILayout.Button("Kick", GUI.skin.button))
                    {
                        if (SelectedPlayerName !=LocalHostDisplayName)
                        {
                            OnClickKickPlayerButton();                            
                        }
                        else
                        {
                            ShowHUDBigInfo($"Impossible to kick yourself, {LocalHostDisplayName}!");
                        }
                        ToggleShowUI(1);
                    }
                    if (GUILayout.Button("Send message", GUI.skin.button))
                    {
                        if (SelectedPlayerName != LocalHostDisplayName)
                        {
                            OnClickSendMessageButton();                           
                        }
                        else
                        {
                            ShowHUDBigInfo($"Impossible to send messages to yourself, {LocalHostDisplayName}!");
                        }
                        ToggleShowUI(1);
                    }
                }
            }
        }

        private void OnClickKickPlayerButton()
        {
            try
            {
                string[] playerNames = GetPlayerNames();
                if (playerNames != null && playerNames.Length > 0)
                {
                    SelectedPlayerName = playerNames[SelectedPlayerIndex];
                    if (!string.IsNullOrEmpty(SelectedPlayerName))
                    {
                        P2PPeer peerPlayerToKick = P2PSession.Instance.m_RemotePeers?.ToList().Find(peer => peer.GetDisplayName().ToLower() == SelectedPlayerName.ToLower());
                        if (peerPlayerToKick != null)
                        {
                            P2PLobbyMemberInfo playerToKickLobbyMemberInfo = P2PTransportLayer.Instance.GetCurrentLobbyMembers()?.ToList().Find(lm => lm.m_Address == peerPlayerToKick.m_Address);
                            if (playerToKickLobbyMemberInfo != null)
                            {
                                P2PTransportLayer.Instance.KickLobbyMember(playerToKickLobbyMemberInfo);
                                ShowHUDBigInfo(HUDBigInfoMessage(PlayerWasKickedMessage(SelectedPlayerName), MessageType.Info, Color.green));
                                P2PSession.Instance.SendTextChatMessage(PlayerWasKickedMessage(SelectedPlayerName));
                            }
                        }
                    }
                }
            }
            catch (Exception exc)
            {
                HandleException(exc, nameof(OnClickSendMessageButton));
            }
        }

        private void PlayerListScrollView()
        {
            GUILayout.Label($"Select player:", GUI.skin.label);
            PlayerListScrollViewPosition = GUILayout.BeginScrollView(PlayerListScrollViewPosition, GUI.skin.scrollView, GUILayout.MinHeight(150f));
            string[] playerNames = GetPlayerNames();
            if (playerNames != null)
            {             
                SelectedPlayerIndex = GUILayout.SelectionGrid(SelectedPlayerIndex, playerNames, 3, GUI.skin.button);
                SelectedPlayerName = playerNames[SelectedPlayerIndex];
            }
            GUILayout.EndScrollView();
        }

        private void InitModManagerScreen(int windowID)
        {
            ModScreenStartPositionX = ModManagerScreen.x;
            ModScreenStartPositionY = ModManagerScreen.y;

            using (var modContentScope = new GUILayout.VerticalScope(GUI.skin.box))
            {
                ScreenMenuBox();
                if (!IsMinimized)
                {
                    if (IsHostManager)
                    {
                        HostManagerBox();
                    }
                    else
                    {
                        ClientManagerBox();
                    }
                    ManageModListBox();                   
                }
            }
            GUI.DragWindow(new Rect(0f, 0f, 10000f, 10000f));
        }

        private void ManageModListBox()
        {
            GUI.color = DefaultGuiColor;
            using (var managemodlistScope = new GUILayout.VerticalScope(GUI.skin.box))
            {
                GUILayout.Label($"Mods currently available from ModAPI as found in runtime configuration file:", GUI.skin.label);
                ModListScrollView();               
                using (var actionScope = new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    if (SelectedMod != null)
                    {
                        ModInfoBox();
                    }
                    if (GUILayout.Button("Info...", GUI.skin.button))
                    {
                        OnClickModInfoButton();
                    }
                }               
            }
        }

        private void OnClickModInfoButton()
        {
            string[] modlistNames = GetModListNames();
            if (modlistNames != null)
            {
                SelectedModID = modlistNames[SelectedModIDIndex];
                SelectedMod = ModList.Find(cfgMod => cfgMod.ID == SelectedModID);              
            } 
        }

        private void ModInfoBox()
        {
            using (var modinfoScope = new GUILayout.VerticalScope(GUI.skin.box))
            {
                GUILayout.Label("Info for selected mod: ", GUI.skin.label);

                GUI.color = Color.cyan;
                GUILayout.Label($"{nameof(ConfigurableMod.GameID)}: {SelectedMod.GameID}", GUI.skin.label);
                GUILayout.Label($"{nameof(ConfigurableMod.ID)}: {SelectedMod.ID}", GUI.skin.label);
                GUILayout.Label($"{nameof(ConfigurableMod.UniqueID)}: {SelectedMod.UniqueID}", GUI.skin.label);
                GUILayout.Label($"{nameof(ConfigurableMod.Version)}: {SelectedMod.Version}", GUI.skin.label);

                foreach (var configurableModButton in SelectedMod.ConfigurableModButtons)
                {
                    GUILayout.Label($"Button {nameof(ConfigurableModButton.ID)}: {configurableModButton.ID}", GUI.skin.label);
                    GUILayout.Label($"{nameof(ConfigurableModButton.KeyBinding)}: {configurableModButton.KeyBinding}", GUI.skin.label);
                }               
            }
            GUI.color = DefaultGuiColor;
        }

        private void ModListScrollView()
        {
            ModListScrollViewPosition = GUILayout.BeginScrollView(ModListScrollViewPosition, GUI.skin.scrollView, GUILayout.MinHeight(150f));
            string[] modlistNames = GetModListNames();
            if (modlistNames != null)
            {
                int _SelectedModIDIndex = SelectedModIDIndex;
                SelectedModIDIndex = GUILayout.SelectionGrid(SelectedModIDIndex, modlistNames, 3, GUI.skin.button);
                SelectedModID = modlistNames[SelectedModIDIndex];
                if (_SelectedModIDIndex != SelectedModIDIndex)
                {
                    SelectedMod = null;
                }
            }
            GUILayout.EndScrollView();
        }

        public static string[] GetModListNames()
        {
            if (ModList == null )
            {
                ModList = new List<ConfigurableMod>();
                var configurableMod = new ConfigurableMod("GH", ModName, "", "");
                configurableMod.AddConfigurableModButton(nameof(ModShortcutKey), ModShortcutKey.ToString());
                if (!ModList.Contains(configurableMod))
                {
                    ModList.Add(configurableMod);
                }            
            }
            string[] modListNames = new string[ModList.Count];
            int modIDIdx = 0;
            foreach (var configurableMod in ModList)
            {
                modListNames[modIDIdx] = configurableMod.ID;
                modIDIdx++;
            }
            return modListNames;
        }

        public static string[] GetPlayerNames()
        {
            if (IsHostWithPlayersInCoop && PlayerCount > 0)
            {
                string[] playerNames = new string[PlayerCount];
                int playerIdx = 0;
                var players = P2PSession.Instance.m_RemotePeers?.ToList();
                foreach (var peer in players)
                {
                    playerNames[playerIdx] = peer.GetDisplayName();
                    playerIdx++;
                }
                return playerNames;
            }
            else
            {
                string[] playerNames = new string[1];
                playerNames[0] = LocalPlayer.GetName();
                return playerNames;
            }
        }

        private void ClientManagerBox()
        {
            using (var clientmngScope = new GUILayout.VerticalScope(GUI.skin.box))
            {
                GUILayout.Label("Client Manager: ", GUI.skin.label);
                ClientRequestBox();
            }
        }

        private void ClientRequestBox()
        {
            using (var clientrqtScope = new GUILayout.HorizontalScope(GUI.skin.box))
            {
                GUILayout.Label("Send chat request to host to allow mods (max. 3 to avoid spam.)", GUI.skin.label);
                if (GUILayout.Button(GetClientCommandToUseMods(), GUI.skin.button))
                {
                    OnClickRequestModsButton();
                }
            }
        }

        private void HostManagerBox()
        {
            using (var hostmngScope = new GUILayout.VerticalScope(GUI.skin.box))
            {
                GUILayout.Label("Host Manager: ", GUI.skin.label);
                ModOptionsBox();
                PlayerListBox();
                //HostServerBox();
            }
        }

        private void PlayerListBox()
        {
            using (var playerlistScope = new GUILayout.HorizontalScope(GUI.skin.box))
            {
                GUILayout.Label("Send messages and manage players", GUI.skin.label);
                if (GUILayout.Button("Manage players", GUI.skin.button))
                {
                    ToggleShowUI(1);
                }
            }
        }

        private void HostServerBox()
        {
            using (var hostsrvScope = new GUILayout.HorizontalScope(GUI.skin.box))
            {
                GUILayout.Label("Save and restart host session with current players.", GUI.skin.label);
                if (GUILayout.Button("Restart", GUI.skin.button))
                {
                    ShowHUDBigInfo(SystemInfoServerRestartMessage());
                    SaveGameOnSwitch();
                }
            }
        }

        private void KickPlayerBox()
        {
            try
            {
                using (var playerListScope = new GUILayout.VerticalScope(GUI.skin.box))
                {
                    string[] playerNames = GetPlayerNames();
                    if (playerNames != null)
                    {
                        GUILayout.Label("Select player grid: ", GUI.skin.label);
                        SelectedPlayerIndex = GUILayout.SelectionGrid(SelectedPlayerIndex, playerNames, 3, GUI.skin.button);
                        if (GUILayout.Button("Kick player", GUI.skin.button))
                        {
                            OnClickSendMessageButton();
                        }
                    }
                }
            }
            catch (Exception exc)
            {
                HandleException(exc, nameof(KickPlayerBox));
            }
        }

        private void ModOptionsBox()
        {
            using (var optionsScope = new GUILayout.VerticalScope(GUI.skin.box))
            {
                GUILayout.Label($"To toggle the {ModName} main UI, press [{ModShortcutKey}]", GUI.skin.label);

                GUILayout.Label("Current game session info: ", GUI.skin.label);
                GUI.color = Color.cyan;
                GUILayout.Label($"{nameof(GreenHellGame)}.{nameof(GreenHellGame.DEBUG)} is {(GreenHellGame.DEBUG ? "enabled" : "disabled")}", GUI.skin.label);
                //GUILayout.Label($"{nameof(GameMode)} at start: {GameModeAtStart}", GUI.skin.label);
                //GUILayout.Label($"{nameof(GreenHellGame)}.{nameof(GreenHellGame.Instance)}.{nameof(GreenHellGame.Instance.m_GHGameMode)}: {GreenHellGame.Instance.m_GHGameMode}", GUI.skin.label);                
                //GUILayout.Label($"{nameof(MainLevel)}.{nameof(MainLevel.Instance)}.{nameof(MainLevel.Instance.m_GameMode)}: {MainLevel.Instance.m_GameMode}", GUI.skin.label);
                GUILayout.Label($"Mods for singleplayer are {(IsModActiveForSingleplayer ? "enabled" : "disabled")}", GUI.skin.label);
                GUILayout.Label($"Mods for multiplayer are {(IsModActiveForMultiplayer ? "enabled" : "disabled")}", GUI.skin.label);
                GUILayout.Label($"Remote player count: {PlayerCount}", GUI.skin.label);
                GUI.color = DefaultGuiColor;
                MultiplayerInfoBox();
                AllowModsAndCheatsOption();
                RequestInfoShownOption();
                SwitchPlayerVersusModeOption();
            }
        }

        private void MultiplayerInfoBox()
        {
            LocalHostDisplayName = GetHostPlayerName();
            SetNewChatRequestId();
            using (var multiplayerinfoScope = new GUILayout.VerticalScope(GUI.skin.box))
            {
                GUILayout.Label("Current multiplayer info: ", GUI.skin.label);

                GUI.color = Color.cyan;                
                GUILayout.Label($"Host player name: {LocalHostDisplayName}", GUI.skin.label);
                GUILayout.Label($"Host is playing with {ModName} { (IsHostManager ? "activated" : "disabled"  )}", GUI.skin.label);
                GUILayout.Label($"Host is {(IsHostWithPlayersInCoop ? "playing in coop" : "not playing in coop")}", GUI.skin.label);             
                GUILayout.Label($"Client player name: {GetClientPlayerName()}", GUI.skin.label);
                GUILayout.Label($"Command to unlock mods: {HostCommandToAllowModsWithRequestId()}", GUI.skin.label);
            }
            GUI.color = DefaultGuiColor;
        }

        private void ScreenMenuBox()
        {
            if (GUI.Button(new Rect(ModManagerScreen.width - 40f, 0f, 20f, 20f), "-", GUI.skin.button))
            {
                CollapseWindow();
            }
            if (GUI.Button(new Rect(ModManagerScreen.width - 20f, 0f, 20f, 20f), "X", GUI.skin.button))
            {
                CloseWindow();
            }
        }

        private void CollapseWindow()
        {
            if (!IsMinimized)
            {
                ModManagerScreen = new Rect(ModScreenStartPositionX, ModScreenStartPositionY, ModScreenTotalWidth, ModScreenMinHeight);
                IsMinimized = true;
            }
            else
            {
                ModManagerScreen = new Rect(ModScreenStartPositionX, ModScreenStartPositionY, ModScreenTotalWidth, ModScreenTotalHeight);
                IsMinimized = false;
            }
            InitWindow();
        }

        private void AllowModsAndCheatsOption()
        {
            bool _allowModsAndCheatsForMultiplayerValue = AllowModsAndCheatsForMultiplayer;
            AllowModsAndCheatsForMultiplayer = GUILayout.Toggle(AllowModsAndCheatsForMultiplayer, "Allow mods and cheats for multiplayer?", GUI.skin.toggle);
            ToggleModOption(_allowModsAndCheatsForMultiplayerValue, nameof(AllowModsAndCheatsForMultiplayer));
        }

        private void RequestInfoShownOption()
        {
            bool _requestInfoShownValue = RequestInfoShown;
            RequestInfoShown = GUILayout.Toggle(RequestInfoShown, "Chat request info shown?", GUI.skin.toggle);
            ToggleModOption(_requestInfoShownValue, nameof(RequestInfoShown));
        }

        private void SwitchPlayerVersusModeOption()
        {
            bool _switchPlayerVersusModeValue = SwitchPlayerVersusMode;
            SwitchPlayerVersusMode = GUILayout.Toggle(SwitchPlayerVersusMode, "Switch to PvP?", GUI.skin.toggle);
            ToggleModOption(_switchPlayerVersusModeValue, nameof(SwitchPlayerVersusMode));
        }

        public static void ToggleModOption(bool optionState, string optionName)
        {
            if (optionName == nameof(AllowModsAndCheatsForMultiplayer) && optionState != AllowModsAndCheatsForMultiplayer)
            {
                onOptionToggled?.Invoke(AllowModsAndCheatsForMultiplayer, $"Using mods and cheats has been");
                onPermissionValueChanged?.Invoke(AllowModsAndCheatsForMultiplayer);
            }

            if (optionName == nameof(RequestInfoShown) && optionState != RequestInfoShown)
            {
                onOptionToggled?.Invoke(RequestInfoShown, $"Chat request info was shown on how mods and cheats can be");
                RequestsSendToHost = 0;
            }

            if (optionName == nameof(SwitchPlayerVersusMode) && optionState != SwitchPlayerVersusMode)
            {
                SaveGameOnSwitch();
                onOptionToggled?.Invoke(SwitchPlayerVersusMode, $"PvP mode has been");
                LoadMainMenu();
            }
        }

        private static void LoadMainMenu()
        {
            LocalMainMenuManager = MainMenuManager.Get();
            if (SwitchPlayerVersusMode)
            {
                MainMenuChooseMode.s_DisplayMode = MainMenuChooseMode.MainMenuChooseModeType.Multiplayer;
                LocalMainMenuManager.SetActiveScreen(typeof(MainMenuChooseMode));
                var choose = (MainMenuChooseMode)LocalMainMenuManager.GetScreen(typeof(MainMenuChooseMode));
                choose.Show();
            }
            else
            {
                MainMenuChooseMode.s_DisplayMode = MainMenuChooseMode.MainMenuChooseModeType.Singleplayer;
                LocalMainMenuManager.SetActiveScreen(typeof(MainMenuChooseMode));
                var choose = (MainMenuChooseMode)LocalMainMenuManager.GetScreen(typeof(MainMenuChooseMode));
                choose.Show();
            }
        }

        private void OnClickSendMessageButton()
        {
            try
            {
                LocalHostDisplayName = GetHostPlayerName();
                string[] playerNames = GetPlayerNames();
                if (playerNames != null && playerNames.Length > 0)
                {
                    SelectedPlayerName = playerNames[SelectedPlayerIndex];
                    if (!string.IsNullOrEmpty(SelectedPlayerName))
                    {
                        P2PPeer peerPlayerToChat = P2PSession.Instance.m_RemotePeers?.ToList().Find(peer => peer.GetDisplayName().ToLower() == SelectedPlayerName.ToLower());
                        if (peerPlayerToChat != null)
                        {
                            P2PLobbyMemberInfo playerToChatLobbyMemberInfo = P2PTransportLayer.Instance.GetCurrentLobbyMembers()?.ToList().Find(lm => lm.m_Address == peerPlayerToChat.m_Address);
                            if (playerToChatLobbyMemberInfo != null)
                            {
                                P2PSession.Instance.SendTextChatMessage($"From: {LocalHostDisplayName} \n To {SelectedPlayerName}: \n" + TextChatMessage);
                            }
                        }
                    }
                }
            }
            catch (Exception exc)
            {
                HandleException(exc, nameof(OnClickSendMessageButton));
            }
        }

        private static void SaveGameOnSwitch()
        {
            try
            {
                ShowHUDBigInfo(HUDBigInfoMessage($"Saving..", MessageType.Info, Color.green));
                if (IsHostWithPlayersInCoop && ReplTools.CanSaveInCoop())
                {
                    P2PSession.Instance.SendTextChatMessage(SystemInfoServerRestartMessage());
                    SaveGame.SaveCoop();                  
                }
                if (!IsHostWithPlayersInCoop)
                {
                    SaveGame.Save();                    
                }
            }
            catch (Exception exc)
            {
                HandleException(exc, nameof(SaveGameOnSwitch));
            }
        }

        private static void ReloadLobby()
        {
            try
            {
                ShowHUDBigInfo(HUDBigInfoMessage($"Reloading lobby...", MessageType.Info, Color.green));
                ReadOnlyCollection<P2PLobbyMemberInfo> hostedLobbyMemberInfo = P2PTransportLayer.Instance.GetCurrentLobbyMembers();
                if (hostedLobbyMemberInfo != null)
                {
                    P2PSession.Instance.UpdateDefaultRespawnPosition(LocalPlayer.GetWorldPosition());
                    P2PSession.Instance.Restart();
                }
            }
            catch (Exception exc)
            {
                HandleException(exc, nameof(ReloadLobby));
            }
        }

        private void OnClickRequestModsButton()
        {
            try
            {
                if (RequestsSendToHost >= 3)
                {
                    ShowHUDBigInfo(MaximumRequestsSendMessage(3));
                    return;
                }
                P2PSession.Instance.SendTextChatMessage(GetClientCommandToUseMods());
                ShowHUDBigInfo(RequestWasSentMessage());
                RequestsSendToHost++;
            }
            catch (Exception exc)
            {
                HandleException(exc, nameof(OnClickRequestModsButton));
            }
        }

        private static void HandleException(Exception exc, string methodName)
        {
            string info = $"[{ModName}:{methodName}] throws exception:\n{exc.Message}";
            ModAPI.Log.Write(info);
            Debug.Log(info);
        }
    }
}