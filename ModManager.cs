/*
 * This code was inspired by Moritz. Thank you!
 * */
using Enums;
using ModManager.Enums;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;

namespace ModManager
{
    /// <summary>
    /// ModManager is a mod for Green Hell
    /// that aims to be a tool to manage mods in multiplayer.
    /// </summary>
    public class ModManager : MonoBehaviour
    {
        private static ModManager Instance;
        private static readonly string ModName = nameof(ModManager);
        private static readonly float ModScreenTotalWidth = 500f;
        private static readonly float ModScreenTotalHeight = 150f;
        private static readonly float ModScreenMinWidth = 50f;
        private static readonly float ModScreenMaxWidth = 550f;
        private static readonly float ModScreenMinHeight = 50f;
        private static readonly float ModScreenMaxHeight = 200f;
        private static float ModScreenStartPositionX { get; set; } = Screen.width - ModScreenMaxWidth;
        private static float ModScreenStartPositionY { get; set; } = Screen.height - ModScreenMaxHeight;
        private static bool IsMinimized { get; set; } = false;

        private static HUDManager LocalHUDManager;
        private static Player LocalPlayer;

        private bool ShowUI = false;

        public static Rect ModManagerScreen = new Rect(ModScreenStartPositionX, ModScreenStartPositionY, ModScreenTotalWidth, ModScreenTotalHeight);
        public static GameMode GameModeAtStart;
        public static string SelectedPlayerName;
        public static int SelectedPlayerIndex;
        public static int PlayerCount => P2PSession.Instance.m_RemotePeers.Count;

        public delegate void OnPermissionValueChanged(bool optionValue);
        public static event OnPermissionValueChanged onPermissionValueChanged;
        public delegate void OnOptionToggled(bool optionValue, string optionText);
        public static event OnOptionToggled onOptionToggled;

        public static bool RequestInfoShown { get; set; } = false;
        public static int RequestsSendToHost { get; set; } = 0;
        public static bool AllowModsForMultiplayer { get; set; } = false;
        public static bool AllowModsAndCheatsForMultiplayer { get; set; } = false;
        public static bool IsHostManager => ReplTools.AmIMaster();
        public static bool IsHostInCoop => ReplTools.IsCoopEnabled();
        public static bool IsHostWithPlayersInCoop => IsHostInCoop && P2PSession.Instance.m_RemotePeers != null && P2PSession.Instance.m_RemotePeers.Count > 0;
        public static bool Disable { get; set; } = false;

        public static string GetClientCommandRequestToUseMods() => "!requestMods";

        public static string GetHostCommandToAllowMods(string chatRequestId) => $"!allowMods{chatRequestId}";

        public static string GetClientPlayerName() => ReplTools.GetLocalPeer().GetDisplayName();

        public static string GetHostPlayerName() => P2PSession.Instance.GetSessionMaster().GetDisplayName();

        public static string[] GetPlayerNames()
        {
            string[] playerNames = new string[P2PSession.Instance.m_RemotePeers.Count];
            int playerIdx = 0;
            var players = P2PSession.Instance.m_RemotePeers.ToList();
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

        public static string PermissionWasGrantedMessage() => $"Permission granted!";

        public static string PermissionWasRevokedMessage() => $"Permission  revoked!";

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

        public static string PermissionChangedMessage(string permission) => $"Permission to use mods and cheats in multiplayer was {permission}";
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
            CursorManager.Get().ShowCursor(blockPlayer);

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

            if (IsHostWithPlayersInCoop)
            {
                RestartHost();
            }
            else
            {
                SaveGame.Save();
            }

            MainLevel.Instance.Initialize();
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
            if (Input.GetKeyDown(KeyCode.Home))
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
            ModManagerScreen = GUILayout.Window(wid, ModManagerScreen, InitModManagerScreen, $"{ModName}", GUI.skin.window);
        }

        private static void InitData()
        {
            LocalHUDManager = HUDManager.Get();
            LocalPlayer = Player.Get();
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
            using (var modContentScope = new GUILayout.VerticalScope(
                                                                                                                    GUI.skin.box,
                                                                                                                    GUILayout.ExpandWidth(true),
                                                                                                                    GUILayout.MinWidth(ModScreenMinWidth),
                                                                                                                    GUILayout.MaxWidth(ModScreenMaxWidth),
                                                                                                                    GUILayout.ExpandHeight(true),
                                                                                                                    GUILayout.MinHeight(ModScreenMinHeight),
                                                                                                                    GUILayout.MaxHeight(ModScreenMaxHeight)))
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
            using (var horizontalScope = new GUILayout.HorizontalScope(GUI.skin.box))
            {
                GUILayout.Label("Send chat request to host to allow mods (max. 3 to avoid spam.)", GUI.skin.label);
                if (GUILayout.Button(GetClientCommandRequestToUseMods(), GUI.skin.button, GUILayout.MaxWidth(200)))
                {
                    OnClickRequestModsButton();
                }
            }
        }

        private void HostManagerBox()
        {
            ModOptionsBox();
            KickPlayerBox();
            HostServerBox();
        }

        private void HostServerBox()
        {
            using (var horizontalScope = new GUILayout.HorizontalScope(GUI.skin.box))
            {
                GUILayout.Label("Save and restart host session with current players", GUI.skin.label);
                if (GUILayout.Button("Restart", GUI.skin.button, GUILayout.MaxWidth(200)))
                {
                    ShowHUDBigInfo(SystemInfoServerRestartMessage());
                    RestartHost();
                }
            }
        }

        private void KickPlayerBox()
        {
            using (var playerListScope = new GUILayout.VerticalScope(GUI.skin.box, GUILayout.ExpandHeight(true)))
            {
                GUILayout.Label("Select player: ", GUI.skin.label);
                SelectedPlayerIndex = GUILayout.SelectionGrid(SelectedPlayerIndex, GetPlayerNames(), 2, GUI.skin.button);
                if (GUILayout.Button("Kick player", GUI.skin.button, GUILayout.MaxWidth(200)))
                {
                    OnClickKickPlayerButton();
                }
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
                ModScreenStartPositionX = ModManagerScreen.x;
                ModScreenStartPositionY = ModManagerScreen.y;
                ModManagerScreen.Set(ModManagerScreen.x, ModManagerScreen.y, ModScreenMinWidth, ModScreenMinHeight);
                IsMinimized = true;
            }
            else
            {
                ModManagerScreen.Set(ModScreenStartPositionX, ModScreenStartPositionY, ModScreenTotalWidth, ModScreenTotalHeight);
                IsMinimized = false;
            }
            InitWindow();
        }

        private void AllowModsAndCheatsOption()
        {
            bool optionState = AllowModsAndCheatsForMultiplayer;
            AllowModsAndCheatsForMultiplayer = GUILayout.Toggle(AllowModsAndCheatsForMultiplayer, "Allow mods and cheats for multiplayer?", GUI.skin.toggle);
            ToggleModOption(optionState, nameof(AllowModsAndCheatsForMultiplayer));
        }

        private void RequestInfoShownOption()
        {
            bool optionState = RequestInfoShown;
            RequestInfoShown = GUILayout.Toggle(RequestInfoShown, "Chat request info shown?", GUI.skin.toggle);
            ToggleModOption(optionState, nameof(RequestInfoShown));
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
            }
        }

        private void OnClickKickPlayerButton()
        {
            try
            {
                string[] playerNames = GetPlayerNames();
                SelectedPlayerName = playerNames[SelectedPlayerIndex];
                if (!string.IsNullOrEmpty(SelectedPlayerName))
                {
                    P2PPeer peerPlayerToKick = P2PSession.Instance.m_RemotePeers.ToList().Find(peer => peer.GetDisplayName().ToLower() == SelectedPlayerName.ToLower());
                    if (peerPlayerToKick != null)
                    {
                        P2PLobbyMemberInfo playerToKickLobbyMemberInfo = P2PTransportLayer.Instance.GetCurrentLobbyMembers().ToList().Find(lm => lm.m_Address == peerPlayerToKick.m_Address);
                        if (playerToKickLobbyMemberInfo != null)
                        {
                            P2PTransportLayer.Instance.KickLobbyMember(playerToKickLobbyMemberInfo);
                            ShowHUDBigInfo(HUDBigInfoMessage(PlayerWasKickedMessage(SelectedPlayerName), MessageType.Info, Color.green));
                            P2PSession.Instance.SendTextChatMessage(PlayerWasKickedMessage(SelectedPlayerName));
                        }
                    }
                }
            }
            catch (Exception exc)
            {
                ModAPI.Log.Write($"[{ModName}:{nameof(OnClickKickPlayerButton)}] throws exception:\n{exc.Message}");
            }
        }

        private void RestartHost()
        {
            try
            {
                ShowHUDBigInfo(HUDBigInfoMessage($"Restarting host...", MessageType.Info, Color.green));
                if (IsHostInCoop)
                {
                    P2PSession.Instance.SendTextChatMessage(SystemInfoServerRestartMessage());
                    SaveGame.SaveCoop();
                    ReloadLobby();
                }
            }
            catch (Exception exc)
            {
                ModAPI.Log.Write($"[{ModName}:{nameof(RestartHost)}] throws exception:\n{exc.Message}");
            }
        }

        private void ReloadLobby()
        {
            ReadOnlyCollection<P2PLobbyMemberInfo> hostedLobbyMemberInfo = P2PTransportLayer.Instance.GetCurrentLobbyMembers();
            if (hostedLobbyMemberInfo != null)
            {
                P2PSession.Instance.UpdateDefaultRespawnPosition(LocalPlayer.GetWorldPosition());
                P2PSession.Instance.Restart();
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
                P2PSession.Instance.SendTextChatMessage(GetClientCommandRequestToUseMods());
                ShowHUDBigInfo(RequestWasSentMessage());
                RequestsSendToHost++;
            }
            catch (Exception exc)
            {
                ModAPI.Log.Write($"[{ModName}:{nameof(OnClickRequestModsButton)}] throws exception:\n{exc.Message}");
            }
        }
    }
}