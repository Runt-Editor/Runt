using System;
using System.IO;

namespace Runt.Core.Model
{
    public class FileContent : Content
    {
        readonly FileInfo _file;
        readonly string _content;

        public FileContent(string contentId, string path, bool read)
            : base(contentId)
        {
            _file = new FileInfo(path);
            if (read)
                _content = File.ReadAllText(path);
        }

        public override string ContentString
        {
            get
            {
                if (_content == null)
                    throw new InvalidOperationException("Content created with read=false");
                return _content;
            }
        }

        public override string Name
        {
            get { return _file.Name; }
        }

        public override string Tooltip
        {
            get { return _file.FullName; }
        }
    }
}
