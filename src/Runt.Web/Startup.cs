using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.FileSystems;
using Microsoft.AspNet.StaticFiles;
using Microsoft.Framework.DependencyInjection;

namespace Runt.Web
{
    public class Startup
    {
        readonly IFileSystem fileSystem = new PhysicalFileSystem(@"C:\Users\alxan_000\Documents\Visual Studio 2013\Projects\Runt\src\Runt.Web");
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
            });

            app.UseSignalR();
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