using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SgrYuki.Utils;

public static class Extensions
{
    public static T GetRandomOne<T>(this System.Collections.Generic.List<T> list)
    {
        return list[UnityEngine.Random.Range(0, list.Count)];
    }

    public static Il2CppSystem.Collections.Generic.IEnumerable<T> ToIEnumerable<T>(this Il2CppSystem.Collections.Generic.List<T> list)
    {
        return new Il2CppSystem.Collections.Generic.IEnumerable<T>(list.Pointer);
    }

    public static Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<T> ToIl2CppReferenceArray<T>(
        this Il2CppSystem.Collections.Generic.IEnumerable<T> enumerable) where T : Il2CppInterop.Runtime.InteropTypes.Il2CppObjectBase
    {
        return enumerable.TryCast<Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<T>>();
    }

    public static System.Collections.Generic.List<KeyT> FilterKey<KeyT, ValueT>(
        this Il2CppSystem.Collections.Generic.Dictionary<KeyT, ValueT> dict, Predicate<ValueT> condition)
    {
        var result = new System.Collections.Generic.List<KeyT>();
        var e = dict.GetEnumerator();
        while (e.MoveNext())
        {
            var kv = e.Current;
            if (condition(kv.Value)) result.Add(kv.Key);
        }
        return result;
    }
}

public sealed class LogWrapper
{
    public readonly BepInEx.Logging.ManualLogSource _inner;
    public readonly string _tag;

    public LogWrapper(BepInEx.Logging.ManualLogSource inner, string tag)
    {
        _inner = inner;
        _tag = tag;
    }

    private static string GetTime() => DateTime.Now.ToString("HH:mm:ss.fff");

    private string TagString(bool withTag) => withTag ? $"[{_tag}] " : "";

    public void Debug(string msg, bool withTag = true) => _inner.LogDebug($"[{GetTime()}] {TagString(withTag)}{msg}");
    public void Info(string msg, bool withTag = true) => _inner.LogInfo($"[{GetTime()}] {TagString(withTag)}{msg}");
    public void Message(string msg, bool withTag = true) => _inner.LogMessage($"[{GetTime()}] {TagString(withTag)}{msg}");
    public void Warning(string msg, bool withTag = true) => _inner.LogWarning($"[{GetTime()}] {TagString(withTag)}{msg}");
    public void Error(string msg, bool withTag = true) => _inner.LogError($"[{GetTime()}] {TagString(withTag)}{msg}");

    public void LogDebug(string msg, bool withTag = true) => Debug(msg, withTag);
    public void LogInfo(string msg, bool withTag = true) => Info(msg, withTag);
    public void LogMessage(string msg, bool withTag = true) => Message(msg, withTag);
    public void LogWarning(string msg, bool withTag = true) => Warning(msg, withTag);
    public void LogError(string msg, bool withTag = true) => Error(msg, withTag);
}

// Support "foreach" any container that implements [] operator and Count member method
public readonly struct IndexableWrapper<T>
{
    private readonly T _inner;
    public IndexableWrapper(T inner) => _inner = inner;

    public Enumerator<T> GetEnumerator() => new Enumerator<T>(_inner);

    public struct Enumerator<TContainer>
    {
        private readonly TContainer _container;
        private int _index;

        public Enumerator(TContainer container)
        {
            _container = container;
            _index = -1;
        }

        public object Current => ((dynamic)_container)[_index];

        public bool MoveNext()
        {
            return ++_index < ((dynamic)_container).Count();
        }
    }
}


public class ConcurrentHashSet<T>
{
    private readonly System.Collections.Concurrent.ConcurrentDictionary<T, byte> _dict = new();

    public bool Add(T item)
        => _dict.TryAdd(item, 0);

    public bool Remove(T item)
        => _dict.TryRemove(item, out _);

    public bool Contains(T item)
        => _dict.ContainsKey(item);

    public int Count
        => _dict.Count;

    public void Clear()
        => _dict.Clear();

    public System.Collections.Generic.IEnumerable<T> Items
        => _dict.Keys;
}

public static class FunctionUtil
{

    // Note: only available for those functions are patched. Not patched functions will not appear in the stacktrace
    public static bool CheckStacktraceContains(string funcName)
    {
        var stack = new System.Diagnostics.StackTrace();
        // foreach (var frame in stack.GetFrames())
        // {
        //     var method = frame.GetMethod();
        //     Log.LogInfo($"{LOG_TAG} \t at {method.Name}");
        // }
        return stack.GetFrames().Any(frame => frame.GetMethod().Name.Contains(funcName));
    }

    public static void LogStacktrace(BepInEx.Logging.ManualLogSource Log)
    {
        string stack = Environment.StackTrace;
        string[] lines = stack.Split([Environment.NewLine], StringSplitOptions.None);

        System.Text.StringBuilder sb = new();
        // ignore this layer and upper layer
        for (int i = 2; i < lines.Length; i++)
        {
            sb.AppendLine(lines[i]);
        }

        Log.LogInfo(sb.ToString());
    }

    public static void ModifyReadonlyField<T, FieldT>(T instance, string fieldName, FieldT newValue)
    {
        var field = typeof(T).GetField(
            fieldName,
            BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
        );

        field.SetValue(instance, newValue);
    }
}


public static class CommandScheduler
{
    private sealed class Command
    {
        public Func<bool> CanExecute;
        public Action Execute;
        public Action OnTimeout;
        public float ExpireTime; // unscaled time
    }

    private static readonly ConcurrentQueue<Command> _pending = new ConcurrentQueue<Command>();

    private static readonly Queue<Command> _queue = new Queue<Command>();

    // ================================
    // Public API 
    // ================================
    public static void Enqueue(
        Func<bool> canExecute,
        Action execute,
        float timeoutSeconds = 60f,
        Action onTimeout = null)
    {
        if (canExecute == null)
            throw new ArgumentNullException(nameof(canExecute));
        if (execute == null)
            throw new ArgumentNullException(nameof(execute));

        _pending.Enqueue(new Command
        {
            CanExecute = canExecute,
            Execute = execute,
            OnTimeout = onTimeout,
            ExpireTime = UnityEngine.Time.unscaledTime + timeoutSeconds
        });
    }

    // ================================
    // Main-thread tick 
    // ================================
    public static void Tick()
    {
        // 转移后台提交
        while (_pending.TryDequeue(out var cmd))
        {
            _queue.Enqueue(cmd);
        }

        // FIFO 扫描
        int count = _queue.Count;

        for (int i = 0; i < count; i++)
        {
            var cmd = _queue.Dequeue();

            if (UnityEngine.Time.unscaledTime > cmd.ExpireTime)
            {
                cmd.OnTimeout?.Invoke();
                continue;
            }

            bool canExecute = false;
            try
            {
                canExecute = cmd.CanExecute();
            }
            catch (MetaMystia.NightGuestManager.GuestInvalidatedException e)
            {
                MetaMystia.Plugin.Instance.Log.LogWarning($"ignore action for {e.Message}");
                continue;
            }
            catch (Exception e)
            {
                MetaMystia.Plugin.Instance.Log.LogWarning($"Error checking condition: {e.Message}");
                continue;
            }

            if (canExecute)
            {
                cmd.Execute();
            }
            else
            {
                _queue.Enqueue(cmd);
            }
        }
    }
}

