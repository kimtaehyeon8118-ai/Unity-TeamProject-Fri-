public static class GameFlowState
{
    private static bool nightPlayRequested;
    private static bool returnedFromNight;
    private static int requestedNightDay = 1;

    public static void RequestNightPlay(int dayNumber = 1)
    {
        nightPlayRequested = true;
        requestedNightDay = dayNumber;
    }

    public static bool ConsumeNightPlayRequest()
    {
        bool requested = nightPlayRequested;
        nightPlayRequested = false;
        return requested;
    }

    public static int RequestedNightDay => requestedNightDay;

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
