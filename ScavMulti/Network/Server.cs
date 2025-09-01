using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace ScavMulti.Network;

public class Server : IEnumerable<Client>, IDisposable
{
	private const int DEFAULT_BACKLOG = 10;

	private readonly TcpListener _listener;
	private readonly ConcurrentBag<Client> _pendingClients;
	private readonly List<Client> _clients;
	public bool IsRunning { get; private set; }

	public Server(IPEndPoint ep)
	{
		_listener = new(ep);
		_listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);
		_clients = new(10);
		_pendingClients = new();
		IsRunning = false;
	}

	void AssertIsRunning()
	{
		if (!IsRunning)
			throw new InvalidOperationException("Server is not running");
	}

	void AssertIsNotRunning()
	{
		if (IsRunning)
			throw new InvalidOperationException("Server is already running");
	}

	private void OnClientConnected(IAsyncResult result)
	{
		try
		{
			var clientSock = _listener.EndAcceptSocket(result);
			var client = new Client(clientSock);
			_pendingClients.Add(client);
		}
		catch (Exception e)
		{
			if (e is SocketException or ObjectDisposedException)
			{
				UnityEngine.Debug.LogError($"error while accepting tcp client: {e}");
			}
			else throw;
		}
		_listener.BeginAcceptSocket(OnClientConnected, null);
	}

	public void Run(int backlog = DEFAULT_BACKLOG)
	{
		AssertIsNotRunning();
		_listener.Start(backlog);
		_listener.BeginAcceptSocket(OnClientConnected, null);
		IsRunning = true;
	}

	public bool NextPendingClient(out Client pendingClient)
	{
		AssertIsRunning();
		return _pendingClients.TryPeek(out pendingClient);
	}

	public void AcceptClient(Client client)
	{
		AssertIsRunning();
		if (!_pendingClients.TryTake(out Client actualPendingClient) || client != actualPendingClient)
		{
			if (actualPendingClient != null)
				_pendingClients.Add(actualPendingClient);
			throw new InvalidOperationException("Not the current pending client");
		}
		client.Start();
		_clients.Add(client);
	}

	public void RefuseClient(Client client)
	{
		AssertIsRunning();
		if (!_pendingClients.TryTake(out Client actualPendingClient) || client != actualPendingClient)
		{
			if (actualPendingClient != null)
				_pendingClients.Add(actualPendingClient);
			throw new InvalidOperationException("Not the current pending client");
		}
		client.Dispose();
	}

	public void KillClient(Client client, bool throwOnNotFound = true)
	{
		AssertIsRunning();
		var clientIndex = _clients.IndexOf(client);
		if (clientIndex >= 0)
		{
			_clients.RemoveAt(clientIndex);
			client.Dispose();
		}
		else if (throwOnNotFound)
			throw new InvalidOperationException("client is not currently in the clients list");
	}

	/// <summary>
    /// Just removes the client(s) from the server's internal list.
    /// The socket should ALREADY be disposed and null, don't access it
    /// </summary>
	public IReadOnlyCollection<Client> RemoveDeadClients()
	{
		AssertIsRunning();
		var list = new List<Client>(5);
		_clients.RemoveAll(x =>
		{
			if (!x.IsRunning)
			{
				list.Add(x);
				return true;
			}
			return false;
		});
		return list;
	}

	public IEnumerator<Client> GetEnumerator()
	{
		AssertIsRunning();
		lock (_clients)
		{
			foreach (var client in _clients)
			{
				yield return client;
			}
		}
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		AssertIsRunning();
		return GetEnumerator();
	}

	private bool _disposed = false;
	public void Dispose()
	{
		if (_disposed)
			return;
		// TODO
		_disposed = true;
	}
}
