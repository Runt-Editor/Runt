/// <reference path='../_ref.d.ts' />
var __extends = this.__extends || function (d, b) {
    for (var p in b) if (b.hasOwnProperty(p)) d[p] = b[p];
    function __() { this.constructor = d; }
    __.prototype = b.prototype;
    d.prototype = new __();
};
define(["require", "exports", './connection', './http', './utils'], function(require, exports, conn, http, utils) {
    var TransportHelper;
    (function (TransportHelper) {
        function getNegotiateResponse(client, connection, connectionData) {
            var negotiateUrl = connection.url + 'negotiate';
            negotiateUrl += appendCustomQueryString(connection, negotiateUrl);

            var appender = '?';
            if (negotiateUrl.indexOf(appender) !== -1) {
                appender = '&';
            }

            negotiateUrl += appender + 'clientProtocol=' + connection.protocol;

            if (connectionData !== null && connectionData !== '')
                negotiateUrl += '&connectionData=' + connectionData;

            client.initialize(connection);

            return client.get(negotiateUrl, connection.prepareRequest.bind(connection), false).then(function (response) {
                var data = JSON.parse(response.body);
                return {
                    "url": data.Url,
                    "connectionToken": data.ConnectionToken,
                    "connectionId": data.ConnectionId,
                    "keepAliveTimeout": data.KeepAliveTimeout * 1000,
                    "disconnectTimeout": data.DisconnectTimeout * 1000,
                    "tryWebSockets": data.TryWebSockets,
                    "protocolVersion": data.ProtocolVersion,
                    "transportConnectTimeout": data.TransportConnectTimeout * 1000,
                    "longPollDelay": data.LongPollDelay * 1000
                };
            });
        }
        TransportHelper.getNegotiateResponse = getNegotiateResponse;

        function appendCustomQueryString(connection, baseUrl) {
            if (baseUrl === null)
                baseUrl = '';

            var appender = '', customQuery = connection.queryString, qs = '';

            if (customQuery !== '' && customQuery !== null) {
                var firstChar = customQuery.substring(0, 1);

                // If the custom query string already starts with an ampersand or question mark
                // then we dont have to use any appender, it can be empty.
                if (firstChar !== '?' && firstChar !== '&') {
                    appender = '?';

                    if (baseUrl.indexOf(appender) !== -1)
                        appender = '&';
                }

                qs = appender + customQuery;
            }

            return qs;
        }
        TransportHelper.appendCustomQueryString = appendCustomQueryString;

        function getReceiveQueryString(connection, connectionData, transport) {
            // ?transport={0}&connectionToken={1}&messageId={2}&groups={3}&connectionData={4}{5}
            var qs = '?transport=' + transport + '&connectionToken=' + encodeURIComponent(connection.connectionToken);

            if (connection.messageId !== null)
                qs += '&messageId=' + encodeURIComponent(connection.messageId);

            if (connection.groupsToken !== null)
                qs += '&groupsToken=' + encodeURIComponent(connection.groupsToken);

            if (connectionData !== null)
                qs += '&connectionData=' + encodeURIComponent(connectionData);

            var customQuery = connection.queryString;
            if (customQuery !== null && customQuery !== '')
                qs += '&' + customQuery;

            qs += '&t' + nocache();
            return qs;
        }
        TransportHelper.getReceiveQueryString = getReceiveQueryString;

        function processResponse(connection, response, shouldReconnect, disconnected, onInitialized) {
            connection.markLastMessage();

            shouldReconnect(false);
            disconnected(false);

            if (response === null || response === '')
                return;

            try  {
                var result = JSON.parse(response);

                if (!result)
                    return;

                if (result['I'] !== null && result['I'] !== undefined) {
                    connection.onReceived(result);
                    return;
                }

                shouldReconnect(result['T'] === 1);
                disconnected(result['D'] === 1);

                if (result['D'] === 1)
                    return;

                updateGroups(connection, result['G']);

                var messages = result['M'];
                if (messages !== null) {
                    connection.messageId = result['C'];

                    messages.forEach(function (m) {
                        return connection.onReceived(m);
                    });

                    tryInitialize(result, onInitialized);
                }
            } catch (e) {
                connection.onError(e);
            }
        }
        TransportHelper.processResponse = processResponse;

        function verifyLastActive(connection) {
            // Ensure that we have not exceeded the reconnect window
            if (Date.now() - connection.lastActiveAt >= connection.reconnectWindow) {
                connection.trace(2 /* events */, 'There has not been an active server connection for an extended period of time. Stopping connection.');
                connection.stop();
                return false;
            }

            return true;
        }
        TransportHelper.verifyLastActive = verifyLastActive;

        function updateGroups(connection, groupsToken) {
            if (groupsToken !== null)
                connection.groupsToken = groupsToken;
        }

        function tryInitialize(response, onInitialized) {
            if (response['S'] === 1) {
                onInitialized();
            }
        }

        var uid = Date.now();
        function nocache() {
            return ++uid;
        }
    })(TransportHelper || (TransportHelper = {}));

    var AutoTransport = (function () {
        function AutoTransport(client, transports) {
            if (typeof transports === "undefined") { transports = [new LongPollingTransport(client)]; }
            // Transport that's in use
            this._transport = null;
            this._client = null;
            this._startIndex = 0;
            this._client = client;
            this._transports = transports;
        }
        Object.defineProperty(AutoTransport.prototype, "supportsKeepAlive", {
            /**
            * Indicates whether or not the active transport supports keep alive
            */
            get: function () {
                return this._transport !== null && this._transport.supportsKeepAlive;
            },
            enumerable: true,
            configurable: true
        });

        Object.defineProperty(AutoTransport.prototype, "name", {
            get: function () {
                return this._transport === null ? null : this._transport.name;
            },
            enumerable: true,
            configurable: true
        });

        AutoTransport.prototype.negotiate = function (connection, connectionData) {
            var _this = this;
            return this.getNegotiateResponse(connection, connectionData).then(function (response) {
                if (!response.tryWebSockets) {
                    _this._transports = _this._transports.filter(function (i) {
                        return i.name !== 'webSockets';
                    });
                }
                return response;
            });
        };

        AutoTransport.prototype.getNegotiateResponse = function (connection, connectionData) {
            return TransportHelper.getNegotiateResponse(this._client, connection, connectionData);
        };

        AutoTransport.prototype.start = function (connection, connectionData) {
            var tcs = new utils.Defer();

            // Resolve the transport
            this.resolveTransport(connection, connectionData, tcs, this._startIndex);

            return tcs.promise;
        };

        AutoTransport.prototype.resolveTransport = function (connection, data, tcs, index) {
            var _this = this;
            // Pick the current transport
            var transport = this._transports[index];

            transport.start(connection, data).then(function (_) {
                // Set the active transport
                _this._transport = transport;

                // Complete the process
                tcs.resolve(null);
            }, function (e) {
                connection.trace(2 /* events */, 'Auto: Failed to connect to using transport {0}. {1}', transport.name, e);

                // If that transport fails to initialize then fallback
                var next = index + 1;
                if (next < _this._transports.length) {
                    // Try the next transport
                    _this.resolveTransport(connection, data, tcs, next);
                } else {
                    // If there's nothing else to try then just fail
                    tcs.reject(e);
                }
            });
        };

        AutoTransport.prototype.send = function (connection, data, connectionData) {
            return this._transport.send(connection, data, connectionData);
        };

        AutoTransport.prototype.abort = function (connection, timeout, connectionData) {
            if (this._transport != null)
                this._transport.abort(connection, timeout, connectionData);
        };

        AutoTransport.prototype.lostConnection = function (connection) {
            this._transport.lostConnection(connection);
        };

        AutoTransport.prototype.dispose = function () {
            this._transport.dispose();
        };
        return AutoTransport;
    })();
    exports.AutoTransport = AutoTransport;

    // The send query string
    var HttpBasedTransport = (function () {
        function HttpBasedTransport(client, transport) {
            this._client = client;
            this._transport = transport;
        }
        Object.defineProperty(HttpBasedTransport.prototype, "name", {
            get: function () {
                return this._transport;
            },
            enumerable: true,
            configurable: true
        });

        Object.defineProperty(HttpBasedTransport.prototype, "supportsKeepAlive", {
            get: function () {
                return utils.abstract();
            },
            enumerable: true,
            configurable: true
        });

        Object.defineProperty(HttpBasedTransport.prototype, "client", {
            get: function () {
                return this._client;
            },
            enumerable: true,
            configurable: true
        });

        HttpBasedTransport.prototype.negotiate = function (connection, connectionData) {
            return TransportHelper.getNegotiateResponse(this._client, connection, connectionData);
        };

        HttpBasedTransport.prototype.start = function (connection, connectionData) {
            var initializeHandler = new TransportInitializationHandler(connection.totalTransportConnectTimeout);

            this.onStart(connection, connectionData, initializeHandler);

            return initializeHandler.task;
        };

        HttpBasedTransport.prototype.onStart = function (connection, connectionData, initializationHandler) {
            return utils.abstract();
        };

        HttpBasedTransport.prototype.send = function (connection, data, connectionData) {
            var url = connection.url + 'send';
            var customQueryString = connection.queryString ? '&' + connection.queryString : '';

            url += utils.formatString(HttpBasedTransport._sendQueryString, [
                this._transport, connectionData, encodeURIComponent(connection.connectionToken),
                customQueryString]);

            var postData = { data: data };

            return this._client.post(url, connection.prepareRequest.bind(connection), postData, false).then(function (response) {
                if (response.body !== '' && response.body !== null) {
                    connection.trace(1 /* messages */, 'onMessage({0})', response.body);
                    connection.onReceived(JSON.parse(response.body));
                }
            }).catch(connection.onError.bind(connection));
        };

        HttpBasedTransport.prototype.abort = function (connection, timeout, connectionData) {
            throw new Error('Not implemented');
        };

        HttpBasedTransport.prototype.lostConnection = function (connection) {
            return utils.abstract();
        };

        HttpBasedTransport.prototype.getReceiveQueryString = function (connection, data) {
            return TransportHelper.getReceiveQueryString(connection, data, this._transport);
        };

        HttpBasedTransport.prototype.dispose = function () {
            ;
        };
        HttpBasedTransport._sendQueryString = '?transport={0}&connectionData={1}&connectionToken={2}{3}';
        return HttpBasedTransport;
    })();

    var TransportInitializationHandler = (function () {
        function TransportInitializationHandler(timeout) {
            var _this = this;
            this._tcs = null;
            this._failure = new utils.LiteEvent();
            this._tcs = new utils.Defer();

            setTimeout(function () {
                _this.fail(new Error('Timed out'));
            }, timeout);
        }
        Object.defineProperty(TransportInitializationHandler.prototype, "failure", {
            get: function () {
                return this._failure;
            },
            enumerable: true,
            configurable: true
        });

        Object.defineProperty(TransportInitializationHandler.prototype, "task", {
            get: function () {
                return this._tcs.promise;
            },
            enumerable: true,
            configurable: true
        });

        TransportInitializationHandler.prototype.success = function () {
            this._tcs.resolve(null);
        };

        TransportInitializationHandler.prototype.fail = function (ex) {
            if (typeof ex === "undefined") { ex = new Error('Error_TransportFailedToConnect'); }
            this._tcs.reject(ex);
        };
        return TransportInitializationHandler;
    })();

    var LongPollingTransport = (function (_super) {
        __extends(LongPollingTransport, _super);
        function LongPollingTransport(client) {
            if (typeof client === "undefined") { client = new http.DefaultClient(); }
            _super.call(this, client, 'longPolling');
            this._reconnectDelay = 0;
            this._errorDelay = 0;

            this._reconnectDelay = 5 * 1000;
            this._errorDelay = 2 * 1000;
        }
        Object.defineProperty(LongPollingTransport.prototype, "supportsKeepAlive", {
            get: function () {
                return false;
            },
            enumerable: true,
            configurable: true
        });

        LongPollingTransport.prototype.onStart = function (connection, connectionData, initializationHandler) {
            var requestHandler = new PollingRequestHandler(this.client);
            var negotiateInitializer = new NegotiateInitializer(initializationHandler);

            var complete = negotiateInitializer.complete.bind(negotiateInitializer);
            var onAbort = function (_) {
                return negotiateInitializer.abort();
            };

            var unregister = utils.Disposable.all([
                requestHandler.error.on(complete),
                requestHandler.abort.on(onAbort)]);

            // If the transport fails to initialize we want to silently stop
            initializationHandler.failure.on(function (_) {
                return requestHandler.stop();
            });

            // Once we've initialized the connection we need to tear down the
            // initializer functions and assign the appropriate onMessage function
            negotiateInitializer.initialize.on(function (_) {
                return unregister.dispose.bind(unregister);
            });

            // Add additional actions to each of the PollingRequestHandler events
            this.pollingSetup(connection, connectionData, requestHandler, complete);

            requestHandler.start();
        };

        LongPollingTransport.prototype.pollingSetup = function (connection, data, requestHandler, onInitialized) {
            var _this = this;
            requestHandler.resolveUrl = function () {
                var url = connection.url;

                if (connection.messageId === null) {
                    url += 'connect';
                    connection.trace(2 /* events */, 'LP Connect: {0}', url);
                } else if (_this.isReconnecting(connection)) {
                    url += 'reconnect';
                    connection.trace(2 /* events */, 'LP Reconnect: {0}', url);
                } else {
                    url += 'poll';
                    connection.trace(2 /* events */, 'LP Poll: {0}', url);
                }

                url += _super.prototype.getReceiveQueryString.call(_this, connection, data);
                return url;
            };

            requestHandler.prepareRequest.on(function (req) {
                return connection.prepareRequest(req);
            });
            requestHandler.message.on(function (message) {
                var shouldReconnect = false;
                var disconnectedReceived = false;

                connection.trace(1 /* messages */, 'LP: OnMessage({0})', message);

                TransportHelper.processResponse(connection, message, function (reconnect) {
                    return shouldReconnect = reconnect;
                }, function (disconnected) {
                    return disconnectedReceived = disconnected;
                }, onInitialized);

                if (_this.isReconnecting(connection)) {
                    // If the timeout for the reconnect hasn't fired as yet just fire the
                    // event here before any incoming messages are processed
                    _this.tryReconnect(connection);
                }

                if (shouldReconnect) {
                    // Transition into reconnecting state
                    conn.Utils.ensureReconnecting(connection);
                }

                if (disconnectedReceived) {
                    connection.trace(1 /* messages */, 'Disconnect command received from server.');
                    connection.disconnect();
                }
            });

            requestHandler.error.on(function (ex) {
                if (!TransportHelper.verifyLastActive(connection))
                    return;

                // Transition into reconnecting state
                conn.Utils.ensureReconnecting(connection);

                // Sometimes a connection might have been closed by the server before we get to write anything
                // so just try again and raise OnError.
                // TODO: Check if abort is requested, and if it is, stop the connection instead
                connection.onError(ex);
            });

            requestHandler.polling.on(function (_) {
                // Capture the cleanup within a closure so it can persist through multiple requests
                _this.tryDelayReconnect(connection);
            });

            requestHandler.onAfterPoll = function (exception) {
                return exception === null ? Promise.resolve(null) : utils.Defer.delay(_this._errorDelay);
            };

            requestHandler.abort.on(function (_) {
                // TODO: implement aborting
            });
        };

        LongPollingTransport.prototype.tryDelayReconnect = function (connection) {
            var _this = this;
            if (this.isReconnecting(connection)) {
                utils.Defer.delay(this._reconnectDelay).then(function (_) {
                    _this.tryReconnect(connection);
                });
            }
        };

        LongPollingTransport.prototype.tryReconnect = function (connection) {
            // Fire the reconnect event after the delay.
            this.fireReconnected(connection);
        };

        LongPollingTransport.prototype.fireReconnected = function (connection) {
            // Mark the connection as connected
            if (connection.changeState(2 /* reconnecting */, 1 /* connected */)) {
                connection.onReconnected();
            }
        };

        LongPollingTransport.prototype.isReconnecting = function (connection) {
            return connection.state === 2 /* reconnecting */;
        };

        LongPollingTransport.prototype.lostConnection = function (connection) {
            ;
        };
        return LongPollingTransport;
    })(HttpBasedTransport);

    var PollingRequestHandler = (function () {
        function PollingRequestHandler(client) {
            this._onPrepareRequest = new utils.LiteEvent();
            this._onMessage = new utils.LiteEvent();
            this._onError = new utils.LiteEvent();
            this._onPolling = new utils.LiteEvent();
            this._onAbort = new utils.LiteEvent();
            this._client = client;
            this._running = 0;

            // Set default events
            this.resolveUrl = function () {
                return '';
            };
            this.onAfterPoll = function (_) {
                return Promise.resolve(null);
            };
        }
        Object.defineProperty(PollingRequestHandler.prototype, "prepareRequest", {
            get: function () {
                return this._onPrepareRequest;
            },
            enumerable: true,
            configurable: true
        });

        Object.defineProperty(PollingRequestHandler.prototype, "message", {
            get: function () {
                return this._onMessage;
            },
            enumerable: true,
            configurable: true
        });

        Object.defineProperty(PollingRequestHandler.prototype, "error", {
            get: function () {
                return this._onError;
            },
            enumerable: true,
            configurable: true
        });

        Object.defineProperty(PollingRequestHandler.prototype, "polling", {
            get: function () {
                return this._onPolling;
            },
            enumerable: true,
            configurable: true
        });

        Object.defineProperty(PollingRequestHandler.prototype, "abort", {
            get: function () {
                return this._onAbort;
            },
            enumerable: true,
            configurable: true
        });

        PollingRequestHandler.prototype.start = function () {
            if (this._running === 0) {
                this._running = 1;
                this.poll();
            }
        };

        PollingRequestHandler.prototype.poll = function () {
            var _this = this;
            // Only poll if we're running
            if (this._running === 0)
                return;

            // A url is required
            var url = this.resolveUrl();

            this._client.post(url, function (request) {
                _this._onPrepareRequest.trigger(request);
                _this._request = request;

                // This is called just prior to posting the request to ensure that any in-flight polling request
                // is always executed before an OnAfterPoll
                _this._onPolling.trigger(null);
            }, null, true).then(function (response) {
                try  {
                    _this._onMessage.trigger(response.body);
                    return null;
                } catch (e) {
                    _this._onError.trigger(e);
                    return e;
                }
            }, function (ex) {
                _this._onError.trigger(ex);
                return ex;
            }).then(function (err) {
                return _this.onAfterPoll(err);
            }).then(function () {
                return _this.poll();
            });
        };

        PollingRequestHandler.prototype.stop = function () {
            if (this._running === 1) {
                this._running = 0;
                if (this._request !== null)
                    this._request.abort();
            }
        };
        return PollingRequestHandler;
    })();

    var NegotiateInitializer = (function () {
        function NegotiateInitializer(initializeHandler) {
            this._onInitialize = new utils.LiteEvent();
            this._initializeCallback = initializeHandler.success.bind(initializeHandler);
            this._errorCallback = initializeHandler.fail.bind(initializeHandler);
        }
        Object.defineProperty(NegotiateInitializer.prototype, "initialize", {
            get: function () {
                return this._onInitialize;
            },
            enumerable: true,
            configurable: true
        });

        NegotiateInitializer.prototype.complete = function (exception) {
            if (typeof exception === "undefined") { exception = null; }
            if (exception === null) {
                this._onInitialize.trigger(null);
                this._initializeCallback();
            } else {
                this._onInitialize.trigger(null);
                this._errorCallback(exception);
            }
        };

        NegotiateInitializer.prototype.abort = function () {
            this._onInitialize.trigger(null);
        };
        return NegotiateInitializer;
    })();
});
//# sourceMappingURL=transport.js.map
