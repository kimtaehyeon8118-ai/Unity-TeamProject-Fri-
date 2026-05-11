using NUnit.Framework;

public class SceneFlowUtilityTests
{
    [Test]
    public void FindSceneIndexByName_ReturnsNegative_WhenSceneDoesNotExist()
    {
        Assert.That(SceneFlowUtility.FindSceneIndexByName("DefinitelyMissingScene"), Is.EqualTo(-1));
    }

    [Test]
    public void ResolveGameplaySceneIndex_ReturnsValidBuildIndex()
    {
        int index = SceneFlowUtility.ResolveGameplaySceneIndex("TitleScene");
        Assert.That(index, Is.GreaterThanOrEqualTo(0));
    }
}
