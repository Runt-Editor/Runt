/// <reference path='../_ref.d.ts' />
define(["require", "exports"], function(require, exports) {
    

    

    

    var XhrRequest = (function () {
        function XhrRequest(url, method, data, isLongRunning) {
            this._headers = {
                'User-Agent': 'XMLHTTP/1.0',
                'Accept': 'application/json'
            };
            this._url = url;
            this._method = method;
            this._data = data;
            this._isLongRunning = false;
        }
        Object.defineProperty(XhrRequest.prototype, "userAgent", {
            get: function () {
                return this._headers['User-Agent'];
            },
            set: function (value) {
                this._headers['User-Agent'] = value;
            },
            enumerable: true,
            configurable: true
        });

        Object.defineProperty(XhrRequest.prototype, "accept", {
            get: function () {
                return this._headers['Accept'];
            },
            set: function (value) {
                this._headers['Accept'] = value;
            },
            enumerable: true,
            configurable: true
        });

        Object.defineProperty(XhrRequest.prototype, "body", {
            get: function () {
                if (this._xhr.readyState !== 4) {
                    throw new Error('not ready');
                }

                return this._xhr.responseText;
            },
            enumerable: true,
            configurable: true
        });

        XhrRequest.prototype.abort = function () {
            this._xhr.abort();
        };

        XhrRequest.prototype.setRequestHeaders = function (headers) {
            var _this = this;
            Object.getOwnPropertyNames(headers).forEach(function (n) {
                _this._headers[n] = headers[n];
            });
        };

        XhrRequest.prototype.send = function () {
            var _this = this;
            return new Promise(function (resolve, reject) {
                _this._xhr = new XMLHttpRequest();
                _this._xhr.open(_this._method, _this._url, true);
                Object.getOwnPropertyNames(_this._headers).forEach(function (n) {
                    if (n.toLowerCase() === 'user-agent')
                        return;
                    _this._xhr.setRequestHeader(n, _this._headers[n]);
                });
                if (!_this._isLongRunning) {
                    // apparently, this is ignored
                }

                _this._xhr.onreadystatechange = function (e) {
                    if (_this._xhr.readyState === 4) {
                        resolve(_this);
                    }
                };

                _this._xhr.send(_this._data);
            });
        };
        return XhrRequest;
    })();

    var DefaultClient = (function () {
        function DefaultClient() {
        }
        DefaultClient.prototype.initialize = function (connection) {
            this._connection = connection;
        };

        DefaultClient.prototype.get = function (url, prepareRequest, isLongRunning) {
            return this.send(url, 'GET', null, prepareRequest, isLongRunning);
        };

        DefaultClient.prototype.post = function (url, prepareRequest, data, isLongRunning) {
            return this.send(url, 'POST', data, prepareRequest, isLongRunning);
        };

        DefaultClient.prototype.send = function (url, method, data, prepareRequest, isLongRunning) {
            var formData = '';
            if (data !== null) {
                Object.getOwnPropertyNames(data).forEach(function (name) {
                    formData += '&' + encodeURIComponent(name) + '=' + encodeURIComponent(data[name]);
                });
                formData = formData.substring(1);
            }

            var request = new XhrRequest(url, method, formData, isLongRunning);
            prepareRequest(request);
            return request.send();
        };
        return DefaultClient;
    })();
    exports.DefaultClient = DefaultClient;
});
