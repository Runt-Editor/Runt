using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Runt.Core.Model
{
    public class EditorState
    {
        public static EditorState Null = new EditorState();

        readonly Workspace _workspace;
        readonly JObject _dialog;

        public EditorState(Workspace workspace = null, JObject dialog = null)
        {
            _workspace = workspace;
            _dialog = dialog;
        }

        public EditorState WithDialog(JObject dialog)
        {
            return new EditorState(_workspace, dialog);
        }

        [JsonProperty("workspace")]
        public Workspace Workspace
        {
            get { return _workspace; }
        }

        [JsonProperty("dialog")]
        public JObject Dialog
        {
            get { return _dialog; }
        }
    }
}
