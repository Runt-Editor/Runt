/// <reference path='./_ref.d.ts' /> 

import conn = require('./signalr/connection');
import React = require('react');
import view = require('./view/main');

export var $updating = false;
var callbacks = {};

var connection = new conn.Connection(window.location.protocol + '//' + window.location.host + '/io');
connection.start();
connection.received.on(message => {
    try {
        var msg = JSON.parse(message);
        handle(msg);
    } catch (e) {
        console.warn(e);
    }
});

var state = {
    _menu: {
        open: null
    },
    _cache: {
        content: {},
        annotate: {}
    }
};

function handle(msg: any): void {
    switch (msg.type) {
        case 'state':
            updateState(msg.data);
            break;

        case 'callback':
            var id = msg.id;
            var cb = callbacks[id];
            delete callbacks[id];
            cb(msg.data);
            break;

        case 'highlight':
            highlightCode(msg.data);
            break;
    }
}

function isNumeric(name: string): boolean {
    return /^\d+$/.test(name);
}

function mergeArray(arr: any[], diff: any): any[] {
    if (Array.isArray(diff))
        return diff;

    // copy
    var ret = arr.slice(0);

    Object.getOwnPropertyNames(diff).forEach(n => {
        var index = parseInt(n, 10);
        ret[index] = merge(arr[index], diff[n]);
    });

    return ret;
}

function merge(obj: any, diff: any): any {
    if (['string', 'number', 'boolean', 'undefined', 'function'].indexOf(typeof diff) !== -1)
        return diff;

    if (diff === null)
        return diff;

    if (obj === null || obj === undefined) {
        // Generally just return the diff, but diff might be an array diff
        if (typeof diff === 'object' && Object.getOwnPropertyNames(diff).every(n => isNumeric(n))) {
            return mergeArray([], diff);
        } else if (Array.isArray(diff)) {
            return mergeArray([], diff);
        } else {
            obj = {};
        }
    }

    if (Array.isArray(obj)) {
        return mergeArray(obj, diff);
    }

    var ret = {};
    // Shallow copy
    Object.getOwnPropertyNames(obj).forEach(n => ret[n] = obj[n]);

    Object.getOwnPropertyNames(diff).forEach(n => {
        if (isNumeric(n))
            throw new Error('Can\'t handle numeric keys');

        var value = diff[n];
        var oldVal = ret[n];
        ret[n] = merge(oldVal, value);
    });

    return ret;
}

var component = React.renderComponent(view.Main({
    onClick: function (evt) {
        if (component.state._menu.open !== null) {
            updateState({
                _menu: {
                    open: null
                }
            });
        }
    }
}), document.body);

function updateState(diff: any): void {
    state = merge(state, diff);

    component.setState(state);
}

function updateContent(content: any): void {
    var cid = content.cid;
    var cb = contentCallbacks[cid];
    delete contentCallbacks[cid];
    cb(content);
}

var _highlight;
function highlightCode(content: any): void {
    var cid = content.cid;
    var displayData = content.data;
    if (state.tabs) {
        var tabs = state.tabs;
        for (var i = 0, l = tabs.length; i < l; i++) {
            var tab = tabs[i];
            if (tab.active) {
                if (cid == tab.cid)
                    _highlight(displayData);
                return;
            }
        }
    }
}

export function invoke(name, ...args): void {
    connection.send(JSON.stringify({
        name: name,
        args: args
    }));
}

export function fnInvoke(...args): (evt: any) => void {
    var stop = false;

    if (args[0] === true) {
        stop = true;
        args = args.slice(1);
    }

    return (evt: any) => {
        if (stop) {
            evt.stopPropagation();
        }

        invoke.apply(null, args);
    };
}

export function cancelDialog(): void {
    invoke('dialog::cancel');
}

export function browseProject(path: string): void {
    invoke('dialog:browse-project::open', path);
}

export function toggleMenu(name: string): void {
    name = component.state._menu.open === name ? null : name;
    component.setState({
        _menu: {
            open: name
        }
    });
}

export function updateCode(update: any): void {
    if (state.tabs) {
        var tabs = state.tabs;
        for (var i = 0, l = tabs.length; i < l; i++) {
            var tab = tabs[i];
            if (tab.active) {
                var cid = tab.cid;
                invoke('code::update', cid, update);
                return;
            }
        }
    }
}

export function routeHighlight(fn) {
    _highlight = fn;
}

var gid = 0;
export function getContent(oldCid: string, oldText: string, newCid: string, callback: (c: any) => void): void {
    var id = gid++;
    callbacks[id] = callback;
    invoke('content::swap', id, oldCid, oldText, newCid);
}

export function getInfo(symbolId: string, callback: (c: any) => void): void {
    var id = gid++;
    callbacks[id] = callback;
    invoke('symbol::get-info', id, symbolId);
}

export function popup(info: any): void {
    component.setState({
        _popup: info
    });
}