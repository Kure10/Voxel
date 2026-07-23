using After.Main;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class Injector
{
    private static Injector _instance;

    public static Injector Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new Injector();
            }
            return _instance;
        }
    }

    private Dictionary<Type, object> _injections;
    private readonly List<IService> _services = new List<IService>();

    private bool _inited;

    public Injector()
    {
        _injections = new Dictionary<Type, object>();
    }

    public void InjectInto(IInjectable injectable)
    {
        if (!injectable.Injected)
        {
            InjectInto((object)injectable);
        }

        injectable.Injected = true;
    }

    public void InjectInto(object subject, bool verbose = false)
    {
        if (subject == null)
        {
            Debug.LogError("Cannot inject into a null!!!");
            return;
        }

        var fields = subject.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
        foreach (var field in fields)
        {

            if (field.GetCustomAttributes(typeof(InjectAttribute), false).Length > 0)
            {
                if (_injections.ContainsKey(field.FieldType))
                {
                    //Debug.Log("Injecting into field " + field.Name);
                    var value = _injections[field.FieldType];
                    field.SetValue(subject, value);
                }
                else
                {
                    if (!verbose)
                    {
                        Debug.LogErrorFormat("<color=\"red\">Missing Injection rule for type {0} defined in {1} AvailableInjections: {2} </color>", field.FieldType, subject.GetType(), GetInjectionsString());
                    }
                }
            }
        }

        var properties = subject.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        foreach (var property in properties)
        {
            if (property.GetCustomAttributes(typeof(InjectAttribute), false).Length > 0)
            {
                if (_injections.ContainsKey(property.PropertyType))
                {
                    var value = _injections[property.PropertyType];
                    property.SetValue(subject, value, null); // (subject, value);
                }
                else
                {
                    if (!verbose)
                    {
                        Debug.LogErrorFormat("<color=\"red\">Missing Injection rule for type {0} defined in {1} AvailableInjections: {2} </color>", property.PropertyType, subject.GetType(), GetInjectionsString());
                    }
                }
            }
        }
    }

    public void InjectIntoArray(object[] subjects)
    {
        foreach (object obj in subjects)
        {
            InjectInto(obj);
        }
    }

    public void InjectIntoEnumerable<T>(IEnumerable<T> subjects)
    {
        foreach (object obj in subjects)
        {
            InjectInto(obj);
        }
    }

    public void InjectIntoCombos<T>(IEnumerable<T> subjects)
    {
        foreach (object obj in subjects)
        {
            // Only check no log, because null combos
            if (obj != null)
            {
                InjectInto(obj);
            }
        }
    }


    public T MapSingleton<T>() where T : new()
    {
        T newInstance = new T();
        MapAndInjectInto(newInstance);
        return newInstance;
    }
    
    public T MapOrGetSingleton<T>() where T : new()
    {
        if (_injections.ContainsKey(typeof(T)))
            return (T)_injections[typeof(T)];

        T newInstance = new T();
        MapAndInjectInto(newInstance);

        if (newInstance is IService service)
        {
            service.Init();
            _services.Add(service);
        }

        return newInstance;
    }

    public T Get<T>() where T : class
    {
        if (_injections.ContainsKey(typeof(T)))
        {
            return (T)_injections[typeof(T)];
        }

        return null;
    }

    public void MapSingletonOf<TRequested, TClass>() where TClass : TRequested, new()
    {
        MapAndInjectInto<TRequested>(new TClass());
    }

    public void MapAndInjectInto<T>(T value, bool verbose = false)
    {
        if (value == null)
        {
            Debug.LogError("Cannot map and inject a null value!!!");
            return;
        }

        _injections[typeof(T)] = value;

        InjectInto(value, verbose: verbose);
    }

    public T TryMapManager<T>(T value, bool verbose = false) where T : IManager
    {
        if (value == null)
        {
            Debug.LogError("Cannot map a null value!!!");
            return default(T);
        }

        if (HasInjection(typeof(T)))
        {
            // Destroy old 
            MonoBehaviour monoBehaviour = value as MonoBehaviour;
            if (monoBehaviour != null)
            {
                UnityEngine.Object.Destroy(monoBehaviour.gameObject);
            }

            // Return the injection
            return (T)_injections[typeof(T)];
        }

        MapAndInjectInto(value, verbose);
        
        value.Initialize();

        return value;
    }
    
    public T TryMapService<T>(T value) where T : IService
    {
        if (value == null)
        {
            Debug.LogError("Cannot map a null value!!!");
            return default(T);
        }

        if (HasInjection(typeof(T)))
            return (T)_injections[typeof(T)];

        MapAndInjectInto(value);
        value.Init();
        _services.Add(value);
        return value;
    }

    public void MapValue<T>(object value, bool verbose = false)
    {
        var type = typeof(T);
        if (_injections.ContainsKey(type))
            Debug.LogWarningFormat("<color=\"aqua\">{0}.MapValue() : Mapping for {1} already defined, you should unmap first if you want to change the mapping</color>", this, value.GetType());

        _injections[type] = value;
        if (_inited)
        {
            InjectInto(value, verbose: verbose);
        }
    }

    public void MapValue(object value, Type type)
    {
        if (_injections.ContainsKey(type))
            Debug.LogWarningFormat("<color=\"aqua\">{0}.MapValue() : Mapping for {1} already defined, you should unmap first if you want to change the mapping</color>", this, value.GetType());

        _injections[type] = value;
        if (_inited)
        {
            InjectInto(value);
        }
    }

    public void Unmap<T>()
    {
        if (_injections.ContainsKey(typeof(T)))
        {
            _injections.Remove(typeof(T));
        }
        else
        {
            Debug.LogWarningFormat("<color=\"aqua\">{0}.Unmap() : There is no Mapping for {1} </color>", this, typeof(T));
        }
    }

    public void Unmap(object obj)
    {
        if (_injections.ContainsKey(obj.GetType()))
        {
            _injections.Remove(obj.GetType());
        }
        else
        {
            Debug.LogWarningFormat("<color=\"aqua\">{0}.Unmap() : There is no Mapping for {1} </color>", this, obj.GetType());
        }
    }

    public void Destroy()
    {
        _injections.Clear();
        _instance = null;
    }

    private string GetInjectionsString()
    {
        var injString = "";
        foreach (KeyValuePair<Type, object> injection in _injections)
        {
            injString += injection.Key + ", ";
        }

        return injString;
    }

    public bool HasInjection(Type t)
    {
        return _injections.ContainsKey(t);
    }
    
    public void DestroyAllServices()
    {
        foreach (var service in _services)
            service.Destroy();
        _services.Clear();
    }

}
