using System.Collections.Generic;
using MessagePack;
using UnityEngine;

namespace ScavMulti.Network.Messages;

[MessagePackObject]
public record class Error(
	[property: Key(0)] bool IsFatal,
	[property: Key(1)] string Message
) : MessageBase;

[MessagePackObject]
public record class PeerHandshake : MessageBase;

/// <summary>
/// sent by the server after the handshake
/// </summary>
[MessagePackObject]
public record class WorldInfo(
	[property: Key(0)] uint NumChunksX,
	[property: Key(1)] uint NumChunksY,
	[property: Key(2)] uint ChunkSize,
	[property: Key(3)] float CurrentExperimentPosX,
	[property: Key(4)] float CurrentExperimentPosY,
	[property: Key(5)] UnityEngine.Random.State WorldGenSeed,
	[property: Key(6)] int BiomeDepth 
) : MessageBase;

[MessagePackObject]
public record class ModifiedBlocksInfo(
	[property: Key(0)] Dictionary<Vector2Int, ushort> Entries
) : MessageBase;
