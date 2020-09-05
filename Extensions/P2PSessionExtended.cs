using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace ModManager
{
    class P2PSessionExtended : P2PSession
    {
        public P2PConnection GetPeerConnection(P2PPeer peer)
        {
            return m_Connections.Find(connection => connection.m_Peer == peer);
        }
    }
}
