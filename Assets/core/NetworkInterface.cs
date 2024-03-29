﻿namespace KBEngine
{
	using System;
	using System.Net.Sockets;
	using System.Net;
	using System.Text.RegularExpressions;
    using grpania_unity3d_demo;

    /// <summary>
    /// 网络模块
    /// 处理连接、收发数据
    /// </summary>
    public class NetworkInterface
	{
		public delegate void AsyncConnectMethod(ConnectState state);
		public const int TCP_PACKET_MAX = 50*1024*1024;


		public delegate void ConnectCallback(string ip, int port, bool success, object userData);

		protected Socket _socket = null;
		//protected EncryptionFilter _filter = null;
		PacketReceiver _packetReceiver = null;
		PacketSender _packetSender = null;

		public bool connected = false;
		
		public class ConnectState
		{
			// for connect
			public string connectIP = "";
			public int connectPort = 0;
			public ConnectCallback connectCB = null;
			public AsyncConnectMethod caller = null;
			public object userData = null;
			public Socket socket = null;
			public NetworkInterface networkInterface = null;
			public string error = "";
		}
		
		public NetworkInterface()
		{
			reset();
		}

		~NetworkInterface()
		{
			Dbg.DEBUG_MSG("NetworkInterface::~NetworkInterface(), destructed!!!");
			reset();
		}

		public virtual Socket sock()
		{
			return _socket;
		}
		
		public void reset()
		{
			_packetReceiver = null;
			_packetSender = null;
			connected = false;

			if(_socket != null)
			{
				if(_socket.RemoteEndPoint != null)
					Dbg.DEBUG_MSG(string.Format("NetworkInterface::reset(), close socket from '{0}'", _socket.RemoteEndPoint.ToString()));

				_socket.Close(0);
				_socket = null;
			}
		}
		

		public void close()
		{
		   if(_socket != null)
			{
				_socket.Close(0);
				_socket = null;
			}

			_socket = null;
			connected = false;
		}

		public virtual PacketReceiver packetReceiver()
		{
			return _packetReceiver;
		}
		
		public virtual bool valid()
		{
			return ((_socket != null) && (_socket.Connected == true));
		}
		
		public void _onConnectionState(ConnectState state)
		{
			bool success = (state.error == "" && valid());
			if (success)
			{
				Dbg.DEBUG_MSG(string.Format("NetworkInterface::_onConnectionState(), connect to {0} is success!", state.socket.RemoteEndPoint.ToString()));
				_packetReceiver = new PacketReceiver(this);
				_packetReceiver.startRecv();
				connected = true;
			}
			else
			{
				reset();
                //Dbg.ERROR_MSG(string.Format("NetworkInterface::_onConnectionState(), connect error! ip: {0}:{1}, err: {2}", state.connectIP, state.connectPort, state.error));
                Dbg.DEBUG_MSG(string.Format("NetworkInterface::_onConnectionState(), connect error! ip: {0}:{1}, err: {2}", state.connectIP, state.connectPort, state.error));
            }

			if (state.connectCB != null)
				state.connectCB(state.connectIP, state.connectPort, success, state.userData);
		}

		private static void connectCB(IAsyncResult ar)
		{
			ConnectState state = null;
			
			try 
			{
				// Retrieve the socket from the state object.
				state = (ConnectState) ar.AsyncState;

				// Complete the connection.
				state.socket.EndConnect(ar);
			} 
			catch (Exception e) 
			{
				state.error = e.ToString();
			}
		}

		/// <summary>
		/// 在非主线程执行：连接服务器
		/// </summary>
		private void _asyncConnect(ConnectState state)
		{
			Dbg.DEBUG_MSG(string.Format("NetWorkInterface::_asyncConnect(), will connect to '{0}:{1}' ...", state.connectIP, state.connectPort));
			try
			{
				state.socket.Connect(state.connectIP, state.connectPort);
			}
			catch (Exception e)
			{
                //Dbg.ERROR_MSG(string.Format("NetWorkInterface::_asyncConnect(), connect to '{0}:{1}' fault! error = '{2}'", state.connectIP, state.connectPort, e));
                Dbg.DEBUG_MSG(string.Format("NetWorkInterface::_asyncConnect(), connect to '{0}:{1}' fault! error = '{2}'", state.connectIP, state.connectPort, e));
                state.error = e.ToString();
            }
		}

		/// <summary>
		/// 在非主线程执行：连接服务器结果回调
		/// </summary>
		private void _asyncConnectCB(IAsyncResult ar)
		{
			ConnectState state = (ConnectState)ar.AsyncState;

			Dbg.DEBUG_MSG(string.Format("NetWorkInterface::_asyncConnectCB(), connect to '{0}:{1}' finish. error = '{2}'", state.connectIP, state.connectPort, state.error));

			// Call EndInvoke to retrieve the results.
			state.caller.EndInvoke(ar);

			_onConnectionState(state);
		}

		public void connectTo(string ip, int port, ConnectCallback callback, object userData)
		{
			if (valid())
				throw new InvalidOperationException("Have already connected!");

			if (!(new Regex(@"((?:(?:25[0-5]|2[0-4]\d|((1\d{2})|([1-9]?\d)))\.){3}(?:25[0-5]|2[0-4]\d|((1\d{2})|([1-9]?\d))))")).IsMatch(ip))
			{
				IPHostEntry ipHost = Dns.GetHostEntry(ip);
				ip = ipHost.AddressList[0].ToString();
			}

			// Security.PrefetchSocketPolicy(ip, 843);
			_socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _socket.SetSocketOption(System.Net.Sockets.SocketOptionLevel.Socket, SocketOptionName.ReceiveBuffer, NetworkInterface.TCP_PACKET_MAX * 2);
            _socket.SetSocketOption(System.Net.Sockets.SocketOptionLevel.Socket, SocketOptionName.SendBuffer, NetworkInterface.TCP_PACKET_MAX * 2);
            _socket.NoDelay = true;
			//_socket.Blocking = false;

			AsyncConnectMethod asyncConnectMethod = new AsyncConnectMethod(this._asyncConnect);

			ConnectState state = new ConnectState();
			state.connectIP = ip;
			state.connectPort = port;
			state.connectCB = callback;
			state.userData = userData;
			state.socket = _socket;
			state.networkInterface = this;
			state.caller = asyncConnectMethod;

			Dbg.DEBUG_MSG("connect to " + ip + ":" + port + " ...");
			connected = false;
			asyncConnectMethod.BeginInvoke(state, new AsyncCallback(this._asyncConnectCB), state);
		}

		public bool send(MemoryStream stream)
		{
			if (!valid())
			{
				throw new ArgumentException("invalid socket!");
			}

			if (_packetSender == null)
				_packetSender = new PacketSender(this);

			return _packetSender.send(stream);
		}

		public void process(Main gameMain)
		{
			if (!valid())
				return;

			if (_packetReceiver != null)
				_packetReceiver.process(gameMain);
		}
	}
}
