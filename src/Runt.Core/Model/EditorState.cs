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

        public EditorState WithDialog(JObject dialog, JObject changes, JObject partials = null)
        {
            Utils.RegisterChange(changes, () => Dialog, dialog, partials);
            return new EditorState(_workspace, dialog);
        }

        public EditorState WithWorkspace(Workspace workspace, JObject changes, JObject partials = null)
        {
            Utils.RegisterChange(changes, () => Workspace, workspace, partials);
            return new EditorState(workspace, _dialog);
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
