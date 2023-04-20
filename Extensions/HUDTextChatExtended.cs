/*
 * This code was inspired by Moritz. Thank you!
 * */

namespace ModManager.Extensions
{
    class HUDTextChatExtended : HUDTextChat
    {   
        protected override void OnShow()
        {
            if (!ModManager.RequestInfoShown)
            {
                ModManager.ToggleModOption(true, nameof(ModManager.RequestInfoShown));
                GreenHellGame.DEBUG = (ReplTools.AmIMaster() || ModManager.AllowModsAndCheatsForMultiplayer) && !ModManager.Disable;
                m_History.StoreMessage(ModManager.Get().ClientSystemInfoChatMessage(ModManager.GetClientCommandToUseMods()));
            }
            base.OnShow();
            ModManager.Disable = true;
        }

        protected override void OnHide()
        {
            base.OnHide();
            ModManager.Disable = false;
            GreenHellGame.DEBUG = (ReplTools.AmIMaster() || ModManager.AllowModsAndCheatsForMultiplayer) && !ModManager.Disable;
        }

        protected override void SendTextMessage()
        {
            string fieldTextMessage = m_Field.text.Trim();

            if (fieldTextMessage.Length > 0)
            {
                if (fieldTextMessage.ToLower().Trim() == ModManager.GetClientCommandToUseMods().ToLower().Trim())
                {
                    ModManager.SetNewChatRequestId();
                    P2PSession.Instance.SendTextChatMessage(ModManager.HostSystemInfoChatMessage(ModManager.GetHostCommandToAllowMods(ModManager.ChatRequestId)));
                    m_History.StoreMessage(ModManager.Get().RequestWasSentMessage());
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