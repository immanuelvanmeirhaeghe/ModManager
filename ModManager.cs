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

        private static HUDManager hUDManager;

        private static Player player;

        private bool showUI;
        public Rect ModManagerWindow = new Rect(10f, 680f, 450f, 150f);

        private static string playerNameToKick = string.Empty;

        public ModManager()
        {
			useGUILayout  = true;
            s_Instance = this;
        }

        public static ModManager Get()
        {
            return s_Instance;
        }

        public static void ShowHUDBigInfo(string text, string header, string textureName)
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

        public static void ShowHUDInfoLog(string ItemInfo, string localizedTextKey)
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

        private void ApplyOption(string optionText, bool optionValue)
        {
            if (optionValue)
            {
                ShowHUDBigInfo(PermissionWasGrantedMessage(optionText), $"{nameof(ModManager)} Info", HUDInfoLogTextureType.Count.ToString());
                P2PSession.Instance.SendTextChatMessage(PermissionWasGrantedMessage(optionText));
            }
            else
            {
                ShowHUDBigInfo(PermissionWasRevokedMessage(optionText), $"{nameof(ModManager)} Info", HUDInfoLogTextureType.Count.ToString());
                P2PSession.Instance.SendTextChatMessage(PermissionWasRevokedMessage(optionText));
            }
        }

        private void EnableCursor(bool blockPlayer = false)
        {
            CursorManager.Get().ShowCursor(blockPlayer);
            player = Player.Get();
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
            ModManagerWindow = GUI.Window(0, ModManagerWindow, InitModWindow, $"{nameof(ModManager)}", GUI.skin.window);
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

        private void InitModWindow(int windowId)
        {
            if (GUI.Button(new Rect(440f, 680f, 20f, 20f), "X", GUI.skin.button))
            {
                CloseWindow();
            }

            if (ReplTools.AmIMaster())
            {
                GUI.Label(new Rect(30f, 700f, 200f, 20f), "Allow mods for multiplayer? (enabled = yes)", GUI.skin.label);
                optionStateBefore = AllowModsForMultiplayer;
                AllowModsForMultiplayer = GUI.Toggle(new Rect(280f, 700f, 20f, 20f), AllowModsForMultiplayer, string.Empty, GUI.skin.toggle);
                if (optionStateBefore != AllowModsForMultiplayer)
                {
                    ApplyOption($"to use mods", AllowModsForMultiplayer);
                    CloseWindow();
                }

                GUI.Label(new Rect(30f, 720f, 200f, 20f), "Allow cheats for multiplayer? (enabled = yes)", GUI.skin.label);
                optionStateBefore = AllowCheatsForMultiplayer;
                AllowCheatsForMultiplayer = GUI.Toggle(new Rect(280f, 720f, 20f, 20f), AllowCheatsForMultiplayer, string.Empty, GUI.skin.toggle);
                if (optionStateBefore != AllowCheatsForMultiplayer)
                {
                    GreenHellGame.DEBUG = (ReplTools.AmIMaster() || AllowCheatsForMultiplayer) && !Disable; ;
                    ApplyOption($"to use cheats", AllowCheatsForMultiplayer);
                    CloseWindow();
                }

                GUI.Label(new Rect(30f, 740f, 100f, 20f), "Player: ", GUI.skin.label);
                playerNameToKick = GUI.TextField(new Rect(150f, 740f, 110f, 20f), playerNameToKick, GUI.skin.textField);
                if (GUI.Button(new Rect(280f, 740f, 150f, 20f), "Kick player", GUI.skin.button))
                {
                    OnClickKickPlayerButton();
                    CloseWindow();
                }
            }
            else
            {
                GUI.Label(new Rect(30f, 700f, 330f, 20f), $"{nameof(ModManager)} UI is only available", GUI.skin.label);
                GUI.Label(new Rect(30f, 720f, 330f, 20f), "for single player or when host.", GUI.skin.label);
            }

            GUI.DragWindow(new Rect(0, 0, 10000, 10000));
        }

        private void OnClickKickPlayerButton()
        {
            try
            {
                P2PPeer playerToKick = ReplTools.GetRemotePeers()?.ToList().Find(peer => peer.GetDisplayName().ToLower() == playerNameToKick.ToLower());
                if (playerToKick != null)
                {
                    FindConnection(playerToKick).Disconnect();
                    ShowHUDBigInfo(PlayerWasKickedMessage(playerNameToKick), $"{nameof(ModManager)} Info", HUDInfoLogTextureType.Count.ToString());
                    P2PSession.Instance.SendTextChatMessage(PlayerWasKickedMessage(playerNameToKick));
                }
            }
            catch (Exception exc)
            {
                ModAPI.Log.Write($"[{nameof(ModManager)}.{nameof(ModManager)}:{nameof(OnClickKickPlayerButton)}] throws exception: {exc.Message}");
            }
        }

        private P2PConnection FindConnection(P2PPeer p2pPeer)
        {
            return ((P2PSessionExtended)P2PSession.Instance).GetPeerConnection(p2pPeer);
        }
    }
}