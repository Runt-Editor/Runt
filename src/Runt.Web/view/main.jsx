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
            <button className="dismissButton layoutRight core-sprite-close imageSprite"></button>
          </div>
          <div className="dialogContent layoutBlock" style={{width: '500px', overflow: 'auto', maxHeight: '400px'}}>
            {React.Children.only(this.props.children)}
          </div>
        </div>
      );
    }
  })

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

      return (
        <div style={{width: '100%', height: '100%', margin: 0, padding: 0}}>
          <Header />
          <SideMenu />
          {extra}
        </div>
      );
    },

    getInitialState: function() { return {}; }
  });
  exports.Main = Main;
});