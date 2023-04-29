using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ModManager.Managers
{

    public class StylingManager : MonoBehaviour
    {
        private static StylingManager Instance;
        private static readonly string ModuleName = nameof(StylingManager);

        public bool IsModEnabled { get; set; } = true;

        public StylingManager()
        {
            useGUILayout = true;
            Instance = this;
        }

        public static StylingManager Get() => Instance;

        protected virtual void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            Instance = null;
        }

        protected virtual void Start()
        {
            InitData();
        }

        protected virtual void Update()
        {
            if (IsModEnabled)
            {
                InitData();
            }
        }

        protected virtual void InitData()
        { }

        private void HandleException(Exception exc, string methodName)
        {
            string info = $"[{ModuleName}:{methodName}] throws exception -  {exc.TargetSite?.Name}:\n{exc.Message}\n{exc.InnerException}\n{exc.Source}\n{exc.StackTrace}";
            ModAPI.Log.Write(info);
            Debug.Log(info);
        }

        public Color DefaultColor = GUI.color;
        public Color DefaultContentColor = GUI.contentColor;
        public Color DefaultBackGroundColor = GUI.backgroundColor;
        public GUIStyle SelectedGridButton => new GUIStyle(GUI.skin.button)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = GUI.skin.button.fontSize,
            border = GUI.skin.button.border,
            font = GUI.skin.button.font,
            clipping = GUI.skin.button.clipping,
            margin = GUI.skin.button.margin,
            padding = GUI.skin.button.padding,
            contentOffset = GUI.skin.button.contentOffset,
            active = GUI.skin.button.active,
           stretchWidth = true
        };
        public GUIStyle WindowBox => new GUIStyle(GUI.skin.box)
        {
            stretchWidth = true,
            stretchHeight = true,
            wordWrap = true
        };
        public GUIStyle HeaderLabel => new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold,
            fontSize = 16,
            stretchWidth = true,
            wordWrap = true
        };
        public GUIStyle SubHeaderLabel => new GUIStyle(GUI.skin.label)
        {
            alignment = HeaderLabel.alignment,
            fontStyle = HeaderLabel.fontStyle,
            fontSize = HeaderLabel.fontSize - 2,
            stretchWidth = true,
            wordWrap = true
        };
        public GUIStyle FormFieldNameLabel => new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleLeft,
            fontSize = 12,
            stretchWidth = true,
            wordWrap = true
        };
        public GUIStyle FormFieldValueLabel => new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleRight,
            fontSize = 12,
            stretchWidth = true,
            wordWrap = true
        };
        public GUIStyle FormInputTextField => new GUIStyle(GUI.skin.textField)
        {
            alignment = TextAnchor.MiddleRight,
            fontSize = 12,
            stretchWidth = true,
            wordWrap = true
        };
        public GUIStyle CommentLabel => new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleLeft,
            fontStyle = FontStyle.Italic,
            fontSize = 12,
            stretchWidth = true,
            wordWrap = true
        };
        public GUIStyle TextLabel => new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleLeft,
            fontSize = 12,
            stretchWidth = true,
            wordWrap = true
        };
        public GUIStyle ToggleButton => new GUIStyle(GUI.skin.toggle)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 12,
            stretchWidth = true
        };
        public GUIStyle ColoredSelectedGridButton(bool isSelectedIndex)
        {
            GUIStyle style = SelectedGridButton;

            style.normal.textColor = isSelectedIndex ? Color.cyan : DefaultColor;
            style.onNormal.textColor = isSelectedIndex ? Color.cyan : DefaultColor;            
            style.hover.textColor = isSelectedIndex ? Color.cyan : DefaultColor;
            style.onHover.textColor = isSelectedIndex ? Color.cyan : DefaultColor;

            return style;
        }
        public GUIStyle ColoredToggleValueTextLabel(bool enabled, Color enabledColor, Color disabledColor)
        {
            GUIStyle style = TextLabel;
            style.normal.textColor = enabled ? enabledColor : disabledColor;
            return style;
        }
        public GUIStyle ColoredToggleButton(bool activated, Color enabledColor, Color disabledColor)
        {
            GUIStyle style = ToggleButton;

            style.normal.textColor = activated ? enabledColor : disabledColor;
            style.onNormal.textColor = activated ? enabledColor : disabledColor;
            style.hover.textColor = activated ? enabledColor : disabledColor;
            style.onHover.textColor = activated ? enabledColor : disabledColor;

            return style;
        }
        public GUIStyle ColoredCommentLabel(Color color)
        {
            GUIStyle style = CommentLabel;
            style.normal.textColor = color;
            return style;
        }
        public GUIStyle ColoredFieldNameLabel(Color color)
        {
            GUIStyle style = FormFieldNameLabel;
            style.normal.textColor = color;
            return style;
        }
        public GUIStyle ColoredFieldValueLabel(Color color)
        {
            GUIStyle style = FormFieldValueLabel;
            style.normal.textColor = color;
            return style;
        }
        public GUIStyle ColoredToggleFieldValueLabel(bool enabled, Color enabledColor, Color disabledColor)
        {
            GUIStyle style = FormFieldValueLabel;
            style.normal.textColor = enabled ? enabledColor : disabledColor;
            return style;
        }
        public GUIStyle ColoredHeaderLabel(Color color)
        {
            GUIStyle style = HeaderLabel;
            style.normal.textColor = color;
            return style;
        }
        public GUIStyle ColoredSubHeaderLabel(Color color)
        {
            GUIStyle style = SubHeaderLabel;
            style.normal.textColor = color;
            return style;
        }

    }

}
