/// <reference path='./_ref.d.ts' /> 

import conn = require('./signalr/connection');

var connection = new conn.Connection(window.location.protocol + '//' + window.location.host + '/io');
connection.start();