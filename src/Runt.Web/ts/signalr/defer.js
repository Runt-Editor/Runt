/// <reference path='../_ref.d.ts' />
define(["require", "exports"], function(require, exports) {
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
        return Defer;
    })();
    exports.Defer = Defer;
});
//# sourceMappingURL=defer.js.map
