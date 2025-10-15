using MessagePack;

namespace ScavMulti;

public static class Constants
{
	public static readonly MessagePackSerializerOptions MessagePackSerializerOptions
		= MessagePackSerializerOptions.Standard
			.WithResolver(ScavMulti.Network.Messages.CustomResolver.Instance);
}
