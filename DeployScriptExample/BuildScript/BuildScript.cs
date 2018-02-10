using System;
using System.Collections.Generic;
using System.Text;
using FlubuCore.Context;
using FlubuCore.Packaging;
using FlubuCore.Scripting;

namespace BuildScript.BuildScript
{
    //// see wiki for detailed tutorial about how to execute script remotely with flubu web api https://github.com/flubu-core/flubu.core/wiki/6-Web-Api:-Getting-started
    public class BuildScript : DefaultBuildScript
    {
        protected override void ConfigureBuildProperties(IBuildPropertiesContext context)
        {
            context.Properties.Set(BuildProps.FlubuWebApiBaseUrl, "http://localhost:5000/");
        }

        /// <summary>
        /// Script takes already prepared deploy package. Normaly your build server would prepare the package.
        /// </summary>
        /// <param name="session"></param>
        protected override void ConfigureTargets(ITaskContext session)
        {
            session.CreateTarget("package")
                .AddTask(x => x.PackageTask("").AddDirectoryToPackage("WebApplication1\\Scripts", "ExampleApp\\Scripts", true)
                    .AddDirectoryToPackage("WebApplication1\\Views", "ExampleApp\\Views", true)
                    .AddDirectoryToPackage("WebApplication1\\Content", "ExampleApp\\Content", true)
                    .AddDirectoryToPackage("WebApplication1\\Bin", "ExampleApp\\Bin", true)
                    .AddDirectoryToPackage("WebApplication1", "ExampleApp", false, new NegativeFilter(new RegexFileFilter(@"^.*\.(svc|asax|config|js|html|ico|bat)$")))
                    .ZipPackage("ExampleApp.zip"));

            session.CreateTarget("deploy")
                .SetAsDefault()
                .AddTask(x => x.FlubuWebApiTasks().GetTokenTask("test", "test"))
                .AddTask(x => x.FlubuWebApiTasks().DeletePackagesTask())
                .AddTask(x => x.FlubuWebApiTasks().UploadPackageTask(".\\BuildScript\\ExamplePackages", "*.zip"))
                .AddTask(x => x.FlubuWebApiTasks().ExecuteScriptTask("deploy", "DeployScript.cs"));
        }
    }
}
