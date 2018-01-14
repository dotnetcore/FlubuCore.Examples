using FlubuCore.Context;
using FlubuCore.Tasks;

namespace FlubuCore.PluginExample
{
    public class ExampleFlubuPluginTask : TaskBase<int, ExampleFlubuPluginTask>
    {
        private string _description;

        private string _message;

        public ExampleFlubuPluginTask ExampleFluentInterface(string message)
        {
            _message = message;
            return this;
        }

        protected override int DoExecute(ITaskContextInternal context)
        {
            //// write task logic here.
            context.LogInfo(!string.IsNullOrEmpty(_message) ? _message : "Just some dummy code");

            return 0;
        }

        protected override string Description
        {
            get => string.IsNullOrEmpty(_description) ? $"Example plugin task.." : _description;

            set => _description = value;
        }
    }
}
