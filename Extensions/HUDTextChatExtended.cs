/*
 * This code was inspired by Moritz. Thank you!
 * */

namespace ModManager.Extensions
{
    class HUDTextChatExtended : HUDTextChat
    {
        private static readonly ModManager LocalModManager = ModManager.Get();
        
        protected override void OnShow()
        {
            if (!LocalModManager.RequestInfoShown)
            {
                LocalModManager.ToggleModOption(true, nameof(LocalModManager.RequestInfoShown));
                m_History.StoreMessage(LocalModManager.ClientSystemInfoChatMessage(LocalModManager.GetClientCommandToUseMods()));
            }
            base.OnShow();
            LocalModManager.Disable = true;
        }

        protected override void OnHide()
        {
            base.OnHide();
            LocalModManager.Disable = false;
        }

        protected override void SendTextMessage()
        {
            string fieldTextMessage = m_Field.text.Trim();

            if (fieldTextMessage.Length > 0)
            {
                if (fieldTextMessage == LocalModManager.GetClientCommandToUseMods())
                {
                    LocalModManager.SetNewChatRequestId();
                    P2PSession.Instance.SendTextChatMessage(LocalModManager.HostSystemInfoChatMessage(LocalModManager.GetHostCommandToAllowMods(LocalModManager.ChatRequestId)));
                    m_History.StoreMessage(LocalModManager.RequestWasSentMessage());
                }
                else
                {
                    P2PSession.Instance.SendTextChatMessage(fieldTextMessage);
                    if ((bool)m_History)
                    {
                        m_History.StoreMessage(
                                                                           fieldTextMessage,
                                                                           LocalModManager.GetClientPlayerName(),
                                                                           ReplicatedLogicalPlayer.s_LocalLogicalPlayer ? ReplicatedLogicalPlayer.s_LocalLogicalPlayer.GetPlayerColor() : HUDTextChatHistory.NormalColor
                                                                        );
                    }
                }
            }
        }
    }
}