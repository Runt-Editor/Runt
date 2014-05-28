/// <reference path='../_ref.d.ts' />
define(["require", "exports"], function(require, exports) {
    // Determines when we warn the developer that the connection may be lost
    var _keepAliveWarnAt = 2.0 / 3.0;

    /** Class to store all the Keep Alive properties */
    var KeepAliveData = (function () {
        function KeepAliveData(timeout, timeoutWarning, checkInterval) {
            if (typeof timeoutWarning === "undefined") { timeoutWarning = timeout * _keepAliveWarnAt; }
            if (typeof checkInterval === "undefined") { checkInterval = (timeout - timeoutWarning) / 3; }
            this._timeout = timeout;
            this._timeoutWarning = timeoutWarning;
            this._checkInterval = checkInterval;
        }
        Object.defineProperty(KeepAliveData.prototype, "timeout", {
            /** Timeout to designate when to force the connection into reconnecting */
            get: function () {
                return this._timeout;
            },
            enumerable: true,
            configurable: true
        });

        Object.defineProperty(KeepAliveData.prototype, "timeoutWarning", {
            /** Timeout to designate when to warn the developer that the connection may be dead or is hanging. */
            get: function () {
                return this._timeoutWarning;
            },
            enumerable: true,
            configurable: true
        });

        Object.defineProperty(KeepAliveData.prototype, "checkInterval", {
            /**
            * Frequency with which we check the keep alive.  It must be short in order to not miss/pick up any changes
            */
            get: function () {
                return this._checkInterval;
            },
            enumerable: true,
            configurable: true
        });
        return KeepAliveData;
    })();
    exports.KeepAliveData = KeepAliveData;
});
//# sourceMappingURL=keepalivedata.js.map
