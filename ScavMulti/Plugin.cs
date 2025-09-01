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

[BepInPlugin(PluginInfo.PluginGUID, PluginInfo.PluginName, PluginInfo.PluginVersion)]
public class Plugin : BaseUnityPlugin
{
	public static Plugin Instance { get; private set; }
	private Harmony _harmony = null;

	private void Awake()
    {
		Instance = this;
		try
		{
			RefCollection.Init();
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
	static bool _isWorldGenFinished = false;

	[HarmonyPostfix]
	[HarmonyPatch(typeof(global::Body), "Start")]
	static void Body_Start_Postfix(Body __instance)
	{
		if (!_mainExperiment.Body)
		{
			Debug.Log("MainBody::Start");
			_mainExperiment = new(__instance.transform.parent.gameObject, __instance);
			_isWorldGenFinished = false;
		}
		// else
		// {
		// 	Debug.Log("OtherBody::Start");
		// 	_otherExperiment = new(__instance.transform.parent.gameObject, __instance);
		// }
	}

	static void SendChunkBackgroundData(Client client)
	{
		var spriteRenderers = WorldGeneration.world.worldGrid.transform.GetComponentsInChildren<SpriteRenderer>();
		var chunkObjects = spriteRenderers.Where(x => x.gameObject.name == "ChunkBack");
		var uniqueSpriteNames = chunkObjects.Select(x => x.sprite).Distinct().Select(AssetResolver.GetAssetPath).ToArray();
		client.Enqueue(new ChunkBackgroundSpriteNames(uniqueSpriteNames));
		var bgsToSend = new List<ChunkBackground.Entry>();
		for (uint x = 0; x < WorldGeneration.world.chunkWidth; x++)
		{
			for (uint y = 0; y < WorldGeneration.world.chunkHeight; y++)
			{
				var chunkObject = WorldGeneration.world.renderChunks[x, y].GetComponentsInChildren<SpriteRenderer>().Where(x => x.gameObject.name == "ChunkBack").FirstOrDefault();
				if (chunkObject)
				{
					bgsToSend.Add(new ChunkBackground.Entry(x, y, Array.IndexOf(uniqueSpriteNames, AssetResolver.GetAssetPath(chunkObject.sprite))));
				}
			}
		}
		client.Enqueue(new ChunkBackground(bgsToSend.ToArray()));
		var wallHoleObjects = spriteRenderers.Where(x => x.gameObject.name.StartsWith("wallholes"));
		var wallHolesToSend = new List<WallHoles.WallHole>(wallHoleObjects.Count());
		foreach (var wallHole in wallHoleObjects)
		{
			var transform = wallHole.transform;
			wallHolesToSend.Add(new WallHoles.WallHole(
				transform.position.x, transform.position.y,
				transform.rotation.eulerAngles.z,
				wallHole.GetComponent<AudioSource>().pitch
			));
		}
		client.Enqueue(new WallHoles(wallHolesToSend.ToArray()));
	}

	static void SendChunk(Client client, uint chunkX, uint chunkY)
	{
		var world = WorldGeneration.world;
		Debug.Log($"Sending chunk {chunkX}x{chunkY}...");
		if (chunkX > world.width || chunkY > world.height)
		{
			Debug.LogError("Invalid chunk requested");
			client.Enqueue(new Error(false, "Out Of Bounds chunk requested"));
			return;
		}
		var chunkSize = WorldGeneration.CHUNKSIZE;
		var basePos = new Vector2Int((int)chunkX, (int)chunkY) * chunkSize;
		var blockData = new ushort[chunkSize, chunkSize];
		var worldBlocks = world.worldBlocks;
		for (int y = 0; y < chunkSize; y++)
		{
			for (int x = 0; x < chunkSize; x++)
			{
				var c = worldBlocks[basePos.x + x, basePos.y + y];
				blockData[x, y] = c;
			}
		}
		var response = new ChunkInfo(blockData);
		client.Enqueue(response);
	}

	static void MainBodyUpdatePostfix()
	{
		foreach (var deadClient in _server.RemoveDeadClients())
		{
			Debug.LogWarning($"Client is leaving. Exception: {deadClient.ClientCancelledException}");
		}
		foreach (var client in _server)
		{
			if (client.IsRunning && !client.IsEmpty)
			{
				var data = client.Dequeue();
				switch (data)
				{
					case ChunkRequest chunkRequest:
						SendChunk(client, chunkRequest.X, chunkRequest.Y);
						break;
					case ChunkRequestWhole:
						for (uint x = 0; x < WorldGeneration.world.chunkWidth; x++)
						{
							for (uint y = 0; y < WorldGeneration.world.chunkHeight; y++)
							{
								SendChunk(client, x, y);
							}
						}
						break;
					case ChunkBackgroundRequest:
						SendChunkBackgroundData(client);
						break;
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

	static Server _server;

	static Client _servInfo;

	[HarmonyPostfix]
	[HarmonyPatch(typeof(global::Body), "Update")]
	static void Body_Update_Postfix(Body __instance)
	{
		if (!_isWorldGenFinished)
		{
			if (!WorldGeneration.world.generatingWorld)
			{
				if (_servInfo == null)
				{
					Debug.Log("World gen finished, instantiating socket");
					
					var ep = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 5000);
					_server = new(ep);
					_server.Run();
				}
				else
					Debug.Log("World transfer finished");
				_isWorldGenFinished = true;
			}
		}
		else
		{
			if (__instance == _mainExperiment.Body && _server != null)
				MainBodyUpdatePostfix();
			else
				OtherBodyUpdatePostfix();
		}
	}

	[HarmonyPrefix]
	[HarmonyPatch(typeof(global::Body), "Update")]
	static void Body_Update_Prefix(Body __instance)
	{
		if (_isWorldGenFinished)
		{
			if (_server != null)
			{
				while (_server.NextPendingClient(out var pendingClient))
				{
					Debug.Log("client pending !!");

					_server.AcceptClient(pendingClient);

					pendingClient.Enqueue(new WorldInfo(
						WorldGeneration.world.chunkWidth,
						WorldGeneration.world.chunkHeight,
						(uint)WorldGeneration.CHUNKSIZE,
						__instance.transform.position.x,
						__instance.transform.position.y,
						WorldGeneration.world.biomeDepth));
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
		yield return null;
		_askForWorld = true;
		yield return instance.WaitLoad();
	}

	static IEnumerator DownloadWorldCoroutine(WorldGeneration instance)
	{
        instance.generatingWorld = true;
        PlayerCamera.main.body.transform.position = Vector3.zero;
        instance.loadingObject.SetActive(true);
		// instance.currentTempCurve = biomeDepth;
		yield return _servInfo.WaitUntilHasData();
		var worldInfo = _servInfo.Dequeue<WorldInfo>();
		Debug.Log($"worldInfo: {worldInfo.NumChunksX}x{worldInfo.NumChunksY}");
		var chunkSize = (int)worldInfo.ChunkSize;
		var worldBlocks = instance.worldBlocks;
		instance.biomeDepth = worldInfo.BiomeDepth;
		instance.UpdateBiomePostProcess();
		var timer = new System.Diagnostics.Stopwatch();
		timer.Start();
		// _servInfo.Enqueue(new ChunkRequestWhole());
		for (uint x = 0; x < worldInfo.NumChunksX; x++)
		{
			for (uint y = 0; y < worldInfo.NumChunksY; y++)
			{
				instance.ChangeLoadingText($"Downloading chunk {x}x{y}...");
				_servInfo.Enqueue(new ChunkRequest(x, y));
				yield return _servInfo.WaitUntilHasData();
				var chunkInfo = _servInfo.Dequeue<ChunkInfo>();
				var blockData = chunkInfo.Data;
				var basePos = new Vector2Int((int)x, (int)y) * chunkSize;
				for (int y2 = 0; y2 < chunkSize; y2++)
				{
					for (int x2 = 0; x2 < chunkSize; x2++)
					{
						ushort c = blockData[x2, y2];
						worldBlocks[basePos.x + x2, basePos.y + y2] = c;
					}
				}
			}
		}
		instance.ChangeLoadingText("Downloading chunk backgrounds...");
		_servInfo.Enqueue(new ChunkBackgroundRequest());
		yield return _servInfo.WaitUntilHasData();
		var backgroundSpriteNames = _servInfo.Dequeue<ChunkBackgroundSpriteNames>().Names;
		yield return _servInfo.WaitUntilHasData();
		var backgroundSprites = _servInfo.Dequeue<ChunkBackground>().Entries;
		var chunks = instance.chunks;
		foreach (var entry in backgroundSprites)
			instance.CreateBackground(backgroundSpriteNames[entry.NameIndex], chunks[entry.X, entry.Y]);
		instance.ChangeLoadingText("Downloading wall holes...");
		yield return _servInfo.WaitUntilHasData();
		var wallholes = _servInfo.Dequeue<WallHoles>().Entries;
		foreach (var entry in wallholes)
		{
			var v = new Vector2(entry.X, entry.Y);
			UnityEngine.Object.Instantiate(
				Resources.Load<GameObject>("Special/wallholes"),
				v,
				Quaternion.Euler(new Vector3(0, 0, entry.Rotation)),
				instance.GetClosestChunk(instance.WorldToBlockPos(v)).transform
			).GetComponent<AudioSource>().pitch = entry.WindPitch;
		}

		timer.Stop();
		Debug.Log($"Elapsed: {timer.Elapsed}");

		instance.ChangeLoadingText("Updating world tiles...");
		instance.UpdateWorld();
		instance.generatingWorld = false;
		instance.DisableAllChunks();
        instance.UpdateChunkVisibility();
        instance.timeSinceFinishedGeneration = 0f;
        instance.loadingObject.SetActive(false);
		PlayerCamera.main.body.transform.position = new Vector2(worldInfo.CurrentExperimentPosX, worldInfo.CurrentExperimentPosY);
	}

	[HarmonyPrefix]
	[HarmonyPatch(typeof(global::WorldGeneration), "GenerateWorld")]
	static bool PreRunScriptGenerateWorldPrefix(ref IEnumerator __result, WorldGeneration __instance)
	{
		if (!_askForWorld)
			return true;
		__result = DownloadWorldCoroutine(__instance);
		return false;
	}

	private void OnDestroy()
	{
		_harmony?.UnpatchSelf();
	}
}
