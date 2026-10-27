using System;
using System.IO;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;

namespace GorillaEscape.Editor
{
    // Explicit developer menu launch. Does not install packages, build sources or elevate privileges.
    public static class JungleEditorLaunch
    {
        public static bool Prepare()
        {
            string repository = Path.GetFullPath(Path.Combine(Application.dataPath, "../.."));
            string package = Environment.GetEnvironmentVariable("GORILLA_PACKAGE_ROOT");
            if (string.IsNullOrEmpty(package))
            {
                string candidate = Path.Combine(Application.dataPath, "../Builds/FoundationLinux");
                package = File.Exists(Path.Combine(candidate, "Vision/manifest.json"))
                    && File.Exists(Path.Combine(candidate, "tools/setup-firewall.sh")) ? Path.GetFullPath(candidate) : repository;
            }
            string executable = Application.platform == RuntimePlatform.WindowsEditor ? "java.exe" : "java";
            string java = Environment.GetEnvironmentVariable("GORILLA_IPC_JAVA");
            if (string.IsNullOrEmpty(java))
            {
                string bundled = Path.Combine(package, "Java/runtime/bin", executable);
                string home = Environment.GetEnvironmentVariable("JAVA_HOME");
                if (File.Exists(bundled)) java = bundled;
                else if (!string.IsNullOrEmpty(home) && File.Exists(Path.Combine(home, "bin", executable))) java = Path.Combine(home, "bin", executable);
                else foreach (string folder in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator))
                {
                    if (string.IsNullOrWhiteSpace(folder)) continue;
                    string candidate = Path.Combine(folder, executable);
                    if (Path.IsPathRooted(candidate) && File.Exists(candidate)) { java = candidate; break; }
                }
            }
            string jar = Environment.GetEnvironmentVariable("GORILLA_IPC_JAR");
            if (string.IsNullOrEmpty(jar)) jar = Path.Combine(repository, "Server/target/local-server-0.1.0-SNAPSHOT.jar");
            if (string.IsNullOrEmpty(java) || !Path.IsPathRooted(java) || !File.Exists(java)
                || !Path.IsPathRooted(jar) || !File.Exists(jar))
            {
                EditorUtility.DisplayDialog("Preparar Gorilla Escape",
                    "Falta el servidor compilado o Java para desarrollo. Compila la PWA y Server, y configura las rutas de Java/JAR. Consulta Unity/README.md. No se inició ningún servicio.", "Aceptar");
                return false;
            }
            Environment.SetEnvironmentVariable("GORILLA_IPC_JAVA", java);
            Environment.SetEnvironmentVariable("GORILLA_IPC_JAR", jar);
            Environment.SetEnvironmentVariable("GORILLA_PACKAGE_ROOT", package);
            Environment.SetEnvironmentVariable("GORILLA_EDITOR_PHYSICAL_QA", "1");
            Environment.SetEnvironmentVariable("GORILLA_MAIN_MENU", "1");
            Environment.SetEnvironmentVariable("GORILLA_QA_OVERLAY", "1");
            Environment.SetEnvironmentVariable("GORILLA_PHONE_INPUT", "1");
            Default("GORILLA_CONNECTION_MODE", "AUTO");
            Default("GORILLA_TRUST_BOOTSTRAP_PORT", "8081");
            Default("GORILLA_HTTPS_PORT", "8443");
            Default("GORILLA_LOCAL_TRUST_DIR", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".gorilla-escape/live-qa-trust"));
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GORILLA_MOBILE_OPERATOR")))
            {
                var bytes = new byte[32];
                using (var random = RandomNumberGenerator.Create()) random.GetBytes(bytes);
                Environment.SetEnvironmentVariable("GORILLA_MOBILE_OPERATOR", Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_'));
                Array.Clear(bytes, 0, bytes.Length);
            }
            Environment.SetEnvironmentVariable("GORILLA_MOBILE_LAB", null);
            return true;
        }
        static void Default(string name, string value)
        {
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(name))) Environment.SetEnvironmentVariable(name, value);
        }
    }
}
