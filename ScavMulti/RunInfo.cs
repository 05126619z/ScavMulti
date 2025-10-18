

using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace ScavMulti;

[HarmonyPatch]
public static class RunInfo
{
	private static Dictionary<Vector2Int, ushort> _modifiedBlocks;
	private static Dictionary<global::BuildingEntity, int> _entityIdentifierMap;
	private static Dictionary<int, global::BuildingEntity> _reverseEntityIdentifierMap;
	private static List<int> _destroyedEntityIds;
	private static int _currentMaxEntityId;

	[HarmonyPostfix]
	[HarmonyPatch(typeof(global::WorldGeneration), nameof(global::WorldGeneration.SetBlock))]
	static void WorldGeneration_SetBlock_Postfix(Vector2Int pos, ushort block)
	{
		if (!GameFlowManager.IsWorldGenerating)
			_modifiedBlocks[pos] = block;
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(global::BuildingEntity), nameof(global::BuildingEntity.Start))]
	static void BuildingEntity_Start_Postfix(BuildingEntity __instance)
	{
		if (!_entityIdentifierMap.ContainsKey(__instance))
		{
			_entityIdentifierMap.Add(__instance, _currentMaxEntityId);
			_reverseEntityIdentifierMap.Add(_currentMaxEntityId, __instance);
			_currentMaxEntityId++;
		}
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(global::BuildingEntity), "OnDestroy")]
	static void BuildingEntity_OnDestroy_Postfix(BuildingEntity __instance)
	{
		if (_entityIdentifierMap.TryGetValue(__instance, out int id))
		{
			_destroyedEntityIds.Add(id);
			_entityIdentifierMap.Remove(__instance);
			_reverseEntityIdentifierMap.Remove(id);
		}
	}

	private static void OnWorldGenStart()
	{
		_entityIdentifierMap = new();
		_reverseEntityIdentifierMap = new();
		_currentMaxEntityId = 0;
	}

	private static void OnWorldGenEnd()
	{
		_modifiedBlocks = new();
		_destroyedEntityIds = new();
	}

	public static void Init()
	{
		GameFlowManager.OnWorldGenEnd += OnWorldGenEnd;
		GameFlowManager.OnWorldGenStart += OnWorldGenStart;
	} 

	public static IReadOnlyDictionary<Vector2Int, ushort> ModifiedBlocks => _modifiedBlocks;
	public static IReadOnlyDictionary<global::BuildingEntity, int> EntityIdentifierMap => _entityIdentifierMap;
	public static IReadOnlyDictionary<int, global::BuildingEntity> ReverseEntityIdentifierMap => _reverseEntityIdentifierMap;
	public static IReadOnlyList<int> DestroyedEntityIds => _destroyedEntityIds;
}
