using System;
using System.Collections.Generic;
using UnityEngine;

namespace ScavMulti;

/// <summary>
/// restores the function of AssetDatabase.GetAssetPath by using the ResourcesAPI.overrideAPI
/// to "hook" into Resources.Load
/// </summary>
public sealed class AssetResolver : ResourcesAPI
{
	public static AssetResolver Instance;

	public readonly HashSet<Type> ObservedTypes;
	private readonly Dictionary<Object, string> _objectToStringMap;

	public AssetResolver(params Type[] observedTypes)
	{
		ObservedTypes = new(observedTypes);
		_objectToStringMap = new();
	}

	public static void Init(params Type[] observedTypes)
	{
		Instance = new AssetResolver(observedTypes);
		ResourcesAPI.overrideAPI = Instance;
	}

	public override Object Load(string path, Type systemTypeInstance)
	{
		var result = base.Load(path, systemTypeInstance);
		if (result)
		{
			if (ObservedTypes.Contains(systemTypeInstance) && !_objectToStringMap.ContainsKey(result))
				_objectToStringMap.Add(result, path);
		}
		return result;
	}

	public static string GetAssetPath(Object obj)
	{
		if (Instance._objectToStringMap.TryGetValue(obj, out string result))
			return result;
		return null;
	}
}
