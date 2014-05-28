using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Runt.Bootstrap
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                Run(args).Wait();
            }
            catch(Exception e)
            {
                Console.Error.Write(e);
                Console.WriteLine("Done, press any key to exit");
                Console.ReadLine();
            }
        }

        static async Task Run(string[] args)
        {
            if (!Kvm.HasAlias("default"))
                await Kvm.Upgrade();

            var kreBin = Kvm.GetRuntime("default");
            var appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Runt");
            if(!Directory.Exists(appData))
            {
                Directory.CreateDirectory(appData);
                Directory.CreateDirectory(Path.Combine(appData, "Runt.Bootstrap"));

                var globalContent = new JObject(
                    new JProperty("sources", new JArray())
                );
                File.WriteAllText(Path.Combine(appData, "global.json"), globalContent.ToString(Formatting.Indented));
            }


            var projectContent = new JObject(
                new JProperty("name", "Runt.Bootstrap"),
                new JProperty("dependencies", new JObject(
                    new JProperty("Runt.Web", new JValue("0.1-*"))
                )),
                new JProperty("commands", new JObject(
                    new JProperty("web", new JValue("Microsoft.AspNet.Hosting server=Microsoft.AspNet.Server.WebListener server.urls=http://localhost:5002"))
                ))
            );
            File.WriteAllText(Path.Combine(appData, "NuGet.Config"), @"
<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <packageSources>
    <add key=""Xunit.KRunner.AppVeyor"" value=""https://ci.appveyor.com/nuget/xunit-krunner-appveyor-nc1iiur8kt0q"" />
    <add key=""XUnit.KRunner"" value=""https://ci.appveyor.com/nuget/testing-41kgpkx6hvln"" />
    <add key=""AspNetVNext"" value=""https://www.myget.org/F/aspnetvnext/api/v2"" />
    <add key=""NuGet.org"" value=""https://nuget.org/api/v2/"" />
  </packageSources>
</configuration>");
            File.WriteAllText(Path.Combine(appData, "Runt.Bootstrap", "project.json"), projectContent.ToString(Formatting.Indented));

            AssertOk(await Update(kreBin, Path.Combine(appData, "Runt.Bootstrap")));
            Run(kreBin, Path.Combine(appData, "Runt.Bootstrap"));
        }

        static void AssertOk(int exitCode)
        {
            if (exitCode != 0)
                throw new Exception("Program failed with exit code: " + exitCode);
        }

        static Task<int> Update(string kreBin, string projectPath)
        {
            return Kre.From(kreBin)
               .Run(Path.Combine(kreBin, "lib", "Microsoft.Framework.PackageManager", "Microsoft.Framework.PackageManager.dll"))
               .WithArgs("restore")
               .At(projectPath)
               .Join();
        }

        static void Run(string kreBin, string projectPath)
        {
            Kre.From(kreBin)
                .Run("Microsoft.Framework.ApplicationHost")
                .WithAppBase(projectPath)
                .WithArgs("Microsoft.AspNet.Hosting", "app=Runt.Web", "server=Microsoft.AspNet.Server.WebListener", "server.urls=http://localhost:5002")
                .At(projectPath)
                .Join();
        }
    }
}
