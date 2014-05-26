using System.Collections.Immutable;
using Runt.DesignTimeHost.Incomming;

namespace Runt.DesignTimeHost
{
    public class ConfigurationsEventArgs : ProjectEventArgs
    {
        readonly ConfigurationsMessage _configurations;

        public ConfigurationsEventArgs(int contextId, ConfigurationsMessage configurations)
            : base(contextId)
        {
            _configurations = configurations;
        }

        public string ProjectName
        {
            get { return _configurations.ProjectName; }
        }

        public IImmutableList<ConfigurationData> Configurations
        {
            get { return _configurations.Configurations.ToImmutableArray(); }
        }

        public IImmutableDictionary<string, string> Commands
        {
            get { return _configurations.Commands.ToImmutableDictionary(); }
        }
    }
}
