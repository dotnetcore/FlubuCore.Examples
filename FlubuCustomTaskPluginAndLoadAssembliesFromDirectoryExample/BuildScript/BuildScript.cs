using FlubuCore.Context;
using FlubuCore.Context.FluentInterface.Interfaces;
using FlubuCore.Scripting;
using Newtonsoft.Json;

/// <summary>
/// Flubu loads Newtonsoft.json and FlubuCore.PluginExample assembly from FlubuLib directory
/// If you dont put the into FlubuLib directory you can also refernce them with #ass or #nuget directive. See wiki - BuildScript fundamentals for more information.
/// </summary>
public class BuildScript : DefaultBuildScript
{
    protected override void ConfigureBuildProperties(IBuildPropertiesContext context)
    {
        context.Properties.Set(BuildProps.CompanyName, "Flubu");
        context.Properties.Set(BuildProps.CompanyCopyright, "Copyright (C) 2010-2018 Flubu");
        context.Properties.Set(BuildProps.ProductId, "FlubuExample");
        context.Properties.Set(BuildProps.ProductName, "FlubuExample");
        context.Properties.Set(BuildProps.SolutionFileName, "FlubuExample.sln");
    }

    protected override void ConfigureTargets(ITaskContext context)
    {
         context.CreateTarget("FlubuPlugin.Example")
            .SetAsDefault()
            .Do(DoPluginExample);

        context.CreateTarget("FlubuPlugin.Example2")
            .AddTask(x => x.ExampleFlubuPluginTask());
    }

    private void DoPluginExample(ITaskContext context)
    {
        //// just example that JsonConver works(Newtonsoft.json assembly got loaded from FlubuLib directory)
        JsonConvert.SerializeObject("test");

        //// Execution of custom written flubu task in FlubuCore.PluginExample assembly.
        context.Tasks().ExampleFlubuPluginTask()
            .ExampleFluentInterface("some example message from plugin")
            .Execute(context);
    }
}
