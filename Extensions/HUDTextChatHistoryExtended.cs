namespace ModManager
{
    class HUDTextChatHistoryExtended : HUDTextChatHistory
    {

        protected override void Awake()
        {
            base.Awake();
            ModManager.AllowModsForMultiplayer = false;
            ModManager.RequestInfoShown = false;
        }

        protected override void OnDestroy()
        {
            ModManager.AllowModsForMultiplayer = false;
            ModManager.RequestInfoShown = false;
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
                    ModManager.AllowModsForMultiplayer = true;
                    ModManager.SetNewRID();
                    StoreMessage(ModManager.PermissionWasGrantedMessage());
                }
                else
                {
                    StoreMessage(ModManager.OnlyHostCanAllowMessage());
                }
            }
            else if (textMessage == ModManager.HostCommandToAllowCheatsWithRequestId())
            {
                if (isMaster)
                {
                    ModManager.AllowCheatsForMultiplayer = true;
                    GreenHellGame.DEBUG = true;
                    ModManager.SetNewRID();
                    StoreMessage(ModManager.PermissionWasGrantedMessage());
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
