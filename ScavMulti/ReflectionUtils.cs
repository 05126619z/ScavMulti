namespace ScavMulti;

static class ReflectionUtils
{
	public static void ShallowCopyPropsOnly<T>(T src, T dest)
	{
		foreach (var prop in typeof(T).GetProperties())
		{
			if (prop.SetMethod != null && prop.GetMethod != null)
				prop.SetValue(dest, prop.GetValue(src));
		}
	}

	public static void ShallowCopyFieldsOnly<T>(T src, T dest)
	{
		foreach (var prop in typeof(T).GetFields())
		{
			prop.SetValue(dest, prop.GetValue(src));
		}
	}

	public static void ShallowCopy<T>(T src, T dest)
	{
		ShallowCopyPropsOnly(src, dest);
		ShallowCopyFieldsOnly(src, dest);
	}
}
