/** @jsx React.DOM */

define(['require', 'exports', 'react', 'app'], function(require, exports, React, app) {
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
      return this.transferPropsTo(
        <header>
          <div className="layoutBlock topRowBanner">
            <div style={{position: 'absolute', left: 0, top: 0, width: '39px', height: '36px', zIndex: 1, borderRight: '1px solid #ddd'}}></div>
            <a className="layoutLeft" href="#"></a>
            <nav className="bannerLeftArea" style={{zIndex: 2}}>
              <button className="centralNavigation commandSprite core-sprite-hamburger" type="button"></button>
            </nav>

            <div className="clear navigationBreadcrumb bannerMiddleArea" style={{textAlign: 'center'}}>
              <div className="currentLocation">
                <span>Koff</span>
              </div>

              <div className="bannerRightArea" style={{zIndex: 2}}>
                <div className="spacingLeft layoutLeft"></div>
              </div>
            </div>
          </div>
        </header>
      );
    }
  });

  var SideMenu = React.createClass({
    render: function() {
      return this.transferPropsTo(
        <div className="sideMenu" style={{width: '40px'}}>
          <ul className="sideMenuList">
            <li className="sideMenuItem sideMenuItemActive">
              <button type="button" className="core-sprite-edit submenu-trigger"></button>
              <ul className="sideMenuSubMenu">
                <li className="sideMenuSubMenuItem">
                  <a className="sideMenuSubMenuItemLink" href="#">
                    <span className="sideMenuSubMenuItemSpan">Show workspace</span>
                  </a>
                </li>
              </ul>
            </li>
          </ul>
        </div>
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
      var className = 'dropdownTrigger orionButton commandButton';
      var ulClassName = 'dropdownMenu';
      if(this.props.open) {
        className += ' dropdownTriggerOpen dropdownSelection';
        ulClassName += ' dropdownMenuOpen';
      }

      return (
        <ul className="commandList layoutLeft pageActions">
          <li>
            {this.transferPropsTo(<button className={className}>{this.props.name}</button>)}
            <ul className={ulClassName}>
              {this.props.children}
            </ul>
          </li>
        </ul>
      );
    }
  });

  var SubMenuItem = React.createClass({
    render: function() {
      return this.props.children ? this.renderWithChildren() : this.renderSimple();
    },

    renderWithChildren: function() {
      return (
        <li className="dropdownSubMenu">
          {this.transferPropsTo(
            <span className="dropdownTrigger dropdownMenuItem">
              <span className="dropdownCommandName">{this.props.name}</span>
              <span className="dropdownArrowRight core-sprite-closedarrow" />
            </span>
          )}
          <ul className="dropdownMenu">
            {this.props.children}
          </ul>
        </li>
      );
    },

    renderSimple: function() {
      return (
        <li>
          {this.transferPropsTo(
            <span className="dropdownMenuItem">
              <span className="dropdownCommandName">{this.props.name}</span>
              <span className="dropdownKeyBinding">{this.props.binding}</span>
            </span>
          )}
        </li>
      );
    }
  });

  var MenuSeparator = React.createClass({
    render: function() {
      return (
        <li className="dropdownSeparator">
          <span className="dropdownSeparator" />
        </li>
      );
    }
  });

  var Menu = React.createClass({
    render: function() {
      return (
        <div className="mainToolbar toolComposite toolbarLayout">
          {this.props.children}
        </div>
      )
    }
  });

  var PageContent = React.createClass({
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

      return (
        <div className="content-fixedHeight" style={{left: '40px'}}>
          <div className="sideMenuToggle"></div>
          <Menu>
            <MenuItem name="File" open={this.props.menu.open === 'file'} onClick={menu('file')}>
              <SubMenuItem name="Open Project" binding="Ctrl+O" onClick={navigate(null)} />
              <MenuSeparator />
              <SubMenuItem name="New">
                <SubMenuItem name="File" binding="Ctrl+N" />
              </SubMenuItem>
            </MenuItem>
          </Menu>
        </div>
      );
    }
  });

  var Main = React.createClass({
    render: function() {
      var extra = null;

      if(this.state.dialog) {
        var dialog = Dialogs[this.state.dialog.name];
        if(dialog) {
          extra = (
            Dialog(this.state.dialog, dialog(this.state.dialog))
          );
        }
      }

      return this.transferPropsTo(
        <div style={{width: '100%', height: '100%', margin: 0, padding: 0}}>
          <Header />
          <SideMenu />
          <PageContent menu={this.state.menu} />
          {extra}
        </div>
      );
    },

    getInitialState: function() { 
      return {
        menu: {
          open: null
        }
      };
    }
  });
  exports.Main = Main;
});