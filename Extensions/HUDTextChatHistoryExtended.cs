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
            bool isMaster = net_msg.m_Connection.m_Peer.IsMaster();
            if (textMessage == $"{ModManager.HostCommandToAllowMods}{ModManager.RID}")
            {
                if (isMaster)
                {
                    ModManager.AllowModsForMultiplayer = true;
                    ModManager.SetNewRID();
                    StoreMessage(ModManager.PermissionWasGrantedMessage);
                }
                else
                {
                    StoreMessage(ModManager.OnlyHostCanAllowMessage);
                }
            }
            base.OnTextChat(net_msg);
        }
    }
}
