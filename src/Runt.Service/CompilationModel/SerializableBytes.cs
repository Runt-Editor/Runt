using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Runt.Service.CompilationModel
{
    class SerializableBytes
    {
        internal static Stream CreateReadableStream(byte[] xmlDocCommentBytes, CancellationToken none)
        {
            return new MemoryStream(xmlDocCommentBytes);
        }
    }
}
