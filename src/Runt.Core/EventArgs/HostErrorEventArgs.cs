using Runt.DesignTimeHost.Incomming;

namespace Runt.Core
{
    public class HostErrorEventArgs
    {
        readonly ErrorMessage _message;

        public HostErrorEventArgs(int contextId, ErrorMessage message)
        {
            _message = message;
        }

        public string Message
        {
            get { return _message.Message; }
        }
    }
}
