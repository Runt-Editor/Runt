using System.Collections.Immutable;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Runt.Core.Model
{
    public class EditorState
    {
        public static EditorState Null = new EditorState(null, null, null);

        readonly Workspace _workspace;
        readonly JObject _dialog;
        readonly ImmutableList<Tab> _tabs;

        public EditorState(Workspace workspace, JObject dialog, ImmutableList<Tab> tabs)
        {
            if (tabs == null)
                tabs = ImmutableList.Create<Tab>();
            _workspace = workspace;
            _dialog = dialog;
            _tabs = tabs;
        }

        public EditorState WithDialog(JObject dialog, JObject changes, JObject partials = null)
        {
            Utils.RegisterChange(changes, () => Dialog, dialog, partials);
            return new EditorState(_workspace, dialog, _tabs);
        }

        public EditorState WithWorkspace(Workspace workspace, JObject changes, JObject partials = null)
        {
            Utils.RegisterChange(changes, () => Workspace, workspace, partials);
            return new EditorState(workspace, _dialog, _tabs);
        }

        public EditorState WithTabs(ImmutableList<Tab> tabs, JObject changes, JObject partials = null)
        {
            Utils.RegisterChange(changes, () => Tabs, tabs, partials);
            return new EditorState(_workspace, _dialog, tabs);
        }

        public EditorState Reset(JObject changes)
        {
            Utils.RegisterChange(changes, () => Dialog, null, null);
            Utils.RegisterChange(changes, () => Workspace, null, null);
            Utils.RegisterChange(changes, () => Tabs, ImmutableList.Create<Tab>(), null);
            return Null;
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

        [JsonProperty("tabs")]
        public ImmutableList<Tab> Tabs
        {
            get { return _tabs; }
        }
    }
}
