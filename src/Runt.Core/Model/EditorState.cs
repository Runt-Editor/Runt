using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Runt.Core.Model
{
    public class EditorState
    {
        readonly Workspace _workspace;

        public EditorState(Workspace workspace)
        {
            _workspace = workspace;
        }

        [JsonProperty("workspace")]
        public Workspace Workspace
        {
            get { return _workspace; }
        }
    }
}
