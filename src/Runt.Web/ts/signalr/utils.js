/// <reference path='../_ref.d.ts' />
define(["require", "exports", './connection'], function(require, exports, conn) {
    function abstract() {
        throw new Error('this function is abstract');
    }
    exports.abstract = abstract;

    function formatString(format, args) {
        for (var i = 0, l = args.length; i < l; i++) {
            format = format.replace('{' + i + '}', args[i]);
        }
        return format;
    }
    exports.formatString = formatString;

    var Disposable = (function () {
        function Disposable(fn) {
            this._disposed = false;
            this._fn = fn;
        }
        Disposable.prototype.dispose = function () {
            if (this._disposed)
                return;

            this._disposed = true;
            this._fn();
        };

        Disposable.all = function (disposables) {
            return new Disposable(function () {
                disposables.forEach(function (d) {
                    return d.dispose();
                });
            });
        };
        return Disposable;
    })();
    exports.Disposable = Disposable;

    var LiteEvent = (function () {
        function LiteEvent() {
            this._handlers = [];
        }
        LiteEvent.prototype.on = function (handler) {
            this._handlers.push(handler);
            return {
                dispose: function () {
                    this.handlers = this.handlers.filter(function (h) {
                        return h !== handler;
                    });
                }
            };
        };

        LiteEvent.prototype.trigger = function (data) {
            this._handlers.forEach(function (h) {
                return h(data);
            });
        };
        return LiteEvent;
    })();
    exports.LiteEvent = LiteEvent;

    var Defer = (function () {
        function Defer() {
            var _this = this;
            this._promise = new Promise(function (resolve, reject) {
                _this._resolve = resolve;
                _this._reject = reject;
            });
        }
        Defer.prototype.resolve = function (value) {
            this._resolve(value);
        };

        Defer.prototype.reject = function (err) {
            this._reject(err);
        };

        Object.defineProperty(Defer.prototype, "promise", {
            get: function () {
                return this._promise;
            },
            enumerable: true,
            configurable: true
        });

        Defer.delay = function (length) {
            return new Promise(function (resolve) {
                return setTimeout(resolve, length);
            });
        };
        return Defer;
    })();
    exports.Defer = Defer;

    var HeartbeatMonitor = (function () {
        function HeartbeatMonitor(connection, beatInterval) {
            this._timer = 0;
            this._connection = null;
            this._beatInterval = 0;
            this._monitorKeepAlive = false;
            this._hasBeenWarned = false;
            this._timedOut = false;
            this._connection = connection;
            this._beatInterval = beatInterval;
        }
        HeartbeatMonitor.prototype.start = function () {
            var _this = this;
            this._monitorKeepAlive = this._connection.keepAliveData !== null && this._connection.transport.supportsKeepAlive;

            this._hasBeenWarned = false;
            this._timedOut = false;
            this._timer = setInterval(function (_) {
                return _this.timedBeat();
            }, this._beatInterval);
        };

        HeartbeatMonitor.prototype.timedBeat = function () {
            var timeElapsed = Date.now() - this._connection.lastMessageAt;
            this.beat(timeElapsed);
        };

        HeartbeatMonitor.prototype.beat = function (ellapsedTime) {
            if (this._monitorKeepAlive)
                this.checkKeepAlive(ellapsedTime);

            this._connection.markActive();
        };

        HeartbeatMonitor.prototype.checkKeepAlive = function (ellapsedTime) {
            if (this._connection.state === 1 /* connected */) {
                if (ellapsedTime >= this._connection.keepAliveData.timeout) {
                    if (!this._timedOut) {
                        // Connection has been lost
                        this._connection.trace(2 /* events */, 'Connection Timed-out : Transport Lost Connection');
                        this._timedOut = true;
                        this._connection.transport.lostConnection(this._connection);
                    }
                } else if (ellapsedTime >= this._connection.keepAliveData.timeoutWarning) {
                    if (!this._hasBeenWarned) {
                        // Inform user and set HasBeenWarned to true
                        this._connection.trace(2 /* events */, 'Connection Timeout Warning : Notifying user');
                        this._hasBeenWarned = true;
                        this._connection.onConnectionSlow();
                    }
                } else {
                    this._hasBeenWarned = false;
                    this._timedOut = false;
                }
            }
        };

        HeartbeatMonitor.prototype.dispose = function () {
            clearInterval(this._timer);
        };
        return HeartbeatMonitor;
    })();
    exports.HeartbeatMonitor = HeartbeatMonitor;

    var TaskQueue = (function () {
        function TaskQueue(initialTask, maxSize) {
            if (typeof initialTask === "undefined") { initialTask = Promise.resolve(null); }
            if (typeof maxSize === "undefined") { maxSize = null; }
            this._lastQueuedTask = null;
            this._drained = false;
            this._maxSize = null;
            this._size = 0;
            this._lastQueuedTask = initialTask;
            this._maxSize = maxSize;
        }
        Object.defineProperty(TaskQueue.prototype, "isDrained", {
            get: function () {
                return this._drained;
            },
            enumerable: true,
            configurable: true
        });

        TaskQueue.prototype.enqueueSync = function (fun, state) {
            if (typeof state === "undefined") { state = null; }
            return this.enqueue(function (s) {
                return new Promise(function (resolve, reject) {
                    fun(s);
                    resolve(null);
                });
            }, state);
        };

        TaskQueue.prototype.enqueue = function (taskFun, state) {
            var _this = this;
            if (typeof state === "undefined") { state = null; }
            if (this._drained)
                return this._lastQueuedTask;

            if (this._maxSize !== null) {
                if (this._size === this._maxSize)
                    return null;
                this._size++;
            }

            var newTask = this._lastQueuedTask.then(function () {
                return taskFun(state).then(function (r) {
                    _this._size--;
                    return r;
                }, function (e) {
                    _this._size--;
                    throw e;
                });
            });

            this._lastQueuedTask = newTask;
            return newTask;
        };

        TaskQueue.prototype.drain = function () {
            this._drained = true;
            return this._lastQueuedTask;
        };
        return TaskQueue;
    })();
    exports.TaskQueue = TaskQueue;
});
//# sourceMappingURL=utils.js.map
