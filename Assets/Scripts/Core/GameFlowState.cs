public static class GameFlowState
{
    private static bool nightPlayRequested;
    private static bool returnedFromNight;

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

    public static void MarkReturnedFromNight()
    {
        returnedFromNight = true;
    }

    public static bool ConsumeReturnedFromNight()
    {
        bool returned = returnedFromNight;
        returnedFromNight = false;
        return returned;
    }
}
