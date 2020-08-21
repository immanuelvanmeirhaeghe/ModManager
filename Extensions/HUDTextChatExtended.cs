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
                m_History.StoreMessage(ModManager.ClientRequestInfoMessage);
                m_History.StoreMessage(ModManager.ClientRequestToUseDebugModeInfoMessage);
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
                if (fieldTextMessage == ModManager.ClientCommandToRequestToUseMods)
                {
                    if (string.IsNullOrEmpty(ModManager.RID))
                    {
                        ModManager.SetNewRID();
                    }
                    P2PSession.Instance.SendTextChatMessage(ModManager.HostRequestToUseMods);
                    m_History.StoreMessage(ModManager.RequestWasSentMessage, "");
                }
                else if (fieldTextMessage == ModManager.ClientCommandToRequestToUseDebugModeMod)
                {
                    if (string.IsNullOrEmpty(ModManager.RID))
                    {
                        ModManager.SetNewRID();
                    }
                    P2PSession.Instance.SendTextChatMessage(ModManager.HostRequestToUseDebugModeMod);
                    m_History.StoreMessage(ModManager.RequestWasSentMessage, "");
                }
                else
                {
                    P2PSession.Instance.SendTextChatMessage(fieldTextMessage);
                    if ((bool)m_History)
                    {
                        m_History.StoreMessage(
                                                                            fieldTextMessage,
                                                                           ModManager.ClientPlayerName,
                                                                            ReplicatedLogicalPlayer.s_LocalLogicalPlayer ? ReplicatedLogicalPlayer.s_LocalLogicalPlayer.GetPlayerColor() : HUDTextChatHistory.NormalColor
                                                                        );
                    }
                }
            }
        }
    }
}