/*
 * This code was inspired by Moritz. Thank you!
 * */
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

        private static Player player;

        private bool showUI;

        public ModManager()
        {
            s_Instance = this;
        }

        public static ModManager Get()
        {
            return s_Instance;
        }

        public static bool RequestInfoShown { get; set; } = false;

        private static void OnToggled(bool elem)
        {
            if (elem)
            {
                P2PSession.Instance.SendTextChatMessage(PermissionWasGrantedMessage());
            }
            else
            {
                P2PSession.Instance.SendTextChatMessage(PermissionWasRevokedMessage());
            }
        }

        public static bool AllowModsForMultiplayer { get; set; } = false;

        public static bool AllowCheatsForMultiplayer { get; set; } = false;

        public static bool Disable { get; set; } = false;

        public static string ClientCommandRequestToUseCheats() => "!requestCheats";

        public static string HostCommandToAllowCheats() => "!allowCheats";

        public static string ClientCommandRequestToUseMods() => "!requestMods";

        public static string HostCommandToAllowMods() => "!allowMods";

        public static string GetClientPlayerName() => ReplTools.GetLocalPeer().GetDisplayName();

        public static string GetHostPlayerName() => P2PSession.Instance.GetSessionMaster().GetDisplayName();

        public static string RID { get; private set; } = string.Empty;
        public static void SetNewRID()
        {
            RID = Random.Range(1000, 9999).ToString();
        }

        public static string HostCommandToAllowCheatsWithRequestId() => $"{HostCommandToAllowCheats()}{RID}";

        public static string HostCommandToAllowModsWithRequestId() => $"{HostCommandToAllowMods()}{RID}";

        public static string ClientSystemInfoChatMessage(string command) => SystemInfoChatMessage($"Send <b><color=#36ff68>{command}</color></b> to request permission to use modAPI.");

        public static string HostSystemInfoChatMessage(string command, string requestId) => SystemInfoChatMessage($"Hello <b><color=#03c6fc>{GetHostPlayerName()}</color></b>"
                                                                                                                                                                                                                       + $"\nto enable the use of mods for {GetClientPlayerName()}, send <b><color=#36ff68>{command}{requestId}</color></b>"
                                                                                                                                                                                                                       + $"\n<color=#ff3b3b>Be aware that this can be used for griefing!</color>");

        public static string RequestWasSentMessage() => SystemInfoChatMessage("<color=#36ff68>Request sent!</color>");

        public static string PermissionWasGrantedMessage() => SystemInfoChatMessage("<color=#36ff68>Permission to use mods granted!</color>");

        public static string PermissionWasRevokedMessage() => SystemInfoChatMessage("<color=#36ff68>Permission to use mods revoked!</color>");

        public static string OnlyHostCanAllowMessage() => SystemInfoChatMessage("<color=#36ff68>Only the host can grant permission!</color>");

        public static string SystemInfoChatMessage(string content) => $"<color=#ff3b3b>System</color>:\n{content}";

        private static void EnableCursor(bool enabled = false)
        {
            CursorManager.Get().ShowCursor(enabled);
            player = Player.Get();
            if (enabled)
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
                    EnableCursor(enabled: true);
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
                InitModUI();
            }
        }

        private static void InitData()
        {
            player = Player.Get();
        }

        private static void InitSkinUI()
        {
            GUI.skin = ModAPI.Interface.Skin;
        }

        private void InitModUI()
        {
            GUI.Box(new Rect(10f, 680f, 450f, 150f), "ModManager UI - Press HOME to open/close", GUI.skin.window);
            if (GUI.Button(new Rect(440f, 680f, 20f, 20f), "X", GUI.skin.button))
            {
                showUI = false;
                EnableCursor(false);
            }
            if (ReplTools.AmIMaster())
            {
                GUI.Label(new Rect(30f, 700f, 200f, 20f), "Allow mods for multiplayer? (enabled = yes)", GUI.skin.label);
                AllowModsForMultiplayer = GUI.Toggle(new Rect(280f, 700f, 20f, 20f), AllowModsForMultiplayer, "");                
                OnToggled(AllowModsForMultiplayer);

                GUI.Label(new Rect(30f, 720f, 200f, 20f), "Allow cheats for multiplayer? (enabled = yes)", GUI.skin.label);
                AllowCheatsForMultiplayer = GUI.Toggle(new Rect(280f, 720f, 20f, 20f), AllowCheatsForMultiplayer, "");
                OnToggled(AllowCheatsForMultiplayer);
            }
            else
            {
                GUI.Label(new Rect(30f, 700f, 330f, 20f), "This manager UI is only visible", GUI.skin.label);
                GUI.Label(new Rect(30f, 720f, 330f, 20f), "for single player or when host", GUI.skin.label);
            }
        }

    }
}