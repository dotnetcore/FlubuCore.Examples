using System;
using System.IO;
using System.Linq;
using FlubuCore.Context;
using FlubuCore.Context.FluentInterface.Interfaces;
using FlubuCore.Scripting;
using FlubuCore.Tasks.Iis;

namespace BuildScript
{
   
    //// Deploy script can be executed with or without flubu web api. If u want to execute deploy script
    //// remotely execute it with flubu web api otherwise if u want to execute it locally execute it with flubu runner or dotnet cli tool.
    //// see wiki for detailed tutorial about how to execute script remotely with flubu web api https://github.com/flubu-core/flubu.core/wiki/6-Web-Api:-Getting-started
    ////
    //// More complex deploy script from real project: https://github.com/flubu-core/examples/blob/master/MVC_NET4.61/BuildScripts/DeployScriptComplexFromRealProjectExample.cs
    //// so that u get an idea what can be done with FlubuCore 
    public class DeployScript : DefaultBuildScript
    {
        protected override void ConfigureBuildProperties(IBuildPropertiesContext context)
        {
        }

        protected override void ConfigureTargets(ITaskContext session)
        {
            session.CreateTarget("deploy.local").AddTasks(Deploy, "c:\\ExamplaApp").SetAsDefault();

            session.CreateTarget("deploy.test").AddTasks(Deploy, "d:\\ExamplaApp");

            session.CreateTarget("deploy.prod").AddTasks(Deploy, "e:\\ExamplaApp");

        }

        private void Deploy(ITarget target, string deployPath)
        {
            target
                .AddTask(x => x.IisTasks().CreateAppPoolTask("Example app pool").Mode(CreateApplicationPoolMode.DoNothingIfExists))
                .AddTask(x => x.IisTasks().ControlAppPoolTask("Example app pool", ControlApplicationPoolAction.Stop).DoNotFailOnError())
                .Do(UnzipPackage)
                .AddTask(x => x.CopyDirectoryStructureTask(@"Packages\ExampleApp", deployPath, true).Retry(20, 5000))
                .Do(CreateWebSite)
                .AddTask(x => x.IisTasks().ControlAppPoolTask("Example app pool", ControlApplicationPoolAction.Start));
        }

        private void UnzipPackage(ITaskContext context)
        {
            var files = Directory.EnumerateFiles("packages", "*.zip").ToList();
            string zip = files[0];
            context.Tasks().UnzipTask(zip, "packages").Execute(context);
        }

        private void CreateWebSite(ITaskContext context)
        {
            context.Tasks().IisTasks().CreateWebsiteTask()
                .ApplicationPoolName("Example app pool")
                .WebsiteName("Example web site")
                .BindingProtocol("Http")
                .Port(3080)
                .PhysicalPath("C:\\ExampleApp")
                .WebsiteMode(CreateWebApplicationMode.DoNothingIfExists).Execute(context);
        }
    }
}
