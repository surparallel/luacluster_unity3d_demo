namespace KBEngine
{
	using System; 
	using System.Collections.Generic;
	using System.Threading;

    /// <summary>
    /// 事件模块: KBEngine插件层与Unity3D表现层通过事件来交互，特别是在多线程模式下较方便
	/// </summary>
    public class Event2
    {
		object toLock = new object();
		LinkedList<byte[]> firedEvents;
	
		public Event2()
		{
			firedEvents = new LinkedList<byte[]>();
		}	

		public  void monitor_Enter(object obj)
		{		
			Monitor.Enter(obj);
		}

		public  void monitor_Exit(object obj)
		{
			Monitor.Exit(obj);
		}

		public  void fire(byte[] buf)
		{
			_fire(firedEvents, buf);
		}
		
		private  void _fire(LinkedList<byte[]> firedEvents, byte[] buf)
		{
			monitor_Enter(toLock);		
			firedEvents.AddLast(buf);
			monitor_Exit(toLock);
		}
		
		public LinkedList<byte[]> processEvents()
		{
			LinkedList<byte[]> ret;
			monitor_Enter(toLock);
			ret = firedEvents;
			firedEvents = new LinkedList<byte[]>();
			monitor_Exit(toLock);

			return ret;
		} 
    }
} 
