using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace SPCP
{
    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    public class MonitoringSourceDesc
    {
        public uint dwVersion;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)] public string szName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)] public string szUnits;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)] public string szFormat;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)] public string szGroup;
        public uint dwID;
        public uint dwInstance;
        public float fltMaxLimit;
        public float fltMinLimit;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)] public string szNameTemplate;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)] public string szGroupTemplate;
    }

    internal sealed class PluginSettings
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern uint GetPrivateProfileString(
            string section, string key, string defaultValue,
            StringBuilder returnedString, uint size, string filePath);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool WritePrivateProfileString(
            string section, string key, string value, string filePath);

        public bool EnablePlugin, EnableLog;
        public string AppId, LogPath, Name;
        public int RefreshIntervalMs, SettingsRefreshIntervalMs;

        public static PluginSettings Load()
        {
            string ini = Path.Combine(
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
                    ?? AppDomain.CurrentDomain.BaseDirectory,
                "SPCP.ini");

            if (!File.Exists(ini))
            {
                try
                {
                    string[] d =
                    {
                        "Name", "auto", "EnablePlugin", "1", "AppId", "3393110",
                        "RefreshIntervalMs", "180000", "SettingsRefreshIntervalMs", "5000",
                        "EnableLog", "1",
                        "LogPath", @"C:\Programas\MSI Afterburner\Plugins\Monitoring\SPCP.log"
                    };
                    for (int i = 0; i < d.Length; i += 2)
                        WritePrivateProfileString("SPCP", d[i], d[i + 1], ini);
                }
                catch { }
            }

            return new PluginSettings
            {
                Name = Str(ini, "Name", "auto"),
                EnablePlugin = Bool(ini, "EnablePlugin", true),
                AppId = Str(ini, "AppId", ""),
                RefreshIntervalMs = Int(ini, "RefreshIntervalMs", 60000),
                SettingsRefreshIntervalMs = Int(ini, "SettingsRefreshIntervalMs", 5000),
                EnableLog = Bool(ini, "EnableLog", true),
                LogPath = Str(ini, "LogPath", @"C:\Users\Public\SPCP.log")
            };
        }

        private static string Str(string ini, string key, string def)
        {
            StringBuilder b = new StringBuilder(1024);
            GetPrivateProfileString("SPCP", key, def, b, 1024, ini);
            return b.ToString().Trim();
        }

        private static int Int(string ini, string key, int def)
        {
            int v;
            return int.TryParse(Str(ini, key, def.ToString(CultureInfo.InvariantCulture)),
                NumberStyles.Integer, CultureInfo.InvariantCulture, out v) && v >= 1000 ? v : def;
        }

        private static bool Bool(string ini, string key, bool def)
        {
            return Regex.IsMatch(Str(ini, key, def ? "1" : "0"), "^(1|true|yes)$", RegexOptions.IgnoreCase);
        }
    }

    internal static class SteamPlayerCount
    {
        private static readonly object Sync = new object();
        private static PluginSettings Settings = PluginSettings.Load();
        private static DateTime lastSettingsLoad = DateTime.MinValue;
        private static DateTime lastApiAttempt = DateTime.MinValue;
        private static DateTime lastNameLookupAttempt = DateTime.MinValue;
        private static float lastPlayerCount;
        private static string cachedAppName = "", cachedAppId = "";

        private static string Get(string url, int timeoutMs)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Timeout = request.ReadWriteTimeout = timeoutMs;
            request.UserAgent = "SPCP/1.0";

            using (WebResponse response = request.GetResponse())
            using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                return reader.ReadToEnd();
        }

        public static string GetGroupName()
        {
            lock (Sync)
            {
                DateTime now = DateTime.UtcNow;
                ReloadSettingsIfNeeded(now);

                string name = (Settings.Name ?? "auto").Trim();

                // Name set manually in SPCP.ini.
                if (!name.Equals("auto", StringComparison.OrdinalIgnoreCase))
                    return name == "" ? "Steam" : name;

                if (string.IsNullOrWhiteSpace(Settings.AppId))
                    return "Steam";

                if (cachedAppId == Settings.AppId && cachedAppName != "")
                    return cachedAppName;

                // Avoid hammering Steam after a failed request.
                if ((now - lastNameLookupAttempt).TotalSeconds < 60)
                    return "Steam";

                lastNameLookupAttempt = now;

                try
                {
                    string json = Get("https://store.steampowered.com/api/appdetails?appids="
                        + Uri.EscapeDataString(Settings.AppId), 5000);

                    Match m = Regex.Match(json,
                        "\"" + Regex.Escape(Settings.AppId) + "\"\\s*:\\s*\\{.*?\"name\"\\s*:\\s*"
                        + "\"((?:\\\\.|[^\"\\\\])*)\"",
                        RegexOptions.Singleline);

                    string appName = m.Success
                        ? Regex.Unescape(m.Groups[1].Value.Replace("\\/", "/"))
                        : "";

                    if (!string.IsNullOrWhiteSpace(appName))
                    {
                        cachedAppId = Settings.AppId;
                        cachedAppName = appName;
                        Log("Steam app name resolved: " + appName);
                        return appName;
                    }

                    Log("Could not find the app name in the Steam response.");
                }
                catch (Exception ex)
                {
                    Log("Steam app details request failed: " + ex.GetType().Name + ": " + ex.Message);
                }

                return "Steam";
            }
        }

        public static float GetPlayerCount()
        {
            lock (Sync)
            {
                DateTime now = DateTime.UtcNow;
                ReloadSettingsIfNeeded(now);

                if (!Settings.EnablePlugin)
                    return 0;

                if ((now - lastApiAttempt).TotalMilliseconds < Settings.RefreshIntervalMs)
                    return lastPlayerCount;

                lastApiAttempt = now;

                int appId;
                if (!int.TryParse(Settings.AppId, NumberStyles.None,
                        CultureInfo.InvariantCulture, out appId) || appId <= 0)
                {
                    Log("Invalid or empty AppId in SPCP.ini: " + Settings.AppId);
                    return lastPlayerCount;
                }

                try
                {
                    Log("Starting Steam API request for AppID " + appId + ".");

                    string json = Get("https://api.steampowered.com/ISteamUserStats/"
                        + "GetNumberOfCurrentPlayers/v1/?appid=" + appId, 10000);

                    Match result = Regex.Match(json, "\"result\"\\s*:\\s*(\\d+)");
                    Match count = Regex.Match(json, "\"player_count\"\\s*:\\s*(\\d+)");
                    int players;

                    if (result.Success && result.Groups[1].Value == "1"
                        && int.TryParse(count.Groups[1].Value, NumberStyles.Integer,
                            CultureInfo.InvariantCulture, out players))
                    {
                        lastPlayerCount = players;
                        Log("Steam API request completed. Current players: " + players + ".");
                    }
                    else
                    {
                        Log("Invalid Steam API response: " + json);
                    }
                }
                catch (Exception ex)
                {
                    Log("Steam API request failed: " + ex.GetType().Name + ": " + ex.Message);
                }

                return lastPlayerCount;
            }
        }

        private static void ReloadSettingsIfNeeded(DateTime now)
        {
            if (lastSettingsLoad != DateTime.MinValue
                && (now - lastSettingsLoad).TotalMilliseconds < Settings.SettingsRefreshIntervalMs)
                return;

            lastSettingsLoad = now;

            try
            {
                PluginSettings s = PluginSettings.Load();
                bool appIdChanged = (Settings.AppId ?? "").Trim() != (s.AppId ?? "").Trim();
                bool stateChanged = Settings.EnablePlugin != s.EnablePlugin;
                Settings = s;

                if (appIdChanged)
                {
                    // Clear caches and allow immediate lookups for the new AppID.
                    lastPlayerCount = 0;
                    lastApiAttempt = lastNameLookupAttempt = DateTime.MinValue;
                    cachedAppId = cachedAppName = "";
                    Log("AppID changed. Player count and app name caches cleared.");
                }

                if (stateChanged)
                {
                    lastApiAttempt = DateTime.MinValue;
                    Log(Settings.EnablePlugin ? "Plugin enabled." : "Plugin disabled.");
                }
            }
            catch (Exception ex)
            {
                Log("Settings reload failed: " + ex.GetType().Name + ": " + ex.Message);
            }
        }

        public static void Log(string message)
        {
            if (Settings == null || !Settings.EnableLog || string.IsNullOrWhiteSpace(Settings.LogPath))
                return;

            try
            {
                string dir = Path.GetDirectoryName(Settings.LogPath);
                if (!string.IsNullOrEmpty(dir))
                    Directory.CreateDirectory(dir);

                File.AppendAllText(Settings.LogPath,
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)
                    + " - " + message + Environment.NewLine);
            }
            catch { } // Logging errors must never stop Afterburner.
        }
    }

    public static class Exports
    {
        [DllExport("GetSourcesNum", CallingConvention = CallingConvention.Cdecl)]
        public static uint GetSourcesNum()
        {
            return 1;
        }

        [DllExport("SetupSource", CallingConvention = CallingConvention.Cdecl)]
        public static bool SetupSource(uint dwIndex, IntPtr hWnd)
        {
            return hWnd == IntPtr.Zero;
        }

        [DllExport("GetSourceDesc", CallingConvention = CallingConvention.Cdecl)]
        public static bool GetSourceDesc(uint dwIndex, [In, Out] MonitoringSourceDesc pDesc)
        {
            if (dwIndex != 0 || pDesc == null)
                return false;

            pDesc.szName = "Steam Player Count";
            pDesc.szUnits = "players";
            pDesc.szFormat = "%.0f";
            pDesc.szGroup = SteamPlayerCount.GetGroupName();
            pDesc.dwID = pDesc.dwInstance = 0;
            pDesc.fltMinLimit = 0;
            pDesc.fltMaxLimit = 5000000;
            pDesc.szNameTemplate = pDesc.szGroupTemplate = "";

            SteamPlayerCount.Log("GetSourceDesc called. Group: " + pDesc.szGroup);
            return true;
        }

        [DllExport("GetSourceData", CallingConvention = CallingConvention.Cdecl)]
        public static float GetSourceData(uint dwIndex)
        {
            return dwIndex == 0 ? SteamPlayerCount.GetPlayerCount() : 0.0f;
        }
    }
}