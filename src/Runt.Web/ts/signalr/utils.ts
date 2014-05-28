/// <reference path='../_ref.d.ts' /> 

import conn = require('./connection');

export function abstract(): any {
    throw new Error('this function is abstract');
}

export function formatString(format: string, args: string[]): string {
    for (var i = 0, l = args.length; i < l; i++) {
        format = format.replace('{' + i + '}', args[i]);
    }
    return format;
}
export interface IDisposable {
    dispose(): void;
}

export class Disposable implements IDisposable {
    private _disposed: boolean = false;
    private _fn: () => void;

    constructor(fn: () => void) {
        this._fn = fn;
    }

    public dispose(): void {
        if (this._disposed)
            return;

        this._disposed = true;
        this._fn();
    }

    public static all(disposables: IDisposable[]): IDisposable {
        return new Disposable(() => {
            disposables.forEach(d => d.dispose());
        });
    }
}

export interface ILiteEvent<T> {
    on(handler: (data: T) => void): IDisposable;
}

export class LiteEvent<T> implements ILiteEvent<T> {
    private _handlers: { (data?: T): void; }[] = [];

    public on(handler: (data: T) => void) {
        this._handlers.push(handler);
        return {
            dispose() {
                this.handlers = this.handlers.filter(h => h !== handler);
            }
        };
    }

    public trigger(data: T): void {
        this._handlers.forEach(h => h(data));
    }
}

export class Defer<T> {
    private _resolve: (value: T) => void;
    private _reject: (err: any) => void;
    private _promise: Promise<T>;

    constructor() {
        this._promise = new Promise((resolve, reject) => {
            this._resolve = resolve;
            this._reject = reject;
        });
    }

    public resolve(value: T) {
        this._resolve(value);
    }

    public reject(err: any) {
        this._reject(err);
    }

    public get promise(): Promise<T> {
        return this._promise;
    }

    public static delay(length: number): Promise<void> {
        return new Promise<void>(resolve => setTimeout(resolve, length));
    }
}

export class HeartbeatMonitor implements IDisposable {
    private _timer: number = 0;
    private _connection: conn.IConnection = null;
    private _beatInterval: number = 0;
    private _monitorKeepAlive: boolean = false;
    private _hasBeenWarned: boolean = false;
    private _timedOut: boolean = false;

    constructor(connection: conn.IConnection, beatInterval: number) {
        this._connection = connection;
        this._beatInterval = beatInterval;
    }

    public start(): void {
        this._monitorKeepAlive = this._connection.keepAliveData !== null && this._connection.transport.supportsKeepAlive;

        this._hasBeenWarned = false;
        this._timedOut = false;
        this._timer = setInterval(_ => this.timedBeat(), this._beatInterval);
    }

    private timedBeat(): void {
        var timeElapsed = Date.now() - this._connection.lastMessageAt;
        this.beat(timeElapsed);
    }

    public beat(ellapsedTime: number): void {
        if (this._monitorKeepAlive)
            this.checkKeepAlive(ellapsedTime);

        this._connection.markActive();
    }

    private checkKeepAlive(ellapsedTime: number): void {
        if (this._connection.state === conn.ConnectionState.connected) {
            if (ellapsedTime >= this._connection.keepAliveData.timeout) {
                if (!this._timedOut) {
                    // Connection has been lost
                    this._connection.trace(conn.TraceLevels.events, 'Connection Timed-out : Transport Lost Connection');
                    this._timedOut = true;
                    this._connection.transport.lostConnection(this._connection);
                }
            } else if (ellapsedTime >= this._connection.keepAliveData.timeoutWarning) {
                if (!this._hasBeenWarned) {
                    // Inform user and set HasBeenWarned to true
                    this._connection.trace(conn.TraceLevels.events, 'Connection Timeout Warning : Notifying user');
                    this._hasBeenWarned = true;
                    this._connection.onConnectionSlow();
                }
            } else {
                this._hasBeenWarned = false;
                this._timedOut = false;
            }
        }
    }

    public dispose(): void {
        clearInterval(this._timer);
    }
}

export class TaskQueue {
    private _lastQueuedTask: Promise<void> = null;
    private _drained: boolean = false;
    private _maxSize: number = null;
    private _size: number = 0;

    constructor(initialTask: Promise<void> = <any>Promise.resolve(null), maxSize: number = null) {
        this._lastQueuedTask = initialTask;
        this._maxSize = maxSize;
    }

    public get isDrained(): boolean {
        return this._drained;
    }

    public enqueueSync(fun: (s: any) => void, state: any = null): Promise<void> {
        return this.enqueue(s => new Promise<void>((resolve, reject) => {
            fun(s);
            resolve(null);
        }), state);
    }

    public enqueue(taskFun: (s: any) => Promise<void>, state: any = null): Promise<void> {
        if (this._drained)
            return this._lastQueuedTask;

        if (this._maxSize !== null) {
            if (this._size === this._maxSize)
                return null;
            this._size++;
        }

        var newTask = this._lastQueuedTask.then(() => {
            return taskFun(state).then(r => {
                this._size--;
                return r;
            }, e => {
                this._size--;
                throw e;
            });
        });

        this._lastQueuedTask = newTask;
        return newTask;
    }

    public drain(): Promise<void> {
        this._drained = true;
        return this._lastQueuedTask;
    }
}