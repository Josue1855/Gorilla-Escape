using System;
using System.IO;
using System.Security.Cryptography;
namespace GorillaEscape.Core
{
    // Direct Player launches get the same local package defaults as the developer launcher.
    public static class PackagedRuntimeEnvironment
    {
        public static void Configure(string root,bool windows,Func<string,string> read=null,Action<string,string> write=null,string home=null)
        {
            if(!Path.IsPathRooted(root))throw new ArgumentException("PACKAGE_ROOT_ABSOLUTE");
            read=read??Environment.GetEnvironmentVariable;write=write??Environment.SetEnvironmentVariable;
            home=home??Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            void Default(string name,string value){if(string.IsNullOrEmpty(read(name)))write(name,value);}
            Default("GORILLA_PACKAGE_ROOT",root);Default("GORILLA_CONNECTION_MODE","AUTO");
            Default("GORILLA_IPC_JAVA",Path.Combine(root,"Java/runtime/bin",windows?"java.exe":"java"));
            Default("GORILLA_IPC_JAR",Path.Combine(root,"Java/server.jar"));
            Default("GORILLA_PHONE_INPUT","1");Default("GORILLA_IPC_MODE","lifecycle");
            if(read("GORILLA_MOBILE_LAB")!="1")Default("GORILLA_LOCAL_TRUST_DIR",Path.Combine(home,".gorilla-escape/live-qa-trust"));
            Default("GORILLA_TRUST_BOOTSTRAP_PORT","8081");Default("GORILLA_HTTPS_PORT","8443");
            if(string.IsNullOrEmpty(read("GORILLA_MOBILE_OPERATOR"))){
                var bytes=new byte[32];using(var random=RandomNumberGenerator.Create())random.GetBytes(bytes);
                write("GORILLA_MOBILE_OPERATOR",Convert.ToBase64String(bytes).TrimEnd('=').Replace('+','-').Replace('/','_'));Array.Clear(bytes,0,bytes.Length);
            }
        }
    }
}
