using GorillaEscape.UI;
using NUnit.Framework;

public class ProductEntryPolicyTests
{
    [TestCase(true, true, false, false, MainMenuController.BootstrapScenePath, false, true)]
    [TestCase(true, true, false, false, "", true, false)]
    [TestCase(true, true, false, false, "Assets/Minigame/Bootstrap.unity", true, false)]
    [TestCase(true, true, false, false, "Assets/Temp/TestScene.unity", true, false)]
    [TestCase(true, true, true, false, MainMenuController.BootstrapScenePath, true, false)]
    [TestCase(true, true, false, true, MainMenuController.BootstrapScenePath, true, false)]
    [TestCase(true, false, false, false, MainMenuController.BootstrapScenePath, true, false)]
    [TestCase(false, true, false, false, "", true, true)]
    [TestCase(false, true, true, false, "", true, true)]
    [TestCase(false, true, false, false, MainMenuController.BootstrapScenePath, false, false)]
    public void EntryIsExplicit(bool editor, bool playing, bool batch, bool tests, string path, bool product, bool expected)
        => Assert.AreEqual(expected, MainMenuController.ShouldAutoLaunch(editor, playing, batch, tests, path, product));
    [TestCase(true,false,true,MainMenuController.BootstrapScenePath,"1",true)]
    [TestCase(true,false,false,MainMenuController.BootstrapScenePath,"1",false)]
    [TestCase(true,true,true,MainMenuController.BootstrapScenePath,"1",false)]
    [TestCase(false,false,true,MainMenuController.BootstrapScenePath,"1",false)]
    [TestCase(true,false,true,"Assets/Fixture.unity","1",false)]
    [TestCase(true,false,true,MainMenuController.BootstrapScenePath,null,false)]
    public void PhysicalEditorServicesRequireExplicitProductOptIn(bool playing,bool batch,bool permitted,string path,string flag,bool expected)
        => Assert.AreEqual(expected,GorillaEscape.QA.QaRuntimeService.EditorServicesAllowed(playing,batch,permitted,path,flag));
}
