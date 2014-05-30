using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Runt.Service
{
    static class Dialog
    {
        public static JObject Browse(string path = null)
        {
            if (path == null)
                path = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

            var dir = new DirectoryInfo(path);
            var dirs = from d in dir.EnumerateDirectories()
                       select new JObject(
                           new JProperty("name", new JValue(d.Name)),
                           new JProperty("type", new JValue("dir"))
                        );
            var files = from f in dir.EnumerateFiles()
                        select new JObject(
                            new JProperty("name", new JValue(f.Name)),
                            new JProperty("type", new JValue("file"))
                        );

            return new JObject(
                new JProperty("name", new JValue("browse")),
                new JProperty("path", new JValue(path)),
                new JProperty("content", new JArray(dirs.Concat(files).ToArray()))
            );
        }
    }
}
