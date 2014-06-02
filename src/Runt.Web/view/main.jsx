/** @jsx React.DOM */

define(['require', 'exports', 'react', '../app', '../editor', 'orion/editor/edit'], function(require, exports, React, app, editor, edit) {
  var Dialogs = {
    browse: React.createClass({
      render: function() {
        function navigate(path) {
          return function() {
            console.log('going to %s', path);
            app.browseProject(path);
            return false;
          };
        }

        var children = [];
        this.props.folders.forEach(function(folder) {
          children.push(
            <tr className="navRow treeTableRow" key={folder.path}>
              <td className="navColumn" style={{paddingLeft: '16px'}}>
                <span className="mainNavColumn">
                  <span className="modelDecorationSprite core-sprite-folder"></span>
                  <a className="navlinkonpage commonNavFolder" href="#" style={{outline: 'none'}} onClick={navigate(folder.path)}>{folder.name}</a>
                </span>
              </td>
            </tr>
          );
        });

        this.props.files.forEach(function(file) {
          children.push(
            <tr className="navRow treeTableRow" key={file.path}>
              <td className="navColumn" style={{paddingLeft: '16px'}}>
                <span className="mainNavColumn">
                  <span className="modelDecorationSprite core-sprite-file"></span>
                  <a className="navlinkonpage commonNavFolder" href="#" style={{outline: 'none'}} onClick={function(){return false;}}>{file.name}</a>
                </span>
              </td>
            </tr>
          );
        });

        return (
          <div>
            <div>
              <span>Path: </span>
              <span>{this.props.path}</span>
            </div>
            <table className="miniNavTreeTable">
              <tbody className="sidebarinnerTreetbody">
                {children}
              </tbody>
            </table>
            <button type="button" onClick={app.fnInvoke('dialog:browse-project::select', this.props.path)}>Select</button>
          </div>
        );
      }
    })
  };

  var Header = React.createClass({
    render: function() {
      function navigate(path) {
        return function(evt) {
          app.browseProject(path);
          evt.preventDefault();
        };
      }

      function menu(name) {
        return function(evt) {
          app.toggleMenu(name);
          evt.stopPropagation();
        };
      }
      
      return this.transferPropsTo(
        <header>
          <nav className="menu-toggle">
            <button className="command-sprite core-sprite-hamburger" type="button"></button>
          </nav>
          <Menu>
            <MenuItem name="File" open={this.props.menu.open === 'file'} onClick={menu('file')}>
              <SubMenuItem name="Open Project" binding="Ctrl+O" onClick={navigate(null)} />
              <MenuSeparator />
              <SubMenuItem name="New">
                <SubMenuItem name="File" binding="Ctrl+N" />
              </SubMenuItem>
            </MenuItem>
          </Menu>
        </header>
      );
    }
  });

  var SideMenu = React.createClass({
    render: function() {
      return this.transferPropsTo(
        <nav className="side-menu fixed-pane">
          <ul>
            <li className="active">
              <button type="button" className="core-sprite-edit"></button>
            </li>
          </ul>
        </nav>
      );
    }
  });

  var Dialog = React.createClass({
    render: function() {
      return (
        <div className="dialog dialogShowing" style={{top: 0, left: '672.5px'}}>
          <div className="dialogTitle">
            <span className="dialogTitleText layoutLeft">{this.props.title}</span>
            <button className="dismissButton layoutRight core-sprite-close imageSprite" onClick={this.cancel}></button>
          </div>
          <div className="dialogContent layoutBlock" style={{width: '500px', overflow: 'auto', maxHeight: '400px'}}>
            {React.Children.only(this.props.children)}
          </div>
        </div>
      );
    },

    cancel: function() {
      app.cancelDialog();
    }
  });

  var MenuItem = React.createClass({
    render: function() {
      var className = '';
      var ulClassName = 'dropdown-menu ';
      if(this.props.open) {
        className = 'open';
        ulClassName += 'open';
      }

      return (
        <li>
          {this.transferPropsTo(<button className={className}>{this.props.name}</button>)}
          <ul className={ulClassName}>
            {this.props.children}
          </ul>
        </li>
      );
    }
  });

  var SubMenuItem = React.createClass({
    render: function() {
      return this.props.children ? this.renderWithChildren() : this.renderSimple();
    },

    renderWithChildren: function() {
      return (
        <li className="sub-menu">
          {this.transferPropsTo(
            <div className="item">
              <span className="name">{this.props.name}</span>
              <span className="arrow core-sprite-closedarrow" />
            </div>
          )}
          <ul className="dropdown-menu">
            {this.props.children}
          </ul>
        </li>
      );
    },

    renderSimple: function() {
      return (
        <li>
          {this.transferPropsTo(
            <div className="item">
              <span className="name">{this.props.name}</span>
              <span className="binding">{this.props.binding}</span>
            </div>
          )}
        </li>
      );
    }
  });

  var MenuSeparator = React.createClass({
    render: function() {
      return (
        <li className="separator">
          <span />
        </li>
      );
    }
  });

  var Menu = React.createClass({
    render: function() {
      return (
        <ul className="menu">
          {this.props.children}
        </ul>
      )
    }
  });

  var FileTree = React.createClass({
    render: function() {
      var _this = this;
      var items = [];
      function getIcon(type) {
        switch(type) {
          case 'file': return 'file';
          case 'project': return 'shell';
          case 'reference': return 'outline';

          default:
            return 'folder';
        }
      }

      function walk(node, indent) {
        if(indent > 0) {
          items.push({
            cid: node.cid,
            name: node.name,
            type: node.type,
            key: node.key,
            hasChildren: node['has-children'],
            indent: indent - 1,
            open: node.open,
            icon: getIcon(node.type, indent)
          });
        }

        node.children.forEach(function(child) {
          walk(child, indent + 1);
        });
      }

      function open(item) {
        return function(evt) {
          evt.preventDefault();
          if(item.cid !== null) {
            app.invoke('tab::open', item.cid);
          }
        };
      }

      if(this.props.content) {
        walk(this.props.content, 0);
      }

      return (
        <table className="file-tree">
          <tbody>
            {items.map(function(item) {
              var arrowClass = 'modelDecorationSprite ' + (item.open ? 'core-sprite-openarrow' : 'core-sprite-closedarrow');

              return (
                <tr key={item.key}>
                  <td style={{paddingLeft: (item.indent * 16) + 'px'}} onDoubleClick={open(item)}>
                    <span className={arrowClass} style={{visibility: item.hasChildren ? 'visible' : 'hidden'}} onClick={app.fnInvoke('tree:node::toggle', item.key)} />
                    <span className={'core-sprite-' + item.icon} />
                    <a href="#">{item.name}</a>
                  </td>
                </tr>
              )
            })}
          </tbody>
        </table>
      );
    }
  });

  var Sidebar = React.createClass({
    render: function() {
      var content = [];
      if(this.props.workspace && this.props.workspace.content) {
        content = [
          <div className="header">
            {this.props.workspace.name}
          </div>,
          <div className="scroll-view">
            <FileTree content={this.props.workspace.content} />
          </div>
        ];
      }

      return (
        <div className={'side-bar ' + this.props.className} style={{width: this.props.width + 'px'}}>
          {content}
        </div>
      );
    }
  });

  var Editor = React.createClass({
    getInitialState: function() {
      return {editor: null};
    },

    componentDidMount: function() {
      var node = this.getDOMNode();
      var e = editor.create(node, {
        contentType: 'text/csharp'
      });
      // var e = edit({parent: node});
      this.setState({
        editor: e
      });
    },

    componentWillUnmount: function() {
      this.state.editor.destroy();
      this.setState({
        editor: null
      });
    },

    componentWillReceiveProps: function(nextProps) {
      if(nextProps.content && nextProps.content !== this.props.content) {
        if(!this.props.content) {
          this.state.editor.install();
        }
        // TODO: Ensure saved
        var editor = this.state.editor;
        var content = nextProps.content;
        editor.setInput(content.name, null, content.content, true /* saved */);
        editor.setText(content.content);
      } else if(!nextProps.content && this.props.content) {
        this.state.editor.uninstall();
      }
    },

    shouldComponentUpdate: function() {
      // updates are handled by orion
      return false;
    },

    render: function() {
      return (
        <div className="content-area editor" />
      );
    }
  });

  var TabBar = React.createClass({
    render: function() {
      return (
        <div className="tab-bar">
          {(this.props.tabs || []).map(function(t) {
            var className = t.active ? 'tab active' : 'tab';
            return (
              <div className={className} onClick={app.fnInvoke('tab::select', t.cid)}>
                {t.name + (t.dirty ? '*' : '')}
                <span className="core-sprite-close imageSprite close" onClick={app.fnInvoke(true, 'tab::close', t.cid)} />
              </div>
            );
          })}
        </div>
        );
    }
  });

  var PageContent = React.createClass({
    getInitialState: function() {
      return {
        open: true,
        sidebarWidth: 308
      };
    },

    render: function() {
      return (
        <div className="pane page-content">
          <TabBar tabs={this.props.tabs} />
          <Editor content={this.props.content} />
        </div>
      );
    }
  });

  var PaneDragger = React.createClass({
    render: function() {
      return (
        <div className="fixed-pane pane-dragger">
          <div />
        </div>
      );
    }
  });

  var LeftPane = React.createClass({
    render: function() {
      return (
        <Sidebar workspace={this.props.workspace} className="fixed-pane" width={this.props.width} />
      );
    }
  });

  var Content = React.createClass({
    getInitialState: function() {
        return {
          open: true,
          sidebarWidth: 308
        };
      },

      render: function() {
        return (
          <div className="content">
            <SideMenu />
            <LeftPane workspace={this.props.workspace} open={this.state.open} width={this.state.sidebarWidth} />
            <PaneDragger open={this.state.open} width={this.state.sidebarWidth} />
            <PageContent workspace={this.props.workspace} tabs={this.props.tabs} content={this.props.content} />
          </div>
        );
      }
  });

  var Main = React.createClass({
    render: function() {
      var extra = null;
      var content = null;
      if(this.state.tabs) {
        var tabs = this.state.tabs;
        for(var i = 0, l = tabs.length; i < l; i++) {
          if(tabs[i].active)
          {
            content = this.state._cache.content[tabs[i].cid];
            break;
          }
        }
      }

      if(this.state.dialog) {
        var dialog = Dialogs[this.state.dialog.name];
        if(dialog) {
          extra = (
            Dialog(this.state.dialog, dialog(this.state.dialog))
          );
        }
      }

      return this.transferPropsTo(
        <div className="root">
          <Header menu={this.state._menu} />
          <Content workspace={this.state.workspace} tabs={this.state.tabs} content={content} />
          {extra}
        </div>
      );
    },

    getInitialState: function() { 
      return {
        _menu: {
          open: null
        },

        _cache: {
          content: {}
        }
      };
    }
  });
  exports.Main = Main;
});