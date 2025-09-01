using MessagePack;

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
	[property: Key(5)] int BiomeDepth 
) : MessageBase;

/// <summary>
/// requests a specific ChunkInfo
/// </summary>
[MessagePackObject]
public record class ChunkRequest(
	[property: Key(0)] uint X,
	[property: Key(1)] uint Y
) : MessageBase;

/// <summary>
/// requests the entire world chunks as separate ChunkInfoes (columns then rows)
/// </summary>
[MessagePackObject]
public record class ChunkRequestWhole : MessageBase;

/// <param name="Data">a two-dimensional array representing a block matrix (columns then rows)</param>
[MessagePackObject]
public record class ChunkInfo(
	[property: Key(0)] ushort[,] Data
) : MessageBase;

[MessagePackObject]
public record class ChunkBackgroundRequest : MessageBase;

[MessagePackObject]
public record class ChunkBackgroundSpriteNames(
	[property: Key(0)] string[] Names
) : MessageBase;

[MessagePackObject]
public record class ChunkBackground(
	[property: Key(0)] ChunkBackground.Entry[] Entries
) : MessageBase
{
	[MessagePackObject]
	public record class Entry(
		[property: Key(0)] uint X,
		[property: Key(1)] uint Y,
		[property: Key(2)] int NameIndex
	);
}

[MessagePackObject]
public record class WallHoles(
	[property: Key(0)] WallHoles.WallHole[] Entries
) : MessageBase
{
	[MessagePackObject]
	public record class WallHole(
		[property: Key(0)] float X,
		[property: Key(1)] float Y,
		[property: Key(2)] float Rotation,
		[property: Key(3)] float WindPitch
	);
}
