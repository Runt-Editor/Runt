using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Runt.DesignTimeHost
{
    class ProcessingQueue : IDisposable
    {
        private readonly BinaryReader _reader;
        private readonly BinaryWriter _writer;
        private readonly Stream _stream;
        private readonly Process _process;

        public event Action<Message> OnReceive;

        public ProcessingQueue(Stream stream, Process process)
        {
            _reader = new BinaryReader(stream);
            _writer = new BinaryWriter(stream);
            _stream = stream;
            _process = process;
        }

        public void Start()
        {
            Trace.TraceInformation("[ProcessingQueue]: Start()");
            new Thread(ReceiveMessages).Start();
        }

        public void Post(Message message)
        {
            lock (_writer)
            {
                Trace.TraceInformation("[ProcessingQueue]: Post({0})", message);
                _writer.Write(JsonConvert.SerializeObject(message));
            }
        }

        private void ReceiveMessages()
        {
            try
            {
                while (true)
                {
                    var message = JsonConvert.DeserializeObject<Message>(_reader.ReadString());
                    Trace.TraceInformation("[ProcessingQueue]: OnReceive({0})", message);
                    OnReceive(message);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public void Dispose()
        {
            try { _process.Close(); } catch { }
            try { _process.Dispose(); } catch { }
            _reader.Dispose();
            _writer.Dispose();
            _stream.Dispose();
        }
    }
}
