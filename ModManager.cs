/*
 * This code was inspired by Moritz. Thank you!
 * */
using Enums;
using ModManager.Enums;
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

        private static readonly string ModName = nameof(ModManager);
        private static readonly float ModScreenTotalWidth = 500f;
        private static readonly float ModScreenTotalHeight = 150f;
        private static readonly float ModScreenMinWidth = 450f;
        private static readonly float ModScreenMaxWidth = 500f;
        private static readonly float ModScreenMinHeight = 50f;
        private static readonly float ModScreenMaxHeight = 200f;
        private static float ModScreenStartPositionX { get; set; } = 0f;
        private static float ModScreenStartPositionY { get; set; } = 0f;
        private static bool IsMinimized { get; set; } = false;

        private static CursorManager LocalCursorManager;
        private static HUDManager LocalHUDManager;
        private static Player LocalPlayer;

        private Color DefaultGuiColor = GUI.color;
        private bool ShowUI = false;

        public static Rect ModManagerScreen = new Rect(ModScreenStartPositionX, ModScreenStartPositionY, ModScreenTotalWidth, ModScreenTotalHeight);
        public static GameMode GameModeAtStart;
        public static string SelectedPlayerName { get; set; } = string.Empty;
        public static int SelectedPlayerIndex { get; set; } = 0;

        public static int PlayerCount => P2PSession.Instance.m_RemotePeers.Count;

        public delegate void OnPermissionValueChanged(bool optionValue);
        public static event OnPermissionValueChanged onPermissionValueChanged;
        public delegate void OnOptionToggled(bool optionValue, string optionText);
        public static event OnOptionToggled onOptionToggled;

        public static bool RequestInfoShown { get; set; } = false;
        public static int RequestsSendToHost { get; set; } = 0;
        public static bool AllowModsForMultiplayer { get; set; } = false;
        public static bool AllowModsAndCheatsForMultiplayer { get; set; } = false;
        public static bool IsHostManager
            => ReplTools.AmIMaster();
        public static bool IsHostInCoop
            => ReplTools.IsCoopEnabled();
        public static bool IsHostWithPlayersInCoop
            => IsHostInCoop && P2PSession.Instance.m_RemotePeers != null && P2PSession.Instance.m_RemotePeers.Count > 0;
        public static bool Disable { get; set; } = false;

        public static string GetClientCommandToUseMods()
            => "!requestMods";

        public static string GetHostCommandToAllowMods(string chatRequestId)
            => $"!allowMods{chatRequestId}";

        public static string GetClientPlayerName()
            => ReplTools.GetLocalPeer().GetDisplayName();

        public static string GetHostPlayerName()
            => P2PSession.Instance.GetSessionMaster().GetDisplayName();

        public static string[] GetPlayerNames()
        {
            string[] playerNames = new string[(int)(P2PSession.Instance.m_RemotePeers?.Count)];
            int playerIdx = 0;
            var players = P2PSession.Instance.m_RemotePeers?.ToList();
            foreach (var peer in players)
            {
                playerNames[playerIdx] = peer.GetDisplayName();
                playerIdx++;
            }
            return playerNames;
        }

        public static string ChatRequestId { get; private set; } = string.Empty;

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
                $"<color=#{(color.HasValue ? ColorUtility.ToHtmlStringRGBA(color.Value) : ColorUtility.ToHtmlStringRGBA(Color.yellow))}>{content} { (flagState ? "granted" : "revoked")  }!</color>",
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
            => $"<color=#{ (headcolor != null ? ColorUtility.ToHtmlStringRGBA(headcolor.Value) : ColorUtility.ToHtmlStringRGBA(Color.red))  }>{messageType}</color>\n{message}";

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

        private static readonly string RuntimeConfigurationFile = Path.Combine(Application.dataPath.Replace("GH_Data", "Mods"), "RuntimeConfiguration.xml");
        private static KeyCode ModKeybindingId { get; set; } = KeyCode.Alpha0;
        private KeyCode GetConfigurableKey(string buttonId)
        {
            KeyCode configuredKeyCode = default;
            string configuredKeybinding = string.Empty;

            try
            {
                if (File.Exists(RuntimeConfigurationFile))
                {
                    using (var xmlReader = XmlReader.Create(new StreamReader(RuntimeConfigurationFile)))
                    {
                        while (xmlReader.Read())
                        {
                            if (xmlReader["ID"] == ModName)
                            {
                                if (xmlReader.ReadToFollowing(nameof(Button)) && xmlReader["ID"] == buttonId)
                                {
                                    configuredKeybinding = xmlReader.ReadElementContentAsString();
                                }
                            }
                        }
                    }
                }

                configuredKeybinding = configuredKeybinding?.Replace("NumPad", "Keypad").Replace("Oem", "");

                configuredKeyCode = (KeyCode)(!string.IsNullOrEmpty(configuredKeybinding)
                                                            ? Enum.Parse(typeof(KeyCode), configuredKeybinding)
                                                            : GetType().GetProperty(buttonId)?.GetValue(this));
                return configuredKeyCode;
            }
            catch (Exception exc)
            {
                HandleException(exc, nameof(GetConfigurableKey));
                configuredKeyCode = (KeyCode)(GetType().GetProperty(buttonId)?.GetValue(this));
                return configuredKeyCode;
            }
        }

        private void Start()
        {
            GameModeAtStart = GreenHellGame.Instance.m_GHGameMode;
            onOptionToggled += ModManager_onOptionToggled;
            onPermissionValueChanged += ModManager_onPermissionValueChanged;
            ModKeybindingId = GetConfigurableKey(nameof(ModKeybindingId));
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
                ToggleShowUI();
                if (!ShowUI)
                {
                    EnableCursor(blockPlayer: false);
                }
            }
        }

        private void ToggleShowUI()
        {
            ShowUI = !ShowUI;
        }

        private void OnGUI()
        {
            if (ShowUI)
            {
                InitData();
                InitSkinUI();
                InitWindow();
            }
        }

        private void InitWindow()
        {
            int wid = GetHashCode();
            ModManagerScreen = GUILayout.Window(
                                                wid,
                                                ModManagerScreen,
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

        private void CloseWindow()
        {
            ShowUI = false;
            EnableCursor(false);
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
                }
            }
            GUI.DragWindow(new Rect(0f, 0f, 10000f, 10000f));
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
                KickPlayerBox();
                HostServerBox();
            }
        }

        private void HostServerBox()
        {
            using (var hostsrvScope = new GUILayout.HorizontalScope(GUI.skin.box))
            {
                GUILayout.Label("Save and restart host session with current players", GUI.skin.label);
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
                            OnClickKickPlayerButton();
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
            }
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

        public static void ToggleModOption(bool optionState, string optionName)
        {
            if (optionName == nameof(AllowModsAndCheatsForMultiplayer) && optionState != AllowModsAndCheatsForMultiplayer)
            {
                onOptionToggled?.Invoke(AllowModsAndCheatsForMultiplayer, $"Permission to use mods and cheats has been");
                onPermissionValueChanged?.Invoke(AllowModsAndCheatsForMultiplayer);
            }

            if (optionName == nameof(RequestInfoShown) && optionState != RequestInfoShown)
            {
                onOptionToggled?.Invoke(RequestInfoShown, $"Chat request info was shown on how permission can be");
                RequestsSendToHost = 0;
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
                HandleException(exc, nameof(OnClickKickPlayerButton));
            }
        }

        private void RestartHost()
        {
            try
            {
                ShowHUDBigInfo(HUDBigInfoMessage($"Restarting host...", MessageType.Info, Color.green));
                if (IsHostWithPlayersInCoop)
                {
                    P2PSession.Instance.SendTextChatMessage(SystemInfoServerRestartMessage());
                    SaveGame.SaveCoop();
                    ReloadLobby();
                }
                else
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