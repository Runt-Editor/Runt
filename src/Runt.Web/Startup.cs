using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.FileSystems;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.StaticFiles;
using Microsoft.Framework.DependencyInjection;
using Runt.Core;
using Runt.Service;

namespace Runt.Web
{
    public class Startup
    {
        readonly IFileSystem fileSystem = new PhysicalFileSystem(@"C:\Users\alxan_000\Documents\GitHub\Runt\src\Runt.Web");
        readonly IContentTypeProvider contentTypeProvider = new FileExtensionContentTypeProvider(new Dictionary<string, string>
        {
            { ".map", "application/json" },
            { ".ts", "application/typescript" },
            { ".js", "application/javascript" },
            { ".html", "text/html" }
        });

        public void Configure(IBuilder app)
        {
            app.UseServices(services =>
            {
                services.AddSignalR();
                services.AddTransient<IEditor, Editor>();
            });

            app.UseSignalR("/io", typeof(RuntConnection), new ConnectionConfiguration
            {
                EnableJSONP = false
            });
            app.UseDefaultFiles();
            app.UseStaticFiles(new StaticFileOptions
            {
                FileSystem = fileSystem,
                ContentTypeProvider = contentTypeProvider,
                ServeUnknownFileTypes = true
            });
            app.UseDirectoryBrowser(new DirectoryBrowserOptions
            {
                FileSystem = fileSystem
            });
        }
    }
}