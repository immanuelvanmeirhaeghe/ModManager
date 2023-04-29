using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace ModManager
{

    public class MenuLogScreen : MenuScreen
    {
        public Text LogFileContentText;

        public string LogFile;

        public string[] LogFiles;

        public MenuLogScreen()
        {
            LogFile = string.Empty;
            LogFiles = new string[1];
        }

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

        protected override void Update()
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

    }

}