namespace KBEngine
{
    using Boid.PureECS.Sample4;
    using grpania_unity3d_demo;
    using scopely.msgpacksharp;
    using System;
    using System.Collections.Generic;
    using System.Net.Sockets; 
	using System.Threading;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Mathematics;
    using Unity.Transforms;
    using UnityEngine;

    /*
		包接收模块(与服务端网络部分的名称对应)
		处理网络数据的接收
	*/
    public class PacketReceiver
	{
		public delegate void AsyncReceiveMethod();
		public delegate void AsyncReceiveMethod2(int i);
		private NetworkInterface _networkInterface = null;

		private byte[] _buffer;
		static uint listLen = 16;
		private List<Event2> _bufferList;

		private object _toLock = new object();
		private HashSet<ulong> _statusList;

		private Dictionary<ulong, Entity> entity_object;
		ulong pid = 0;

		private int step = 0;
		private int len = 0;
		private Event2 _mainBuffer;
		void addStatus(ulong entity)
        {
			Monitor.Enter(this._toLock);
			_statusList.Add(entity);
			Monitor.Exit(this._toLock);
		}

		HashSet<ulong> procesStatus()
        {
			HashSet<ulong> ret;
			Monitor.Enter(this._toLock);
			ret = _statusList;
			_statusList = new HashSet<ulong>();
			Monitor.Exit(this._toLock);
			return ret;
		}

		public PacketReceiver(NetworkInterface networkInterface)
		{
			entity_object = new Dictionary<ulong, Entity>();
			_init(networkInterface);
		}

		~PacketReceiver()
		{
			Dbg.DEBUG_MSG("PacketReceiver::~PacketReceiver(), destroyed!");
		}

		void _init(NetworkInterface networkInterface)
		{
			_networkInterface = networkInterface;
			_buffer = new byte[NetworkInterface.TCP_PACKET_MAX];

			_mainBuffer = new Event2();
			_statusList = new HashSet<ulong>();
			_bufferList = new List<Event2>();
			for (int j = 0; j < listLen; j++)
			{
				_bufferList.Add(new Event2());
			}
			Dbg.DEBUG_MSG("PacketReceiver::PacketReceiver(), _init2!");
		}

		public NetworkInterface networkInterface()
		{
			return _networkInterface;
		}

		public void process(Main gameMain)
		{
			Bootstrap.Instance.CompleteAllJobs();

			LinkedList<byte[]> processData = _mainBuffer.processEvents();
			if(processData.Count != 0)
            {
				foreach (byte[] buf in processData)
				{
					MemoryStream ms = new MemoryStream();
					ms.append(buf, 0, (uint)buf.Length);
					var len = ms.readUint32();
					var proto = ms.readUint8();

					ulong did = 0;
					if (proto == 2)
					{
						did = ms.readUint64();
					}
					else if (proto == 4)
					{
						did = ms.readUint64();
						ulong pid = ms.readUint64();
					}

					byte[] bufmsg = ms.getbuffer();
					MsgPackSerializer.DeserializeRet ret = new MsgPackSerializer.DeserializeRet();
					ret = MsgPackSerializer.DeserializeObject2(bufmsg, 0);
					if ((string)ret.o == "Pong")
					{
						ret = MsgPackSerializer.DeserializeObject2(bufmsg, ret.numRead);
						ret.o = Convert.ChangeType(ret.o, typeof(uint));
						uint count = (uint)ret.o;
						gameMain.player_entity.Pong(count);
					}
				}
            }

			HashSet<ulong> status = procesStatus();
			//添加新创建的对象和筛选要启动的线程
			foreach (ulong id in status)
            {
                if (!entity_object.ContainsKey(id))
                {
					Entity myentity = Bootstrap.Instance.Create();
					entity_object[id] = myentity;
				}
			}

			WaitHandle[] wharrary = new WaitHandle[listLen];
			//释放线程处理消息队列
			for (int i = 0; i < listLen; i++)
			{
				AsyncReceiveMethod2 asyncReceiveMethod = new AsyncReceiveMethod2(this._asyncProcess);
				IAsyncResult ar = asyncReceiveMethod.BeginInvoke(i, new AsyncCallback(_onProcess), asyncReceiveMethod);
				wharrary[i] = ar.AsyncWaitHandle;
			}

			if (WaitHandle.WaitAll(wharrary))
			{
				Console.WriteLine("Files written - main exiting.");
			}
			else
			{
				// The wait operation times out.
				Console.WriteLine("Error writing files - main exiting.");
			}
		}
		public void startRecv()
		{
			AsyncReceiveMethod asyncReceiveMethod = new AsyncReceiveMethod(this._asyncReceive);
			asyncReceiveMethod.BeginInvoke(new AsyncCallback(_onRecv), asyncReceiveMethod);
		}

		private void processBuf(byte[] pBuf)
        {
			MemoryStream ms = new MemoryStream();
			ms.append(pBuf, 0, (uint)pBuf.Length);

			uint len = ms.readUint32();
			uint proto = ms.readUint8();
			ulong did = 0;
			if (proto == 4)
			{
				did = ms.readUint64();
				this.pid = ms.readUint64();
			}
			else if (proto == 2)
			{
				did = ms.readUint64();
            }
            else if (proto == 12)
			{

				did = ms.readUint64();
				byte[] bufmsg = ms.getbuffer();
				MemoryStream cms = new MemoryStream();
				cms.append(bufmsg, 0, (uint)bufmsg.Length);
				while (true)
				{
					int rpos = cms.rpos;
					int clen = (int)cms.readUint32();
					cms.rpos = rpos;
					if (clen > cms.length())
						break;

					byte[] cbuf = cms.getbuffer(clen);
					processBuf(cbuf);

					if (cms.length() == 0)
						break;
				}
				return;
			}

			if (did == this.pid)
			{
				byte[] bufmsg = ms.getbuffer();
				MsgPackSerializer.DeserializeRet ret = new MsgPackSerializer.DeserializeRet();
				ret = MsgPackSerializer.DeserializeObject2(bufmsg, 0);

				ms.rpos = 0;
				byte[] buf = ms.getbuffer();
				//发送给玩家的消息要单独处理
				if ((string)ret.o == "Pong")
				{
					_mainBuffer.fire(buf);
				}
				else if ((string)ret.o == "OnEntryWorld")
				{
				}
			}
			else
			{
				System.Diagnostics.Debug.Assert(pBuf.Length != 0);
				ms.rpos = 0;
				ulong list = did % listLen;
				addStatus(did);
				_bufferList[(int)list].fire(pBuf);
			}
		}
		private void _asyncReceive()
		{
			if (_networkInterface == null || !_networkInterface.valid())
			{
				Dbg.WARNING_MSG("PacketReceiver::_asyncReceive(): network interface invalid!");
				return;
			}

			var socket = _networkInterface.sock();
			MemoryStream ms = new MemoryStream();
			while (true)
			{
				int bytesRead = 0;
				try
				{
					bytesRead = socket.Receive(_buffer);
				}
				catch (SocketException se)
				{
					Dbg.ERROR_MSG(string.Format("PacketReceiver::_asyncReceive(): receive error, disconnect from '{0}'! error = '{1}'", socket.RemoteEndPoint, se));
					return;
				}
				try
				{
					while (true)
					{
						if (step == 0)
						{
							ms.append(_buffer, 0, (uint)bytesRead);
							if (ms.length() < sizeof(uint))
							{
								Dbg.ERROR_MSG("uint");
								break;
							}
							int rpos = ms.rpos;
							len = (int)ms.readUint32();
							ms.rpos = rpos;
							step = 1;
						}
						else if (step == 1)
						{
							if (ms.length() > len)
							{
								byte[] buf = ms.getbuffer(len);
								//处理封包
								processBuf(buf);
								//继续处理剩余部分
								step = 3;
							} else if (ms.length() == len)
							{
								byte[] buf = ms.getbuffer(len);
								//处理封包
								processBuf(buf);

								step = 0;
								ms.clear();
								//跳出去重新开始
								break;
							}
							else if (ms.length() < len)
							{
								step = 2;
								//跳出去收缓冲区
								if(ms.rpos != 0)
								{
									//删除已经读取的部分
									byte[] tbuf = ms.getbuffer();
									ms.clear();
									ms.append(tbuf, 0, (uint)tbuf.Length);
								}					 
								break;
							}
						}
						else if(step == 2)
						{
							ms.append(_buffer, 0, (uint)bytesRead);
							if (ms.length() < len)
							{
								//长度不够继续收		
								break;
							}
							else
							{
								step = 1;
							}
						}
						else if(step == 3)
						{
							int rpos = ms.rpos;
							len = (int)ms.readUint32();
                            if (len < 0)
                            {
								Dbg.ERROR_MSG("int");
								return;
							}

							ms.rpos = rpos;
							step = 1;
						}
					}

				}
				catch (ObjectDisposedException se)
				{
					Dbg.ERROR_MSG(string.Format("PacketReceiver::_asyncReceive()::processBuf  '{0}'! error = '{1}'", socket.RemoteEndPoint, se));
					return;
				}
			}
		}

		private void _onRecv(IAsyncResult ar)
		{
			try
			{
				AsyncReceiveMethod caller = (AsyncReceiveMethod)ar.AsyncState;
				caller.EndInvoke(ar);
			}
			catch (ObjectDisposedException)
			{
				//通常出现这个错误, 是因为longin_baseapp时, networkInterface已经reset, _packetReceiver被置为null, 而之后刚好该回调被调用
			}
		}

		private void _onProcess(IAsyncResult ar)
		{
			try
			{
				AsyncReceiveMethod2 caller = (AsyncReceiveMethod2)ar.AsyncState;
				caller.EndInvoke(ar);
			}
			catch (ObjectDisposedException)
			{
				//通常出现这个错误, 是因为longin_baseapp时, networkInterface已经reset, _packetReceiver被置为null, 而之后刚好该回调被调用
			}
		}
		public static quaternion unityEulerToQuaternion(float3 v)
		{
			return unityEulerToQuaternion(v.y, v.x, v.z);
		}

		public static quaternion unityEulerToQuaternion(float yaw, float pitch, float roll)
		{
			yaw = math.radians(yaw);
			pitch = math.radians(pitch);
			roll = math.radians(roll);

			float rollOver2 = roll * 0.5f;
			float sinRollOver2 = (float)math.sin((double)rollOver2);
			float cosRollOver2 = (float)math.cos((double)rollOver2);
			float pitchOver2 = pitch * 0.5f;
			float sinPitchOver2 = (float)math.sin((double)pitchOver2);
			float cosPitchOver2 = (float)math.cos((double)pitchOver2);
			float yawOver2 = yaw * 0.5f;
			float sinYawOver2 = (float)math.sin((double)yawOver2);
			float cosYawOver2 = (float)math.cos((double)yawOver2);
			float4 result;
			result.w = cosYawOver2 * cosPitchOver2 * cosRollOver2 + sinYawOver2 * sinPitchOver2 * sinRollOver2;
			result.x = cosYawOver2 * sinPitchOver2 * cosRollOver2 + sinYawOver2 * cosPitchOver2 * sinRollOver2;
			result.y = sinYawOver2 * cosPitchOver2 * cosRollOver2 - cosYawOver2 * sinPitchOver2 * sinRollOver2;
			result.z = cosYawOver2 * cosPitchOver2 * sinRollOver2 - sinYawOver2 * sinPitchOver2 * cosRollOver2;

			return new quaternion(result);
		}

		public static float3 unityQuaternionToEuler(quaternion q2)
		{
			float4 q1 = q2.value;

			float sqw = q1.w * q1.w;
			float sqx = q1.x * q1.x;
			float sqy = q1.y * q1.y;
			float sqz = q1.z * q1.z;
			float unit = sqx + sqy + sqz + sqw; // if normalised is one, otherwise is correction factor
			float test = q1.x * q1.w - q1.y * q1.z;
			float3 v;

			if (test > 0.4995f * unit)
			{ // singularity at north pole
				v.z = 2f * math.atan2(q1.y, q1.x);
				v.y = math.PI / 2;
				v.x = 0;
				return NormalizeAngles(math.degrees(v));
			}
			if (test < -0.4995f * unit)
			{ // singularity at south pole
				v.z = -2f * math.atan2(q1.y, q1.x);
				v.y = -math.PI / 2;
				v.x = 0;
				return NormalizeAngles(math.degrees(v));
			}

			quaternion q3 = new quaternion(q1.w, q1.z, q1.x, q1.y);
			float4 q = q3.value;

			v.z = math.atan2(2f * q.x * q.w + 2f * q.y * q.z, 1 - 2f * (q.z * q.z + q.w * q.w));   // Yaw
			v.y = math.asin(2f * (q.x * q.z - q.w * q.y));                                         // Pitch
			v.x = math.atan2(2f * q.x * q.y + 2f * q.z * q.w, 1 - 2f * (q.y * q.y + q.z * q.z));   // Roll

			return NormalizeAngles(math.degrees(v));
		}

		static float3 NormalizeAngles(float3 angles)
		{
			angles.x = NormalizeAngle(angles.x);
			angles.y = NormalizeAngle(angles.y);
			angles.z = NormalizeAngle(angles.z);
			return angles;
		}

		static float NormalizeAngle(float angle)
		{
			while (angle > 360)
				angle -= 360;
			while (angle < 0)
				angle += 360;
			return angle;
		}

		static ulong double2u64(double x)
		{

			ulong i = (ulong)x;
			if ( i <= 9007199254740992)
			{
				return i;
			}
			else
			{
				MemoryStream convert = new MemoryStream();
				convert.writeDouble(x);
				ulong uid = convert.readUint64();
				return uid;
			}
		}

		private void msgProcess(byte[] buf)
        {
			MemoryStream ms = new MemoryStream();
			ms.append(buf, 0, (uint)buf.Length);
			var len = ms.readUint32();
			var proto = ms.readUint8();
			ulong did = 0;

			if (proto == 2)
			{
				did = ms.readUint64();
			}
			else if (proto == 4)
			{
				did = ms.readUint64();
				ulong pid = ms.readUint64();
			}

			byte[] bufmsg = ms.getbuffer();

			MsgPackSerializer.DeserializeRet ret = new MsgPackSerializer.DeserializeRet();
			ret = MsgPackSerializer.DeserializeObject2(bufmsg, 0);
			string name = (string)ret.o;
			if (name == "OnMove")
			{
				ret = MsgPackSerializer.DeserializeObject2(bufmsg, ret.numRead);//0

				double id = (double)(ret.o);
				ulong uid = double2u64(id);

				if (!entity_object.ContainsKey(uid))
				{
					return;
				}
				Entity myentity = entity_object[uid];
				var manager = World.Active.GetOrCreateManager<EntityManager>();

				Position position = new Position();
				Origin origin = new Origin();
				ret = MsgPackSerializer.DeserializeObject2(bufmsg, ret.numRead);//1
				if (typeof(double) == ret.o.GetType())
					position.Value.x = (float)(double)(ret.o);
				else
					position.Value.x = (float)(ret.o);
				origin.Value.x = position.Value.x;
				ret = MsgPackSerializer.DeserializeObject2(bufmsg, ret.numRead);//2

				if (typeof(double) == ret.o.GetType())
					position.Value.y = (float)(double)(ret.o);
				else
					position.Value.y = (float)(ret.o);

				origin.Value.y = position.Value.y;
				ret = MsgPackSerializer.DeserializeObject2(bufmsg, ret.numRead);//3
				position.Value.z = (float)(double)(ret.o);

				if (typeof(double) == ret.o.GetType())
					position.Value.z = (float)(double)(ret.o);
				else
					position.Value.z = (float)(ret.o);

				origin.Value.z = position.Value.z;
				manager.SetComponentData<Position>(myentity, position);
				manager.SetComponentData<Origin>(myentity, origin);

				Rotation rotation = new Rotation();
				float3 euler;
				ret = MsgPackSerializer.DeserializeObject2(bufmsg, ret.numRead);//4
				if (typeof(double) == ret.o.GetType())
					euler.x = (float)(double)(ret.o) * Mathf.Deg2Rad;
				else
					euler.x = (float)(ret.o) * Mathf.Deg2Rad;

				float j = Mathf.PI / 2;
				if (euler.x != 0f)
				{
					j = -Mathf.PI / 2;
				}

				ret = MsgPackSerializer.DeserializeObject2(bufmsg, ret.numRead);//5
				if (typeof(double) == ret.o.GetType())
					euler.y = (float)(double)(ret.o) * Mathf.Deg2Rad + j;
				else
					euler.y = (float)(ret.o) * Mathf.Deg2Rad + j;

				ret = MsgPackSerializer.DeserializeObject2(bufmsg, ret.numRead);//6
				if (typeof(double) == ret.o.GetType())
					euler.z = (float)(double)(ret.o) * Mathf.Deg2Rad;
				else
					euler.z = (float)(ret.o) * Mathf.Deg2Rad;

				rotation.Value = quaternion.EulerZYX(euler);
				manager.SetComponentData<Rotation>(myentity, rotation);

				Velocity velocity = new Velocity();
				ret = MsgPackSerializer.DeserializeObject2(bufmsg, ret.numRead);//7
				if (typeof(double) == ret.o.GetType())
					velocity.Value = (float)(double)(ret.o);
				else
					velocity.Value = (float)(ret.o);

				ret = MsgPackSerializer.DeserializeObject2(bufmsg, ret.numRead);//8
				if (typeof(double) == ret.o.GetType())
					velocity.begin = (uint)(double)(ret.o);
				else
					velocity.begin = (uint)(float)(ret.o);

				ret = MsgPackSerializer.DeserializeObject2(bufmsg, ret.numRead);//9
				if (typeof(double) == ret.o.GetType())
					velocity.stop = (uint)(double)(ret.o);
				else
					velocity.stop = (uint)(float)(ret.o);

				velocity.dt = 0;
				manager.SetComponentData<Velocity>(myentity, velocity);

				//Dbg.ERROR_MSG(string.Format("OnMove point({0},{1},{2}) euler({3},{4},{5})", position.Value.x, position.Value.y, position.Value.z
				//						, euler.x * Mathf.Rad2Deg, euler.y * Mathf.Rad2Deg, euler.z * Mathf.Rad2Deg));
			}
			else if (name == "OnAddView")
			{
				//注意这里没有根据时间戳同步当前位置
				ret = MsgPackSerializer.DeserializeObject2(bufmsg, ret.numRead);
				Array myret = (Array)(ret.o);//jump 0

				double id = (double)myret.GetValue(0);
				ulong uid = double2u64(id);

				if (!entity_object.ContainsKey(uid))
				{
					return;
				}

				Entity myentity = entity_object[uid];
				var manager = World.Active.GetOrCreateManager<EntityManager>();

				Position position = new Position();
				Origin origin = new Origin();
				position.Value.x = (float)(myret.GetValue(1));
				origin.Value.x = position.Value.x;
				position.Value.y = (float)(myret.GetValue(2));
				origin.Value.y = position.Value.y;
				position.Value.z = (float)(myret.GetValue(3));
				origin.Value.z = position.Value.z;

				Rotation rotation = new Rotation();
				float3 euler;
				euler.x = (float)(myret.GetValue(4)) * Mathf.Deg2Rad;

				float j = Mathf.PI / 2;
				if (euler.x != 0f)
				{
					j = -Mathf.PI / 2;
				}

				euler.y = (float)(myret.GetValue(5)) * Mathf.Deg2Rad + j;
				euler.z = (float)(myret.GetValue(6)) * Mathf.Deg2Rad;
				rotation.Value = quaternion.EulerZYX(euler);


				Velocity velocity = new Velocity();
				velocity.Value = (float)(myret.GetValue(7));
				velocity.begin = (uint)(myret.GetValue(8));
				if (typeof(int) == myret.GetValue(9).GetType())
				{
					velocity.stop = 0;
				}
				else
				{
					velocity.stop = (uint)(myret.GetValue(9));
				}
				velocity.dt = Tool.TimeStamp() - velocity.begin;

				Vector3 right = new float3(0f, 0f, 1f);
				Quaternion q = new Quaternion(rotation.Value.value.x, rotation.Value.value.y, rotation.Value.value.z, rotation.Value.value.w);
				Vector3 length = q * right;
				var speed = velocity.Value;

				length.Scale(new Vector3(speed * velocity.dt, speed * velocity.dt, speed * velocity.dt));
				float3 fv = length;
				position.Value = origin.Value + fv;

				manager.SetComponentData<Position>(myentity, position);
				manager.SetComponentData<Origin>(myentity, origin);
				manager.SetComponentData<Rotation>(myentity, rotation);
				manager.SetComponentData<Velocity>(myentity, velocity);

				//Dbg.ERROR_MSG(string.Format("OnAddView point({0},{1},{2}) euler({3},{4},{5})", position.Value.x, position.Value.y, position.Value.z
				//	, euler.x * Mathf.Rad2Deg, euler.y * Mathf.Rad2Deg, euler.z * Mathf.Rad2Deg));
			}
			else if ((string)ret.o == "OnDelView")
			{
				ret = MsgPackSerializer.DeserializeObject2(bufmsg, ret.numRead);//0

				double id = (double)(ret.o);
				MemoryStream convert = new MemoryStream();
				convert.writeDouble(id);
				ulong uid = convert.readUint64();

				if (!entity_object.ContainsKey(uid))
				{
					return;
				}
				Entity myentity = entity_object[uid];
				var manager = World.Active.GetOrCreateManager<EntityManager>();
				manager.DestroyEntity(myentity);
				entity_object.Remove(uid);
			}
		}

		private void _asyncProcess(int i)
		{
			LinkedList<byte[]>  processData = _bufferList[i].processEvents();
			foreach(byte[] buf in processData)
            {
				msgProcess(buf);
			}
		}
	}
} 
