using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Runt.Service
{
    static class Dialog
    {
        public static JObject Browse(string path = null)
        {
            if (path == null)
                path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            var dir = new DirectoryInfo(path);
            var dirs = from d in dir.EnumerateDirectories()
                       select new JObject(
                           new JProperty("name", new JValue(d.Name)),
                           new JProperty("type", new JValue("dir")),
                           new JProperty("path", new JValue(d.FullName))
                        );
            var files = from f in dir.EnumerateFiles()
                        select new JObject(
                            new JProperty("name", new JValue(f.Name)),
                            new JProperty("type", new JValue("file")),
                            new JProperty("path", new JValue(f.FullName))
                        );

            if (dir.Parent != null)
                dirs = new[] { new JObject(
                           new JProperty("name", new JValue("..")),
                           new JProperty("type", new JValue("dir")),
                           new JProperty("path", new JValue(dir.Parent.FullName))
                        ) }.Concat(dirs);

            return new JObject(
                new JProperty("name", new JValue("browse")),
                new JProperty("title", new JValue("Open project")),
                new JProperty("path", new JValue(path)),
                new JProperty("folders", new JArray(dirs.ToArray())),
                new JProperty("files", new JArray(files.ToArray()))
            );
        }
    }
}
