using Enums;
using ModManager.Data.Enums;
using ModManager.Data.Interfaces;
using ModManager.Data.Modding;
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

        private static float ModManagerScreenTotalWidth { get; set; } = 500f;
        private static float ModManagerScreenTotalHeight { get; set; } = 350f;
        private static float ModManagerScreenMinWidth { get; set; } = 500f;
        private static float ModManagerScreenMinHeight { get; set; } = 50f;
        private static float ModManagerScreenMaxWidth { get; set; } = Screen.width;
        private static float ModManagerScreenMaxHeight { get; set; } = Screen.height;
        private static float ModManagerScreenStartPositionX { get; set; } = Screen.width / 2f;
        private static float ModManagerScreenStartPositionY { get; set; } = Screen.height / 2f;
        private static bool IsMinimized { get; set; } = false;
        private static int ModManagerScreenId { get; set; } = -2;

        private static float ModMpMngrScreenTotalWidth { get; set; } = 500f;
        private static float ModMpMngrScreenTotalHeight { get; set; } = 350f;
        private static float ModMpMngrScreenMinWidth { get; set; } = 500f;        
        private static float ModMpMngrScreenMinHeight { get; set; } = 50f;
        private static float ModMpMngrScreenMaxWidth { get; set; } = Screen.width;
        private static float ModMpMngrScreenMaxHeight { get; set; } = Screen.height;
        private static float ModMpMngrScreenStartPositionX { get; set; } = Screen.width / 2.5f;
        private static float ModMpMngrScreenStartPositionY { get; set; } = Screen.height / 2.5f;
        private static bool IsMpMinimized { get; set; }
        private static int ModMpMngrScreenId { get; set; } = -1;

        private static CursorManager LocalCursorManager;
        private static HUDManager LocalHUDManager;
        private static Player LocalPlayer;

        private Color DefaultColor = GUI.color;

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

        public GUIStyle InfoHeaderLabel => new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold,
            fontSize = 12             
        };
        public GUIStyle InfoFieldNameLabel => new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleLeft,
            fontSize = 12,
            stretchWidth = true,
            wordWrap = true,
            fontStyle= FontStyle.Italic
        };
        public GUIStyle InfoFieldValueLabel => new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleRight,
            fontSize = 12,
            stretchWidth = true,
            wordWrap = true        
        };

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

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            Instance = null;
        }

        private void Start()
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

        private void Update()
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
                    break;
                case 1:
                    ShowModMpMngrScreen = !ShowModMpMngrScreen;
                    break;
                case 2:
                    ShowGameInfo = !ShowGameInfo;
                    break; 
                case 3:
                    ShowMpInfo = !ShowMpInfo;
                    break;
                case 4:
                    ShowModInfo = !ShowModInfo;
                    break;
                case 5:
                    ShowInfo = !ShowInfo;
                    break;
                case 6:
                    ShowModList = !ShowModList;
                    break;
                default:
                    ShowModManagerScreen = !ShowModManagerScreen;
                    ShowModMpMngrScreen = !ShowModMpMngrScreen;
                    ShowGameInfo = !ShowGameInfo;
                    ShowMpInfo = !ShowMpInfo;
                    ShowModInfo = !ShowModInfo;
                    ShowInfo = !ShowInfo;
                    ShowModList = !ShowModList;
                    break;
            }
            if (!ShowModManagerScreen && !ShowModMpMngrScreen)
            {
                EnableCursor(false);
            }
            else
            {
                EnableCursor(true);
            }
        }

        private void OnGUI()
        {
            if (ShowModManagerScreen || ShowModMpMngrScreen)
            {
                InitData();
                InitSkinUI();
                if (ShowModManagerScreen)
                {
                    InitModManagerScreen();
                }
                if (ShowModMpMngrScreen)
                {
                    InittModMpMngrScreen();
                }
            }           
        }

        private void InitModManagerScreen()
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

        private void InittModMpMngrScreen()
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

        private void InitData()
        {
            LocalHUDManager = HUDManager.Get();
            LocalPlayer = Player.Get();
            LocalCursorManager = CursorManager.Get();
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

        private void CloseWindow(bool blockPlayer = false)
        {
            ShowModManagerScreen = false;
            ShowModMpMngrScreen = false;
            EnableCursor(blockPlayer);            
        }
        
        private void InitModManagerWindow(int windowID)
        {
            ModManagerScreenStartPositionX = ModManagerScreen.x;
            ModManagerScreenStartPositionY = ModManagerScreen.y;
            ModManagerScreenTotalWidth = ModManagerScreen.width;
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
                if (!IsMpMinimized)
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
                GUI.color = DefaultColor;

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
            using (var textmsgViewScope = new GUILayout.HorizontalScope(GUI.skin.box))
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
                if (ShowModInfo)
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
                GUILayout.Label($"ModAPI mod list", ColoredInfoHeaderLabel(InfoHeaderLabel, Color.cyan));

                AllModsScrollView();
            }
        }

        private void AllModsScrollView()
        {
            ModListScrollViewPosition = GUILayout.BeginScrollView(ModListScrollViewPosition, GUI.skin.scrollView, GUILayout.MinHeight(100f));
            int _SelectedModIDIndex = SelectedModIDIndex;
            string[] modlistNames = GetModListNames();
            if (modlistNames != null)
            {
                SelectedModIDIndex = GUILayout.SelectionGrid(SelectedModIDIndex, modlistNames, 3, GUI.skin.button);
                if (_SelectedModIDIndex != SelectedModIDIndex)
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
                GUILayout.Label("Client Manager", ColoredInfoHeaderLabel(InfoHeaderLabel, Color.yellow));
                ClientCheatsBox();
                ClientRequestBox();
            }
        }

        private void ClientCheatsBox()
        {       
            GUILayout.Label("Available cheats", ColoredInfoHeaderLabel(InfoHeaderLabel, Color.cyan));
            if (IsModActiveForMultiplayer)
            {
                CheatOptionsBox();
            }
            else
            {
                using (var infoScope = new GUILayout.VerticalScope(GUI.skin.label))
                {
                    GUILayout.Label(OnlyForSinglePlayerOrHostMessage(), ColoredInfoHeaderLabel(InfoHeaderLabel, Color.yellow));
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
                GUILayout.Label("Host Manager", ColoredInfoHeaderLabel(InfoHeaderLabel, Color.yellow));

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
                        GUILayout.Label($"To use {MpManagerTitle}, first enable {nameof(IsMultiplayerGameModeActive)} ", ColoredInfoHeaderLabel(InfoHeaderLabel, Color.yellow));
                    }
                }
            }
        }

        private void HostGameInfoBox()
        {
            using (var hgibScope = new GUILayout.VerticalScope(GUI.skin.box))
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
            using (var optionsScope = new GUILayout.VerticalScope(GUI.skin.box))
            {
                GUILayout.Label($"{ModName} Options", ColoredInfoHeaderLabel(InfoHeaderLabel, Color.cyan));

                AllowModsAndCheatsOption();
                if (AllowModsAndCheatsForMultiplayer)
                {
                    CheatOptionsBox();
                }
                else
                {
                    using (var infoScope = new GUILayout.VerticalScope(GUI.skin.label))
                    {
                        GUILayout.Label($"To use cheats, first enable {nameof(AllowModsAndCheatsForMultiplayer)} ", ColoredInfoHeaderLabel(InfoHeaderLabel, Color.yellow));                     
                    }
                }

                EnableDebugModeOption();

                RequestInfoShownOption();
                
                SwitchGameModeOption();
            }
        }

        public GUIStyle EnabledDisabledInfoFieldValueLabel(GUIStyle style, bool enabled)
        {
            if (enabled)
            {
                style.normal.textColor = Color.green;
            }
            else
            {
                style.normal.textColor = DefaultColor;
            }
            return style;
        }

        public GUIStyle ColoredInfoHeaderLabel(GUIStyle style, Color color)
        {
            style.normal.textColor = color;
            return style;
        }

        private void CheatOptionsBox()
        {
            using (var optscheatsHScope = new GUILayout.VerticalScope(GUI.skin.box))
            {
                GUILayout.Label($"Cheat Options", ColoredInfoHeaderLabel(InfoHeaderLabel, Color.cyan));
                
                Cheats.m_OneShotAI = GUILayout.Toggle(Cheats.m_OneShotAI, "One shot AI cheat on / off?", GUI.skin.toggle);
                Cheats.m_OneShotConstructions = GUILayout.Toggle(Cheats.m_OneShotConstructions, "One shot constructions cheat on / off?", GUI.skin.toggle);
                Cheats.m_GhostMode = GUILayout.Toggle(Cheats.m_GhostMode, "Ghost mode cheat on / off?", GUI.skin.toggle);
                Cheats.m_GodMode = GUILayout.Toggle(Cheats.m_GodMode, "God mode cheat on / off?", GUI.skin.toggle);
                Cheats.m_ImmortalItems = GUILayout.Toggle(Cheats.m_ImmortalItems, "No item decay cheat on / off?", GUI.skin.toggle);
            }
        }

        private void GameInfoBox()
        {
            using (var gameinfoScope = new GUILayout.VerticalScope(GUI.skin.box))
            {
                GameInfoScrollViewPosition = GUILayout.BeginScrollView(GameInfoScrollViewPosition, GUI.skin.scrollView, GUILayout.MinHeight(100f));

                GUILayout.Label($"{nameof(GreenHellGame)}", ColoredInfoHeaderLabel(InfoHeaderLabel, Color.cyan));
                
                using (var steamappScope = new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    GUILayout.Label($"{nameof(SteamAppId)}: ", InfoFieldNameLabel);
                    GUILayout.Label($"{SteamAppId}", InfoFieldValueLabel);
                }
                using (var gghmodestartScope = new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    GUILayout.Label($"{nameof(GameModeAtStart)}:", InfoFieldNameLabel);
                    GUILayout.Label($"{GameModeAtStart}", InfoFieldValueLabel);
                }
                using (var gghmodevisstartScope = new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    GUILayout.Label($"{nameof(GameVisibilityAtStart)}:", InfoFieldNameLabel);
                    GUILayout.Label($"{GameVisibilityAtStart}", InfoFieldValueLabel);
                }
                using (var gghsesvisstartScope = new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    GUILayout.Label($"{nameof(GameVisibilityAtSessionStart)}:", InfoFieldNameLabel);
                    GUILayout.Label($"{GameVisibilityAtSessionStart}", InfoFieldValueLabel);
                }
                using (var gghmodeScope = new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    GUILayout.Label($"{nameof(GameMode)}:", InfoFieldNameLabel);
                    GUILayout.Label($"{GreenHellGame.Instance.m_GHGameMode}", InfoFieldValueLabel);
                }
                using (var gmvisScope = new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    GUILayout.Label($"{nameof(P2PGameVisibility)}:", InfoFieldNameLabel);
                    GUILayout.Label($"{GreenHellGame.Instance.m_Settings.m_GameVisibility}", InfoFieldValueLabel);
                }

                GUILayout.Label($"{nameof(MainLevel)}", ColoredInfoHeaderLabel(InfoHeaderLabel, Color.cyan));

                using (var mlevelggmodeScope = new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    GUILayout.Label($"{nameof(GameMode)}:", InfoFieldNameLabel);
                    GUILayout.Label($"{MainLevel.Instance.m_GameMode}", InfoFieldValueLabel);
                }
                using (var mleveljoinScope = new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    GUILayout.Label($"{nameof(MainLevel)}.{nameof(MainLevel.Instance)}.{nameof(MainLevel.Instance.m_CanJoinSession)}:", InfoFieldNameLabel);
                    GUILayout.Label($"{(MainLevel.Instance.m_CanJoinSession ? "enabled" : "disabled")}", EnabledDisabledInfoFieldValueLabel(InfoFieldValueLabel, MainLevel.Instance.m_CanJoinSession));
                }
                using (var mleveltutScope = new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    GUILayout.Label($"{nameof(MainLevel)}.{nameof(MainLevel.Instance)}.{nameof(MainLevel.Instance.m_Tutorial)}:", InfoFieldNameLabel);
                    GUILayout.Label($"{(MainLevel.Instance.m_Tutorial ? "enabled" : "disabled")}", EnabledDisabledInfoFieldValueLabel(InfoFieldValueLabel, MainLevel.Instance.m_Tutorial));
                }

                GUILayout.Label(ModName, ColoredInfoHeaderLabel(InfoHeaderLabel, Color.cyan));

                using (var ngmtScope = new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    string NextGameMode = IsMultiplayerGameModeActive ? "Switch to singleplayer" : "Switch to multiplayer";
                    GUILayout.Label($"{nameof(NextGameMode)}: ", InfoFieldNameLabel);
                    GUILayout.Label(NextGameMode, InfoFieldValueLabel);
                }
                using (var isammodeScope = new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    GUILayout.Label($"{nameof(IsMultiplayerGameModeActive)}: ", InfoFieldNameLabel);
                    GUILayout.Label($"{(IsMultiplayerGameModeActive ? "enabled" : "disabled")}", EnabledDisabledInfoFieldValueLabel(InfoFieldValueLabel, IsMultiplayerGameModeActive));
                }
                using (var aspmodeScope = new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    GUILayout.Label($"{nameof(IsModActiveForSingleplayer)}: ", InfoFieldNameLabel);
                    GUILayout.Label($"{(IsModActiveForSingleplayer ? "enabled" : "disabled")}", EnabledDisabledInfoFieldValueLabel(InfoFieldValueLabel, IsModActiveForSingleplayer));
                }               
                using (var ampmodeScope = new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    GUILayout.Label($"{nameof(IsModActiveForMultiplayer)}", InfoFieldNameLabel);
                    GUILayout.Label($"{(IsModActiveForMultiplayer ? "enabled" : "disabled")}", EnabledDisabledInfoFieldValueLabel(InfoFieldValueLabel, IsModActiveForMultiplayer));
                }                
                using (var permScope = new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    GUILayout.Label($"{nameof(AllowModsAndCheatsForMultiplayer)}:", InfoFieldNameLabel);
                    GUILayout.Label($"{(AllowModsAndCheatsForMultiplayer ? "enabled" : "disabled")}", EnabledDisabledInfoFieldValueLabel(InfoFieldValueLabel, AllowModsAndCheatsForMultiplayer));
                }
                using (var ghdebugmodeScope = new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    GUILayout.Label($"{nameof(GreenHellGame.DEBUG)} Mode: ", InfoFieldNameLabel);
                    GUILayout.Label($"{(GreenHellGame.DEBUG ? "enabled" : "disabled")}", EnabledDisabledInfoFieldValueLabel(InfoFieldValueLabel, GreenHellGame.DEBUG));
                }
                using (var debugmodeScope = new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    GUILayout.Label($"{nameof(EnableDebugMode)}:", InfoFieldNameLabel);
                    GUILayout.Label($"{(EnableDebugMode ? "enabled" : "disabled")}", EnabledDisabledInfoFieldValueLabel(InfoFieldValueLabel, EnableDebugMode));
                }
       
                GUILayout.Label($"{nameof(Cheats)}", ColoredInfoHeaderLabel(InfoHeaderLabel, Color.cyan));

                using (var cheatsOSAIScope = new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    GUILayout.Label($"{nameof(Cheats)}.{nameof(Cheats.m_OneShotAI)}:", InfoFieldNameLabel);
                    GUILayout.Label($"{(Cheats.m_OneShotAI ? "enabled" : "disabled")}", EnabledDisabledInfoFieldValueLabel(InfoFieldValueLabel, Cheats.m_OneShotAI));
                }
                using (var cheatsOSCScope = new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    GUILayout.Label($"{nameof(Cheats)}.{nameof(Cheats.m_OneShotConstructions)}:", InfoFieldNameLabel);
                    GUILayout.Label($"{(Cheats.m_OneShotConstructions ? "enabled" : "disabled")}", EnabledDisabledInfoFieldValueLabel(InfoFieldValueLabel, Cheats.m_OneShotConstructions));
                }
               
                using (var cheatsIBScope = new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    GUILayout.Label($"{nameof(Cheats)}.{nameof(Cheats.m_InstantBuild)}: ", InfoFieldNameLabel);
                    GUILayout.Label($"{(Cheats.m_InstantBuild ? "enabled" : "disabled")}", EnabledDisabledInfoFieldValueLabel(InfoFieldValueLabel, Cheats.m_InstantBuild));
                }
                using (var cheatsGhostModeScope = new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    GUILayout.Label($"{nameof(Cheats)}.{nameof(Cheats.m_GhostMode)}:", InfoFieldNameLabel);
                    GUILayout.Label($"{(Cheats.m_GhostMode ? "enabled" : "disabled")}", EnabledDisabledInfoFieldValueLabel(InfoFieldValueLabel, Cheats.m_GhostMode));
                }
                using (var cheatsGodScope = new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    GUILayout.Label($"{nameof(Cheats)}.{nameof(Cheats.m_GodMode)}: ", InfoFieldNameLabel);
                    GUILayout.Label($"{(Cheats.m_GodMode ? "enabled" : "disabled")}", EnabledDisabledInfoFieldValueLabel(InfoFieldValueLabel, Cheats.m_GodMode));
                }
                using (var cheatsDecayScope = new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    GUILayout.Label($"{nameof(Cheats)}.{nameof(Cheats.m_ImmortalItems)}: ", InfoFieldNameLabel);
                    GUILayout.Label($"{(Cheats.m_ImmortalItems ? "enabled" : "disabled")}", EnabledDisabledInfoFieldValueLabel(InfoFieldValueLabel, Cheats.m_ImmortalItems));
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
            using (var multiplayerinfoScope = new GUILayout.VerticalScope(GUI.skin.box))
            {
                MpInfoScrollViewPosition = GUILayout.BeginScrollView(MpInfoScrollViewPosition, GUI.skin.scrollView, GUILayout.MinHeight(100f));

                GUILayout.Label("Multiplayer info", ColoredInfoHeaderLabel(InfoHeaderLabel, Color.cyan));

                using (var lhdisScope = new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    GUILayout.Label($"{nameof(LocalHostDisplayName)}:", InfoFieldNameLabel);
                    GUILayout.Label($"{LocalHostDisplayName}", InfoFieldValueLabel);
                }
                using (var pctScope = new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    GUILayout.Label($"{nameof(PlayerCount)}:", InfoFieldNameLabel);
                    GUILayout.Label($"{PlayerCount}", InfoFieldValueLabel);
                }
                using (var ihmScope = new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    GUILayout.Label($"{nameof(IsHostManager)}:", InfoFieldNameLabel);
                    GUILayout.Label($"{(IsHostManager ? "enabled" : "disabled")}", EnabledDisabledInfoFieldValueLabel(InfoFieldValueLabel, IsHostManager));
                }               
                using (var ihwpcScope = new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    GUILayout.Label($"{nameof(IsHostWithPlayersInCoop)}:", InfoFieldNameLabel);
                    GUILayout.Label($"{(IsHostWithPlayersInCoop ? "enabled" : "disabled")}", EnabledDisabledInfoFieldValueLabel(InfoFieldValueLabel, IsHostWithPlayersInCoop));
                }               
                using (var amacScope = new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    GUILayout.Label($"{nameof(AllowModsAndCheatsForMultiplayer)}:", InfoFieldNameLabel);
                    GUILayout.Label($"{(AllowModsAndCheatsForMultiplayer ? "enabled" : "disabled")}", EnabledDisabledInfoFieldValueLabel(InfoFieldValueLabel, AllowModsAndCheatsForMultiplayer));
                }               
                using (var hctamdScope = new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    GUILayout.Label($"Host command to allow mods:", InfoFieldNameLabel);
                    GUILayout.Label($"{HostCommandToAllowModsWithRequestId()}", InfoFieldValueLabel);
                }
                using (var edmcScope = new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    GUILayout.Label($"{nameof(EnableDebugMode)}:", InfoFieldNameLabel);
                    GUILayout.Label($"{(EnableDebugMode ? "enabled" : "disabled")}", EnabledDisabledInfoFieldValueLabel(InfoFieldValueLabel, EnableDebugMode));
                }
                using (var hctedScope = new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    GUILayout.Label($"Host command to enable Debug Mode:", InfoFieldNameLabel);
                    GUILayout.Label($"{HostCommandToEnableDebugWithRequestId()}", InfoFieldValueLabel);
                }               

                GUILayout.EndScrollView();
            }
        }

        private void ModInfoBox()
        {
            using (var modinfoScope = new GUILayout.VerticalScope(GUI.skin.box))
            {
                ModInfoScrollViewPosition = GUILayout.BeginScrollView(ModInfoScrollViewPosition, GUI.skin.scrollView, GUILayout.MinHeight(100f));

                GUILayout.Label("Mod info", ColoredInfoHeaderLabel(InfoHeaderLabel, Color.cyan));

                using (var gidScope = new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    GUILayout.Label($"{nameof(ConfigurableMod.GameID)}:", InfoFieldNameLabel);
                    GUILayout.Label($"{SelectedMod.GameID}", InfoFieldValueLabel);
                }
                using (var midScope = new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    GUILayout.Label($"{nameof(ConfigurableMod.ID)}:", InfoFieldNameLabel);
                    GUILayout.Label($"{SelectedMod.ID}", InfoFieldValueLabel);
                }
                using (var uidScope = new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    GUILayout.Label($"{nameof(ConfigurableMod.UniqueID)}:", InfoFieldNameLabel);
                    GUILayout.Label($"{SelectedMod.UniqueID}", InfoFieldValueLabel);
                }
                using (var versionScope = new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    GUILayout.Label($"{nameof(ConfigurableMod.Version)}:", InfoFieldNameLabel);
                    GUILayout.Label($"{SelectedMod.Version}", InfoFieldValueLabel);
                }

                GUILayout.Label("Mod buttons info", ColoredInfoHeaderLabel(InfoHeaderLabel, Color.cyan));

                foreach (var configurableModButton in SelectedMod.ConfigurableModButtons)
                {
                    using (var btnidScope = new GUILayout.HorizontalScope(GUI.skin.box))
                    {
                        GUILayout.Label($"{nameof(ConfigurableModButton.ID)}:", InfoFieldNameLabel);
                        GUILayout.Label($"{configurableModButton.ID}", InfoFieldValueLabel);
                    }
                    using (var btnbindScope = new GUILayout.HorizontalScope(GUI.skin.box))
                    {
                        GUILayout.Label($"{nameof(ConfigurableModButton.KeyBinding)}:", InfoFieldNameLabel);
                        GUILayout.Label($"{configurableModButton.KeyBinding}", InfoFieldValueLabel);
                    }
                }

                GUILayout.EndScrollView();
            }
        }

        private void ScreenMenuBox()
        {
            string collapseButtonText = IsMinimized ? "O" : "-";

            if (GUI.Button(new Rect(ModManagerScreen.width - 40f, 0f, 20f, 20f), collapseButtonText, GUI.skin.button))
            {
                CollapseWindow();
            }
            if (GUI.Button(new Rect(ModManagerScreen.width - 20f, 0f, 20f, 20f), "X", GUI.skin.button))
            {
                ToggleShowUI(0);
            }
        }

        private void MpScreenMenuBox()
        {
            string collapseButtonText = IsMpMinimized ? "O" : "-";

            if (GUI.Button(new Rect(ModMpMngrScreen.width - 40f, 0f, 20f, 20f), collapseButtonText, GUI.skin.button))
            {
                CollapseMpWindow();
            }
            if (GUI.Button(new Rect(ModMpMngrScreen.width - 20f, 0f, 20f, 20f), "X", GUI.skin.button))
            {
                ToggleShowUI(1);
            }
        }

        private void CollapseWindow()
        {
            ModManagerScreenStartPositionX = ModManagerScreen.x;
            ModManagerScreenStartPositionY = ModManagerScreen.y;
            ModManagerScreenTotalWidth = ModManagerScreen.width;

            if (!IsMinimized)
            {
                ModManagerScreen = new Rect(ModManagerScreenStartPositionX, ModManagerScreenStartPositionY, ModManagerScreenTotalWidth, ModManagerScreenMinHeight);
                IsMinimized = true;
            }
            else
            {
                ModManagerScreen = new Rect(ModManagerScreenStartPositionX, ModManagerScreenStartPositionY, ModManagerScreenTotalWidth, ModManagerScreenTotalHeight);
                IsMinimized = false;
            }
            InitModManagerScreen();
        }

        private void CollapseMpWindow()
        {
            ModMpMngrScreenStartPositionX = ModMpMngrScreen.x;
            ModMpMngrScreenStartPositionY = ModMpMngrScreen.y;
            ModMpMngrScreenTotalWidth = ModMpMngrScreen.width;

            if (!IsMpMinimized)
            {
                ModMpMngrScreen = new Rect(ModMpMngrScreenStartPositionX, ModMpMngrScreenStartPositionY, ModMpMngrScreenTotalWidth, ModMpMngrScreenMinHeight);
                IsMpMinimized = true;
            }
            else
            {
                ModMpMngrScreen = new Rect(ModMpMngrScreenStartPositionX, ModMpMngrScreenStartPositionY, ModMpMngrScreenTotalWidth, ModMpMngrScreenTotalHeight);
                IsMpMinimized = false;
            }
            InittModMpMngrScreen();
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
                CloseWindow(false);
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