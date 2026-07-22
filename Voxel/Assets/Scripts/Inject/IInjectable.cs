/// <summary>
/// An interface that tracks if there was an Injection done on the object. Otherwise we would inject 
/// multiple times and it is expensive operation.
/// </summary>
public interface IInjectable
{
    bool Injected { get; set; }
}
