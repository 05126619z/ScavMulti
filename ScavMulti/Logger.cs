using BepInEx.Logging;

namespace ScavMulti;

public static class Logger
{
	private static ManualLogSource _underlyingLogger;

	public static void Init(ManualLogSource underlyingLogger)
	{
		_underlyingLogger = underlyingLogger;
	}

	public static void Log(LogLevel level, object data) => _underlyingLogger.Log(level, data);
	public static void LogDebug(object data) => _underlyingLogger.LogDebug(data);
	public static void LogError(object data) => _underlyingLogger.LogError(data);
	public static void LogFatal(object data) => _underlyingLogger.LogFatal(data);
	public static void LogInfo(object data) => _underlyingLogger.LogInfo(data);
	public static void LogMessage(object data) => _underlyingLogger.LogMessage(data);
	public static void LogWarning(object data) => _underlyingLogger.LogWarning(data);
}
