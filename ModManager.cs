/*
 * This code was inspired by Moritz. Thank you!
 * */
using UnityEngine;

namespace ModManager
{
    /// <summary>
    /// ModManager
    /// </summary>
    public class ModManager : MonoBehaviour
    {
        private static ModManager s_Instance;

        public ModManager()
        {
            s_Instance = this;
        }

        public static ModManager Get()
        {
            return s_Instance;
        }

        public static string RID = string.Empty;

        public static bool RequestInfoShown = false;

        public static bool AllowModsForMultiplayer = false;

        public static string ClientCommandToRequestToUseDebugModeMod => "!requestCheats";

        public static string ClientCommandToRequestToUseMods => "!requestMods";

        public static string HostCommandToAllowMods => "!allowMods";

        public static string ClientRequestInfoMessage => $"<color =#ff3b3b>System</color>:"
                                                                                   + $"\nSend: <b><color=#36ff68>{ClientCommandToRequestToUseMods}</color></b>"
                                                                                   + $"\nto request permission to use mods.";

        public static void SetNewRID()
        {
            RID = Random.Range(1000, 9999).ToString();
        }

        public static string ClientPlayerName => ReplTools.GetLocalPeer().GetDisplayName();

        public static string HostPlayerName => P2PSession.Instance.GetSessionMaster().GetDisplayName();

        public static string HostRequestMessage => $"Hello <b><color=#03c6fc>{HostPlayerName}</color></b>"
                                                                                           + $"\nto enable the use of mods for {ClientPlayerName}, send:"
                                                                                           + $"\n<b><color=#36ff68>{HostCommandToAllowMods}{RID}</color></b>"
                                                                                           + $"\n<color=#ff3b3b>Be aware that this can be used for griefing!</color>";

        public static string RequestWasSentMessage => $"<color=#ff3b3b>System</color>: <b><color=#36ff68>Request sent!</color>";

        public static string PermissionWasGrantedMessage => $"<color=#ff3b3b>System</color>: <b><color=#36ff68>Permission granted!</color></b>";

        public static string OnlyHostCanAllowMessage => $"<color=#ff3b3b>System</color>: <color=#36ff68>Only the host can grant permission!</color>";

        public static bool Disable = false;
    }
}