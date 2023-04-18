namespace ModManager.Extensions
{
    class HUDTextChatHistoryExtended : HUDTextChatHistory
    {
        protected override void Start()
        {
            base.Start();         
        }

        private void InitModManager(bool optionValue)
        {
            ModManager.Get().RequestInfoShown = optionValue;
            ModManager.Get().ToggleModOption(optionValue, nameof(ModManager.RequestInfoShown));

            ModManager.Get().AllowModsAndCheatsForMultiplayer = optionValue;
            ModManager.Get().ToggleModOption(optionValue, nameof(ModManager.AllowModsAndCheatsForMultiplayer));
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

            if (textMessage == ModManager.Get().HostCommandToAllowModsWithRequestId())
            {
                if (isMaster)
                {
                    ModManager.Get().AllowModsAndCheatsForMultiplayer = true;
                    ModManager.Get().ToggleModOption(true, nameof(ModManager.AllowModsAndCheatsForMultiplayer));
                    StoreMessage(ModManager.Get().FlagStateChangedMessage(true, $"Permission to use mods and cheats has been"));
                    ModManager.Get().SetNewChatRequestId();
                }
                else
                {
                    StoreMessage(ModManager.Get().OnlyHostCanAllowMessage());
                }
            }
            else
            {
                StoreMessage(textMessage, p2pPeerName, playerComponent ? playerComponent.GetPlayerColor() : m_NormalColor);
            }
        }
    }
}
