using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Newtonsoft.Json.Linq;

namespace Runt.DesignTimeHost
{
    public class Host : IDisposable
    {
        readonly string _applicationRoot;
        readonly string _hostId;

        string _runtimePath;
        ProcessingQueue _host;

        public bool IsConnected
        {
            get { return _host != null; }
        }

        public Host(string applicationRoot)
        {
            _hostId = Guid.NewGuid().ToString();
            _applicationRoot = applicationRoot;
        }

        public void Start(string runtimePath)
        {
            if (_host != null)
                _host.Dispose();

            _runtimePath = runtimePath;
            Start();
        }

        public event EventHandler Connected;
        private void OnConnected(EventArgs e)
        {
            var c = Connected;
            if (c != null)
                c(this, e);
        }

        public event EventHandler<ConfigurationsEventArgs> Configurations;
        private void OnConfigurations(ConfigurationsEventArgs e)
        {
            var c = Configurations;
            if (c != null)
                c(this, e);
        }

        private void Start()
        {
            var port = FreeTcpPort();
            _host = StartRuntime(port);
        }

        private ProcessingQueue StartRuntime(int port)
        {
            var psi = new ProcessStartInfo
            {
                FileName = Path.Combine(_runtimePath, "klr.exe"),
                Arguments = String.Format(@"--appbase ""{0}"" {1} {2} {3} {4}",
                                          Directory.GetCurrentDirectory(),
                                          Path.Combine(_runtimePath, "lib", "Microsoft.Framework.DesignTimeHost", "Microsoft.Framework.DesignTimeHost.dll"),
                                          port,
                                          Process.GetCurrentProcess().Id,
                                          _hostId),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                UseShellExecute = false
            };

            var kreProcess = Process.Start(psi);
            kreProcess.BeginOutputReadLine();
            kreProcess.BeginErrorReadLine();

            bool started = false;
            ManualResetEvent wait = new ManualResetEvent(false);
            string output = "";
            DataReceivedEventHandler waitingForOpen = null;
            waitingForOpen = (sender, e) =>
            {
                output += e.Data;
                if(output.Contains("Listening on port " + port))
                {
                    Volatile.Write(ref started, true);
                    wait.Set();
                    kreProcess.OutputDataReceived -= waitingForOpen;
                }
            };

            kreProcess.OutputDataReceived += waitingForOpen;
            if (kreProcess.HasExited)
                throw new Exception("Error starting kre");

            kreProcess.EnableRaisingEvents = true;
            kreProcess.Exited += (sender, e) =>
            {
                wait.Set();
                if (Volatile.Read(ref started))
                    Start();
            };

            wait.WaitOne();
            if (!Volatile.Read(ref started))
                throw new Exception("Error starting kre");

            var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            socket.Connect(new IPEndPoint(IPAddress.Loopback, port));

            var networkStream = new NetworkStream(socket);
            var mapping = new Dictionary<int, string>();
            var queue = new ProcessingQueue(networkStream, kreProcess);

            queue.OnReceive += OnReceive;
            queue.Start();
            OnConnected(new EventArgs());

            return queue;
        }

        public void InitProject(int id, string path)
        {
            var payload = new Outgoing.InitializeMessage
            {
                ProjectFolder = path,
                TargetFramework = "net45"
            };

            var msg = new Message
            {
                ContextId = id,
                HostId = _hostId,
                MessageType = "Initialize",
                Payload = JToken.FromObject(payload)
            };

            if (_host != null)
                _host.Post(msg);
        }

        private void OnReceive(Message obj)
        {
            switch (obj.MessageType)
            {
                case "Configurations":
                    var configurations = obj.Payload.ToObject<Incomming.ConfigurationsMessage>();
                    OnConfigurations(new ConfigurationsEventArgs(obj.ContextId, configurations));
                    break;

                default:
                    break;
            }
        }

        public void Dispose()
        {
            _host.Dispose();
        }

        static int FreeTcpPort()
        {
            TcpListener l = new TcpListener(IPAddress.Loopback, 0);
            l.Start();
            int port = ((IPEndPoint)l.LocalEndpoint).Port;
            l.Stop();
            return port;
        }
    }
}
