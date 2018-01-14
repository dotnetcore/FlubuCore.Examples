using System;
using System.Collections.Generic;
using System.Text;
using FlubuCore.Context.FluentInterface.Interfaces;
using FlubuCore.PluginExample;

// ReSharper disable once CheckNamespace
namespace FlubuCore.Context.FluentInterface.Interfaces
{
   public static class TaskFluentInterfaceExtension
    {
        public static ExampleFlubuPluginTask ExampleFlubuPluginTask(this ITaskFluentInterface flubu)
        {
            return new ExampleFlubuPluginTask();
        }
    }
}
