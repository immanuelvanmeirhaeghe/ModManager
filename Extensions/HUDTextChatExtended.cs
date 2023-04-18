/*
 * This code was inspired by Moritz. Thank you!
 * */

namespace ModManager.Extensions
{
    class HUDTextChatExtended : HUDTextChat
    {   
        protected override void OnShow()
        {
            if (!ModManager.Get().RequestInfoShown)
            {
                ModManager.Get().ToggleModOption(true, nameof(ModManager.RequestInfoShown));
                m_History.StoreMessage(ModManager.Get().ClientSystemInfoChatMessage(ModManager.Get().GetClientCommandToUseMods()));
            }
            base.OnShow();
            ModManager.Get().Disable = true;
        }

        protected override void OnHide()
        {
            base.OnHide();
            ModManager.Get().Disable = false;
        }

        protected override void SendTextMessage()
        {
            string fieldTextMessage = m_Field.text.Trim();

            if (fieldTextMessage.Length > 0)
            {
                if (fieldTextMessage == ModManager.Get().GetClientCommandToUseMods())
                {
                    ModManager.Get().SetNewChatRequestId();
                    P2PSession.Instance.SendTextChatMessage(ModManager.Get().HostSystemInfoChatMessage(ModManager.Get().GetHostCommandToAllowMods(ModManager.Get().ChatRequestId)));
                    m_History.StoreMessage(ModManager.Get().RequestWasSentMessage());
                }
                else
                {
                    P2PSession.Instance.SendTextChatMessage(fieldTextMessage);
                    if ((bool)m_History)
                    {
                        m_History.StoreMessage(
                                                                           fieldTextMessage,
                                                                           ModManager.Get().GetClientPlayerName(),
                                                                           ReplicatedLogicalPlayer.s_LocalLogicalPlayer ? ReplicatedLogicalPlayer.s_LocalLogicalPlayer.GetPlayerColor() : HUDTextChatHistory.NormalColor
                                                                        );
                    }
                }
            }
        }
    }
}