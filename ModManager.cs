/*
 * This code was inspired by Moritz. Thank you!
 * */
using Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using DebugMode;
using System.Reflection;

namespace ModManager
{
    /// <summary>
    /// ModManager is a mod for Green Hell
    /// that aims to be a tool to manage mods in multiplayer.
    /// </summary>
    public class ModManager : MonoBehaviour
    {
        private static ModManager s_Instance;

        private static readonly string ModName = nameof(ModManager);

        private static HUDManager hUDManager;

        private static Player player;

        private bool ShowUI;

        private static GameMode GameModeAtStart;

        private static string SelectedPlayerName = string.Empty;
        private static int SelectedPlayerIndex = 0;
        private static int PlayerCount => P2PSession.Instance.m_RemotePeers.Count;

        public static Rect ModManagerScreen = new Rect(Screen.width / 25f, Screen.height / 25f, 450f, 150f);

        public delegate void OnPermissionValueChanged(bool optionValue);

        public static event OnPermissionValueChanged onPermissionValueChanged;

        public delegate void OnOptionToggled(bool optionValue, string optionText);

        public static event OnOptionToggled onOptionToggled;

        public ModManager()
        {
            useGUILayout = true;
            s_Instance = this;
        }

        public static ModManager Get()
        {
            return s_Instance;
        }

        public void ShowHUDBigInfo(string text, string header, string textureName)
        {
            HUDBigInfo obj = (HUDBigInfo)hUDManager.GetHUD(typeof(HUDBigInfo));
            HUDBigInfoData data = new HUDBigInfoData
            {
                m_Header = header,
                m_Text = text,
                m_TextureName = textureName,
                m_ShowTime = Time.time
            };
            obj.AddInfo(data);
            obj.Show(show: true);
        }

        public void ShowHUDInfoLog(string ItemInfo, string localizedTextKey)
        {
            Localization localization = GreenHellGame.Instance.GetLocalization();
            ((HUDMessages)hUDManager.GetHUD(typeof(HUDMessages))).AddMessage(localization.Get(localizedTextKey) + "  " + localization.Get(ItemInfo));
        }

        public static bool RequestInfoShown { get; set; } = false;

        public static int RequestsSendToHost { get; set; } = 0;

        private static bool optionState;

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

        private void EnableCursor(bool blockPlayer = false)
        {
            CursorManager.Get().ShowCursor(blockPlayer);

            if (blockPlayer)
            {
                player.BlockMoves();
                player.BlockRotation();
                player.BlockInspection();
            }
            else
            {
                player.UnblockMoves();
                player.UnblockRotation();
                player.UnblockInspection();
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
            MainLevel.Instance.Initialize();
        }

        private void ModManager_onOptionToggled(bool optionValue, string optionText)
        {
            ShowHUDBigInfo(
                FlagStateChangedMessage(optionValue, optionText),
                $"{ModName} Info",
                HUDInfoLogTextureType.Count.ToString()
                );

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
            hUDManager = HUDManager.Get();
            player = Player.Get();
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
            using (var verticalScope = new GUILayout.VerticalScope(GUI.skin.box))
            {
                ScreenToolbox(ModManagerScreen.width);

                if (IsHostManager)
                {
                    using (var vertical2Scope = new GUILayout.VerticalScope(GUI.skin.box))
                    {
                        AllowModsAndCheatsOption();
                        RequestInfoShownStateOptionButton();
                    }
                    using (var horizontalScope = new GUILayout.HorizontalScope(GUI.skin.box))
                    {
                        KickPlayerButton();
                    }
                    using (var horizontalScope = new GUILayout.HorizontalScope(GUI.skin.box))
                    {
                        RestartServerButton();
                    }
                }
                else
                {
                    using (var horizontalScope = new GUILayout.HorizontalScope(GUI.skin.box))
                    {
                        ClientRequestButton();
                    }
                }
            }
            GUI.DragWindow(new Rect(0f, 0f, 10000f, 10000f));
        }

        private void ScreenToolbox(float x)
        {
            if (GUI.Button(new Rect(x - 20f, 0f, 20f, 20f), "X", GUI.skin.button))
            {
                CloseWindow();
            }
        }

        private void ClientRequestButton()
        {
            GUILayout.Label("Send chat request to host to allow mods (max. 3 to avoid spam.)", GUI.skin.label);
            if (GUILayout.Button(GetClientCommandRequestToUseMods(), GUI.skin.button))
            {
                OnClickRequestModsButton();
                CloseWindow();
            }
        }

        private void RestartServerButton()
        {
            GUILayout.Label("Save and restart host session with current players", GUI.skin.label);
            if (GUILayout.Button("Restart", GUI.skin.button))
            {
                OnClickRestartButton();
                CloseWindow();
            }
        }

        private void KickPlayerButton()
        {
            GUILayout.Label("Select player: ", GUI.skin.label);
            SelectedPlayerIndex = GUILayout.SelectionGrid(SelectedPlayerIndex, GetPlayerNames(), 2, GUI.skin.button);
            if (GUILayout.Button("Kick player", GUI.skin.button))
            {
                OnClickKickPlayerButton();
            }
        }

        private void AllowModsAndCheatsOption()
        {
            optionState = AllowModsAndCheatsForMultiplayer;
            AllowModsAndCheatsForMultiplayer = GUILayout.Toggle(AllowModsAndCheatsForMultiplayer, "Allow mods and cheats for multiplayer?", GUI.skin.toggle);
            ToggleModOption(optionState, nameof(AllowModsAndCheatsForMultiplayer));
        }

        private void RequestInfoShownStateOptionButton()
        {
            optionState = RequestInfoShown;
            RequestInfoShown = GUILayout.Toggle(RequestInfoShown, "Chat request info shown?", GUI.skin.toggle);
            ToggleModOption(optionState, nameof(RequestInfoShown));
        }

        public static void ToggleModOption(bool state, string optionName)
        {
            if (optionName == nameof(AllowModsAndCheatsForMultiplayer) && state != AllowModsAndCheatsForMultiplayer)
            {
                onOptionToggled?.Invoke(AllowModsAndCheatsForMultiplayer, $"Permission to use mods and cheats has been");
                onPermissionValueChanged?.Invoke(AllowModsAndCheatsForMultiplayer);
                AllowModsAndCheatsForMultiplayer = state;
            }

            if (optionName == nameof(RequestInfoShown) && state != RequestInfoShown)
            {
                onOptionToggled?.Invoke(RequestInfoShown, $"Chat request info was shown on how permission can be");
                RequestInfoShown = state;
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
                            ShowHUDBigInfo(PlayerWasKickedMessage(SelectedPlayerName), $"{ModName} Info", HUDInfoLogTextureType.Count.ToString());
                            P2PSession.Instance.SendTextChatMessage(PlayerWasKickedMessage(SelectedPlayerName));
                        }
                    }
                }
            }
            catch (Exception exc)
            {
                ModAPI.Log.Write($"[{ModName}.{ModName}:{nameof(OnClickKickPlayerButton)}] throws exception: {exc.Message}");
            }
        }

        private void OnClickRestartButton()
        {
            try
            {
                if (ReplTools.IsCoopEnabled())
                {
                    ShowHUDBigInfo(SystemInfoServerRestartMessage(), $"{ModName} Info", HUDInfoLogTextureType.Count.ToString());
                    P2PSession.Instance.SendTextChatMessage(SystemInfoServerRestartMessage());
                    SaveGame.SaveCoop();
                    List<P2PPeer> players = P2PSession.Instance.m_RemotePeers.ToList();
                    P2PSession.Instance.Restart();

                    if (players != null && players.Count > 0)
                    {
                        foreach (P2PPeer peerPlayer in players)
                        {
                            ShowHUDBigInfo($"Reconnecting player {peerPlayer.GetDisplayName()}...", $"{ModName} Info", HUDInfoLogTextureType.Count.ToString());
                            P2PSession.Instance.JoinLobby(peerPlayer.m_Address);
                        }
                        P2PTransportLayer.Instance.OpenSystemOverlay(P2PSystemOverlay.Players);
                    }
                }
            }
            catch (Exception exc)
            {
                ModAPI.Log.Write($"[{ModName}.{ModName}:{nameof(OnClickRestartButton)}] throws exception: {exc.Message}");
            }
        }

        private void OnClickRequestModsButton()
        {
            try
            {
                if (RequestsSendToHost >= 3)
                {
                    ShowHUDBigInfo(MaximumRequestsSendMessage(3), $"{ModName} Info", HUDInfoLogTextureType.Count.ToString());
                    return;
                }
                P2PSession.Instance.SendTextChatMessage(GetClientCommandRequestToUseMods());
                ShowHUDBigInfo(RequestWasSentMessage(), $"{ModName} Info", HUDInfoLogTextureType.Count.ToString());
                RequestsSendToHost++;
            }
            catch (Exception exc)
            {
                ModAPI.Log.Write($"[{ModName}.{ModName}:{nameof(OnClickRequestModsButton)}] throws exception: {exc.Message}");
            }
        }

    }
}