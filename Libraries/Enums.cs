using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ModManager.Enums
{
    public enum EventID
    {
		None,
		AllowModsValueChanged,
		AllowCheatsValueChanged,
		Count
	}

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
