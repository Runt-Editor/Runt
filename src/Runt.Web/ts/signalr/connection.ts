/// <reference path='../_ref.d.ts' /> 

import ka = require('./keepalivedata')
import t = require('./transport');
import http = require('./http');
import utils = require('./utils');

function setDisposableTimeout(fn: Function, time: number): () => void {
    var timeout = setTimeout(fn, time);
    return () => clearTimeout(timeout);
}

export enum ConnectionState {
    connecting,
    connected,
    reconnecting,
    disconnected
}

export class StateChange {
    private _oldState: ConnectionState;
    private _newState: ConnectionState;

    constructor(oldState: ConnectionState, newState: ConnectionState) {
        this._oldState = oldState;
        this._newState = newState;
    }

    public get oldState(): ConnectionState {
        return this._oldState;
    }

    public get newState(): ConnectionState {
        return this._newState;
    }
}

export enum TraceLevels {
    none = 0,
    messages = 1,
    events = 2,
    statchanges = 4,
    all = messages | events | statchanges
}

export interface IConnection {
    protocol: string;
    transportConnectTimeout: number;
    totalTransportConnectTimeout: number;
    reconnectWindow: number;
    keepAliveData: ka.KeepAliveData;
    messageId: string;
    groupsToken: string;
    items: { [s: string]: any };
    connectionId: string;
    connectionToken: string;
    url: string;
    queryString: string;
    state: ConnectionState;
    transport: t.IClientTransport;
    lastMessageAt: number;
    lastActiveAt: number;

    changeState(oldState: ConnectionState, newState: ConnectionState): boolean;
    headers: { [s: string]: string };

    stop(): void;
    disconnect(): void;
    send(data: string): Promise<void>;

    onReceived(data: {}): void;
    onError(err: Error): void;
    onReconnecting(): void;
    onReconnected(): void;
    onConnectionSlow(): void;
    prepareRequest(request: http.IRequest): void;
    markLastMessage(): void;
    markActive(): void;
    trace(level: TraceLevels, format: string, ...args: string[]): void;
}

var defaultAbortTimeout = 30 * 1000; // 30 sec

export class Connection implements IConnection {
    private _url: string;
    private _queryString: string;
    private _transport: t.IClientTransport = null;
    private _disconnectTimeout: number = 0;
    private _totalTransportConnectTimeout: number = 0;
    private _disconnectTimeoutOperation: () => void = null;
    private _state: ConnectionState = ConnectionState.disconnected;
    private _keepAliveData: ka.KeepAliveData = null;
    private _reconnectWindow: number = 0;
    private _connectTask: Promise<void> = null;
    private _connectionData: string = null;
    private _receiveQueue: utils.TaskQueue = null;
    private _lastQueuedReceiveTask: Promise<void> = null;
    private _startTcs: utils.Defer<void> = null;
    private _lastMessageAt: number = 0;
    private _lastActiveAt: number = 0;
    private _monitor: utils.HeartbeatMonitor = null;
    private _items: { [s: string]: any } = null;
    private _traceLevel: TraceLevels = TraceLevels.none;
    private _headers: { [s: string]: string } = null;
    private _transportConnectTimeout: number = 0;
    private _protocol: string = null;
    private _messageId: string = null;
    private _connectionId: string = null;
    private _connectionToken: string = null;
    private _groupsToken: string = null;

    private _received: utils.LiteEvent<string> = new utils.LiteEvent<string>();
    private _error: utils.LiteEvent<any> = new utils.LiteEvent<any>();
    private _closed: utils.LiteEvent<void> = new utils.LiteEvent<void>();
    private _reconnecting: utils.LiteEvent<void> = new utils.LiteEvent<void>();
    private _reconnected: utils.LiteEvent<void> = new utils.LiteEvent<void>();
    private _stateChanged: utils.LiteEvent<StateChange> = new utils.LiteEvent<StateChange>();
    private _connectionSlow: utils.LiteEvent<void> = new utils.LiteEvent<void>();

    public get received(): utils.ILiteEvent<string> {
        return this._received;
    }

    public get error(): utils.ILiteEvent<any> {
        return this._error;
    }

    public get closed(): utils.ILiteEvent<void> {
        return this._closed;
    }

    public get reconnecting(): utils.ILiteEvent<void> {
        return this._reconnecting;
    }

    public get reconnected(): utils.ILiteEvent<void> {
        return this._reconnected;
    }

    public get stateChanged(): utils.ILiteEvent<StateChange> {
        return this._stateChanged;
    }

    public get connectionSlow(): utils.ILiteEvent<void> {
        return this._connectionSlow;
    }

    constructor(url: string, queryString: string = null) {
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
        this._state = ConnectionState.disconnected;
        this._traceLevel = TraceLevels.all;
        this._headers = {};
        this._transportConnectTimeout = 0;
        this._totalTransportConnectTimeout = 0;

        // current client protocol
        this._protocol = "1.3";
    }

    /**
     * The amount of time a transport will wait (while connecting) before failing.
     * This value is modified by adding the server's TransportConnectTimeout configuration value.
     */
    public get transportConnectTimeout(): number {
        return this._transportConnectTimeout;
    }
    public set transportConnectTimeout(value: number) {
        this._transportConnectTimeout = value;
    }

    /**
     * The amount of time a transport will wait (while connecting) before failing.
     * This is the total vaue obtained by adding the server's configuration value and the timeout specified by the user
     */
    public get totalTransportConnectTimeout(): number {
        return this._totalTransportConnectTimeout;
    }

    public get protocol(): string {
        return this._protocol;
    }
    public set protocol(value: string) {
        this._protocol = value;
    }

    /**
     * The maximum amount of time a connection will allow to try and reconnect.
     * This value is equivalent to the summation of the servers disconnect and keep alive timeout values.
     */
    public get reconnectWindow(): number {
        return this._reconnectWindow;
    }
    public set reconnectWindow(value: number) {
        this._reconnectWindow = value;
    }

    /**
     * Object to store the various keep alive timeout values
     */
    public get keepAliveData(): ka.KeepAliveData {
        return this._keepAliveData;
    }
    public set keepAliveData(value: ka.KeepAliveData) {
        this._keepAliveData = value;
    }

    /**
     * The timestamp of the last message received by the connection.
     */
    public get lastMessageAt(): number {
        return this._lastMessageAt;
    }

    public get lastActiveAt(): number {
        return this._lastActiveAt;
    }

    public get traceLevel(): TraceLevels {
        return this._traceLevel;
    }
    public set traceLevel(value: TraceLevels) {
        this._traceLevel = value;
    }

    /**
     * Gets and sets headers for the requests
     */
    public get headers(): { [s: string]: string } {
        return this._headers;
    }

    /**
     * Gets the url for the connection.
     */
    public get url(): string {
        return this._url;
    }

    /**
     * Gets or sets the last message id for the connection.
     */
    public get messageId(): string {
        return this._messageId;
    }
    public set messageId(value: string) {
        this._messageId = value;
    }

    /**
     * Gets or sets the connection id for the connection.
     */
    public get connectionId(): string {
        return this._connectionId;
    }
    public set connectionId(value: string) {
        this._connectionId = value;
    }

    /**
     * Gets or sets the connection token for the connection.
     */
    public get connectionToken(): string {
        return this._connectionToken;
    }
    public set connectionToken(value: string) {
        this._connectionToken = value;
    }

    /**
     * Gets or sets the groups token for the connection.
     */
    public get groupsToken(): string {
        return this._groupsToken;
    }
    public set groupsToken(value: string) {
        this._groupsToken = value;
    }

    /**
     * Gets a dictionary for storing state for a the connection.
     */
    public get items(): { [s: string]: any } {
        return this._items;
    }

    /**
     * Gets the querystring specified in the ctor.
     */
    public get queryString(): string {
        return this._queryString;
    }

    public get transport(): t.IClientTransport {
        return this._transport;
    }

    /**
     * Gets the current ConnectionState of the connection.
     */
    public get state(): ConnectionState {
        return this._state;
    }
    public set state(value: ConnectionState) {
        if (this._state != value) {
            var stateChange = new StateChange(this._state, value);
            this._state = value;
            this._stateChanged.trigger(stateChange);
        }
    }

    /*
     * Starts the connection
     */
    public start(): Promise<void> {
        return this.startWithClient(new http.DefaultClient());
    }

    /*
     * Starts the connection
     */
    public startWithClient(client: http.IClient): Promise<void> {
        return this.startWithTransport(new t.AutoTransport(client));
    }

    /*
     * Starts the connection
     */
    public startWithTransport(transport: t.IClientTransport): Promise<void> {
        if (!this.changeState(ConnectionState.disconnected, ConnectionState.connecting))
            return this._connectTask || (<any>Promise.resolve(null));

        this._startTcs = new utils.Defer<void>();
        this._receiveQueue = new utils.TaskQueue(this._startTcs.promise);
        this._lastQueuedReceiveTask = <any>Promise.resolve(null);

        this._transport = transport;

        return this._connectTask = this.negotiate(transport);
    }

    public onSending(): string {
        return null;
    }

    private negotiate(transport: t.IClientTransport) {
        this._connectionData = this.onSending();

        return transport.negotiate(this, this._connectionData)
            .then(negotiationResponse => {
                this.verifyProtocolVersion(negotiationResponse.protocolVersion);

                this._connectionId = negotiationResponse.connectionId;
                this._connectionToken = negotiationResponse.connectionToken;
                this._disconnectTimeout = negotiationResponse.disconnectTimeout;
                this._totalTransportConnectTimeout = this.transportConnectTimeout + negotiationResponse.transportConnectTimeout;

                // default the beat interval to be 5 seconds in case keep alive is disabled.
                var beatInterval = 5 * 1000;

                // If we have a keep alive
                if (negotiationResponse.keepAliveTimeout != null) {
                    this._keepAliveData = new ka.KeepAliveData(negotiationResponse.keepAliveTimeout);
                    this._reconnectWindow = this._disconnectTimeout + this._keepAliveData.timeout;

                    beatInterval = this._keepAliveData.checkInterval;
                } else {
                    this._reconnectWindow = this._disconnectTimeout;
                }

                this._monitor = new utils.HeartbeatMonitor(this, beatInterval);

                return this.startTransport();
            })
            .catch(() => this.disconnect());
    }

    private startTransport(): Promise<void> {
        return this._transport.start(this, this._connectionData)
            .then(() => {
                // NOTE: We have tests that rely on this state change occuring *BEFORE* the start task is complete
                this.changeState(ConnectionState.connecting, ConnectionState.connected);

                // now that we're connected complete the start task that the
                // receive queue is waiting on
                this._startTcs.resolve(null);

                // start the monitor to check for server activity
                this._lastMessageAt = Date.now();
                this._lastActiveAt = Date.now();
                this._monitor.start();
            })
            // don't return until the last receive has been processed to ensure messages/state sent in OnConnected
            // are processed prior to the Start() method task finishing
            .then(() => this._lastQueuedReceiveTask);
    }

    public changeState(oldState: ConnectionState, newState: ConnectionState): boolean {
        // If we're in the expected old state then change state and return true
        if (this._state === oldState) {
            this.trace(TraceLevels.statchanges, 'ChangeState({0}, {1})', ConnectionState[oldState], ConnectionState[newState]);

            this.state = newState;
            return true;
        }

        return false;
    }

    private verifyProtocolVersion(versionString: string): void {
        if (versionString !== this._protocol) {
            throw new Error('Invalid protocol version');
        }
    }

    public stop(timeout: number = defaultAbortTimeout): void {
        throw new Error('not implemented');
    }

    public disconnect(): void {
        throw new Error('not implemented');
    }

    public onClosed(): void {
        this._closed.trigger(null);
    }

    public send(data: string): Promise<void> {
        if (this.state === ConnectionState.disconnected)
            throw new Error('can\'t send when disconnected');
        if (this.state === ConnectionState.connecting)
            throw new Error('can\'t send while connecting');

        return this._transport.send(this, data, this._connectionData);
    }

    public trace(level: TraceLevels, format: string, ...args: string[]): void {
        if ((level & this._traceLevel) === level) {
            if (console && console.info)
                console.info(utils.formatString(format, args));
            else if (console)
                console.log(utils.formatString(format, args));
        }
    }

    public onReceived(data: {}) {
        this._lastQueuedReceiveTask = this._receiveQueue.enqueueSync(() => {
            try {
                this.onMessageReceived(data);
            } catch (e) {
                this.onError(e);
            }
        });
    }

    public onMessageReceived(data: {}): void {
        try {
            this._received.trigger(JSON.stringify(data));
        } catch (e) {
            this.onError(e);
        }
    }

    public onError(error: any): void {
        this.trace(TraceLevels.events, "onError({0})", error);

        this._error.trigger(error);
    }

    public onReconnecting(): void {
        this._disconnectTimeoutOperation = setDisposableTimeout(this.disconnect.bind(this), this._disconnectTimeout);

        this._reconnecting.trigger(null);
    }

    public onReconnected(): void {
        this._disconnectTimeoutOperation();

        this._reconnected.trigger(null);
        this.markLastMessage();
    }

    public onConnectionSlow(): void {
        this.trace(TraceLevels.events, "onConnectionSlow");

        this._connectionSlow.trigger(null);
    }

    public markLastMessage(): void {
        this._lastMessageAt = Date.now();
    }

    public markActive(): void {
        this._lastActiveAt = Date.now();
    }

    public prepareRequest(request: http.IRequest): void {
        request.userAgent = 'SignalR.Client.TS';
        request.setRequestHeaders(this.headers);
    }
}

export module Utils {
    export function ensureReconnecting(connection: IConnection): boolean {
        if (connection.changeState(ConnectionState.connected, ConnectionState.reconnecting)) {
            connection.onReconnecting();
        }

        return connection.state === ConnectionState.reconnecting;
    }
}