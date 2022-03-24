using System;
using System.Collections.Generic;
using UnityEngine;

namespace grpania_unity3d_demo
{
    public class TimerManager
    {
        private static TimerManager instance;

        private Dictionary<Action<float>, TimerListener> times;
        private Dictionary<string, TimerListener> times_ex;
        private Dictionary<Action<float>, TimerListener> updates;
        private Dictionary<Action<float>, TimerListener> fixedUpdates;

        private List<TimerListener> temp;
        private List<string> tempName;

        public TimerManager()
        {
            times = new Dictionary<Action<float>, TimerListener>();
            updates = new Dictionary<Action<float>, TimerListener>();
            fixedUpdates = new Dictionary<Action<float>, TimerListener>();
            times_ex = new Dictionary<string, TimerListener>();
        }

        public static TimerManager inst
        {
            get
            {
                if (instance == null)
                    instance = new TimerManager();
                return instance;
            }
        }

        public Action<float> Add(float interval, int repeat, Action<float> fun)
        {
            TimerListener tl = new TimerListener();
            tl.interval = interval;
            tl.repeat = repeat;
            tl.onTime = fun;
            tl.isDelete = false;
            times[fun] = tl;
            return fun;
        }

        public Action<float> Add(string name, float interval, int repeat, Action<float> fun)
        {
            TimerListener tl = new TimerListener();
            tl.interval = interval;
            tl.repeat = repeat;
            tl.onTime = fun;
            tl.isDelete = false;
            times_ex[name] = tl;
            return fun;
        }

        public Action<float> AddUpdate(Action<float> fun)
        {
            TimerListener tl = new TimerListener();
            tl.onTime = fun;
            updates[fun] = tl;
            return fun;
        }

        public Action<float> AddFixedUpdate(Action<float> fun)
        {
            TimerListener tl = new TimerListener();
            tl.onTime = fun;
            fixedUpdates[fun] = tl;
            return fun;
        }

        public void Remove(Action<float> fun)
        {
            if (times.ContainsKey(fun))
            {
                times[fun].isDelete = true;
            }
            if (updates.ContainsKey(fun))
            {
                updates[fun].isDelete = true;
            }
        }

        public void Remove(string name)
        {
            if (times_ex.ContainsKey(name))
            {
                times_ex[name].isDelete = true;
            }
        }

        public void Update()
        {
            //		Log.debug ("delay" + Time.deltaTime.ToString () + "|time" + Time.time.ToString ());
            float time = Time.deltaTime;

            temp = new List<TimerListener>(times.Values);
            if (temp.Count > 0)
            {
                foreach (TimerListener tl1 in temp)
                {
                    if (tl1.Timer(time))
                        times.Remove(tl1.onTime);
                }
            }

            temp = new List<TimerListener>(updates.Values);
            if (temp.Count > 0)
            {
                foreach (TimerListener tl2 in temp)
                {
                    if (tl2.isDelete)
                        updates.Remove(tl2.onTime);
                    else
                        tl2.Update(time);
                }
            }

            tempName = new List<string>(times_ex.Keys);
            if (tempName.Count > 0)
            {
                foreach (string tl3 in tempName)
                {
                    if (times_ex[tl3].Timer(time))
                        times_ex.Remove(tl3);
                }
            }
        }

        public void FixedUpdate()
        {
            float time = Time.deltaTime;
            temp = new List<TimerListener>(fixedUpdates.Values);
            if (temp.Count > 0)
            {
                foreach (TimerListener tl2 in temp)
                {
                    if (tl2.isDelete)
                        fixedUpdates.Remove(tl2.onTime);
                    else
                        tl2.Update(time);
                }
            }
        }
    }

    public class TimerListener
    {
        private float elapsed = 0;
        public Action<float> onTime;
        public float interval;
        public int repeat;
        public bool isDelete = false;

        public bool Timer(float time)
        {
            if (isDelete)
                return isDelete;
            elapsed += time;
            //Log.debug ("time - " + time.ToString ());
            while (elapsed >= interval)
            {
                //Debug.Log("elapsed:" + elapsed + " interval:" + interval);
                this.onTime(time);
                elapsed = elapsed - interval;
                //elapsed = 0;
                if (repeat > 0)
                {
                    repeat--;
                    if (repeat == 0)
                    {
                        isDelete = true;
                        break;
                    }
                }
            }
            return isDelete;
        }

        public void Update(float time)
        {
            this.onTime(time);
        }
    }
}
