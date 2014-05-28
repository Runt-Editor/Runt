/// <reference path='../_ref.d.ts' /> 

import conn = require('./connection');

/** The http request */
export interface IRequest {
    /** The user agent for this request. */
    userAgent: string;

    /** The accept header for this request. */
    accept: string;

    /** Aborts the request. */
    abort(): void;

    /** Set Request Headers */
    setRequestHeaders(headers: { [s: string]: string }): void;
} 

/**
 * The http response.
 */
export interface IResponse {
    body: string;
}

/**
 * A client that can make http request.
 */
export interface IClient {
    /**
     * Initializes the Http Clients
     */
    initialize(connection: conn.IConnection);

    /**
     * Makes an asynchronous http GET request to the specified url.
     */
    get(url: string, prepareRequest: (request: IRequest) => void, isLongRunning: boolean): Promise<IResponse>;

    /**
     * Makes an asynchronous http POST request to the specified url.
     */
    post(url: string, prepareRequest: (request: IRequest) => void, data: { [s: string]: string }, isLongRunning: boolean): Promise<IResponse>;
}

class XhrRequest implements IRequest, IResponse {
    private _url: string;
    private _method: string;
    private _xhr: XMLHttpRequest;
    private _data: string;
    private _isLongRunning: boolean;
    private _headers: { [s: string]: string } = {
        'User-Agent': 'XMLHTTP/1.0',
        'Accept': 'application/json'
    };

    constructor(url: string, method: string, data: string, isLongRunning: boolean) {
        this._url = url;
        this._method = method;
        this._data = data;
        this._isLongRunning = false;
    }

    public get userAgent(): string {
        return this._headers['User-Agent'];
    }
    public set userAgent(value: string) {
        this._headers['User-Agent'] = value;
    }

    public get accept(): string {
        return this._headers['Accept'];
    }
    public set accept(value: string) {
        this._headers['Accept'] = value;
    }

    public get body(): string {
        if (this._xhr.readyState !== 4) {
            throw new Error('not ready');
        }

        return this._xhr.responseText;
    }

    public abort(): void {
        this._xhr.abort();
    }

    public setRequestHeaders(headers: { [s: string]: string }): void {
        Object.getOwnPropertyNames(headers).forEach(n => {
            this._headers[n] = headers[n];
        });
    }

    public send(): Promise<IResponse> {
        return new Promise<IResponse>((resolve, reject) => {
            this._xhr = new XMLHttpRequest();
            this._xhr.open(this._method, this._url, true);
            Object.getOwnPropertyNames(this._headers).forEach(n => {
                if (n.toLowerCase() === 'user-agent')
                    return;
                this._xhr.setRequestHeader(n, this._headers[n]);
            });
            if (!this._isLongRunning) {
                // apparently, this is ignored
            }

            this._xhr.onreadystatechange = e => {
                if (this._xhr.readyState === 4) {
                    resolve(this);
                }
            };

            this._xhr.send(this._data);
        });
    }
}

export class DefaultClient implements IClient {
    private _connection: conn.IConnection;

    public initialize(connection: conn.IConnection): void {
        this._connection = connection;
    }

    public get(url: string, prepareRequest: (request: IRequest) => void, isLongRunning: boolean): Promise<IResponse> {
        return this.send(url, 'GET', null, prepareRequest, isLongRunning);
    }

    public post(url: string, prepareRequest: (request: IRequest) => void, data: { [s: string]: string }, isLongRunning: boolean): Promise<IResponse> {
        return this.send(url, 'POST', data, prepareRequest, isLongRunning);
    }

    private send(url: string, method: string, data: { [s: string]: string }, prepareRequest: (request: IRequest) => void, isLongRunning: boolean): Promise<IResponse> {
        var request = new XhrRequest(url, method, JSON.stringify(data), isLongRunning);
        prepareRequest(request);
        return request.send();
    }
}