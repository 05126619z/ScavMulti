using System;
using System.Reflection;
using HarmonyLib;
using MonoMod.Utils;
using UnityEngine.Tilemaps;

namespace ScavMulti;

/// <summary>
/// Contains a static collection of references to various private members of the game
/// </summary>
public static class RefCollection
{	
	public static AccessTools.FieldRef<WorldGeneration, ushort[,]> WorldGenerationWorldBlocksField;
	public static AccessTools.FieldRef<WorldGeneration, Tilemap[,]> WorldGenerationChunksField;

	public static Action<WorldGeneration> WorldGenerationUpdateBiomePostProcessMethod;

	private static void GetMethod<D>(string methodName, out D outDelegate) where D : Delegate
	{
		var genericArgs = typeof(D).GenericTypeArguments;
		if (genericArgs.Length == 0)
			throw new InvalidOperationException($"Passed delegate of type {typeof(D)} has no arguments. If the target method is a static method, use GetStaticMethod");
		var type = genericArgs[0];
		var info = AccessTools.Method(type, methodName);
		outDelegate = info.CreateDelegate<D>();
	}

	private static void GetStaticMethod<T, D>(string methodName, out D outDelegate) where D : Delegate
	{
		var info = AccessTools.Method(typeof(T), methodName);
		outDelegate = info.CreateDelegate<D>();
	}

	private static void GetField<T, O>(string fieldName, out AccessTools.FieldRef<T, O> outFieldRef)
	{
		outFieldRef = AccessTools.FieldRefAccess<T, O>(fieldName);
	}

	public static void Init()
	{
		GetMethod("UpdateBiomePostProcess", out WorldGenerationUpdateBiomePostProcessMethod);
		GetField("worldBlocks", out WorldGenerationWorldBlocksField);
		GetField("chunks", out WorldGenerationChunksField);
	}
}
