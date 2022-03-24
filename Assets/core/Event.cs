namespace KBEngine
{
	using System; 
	using System.Collections.Generic;
	using System.Threading;

    /// <summary>
    /// 事件模块: KBEngine插件层与Unity3D表现层通过事件来交互，特别是在多线程模式下较方便
	/// </summary>
    public class Event
    {
		public struct Pair
		{
			public object obj;
			public string funcname;
			public System.Reflection.MethodInfo method;
        };
		
		public struct EventObj
		{
			public Pair info;
            public string eventname;
            public object[] args;
		};
		
    	static Dictionary<string, List<Pair>> events = new Dictionary<string, List<Pair>>();
		
		public static bool EventsImmediately = true;

		static LinkedList<EventObj> firedEvents = new LinkedList<EventObj>();
		static LinkedList<EventObj> doingEvents = new LinkedList<EventObj>();
		
		static bool _isPause = false;

		public Event()
		{
		}
		
		public static void clear()
		{
			events.Clear();
			clearFiredEvents();
		}

		public static void clearFiredEvents()
		{
			monitor_Enter(events);
			firedEvents.Clear();
			monitor_Exit(events);
			
			doingEvents.Clear();	
			_isPause = false;
		}
		
		public static void pause()
		{
			_isPause = true;
		}

		public static void resume()
		{
			_isPause = false;
			processEvents();
		}

		public static bool isPause()
		{
			return _isPause;
		}

		public static void monitor_Enter(object obj)
		{		
			Monitor.Enter(obj);
		}

		public static void monitor_Exit(object obj)
		{
			Monitor.Exit(obj);
		}
		
		public static bool hasRegisterOut(string eventname)
		{
			return _hasRegister(events, eventname);
		}
		
		private static bool _hasRegister(Dictionary<string, List<Pair>> events, string eventname)
		{
			bool has = false;
			
			monitor_Enter(events);
			has = events.ContainsKey(eventname);
			monitor_Exit(events);
			
			return has;
		}

        /// <summary>
		///	注册监听由kbe插件抛出的事件。(out = kbe->render)
		///	通常由渲染表现层来注册, 例如：监听角色血量属性的变化， 如果UI层注册这个事件，
		///	事件触发后就可以根据事件所附带的当前血量值来改变角色头顶的血条值。
        /// </summary>
        public static bool register(string eventname, object obj, string funcname)
		{
			return _register(events, eventname, obj, funcname);
		}

        /// <summary>
		///	注册监听由kbe插件抛出的事件。(out = kbe->render)
		///	通常由渲染表现层来注册, 例如：监听角色血量属性的变化， 如果UI层注册这个事件，
		///	事件触发后就可以根据事件所附带的当前血量值来改变角色头顶的血条值。
        /// </summary>
        public static bool register(string eventname, Action handler)
        {
            return register(eventname, handler.Target, handler.Method.Name);
        }

        /// <summary>
		///	注册监听由kbe插件抛出的事件。(out = kbe->render)
		///	通常由渲染表现层来注册, 例如：监听角色血量属性的变化， 如果UI层注册这个事件，
		///	事件触发后就可以根据事件所附带的当前血量值来改变角色头顶的血条值。
        /// </summary>
        public static bool register<T1>(string eventname, Action<T1> handler)
        {
            return register(eventname, handler.Target, handler.Method.Name);
        }

        /// <summary>
		///	注册监听由kbe插件抛出的事件。(out = kbe->render)
		///	通常由渲染表现层来注册, 例如：监听角色血量属性的变化， 如果UI层注册这个事件，
		///	事件触发后就可以根据事件所附带的当前血量值来改变角色头顶的血条值。
        /// </summary>
        public static bool register<T1, T2>(string eventname, Action<T1, T2> handler)
        {
            return register(eventname, handler.Target, handler.Method.Name);
        }

        /// <summary>
		///	注册监听由kbe插件抛出的事件。(out = kbe->render)
		///	通常由渲染表现层来注册, 例如：监听角色血量属性的变化， 如果UI层注册这个事件，
		///	事件触发后就可以根据事件所附带的当前血量值来改变角色头顶的血条值。
        /// </summary>
        public static bool register<T1, T2, T3>(string eventname, Action<T1, T2, T3> handler)
        {
            return register(eventname, handler.Target, handler.Method.Name);
        }

        /// <summary>
		///	注册监听由kbe插件抛出的事件。(out = kbe->render)
		///	通常由渲染表现层来注册, 例如：监听角色血量属性的变化， 如果UI层注册这个事件，
		///	事件触发后就可以根据事件所附带的当前血量值来改变角色头顶的血条值。
        /// </summary>
        public static bool register<T1, T2, T3, T4>(string eventname, Action<T1, T2, T3, T4> handler)
        {
            return register(eventname, handler.Target, handler.Method.Name);
        }

        private static bool _register(Dictionary<string, List<Pair>> events, string eventname, object obj, string funcname)
		{
			_deregister(events, eventname, obj, funcname);
			List<Pair> lst = null;
			
			Pair pair = new Pair();
			pair.obj = obj;
			pair.funcname = funcname;
			pair.method = obj.GetType().GetMethod(funcname);
			
			if(pair.method == null)
			{
				Dbg.ERROR_MSG("Event::register: " + obj + "not found method[" + funcname + "]");
				return false;
			}
			
			monitor_Enter(events);
			if(!events.TryGetValue(eventname, out lst))
			{
				lst = new List<Pair>();
				lst.Add(pair);
				//Dbg.DEBUG_MSG("Event::register: event(" + eventname + ")!");
				events.Add(eventname, lst);
				monitor_Exit(events);
				return true;
			}
			
			//Dbg.DEBUG_MSG("Event::register: event(" + eventname + ")!");
			lst.Add(pair);
			monitor_Exit(events);
			return true;
		}

		public static bool deregister(string eventname, object obj, string funcname)
		{
            removeFiredEvent(obj, eventname, funcname);
            return _deregister(events, eventname, obj, funcname);
		}
  
        public static bool deregister(string eventname, Action handler)
        {
            return deregister(eventname, handler.Target, handler.Method.Name);
        }

        private static bool _deregister(Dictionary<string, List<Pair>> events, string eventname, object obj, string funcname)
		{
			monitor_Enter(events);
			List<Pair> lst = null;
			
			if(!events.TryGetValue(eventname, out lst))
			{
				monitor_Exit(events);
				return false;
			}
			
			for(int i=0; i<lst.Count; i++)
			{
				if(obj == lst[i].obj && lst[i].funcname == funcname)
				{
					//Dbg.DEBUG_MSG("Event::deregister: event(" + eventname + ":" + funcname + ")!");
					lst.RemoveAt(i);
					monitor_Exit(events);
					return true;
				}
			}
			
			monitor_Exit(events);
			return false;
		}

		public static bool deregister(object obj)
		{
            removeAllFiredEvent(obj);
			return deregister(events, obj);
		}
		
		private static bool deregister(Dictionary<string, List<Pair>> events, object obj)
		{
			monitor_Enter(events);
			
			var iter = events.GetEnumerator();
			while (iter.MoveNext())
			{
				List<Pair> lst = iter.Current.Value;
				// 从后往前遍历，以避免中途删除的问题
				for (int i = lst.Count - 1; i >= 0; i--)
				{
					if (obj == lst[i].obj)
					{
						//Dbg.DEBUG_MSG("Event::deregister: event(" + e.Key + ":" + lst[i].funcname + ")!");
						lst.RemoveAt(i);
					}
				}
			}
			
			monitor_Exit(events);
			return true;
		}

        /// <summary>
        /// kbe插件触发事件(out = kbe->render)
		/// 通常由渲染表现层来注册, 例如：监听角色血量属性的变化， 如果UI层注册这个事件，
		/// 事件触发后就可以根据事件所附带的当前血量值来改变角色头顶的血条值。
		/// </summary>
		public static void fire(string eventname, params object[] args)
		{
			_fire(events, firedEvents, eventname, args, EventsImmediately);
		}

        /// <summary>
        /// 触发kbe插件和渲染表现层都能够收到的事件
        /// <summary>
        public static void fireAll(string eventname, params object[] args)
		{
			_fire(events, firedEvents, eventname, args, false);
		}
		
		private static void _fire(Dictionary<string, List<Pair>> events, LinkedList<EventObj> firedEvents, string eventname, object[] args, bool eventsImmediately)
		{
			monitor_Enter(events);
			List<Pair> lst = null;
			
			if(!events.TryGetValue(eventname, out lst))
			{			
				monitor_Exit(events);
				return;
			}
			
			if(eventsImmediately && !_isPause)
			{
				for(int i=0; i<lst.Count; i++)
				{
					Pair info = lst[i];

					try
					{
						info.method.Invoke (info.obj, args);
					}
					catch (Exception e)
					{
						Dbg.ERROR_MSG("Event::fire_: event=" + info.method.DeclaringType.FullName + "::" + info.funcname + "\n" + e.ToString());
					}
				}
			}
			else
			{
				for(int i=0; i<lst.Count; i++)
				{
					EventObj eobj = new EventObj();
					eobj.info = lst[i];
                    eobj.eventname = eventname;
                    eobj.args = args;
					firedEvents.AddLast(eobj);
				}
			}

			monitor_Exit(events);
		}
		
		public static void processEvents()
		{
			monitor_Enter(events);

			if(firedEvents.Count > 0)
			{
				var iter = firedEvents.GetEnumerator();
				while (iter.MoveNext())
				{
					doingEvents.AddLast(iter.Current);
				}

				firedEvents.Clear();
			}

			monitor_Exit(events);

			while (doingEvents.Count > 0 && !_isPause) 
			{

				EventObj eobj = doingEvents.First.Value;
				try
				{
					eobj.info.method.Invoke (eobj.info.obj, eobj.args);
				}
	            catch (Exception e)
	            {
	            	Dbg.ERROR_MSG("Event::processOutEvents: event=" + eobj.info.method.DeclaringType.FullName + "::" + eobj.info.funcname + "\n" + e.ToString());
	            }
            
				if(doingEvents.Count > 0)
					doingEvents.RemoveFirst();
			}
		}

        public static void removeAllFiredEvent(object obj)
        {
			_removeFiredEvent(firedEvents, obj);
        }

        public static void removeFiredEvent(object obj, string eventname, string funcname)
        {
            _removeFiredEvent(firedEvents, obj, eventname, funcname);
        }

        public static void _removeFiredEvent(LinkedList<EventObj> firedEvents, object obj, string eventname="", string funcname="")
        {
            monitor_Enter(events);
           
            while(true)
            {
                bool found = false;
                foreach(EventObj eobj in firedEvents)
                {
                    if( ((eventname == "" && funcname == "") || (eventname == eobj.eventname && funcname == eobj.info.funcname))
                        && eobj.info.obj == obj)
                    {
                        firedEvents.Remove(eobj);
                        found = true;
                        break;
                    }
                }

                if (!found)
                    break;
            }
           
            monitor_Exit(events);
        }
    
    }
} 
