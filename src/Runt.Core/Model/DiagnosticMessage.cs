using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace Runt.Core.Model
{
    public enum DiagnosticKind
    {
        Error = 0,
        Warning = 1,
        Info = 2
    }

    public class DiagnosticMessage
    {
        static Regex _parser;

        static DiagnosticMessage()
        {
            var invalidPathChars = Path.GetInvalidPathChars();
            var invalidFileNameChars = Path.GetInvalidFileNameChars();

            var pathPart = "[^" + Regex.Escape(string.Join("", invalidPathChars)) + "]+?";
            var namePart = "[^" + Regex.Escape(string.Join("", invalidFileNameChars)) + "]+?";

            var path = "(?<file>(" + pathPart + ")+?(" + namePart + "))";
            var loc = @"\((?<line>\d+),(?<col>\d+)\)";
            var type = "(?<type>[a-z]+)";
            var errorCode = "(?<code>[a-z0-9]+)";
            var message = "(?<message>.*)";

            var regex = "^" + path + loc + ": " + type + " " + errorCode + ": " + message + "$";


            _parser = new Regex(regex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase | RegexOptions.Singleline);
        }

        public static DiagnosticMessage Create(string rawMessage)
        {
            // Format: {file}({line},{column}): {type} {error-code}: {message}
            var match = _parser.Match(rawMessage);
            if (!match.Success)
                throw new ArgumentException("Message format was unexpected", "rawMessage");

            var file = match.Groups["file"].Value;
            var line = int.Parse(match.Groups["line"].Value);
            var col = int.Parse(match.Groups["col"].Value);
            var type = match.Groups["type"].Value;
            var code = match.Groups["code"].Value;
            var msg = match.Groups["message"].Value;

            DiagnosticKind kind;
            switch (type.ToLowerInvariant())
            {
                case "error":
                    kind = DiagnosticKind.Error;
                    break;

                case "warning":
                    kind = DiagnosticKind.Warning;
                    break;

                case "info":
                    kind = DiagnosticKind.Info;
                    break;

                default:
                    throw new ArgumentException("Unknown type: " + type, "rawMessage");
            }

            return new DiagnosticMessage(kind, msg, file, line, col, code);
        }

        readonly string _file;
        readonly int _line;
        readonly int _column;
        readonly string _code;
        readonly string _message;
        readonly DiagnosticKind _kind;

        private DiagnosticMessage(DiagnosticKind kind, string message, string file, int line, int column, string code)
        {
            _kind = kind;
            _message = message;
            _file = file;
            _line = line;
            _column = column;
            _code = code;
        }

        [JsonProperty("message")]
        public string Message
        {
            get { return _message; }
        }

        [JsonProperty("kind")]
        public DiagnosticKind Kind
        {
            get { return _kind; }
        }

        [JsonProperty("file")]
        public string File
        {
            get { return _file; }
        }

        [JsonProperty("line")]
        public int Line
        {
            get { return _line; }
        }

        [JsonProperty("column")]
        public int Column
        {
            get { return _column; }
        }

        [JsonProperty("code")]
        public string Code
        {
            get { return _code; }
        }
    }
}
