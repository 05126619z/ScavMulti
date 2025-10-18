using System.ComponentModel;

namespace System.Runtime.CompilerServices;

// Little hack to allow to use C#9 's record struct in .NET 4.x
// https://blog.ndepend.com/using-c9-record-and-init-property-in-your-net-framework-4-x-net-standard-and-net-core-projects/

/// <summary>
/// Reserved to be used by the compiler for tracking metadata.
/// This class should not be used by developers in source code.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
internal static class IsExternalInit { }
