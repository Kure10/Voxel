using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace After.Main
{
    /// <summary>
    /// A manager that handles events.
    /// 
    /// Create events from AbstractEvent or use EventName enum to dispatch events without data.
    /// </summary>
    public class MyEventManager : MonoBehaviour, IManager
    {
        private Dictionary<Type, IList> _events = new();
        private Dictionary<EventName, IList> _enumEvents = new();

        public void Initialize()
        {

        }

        /// <summary>
        /// Removes all popups.
        /// </summary>
        public void RemoveAll()
        {
            _events.Clear();
            _enumEvents.Clear();
        }

        /// <summary>
        /// Adds an action that should be called on the event of type T.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="action"></param>
        public void AddListener<T>(Action<T> action) where T : AbstractEvent
        {
            Type t = typeof(T);
            if (_events.ContainsKey(t))
            {
                IList l = _events[t];
                if (l.Contains(action))
                {
                    Debug.LogWarningFormat("<color=\"aqua\">{0}.AddListener : OBSERVER WARNING: Event {1} is already registerd with action {2}</color>", this, t, action);
                }
                else
                {
                    l.Add(action);
                }
            }
            else
            {
                _events.Add(t, new List<Action<T>> { action });
            }
        }

        /// <summary>
        /// Registers a new action for EventName event type.
        /// </summary>
        /// <param name="eventName"></param>
        /// <param name="action"></param>
        public void AddListener(EventName eventName, Action action)
        {
            if (_enumEvents.ContainsKey(eventName))
            {
                IList l = _enumEvents[eventName];
                if (l.Contains(action))
                {
                    Debug.LogWarningFormat("<color=\"aqua\">{0}.AddListener : OBSERVER WARNING: Event {1} is already registerd with action {2}</color>", this, eventName, action);
                }
                else
                {
                    l.Add(action);
                }
            }
            else
            {
                _enumEvents.Add(eventName, new List<Action> { action });
            }
        }

        /// <summary>
        /// Removes a listener for the given event type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="action"></param>
        public void RemoveListener<T>(Action<T> action) where T : AbstractEvent
        {
            Type t = typeof(T);
            if (_events.ContainsKey(t))
            {
                IList l = _events[t];

                if (!l.Contains(action))
                {
                    //Logger.WarningEvents("Cannot remove action. It doesn't exist on the event manager.");
                }

                l.Remove(action);
            }
            else
            {
               // Logger.WarningEvents("Event listener not found!");
            }
        }

        /// <summary>
        /// Removes a specific action registered for the given EventName.
        /// </summary>
        /// <param name="eventName"></param>
        /// <param name="a"></param>
        /// <returns>True if something was removed</returns>
        public bool RemoveListener(EventName eventName, Action a)
        {
            if (_enumEvents.ContainsKey(eventName))
            {
                _enumEvents[eventName].Remove(a);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Dispatches a new event.
        /// </summary>
        /// <param name="eventName"></param>
        public void DispatchEvent(EventName eventName)
        {

#if UNITY_EDITOR
           // Logger.LogEvents("Dispatching event: " + eventName);
#endif

            if (_enumEvents.ContainsKey(eventName))
            {
                IList l = _enumEvents[eventName];
                for (int i = l.Count - 1; i > -1; i--)
                {
                    // If some events were removed or listener cleared during for loop
                    if (i >= l.Count)
                    {
                        continue;
                    }

                    Action a = (Action)l[i];
                    a.Invoke();
                }
            }
        }

        /// <summary>
        /// Dispatches a new event.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="e"></param>
        public void DispatchEvent<T>(T e) where T : AbstractEvent
        {
            Type t = typeof(T);

#if UNITY_EDITOR
         //   Logger.LogEvents("Dispatching event: " + e.ToString());
#endif

            if (_events.ContainsKey(t))
            {
                IList l = _events[t];
                for (int i = l.Count - 1; i > -1; i--)
                {
                    // If some events were removed or listener cleared during for loop
                    if (i >= l.Count)
                    {
                        continue;
                    }

                    Action<T> a = (Action<T>)l[i];
                    a.Invoke(e);
                }
            }
        }

        /// <summary>
        /// Logs all listeners currently registered.
        /// </summary>
        [ContextMenu("Log all listeners")]
        public void LogAllListeners()
        {
            foreach (KeyValuePair<Type, IList> listeners in _events)
            {
                var color = listeners.Value.Count > 1 ? "red" : "magenta";
                if (listeners.Value.Count > 0)
                {
                    Debug.Log($"EventType: <color=\"{color}\">{listeners.Key}</color> : Listeners:<color=\"{color}\">{listeners.Value.Count}</color>");
                }
            }

            foreach (KeyValuePair<EventName, IList> listeners in _enumEvents)
            {
                var color = listeners.Value.Count > 1 ? "red" : "magenta";
                if (listeners.Value.Count > 0)
                {
                    Debug.Log($"EventType: <color=\"{color}\">{listeners.Key}</color> : Listeners:<color=\"{color}\">{listeners.Value.Count}</color>");
                }
            }
        }
    }
}

