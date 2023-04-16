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
        private static KeyCode ShortcutKey { get; set; } = KeyCode.Alpha0;

        private static readonly string ModName = nameof(ModManager);
        private static readonly float ModScreenTotalWidth = 500f;
        private static readonly float ModScreenTotalHeight = 150f;
        private static readonly float ModScreenMinWidth = 450f;
        private static readonly float ModScreenMaxWidth = 550f;
        private static readonly float ModScreenMinHeight = 50f;
        private static readonly float ModScreenMaxHeight = 200f;
        private static float ModScreenStartPositionX { get; set; } = Screen.width / 2f;
        private static float ModScreenStartPositionY { get; set; } = Screen.height / 2f;
        private static float ModMpScreenStartPositionX { get; set; } = Screen.width / 2.5f;
        private static float ModMpScreenStartPositionY { get; set; } = Screen.height / 2.5f;

        private static bool IsMinimized { get; set; } = false;

        private static CursorManager LocalCursorManager;
        private static HUDManager LocalHUDManager;
        private static Player LocalPlayer;
        private static SteamManager LocalSteamManager;
        private static MainMenuManager LocalMainMenuManager;

        private Color DefaultGuiColor = GUI.color;
        private GUIStyle SelectButtonStyle
        {
            get
            {
                var style = new GUIStyle(GUI.skin.button);
                style.onActive.textColor = Color.cyan;
                return style;
            }
        }

        private bool ShowUI = false;
        private bool ShowGameInfo = false;
        private bool ShowMpInfo = false;
        private bool ShowMpMngr = false;
        private bool ShowModInfo = false;

        public static Rect ModManagerScreen = new Rect(ModScreenStartPositionX, ModScreenStartPositionY, ModScreenTotalWidth, ModScreenTotalHeight);
        public static Rect ModMpMngrScreen = new Rect(ModManagerScreen.width / 2f, ModManagerScreen.height / 2f, 500f, 150f);

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
            onOptionToggled += ModManager_onOptionToggled;
            onPermissionValueChanged += ModManager_onPermissionValueChanged;
            GameModeAtStart = GreenHellGame.Instance.m_GHGameMode;
            ModList = GetModList();
            ShortcutKey = GetShortcutKey(nameof(ShortcutKey));                      
        }

        private KeyCode GetShortcutKey(string buttonID)
        {
            return ModList.Find(cfgMod => cfgMod.ID == ModName).ConfigurableModButtons.Find(cfgButton => cfgButton.ID == buttonID).KeyCode;
        }

        private List<ConfigurableMod> GetModList()
        {           
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

                                var configurableMod = new ConfigurableMod(gameID, modID, uniqueID, version);

                                configFileReader.ReadToDescendant("Button");
                                do
                                {
                                    string buttonID = configFileReader.GetAttribute("ID");
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
                HandleException(exc, "GetModList");
                modList = new List<ConfigurableMod>();
                return modList;
            }
        }

        private void ModManager_onPermissionValueChanged(bool optionValue)
        {
            GreenHellGame.DEBUG = optionValue;
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
            if (Input.GetKeyDown(ShortcutKey))
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
                    ShowMpMngr = !ShowMpMngr;
                    return;
                case 2:
                    ShowGameInfo = !ShowGameInfo;
                    return; 
                case 3:
                    ShowMpInfo = !ShowMpInfo;
                    return;
                case 4:
                    ShowModInfo = !ShowModInfo;
                    return;
                default:
                    ShowUI = !ShowUI;
                    ShowMpMngr = !ShowMpMngr;
                    ShowGameInfo = !ShowGameInfo;
                    ShowMpInfo = !ShowMpInfo;
                    ShowModInfo = !ShowModInfo;
                    return;
            }          
        }

        private void OnGUI()
        {
            if (ShowUI || ShowMpMngr)
            {
                InitData();
                InitSkinUI();
                InitWindow();
            }
        }

        private void InitWindow()
        {
            if (ShowUI || ShowMpMngr)
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

                if (ShowMpMngr)
                {
                    ModMpMngrScreen = GUILayout.Window(
                                                                                                        GetHashCode(),
                                                                                                        ModMpMngrScreen,
                                                                                                        InitMpMngrWindow,
                                                                                                        $"{ModName} Multiplayer",
                                                                                                        GUI.skin.window,
                                                                                                        GUILayout.ExpandWidth(true),
                                                                                                        GUILayout.MinWidth(ModScreenMinWidth),
                                                                                                        GUILayout.MaxWidth(ModScreenMaxWidth),
                                                                                                        GUILayout.ExpandHeight(true),
                                                                                                        GUILayout.MinHeight(ModScreenMinHeight),
                                                                                                        GUILayout.MaxHeight(ModScreenMaxHeight));
                }
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
            ShowMpMngr = false;
            ShowMpInfo = false;
            ShowGameInfo = false;
            ShowModInfo = false;
            EnableCursor(false);
        }

        private void InitMpMngrWindow(int windowID)
        {
            ModMpScreenStartPositionX = ModMpMngrScreen.x;
            ModMpScreenStartPositionY = ModMpMngrScreen.y;

            using (var modplayersScope = new GUILayout.VerticalScope(GUI.skin.box))
            {
                MpPlayersBox();
            }
            GUI.DragWindow(new Rect(0f, 0f, 10000f, 10000f));
        }

        private void MpPlayersBox()
        {
            GUILayout.Label($"Multiplayer", GUI.skin.label);
            PlayersScrollViewBox();
            using (var actionScope = new GUILayout.HorizontalScope(GUI.skin.box))
            {
                if (GUILayout.Button("Kick", GUI.skin.button))
                {
                    if (SelectedPlayerName != LocalHostDisplayName)
                    {
                        OnClickKickPlayerButton();
                    }
                    else
                    {
                        ShowHUDBigInfo($"Impossible to kick yourself, {LocalHostDisplayName}!");
                    }                 
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
                }
                if (GUILayout.Button("Close", GUI.skin.button))
                {
                    ToggleShowUI(1);
                }
            }
        }

        private void PlayersScrollViewBox()
        {
            LocalHostDisplayName = GetHostPlayerName();
          
            GUI.color = DefaultGuiColor;
            GUILayout.Label($"Type in your message to send to {SelectedPlayerName}:", GUI.skin.label);
            TextChatMessage = GUILayout.TextArea(TextChatMessage, GUI.skin.textArea);

            using (var pScrollViewScope = new GUILayout.VerticalScope(GUI.skin.box))
            {
                PlayerListScrollView();                
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
            PlayerListScrollViewPosition = GUILayout.BeginScrollView(PlayerListScrollViewPosition, GUI.skin.scrollView, GUILayout.MinHeight(100f));
            int _SelectedPlayerIndex = SelectedPlayerIndex;
            string[] playerNames = GetPlayerNames();
            if (playerNames != null)
            {             
                SelectedPlayerIndex = GUILayout.SelectionGrid(SelectedPlayerIndex, playerNames, 3, SelectButtonStyle);
                if (_SelectedPlayerIndex != SelectedPlayerIndex)
                {
                    SelectedPlayerName = playerNames[SelectedPlayerIndex];
                }
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
            GUILayout.Label($"Mods currently available from ModAPI as found in runtime configuration file:", GUI.skin.label);
            using (var managemodlistScope = new GUILayout.VerticalScope(GUI.skin.box))
            {
                ModListScrollView();
                if (GUILayout.Button("Mod Info", GUI.skin.button))
                {
                    ToggleShowUI(4);
                }
                if (ShowModInfo)
                {
                  ModInfoBox();
                }               
            }
        }
        
        private void ModInfoBox()
        {
            GUILayout.Label("Mod Info", GUI.skin.label);
            using (var modinfoScope = new GUILayout.VerticalScope(GUI.skin.box))
            {                
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
        }

        private void ModListScrollView()
        {
            GUILayout.Label($"Select mod:", GUI.skin.label);
            ModListScrollViewPosition = GUILayout.BeginScrollView(ModListScrollViewPosition, GUI.skin.scrollView, GUILayout.MinHeight(100f));
            int _SelectedModIDIndex = SelectedModIDIndex;
            string[] modlistNames = GetModListNames();
            if (modlistNames != null)
            {             
                SelectedModIDIndex = GUILayout.SelectionGrid(SelectedModIDIndex, modlistNames, 3, SelectButtonStyle);
                if (_SelectedModIDIndex != SelectedModIDIndex)
                {
                    SelectedModID = modlistNames[SelectedModIDIndex];
                    SelectedMod = ModList.Find(cfgMod => cfgMod.ID == SelectedModID);
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
                configurableMod.AddConfigurableModButton(nameof(ShortcutKey), ShortcutKey.ToString());
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
            GUILayout.Label("Client Manager: ", GUI.skin.label);
            using (var clientmngScope = new GUILayout.VerticalScope(GUI.skin.box))
            {
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
            GUILayout.Label("Host Manager: ", GUI.skin.label);
            using (var hostmngScope = new GUILayout.VerticalScope(GUI.skin.box))
            {
                ModOptionsBox();
                MpMngrBox();
            }
        }

        private void MpMngrBox()
        {
            using (var playerlistScope = new GUILayout.HorizontalScope(GUI.skin.box))
            {
                if (GUILayout.Button("Multiplayer Manager",SelectButtonStyle))
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
            GUI.color = DefaultGuiColor;

            GUILayout.Label($"To toggle the {ModName} main UI, press [{ShortcutKey}]", GUI.skin.label);
            using (var optionsScope = new GUILayout.VerticalScope(GUI.skin.box))
            {
                if (GUILayout.Button("Game Info", SelectButtonStyle))
                {
                    ToggleShowUI(2);
                }
                if (ShowGameInfo)
                {
                    GUILayout.Label("Game Info", GUI.skin.label);
                    GameInfoBox();
                }
                if (GUILayout.Button("Multiplayer Info", SelectButtonStyle))
                {
                    ToggleShowUI(3);
                }
                if (ShowMpInfo)
                {
                    GUILayout.Label("Multiplayer Info", GUI.skin.label);
                    MultiplayerInfoBox();
                }
                AllowModsAndCheatsOption();
                RequestInfoShownOption();
                SwitchPlayerVersusModeOption();
            }
        }

        private void GameInfoBox()
        {
            using (var gameinfoScope = new GUILayout.VerticalScope(GUI.skin.box))
            {
                bool IsAnyCheatEnabled = Cheats.m_GhostMode || Cheats.m_OneShotConstructions || Cheats.m_InstantBuild || Cheats.m_GodMode;
                GUILayout.Label($"{nameof(Cheats)}: {(IsAnyCheatEnabled ? "enabled" : "disabled")}", GUI.skin.label);
                GUILayout.Label($"{nameof(GreenHellGame)}.{nameof(GreenHellGame.DEBUG)} Mode: {(GreenHellGame.DEBUG ? "enabled" : "disabled")}", GUI.skin.label);
                GUILayout.Label($"{nameof(GameModeAtStart)}: {GameModeAtStart}", GUI.skin.label);
                GUILayout.Label($"{nameof(GreenHellGame)}.{nameof(GreenHellGame.Instance)}.{nameof(GreenHellGame.Instance.m_GHGameMode)}: {GreenHellGame.Instance.m_GHGameMode}", GUI.skin.label);
                GUILayout.Label($"{nameof(MainLevel)}.{nameof(MainLevel.Instance)}.{nameof(MainLevel.Instance.m_GameMode)}: {MainLevel.Instance.m_GameMode}", GUI.skin.label);
                GUILayout.Label($"{nameof(IsModActiveForSingleplayer)}: {(IsModActiveForSingleplayer ? "enabled" : "disabled")}", GUI.skin.label);
                GUILayout.Label($"{nameof(IsModActiveForMultiplayer)}: {(IsModActiveForMultiplayer ? "enabled" : "disabled")}", GUI.skin.label);
                GUILayout.Label($"{nameof(PlayerCount)}: {PlayerCount}", GUI.skin.label);
            }
        }

        private void MultiplayerInfoBox()
        {
            LocalHostDisplayName = GetHostPlayerName();           
            using (var multiplayerinfoScope = new GUILayout.VerticalScope(GUI.skin.box))
            {       
                GUILayout.Label($"{nameof(LocalHostDisplayName)}:  {LocalHostDisplayName}", GUI.skin.label);
                GUILayout.Label($"{nameof(IsHostManager)}: { (IsHostManager ? "enabled" : "disabled"  )}", GUI.skin.label);
                GUILayout.Label($"{nameof(IsHostWithPlayersInCoop)}: {( IsHostWithPlayersInCoop ? "enabled" : "disabled")}", GUI.skin.label);             
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