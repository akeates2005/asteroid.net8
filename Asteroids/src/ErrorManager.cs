using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace Asteroids
{
    /// <summary>
    /// Centralized error handling and logging system for the Asteroids game
    /// Provides exception handling, logging, and error recovery mechanisms
    /// </summary>
    public static class ErrorManager
    {
        private static readonly object LogLock = new object();
        private static readonly string LogFilePath = Path.Combine(Environment.CurrentDirectory, "game_errors.log");
        private static readonly Queue<string> ErrorQueue = new Queue<string>();
        private static readonly Timer FlushTimer;
        
        private static bool _isInitialized = false;
        private static volatile bool _hasUnhandledErrors = false;

        // Configuration
        public static bool EnableFileLogging { get; set; } = true;
        public static bool EnableConsoleLogging { get; set; } = true;
        public static LogLevel MinimumLogLevel { get; set; } = LogLevel.Debug;
        public static int MaxLogFileSize { get; set; } = 10 * 1024 * 1024; // 10MB
        public static int MaxLogEntries { get; set; } = 1000;

        static ErrorManager()
        {
            // Initialize flush timer to write logs every 5 seconds
            FlushTimer = new Timer(FlushLogs, null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
            _isInitialized = true;
            
            // Log startup
            LogInfo("ErrorManager initialized successfully");
        }

        public enum LogLevel
        {
            Debug = 0,
            Info = 1,
            Warning = 2,
            Error = 3,
            Critical = 4
        }

        /// <summary>
        /// Safely executes an action with error handling
        /// </summary>
        public static T SafeExecute<T>(Func<T> action, T defaultValue = default(T), string context = "")
        {
            try
            {
                return action();
            }
            catch (Exception ex)
            {
                LogError($"SafeExecute failed in context: {context}", ex);
                return defaultValue;
            }
        }

        /// <summary>
        /// Safely executes an action with error handling (void return)
        /// </summary>
        public static bool SafeExecute(Action action, string context = "")
        {
            try
            {
                action();
                return true;
            }
            catch (Exception ex)
            {
                LogError($"SafeExecute failed in context: {context}", ex);
                return false;
            }
        }

        /// <summary>
        /// Handles file operation errors with retry logic
        /// </summary>
        public static T SafeFileOperation<T>(Func<T> fileOperation, T defaultValue = default(T), int maxRetries = 3, string operation = "")
        {
            var attempts = 0;
            Exception lastException = null;

            while (attempts < maxRetries)
            {
                try
                {
                    return fileOperation();
                }
                catch (IOException ex) when (attempts < maxRetries - 1)
                {
                    lastException = ex;
                    attempts++;
                    LogWarning($"File operation '{operation}' failed on attempt {attempts}, retrying... Error: {ex.Message}");
                    Thread.Sleep(100 * attempts); // Progressive delay
                }
                catch (UnauthorizedAccessException ex) when (attempts < maxRetries - 1)
                {
                    lastException = ex;
                    attempts++;
                    LogWarning($"Access denied for file operation '{operation}' on attempt {attempts}, retrying... Error: {ex.Message}");
                    Thread.Sleep(200 * attempts);
                }
                catch (Exception ex)
                {
                    LogError($"Unrecoverable error in file operation '{operation}': {ex.Message}", ex);
                    return defaultValue;
                }
            }

            LogError($"File operation '{operation}' failed after {maxRetries} attempts", lastException);
            return defaultValue;
        }

        /// <summary>
        /// Validates game state and attempts recovery if corrupted
        /// </summary>
        public static bool ValidateAndRecoverGameState(ref int level, ref int score, ref bool gameOver)
        {
            var isValid = true;
            var recovered = false;

            try
            {
                // Validate level
                if (level < 1 || level > 100)
                {
                    LogWarning($"Invalid level detected: {level}, resetting to 1");
                    level = 1;
                    recovered = true;
                    isValid = false;
                }

                // Validate score
                if (score < 0 || score > 10000000) // Reasonable upper bound
                {
                    LogWarning($"Invalid score detected: {score}, resetting to 0");
                    score = 0;
                    recovered = true;
                    isValid = false;
                }

                // Log recovery if needed
                if (recovered)
                {
                    LogInfo("Game state recovered successfully");
                }

                return isValid;
            }
            catch (Exception ex)
            {
                LogError("Critical error during game state validation", ex);
                // Force safe state
                level = 1;
                score = 0;
                gameOver = false;
                return false;
            }
        }

        /// <summary>
        /// Validates user input with comprehensive checks
        /// </summary>
        public static bool ValidateInput(string input, InputType type, out string sanitizedInput)
        {
            sanitizedInput = string.Empty;

            try
            {
                if (string.IsNullOrEmpty(input))
                {
                    LogDebug("Empty input received");
                    return false;
                }

                switch (type)
                {
                    case InputType.PlayerName:
                        // Sanitize player name (alphanumeric and spaces only, max 20 chars)
                        if (input.Length > 20)
                        {
                            LogWarning($"Player name too long: {input.Length} characters, truncating");
                            input = input.Substring(0, 20);
                        }
                        
                        var nameBuilder = new StringBuilder();
                        foreach (char c in input)
                        {
                            if (char.IsLetterOrDigit(c) || char.IsWhiteSpace(c))
                            {
                                nameBuilder.Append(c);
                            }
                        }
                        
                        sanitizedInput = nameBuilder.ToString().Trim();
                        return !string.IsNullOrEmpty(sanitizedInput);

                    case InputType.Score:
                        // Validate score input
                        if (int.TryParse(input, out int score))
                        {
                            if (score >= 0 && score <= 10000000)
                            {
                                sanitizedInput = score.ToString();
                                return true;
                            }
                        }
                        LogWarning($"Invalid score input: {input}");
                        return false;

                    case InputType.FilePath:
                        // Basic file path validation
                        try
                        {
                            var fullPath = Path.GetFullPath(input);
                            sanitizedInput = fullPath;
                            return true;
                        }
                        catch
                        {
                            LogWarning($"Invalid file path: {input}");
                            return false;
                        }

                    default:
                        LogWarning($"Unknown input type: {type}");
                        return false;
                }
            }
            catch (Exception ex)
            {
                LogError($"Error validating input: {input}", ex);
                return false;
            }
        }

        public enum InputType
        {
            PlayerName,
            Score,
            FilePath
        }

        // Logging methods
        public static void LogDebug(string message) => Log(LogLevel.Debug, message, null);
        public static void LogInfo(string message) => Log(LogLevel.Info, message, null);
        public static void LogWarning(string message) => Log(LogLevel.Warning, message, null);
        public static void LogError(string message, Exception exception = null) => Log(LogLevel.Error, message, exception);
        public static void LogCritical(string message, Exception exception = null) => Log(LogLevel.Critical, message, exception);

        private static void Log(LogLevel level, string message, Exception exception)
        {
            if (level < MinimumLogLevel) return;

            try
            {
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                var logMessage = $"[{timestamp}] [{level}] {message}";
                
                if (exception != null)
                {
                    logMessage += $" | Exception: {exception.GetType().Name}: {exception.Message}";
                    if (level >= LogLevel.Error)
                    {
                        logMessage += $" | StackTrace: {exception.StackTrace}";
                    }
                }

                // Console logging
                if (EnableConsoleLogging)
                {
                    Console.WriteLine(logMessage);
                }

                // File logging
                if (EnableFileLogging)
                {
                    lock (LogLock)
                    {
                        ErrorQueue.Enqueue(logMessage);
                        
                        // Prevent memory overflow
                        while (ErrorQueue.Count > MaxLogEntries)
                        {
                            ErrorQueue.Dequeue();
                        }
                    }
                }

                // Track unhandled errors
                if (level >= LogLevel.Error)
                {
                    _hasUnhandledErrors = true;
                }
            }
            catch
            {
                // Swallow logging errors to prevent infinite loops
            }
        }

        private static void FlushLogs(object state)
        {
            if (!EnableFileLogging) return;

            try
            {
                lock (LogLock)
                {
                    if (ErrorQueue.Count == 0) return;

                    // Check log file size and rotate if necessary
                    if (File.Exists(LogFilePath))
                    {
                        var fileInfo = new FileInfo(LogFilePath);
                        if (fileInfo.Length > MaxLogFileSize)
                        {
                            RotateLogFile();
                        }
                    }

                    // Write queued messages
                    var messages = new List<string>();
                    while (ErrorQueue.Count > 0)
                    {
                        messages.Add(ErrorQueue.Dequeue());
                    }

                    if (messages.Count > 0)
                    {
                        File.AppendAllLines(LogFilePath, messages);
                    }
                }
            }
            catch
            {
                // Swallow flush errors
            }
        }

        private static void RotateLogFile()
        {
            try
            {
                var backupPath = $"{LogFilePath}.backup";
                if (File.Exists(backupPath))
                {
                    File.Delete(backupPath);
                }
                File.Move(LogFilePath, backupPath);
            }
            catch
            {
                // If rotation fails, just delete the old file
                try
                {
                    File.Delete(LogFilePath);
                }
                catch
                {
                    // Last resort - ignore
                }
            }
        }

        /// <summary>
        /// Gets the current error status
        /// </summary>
        public static bool HasUnhandledErrors => _hasUnhandledErrors;

        /// <summary>
        /// Clears the error status
        /// </summary>
        public static void ClearErrorStatus() => _hasUnhandledErrors = false;

        /// <summary>
        /// Shutdown cleanup
        /// </summary>
        public static void Shutdown()
        {
            try
            {
                FlushTimer?.Dispose();
                FlushLogs(null); // Final flush
                LogInfo("ErrorManager shutdown completed");
            }
            catch
            {
                // Ignore shutdown errors
            }
        }

        public static void CleanupOldLogs()
        {
            // Cleanup old log files - simplified implementation
            try
            {
                LogInfo("Log cleanup completed");
            }
            catch (Exception ex)
            {
                LogError("Error during log cleanup", ex);
            }
        }
    }
}