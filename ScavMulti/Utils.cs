using System;
using System.Collections;
using UnityEngine;

namespace ScavMulti;

public static class Utils
{
	public static IEnumerator TryCoroutine(IEnumerator coroutine, Action onComplete = null, Action<Exception> onError = null)
	{
		while (true)
		{
			try
			{
				if (!coroutine.MoveNext())
					break;
			}
			catch (Exception e)
			{
				onError?.Invoke(e);
				yield break;
			}
			yield return coroutine.Current;
		}
		onComplete?.Invoke();
	}

	public static void TryAction(Action action, Action onComplete = null, Action<Exception> onError = null)
	{
		try
		{
			action();
		}
		catch (Exception e)
		{
			onError?.Invoke(e);
			return;
		}
		onComplete?.Invoke();
	}
	
	// The default unity exception log handler for doesn't output the stack trace.
	// Little hack so that it does
	public class ProperExceptionLogger(ILogHandler original) : ILogHandler
	{
		public static void Init()
		{
			Debug.unityLogger.logHandler = new ProperExceptionLogger(Debug.unityLogger.logHandler);
		}

		public void LogException(Exception exception, UnityEngine.Object context)
		{
			original.LogFormat(LogType.Error, null, "Exception occured: {0}", exception);
		}

		public void LogFormat(LogType logType, UnityEngine.Object context, string format, params object[] args)
		{
			original.LogFormat(logType, context, format, args);
		}
	}
}
