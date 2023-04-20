﻿namespace ModManager.Extensions
{
    class HUDTextChatHistoryExtended : HUDTextChatHistory
    {
        protected override void Start()
        {
            base.Start();         
        }

        private void InitModManager(bool optionValue)
        {
            ModManager.RequestInfoShown = optionValue;
            ModManager.ToggleModOption(optionValue, nameof(ModManager.RequestInfoShown));

            ModManager.AllowModsAndCheatsForMultiplayer = optionValue;
            ModManager.ToggleModOption(optionValue, nameof(ModManager.AllowModsAndCheatsForMultiplayer));
        }

        protected override void Awake()
        {
            base.Awake();
            InitModManager(false);
        }

        protected override void OnDestroy()
        {
            InitModManager(false);
            base.OnDestroy();
        }

        protected override void OnTextChat(P2PNetworkMessage net_msg)
        {
            string textMessage = net_msg.m_Reader.ReadString();
            P2PPeer l_P2PPeer = net_msg.m_Connection.m_Peer;
            ReplicatedLogicalPlayer playerComponent = ReplicatedPlayerHelpers.GetPlayerComponent<ReplicatedLogicalPlayer>(l_P2PPeer);
            bool isMaster = l_P2PPeer.IsMaster();
            string p2pPeerName = l_P2PPeer.GetDisplayName();

            if (textMessage == ModManager.HostCommandToAllowModsWithRequestId())
            {
                if (isMaster)
                {
                    ModManager.AllowModsAndCheatsForMultiplayer = true;
                    ModManager.ToggleModOption(true, nameof(ModManager.AllowModsAndCheatsForMultiplayer));
                    StoreMessage(ModManager.FlagStateChangedMessage(true, $"Permission to use mods and cheats has been"));
                    ModManager.SetNewChatRequestId();
                }
                else
                {
                    StoreMessage(ModManager.OnlyHostCanAllowMessage());
                }
            }
            else
            {
                StoreMessage(textMessage, p2pPeerName, playerComponent ? playerComponent.GetPlayerColor() : m_NormalColor);
            }
        }
    }
}
