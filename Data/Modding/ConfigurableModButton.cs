using ModManager.Data.Interfaces;
using System.Reflection;
using UnityEngine;

namespace ModManager.Data.Modding
{
    /// <summary>
    /// Represents a base class
    /// for a configurable mod button
    /// created using ModAPI.
    /// </summary>
    public class ConfigurableModButton : IConfigurableModButton
    {
        public string ID { get; set; } = string.Empty;

        public string KeyBinding { get; set; } = string.Empty;

        public KeyCode ShortcutKey { get; set; } = KeyCode.None;

        public string Description { get; set; } = string.Empty;

        public ConfigurableModButton()
        { }

        public ConfigurableModButton(string buttonId, string keyBinding, string description = "")
        {
            ID = buttonId;
            KeyBinding = keyBinding;
            ShortcutKey = GetKeyCode(keyBinding);
            Description = description;
        }

        public KeyCode GetKeyCode(string keyBinding)
        {
            try
            {
                if (!string.IsNullOrEmpty(keyBinding))
                {
                    keyBinding = keyBinding.Trim();
                    if (keyBinding.StartsWith('D'))
                    {
                        keyBinding = keyBinding.Replace("D", "Alpha");
                    }
                    else
                    {
                        keyBinding = keyBinding.Replace("Subtract", "KeypadMinus");
                        keyBinding = keyBinding.Replace("NumPad", "Keypad");
                        keyBinding = keyBinding.Replace("Oem", string.Empty);
                    }
                    return EnumUtils<KeyCode>.GetValue(keyBinding);
                }
                return KeyCode.None;
            }
            catch (System.Exception ex)
            {
                string info = $"[{nameof(ConfigurableModButton)}:{nameof(GetKeyCode)}] throws exception:\n{ex.Message}";
                ModAPI.Log.Write(info);
                Debug.Log(info);
                return KeyCode.None;
            }
        }

    }
}