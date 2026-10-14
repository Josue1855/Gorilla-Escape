using System;
using System.IO;
namespace GorillaEscape.CameraTracking
{
    // Build-relative defaults; absolute developer paths require the explicit LAB flag.
    public sealed class CameraRuntimePaths
    {
        public string Python { get; private set; }
        public string Worker { get; private set; }
        public string Model { get; private set; }
        public string Fixture { get; private set; }
        public string Device { get; private set; }
        public string Mode { get; private set; }
        public static CameraRuntimePaths Resolve(string productDirectory,Func<string,string> environment=null)
        {
            var env=environment??Environment.GetEnvironmentVariable;
            var root=Path.Combine(Path.GetFullPath(productDirectory),"Vision");
            bool lab=env("GORILLA_CAMERA_LAB")=="1";
            string Pick(string name,string fallback) => lab&&!string.IsNullOrEmpty(env(name))?env(name):fallback;
            var paths=new CameraRuntimePaths {
                Python=Pick("GORILLA_CAMERA_PYTHON",Path.Combine(root,"python")),
                Worker=Pick("GORILLA_CAMERA_SCRIPT",Path.Combine(root,"worker.py")),
                Model=Pick("GORILLA_CAMERA_MODEL",Path.Combine(root,"pose_landmarker_lite-v1.task")),
                Fixture=Pick("GORILLA_CAMERA_FIXTURE",Path.Combine(root,"corpus-v1.json")),
                Device=env("GORILLA_CAMERA_DEVICE")??"/dev/video0",
                Mode=Pick("GORILLA_CAMERA_MODE","physical")
            };
            if(!File.Exists(paths.Python))throw new FileNotFoundException("CAMERA_RUNTIME_MISSING");
            if(!File.Exists(paths.Worker))throw new FileNotFoundException("CAMERA_WORKER_MISSING");
            if(paths.Mode=="physical"&&!File.Exists(paths.Model))throw new FileNotFoundException("CAMERA_MODEL_MISSING");
            if(!lab&&!File.Exists(Path.Combine(root,"manifest.json")))throw new FileNotFoundException("CAMERA_MANIFEST_MISSING");
            return paths;
        }
    }
}
