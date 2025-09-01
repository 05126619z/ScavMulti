using System;
using System.Threading;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;

namespace ScavMulti.Network;

public partial class Client
{
	public enum ClientState
	{
		NotRunningYet,
		Running,
		Cancelled,
		CancelledFromSend,
		CancelledFromRecv,
	}

	private class ClientCancellationContext : IDisposable
	{
		private readonly CancellationTokenSource _cts = new();
		private readonly object _sync = new();
		public CancellationToken Token => _cts.Token;
		public Exception CancelReason { get; private set; }
		public ClientCancelledException ClientCancelledException { get; private set; }
		public ClientState State { get; private set; } = ClientState.NotRunningYet;

		public void CancelFromSend(Exception cancelReason)
		{
			lock (_sync)
			{
				if (State != ClientState.NotRunningYet && !_cts.IsCancellationRequested)
				{
					CancelReason = cancelReason;
					ClientCancelledException = new("Send cancelled", cancelReason);
					State = ClientState.CancelledFromRecv;
					_cts.Cancel();
				}
			}
		}

		public void CancelFromRecv(Exception cancelReason)
		{
			lock (_sync)
			{
				if (State != ClientState.NotRunningYet && !_cts.IsCancellationRequested)
				{
					CancelReason = cancelReason;
					ClientCancelledException = new("Receive cancelled", cancelReason);
					State = ClientState.CancelledFromSend;
					_cts.Cancel();
				}
			}
		}

		public void Cancel()
		{
			lock (_sync)
			{
				if (State != ClientState.NotRunningYet && !_cts.IsCancellationRequested)
				{
					CancelReason = null;
					ClientCancelledException = new("Client stopped", null);
					State = ClientState.Cancelled;
					_cts.Cancel();
				}
			}
		}

		public void SetIsRunning()
		{
			if (State == ClientState.NotRunningYet)
				State = ClientState.Running;
		}

		public void Dispose()
		{
			_cts.Dispose();
		}
	}

	private class AwaitableEventArgs(Socket sock) : SocketAsyncEventArgs, IValueTaskSource<int>
	{
		protected Socket _sock = sock;
		ManualResetValueTaskSourceCore<int> _source = new();

		public int GetResult(short token)
		{
			int result = _source.GetResult(token);
			_source.Reset();
			return result;
		}

		public ValueTaskSourceStatus GetStatus(short token) => _source.GetStatus(token);

		public void OnCompleted(Action<object> continuation, object state, short token, ValueTaskSourceOnCompletedFlags flags)
			=> _source.OnCompleted(continuation, state, token, flags);

		protected override void OnCompleted(SocketAsyncEventArgs e)
		{
			var err = SocketError;
			if (err != SocketError.Success)
				_source.SetException(new SocketException((int)err));
			_source.SetResult(BytesTransferred);
		}
	}

	private class Sender(Socket sock) : AwaitableEventArgs(sock)
	{
		private short _token = 0;

		public ValueTask<int> SendAsync(byte[] buffer, int offset, int count)
		{
			SetBuffer(buffer, offset, count);
			if (_sock.SendAsync(this))
				return new ValueTask<int>(this, _token++);

			var err = SocketError;
			return err == SocketError.Success
				? new ValueTask<int>(BytesTransferred)
				: new ValueTask<int>(Task.FromException<int>(new SocketException((int)err)));
		}
	}

	private class Receiver(Socket sock) : AwaitableEventArgs(sock)
	{
		private short _token = 0;

		public ValueTask<int> ReceiveAsync(byte[] buffer, int offset, int count)
		{
			SetBuffer(buffer, offset, count);
			if (_sock.ReceiveAsync(this))
				return new ValueTask<int>(this, _token++);

			var err = SocketError;
			return err == SocketError.Success
				? new ValueTask<int>(BytesTransferred)
				: new ValueTask<int>(Task.FromException<int>(new SocketException((int)err)));
		}
	}
}
