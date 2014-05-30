/// <reference path='./_ref.d.ts' /> 

import conn = require('./signalr/connection');
import React = require('react');
import view = require('./view/main');

var connection = new conn.Connection(window.location.protocol + '//' + window.location.host + '/io');
connection.start().then(() => {
    window.setTimeout(() => {
        invoke('browse-project');
    }, 2000);
});
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

function merge(obj: any, diff: any) {
    if (obj === null || obj === undefined || typeof obj !== 'object' || Array.isArray(obj))
        obj = {};

    Object.getOwnPropertyNames(diff).forEach(name => {
        var val = diff[name];
        if (Array.isArray(val))
            obj[name] = val;
        else if (typeof val === 'object' && val !== null)
            obj[name] = merge(obj[name], val);
        else
            obj[name] = val;
    });

    return obj;
}

var component = React.renderComponent(view.Main(), document.body);

function updateState(diff: any): void {
    state = diff;

    component.setState(diff);
}

function invoke(name, ...args): void {
    connection.send(JSON.stringify({
        name: name,
        args: args
    }));
}