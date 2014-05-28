/// <reference path='../_ref.d.ts' />
define(["require", "exports", './keepalivedata', './transport', './http', './utils'], function(require, exports, ka, t, http, utils) {
    function setDisposableTimeout(fn, time) {
        var timeout = setTimeout(fn, time);
        return function () {
            return clearTimeout(timeout);
        };
    }

    (function (ConnectionState) {
        ConnectionState[ConnectionState["connecting"] = 0] = "connecting";
        ConnectionState[ConnectionState["connected"] = 1] = "connected";
        ConnectionState[ConnectionState["reconnecting"] = 2] = "reconnecting";
        ConnectionState[ConnectionState["disconnected"] = 3] = "disconnected";
    })(exports.ConnectionState || (exports.ConnectionState = {}));
    var ConnectionState = exports.ConnectionState;

    var StateChange = (function () {
        function StateChange(oldState, newState) {
            this._oldState = oldState;
            this._newState = newState;
        }
        Object.defineProperty(StateChange.prototype, "oldState", {
            get: function () {
                return this._oldState;
            },
            enumerable: true,
            configurable: true
        });

        Object.defineProperty(StateChange.prototype, "newState", {
            get: function () {
                return this._newState;
            },
            enumerable: true,
            configurable: true
        });
        return StateChange;
    })();
    exports.StateChange = StateChange;

    (function (TraceLevels) {
        TraceLevels[TraceLevels["none"] = 0] = "none";
        TraceLevels[TraceLevels["messages"] = 1] = "messages";
        TraceLevels[TraceLevels["events"] = 2] = "events";
        TraceLevels[TraceLevels["statchanges"] = 4] = "statchanges";
        TraceLevels[TraceLevels["all"] = TraceLevels.messages | TraceLevels.events | TraceLevels.statchanges] = "all";
    })(exports.TraceLevels || (exports.TraceLevels = {}));
    var TraceLevels = exports.TraceLevels;

    var defaultAbortTimeout = 30 * 1000;

    var Connection = (function () {
        function Connection(url, queryString) {
            if (typeof queryString === "undefined") { queryString = null; }
            this._transport = null;
            this._disconnectTimeout = 0;
            this._totalTransportConnectTimeout = 0;
            this._disconnectTimeoutOperation = null;
            this._state = 3 /* disconnected */;
            this._keepAliveData = null;
            this._reconnectWindow = 0;
            this._connectTask = null;
            this._connectionData = null;
            this._receiveQueue = null;
            this._lastQueuedReceiveTask = null;
            this._startTcs = null;
            this._lastMessageAt = 0;
            this._lastActiveAt = 0;
            this._monitor = null;
            this._items = null;
            this._traceLevel = 0 /* none */;
            this._headers = null;
            this._transportConnectTimeout = 0;
            this._protocol = null;
            this._messageId = null;
            this._connectionId = null;
            this._connectionToken = null;
            this._groupsToken = null;
            this._received = new utils.LiteEvent();
            this._error = new utils.LiteEvent();
            this._closed = new utils.LiteEvent();
            this._reconnecting = new utils.LiteEvent();
            this._reconnected = new utils.LiteEvent();
            this._stateChanged = new utils.LiteEvent();
            this._connectionSlow = new utils.LiteEvent();
            if (!url)
                throw new Error('url can\'t be empty');

            if (url.indexOf('?') !== -1)
                throw new Error('url can\'t contain querystring');

            if (url.substr(url.length - 1, 1) != '/')
                url = url + '/';

            this._url = url;
            this._queryString = queryString;
            this._lastMessageAt = Date.now();
            this._lastActiveAt = Date.now();
            this._reconnectWindow = 0;
            this._items = {};
            this._state = 3 /* disconnected */;
            this._traceLevel = TraceLevels.all;
            this._headers = {};
            this._transportConnectTimeout = 0;
            this._totalTransportConnectTimeout = 0;

            // current client protocol
            this._protocol = "1.3";
        }
        Object.defineProperty(Connection.prototype, "received", {
            get: function () {
                return this._received;
            },
            enumerable: true,
            configurable: true
        });

        Object.defineProperty(Connection.prototype, "error", {
            get: function () {
                return this._error;
            },
            enumerable: true,
            configurable: true
        });

        Object.defineProperty(Connection.prototype, "closed", {
            get: function () {
                return this._closed;
            },
            enumerable: true,
            configurable: true
        });

        Object.defineProperty(Connection.prototype, "reconnecting", {
            get: function () {
                return this._reconnecting;
            },
            enumerable: true,
            configurable: true
        });

        Object.defineProperty(Connection.prototype, "reconnected", {
            get: function () {
                return this._reconnected;
            },
            enumerable: true,
            configurable: true
        });

        Object.defineProperty(Connection.prototype, "stateChanged", {
            get: function () {
                return this._stateChanged;
            },
            enumerable: true,
            configurable: true
        });

        Object.defineProperty(Connection.prototype, "connectionSlow", {
            get: function () {
                return this._connectionSlow;
            },
            enumerable: true,
            configurable: true
        });

        Object.defineProperty(Connection.prototype, "transportConnectTimeout", {
            /**
            * The amount of time a transport will wait (while connecting) before failing.
            * This value is modified by adding the server's TransportConnectTimeout configuration value.
            */
            get: function () {
                return this._transportConnectTimeout;
            },
            set: function (value) {
                this._transportConnectTimeout = value;
            },
            enumerable: true,
            configurable: true
        });

        Object.defineProperty(Connection.prototype, "totalTransportConnectTimeout", {
            /**
            * The amount of time a transport will wait (while connecting) before failing.
            * This is the total vaue obtained by adding the server's configuration value and the timeout specified by the user
            */
            get: function () {
                return this._totalTransportConnectTimeout;
            },
            enumerable: true,
            configurable: true
        });

        Object.defineProperty(Connection.prototype, "protocol", {
            get: function () {
                return this._protocol;
            },
            set: function (value) {
                this._protocol = value;
            },
            enumerable: true,
            configurable: true
        });

        Object.defineProperty(Connection.prototype, "reconnectWindow", {
            /**
            * The maximum amount of time a connection will allow to try and reconnect.
            * This value is equivalent to the summation of the servers disconnect and keep alive timeout values.
            */
            get: function () {
                return this._reconnectWindow;
            },
            set: function (value) {
                this._reconnectWindow = value;
            },
            enumerable: true,
            configurable: true
        });

        Object.defineProperty(Connection.prototype, "keepAliveData", {
            /**
            * Object to store the various keep alive timeout values
            */
            get: function () {
                return this._keepAliveData;
            },
            set: function (value) {
                this._keepAliveData = value;
            },
            enumerable: true,
            configurable: true
        });

        Object.defineProperty(Connection.prototype, "lastMessageAt", {
            /**
            * The timestamp of the last message received by the connection.
            */
            get: function () {
                return this._lastMessageAt;
            },
            enumerable: true,
            configurable: true
        });

        Object.defineProperty(Connection.prototype, "lastActiveAt", {
            get: function () {
                return this._lastActiveAt;
            },
            enumerable: true,
            configurable: true
        });

        Object.defineProperty(Connection.prototype, "traceLevel", {
            get: function () {
                return this._traceLevel;
            },
            set: function (value) {
                this._traceLevel = value;
            },
            enumerable: true,
            configurable: true
        });

        Object.defineProperty(Connection.prototype, "headers", {
            /**
            * Gets and sets headers for the requests
            */
            get: function () {
                return this._headers;
            },
            enumerable: true,
            configurable: true
        });

        Object.defineProperty(Connection.prototype, "url", {
            /**
            * Gets the url for the connection.
            */
            get: function () {
                return this._url;
            },
            enumerable: true,
            configurable: true
        });

        Object.defineProperty(Connection.prototype, "messageId", {
            /**
            * Gets or sets the last message id for the connection.
            */
            get: function () {
                return this._messageId;
            },
            set: function (value) {
                this._messageId = value;
            },
            enumerable: true,
            configurable: true
        });

        Object.defineProperty(Connection.prototype, "connectionId", {
            /**
            * Gets or sets the connection id for the connection.
            */
            get: function () {
                return this._connectionId;
            },
            set: function (value) {
                this._connectionId = value;
            },
            enumerable: true,
            configurable: true
        });

        Object.defineProperty(Connection.prototype, "connectionToken", {
            /**
            * Gets or sets the connection token for the connection.
            */
            get: function () {
                return this._connectionToken;
            },
            set: function (value) {
                this._connectionToken = value;
            },
            enumerable: true,
            configurable: true
        });

        Object.defineProperty(Connection.prototype, "groupsToken", {
            /**
            * Gets or sets the groups token for the connection.
            */
            get: function () {
                return this._groupsToken;
            },
            set: function (value) {
                this._groupsToken = value;
            },
            enumerable: true,
            configurable: true
        });

        Object.defineProperty(Connection.prototype, "items", {
            /**
            * Gets a dictionary for storing state for a the connection.
            */
            get: function () {
                return this._items;
            },
            enumerable: true,
            configurable: true
        });

        Object.defineProperty(Connection.prototype, "queryString", {
            /**
            * Gets the querystring specified in the ctor.
            */
            get: function () {
                return this._queryString;
            },
            enumerable: true,
            configurable: true
        });

        Object.defineProperty(Connection.prototype, "transport", {
            get: function () {
                return this._transport;
            },
            enumerable: true,
            configurable: true
        });

        Object.defineProperty(Connection.prototype, "state", {
            /**
            * Gets the current ConnectionState of the connection.
            */
            get: function () {
                return this._state;
            },
            set: function (value) {
                if (this._state != value) {
                    var stateChange = new StateChange(this._state, value);
                    this._state = value;
                    this._stateChanged.trigger(stateChange);
                }
            },
            enumerable: true,
            configurable: true
        });

        /*
        * Starts the connection
        */
        Connection.prototype.start = function () {
            return this.startWithClient(new http.DefaultClient());
        };

        /*
        * Starts the connection
        */
        Connection.prototype.startWithClient = function (client) {
            return this.startWithTransport(new t.AutoTransport(client));
        };

        /*
        * Starts the connection
        */
        Connection.prototype.startWithTransport = function (transport) {
            if (!this.changeState(3 /* disconnected */, 0 /* connecting */))
                return this._connectTask || Promise.resolve(null);

            this._startTcs = new utils.Defer();
            this._receiveQueue = new utils.TaskQueue(this._startTcs.promise);
            this._lastQueuedReceiveTask = Promise.resolve(null);

            this._transport = transport;

            return this._connectTask = this.negotiate(transport);
        };

        Connection.prototype.onSending = function () {
            return null;
        };

        Connection.prototype.negotiate = function (transport) {
            var _this = this;
            this._connectionData = this.onSending();

            return transport.negotiate(this, this._connectionData).then(function (negotiationResponse) {
                _this.verifyProtocolVersion(negotiationResponse.protocolVersion);

                _this._connectionId = negotiationResponse.connectionId;
                _this._connectionToken = negotiationResponse.connectionToken;
                _this._disconnectTimeout = negotiationResponse.disconnectTimeout;
                _this._totalTransportConnectTimeout = _this.transportConnectTimeout + negotiationResponse.transportConnectTimeout;

                // default the beat interval to be 5 seconds in case keep alive is disabled.
                var beatInterval = 5 * 1000;

                // If we have a keep alive
                if (negotiationResponse.keepAliveTimeout != null) {
                    _this._keepAliveData = new ka.KeepAliveData(negotiationResponse.keepAliveTimeout);
                    _this._reconnectWindow = _this._disconnectTimeout + _this._keepAliveData.timeout;

                    beatInterval = _this._keepAliveData.checkInterval;
                } else {
                    _this._reconnectWindow = _this._disconnectTimeout;
                }

                _this._monitor = new utils.HeartbeatMonitor(_this, beatInterval);

                return _this.startTransport();
            }).catch(function () {
                return _this.disconnect();
            });
        };

        Connection.prototype.startTransport = function () {
            var _this = this;
            return this._transport.start(this, this._connectionData).then(function () {
                // NOTE: We have tests that rely on this state change occuring *BEFORE* the start task is complete
                _this.changeState(0 /* connecting */, 1 /* connected */);

                // now that we're connected complete the start task that the
                // receive queue is waiting on
                _this._startTcs.resolve(null);

                // start the monitor to check for server activity
                _this._lastMessageAt = Date.now();
                _this._lastActiveAt = Date.now();
                _this._monitor.start();
            }).then(function () {
                return _this._lastQueuedReceiveTask;
            });
        };

        Connection.prototype.changeState = function (oldState, newState) {
            // If we're in the expected old state then change state and return true
            if (this._state === oldState) {
                this.trace(4 /* statchanges */, 'ChangeState({0}, {1})', ConnectionState[oldState], ConnectionState[newState]);

                this.state = newState;
                return true;
            }

            return false;
        };

        Connection.prototype.verifyProtocolVersion = function (versionString) {
            if (versionString !== this._protocol) {
                throw new Error('Invalid protocol version');
            }
        };

        Connection.prototype.stop = function (timeout) {
            if (typeof timeout === "undefined") { timeout = defaultAbortTimeout; }
            throw new Error('not implemented');
        };

        Connection.prototype.disconnect = function () {
            throw new Error('not implemented');
        };

        Connection.prototype.onClosed = function () {
            this._closed.trigger(null);
        };

        Connection.prototype.send = function (data) {
            if (this.state === 3 /* disconnected */)
                throw new Error('can\'t send when disconnected');
            if (this.state === 0 /* connecting */)
                throw new Error('can\'t send while connecting');

            return this._transport.send(this, data, this._connectionData);
        };

        Connection.prototype.trace = function (level, format) {
            var args = [];
            for (var _i = 0; _i < (arguments.length - 2); _i++) {
                args[_i] = arguments[_i + 2];
            }
            if ((level & this._traceLevel) === level) {
                console.log(utils.formatString(format, args));
            }
        };

        Connection.prototype.onReceived = function (data) {
            var _this = this;
            this._lastQueuedReceiveTask = this._receiveQueue.enqueueSync(function () {
                try  {
                    _this.onMessageReceived(data);
                } catch (e) {
                    _this.onError(e);
                }
            });
        };

        Connection.prototype.onMessageReceived = function (data) {
            try  {
                this._received.trigger(JSON.stringify(data));
            } catch (e) {
                this.onError(e);
            }
        };

        Connection.prototype.onError = function (error) {
            this.trace(2 /* events */, "onError({0})", error);

            this._error.trigger(error);
        };

        Connection.prototype.onReconnecting = function () {
            this._disconnectTimeoutOperation = setDisposableTimeout(this.disconnect.bind(this), this._disconnectTimeout);

            this._reconnecting.trigger(null);
        };

        Connection.prototype.onReconnected = function () {
            this._disconnectTimeoutOperation();

            this._reconnected.trigger(null);
            this.markLastMessage();
        };

        Connection.prototype.onConnectionSlow = function () {
            this.trace(2 /* events */, "onConnectionSlow");

            this._connectionSlow.trigger(null);
        };

        Connection.prototype.markLastMessage = function () {
            this._lastMessageAt = Date.now();
        };

        Connection.prototype.markActive = function () {
            this._lastActiveAt = Date.now();
        };

        Connection.prototype.prepareRequest = function (request) {
            request.userAgent = 'SignalR.Client.TS';
            request.setRequestHeaders(this.headers);
        };
        return Connection;
    })();
    exports.Connection = Connection;

    (function (Utils) {
        function ensureReconnecting(connection) {
            if (connection.changeState(1 /* connected */, 2 /* reconnecting */)) {
                connection.onReconnecting();
            }

            return connection.state === 2 /* reconnecting */;
        }
        Utils.ensureReconnecting = ensureReconnecting;
    })(exports.Utils || (exports.Utils = {}));
    var Utils = exports.Utils;
});
//# sourceMappingURL=connection.js.map
