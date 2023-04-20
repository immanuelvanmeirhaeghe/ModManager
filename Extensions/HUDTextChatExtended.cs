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
                GreenHellGame.DEBUG = (ReplTools.AmIMaster() || ModManager.EnableDebugMode) && !ModManager.Disable;
                m_History.StoreMessage(ModManager.ClientSystemInfoChatMessage(ModManager.GetClientCommandToUseMods()));
            }
            base.OnShow();
            ModManager.Disable = true;
        }

        protected override void OnHide()
        {
            base.OnHide();
            ModManager.Disable = false;
            GreenHellGame.DEBUG = (ReplTools.AmIMaster() || ModManager.EnableDebugMode) && !ModManager.Disable;
        }

        protected override void SendTextMessage()
        {
            string fieldTextMessage = m_Field.text.Trim();
            bool flag1 = ModManager.AllowModsAndCheatsForMultiplayer;
            bool flag2 = ModManager.EnableDebugMode;

            if (fieldTextMessage.Length > 0)
            {
                if (fieldTextMessage.ToLower().Trim() == ModManager.GetClientCommandToUseMods().ToLower().Trim() ||
                    fieldTextMessage.ToLower().Trim() == ModManager.GetClientCommandToUseDebugMode().ToLower().Trim())
                {
                    if (flag1 && flag2)
                    {
                        ModManager.SetNewChatRequestId();
                    }

                    if (fieldTextMessage.ToLower().Trim() == ModManager.GetClientCommandToUseMods().ToLower().Trim())
                    {
                        P2PSession.Instance.SendTextChatMessage(ModManager.HostSystemInfoChatMessage(ModManager.GetHostCommandToAllowMods(ModManager.ChatRequestId)));

                        m_History.StoreMessage(ModManager.RequestWasSentMessage());
                    }

                    if (fieldTextMessage.ToLower().Trim() == ModManager.GetClientCommandToUseDebugMode().ToLower().Trim())
                    {
                        P2PSession.Instance.SendTextChatMessage(ModManager.HostSystemInfoChatMessage(ModManager.GetHostCommandToEnableDebug(ModManager.ChatRequestId)));

                        m_History.StoreMessage(ModManager.RequestWasSentMessage());
                    }                                    
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