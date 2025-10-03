using MessagePack;

namespace ScavMulti.Network.Messages;

[Union(0, typeof(Error))]
[Union(1, typeof(PeerHandshake))]
[Union(2, typeof(WorldInfo))]
[Union(3, typeof(ModifiedBlocksInfo))]
public abstract record class MessageBase { }
