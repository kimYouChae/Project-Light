using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>플레이 세션의 사건을 콘솔과 로그 파일에 기록합니다.</summary>
public static class PlayLog
{
    /// <summary>Play 중 사건을 Unity 콘솔과 세션 파일에 기록합니다.</summary>
    [Conditional("UNITY_EDITOR")]
    [Conditional("DEVELOPMENT_BUILD")]
    public static void Write(
        string category, string message,
        LogType level = LogType.Log, UnityEngine.Object context = null)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (!sessionActive || !enabled)
        {
            return;
        }

        string line = FormatLine(category, message);
        UnityEngine.Debug.unityLogger.Log(level, (object)line, context);

        if (writer == null)
        {
            return;
        }

        try
        {
            writer.WriteLine(line);
        }
        catch (Exception exception) when (IsFileFailure(exception))
        {
            CloseWriter();
            ReportFileIssue(exception);
        }
#endif
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private const int RETAINED_PLAY_COUNT = 10;
#if UNITY_EDITOR
    private const string EDITOR_PREF = "ProjectLight.PlayLog.Enabled";
    private const string MENU_PATH = "Tools/Project Light/Play Log";
#endif

    private static StreamWriter writer;
    private static string sessionStamp;
    private static bool sessionActive;
    private static bool enabled;
    private static bool fileAttempted;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void BeginSession()
    {
        EndSession();
        Application.quitting -= EndSession;
        Application.quitting += EndSession;

        sessionStamp = DateTime.Now.ToString(
            "yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
        sessionActive = true;
        fileAttempted = false;

#if UNITY_EDITOR
        SetEnabled(UnityEditor.EditorPrefs.GetBool(EDITOR_PREF, true));
#else
        SetEnabled(Array.IndexOf(Environment.GetCommandLineArgs(), "-no-play-log") < 0);
#endif
    }

    private static void SetEnabled(bool value)
    {
        enabled = value;

        if (enabled && sessionActive && !fileAttempted)
        {
            OpenSessionFile();
        }
    }

    private static string FormatLine(string category, string message)
    {
        return string.Format(
            CultureInfo.InvariantCulture, "[{0}][{1:F3}][{2}] {3}",
            category, Time.time, Time.frameCount, message);
    }

    private static void OpenSessionFile()
    {
        fileAttempted = true;

        try
        {
#if UNITY_EDITOR
            string root = Directory.GetParent(Application.dataPath).FullName;
#else
            string root = Application.persistentDataPath;
#endif
            string folder = Path.Combine(root, "Logs");
            Directory.CreateDirectory(folder);

            string path = GetAvailablePath(folder);
            writer = new StreamWriter(path, false, new UTF8Encoding(false))
            {
                AutoFlush = true
            };

            TrimOldLogs(folder);
        }
        catch (Exception exception) when (IsFileFailure(exception))
        {
            ReportFileIssue(exception);
        }
    }

    private static string GetAvailablePath(string folder)
    {
        string prefix = "PlayLog_" + sessionStamp;
        string path = Path.Combine(folder, prefix + ".txt");
        int suffix = 1;

        while (File.Exists(path))
        {
            string number = suffix.ToString("D4", CultureInfo.InvariantCulture);
            path = Path.Combine(folder, prefix + "_" + number + ".txt");
            suffix++;
        }

        return path;
    }

    private static void TrimOldLogs(string folder)
    {
        string[] files = Directory.GetFiles(folder, "PlayLog_*.txt");
        Array.Sort(files, StringComparer.Ordinal);

        for (int index = 0; index < files.Length - RETAINED_PLAY_COUNT; index++)
        {
            File.Delete(files[index]);
        }
    }

    private static void EndSession()
    {
        sessionActive = false;
        CloseWriter();
    }

    private static void CloseWriter()
    {
        StreamWriter current = writer;
        writer = null;

        try
        {
            current?.Dispose();
        }
        catch (Exception exception) when (IsFileFailure(exception))
        {
            ReportFileIssue(exception);
        }
    }

    private static bool IsFileFailure(Exception exception)
    {
        return exception is IOException || exception is UnauthorizedAccessException;
    }

    private static void ReportFileIssue(Exception exception)
    {
        // 파일 쓰기 실패를 다시 파일에 쓰지 않고 콘솔에만 알린다.
        UnityEngine.Debug.unityLogger.Log(
            LogType.Warning, FormatLine("System", exception.Message));
    }

#if UNITY_EDITOR
    [UnityEditor.MenuItem(MENU_PATH)]
    private static void ToggleFromMenu()
    {
        bool value = !UnityEditor.EditorPrefs.GetBool(EDITOR_PREF, true);
        UnityEditor.EditorPrefs.SetBool(EDITOR_PREF, value);
        SetEnabled(value);
    }

    [UnityEditor.MenuItem(MENU_PATH, true)]
    private static bool RefreshMenuCheck()
    {
        UnityEditor.Menu.SetChecked(
            MENU_PATH, UnityEditor.EditorPrefs.GetBool(EDITOR_PREF, true));
        return true;
    }
#endif
#endif
}
