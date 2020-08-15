/*
 * This code was inspired by Moritz. Thank you!
 * */

using System.Text;
using UnityEngine;

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
            if (fieldTextMessage == ModManager.ClientCommandToRequestToUseMods)
            {
                if (string.IsNullOrEmpty(ModManager.RID))
                {
                    ModManager.SetNewRID();
                }
                P2PSession.Instance.SendTextChatMessage(ModManager.HostRequestMessage);
                m_History.StoreMessage(ModManager.RequestWasSentMessage, "");
            }
            else if (fieldTextMessage.Length > 0)
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