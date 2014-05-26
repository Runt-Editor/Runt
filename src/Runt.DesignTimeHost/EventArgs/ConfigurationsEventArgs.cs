using System.Collections.Immutable;
using Runt.DesignTimeHost.Incomming;

namespace Runt.DesignTimeHost
{
    public class ConfigurationsEventArgs : ProjectEventArgs
    {
        readonly ConfigurationsMessage _message;

        public ConfigurationsEventArgs(int contextId, ConfigurationsMessage message)
            : base(contextId)
        {
            _message = message;
        }

        public string ProjectName
        {
            get { return _message.ProjectName; }
        }

        public IImmutableList<ConfigurationData> Configurations
        {
            get { return _message.Configurations.ToImmutableArray(); }
        }

        public IImmutableDictionary<string, string> Commands
        {
            get { return _message.Commands.ToImmutableDictionary(); }
        }
    }
}
