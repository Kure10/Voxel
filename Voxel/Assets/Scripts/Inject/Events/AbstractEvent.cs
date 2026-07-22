namespace After.Main
{
    /// <summary>
    /// Base class for all typed events dispatched through MyEventManager.
    /// Must be a plain C# class (not MonoBehaviour) so instances can be
    /// created with `new` inside input callbacks or any other code.
    /// </summary>
    public class AbstractEvent
    {
        public override string ToString()
        {
            return GetType().Name;
        }
    }
}
