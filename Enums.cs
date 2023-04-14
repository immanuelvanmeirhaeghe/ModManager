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
}
