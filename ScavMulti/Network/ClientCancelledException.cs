using System;

namespace ScavMulti.Network;

public class ClientCancelledException : Exception
{
	public ClientCancelledException() { }
	public ClientCancelledException(string message) : base(message) { }
	public ClientCancelledException(string message, Exception inner) : base(message, inner) { }
}