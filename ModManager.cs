/*
 * This code was inspired by Moritz. Thank you!
 * */
using Enums;
using ModManager.Enums;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
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
        private static KeyCode ModKeybindingId { get; set; } = KeyCode.Alpha0;

        private static readonly string ModName = nameof(ModManager);
        private static readonly float ModScreenTotalWidth = 500f;
        private static readonly float ModScreenTotalHeight = 300f;
        private static readonly float ModScreenMinWidth = 450f;
        private static readonly float ModScreenMaxWidth = 550f;
        private static readonly float ModScreenMinHeight = 50f;
        private static readonly float ModScreenMaxHeight = 350f;

        public int ModManagerWindowID { get; private set; }
        private static float ModManagerWindowStartPositionX { get; set; } = 0f;
        private static float ModManagerWindowStartPositionY { get; set; } = 0f;
        private static bool IsModManagerWindowMinimized { get; set; } = false;

        public int ModManagePlayersWindowID { get; private set; }
        private static float ModManagePlayersWindowStartPositionX { get; set; } = 0f;
        private static float ModManagePlayersWindowStartPositionY { get; set; } = 0f;
        private static bool IsModPlayerListWindowMinimized { get; set; } = false;

        private static CursorManager LocalCursorManager;
        private static HUDManager LocalHUDManager;
        private static Player LocalPlayer;

        private Color DefaultGuiColor = GUI.color;
        private bool ShowUI = false;
        private bool ShowPlayerList = false;

        public static Rect ModManagerWindow = new Rect(ModManagerWindowStartPositionX, ModManagerWindowStartPositionY, ModScreenTotalWidth, ModScreenTotalHeight);
        public static Rect ModManagePlayersWindow = new Rect(ModManagePlayersWindowStartPositionX, ModManagePlayersWindowStartPositionY, ModScreenTotalWidth, ModScreenTotalHeight);

        public static GameMode GameModeAtStart;

        public static List<ConfigurableMod> ModList { get; set; } = new List<ConfigurableMod>();

        public static string SteamAppId
            => GreenHellGame.s_SteamAppId.m_AppId.ToString();

        public static string SelectedPlayerName { get; set; } = string.Empty;
        public static int SelectedPlayerIndex { get; set; } = 0;
        public static string TextChatMessage { get; set; } = string.Empty;

        public static int PlayerCount => P2PSession.Instance.m_RemotePeers.Count;

        public delegate void OnPermissionValueChanged(bool optionValue);
        public static event OnPermissionValueChanged onPermissionValueChanged;
        public delegate void OnOptionToggled(bool optionValue, string optionText);
        public static event OnOptionToggled onOptionToggled;

        public static string ChatRequestId { get; set; } = string.Empty;
        public static Vector2 PlayerListScrollViewPosition { get; set; }
        public static string LocalHostDisplayName { get; set; }

        public static Vector2 ModListScrollViewPosition { get; set; }
        public static int SelectedModIDIndex { get; set; }
        public static string SelectedModID { get; set; }

        public static bool SwitchPlayerVersusMode { get; set; } = false;
        public static bool RequestInfoShown { get; set; } = false;
        public static int RequestsSendToHost { get; set; } = 0;
        public static bool AllowModsForMultiplayer { get; set; } = false;
        public static bool AllowModsAndCheatsForMultiplayer { get; set; } = false;
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
            ChatRequestId = UnityEngine.Random.Range(1000, 9999).ToString();
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

        public void ShowHUDBigInfo(string text)
        {
            string header = $"{ModName} Info";
            string textureName = HUDInfoLogTextureType.Count.ToString();

            HUDBigInfo bigInfo = (HUDBigInfo)LocalHUDManager.GetHUD(typeof(HUDBigInfo));
            HUDBigInfoData.s_Duration = 6f;
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
            ModList = GetGameModList();
            ModKeybindingId = ModList.Find(cfgMod => cfgMod.ID == ModName).ConfigurableModButtons.Find(cfgButton => cfgButton.ID == nameof(ModKeybindingId)).KeyCode;
        }

        private List<ConfigurableMod> GetGameModList()
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
                            configFileReader.ReadToDescendant("Mod");
                            string modID = configFileReader.GetAttribute("ID");
                            string uniqueID = configFileReader.GetAttribute("UniqueID");
                            string version = configFileReader.GetAttribute("Version");

                            var configurableMod = new ConfigurableMod("GH", modID, uniqueID, version);

                            while (configFileReader.ReadToFollowing("Button"))
                            {                              
                                string buttonID = configFileReader.GetAttribute("ID");
                                string buttonKeybinding = configFileReader.ReadElementContentAsString();
                                configurableMod.AddConfigurableModButton(buttonID, buttonKeybinding);
                            }
                            if (!modList.Contains(configurableMod))
                            {
                                modList.Add(configurableMod);
                            }
                        }
                    }
                }                
                return modList;
            }
            catch (Exception exc)
            {
                HandleException(exc, "GetGameModList");
                modList = new List<ConfigurableMod>();
                return modList;
            }
        }

        private void ModManager_onPermissionValueChanged(bool optionValue)
        {
            GreenHellGame.DEBUG = optionValue;
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

            if (IsHostWithPlayersInCoop)
            {
                P2PSession.Instance.SendTextChatMessage(FlagStateChangedMessage(optionValue, optionText));
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(ModKeybindingId))
            {
                if (!ShowUI)
                {
                    InitData();
                    EnableCursor(blockPlayer: true);
                }
                ToggleShowUI(1);
                if (!ShowUI)
                {
                    EnableCursor(blockPlayer: false);
                }
            }
        }

        private void ToggleShowUI(int windowID)
        {
            if (windowID == 1 || windowID == ModManagerWindowID)
            {
                ShowUI = !ShowUI;
            }
            if (windowID == 2 || windowID== ModManagePlayersWindowID)
            {
                ShowPlayerList = !ShowPlayerList;
            }
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
                ModManagerWindow = GUILayout.Window(
                                               GetHashCode(),
                                                ModManagerWindow,
                                                InitModManagerScreen,
                                                ModName,
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
                ModManagePlayersWindow = GUILayout.Window(
                                                                                                    GetHashCode(),       
                                                                                                    ModManagePlayersWindow,
                                                                                                    InitModManagePlayersWindow,
                                                                                                    " Player List",
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
        }

        private static void InitSkinUI()
        {
            GUI.skin = ModAPI.Interface.Skin;
        }

        private void CloseWindow(int windowID)
        {
            if (windowID == ModManagerWindowID)
            {
                ShowUI = false;
            }
            if (windowID == ModManagePlayersWindowID)
            {
                ShowPlayerList = false;
            }
            EnableCursor(ShowUI && ShowPlayerList);
        }

        private void InitModManagePlayersWindow(int windowID)
        {
            ModManagePlayersWindowID = windowID;
            ModManagePlayersWindowStartPositionX = ModManagePlayersWindow.x;
            ModManagePlayersWindowStartPositionY = ModManagePlayersWindow.y;

            using (var modplayersScope = new GUILayout.VerticalScope(GUI.skin.box))
            {
                ScreenMenuBox(ModManagePlayersWindow, ModManagePlayersWindowID);
                if (!IsModPlayerListWindowMinimized)
                {
                    ManagePlayersScrollViewBox();
                }
            }
            GUI.DragWindow(new Rect(0f, 0f, 10000f, 10000f));
        }

        private void ManagePlayersScrollViewBox()
        { 
            GUI.color = Color.cyan;
            GUILayout.Label($"Selected player: {SelectedPlayerName}", GUI.skin.label);
            GUI.color = DefaultGuiColor;
            using (var pScrollViewScope = new GUILayout.VerticalScope(GUI.skin.box))
            {
                GUILayout.Label($"Type in your message to send to {SelectedPlayerName}:", GUI.skin.label);
                TextChatMessage = GUILayout.TextArea(TextChatMessage, GUI.skin.textArea);

                PlayerListScrollView();
                using (var actionScope = new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    if (GUILayout.Button("Kick", GUI.skin.button))
                    {
                        OnClickKickPlayerButton();
                    }
                    if (GUILayout.Button("Send message", GUI.skin.button))
                    {
                        OnClickSendMessageButton();
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
            GUILayout.Label($"Players:", GUI.skin.label);
            PlayerListScrollViewPosition = GUILayout.BeginScrollView(PlayerListScrollViewPosition, GUI.skin.scrollView, GUILayout.MinHeight(300f));
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
            ModManagerWindowID = windowID;
            ModManagerWindowStartPositionX = ModManagerWindow.x;
            ModManagerWindowStartPositionY = ModManagerWindow.y;

            using (var modContentScope = new GUILayout.VerticalScope(GUI.skin.box))
            {
                ScreenMenuBox(ModManagerWindow, ModManagerWindowID);
                if (!IsModManagerWindowMinimized)
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
                ModListScrollView();           
            }
        }

        private void ModListScrollView()
        {
            GUILayout.Label($"All loaded mods from ModAPI as found in runtime configuration file:", GUI.skin.label);
            ModListScrollViewPosition = GUILayout.BeginScrollView(ModListScrollViewPosition, GUI.skin.scrollView, GUILayout.MaxHeight(150f));            
            if (ModList != null)
            {
                string[] modlistNames = GetModListNames();
                SelectedModIDIndex = GUILayout.SelectionGrid(SelectedModIDIndex, modlistNames, 3, GUI.skin.button);
                SelectedModID = modlistNames[SelectedModIDIndex];
            }
            GUILayout.EndScrollView();
        }

        public static string[] GetModListNames()
        {
            if (ModList != null && ModList.Count > 0)
            {
                string[] modListNames = new string[ModList.Count];
                int modIDIdx = 0;
                foreach (var configuredMod in ModList)
                {
                    modListNames[modIDIdx] = configuredMod.ID;
                    modIDIdx++;
                }
                return modListNames;
            }
            else 
            {
                string[] modListNames = new string[1];
                modListNames[0] = ModName;
                return modListNames;
            } 
        }

        public static string[] GetPlayerNames()
        {
            if (ReplTools.IsCoopEnabled())
            {
                string[] playerNames = new string[(P2PSession.Instance.GetRemotePeerCount())];
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
                playerNames[0] = GetHostPlayerName();
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
                HostServerBox();
            }
        }

        private void PlayerListBox()
        {
            using (var playerlistScope = new GUILayout.HorizontalScope(GUI.skin.box))
            {
                GUILayout.Label("Send chatmessages and manage players", GUI.skin.label);
                if (GUILayout.Button("Manage players", GUI.skin.button))
                {
                    ShowPlayerList = true;
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
                    RestartHost();
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
                AllowModsAndCheatsOption();
                RequestInfoShownOption();
                SwitchPlayerVersusModeOption();
            }
        }

        private void ScreenMenuBox(Rect screen, int id)
        {
            if (GUI.Button(new Rect(screen.width - 40f, 0f, 20f, 20f), "-", GUI.skin.button))
            {
                CollapseWindow(id);
            }
            if (GUI.Button(new Rect(screen.width - 20f, 0f, 20f, 20f), "X", GUI.skin.button))
            {
                CloseWindow(id);
            }
        }

        private void CollapseWindow(int windowID)
        {
            if (windowID == ModManagerWindowID)
            {
                if (!IsModManagerWindowMinimized)
                {
                    ModManagerWindow = new Rect(ModManagerWindowStartPositionX, ModManagerWindowStartPositionY, ModScreenTotalWidth, ModScreenMinHeight);
                    IsModManagerWindowMinimized = true;
                }
                else
                {
                    ModManagerWindow = new Rect(ModManagerWindowStartPositionX, ModManagerWindowStartPositionY, ModScreenTotalWidth, ModScreenTotalHeight);
                    IsModManagerWindowMinimized = false;
                }
            }

            if (windowID == ModManagePlayersWindowID)
            {
                if (!IsModPlayerListWindowMinimized)
                {
                    ModManagePlayersWindow = new Rect(ModManagePlayersWindowStartPositionX, ModManagePlayersWindowStartPositionY, ModScreenTotalWidth, ModScreenMinHeight);
                    IsModPlayerListWindowMinimized = true;
                }
                else
                {
                    ModManagePlayersWindow = new Rect(ModManagePlayersWindowStartPositionX, ModManagePlayersWindowStartPositionY, ModScreenTotalWidth, ModScreenTotalHeight);
                    IsModPlayerListWindowMinimized = false;
                }
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
                onOptionToggled?.Invoke(SwitchPlayerVersusMode, $"PvP mode has been");
                if (SwitchPlayerVersusMode)
                {
                    GreenHellGame.Instance.m_OnlyTutorial = false;
                    GreenHellGame.Instance.m_Settings.m_GameVisibility = P2PGameVisibility.Friends;
                    MainMenuChooseMode.s_DisplayMode = MainMenuChooseMode.MainMenuChooseModeType.Multiplayer;
                    MainMenuManager.Get().SetActiveScreen(typeof(MainMenuJoinOrHost));
                }
                else
                {
                    GreenHellGame.Instance.m_OnlyTutorial = false;
                    GreenHellGame.Instance.m_Settings.m_GameVisibility = P2PGameVisibility.Singleplayer;
                    MainMenuChooseMode.s_DisplayMode = MainMenuChooseMode.MainMenuChooseModeType.Singleplayer;
                    MainMenuManager.Get().SetActiveScreen(typeof(MainMenuChooseMode));
                }
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

        private void RestartHost()
        {
            try
            {
                ShowHUDBigInfo(HUDBigInfoMessage($"Restarting host...", MessageType.Info, Color.green));
                if (IsHostWithPlayersInCoop && ReplTools.CanSaveInCoop())
                {
                    P2PSession.Instance.SendTextChatMessage(SystemInfoServerRestartMessage());
                    SaveGame.SaveCoop();
                    ReloadLobby();
                }
                if (!IsHostWithPlayersInCoop)
                {
                    SaveGame.Save();
                    MainLevel.Instance.Initialize();
                }
            }
            catch (Exception exc)
            {
                HandleException(exc, nameof(RestartHost));
            }
        }

        private void ReloadLobby()
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

        private void HandleException(Exception exc, string methodName)
        {
            string info = $"[{ModName}:{methodName}] throws exception:\n{exc.Message}";
            ModAPI.Log.Write(info);
            ShowHUDBigInfo(HUDBigInfoMessage(info, MessageType.Error, Color.red));
        }
    }
}