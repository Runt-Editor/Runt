/// <reference path='../_ref.d.ts' /> 

// Determines when we warn the developer that the connection may be lost
var _keepAliveWarnAt = 2.0 / 3.0;

/** Class to store all the Keep Alive properties */
export class KeepAliveData {
    private _timeout: number;
    private _timeoutWarning: number;
    private _checkInterval: number;

    constructor(timeout: number, timeoutWarning: number = timeout * _keepAliveWarnAt, checkInterval: number = (timeout - timeoutWarning) / 3) {
        this._timeout = timeout;
        this._timeoutWarning = timeoutWarning;
        this._checkInterval = checkInterval;
    }

    /** Timeout to designate when to force the connection into reconnecting */
    public get timeout(): number {
        return this._timeout;
    }

    /** Timeout to designate when to warn the developer that the connection may be dead or is hanging. */
    public get timeoutWarning(): number {
        return this._timeoutWarning;
    }

    /**
      * Frequency with which we check the keep alive.  It must be short in order to not miss/pick up any changes
      */
    public get checkInterval(): number {
        return this._checkInterval;
    }
}