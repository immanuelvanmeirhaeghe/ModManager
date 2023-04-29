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
            Log.Write(info);
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

        public void SwitchGameMode(bool debug)
        {
            try
            {
                bool _isMultiplayerGameModeActive = IsMultiplayerGameModeActive;
                IsMultiplayerGameModeActive = !IsMultiplayerGameModeActive;

                if (IsMultiplayerGameModeActive && ReplTools.IsCoopEnabled())
                {
                    if (debug)
                    {
                        OnDebug(true);
                    }
                    else
                    {
                        OnSurvival(true);
                    }
                }
                else
                {
                    if (debug)
                    {
                        OnDebug(false);
                    }
                    else
                    {
                        OnPVEwithMaps();
                    }
                }                
            }
            catch (Exception exc)
            {
                HandleException(exc, nameof(SwitchGameMode));
            }
        }

        public void SaveGameOnSwitch()
        {
            try
            {                
                if (IsHostWithPlayersInCoop && ReplTools.CanSaveInCoop())
                {                    
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

        public P2PPeer GetSelectedPeer(string selectedPlayerName)
        {
            return CoopPlayerList?.Find(peer => peer.GetDisplayName().ToLowerInvariant() == selectedPlayerName.ToLowerInvariant());
        }

        public void OnStory()
        {
            P2PSession.Instance.SetGameVisibility(P2PGameVisibility.Singleplayer);
            GreenHellGame.Instance.m_Settings.m_GameVisibility = P2PGameVisibility.Singleplayer;
            MainLevel.Instance.m_GameMode = GameMode.Story;
            GreenHellGame.Instance.m_GHGameMode = GameMode.Story;
            MainLevel.Instance.Initialize();
            MenuInGameManager.Get().HideMenu();
            StartRainforestAmbienceMultisample();
        }

        public void OnStoryCoop()
        {
            MainLevel.Instance.m_GameMode = GameMode.Story;
            GreenHellGame.Instance.m_GHGameMode = GameMode.Story;
            MenuInGameManager.Get().HideMenu();
            if (ReplTools.IsCoopEnabled())
            {
                P2PSession.Instance.Start(null);
            }
            P2PSession.Instance.SetGameVisibility(P2PGameVisibility.Friends);
            GreenHellGame.Instance.m_Settings.m_GameVisibility = P2PGameVisibility.Friends;
            MainLevel.Instance.Initialize();
            StartRainforestAmbienceMultisample();
        }

        public void OnSurvival(bool mp)
        {
            P2PSession.Instance.SetGameVisibility(mp ? P2PGameVisibility.Friends : P2PGameVisibility.Singleplayer);
            GreenHellGame.Instance.m_Settings.m_GameVisibility = (mp ? P2PGameVisibility.Friends : P2PGameVisibility.Singleplayer);
            ScenarioManager.Get().SetSkipTutorial(set: true);
            MainLevel.Instance.m_GameMode = GameMode.Survival;
            GreenHellGame.Instance.m_GHGameMode = GameMode.Survival;
            MainLevel.Instance.Initialize();
            MenuInGameManager.Get().HideMenu();
            StartRainforestAmbienceMultisample();
            if (mp)
            {
                P2PSession.Instance.Start();
            }
        }

        public void OnPVE()
        {
            P2PSession.Instance.SetGameVisibility(P2PGameVisibility.Friends);
            GreenHellGame.Instance.m_Settings.m_GameVisibility = P2PGameVisibility.Friends;
            ScenarioManager.Get().SetSkipTutorial(set: true);
            MainLevel.Instance.m_GameMode = GameMode.PVE;
            GreenHellGame.Instance.m_GHGameMode = GameMode.PVE;
            GreenHellGame.Instance.m_LoadState = GreenHellGame.LoadState.GameLoading;
            MainLevel.Instance.Initialize();
            MenuInGameManager.Get().HideMenu();
            StartRainforestAmbienceMultisample();
        }
              
        public void OnPVEwithMaps()
        {
            P2PSession.Instance.SetGameVisibility(P2PGameVisibility.Friends);
            GreenHellGame.Instance.m_Settings.m_GameVisibility = P2PGameVisibility.Friends;
            ScenarioManager.Get().SetSkipTutorial(set: true);
            MainLevel.Instance.m_GameMode = GameMode.PVE;
            GreenHellGame.Instance.m_GHGameMode = GameMode.PVE;
            GreenHellGame.Instance.LoadScenesFromScript();
            GreenHellGame.Instance.m_LoadState = GreenHellGame.LoadState.GameLoading;
            MainLevel.Instance.Initialize();
            MenuInGameManager.Get().HideMenu();
            StartRainforestAmbienceMultisample();
        }

        public void OnTutorial()
        {
            P2PSession.Instance.SetGameVisibility(P2PGameVisibility.Singleplayer);
            GreenHellGame.Instance.m_Settings.m_GameVisibility = P2PGameVisibility.Singleplayer;
            MainLevel.Instance.m_GameMode = GameMode.Survival;
            GreenHellGame.Instance.m_GHGameMode = GameMode.Survival;
            MainLevel.Instance.Initialize();
            MenuInGameManager.Get().HideMenu();
            StartRainforestAmbienceMultisample();
        }

        public void OnDebug(bool mp)
        {
            P2PSession.Instance.SetGameVisibility(mp ? P2PGameVisibility.Friends : P2PGameVisibility.Singleplayer);
            GreenHellGame.Instance.m_Settings.m_GameVisibility = (mp ? P2PGameVisibility.Friends : P2PGameVisibility.Singleplayer);
            Player.Get().UnlockMap();
            Player.Get().UnlockNotepad();
            Player.Get().UnlockWatch();
            ItemsManager.Get().UnlockAllItemsInNotepad();
            PlayerDiseasesModule.Get().UnlockAllDiseasesInNotepad();
            PlayerDiseasesModule.Get().UnlockAllDiseasesTratmentInNotepad();
            PlayerDiseasesModule.Get().UnlockAllSymptomsInNotepad();
            PlayerDiseasesModule.Get().UnlockAllSymptomTreatmentsInNotepad();
            PlayerInjuryModule.Get().UnlockAllInjuryState();
            PlayerInjuryModule.Get().UnlockAllInjuryStateTreatment();
            MapTab.Get().UnlockAll(achevements_events: false);
            MainLevel.Instance.m_GameMode = GameMode.Debug;
            GreenHellGame.Instance.m_GHGameMode = GameMode.Debug;
            MainLevel.Instance.Initialize();
            MenuInGameManager.Get().HideMenu();
            StartRainforestAmbienceMultisample();
            if (mp)
            {
                P2PSession.Instance.Start();
            }
        }

        public void OnDebugPermaDeath()
        {
            Player.Get().UnlockMap();
            Player.Get().UnlockNotepad();
            Player.Get().UnlockWatch();
            ItemsManager.Get().UnlockAllItemsInNotepad();
            PlayerDiseasesModule.Get().UnlockAllDiseasesInNotepad();
            PlayerDiseasesModule.Get().UnlockAllDiseasesTratmentInNotepad();
            PlayerDiseasesModule.Get().UnlockAllSymptomsInNotepad();
            PlayerDiseasesModule.Get().UnlockAllSymptomTreatmentsInNotepad();
            PlayerInjuryModule.Get().UnlockAllInjuryState();
            PlayerInjuryModule.Get().UnlockAllInjuryStateTreatment();
            MainLevel.Instance.m_GameMode = GameMode.Debug;
            GreenHellGame.Instance.m_GHGameMode = GameMode.None;
            DifficultySettings.SetActivePresetType(DifficultySettings.PresetType.PermaDeath);
            MainLevel.Instance.Initialize();
            MenuInGameManager.Get().HideMenu();
            StartRainforestAmbienceMultisample();
        }

        public void OnResetChallenges()
        {
            ChallengesManager.Get().ResetChallenges();
        }

        public void OnFirecampChallenge()
        {
            OnChallenge("Firecamp");
        }

        public void OnFirecampHardChallenge()
        {
            OnChallenge("FirecampHard");
        }

        public void OnBoatChallenge()
        {
            OnChallenge("Boat");
        }

        public void OnCampChallenge()
        {
            OnChallenge("Camp");
        }

        public void OnTribeRadioChallenge()
        {
            OnChallenge("TribeRadio");
        }

        public void OnTribeRunawayChallenge()
        {
            OnChallenge("TribeRunaway");
        }

        public void OnHunterChallenge()
        {
            OnChallenge("Hunter");
        }

        public void OnChallenge(string name)
        {
            ChallengesManager.Get().m_ChallengeToActivate = name;
            ScenarioManager.Get().SetSkipTutorial(set: true);
            MainLevel.Instance.m_GameMode = GameMode.Survival;
            GreenHellGame.Instance.m_GHGameMode = GameMode.Survival;
            MainLevel.Instance.Initialize();
            MenuInGameManager.Get().HideMenu();
            StartRainforestAmbienceMultisample();
            BalanceSystem20.Get().Initialize();
        }

        private void StartRainforestAmbienceMultisample()
        {
            AmbientAudioSystem.Instance.StartRainForestAmbienceMultisample();
        }

        public void OnDream(int i)
        {
            ScenarioManager.Get().SetSkipTutorial(set: true);
            MainLevel.Instance.m_GameMode = GameMode.Story;
            GreenHellGame.Instance.m_GHGameMode = GameMode.Story;
            MainLevel.Instance.Initialize();
            MenuInGameManager.Get().HideMenu();
            StartRainforestAmbienceMultisample();
            ScenarioManager.Get().SetBoolVariable("ShouldDream_0" + i + "_Start", val: true);
        }

        public void OnDream(string variable)
        {
            ScenarioManager.Get().SetSkipTutorial(set: true);
            MainLevel.Instance.m_GameMode = GameMode.Story;
            GreenHellGame.Instance.m_GHGameMode = GameMode.Story;
            MainLevel.Instance.Initialize();
            MenuInGameManager.Get().HideMenu();
            StartRainforestAmbienceMultisample();
            ScenarioManager.Get().SetBoolVariable(variable, val: true);
        }

        public void OnDebugFromEditorPos()
        {
        }

        public void OnlyTutorial()
        {
            MainLevel.Instance.m_GameMode = GameMode.Story;
            GreenHellGame.Instance.m_GHGameMode = GameMode.Story;
            GreenHellGame.Instance.m_OnlyTutorial = true;
            MainLevel.Instance.Initialize();
            MenuInGameManager.Get().HideMenu();
            StartRainforestAmbienceMultisample();
        }

    }
}
