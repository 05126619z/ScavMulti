using MessagePack;
using MessagePack.Unity;

namespace ScavMulti;

public static class Constants
{
	public static readonly MessagePackSerializerOptions MessagePackSerializerOptions
		= MessagePackSerializerOptions.Standard
			.WithResolver(UnityResolver.InstanceWithStandardResolver);
}
