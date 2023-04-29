using Enums;
using ModAPI;
using ModManager.Data.Enums;
using ModManager.Data.Interfaces;
using ModManager.Data.Modding;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using UnityEngine;

namespace ModManager.Managers
{
    public class MultiplayerManager : MonoBehaviour
    {
        private static MultiplayerManager Instance;
        private static readonly string ModuleName = nameof(MultiplayerManager);
        public const string MpManagerTitle = "Multiplayer Manager";

        public bool IsModEnabled { get; set; } = true;
        public bool IsHostManager
           => ReplTools.AmIMaster();
        public bool IsHostInCoop
            => IsHostManager && ReplTools.IsCoopEnabled();
        public bool IsHostWithPlayersInCoop
            => IsHostInCoop && !ReplTools.IsPlayingAlone();

        public string LocalHostDisplayName
            =>  P2PSession.Instance.GetSessionMaster().GetDisplayName();
        public string LocalClientPlayerName
            => ReplTools.GetLocalPeer().GetDisplayName();
        public bool IsMultiplayerGameModeActive { get; set; } = false;
        public int PlayerCount => P2PSession.Instance.GetRemotePeerCount();
        public string[] PlayerNames { get; set; } = default;
        public int SelectedPlayerNameIndex { get; set; } = 0;
        public string SelectedPlayerName { get; set; } = string.Empty;
        
        public SessionJoinHelper SessionJoinHelperAtStart { get; set; } = default;
        public bool CanJoinSessionAtStart { get; set; } = true;
        public List<P2PPeer> CoopPlayerList { get; set; } = default;
        public List<P2PLobbyMemberInfo> CoopLobbyMembers { get; set; } = default;
        
        public GameMode GameModeAtStart { get; set; } = GameMode.None;
        public P2PGameVisibility GameVisibilityAtSessionStart { get; set; } = P2PGameVisibility.Private;
        public P2PGameVisibility GameVisibilityAtStart { get; set; } = P2PGameVisibility.Private;

        public MultiplayerManager()
        {
            useGUILayout = true;            
            Instance = this;
        }

        public static MultiplayerManager Get() => Instance;

        private void HandleException(Exception exc, string methodName)
        {
            string info = $"[{ModuleName}:{methodName}] throws exception -  {exc.TargetSite?.Name}:\n{exc.Message}\n{exc.InnerException}\n{exc.Source}\n{exc.StackTrace}";
            ModAPI.Log.Write(info);
            Debug.Log(info);
        }      

        protected virtual void Awake()
        {
            Instance = this;
        }

        protected virtual void OnDestroy()
        {
            Instance = null;
        }

        protected virtual void Start()
        {
            InitData();
        }

        protected virtual void Update()
        {
            if (IsModEnabled)
            {
                InitData();
            }
        }

        protected virtual void InitData()
        {
            InitGameInfo();
            InitMultiplayerInfo();
        }

        private void InitMultiplayerInfo()
        {
            CoopPlayerList = P2PSession.Instance.m_RemotePeers?.ToList();
            PlayerNames = GetPlayerNames();
        }

        protected virtual void InitGameInfo()
        {
            GameModeAtStart = GreenHellGame.Instance.m_GHGameMode;
            GameVisibilityAtSessionStart = P2PSession.Instance.GetGameVisibility();
            GameVisibilityAtStart = GreenHellGame.Instance.m_Settings.m_GameVisibility;
            IsMultiplayerGameModeActive = GameVisibilityAtSessionStart != P2PGameVisibility.Singleplayer && GameVisibilityAtStart != P2PGameVisibility.Singleplayer;
            SessionJoinHelperAtStart = GreenHellGame.Instance.m_SessionJoinHelper;
            CanJoinSessionAtStart = MainLevel.Instance.m_CanJoinSession;           
        }

        public P2PLobbyMemberInfo GetPlayerLobbyMemberInfo(P2PPeer peerPlayerToKick)
        {
            return CoopLobbyMembers.Find(lm => lm.m_Address == peerPlayerToKick.m_Address);
        }

        public string[] GetPlayerNames()
        {
            string localPeerDisplayName = P2PSession.Instance.LocalPeer.GetDisplayName();
            string[] playerNames = default;
            if (IsHostWithPlayersInCoop && CoopPlayerList != null)
            {
                playerNames = new string[CoopPlayerList.Count + 1];
                playerNames[0] = localPeerDisplayName;
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
                playerNames[0] = localPeerDisplayName;
                return playerNames;
            }
            return playerNames;
        }

        public bool KickPlayer(string name)
        {
            P2PPeer peerPlayerToKick = CoopPlayerList.Find(peer => peer.GetDisplayName().ToLowerInvariant() == name.ToLowerInvariant());
            if (peerPlayerToKick != null)
            {
                P2PLobbyMemberInfo playerToKickLobbyMemberInfo = GetPlayerLobbyMemberInfo(peerPlayerToKick);
                if (playerToKickLobbyMemberInfo != null)
                {
                    P2PTransportLayer.Instance.KickLobbyMember(playerToKickLobbyMemberInfo);
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
            return true;
        }

        public void SwitchGameMode()
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
                    MainLevel.Instance.m_GameMode =MultiplayerManager.Get().GameModeAtStart;
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

        public P2PPeer GetSelectedPeer(string selectedPlayerName)
        {
            return CoopPlayerList?.Find(peer => peer.GetDisplayName().ToLowerInvariant() == selectedPlayerName.ToLowerInvariant());
        }
    }
}
