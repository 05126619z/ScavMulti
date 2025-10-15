using System;
using MessagePack;
using MessagePack.Formatters;

namespace ScavMulti.Network.Messages;

internal class UnityRandomStateFormatter : IMessagePackFormatter<UnityEngine.Random.State>
{
	public void Serialize(ref MessagePackWriter writer, UnityEngine.Random.State value, MessagePackSerializerOptions options)
	{
		writer.WriteArrayHeader(4);
		writer.Write(value.s0);
		writer.Write(value.s1);
		writer.Write(value.s2);
		writer.Write(value.s3);
	}

	public UnityEngine.Random.State Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
	{
		if (reader.IsNil)
		{
			throw new InvalidOperationException("typecode is null, struct not supported");
		}

		var length = reader.ReadArrayHeader();
		var s0 = default(int);
		var s1 = default(int);
		var s2 = default(int);
		var s3 = default(int);
		for (int i = 0; i < length; i++)
		{
			var key = i;
			switch (key)
			{
				case 0:
					s0 = reader.ReadInt32();
					break;
				case 1:
					s1 = reader.ReadInt32();
					break;
				case 2:
					s2 = reader.ReadInt32();
					break;
				case 3:
					s3 = reader.ReadInt32();
					break;
				default:
					reader.Skip();
					break;
			}
		}

		var result = new UnityEngine.Random.State()
		{
			s0 = s0,
			s1 = s1,
			s2 = s2,
			s3 = s3
		};
		return result;
	}
}
