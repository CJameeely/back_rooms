/// Static, cross-scene handoff between the game scene and End.unity. Time.time is not reset by
/// scene loads, so StartTime/ElapsedSeconds work as plain stopwatch values within one play session.
public static class GameResult
{
    public static bool Won;
    public static float StartTime;
    public static float ElapsedSeconds;
}
