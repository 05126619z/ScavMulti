using System;
using System.Collections.Generic;
using MessagePack;
using MessagePack.Formatters;
using MessagePack.Unity;

namespace ScavMulti.Network.Messages;

public class CustomResolver : IFormatterResolver
{
	// Resolver should be singleton.
	public static readonly IFormatterResolver Instance = new CustomResolver();

	private CustomResolver()
	{
	}

	// GetFormatter<T>'s get cost should be minimized so use type cache.
	public IMessagePackFormatter<T> GetFormatter<T>()
	{
		return FormatterCache<T>.Formatter;
	}

	private static class FormatterCache<T>
	{
		public static readonly IMessagePackFormatter<T> Formatter;

		// generic's static constructor should be minimized for reduce type generation size!
		// use outer helper method.
		static FormatterCache()
		{
			Formatter =
				UnityResolver.InstanceWithStandardResolver.GetFormatter<T>()
				?? (IMessagePackFormatter<T>)CustomResolverGetFormatterHelper.GetFormatter(typeof(T));
		}
	}
}

internal static class CustomResolverGetFormatterHelper
{
	// If type is concrete type, use type-formatter map
	static readonly Dictionary<Type, object> formatterMap = new Dictionary<Type, object>()
	{
        // add more your own custom serializers.
		{ typeof(UnityEngine.Random.State), new UnityRandomStateFormatter() },
	};

	internal static object GetFormatter(Type t)
	{
		object formatter;
		if (formatterMap.TryGetValue(t, out formatter))
		{
			return formatter;
		}

		// If type can not get, must return null for fallback mechanism.
		return null;
	}
}
