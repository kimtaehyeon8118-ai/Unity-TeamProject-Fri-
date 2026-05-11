using UnityEngine;

public static class DisplayBootstrap
{
    private const int MaxWidth = 1600;
    private const int MaxHeight = 900;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ApplyDefaultResolution()
    {
        if (Application.isMobilePlatform)
            return;

        int fittedWidth = Mathf.Min(Display.main.systemWidth, MaxWidth);
        int fittedHeight = Mathf.Min(Display.main.systemHeight, MaxHeight);

        if (fittedWidth <= 0 || fittedHeight <= 0)
            return;

        int targetHeight = Mathf.FloorToInt(fittedWidth * 9f / 16f);
        if (targetHeight > fittedHeight)
        {
            targetHeight = fittedHeight;
            fittedWidth = Mathf.FloorToInt(targetHeight * 16f / 9f);
        }

        fittedWidth = Mathf.Max(1280, fittedWidth - fittedWidth % 2);
        fittedHeight = Mathf.Max(720, targetHeight - targetHeight % 2);

        if (Screen.width == fittedWidth && Screen.height == fittedHeight)
            return;

        Screen.SetResolution(fittedWidth, fittedHeight, FullScreenMode.Windowed);
    }
}
