namespace ModManager.Extensions
{
    class HUDTextChatHistoryExtended : HUDTextChatHistory
    {
        private static readonly ModManager LocalModManager = ModManager.Get();

        protected override void Start()
        {
            base.Start();         
        }

        private void InitModManager(bool optionValue)
        {
            LocalModManager.RequestInfoShown = optionValue;
            LocalModManager.ToggleModOption(optionValue, nameof(LocalModManager.RequestInfoShown));

            LocalModManager.AllowModsAndCheatsForMultiplayer = optionValue;
            LocalModManager.ToggleModOption(optionValue, nameof(LocalModManager.AllowModsAndCheatsForMultiplayer));
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

            if (textMessage == LocalModManager.HostCommandToAllowModsWithRequestId())
            {
                if (isMaster)
                {
                    LocalModManager.AllowModsAndCheatsForMultiplayer = true;
                    LocalModManager.ToggleModOption(true, nameof(LocalModManager.AllowModsAndCheatsForMultiplayer));
                    StoreMessage(LocalModManager.FlagStateChangedMessage(true, $"Permission to use mods and cheats has been"));
                    LocalModManager.SetNewChatRequestId();
                }
                else
                {
                    StoreMessage(LocalModManager.OnlyHostCanAllowMessage());
                }
            }
            else
            {
                StoreMessage(textMessage, p2pPeerName, playerComponent ? playerComponent.GetPlayerColor() : m_NormalColor);
            }
        }
    }
}
