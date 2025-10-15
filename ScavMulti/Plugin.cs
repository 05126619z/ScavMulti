using System;
using System.Collections;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using Process = System.Diagnostics.Process;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ScavMulti.Network;
using ScavMulti.Network.Messages;

namespace ScavMulti;

record class MasterData
{
	public UnityEngine.Random.State WorldGenSeed { get; set; }
	public Server Server { get; set; }
}

[BepInPlugin(PluginInfo.PluginGUID, PluginInfo.PluginName, PluginInfo.PluginVersion)]
public class Plugin : BaseUnityPlugin
{
	public static Plugin Instance { get; private set; }
	private Harmony _harmony = null;

	public new BepInEx.Logging.ManualLogSource Logger => base.Logger;

	private void Awake()
    {
		Instance = this;
		try
		{
            MessagePack.MessagePackSerializer.DefaultOptions = Constants.MessagePackSerializerOptions;
			Utils.ProperExceptionLogger.Init();
			AssetResolver.Init(typeof(Sprite));
			_harmony = new Harmony(PluginInfo.PluginGUID);
			_harmony.PatchAll(GetType());
		}
		catch (Exception e)
		{
			Logger.LogError($"Exception thrown on initialization of {PluginInfo.PluginGUID}:\n{e}");
			throw;
		}
		Logger.LogInfo($"Plugin {PluginInfo.PluginGUID} loaded succesfully");
	}

	static ExperimentInfo _mainExperiment = new(null, null);
	static bool _isWorldInitFinished = false;
	static MasterData _masterData;

	[HarmonyPostfix]
	[HarmonyPatch(typeof(global::Body), "Start")]
	static void Body_Start_Postfix(Body __instance)
	{
		if (!_mainExperiment.Body)
		{
			Debug.Log("MainBody::Start");
			_mainExperiment = new(__instance.transform.parent.gameObject, __instance);
			_isWorldInitFinished = false;
		}
		// else
		// {
		// 	Debug.Log("OtherBody::Start");
		// 	_otherExperiment = new(__instance.transform.parent.gameObject, __instance);
		// }
	}

	static void MainBodyUpdatePostfix()
	{
		foreach (var deadClient in _masterData.Server.RemoveDeadClients())
		{
			Debug.LogWarning($"Client is leaving. Exception: {deadClient.ClientCancelledException}");
		}
		foreach (var client in _masterData.Server)
		{
			if (client.IsRunning && !client.IsEmpty)
			{
				var data = client.Dequeue();
				switch (data)
				{
					default:
						Debug.LogError($"Unknown or unimplemented message received: {data.GetType()}");
						break;
				}
			}
		}
	}

	static void MainBodyUpdatePrefix()
	{

	}

	static void OtherBodyUpdatePostfix()
	{

	}

	static void OtherBodyUpdatePrefix()
	{
		// var body = _otherExperiment.Body;
		// body.moveDir.x = (Input.GetKey(KeyCode.L) ? 1f : 0f) - (Input.GetKey(KeyCode.J) ? 1f : 0f);
		// body.moveDir.y = (Input.GetKey(KeyCode.I) ? 1f : 0f) - (Input.GetKey(KeyCode.K) ? 1f : 0f);
		// if (Input.GetKeyDown(KeyCode.M))
		// {
		// 	body.Jump();
		// 	body.endedJump = false;
		// }
		// else if (Input.GetKeyUp(KeyCode.M))
		// 	body.endedJump = true;
		
		// body.crouching = (!body.reversedControls && Input.GetKey(KeyCode.K)) || (body.reversedControls && Input.GetKey(KeyCode.I));
	}

	static Client _servInfo;

	[HarmonyPostfix]
	[HarmonyPatch(typeof(global::Body), "Update")]
	static void Body_Update_Postfix(Body __instance)
	{
		if (_isWorldInitFinished)
		{
			if (__instance == _mainExperiment.Body && _masterData != null)
				MainBodyUpdatePostfix();
			else
				OtherBodyUpdatePostfix();
		}
	}

	[HarmonyPrefix]
	[HarmonyPatch(typeof(global::Body), "Update")]
	static void Body_Update_Prefix(Body __instance)
	{
		if (_isWorldInitFinished)
		{
			if (_masterData != null)
			{
				while (_masterData.Server.NextPendingClient(out var pendingClient))
				{
					Debug.Log("client pending !!");

					_masterData.Server.AcceptClient(pendingClient);

					pendingClient.Enqueue(new WorldInfo(
						WorldGeneration.world.chunkWidth,
						WorldGeneration.world.chunkHeight,
						(uint)WorldGeneration.CHUNKSIZE,
						__instance.transform.position.x,
						__instance.transform.position.y,
						_masterData.WorldGenSeed,
						WorldGeneration.world.biomeDepth));
					pendingClient.Enqueue(new ModifiedBlocksInfo(_modifiedBlocks));
					pendingClient.Enqueue(new DestroyedEntitiesInfo(_destroyedEntityIds));
				}
			}
			if (__instance == _mainExperiment.Body)
				MainBodyUpdatePrefix();
			else
				OtherBodyUpdatePrefix();
		}
	}

	class Ipv4Validator : TMP_InputValidator
	{
		public override char Validate(ref string text, ref int pos, char ch)
		{
			if ((!char.IsDigit(ch) && ch != '.' && ch != ':') || text.Length > 22)
				return '\0';
			text = text.Insert(pos, new string(ch, 1));
			pos++;
			return ch;
		}
	}

	static bool _askForWorld = false;

	[HarmonyPostfix]
	[HarmonyPatch(typeof(global::PreRunScript), "Start")]
	static void PreRunScript_Start_Postfix(PreRunScript __instance)
	{
		var res = Screen.currentResolution;
		Screen.SetResolution(res.width / 2, res.height / 2, FullScreenMode.Windowed, res.refreshRateRatio);
		Application.runInBackground = true;
		var currentProcess = Process.GetCurrentProcess();
		var otherProcess = Process.GetProcessesByName(currentProcess.ProcessName).Where(x => x != currentProcess).FirstOrDefault();
		if (otherProcess != null && currentProcess.StartTime > otherProcess.StartTime)
			Screen.MoveMainWindowTo(Screen.mainWindowDisplayInfo, new Vector2Int(Screen.width, 0));
		else
		{
			Screen.MoveMainWindowTo(Screen.mainWindowDisplayInfo, new Vector2Int(0, 0));
			__instance.StartRun();
			return;
		}

		UiUtils.DefaultBackgroundSprite = __instance.loadButton.GetComponent<Image>().sprite;
		UiUtils.ReferenceText = __instance.loadButton.GetComponentInChildren<TextMeshProUGUI>(true);
		var canvas = __instance.GetComponent<Canvas>();

		var netCanvasObject = UiUtils.CreateAutoGrowingUI(canvas.transform, "ScavMultiUI", ContentAlignment.TopLeft, new Vector2(100, -100), 5, new RectOffset(15, 22, 5, 15));

		UiUtils.CreateTMPLabel(netCanvasObject.transform, "Label", "ScavMulti");

		var inputField = UiUtils.CreateInputField(netCanvasObject.transform, customWidth: 220, prompt: "Enter an IP address");
		inputField.text = "127.0.0.1:5000";

		inputField.characterValidation = TMP_InputField.CharacterValidation.CustomValidator;
		inputField.inputValidator = ScriptableObject.CreateInstance<Ipv4Validator>();
		
		var connectButton = UiUtils.CreateButton(netCanvasObject.transform, "btn", "Connect", null);

		var errorLabel = UiUtils.CreateTMPLabel(netCanvasObject.transform, "errorLabel", "", keepDefault: true, overrideColor: Color.red);

		connectButton.onClick.AddListener(() =>
		{
			__instance.StartCoroutine(Coroutine(__instance, inputField.text, errorLabel));
		});
		UiUtils.ForceRealodObject(netCanvasObject);
	}

	static IEnumerator Coroutine(PreRunScript instance, string ipAddress, TextMeshProUGUI errorLabel)
	{
		try
		{
			Debug.Log(ipAddress);
			var split = ipAddress.Split(':');
			if (split.Length != 2)
				throw new Exception("colon");
			uint parsed = uint.Parse(split[1]);
			var addr = IPAddress.Parse(split[0]);
			var ep = new IPEndPoint(addr, (int)parsed);
			var client = new Socket(ep.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
			client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);
			client.Connect(ep);
			_servInfo = new Client(client);
			_servInfo.Start();
			Debug.Log("Connection accepted, receiving handshake");
		}
		catch (Exception e)
		{
			errorLabel.text = e.Message;
			yield break;
		}
		yield return null;
		Debug.Log("Received handshake");
		yield return _servInfo.WaitUntilHasData();
		_worldInfo = _servInfo.Dequeue<WorldInfo>();
		_askForWorld = true;
		yield return instance.WaitLoad();
	}

	static WorldInfo _worldInfo;

	[HarmonyPrefix]
	[HarmonyPatch(typeof(global::WorldGeneration), "InstantiateWorld")]
	static void PreRunScriptInstantiateWorldPrefix(ref bool generate)
	{
		if (!_askForWorld)
		{
			_masterData = new();
			_masterData.WorldGenSeed = UnityEngine.Random.state;
		}
		else
		{
			generate = true;
			WorldGeneration.world.chunkWidth = _worldInfo.NumChunksX;
			WorldGeneration.world.chunkHeight = _worldInfo.NumChunksY;
			WorldGeneration.world.biomeDepth = _worldInfo.BiomeDepth;
			UnityEngine.Random.state = _worldInfo.WorldGenSeed;
		}
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(global::WorldGeneration), nameof(global::WorldGeneration.FinishWorldGeneration))]
	static void WorldGenerationFinishWorldGenerationPostfix(WorldGeneration __instance, ref IEnumerator __result)
	{
		static IEnumerator ExecutePostfix(WorldGeneration instance, IEnumerator coroutine)
		{
			if (_servInfo == null)
			{
				Debug.Log("World gen finished, instantiating socket");
				
				var ep = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 5000);
				_masterData.Server = new(ep);
				_masterData.Server.Run();
				_destroyedEntityIds = new();
			}
			else
			{
				Debug.Log("World gen finished, fixing world");
				
				yield return _servInfo.WaitUntilHasData();
				var modifiedBlocks = _servInfo.Dequeue<ModifiedBlocksInfo>();
				foreach (var kv in modifiedBlocks.Entries)
				{
					instance.SetBlock(kv.Key, kv.Value);
				}
				yield return _servInfo.WaitUntilHasData();
				var destroyedEntities = _servInfo.Dequeue<DestroyedEntitiesInfo>();
				var reverseMap = _entityIdentifierMap.ToDictionary(x => x.Value, x => x.Key);
				foreach (var id in destroyedEntities.Entries)
				{
					if (reverseMap.TryGetValue(id, out BuildingEntity e) && e)
						Object.Destroy(e.gameObject);
				}
			}
			while (coroutine.MoveNext())
				yield return coroutine.Current;
			_isWorldInitFinished = true;
		}
		__result = ExecutePostfix(__instance, __result);
	}

	private static readonly Dictionary<Vector2Int, ushort> _modifiedBlocks = new();

	[HarmonyPostfix]
	[HarmonyPatch(typeof(global::WorldGeneration), nameof(global::WorldGeneration.SetBlock))]
	static void WorldGenerationSetBlockPostfix(Vector2Int pos, ushort block)
	{
		if (_isWorldInitFinished && _masterData != null)
			_modifiedBlocks[pos] = block;
	}

	private void OnDestroy()
	{
		_harmony?.UnpatchSelf();
	}

	private static Dictionary<BuildingEntity, int> _entityIdentifierMap = new();
	private static List<int> _destroyedEntityIds;
	private static int _currentMaxEntityId = 0;

	[HarmonyPostfix]
	[HarmonyPatch(typeof(global::BuildingEntity), nameof(global::BuildingEntity.Start))]
	static void BuildingEntityStartPostfix(BuildingEntity __instance)
	{
		if (!_entityIdentifierMap.ContainsKey(__instance))
			_entityIdentifierMap.Add(__instance, _currentMaxEntityId++);
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(global::BuildingEntity), "OnDestroy")]
	static void BuildingEntityOnDestroyPostfix(BuildingEntity __instance)
	{
		if (_masterData != null && _entityIdentifierMap.TryGetValue(__instance, out int id))
		{
			_destroyedEntityIds.Add(id);
			_entityIdentifierMap.Remove(__instance);
		}
	}
}
