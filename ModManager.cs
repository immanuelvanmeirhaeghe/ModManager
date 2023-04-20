using Enums;
using ModManager.Data;
using ModManager.Data.Enums;
using ModManager.Data.Interfaces;
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
        private const string MpManagerText = "Multiplayer Manager";
        private static ModManager Instance;
        private static readonly string RuntimeConfiguration = Path.Combine(Application.dataPath.Replace("GH_Data", "Mods"), $"{nameof(RuntimeConfiguration)}.xml");
      
        private static readonly string ModName = nameof(ModManager);
        private static readonly float ModScreenTotalWidth = 500f;
        private static readonly float ModScreenTotalHeight = 500f;
        private static readonly float ModScreenMinWidth = 500f;
        private static readonly float ModScreenMaxWidth = Screen.width;
        private static readonly float ModScreenMinHeight = 50f;
        private static readonly float ModScreenMaxHeight = Screen.height;
        private static float ModScreenStartPositionX { get; set; } = Screen.width / 2f;
        private static float ModScreenStartPositionY { get; set; } = Screen.height / 2f;
        private static float ModMpScreenStartPositionX { get; set; } = Screen.width / 2.5f;
        private static float ModMpScreenStartPositionY { get; set; } = Screen.height / 2.5f;       
        private static bool IsMinimized { get; set; } = false;

        private static CursorManager LocalCursorManager;
        private static HUDManager LocalHUDManager;
        private static Player LocalPlayer;
       
        private static MainMenuManager LocalMainMenuManager;

        private Color DefaultColor = GUI.color;

        private bool ShowUI = false;
        private bool ShowGameInfo = false;
        private bool ShowMpInfo = false;
        private bool ShowMpMngr = false;
        private bool ShowModInfo = false;
        private bool ShowInfo = false;

        public static Rect ModManagerScreen = new Rect(ModScreenStartPositionX, ModScreenStartPositionY, ModScreenTotalWidth, ModScreenTotalHeight);
        public static Rect ModMpMngrScreen = new Rect(ModMpScreenStartPositionX, ModMpScreenStartPositionY, ModScreenTotalWidth, ModScreenTotalHeight);
        public static KeyCode ShortcutKey { get; set; } = KeyCode.Alpha0;
        public static List<IConfigurableMod> ConfigurableModList { get; set; } = new List<IConfigurableMod>();
        public static List<P2PPeer> CoopPlayerList { get; set; } = default;
       
        public static int ChatRequestId { get; set; } 
        public string LocalHostDisplayName { get; set; } = string.Empty;
        public static GameMode GameModeAtStart { get; set; } = GameMode.None;
        public string SteamAppId  => GreenHellGame.s_SteamAppId.m_AppId.ToString();
        public int PlayerCount => P2PSession.Instance.GetRemotePeerCount();

        public delegate void OnPermissionValueChanged(bool optionValue);
        public static event OnPermissionValueChanged onPermissionValueChanged;
        public delegate void OnOptionToggled(bool optionValue, string optionText);
        public static event OnOptionToggled onOptionToggled;

        public string TextChatMessage { get; set; } = string.Empty;

        public Vector2 PlayerListScrollViewPosition { get; set; } = default;
        public string SelectedPlayerName { get; set; } = string.Empty;
        public int SelectedPlayerIndex { get; set; } = 0;

        public Vector2 ModListScrollViewPosition { get; set; } = default;
        public int SelectedModIDIndex { get; set; } = 0;
        public string SelectedModID { get; set; } = string.Empty;
        public IConfigurableMod SelectedMod { get; set; } = default;
        public static bool GameModeSwitched { get; set; } = false;
        public static bool RequestInfoShown { get; set; } = false;
        public static int RequestsSendToHost { get; set; } = 0;

        public static bool EnableDebugMode { get; set; } = false;
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
        public static List<P2PLobbyMemberInfo> CoopLobbyMembers { get; set; }
        public static SessionJoinHelper SessionJoinHelperAtStart { get; set; }
        public static bool CanJoinSessionAtStart { get; set; }

        public static string GetClientCommandToUseMods()
            => "!requestMods";

        public static string GetClientCommandToUseDebugMode()
            => "!requestDebug";

        public static string GetHostCommandToAllowMods(float chatRequestId)
            => $"!allowMods{chatRequestId}";

        public static string GetHostCommandToEnableDebug(float chatRequestId)
         => $"!allowDebug{chatRequestId}";

        public static string GetClientPlayerName()
            => ReplTools.GetLocalPeer().GetDisplayName();

        public static string GetHostPlayerName()
            => P2PSession.Instance.GetSessionMaster().GetDisplayName();

        public static void SetNewChatRequestId()
        {
            ChatRequestId = Mathf.FloorToInt(UnityEngine.Random.Range(0f,9999f));
        }

        public static string HostCommandToAllowModsWithRequestId()
            => GetHostCommandToAllowMods(ChatRequestId);

        public static string HostCommandToEnableDebugWithRequestId()
            => GetHostCommandToEnableDebug(ChatRequestId);

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
            if (!optionValue)
            {
                Cheats.m_OneShotAI = optionValue;
                Cheats.m_OneShotConstructions = optionValue;
                Cheats.m_GhostMode = optionValue;
                Cheats.m_GodMode = optionValue;
                Cheats.m_ImmortalItems = optionValue;
            }
        }

        private void ModManager_onOptionToggled(bool optionValue, string optionText)
        {
            ShowHUDBigInfo(FlagStateChangedMessage(optionValue, optionText));
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

            if (Input.GetKey(KeyCode.Escape) && Input.GetKeyDown(ShortcutKey))
            {
                CloseWindow();
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
                case 5:
                    ShowInfo = !ShowInfo;
                    return;
                default:
                    ShowUI = !ShowUI;
                    ShowMpMngr = !ShowMpMngr;
                    ShowGameInfo = !ShowGameInfo;
                    ShowMpInfo = !ShowMpInfo;
                    ShowModInfo = !ShowModInfo;
                    ShowInfo = !ShowInfo;
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
            if (ShowMpMngr)
            {
                ModMpMngrScreen = GUILayout.Window(
                    GetHashCode(),
                    ModMpMngrScreen,
                    InitMpMngrWindow,
                    $"{ModName} - {MpManagerText}",
                    GUI.skin.window,
                    GUILayout.ExpandWidth(true),
                    GUILayout.MinWidth(ModScreenMinWidth),
                    GUILayout.MaxWidth(ModScreenMaxWidth),
                    GUILayout.ExpandHeight(true),
                    GUILayout.MinHeight(ModScreenMinHeight),
                    GUILayout.MaxHeight(ModScreenMaxHeight));
            }
        }

        private void InitData()
        {
            LocalHUDManager = HUDManager.Get();
            LocalPlayer = Player.Get();
            LocalCursorManager = CursorManager.Get();         
            LocalMainMenuManager = MainMenuManager.Get();
            CoopPlayerList = P2PSession.Instance.m_RemotePeers?.ToList();
            
        }

        private void InitSkinUI()
        {
            GUI.skin = ModAPI.Interface.Skin;
        }

        private void CloseWindow()
        {
            ShowUI = false;
            ShowMpMngr = false;
        
            EnableCursor(false);
        }

        private void PlayerListScrollView()
        {            
            PlayerListScrollViewPosition = GUILayout.BeginScrollView(PlayerListScrollViewPosition, GUI.skin.scrollView, GUILayout.MinHeight(100f));
            int _SelectedPlayerIndex = SelectedPlayerIndex;
            string[] playerNames = GetPlayerNames();
            if (playerNames != null)
            {             
                SelectedPlayerIndex = GUILayout.SelectionGrid(SelectedPlayerIndex, playerNames, 3, GUI.skin.button);
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
                    AllModsListBox();                   
                }
            }
            GUI.DragWindow(new Rect(0f, 0f, 10000f, 10000f));
        }

        private void InitMpMngrWindow(int windowID)
        {
            ModMpScreenStartPositionX = ModMpMngrScreen.x;
            ModMpScreenStartPositionY = ModMpMngrScreen.y;

            using (var modplayersScope = new GUILayout.VerticalScope(GUI.skin.box))
            {
                if (GUILayout.Button("Multiplayer Info", GUI.skin.button))
                {
                    ToggleShowUI(3);
                }
                if (ShowMpInfo)
                {
                    MultiplayerInfoBox();
                }
                PlayersScrollViewBox();
                SendTexMessagesBox();
                MpActionButtons();
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
                    ShowMpMngr = false;
                }
            }
        }

        private void PlayersScrollViewBox()
        {
            using (var pScrollViewScope = new GUILayout.VerticalScope(GUI.skin.box))
            {
                GUILayout.Label($"All players currently in game:", GUI.skin.label);
                PlayerListScrollView();
            }
        }

        private void SendTexMessagesBox()
        {
            using (var textmsgViewScope = new GUILayout.VerticalScope(GUI.skin.box))
            {
                GUILayout.Label($"Type in your message to send to player.", GUI.skin.label);
                TextChatMessage = GUILayout.TextArea(TextChatMessage, GUI.skin.textArea);
            }
        }

        private void OnClickKickPlayerButton()
        {
            try
            {
                if (!string.IsNullOrEmpty(SelectedPlayerName) && CoopPlayerList != null)
                {
                    P2PPeer peerPlayerToKick = CoopPlayerList.Find(peer => peer.GetDisplayName().ToLower() == SelectedPlayerName.ToLower());
                    if (peerPlayerToKick != null)
                    {
                        P2PLobbyMemberInfo playerToKickLobbyMemberInfo = GetPlayerToKickLobbyMemberInfo(peerPlayerToKick);
                        if (playerToKickLobbyMemberInfo != null)
                        {
                            P2PTransportLayer.Instance.KickLobbyMember(playerToKickLobbyMemberInfo);
                            ShowHUDBigInfo(HUDBigInfoMessage(PlayerWasKickedMessage(SelectedPlayerName), MessageType.Info, Color.green));
                            P2PSession.Instance.SendTextChatMessage(PlayerWasKickedMessage(SelectedPlayerName));
                        }
                    }
                }
                else
                {
                    GUILayout.Label($"Please select a player first.", GUI.skin.label);
                }
            }
            catch (Exception exc)
            {
                HandleException(exc, nameof(OnClickKickPlayerButton));
            }
        }

        private static P2PLobbyMemberInfo GetPlayerToKickLobbyMemberInfo(P2PPeer peerPlayerToKick)
        {
            return CoopLobbyMembers.Find(lm => lm.m_Address == peerPlayerToKick.m_Address);
        }

        private void OnClickSendMessageButton()
        {
            try
            {
                if (!string.IsNullOrEmpty(SelectedPlayerName) && CoopPlayerList != null)
                {
                    P2PPeer peerPlayerToChat = CoopPlayerList.Find(peer => peer.GetDisplayName().ToLower() == SelectedPlayerName.ToLower());
                    if (peerPlayerToChat != null)
                    {
                        P2PLobbyMemberInfo playerToChatLobbyMemberInfo = GetPlayerToChatLobbyMemberInfo(peerPlayerToChat);
                        if (playerToChatLobbyMemberInfo != null)
                        {
                            P2PSession.Instance.SendTextChatMessage($"From: {GetHostPlayerName()} \n To {SelectedPlayerName}: \n" + TextChatMessage);
                        }
                    }
                }
                else
                {
                    GUILayout.Label($"Please select a player first.", GUI.skin.label);
                }
            }
            catch (Exception exc)
            {
                HandleException(exc, nameof(OnClickSendMessageButton));
            }
        }

        private static P2PLobbyMemberInfo GetPlayerToChatLobbyMemberInfo(P2PPeer peerPlayerToChat)
        {
            return CoopLobbyMembers.Find(lm => lm.m_Address == peerPlayerToChat.m_Address);
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
                GUI.color = Color.cyan;
                GUILayout.Label($"ModAPI mod list:", GUI.skin.label);
                GUI.color = DefaultColor;

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

        public string[] GetPlayerNames()
        {
            string[] playerNames = default;
            if (IsHostWithPlayersInCoop && CoopPlayerList != null)
            {
                playerNames = new string[CoopPlayerList.Count];
                int playerIdx = 0;
                
                foreach (var peer in CoopPlayerList)
                {
                    playerNames[playerIdx] = peer.GetDisplayName();
                    playerIdx++;
                }
                return playerNames;
            }
            if(IsHostManager)
            {
                playerNames = new string[1];
                playerNames[0] = P2PSession.Instance.LocalPeer.GetDisplayName();
                return playerNames;
            }
            return playerNames;
        }

        private void ClientManagerBox()
        {
            using (var clientmngScope = new GUILayout.VerticalScope(GUI.skin.box))
            {
                GUI.color = Color.yellow;
                GUILayout.Label("Client Manager: ", GUI.skin.label);
                GUI.color = DefaultColor;

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
                GUI.color = Color.yellow;
                GUILayout.Label("Host Manager: ", GUI.skin.label);

                ModOptionsBox();

                if (GUILayout.Button(MpManagerText, GUI.skin.button))
                {
                   ShowMpMngr = true;
                }
            }
        }

        private void ModOptionsBox()
        {      
            using (var optionsScope = new GUILayout.VerticalScope(GUI.skin.box))
            {
                GUI.color = Color.cyan;
                GUILayout.Label($"{ModName} options:", GUI.skin.label);
                GUI.color = DefaultColor;

                if (GUILayout.Button("Mod Info", GUI.skin.button))
                {
                    ToggleShowUI(5);
                    SelectedMod = ConfigurableModList.Find(cfgMod => cfgMod.ID == ModName);
                }
                if (ShowInfo && SelectedMod != null)
                {
                    ModInfoBox();
                }
                else
                {
                    GUILayout.Label($"Could not get info for {ModName}.", GUI.skin.label);
                }

                AllowModsAndCheatsOption();
                if (AllowModsAndCheatsForMultiplayer)
                {
                    CheatOptionsBox();
                }

                EnableDebugModeOption();

                RequestInfoShownOption();
                
                SwitchPlayerVersusModeOption();
                
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

        private void CheatOptionsBox()
        {
            using (var optscheatsHScope = new GUILayout.VerticalScope(GUI.skin.box))
            {
                GUI.color = Color.cyan;
                GUILayout.Label($"Cheat options:", GUI.skin.label);
                GUI.color = DefaultColor;

                Cheats.m_OneShotAI = GUILayout.Toggle(Cheats.m_OneShotAI, "One shot AI cheat on / off?", GUI.skin.toggle);
                Cheats.m_OneShotConstructions = GUILayout.Toggle(Cheats.m_OneShotAI, "One shot AI cheat on / off?", GUI.skin.toggle);
                Cheats.m_GhostMode = GUILayout.Toggle(Cheats.m_GhostMode, "Ghost mode cheat on / off?", GUI.skin.toggle);
                Cheats.m_GodMode = GUILayout.Toggle(Cheats.m_GodMode, "God mode cheat on / off?", GUI.skin.toggle);
                Cheats.m_ImmortalItems = GUILayout.Toggle(Cheats.m_ImmortalItems, "No item decay cheat on / off?", GUI.skin.toggle);
            }
        }

        private void GameInfoBox()
        {
            using (var gameinfoScope = new GUILayout.VerticalScope(GUI.skin.box))
            {
                GUI.color = Color.cyan;
                GUILayout.Label("Game info:", GUI.skin.label);
                GUI.color = DefaultColor;

                GUILayout.Label($"{nameof(SteamAppId)}: {SteamAppId}", GUI.skin.label);
                bool IsAnyCheatEnabled = Cheats.m_GhostMode || Cheats.m_OneShotConstructions || Cheats.m_InstantBuild || Cheats.m_GodMode;
                GUILayout.Label($"{nameof(Cheats)}: {(IsAnyCheatEnabled ? "enabled" : "disabled")}", GUI.skin.label);
                GUILayout.Label($"{nameof(GreenHellGame.DEBUG)} Mode: {(GreenHellGame.DEBUG ? "enabled" : "disabled")}", GUI.skin.label);
                GUILayout.Label($"{nameof(GameMode)}: {GreenHellGame.Instance.m_GHGameMode}", GUI.skin.label);
                GUILayout.Label($"{nameof(P2PGameVisibility)}: {GreenHellGame.Instance.m_Settings.m_GameVisibility}", GUI.skin.label);
                GUILayout.Label($"{nameof(IsModActiveForSingleplayer)}: {(IsModActiveForSingleplayer ? "enabled" : "disabled")}", GUI.skin.label);
                GUILayout.Label($"{nameof(IsModActiveForMultiplayer)}: {(IsModActiveForMultiplayer ? "enabled" : "disabled")}", GUI.skin.label);               
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
                GUI.color = Color.cyan;
                GUILayout.Label("Multiplayer info:", GUI.skin.label);
                GUI.color = DefaultColor;

                GUILayout.Label($"{nameof(LocalHostDisplayName)}: {LocalHostDisplayName}", GUI.skin.label);
                GUILayout.Label($"{nameof(PlayerCount)}: {PlayerCount}", GUI.skin.label);
                GUILayout.Label($"{nameof(IsHostManager)}: { (IsHostManager ? "enabled" : "disabled"  )}", GUI.skin.label);
                GUILayout.Label($"{nameof(IsHostWithPlayersInCoop)}: {( IsHostWithPlayersInCoop ? "enabled" : "disabled")}", GUI.skin.label);
                GUILayout.Label($"{nameof(AllowModsAndCheatsForMultiplayer)}: {(AllowModsAndCheatsForMultiplayer ? "enabled" : "disabled")}", GUI.skin.label);                
                GUILayout.Label($"Host command to allow mods for multiplayer: {HostCommandToAllowModsWithRequestId()}", GUI.skin.label);
                GUILayout.Label($"{nameof(EnableDebugMode)}: {(EnableDebugMode ? "enabled" : "disabled")}", GUI.skin.label);
                GUILayout.Label($"Host command to enable Debug Mode for multiplayer: {HostCommandToEnableDebugWithRequestId()}", GUI.skin.label);
            }
        }

        private void ModInfoBox()
        {
            using (var modinfoScope = new GUILayout.VerticalScope(GUI.skin.box))
            {
                GUI.color = Color.cyan;
                GUILayout.Label("Mod info:", GUI.skin.label);
                GUI.color = DefaultColor;

                using (var gidScope = new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    GUILayout.Label($"{nameof(ConfigurableMod.GameID)}:", GUI.skin.label);
                    GUILayout.Label($"{SelectedMod.GameID}", GUI.skin.label);
                }
                using (var midScope = new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    GUILayout.Label($"{nameof(ConfigurableMod.ID)}:", GUI.skin.label);
                    GUILayout.Label($"{SelectedMod.ID}", GUI.skin.label);
                }
                using (var uidScope = new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    GUILayout.Label($"{nameof(ConfigurableMod.UniqueID)}:", GUI.skin.label);
                    GUILayout.Label($"{SelectedMod.UniqueID}", GUI.skin.label);
                }
                using (var versionScope = new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    GUILayout.Label($"{nameof(ConfigurableMod.Version)}:", GUI.skin.label);
                    GUILayout.Label($"{SelectedMod.Version}", GUI.skin.label);
                }
                GUI.color = Color.cyan;
                GUILayout.Label("Mod buttons info: ", GUI.skin.label);
                GUI.color = DefaultColor;
                foreach (var configurableModButton in SelectedMod.ConfigurableModButtons)
                {
                    using (var btnidScope = new GUILayout.HorizontalScope(GUI.skin.box))
                    {
                        GUILayout.Label($"{nameof(ConfigurableModButton.ID)}:", GUI.skin.label);
                        GUILayout.Label($"{configurableModButton.ID}", GUI.skin.label);
                    }
                    using (var btnbindScope = new GUILayout.HorizontalScope(GUI.skin.box))
                    {
                        GUILayout.Label($"{nameof(ConfigurableModButton.KeyBinding)}:", GUI.skin.label);
                        GUILayout.Label($"{configurableModButton.KeyBinding}", GUI.skin.label);
                    }
                }
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
                CloseWindow();
            }
        }

        private void CollapseWindow()
        {
            ModScreenStartPositionX = ModManagerScreen.x;
            ModScreenStartPositionY = ModManagerScreen.y;

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

        private void EnableDebugModeOption()
        {
            bool _EnableDebugMode = EnableDebugMode;
            EnableDebugMode = GUILayout.Toggle(EnableDebugMode, "Enable Debug Mode for multiplayer?", GUI.skin.toggle);
            ToggleModOption(_EnableDebugMode, nameof(EnableDebugMode));
        }

        private void RequestInfoShownOption()
        {
            bool _requestInfoShownValue = RequestInfoShown;
            RequestInfoShown = GUILayout.Toggle(RequestInfoShown, "Chat request info shown?", GUI.skin.toggle);
            ToggleModOption(_requestInfoShownValue, nameof(RequestInfoShown));
        }

        private void SwitchPlayerVersusModeOption()
        {
            bool _switchPlayerVersusModeValue = GameModeSwitched;
            GameModeSwitched = GUILayout.Toggle(GameModeSwitched, "Switch to PvP?", GUI.skin.toggle);
            ToggleModOption(_switchPlayerVersusModeValue, nameof(GameModeSwitched));
            if (_switchPlayerVersusModeValue != GameModeSwitched)
            {
                ShowConfirmSwitchGameModeDialog();
            }          
        }

        public static void ToggleModOption(bool optionState, string optionName)
        {
            if (optionName == nameof(EnableDebugMode) && optionState != EnableDebugMode)
            {
                if (optionState)
                {
                    GreenHellGame.DEBUG = true;
                    GreenHellGame.Instance.m_GHGameMode = GameMode.Debug;
                    MainLevel.Instance.m_GameMode = GameMode.Debug;
                }
                else
                {
                    GreenHellGame.DEBUG = false;
                    GreenHellGame.Instance.m_GHGameMode = GameModeAtStart;
                    MainLevel.Instance.m_GameMode = GameModeAtStart;
                }
                onOptionToggled?.Invoke(EnableDebugMode, $"Using Debug Mode has been");               
            }

            if (optionName == nameof(AllowModsAndCheatsForMultiplayer) && optionState != AllowModsAndCheatsForMultiplayer)
            {
                Instance.IsModActiveForMultiplayer = optionState;
                onOptionToggled?.Invoke(AllowModsAndCheatsForMultiplayer, $"Using mods and cheats has been");
                onPermissionValueChanged?.Invoke(AllowModsAndCheatsForMultiplayer);
            }

            if (optionName == nameof(RequestInfoShown) && optionState != RequestInfoShown)
            {
                onOptionToggled?.Invoke(RequestInfoShown, $"Chat request info was shown on how mods and cheats can be");
                RequestsSendToHost = 0;
            }

            if (optionName == nameof(GameModeSwitched) && optionState != GameModeSwitched)
            {
                onOptionToggled?.Invoke(GameModeSwitched, $"Multiplayer mode has been");
            }
        }

        private static void SwitchGameMode()
        {
            try
            {
                GreenHellGame.Instance.m_Settings.m_GameVisibility = GameModeSwitched == true ? P2PGameVisibility.Friends : P2PGameVisibility.Singleplayer;
                if (GameModeSwitched && ReplTools.IsCoopEnabled())
                {
                    GreenHellGame.Instance.m_SessionJoinHelper = SessionJoinHelperAtStart ?? new SessionJoinHelper();
                    GreenHellGame.Instance.m_GHGameMode = GameMode.PVE;

                    MainLevel.Instance.m_CanJoinSession = CanJoinSessionAtStart;
                    MainLevel.Instance.m_Tutorial = false;
                    MainLevel.Instance.m_GameMode = GameMode.PVE;

                    P2PSession.Instance.SetGameVisibility(P2PGameVisibility.Friends);
                    P2PSession.Instance.Start();

                    MainLevel.Instance.StartLevel();
                }
                else
                {
                    GreenHellGame.Instance.m_SessionJoinHelper = SessionJoinHelperAtStart ?? new SessionJoinHelper();
                    GreenHellGame.Instance.m_GHGameMode = GameModeAtStart;

                    MainLevel.Instance.m_CanJoinSession = CanJoinSessionAtStart;
                    MainLevel.Instance.m_Tutorial = false;
                    MainLevel.Instance.m_GameMode = GameModeAtStart;

                    P2PSession.Instance.SetGameVisibility(P2PGameVisibility.Singleplayer);
                    P2PSession.Instance.Start(null);

                    MainLevel.Instance.StartLevel();
                }               
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
                EnableCursor(true);
                string description = $"Are you sure you want to switch to  {(GameModeSwitched == true ? "multiplayer?  Your current game will first be saved, if possible.\n" : "singleplayer? Your current game and of coop players' games will first be saved, if possible.\n")}\n";
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