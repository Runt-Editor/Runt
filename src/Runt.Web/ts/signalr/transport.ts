/// <reference path='../_ref.d.ts' /> 

import conn = require('./connection');
import http = require('./http');
import utils = require('./utils');

export interface NegotiationResult {
    connectionId: string;
    connectionToken: string;
    url: string;
    protocolVersion: string;
    disconnectTimeout: number;
    tryWebSockets: boolean;
    keepAliveTimeout: number;
    transportConnectTimeout: number;
}

export interface IClientTransport extends utils.IDisposable {
    name: string;
    supportsKeepAlive: boolean;

    negotiate(connection: conn.IConnection, connectionData: string): Promise<NegotiationResult>;
    start(connection: conn.IConnection, connectionData: string): Promise<void>;
    send(connection: conn.IConnection, data: string, connectionData: string): Promise<void>;
    abort(connection: conn.IConnection, timeout: number, connectionData: string): void;

    lostConnection(connection: conn.IConnection): void;
}

module TransportHelper {
    export function getNegotiateResponse(client: http.IClient, connection: conn.IConnection,
        connectionData: string): Promise<NegotiationResult> {
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

        return client.get(negotiateUrl, connection.prepareRequest.bind(connection), false)
            .then(response => {
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

    export function appendCustomQueryString(connection: conn.IConnection, baseUrl: string): string {
        if (baseUrl === null)
            baseUrl = '';

        var appender = '',
            customQuery = connection.queryString,
            qs = '';

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

    export function getReceiveQueryString(connection: conn.IConnection, connectionData: string, transport: string): string {
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

    export function processResponse(connection: conn.IConnection, response: string,
        shouldReconnect: (val: boolean) => void,
        disconnected: (val: boolean) => void,
        onInitialized: () => void): void {
        connection.markLastMessage();

        shouldReconnect(false);
        disconnected(false);

        if (response === null || response === '')
            return;

        try {
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

            var messages: {}[] = result['M'];
            if (messages !== null) {
                connection.messageId = result['C'];

                messages.forEach(m => connection.onReceived(m));

                tryInitialize(result, onInitialized);
            }
        } catch (e) {
            connection.onError(e);
        }
    }

    export function verifyLastActive(connection: conn.IConnection): boolean {
        // Ensure that we have not exceeded the reconnect window
        if (Date.now() - connection.lastActiveAt >= connection.reconnectWindow) {
            connection.trace(conn.TraceLevels.events,
                'There has not been an active server connection for an extended period of time. Stopping connection.');
            connection.stop();
            return false;
        }

        return true;
    }

    function updateGroups(connection: conn.IConnection, groupsToken: string): void {
        if (groupsToken !== null)
            connection.groupsToken = groupsToken;
    }

    function tryInitialize(response: any, onInitialized: () => void): void {
        if (response['S'] === 1) {
            onInitialized();
        }
    }

    var uid = Date.now();
    function nocache() {
        return ++uid;
    }
}

export class AutoTransport implements IClientTransport {
    // Transport that's in use
    private _transport: IClientTransport = null;
    private _client: http.IClient = null;
    private _startIndex: number = 0;

    // List of transports in fallback order
    private _transports: IClientTransport[];

    constructor(client: http.IClient, transports: IClientTransport[]= [new LongPollingTransport(client)]) {
        this._client = client;
        this._transports = transports;
    }

    /**
     * Indicates whether or not the active transport supports keep alive
     */
    public get supportsKeepAlive() {
        return this._transport !== null && this._transport.supportsKeepAlive;
    }

    public get name() {
        return this._transport === null ? null : this._transport.name;
    }

    public negotiate(connection: conn.IConnection, connectionData: string): Promise<NegotiationResult> {
        return this.getNegotiateResponse(connection, connectionData)
            .then(response => {
                if (!response.tryWebSockets) {
                    this._transports = this._transports.filter(i => i.name !== 'webSockets');
                }
                return response;
            });
    }

    public getNegotiateResponse(connection: conn.IConnection, connectionData: string): Promise<NegotiationResult> {
        return TransportHelper.getNegotiateResponse(this._client, connection, connectionData);
    }

    public start(connection: conn.IConnection, connectionData: string): Promise<void> {
        var tcs = new utils.Defer<void>();

        // Resolve the transport
        this.resolveTransport(connection, connectionData, tcs, this._startIndex);

        return tcs.promise;
    }

    private resolveTransport(connection: conn.IConnection, data: string, tcs: utils.Defer<void>, index: number): void {
        // Pick the current transport
        var transport = this._transports[index];

        transport.start(connection, data).then(_ => {
            // Set the active transport
            this._transport = transport;

            // Complete the process
            tcs.resolve(null);
        }, e => {
                connection.trace(conn.TraceLevels.events, 'Auto: Failed to connect to using transport {0}. {1}', transport.name, e);

                // If that transport fails to initialize then fallback
                var next = index + 1;
                if (next < this._transports.length) {
                    // Try the next transport
                    this.resolveTransport(connection, data, tcs, next);
                } else {
                    // If there's nothing else to try then just fail
                    tcs.reject(e);
                }
            });
    }

    public send(connection: conn.IConnection, data: string, connectionData: string): Promise<void> {
        return this._transport.send(connection, data, connectionData);
    }

    public abort(connection: conn.IConnection, timeout: number, connectionData: string): void {
        if (this._transport != null)
            this._transport.abort(connection, timeout, connectionData);
    }

    public lostConnection(connection: conn.IConnection): void {
        this._transport.lostConnection(connection);
    }

    public dispose() {
        this._transport.dispose();
    }
}

// The send query string
class HttpBasedTransport implements IClientTransport {
    // The send query string
    private static _sendQueryString: string = '?transport={0}&connectionData={1}&connectionToken={2}{3}';

    // The transport name
    private _transport: string;

    private _client: http.IClient;

    constructor(client: http.IClient, transport: string) {
        this._client = client;
        this._transport = transport;
    }

    public get name(): string {
        return this._transport;
    }

    public get supportsKeepAlive(): boolean {
        return utils.abstract();
    }

    public get client(): http.IClient {
        return this._client;
    }

    public negotiate(connection: conn.IConnection, connectionData: string): Promise<NegotiationResult> {
        return TransportHelper.getNegotiateResponse(this._client, connection, connectionData);
    }

    public start(connection: conn.IConnection, connectionData: string): Promise<void> {
        var initializeHandler = new TransportInitializationHandler(connection.totalTransportConnectTimeout);

        this.onStart(connection, connectionData, initializeHandler);

        return initializeHandler.task;
    }

    public onStart(connection: conn.IConnection, connectionData: string, initializationHandler: TransportInitializationHandler): void {
        return utils.abstract();
    }

    public send(connection: conn.IConnection, data: string, connectionData: string): Promise<void> {
        var url = connection.url + 'send';
        var customQueryString = connection.queryString ? '&' + connection.queryString : '';

        url += utils.formatString(HttpBasedTransport._sendQueryString,
            [this._transport, connectionData, encodeURIComponent(connection.connectionToken),
                customQueryString]);

        var postData: { [s: string]: string } = { data: data };

        return <any>this._client.post(url, connection.prepareRequest.bind(connection), postData, false)
            .then(response => {
                if (response.body !== '' && response.body !== null) {
                    connection.trace(conn.TraceLevels.messages, 'onMessage({0})', response.body);
                    connection.onReceived(JSON.parse(response.body));
                }
            }).catch(connection.onError.bind(connection));
    }

    public abort(connection: conn.IConnection, timeout: number, connectionData: string): void {
        throw new Error('Not implemented');
    }

    public lostConnection(connection: conn.IConnection): void {
        return utils.abstract();
    }

    public getReceiveQueryString(connection: conn.IConnection, data: string): string {
        return TransportHelper.getReceiveQueryString(connection, data, this._transport);
    }

    public dispose(): void { ; /* do nothing */ }
}

class TransportInitializationHandler {
    private _tcs: utils.Defer<void> = null;

    private _failure: utils.LiteEvent<void> = new utils.LiteEvent<void>();

    public get failure(): utils.ILiteEvent<void> {
        return this._failure;
    }

    constructor(timeout: number) {
        this._tcs = new utils.Defer<void>();

        setTimeout(() => {
            this.fail(new Error('Timed out'));
        }, timeout);
    }

    public get task(): Promise<void> {
        return this._tcs.promise;
    }

    public success(): void {
        this._tcs.resolve(null);
    }

    public fail(ex: any = new Error('Error_TransportFailedToConnect')) {
        this._tcs.reject(ex);
    }
}

class LongPollingTransport extends HttpBasedTransport {
    private _reconnectDelay: number = 0;
    private _errorDelay: number = 0;

    constructor(client: http.IClient = new http.DefaultClient()) {
        super(client, 'longPolling');

        this._reconnectDelay = 5 * 1000;
        this._errorDelay = 2 * 1000;
    }

    public get supportsKeepAlive() {
        return false;
    }

    public onStart(connection: conn.IConnection, connectionData: string, initializationHandler: TransportInitializationHandler) {
        var requestHandler = new PollingRequestHandler(this.client);
        var negotiateInitializer = new NegotiateInitializer(initializationHandler);

        var complete = negotiateInitializer.complete.bind(negotiateInitializer);
        var onAbort = _ => negotiateInitializer.abort();

        var unregister =
            utils.Disposable.all([
                requestHandler.error.on(complete),
                requestHandler.abort.on(onAbort)]);

        // If the transport fails to initialize we want to silently stop
        initializationHandler.failure.on(_ => requestHandler.stop());

        // Once we've initialized the connection we need to tear down the 
        // initializer functions and assign the appropriate onMessage function
        negotiateInitializer.initialize.on(_ => unregister.dispose.bind(unregister));

        // Add additional actions to each of the PollingRequestHandler events
        this.pollingSetup(connection, connectionData, requestHandler, complete);

        requestHandler.start();
    }

    private pollingSetup(connection: conn.IConnection, data: string,
        requestHandler: PollingRequestHandler, onInitialized: () => void): void {
        requestHandler.resolveUrl = () => {
            var url = connection.url;

            if (connection.messageId === null) {
                url += 'connect';
                connection.trace(conn.TraceLevels.events, 'LP Connect: {0}', url);
            } else if (this.isReconnecting(connection)) {
                url += 'reconnect';
                connection.trace(conn.TraceLevels.events, 'LP Reconnect: {0}', url);
            } else {
                url += 'poll';
                connection.trace(conn.TraceLevels.events, 'LP Poll: {0}', url);
            }

            url += super.getReceiveQueryString(connection, data);
            return url;
        };

        requestHandler.prepareRequest.on(req => connection.prepareRequest(req));
        requestHandler.message.on(message => {
            var shouldReconnect = false;
            var disconnectedReceived = false;

            connection.trace(conn.TraceLevels.messages, 'LP: OnMessage({0})', message);

            TransportHelper.processResponse(connection, message,
                reconnect => shouldReconnect = reconnect,
                disconnected => disconnectedReceived = disconnected,
                onInitialized);

            if (this.isReconnecting(connection)) {
                // If the timeout for the reconnect hasn't fired as yet just fire the 
                // event here before any incoming messages are processed
                this.tryReconnect(connection);
            }

            if (shouldReconnect) {
                // Transition into reconnecting state
                conn.Utils.ensureReconnecting(connection);
            }

            if (disconnectedReceived) {
                connection.trace(conn.TraceLevels.messages, 'Disconnect command received from server.');
                connection.disconnect();
            }
        });

        requestHandler.error.on(ex => {
            if (!TransportHelper.verifyLastActive(connection))
                return;

            // Transition into reconnecting state
            conn.Utils.ensureReconnecting(connection);

            // Sometimes a connection might have been closed by the server before we get to write anything
            // so just try again and raise OnError.
            // TODO: Check if abort is requested, and if it is, stop the connection instead
            connection.onError(ex);
        });

        requestHandler.polling.on(_ => {
            // Capture the cleanup within a closure so it can persist through multiple requests
            this.tryDelayReconnect(connection);
        });

        requestHandler.onAfterPoll = exception => {
            return exception === null ? Promise.resolve<void>(null) : utils.Defer.delay(this._errorDelay);
        };

        requestHandler.abort.on(_ => {
            // TODO: implement aborting
        });
    }

    private tryDelayReconnect(connection: conn.IConnection): void {
        if (this.isReconnecting(connection)) {
            utils.Defer.delay(this._reconnectDelay).then(_ => {
                this.tryReconnect(connection);
            });
        }
    }

    private tryReconnect(connection: conn.IConnection): void {
        // Fire the reconnect event after the delay.
        this.fireReconnected(connection);
    }

    private fireReconnected(connection: conn.IConnection): void {
        // Mark the connection as connected
        if (connection.changeState(conn.ConnectionState.reconnecting, conn.ConnectionState.connected)) {
            connection.onReconnected();
        }
    }

    private isReconnecting(connection: conn.IConnection): boolean {
        return connection.state === conn.ConnectionState.reconnecting;
    }

    public lostConnection(connection: conn.IConnection): void { ; /* do nothing */ }
}

class PollingRequestHandler {
    private _client: http.IClient;
    private _request: http.IRequest;
    private _running: number;

    private _onPrepareRequest: utils.LiteEvent<http.IRequest> = new utils.LiteEvent<http.IRequest>();
    private _onMessage: utils.LiteEvent<string> = new utils.LiteEvent<string>();
    private _onError: utils.LiteEvent<any> = new utils.LiteEvent<any>();
    private _onPolling: utils.LiteEvent<void> = new utils.LiteEvent<void>();
    private _onAbort: utils.LiteEvent<http.IRequest> = new utils.LiteEvent<http.IRequest>();

    public resolveUrl: () => string;
    public onAfterPoll: (e: any) => Promise<void>;

    public get prepareRequest(): utils.ILiteEvent<http.IRequest> {
        return this._onPrepareRequest;
    }

    public get message(): utils.ILiteEvent<string> {
        return this._onMessage;
    }

    public get error(): utils.ILiteEvent<any> {
        return this._onError;
    }

    public get polling(): utils.ILiteEvent<void> {
        return this._onPolling;
    }

    public get abort(): utils.ILiteEvent<http.IRequest> {
        return this._onAbort;
    }

    constructor(client: http.IClient) {
        this._client = client;
        this._running = 0;

        // Set default events
        this.resolveUrl = () => '';
        this.onAfterPoll = _ => Promise.resolve<void>(null);
    }


    public start(): void {
        if (this._running === 0) {
            this._running = 1;
            this.poll();
        }
    }

    private poll(): void {
        // Only poll if we're running
        if (this._running === 0)
            return;

        // A url is required
        var url = this.resolveUrl();

        this._client.post(url, request => {
            this._onPrepareRequest.trigger(request);
            this._request = request;

            // This is called just prior to posting the request to ensure that any in-flight polling request
            // is always executed before an OnAfterPoll
            this._onPolling.trigger(null);
        }, null, true).then(response => {
                try {
                    this._onMessage.trigger(response.body);
                    return null;
                } catch (e) {
                    this._onError.trigger(e);
                    return e;
                }
            }, ex => {
                this._onError.trigger(ex);
                return ex;
            }).then(err => this.onAfterPoll(err)).then(() => this.poll());
    }

    public stop(): void {
        if (this._running === 1) {
            this._running = 0;
            if (this._request !== null)
                this._request.abort();
        }
    }
}

class NegotiateInitializer {
    private _initializeCallback: () => void;
    private _errorCallback: (any) => void;
    private _onInitialize: utils.LiteEvent<void> = new utils.LiteEvent<void>();

    public get initialize(): utils.ILiteEvent<void> {
        return this._onInitialize;
    }

    constructor(initializeHandler: TransportInitializationHandler) {
        this._initializeCallback = initializeHandler.success.bind(initializeHandler);
        this._errorCallback = initializeHandler.fail.bind(initializeHandler);
    }

    public complete(exception: any = null) {
        if (exception === null) {
            this._onInitialize.trigger(null);
            this._initializeCallback();
        } else {
            this._onInitialize.trigger(null);
            this._errorCallback(exception);
        }
    }

    public abort(): void {
        this._onInitialize.trigger(null);
    }
}