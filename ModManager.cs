/*
 * This code was inspired by Moritz. Thank you!
 * */
using System;
using System.Collections.Generic;
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
        private static ModManager s_Instance;

        private static readonly string ModName = nameof(ModManager);

        private static HUDManager hUDManager;

        private static Player player;

        private bool showUI;

        public Rect ModManagerScreen = new Rect(10f, 680f, 450f, 150f);

        private static string playerNameToKick = string.Empty;

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

        private static bool optionStateBefore = false;

        public static bool AllowModsForMultiplayer { get; set; } = false;

        public static bool AllowCheatsForMultiplayer { get; set; } = false;

        public static bool Disable { get; set; } = false;

        public static string GetClientCommandRequestToUseMods() => "!requestMods";

        public static string GetHostCommandToAllowMods(string chatRequestId) => $"!allowMods{chatRequestId}";

        public static string GetClientPlayerName() => ReplTools.GetLocalPeer().GetDisplayName();

        public static string GetHostPlayerName() => P2PSession.Instance.GetSessionMaster().GetDisplayName();

        public static string ChatRequestId { get; private set; } = string.Empty;
        public static void SetNewChatRequestId()
        {
            ChatRequestId = UnityEngine.Random.Range(1000, 9999).ToString();
        }

        public static string HostCommandToAllowModsWithRequestId() => GetHostCommandToAllowMods(ChatRequestId);

        public static string ClientSystemInfoChatMessage(string command, Color? color = null) => SystemInfoChatMessage($"Send <b><color=#{(color.HasValue ? ColorUtility.ToHtmlStringRGBA(color.Value) : ColorUtility.ToHtmlStringRGBA(Color.cyan))}>{command}</color></b> to request permission to use mods.");

        public static string HostSystemInfoChatMessage(string command, Color? color = null, Color? subColor = null) => SystemInfoChatMessage($"Hello <b><color=#{(color.HasValue ? ColorUtility.ToHtmlStringRGBA(color.Value) : ColorUtility.ToHtmlStringRGBA(Color.blue))}>{GetHostPlayerName()}</color></b>"
                                                                                                                                                                                                                       + $"\nto enable the use of mods for {GetClientPlayerName()}, send <b><color=#{(subColor.HasValue ? ColorUtility.ToHtmlStringRGBA(subColor.Value) : ColorUtility.ToHtmlStringRGBA(Color.cyan))}>{command}</color></b>"
                                                                                                                                                                                                                       + $"\n<color=#{(color.HasValue ? ColorUtility.ToHtmlStringRGBA(color.Value) : ColorUtility.ToHtmlStringRGBA(Color.blue))}>Be aware that this can be used for griefing!</color>");

        public static string RequestWasSentMessage(Color? color = null) => SystemInfoChatMessage($"<color=#{(color.HasValue ? ColorUtility.ToHtmlStringRGBA(color.Value) : ColorUtility.ToHtmlStringRGBA(Color.green))}>Request sent to host!</color>");

        public static string PermissionWasGrantedMessage(string permission, Color? color = null) => SystemInfoChatMessage($"<color=#{(color.HasValue ? ColorUtility.ToHtmlStringRGBA(color.Value) : ColorUtility.ToHtmlStringRGBA(Color.green))}>Permission {permission} granted!</color>");

        public static string PermissionWasRevokedMessage(string permission, Color? color = null) => SystemInfoChatMessage($"<color=#{(color.HasValue ? ColorUtility.ToHtmlStringRGBA(color.Value) : ColorUtility.ToHtmlStringRGBA(Color.yellow))}>Permission {permission} revoked!</color>");

        public static string OnlyHostCanAllowMessage(Color? color = null) => SystemInfoChatMessage($"<color=#{(color.HasValue ? ColorUtility.ToHtmlStringRGBA(color.Value) : ColorUtility.ToHtmlStringRGBA(Color.yellow))}>Only the host can grant permission!</color>");

        public static string PlayerWasKickedMessage(string playerNameToKick, Color? color = null) => SystemInfoChatMessage($"<color=#{(color.HasValue ? ColorUtility.ToHtmlStringRGBA(color.Value) : ColorUtility.ToHtmlStringRGBA(Color.yellow))}>{playerNameToKick} got kicked from the game!</color>");

        public static string SystemInfoChatMessage(string content, Color? color = null) => $"<color=#{(color.HasValue ? ColorUtility.ToHtmlStringRGBA(color.Value) : ColorUtility.ToHtmlStringRGBA(Color.red))}>System</color>:\n{content}";

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

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Home))
            {
                if (!showUI)
                {
                    InitData();
                    EnableCursor(blockPlayer: true);
                }
                showUI = !showUI;
                if (!showUI)
                {
                    EnableCursor();
                }
            }
        }

        private void OnGUI()
        {
            if (showUI)
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
            showUI = false;
            EnableCursor(false);
        }

        private void InitModManagerScreen(int windowID)
        {
            using (var verticalScope = new GUILayout.VerticalScope($"{ModName}box"))
            {
                if (GUI.Button(new Rect(430f, 0f, 20f, 20f), "X", GUI.skin.button))
                {
                    CloseWindow();
                }

                if (ReplTools.AmIMaster())
                {
                    using (var horizontalScope = new GUILayout.HorizontalScope("modsBox"))
                    {
                        GUILayout.Label("Allow mods for multiplayer? (enabled = yes)", GUI.skin.label);
                        optionStateBefore = AllowModsForMultiplayer;
                        AllowModsForMultiplayer = GUILayout.Toggle(AllowModsForMultiplayer, string.Empty, GUI.skin.toggle);
                        if (optionStateBefore != AllowModsForMultiplayer)
                        {
                            OnToggled(AllowModsForMultiplayer, $"to use mods");
                            CloseWindow();
                        }
                    }
                    using (var horizontalScope = new GUILayout.HorizontalScope("cheatsBox"))
                    {
                        GUILayout.Label("Allow cheats for multiplayer? (enabled = yes)", GUI.skin.label);
                        optionStateBefore = AllowCheatsForMultiplayer;
                        AllowCheatsForMultiplayer = GUILayout.Toggle(AllowCheatsForMultiplayer, string.Empty, GUI.skin.toggle);
                        if (optionStateBefore != AllowCheatsForMultiplayer)
                        {
                            GreenHellGame.DEBUG = (ReplTools.AmIMaster() || AllowCheatsForMultiplayer) && !Disable; ;
                            OnToggled(AllowCheatsForMultiplayer, $"to use cheats");
                            CloseWindow();
                        }
                    }
                    using (var horizontalScope = new GUILayout.HorizontalScope("kickBox"))
                    {
                        GUILayout.Label("Player: ", GUI.skin.label);
                        playerNameToKick = GUILayout.TextField(playerNameToKick, GUI.skin.textField);
                        if (GUILayout.Button("Kick player", GUI.skin.button))
                        {
                            OnClickKickPlayerButton();
                            CloseWindow();
                        }
                    }
                }
                else
                {
                    using (var infoVerticalScope = new GUILayout.VerticalScope($"infoBox"))
                    {
                        GUILayout.Label($"{ModName} UI is only available", GUI.skin.label);
                        GUILayout.Label("for single player or when host.", GUI.skin.label);
                    }
                }
            }

            GUI.DragWindow(new Rect(0f, 0f, 10000f, 10000f));
        }

        private void OnToggled(bool optionValue, string optionText)
        {
            if (optionValue)
            {
                ShowHUDBigInfo(PermissionWasGrantedMessage(optionText), $"{ModName} Info", HUDInfoLogTextureType.Count.ToString());
                P2PSession.Instance.SendTextChatMessage(PermissionWasGrantedMessage(optionText));
            }
            else
            {
                ShowHUDBigInfo(PermissionWasRevokedMessage(optionText), $"{ModName} Info", HUDInfoLogTextureType.Count.ToString());
                P2PSession.Instance.SendTextChatMessage(PermissionWasRevokedMessage(optionText));
            }
        }

        private void OnClickKickPlayerButton()
        {
            try
            {
                P2PPeer playerToKick = ReplTools.GetRemotePeers()?.ToList().Find(peer => peer.GetDisplayName().ToLower() == playerNameToKick.ToLower());
                if (playerToKick != null)
                {
                    FindConnection(playerToKick).Disconnect();
                    ShowHUDBigInfo(PlayerWasKickedMessage(playerNameToKick), $"{ModName} Info", HUDInfoLogTextureType.Count.ToString());
                    P2PSession.Instance.SendTextChatMessage(PlayerWasKickedMessage(playerNameToKick));
                }
            }
            catch (Exception exc)
            {
                ModAPI.Log.Write($"[{ModName}.{ModName}:{nameof(OnClickKickPlayerButton)}] throws exception: {exc.Message}");
            }
        }

        private P2PConnection FindConnection(P2PPeer p2pPeer)
        {
            return ((P2PSessionExtended)P2PSession.Instance).GetPeerConnection(p2pPeer);
        }
    }
}