public static class GameFlowState
{
    private static bool nightPlayRequested;

    public static void RequestNightPlay()
    {
        nightPlayRequested = true;
    }

    public static bool ConsumeNightPlayRequest()
    {
        bool requested = nightPlayRequested;
        nightPlayRequested = false;
        return requested;
    }
}
