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
    [Test]
    public void DirectPlayerUsesBundledServicesAndPreservesExplicitOfflineMode()
    {
        var values=new System.Collections.Generic.Dictionary<string,string>();
        string Read(string name)=>values.TryGetValue(name,out var value)?value:null;
        void Write(string name,string value)=>values[name]=value;
        string root=System.IO.Path.GetFullPath("packaged-game");
        GorillaEscape.QA.QaRuntimeService.ConfigurePackagedRuntime(root,Read,Write);
        Assert.AreEqual("AUTO",values["GORILLA_CONNECTION_MODE"]);
        Assert.AreEqual(System.IO.Path.Combine(root,"Java","server.jar"),values["GORILLA_IPC_JAR"]);
        Assert.AreEqual(System.IO.Path.Combine(root,"Tools","cloudflared"),values["GORILLA_CLOUDFLARED_PATH"]);
        Assert.AreEqual(root,values["GORILLA_PACKAGE_ROOT"]);
        string credential=values["GORILLA_MOBILE_OPERATOR"];
        Assert.That(credential,Does.Match("^[A-Za-z0-9_-]{43}$"));
        values["GORILLA_CONNECTION_MODE"]="CABIN";
        values["GORILLA_IPC_JAR"]="custom.jar";
        GorillaEscape.QA.QaRuntimeService.ConfigurePackagedRuntime(root,Read,Write);
        Assert.AreEqual("CABIN",values["GORILLA_CONNECTION_MODE"]);
        Assert.AreEqual("custom.jar",values["GORILLA_IPC_JAR"]);
        Assert.AreEqual(credential,values["GORILLA_MOBILE_OPERATOR"]);
    }
    [Test]
    public void ExplicitEmulatorLabDoesNotEnableLanTrust()
    {
        var values=new System.Collections.Generic.Dictionary<string,string>{{"GORILLA_MOBILE_LAB","1"}};
        string Read(string name)=>values.TryGetValue(name,out var value)?value:null;
        void Write(string name,string value)=>values[name]=value;
        GorillaEscape.QA.QaRuntimeService.ConfigurePackagedRuntime(System.IO.Path.GetFullPath("lab-package"),Read,Write);
        Assert.False(values.ContainsKey("GORILLA_LOCAL_TRUST_DIR"));
        Assert.AreEqual("1",values["GORILLA_MOBILE_LAB"]);
    }
}
