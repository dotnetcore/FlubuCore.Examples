using FlubuCore.Context;
using FlubuCore.Scripting;

/// <summary>
/// Examine flubusettings.json
/// </summary>
public class MyBuildScript : DefaultBuildScript
{
    protected override void ConfigureBuildProperties(IBuildPropertiesContext context)
    {
        context.Properties.Set(BuildProps.CompanyName, "Flubu");
        context.Properties.Set(BuildProps.CompanyCopyright, "Copyright (C) 2010-2016 Flubu");
        context.Properties.Set(BuildProps.ProductId, "FlubuExample");
        context.Properties.Set(BuildProps.ProductName, "FlubuExample");
        context.Properties.Set(BuildProps.SolutionFileName, "FlubuExample.sln");
        ////   context.Properties.Set(BuildProps.BuildConfiguration, "Release");
    }

    protected override void ConfigureTargets(ITaskContext context)
    {
        context
            .CreateTarget("compile")
            .SetDescription("Compiles the VS solution.")
            .AddCoreTask(x => x.Restore())
             //// this is just example and in real scenario is recomended to set BuildConfiguration only through build properties as other tasks might use it. 
            .AddCoreTask(x => x.Build().ForMember(y => y.Configuration("Debug"), "c")); //// Debug is default value. If not set in enviroment variable, config or pass through from argument.

        //// run in cmd 'dotnet flubu -key2=5'
        context.CreateTarget("DoExample").SetAsDefault().Do(DoExample, "DefaultValue", 0, taskAction: o =>
        {
            o.SetTaskName("Do example with ForMember");
            o.ForMember(x => x.Param, "key1", "custom help");
            o.ForMember(x => x.Param2, "key2"); //// adds default help to target help. Run 'dotnet flubu DoExample help'
        });

    }

    private void DoExample(ITaskContext context, string param1, int param2)
    {
       context.LogInfo(param1);
       context.LogInfo($"Integer value: {param2}");
    }
}