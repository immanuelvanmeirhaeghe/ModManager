using System.Reflection;
using UnityEngine;

namespace ModManager
{
    public class ConfigurableModButton
    {
        /// <summary>
        /// xml node Button, attribute ID
        /// </summary>
        public string ID { get; set; } = string.Empty;

        /// <summary>
        /// xml node Button, text content
        /// </summary>
        public string KeyBinding { get; set; } = string.Empty;

        /// <summary>
        /// The input key bound to the mod
        /// based on <see cref="KeyBinding"/>
        /// </summary>
        public KeyCode KeyCode { get; set; } = KeyCode.None;

        /// <summary>
        /// Button description
        /// </summary>
        public string Description { get; set; } = string.Empty;

        public ConfigurableModButton()
        {  }

        public ConfigurableModButton(string buttonId, string keyBinding, string description = "")
        {
            ID = buttonId;
            KeyBinding = keyBinding;
            KeyCode = GetKeyCode(keyBinding);
            Description = description;
        }

        /// <summary>
        /// Get input key for given keybinding
        /// </summary>
        /// <param name="keyBinding"></param>
        /// <returns></returns>
        public static KeyCode GetKeyCode(string keyBinding)
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
                        keyBinding = keyBinding.Replace("NumPad", "Keypad").Replace("Oem", string.Empty);
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