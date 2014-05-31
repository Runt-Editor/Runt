/// <reference path='./_ref.d.ts' /> 

import conn = require('./signalr/connection');
import React = require('react');
import view = require('./view/main');

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

var state = {};

function handle(msg: any): void {
    switch (msg.type) {
        case 'state':
            updateState(msg.data);
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
        if (component.state.menu.open !== null) {
            component.setState({
                menu: {
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

function invoke(name, ...args): void {
    connection.send(JSON.stringify({
        name: name,
        args: args
    }));
}

export function fnInvoke(name, ...args): (evt: any) => void {
    return (evt: any) => {
        invoke.apply(null, [name].concat(args));
    };
}

export function cancelDialog(): void {
    invoke('dialog::cancel');
}

export function browseProject(path: string): void {
    invoke('dialog:browse-project::open', path);
}

export function toggleMenu(name: string): void {
    name = component.state.menu.open === name ? null : name;
    component.setState({
        menu: {
            open: name
        }
    });
}