﻿/// <reference path='./_ref.d.ts' />
define(["require", "exports", './signalr/connection'], function(require, exports, conn) {
    var connection = new conn.Connection(window.location.protocol + '//' + window.location.host + '/signalr');
    connection.start();
});
//# sourceMappingURL=app.js.map