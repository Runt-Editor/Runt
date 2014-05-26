using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Runt
{
    public static class Kvm
    {
        public struct KRuntime
        {
            readonly string _version;
            readonly string _runtime;
            readonly string _architecture;
            readonly string _location;
            readonly bool _active;

            public KRuntime(string location, string version, string runtime, string architecture, bool active)
            {
                _version = version;
                _runtime = runtime;
                _architecture = architecture;
                _location = location;
                _active = active;
            }

            public string Version { get { return _version; } }
            public string Runtime { get { return _runtime; } }
            public string Architecture { get { return _architecture; } }
            public string Location { get { return _location; } }
            public bool Active { get { return _active;  } }

            internal string PackageName
            {
                get { return "KRE-" + Runtime + "-" + Architecture; }
            }

            internal static KRuntime Parse(string fullName)
            {
                var firstDot = fullName.IndexOf('.');
                var lastDashBefore = fullName.Substring(0, firstDot).LastIndexOf('-');

                string runtime = null, arch = null, version = fullName;
                if (lastDashBefore != -1)
                {
                    var nameParts = fullName.Substring(0, lastDashBefore).Split(new[] { '-' }, 3);
                    runtime = nameParts[1];
                    arch = nameParts[2];
                    version = fullName.Substring(lastDashBefore + 1);
                }

                return new KRuntime(null, version, runtime, arch, false);
            }
        }

        public struct KAlias
        {
            readonly string _alias;
            readonly string _name;

            public KAlias(string alias, string name)
            {
                _alias = alias;
                _name = name;
            }

            public string Alias { get { return _alias; } }
            public string Name { get { return _name; } }
        }

        const string packages = "packages";
        const string alias = "alias";
        const string defaultPlatform = "svr50";
        const string defaultArchitecture = "x86";
        const string defaultAlias = "default";

        const string credentialsUser = "aspnetreadonly";
        const string credentialsPassword = "4d8a2d9c-7b80-4162-9978-47e918c9658c";

        static readonly string globalKrePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "KRE");
        static readonly string userKrePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".kre");
        static readonly string userKrePackages = Path.Combine(userKrePath, packages);
        static readonly string userKreAlias = Path.Combine(userKrePath, alias);
        static readonly NetworkCredential credentials = new NetworkCredential(credentialsUser, credentialsPassword);

        public static string AliasDirectory
        {
            get { return userKreAlias; }
        }

        public static string GetRuntime(string alias)
        {
            return LocateKreBinFromFullName(VersionOrAlias(alias));
        }

        public static async Task Upgrade(string platform = defaultPlatform, string architecture = defaultArchitecture)
        {
            var version = await FindLatest(platform, architecture);
            await Install(version, platform, architecture);
            AliasSet(defaultAlias, version);
        }

        static async Task<string> FindLatest(string platform = defaultPlatform, string architecture = defaultArchitecture)
        {
            const string URL = "https://www.myget.org/F/aspnetvnext/api/v2/GetUpdates()?packageIds=%27KRE-{0}-{1}%27&versions=%270.0%27&includePrerelease=true&includeAllVersions=false";
            var url = string.Format(CultureInfo.InvariantCulture, URL, platform, architecture);

            using(var client = new WebClient())
            {
                client.Credentials = credentials;
                var xml = await client.DownloadStringTaskAsync(url);
                var xdoc = new XmlDocument();
                xdoc.LoadXml(xml);
                var nsmgr = new XmlNamespaceManager(xdoc.NameTable);
                nsmgr.AddNamespace("d", "http://schemas.microsoft.com/ado/2007/08/dataservices");
                var elm = xdoc.DocumentElement.SelectSingleNode("//d:Version", nsmgr);
                return elm.InnerText;
            }
        }

        static async Task InstallLatest(string platform = defaultPlatform, string architecture = defaultArchitecture)
        {
            var latest = await FindLatest(platform, architecture);
            await Install(latest);
        }

        /// <summary>
        /// Downloads a KRE
        /// </summary>
        /// <param name="fullName"></param>
        /// <param name="folder"></param>
        /// <returns><c>true</c> if not already downloaded, otherwise <c>false</c>.</returns>
        static async Task<bool> Download(string fullName, string folder)
        {
            const string URL = "https://www.myget.org/F/aspnetvnext/api/v2/package/{0}/{1}";

            var runtime = KRuntime.Parse(fullName);
            
            var parts = new[] {
                runtime.PackageName,
                runtime.Version
            };

            var url = String.Format(CultureInfo.InvariantCulture, URL, parts);
            var file = Path.Combine(folder, fullName + ".nupkg");

            if (Directory.Exists(folder))
                return false;

            Directory.CreateDirectory(folder);

            using(var client = new WebClient())
            {
                client.Credentials = credentials;
                await client.DownloadFileTaskAsync(url, file);
            }

            await Unpack(file, folder);
            return true;
        }

        static Task Unpack(string file, string folder)
        {
            const string contentTypesXml = "[Content_Types].xml";
            const string rels = "_rels";
            const string package = "package";

            return Task.Run(() =>
            {
                ZipFile.ExtractToDirectory(file, folder);

                if (File.Exists(Path.Combine(folder, contentTypesXml)))
                    File.Delete(Path.Combine(folder, contentTypesXml));

                if (Directory.Exists(Path.Combine(folder, rels)))
                    Directory.Delete(Path.Combine(folder, rels), true);

                if (Directory.Exists(Path.Combine(folder, package)))
                    Directory.Delete(Path.Combine(folder, package), true);
            });
        }

        static async Task Install(string versionOrAlias, string platform = defaultPlatform, string architecture = defaultArchitecture)
        {
            if(Path.GetExtension(versionOrAlias).Equals(".nupkg", StringComparison.OrdinalIgnoreCase))
            {
                var fullName = Path.GetFileNameWithoutExtension(versionOrAlias);
                var folder = Path.Combine(userKrePackages, fullName);
                var file = Path.Combine(folder, fullName + ".nupkg");

                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                    await CopyAsync(versionOrAlias, file);
                    await Unpack(file, folder);
                }

                var bin = Path.Combine(folder, "bin");
                SetPath(ChangePath(Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User), bin, globalKrePath, userKrePath));
            }
            else
            {
                var fullName = VersionOrAlias(versionOrAlias, platform, architecture);
                var folder = Path.Combine(userKrePackages, fullName);

                await Download(fullName, folder);
                Use(versionOrAlias);
            }
        }

        public static IEnumerable<KRuntime> List()
        {
            var home = Environment.GetEnvironmentVariable("KRE_HOME");
            if (string.IsNullOrEmpty(home))
                home = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "KRE") + ";%USERPROFILE%\\.kre";

            var items = new List<KRuntime>();
            foreach(var portion in home.Split(';'))
            {
                var path = Environment.ExpandEnvironmentVariables(portion);
                if (Directory.Exists(Path.Combine(path, packages)))
                    items.AddRange(ListParts(Path.Combine(path, packages)));
            }

            return from r in items
                   orderby r.Version, r.Runtime, r.Architecture
                   select r;
        }

        static IEnumerable<KRuntime> ListParts(string packagesFolder)
        {
            foreach (var path in new DirectoryInfo(packagesFolder).EnumerateDirectories("KRE-*", SearchOption.TopDirectoryOnly))
            {
                if (!Directory.Exists(Path.Combine(path.FullName, "bin")))
                    continue;

                var active = false;
                foreach (var portion in Environment.ExpandEnvironmentVariables("%PATH%").Split(';'))
                    if (portion.StartsWith(path.FullName, StringComparison.OrdinalIgnoreCase))
                        active = true;

                var parts1 = path.Name.Split(new[] { '.' }, 2);
                var parts2 = parts1[0].Split(new[] { '-' }, 3);
                yield return new KRuntime(
                    active: active,
                    version: parts1[1],
                    runtime: parts2[1],
                    architecture: parts2[2],
                    location: path.FullName);
            }
        }

        static void Use(string versionOrAlias)
        {
            if(versionOrAlias == null)
            {
                SetPath(ChangePath(Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User), "", globalKrePath, userKrePackages));
                return;
            }

            var fullName = VersionOrAlias(versionOrAlias);
            var bin = LocateKreBinFromFullName(fullName);
            if (bin == null)
                throw new ArgumentException("Version not found");

            SetPath(ChangePath(Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User), bin, globalKrePath, userKrePackages));
        }

        public static IEnumerable<KAlias> ListAlias()
        {
            if (!Directory.Exists(userKreAlias))
                Directory.CreateDirectory(userKreAlias);

            foreach (var aliasFile in new DirectoryInfo(userKreAlias).EnumerateFiles())
            {
                var name = File.ReadAllText(aliasFile.FullName, Encoding.ASCII).Trim();
                yield return new KAlias(
                    alias: Path.GetFileNameWithoutExtension(aliasFile.Name),
                    name: name);
            }
        }

        static string AliasGet(string name)
        {
            if (!Directory.Exists(userKreAlias))
                Directory.CreateDirectory(userKreAlias);

            var file = Path.Combine(userKreAlias, name + ".txt");
            if (File.Exists(file))
                return File.ReadAllText(file, Encoding.ASCII).Trim();

            throw new ArgumentException("Alias not found");
        }

        static void AliasSet(string name, string value)
        {
            if (!Directory.Exists(userKreAlias))
                Directory.CreateDirectory(userKreAlias);

            var file = Path.Combine(userKreAlias, name + ".txt");
            File.WriteAllText(file, value + Environment.NewLine, Encoding.ASCII);
        }

        public static bool HasAlias(string name)
        {
            if (!Directory.Exists(userKreAlias))
                Directory.CreateDirectory(userKreAlias);

            var file = Path.Combine(userKreAlias, name + ".txt");
            return File.Exists(file);
        }

        static string LocateKreBinFromFullName(string fullName)
        {
            var home = Environment.GetEnvironmentVariable("KRE_HOME");
            if (string.IsNullOrEmpty(home))
                home = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "KRE") + ";%USERPROFILE%\\.kre";

            foreach (var portion in home.Split(';'))
            {
                var path = Environment.ExpandEnvironmentVariables(portion);
                if (Directory.Exists(Path.Combine(path, packages, fullName, "bin")))
                    return Path.Combine(path, packages, fullName, "bin");
            }

            return null;
        }

        static string VersionOrAlias(string versionOrAlias, string platform = defaultPlatform, string architecture = defaultArchitecture)
        {
            string version;
            if (File.Exists(Path.Combine(userKreAlias, versionOrAlias + ".txt")))
            {
                var runtime = KRuntime.Parse(AliasGet(versionOrAlias));
                version = runtime.Version;
                platform = runtime.Runtime ?? platform;
                architecture = runtime.Architecture ?? architecture;
            }
            else
            {
                version = versionOrAlias;
            }

            return "KRE-" + platform + "-" + architecture + "-" + version;
        }

        static string ChangePath(string oldPath, string prependPath, params string[] removePaths)
        {
            var newPath = prependPath;
            foreach(var portion in oldPath.Split(';'))
            {
                var skip = string.IsNullOrEmpty(portion);
                foreach(var rp in removePaths)
                    if (portion.StartsWith(rp, StringComparison.OrdinalIgnoreCase))
                        skip = true;

                if (!skip)
                    newPath = newPath + ';' + portion;
            }

            return newPath;
        }

        static void SetPath(string newPath)
        {
            Environment.SetEnvironmentVariable("PATH", newPath, EnvironmentVariableTarget.User);
        }

        static T Default<T>(T[] array, int index, T defaultValue)
        {
            return array.Length > index ? defaultValue : array[index];
        }

        static async Task CopyAsync(string source, string destination)
        {
            using (var sourceStream = File.OpenRead(source))
            using (var destinationStream = File.OpenWrite(destination))
                await sourceStream.CopyToAsync(destinationStream);
        }
    }
}
