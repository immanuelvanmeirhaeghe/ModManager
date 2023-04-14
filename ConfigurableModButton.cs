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
        public string Keybinding { get; set; } = string.Empty;

        /// <summary>
        /// The input key bound to the mod
        /// based on <see cref="Keybinding"/>
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
            Keybinding = keyBinding;
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
            if (!string.IsNullOrEmpty(keyBinding))
            {
                keyBinding = keyBinding.ToLower().Trim();
                if (keyBinding.StartsWith('d'))
                {
                    keyBinding = keyBinding.Replace("d", "Alpha");
                }
                else
                {
                    keyBinding = keyBinding.Replace("numpad", "Keypad").Replace("oem", string.Empty);
                }
                return EnumUtils<KeyCode>.GetValue(keyBinding);
            }
            return KeyCode.None;
        }

    }
}