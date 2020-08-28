/*
 * This code was inspired by Moritz. Thank you!
 * */

namespace ModManager
{
    class HUDTextChatExtended : HUDTextChat
    {
        protected override void OnShow()
        {
            if (!ModManager.RequestInfoShown)
            {
                ModManager.RequestInfoShown = true;
                m_History.StoreMessage(ModManager.ClientSystemInfoChatMessage(ModManager.ClientCommandRequestToUseMods()));
                m_History.StoreMessage(ModManager.ClientSystemInfoChatMessage(ModManager.ClientCommandRequestToUseCheats()));
            }
            base.OnShow();
            ModManager.Disable = true;
        }

        protected override void OnHide()
        {
            base.OnHide();
            ModManager.Disable = false;
        }

        protected override void SendTextMessage()
        {
            string fieldTextMessage = m_Field.text.Trim();

            if (fieldTextMessage.Length > 0)
            {
                if (fieldTextMessage == ModManager.ClientCommandRequestToUseMods())
                {
                    if (string.IsNullOrEmpty(ModManager.RID))
                    {
                        ModManager.SetNewRID();
                    }
                    P2PSession.Instance.SendTextChatMessage(ModManager.HostSystemInfoChatMessage(ModManager.HostCommandToAllowMods(), ModManager.RID));
                    m_History.StoreMessage(ModManager.RequestWasSentMessage(), "");
                }
                else if (fieldTextMessage == ModManager.ClientCommandRequestToUseCheats())
                {
                    if (string.IsNullOrEmpty(ModManager.RID))
                    {
                        ModManager.SetNewRID();
                    }
                    P2PSession.Instance.SendTextChatMessage(ModManager.HostSystemInfoChatMessage(ModManager.HostCommandToAllowCheats(), ModManager.RID));
                    m_History.StoreMessage(ModManager.RequestWasSentMessage(), "");
                }
                else
                {
                    P2PSession.Instance.SendTextChatMessage(fieldTextMessage);
                    if ((bool)m_History)
                    {
                        m_History.StoreMessage(
                                                                           fieldTextMessage,
                                                                           ModManager.GetClientPlayerName(),
                                                                           ReplicatedLogicalPlayer.s_LocalLogicalPlayer ? ReplicatedLogicalPlayer.s_LocalLogicalPlayer.GetPlayerColor() : HUDTextChatHistory.NormalColor
                                                                        );
                    }
                }
            }
        }
    }
}