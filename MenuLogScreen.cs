using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using static MenuBase;

namespace ModManager
{

    public class MenuLogScreen : MonoBehaviour, IInputsReceiver
    {
        public new static GameObject gameObject;
        public IUIChangeableOption[] m_ChangeableOptions;
        protected static GameObject s_HighlightGO;
        protected static RectTransform s_HighlightRect;
        private bool m_SetupBackAndAcceptButtonsPos = true;
        public Dictionary<GameObject, MenuOptionData> m_OptionsObjects = new Dictionary<GameObject, MenuOptionData>();
        public virtual void SetupController()
        {
            bool flag = GreenHellGame.IsPadControllerActive();
            foreach (GameObject padDisableElement in m_PadDisableElements)
            {
                padDisableElement?.SetActive(!flag);
            }
            foreach (GameObject padEnableElement in m_PadEnableElements)
            {
                padEnableElement?.SetActive(flag);
            }
            SetupActiveMenuOption();
        }

        public virtual bool IsMenuButtonEnabled(Button b)
        {
            if (b != null && b.enabled)
            {
                return b.gameObject.activeInHierarchy;
            }
            return false;
        }

        public virtual bool IsMenuSliderEnabled(Slider s)
        {
            if (s != null && s.enabled)
            {
                return s.gameObject.activeInHierarchy;
            }
            return false;
        }

        public virtual bool IsMenuSelectButtonEnabled(UISelectButton s)
        {
            if (s != null && s.enabled)
            {
                return s.gameObject.activeInHierarchy;
            }
            return false;
        }

        public void SetupActiveMenuOption()
        {
            if (!GreenHellGame.IsPadControllerActive())
            {
                return;
            }
            if (m_ActiveMenuOption != null && m_ActiveMenuOption.m_Button != null)
            {
                m_ActiveMenuOption.m_Button.Select();
                m_ActiveMenuOption.m_Button.OnSelect(null);
                return;
            }
            foreach (MenuOptionData value in m_OptionsObjects.Values)
            {
                if (value == null)
                {
                    continue;
                }
                if (IsMenuButtonEnabled(value.m_Button))
                {
                    UIButtonEx component = value.m_Button.GetComponent<UIButtonEx>();
                    if (!component || component.m_MoveWhenFocused)
                    {
                        m_ActiveMenuOption = value;
                        OnSelectionChanged();
                        break;
                    }
                }
                if (IsMenuSliderEnabled(value.m_Slider))
                {
                    m_ActiveMenuOption = value;
                    OnSelectionChanged();
                    break;
                }
                if (IsMenuSelectButtonEnabled(value.m_SelectButton))
                {
                    m_ActiveMenuOption = value;
                    OnSelectionChanged();
                    break;
                }
            }
        }

        public MenuLogScreen()
        {
            gameObject = GetComponent<MenuLogScreen>().transform.gameObject;
            LogFile = string.Empty;
            LogFiles = new string[1];
        }

        public virtual bool CanReceiveAction()
        {
            return true;
        }

        public virtual bool CanReceiveActionPaused()
        {
            return true;
        }

        public virtual void OnBack()
        {
        }

        public virtual void OnAccept()
        {
        }


        public virtual void OnInputAction(InputActionData action_data)
        {
            if (action_data.m_Action == InputsManager.InputAction.Button_B)
            {
                if (!GreenHellGame.GetYesNoDialog().gameObject.activeSelf)
                {
                    OnBack();
                }
            }
            else if (action_data.m_Action == InputsManager.InputAction.Button_X)
            {
                if (!GreenHellGame.GetYesNoDialog().gameObject.activeSelf)
                {
                    OnAccept();
                }
            }
            else if (action_data.m_Action == InputsManager.InputAction.Button_A)
            {
                if (m_ActiveMenuOption != null && (bool)m_ActiveMenuOption.m_Button && !GreenHellGame.IsYesNoDialogActive())
                {
                    string persistentMethodName = m_ActiveMenuOption.m_Button.onClick.GetPersistentMethodName(0);
                    SendMessage(persistentMethodName);
                }
            }
            else if (action_data.m_Action == InputsManager.InputAction.LSRight || action_data.m_Action == InputsManager.InputAction.DPadRight)
            {
                if (m_ActiveMenuOption != null && (bool)m_ActiveMenuOption.m_SelectButton)
                {
                    m_ActiveMenuOption.m_SelectButton.PressRightArrow();
                }
            }
            else if (action_data.m_Action == InputsManager.InputAction.LSLeft || action_data.m_Action == InputsManager.InputAction.DPadLeft)
            {
                if (m_ActiveMenuOption != null && (bool)m_ActiveMenuOption.m_SelectButton)
                {
                    m_ActiveMenuOption.m_SelectButton.PressLeftArrow();
                }
            }
            else if (action_data.m_Action == InputsManager.InputAction.LSBackward || action_data.m_Action == InputsManager.InputAction.DPadDown)
            {
                SelectDown();
            }
            else if (action_data.m_Action == InputsManager.InputAction.LSForward || action_data.m_Action == InputsManager.InputAction.DPadUp)
            {
                SelectUp();
            }
        }

        protected void OnSelectionChanged()
        {
            if (!GreenHellGame.IsPadControllerActive() || !(s_HighlightGO != null) || !(s_HighlightRect != null) || m_ActiveMenuOption == null)
            {
                return;
            }
            RectTransform rectTransform = null;
            if (m_ActiveMenuOption.m_Button != null)
            {
                MainMenuManager mainMenuManager = MainMenuManager.Get();
                MainMenu mainMenu = null;
                if ((bool)mainMenuManager)
                {
                    mainMenu = (MainMenu)mainMenuManager.GetScreen(typeof(MainMenu));
                }
                if (s_HighlightGO != null && !s_HighlightGO.activeSelf && (mainMenu == null || mainMenu.AreButtonsEnabled()))
                {
                    s_HighlightGO.SetActive(value: true);
                }
                rectTransform = m_ActiveMenuOption.m_Button.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    rectTransform.GetWorldCorners(m_TempCorners);
                    if (m_Canvas == null)
                    {
                        m_Canvas = rectTransform.GetComponentInParent<Canvas>();
                        m_CanvasRect = m_Canvas.GetComponent<RectTransform>();
                    }
                    for (int i = 0; i < 4; i++)
                    {
                        m_TempCorners2[i] = m_CanvasRect.InverseTransformPoint(m_TempCorners[i]);
                    }
                    s_HighlightRect.anchoredPosition = rectTransform.anchoredPosition;
                    s_HighlightRect.anchorMax = rectTransform.anchorMax;
                    s_HighlightRect.anchorMin = rectTransform.anchorMin;
                    s_HighlightRect.offsetMax = rectTransform.offsetMax;
                    s_HighlightRect.offsetMin = rectTransform.offsetMin;
                    s_HighlightRect.pivot = rectTransform.pivot;
                    float size = Vector3.Distance(m_TempCorners2[0], m_TempCorners2[3]);
                    float size2 = Vector3.Distance(m_TempCorners2[0], m_TempCorners2[1]);
                    s_HighlightRect.sizeDelta = rectTransform.sizeDelta;
                    s_HighlightRect.position = rectTransform.position;
                    s_HighlightRect.rotation = rectTransform.rotation;
                    s_HighlightRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size);
                    s_HighlightRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size2);
                }
                for (int j = 0; j < m_OptionsObjects.Values.Count; j++)
                {
                    MenuOptionData menuOptionData = Enumerable.ElementAt(m_OptionsObjects.Values, j);
                    Transform transform = menuOptionData.m_Object.transform.Find("HL");
                    if (!transform)
                    {
                        continue;
                    }
                    if (m_ActiveMenuOption == menuOptionData)
                    {
                        transform.gameObject.SetActive(value: true);
                        if (s_HighlightGO != null)
                        {
                            s_HighlightGO.SetActive(value: false);
                        }
                    }
                    else
                    {
                        transform.gameObject.SetActive(value: false);
                    }
                }
            }
            else if (s_HighlightGO != null && s_HighlightGO.activeSelf)
            {
                s_HighlightGO.SetActive(value: false);
            }
            if (!(m_ActiveMenuOption.m_SelectButton != null) && (!(m_ActiveMenuOption.m_Slider != null) || !(m_ActiveMenuOption.m_Object != null)))
            {
                return;
            }
            for (int k = 0; k < m_OptionsObjects.Values.Count; k++)
            {
                MenuOptionData menuOptionData2 = Enumerable.ElementAt(m_OptionsObjects.Values, k);
                Transform transform2 = menuOptionData2.m_Object.transform.Find("HL");
                if ((bool)transform2)
                {
                    if (m_ActiveMenuOption == menuOptionData2)
                    {
                        transform2.gameObject.SetActive(value: true);
                    }
                    else
                    {
                        transform2.gameObject.SetActive(value: false);
                    }
                }
            }
        }

        private void SelectUp()
        {
            for (int i = 0; i < m_OptionsObjects.Values.Count; i++)
            {
                MenuOptionData menuOptionData = Enumerable.ElementAt(m_OptionsObjects.Values, i);
                if (m_ActiveMenuOption != menuOptionData)
                {
                    continue;
                }
                for (int num = i - 1; num >= 0; num--)
                {
                    menuOptionData = Enumerable.ElementAt(m_OptionsObjects.Values, num);
                    if (IsMenuButtonEnabled(menuOptionData.m_Button))
                    {
                        UIButtonEx component = menuOptionData.m_Button.GetComponent<UIButtonEx>();
                        if (!component || component.m_MoveWhenFocused)
                        {
                            m_ActiveMenuOption = menuOptionData;
                            OnSelectionChanged();
                            UIAudioPlayer.Play(UIAudioPlayer.UISoundType.Focus);
                            break;
                        }
                    }
                    if (IsMenuSliderEnabled(menuOptionData.m_Slider))
                    {
                        m_ActiveMenuOption = menuOptionData;
                        OnSelectionChanged();
                        UIAudioPlayer.Play(UIAudioPlayer.UISoundType.Focus);
                        break;
                    }
                    if (IsMenuSelectButtonEnabled(menuOptionData.m_SelectButton))
                    {
                        m_ActiveMenuOption = menuOptionData;
                        OnSelectionChanged();
                        UIAudioPlayer.Play(UIAudioPlayer.UISoundType.Focus);
                        break;
                    }
                }
                break;
            }
        }

        private void SelectDown()
        {
            for (int i = 0; i < m_OptionsObjects.Values.Count; i++)
            {
                MenuOptionData menuOptionData = Enumerable.ElementAt(m_OptionsObjects.Values, i);
                if (m_ActiveMenuOption != menuOptionData)
                {
                    continue;
                }
                for (int j = i + 1; j < m_OptionsObjects.Values.Count; j++)
                {
                    menuOptionData = Enumerable.ElementAt(m_OptionsObjects.Values, j);
                    if (IsMenuButtonEnabled(menuOptionData.m_Button))
                    {
                        UIButtonEx component = menuOptionData.m_Button.GetComponent<UIButtonEx>();
                        if (!component || component.m_MoveWhenFocused)
                        {
                            m_ActiveMenuOption = menuOptionData;
                            OnSelectionChanged();
                            UIAudioPlayer.Play(UIAudioPlayer.UISoundType.Focus);
                            break;
                        }
                    }
                    if (IsMenuSliderEnabled(menuOptionData.m_Slider))
                    {
                        m_ActiveMenuOption = menuOptionData;
                        OnSelectionChanged();
                        UIAudioPlayer.Play(UIAudioPlayer.UISoundType.Focus);
                        break;
                    }
                    if (IsMenuSelectButtonEnabled(menuOptionData.m_SelectButton))
                    {
                        m_ActiveMenuOption = menuOptionData;
                        OnSelectionChanged();
                        UIAudioPlayer.Play(UIAudioPlayer.UISoundType.Focus);
                        break;
                    }
                }
                break;
            }
        }


        public static float s_ButtonsAlpha = 1f;

        public static float s_ButtonsHighlightedAlpha = 1f;

        public static float s_InactiveButtonsAlpha = 0.3f;

        public bool m_IsIngame;

        private HUDManager.HUDGroup m_VisibleHUD;

        public MenuInGameManager m_MenuInGameManager;

        public List<GameObject> m_PadEnableElements = new List<GameObject>();

        public List<GameObject> m_PadDisableElements = new List<GameObject>();

        protected Vector3 m_AcceptInitialPosLocal = Vector3.zero;

        protected Vector3 m_BackInitialPosLocal = Vector3.zero;

        public Vector3 m_PadSelectButtonInitialPosLocal = Vector3.zero;

        public Image m_PadSelectBtn;

        public Vector3 m_PadBackButtonInitialPosLocal = Vector3.zero;

        public Image m_PadBackBtn;

        public Vector3 m_PadAcceptButtonInitialPosLocal = Vector3.zero;

        public Image m_PadAcceptBtn;

        private Vector3[] m_TempCorners = new Vector3[4];

        private Vector3[] m_TempCorners2 = new Vector3[4];

        private Canvas m_Canvas;

        private RectTransform m_CanvasRect;

        private float m_LastScrollTime;

        protected void Awake()
        {
            SetupController();
            m_AcceptInitialPosLocal.Set(0.769f, 0.187f, 0f);
            m_BackInitialPosLocal.Set(0.8864f, 0.187f, 0f);
            float num = ((MainLevel.Instance != null) ? (-0.2f) : 0f);
            m_PadSelectButtonInitialPosLocal.Set(0.44f + num, 0.15f, 0f);
            m_PadBackButtonInitialPosLocal.Set(0.68f + num, 0.15f, 0f);
            m_PadAcceptButtonInitialPosLocal.Set(0.2f + num, 0.15f, 0f);
        }

        public Text LogFileContentText;

        public string LogFile;

        public string[] LogFiles;
        private MenuOptionData m_ActiveMenuOption;

        public MenuLogScreen(string logFile)
            : this()
        {
            SetLogFile(logFile);
        }

        public MenuLogScreen(string[] logFiles)
           : this()
        {
            SetLogFiles(logFiles);
        }

        protected virtual void SetLogFiles(string[] logFiles)
        {
            if (logFiles != null && logFiles.Length > 0)
            {
                LogFiles = logFiles;
                LogFile = logFiles[0];
            }
        }

        protected virtual void SetLogFile(string logFile)
        {
            LogFile = logFile;
            if (LogFiles != null && LogFiles.Length > 0)
            {
                if (!LogFiles.Contains(logFile))
                {
                    LogFiles[0] = logFile;
                }
            }
        }

        protected void Update()
        {
            LogFileContentText.text = string.Empty;

            if (LogFiles != null && LogFiles.Length > 0)
            {
                foreach (string logFilePath in LogFiles)
                {
                    if (File.Exists(logFilePath))
                    {
                        string[] logFileContent = File.ReadAllLines(logFilePath);                        
                        Text contentText = LogFileContentText;
                        foreach (string line in logFileContent)
                        {
                            contentText.text += line;
                            contentText.text += "\n";
                        }
                        LogFileContentText = contentText;
                    }
                }
            }
          
            LogFileContentText.text += "\n";
            LogFileContentText.text += CursorControl.GetGlobalCursorPos().ToString();
            LogFileContentText.text += Cursor.lockState;
        }

        public void Show()
        {
            gameObject.SetActive(value: true);
            OnShow();
            OnPostShow();         
        }

        public void Hide()
        {
            gameObject.SetActive(value: false);
            OnHide();      
        }

        public void OnShow()
        {
            UpdateItemsVisibility();
          
            InputsManager.Get().RegisterReceiver(this);
            SetupController();
            m_SetupBackAndAcceptButtonsPos = true;
        }

        public void OnHide()
        {      
            InputsManager.Get().UnregisterReceiver(this);
            if (GreenHellGame.IsPadControllerActive() && s_HighlightGO != null)
            {
                s_HighlightGO.SetActive(value: false);
            }
        }

        public virtual void OnPostShow()
        {
            RememberOptionValues();
        }

        public void RememberOptionValues()
        {
            if (m_ChangeableOptions != null)
            {
                IUIChangeableOption[] changeableOptions = m_ChangeableOptions;
                for (int i = 0; i < changeableOptions.Length; i++)
                {
                    changeableOptions[i].StoreValue();
                }
            }
        }

        private void UpdateItemsVisibility()
        {
            foreach (MenuOptionData value in m_OptionsObjects.Values)
            {
                value.SetBackgroundVisible(!m_IsIngame);
            }
        }

     
    }

}