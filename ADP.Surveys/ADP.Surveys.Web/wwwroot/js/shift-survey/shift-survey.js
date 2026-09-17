var lv = Object.defineProperty;
var _y = (i) => {
  throw TypeError(i);
};
var nv = (i, f, s) => f in i ? lv(i, f, { enumerable: !0, configurable: !0, writable: !0, value: s }) : i[f] = s;
var Al = (i, f, s) => nv(i, typeof f != "symbol" ? f + "" : f, s), us = (i, f, s) => f.has(i) || _y("Cannot " + s);
var jt = (i, f, s) => (us(i, f, "read from private field"), s ? s.call(i) : f.get(i)), Je = (i, f, s) => f.has(i) ? _y("Cannot add the same private member more than once") : f instanceof WeakSet ? f.add(i) : f.set(i, s), Oe = (i, f, s, r) => (us(i, f, "write to private field"), r ? r.call(i, s) : f.set(i, s), s), ie = (i, f, s) => (us(i, f, "access private method"), s);
var is = { exports: {} }, tt = {};
/**
 * @license React
 * react.production.js
 *
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 */
var Ey;
function av() {
  if (Ey) return tt;
  Ey = 1;
  var i = Symbol.for("react.transitional.element"), f = Symbol.for("react.portal"), s = Symbol.for("react.fragment"), r = Symbol.for("react.strict_mode"), d = Symbol.for("react.profiler"), m = Symbol.for("react.consumer"), b = Symbol.for("react.context"), T = Symbol.for("react.forward_ref"), q = Symbol.for("react.suspense"), g = Symbol.for("react.memo"), j = Symbol.for("react.lazy"), S = Symbol.for("react.activity"), C = Symbol.iterator;
  function G(h) {
    return h === null || typeof h != "object" ? null : (h = C && h[C] || h["@@iterator"], typeof h == "function" ? h : null);
  }
  var K = {
    isMounted: function() {
      return !1;
    },
    enqueueForceUpdate: function() {
    },
    enqueueReplaceState: function() {
    },
    enqueueSetState: function() {
    }
  }, P = Object.assign, B = {};
  function W(h, D, L) {
    this.props = h, this.context = D, this.refs = B, this.updater = L || K;
  }
  W.prototype.isReactComponent = {}, W.prototype.setState = function(h, D) {
    if (typeof h != "object" && typeof h != "function" && h != null)
      throw Error(
        "takes an object of state variables to update or a function which returns an object of state variables."
      );
    this.updater.enqueueSetState(this, h, D, "setState");
  }, W.prototype.forceUpdate = function(h) {
    this.updater.enqueueForceUpdate(this, h, "forceUpdate");
  };
  function it() {
  }
  it.prototype = W.prototype;
  function I(h, D, L) {
    this.props = h, this.context = D, this.refs = B, this.updater = L || K;
  }
  var ct = I.prototype = new it();
  ct.constructor = I, P(ct, W.prototype), ct.isPureReactComponent = !0;
  var Ft = Array.isArray;
  function Lt() {
  }
  var nt = { H: null, A: null, T: null, S: null }, _t = Object.prototype.hasOwnProperty;
  function _e(h, D, L) {
    var Q = L.ref;
    return {
      $$typeof: i,
      type: h,
      key: D,
      ref: Q !== void 0 ? Q : null,
      props: L
    };
  }
  function ql(h, D) {
    return _e(h.type, D, h.props);
  }
  function bt(h) {
    return typeof h == "object" && h !== null && h.$$typeof === i;
  }
  function kt(h) {
    var D = { "=": "=0", ":": "=2" };
    return "$" + h.replace(/[=:]/g, function(L) {
      return D[L];
    });
  }
  var nl = /\/+/g;
  function Qe(h, D) {
    return typeof h == "object" && h !== null && h.key != null ? kt("" + h.key) : D.toString(36);
  }
  function Qt(h) {
    switch (h.status) {
      case "fulfilled":
        return h.value;
      case "rejected":
        throw h.reason;
      default:
        switch (typeof h.status == "string" ? h.then(Lt, Lt) : (h.status = "pending", h.then(
          function(D) {
            h.status === "pending" && (h.status = "fulfilled", h.value = D);
          },
          function(D) {
            h.status === "pending" && (h.status = "rejected", h.reason = D);
          }
        )), h.status) {
          case "fulfilled":
            return h.value;
          case "rejected":
            throw h.reason;
        }
    }
    throw h;
  }
  function N(h, D, L, Q, F) {
    var at = typeof h;
    (at === "undefined" || at === "boolean") && (h = null);
    var vt = !1;
    if (h === null) vt = !0;
    else
      switch (at) {
        case "bigint":
        case "string":
        case "number":
          vt = !0;
          break;
        case "object":
          switch (h.$$typeof) {
            case i:
            case f:
              vt = !0;
              break;
            case j:
              return vt = h._init, N(
                vt(h._payload),
                D,
                L,
                Q,
                F
              );
          }
      }
    if (vt)
      return F = F(h), vt = Q === "" ? "." + Qe(h, 0) : Q, Ft(F) ? (L = "", vt != null && (L = vt.replace(nl, "$&/") + "/"), N(F, D, L, "", function(ke) {
        return ke;
      })) : F != null && (bt(F) && (F = ql(
        F,
        L + (F.key == null || h && h.key === F.key ? "" : ("" + F.key).replace(
          nl,
          "$&/"
        ) + "/") + vt
      )), D.push(F)), 1;
    vt = 0;
    var Xt = Q === "" ? "." : Q + ":";
    if (Ft(h))
      for (var Ct = 0; Ct < h.length; Ct++)
        Q = h[Ct], at = Xt + Qe(Q, Ct), vt += N(
          Q,
          D,
          L,
          at,
          F
        );
    else if (Ct = G(h), typeof Ct == "function")
      for (h = Ct.call(h), Ct = 0; !(Q = h.next()).done; )
        Q = Q.value, at = Xt + Qe(Q, Ct++), vt += N(
          Q,
          D,
          L,
          at,
          F
        );
    else if (at === "object") {
      if (typeof h.then == "function")
        return N(
          Qt(h),
          D,
          L,
          Q,
          F
        );
      throw D = String(h), Error(
        "Objects are not valid as a React child (found: " + (D === "[object Object]" ? "object with keys {" + Object.keys(h).join(", ") + "}" : D) + "). If you meant to render a collection of children, use an array instead."
      );
    }
    return vt;
  }
  function H(h, D, L) {
    if (h == null) return h;
    var Q = [], F = 0;
    return N(h, Q, "", "", function(at) {
      return D.call(L, at, F++);
    }), Q;
  }
  function $(h) {
    if (h._status === -1) {
      var D = h._result;
      D = D(), D.then(
        function(L) {
          (h._status === 0 || h._status === -1) && (h._status = 1, h._result = L);
        },
        function(L) {
          (h._status === 0 || h._status === -1) && (h._status = 2, h._result = L);
        }
      ), h._status === -1 && (h._status = 0, h._result = D);
    }
    if (h._status === 1) return h._result.default;
    throw h._result;
  }
  var Y = typeof reportError == "function" ? reportError : function(h) {
    if (typeof window == "object" && typeof window.ErrorEvent == "function") {
      var D = new window.ErrorEvent("error", {
        bubbles: !0,
        cancelable: !0,
        message: typeof h == "object" && h !== null && typeof h.message == "string" ? String(h.message) : String(h),
        error: h
      });
      if (!window.dispatchEvent(D)) return;
    } else if (typeof process == "object" && typeof process.emit == "function") {
      process.emit("uncaughtException", h);
      return;
    }
    console.error(h);
  }, yt = {
    map: H,
    forEach: function(h, D, L) {
      H(
        h,
        function() {
          D.apply(this, arguments);
        },
        L
      );
    },
    count: function(h) {
      var D = 0;
      return H(h, function() {
        D++;
      }), D;
    },
    toArray: function(h) {
      return H(h, function(D) {
        return D;
      }) || [];
    },
    only: function(h) {
      if (!bt(h))
        throw Error(
          "React.Children.only expected to receive a single React element child."
        );
      return h;
    }
  };
  return tt.Activity = S, tt.Children = yt, tt.Component = W, tt.Fragment = s, tt.Profiler = d, tt.PureComponent = I, tt.StrictMode = r, tt.Suspense = q, tt.__CLIENT_INTERNALS_DO_NOT_USE_OR_WARN_USERS_THEY_CANNOT_UPGRADE = nt, tt.__COMPILER_RUNTIME = {
    __proto__: null,
    c: function(h) {
      return nt.H.useMemoCache(h);
    }
  }, tt.cache = function(h) {
    return function() {
      return h.apply(null, arguments);
    };
  }, tt.cacheSignal = function() {
    return null;
  }, tt.cloneElement = function(h, D, L) {
    if (h == null)
      throw Error(
        "The argument must be a React element, but you passed " + h + "."
      );
    var Q = P({}, h.props), F = h.key;
    if (D != null)
      for (at in D.key !== void 0 && (F = "" + D.key), D)
        !_t.call(D, at) || at === "key" || at === "__self" || at === "__source" || at === "ref" && D.ref === void 0 || (Q[at] = D[at]);
    var at = arguments.length - 2;
    if (at === 1) Q.children = L;
    else if (1 < at) {
      for (var vt = Array(at), Xt = 0; Xt < at; Xt++)
        vt[Xt] = arguments[Xt + 2];
      Q.children = vt;
    }
    return _e(h.type, F, Q);
  }, tt.createContext = function(h) {
    return h = {
      $$typeof: b,
      _currentValue: h,
      _currentValue2: h,
      _threadCount: 0,
      Provider: null,
      Consumer: null
    }, h.Provider = h, h.Consumer = {
      $$typeof: m,
      _context: h
    }, h;
  }, tt.createElement = function(h, D, L) {
    var Q, F = {}, at = null;
    if (D != null)
      for (Q in D.key !== void 0 && (at = "" + D.key), D)
        _t.call(D, Q) && Q !== "key" && Q !== "__self" && Q !== "__source" && (F[Q] = D[Q]);
    var vt = arguments.length - 2;
    if (vt === 1) F.children = L;
    else if (1 < vt) {
      for (var Xt = Array(vt), Ct = 0; Ct < vt; Ct++)
        Xt[Ct] = arguments[Ct + 2];
      F.children = Xt;
    }
    if (h && h.defaultProps)
      for (Q in vt = h.defaultProps, vt)
        F[Q] === void 0 && (F[Q] = vt[Q]);
    return _e(h, at, F);
  }, tt.createRef = function() {
    return { current: null };
  }, tt.forwardRef = function(h) {
    return { $$typeof: T, render: h };
  }, tt.isValidElement = bt, tt.lazy = function(h) {
    return {
      $$typeof: j,
      _payload: { _status: -1, _result: h },
      _init: $
    };
  }, tt.memo = function(h, D) {
    return {
      $$typeof: g,
      type: h,
      compare: D === void 0 ? null : D
    };
  }, tt.startTransition = function(h) {
    var D = nt.T, L = {};
    nt.T = L;
    try {
      var Q = h(), F = nt.S;
      F !== null && F(L, Q), typeof Q == "object" && Q !== null && typeof Q.then == "function" && Q.then(Lt, Y);
    } catch (at) {
      Y(at);
    } finally {
      D !== null && L.types !== null && (D.types = L.types), nt.T = D;
    }
  }, tt.unstable_useCacheRefresh = function() {
    return nt.H.useCacheRefresh();
  }, tt.use = function(h) {
    return nt.H.use(h);
  }, tt.useActionState = function(h, D, L) {
    return nt.H.useActionState(h, D, L);
  }, tt.useCallback = function(h, D) {
    return nt.H.useCallback(h, D);
  }, tt.useContext = function(h) {
    return nt.H.useContext(h);
  }, tt.useDebugValue = function() {
  }, tt.useDeferredValue = function(h, D) {
    return nt.H.useDeferredValue(h, D);
  }, tt.useEffect = function(h, D) {
    return nt.H.useEffect(h, D);
  }, tt.useEffectEvent = function(h) {
    return nt.H.useEffectEvent(h);
  }, tt.useId = function() {
    return nt.H.useId();
  }, tt.useImperativeHandle = function(h, D, L) {
    return nt.H.useImperativeHandle(h, D, L);
  }, tt.useInsertionEffect = function(h, D) {
    return nt.H.useInsertionEffect(h, D);
  }, tt.useLayoutEffect = function(h, D) {
    return nt.H.useLayoutEffect(h, D);
  }, tt.useMemo = function(h, D) {
    return nt.H.useMemo(h, D);
  }, tt.useOptimistic = function(h, D) {
    return nt.H.useOptimistic(h, D);
  }, tt.useReducer = function(h, D, L) {
    return nt.H.useReducer(h, D, L);
  }, tt.useRef = function(h) {
    return nt.H.useRef(h);
  }, tt.useState = function(h) {
    return nt.H.useState(h);
  }, tt.useSyncExternalStore = function(h, D, L) {
    return nt.H.useSyncExternalStore(
      h,
      D,
      L
    );
  }, tt.useTransition = function() {
    return nt.H.useTransition();
  }, tt.version = "19.2.5", tt;
}
var xy;
function As() {
  return xy || (xy = 1, is.exports = av()), is.exports;
}
var V = As(), cs = { exports: {} }, Su = {}, fs = { exports: {} }, ss = {};
/**
 * @license React
 * scheduler.production.js
 *
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 */
var Ay;
function uv() {
  return Ay || (Ay = 1, (function(i) {
    function f(N, H) {
      var $ = N.length;
      N.push(H);
      t: for (; 0 < $; ) {
        var Y = $ - 1 >>> 1, yt = N[Y];
        if (0 < d(yt, H))
          N[Y] = H, N[$] = yt, $ = Y;
        else break t;
      }
    }
    function s(N) {
      return N.length === 0 ? null : N[0];
    }
    function r(N) {
      if (N.length === 0) return null;
      var H = N[0], $ = N.pop();
      if ($ !== H) {
        N[0] = $;
        t: for (var Y = 0, yt = N.length, h = yt >>> 1; Y < h; ) {
          var D = 2 * (Y + 1) - 1, L = N[D], Q = D + 1, F = N[Q];
          if (0 > d(L, $))
            Q < yt && 0 > d(F, L) ? (N[Y] = F, N[Q] = $, Y = Q) : (N[Y] = L, N[D] = $, Y = D);
          else if (Q < yt && 0 > d(F, $))
            N[Y] = F, N[Q] = $, Y = Q;
          else break t;
        }
      }
      return H;
    }
    function d(N, H) {
      var $ = N.sortIndex - H.sortIndex;
      return $ !== 0 ? $ : N.id - H.id;
    }
    if (i.unstable_now = void 0, typeof performance == "object" && typeof performance.now == "function") {
      var m = performance;
      i.unstable_now = function() {
        return m.now();
      };
    } else {
      var b = Date, T = b.now();
      i.unstable_now = function() {
        return b.now() - T;
      };
    }
    var q = [], g = [], j = 1, S = null, C = 3, G = !1, K = !1, P = !1, B = !1, W = typeof setTimeout == "function" ? setTimeout : null, it = typeof clearTimeout == "function" ? clearTimeout : null, I = typeof setImmediate < "u" ? setImmediate : null;
    function ct(N) {
      for (var H = s(g); H !== null; ) {
        if (H.callback === null) r(g);
        else if (H.startTime <= N)
          r(g), H.sortIndex = H.expirationTime, f(q, H);
        else break;
        H = s(g);
      }
    }
    function Ft(N) {
      if (P = !1, ct(N), !K)
        if (s(q) !== null)
          K = !0, Lt || (Lt = !0, kt());
        else {
          var H = s(g);
          H !== null && Qt(Ft, H.startTime - N);
        }
    }
    var Lt = !1, nt = -1, _t = 5, _e = -1;
    function ql() {
      return B ? !0 : !(i.unstable_now() - _e < _t);
    }
    function bt() {
      if (B = !1, Lt) {
        var N = i.unstable_now();
        _e = N;
        var H = !0;
        try {
          t: {
            K = !1, P && (P = !1, it(nt), nt = -1), G = !0;
            var $ = C;
            try {
              e: {
                for (ct(N), S = s(q); S !== null && !(S.expirationTime > N && ql()); ) {
                  var Y = S.callback;
                  if (typeof Y == "function") {
                    S.callback = null, C = S.priorityLevel;
                    var yt = Y(
                      S.expirationTime <= N
                    );
                    if (N = i.unstable_now(), typeof yt == "function") {
                      S.callback = yt, ct(N), H = !0;
                      break e;
                    }
                    S === s(q) && r(q), ct(N);
                  } else r(q);
                  S = s(q);
                }
                if (S !== null) H = !0;
                else {
                  var h = s(g);
                  h !== null && Qt(
                    Ft,
                    h.startTime - N
                  ), H = !1;
                }
              }
              break t;
            } finally {
              S = null, C = $, G = !1;
            }
            H = void 0;
          }
        } finally {
          H ? kt() : Lt = !1;
        }
      }
    }
    var kt;
    if (typeof I == "function")
      kt = function() {
        I(bt);
      };
    else if (typeof MessageChannel < "u") {
      var nl = new MessageChannel(), Qe = nl.port2;
      nl.port1.onmessage = bt, kt = function() {
        Qe.postMessage(null);
      };
    } else
      kt = function() {
        W(bt, 0);
      };
    function Qt(N, H) {
      nt = W(function() {
        N(i.unstable_now());
      }, H);
    }
    i.unstable_IdlePriority = 5, i.unstable_ImmediatePriority = 1, i.unstable_LowPriority = 4, i.unstable_NormalPriority = 3, i.unstable_Profiling = null, i.unstable_UserBlockingPriority = 2, i.unstable_cancelCallback = function(N) {
      N.callback = null;
    }, i.unstable_forceFrameRate = function(N) {
      0 > N || 125 < N ? console.error(
        "forceFrameRate takes a positive int between 0 and 125, forcing frame rates higher than 125 fps is not supported"
      ) : _t = 0 < N ? Math.floor(1e3 / N) : 5;
    }, i.unstable_getCurrentPriorityLevel = function() {
      return C;
    }, i.unstable_next = function(N) {
      switch (C) {
        case 1:
        case 2:
        case 3:
          var H = 3;
          break;
        default:
          H = C;
      }
      var $ = C;
      C = H;
      try {
        return N();
      } finally {
        C = $;
      }
    }, i.unstable_requestPaint = function() {
      B = !0;
    }, i.unstable_runWithPriority = function(N, H) {
      switch (N) {
        case 1:
        case 2:
        case 3:
        case 4:
        case 5:
          break;
        default:
          N = 3;
      }
      var $ = C;
      C = N;
      try {
        return H();
      } finally {
        C = $;
      }
    }, i.unstable_scheduleCallback = function(N, H, $) {
      var Y = i.unstable_now();
      switch (typeof $ == "object" && $ !== null ? ($ = $.delay, $ = typeof $ == "number" && 0 < $ ? Y + $ : Y) : $ = Y, N) {
        case 1:
          var yt = -1;
          break;
        case 2:
          yt = 250;
          break;
        case 5:
          yt = 1073741823;
          break;
        case 4:
          yt = 1e4;
          break;
        default:
          yt = 5e3;
      }
      return yt = $ + yt, N = {
        id: j++,
        callback: H,
        priorityLevel: N,
        startTime: $,
        expirationTime: yt,
        sortIndex: -1
      }, $ > Y ? (N.sortIndex = $, f(g, N), s(q) === null && N === s(g) && (P ? (it(nt), nt = -1) : P = !0, Qt(Ft, $ - Y))) : (N.sortIndex = yt, f(q, N), K || G || (K = !0, Lt || (Lt = !0, kt()))), N;
    }, i.unstable_shouldYield = ql, i.unstable_wrapCallback = function(N) {
      var H = C;
      return function() {
        var $ = C;
        C = H;
        try {
          return N.apply(this, arguments);
        } finally {
          C = $;
        }
      };
    };
  })(ss)), ss;
}
var Ty;
function iv() {
  return Ty || (Ty = 1, fs.exports = uv()), fs.exports;
}
var rs = { exports: {} }, se = {};
/**
 * @license React
 * react-dom.production.js
 *
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 */
var zy;
function cv() {
  if (zy) return se;
  zy = 1;
  var i = As();
  function f(q) {
    var g = "https://react.dev/errors/" + q;
    if (1 < arguments.length) {
      g += "?args[]=" + encodeURIComponent(arguments[1]);
      for (var j = 2; j < arguments.length; j++)
        g += "&args[]=" + encodeURIComponent(arguments[j]);
    }
    return "Minified React error #" + q + "; visit " + g + " for the full message or use the non-minified dev environment for full errors and additional helpful warnings.";
  }
  function s() {
  }
  var r = {
    d: {
      f: s,
      r: function() {
        throw Error(f(522));
      },
      D: s,
      C: s,
      L: s,
      m: s,
      X: s,
      S: s,
      M: s
    },
    p: 0,
    findDOMNode: null
  }, d = Symbol.for("react.portal");
  function m(q, g, j) {
    var S = 3 < arguments.length && arguments[3] !== void 0 ? arguments[3] : null;
    return {
      $$typeof: d,
      key: S == null ? null : "" + S,
      children: q,
      containerInfo: g,
      implementation: j
    };
  }
  var b = i.__CLIENT_INTERNALS_DO_NOT_USE_OR_WARN_USERS_THEY_CANNOT_UPGRADE;
  function T(q, g) {
    if (q === "font") return "";
    if (typeof g == "string")
      return g === "use-credentials" ? g : "";
  }
  return se.__DOM_INTERNALS_DO_NOT_USE_OR_WARN_USERS_THEY_CANNOT_UPGRADE = r, se.createPortal = function(q, g) {
    var j = 2 < arguments.length && arguments[2] !== void 0 ? arguments[2] : null;
    if (!g || g.nodeType !== 1 && g.nodeType !== 9 && g.nodeType !== 11)
      throw Error(f(299));
    return m(q, g, null, j);
  }, se.flushSync = function(q) {
    var g = b.T, j = r.p;
    try {
      if (b.T = null, r.p = 2, q) return q();
    } finally {
      b.T = g, r.p = j, r.d.f();
    }
  }, se.preconnect = function(q, g) {
    typeof q == "string" && (g ? (g = g.crossOrigin, g = typeof g == "string" ? g === "use-credentials" ? g : "" : void 0) : g = null, r.d.C(q, g));
  }, se.prefetchDNS = function(q) {
    typeof q == "string" && r.d.D(q);
  }, se.preinit = function(q, g) {
    if (typeof q == "string" && g && typeof g.as == "string") {
      var j = g.as, S = T(j, g.crossOrigin), C = typeof g.integrity == "string" ? g.integrity : void 0, G = typeof g.fetchPriority == "string" ? g.fetchPriority : void 0;
      j === "style" ? r.d.S(
        q,
        typeof g.precedence == "string" ? g.precedence : void 0,
        {
          crossOrigin: S,
          integrity: C,
          fetchPriority: G
        }
      ) : j === "script" && r.d.X(q, {
        crossOrigin: S,
        integrity: C,
        fetchPriority: G,
        nonce: typeof g.nonce == "string" ? g.nonce : void 0
      });
    }
  }, se.preinitModule = function(q, g) {
    if (typeof q == "string")
      if (typeof g == "object" && g !== null) {
        if (g.as == null || g.as === "script") {
          var j = T(
            g.as,
            g.crossOrigin
          );
          r.d.M(q, {
            crossOrigin: j,
            integrity: typeof g.integrity == "string" ? g.integrity : void 0,
            nonce: typeof g.nonce == "string" ? g.nonce : void 0
          });
        }
      } else g == null && r.d.M(q);
  }, se.preload = function(q, g) {
    if (typeof q == "string" && typeof g == "object" && g !== null && typeof g.as == "string") {
      var j = g.as, S = T(j, g.crossOrigin);
      r.d.L(q, j, {
        crossOrigin: S,
        integrity: typeof g.integrity == "string" ? g.integrity : void 0,
        nonce: typeof g.nonce == "string" ? g.nonce : void 0,
        type: typeof g.type == "string" ? g.type : void 0,
        fetchPriority: typeof g.fetchPriority == "string" ? g.fetchPriority : void 0,
        referrerPolicy: typeof g.referrerPolicy == "string" ? g.referrerPolicy : void 0,
        imageSrcSet: typeof g.imageSrcSet == "string" ? g.imageSrcSet : void 0,
        imageSizes: typeof g.imageSizes == "string" ? g.imageSizes : void 0,
        media: typeof g.media == "string" ? g.media : void 0
      });
    }
  }, se.preloadModule = function(q, g) {
    if (typeof q == "string")
      if (g) {
        var j = T(g.as, g.crossOrigin);
        r.d.m(q, {
          as: typeof g.as == "string" && g.as !== "script" ? g.as : void 0,
          crossOrigin: j,
          integrity: typeof g.integrity == "string" ? g.integrity : void 0
        });
      } else r.d.m(q);
  }, se.requestFormReset = function(q) {
    r.d.r(q);
  }, se.unstable_batchedUpdates = function(q, g) {
    return q(g);
  }, se.useFormState = function(q, g, j) {
    return b.H.useFormState(q, g, j);
  }, se.useFormStatus = function() {
    return b.H.useHostTransitionStatus();
  }, se.version = "19.2.5", se;
}
var qy;
function fv() {
  if (qy) return rs.exports;
  qy = 1;
  function i() {
    if (!(typeof __REACT_DEVTOOLS_GLOBAL_HOOK__ > "u" || typeof __REACT_DEVTOOLS_GLOBAL_HOOK__.checkDCE != "function"))
      try {
        __REACT_DEVTOOLS_GLOBAL_HOOK__.checkDCE(i);
      } catch (f) {
        console.error(f);
      }
  }
  return i(), rs.exports = cv(), rs.exports;
}
/**
 * @license React
 * react-dom-client.production.js
 *
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 */
var Ny;
function sv() {
  if (Ny) return Su;
  Ny = 1;
  var i = iv(), f = As(), s = fv();
  function r(t) {
    var e = "https://react.dev/errors/" + t;
    if (1 < arguments.length) {
      e += "?args[]=" + encodeURIComponent(arguments[1]);
      for (var l = 2; l < arguments.length; l++)
        e += "&args[]=" + encodeURIComponent(arguments[l]);
    }
    return "Minified React error #" + t + "; visit " + e + " for the full message or use the non-minified dev environment for full errors and additional helpful warnings.";
  }
  function d(t) {
    return !(!t || t.nodeType !== 1 && t.nodeType !== 9 && t.nodeType !== 11);
  }
  function m(t) {
    var e = t, l = t;
    if (t.alternate) for (; e.return; ) e = e.return;
    else {
      t = e;
      do
        e = t, (e.flags & 4098) !== 0 && (l = e.return), t = e.return;
      while (t);
    }
    return e.tag === 3 ? l : null;
  }
  function b(t) {
    if (t.tag === 13) {
      var e = t.memoizedState;
      if (e === null && (t = t.alternate, t !== null && (e = t.memoizedState)), e !== null) return e.dehydrated;
    }
    return null;
  }
  function T(t) {
    if (t.tag === 31) {
      var e = t.memoizedState;
      if (e === null && (t = t.alternate, t !== null && (e = t.memoizedState)), e !== null) return e.dehydrated;
    }
    return null;
  }
  function q(t) {
    if (m(t) !== t)
      throw Error(r(188));
  }
  function g(t) {
    var e = t.alternate;
    if (!e) {
      if (e = m(t), e === null) throw Error(r(188));
      return e !== t ? null : t;
    }
    for (var l = t, n = e; ; ) {
      var a = l.return;
      if (a === null) break;
      var u = a.alternate;
      if (u === null) {
        if (n = a.return, n !== null) {
          l = n;
          continue;
        }
        break;
      }
      if (a.child === u.child) {
        for (u = a.child; u; ) {
          if (u === l) return q(a), t;
          if (u === n) return q(a), e;
          u = u.sibling;
        }
        throw Error(r(188));
      }
      if (l.return !== n.return) l = a, n = u;
      else {
        for (var c = !1, o = a.child; o; ) {
          if (o === l) {
            c = !0, l = a, n = u;
            break;
          }
          if (o === n) {
            c = !0, n = a, l = u;
            break;
          }
          o = o.sibling;
        }
        if (!c) {
          for (o = u.child; o; ) {
            if (o === l) {
              c = !0, l = u, n = a;
              break;
            }
            if (o === n) {
              c = !0, n = u, l = a;
              break;
            }
            o = o.sibling;
          }
          if (!c) throw Error(r(189));
        }
      }
      if (l.alternate !== n) throw Error(r(190));
    }
    if (l.tag !== 3) throw Error(r(188));
    return l.stateNode.current === l ? t : e;
  }
  function j(t) {
    var e = t.tag;
    if (e === 5 || e === 26 || e === 27 || e === 6) return t;
    for (t = t.child; t !== null; ) {
      if (e = j(t), e !== null) return e;
      t = t.sibling;
    }
    return null;
  }
  var S = Object.assign, C = Symbol.for("react.element"), G = Symbol.for("react.transitional.element"), K = Symbol.for("react.portal"), P = Symbol.for("react.fragment"), B = Symbol.for("react.strict_mode"), W = Symbol.for("react.profiler"), it = Symbol.for("react.consumer"), I = Symbol.for("react.context"), ct = Symbol.for("react.forward_ref"), Ft = Symbol.for("react.suspense"), Lt = Symbol.for("react.suspense_list"), nt = Symbol.for("react.memo"), _t = Symbol.for("react.lazy"), _e = Symbol.for("react.activity"), ql = Symbol.for("react.memo_cache_sentinel"), bt = Symbol.iterator;
  function kt(t) {
    return t === null || typeof t != "object" ? null : (t = bt && t[bt] || t["@@iterator"], typeof t == "function" ? t : null);
  }
  var nl = Symbol.for("react.client.reference");
  function Qe(t) {
    if (t == null) return null;
    if (typeof t == "function")
      return t.$$typeof === nl ? null : t.displayName || t.name || null;
    if (typeof t == "string") return t;
    switch (t) {
      case P:
        return "Fragment";
      case W:
        return "Profiler";
      case B:
        return "StrictMode";
      case Ft:
        return "Suspense";
      case Lt:
        return "SuspenseList";
      case _e:
        return "Activity";
    }
    if (typeof t == "object")
      switch (t.$$typeof) {
        case K:
          return "Portal";
        case I:
          return t.displayName || "Context";
        case it:
          return (t._context.displayName || "Context") + ".Consumer";
        case ct:
          var e = t.render;
          return t = t.displayName, t || (t = e.displayName || e.name || "", t = t !== "" ? "ForwardRef(" + t + ")" : "ForwardRef"), t;
        case nt:
          return e = t.displayName || null, e !== null ? e : Qe(t.type) || "Memo";
        case _t:
          e = t._payload, t = t._init;
          try {
            return Qe(t(e));
          } catch {
          }
      }
    return null;
  }
  var Qt = Array.isArray, N = f.__CLIENT_INTERNALS_DO_NOT_USE_OR_WARN_USERS_THEY_CANNOT_UPGRADE, H = s.__DOM_INTERNALS_DO_NOT_USE_OR_WARN_USERS_THEY_CANNOT_UPGRADE, $ = {
    pending: !1,
    data: null,
    method: null,
    action: null
  }, Y = [], yt = -1;
  function h(t) {
    return { current: t };
  }
  function D(t) {
    0 > yt || (t.current = Y[yt], Y[yt] = null, yt--);
  }
  function L(t, e) {
    yt++, Y[yt] = t.current, t.current = e;
  }
  var Q = h(null), F = h(null), at = h(null), vt = h(null);
  function Xt(t, e) {
    switch (L(at, e), L(F, t), L(Q, null), e.nodeType) {
      case 9:
      case 11:
        t = (t = e.documentElement) && (t = t.namespaceURI) ? Vd(t) : 0;
        break;
      default:
        if (t = e.tagName, e = e.namespaceURI)
          e = Vd(e), t = wd(e, t);
        else
          switch (t) {
            case "svg":
              t = 1;
              break;
            case "math":
              t = 2;
              break;
            default:
              t = 0;
          }
    }
    D(Q), L(Q, t);
  }
  function Ct() {
    D(Q), D(F), D(at);
  }
  function ke(t) {
    t.memoizedState !== null && L(vt, t);
    var e = Q.current, l = wd(e, t.type);
    e !== l && (L(F, t), L(Q, l));
  }
  function oe(t) {
    F.current === t && (D(Q), D(F)), vt.current === t && (D(vt), vu._currentValue = $);
  }
  var za, qa;
  function al(t) {
    if (za === void 0)
      try {
        throw Error();
      } catch (l) {
        var e = l.stack.trim().match(/\n( *(at )?)/);
        za = e && e[1] || "", qa = -1 < l.stack.indexOf(`
    at`) ? " (<anonymous>)" : -1 < l.stack.indexOf("@") ? "@unknown:0:0" : "";
      }
    return `
` + za + t + qa;
  }
  var Xe = !1;
  function ce(t, e) {
    if (!t || Xe) return "";
    Xe = !0;
    var l = Error.prepareStackTrace;
    Error.prepareStackTrace = void 0;
    try {
      var n = {
        DetermineComponentFrameRoot: function() {
          try {
            if (e) {
              var U = function() {
                throw Error();
              };
              if (Object.defineProperty(U.prototype, "props", {
                set: function() {
                  throw Error();
                }
              }), typeof Reflect == "object" && Reflect.construct) {
                try {
                  Reflect.construct(U, []);
                } catch (z) {
                  var x = z;
                }
                Reflect.construct(t, [], U);
              } else {
                try {
                  U.call();
                } catch (z) {
                  x = z;
                }
                t.call(U.prototype);
              }
            } else {
              try {
                throw Error();
              } catch (z) {
                x = z;
              }
              (U = t()) && typeof U.catch == "function" && U.catch(function() {
              });
            }
          } catch (z) {
            if (z && x && typeof z.stack == "string")
              return [z.stack, x.stack];
          }
          return [null, null];
        }
      };
      n.DetermineComponentFrameRoot.displayName = "DetermineComponentFrameRoot";
      var a = Object.getOwnPropertyDescriptor(
        n.DetermineComponentFrameRoot,
        "name"
      );
      a && a.configurable && Object.defineProperty(
        n.DetermineComponentFrameRoot,
        "name",
        { value: "DetermineComponentFrameRoot" }
      );
      var u = n.DetermineComponentFrameRoot(), c = u[0], o = u[1];
      if (c && o) {
        var y = c.split(`
`), E = o.split(`
`);
        for (a = n = 0; n < y.length && !y[n].includes("DetermineComponentFrameRoot"); )
          n++;
        for (; a < E.length && !E[a].includes(
          "DetermineComponentFrameRoot"
        ); )
          a++;
        if (n === y.length || a === E.length)
          for (n = y.length - 1, a = E.length - 1; 1 <= n && 0 <= a && y[n] !== E[a]; )
            a--;
        for (; 1 <= n && 0 <= a; n--, a--)
          if (y[n] !== E[a]) {
            if (n !== 1 || a !== 1)
              do
                if (n--, a--, 0 > a || y[n] !== E[a]) {
                  var O = `
` + y[n].replace(" at new ", " at ");
                  return t.displayName && O.includes("<anonymous>") && (O = O.replace("<anonymous>", t.displayName)), O;
                }
              while (1 <= n && 0 <= a);
            break;
          }
      }
    } finally {
      Xe = !1, Error.prepareStackTrace = l;
    }
    return (l = t ? t.displayName || t.name : "") ? al(l) : "";
  }
  function Au(t, e) {
    switch (t.tag) {
      case 26:
      case 27:
      case 5:
        return al(t.type);
      case 16:
        return al("Lazy");
      case 13:
        return t.child !== e && e !== null ? al("Suspense Fallback") : al("Suspense");
      case 19:
        return al("SuspenseList");
      case 0:
      case 15:
        return ce(t.type, !1);
      case 11:
        return ce(t.type.render, !1);
      case 1:
        return ce(t.type, !0);
      case 31:
        return al("Activity");
      default:
        return "";
    }
  }
  function fe(t) {
    try {
      var e = "", l = null;
      do
        e += Au(t, l), l = t, t = t.return;
      while (t);
      return e;
    } catch (n) {
      return `
Error generating stack: ` + n.message + `
` + n.stack;
    }
  }
  var un = Object.prototype.hasOwnProperty, Na = i.unstable_scheduleCallback, Oa = i.unstable_cancelCallback, Tu = i.unstable_shouldYield, Dn = i.unstable_requestPaint, Nt = i.unstable_now, zu = i.unstable_getCurrentPriorityLevel, cn = i.unstable_ImmediatePriority, fn = i.unstable_UserBlockingPriority, Un = i.unstable_NormalPriority, wi = i.unstable_LowPriority, jn = i.unstable_IdlePriority, Nl = i.log, Ji = i.unstable_setDisableYieldValue, Ol = null, $t = null;
  function $e(t) {
    if (typeof Nl == "function" && Ji(t), $t && typeof $t.setStrictMode == "function")
      try {
        $t.setStrictMode(Ol, t);
      } catch {
      }
  }
  var te = Math.clz32 ? Math.clz32 : Nu, qu = Math.log, Ki = Math.LN2;
  function Nu(t) {
    return t >>>= 0, t === 0 ? 32 : 31 - (qu(t) / Ki | 0) | 0;
  }
  var Cn = 256, sn = 262144, ul = 4194304;
  function We(t) {
    var e = t & 42;
    if (e !== 0) return e;
    switch (t & -t) {
      case 1:
        return 1;
      case 2:
        return 2;
      case 4:
        return 4;
      case 8:
        return 8;
      case 16:
        return 16;
      case 32:
        return 32;
      case 64:
        return 64;
      case 128:
        return 128;
      case 256:
      case 512:
      case 1024:
      case 2048:
      case 4096:
      case 8192:
      case 16384:
      case 32768:
      case 65536:
      case 131072:
        return t & 261888;
      case 262144:
      case 524288:
      case 1048576:
      case 2097152:
        return t & 3932160;
      case 4194304:
      case 8388608:
      case 16777216:
      case 33554432:
        return t & 62914560;
      case 67108864:
        return 67108864;
      case 134217728:
        return 134217728;
      case 268435456:
        return 268435456;
      case 536870912:
        return 536870912;
      case 1073741824:
        return 0;
      default:
        return t;
    }
  }
  function Ml(t, e, l) {
    var n = t.pendingLanes;
    if (n === 0) return 0;
    var a = 0, u = t.suspendedLanes, c = t.pingedLanes;
    t = t.warmLanes;
    var o = n & 134217727;
    return o !== 0 ? (n = o & ~u, n !== 0 ? a = We(n) : (c &= o, c !== 0 ? a = We(c) : l || (l = o & ~t, l !== 0 && (a = We(l))))) : (o = n & ~u, o !== 0 ? a = We(o) : c !== 0 ? a = We(c) : l || (l = n & ~t, l !== 0 && (a = We(l)))), a === 0 ? 0 : e !== 0 && e !== a && (e & u) === 0 && (u = a & -a, l = e & -e, u >= l || u === 32 && (l & 4194048) !== 0) ? e : a;
  }
  function Fe(t, e) {
    return (t.pendingLanes & ~(t.suspendedLanes & ~t.pingedLanes) & e) === 0;
  }
  function Ou(t, e) {
    switch (t) {
      case 1:
      case 2:
      case 4:
      case 8:
      case 64:
        return e + 250;
      case 16:
      case 32:
      case 128:
      case 256:
      case 512:
      case 1024:
      case 2048:
      case 4096:
      case 8192:
      case 16384:
      case 32768:
      case 65536:
      case 131072:
      case 262144:
      case 524288:
      case 1048576:
      case 2097152:
        return e + 5e3;
      case 4194304:
      case 8388608:
      case 16777216:
      case 33554432:
        return -1;
      case 67108864:
      case 134217728:
      case 268435456:
      case 536870912:
      case 1073741824:
        return -1;
      default:
        return -1;
    }
  }
  function rn() {
    var t = ul;
    return ul <<= 1, (ul & 62914560) === 0 && (ul = 4194304), t;
  }
  function Ma(t) {
    for (var e = [], l = 0; 31 > l; l++) e.push(t);
    return e;
  }
  function R(t, e) {
    t.pendingLanes |= e, e !== 268435456 && (t.suspendedLanes = 0, t.pingedLanes = 0, t.warmLanes = 0);
  }
  function w(t, e, l, n, a, u) {
    var c = t.pendingLanes;
    t.pendingLanes = l, t.suspendedLanes = 0, t.pingedLanes = 0, t.warmLanes = 0, t.expiredLanes &= l, t.entangledLanes &= l, t.errorRecoveryDisabledLanes &= l, t.shellSuspendCounter = 0;
    var o = t.entanglements, y = t.expirationTimes, E = t.hiddenUpdates;
    for (l = c & ~l; 0 < l; ) {
      var O = 31 - te(l), U = 1 << O;
      o[O] = 0, y[O] = -1;
      var x = E[O];
      if (x !== null)
        for (E[O] = null, O = 0; O < x.length; O++) {
          var z = x[O];
          z !== null && (z.lane &= -536870913);
        }
      l &= ~U;
    }
    n !== 0 && mt(t, n, 0), u !== 0 && a === 0 && t.tag !== 0 && (t.suspendedLanes |= u & ~(c & ~e));
  }
  function mt(t, e, l) {
    t.pendingLanes |= e, t.suspendedLanes &= ~e;
    var n = 31 - te(e);
    t.entangledLanes |= e, t.entanglements[n] = t.entanglements[n] | 1073741824 | l & 261930;
  }
  function Dt(t, e) {
    var l = t.entangledLanes |= e;
    for (t = t.entanglements; l; ) {
      var n = 31 - te(l), a = 1 << n;
      a & e | t[n] & e && (t[n] |= e), l &= ~a;
    }
  }
  function Zt(t, e) {
    var l = e & -e;
    return l = (l & 42) !== 0 ? 1 : Rn(l), (l & (t.suspendedLanes | e)) !== 0 ? 0 : l;
  }
  function Rn(t) {
    switch (t) {
      case 2:
        t = 1;
        break;
      case 8:
        t = 4;
        break;
      case 32:
        t = 16;
        break;
      case 256:
      case 512:
      case 1024:
      case 2048:
      case 4096:
      case 8192:
      case 16384:
      case 32768:
      case 65536:
      case 131072:
      case 262144:
      case 524288:
      case 1048576:
      case 2097152:
      case 4194304:
      case 8388608:
      case 16777216:
      case 33554432:
        t = 128;
        break;
      case 268435456:
        t = 134217728;
        break;
      default:
        t = 0;
    }
    return t;
  }
  function ki(t) {
    return t &= -t, 2 < t ? 8 < t ? (t & 134217727) !== 0 ? 32 : 268435456 : 8 : 2;
  }
  function Ns() {
    var t = H.p;
    return t !== 0 ? t : (t = window.event, t === void 0 ? 32 : my(t.type));
  }
  function Os(t, e) {
    var l = H.p;
    try {
      return H.p = t, e();
    } finally {
      H.p = l;
    }
  }
  var Dl = Math.random().toString(36).slice(2), ee = "__reactFiber$" + Dl, ye = "__reactProps$" + Dl, Hn = "__reactContainer$" + Dl, $i = "__reactEvents$" + Dl, wy = "__reactListeners$" + Dl, Jy = "__reactHandles$" + Dl, Ms = "__reactResources$" + Dl, Da = "__reactMarker$" + Dl;
  function Wi(t) {
    delete t[ee], delete t[ye], delete t[$i], delete t[wy], delete t[Jy];
  }
  function Ln(t) {
    var e = t[ee];
    if (e) return e;
    for (var l = t.parentNode; l; ) {
      if (e = l[Hn] || l[ee]) {
        if (l = e.alternate, e.child !== null || l !== null && l.child !== null)
          for (t = Id(t); t !== null; ) {
            if (l = t[ee]) return l;
            t = Id(t);
          }
        return e;
      }
      t = l, l = t.parentNode;
    }
    return null;
  }
  function Bn(t) {
    if (t = t[ee] || t[Hn]) {
      var e = t.tag;
      if (e === 5 || e === 6 || e === 13 || e === 31 || e === 26 || e === 27 || e === 3)
        return t;
    }
    return null;
  }
  function Ua(t) {
    var e = t.tag;
    if (e === 5 || e === 26 || e === 27 || e === 6) return t.stateNode;
    throw Error(r(33));
  }
  function Yn(t) {
    var e = t[Ms];
    return e || (e = t[Ms] = { hoistableStyles: /* @__PURE__ */ new Map(), hoistableScripts: /* @__PURE__ */ new Map() }), e;
  }
  function It(t) {
    t[Da] = !0;
  }
  var Ds = /* @__PURE__ */ new Set(), Us = {};
  function on(t, e) {
    Gn(t, e), Gn(t + "Capture", e);
  }
  function Gn(t, e) {
    for (Us[t] = e, t = 0; t < e.length; t++)
      Ds.add(e[t]);
  }
  var Ky = RegExp(
    "^[:A-Z_a-z\\u00C0-\\u00D6\\u00D8-\\u00F6\\u00F8-\\u02FF\\u0370-\\u037D\\u037F-\\u1FFF\\u200C-\\u200D\\u2070-\\u218F\\u2C00-\\u2FEF\\u3001-\\uD7FF\\uF900-\\uFDCF\\uFDF0-\\uFFFD][:A-Z_a-z\\u00C0-\\u00D6\\u00D8-\\u00F6\\u00F8-\\u02FF\\u0370-\\u037D\\u037F-\\u1FFF\\u200C-\\u200D\\u2070-\\u218F\\u2C00-\\u2FEF\\u3001-\\uD7FF\\uF900-\\uFDCF\\uFDF0-\\uFFFD\\-.0-9\\u00B7\\u0300-\\u036F\\u203F-\\u2040]*$"
  ), js = {}, Cs = {};
  function ky(t) {
    return un.call(Cs, t) ? !0 : un.call(js, t) ? !1 : Ky.test(t) ? Cs[t] = !0 : (js[t] = !0, !1);
  }
  function Mu(t, e, l) {
    if (ky(e))
      if (l === null) t.removeAttribute(e);
      else {
        switch (typeof l) {
          case "undefined":
          case "function":
          case "symbol":
            t.removeAttribute(e);
            return;
          case "boolean":
            var n = e.toLowerCase().slice(0, 5);
            if (n !== "data-" && n !== "aria-") {
              t.removeAttribute(e);
              return;
            }
        }
        t.setAttribute(e, "" + l);
      }
  }
  function Du(t, e, l) {
    if (l === null) t.removeAttribute(e);
    else {
      switch (typeof l) {
        case "undefined":
        case "function":
        case "symbol":
        case "boolean":
          t.removeAttribute(e);
          return;
      }
      t.setAttribute(e, "" + l);
    }
  }
  function il(t, e, l, n) {
    if (n === null) t.removeAttribute(l);
    else {
      switch (typeof n) {
        case "undefined":
        case "function":
        case "symbol":
        case "boolean":
          t.removeAttribute(l);
          return;
      }
      t.setAttributeNS(e, l, "" + n);
    }
  }
  function Me(t) {
    switch (typeof t) {
      case "bigint":
      case "boolean":
      case "number":
      case "string":
      case "undefined":
        return t;
      case "object":
        return t;
      default:
        return "";
    }
  }
  function Rs(t) {
    var e = t.type;
    return (t = t.nodeName) && t.toLowerCase() === "input" && (e === "checkbox" || e === "radio");
  }
  function $y(t, e, l) {
    var n = Object.getOwnPropertyDescriptor(
      t.constructor.prototype,
      e
    );
    if (!t.hasOwnProperty(e) && typeof n < "u" && typeof n.get == "function" && typeof n.set == "function") {
      var a = n.get, u = n.set;
      return Object.defineProperty(t, e, {
        configurable: !0,
        get: function() {
          return a.call(this);
        },
        set: function(c) {
          l = "" + c, u.call(this, c);
        }
      }), Object.defineProperty(t, e, {
        enumerable: n.enumerable
      }), {
        getValue: function() {
          return l;
        },
        setValue: function(c) {
          l = "" + c;
        },
        stopTracking: function() {
          t._valueTracker = null, delete t[e];
        }
      };
    }
  }
  function Fi(t) {
    if (!t._valueTracker) {
      var e = Rs(t) ? "checked" : "value";
      t._valueTracker = $y(
        t,
        e,
        "" + t[e]
      );
    }
  }
  function Hs(t) {
    if (!t) return !1;
    var e = t._valueTracker;
    if (!e) return !0;
    var l = e.getValue(), n = "";
    return t && (n = Rs(t) ? t.checked ? "true" : "false" : t.value), t = n, t !== l ? (e.setValue(t), !0) : !1;
  }
  function Uu(t) {
    if (t = t || (typeof document < "u" ? document : void 0), typeof t > "u") return null;
    try {
      return t.activeElement || t.body;
    } catch {
      return t.body;
    }
  }
  var Wy = /[\n"\\]/g;
  function De(t) {
    return t.replace(
      Wy,
      function(e) {
        return "\\" + e.charCodeAt(0).toString(16) + " ";
      }
    );
  }
  function Ii(t, e, l, n, a, u, c, o) {
    t.name = "", c != null && typeof c != "function" && typeof c != "symbol" && typeof c != "boolean" ? t.type = c : t.removeAttribute("type"), e != null ? c === "number" ? (e === 0 && t.value === "" || t.value != e) && (t.value = "" + Me(e)) : t.value !== "" + Me(e) && (t.value = "" + Me(e)) : c !== "submit" && c !== "reset" || t.removeAttribute("value"), e != null ? Pi(t, c, Me(e)) : l != null ? Pi(t, c, Me(l)) : n != null && t.removeAttribute("value"), a == null && u != null && (t.defaultChecked = !!u), a != null && (t.checked = a && typeof a != "function" && typeof a != "symbol"), o != null && typeof o != "function" && typeof o != "symbol" && typeof o != "boolean" ? t.name = "" + Me(o) : t.removeAttribute("name");
  }
  function Ls(t, e, l, n, a, u, c, o) {
    if (u != null && typeof u != "function" && typeof u != "symbol" && typeof u != "boolean" && (t.type = u), e != null || l != null) {
      if (!(u !== "submit" && u !== "reset" || e != null)) {
        Fi(t);
        return;
      }
      l = l != null ? "" + Me(l) : "", e = e != null ? "" + Me(e) : l, o || e === t.value || (t.value = e), t.defaultValue = e;
    }
    n = n ?? a, n = typeof n != "function" && typeof n != "symbol" && !!n, t.checked = o ? t.checked : !!n, t.defaultChecked = !!n, c != null && typeof c != "function" && typeof c != "symbol" && typeof c != "boolean" && (t.name = c), Fi(t);
  }
  function Pi(t, e, l) {
    e === "number" && Uu(t.ownerDocument) === t || t.defaultValue === "" + l || (t.defaultValue = "" + l);
  }
  function Qn(t, e, l, n) {
    if (t = t.options, e) {
      e = {};
      for (var a = 0; a < l.length; a++)
        e["$" + l[a]] = !0;
      for (l = 0; l < t.length; l++)
        a = e.hasOwnProperty("$" + t[l].value), t[l].selected !== a && (t[l].selected = a), a && n && (t[l].defaultSelected = !0);
    } else {
      for (l = "" + Me(l), e = null, a = 0; a < t.length; a++) {
        if (t[a].value === l) {
          t[a].selected = !0, n && (t[a].defaultSelected = !0);
          return;
        }
        e !== null || t[a].disabled || (e = t[a]);
      }
      e !== null && (e.selected = !0);
    }
  }
  function Bs(t, e, l) {
    if (e != null && (e = "" + Me(e), e !== t.value && (t.value = e), l == null)) {
      t.defaultValue !== e && (t.defaultValue = e);
      return;
    }
    t.defaultValue = l != null ? "" + Me(l) : "";
  }
  function Ys(t, e, l, n) {
    if (e == null) {
      if (n != null) {
        if (l != null) throw Error(r(92));
        if (Qt(n)) {
          if (1 < n.length) throw Error(r(93));
          n = n[0];
        }
        l = n;
      }
      l == null && (l = ""), e = l;
    }
    l = Me(e), t.defaultValue = l, n = t.textContent, n === l && n !== "" && n !== null && (t.value = n), Fi(t);
  }
  function Xn(t, e) {
    if (e) {
      var l = t.firstChild;
      if (l && l === t.lastChild && l.nodeType === 3) {
        l.nodeValue = e;
        return;
      }
    }
    t.textContent = e;
  }
  var Fy = new Set(
    "animationIterationCount aspectRatio borderImageOutset borderImageSlice borderImageWidth boxFlex boxFlexGroup boxOrdinalGroup columnCount columns flex flexGrow flexPositive flexShrink flexNegative flexOrder gridArea gridRow gridRowEnd gridRowSpan gridRowStart gridColumn gridColumnEnd gridColumnSpan gridColumnStart fontWeight lineClamp lineHeight opacity order orphans scale tabSize widows zIndex zoom fillOpacity floodOpacity stopOpacity strokeDasharray strokeDashoffset strokeMiterlimit strokeOpacity strokeWidth MozAnimationIterationCount MozBoxFlex MozBoxFlexGroup MozLineClamp msAnimationIterationCount msFlex msZoom msFlexGrow msFlexNegative msFlexOrder msFlexPositive msFlexShrink msGridColumn msGridColumnSpan msGridRow msGridRowSpan WebkitAnimationIterationCount WebkitBoxFlex WebKitBoxFlexGroup WebkitBoxOrdinalGroup WebkitColumnCount WebkitColumns WebkitFlex WebkitFlexGrow WebkitFlexPositive WebkitFlexShrink WebkitLineClamp".split(
      " "
    )
  );
  function Gs(t, e, l) {
    var n = e.indexOf("--") === 0;
    l == null || typeof l == "boolean" || l === "" ? n ? t.setProperty(e, "") : e === "float" ? t.cssFloat = "" : t[e] = "" : n ? t.setProperty(e, l) : typeof l != "number" || l === 0 || Fy.has(e) ? e === "float" ? t.cssFloat = l : t[e] = ("" + l).trim() : t[e] = l + "px";
  }
  function Qs(t, e, l) {
    if (e != null && typeof e != "object")
      throw Error(r(62));
    if (t = t.style, l != null) {
      for (var n in l)
        !l.hasOwnProperty(n) || e != null && e.hasOwnProperty(n) || (n.indexOf("--") === 0 ? t.setProperty(n, "") : n === "float" ? t.cssFloat = "" : t[n] = "");
      for (var a in e)
        n = e[a], e.hasOwnProperty(a) && l[a] !== n && Gs(t, a, n);
    } else
      for (var u in e)
        e.hasOwnProperty(u) && Gs(t, u, e[u]);
  }
  function tc(t) {
    if (t.indexOf("-") === -1) return !1;
    switch (t) {
      case "annotation-xml":
      case "color-profile":
      case "font-face":
      case "font-face-src":
      case "font-face-uri":
      case "font-face-format":
      case "font-face-name":
      case "missing-glyph":
        return !1;
      default:
        return !0;
    }
  }
  var Iy = /* @__PURE__ */ new Map([
    ["acceptCharset", "accept-charset"],
    ["htmlFor", "for"],
    ["httpEquiv", "http-equiv"],
    ["crossOrigin", "crossorigin"],
    ["accentHeight", "accent-height"],
    ["alignmentBaseline", "alignment-baseline"],
    ["arabicForm", "arabic-form"],
    ["baselineShift", "baseline-shift"],
    ["capHeight", "cap-height"],
    ["clipPath", "clip-path"],
    ["clipRule", "clip-rule"],
    ["colorInterpolation", "color-interpolation"],
    ["colorInterpolationFilters", "color-interpolation-filters"],
    ["colorProfile", "color-profile"],
    ["colorRendering", "color-rendering"],
    ["dominantBaseline", "dominant-baseline"],
    ["enableBackground", "enable-background"],
    ["fillOpacity", "fill-opacity"],
    ["fillRule", "fill-rule"],
    ["floodColor", "flood-color"],
    ["floodOpacity", "flood-opacity"],
    ["fontFamily", "font-family"],
    ["fontSize", "font-size"],
    ["fontSizeAdjust", "font-size-adjust"],
    ["fontStretch", "font-stretch"],
    ["fontStyle", "font-style"],
    ["fontVariant", "font-variant"],
    ["fontWeight", "font-weight"],
    ["glyphName", "glyph-name"],
    ["glyphOrientationHorizontal", "glyph-orientation-horizontal"],
    ["glyphOrientationVertical", "glyph-orientation-vertical"],
    ["horizAdvX", "horiz-adv-x"],
    ["horizOriginX", "horiz-origin-x"],
    ["imageRendering", "image-rendering"],
    ["letterSpacing", "letter-spacing"],
    ["lightingColor", "lighting-color"],
    ["markerEnd", "marker-end"],
    ["markerMid", "marker-mid"],
    ["markerStart", "marker-start"],
    ["overlinePosition", "overline-position"],
    ["overlineThickness", "overline-thickness"],
    ["paintOrder", "paint-order"],
    ["panose-1", "panose-1"],
    ["pointerEvents", "pointer-events"],
    ["renderingIntent", "rendering-intent"],
    ["shapeRendering", "shape-rendering"],
    ["stopColor", "stop-color"],
    ["stopOpacity", "stop-opacity"],
    ["strikethroughPosition", "strikethrough-position"],
    ["strikethroughThickness", "strikethrough-thickness"],
    ["strokeDasharray", "stroke-dasharray"],
    ["strokeDashoffset", "stroke-dashoffset"],
    ["strokeLinecap", "stroke-linecap"],
    ["strokeLinejoin", "stroke-linejoin"],
    ["strokeMiterlimit", "stroke-miterlimit"],
    ["strokeOpacity", "stroke-opacity"],
    ["strokeWidth", "stroke-width"],
    ["textAnchor", "text-anchor"],
    ["textDecoration", "text-decoration"],
    ["textRendering", "text-rendering"],
    ["transformOrigin", "transform-origin"],
    ["underlinePosition", "underline-position"],
    ["underlineThickness", "underline-thickness"],
    ["unicodeBidi", "unicode-bidi"],
    ["unicodeRange", "unicode-range"],
    ["unitsPerEm", "units-per-em"],
    ["vAlphabetic", "v-alphabetic"],
    ["vHanging", "v-hanging"],
    ["vIdeographic", "v-ideographic"],
    ["vMathematical", "v-mathematical"],
    ["vectorEffect", "vector-effect"],
    ["vertAdvY", "vert-adv-y"],
    ["vertOriginX", "vert-origin-x"],
    ["vertOriginY", "vert-origin-y"],
    ["wordSpacing", "word-spacing"],
    ["writingMode", "writing-mode"],
    ["xmlnsXlink", "xmlns:xlink"],
    ["xHeight", "x-height"]
  ]), Py = /^[\u0000-\u001F ]*j[\r\n\t]*a[\r\n\t]*v[\r\n\t]*a[\r\n\t]*s[\r\n\t]*c[\r\n\t]*r[\r\n\t]*i[\r\n\t]*p[\r\n\t]*t[\r\n\t]*:/i;
  function ju(t) {
    return Py.test("" + t) ? "javascript:throw new Error('React has blocked a javascript: URL as a security precaution.')" : t;
  }
  function cl() {
  }
  var ec = null;
  function lc(t) {
    return t = t.target || t.srcElement || window, t.correspondingUseElement && (t = t.correspondingUseElement), t.nodeType === 3 ? t.parentNode : t;
  }
  var Zn = null, Vn = null;
  function Xs(t) {
    var e = Bn(t);
    if (e && (t = e.stateNode)) {
      var l = t[ye] || null;
      t: switch (t = e.stateNode, e.type) {
        case "input":
          if (Ii(
            t,
            l.value,
            l.defaultValue,
            l.defaultValue,
            l.checked,
            l.defaultChecked,
            l.type,
            l.name
          ), e = l.name, l.type === "radio" && e != null) {
            for (l = t; l.parentNode; ) l = l.parentNode;
            for (l = l.querySelectorAll(
              'input[name="' + De(
                "" + e
              ) + '"][type="radio"]'
            ), e = 0; e < l.length; e++) {
              var n = l[e];
              if (n !== t && n.form === t.form) {
                var a = n[ye] || null;
                if (!a) throw Error(r(90));
                Ii(
                  n,
                  a.value,
                  a.defaultValue,
                  a.defaultValue,
                  a.checked,
                  a.defaultChecked,
                  a.type,
                  a.name
                );
              }
            }
            for (e = 0; e < l.length; e++)
              n = l[e], n.form === t.form && Hs(n);
          }
          break t;
        case "textarea":
          Bs(t, l.value, l.defaultValue);
          break t;
        case "select":
          e = l.value, e != null && Qn(t, !!l.multiple, e, !1);
      }
    }
  }
  var nc = !1;
  function Zs(t, e, l) {
    if (nc) return t(e, l);
    nc = !0;
    try {
      var n = t(e);
      return n;
    } finally {
      if (nc = !1, (Zn !== null || Vn !== null) && (Si(), Zn && (e = Zn, t = Vn, Vn = Zn = null, Xs(e), t)))
        for (e = 0; e < t.length; e++) Xs(t[e]);
    }
  }
  function ja(t, e) {
    var l = t.stateNode;
    if (l === null) return null;
    var n = l[ye] || null;
    if (n === null) return null;
    l = n[e];
    t: switch (e) {
      case "onClick":
      case "onClickCapture":
      case "onDoubleClick":
      case "onDoubleClickCapture":
      case "onMouseDown":
      case "onMouseDownCapture":
      case "onMouseMove":
      case "onMouseMoveCapture":
      case "onMouseUp":
      case "onMouseUpCapture":
      case "onMouseEnter":
        (n = !n.disabled) || (t = t.type, n = !(t === "button" || t === "input" || t === "select" || t === "textarea")), t = !n;
        break t;
      default:
        t = !1;
    }
    if (t) return null;
    if (l && typeof l != "function")
      throw Error(
        r(231, e, typeof l)
      );
    return l;
  }
  var fl = !(typeof window > "u" || typeof window.document > "u" || typeof window.document.createElement > "u"), ac = !1;
  if (fl)
    try {
      var Ca = {};
      Object.defineProperty(Ca, "passive", {
        get: function() {
          ac = !0;
        }
      }), window.addEventListener("test", Ca, Ca), window.removeEventListener("test", Ca, Ca);
    } catch {
      ac = !1;
    }
  var Ul = null, uc = null, Cu = null;
  function Vs() {
    if (Cu) return Cu;
    var t, e = uc, l = e.length, n, a = "value" in Ul ? Ul.value : Ul.textContent, u = a.length;
    for (t = 0; t < l && e[t] === a[t]; t++) ;
    var c = l - t;
    for (n = 1; n <= c && e[l - n] === a[u - n]; n++) ;
    return Cu = a.slice(t, 1 < n ? 1 - n : void 0);
  }
  function Ru(t) {
    var e = t.keyCode;
    return "charCode" in t ? (t = t.charCode, t === 0 && e === 13 && (t = 13)) : t = e, t === 10 && (t = 13), 32 <= t || t === 13 ? t : 0;
  }
  function Hu() {
    return !0;
  }
  function ws() {
    return !1;
  }
  function me(t) {
    function e(l, n, a, u, c) {
      this._reactName = l, this._targetInst = a, this.type = n, this.nativeEvent = u, this.target = c, this.currentTarget = null;
      for (var o in t)
        t.hasOwnProperty(o) && (l = t[o], this[o] = l ? l(u) : u[o]);
      return this.isDefaultPrevented = (u.defaultPrevented != null ? u.defaultPrevented : u.returnValue === !1) ? Hu : ws, this.isPropagationStopped = ws, this;
    }
    return S(e.prototype, {
      preventDefault: function() {
        this.defaultPrevented = !0;
        var l = this.nativeEvent;
        l && (l.preventDefault ? l.preventDefault() : typeof l.returnValue != "unknown" && (l.returnValue = !1), this.isDefaultPrevented = Hu);
      },
      stopPropagation: function() {
        var l = this.nativeEvent;
        l && (l.stopPropagation ? l.stopPropagation() : typeof l.cancelBubble != "unknown" && (l.cancelBubble = !0), this.isPropagationStopped = Hu);
      },
      persist: function() {
      },
      isPersistent: Hu
    }), e;
  }
  var dn = {
    eventPhase: 0,
    bubbles: 0,
    cancelable: 0,
    timeStamp: function(t) {
      return t.timeStamp || Date.now();
    },
    defaultPrevented: 0,
    isTrusted: 0
  }, Lu = me(dn), Ra = S({}, dn, { view: 0, detail: 0 }), tm = me(Ra), ic, cc, Ha, Bu = S({}, Ra, {
    screenX: 0,
    screenY: 0,
    clientX: 0,
    clientY: 0,
    pageX: 0,
    pageY: 0,
    ctrlKey: 0,
    shiftKey: 0,
    altKey: 0,
    metaKey: 0,
    getModifierState: sc,
    button: 0,
    buttons: 0,
    relatedTarget: function(t) {
      return t.relatedTarget === void 0 ? t.fromElement === t.srcElement ? t.toElement : t.fromElement : t.relatedTarget;
    },
    movementX: function(t) {
      return "movementX" in t ? t.movementX : (t !== Ha && (Ha && t.type === "mousemove" ? (ic = t.screenX - Ha.screenX, cc = t.screenY - Ha.screenY) : cc = ic = 0, Ha = t), ic);
    },
    movementY: function(t) {
      return "movementY" in t ? t.movementY : cc;
    }
  }), Js = me(Bu), em = S({}, Bu, { dataTransfer: 0 }), lm = me(em), nm = S({}, Ra, { relatedTarget: 0 }), fc = me(nm), am = S({}, dn, {
    animationName: 0,
    elapsedTime: 0,
    pseudoElement: 0
  }), um = me(am), im = S({}, dn, {
    clipboardData: function(t) {
      return "clipboardData" in t ? t.clipboardData : window.clipboardData;
    }
  }), cm = me(im), fm = S({}, dn, { data: 0 }), Ks = me(fm), sm = {
    Esc: "Escape",
    Spacebar: " ",
    Left: "ArrowLeft",
    Up: "ArrowUp",
    Right: "ArrowRight",
    Down: "ArrowDown",
    Del: "Delete",
    Win: "OS",
    Menu: "ContextMenu",
    Apps: "ContextMenu",
    Scroll: "ScrollLock",
    MozPrintableKey: "Unidentified"
  }, rm = {
    8: "Backspace",
    9: "Tab",
    12: "Clear",
    13: "Enter",
    16: "Shift",
    17: "Control",
    18: "Alt",
    19: "Pause",
    20: "CapsLock",
    27: "Escape",
    32: " ",
    33: "PageUp",
    34: "PageDown",
    35: "End",
    36: "Home",
    37: "ArrowLeft",
    38: "ArrowUp",
    39: "ArrowRight",
    40: "ArrowDown",
    45: "Insert",
    46: "Delete",
    112: "F1",
    113: "F2",
    114: "F3",
    115: "F4",
    116: "F5",
    117: "F6",
    118: "F7",
    119: "F8",
    120: "F9",
    121: "F10",
    122: "F11",
    123: "F12",
    144: "NumLock",
    145: "ScrollLock",
    224: "Meta"
  }, om = {
    Alt: "altKey",
    Control: "ctrlKey",
    Meta: "metaKey",
    Shift: "shiftKey"
  };
  function dm(t) {
    var e = this.nativeEvent;
    return e.getModifierState ? e.getModifierState(t) : (t = om[t]) ? !!e[t] : !1;
  }
  function sc() {
    return dm;
  }
  var ym = S({}, Ra, {
    key: function(t) {
      if (t.key) {
        var e = sm[t.key] || t.key;
        if (e !== "Unidentified") return e;
      }
      return t.type === "keypress" ? (t = Ru(t), t === 13 ? "Enter" : String.fromCharCode(t)) : t.type === "keydown" || t.type === "keyup" ? rm[t.keyCode] || "Unidentified" : "";
    },
    code: 0,
    location: 0,
    ctrlKey: 0,
    shiftKey: 0,
    altKey: 0,
    metaKey: 0,
    repeat: 0,
    locale: 0,
    getModifierState: sc,
    charCode: function(t) {
      return t.type === "keypress" ? Ru(t) : 0;
    },
    keyCode: function(t) {
      return t.type === "keydown" || t.type === "keyup" ? t.keyCode : 0;
    },
    which: function(t) {
      return t.type === "keypress" ? Ru(t) : t.type === "keydown" || t.type === "keyup" ? t.keyCode : 0;
    }
  }), mm = me(ym), hm = S({}, Bu, {
    pointerId: 0,
    width: 0,
    height: 0,
    pressure: 0,
    tangentialPressure: 0,
    tiltX: 0,
    tiltY: 0,
    twist: 0,
    pointerType: 0,
    isPrimary: 0
  }), ks = me(hm), vm = S({}, Ra, {
    touches: 0,
    targetTouches: 0,
    changedTouches: 0,
    altKey: 0,
    metaKey: 0,
    ctrlKey: 0,
    shiftKey: 0,
    getModifierState: sc
  }), gm = me(vm), pm = S({}, dn, {
    propertyName: 0,
    elapsedTime: 0,
    pseudoElement: 0
  }), bm = me(pm), Sm = S({}, Bu, {
    deltaX: function(t) {
      return "deltaX" in t ? t.deltaX : "wheelDeltaX" in t ? -t.wheelDeltaX : 0;
    },
    deltaY: function(t) {
      return "deltaY" in t ? t.deltaY : "wheelDeltaY" in t ? -t.wheelDeltaY : "wheelDelta" in t ? -t.wheelDelta : 0;
    },
    deltaZ: 0,
    deltaMode: 0
  }), _m = me(Sm), Em = S({}, dn, {
    newState: 0,
    oldState: 0
  }), xm = me(Em), Am = [9, 13, 27, 32], rc = fl && "CompositionEvent" in window, La = null;
  fl && "documentMode" in document && (La = document.documentMode);
  var Tm = fl && "TextEvent" in window && !La, $s = fl && (!rc || La && 8 < La && 11 >= La), Ws = " ", Fs = !1;
  function Is(t, e) {
    switch (t) {
      case "keyup":
        return Am.indexOf(e.keyCode) !== -1;
      case "keydown":
        return e.keyCode !== 229;
      case "keypress":
      case "mousedown":
      case "focusout":
        return !0;
      default:
        return !1;
    }
  }
  function Ps(t) {
    return t = t.detail, typeof t == "object" && "data" in t ? t.data : null;
  }
  var wn = !1;
  function zm(t, e) {
    switch (t) {
      case "compositionend":
        return Ps(e);
      case "keypress":
        return e.which !== 32 ? null : (Fs = !0, Ws);
      case "textInput":
        return t = e.data, t === Ws && Fs ? null : t;
      default:
        return null;
    }
  }
  function qm(t, e) {
    if (wn)
      return t === "compositionend" || !rc && Is(t, e) ? (t = Vs(), Cu = uc = Ul = null, wn = !1, t) : null;
    switch (t) {
      case "paste":
        return null;
      case "keypress":
        if (!(e.ctrlKey || e.altKey || e.metaKey) || e.ctrlKey && e.altKey) {
          if (e.char && 1 < e.char.length)
            return e.char;
          if (e.which) return String.fromCharCode(e.which);
        }
        return null;
      case "compositionend":
        return $s && e.locale !== "ko" ? null : e.data;
      default:
        return null;
    }
  }
  var Nm = {
    color: !0,
    date: !0,
    datetime: !0,
    "datetime-local": !0,
    email: !0,
    month: !0,
    number: !0,
    password: !0,
    range: !0,
    search: !0,
    tel: !0,
    text: !0,
    time: !0,
    url: !0,
    week: !0
  };
  function tr(t) {
    var e = t && t.nodeName && t.nodeName.toLowerCase();
    return e === "input" ? !!Nm[t.type] : e === "textarea";
  }
  function er(t, e, l, n) {
    Zn ? Vn ? Vn.push(n) : Vn = [n] : Zn = n, e = qi(e, "onChange"), 0 < e.length && (l = new Lu(
      "onChange",
      "change",
      null,
      l,
      n
    ), t.push({ event: l, listeners: e }));
  }
  var Ba = null, Ya = null;
  function Om(t) {
    Bd(t, 0);
  }
  function Yu(t) {
    var e = Ua(t);
    if (Hs(e)) return t;
  }
  function lr(t, e) {
    if (t === "change") return e;
  }
  var nr = !1;
  if (fl) {
    var oc;
    if (fl) {
      var dc = "oninput" in document;
      if (!dc) {
        var ar = document.createElement("div");
        ar.setAttribute("oninput", "return;"), dc = typeof ar.oninput == "function";
      }
      oc = dc;
    } else oc = !1;
    nr = oc && (!document.documentMode || 9 < document.documentMode);
  }
  function ur() {
    Ba && (Ba.detachEvent("onpropertychange", ir), Ya = Ba = null);
  }
  function ir(t) {
    if (t.propertyName === "value" && Yu(Ya)) {
      var e = [];
      er(
        e,
        Ya,
        t,
        lc(t)
      ), Zs(Om, e);
    }
  }
  function Mm(t, e, l) {
    t === "focusin" ? (ur(), Ba = e, Ya = l, Ba.attachEvent("onpropertychange", ir)) : t === "focusout" && ur();
  }
  function Dm(t) {
    if (t === "selectionchange" || t === "keyup" || t === "keydown")
      return Yu(Ya);
  }
  function Um(t, e) {
    if (t === "click") return Yu(e);
  }
  function jm(t, e) {
    if (t === "input" || t === "change")
      return Yu(e);
  }
  function Cm(t, e) {
    return t === e && (t !== 0 || 1 / t === 1 / e) || t !== t && e !== e;
  }
  var Ee = typeof Object.is == "function" ? Object.is : Cm;
  function Ga(t, e) {
    if (Ee(t, e)) return !0;
    if (typeof t != "object" || t === null || typeof e != "object" || e === null)
      return !1;
    var l = Object.keys(t), n = Object.keys(e);
    if (l.length !== n.length) return !1;
    for (n = 0; n < l.length; n++) {
      var a = l[n];
      if (!un.call(e, a) || !Ee(t[a], e[a]))
        return !1;
    }
    return !0;
  }
  function cr(t) {
    for (; t && t.firstChild; ) t = t.firstChild;
    return t;
  }
  function fr(t, e) {
    var l = cr(t);
    t = 0;
    for (var n; l; ) {
      if (l.nodeType === 3) {
        if (n = t + l.textContent.length, t <= e && n >= e)
          return { node: l, offset: e - t };
        t = n;
      }
      t: {
        for (; l; ) {
          if (l.nextSibling) {
            l = l.nextSibling;
            break t;
          }
          l = l.parentNode;
        }
        l = void 0;
      }
      l = cr(l);
    }
  }
  function sr(t, e) {
    return t && e ? t === e ? !0 : t && t.nodeType === 3 ? !1 : e && e.nodeType === 3 ? sr(t, e.parentNode) : "contains" in t ? t.contains(e) : t.compareDocumentPosition ? !!(t.compareDocumentPosition(e) & 16) : !1 : !1;
  }
  function rr(t) {
    t = t != null && t.ownerDocument != null && t.ownerDocument.defaultView != null ? t.ownerDocument.defaultView : window;
    for (var e = Uu(t.document); e instanceof t.HTMLIFrameElement; ) {
      try {
        var l = typeof e.contentWindow.location.href == "string";
      } catch {
        l = !1;
      }
      if (l) t = e.contentWindow;
      else break;
      e = Uu(t.document);
    }
    return e;
  }
  function yc(t) {
    var e = t && t.nodeName && t.nodeName.toLowerCase();
    return e && (e === "input" && (t.type === "text" || t.type === "search" || t.type === "tel" || t.type === "url" || t.type === "password") || e === "textarea" || t.contentEditable === "true");
  }
  var Rm = fl && "documentMode" in document && 11 >= document.documentMode, Jn = null, mc = null, Qa = null, hc = !1;
  function or(t, e, l) {
    var n = l.window === l ? l.document : l.nodeType === 9 ? l : l.ownerDocument;
    hc || Jn == null || Jn !== Uu(n) || (n = Jn, "selectionStart" in n && yc(n) ? n = { start: n.selectionStart, end: n.selectionEnd } : (n = (n.ownerDocument && n.ownerDocument.defaultView || window).getSelection(), n = {
      anchorNode: n.anchorNode,
      anchorOffset: n.anchorOffset,
      focusNode: n.focusNode,
      focusOffset: n.focusOffset
    }), Qa && Ga(Qa, n) || (Qa = n, n = qi(mc, "onSelect"), 0 < n.length && (e = new Lu(
      "onSelect",
      "select",
      null,
      e,
      l
    ), t.push({ event: e, listeners: n }), e.target = Jn)));
  }
  function yn(t, e) {
    var l = {};
    return l[t.toLowerCase()] = e.toLowerCase(), l["Webkit" + t] = "webkit" + e, l["Moz" + t] = "moz" + e, l;
  }
  var Kn = {
    animationend: yn("Animation", "AnimationEnd"),
    animationiteration: yn("Animation", "AnimationIteration"),
    animationstart: yn("Animation", "AnimationStart"),
    transitionrun: yn("Transition", "TransitionRun"),
    transitionstart: yn("Transition", "TransitionStart"),
    transitioncancel: yn("Transition", "TransitionCancel"),
    transitionend: yn("Transition", "TransitionEnd")
  }, vc = {}, dr = {};
  fl && (dr = document.createElement("div").style, "AnimationEvent" in window || (delete Kn.animationend.animation, delete Kn.animationiteration.animation, delete Kn.animationstart.animation), "TransitionEvent" in window || delete Kn.transitionend.transition);
  function mn(t) {
    if (vc[t]) return vc[t];
    if (!Kn[t]) return t;
    var e = Kn[t], l;
    for (l in e)
      if (e.hasOwnProperty(l) && l in dr)
        return vc[t] = e[l];
    return t;
  }
  var yr = mn("animationend"), mr = mn("animationiteration"), hr = mn("animationstart"), Hm = mn("transitionrun"), Lm = mn("transitionstart"), Bm = mn("transitioncancel"), vr = mn("transitionend"), gr = /* @__PURE__ */ new Map(), gc = "abort auxClick beforeToggle cancel canPlay canPlayThrough click close contextMenu copy cut drag dragEnd dragEnter dragExit dragLeave dragOver dragStart drop durationChange emptied encrypted ended error gotPointerCapture input invalid keyDown keyPress keyUp load loadedData loadedMetadata loadStart lostPointerCapture mouseDown mouseMove mouseOut mouseOver mouseUp paste pause play playing pointerCancel pointerDown pointerMove pointerOut pointerOver pointerUp progress rateChange reset resize seeked seeking stalled submit suspend timeUpdate touchCancel touchEnd touchStart volumeChange scroll toggle touchMove waiting wheel".split(
    " "
  );
  gc.push("scrollEnd");
  function Ze(t, e) {
    gr.set(t, e), on(e, [t]);
  }
  var Gu = typeof reportError == "function" ? reportError : function(t) {
    if (typeof window == "object" && typeof window.ErrorEvent == "function") {
      var e = new window.ErrorEvent("error", {
        bubbles: !0,
        cancelable: !0,
        message: typeof t == "object" && t !== null && typeof t.message == "string" ? String(t.message) : String(t),
        error: t
      });
      if (!window.dispatchEvent(e)) return;
    } else if (typeof process == "object" && typeof process.emit == "function") {
      process.emit("uncaughtException", t);
      return;
    }
    console.error(t);
  }, Ue = [], kn = 0, pc = 0;
  function Qu() {
    for (var t = kn, e = pc = kn = 0; e < t; ) {
      var l = Ue[e];
      Ue[e++] = null;
      var n = Ue[e];
      Ue[e++] = null;
      var a = Ue[e];
      Ue[e++] = null;
      var u = Ue[e];
      if (Ue[e++] = null, n !== null && a !== null) {
        var c = n.pending;
        c === null ? a.next = a : (a.next = c.next, c.next = a), n.pending = a;
      }
      u !== 0 && pr(l, a, u);
    }
  }
  function Xu(t, e, l, n) {
    Ue[kn++] = t, Ue[kn++] = e, Ue[kn++] = l, Ue[kn++] = n, pc |= n, t.lanes |= n, t = t.alternate, t !== null && (t.lanes |= n);
  }
  function bc(t, e, l, n) {
    return Xu(t, e, l, n), Zu(t);
  }
  function hn(t, e) {
    return Xu(t, null, null, e), Zu(t);
  }
  function pr(t, e, l) {
    t.lanes |= l;
    var n = t.alternate;
    n !== null && (n.lanes |= l);
    for (var a = !1, u = t.return; u !== null; )
      u.childLanes |= l, n = u.alternate, n !== null && (n.childLanes |= l), u.tag === 22 && (t = u.stateNode, t === null || t._visibility & 1 || (a = !0)), t = u, u = u.return;
    return t.tag === 3 ? (u = t.stateNode, a && e !== null && (a = 31 - te(l), t = u.hiddenUpdates, n = t[a], n === null ? t[a] = [e] : n.push(e), e.lane = l | 536870912), u) : null;
  }
  function Zu(t) {
    if (50 < su)
      throw su = 0, Of = null, Error(r(185));
    for (var e = t.return; e !== null; )
      t = e, e = t.return;
    return t.tag === 3 ? t.stateNode : null;
  }
  var $n = {};
  function Ym(t, e, l, n) {
    this.tag = t, this.key = l, this.sibling = this.child = this.return = this.stateNode = this.type = this.elementType = null, this.index = 0, this.refCleanup = this.ref = null, this.pendingProps = e, this.dependencies = this.memoizedState = this.updateQueue = this.memoizedProps = null, this.mode = n, this.subtreeFlags = this.flags = 0, this.deletions = null, this.childLanes = this.lanes = 0, this.alternate = null;
  }
  function xe(t, e, l, n) {
    return new Ym(t, e, l, n);
  }
  function Sc(t) {
    return t = t.prototype, !(!t || !t.isReactComponent);
  }
  function sl(t, e) {
    var l = t.alternate;
    return l === null ? (l = xe(
      t.tag,
      e,
      t.key,
      t.mode
    ), l.elementType = t.elementType, l.type = t.type, l.stateNode = t.stateNode, l.alternate = t, t.alternate = l) : (l.pendingProps = e, l.type = t.type, l.flags = 0, l.subtreeFlags = 0, l.deletions = null), l.flags = t.flags & 65011712, l.childLanes = t.childLanes, l.lanes = t.lanes, l.child = t.child, l.memoizedProps = t.memoizedProps, l.memoizedState = t.memoizedState, l.updateQueue = t.updateQueue, e = t.dependencies, l.dependencies = e === null ? null : { lanes: e.lanes, firstContext: e.firstContext }, l.sibling = t.sibling, l.index = t.index, l.ref = t.ref, l.refCleanup = t.refCleanup, l;
  }
  function br(t, e) {
    t.flags &= 65011714;
    var l = t.alternate;
    return l === null ? (t.childLanes = 0, t.lanes = e, t.child = null, t.subtreeFlags = 0, t.memoizedProps = null, t.memoizedState = null, t.updateQueue = null, t.dependencies = null, t.stateNode = null) : (t.childLanes = l.childLanes, t.lanes = l.lanes, t.child = l.child, t.subtreeFlags = 0, t.deletions = null, t.memoizedProps = l.memoizedProps, t.memoizedState = l.memoizedState, t.updateQueue = l.updateQueue, t.type = l.type, e = l.dependencies, t.dependencies = e === null ? null : {
      lanes: e.lanes,
      firstContext: e.firstContext
    }), t;
  }
  function Vu(t, e, l, n, a, u) {
    var c = 0;
    if (n = t, typeof t == "function") Sc(t) && (c = 1);
    else if (typeof t == "string")
      c = Vh(
        t,
        l,
        Q.current
      ) ? 26 : t === "html" || t === "head" || t === "body" ? 27 : 5;
    else
      t: switch (t) {
        case _e:
          return t = xe(31, l, e, a), t.elementType = _e, t.lanes = u, t;
        case P:
          return vn(l.children, a, u, e);
        case B:
          c = 8, a |= 24;
          break;
        case W:
          return t = xe(12, l, e, a | 2), t.elementType = W, t.lanes = u, t;
        case Ft:
          return t = xe(13, l, e, a), t.elementType = Ft, t.lanes = u, t;
        case Lt:
          return t = xe(19, l, e, a), t.elementType = Lt, t.lanes = u, t;
        default:
          if (typeof t == "object" && t !== null)
            switch (t.$$typeof) {
              case I:
                c = 10;
                break t;
              case it:
                c = 9;
                break t;
              case ct:
                c = 11;
                break t;
              case nt:
                c = 14;
                break t;
              case _t:
                c = 16, n = null;
                break t;
            }
          c = 29, l = Error(
            r(130, t === null ? "null" : typeof t, "")
          ), n = null;
      }
    return e = xe(c, l, e, a), e.elementType = t, e.type = n, e.lanes = u, e;
  }
  function vn(t, e, l, n) {
    return t = xe(7, t, n, e), t.lanes = l, t;
  }
  function _c(t, e, l) {
    return t = xe(6, t, null, e), t.lanes = l, t;
  }
  function Sr(t) {
    var e = xe(18, null, null, 0);
    return e.stateNode = t, e;
  }
  function Ec(t, e, l) {
    return e = xe(
      4,
      t.children !== null ? t.children : [],
      t.key,
      e
    ), e.lanes = l, e.stateNode = {
      containerInfo: t.containerInfo,
      pendingChildren: null,
      implementation: t.implementation
    }, e;
  }
  var _r = /* @__PURE__ */ new WeakMap();
  function je(t, e) {
    if (typeof t == "object" && t !== null) {
      var l = _r.get(t);
      return l !== void 0 ? l : (e = {
        value: t,
        source: e,
        stack: fe(e)
      }, _r.set(t, e), e);
    }
    return {
      value: t,
      source: e,
      stack: fe(e)
    };
  }
  var Wn = [], Fn = 0, wu = null, Xa = 0, Ce = [], Re = 0, jl = null, Ie = 1, Pe = "";
  function rl(t, e) {
    Wn[Fn++] = Xa, Wn[Fn++] = wu, wu = t, Xa = e;
  }
  function Er(t, e, l) {
    Ce[Re++] = Ie, Ce[Re++] = Pe, Ce[Re++] = jl, jl = t;
    var n = Ie;
    t = Pe;
    var a = 32 - te(n) - 1;
    n &= ~(1 << a), l += 1;
    var u = 32 - te(e) + a;
    if (30 < u) {
      var c = a - a % 5;
      u = (n & (1 << c) - 1).toString(32), n >>= c, a -= c, Ie = 1 << 32 - te(e) + a | l << a | n, Pe = u + t;
    } else
      Ie = 1 << u | l << a | n, Pe = t;
  }
  function xc(t) {
    t.return !== null && (rl(t, 1), Er(t, 1, 0));
  }
  function Ac(t) {
    for (; t === wu; )
      wu = Wn[--Fn], Wn[Fn] = null, Xa = Wn[--Fn], Wn[Fn] = null;
    for (; t === jl; )
      jl = Ce[--Re], Ce[Re] = null, Pe = Ce[--Re], Ce[Re] = null, Ie = Ce[--Re], Ce[Re] = null;
  }
  function xr(t, e) {
    Ce[Re++] = Ie, Ce[Re++] = Pe, Ce[Re++] = jl, Ie = e.id, Pe = e.overflow, jl = t;
  }
  var le = null, Ot = null, dt = !1, Cl = null, He = !1, Tc = Error(r(519));
  function Rl(t) {
    var e = Error(
      r(
        418,
        1 < arguments.length && arguments[1] !== void 0 && arguments[1] ? "text" : "HTML",
        ""
      )
    );
    throw Za(je(e, t)), Tc;
  }
  function Ar(t) {
    var e = t.stateNode, l = t.type, n = t.memoizedProps;
    switch (e[ee] = t, e[ye] = n, l) {
      case "dialog":
        st("cancel", e), st("close", e);
        break;
      case "iframe":
      case "object":
      case "embed":
        st("load", e);
        break;
      case "video":
      case "audio":
        for (l = 0; l < ou.length; l++)
          st(ou[l], e);
        break;
      case "source":
        st("error", e);
        break;
      case "img":
      case "image":
      case "link":
        st("error", e), st("load", e);
        break;
      case "details":
        st("toggle", e);
        break;
      case "input":
        st("invalid", e), Ls(
          e,
          n.value,
          n.defaultValue,
          n.checked,
          n.defaultChecked,
          n.type,
          n.name,
          !0
        );
        break;
      case "select":
        st("invalid", e);
        break;
      case "textarea":
        st("invalid", e), Ys(e, n.value, n.defaultValue, n.children);
    }
    l = n.children, typeof l != "string" && typeof l != "number" && typeof l != "bigint" || e.textContent === "" + l || n.suppressHydrationWarning === !0 || Xd(e.textContent, l) ? (n.popover != null && (st("beforetoggle", e), st("toggle", e)), n.onScroll != null && st("scroll", e), n.onScrollEnd != null && st("scrollend", e), n.onClick != null && (e.onclick = cl), e = !0) : e = !1, e || Rl(t, !0);
  }
  function Tr(t) {
    for (le = t.return; le; )
      switch (le.tag) {
        case 5:
        case 31:
        case 13:
          He = !1;
          return;
        case 27:
        case 3:
          He = !0;
          return;
        default:
          le = le.return;
      }
  }
  function In(t) {
    if (t !== le) return !1;
    if (!dt) return Tr(t), dt = !0, !1;
    var e = t.tag, l;
    if ((l = e !== 3 && e !== 27) && ((l = e === 5) && (l = t.type, l = !(l !== "form" && l !== "button") || Vf(t.type, t.memoizedProps)), l = !l), l && Ot && Rl(t), Tr(t), e === 13) {
      if (t = t.memoizedState, t = t !== null ? t.dehydrated : null, !t) throw Error(r(317));
      Ot = Fd(t);
    } else if (e === 31) {
      if (t = t.memoizedState, t = t !== null ? t.dehydrated : null, !t) throw Error(r(317));
      Ot = Fd(t);
    } else
      e === 27 ? (e = Ot, $l(t.type) ? (t = $f, $f = null, Ot = t) : Ot = e) : Ot = le ? Be(t.stateNode.nextSibling) : null;
    return !0;
  }
  function gn() {
    Ot = le = null, dt = !1;
  }
  function zc() {
    var t = Cl;
    return t !== null && (pe === null ? pe = t : pe.push.apply(
      pe,
      t
    ), Cl = null), t;
  }
  function Za(t) {
    Cl === null ? Cl = [t] : Cl.push(t);
  }
  var qc = h(null), pn = null, ol = null;
  function Hl(t, e, l) {
    L(qc, e._currentValue), e._currentValue = l;
  }
  function dl(t) {
    t._currentValue = qc.current, D(qc);
  }
  function Nc(t, e, l) {
    for (; t !== null; ) {
      var n = t.alternate;
      if ((t.childLanes & e) !== e ? (t.childLanes |= e, n !== null && (n.childLanes |= e)) : n !== null && (n.childLanes & e) !== e && (n.childLanes |= e), t === l) break;
      t = t.return;
    }
  }
  function Oc(t, e, l, n) {
    var a = t.child;
    for (a !== null && (a.return = t); a !== null; ) {
      var u = a.dependencies;
      if (u !== null) {
        var c = a.child;
        u = u.firstContext;
        t: for (; u !== null; ) {
          var o = u;
          u = a;
          for (var y = 0; y < e.length; y++)
            if (o.context === e[y]) {
              u.lanes |= l, o = u.alternate, o !== null && (o.lanes |= l), Nc(
                u.return,
                l,
                t
              ), n || (c = null);
              break t;
            }
          u = o.next;
        }
      } else if (a.tag === 18) {
        if (c = a.return, c === null) throw Error(r(341));
        c.lanes |= l, u = c.alternate, u !== null && (u.lanes |= l), Nc(c, l, t), c = null;
      } else c = a.child;
      if (c !== null) c.return = a;
      else
        for (c = a; c !== null; ) {
          if (c === t) {
            c = null;
            break;
          }
          if (a = c.sibling, a !== null) {
            a.return = c.return, c = a;
            break;
          }
          c = c.return;
        }
      a = c;
    }
  }
  function Pn(t, e, l, n) {
    t = null;
    for (var a = e, u = !1; a !== null; ) {
      if (!u) {
        if ((a.flags & 524288) !== 0) u = !0;
        else if ((a.flags & 262144) !== 0) break;
      }
      if (a.tag === 10) {
        var c = a.alternate;
        if (c === null) throw Error(r(387));
        if (c = c.memoizedProps, c !== null) {
          var o = a.type;
          Ee(a.pendingProps.value, c.value) || (t !== null ? t.push(o) : t = [o]);
        }
      } else if (a === vt.current) {
        if (c = a.alternate, c === null) throw Error(r(387));
        c.memoizedState.memoizedState !== a.memoizedState.memoizedState && (t !== null ? t.push(vu) : t = [vu]);
      }
      a = a.return;
    }
    t !== null && Oc(
      e,
      t,
      l,
      n
    ), e.flags |= 262144;
  }
  function Ju(t) {
    for (t = t.firstContext; t !== null; ) {
      if (!Ee(
        t.context._currentValue,
        t.memoizedValue
      ))
        return !0;
      t = t.next;
    }
    return !1;
  }
  function bn(t) {
    pn = t, ol = null, t = t.dependencies, t !== null && (t.firstContext = null);
  }
  function ne(t) {
    return zr(pn, t);
  }
  function Ku(t, e) {
    return pn === null && bn(t), zr(t, e);
  }
  function zr(t, e) {
    var l = e._currentValue;
    if (e = { context: e, memoizedValue: l, next: null }, ol === null) {
      if (t === null) throw Error(r(308));
      ol = e, t.dependencies = { lanes: 0, firstContext: e }, t.flags |= 524288;
    } else ol = ol.next = e;
    return l;
  }
  var Gm = typeof AbortController < "u" ? AbortController : function() {
    var t = [], e = this.signal = {
      aborted: !1,
      addEventListener: function(l, n) {
        t.push(n);
      }
    };
    this.abort = function() {
      e.aborted = !0, t.forEach(function(l) {
        return l();
      });
    };
  }, Qm = i.unstable_scheduleCallback, Xm = i.unstable_NormalPriority, Vt = {
    $$typeof: I,
    Consumer: null,
    Provider: null,
    _currentValue: null,
    _currentValue2: null,
    _threadCount: 0
  };
  function Mc() {
    return {
      controller: new Gm(),
      data: /* @__PURE__ */ new Map(),
      refCount: 0
    };
  }
  function Va(t) {
    t.refCount--, t.refCount === 0 && Qm(Xm, function() {
      t.controller.abort();
    });
  }
  var wa = null, Dc = 0, ta = 0, ea = null;
  function Zm(t, e) {
    if (wa === null) {
      var l = wa = [];
      Dc = 0, ta = Rf(), ea = {
        status: "pending",
        value: void 0,
        then: function(n) {
          l.push(n);
        }
      };
    }
    return Dc++, e.then(qr, qr), e;
  }
  function qr() {
    if (--Dc === 0 && wa !== null) {
      ea !== null && (ea.status = "fulfilled");
      var t = wa;
      wa = null, ta = 0, ea = null;
      for (var e = 0; e < t.length; e++) (0, t[e])();
    }
  }
  function Vm(t, e) {
    var l = [], n = {
      status: "pending",
      value: null,
      reason: null,
      then: function(a) {
        l.push(a);
      }
    };
    return t.then(
      function() {
        n.status = "fulfilled", n.value = e;
        for (var a = 0; a < l.length; a++) (0, l[a])(e);
      },
      function(a) {
        for (n.status = "rejected", n.reason = a, a = 0; a < l.length; a++)
          (0, l[a])(void 0);
      }
    ), n;
  }
  var Nr = N.S;
  N.S = function(t, e) {
    dd = Nt(), typeof e == "object" && e !== null && typeof e.then == "function" && Zm(t, e), Nr !== null && Nr(t, e);
  };
  var Sn = h(null);
  function Uc() {
    var t = Sn.current;
    return t !== null ? t : qt.pooledCache;
  }
  function ku(t, e) {
    e === null ? L(Sn, Sn.current) : L(Sn, e.pool);
  }
  function Or() {
    var t = Uc();
    return t === null ? null : { parent: Vt._currentValue, pool: t };
  }
  var la = Error(r(460)), jc = Error(r(474)), $u = Error(r(542)), Wu = { then: function() {
  } };
  function Mr(t) {
    return t = t.status, t === "fulfilled" || t === "rejected";
  }
  function Dr(t, e, l) {
    switch (l = t[l], l === void 0 ? t.push(e) : l !== e && (e.then(cl, cl), e = l), e.status) {
      case "fulfilled":
        return e.value;
      case "rejected":
        throw t = e.reason, jr(t), t;
      default:
        if (typeof e.status == "string") e.then(cl, cl);
        else {
          if (t = qt, t !== null && 100 < t.shellSuspendCounter)
            throw Error(r(482));
          t = e, t.status = "pending", t.then(
            function(n) {
              if (e.status === "pending") {
                var a = e;
                a.status = "fulfilled", a.value = n;
              }
            },
            function(n) {
              if (e.status === "pending") {
                var a = e;
                a.status = "rejected", a.reason = n;
              }
            }
          );
        }
        switch (e.status) {
          case "fulfilled":
            return e.value;
          case "rejected":
            throw t = e.reason, jr(t), t;
        }
        throw En = e, la;
    }
  }
  function _n(t) {
    try {
      var e = t._init;
      return e(t._payload);
    } catch (l) {
      throw l !== null && typeof l == "object" && typeof l.then == "function" ? (En = l, la) : l;
    }
  }
  var En = null;
  function Ur() {
    if (En === null) throw Error(r(459));
    var t = En;
    return En = null, t;
  }
  function jr(t) {
    if (t === la || t === $u)
      throw Error(r(483));
  }
  var na = null, Ja = 0;
  function Fu(t) {
    var e = Ja;
    return Ja += 1, na === null && (na = []), Dr(na, t, e);
  }
  function Ka(t, e) {
    e = e.props.ref, t.ref = e !== void 0 ? e : null;
  }
  function Iu(t, e) {
    throw e.$$typeof === C ? Error(r(525)) : (t = Object.prototype.toString.call(e), Error(
      r(
        31,
        t === "[object Object]" ? "object with keys {" + Object.keys(e).join(", ") + "}" : t
      )
    ));
  }
  function Cr(t) {
    function e(p, v) {
      if (t) {
        var _ = p.deletions;
        _ === null ? (p.deletions = [v], p.flags |= 16) : _.push(v);
      }
    }
    function l(p, v) {
      if (!t) return null;
      for (; v !== null; )
        e(p, v), v = v.sibling;
      return null;
    }
    function n(p) {
      for (var v = /* @__PURE__ */ new Map(); p !== null; )
        p.key !== null ? v.set(p.key, p) : v.set(p.index, p), p = p.sibling;
      return v;
    }
    function a(p, v) {
      return p = sl(p, v), p.index = 0, p.sibling = null, p;
    }
    function u(p, v, _) {
      return p.index = _, t ? (_ = p.alternate, _ !== null ? (_ = _.index, _ < v ? (p.flags |= 67108866, v) : _) : (p.flags |= 67108866, v)) : (p.flags |= 1048576, v);
    }
    function c(p) {
      return t && p.alternate === null && (p.flags |= 67108866), p;
    }
    function o(p, v, _, M) {
      return v === null || v.tag !== 6 ? (v = _c(_, p.mode, M), v.return = p, v) : (v = a(v, _), v.return = p, v);
    }
    function y(p, v, _, M) {
      var J = _.type;
      return J === P ? O(
        p,
        v,
        _.props.children,
        M,
        _.key
      ) : v !== null && (v.elementType === J || typeof J == "object" && J !== null && J.$$typeof === _t && _n(J) === v.type) ? (v = a(v, _.props), Ka(v, _), v.return = p, v) : (v = Vu(
        _.type,
        _.key,
        _.props,
        null,
        p.mode,
        M
      ), Ka(v, _), v.return = p, v);
    }
    function E(p, v, _, M) {
      return v === null || v.tag !== 4 || v.stateNode.containerInfo !== _.containerInfo || v.stateNode.implementation !== _.implementation ? (v = Ec(_, p.mode, M), v.return = p, v) : (v = a(v, _.children || []), v.return = p, v);
    }
    function O(p, v, _, M, J) {
      return v === null || v.tag !== 7 ? (v = vn(
        _,
        p.mode,
        M,
        J
      ), v.return = p, v) : (v = a(v, _), v.return = p, v);
    }
    function U(p, v, _) {
      if (typeof v == "string" && v !== "" || typeof v == "number" || typeof v == "bigint")
        return v = _c(
          "" + v,
          p.mode,
          _
        ), v.return = p, v;
      if (typeof v == "object" && v !== null) {
        switch (v.$$typeof) {
          case G:
            return _ = Vu(
              v.type,
              v.key,
              v.props,
              null,
              p.mode,
              _
            ), Ka(_, v), _.return = p, _;
          case K:
            return v = Ec(
              v,
              p.mode,
              _
            ), v.return = p, v;
          case _t:
            return v = _n(v), U(p, v, _);
        }
        if (Qt(v) || kt(v))
          return v = vn(
            v,
            p.mode,
            _,
            null
          ), v.return = p, v;
        if (typeof v.then == "function")
          return U(p, Fu(v), _);
        if (v.$$typeof === I)
          return U(
            p,
            Ku(p, v),
            _
          );
        Iu(p, v);
      }
      return null;
    }
    function x(p, v, _, M) {
      var J = v !== null ? v.key : null;
      if (typeof _ == "string" && _ !== "" || typeof _ == "number" || typeof _ == "bigint")
        return J !== null ? null : o(p, v, "" + _, M);
      if (typeof _ == "object" && _ !== null) {
        switch (_.$$typeof) {
          case G:
            return _.key === J ? y(p, v, _, M) : null;
          case K:
            return _.key === J ? E(p, v, _, M) : null;
          case _t:
            return _ = _n(_), x(p, v, _, M);
        }
        if (Qt(_) || kt(_))
          return J !== null ? null : O(p, v, _, M, null);
        if (typeof _.then == "function")
          return x(
            p,
            v,
            Fu(_),
            M
          );
        if (_.$$typeof === I)
          return x(
            p,
            v,
            Ku(p, _),
            M
          );
        Iu(p, _);
      }
      return null;
    }
    function z(p, v, _, M, J) {
      if (typeof M == "string" && M !== "" || typeof M == "number" || typeof M == "bigint")
        return p = p.get(_) || null, o(v, p, "" + M, J);
      if (typeof M == "object" && M !== null) {
        switch (M.$$typeof) {
          case G:
            return p = p.get(
              M.key === null ? _ : M.key
            ) || null, y(v, p, M, J);
          case K:
            return p = p.get(
              M.key === null ? _ : M.key
            ) || null, E(v, p, M, J);
          case _t:
            return M = _n(M), z(
              p,
              v,
              _,
              M,
              J
            );
        }
        if (Qt(M) || kt(M))
          return p = p.get(_) || null, O(v, p, M, J, null);
        if (typeof M.then == "function")
          return z(
            p,
            v,
            _,
            Fu(M),
            J
          );
        if (M.$$typeof === I)
          return z(
            p,
            v,
            _,
            Ku(v, M),
            J
          );
        Iu(v, M);
      }
      return null;
    }
    function X(p, v, _, M) {
      for (var J = null, gt = null, Z = v, ut = v = 0, ot = null; Z !== null && ut < _.length; ut++) {
        Z.index > ut ? (ot = Z, Z = null) : ot = Z.sibling;
        var pt = x(
          p,
          Z,
          _[ut],
          M
        );
        if (pt === null) {
          Z === null && (Z = ot);
          break;
        }
        t && Z && pt.alternate === null && e(p, Z), v = u(pt, v, ut), gt === null ? J = pt : gt.sibling = pt, gt = pt, Z = ot;
      }
      if (ut === _.length)
        return l(p, Z), dt && rl(p, ut), J;
      if (Z === null) {
        for (; ut < _.length; ut++)
          Z = U(p, _[ut], M), Z !== null && (v = u(
            Z,
            v,
            ut
          ), gt === null ? J = Z : gt.sibling = Z, gt = Z);
        return dt && rl(p, ut), J;
      }
      for (Z = n(Z); ut < _.length; ut++)
        ot = z(
          Z,
          p,
          ut,
          _[ut],
          M
        ), ot !== null && (t && ot.alternate !== null && Z.delete(
          ot.key === null ? ut : ot.key
        ), v = u(
          ot,
          v,
          ut
        ), gt === null ? J = ot : gt.sibling = ot, gt = ot);
      return t && Z.forEach(function(tn) {
        return e(p, tn);
      }), dt && rl(p, ut), J;
    }
    function k(p, v, _, M) {
      if (_ == null) throw Error(r(151));
      for (var J = null, gt = null, Z = v, ut = v = 0, ot = null, pt = _.next(); Z !== null && !pt.done; ut++, pt = _.next()) {
        Z.index > ut ? (ot = Z, Z = null) : ot = Z.sibling;
        var tn = x(p, Z, pt.value, M);
        if (tn === null) {
          Z === null && (Z = ot);
          break;
        }
        t && Z && tn.alternate === null && e(p, Z), v = u(tn, v, ut), gt === null ? J = tn : gt.sibling = tn, gt = tn, Z = ot;
      }
      if (pt.done)
        return l(p, Z), dt && rl(p, ut), J;
      if (Z === null) {
        for (; !pt.done; ut++, pt = _.next())
          pt = U(p, pt.value, M), pt !== null && (v = u(pt, v, ut), gt === null ? J = pt : gt.sibling = pt, gt = pt);
        return dt && rl(p, ut), J;
      }
      for (Z = n(Z); !pt.done; ut++, pt = _.next())
        pt = z(Z, p, ut, pt.value, M), pt !== null && (t && pt.alternate !== null && Z.delete(pt.key === null ? ut : pt.key), v = u(pt, v, ut), gt === null ? J = pt : gt.sibling = pt, gt = pt);
      return t && Z.forEach(function(ev) {
        return e(p, ev);
      }), dt && rl(p, ut), J;
    }
    function zt(p, v, _, M) {
      if (typeof _ == "object" && _ !== null && _.type === P && _.key === null && (_ = _.props.children), typeof _ == "object" && _ !== null) {
        switch (_.$$typeof) {
          case G:
            t: {
              for (var J = _.key; v !== null; ) {
                if (v.key === J) {
                  if (J = _.type, J === P) {
                    if (v.tag === 7) {
                      l(
                        p,
                        v.sibling
                      ), M = a(
                        v,
                        _.props.children
                      ), M.return = p, p = M;
                      break t;
                    }
                  } else if (v.elementType === J || typeof J == "object" && J !== null && J.$$typeof === _t && _n(J) === v.type) {
                    l(
                      p,
                      v.sibling
                    ), M = a(v, _.props), Ka(M, _), M.return = p, p = M;
                    break t;
                  }
                  l(p, v);
                  break;
                } else e(p, v);
                v = v.sibling;
              }
              _.type === P ? (M = vn(
                _.props.children,
                p.mode,
                M,
                _.key
              ), M.return = p, p = M) : (M = Vu(
                _.type,
                _.key,
                _.props,
                null,
                p.mode,
                M
              ), Ka(M, _), M.return = p, p = M);
            }
            return c(p);
          case K:
            t: {
              for (J = _.key; v !== null; ) {
                if (v.key === J)
                  if (v.tag === 4 && v.stateNode.containerInfo === _.containerInfo && v.stateNode.implementation === _.implementation) {
                    l(
                      p,
                      v.sibling
                    ), M = a(v, _.children || []), M.return = p, p = M;
                    break t;
                  } else {
                    l(p, v);
                    break;
                  }
                else e(p, v);
                v = v.sibling;
              }
              M = Ec(_, p.mode, M), M.return = p, p = M;
            }
            return c(p);
          case _t:
            return _ = _n(_), zt(
              p,
              v,
              _,
              M
            );
        }
        if (Qt(_))
          return X(
            p,
            v,
            _,
            M
          );
        if (kt(_)) {
          if (J = kt(_), typeof J != "function") throw Error(r(150));
          return _ = J.call(_), k(
            p,
            v,
            _,
            M
          );
        }
        if (typeof _.then == "function")
          return zt(
            p,
            v,
            Fu(_),
            M
          );
        if (_.$$typeof === I)
          return zt(
            p,
            v,
            Ku(p, _),
            M
          );
        Iu(p, _);
      }
      return typeof _ == "string" && _ !== "" || typeof _ == "number" || typeof _ == "bigint" ? (_ = "" + _, v !== null && v.tag === 6 ? (l(p, v.sibling), M = a(v, _), M.return = p, p = M) : (l(p, v), M = _c(_, p.mode, M), M.return = p, p = M), c(p)) : l(p, v);
    }
    return function(p, v, _, M) {
      try {
        Ja = 0;
        var J = zt(
          p,
          v,
          _,
          M
        );
        return na = null, J;
      } catch (Z) {
        if (Z === la || Z === $u) throw Z;
        var gt = xe(29, Z, null, p.mode);
        return gt.lanes = M, gt.return = p, gt;
      } finally {
      }
    };
  }
  var xn = Cr(!0), Rr = Cr(!1), Ll = !1;
  function Cc(t) {
    t.updateQueue = {
      baseState: t.memoizedState,
      firstBaseUpdate: null,
      lastBaseUpdate: null,
      shared: { pending: null, lanes: 0, hiddenCallbacks: null },
      callbacks: null
    };
  }
  function Rc(t, e) {
    t = t.updateQueue, e.updateQueue === t && (e.updateQueue = {
      baseState: t.baseState,
      firstBaseUpdate: t.firstBaseUpdate,
      lastBaseUpdate: t.lastBaseUpdate,
      shared: t.shared,
      callbacks: null
    });
  }
  function Bl(t) {
    return { lane: t, tag: 0, payload: null, callback: null, next: null };
  }
  function Yl(t, e, l) {
    var n = t.updateQueue;
    if (n === null) return null;
    if (n = n.shared, (St & 2) !== 0) {
      var a = n.pending;
      return a === null ? e.next = e : (e.next = a.next, a.next = e), n.pending = e, e = Zu(t), pr(t, null, l), e;
    }
    return Xu(t, n, e, l), Zu(t);
  }
  function ka(t, e, l) {
    if (e = e.updateQueue, e !== null && (e = e.shared, (l & 4194048) !== 0)) {
      var n = e.lanes;
      n &= t.pendingLanes, l |= n, e.lanes = l, Dt(t, l);
    }
  }
  function Hc(t, e) {
    var l = t.updateQueue, n = t.alternate;
    if (n !== null && (n = n.updateQueue, l === n)) {
      var a = null, u = null;
      if (l = l.firstBaseUpdate, l !== null) {
        do {
          var c = {
            lane: l.lane,
            tag: l.tag,
            payload: l.payload,
            callback: null,
            next: null
          };
          u === null ? a = u = c : u = u.next = c, l = l.next;
        } while (l !== null);
        u === null ? a = u = e : u = u.next = e;
      } else a = u = e;
      l = {
        baseState: n.baseState,
        firstBaseUpdate: a,
        lastBaseUpdate: u,
        shared: n.shared,
        callbacks: n.callbacks
      }, t.updateQueue = l;
      return;
    }
    t = l.lastBaseUpdate, t === null ? l.firstBaseUpdate = e : t.next = e, l.lastBaseUpdate = e;
  }
  var Lc = !1;
  function $a() {
    if (Lc) {
      var t = ea;
      if (t !== null) throw t;
    }
  }
  function Wa(t, e, l, n) {
    Lc = !1;
    var a = t.updateQueue;
    Ll = !1;
    var u = a.firstBaseUpdate, c = a.lastBaseUpdate, o = a.shared.pending;
    if (o !== null) {
      a.shared.pending = null;
      var y = o, E = y.next;
      y.next = null, c === null ? u = E : c.next = E, c = y;
      var O = t.alternate;
      O !== null && (O = O.updateQueue, o = O.lastBaseUpdate, o !== c && (o === null ? O.firstBaseUpdate = E : o.next = E, O.lastBaseUpdate = y));
    }
    if (u !== null) {
      var U = a.baseState;
      c = 0, O = E = y = null, o = u;
      do {
        var x = o.lane & -536870913, z = x !== o.lane;
        if (z ? (rt & x) === x : (n & x) === x) {
          x !== 0 && x === ta && (Lc = !0), O !== null && (O = O.next = {
            lane: 0,
            tag: o.tag,
            payload: o.payload,
            callback: null,
            next: null
          });
          t: {
            var X = t, k = o;
            x = e;
            var zt = l;
            switch (k.tag) {
              case 1:
                if (X = k.payload, typeof X == "function") {
                  U = X.call(zt, U, x);
                  break t;
                }
                U = X;
                break t;
              case 3:
                X.flags = X.flags & -65537 | 128;
              case 0:
                if (X = k.payload, x = typeof X == "function" ? X.call(zt, U, x) : X, x == null) break t;
                U = S({}, U, x);
                break t;
              case 2:
                Ll = !0;
            }
          }
          x = o.callback, x !== null && (t.flags |= 64, z && (t.flags |= 8192), z = a.callbacks, z === null ? a.callbacks = [x] : z.push(x));
        } else
          z = {
            lane: x,
            tag: o.tag,
            payload: o.payload,
            callback: o.callback,
            next: null
          }, O === null ? (E = O = z, y = U) : O = O.next = z, c |= x;
        if (o = o.next, o === null) {
          if (o = a.shared.pending, o === null)
            break;
          z = o, o = z.next, z.next = null, a.lastBaseUpdate = z, a.shared.pending = null;
        }
      } while (!0);
      O === null && (y = U), a.baseState = y, a.firstBaseUpdate = E, a.lastBaseUpdate = O, u === null && (a.shared.lanes = 0), Vl |= c, t.lanes = c, t.memoizedState = U;
    }
  }
  function Hr(t, e) {
    if (typeof t != "function")
      throw Error(r(191, t));
    t.call(e);
  }
  function Lr(t, e) {
    var l = t.callbacks;
    if (l !== null)
      for (t.callbacks = null, t = 0; t < l.length; t++)
        Hr(l[t], e);
  }
  var aa = h(null), Pu = h(0);
  function Br(t, e) {
    t = _l, L(Pu, t), L(aa, e), _l = t | e.baseLanes;
  }
  function Bc() {
    L(Pu, _l), L(aa, aa.current);
  }
  function Yc() {
    _l = Pu.current, D(aa), D(Pu);
  }
  var Ae = h(null), Le = null;
  function Gl(t) {
    var e = t.alternate;
    L(Bt, Bt.current & 1), L(Ae, t), Le === null && (e === null || aa.current !== null || e.memoizedState !== null) && (Le = t);
  }
  function Gc(t) {
    L(Bt, Bt.current), L(Ae, t), Le === null && (Le = t);
  }
  function Yr(t) {
    t.tag === 22 ? (L(Bt, Bt.current), L(Ae, t), Le === null && (Le = t)) : Ql();
  }
  function Ql() {
    L(Bt, Bt.current), L(Ae, Ae.current);
  }
  function Te(t) {
    D(Ae), Le === t && (Le = null), D(Bt);
  }
  var Bt = h(0);
  function ti(t) {
    for (var e = t; e !== null; ) {
      if (e.tag === 13) {
        var l = e.memoizedState;
        if (l !== null && (l = l.dehydrated, l === null || Kf(l) || kf(l)))
          return e;
      } else if (e.tag === 19 && (e.memoizedProps.revealOrder === "forwards" || e.memoizedProps.revealOrder === "backwards" || e.memoizedProps.revealOrder === "unstable_legacy-backwards" || e.memoizedProps.revealOrder === "together")) {
        if ((e.flags & 128) !== 0) return e;
      } else if (e.child !== null) {
        e.child.return = e, e = e.child;
        continue;
      }
      if (e === t) break;
      for (; e.sibling === null; ) {
        if (e.return === null || e.return === t) return null;
        e = e.return;
      }
      e.sibling.return = e.return, e = e.sibling;
    }
    return null;
  }
  var yl = 0, lt = null, At = null, wt = null, ei = !1, ua = !1, An = !1, li = 0, Fa = 0, ia = null, wm = 0;
  function Rt() {
    throw Error(r(321));
  }
  function Qc(t, e) {
    if (e === null) return !1;
    for (var l = 0; l < e.length && l < t.length; l++)
      if (!Ee(t[l], e[l])) return !1;
    return !0;
  }
  function Xc(t, e, l, n, a, u) {
    return yl = u, lt = e, e.memoizedState = null, e.updateQueue = null, e.lanes = 0, N.H = t === null || t.memoizedState === null ? xo : nf, An = !1, u = l(n, a), An = !1, ua && (u = Qr(
      e,
      l,
      n,
      a
    )), Gr(t), u;
  }
  function Gr(t) {
    N.H = tu;
    var e = At !== null && At.next !== null;
    if (yl = 0, wt = At = lt = null, ei = !1, Fa = 0, ia = null, e) throw Error(r(300));
    t === null || Jt || (t = t.dependencies, t !== null && Ju(t) && (Jt = !0));
  }
  function Qr(t, e, l, n) {
    lt = t;
    var a = 0;
    do {
      if (ua && (ia = null), Fa = 0, ua = !1, 25 <= a) throw Error(r(301));
      if (a += 1, wt = At = null, t.updateQueue != null) {
        var u = t.updateQueue;
        u.lastEffect = null, u.events = null, u.stores = null, u.memoCache != null && (u.memoCache.index = 0);
      }
      N.H = Ao, u = e(l, n);
    } while (ua);
    return u;
  }
  function Jm() {
    var t = N.H, e = t.useState()[0];
    return e = typeof e.then == "function" ? Ia(e) : e, t = t.useState()[0], (At !== null ? At.memoizedState : null) !== t && (lt.flags |= 1024), e;
  }
  function Zc() {
    var t = li !== 0;
    return li = 0, t;
  }
  function Vc(t, e, l) {
    e.updateQueue = t.updateQueue, e.flags &= -2053, t.lanes &= ~l;
  }
  function wc(t) {
    if (ei) {
      for (t = t.memoizedState; t !== null; ) {
        var e = t.queue;
        e !== null && (e.pending = null), t = t.next;
      }
      ei = !1;
    }
    yl = 0, wt = At = lt = null, ua = !1, Fa = li = 0, ia = null;
  }
  function de() {
    var t = {
      memoizedState: null,
      baseState: null,
      baseQueue: null,
      queue: null,
      next: null
    };
    return wt === null ? lt.memoizedState = wt = t : wt = wt.next = t, wt;
  }
  function Yt() {
    if (At === null) {
      var t = lt.alternate;
      t = t !== null ? t.memoizedState : null;
    } else t = At.next;
    var e = wt === null ? lt.memoizedState : wt.next;
    if (e !== null)
      wt = e, At = t;
    else {
      if (t === null)
        throw lt.alternate === null ? Error(r(467)) : Error(r(310));
      At = t, t = {
        memoizedState: At.memoizedState,
        baseState: At.baseState,
        baseQueue: At.baseQueue,
        queue: At.queue,
        next: null
      }, wt === null ? lt.memoizedState = wt = t : wt = wt.next = t;
    }
    return wt;
  }
  function ni() {
    return { lastEffect: null, events: null, stores: null, memoCache: null };
  }
  function Ia(t) {
    var e = Fa;
    return Fa += 1, ia === null && (ia = []), t = Dr(ia, t, e), e = lt, (wt === null ? e.memoizedState : wt.next) === null && (e = e.alternate, N.H = e === null || e.memoizedState === null ? xo : nf), t;
  }
  function ai(t) {
    if (t !== null && typeof t == "object") {
      if (typeof t.then == "function") return Ia(t);
      if (t.$$typeof === I) return ne(t);
    }
    throw Error(r(438, String(t)));
  }
  function Jc(t) {
    var e = null, l = lt.updateQueue;
    if (l !== null && (e = l.memoCache), e == null) {
      var n = lt.alternate;
      n !== null && (n = n.updateQueue, n !== null && (n = n.memoCache, n != null && (e = {
        data: n.data.map(function(a) {
          return a.slice();
        }),
        index: 0
      })));
    }
    if (e == null && (e = { data: [], index: 0 }), l === null && (l = ni(), lt.updateQueue = l), l.memoCache = e, l = e.data[e.index], l === void 0)
      for (l = e.data[e.index] = Array(t), n = 0; n < t; n++)
        l[n] = ql;
    return e.index++, l;
  }
  function ml(t, e) {
    return typeof e == "function" ? e(t) : e;
  }
  function ui(t) {
    var e = Yt();
    return Kc(e, At, t);
  }
  function Kc(t, e, l) {
    var n = t.queue;
    if (n === null) throw Error(r(311));
    n.lastRenderedReducer = l;
    var a = t.baseQueue, u = n.pending;
    if (u !== null) {
      if (a !== null) {
        var c = a.next;
        a.next = u.next, u.next = c;
      }
      e.baseQueue = a = u, n.pending = null;
    }
    if (u = t.baseState, a === null) t.memoizedState = u;
    else {
      e = a.next;
      var o = c = null, y = null, E = e, O = !1;
      do {
        var U = E.lane & -536870913;
        if (U !== E.lane ? (rt & U) === U : (yl & U) === U) {
          var x = E.revertLane;
          if (x === 0)
            y !== null && (y = y.next = {
              lane: 0,
              revertLane: 0,
              gesture: null,
              action: E.action,
              hasEagerState: E.hasEagerState,
              eagerState: E.eagerState,
              next: null
            }), U === ta && (O = !0);
          else if ((yl & x) === x) {
            E = E.next, x === ta && (O = !0);
            continue;
          } else
            U = {
              lane: 0,
              revertLane: E.revertLane,
              gesture: null,
              action: E.action,
              hasEagerState: E.hasEagerState,
              eagerState: E.eagerState,
              next: null
            }, y === null ? (o = y = U, c = u) : y = y.next = U, lt.lanes |= x, Vl |= x;
          U = E.action, An && l(u, U), u = E.hasEagerState ? E.eagerState : l(u, U);
        } else
          x = {
            lane: U,
            revertLane: E.revertLane,
            gesture: E.gesture,
            action: E.action,
            hasEagerState: E.hasEagerState,
            eagerState: E.eagerState,
            next: null
          }, y === null ? (o = y = x, c = u) : y = y.next = x, lt.lanes |= U, Vl |= U;
        E = E.next;
      } while (E !== null && E !== e);
      if (y === null ? c = u : y.next = o, !Ee(u, t.memoizedState) && (Jt = !0, O && (l = ea, l !== null)))
        throw l;
      t.memoizedState = u, t.baseState = c, t.baseQueue = y, n.lastRenderedState = u;
    }
    return a === null && (n.lanes = 0), [t.memoizedState, n.dispatch];
  }
  function kc(t) {
    var e = Yt(), l = e.queue;
    if (l === null) throw Error(r(311));
    l.lastRenderedReducer = t;
    var n = l.dispatch, a = l.pending, u = e.memoizedState;
    if (a !== null) {
      l.pending = null;
      var c = a = a.next;
      do
        u = t(u, c.action), c = c.next;
      while (c !== a);
      Ee(u, e.memoizedState) || (Jt = !0), e.memoizedState = u, e.baseQueue === null && (e.baseState = u), l.lastRenderedState = u;
    }
    return [u, n];
  }
  function Xr(t, e, l) {
    var n = lt, a = Yt(), u = dt;
    if (u) {
      if (l === void 0) throw Error(r(407));
      l = l();
    } else l = e();
    var c = !Ee(
      (At || a).memoizedState,
      l
    );
    if (c && (a.memoizedState = l, Jt = !0), a = a.queue, Fc(wr.bind(null, n, a, t), [
      t
    ]), a.getSnapshot !== e || c || wt !== null && wt.memoizedState.tag & 1) {
      if (n.flags |= 2048, ca(
        9,
        { destroy: void 0 },
        Vr.bind(
          null,
          n,
          a,
          l,
          e
        ),
        null
      ), qt === null) throw Error(r(349));
      u || (yl & 127) !== 0 || Zr(n, e, l);
    }
    return l;
  }
  function Zr(t, e, l) {
    t.flags |= 16384, t = { getSnapshot: e, value: l }, e = lt.updateQueue, e === null ? (e = ni(), lt.updateQueue = e, e.stores = [t]) : (l = e.stores, l === null ? e.stores = [t] : l.push(t));
  }
  function Vr(t, e, l, n) {
    e.value = l, e.getSnapshot = n, Jr(e) && Kr(t);
  }
  function wr(t, e, l) {
    return l(function() {
      Jr(e) && Kr(t);
    });
  }
  function Jr(t) {
    var e = t.getSnapshot;
    t = t.value;
    try {
      var l = e();
      return !Ee(t, l);
    } catch {
      return !0;
    }
  }
  function Kr(t) {
    var e = hn(t, 2);
    e !== null && be(e, t, 2);
  }
  function $c(t) {
    var e = de();
    if (typeof t == "function") {
      var l = t;
      if (t = l(), An) {
        $e(!0);
        try {
          l();
        } finally {
          $e(!1);
        }
      }
    }
    return e.memoizedState = e.baseState = t, e.queue = {
      pending: null,
      lanes: 0,
      dispatch: null,
      lastRenderedReducer: ml,
      lastRenderedState: t
    }, e;
  }
  function kr(t, e, l, n) {
    return t.baseState = l, Kc(
      t,
      At,
      typeof n == "function" ? n : ml
    );
  }
  function Km(t, e, l, n, a) {
    if (fi(t)) throw Error(r(485));
    if (t = e.action, t !== null) {
      var u = {
        payload: a,
        action: t,
        next: null,
        isTransition: !0,
        status: "pending",
        value: null,
        reason: null,
        listeners: [],
        then: function(c) {
          u.listeners.push(c);
        }
      };
      N.T !== null ? l(!0) : u.isTransition = !1, n(u), l = e.pending, l === null ? (u.next = e.pending = u, $r(e, u)) : (u.next = l.next, e.pending = l.next = u);
    }
  }
  function $r(t, e) {
    var l = e.action, n = e.payload, a = t.state;
    if (e.isTransition) {
      var u = N.T, c = {};
      N.T = c;
      try {
        var o = l(a, n), y = N.S;
        y !== null && y(c, o), Wr(t, e, o);
      } catch (E) {
        Wc(t, e, E);
      } finally {
        u !== null && c.types !== null && (u.types = c.types), N.T = u;
      }
    } else
      try {
        u = l(a, n), Wr(t, e, u);
      } catch (E) {
        Wc(t, e, E);
      }
  }
  function Wr(t, e, l) {
    l !== null && typeof l == "object" && typeof l.then == "function" ? l.then(
      function(n) {
        Fr(t, e, n);
      },
      function(n) {
        return Wc(t, e, n);
      }
    ) : Fr(t, e, l);
  }
  function Fr(t, e, l) {
    e.status = "fulfilled", e.value = l, Ir(e), t.state = l, e = t.pending, e !== null && (l = e.next, l === e ? t.pending = null : (l = l.next, e.next = l, $r(t, l)));
  }
  function Wc(t, e, l) {
    var n = t.pending;
    if (t.pending = null, n !== null) {
      n = n.next;
      do
        e.status = "rejected", e.reason = l, Ir(e), e = e.next;
      while (e !== n);
    }
    t.action = null;
  }
  function Ir(t) {
    t = t.listeners;
    for (var e = 0; e < t.length; e++) (0, t[e])();
  }
  function Pr(t, e) {
    return e;
  }
  function to(t, e) {
    if (dt) {
      var l = qt.formState;
      if (l !== null) {
        t: {
          var n = lt;
          if (dt) {
            if (Ot) {
              e: {
                for (var a = Ot, u = He; a.nodeType !== 8; ) {
                  if (!u) {
                    a = null;
                    break e;
                  }
                  if (a = Be(
                    a.nextSibling
                  ), a === null) {
                    a = null;
                    break e;
                  }
                }
                u = a.data, a = u === "F!" || u === "F" ? a : null;
              }
              if (a) {
                Ot = Be(
                  a.nextSibling
                ), n = a.data === "F!";
                break t;
              }
            }
            Rl(n);
          }
          n = !1;
        }
        n && (e = l[0]);
      }
    }
    return l = de(), l.memoizedState = l.baseState = e, n = {
      pending: null,
      lanes: 0,
      dispatch: null,
      lastRenderedReducer: Pr,
      lastRenderedState: e
    }, l.queue = n, l = So.bind(
      null,
      lt,
      n
    ), n.dispatch = l, n = $c(!1), u = lf.bind(
      null,
      lt,
      !1,
      n.queue
    ), n = de(), a = {
      state: e,
      dispatch: null,
      action: t,
      pending: null
    }, n.queue = a, l = Km.bind(
      null,
      lt,
      a,
      u,
      l
    ), a.dispatch = l, n.memoizedState = t, [e, l, !1];
  }
  function eo(t) {
    var e = Yt();
    return lo(e, At, t);
  }
  function lo(t, e, l) {
    if (e = Kc(
      t,
      e,
      Pr
    )[0], t = ui(ml)[0], typeof e == "object" && e !== null && typeof e.then == "function")
      try {
        var n = Ia(e);
      } catch (c) {
        throw c === la ? $u : c;
      }
    else n = e;
    e = Yt();
    var a = e.queue, u = a.dispatch;
    return l !== e.memoizedState && (lt.flags |= 2048, ca(
      9,
      { destroy: void 0 },
      km.bind(null, a, l),
      null
    )), [n, u, t];
  }
  function km(t, e) {
    t.action = e;
  }
  function no(t) {
    var e = Yt(), l = At;
    if (l !== null)
      return lo(e, l, t);
    Yt(), e = e.memoizedState, l = Yt();
    var n = l.queue.dispatch;
    return l.memoizedState = t, [e, n, !1];
  }
  function ca(t, e, l, n) {
    return t = { tag: t, create: l, deps: n, inst: e, next: null }, e = lt.updateQueue, e === null && (e = ni(), lt.updateQueue = e), l = e.lastEffect, l === null ? e.lastEffect = t.next = t : (n = l.next, l.next = t, t.next = n, e.lastEffect = t), t;
  }
  function ao() {
    return Yt().memoizedState;
  }
  function ii(t, e, l, n) {
    var a = de();
    lt.flags |= t, a.memoizedState = ca(
      1 | e,
      { destroy: void 0 },
      l,
      n === void 0 ? null : n
    );
  }
  function ci(t, e, l, n) {
    var a = Yt();
    n = n === void 0 ? null : n;
    var u = a.memoizedState.inst;
    At !== null && n !== null && Qc(n, At.memoizedState.deps) ? a.memoizedState = ca(e, u, l, n) : (lt.flags |= t, a.memoizedState = ca(
      1 | e,
      u,
      l,
      n
    ));
  }
  function uo(t, e) {
    ii(8390656, 8, t, e);
  }
  function Fc(t, e) {
    ci(2048, 8, t, e);
  }
  function $m(t) {
    lt.flags |= 4;
    var e = lt.updateQueue;
    if (e === null)
      e = ni(), lt.updateQueue = e, e.events = [t];
    else {
      var l = e.events;
      l === null ? e.events = [t] : l.push(t);
    }
  }
  function io(t) {
    var e = Yt().memoizedState;
    return $m({ ref: e, nextImpl: t }), function() {
      if ((St & 2) !== 0) throw Error(r(440));
      return e.impl.apply(void 0, arguments);
    };
  }
  function co(t, e) {
    return ci(4, 2, t, e);
  }
  function fo(t, e) {
    return ci(4, 4, t, e);
  }
  function so(t, e) {
    if (typeof e == "function") {
      t = t();
      var l = e(t);
      return function() {
        typeof l == "function" ? l() : e(null);
      };
    }
    if (e != null)
      return t = t(), e.current = t, function() {
        e.current = null;
      };
  }
  function ro(t, e, l) {
    l = l != null ? l.concat([t]) : null, ci(4, 4, so.bind(null, e, t), l);
  }
  function Ic() {
  }
  function oo(t, e) {
    var l = Yt();
    e = e === void 0 ? null : e;
    var n = l.memoizedState;
    return e !== null && Qc(e, n[1]) ? n[0] : (l.memoizedState = [t, e], t);
  }
  function yo(t, e) {
    var l = Yt();
    e = e === void 0 ? null : e;
    var n = l.memoizedState;
    if (e !== null && Qc(e, n[1]))
      return n[0];
    if (n = t(), An) {
      $e(!0);
      try {
        t();
      } finally {
        $e(!1);
      }
    }
    return l.memoizedState = [n, e], n;
  }
  function Pc(t, e, l) {
    return l === void 0 || (yl & 1073741824) !== 0 && (rt & 261930) === 0 ? t.memoizedState = e : (t.memoizedState = l, t = md(), lt.lanes |= t, Vl |= t, l);
  }
  function mo(t, e, l, n) {
    return Ee(l, e) ? l : aa.current !== null ? (t = Pc(t, l, n), Ee(t, e) || (Jt = !0), t) : (yl & 42) === 0 || (yl & 1073741824) !== 0 && (rt & 261930) === 0 ? (Jt = !0, t.memoizedState = l) : (t = md(), lt.lanes |= t, Vl |= t, e);
  }
  function ho(t, e, l, n, a) {
    var u = H.p;
    H.p = u !== 0 && 8 > u ? u : 8;
    var c = N.T, o = {};
    N.T = o, lf(t, !1, e, l);
    try {
      var y = a(), E = N.S;
      if (E !== null && E(o, y), y !== null && typeof y == "object" && typeof y.then == "function") {
        var O = Vm(
          y,
          n
        );
        Pa(
          t,
          e,
          O,
          Ne(t)
        );
      } else
        Pa(
          t,
          e,
          n,
          Ne(t)
        );
    } catch (U) {
      Pa(
        t,
        e,
        { then: function() {
        }, status: "rejected", reason: U },
        Ne()
      );
    } finally {
      H.p = u, c !== null && o.types !== null && (c.types = o.types), N.T = c;
    }
  }
  function Wm() {
  }
  function tf(t, e, l, n) {
    if (t.tag !== 5) throw Error(r(476));
    var a = vo(t).queue;
    ho(
      t,
      a,
      e,
      $,
      l === null ? Wm : function() {
        return go(t), l(n);
      }
    );
  }
  function vo(t) {
    var e = t.memoizedState;
    if (e !== null) return e;
    e = {
      memoizedState: $,
      baseState: $,
      baseQueue: null,
      queue: {
        pending: null,
        lanes: 0,
        dispatch: null,
        lastRenderedReducer: ml,
        lastRenderedState: $
      },
      next: null
    };
    var l = {};
    return e.next = {
      memoizedState: l,
      baseState: l,
      baseQueue: null,
      queue: {
        pending: null,
        lanes: 0,
        dispatch: null,
        lastRenderedReducer: ml,
        lastRenderedState: l
      },
      next: null
    }, t.memoizedState = e, t = t.alternate, t !== null && (t.memoizedState = e), e;
  }
  function go(t) {
    var e = vo(t);
    e.next === null && (e = t.alternate.memoizedState), Pa(
      t,
      e.next.queue,
      {},
      Ne()
    );
  }
  function ef() {
    return ne(vu);
  }
  function po() {
    return Yt().memoizedState;
  }
  function bo() {
    return Yt().memoizedState;
  }
  function Fm(t) {
    for (var e = t.return; e !== null; ) {
      switch (e.tag) {
        case 24:
        case 3:
          var l = Ne();
          t = Bl(l);
          var n = Yl(e, t, l);
          n !== null && (be(n, e, l), ka(n, e, l)), e = { cache: Mc() }, t.payload = e;
          return;
      }
      e = e.return;
    }
  }
  function Im(t, e, l) {
    var n = Ne();
    l = {
      lane: n,
      revertLane: 0,
      gesture: null,
      action: l,
      hasEagerState: !1,
      eagerState: null,
      next: null
    }, fi(t) ? _o(e, l) : (l = bc(t, e, l, n), l !== null && (be(l, t, n), Eo(l, e, n)));
  }
  function So(t, e, l) {
    var n = Ne();
    Pa(t, e, l, n);
  }
  function Pa(t, e, l, n) {
    var a = {
      lane: n,
      revertLane: 0,
      gesture: null,
      action: l,
      hasEagerState: !1,
      eagerState: null,
      next: null
    };
    if (fi(t)) _o(e, a);
    else {
      var u = t.alternate;
      if (t.lanes === 0 && (u === null || u.lanes === 0) && (u = e.lastRenderedReducer, u !== null))
        try {
          var c = e.lastRenderedState, o = u(c, l);
          if (a.hasEagerState = !0, a.eagerState = o, Ee(o, c))
            return Xu(t, e, a, 0), qt === null && Qu(), !1;
        } catch {
        } finally {
        }
      if (l = bc(t, e, a, n), l !== null)
        return be(l, t, n), Eo(l, e, n), !0;
    }
    return !1;
  }
  function lf(t, e, l, n) {
    if (n = {
      lane: 2,
      revertLane: Rf(),
      gesture: null,
      action: n,
      hasEagerState: !1,
      eagerState: null,
      next: null
    }, fi(t)) {
      if (e) throw Error(r(479));
    } else
      e = bc(
        t,
        l,
        n,
        2
      ), e !== null && be(e, t, 2);
  }
  function fi(t) {
    var e = t.alternate;
    return t === lt || e !== null && e === lt;
  }
  function _o(t, e) {
    ua = ei = !0;
    var l = t.pending;
    l === null ? e.next = e : (e.next = l.next, l.next = e), t.pending = e;
  }
  function Eo(t, e, l) {
    if ((l & 4194048) !== 0) {
      var n = e.lanes;
      n &= t.pendingLanes, l |= n, e.lanes = l, Dt(t, l);
    }
  }
  var tu = {
    readContext: ne,
    use: ai,
    useCallback: Rt,
    useContext: Rt,
    useEffect: Rt,
    useImperativeHandle: Rt,
    useLayoutEffect: Rt,
    useInsertionEffect: Rt,
    useMemo: Rt,
    useReducer: Rt,
    useRef: Rt,
    useState: Rt,
    useDebugValue: Rt,
    useDeferredValue: Rt,
    useTransition: Rt,
    useSyncExternalStore: Rt,
    useId: Rt,
    useHostTransitionStatus: Rt,
    useFormState: Rt,
    useActionState: Rt,
    useOptimistic: Rt,
    useMemoCache: Rt,
    useCacheRefresh: Rt
  };
  tu.useEffectEvent = Rt;
  var xo = {
    readContext: ne,
    use: ai,
    useCallback: function(t, e) {
      return de().memoizedState = [
        t,
        e === void 0 ? null : e
      ], t;
    },
    useContext: ne,
    useEffect: uo,
    useImperativeHandle: function(t, e, l) {
      l = l != null ? l.concat([t]) : null, ii(
        4194308,
        4,
        so.bind(null, e, t),
        l
      );
    },
    useLayoutEffect: function(t, e) {
      return ii(4194308, 4, t, e);
    },
    useInsertionEffect: function(t, e) {
      ii(4, 2, t, e);
    },
    useMemo: function(t, e) {
      var l = de();
      e = e === void 0 ? null : e;
      var n = t();
      if (An) {
        $e(!0);
        try {
          t();
        } finally {
          $e(!1);
        }
      }
      return l.memoizedState = [n, e], n;
    },
    useReducer: function(t, e, l) {
      var n = de();
      if (l !== void 0) {
        var a = l(e);
        if (An) {
          $e(!0);
          try {
            l(e);
          } finally {
            $e(!1);
          }
        }
      } else a = e;
      return n.memoizedState = n.baseState = a, t = {
        pending: null,
        lanes: 0,
        dispatch: null,
        lastRenderedReducer: t,
        lastRenderedState: a
      }, n.queue = t, t = t.dispatch = Im.bind(
        null,
        lt,
        t
      ), [n.memoizedState, t];
    },
    useRef: function(t) {
      var e = de();
      return t = { current: t }, e.memoizedState = t;
    },
    useState: function(t) {
      t = $c(t);
      var e = t.queue, l = So.bind(null, lt, e);
      return e.dispatch = l, [t.memoizedState, l];
    },
    useDebugValue: Ic,
    useDeferredValue: function(t, e) {
      var l = de();
      return Pc(l, t, e);
    },
    useTransition: function() {
      var t = $c(!1);
      return t = ho.bind(
        null,
        lt,
        t.queue,
        !0,
        !1
      ), de().memoizedState = t, [!1, t];
    },
    useSyncExternalStore: function(t, e, l) {
      var n = lt, a = de();
      if (dt) {
        if (l === void 0)
          throw Error(r(407));
        l = l();
      } else {
        if (l = e(), qt === null)
          throw Error(r(349));
        (rt & 127) !== 0 || Zr(n, e, l);
      }
      a.memoizedState = l;
      var u = { value: l, getSnapshot: e };
      return a.queue = u, uo(wr.bind(null, n, u, t), [
        t
      ]), n.flags |= 2048, ca(
        9,
        { destroy: void 0 },
        Vr.bind(
          null,
          n,
          u,
          l,
          e
        ),
        null
      ), l;
    },
    useId: function() {
      var t = de(), e = qt.identifierPrefix;
      if (dt) {
        var l = Pe, n = Ie;
        l = (n & ~(1 << 32 - te(n) - 1)).toString(32) + l, e = "_" + e + "R_" + l, l = li++, 0 < l && (e += "H" + l.toString(32)), e += "_";
      } else
        l = wm++, e = "_" + e + "r_" + l.toString(32) + "_";
      return t.memoizedState = e;
    },
    useHostTransitionStatus: ef,
    useFormState: to,
    useActionState: to,
    useOptimistic: function(t) {
      var e = de();
      e.memoizedState = e.baseState = t;
      var l = {
        pending: null,
        lanes: 0,
        dispatch: null,
        lastRenderedReducer: null,
        lastRenderedState: null
      };
      return e.queue = l, e = lf.bind(
        null,
        lt,
        !0,
        l
      ), l.dispatch = e, [t, e];
    },
    useMemoCache: Jc,
    useCacheRefresh: function() {
      return de().memoizedState = Fm.bind(
        null,
        lt
      );
    },
    useEffectEvent: function(t) {
      var e = de(), l = { impl: t };
      return e.memoizedState = l, function() {
        if ((St & 2) !== 0)
          throw Error(r(440));
        return l.impl.apply(void 0, arguments);
      };
    }
  }, nf = {
    readContext: ne,
    use: ai,
    useCallback: oo,
    useContext: ne,
    useEffect: Fc,
    useImperativeHandle: ro,
    useInsertionEffect: co,
    useLayoutEffect: fo,
    useMemo: yo,
    useReducer: ui,
    useRef: ao,
    useState: function() {
      return ui(ml);
    },
    useDebugValue: Ic,
    useDeferredValue: function(t, e) {
      var l = Yt();
      return mo(
        l,
        At.memoizedState,
        t,
        e
      );
    },
    useTransition: function() {
      var t = ui(ml)[0], e = Yt().memoizedState;
      return [
        typeof t == "boolean" ? t : Ia(t),
        e
      ];
    },
    useSyncExternalStore: Xr,
    useId: po,
    useHostTransitionStatus: ef,
    useFormState: eo,
    useActionState: eo,
    useOptimistic: function(t, e) {
      var l = Yt();
      return kr(l, At, t, e);
    },
    useMemoCache: Jc,
    useCacheRefresh: bo
  };
  nf.useEffectEvent = io;
  var Ao = {
    readContext: ne,
    use: ai,
    useCallback: oo,
    useContext: ne,
    useEffect: Fc,
    useImperativeHandle: ro,
    useInsertionEffect: co,
    useLayoutEffect: fo,
    useMemo: yo,
    useReducer: kc,
    useRef: ao,
    useState: function() {
      return kc(ml);
    },
    useDebugValue: Ic,
    useDeferredValue: function(t, e) {
      var l = Yt();
      return At === null ? Pc(l, t, e) : mo(
        l,
        At.memoizedState,
        t,
        e
      );
    },
    useTransition: function() {
      var t = kc(ml)[0], e = Yt().memoizedState;
      return [
        typeof t == "boolean" ? t : Ia(t),
        e
      ];
    },
    useSyncExternalStore: Xr,
    useId: po,
    useHostTransitionStatus: ef,
    useFormState: no,
    useActionState: no,
    useOptimistic: function(t, e) {
      var l = Yt();
      return At !== null ? kr(l, At, t, e) : (l.baseState = t, [t, l.queue.dispatch]);
    },
    useMemoCache: Jc,
    useCacheRefresh: bo
  };
  Ao.useEffectEvent = io;
  function af(t, e, l, n) {
    e = t.memoizedState, l = l(n, e), l = l == null ? e : S({}, e, l), t.memoizedState = l, t.lanes === 0 && (t.updateQueue.baseState = l);
  }
  var uf = {
    enqueueSetState: function(t, e, l) {
      t = t._reactInternals;
      var n = Ne(), a = Bl(n);
      a.payload = e, l != null && (a.callback = l), e = Yl(t, a, n), e !== null && (be(e, t, n), ka(e, t, n));
    },
    enqueueReplaceState: function(t, e, l) {
      t = t._reactInternals;
      var n = Ne(), a = Bl(n);
      a.tag = 1, a.payload = e, l != null && (a.callback = l), e = Yl(t, a, n), e !== null && (be(e, t, n), ka(e, t, n));
    },
    enqueueForceUpdate: function(t, e) {
      t = t._reactInternals;
      var l = Ne(), n = Bl(l);
      n.tag = 2, e != null && (n.callback = e), e = Yl(t, n, l), e !== null && (be(e, t, l), ka(e, t, l));
    }
  };
  function To(t, e, l, n, a, u, c) {
    return t = t.stateNode, typeof t.shouldComponentUpdate == "function" ? t.shouldComponentUpdate(n, u, c) : e.prototype && e.prototype.isPureReactComponent ? !Ga(l, n) || !Ga(a, u) : !0;
  }
  function zo(t, e, l, n) {
    t = e.state, typeof e.componentWillReceiveProps == "function" && e.componentWillReceiveProps(l, n), typeof e.UNSAFE_componentWillReceiveProps == "function" && e.UNSAFE_componentWillReceiveProps(l, n), e.state !== t && uf.enqueueReplaceState(e, e.state, null);
  }
  function Tn(t, e) {
    var l = e;
    if ("ref" in e) {
      l = {};
      for (var n in e)
        n !== "ref" && (l[n] = e[n]);
    }
    if (t = t.defaultProps) {
      l === e && (l = S({}, l));
      for (var a in t)
        l[a] === void 0 && (l[a] = t[a]);
    }
    return l;
  }
  function qo(t) {
    Gu(t);
  }
  function No(t) {
    console.error(t);
  }
  function Oo(t) {
    Gu(t);
  }
  function si(t, e) {
    try {
      var l = t.onUncaughtError;
      l(e.value, { componentStack: e.stack });
    } catch (n) {
      setTimeout(function() {
        throw n;
      });
    }
  }
  function Mo(t, e, l) {
    try {
      var n = t.onCaughtError;
      n(l.value, {
        componentStack: l.stack,
        errorBoundary: e.tag === 1 ? e.stateNode : null
      });
    } catch (a) {
      setTimeout(function() {
        throw a;
      });
    }
  }
  function cf(t, e, l) {
    return l = Bl(l), l.tag = 3, l.payload = { element: null }, l.callback = function() {
      si(t, e);
    }, l;
  }
  function Do(t) {
    return t = Bl(t), t.tag = 3, t;
  }
  function Uo(t, e, l, n) {
    var a = l.type.getDerivedStateFromError;
    if (typeof a == "function") {
      var u = n.value;
      t.payload = function() {
        return a(u);
      }, t.callback = function() {
        Mo(e, l, n);
      };
    }
    var c = l.stateNode;
    c !== null && typeof c.componentDidCatch == "function" && (t.callback = function() {
      Mo(e, l, n), typeof a != "function" && (wl === null ? wl = /* @__PURE__ */ new Set([this]) : wl.add(this));
      var o = n.stack;
      this.componentDidCatch(n.value, {
        componentStack: o !== null ? o : ""
      });
    });
  }
  function Pm(t, e, l, n, a) {
    if (l.flags |= 32768, n !== null && typeof n == "object" && typeof n.then == "function") {
      if (e = l.alternate, e !== null && Pn(
        e,
        l,
        a,
        !0
      ), l = Ae.current, l !== null) {
        switch (l.tag) {
          case 31:
          case 13:
            return Le === null ? _i() : l.alternate === null && Ht === 0 && (Ht = 3), l.flags &= -257, l.flags |= 65536, l.lanes = a, n === Wu ? l.flags |= 16384 : (e = l.updateQueue, e === null ? l.updateQueue = /* @__PURE__ */ new Set([n]) : e.add(n), Uf(t, n, a)), !1;
          case 22:
            return l.flags |= 65536, n === Wu ? l.flags |= 16384 : (e = l.updateQueue, e === null ? (e = {
              transitions: null,
              markerInstances: null,
              retryQueue: /* @__PURE__ */ new Set([n])
            }, l.updateQueue = e) : (l = e.retryQueue, l === null ? e.retryQueue = /* @__PURE__ */ new Set([n]) : l.add(n)), Uf(t, n, a)), !1;
        }
        throw Error(r(435, l.tag));
      }
      return Uf(t, n, a), _i(), !1;
    }
    if (dt)
      return e = Ae.current, e !== null ? ((e.flags & 65536) === 0 && (e.flags |= 256), e.flags |= 65536, e.lanes = a, n !== Tc && (t = Error(r(422), { cause: n }), Za(je(t, l)))) : (n !== Tc && (e = Error(r(423), {
        cause: n
      }), Za(
        je(e, l)
      )), t = t.current.alternate, t.flags |= 65536, a &= -a, t.lanes |= a, n = je(n, l), a = cf(
        t.stateNode,
        n,
        a
      ), Hc(t, a), Ht !== 4 && (Ht = 2)), !1;
    var u = Error(r(520), { cause: n });
    if (u = je(u, l), fu === null ? fu = [u] : fu.push(u), Ht !== 4 && (Ht = 2), e === null) return !0;
    n = je(n, l), l = e;
    do {
      switch (l.tag) {
        case 3:
          return l.flags |= 65536, t = a & -a, l.lanes |= t, t = cf(l.stateNode, n, t), Hc(l, t), !1;
        case 1:
          if (e = l.type, u = l.stateNode, (l.flags & 128) === 0 && (typeof e.getDerivedStateFromError == "function" || u !== null && typeof u.componentDidCatch == "function" && (wl === null || !wl.has(u))))
            return l.flags |= 65536, a &= -a, l.lanes |= a, a = Do(a), Uo(
              a,
              t,
              l,
              n
            ), Hc(l, a), !1;
      }
      l = l.return;
    } while (l !== null);
    return !1;
  }
  var ff = Error(r(461)), Jt = !1;
  function ae(t, e, l, n) {
    e.child = t === null ? Rr(e, null, l, n) : xn(
      e,
      t.child,
      l,
      n
    );
  }
  function jo(t, e, l, n, a) {
    l = l.render;
    var u = e.ref;
    if ("ref" in n) {
      var c = {};
      for (var o in n)
        o !== "ref" && (c[o] = n[o]);
    } else c = n;
    return bn(e), n = Xc(
      t,
      e,
      l,
      c,
      u,
      a
    ), o = Zc(), t !== null && !Jt ? (Vc(t, e, a), hl(t, e, a)) : (dt && o && xc(e), e.flags |= 1, ae(t, e, n, a), e.child);
  }
  function Co(t, e, l, n, a) {
    if (t === null) {
      var u = l.type;
      return typeof u == "function" && !Sc(u) && u.defaultProps === void 0 && l.compare === null ? (e.tag = 15, e.type = u, Ro(
        t,
        e,
        u,
        n,
        a
      )) : (t = Vu(
        l.type,
        null,
        n,
        e,
        e.mode,
        a
      ), t.ref = e.ref, t.return = e, e.child = t);
    }
    if (u = t.child, !vf(t, a)) {
      var c = u.memoizedProps;
      if (l = l.compare, l = l !== null ? l : Ga, l(c, n) && t.ref === e.ref)
        return hl(t, e, a);
    }
    return e.flags |= 1, t = sl(u, n), t.ref = e.ref, t.return = e, e.child = t;
  }
  function Ro(t, e, l, n, a) {
    if (t !== null) {
      var u = t.memoizedProps;
      if (Ga(u, n) && t.ref === e.ref)
        if (Jt = !1, e.pendingProps = n = u, vf(t, a))
          (t.flags & 131072) !== 0 && (Jt = !0);
        else
          return e.lanes = t.lanes, hl(t, e, a);
    }
    return sf(
      t,
      e,
      l,
      n,
      a
    );
  }
  function Ho(t, e, l, n) {
    var a = n.children, u = t !== null ? t.memoizedState : null;
    if (t === null && e.stateNode === null && (e.stateNode = {
      _visibility: 1,
      _pendingMarkers: null,
      _retryCache: null,
      _transitions: null
    }), n.mode === "hidden") {
      if ((e.flags & 128) !== 0) {
        if (u = u !== null ? u.baseLanes | l : l, t !== null) {
          for (n = e.child = t.child, a = 0; n !== null; )
            a = a | n.lanes | n.childLanes, n = n.sibling;
          n = a & ~u;
        } else n = 0, e.child = null;
        return Lo(
          t,
          e,
          u,
          l,
          n
        );
      }
      if ((l & 536870912) !== 0)
        e.memoizedState = { baseLanes: 0, cachePool: null }, t !== null && ku(
          e,
          u !== null ? u.cachePool : null
        ), u !== null ? Br(e, u) : Bc(), Yr(e);
      else
        return n = e.lanes = 536870912, Lo(
          t,
          e,
          u !== null ? u.baseLanes | l : l,
          l,
          n
        );
    } else
      u !== null ? (ku(e, u.cachePool), Br(e, u), Ql(), e.memoizedState = null) : (t !== null && ku(e, null), Bc(), Ql());
    return ae(t, e, a, l), e.child;
  }
  function eu(t, e) {
    return t !== null && t.tag === 22 || e.stateNode !== null || (e.stateNode = {
      _visibility: 1,
      _pendingMarkers: null,
      _retryCache: null,
      _transitions: null
    }), e.sibling;
  }
  function Lo(t, e, l, n, a) {
    var u = Uc();
    return u = u === null ? null : { parent: Vt._currentValue, pool: u }, e.memoizedState = {
      baseLanes: l,
      cachePool: u
    }, t !== null && ku(e, null), Bc(), Yr(e), t !== null && Pn(t, e, n, !0), e.childLanes = a, null;
  }
  function ri(t, e) {
    return e = di(
      { mode: e.mode, children: e.children },
      t.mode
    ), e.ref = t.ref, t.child = e, e.return = t, e;
  }
  function Bo(t, e, l) {
    return xn(e, t.child, null, l), t = ri(e, e.pendingProps), t.flags |= 2, Te(e), e.memoizedState = null, t;
  }
  function th(t, e, l) {
    var n = e.pendingProps, a = (e.flags & 128) !== 0;
    if (e.flags &= -129, t === null) {
      if (dt) {
        if (n.mode === "hidden")
          return t = ri(e, n), e.lanes = 536870912, eu(null, t);
        if (Gc(e), (t = Ot) ? (t = Wd(
          t,
          He
        ), t = t !== null && t.data === "&" ? t : null, t !== null && (e.memoizedState = {
          dehydrated: t,
          treeContext: jl !== null ? { id: Ie, overflow: Pe } : null,
          retryLane: 536870912,
          hydrationErrors: null
        }, l = Sr(t), l.return = e, e.child = l, le = e, Ot = null)) : t = null, t === null) throw Rl(e);
        return e.lanes = 536870912, null;
      }
      return ri(e, n);
    }
    var u = t.memoizedState;
    if (u !== null) {
      var c = u.dehydrated;
      if (Gc(e), a)
        if (e.flags & 256)
          e.flags &= -257, e = Bo(
            t,
            e,
            l
          );
        else if (e.memoizedState !== null)
          e.child = t.child, e.flags |= 128, e = null;
        else throw Error(r(558));
      else if (Jt || Pn(t, e, l, !1), a = (l & t.childLanes) !== 0, Jt || a) {
        if (n = qt, n !== null && (c = Zt(n, l), c !== 0 && c !== u.retryLane))
          throw u.retryLane = c, hn(t, c), be(n, t, c), ff;
        _i(), e = Bo(
          t,
          e,
          l
        );
      } else
        t = u.treeContext, Ot = Be(c.nextSibling), le = e, dt = !0, Cl = null, He = !1, t !== null && xr(e, t), e = ri(e, n), e.flags |= 4096;
      return e;
    }
    return t = sl(t.child, {
      mode: n.mode,
      children: n.children
    }), t.ref = e.ref, e.child = t, t.return = e, t;
  }
  function oi(t, e) {
    var l = e.ref;
    if (l === null)
      t !== null && t.ref !== null && (e.flags |= 4194816);
    else {
      if (typeof l != "function" && typeof l != "object")
        throw Error(r(284));
      (t === null || t.ref !== l) && (e.flags |= 4194816);
    }
  }
  function sf(t, e, l, n, a) {
    return bn(e), l = Xc(
      t,
      e,
      l,
      n,
      void 0,
      a
    ), n = Zc(), t !== null && !Jt ? (Vc(t, e, a), hl(t, e, a)) : (dt && n && xc(e), e.flags |= 1, ae(t, e, l, a), e.child);
  }
  function Yo(t, e, l, n, a, u) {
    return bn(e), e.updateQueue = null, l = Qr(
      e,
      n,
      l,
      a
    ), Gr(t), n = Zc(), t !== null && !Jt ? (Vc(t, e, u), hl(t, e, u)) : (dt && n && xc(e), e.flags |= 1, ae(t, e, l, u), e.child);
  }
  function Go(t, e, l, n, a) {
    if (bn(e), e.stateNode === null) {
      var u = $n, c = l.contextType;
      typeof c == "object" && c !== null && (u = ne(c)), u = new l(n, u), e.memoizedState = u.state !== null && u.state !== void 0 ? u.state : null, u.updater = uf, e.stateNode = u, u._reactInternals = e, u = e.stateNode, u.props = n, u.state = e.memoizedState, u.refs = {}, Cc(e), c = l.contextType, u.context = typeof c == "object" && c !== null ? ne(c) : $n, u.state = e.memoizedState, c = l.getDerivedStateFromProps, typeof c == "function" && (af(
        e,
        l,
        c,
        n
      ), u.state = e.memoizedState), typeof l.getDerivedStateFromProps == "function" || typeof u.getSnapshotBeforeUpdate == "function" || typeof u.UNSAFE_componentWillMount != "function" && typeof u.componentWillMount != "function" || (c = u.state, typeof u.componentWillMount == "function" && u.componentWillMount(), typeof u.UNSAFE_componentWillMount == "function" && u.UNSAFE_componentWillMount(), c !== u.state && uf.enqueueReplaceState(u, u.state, null), Wa(e, n, u, a), $a(), u.state = e.memoizedState), typeof u.componentDidMount == "function" && (e.flags |= 4194308), n = !0;
    } else if (t === null) {
      u = e.stateNode;
      var o = e.memoizedProps, y = Tn(l, o);
      u.props = y;
      var E = u.context, O = l.contextType;
      c = $n, typeof O == "object" && O !== null && (c = ne(O));
      var U = l.getDerivedStateFromProps;
      O = typeof U == "function" || typeof u.getSnapshotBeforeUpdate == "function", o = e.pendingProps !== o, O || typeof u.UNSAFE_componentWillReceiveProps != "function" && typeof u.componentWillReceiveProps != "function" || (o || E !== c) && zo(
        e,
        u,
        n,
        c
      ), Ll = !1;
      var x = e.memoizedState;
      u.state = x, Wa(e, n, u, a), $a(), E = e.memoizedState, o || x !== E || Ll ? (typeof U == "function" && (af(
        e,
        l,
        U,
        n
      ), E = e.memoizedState), (y = Ll || To(
        e,
        l,
        y,
        n,
        x,
        E,
        c
      )) ? (O || typeof u.UNSAFE_componentWillMount != "function" && typeof u.componentWillMount != "function" || (typeof u.componentWillMount == "function" && u.componentWillMount(), typeof u.UNSAFE_componentWillMount == "function" && u.UNSAFE_componentWillMount()), typeof u.componentDidMount == "function" && (e.flags |= 4194308)) : (typeof u.componentDidMount == "function" && (e.flags |= 4194308), e.memoizedProps = n, e.memoizedState = E), u.props = n, u.state = E, u.context = c, n = y) : (typeof u.componentDidMount == "function" && (e.flags |= 4194308), n = !1);
    } else {
      u = e.stateNode, Rc(t, e), c = e.memoizedProps, O = Tn(l, c), u.props = O, U = e.pendingProps, x = u.context, E = l.contextType, y = $n, typeof E == "object" && E !== null && (y = ne(E)), o = l.getDerivedStateFromProps, (E = typeof o == "function" || typeof u.getSnapshotBeforeUpdate == "function") || typeof u.UNSAFE_componentWillReceiveProps != "function" && typeof u.componentWillReceiveProps != "function" || (c !== U || x !== y) && zo(
        e,
        u,
        n,
        y
      ), Ll = !1, x = e.memoizedState, u.state = x, Wa(e, n, u, a), $a();
      var z = e.memoizedState;
      c !== U || x !== z || Ll || t !== null && t.dependencies !== null && Ju(t.dependencies) ? (typeof o == "function" && (af(
        e,
        l,
        o,
        n
      ), z = e.memoizedState), (O = Ll || To(
        e,
        l,
        O,
        n,
        x,
        z,
        y
      ) || t !== null && t.dependencies !== null && Ju(t.dependencies)) ? (E || typeof u.UNSAFE_componentWillUpdate != "function" && typeof u.componentWillUpdate != "function" || (typeof u.componentWillUpdate == "function" && u.componentWillUpdate(n, z, y), typeof u.UNSAFE_componentWillUpdate == "function" && u.UNSAFE_componentWillUpdate(
        n,
        z,
        y
      )), typeof u.componentDidUpdate == "function" && (e.flags |= 4), typeof u.getSnapshotBeforeUpdate == "function" && (e.flags |= 1024)) : (typeof u.componentDidUpdate != "function" || c === t.memoizedProps && x === t.memoizedState || (e.flags |= 4), typeof u.getSnapshotBeforeUpdate != "function" || c === t.memoizedProps && x === t.memoizedState || (e.flags |= 1024), e.memoizedProps = n, e.memoizedState = z), u.props = n, u.state = z, u.context = y, n = O) : (typeof u.componentDidUpdate != "function" || c === t.memoizedProps && x === t.memoizedState || (e.flags |= 4), typeof u.getSnapshotBeforeUpdate != "function" || c === t.memoizedProps && x === t.memoizedState || (e.flags |= 1024), n = !1);
    }
    return u = n, oi(t, e), n = (e.flags & 128) !== 0, u || n ? (u = e.stateNode, l = n && typeof l.getDerivedStateFromError != "function" ? null : u.render(), e.flags |= 1, t !== null && n ? (e.child = xn(
      e,
      t.child,
      null,
      a
    ), e.child = xn(
      e,
      null,
      l,
      a
    )) : ae(t, e, l, a), e.memoizedState = u.state, t = e.child) : t = hl(
      t,
      e,
      a
    ), t;
  }
  function Qo(t, e, l, n) {
    return gn(), e.flags |= 256, ae(t, e, l, n), e.child;
  }
  var rf = {
    dehydrated: null,
    treeContext: null,
    retryLane: 0,
    hydrationErrors: null
  };
  function of(t) {
    return { baseLanes: t, cachePool: Or() };
  }
  function df(t, e, l) {
    return t = t !== null ? t.childLanes & ~l : 0, e && (t |= qe), t;
  }
  function Xo(t, e, l) {
    var n = e.pendingProps, a = !1, u = (e.flags & 128) !== 0, c;
    if ((c = u) || (c = t !== null && t.memoizedState === null ? !1 : (Bt.current & 2) !== 0), c && (a = !0, e.flags &= -129), c = (e.flags & 32) !== 0, e.flags &= -33, t === null) {
      if (dt) {
        if (a ? Gl(e) : Ql(), (t = Ot) ? (t = Wd(
          t,
          He
        ), t = t !== null && t.data !== "&" ? t : null, t !== null && (e.memoizedState = {
          dehydrated: t,
          treeContext: jl !== null ? { id: Ie, overflow: Pe } : null,
          retryLane: 536870912,
          hydrationErrors: null
        }, l = Sr(t), l.return = e, e.child = l, le = e, Ot = null)) : t = null, t === null) throw Rl(e);
        return kf(t) ? e.lanes = 32 : e.lanes = 536870912, null;
      }
      var o = n.children;
      return n = n.fallback, a ? (Ql(), a = e.mode, o = di(
        { mode: "hidden", children: o },
        a
      ), n = vn(
        n,
        a,
        l,
        null
      ), o.return = e, n.return = e, o.sibling = n, e.child = o, n = e.child, n.memoizedState = of(l), n.childLanes = df(
        t,
        c,
        l
      ), e.memoizedState = rf, eu(null, n)) : (Gl(e), yf(e, o));
    }
    var y = t.memoizedState;
    if (y !== null && (o = y.dehydrated, o !== null)) {
      if (u)
        e.flags & 256 ? (Gl(e), e.flags &= -257, e = mf(
          t,
          e,
          l
        )) : e.memoizedState !== null ? (Ql(), e.child = t.child, e.flags |= 128, e = null) : (Ql(), o = n.fallback, a = e.mode, n = di(
          { mode: "visible", children: n.children },
          a
        ), o = vn(
          o,
          a,
          l,
          null
        ), o.flags |= 2, n.return = e, o.return = e, n.sibling = o, e.child = n, xn(
          e,
          t.child,
          null,
          l
        ), n = e.child, n.memoizedState = of(l), n.childLanes = df(
          t,
          c,
          l
        ), e.memoizedState = rf, e = eu(null, n));
      else if (Gl(e), kf(o)) {
        if (c = o.nextSibling && o.nextSibling.dataset, c) var E = c.dgst;
        c = E, n = Error(r(419)), n.stack = "", n.digest = c, Za({ value: n, source: null, stack: null }), e = mf(
          t,
          e,
          l
        );
      } else if (Jt || Pn(t, e, l, !1), c = (l & t.childLanes) !== 0, Jt || c) {
        if (c = qt, c !== null && (n = Zt(c, l), n !== 0 && n !== y.retryLane))
          throw y.retryLane = n, hn(t, n), be(c, t, n), ff;
        Kf(o) || _i(), e = mf(
          t,
          e,
          l
        );
      } else
        Kf(o) ? (e.flags |= 192, e.child = t.child, e = null) : (t = y.treeContext, Ot = Be(
          o.nextSibling
        ), le = e, dt = !0, Cl = null, He = !1, t !== null && xr(e, t), e = yf(
          e,
          n.children
        ), e.flags |= 4096);
      return e;
    }
    return a ? (Ql(), o = n.fallback, a = e.mode, y = t.child, E = y.sibling, n = sl(y, {
      mode: "hidden",
      children: n.children
    }), n.subtreeFlags = y.subtreeFlags & 65011712, E !== null ? o = sl(
      E,
      o
    ) : (o = vn(
      o,
      a,
      l,
      null
    ), o.flags |= 2), o.return = e, n.return = e, n.sibling = o, e.child = n, eu(null, n), n = e.child, o = t.child.memoizedState, o === null ? o = of(l) : (a = o.cachePool, a !== null ? (y = Vt._currentValue, a = a.parent !== y ? { parent: y, pool: y } : a) : a = Or(), o = {
      baseLanes: o.baseLanes | l,
      cachePool: a
    }), n.memoizedState = o, n.childLanes = df(
      t,
      c,
      l
    ), e.memoizedState = rf, eu(t.child, n)) : (Gl(e), l = t.child, t = l.sibling, l = sl(l, {
      mode: "visible",
      children: n.children
    }), l.return = e, l.sibling = null, t !== null && (c = e.deletions, c === null ? (e.deletions = [t], e.flags |= 16) : c.push(t)), e.child = l, e.memoizedState = null, l);
  }
  function yf(t, e) {
    return e = di(
      { mode: "visible", children: e },
      t.mode
    ), e.return = t, t.child = e;
  }
  function di(t, e) {
    return t = xe(22, t, null, e), t.lanes = 0, t;
  }
  function mf(t, e, l) {
    return xn(e, t.child, null, l), t = yf(
      e,
      e.pendingProps.children
    ), t.flags |= 2, e.memoizedState = null, t;
  }
  function Zo(t, e, l) {
    t.lanes |= e;
    var n = t.alternate;
    n !== null && (n.lanes |= e), Nc(t.return, e, l);
  }
  function hf(t, e, l, n, a, u) {
    var c = t.memoizedState;
    c === null ? t.memoizedState = {
      isBackwards: e,
      rendering: null,
      renderingStartTime: 0,
      last: n,
      tail: l,
      tailMode: a,
      treeForkCount: u
    } : (c.isBackwards = e, c.rendering = null, c.renderingStartTime = 0, c.last = n, c.tail = l, c.tailMode = a, c.treeForkCount = u);
  }
  function Vo(t, e, l) {
    var n = e.pendingProps, a = n.revealOrder, u = n.tail;
    n = n.children;
    var c = Bt.current, o = (c & 2) !== 0;
    if (o ? (c = c & 1 | 2, e.flags |= 128) : c &= 1, L(Bt, c), ae(t, e, n, l), n = dt ? Xa : 0, !o && t !== null && (t.flags & 128) !== 0)
      t: for (t = e.child; t !== null; ) {
        if (t.tag === 13)
          t.memoizedState !== null && Zo(t, l, e);
        else if (t.tag === 19)
          Zo(t, l, e);
        else if (t.child !== null) {
          t.child.return = t, t = t.child;
          continue;
        }
        if (t === e) break t;
        for (; t.sibling === null; ) {
          if (t.return === null || t.return === e)
            break t;
          t = t.return;
        }
        t.sibling.return = t.return, t = t.sibling;
      }
    switch (a) {
      case "forwards":
        for (l = e.child, a = null; l !== null; )
          t = l.alternate, t !== null && ti(t) === null && (a = l), l = l.sibling;
        l = a, l === null ? (a = e.child, e.child = null) : (a = l.sibling, l.sibling = null), hf(
          e,
          !1,
          a,
          l,
          u,
          n
        );
        break;
      case "backwards":
      case "unstable_legacy-backwards":
        for (l = null, a = e.child, e.child = null; a !== null; ) {
          if (t = a.alternate, t !== null && ti(t) === null) {
            e.child = a;
            break;
          }
          t = a.sibling, a.sibling = l, l = a, a = t;
        }
        hf(
          e,
          !0,
          l,
          null,
          u,
          n
        );
        break;
      case "together":
        hf(
          e,
          !1,
          null,
          null,
          void 0,
          n
        );
        break;
      default:
        e.memoizedState = null;
    }
    return e.child;
  }
  function hl(t, e, l) {
    if (t !== null && (e.dependencies = t.dependencies), Vl |= e.lanes, (l & e.childLanes) === 0)
      if (t !== null) {
        if (Pn(
          t,
          e,
          l,
          !1
        ), (l & e.childLanes) === 0)
          return null;
      } else return null;
    if (t !== null && e.child !== t.child)
      throw Error(r(153));
    if (e.child !== null) {
      for (t = e.child, l = sl(t, t.pendingProps), e.child = l, l.return = e; t.sibling !== null; )
        t = t.sibling, l = l.sibling = sl(t, t.pendingProps), l.return = e;
      l.sibling = null;
    }
    return e.child;
  }
  function vf(t, e) {
    return (t.lanes & e) !== 0 ? !0 : (t = t.dependencies, !!(t !== null && Ju(t)));
  }
  function eh(t, e, l) {
    switch (e.tag) {
      case 3:
        Xt(e, e.stateNode.containerInfo), Hl(e, Vt, t.memoizedState.cache), gn();
        break;
      case 27:
      case 5:
        ke(e);
        break;
      case 4:
        Xt(e, e.stateNode.containerInfo);
        break;
      case 10:
        Hl(
          e,
          e.type,
          e.memoizedProps.value
        );
        break;
      case 31:
        if (e.memoizedState !== null)
          return e.flags |= 128, Gc(e), null;
        break;
      case 13:
        var n = e.memoizedState;
        if (n !== null)
          return n.dehydrated !== null ? (Gl(e), e.flags |= 128, null) : (l & e.child.childLanes) !== 0 ? Xo(t, e, l) : (Gl(e), t = hl(
            t,
            e,
            l
          ), t !== null ? t.sibling : null);
        Gl(e);
        break;
      case 19:
        var a = (t.flags & 128) !== 0;
        if (n = (l & e.childLanes) !== 0, n || (Pn(
          t,
          e,
          l,
          !1
        ), n = (l & e.childLanes) !== 0), a) {
          if (n)
            return Vo(
              t,
              e,
              l
            );
          e.flags |= 128;
        }
        if (a = e.memoizedState, a !== null && (a.rendering = null, a.tail = null, a.lastEffect = null), L(Bt, Bt.current), n) break;
        return null;
      case 22:
        return e.lanes = 0, Ho(
          t,
          e,
          l,
          e.pendingProps
        );
      case 24:
        Hl(e, Vt, t.memoizedState.cache);
    }
    return hl(t, e, l);
  }
  function wo(t, e, l) {
    if (t !== null)
      if (t.memoizedProps !== e.pendingProps)
        Jt = !0;
      else {
        if (!vf(t, l) && (e.flags & 128) === 0)
          return Jt = !1, eh(
            t,
            e,
            l
          );
        Jt = (t.flags & 131072) !== 0;
      }
    else
      Jt = !1, dt && (e.flags & 1048576) !== 0 && Er(e, Xa, e.index);
    switch (e.lanes = 0, e.tag) {
      case 16:
        t: {
          var n = e.pendingProps;
          if (t = _n(e.elementType), e.type = t, typeof t == "function")
            Sc(t) ? (n = Tn(t, n), e.tag = 1, e = Go(
              null,
              e,
              t,
              n,
              l
            )) : (e.tag = 0, e = sf(
              null,
              e,
              t,
              n,
              l
            ));
          else {
            if (t != null) {
              var a = t.$$typeof;
              if (a === ct) {
                e.tag = 11, e = jo(
                  null,
                  e,
                  t,
                  n,
                  l
                );
                break t;
              } else if (a === nt) {
                e.tag = 14, e = Co(
                  null,
                  e,
                  t,
                  n,
                  l
                );
                break t;
              }
            }
            throw e = Qe(t) || t, Error(r(306, e, ""));
          }
        }
        return e;
      case 0:
        return sf(
          t,
          e,
          e.type,
          e.pendingProps,
          l
        );
      case 1:
        return n = e.type, a = Tn(
          n,
          e.pendingProps
        ), Go(
          t,
          e,
          n,
          a,
          l
        );
      case 3:
        t: {
          if (Xt(
            e,
            e.stateNode.containerInfo
          ), t === null) throw Error(r(387));
          n = e.pendingProps;
          var u = e.memoizedState;
          a = u.element, Rc(t, e), Wa(e, n, null, l);
          var c = e.memoizedState;
          if (n = c.cache, Hl(e, Vt, n), n !== u.cache && Oc(
            e,
            [Vt],
            l,
            !0
          ), $a(), n = c.element, u.isDehydrated)
            if (u = {
              element: n,
              isDehydrated: !1,
              cache: c.cache
            }, e.updateQueue.baseState = u, e.memoizedState = u, e.flags & 256) {
              e = Qo(
                t,
                e,
                n,
                l
              );
              break t;
            } else if (n !== a) {
              a = je(
                Error(r(424)),
                e
              ), Za(a), e = Qo(
                t,
                e,
                n,
                l
              );
              break t;
            } else {
              switch (t = e.stateNode.containerInfo, t.nodeType) {
                case 9:
                  t = t.body;
                  break;
                default:
                  t = t.nodeName === "HTML" ? t.ownerDocument.body : t;
              }
              for (Ot = Be(t.firstChild), le = e, dt = !0, Cl = null, He = !0, l = Rr(
                e,
                null,
                n,
                l
              ), e.child = l; l; )
                l.flags = l.flags & -3 | 4096, l = l.sibling;
            }
          else {
            if (gn(), n === a) {
              e = hl(
                t,
                e,
                l
              );
              break t;
            }
            ae(t, e, n, l);
          }
          e = e.child;
        }
        return e;
      case 26:
        return oi(t, e), t === null ? (l = ly(
          e.type,
          null,
          e.pendingProps,
          null
        )) ? e.memoizedState = l : dt || (l = e.type, t = e.pendingProps, n = Ni(
          at.current
        ).createElement(l), n[ee] = e, n[ye] = t, ue(n, l, t), It(n), e.stateNode = n) : e.memoizedState = ly(
          e.type,
          t.memoizedProps,
          e.pendingProps,
          t.memoizedState
        ), null;
      case 27:
        return ke(e), t === null && dt && (n = e.stateNode = Pd(
          e.type,
          e.pendingProps,
          at.current
        ), le = e, He = !0, a = Ot, $l(e.type) ? ($f = a, Ot = Be(n.firstChild)) : Ot = a), ae(
          t,
          e,
          e.pendingProps.children,
          l
        ), oi(t, e), t === null && (e.flags |= 4194304), e.child;
      case 5:
        return t === null && dt && ((a = n = Ot) && (n = Dh(
          n,
          e.type,
          e.pendingProps,
          He
        ), n !== null ? (e.stateNode = n, le = e, Ot = Be(n.firstChild), He = !1, a = !0) : a = !1), a || Rl(e)), ke(e), a = e.type, u = e.pendingProps, c = t !== null ? t.memoizedProps : null, n = u.children, Vf(a, u) ? n = null : c !== null && Vf(a, c) && (e.flags |= 32), e.memoizedState !== null && (a = Xc(
          t,
          e,
          Jm,
          null,
          null,
          l
        ), vu._currentValue = a), oi(t, e), ae(t, e, n, l), e.child;
      case 6:
        return t === null && dt && ((t = l = Ot) && (l = Uh(
          l,
          e.pendingProps,
          He
        ), l !== null ? (e.stateNode = l, le = e, Ot = null, t = !0) : t = !1), t || Rl(e)), null;
      case 13:
        return Xo(t, e, l);
      case 4:
        return Xt(
          e,
          e.stateNode.containerInfo
        ), n = e.pendingProps, t === null ? e.child = xn(
          e,
          null,
          n,
          l
        ) : ae(t, e, n, l), e.child;
      case 11:
        return jo(
          t,
          e,
          e.type,
          e.pendingProps,
          l
        );
      case 7:
        return ae(
          t,
          e,
          e.pendingProps,
          l
        ), e.child;
      case 8:
        return ae(
          t,
          e,
          e.pendingProps.children,
          l
        ), e.child;
      case 12:
        return ae(
          t,
          e,
          e.pendingProps.children,
          l
        ), e.child;
      case 10:
        return n = e.pendingProps, Hl(e, e.type, n.value), ae(t, e, n.children, l), e.child;
      case 9:
        return a = e.type._context, n = e.pendingProps.children, bn(e), a = ne(a), n = n(a), e.flags |= 1, ae(t, e, n, l), e.child;
      case 14:
        return Co(
          t,
          e,
          e.type,
          e.pendingProps,
          l
        );
      case 15:
        return Ro(
          t,
          e,
          e.type,
          e.pendingProps,
          l
        );
      case 19:
        return Vo(t, e, l);
      case 31:
        return th(t, e, l);
      case 22:
        return Ho(
          t,
          e,
          l,
          e.pendingProps
        );
      case 24:
        return bn(e), n = ne(Vt), t === null ? (a = Uc(), a === null && (a = qt, u = Mc(), a.pooledCache = u, u.refCount++, u !== null && (a.pooledCacheLanes |= l), a = u), e.memoizedState = { parent: n, cache: a }, Cc(e), Hl(e, Vt, a)) : ((t.lanes & l) !== 0 && (Rc(t, e), Wa(e, null, null, l), $a()), a = t.memoizedState, u = e.memoizedState, a.parent !== n ? (a = { parent: n, cache: n }, e.memoizedState = a, e.lanes === 0 && (e.memoizedState = e.updateQueue.baseState = a), Hl(e, Vt, n)) : (n = u.cache, Hl(e, Vt, n), n !== a.cache && Oc(
          e,
          [Vt],
          l,
          !0
        ))), ae(
          t,
          e,
          e.pendingProps.children,
          l
        ), e.child;
      case 29:
        throw e.pendingProps;
    }
    throw Error(r(156, e.tag));
  }
  function vl(t) {
    t.flags |= 4;
  }
  function gf(t, e, l, n, a) {
    if ((e = (t.mode & 32) !== 0) && (e = !1), e) {
      if (t.flags |= 16777216, (a & 335544128) === a)
        if (t.stateNode.complete) t.flags |= 8192;
        else if (pd()) t.flags |= 8192;
        else
          throw En = Wu, jc;
    } else t.flags &= -16777217;
  }
  function Jo(t, e) {
    if (e.type !== "stylesheet" || (e.state.loading & 4) !== 0)
      t.flags &= -16777217;
    else if (t.flags |= 16777216, !cy(e))
      if (pd()) t.flags |= 8192;
      else
        throw En = Wu, jc;
  }
  function yi(t, e) {
    e !== null && (t.flags |= 4), t.flags & 16384 && (e = t.tag !== 22 ? rn() : 536870912, t.lanes |= e, oa |= e);
  }
  function lu(t, e) {
    if (!dt)
      switch (t.tailMode) {
        case "hidden":
          e = t.tail;
          for (var l = null; e !== null; )
            e.alternate !== null && (l = e), e = e.sibling;
          l === null ? t.tail = null : l.sibling = null;
          break;
        case "collapsed":
          l = t.tail;
          for (var n = null; l !== null; )
            l.alternate !== null && (n = l), l = l.sibling;
          n === null ? e || t.tail === null ? t.tail = null : t.tail.sibling = null : n.sibling = null;
      }
  }
  function Mt(t) {
    var e = t.alternate !== null && t.alternate.child === t.child, l = 0, n = 0;
    if (e)
      for (var a = t.child; a !== null; )
        l |= a.lanes | a.childLanes, n |= a.subtreeFlags & 65011712, n |= a.flags & 65011712, a.return = t, a = a.sibling;
    else
      for (a = t.child; a !== null; )
        l |= a.lanes | a.childLanes, n |= a.subtreeFlags, n |= a.flags, a.return = t, a = a.sibling;
    return t.subtreeFlags |= n, t.childLanes = l, e;
  }
  function lh(t, e, l) {
    var n = e.pendingProps;
    switch (Ac(e), e.tag) {
      case 16:
      case 15:
      case 0:
      case 11:
      case 7:
      case 8:
      case 12:
      case 9:
      case 14:
        return Mt(e), null;
      case 1:
        return Mt(e), null;
      case 3:
        return l = e.stateNode, n = null, t !== null && (n = t.memoizedState.cache), e.memoizedState.cache !== n && (e.flags |= 2048), dl(Vt), Ct(), l.pendingContext && (l.context = l.pendingContext, l.pendingContext = null), (t === null || t.child === null) && (In(e) ? vl(e) : t === null || t.memoizedState.isDehydrated && (e.flags & 256) === 0 || (e.flags |= 1024, zc())), Mt(e), null;
      case 26:
        var a = e.type, u = e.memoizedState;
        return t === null ? (vl(e), u !== null ? (Mt(e), Jo(e, u)) : (Mt(e), gf(
          e,
          a,
          null,
          n,
          l
        ))) : u ? u !== t.memoizedState ? (vl(e), Mt(e), Jo(e, u)) : (Mt(e), e.flags &= -16777217) : (t = t.memoizedProps, t !== n && vl(e), Mt(e), gf(
          e,
          a,
          t,
          n,
          l
        )), null;
      case 27:
        if (oe(e), l = at.current, a = e.type, t !== null && e.stateNode != null)
          t.memoizedProps !== n && vl(e);
        else {
          if (!n) {
            if (e.stateNode === null)
              throw Error(r(166));
            return Mt(e), null;
          }
          t = Q.current, In(e) ? Ar(e) : (t = Pd(a, n, l), e.stateNode = t, vl(e));
        }
        return Mt(e), null;
      case 5:
        if (oe(e), a = e.type, t !== null && e.stateNode != null)
          t.memoizedProps !== n && vl(e);
        else {
          if (!n) {
            if (e.stateNode === null)
              throw Error(r(166));
            return Mt(e), null;
          }
          if (u = Q.current, In(e))
            Ar(e);
          else {
            var c = Ni(
              at.current
            );
            switch (u) {
              case 1:
                u = c.createElementNS(
                  "http://www.w3.org/2000/svg",
                  a
                );
                break;
              case 2:
                u = c.createElementNS(
                  "http://www.w3.org/1998/Math/MathML",
                  a
                );
                break;
              default:
                switch (a) {
                  case "svg":
                    u = c.createElementNS(
                      "http://www.w3.org/2000/svg",
                      a
                    );
                    break;
                  case "math":
                    u = c.createElementNS(
                      "http://www.w3.org/1998/Math/MathML",
                      a
                    );
                    break;
                  case "script":
                    u = c.createElement("div"), u.innerHTML = "<script><\/script>", u = u.removeChild(
                      u.firstChild
                    );
                    break;
                  case "select":
                    u = typeof n.is == "string" ? c.createElement("select", {
                      is: n.is
                    }) : c.createElement("select"), n.multiple ? u.multiple = !0 : n.size && (u.size = n.size);
                    break;
                  default:
                    u = typeof n.is == "string" ? c.createElement(a, { is: n.is }) : c.createElement(a);
                }
            }
            u[ee] = e, u[ye] = n;
            t: for (c = e.child; c !== null; ) {
              if (c.tag === 5 || c.tag === 6)
                u.appendChild(c.stateNode);
              else if (c.tag !== 4 && c.tag !== 27 && c.child !== null) {
                c.child.return = c, c = c.child;
                continue;
              }
              if (c === e) break t;
              for (; c.sibling === null; ) {
                if (c.return === null || c.return === e)
                  break t;
                c = c.return;
              }
              c.sibling.return = c.return, c = c.sibling;
            }
            e.stateNode = u;
            t: switch (ue(u, a, n), a) {
              case "button":
              case "input":
              case "select":
              case "textarea":
                n = !!n.autoFocus;
                break t;
              case "img":
                n = !0;
                break t;
              default:
                n = !1;
            }
            n && vl(e);
          }
        }
        return Mt(e), gf(
          e,
          e.type,
          t === null ? null : t.memoizedProps,
          e.pendingProps,
          l
        ), null;
      case 6:
        if (t && e.stateNode != null)
          t.memoizedProps !== n && vl(e);
        else {
          if (typeof n != "string" && e.stateNode === null)
            throw Error(r(166));
          if (t = at.current, In(e)) {
            if (t = e.stateNode, l = e.memoizedProps, n = null, a = le, a !== null)
              switch (a.tag) {
                case 27:
                case 5:
                  n = a.memoizedProps;
              }
            t[ee] = e, t = !!(t.nodeValue === l || n !== null && n.suppressHydrationWarning === !0 || Xd(t.nodeValue, l)), t || Rl(e, !0);
          } else
            t = Ni(t).createTextNode(
              n
            ), t[ee] = e, e.stateNode = t;
        }
        return Mt(e), null;
      case 31:
        if (l = e.memoizedState, t === null || t.memoizedState !== null) {
          if (n = In(e), l !== null) {
            if (t === null) {
              if (!n) throw Error(r(318));
              if (t = e.memoizedState, t = t !== null ? t.dehydrated : null, !t) throw Error(r(557));
              t[ee] = e;
            } else
              gn(), (e.flags & 128) === 0 && (e.memoizedState = null), e.flags |= 4;
            Mt(e), t = !1;
          } else
            l = zc(), t !== null && t.memoizedState !== null && (t.memoizedState.hydrationErrors = l), t = !0;
          if (!t)
            return e.flags & 256 ? (Te(e), e) : (Te(e), null);
          if ((e.flags & 128) !== 0)
            throw Error(r(558));
        }
        return Mt(e), null;
      case 13:
        if (n = e.memoizedState, t === null || t.memoizedState !== null && t.memoizedState.dehydrated !== null) {
          if (a = In(e), n !== null && n.dehydrated !== null) {
            if (t === null) {
              if (!a) throw Error(r(318));
              if (a = e.memoizedState, a = a !== null ? a.dehydrated : null, !a) throw Error(r(317));
              a[ee] = e;
            } else
              gn(), (e.flags & 128) === 0 && (e.memoizedState = null), e.flags |= 4;
            Mt(e), a = !1;
          } else
            a = zc(), t !== null && t.memoizedState !== null && (t.memoizedState.hydrationErrors = a), a = !0;
          if (!a)
            return e.flags & 256 ? (Te(e), e) : (Te(e), null);
        }
        return Te(e), (e.flags & 128) !== 0 ? (e.lanes = l, e) : (l = n !== null, t = t !== null && t.memoizedState !== null, l && (n = e.child, a = null, n.alternate !== null && n.alternate.memoizedState !== null && n.alternate.memoizedState.cachePool !== null && (a = n.alternate.memoizedState.cachePool.pool), u = null, n.memoizedState !== null && n.memoizedState.cachePool !== null && (u = n.memoizedState.cachePool.pool), u !== a && (n.flags |= 2048)), l !== t && l && (e.child.flags |= 8192), yi(e, e.updateQueue), Mt(e), null);
      case 4:
        return Ct(), t === null && Yf(e.stateNode.containerInfo), Mt(e), null;
      case 10:
        return dl(e.type), Mt(e), null;
      case 19:
        if (D(Bt), n = e.memoizedState, n === null) return Mt(e), null;
        if (a = (e.flags & 128) !== 0, u = n.rendering, u === null)
          if (a) lu(n, !1);
          else {
            if (Ht !== 0 || t !== null && (t.flags & 128) !== 0)
              for (t = e.child; t !== null; ) {
                if (u = ti(t), u !== null) {
                  for (e.flags |= 128, lu(n, !1), t = u.updateQueue, e.updateQueue = t, yi(e, t), e.subtreeFlags = 0, t = l, l = e.child; l !== null; )
                    br(l, t), l = l.sibling;
                  return L(
                    Bt,
                    Bt.current & 1 | 2
                  ), dt && rl(e, n.treeForkCount), e.child;
                }
                t = t.sibling;
              }
            n.tail !== null && Nt() > pi && (e.flags |= 128, a = !0, lu(n, !1), e.lanes = 4194304);
          }
        else {
          if (!a)
            if (t = ti(u), t !== null) {
              if (e.flags |= 128, a = !0, t = t.updateQueue, e.updateQueue = t, yi(e, t), lu(n, !0), n.tail === null && n.tailMode === "hidden" && !u.alternate && !dt)
                return Mt(e), null;
            } else
              2 * Nt() - n.renderingStartTime > pi && l !== 536870912 && (e.flags |= 128, a = !0, lu(n, !1), e.lanes = 4194304);
          n.isBackwards ? (u.sibling = e.child, e.child = u) : (t = n.last, t !== null ? t.sibling = u : e.child = u, n.last = u);
        }
        return n.tail !== null ? (t = n.tail, n.rendering = t, n.tail = t.sibling, n.renderingStartTime = Nt(), t.sibling = null, l = Bt.current, L(
          Bt,
          a ? l & 1 | 2 : l & 1
        ), dt && rl(e, n.treeForkCount), t) : (Mt(e), null);
      case 22:
      case 23:
        return Te(e), Yc(), n = e.memoizedState !== null, t !== null ? t.memoizedState !== null !== n && (e.flags |= 8192) : n && (e.flags |= 8192), n ? (l & 536870912) !== 0 && (e.flags & 128) === 0 && (Mt(e), e.subtreeFlags & 6 && (e.flags |= 8192)) : Mt(e), l = e.updateQueue, l !== null && yi(e, l.retryQueue), l = null, t !== null && t.memoizedState !== null && t.memoizedState.cachePool !== null && (l = t.memoizedState.cachePool.pool), n = null, e.memoizedState !== null && e.memoizedState.cachePool !== null && (n = e.memoizedState.cachePool.pool), n !== l && (e.flags |= 2048), t !== null && D(Sn), null;
      case 24:
        return l = null, t !== null && (l = t.memoizedState.cache), e.memoizedState.cache !== l && (e.flags |= 2048), dl(Vt), Mt(e), null;
      case 25:
        return null;
      case 30:
        return null;
    }
    throw Error(r(156, e.tag));
  }
  function nh(t, e) {
    switch (Ac(e), e.tag) {
      case 1:
        return t = e.flags, t & 65536 ? (e.flags = t & -65537 | 128, e) : null;
      case 3:
        return dl(Vt), Ct(), t = e.flags, (t & 65536) !== 0 && (t & 128) === 0 ? (e.flags = t & -65537 | 128, e) : null;
      case 26:
      case 27:
      case 5:
        return oe(e), null;
      case 31:
        if (e.memoizedState !== null) {
          if (Te(e), e.alternate === null)
            throw Error(r(340));
          gn();
        }
        return t = e.flags, t & 65536 ? (e.flags = t & -65537 | 128, e) : null;
      case 13:
        if (Te(e), t = e.memoizedState, t !== null && t.dehydrated !== null) {
          if (e.alternate === null)
            throw Error(r(340));
          gn();
        }
        return t = e.flags, t & 65536 ? (e.flags = t & -65537 | 128, e) : null;
      case 19:
        return D(Bt), null;
      case 4:
        return Ct(), null;
      case 10:
        return dl(e.type), null;
      case 22:
      case 23:
        return Te(e), Yc(), t !== null && D(Sn), t = e.flags, t & 65536 ? (e.flags = t & -65537 | 128, e) : null;
      case 24:
        return dl(Vt), null;
      case 25:
        return null;
      default:
        return null;
    }
  }
  function Ko(t, e) {
    switch (Ac(e), e.tag) {
      case 3:
        dl(Vt), Ct();
        break;
      case 26:
      case 27:
      case 5:
        oe(e);
        break;
      case 4:
        Ct();
        break;
      case 31:
        e.memoizedState !== null && Te(e);
        break;
      case 13:
        Te(e);
        break;
      case 19:
        D(Bt);
        break;
      case 10:
        dl(e.type);
        break;
      case 22:
      case 23:
        Te(e), Yc(), t !== null && D(Sn);
        break;
      case 24:
        dl(Vt);
    }
  }
  function nu(t, e) {
    try {
      var l = e.updateQueue, n = l !== null ? l.lastEffect : null;
      if (n !== null) {
        var a = n.next;
        l = a;
        do {
          if ((l.tag & t) === t) {
            n = void 0;
            var u = l.create, c = l.inst;
            n = u(), c.destroy = n;
          }
          l = l.next;
        } while (l !== a);
      }
    } catch (o) {
      xt(e, e.return, o);
    }
  }
  function Xl(t, e, l) {
    try {
      var n = e.updateQueue, a = n !== null ? n.lastEffect : null;
      if (a !== null) {
        var u = a.next;
        n = u;
        do {
          if ((n.tag & t) === t) {
            var c = n.inst, o = c.destroy;
            if (o !== void 0) {
              c.destroy = void 0, a = e;
              var y = l, E = o;
              try {
                E();
              } catch (O) {
                xt(
                  a,
                  y,
                  O
                );
              }
            }
          }
          n = n.next;
        } while (n !== u);
      }
    } catch (O) {
      xt(e, e.return, O);
    }
  }
  function ko(t) {
    var e = t.updateQueue;
    if (e !== null) {
      var l = t.stateNode;
      try {
        Lr(e, l);
      } catch (n) {
        xt(t, t.return, n);
      }
    }
  }
  function $o(t, e, l) {
    l.props = Tn(
      t.type,
      t.memoizedProps
    ), l.state = t.memoizedState;
    try {
      l.componentWillUnmount();
    } catch (n) {
      xt(t, e, n);
    }
  }
  function au(t, e) {
    try {
      var l = t.ref;
      if (l !== null) {
        switch (t.tag) {
          case 26:
          case 27:
          case 5:
            var n = t.stateNode;
            break;
          case 30:
            n = t.stateNode;
            break;
          default:
            n = t.stateNode;
        }
        typeof l == "function" ? t.refCleanup = l(n) : l.current = n;
      }
    } catch (a) {
      xt(t, e, a);
    }
  }
  function tl(t, e) {
    var l = t.ref, n = t.refCleanup;
    if (l !== null)
      if (typeof n == "function")
        try {
          n();
        } catch (a) {
          xt(t, e, a);
        } finally {
          t.refCleanup = null, t = t.alternate, t != null && (t.refCleanup = null);
        }
      else if (typeof l == "function")
        try {
          l(null);
        } catch (a) {
          xt(t, e, a);
        }
      else l.current = null;
  }
  function Wo(t) {
    var e = t.type, l = t.memoizedProps, n = t.stateNode;
    try {
      t: switch (e) {
        case "button":
        case "input":
        case "select":
        case "textarea":
          l.autoFocus && n.focus();
          break t;
        case "img":
          l.src ? n.src = l.src : l.srcSet && (n.srcset = l.srcSet);
      }
    } catch (a) {
      xt(t, t.return, a);
    }
  }
  function pf(t, e, l) {
    try {
      var n = t.stateNode;
      Th(n, t.type, l, e), n[ye] = e;
    } catch (a) {
      xt(t, t.return, a);
    }
  }
  function Fo(t) {
    return t.tag === 5 || t.tag === 3 || t.tag === 26 || t.tag === 27 && $l(t.type) || t.tag === 4;
  }
  function bf(t) {
    t: for (; ; ) {
      for (; t.sibling === null; ) {
        if (t.return === null || Fo(t.return)) return null;
        t = t.return;
      }
      for (t.sibling.return = t.return, t = t.sibling; t.tag !== 5 && t.tag !== 6 && t.tag !== 18; ) {
        if (t.tag === 27 && $l(t.type) || t.flags & 2 || t.child === null || t.tag === 4) continue t;
        t.child.return = t, t = t.child;
      }
      if (!(t.flags & 2)) return t.stateNode;
    }
  }
  function Sf(t, e, l) {
    var n = t.tag;
    if (n === 5 || n === 6)
      t = t.stateNode, e ? (l.nodeType === 9 ? l.body : l.nodeName === "HTML" ? l.ownerDocument.body : l).insertBefore(t, e) : (e = l.nodeType === 9 ? l.body : l.nodeName === "HTML" ? l.ownerDocument.body : l, e.appendChild(t), l = l._reactRootContainer, l != null || e.onclick !== null || (e.onclick = cl));
    else if (n !== 4 && (n === 27 && $l(t.type) && (l = t.stateNode, e = null), t = t.child, t !== null))
      for (Sf(t, e, l), t = t.sibling; t !== null; )
        Sf(t, e, l), t = t.sibling;
  }
  function mi(t, e, l) {
    var n = t.tag;
    if (n === 5 || n === 6)
      t = t.stateNode, e ? l.insertBefore(t, e) : l.appendChild(t);
    else if (n !== 4 && (n === 27 && $l(t.type) && (l = t.stateNode), t = t.child, t !== null))
      for (mi(t, e, l), t = t.sibling; t !== null; )
        mi(t, e, l), t = t.sibling;
  }
  function Io(t) {
    var e = t.stateNode, l = t.memoizedProps;
    try {
      for (var n = t.type, a = e.attributes; a.length; )
        e.removeAttributeNode(a[0]);
      ue(e, n, l), e[ee] = t, e[ye] = l;
    } catch (u) {
      xt(t, t.return, u);
    }
  }
  var gl = !1, Kt = !1, _f = !1, Po = typeof WeakSet == "function" ? WeakSet : Set, Pt = null;
  function ah(t, e) {
    if (t = t.containerInfo, Xf = Ri, t = rr(t), yc(t)) {
      if ("selectionStart" in t)
        var l = {
          start: t.selectionStart,
          end: t.selectionEnd
        };
      else
        t: {
          l = (l = t.ownerDocument) && l.defaultView || window;
          var n = l.getSelection && l.getSelection();
          if (n && n.rangeCount !== 0) {
            l = n.anchorNode;
            var a = n.anchorOffset, u = n.focusNode;
            n = n.focusOffset;
            try {
              l.nodeType, u.nodeType;
            } catch {
              l = null;
              break t;
            }
            var c = 0, o = -1, y = -1, E = 0, O = 0, U = t, x = null;
            e: for (; ; ) {
              for (var z; U !== l || a !== 0 && U.nodeType !== 3 || (o = c + a), U !== u || n !== 0 && U.nodeType !== 3 || (y = c + n), U.nodeType === 3 && (c += U.nodeValue.length), (z = U.firstChild) !== null; )
                x = U, U = z;
              for (; ; ) {
                if (U === t) break e;
                if (x === l && ++E === a && (o = c), x === u && ++O === n && (y = c), (z = U.nextSibling) !== null) break;
                U = x, x = U.parentNode;
              }
              U = z;
            }
            l = o === -1 || y === -1 ? null : { start: o, end: y };
          } else l = null;
        }
      l = l || { start: 0, end: 0 };
    } else l = null;
    for (Zf = { focusedElem: t, selectionRange: l }, Ri = !1, Pt = e; Pt !== null; )
      if (e = Pt, t = e.child, (e.subtreeFlags & 1028) !== 0 && t !== null)
        t.return = e, Pt = t;
      else
        for (; Pt !== null; ) {
          switch (e = Pt, u = e.alternate, t = e.flags, e.tag) {
            case 0:
              if ((t & 4) !== 0 && (t = e.updateQueue, t = t !== null ? t.events : null, t !== null))
                for (l = 0; l < t.length; l++)
                  a = t[l], a.ref.impl = a.nextImpl;
              break;
            case 11:
            case 15:
              break;
            case 1:
              if ((t & 1024) !== 0 && u !== null) {
                t = void 0, l = e, a = u.memoizedProps, u = u.memoizedState, n = l.stateNode;
                try {
                  var X = Tn(
                    l.type,
                    a
                  );
                  t = n.getSnapshotBeforeUpdate(
                    X,
                    u
                  ), n.__reactInternalSnapshotBeforeUpdate = t;
                } catch (k) {
                  xt(
                    l,
                    l.return,
                    k
                  );
                }
              }
              break;
            case 3:
              if ((t & 1024) !== 0) {
                if (t = e.stateNode.containerInfo, l = t.nodeType, l === 9)
                  Jf(t);
                else if (l === 1)
                  switch (t.nodeName) {
                    case "HEAD":
                    case "HTML":
                    case "BODY":
                      Jf(t);
                      break;
                    default:
                      t.textContent = "";
                  }
              }
              break;
            case 5:
            case 26:
            case 27:
            case 6:
            case 4:
            case 17:
              break;
            default:
              if ((t & 1024) !== 0) throw Error(r(163));
          }
          if (t = e.sibling, t !== null) {
            t.return = e.return, Pt = t;
            break;
          }
          Pt = e.return;
        }
  }
  function td(t, e, l) {
    var n = l.flags;
    switch (l.tag) {
      case 0:
      case 11:
      case 15:
        bl(t, l), n & 4 && nu(5, l);
        break;
      case 1:
        if (bl(t, l), n & 4)
          if (t = l.stateNode, e === null)
            try {
              t.componentDidMount();
            } catch (c) {
              xt(l, l.return, c);
            }
          else {
            var a = Tn(
              l.type,
              e.memoizedProps
            );
            e = e.memoizedState;
            try {
              t.componentDidUpdate(
                a,
                e,
                t.__reactInternalSnapshotBeforeUpdate
              );
            } catch (c) {
              xt(
                l,
                l.return,
                c
              );
            }
          }
        n & 64 && ko(l), n & 512 && au(l, l.return);
        break;
      case 3:
        if (bl(t, l), n & 64 && (t = l.updateQueue, t !== null)) {
          if (e = null, l.child !== null)
            switch (l.child.tag) {
              case 27:
              case 5:
                e = l.child.stateNode;
                break;
              case 1:
                e = l.child.stateNode;
            }
          try {
            Lr(t, e);
          } catch (c) {
            xt(l, l.return, c);
          }
        }
        break;
      case 27:
        e === null && n & 4 && Io(l);
      case 26:
      case 5:
        bl(t, l), e === null && n & 4 && Wo(l), n & 512 && au(l, l.return);
        break;
      case 12:
        bl(t, l);
        break;
      case 31:
        bl(t, l), n & 4 && nd(t, l);
        break;
      case 13:
        bl(t, l), n & 4 && ad(t, l), n & 64 && (t = l.memoizedState, t !== null && (t = t.dehydrated, t !== null && (l = yh.bind(
          null,
          l
        ), jh(t, l))));
        break;
      case 22:
        if (n = l.memoizedState !== null || gl, !n) {
          e = e !== null && e.memoizedState !== null || Kt, a = gl;
          var u = Kt;
          gl = n, (Kt = e) && !u ? Sl(
            t,
            l,
            (l.subtreeFlags & 8772) !== 0
          ) : bl(t, l), gl = a, Kt = u;
        }
        break;
      case 30:
        break;
      default:
        bl(t, l);
    }
  }
  function ed(t) {
    var e = t.alternate;
    e !== null && (t.alternate = null, ed(e)), t.child = null, t.deletions = null, t.sibling = null, t.tag === 5 && (e = t.stateNode, e !== null && Wi(e)), t.stateNode = null, t.return = null, t.dependencies = null, t.memoizedProps = null, t.memoizedState = null, t.pendingProps = null, t.stateNode = null, t.updateQueue = null;
  }
  var Ut = null, he = !1;
  function pl(t, e, l) {
    for (l = l.child; l !== null; )
      ld(t, e, l), l = l.sibling;
  }
  function ld(t, e, l) {
    if ($t && typeof $t.onCommitFiberUnmount == "function")
      try {
        $t.onCommitFiberUnmount(Ol, l);
      } catch {
      }
    switch (l.tag) {
      case 26:
        Kt || tl(l, e), pl(
          t,
          e,
          l
        ), l.memoizedState ? l.memoizedState.count-- : l.stateNode && (l = l.stateNode, l.parentNode.removeChild(l));
        break;
      case 27:
        Kt || tl(l, e);
        var n = Ut, a = he;
        $l(l.type) && (Ut = l.stateNode, he = !1), pl(
          t,
          e,
          l
        ), yu(l.stateNode), Ut = n, he = a;
        break;
      case 5:
        Kt || tl(l, e);
      case 6:
        if (n = Ut, a = he, Ut = null, pl(
          t,
          e,
          l
        ), Ut = n, he = a, Ut !== null)
          if (he)
            try {
              (Ut.nodeType === 9 ? Ut.body : Ut.nodeName === "HTML" ? Ut.ownerDocument.body : Ut).removeChild(l.stateNode);
            } catch (u) {
              xt(
                l,
                e,
                u
              );
            }
          else
            try {
              Ut.removeChild(l.stateNode);
            } catch (u) {
              xt(
                l,
                e,
                u
              );
            }
        break;
      case 18:
        Ut !== null && (he ? (t = Ut, kd(
          t.nodeType === 9 ? t.body : t.nodeName === "HTML" ? t.ownerDocument.body : t,
          l.stateNode
        ), ba(t)) : kd(Ut, l.stateNode));
        break;
      case 4:
        n = Ut, a = he, Ut = l.stateNode.containerInfo, he = !0, pl(
          t,
          e,
          l
        ), Ut = n, he = a;
        break;
      case 0:
      case 11:
      case 14:
      case 15:
        Xl(2, l, e), Kt || Xl(4, l, e), pl(
          t,
          e,
          l
        );
        break;
      case 1:
        Kt || (tl(l, e), n = l.stateNode, typeof n.componentWillUnmount == "function" && $o(
          l,
          e,
          n
        )), pl(
          t,
          e,
          l
        );
        break;
      case 21:
        pl(
          t,
          e,
          l
        );
        break;
      case 22:
        Kt = (n = Kt) || l.memoizedState !== null, pl(
          t,
          e,
          l
        ), Kt = n;
        break;
      default:
        pl(
          t,
          e,
          l
        );
    }
  }
  function nd(t, e) {
    if (e.memoizedState === null && (t = e.alternate, t !== null && (t = t.memoizedState, t !== null))) {
      t = t.dehydrated;
      try {
        ba(t);
      } catch (l) {
        xt(e, e.return, l);
      }
    }
  }
  function ad(t, e) {
    if (e.memoizedState === null && (t = e.alternate, t !== null && (t = t.memoizedState, t !== null && (t = t.dehydrated, t !== null))))
      try {
        ba(t);
      } catch (l) {
        xt(e, e.return, l);
      }
  }
  function uh(t) {
    switch (t.tag) {
      case 31:
      case 13:
      case 19:
        var e = t.stateNode;
        return e === null && (e = t.stateNode = new Po()), e;
      case 22:
        return t = t.stateNode, e = t._retryCache, e === null && (e = t._retryCache = new Po()), e;
      default:
        throw Error(r(435, t.tag));
    }
  }
  function hi(t, e) {
    var l = uh(t);
    e.forEach(function(n) {
      if (!l.has(n)) {
        l.add(n);
        var a = mh.bind(null, t, n);
        n.then(a, a);
      }
    });
  }
  function ve(t, e) {
    var l = e.deletions;
    if (l !== null)
      for (var n = 0; n < l.length; n++) {
        var a = l[n], u = t, c = e, o = c;
        t: for (; o !== null; ) {
          switch (o.tag) {
            case 27:
              if ($l(o.type)) {
                Ut = o.stateNode, he = !1;
                break t;
              }
              break;
            case 5:
              Ut = o.stateNode, he = !1;
              break t;
            case 3:
            case 4:
              Ut = o.stateNode.containerInfo, he = !0;
              break t;
          }
          o = o.return;
        }
        if (Ut === null) throw Error(r(160));
        ld(u, c, a), Ut = null, he = !1, u = a.alternate, u !== null && (u.return = null), a.return = null;
      }
    if (e.subtreeFlags & 13886)
      for (e = e.child; e !== null; )
        ud(e, t), e = e.sibling;
  }
  var Ve = null;
  function ud(t, e) {
    var l = t.alternate, n = t.flags;
    switch (t.tag) {
      case 0:
      case 11:
      case 14:
      case 15:
        ve(e, t), ge(t), n & 4 && (Xl(3, t, t.return), nu(3, t), Xl(5, t, t.return));
        break;
      case 1:
        ve(e, t), ge(t), n & 512 && (Kt || l === null || tl(l, l.return)), n & 64 && gl && (t = t.updateQueue, t !== null && (n = t.callbacks, n !== null && (l = t.shared.hiddenCallbacks, t.shared.hiddenCallbacks = l === null ? n : l.concat(n))));
        break;
      case 26:
        var a = Ve;
        if (ve(e, t), ge(t), n & 512 && (Kt || l === null || tl(l, l.return)), n & 4) {
          var u = l !== null ? l.memoizedState : null;
          if (n = t.memoizedState, l === null)
            if (n === null)
              if (t.stateNode === null) {
                t: {
                  n = t.type, l = t.memoizedProps, a = a.ownerDocument || a;
                  e: switch (n) {
                    case "title":
                      u = a.getElementsByTagName("title")[0], (!u || u[Da] || u[ee] || u.namespaceURI === "http://www.w3.org/2000/svg" || u.hasAttribute("itemprop")) && (u = a.createElement(n), a.head.insertBefore(
                        u,
                        a.querySelector("head > title")
                      )), ue(u, n, l), u[ee] = t, It(u), n = u;
                      break t;
                    case "link":
                      var c = uy(
                        "link",
                        "href",
                        a
                      ).get(n + (l.href || ""));
                      if (c) {
                        for (var o = 0; o < c.length; o++)
                          if (u = c[o], u.getAttribute("href") === (l.href == null || l.href === "" ? null : l.href) && u.getAttribute("rel") === (l.rel == null ? null : l.rel) && u.getAttribute("title") === (l.title == null ? null : l.title) && u.getAttribute("crossorigin") === (l.crossOrigin == null ? null : l.crossOrigin)) {
                            c.splice(o, 1);
                            break e;
                          }
                      }
                      u = a.createElement(n), ue(u, n, l), a.head.appendChild(u);
                      break;
                    case "meta":
                      if (c = uy(
                        "meta",
                        "content",
                        a
                      ).get(n + (l.content || ""))) {
                        for (o = 0; o < c.length; o++)
                          if (u = c[o], u.getAttribute("content") === (l.content == null ? null : "" + l.content) && u.getAttribute("name") === (l.name == null ? null : l.name) && u.getAttribute("property") === (l.property == null ? null : l.property) && u.getAttribute("http-equiv") === (l.httpEquiv == null ? null : l.httpEquiv) && u.getAttribute("charset") === (l.charSet == null ? null : l.charSet)) {
                            c.splice(o, 1);
                            break e;
                          }
                      }
                      u = a.createElement(n), ue(u, n, l), a.head.appendChild(u);
                      break;
                    default:
                      throw Error(r(468, n));
                  }
                  u[ee] = t, It(u), n = u;
                }
                t.stateNode = n;
              } else
                iy(
                  a,
                  t.type,
                  t.stateNode
                );
            else
              t.stateNode = ay(
                a,
                n,
                t.memoizedProps
              );
          else
            u !== n ? (u === null ? l.stateNode !== null && (l = l.stateNode, l.parentNode.removeChild(l)) : u.count--, n === null ? iy(
              a,
              t.type,
              t.stateNode
            ) : ay(
              a,
              n,
              t.memoizedProps
            )) : n === null && t.stateNode !== null && pf(
              t,
              t.memoizedProps,
              l.memoizedProps
            );
        }
        break;
      case 27:
        ve(e, t), ge(t), n & 512 && (Kt || l === null || tl(l, l.return)), l !== null && n & 4 && pf(
          t,
          t.memoizedProps,
          l.memoizedProps
        );
        break;
      case 5:
        if (ve(e, t), ge(t), n & 512 && (Kt || l === null || tl(l, l.return)), t.flags & 32) {
          a = t.stateNode;
          try {
            Xn(a, "");
          } catch (X) {
            xt(t, t.return, X);
          }
        }
        n & 4 && t.stateNode != null && (a = t.memoizedProps, pf(
          t,
          a,
          l !== null ? l.memoizedProps : a
        )), n & 1024 && (_f = !0);
        break;
      case 6:
        if (ve(e, t), ge(t), n & 4) {
          if (t.stateNode === null)
            throw Error(r(162));
          n = t.memoizedProps, l = t.stateNode;
          try {
            l.nodeValue = n;
          } catch (X) {
            xt(t, t.return, X);
          }
        }
        break;
      case 3:
        if (Di = null, a = Ve, Ve = Oi(e.containerInfo), ve(e, t), Ve = a, ge(t), n & 4 && l !== null && l.memoizedState.isDehydrated)
          try {
            ba(e.containerInfo);
          } catch (X) {
            xt(t, t.return, X);
          }
        _f && (_f = !1, id(t));
        break;
      case 4:
        n = Ve, Ve = Oi(
          t.stateNode.containerInfo
        ), ve(e, t), ge(t), Ve = n;
        break;
      case 12:
        ve(e, t), ge(t);
        break;
      case 31:
        ve(e, t), ge(t), n & 4 && (n = t.updateQueue, n !== null && (t.updateQueue = null, hi(t, n)));
        break;
      case 13:
        ve(e, t), ge(t), t.child.flags & 8192 && t.memoizedState !== null != (l !== null && l.memoizedState !== null) && (gi = Nt()), n & 4 && (n = t.updateQueue, n !== null && (t.updateQueue = null, hi(t, n)));
        break;
      case 22:
        a = t.memoizedState !== null;
        var y = l !== null && l.memoizedState !== null, E = gl, O = Kt;
        if (gl = E || a, Kt = O || y, ve(e, t), Kt = O, gl = E, ge(t), n & 8192)
          t: for (e = t.stateNode, e._visibility = a ? e._visibility & -2 : e._visibility | 1, a && (l === null || y || gl || Kt || zn(t)), l = null, e = t; ; ) {
            if (e.tag === 5 || e.tag === 26) {
              if (l === null) {
                y = l = e;
                try {
                  if (u = y.stateNode, a)
                    c = u.style, typeof c.setProperty == "function" ? c.setProperty("display", "none", "important") : c.display = "none";
                  else {
                    o = y.stateNode;
                    var U = y.memoizedProps.style, x = U != null && U.hasOwnProperty("display") ? U.display : null;
                    o.style.display = x == null || typeof x == "boolean" ? "" : ("" + x).trim();
                  }
                } catch (X) {
                  xt(y, y.return, X);
                }
              }
            } else if (e.tag === 6) {
              if (l === null) {
                y = e;
                try {
                  y.stateNode.nodeValue = a ? "" : y.memoizedProps;
                } catch (X) {
                  xt(y, y.return, X);
                }
              }
            } else if (e.tag === 18) {
              if (l === null) {
                y = e;
                try {
                  var z = y.stateNode;
                  a ? $d(z, !0) : $d(y.stateNode, !1);
                } catch (X) {
                  xt(y, y.return, X);
                }
              }
            } else if ((e.tag !== 22 && e.tag !== 23 || e.memoizedState === null || e === t) && e.child !== null) {
              e.child.return = e, e = e.child;
              continue;
            }
            if (e === t) break t;
            for (; e.sibling === null; ) {
              if (e.return === null || e.return === t) break t;
              l === e && (l = null), e = e.return;
            }
            l === e && (l = null), e.sibling.return = e.return, e = e.sibling;
          }
        n & 4 && (n = t.updateQueue, n !== null && (l = n.retryQueue, l !== null && (n.retryQueue = null, hi(t, l))));
        break;
      case 19:
        ve(e, t), ge(t), n & 4 && (n = t.updateQueue, n !== null && (t.updateQueue = null, hi(t, n)));
        break;
      case 30:
        break;
      case 21:
        break;
      default:
        ve(e, t), ge(t);
    }
  }
  function ge(t) {
    var e = t.flags;
    if (e & 2) {
      try {
        for (var l, n = t.return; n !== null; ) {
          if (Fo(n)) {
            l = n;
            break;
          }
          n = n.return;
        }
        if (l == null) throw Error(r(160));
        switch (l.tag) {
          case 27:
            var a = l.stateNode, u = bf(t);
            mi(t, u, a);
            break;
          case 5:
            var c = l.stateNode;
            l.flags & 32 && (Xn(c, ""), l.flags &= -33);
            var o = bf(t);
            mi(t, o, c);
            break;
          case 3:
          case 4:
            var y = l.stateNode.containerInfo, E = bf(t);
            Sf(
              t,
              E,
              y
            );
            break;
          default:
            throw Error(r(161));
        }
      } catch (O) {
        xt(t, t.return, O);
      }
      t.flags &= -3;
    }
    e & 4096 && (t.flags &= -4097);
  }
  function id(t) {
    if (t.subtreeFlags & 1024)
      for (t = t.child; t !== null; ) {
        var e = t;
        id(e), e.tag === 5 && e.flags & 1024 && e.stateNode.reset(), t = t.sibling;
      }
  }
  function bl(t, e) {
    if (e.subtreeFlags & 8772)
      for (e = e.child; e !== null; )
        td(t, e.alternate, e), e = e.sibling;
  }
  function zn(t) {
    for (t = t.child; t !== null; ) {
      var e = t;
      switch (e.tag) {
        case 0:
        case 11:
        case 14:
        case 15:
          Xl(4, e, e.return), zn(e);
          break;
        case 1:
          tl(e, e.return);
          var l = e.stateNode;
          typeof l.componentWillUnmount == "function" && $o(
            e,
            e.return,
            l
          ), zn(e);
          break;
        case 27:
          yu(e.stateNode);
        case 26:
        case 5:
          tl(e, e.return), zn(e);
          break;
        case 22:
          e.memoizedState === null && zn(e);
          break;
        case 30:
          zn(e);
          break;
        default:
          zn(e);
      }
      t = t.sibling;
    }
  }
  function Sl(t, e, l) {
    for (l = l && (e.subtreeFlags & 8772) !== 0, e = e.child; e !== null; ) {
      var n = e.alternate, a = t, u = e, c = u.flags;
      switch (u.tag) {
        case 0:
        case 11:
        case 15:
          Sl(
            a,
            u,
            l
          ), nu(4, u);
          break;
        case 1:
          if (Sl(
            a,
            u,
            l
          ), n = u, a = n.stateNode, typeof a.componentDidMount == "function")
            try {
              a.componentDidMount();
            } catch (E) {
              xt(n, n.return, E);
            }
          if (n = u, a = n.updateQueue, a !== null) {
            var o = n.stateNode;
            try {
              var y = a.shared.hiddenCallbacks;
              if (y !== null)
                for (a.shared.hiddenCallbacks = null, a = 0; a < y.length; a++)
                  Hr(y[a], o);
            } catch (E) {
              xt(n, n.return, E);
            }
          }
          l && c & 64 && ko(u), au(u, u.return);
          break;
        case 27:
          Io(u);
        case 26:
        case 5:
          Sl(
            a,
            u,
            l
          ), l && n === null && c & 4 && Wo(u), au(u, u.return);
          break;
        case 12:
          Sl(
            a,
            u,
            l
          );
          break;
        case 31:
          Sl(
            a,
            u,
            l
          ), l && c & 4 && nd(a, u);
          break;
        case 13:
          Sl(
            a,
            u,
            l
          ), l && c & 4 && ad(a, u);
          break;
        case 22:
          u.memoizedState === null && Sl(
            a,
            u,
            l
          ), au(u, u.return);
          break;
        case 30:
          break;
        default:
          Sl(
            a,
            u,
            l
          );
      }
      e = e.sibling;
    }
  }
  function Ef(t, e) {
    var l = null;
    t !== null && t.memoizedState !== null && t.memoizedState.cachePool !== null && (l = t.memoizedState.cachePool.pool), t = null, e.memoizedState !== null && e.memoizedState.cachePool !== null && (t = e.memoizedState.cachePool.pool), t !== l && (t != null && t.refCount++, l != null && Va(l));
  }
  function xf(t, e) {
    t = null, e.alternate !== null && (t = e.alternate.memoizedState.cache), e = e.memoizedState.cache, e !== t && (e.refCount++, t != null && Va(t));
  }
  function we(t, e, l, n) {
    if (e.subtreeFlags & 10256)
      for (e = e.child; e !== null; )
        cd(
          t,
          e,
          l,
          n
        ), e = e.sibling;
  }
  function cd(t, e, l, n) {
    var a = e.flags;
    switch (e.tag) {
      case 0:
      case 11:
      case 15:
        we(
          t,
          e,
          l,
          n
        ), a & 2048 && nu(9, e);
        break;
      case 1:
        we(
          t,
          e,
          l,
          n
        );
        break;
      case 3:
        we(
          t,
          e,
          l,
          n
        ), a & 2048 && (t = null, e.alternate !== null && (t = e.alternate.memoizedState.cache), e = e.memoizedState.cache, e !== t && (e.refCount++, t != null && Va(t)));
        break;
      case 12:
        if (a & 2048) {
          we(
            t,
            e,
            l,
            n
          ), t = e.stateNode;
          try {
            var u = e.memoizedProps, c = u.id, o = u.onPostCommit;
            typeof o == "function" && o(
              c,
              e.alternate === null ? "mount" : "update",
              t.passiveEffectDuration,
              -0
            );
          } catch (y) {
            xt(e, e.return, y);
          }
        } else
          we(
            t,
            e,
            l,
            n
          );
        break;
      case 31:
        we(
          t,
          e,
          l,
          n
        );
        break;
      case 13:
        we(
          t,
          e,
          l,
          n
        );
        break;
      case 23:
        break;
      case 22:
        u = e.stateNode, c = e.alternate, e.memoizedState !== null ? u._visibility & 2 ? we(
          t,
          e,
          l,
          n
        ) : uu(t, e) : u._visibility & 2 ? we(
          t,
          e,
          l,
          n
        ) : (u._visibility |= 2, fa(
          t,
          e,
          l,
          n,
          (e.subtreeFlags & 10256) !== 0 || !1
        )), a & 2048 && Ef(c, e);
        break;
      case 24:
        we(
          t,
          e,
          l,
          n
        ), a & 2048 && xf(e.alternate, e);
        break;
      default:
        we(
          t,
          e,
          l,
          n
        );
    }
  }
  function fa(t, e, l, n, a) {
    for (a = a && ((e.subtreeFlags & 10256) !== 0 || !1), e = e.child; e !== null; ) {
      var u = t, c = e, o = l, y = n, E = c.flags;
      switch (c.tag) {
        case 0:
        case 11:
        case 15:
          fa(
            u,
            c,
            o,
            y,
            a
          ), nu(8, c);
          break;
        case 23:
          break;
        case 22:
          var O = c.stateNode;
          c.memoizedState !== null ? O._visibility & 2 ? fa(
            u,
            c,
            o,
            y,
            a
          ) : uu(
            u,
            c
          ) : (O._visibility |= 2, fa(
            u,
            c,
            o,
            y,
            a
          )), a && E & 2048 && Ef(
            c.alternate,
            c
          );
          break;
        case 24:
          fa(
            u,
            c,
            o,
            y,
            a
          ), a && E & 2048 && xf(c.alternate, c);
          break;
        default:
          fa(
            u,
            c,
            o,
            y,
            a
          );
      }
      e = e.sibling;
    }
  }
  function uu(t, e) {
    if (e.subtreeFlags & 10256)
      for (e = e.child; e !== null; ) {
        var l = t, n = e, a = n.flags;
        switch (n.tag) {
          case 22:
            uu(l, n), a & 2048 && Ef(
              n.alternate,
              n
            );
            break;
          case 24:
            uu(l, n), a & 2048 && xf(n.alternate, n);
            break;
          default:
            uu(l, n);
        }
        e = e.sibling;
      }
  }
  var iu = 8192;
  function sa(t, e, l) {
    if (t.subtreeFlags & iu)
      for (t = t.child; t !== null; )
        fd(
          t,
          e,
          l
        ), t = t.sibling;
  }
  function fd(t, e, l) {
    switch (t.tag) {
      case 26:
        sa(
          t,
          e,
          l
        ), t.flags & iu && t.memoizedState !== null && wh(
          l,
          Ve,
          t.memoizedState,
          t.memoizedProps
        );
        break;
      case 5:
        sa(
          t,
          e,
          l
        );
        break;
      case 3:
      case 4:
        var n = Ve;
        Ve = Oi(t.stateNode.containerInfo), sa(
          t,
          e,
          l
        ), Ve = n;
        break;
      case 22:
        t.memoizedState === null && (n = t.alternate, n !== null && n.memoizedState !== null ? (n = iu, iu = 16777216, sa(
          t,
          e,
          l
        ), iu = n) : sa(
          t,
          e,
          l
        ));
        break;
      default:
        sa(
          t,
          e,
          l
        );
    }
  }
  function sd(t) {
    var e = t.alternate;
    if (e !== null && (t = e.child, t !== null)) {
      e.child = null;
      do
        e = t.sibling, t.sibling = null, t = e;
      while (t !== null);
    }
  }
  function cu(t) {
    var e = t.deletions;
    if ((t.flags & 16) !== 0) {
      if (e !== null)
        for (var l = 0; l < e.length; l++) {
          var n = e[l];
          Pt = n, od(
            n,
            t
          );
        }
      sd(t);
    }
    if (t.subtreeFlags & 10256)
      for (t = t.child; t !== null; )
        rd(t), t = t.sibling;
  }
  function rd(t) {
    switch (t.tag) {
      case 0:
      case 11:
      case 15:
        cu(t), t.flags & 2048 && Xl(9, t, t.return);
        break;
      case 3:
        cu(t);
        break;
      case 12:
        cu(t);
        break;
      case 22:
        var e = t.stateNode;
        t.memoizedState !== null && e._visibility & 2 && (t.return === null || t.return.tag !== 13) ? (e._visibility &= -3, vi(t)) : cu(t);
        break;
      default:
        cu(t);
    }
  }
  function vi(t) {
    var e = t.deletions;
    if ((t.flags & 16) !== 0) {
      if (e !== null)
        for (var l = 0; l < e.length; l++) {
          var n = e[l];
          Pt = n, od(
            n,
            t
          );
        }
      sd(t);
    }
    for (t = t.child; t !== null; ) {
      switch (e = t, e.tag) {
        case 0:
        case 11:
        case 15:
          Xl(8, e, e.return), vi(e);
          break;
        case 22:
          l = e.stateNode, l._visibility & 2 && (l._visibility &= -3, vi(e));
          break;
        default:
          vi(e);
      }
      t = t.sibling;
    }
  }
  function od(t, e) {
    for (; Pt !== null; ) {
      var l = Pt;
      switch (l.tag) {
        case 0:
        case 11:
        case 15:
          Xl(8, l, e);
          break;
        case 23:
        case 22:
          if (l.memoizedState !== null && l.memoizedState.cachePool !== null) {
            var n = l.memoizedState.cachePool.pool;
            n != null && n.refCount++;
          }
          break;
        case 24:
          Va(l.memoizedState.cache);
      }
      if (n = l.child, n !== null) n.return = l, Pt = n;
      else
        t: for (l = t; Pt !== null; ) {
          n = Pt;
          var a = n.sibling, u = n.return;
          if (ed(n), n === l) {
            Pt = null;
            break t;
          }
          if (a !== null) {
            a.return = u, Pt = a;
            break t;
          }
          Pt = u;
        }
    }
  }
  var ih = {
    getCacheForType: function(t) {
      var e = ne(Vt), l = e.data.get(t);
      return l === void 0 && (l = t(), e.data.set(t, l)), l;
    },
    cacheSignal: function() {
      return ne(Vt).controller.signal;
    }
  }, ch = typeof WeakMap == "function" ? WeakMap : Map, St = 0, qt = null, ft = null, rt = 0, Et = 0, ze = null, Zl = !1, ra = !1, Af = !1, _l = 0, Ht = 0, Vl = 0, qn = 0, Tf = 0, qe = 0, oa = 0, fu = null, pe = null, zf = !1, gi = 0, dd = 0, pi = 1 / 0, bi = null, wl = null, Wt = 0, Jl = null, da = null, El = 0, qf = 0, Nf = null, yd = null, su = 0, Of = null;
  function Ne() {
    return (St & 2) !== 0 && rt !== 0 ? rt & -rt : N.T !== null ? Rf() : Ns();
  }
  function md() {
    if (qe === 0)
      if ((rt & 536870912) === 0 || dt) {
        var t = sn;
        sn <<= 1, (sn & 3932160) === 0 && (sn = 262144), qe = t;
      } else qe = 536870912;
    return t = Ae.current, t !== null && (t.flags |= 32), qe;
  }
  function be(t, e, l) {
    (t === qt && (Et === 2 || Et === 9) || t.cancelPendingCommit !== null) && (ya(t, 0), Kl(
      t,
      rt,
      qe,
      !1
    )), R(t, l), ((St & 2) === 0 || t !== qt) && (t === qt && ((St & 2) === 0 && (qn |= l), Ht === 4 && Kl(
      t,
      rt,
      qe,
      !1
    )), el(t));
  }
  function hd(t, e, l) {
    if ((St & 6) !== 0) throw Error(r(327));
    var n = !l && (e & 127) === 0 && (e & t.expiredLanes) === 0 || Fe(t, e), a = n ? rh(t, e) : Df(t, e, !0), u = n;
    do {
      if (a === 0) {
        ra && !n && Kl(t, e, 0, !1);
        break;
      } else {
        if (l = t.current.alternate, u && !fh(l)) {
          a = Df(t, e, !1), u = !1;
          continue;
        }
        if (a === 2) {
          if (u = e, t.errorRecoveryDisabledLanes & u)
            var c = 0;
          else
            c = t.pendingLanes & -536870913, c = c !== 0 ? c : c & 536870912 ? 536870912 : 0;
          if (c !== 0) {
            e = c;
            t: {
              var o = t;
              a = fu;
              var y = o.current.memoizedState.isDehydrated;
              if (y && (ya(o, c).flags |= 256), c = Df(
                o,
                c,
                !1
              ), c !== 2) {
                if (Af && !y) {
                  o.errorRecoveryDisabledLanes |= u, qn |= u, a = 4;
                  break t;
                }
                u = pe, pe = a, u !== null && (pe === null ? pe = u : pe.push.apply(
                  pe,
                  u
                ));
              }
              a = c;
            }
            if (u = !1, a !== 2) continue;
          }
        }
        if (a === 1) {
          ya(t, 0), Kl(t, e, 0, !0);
          break;
        }
        t: {
          switch (n = t, u = a, u) {
            case 0:
            case 1:
              throw Error(r(345));
            case 4:
              if ((e & 4194048) !== e) break;
            case 6:
              Kl(
                n,
                e,
                qe,
                !Zl
              );
              break t;
            case 2:
              pe = null;
              break;
            case 3:
            case 5:
              break;
            default:
              throw Error(r(329));
          }
          if ((e & 62914560) === e && (a = gi + 300 - Nt(), 10 < a)) {
            if (Kl(
              n,
              e,
              qe,
              !Zl
            ), Ml(n, 0, !0) !== 0) break t;
            El = e, n.timeoutHandle = Jd(
              vd.bind(
                null,
                n,
                l,
                pe,
                bi,
                zf,
                e,
                qe,
                qn,
                oa,
                Zl,
                u,
                "Throttled",
                -0,
                0
              ),
              a
            );
            break t;
          }
          vd(
            n,
            l,
            pe,
            bi,
            zf,
            e,
            qe,
            qn,
            oa,
            Zl,
            u,
            null,
            -0,
            0
          );
        }
      }
      break;
    } while (!0);
    el(t);
  }
  function vd(t, e, l, n, a, u, c, o, y, E, O, U, x, z) {
    if (t.timeoutHandle = -1, U = e.subtreeFlags, U & 8192 || (U & 16785408) === 16785408) {
      U = {
        stylesheets: null,
        count: 0,
        imgCount: 0,
        imgBytes: 0,
        suspenseyImages: [],
        waitingForImages: !0,
        waitingForViewTransition: !1,
        unsuspend: cl
      }, fd(
        e,
        u,
        U
      );
      var X = (u & 62914560) === u ? gi - Nt() : (u & 4194048) === u ? dd - Nt() : 0;
      if (X = Jh(
        U,
        X
      ), X !== null) {
        El = u, t.cancelPendingCommit = X(
          Ad.bind(
            null,
            t,
            e,
            u,
            l,
            n,
            a,
            c,
            o,
            y,
            O,
            U,
            null,
            x,
            z
          )
        ), Kl(t, u, c, !E);
        return;
      }
    }
    Ad(
      t,
      e,
      u,
      l,
      n,
      a,
      c,
      o,
      y
    );
  }
  function fh(t) {
    for (var e = t; ; ) {
      var l = e.tag;
      if ((l === 0 || l === 11 || l === 15) && e.flags & 16384 && (l = e.updateQueue, l !== null && (l = l.stores, l !== null)))
        for (var n = 0; n < l.length; n++) {
          var a = l[n], u = a.getSnapshot;
          a = a.value;
          try {
            if (!Ee(u(), a)) return !1;
          } catch {
            return !1;
          }
        }
      if (l = e.child, e.subtreeFlags & 16384 && l !== null)
        l.return = e, e = l;
      else {
        if (e === t) break;
        for (; e.sibling === null; ) {
          if (e.return === null || e.return === t) return !0;
          e = e.return;
        }
        e.sibling.return = e.return, e = e.sibling;
      }
    }
    return !0;
  }
  function Kl(t, e, l, n) {
    e &= ~Tf, e &= ~qn, t.suspendedLanes |= e, t.pingedLanes &= ~e, n && (t.warmLanes |= e), n = t.expirationTimes;
    for (var a = e; 0 < a; ) {
      var u = 31 - te(a), c = 1 << u;
      n[u] = -1, a &= ~c;
    }
    l !== 0 && mt(t, l, e);
  }
  function Si() {
    return (St & 6) === 0 ? (ru(0), !1) : !0;
  }
  function Mf() {
    if (ft !== null) {
      if (Et === 0)
        var t = ft.return;
      else
        t = ft, ol = pn = null, wc(t), na = null, Ja = 0, t = ft;
      for (; t !== null; )
        Ko(t.alternate, t), t = t.return;
      ft = null;
    }
  }
  function ya(t, e) {
    var l = t.timeoutHandle;
    l !== -1 && (t.timeoutHandle = -1, Nh(l)), l = t.cancelPendingCommit, l !== null && (t.cancelPendingCommit = null, l()), El = 0, Mf(), qt = t, ft = l = sl(t.current, null), rt = e, Et = 0, ze = null, Zl = !1, ra = Fe(t, e), Af = !1, oa = qe = Tf = qn = Vl = Ht = 0, pe = fu = null, zf = !1, (e & 8) !== 0 && (e |= e & 32);
    var n = t.entangledLanes;
    if (n !== 0)
      for (t = t.entanglements, n &= e; 0 < n; ) {
        var a = 31 - te(n), u = 1 << a;
        e |= t[a], n &= ~u;
      }
    return _l = e, Qu(), l;
  }
  function gd(t, e) {
    lt = null, N.H = tu, e === la || e === $u ? (e = Ur(), Et = 3) : e === jc ? (e = Ur(), Et = 4) : Et = e === ff ? 8 : e !== null && typeof e == "object" && typeof e.then == "function" ? 6 : 1, ze = e, ft === null && (Ht = 1, si(
      t,
      je(e, t.current)
    ));
  }
  function pd() {
    var t = Ae.current;
    return t === null ? !0 : (rt & 4194048) === rt ? Le === null : (rt & 62914560) === rt || (rt & 536870912) !== 0 ? t === Le : !1;
  }
  function bd() {
    var t = N.H;
    return N.H = tu, t === null ? tu : t;
  }
  function Sd() {
    var t = N.A;
    return N.A = ih, t;
  }
  function _i() {
    Ht = 4, Zl || (rt & 4194048) !== rt && Ae.current !== null || (ra = !0), (Vl & 134217727) === 0 && (qn & 134217727) === 0 || qt === null || Kl(
      qt,
      rt,
      qe,
      !1
    );
  }
  function Df(t, e, l) {
    var n = St;
    St |= 2;
    var a = bd(), u = Sd();
    (qt !== t || rt !== e) && (bi = null, ya(t, e)), e = !1;
    var c = Ht;
    t: do
      try {
        if (Et !== 0 && ft !== null) {
          var o = ft, y = ze;
          switch (Et) {
            case 8:
              Mf(), c = 6;
              break t;
            case 3:
            case 2:
            case 9:
            case 6:
              Ae.current === null && (e = !0);
              var E = Et;
              if (Et = 0, ze = null, ma(t, o, y, E), l && ra) {
                c = 0;
                break t;
              }
              break;
            default:
              E = Et, Et = 0, ze = null, ma(t, o, y, E);
          }
        }
        sh(), c = Ht;
        break;
      } catch (O) {
        gd(t, O);
      }
    while (!0);
    return e && t.shellSuspendCounter++, ol = pn = null, St = n, N.H = a, N.A = u, ft === null && (qt = null, rt = 0, Qu()), c;
  }
  function sh() {
    for (; ft !== null; ) _d(ft);
  }
  function rh(t, e) {
    var l = St;
    St |= 2;
    var n = bd(), a = Sd();
    qt !== t || rt !== e ? (bi = null, pi = Nt() + 500, ya(t, e)) : ra = Fe(
      t,
      e
    );
    t: do
      try {
        if (Et !== 0 && ft !== null) {
          e = ft;
          var u = ze;
          e: switch (Et) {
            case 1:
              Et = 0, ze = null, ma(t, e, u, 1);
              break;
            case 2:
            case 9:
              if (Mr(u)) {
                Et = 0, ze = null, Ed(e);
                break;
              }
              e = function() {
                Et !== 2 && Et !== 9 || qt !== t || (Et = 7), el(t);
              }, u.then(e, e);
              break t;
            case 3:
              Et = 7;
              break t;
            case 4:
              Et = 5;
              break t;
            case 7:
              Mr(u) ? (Et = 0, ze = null, Ed(e)) : (Et = 0, ze = null, ma(t, e, u, 7));
              break;
            case 5:
              var c = null;
              switch (ft.tag) {
                case 26:
                  c = ft.memoizedState;
                case 5:
                case 27:
                  var o = ft;
                  if (c ? cy(c) : o.stateNode.complete) {
                    Et = 0, ze = null;
                    var y = o.sibling;
                    if (y !== null) ft = y;
                    else {
                      var E = o.return;
                      E !== null ? (ft = E, Ei(E)) : ft = null;
                    }
                    break e;
                  }
              }
              Et = 0, ze = null, ma(t, e, u, 5);
              break;
            case 6:
              Et = 0, ze = null, ma(t, e, u, 6);
              break;
            case 8:
              Mf(), Ht = 6;
              break t;
            default:
              throw Error(r(462));
          }
        }
        oh();
        break;
      } catch (O) {
        gd(t, O);
      }
    while (!0);
    return ol = pn = null, N.H = n, N.A = a, St = l, ft !== null ? 0 : (qt = null, rt = 0, Qu(), Ht);
  }
  function oh() {
    for (; ft !== null && !Tu(); )
      _d(ft);
  }
  function _d(t) {
    var e = wo(t.alternate, t, _l);
    t.memoizedProps = t.pendingProps, e === null ? Ei(t) : ft = e;
  }
  function Ed(t) {
    var e = t, l = e.alternate;
    switch (e.tag) {
      case 15:
      case 0:
        e = Yo(
          l,
          e,
          e.pendingProps,
          e.type,
          void 0,
          rt
        );
        break;
      case 11:
        e = Yo(
          l,
          e,
          e.pendingProps,
          e.type.render,
          e.ref,
          rt
        );
        break;
      case 5:
        wc(e);
      default:
        Ko(l, e), e = ft = br(e, _l), e = wo(l, e, _l);
    }
    t.memoizedProps = t.pendingProps, e === null ? Ei(t) : ft = e;
  }
  function ma(t, e, l, n) {
    ol = pn = null, wc(e), na = null, Ja = 0;
    var a = e.return;
    try {
      if (Pm(
        t,
        a,
        e,
        l,
        rt
      )) {
        Ht = 1, si(
          t,
          je(l, t.current)
        ), ft = null;
        return;
      }
    } catch (u) {
      if (a !== null) throw ft = a, u;
      Ht = 1, si(
        t,
        je(l, t.current)
      ), ft = null;
      return;
    }
    e.flags & 32768 ? (dt || n === 1 ? t = !0 : ra || (rt & 536870912) !== 0 ? t = !1 : (Zl = t = !0, (n === 2 || n === 9 || n === 3 || n === 6) && (n = Ae.current, n !== null && n.tag === 13 && (n.flags |= 16384))), xd(e, t)) : Ei(e);
  }
  function Ei(t) {
    var e = t;
    do {
      if ((e.flags & 32768) !== 0) {
        xd(
          e,
          Zl
        );
        return;
      }
      t = e.return;
      var l = lh(
        e.alternate,
        e,
        _l
      );
      if (l !== null) {
        ft = l;
        return;
      }
      if (e = e.sibling, e !== null) {
        ft = e;
        return;
      }
      ft = e = t;
    } while (e !== null);
    Ht === 0 && (Ht = 5);
  }
  function xd(t, e) {
    do {
      var l = nh(t.alternate, t);
      if (l !== null) {
        l.flags &= 32767, ft = l;
        return;
      }
      if (l = t.return, l !== null && (l.flags |= 32768, l.subtreeFlags = 0, l.deletions = null), !e && (t = t.sibling, t !== null)) {
        ft = t;
        return;
      }
      ft = t = l;
    } while (t !== null);
    Ht = 6, ft = null;
  }
  function Ad(t, e, l, n, a, u, c, o, y) {
    t.cancelPendingCommit = null;
    do
      xi();
    while (Wt !== 0);
    if ((St & 6) !== 0) throw Error(r(327));
    if (e !== null) {
      if (e === t.current) throw Error(r(177));
      if (u = e.lanes | e.childLanes, u |= pc, w(
        t,
        l,
        u,
        c,
        o,
        y
      ), t === qt && (ft = qt = null, rt = 0), da = e, Jl = t, El = l, qf = u, Nf = a, yd = n, (e.subtreeFlags & 10256) !== 0 || (e.flags & 10256) !== 0 ? (t.callbackNode = null, t.callbackPriority = 0, hh(Un, function() {
        return Od(), null;
      })) : (t.callbackNode = null, t.callbackPriority = 0), n = (e.flags & 13878) !== 0, (e.subtreeFlags & 13878) !== 0 || n) {
        n = N.T, N.T = null, a = H.p, H.p = 2, c = St, St |= 4;
        try {
          ah(t, e, l);
        } finally {
          St = c, H.p = a, N.T = n;
        }
      }
      Wt = 1, Td(), zd(), qd();
    }
  }
  function Td() {
    if (Wt === 1) {
      Wt = 0;
      var t = Jl, e = da, l = (e.flags & 13878) !== 0;
      if ((e.subtreeFlags & 13878) !== 0 || l) {
        l = N.T, N.T = null;
        var n = H.p;
        H.p = 2;
        var a = St;
        St |= 4;
        try {
          ud(e, t);
          var u = Zf, c = rr(t.containerInfo), o = u.focusedElem, y = u.selectionRange;
          if (c !== o && o && o.ownerDocument && sr(
            o.ownerDocument.documentElement,
            o
          )) {
            if (y !== null && yc(o)) {
              var E = y.start, O = y.end;
              if (O === void 0 && (O = E), "selectionStart" in o)
                o.selectionStart = E, o.selectionEnd = Math.min(
                  O,
                  o.value.length
                );
              else {
                var U = o.ownerDocument || document, x = U && U.defaultView || window;
                if (x.getSelection) {
                  var z = x.getSelection(), X = o.textContent.length, k = Math.min(y.start, X), zt = y.end === void 0 ? k : Math.min(y.end, X);
                  !z.extend && k > zt && (c = zt, zt = k, k = c);
                  var p = fr(
                    o,
                    k
                  ), v = fr(
                    o,
                    zt
                  );
                  if (p && v && (z.rangeCount !== 1 || z.anchorNode !== p.node || z.anchorOffset !== p.offset || z.focusNode !== v.node || z.focusOffset !== v.offset)) {
                    var _ = U.createRange();
                    _.setStart(p.node, p.offset), z.removeAllRanges(), k > zt ? (z.addRange(_), z.extend(v.node, v.offset)) : (_.setEnd(v.node, v.offset), z.addRange(_));
                  }
                }
              }
            }
            for (U = [], z = o; z = z.parentNode; )
              z.nodeType === 1 && U.push({
                element: z,
                left: z.scrollLeft,
                top: z.scrollTop
              });
            for (typeof o.focus == "function" && o.focus(), o = 0; o < U.length; o++) {
              var M = U[o];
              M.element.scrollLeft = M.left, M.element.scrollTop = M.top;
            }
          }
          Ri = !!Xf, Zf = Xf = null;
        } finally {
          St = a, H.p = n, N.T = l;
        }
      }
      t.current = e, Wt = 2;
    }
  }
  function zd() {
    if (Wt === 2) {
      Wt = 0;
      var t = Jl, e = da, l = (e.flags & 8772) !== 0;
      if ((e.subtreeFlags & 8772) !== 0 || l) {
        l = N.T, N.T = null;
        var n = H.p;
        H.p = 2;
        var a = St;
        St |= 4;
        try {
          td(t, e.alternate, e);
        } finally {
          St = a, H.p = n, N.T = l;
        }
      }
      Wt = 3;
    }
  }
  function qd() {
    if (Wt === 4 || Wt === 3) {
      Wt = 0, Dn();
      var t = Jl, e = da, l = El, n = yd;
      (e.subtreeFlags & 10256) !== 0 || (e.flags & 10256) !== 0 ? Wt = 5 : (Wt = 0, da = Jl = null, Nd(t, t.pendingLanes));
      var a = t.pendingLanes;
      if (a === 0 && (wl = null), ki(l), e = e.stateNode, $t && typeof $t.onCommitFiberRoot == "function")
        try {
          $t.onCommitFiberRoot(
            Ol,
            e,
            void 0,
            (e.current.flags & 128) === 128
          );
        } catch {
        }
      if (n !== null) {
        e = N.T, a = H.p, H.p = 2, N.T = null;
        try {
          for (var u = t.onRecoverableError, c = 0; c < n.length; c++) {
            var o = n[c];
            u(o.value, {
              componentStack: o.stack
            });
          }
        } finally {
          N.T = e, H.p = a;
        }
      }
      (El & 3) !== 0 && xi(), el(t), a = t.pendingLanes, (l & 261930) !== 0 && (a & 42) !== 0 ? t === Of ? su++ : (su = 0, Of = t) : su = 0, ru(0);
    }
  }
  function Nd(t, e) {
    (t.pooledCacheLanes &= e) === 0 && (e = t.pooledCache, e != null && (t.pooledCache = null, Va(e)));
  }
  function xi() {
    return Td(), zd(), qd(), Od();
  }
  function Od() {
    if (Wt !== 5) return !1;
    var t = Jl, e = qf;
    qf = 0;
    var l = ki(El), n = N.T, a = H.p;
    try {
      H.p = 32 > l ? 32 : l, N.T = null, l = Nf, Nf = null;
      var u = Jl, c = El;
      if (Wt = 0, da = Jl = null, El = 0, (St & 6) !== 0) throw Error(r(331));
      var o = St;
      if (St |= 4, rd(u.current), cd(
        u,
        u.current,
        c,
        l
      ), St = o, ru(0, !1), $t && typeof $t.onPostCommitFiberRoot == "function")
        try {
          $t.onPostCommitFiberRoot(Ol, u);
        } catch {
        }
      return !0;
    } finally {
      H.p = a, N.T = n, Nd(t, e);
    }
  }
  function Md(t, e, l) {
    e = je(l, e), e = cf(t.stateNode, e, 2), t = Yl(t, e, 2), t !== null && (R(t, 2), el(t));
  }
  function xt(t, e, l) {
    if (t.tag === 3)
      Md(t, t, l);
    else
      for (; e !== null; ) {
        if (e.tag === 3) {
          Md(
            e,
            t,
            l
          );
          break;
        } else if (e.tag === 1) {
          var n = e.stateNode;
          if (typeof e.type.getDerivedStateFromError == "function" || typeof n.componentDidCatch == "function" && (wl === null || !wl.has(n))) {
            t = je(l, t), l = Do(2), n = Yl(e, l, 2), n !== null && (Uo(
              l,
              n,
              e,
              t
            ), R(n, 2), el(n));
            break;
          }
        }
        e = e.return;
      }
  }
  function Uf(t, e, l) {
    var n = t.pingCache;
    if (n === null) {
      n = t.pingCache = new ch();
      var a = /* @__PURE__ */ new Set();
      n.set(e, a);
    } else
      a = n.get(e), a === void 0 && (a = /* @__PURE__ */ new Set(), n.set(e, a));
    a.has(l) || (Af = !0, a.add(l), t = dh.bind(null, t, e, l), e.then(t, t));
  }
  function dh(t, e, l) {
    var n = t.pingCache;
    n !== null && n.delete(e), t.pingedLanes |= t.suspendedLanes & l, t.warmLanes &= ~l, qt === t && (rt & l) === l && (Ht === 4 || Ht === 3 && (rt & 62914560) === rt && 300 > Nt() - gi ? (St & 2) === 0 && ya(t, 0) : Tf |= l, oa === rt && (oa = 0)), el(t);
  }
  function Dd(t, e) {
    e === 0 && (e = rn()), t = hn(t, e), t !== null && (R(t, e), el(t));
  }
  function yh(t) {
    var e = t.memoizedState, l = 0;
    e !== null && (l = e.retryLane), Dd(t, l);
  }
  function mh(t, e) {
    var l = 0;
    switch (t.tag) {
      case 31:
      case 13:
        var n = t.stateNode, a = t.memoizedState;
        a !== null && (l = a.retryLane);
        break;
      case 19:
        n = t.stateNode;
        break;
      case 22:
        n = t.stateNode._retryCache;
        break;
      default:
        throw Error(r(314));
    }
    n !== null && n.delete(e), Dd(t, l);
  }
  function hh(t, e) {
    return Na(t, e);
  }
  var Ai = null, ha = null, jf = !1, Ti = !1, Cf = !1, kl = 0;
  function el(t) {
    t !== ha && t.next === null && (ha === null ? Ai = ha = t : ha = ha.next = t), Ti = !0, jf || (jf = !0, gh());
  }
  function ru(t, e) {
    if (!Cf && Ti) {
      Cf = !0;
      do
        for (var l = !1, n = Ai; n !== null; ) {
          if (t !== 0) {
            var a = n.pendingLanes;
            if (a === 0) var u = 0;
            else {
              var c = n.suspendedLanes, o = n.pingedLanes;
              u = (1 << 31 - te(42 | t) + 1) - 1, u &= a & ~(c & ~o), u = u & 201326741 ? u & 201326741 | 1 : u ? u | 2 : 0;
            }
            u !== 0 && (l = !0, Rd(n, u));
          } else
            u = rt, u = Ml(
              n,
              n === qt ? u : 0,
              n.cancelPendingCommit !== null || n.timeoutHandle !== -1
            ), (u & 3) === 0 || Fe(n, u) || (l = !0, Rd(n, u));
          n = n.next;
        }
      while (l);
      Cf = !1;
    }
  }
  function vh() {
    Ud();
  }
  function Ud() {
    Ti = jf = !1;
    var t = 0;
    kl !== 0 && qh() && (t = kl);
    for (var e = Nt(), l = null, n = Ai; n !== null; ) {
      var a = n.next, u = jd(n, e);
      u === 0 ? (n.next = null, l === null ? Ai = a : l.next = a, a === null && (ha = l)) : (l = n, (t !== 0 || (u & 3) !== 0) && (Ti = !0)), n = a;
    }
    Wt !== 0 && Wt !== 5 || ru(t), kl !== 0 && (kl = 0);
  }
  function jd(t, e) {
    for (var l = t.suspendedLanes, n = t.pingedLanes, a = t.expirationTimes, u = t.pendingLanes & -62914561; 0 < u; ) {
      var c = 31 - te(u), o = 1 << c, y = a[c];
      y === -1 ? ((o & l) === 0 || (o & n) !== 0) && (a[c] = Ou(o, e)) : y <= e && (t.expiredLanes |= o), u &= ~o;
    }
    if (e = qt, l = rt, l = Ml(
      t,
      t === e ? l : 0,
      t.cancelPendingCommit !== null || t.timeoutHandle !== -1
    ), n = t.callbackNode, l === 0 || t === e && (Et === 2 || Et === 9) || t.cancelPendingCommit !== null)
      return n !== null && n !== null && Oa(n), t.callbackNode = null, t.callbackPriority = 0;
    if ((l & 3) === 0 || Fe(t, l)) {
      if (e = l & -l, e === t.callbackPriority) return e;
      switch (n !== null && Oa(n), ki(l)) {
        case 2:
        case 8:
          l = fn;
          break;
        case 32:
          l = Un;
          break;
        case 268435456:
          l = jn;
          break;
        default:
          l = Un;
      }
      return n = Cd.bind(null, t), l = Na(l, n), t.callbackPriority = e, t.callbackNode = l, e;
    }
    return n !== null && n !== null && Oa(n), t.callbackPriority = 2, t.callbackNode = null, 2;
  }
  function Cd(t, e) {
    if (Wt !== 0 && Wt !== 5)
      return t.callbackNode = null, t.callbackPriority = 0, null;
    var l = t.callbackNode;
    if (xi() && t.callbackNode !== l)
      return null;
    var n = rt;
    return n = Ml(
      t,
      t === qt ? n : 0,
      t.cancelPendingCommit !== null || t.timeoutHandle !== -1
    ), n === 0 ? null : (hd(t, n, e), jd(t, Nt()), t.callbackNode != null && t.callbackNode === l ? Cd.bind(null, t) : null);
  }
  function Rd(t, e) {
    if (xi()) return null;
    hd(t, e, !0);
  }
  function gh() {
    Oh(function() {
      (St & 6) !== 0 ? Na(
        cn,
        vh
      ) : Ud();
    });
  }
  function Rf() {
    if (kl === 0) {
      var t = ta;
      t === 0 && (t = Cn, Cn <<= 1, (Cn & 261888) === 0 && (Cn = 256)), kl = t;
    }
    return kl;
  }
  function Hd(t) {
    return t == null || typeof t == "symbol" || typeof t == "boolean" ? null : typeof t == "function" ? t : ju("" + t);
  }
  function Ld(t, e) {
    var l = e.ownerDocument.createElement("input");
    return l.name = e.name, l.value = e.value, t.id && l.setAttribute("form", t.id), e.parentNode.insertBefore(l, e), t = new FormData(t), l.parentNode.removeChild(l), t;
  }
  function ph(t, e, l, n, a) {
    if (e === "submit" && l && l.stateNode === a) {
      var u = Hd(
        (a[ye] || null).action
      ), c = n.submitter;
      c && (e = (e = c[ye] || null) ? Hd(e.formAction) : c.getAttribute("formAction"), e !== null && (u = e, c = null));
      var o = new Lu(
        "action",
        "action",
        null,
        n,
        a
      );
      t.push({
        event: o,
        listeners: [
          {
            instance: null,
            listener: function() {
              if (n.defaultPrevented) {
                if (kl !== 0) {
                  var y = c ? Ld(a, c) : new FormData(a);
                  tf(
                    l,
                    {
                      pending: !0,
                      data: y,
                      method: a.method,
                      action: u
                    },
                    null,
                    y
                  );
                }
              } else
                typeof u == "function" && (o.preventDefault(), y = c ? Ld(a, c) : new FormData(a), tf(
                  l,
                  {
                    pending: !0,
                    data: y,
                    method: a.method,
                    action: u
                  },
                  u,
                  y
                ));
            },
            currentTarget: a
          }
        ]
      });
    }
  }
  for (var Hf = 0; Hf < gc.length; Hf++) {
    var Lf = gc[Hf], bh = Lf.toLowerCase(), Sh = Lf[0].toUpperCase() + Lf.slice(1);
    Ze(
      bh,
      "on" + Sh
    );
  }
  Ze(yr, "onAnimationEnd"), Ze(mr, "onAnimationIteration"), Ze(hr, "onAnimationStart"), Ze("dblclick", "onDoubleClick"), Ze("focusin", "onFocus"), Ze("focusout", "onBlur"), Ze(Hm, "onTransitionRun"), Ze(Lm, "onTransitionStart"), Ze(Bm, "onTransitionCancel"), Ze(vr, "onTransitionEnd"), Gn("onMouseEnter", ["mouseout", "mouseover"]), Gn("onMouseLeave", ["mouseout", "mouseover"]), Gn("onPointerEnter", ["pointerout", "pointerover"]), Gn("onPointerLeave", ["pointerout", "pointerover"]), on(
    "onChange",
    "change click focusin focusout input keydown keyup selectionchange".split(" ")
  ), on(
    "onSelect",
    "focusout contextmenu dragend focusin keydown keyup mousedown mouseup selectionchange".split(
      " "
    )
  ), on("onBeforeInput", [
    "compositionend",
    "keypress",
    "textInput",
    "paste"
  ]), on(
    "onCompositionEnd",
    "compositionend focusout keydown keypress keyup mousedown".split(" ")
  ), on(
    "onCompositionStart",
    "compositionstart focusout keydown keypress keyup mousedown".split(" ")
  ), on(
    "onCompositionUpdate",
    "compositionupdate focusout keydown keypress keyup mousedown".split(" ")
  );
  var ou = "abort canplay canplaythrough durationchange emptied encrypted ended error loadeddata loadedmetadata loadstart pause play playing progress ratechange resize seeked seeking stalled suspend timeupdate volumechange waiting".split(
    " "
  ), _h = new Set(
    "beforetoggle cancel close invalid load scroll scrollend toggle".split(" ").concat(ou)
  );
  function Bd(t, e) {
    e = (e & 4) !== 0;
    for (var l = 0; l < t.length; l++) {
      var n = t[l], a = n.event;
      n = n.listeners;
      t: {
        var u = void 0;
        if (e)
          for (var c = n.length - 1; 0 <= c; c--) {
            var o = n[c], y = o.instance, E = o.currentTarget;
            if (o = o.listener, y !== u && a.isPropagationStopped())
              break t;
            u = o, a.currentTarget = E;
            try {
              u(a);
            } catch (O) {
              Gu(O);
            }
            a.currentTarget = null, u = y;
          }
        else
          for (c = 0; c < n.length; c++) {
            if (o = n[c], y = o.instance, E = o.currentTarget, o = o.listener, y !== u && a.isPropagationStopped())
              break t;
            u = o, a.currentTarget = E;
            try {
              u(a);
            } catch (O) {
              Gu(O);
            }
            a.currentTarget = null, u = y;
          }
      }
    }
  }
  function st(t, e) {
    var l = e[$i];
    l === void 0 && (l = e[$i] = /* @__PURE__ */ new Set());
    var n = t + "__bubble";
    l.has(n) || (Yd(e, t, 2, !1), l.add(n));
  }
  function Bf(t, e, l) {
    var n = 0;
    e && (n |= 4), Yd(
      l,
      t,
      n,
      e
    );
  }
  var zi = "_reactListening" + Math.random().toString(36).slice(2);
  function Yf(t) {
    if (!t[zi]) {
      t[zi] = !0, Ds.forEach(function(l) {
        l !== "selectionchange" && (_h.has(l) || Bf(l, !1, t), Bf(l, !0, t));
      });
      var e = t.nodeType === 9 ? t : t.ownerDocument;
      e === null || e[zi] || (e[zi] = !0, Bf("selectionchange", !1, e));
    }
  }
  function Yd(t, e, l, n) {
    switch (my(e)) {
      case 2:
        var a = $h;
        break;
      case 8:
        a = Wh;
        break;
      default:
        a = ts;
    }
    l = a.bind(
      null,
      e,
      l,
      t
    ), a = void 0, !ac || e !== "touchstart" && e !== "touchmove" && e !== "wheel" || (a = !0), n ? a !== void 0 ? t.addEventListener(e, l, {
      capture: !0,
      passive: a
    }) : t.addEventListener(e, l, !0) : a !== void 0 ? t.addEventListener(e, l, {
      passive: a
    }) : t.addEventListener(e, l, !1);
  }
  function Gf(t, e, l, n, a) {
    var u = n;
    if ((e & 1) === 0 && (e & 2) === 0 && n !== null)
      t: for (; ; ) {
        if (n === null) return;
        var c = n.tag;
        if (c === 3 || c === 4) {
          var o = n.stateNode.containerInfo;
          if (o === a) break;
          if (c === 4)
            for (c = n.return; c !== null; ) {
              var y = c.tag;
              if ((y === 3 || y === 4) && c.stateNode.containerInfo === a)
                return;
              c = c.return;
            }
          for (; o !== null; ) {
            if (c = Ln(o), c === null) return;
            if (y = c.tag, y === 5 || y === 6 || y === 26 || y === 27) {
              n = u = c;
              continue t;
            }
            o = o.parentNode;
          }
        }
        n = n.return;
      }
    Zs(function() {
      var E = u, O = lc(l), U = [];
      t: {
        var x = gr.get(t);
        if (x !== void 0) {
          var z = Lu, X = t;
          switch (t) {
            case "keypress":
              if (Ru(l) === 0) break t;
            case "keydown":
            case "keyup":
              z = mm;
              break;
            case "focusin":
              X = "focus", z = fc;
              break;
            case "focusout":
              X = "blur", z = fc;
              break;
            case "beforeblur":
            case "afterblur":
              z = fc;
              break;
            case "click":
              if (l.button === 2) break t;
            case "auxclick":
            case "dblclick":
            case "mousedown":
            case "mousemove":
            case "mouseup":
            case "mouseout":
            case "mouseover":
            case "contextmenu":
              z = Js;
              break;
            case "drag":
            case "dragend":
            case "dragenter":
            case "dragexit":
            case "dragleave":
            case "dragover":
            case "dragstart":
            case "drop":
              z = lm;
              break;
            case "touchcancel":
            case "touchend":
            case "touchmove":
            case "touchstart":
              z = gm;
              break;
            case yr:
            case mr:
            case hr:
              z = um;
              break;
            case vr:
              z = bm;
              break;
            case "scroll":
            case "scrollend":
              z = tm;
              break;
            case "wheel":
              z = _m;
              break;
            case "copy":
            case "cut":
            case "paste":
              z = cm;
              break;
            case "gotpointercapture":
            case "lostpointercapture":
            case "pointercancel":
            case "pointerdown":
            case "pointermove":
            case "pointerout":
            case "pointerover":
            case "pointerup":
              z = ks;
              break;
            case "toggle":
            case "beforetoggle":
              z = xm;
          }
          var k = (e & 4) !== 0, zt = !k && (t === "scroll" || t === "scrollend"), p = k ? x !== null ? x + "Capture" : null : x;
          k = [];
          for (var v = E, _; v !== null; ) {
            var M = v;
            if (_ = M.stateNode, M = M.tag, M !== 5 && M !== 26 && M !== 27 || _ === null || p === null || (M = ja(v, p), M != null && k.push(
              du(v, M, _)
            )), zt) break;
            v = v.return;
          }
          0 < k.length && (x = new z(
            x,
            X,
            null,
            l,
            O
          ), U.push({ event: x, listeners: k }));
        }
      }
      if ((e & 7) === 0) {
        t: {
          if (x = t === "mouseover" || t === "pointerover", z = t === "mouseout" || t === "pointerout", x && l !== ec && (X = l.relatedTarget || l.fromElement) && (Ln(X) || X[Hn]))
            break t;
          if ((z || x) && (x = O.window === O ? O : (x = O.ownerDocument) ? x.defaultView || x.parentWindow : window, z ? (X = l.relatedTarget || l.toElement, z = E, X = X ? Ln(X) : null, X !== null && (zt = m(X), k = X.tag, X !== zt || k !== 5 && k !== 27 && k !== 6) && (X = null)) : (z = null, X = E), z !== X)) {
            if (k = Js, M = "onMouseLeave", p = "onMouseEnter", v = "mouse", (t === "pointerout" || t === "pointerover") && (k = ks, M = "onPointerLeave", p = "onPointerEnter", v = "pointer"), zt = z == null ? x : Ua(z), _ = X == null ? x : Ua(X), x = new k(
              M,
              v + "leave",
              z,
              l,
              O
            ), x.target = zt, x.relatedTarget = _, M = null, Ln(O) === E && (k = new k(
              p,
              v + "enter",
              X,
              l,
              O
            ), k.target = _, k.relatedTarget = zt, M = k), zt = M, z && X)
              e: {
                for (k = Eh, p = z, v = X, _ = 0, M = p; M; M = k(M))
                  _++;
                M = 0;
                for (var J = v; J; J = k(J))
                  M++;
                for (; 0 < _ - M; )
                  p = k(p), _--;
                for (; 0 < M - _; )
                  v = k(v), M--;
                for (; _--; ) {
                  if (p === v || v !== null && p === v.alternate) {
                    k = p;
                    break e;
                  }
                  p = k(p), v = k(v);
                }
                k = null;
              }
            else k = null;
            z !== null && Gd(
              U,
              x,
              z,
              k,
              !1
            ), X !== null && zt !== null && Gd(
              U,
              zt,
              X,
              k,
              !0
            );
          }
        }
        t: {
          if (x = E ? Ua(E) : window, z = x.nodeName && x.nodeName.toLowerCase(), z === "select" || z === "input" && x.type === "file")
            var gt = lr;
          else if (tr(x))
            if (nr)
              gt = jm;
            else {
              gt = Dm;
              var Z = Mm;
            }
          else
            z = x.nodeName, !z || z.toLowerCase() !== "input" || x.type !== "checkbox" && x.type !== "radio" ? E && tc(E.elementType) && (gt = lr) : gt = Um;
          if (gt && (gt = gt(t, E))) {
            er(
              U,
              gt,
              l,
              O
            );
            break t;
          }
          Z && Z(t, x, E), t === "focusout" && E && x.type === "number" && E.memoizedProps.value != null && Pi(x, "number", x.value);
        }
        switch (Z = E ? Ua(E) : window, t) {
          case "focusin":
            (tr(Z) || Z.contentEditable === "true") && (Jn = Z, mc = E, Qa = null);
            break;
          case "focusout":
            Qa = mc = Jn = null;
            break;
          case "mousedown":
            hc = !0;
            break;
          case "contextmenu":
          case "mouseup":
          case "dragend":
            hc = !1, or(U, l, O);
            break;
          case "selectionchange":
            if (Rm) break;
          case "keydown":
          case "keyup":
            or(U, l, O);
        }
        var ut;
        if (rc)
          t: {
            switch (t) {
              case "compositionstart":
                var ot = "onCompositionStart";
                break t;
              case "compositionend":
                ot = "onCompositionEnd";
                break t;
              case "compositionupdate":
                ot = "onCompositionUpdate";
                break t;
            }
            ot = void 0;
          }
        else
          wn ? Is(t, l) && (ot = "onCompositionEnd") : t === "keydown" && l.keyCode === 229 && (ot = "onCompositionStart");
        ot && ($s && l.locale !== "ko" && (wn || ot !== "onCompositionStart" ? ot === "onCompositionEnd" && wn && (ut = Vs()) : (Ul = O, uc = "value" in Ul ? Ul.value : Ul.textContent, wn = !0)), Z = qi(E, ot), 0 < Z.length && (ot = new Ks(
          ot,
          t,
          null,
          l,
          O
        ), U.push({ event: ot, listeners: Z }), ut ? ot.data = ut : (ut = Ps(l), ut !== null && (ot.data = ut)))), (ut = Tm ? zm(t, l) : qm(t, l)) && (ot = qi(E, "onBeforeInput"), 0 < ot.length && (Z = new Ks(
          "onBeforeInput",
          "beforeinput",
          null,
          l,
          O
        ), U.push({
          event: Z,
          listeners: ot
        }), Z.data = ut)), ph(
          U,
          t,
          E,
          l,
          O
        );
      }
      Bd(U, e);
    });
  }
  function du(t, e, l) {
    return {
      instance: t,
      listener: e,
      currentTarget: l
    };
  }
  function qi(t, e) {
    for (var l = e + "Capture", n = []; t !== null; ) {
      var a = t, u = a.stateNode;
      if (a = a.tag, a !== 5 && a !== 26 && a !== 27 || u === null || (a = ja(t, l), a != null && n.unshift(
        du(t, a, u)
      ), a = ja(t, e), a != null && n.push(
        du(t, a, u)
      )), t.tag === 3) return n;
      t = t.return;
    }
    return [];
  }
  function Eh(t) {
    if (t === null) return null;
    do
      t = t.return;
    while (t && t.tag !== 5 && t.tag !== 27);
    return t || null;
  }
  function Gd(t, e, l, n, a) {
    for (var u = e._reactName, c = []; l !== null && l !== n; ) {
      var o = l, y = o.alternate, E = o.stateNode;
      if (o = o.tag, y !== null && y === n) break;
      o !== 5 && o !== 26 && o !== 27 || E === null || (y = E, a ? (E = ja(l, u), E != null && c.unshift(
        du(l, E, y)
      )) : a || (E = ja(l, u), E != null && c.push(
        du(l, E, y)
      ))), l = l.return;
    }
    c.length !== 0 && t.push({ event: e, listeners: c });
  }
  var xh = /\r\n?/g, Ah = /\u0000|\uFFFD/g;
  function Qd(t) {
    return (typeof t == "string" ? t : "" + t).replace(xh, `
`).replace(Ah, "");
  }
  function Xd(t, e) {
    return e = Qd(e), Qd(t) === e;
  }
  function Tt(t, e, l, n, a, u) {
    switch (l) {
      case "children":
        typeof n == "string" ? e === "body" || e === "textarea" && n === "" || Xn(t, n) : (typeof n == "number" || typeof n == "bigint") && e !== "body" && Xn(t, "" + n);
        break;
      case "className":
        Du(t, "class", n);
        break;
      case "tabIndex":
        Du(t, "tabindex", n);
        break;
      case "dir":
      case "role":
      case "viewBox":
      case "width":
      case "height":
        Du(t, l, n);
        break;
      case "style":
        Qs(t, n, u);
        break;
      case "data":
        if (e !== "object") {
          Du(t, "data", n);
          break;
        }
      case "src":
      case "href":
        if (n === "" && (e !== "a" || l !== "href")) {
          t.removeAttribute(l);
          break;
        }
        if (n == null || typeof n == "function" || typeof n == "symbol" || typeof n == "boolean") {
          t.removeAttribute(l);
          break;
        }
        n = ju("" + n), t.setAttribute(l, n);
        break;
      case "action":
      case "formAction":
        if (typeof n == "function") {
          t.setAttribute(
            l,
            "javascript:throw new Error('A React form was unexpectedly submitted. If you called form.submit() manually, consider using form.requestSubmit() instead. If you\\'re trying to use event.stopPropagation() in a submit event handler, consider also calling event.preventDefault().')"
          );
          break;
        } else
          typeof u == "function" && (l === "formAction" ? (e !== "input" && Tt(t, e, "name", a.name, a, null), Tt(
            t,
            e,
            "formEncType",
            a.formEncType,
            a,
            null
          ), Tt(
            t,
            e,
            "formMethod",
            a.formMethod,
            a,
            null
          ), Tt(
            t,
            e,
            "formTarget",
            a.formTarget,
            a,
            null
          )) : (Tt(t, e, "encType", a.encType, a, null), Tt(t, e, "method", a.method, a, null), Tt(t, e, "target", a.target, a, null)));
        if (n == null || typeof n == "symbol" || typeof n == "boolean") {
          t.removeAttribute(l);
          break;
        }
        n = ju("" + n), t.setAttribute(l, n);
        break;
      case "onClick":
        n != null && (t.onclick = cl);
        break;
      case "onScroll":
        n != null && st("scroll", t);
        break;
      case "onScrollEnd":
        n != null && st("scrollend", t);
        break;
      case "dangerouslySetInnerHTML":
        if (n != null) {
          if (typeof n != "object" || !("__html" in n))
            throw Error(r(61));
          if (l = n.__html, l != null) {
            if (a.children != null) throw Error(r(60));
            t.innerHTML = l;
          }
        }
        break;
      case "multiple":
        t.multiple = n && typeof n != "function" && typeof n != "symbol";
        break;
      case "muted":
        t.muted = n && typeof n != "function" && typeof n != "symbol";
        break;
      case "suppressContentEditableWarning":
      case "suppressHydrationWarning":
      case "defaultValue":
      case "defaultChecked":
      case "innerHTML":
      case "ref":
        break;
      case "autoFocus":
        break;
      case "xlinkHref":
        if (n == null || typeof n == "function" || typeof n == "boolean" || typeof n == "symbol") {
          t.removeAttribute("xlink:href");
          break;
        }
        l = ju("" + n), t.setAttributeNS(
          "http://www.w3.org/1999/xlink",
          "xlink:href",
          l
        );
        break;
      case "contentEditable":
      case "spellCheck":
      case "draggable":
      case "value":
      case "autoReverse":
      case "externalResourcesRequired":
      case "focusable":
      case "preserveAlpha":
        n != null && typeof n != "function" && typeof n != "symbol" ? t.setAttribute(l, "" + n) : t.removeAttribute(l);
        break;
      case "inert":
      case "allowFullScreen":
      case "async":
      case "autoPlay":
      case "controls":
      case "default":
      case "defer":
      case "disabled":
      case "disablePictureInPicture":
      case "disableRemotePlayback":
      case "formNoValidate":
      case "hidden":
      case "loop":
      case "noModule":
      case "noValidate":
      case "open":
      case "playsInline":
      case "readOnly":
      case "required":
      case "reversed":
      case "scoped":
      case "seamless":
      case "itemScope":
        n && typeof n != "function" && typeof n != "symbol" ? t.setAttribute(l, "") : t.removeAttribute(l);
        break;
      case "capture":
      case "download":
        n === !0 ? t.setAttribute(l, "") : n !== !1 && n != null && typeof n != "function" && typeof n != "symbol" ? t.setAttribute(l, n) : t.removeAttribute(l);
        break;
      case "cols":
      case "rows":
      case "size":
      case "span":
        n != null && typeof n != "function" && typeof n != "symbol" && !isNaN(n) && 1 <= n ? t.setAttribute(l, n) : t.removeAttribute(l);
        break;
      case "rowSpan":
      case "start":
        n == null || typeof n == "function" || typeof n == "symbol" || isNaN(n) ? t.removeAttribute(l) : t.setAttribute(l, n);
        break;
      case "popover":
        st("beforetoggle", t), st("toggle", t), Mu(t, "popover", n);
        break;
      case "xlinkActuate":
        il(
          t,
          "http://www.w3.org/1999/xlink",
          "xlink:actuate",
          n
        );
        break;
      case "xlinkArcrole":
        il(
          t,
          "http://www.w3.org/1999/xlink",
          "xlink:arcrole",
          n
        );
        break;
      case "xlinkRole":
        il(
          t,
          "http://www.w3.org/1999/xlink",
          "xlink:role",
          n
        );
        break;
      case "xlinkShow":
        il(
          t,
          "http://www.w3.org/1999/xlink",
          "xlink:show",
          n
        );
        break;
      case "xlinkTitle":
        il(
          t,
          "http://www.w3.org/1999/xlink",
          "xlink:title",
          n
        );
        break;
      case "xlinkType":
        il(
          t,
          "http://www.w3.org/1999/xlink",
          "xlink:type",
          n
        );
        break;
      case "xmlBase":
        il(
          t,
          "http://www.w3.org/XML/1998/namespace",
          "xml:base",
          n
        );
        break;
      case "xmlLang":
        il(
          t,
          "http://www.w3.org/XML/1998/namespace",
          "xml:lang",
          n
        );
        break;
      case "xmlSpace":
        il(
          t,
          "http://www.w3.org/XML/1998/namespace",
          "xml:space",
          n
        );
        break;
      case "is":
        Mu(t, "is", n);
        break;
      case "innerText":
      case "textContent":
        break;
      default:
        (!(2 < l.length) || l[0] !== "o" && l[0] !== "O" || l[1] !== "n" && l[1] !== "N") && (l = Iy.get(l) || l, Mu(t, l, n));
    }
  }
  function Qf(t, e, l, n, a, u) {
    switch (l) {
      case "style":
        Qs(t, n, u);
        break;
      case "dangerouslySetInnerHTML":
        if (n != null) {
          if (typeof n != "object" || !("__html" in n))
            throw Error(r(61));
          if (l = n.__html, l != null) {
            if (a.children != null) throw Error(r(60));
            t.innerHTML = l;
          }
        }
        break;
      case "children":
        typeof n == "string" ? Xn(t, n) : (typeof n == "number" || typeof n == "bigint") && Xn(t, "" + n);
        break;
      case "onScroll":
        n != null && st("scroll", t);
        break;
      case "onScrollEnd":
        n != null && st("scrollend", t);
        break;
      case "onClick":
        n != null && (t.onclick = cl);
        break;
      case "suppressContentEditableWarning":
      case "suppressHydrationWarning":
      case "innerHTML":
      case "ref":
        break;
      case "innerText":
      case "textContent":
        break;
      default:
        if (!Us.hasOwnProperty(l))
          t: {
            if (l[0] === "o" && l[1] === "n" && (a = l.endsWith("Capture"), e = l.slice(2, a ? l.length - 7 : void 0), u = t[ye] || null, u = u != null ? u[l] : null, typeof u == "function" && t.removeEventListener(e, u, a), typeof n == "function")) {
              typeof u != "function" && u !== null && (l in t ? t[l] = null : t.hasAttribute(l) && t.removeAttribute(l)), t.addEventListener(e, n, a);
              break t;
            }
            l in t ? t[l] = n : n === !0 ? t.setAttribute(l, "") : Mu(t, l, n);
          }
    }
  }
  function ue(t, e, l) {
    switch (e) {
      case "div":
      case "span":
      case "svg":
      case "path":
      case "a":
      case "g":
      case "p":
      case "li":
        break;
      case "img":
        st("error", t), st("load", t);
        var n = !1, a = !1, u;
        for (u in l)
          if (l.hasOwnProperty(u)) {
            var c = l[u];
            if (c != null)
              switch (u) {
                case "src":
                  n = !0;
                  break;
                case "srcSet":
                  a = !0;
                  break;
                case "children":
                case "dangerouslySetInnerHTML":
                  throw Error(r(137, e));
                default:
                  Tt(t, e, u, c, l, null);
              }
          }
        a && Tt(t, e, "srcSet", l.srcSet, l, null), n && Tt(t, e, "src", l.src, l, null);
        return;
      case "input":
        st("invalid", t);
        var o = u = c = a = null, y = null, E = null;
        for (n in l)
          if (l.hasOwnProperty(n)) {
            var O = l[n];
            if (O != null)
              switch (n) {
                case "name":
                  a = O;
                  break;
                case "type":
                  c = O;
                  break;
                case "checked":
                  y = O;
                  break;
                case "defaultChecked":
                  E = O;
                  break;
                case "value":
                  u = O;
                  break;
                case "defaultValue":
                  o = O;
                  break;
                case "children":
                case "dangerouslySetInnerHTML":
                  if (O != null)
                    throw Error(r(137, e));
                  break;
                default:
                  Tt(t, e, n, O, l, null);
              }
          }
        Ls(
          t,
          u,
          o,
          y,
          E,
          c,
          a,
          !1
        );
        return;
      case "select":
        st("invalid", t), n = c = u = null;
        for (a in l)
          if (l.hasOwnProperty(a) && (o = l[a], o != null))
            switch (a) {
              case "value":
                u = o;
                break;
              case "defaultValue":
                c = o;
                break;
              case "multiple":
                n = o;
              default:
                Tt(t, e, a, o, l, null);
            }
        e = u, l = c, t.multiple = !!n, e != null ? Qn(t, !!n, e, !1) : l != null && Qn(t, !!n, l, !0);
        return;
      case "textarea":
        st("invalid", t), u = a = n = null;
        for (c in l)
          if (l.hasOwnProperty(c) && (o = l[c], o != null))
            switch (c) {
              case "value":
                n = o;
                break;
              case "defaultValue":
                a = o;
                break;
              case "children":
                u = o;
                break;
              case "dangerouslySetInnerHTML":
                if (o != null) throw Error(r(91));
                break;
              default:
                Tt(t, e, c, o, l, null);
            }
        Ys(t, n, a, u);
        return;
      case "option":
        for (y in l)
          if (l.hasOwnProperty(y) && (n = l[y], n != null))
            switch (y) {
              case "selected":
                t.selected = n && typeof n != "function" && typeof n != "symbol";
                break;
              default:
                Tt(t, e, y, n, l, null);
            }
        return;
      case "dialog":
        st("beforetoggle", t), st("toggle", t), st("cancel", t), st("close", t);
        break;
      case "iframe":
      case "object":
        st("load", t);
        break;
      case "video":
      case "audio":
        for (n = 0; n < ou.length; n++)
          st(ou[n], t);
        break;
      case "image":
        st("error", t), st("load", t);
        break;
      case "details":
        st("toggle", t);
        break;
      case "embed":
      case "source":
      case "link":
        st("error", t), st("load", t);
      case "area":
      case "base":
      case "br":
      case "col":
      case "hr":
      case "keygen":
      case "meta":
      case "param":
      case "track":
      case "wbr":
      case "menuitem":
        for (E in l)
          if (l.hasOwnProperty(E) && (n = l[E], n != null))
            switch (E) {
              case "children":
              case "dangerouslySetInnerHTML":
                throw Error(r(137, e));
              default:
                Tt(t, e, E, n, l, null);
            }
        return;
      default:
        if (tc(e)) {
          for (O in l)
            l.hasOwnProperty(O) && (n = l[O], n !== void 0 && Qf(
              t,
              e,
              O,
              n,
              l,
              void 0
            ));
          return;
        }
    }
    for (o in l)
      l.hasOwnProperty(o) && (n = l[o], n != null && Tt(t, e, o, n, l, null));
  }
  function Th(t, e, l, n) {
    switch (e) {
      case "div":
      case "span":
      case "svg":
      case "path":
      case "a":
      case "g":
      case "p":
      case "li":
        break;
      case "input":
        var a = null, u = null, c = null, o = null, y = null, E = null, O = null;
        for (z in l) {
          var U = l[z];
          if (l.hasOwnProperty(z) && U != null)
            switch (z) {
              case "checked":
                break;
              case "value":
                break;
              case "defaultValue":
                y = U;
              default:
                n.hasOwnProperty(z) || Tt(t, e, z, null, n, U);
            }
        }
        for (var x in n) {
          var z = n[x];
          if (U = l[x], n.hasOwnProperty(x) && (z != null || U != null))
            switch (x) {
              case "type":
                u = z;
                break;
              case "name":
                a = z;
                break;
              case "checked":
                E = z;
                break;
              case "defaultChecked":
                O = z;
                break;
              case "value":
                c = z;
                break;
              case "defaultValue":
                o = z;
                break;
              case "children":
              case "dangerouslySetInnerHTML":
                if (z != null)
                  throw Error(r(137, e));
                break;
              default:
                z !== U && Tt(
                  t,
                  e,
                  x,
                  z,
                  n,
                  U
                );
            }
        }
        Ii(
          t,
          c,
          o,
          y,
          E,
          O,
          u,
          a
        );
        return;
      case "select":
        z = c = o = x = null;
        for (u in l)
          if (y = l[u], l.hasOwnProperty(u) && y != null)
            switch (u) {
              case "value":
                break;
              case "multiple":
                z = y;
              default:
                n.hasOwnProperty(u) || Tt(
                  t,
                  e,
                  u,
                  null,
                  n,
                  y
                );
            }
        for (a in n)
          if (u = n[a], y = l[a], n.hasOwnProperty(a) && (u != null || y != null))
            switch (a) {
              case "value":
                x = u;
                break;
              case "defaultValue":
                o = u;
                break;
              case "multiple":
                c = u;
              default:
                u !== y && Tt(
                  t,
                  e,
                  a,
                  u,
                  n,
                  y
                );
            }
        e = o, l = c, n = z, x != null ? Qn(t, !!l, x, !1) : !!n != !!l && (e != null ? Qn(t, !!l, e, !0) : Qn(t, !!l, l ? [] : "", !1));
        return;
      case "textarea":
        z = x = null;
        for (o in l)
          if (a = l[o], l.hasOwnProperty(o) && a != null && !n.hasOwnProperty(o))
            switch (o) {
              case "value":
                break;
              case "children":
                break;
              default:
                Tt(t, e, o, null, n, a);
            }
        for (c in n)
          if (a = n[c], u = l[c], n.hasOwnProperty(c) && (a != null || u != null))
            switch (c) {
              case "value":
                x = a;
                break;
              case "defaultValue":
                z = a;
                break;
              case "children":
                break;
              case "dangerouslySetInnerHTML":
                if (a != null) throw Error(r(91));
                break;
              default:
                a !== u && Tt(t, e, c, a, n, u);
            }
        Bs(t, x, z);
        return;
      case "option":
        for (var X in l)
          if (x = l[X], l.hasOwnProperty(X) && x != null && !n.hasOwnProperty(X))
            switch (X) {
              case "selected":
                t.selected = !1;
                break;
              default:
                Tt(
                  t,
                  e,
                  X,
                  null,
                  n,
                  x
                );
            }
        for (y in n)
          if (x = n[y], z = l[y], n.hasOwnProperty(y) && x !== z && (x != null || z != null))
            switch (y) {
              case "selected":
                t.selected = x && typeof x != "function" && typeof x != "symbol";
                break;
              default:
                Tt(
                  t,
                  e,
                  y,
                  x,
                  n,
                  z
                );
            }
        return;
      case "img":
      case "link":
      case "area":
      case "base":
      case "br":
      case "col":
      case "embed":
      case "hr":
      case "keygen":
      case "meta":
      case "param":
      case "source":
      case "track":
      case "wbr":
      case "menuitem":
        for (var k in l)
          x = l[k], l.hasOwnProperty(k) && x != null && !n.hasOwnProperty(k) && Tt(t, e, k, null, n, x);
        for (E in n)
          if (x = n[E], z = l[E], n.hasOwnProperty(E) && x !== z && (x != null || z != null))
            switch (E) {
              case "children":
              case "dangerouslySetInnerHTML":
                if (x != null)
                  throw Error(r(137, e));
                break;
              default:
                Tt(
                  t,
                  e,
                  E,
                  x,
                  n,
                  z
                );
            }
        return;
      default:
        if (tc(e)) {
          for (var zt in l)
            x = l[zt], l.hasOwnProperty(zt) && x !== void 0 && !n.hasOwnProperty(zt) && Qf(
              t,
              e,
              zt,
              void 0,
              n,
              x
            );
          for (O in n)
            x = n[O], z = l[O], !n.hasOwnProperty(O) || x === z || x === void 0 && z === void 0 || Qf(
              t,
              e,
              O,
              x,
              n,
              z
            );
          return;
        }
    }
    for (var p in l)
      x = l[p], l.hasOwnProperty(p) && x != null && !n.hasOwnProperty(p) && Tt(t, e, p, null, n, x);
    for (U in n)
      x = n[U], z = l[U], !n.hasOwnProperty(U) || x === z || x == null && z == null || Tt(t, e, U, x, n, z);
  }
  function Zd(t) {
    switch (t) {
      case "css":
      case "script":
      case "font":
      case "img":
      case "image":
      case "input":
      case "link":
        return !0;
      default:
        return !1;
    }
  }
  function zh() {
    if (typeof performance.getEntriesByType == "function") {
      for (var t = 0, e = 0, l = performance.getEntriesByType("resource"), n = 0; n < l.length; n++) {
        var a = l[n], u = a.transferSize, c = a.initiatorType, o = a.duration;
        if (u && o && Zd(c)) {
          for (c = 0, o = a.responseEnd, n += 1; n < l.length; n++) {
            var y = l[n], E = y.startTime;
            if (E > o) break;
            var O = y.transferSize, U = y.initiatorType;
            O && Zd(U) && (y = y.responseEnd, c += O * (y < o ? 1 : (o - E) / (y - E)));
          }
          if (--n, e += 8 * (u + c) / (a.duration / 1e3), t++, 10 < t) break;
        }
      }
      if (0 < t) return e / t / 1e6;
    }
    return navigator.connection && (t = navigator.connection.downlink, typeof t == "number") ? t : 5;
  }
  var Xf = null, Zf = null;
  function Ni(t) {
    return t.nodeType === 9 ? t : t.ownerDocument;
  }
  function Vd(t) {
    switch (t) {
      case "http://www.w3.org/2000/svg":
        return 1;
      case "http://www.w3.org/1998/Math/MathML":
        return 2;
      default:
        return 0;
    }
  }
  function wd(t, e) {
    if (t === 0)
      switch (e) {
        case "svg":
          return 1;
        case "math":
          return 2;
        default:
          return 0;
      }
    return t === 1 && e === "foreignObject" ? 0 : t;
  }
  function Vf(t, e) {
    return t === "textarea" || t === "noscript" || typeof e.children == "string" || typeof e.children == "number" || typeof e.children == "bigint" || typeof e.dangerouslySetInnerHTML == "object" && e.dangerouslySetInnerHTML !== null && e.dangerouslySetInnerHTML.__html != null;
  }
  var wf = null;
  function qh() {
    var t = window.event;
    return t && t.type === "popstate" ? t === wf ? !1 : (wf = t, !0) : (wf = null, !1);
  }
  var Jd = typeof setTimeout == "function" ? setTimeout : void 0, Nh = typeof clearTimeout == "function" ? clearTimeout : void 0, Kd = typeof Promise == "function" ? Promise : void 0, Oh = typeof queueMicrotask == "function" ? queueMicrotask : typeof Kd < "u" ? function(t) {
    return Kd.resolve(null).then(t).catch(Mh);
  } : Jd;
  function Mh(t) {
    setTimeout(function() {
      throw t;
    });
  }
  function $l(t) {
    return t === "head";
  }
  function kd(t, e) {
    var l = e, n = 0;
    do {
      var a = l.nextSibling;
      if (t.removeChild(l), a && a.nodeType === 8)
        if (l = a.data, l === "/$" || l === "/&") {
          if (n === 0) {
            t.removeChild(a), ba(e);
            return;
          }
          n--;
        } else if (l === "$" || l === "$?" || l === "$~" || l === "$!" || l === "&")
          n++;
        else if (l === "html")
          yu(t.ownerDocument.documentElement);
        else if (l === "head") {
          l = t.ownerDocument.head, yu(l);
          for (var u = l.firstChild; u; ) {
            var c = u.nextSibling, o = u.nodeName;
            u[Da] || o === "SCRIPT" || o === "STYLE" || o === "LINK" && u.rel.toLowerCase() === "stylesheet" || l.removeChild(u), u = c;
          }
        } else
          l === "body" && yu(t.ownerDocument.body);
      l = a;
    } while (l);
    ba(e);
  }
  function $d(t, e) {
    var l = t;
    t = 0;
    do {
      var n = l.nextSibling;
      if (l.nodeType === 1 ? e ? (l._stashedDisplay = l.style.display, l.style.display = "none") : (l.style.display = l._stashedDisplay || "", l.getAttribute("style") === "" && l.removeAttribute("style")) : l.nodeType === 3 && (e ? (l._stashedText = l.nodeValue, l.nodeValue = "") : l.nodeValue = l._stashedText || ""), n && n.nodeType === 8)
        if (l = n.data, l === "/$") {
          if (t === 0) break;
          t--;
        } else
          l !== "$" && l !== "$?" && l !== "$~" && l !== "$!" || t++;
      l = n;
    } while (l);
  }
  function Jf(t) {
    var e = t.firstChild;
    for (e && e.nodeType === 10 && (e = e.nextSibling); e; ) {
      var l = e;
      switch (e = e.nextSibling, l.nodeName) {
        case "HTML":
        case "HEAD":
        case "BODY":
          Jf(l), Wi(l);
          continue;
        case "SCRIPT":
        case "STYLE":
          continue;
        case "LINK":
          if (l.rel.toLowerCase() === "stylesheet") continue;
      }
      t.removeChild(l);
    }
  }
  function Dh(t, e, l, n) {
    for (; t.nodeType === 1; ) {
      var a = l;
      if (t.nodeName.toLowerCase() !== e.toLowerCase()) {
        if (!n && (t.nodeName !== "INPUT" || t.type !== "hidden"))
          break;
      } else if (n) {
        if (!t[Da])
          switch (e) {
            case "meta":
              if (!t.hasAttribute("itemprop")) break;
              return t;
            case "link":
              if (u = t.getAttribute("rel"), u === "stylesheet" && t.hasAttribute("data-precedence"))
                break;
              if (u !== a.rel || t.getAttribute("href") !== (a.href == null || a.href === "" ? null : a.href) || t.getAttribute("crossorigin") !== (a.crossOrigin == null ? null : a.crossOrigin) || t.getAttribute("title") !== (a.title == null ? null : a.title))
                break;
              return t;
            case "style":
              if (t.hasAttribute("data-precedence")) break;
              return t;
            case "script":
              if (u = t.getAttribute("src"), (u !== (a.src == null ? null : a.src) || t.getAttribute("type") !== (a.type == null ? null : a.type) || t.getAttribute("crossorigin") !== (a.crossOrigin == null ? null : a.crossOrigin)) && u && t.hasAttribute("async") && !t.hasAttribute("itemprop"))
                break;
              return t;
            default:
              return t;
          }
      } else if (e === "input" && t.type === "hidden") {
        var u = a.name == null ? null : "" + a.name;
        if (a.type === "hidden" && t.getAttribute("name") === u)
          return t;
      } else return t;
      if (t = Be(t.nextSibling), t === null) break;
    }
    return null;
  }
  function Uh(t, e, l) {
    if (e === "") return null;
    for (; t.nodeType !== 3; )
      if ((t.nodeType !== 1 || t.nodeName !== "INPUT" || t.type !== "hidden") && !l || (t = Be(t.nextSibling), t === null)) return null;
    return t;
  }
  function Wd(t, e) {
    for (; t.nodeType !== 8; )
      if ((t.nodeType !== 1 || t.nodeName !== "INPUT" || t.type !== "hidden") && !e || (t = Be(t.nextSibling), t === null)) return null;
    return t;
  }
  function Kf(t) {
    return t.data === "$?" || t.data === "$~";
  }
  function kf(t) {
    return t.data === "$!" || t.data === "$?" && t.ownerDocument.readyState !== "loading";
  }
  function jh(t, e) {
    var l = t.ownerDocument;
    if (t.data === "$~") t._reactRetry = e;
    else if (t.data !== "$?" || l.readyState !== "loading")
      e();
    else {
      var n = function() {
        e(), l.removeEventListener("DOMContentLoaded", n);
      };
      l.addEventListener("DOMContentLoaded", n), t._reactRetry = n;
    }
  }
  function Be(t) {
    for (; t != null; t = t.nextSibling) {
      var e = t.nodeType;
      if (e === 1 || e === 3) break;
      if (e === 8) {
        if (e = t.data, e === "$" || e === "$!" || e === "$?" || e === "$~" || e === "&" || e === "F!" || e === "F")
          break;
        if (e === "/$" || e === "/&") return null;
      }
    }
    return t;
  }
  var $f = null;
  function Fd(t) {
    t = t.nextSibling;
    for (var e = 0; t; ) {
      if (t.nodeType === 8) {
        var l = t.data;
        if (l === "/$" || l === "/&") {
          if (e === 0)
            return Be(t.nextSibling);
          e--;
        } else
          l !== "$" && l !== "$!" && l !== "$?" && l !== "$~" && l !== "&" || e++;
      }
      t = t.nextSibling;
    }
    return null;
  }
  function Id(t) {
    t = t.previousSibling;
    for (var e = 0; t; ) {
      if (t.nodeType === 8) {
        var l = t.data;
        if (l === "$" || l === "$!" || l === "$?" || l === "$~" || l === "&") {
          if (e === 0) return t;
          e--;
        } else l !== "/$" && l !== "/&" || e++;
      }
      t = t.previousSibling;
    }
    return null;
  }
  function Pd(t, e, l) {
    switch (e = Ni(l), t) {
      case "html":
        if (t = e.documentElement, !t) throw Error(r(452));
        return t;
      case "head":
        if (t = e.head, !t) throw Error(r(453));
        return t;
      case "body":
        if (t = e.body, !t) throw Error(r(454));
        return t;
      default:
        throw Error(r(451));
    }
  }
  function yu(t) {
    for (var e = t.attributes; e.length; )
      t.removeAttributeNode(e[0]);
    Wi(t);
  }
  var Ye = /* @__PURE__ */ new Map(), ty = /* @__PURE__ */ new Set();
  function Oi(t) {
    return typeof t.getRootNode == "function" ? t.getRootNode() : t.nodeType === 9 ? t : t.ownerDocument;
  }
  var xl = H.d;
  H.d = {
    f: Ch,
    r: Rh,
    D: Hh,
    C: Lh,
    L: Bh,
    m: Yh,
    X: Qh,
    S: Gh,
    M: Xh
  };
  function Ch() {
    var t = xl.f(), e = Si();
    return t || e;
  }
  function Rh(t) {
    var e = Bn(t);
    e !== null && e.tag === 5 && e.type === "form" ? go(e) : xl.r(t);
  }
  var va = typeof document > "u" ? null : document;
  function ey(t, e, l) {
    var n = va;
    if (n && typeof e == "string" && e) {
      var a = De(e);
      a = 'link[rel="' + t + '"][href="' + a + '"]', typeof l == "string" && (a += '[crossorigin="' + l + '"]'), ty.has(a) || (ty.add(a), t = { rel: t, crossOrigin: l, href: e }, n.querySelector(a) === null && (e = n.createElement("link"), ue(e, "link", t), It(e), n.head.appendChild(e)));
    }
  }
  function Hh(t) {
    xl.D(t), ey("dns-prefetch", t, null);
  }
  function Lh(t, e) {
    xl.C(t, e), ey("preconnect", t, e);
  }
  function Bh(t, e, l) {
    xl.L(t, e, l);
    var n = va;
    if (n && t && e) {
      var a = 'link[rel="preload"][as="' + De(e) + '"]';
      e === "image" && l && l.imageSrcSet ? (a += '[imagesrcset="' + De(
        l.imageSrcSet
      ) + '"]', typeof l.imageSizes == "string" && (a += '[imagesizes="' + De(
        l.imageSizes
      ) + '"]')) : a += '[href="' + De(t) + '"]';
      var u = a;
      switch (e) {
        case "style":
          u = ga(t);
          break;
        case "script":
          u = pa(t);
      }
      Ye.has(u) || (t = S(
        {
          rel: "preload",
          href: e === "image" && l && l.imageSrcSet ? void 0 : t,
          as: e
        },
        l
      ), Ye.set(u, t), n.querySelector(a) !== null || e === "style" && n.querySelector(mu(u)) || e === "script" && n.querySelector(hu(u)) || (e = n.createElement("link"), ue(e, "link", t), It(e), n.head.appendChild(e)));
    }
  }
  function Yh(t, e) {
    xl.m(t, e);
    var l = va;
    if (l && t) {
      var n = e && typeof e.as == "string" ? e.as : "script", a = 'link[rel="modulepreload"][as="' + De(n) + '"][href="' + De(t) + '"]', u = a;
      switch (n) {
        case "audioworklet":
        case "paintworklet":
        case "serviceworker":
        case "sharedworker":
        case "worker":
        case "script":
          u = pa(t);
      }
      if (!Ye.has(u) && (t = S({ rel: "modulepreload", href: t }, e), Ye.set(u, t), l.querySelector(a) === null)) {
        switch (n) {
          case "audioworklet":
          case "paintworklet":
          case "serviceworker":
          case "sharedworker":
          case "worker":
          case "script":
            if (l.querySelector(hu(u)))
              return;
        }
        n = l.createElement("link"), ue(n, "link", t), It(n), l.head.appendChild(n);
      }
    }
  }
  function Gh(t, e, l) {
    xl.S(t, e, l);
    var n = va;
    if (n && t) {
      var a = Yn(n).hoistableStyles, u = ga(t);
      e = e || "default";
      var c = a.get(u);
      if (!c) {
        var o = { loading: 0, preload: null };
        if (c = n.querySelector(
          mu(u)
        ))
          o.loading = 5;
        else {
          t = S(
            { rel: "stylesheet", href: t, "data-precedence": e },
            l
          ), (l = Ye.get(u)) && Wf(t, l);
          var y = c = n.createElement("link");
          It(y), ue(y, "link", t), y._p = new Promise(function(E, O) {
            y.onload = E, y.onerror = O;
          }), y.addEventListener("load", function() {
            o.loading |= 1;
          }), y.addEventListener("error", function() {
            o.loading |= 2;
          }), o.loading |= 4, Mi(c, e, n);
        }
        c = {
          type: "stylesheet",
          instance: c,
          count: 1,
          state: o
        }, a.set(u, c);
      }
    }
  }
  function Qh(t, e) {
    xl.X(t, e);
    var l = va;
    if (l && t) {
      var n = Yn(l).hoistableScripts, a = pa(t), u = n.get(a);
      u || (u = l.querySelector(hu(a)), u || (t = S({ src: t, async: !0 }, e), (e = Ye.get(a)) && Ff(t, e), u = l.createElement("script"), It(u), ue(u, "link", t), l.head.appendChild(u)), u = {
        type: "script",
        instance: u,
        count: 1,
        state: null
      }, n.set(a, u));
    }
  }
  function Xh(t, e) {
    xl.M(t, e);
    var l = va;
    if (l && t) {
      var n = Yn(l).hoistableScripts, a = pa(t), u = n.get(a);
      u || (u = l.querySelector(hu(a)), u || (t = S({ src: t, async: !0, type: "module" }, e), (e = Ye.get(a)) && Ff(t, e), u = l.createElement("script"), It(u), ue(u, "link", t), l.head.appendChild(u)), u = {
        type: "script",
        instance: u,
        count: 1,
        state: null
      }, n.set(a, u));
    }
  }
  function ly(t, e, l, n) {
    var a = (a = at.current) ? Oi(a) : null;
    if (!a) throw Error(r(446));
    switch (t) {
      case "meta":
      case "title":
        return null;
      case "style":
        return typeof l.precedence == "string" && typeof l.href == "string" ? (e = ga(l.href), l = Yn(
          a
        ).hoistableStyles, n = l.get(e), n || (n = {
          type: "style",
          instance: null,
          count: 0,
          state: null
        }, l.set(e, n)), n) : { type: "void", instance: null, count: 0, state: null };
      case "link":
        if (l.rel === "stylesheet" && typeof l.href == "string" && typeof l.precedence == "string") {
          t = ga(l.href);
          var u = Yn(
            a
          ).hoistableStyles, c = u.get(t);
          if (c || (a = a.ownerDocument || a, c = {
            type: "stylesheet",
            instance: null,
            count: 0,
            state: { loading: 0, preload: null }
          }, u.set(t, c), (u = a.querySelector(
            mu(t)
          )) && !u._p && (c.instance = u, c.state.loading = 5), Ye.has(t) || (l = {
            rel: "preload",
            as: "style",
            href: l.href,
            crossOrigin: l.crossOrigin,
            integrity: l.integrity,
            media: l.media,
            hrefLang: l.hrefLang,
            referrerPolicy: l.referrerPolicy
          }, Ye.set(t, l), u || Zh(
            a,
            t,
            l,
            c.state
          ))), e && n === null)
            throw Error(r(528, ""));
          return c;
        }
        if (e && n !== null)
          throw Error(r(529, ""));
        return null;
      case "script":
        return e = l.async, l = l.src, typeof l == "string" && e && typeof e != "function" && typeof e != "symbol" ? (e = pa(l), l = Yn(
          a
        ).hoistableScripts, n = l.get(e), n || (n = {
          type: "script",
          instance: null,
          count: 0,
          state: null
        }, l.set(e, n)), n) : { type: "void", instance: null, count: 0, state: null };
      default:
        throw Error(r(444, t));
    }
  }
  function ga(t) {
    return 'href="' + De(t) + '"';
  }
  function mu(t) {
    return 'link[rel="stylesheet"][' + t + "]";
  }
  function ny(t) {
    return S({}, t, {
      "data-precedence": t.precedence,
      precedence: null
    });
  }
  function Zh(t, e, l, n) {
    t.querySelector('link[rel="preload"][as="style"][' + e + "]") ? n.loading = 1 : (e = t.createElement("link"), n.preload = e, e.addEventListener("load", function() {
      return n.loading |= 1;
    }), e.addEventListener("error", function() {
      return n.loading |= 2;
    }), ue(e, "link", l), It(e), t.head.appendChild(e));
  }
  function pa(t) {
    return '[src="' + De(t) + '"]';
  }
  function hu(t) {
    return "script[async]" + t;
  }
  function ay(t, e, l) {
    if (e.count++, e.instance === null)
      switch (e.type) {
        case "style":
          var n = t.querySelector(
            'style[data-href~="' + De(l.href) + '"]'
          );
          if (n)
            return e.instance = n, It(n), n;
          var a = S({}, l, {
            "data-href": l.href,
            "data-precedence": l.precedence,
            href: null,
            precedence: null
          });
          return n = (t.ownerDocument || t).createElement(
            "style"
          ), It(n), ue(n, "style", a), Mi(n, l.precedence, t), e.instance = n;
        case "stylesheet":
          a = ga(l.href);
          var u = t.querySelector(
            mu(a)
          );
          if (u)
            return e.state.loading |= 4, e.instance = u, It(u), u;
          n = ny(l), (a = Ye.get(a)) && Wf(n, a), u = (t.ownerDocument || t).createElement("link"), It(u);
          var c = u;
          return c._p = new Promise(function(o, y) {
            c.onload = o, c.onerror = y;
          }), ue(u, "link", n), e.state.loading |= 4, Mi(u, l.precedence, t), e.instance = u;
        case "script":
          return u = pa(l.src), (a = t.querySelector(
            hu(u)
          )) ? (e.instance = a, It(a), a) : (n = l, (a = Ye.get(u)) && (n = S({}, l), Ff(n, a)), t = t.ownerDocument || t, a = t.createElement("script"), It(a), ue(a, "link", n), t.head.appendChild(a), e.instance = a);
        case "void":
          return null;
        default:
          throw Error(r(443, e.type));
      }
    else
      e.type === "stylesheet" && (e.state.loading & 4) === 0 && (n = e.instance, e.state.loading |= 4, Mi(n, l.precedence, t));
    return e.instance;
  }
  function Mi(t, e, l) {
    for (var n = l.querySelectorAll(
      'link[rel="stylesheet"][data-precedence],style[data-precedence]'
    ), a = n.length ? n[n.length - 1] : null, u = a, c = 0; c < n.length; c++) {
      var o = n[c];
      if (o.dataset.precedence === e) u = o;
      else if (u !== a) break;
    }
    u ? u.parentNode.insertBefore(t, u.nextSibling) : (e = l.nodeType === 9 ? l.head : l, e.insertBefore(t, e.firstChild));
  }
  function Wf(t, e) {
    t.crossOrigin == null && (t.crossOrigin = e.crossOrigin), t.referrerPolicy == null && (t.referrerPolicy = e.referrerPolicy), t.title == null && (t.title = e.title);
  }
  function Ff(t, e) {
    t.crossOrigin == null && (t.crossOrigin = e.crossOrigin), t.referrerPolicy == null && (t.referrerPolicy = e.referrerPolicy), t.integrity == null && (t.integrity = e.integrity);
  }
  var Di = null;
  function uy(t, e, l) {
    if (Di === null) {
      var n = /* @__PURE__ */ new Map(), a = Di = /* @__PURE__ */ new Map();
      a.set(l, n);
    } else
      a = Di, n = a.get(l), n || (n = /* @__PURE__ */ new Map(), a.set(l, n));
    if (n.has(t)) return n;
    for (n.set(t, null), l = l.getElementsByTagName(t), a = 0; a < l.length; a++) {
      var u = l[a];
      if (!(u[Da] || u[ee] || t === "link" && u.getAttribute("rel") === "stylesheet") && u.namespaceURI !== "http://www.w3.org/2000/svg") {
        var c = u.getAttribute(e) || "";
        c = t + c;
        var o = n.get(c);
        o ? o.push(u) : n.set(c, [u]);
      }
    }
    return n;
  }
  function iy(t, e, l) {
    t = t.ownerDocument || t, t.head.insertBefore(
      l,
      e === "title" ? t.querySelector("head > title") : null
    );
  }
  function Vh(t, e, l) {
    if (l === 1 || e.itemProp != null) return !1;
    switch (t) {
      case "meta":
      case "title":
        return !0;
      case "style":
        if (typeof e.precedence != "string" || typeof e.href != "string" || e.href === "")
          break;
        return !0;
      case "link":
        if (typeof e.rel != "string" || typeof e.href != "string" || e.href === "" || e.onLoad || e.onError)
          break;
        switch (e.rel) {
          case "stylesheet":
            return t = e.disabled, typeof e.precedence == "string" && t == null;
          default:
            return !0;
        }
      case "script":
        if (e.async && typeof e.async != "function" && typeof e.async != "symbol" && !e.onLoad && !e.onError && e.src && typeof e.src == "string")
          return !0;
    }
    return !1;
  }
  function cy(t) {
    return !(t.type === "stylesheet" && (t.state.loading & 3) === 0);
  }
  function wh(t, e, l, n) {
    if (l.type === "stylesheet" && (typeof n.media != "string" || matchMedia(n.media).matches !== !1) && (l.state.loading & 4) === 0) {
      if (l.instance === null) {
        var a = ga(n.href), u = e.querySelector(
          mu(a)
        );
        if (u) {
          e = u._p, e !== null && typeof e == "object" && typeof e.then == "function" && (t.count++, t = Ui.bind(t), e.then(t, t)), l.state.loading |= 4, l.instance = u, It(u);
          return;
        }
        u = e.ownerDocument || e, n = ny(n), (a = Ye.get(a)) && Wf(n, a), u = u.createElement("link"), It(u);
        var c = u;
        c._p = new Promise(function(o, y) {
          c.onload = o, c.onerror = y;
        }), ue(u, "link", n), l.instance = u;
      }
      t.stylesheets === null && (t.stylesheets = /* @__PURE__ */ new Map()), t.stylesheets.set(l, e), (e = l.state.preload) && (l.state.loading & 3) === 0 && (t.count++, l = Ui.bind(t), e.addEventListener("load", l), e.addEventListener("error", l));
    }
  }
  var If = 0;
  function Jh(t, e) {
    return t.stylesheets && t.count === 0 && Ci(t, t.stylesheets), 0 < t.count || 0 < t.imgCount ? function(l) {
      var n = setTimeout(function() {
        if (t.stylesheets && Ci(t, t.stylesheets), t.unsuspend) {
          var u = t.unsuspend;
          t.unsuspend = null, u();
        }
      }, 6e4 + e);
      0 < t.imgBytes && If === 0 && (If = 62500 * zh());
      var a = setTimeout(
        function() {
          if (t.waitingForImages = !1, t.count === 0 && (t.stylesheets && Ci(t, t.stylesheets), t.unsuspend)) {
            var u = t.unsuspend;
            t.unsuspend = null, u();
          }
        },
        (t.imgBytes > If ? 50 : 800) + e
      );
      return t.unsuspend = l, function() {
        t.unsuspend = null, clearTimeout(n), clearTimeout(a);
      };
    } : null;
  }
  function Ui() {
    if (this.count--, this.count === 0 && (this.imgCount === 0 || !this.waitingForImages)) {
      if (this.stylesheets) Ci(this, this.stylesheets);
      else if (this.unsuspend) {
        var t = this.unsuspend;
        this.unsuspend = null, t();
      }
    }
  }
  var ji = null;
  function Ci(t, e) {
    t.stylesheets = null, t.unsuspend !== null && (t.count++, ji = /* @__PURE__ */ new Map(), e.forEach(Kh, t), ji = null, Ui.call(t));
  }
  function Kh(t, e) {
    if (!(e.state.loading & 4)) {
      var l = ji.get(t);
      if (l) var n = l.get(null);
      else {
        l = /* @__PURE__ */ new Map(), ji.set(t, l);
        for (var a = t.querySelectorAll(
          "link[data-precedence],style[data-precedence]"
        ), u = 0; u < a.length; u++) {
          var c = a[u];
          (c.nodeName === "LINK" || c.getAttribute("media") !== "not all") && (l.set(c.dataset.precedence, c), n = c);
        }
        n && l.set(null, n);
      }
      a = e.instance, c = a.getAttribute("data-precedence"), u = l.get(c) || n, u === n && l.set(null, a), l.set(c, a), this.count++, n = Ui.bind(this), a.addEventListener("load", n), a.addEventListener("error", n), u ? u.parentNode.insertBefore(a, u.nextSibling) : (t = t.nodeType === 9 ? t.head : t, t.insertBefore(a, t.firstChild)), e.state.loading |= 4;
    }
  }
  var vu = {
    $$typeof: I,
    Provider: null,
    Consumer: null,
    _currentValue: $,
    _currentValue2: $,
    _threadCount: 0
  };
  function kh(t, e, l, n, a, u, c, o, y) {
    this.tag = 1, this.containerInfo = t, this.pingCache = this.current = this.pendingChildren = null, this.timeoutHandle = -1, this.callbackNode = this.next = this.pendingContext = this.context = this.cancelPendingCommit = null, this.callbackPriority = 0, this.expirationTimes = Ma(-1), this.entangledLanes = this.shellSuspendCounter = this.errorRecoveryDisabledLanes = this.expiredLanes = this.warmLanes = this.pingedLanes = this.suspendedLanes = this.pendingLanes = 0, this.entanglements = Ma(0), this.hiddenUpdates = Ma(null), this.identifierPrefix = n, this.onUncaughtError = a, this.onCaughtError = u, this.onRecoverableError = c, this.pooledCache = null, this.pooledCacheLanes = 0, this.formState = y, this.incompleteTransitions = /* @__PURE__ */ new Map();
  }
  function fy(t, e, l, n, a, u, c, o, y, E, O, U) {
    return t = new kh(
      t,
      e,
      l,
      c,
      y,
      E,
      O,
      U,
      o
    ), e = 1, u === !0 && (e |= 24), u = xe(3, null, null, e), t.current = u, u.stateNode = t, e = Mc(), e.refCount++, t.pooledCache = e, e.refCount++, u.memoizedState = {
      element: n,
      isDehydrated: l,
      cache: e
    }, Cc(u), t;
  }
  function sy(t) {
    return t ? (t = $n, t) : $n;
  }
  function ry(t, e, l, n, a, u) {
    a = sy(a), n.context === null ? n.context = a : n.pendingContext = a, n = Bl(e), n.payload = { element: l }, u = u === void 0 ? null : u, u !== null && (n.callback = u), l = Yl(t, n, e), l !== null && (be(l, t, e), ka(l, t, e));
  }
  function oy(t, e) {
    if (t = t.memoizedState, t !== null && t.dehydrated !== null) {
      var l = t.retryLane;
      t.retryLane = l !== 0 && l < e ? l : e;
    }
  }
  function Pf(t, e) {
    oy(t, e), (t = t.alternate) && oy(t, e);
  }
  function dy(t) {
    if (t.tag === 13 || t.tag === 31) {
      var e = hn(t, 67108864);
      e !== null && be(e, t, 67108864), Pf(t, 67108864);
    }
  }
  function yy(t) {
    if (t.tag === 13 || t.tag === 31) {
      var e = Ne();
      e = Rn(e);
      var l = hn(t, e);
      l !== null && be(l, t, e), Pf(t, e);
    }
  }
  var Ri = !0;
  function $h(t, e, l, n) {
    var a = N.T;
    N.T = null;
    var u = H.p;
    try {
      H.p = 2, ts(t, e, l, n);
    } finally {
      H.p = u, N.T = a;
    }
  }
  function Wh(t, e, l, n) {
    var a = N.T;
    N.T = null;
    var u = H.p;
    try {
      H.p = 8, ts(t, e, l, n);
    } finally {
      H.p = u, N.T = a;
    }
  }
  function ts(t, e, l, n) {
    if (Ri) {
      var a = es(n);
      if (a === null)
        Gf(
          t,
          e,
          n,
          Hi,
          l
        ), hy(t, n);
      else if (Ih(
        a,
        t,
        e,
        l,
        n
      ))
        n.stopPropagation();
      else if (hy(t, n), e & 4 && -1 < Fh.indexOf(t)) {
        for (; a !== null; ) {
          var u = Bn(a);
          if (u !== null)
            switch (u.tag) {
              case 3:
                if (u = u.stateNode, u.current.memoizedState.isDehydrated) {
                  var c = We(u.pendingLanes);
                  if (c !== 0) {
                    var o = u;
                    for (o.pendingLanes |= 2, o.entangledLanes |= 2; c; ) {
                      var y = 1 << 31 - te(c);
                      o.entanglements[1] |= y, c &= ~y;
                    }
                    el(u), (St & 6) === 0 && (pi = Nt() + 500, ru(0));
                  }
                }
                break;
              case 31:
              case 13:
                o = hn(u, 2), o !== null && be(o, u, 2), Si(), Pf(u, 2);
            }
          if (u = es(n), u === null && Gf(
            t,
            e,
            n,
            Hi,
            l
          ), u === a) break;
          a = u;
        }
        a !== null && n.stopPropagation();
      } else
        Gf(
          t,
          e,
          n,
          null,
          l
        );
    }
  }
  function es(t) {
    return t = lc(t), ls(t);
  }
  var Hi = null;
  function ls(t) {
    if (Hi = null, t = Ln(t), t !== null) {
      var e = m(t);
      if (e === null) t = null;
      else {
        var l = e.tag;
        if (l === 13) {
          if (t = b(e), t !== null) return t;
          t = null;
        } else if (l === 31) {
          if (t = T(e), t !== null) return t;
          t = null;
        } else if (l === 3) {
          if (e.stateNode.current.memoizedState.isDehydrated)
            return e.tag === 3 ? e.stateNode.containerInfo : null;
          t = null;
        } else e !== t && (t = null);
      }
    }
    return Hi = t, null;
  }
  function my(t) {
    switch (t) {
      case "beforetoggle":
      case "cancel":
      case "click":
      case "close":
      case "contextmenu":
      case "copy":
      case "cut":
      case "auxclick":
      case "dblclick":
      case "dragend":
      case "dragstart":
      case "drop":
      case "focusin":
      case "focusout":
      case "input":
      case "invalid":
      case "keydown":
      case "keypress":
      case "keyup":
      case "mousedown":
      case "mouseup":
      case "paste":
      case "pause":
      case "play":
      case "pointercancel":
      case "pointerdown":
      case "pointerup":
      case "ratechange":
      case "reset":
      case "resize":
      case "seeked":
      case "submit":
      case "toggle":
      case "touchcancel":
      case "touchend":
      case "touchstart":
      case "volumechange":
      case "change":
      case "selectionchange":
      case "textInput":
      case "compositionstart":
      case "compositionend":
      case "compositionupdate":
      case "beforeblur":
      case "afterblur":
      case "beforeinput":
      case "blur":
      case "fullscreenchange":
      case "focus":
      case "hashchange":
      case "popstate":
      case "select":
      case "selectstart":
        return 2;
      case "drag":
      case "dragenter":
      case "dragexit":
      case "dragleave":
      case "dragover":
      case "mousemove":
      case "mouseout":
      case "mouseover":
      case "pointermove":
      case "pointerout":
      case "pointerover":
      case "scroll":
      case "touchmove":
      case "wheel":
      case "mouseenter":
      case "mouseleave":
      case "pointerenter":
      case "pointerleave":
        return 8;
      case "message":
        switch (zu()) {
          case cn:
            return 2;
          case fn:
            return 8;
          case Un:
          case wi:
            return 32;
          case jn:
            return 268435456;
          default:
            return 32;
        }
      default:
        return 32;
    }
  }
  var ns = !1, Wl = null, Fl = null, Il = null, gu = /* @__PURE__ */ new Map(), pu = /* @__PURE__ */ new Map(), Pl = [], Fh = "mousedown mouseup touchcancel touchend touchstart auxclick dblclick pointercancel pointerdown pointerup dragend dragstart drop compositionend compositionstart keydown keypress keyup input textInput copy cut paste click change contextmenu reset".split(
    " "
  );
  function hy(t, e) {
    switch (t) {
      case "focusin":
      case "focusout":
        Wl = null;
        break;
      case "dragenter":
      case "dragleave":
        Fl = null;
        break;
      case "mouseover":
      case "mouseout":
        Il = null;
        break;
      case "pointerover":
      case "pointerout":
        gu.delete(e.pointerId);
        break;
      case "gotpointercapture":
      case "lostpointercapture":
        pu.delete(e.pointerId);
    }
  }
  function bu(t, e, l, n, a, u) {
    return t === null || t.nativeEvent !== u ? (t = {
      blockedOn: e,
      domEventName: l,
      eventSystemFlags: n,
      nativeEvent: u,
      targetContainers: [a]
    }, e !== null && (e = Bn(e), e !== null && dy(e)), t) : (t.eventSystemFlags |= n, e = t.targetContainers, a !== null && e.indexOf(a) === -1 && e.push(a), t);
  }
  function Ih(t, e, l, n, a) {
    switch (e) {
      case "focusin":
        return Wl = bu(
          Wl,
          t,
          e,
          l,
          n,
          a
        ), !0;
      case "dragenter":
        return Fl = bu(
          Fl,
          t,
          e,
          l,
          n,
          a
        ), !0;
      case "mouseover":
        return Il = bu(
          Il,
          t,
          e,
          l,
          n,
          a
        ), !0;
      case "pointerover":
        var u = a.pointerId;
        return gu.set(
          u,
          bu(
            gu.get(u) || null,
            t,
            e,
            l,
            n,
            a
          )
        ), !0;
      case "gotpointercapture":
        return u = a.pointerId, pu.set(
          u,
          bu(
            pu.get(u) || null,
            t,
            e,
            l,
            n,
            a
          )
        ), !0;
    }
    return !1;
  }
  function vy(t) {
    var e = Ln(t.target);
    if (e !== null) {
      var l = m(e);
      if (l !== null) {
        if (e = l.tag, e === 13) {
          if (e = b(l), e !== null) {
            t.blockedOn = e, Os(t.priority, function() {
              yy(l);
            });
            return;
          }
        } else if (e === 31) {
          if (e = T(l), e !== null) {
            t.blockedOn = e, Os(t.priority, function() {
              yy(l);
            });
            return;
          }
        } else if (e === 3 && l.stateNode.current.memoizedState.isDehydrated) {
          t.blockedOn = l.tag === 3 ? l.stateNode.containerInfo : null;
          return;
        }
      }
    }
    t.blockedOn = null;
  }
  function Li(t) {
    if (t.blockedOn !== null) return !1;
    for (var e = t.targetContainers; 0 < e.length; ) {
      var l = es(t.nativeEvent);
      if (l === null) {
        l = t.nativeEvent;
        var n = new l.constructor(
          l.type,
          l
        );
        ec = n, l.target.dispatchEvent(n), ec = null;
      } else
        return e = Bn(l), e !== null && dy(e), t.blockedOn = l, !1;
      e.shift();
    }
    return !0;
  }
  function gy(t, e, l) {
    Li(t) && l.delete(e);
  }
  function Ph() {
    ns = !1, Wl !== null && Li(Wl) && (Wl = null), Fl !== null && Li(Fl) && (Fl = null), Il !== null && Li(Il) && (Il = null), gu.forEach(gy), pu.forEach(gy);
  }
  function Bi(t, e) {
    t.blockedOn === e && (t.blockedOn = null, ns || (ns = !0, i.unstable_scheduleCallback(
      i.unstable_NormalPriority,
      Ph
    )));
  }
  var Yi = null;
  function py(t) {
    Yi !== t && (Yi = t, i.unstable_scheduleCallback(
      i.unstable_NormalPriority,
      function() {
        Yi === t && (Yi = null);
        for (var e = 0; e < t.length; e += 3) {
          var l = t[e], n = t[e + 1], a = t[e + 2];
          if (typeof n != "function") {
            if (ls(n || l) === null)
              continue;
            break;
          }
          var u = Bn(l);
          u !== null && (t.splice(e, 3), e -= 3, tf(
            u,
            {
              pending: !0,
              data: a,
              method: l.method,
              action: n
            },
            n,
            a
          ));
        }
      }
    ));
  }
  function ba(t) {
    function e(y) {
      return Bi(y, t);
    }
    Wl !== null && Bi(Wl, t), Fl !== null && Bi(Fl, t), Il !== null && Bi(Il, t), gu.forEach(e), pu.forEach(e);
    for (var l = 0; l < Pl.length; l++) {
      var n = Pl[l];
      n.blockedOn === t && (n.blockedOn = null);
    }
    for (; 0 < Pl.length && (l = Pl[0], l.blockedOn === null); )
      vy(l), l.blockedOn === null && Pl.shift();
    if (l = (t.ownerDocument || t).$$reactFormReplay, l != null)
      for (n = 0; n < l.length; n += 3) {
        var a = l[n], u = l[n + 1], c = a[ye] || null;
        if (typeof u == "function")
          c || py(l);
        else if (c) {
          var o = null;
          if (u && u.hasAttribute("formAction")) {
            if (a = u, c = u[ye] || null)
              o = c.formAction;
            else if (ls(a) !== null) continue;
          } else o = c.action;
          typeof o == "function" ? l[n + 1] = o : (l.splice(n, 3), n -= 3), py(l);
        }
      }
  }
  function by() {
    function t(u) {
      u.canIntercept && u.info === "react-transition" && u.intercept({
        handler: function() {
          return new Promise(function(c) {
            return a = c;
          });
        },
        focusReset: "manual",
        scroll: "manual"
      });
    }
    function e() {
      a !== null && (a(), a = null), n || setTimeout(l, 20);
    }
    function l() {
      if (!n && !navigation.transition) {
        var u = navigation.currentEntry;
        u && u.url != null && navigation.navigate(u.url, {
          state: u.getState(),
          info: "react-transition",
          history: "replace"
        });
      }
    }
    if (typeof navigation == "object") {
      var n = !1, a = null;
      return navigation.addEventListener("navigate", t), navigation.addEventListener("navigatesuccess", e), navigation.addEventListener("navigateerror", e), setTimeout(l, 100), function() {
        n = !0, navigation.removeEventListener("navigate", t), navigation.removeEventListener("navigatesuccess", e), navigation.removeEventListener("navigateerror", e), a !== null && (a(), a = null);
      };
    }
  }
  function as(t) {
    this._internalRoot = t;
  }
  Gi.prototype.render = as.prototype.render = function(t) {
    var e = this._internalRoot;
    if (e === null) throw Error(r(409));
    var l = e.current, n = Ne();
    ry(l, n, t, e, null, null);
  }, Gi.prototype.unmount = as.prototype.unmount = function() {
    var t = this._internalRoot;
    if (t !== null) {
      this._internalRoot = null;
      var e = t.containerInfo;
      ry(t.current, 2, null, t, null, null), Si(), e[Hn] = null;
    }
  };
  function Gi(t) {
    this._internalRoot = t;
  }
  Gi.prototype.unstable_scheduleHydration = function(t) {
    if (t) {
      var e = Ns();
      t = { blockedOn: null, target: t, priority: e };
      for (var l = 0; l < Pl.length && e !== 0 && e < Pl[l].priority; l++) ;
      Pl.splice(l, 0, t), l === 0 && vy(t);
    }
  };
  var Sy = f.version;
  if (Sy !== "19.2.5")
    throw Error(
      r(
        527,
        Sy,
        "19.2.5"
      )
    );
  H.findDOMNode = function(t) {
    var e = t._reactInternals;
    if (e === void 0)
      throw typeof t.render == "function" ? Error(r(188)) : (t = Object.keys(t).join(","), Error(r(268, t)));
    return t = g(e), t = t !== null ? j(t) : null, t = t === null ? null : t.stateNode, t;
  };
  var tv = {
    bundleType: 0,
    version: "19.2.5",
    rendererPackageName: "react-dom",
    currentDispatcherRef: N,
    reconcilerVersion: "19.2.5"
  };
  if (typeof __REACT_DEVTOOLS_GLOBAL_HOOK__ < "u") {
    var Qi = __REACT_DEVTOOLS_GLOBAL_HOOK__;
    if (!Qi.isDisabled && Qi.supportsFiber)
      try {
        Ol = Qi.inject(
          tv
        ), $t = Qi;
      } catch {
      }
  }
  return Su.createRoot = function(t, e) {
    if (!d(t)) throw Error(r(299));
    var l = !1, n = "", a = qo, u = No, c = Oo;
    return e != null && (e.unstable_strictMode === !0 && (l = !0), e.identifierPrefix !== void 0 && (n = e.identifierPrefix), e.onUncaughtError !== void 0 && (a = e.onUncaughtError), e.onCaughtError !== void 0 && (u = e.onCaughtError), e.onRecoverableError !== void 0 && (c = e.onRecoverableError)), e = fy(
      t,
      1,
      !1,
      null,
      null,
      l,
      n,
      null,
      a,
      u,
      c,
      by
    ), t[Hn] = e.current, Yf(t), new as(e);
  }, Su.hydrateRoot = function(t, e, l) {
    if (!d(t)) throw Error(r(299));
    var n = !1, a = "", u = qo, c = No, o = Oo, y = null;
    return l != null && (l.unstable_strictMode === !0 && (n = !0), l.identifierPrefix !== void 0 && (a = l.identifierPrefix), l.onUncaughtError !== void 0 && (u = l.onUncaughtError), l.onCaughtError !== void 0 && (c = l.onCaughtError), l.onRecoverableError !== void 0 && (o = l.onRecoverableError), l.formState !== void 0 && (y = l.formState)), e = fy(
      t,
      1,
      !0,
      e,
      l ?? null,
      n,
      a,
      y,
      u,
      c,
      o,
      by
    ), e.context = sy(null), l = e.current, n = Ne(), n = Rn(n), a = Bl(n), a.callback = null, Yl(l, a, n), l = n, e.current.lanes = l, R(e, l), el(e), t[Hn] = e.current, Yf(t), new Gi(e);
  }, Su.version = "19.2.5", Su;
}
var Oy;
function rv() {
  if (Oy) return cs.exports;
  Oy = 1;
  function i() {
    if (!(typeof __REACT_DEVTOOLS_GLOBAL_HOOK__ > "u" || typeof __REACT_DEVTOOLS_GLOBAL_HOOK__.checkDCE != "function"))
      try {
        __REACT_DEVTOOLS_GLOBAL_HOOK__.checkDCE(i);
      } catch (f) {
        console.error(f);
      }
  }
  return i(), cs.exports = sv(), cs.exports;
}
var ov = rv(), os = { exports: {} }, _u = {};
/**
 * @license React
 * react-jsx-runtime.production.js
 *
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 */
var My;
function dv() {
  if (My) return _u;
  My = 1;
  var i = Symbol.for("react.transitional.element"), f = Symbol.for("react.fragment");
  function s(r, d, m) {
    var b = null;
    if (m !== void 0 && (b = "" + m), d.key !== void 0 && (b = "" + d.key), "key" in d) {
      m = {};
      for (var T in d)
        T !== "key" && (m[T] = d[T]);
    } else m = d;
    return d = m.ref, {
      $$typeof: i,
      type: r,
      key: b,
      ref: d !== void 0 ? d : null,
      props: m
    };
  }
  return _u.Fragment = f, _u.jsx = s, _u.jsxs = s, _u;
}
var Dy;
function yv() {
  return Dy || (Dy = 1, os.exports = dv()), os.exports;
}
var A = yv();
function mv(i) {
  return typeof i.questionId == "string";
}
function hv(i) {
  const f = i;
  return Array.isArray(f.all) || Array.isArray(f.any);
}
function vv(i) {
  return typeof i.expression == "string";
}
class ll extends Error {
  constructor(s, r) {
    super(`Expression syntax error at column ${r}: ${s}`);
    Al(this, "position");
    this.position = r, this.name = "ExpressionSyntaxError";
  }
}
function Xi(i) {
  return i >= "0" && i <= "9";
}
function By(i) {
  return i >= "a" && i <= "z" || i >= "A" && i <= "Z";
}
function gv(i) {
  return By(i) || Xi(i);
}
function pv(i) {
  return i === " " || i === "	" || i === `
` || i === "\r" || i === "\f" || i === "\v";
}
function bv(i) {
  const f = [];
  let s = 0;
  const r = () => s >= i.length, d = (S = 0) => i.charAt(s + S), m = (S) => {
    if (s + S.length > i.length)
      return !1;
    for (let C = 0; C < S.length; C++)
      if (i.charAt(s + C) !== S.charAt(C))
        return !1;
    return s += S.length, !0;
  }, b = () => {
    for (; !r() && pv(d()); )
      s++;
  }, T = (S) => {
    for (; !r() && Xi(d()); )
      s++;
    if (!r() && d() === ".")
      for (s++; !r() && Xi(d()); )
        s++;
    const C = i.substring(S, s), G = parseFloat(C);
    return { kind: "Number", text: C, literal: G, position: S };
  }, q = (S, C) => {
    s++;
    let G = "";
    for (; !r() && d() !== C; ) {
      const K = d();
      if (K === "\\" && s + 1 < i.length) {
        const P = d(1), W = {
          n: `
`,
          t: "	",
          r: "\r",
          "\\": "\\",
          "'": "'",
          '"': '"'
        }[P];
        if (W === void 0)
          throw new ll(`unknown escape '\\${P}'.`, s);
        G += W, s += 2;
      } else
        G += K, s++;
    }
    if (r())
      throw new ll("unterminated string literal.", S);
    return s++, { kind: "String", text: G, literal: G, position: S };
  }, g = (S) => {
    for (; !r(); ) {
      const G = d();
      if (G === "_" || G === "-" || gv(G))
        s++;
      else
        break;
    }
    const C = i.substring(S, s);
    return C === "true" ? { kind: "True", text: C, literal: !0, position: S } : C === "false" ? { kind: "False", text: C, literal: !1, position: S } : C === "null" ? { kind: "Null", text: C, literal: null, position: S } : { kind: "Identifier", text: C, literal: null, position: S };
  }, j = () => {
    const S = s, C = d();
    if (Xi(C))
      return T(S);
    if (C === "'" || C === '"')
      return q(S, C);
    if (C === "_" || By(C))
      return g(S);
    switch (C) {
      case "(":
        return s++, { kind: "LParen", text: "(", literal: null, position: S };
      case ")":
        return s++, { kind: "RParen", text: ")", literal: null, position: S };
      case "[":
        return s++, { kind: "LBracket", text: "[", literal: null, position: S };
      case "]":
        return s++, { kind: "RBracket", text: "]", literal: null, position: S };
      case ",":
        return s++, { kind: "Comma", text: ",", literal: null, position: S };
      case ".":
        return s++, { kind: "Dot", text: ".", literal: null, position: S };
      case "=":
        if (m("==="))
          return { kind: "StrictEq", text: "===", literal: null, position: S };
        if (m("=="))
          return { kind: "Eq", text: "==", literal: null, position: S };
        throw new ll("bare '=' is not a valid operator (use '==' or '===').", S);
      case "!":
        return m("!==") ? { kind: "StrictNotEq", text: "!==", literal: null, position: S } : m("!=") ? { kind: "NotEq", text: "!=", literal: null, position: S } : (s++, { kind: "Not", text: "!", literal: null, position: S });
      case "<":
        return m("<=") ? { kind: "LtEq", text: "<=", literal: null, position: S } : (s++, { kind: "Lt", text: "<", literal: null, position: S });
      case ">":
        return m(">=") ? { kind: "GtEq", text: ">=", literal: null, position: S } : (s++, { kind: "Gt", text: ">", literal: null, position: S });
      case "&":
        if (m("&&"))
          return { kind: "And", text: "&&", literal: null, position: S };
        throw new ll("expected '&&'.", S);
      case "|":
        if (m("||"))
          return { kind: "Or", text: "||", literal: null, position: S };
        throw new ll("expected '||'.", S);
    }
    throw new ll(`unexpected character '${C}'.`, S);
  };
  for (; ; ) {
    if (b(), r())
      return f.push({ kind: "EndOfInput", text: "", literal: null, position: s }), f;
    f.push(j());
  }
}
function Sv(i) {
  let f = 0;
  const s = () => {
    const B = i[f];
    if (!B)
      throw new ll("unexpected end of tokens.", 0);
    return B;
  }, r = () => {
    const B = s();
    return B.kind !== "EndOfInput" && f++, B;
  }, d = (B) => s().kind !== B ? !1 : (r(), !0), m = (B) => {
    const W = s();
    if (W.kind !== B)
      throw new ll(`expected ${B}, got '${W.text}'.`, W.position);
    return r(), W;
  }, b = () => {
    let B = T();
    for (; d("Or"); )
      B = { kind: "BinaryOp", op: "||", left: B, right: T() };
    return B;
  }, T = () => {
    let B = q();
    for (; d("And"); )
      B = { kind: "BinaryOp", op: "&&", left: B, right: q() };
    return B;
  }, q = () => {
    let B = g();
    for (; ; ) {
      const W = s().kind;
      let it = null;
      if (W === "Eq" || W === "StrictEq" ? it = "==" : (W === "NotEq" || W === "StrictNotEq") && (it = "!="), it === null)
        break;
      r(), B = { kind: "BinaryOp", op: it, left: B, right: g() };
    }
    return B;
  }, g = () => {
    let B = j();
    for (; ; ) {
      const W = s().kind;
      let it = null;
      if (W === "Lt" ? it = "<" : W === "Gt" ? it = ">" : W === "LtEq" ? it = "<=" : W === "GtEq" && (it = ">="), it === null)
        break;
      r(), B = { kind: "BinaryOp", op: it, left: B, right: j() };
    }
    return B;
  }, j = () => d("Not") ? { kind: "UnaryOp", op: "!", operand: j() } : K(), S = () => {
    m("LBracket");
    const B = [];
    if (s().kind !== "RBracket")
      for (B.push(b()); d("Comma"); )
        B.push(b());
    return m("RBracket"), { kind: "Array", items: B };
  }, C = (B) => {
    let W;
    if (d("Dot"))
      W = m("Identifier").text;
    else if (d("LBracket")) {
      const it = m("String");
      m("RBracket"), W = it.literal;
    } else
      throw new ll("'answers' must be followed by .key or ['key'].", B);
    return { kind: "AnswersAccess", key: W };
  }, G = () => {
    const B = r();
    if (B.text === "answers")
      return C(B.position);
    m("LParen");
    const W = [];
    if (s().kind !== "RParen")
      for (W.push(b()); d("Comma"); )
        W.push(b());
    return m("RParen"), { kind: "Call", name: B.text, args: W };
  }, K = () => {
    const B = s();
    switch (B.kind) {
      case "Number":
      case "String":
      case "True":
      case "False":
      case "Null":
        return r(), { kind: "Literal", value: B.literal };
      case "LParen": {
        r();
        const W = b();
        return m("RParen"), W;
      }
      case "LBracket":
        return S();
      case "Identifier":
        return G();
      default:
        throw new ll(`unexpected token '${B.text}'.`, B.position);
    }
  }, P = b();
  return m("EndOfInput"), P;
}
function zl(i) {
  return i === void 0 || i === null ? null : typeof i == "boolean" || typeof i == "number" || typeof i == "string" ? i : Array.isArray(i) ? i.map(zl) : null;
}
function Mn(i, f) {
  const s = zl(i), r = zl(f);
  if (s === null || r === null)
    return s === null && r === null;
  if (typeof s == "number" && typeof r == "number" || typeof s == "string" && typeof r == "string" || typeof s == "boolean" && typeof r == "boolean")
    return s === r;
  if (Array.isArray(s) && Array.isArray(r)) {
    if (s.length !== r.length)
      return !1;
    for (let d = 0; d < s.length; d++)
      if (!Mn(s[d], r[d]))
        return !1;
    return !0;
  }
  return !1;
}
function an(i, f) {
  const s = zl(i), r = zl(f);
  if (typeof s == "number" && typeof r == "number" || typeof s == "string" && typeof r == "string")
    return s < r ? -1 : s > r ? 1 : 0;
  throw new Error("Comparison operators require two numbers or two strings.");
}
function _a(i) {
  const f = zl(i);
  return f === null ? !1 : typeof f == "boolean" ? f : typeof f == "number" ? f !== 0 : typeof f == "string" || Array.isArray(f) ? f.length > 0 : !0;
}
function Ge(i, f) {
  switch (i.kind) {
    case "Literal":
      return i.value;
    case "AnswersAccess":
      return Tv(i.key, f);
    case "UnaryOp":
      return _v(i, f);
    case "BinaryOp":
      return Ev(i, f);
    case "Call":
      return xv(i, f);
    case "Array":
      return i.items.map((s) => Ge(s, f));
  }
}
function _v(i, f) {
  const s = Ge(i.operand, f);
  if (i.op === "!")
    return !_a(s);
  throw new Error(`Unknown unary operator '${i.op}'.`);
}
function Ev(i, f) {
  if (i.op === "&&") {
    const d = Ge(i.left, f);
    return _a(d) ? _a(Ge(i.right, f)) : !1;
  }
  if (i.op === "||") {
    const d = Ge(i.left, f);
    return _a(d) ? !0 : _a(Ge(i.right, f));
  }
  const s = Ge(i.left, f), r = Ge(i.right, f);
  switch (i.op) {
    case "==":
      return Mn(s, r);
    case "!=":
      return !Mn(s, r);
    case "<":
      return an(s, r) < 0;
    case ">":
      return an(s, r) > 0;
    case "<=":
      return an(s, r) <= 0;
    case ">=":
      return an(s, r) >= 0;
    default:
      throw new Error(`Unknown binary operator '${i.op}'.`);
  }
}
function xv(i, f) {
  switch (i.name) {
    case "has":
    case "isSet":
      return Uy(i, f);
    case "isNotSet":
      return !Uy(i, f);
    case "in":
      return Av(i, f);
    default:
      throw new Error(`Unknown function '${i.name}'.`);
  }
}
function Uy(i, f) {
  if (i.args.length !== 1)
    throw new Error(`${i.name}() takes one argument.`);
  const s = i.args[0];
  if (!s)
    return !1;
  const r = Ge(s, f);
  return typeof r != "string" ? !1 : r in f && f[r] !== null && f[r] !== void 0;
}
function Av(i, f) {
  if (i.args.length !== 2)
    throw new Error("in() takes two arguments: in(value, [array]).");
  const s = i.args[0], r = i.args[1];
  if (!s || !r)
    return !1;
  const d = Ge(s, f), m = Ge(r, f);
  return Array.isArray(m) ? m.some((b) => Mn(d, b)) : !1;
}
function Tv(i, f) {
  return i in f ? zl(f[i]) : null;
}
function zv(i) {
  const f = bv(i);
  return Sv(f);
}
function qv(i, f) {
  try {
    const s = typeof i == "string" ? zv(i) : i;
    return _a(Ge(s, f));
  } catch {
    return !1;
  }
}
function Nv(i, f) {
  var s;
  if (!i.logic)
    return null;
  for (const r of i.logic)
    if (bs(r.if, f))
      return ((s = r.then) == null ? void 0 : s.goto) ?? null;
  return null;
}
function bs(i, f) {
  try {
    return mv(i) ? Mv(i, f) : hv(i) ? Ov(i, f) : vv(i) ? qv(i.expression, f) : !1;
  } catch {
    return !1;
  }
}
function Ov(i, f) {
  return i.all && i.all.length > 0 ? i.all.every((s) => bs(s, f)) : i.any && i.any.length > 0 ? i.any.some((s) => bs(s, f)) : !1;
}
function Mv(i, f) {
  const s = i.questionId in f && f[i.questionId] !== null && f[i.questionId] !== void 0;
  if (i.op === "isSet")
    return s;
  if (i.op === "isNotSet")
    return !s;
  if (i.value === void 0)
    return !1;
  const r = s ? zl(f[i.questionId]) : null, d = zl(i.value);
  return Dv(i.op, r, d);
}
function Dv(i, f, s) {
  switch (i) {
    case "==":
      return Mn(f, s);
    case "!=":
      return !Mn(f, s);
    case ">":
      return an(f, s) > 0;
    case ">=":
      return an(f, s) >= 0;
    case "<":
      return an(f, s) < 0;
    case "<=":
      return an(f, s) <= 0;
    case "in":
      return jy(s, f);
    case "notIn":
      return !jy(s, f);
    default:
      return !1;
  }
}
function jy(i, f) {
  return Array.isArray(i) ? i.some((s) => Mn(f, s)) : !1;
}
function Ea(i, f, s) {
  const r = new Set(i.screens.map((T) => T.id)), d = i.screens.find((T) => T.id === f);
  if (d && (!d.questions || d.questions.length === 0) && !d.nextScreen)
    return { kind: "end" };
  const m = Nv(i, s);
  if (m && m !== f && r.has(m))
    return { kind: "screen", screenId: m };
  if (d != null && d.nextScreen && d.nextScreen !== f && r.has(d.nextScreen))
    return { kind: "screen", screenId: d.nextScreen };
  const b = i.screens.findIndex((T) => T.id === f);
  if (b >= 0 && b + 1 < i.screens.length) {
    const T = i.screens[b + 1];
    if (T)
      return { kind: "screen", screenId: T.id };
  }
  return { kind: "end" };
}
function Uv(i, f, s, r) {
  const d = new Set(f.screens.map((m) => m.id));
  return i.nextScreen && d.has(i.nextScreen) ? { kind: "screen", screenId: i.nextScreen } : Ea(f, s, r);
}
function jv(i, f, s) {
  var b;
  const r = [], d = /* @__PURE__ */ new Set();
  let m = (b = i.screens[0]) == null ? void 0 : b.id;
  for (; m !== void 0 && !d.has(m); ) {
    if (m === s)
      return r;
    d.add(m), r.push(m);
    const T = Cv(i, m, f);
    if (T.kind === "end")
      return null;
    m = T.screenId;
  }
  return null;
}
function Cv(i, f, s) {
  var m;
  const r = new Set(i.screens.map((b) => b.id)), d = i.screens.find((b) => b.id === f);
  for (const b of (d == null ? void 0 : d.questions) ?? []) {
    if (b.type !== "navigationList")
      continue;
    const T = s[b.id];
    if (typeof T != "string")
      continue;
    const g = (b.options ?? []).find((S) => S.id === T);
    if (g != null && g.nextScreen && r.has(g.nextScreen))
      return { kind: "screen", screenId: g.nextScreen };
    const j = (m = b.optionsSource) == null ? void 0 : m.nextScreen;
    if (!g && j && r.has(j))
      return { kind: "screen", screenId: j };
  }
  return Ea(i, f, s);
}
const ht = (i, f, s, r) => ({ questionId: i, code: f, message: s, ...r ? { params: r } : {} }), Se = (i) => typeof i == "number" && Number.isFinite(i);
function ds(i) {
  if (!/^\d{4}-\d{2}-\d{2}$/.test(i))
    return null;
  const [f, s, r] = i.split("-").map((m) => Number.parseInt(m, 10)), d = new Date(Date.UTC(f, s - 1, r));
  return d.getUTCFullYear() !== f || d.getUTCMonth() !== s - 1 || d.getUTCDate() !== r ? null : d.getTime();
}
function ys(i) {
  const f = Date.parse(i);
  return Number.isNaN(f) ? null : f;
}
function Yy(i, f) {
  const s = i.id, r = [];
  switch (i.type) {
    case "text": {
      if (typeof f != "string") {
        r.push(ht(s, "type", "Text answer must be a JSON string."));
        break;
      }
      const d = i.minLength, m = i.maxLength, b = i.pattern;
      if (Se(d) && f.length < d && r.push(ht(s, "minLength", `Answer length ${f.length} is less than minLength ${d}.`, { n: d, actual: f.length })), Se(m) && f.length > m && r.push(ht(s, "maxLength", `Answer length ${f.length} exceeds maxLength ${m}.`, { n: m, actual: f.length })), typeof b == "string" && b.length > 0)
        try {
          new RegExp(b).test(f) || r.push(ht(s, "pattern", "Answer does not match the required pattern."));
        } catch {
        }
      break;
    }
    case "paragraph": {
      if (typeof f != "string") {
        r.push(ht(s, "type", "Paragraph answer must be a JSON string."));
        break;
      }
      const d = i.minLength, m = i.maxLength;
      Se(d) && f.length < d && r.push(ht(s, "minLength", `Answer length ${f.length} is less than minLength ${d}.`, { n: d, actual: f.length })), Se(m) && f.length > m && r.push(ht(s, "maxLength", `Answer length ${f.length} exceeds maxLength ${m}.`, { n: m, actual: f.length }));
      break;
    }
    case "number": {
      if (!Se(f)) {
        r.push(ht(s, "type", "Number answer must be a JSON number."));
        break;
      }
      const d = i.min, m = i.max;
      Se(d) && f < d && r.push(ht(s, "min", `Answer ${f} is less than min ${d}.`, { n: d })), Se(m) && f > m && r.push(ht(s, "max", `Answer ${f} exceeds max ${m}.`, { n: m }));
      break;
    }
    case "rating": {
      if (!Se(f)) {
        r.push(ht(s, "type", "Rating answer must be a JSON number."));
        break;
      }
      const d = Se(i.max) ? i.max : 0;
      (f < 0 || f > d) && r.push(ht(s, "range", `Rating ${f} is outside 0..${d}.`, { min: 0, max: d })), i.allowHalf !== !0 && f !== Math.floor(f) && r.push(ht(s, "halfNotAllowed", "Rating does not allow half values."));
      break;
    }
    case "nps": {
      if (!Se(f) || !Number.isInteger(f)) {
        r.push(ht(s, "type", "NPS answer must be a JSON number."));
        break;
      }
      const d = Se(i.min) ? i.min : 0, m = Se(i.max) ? i.max : 10;
      (f < d || f > m) && r.push(ht(s, "range", `NPS answer ${f} is outside ${d}..${m}.`, { min: d, max: m }));
      break;
    }
    case "singleChoice":
    case "dropdown":
    case "navigationList": {
      if (typeof f != "string") {
        r.push(ht(s, "type", "Choice answer must be a JSON string (option id)."));
        break;
      }
      if (i.optionsSource != null)
        break;
      (Array.isArray(i.options) ? i.options : []).some((m) => m.id === f) || r.push(ht(s, "invalidOption", `'${f}' is not a valid option id for this question.`, { option: f }));
      break;
    }
    case "multiChoice": {
      if (!Array.isArray(f)) {
        r.push(ht(s, "type", "MultiChoice answer must be a JSON array of option ids."));
        break;
      }
      const d = Array.isArray(i.options) ? i.options : [], m = new Set(d.map((j) => j.id)), b = [];
      let T = !1;
      for (const j of f) {
        if (typeof j != "string") {
          r.push(ht(s, "type", "Each MultiChoice array entry must be a string option id.")), T = !0;
          break;
        }
        b.push(j);
      }
      if (T)
        break;
      if (i.optionsSource == null)
        for (const j of b)
          m.has(j) || r.push(ht(s, "invalidOption", `'${j}' is not a valid option id for this question.`, { option: j }));
      const q = i.minSelected, g = i.maxSelected;
      Se(q) && b.length < q && r.push(ht(s, "minSelected", `At least ${q} option(s) must be selected.`, { n: q })), Se(g) && b.length > g && r.push(ht(s, "maxSelected", `At most ${g} option(s) may be selected.`, { n: g }));
      break;
    }
    case "date": {
      if (typeof f != "string") {
        r.push(ht(s, "type", "Date answer must be a JSON string in yyyy-MM-dd format."));
        break;
      }
      const d = ds(f);
      if (d === null) {
        r.push(ht(s, "invalidDate", `Date '${f}' is not yyyy-MM-dd.`));
        break;
      }
      const m = i.minDate, b = i.maxDate;
      if (typeof m == "string") {
        const T = ds(m);
        T !== null && d < T && r.push(ht(s, "minDate", `Date ${f} is before minDate ${m}.`, { min: m }));
      }
      if (typeof b == "string") {
        const T = ds(b);
        T !== null && d > T && r.push(ht(s, "maxDate", `Date ${f} is after maxDate ${b}.`, { max: b }));
      }
      break;
    }
    case "dateTime": {
      if (typeof f != "string") {
        r.push(ht(s, "type", "DateTime answer must be a JSON string in ISO 8601 format."));
        break;
      }
      const d = ys(f);
      if (d === null) {
        r.push(ht(s, "invalidDateTime", `DateTime '${f}' is not valid ISO 8601.`));
        break;
      }
      const m = i.minDateTime, b = i.maxDateTime;
      if (typeof m == "string" && m.length > 0) {
        const T = ys(m);
        T !== null && d < T && r.push(ht(s, "minDateTime", `DateTime is before minDateTime ${m}.`, { min: m }));
      }
      if (typeof b == "string" && b.length > 0) {
        const T = ys(b);
        T !== null && d > T && r.push(ht(s, "maxDateTime", `DateTime is after maxDateTime ${b}.`, { max: b }));
      }
      break;
    }
    case "file": {
      (typeof f != "string" || f.length === 0) && r.push(ht(s, "empty", "Answer must be a non-empty file reference string."));
      break;
    }
    case "signature": {
      (typeof f != "string" || f.length === 0) && r.push(ht(s, "empty", "Answer must be a non-empty signature data url string."));
      break;
    }
    case "yesNo": {
      typeof f != "boolean" && r.push(ht(s, "type", "Yes/No answer must be a JSON boolean."));
      break;
    }
  }
  return r;
}
function Rv(i, f) {
  const s = [];
  for (const r of i ?? []) {
    const d = r, m = d.id;
    if (typeof m != "string")
      continue;
    const b = f[m];
    b != null && s.push(...Yy(d, b));
  }
  return s;
}
const Hv = /\{\{\s*([A-Za-z_][A-Za-z0-9_.\-]*)\s*(?:\|([^}]*))?\}\}/g, Ss = "answers.", _s = ".label";
function Lv(i) {
  return typeof i == "string" && i.includes("{{");
}
function Bv(i) {
  return typeof i == "string" && i.startsWith(Ss) && i.length > Ss.length;
}
function Yv(i) {
  if (!Bv(i))
    return null;
  let f = i.slice(Ss.length);
  return f.endsWith(_s) && (f = f.slice(0, -_s.length)), f.length === 0 ? null : f;
}
function Gv(i, f) {
  switch (f) {
    case "url":
    case "formBody":
      return encodeURIComponent(i);
    case "jsonBody":
      return JSON.stringify(i).slice(1, -1);
    case "headerValue":
      return i.replace(/[\r\n]/g, "");
    default:
      return i;
  }
}
function Qv(i) {
  const f = (i ?? "").split(";")[0].trim().toLowerCase();
  return f === "application/json" || f.endsWith("+json") ? "jsonBody" : f === "application/x-www-form-urlencoded" ? "formBody" : "rawBody";
}
function Zi(i, f, s, r = "copy") {
  return Lv(i) ? i.replace(Hv, (d, m, b) => {
    const T = b == null ? void 0 : b.trim(), q = f.value(m, s) ?? (T || null) ?? f.fallback(m, s);
    return q === null ? r === "copy" ? d : "" : Gv(q, r);
  }) : i;
}
function Ts(i, f, s) {
  if (i == null)
    return null;
  if (typeof i == "string")
    return i || null;
  const r = i[f];
  if (r)
    return r;
  if (s && i[s])
    return i[s];
  for (const d of Object.keys(i))
    if (i[d])
      return i[d];
  return null;
}
function Xv(i, f) {
  for (const s of i.screens)
    for (const r of s.questions ?? [])
      if (r && r.id === f)
        return r;
}
function zs(i) {
  if (i == null)
    return null;
  if (typeof i == "string")
    return i.length === 0 ? null : i;
  if (typeof i == "number" || typeof i == "boolean")
    return String(i);
  if (Array.isArray(i)) {
    const f = i.map(zs).filter((s) => s !== null);
    return f.length === 0 ? null : f.join(", ");
  }
  if (typeof i == "object") {
    const f = i.name;
    return typeof f == "string" && f.length > 0 ? f : JSON.stringify(i);
  }
  return String(i);
}
function Cy(i, f, s, r) {
  const d = i == null ? void 0 : i.find((m) => m.id === f);
  return d ? Ts(d.label, s, r) : null;
}
function Zv(i, f, s, r) {
  var T, q;
  const d = zs(f);
  if (d === null || !i)
    return d;
  const m = r.schema.defaultLocale, b = (g) => {
    var j;
    return Cy(i.options, g, s, m) ?? Cy(i.id ? (j = r.lookupOptions) == null ? void 0 : j.call(r, i.id) : void 0, g, s, m) ?? g;
  };
  switch (i.type) {
    case "singleChoice":
    case "dropdown":
    case "navigationList":
      return b(String(f));
    case "multiChoice":
      return Array.isArray(f) ? f.map((g) => b(String(g))).join(", ") : b(String(f));
    case "yesNo": {
      const g = f === !0 || f === "true";
      return Ts(i[g ? "yesLabel" : "noLabel"], s, m) ?? (g ? ((T = r.yesNoLabels) == null ? void 0 : T.yes) ?? "Yes" : ((q = r.yesNoLabels) == null ? void 0 : q.no) ?? "No");
    }
    case "date":
    case "dateTime": {
      const g = new Date(d);
      if (Number.isNaN(g.getTime()))
        return d;
      try {
        return i.type === "date" ? new Intl.DateTimeFormat(s, { dateStyle: "medium" }).format(g) : new Intl.DateTimeFormat(s, { dateStyle: "medium", timeStyle: "short" }).format(g);
      } catch {
        return d;
      }
    }
    default:
      return d;
  }
}
function Vv(i) {
  const { schema: f, answers: s } = i;
  return {
    value(r, d) {
      const m = Yv(r);
      if (m === null)
        return null;
      const b = s[m];
      return r.endsWith(_s) ? Zv(Xv(f, m), b, d, i) : zs(b);
    },
    fallback(r, d) {
      var b;
      const m = (b = f.variables) == null ? void 0 : b.find((T) => {
        var q;
        return ((q = T.name) == null ? void 0 : q.trim()) === r;
      });
      return Ts(m == null ? void 0 : m.fallback, d, f.defaultLocale);
    }
  };
}
const wv = [
  "title",
  "description",
  "help",
  "placeholder",
  "lowLabel",
  "highLabel",
  "unit",
  "yesLabel",
  "noLabel",
  "label"
];
function Jv(i) {
  return i === null || typeof i != "object" || Array.isArray(i) ? !1 : Object.values(i).every((f) => typeof f == "string");
}
function Kv(i, f) {
  let s = !1;
  const r = {};
  for (const [d, m] of Object.entries(i)) {
    const b = Zi(m, f, d, "copy");
    b !== m && (s = !0), r[d] = b;
  }
  return s ? r : i;
}
function Es(i, f) {
  let s = null;
  for (const r of wv) {
    const d = i[r];
    if (!Jv(d))
      continue;
    const m = Kv(d, f);
    m !== d && (s ?? (s = { ...i }), s[r] = m);
  }
  return s ?? i;
}
function kv(i, f) {
  let s = Es(i, f);
  const r = i.options;
  if (Array.isArray(r)) {
    let d = !1;
    const m = r.map((b) => {
      if (b === null || typeof b != "object")
        return b;
      const T = Es(b, f);
      return T !== b && (d = !0), T;
    });
    d && (s = { ...s, options: m });
  }
  return s;
}
function $v(i, f) {
  let s = Es(i, f);
  const r = i.questions;
  if (Array.isArray(r)) {
    let d = !1;
    const m = r.map((b) => {
      if (b === null || typeof b != "object")
        return b;
      const T = kv(b, f);
      return T !== b && (d = !0), T;
    });
    d && (s = { ...s, questions: m });
  }
  return s;
}
function Wv(i, f, s) {
  let r = null;
  const d = (q, g) => {
    r ?? (r = { ...i }), r[q] = g;
  }, m = Zi(i.url, f, s, "url");
  if (m !== i.url && d("url", m), i.body != null) {
    const q = Zi(i.body, f, s, Qv(Gy(i)));
    q !== i.body && d("body", q);
  }
  const b = Ry(i.queryParams, f, s, "queryValue");
  b !== i.queryParams && d("queryParams", b);
  const T = Ry(i.headers, f, s, "headerValue");
  return T !== i.headers && d("headers", T), r ?? i;
}
function Gy(i) {
  var s;
  const f = (s = i.contentType) == null ? void 0 : s.trim();
  return f || (i.body != null ? "application/json" : void 0);
}
function Ry(i, f, s, r) {
  if (!i)
    return i;
  let d = null;
  for (const [m, b] of Object.entries(i)) {
    const T = Zi(b, f, s, r);
    T !== b && (d ?? (d = { ...i }), d[m] = T);
  }
  return d ?? i;
}
function ms(i, f) {
  let s = i;
  for (const r of f.split(".")) {
    if (s === null || typeof s != "object")
      return;
    s = s[r];
  }
  return s;
}
function Qy(i) {
  const f = new URL(i.url);
  for (const [s, r] of Object.entries(i.queryParams ?? {}))
    f.searchParams.set(s, r);
  return f.toString();
}
function Fv(i, f) {
  const s = f.itemsPath ? ms(i, f.itemsPath) : i;
  if (!Array.isArray(s))
    throw new Error(`optionsSource response is not an array${f.itemsPath ? ` at '${f.itemsPath}'` : ""}.`);
  const r = f.valuePath || "ID", d = f.labelPath || "Name", m = [];
  for (const b of s) {
    const T = ms(b, r);
    if (T == null || T === "")
      continue;
    const q = ms(b, d);
    m.push({
      id: String(T),
      label: q == null || q === "" ? String(T) : String(q)
    });
  }
  return m;
}
function Vi(i) {
  var f;
  return ((f = i.method) == null ? void 0 : f.trim().toUpperCase()) === "POST" ? "POST" : "GET";
}
function Xy(i, f) {
  const s = {};
  f && (s["Accept-Language"] = f);
  const r = Vi(i) === "POST" && i.body != null ? Gy(i) : void 0;
  r && (s["Content-Type"] = r);
  for (const [d, m] of Object.entries(i.headers ?? {})) {
    for (const b of Object.keys(s))
      b.toLowerCase() === d.toLowerCase() && delete s[b];
    s[d] = m;
  }
  return s;
}
function Iv(i, f) {
  const s = Xy(i, f), r = Object.keys(s).sort().map((m) => `${m}=${s[m]}`).join(`
`), d = Vi(i) === "POST" && i.body != null ? i.body : "";
  return `${Vi(i)} ${Qy(i)}
${r}
${d}`;
}
async function Pv(i, f) {
  const s = (f == null ? void 0 : f.fetchImpl) ?? fetch, r = Vi(i), d = await s(Qy(i), {
    method: r,
    headers: Xy(i, f == null ? void 0 : f.locale),
    ...r === "POST" && i.body != null ? { body: i.body } : {},
    ...f != null && f.signal ? { signal: f.signal } : {}
  });
  if (!d.ok)
    throw new Error(`optionsSource fetch failed: HTTP ${d.status}.`);
  return Fv(await d.json(), i);
}
class Sa extends Error {
  constructor(s) {
    super(s.message);
    Al(this, "status");
    Al(this, "code");
    Al(this, "serverMessage");
    Al(this, "validationErrors");
    Al(this, "raw");
    this.name = "SurveyClientError", this.status = s.status, this.code = s.code, this.serverMessage = s.serverMessage, this.validationErrors = s.validationErrors, this.raw = s.raw;
  }
}
class Hy {
  constructor(f) {
    Al(this, "baseUrl");
    Al(this, "fetchFn");
    this.baseUrl = f.baseUrl.replace(/\/+$/, "");
    const s = f.fetch ?? globalThis.fetch;
    if (!s)
      throw new Error("SurveyClient: no fetch available. Provide options.fetch or run in an environment with a global fetch.");
    this.fetchFn = s.bind(globalThis);
  }
  async fetchSchema(f) {
    const s = await this.send("GET", `/SurveyInstances/${encodeURIComponent(f)}/schema`);
    return this.readJson(s);
  }
  async getStatus(f) {
    const s = await this.send("GET", `/SurveyInstances/${encodeURIComponent(f)}/status`), r = await this.readJson(s);
    return {
      status: String(r.Status ?? r.status ?? "Pending"),
      schemaVersion: Number(r.SchemaVersion ?? r.schemaVersion ?? 0),
      triggeredAt: r.TriggeredAt ?? r.triggeredAt
    };
  }
  async submitResponse(f, s) {
    await this.send("POST", `/SurveyInstances/${encodeURIComponent(f)}/responses`, s);
  }
  async send(f, s, r) {
    let d;
    try {
      d = await this.fetchFn(`${this.baseUrl}${s}`, {
        method: f,
        headers: r === void 0 ? void 0 : { "Content-Type": "application/json" },
        body: r === void 0 ? void 0 : JSON.stringify(r)
      });
    } catch (m) {
      throw new Sa({
        status: 0,
        code: "network",
        message: `Network error calling ${f} ${s}: ${m.message ?? m}`
      });
    }
    if (!d.ok)
      throw await this.toError(d, f, s);
    return d;
  }
  async readJson(f) {
    const s = await f.text();
    if (!s)
      throw new Sa({
        status: f.status,
        code: "parse",
        message: `Empty body from ${f.url}`
      });
    try {
      return JSON.parse(s);
    } catch (r) {
      throw new Sa({
        status: f.status,
        code: "parse",
        message: `Could not parse JSON from ${f.url}: ${r.message}`,
        raw: s
      });
    }
  }
  async toError(f, s, r) {
    const d = f.status === 404 ? "notFound" : f.status === 410 ? "gone" : f.status === 409 ? "conflict" : f.status === 400 ? "badRequest" : (f.status >= 500, "server"), m = await f.text();
    if (!m)
      return new Sa({
        status: f.status,
        code: d,
        message: `${s} ${r} → ${f.status}`
      });
    let b;
    try {
      b = JSON.parse(m);
    } catch {
      return new Sa({
        status: f.status,
        code: d,
        message: `${s} ${r} → ${f.status}: ${m.slice(0, 200)}`,
        raw: m
      });
    }
    const T = b.Message ?? b.message, q = b.Errors ?? b.errors, g = Array.isArray(q) ? q.flatMap((j) => {
      const S = j.QuestionId ?? j.questionId, C = j.Message ?? j.message;
      return S && C ? [{ questionId: S, message: C }] : [];
    }) : void 0;
    return new Sa({
      status: f.status,
      code: d,
      message: `${s} ${r} → ${f.status}${T ? ": " + T : ""}`,
      serverMessage: T,
      validationErrors: g && g.length > 0 ? g : void 0,
      raw: b
    });
  }
}
function Ly(i) {
  const f = i.trim().replace(/^#/, "");
  if (!/^[0-9a-fA-F]{3}$|^[0-9a-fA-F]{6}$|^[0-9a-fA-F]{8}$/.test(f)) return null;
  const s = f.length === 3 ? f.split("").map((r) => r + r).join("") : f.slice(0, 6);
  return [
    parseInt(s.slice(0, 2), 16),
    parseInt(s.slice(2, 4), 16),
    parseInt(s.slice(4, 6), 16)
  ];
}
function hs([i, f, s]) {
  const r = (d) => Math.max(0, Math.min(255, Math.round(d))).toString(16).padStart(2, "0");
  return `#${r(i)}${r(f)}${r(s)}`;
}
function t0(i, f) {
  return [i[0] * f, i[1] * f, i[2] * f];
}
function e0([i, f, s]) {
  const r = (d) => {
    const m = d / 255;
    return m <= 0.03928 ? m / 12.92 : Math.pow((m + 0.055) / 1.055, 2.4);
  };
  return 0.2126 * r(i) + 0.7152 * r(f) + 0.0722 * r(s);
}
function l0(i) {
  const f = {}, s = i != null && i.primaryColor ? Ly(i.primaryColor) : null;
  s && (f["--survey-primary"] = hs(s), f["--survey-primary-hover"] = hs(t0(s, 0.82)), f["--survey-primary-contrast"] = e0(s) > 0.45 ? "#111111" : "#ffffff");
  const r = i != null && i.secondaryColor ? Ly(i.secondaryColor) : null;
  return r && (f["--survey-accent"] = hs(r)), f;
}
const Zy = V.createContext(null), n0 = Zy.Provider;
function re() {
  const i = V.useContext(Zy);
  if (!i)
    throw new Error(
      "useSurveyContext must be used inside <SurveyRenderer>. Question components rely on survey state from the enclosing provider."
    );
  return i;
}
function et(i, f, s) {
  if (i == null) return "";
  if (typeof i == "string") return i;
  if (i[f]) return i[f];
  if (s && i[s]) return i[s];
  const r = Object.keys(i);
  return r.length > 0 ? i[r[0]] : "";
}
const Vy = {
  direction: "ltr",
  strings: {
    next: "Next",
    back: "Back",
    submit: "Submit",
    submitting: "Submitting…",
    loading: "Loading survey…",
    thankYou: "Thank you.",
    selectPlaceholder: "Select…",
    clearSignature: "Clear",
    noScreens: "No screens in this survey.",
    unsupportedQuestion: "Unsupported question type:",
    couldNotSubmit: "Could not submit:",
    requiredError: "This question is required.",
    minLengthError: "Must be at least {n} characters.",
    maxLengthError: "Must be at most {n} characters.",
    patternError: "Does not match the required format.",
    minError: "Must be at least {n}.",
    maxError: "Must be at most {n}.",
    rangeError: "Must be between {min} and {max}.",
    minSelectedError: "Select at least {n} option(s).",
    maxSelectedError: "Select at most {n} option(s).",
    invalidAnswerError: "Please check this answer.",
    loadingOptions: "Loading options…",
    optionsLoadError: "Could not load the options.",
    retry: "Retry",
    yes: "Yes",
    no: "No",
    fileRecordedName: "Recorded file details: {name}",
    language: "Language"
  }
}, a0 = {
  direction: "rtl",
  strings: {
    next: "التالي",
    back: "رجوع",
    submit: "إرسال",
    submitting: "جاري الإرسال…",
    loading: "جاري تحميل الاستبيان…",
    thankYou: "شكراً لك.",
    selectPlaceholder: "اختر…",
    clearSignature: "مسح",
    noScreens: "لا توجد شاشات في هذا الاستبيان.",
    unsupportedQuestion: "نوع سؤال غير مدعوم:",
    couldNotSubmit: "تعذر الإرسال:",
    requiredError: "هذا السؤال مطلوب.",
    minLengthError: "يجب ألا يقل عن {n} حرفاً.",
    maxLengthError: "يجب ألا يزيد عن {n} حرفاً.",
    patternError: "لا يطابق التنسيق المطلوب.",
    minError: "يجب ألا يقل عن {n}.",
    maxError: "يجب ألا يزيد عن {n}.",
    rangeError: "يجب أن يكون بين {min} و {max}.",
    minSelectedError: "اختر {n} خيارات على الأقل.",
    maxSelectedError: "اختر {n} خيارات كحد أقصى.",
    invalidAnswerError: "يرجى التحقق من هذه الإجابة.",
    loadingOptions: "جاري تحميل الخيارات…",
    optionsLoadError: "تعذر تحميل الخيارات.",
    retry: "إعادة المحاولة",
    yes: "نعم",
    no: "لا",
    fileRecordedName: "تم تسجيل تفاصيل الملف: {name}",
    language: "اللغة"
  }
}, u0 = {
  direction: "rtl",
  strings: {
    next: "دواتر",
    back: "گەڕانەوە",
    submit: "ناردن",
    submitting: "دەنێردرێت…",
    loading: "ڕاپرسی باردەکرێت…",
    thankYou: "سوپاس.",
    selectPlaceholder: "هەڵبژێرە…",
    clearSignature: "سڕینەوە",
    noScreens: "هیچ پەڕەیەک لەم ڕاپرسییەدا نییە.",
    unsupportedQuestion: "جۆری پرسیاری پشتگیری نەکراو:",
    couldNotSubmit: "ناردن سەرکەوتوو نەبوو:",
    requiredError: "ئەم پرسیارە پێویستە.",
    minLengthError: "دەبێت لانیکەم {n} پیت بێت.",
    maxLengthError: "دەبێت زۆرترین {n} پیت بێت.",
    patternError: "لەگەڵ فۆرماتی داواکراودا یەک ناگرێتەوە.",
    minError: "دەبێت لانیکەم {n} بێت.",
    maxError: "دەبێت زۆرترین {n} بێت.",
    rangeError: "دەبێت لە نێوان {min} و {max} بێت.",
    minSelectedError: "لانیکەم {n} هەڵبژاردە هەڵبژێرە.",
    maxSelectedError: "زۆرترین {n} هەڵبژاردە هەڵبژێرە.",
    invalidAnswerError: "تکایە ئەم وەڵامە بپشکنە.",
    loadingOptions: "هەڵبژاردەکان باردەکرێن…",
    optionsLoadError: "نەتوانرا هەڵبژاردەکان باربکرێن.",
    retry: "دووبارە هەوڵبدەوە",
    yes: "بەڵێ",
    no: "نەخێر",
    fileRecordedName: "زانیاری فایل تۆمارکرا: {name}",
    language: "زمان"
  }
}, i0 = {
  direction: "ltr",
  strings: {
    next: "Далее",
    back: "Назад",
    submit: "Отправить",
    submitting: "Отправка…",
    loading: "Загрузка опроса…",
    thankYou: "Спасибо.",
    selectPlaceholder: "Выберите…",
    clearSignature: "Очистить",
    noScreens: "В этом опросе нет экранов.",
    unsupportedQuestion: "Неподдерживаемый тип вопроса:",
    couldNotSubmit: "Не удалось отправить:",
    requiredError: "Этот вопрос обязателен.",
    minLengthError: "Не менее {n} символов.",
    maxLengthError: "Не более {n} символов.",
    patternError: "Не соответствует требуемому формату.",
    minError: "Не менее {n}.",
    maxError: "Не более {n}.",
    rangeError: "Должно быть от {min} до {max}.",
    minSelectedError: "Выберите не менее {n} вариантов.",
    maxSelectedError: "Выберите не более {n} вариантов.",
    invalidAnswerError: "Пожалуйста, проверьте этот ответ.",
    loadingOptions: "Загрузка вариантов…",
    optionsLoadError: "Не удалось загрузить варианты.",
    retry: "Повторить",
    yes: "Да",
    no: "Нет",
    fileRecordedName: "Записаны сведения о файле: {name}",
    language: "Язык"
  }
}, c0 = { en: Vy, ar: a0, ku: u0, ru: i0 }, f0 = {
  en: "English",
  ar: "العربية",
  ku: "کوردی",
  ru: "Русский",
  tr: "Türkçe",
  fa: "فارسی"
};
function s0(i) {
  const f = f0[i];
  if (f) return f;
  try {
    const s = new Intl.DisplayNames([i], { type: "language" }).of(i);
    if (s && s !== i) return s;
  } catch {
  }
  return i;
}
function en(i, f) {
  return f ? i.replace(
    /\{(\w+)\}/g,
    (s, r) => r in f ? String(f[r]) : s
  ) : i;
}
function r0(i, f, s) {
  const r = { ...c0, ...s ?? {} };
  return r[i] ?? (f ? r[f] : void 0) ?? r.en ?? Vy;
}
const o0 = "adp-surveys", d0 = 1;
function y0(i = {}) {
  const f = typeof window < "u", s = f && window.parent !== window, r = i.enabled ?? s, d = i.target ?? (f ? window.parent : null), m = i.targetOrigin ?? "*";
  if (!r || !d)
    return {
      loaded: () => {
      },
      screenChanged: () => {
      },
      completed: () => {
      },
      error: () => {
      },
      resize: () => {
      }
    };
  const b = (T, q) => {
    const g = {
      source: o0,
      version: d0,
      type: T,
      payload: q
    };
    try {
      d.postMessage(g, m);
    } catch {
    }
  };
  return {
    loaded: () => b("survey:loaded", {}),
    screenChanged: (T) => b("survey:screen-changed", { screenId: T }),
    completed: (T) => b("survey:completed", T),
    error: (T) => b("survey:error", { message: T }),
    resize: (T) => b("survey:resize", { height: T })
  };
}
function qs(i) {
  return `adp-surveys:resume:${i}`;
}
function m0(i, f) {
  try {
    const s = i.getItem(qs(f));
    if (!s) return null;
    const r = JSON.parse(s);
    return !r || typeof r != "object" || !r.answers ? null : r;
  } catch {
    return null;
  }
}
function h0(i, f, s) {
  try {
    const r = { ...s, savedAt: Date.now() };
    i.setItem(qs(f), JSON.stringify(r));
  } catch {
  }
}
function v0(i, f) {
  try {
    i.removeItem(qs(f));
  } catch {
  }
}
const vs = /* @__PURE__ */ new Map();
function g0({
  question: i,
  Component: f
}) {
  const { locale: s, schema: r, ui: d, answerContext: m, registerSourcedOptions: b } = re(), T = Wv(i.optionsSource, m, s), q = i.id, g = `${s}|${Iv(T, s)}`, [j, S] = V.useState(() => {
    const it = vs.get(g);
    return it ? { status: "ready", options: it } : { status: "loading" };
  }), [C, G] = V.useState(0);
  V.useEffect(() => {
    const it = vs.get(g);
    if (it) {
      S({ status: "ready", options: it });
      return;
    }
    let I = !1;
    return S({ status: "loading" }), Pv(T, { locale: s }).then((ct) => {
      vs.set(g, ct), I || S({ status: "ready", options: ct });
    }).catch((ct) => {
      I || S({ status: "error", message: ct.message ?? String(ct) });
    }), () => {
      I = !0;
    };
  }, [g, C]), V.useEffect(() => {
    j.status === "ready" && q && b(q, j.options);
  }, [j, q, b]);
  const K = i.title, P = K ? /* @__PURE__ */ A.jsx("span", { className: "survey-question__label", children: et(K, s, r.defaultLocale) }) : null;
  if (j.status === "loading")
    return /* @__PURE__ */ A.jsxs("div", { className: "survey-question survey-question--options-loading", role: "status", children: [
      P,
      /* @__PURE__ */ A.jsx("p", { className: "survey-question__options-status", children: d.loadingOptions })
    ] });
  if (j.status === "error")
    return /* @__PURE__ */ A.jsxs("div", { className: "survey-question survey-question--options-error", children: [
      P,
      /* @__PURE__ */ A.jsx("p", { className: "survey-question__options-status", role: "alert", children: d.optionsLoadError }),
      /* @__PURE__ */ A.jsx(
        "button",
        {
          type: "button",
          className: "survey-button survey-button--retry",
          onClick: () => G((it) => it + 1),
          children: d.retry
        }
      )
    ] });
  const B = i.type === "navigationList", W = {
    ...i,
    options: j.options.map((it) => ({
      id: it.id,
      label: { [s]: it.label },
      ...B && T.nextScreen ? { nextScreen: T.nextScreen } : {}
    }))
  };
  return /* @__PURE__ */ A.jsx(f, { question: W });
}
function p0({
  question: i,
  registry: f
}) {
  const { ui: s } = re(), r = i.type, d = r ? f[r] : void 0;
  if (!d)
    return /* @__PURE__ */ A.jsx("div", { className: "survey-question survey-question--unknown", children: /* @__PURE__ */ A.jsxs("em", { children: [
      s.unsupportedQuestion,
      " ",
      String(r ?? "missing")
    ] }) });
  const m = Array.isArray(i.options) && i.options.length > 0;
  return i.optionsSource != null && !m ? /* @__PURE__ */ A.jsx(g0, { question: i, Component: d }) : /* @__PURE__ */ A.jsx(d, { question: i });
}
function b0({ question: i }) {
  const { locale: f, schema: s, answers: r, setAnswer: d } = re(), m = i.id, b = i.title, T = i.help, q = !!i.required, g = r[m] ?? "";
  return /* @__PURE__ */ A.jsxs("div", { className: "survey-question survey-question--text", children: [
    /* @__PURE__ */ A.jsxs("label", { className: "survey-question__label", htmlFor: `q-${m}`, children: [
      et(b, f, s.defaultLocale),
      q && /* @__PURE__ */ A.jsx("span", { "aria-label": "required", className: "survey-question__required", children: " *" })
    ] }),
    T && /* @__PURE__ */ A.jsx("p", { className: "survey-question__help", children: et(T, f, s.defaultLocale) }),
    /* @__PURE__ */ A.jsx(
      "input",
      {
        id: `q-${m}`,
        className: "survey-question__input",
        type: "text",
        value: g,
        required: q,
        onChange: (j) => d(m, j.target.value)
      }
    )
  ] });
}
function S0({ question: i }) {
  const { locale: f, schema: s, answers: r, setAnswer: d } = re(), m = i.id, b = i.title, T = i.help, q = !!i.required, g = Number(i.min ?? 0), j = Number(i.max ?? 10), S = i.lowLabel, C = i.highLabel, G = r[m], K = [];
  for (let P = g; P <= j; P++) K.push(P);
  return /* @__PURE__ */ A.jsxs("fieldset", { className: "survey-question survey-question--nps", children: [
    /* @__PURE__ */ A.jsxs("legend", { className: "survey-question__label", children: [
      et(b, f, s.defaultLocale),
      q && /* @__PURE__ */ A.jsx("span", { "aria-label": "required", className: "survey-question__required", children: " *" })
    ] }),
    T && /* @__PURE__ */ A.jsx("p", { className: "survey-question__help", children: et(T, f, s.defaultLocale) }),
    /* @__PURE__ */ A.jsx("div", { className: "survey-question__nps-scale", role: "radiogroup", children: K.map((P) => {
      const B = G === P;
      return /* @__PURE__ */ A.jsx(
        "button",
        {
          type: "button",
          role: "radio",
          "aria-checked": B,
          className: "survey-question__nps-step" + (B ? " survey-question__nps-step--selected" : ""),
          onClick: () => d(m, P),
          children: P
        },
        P
      );
    }) }),
    (S || C) && /* @__PURE__ */ A.jsxs("div", { className: "survey-question__nps-labels", children: [
      /* @__PURE__ */ A.jsx("span", { children: S ? et(S, f, s.defaultLocale) : "" }),
      /* @__PURE__ */ A.jsx("span", { children: C ? et(C, f, s.defaultLocale) : "" })
    ] })
  ] });
}
function _0({ question: i }) {
  const { locale: f, schema: s, answers: r } = re(), d = i.id, m = r[d], b = i.title, T = i.help, q = i.options ?? [], g = (j, S) => {
    const C = {
      questionId: d,
      option: {
        id: S.id,
        nextScreen: S.nextScreen
      }
    };
    j.currentTarget.dispatchEvent(
      new CustomEvent("survey:navigationListSelect", {
        detail: C,
        bubbles: !0
      })
    );
  };
  return /* @__PURE__ */ A.jsxs("div", { className: "survey-question survey-question--navlist", children: [
    /* @__PURE__ */ A.jsx("div", { className: "survey-question__label", children: et(b, f, s.defaultLocale) }),
    T && /* @__PURE__ */ A.jsx("p", { className: "survey-question__help", children: et(T, f, s.defaultLocale) }),
    /* @__PURE__ */ A.jsx("ul", { className: "survey-navlist", role: "radiogroup", "aria-description": "Selecting an option navigates to the next screen.", children: q.map((j) => {
      const S = j.id, C = j.label, G = m === S;
      return /* @__PURE__ */ A.jsx("li", { className: "survey-navlist__row", children: /* @__PURE__ */ A.jsxs(
        "button",
        {
          type: "button",
          className: G ? "survey-navlist__button survey-navlist__button--selected" : "survey-navlist__button",
          "aria-pressed": G,
          onClick: (K) => g(K, j),
          children: [
            /* @__PURE__ */ A.jsx("span", { className: "survey-navlist__label", children: et(C, f, s.defaultLocale) }),
            /* @__PURE__ */ A.jsx("span", { "aria-hidden": "true", className: "survey-navlist__chevron", children: "›" })
          ]
        }
      ) }, S);
    }) })
  ] });
}
function E0({ question: i }) {
  const { locale: f, schema: s, answers: r, setAnswer: d } = re(), m = i.id, b = i.title, T = i.help, q = i.placeholder, g = !!i.required, j = i.minLength, S = i.maxLength, C = r[m] ?? "";
  return /* @__PURE__ */ A.jsxs("div", { className: "survey-question survey-question--paragraph", children: [
    /* @__PURE__ */ A.jsxs("label", { className: "survey-question__label", htmlFor: `q-${m}`, children: [
      et(b, f, s.defaultLocale),
      g && /* @__PURE__ */ A.jsx("span", { "aria-label": "required", className: "survey-question__required", children: " *" })
    ] }),
    T && /* @__PURE__ */ A.jsx("p", { className: "survey-question__help", children: et(T, f, s.defaultLocale) }),
    /* @__PURE__ */ A.jsx(
      "textarea",
      {
        id: `q-${m}`,
        className: "survey-question__textarea",
        value: C,
        required: g,
        rows: 5,
        minLength: j,
        maxLength: S,
        placeholder: q ? et(q, f, s.defaultLocale) : void 0,
        onChange: (G) => d(m, G.target.value)
      }
    )
  ] });
}
function x0({ question: i }) {
  const { locale: f, schema: s, answers: r, setAnswer: d } = re(), m = i.id, b = i.title, T = i.help, q = !!i.required, g = i.min, j = i.max, S = i.step, C = i.unit, G = r[m], K = G == null ? "" : String(G);
  return /* @__PURE__ */ A.jsxs("div", { className: "survey-question survey-question--number", children: [
    /* @__PURE__ */ A.jsxs("label", { className: "survey-question__label", htmlFor: `q-${m}`, children: [
      et(b, f, s.defaultLocale),
      q && /* @__PURE__ */ A.jsx("span", { "aria-label": "required", className: "survey-question__required", children: " *" })
    ] }),
    T && /* @__PURE__ */ A.jsx("p", { className: "survey-question__help", children: et(T, f, s.defaultLocale) }),
    /* @__PURE__ */ A.jsxs("div", { className: "survey-question__number-wrap", children: [
      /* @__PURE__ */ A.jsx(
        "input",
        {
          id: `q-${m}`,
          className: "survey-question__input",
          type: "number",
          value: K,
          required: q,
          min: g,
          max: j,
          step: S,
          onChange: (P) => {
            const B = P.target.value;
            d(m, B === "" ? null : Number(B));
          }
        }
      ),
      C && /* @__PURE__ */ A.jsx("span", { className: "survey-question__unit", children: et(C, f, s.defaultLocale) })
    ] })
  ] });
}
function A0({ question: i }) {
  const { locale: f, schema: s, answers: r, setAnswer: d } = re(), m = i.id, b = i.title, T = i.help, q = !!i.required, g = Number(i.max ?? 5), j = r[m], S = [];
  for (let C = 1; C <= g; C++) S.push(C);
  return /* @__PURE__ */ A.jsxs("fieldset", { className: "survey-question survey-question--rating", children: [
    /* @__PURE__ */ A.jsxs("legend", { className: "survey-question__label", children: [
      et(b, f, s.defaultLocale),
      q && /* @__PURE__ */ A.jsx("span", { "aria-label": "required", className: "survey-question__required", children: " *" })
    ] }),
    T && /* @__PURE__ */ A.jsx("p", { className: "survey-question__help", children: et(T, f, s.defaultLocale) }),
    /* @__PURE__ */ A.jsx("div", { className: "survey-question__rating-scale", role: "radiogroup", children: S.map((C) => {
      const G = typeof j == "number" && C <= j;
      return /* @__PURE__ */ A.jsx(
        "button",
        {
          type: "button",
          role: "radio",
          "aria-checked": j === C,
          "aria-label": `${C}`,
          className: "survey-question__rating-star" + (G ? " survey-question__rating-star--selected" : ""),
          onClick: () => d(m, C),
          children: /* @__PURE__ */ A.jsx("span", { "aria-hidden": "true", children: "★" })
        },
        C
      );
    }) })
  ] });
}
function T0({ question: i }) {
  const { locale: f, schema: s, answers: r, setAnswer: d } = re(), m = i.id, b = i.title, T = i.help, q = !!i.required, g = i.options ?? [], j = r[m];
  return /* @__PURE__ */ A.jsxs("fieldset", { className: "survey-question survey-question--single", children: [
    /* @__PURE__ */ A.jsxs("legend", { className: "survey-question__label", children: [
      et(b, f, s.defaultLocale),
      q && /* @__PURE__ */ A.jsx("span", { "aria-label": "required", className: "survey-question__required", children: " *" })
    ] }),
    T && /* @__PURE__ */ A.jsx("p", { className: "survey-question__help", children: et(T, f, s.defaultLocale) }),
    /* @__PURE__ */ A.jsx("div", { className: "survey-question__options", children: g.map((S) => /* @__PURE__ */ A.jsxs("label", { className: "survey-question__option", children: [
      /* @__PURE__ */ A.jsx(
        "input",
        {
          type: "radio",
          name: `q-${m}`,
          value: S.id,
          checked: j === S.id,
          onChange: () => d(m, S.id)
        }
      ),
      /* @__PURE__ */ A.jsx("span", { children: et(S.label, f, s.defaultLocale) })
    ] }, S.id)) })
  ] });
}
function z0({ question: i }) {
  const { locale: f, schema: s, answers: r, setAnswer: d } = re(), m = i.id, b = i.title, T = i.help, q = !!i.required, g = i.options ?? [], j = i.maxSelected, S = r[m] ?? [], C = (G) => {
    if (S.includes(G)) {
      d(m, S.filter((K) => K !== G));
      return;
    }
    j !== void 0 && S.length >= j || d(m, [...S, G]);
  };
  return /* @__PURE__ */ A.jsxs("fieldset", { className: "survey-question survey-question--multi", children: [
    /* @__PURE__ */ A.jsxs("legend", { className: "survey-question__label", children: [
      et(b, f, s.defaultLocale),
      q && /* @__PURE__ */ A.jsx("span", { "aria-label": "required", className: "survey-question__required", children: " *" })
    ] }),
    T && /* @__PURE__ */ A.jsx("p", { className: "survey-question__help", children: et(T, f, s.defaultLocale) }),
    /* @__PURE__ */ A.jsx("div", { className: "survey-question__options", children: g.map((G) => {
      const K = S.includes(G.id);
      return /* @__PURE__ */ A.jsxs("label", { className: "survey-question__option", children: [
        /* @__PURE__ */ A.jsx(
          "input",
          {
            type: "checkbox",
            checked: K,
            onChange: () => C(G.id)
          }
        ),
        /* @__PURE__ */ A.jsx("span", { children: et(G.label, f, s.defaultLocale) })
      ] }, G.id);
    }) })
  ] });
}
function q0({ question: i }) {
  const { locale: f, schema: s, answers: r, setAnswer: d, ui: m } = re(), b = i.id, T = i.title, q = i.help, g = !!i.required, j = i.options ?? [], S = i.placeholder, C = r[b] ?? "", G = S ? et(S, f, s.defaultLocale) : m.selectPlaceholder;
  return /* @__PURE__ */ A.jsxs("div", { className: "survey-question survey-question--dropdown", children: [
    /* @__PURE__ */ A.jsxs("label", { className: "survey-question__label", htmlFor: `q-${b}`, children: [
      et(T, f, s.defaultLocale),
      g && /* @__PURE__ */ A.jsx("span", { "aria-label": "required", className: "survey-question__required", children: " *" })
    ] }),
    q && /* @__PURE__ */ A.jsx("p", { className: "survey-question__help", children: et(q, f, s.defaultLocale) }),
    /* @__PURE__ */ A.jsxs(
      "select",
      {
        id: `q-${b}`,
        className: "survey-question__select",
        value: C,
        required: g,
        onChange: (K) => d(b, K.target.value || null),
        children: [
          /* @__PURE__ */ A.jsx("option", { value: "", children: G }),
          j.map((K) => /* @__PURE__ */ A.jsx("option", { value: K.id, children: et(K.label, f, s.defaultLocale) }, K.id))
        ]
      }
    )
  ] });
}
function N0({ question: i }) {
  const { locale: f, schema: s, answers: r, setAnswer: d } = re(), m = i.id, b = i.title, T = i.help, q = !!i.required, g = i.minDate, j = i.maxDate, S = r[m] ?? "";
  return /* @__PURE__ */ A.jsxs("div", { className: "survey-question survey-question--date", children: [
    /* @__PURE__ */ A.jsxs("label", { className: "survey-question__label", htmlFor: `q-${m}`, children: [
      et(b, f, s.defaultLocale),
      q && /* @__PURE__ */ A.jsx("span", { "aria-label": "required", className: "survey-question__required", children: " *" })
    ] }),
    T && /* @__PURE__ */ A.jsx("p", { className: "survey-question__help", children: et(T, f, s.defaultLocale) }),
    /* @__PURE__ */ A.jsx(
      "input",
      {
        id: `q-${m}`,
        className: "survey-question__input",
        type: "date",
        value: S,
        required: q,
        min: g,
        max: j,
        onChange: (C) => d(m, C.target.value || null)
      }
    )
  ] });
}
function O0({ question: i }) {
  const { locale: f, schema: s, answers: r, setAnswer: d } = re(), m = i.id, b = i.title, T = i.help, q = !!i.required, g = i.minDateTime, j = i.maxDateTime, S = r[m] ?? "", C = (G) => {
    if (!G) return;
    const K = G.match(/^(\d{4}-\d{2}-\d{2}T\d{2}:\d{2})/);
    return (K == null ? void 0 : K[1]) ?? void 0;
  };
  return /* @__PURE__ */ A.jsxs("div", { className: "survey-question survey-question--datetime", children: [
    /* @__PURE__ */ A.jsxs("label", { className: "survey-question__label", htmlFor: `q-${m}`, children: [
      et(b, f, s.defaultLocale),
      q && /* @__PURE__ */ A.jsx("span", { "aria-label": "required", className: "survey-question__required", children: " *" })
    ] }),
    T && /* @__PURE__ */ A.jsx("p", { className: "survey-question__help", children: et(T, f, s.defaultLocale) }),
    /* @__PURE__ */ A.jsx(
      "input",
      {
        id: `q-${m}`,
        className: "survey-question__input",
        type: "datetime-local",
        value: C(S) ?? "",
        required: q,
        min: C(g),
        max: C(j),
        onChange: (G) => d(m, G.target.value || null)
      }
    )
  ] });
}
function M0({ question: i }) {
  const { locale: f, schema: s, answers: r, setAnswer: d, ui: m } = re(), b = i.id, T = i.title, q = i.help, g = !!i.required, j = i.acceptedTypes, S = V.useRef(null), C = r[b], G = j && j.length > 0 ? j.join(",") : void 0;
  return /* @__PURE__ */ A.jsxs("div", { className: "survey-question survey-question--file", children: [
    /* @__PURE__ */ A.jsxs("label", { className: "survey-question__label", htmlFor: `q-${b}`, children: [
      et(T, f, s.defaultLocale),
      g && /* @__PURE__ */ A.jsx("span", { "aria-label": "required", className: "survey-question__required", children: " *" })
    ] }),
    q && /* @__PURE__ */ A.jsx("p", { className: "survey-question__help", children: et(q, f, s.defaultLocale) }),
    /* @__PURE__ */ A.jsx(
      "input",
      {
        ref: S,
        id: `q-${b}`,
        className: "survey-question__file",
        type: "file",
        required: g,
        accept: G,
        onChange: (K) => {
          var P;
          const B = (P = K.target.files) == null ? void 0 : P[0];
          if (!B) {
            d(b, null);
            return;
          }
          d(b, { name: B.name, size: B.size, type: B.type });
        }
      }
    ),
    (C == null ? void 0 : C.name) && /* @__PURE__ */ A.jsx("p", { className: "survey-question__file-name", children: en(m.fileRecordedName, { name: C.name }) })
  ] });
}
const gs = 480, ps = 160;
function D0({ question: i }) {
  const { locale: f, schema: s, answers: r, setAnswer: d, ui: m } = re(), b = i.id, T = i.title, q = i.help, g = !!i.required, j = V.useRef(null), [S, C] = V.useState(!1), [G, K] = V.useState(!!r[b]), P = () => {
    var I;
    return ((I = j.current) == null ? void 0 : I.getContext("2d")) ?? null;
  }, B = (I) => {
    const ct = I.target.getBoundingClientRect();
    return {
      x: (I.clientX - ct.left) / ct.width * gs,
      y: (I.clientY - ct.top) / ct.height * ps
    };
  }, W = V.useCallback(() => {
    var I;
    const ct = (I = j.current) == null ? void 0 : I.toDataURL("image/png");
    ct && d(b, ct);
  }, [b, d]), it = () => {
    const I = P();
    I && (I.clearRect(0, 0, gs, ps), K(!1), d(b, null));
  };
  return V.useEffect(() => {
    const I = P();
    I && (I.lineWidth = 2, I.lineCap = "round", I.strokeStyle = "#111");
  }, []), /* @__PURE__ */ A.jsxs("div", { className: "survey-question survey-question--signature", children: [
    /* @__PURE__ */ A.jsxs("div", { className: "survey-question__label", children: [
      et(T, f, s.defaultLocale),
      g && /* @__PURE__ */ A.jsx("span", { "aria-label": "required", className: "survey-question__required", children: " *" })
    ] }),
    q && /* @__PURE__ */ A.jsx("p", { className: "survey-question__help", children: et(q, f, s.defaultLocale) }),
    /* @__PURE__ */ A.jsx(
      "canvas",
      {
        ref: j,
        className: "survey-question__signature-canvas",
        width: gs,
        height: ps,
        role: "img",
        "aria-label": "signature pad",
        onPointerDown: (I) => {
          I.target.setPointerCapture(I.pointerId);
          const ct = P();
          if (!ct) return;
          const { x: Ft, y: Lt } = B(I);
          ct.beginPath(), ct.moveTo(Ft, Lt), C(!0);
        },
        onPointerMove: (I) => {
          if (!S) return;
          const ct = P();
          if (!ct) return;
          const { x: Ft, y: Lt } = B(I);
          ct.lineTo(Ft, Lt), ct.stroke(), K(!0);
        },
        onPointerUp: () => {
          C(!1), G && W();
        }
      }
    ),
    /* @__PURE__ */ A.jsx("div", { className: "survey-question__signature-actions", children: /* @__PURE__ */ A.jsx("button", { type: "button", className: "survey-button survey-button--ghost", onClick: it, children: m.clearSignature }) })
  ] });
}
function U0({ question: i }) {
  const { locale: f, schema: s, answers: r, setAnswer: d, ui: m } = re(), b = i.id, T = i.title, q = i.help, g = !!i.required, j = i.yesLabel, S = i.noLabel, C = r[b], G = j ? et(j, f, s.defaultLocale) : m.yes, K = S ? et(S, f, s.defaultLocale) : m.no;
  return /* @__PURE__ */ A.jsxs("fieldset", { className: "survey-question survey-question--yesno", children: [
    /* @__PURE__ */ A.jsxs("legend", { className: "survey-question__label", children: [
      et(T, f, s.defaultLocale),
      g && /* @__PURE__ */ A.jsx("span", { "aria-label": "required", className: "survey-question__required", children: " *" })
    ] }),
    q && /* @__PURE__ */ A.jsx("p", { className: "survey-question__help", children: et(q, f, s.defaultLocale) }),
    /* @__PURE__ */ A.jsxs("div", { className: "survey-question__yesno", role: "radiogroup", children: [
      /* @__PURE__ */ A.jsx(
        "button",
        {
          type: "button",
          role: "radio",
          "aria-checked": C === !0,
          className: "survey-question__yesno-button" + (C === !0 ? " survey-question__yesno-button--selected" : ""),
          onClick: () => d(b, !0),
          children: G
        }
      ),
      /* @__PURE__ */ A.jsx(
        "button",
        {
          type: "button",
          role: "radio",
          "aria-checked": C === !1,
          className: "survey-question__yesno-button" + (C === !1 ? " survey-question__yesno-button--selected" : ""),
          onClick: () => d(b, !1),
          children: K
        }
      )
    ] })
  ] });
}
function j0(i, f) {
  switch (i.code) {
    case "minLength":
      return en(f.minLengthError, i.params);
    case "maxLength":
      return en(f.maxLengthError, i.params);
    case "pattern":
      return f.patternError;
    case "min":
      return en(f.minError, i.params);
    case "max":
      return en(f.maxError, i.params);
    case "range":
      return en(f.rangeError, i.params);
    case "minSelected":
      return en(f.minSelectedError, i.params);
    case "maxSelected":
      return en(f.maxSelectedError, i.params);
    default:
      return f.invalidAnswerError;
  }
}
const C0 = {
  text: b0,
  paragraph: E0,
  number: x0,
  rating: A0,
  nps: S0,
  singleChoice: T0,
  multiChoice: z0,
  dropdown: q0,
  date: N0,
  dateTime: O0,
  file: M0,
  signature: D0,
  yesNo: U0,
  navigationList: _0
};
function R0(i, f, s) {
  const r = i.screens.find((d) => d.id === f);
  return !r || (r.questions ?? []).length > 0 ? !1 : Ea(i, f, s).kind === "end";
}
function H0({
  schema: i,
  onSubmit: f,
  initialAnswers: s,
  locale: r,
  onLocaleChange: d,
  showLocalePicker: m,
  onScreenChange: b,
  onCompleted: T,
  registry: q,
  submissionMeta: g,
  uiLocales: j,
  resumeKey: S,
  storage: C,
  emitHostMessages: G,
  hostMessageOrigin: K,
  hostMessageTarget: P,
  activeScreenId: B,
  activeScreenJumpToken: W
}) {
  var it, I, ct;
  const [Ft, Lt] = V.useState(null), nt = r ?? i.defaultLocale ?? "en", _t = Ft !== null && (((it = i.locales) == null ? void 0 : it.includes(Ft)) ?? !1) ? Ft : nt, _e = V.useRef(nt);
  V.useEffect(() => {
    _e.current !== nt && (_e.current = nt, Lt(null));
  }, [nt]);
  const ql = q ?? C0, bt = V.useMemo(
    () => r0(_t, i.defaultLocale, j),
    [_t, i.defaultLocale, j]
  ), kt = i.locales ?? [], nl = m ?? kt.length > 1, Qe = V.useCallback(
    (R) => {
      Lt(R), d == null || d(R);
    },
    [d]
  ), Qt = C ?? (typeof globalThis < "u" ? globalThis.localStorage : void 0), N = V.useMemo(() => {
    var R;
    if (!S || !Qt) return null;
    const w = m0(Qt, S);
    return w ? w.currentScreenId === null || i.screens.some((mt) => mt.id === w.currentScreenId) ? !w.history && w.currentScreenId ? { ...w, history: jv(i, w.answers, w.currentScreenId) ?? [] } : w : { ...w, currentScreenId: ((R = i.screens[0]) == null ? void 0 : R.id) ?? null, history: [] } : null;
  }, []), [H, $] = V.useState(() => ({
    ...s ?? {},
    ...(N == null ? void 0 : N.answers) ?? {}
  })), [Y, yt] = V.useState(
    () => {
      var R;
      return (N == null ? void 0 : N.currentScreenId) ?? ((R = i.screens[0]) == null ? void 0 : R.id) ?? null;
    }
  ), [h, D] = V.useState(() => (N == null ? void 0 : N.history) ?? []);
  V.useEffect(() => {
    if (i.screens.length === 0) {
      Y !== null && yt(null);
      return;
    }
    Y !== null && i.screens.some((R) => R.id === Y) || yt(i.screens[0].id);
  }, [i, Y]);
  const [L, Q] = V.useState(!1), [F, at] = V.useState(null), [vt, Xt] = V.useState(/* @__PURE__ */ new Set()), [Ct, ke] = V.useState(/* @__PURE__ */ new Set()), [oe, za] = V.useState(!1), qa = V.useRef(void 0);
  V.useEffect(() => {
    if (B === void 0) return;
    const R = `${W ?? ""}:${B ?? ""}`;
    qa.current !== R && (qa.current = R, !(B === null || oe) && i.screens.some((w) => w.id === B) && (Xt(/* @__PURE__ */ new Set()), ke(/* @__PURE__ */ new Set()), D((w) => {
      const mt = w.indexOf(B);
      return mt >= 0 ? w.slice(0, mt) : Y && Y !== B ? [...w, Y] : w;
    }), yt(B)));
  }, [B, W, i, oe, Y]);
  const al = V.useRef((/* @__PURE__ */ new Date()).toISOString()), Xe = V.useRef(null);
  if (Xe.current === null) {
    const R = {};
    P !== void 0 && (R.target = P), K !== void 0 && (R.targetOrigin = K), G !== void 0 && (R.enabled = G), Xe.current = y0(R);
  }
  const ce = V.useMemo(
    () => Y ? i.screens.find((R) => R.id === Y) ?? null : null,
    [i, Y]
  ), Au = V.useMemo(() => {
    const R = /* @__PURE__ */ new Map();
    for (const w of i.screens)
      for (const mt of w.questions ?? []) {
        const Dt = mt.id;
        typeof Dt == "string" && R.set(Dt, w.id);
      }
    return R;
  }, [i]), fe = V.useMemo(() => {
    const R = new Set(h);
    Y && R.add(Y);
    const w = {};
    for (const [mt, Dt] of Object.entries(H)) {
      const Zt = Au.get(mt);
      (Zt === void 0 || R.has(Zt)) && (w[mt] = Dt);
    }
    return w;
  }, [H, h, Y, Au]), un = V.useRef(/* @__PURE__ */ new Map()), [Na, Oa] = V.useState(0), Tu = V.useCallback((R, w) => {
    un.current.get(R) !== w && (un.current.set(R, w), Oa((mt) => mt + 1));
  }, []), Dn = V.useMemo(
    () => Vv({
      schema: i,
      answers: fe,
      lookupOptions: (R) => un.current.get(R),
      yesNoLabels: { yes: bt.strings.yes, no: bt.strings.no }
    }),
    // sourcedVersion is the change signal for the ref-held map.
    // eslint-disable-next-line react-hooks/exhaustive-deps
    [i, fe, bt.strings.yes, bt.strings.no, Na]
  ), Nt = V.useMemo(
    () => ce ? $v(ce, Dn) : null,
    [ce, Dn]
  );
  V.useEffect(() => {
    var R;
    b == null || b(Y), (R = Xe.current) == null || R.screenChanged(Y);
  }, [Y, b]);
  const zu = V.useRef(!1);
  V.useEffect(() => {
    var R;
    zu.current || !Y || (zu.current = !0, (R = Xe.current) == null || R.loaded());
  }, [Y]), V.useEffect(() => {
    !S || !Qt || oe || h0(Qt, S, {
      answers: H,
      currentScreenId: Y,
      history: [...h],
      schemaVersion: i.version
    });
  }, [H, Y, h, S, Qt, oe, i.version]), V.useEffect(() => {
    oe && S && Qt && v0(Qt, S);
  }, [oe, S, Qt]), V.useEffect(() => {
    var R;
    F && ((R = Xe.current) == null || R.error(F));
  }, [F]);
  const cn = V.useCallback((R, w) => {
    $((mt) => ({ ...mt, [R]: w }));
  }, []), fn = V.useCallback(
    (R) => {
      R !== null && (Xt(/* @__PURE__ */ new Set()), ke(/* @__PURE__ */ new Set()), Y && Y !== R && D((w) => [...w, Y]), yt(R));
    },
    [Y]
  ), Un = V.useCallback(() => {
    let R = h.length - 1;
    for (; R >= 0 && !i.screens.some((mt) => mt.id === h[R]); ) R--;
    if (R < 0) return;
    const w = h[R];
    Xt(/* @__PURE__ */ new Set()), ke(/* @__PURE__ */ new Set()), at(null), D(h.slice(0, R)), yt(w);
  }, [h, i]), wi = h.some((R) => i.screens.some((w) => w.id === R)), jn = V.useCallback(
    (R) => {
      if (!R.required) return !1;
      const w = H[R.id];
      return !!(w == null || typeof w == "string" && w.trim() === "" || Array.isArray(w) && w.length === 0);
    },
    [H]
  ), Nl = V.useCallback(async () => {
    var R;
    Q(!0), at(null);
    try {
      await f({
        schemaVersion: i.version ?? 0,
        answers: fe,
        meta: {
          startedAt: (g == null ? void 0 : g.startedAt) ?? al.current,
          completedAt: (g == null ? void 0 : g.completedAt) ?? (/* @__PURE__ */ new Date()).toISOString(),
          ...g ?? {}
        }
      }), za(!0), T == null || T(Y), (R = Xe.current) == null || R.completed({ screenId: Y, answers: fe });
    } catch (w) {
      at(w.message ?? String(w));
    } finally {
      Q(!1);
    }
  }, [i.version, fe, g, f, T, Y]), Ji = V.useCallback(() => {
    if (!Y) return;
    const R = i.screens.find((Zt) => Zt.id === Y), w = ((R == null ? void 0 : R.questions) ?? []).filter(jn).map((Zt) => Zt.id);
    if (w.length > 0) {
      Xt(new Set(w));
      return;
    }
    const mt = Rv(R == null ? void 0 : R.questions, H);
    if (mt.length > 0) {
      ke(new Set(mt.map((Zt) => Zt.questionId)));
      return;
    }
    const Dt = Ea(i, Y, fe);
    Dt.kind === "end" ? Nl() : fn(Dt.screenId);
  }, [i, Y, H, fe, jn, fn, Nl]), Ol = V.useRef(null);
  V.useEffect(() => {
    oe || L || !Y || !ce || Ol.current === Y || !(!ce.questions || ce.questions.length === 0) || Ea(i, Y, fe).kind === "end" && (Ol.current = Y, Nl());
  }, [Y, ce, oe, L, i, fe, Nl]);
  const $t = V.useRef(null);
  V.useEffect(() => {
    const R = $t.current;
    if (!R || typeof ResizeObserver > "u") return;
    const w = new ResizeObserver((mt) => {
      var Dt;
      const Zt = mt[0];
      Zt && ((Dt = Xe.current) == null || Dt.resize(Math.ceil(Zt.contentRect.height)));
    });
    return w.observe(R), () => w.disconnect();
  }, []), V.useEffect(() => {
    const R = $t.current;
    if (!R) return;
    const w = (mt) => {
      const Dt = mt.detail;
      if (!Dt || !Y) return;
      cn(Dt.questionId, Dt.option.id);
      const Zt = { ...fe, [Dt.questionId]: Dt.option.id }, Rn = Uv(
        Dt.option,
        i,
        Y,
        Zt
      );
      Rn.kind === "end" ? Nl() : fn(Rn.screenId);
    };
    return R.addEventListener("survey:navigationListSelect", w), () => R.removeEventListener("survey:navigationListSelect", w);
  }, [fe, Y, i, cn, fn, Nl]);
  const $e = V.useMemo(
    () => ({
      schema: i,
      locale: _t,
      direction: bt.direction,
      ui: bt.strings,
      answers: H,
      setAnswer: cn,
      answerContext: Dn,
      registerSourcedOptions: Tu
    }),
    [i, _t, bt, H, cn, Dn, Tu]
  ), te = V.useMemo(() => l0(i.branding), [i.branding]), qu = (I = i.branding) != null && I.logoUrl ? /* @__PURE__ */ A.jsx("div", { className: "survey-brand", children: /* @__PURE__ */ A.jsx(
    "img",
    {
      className: "survey-brand__logo",
      src: i.branding.logoUrl,
      alt: "",
      onError: (R) => {
        R.currentTarget.parentElement.style.display = "none";
      }
    }
  ) }) : null, Ki = kt.includes(_t) ? kt : [_t, ...kt], Nu = nl ? /* @__PURE__ */ A.jsxs("label", { className: "survey-locale", children: [
    /* @__PURE__ */ A.jsx("span", { className: "survey-locale__icon", "aria-hidden": "true", children: /* @__PURE__ */ A.jsxs("svg", { viewBox: "0 0 24 24", width: "18", height: "18", fill: "none", stroke: "currentColor", strokeWidth: "1.8", children: [
      /* @__PURE__ */ A.jsx("circle", { cx: "12", cy: "12", r: "9" }),
      /* @__PURE__ */ A.jsx("path", { d: "M3 12h18M12 3a15 15 0 0 1 0 18a15 15 0 0 1 0-18" })
    ] }) }),
    /* @__PURE__ */ A.jsx("span", { className: "survey-locale__code", "aria-hidden": "true", children: _t.toUpperCase() }),
    /* @__PURE__ */ A.jsx(
      "select",
      {
        className: "survey-locale__select",
        "aria-label": bt.strings.language,
        value: _t,
        onChange: (R) => Qe(R.target.value),
        children: Ki.map((R) => /* @__PURE__ */ A.jsx("option", { value: R, children: s0(R) }, R))
      }
    )
  ] }) : null, Cn = (ce == null ? void 0 : ce.questions) ?? [], sn = ce !== null && Cn.length === 0 && !ce.nextScreen, ul = !oe && wi && !L && !sn, We = i.screens.length > 1 ? /* @__PURE__ */ A.jsx(
    "button",
    {
      type: "button",
      className: ul ? "survey-back" : "survey-back survey-back--hidden",
      "aria-label": bt.strings.back,
      "aria-hidden": !ul,
      tabIndex: ul ? void 0 : -1,
      onClick: Un,
      children: /* @__PURE__ */ A.jsx(
        "svg",
        {
          className: "survey-back__icon",
          viewBox: "0 0 24 24",
          width: "24",
          height: "24",
          fill: "none",
          stroke: "currentColor",
          strokeWidth: "2.2",
          strokeLinecap: "round",
          strokeLinejoin: "round",
          "aria-hidden": "true",
          children: /* @__PURE__ */ A.jsx("path", { d: "M15 5l-7 7 7 7" })
        }
      )
    }
  ) : null, Ml = qu || Nu || We ? /* @__PURE__ */ A.jsxs("div", { className: ul ? "survey-chrome survey-chrome--back" : "survey-chrome", children: [
    /* @__PURE__ */ A.jsx("div", { className: "survey-chrome__start", children: We }),
    qu,
    /* @__PURE__ */ A.jsx("div", { className: "survey-chrome__end", children: Nu })
  ] }) : null;
  if (oe)
    return /* @__PURE__ */ A.jsxs(
      "div",
      {
        ref: $t,
        className: "survey-root survey-root--done",
        dir: bt.direction,
        lang: _t,
        style: te,
        children: [
          Ml,
          /* @__PURE__ */ A.jsxs("div", { className: "survey-screen", children: [
            /* @__PURE__ */ A.jsx("h2", { className: "survey-screen__title", children: Nt != null && Nt.title ? et(Nt.title, _t, i.defaultLocale) : bt.strings.thankYou }),
            (Nt == null ? void 0 : Nt.description) && /* @__PURE__ */ A.jsx("p", { className: "survey-screen__description", children: et(Nt.description, _t, i.defaultLocale) })
          ] })
        ]
      }
    );
  if (!ce || !Nt)
    return /* @__PURE__ */ A.jsxs("div", { ref: $t, className: "survey-root", dir: bt.direction, lang: _t, style: te, children: [
      Ml,
      /* @__PURE__ */ A.jsx("div", { className: "survey-screen", children: /* @__PURE__ */ A.jsx("em", { children: bt.strings.noScreens }) })
    ] });
  const Fe = Nt.questions ?? [], Ou = !(Fe.length > 0 && ((ct = Fe[Fe.length - 1]) == null ? void 0 : ct.type) === "navigationList") && !sn, rn = Ou && Y !== null ? Ea(i, Y, fe) : null, Ma = rn !== null && (rn.kind === "end" || rn.kind === "screen" && R0(i, rn.screenId, fe));
  return /* @__PURE__ */ A.jsx(n0, { value: $e, children: /* @__PURE__ */ A.jsxs("div", { ref: $t, className: "survey-root", dir: bt.direction, lang: _t, style: te, children: [
    Ml,
    /* @__PURE__ */ A.jsxs("div", { className: "survey-screen", children: [
      Nt.title && /* @__PURE__ */ A.jsx("h2", { className: "survey-screen__title", children: et(Nt.title, _t, i.defaultLocale) }),
      Nt.description && /* @__PURE__ */ A.jsx("p", { className: "survey-screen__description", children: et(Nt.description, _t, i.defaultLocale) }),
      /* @__PURE__ */ A.jsx("div", { className: "survey-screen__questions", children: Fe.map((R, w) => {
        const mt = R.id, Dt = mt !== void 0 && vt.has(mt) && jn(R), Zt = !Dt && mt !== void 0 && Ct.has(mt) && H[mt] != null ? Yy(R, H[mt])[0] ?? null : null;
        return /* @__PURE__ */ A.jsxs("div", { className: Dt || Zt !== null ? "survey-question-slot survey-question-slot--invalid" : "survey-question-slot", children: [
          /* @__PURE__ */ A.jsx(p0, { question: R, registry: ql }),
          Dt && /* @__PURE__ */ A.jsx("p", { className: "survey-question__required-error", role: "alert", children: bt.strings.requiredError }),
          Zt && /* @__PURE__ */ A.jsx("p", { className: "survey-question__required-error", role: "alert", children: j0(Zt, bt.strings) })
        ] }, mt ?? w);
      }) }),
      Ou && /* @__PURE__ */ A.jsx("div", { className: "survey-screen__actions", children: /* @__PURE__ */ A.jsx(
        "button",
        {
          type: "button",
          className: "survey-button survey-button--primary",
          disabled: L,
          onClick: Ji,
          children: L ? bt.strings.submitting : Ma ? bt.strings.submit : bt.strings.next
        }
      ) }),
      F && /* @__PURE__ */ A.jsxs("p", { className: "survey-screen__error", role: "alert", children: [
        bt.strings.couldNotSubmit,
        " ",
        F
      ] })
    ] })
  ] }) });
}
const L0 = ".survey-root{--survey-primary: #2563eb;--survey-primary-hover: #1e40af;--survey-primary-contrast: #ffffff;--survey-accent: #f5b60c;font-family:system-ui,-apple-system,Segoe UI,Roboto,sans-serif;color:#111;max-width:640px;margin:0 auto;padding:24px 16px 32px}.survey-chrome{display:flex;align-items:center;min-height:40px;margin-bottom:20px}.survey-chrome__start{flex:none;width:0;margin-inline-end:0;transition:width .25s ease,margin .25s ease}.survey-chrome--back .survey-chrome__start{width:32px;margin-inline-end:8px}.survey-chrome__end{flex:none;margin-inline-start:auto;padding-inline-start:8px}.survey-brand{display:flex;flex:0 1 auto;min-width:0}.survey-brand__logo{height:28px;width:auto;max-width:100%;object-fit:contain}.survey-back{width:40px;height:40px;display:inline-flex;align-items:center;justify-content:center;border:0;border-radius:999px;background:transparent;color:#344054;cursor:pointer;margin-inline-start:-8px;opacity:1;transform:none;transition:opacity .25s ease,transform .25s ease,background-color .15s ease,visibility 0s linear 0s}.survey-back--hidden{opacity:0;transform:translate(-12px);visibility:hidden;pointer-events:none;transition:opacity .2s ease,transform .2s ease,visibility 0s linear .2s}.survey-root[dir=rtl] .survey-back--hidden{transform:translate(12px)}.survey-back:hover{background:#f2f4f7}.survey-back:focus-visible{outline:2px solid var(--survey-primary, #2563eb);outline-offset:1px}.survey-root[dir=rtl] .survey-back__icon{transform:scaleX(-1)}@media(prefers-reduced-motion:reduce){.survey-chrome__start,.survey-back{transition:none}}.survey-locale{position:relative;display:inline-flex;align-items:center;gap:4px;min-height:40px;padding:0 4px;border-radius:6px;color:#6b7280;cursor:pointer}.survey-locale:hover{background:#f2f4f7}.survey-locale:focus-within{outline:2px solid var(--survey-primary, #2563eb);outline-offset:1px}.survey-locale__icon{display:inline-flex;flex:none}.survey-locale__code{font-size:.8rem;font-weight:600;letter-spacing:.02em;line-height:1}.survey-locale__select{position:absolute;top:0;right:0;bottom:0;left:0;width:100%;height:100%;opacity:0;-moz-appearance:none;appearance:none;-webkit-appearance:none;border:0;margin:0;padding:0;font:inherit;cursor:pointer}.survey-screen{display:flex;flex-direction:column;gap:24px}.survey-screen__title{font-size:1.5rem;font-weight:600;margin:0}.survey-screen__description{color:#555;margin:0}.survey-screen__questions{display:flex;flex-direction:column;gap:24px}.survey-screen__actions{display:flex;justify-content:flex-end}.survey-screen__error{color:#b42318;background:#fef3f2;border:1px solid #fecdca;padding:12px 14px;border-radius:8px;margin:0}.survey-question-slot--invalid{border-inline-start:3px solid #b42318;padding-inline-start:10px}.survey-question__required-error{color:#b42318;font-size:.9rem;margin:4px 0 0}.survey-question{display:flex;flex-direction:column;gap:8px}.survey-question__label{font-weight:600;display:block}.survey-question__required{color:#b42318}.survey-question__help{margin:0;color:#666;font-size:.9rem}.survey-question__input{padding:10px 12px;border:1px solid #d0d5dd;border-radius:8px;font:inherit}.survey-question__input:focus-visible{outline:2px solid var(--survey-primary);outline-offset:1px;border-color:var(--survey-primary)}.survey-question--nps{border:none;padding:0;margin:0;display:flex;flex-direction:column;gap:8px}.survey-question__nps-scale{display:flex;gap:6px;flex-wrap:wrap}.survey-question__nps-step{min-width:40px;min-height:40px;padding:8px;border:1px solid #d0d5dd;border-radius:8px;background:#fff;font-weight:500;cursor:pointer}.survey-question__nps-step:hover{background:#f5f7fa}.survey-question__nps-step--selected{background:var(--survey-primary);border-color:var(--survey-primary);color:var(--survey-primary-contrast)}.survey-question__nps-labels{display:flex;justify-content:space-between;color:#555;font-size:.85rem}.survey-question--navlist{gap:12px}.survey-navlist{list-style:none;padding:0;margin:0;display:flex;flex-direction:column;gap:8px}.survey-navlist__row{margin:0}.survey-navlist__button{width:100%;display:flex;align-items:center;justify-content:space-between;padding:14px 16px;border:1px solid #d0d5dd;border-radius:10px;background:#fff;cursor:pointer;font:inherit;text-align:start}.survey-navlist__button:hover{background:#f5f7fa;border-color:var(--survey-primary)}.survey-navlist__button--selected{border-color:var(--survey-primary);box-shadow:inset 0 0 0 1px var(--survey-primary)}.survey-navlist__chevron{font-size:1.5rem;color:#667085}.survey-root[dir=rtl] .survey-navlist__chevron{transform:scaleX(-1)}.survey-navlist__label{font-weight:500}.survey-question__textarea{padding:10px 12px;border:1px solid #d0d5dd;border-radius:8px;font:inherit;resize:vertical;min-height:96px}.survey-question__textarea:focus-visible{outline:2px solid var(--survey-primary);outline-offset:1px;border-color:var(--survey-primary)}.survey-question__number-wrap{display:flex;align-items:center;gap:8px}.survey-question__number-wrap .survey-question__input{flex:1}.survey-question__unit{color:#555;font-size:.9rem}.survey-question__rating-scale{display:flex;gap:4px}.survey-question__rating-star{background:transparent;border:none;cursor:pointer;font-size:1.8rem;line-height:1;color:#d0d5dd;padding:4px}.survey-question__rating-star:hover,.survey-question__rating-star--selected{color:var(--survey-accent)}.survey-question__options{display:flex;flex-direction:column;gap:8px}.survey-question__option{display:flex;align-items:center;gap:8px;padding:8px 12px;border:1px solid #d0d5dd;border-radius:8px;cursor:pointer}.survey-question__option:hover{background:#f5f7fa;border-color:var(--survey-primary)}.survey-question__option input{margin:0}.survey-question__select{padding:10px 12px;border:1px solid #d0d5dd;border-radius:8px;font:inherit;background:#fff}.survey-question__yesno{display:flex;gap:12px}.survey-question__yesno-button{flex:1;padding:14px 16px;border:1px solid #d0d5dd;border-radius:10px;background:#fff;font:inherit;font-weight:500;cursor:pointer}.survey-question__yesno-button:hover{background:#f5f7fa;border-color:var(--survey-primary)}.survey-question__yesno-button--selected{background:var(--survey-primary);border-color:var(--survey-primary);color:var(--survey-primary-contrast)}.survey-question__file{font:inherit}.survey-question__file-name{color:#555;font-size:.9rem;margin:0}.survey-question__signature-canvas{width:100%;max-width:480px;height:auto;aspect-ratio:3 / 1;border:1px dashed #d0d5dd;border-radius:8px;background:#fff;touch-action:none}.survey-question__signature-actions{display:flex;justify-content:flex-start;gap:8px}.survey-button{padding:10px 20px;border-radius:8px;border:1px solid transparent;cursor:pointer;font:inherit;font-weight:600}.survey-button--primary{background:var(--survey-primary);color:var(--survey-primary-contrast)}.survey-button--primary:hover{background:var(--survey-primary-hover)}.survey-button--ghost{background:#fff;color:#555;border-color:#d0d5dd}.survey-button--ghost:hover{background:#f5f7fa}.survey-button:disabled{opacity:.5;cursor:not-allowed}.survey-question__options-status{margin:6px 0;font-size:.9rem;color:var(--survey-muted, #667085)}.survey-question--options-error .survey-question__options-status{color:var(--survey-error, #b42318)}.survey-button--retry{background:transparent;color:var(--survey-primary, #4338ca);border:1px solid currentColor;padding:4px 14px;font-size:.85rem}";
var ln, xa, Ke, nn, Aa, On, Eu, Ta, Gt, xs, Tl, xu, Nn;
class B0 extends HTMLElement {
  constructor() {
    super();
    Je(this, Gt);
    /** Schema-mode setter. Assigning this swaps the element into schema mode and
     *  re-renders with the new schema immediately. */
    Je(this, ln, null);
    /** Schema-mode submit handler. In API mode the element manages this itself. */
    Je(this, xa, null);
    Je(this, Ke, null);
    Je(this, nn, null);
    Je(this, Aa, null);
    Je(this, On, null);
    /** Builder-preview jump target. Assigning a screen id makes the renderer
     *  jump to that screen (answers preserved); the user can navigate freely
     *  afterwards. Mirrors the `active-screen-id` attribute; the property wins
     *  when both are set. */
    Je(this, Eu, null);
    /** Bump to re-issue a jump to the screen already set on {@link activeScreenId}.
     *  Property-only (no attribute) — it is a transient signal, not page state. */
    Je(this, Ta, 0);
    Je(this, xu, !1);
    this.attachShadow({ mode: "open" });
  }
  static get observedAttributes() {
    return ["instance-id", "api-base", "locale", "mode", "active-screen-id", "locale-picker"];
  }
  // ─── Lifecycle ───────────────────────────────────────────────────────────
  connectedCallback() {
    if (this.shadowRoot) {
      if (!this.shadowRoot.querySelector("style[data-shift-survey]")) {
        const s = document.createElement("style");
        s.setAttribute("data-shift-survey", ""), s.textContent = L0, this.shadowRoot.appendChild(s);
      }
      jt(this, nn) || (Oe(this, nn, document.createElement("div")), jt(this, nn).className = "shift-survey-mount", this.shadowRoot.appendChild(jt(this, nn))), jt(this, Ke) || Oe(this, Ke, ov.createRoot(jt(this, nn))), ie(this, Gt, Tl).call(this), ie(this, Gt, xs).call(this);
    }
  }
  disconnectedCallback() {
    queueMicrotask(() => {
      var s;
      if (!(this.isConnected || typeof window > "u")) {
        try {
          (s = jt(this, Ke)) == null || s.unmount();
        } catch {
        }
        Oe(this, Ke, null);
      }
    });
  }
  attributeChangedCallback(s, r, d) {
    r !== d && ((s === "instance-id" || s === "api-base") && (Oe(this, Aa, null), Oe(this, On, null), ie(this, Gt, xs).call(this)), ie(this, Gt, Tl).call(this));
  }
  // ─── Properties ──────────────────────────────────────────────────────────
  get schema() {
    return jt(this, ln);
  }
  set schema(s) {
    Oe(this, ln, s), ie(this, Gt, Tl).call(this);
  }
  get onSubmit() {
    return jt(this, xa);
  }
  set onSubmit(s) {
    Oe(this, xa, s), ie(this, Gt, Tl).call(this);
  }
  get activeScreenId() {
    return jt(this, Eu) ?? this.getAttribute("active-screen-id");
  }
  set activeScreenId(s) {
    Oe(this, Eu, s), ie(this, Gt, Tl).call(this);
  }
  get activeScreenJumpToken() {
    return jt(this, Ta);
  }
  set activeScreenJumpToken(s) {
    Oe(this, Ta, s), ie(this, Gt, Tl).call(this);
  }
}
ln = new WeakMap(), xa = new WeakMap(), Ke = new WeakMap(), nn = new WeakMap(), Aa = new WeakMap(), On = new WeakMap(), Eu = new WeakMap(), Ta = new WeakMap(), Gt = new WeakSet(), // ─── Internals ───────────────────────────────────────────────────────────
xs = function() {
  if (jt(this, ln)) return;
  const s = this.getAttribute("instance-id");
  if (!s) return;
  const r = this.getAttribute("api-base");
  if (!r) return;
  new Hy({ baseUrl: r }).fetchSchema(s).then((m) => {
    Oe(this, Aa, m), ie(this, Gt, Tl).call(this);
  }).catch((m) => {
    Oe(this, On, m), ie(this, Gt, Nn).call(this, "survey:error", { message: m.message }), ie(this, Gt, Tl).call(this);
  });
}, Tl = function() {
  if (!jt(this, Ke)) return;
  const s = this.getAttribute("api-base"), r = this.getAttribute("instance-id"), d = this.getAttribute("locale") ?? void 0, m = this.getAttribute("mode") === "agent", b = this.getAttribute("locale-picker"), T = b === null ? void 0 : b !== "false" && b !== "off", q = jt(this, ln) ?? jt(this, Aa);
  if (jt(this, On) && !q) {
    jt(this, Ke).render(
      V.createElement(
        "div",
        { className: "shift-survey-error", role: "alert" },
        jt(this, On).message
      )
    );
    return;
  }
  if (!q) {
    jt(this, Ke).render(
      V.createElement("div", { className: "shift-survey-loading" }, "Loading…")
    );
    return;
  }
  const g = jt(this, ln) ? jt(this, xa) ?? ((S) => {
    ie(this, Gt, Nn).call(this, "survey:completed", { ...S });
  }) : async (S) => {
    if (!s || !r)
      throw new Error("shift-survey: API mode requires both instance-id and api-base attributes.");
    await new Hy({ baseUrl: s }).submitResponse(r, S);
  }, j = this.activeScreenId;
  jt(this, Ke).render(
    V.createElement(H0, {
      schema: q,
      onSubmit: g,
      ...d ? { locale: d } : {},
      ...T === void 0 ? {} : { showLocalePicker: T },
      // Mirror the respondent's pick back onto the element so a host can read it
      // (and so the attribute stays an accurate description of what is showing).
      onLocaleChange: (S) => {
        this.getAttribute("locale") !== S && this.setAttribute("locale", S), ie(this, Gt, Nn).call(this, "survey:locale-changed", { locale: S });
      },
      ...j ? { activeScreenId: j, activeScreenJumpToken: jt(this, Ta) } : {},
      // Let the element be the resume key in API mode so two surveys on the
      // same host page don't clobber each other.
      ...r ? { resumeKey: r } : {},
      ...m ? { submissionMeta: { mode: "agent" } } : {},
      // CustomEvents are the web-component's channel; postMessage stays opt-in
      // via iframe auto-detect on the enclosing page (unchanged).
      onScreenChange: (S) => ie(this, Gt, Nn).call(this, "survey:screen-changed", { screenId: S }),
      onCompleted: (S) => ie(this, Gt, Nn).call(this, "survey:completed", { screenId: S })
    })
  ), jt(this, xu) || (Oe(this, xu, !0), ie(this, Gt, Nn).call(this, "survey:loaded", {}));
}, xu = new WeakMap(), Nn = function(s, r) {
  this.dispatchEvent(
    new CustomEvent(s, { detail: r, bubbles: !0, composed: !0 })
  );
};
function Y0(i = "shift-survey") {
  typeof window > "u" || typeof customElements > "u" || customElements.get(i) || customElements.define(i, B0);
}
Y0();
export {
  B0 as ShiftSurveyElement,
  Y0 as registerShiftSurvey
};
//# sourceMappingURL=shift-survey.js.map
