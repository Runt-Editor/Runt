using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Runt.DesignTimeHost
{
    public class Host : IDisposable
    {
        readonly string _applicationRoot;
        readonly string _hostId;

        string _runtimePath;
        IDisposable _host;

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
        //public event EventHandler<MessageEventArgs> Message;

        private void OnConnected(EventArgs e)
        {
            var c = Connected;
            if (c != null)
                c(this, e);
        }

        private void Start()
        {
            var port = FreeTcpPort();
            _host = StartRuntime(port);
        }

        private IDisposable StartRuntime(int port)
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
            var queue = new ProcessingQueue(networkStream);

            queue.OnReceive += OnReceive;
            queue.Start();
            OnConnected(new EventArgs());

            return kreProcess;
        }

        private void OnReceive(Message obj)
        {
            throw new NotImplementedException();
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
