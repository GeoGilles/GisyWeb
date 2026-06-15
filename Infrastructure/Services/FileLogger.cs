// @author: Gilles Lavoie - Architecte principal - 2024-2026
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace GisyWeb.Services
{
    public static class FileLogger
    {
        private static string _logDirectory = string.Empty;
        private static readonly object _writeLock = new object();
        private static bool _isEnabled = true;
        private static bool _isInitialized = false;

        static FileLogger()
        {
            _logDirectory = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs"));
        }

        #region INITIALISATION

        public static void Init(IConfiguration configuration)
        {
            try
            {
                var section = configuration.GetSection("CustomSettings");
                _isEnabled = section.GetValue<bool>("EnableLogs", true);
                string configPath = section["LogDirectory"] ?? "Logs";

                // Ancrage absolu pour éviter la perte de logs dans le "bin"
                _logDirectory = Path.IsPathRooted(configPath)
                    ? configPath
                    : Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, configPath));

                if (_isEnabled && !Directory.Exists(_logDirectory))
                    Directory.CreateDirectory(_logDirectory);

                _isInitialized = true;
                WriteSection("DÉMARRAGE SESSION CARTO⁵", "SYSTEM");
                Write($"Ancrage du journal : {_logDirectory}", "SYSTEM");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ [LOGGER-INIT-FATAL] {ex.Message}");
            }
        }

        #endregion

        #region API PUBLIQUE (SIGNATURES UNIVERSELLES)

        public static void WriteSection(string title, string level = "INFO")
        {
            string separator = new string('=', 60);
            InternalWrite(level, "", "", separator);
            InternalWrite(level, "", "", $"   {title.ToUpper()}");
            InternalWrite(level, "", "", separator);
        }

        public static void Write(string message, string level = "GENERIC", [CallerFilePath] string file = "", [CallerMemberName] string member = "")
        {
            InternalWrite(level, file, member, message);
        }

        public static void LogInfo(string message, [CallerFilePath] string file = "", [CallerMemberName] string member = "")
        {
            InternalWrite("INFO", file, member, message);
        }

        public static void LogWarning(string message, [CallerFilePath] string file = "", [CallerMemberName] string member = "")
        {
            InternalWrite("WARN", file, member, message);
        }

        // =====================================================================
        // SURCHARGE 1 : string en premier (Ex: LogError("Crash", ex))
        // =====================================================================
        public static void LogError(string message, Exception ex = null, [CallerFilePath] string file = "", [CallerMemberName] string member = "")
        {
            FormatAndWriteError(message, ex, file, member);
        }

        // =====================================================================
        // SURCHARGE 2 : Exception en premier (Ex: LogError(ex, "Crash"))
        // =====================================================================
        public static void LogError(Exception ex, string message = "", [CallerFilePath] string file = "", [CallerMemberName] string member = "")
        {
            FormatAndWriteError(message, ex, file, member);
        }

        // --- Méthode de formatage commune pour les deux surcharges ---
        private static void FormatAndWriteError(string message, Exception ex, string file, string member)
        {
            if (!_isEnabled) return;
            var sb = new StringBuilder();
            if (!string.IsNullOrEmpty(message)) sb.AppendLine(message);
            if (ex != null)
            {
                sb.AppendLine($"   Type    : {ex.GetType().Name}");
                sb.AppendLine($"   Message : {ex.Message}");
                if (!string.IsNullOrEmpty(ex.StackTrace)) sb.AppendLine($"   Stack   : {ex.StackTrace}");
                if (ex.InnerException != null) sb.AppendLine($"   Inner   : {ex.InnerException.Message}");
            }
            InternalWrite("ERROR", file, member, sb.ToString());
        }

        public static void Clear()
        {
            try
            {
                string path = Path.Combine(_logDirectory, $"Carto_Log_{DateTime.Now:yyyyMMdd}.txt");
                lock (_writeLock) { if (File.Exists(path)) File.Delete(path); }
                WriteSection("JOURNAL RÉINITIALISÉ", "SYS");
            }
            catch { }
        }

        #endregion

        #region MOTEUR DE PERSISTANCE

        private static void InternalWrite(string level, string filePath, string memberName, string message)
        {
            if (!_isEnabled) return;

            string className = string.IsNullOrEmpty(filePath) ? "" : Path.GetFileNameWithoutExtension(filePath);
            string context = string.IsNullOrEmpty(className) ? "" : $"[{className}] ";
            string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            string line = $"[{timestamp}] [{level.PadRight(7)}] {context}{message}";

            // Miroir Console
            Console.WriteLine($"📝 {line}");

            if (!_isInitialized) return;

            try
            {
                if (string.IsNullOrEmpty(_logDirectory)) return;
                string fullPath = Path.Combine(_logDirectory, $"Carto_Log_{DateTime.Now:yyyyMMdd}.txt");

                lock (_writeLock)
                {
                    if (!Directory.Exists(_logDirectory)) Directory.CreateDirectory(_logDirectory);
                    File.AppendAllText(fullPath, line + Environment.NewLine);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ [DISK-IO-FAIL] {ex.Message}");
            }
        }

        #endregion
    }
}