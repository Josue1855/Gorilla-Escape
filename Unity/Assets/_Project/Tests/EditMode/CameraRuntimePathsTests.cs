using System;
using System.IO;
using GorillaEscape.CameraTracking;
using NUnit.Framework;
public sealed class CameraRuntimePathsTests
{
    string root;
    [SetUp] public void Setup(){root=Path.Combine(Path.GetTempPath(),"gorilla-paths-"+Guid.NewGuid());Directory.CreateDirectory(Path.Combine(root,"Vision"));foreach(var name in new[]{"python","worker.py","pose_landmarker_lite-v1.task","manifest.json"})File.WriteAllText(Path.Combine(root,"Vision",name),"");}
    [TearDown] public void Cleanup(){Directory.Delete(root,true);}
    [Test] public void DefaultsAreBuildRelativePhysical(){var p=CameraRuntimePaths.Resolve(root,_=>null);Assert.AreEqual(Path.Combine(root,"Vision","python"),p.Python);Assert.AreEqual("physical",p.Mode);Assert.AreEqual("/dev/video0",p.Device);}
    [Test] public void DeveloperOverridesRequireLabFlag(){var p=CameraRuntimePaths.Resolve(root,name=>name=="GORILLA_CAMERA_PYTHON"?"/missing/developer":null);Assert.IsTrue(p.Python.StartsWith(root));}
    [Test] public void ExplicitLabOverridesPreserveReplay(){var p=CameraRuntimePaths.Resolve(root,name=>name=="GORILLA_CAMERA_LAB"?"1":name=="GORILLA_CAMERA_MODE"?"replay":name=="GORILLA_CAMERA_SCRIPT"?Path.Combine(root,"Vision","python"):null);Assert.AreEqual("replay",p.Mode);Assert.AreEqual(Path.Combine(root,"Vision","python"),p.Worker);}
    [TestCase("python","CAMERA_RUNTIME_MISSING")][TestCase("worker.py","CAMERA_WORKER_MISSING")][TestCase("pose_landmarker_lite-v1.task","CAMERA_MODEL_MISSING")][TestCase("manifest.json","CAMERA_MANIFEST_MISSING")]
    public void MissingBundleFailsExplicitly(string file,string code){File.Delete(Path.Combine(root,"Vision",file));Assert.AreEqual(code,Assert.Throws<FileNotFoundException>(()=>CameraRuntimePaths.Resolve(root,_=>null)).Message);}
}
