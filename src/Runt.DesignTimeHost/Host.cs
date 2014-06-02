using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Runt.Core;

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

        public event EventHandler<ReferencesEventArgs> References;
        private void OnReferences(ReferencesEventArgs e)
        {
            var r = References;
            if (r != null)
                r(this, e);
        }

        public event EventHandler<DiagnosticsEventArgs> Diagnostics;
        private void OnDiagnostics(DiagnosticsEventArgs e)
        {
            var d = Diagnostics;
            if (d != null)
                d(this, e);
        }

        public event EventHandler<SourcesEventArgs> Sources;
        private void OnSources(SourcesEventArgs e)
        {
            var s = Sources;
            if (s != null)
                s(this, e);
        }

        public event EventHandler<HostErrorEventArgs> Error;
        private void OnError(HostErrorEventArgs e)
        {
            var er = Error;
            if (er != null)
                er(this, e);

            // restart host
            _host.Dispose();
            Start();
        }

        private void Start()
        {
            var port = FreeTcpPort();
            StartRuntime(port);
        }

        private void StartRuntime(int port)
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

            _host = queue;
            OnConnected(new EventArgs());
        }

        public Task RestorePackages(string runtime, string workingDir)
        {
            return Task.Run(() =>
            {
                var psi = new ProcessStartInfo
                {
                    FileName = Path.Combine(runtime, "klr.exe"),
                    Arguments = String.Format(@"""{0}"" restore",
                                          Path.Combine(runtime, "lib", "Microsoft.Framework.PackageManager", "Microsoft.Framework.PackageManager.dll")),
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    WorkingDirectory = workingDir
                };

                var kreProcess = Process.Start(psi);
                kreProcess.BeginOutputReadLine();
                kreProcess.BeginErrorReadLine();
                kreProcess.EnableRaisingEvents = true;
                kreProcess.WaitForExit();
            });
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

                case "References":
                    var references = obj.Payload.ToObject<Incomming.ReferencesMessage>();
                    OnReferences(new ReferencesEventArgs(obj.ContextId, references));
                    break;

                case "Diagnostics":
                    var diagnostics = obj.Payload.ToObject<Incomming.DiagnosticsMessage>();
                    OnDiagnostics(new DiagnosticsEventArgs(obj.ContextId, diagnostics));
                    break;

                case "Sources":
                    var sources = obj.Payload.ToObject<Incomming.SourcesMessage>();
                    OnSources(new SourcesEventArgs(obj.ContextId, sources));
                    break;

                case "Error":
                    var error = obj.Payload.ToObject<Incomming.ErrorMessage>();
                    OnError(new HostErrorEventArgs(obj.ContextId, error));
                    break;

                default:
                    if (Debugger.IsAttached)
                        Debugger.Break();
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
