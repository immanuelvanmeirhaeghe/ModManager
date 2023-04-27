using Enums;
using ModManager.Data.Enums;
using ModManager.Data.Interfaces;
using ModManager.Data.Modding;
using ModManager.Managers;
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
    public class ModManager : MonoBehaviour, IYesNoDialogOwner
    {
        private const string MpManagerTitle = "Multiplayer Manager";
        private static ModManager Instance;
        private static readonly string RuntimeConfiguration = Path.Combine(Application.dataPath.Replace("GH_Data", "Mods"), $"{nameof(RuntimeConfiguration)}.xml");
      
        private static readonly string ModName = nameof(ModManager);

        private static float ModManagerScreenTotalWidth { get; set; } = 700f;
        private static float ModManagerScreenTotalHeight { get; set; } = 600f;
        private static float ModManagerScreenMinWidth { get; set; } = 700f;
        private static float ModManagerScreenMinHeight { get; set; } = 50f;
        private static float ModManagerScreenMaxWidth { get; set; } = Screen.width;
        private static float ModManagerScreenMaxHeight { get; set; } = Screen.height;
        private static float ModManagerScreenStartPositionX { get; set; } = Screen.width / 2f;
        private static float ModManagerScreenStartPositionY { get; set; } = Screen.height / 2f;
        private static bool IsModManagerScreenMinimized { get; set; } = false;
        private static int ModManagerScreenId { get; set; }

        private static float ModMpMngrScreenTotalWidth { get; set; } = 500f;
        private static float ModMpMngrScreenTotalHeight { get; set; } = 350f;
        private static float ModMpMngrScreenMinWidth { get; set; } = 500f;        
        private static float ModMpMngrScreenMinHeight { get; set; } = 50f;
        private static float ModMpMngrScreenMaxWidth { get; set; } = Screen.width;
        private static float ModMpMngrScreenMaxHeight { get; set; } = Screen.height;
        private static float ModMpMngrScreenStartPositionX { get; set; } = Screen.width / 2.5f;
        private static float ModMpMngrScreenStartPositionY { get; set; } = Screen.height / 2.5f;
        private static bool IsModMpMngrScreenMinimized { get; set; }
        private static int ModMpMngrScreenId { get; set; }

        private static CursorManager LocalCursorManager;
        private static HUDManager LocalHUDManager;
        private static Player LocalPlayer;
        private static StylingManager LocalStylingManager;

        private bool ShowModManagerScreen = false;
        private bool ShowGameInfo = false;
        private bool ShowMpInfo = false;
        private bool ShowModMpMngrScreen = false;
        private bool ShowModInfo = false;
        private bool ShowInfo = false;
        private bool ShowModList =false;

        public static Rect ModManagerScreen = new Rect(ModManagerScreenStartPositionX, ModManagerScreenStartPositionY, ModManagerScreenTotalWidth, ModManagerScreenTotalHeight);
        public static Rect ModMpMngrScreen = new Rect(ModMpMngrScreenStartPositionX, ModMpMngrScreenStartPositionY, ModMpMngrScreenTotalWidth, ModMpMngrScreenTotalHeight);
        public static KeyCode ShortcutKey { get; set; } = KeyCode.Alpha0;
        public static List<IConfigurableMod> ConfigurableModList { get; set; } = new List<IConfigurableMod>();
        public static List<P2PPeer> CoopPlayerList { get; set; } = default;       
        public static int ChatRequestId { get; set; }     
        public static GameMode GameModeAtStart { get; set; } = GameMode.None;      
        public static P2PGameVisibility GameVisibilityAtSessionStart { get; set; } = P2PGameVisibility.Private;
        public static P2PGameVisibility GameVisibilityAtStart { get; set; } = P2PGameVisibility.Private;

        public static EventID PermissionChanged { get; set; } = EventID.NoneEnabled;
        public delegate void OnPermissionValueChanged(bool optionValue);
        public static event OnPermissionValueChanged onPermissionValueChanged;
        public delegate void OnOptionToggled(bool optionValue, string optionText);
        public static event OnOptionToggled onOptionToggled;

        public string LocalHostDisplayName { get; set; } = string.Empty;
        public string TextChatMessage { get; set; } = string.Empty;
        public string SteamAppId => GreenHellGame.s_SteamAppId.m_AppId.ToString();
        public int PlayerCount => P2PSession.Instance.GetRemotePeerCount();
        public Vector2 PlayerListScrollViewPosition { get; set; } = default;
        public string SelectedPlayerName { get; set; } = string.Empty;
        public int SelectedPlayerIndex { get; set; } = 0;

        public Vector2 ModListScrollViewPosition { get; set; } = default;
        public Vector2 GameInfoScrollViewPosition { get; set; } = default;
        public Vector2 ModInfoScrollViewPosition { get; set; } = default;
        public Vector2 MpInfoScrollViewPosition { get; set; } = default;

        public int SelectedModIDIndex { get; set; } = 0;
        public string SelectedModID { get; set; } = string.Empty;
        public IConfigurableMod SelectedMod { get; set; } = default;
        public bool IsMultiplayerGameModeActive { get; set; } = false;

        public bool IsModActiveForMultiplayer { get;  private set; }
        public bool IsModActiveForSingleplayer => ReplTools.AmIMaster();

        public static bool IsHostManager
            => ReplTools.AmIMaster();
        public static bool IsHostInCoop
            => IsHostManager && ReplTools.IsCoopEnabled();
        public static bool IsHostWithPlayersInCoop
            => IsHostInCoop && !ReplTools.IsPlayingAlone();
        public static bool Disable { get; set; } = false;
        public static List<P2PLobbyMemberInfo> CoopLobbyMembers { get; set; }
        public static SessionJoinHelper SessionJoinHelperAtStart { get; set; }
        public static bool CanJoinSessionAtStart { get; set; }
        
        public static bool RequestInfoShown { get; set; } = false;
        public static int RequestsSendToHost { get; set; } = 0;
        public static bool EnableDebugMode { get; set; } = false;
        public static bool AllowModsAndCheatsForMultiplayer { get; set; } = false;

        public static string OnlyForSinglePlayerOrHostMessage() 
            => $"Only available for single player or when host. Host {GetHostPlayerName()} can activate using {ModName}.";

        public static string GetClientCommandToUseMods()
            => "!requestMods";

        public static string GetClientCommandToUseDebugMode()
            => "!requestDebug";

        public static string GetHostCommandToAllowMods(float chatRequestId)
            => $"!allowMods{chatRequestId}";

        public static string GetHostCommandToUseDebugMode(float chatRequestId)
         => $"!allowDebug{chatRequestId}";

        public static string GetClientPlayerName()
            => ReplTools.GetLocalPeer().GetDisplayName();

        public static string GetHostPlayerName()
            => P2PSession.Instance.GetSessionMaster().GetDisplayName();

        public static void SetNewChatRequestId()
        {
            ChatRequestId = Mathf.FloorToInt(UnityEngine.Random.Range(0f,9999f));
        }

        public static string EnableModsAndCheatsClientRequest()
            => $"to allow mods and cheats for multiplayer";
        
        public static string EnableDebugModeClientRequest()
            => $"to enable Debug Mode for multiplayer";

        public static string HostCommandToAllowModsWithRequestId()
            => GetHostCommandToAllowMods(ChatRequestId);

        public static string HostCommandToEnableDebugWithRequestId()
            => GetHostCommandToUseDebugMode(ChatRequestId);

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

        public static string RequestWasSentMessage(string text, Color? color = null)
            => SystemInfoChatMessage(
                $"<color=#{(color.HasValue ? ColorUtility.ToHtmlStringRGBA(color.Value) : ColorUtility.ToHtmlStringRGBA(Color.green))}>Request {text} sent to host!</color>",
                color);

        public string PermissionWasGrantedMessage()
            => $"Permission granted!";

        public string PermissionWasRevokedMessage()
            => $"Permission  revoked!";

        public static string FlagStateChangedMessage(bool flagState, string content, Color? color = null)
            => SystemInfoChatMessage(
                $"<color=#{(color.HasValue ? ColorUtility.ToHtmlStringRGBA(color.Value) : ColorUtility.ToHtmlStringRGBA(Color.yellow))}>{content} {(flagState ? "enabled" : "disabled")}!</color>",
                color);

        public static string OnlyHostCanAllowMessage(Color? color = null)
            => SystemInfoChatMessage(
                $"<color=#{(color.HasValue ? ColorUtility.ToHtmlStringRGBA(color.Value) : ColorUtility.ToHtmlStringRGBA(Color.yellow))}>Only the host {GetHostPlayerName()} can grant permission!</color>",
                color);

        public string PlayerWasKickedMessage(string playerNameToKick, Color? color = null)
            => SystemInfoChatMessage($"<color=#{(color.HasValue ? ColorUtility.ToHtmlStringRGBA(color.Value) : ColorUtility.ToHtmlStringRGBA(Color.yellow))}>{playerNameToKick} got kicked from the game!</color>");

        public string MaximumRequestsSendMessage(int maxRequest, Color? color = null)
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

        public string PermissionChangedMessage(string permission)
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

        public static void ShowHUDBigInfo(string text, float duration = 3f)
        {
            string header = $"{ModName} Info";
            string textureName = HUDInfoLogTextureType.Count.ToString();

            HUDBigInfo bigInfo = (HUDBigInfo)LocalHUDManager.GetHUD(typeof(HUDBigInfo));
            HUDBigInfoData.s_Duration = duration;
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
            var messages = (HUDMessages)LocalHUDManager.GetHUD(typeof(HUDMessages));
            messages.AddMessage(localization.Get(localizedTextKey) + "  " + localization.Get(ItemInfo));
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
            onOptionToggled += ModManager_onOptionToggled;
            onPermissionValueChanged += ModManager_onPermissionValueChanged;
            GameModeAtStart = GreenHellGame.Instance.m_GHGameMode;
            GameVisibilityAtSessionStart = P2PSession.Instance.GetGameVisibility();
            GameVisibilityAtStart = GreenHellGame.Instance.m_Settings.m_GameVisibility;
            IsMultiplayerGameModeActive = ( GameVisibilityAtSessionStart == P2PGameVisibility.Singleplayer || GameVisibilityAtStart == P2PGameVisibility.Singleplayer) ? false : true;
            SessionJoinHelperAtStart = GreenHellGame.Instance.m_SessionJoinHelper;
            CanJoinSessionAtStart = MainLevel.Instance.m_CanJoinSession;
            ConfigurableModList = GetModList();
            ShortcutKey = GetShortcutKey();
            SelectedMod = ConfigurableModList.Find(cfgMod => cfgMod.ID == ModName);
            SetNewChatRequestId();
        }

        public KeyCode GetShortcutKey()
        {
            string buttonID = nameof(ShortcutKey);
            return ConfigurableModList.Find(cfgMod => cfgMod.ID == ModName).ConfigurableModButtons.Find(cfgButton => cfgButton.ID == buttonID).ShortcutKey;
        }

        private List<IConfigurableMod> GetModList()
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

        private void ModManager_onPermissionValueChanged(bool optionValue)
        {
            ShowHUDBigInfo(
                          (optionValue ?
                            HUDBigInfoMessage(PermissionChangedMessage($"granted"), MessageType.Info, Color.green)
                            : HUDBigInfoMessage(PermissionChangedMessage($"revoked"), MessageType.Warning, Color.yellow)), 3f);           
        }

        private void ModManager_onOptionToggled(bool optionValue, string optionText)
        {
            ShowHUDBigInfo(FlagStateChangedMessage(optionValue, optionText));
        }

        protected virtual void Update()
        {
            if (Input.GetKeyDown(ShortcutKey))
            {
                if (!ShowModManagerScreen)
                {
                    InitData();
                    EnableCursor(blockPlayer: true);
                }
                ToggleShowUI(0);
                if (!ShowModManagerScreen)
                {
                    EnableCursor(blockPlayer: false);
                }
            }
        }

        private void ToggleShowUI(int controlId)
        {
            switch (controlId)
            {
                case 0:
                    ShowModManagerScreen = !ShowModManagerScreen;
                    return;
                case 1:
                    ShowModMpMngrScreen = !ShowModMpMngrScreen;
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
                case 5:
                    ShowInfo = !ShowInfo;
                    return;
                case 6:
                    ShowModList = !ShowModList;
                    return;
                default:
                    ShowModManagerScreen = !ShowModManagerScreen;
                    ShowModMpMngrScreen = !ShowModMpMngrScreen;
                    ShowGameInfo = !ShowGameInfo;
                    ShowMpInfo = !ShowMpInfo;
                    ShowModInfo = !ShowModInfo;
                    ShowInfo = !ShowInfo;
                    ShowModList = !ShowModList;
                    return;
            }         
        }

        private void OnGUI()
        {
            if (ShowModManagerScreen)
            {
                InitData();
                InitSkinUI();
                ShowModManagerWindow();
            }
            if (ShowModMpMngrScreen)
            {
                InitData();
                InitSkinUI();
                ShowModMpMngrWindow();
            }
        }

        private void ShowModManagerWindow()
        {
            if (ModManagerScreenId < 0 || ModManagerScreenId == ModMpMngrScreenId)
            {
                ModManagerScreenId = GetHashCode();
            }
            ModManagerScreen = GUILayout.Window(
                                               ModManagerScreenId,
                                               ModManagerScreen,
                                               InitModManagerWindow,
                                               $"{ModName} created by [Dragon Legion] Immaanuel#4300",
                                               GUI.skin.window,
                                               GUILayout.ExpandWidth(true),
                                               GUILayout.MinWidth(ModManagerScreenMinWidth),
                                               GUILayout.MaxWidth(ModManagerScreenMaxWidth),
                                               GUILayout.ExpandHeight(true),
                                               GUILayout.MinHeight(ModManagerScreenMinHeight),
                                               GUILayout.MaxHeight(ModManagerScreenMaxHeight));
        }

        private void ShowModMpMngrWindow()
        {
            if (ModMpMngrScreenId < 0 || ModMpMngrScreenId == ModManagerScreenId)
            {
                ModMpMngrScreenId = GetHashCode() + 1;
            }
            ModMpMngrScreen = GUILayout.Window(
                   ModMpMngrScreenId,
                   ModMpMngrScreen,
                   InitModMpMngrWindow,
                   MpManagerTitle,
                   GUI.skin.window,
                   GUILayout.ExpandWidth(true),
                   GUILayout.MinWidth(ModMpMngrScreenMinWidth),
                   GUILayout.MaxWidth(ModMpMngrScreenMaxWidth),
                   GUILayout.ExpandHeight(true),
                   GUILayout.MinHeight(ModMpMngrScreenMinHeight),
                   GUILayout.MaxHeight(ModMpMngrScreenMaxHeight));
        }

        protected virtual void InitData()
        {
            LocalHUDManager = HUDManager.Get();
            LocalPlayer = Player.Get();
            LocalCursorManager = CursorManager.Get();
            LocalStylingManager = StylingManager.Get();
            CoopPlayerList = P2PSession.Instance.m_RemotePeers?.ToList();
            InitPermissionChanged();
        }

        private void InitPermissionChanged()
        {
            if (IsModActiveForMultiplayer)
            {
                PermissionChanged = EventID.ModsAndCheatsEnabled;
            }
            else
            {
                PermissionChanged = EventID.ModsAndCheatsNotEnabled;
            }
            if (EnableDebugMode)
            {
                PermissionChanged = EventID.EnableDebugModeEnabled;
            }
            else
            {
                PermissionChanged = EventID.EnableDebugModeNotEnabled;
            }
            if (IsModActiveForMultiplayer == false && EnableDebugMode == false)
            {
                PermissionChanged = EventID.NoneEnabled;
            }
            if (IsModActiveForMultiplayer == true && EnableDebugMode == true)
            {
                PermissionChanged = EventID.AllEnabled;
            }
        }

        private void InitSkinUI()
        {
            GUI.skin = ModAPI.Interface.Skin;
        }

        private void CloseWindow(int controlId)
        {
            switch (controlId)
            {
                case 0:
                    ShowModManagerScreen = false;
                    ShowModMpMngrScreen = false;
                    EnableCursor(false);
                    return;
                case 1:
                    ShowModMpMngrScreen = false;
                    EnableCursor(false);
                    return;
                default:                    
                    ShowModManagerScreen = false;
                    ShowModMpMngrScreen= false;
                    EnableCursor(false);
                    return;
            }           
        }
        
        private void InitModManagerWindow(int windowID)
        {
            ModManagerScreenStartPositionX = ModManagerScreen.x;
            ModManagerScreenStartPositionY = ModManagerScreen.y;
            ModManagerScreenTotalWidth = ModManagerScreen.width;
            using (var modContentScope = new GUILayout.VerticalScope(GUI.skin.box))
            {
                ScreenMenuBox();
                if (!IsModManagerScreenMinimized)
                {
                    if (IsHostManager)
                    {
                        HostManagerBox();
                    }
                    else
                    {
                        ClientManagerBox();
                    }
                    if (GUILayout.Button("Mod List", GUI.skin.button))
                    {
                        ToggleShowUI(6);
                    }
                    if (ShowModList)
                    {
                        AllModsListBox();
                    }                   
                }
            }
            GUI.DragWindow(new Rect(0f, 0f, 10000f, 10000f));
        }

        private void InitModMpMngrWindow(int windowID)
        {
            ModMpMngrScreenStartPositionX = ModMpMngrScreen.x;
            ModMpMngrScreenStartPositionY = ModMpMngrScreen.y;
            ModMpMngrScreenTotalWidth = ModMpMngrScreen.width;
            using (var modplayersScope = new GUILayout.VerticalScope(GUI.skin.box))
            {
                MpScreenMenuBox();
                if (!IsModMpMngrScreenMinimized)
                {
                    if (GUILayout.Button("Multiplayer Info", GUI.skin.button))
                    {
                        ToggleShowUI(3);
                    }
                    if (ShowMpInfo)
                    {
                        MultiplayerInfoBox();
                    }

                    GUILayout.Label($"All players currently in game: ", GUI.skin.label);
                    PlayersScrollViewBox();

                    SendTexMessagesBox();
                    MpActionButtons();
                }
            }
            GUI.DragWindow(new Rect(0f, 0f, 10000f, 10000f));
        }

        private void MpActionButtons()
        {
            using (new GUILayout.HorizontalScope(GUI.skin.box))
            {
                if (GUILayout.Button("Kick", GUI.skin.button))
                {
                    OnClickKickPlayerButton();
                }
                if (GUILayout.Button("Send message", GUI.skin.button))
                {
                    OnClickSendMessageButton();
                }
                if (GUILayout.Button("Close", GUI.skin.button))
                {
                    ToggleShowUI(1);
                }
            }
        }

        public string[] GetPlayerNames()
        {
            string LocalPeerDisplayName = P2PSession.Instance.LocalPeer.GetDisplayName();
            string[] playerNames = default;
            if (IsHostWithPlayersInCoop && CoopPlayerList != null)
            {
                playerNames = new string[CoopPlayerList.Count + 1];
                playerNames[0] = LocalPeerDisplayName;
                int playerIdx = 1;
                foreach (var peer in CoopPlayerList)
                {
                    playerNames[playerIdx] = peer.GetDisplayName();
                    playerIdx++;
                }
                return playerNames;
            }
            if (IsHostManager)
            {
                playerNames = new string[1];
                playerNames[0] = LocalPeerDisplayName;
                return playerNames;
            }
            return playerNames;
        }

        private void PlayersScrollViewBox()
        {
            using (var pScrollViewScope = new GUILayout.VerticalScope(GUI.skin.box))
            {
                GUI.color = Color.cyan;
                GUILayout.Label($"Selected player: {SelectedPlayerName}", GUI.skin.label);
                GUI.color = LocalStylingManager.DefaultColor;

                PlayerListScrollView();
            }
        }

        private void PlayerListScrollView()
        {
            PlayerListScrollViewPosition = GUILayout.BeginScrollView(PlayerListScrollViewPosition, GUI.skin.scrollView, GUILayout.MinHeight(100f));

            int _SelectedPlayerIndex = SelectedPlayerIndex;
            string[] playerNames = GetPlayerNames();
            if (playerNames != null)
            {
                SelectedPlayerName = playerNames[SelectedPlayerIndex];
                SelectedPlayerIndex = GUILayout.SelectionGrid(SelectedPlayerIndex, playerNames, 3, GUI.skin.button);                
            }

            if (_SelectedPlayerIndex != SelectedPlayerIndex)
            {
                SelectedPlayerName = playerNames[SelectedPlayerIndex];
            }

            GUILayout.EndScrollView();
        }

        private void SendTexMessagesBox()
        {
            using (new GUILayout.HorizontalScope(GUI.skin.box))
            {
                GUILayout.Label($"Type your message: ", GUI.skin.label , GUILayout.Width(150f));
                TextChatMessage = GUILayout.TextArea(TextChatMessage, GUI.skin.textArea);
            }
        }

        private void OnClickKickPlayerButton()
        {
            try
            {
                if (!string.IsNullOrEmpty(SelectedPlayerName) &&
                    CoopPlayerList != null &&
                    SelectedPlayerName?.ToLowerInvariant() != GetHostPlayerName()?.ToLowerInvariant())
                {
                    P2PPeer peerPlayerToKick = CoopPlayerList.Find(peer => peer.GetDisplayName().ToLowerInvariant() == SelectedPlayerName.ToLowerInvariant());
                    if (peerPlayerToKick != null)
                    {
                        P2PLobbyMemberInfo playerToKickLobbyMemberInfo = GetPlayerLobbyMemberInfo(peerPlayerToKick);
                        if (playerToKickLobbyMemberInfo != null)
                        {
                            P2PTransportLayer.Instance.KickLobbyMember(playerToKickLobbyMemberInfo);
                            ShowHUDBigInfo(HUDBigInfoMessage(PlayerWasKickedMessage(SelectedPlayerName), MessageType.Info, Color.green));
                            P2PSession.Instance.SendTextChatMessage(PlayerWasKickedMessage(SelectedPlayerName));
                        }
                        else
                        {
                            ShowHUDBigInfo(HUDBigInfoMessage($"Could not find selected player in lobby!", MessageType.Warning, Color.yellow));
                        }
                    }
                    else
                    {
                        ShowHUDBigInfo(HUDBigInfoMessage($"Could not find selected player in lobby!", MessageType.Warning, Color.yellow));
                    }
                }
                if (SelectedPlayerName?.ToLowerInvariant() == GetHostPlayerName()?.ToLowerInvariant())
                {
                    ShowHUDBigInfo(HUDBigInfoMessage($"Are you trying to kick your self now OO? Impossibru!", MessageType.Warning, Color.red));
                }
                else
                {
                    ShowHUDBigInfo(HUDBigInfoMessage($"Please select a player first.", MessageType.Info, Color.yellow));
                }
            }
            catch (Exception exc)
            {
                HandleException(exc, nameof(OnClickKickPlayerButton));
            }
        }

        private static P2PLobbyMemberInfo GetPlayerLobbyMemberInfo(P2PPeer peerPlayerToKick)
        {
            return CoopLobbyMembers.Find(lm => lm.m_Address == peerPlayerToKick.m_Address);
        }

        private void OnClickSendMessageButton()
        {
            try
            {
                if (!string.IsNullOrEmpty(SelectedPlayerName) &&
                    CoopPlayerList != null &&
                    SelectedPlayerName?.ToLowerInvariant() != GetHostPlayerName()?.ToLowerInvariant())
                {
                    P2PPeer peerPlayerToChat = CoopPlayerList.Find(peer => peer.GetDisplayName().ToLowerInvariant() == SelectedPlayerName.ToLowerInvariant());
                    if (peerPlayerToChat != null)
                    {
                        P2PLobbyMemberInfo playerToChatLobbyMemberInfo = GetPlayerLobbyMemberInfo(peerPlayerToChat);
                        if (playerToChatLobbyMemberInfo != null)
                        {
                            P2PSession.Instance.SendTextChatMessage($"From: {GetHostPlayerName()} \n To {SelectedPlayerName}: \n" + TextChatMessage);
                        }
                        else
                        {
                            ShowHUDBigInfo(HUDBigInfoMessage($"Could not find selected player in lobby!", MessageType.Warning, Color.yellow));
                        }
                    }
                    else
                    {
                        ShowHUDBigInfo(HUDBigInfoMessage($"Could not find selected player in lobby!", MessageType.Warning, Color.yellow));
                    }
                }
                if (SelectedPlayerName?.ToLowerInvariant() == GetHostPlayerName()?.ToLowerInvariant())
                {
                    ShowHUDBigInfo(HUDBigInfoMessage($"Are you trying to talk to your self now OO? Madness inc!", MessageType.Warning, Color.red));
                }
                else
                {
                    ShowHUDBigInfo(HUDBigInfoMessage($"Please select a player first.", MessageType.Info, Color.yellow));
                }
            }
            catch (Exception exc)
            {
                HandleException(exc, nameof(OnClickSendMessageButton));
            }
        }

        private void AllModsListBox()
        {          
            using (var managemodlistScope = new GUILayout.VerticalScope(GUI.skin.box))
            {
                ModListScrollView();

                if (GUILayout.Button("Mod Info", GUI.skin.button))
                {
                    ToggleShowUI(4);
                }
                if (ShowModInfo && SelectedMod != null)
                {
                  ModInfoBox();
                }
                else
                {
                    GUILayout.Label($"Please select a mod first from the mod list.", GUI.skin.label);                    
                }
            }
        }
       
        private void ModListScrollView()
        {
            using (var list123scrollscope = new GUILayout.VerticalScope(GUI.skin.box))
            {
                GUILayout.Label($"ModAPI mod list", LocalStylingManager.ColoredSubHeaderLabel(Color.cyan));

                AllModsScrollView();
            }
        }

        private void AllModsScrollView()
        {
            ModListScrollViewPosition = GUILayout.BeginScrollView(ModListScrollViewPosition, GUI.skin.scrollView, GUILayout.MinHeight(100f));
            int _selectedModIDIndex = SelectedModIDIndex;
            string[] modlistNames = GetModListNames();
            if (modlistNames != null)
            {
                SelectedModID = modlistNames[SelectedModIDIndex];
                SelectedModIDIndex = GUILayout.SelectionGrid(SelectedModIDIndex, modlistNames, 3, GUI.skin.button);

                if (_selectedModIDIndex != SelectedModIDIndex)
                {
                    SelectedModID = modlistNames[SelectedModIDIndex];
                    SelectedMod = ConfigurableModList.Find(cfgMod => cfgMod.ID == SelectedModID);
                }
            }
            GUILayout.EndScrollView();
        }

        public string[] GetModListNames()
        {
            if (ConfigurableModList == null )
            {
                ConfigurableModList = new List<IConfigurableMod>();
                var configurableMod = new ConfigurableMod(GameID.GreenHell.ToString(), ModName, "", "");
                configurableMod.AddConfigurableModButton(nameof(ShortcutKey), ShortcutKey.ToString());
                if (!ConfigurableModList.Contains(configurableMod))
                {
                    ConfigurableModList.Add(configurableMod);
                }            
            }
            string[] modListNames = new string[ConfigurableModList.Count];
            int modIDIdx = 0;
            foreach (var configurableMod in ConfigurableModList)
            {
                modListNames[modIDIdx] = configurableMod.ID;
                modIDIdx++;
            }
            return modListNames;
        }

        private void ClientManagerBox()
        {
            using (var clientmngScope = new GUILayout.VerticalScope(GUI.skin.box))
            {           
                GUILayout.Label("Client Manager", LocalStylingManager.ColoredHeaderLabel(Color.yellow));
                ClientCheatsBox();
                ClientRequestBox();
            }
        }

        private void ClientCheatsBox()
        {       
            GUILayout.Label("Available cheats", LocalStylingManager.ColoredSubHeaderLabel(Color.cyan));
            if (IsModActiveForMultiplayer)
            {
                CheatManagerBox();
            }
            else
            {
                using (var infoScope = new GUILayout.VerticalScope(GUI.skin.label))
                {
                    GUILayout.Label(OnlyForSinglePlayerOrHostMessage(), LocalStylingManager.ColoredCommentLabel(Color.yellow));
                }
            }
        }

        private void ClientRequestBox()
        {
            using (var clientrqtScope = new GUILayout.VerticalScope(GUI.skin.box))
            {
                GUILayout.Label("Send chat request to host to allow mods (max. 6 to avoid spam.)", GUI.skin.label);
                if (GUILayout.Button(GetClientCommandToUseMods(), GUI.skin.button))
                {
                    OnClickRequestModsButton();
                }
                GUILayout.Label("Send chat request to host to allow Debug Mode (max. 6 to avoid spam.)", GUI.skin.label);
                if (GUILayout.Button(GetClientCommandToUseDebugMode(), GUI.skin.button))
                {
                    OnClickRequestDebugModeButton();
                }
            }
        }

        private void OnClickRequestModsButton()
        {
            try
            {
                if (RequestsSendToHost >= 6)
                {
                    ShowHUDBigInfo(MaximumRequestsSendMessage(6));
                    return;
                }
                P2PSession.Instance.SendTextChatMessage(GetClientCommandToUseMods());
                ShowHUDBigInfo(RequestWasSentMessage(EnableModsAndCheatsClientRequest()));
                RequestsSendToHost++;
            }
            catch (Exception exc)
            {
                HandleException(exc, nameof(OnClickRequestModsButton));
            }
        }

        private void OnClickRequestDebugModeButton()
        {
            try
            {
                if (RequestsSendToHost >= 6)
                {
                    ShowHUDBigInfo(MaximumRequestsSendMessage(3));
                    return;
                }
                P2PSession.Instance.SendTextChatMessage(GetClientCommandToUseDebugMode());
                ShowHUDBigInfo(RequestWasSentMessage(EnableDebugModeClientRequest()));
                RequestsSendToHost++;
            }
            catch (Exception exc)
            {
                HandleException(exc, nameof(OnClickRequestDebugModeButton));
            }
        }

        private void HostManagerBox()
        {
            using (var hostmngScope = new GUILayout.VerticalScope(GUI.skin.box))
            {
                GUILayout.Label($"Host Manager", LocalStylingManager.ColoredHeaderLabel(Color.yellow));

                GUILayout.Label($"Host Options", LocalStylingManager.ColoredSubHeaderLabel(Color.yellow)); ;

                HostGameInfoBox();

                ModOptionsBox();

                HostMpMngrBox();
            }
        }

        private void HostMpMngrBox()
        {
            using (var ohmpmngScope = new GUILayout.VerticalScope(GUI.skin.box))
            {
                if (IsMultiplayerGameModeActive)
                {
                    if (GUILayout.Button(MpManagerTitle, GUI.skin.button))
                    {
                        ToggleShowUI(1);
                    }
                }
                else
                {
                    using (var infoScope = new GUILayout.VerticalScope(GUI.skin.label))
                    {
                        GUILayout.Label($"To use {MpManagerTitle}, first enable {nameof(IsMultiplayerGameModeActive)} ", LocalStylingManager.ColoredCommentLabel(Color.yellow));
                    }
                }
            }
        }

        private void HostGameInfoBox()
        {
            using (new GUILayout.VerticalScope(GUI.skin.box))
            {
                if (GUILayout.Button("Game Info", GUI.skin.button))
                {
                    ToggleShowUI(2);
                }
                if (ShowGameInfo)
                {
                    GameInfoBox();
                }
            }
        }

        private void ModOptionsBox()
        {      
            using (new GUILayout.VerticalScope(GUI.skin.box))
            {
                GUILayout.Label(ModName, LocalStylingManager.ColoredHeaderLabel(Color.yellow));

                GUILayout.Label($"{ModName} Options", LocalStylingManager.ColoredSubHeaderLabel(Color.yellow));

                AllowModsAndCheatsOption();
                if (AllowModsAndCheatsForMultiplayer)
                {
                    CheatManagerBox();
                }
                else
                {
                    using ( new GUILayout.VerticalScope(GUI.skin.label))
                    {
                        GUILayout.Label($"To use, first enable cheats in the options above.", LocalStylingManager.ColoredCommentLabel(Color.yellow));
                    }
                }

                EnableDebugModeOption();

                RequestInfoShownOption();
                
                SwitchGameModeOption();
            }
        }

        private void CheatManagerBox()
        {
            using (new GUILayout.VerticalScope(GUI.skin.box))
            {
                GUILayout.Label($"Cheat manager", LocalStylingManager.ColoredHeaderLabel(Color.yellow));

                GUILayout.Label($"Cheat Options", LocalStylingManager.ColoredSubHeaderLabel(Color.yellow));

                Cheats.m_OneShotAI = GUILayout.Toggle(Cheats.m_OneShotAI, "One shot AI cheat on / off", GUI.skin.toggle);
                Cheats.m_OneShotConstructions = GUILayout.Toggle(Cheats.m_OneShotConstructions, "One shot constructions cheat on / off", GUI.skin.toggle);
                Cheats.m_GhostMode = GUILayout.Toggle(Cheats.m_GhostMode, "Ghost mode cheat on / off", GUI.skin.toggle);
                Cheats.m_GodMode = GUILayout.Toggle(Cheats.m_GodMode, "God mode cheat on / off", GUI.skin.toggle);
                Cheats.m_ImmortalItems = GUILayout.Toggle(Cheats.m_ImmortalItems, "No item decay cheat on / off", GUI.skin.toggle);
                Cheats.m_InstantBuild = GUILayout.Toggle(Cheats.m_InstantBuild, "Instant build cheat on / off", GUI.skin.toggle);
            }
        }

        private void GameInfoBox()
        {
            using (var gameinfoScope = new GUILayout.VerticalScope(GUI.skin.box))
            {
                GameInfoScrollViewPosition = GUILayout.BeginScrollView(GameInfoScrollViewPosition, GUI.skin.scrollView, GUILayout.MinHeight(250f));

                GUILayout.Label($"{nameof(GreenHellGame)}", LocalStylingManager.ColoredSubHeaderLabel(Color.cyan));
                using (new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    GUILayout.Label($"{nameof(PermissionChanged)}: ", LocalStylingManager.FormFieldNameLabel);
                    GUILayout.Label($"{PermissionChanged}", LocalStylingManager.FormFieldValueLabel);
                }
                using (new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    GUILayout.Label($"{nameof(SteamAppId)}: ", LocalStylingManager.FormFieldNameLabel);
                    GUILayout.Label($"{SteamAppId}", LocalStylingManager.FormFieldValueLabel);
                }
                using (new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    GUILayout.Label($"{nameof(GameModeAtStart)}:", LocalStylingManager.FormFieldNameLabel);
                    GUILayout.Label($"{GameModeAtStart}", LocalStylingManager.FormFieldValueLabel);
                }
                using (new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    GUILayout.Label($"{nameof(GameVisibilityAtStart)}:", LocalStylingManager.FormFieldNameLabel);
                    GUILayout.Label($"{GameVisibilityAtStart}", LocalStylingManager.FormFieldValueLabel);
                }
                using (new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    GUILayout.Label($"{nameof(GameVisibilityAtSessionStart)}:", LocalStylingManager.FormFieldNameLabel);
                    GUILayout.Label($"{GameVisibilityAtSessionStart}", LocalStylingManager.FormFieldValueLabel);
                }
                using (new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    GUILayout.Label($"{nameof(GameMode)}:", LocalStylingManager.FormFieldNameLabel);
                    GUILayout.Label($"{GreenHellGame.Instance.m_GHGameMode}", LocalStylingManager.FormFieldValueLabel);
                }
                using (new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    GUILayout.Label($"{nameof(P2PGameVisibility)}:", LocalStylingManager.FormFieldNameLabel);
                    GUILayout.Label($"{GreenHellGame.Instance.m_Settings.m_GameVisibility}", LocalStylingManager.FormFieldValueLabel);
                }

                GUILayout.Label($"{nameof(MainLevel)}", LocalStylingManager.ColoredSubHeaderLabel(Color.cyan));

                using (new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    GUILayout.Label($"{nameof(GameMode)}:", LocalStylingManager.FormFieldNameLabel);
                    GUILayout.Label($"{MainLevel.Instance.m_GameMode}", LocalStylingManager.FormFieldValueLabel);
                }
                using (new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    GUILayout.Label($"{nameof(MainLevel)}.{nameof(MainLevel.Instance)}.{nameof(MainLevel.Instance.m_CanJoinSession)}:", LocalStylingManager.FormFieldNameLabel);
                    GUILayout.Label($"{(MainLevel.Instance.m_CanJoinSession ? "enabled" : "disabled")}", LocalStylingManager.ColoredToggleFieldValueLabel( MainLevel.Instance.m_CanJoinSession, Color.green, LocalStylingManager.DefaultColor));
                }
                using (new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    GUILayout.Label($"{nameof(MainLevel)}.{nameof(MainLevel.Instance)}.{nameof(MainLevel.Instance.m_Tutorial)}:", LocalStylingManager.FormFieldNameLabel);
                    GUILayout.Label($"{(MainLevel.Instance.m_Tutorial ? "enabled" : "disabled")}", LocalStylingManager.ColoredToggleFieldValueLabel(MainLevel.Instance.m_Tutorial, Color.green, LocalStylingManager.DefaultColor));
                }

                GUILayout.Label(ModName, LocalStylingManager.ColoredSubHeaderLabel(Color.cyan));

                using (new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    string NextGameMode = IsMultiplayerGameModeActive ? "Switch to singleplayer" : "Switch to multiplayer";
                    GUILayout.Label($"{nameof(NextGameMode)}: ", LocalStylingManager.FormFieldNameLabel);
                    GUILayout.Label(NextGameMode, LocalStylingManager.FormFieldValueLabel);
                }
                using (new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    GUILayout.Label($"{nameof(IsMultiplayerGameModeActive)}: ", LocalStylingManager.FormFieldNameLabel);
                    GUILayout.Label($"{(IsMultiplayerGameModeActive ? "enabled" : "disabled")}", LocalStylingManager.ColoredToggleFieldValueLabel(IsMultiplayerGameModeActive, Color.green, LocalStylingManager.DefaultColor));
                }
                using (new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    GUILayout.Label($"{nameof(IsModActiveForSingleplayer)}: ", LocalStylingManager.FormFieldNameLabel);
                    GUILayout.Label($"{(IsModActiveForSingleplayer ? "enabled" : "disabled")}", LocalStylingManager.ColoredToggleFieldValueLabel(IsModActiveForSingleplayer, Color.green, LocalStylingManager.DefaultColor));
                }               
                using (new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    GUILayout.Label($"{nameof(IsModActiveForMultiplayer)}", LocalStylingManager.FormFieldNameLabel);
                    GUILayout.Label($"{(IsModActiveForMultiplayer ? "enabled" : "disabled")}", LocalStylingManager.ColoredToggleFieldValueLabel(IsModActiveForMultiplayer, Color.green, LocalStylingManager.DefaultColor));
                }                
                using (new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    GUILayout.Label($"{nameof(AllowModsAndCheatsForMultiplayer)}:", LocalStylingManager.FormFieldNameLabel);
                    GUILayout.Label($"{(AllowModsAndCheatsForMultiplayer ? "enabled" : "disabled")}", LocalStylingManager.ColoredToggleFieldValueLabel(AllowModsAndCheatsForMultiplayer, Color.green, LocalStylingManager.DefaultColor));
                }
                using (new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    GUILayout.Label($"{nameof(GreenHellGame.DEBUG)} Mode: ", LocalStylingManager.FormFieldNameLabel);
                    GUILayout.Label($"{(GreenHellGame.DEBUG ? "enabled" : "disabled")}", LocalStylingManager.ColoredToggleFieldValueLabel(GreenHellGame.DEBUG, Color.green, LocalStylingManager.DefaultColor));
                }
                using (new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    GUILayout.Label($"{nameof(EnableDebugMode)}:", LocalStylingManager.FormFieldNameLabel);
                    GUILayout.Label($"{(EnableDebugMode ? "enabled" : "disabled")}", LocalStylingManager.ColoredToggleFieldValueLabel(EnableDebugMode, Color.green, LocalStylingManager.DefaultColor));
                }
       
                GUILayout.Label($"{nameof(Cheats)}", LocalStylingManager.ColoredSubHeaderLabel(Color.cyan));

                using (new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    GUILayout.Label($"{nameof(Cheats)}.{nameof(Cheats.m_OneShotAI)}:", LocalStylingManager.FormFieldNameLabel);
                    GUILayout.Label($"{(Cheats.m_OneShotAI ? "enabled" : "disabled")}", LocalStylingManager.ColoredToggleFieldValueLabel(Cheats.m_OneShotAI, Color.green, LocalStylingManager.DefaultColor));
                }
                using (new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    GUILayout.Label($"{nameof(Cheats)}.{nameof(Cheats.m_OneShotConstructions)}:", LocalStylingManager.FormFieldNameLabel);
                    GUILayout.Label($"{(Cheats.m_OneShotConstructions ? "enabled" : "disabled")}", LocalStylingManager.ColoredToggleFieldValueLabel(Cheats.m_OneShotConstructions, Color.green, LocalStylingManager.DefaultColor));
                }
               
                using (new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    GUILayout.Label($"{nameof(Cheats)}.{nameof(Cheats.m_InstantBuild)}: ", LocalStylingManager.FormFieldNameLabel);
                    GUILayout.Label($"{(Cheats.m_InstantBuild ? "enabled" : "disabled")}", LocalStylingManager.ColoredToggleFieldValueLabel(Cheats.m_InstantBuild, Color.green, LocalStylingManager.DefaultColor));
                }
                using (new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    GUILayout.Label($"{nameof(Cheats)}.{nameof(Cheats.m_GhostMode)}:", LocalStylingManager.FormFieldNameLabel);
                    GUILayout.Label($"{(Cheats.m_GhostMode ? "enabled" : "disabled")}", LocalStylingManager.ColoredToggleFieldValueLabel(Cheats.m_GhostMode, Color.green, LocalStylingManager.DefaultColor));
                }
                using (new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    GUILayout.Label($"{nameof(Cheats)}.{nameof(Cheats.m_GodMode)}: ", LocalStylingManager.FormFieldNameLabel);
                    GUILayout.Label($"{(Cheats.m_GodMode ? "enabled" : "disabled")}", LocalStylingManager.ColoredToggleFieldValueLabel(Cheats.m_GodMode, Color.green, LocalStylingManager.DefaultColor));
                }
                using (new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    GUILayout.Label($"{nameof(Cheats)}.{nameof(Cheats.m_ImmortalItems)}: ", LocalStylingManager.FormFieldNameLabel);
                    GUILayout.Label($"{(Cheats.m_ImmortalItems ? "enabled" : "disabled")}", LocalStylingManager.ColoredToggleFieldValueLabel(Cheats.m_ImmortalItems, Color.green, LocalStylingManager.DefaultColor));
                }

                GUILayout.EndScrollView();
            }
        }

        private void MultiplayerInfoBox()
        {
            LocalHostDisplayName = GetHostPlayerName();
            if (ChatRequestId == 0 || ChatRequestId == int.MinValue || ChatRequestId == int.MaxValue)
            {
                SetNewChatRequestId();
            }
            using (new GUILayout.VerticalScope(GUI.skin.box))
            {
                MpInfoScrollViewPosition = GUILayout.BeginScrollView(MpInfoScrollViewPosition, GUI.skin.scrollView, GUILayout.MinHeight(250f));

                GUILayout.Label("Multiplayer info", LocalStylingManager.ColoredSubHeaderLabel( Color.cyan));

                using (new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    GUILayout.Label($"{nameof(LocalHostDisplayName)}:", LocalStylingManager.FormFieldNameLabel);
                    GUILayout.Label($"{LocalHostDisplayName}", LocalStylingManager.FormFieldValueLabel);
                }
                using (new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    GUILayout.Label($"{nameof(PlayerCount)}:", LocalStylingManager.FormFieldNameLabel);
                    GUILayout.Label($"{PlayerCount}", LocalStylingManager.FormFieldValueLabel);
                }
                using (new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    GUILayout.Label($"{nameof(IsHostManager)}:", LocalStylingManager.FormFieldNameLabel);
                    GUILayout.Label($"{(IsHostManager ? "enabled" : "disabled")}",LocalStylingManager.ColoredToggleFieldValueLabel(IsHostManager, Color.green, LocalStylingManager.DefaultColor));
                }               
                using (new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    GUILayout.Label($"{nameof(IsHostWithPlayersInCoop)}:", LocalStylingManager.FormFieldNameLabel);
                    GUILayout.Label($"{(IsHostWithPlayersInCoop ? "enabled" : "disabled")}",LocalStylingManager.ColoredToggleFieldValueLabel(IsHostWithPlayersInCoop, Color.green, LocalStylingManager.DefaultColor));
                }               
                using (new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    GUILayout.Label($"{nameof(AllowModsAndCheatsForMultiplayer)}:", LocalStylingManager.FormFieldNameLabel);
                    GUILayout.Label($"{(AllowModsAndCheatsForMultiplayer ? "enabled" : "disabled")}",LocalStylingManager.ColoredToggleFieldValueLabel(AllowModsAndCheatsForMultiplayer, Color.green, LocalStylingManager.DefaultColor));
                }               
                using (new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    GUILayout.Label($"Host command to allow mods:", LocalStylingManager.FormFieldNameLabel);
                    GUILayout.Label($"{HostCommandToAllowModsWithRequestId()}", LocalStylingManager.FormFieldValueLabel);
                }
                using (new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    GUILayout.Label($"{nameof(EnableDebugMode)}:", LocalStylingManager.FormFieldNameLabel);
                    GUILayout.Label($"{(EnableDebugMode ? "enabled" : "disabled")}",LocalStylingManager.ColoredToggleFieldValueLabel(EnableDebugMode, Color.green, LocalStylingManager.DefaultColor));
                }
                using (new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    GUILayout.Label($"Host command to enable Debug Mode:", LocalStylingManager.FormFieldNameLabel);
                    GUILayout.Label($"{HostCommandToEnableDebugWithRequestId()}", LocalStylingManager.FormFieldValueLabel);
                }               

                GUILayout.EndScrollView();
            }
        }

        private void ModInfoBox()
        {
            using (var modinfoScope = new GUILayout.VerticalScope(GUI.skin.box))
            {
                ModInfoScrollViewPosition = GUILayout.BeginScrollView(ModInfoScrollViewPosition, GUI.skin.scrollView, GUILayout.MinHeight(150f));

                GUILayout.Label("Mod Info", LocalStylingManager.ColoredSubHeaderLabel(Color.cyan));

                using (new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    GUILayout.Label($"{nameof(IConfigurableMod.GameID)}:", LocalStylingManager.FormFieldNameLabel);
                    GUILayout.Label($"{SelectedMod.GameID}", LocalStylingManager.FormFieldValueLabel);
                }
                using (new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    GUILayout.Label($"{nameof(IConfigurableMod.ID)}:", LocalStylingManager.FormFieldNameLabel);
                    GUILayout.Label($"{SelectedMod.ID}", LocalStylingManager.FormFieldValueLabel);
                }
                using (new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    GUILayout.Label($"{nameof(IConfigurableMod.UniqueID)}:", LocalStylingManager.FormFieldNameLabel);
                    GUILayout.Label($"{SelectedMod.UniqueID}", LocalStylingManager.FormFieldValueLabel);
                }
                using (new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    GUILayout.Label($"{nameof(IConfigurableMod.Version)}:", LocalStylingManager.FormFieldNameLabel);
                    GUILayout.Label($"{SelectedMod.Version}", LocalStylingManager.FormFieldValueLabel);
                }

                GUILayout.Label("Buttons Info", LocalStylingManager.ColoredSubHeaderLabel(Color.cyan));

                foreach (var configurableModButton in SelectedMod.ConfigurableModButtons)
                {
                    using (new GUILayout.HorizontalScope(GUI.skin.box))
                    {
                        GUILayout.Label($"{nameof(IConfigurableModButton.ID)}:", LocalStylingManager.FormFieldNameLabel);
                        GUILayout.Label($"{configurableModButton.ID}", LocalStylingManager.FormFieldValueLabel);
                    }
                    using (new GUILayout.HorizontalScope(GUI.skin.box))
                    {
                        GUILayout.Label($"{nameof(IConfigurableModButton.KeyBinding)}:", LocalStylingManager.FormFieldNameLabel);
                        GUILayout.Label($"{configurableModButton.KeyBinding}", LocalStylingManager.FormFieldValueLabel);
                    }
                }

                GUILayout.EndScrollView();
            }
        }

        private void ScreenMenuBox()
        {
            string collapseButtonText = IsModManagerScreenMinimized ? "O" : "-";

            if (GUI.Button(new Rect(ModManagerScreen.width - 40f, 0f, 20f, 20f), collapseButtonText, GUI.skin.button))
            {
                CollapseWindow();
            }
            if (GUI.Button(new Rect(ModManagerScreen.width - 20f, 0f, 20f, 20f), "X", GUI.skin.button))
            {
                CloseWindow(0);
            }
        }

        private void MpScreenMenuBox()
        {
            string collapseButtonText = IsModMpMngrScreenMinimized ? "O" : "-";

            if (GUI.Button(new Rect(ModMpMngrScreen.width - 40f, 0f, 20f, 20f), collapseButtonText, GUI.skin.button))
            {
                CollapseMpWindow();
            }
            if (GUI.Button(new Rect(ModMpMngrScreen.width - 20f, 0f, 20f, 20f), "X", GUI.skin.button))
            {
                CloseWindow(1);
            }
        }

        private void CollapseWindow()
        {
            ModManagerScreenStartPositionX = ModManagerScreen.x;
            ModManagerScreenStartPositionY = ModManagerScreen.y;
            ModManagerScreenTotalWidth = ModManagerScreen.width;

            if (!IsModManagerScreenMinimized)
            {
                ModManagerScreen = new Rect(ModManagerScreenStartPositionX, ModManagerScreenStartPositionY, ModManagerScreenTotalWidth, ModManagerScreenMinHeight);
                IsModManagerScreenMinimized = true;
            }
            else
            {
                ModManagerScreen = new Rect(ModManagerScreenStartPositionX, ModManagerScreenStartPositionY, ModManagerScreenTotalWidth, ModManagerScreenTotalHeight);
                IsModManagerScreenMinimized = false;
            }
            ShowModManagerWindow();
        }

        private void CollapseMpWindow()
        {
            ModMpMngrScreenStartPositionX = ModMpMngrScreen.x;
            ModMpMngrScreenStartPositionY = ModMpMngrScreen.y;
            ModMpMngrScreenTotalWidth = ModMpMngrScreen.width;

            if (!IsModMpMngrScreenMinimized)
            {
                ModMpMngrScreen = new Rect(ModMpMngrScreenStartPositionX, ModMpMngrScreenStartPositionY, ModMpMngrScreenTotalWidth, ModMpMngrScreenMinHeight);
                IsModMpMngrScreenMinimized = true;
            }
            else
            {
                ModMpMngrScreen = new Rect(ModMpMngrScreenStartPositionX, ModMpMngrScreenStartPositionY, ModMpMngrScreenTotalWidth, ModMpMngrScreenTotalHeight);
                IsModMpMngrScreenMinimized = false;
            }
            ShowModMpMngrWindow();
        }

        private void AllowModsAndCheatsOption()
        {
            bool _allowModsAndCheatsForMultiplayerValue = AllowModsAndCheatsForMultiplayer;
            AllowModsAndCheatsForMultiplayer = GUILayout.Toggle(AllowModsAndCheatsForMultiplayer, "Allow mods and cheats for multiplayer?", GUI.skin.toggle);
            IsModActiveForMultiplayer = AllowModsAndCheatsForMultiplayer;
            ToggleModOption(_allowModsAndCheatsForMultiplayerValue, nameof(AllowModsAndCheatsForMultiplayer));
        }

        private void EnableDebugModeOption()
        {
            bool _enableDebugMode = GreenHellGame.DEBUG;
            GreenHellGame.DEBUG = GUILayout.Toggle(GreenHellGame.DEBUG, "Enable Debug Mode for multiplayer?", GUI.skin.toggle);
            EnableDebugMode = GreenHellGame.DEBUG;
            ToggleModOption(_enableDebugMode, nameof(GreenHellGame.DEBUG));
        }

        private void RequestInfoShownOption()
        {
            bool _requestInfoShownValue = RequestInfoShown;
            RequestInfoShown = GUILayout.Toggle(RequestInfoShown, "Chat request info shown?", GUI.skin.toggle);
            ToggleModOption(_requestInfoShownValue, nameof(RequestInfoShown));
        }

        private void SwitchGameModeOption()
        {
            string NextGameMode = IsMultiplayerGameModeActive == true ? "Switch to singleplayer" : "Switch to multiplayer";
            bool _isMultiplayerGameModeActive = IsMultiplayerGameModeActive;

            if (GUILayout.Button(NextGameMode, GUI.skin.button))
            {
                IsMultiplayerGameModeActive = !IsMultiplayerGameModeActive;
            }
                     
            if (_isMultiplayerGameModeActive != IsMultiplayerGameModeActive)
            {   
                ShowConfirmSwitchGameModeDialog();
            }          
        }

        public static void ToggleModOption(bool optionState, string optionName)
        {
            if (optionName == nameof(GreenHellGame.DEBUG) && optionState != GreenHellGame.DEBUG)
            {
                if (GreenHellGame.DEBUG)
                {
                    GreenHellGame.Instance.m_GHGameMode = GameMode.Debug;
                    MainLevel.Instance.m_GameMode = GameMode.Debug;
                }
                else
                {
                    GreenHellGame.Instance.m_GHGameMode = GameModeAtStart;
                    MainLevel.Instance.m_GameMode = GameModeAtStart;
                }
                onOptionToggled?.Invoke(GreenHellGame.DEBUG, $"Debug Mode has been");               
            }

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
        }

        private void SwitchGameMode()
        {
            try
            {
                GreenHellGame.Instance.m_Settings.m_GameVisibility = IsMultiplayerGameModeActive == true ? P2PGameVisibility.Friends : P2PGameVisibility.Singleplayer;
                GreenHellGame.Instance.m_SessionJoinHelper = SessionJoinHelperAtStart ?? new SessionJoinHelper();
                GreenHellGame.FORCE_SURVIVAL = true;
                MainLevel.Instance.m_CanJoinSession = CanJoinSessionAtStart;
                MainLevel.Instance.m_Tutorial = false;
                if (IsMultiplayerGameModeActive && ReplTools.IsCoopEnabled())
                {
                    GreenHellGame.Instance.m_GHGameMode = GameMode.PVE;
                    MainLevel.Instance.m_GameMode = GameMode.PVE;
                    P2PSession.Instance.SetGameVisibility(P2PGameVisibility.Friends);
                    P2PSession.Instance.Start();
                }
                else
                {                 
                    GreenHellGame.Instance.m_GHGameMode = GameModeAtStart;
                    MainLevel.Instance.m_GameMode = GameModeAtStart;
                    P2PSession.Instance.SetGameVisibility(P2PGameVisibility.Singleplayer);
                    P2PSession.Instance.Start(null);
                }
                MainLevel.Instance.StartLevel();
            }
            catch (Exception exc)
            {
                HandleException(exc, nameof(SwitchGameMode));
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

        private static void HandleException(Exception exc, string methodName)
        {           
            string info =
                $"[{ModName}:{methodName}] throws exception: {exc?.Message}\n" +
                $"{exc?.StackTrace}\n" +
                $"{exc?.InnerException}\n" +
                $"Source: {exc?.Source}\n" +
                $"{exc?.InnerException?.InnerException?.Message}\n";
            ModAPI.Log.Write(info);
            Debug.Log(info);
        }

        private void ShowConfirmSwitchGameModeDialog()
        {
            try
            {
                string description = $"Are you sure you want to switch to  {(IsMultiplayerGameModeActive == true ? "multiplayer?\nYour current game will first be saved, if possible.\n"  : "singleplayer?\nYour current game and of coop players' games will first be saved, if possible.\n")}\n";
                YesNoDialog switchYesNoDialog = GreenHellGame.GetYesNoDialog();
                switchYesNoDialog.Show(this, DialogWindowType.YesNo, $"{ModName} Info", description, true, false);
                switchYesNoDialog.gameObject.SetActive(true);
            }
            catch (Exception exc)
            {
                HandleException(exc, nameof(ShowConfirmSwitchGameModeDialog));
            }
        }

        public void OnYesFromDialog()
        {
            SaveGameOnSwitch();
            SwitchGameMode();
            EnableCursor(false);
        }

        public void OnNoFromDialog()
        {
         
            EnableCursor(false);
        }

        public void OnOkFromDialog()
        {
            OnYesFromDialog();
        }

        public void OnCloseDialog()
        {
            EnableCursor(false);
        }
    }
}