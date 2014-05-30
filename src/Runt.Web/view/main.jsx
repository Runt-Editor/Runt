/** @jsx React.DOM */

define(['require', 'exports', 'react'], function(require, exports, React) {
  var Dialogs = {
    browse: React.createClass({
      render: function() {
        return (
          <div>
            <span>{'Path: ' + this.props.path}</span>
            <ul>
              {this.props.content.map(function(entry) {
                return <li>{entry.name}</li>;
              })}
            </ul>
          </div>
        );
      }
    })
  };

  var Main = React.createClass({
    render: function() {
      if(this.state.dialog) {
        var d = Dialogs[this.state.dialog.name];
        if(d) {
          return d(this.state.dialog);
        }
      }

      return <div>Hello world!</div>;
    },

    getInitialState: function() { return {}; }
  });
  exports.Main = Main;
});