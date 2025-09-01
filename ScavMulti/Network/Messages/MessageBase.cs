using MessagePack;

namespace ScavMulti.Network.Messages;

[Union(0, typeof(Error))]
[Union(1, typeof(PeerHandshake))]
[Union(2, typeof(WorldInfo))]
[Union(3, typeof(ChunkRequest))]
[Union(4, typeof(ChunkRequestWhole))]
[Union(5, typeof(ChunkInfo))]
[Union(6, typeof(ChunkBackgroundRequest))]
[Union(7, typeof(ChunkBackgroundSpriteNames))]
[Union(8, typeof(ChunkBackground))]
[Union(9, typeof(WallHoles))]
public abstract record class MessageBase { }
