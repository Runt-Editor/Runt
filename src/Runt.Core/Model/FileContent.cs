using System;
using System.IO;

namespace Runt.Core.Model
{
    public class FileContent : Content
    {
        readonly FileInfo _file;
        readonly string _content;

        public static FileContent Create(string contentId, string relativePath, string path, bool read)
        {
            string text = null;
            if (read)
                text = File.ReadAllText(path);

            return new FileContent(contentId, relativePath, false, new FileInfo(path), text);
        }

        public FileContent(string contentId, string relativePath, bool dirty, FileInfo file, string text)
            : base(contentId, relativePath, dirty)
        {
            _file = file;
            _content = text;
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

        public override Content WithText(string text)
        {
            return new FileContent(ContentId, RelativePath, true, _file, text);
        }
    }
}
