using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Runt.Bootstrap
{
    internal class Kre
    {
        readonly string _bin;
        string _program;
        string[] _args;
        string _workingDir;
        string _appBase;

        public static Kre From(string path)
        {
            return new Kre(path);
        }

        private Kre(string bin)
        {
            _bin = bin;
        }

        public Kre Run(string program)
        {
            _program = program;
            return this;
        }

        public Kre WithArgs(params string[] args)
        {
            _args = args;
            return this;
        }

        public Kre At(string path)
        {
            _workingDir = path;
            return this;
        }

        public Kre WithAppBase(string appBase)
        {
            _appBase = appBase;
            return this;
        }

        private Process CreateProcess()
        {
            var args = new List<string>();
            if (_appBase != null)
            {
                args.Add("--appbase");
                args.Add(_appBase);
            }

            args.Add(_program);
            args.AddRange(_args);

            var psi = new ProcessStartInfo
            {
                FileName = Path.Combine(_bin, "klr.exe"),
                Arguments = string.Join(" ", args.Select(MaybeQuote)),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                UseShellExecute = false,
                WorkingDirectory = _workingDir ?? Environment.CurrentDirectory
            };

            return new Process
            {
                StartInfo = psi
            };
        }

        public void Start()
        {
            var process = CreateProcess();
            process.StartInfo.CreateNoWindow = false;
            process.StartInfo.RedirectStandardError = false;
            process.StartInfo.RedirectStandardInput = false;
            process.StartInfo.RedirectStandardOutput = false;
            process.Start();
        }

        public Task<int> Join()
        {
            return Task.Run(() =>
            {
                var process = CreateProcess();

                DataReceivedEventHandler output = (s, e) =>
                {
                    Console.WriteLine(e.Data);
                };

                DataReceivedEventHandler error = (s, e) =>
                {
                    Console.Error.WriteLine(e.Data);
                };

                process.EnableRaisingEvents = true;
                process.OutputDataReceived += output;
                process.ErrorDataReceived += error;
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();
                return process.ExitCode;
            });
        }

        static string MaybeQuote(string arg)
        {
            arg = arg.Trim().Trim('"').Trim();
            if (arg.IndexOf(' ') != -1)
                return "\"" + arg + "\"";
            return arg;
        }
    }
}
