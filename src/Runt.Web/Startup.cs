using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.FileSystems;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.StaticFiles;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Runtime;
using Runt.Core;
using Runt.Service;

namespace Runt.Web
{
    public class Startup
    {
        readonly IContentTypeProvider contentTypeProvider = new FileExtensionContentTypeProvider(new Dictionary<string, string>
        {
            { ".map", "application/json" },
            { ".ts", "application/typescript" },
            { ".js", "application/javascript" },
            { ".html", "text/html" }
        });

        public void Configure(IBuilder app, ILibraryManager libManager, IApplicationShutdown shutdown)
        {
            var web = libManager.GetLibraryInformation("Runt.Web");

            Console.WriteLine("Path: " + web.Path);
            Console.WriteLine("Name: " + web.Name);
            var fileSystem = new PhysicalFileSystem(Path.GetDirectoryName(web.Path));

            app.UseServices(services =>
            {
                services.AddSignalR();
                services.AddSingleton<IEditor, Editor>();
            });

            app.UseSignalR("/io", typeof(RuntConnection), new ConnectionConfiguration
            {
                EnableJSONP = false
            });
            app.UseDefaultFiles(new DefaultFilesOptions
            {
                FileSystem = fileSystem
            });
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