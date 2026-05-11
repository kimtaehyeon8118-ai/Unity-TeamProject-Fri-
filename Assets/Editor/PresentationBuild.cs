#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

public static class PresentationBuild
{
    private const string BuildDirectory = "Builds/PresentationBuild";
    private const string BuildPath = BuildDirectory + "/taehyeon2ne.app";

    [MenuItem("Build/Presentation/macOS Build")]
    public static void BuildMacOS()
    {
        Directory.CreateDirectory(BuildDirectory);

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = new[]
            {
                "Assets/Scenes/TitleScene.unity",
                "Assets/Scenes/DayScene.unity",
                "Assets/Scenes/Stage01_CyberStreet.unity"
            },
            locationPathName = BuildPath,
            target = BuildTarget.StandaloneOSX,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result != BuildResult.Succeeded)
        {
            throw new BuildFailedException("Presentation build failed: " + report.summary.result);
        }

        EditorUtility.RevealInFinder(BuildPath);
    }
}
#endif
