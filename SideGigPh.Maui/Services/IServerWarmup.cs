namespace SideGigPh.Maui.Services;

/// <summary>The port that wakes the server (a cold API and a paused database). Best effort: never throws.</summary>
public interface IServerWarmup
{
    Task WarmUpAsync();
}
