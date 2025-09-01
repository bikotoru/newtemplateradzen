const IS_FOCUSABLE_ATTRIBUTE = 'data-is-focusable';
const IS_SCROLLABLE_ATTRIBUTE = 'data-is-scrollable';
const IS_VISIBLE_ATTRIBUTE = 'data-is-visible';
const FOCUSZONE_ID_ATTRIBUTE = 'data-focuszone-id';
const FOCUSZONE_SUB_ATTRIBUTE = 'data-is-sub-focuszone';
const IsFocusVisibleClassName = 'ms-Fabric--isFocusVisible';

export function initializeFocusRects() {
    if (!window.__hasInitializeFocusRects__) {
        window.__hasInitializeFocusRects__ = true;
        window.addEventListener("mousedown", _onFocusRectMouseDown, true);
        window.addEventListener("keydown", _onFocusRectKeyDown, true);
    }
}

function _onFocusRectMouseDown(ev) {
    if (window.document.body.classList.contains(IsFocusVisibleClassName)) {
        window.document.body.classList.remove(IsFocusVisibleClassName);
    }
}

function _onFocusRectKeyDown(ev) {
    if (isDirectionalKeyCode(ev.which) && !window.document.body.classList.contains(IsFocusVisibleClassName)) {
        window.document.body.classList.add(IsFocusVisibleClassName);
    }
}

const DirectionalKeyCodes = {
    [38]: 1, // up
    [40]: 1, // down
    [37]: 1, // left
    [39]: 1, // right
    [36]: 1, // home
    [35]: 1, // end
    [9]: 1,  // tab
    [33]: 1, // pageUp
    [34]: 1  // pageDown
};

function isDirectionalKeyCode(which) {
    return !!DirectionalKeyCodes[which];
}

export function measureElement(element) {
    var rect = {
        width: element.clientWidth,
        height: element.clientHeight,
        left: 0,
        top: 0
    };
    return rect;
}

export function measureElementRect(element) {
    if (element !== undefined && element !== null) {
        var rect = element.getBoundingClientRect();
        return rect;
    }
    else
        return { height: 0, width: 0, left: 0, right: 0, top: 0, bottom: 0 };
}

export function getWindow(element) {
    return element?.ownerDocument.defaultView;
}

export function getWindowRect() {
    var rect = {
        width: window.innerWidth,
        height: window.innerHeight,
        top: 0,
        left: 0
    };
    return rect;
}

const _disableIosBodyScroll = (event) => {
    event.preventDefault();
};

var _bodyScrollDisabledCount = 0;

export function enableBodyScroll() {
    if (_bodyScrollDisabledCount > 0) {
        if (_bodyScrollDisabledCount === 1) {
            document.body.classList.remove("disabledBodyScroll");
            document.body.removeEventListener('touchmove', _disableIosBodyScroll);
        }
        _bodyScrollDisabledCount--;
    }
}

export function disableBodyScroll() {
    if (!_bodyScrollDisabledCount) {
        document.body.classList.add("disabledBodyScroll");
        document.body.addEventListener('touchmove', _disableIosBodyScroll, { passive: false, capture: false });
    }
    _bodyScrollDisabledCount++;
}

var _lastId = 0;
var cachedViewports = new Map();

export class Async {
    constructor(parent, onError) {
        this._timeoutIds = null;
        this._immediateIds = null;
        this._intervalIds = null;
        this._animationFrameIds = null;
        this._isDisposed = false;
        this._parent = parent || null;
        this._onErrorHandler = onError;
        this._noop = () => {
            /* do nothing */
        };
    }

    dispose() {
        let id;
        this._isDisposed = true;
        this._parent = null;

        if (this._timeoutIds) {
            for (id in this._timeoutIds) {
                if (this._timeoutIds.hasOwnProperty(id)) {
                    this.clearTimeout(parseInt(id, 10));
                }
            }
            this._timeoutIds = null;
        }

        if (this._immediateIds) {
            for (id in this._immediateIds) {
                if (this._immediateIds.hasOwnProperty(id)) {
                    this.clearImmediate(parseInt(id, 10));
                }
            }
            this._immediateIds = null;
        }

        if (this._intervalIds) {
            for (id in this._intervalIds) {
                if (this._intervalIds.hasOwnProperty(id)) {
                    this.clearInterval(parseInt(id, 10));
                }
            }
            this._intervalIds = null;
        }

        if (this._animationFrameIds) {
            for (id in this._animationFrameIds) {
                if (this._animationFrameIds.hasOwnProperty(id)) {
                    this.cancelAnimationFrame(parseInt(id, 10));
                }
            }
            this._animationFrameIds = null;
        }
    }

    setTimeout(callback, duration) {
        let timeoutId = 0;
        if (!this._isDisposed) {
            if (!this._timeoutIds) {
                this._timeoutIds = {};
            }
            timeoutId = setTimeout(() => {
                try {
                    if (this._timeoutIds) {
                        delete this._timeoutIds[timeoutId];
                    }
                    callback.apply(this._parent);
                }
                catch (e) {
                    this._logError(e);
                }
            }, duration);
            this._timeoutIds[timeoutId] = true;
        }
        return timeoutId;
    }

    clearTimeout(id) {
        if (this._timeoutIds && this._timeoutIds[id]) {
            clearTimeout(id);
            delete this._timeoutIds[id];
        }
    }

    debounce(func, wait, options) {
        if (this._isDisposed) {
            let noOpFunction = (() => {
                /** Do nothing */
            });
            noOpFunction.cancel = () => {
                return;
            };
            noOpFunction.flush = (() => null);
            noOpFunction.pending = () => false;
            return noOpFunction;
        }

        let waitMS = wait || 0;
        let leading = false;
        let trailing = true;
        let maxWait = null;
        let lastCallTime = 0;
        let lastExecuteTime = Date.now();
        let lastResult;
        let lastArgs;
        let timeoutId = null;

        if (options && typeof options.leading === 'boolean') {
            leading = options.leading;
        }
        if (options && typeof options.trailing === 'boolean') {
            trailing = options.trailing;
        }
        if (options && typeof options.maxWait === 'number' && !isNaN(options.maxWait)) {
            maxWait = options.maxWait;
        }

        let markExecuted = (time) => {
            if (timeoutId) {
                this.clearTimeout(timeoutId);
                timeoutId = null;
            }
            lastExecuteTime = time;
        };

        let invokeFunction = (time) => {
            markExecuted(time);
            lastResult = func.apply(this._parent, lastArgs);
        };

        let callback = (userCall) => {
            let now = Date.now();
            let executeImmediately = false;

            if (userCall) {
                if (leading && now - lastCallTime >= waitMS) {
                    executeImmediately = true;
                }
                lastCallTime = now;
            }

            let delta = now - lastCallTime;
            let waitLength = waitMS - delta;
            let maxWaitDelta = now - lastExecuteTime;
            let maxWaitExpired = false;

            if (maxWait !== null) {
                if (maxWaitDelta >= maxWait && timeoutId) {
                    maxWaitExpired = true;
                }
                else {
                    waitLength = Math.min(waitLength, maxWait - maxWaitDelta);
                }
            }

            if (delta >= waitMS || maxWaitExpired || executeImmediately) {
                invokeFunction(now);
            }
            else if ((timeoutId === null || !userCall) && trailing) {
                timeoutId = this.setTimeout(callback, waitLength);
            }

            return lastResult;
        };

        let resultFunction = ((...args) => {
            lastArgs = args;
            return callback(true);
        });

        resultFunction.cancel = () => {
            if (timeoutId) {
                markExecuted(Date.now());
            }
        };

        resultFunction.flush = () => {
            if (timeoutId) {
                invokeFunction(Date.now());
            }
            return lastResult;
        };

        resultFunction.pending = () => {
            return !!timeoutId;
        };

        return resultFunction;
    }

    _logError(e) {
        if (this._onErrorHandler) {
            this._onErrorHandler(e);
        }
    }
}

class Viewport {
    constructor(component, rootElement, fireInitialViewport = false) {
        this.RESIZE_DELAY = 100;
        this.MAX_RESIZE_ATTEMPTS = 3;
        this.viewport = { width: 0, height: 0 };
        this._onAsyncResizeAsync = () => {
            this._updateViewportAsync();
        };

        this.id = _lastId++;
        this.component = component;
        this.rootElement = rootElement;
        this._async = new Async(this);
        this._onAsyncResizeAsync = this._async.debounce(this._onAsyncResizeAsync, this.RESIZE_DELAY, { leading: true });
        this.viewportResizeObserver = new window.ResizeObserver(this._onAsyncResizeAsync);
        this.viewportResizeObserver.observe(this.rootElement);

        if (fireInitialViewport) {
            this._onAsyncResizeAsync();
        }
    }

    disconnect() {
        this.viewportResizeObserver.disconnect();
    }

    async _updateViewportAsync(withForceUpdate) {
        const viewportElement = this.rootElement;
        const scrollElement = findScrollableParent(viewportElement);
        const scrollRect = getRect(scrollElement);
        const clientRect = getRect(viewportElement);

        const updateComponentAsync = async () => {
            if (withForceUpdate) {
                await this.component.invokeMethodAsync("ForceUpdate");
            }
        };

        const isSizeChanged = (clientRect && clientRect.width) !== this.viewport.width || 
                              (scrollRect && scrollRect.height) !== this.viewport.height;

        if (isSizeChanged && this._resizeAttempts < this.MAX_RESIZE_ATTEMPTS && clientRect && scrollRect) {
            this._resizeAttempts++;
            this.viewport = {
                width: clientRect.width,
                height: scrollRect.height
            };
            await this.component.invokeMethodAsync("ViewportChanged", this.viewport);
            await this._updateViewportAsync(withForceUpdate);
        }
        else {
            this._resizeAttempts = 0;
            await updateComponentAsync();
        }
    }
}

export function addViewport(component, rootElement, fireInitialViewport = false) {
    let viewport = new Viewport(component, rootElement, fireInitialViewport);
    cachedViewports.set(viewport.id, viewport);
    return viewport.id;
}

export function removeViewport(id) {
    let viewport = cachedViewports.get(id);
    viewport.disconnect();
    cachedViewports.delete(id);
}

export function getRect(element) {
    let rect;
    if (element) {
        if (element === window) {
            rect = {
                left: 0,
                top: 0,
                width: window.innerWidth,
                height: window.innerHeight,
                right: window.innerWidth,
                bottom: window.innerHeight,
            };
        }
        else if (element.getBoundingClientRect) {
            rect = element.getBoundingClientRect();
        }
    }
    return rect;
}

export function findScrollableParent(startingElement) {
    let el = startingElement;
    
    while (el && el !== document.body) {
        if (el.getAttribute(IS_SCROLLABLE_ATTRIBUTE) === 'true') {
            return el;
        }
        el = el.parentElement;
    }

    el = startingElement;
    while (el && el !== document.body) {
        if (el.getAttribute(IS_SCROLLABLE_ATTRIBUTE) !== 'false') {
            const computedStyles = getComputedStyle(el);
            let overflowY = computedStyles ? computedStyles.getPropertyValue('overflow-y') : '';
            if (overflowY && (overflowY === 'scroll' || overflowY === 'auto')) {
                return el;
            }
        }
        el = el.parentElement;
    }

    if (!el || el === document.body) {
        el = window;
    }
    return el;
}