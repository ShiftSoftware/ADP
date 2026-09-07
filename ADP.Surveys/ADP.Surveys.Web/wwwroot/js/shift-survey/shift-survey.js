var wh = Object.defineProperty;
var dy = (c) => {
  throw TypeError(c);
};
var Jh = (c, f, r) => f in c ? wh(c, f, { enumerable: !0, configurable: !0, writable: !0, value: r }) : c[f] = r;
var gl = (c, f, r) => Jh(c, typeof f != "symbol" ? f + "" : f, r), Ff = (c, f, r) => f.has(c) || dy("Cannot " + r);
var Ut = (c, f, r) => (Ff(c, f, "read from private field"), r ? r.call(c) : f.get(c)), Xe = (c, f, r) => f.has(c) ? dy("Cannot add the same private member more than once") : f instanceof WeakSet ? f.add(c) : f.set(c, r), Te = (c, f, r, s) => (Ff(c, f, "write to private field"), s ? s.call(c, r) : f.set(c, r), r), ne = (c, f, r) => (Ff(c, f, "access private method"), r);
var If = { exports: {} }, I = {};
/**
 * @license React
 * react.production.js
 *
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 */
var yy;
function Kh() {
  if (yy) return I;
  yy = 1;
  var c = Symbol.for("react.transitional.element"), f = Symbol.for("react.portal"), r = Symbol.for("react.fragment"), s = Symbol.for("react.strict_mode"), h = Symbol.for("react.profiler"), g = Symbol.for("react.consumer"), q = Symbol.for("react.context"), U = Symbol.for("react.forward_ref"), N = Symbol.for("react.suspense"), p = Symbol.for("react.memo"), C = Symbol.for("react.lazy"), b = Symbol.for("react.activity"), j = Symbol.iterator;
  function Q(m) {
    return m === null || typeof m != "object" ? null : (m = j && m[j] || m["@@iterator"], typeof m == "function" ? m : null);
  }
  var W = {
    isMounted: function() {
      return !1;
    },
    enqueueForceUpdate: function() {
    },
    enqueueReplaceState: function() {
    },
    enqueueSetState: function() {
    }
  }, w = Object.assign, B = {};
  function J(m, D, H) {
    this.props = m, this.context = D, this.refs = B, this.updater = H || W;
  }
  J.prototype.isReactComponent = {}, J.prototype.setState = function(m, D) {
    if (typeof m != "object" && typeof m != "function" && m != null)
      throw Error(
        "takes an object of state variables to update or a function which returns an object of state variables."
      );
    this.updater.enqueueSetState(this, m, D, "setState");
  }, J.prototype.forceUpdate = function(m) {
    this.updater.enqueueForceUpdate(this, m, "forceUpdate");
  };
  function Et() {
  }
  Et.prototype = J.prototype;
  function tt(m, D, H) {
    this.props = m, this.context = D, this.refs = B, this.updater = H || W;
  }
  var vt = tt.prototype = new Et();
  vt.constructor = tt, w(vt, J.prototype), vt.isPureReactComponent = !0;
  var Kt = Array.isArray;
  function Rt() {
  }
  var at = { H: null, A: null, T: null, S: null }, bt = Object.prototype.hasOwnProperty;
  function pe(m, D, H) {
    var Y = H.ref;
    return {
      $$typeof: c,
      type: m,
      key: D,
      ref: Y !== void 0 ? Y : null,
      props: H
    };
  }
  function Sl(m, D) {
    return pe(m.type, D, m.props);
  }
  function qt(m) {
    return typeof m == "object" && m !== null && m.$$typeof === c;
  }
  function wt(m) {
    var D = { "=": "=0", ":": "=2" };
    return "$" + m.replace(/[=:]/g, function(H) {
      return D[H];
    });
  }
  var Ie = /\/+/g;
  function Le(m, D) {
    return typeof m == "object" && m !== null && m.key != null ? wt("" + m.key) : D.toString(36);
  }
  function Yt(m) {
    switch (m.status) {
      case "fulfilled":
        return m.value;
      case "rejected":
        throw m.reason;
      default:
        switch (typeof m.status == "string" ? m.then(Rt, Rt) : (m.status = "pending", m.then(
          function(D) {
            m.status === "pending" && (m.status = "fulfilled", m.value = D);
          },
          function(D) {
            m.status === "pending" && (m.status = "rejected", m.reason = D);
          }
        )), m.status) {
          case "fulfilled":
            return m.value;
          case "rejected":
            throw m.reason;
        }
    }
    throw m;
  }
  function z(m, D, H, Y, F) {
    var nt = typeof m;
    (nt === "undefined" || nt === "boolean") && (m = null);
    var yt = !1;
    if (m === null) yt = !0;
    else
      switch (nt) {
        case "bigint":
        case "string":
        case "number":
          yt = !0;
          break;
        case "object":
          switch (m.$$typeof) {
            case c:
            case f:
              yt = !0;
              break;
            case C:
              return yt = m._init, z(
                yt(m._payload),
                D,
                H,
                Y,
                F
              );
          }
      }
    if (yt)
      return F = F(m), yt = Y === "" ? "." + Le(m, 0) : Y, Kt(F) ? (H = "", yt != null && (H = yt.replace(Ie, "$&/") + "/"), z(F, D, H, "", function(Fl) {
        return Fl;
      })) : F != null && (qt(F) && (F = Sl(
        F,
        H + (F.key == null || m && m.key === F.key ? "" : ("" + F.key).replace(
          Ie,
          "$&/"
        ) + "/") + yt
      )), D.push(F)), 1;
    yt = 0;
    var kt = Y === "" ? "." : Y + ":";
    if (Kt(m))
      for (var gt = 0; gt < m.length; gt++)
        Y = m[gt], nt = kt + Le(Y, gt), yt += z(
          Y,
          D,
          H,
          nt,
          F
        );
    else if (gt = Q(m), typeof gt == "function")
      for (m = gt.call(m), gt = 0; !(Y = m.next()).done; )
        Y = Y.value, nt = kt + Le(Y, gt++), yt += z(
          Y,
          D,
          H,
          nt,
          F
        );
    else if (nt === "object") {
      if (typeof m.then == "function")
        return z(
          Yt(m),
          D,
          H,
          Y,
          F
        );
      throw D = String(m), Error(
        "Objects are not valid as a React child (found: " + (D === "[object Object]" ? "object with keys {" + Object.keys(m).join(", ") + "}" : D) + "). If you meant to render a collection of children, use an array instead."
      );
    }
    return yt;
  }
  function R(m, D, H) {
    if (m == null) return m;
    var Y = [], F = 0;
    return z(m, Y, "", "", function(nt) {
      return D.call(H, nt, F++);
    }), Y;
  }
  function $(m) {
    if (m._status === -1) {
      var D = m._result;
      D = D(), D.then(
        function(H) {
          (m._status === 0 || m._status === -1) && (m._status = 1, m._result = H);
        },
        function(H) {
          (m._status === 0 || m._status === -1) && (m._status = 2, m._result = H);
        }
      ), m._status === -1 && (m._status = 0, m._result = D);
    }
    if (m._status === 1) return m._result.default;
    throw m._result;
  }
  var Z = typeof reportError == "function" ? reportError : function(m) {
    if (typeof window == "object" && typeof window.ErrorEvent == "function") {
      var D = new window.ErrorEvent("error", {
        bubbles: !0,
        cancelable: !0,
        message: typeof m == "object" && m !== null && typeof m.message == "string" ? String(m.message) : String(m),
        error: m
      });
      if (!window.dispatchEvent(D)) return;
    } else if (typeof process == "object" && typeof process.emit == "function") {
      process.emit("uncaughtException", m);
      return;
    }
    console.error(m);
  }, dt = {
    map: R,
    forEach: function(m, D, H) {
      R(
        m,
        function() {
          D.apply(this, arguments);
        },
        H
      );
    },
    count: function(m) {
      var D = 0;
      return R(m, function() {
        D++;
      }), D;
    },
    toArray: function(m) {
      return R(m, function(D) {
        return D;
      }) || [];
    },
    only: function(m) {
      if (!qt(m))
        throw Error(
          "React.Children.only expected to receive a single React element child."
        );
      return m;
    }
  };
  return I.Activity = b, I.Children = dt, I.Component = J, I.Fragment = r, I.Profiler = h, I.PureComponent = tt, I.StrictMode = s, I.Suspense = N, I.__CLIENT_INTERNALS_DO_NOT_USE_OR_WARN_USERS_THEY_CANNOT_UPGRADE = at, I.__COMPILER_RUNTIME = {
    __proto__: null,
    c: function(m) {
      return at.H.useMemoCache(m);
    }
  }, I.cache = function(m) {
    return function() {
      return m.apply(null, arguments);
    };
  }, I.cacheSignal = function() {
    return null;
  }, I.cloneElement = function(m, D, H) {
    if (m == null)
      throw Error(
        "The argument must be a React element, but you passed " + m + "."
      );
    var Y = w({}, m.props), F = m.key;
    if (D != null)
      for (nt in D.key !== void 0 && (F = "" + D.key), D)
        !bt.call(D, nt) || nt === "key" || nt === "__self" || nt === "__source" || nt === "ref" && D.ref === void 0 || (Y[nt] = D[nt]);
    var nt = arguments.length - 2;
    if (nt === 1) Y.children = H;
    else if (1 < nt) {
      for (var yt = Array(nt), kt = 0; kt < nt; kt++)
        yt[kt] = arguments[kt + 2];
      Y.children = yt;
    }
    return pe(m.type, F, Y);
  }, I.createContext = function(m) {
    return m = {
      $$typeof: q,
      _currentValue: m,
      _currentValue2: m,
      _threadCount: 0,
      Provider: null,
      Consumer: null
    }, m.Provider = m, m.Consumer = {
      $$typeof: g,
      _context: m
    }, m;
  }, I.createElement = function(m, D, H) {
    var Y, F = {}, nt = null;
    if (D != null)
      for (Y in D.key !== void 0 && (nt = "" + D.key), D)
        bt.call(D, Y) && Y !== "key" && Y !== "__self" && Y !== "__source" && (F[Y] = D[Y]);
    var yt = arguments.length - 2;
    if (yt === 1) F.children = H;
    else if (1 < yt) {
      for (var kt = Array(yt), gt = 0; gt < yt; gt++)
        kt[gt] = arguments[gt + 2];
      F.children = kt;
    }
    if (m && m.defaultProps)
      for (Y in yt = m.defaultProps, yt)
        F[Y] === void 0 && (F[Y] = yt[Y]);
    return pe(m, nt, F);
  }, I.createRef = function() {
    return { current: null };
  }, I.forwardRef = function(m) {
    return { $$typeof: U, render: m };
  }, I.isValidElement = qt, I.lazy = function(m) {
    return {
      $$typeof: C,
      _payload: { _status: -1, _result: m },
      _init: $
    };
  }, I.memo = function(m, D) {
    return {
      $$typeof: p,
      type: m,
      compare: D === void 0 ? null : D
    };
  }, I.startTransition = function(m) {
    var D = at.T, H = {};
    at.T = H;
    try {
      var Y = m(), F = at.S;
      F !== null && F(H, Y), typeof Y == "object" && Y !== null && typeof Y.then == "function" && Y.then(Rt, Z);
    } catch (nt) {
      Z(nt);
    } finally {
      D !== null && H.types !== null && (D.types = H.types), at.T = D;
    }
  }, I.unstable_useCacheRefresh = function() {
    return at.H.useCacheRefresh();
  }, I.use = function(m) {
    return at.H.use(m);
  }, I.useActionState = function(m, D, H) {
    return at.H.useActionState(m, D, H);
  }, I.useCallback = function(m, D) {
    return at.H.useCallback(m, D);
  }, I.useContext = function(m) {
    return at.H.useContext(m);
  }, I.useDebugValue = function() {
  }, I.useDeferredValue = function(m, D) {
    return at.H.useDeferredValue(m, D);
  }, I.useEffect = function(m, D) {
    return at.H.useEffect(m, D);
  }, I.useEffectEvent = function(m) {
    return at.H.useEffectEvent(m);
  }, I.useId = function() {
    return at.H.useId();
  }, I.useImperativeHandle = function(m, D, H) {
    return at.H.useImperativeHandle(m, D, H);
  }, I.useInsertionEffect = function(m, D) {
    return at.H.useInsertionEffect(m, D);
  }, I.useLayoutEffect = function(m, D) {
    return at.H.useLayoutEffect(m, D);
  }, I.useMemo = function(m, D) {
    return at.H.useMemo(m, D);
  }, I.useOptimistic = function(m, D) {
    return at.H.useOptimistic(m, D);
  }, I.useReducer = function(m, D, H) {
    return at.H.useReducer(m, D, H);
  }, I.useRef = function(m) {
    return at.H.useRef(m);
  }, I.useState = function(m) {
    return at.H.useState(m);
  }, I.useSyncExternalStore = function(m, D, H) {
    return at.H.useSyncExternalStore(
      m,
      D,
      H
    );
  }, I.useTransition = function() {
    return at.H.useTransition();
  }, I.version = "19.2.5", I;
}
var my;
function ys() {
  return my || (my = 1, If.exports = Kh()), If.exports;
}
var k = ys(), Pf = { exports: {} }, ou = {}, ts = { exports: {} }, es = {};
/**
 * @license React
 * scheduler.production.js
 *
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 */
var hy;
function kh() {
  return hy || (hy = 1, (function(c) {
    function f(z, R) {
      var $ = z.length;
      z.push(R);
      t: for (; 0 < $; ) {
        var Z = $ - 1 >>> 1, dt = z[Z];
        if (0 < h(dt, R))
          z[Z] = R, z[$] = dt, $ = Z;
        else break t;
      }
    }
    function r(z) {
      return z.length === 0 ? null : z[0];
    }
    function s(z) {
      if (z.length === 0) return null;
      var R = z[0], $ = z.pop();
      if ($ !== R) {
        z[0] = $;
        t: for (var Z = 0, dt = z.length, m = dt >>> 1; Z < m; ) {
          var D = 2 * (Z + 1) - 1, H = z[D], Y = D + 1, F = z[Y];
          if (0 > h(H, $))
            Y < dt && 0 > h(F, H) ? (z[Z] = F, z[Y] = $, Z = Y) : (z[Z] = H, z[D] = $, Z = D);
          else if (Y < dt && 0 > h(F, $))
            z[Z] = F, z[Y] = $, Z = Y;
          else break t;
        }
      }
      return R;
    }
    function h(z, R) {
      var $ = z.sortIndex - R.sortIndex;
      return $ !== 0 ? $ : z.id - R.id;
    }
    if (c.unstable_now = void 0, typeof performance == "object" && typeof performance.now == "function") {
      var g = performance;
      c.unstable_now = function() {
        return g.now();
      };
    } else {
      var q = Date, U = q.now();
      c.unstable_now = function() {
        return q.now() - U;
      };
    }
    var N = [], p = [], C = 1, b = null, j = 3, Q = !1, W = !1, w = !1, B = !1, J = typeof setTimeout == "function" ? setTimeout : null, Et = typeof clearTimeout == "function" ? clearTimeout : null, tt = typeof setImmediate < "u" ? setImmediate : null;
    function vt(z) {
      for (var R = r(p); R !== null; ) {
        if (R.callback === null) s(p);
        else if (R.startTime <= z)
          s(p), R.sortIndex = R.expirationTime, f(N, R);
        else break;
        R = r(p);
      }
    }
    function Kt(z) {
      if (w = !1, vt(z), !W)
        if (r(N) !== null)
          W = !0, Rt || (Rt = !0, wt());
        else {
          var R = r(p);
          R !== null && Yt(Kt, R.startTime - z);
        }
    }
    var Rt = !1, at = -1, bt = 5, pe = -1;
    function Sl() {
      return B ? !0 : !(c.unstable_now() - pe < bt);
    }
    function qt() {
      if (B = !1, Rt) {
        var z = c.unstable_now();
        pe = z;
        var R = !0;
        try {
          t: {
            W = !1, w && (w = !1, Et(at), at = -1), Q = !0;
            var $ = j;
            try {
              e: {
                for (vt(z), b = r(N); b !== null && !(b.expirationTime > z && Sl()); ) {
                  var Z = b.callback;
                  if (typeof Z == "function") {
                    b.callback = null, j = b.priorityLevel;
                    var dt = Z(
                      b.expirationTime <= z
                    );
                    if (z = c.unstable_now(), typeof dt == "function") {
                      b.callback = dt, vt(z), R = !0;
                      break e;
                    }
                    b === r(N) && s(N), vt(z);
                  } else s(N);
                  b = r(N);
                }
                if (b !== null) R = !0;
                else {
                  var m = r(p);
                  m !== null && Yt(
                    Kt,
                    m.startTime - z
                  ), R = !1;
                }
              }
              break t;
            } finally {
              b = null, j = $, Q = !1;
            }
            R = void 0;
          }
        } finally {
          R ? wt() : Rt = !1;
        }
      }
    }
    var wt;
    if (typeof tt == "function")
      wt = function() {
        tt(qt);
      };
    else if (typeof MessageChannel < "u") {
      var Ie = new MessageChannel(), Le = Ie.port2;
      Ie.port1.onmessage = qt, wt = function() {
        Le.postMessage(null);
      };
    } else
      wt = function() {
        J(qt, 0);
      };
    function Yt(z, R) {
      at = J(function() {
        z(c.unstable_now());
      }, R);
    }
    c.unstable_IdlePriority = 5, c.unstable_ImmediatePriority = 1, c.unstable_LowPriority = 4, c.unstable_NormalPriority = 3, c.unstable_Profiling = null, c.unstable_UserBlockingPriority = 2, c.unstable_cancelCallback = function(z) {
      z.callback = null;
    }, c.unstable_forceFrameRate = function(z) {
      0 > z || 125 < z ? console.error(
        "forceFrameRate takes a positive int between 0 and 125, forcing frame rates higher than 125 fps is not supported"
      ) : bt = 0 < z ? Math.floor(1e3 / z) : 5;
    }, c.unstable_getCurrentPriorityLevel = function() {
      return j;
    }, c.unstable_next = function(z) {
      switch (j) {
        case 1:
        case 2:
        case 3:
          var R = 3;
          break;
        default:
          R = j;
      }
      var $ = j;
      j = R;
      try {
        return z();
      } finally {
        j = $;
      }
    }, c.unstable_requestPaint = function() {
      B = !0;
    }, c.unstable_runWithPriority = function(z, R) {
      switch (z) {
        case 1:
        case 2:
        case 3:
        case 4:
        case 5:
          break;
        default:
          z = 3;
      }
      var $ = j;
      j = z;
      try {
        return R();
      } finally {
        j = $;
      }
    }, c.unstable_scheduleCallback = function(z, R, $) {
      var Z = c.unstable_now();
      switch (typeof $ == "object" && $ !== null ? ($ = $.delay, $ = typeof $ == "number" && 0 < $ ? Z + $ : Z) : $ = Z, z) {
        case 1:
          var dt = -1;
          break;
        case 2:
          dt = 250;
          break;
        case 5:
          dt = 1073741823;
          break;
        case 4:
          dt = 1e4;
          break;
        default:
          dt = 5e3;
      }
      return dt = $ + dt, z = {
        id: C++,
        callback: R,
        priorityLevel: z,
        startTime: $,
        expirationTime: dt,
        sortIndex: -1
      }, $ > Z ? (z.sortIndex = $, f(p, z), r(N) === null && z === r(p) && (w ? (Et(at), at = -1) : w = !0, Yt(Kt, $ - Z))) : (z.sortIndex = dt, f(N, z), W || Q || (W = !0, Rt || (Rt = !0, wt()))), z;
    }, c.unstable_shouldYield = Sl, c.unstable_wrapCallback = function(z) {
      var R = j;
      return function() {
        var $ = j;
        j = R;
        try {
          return z.apply(this, arguments);
        } finally {
          j = $;
        }
      };
    };
  })(es)), es;
}
var vy;
function $h() {
  return vy || (vy = 1, ts.exports = kh()), ts.exports;
}
var ls = { exports: {} }, ie = {};
/**
 * @license React
 * react-dom.production.js
 *
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 */
var gy;
function Wh() {
  if (gy) return ie;
  gy = 1;
  var c = ys();
  function f(N) {
    var p = "https://react.dev/errors/" + N;
    if (1 < arguments.length) {
      p += "?args[]=" + encodeURIComponent(arguments[1]);
      for (var C = 2; C < arguments.length; C++)
        p += "&args[]=" + encodeURIComponent(arguments[C]);
    }
    return "Minified React error #" + N + "; visit " + p + " for the full message or use the non-minified dev environment for full errors and additional helpful warnings.";
  }
  function r() {
  }
  var s = {
    d: {
      f: r,
      r: function() {
        throw Error(f(522));
      },
      D: r,
      C: r,
      L: r,
      m: r,
      X: r,
      S: r,
      M: r
    },
    p: 0,
    findDOMNode: null
  }, h = Symbol.for("react.portal");
  function g(N, p, C) {
    var b = 3 < arguments.length && arguments[3] !== void 0 ? arguments[3] : null;
    return {
      $$typeof: h,
      key: b == null ? null : "" + b,
      children: N,
      containerInfo: p,
      implementation: C
    };
  }
  var q = c.__CLIENT_INTERNALS_DO_NOT_USE_OR_WARN_USERS_THEY_CANNOT_UPGRADE;
  function U(N, p) {
    if (N === "font") return "";
    if (typeof p == "string")
      return p === "use-credentials" ? p : "";
  }
  return ie.__DOM_INTERNALS_DO_NOT_USE_OR_WARN_USERS_THEY_CANNOT_UPGRADE = s, ie.createPortal = function(N, p) {
    var C = 2 < arguments.length && arguments[2] !== void 0 ? arguments[2] : null;
    if (!p || p.nodeType !== 1 && p.nodeType !== 9 && p.nodeType !== 11)
      throw Error(f(299));
    return g(N, p, null, C);
  }, ie.flushSync = function(N) {
    var p = q.T, C = s.p;
    try {
      if (q.T = null, s.p = 2, N) return N();
    } finally {
      q.T = p, s.p = C, s.d.f();
    }
  }, ie.preconnect = function(N, p) {
    typeof N == "string" && (p ? (p = p.crossOrigin, p = typeof p == "string" ? p === "use-credentials" ? p : "" : void 0) : p = null, s.d.C(N, p));
  }, ie.prefetchDNS = function(N) {
    typeof N == "string" && s.d.D(N);
  }, ie.preinit = function(N, p) {
    if (typeof N == "string" && p && typeof p.as == "string") {
      var C = p.as, b = U(C, p.crossOrigin), j = typeof p.integrity == "string" ? p.integrity : void 0, Q = typeof p.fetchPriority == "string" ? p.fetchPriority : void 0;
      C === "style" ? s.d.S(
        N,
        typeof p.precedence == "string" ? p.precedence : void 0,
        {
          crossOrigin: b,
          integrity: j,
          fetchPriority: Q
        }
      ) : C === "script" && s.d.X(N, {
        crossOrigin: b,
        integrity: j,
        fetchPriority: Q,
        nonce: typeof p.nonce == "string" ? p.nonce : void 0
      });
    }
  }, ie.preinitModule = function(N, p) {
    if (typeof N == "string")
      if (typeof p == "object" && p !== null) {
        if (p.as == null || p.as === "script") {
          var C = U(
            p.as,
            p.crossOrigin
          );
          s.d.M(N, {
            crossOrigin: C,
            integrity: typeof p.integrity == "string" ? p.integrity : void 0,
            nonce: typeof p.nonce == "string" ? p.nonce : void 0
          });
        }
      } else p == null && s.d.M(N);
  }, ie.preload = function(N, p) {
    if (typeof N == "string" && typeof p == "object" && p !== null && typeof p.as == "string") {
      var C = p.as, b = U(C, p.crossOrigin);
      s.d.L(N, C, {
        crossOrigin: b,
        integrity: typeof p.integrity == "string" ? p.integrity : void 0,
        nonce: typeof p.nonce == "string" ? p.nonce : void 0,
        type: typeof p.type == "string" ? p.type : void 0,
        fetchPriority: typeof p.fetchPriority == "string" ? p.fetchPriority : void 0,
        referrerPolicy: typeof p.referrerPolicy == "string" ? p.referrerPolicy : void 0,
        imageSrcSet: typeof p.imageSrcSet == "string" ? p.imageSrcSet : void 0,
        imageSizes: typeof p.imageSizes == "string" ? p.imageSizes : void 0,
        media: typeof p.media == "string" ? p.media : void 0
      });
    }
  }, ie.preloadModule = function(N, p) {
    if (typeof N == "string")
      if (p) {
        var C = U(p.as, p.crossOrigin);
        s.d.m(N, {
          as: typeof p.as == "string" && p.as !== "script" ? p.as : void 0,
          crossOrigin: C,
          integrity: typeof p.integrity == "string" ? p.integrity : void 0
        });
      } else s.d.m(N);
  }, ie.requestFormReset = function(N) {
    s.d.r(N);
  }, ie.unstable_batchedUpdates = function(N, p) {
    return N(p);
  }, ie.useFormState = function(N, p, C) {
    return q.H.useFormState(N, p, C);
  }, ie.useFormStatus = function() {
    return q.H.useHostTransitionStatus();
  }, ie.version = "19.2.5", ie;
}
var py;
function Fh() {
  if (py) return ls.exports;
  py = 1;
  function c() {
    if (!(typeof __REACT_DEVTOOLS_GLOBAL_HOOK__ > "u" || typeof __REACT_DEVTOOLS_GLOBAL_HOOK__.checkDCE != "function"))
      try {
        __REACT_DEVTOOLS_GLOBAL_HOOK__.checkDCE(c);
      } catch (f) {
        console.error(f);
      }
  }
  return c(), ls.exports = Wh(), ls.exports;
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
var by;
function Ih() {
  if (by) return ou;
  by = 1;
  var c = $h(), f = ys(), r = Fh();
  function s(t) {
    var e = "https://react.dev/errors/" + t;
    if (1 < arguments.length) {
      e += "?args[]=" + encodeURIComponent(arguments[1]);
      for (var l = 2; l < arguments.length; l++)
        e += "&args[]=" + encodeURIComponent(arguments[l]);
    }
    return "Minified React error #" + t + "; visit " + e + " for the full message or use the non-minified dev environment for full errors and additional helpful warnings.";
  }
  function h(t) {
    return !(!t || t.nodeType !== 1 && t.nodeType !== 9 && t.nodeType !== 11);
  }
  function g(t) {
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
  function q(t) {
    if (t.tag === 13) {
      var e = t.memoizedState;
      if (e === null && (t = t.alternate, t !== null && (e = t.memoizedState)), e !== null) return e.dehydrated;
    }
    return null;
  }
  function U(t) {
    if (t.tag === 31) {
      var e = t.memoizedState;
      if (e === null && (t = t.alternate, t !== null && (e = t.memoizedState)), e !== null) return e.dehydrated;
    }
    return null;
  }
  function N(t) {
    if (g(t) !== t)
      throw Error(s(188));
  }
  function p(t) {
    var e = t.alternate;
    if (!e) {
      if (e = g(t), e === null) throw Error(s(188));
      return e !== t ? null : t;
    }
    for (var l = t, a = e; ; ) {
      var n = l.return;
      if (n === null) break;
      var u = n.alternate;
      if (u === null) {
        if (a = n.return, a !== null) {
          l = a;
          continue;
        }
        break;
      }
      if (n.child === u.child) {
        for (u = n.child; u; ) {
          if (u === l) return N(n), t;
          if (u === a) return N(n), e;
          u = u.sibling;
        }
        throw Error(s(188));
      }
      if (l.return !== a.return) l = n, a = u;
      else {
        for (var i = !1, o = n.child; o; ) {
          if (o === l) {
            i = !0, l = n, a = u;
            break;
          }
          if (o === a) {
            i = !0, a = n, l = u;
            break;
          }
          o = o.sibling;
        }
        if (!i) {
          for (o = u.child; o; ) {
            if (o === l) {
              i = !0, l = u, a = n;
              break;
            }
            if (o === a) {
              i = !0, a = u, l = n;
              break;
            }
            o = o.sibling;
          }
          if (!i) throw Error(s(189));
        }
      }
      if (l.alternate !== a) throw Error(s(190));
    }
    if (l.tag !== 3) throw Error(s(188));
    return l.stateNode.current === l ? t : e;
  }
  function C(t) {
    var e = t.tag;
    if (e === 5 || e === 26 || e === 27 || e === 6) return t;
    for (t = t.child; t !== null; ) {
      if (e = C(t), e !== null) return e;
      t = t.sibling;
    }
    return null;
  }
  var b = Object.assign, j = Symbol.for("react.element"), Q = Symbol.for("react.transitional.element"), W = Symbol.for("react.portal"), w = Symbol.for("react.fragment"), B = Symbol.for("react.strict_mode"), J = Symbol.for("react.profiler"), Et = Symbol.for("react.consumer"), tt = Symbol.for("react.context"), vt = Symbol.for("react.forward_ref"), Kt = Symbol.for("react.suspense"), Rt = Symbol.for("react.suspense_list"), at = Symbol.for("react.memo"), bt = Symbol.for("react.lazy"), pe = Symbol.for("react.activity"), Sl = Symbol.for("react.memo_cache_sentinel"), qt = Symbol.iterator;
  function wt(t) {
    return t === null || typeof t != "object" ? null : (t = qt && t[qt] || t["@@iterator"], typeof t == "function" ? t : null);
  }
  var Ie = Symbol.for("react.client.reference");
  function Le(t) {
    if (t == null) return null;
    if (typeof t == "function")
      return t.$$typeof === Ie ? null : t.displayName || t.name || null;
    if (typeof t == "string") return t;
    switch (t) {
      case w:
        return "Fragment";
      case J:
        return "Profiler";
      case B:
        return "StrictMode";
      case Kt:
        return "Suspense";
      case Rt:
        return "SuspenseList";
      case pe:
        return "Activity";
    }
    if (typeof t == "object")
      switch (t.$$typeof) {
        case W:
          return "Portal";
        case tt:
          return t.displayName || "Context";
        case Et:
          return (t._context.displayName || "Context") + ".Consumer";
        case vt:
          var e = t.render;
          return t = t.displayName, t || (t = e.displayName || e.name || "", t = t !== "" ? "ForwardRef(" + t + ")" : "ForwardRef"), t;
        case at:
          return e = t.displayName || null, e !== null ? e : Le(t.type) || "Memo";
        case bt:
          e = t._payload, t = t._init;
          try {
            return Le(t(e));
          } catch {
          }
      }
    return null;
  }
  var Yt = Array.isArray, z = f.__CLIENT_INTERNALS_DO_NOT_USE_OR_WARN_USERS_THEY_CANNOT_UPGRADE, R = r.__DOM_INTERNALS_DO_NOT_USE_OR_WARN_USERS_THEY_CANNOT_UPGRADE, $ = {
    pending: !1,
    data: null,
    method: null,
    action: null
  }, Z = [], dt = -1;
  function m(t) {
    return { current: t };
  }
  function D(t) {
    0 > dt || (t.current = Z[dt], Z[dt] = null, dt--);
  }
  function H(t, e) {
    dt++, Z[dt] = t.current, t.current = e;
  }
  var Y = m(null), F = m(null), nt = m(null), yt = m(null);
  function kt(t, e) {
    switch (H(nt, e), H(F, t), H(Y, null), e.nodeType) {
      case 9:
      case 11:
        t = (t = e.documentElement) && (t = t.namespaceURI) ? Rd(t) : 0;
        break;
      default:
        if (t = e.tagName, e = e.namespaceURI)
          e = Rd(e), t = Hd(e, t);
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
    D(Y), H(Y, t);
  }
  function gt() {
    D(Y), D(F), D(nt);
  }
  function Fl(t) {
    t.memoizedState !== null && H(yt, t);
    var e = Y.current, l = Hd(e, t.type);
    e !== l && (H(F, t), H(Y, l));
  }
  function Il(t) {
    F.current === t && (D(Y), D(F)), yt.current === t && (D(yt), cu._currentValue = $);
  }
  var gn, Ve;
  function Nt(t) {
    if (gn === void 0)
      try {
        throw Error();
      } catch (l) {
        var e = l.stack.trim().match(/\n( *(at )?)/);
        gn = e && e[1] || "", Ve = -1 < l.stack.indexOf(`
    at`) ? " (<anonymous>)" : -1 < l.stack.indexOf("@") ? "@unknown:0:0" : "";
      }
    return `
` + gn + t + Ve;
  }
  var Aa = !1;
  function _l(t, e) {
    if (!t || Aa) return "";
    Aa = !0;
    var l = Error.prepareStackTrace;
    Error.prepareStackTrace = void 0;
    try {
      var a = {
        DetermineComponentFrameRoot: function() {
          try {
            if (e) {
              var M = function() {
                throw Error();
              };
              if (Object.defineProperty(M.prototype, "props", {
                set: function() {
                  throw Error();
                }
              }), typeof Reflect == "object" && Reflect.construct) {
                try {
                  Reflect.construct(M, []);
                } catch (A) {
                  var E = A;
                }
                Reflect.construct(t, [], M);
              } else {
                try {
                  M.call();
                } catch (A) {
                  E = A;
                }
                t.call(M.prototype);
              }
            } else {
              try {
                throw Error();
              } catch (A) {
                E = A;
              }
              (M = t()) && typeof M.catch == "function" && M.catch(function() {
              });
            }
          } catch (A) {
            if (A && E && typeof A.stack == "string")
              return [A.stack, E.stack];
          }
          return [null, null];
        }
      };
      a.DetermineComponentFrameRoot.displayName = "DetermineComponentFrameRoot";
      var n = Object.getOwnPropertyDescriptor(
        a.DetermineComponentFrameRoot,
        "name"
      );
      n && n.configurable && Object.defineProperty(
        a.DetermineComponentFrameRoot,
        "name",
        { value: "DetermineComponentFrameRoot" }
      );
      var u = a.DetermineComponentFrameRoot(), i = u[0], o = u[1];
      if (i && o) {
        var d = i.split(`
`), _ = o.split(`
`);
        for (n = a = 0; a < d.length && !d[a].includes("DetermineComponentFrameRoot"); )
          a++;
        for (; n < _.length && !_[n].includes(
          "DetermineComponentFrameRoot"
        ); )
          n++;
        if (a === d.length || n === _.length)
          for (a = d.length - 1, n = _.length - 1; 1 <= a && 0 <= n && d[a] !== _[n]; )
            n--;
        for (; 1 <= a && 0 <= n; a--, n--)
          if (d[a] !== _[n]) {
            if (a !== 1 || n !== 1)
              do
                if (a--, n--, 0 > n || d[a] !== _[n]) {
                  var T = `
` + d[a].replace(" at new ", " at ");
                  return t.displayName && T.includes("<anonymous>") && (T = T.replace("<anonymous>", t.displayName)), T;
                }
              while (1 <= a && 0 <= n);
            break;
          }
      }
    } finally {
      Aa = !1, Error.prepareStackTrace = l;
    }
    return (l = t ? t.displayName || t.name : "") ? Nt(l) : "";
  }
  function xa(t, e) {
    switch (t.tag) {
      case 26:
      case 27:
      case 5:
        return Nt(t.type);
      case 16:
        return Nt("Lazy");
      case 13:
        return t.child !== e && e !== null ? Nt("Suspense Fallback") : Nt("Suspense");
      case 19:
        return Nt("SuspenseList");
      case 0:
      case 15:
        return _l(t.type, !1);
      case 11:
        return _l(t.type.render, !1);
      case 1:
        return _l(t.type, !0);
      case 31:
        return Nt("Activity");
      default:
        return "";
    }
  }
  function za(t) {
    try {
      var e = "", l = null;
      do
        e += xa(t, l), l = t, t = t.return;
      while (t);
      return e;
    } catch (a) {
      return `
Error generating stack: ` + a.message + `
` + a.stack;
    }
  }
  var we = Object.prototype.hasOwnProperty, pn = c.unstable_scheduleCallback, Ta = c.unstable_cancelCallback, Pl = c.unstable_shouldYield, Hi = c.unstable_requestPaint, It = c.unstable_now, vu = c.unstable_getCurrentPriorityLevel, gu = c.unstable_ImmediatePriority, bn = c.unstable_UserBlockingPriority, El = c.unstable_NormalPriority, ta = c.unstable_LowPriority, pu = c.unstable_IdlePriority, Bi = c.log, bu = c.unstable_setDisableYieldValue, Je = null, fe = null;
  function L(t) {
    if (typeof Bi == "function" && bu(t), fe && typeof fe.setStrictMode == "function")
      try {
        fe.setStrictMode(Je, t);
      } catch {
      }
  }
  var et = Math.clz32 ? Math.clz32 : ue, Gt = Math.log, $t = Math.LN2;
  function ue(t) {
    return t >>>= 0, t === 0 ? 32 : 31 - (Gt(t) / $t | 0) | 0;
  }
  var ea = 256, Su = 262144, _u = 4194304;
  function la(t) {
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
  function Eu(t, e, l) {
    var a = t.pendingLanes;
    if (a === 0) return 0;
    var n = 0, u = t.suspendedLanes, i = t.pingedLanes;
    t = t.warmLanes;
    var o = a & 134217727;
    return o !== 0 ? (a = o & ~u, a !== 0 ? n = la(a) : (i &= o, i !== 0 ? n = la(i) : l || (l = o & ~t, l !== 0 && (n = la(l))))) : (o = a & ~u, o !== 0 ? n = la(o) : i !== 0 ? n = la(i) : l || (l = a & ~t, l !== 0 && (n = la(l)))), n === 0 ? 0 : e !== 0 && e !== n && (e & u) === 0 && (u = n & -n, l = e & -e, u >= l || u === 32 && (l & 4194048) !== 0) ? e : n;
  }
  function Sn(t, e) {
    return (t.pendingLanes & ~(t.suspendedLanes & ~t.pingedLanes) & e) === 0;
  }
  function Uy(t, e) {
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
  function hs() {
    var t = _u;
    return _u <<= 1, (_u & 62914560) === 0 && (_u = 4194304), t;
  }
  function Li(t) {
    for (var e = [], l = 0; 31 > l; l++) e.push(t);
    return e;
  }
  function _n(t, e) {
    t.pendingLanes |= e, e !== 268435456 && (t.suspendedLanes = 0, t.pingedLanes = 0, t.warmLanes = 0);
  }
  function jy(t, e, l, a, n, u) {
    var i = t.pendingLanes;
    t.pendingLanes = l, t.suspendedLanes = 0, t.pingedLanes = 0, t.warmLanes = 0, t.expiredLanes &= l, t.entangledLanes &= l, t.errorRecoveryDisabledLanes &= l, t.shellSuspendCounter = 0;
    var o = t.entanglements, d = t.expirationTimes, _ = t.hiddenUpdates;
    for (l = i & ~l; 0 < l; ) {
      var T = 31 - et(l), M = 1 << T;
      o[T] = 0, d[T] = -1;
      var E = _[T];
      if (E !== null)
        for (_[T] = null, T = 0; T < E.length; T++) {
          var A = E[T];
          A !== null && (A.lane &= -536870913);
        }
      l &= ~M;
    }
    a !== 0 && vs(t, a, 0), u !== 0 && n === 0 && t.tag !== 0 && (t.suspendedLanes |= u & ~(i & ~e));
  }
  function vs(t, e, l) {
    t.pendingLanes |= e, t.suspendedLanes &= ~e;
    var a = 31 - et(e);
    t.entangledLanes |= e, t.entanglements[a] = t.entanglements[a] | 1073741824 | l & 261930;
  }
  function gs(t, e) {
    var l = t.entangledLanes |= e;
    for (t = t.entanglements; l; ) {
      var a = 31 - et(l), n = 1 << a;
      n & e | t[a] & e && (t[a] |= e), l &= ~n;
    }
  }
  function ps(t, e) {
    var l = e & -e;
    return l = (l & 42) !== 0 ? 1 : Yi(l), (l & (t.suspendedLanes | e)) !== 0 ? 0 : l;
  }
  function Yi(t) {
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
  function Gi(t) {
    return t &= -t, 2 < t ? 8 < t ? (t & 134217727) !== 0 ? 32 : 268435456 : 8 : 2;
  }
  function bs() {
    var t = R.p;
    return t !== 0 ? t : (t = window.event, t === void 0 ? 32 : uy(t.type));
  }
  function Ss(t, e) {
    var l = R.p;
    try {
      return R.p = t, e();
    } finally {
      R.p = l;
    }
  }
  var Al = Math.random().toString(36).slice(2), Pt = "__reactFiber$" + Al, re = "__reactProps$" + Al, qa = "__reactContainer$" + Al, Qi = "__reactEvents$" + Al, Cy = "__reactListeners$" + Al, Ry = "__reactHandles$" + Al, _s = "__reactResources$" + Al, En = "__reactMarker$" + Al;
  function Xi(t) {
    delete t[Pt], delete t[re], delete t[Qi], delete t[Cy], delete t[Ry];
  }
  function Na(t) {
    var e = t[Pt];
    if (e) return e;
    for (var l = t.parentNode; l; ) {
      if (e = l[qa] || l[Pt]) {
        if (l = e.alternate, e.child !== null || l !== null && l.child !== null)
          for (t = Zd(t); t !== null; ) {
            if (l = t[Pt]) return l;
            t = Zd(t);
          }
        return e;
      }
      t = l, l = t.parentNode;
    }
    return null;
  }
  function Oa(t) {
    if (t = t[Pt] || t[qa]) {
      var e = t.tag;
      if (e === 5 || e === 6 || e === 13 || e === 31 || e === 26 || e === 27 || e === 3)
        return t;
    }
    return null;
  }
  function An(t) {
    var e = t.tag;
    if (e === 5 || e === 26 || e === 27 || e === 6) return t.stateNode;
    throw Error(s(33));
  }
  function Ma(t) {
    var e = t[_s];
    return e || (e = t[_s] = { hoistableStyles: /* @__PURE__ */ new Map(), hoistableScripts: /* @__PURE__ */ new Map() }), e;
  }
  function Wt(t) {
    t[En] = !0;
  }
  var Es = /* @__PURE__ */ new Set(), As = {};
  function aa(t, e) {
    Da(t, e), Da(t + "Capture", e);
  }
  function Da(t, e) {
    for (As[t] = e, t = 0; t < e.length; t++)
      Es.add(e[t]);
  }
  var Hy = RegExp(
    "^[:A-Z_a-z\\u00C0-\\u00D6\\u00D8-\\u00F6\\u00F8-\\u02FF\\u0370-\\u037D\\u037F-\\u1FFF\\u200C-\\u200D\\u2070-\\u218F\\u2C00-\\u2FEF\\u3001-\\uD7FF\\uF900-\\uFDCF\\uFDF0-\\uFFFD][:A-Z_a-z\\u00C0-\\u00D6\\u00D8-\\u00F6\\u00F8-\\u02FF\\u0370-\\u037D\\u037F-\\u1FFF\\u200C-\\u200D\\u2070-\\u218F\\u2C00-\\u2FEF\\u3001-\\uD7FF\\uF900-\\uFDCF\\uFDF0-\\uFFFD\\-.0-9\\u00B7\\u0300-\\u036F\\u203F-\\u2040]*$"
  ), xs = {}, zs = {};
  function By(t) {
    return we.call(zs, t) ? !0 : we.call(xs, t) ? !1 : Hy.test(t) ? zs[t] = !0 : (xs[t] = !0, !1);
  }
  function Au(t, e, l) {
    if (By(e))
      if (l === null) t.removeAttribute(e);
      else {
        switch (typeof l) {
          case "undefined":
          case "function":
          case "symbol":
            t.removeAttribute(e);
            return;
          case "boolean":
            var a = e.toLowerCase().slice(0, 5);
            if (a !== "data-" && a !== "aria-") {
              t.removeAttribute(e);
              return;
            }
        }
        t.setAttribute(e, "" + l);
      }
  }
  function xu(t, e, l) {
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
  function Pe(t, e, l, a) {
    if (a === null) t.removeAttribute(l);
    else {
      switch (typeof a) {
        case "undefined":
        case "function":
        case "symbol":
        case "boolean":
          t.removeAttribute(l);
          return;
      }
      t.setAttributeNS(e, l, "" + a);
    }
  }
  function qe(t) {
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
  function Ts(t) {
    var e = t.type;
    return (t = t.nodeName) && t.toLowerCase() === "input" && (e === "checkbox" || e === "radio");
  }
  function Ly(t, e, l) {
    var a = Object.getOwnPropertyDescriptor(
      t.constructor.prototype,
      e
    );
    if (!t.hasOwnProperty(e) && typeof a < "u" && typeof a.get == "function" && typeof a.set == "function") {
      var n = a.get, u = a.set;
      return Object.defineProperty(t, e, {
        configurable: !0,
        get: function() {
          return n.call(this);
        },
        set: function(i) {
          l = "" + i, u.call(this, i);
        }
      }), Object.defineProperty(t, e, {
        enumerable: a.enumerable
      }), {
        getValue: function() {
          return l;
        },
        setValue: function(i) {
          l = "" + i;
        },
        stopTracking: function() {
          t._valueTracker = null, delete t[e];
        }
      };
    }
  }
  function Zi(t) {
    if (!t._valueTracker) {
      var e = Ts(t) ? "checked" : "value";
      t._valueTracker = Ly(
        t,
        e,
        "" + t[e]
      );
    }
  }
  function qs(t) {
    if (!t) return !1;
    var e = t._valueTracker;
    if (!e) return !0;
    var l = e.getValue(), a = "";
    return t && (a = Ts(t) ? t.checked ? "true" : "false" : t.value), t = a, t !== l ? (e.setValue(t), !0) : !1;
  }
  function zu(t) {
    if (t = t || (typeof document < "u" ? document : void 0), typeof t > "u") return null;
    try {
      return t.activeElement || t.body;
    } catch {
      return t.body;
    }
  }
  var Yy = /[\n"\\]/g;
  function Ne(t) {
    return t.replace(
      Yy,
      function(e) {
        return "\\" + e.charCodeAt(0).toString(16) + " ";
      }
    );
  }
  function Vi(t, e, l, a, n, u, i, o) {
    t.name = "", i != null && typeof i != "function" && typeof i != "symbol" && typeof i != "boolean" ? t.type = i : t.removeAttribute("type"), e != null ? i === "number" ? (e === 0 && t.value === "" || t.value != e) && (t.value = "" + qe(e)) : t.value !== "" + qe(e) && (t.value = "" + qe(e)) : i !== "submit" && i !== "reset" || t.removeAttribute("value"), e != null ? wi(t, i, qe(e)) : l != null ? wi(t, i, qe(l)) : a != null && t.removeAttribute("value"), n == null && u != null && (t.defaultChecked = !!u), n != null && (t.checked = n && typeof n != "function" && typeof n != "symbol"), o != null && typeof o != "function" && typeof o != "symbol" && typeof o != "boolean" ? t.name = "" + qe(o) : t.removeAttribute("name");
  }
  function Ns(t, e, l, a, n, u, i, o) {
    if (u != null && typeof u != "function" && typeof u != "symbol" && typeof u != "boolean" && (t.type = u), e != null || l != null) {
      if (!(u !== "submit" && u !== "reset" || e != null)) {
        Zi(t);
        return;
      }
      l = l != null ? "" + qe(l) : "", e = e != null ? "" + qe(e) : l, o || e === t.value || (t.value = e), t.defaultValue = e;
    }
    a = a ?? n, a = typeof a != "function" && typeof a != "symbol" && !!a, t.checked = o ? t.checked : !!a, t.defaultChecked = !!a, i != null && typeof i != "function" && typeof i != "symbol" && typeof i != "boolean" && (t.name = i), Zi(t);
  }
  function wi(t, e, l) {
    e === "number" && zu(t.ownerDocument) === t || t.defaultValue === "" + l || (t.defaultValue = "" + l);
  }
  function Ua(t, e, l, a) {
    if (t = t.options, e) {
      e = {};
      for (var n = 0; n < l.length; n++)
        e["$" + l[n]] = !0;
      for (l = 0; l < t.length; l++)
        n = e.hasOwnProperty("$" + t[l].value), t[l].selected !== n && (t[l].selected = n), n && a && (t[l].defaultSelected = !0);
    } else {
      for (l = "" + qe(l), e = null, n = 0; n < t.length; n++) {
        if (t[n].value === l) {
          t[n].selected = !0, a && (t[n].defaultSelected = !0);
          return;
        }
        e !== null || t[n].disabled || (e = t[n]);
      }
      e !== null && (e.selected = !0);
    }
  }
  function Os(t, e, l) {
    if (e != null && (e = "" + qe(e), e !== t.value && (t.value = e), l == null)) {
      t.defaultValue !== e && (t.defaultValue = e);
      return;
    }
    t.defaultValue = l != null ? "" + qe(l) : "";
  }
  function Ms(t, e, l, a) {
    if (e == null) {
      if (a != null) {
        if (l != null) throw Error(s(92));
        if (Yt(a)) {
          if (1 < a.length) throw Error(s(93));
          a = a[0];
        }
        l = a;
      }
      l == null && (l = ""), e = l;
    }
    l = qe(e), t.defaultValue = l, a = t.textContent, a === l && a !== "" && a !== null && (t.value = a), Zi(t);
  }
  function ja(t, e) {
    if (e) {
      var l = t.firstChild;
      if (l && l === t.lastChild && l.nodeType === 3) {
        l.nodeValue = e;
        return;
      }
    }
    t.textContent = e;
  }
  var Gy = new Set(
    "animationIterationCount aspectRatio borderImageOutset borderImageSlice borderImageWidth boxFlex boxFlexGroup boxOrdinalGroup columnCount columns flex flexGrow flexPositive flexShrink flexNegative flexOrder gridArea gridRow gridRowEnd gridRowSpan gridRowStart gridColumn gridColumnEnd gridColumnSpan gridColumnStart fontWeight lineClamp lineHeight opacity order orphans scale tabSize widows zIndex zoom fillOpacity floodOpacity stopOpacity strokeDasharray strokeDashoffset strokeMiterlimit strokeOpacity strokeWidth MozAnimationIterationCount MozBoxFlex MozBoxFlexGroup MozLineClamp msAnimationIterationCount msFlex msZoom msFlexGrow msFlexNegative msFlexOrder msFlexPositive msFlexShrink msGridColumn msGridColumnSpan msGridRow msGridRowSpan WebkitAnimationIterationCount WebkitBoxFlex WebKitBoxFlexGroup WebkitBoxOrdinalGroup WebkitColumnCount WebkitColumns WebkitFlex WebkitFlexGrow WebkitFlexPositive WebkitFlexShrink WebkitLineClamp".split(
      " "
    )
  );
  function Ds(t, e, l) {
    var a = e.indexOf("--") === 0;
    l == null || typeof l == "boolean" || l === "" ? a ? t.setProperty(e, "") : e === "float" ? t.cssFloat = "" : t[e] = "" : a ? t.setProperty(e, l) : typeof l != "number" || l === 0 || Gy.has(e) ? e === "float" ? t.cssFloat = l : t[e] = ("" + l).trim() : t[e] = l + "px";
  }
  function Us(t, e, l) {
    if (e != null && typeof e != "object")
      throw Error(s(62));
    if (t = t.style, l != null) {
      for (var a in l)
        !l.hasOwnProperty(a) || e != null && e.hasOwnProperty(a) || (a.indexOf("--") === 0 ? t.setProperty(a, "") : a === "float" ? t.cssFloat = "" : t[a] = "");
      for (var n in e)
        a = e[n], e.hasOwnProperty(n) && l[n] !== a && Ds(t, n, a);
    } else
      for (var u in e)
        e.hasOwnProperty(u) && Ds(t, u, e[u]);
  }
  function Ji(t) {
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
  var Qy = /* @__PURE__ */ new Map([
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
  ]), Xy = /^[\u0000-\u001F ]*j[\r\n\t]*a[\r\n\t]*v[\r\n\t]*a[\r\n\t]*s[\r\n\t]*c[\r\n\t]*r[\r\n\t]*i[\r\n\t]*p[\r\n\t]*t[\r\n\t]*:/i;
  function Tu(t) {
    return Xy.test("" + t) ? "javascript:throw new Error('React has blocked a javascript: URL as a security precaution.')" : t;
  }
  function tl() {
  }
  var Ki = null;
  function ki(t) {
    return t = t.target || t.srcElement || window, t.correspondingUseElement && (t = t.correspondingUseElement), t.nodeType === 3 ? t.parentNode : t;
  }
  var Ca = null, Ra = null;
  function js(t) {
    var e = Oa(t);
    if (e && (t = e.stateNode)) {
      var l = t[re] || null;
      t: switch (t = e.stateNode, e.type) {
        case "input":
          if (Vi(
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
              'input[name="' + Ne(
                "" + e
              ) + '"][type="radio"]'
            ), e = 0; e < l.length; e++) {
              var a = l[e];
              if (a !== t && a.form === t.form) {
                var n = a[re] || null;
                if (!n) throw Error(s(90));
                Vi(
                  a,
                  n.value,
                  n.defaultValue,
                  n.defaultValue,
                  n.checked,
                  n.defaultChecked,
                  n.type,
                  n.name
                );
              }
            }
            for (e = 0; e < l.length; e++)
              a = l[e], a.form === t.form && qs(a);
          }
          break t;
        case "textarea":
          Os(t, l.value, l.defaultValue);
          break t;
        case "select":
          e = l.value, e != null && Ua(t, !!l.multiple, e, !1);
      }
    }
  }
  var $i = !1;
  function Cs(t, e, l) {
    if ($i) return t(e, l);
    $i = !0;
    try {
      var a = t(e);
      return a;
    } finally {
      if ($i = !1, (Ca !== null || Ra !== null) && (yi(), Ca && (e = Ca, t = Ra, Ra = Ca = null, js(e), t)))
        for (e = 0; e < t.length; e++) js(t[e]);
    }
  }
  function xn(t, e) {
    var l = t.stateNode;
    if (l === null) return null;
    var a = l[re] || null;
    if (a === null) return null;
    l = a[e];
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
        (a = !a.disabled) || (t = t.type, a = !(t === "button" || t === "input" || t === "select" || t === "textarea")), t = !a;
        break t;
      default:
        t = !1;
    }
    if (t) return null;
    if (l && typeof l != "function")
      throw Error(
        s(231, e, typeof l)
      );
    return l;
  }
  var el = !(typeof window > "u" || typeof window.document > "u" || typeof window.document.createElement > "u"), Wi = !1;
  if (el)
    try {
      var zn = {};
      Object.defineProperty(zn, "passive", {
        get: function() {
          Wi = !0;
        }
      }), window.addEventListener("test", zn, zn), window.removeEventListener("test", zn, zn);
    } catch {
      Wi = !1;
    }
  var xl = null, Fi = null, qu = null;
  function Rs() {
    if (qu) return qu;
    var t, e = Fi, l = e.length, a, n = "value" in xl ? xl.value : xl.textContent, u = n.length;
    for (t = 0; t < l && e[t] === n[t]; t++) ;
    var i = l - t;
    for (a = 1; a <= i && e[l - a] === n[u - a]; a++) ;
    return qu = n.slice(t, 1 < a ? 1 - a : void 0);
  }
  function Nu(t) {
    var e = t.keyCode;
    return "charCode" in t ? (t = t.charCode, t === 0 && e === 13 && (t = 13)) : t = e, t === 10 && (t = 13), 32 <= t || t === 13 ? t : 0;
  }
  function Ou() {
    return !0;
  }
  function Hs() {
    return !1;
  }
  function oe(t) {
    function e(l, a, n, u, i) {
      this._reactName = l, this._targetInst = n, this.type = a, this.nativeEvent = u, this.target = i, this.currentTarget = null;
      for (var o in t)
        t.hasOwnProperty(o) && (l = t[o], this[o] = l ? l(u) : u[o]);
      return this.isDefaultPrevented = (u.defaultPrevented != null ? u.defaultPrevented : u.returnValue === !1) ? Ou : Hs, this.isPropagationStopped = Hs, this;
    }
    return b(e.prototype, {
      preventDefault: function() {
        this.defaultPrevented = !0;
        var l = this.nativeEvent;
        l && (l.preventDefault ? l.preventDefault() : typeof l.returnValue != "unknown" && (l.returnValue = !1), this.isDefaultPrevented = Ou);
      },
      stopPropagation: function() {
        var l = this.nativeEvent;
        l && (l.stopPropagation ? l.stopPropagation() : typeof l.cancelBubble != "unknown" && (l.cancelBubble = !0), this.isPropagationStopped = Ou);
      },
      persist: function() {
      },
      isPersistent: Ou
    }), e;
  }
  var na = {
    eventPhase: 0,
    bubbles: 0,
    cancelable: 0,
    timeStamp: function(t) {
      return t.timeStamp || Date.now();
    },
    defaultPrevented: 0,
    isTrusted: 0
  }, Mu = oe(na), Tn = b({}, na, { view: 0, detail: 0 }), Zy = oe(Tn), Ii, Pi, qn, Du = b({}, Tn, {
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
    getModifierState: ec,
    button: 0,
    buttons: 0,
    relatedTarget: function(t) {
      return t.relatedTarget === void 0 ? t.fromElement === t.srcElement ? t.toElement : t.fromElement : t.relatedTarget;
    },
    movementX: function(t) {
      return "movementX" in t ? t.movementX : (t !== qn && (qn && t.type === "mousemove" ? (Ii = t.screenX - qn.screenX, Pi = t.screenY - qn.screenY) : Pi = Ii = 0, qn = t), Ii);
    },
    movementY: function(t) {
      return "movementY" in t ? t.movementY : Pi;
    }
  }), Bs = oe(Du), Vy = b({}, Du, { dataTransfer: 0 }), wy = oe(Vy), Jy = b({}, Tn, { relatedTarget: 0 }), tc = oe(Jy), Ky = b({}, na, {
    animationName: 0,
    elapsedTime: 0,
    pseudoElement: 0
  }), ky = oe(Ky), $y = b({}, na, {
    clipboardData: function(t) {
      return "clipboardData" in t ? t.clipboardData : window.clipboardData;
    }
  }), Wy = oe($y), Fy = b({}, na, { data: 0 }), Ls = oe(Fy), Iy = {
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
  }, Py = {
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
  }, tm = {
    Alt: "altKey",
    Control: "ctrlKey",
    Meta: "metaKey",
    Shift: "shiftKey"
  };
  function em(t) {
    var e = this.nativeEvent;
    return e.getModifierState ? e.getModifierState(t) : (t = tm[t]) ? !!e[t] : !1;
  }
  function ec() {
    return em;
  }
  var lm = b({}, Tn, {
    key: function(t) {
      if (t.key) {
        var e = Iy[t.key] || t.key;
        if (e !== "Unidentified") return e;
      }
      return t.type === "keypress" ? (t = Nu(t), t === 13 ? "Enter" : String.fromCharCode(t)) : t.type === "keydown" || t.type === "keyup" ? Py[t.keyCode] || "Unidentified" : "";
    },
    code: 0,
    location: 0,
    ctrlKey: 0,
    shiftKey: 0,
    altKey: 0,
    metaKey: 0,
    repeat: 0,
    locale: 0,
    getModifierState: ec,
    charCode: function(t) {
      return t.type === "keypress" ? Nu(t) : 0;
    },
    keyCode: function(t) {
      return t.type === "keydown" || t.type === "keyup" ? t.keyCode : 0;
    },
    which: function(t) {
      return t.type === "keypress" ? Nu(t) : t.type === "keydown" || t.type === "keyup" ? t.keyCode : 0;
    }
  }), am = oe(lm), nm = b({}, Du, {
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
  }), Ys = oe(nm), um = b({}, Tn, {
    touches: 0,
    targetTouches: 0,
    changedTouches: 0,
    altKey: 0,
    metaKey: 0,
    ctrlKey: 0,
    shiftKey: 0,
    getModifierState: ec
  }), im = oe(um), cm = b({}, na, {
    propertyName: 0,
    elapsedTime: 0,
    pseudoElement: 0
  }), fm = oe(cm), sm = b({}, Du, {
    deltaX: function(t) {
      return "deltaX" in t ? t.deltaX : "wheelDeltaX" in t ? -t.wheelDeltaX : 0;
    },
    deltaY: function(t) {
      return "deltaY" in t ? t.deltaY : "wheelDeltaY" in t ? -t.wheelDeltaY : "wheelDelta" in t ? -t.wheelDelta : 0;
    },
    deltaZ: 0,
    deltaMode: 0
  }), rm = oe(sm), om = b({}, na, {
    newState: 0,
    oldState: 0
  }), dm = oe(om), ym = [9, 13, 27, 32], lc = el && "CompositionEvent" in window, Nn = null;
  el && "documentMode" in document && (Nn = document.documentMode);
  var mm = el && "TextEvent" in window && !Nn, Gs = el && (!lc || Nn && 8 < Nn && 11 >= Nn), Qs = " ", Xs = !1;
  function Zs(t, e) {
    switch (t) {
      case "keyup":
        return ym.indexOf(e.keyCode) !== -1;
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
  function Vs(t) {
    return t = t.detail, typeof t == "object" && "data" in t ? t.data : null;
  }
  var Ha = !1;
  function hm(t, e) {
    switch (t) {
      case "compositionend":
        return Vs(e);
      case "keypress":
        return e.which !== 32 ? null : (Xs = !0, Qs);
      case "textInput":
        return t = e.data, t === Qs && Xs ? null : t;
      default:
        return null;
    }
  }
  function vm(t, e) {
    if (Ha)
      return t === "compositionend" || !lc && Zs(t, e) ? (t = Rs(), qu = Fi = xl = null, Ha = !1, t) : null;
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
        return Gs && e.locale !== "ko" ? null : e.data;
      default:
        return null;
    }
  }
  var gm = {
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
  function ws(t) {
    var e = t && t.nodeName && t.nodeName.toLowerCase();
    return e === "input" ? !!gm[t.type] : e === "textarea";
  }
  function Js(t, e, l, a) {
    Ca ? Ra ? Ra.push(a) : Ra = [a] : Ca = a, e = Si(e, "onChange"), 0 < e.length && (l = new Mu(
      "onChange",
      "change",
      null,
      l,
      a
    ), t.push({ event: l, listeners: e }));
  }
  var On = null, Mn = null;
  function pm(t) {
    Od(t, 0);
  }
  function Uu(t) {
    var e = An(t);
    if (qs(e)) return t;
  }
  function Ks(t, e) {
    if (t === "change") return e;
  }
  var ks = !1;
  if (el) {
    var ac;
    if (el) {
      var nc = "oninput" in document;
      if (!nc) {
        var $s = document.createElement("div");
        $s.setAttribute("oninput", "return;"), nc = typeof $s.oninput == "function";
      }
      ac = nc;
    } else ac = !1;
    ks = ac && (!document.documentMode || 9 < document.documentMode);
  }
  function Ws() {
    On && (On.detachEvent("onpropertychange", Fs), Mn = On = null);
  }
  function Fs(t) {
    if (t.propertyName === "value" && Uu(Mn)) {
      var e = [];
      Js(
        e,
        Mn,
        t,
        ki(t)
      ), Cs(pm, e);
    }
  }
  function bm(t, e, l) {
    t === "focusin" ? (Ws(), On = e, Mn = l, On.attachEvent("onpropertychange", Fs)) : t === "focusout" && Ws();
  }
  function Sm(t) {
    if (t === "selectionchange" || t === "keyup" || t === "keydown")
      return Uu(Mn);
  }
  function _m(t, e) {
    if (t === "click") return Uu(e);
  }
  function Em(t, e) {
    if (t === "input" || t === "change")
      return Uu(e);
  }
  function Am(t, e) {
    return t === e && (t !== 0 || 1 / t === 1 / e) || t !== t && e !== e;
  }
  var be = typeof Object.is == "function" ? Object.is : Am;
  function Dn(t, e) {
    if (be(t, e)) return !0;
    if (typeof t != "object" || t === null || typeof e != "object" || e === null)
      return !1;
    var l = Object.keys(t), a = Object.keys(e);
    if (l.length !== a.length) return !1;
    for (a = 0; a < l.length; a++) {
      var n = l[a];
      if (!we.call(e, n) || !be(t[n], e[n]))
        return !1;
    }
    return !0;
  }
  function Is(t) {
    for (; t && t.firstChild; ) t = t.firstChild;
    return t;
  }
  function Ps(t, e) {
    var l = Is(t);
    t = 0;
    for (var a; l; ) {
      if (l.nodeType === 3) {
        if (a = t + l.textContent.length, t <= e && a >= e)
          return { node: l, offset: e - t };
        t = a;
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
      l = Is(l);
    }
  }
  function tr(t, e) {
    return t && e ? t === e ? !0 : t && t.nodeType === 3 ? !1 : e && e.nodeType === 3 ? tr(t, e.parentNode) : "contains" in t ? t.contains(e) : t.compareDocumentPosition ? !!(t.compareDocumentPosition(e) & 16) : !1 : !1;
  }
  function er(t) {
    t = t != null && t.ownerDocument != null && t.ownerDocument.defaultView != null ? t.ownerDocument.defaultView : window;
    for (var e = zu(t.document); e instanceof t.HTMLIFrameElement; ) {
      try {
        var l = typeof e.contentWindow.location.href == "string";
      } catch {
        l = !1;
      }
      if (l) t = e.contentWindow;
      else break;
      e = zu(t.document);
    }
    return e;
  }
  function uc(t) {
    var e = t && t.nodeName && t.nodeName.toLowerCase();
    return e && (e === "input" && (t.type === "text" || t.type === "search" || t.type === "tel" || t.type === "url" || t.type === "password") || e === "textarea" || t.contentEditable === "true");
  }
  var xm = el && "documentMode" in document && 11 >= document.documentMode, Ba = null, ic = null, Un = null, cc = !1;
  function lr(t, e, l) {
    var a = l.window === l ? l.document : l.nodeType === 9 ? l : l.ownerDocument;
    cc || Ba == null || Ba !== zu(a) || (a = Ba, "selectionStart" in a && uc(a) ? a = { start: a.selectionStart, end: a.selectionEnd } : (a = (a.ownerDocument && a.ownerDocument.defaultView || window).getSelection(), a = {
      anchorNode: a.anchorNode,
      anchorOffset: a.anchorOffset,
      focusNode: a.focusNode,
      focusOffset: a.focusOffset
    }), Un && Dn(Un, a) || (Un = a, a = Si(ic, "onSelect"), 0 < a.length && (e = new Mu(
      "onSelect",
      "select",
      null,
      e,
      l
    ), t.push({ event: e, listeners: a }), e.target = Ba)));
  }
  function ua(t, e) {
    var l = {};
    return l[t.toLowerCase()] = e.toLowerCase(), l["Webkit" + t] = "webkit" + e, l["Moz" + t] = "moz" + e, l;
  }
  var La = {
    animationend: ua("Animation", "AnimationEnd"),
    animationiteration: ua("Animation", "AnimationIteration"),
    animationstart: ua("Animation", "AnimationStart"),
    transitionrun: ua("Transition", "TransitionRun"),
    transitionstart: ua("Transition", "TransitionStart"),
    transitioncancel: ua("Transition", "TransitionCancel"),
    transitionend: ua("Transition", "TransitionEnd")
  }, fc = {}, ar = {};
  el && (ar = document.createElement("div").style, "AnimationEvent" in window || (delete La.animationend.animation, delete La.animationiteration.animation, delete La.animationstart.animation), "TransitionEvent" in window || delete La.transitionend.transition);
  function ia(t) {
    if (fc[t]) return fc[t];
    if (!La[t]) return t;
    var e = La[t], l;
    for (l in e)
      if (e.hasOwnProperty(l) && l in ar)
        return fc[t] = e[l];
    return t;
  }
  var nr = ia("animationend"), ur = ia("animationiteration"), ir = ia("animationstart"), zm = ia("transitionrun"), Tm = ia("transitionstart"), qm = ia("transitioncancel"), cr = ia("transitionend"), fr = /* @__PURE__ */ new Map(), sc = "abort auxClick beforeToggle cancel canPlay canPlayThrough click close contextMenu copy cut drag dragEnd dragEnter dragExit dragLeave dragOver dragStart drop durationChange emptied encrypted ended error gotPointerCapture input invalid keyDown keyPress keyUp load loadedData loadedMetadata loadStart lostPointerCapture mouseDown mouseMove mouseOut mouseOver mouseUp paste pause play playing pointerCancel pointerDown pointerMove pointerOut pointerOver pointerUp progress rateChange reset resize seeked seeking stalled submit suspend timeUpdate touchCancel touchEnd touchStart volumeChange scroll toggle touchMove waiting wheel".split(
    " "
  );
  sc.push("scrollEnd");
  function Ye(t, e) {
    fr.set(t, e), aa(e, [t]);
  }
  var ju = typeof reportError == "function" ? reportError : function(t) {
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
  }, Oe = [], Ya = 0, rc = 0;
  function Cu() {
    for (var t = Ya, e = rc = Ya = 0; e < t; ) {
      var l = Oe[e];
      Oe[e++] = null;
      var a = Oe[e];
      Oe[e++] = null;
      var n = Oe[e];
      Oe[e++] = null;
      var u = Oe[e];
      if (Oe[e++] = null, a !== null && n !== null) {
        var i = a.pending;
        i === null ? n.next = n : (n.next = i.next, i.next = n), a.pending = n;
      }
      u !== 0 && sr(l, n, u);
    }
  }
  function Ru(t, e, l, a) {
    Oe[Ya++] = t, Oe[Ya++] = e, Oe[Ya++] = l, Oe[Ya++] = a, rc |= a, t.lanes |= a, t = t.alternate, t !== null && (t.lanes |= a);
  }
  function oc(t, e, l, a) {
    return Ru(t, e, l, a), Hu(t);
  }
  function ca(t, e) {
    return Ru(t, null, null, e), Hu(t);
  }
  function sr(t, e, l) {
    t.lanes |= l;
    var a = t.alternate;
    a !== null && (a.lanes |= l);
    for (var n = !1, u = t.return; u !== null; )
      u.childLanes |= l, a = u.alternate, a !== null && (a.childLanes |= l), u.tag === 22 && (t = u.stateNode, t === null || t._visibility & 1 || (n = !0)), t = u, u = u.return;
    return t.tag === 3 ? (u = t.stateNode, n && e !== null && (n = 31 - et(l), t = u.hiddenUpdates, a = t[n], a === null ? t[n] = [e] : a.push(e), e.lane = l | 536870912), u) : null;
  }
  function Hu(t) {
    if (50 < tu)
      throw tu = 0, _f = null, Error(s(185));
    for (var e = t.return; e !== null; )
      t = e, e = t.return;
    return t.tag === 3 ? t.stateNode : null;
  }
  var Ga = {};
  function Nm(t, e, l, a) {
    this.tag = t, this.key = l, this.sibling = this.child = this.return = this.stateNode = this.type = this.elementType = null, this.index = 0, this.refCleanup = this.ref = null, this.pendingProps = e, this.dependencies = this.memoizedState = this.updateQueue = this.memoizedProps = null, this.mode = a, this.subtreeFlags = this.flags = 0, this.deletions = null, this.childLanes = this.lanes = 0, this.alternate = null;
  }
  function Se(t, e, l, a) {
    return new Nm(t, e, l, a);
  }
  function dc(t) {
    return t = t.prototype, !(!t || !t.isReactComponent);
  }
  function ll(t, e) {
    var l = t.alternate;
    return l === null ? (l = Se(
      t.tag,
      e,
      t.key,
      t.mode
    ), l.elementType = t.elementType, l.type = t.type, l.stateNode = t.stateNode, l.alternate = t, t.alternate = l) : (l.pendingProps = e, l.type = t.type, l.flags = 0, l.subtreeFlags = 0, l.deletions = null), l.flags = t.flags & 65011712, l.childLanes = t.childLanes, l.lanes = t.lanes, l.child = t.child, l.memoizedProps = t.memoizedProps, l.memoizedState = t.memoizedState, l.updateQueue = t.updateQueue, e = t.dependencies, l.dependencies = e === null ? null : { lanes: e.lanes, firstContext: e.firstContext }, l.sibling = t.sibling, l.index = t.index, l.ref = t.ref, l.refCleanup = t.refCleanup, l;
  }
  function rr(t, e) {
    t.flags &= 65011714;
    var l = t.alternate;
    return l === null ? (t.childLanes = 0, t.lanes = e, t.child = null, t.subtreeFlags = 0, t.memoizedProps = null, t.memoizedState = null, t.updateQueue = null, t.dependencies = null, t.stateNode = null) : (t.childLanes = l.childLanes, t.lanes = l.lanes, t.child = l.child, t.subtreeFlags = 0, t.deletions = null, t.memoizedProps = l.memoizedProps, t.memoizedState = l.memoizedState, t.updateQueue = l.updateQueue, t.type = l.type, e = l.dependencies, t.dependencies = e === null ? null : {
      lanes: e.lanes,
      firstContext: e.firstContext
    }), t;
  }
  function Bu(t, e, l, a, n, u) {
    var i = 0;
    if (a = t, typeof t == "function") dc(t) && (i = 1);
    else if (typeof t == "string")
      i = jh(
        t,
        l,
        Y.current
      ) ? 26 : t === "html" || t === "head" || t === "body" ? 27 : 5;
    else
      t: switch (t) {
        case pe:
          return t = Se(31, l, e, n), t.elementType = pe, t.lanes = u, t;
        case w:
          return fa(l.children, n, u, e);
        case B:
          i = 8, n |= 24;
          break;
        case J:
          return t = Se(12, l, e, n | 2), t.elementType = J, t.lanes = u, t;
        case Kt:
          return t = Se(13, l, e, n), t.elementType = Kt, t.lanes = u, t;
        case Rt:
          return t = Se(19, l, e, n), t.elementType = Rt, t.lanes = u, t;
        default:
          if (typeof t == "object" && t !== null)
            switch (t.$$typeof) {
              case tt:
                i = 10;
                break t;
              case Et:
                i = 9;
                break t;
              case vt:
                i = 11;
                break t;
              case at:
                i = 14;
                break t;
              case bt:
                i = 16, a = null;
                break t;
            }
          i = 29, l = Error(
            s(130, t === null ? "null" : typeof t, "")
          ), a = null;
      }
    return e = Se(i, l, e, n), e.elementType = t, e.type = a, e.lanes = u, e;
  }
  function fa(t, e, l, a) {
    return t = Se(7, t, a, e), t.lanes = l, t;
  }
  function yc(t, e, l) {
    return t = Se(6, t, null, e), t.lanes = l, t;
  }
  function or(t) {
    var e = Se(18, null, null, 0);
    return e.stateNode = t, e;
  }
  function mc(t, e, l) {
    return e = Se(
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
  var dr = /* @__PURE__ */ new WeakMap();
  function Me(t, e) {
    if (typeof t == "object" && t !== null) {
      var l = dr.get(t);
      return l !== void 0 ? l : (e = {
        value: t,
        source: e,
        stack: za(e)
      }, dr.set(t, e), e);
    }
    return {
      value: t,
      source: e,
      stack: za(e)
    };
  }
  var Qa = [], Xa = 0, Lu = null, jn = 0, De = [], Ue = 0, zl = null, Ke = 1, ke = "";
  function al(t, e) {
    Qa[Xa++] = jn, Qa[Xa++] = Lu, Lu = t, jn = e;
  }
  function yr(t, e, l) {
    De[Ue++] = Ke, De[Ue++] = ke, De[Ue++] = zl, zl = t;
    var a = Ke;
    t = ke;
    var n = 32 - et(a) - 1;
    a &= ~(1 << n), l += 1;
    var u = 32 - et(e) + n;
    if (30 < u) {
      var i = n - n % 5;
      u = (a & (1 << i) - 1).toString(32), a >>= i, n -= i, Ke = 1 << 32 - et(e) + n | l << n | a, ke = u + t;
    } else
      Ke = 1 << u | l << n | a, ke = t;
  }
  function hc(t) {
    t.return !== null && (al(t, 1), yr(t, 1, 0));
  }
  function vc(t) {
    for (; t === Lu; )
      Lu = Qa[--Xa], Qa[Xa] = null, jn = Qa[--Xa], Qa[Xa] = null;
    for (; t === zl; )
      zl = De[--Ue], De[Ue] = null, ke = De[--Ue], De[Ue] = null, Ke = De[--Ue], De[Ue] = null;
  }
  function mr(t, e) {
    De[Ue++] = Ke, De[Ue++] = ke, De[Ue++] = zl, Ke = e.id, ke = e.overflow, zl = t;
  }
  var te = null, Ot = null, rt = !1, Tl = null, je = !1, gc = Error(s(519));
  function ql(t) {
    var e = Error(
      s(
        418,
        1 < arguments.length && arguments[1] !== void 0 && arguments[1] ? "text" : "HTML",
        ""
      )
    );
    throw Cn(Me(e, t)), gc;
  }
  function hr(t) {
    var e = t.stateNode, l = t.type, a = t.memoizedProps;
    switch (e[Pt] = t, e[re] = a, l) {
      case "dialog":
        ct("cancel", e), ct("close", e);
        break;
      case "iframe":
      case "object":
      case "embed":
        ct("load", e);
        break;
      case "video":
      case "audio":
        for (l = 0; l < lu.length; l++)
          ct(lu[l], e);
        break;
      case "source":
        ct("error", e);
        break;
      case "img":
      case "image":
      case "link":
        ct("error", e), ct("load", e);
        break;
      case "details":
        ct("toggle", e);
        break;
      case "input":
        ct("invalid", e), Ns(
          e,
          a.value,
          a.defaultValue,
          a.checked,
          a.defaultChecked,
          a.type,
          a.name,
          !0
        );
        break;
      case "select":
        ct("invalid", e);
        break;
      case "textarea":
        ct("invalid", e), Ms(e, a.value, a.defaultValue, a.children);
    }
    l = a.children, typeof l != "string" && typeof l != "number" && typeof l != "bigint" || e.textContent === "" + l || a.suppressHydrationWarning === !0 || jd(e.textContent, l) ? (a.popover != null && (ct("beforetoggle", e), ct("toggle", e)), a.onScroll != null && ct("scroll", e), a.onScrollEnd != null && ct("scrollend", e), a.onClick != null && (e.onclick = tl), e = !0) : e = !1, e || ql(t, !0);
  }
  function vr(t) {
    for (te = t.return; te; )
      switch (te.tag) {
        case 5:
        case 31:
        case 13:
          je = !1;
          return;
        case 27:
        case 3:
          je = !0;
          return;
        default:
          te = te.return;
      }
  }
  function Za(t) {
    if (t !== te) return !1;
    if (!rt) return vr(t), rt = !0, !1;
    var e = t.tag, l;
    if ((l = e !== 3 && e !== 27) && ((l = e === 5) && (l = t.type, l = !(l !== "form" && l !== "button") || Hf(t.type, t.memoizedProps)), l = !l), l && Ot && ql(t), vr(t), e === 13) {
      if (t = t.memoizedState, t = t !== null ? t.dehydrated : null, !t) throw Error(s(317));
      Ot = Xd(t);
    } else if (e === 31) {
      if (t = t.memoizedState, t = t !== null ? t.dehydrated : null, !t) throw Error(s(317));
      Ot = Xd(t);
    } else
      e === 27 ? (e = Ot, Ql(t.type) ? (t = Qf, Qf = null, Ot = t) : Ot = e) : Ot = te ? Re(t.stateNode.nextSibling) : null;
    return !0;
  }
  function sa() {
    Ot = te = null, rt = !1;
  }
  function pc() {
    var t = Tl;
    return t !== null && (he === null ? he = t : he.push.apply(
      he,
      t
    ), Tl = null), t;
  }
  function Cn(t) {
    Tl === null ? Tl = [t] : Tl.push(t);
  }
  var bc = m(null), ra = null, nl = null;
  function Nl(t, e, l) {
    H(bc, e._currentValue), e._currentValue = l;
  }
  function ul(t) {
    t._currentValue = bc.current, D(bc);
  }
  function Sc(t, e, l) {
    for (; t !== null; ) {
      var a = t.alternate;
      if ((t.childLanes & e) !== e ? (t.childLanes |= e, a !== null && (a.childLanes |= e)) : a !== null && (a.childLanes & e) !== e && (a.childLanes |= e), t === l) break;
      t = t.return;
    }
  }
  function _c(t, e, l, a) {
    var n = t.child;
    for (n !== null && (n.return = t); n !== null; ) {
      var u = n.dependencies;
      if (u !== null) {
        var i = n.child;
        u = u.firstContext;
        t: for (; u !== null; ) {
          var o = u;
          u = n;
          for (var d = 0; d < e.length; d++)
            if (o.context === e[d]) {
              u.lanes |= l, o = u.alternate, o !== null && (o.lanes |= l), Sc(
                u.return,
                l,
                t
              ), a || (i = null);
              break t;
            }
          u = o.next;
        }
      } else if (n.tag === 18) {
        if (i = n.return, i === null) throw Error(s(341));
        i.lanes |= l, u = i.alternate, u !== null && (u.lanes |= l), Sc(i, l, t), i = null;
      } else i = n.child;
      if (i !== null) i.return = n;
      else
        for (i = n; i !== null; ) {
          if (i === t) {
            i = null;
            break;
          }
          if (n = i.sibling, n !== null) {
            n.return = i.return, i = n;
            break;
          }
          i = i.return;
        }
      n = i;
    }
  }
  function Va(t, e, l, a) {
    t = null;
    for (var n = e, u = !1; n !== null; ) {
      if (!u) {
        if ((n.flags & 524288) !== 0) u = !0;
        else if ((n.flags & 262144) !== 0) break;
      }
      if (n.tag === 10) {
        var i = n.alternate;
        if (i === null) throw Error(s(387));
        if (i = i.memoizedProps, i !== null) {
          var o = n.type;
          be(n.pendingProps.value, i.value) || (t !== null ? t.push(o) : t = [o]);
        }
      } else if (n === yt.current) {
        if (i = n.alternate, i === null) throw Error(s(387));
        i.memoizedState.memoizedState !== n.memoizedState.memoizedState && (t !== null ? t.push(cu) : t = [cu]);
      }
      n = n.return;
    }
    t !== null && _c(
      e,
      t,
      l,
      a
    ), e.flags |= 262144;
  }
  function Yu(t) {
    for (t = t.firstContext; t !== null; ) {
      if (!be(
        t.context._currentValue,
        t.memoizedValue
      ))
        return !0;
      t = t.next;
    }
    return !1;
  }
  function oa(t) {
    ra = t, nl = null, t = t.dependencies, t !== null && (t.firstContext = null);
  }
  function ee(t) {
    return gr(ra, t);
  }
  function Gu(t, e) {
    return ra === null && oa(t), gr(t, e);
  }
  function gr(t, e) {
    var l = e._currentValue;
    if (e = { context: e, memoizedValue: l, next: null }, nl === null) {
      if (t === null) throw Error(s(308));
      nl = e, t.dependencies = { lanes: 0, firstContext: e }, t.flags |= 524288;
    } else nl = nl.next = e;
    return l;
  }
  var Om = typeof AbortController < "u" ? AbortController : function() {
    var t = [], e = this.signal = {
      aborted: !1,
      addEventListener: function(l, a) {
        t.push(a);
      }
    };
    this.abort = function() {
      e.aborted = !0, t.forEach(function(l) {
        return l();
      });
    };
  }, Mm = c.unstable_scheduleCallback, Dm = c.unstable_NormalPriority, Qt = {
    $$typeof: tt,
    Consumer: null,
    Provider: null,
    _currentValue: null,
    _currentValue2: null,
    _threadCount: 0
  };
  function Ec() {
    return {
      controller: new Om(),
      data: /* @__PURE__ */ new Map(),
      refCount: 0
    };
  }
  function Rn(t) {
    t.refCount--, t.refCount === 0 && Mm(Dm, function() {
      t.controller.abort();
    });
  }
  var Hn = null, Ac = 0, wa = 0, Ja = null;
  function Um(t, e) {
    if (Hn === null) {
      var l = Hn = [];
      Ac = 0, wa = qf(), Ja = {
        status: "pending",
        value: void 0,
        then: function(a) {
          l.push(a);
        }
      };
    }
    return Ac++, e.then(pr, pr), e;
  }
  function pr() {
    if (--Ac === 0 && Hn !== null) {
      Ja !== null && (Ja.status = "fulfilled");
      var t = Hn;
      Hn = null, wa = 0, Ja = null;
      for (var e = 0; e < t.length; e++) (0, t[e])();
    }
  }
  function jm(t, e) {
    var l = [], a = {
      status: "pending",
      value: null,
      reason: null,
      then: function(n) {
        l.push(n);
      }
    };
    return t.then(
      function() {
        a.status = "fulfilled", a.value = e;
        for (var n = 0; n < l.length; n++) (0, l[n])(e);
      },
      function(n) {
        for (a.status = "rejected", a.reason = n, n = 0; n < l.length; n++)
          (0, l[n])(void 0);
      }
    ), a;
  }
  var br = z.S;
  z.S = function(t, e) {
    ad = It(), typeof e == "object" && e !== null && typeof e.then == "function" && Um(t, e), br !== null && br(t, e);
  };
  var da = m(null);
  function xc() {
    var t = da.current;
    return t !== null ? t : Tt.pooledCache;
  }
  function Qu(t, e) {
    e === null ? H(da, da.current) : H(da, e.pool);
  }
  function Sr() {
    var t = xc();
    return t === null ? null : { parent: Qt._currentValue, pool: t };
  }
  var Ka = Error(s(460)), zc = Error(s(474)), Xu = Error(s(542)), Zu = { then: function() {
  } };
  function _r(t) {
    return t = t.status, t === "fulfilled" || t === "rejected";
  }
  function Er(t, e, l) {
    switch (l = t[l], l === void 0 ? t.push(e) : l !== e && (e.then(tl, tl), e = l), e.status) {
      case "fulfilled":
        return e.value;
      case "rejected":
        throw t = e.reason, xr(t), t;
      default:
        if (typeof e.status == "string") e.then(tl, tl);
        else {
          if (t = Tt, t !== null && 100 < t.shellSuspendCounter)
            throw Error(s(482));
          t = e, t.status = "pending", t.then(
            function(a) {
              if (e.status === "pending") {
                var n = e;
                n.status = "fulfilled", n.value = a;
              }
            },
            function(a) {
              if (e.status === "pending") {
                var n = e;
                n.status = "rejected", n.reason = a;
              }
            }
          );
        }
        switch (e.status) {
          case "fulfilled":
            return e.value;
          case "rejected":
            throw t = e.reason, xr(t), t;
        }
        throw ma = e, Ka;
    }
  }
  function ya(t) {
    try {
      var e = t._init;
      return e(t._payload);
    } catch (l) {
      throw l !== null && typeof l == "object" && typeof l.then == "function" ? (ma = l, Ka) : l;
    }
  }
  var ma = null;
  function Ar() {
    if (ma === null) throw Error(s(459));
    var t = ma;
    return ma = null, t;
  }
  function xr(t) {
    if (t === Ka || t === Xu)
      throw Error(s(483));
  }
  var ka = null, Bn = 0;
  function Vu(t) {
    var e = Bn;
    return Bn += 1, ka === null && (ka = []), Er(ka, t, e);
  }
  function Ln(t, e) {
    e = e.props.ref, t.ref = e !== void 0 ? e : null;
  }
  function wu(t, e) {
    throw e.$$typeof === j ? Error(s(525)) : (t = Object.prototype.toString.call(e), Error(
      s(
        31,
        t === "[object Object]" ? "object with keys {" + Object.keys(e).join(", ") + "}" : t
      )
    ));
  }
  function zr(t) {
    function e(v, y) {
      if (t) {
        var S = v.deletions;
        S === null ? (v.deletions = [y], v.flags |= 16) : S.push(y);
      }
    }
    function l(v, y) {
      if (!t) return null;
      for (; y !== null; )
        e(v, y), y = y.sibling;
      return null;
    }
    function a(v) {
      for (var y = /* @__PURE__ */ new Map(); v !== null; )
        v.key !== null ? y.set(v.key, v) : y.set(v.index, v), v = v.sibling;
      return y;
    }
    function n(v, y) {
      return v = ll(v, y), v.index = 0, v.sibling = null, v;
    }
    function u(v, y, S) {
      return v.index = S, t ? (S = v.alternate, S !== null ? (S = S.index, S < y ? (v.flags |= 67108866, y) : S) : (v.flags |= 67108866, y)) : (v.flags |= 1048576, y);
    }
    function i(v) {
      return t && v.alternate === null && (v.flags |= 67108866), v;
    }
    function o(v, y, S, O) {
      return y === null || y.tag !== 6 ? (y = yc(S, v.mode, O), y.return = v, y) : (y = n(y, S), y.return = v, y);
    }
    function d(v, y, S, O) {
      var V = S.type;
      return V === w ? T(
        v,
        y,
        S.props.children,
        O,
        S.key
      ) : y !== null && (y.elementType === V || typeof V == "object" && V !== null && V.$$typeof === bt && ya(V) === y.type) ? (y = n(y, S.props), Ln(y, S), y.return = v, y) : (y = Bu(
        S.type,
        S.key,
        S.props,
        null,
        v.mode,
        O
      ), Ln(y, S), y.return = v, y);
    }
    function _(v, y, S, O) {
      return y === null || y.tag !== 4 || y.stateNode.containerInfo !== S.containerInfo || y.stateNode.implementation !== S.implementation ? (y = mc(S, v.mode, O), y.return = v, y) : (y = n(y, S.children || []), y.return = v, y);
    }
    function T(v, y, S, O, V) {
      return y === null || y.tag !== 7 ? (y = fa(
        S,
        v.mode,
        O,
        V
      ), y.return = v, y) : (y = n(y, S), y.return = v, y);
    }
    function M(v, y, S) {
      if (typeof y == "string" && y !== "" || typeof y == "number" || typeof y == "bigint")
        return y = yc(
          "" + y,
          v.mode,
          S
        ), y.return = v, y;
      if (typeof y == "object" && y !== null) {
        switch (y.$$typeof) {
          case Q:
            return S = Bu(
              y.type,
              y.key,
              y.props,
              null,
              v.mode,
              S
            ), Ln(S, y), S.return = v, S;
          case W:
            return y = mc(
              y,
              v.mode,
              S
            ), y.return = v, y;
          case bt:
            return y = ya(y), M(v, y, S);
        }
        if (Yt(y) || wt(y))
          return y = fa(
            y,
            v.mode,
            S,
            null
          ), y.return = v, y;
        if (typeof y.then == "function")
          return M(v, Vu(y), S);
        if (y.$$typeof === tt)
          return M(
            v,
            Gu(v, y),
            S
          );
        wu(v, y);
      }
      return null;
    }
    function E(v, y, S, O) {
      var V = y !== null ? y.key : null;
      if (typeof S == "string" && S !== "" || typeof S == "number" || typeof S == "bigint")
        return V !== null ? null : o(v, y, "" + S, O);
      if (typeof S == "object" && S !== null) {
        switch (S.$$typeof) {
          case Q:
            return S.key === V ? d(v, y, S, O) : null;
          case W:
            return S.key === V ? _(v, y, S, O) : null;
          case bt:
            return S = ya(S), E(v, y, S, O);
        }
        if (Yt(S) || wt(S))
          return V !== null ? null : T(v, y, S, O, null);
        if (typeof S.then == "function")
          return E(
            v,
            y,
            Vu(S),
            O
          );
        if (S.$$typeof === tt)
          return E(
            v,
            y,
            Gu(v, S),
            O
          );
        wu(v, S);
      }
      return null;
    }
    function A(v, y, S, O, V) {
      if (typeof O == "string" && O !== "" || typeof O == "number" || typeof O == "bigint")
        return v = v.get(S) || null, o(y, v, "" + O, V);
      if (typeof O == "object" && O !== null) {
        switch (O.$$typeof) {
          case Q:
            return v = v.get(
              O.key === null ? S : O.key
            ) || null, d(y, v, O, V);
          case W:
            return v = v.get(
              O.key === null ? S : O.key
            ) || null, _(y, v, O, V);
          case bt:
            return O = ya(O), A(
              v,
              y,
              S,
              O,
              V
            );
        }
        if (Yt(O) || wt(O))
          return v = v.get(S) || null, T(y, v, O, V, null);
        if (typeof O.then == "function")
          return A(
            v,
            y,
            S,
            Vu(O),
            V
          );
        if (O.$$typeof === tt)
          return A(
            v,
            y,
            S,
            Gu(y, O),
            V
          );
        wu(y, O);
      }
      return null;
    }
    function G(v, y, S, O) {
      for (var V = null, mt = null, X = y, ut = y = 0, st = null; X !== null && ut < S.length; ut++) {
        X.index > ut ? (st = X, X = null) : st = X.sibling;
        var ht = E(
          v,
          X,
          S[ut],
          O
        );
        if (ht === null) {
          X === null && (X = st);
          break;
        }
        t && X && ht.alternate === null && e(v, X), y = u(ht, y, ut), mt === null ? V = ht : mt.sibling = ht, mt = ht, X = st;
      }
      if (ut === S.length)
        return l(v, X), rt && al(v, ut), V;
      if (X === null) {
        for (; ut < S.length; ut++)
          X = M(v, S[ut], O), X !== null && (y = u(
            X,
            y,
            ut
          ), mt === null ? V = X : mt.sibling = X, mt = X);
        return rt && al(v, ut), V;
      }
      for (X = a(X); ut < S.length; ut++)
        st = A(
          X,
          v,
          ut,
          S[ut],
          O
        ), st !== null && (t && st.alternate !== null && X.delete(
          st.key === null ? ut : st.key
        ), y = u(
          st,
          y,
          ut
        ), mt === null ? V = st : mt.sibling = st, mt = st);
      return t && X.forEach(function(Jl) {
        return e(v, Jl);
      }), rt && al(v, ut), V;
    }
    function K(v, y, S, O) {
      if (S == null) throw Error(s(151));
      for (var V = null, mt = null, X = y, ut = y = 0, st = null, ht = S.next(); X !== null && !ht.done; ut++, ht = S.next()) {
        X.index > ut ? (st = X, X = null) : st = X.sibling;
        var Jl = E(v, X, ht.value, O);
        if (Jl === null) {
          X === null && (X = st);
          break;
        }
        t && X && Jl.alternate === null && e(v, X), y = u(Jl, y, ut), mt === null ? V = Jl : mt.sibling = Jl, mt = Jl, X = st;
      }
      if (ht.done)
        return l(v, X), rt && al(v, ut), V;
      if (X === null) {
        for (; !ht.done; ut++, ht = S.next())
          ht = M(v, ht.value, O), ht !== null && (y = u(ht, y, ut), mt === null ? V = ht : mt.sibling = ht, mt = ht);
        return rt && al(v, ut), V;
      }
      for (X = a(X); !ht.done; ut++, ht = S.next())
        ht = A(X, v, ut, ht.value, O), ht !== null && (t && ht.alternate !== null && X.delete(ht.key === null ? ut : ht.key), y = u(ht, y, ut), mt === null ? V = ht : mt.sibling = ht, mt = ht);
      return t && X.forEach(function(Vh) {
        return e(v, Vh);
      }), rt && al(v, ut), V;
    }
    function zt(v, y, S, O) {
      if (typeof S == "object" && S !== null && S.type === w && S.key === null && (S = S.props.children), typeof S == "object" && S !== null) {
        switch (S.$$typeof) {
          case Q:
            t: {
              for (var V = S.key; y !== null; ) {
                if (y.key === V) {
                  if (V = S.type, V === w) {
                    if (y.tag === 7) {
                      l(
                        v,
                        y.sibling
                      ), O = n(
                        y,
                        S.props.children
                      ), O.return = v, v = O;
                      break t;
                    }
                  } else if (y.elementType === V || typeof V == "object" && V !== null && V.$$typeof === bt && ya(V) === y.type) {
                    l(
                      v,
                      y.sibling
                    ), O = n(y, S.props), Ln(O, S), O.return = v, v = O;
                    break t;
                  }
                  l(v, y);
                  break;
                } else e(v, y);
                y = y.sibling;
              }
              S.type === w ? (O = fa(
                S.props.children,
                v.mode,
                O,
                S.key
              ), O.return = v, v = O) : (O = Bu(
                S.type,
                S.key,
                S.props,
                null,
                v.mode,
                O
              ), Ln(O, S), O.return = v, v = O);
            }
            return i(v);
          case W:
            t: {
              for (V = S.key; y !== null; ) {
                if (y.key === V)
                  if (y.tag === 4 && y.stateNode.containerInfo === S.containerInfo && y.stateNode.implementation === S.implementation) {
                    l(
                      v,
                      y.sibling
                    ), O = n(y, S.children || []), O.return = v, v = O;
                    break t;
                  } else {
                    l(v, y);
                    break;
                  }
                else e(v, y);
                y = y.sibling;
              }
              O = mc(S, v.mode, O), O.return = v, v = O;
            }
            return i(v);
          case bt:
            return S = ya(S), zt(
              v,
              y,
              S,
              O
            );
        }
        if (Yt(S))
          return G(
            v,
            y,
            S,
            O
          );
        if (wt(S)) {
          if (V = wt(S), typeof V != "function") throw Error(s(150));
          return S = V.call(S), K(
            v,
            y,
            S,
            O
          );
        }
        if (typeof S.then == "function")
          return zt(
            v,
            y,
            Vu(S),
            O
          );
        if (S.$$typeof === tt)
          return zt(
            v,
            y,
            Gu(v, S),
            O
          );
        wu(v, S);
      }
      return typeof S == "string" && S !== "" || typeof S == "number" || typeof S == "bigint" ? (S = "" + S, y !== null && y.tag === 6 ? (l(v, y.sibling), O = n(y, S), O.return = v, v = O) : (l(v, y), O = yc(S, v.mode, O), O.return = v, v = O), i(v)) : l(v, y);
    }
    return function(v, y, S, O) {
      try {
        Bn = 0;
        var V = zt(
          v,
          y,
          S,
          O
        );
        return ka = null, V;
      } catch (X) {
        if (X === Ka || X === Xu) throw X;
        var mt = Se(29, X, null, v.mode);
        return mt.lanes = O, mt.return = v, mt;
      } finally {
      }
    };
  }
  var ha = zr(!0), Tr = zr(!1), Ol = !1;
  function Tc(t) {
    t.updateQueue = {
      baseState: t.memoizedState,
      firstBaseUpdate: null,
      lastBaseUpdate: null,
      shared: { pending: null, lanes: 0, hiddenCallbacks: null },
      callbacks: null
    };
  }
  function qc(t, e) {
    t = t.updateQueue, e.updateQueue === t && (e.updateQueue = {
      baseState: t.baseState,
      firstBaseUpdate: t.firstBaseUpdate,
      lastBaseUpdate: t.lastBaseUpdate,
      shared: t.shared,
      callbacks: null
    });
  }
  function Ml(t) {
    return { lane: t, tag: 0, payload: null, callback: null, next: null };
  }
  function Dl(t, e, l) {
    var a = t.updateQueue;
    if (a === null) return null;
    if (a = a.shared, (pt & 2) !== 0) {
      var n = a.pending;
      return n === null ? e.next = e : (e.next = n.next, n.next = e), a.pending = e, e = Hu(t), sr(t, null, l), e;
    }
    return Ru(t, a, e, l), Hu(t);
  }
  function Yn(t, e, l) {
    if (e = e.updateQueue, e !== null && (e = e.shared, (l & 4194048) !== 0)) {
      var a = e.lanes;
      a &= t.pendingLanes, l |= a, e.lanes = l, gs(t, l);
    }
  }
  function Nc(t, e) {
    var l = t.updateQueue, a = t.alternate;
    if (a !== null && (a = a.updateQueue, l === a)) {
      var n = null, u = null;
      if (l = l.firstBaseUpdate, l !== null) {
        do {
          var i = {
            lane: l.lane,
            tag: l.tag,
            payload: l.payload,
            callback: null,
            next: null
          };
          u === null ? n = u = i : u = u.next = i, l = l.next;
        } while (l !== null);
        u === null ? n = u = e : u = u.next = e;
      } else n = u = e;
      l = {
        baseState: a.baseState,
        firstBaseUpdate: n,
        lastBaseUpdate: u,
        shared: a.shared,
        callbacks: a.callbacks
      }, t.updateQueue = l;
      return;
    }
    t = l.lastBaseUpdate, t === null ? l.firstBaseUpdate = e : t.next = e, l.lastBaseUpdate = e;
  }
  var Oc = !1;
  function Gn() {
    if (Oc) {
      var t = Ja;
      if (t !== null) throw t;
    }
  }
  function Qn(t, e, l, a) {
    Oc = !1;
    var n = t.updateQueue;
    Ol = !1;
    var u = n.firstBaseUpdate, i = n.lastBaseUpdate, o = n.shared.pending;
    if (o !== null) {
      n.shared.pending = null;
      var d = o, _ = d.next;
      d.next = null, i === null ? u = _ : i.next = _, i = d;
      var T = t.alternate;
      T !== null && (T = T.updateQueue, o = T.lastBaseUpdate, o !== i && (o === null ? T.firstBaseUpdate = _ : o.next = _, T.lastBaseUpdate = d));
    }
    if (u !== null) {
      var M = n.baseState;
      i = 0, T = _ = d = null, o = u;
      do {
        var E = o.lane & -536870913, A = E !== o.lane;
        if (A ? (ft & E) === E : (a & E) === E) {
          E !== 0 && E === wa && (Oc = !0), T !== null && (T = T.next = {
            lane: 0,
            tag: o.tag,
            payload: o.payload,
            callback: null,
            next: null
          });
          t: {
            var G = t, K = o;
            E = e;
            var zt = l;
            switch (K.tag) {
              case 1:
                if (G = K.payload, typeof G == "function") {
                  M = G.call(zt, M, E);
                  break t;
                }
                M = G;
                break t;
              case 3:
                G.flags = G.flags & -65537 | 128;
              case 0:
                if (G = K.payload, E = typeof G == "function" ? G.call(zt, M, E) : G, E == null) break t;
                M = b({}, M, E);
                break t;
              case 2:
                Ol = !0;
            }
          }
          E = o.callback, E !== null && (t.flags |= 64, A && (t.flags |= 8192), A = n.callbacks, A === null ? n.callbacks = [E] : A.push(E));
        } else
          A = {
            lane: E,
            tag: o.tag,
            payload: o.payload,
            callback: o.callback,
            next: null
          }, T === null ? (_ = T = A, d = M) : T = T.next = A, i |= E;
        if (o = o.next, o === null) {
          if (o = n.shared.pending, o === null)
            break;
          A = o, o = A.next, A.next = null, n.lastBaseUpdate = A, n.shared.pending = null;
        }
      } while (!0);
      T === null && (d = M), n.baseState = d, n.firstBaseUpdate = _, n.lastBaseUpdate = T, u === null && (n.shared.lanes = 0), Hl |= i, t.lanes = i, t.memoizedState = M;
    }
  }
  function qr(t, e) {
    if (typeof t != "function")
      throw Error(s(191, t));
    t.call(e);
  }
  function Nr(t, e) {
    var l = t.callbacks;
    if (l !== null)
      for (t.callbacks = null, t = 0; t < l.length; t++)
        qr(l[t], e);
  }
  var $a = m(null), Ju = m(0);
  function Or(t, e) {
    t = ml, H(Ju, t), H($a, e), ml = t | e.baseLanes;
  }
  function Mc() {
    H(Ju, ml), H($a, $a.current);
  }
  function Dc() {
    ml = Ju.current, D($a), D(Ju);
  }
  var _e = m(null), Ce = null;
  function Ul(t) {
    var e = t.alternate;
    H(Ht, Ht.current & 1), H(_e, t), Ce === null && (e === null || $a.current !== null || e.memoizedState !== null) && (Ce = t);
  }
  function Uc(t) {
    H(Ht, Ht.current), H(_e, t), Ce === null && (Ce = t);
  }
  function Mr(t) {
    t.tag === 22 ? (H(Ht, Ht.current), H(_e, t), Ce === null && (Ce = t)) : jl();
  }
  function jl() {
    H(Ht, Ht.current), H(_e, _e.current);
  }
  function Ee(t) {
    D(_e), Ce === t && (Ce = null), D(Ht);
  }
  var Ht = m(0);
  function Ku(t) {
    for (var e = t; e !== null; ) {
      if (e.tag === 13) {
        var l = e.memoizedState;
        if (l !== null && (l = l.dehydrated, l === null || Yf(l) || Gf(l)))
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
  var il = 0, lt = null, At = null, Xt = null, ku = !1, Wa = !1, va = !1, $u = 0, Xn = 0, Fa = null, Cm = 0;
  function jt() {
    throw Error(s(321));
  }
  function jc(t, e) {
    if (e === null) return !1;
    for (var l = 0; l < e.length && l < t.length; l++)
      if (!be(t[l], e[l])) return !1;
    return !0;
  }
  function Cc(t, e, l, a, n, u) {
    return il = u, lt = e, e.memoizedState = null, e.updateQueue = null, e.lanes = 0, z.H = t === null || t.memoizedState === null ? mo : $c, va = !1, u = l(a, n), va = !1, Wa && (u = Ur(
      e,
      l,
      a,
      n
    )), Dr(t), u;
  }
  function Dr(t) {
    z.H = wn;
    var e = At !== null && At.next !== null;
    if (il = 0, Xt = At = lt = null, ku = !1, Xn = 0, Fa = null, e) throw Error(s(300));
    t === null || Zt || (t = t.dependencies, t !== null && Yu(t) && (Zt = !0));
  }
  function Ur(t, e, l, a) {
    lt = t;
    var n = 0;
    do {
      if (Wa && (Fa = null), Xn = 0, Wa = !1, 25 <= n) throw Error(s(301));
      if (n += 1, Xt = At = null, t.updateQueue != null) {
        var u = t.updateQueue;
        u.lastEffect = null, u.events = null, u.stores = null, u.memoCache != null && (u.memoCache.index = 0);
      }
      z.H = ho, u = e(l, a);
    } while (Wa);
    return u;
  }
  function Rm() {
    var t = z.H, e = t.useState()[0];
    return e = typeof e.then == "function" ? Zn(e) : e, t = t.useState()[0], (At !== null ? At.memoizedState : null) !== t && (lt.flags |= 1024), e;
  }
  function Rc() {
    var t = $u !== 0;
    return $u = 0, t;
  }
  function Hc(t, e, l) {
    e.updateQueue = t.updateQueue, e.flags &= -2053, t.lanes &= ~l;
  }
  function Bc(t) {
    if (ku) {
      for (t = t.memoizedState; t !== null; ) {
        var e = t.queue;
        e !== null && (e.pending = null), t = t.next;
      }
      ku = !1;
    }
    il = 0, Xt = At = lt = null, Wa = !1, Xn = $u = 0, Fa = null;
  }
  function se() {
    var t = {
      memoizedState: null,
      baseState: null,
      baseQueue: null,
      queue: null,
      next: null
    };
    return Xt === null ? lt.memoizedState = Xt = t : Xt = Xt.next = t, Xt;
  }
  function Bt() {
    if (At === null) {
      var t = lt.alternate;
      t = t !== null ? t.memoizedState : null;
    } else t = At.next;
    var e = Xt === null ? lt.memoizedState : Xt.next;
    if (e !== null)
      Xt = e, At = t;
    else {
      if (t === null)
        throw lt.alternate === null ? Error(s(467)) : Error(s(310));
      At = t, t = {
        memoizedState: At.memoizedState,
        baseState: At.baseState,
        baseQueue: At.baseQueue,
        queue: At.queue,
        next: null
      }, Xt === null ? lt.memoizedState = Xt = t : Xt = Xt.next = t;
    }
    return Xt;
  }
  function Wu() {
    return { lastEffect: null, events: null, stores: null, memoCache: null };
  }
  function Zn(t) {
    var e = Xn;
    return Xn += 1, Fa === null && (Fa = []), t = Er(Fa, t, e), e = lt, (Xt === null ? e.memoizedState : Xt.next) === null && (e = e.alternate, z.H = e === null || e.memoizedState === null ? mo : $c), t;
  }
  function Fu(t) {
    if (t !== null && typeof t == "object") {
      if (typeof t.then == "function") return Zn(t);
      if (t.$$typeof === tt) return ee(t);
    }
    throw Error(s(438, String(t)));
  }
  function Lc(t) {
    var e = null, l = lt.updateQueue;
    if (l !== null && (e = l.memoCache), e == null) {
      var a = lt.alternate;
      a !== null && (a = a.updateQueue, a !== null && (a = a.memoCache, a != null && (e = {
        data: a.data.map(function(n) {
          return n.slice();
        }),
        index: 0
      })));
    }
    if (e == null && (e = { data: [], index: 0 }), l === null && (l = Wu(), lt.updateQueue = l), l.memoCache = e, l = e.data[e.index], l === void 0)
      for (l = e.data[e.index] = Array(t), a = 0; a < t; a++)
        l[a] = Sl;
    return e.index++, l;
  }
  function cl(t, e) {
    return typeof e == "function" ? e(t) : e;
  }
  function Iu(t) {
    var e = Bt();
    return Yc(e, At, t);
  }
  function Yc(t, e, l) {
    var a = t.queue;
    if (a === null) throw Error(s(311));
    a.lastRenderedReducer = l;
    var n = t.baseQueue, u = a.pending;
    if (u !== null) {
      if (n !== null) {
        var i = n.next;
        n.next = u.next, u.next = i;
      }
      e.baseQueue = n = u, a.pending = null;
    }
    if (u = t.baseState, n === null) t.memoizedState = u;
    else {
      e = n.next;
      var o = i = null, d = null, _ = e, T = !1;
      do {
        var M = _.lane & -536870913;
        if (M !== _.lane ? (ft & M) === M : (il & M) === M) {
          var E = _.revertLane;
          if (E === 0)
            d !== null && (d = d.next = {
              lane: 0,
              revertLane: 0,
              gesture: null,
              action: _.action,
              hasEagerState: _.hasEagerState,
              eagerState: _.eagerState,
              next: null
            }), M === wa && (T = !0);
          else if ((il & E) === E) {
            _ = _.next, E === wa && (T = !0);
            continue;
          } else
            M = {
              lane: 0,
              revertLane: _.revertLane,
              gesture: null,
              action: _.action,
              hasEagerState: _.hasEagerState,
              eagerState: _.eagerState,
              next: null
            }, d === null ? (o = d = M, i = u) : d = d.next = M, lt.lanes |= E, Hl |= E;
          M = _.action, va && l(u, M), u = _.hasEagerState ? _.eagerState : l(u, M);
        } else
          E = {
            lane: M,
            revertLane: _.revertLane,
            gesture: _.gesture,
            action: _.action,
            hasEagerState: _.hasEagerState,
            eagerState: _.eagerState,
            next: null
          }, d === null ? (o = d = E, i = u) : d = d.next = E, lt.lanes |= M, Hl |= M;
        _ = _.next;
      } while (_ !== null && _ !== e);
      if (d === null ? i = u : d.next = o, !be(u, t.memoizedState) && (Zt = !0, T && (l = Ja, l !== null)))
        throw l;
      t.memoizedState = u, t.baseState = i, t.baseQueue = d, a.lastRenderedState = u;
    }
    return n === null && (a.lanes = 0), [t.memoizedState, a.dispatch];
  }
  function Gc(t) {
    var e = Bt(), l = e.queue;
    if (l === null) throw Error(s(311));
    l.lastRenderedReducer = t;
    var a = l.dispatch, n = l.pending, u = e.memoizedState;
    if (n !== null) {
      l.pending = null;
      var i = n = n.next;
      do
        u = t(u, i.action), i = i.next;
      while (i !== n);
      be(u, e.memoizedState) || (Zt = !0), e.memoizedState = u, e.baseQueue === null && (e.baseState = u), l.lastRenderedState = u;
    }
    return [u, a];
  }
  function jr(t, e, l) {
    var a = lt, n = Bt(), u = rt;
    if (u) {
      if (l === void 0) throw Error(s(407));
      l = l();
    } else l = e();
    var i = !be(
      (At || n).memoizedState,
      l
    );
    if (i && (n.memoizedState = l, Zt = !0), n = n.queue, Zc(Hr.bind(null, a, n, t), [
      t
    ]), n.getSnapshot !== e || i || Xt !== null && Xt.memoizedState.tag & 1) {
      if (a.flags |= 2048, Ia(
        9,
        { destroy: void 0 },
        Rr.bind(
          null,
          a,
          n,
          l,
          e
        ),
        null
      ), Tt === null) throw Error(s(349));
      u || (il & 127) !== 0 || Cr(a, e, l);
    }
    return l;
  }
  function Cr(t, e, l) {
    t.flags |= 16384, t = { getSnapshot: e, value: l }, e = lt.updateQueue, e === null ? (e = Wu(), lt.updateQueue = e, e.stores = [t]) : (l = e.stores, l === null ? e.stores = [t] : l.push(t));
  }
  function Rr(t, e, l, a) {
    e.value = l, e.getSnapshot = a, Br(e) && Lr(t);
  }
  function Hr(t, e, l) {
    return l(function() {
      Br(e) && Lr(t);
    });
  }
  function Br(t) {
    var e = t.getSnapshot;
    t = t.value;
    try {
      var l = e();
      return !be(t, l);
    } catch {
      return !0;
    }
  }
  function Lr(t) {
    var e = ca(t, 2);
    e !== null && ve(e, t, 2);
  }
  function Qc(t) {
    var e = se();
    if (typeof t == "function") {
      var l = t;
      if (t = l(), va) {
        L(!0);
        try {
          l();
        } finally {
          L(!1);
        }
      }
    }
    return e.memoizedState = e.baseState = t, e.queue = {
      pending: null,
      lanes: 0,
      dispatch: null,
      lastRenderedReducer: cl,
      lastRenderedState: t
    }, e;
  }
  function Yr(t, e, l, a) {
    return t.baseState = l, Yc(
      t,
      At,
      typeof a == "function" ? a : cl
    );
  }
  function Hm(t, e, l, a, n) {
    if (ei(t)) throw Error(s(485));
    if (t = e.action, t !== null) {
      var u = {
        payload: n,
        action: t,
        next: null,
        isTransition: !0,
        status: "pending",
        value: null,
        reason: null,
        listeners: [],
        then: function(i) {
          u.listeners.push(i);
        }
      };
      z.T !== null ? l(!0) : u.isTransition = !1, a(u), l = e.pending, l === null ? (u.next = e.pending = u, Gr(e, u)) : (u.next = l.next, e.pending = l.next = u);
    }
  }
  function Gr(t, e) {
    var l = e.action, a = e.payload, n = t.state;
    if (e.isTransition) {
      var u = z.T, i = {};
      z.T = i;
      try {
        var o = l(n, a), d = z.S;
        d !== null && d(i, o), Qr(t, e, o);
      } catch (_) {
        Xc(t, e, _);
      } finally {
        u !== null && i.types !== null && (u.types = i.types), z.T = u;
      }
    } else
      try {
        u = l(n, a), Qr(t, e, u);
      } catch (_) {
        Xc(t, e, _);
      }
  }
  function Qr(t, e, l) {
    l !== null && typeof l == "object" && typeof l.then == "function" ? l.then(
      function(a) {
        Xr(t, e, a);
      },
      function(a) {
        return Xc(t, e, a);
      }
    ) : Xr(t, e, l);
  }
  function Xr(t, e, l) {
    e.status = "fulfilled", e.value = l, Zr(e), t.state = l, e = t.pending, e !== null && (l = e.next, l === e ? t.pending = null : (l = l.next, e.next = l, Gr(t, l)));
  }
  function Xc(t, e, l) {
    var a = t.pending;
    if (t.pending = null, a !== null) {
      a = a.next;
      do
        e.status = "rejected", e.reason = l, Zr(e), e = e.next;
      while (e !== a);
    }
    t.action = null;
  }
  function Zr(t) {
    t = t.listeners;
    for (var e = 0; e < t.length; e++) (0, t[e])();
  }
  function Vr(t, e) {
    return e;
  }
  function wr(t, e) {
    if (rt) {
      var l = Tt.formState;
      if (l !== null) {
        t: {
          var a = lt;
          if (rt) {
            if (Ot) {
              e: {
                for (var n = Ot, u = je; n.nodeType !== 8; ) {
                  if (!u) {
                    n = null;
                    break e;
                  }
                  if (n = Re(
                    n.nextSibling
                  ), n === null) {
                    n = null;
                    break e;
                  }
                }
                u = n.data, n = u === "F!" || u === "F" ? n : null;
              }
              if (n) {
                Ot = Re(
                  n.nextSibling
                ), a = n.data === "F!";
                break t;
              }
            }
            ql(a);
          }
          a = !1;
        }
        a && (e = l[0]);
      }
    }
    return l = se(), l.memoizedState = l.baseState = e, a = {
      pending: null,
      lanes: 0,
      dispatch: null,
      lastRenderedReducer: Vr,
      lastRenderedState: e
    }, l.queue = a, l = ro.bind(
      null,
      lt,
      a
    ), a.dispatch = l, a = Qc(!1), u = kc.bind(
      null,
      lt,
      !1,
      a.queue
    ), a = se(), n = {
      state: e,
      dispatch: null,
      action: t,
      pending: null
    }, a.queue = n, l = Hm.bind(
      null,
      lt,
      n,
      u,
      l
    ), n.dispatch = l, a.memoizedState = t, [e, l, !1];
  }
  function Jr(t) {
    var e = Bt();
    return Kr(e, At, t);
  }
  function Kr(t, e, l) {
    if (e = Yc(
      t,
      e,
      Vr
    )[0], t = Iu(cl)[0], typeof e == "object" && e !== null && typeof e.then == "function")
      try {
        var a = Zn(e);
      } catch (i) {
        throw i === Ka ? Xu : i;
      }
    else a = e;
    e = Bt();
    var n = e.queue, u = n.dispatch;
    return l !== e.memoizedState && (lt.flags |= 2048, Ia(
      9,
      { destroy: void 0 },
      Bm.bind(null, n, l),
      null
    )), [a, u, t];
  }
  function Bm(t, e) {
    t.action = e;
  }
  function kr(t) {
    var e = Bt(), l = At;
    if (l !== null)
      return Kr(e, l, t);
    Bt(), e = e.memoizedState, l = Bt();
    var a = l.queue.dispatch;
    return l.memoizedState = t, [e, a, !1];
  }
  function Ia(t, e, l, a) {
    return t = { tag: t, create: l, deps: a, inst: e, next: null }, e = lt.updateQueue, e === null && (e = Wu(), lt.updateQueue = e), l = e.lastEffect, l === null ? e.lastEffect = t.next = t : (a = l.next, l.next = t, t.next = a, e.lastEffect = t), t;
  }
  function $r() {
    return Bt().memoizedState;
  }
  function Pu(t, e, l, a) {
    var n = se();
    lt.flags |= t, n.memoizedState = Ia(
      1 | e,
      { destroy: void 0 },
      l,
      a === void 0 ? null : a
    );
  }
  function ti(t, e, l, a) {
    var n = Bt();
    a = a === void 0 ? null : a;
    var u = n.memoizedState.inst;
    At !== null && a !== null && jc(a, At.memoizedState.deps) ? n.memoizedState = Ia(e, u, l, a) : (lt.flags |= t, n.memoizedState = Ia(
      1 | e,
      u,
      l,
      a
    ));
  }
  function Wr(t, e) {
    Pu(8390656, 8, t, e);
  }
  function Zc(t, e) {
    ti(2048, 8, t, e);
  }
  function Lm(t) {
    lt.flags |= 4;
    var e = lt.updateQueue;
    if (e === null)
      e = Wu(), lt.updateQueue = e, e.events = [t];
    else {
      var l = e.events;
      l === null ? e.events = [t] : l.push(t);
    }
  }
  function Fr(t) {
    var e = Bt().memoizedState;
    return Lm({ ref: e, nextImpl: t }), function() {
      if ((pt & 2) !== 0) throw Error(s(440));
      return e.impl.apply(void 0, arguments);
    };
  }
  function Ir(t, e) {
    return ti(4, 2, t, e);
  }
  function Pr(t, e) {
    return ti(4, 4, t, e);
  }
  function to(t, e) {
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
  function eo(t, e, l) {
    l = l != null ? l.concat([t]) : null, ti(4, 4, to.bind(null, e, t), l);
  }
  function Vc() {
  }
  function lo(t, e) {
    var l = Bt();
    e = e === void 0 ? null : e;
    var a = l.memoizedState;
    return e !== null && jc(e, a[1]) ? a[0] : (l.memoizedState = [t, e], t);
  }
  function ao(t, e) {
    var l = Bt();
    e = e === void 0 ? null : e;
    var a = l.memoizedState;
    if (e !== null && jc(e, a[1]))
      return a[0];
    if (a = t(), va) {
      L(!0);
      try {
        t();
      } finally {
        L(!1);
      }
    }
    return l.memoizedState = [a, e], a;
  }
  function wc(t, e, l) {
    return l === void 0 || (il & 1073741824) !== 0 && (ft & 261930) === 0 ? t.memoizedState = e : (t.memoizedState = l, t = ud(), lt.lanes |= t, Hl |= t, l);
  }
  function no(t, e, l, a) {
    return be(l, e) ? l : $a.current !== null ? (t = wc(t, l, a), be(t, e) || (Zt = !0), t) : (il & 42) === 0 || (il & 1073741824) !== 0 && (ft & 261930) === 0 ? (Zt = !0, t.memoizedState = l) : (t = ud(), lt.lanes |= t, Hl |= t, e);
  }
  function uo(t, e, l, a, n) {
    var u = R.p;
    R.p = u !== 0 && 8 > u ? u : 8;
    var i = z.T, o = {};
    z.T = o, kc(t, !1, e, l);
    try {
      var d = n(), _ = z.S;
      if (_ !== null && _(o, d), d !== null && typeof d == "object" && typeof d.then == "function") {
        var T = jm(
          d,
          a
        );
        Vn(
          t,
          e,
          T,
          ze(t)
        );
      } else
        Vn(
          t,
          e,
          a,
          ze(t)
        );
    } catch (M) {
      Vn(
        t,
        e,
        { then: function() {
        }, status: "rejected", reason: M },
        ze()
      );
    } finally {
      R.p = u, i !== null && o.types !== null && (i.types = o.types), z.T = i;
    }
  }
  function Ym() {
  }
  function Jc(t, e, l, a) {
    if (t.tag !== 5) throw Error(s(476));
    var n = io(t).queue;
    uo(
      t,
      n,
      e,
      $,
      l === null ? Ym : function() {
        return co(t), l(a);
      }
    );
  }
  function io(t) {
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
        lastRenderedReducer: cl,
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
        lastRenderedReducer: cl,
        lastRenderedState: l
      },
      next: null
    }, t.memoizedState = e, t = t.alternate, t !== null && (t.memoizedState = e), e;
  }
  function co(t) {
    var e = io(t);
    e.next === null && (e = t.alternate.memoizedState), Vn(
      t,
      e.next.queue,
      {},
      ze()
    );
  }
  function Kc() {
    return ee(cu);
  }
  function fo() {
    return Bt().memoizedState;
  }
  function so() {
    return Bt().memoizedState;
  }
  function Gm(t) {
    for (var e = t.return; e !== null; ) {
      switch (e.tag) {
        case 24:
        case 3:
          var l = ze();
          t = Ml(l);
          var a = Dl(e, t, l);
          a !== null && (ve(a, e, l), Yn(a, e, l)), e = { cache: Ec() }, t.payload = e;
          return;
      }
      e = e.return;
    }
  }
  function Qm(t, e, l) {
    var a = ze();
    l = {
      lane: a,
      revertLane: 0,
      gesture: null,
      action: l,
      hasEagerState: !1,
      eagerState: null,
      next: null
    }, ei(t) ? oo(e, l) : (l = oc(t, e, l, a), l !== null && (ve(l, t, a), yo(l, e, a)));
  }
  function ro(t, e, l) {
    var a = ze();
    Vn(t, e, l, a);
  }
  function Vn(t, e, l, a) {
    var n = {
      lane: a,
      revertLane: 0,
      gesture: null,
      action: l,
      hasEagerState: !1,
      eagerState: null,
      next: null
    };
    if (ei(t)) oo(e, n);
    else {
      var u = t.alternate;
      if (t.lanes === 0 && (u === null || u.lanes === 0) && (u = e.lastRenderedReducer, u !== null))
        try {
          var i = e.lastRenderedState, o = u(i, l);
          if (n.hasEagerState = !0, n.eagerState = o, be(o, i))
            return Ru(t, e, n, 0), Tt === null && Cu(), !1;
        } catch {
        } finally {
        }
      if (l = oc(t, e, n, a), l !== null)
        return ve(l, t, a), yo(l, e, a), !0;
    }
    return !1;
  }
  function kc(t, e, l, a) {
    if (a = {
      lane: 2,
      revertLane: qf(),
      gesture: null,
      action: a,
      hasEagerState: !1,
      eagerState: null,
      next: null
    }, ei(t)) {
      if (e) throw Error(s(479));
    } else
      e = oc(
        t,
        l,
        a,
        2
      ), e !== null && ve(e, t, 2);
  }
  function ei(t) {
    var e = t.alternate;
    return t === lt || e !== null && e === lt;
  }
  function oo(t, e) {
    Wa = ku = !0;
    var l = t.pending;
    l === null ? e.next = e : (e.next = l.next, l.next = e), t.pending = e;
  }
  function yo(t, e, l) {
    if ((l & 4194048) !== 0) {
      var a = e.lanes;
      a &= t.pendingLanes, l |= a, e.lanes = l, gs(t, l);
    }
  }
  var wn = {
    readContext: ee,
    use: Fu,
    useCallback: jt,
    useContext: jt,
    useEffect: jt,
    useImperativeHandle: jt,
    useLayoutEffect: jt,
    useInsertionEffect: jt,
    useMemo: jt,
    useReducer: jt,
    useRef: jt,
    useState: jt,
    useDebugValue: jt,
    useDeferredValue: jt,
    useTransition: jt,
    useSyncExternalStore: jt,
    useId: jt,
    useHostTransitionStatus: jt,
    useFormState: jt,
    useActionState: jt,
    useOptimistic: jt,
    useMemoCache: jt,
    useCacheRefresh: jt
  };
  wn.useEffectEvent = jt;
  var mo = {
    readContext: ee,
    use: Fu,
    useCallback: function(t, e) {
      return se().memoizedState = [
        t,
        e === void 0 ? null : e
      ], t;
    },
    useContext: ee,
    useEffect: Wr,
    useImperativeHandle: function(t, e, l) {
      l = l != null ? l.concat([t]) : null, Pu(
        4194308,
        4,
        to.bind(null, e, t),
        l
      );
    },
    useLayoutEffect: function(t, e) {
      return Pu(4194308, 4, t, e);
    },
    useInsertionEffect: function(t, e) {
      Pu(4, 2, t, e);
    },
    useMemo: function(t, e) {
      var l = se();
      e = e === void 0 ? null : e;
      var a = t();
      if (va) {
        L(!0);
        try {
          t();
        } finally {
          L(!1);
        }
      }
      return l.memoizedState = [a, e], a;
    },
    useReducer: function(t, e, l) {
      var a = se();
      if (l !== void 0) {
        var n = l(e);
        if (va) {
          L(!0);
          try {
            l(e);
          } finally {
            L(!1);
          }
        }
      } else n = e;
      return a.memoizedState = a.baseState = n, t = {
        pending: null,
        lanes: 0,
        dispatch: null,
        lastRenderedReducer: t,
        lastRenderedState: n
      }, a.queue = t, t = t.dispatch = Qm.bind(
        null,
        lt,
        t
      ), [a.memoizedState, t];
    },
    useRef: function(t) {
      var e = se();
      return t = { current: t }, e.memoizedState = t;
    },
    useState: function(t) {
      t = Qc(t);
      var e = t.queue, l = ro.bind(null, lt, e);
      return e.dispatch = l, [t.memoizedState, l];
    },
    useDebugValue: Vc,
    useDeferredValue: function(t, e) {
      var l = se();
      return wc(l, t, e);
    },
    useTransition: function() {
      var t = Qc(!1);
      return t = uo.bind(
        null,
        lt,
        t.queue,
        !0,
        !1
      ), se().memoizedState = t, [!1, t];
    },
    useSyncExternalStore: function(t, e, l) {
      var a = lt, n = se();
      if (rt) {
        if (l === void 0)
          throw Error(s(407));
        l = l();
      } else {
        if (l = e(), Tt === null)
          throw Error(s(349));
        (ft & 127) !== 0 || Cr(a, e, l);
      }
      n.memoizedState = l;
      var u = { value: l, getSnapshot: e };
      return n.queue = u, Wr(Hr.bind(null, a, u, t), [
        t
      ]), a.flags |= 2048, Ia(
        9,
        { destroy: void 0 },
        Rr.bind(
          null,
          a,
          u,
          l,
          e
        ),
        null
      ), l;
    },
    useId: function() {
      var t = se(), e = Tt.identifierPrefix;
      if (rt) {
        var l = ke, a = Ke;
        l = (a & ~(1 << 32 - et(a) - 1)).toString(32) + l, e = "_" + e + "R_" + l, l = $u++, 0 < l && (e += "H" + l.toString(32)), e += "_";
      } else
        l = Cm++, e = "_" + e + "r_" + l.toString(32) + "_";
      return t.memoizedState = e;
    },
    useHostTransitionStatus: Kc,
    useFormState: wr,
    useActionState: wr,
    useOptimistic: function(t) {
      var e = se();
      e.memoizedState = e.baseState = t;
      var l = {
        pending: null,
        lanes: 0,
        dispatch: null,
        lastRenderedReducer: null,
        lastRenderedState: null
      };
      return e.queue = l, e = kc.bind(
        null,
        lt,
        !0,
        l
      ), l.dispatch = e, [t, e];
    },
    useMemoCache: Lc,
    useCacheRefresh: function() {
      return se().memoizedState = Gm.bind(
        null,
        lt
      );
    },
    useEffectEvent: function(t) {
      var e = se(), l = { impl: t };
      return e.memoizedState = l, function() {
        if ((pt & 2) !== 0)
          throw Error(s(440));
        return l.impl.apply(void 0, arguments);
      };
    }
  }, $c = {
    readContext: ee,
    use: Fu,
    useCallback: lo,
    useContext: ee,
    useEffect: Zc,
    useImperativeHandle: eo,
    useInsertionEffect: Ir,
    useLayoutEffect: Pr,
    useMemo: ao,
    useReducer: Iu,
    useRef: $r,
    useState: function() {
      return Iu(cl);
    },
    useDebugValue: Vc,
    useDeferredValue: function(t, e) {
      var l = Bt();
      return no(
        l,
        At.memoizedState,
        t,
        e
      );
    },
    useTransition: function() {
      var t = Iu(cl)[0], e = Bt().memoizedState;
      return [
        typeof t == "boolean" ? t : Zn(t),
        e
      ];
    },
    useSyncExternalStore: jr,
    useId: fo,
    useHostTransitionStatus: Kc,
    useFormState: Jr,
    useActionState: Jr,
    useOptimistic: function(t, e) {
      var l = Bt();
      return Yr(l, At, t, e);
    },
    useMemoCache: Lc,
    useCacheRefresh: so
  };
  $c.useEffectEvent = Fr;
  var ho = {
    readContext: ee,
    use: Fu,
    useCallback: lo,
    useContext: ee,
    useEffect: Zc,
    useImperativeHandle: eo,
    useInsertionEffect: Ir,
    useLayoutEffect: Pr,
    useMemo: ao,
    useReducer: Gc,
    useRef: $r,
    useState: function() {
      return Gc(cl);
    },
    useDebugValue: Vc,
    useDeferredValue: function(t, e) {
      var l = Bt();
      return At === null ? wc(l, t, e) : no(
        l,
        At.memoizedState,
        t,
        e
      );
    },
    useTransition: function() {
      var t = Gc(cl)[0], e = Bt().memoizedState;
      return [
        typeof t == "boolean" ? t : Zn(t),
        e
      ];
    },
    useSyncExternalStore: jr,
    useId: fo,
    useHostTransitionStatus: Kc,
    useFormState: kr,
    useActionState: kr,
    useOptimistic: function(t, e) {
      var l = Bt();
      return At !== null ? Yr(l, At, t, e) : (l.baseState = t, [t, l.queue.dispatch]);
    },
    useMemoCache: Lc,
    useCacheRefresh: so
  };
  ho.useEffectEvent = Fr;
  function Wc(t, e, l, a) {
    e = t.memoizedState, l = l(a, e), l = l == null ? e : b({}, e, l), t.memoizedState = l, t.lanes === 0 && (t.updateQueue.baseState = l);
  }
  var Fc = {
    enqueueSetState: function(t, e, l) {
      t = t._reactInternals;
      var a = ze(), n = Ml(a);
      n.payload = e, l != null && (n.callback = l), e = Dl(t, n, a), e !== null && (ve(e, t, a), Yn(e, t, a));
    },
    enqueueReplaceState: function(t, e, l) {
      t = t._reactInternals;
      var a = ze(), n = Ml(a);
      n.tag = 1, n.payload = e, l != null && (n.callback = l), e = Dl(t, n, a), e !== null && (ve(e, t, a), Yn(e, t, a));
    },
    enqueueForceUpdate: function(t, e) {
      t = t._reactInternals;
      var l = ze(), a = Ml(l);
      a.tag = 2, e != null && (a.callback = e), e = Dl(t, a, l), e !== null && (ve(e, t, l), Yn(e, t, l));
    }
  };
  function vo(t, e, l, a, n, u, i) {
    return t = t.stateNode, typeof t.shouldComponentUpdate == "function" ? t.shouldComponentUpdate(a, u, i) : e.prototype && e.prototype.isPureReactComponent ? !Dn(l, a) || !Dn(n, u) : !0;
  }
  function go(t, e, l, a) {
    t = e.state, typeof e.componentWillReceiveProps == "function" && e.componentWillReceiveProps(l, a), typeof e.UNSAFE_componentWillReceiveProps == "function" && e.UNSAFE_componentWillReceiveProps(l, a), e.state !== t && Fc.enqueueReplaceState(e, e.state, null);
  }
  function ga(t, e) {
    var l = e;
    if ("ref" in e) {
      l = {};
      for (var a in e)
        a !== "ref" && (l[a] = e[a]);
    }
    if (t = t.defaultProps) {
      l === e && (l = b({}, l));
      for (var n in t)
        l[n] === void 0 && (l[n] = t[n]);
    }
    return l;
  }
  function po(t) {
    ju(t);
  }
  function bo(t) {
    console.error(t);
  }
  function So(t) {
    ju(t);
  }
  function li(t, e) {
    try {
      var l = t.onUncaughtError;
      l(e.value, { componentStack: e.stack });
    } catch (a) {
      setTimeout(function() {
        throw a;
      });
    }
  }
  function _o(t, e, l) {
    try {
      var a = t.onCaughtError;
      a(l.value, {
        componentStack: l.stack,
        errorBoundary: e.tag === 1 ? e.stateNode : null
      });
    } catch (n) {
      setTimeout(function() {
        throw n;
      });
    }
  }
  function Ic(t, e, l) {
    return l = Ml(l), l.tag = 3, l.payload = { element: null }, l.callback = function() {
      li(t, e);
    }, l;
  }
  function Eo(t) {
    return t = Ml(t), t.tag = 3, t;
  }
  function Ao(t, e, l, a) {
    var n = l.type.getDerivedStateFromError;
    if (typeof n == "function") {
      var u = a.value;
      t.payload = function() {
        return n(u);
      }, t.callback = function() {
        _o(e, l, a);
      };
    }
    var i = l.stateNode;
    i !== null && typeof i.componentDidCatch == "function" && (t.callback = function() {
      _o(e, l, a), typeof n != "function" && (Bl === null ? Bl = /* @__PURE__ */ new Set([this]) : Bl.add(this));
      var o = a.stack;
      this.componentDidCatch(a.value, {
        componentStack: o !== null ? o : ""
      });
    });
  }
  function Xm(t, e, l, a, n) {
    if (l.flags |= 32768, a !== null && typeof a == "object" && typeof a.then == "function") {
      if (e = l.alternate, e !== null && Va(
        e,
        l,
        n,
        !0
      ), l = _e.current, l !== null) {
        switch (l.tag) {
          case 31:
          case 13:
            return Ce === null ? mi() : l.alternate === null && Ct === 0 && (Ct = 3), l.flags &= -257, l.flags |= 65536, l.lanes = n, a === Zu ? l.flags |= 16384 : (e = l.updateQueue, e === null ? l.updateQueue = /* @__PURE__ */ new Set([a]) : e.add(a), xf(t, a, n)), !1;
          case 22:
            return l.flags |= 65536, a === Zu ? l.flags |= 16384 : (e = l.updateQueue, e === null ? (e = {
              transitions: null,
              markerInstances: null,
              retryQueue: /* @__PURE__ */ new Set([a])
            }, l.updateQueue = e) : (l = e.retryQueue, l === null ? e.retryQueue = /* @__PURE__ */ new Set([a]) : l.add(a)), xf(t, a, n)), !1;
        }
        throw Error(s(435, l.tag));
      }
      return xf(t, a, n), mi(), !1;
    }
    if (rt)
      return e = _e.current, e !== null ? ((e.flags & 65536) === 0 && (e.flags |= 256), e.flags |= 65536, e.lanes = n, a !== gc && (t = Error(s(422), { cause: a }), Cn(Me(t, l)))) : (a !== gc && (e = Error(s(423), {
        cause: a
      }), Cn(
        Me(e, l)
      )), t = t.current.alternate, t.flags |= 65536, n &= -n, t.lanes |= n, a = Me(a, l), n = Ic(
        t.stateNode,
        a,
        n
      ), Nc(t, n), Ct !== 4 && (Ct = 2)), !1;
    var u = Error(s(520), { cause: a });
    if (u = Me(u, l), Pn === null ? Pn = [u] : Pn.push(u), Ct !== 4 && (Ct = 2), e === null) return !0;
    a = Me(a, l), l = e;
    do {
      switch (l.tag) {
        case 3:
          return l.flags |= 65536, t = n & -n, l.lanes |= t, t = Ic(l.stateNode, a, t), Nc(l, t), !1;
        case 1:
          if (e = l.type, u = l.stateNode, (l.flags & 128) === 0 && (typeof e.getDerivedStateFromError == "function" || u !== null && typeof u.componentDidCatch == "function" && (Bl === null || !Bl.has(u))))
            return l.flags |= 65536, n &= -n, l.lanes |= n, n = Eo(n), Ao(
              n,
              t,
              l,
              a
            ), Nc(l, n), !1;
      }
      l = l.return;
    } while (l !== null);
    return !1;
  }
  var Pc = Error(s(461)), Zt = !1;
  function le(t, e, l, a) {
    e.child = t === null ? Tr(e, null, l, a) : ha(
      e,
      t.child,
      l,
      a
    );
  }
  function xo(t, e, l, a, n) {
    l = l.render;
    var u = e.ref;
    if ("ref" in a) {
      var i = {};
      for (var o in a)
        o !== "ref" && (i[o] = a[o]);
    } else i = a;
    return oa(e), a = Cc(
      t,
      e,
      l,
      i,
      u,
      n
    ), o = Rc(), t !== null && !Zt ? (Hc(t, e, n), fl(t, e, n)) : (rt && o && hc(e), e.flags |= 1, le(t, e, a, n), e.child);
  }
  function zo(t, e, l, a, n) {
    if (t === null) {
      var u = l.type;
      return typeof u == "function" && !dc(u) && u.defaultProps === void 0 && l.compare === null ? (e.tag = 15, e.type = u, To(
        t,
        e,
        u,
        a,
        n
      )) : (t = Bu(
        l.type,
        null,
        a,
        e,
        e.mode,
        n
      ), t.ref = e.ref, t.return = e, e.child = t);
    }
    if (u = t.child, !ff(t, n)) {
      var i = u.memoizedProps;
      if (l = l.compare, l = l !== null ? l : Dn, l(i, a) && t.ref === e.ref)
        return fl(t, e, n);
    }
    return e.flags |= 1, t = ll(u, a), t.ref = e.ref, t.return = e, e.child = t;
  }
  function To(t, e, l, a, n) {
    if (t !== null) {
      var u = t.memoizedProps;
      if (Dn(u, a) && t.ref === e.ref)
        if (Zt = !1, e.pendingProps = a = u, ff(t, n))
          (t.flags & 131072) !== 0 && (Zt = !0);
        else
          return e.lanes = t.lanes, fl(t, e, n);
    }
    return tf(
      t,
      e,
      l,
      a,
      n
    );
  }
  function qo(t, e, l, a) {
    var n = a.children, u = t !== null ? t.memoizedState : null;
    if (t === null && e.stateNode === null && (e.stateNode = {
      _visibility: 1,
      _pendingMarkers: null,
      _retryCache: null,
      _transitions: null
    }), a.mode === "hidden") {
      if ((e.flags & 128) !== 0) {
        if (u = u !== null ? u.baseLanes | l : l, t !== null) {
          for (a = e.child = t.child, n = 0; a !== null; )
            n = n | a.lanes | a.childLanes, a = a.sibling;
          a = n & ~u;
        } else a = 0, e.child = null;
        return No(
          t,
          e,
          u,
          l,
          a
        );
      }
      if ((l & 536870912) !== 0)
        e.memoizedState = { baseLanes: 0, cachePool: null }, t !== null && Qu(
          e,
          u !== null ? u.cachePool : null
        ), u !== null ? Or(e, u) : Mc(), Mr(e);
      else
        return a = e.lanes = 536870912, No(
          t,
          e,
          u !== null ? u.baseLanes | l : l,
          l,
          a
        );
    } else
      u !== null ? (Qu(e, u.cachePool), Or(e, u), jl(), e.memoizedState = null) : (t !== null && Qu(e, null), Mc(), jl());
    return le(t, e, n, l), e.child;
  }
  function Jn(t, e) {
    return t !== null && t.tag === 22 || e.stateNode !== null || (e.stateNode = {
      _visibility: 1,
      _pendingMarkers: null,
      _retryCache: null,
      _transitions: null
    }), e.sibling;
  }
  function No(t, e, l, a, n) {
    var u = xc();
    return u = u === null ? null : { parent: Qt._currentValue, pool: u }, e.memoizedState = {
      baseLanes: l,
      cachePool: u
    }, t !== null && Qu(e, null), Mc(), Mr(e), t !== null && Va(t, e, a, !0), e.childLanes = n, null;
  }
  function ai(t, e) {
    return e = ui(
      { mode: e.mode, children: e.children },
      t.mode
    ), e.ref = t.ref, t.child = e, e.return = t, e;
  }
  function Oo(t, e, l) {
    return ha(e, t.child, null, l), t = ai(e, e.pendingProps), t.flags |= 2, Ee(e), e.memoizedState = null, t;
  }
  function Zm(t, e, l) {
    var a = e.pendingProps, n = (e.flags & 128) !== 0;
    if (e.flags &= -129, t === null) {
      if (rt) {
        if (a.mode === "hidden")
          return t = ai(e, a), e.lanes = 536870912, Jn(null, t);
        if (Uc(e), (t = Ot) ? (t = Qd(
          t,
          je
        ), t = t !== null && t.data === "&" ? t : null, t !== null && (e.memoizedState = {
          dehydrated: t,
          treeContext: zl !== null ? { id: Ke, overflow: ke } : null,
          retryLane: 536870912,
          hydrationErrors: null
        }, l = or(t), l.return = e, e.child = l, te = e, Ot = null)) : t = null, t === null) throw ql(e);
        return e.lanes = 536870912, null;
      }
      return ai(e, a);
    }
    var u = t.memoizedState;
    if (u !== null) {
      var i = u.dehydrated;
      if (Uc(e), n)
        if (e.flags & 256)
          e.flags &= -257, e = Oo(
            t,
            e,
            l
          );
        else if (e.memoizedState !== null)
          e.child = t.child, e.flags |= 128, e = null;
        else throw Error(s(558));
      else if (Zt || Va(t, e, l, !1), n = (l & t.childLanes) !== 0, Zt || n) {
        if (a = Tt, a !== null && (i = ps(a, l), i !== 0 && i !== u.retryLane))
          throw u.retryLane = i, ca(t, i), ve(a, t, i), Pc;
        mi(), e = Oo(
          t,
          e,
          l
        );
      } else
        t = u.treeContext, Ot = Re(i.nextSibling), te = e, rt = !0, Tl = null, je = !1, t !== null && mr(e, t), e = ai(e, a), e.flags |= 4096;
      return e;
    }
    return t = ll(t.child, {
      mode: a.mode,
      children: a.children
    }), t.ref = e.ref, e.child = t, t.return = e, t;
  }
  function ni(t, e) {
    var l = e.ref;
    if (l === null)
      t !== null && t.ref !== null && (e.flags |= 4194816);
    else {
      if (typeof l != "function" && typeof l != "object")
        throw Error(s(284));
      (t === null || t.ref !== l) && (e.flags |= 4194816);
    }
  }
  function tf(t, e, l, a, n) {
    return oa(e), l = Cc(
      t,
      e,
      l,
      a,
      void 0,
      n
    ), a = Rc(), t !== null && !Zt ? (Hc(t, e, n), fl(t, e, n)) : (rt && a && hc(e), e.flags |= 1, le(t, e, l, n), e.child);
  }
  function Mo(t, e, l, a, n, u) {
    return oa(e), e.updateQueue = null, l = Ur(
      e,
      a,
      l,
      n
    ), Dr(t), a = Rc(), t !== null && !Zt ? (Hc(t, e, u), fl(t, e, u)) : (rt && a && hc(e), e.flags |= 1, le(t, e, l, u), e.child);
  }
  function Do(t, e, l, a, n) {
    if (oa(e), e.stateNode === null) {
      var u = Ga, i = l.contextType;
      typeof i == "object" && i !== null && (u = ee(i)), u = new l(a, u), e.memoizedState = u.state !== null && u.state !== void 0 ? u.state : null, u.updater = Fc, e.stateNode = u, u._reactInternals = e, u = e.stateNode, u.props = a, u.state = e.memoizedState, u.refs = {}, Tc(e), i = l.contextType, u.context = typeof i == "object" && i !== null ? ee(i) : Ga, u.state = e.memoizedState, i = l.getDerivedStateFromProps, typeof i == "function" && (Wc(
        e,
        l,
        i,
        a
      ), u.state = e.memoizedState), typeof l.getDerivedStateFromProps == "function" || typeof u.getSnapshotBeforeUpdate == "function" || typeof u.UNSAFE_componentWillMount != "function" && typeof u.componentWillMount != "function" || (i = u.state, typeof u.componentWillMount == "function" && u.componentWillMount(), typeof u.UNSAFE_componentWillMount == "function" && u.UNSAFE_componentWillMount(), i !== u.state && Fc.enqueueReplaceState(u, u.state, null), Qn(e, a, u, n), Gn(), u.state = e.memoizedState), typeof u.componentDidMount == "function" && (e.flags |= 4194308), a = !0;
    } else if (t === null) {
      u = e.stateNode;
      var o = e.memoizedProps, d = ga(l, o);
      u.props = d;
      var _ = u.context, T = l.contextType;
      i = Ga, typeof T == "object" && T !== null && (i = ee(T));
      var M = l.getDerivedStateFromProps;
      T = typeof M == "function" || typeof u.getSnapshotBeforeUpdate == "function", o = e.pendingProps !== o, T || typeof u.UNSAFE_componentWillReceiveProps != "function" && typeof u.componentWillReceiveProps != "function" || (o || _ !== i) && go(
        e,
        u,
        a,
        i
      ), Ol = !1;
      var E = e.memoizedState;
      u.state = E, Qn(e, a, u, n), Gn(), _ = e.memoizedState, o || E !== _ || Ol ? (typeof M == "function" && (Wc(
        e,
        l,
        M,
        a
      ), _ = e.memoizedState), (d = Ol || vo(
        e,
        l,
        d,
        a,
        E,
        _,
        i
      )) ? (T || typeof u.UNSAFE_componentWillMount != "function" && typeof u.componentWillMount != "function" || (typeof u.componentWillMount == "function" && u.componentWillMount(), typeof u.UNSAFE_componentWillMount == "function" && u.UNSAFE_componentWillMount()), typeof u.componentDidMount == "function" && (e.flags |= 4194308)) : (typeof u.componentDidMount == "function" && (e.flags |= 4194308), e.memoizedProps = a, e.memoizedState = _), u.props = a, u.state = _, u.context = i, a = d) : (typeof u.componentDidMount == "function" && (e.flags |= 4194308), a = !1);
    } else {
      u = e.stateNode, qc(t, e), i = e.memoizedProps, T = ga(l, i), u.props = T, M = e.pendingProps, E = u.context, _ = l.contextType, d = Ga, typeof _ == "object" && _ !== null && (d = ee(_)), o = l.getDerivedStateFromProps, (_ = typeof o == "function" || typeof u.getSnapshotBeforeUpdate == "function") || typeof u.UNSAFE_componentWillReceiveProps != "function" && typeof u.componentWillReceiveProps != "function" || (i !== M || E !== d) && go(
        e,
        u,
        a,
        d
      ), Ol = !1, E = e.memoizedState, u.state = E, Qn(e, a, u, n), Gn();
      var A = e.memoizedState;
      i !== M || E !== A || Ol || t !== null && t.dependencies !== null && Yu(t.dependencies) ? (typeof o == "function" && (Wc(
        e,
        l,
        o,
        a
      ), A = e.memoizedState), (T = Ol || vo(
        e,
        l,
        T,
        a,
        E,
        A,
        d
      ) || t !== null && t.dependencies !== null && Yu(t.dependencies)) ? (_ || typeof u.UNSAFE_componentWillUpdate != "function" && typeof u.componentWillUpdate != "function" || (typeof u.componentWillUpdate == "function" && u.componentWillUpdate(a, A, d), typeof u.UNSAFE_componentWillUpdate == "function" && u.UNSAFE_componentWillUpdate(
        a,
        A,
        d
      )), typeof u.componentDidUpdate == "function" && (e.flags |= 4), typeof u.getSnapshotBeforeUpdate == "function" && (e.flags |= 1024)) : (typeof u.componentDidUpdate != "function" || i === t.memoizedProps && E === t.memoizedState || (e.flags |= 4), typeof u.getSnapshotBeforeUpdate != "function" || i === t.memoizedProps && E === t.memoizedState || (e.flags |= 1024), e.memoizedProps = a, e.memoizedState = A), u.props = a, u.state = A, u.context = d, a = T) : (typeof u.componentDidUpdate != "function" || i === t.memoizedProps && E === t.memoizedState || (e.flags |= 4), typeof u.getSnapshotBeforeUpdate != "function" || i === t.memoizedProps && E === t.memoizedState || (e.flags |= 1024), a = !1);
    }
    return u = a, ni(t, e), a = (e.flags & 128) !== 0, u || a ? (u = e.stateNode, l = a && typeof l.getDerivedStateFromError != "function" ? null : u.render(), e.flags |= 1, t !== null && a ? (e.child = ha(
      e,
      t.child,
      null,
      n
    ), e.child = ha(
      e,
      null,
      l,
      n
    )) : le(t, e, l, n), e.memoizedState = u.state, t = e.child) : t = fl(
      t,
      e,
      n
    ), t;
  }
  function Uo(t, e, l, a) {
    return sa(), e.flags |= 256, le(t, e, l, a), e.child;
  }
  var ef = {
    dehydrated: null,
    treeContext: null,
    retryLane: 0,
    hydrationErrors: null
  };
  function lf(t) {
    return { baseLanes: t, cachePool: Sr() };
  }
  function af(t, e, l) {
    return t = t !== null ? t.childLanes & ~l : 0, e && (t |= xe), t;
  }
  function jo(t, e, l) {
    var a = e.pendingProps, n = !1, u = (e.flags & 128) !== 0, i;
    if ((i = u) || (i = t !== null && t.memoizedState === null ? !1 : (Ht.current & 2) !== 0), i && (n = !0, e.flags &= -129), i = (e.flags & 32) !== 0, e.flags &= -33, t === null) {
      if (rt) {
        if (n ? Ul(e) : jl(), (t = Ot) ? (t = Qd(
          t,
          je
        ), t = t !== null && t.data !== "&" ? t : null, t !== null && (e.memoizedState = {
          dehydrated: t,
          treeContext: zl !== null ? { id: Ke, overflow: ke } : null,
          retryLane: 536870912,
          hydrationErrors: null
        }, l = or(t), l.return = e, e.child = l, te = e, Ot = null)) : t = null, t === null) throw ql(e);
        return Gf(t) ? e.lanes = 32 : e.lanes = 536870912, null;
      }
      var o = a.children;
      return a = a.fallback, n ? (jl(), n = e.mode, o = ui(
        { mode: "hidden", children: o },
        n
      ), a = fa(
        a,
        n,
        l,
        null
      ), o.return = e, a.return = e, o.sibling = a, e.child = o, a = e.child, a.memoizedState = lf(l), a.childLanes = af(
        t,
        i,
        l
      ), e.memoizedState = ef, Jn(null, a)) : (Ul(e), nf(e, o));
    }
    var d = t.memoizedState;
    if (d !== null && (o = d.dehydrated, o !== null)) {
      if (u)
        e.flags & 256 ? (Ul(e), e.flags &= -257, e = uf(
          t,
          e,
          l
        )) : e.memoizedState !== null ? (jl(), e.child = t.child, e.flags |= 128, e = null) : (jl(), o = a.fallback, n = e.mode, a = ui(
          { mode: "visible", children: a.children },
          n
        ), o = fa(
          o,
          n,
          l,
          null
        ), o.flags |= 2, a.return = e, o.return = e, a.sibling = o, e.child = a, ha(
          e,
          t.child,
          null,
          l
        ), a = e.child, a.memoizedState = lf(l), a.childLanes = af(
          t,
          i,
          l
        ), e.memoizedState = ef, e = Jn(null, a));
      else if (Ul(e), Gf(o)) {
        if (i = o.nextSibling && o.nextSibling.dataset, i) var _ = i.dgst;
        i = _, a = Error(s(419)), a.stack = "", a.digest = i, Cn({ value: a, source: null, stack: null }), e = uf(
          t,
          e,
          l
        );
      } else if (Zt || Va(t, e, l, !1), i = (l & t.childLanes) !== 0, Zt || i) {
        if (i = Tt, i !== null && (a = ps(i, l), a !== 0 && a !== d.retryLane))
          throw d.retryLane = a, ca(t, a), ve(i, t, a), Pc;
        Yf(o) || mi(), e = uf(
          t,
          e,
          l
        );
      } else
        Yf(o) ? (e.flags |= 192, e.child = t.child, e = null) : (t = d.treeContext, Ot = Re(
          o.nextSibling
        ), te = e, rt = !0, Tl = null, je = !1, t !== null && mr(e, t), e = nf(
          e,
          a.children
        ), e.flags |= 4096);
      return e;
    }
    return n ? (jl(), o = a.fallback, n = e.mode, d = t.child, _ = d.sibling, a = ll(d, {
      mode: "hidden",
      children: a.children
    }), a.subtreeFlags = d.subtreeFlags & 65011712, _ !== null ? o = ll(
      _,
      o
    ) : (o = fa(
      o,
      n,
      l,
      null
    ), o.flags |= 2), o.return = e, a.return = e, a.sibling = o, e.child = a, Jn(null, a), a = e.child, o = t.child.memoizedState, o === null ? o = lf(l) : (n = o.cachePool, n !== null ? (d = Qt._currentValue, n = n.parent !== d ? { parent: d, pool: d } : n) : n = Sr(), o = {
      baseLanes: o.baseLanes | l,
      cachePool: n
    }), a.memoizedState = o, a.childLanes = af(
      t,
      i,
      l
    ), e.memoizedState = ef, Jn(t.child, a)) : (Ul(e), l = t.child, t = l.sibling, l = ll(l, {
      mode: "visible",
      children: a.children
    }), l.return = e, l.sibling = null, t !== null && (i = e.deletions, i === null ? (e.deletions = [t], e.flags |= 16) : i.push(t)), e.child = l, e.memoizedState = null, l);
  }
  function nf(t, e) {
    return e = ui(
      { mode: "visible", children: e },
      t.mode
    ), e.return = t, t.child = e;
  }
  function ui(t, e) {
    return t = Se(22, t, null, e), t.lanes = 0, t;
  }
  function uf(t, e, l) {
    return ha(e, t.child, null, l), t = nf(
      e,
      e.pendingProps.children
    ), t.flags |= 2, e.memoizedState = null, t;
  }
  function Co(t, e, l) {
    t.lanes |= e;
    var a = t.alternate;
    a !== null && (a.lanes |= e), Sc(t.return, e, l);
  }
  function cf(t, e, l, a, n, u) {
    var i = t.memoizedState;
    i === null ? t.memoizedState = {
      isBackwards: e,
      rendering: null,
      renderingStartTime: 0,
      last: a,
      tail: l,
      tailMode: n,
      treeForkCount: u
    } : (i.isBackwards = e, i.rendering = null, i.renderingStartTime = 0, i.last = a, i.tail = l, i.tailMode = n, i.treeForkCount = u);
  }
  function Ro(t, e, l) {
    var a = e.pendingProps, n = a.revealOrder, u = a.tail;
    a = a.children;
    var i = Ht.current, o = (i & 2) !== 0;
    if (o ? (i = i & 1 | 2, e.flags |= 128) : i &= 1, H(Ht, i), le(t, e, a, l), a = rt ? jn : 0, !o && t !== null && (t.flags & 128) !== 0)
      t: for (t = e.child; t !== null; ) {
        if (t.tag === 13)
          t.memoizedState !== null && Co(t, l, e);
        else if (t.tag === 19)
          Co(t, l, e);
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
    switch (n) {
      case "forwards":
        for (l = e.child, n = null; l !== null; )
          t = l.alternate, t !== null && Ku(t) === null && (n = l), l = l.sibling;
        l = n, l === null ? (n = e.child, e.child = null) : (n = l.sibling, l.sibling = null), cf(
          e,
          !1,
          n,
          l,
          u,
          a
        );
        break;
      case "backwards":
      case "unstable_legacy-backwards":
        for (l = null, n = e.child, e.child = null; n !== null; ) {
          if (t = n.alternate, t !== null && Ku(t) === null) {
            e.child = n;
            break;
          }
          t = n.sibling, n.sibling = l, l = n, n = t;
        }
        cf(
          e,
          !0,
          l,
          null,
          u,
          a
        );
        break;
      case "together":
        cf(
          e,
          !1,
          null,
          null,
          void 0,
          a
        );
        break;
      default:
        e.memoizedState = null;
    }
    return e.child;
  }
  function fl(t, e, l) {
    if (t !== null && (e.dependencies = t.dependencies), Hl |= e.lanes, (l & e.childLanes) === 0)
      if (t !== null) {
        if (Va(
          t,
          e,
          l,
          !1
        ), (l & e.childLanes) === 0)
          return null;
      } else return null;
    if (t !== null && e.child !== t.child)
      throw Error(s(153));
    if (e.child !== null) {
      for (t = e.child, l = ll(t, t.pendingProps), e.child = l, l.return = e; t.sibling !== null; )
        t = t.sibling, l = l.sibling = ll(t, t.pendingProps), l.return = e;
      l.sibling = null;
    }
    return e.child;
  }
  function ff(t, e) {
    return (t.lanes & e) !== 0 ? !0 : (t = t.dependencies, !!(t !== null && Yu(t)));
  }
  function Vm(t, e, l) {
    switch (e.tag) {
      case 3:
        kt(e, e.stateNode.containerInfo), Nl(e, Qt, t.memoizedState.cache), sa();
        break;
      case 27:
      case 5:
        Fl(e);
        break;
      case 4:
        kt(e, e.stateNode.containerInfo);
        break;
      case 10:
        Nl(
          e,
          e.type,
          e.memoizedProps.value
        );
        break;
      case 31:
        if (e.memoizedState !== null)
          return e.flags |= 128, Uc(e), null;
        break;
      case 13:
        var a = e.memoizedState;
        if (a !== null)
          return a.dehydrated !== null ? (Ul(e), e.flags |= 128, null) : (l & e.child.childLanes) !== 0 ? jo(t, e, l) : (Ul(e), t = fl(
            t,
            e,
            l
          ), t !== null ? t.sibling : null);
        Ul(e);
        break;
      case 19:
        var n = (t.flags & 128) !== 0;
        if (a = (l & e.childLanes) !== 0, a || (Va(
          t,
          e,
          l,
          !1
        ), a = (l & e.childLanes) !== 0), n) {
          if (a)
            return Ro(
              t,
              e,
              l
            );
          e.flags |= 128;
        }
        if (n = e.memoizedState, n !== null && (n.rendering = null, n.tail = null, n.lastEffect = null), H(Ht, Ht.current), a) break;
        return null;
      case 22:
        return e.lanes = 0, qo(
          t,
          e,
          l,
          e.pendingProps
        );
      case 24:
        Nl(e, Qt, t.memoizedState.cache);
    }
    return fl(t, e, l);
  }
  function Ho(t, e, l) {
    if (t !== null)
      if (t.memoizedProps !== e.pendingProps)
        Zt = !0;
      else {
        if (!ff(t, l) && (e.flags & 128) === 0)
          return Zt = !1, Vm(
            t,
            e,
            l
          );
        Zt = (t.flags & 131072) !== 0;
      }
    else
      Zt = !1, rt && (e.flags & 1048576) !== 0 && yr(e, jn, e.index);
    switch (e.lanes = 0, e.tag) {
      case 16:
        t: {
          var a = e.pendingProps;
          if (t = ya(e.elementType), e.type = t, typeof t == "function")
            dc(t) ? (a = ga(t, a), e.tag = 1, e = Do(
              null,
              e,
              t,
              a,
              l
            )) : (e.tag = 0, e = tf(
              null,
              e,
              t,
              a,
              l
            ));
          else {
            if (t != null) {
              var n = t.$$typeof;
              if (n === vt) {
                e.tag = 11, e = xo(
                  null,
                  e,
                  t,
                  a,
                  l
                );
                break t;
              } else if (n === at) {
                e.tag = 14, e = zo(
                  null,
                  e,
                  t,
                  a,
                  l
                );
                break t;
              }
            }
            throw e = Le(t) || t, Error(s(306, e, ""));
          }
        }
        return e;
      case 0:
        return tf(
          t,
          e,
          e.type,
          e.pendingProps,
          l
        );
      case 1:
        return a = e.type, n = ga(
          a,
          e.pendingProps
        ), Do(
          t,
          e,
          a,
          n,
          l
        );
      case 3:
        t: {
          if (kt(
            e,
            e.stateNode.containerInfo
          ), t === null) throw Error(s(387));
          a = e.pendingProps;
          var u = e.memoizedState;
          n = u.element, qc(t, e), Qn(e, a, null, l);
          var i = e.memoizedState;
          if (a = i.cache, Nl(e, Qt, a), a !== u.cache && _c(
            e,
            [Qt],
            l,
            !0
          ), Gn(), a = i.element, u.isDehydrated)
            if (u = {
              element: a,
              isDehydrated: !1,
              cache: i.cache
            }, e.updateQueue.baseState = u, e.memoizedState = u, e.flags & 256) {
              e = Uo(
                t,
                e,
                a,
                l
              );
              break t;
            } else if (a !== n) {
              n = Me(
                Error(s(424)),
                e
              ), Cn(n), e = Uo(
                t,
                e,
                a,
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
              for (Ot = Re(t.firstChild), te = e, rt = !0, Tl = null, je = !0, l = Tr(
                e,
                null,
                a,
                l
              ), e.child = l; l; )
                l.flags = l.flags & -3 | 4096, l = l.sibling;
            }
          else {
            if (sa(), a === n) {
              e = fl(
                t,
                e,
                l
              );
              break t;
            }
            le(t, e, a, l);
          }
          e = e.child;
        }
        return e;
      case 26:
        return ni(t, e), t === null ? (l = Kd(
          e.type,
          null,
          e.pendingProps,
          null
        )) ? e.memoizedState = l : rt || (l = e.type, t = e.pendingProps, a = _i(
          nt.current
        ).createElement(l), a[Pt] = e, a[re] = t, ae(a, l, t), Wt(a), e.stateNode = a) : e.memoizedState = Kd(
          e.type,
          t.memoizedProps,
          e.pendingProps,
          t.memoizedState
        ), null;
      case 27:
        return Fl(e), t === null && rt && (a = e.stateNode = Vd(
          e.type,
          e.pendingProps,
          nt.current
        ), te = e, je = !0, n = Ot, Ql(e.type) ? (Qf = n, Ot = Re(a.firstChild)) : Ot = n), le(
          t,
          e,
          e.pendingProps.children,
          l
        ), ni(t, e), t === null && (e.flags |= 4194304), e.child;
      case 5:
        return t === null && rt && ((n = a = Ot) && (a = Sh(
          a,
          e.type,
          e.pendingProps,
          je
        ), a !== null ? (e.stateNode = a, te = e, Ot = Re(a.firstChild), je = !1, n = !0) : n = !1), n || ql(e)), Fl(e), n = e.type, u = e.pendingProps, i = t !== null ? t.memoizedProps : null, a = u.children, Hf(n, u) ? a = null : i !== null && Hf(n, i) && (e.flags |= 32), e.memoizedState !== null && (n = Cc(
          t,
          e,
          Rm,
          null,
          null,
          l
        ), cu._currentValue = n), ni(t, e), le(t, e, a, l), e.child;
      case 6:
        return t === null && rt && ((t = l = Ot) && (l = _h(
          l,
          e.pendingProps,
          je
        ), l !== null ? (e.stateNode = l, te = e, Ot = null, t = !0) : t = !1), t || ql(e)), null;
      case 13:
        return jo(t, e, l);
      case 4:
        return kt(
          e,
          e.stateNode.containerInfo
        ), a = e.pendingProps, t === null ? e.child = ha(
          e,
          null,
          a,
          l
        ) : le(t, e, a, l), e.child;
      case 11:
        return xo(
          t,
          e,
          e.type,
          e.pendingProps,
          l
        );
      case 7:
        return le(
          t,
          e,
          e.pendingProps,
          l
        ), e.child;
      case 8:
        return le(
          t,
          e,
          e.pendingProps.children,
          l
        ), e.child;
      case 12:
        return le(
          t,
          e,
          e.pendingProps.children,
          l
        ), e.child;
      case 10:
        return a = e.pendingProps, Nl(e, e.type, a.value), le(t, e, a.children, l), e.child;
      case 9:
        return n = e.type._context, a = e.pendingProps.children, oa(e), n = ee(n), a = a(n), e.flags |= 1, le(t, e, a, l), e.child;
      case 14:
        return zo(
          t,
          e,
          e.type,
          e.pendingProps,
          l
        );
      case 15:
        return To(
          t,
          e,
          e.type,
          e.pendingProps,
          l
        );
      case 19:
        return Ro(t, e, l);
      case 31:
        return Zm(t, e, l);
      case 22:
        return qo(
          t,
          e,
          l,
          e.pendingProps
        );
      case 24:
        return oa(e), a = ee(Qt), t === null ? (n = xc(), n === null && (n = Tt, u = Ec(), n.pooledCache = u, u.refCount++, u !== null && (n.pooledCacheLanes |= l), n = u), e.memoizedState = { parent: a, cache: n }, Tc(e), Nl(e, Qt, n)) : ((t.lanes & l) !== 0 && (qc(t, e), Qn(e, null, null, l), Gn()), n = t.memoizedState, u = e.memoizedState, n.parent !== a ? (n = { parent: a, cache: a }, e.memoizedState = n, e.lanes === 0 && (e.memoizedState = e.updateQueue.baseState = n), Nl(e, Qt, a)) : (a = u.cache, Nl(e, Qt, a), a !== n.cache && _c(
          e,
          [Qt],
          l,
          !0
        ))), le(
          t,
          e,
          e.pendingProps.children,
          l
        ), e.child;
      case 29:
        throw e.pendingProps;
    }
    throw Error(s(156, e.tag));
  }
  function sl(t) {
    t.flags |= 4;
  }
  function sf(t, e, l, a, n) {
    if ((e = (t.mode & 32) !== 0) && (e = !1), e) {
      if (t.flags |= 16777216, (n & 335544128) === n)
        if (t.stateNode.complete) t.flags |= 8192;
        else if (sd()) t.flags |= 8192;
        else
          throw ma = Zu, zc;
    } else t.flags &= -16777217;
  }
  function Bo(t, e) {
    if (e.type !== "stylesheet" || (e.state.loading & 4) !== 0)
      t.flags &= -16777217;
    else if (t.flags |= 16777216, !Id(e))
      if (sd()) t.flags |= 8192;
      else
        throw ma = Zu, zc;
  }
  function ii(t, e) {
    e !== null && (t.flags |= 4), t.flags & 16384 && (e = t.tag !== 22 ? hs() : 536870912, t.lanes |= e, ln |= e);
  }
  function Kn(t, e) {
    if (!rt)
      switch (t.tailMode) {
        case "hidden":
          e = t.tail;
          for (var l = null; e !== null; )
            e.alternate !== null && (l = e), e = e.sibling;
          l === null ? t.tail = null : l.sibling = null;
          break;
        case "collapsed":
          l = t.tail;
          for (var a = null; l !== null; )
            l.alternate !== null && (a = l), l = l.sibling;
          a === null ? e || t.tail === null ? t.tail = null : t.tail.sibling = null : a.sibling = null;
      }
  }
  function Mt(t) {
    var e = t.alternate !== null && t.alternate.child === t.child, l = 0, a = 0;
    if (e)
      for (var n = t.child; n !== null; )
        l |= n.lanes | n.childLanes, a |= n.subtreeFlags & 65011712, a |= n.flags & 65011712, n.return = t, n = n.sibling;
    else
      for (n = t.child; n !== null; )
        l |= n.lanes | n.childLanes, a |= n.subtreeFlags, a |= n.flags, n.return = t, n = n.sibling;
    return t.subtreeFlags |= a, t.childLanes = l, e;
  }
  function wm(t, e, l) {
    var a = e.pendingProps;
    switch (vc(e), e.tag) {
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
        return l = e.stateNode, a = null, t !== null && (a = t.memoizedState.cache), e.memoizedState.cache !== a && (e.flags |= 2048), ul(Qt), gt(), l.pendingContext && (l.context = l.pendingContext, l.pendingContext = null), (t === null || t.child === null) && (Za(e) ? sl(e) : t === null || t.memoizedState.isDehydrated && (e.flags & 256) === 0 || (e.flags |= 1024, pc())), Mt(e), null;
      case 26:
        var n = e.type, u = e.memoizedState;
        return t === null ? (sl(e), u !== null ? (Mt(e), Bo(e, u)) : (Mt(e), sf(
          e,
          n,
          null,
          a,
          l
        ))) : u ? u !== t.memoizedState ? (sl(e), Mt(e), Bo(e, u)) : (Mt(e), e.flags &= -16777217) : (t = t.memoizedProps, t !== a && sl(e), Mt(e), sf(
          e,
          n,
          t,
          a,
          l
        )), null;
      case 27:
        if (Il(e), l = nt.current, n = e.type, t !== null && e.stateNode != null)
          t.memoizedProps !== a && sl(e);
        else {
          if (!a) {
            if (e.stateNode === null)
              throw Error(s(166));
            return Mt(e), null;
          }
          t = Y.current, Za(e) ? hr(e) : (t = Vd(n, a, l), e.stateNode = t, sl(e));
        }
        return Mt(e), null;
      case 5:
        if (Il(e), n = e.type, t !== null && e.stateNode != null)
          t.memoizedProps !== a && sl(e);
        else {
          if (!a) {
            if (e.stateNode === null)
              throw Error(s(166));
            return Mt(e), null;
          }
          if (u = Y.current, Za(e))
            hr(e);
          else {
            var i = _i(
              nt.current
            );
            switch (u) {
              case 1:
                u = i.createElementNS(
                  "http://www.w3.org/2000/svg",
                  n
                );
                break;
              case 2:
                u = i.createElementNS(
                  "http://www.w3.org/1998/Math/MathML",
                  n
                );
                break;
              default:
                switch (n) {
                  case "svg":
                    u = i.createElementNS(
                      "http://www.w3.org/2000/svg",
                      n
                    );
                    break;
                  case "math":
                    u = i.createElementNS(
                      "http://www.w3.org/1998/Math/MathML",
                      n
                    );
                    break;
                  case "script":
                    u = i.createElement("div"), u.innerHTML = "<script><\/script>", u = u.removeChild(
                      u.firstChild
                    );
                    break;
                  case "select":
                    u = typeof a.is == "string" ? i.createElement("select", {
                      is: a.is
                    }) : i.createElement("select"), a.multiple ? u.multiple = !0 : a.size && (u.size = a.size);
                    break;
                  default:
                    u = typeof a.is == "string" ? i.createElement(n, { is: a.is }) : i.createElement(n);
                }
            }
            u[Pt] = e, u[re] = a;
            t: for (i = e.child; i !== null; ) {
              if (i.tag === 5 || i.tag === 6)
                u.appendChild(i.stateNode);
              else if (i.tag !== 4 && i.tag !== 27 && i.child !== null) {
                i.child.return = i, i = i.child;
                continue;
              }
              if (i === e) break t;
              for (; i.sibling === null; ) {
                if (i.return === null || i.return === e)
                  break t;
                i = i.return;
              }
              i.sibling.return = i.return, i = i.sibling;
            }
            e.stateNode = u;
            t: switch (ae(u, n, a), n) {
              case "button":
              case "input":
              case "select":
              case "textarea":
                a = !!a.autoFocus;
                break t;
              case "img":
                a = !0;
                break t;
              default:
                a = !1;
            }
            a && sl(e);
          }
        }
        return Mt(e), sf(
          e,
          e.type,
          t === null ? null : t.memoizedProps,
          e.pendingProps,
          l
        ), null;
      case 6:
        if (t && e.stateNode != null)
          t.memoizedProps !== a && sl(e);
        else {
          if (typeof a != "string" && e.stateNode === null)
            throw Error(s(166));
          if (t = nt.current, Za(e)) {
            if (t = e.stateNode, l = e.memoizedProps, a = null, n = te, n !== null)
              switch (n.tag) {
                case 27:
                case 5:
                  a = n.memoizedProps;
              }
            t[Pt] = e, t = !!(t.nodeValue === l || a !== null && a.suppressHydrationWarning === !0 || jd(t.nodeValue, l)), t || ql(e, !0);
          } else
            t = _i(t).createTextNode(
              a
            ), t[Pt] = e, e.stateNode = t;
        }
        return Mt(e), null;
      case 31:
        if (l = e.memoizedState, t === null || t.memoizedState !== null) {
          if (a = Za(e), l !== null) {
            if (t === null) {
              if (!a) throw Error(s(318));
              if (t = e.memoizedState, t = t !== null ? t.dehydrated : null, !t) throw Error(s(557));
              t[Pt] = e;
            } else
              sa(), (e.flags & 128) === 0 && (e.memoizedState = null), e.flags |= 4;
            Mt(e), t = !1;
          } else
            l = pc(), t !== null && t.memoizedState !== null && (t.memoizedState.hydrationErrors = l), t = !0;
          if (!t)
            return e.flags & 256 ? (Ee(e), e) : (Ee(e), null);
          if ((e.flags & 128) !== 0)
            throw Error(s(558));
        }
        return Mt(e), null;
      case 13:
        if (a = e.memoizedState, t === null || t.memoizedState !== null && t.memoizedState.dehydrated !== null) {
          if (n = Za(e), a !== null && a.dehydrated !== null) {
            if (t === null) {
              if (!n) throw Error(s(318));
              if (n = e.memoizedState, n = n !== null ? n.dehydrated : null, !n) throw Error(s(317));
              n[Pt] = e;
            } else
              sa(), (e.flags & 128) === 0 && (e.memoizedState = null), e.flags |= 4;
            Mt(e), n = !1;
          } else
            n = pc(), t !== null && t.memoizedState !== null && (t.memoizedState.hydrationErrors = n), n = !0;
          if (!n)
            return e.flags & 256 ? (Ee(e), e) : (Ee(e), null);
        }
        return Ee(e), (e.flags & 128) !== 0 ? (e.lanes = l, e) : (l = a !== null, t = t !== null && t.memoizedState !== null, l && (a = e.child, n = null, a.alternate !== null && a.alternate.memoizedState !== null && a.alternate.memoizedState.cachePool !== null && (n = a.alternate.memoizedState.cachePool.pool), u = null, a.memoizedState !== null && a.memoizedState.cachePool !== null && (u = a.memoizedState.cachePool.pool), u !== n && (a.flags |= 2048)), l !== t && l && (e.child.flags |= 8192), ii(e, e.updateQueue), Mt(e), null);
      case 4:
        return gt(), t === null && Df(e.stateNode.containerInfo), Mt(e), null;
      case 10:
        return ul(e.type), Mt(e), null;
      case 19:
        if (D(Ht), a = e.memoizedState, a === null) return Mt(e), null;
        if (n = (e.flags & 128) !== 0, u = a.rendering, u === null)
          if (n) Kn(a, !1);
          else {
            if (Ct !== 0 || t !== null && (t.flags & 128) !== 0)
              for (t = e.child; t !== null; ) {
                if (u = Ku(t), u !== null) {
                  for (e.flags |= 128, Kn(a, !1), t = u.updateQueue, e.updateQueue = t, ii(e, t), e.subtreeFlags = 0, t = l, l = e.child; l !== null; )
                    rr(l, t), l = l.sibling;
                  return H(
                    Ht,
                    Ht.current & 1 | 2
                  ), rt && al(e, a.treeForkCount), e.child;
                }
                t = t.sibling;
              }
            a.tail !== null && It() > oi && (e.flags |= 128, n = !0, Kn(a, !1), e.lanes = 4194304);
          }
        else {
          if (!n)
            if (t = Ku(u), t !== null) {
              if (e.flags |= 128, n = !0, t = t.updateQueue, e.updateQueue = t, ii(e, t), Kn(a, !0), a.tail === null && a.tailMode === "hidden" && !u.alternate && !rt)
                return Mt(e), null;
            } else
              2 * It() - a.renderingStartTime > oi && l !== 536870912 && (e.flags |= 128, n = !0, Kn(a, !1), e.lanes = 4194304);
          a.isBackwards ? (u.sibling = e.child, e.child = u) : (t = a.last, t !== null ? t.sibling = u : e.child = u, a.last = u);
        }
        return a.tail !== null ? (t = a.tail, a.rendering = t, a.tail = t.sibling, a.renderingStartTime = It(), t.sibling = null, l = Ht.current, H(
          Ht,
          n ? l & 1 | 2 : l & 1
        ), rt && al(e, a.treeForkCount), t) : (Mt(e), null);
      case 22:
      case 23:
        return Ee(e), Dc(), a = e.memoizedState !== null, t !== null ? t.memoizedState !== null !== a && (e.flags |= 8192) : a && (e.flags |= 8192), a ? (l & 536870912) !== 0 && (e.flags & 128) === 0 && (Mt(e), e.subtreeFlags & 6 && (e.flags |= 8192)) : Mt(e), l = e.updateQueue, l !== null && ii(e, l.retryQueue), l = null, t !== null && t.memoizedState !== null && t.memoizedState.cachePool !== null && (l = t.memoizedState.cachePool.pool), a = null, e.memoizedState !== null && e.memoizedState.cachePool !== null && (a = e.memoizedState.cachePool.pool), a !== l && (e.flags |= 2048), t !== null && D(da), null;
      case 24:
        return l = null, t !== null && (l = t.memoizedState.cache), e.memoizedState.cache !== l && (e.flags |= 2048), ul(Qt), Mt(e), null;
      case 25:
        return null;
      case 30:
        return null;
    }
    throw Error(s(156, e.tag));
  }
  function Jm(t, e) {
    switch (vc(e), e.tag) {
      case 1:
        return t = e.flags, t & 65536 ? (e.flags = t & -65537 | 128, e) : null;
      case 3:
        return ul(Qt), gt(), t = e.flags, (t & 65536) !== 0 && (t & 128) === 0 ? (e.flags = t & -65537 | 128, e) : null;
      case 26:
      case 27:
      case 5:
        return Il(e), null;
      case 31:
        if (e.memoizedState !== null) {
          if (Ee(e), e.alternate === null)
            throw Error(s(340));
          sa();
        }
        return t = e.flags, t & 65536 ? (e.flags = t & -65537 | 128, e) : null;
      case 13:
        if (Ee(e), t = e.memoizedState, t !== null && t.dehydrated !== null) {
          if (e.alternate === null)
            throw Error(s(340));
          sa();
        }
        return t = e.flags, t & 65536 ? (e.flags = t & -65537 | 128, e) : null;
      case 19:
        return D(Ht), null;
      case 4:
        return gt(), null;
      case 10:
        return ul(e.type), null;
      case 22:
      case 23:
        return Ee(e), Dc(), t !== null && D(da), t = e.flags, t & 65536 ? (e.flags = t & -65537 | 128, e) : null;
      case 24:
        return ul(Qt), null;
      case 25:
        return null;
      default:
        return null;
    }
  }
  function Lo(t, e) {
    switch (vc(e), e.tag) {
      case 3:
        ul(Qt), gt();
        break;
      case 26:
      case 27:
      case 5:
        Il(e);
        break;
      case 4:
        gt();
        break;
      case 31:
        e.memoizedState !== null && Ee(e);
        break;
      case 13:
        Ee(e);
        break;
      case 19:
        D(Ht);
        break;
      case 10:
        ul(e.type);
        break;
      case 22:
      case 23:
        Ee(e), Dc(), t !== null && D(da);
        break;
      case 24:
        ul(Qt);
    }
  }
  function kn(t, e) {
    try {
      var l = e.updateQueue, a = l !== null ? l.lastEffect : null;
      if (a !== null) {
        var n = a.next;
        l = n;
        do {
          if ((l.tag & t) === t) {
            a = void 0;
            var u = l.create, i = l.inst;
            a = u(), i.destroy = a;
          }
          l = l.next;
        } while (l !== n);
      }
    } catch (o) {
      _t(e, e.return, o);
    }
  }
  function Cl(t, e, l) {
    try {
      var a = e.updateQueue, n = a !== null ? a.lastEffect : null;
      if (n !== null) {
        var u = n.next;
        a = u;
        do {
          if ((a.tag & t) === t) {
            var i = a.inst, o = i.destroy;
            if (o !== void 0) {
              i.destroy = void 0, n = e;
              var d = l, _ = o;
              try {
                _();
              } catch (T) {
                _t(
                  n,
                  d,
                  T
                );
              }
            }
          }
          a = a.next;
        } while (a !== u);
      }
    } catch (T) {
      _t(e, e.return, T);
    }
  }
  function Yo(t) {
    var e = t.updateQueue;
    if (e !== null) {
      var l = t.stateNode;
      try {
        Nr(e, l);
      } catch (a) {
        _t(t, t.return, a);
      }
    }
  }
  function Go(t, e, l) {
    l.props = ga(
      t.type,
      t.memoizedProps
    ), l.state = t.memoizedState;
    try {
      l.componentWillUnmount();
    } catch (a) {
      _t(t, e, a);
    }
  }
  function $n(t, e) {
    try {
      var l = t.ref;
      if (l !== null) {
        switch (t.tag) {
          case 26:
          case 27:
          case 5:
            var a = t.stateNode;
            break;
          case 30:
            a = t.stateNode;
            break;
          default:
            a = t.stateNode;
        }
        typeof l == "function" ? t.refCleanup = l(a) : l.current = a;
      }
    } catch (n) {
      _t(t, e, n);
    }
  }
  function $e(t, e) {
    var l = t.ref, a = t.refCleanup;
    if (l !== null)
      if (typeof a == "function")
        try {
          a();
        } catch (n) {
          _t(t, e, n);
        } finally {
          t.refCleanup = null, t = t.alternate, t != null && (t.refCleanup = null);
        }
      else if (typeof l == "function")
        try {
          l(null);
        } catch (n) {
          _t(t, e, n);
        }
      else l.current = null;
  }
  function Qo(t) {
    var e = t.type, l = t.memoizedProps, a = t.stateNode;
    try {
      t: switch (e) {
        case "button":
        case "input":
        case "select":
        case "textarea":
          l.autoFocus && a.focus();
          break t;
        case "img":
          l.src ? a.src = l.src : l.srcSet && (a.srcset = l.srcSet);
      }
    } catch (n) {
      _t(t, t.return, n);
    }
  }
  function rf(t, e, l) {
    try {
      var a = t.stateNode;
      mh(a, t.type, l, e), a[re] = e;
    } catch (n) {
      _t(t, t.return, n);
    }
  }
  function Xo(t) {
    return t.tag === 5 || t.tag === 3 || t.tag === 26 || t.tag === 27 && Ql(t.type) || t.tag === 4;
  }
  function of(t) {
    t: for (; ; ) {
      for (; t.sibling === null; ) {
        if (t.return === null || Xo(t.return)) return null;
        t = t.return;
      }
      for (t.sibling.return = t.return, t = t.sibling; t.tag !== 5 && t.tag !== 6 && t.tag !== 18; ) {
        if (t.tag === 27 && Ql(t.type) || t.flags & 2 || t.child === null || t.tag === 4) continue t;
        t.child.return = t, t = t.child;
      }
      if (!(t.flags & 2)) return t.stateNode;
    }
  }
  function df(t, e, l) {
    var a = t.tag;
    if (a === 5 || a === 6)
      t = t.stateNode, e ? (l.nodeType === 9 ? l.body : l.nodeName === "HTML" ? l.ownerDocument.body : l).insertBefore(t, e) : (e = l.nodeType === 9 ? l.body : l.nodeName === "HTML" ? l.ownerDocument.body : l, e.appendChild(t), l = l._reactRootContainer, l != null || e.onclick !== null || (e.onclick = tl));
    else if (a !== 4 && (a === 27 && Ql(t.type) && (l = t.stateNode, e = null), t = t.child, t !== null))
      for (df(t, e, l), t = t.sibling; t !== null; )
        df(t, e, l), t = t.sibling;
  }
  function ci(t, e, l) {
    var a = t.tag;
    if (a === 5 || a === 6)
      t = t.stateNode, e ? l.insertBefore(t, e) : l.appendChild(t);
    else if (a !== 4 && (a === 27 && Ql(t.type) && (l = t.stateNode), t = t.child, t !== null))
      for (ci(t, e, l), t = t.sibling; t !== null; )
        ci(t, e, l), t = t.sibling;
  }
  function Zo(t) {
    var e = t.stateNode, l = t.memoizedProps;
    try {
      for (var a = t.type, n = e.attributes; n.length; )
        e.removeAttributeNode(n[0]);
      ae(e, a, l), e[Pt] = t, e[re] = l;
    } catch (u) {
      _t(t, t.return, u);
    }
  }
  var rl = !1, Vt = !1, yf = !1, Vo = typeof WeakSet == "function" ? WeakSet : Set, Ft = null;
  function Km(t, e) {
    if (t = t.containerInfo, Cf = Ni, t = er(t), uc(t)) {
      if ("selectionStart" in t)
        var l = {
          start: t.selectionStart,
          end: t.selectionEnd
        };
      else
        t: {
          l = (l = t.ownerDocument) && l.defaultView || window;
          var a = l.getSelection && l.getSelection();
          if (a && a.rangeCount !== 0) {
            l = a.anchorNode;
            var n = a.anchorOffset, u = a.focusNode;
            a = a.focusOffset;
            try {
              l.nodeType, u.nodeType;
            } catch {
              l = null;
              break t;
            }
            var i = 0, o = -1, d = -1, _ = 0, T = 0, M = t, E = null;
            e: for (; ; ) {
              for (var A; M !== l || n !== 0 && M.nodeType !== 3 || (o = i + n), M !== u || a !== 0 && M.nodeType !== 3 || (d = i + a), M.nodeType === 3 && (i += M.nodeValue.length), (A = M.firstChild) !== null; )
                E = M, M = A;
              for (; ; ) {
                if (M === t) break e;
                if (E === l && ++_ === n && (o = i), E === u && ++T === a && (d = i), (A = M.nextSibling) !== null) break;
                M = E, E = M.parentNode;
              }
              M = A;
            }
            l = o === -1 || d === -1 ? null : { start: o, end: d };
          } else l = null;
        }
      l = l || { start: 0, end: 0 };
    } else l = null;
    for (Rf = { focusedElem: t, selectionRange: l }, Ni = !1, Ft = e; Ft !== null; )
      if (e = Ft, t = e.child, (e.subtreeFlags & 1028) !== 0 && t !== null)
        t.return = e, Ft = t;
      else
        for (; Ft !== null; ) {
          switch (e = Ft, u = e.alternate, t = e.flags, e.tag) {
            case 0:
              if ((t & 4) !== 0 && (t = e.updateQueue, t = t !== null ? t.events : null, t !== null))
                for (l = 0; l < t.length; l++)
                  n = t[l], n.ref.impl = n.nextImpl;
              break;
            case 11:
            case 15:
              break;
            case 1:
              if ((t & 1024) !== 0 && u !== null) {
                t = void 0, l = e, n = u.memoizedProps, u = u.memoizedState, a = l.stateNode;
                try {
                  var G = ga(
                    l.type,
                    n
                  );
                  t = a.getSnapshotBeforeUpdate(
                    G,
                    u
                  ), a.__reactInternalSnapshotBeforeUpdate = t;
                } catch (K) {
                  _t(
                    l,
                    l.return,
                    K
                  );
                }
              }
              break;
            case 3:
              if ((t & 1024) !== 0) {
                if (t = e.stateNode.containerInfo, l = t.nodeType, l === 9)
                  Lf(t);
                else if (l === 1)
                  switch (t.nodeName) {
                    case "HEAD":
                    case "HTML":
                    case "BODY":
                      Lf(t);
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
              if ((t & 1024) !== 0) throw Error(s(163));
          }
          if (t = e.sibling, t !== null) {
            t.return = e.return, Ft = t;
            break;
          }
          Ft = e.return;
        }
  }
  function wo(t, e, l) {
    var a = l.flags;
    switch (l.tag) {
      case 0:
      case 11:
      case 15:
        dl(t, l), a & 4 && kn(5, l);
        break;
      case 1:
        if (dl(t, l), a & 4)
          if (t = l.stateNode, e === null)
            try {
              t.componentDidMount();
            } catch (i) {
              _t(l, l.return, i);
            }
          else {
            var n = ga(
              l.type,
              e.memoizedProps
            );
            e = e.memoizedState;
            try {
              t.componentDidUpdate(
                n,
                e,
                t.__reactInternalSnapshotBeforeUpdate
              );
            } catch (i) {
              _t(
                l,
                l.return,
                i
              );
            }
          }
        a & 64 && Yo(l), a & 512 && $n(l, l.return);
        break;
      case 3:
        if (dl(t, l), a & 64 && (t = l.updateQueue, t !== null)) {
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
            Nr(t, e);
          } catch (i) {
            _t(l, l.return, i);
          }
        }
        break;
      case 27:
        e === null && a & 4 && Zo(l);
      case 26:
      case 5:
        dl(t, l), e === null && a & 4 && Qo(l), a & 512 && $n(l, l.return);
        break;
      case 12:
        dl(t, l);
        break;
      case 31:
        dl(t, l), a & 4 && ko(t, l);
        break;
      case 13:
        dl(t, l), a & 4 && $o(t, l), a & 64 && (t = l.memoizedState, t !== null && (t = t.dehydrated, t !== null && (l = lh.bind(
          null,
          l
        ), Eh(t, l))));
        break;
      case 22:
        if (a = l.memoizedState !== null || rl, !a) {
          e = e !== null && e.memoizedState !== null || Vt, n = rl;
          var u = Vt;
          rl = a, (Vt = e) && !u ? yl(
            t,
            l,
            (l.subtreeFlags & 8772) !== 0
          ) : dl(t, l), rl = n, Vt = u;
        }
        break;
      case 30:
        break;
      default:
        dl(t, l);
    }
  }
  function Jo(t) {
    var e = t.alternate;
    e !== null && (t.alternate = null, Jo(e)), t.child = null, t.deletions = null, t.sibling = null, t.tag === 5 && (e = t.stateNode, e !== null && Xi(e)), t.stateNode = null, t.return = null, t.dependencies = null, t.memoizedProps = null, t.memoizedState = null, t.pendingProps = null, t.stateNode = null, t.updateQueue = null;
  }
  var Dt = null, de = !1;
  function ol(t, e, l) {
    for (l = l.child; l !== null; )
      Ko(t, e, l), l = l.sibling;
  }
  function Ko(t, e, l) {
    if (fe && typeof fe.onCommitFiberUnmount == "function")
      try {
        fe.onCommitFiberUnmount(Je, l);
      } catch {
      }
    switch (l.tag) {
      case 26:
        Vt || $e(l, e), ol(
          t,
          e,
          l
        ), l.memoizedState ? l.memoizedState.count-- : l.stateNode && (l = l.stateNode, l.parentNode.removeChild(l));
        break;
      case 27:
        Vt || $e(l, e);
        var a = Dt, n = de;
        Ql(l.type) && (Dt = l.stateNode, de = !1), ol(
          t,
          e,
          l
        ), nu(l.stateNode), Dt = a, de = n;
        break;
      case 5:
        Vt || $e(l, e);
      case 6:
        if (a = Dt, n = de, Dt = null, ol(
          t,
          e,
          l
        ), Dt = a, de = n, Dt !== null)
          if (de)
            try {
              (Dt.nodeType === 9 ? Dt.body : Dt.nodeName === "HTML" ? Dt.ownerDocument.body : Dt).removeChild(l.stateNode);
            } catch (u) {
              _t(
                l,
                e,
                u
              );
            }
          else
            try {
              Dt.removeChild(l.stateNode);
            } catch (u) {
              _t(
                l,
                e,
                u
              );
            }
        break;
      case 18:
        Dt !== null && (de ? (t = Dt, Yd(
          t.nodeType === 9 ? t.body : t.nodeName === "HTML" ? t.ownerDocument.body : t,
          l.stateNode
        ), on(t)) : Yd(Dt, l.stateNode));
        break;
      case 4:
        a = Dt, n = de, Dt = l.stateNode.containerInfo, de = !0, ol(
          t,
          e,
          l
        ), Dt = a, de = n;
        break;
      case 0:
      case 11:
      case 14:
      case 15:
        Cl(2, l, e), Vt || Cl(4, l, e), ol(
          t,
          e,
          l
        );
        break;
      case 1:
        Vt || ($e(l, e), a = l.stateNode, typeof a.componentWillUnmount == "function" && Go(
          l,
          e,
          a
        )), ol(
          t,
          e,
          l
        );
        break;
      case 21:
        ol(
          t,
          e,
          l
        );
        break;
      case 22:
        Vt = (a = Vt) || l.memoizedState !== null, ol(
          t,
          e,
          l
        ), Vt = a;
        break;
      default:
        ol(
          t,
          e,
          l
        );
    }
  }
  function ko(t, e) {
    if (e.memoizedState === null && (t = e.alternate, t !== null && (t = t.memoizedState, t !== null))) {
      t = t.dehydrated;
      try {
        on(t);
      } catch (l) {
        _t(e, e.return, l);
      }
    }
  }
  function $o(t, e) {
    if (e.memoizedState === null && (t = e.alternate, t !== null && (t = t.memoizedState, t !== null && (t = t.dehydrated, t !== null))))
      try {
        on(t);
      } catch (l) {
        _t(e, e.return, l);
      }
  }
  function km(t) {
    switch (t.tag) {
      case 31:
      case 13:
      case 19:
        var e = t.stateNode;
        return e === null && (e = t.stateNode = new Vo()), e;
      case 22:
        return t = t.stateNode, e = t._retryCache, e === null && (e = t._retryCache = new Vo()), e;
      default:
        throw Error(s(435, t.tag));
    }
  }
  function fi(t, e) {
    var l = km(t);
    e.forEach(function(a) {
      if (!l.has(a)) {
        l.add(a);
        var n = ah.bind(null, t, a);
        a.then(n, n);
      }
    });
  }
  function ye(t, e) {
    var l = e.deletions;
    if (l !== null)
      for (var a = 0; a < l.length; a++) {
        var n = l[a], u = t, i = e, o = i;
        t: for (; o !== null; ) {
          switch (o.tag) {
            case 27:
              if (Ql(o.type)) {
                Dt = o.stateNode, de = !1;
                break t;
              }
              break;
            case 5:
              Dt = o.stateNode, de = !1;
              break t;
            case 3:
            case 4:
              Dt = o.stateNode.containerInfo, de = !0;
              break t;
          }
          o = o.return;
        }
        if (Dt === null) throw Error(s(160));
        Ko(u, i, n), Dt = null, de = !1, u = n.alternate, u !== null && (u.return = null), n.return = null;
      }
    if (e.subtreeFlags & 13886)
      for (e = e.child; e !== null; )
        Wo(e, t), e = e.sibling;
  }
  var Ge = null;
  function Wo(t, e) {
    var l = t.alternate, a = t.flags;
    switch (t.tag) {
      case 0:
      case 11:
      case 14:
      case 15:
        ye(e, t), me(t), a & 4 && (Cl(3, t, t.return), kn(3, t), Cl(5, t, t.return));
        break;
      case 1:
        ye(e, t), me(t), a & 512 && (Vt || l === null || $e(l, l.return)), a & 64 && rl && (t = t.updateQueue, t !== null && (a = t.callbacks, a !== null && (l = t.shared.hiddenCallbacks, t.shared.hiddenCallbacks = l === null ? a : l.concat(a))));
        break;
      case 26:
        var n = Ge;
        if (ye(e, t), me(t), a & 512 && (Vt || l === null || $e(l, l.return)), a & 4) {
          var u = l !== null ? l.memoizedState : null;
          if (a = t.memoizedState, l === null)
            if (a === null)
              if (t.stateNode === null) {
                t: {
                  a = t.type, l = t.memoizedProps, n = n.ownerDocument || n;
                  e: switch (a) {
                    case "title":
                      u = n.getElementsByTagName("title")[0], (!u || u[En] || u[Pt] || u.namespaceURI === "http://www.w3.org/2000/svg" || u.hasAttribute("itemprop")) && (u = n.createElement(a), n.head.insertBefore(
                        u,
                        n.querySelector("head > title")
                      )), ae(u, a, l), u[Pt] = t, Wt(u), a = u;
                      break t;
                    case "link":
                      var i = Wd(
                        "link",
                        "href",
                        n
                      ).get(a + (l.href || ""));
                      if (i) {
                        for (var o = 0; o < i.length; o++)
                          if (u = i[o], u.getAttribute("href") === (l.href == null || l.href === "" ? null : l.href) && u.getAttribute("rel") === (l.rel == null ? null : l.rel) && u.getAttribute("title") === (l.title == null ? null : l.title) && u.getAttribute("crossorigin") === (l.crossOrigin == null ? null : l.crossOrigin)) {
                            i.splice(o, 1);
                            break e;
                          }
                      }
                      u = n.createElement(a), ae(u, a, l), n.head.appendChild(u);
                      break;
                    case "meta":
                      if (i = Wd(
                        "meta",
                        "content",
                        n
                      ).get(a + (l.content || ""))) {
                        for (o = 0; o < i.length; o++)
                          if (u = i[o], u.getAttribute("content") === (l.content == null ? null : "" + l.content) && u.getAttribute("name") === (l.name == null ? null : l.name) && u.getAttribute("property") === (l.property == null ? null : l.property) && u.getAttribute("http-equiv") === (l.httpEquiv == null ? null : l.httpEquiv) && u.getAttribute("charset") === (l.charSet == null ? null : l.charSet)) {
                            i.splice(o, 1);
                            break e;
                          }
                      }
                      u = n.createElement(a), ae(u, a, l), n.head.appendChild(u);
                      break;
                    default:
                      throw Error(s(468, a));
                  }
                  u[Pt] = t, Wt(u), a = u;
                }
                t.stateNode = a;
              } else
                Fd(
                  n,
                  t.type,
                  t.stateNode
                );
            else
              t.stateNode = $d(
                n,
                a,
                t.memoizedProps
              );
          else
            u !== a ? (u === null ? l.stateNode !== null && (l = l.stateNode, l.parentNode.removeChild(l)) : u.count--, a === null ? Fd(
              n,
              t.type,
              t.stateNode
            ) : $d(
              n,
              a,
              t.memoizedProps
            )) : a === null && t.stateNode !== null && rf(
              t,
              t.memoizedProps,
              l.memoizedProps
            );
        }
        break;
      case 27:
        ye(e, t), me(t), a & 512 && (Vt || l === null || $e(l, l.return)), l !== null && a & 4 && rf(
          t,
          t.memoizedProps,
          l.memoizedProps
        );
        break;
      case 5:
        if (ye(e, t), me(t), a & 512 && (Vt || l === null || $e(l, l.return)), t.flags & 32) {
          n = t.stateNode;
          try {
            ja(n, "");
          } catch (G) {
            _t(t, t.return, G);
          }
        }
        a & 4 && t.stateNode != null && (n = t.memoizedProps, rf(
          t,
          n,
          l !== null ? l.memoizedProps : n
        )), a & 1024 && (yf = !0);
        break;
      case 6:
        if (ye(e, t), me(t), a & 4) {
          if (t.stateNode === null)
            throw Error(s(162));
          a = t.memoizedProps, l = t.stateNode;
          try {
            l.nodeValue = a;
          } catch (G) {
            _t(t, t.return, G);
          }
        }
        break;
      case 3:
        if (xi = null, n = Ge, Ge = Ei(e.containerInfo), ye(e, t), Ge = n, me(t), a & 4 && l !== null && l.memoizedState.isDehydrated)
          try {
            on(e.containerInfo);
          } catch (G) {
            _t(t, t.return, G);
          }
        yf && (yf = !1, Fo(t));
        break;
      case 4:
        a = Ge, Ge = Ei(
          t.stateNode.containerInfo
        ), ye(e, t), me(t), Ge = a;
        break;
      case 12:
        ye(e, t), me(t);
        break;
      case 31:
        ye(e, t), me(t), a & 4 && (a = t.updateQueue, a !== null && (t.updateQueue = null, fi(t, a)));
        break;
      case 13:
        ye(e, t), me(t), t.child.flags & 8192 && t.memoizedState !== null != (l !== null && l.memoizedState !== null) && (ri = It()), a & 4 && (a = t.updateQueue, a !== null && (t.updateQueue = null, fi(t, a)));
        break;
      case 22:
        n = t.memoizedState !== null;
        var d = l !== null && l.memoizedState !== null, _ = rl, T = Vt;
        if (rl = _ || n, Vt = T || d, ye(e, t), Vt = T, rl = _, me(t), a & 8192)
          t: for (e = t.stateNode, e._visibility = n ? e._visibility & -2 : e._visibility | 1, n && (l === null || d || rl || Vt || pa(t)), l = null, e = t; ; ) {
            if (e.tag === 5 || e.tag === 26) {
              if (l === null) {
                d = l = e;
                try {
                  if (u = d.stateNode, n)
                    i = u.style, typeof i.setProperty == "function" ? i.setProperty("display", "none", "important") : i.display = "none";
                  else {
                    o = d.stateNode;
                    var M = d.memoizedProps.style, E = M != null && M.hasOwnProperty("display") ? M.display : null;
                    o.style.display = E == null || typeof E == "boolean" ? "" : ("" + E).trim();
                  }
                } catch (G) {
                  _t(d, d.return, G);
                }
              }
            } else if (e.tag === 6) {
              if (l === null) {
                d = e;
                try {
                  d.stateNode.nodeValue = n ? "" : d.memoizedProps;
                } catch (G) {
                  _t(d, d.return, G);
                }
              }
            } else if (e.tag === 18) {
              if (l === null) {
                d = e;
                try {
                  var A = d.stateNode;
                  n ? Gd(A, !0) : Gd(d.stateNode, !1);
                } catch (G) {
                  _t(d, d.return, G);
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
        a & 4 && (a = t.updateQueue, a !== null && (l = a.retryQueue, l !== null && (a.retryQueue = null, fi(t, l))));
        break;
      case 19:
        ye(e, t), me(t), a & 4 && (a = t.updateQueue, a !== null && (t.updateQueue = null, fi(t, a)));
        break;
      case 30:
        break;
      case 21:
        break;
      default:
        ye(e, t), me(t);
    }
  }
  function me(t) {
    var e = t.flags;
    if (e & 2) {
      try {
        for (var l, a = t.return; a !== null; ) {
          if (Xo(a)) {
            l = a;
            break;
          }
          a = a.return;
        }
        if (l == null) throw Error(s(160));
        switch (l.tag) {
          case 27:
            var n = l.stateNode, u = of(t);
            ci(t, u, n);
            break;
          case 5:
            var i = l.stateNode;
            l.flags & 32 && (ja(i, ""), l.flags &= -33);
            var o = of(t);
            ci(t, o, i);
            break;
          case 3:
          case 4:
            var d = l.stateNode.containerInfo, _ = of(t);
            df(
              t,
              _,
              d
            );
            break;
          default:
            throw Error(s(161));
        }
      } catch (T) {
        _t(t, t.return, T);
      }
      t.flags &= -3;
    }
    e & 4096 && (t.flags &= -4097);
  }
  function Fo(t) {
    if (t.subtreeFlags & 1024)
      for (t = t.child; t !== null; ) {
        var e = t;
        Fo(e), e.tag === 5 && e.flags & 1024 && e.stateNode.reset(), t = t.sibling;
      }
  }
  function dl(t, e) {
    if (e.subtreeFlags & 8772)
      for (e = e.child; e !== null; )
        wo(t, e.alternate, e), e = e.sibling;
  }
  function pa(t) {
    for (t = t.child; t !== null; ) {
      var e = t;
      switch (e.tag) {
        case 0:
        case 11:
        case 14:
        case 15:
          Cl(4, e, e.return), pa(e);
          break;
        case 1:
          $e(e, e.return);
          var l = e.stateNode;
          typeof l.componentWillUnmount == "function" && Go(
            e,
            e.return,
            l
          ), pa(e);
          break;
        case 27:
          nu(e.stateNode);
        case 26:
        case 5:
          $e(e, e.return), pa(e);
          break;
        case 22:
          e.memoizedState === null && pa(e);
          break;
        case 30:
          pa(e);
          break;
        default:
          pa(e);
      }
      t = t.sibling;
    }
  }
  function yl(t, e, l) {
    for (l = l && (e.subtreeFlags & 8772) !== 0, e = e.child; e !== null; ) {
      var a = e.alternate, n = t, u = e, i = u.flags;
      switch (u.tag) {
        case 0:
        case 11:
        case 15:
          yl(
            n,
            u,
            l
          ), kn(4, u);
          break;
        case 1:
          if (yl(
            n,
            u,
            l
          ), a = u, n = a.stateNode, typeof n.componentDidMount == "function")
            try {
              n.componentDidMount();
            } catch (_) {
              _t(a, a.return, _);
            }
          if (a = u, n = a.updateQueue, n !== null) {
            var o = a.stateNode;
            try {
              var d = n.shared.hiddenCallbacks;
              if (d !== null)
                for (n.shared.hiddenCallbacks = null, n = 0; n < d.length; n++)
                  qr(d[n], o);
            } catch (_) {
              _t(a, a.return, _);
            }
          }
          l && i & 64 && Yo(u), $n(u, u.return);
          break;
        case 27:
          Zo(u);
        case 26:
        case 5:
          yl(
            n,
            u,
            l
          ), l && a === null && i & 4 && Qo(u), $n(u, u.return);
          break;
        case 12:
          yl(
            n,
            u,
            l
          );
          break;
        case 31:
          yl(
            n,
            u,
            l
          ), l && i & 4 && ko(n, u);
          break;
        case 13:
          yl(
            n,
            u,
            l
          ), l && i & 4 && $o(n, u);
          break;
        case 22:
          u.memoizedState === null && yl(
            n,
            u,
            l
          ), $n(u, u.return);
          break;
        case 30:
          break;
        default:
          yl(
            n,
            u,
            l
          );
      }
      e = e.sibling;
    }
  }
  function mf(t, e) {
    var l = null;
    t !== null && t.memoizedState !== null && t.memoizedState.cachePool !== null && (l = t.memoizedState.cachePool.pool), t = null, e.memoizedState !== null && e.memoizedState.cachePool !== null && (t = e.memoizedState.cachePool.pool), t !== l && (t != null && t.refCount++, l != null && Rn(l));
  }
  function hf(t, e) {
    t = null, e.alternate !== null && (t = e.alternate.memoizedState.cache), e = e.memoizedState.cache, e !== t && (e.refCount++, t != null && Rn(t));
  }
  function Qe(t, e, l, a) {
    if (e.subtreeFlags & 10256)
      for (e = e.child; e !== null; )
        Io(
          t,
          e,
          l,
          a
        ), e = e.sibling;
  }
  function Io(t, e, l, a) {
    var n = e.flags;
    switch (e.tag) {
      case 0:
      case 11:
      case 15:
        Qe(
          t,
          e,
          l,
          a
        ), n & 2048 && kn(9, e);
        break;
      case 1:
        Qe(
          t,
          e,
          l,
          a
        );
        break;
      case 3:
        Qe(
          t,
          e,
          l,
          a
        ), n & 2048 && (t = null, e.alternate !== null && (t = e.alternate.memoizedState.cache), e = e.memoizedState.cache, e !== t && (e.refCount++, t != null && Rn(t)));
        break;
      case 12:
        if (n & 2048) {
          Qe(
            t,
            e,
            l,
            a
          ), t = e.stateNode;
          try {
            var u = e.memoizedProps, i = u.id, o = u.onPostCommit;
            typeof o == "function" && o(
              i,
              e.alternate === null ? "mount" : "update",
              t.passiveEffectDuration,
              -0
            );
          } catch (d) {
            _t(e, e.return, d);
          }
        } else
          Qe(
            t,
            e,
            l,
            a
          );
        break;
      case 31:
        Qe(
          t,
          e,
          l,
          a
        );
        break;
      case 13:
        Qe(
          t,
          e,
          l,
          a
        );
        break;
      case 23:
        break;
      case 22:
        u = e.stateNode, i = e.alternate, e.memoizedState !== null ? u._visibility & 2 ? Qe(
          t,
          e,
          l,
          a
        ) : Wn(t, e) : u._visibility & 2 ? Qe(
          t,
          e,
          l,
          a
        ) : (u._visibility |= 2, Pa(
          t,
          e,
          l,
          a,
          (e.subtreeFlags & 10256) !== 0 || !1
        )), n & 2048 && mf(i, e);
        break;
      case 24:
        Qe(
          t,
          e,
          l,
          a
        ), n & 2048 && hf(e.alternate, e);
        break;
      default:
        Qe(
          t,
          e,
          l,
          a
        );
    }
  }
  function Pa(t, e, l, a, n) {
    for (n = n && ((e.subtreeFlags & 10256) !== 0 || !1), e = e.child; e !== null; ) {
      var u = t, i = e, o = l, d = a, _ = i.flags;
      switch (i.tag) {
        case 0:
        case 11:
        case 15:
          Pa(
            u,
            i,
            o,
            d,
            n
          ), kn(8, i);
          break;
        case 23:
          break;
        case 22:
          var T = i.stateNode;
          i.memoizedState !== null ? T._visibility & 2 ? Pa(
            u,
            i,
            o,
            d,
            n
          ) : Wn(
            u,
            i
          ) : (T._visibility |= 2, Pa(
            u,
            i,
            o,
            d,
            n
          )), n && _ & 2048 && mf(
            i.alternate,
            i
          );
          break;
        case 24:
          Pa(
            u,
            i,
            o,
            d,
            n
          ), n && _ & 2048 && hf(i.alternate, i);
          break;
        default:
          Pa(
            u,
            i,
            o,
            d,
            n
          );
      }
      e = e.sibling;
    }
  }
  function Wn(t, e) {
    if (e.subtreeFlags & 10256)
      for (e = e.child; e !== null; ) {
        var l = t, a = e, n = a.flags;
        switch (a.tag) {
          case 22:
            Wn(l, a), n & 2048 && mf(
              a.alternate,
              a
            );
            break;
          case 24:
            Wn(l, a), n & 2048 && hf(a.alternate, a);
            break;
          default:
            Wn(l, a);
        }
        e = e.sibling;
      }
  }
  var Fn = 8192;
  function tn(t, e, l) {
    if (t.subtreeFlags & Fn)
      for (t = t.child; t !== null; )
        Po(
          t,
          e,
          l
        ), t = t.sibling;
  }
  function Po(t, e, l) {
    switch (t.tag) {
      case 26:
        tn(
          t,
          e,
          l
        ), t.flags & Fn && t.memoizedState !== null && Ch(
          l,
          Ge,
          t.memoizedState,
          t.memoizedProps
        );
        break;
      case 5:
        tn(
          t,
          e,
          l
        );
        break;
      case 3:
      case 4:
        var a = Ge;
        Ge = Ei(t.stateNode.containerInfo), tn(
          t,
          e,
          l
        ), Ge = a;
        break;
      case 22:
        t.memoizedState === null && (a = t.alternate, a !== null && a.memoizedState !== null ? (a = Fn, Fn = 16777216, tn(
          t,
          e,
          l
        ), Fn = a) : tn(
          t,
          e,
          l
        ));
        break;
      default:
        tn(
          t,
          e,
          l
        );
    }
  }
  function td(t) {
    var e = t.alternate;
    if (e !== null && (t = e.child, t !== null)) {
      e.child = null;
      do
        e = t.sibling, t.sibling = null, t = e;
      while (t !== null);
    }
  }
  function In(t) {
    var e = t.deletions;
    if ((t.flags & 16) !== 0) {
      if (e !== null)
        for (var l = 0; l < e.length; l++) {
          var a = e[l];
          Ft = a, ld(
            a,
            t
          );
        }
      td(t);
    }
    if (t.subtreeFlags & 10256)
      for (t = t.child; t !== null; )
        ed(t), t = t.sibling;
  }
  function ed(t) {
    switch (t.tag) {
      case 0:
      case 11:
      case 15:
        In(t), t.flags & 2048 && Cl(9, t, t.return);
        break;
      case 3:
        In(t);
        break;
      case 12:
        In(t);
        break;
      case 22:
        var e = t.stateNode;
        t.memoizedState !== null && e._visibility & 2 && (t.return === null || t.return.tag !== 13) ? (e._visibility &= -3, si(t)) : In(t);
        break;
      default:
        In(t);
    }
  }
  function si(t) {
    var e = t.deletions;
    if ((t.flags & 16) !== 0) {
      if (e !== null)
        for (var l = 0; l < e.length; l++) {
          var a = e[l];
          Ft = a, ld(
            a,
            t
          );
        }
      td(t);
    }
    for (t = t.child; t !== null; ) {
      switch (e = t, e.tag) {
        case 0:
        case 11:
        case 15:
          Cl(8, e, e.return), si(e);
          break;
        case 22:
          l = e.stateNode, l._visibility & 2 && (l._visibility &= -3, si(e));
          break;
        default:
          si(e);
      }
      t = t.sibling;
    }
  }
  function ld(t, e) {
    for (; Ft !== null; ) {
      var l = Ft;
      switch (l.tag) {
        case 0:
        case 11:
        case 15:
          Cl(8, l, e);
          break;
        case 23:
        case 22:
          if (l.memoizedState !== null && l.memoizedState.cachePool !== null) {
            var a = l.memoizedState.cachePool.pool;
            a != null && a.refCount++;
          }
          break;
        case 24:
          Rn(l.memoizedState.cache);
      }
      if (a = l.child, a !== null) a.return = l, Ft = a;
      else
        t: for (l = t; Ft !== null; ) {
          a = Ft;
          var n = a.sibling, u = a.return;
          if (Jo(a), a === l) {
            Ft = null;
            break t;
          }
          if (n !== null) {
            n.return = u, Ft = n;
            break t;
          }
          Ft = u;
        }
    }
  }
  var $m = {
    getCacheForType: function(t) {
      var e = ee(Qt), l = e.data.get(t);
      return l === void 0 && (l = t(), e.data.set(t, l)), l;
    },
    cacheSignal: function() {
      return ee(Qt).controller.signal;
    }
  }, Wm = typeof WeakMap == "function" ? WeakMap : Map, pt = 0, Tt = null, it = null, ft = 0, St = 0, Ae = null, Rl = !1, en = !1, vf = !1, ml = 0, Ct = 0, Hl = 0, ba = 0, gf = 0, xe = 0, ln = 0, Pn = null, he = null, pf = !1, ri = 0, ad = 0, oi = 1 / 0, di = null, Bl = null, Jt = 0, Ll = null, an = null, hl = 0, bf = 0, Sf = null, nd = null, tu = 0, _f = null;
  function ze() {
    return (pt & 2) !== 0 && ft !== 0 ? ft & -ft : z.T !== null ? qf() : bs();
  }
  function ud() {
    if (xe === 0)
      if ((ft & 536870912) === 0 || rt) {
        var t = Su;
        Su <<= 1, (Su & 3932160) === 0 && (Su = 262144), xe = t;
      } else xe = 536870912;
    return t = _e.current, t !== null && (t.flags |= 32), xe;
  }
  function ve(t, e, l) {
    (t === Tt && (St === 2 || St === 9) || t.cancelPendingCommit !== null) && (nn(t, 0), Yl(
      t,
      ft,
      xe,
      !1
    )), _n(t, l), ((pt & 2) === 0 || t !== Tt) && (t === Tt && ((pt & 2) === 0 && (ba |= l), Ct === 4 && Yl(
      t,
      ft,
      xe,
      !1
    )), We(t));
  }
  function id(t, e, l) {
    if ((pt & 6) !== 0) throw Error(s(327));
    var a = !l && (e & 127) === 0 && (e & t.expiredLanes) === 0 || Sn(t, e), n = a ? Pm(t, e) : Af(t, e, !0), u = a;
    do {
      if (n === 0) {
        en && !a && Yl(t, e, 0, !1);
        break;
      } else {
        if (l = t.current.alternate, u && !Fm(l)) {
          n = Af(t, e, !1), u = !1;
          continue;
        }
        if (n === 2) {
          if (u = e, t.errorRecoveryDisabledLanes & u)
            var i = 0;
          else
            i = t.pendingLanes & -536870913, i = i !== 0 ? i : i & 536870912 ? 536870912 : 0;
          if (i !== 0) {
            e = i;
            t: {
              var o = t;
              n = Pn;
              var d = o.current.memoizedState.isDehydrated;
              if (d && (nn(o, i).flags |= 256), i = Af(
                o,
                i,
                !1
              ), i !== 2) {
                if (vf && !d) {
                  o.errorRecoveryDisabledLanes |= u, ba |= u, n = 4;
                  break t;
                }
                u = he, he = n, u !== null && (he === null ? he = u : he.push.apply(
                  he,
                  u
                ));
              }
              n = i;
            }
            if (u = !1, n !== 2) continue;
          }
        }
        if (n === 1) {
          nn(t, 0), Yl(t, e, 0, !0);
          break;
        }
        t: {
          switch (a = t, u = n, u) {
            case 0:
            case 1:
              throw Error(s(345));
            case 4:
              if ((e & 4194048) !== e) break;
            case 6:
              Yl(
                a,
                e,
                xe,
                !Rl
              );
              break t;
            case 2:
              he = null;
              break;
            case 3:
            case 5:
              break;
            default:
              throw Error(s(329));
          }
          if ((e & 62914560) === e && (n = ri + 300 - It(), 10 < n)) {
            if (Yl(
              a,
              e,
              xe,
              !Rl
            ), Eu(a, 0, !0) !== 0) break t;
            hl = e, a.timeoutHandle = Bd(
              cd.bind(
                null,
                a,
                l,
                he,
                di,
                pf,
                e,
                xe,
                ba,
                ln,
                Rl,
                u,
                "Throttled",
                -0,
                0
              ),
              n
            );
            break t;
          }
          cd(
            a,
            l,
            he,
            di,
            pf,
            e,
            xe,
            ba,
            ln,
            Rl,
            u,
            null,
            -0,
            0
          );
        }
      }
      break;
    } while (!0);
    We(t);
  }
  function cd(t, e, l, a, n, u, i, o, d, _, T, M, E, A) {
    if (t.timeoutHandle = -1, M = e.subtreeFlags, M & 8192 || (M & 16785408) === 16785408) {
      M = {
        stylesheets: null,
        count: 0,
        imgCount: 0,
        imgBytes: 0,
        suspenseyImages: [],
        waitingForImages: !0,
        waitingForViewTransition: !1,
        unsuspend: tl
      }, Po(
        e,
        u,
        M
      );
      var G = (u & 62914560) === u ? ri - It() : (u & 4194048) === u ? ad - It() : 0;
      if (G = Rh(
        M,
        G
      ), G !== null) {
        hl = u, t.cancelPendingCommit = G(
          hd.bind(
            null,
            t,
            e,
            u,
            l,
            a,
            n,
            i,
            o,
            d,
            T,
            M,
            null,
            E,
            A
          )
        ), Yl(t, u, i, !_);
        return;
      }
    }
    hd(
      t,
      e,
      u,
      l,
      a,
      n,
      i,
      o,
      d
    );
  }
  function Fm(t) {
    for (var e = t; ; ) {
      var l = e.tag;
      if ((l === 0 || l === 11 || l === 15) && e.flags & 16384 && (l = e.updateQueue, l !== null && (l = l.stores, l !== null)))
        for (var a = 0; a < l.length; a++) {
          var n = l[a], u = n.getSnapshot;
          n = n.value;
          try {
            if (!be(u(), n)) return !1;
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
  function Yl(t, e, l, a) {
    e &= ~gf, e &= ~ba, t.suspendedLanes |= e, t.pingedLanes &= ~e, a && (t.warmLanes |= e), a = t.expirationTimes;
    for (var n = e; 0 < n; ) {
      var u = 31 - et(n), i = 1 << u;
      a[u] = -1, n &= ~i;
    }
    l !== 0 && vs(t, l, e);
  }
  function yi() {
    return (pt & 6) === 0 ? (eu(0), !1) : !0;
  }
  function Ef() {
    if (it !== null) {
      if (St === 0)
        var t = it.return;
      else
        t = it, nl = ra = null, Bc(t), ka = null, Bn = 0, t = it;
      for (; t !== null; )
        Lo(t.alternate, t), t = t.return;
      it = null;
    }
  }
  function nn(t, e) {
    var l = t.timeoutHandle;
    l !== -1 && (t.timeoutHandle = -1, gh(l)), l = t.cancelPendingCommit, l !== null && (t.cancelPendingCommit = null, l()), hl = 0, Ef(), Tt = t, it = l = ll(t.current, null), ft = e, St = 0, Ae = null, Rl = !1, en = Sn(t, e), vf = !1, ln = xe = gf = ba = Hl = Ct = 0, he = Pn = null, pf = !1, (e & 8) !== 0 && (e |= e & 32);
    var a = t.entangledLanes;
    if (a !== 0)
      for (t = t.entanglements, a &= e; 0 < a; ) {
        var n = 31 - et(a), u = 1 << n;
        e |= t[n], a &= ~u;
      }
    return ml = e, Cu(), l;
  }
  function fd(t, e) {
    lt = null, z.H = wn, e === Ka || e === Xu ? (e = Ar(), St = 3) : e === zc ? (e = Ar(), St = 4) : St = e === Pc ? 8 : e !== null && typeof e == "object" && typeof e.then == "function" ? 6 : 1, Ae = e, it === null && (Ct = 1, li(
      t,
      Me(e, t.current)
    ));
  }
  function sd() {
    var t = _e.current;
    return t === null ? !0 : (ft & 4194048) === ft ? Ce === null : (ft & 62914560) === ft || (ft & 536870912) !== 0 ? t === Ce : !1;
  }
  function rd() {
    var t = z.H;
    return z.H = wn, t === null ? wn : t;
  }
  function od() {
    var t = z.A;
    return z.A = $m, t;
  }
  function mi() {
    Ct = 4, Rl || (ft & 4194048) !== ft && _e.current !== null || (en = !0), (Hl & 134217727) === 0 && (ba & 134217727) === 0 || Tt === null || Yl(
      Tt,
      ft,
      xe,
      !1
    );
  }
  function Af(t, e, l) {
    var a = pt;
    pt |= 2;
    var n = rd(), u = od();
    (Tt !== t || ft !== e) && (di = null, nn(t, e)), e = !1;
    var i = Ct;
    t: do
      try {
        if (St !== 0 && it !== null) {
          var o = it, d = Ae;
          switch (St) {
            case 8:
              Ef(), i = 6;
              break t;
            case 3:
            case 2:
            case 9:
            case 6:
              _e.current === null && (e = !0);
              var _ = St;
              if (St = 0, Ae = null, un(t, o, d, _), l && en) {
                i = 0;
                break t;
              }
              break;
            default:
              _ = St, St = 0, Ae = null, un(t, o, d, _);
          }
        }
        Im(), i = Ct;
        break;
      } catch (T) {
        fd(t, T);
      }
    while (!0);
    return e && t.shellSuspendCounter++, nl = ra = null, pt = a, z.H = n, z.A = u, it === null && (Tt = null, ft = 0, Cu()), i;
  }
  function Im() {
    for (; it !== null; ) dd(it);
  }
  function Pm(t, e) {
    var l = pt;
    pt |= 2;
    var a = rd(), n = od();
    Tt !== t || ft !== e ? (di = null, oi = It() + 500, nn(t, e)) : en = Sn(
      t,
      e
    );
    t: do
      try {
        if (St !== 0 && it !== null) {
          e = it;
          var u = Ae;
          e: switch (St) {
            case 1:
              St = 0, Ae = null, un(t, e, u, 1);
              break;
            case 2:
            case 9:
              if (_r(u)) {
                St = 0, Ae = null, yd(e);
                break;
              }
              e = function() {
                St !== 2 && St !== 9 || Tt !== t || (St = 7), We(t);
              }, u.then(e, e);
              break t;
            case 3:
              St = 7;
              break t;
            case 4:
              St = 5;
              break t;
            case 7:
              _r(u) ? (St = 0, Ae = null, yd(e)) : (St = 0, Ae = null, un(t, e, u, 7));
              break;
            case 5:
              var i = null;
              switch (it.tag) {
                case 26:
                  i = it.memoizedState;
                case 5:
                case 27:
                  var o = it;
                  if (i ? Id(i) : o.stateNode.complete) {
                    St = 0, Ae = null;
                    var d = o.sibling;
                    if (d !== null) it = d;
                    else {
                      var _ = o.return;
                      _ !== null ? (it = _, hi(_)) : it = null;
                    }
                    break e;
                  }
              }
              St = 0, Ae = null, un(t, e, u, 5);
              break;
            case 6:
              St = 0, Ae = null, un(t, e, u, 6);
              break;
            case 8:
              Ef(), Ct = 6;
              break t;
            default:
              throw Error(s(462));
          }
        }
        th();
        break;
      } catch (T) {
        fd(t, T);
      }
    while (!0);
    return nl = ra = null, z.H = a, z.A = n, pt = l, it !== null ? 0 : (Tt = null, ft = 0, Cu(), Ct);
  }
  function th() {
    for (; it !== null && !Pl(); )
      dd(it);
  }
  function dd(t) {
    var e = Ho(t.alternate, t, ml);
    t.memoizedProps = t.pendingProps, e === null ? hi(t) : it = e;
  }
  function yd(t) {
    var e = t, l = e.alternate;
    switch (e.tag) {
      case 15:
      case 0:
        e = Mo(
          l,
          e,
          e.pendingProps,
          e.type,
          void 0,
          ft
        );
        break;
      case 11:
        e = Mo(
          l,
          e,
          e.pendingProps,
          e.type.render,
          e.ref,
          ft
        );
        break;
      case 5:
        Bc(e);
      default:
        Lo(l, e), e = it = rr(e, ml), e = Ho(l, e, ml);
    }
    t.memoizedProps = t.pendingProps, e === null ? hi(t) : it = e;
  }
  function un(t, e, l, a) {
    nl = ra = null, Bc(e), ka = null, Bn = 0;
    var n = e.return;
    try {
      if (Xm(
        t,
        n,
        e,
        l,
        ft
      )) {
        Ct = 1, li(
          t,
          Me(l, t.current)
        ), it = null;
        return;
      }
    } catch (u) {
      if (n !== null) throw it = n, u;
      Ct = 1, li(
        t,
        Me(l, t.current)
      ), it = null;
      return;
    }
    e.flags & 32768 ? (rt || a === 1 ? t = !0 : en || (ft & 536870912) !== 0 ? t = !1 : (Rl = t = !0, (a === 2 || a === 9 || a === 3 || a === 6) && (a = _e.current, a !== null && a.tag === 13 && (a.flags |= 16384))), md(e, t)) : hi(e);
  }
  function hi(t) {
    var e = t;
    do {
      if ((e.flags & 32768) !== 0) {
        md(
          e,
          Rl
        );
        return;
      }
      t = e.return;
      var l = wm(
        e.alternate,
        e,
        ml
      );
      if (l !== null) {
        it = l;
        return;
      }
      if (e = e.sibling, e !== null) {
        it = e;
        return;
      }
      it = e = t;
    } while (e !== null);
    Ct === 0 && (Ct = 5);
  }
  function md(t, e) {
    do {
      var l = Jm(t.alternate, t);
      if (l !== null) {
        l.flags &= 32767, it = l;
        return;
      }
      if (l = t.return, l !== null && (l.flags |= 32768, l.subtreeFlags = 0, l.deletions = null), !e && (t = t.sibling, t !== null)) {
        it = t;
        return;
      }
      it = t = l;
    } while (t !== null);
    Ct = 6, it = null;
  }
  function hd(t, e, l, a, n, u, i, o, d) {
    t.cancelPendingCommit = null;
    do
      vi();
    while (Jt !== 0);
    if ((pt & 6) !== 0) throw Error(s(327));
    if (e !== null) {
      if (e === t.current) throw Error(s(177));
      if (u = e.lanes | e.childLanes, u |= rc, jy(
        t,
        l,
        u,
        i,
        o,
        d
      ), t === Tt && (it = Tt = null, ft = 0), an = e, Ll = t, hl = l, bf = u, Sf = n, nd = a, (e.subtreeFlags & 10256) !== 0 || (e.flags & 10256) !== 0 ? (t.callbackNode = null, t.callbackPriority = 0, nh(El, function() {
        return Sd(), null;
      })) : (t.callbackNode = null, t.callbackPriority = 0), a = (e.flags & 13878) !== 0, (e.subtreeFlags & 13878) !== 0 || a) {
        a = z.T, z.T = null, n = R.p, R.p = 2, i = pt, pt |= 4;
        try {
          Km(t, e, l);
        } finally {
          pt = i, R.p = n, z.T = a;
        }
      }
      Jt = 1, vd(), gd(), pd();
    }
  }
  function vd() {
    if (Jt === 1) {
      Jt = 0;
      var t = Ll, e = an, l = (e.flags & 13878) !== 0;
      if ((e.subtreeFlags & 13878) !== 0 || l) {
        l = z.T, z.T = null;
        var a = R.p;
        R.p = 2;
        var n = pt;
        pt |= 4;
        try {
          Wo(e, t);
          var u = Rf, i = er(t.containerInfo), o = u.focusedElem, d = u.selectionRange;
          if (i !== o && o && o.ownerDocument && tr(
            o.ownerDocument.documentElement,
            o
          )) {
            if (d !== null && uc(o)) {
              var _ = d.start, T = d.end;
              if (T === void 0 && (T = _), "selectionStart" in o)
                o.selectionStart = _, o.selectionEnd = Math.min(
                  T,
                  o.value.length
                );
              else {
                var M = o.ownerDocument || document, E = M && M.defaultView || window;
                if (E.getSelection) {
                  var A = E.getSelection(), G = o.textContent.length, K = Math.min(d.start, G), zt = d.end === void 0 ? K : Math.min(d.end, G);
                  !A.extend && K > zt && (i = zt, zt = K, K = i);
                  var v = Ps(
                    o,
                    K
                  ), y = Ps(
                    o,
                    zt
                  );
                  if (v && y && (A.rangeCount !== 1 || A.anchorNode !== v.node || A.anchorOffset !== v.offset || A.focusNode !== y.node || A.focusOffset !== y.offset)) {
                    var S = M.createRange();
                    S.setStart(v.node, v.offset), A.removeAllRanges(), K > zt ? (A.addRange(S), A.extend(y.node, y.offset)) : (S.setEnd(y.node, y.offset), A.addRange(S));
                  }
                }
              }
            }
            for (M = [], A = o; A = A.parentNode; )
              A.nodeType === 1 && M.push({
                element: A,
                left: A.scrollLeft,
                top: A.scrollTop
              });
            for (typeof o.focus == "function" && o.focus(), o = 0; o < M.length; o++) {
              var O = M[o];
              O.element.scrollLeft = O.left, O.element.scrollTop = O.top;
            }
          }
          Ni = !!Cf, Rf = Cf = null;
        } finally {
          pt = n, R.p = a, z.T = l;
        }
      }
      t.current = e, Jt = 2;
    }
  }
  function gd() {
    if (Jt === 2) {
      Jt = 0;
      var t = Ll, e = an, l = (e.flags & 8772) !== 0;
      if ((e.subtreeFlags & 8772) !== 0 || l) {
        l = z.T, z.T = null;
        var a = R.p;
        R.p = 2;
        var n = pt;
        pt |= 4;
        try {
          wo(t, e.alternate, e);
        } finally {
          pt = n, R.p = a, z.T = l;
        }
      }
      Jt = 3;
    }
  }
  function pd() {
    if (Jt === 4 || Jt === 3) {
      Jt = 0, Hi();
      var t = Ll, e = an, l = hl, a = nd;
      (e.subtreeFlags & 10256) !== 0 || (e.flags & 10256) !== 0 ? Jt = 5 : (Jt = 0, an = Ll = null, bd(t, t.pendingLanes));
      var n = t.pendingLanes;
      if (n === 0 && (Bl = null), Gi(l), e = e.stateNode, fe && typeof fe.onCommitFiberRoot == "function")
        try {
          fe.onCommitFiberRoot(
            Je,
            e,
            void 0,
            (e.current.flags & 128) === 128
          );
        } catch {
        }
      if (a !== null) {
        e = z.T, n = R.p, R.p = 2, z.T = null;
        try {
          for (var u = t.onRecoverableError, i = 0; i < a.length; i++) {
            var o = a[i];
            u(o.value, {
              componentStack: o.stack
            });
          }
        } finally {
          z.T = e, R.p = n;
        }
      }
      (hl & 3) !== 0 && vi(), We(t), n = t.pendingLanes, (l & 261930) !== 0 && (n & 42) !== 0 ? t === _f ? tu++ : (tu = 0, _f = t) : tu = 0, eu(0);
    }
  }
  function bd(t, e) {
    (t.pooledCacheLanes &= e) === 0 && (e = t.pooledCache, e != null && (t.pooledCache = null, Rn(e)));
  }
  function vi() {
    return vd(), gd(), pd(), Sd();
  }
  function Sd() {
    if (Jt !== 5) return !1;
    var t = Ll, e = bf;
    bf = 0;
    var l = Gi(hl), a = z.T, n = R.p;
    try {
      R.p = 32 > l ? 32 : l, z.T = null, l = Sf, Sf = null;
      var u = Ll, i = hl;
      if (Jt = 0, an = Ll = null, hl = 0, (pt & 6) !== 0) throw Error(s(331));
      var o = pt;
      if (pt |= 4, ed(u.current), Io(
        u,
        u.current,
        i,
        l
      ), pt = o, eu(0, !1), fe && typeof fe.onPostCommitFiberRoot == "function")
        try {
          fe.onPostCommitFiberRoot(Je, u);
        } catch {
        }
      return !0;
    } finally {
      R.p = n, z.T = a, bd(t, e);
    }
  }
  function _d(t, e, l) {
    e = Me(l, e), e = Ic(t.stateNode, e, 2), t = Dl(t, e, 2), t !== null && (_n(t, 2), We(t));
  }
  function _t(t, e, l) {
    if (t.tag === 3)
      _d(t, t, l);
    else
      for (; e !== null; ) {
        if (e.tag === 3) {
          _d(
            e,
            t,
            l
          );
          break;
        } else if (e.tag === 1) {
          var a = e.stateNode;
          if (typeof e.type.getDerivedStateFromError == "function" || typeof a.componentDidCatch == "function" && (Bl === null || !Bl.has(a))) {
            t = Me(l, t), l = Eo(2), a = Dl(e, l, 2), a !== null && (Ao(
              l,
              a,
              e,
              t
            ), _n(a, 2), We(a));
            break;
          }
        }
        e = e.return;
      }
  }
  function xf(t, e, l) {
    var a = t.pingCache;
    if (a === null) {
      a = t.pingCache = new Wm();
      var n = /* @__PURE__ */ new Set();
      a.set(e, n);
    } else
      n = a.get(e), n === void 0 && (n = /* @__PURE__ */ new Set(), a.set(e, n));
    n.has(l) || (vf = !0, n.add(l), t = eh.bind(null, t, e, l), e.then(t, t));
  }
  function eh(t, e, l) {
    var a = t.pingCache;
    a !== null && a.delete(e), t.pingedLanes |= t.suspendedLanes & l, t.warmLanes &= ~l, Tt === t && (ft & l) === l && (Ct === 4 || Ct === 3 && (ft & 62914560) === ft && 300 > It() - ri ? (pt & 2) === 0 && nn(t, 0) : gf |= l, ln === ft && (ln = 0)), We(t);
  }
  function Ed(t, e) {
    e === 0 && (e = hs()), t = ca(t, e), t !== null && (_n(t, e), We(t));
  }
  function lh(t) {
    var e = t.memoizedState, l = 0;
    e !== null && (l = e.retryLane), Ed(t, l);
  }
  function ah(t, e) {
    var l = 0;
    switch (t.tag) {
      case 31:
      case 13:
        var a = t.stateNode, n = t.memoizedState;
        n !== null && (l = n.retryLane);
        break;
      case 19:
        a = t.stateNode;
        break;
      case 22:
        a = t.stateNode._retryCache;
        break;
      default:
        throw Error(s(314));
    }
    a !== null && a.delete(e), Ed(t, l);
  }
  function nh(t, e) {
    return pn(t, e);
  }
  var gi = null, cn = null, zf = !1, pi = !1, Tf = !1, Gl = 0;
  function We(t) {
    t !== cn && t.next === null && (cn === null ? gi = cn = t : cn = cn.next = t), pi = !0, zf || (zf = !0, ih());
  }
  function eu(t, e) {
    if (!Tf && pi) {
      Tf = !0;
      do
        for (var l = !1, a = gi; a !== null; ) {
          if (t !== 0) {
            var n = a.pendingLanes;
            if (n === 0) var u = 0;
            else {
              var i = a.suspendedLanes, o = a.pingedLanes;
              u = (1 << 31 - et(42 | t) + 1) - 1, u &= n & ~(i & ~o), u = u & 201326741 ? u & 201326741 | 1 : u ? u | 2 : 0;
            }
            u !== 0 && (l = !0, Td(a, u));
          } else
            u = ft, u = Eu(
              a,
              a === Tt ? u : 0,
              a.cancelPendingCommit !== null || a.timeoutHandle !== -1
            ), (u & 3) === 0 || Sn(a, u) || (l = !0, Td(a, u));
          a = a.next;
        }
      while (l);
      Tf = !1;
    }
  }
  function uh() {
    Ad();
  }
  function Ad() {
    pi = zf = !1;
    var t = 0;
    Gl !== 0 && vh() && (t = Gl);
    for (var e = It(), l = null, a = gi; a !== null; ) {
      var n = a.next, u = xd(a, e);
      u === 0 ? (a.next = null, l === null ? gi = n : l.next = n, n === null && (cn = l)) : (l = a, (t !== 0 || (u & 3) !== 0) && (pi = !0)), a = n;
    }
    Jt !== 0 && Jt !== 5 || eu(t), Gl !== 0 && (Gl = 0);
  }
  function xd(t, e) {
    for (var l = t.suspendedLanes, a = t.pingedLanes, n = t.expirationTimes, u = t.pendingLanes & -62914561; 0 < u; ) {
      var i = 31 - et(u), o = 1 << i, d = n[i];
      d === -1 ? ((o & l) === 0 || (o & a) !== 0) && (n[i] = Uy(o, e)) : d <= e && (t.expiredLanes |= o), u &= ~o;
    }
    if (e = Tt, l = ft, l = Eu(
      t,
      t === e ? l : 0,
      t.cancelPendingCommit !== null || t.timeoutHandle !== -1
    ), a = t.callbackNode, l === 0 || t === e && (St === 2 || St === 9) || t.cancelPendingCommit !== null)
      return a !== null && a !== null && Ta(a), t.callbackNode = null, t.callbackPriority = 0;
    if ((l & 3) === 0 || Sn(t, l)) {
      if (e = l & -l, e === t.callbackPriority) return e;
      switch (a !== null && Ta(a), Gi(l)) {
        case 2:
        case 8:
          l = bn;
          break;
        case 32:
          l = El;
          break;
        case 268435456:
          l = pu;
          break;
        default:
          l = El;
      }
      return a = zd.bind(null, t), l = pn(l, a), t.callbackPriority = e, t.callbackNode = l, e;
    }
    return a !== null && a !== null && Ta(a), t.callbackPriority = 2, t.callbackNode = null, 2;
  }
  function zd(t, e) {
    if (Jt !== 0 && Jt !== 5)
      return t.callbackNode = null, t.callbackPriority = 0, null;
    var l = t.callbackNode;
    if (vi() && t.callbackNode !== l)
      return null;
    var a = ft;
    return a = Eu(
      t,
      t === Tt ? a : 0,
      t.cancelPendingCommit !== null || t.timeoutHandle !== -1
    ), a === 0 ? null : (id(t, a, e), xd(t, It()), t.callbackNode != null && t.callbackNode === l ? zd.bind(null, t) : null);
  }
  function Td(t, e) {
    if (vi()) return null;
    id(t, e, !0);
  }
  function ih() {
    ph(function() {
      (pt & 6) !== 0 ? pn(
        gu,
        uh
      ) : Ad();
    });
  }
  function qf() {
    if (Gl === 0) {
      var t = wa;
      t === 0 && (t = ea, ea <<= 1, (ea & 261888) === 0 && (ea = 256)), Gl = t;
    }
    return Gl;
  }
  function qd(t) {
    return t == null || typeof t == "symbol" || typeof t == "boolean" ? null : typeof t == "function" ? t : Tu("" + t);
  }
  function Nd(t, e) {
    var l = e.ownerDocument.createElement("input");
    return l.name = e.name, l.value = e.value, t.id && l.setAttribute("form", t.id), e.parentNode.insertBefore(l, e), t = new FormData(t), l.parentNode.removeChild(l), t;
  }
  function ch(t, e, l, a, n) {
    if (e === "submit" && l && l.stateNode === n) {
      var u = qd(
        (n[re] || null).action
      ), i = a.submitter;
      i && (e = (e = i[re] || null) ? qd(e.formAction) : i.getAttribute("formAction"), e !== null && (u = e, i = null));
      var o = new Mu(
        "action",
        "action",
        null,
        a,
        n
      );
      t.push({
        event: o,
        listeners: [
          {
            instance: null,
            listener: function() {
              if (a.defaultPrevented) {
                if (Gl !== 0) {
                  var d = i ? Nd(n, i) : new FormData(n);
                  Jc(
                    l,
                    {
                      pending: !0,
                      data: d,
                      method: n.method,
                      action: u
                    },
                    null,
                    d
                  );
                }
              } else
                typeof u == "function" && (o.preventDefault(), d = i ? Nd(n, i) : new FormData(n), Jc(
                  l,
                  {
                    pending: !0,
                    data: d,
                    method: n.method,
                    action: u
                  },
                  u,
                  d
                ));
            },
            currentTarget: n
          }
        ]
      });
    }
  }
  for (var Nf = 0; Nf < sc.length; Nf++) {
    var Of = sc[Nf], fh = Of.toLowerCase(), sh = Of[0].toUpperCase() + Of.slice(1);
    Ye(
      fh,
      "on" + sh
    );
  }
  Ye(nr, "onAnimationEnd"), Ye(ur, "onAnimationIteration"), Ye(ir, "onAnimationStart"), Ye("dblclick", "onDoubleClick"), Ye("focusin", "onFocus"), Ye("focusout", "onBlur"), Ye(zm, "onTransitionRun"), Ye(Tm, "onTransitionStart"), Ye(qm, "onTransitionCancel"), Ye(cr, "onTransitionEnd"), Da("onMouseEnter", ["mouseout", "mouseover"]), Da("onMouseLeave", ["mouseout", "mouseover"]), Da("onPointerEnter", ["pointerout", "pointerover"]), Da("onPointerLeave", ["pointerout", "pointerover"]), aa(
    "onChange",
    "change click focusin focusout input keydown keyup selectionchange".split(" ")
  ), aa(
    "onSelect",
    "focusout contextmenu dragend focusin keydown keyup mousedown mouseup selectionchange".split(
      " "
    )
  ), aa("onBeforeInput", [
    "compositionend",
    "keypress",
    "textInput",
    "paste"
  ]), aa(
    "onCompositionEnd",
    "compositionend focusout keydown keypress keyup mousedown".split(" ")
  ), aa(
    "onCompositionStart",
    "compositionstart focusout keydown keypress keyup mousedown".split(" ")
  ), aa(
    "onCompositionUpdate",
    "compositionupdate focusout keydown keypress keyup mousedown".split(" ")
  );
  var lu = "abort canplay canplaythrough durationchange emptied encrypted ended error loadeddata loadedmetadata loadstart pause play playing progress ratechange resize seeked seeking stalled suspend timeupdate volumechange waiting".split(
    " "
  ), rh = new Set(
    "beforetoggle cancel close invalid load scroll scrollend toggle".split(" ").concat(lu)
  );
  function Od(t, e) {
    e = (e & 4) !== 0;
    for (var l = 0; l < t.length; l++) {
      var a = t[l], n = a.event;
      a = a.listeners;
      t: {
        var u = void 0;
        if (e)
          for (var i = a.length - 1; 0 <= i; i--) {
            var o = a[i], d = o.instance, _ = o.currentTarget;
            if (o = o.listener, d !== u && n.isPropagationStopped())
              break t;
            u = o, n.currentTarget = _;
            try {
              u(n);
            } catch (T) {
              ju(T);
            }
            n.currentTarget = null, u = d;
          }
        else
          for (i = 0; i < a.length; i++) {
            if (o = a[i], d = o.instance, _ = o.currentTarget, o = o.listener, d !== u && n.isPropagationStopped())
              break t;
            u = o, n.currentTarget = _;
            try {
              u(n);
            } catch (T) {
              ju(T);
            }
            n.currentTarget = null, u = d;
          }
      }
    }
  }
  function ct(t, e) {
    var l = e[Qi];
    l === void 0 && (l = e[Qi] = /* @__PURE__ */ new Set());
    var a = t + "__bubble";
    l.has(a) || (Md(e, t, 2, !1), l.add(a));
  }
  function Mf(t, e, l) {
    var a = 0;
    e && (a |= 4), Md(
      l,
      t,
      a,
      e
    );
  }
  var bi = "_reactListening" + Math.random().toString(36).slice(2);
  function Df(t) {
    if (!t[bi]) {
      t[bi] = !0, Es.forEach(function(l) {
        l !== "selectionchange" && (rh.has(l) || Mf(l, !1, t), Mf(l, !0, t));
      });
      var e = t.nodeType === 9 ? t : t.ownerDocument;
      e === null || e[bi] || (e[bi] = !0, Mf("selectionchange", !1, e));
    }
  }
  function Md(t, e, l, a) {
    switch (uy(e)) {
      case 2:
        var n = Lh;
        break;
      case 8:
        n = Yh;
        break;
      default:
        n = Jf;
    }
    l = n.bind(
      null,
      e,
      l,
      t
    ), n = void 0, !Wi || e !== "touchstart" && e !== "touchmove" && e !== "wheel" || (n = !0), a ? n !== void 0 ? t.addEventListener(e, l, {
      capture: !0,
      passive: n
    }) : t.addEventListener(e, l, !0) : n !== void 0 ? t.addEventListener(e, l, {
      passive: n
    }) : t.addEventListener(e, l, !1);
  }
  function Uf(t, e, l, a, n) {
    var u = a;
    if ((e & 1) === 0 && (e & 2) === 0 && a !== null)
      t: for (; ; ) {
        if (a === null) return;
        var i = a.tag;
        if (i === 3 || i === 4) {
          var o = a.stateNode.containerInfo;
          if (o === n) break;
          if (i === 4)
            for (i = a.return; i !== null; ) {
              var d = i.tag;
              if ((d === 3 || d === 4) && i.stateNode.containerInfo === n)
                return;
              i = i.return;
            }
          for (; o !== null; ) {
            if (i = Na(o), i === null) return;
            if (d = i.tag, d === 5 || d === 6 || d === 26 || d === 27) {
              a = u = i;
              continue t;
            }
            o = o.parentNode;
          }
        }
        a = a.return;
      }
    Cs(function() {
      var _ = u, T = ki(l), M = [];
      t: {
        var E = fr.get(t);
        if (E !== void 0) {
          var A = Mu, G = t;
          switch (t) {
            case "keypress":
              if (Nu(l) === 0) break t;
            case "keydown":
            case "keyup":
              A = am;
              break;
            case "focusin":
              G = "focus", A = tc;
              break;
            case "focusout":
              G = "blur", A = tc;
              break;
            case "beforeblur":
            case "afterblur":
              A = tc;
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
              A = Bs;
              break;
            case "drag":
            case "dragend":
            case "dragenter":
            case "dragexit":
            case "dragleave":
            case "dragover":
            case "dragstart":
            case "drop":
              A = wy;
              break;
            case "touchcancel":
            case "touchend":
            case "touchmove":
            case "touchstart":
              A = im;
              break;
            case nr:
            case ur:
            case ir:
              A = ky;
              break;
            case cr:
              A = fm;
              break;
            case "scroll":
            case "scrollend":
              A = Zy;
              break;
            case "wheel":
              A = rm;
              break;
            case "copy":
            case "cut":
            case "paste":
              A = Wy;
              break;
            case "gotpointercapture":
            case "lostpointercapture":
            case "pointercancel":
            case "pointerdown":
            case "pointermove":
            case "pointerout":
            case "pointerover":
            case "pointerup":
              A = Ys;
              break;
            case "toggle":
            case "beforetoggle":
              A = dm;
          }
          var K = (e & 4) !== 0, zt = !K && (t === "scroll" || t === "scrollend"), v = K ? E !== null ? E + "Capture" : null : E;
          K = [];
          for (var y = _, S; y !== null; ) {
            var O = y;
            if (S = O.stateNode, O = O.tag, O !== 5 && O !== 26 && O !== 27 || S === null || v === null || (O = xn(y, v), O != null && K.push(
              au(y, O, S)
            )), zt) break;
            y = y.return;
          }
          0 < K.length && (E = new A(
            E,
            G,
            null,
            l,
            T
          ), M.push({ event: E, listeners: K }));
        }
      }
      if ((e & 7) === 0) {
        t: {
          if (E = t === "mouseover" || t === "pointerover", A = t === "mouseout" || t === "pointerout", E && l !== Ki && (G = l.relatedTarget || l.fromElement) && (Na(G) || G[qa]))
            break t;
          if ((A || E) && (E = T.window === T ? T : (E = T.ownerDocument) ? E.defaultView || E.parentWindow : window, A ? (G = l.relatedTarget || l.toElement, A = _, G = G ? Na(G) : null, G !== null && (zt = g(G), K = G.tag, G !== zt || K !== 5 && K !== 27 && K !== 6) && (G = null)) : (A = null, G = _), A !== G)) {
            if (K = Bs, O = "onMouseLeave", v = "onMouseEnter", y = "mouse", (t === "pointerout" || t === "pointerover") && (K = Ys, O = "onPointerLeave", v = "onPointerEnter", y = "pointer"), zt = A == null ? E : An(A), S = G == null ? E : An(G), E = new K(
              O,
              y + "leave",
              A,
              l,
              T
            ), E.target = zt, E.relatedTarget = S, O = null, Na(T) === _ && (K = new K(
              v,
              y + "enter",
              G,
              l,
              T
            ), K.target = S, K.relatedTarget = zt, O = K), zt = O, A && G)
              e: {
                for (K = oh, v = A, y = G, S = 0, O = v; O; O = K(O))
                  S++;
                O = 0;
                for (var V = y; V; V = K(V))
                  O++;
                for (; 0 < S - O; )
                  v = K(v), S--;
                for (; 0 < O - S; )
                  y = K(y), O--;
                for (; S--; ) {
                  if (v === y || y !== null && v === y.alternate) {
                    K = v;
                    break e;
                  }
                  v = K(v), y = K(y);
                }
                K = null;
              }
            else K = null;
            A !== null && Dd(
              M,
              E,
              A,
              K,
              !1
            ), G !== null && zt !== null && Dd(
              M,
              zt,
              G,
              K,
              !0
            );
          }
        }
        t: {
          if (E = _ ? An(_) : window, A = E.nodeName && E.nodeName.toLowerCase(), A === "select" || A === "input" && E.type === "file")
            var mt = Ks;
          else if (ws(E))
            if (ks)
              mt = Em;
            else {
              mt = Sm;
              var X = bm;
            }
          else
            A = E.nodeName, !A || A.toLowerCase() !== "input" || E.type !== "checkbox" && E.type !== "radio" ? _ && Ji(_.elementType) && (mt = Ks) : mt = _m;
          if (mt && (mt = mt(t, _))) {
            Js(
              M,
              mt,
              l,
              T
            );
            break t;
          }
          X && X(t, E, _), t === "focusout" && _ && E.type === "number" && _.memoizedProps.value != null && wi(E, "number", E.value);
        }
        switch (X = _ ? An(_) : window, t) {
          case "focusin":
            (ws(X) || X.contentEditable === "true") && (Ba = X, ic = _, Un = null);
            break;
          case "focusout":
            Un = ic = Ba = null;
            break;
          case "mousedown":
            cc = !0;
            break;
          case "contextmenu":
          case "mouseup":
          case "dragend":
            cc = !1, lr(M, l, T);
            break;
          case "selectionchange":
            if (xm) break;
          case "keydown":
          case "keyup":
            lr(M, l, T);
        }
        var ut;
        if (lc)
          t: {
            switch (t) {
              case "compositionstart":
                var st = "onCompositionStart";
                break t;
              case "compositionend":
                st = "onCompositionEnd";
                break t;
              case "compositionupdate":
                st = "onCompositionUpdate";
                break t;
            }
            st = void 0;
          }
        else
          Ha ? Zs(t, l) && (st = "onCompositionEnd") : t === "keydown" && l.keyCode === 229 && (st = "onCompositionStart");
        st && (Gs && l.locale !== "ko" && (Ha || st !== "onCompositionStart" ? st === "onCompositionEnd" && Ha && (ut = Rs()) : (xl = T, Fi = "value" in xl ? xl.value : xl.textContent, Ha = !0)), X = Si(_, st), 0 < X.length && (st = new Ls(
          st,
          t,
          null,
          l,
          T
        ), M.push({ event: st, listeners: X }), ut ? st.data = ut : (ut = Vs(l), ut !== null && (st.data = ut)))), (ut = mm ? hm(t, l) : vm(t, l)) && (st = Si(_, "onBeforeInput"), 0 < st.length && (X = new Ls(
          "onBeforeInput",
          "beforeinput",
          null,
          l,
          T
        ), M.push({
          event: X,
          listeners: st
        }), X.data = ut)), ch(
          M,
          t,
          _,
          l,
          T
        );
      }
      Od(M, e);
    });
  }
  function au(t, e, l) {
    return {
      instance: t,
      listener: e,
      currentTarget: l
    };
  }
  function Si(t, e) {
    for (var l = e + "Capture", a = []; t !== null; ) {
      var n = t, u = n.stateNode;
      if (n = n.tag, n !== 5 && n !== 26 && n !== 27 || u === null || (n = xn(t, l), n != null && a.unshift(
        au(t, n, u)
      ), n = xn(t, e), n != null && a.push(
        au(t, n, u)
      )), t.tag === 3) return a;
      t = t.return;
    }
    return [];
  }
  function oh(t) {
    if (t === null) return null;
    do
      t = t.return;
    while (t && t.tag !== 5 && t.tag !== 27);
    return t || null;
  }
  function Dd(t, e, l, a, n) {
    for (var u = e._reactName, i = []; l !== null && l !== a; ) {
      var o = l, d = o.alternate, _ = o.stateNode;
      if (o = o.tag, d !== null && d === a) break;
      o !== 5 && o !== 26 && o !== 27 || _ === null || (d = _, n ? (_ = xn(l, u), _ != null && i.unshift(
        au(l, _, d)
      )) : n || (_ = xn(l, u), _ != null && i.push(
        au(l, _, d)
      ))), l = l.return;
    }
    i.length !== 0 && t.push({ event: e, listeners: i });
  }
  var dh = /\r\n?/g, yh = /\u0000|\uFFFD/g;
  function Ud(t) {
    return (typeof t == "string" ? t : "" + t).replace(dh, `
`).replace(yh, "");
  }
  function jd(t, e) {
    return e = Ud(e), Ud(t) === e;
  }
  function xt(t, e, l, a, n, u) {
    switch (l) {
      case "children":
        typeof a == "string" ? e === "body" || e === "textarea" && a === "" || ja(t, a) : (typeof a == "number" || typeof a == "bigint") && e !== "body" && ja(t, "" + a);
        break;
      case "className":
        xu(t, "class", a);
        break;
      case "tabIndex":
        xu(t, "tabindex", a);
        break;
      case "dir":
      case "role":
      case "viewBox":
      case "width":
      case "height":
        xu(t, l, a);
        break;
      case "style":
        Us(t, a, u);
        break;
      case "data":
        if (e !== "object") {
          xu(t, "data", a);
          break;
        }
      case "src":
      case "href":
        if (a === "" && (e !== "a" || l !== "href")) {
          t.removeAttribute(l);
          break;
        }
        if (a == null || typeof a == "function" || typeof a == "symbol" || typeof a == "boolean") {
          t.removeAttribute(l);
          break;
        }
        a = Tu("" + a), t.setAttribute(l, a);
        break;
      case "action":
      case "formAction":
        if (typeof a == "function") {
          t.setAttribute(
            l,
            "javascript:throw new Error('A React form was unexpectedly submitted. If you called form.submit() manually, consider using form.requestSubmit() instead. If you\\'re trying to use event.stopPropagation() in a submit event handler, consider also calling event.preventDefault().')"
          );
          break;
        } else
          typeof u == "function" && (l === "formAction" ? (e !== "input" && xt(t, e, "name", n.name, n, null), xt(
            t,
            e,
            "formEncType",
            n.formEncType,
            n,
            null
          ), xt(
            t,
            e,
            "formMethod",
            n.formMethod,
            n,
            null
          ), xt(
            t,
            e,
            "formTarget",
            n.formTarget,
            n,
            null
          )) : (xt(t, e, "encType", n.encType, n, null), xt(t, e, "method", n.method, n, null), xt(t, e, "target", n.target, n, null)));
        if (a == null || typeof a == "symbol" || typeof a == "boolean") {
          t.removeAttribute(l);
          break;
        }
        a = Tu("" + a), t.setAttribute(l, a);
        break;
      case "onClick":
        a != null && (t.onclick = tl);
        break;
      case "onScroll":
        a != null && ct("scroll", t);
        break;
      case "onScrollEnd":
        a != null && ct("scrollend", t);
        break;
      case "dangerouslySetInnerHTML":
        if (a != null) {
          if (typeof a != "object" || !("__html" in a))
            throw Error(s(61));
          if (l = a.__html, l != null) {
            if (n.children != null) throw Error(s(60));
            t.innerHTML = l;
          }
        }
        break;
      case "multiple":
        t.multiple = a && typeof a != "function" && typeof a != "symbol";
        break;
      case "muted":
        t.muted = a && typeof a != "function" && typeof a != "symbol";
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
        if (a == null || typeof a == "function" || typeof a == "boolean" || typeof a == "symbol") {
          t.removeAttribute("xlink:href");
          break;
        }
        l = Tu("" + a), t.setAttributeNS(
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
        a != null && typeof a != "function" && typeof a != "symbol" ? t.setAttribute(l, "" + a) : t.removeAttribute(l);
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
        a && typeof a != "function" && typeof a != "symbol" ? t.setAttribute(l, "") : t.removeAttribute(l);
        break;
      case "capture":
      case "download":
        a === !0 ? t.setAttribute(l, "") : a !== !1 && a != null && typeof a != "function" && typeof a != "symbol" ? t.setAttribute(l, a) : t.removeAttribute(l);
        break;
      case "cols":
      case "rows":
      case "size":
      case "span":
        a != null && typeof a != "function" && typeof a != "symbol" && !isNaN(a) && 1 <= a ? t.setAttribute(l, a) : t.removeAttribute(l);
        break;
      case "rowSpan":
      case "start":
        a == null || typeof a == "function" || typeof a == "symbol" || isNaN(a) ? t.removeAttribute(l) : t.setAttribute(l, a);
        break;
      case "popover":
        ct("beforetoggle", t), ct("toggle", t), Au(t, "popover", a);
        break;
      case "xlinkActuate":
        Pe(
          t,
          "http://www.w3.org/1999/xlink",
          "xlink:actuate",
          a
        );
        break;
      case "xlinkArcrole":
        Pe(
          t,
          "http://www.w3.org/1999/xlink",
          "xlink:arcrole",
          a
        );
        break;
      case "xlinkRole":
        Pe(
          t,
          "http://www.w3.org/1999/xlink",
          "xlink:role",
          a
        );
        break;
      case "xlinkShow":
        Pe(
          t,
          "http://www.w3.org/1999/xlink",
          "xlink:show",
          a
        );
        break;
      case "xlinkTitle":
        Pe(
          t,
          "http://www.w3.org/1999/xlink",
          "xlink:title",
          a
        );
        break;
      case "xlinkType":
        Pe(
          t,
          "http://www.w3.org/1999/xlink",
          "xlink:type",
          a
        );
        break;
      case "xmlBase":
        Pe(
          t,
          "http://www.w3.org/XML/1998/namespace",
          "xml:base",
          a
        );
        break;
      case "xmlLang":
        Pe(
          t,
          "http://www.w3.org/XML/1998/namespace",
          "xml:lang",
          a
        );
        break;
      case "xmlSpace":
        Pe(
          t,
          "http://www.w3.org/XML/1998/namespace",
          "xml:space",
          a
        );
        break;
      case "is":
        Au(t, "is", a);
        break;
      case "innerText":
      case "textContent":
        break;
      default:
        (!(2 < l.length) || l[0] !== "o" && l[0] !== "O" || l[1] !== "n" && l[1] !== "N") && (l = Qy.get(l) || l, Au(t, l, a));
    }
  }
  function jf(t, e, l, a, n, u) {
    switch (l) {
      case "style":
        Us(t, a, u);
        break;
      case "dangerouslySetInnerHTML":
        if (a != null) {
          if (typeof a != "object" || !("__html" in a))
            throw Error(s(61));
          if (l = a.__html, l != null) {
            if (n.children != null) throw Error(s(60));
            t.innerHTML = l;
          }
        }
        break;
      case "children":
        typeof a == "string" ? ja(t, a) : (typeof a == "number" || typeof a == "bigint") && ja(t, "" + a);
        break;
      case "onScroll":
        a != null && ct("scroll", t);
        break;
      case "onScrollEnd":
        a != null && ct("scrollend", t);
        break;
      case "onClick":
        a != null && (t.onclick = tl);
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
        if (!As.hasOwnProperty(l))
          t: {
            if (l[0] === "o" && l[1] === "n" && (n = l.endsWith("Capture"), e = l.slice(2, n ? l.length - 7 : void 0), u = t[re] || null, u = u != null ? u[l] : null, typeof u == "function" && t.removeEventListener(e, u, n), typeof a == "function")) {
              typeof u != "function" && u !== null && (l in t ? t[l] = null : t.hasAttribute(l) && t.removeAttribute(l)), t.addEventListener(e, a, n);
              break t;
            }
            l in t ? t[l] = a : a === !0 ? t.setAttribute(l, "") : Au(t, l, a);
          }
    }
  }
  function ae(t, e, l) {
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
        ct("error", t), ct("load", t);
        var a = !1, n = !1, u;
        for (u in l)
          if (l.hasOwnProperty(u)) {
            var i = l[u];
            if (i != null)
              switch (u) {
                case "src":
                  a = !0;
                  break;
                case "srcSet":
                  n = !0;
                  break;
                case "children":
                case "dangerouslySetInnerHTML":
                  throw Error(s(137, e));
                default:
                  xt(t, e, u, i, l, null);
              }
          }
        n && xt(t, e, "srcSet", l.srcSet, l, null), a && xt(t, e, "src", l.src, l, null);
        return;
      case "input":
        ct("invalid", t);
        var o = u = i = n = null, d = null, _ = null;
        for (a in l)
          if (l.hasOwnProperty(a)) {
            var T = l[a];
            if (T != null)
              switch (a) {
                case "name":
                  n = T;
                  break;
                case "type":
                  i = T;
                  break;
                case "checked":
                  d = T;
                  break;
                case "defaultChecked":
                  _ = T;
                  break;
                case "value":
                  u = T;
                  break;
                case "defaultValue":
                  o = T;
                  break;
                case "children":
                case "dangerouslySetInnerHTML":
                  if (T != null)
                    throw Error(s(137, e));
                  break;
                default:
                  xt(t, e, a, T, l, null);
              }
          }
        Ns(
          t,
          u,
          o,
          d,
          _,
          i,
          n,
          !1
        );
        return;
      case "select":
        ct("invalid", t), a = i = u = null;
        for (n in l)
          if (l.hasOwnProperty(n) && (o = l[n], o != null))
            switch (n) {
              case "value":
                u = o;
                break;
              case "defaultValue":
                i = o;
                break;
              case "multiple":
                a = o;
              default:
                xt(t, e, n, o, l, null);
            }
        e = u, l = i, t.multiple = !!a, e != null ? Ua(t, !!a, e, !1) : l != null && Ua(t, !!a, l, !0);
        return;
      case "textarea":
        ct("invalid", t), u = n = a = null;
        for (i in l)
          if (l.hasOwnProperty(i) && (o = l[i], o != null))
            switch (i) {
              case "value":
                a = o;
                break;
              case "defaultValue":
                n = o;
                break;
              case "children":
                u = o;
                break;
              case "dangerouslySetInnerHTML":
                if (o != null) throw Error(s(91));
                break;
              default:
                xt(t, e, i, o, l, null);
            }
        Ms(t, a, n, u);
        return;
      case "option":
        for (d in l)
          if (l.hasOwnProperty(d) && (a = l[d], a != null))
            switch (d) {
              case "selected":
                t.selected = a && typeof a != "function" && typeof a != "symbol";
                break;
              default:
                xt(t, e, d, a, l, null);
            }
        return;
      case "dialog":
        ct("beforetoggle", t), ct("toggle", t), ct("cancel", t), ct("close", t);
        break;
      case "iframe":
      case "object":
        ct("load", t);
        break;
      case "video":
      case "audio":
        for (a = 0; a < lu.length; a++)
          ct(lu[a], t);
        break;
      case "image":
        ct("error", t), ct("load", t);
        break;
      case "details":
        ct("toggle", t);
        break;
      case "embed":
      case "source":
      case "link":
        ct("error", t), ct("load", t);
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
        for (_ in l)
          if (l.hasOwnProperty(_) && (a = l[_], a != null))
            switch (_) {
              case "children":
              case "dangerouslySetInnerHTML":
                throw Error(s(137, e));
              default:
                xt(t, e, _, a, l, null);
            }
        return;
      default:
        if (Ji(e)) {
          for (T in l)
            l.hasOwnProperty(T) && (a = l[T], a !== void 0 && jf(
              t,
              e,
              T,
              a,
              l,
              void 0
            ));
          return;
        }
    }
    for (o in l)
      l.hasOwnProperty(o) && (a = l[o], a != null && xt(t, e, o, a, l, null));
  }
  function mh(t, e, l, a) {
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
        var n = null, u = null, i = null, o = null, d = null, _ = null, T = null;
        for (A in l) {
          var M = l[A];
          if (l.hasOwnProperty(A) && M != null)
            switch (A) {
              case "checked":
                break;
              case "value":
                break;
              case "defaultValue":
                d = M;
              default:
                a.hasOwnProperty(A) || xt(t, e, A, null, a, M);
            }
        }
        for (var E in a) {
          var A = a[E];
          if (M = l[E], a.hasOwnProperty(E) && (A != null || M != null))
            switch (E) {
              case "type":
                u = A;
                break;
              case "name":
                n = A;
                break;
              case "checked":
                _ = A;
                break;
              case "defaultChecked":
                T = A;
                break;
              case "value":
                i = A;
                break;
              case "defaultValue":
                o = A;
                break;
              case "children":
              case "dangerouslySetInnerHTML":
                if (A != null)
                  throw Error(s(137, e));
                break;
              default:
                A !== M && xt(
                  t,
                  e,
                  E,
                  A,
                  a,
                  M
                );
            }
        }
        Vi(
          t,
          i,
          o,
          d,
          _,
          T,
          u,
          n
        );
        return;
      case "select":
        A = i = o = E = null;
        for (u in l)
          if (d = l[u], l.hasOwnProperty(u) && d != null)
            switch (u) {
              case "value":
                break;
              case "multiple":
                A = d;
              default:
                a.hasOwnProperty(u) || xt(
                  t,
                  e,
                  u,
                  null,
                  a,
                  d
                );
            }
        for (n in a)
          if (u = a[n], d = l[n], a.hasOwnProperty(n) && (u != null || d != null))
            switch (n) {
              case "value":
                E = u;
                break;
              case "defaultValue":
                o = u;
                break;
              case "multiple":
                i = u;
              default:
                u !== d && xt(
                  t,
                  e,
                  n,
                  u,
                  a,
                  d
                );
            }
        e = o, l = i, a = A, E != null ? Ua(t, !!l, E, !1) : !!a != !!l && (e != null ? Ua(t, !!l, e, !0) : Ua(t, !!l, l ? [] : "", !1));
        return;
      case "textarea":
        A = E = null;
        for (o in l)
          if (n = l[o], l.hasOwnProperty(o) && n != null && !a.hasOwnProperty(o))
            switch (o) {
              case "value":
                break;
              case "children":
                break;
              default:
                xt(t, e, o, null, a, n);
            }
        for (i in a)
          if (n = a[i], u = l[i], a.hasOwnProperty(i) && (n != null || u != null))
            switch (i) {
              case "value":
                E = n;
                break;
              case "defaultValue":
                A = n;
                break;
              case "children":
                break;
              case "dangerouslySetInnerHTML":
                if (n != null) throw Error(s(91));
                break;
              default:
                n !== u && xt(t, e, i, n, a, u);
            }
        Os(t, E, A);
        return;
      case "option":
        for (var G in l)
          if (E = l[G], l.hasOwnProperty(G) && E != null && !a.hasOwnProperty(G))
            switch (G) {
              case "selected":
                t.selected = !1;
                break;
              default:
                xt(
                  t,
                  e,
                  G,
                  null,
                  a,
                  E
                );
            }
        for (d in a)
          if (E = a[d], A = l[d], a.hasOwnProperty(d) && E !== A && (E != null || A != null))
            switch (d) {
              case "selected":
                t.selected = E && typeof E != "function" && typeof E != "symbol";
                break;
              default:
                xt(
                  t,
                  e,
                  d,
                  E,
                  a,
                  A
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
        for (var K in l)
          E = l[K], l.hasOwnProperty(K) && E != null && !a.hasOwnProperty(K) && xt(t, e, K, null, a, E);
        for (_ in a)
          if (E = a[_], A = l[_], a.hasOwnProperty(_) && E !== A && (E != null || A != null))
            switch (_) {
              case "children":
              case "dangerouslySetInnerHTML":
                if (E != null)
                  throw Error(s(137, e));
                break;
              default:
                xt(
                  t,
                  e,
                  _,
                  E,
                  a,
                  A
                );
            }
        return;
      default:
        if (Ji(e)) {
          for (var zt in l)
            E = l[zt], l.hasOwnProperty(zt) && E !== void 0 && !a.hasOwnProperty(zt) && jf(
              t,
              e,
              zt,
              void 0,
              a,
              E
            );
          for (T in a)
            E = a[T], A = l[T], !a.hasOwnProperty(T) || E === A || E === void 0 && A === void 0 || jf(
              t,
              e,
              T,
              E,
              a,
              A
            );
          return;
        }
    }
    for (var v in l)
      E = l[v], l.hasOwnProperty(v) && E != null && !a.hasOwnProperty(v) && xt(t, e, v, null, a, E);
    for (M in a)
      E = a[M], A = l[M], !a.hasOwnProperty(M) || E === A || E == null && A == null || xt(t, e, M, E, a, A);
  }
  function Cd(t) {
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
  function hh() {
    if (typeof performance.getEntriesByType == "function") {
      for (var t = 0, e = 0, l = performance.getEntriesByType("resource"), a = 0; a < l.length; a++) {
        var n = l[a], u = n.transferSize, i = n.initiatorType, o = n.duration;
        if (u && o && Cd(i)) {
          for (i = 0, o = n.responseEnd, a += 1; a < l.length; a++) {
            var d = l[a], _ = d.startTime;
            if (_ > o) break;
            var T = d.transferSize, M = d.initiatorType;
            T && Cd(M) && (d = d.responseEnd, i += T * (d < o ? 1 : (o - _) / (d - _)));
          }
          if (--a, e += 8 * (u + i) / (n.duration / 1e3), t++, 10 < t) break;
        }
      }
      if (0 < t) return e / t / 1e6;
    }
    return navigator.connection && (t = navigator.connection.downlink, typeof t == "number") ? t : 5;
  }
  var Cf = null, Rf = null;
  function _i(t) {
    return t.nodeType === 9 ? t : t.ownerDocument;
  }
  function Rd(t) {
    switch (t) {
      case "http://www.w3.org/2000/svg":
        return 1;
      case "http://www.w3.org/1998/Math/MathML":
        return 2;
      default:
        return 0;
    }
  }
  function Hd(t, e) {
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
  function Hf(t, e) {
    return t === "textarea" || t === "noscript" || typeof e.children == "string" || typeof e.children == "number" || typeof e.children == "bigint" || typeof e.dangerouslySetInnerHTML == "object" && e.dangerouslySetInnerHTML !== null && e.dangerouslySetInnerHTML.__html != null;
  }
  var Bf = null;
  function vh() {
    var t = window.event;
    return t && t.type === "popstate" ? t === Bf ? !1 : (Bf = t, !0) : (Bf = null, !1);
  }
  var Bd = typeof setTimeout == "function" ? setTimeout : void 0, gh = typeof clearTimeout == "function" ? clearTimeout : void 0, Ld = typeof Promise == "function" ? Promise : void 0, ph = typeof queueMicrotask == "function" ? queueMicrotask : typeof Ld < "u" ? function(t) {
    return Ld.resolve(null).then(t).catch(bh);
  } : Bd;
  function bh(t) {
    setTimeout(function() {
      throw t;
    });
  }
  function Ql(t) {
    return t === "head";
  }
  function Yd(t, e) {
    var l = e, a = 0;
    do {
      var n = l.nextSibling;
      if (t.removeChild(l), n && n.nodeType === 8)
        if (l = n.data, l === "/$" || l === "/&") {
          if (a === 0) {
            t.removeChild(n), on(e);
            return;
          }
          a--;
        } else if (l === "$" || l === "$?" || l === "$~" || l === "$!" || l === "&")
          a++;
        else if (l === "html")
          nu(t.ownerDocument.documentElement);
        else if (l === "head") {
          l = t.ownerDocument.head, nu(l);
          for (var u = l.firstChild; u; ) {
            var i = u.nextSibling, o = u.nodeName;
            u[En] || o === "SCRIPT" || o === "STYLE" || o === "LINK" && u.rel.toLowerCase() === "stylesheet" || l.removeChild(u), u = i;
          }
        } else
          l === "body" && nu(t.ownerDocument.body);
      l = n;
    } while (l);
    on(e);
  }
  function Gd(t, e) {
    var l = t;
    t = 0;
    do {
      var a = l.nextSibling;
      if (l.nodeType === 1 ? e ? (l._stashedDisplay = l.style.display, l.style.display = "none") : (l.style.display = l._stashedDisplay || "", l.getAttribute("style") === "" && l.removeAttribute("style")) : l.nodeType === 3 && (e ? (l._stashedText = l.nodeValue, l.nodeValue = "") : l.nodeValue = l._stashedText || ""), a && a.nodeType === 8)
        if (l = a.data, l === "/$") {
          if (t === 0) break;
          t--;
        } else
          l !== "$" && l !== "$?" && l !== "$~" && l !== "$!" || t++;
      l = a;
    } while (l);
  }
  function Lf(t) {
    var e = t.firstChild;
    for (e && e.nodeType === 10 && (e = e.nextSibling); e; ) {
      var l = e;
      switch (e = e.nextSibling, l.nodeName) {
        case "HTML":
        case "HEAD":
        case "BODY":
          Lf(l), Xi(l);
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
  function Sh(t, e, l, a) {
    for (; t.nodeType === 1; ) {
      var n = l;
      if (t.nodeName.toLowerCase() !== e.toLowerCase()) {
        if (!a && (t.nodeName !== "INPUT" || t.type !== "hidden"))
          break;
      } else if (a) {
        if (!t[En])
          switch (e) {
            case "meta":
              if (!t.hasAttribute("itemprop")) break;
              return t;
            case "link":
              if (u = t.getAttribute("rel"), u === "stylesheet" && t.hasAttribute("data-precedence"))
                break;
              if (u !== n.rel || t.getAttribute("href") !== (n.href == null || n.href === "" ? null : n.href) || t.getAttribute("crossorigin") !== (n.crossOrigin == null ? null : n.crossOrigin) || t.getAttribute("title") !== (n.title == null ? null : n.title))
                break;
              return t;
            case "style":
              if (t.hasAttribute("data-precedence")) break;
              return t;
            case "script":
              if (u = t.getAttribute("src"), (u !== (n.src == null ? null : n.src) || t.getAttribute("type") !== (n.type == null ? null : n.type) || t.getAttribute("crossorigin") !== (n.crossOrigin == null ? null : n.crossOrigin)) && u && t.hasAttribute("async") && !t.hasAttribute("itemprop"))
                break;
              return t;
            default:
              return t;
          }
      } else if (e === "input" && t.type === "hidden") {
        var u = n.name == null ? null : "" + n.name;
        if (n.type === "hidden" && t.getAttribute("name") === u)
          return t;
      } else return t;
      if (t = Re(t.nextSibling), t === null) break;
    }
    return null;
  }
  function _h(t, e, l) {
    if (e === "") return null;
    for (; t.nodeType !== 3; )
      if ((t.nodeType !== 1 || t.nodeName !== "INPUT" || t.type !== "hidden") && !l || (t = Re(t.nextSibling), t === null)) return null;
    return t;
  }
  function Qd(t, e) {
    for (; t.nodeType !== 8; )
      if ((t.nodeType !== 1 || t.nodeName !== "INPUT" || t.type !== "hidden") && !e || (t = Re(t.nextSibling), t === null)) return null;
    return t;
  }
  function Yf(t) {
    return t.data === "$?" || t.data === "$~";
  }
  function Gf(t) {
    return t.data === "$!" || t.data === "$?" && t.ownerDocument.readyState !== "loading";
  }
  function Eh(t, e) {
    var l = t.ownerDocument;
    if (t.data === "$~") t._reactRetry = e;
    else if (t.data !== "$?" || l.readyState !== "loading")
      e();
    else {
      var a = function() {
        e(), l.removeEventListener("DOMContentLoaded", a);
      };
      l.addEventListener("DOMContentLoaded", a), t._reactRetry = a;
    }
  }
  function Re(t) {
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
  var Qf = null;
  function Xd(t) {
    t = t.nextSibling;
    for (var e = 0; t; ) {
      if (t.nodeType === 8) {
        var l = t.data;
        if (l === "/$" || l === "/&") {
          if (e === 0)
            return Re(t.nextSibling);
          e--;
        } else
          l !== "$" && l !== "$!" && l !== "$?" && l !== "$~" && l !== "&" || e++;
      }
      t = t.nextSibling;
    }
    return null;
  }
  function Zd(t) {
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
  function Vd(t, e, l) {
    switch (e = _i(l), t) {
      case "html":
        if (t = e.documentElement, !t) throw Error(s(452));
        return t;
      case "head":
        if (t = e.head, !t) throw Error(s(453));
        return t;
      case "body":
        if (t = e.body, !t) throw Error(s(454));
        return t;
      default:
        throw Error(s(451));
    }
  }
  function nu(t) {
    for (var e = t.attributes; e.length; )
      t.removeAttributeNode(e[0]);
    Xi(t);
  }
  var He = /* @__PURE__ */ new Map(), wd = /* @__PURE__ */ new Set();
  function Ei(t) {
    return typeof t.getRootNode == "function" ? t.getRootNode() : t.nodeType === 9 ? t : t.ownerDocument;
  }
  var vl = R.d;
  R.d = {
    f: Ah,
    r: xh,
    D: zh,
    C: Th,
    L: qh,
    m: Nh,
    X: Mh,
    S: Oh,
    M: Dh
  };
  function Ah() {
    var t = vl.f(), e = yi();
    return t || e;
  }
  function xh(t) {
    var e = Oa(t);
    e !== null && e.tag === 5 && e.type === "form" ? co(e) : vl.r(t);
  }
  var fn = typeof document > "u" ? null : document;
  function Jd(t, e, l) {
    var a = fn;
    if (a && typeof e == "string" && e) {
      var n = Ne(e);
      n = 'link[rel="' + t + '"][href="' + n + '"]', typeof l == "string" && (n += '[crossorigin="' + l + '"]'), wd.has(n) || (wd.add(n), t = { rel: t, crossOrigin: l, href: e }, a.querySelector(n) === null && (e = a.createElement("link"), ae(e, "link", t), Wt(e), a.head.appendChild(e)));
    }
  }
  function zh(t) {
    vl.D(t), Jd("dns-prefetch", t, null);
  }
  function Th(t, e) {
    vl.C(t, e), Jd("preconnect", t, e);
  }
  function qh(t, e, l) {
    vl.L(t, e, l);
    var a = fn;
    if (a && t && e) {
      var n = 'link[rel="preload"][as="' + Ne(e) + '"]';
      e === "image" && l && l.imageSrcSet ? (n += '[imagesrcset="' + Ne(
        l.imageSrcSet
      ) + '"]', typeof l.imageSizes == "string" && (n += '[imagesizes="' + Ne(
        l.imageSizes
      ) + '"]')) : n += '[href="' + Ne(t) + '"]';
      var u = n;
      switch (e) {
        case "style":
          u = sn(t);
          break;
        case "script":
          u = rn(t);
      }
      He.has(u) || (t = b(
        {
          rel: "preload",
          href: e === "image" && l && l.imageSrcSet ? void 0 : t,
          as: e
        },
        l
      ), He.set(u, t), a.querySelector(n) !== null || e === "style" && a.querySelector(uu(u)) || e === "script" && a.querySelector(iu(u)) || (e = a.createElement("link"), ae(e, "link", t), Wt(e), a.head.appendChild(e)));
    }
  }
  function Nh(t, e) {
    vl.m(t, e);
    var l = fn;
    if (l && t) {
      var a = e && typeof e.as == "string" ? e.as : "script", n = 'link[rel="modulepreload"][as="' + Ne(a) + '"][href="' + Ne(t) + '"]', u = n;
      switch (a) {
        case "audioworklet":
        case "paintworklet":
        case "serviceworker":
        case "sharedworker":
        case "worker":
        case "script":
          u = rn(t);
      }
      if (!He.has(u) && (t = b({ rel: "modulepreload", href: t }, e), He.set(u, t), l.querySelector(n) === null)) {
        switch (a) {
          case "audioworklet":
          case "paintworklet":
          case "serviceworker":
          case "sharedworker":
          case "worker":
          case "script":
            if (l.querySelector(iu(u)))
              return;
        }
        a = l.createElement("link"), ae(a, "link", t), Wt(a), l.head.appendChild(a);
      }
    }
  }
  function Oh(t, e, l) {
    vl.S(t, e, l);
    var a = fn;
    if (a && t) {
      var n = Ma(a).hoistableStyles, u = sn(t);
      e = e || "default";
      var i = n.get(u);
      if (!i) {
        var o = { loading: 0, preload: null };
        if (i = a.querySelector(
          uu(u)
        ))
          o.loading = 5;
        else {
          t = b(
            { rel: "stylesheet", href: t, "data-precedence": e },
            l
          ), (l = He.get(u)) && Xf(t, l);
          var d = i = a.createElement("link");
          Wt(d), ae(d, "link", t), d._p = new Promise(function(_, T) {
            d.onload = _, d.onerror = T;
          }), d.addEventListener("load", function() {
            o.loading |= 1;
          }), d.addEventListener("error", function() {
            o.loading |= 2;
          }), o.loading |= 4, Ai(i, e, a);
        }
        i = {
          type: "stylesheet",
          instance: i,
          count: 1,
          state: o
        }, n.set(u, i);
      }
    }
  }
  function Mh(t, e) {
    vl.X(t, e);
    var l = fn;
    if (l && t) {
      var a = Ma(l).hoistableScripts, n = rn(t), u = a.get(n);
      u || (u = l.querySelector(iu(n)), u || (t = b({ src: t, async: !0 }, e), (e = He.get(n)) && Zf(t, e), u = l.createElement("script"), Wt(u), ae(u, "link", t), l.head.appendChild(u)), u = {
        type: "script",
        instance: u,
        count: 1,
        state: null
      }, a.set(n, u));
    }
  }
  function Dh(t, e) {
    vl.M(t, e);
    var l = fn;
    if (l && t) {
      var a = Ma(l).hoistableScripts, n = rn(t), u = a.get(n);
      u || (u = l.querySelector(iu(n)), u || (t = b({ src: t, async: !0, type: "module" }, e), (e = He.get(n)) && Zf(t, e), u = l.createElement("script"), Wt(u), ae(u, "link", t), l.head.appendChild(u)), u = {
        type: "script",
        instance: u,
        count: 1,
        state: null
      }, a.set(n, u));
    }
  }
  function Kd(t, e, l, a) {
    var n = (n = nt.current) ? Ei(n) : null;
    if (!n) throw Error(s(446));
    switch (t) {
      case "meta":
      case "title":
        return null;
      case "style":
        return typeof l.precedence == "string" && typeof l.href == "string" ? (e = sn(l.href), l = Ma(
          n
        ).hoistableStyles, a = l.get(e), a || (a = {
          type: "style",
          instance: null,
          count: 0,
          state: null
        }, l.set(e, a)), a) : { type: "void", instance: null, count: 0, state: null };
      case "link":
        if (l.rel === "stylesheet" && typeof l.href == "string" && typeof l.precedence == "string") {
          t = sn(l.href);
          var u = Ma(
            n
          ).hoistableStyles, i = u.get(t);
          if (i || (n = n.ownerDocument || n, i = {
            type: "stylesheet",
            instance: null,
            count: 0,
            state: { loading: 0, preload: null }
          }, u.set(t, i), (u = n.querySelector(
            uu(t)
          )) && !u._p && (i.instance = u, i.state.loading = 5), He.has(t) || (l = {
            rel: "preload",
            as: "style",
            href: l.href,
            crossOrigin: l.crossOrigin,
            integrity: l.integrity,
            media: l.media,
            hrefLang: l.hrefLang,
            referrerPolicy: l.referrerPolicy
          }, He.set(t, l), u || Uh(
            n,
            t,
            l,
            i.state
          ))), e && a === null)
            throw Error(s(528, ""));
          return i;
        }
        if (e && a !== null)
          throw Error(s(529, ""));
        return null;
      case "script":
        return e = l.async, l = l.src, typeof l == "string" && e && typeof e != "function" && typeof e != "symbol" ? (e = rn(l), l = Ma(
          n
        ).hoistableScripts, a = l.get(e), a || (a = {
          type: "script",
          instance: null,
          count: 0,
          state: null
        }, l.set(e, a)), a) : { type: "void", instance: null, count: 0, state: null };
      default:
        throw Error(s(444, t));
    }
  }
  function sn(t) {
    return 'href="' + Ne(t) + '"';
  }
  function uu(t) {
    return 'link[rel="stylesheet"][' + t + "]";
  }
  function kd(t) {
    return b({}, t, {
      "data-precedence": t.precedence,
      precedence: null
    });
  }
  function Uh(t, e, l, a) {
    t.querySelector('link[rel="preload"][as="style"][' + e + "]") ? a.loading = 1 : (e = t.createElement("link"), a.preload = e, e.addEventListener("load", function() {
      return a.loading |= 1;
    }), e.addEventListener("error", function() {
      return a.loading |= 2;
    }), ae(e, "link", l), Wt(e), t.head.appendChild(e));
  }
  function rn(t) {
    return '[src="' + Ne(t) + '"]';
  }
  function iu(t) {
    return "script[async]" + t;
  }
  function $d(t, e, l) {
    if (e.count++, e.instance === null)
      switch (e.type) {
        case "style":
          var a = t.querySelector(
            'style[data-href~="' + Ne(l.href) + '"]'
          );
          if (a)
            return e.instance = a, Wt(a), a;
          var n = b({}, l, {
            "data-href": l.href,
            "data-precedence": l.precedence,
            href: null,
            precedence: null
          });
          return a = (t.ownerDocument || t).createElement(
            "style"
          ), Wt(a), ae(a, "style", n), Ai(a, l.precedence, t), e.instance = a;
        case "stylesheet":
          n = sn(l.href);
          var u = t.querySelector(
            uu(n)
          );
          if (u)
            return e.state.loading |= 4, e.instance = u, Wt(u), u;
          a = kd(l), (n = He.get(n)) && Xf(a, n), u = (t.ownerDocument || t).createElement("link"), Wt(u);
          var i = u;
          return i._p = new Promise(function(o, d) {
            i.onload = o, i.onerror = d;
          }), ae(u, "link", a), e.state.loading |= 4, Ai(u, l.precedence, t), e.instance = u;
        case "script":
          return u = rn(l.src), (n = t.querySelector(
            iu(u)
          )) ? (e.instance = n, Wt(n), n) : (a = l, (n = He.get(u)) && (a = b({}, l), Zf(a, n)), t = t.ownerDocument || t, n = t.createElement("script"), Wt(n), ae(n, "link", a), t.head.appendChild(n), e.instance = n);
        case "void":
          return null;
        default:
          throw Error(s(443, e.type));
      }
    else
      e.type === "stylesheet" && (e.state.loading & 4) === 0 && (a = e.instance, e.state.loading |= 4, Ai(a, l.precedence, t));
    return e.instance;
  }
  function Ai(t, e, l) {
    for (var a = l.querySelectorAll(
      'link[rel="stylesheet"][data-precedence],style[data-precedence]'
    ), n = a.length ? a[a.length - 1] : null, u = n, i = 0; i < a.length; i++) {
      var o = a[i];
      if (o.dataset.precedence === e) u = o;
      else if (u !== n) break;
    }
    u ? u.parentNode.insertBefore(t, u.nextSibling) : (e = l.nodeType === 9 ? l.head : l, e.insertBefore(t, e.firstChild));
  }
  function Xf(t, e) {
    t.crossOrigin == null && (t.crossOrigin = e.crossOrigin), t.referrerPolicy == null && (t.referrerPolicy = e.referrerPolicy), t.title == null && (t.title = e.title);
  }
  function Zf(t, e) {
    t.crossOrigin == null && (t.crossOrigin = e.crossOrigin), t.referrerPolicy == null && (t.referrerPolicy = e.referrerPolicy), t.integrity == null && (t.integrity = e.integrity);
  }
  var xi = null;
  function Wd(t, e, l) {
    if (xi === null) {
      var a = /* @__PURE__ */ new Map(), n = xi = /* @__PURE__ */ new Map();
      n.set(l, a);
    } else
      n = xi, a = n.get(l), a || (a = /* @__PURE__ */ new Map(), n.set(l, a));
    if (a.has(t)) return a;
    for (a.set(t, null), l = l.getElementsByTagName(t), n = 0; n < l.length; n++) {
      var u = l[n];
      if (!(u[En] || u[Pt] || t === "link" && u.getAttribute("rel") === "stylesheet") && u.namespaceURI !== "http://www.w3.org/2000/svg") {
        var i = u.getAttribute(e) || "";
        i = t + i;
        var o = a.get(i);
        o ? o.push(u) : a.set(i, [u]);
      }
    }
    return a;
  }
  function Fd(t, e, l) {
    t = t.ownerDocument || t, t.head.insertBefore(
      l,
      e === "title" ? t.querySelector("head > title") : null
    );
  }
  function jh(t, e, l) {
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
  function Id(t) {
    return !(t.type === "stylesheet" && (t.state.loading & 3) === 0);
  }
  function Ch(t, e, l, a) {
    if (l.type === "stylesheet" && (typeof a.media != "string" || matchMedia(a.media).matches !== !1) && (l.state.loading & 4) === 0) {
      if (l.instance === null) {
        var n = sn(a.href), u = e.querySelector(
          uu(n)
        );
        if (u) {
          e = u._p, e !== null && typeof e == "object" && typeof e.then == "function" && (t.count++, t = zi.bind(t), e.then(t, t)), l.state.loading |= 4, l.instance = u, Wt(u);
          return;
        }
        u = e.ownerDocument || e, a = kd(a), (n = He.get(n)) && Xf(a, n), u = u.createElement("link"), Wt(u);
        var i = u;
        i._p = new Promise(function(o, d) {
          i.onload = o, i.onerror = d;
        }), ae(u, "link", a), l.instance = u;
      }
      t.stylesheets === null && (t.stylesheets = /* @__PURE__ */ new Map()), t.stylesheets.set(l, e), (e = l.state.preload) && (l.state.loading & 3) === 0 && (t.count++, l = zi.bind(t), e.addEventListener("load", l), e.addEventListener("error", l));
    }
  }
  var Vf = 0;
  function Rh(t, e) {
    return t.stylesheets && t.count === 0 && qi(t, t.stylesheets), 0 < t.count || 0 < t.imgCount ? function(l) {
      var a = setTimeout(function() {
        if (t.stylesheets && qi(t, t.stylesheets), t.unsuspend) {
          var u = t.unsuspend;
          t.unsuspend = null, u();
        }
      }, 6e4 + e);
      0 < t.imgBytes && Vf === 0 && (Vf = 62500 * hh());
      var n = setTimeout(
        function() {
          if (t.waitingForImages = !1, t.count === 0 && (t.stylesheets && qi(t, t.stylesheets), t.unsuspend)) {
            var u = t.unsuspend;
            t.unsuspend = null, u();
          }
        },
        (t.imgBytes > Vf ? 50 : 800) + e
      );
      return t.unsuspend = l, function() {
        t.unsuspend = null, clearTimeout(a), clearTimeout(n);
      };
    } : null;
  }
  function zi() {
    if (this.count--, this.count === 0 && (this.imgCount === 0 || !this.waitingForImages)) {
      if (this.stylesheets) qi(this, this.stylesheets);
      else if (this.unsuspend) {
        var t = this.unsuspend;
        this.unsuspend = null, t();
      }
    }
  }
  var Ti = null;
  function qi(t, e) {
    t.stylesheets = null, t.unsuspend !== null && (t.count++, Ti = /* @__PURE__ */ new Map(), e.forEach(Hh, t), Ti = null, zi.call(t));
  }
  function Hh(t, e) {
    if (!(e.state.loading & 4)) {
      var l = Ti.get(t);
      if (l) var a = l.get(null);
      else {
        l = /* @__PURE__ */ new Map(), Ti.set(t, l);
        for (var n = t.querySelectorAll(
          "link[data-precedence],style[data-precedence]"
        ), u = 0; u < n.length; u++) {
          var i = n[u];
          (i.nodeName === "LINK" || i.getAttribute("media") !== "not all") && (l.set(i.dataset.precedence, i), a = i);
        }
        a && l.set(null, a);
      }
      n = e.instance, i = n.getAttribute("data-precedence"), u = l.get(i) || a, u === a && l.set(null, n), l.set(i, n), this.count++, a = zi.bind(this), n.addEventListener("load", a), n.addEventListener("error", a), u ? u.parentNode.insertBefore(n, u.nextSibling) : (t = t.nodeType === 9 ? t.head : t, t.insertBefore(n, t.firstChild)), e.state.loading |= 4;
    }
  }
  var cu = {
    $$typeof: tt,
    Provider: null,
    Consumer: null,
    _currentValue: $,
    _currentValue2: $,
    _threadCount: 0
  };
  function Bh(t, e, l, a, n, u, i, o, d) {
    this.tag = 1, this.containerInfo = t, this.pingCache = this.current = this.pendingChildren = null, this.timeoutHandle = -1, this.callbackNode = this.next = this.pendingContext = this.context = this.cancelPendingCommit = null, this.callbackPriority = 0, this.expirationTimes = Li(-1), this.entangledLanes = this.shellSuspendCounter = this.errorRecoveryDisabledLanes = this.expiredLanes = this.warmLanes = this.pingedLanes = this.suspendedLanes = this.pendingLanes = 0, this.entanglements = Li(0), this.hiddenUpdates = Li(null), this.identifierPrefix = a, this.onUncaughtError = n, this.onCaughtError = u, this.onRecoverableError = i, this.pooledCache = null, this.pooledCacheLanes = 0, this.formState = d, this.incompleteTransitions = /* @__PURE__ */ new Map();
  }
  function Pd(t, e, l, a, n, u, i, o, d, _, T, M) {
    return t = new Bh(
      t,
      e,
      l,
      i,
      d,
      _,
      T,
      M,
      o
    ), e = 1, u === !0 && (e |= 24), u = Se(3, null, null, e), t.current = u, u.stateNode = t, e = Ec(), e.refCount++, t.pooledCache = e, e.refCount++, u.memoizedState = {
      element: a,
      isDehydrated: l,
      cache: e
    }, Tc(u), t;
  }
  function ty(t) {
    return t ? (t = Ga, t) : Ga;
  }
  function ey(t, e, l, a, n, u) {
    n = ty(n), a.context === null ? a.context = n : a.pendingContext = n, a = Ml(e), a.payload = { element: l }, u = u === void 0 ? null : u, u !== null && (a.callback = u), l = Dl(t, a, e), l !== null && (ve(l, t, e), Yn(l, t, e));
  }
  function ly(t, e) {
    if (t = t.memoizedState, t !== null && t.dehydrated !== null) {
      var l = t.retryLane;
      t.retryLane = l !== 0 && l < e ? l : e;
    }
  }
  function wf(t, e) {
    ly(t, e), (t = t.alternate) && ly(t, e);
  }
  function ay(t) {
    if (t.tag === 13 || t.tag === 31) {
      var e = ca(t, 67108864);
      e !== null && ve(e, t, 67108864), wf(t, 67108864);
    }
  }
  function ny(t) {
    if (t.tag === 13 || t.tag === 31) {
      var e = ze();
      e = Yi(e);
      var l = ca(t, e);
      l !== null && ve(l, t, e), wf(t, e);
    }
  }
  var Ni = !0;
  function Lh(t, e, l, a) {
    var n = z.T;
    z.T = null;
    var u = R.p;
    try {
      R.p = 2, Jf(t, e, l, a);
    } finally {
      R.p = u, z.T = n;
    }
  }
  function Yh(t, e, l, a) {
    var n = z.T;
    z.T = null;
    var u = R.p;
    try {
      R.p = 8, Jf(t, e, l, a);
    } finally {
      R.p = u, z.T = n;
    }
  }
  function Jf(t, e, l, a) {
    if (Ni) {
      var n = Kf(a);
      if (n === null)
        Uf(
          t,
          e,
          a,
          Oi,
          l
        ), iy(t, a);
      else if (Qh(
        n,
        t,
        e,
        l,
        a
      ))
        a.stopPropagation();
      else if (iy(t, a), e & 4 && -1 < Gh.indexOf(t)) {
        for (; n !== null; ) {
          var u = Oa(n);
          if (u !== null)
            switch (u.tag) {
              case 3:
                if (u = u.stateNode, u.current.memoizedState.isDehydrated) {
                  var i = la(u.pendingLanes);
                  if (i !== 0) {
                    var o = u;
                    for (o.pendingLanes |= 2, o.entangledLanes |= 2; i; ) {
                      var d = 1 << 31 - et(i);
                      o.entanglements[1] |= d, i &= ~d;
                    }
                    We(u), (pt & 6) === 0 && (oi = It() + 500, eu(0));
                  }
                }
                break;
              case 31:
              case 13:
                o = ca(u, 2), o !== null && ve(o, u, 2), yi(), wf(u, 2);
            }
          if (u = Kf(a), u === null && Uf(
            t,
            e,
            a,
            Oi,
            l
          ), u === n) break;
          n = u;
        }
        n !== null && a.stopPropagation();
      } else
        Uf(
          t,
          e,
          a,
          null,
          l
        );
    }
  }
  function Kf(t) {
    return t = ki(t), kf(t);
  }
  var Oi = null;
  function kf(t) {
    if (Oi = null, t = Na(t), t !== null) {
      var e = g(t);
      if (e === null) t = null;
      else {
        var l = e.tag;
        if (l === 13) {
          if (t = q(e), t !== null) return t;
          t = null;
        } else if (l === 31) {
          if (t = U(e), t !== null) return t;
          t = null;
        } else if (l === 3) {
          if (e.stateNode.current.memoizedState.isDehydrated)
            return e.tag === 3 ? e.stateNode.containerInfo : null;
          t = null;
        } else e !== t && (t = null);
      }
    }
    return Oi = t, null;
  }
  function uy(t) {
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
        switch (vu()) {
          case gu:
            return 2;
          case bn:
            return 8;
          case El:
          case ta:
            return 32;
          case pu:
            return 268435456;
          default:
            return 32;
        }
      default:
        return 32;
    }
  }
  var $f = !1, Xl = null, Zl = null, Vl = null, fu = /* @__PURE__ */ new Map(), su = /* @__PURE__ */ new Map(), wl = [], Gh = "mousedown mouseup touchcancel touchend touchstart auxclick dblclick pointercancel pointerdown pointerup dragend dragstart drop compositionend compositionstart keydown keypress keyup input textInput copy cut paste click change contextmenu reset".split(
    " "
  );
  function iy(t, e) {
    switch (t) {
      case "focusin":
      case "focusout":
        Xl = null;
        break;
      case "dragenter":
      case "dragleave":
        Zl = null;
        break;
      case "mouseover":
      case "mouseout":
        Vl = null;
        break;
      case "pointerover":
      case "pointerout":
        fu.delete(e.pointerId);
        break;
      case "gotpointercapture":
      case "lostpointercapture":
        su.delete(e.pointerId);
    }
  }
  function ru(t, e, l, a, n, u) {
    return t === null || t.nativeEvent !== u ? (t = {
      blockedOn: e,
      domEventName: l,
      eventSystemFlags: a,
      nativeEvent: u,
      targetContainers: [n]
    }, e !== null && (e = Oa(e), e !== null && ay(e)), t) : (t.eventSystemFlags |= a, e = t.targetContainers, n !== null && e.indexOf(n) === -1 && e.push(n), t);
  }
  function Qh(t, e, l, a, n) {
    switch (e) {
      case "focusin":
        return Xl = ru(
          Xl,
          t,
          e,
          l,
          a,
          n
        ), !0;
      case "dragenter":
        return Zl = ru(
          Zl,
          t,
          e,
          l,
          a,
          n
        ), !0;
      case "mouseover":
        return Vl = ru(
          Vl,
          t,
          e,
          l,
          a,
          n
        ), !0;
      case "pointerover":
        var u = n.pointerId;
        return fu.set(
          u,
          ru(
            fu.get(u) || null,
            t,
            e,
            l,
            a,
            n
          )
        ), !0;
      case "gotpointercapture":
        return u = n.pointerId, su.set(
          u,
          ru(
            su.get(u) || null,
            t,
            e,
            l,
            a,
            n
          )
        ), !0;
    }
    return !1;
  }
  function cy(t) {
    var e = Na(t.target);
    if (e !== null) {
      var l = g(e);
      if (l !== null) {
        if (e = l.tag, e === 13) {
          if (e = q(l), e !== null) {
            t.blockedOn = e, Ss(t.priority, function() {
              ny(l);
            });
            return;
          }
        } else if (e === 31) {
          if (e = U(l), e !== null) {
            t.blockedOn = e, Ss(t.priority, function() {
              ny(l);
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
  function Mi(t) {
    if (t.blockedOn !== null) return !1;
    for (var e = t.targetContainers; 0 < e.length; ) {
      var l = Kf(t.nativeEvent);
      if (l === null) {
        l = t.nativeEvent;
        var a = new l.constructor(
          l.type,
          l
        );
        Ki = a, l.target.dispatchEvent(a), Ki = null;
      } else
        return e = Oa(l), e !== null && ay(e), t.blockedOn = l, !1;
      e.shift();
    }
    return !0;
  }
  function fy(t, e, l) {
    Mi(t) && l.delete(e);
  }
  function Xh() {
    $f = !1, Xl !== null && Mi(Xl) && (Xl = null), Zl !== null && Mi(Zl) && (Zl = null), Vl !== null && Mi(Vl) && (Vl = null), fu.forEach(fy), su.forEach(fy);
  }
  function Di(t, e) {
    t.blockedOn === e && (t.blockedOn = null, $f || ($f = !0, c.unstable_scheduleCallback(
      c.unstable_NormalPriority,
      Xh
    )));
  }
  var Ui = null;
  function sy(t) {
    Ui !== t && (Ui = t, c.unstable_scheduleCallback(
      c.unstable_NormalPriority,
      function() {
        Ui === t && (Ui = null);
        for (var e = 0; e < t.length; e += 3) {
          var l = t[e], a = t[e + 1], n = t[e + 2];
          if (typeof a != "function") {
            if (kf(a || l) === null)
              continue;
            break;
          }
          var u = Oa(l);
          u !== null && (t.splice(e, 3), e -= 3, Jc(
            u,
            {
              pending: !0,
              data: n,
              method: l.method,
              action: a
            },
            a,
            n
          ));
        }
      }
    ));
  }
  function on(t) {
    function e(d) {
      return Di(d, t);
    }
    Xl !== null && Di(Xl, t), Zl !== null && Di(Zl, t), Vl !== null && Di(Vl, t), fu.forEach(e), su.forEach(e);
    for (var l = 0; l < wl.length; l++) {
      var a = wl[l];
      a.blockedOn === t && (a.blockedOn = null);
    }
    for (; 0 < wl.length && (l = wl[0], l.blockedOn === null); )
      cy(l), l.blockedOn === null && wl.shift();
    if (l = (t.ownerDocument || t).$$reactFormReplay, l != null)
      for (a = 0; a < l.length; a += 3) {
        var n = l[a], u = l[a + 1], i = n[re] || null;
        if (typeof u == "function")
          i || sy(l);
        else if (i) {
          var o = null;
          if (u && u.hasAttribute("formAction")) {
            if (n = u, i = u[re] || null)
              o = i.formAction;
            else if (kf(n) !== null) continue;
          } else o = i.action;
          typeof o == "function" ? l[a + 1] = o : (l.splice(a, 3), a -= 3), sy(l);
        }
      }
  }
  function ry() {
    function t(u) {
      u.canIntercept && u.info === "react-transition" && u.intercept({
        handler: function() {
          return new Promise(function(i) {
            return n = i;
          });
        },
        focusReset: "manual",
        scroll: "manual"
      });
    }
    function e() {
      n !== null && (n(), n = null), a || setTimeout(l, 20);
    }
    function l() {
      if (!a && !navigation.transition) {
        var u = navigation.currentEntry;
        u && u.url != null && navigation.navigate(u.url, {
          state: u.getState(),
          info: "react-transition",
          history: "replace"
        });
      }
    }
    if (typeof navigation == "object") {
      var a = !1, n = null;
      return navigation.addEventListener("navigate", t), navigation.addEventListener("navigatesuccess", e), navigation.addEventListener("navigateerror", e), setTimeout(l, 100), function() {
        a = !0, navigation.removeEventListener("navigate", t), navigation.removeEventListener("navigatesuccess", e), navigation.removeEventListener("navigateerror", e), n !== null && (n(), n = null);
      };
    }
  }
  function Wf(t) {
    this._internalRoot = t;
  }
  ji.prototype.render = Wf.prototype.render = function(t) {
    var e = this._internalRoot;
    if (e === null) throw Error(s(409));
    var l = e.current, a = ze();
    ey(l, a, t, e, null, null);
  }, ji.prototype.unmount = Wf.prototype.unmount = function() {
    var t = this._internalRoot;
    if (t !== null) {
      this._internalRoot = null;
      var e = t.containerInfo;
      ey(t.current, 2, null, t, null, null), yi(), e[qa] = null;
    }
  };
  function ji(t) {
    this._internalRoot = t;
  }
  ji.prototype.unstable_scheduleHydration = function(t) {
    if (t) {
      var e = bs();
      t = { blockedOn: null, target: t, priority: e };
      for (var l = 0; l < wl.length && e !== 0 && e < wl[l].priority; l++) ;
      wl.splice(l, 0, t), l === 0 && cy(t);
    }
  };
  var oy = f.version;
  if (oy !== "19.2.5")
    throw Error(
      s(
        527,
        oy,
        "19.2.5"
      )
    );
  R.findDOMNode = function(t) {
    var e = t._reactInternals;
    if (e === void 0)
      throw typeof t.render == "function" ? Error(s(188)) : (t = Object.keys(t).join(","), Error(s(268, t)));
    return t = p(e), t = t !== null ? C(t) : null, t = t === null ? null : t.stateNode, t;
  };
  var Zh = {
    bundleType: 0,
    version: "19.2.5",
    rendererPackageName: "react-dom",
    currentDispatcherRef: z,
    reconcilerVersion: "19.2.5"
  };
  if (typeof __REACT_DEVTOOLS_GLOBAL_HOOK__ < "u") {
    var Ci = __REACT_DEVTOOLS_GLOBAL_HOOK__;
    if (!Ci.isDisabled && Ci.supportsFiber)
      try {
        Je = Ci.inject(
          Zh
        ), fe = Ci;
      } catch {
      }
  }
  return ou.createRoot = function(t, e) {
    if (!h(t)) throw Error(s(299));
    var l = !1, a = "", n = po, u = bo, i = So;
    return e != null && (e.unstable_strictMode === !0 && (l = !0), e.identifierPrefix !== void 0 && (a = e.identifierPrefix), e.onUncaughtError !== void 0 && (n = e.onUncaughtError), e.onCaughtError !== void 0 && (u = e.onCaughtError), e.onRecoverableError !== void 0 && (i = e.onRecoverableError)), e = Pd(
      t,
      1,
      !1,
      null,
      null,
      l,
      a,
      null,
      n,
      u,
      i,
      ry
    ), t[qa] = e.current, Df(t), new Wf(e);
  }, ou.hydrateRoot = function(t, e, l) {
    if (!h(t)) throw Error(s(299));
    var a = !1, n = "", u = po, i = bo, o = So, d = null;
    return l != null && (l.unstable_strictMode === !0 && (a = !0), l.identifierPrefix !== void 0 && (n = l.identifierPrefix), l.onUncaughtError !== void 0 && (u = l.onUncaughtError), l.onCaughtError !== void 0 && (i = l.onCaughtError), l.onRecoverableError !== void 0 && (o = l.onRecoverableError), l.formState !== void 0 && (d = l.formState)), e = Pd(
      t,
      1,
      !0,
      e,
      l ?? null,
      a,
      n,
      d,
      u,
      i,
      o,
      ry
    ), e.context = ty(null), l = e.current, a = ze(), a = Yi(a), n = Ml(a), n.callback = null, Dl(l, n, a), l = a, e.current.lanes = l, _n(e, l), We(e), t[qa] = e.current, Df(t), new ji(e);
  }, ou.version = "19.2.5", ou;
}
var Sy;
function Ph() {
  if (Sy) return Pf.exports;
  Sy = 1;
  function c() {
    if (!(typeof __REACT_DEVTOOLS_GLOBAL_HOOK__ > "u" || typeof __REACT_DEVTOOLS_GLOBAL_HOOK__.checkDCE != "function"))
      try {
        __REACT_DEVTOOLS_GLOBAL_HOOK__.checkDCE(c);
      } catch (f) {
        console.error(f);
      }
  }
  return c(), Pf.exports = Ih(), Pf.exports;
}
var tv = Ph(), as = { exports: {} }, du = {};
/**
 * @license React
 * react-jsx-runtime.production.js
 *
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 */
var _y;
function ev() {
  if (_y) return du;
  _y = 1;
  var c = Symbol.for("react.transitional.element"), f = Symbol.for("react.fragment");
  function r(s, h, g) {
    var q = null;
    if (g !== void 0 && (q = "" + g), h.key !== void 0 && (q = "" + h.key), "key" in h) {
      g = {};
      for (var U in h)
        U !== "key" && (g[U] = h[U]);
    } else g = h;
    return h = g.ref, {
      $$typeof: c,
      type: s,
      key: q,
      ref: h !== void 0 ? h : null,
      props: g
    };
  }
  return du.Fragment = f, du.jsx = r, du.jsxs = r, du;
}
var Ey;
function lv() {
  return Ey || (Ey = 1, as.exports = ev()), as.exports;
}
var x = lv();
function av(c) {
  return typeof c.questionId == "string";
}
function nv(c) {
  const f = c;
  return Array.isArray(f.all) || Array.isArray(f.any);
}
function uv(c) {
  return typeof c.expression == "string";
}
class Fe extends Error {
  constructor(r, s) {
    super(`Expression syntax error at column ${s}: ${r}`);
    gl(this, "position");
    this.position = s, this.name = "ExpressionSyntaxError";
  }
}
function Ri(c) {
  return c >= "0" && c <= "9";
}
function qy(c) {
  return c >= "a" && c <= "z" || c >= "A" && c <= "Z";
}
function iv(c) {
  return qy(c) || Ri(c);
}
function cv(c) {
  return c === " " || c === "	" || c === `
` || c === "\r" || c === "\f" || c === "\v";
}
function fv(c) {
  const f = [];
  let r = 0;
  const s = () => r >= c.length, h = (b = 0) => c.charAt(r + b), g = (b) => {
    if (r + b.length > c.length)
      return !1;
    for (let j = 0; j < b.length; j++)
      if (c.charAt(r + j) !== b.charAt(j))
        return !1;
    return r += b.length, !0;
  }, q = () => {
    for (; !s() && cv(h()); )
      r++;
  }, U = (b) => {
    for (; !s() && Ri(h()); )
      r++;
    if (!s() && h() === ".")
      for (r++; !s() && Ri(h()); )
        r++;
    const j = c.substring(b, r), Q = parseFloat(j);
    return { kind: "Number", text: j, literal: Q, position: b };
  }, N = (b, j) => {
    r++;
    let Q = "";
    for (; !s() && h() !== j; ) {
      const W = h();
      if (W === "\\" && r + 1 < c.length) {
        const w = h(1), J = {
          n: `
`,
          t: "	",
          r: "\r",
          "\\": "\\",
          "'": "'",
          '"': '"'
        }[w];
        if (J === void 0)
          throw new Fe(`unknown escape '\\${w}'.`, r);
        Q += J, r += 2;
      } else
        Q += W, r++;
    }
    if (s())
      throw new Fe("unterminated string literal.", b);
    return r++, { kind: "String", text: Q, literal: Q, position: b };
  }, p = (b) => {
    for (; !s(); ) {
      const Q = h();
      if (Q === "_" || Q === "-" || iv(Q))
        r++;
      else
        break;
    }
    const j = c.substring(b, r);
    return j === "true" ? { kind: "True", text: j, literal: !0, position: b } : j === "false" ? { kind: "False", text: j, literal: !1, position: b } : j === "null" ? { kind: "Null", text: j, literal: null, position: b } : { kind: "Identifier", text: j, literal: null, position: b };
  }, C = () => {
    const b = r, j = h();
    if (Ri(j))
      return U(b);
    if (j === "'" || j === '"')
      return N(b, j);
    if (j === "_" || qy(j))
      return p(b);
    switch (j) {
      case "(":
        return r++, { kind: "LParen", text: "(", literal: null, position: b };
      case ")":
        return r++, { kind: "RParen", text: ")", literal: null, position: b };
      case "[":
        return r++, { kind: "LBracket", text: "[", literal: null, position: b };
      case "]":
        return r++, { kind: "RBracket", text: "]", literal: null, position: b };
      case ",":
        return r++, { kind: "Comma", text: ",", literal: null, position: b };
      case ".":
        return r++, { kind: "Dot", text: ".", literal: null, position: b };
      case "=":
        if (g("==="))
          return { kind: "StrictEq", text: "===", literal: null, position: b };
        if (g("=="))
          return { kind: "Eq", text: "==", literal: null, position: b };
        throw new Fe("bare '=' is not a valid operator (use '==' or '===').", b);
      case "!":
        return g("!==") ? { kind: "StrictNotEq", text: "!==", literal: null, position: b } : g("!=") ? { kind: "NotEq", text: "!=", literal: null, position: b } : (r++, { kind: "Not", text: "!", literal: null, position: b });
      case "<":
        return g("<=") ? { kind: "LtEq", text: "<=", literal: null, position: b } : (r++, { kind: "Lt", text: "<", literal: null, position: b });
      case ">":
        return g(">=") ? { kind: "GtEq", text: ">=", literal: null, position: b } : (r++, { kind: "Gt", text: ">", literal: null, position: b });
      case "&":
        if (g("&&"))
          return { kind: "And", text: "&&", literal: null, position: b };
        throw new Fe("expected '&&'.", b);
      case "|":
        if (g("||"))
          return { kind: "Or", text: "||", literal: null, position: b };
        throw new Fe("expected '||'.", b);
    }
    throw new Fe(`unexpected character '${j}'.`, b);
  };
  for (; ; ) {
    if (q(), s())
      return f.push({ kind: "EndOfInput", text: "", literal: null, position: r }), f;
    f.push(C());
  }
}
function sv(c) {
  let f = 0;
  const r = () => {
    const B = c[f];
    if (!B)
      throw new Fe("unexpected end of tokens.", 0);
    return B;
  }, s = () => {
    const B = r();
    return B.kind !== "EndOfInput" && f++, B;
  }, h = (B) => r().kind !== B ? !1 : (s(), !0), g = (B) => {
    const J = r();
    if (J.kind !== B)
      throw new Fe(`expected ${B}, got '${J.text}'.`, J.position);
    return s(), J;
  }, q = () => {
    let B = U();
    for (; h("Or"); )
      B = { kind: "BinaryOp", op: "||", left: B, right: U() };
    return B;
  }, U = () => {
    let B = N();
    for (; h("And"); )
      B = { kind: "BinaryOp", op: "&&", left: B, right: N() };
    return B;
  }, N = () => {
    let B = p();
    for (; ; ) {
      const J = r().kind;
      let Et = null;
      if (J === "Eq" || J === "StrictEq" ? Et = "==" : (J === "NotEq" || J === "StrictNotEq") && (Et = "!="), Et === null)
        break;
      s(), B = { kind: "BinaryOp", op: Et, left: B, right: p() };
    }
    return B;
  }, p = () => {
    let B = C();
    for (; ; ) {
      const J = r().kind;
      let Et = null;
      if (J === "Lt" ? Et = "<" : J === "Gt" ? Et = ">" : J === "LtEq" ? Et = "<=" : J === "GtEq" && (Et = ">="), Et === null)
        break;
      s(), B = { kind: "BinaryOp", op: Et, left: B, right: C() };
    }
    return B;
  }, C = () => h("Not") ? { kind: "UnaryOp", op: "!", operand: C() } : W(), b = () => {
    g("LBracket");
    const B = [];
    if (r().kind !== "RBracket")
      for (B.push(q()); h("Comma"); )
        B.push(q());
    return g("RBracket"), { kind: "Array", items: B };
  }, j = (B) => {
    let J;
    if (h("Dot"))
      J = g("Identifier").text;
    else if (h("LBracket")) {
      const Et = g("String");
      g("RBracket"), J = Et.literal;
    } else
      throw new Fe("'answers' must be followed by .key or ['key'].", B);
    return { kind: "AnswersAccess", key: J };
  }, Q = () => {
    const B = s();
    if (B.text === "answers")
      return j(B.position);
    g("LParen");
    const J = [];
    if (r().kind !== "RParen")
      for (J.push(q()); h("Comma"); )
        J.push(q());
    return g("RParen"), { kind: "Call", name: B.text, args: J };
  }, W = () => {
    const B = r();
    switch (B.kind) {
      case "Number":
      case "String":
      case "True":
      case "False":
      case "Null":
        return s(), { kind: "Literal", value: B.literal };
      case "LParen": {
        s();
        const J = q();
        return g("RParen"), J;
      }
      case "LBracket":
        return b();
      case "Identifier":
        return Q();
      default:
        throw new Fe(`unexpected token '${B.text}'.`, B.position);
    }
  }, w = q();
  return g("EndOfInput"), w;
}
function bl(c) {
  return c === void 0 || c === null ? null : typeof c == "boolean" || typeof c == "number" || typeof c == "string" ? c : Array.isArray(c) ? c.map(bl) : null;
}
function Ea(c, f) {
  const r = bl(c), s = bl(f);
  if (r === null || s === null)
    return r === null && s === null;
  if (typeof r == "number" && typeof s == "number" || typeof r == "string" && typeof s == "string" || typeof r == "boolean" && typeof s == "boolean")
    return r === s;
  if (Array.isArray(r) && Array.isArray(s)) {
    if (r.length !== s.length)
      return !1;
    for (let h = 0; h < r.length; h++)
      if (!Ea(r[h], s[h]))
        return !1;
    return !0;
  }
  return !1;
}
function Wl(c, f) {
  const r = bl(c), s = bl(f);
  if (typeof r == "number" && typeof s == "number" || typeof r == "string" && typeof s == "string")
    return r < s ? -1 : r > s ? 1 : 0;
  throw new Error("Comparison operators require two numbers or two strings.");
}
function yn(c) {
  const f = bl(c);
  return f === null ? !1 : typeof f == "boolean" ? f : typeof f == "number" ? f !== 0 : typeof f == "string" || Array.isArray(f) ? f.length > 0 : !0;
}
function Be(c, f) {
  switch (c.kind) {
    case "Literal":
      return c.value;
    case "AnswersAccess":
      return mv(c.key, f);
    case "UnaryOp":
      return rv(c, f);
    case "BinaryOp":
      return ov(c, f);
    case "Call":
      return dv(c, f);
    case "Array":
      return c.items.map((r) => Be(r, f));
  }
}
function rv(c, f) {
  const r = Be(c.operand, f);
  if (c.op === "!")
    return !yn(r);
  throw new Error(`Unknown unary operator '${c.op}'.`);
}
function ov(c, f) {
  if (c.op === "&&") {
    const h = Be(c.left, f);
    return yn(h) ? yn(Be(c.right, f)) : !1;
  }
  if (c.op === "||") {
    const h = Be(c.left, f);
    return yn(h) ? !0 : yn(Be(c.right, f));
  }
  const r = Be(c.left, f), s = Be(c.right, f);
  switch (c.op) {
    case "==":
      return Ea(r, s);
    case "!=":
      return !Ea(r, s);
    case "<":
      return Wl(r, s) < 0;
    case ">":
      return Wl(r, s) > 0;
    case "<=":
      return Wl(r, s) <= 0;
    case ">=":
      return Wl(r, s) >= 0;
    default:
      throw new Error(`Unknown binary operator '${c.op}'.`);
  }
}
function dv(c, f) {
  switch (c.name) {
    case "has":
    case "isSet":
      return Ay(c, f);
    case "isNotSet":
      return !Ay(c, f);
    case "in":
      return yv(c, f);
    default:
      throw new Error(`Unknown function '${c.name}'.`);
  }
}
function Ay(c, f) {
  if (c.args.length !== 1)
    throw new Error(`${c.name}() takes one argument.`);
  const r = c.args[0];
  if (!r)
    return !1;
  const s = Be(r, f);
  return typeof s != "string" ? !1 : s in f && f[s] !== null && f[s] !== void 0;
}
function yv(c, f) {
  if (c.args.length !== 2)
    throw new Error("in() takes two arguments: in(value, [array]).");
  const r = c.args[0], s = c.args[1];
  if (!r || !s)
    return !1;
  const h = Be(r, f), g = Be(s, f);
  return Array.isArray(g) ? g.some((q) => Ea(h, q)) : !1;
}
function mv(c, f) {
  return c in f ? bl(f[c]) : null;
}
function hv(c) {
  const f = fv(c);
  return sv(f);
}
function vv(c, f) {
  try {
    const r = typeof c == "string" ? hv(c) : c;
    return yn(Be(r, f));
  } catch {
    return !1;
  }
}
function gv(c, f) {
  var r;
  if (!c.logic)
    return null;
  for (const s of c.logic)
    if (os(s.if, f))
      return ((r = s.then) == null ? void 0 : r.goto) ?? null;
  return null;
}
function os(c, f) {
  try {
    return av(c) ? bv(c, f) : nv(c) ? pv(c, f) : uv(c) ? vv(c.expression, f) : !1;
  } catch {
    return !1;
  }
}
function pv(c, f) {
  return c.all && c.all.length > 0 ? c.all.every((r) => os(r, f)) : c.any && c.any.length > 0 ? c.any.some((r) => os(r, f)) : !1;
}
function bv(c, f) {
  const r = c.questionId in f && f[c.questionId] !== null && f[c.questionId] !== void 0;
  if (c.op === "isSet")
    return r;
  if (c.op === "isNotSet")
    return !r;
  if (c.value === void 0)
    return !1;
  const s = r ? bl(f[c.questionId]) : null, h = bl(c.value);
  return Sv(c.op, s, h);
}
function Sv(c, f, r) {
  switch (c) {
    case "==":
      return Ea(f, r);
    case "!=":
      return !Ea(f, r);
    case ">":
      return Wl(f, r) > 0;
    case ">=":
      return Wl(f, r) >= 0;
    case "<":
      return Wl(f, r) < 0;
    case "<=":
      return Wl(f, r) <= 0;
    case "in":
      return xy(r, f);
    case "notIn":
      return !xy(r, f);
    default:
      return !1;
  }
}
function xy(c, f) {
  return Array.isArray(c) ? c.some((r) => Ea(f, r)) : !1;
}
function yu(c, f, r) {
  const s = new Set(c.screens.map((U) => U.id)), h = c.screens.find((U) => U.id === f);
  if (h && (!h.questions || h.questions.length === 0) && !h.nextScreen)
    return { kind: "end" };
  const g = gv(c, r);
  if (g && g !== f && s.has(g))
    return { kind: "screen", screenId: g };
  if (h != null && h.nextScreen && h.nextScreen !== f && s.has(h.nextScreen))
    return { kind: "screen", screenId: h.nextScreen };
  const q = c.screens.findIndex((U) => U.id === f);
  if (q >= 0 && q + 1 < c.screens.length) {
    const U = c.screens[q + 1];
    if (U)
      return { kind: "screen", screenId: U.id };
  }
  return { kind: "end" };
}
function _v(c, f, r, s) {
  const h = new Set(f.screens.map((g) => g.id));
  return c.nextScreen && h.has(c.nextScreen) ? { kind: "screen", screenId: c.nextScreen } : yu(f, r, s);
}
const ot = (c, f, r, s) => ({ questionId: c, code: f, message: r, ...s ? { params: s } : {} }), ge = (c) => typeof c == "number" && Number.isFinite(c);
function ns(c) {
  if (!/^\d{4}-\d{2}-\d{2}$/.test(c))
    return null;
  const [f, r, s] = c.split("-").map((g) => Number.parseInt(g, 10)), h = new Date(Date.UTC(f, r - 1, s));
  return h.getUTCFullYear() !== f || h.getUTCMonth() !== r - 1 || h.getUTCDate() !== s ? null : h.getTime();
}
function us(c) {
  const f = Date.parse(c);
  return Number.isNaN(f) ? null : f;
}
function Ny(c, f) {
  const r = c.id, s = [];
  switch (c.type) {
    case "text": {
      if (typeof f != "string") {
        s.push(ot(r, "type", "Text answer must be a JSON string."));
        break;
      }
      const h = c.minLength, g = c.maxLength, q = c.pattern;
      if (ge(h) && f.length < h && s.push(ot(r, "minLength", `Answer length ${f.length} is less than minLength ${h}.`, { n: h, actual: f.length })), ge(g) && f.length > g && s.push(ot(r, "maxLength", `Answer length ${f.length} exceeds maxLength ${g}.`, { n: g, actual: f.length })), typeof q == "string" && q.length > 0)
        try {
          new RegExp(q).test(f) || s.push(ot(r, "pattern", "Answer does not match the required pattern."));
        } catch {
        }
      break;
    }
    case "paragraph": {
      if (typeof f != "string") {
        s.push(ot(r, "type", "Paragraph answer must be a JSON string."));
        break;
      }
      const h = c.minLength, g = c.maxLength;
      ge(h) && f.length < h && s.push(ot(r, "minLength", `Answer length ${f.length} is less than minLength ${h}.`, { n: h, actual: f.length })), ge(g) && f.length > g && s.push(ot(r, "maxLength", `Answer length ${f.length} exceeds maxLength ${g}.`, { n: g, actual: f.length }));
      break;
    }
    case "number": {
      if (!ge(f)) {
        s.push(ot(r, "type", "Number answer must be a JSON number."));
        break;
      }
      const h = c.min, g = c.max;
      ge(h) && f < h && s.push(ot(r, "min", `Answer ${f} is less than min ${h}.`, { n: h })), ge(g) && f > g && s.push(ot(r, "max", `Answer ${f} exceeds max ${g}.`, { n: g }));
      break;
    }
    case "rating": {
      if (!ge(f)) {
        s.push(ot(r, "type", "Rating answer must be a JSON number."));
        break;
      }
      const h = ge(c.max) ? c.max : 0;
      (f < 0 || f > h) && s.push(ot(r, "range", `Rating ${f} is outside 0..${h}.`, { min: 0, max: h })), c.allowHalf !== !0 && f !== Math.floor(f) && s.push(ot(r, "halfNotAllowed", "Rating does not allow half values."));
      break;
    }
    case "nps": {
      if (!ge(f) || !Number.isInteger(f)) {
        s.push(ot(r, "type", "NPS answer must be a JSON number."));
        break;
      }
      const h = ge(c.min) ? c.min : 0, g = ge(c.max) ? c.max : 10;
      (f < h || f > g) && s.push(ot(r, "range", `NPS answer ${f} is outside ${h}..${g}.`, { min: h, max: g }));
      break;
    }
    case "singleChoice":
    case "dropdown":
    case "navigationList": {
      if (typeof f != "string") {
        s.push(ot(r, "type", "Choice answer must be a JSON string (option id)."));
        break;
      }
      if (c.optionsSource != null)
        break;
      (Array.isArray(c.options) ? c.options : []).some((g) => g.id === f) || s.push(ot(r, "invalidOption", `'${f}' is not a valid option id for this question.`, { option: f }));
      break;
    }
    case "multiChoice": {
      if (!Array.isArray(f)) {
        s.push(ot(r, "type", "MultiChoice answer must be a JSON array of option ids."));
        break;
      }
      const h = Array.isArray(c.options) ? c.options : [], g = new Set(h.map((C) => C.id)), q = [];
      let U = !1;
      for (const C of f) {
        if (typeof C != "string") {
          s.push(ot(r, "type", "Each MultiChoice array entry must be a string option id.")), U = !0;
          break;
        }
        q.push(C);
      }
      if (U)
        break;
      if (c.optionsSource == null)
        for (const C of q)
          g.has(C) || s.push(ot(r, "invalidOption", `'${C}' is not a valid option id for this question.`, { option: C }));
      const N = c.minSelected, p = c.maxSelected;
      ge(N) && q.length < N && s.push(ot(r, "minSelected", `At least ${N} option(s) must be selected.`, { n: N })), ge(p) && q.length > p && s.push(ot(r, "maxSelected", `At most ${p} option(s) may be selected.`, { n: p }));
      break;
    }
    case "date": {
      if (typeof f != "string") {
        s.push(ot(r, "type", "Date answer must be a JSON string in yyyy-MM-dd format."));
        break;
      }
      const h = ns(f);
      if (h === null) {
        s.push(ot(r, "invalidDate", `Date '${f}' is not yyyy-MM-dd.`));
        break;
      }
      const g = c.minDate, q = c.maxDate;
      if (typeof g == "string") {
        const U = ns(g);
        U !== null && h < U && s.push(ot(r, "minDate", `Date ${f} is before minDate ${g}.`, { min: g }));
      }
      if (typeof q == "string") {
        const U = ns(q);
        U !== null && h > U && s.push(ot(r, "maxDate", `Date ${f} is after maxDate ${q}.`, { max: q }));
      }
      break;
    }
    case "dateTime": {
      if (typeof f != "string") {
        s.push(ot(r, "type", "DateTime answer must be a JSON string in ISO 8601 format."));
        break;
      }
      const h = us(f);
      if (h === null) {
        s.push(ot(r, "invalidDateTime", `DateTime '${f}' is not valid ISO 8601.`));
        break;
      }
      const g = c.minDateTime, q = c.maxDateTime;
      if (typeof g == "string" && g.length > 0) {
        const U = us(g);
        U !== null && h < U && s.push(ot(r, "minDateTime", `DateTime is before minDateTime ${g}.`, { min: g }));
      }
      if (typeof q == "string" && q.length > 0) {
        const U = us(q);
        U !== null && h > U && s.push(ot(r, "maxDateTime", `DateTime is after maxDateTime ${q}.`, { max: q }));
      }
      break;
    }
    case "file": {
      (typeof f != "string" || f.length === 0) && s.push(ot(r, "empty", "Answer must be a non-empty file reference string."));
      break;
    }
    case "signature": {
      (typeof f != "string" || f.length === 0) && s.push(ot(r, "empty", "Answer must be a non-empty signature data url string."));
      break;
    }
    case "yesNo": {
      typeof f != "boolean" && s.push(ot(r, "type", "Yes/No answer must be a JSON boolean."));
      break;
    }
  }
  return s;
}
function Ev(c, f) {
  const r = [];
  for (const s of c ?? []) {
    const h = s, g = h.id;
    if (typeof g != "string")
      continue;
    const q = f[g];
    q != null && r.push(...Ny(h, q));
  }
  return r;
}
function is(c, f) {
  let r = c;
  for (const s of f.split(".")) {
    if (r === null || typeof r != "object")
      return;
    r = r[s];
  }
  return r;
}
function Oy(c) {
  const f = new URL(c.url);
  for (const [r, s] of Object.entries(c.queryParams ?? {}))
    f.searchParams.set(r, s);
  return f.toString();
}
function Av(c, f) {
  const r = f.itemsPath ? is(c, f.itemsPath) : c;
  if (!Array.isArray(r))
    throw new Error(`optionsSource response is not an array${f.itemsPath ? ` at '${f.itemsPath}'` : ""}.`);
  const s = f.valuePath || "ID", h = f.labelPath || "Name", g = [];
  for (const q of r) {
    const U = is(q, s);
    if (U == null || U === "")
      continue;
    const N = is(q, h);
    g.push({
      id: String(U),
      label: N == null || N === "" ? String(U) : String(N)
    });
  }
  return g;
}
async function xv(c, f) {
  const r = (f == null ? void 0 : f.fetchImpl) ?? fetch, s = {};
  f != null && f.locale && (s["Accept-Language"] = f.locale), Object.assign(s, c.headers ?? {});
  const h = await r(Oy(c), {
    headers: s,
    ...f != null && f.signal ? { signal: f.signal } : {}
  });
  if (!h.ok)
    throw new Error(`optionsSource fetch failed: HTTP ${h.status}.`);
  return Av(await h.json(), c);
}
class dn extends Error {
  constructor(r) {
    super(r.message);
    gl(this, "status");
    gl(this, "code");
    gl(this, "serverMessage");
    gl(this, "validationErrors");
    gl(this, "raw");
    this.name = "SurveyClientError", this.status = r.status, this.code = r.code, this.serverMessage = r.serverMessage, this.validationErrors = r.validationErrors, this.raw = r.raw;
  }
}
class zy {
  constructor(f) {
    gl(this, "baseUrl");
    gl(this, "fetchFn");
    this.baseUrl = f.baseUrl.replace(/\/+$/, "");
    const r = f.fetch ?? globalThis.fetch;
    if (!r)
      throw new Error("SurveyClient: no fetch available. Provide options.fetch or run in an environment with a global fetch.");
    this.fetchFn = r.bind(globalThis);
  }
  async fetchSchema(f) {
    const r = await this.send("GET", `/SurveyInstances/${encodeURIComponent(f)}/schema`);
    return this.readJson(r);
  }
  async getStatus(f) {
    const r = await this.send("GET", `/SurveyInstances/${encodeURIComponent(f)}/status`), s = await this.readJson(r);
    return {
      status: String(s.Status ?? s.status ?? "Pending"),
      schemaVersion: Number(s.SchemaVersion ?? s.schemaVersion ?? 0),
      triggeredAt: s.TriggeredAt ?? s.triggeredAt
    };
  }
  async submitResponse(f, r) {
    await this.send("POST", `/SurveyInstances/${encodeURIComponent(f)}/responses`, r);
  }
  async send(f, r, s) {
    let h;
    try {
      h = await this.fetchFn(`${this.baseUrl}${r}`, {
        method: f,
        headers: s === void 0 ? void 0 : { "Content-Type": "application/json" },
        body: s === void 0 ? void 0 : JSON.stringify(s)
      });
    } catch (g) {
      throw new dn({
        status: 0,
        code: "network",
        message: `Network error calling ${f} ${r}: ${g.message ?? g}`
      });
    }
    if (!h.ok)
      throw await this.toError(h, f, r);
    return h;
  }
  async readJson(f) {
    const r = await f.text();
    if (!r)
      throw new dn({
        status: f.status,
        code: "parse",
        message: `Empty body from ${f.url}`
      });
    try {
      return JSON.parse(r);
    } catch (s) {
      throw new dn({
        status: f.status,
        code: "parse",
        message: `Could not parse JSON from ${f.url}: ${s.message}`,
        raw: r
      });
    }
  }
  async toError(f, r, s) {
    const h = f.status === 404 ? "notFound" : f.status === 410 ? "gone" : f.status === 409 ? "conflict" : f.status === 400 ? "badRequest" : (f.status >= 500, "server"), g = await f.text();
    if (!g)
      return new dn({
        status: f.status,
        code: h,
        message: `${r} ${s} → ${f.status}`
      });
    let q;
    try {
      q = JSON.parse(g);
    } catch {
      return new dn({
        status: f.status,
        code: h,
        message: `${r} ${s} → ${f.status}: ${g.slice(0, 200)}`,
        raw: g
      });
    }
    const U = q.Message ?? q.message, N = q.Errors ?? q.errors, p = Array.isArray(N) ? N.flatMap((C) => {
      const b = C.QuestionId ?? C.questionId, j = C.Message ?? C.message;
      return b && j ? [{ questionId: b, message: j }] : [];
    }) : void 0;
    return new dn({
      status: f.status,
      code: h,
      message: `${r} ${s} → ${f.status}${U ? ": " + U : ""}`,
      serverMessage: U,
      validationErrors: p && p.length > 0 ? p : void 0,
      raw: q
    });
  }
}
function Ty(c) {
  const f = c.trim().replace(/^#/, "");
  if (!/^[0-9a-fA-F]{3}$|^[0-9a-fA-F]{6}$|^[0-9a-fA-F]{8}$/.test(f)) return null;
  const r = f.length === 3 ? f.split("").map((s) => s + s).join("") : f.slice(0, 6);
  return [
    parseInt(r.slice(0, 2), 16),
    parseInt(r.slice(2, 4), 16),
    parseInt(r.slice(4, 6), 16)
  ];
}
function cs([c, f, r]) {
  const s = (h) => Math.max(0, Math.min(255, Math.round(h))).toString(16).padStart(2, "0");
  return `#${s(c)}${s(f)}${s(r)}`;
}
function zv(c, f) {
  return [c[0] * f, c[1] * f, c[2] * f];
}
function Tv([c, f, r]) {
  const s = (h) => {
    const g = h / 255;
    return g <= 0.03928 ? g / 12.92 : Math.pow((g + 0.055) / 1.055, 2.4);
  };
  return 0.2126 * s(c) + 0.7152 * s(f) + 0.0722 * s(r);
}
function qv(c) {
  const f = {}, r = c != null && c.primaryColor ? Ty(c.primaryColor) : null;
  r && (f["--survey-primary"] = cs(r), f["--survey-primary-hover"] = cs(zv(r, 0.82)), f["--survey-primary-contrast"] = Tv(r) > 0.45 ? "#111111" : "#ffffff");
  const s = c != null && c.secondaryColor ? Ty(c.secondaryColor) : null;
  return s && (f["--survey-accent"] = cs(s)), f;
}
const My = k.createContext(null), Nv = My.Provider;
function ce() {
  const c = k.useContext(My);
  if (!c)
    throw new Error(
      "useSurveyContext must be used inside <SurveyRenderer>. Question components rely on survey state from the enclosing provider."
    );
  return c;
}
function P(c, f, r) {
  if (c == null) return "";
  if (typeof c == "string") return c;
  if (c[f]) return c[f];
  if (r && c[r]) return c[r];
  const s = Object.keys(c);
  return s.length > 0 ? c[s[0]] : "";
}
const Dy = {
  direction: "ltr",
  strings: {
    next: "Next",
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
}, Ov = {
  direction: "rtl",
  strings: {
    next: "التالي",
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
}, Mv = {
  direction: "rtl",
  strings: {
    next: "دواتر",
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
}, Dv = {
  direction: "ltr",
  strings: {
    next: "Далее",
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
}, Uv = { en: Dy, ar: Ov, ku: Mv, ru: Dv }, jv = {
  en: "English",
  ar: "العربية",
  ku: "کوردی",
  ru: "Русский",
  tr: "Türkçe",
  fa: "فارسی"
};
function Cv(c) {
  const f = jv[c];
  if (f) return f;
  try {
    const r = new Intl.DisplayNames([c], { type: "language" }).of(c);
    if (r && r !== c) return r;
  } catch {
  }
  return c;
}
function Kl(c, f) {
  return f ? c.replace(
    /\{(\w+)\}/g,
    (r, s) => s in f ? String(f[s]) : r
  ) : c;
}
function Rv(c, f, r) {
  const s = { ...Uv, ...r ?? {} };
  return s[c] ?? (f ? s[f] : void 0) ?? s.en ?? Dy;
}
const Hv = "adp-surveys", Bv = 1;
function Lv(c = {}) {
  const f = typeof window < "u", r = f && window.parent !== window, s = c.enabled ?? r, h = c.target ?? (f ? window.parent : null), g = c.targetOrigin ?? "*";
  if (!s || !h)
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
  const q = (U, N) => {
    const p = {
      source: Hv,
      version: Bv,
      type: U,
      payload: N
    };
    try {
      h.postMessage(p, g);
    } catch {
    }
  };
  return {
    loaded: () => q("survey:loaded", {}),
    screenChanged: (U) => q("survey:screen-changed", { screenId: U }),
    completed: (U) => q("survey:completed", U),
    error: (U) => q("survey:error", { message: U }),
    resize: (U) => q("survey:resize", { height: U })
  };
}
function ms(c) {
  return `adp-surveys:resume:${c}`;
}
function Yv(c, f) {
  try {
    const r = c.getItem(ms(f));
    if (!r) return null;
    const s = JSON.parse(r);
    return !s || typeof s != "object" || !s.answers ? null : s;
  } catch {
    return null;
  }
}
function Gv(c, f, r) {
  try {
    const s = { ...r, savedAt: Date.now() };
    c.setItem(ms(f), JSON.stringify(s));
  } catch {
  }
}
function Qv(c, f) {
  try {
    c.removeItem(ms(f));
  } catch {
  }
}
const fs = /* @__PURE__ */ new Map();
function Xv({
  question: c,
  Component: f
}) {
  const { locale: r, schema: s, ui: h } = ce(), g = c.optionsSource, q = `${r}|${Oy(g)}`, [U, N] = k.useState(() => {
    const w = fs.get(q);
    return w ? { status: "ready", options: w } : { status: "loading" };
  }), [p, C] = k.useState(0);
  k.useEffect(() => {
    const w = fs.get(q);
    if (w) {
      N({ status: "ready", options: w });
      return;
    }
    let B = !1;
    return N({ status: "loading" }), xv(g, { locale: r }).then((J) => {
      fs.set(q, J), B || N({ status: "ready", options: J });
    }).catch((J) => {
      B || N({ status: "error", message: J.message ?? String(J) });
    }), () => {
      B = !0;
    };
  }, [q, p]);
  const b = c.title, j = b ? /* @__PURE__ */ x.jsx("span", { className: "survey-question__label", children: P(b, r, s.defaultLocale) }) : null;
  if (U.status === "loading")
    return /* @__PURE__ */ x.jsxs("div", { className: "survey-question survey-question--options-loading", role: "status", children: [
      j,
      /* @__PURE__ */ x.jsx("p", { className: "survey-question__options-status", children: h.loadingOptions })
    ] });
  if (U.status === "error")
    return /* @__PURE__ */ x.jsxs("div", { className: "survey-question survey-question--options-error", children: [
      j,
      /* @__PURE__ */ x.jsx("p", { className: "survey-question__options-status", role: "alert", children: h.optionsLoadError }),
      /* @__PURE__ */ x.jsx(
        "button",
        {
          type: "button",
          className: "survey-button survey-button--retry",
          onClick: () => C((w) => w + 1),
          children: h.retry
        }
      )
    ] });
  const Q = c.type === "navigationList", W = {
    ...c,
    options: U.options.map((w) => ({
      id: w.id,
      label: { [r]: w.label },
      ...Q && g.nextScreen ? { nextScreen: g.nextScreen } : {}
    }))
  };
  return /* @__PURE__ */ x.jsx(f, { question: W });
}
function Zv({
  question: c,
  registry: f
}) {
  const { ui: r } = ce(), s = c.type, h = s ? f[s] : void 0;
  if (!h)
    return /* @__PURE__ */ x.jsx("div", { className: "survey-question survey-question--unknown", children: /* @__PURE__ */ x.jsxs("em", { children: [
      r.unsupportedQuestion,
      " ",
      String(s ?? "missing")
    ] }) });
  const g = Array.isArray(c.options) && c.options.length > 0;
  return c.optionsSource != null && !g ? /* @__PURE__ */ x.jsx(Xv, { question: c, Component: h }) : /* @__PURE__ */ x.jsx(h, { question: c });
}
function Vv({ question: c }) {
  const { locale: f, schema: r, answers: s, setAnswer: h } = ce(), g = c.id, q = c.title, U = c.help, N = !!c.required, p = s[g] ?? "";
  return /* @__PURE__ */ x.jsxs("div", { className: "survey-question survey-question--text", children: [
    /* @__PURE__ */ x.jsxs("label", { className: "survey-question__label", htmlFor: `q-${g}`, children: [
      P(q, f, r.defaultLocale),
      N && /* @__PURE__ */ x.jsx("span", { "aria-label": "required", className: "survey-question__required", children: " *" })
    ] }),
    U && /* @__PURE__ */ x.jsx("p", { className: "survey-question__help", children: P(U, f, r.defaultLocale) }),
    /* @__PURE__ */ x.jsx(
      "input",
      {
        id: `q-${g}`,
        className: "survey-question__input",
        type: "text",
        value: p,
        required: N,
        onChange: (C) => h(g, C.target.value)
      }
    )
  ] });
}
function wv({ question: c }) {
  const { locale: f, schema: r, answers: s, setAnswer: h } = ce(), g = c.id, q = c.title, U = c.help, N = !!c.required, p = Number(c.min ?? 0), C = Number(c.max ?? 10), b = c.lowLabel, j = c.highLabel, Q = s[g], W = [];
  for (let w = p; w <= C; w++) W.push(w);
  return /* @__PURE__ */ x.jsxs("fieldset", { className: "survey-question survey-question--nps", children: [
    /* @__PURE__ */ x.jsxs("legend", { className: "survey-question__label", children: [
      P(q, f, r.defaultLocale),
      N && /* @__PURE__ */ x.jsx("span", { "aria-label": "required", className: "survey-question__required", children: " *" })
    ] }),
    U && /* @__PURE__ */ x.jsx("p", { className: "survey-question__help", children: P(U, f, r.defaultLocale) }),
    /* @__PURE__ */ x.jsx("div", { className: "survey-question__nps-scale", role: "radiogroup", children: W.map((w) => {
      const B = Q === w;
      return /* @__PURE__ */ x.jsx(
        "button",
        {
          type: "button",
          role: "radio",
          "aria-checked": B,
          className: "survey-question__nps-step" + (B ? " survey-question__nps-step--selected" : ""),
          onClick: () => h(g, w),
          children: w
        },
        w
      );
    }) }),
    (b || j) && /* @__PURE__ */ x.jsxs("div", { className: "survey-question__nps-labels", children: [
      /* @__PURE__ */ x.jsx("span", { children: b ? P(b, f, r.defaultLocale) : "" }),
      /* @__PURE__ */ x.jsx("span", { children: j ? P(j, f, r.defaultLocale) : "" })
    ] })
  ] });
}
function Jv({ question: c }) {
  const { locale: f, schema: r } = ce(), s = c.id, h = c.title, g = c.help, q = c.options ?? [], U = (N, p) => {
    const C = {
      questionId: s,
      option: {
        id: p.id,
        nextScreen: p.nextScreen
      }
    };
    N.currentTarget.dispatchEvent(
      new CustomEvent("survey:navigationListSelect", {
        detail: C,
        bubbles: !0
      })
    );
  };
  return /* @__PURE__ */ x.jsxs("div", { className: "survey-question survey-question--navlist", children: [
    /* @__PURE__ */ x.jsx("div", { className: "survey-question__label", children: P(h, f, r.defaultLocale) }),
    g && /* @__PURE__ */ x.jsx("p", { className: "survey-question__help", children: P(g, f, r.defaultLocale) }),
    /* @__PURE__ */ x.jsx("ul", { className: "survey-navlist", role: "radiogroup", "aria-description": "Selecting an option navigates to the next screen.", children: q.map((N) => {
      const p = N.id, C = N.label;
      return /* @__PURE__ */ x.jsx("li", { className: "survey-navlist__row", children: /* @__PURE__ */ x.jsxs(
        "button",
        {
          type: "button",
          className: "survey-navlist__button",
          onClick: (b) => U(b, N),
          children: [
            /* @__PURE__ */ x.jsx("span", { className: "survey-navlist__label", children: P(C, f, r.defaultLocale) }),
            /* @__PURE__ */ x.jsx("span", { "aria-hidden": "true", className: "survey-navlist__chevron", children: "›" })
          ]
        }
      ) }, p);
    }) })
  ] });
}
function Kv({ question: c }) {
  const { locale: f, schema: r, answers: s, setAnswer: h } = ce(), g = c.id, q = c.title, U = c.help, N = c.placeholder, p = !!c.required, C = c.minLength, b = c.maxLength, j = s[g] ?? "";
  return /* @__PURE__ */ x.jsxs("div", { className: "survey-question survey-question--paragraph", children: [
    /* @__PURE__ */ x.jsxs("label", { className: "survey-question__label", htmlFor: `q-${g}`, children: [
      P(q, f, r.defaultLocale),
      p && /* @__PURE__ */ x.jsx("span", { "aria-label": "required", className: "survey-question__required", children: " *" })
    ] }),
    U && /* @__PURE__ */ x.jsx("p", { className: "survey-question__help", children: P(U, f, r.defaultLocale) }),
    /* @__PURE__ */ x.jsx(
      "textarea",
      {
        id: `q-${g}`,
        className: "survey-question__textarea",
        value: j,
        required: p,
        rows: 5,
        minLength: C,
        maxLength: b,
        placeholder: N ? P(N, f, r.defaultLocale) : void 0,
        onChange: (Q) => h(g, Q.target.value)
      }
    )
  ] });
}
function kv({ question: c }) {
  const { locale: f, schema: r, answers: s, setAnswer: h } = ce(), g = c.id, q = c.title, U = c.help, N = !!c.required, p = c.min, C = c.max, b = c.step, j = c.unit, Q = s[g], W = Q == null ? "" : String(Q);
  return /* @__PURE__ */ x.jsxs("div", { className: "survey-question survey-question--number", children: [
    /* @__PURE__ */ x.jsxs("label", { className: "survey-question__label", htmlFor: `q-${g}`, children: [
      P(q, f, r.defaultLocale),
      N && /* @__PURE__ */ x.jsx("span", { "aria-label": "required", className: "survey-question__required", children: " *" })
    ] }),
    U && /* @__PURE__ */ x.jsx("p", { className: "survey-question__help", children: P(U, f, r.defaultLocale) }),
    /* @__PURE__ */ x.jsxs("div", { className: "survey-question__number-wrap", children: [
      /* @__PURE__ */ x.jsx(
        "input",
        {
          id: `q-${g}`,
          className: "survey-question__input",
          type: "number",
          value: W,
          required: N,
          min: p,
          max: C,
          step: b,
          onChange: (w) => {
            const B = w.target.value;
            h(g, B === "" ? null : Number(B));
          }
        }
      ),
      j && /* @__PURE__ */ x.jsx("span", { className: "survey-question__unit", children: P(j, f, r.defaultLocale) })
    ] })
  ] });
}
function $v({ question: c }) {
  const { locale: f, schema: r, answers: s, setAnswer: h } = ce(), g = c.id, q = c.title, U = c.help, N = !!c.required, p = Number(c.max ?? 5), C = s[g], b = [];
  for (let j = 1; j <= p; j++) b.push(j);
  return /* @__PURE__ */ x.jsxs("fieldset", { className: "survey-question survey-question--rating", children: [
    /* @__PURE__ */ x.jsxs("legend", { className: "survey-question__label", children: [
      P(q, f, r.defaultLocale),
      N && /* @__PURE__ */ x.jsx("span", { "aria-label": "required", className: "survey-question__required", children: " *" })
    ] }),
    U && /* @__PURE__ */ x.jsx("p", { className: "survey-question__help", children: P(U, f, r.defaultLocale) }),
    /* @__PURE__ */ x.jsx("div", { className: "survey-question__rating-scale", role: "radiogroup", children: b.map((j) => {
      const Q = typeof C == "number" && j <= C;
      return /* @__PURE__ */ x.jsx(
        "button",
        {
          type: "button",
          role: "radio",
          "aria-checked": C === j,
          "aria-label": `${j}`,
          className: "survey-question__rating-star" + (Q ? " survey-question__rating-star--selected" : ""),
          onClick: () => h(g, j),
          children: /* @__PURE__ */ x.jsx("span", { "aria-hidden": "true", children: "★" })
        },
        j
      );
    }) })
  ] });
}
function Wv({ question: c }) {
  const { locale: f, schema: r, answers: s, setAnswer: h } = ce(), g = c.id, q = c.title, U = c.help, N = !!c.required, p = c.options ?? [], C = s[g];
  return /* @__PURE__ */ x.jsxs("fieldset", { className: "survey-question survey-question--single", children: [
    /* @__PURE__ */ x.jsxs("legend", { className: "survey-question__label", children: [
      P(q, f, r.defaultLocale),
      N && /* @__PURE__ */ x.jsx("span", { "aria-label": "required", className: "survey-question__required", children: " *" })
    ] }),
    U && /* @__PURE__ */ x.jsx("p", { className: "survey-question__help", children: P(U, f, r.defaultLocale) }),
    /* @__PURE__ */ x.jsx("div", { className: "survey-question__options", children: p.map((b) => /* @__PURE__ */ x.jsxs("label", { className: "survey-question__option", children: [
      /* @__PURE__ */ x.jsx(
        "input",
        {
          type: "radio",
          name: `q-${g}`,
          value: b.id,
          checked: C === b.id,
          onChange: () => h(g, b.id)
        }
      ),
      /* @__PURE__ */ x.jsx("span", { children: P(b.label, f, r.defaultLocale) })
    ] }, b.id)) })
  ] });
}
function Fv({ question: c }) {
  const { locale: f, schema: r, answers: s, setAnswer: h } = ce(), g = c.id, q = c.title, U = c.help, N = !!c.required, p = c.options ?? [], C = c.maxSelected, b = s[g] ?? [], j = (Q) => {
    if (b.includes(Q)) {
      h(g, b.filter((W) => W !== Q));
      return;
    }
    C !== void 0 && b.length >= C || h(g, [...b, Q]);
  };
  return /* @__PURE__ */ x.jsxs("fieldset", { className: "survey-question survey-question--multi", children: [
    /* @__PURE__ */ x.jsxs("legend", { className: "survey-question__label", children: [
      P(q, f, r.defaultLocale),
      N && /* @__PURE__ */ x.jsx("span", { "aria-label": "required", className: "survey-question__required", children: " *" })
    ] }),
    U && /* @__PURE__ */ x.jsx("p", { className: "survey-question__help", children: P(U, f, r.defaultLocale) }),
    /* @__PURE__ */ x.jsx("div", { className: "survey-question__options", children: p.map((Q) => {
      const W = b.includes(Q.id);
      return /* @__PURE__ */ x.jsxs("label", { className: "survey-question__option", children: [
        /* @__PURE__ */ x.jsx(
          "input",
          {
            type: "checkbox",
            checked: W,
            onChange: () => j(Q.id)
          }
        ),
        /* @__PURE__ */ x.jsx("span", { children: P(Q.label, f, r.defaultLocale) })
      ] }, Q.id);
    }) })
  ] });
}
function Iv({ question: c }) {
  const { locale: f, schema: r, answers: s, setAnswer: h, ui: g } = ce(), q = c.id, U = c.title, N = c.help, p = !!c.required, C = c.options ?? [], b = c.placeholder, j = s[q] ?? "", Q = b ? P(b, f, r.defaultLocale) : g.selectPlaceholder;
  return /* @__PURE__ */ x.jsxs("div", { className: "survey-question survey-question--dropdown", children: [
    /* @__PURE__ */ x.jsxs("label", { className: "survey-question__label", htmlFor: `q-${q}`, children: [
      P(U, f, r.defaultLocale),
      p && /* @__PURE__ */ x.jsx("span", { "aria-label": "required", className: "survey-question__required", children: " *" })
    ] }),
    N && /* @__PURE__ */ x.jsx("p", { className: "survey-question__help", children: P(N, f, r.defaultLocale) }),
    /* @__PURE__ */ x.jsxs(
      "select",
      {
        id: `q-${q}`,
        className: "survey-question__select",
        value: j,
        required: p,
        onChange: (W) => h(q, W.target.value || null),
        children: [
          /* @__PURE__ */ x.jsx("option", { value: "", children: Q }),
          C.map((W) => /* @__PURE__ */ x.jsx("option", { value: W.id, children: P(W.label, f, r.defaultLocale) }, W.id))
        ]
      }
    )
  ] });
}
function Pv({ question: c }) {
  const { locale: f, schema: r, answers: s, setAnswer: h } = ce(), g = c.id, q = c.title, U = c.help, N = !!c.required, p = c.minDate, C = c.maxDate, b = s[g] ?? "";
  return /* @__PURE__ */ x.jsxs("div", { className: "survey-question survey-question--date", children: [
    /* @__PURE__ */ x.jsxs("label", { className: "survey-question__label", htmlFor: `q-${g}`, children: [
      P(q, f, r.defaultLocale),
      N && /* @__PURE__ */ x.jsx("span", { "aria-label": "required", className: "survey-question__required", children: " *" })
    ] }),
    U && /* @__PURE__ */ x.jsx("p", { className: "survey-question__help", children: P(U, f, r.defaultLocale) }),
    /* @__PURE__ */ x.jsx(
      "input",
      {
        id: `q-${g}`,
        className: "survey-question__input",
        type: "date",
        value: b,
        required: N,
        min: p,
        max: C,
        onChange: (j) => h(g, j.target.value || null)
      }
    )
  ] });
}
function t0({ question: c }) {
  const { locale: f, schema: r, answers: s, setAnswer: h } = ce(), g = c.id, q = c.title, U = c.help, N = !!c.required, p = c.minDateTime, C = c.maxDateTime, b = s[g] ?? "", j = (Q) => {
    if (!Q) return;
    const W = Q.match(/^(\d{4}-\d{2}-\d{2}T\d{2}:\d{2})/);
    return (W == null ? void 0 : W[1]) ?? void 0;
  };
  return /* @__PURE__ */ x.jsxs("div", { className: "survey-question survey-question--datetime", children: [
    /* @__PURE__ */ x.jsxs("label", { className: "survey-question__label", htmlFor: `q-${g}`, children: [
      P(q, f, r.defaultLocale),
      N && /* @__PURE__ */ x.jsx("span", { "aria-label": "required", className: "survey-question__required", children: " *" })
    ] }),
    U && /* @__PURE__ */ x.jsx("p", { className: "survey-question__help", children: P(U, f, r.defaultLocale) }),
    /* @__PURE__ */ x.jsx(
      "input",
      {
        id: `q-${g}`,
        className: "survey-question__input",
        type: "datetime-local",
        value: j(b) ?? "",
        required: N,
        min: j(p),
        max: j(C),
        onChange: (Q) => h(g, Q.target.value || null)
      }
    )
  ] });
}
function e0({ question: c }) {
  const { locale: f, schema: r, answers: s, setAnswer: h, ui: g } = ce(), q = c.id, U = c.title, N = c.help, p = !!c.required, C = c.acceptedTypes, b = k.useRef(null), j = s[q], Q = C && C.length > 0 ? C.join(",") : void 0;
  return /* @__PURE__ */ x.jsxs("div", { className: "survey-question survey-question--file", children: [
    /* @__PURE__ */ x.jsxs("label", { className: "survey-question__label", htmlFor: `q-${q}`, children: [
      P(U, f, r.defaultLocale),
      p && /* @__PURE__ */ x.jsx("span", { "aria-label": "required", className: "survey-question__required", children: " *" })
    ] }),
    N && /* @__PURE__ */ x.jsx("p", { className: "survey-question__help", children: P(N, f, r.defaultLocale) }),
    /* @__PURE__ */ x.jsx(
      "input",
      {
        ref: b,
        id: `q-${q}`,
        className: "survey-question__file",
        type: "file",
        required: p,
        accept: Q,
        onChange: (W) => {
          var w;
          const B = (w = W.target.files) == null ? void 0 : w[0];
          if (!B) {
            h(q, null);
            return;
          }
          h(q, { name: B.name, size: B.size, type: B.type });
        }
      }
    ),
    (j == null ? void 0 : j.name) && /* @__PURE__ */ x.jsx("p", { className: "survey-question__file-name", children: Kl(g.fileRecordedName, { name: j.name }) })
  ] });
}
const ss = 480, rs = 160;
function l0({ question: c }) {
  const { locale: f, schema: r, answers: s, setAnswer: h, ui: g } = ce(), q = c.id, U = c.title, N = c.help, p = !!c.required, C = k.useRef(null), [b, j] = k.useState(!1), [Q, W] = k.useState(!!s[q]), w = () => {
    var tt;
    return ((tt = C.current) == null ? void 0 : tt.getContext("2d")) ?? null;
  }, B = (tt) => {
    const vt = tt.target.getBoundingClientRect();
    return {
      x: (tt.clientX - vt.left) / vt.width * ss,
      y: (tt.clientY - vt.top) / vt.height * rs
    };
  }, J = k.useCallback(() => {
    var tt;
    const vt = (tt = C.current) == null ? void 0 : tt.toDataURL("image/png");
    vt && h(q, vt);
  }, [q, h]), Et = () => {
    const tt = w();
    tt && (tt.clearRect(0, 0, ss, rs), W(!1), h(q, null));
  };
  return k.useEffect(() => {
    const tt = w();
    tt && (tt.lineWidth = 2, tt.lineCap = "round", tt.strokeStyle = "#111");
  }, []), /* @__PURE__ */ x.jsxs("div", { className: "survey-question survey-question--signature", children: [
    /* @__PURE__ */ x.jsxs("div", { className: "survey-question__label", children: [
      P(U, f, r.defaultLocale),
      p && /* @__PURE__ */ x.jsx("span", { "aria-label": "required", className: "survey-question__required", children: " *" })
    ] }),
    N && /* @__PURE__ */ x.jsx("p", { className: "survey-question__help", children: P(N, f, r.defaultLocale) }),
    /* @__PURE__ */ x.jsx(
      "canvas",
      {
        ref: C,
        className: "survey-question__signature-canvas",
        width: ss,
        height: rs,
        role: "img",
        "aria-label": "signature pad",
        onPointerDown: (tt) => {
          tt.target.setPointerCapture(tt.pointerId);
          const vt = w();
          if (!vt) return;
          const { x: Kt, y: Rt } = B(tt);
          vt.beginPath(), vt.moveTo(Kt, Rt), j(!0);
        },
        onPointerMove: (tt) => {
          if (!b) return;
          const vt = w();
          if (!vt) return;
          const { x: Kt, y: Rt } = B(tt);
          vt.lineTo(Kt, Rt), vt.stroke(), W(!0);
        },
        onPointerUp: () => {
          j(!1), Q && J();
        }
      }
    ),
    /* @__PURE__ */ x.jsx("div", { className: "survey-question__signature-actions", children: /* @__PURE__ */ x.jsx("button", { type: "button", className: "survey-button survey-button--ghost", onClick: Et, children: g.clearSignature }) })
  ] });
}
function a0({ question: c }) {
  const { locale: f, schema: r, answers: s, setAnswer: h, ui: g } = ce(), q = c.id, U = c.title, N = c.help, p = !!c.required, C = c.yesLabel, b = c.noLabel, j = s[q], Q = C ? P(C, f, r.defaultLocale) : g.yes, W = b ? P(b, f, r.defaultLocale) : g.no;
  return /* @__PURE__ */ x.jsxs("fieldset", { className: "survey-question survey-question--yesno", children: [
    /* @__PURE__ */ x.jsxs("legend", { className: "survey-question__label", children: [
      P(U, f, r.defaultLocale),
      p && /* @__PURE__ */ x.jsx("span", { "aria-label": "required", className: "survey-question__required", children: " *" })
    ] }),
    N && /* @__PURE__ */ x.jsx("p", { className: "survey-question__help", children: P(N, f, r.defaultLocale) }),
    /* @__PURE__ */ x.jsxs("div", { className: "survey-question__yesno", role: "radiogroup", children: [
      /* @__PURE__ */ x.jsx(
        "button",
        {
          type: "button",
          role: "radio",
          "aria-checked": j === !0,
          className: "survey-question__yesno-button" + (j === !0 ? " survey-question__yesno-button--selected" : ""),
          onClick: () => h(q, !0),
          children: Q
        }
      ),
      /* @__PURE__ */ x.jsx(
        "button",
        {
          type: "button",
          role: "radio",
          "aria-checked": j === !1,
          className: "survey-question__yesno-button" + (j === !1 ? " survey-question__yesno-button--selected" : ""),
          onClick: () => h(q, !1),
          children: W
        }
      )
    ] })
  ] });
}
function n0(c, f) {
  switch (c.code) {
    case "minLength":
      return Kl(f.minLengthError, c.params);
    case "maxLength":
      return Kl(f.maxLengthError, c.params);
    case "pattern":
      return f.patternError;
    case "min":
      return Kl(f.minError, c.params);
    case "max":
      return Kl(f.maxError, c.params);
    case "range":
      return Kl(f.rangeError, c.params);
    case "minSelected":
      return Kl(f.minSelectedError, c.params);
    case "maxSelected":
      return Kl(f.maxSelectedError, c.params);
    default:
      return f.invalidAnswerError;
  }
}
const u0 = {
  text: Vv,
  paragraph: Kv,
  number: kv,
  rating: $v,
  nps: wv,
  singleChoice: Wv,
  multiChoice: Fv,
  dropdown: Iv,
  date: Pv,
  dateTime: t0,
  file: e0,
  signature: l0,
  yesNo: a0,
  navigationList: Jv
};
function i0(c, f, r) {
  const s = c.screens.find((h) => h.id === f);
  return !s || (s.questions ?? []).length > 0 ? !1 : yu(c, f, r).kind === "end";
}
function c0({
  schema: c,
  onSubmit: f,
  initialAnswers: r,
  locale: s,
  onLocaleChange: h,
  showLocalePicker: g,
  onScreenChange: q,
  onCompleted: U,
  registry: N,
  submissionMeta: p,
  uiLocales: C,
  resumeKey: b,
  storage: j,
  emitHostMessages: Q,
  hostMessageOrigin: W,
  hostMessageTarget: w,
  activeScreenId: B,
  activeScreenJumpToken: J
}) {
  var Et, tt, vt;
  const [Kt, Rt] = k.useState(null), at = s ?? c.defaultLocale ?? "en", bt = Kt !== null && (((Et = c.locales) == null ? void 0 : Et.includes(Kt)) ?? !1) ? Kt : at, pe = k.useRef(at);
  k.useEffect(() => {
    pe.current !== at && (pe.current = at, Rt(null));
  }, [at]);
  const Sl = N ?? u0, qt = k.useMemo(
    () => Rv(bt, c.defaultLocale, C),
    [bt, c.defaultLocale, C]
  ), wt = c.locales ?? [], Ie = g ?? wt.length > 1, Le = k.useCallback(
    (L) => {
      Rt(L), h == null || h(L);
    },
    [h]
  ), Yt = j ?? (typeof globalThis < "u" ? globalThis.localStorage : void 0), z = k.useMemo(() => {
    var L;
    if (!b || !Yt) return null;
    const et = Yv(Yt, b);
    return et ? et.currentScreenId === null || c.screens.some((Gt) => Gt.id === et.currentScreenId) ? et : { ...et, currentScreenId: ((L = c.screens[0]) == null ? void 0 : L.id) ?? null } : null;
  }, []), [R, $] = k.useState(() => ({
    ...r ?? {},
    ...(z == null ? void 0 : z.answers) ?? {}
  })), [Z, dt] = k.useState(
    () => {
      var L;
      return (z == null ? void 0 : z.currentScreenId) ?? ((L = c.screens[0]) == null ? void 0 : L.id) ?? null;
    }
  );
  k.useEffect(() => {
    if (c.screens.length === 0) {
      Z !== null && dt(null);
      return;
    }
    Z !== null && c.screens.some((L) => L.id === Z) || dt(c.screens[0].id);
  }, [c, Z]);
  const [m, D] = k.useState(!1), [H, Y] = k.useState(null), [F, nt] = k.useState(/* @__PURE__ */ new Set()), [yt, kt] = k.useState(/* @__PURE__ */ new Set()), [gt, Fl] = k.useState(!1), Il = k.useRef(void 0);
  k.useEffect(() => {
    if (B === void 0) return;
    const L = `${J ?? ""}:${B ?? ""}`;
    Il.current !== L && (Il.current = L, !(B === null || gt) && c.screens.some((et) => et.id === B) && (nt(/* @__PURE__ */ new Set()), dt(B)));
  }, [B, J, c, gt]);
  const gn = k.useRef((/* @__PURE__ */ new Date()).toISOString()), Ve = k.useRef(null);
  if (Ve.current === null) {
    const L = {};
    w !== void 0 && (L.target = w), W !== void 0 && (L.targetOrigin = W), Q !== void 0 && (L.enabled = Q), Ve.current = Lv(L);
  }
  const Nt = k.useMemo(
    () => Z ? c.screens.find((L) => L.id === Z) ?? null : null,
    [c, Z]
  );
  k.useEffect(() => {
    var L;
    q == null || q(Z), (L = Ve.current) == null || L.screenChanged(Z);
  }, [Z, q]);
  const Aa = k.useRef(!1);
  k.useEffect(() => {
    var L;
    Aa.current || !Z || (Aa.current = !0, (L = Ve.current) == null || L.loaded());
  }, [Z]), k.useEffect(() => {
    !b || !Yt || gt || Gv(Yt, b, {
      answers: R,
      currentScreenId: Z,
      schemaVersion: c.version
    });
  }, [R, Z, b, Yt, gt, c.version]), k.useEffect(() => {
    gt && b && Yt && Qv(Yt, b);
  }, [gt, b, Yt]), k.useEffect(() => {
    var L;
    H && ((L = Ve.current) == null || L.error(H));
  }, [H]);
  const _l = k.useCallback((L, et) => {
    $((Gt) => ({ ...Gt, [L]: et }));
  }, []), xa = k.useCallback(
    (L) => {
      L !== null && (nt(/* @__PURE__ */ new Set()), kt(/* @__PURE__ */ new Set()), dt(L));
    },
    []
  ), za = k.useCallback(
    (L) => {
      if (!L.required) return !1;
      const et = R[L.id];
      return !!(et == null || typeof et == "string" && et.trim() === "" || Array.isArray(et) && et.length === 0);
    },
    [R]
  ), we = k.useCallback(async () => {
    var L;
    D(!0), Y(null);
    try {
      await f({
        schemaVersion: c.version ?? 0,
        answers: R,
        meta: {
          startedAt: (p == null ? void 0 : p.startedAt) ?? gn.current,
          completedAt: (p == null ? void 0 : p.completedAt) ?? (/* @__PURE__ */ new Date()).toISOString(),
          ...p ?? {}
        }
      }), Fl(!0), U == null || U(Z), (L = Ve.current) == null || L.completed({ screenId: Z, answers: R });
    } catch (et) {
      Y(et.message ?? String(et));
    } finally {
      D(!1);
    }
  }, [c.version, R, p, f, U, Z]), pn = k.useCallback(() => {
    if (!Z) return;
    const L = c.screens.find((ue) => ue.id === Z), et = ((L == null ? void 0 : L.questions) ?? []).filter(za).map((ue) => ue.id);
    if (et.length > 0) {
      nt(new Set(et));
      return;
    }
    const Gt = Ev(L == null ? void 0 : L.questions, R);
    if (Gt.length > 0) {
      kt(new Set(Gt.map((ue) => ue.questionId)));
      return;
    }
    const $t = yu(c, Z, R);
    $t.kind === "end" ? we() : xa($t.screenId);
  }, [c, Z, R, za, xa, we]), Ta = k.useRef(null);
  k.useEffect(() => {
    gt || m || !Z || !Nt || Ta.current === Z || !(!Nt.questions || Nt.questions.length === 0) || yu(c, Z, R).kind === "end" && (Ta.current = Z, we());
  }, [Z, Nt, gt, m, c, R, we]);
  const Pl = k.useRef(null);
  k.useEffect(() => {
    const L = Pl.current;
    if (!L || typeof ResizeObserver > "u") return;
    const et = new ResizeObserver((Gt) => {
      var $t;
      const ue = Gt[0];
      ue && (($t = Ve.current) == null || $t.resize(Math.ceil(ue.contentRect.height)));
    });
    return et.observe(L), () => et.disconnect();
  }, []), k.useEffect(() => {
    const L = Pl.current;
    if (!L) return;
    const et = (Gt) => {
      const $t = Gt.detail;
      if (!$t || !Z) return;
      _l($t.questionId, $t.option.id);
      const ue = { ...R, [$t.questionId]: $t.option.id }, ea = _v(
        $t.option,
        c,
        Z,
        ue
      );
      ea.kind === "end" ? we() : xa(ea.screenId);
    };
    return L.addEventListener("survey:navigationListSelect", et), () => L.removeEventListener("survey:navigationListSelect", et);
  }, [R, Z, c, _l, xa, we]);
  const Hi = k.useMemo(
    () => ({
      schema: c,
      locale: bt,
      direction: qt.direction,
      ui: qt.strings,
      answers: R,
      setAnswer: _l
    }),
    [c, bt, qt, R, _l]
  ), It = k.useMemo(() => qv(c.branding), [c.branding]), vu = (tt = c.branding) != null && tt.logoUrl ? /* @__PURE__ */ x.jsx("div", { className: "survey-brand", children: /* @__PURE__ */ x.jsx(
    "img",
    {
      className: "survey-brand__logo",
      src: c.branding.logoUrl,
      alt: "",
      onError: (L) => {
        L.currentTarget.parentElement.style.display = "none";
      }
    }
  ) }) : null, gu = wt.includes(bt) ? wt : [bt, ...wt], bn = Ie ? /* @__PURE__ */ x.jsxs("div", { className: "survey-locale", children: [
    /* @__PURE__ */ x.jsx("span", { className: "survey-locale__icon", "aria-hidden": "true", children: /* @__PURE__ */ x.jsxs("svg", { viewBox: "0 0 24 24", width: "16", height: "16", fill: "none", stroke: "currentColor", strokeWidth: "1.8", children: [
      /* @__PURE__ */ x.jsx("circle", { cx: "12", cy: "12", r: "9" }),
      /* @__PURE__ */ x.jsx("path", { d: "M3 12h18M12 3a15 15 0 0 1 0 18a15 15 0 0 1 0-18" })
    ] }) }),
    /* @__PURE__ */ x.jsx(
      "select",
      {
        className: "survey-locale__select",
        "aria-label": qt.strings.language,
        value: bt,
        onChange: (L) => Le(L.target.value),
        children: gu.map((L) => /* @__PURE__ */ x.jsx("option", { value: L, children: Cv(L) }, L))
      }
    )
  ] }) : null, El = vu || bn ? /* @__PURE__ */ x.jsxs("div", { className: "survey-chrome", children: [
    vu,
    bn
  ] }) : null;
  if (gt)
    return /* @__PURE__ */ x.jsxs(
      "div",
      {
        ref: Pl,
        className: "survey-root survey-root--done",
        dir: qt.direction,
        lang: bt,
        style: It,
        children: [
          El,
          /* @__PURE__ */ x.jsxs("div", { className: "survey-screen", children: [
            /* @__PURE__ */ x.jsx("h2", { className: "survey-screen__title", children: Nt != null && Nt.title ? P(Nt.title, bt, c.defaultLocale) : qt.strings.thankYou }),
            (Nt == null ? void 0 : Nt.description) && /* @__PURE__ */ x.jsx("p", { className: "survey-screen__description", children: P(Nt.description, bt, c.defaultLocale) })
          ] })
        ]
      }
    );
  if (!Nt)
    return /* @__PURE__ */ x.jsxs("div", { ref: Pl, className: "survey-root", dir: qt.direction, lang: bt, style: It, children: [
      El,
      /* @__PURE__ */ x.jsx("div", { className: "survey-screen", children: /* @__PURE__ */ x.jsx("em", { children: qt.strings.noScreens }) })
    ] });
  const ta = Nt.questions ?? [], pu = ta.length > 0 && ((vt = ta[ta.length - 1]) == null ? void 0 : vt.type) === "navigationList", Bi = ta.length === 0 && !Nt.nextScreen, bu = !pu && !Bi, Je = bu && Z !== null ? yu(c, Z, R) : null, fe = Je !== null && (Je.kind === "end" || Je.kind === "screen" && i0(c, Je.screenId, R));
  return /* @__PURE__ */ x.jsx(Nv, { value: Hi, children: /* @__PURE__ */ x.jsxs("div", { ref: Pl, className: "survey-root", dir: qt.direction, lang: bt, style: It, children: [
    El,
    /* @__PURE__ */ x.jsxs("div", { className: "survey-screen", children: [
      Nt.title && /* @__PURE__ */ x.jsx("h2", { className: "survey-screen__title", children: P(Nt.title, bt, c.defaultLocale) }),
      Nt.description && /* @__PURE__ */ x.jsx("p", { className: "survey-screen__description", children: P(Nt.description, bt, c.defaultLocale) }),
      /* @__PURE__ */ x.jsx("div", { className: "survey-screen__questions", children: ta.map((L, et) => {
        const Gt = L.id, $t = Gt !== void 0 && F.has(Gt) && za(L), ue = !$t && Gt !== void 0 && yt.has(Gt) && R[Gt] != null ? Ny(L, R[Gt])[0] ?? null : null;
        return /* @__PURE__ */ x.jsxs("div", { className: $t || ue !== null ? "survey-question-slot survey-question-slot--invalid" : "survey-question-slot", children: [
          /* @__PURE__ */ x.jsx(Zv, { question: L, registry: Sl }),
          $t && /* @__PURE__ */ x.jsx("p", { className: "survey-question__required-error", role: "alert", children: qt.strings.requiredError }),
          ue && /* @__PURE__ */ x.jsx("p", { className: "survey-question__required-error", role: "alert", children: n0(ue, qt.strings) })
        ] }, Gt ?? et);
      }) }),
      bu && /* @__PURE__ */ x.jsx("div", { className: "survey-screen__actions", children: /* @__PURE__ */ x.jsx(
        "button",
        {
          type: "button",
          className: "survey-button survey-button--primary",
          disabled: m,
          onClick: pn,
          children: m ? qt.strings.submitting : fe ? qt.strings.submit : qt.strings.next
        }
      ) }),
      H && /* @__PURE__ */ x.jsxs("p", { className: "survey-screen__error", role: "alert", children: [
        qt.strings.couldNotSubmit,
        " ",
        H
      ] })
    ] })
  ] }) });
}
const f0 = ".survey-root{--survey-primary: #2563eb;--survey-primary-hover: #1e40af;--survey-primary-contrast: #ffffff;--survey-accent: #f5b60c;font-family:system-ui,-apple-system,Segoe UI,Roboto,sans-serif;color:#111;max-width:640px;margin:0 auto;padding:32px 16px}.survey-chrome{display:flex;align-items:center;justify-content:space-between;gap:12px;margin-bottom:20px}.survey-brand{display:flex}.survey-brand__logo{height:28px;width:auto}.survey-locale{display:inline-flex;align-items:center;gap:4px;margin-inline-start:auto;color:#6b7280}.survey-locale__icon{display:inline-flex;flex:none}.survey-locale__select{-moz-appearance:none;appearance:none;-webkit-appearance:none;border:1px solid transparent;border-radius:6px;background:transparent;color:inherit;font:inherit;font-size:.85rem;line-height:1.2;padding:6px 4px;cursor:pointer;max-width:10rem}.survey-locale__select:hover{border-color:#d1d5db}.survey-locale__select:focus-visible{outline:2px solid var(--survey-primary, #2563eb);outline-offset:1px}.survey-screen{display:flex;flex-direction:column;gap:24px}.survey-screen__title{font-size:1.5rem;font-weight:600;margin:0}.survey-screen__description{color:#555;margin:0}.survey-screen__questions{display:flex;flex-direction:column;gap:24px}.survey-screen__actions{display:flex;justify-content:flex-end}.survey-screen__error{color:#b42318;background:#fef3f2;border:1px solid #fecdca;padding:12px 14px;border-radius:8px;margin:0}.survey-question-slot--invalid{border-inline-start:3px solid #b42318;padding-inline-start:10px}.survey-question__required-error{color:#b42318;font-size:.9rem;margin:4px 0 0}.survey-question{display:flex;flex-direction:column;gap:8px}.survey-question__label{font-weight:600;display:block}.survey-question__required{color:#b42318}.survey-question__help{margin:0;color:#666;font-size:.9rem}.survey-question__input{padding:10px 12px;border:1px solid #d0d5dd;border-radius:8px;font:inherit}.survey-question__input:focus-visible{outline:2px solid var(--survey-primary);outline-offset:1px;border-color:var(--survey-primary)}.survey-question--nps{border:none;padding:0;margin:0;display:flex;flex-direction:column;gap:8px}.survey-question__nps-scale{display:flex;gap:6px;flex-wrap:wrap}.survey-question__nps-step{min-width:40px;min-height:40px;padding:8px;border:1px solid #d0d5dd;border-radius:8px;background:#fff;font-weight:500;cursor:pointer}.survey-question__nps-step:hover{background:#f5f7fa}.survey-question__nps-step--selected{background:var(--survey-primary);border-color:var(--survey-primary);color:var(--survey-primary-contrast)}.survey-question__nps-labels{display:flex;justify-content:space-between;color:#555;font-size:.85rem}.survey-question--navlist{gap:12px}.survey-navlist{list-style:none;padding:0;margin:0;display:flex;flex-direction:column;gap:8px}.survey-navlist__row{margin:0}.survey-navlist__button{width:100%;display:flex;align-items:center;justify-content:space-between;padding:14px 16px;border:1px solid #d0d5dd;border-radius:10px;background:#fff;cursor:pointer;font:inherit;text-align:start}.survey-navlist__button:hover{background:#f5f7fa;border-color:var(--survey-primary)}.survey-navlist__chevron{font-size:1.5rem;color:#667085}.survey-root[dir=rtl] .survey-navlist__chevron{transform:scaleX(-1)}.survey-navlist__label{font-weight:500}.survey-question__textarea{padding:10px 12px;border:1px solid #d0d5dd;border-radius:8px;font:inherit;resize:vertical;min-height:96px}.survey-question__textarea:focus-visible{outline:2px solid var(--survey-primary);outline-offset:1px;border-color:var(--survey-primary)}.survey-question__number-wrap{display:flex;align-items:center;gap:8px}.survey-question__number-wrap .survey-question__input{flex:1}.survey-question__unit{color:#555;font-size:.9rem}.survey-question__rating-scale{display:flex;gap:4px}.survey-question__rating-star{background:transparent;border:none;cursor:pointer;font-size:1.8rem;line-height:1;color:#d0d5dd;padding:4px}.survey-question__rating-star:hover,.survey-question__rating-star--selected{color:var(--survey-accent)}.survey-question__options{display:flex;flex-direction:column;gap:8px}.survey-question__option{display:flex;align-items:center;gap:8px;padding:8px 12px;border:1px solid #d0d5dd;border-radius:8px;cursor:pointer}.survey-question__option:hover{background:#f5f7fa;border-color:var(--survey-primary)}.survey-question__option input{margin:0}.survey-question__select{padding:10px 12px;border:1px solid #d0d5dd;border-radius:8px;font:inherit;background:#fff}.survey-question__yesno{display:flex;gap:12px}.survey-question__yesno-button{flex:1;padding:14px 16px;border:1px solid #d0d5dd;border-radius:10px;background:#fff;font:inherit;font-weight:500;cursor:pointer}.survey-question__yesno-button:hover{background:#f5f7fa;border-color:var(--survey-primary)}.survey-question__yesno-button--selected{background:var(--survey-primary);border-color:var(--survey-primary);color:var(--survey-primary-contrast)}.survey-question__file{font:inherit}.survey-question__file-name{color:#555;font-size:.9rem;margin:0}.survey-question__signature-canvas{width:100%;max-width:480px;height:auto;aspect-ratio:3 / 1;border:1px dashed #d0d5dd;border-radius:8px;background:#fff;touch-action:none}.survey-question__signature-actions{display:flex;justify-content:flex-start;gap:8px}.survey-button{padding:10px 20px;border-radius:8px;border:1px solid transparent;cursor:pointer;font:inherit;font-weight:600}.survey-button--primary{background:var(--survey-primary);color:var(--survey-primary-contrast)}.survey-button--primary:hover{background:var(--survey-primary-hover)}.survey-button--ghost{background:#fff;color:#555;border-color:#d0d5dd}.survey-button--ghost:hover{background:#f5f7fa}.survey-button:disabled{opacity:.5;cursor:not-allowed}.survey-question__options-status{margin:6px 0;font-size:.9rem;color:var(--survey-muted, #667085)}.survey-question--options-error .survey-question__options-status{color:var(--survey-error, #b42318)}.survey-button--retry{background:transparent;color:var(--survey-primary, #4338ca);border:1px solid currentColor;padding:4px 14px;font-size:.85rem}";
var kl, mn, Ze, $l, hn, _a, mu, vn, Lt, ds, pl, hu, Sa;
class s0 extends HTMLElement {
  constructor() {
    super();
    Xe(this, Lt);
    /** Schema-mode setter. Assigning this swaps the element into schema mode and
     *  re-renders with the new schema immediately. */
    Xe(this, kl, null);
    /** Schema-mode submit handler. In API mode the element manages this itself. */
    Xe(this, mn, null);
    Xe(this, Ze, null);
    Xe(this, $l, null);
    Xe(this, hn, null);
    Xe(this, _a, null);
    /** Builder-preview jump target. Assigning a screen id makes the renderer
     *  jump to that screen (answers preserved); the user can navigate freely
     *  afterwards. Mirrors the `active-screen-id` attribute; the property wins
     *  when both are set. */
    Xe(this, mu, null);
    /** Bump to re-issue a jump to the screen already set on {@link activeScreenId}.
     *  Property-only (no attribute) — it is a transient signal, not page state. */
    Xe(this, vn, 0);
    Xe(this, hu, !1);
    this.attachShadow({ mode: "open" });
  }
  static get observedAttributes() {
    return ["instance-id", "api-base", "locale", "mode", "active-screen-id", "locale-picker"];
  }
  // ─── Lifecycle ───────────────────────────────────────────────────────────
  connectedCallback() {
    if (this.shadowRoot) {
      if (!this.shadowRoot.querySelector("style[data-shift-survey]")) {
        const r = document.createElement("style");
        r.setAttribute("data-shift-survey", ""), r.textContent = f0, this.shadowRoot.appendChild(r);
      }
      Ut(this, $l) || (Te(this, $l, document.createElement("div")), Ut(this, $l).className = "shift-survey-mount", this.shadowRoot.appendChild(Ut(this, $l))), Ut(this, Ze) || Te(this, Ze, tv.createRoot(Ut(this, $l))), ne(this, Lt, pl).call(this), ne(this, Lt, ds).call(this);
    }
  }
  disconnectedCallback() {
    queueMicrotask(() => {
      var r;
      if (!(this.isConnected || typeof window > "u")) {
        try {
          (r = Ut(this, Ze)) == null || r.unmount();
        } catch {
        }
        Te(this, Ze, null);
      }
    });
  }
  attributeChangedCallback(r, s, h) {
    s !== h && ((r === "instance-id" || r === "api-base") && (Te(this, hn, null), Te(this, _a, null), ne(this, Lt, ds).call(this)), ne(this, Lt, pl).call(this));
  }
  // ─── Properties ──────────────────────────────────────────────────────────
  get schema() {
    return Ut(this, kl);
  }
  set schema(r) {
    Te(this, kl, r), ne(this, Lt, pl).call(this);
  }
  get onSubmit() {
    return Ut(this, mn);
  }
  set onSubmit(r) {
    Te(this, mn, r), ne(this, Lt, pl).call(this);
  }
  get activeScreenId() {
    return Ut(this, mu) ?? this.getAttribute("active-screen-id");
  }
  set activeScreenId(r) {
    Te(this, mu, r), ne(this, Lt, pl).call(this);
  }
  get activeScreenJumpToken() {
    return Ut(this, vn);
  }
  set activeScreenJumpToken(r) {
    Te(this, vn, r), ne(this, Lt, pl).call(this);
  }
}
kl = new WeakMap(), mn = new WeakMap(), Ze = new WeakMap(), $l = new WeakMap(), hn = new WeakMap(), _a = new WeakMap(), mu = new WeakMap(), vn = new WeakMap(), Lt = new WeakSet(), // ─── Internals ───────────────────────────────────────────────────────────
ds = function() {
  if (Ut(this, kl)) return;
  const r = this.getAttribute("instance-id");
  if (!r) return;
  const s = this.getAttribute("api-base");
  if (!s) return;
  new zy({ baseUrl: s }).fetchSchema(r).then((g) => {
    Te(this, hn, g), ne(this, Lt, pl).call(this);
  }).catch((g) => {
    Te(this, _a, g), ne(this, Lt, Sa).call(this, "survey:error", { message: g.message }), ne(this, Lt, pl).call(this);
  });
}, pl = function() {
  if (!Ut(this, Ze)) return;
  const r = this.getAttribute("api-base"), s = this.getAttribute("instance-id"), h = this.getAttribute("locale") ?? void 0, g = this.getAttribute("mode") === "agent", q = this.getAttribute("locale-picker"), U = q === null ? void 0 : q !== "false" && q !== "off", N = Ut(this, kl) ?? Ut(this, hn);
  if (Ut(this, _a) && !N) {
    Ut(this, Ze).render(
      k.createElement(
        "div",
        { className: "shift-survey-error", role: "alert" },
        Ut(this, _a).message
      )
    );
    return;
  }
  if (!N) {
    Ut(this, Ze).render(
      k.createElement("div", { className: "shift-survey-loading" }, "Loading…")
    );
    return;
  }
  const p = Ut(this, kl) ? Ut(this, mn) ?? ((b) => {
    ne(this, Lt, Sa).call(this, "survey:completed", { ...b });
  }) : async (b) => {
    if (!r || !s)
      throw new Error("shift-survey: API mode requires both instance-id and api-base attributes.");
    await new zy({ baseUrl: r }).submitResponse(s, b);
  }, C = this.activeScreenId;
  Ut(this, Ze).render(
    k.createElement(c0, {
      schema: N,
      onSubmit: p,
      ...h ? { locale: h } : {},
      ...U === void 0 ? {} : { showLocalePicker: U },
      // Mirror the respondent's pick back onto the element so a host can read it
      // (and so the attribute stays an accurate description of what is showing).
      onLocaleChange: (b) => {
        this.getAttribute("locale") !== b && this.setAttribute("locale", b), ne(this, Lt, Sa).call(this, "survey:locale-changed", { locale: b });
      },
      ...C ? { activeScreenId: C, activeScreenJumpToken: Ut(this, vn) } : {},
      // Let the element be the resume key in API mode so two surveys on the
      // same host page don't clobber each other.
      ...s ? { resumeKey: s } : {},
      ...g ? { submissionMeta: { mode: "agent" } } : {},
      // CustomEvents are the web-component's channel; postMessage stays opt-in
      // via iframe auto-detect on the enclosing page (unchanged).
      onScreenChange: (b) => ne(this, Lt, Sa).call(this, "survey:screen-changed", { screenId: b }),
      onCompleted: (b) => ne(this, Lt, Sa).call(this, "survey:completed", { screenId: b })
    })
  ), Ut(this, hu) || (Te(this, hu, !0), ne(this, Lt, Sa).call(this, "survey:loaded", {}));
}, hu = new WeakMap(), Sa = function(r, s) {
  this.dispatchEvent(
    new CustomEvent(r, { detail: s, bubbles: !0, composed: !0 })
  );
};
function r0(c = "shift-survey") {
  typeof window > "u" || typeof customElements > "u" || customElements.get(c) || customElements.define(c, s0);
}
r0();
export {
  s0 as ShiftSurveyElement,
  r0 as registerShiftSurvey
};
//# sourceMappingURL=shift-survey.js.map
