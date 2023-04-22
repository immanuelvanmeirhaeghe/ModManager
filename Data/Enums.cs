using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ModManager.Data.Enums
{
    /// <summary>
    /// Enumerates event identifiers
    /// </summary>
    public enum EventID
    {
        NoneEnabled = 0,
        ModsAndCheatsNotEnabled = 1,
        EnableDebugModeNotEnabled = 2,
        ModsAndCheatsEnabled = 4,
        EnableDebugModeEnabled = 16,
        AllEnabled = 32
    }
    /// <summary>
    /// Enumerates message types
    /// </summary>
    public enum MessageType
    {
        Info,
        Warning,
        Error
    }
    /// <summary>
    /// Enumerates ModAPI supported game identifiers.
    /// </summary>
    public enum GameID
    {
        EscapeThePacific,
        GreenHell,
        SonsOfTheForest,
        Subnautica,
        TheForest,
        TheForestDedicatedServer,
        TheForestVR
    }
        
}
