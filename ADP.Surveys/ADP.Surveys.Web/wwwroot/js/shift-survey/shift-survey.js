var Vh = Object.defineProperty;
var ny = (c) => {
  throw TypeError(c);
};
var Kh = (c, f, r) => f in c ? Vh(c, f, { enumerable: !0, configurable: !0, writable: !0, value: r }) : c[f] = r;
var gl = (c, f, r) => Kh(c, typeof f != "symbol" ? f + "" : f, r), Xf = (c, f, r) => f.has(c) || ny("Cannot " + r);
var Rt = (c, f, r) => (Xf(c, f, "read from private field"), r ? r.call(c) : f.get(c)), We = (c, f, r) => f.has(c) ? ny("Cannot add the same private member more than once") : f instanceof WeakSet ? f.add(c) : f.set(c, r), Re = (c, f, r, s) => (Xf(c, f, "write to private field"), s ? s.call(c, r) : f.set(c, r), r), ie = (c, f, r) => (Xf(c, f, "access private method"), r);
var Zf = { exports: {} }, P = {};
/**
 * @license React
 * react.production.js
 *
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 */
var iy;
function Jh() {
  if (iy) return P;
  iy = 1;
  var c = Symbol.for("react.transitional.element"), f = Symbol.for("react.portal"), r = Symbol.for("react.fragment"), s = Symbol.for("react.strict_mode"), h = Symbol.for("react.profiler"), g = Symbol.for("react.consumer"), N = Symbol.for("react.context"), D = Symbol.for("react.forward_ref"), x = Symbol.for("react.suspense"), p = Symbol.for("react.memo"), j = Symbol.for("react.lazy"), E = Symbol.for("react.activity"), C = Symbol.iterator;
  function G(m) {
    return m === null || typeof m != "object" ? null : (m = C && m[C] || m["@@iterator"], typeof m == "function" ? m : null);
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
  }, J = Object.assign, B = {};
  function Q(m, O, R) {
    this.props = m, this.context = O, this.refs = B, this.updater = R || K;
  }
  Q.prototype.isReactComponent = {}, Q.prototype.setState = function(m, O) {
    if (typeof m != "object" && typeof m != "function" && m != null)
      throw Error(
        "takes an object of state variables to update or a function which returns an object of state variables."
      );
    this.updater.enqueueSetState(this, m, O, "setState");
  }, Q.prototype.forceUpdate = function(m) {
    this.updater.enqueueForceUpdate(this, m, "forceUpdate");
  };
  function xt() {
  }
  xt.prototype = Q.prototype;
  function w(m, O, R) {
    this.props = m, this.context = O, this.refs = B, this.updater = R || K;
  }
  var nt = w.prototype = new xt();
  nt.constructor = w, J(nt, Q.prototype), nt.isPureReactComponent = !0;
  var Vt = Array.isArray;
  function ut() {
  }
  var it = { H: null, A: null, T: null, S: null }, I = Object.prototype.hasOwnProperty;
  function ae(m, O, R) {
    var Y = R.ref;
    return {
      $$typeof: c,
      type: m,
      key: O,
      ref: Y !== void 0 ? Y : null,
      props: R
    };
  }
  function Be(m, O) {
    return ae(m.type, O, m.props);
  }
  function he(m) {
    return typeof m == "object" && m !== null && m.$$typeof === c;
  }
  function Yt(m) {
    var O = { "=": "=0", ":": "=2" };
    return "$" + m.replace(/[=:]/g, function(R) {
      return O[R];
    });
  }
  var Ze = /\/+/g;
  function Ye(m, O) {
    return typeof m == "object" && m !== null && m.key != null ? Yt("" + m.key) : O.toString(36);
  }
  function ue(m) {
    switch (m.status) {
      case "fulfilled":
        return m.value;
      case "rejected":
        throw m.reason;
      default:
        switch (typeof m.status == "string" ? m.then(ut, ut) : (m.status = "pending", m.then(
          function(O) {
            m.status === "pending" && (m.status = "fulfilled", m.value = O);
          },
          function(O) {
            m.status === "pending" && (m.status = "rejected", m.reason = O);
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
  function q(m, O, R, Y, W) {
    var et = typeof m;
    (et === "undefined" || et === "boolean") && (m = null);
    var dt = !1;
    if (m === null) dt = !0;
    else
      switch (et) {
        case "bigint":
        case "string":
        case "number":
          dt = !0;
          break;
        case "object":
          switch (m.$$typeof) {
            case c:
            case f:
              dt = !0;
              break;
            case j:
              return dt = m._init, q(
                dt(m._payload),
                O,
                R,
                Y,
                W
              );
          }
      }
    if (dt)
      return W = W(m), dt = Y === "" ? "." + Ye(m, 0) : Y, Vt(W) ? (R = "", dt != null && (R = dt.replace(Ze, "$&/") + "/"), q(W, O, R, "", function(bl) {
        return bl;
      })) : W != null && (he(W) && (W = Be(
        W,
        R + (W.key == null || m && m.key === W.key ? "" : ("" + W.key).replace(
          Ze,
          "$&/"
        ) + "/") + dt
      )), O.push(W)), 1;
    dt = 0;
    var Ut = Y === "" ? "." : Y + ":";
    if (Vt(m))
      for (var Dt = 0; Dt < m.length; Dt++)
        Y = m[Dt], et = Ut + Ye(Y, Dt), dt += q(
          Y,
          O,
          R,
          et,
          W
        );
    else if (Dt = G(m), typeof Dt == "function")
      for (m = Dt.call(m), Dt = 0; !(Y = m.next()).done; )
        Y = Y.value, et = Ut + Ye(Y, Dt++), dt += q(
          Y,
          O,
          R,
          et,
          W
        );
    else if (et === "object") {
      if (typeof m.then == "function")
        return q(
          ue(m),
          O,
          R,
          Y,
          W
        );
      throw O = String(m), Error(
        "Objects are not valid as a React child (found: " + (O === "[object Object]" ? "object with keys {" + Object.keys(m).join(", ") + "}" : O) + "). If you meant to render a collection of children, use an array instead."
      );
    }
    return dt;
  }
  function H(m, O, R) {
    if (m == null) return m;
    var Y = [], W = 0;
    return q(m, Y, "", "", function(et) {
      return O.call(R, et, W++);
    }), Y;
  }
  function V(m) {
    if (m._status === -1) {
      var O = m._result;
      O = O(), O.then(
        function(R) {
          (m._status === 0 || m._status === -1) && (m._status = 1, m._result = R);
        },
        function(R) {
          (m._status === 0 || m._status === -1) && (m._status = 2, m._result = R);
        }
      ), m._status === -1 && (m._status = 0, m._result = O);
    }
    if (m._status === 1) return m._result.default;
    throw m._result;
  }
  var gt = typeof reportError == "function" ? reportError : function(m) {
    if (typeof window == "object" && typeof window.ErrorEvent == "function") {
      var O = new window.ErrorEvent("error", {
        bubbles: !0,
        cancelable: !0,
        message: typeof m == "object" && m !== null && typeof m.message == "string" ? String(m.message) : String(m),
        error: m
      });
      if (!window.dispatchEvent(O)) return;
    } else if (typeof process == "object" && typeof process.emit == "function") {
      process.emit("uncaughtException", m);
      return;
    }
    console.error(m);
  }, pt = {
    map: H,
    forEach: function(m, O, R) {
      H(
        m,
        function() {
          O.apply(this, arguments);
        },
        R
      );
    },
    count: function(m) {
      var O = 0;
      return H(m, function() {
        O++;
      }), O;
    },
    toArray: function(m) {
      return H(m, function(O) {
        return O;
      }) || [];
    },
    only: function(m) {
      if (!he(m))
        throw Error(
          "React.Children.only expected to receive a single React element child."
        );
      return m;
    }
  };
  return P.Activity = E, P.Children = pt, P.Component = Q, P.Fragment = r, P.Profiler = h, P.PureComponent = w, P.StrictMode = s, P.Suspense = x, P.__CLIENT_INTERNALS_DO_NOT_USE_OR_WARN_USERS_THEY_CANNOT_UPGRADE = it, P.__COMPILER_RUNTIME = {
    __proto__: null,
    c: function(m) {
      return it.H.useMemoCache(m);
    }
  }, P.cache = function(m) {
    return function() {
      return m.apply(null, arguments);
    };
  }, P.cacheSignal = function() {
    return null;
  }, P.cloneElement = function(m, O, R) {
    if (m == null)
      throw Error(
        "The argument must be a React element, but you passed " + m + "."
      );
    var Y = J({}, m.props), W = m.key;
    if (O != null)
      for (et in O.key !== void 0 && (W = "" + O.key), O)
        !I.call(O, et) || et === "key" || et === "__self" || et === "__source" || et === "ref" && O.ref === void 0 || (Y[et] = O[et]);
    var et = arguments.length - 2;
    if (et === 1) Y.children = R;
    else if (1 < et) {
      for (var dt = Array(et), Ut = 0; Ut < et; Ut++)
        dt[Ut] = arguments[Ut + 2];
      Y.children = dt;
    }
    return ae(m.type, W, Y);
  }, P.createContext = function(m) {
    return m = {
      $$typeof: N,
      _currentValue: m,
      _currentValue2: m,
      _threadCount: 0,
      Provider: null,
      Consumer: null
    }, m.Provider = m, m.Consumer = {
      $$typeof: g,
      _context: m
    }, m;
  }, P.createElement = function(m, O, R) {
    var Y, W = {}, et = null;
    if (O != null)
      for (Y in O.key !== void 0 && (et = "" + O.key), O)
        I.call(O, Y) && Y !== "key" && Y !== "__self" && Y !== "__source" && (W[Y] = O[Y]);
    var dt = arguments.length - 2;
    if (dt === 1) W.children = R;
    else if (1 < dt) {
      for (var Ut = Array(dt), Dt = 0; Dt < dt; Dt++)
        Ut[Dt] = arguments[Dt + 2];
      W.children = Ut;
    }
    if (m && m.defaultProps)
      for (Y in dt = m.defaultProps, dt)
        W[Y] === void 0 && (W[Y] = dt[Y]);
    return ae(m, et, W);
  }, P.createRef = function() {
    return { current: null };
  }, P.forwardRef = function(m) {
    return { $$typeof: D, render: m };
  }, P.isValidElement = he, P.lazy = function(m) {
    return {
      $$typeof: j,
      _payload: { _status: -1, _result: m },
      _init: V
    };
  }, P.memo = function(m, O) {
    return {
      $$typeof: p,
      type: m,
      compare: O === void 0 ? null : O
    };
  }, P.startTransition = function(m) {
    var O = it.T, R = {};
    it.T = R;
    try {
      var Y = m(), W = it.S;
      W !== null && W(R, Y), typeof Y == "object" && Y !== null && typeof Y.then == "function" && Y.then(ut, gt);
    } catch (et) {
      gt(et);
    } finally {
      O !== null && R.types !== null && (O.types = R.types), it.T = O;
    }
  }, P.unstable_useCacheRefresh = function() {
    return it.H.useCacheRefresh();
  }, P.use = function(m) {
    return it.H.use(m);
  }, P.useActionState = function(m, O, R) {
    return it.H.useActionState(m, O, R);
  }, P.useCallback = function(m, O) {
    return it.H.useCallback(m, O);
  }, P.useContext = function(m) {
    return it.H.useContext(m);
  }, P.useDebugValue = function() {
  }, P.useDeferredValue = function(m, O) {
    return it.H.useDeferredValue(m, O);
  }, P.useEffect = function(m, O) {
    return it.H.useEffect(m, O);
  }, P.useEffectEvent = function(m) {
    return it.H.useEffectEvent(m);
  }, P.useId = function() {
    return it.H.useId();
  }, P.useImperativeHandle = function(m, O, R) {
    return it.H.useImperativeHandle(m, O, R);
  }, P.useInsertionEffect = function(m, O) {
    return it.H.useInsertionEffect(m, O);
  }, P.useLayoutEffect = function(m, O) {
    return it.H.useLayoutEffect(m, O);
  }, P.useMemo = function(m, O) {
    return it.H.useMemo(m, O);
  }, P.useOptimistic = function(m, O) {
    return it.H.useOptimistic(m, O);
  }, P.useReducer = function(m, O, R) {
    return it.H.useReducer(m, O, R);
  }, P.useRef = function(m) {
    return it.H.useRef(m);
  }, P.useState = function(m) {
    return it.H.useState(m);
  }, P.useSyncExternalStore = function(m, O, R) {
    return it.H.useSyncExternalStore(
      m,
      O,
      R
    );
  }, P.useTransition = function() {
    return it.H.useTransition();
  }, P.version = "19.2.5", P;
}
var cy;
function us() {
  return cy || (cy = 1, Zf.exports = Jh()), Zf.exports;
}
var F = us(), Vf = { exports: {} }, un = {}, Kf = { exports: {} }, Jf = {};
/**
 * @license React
 * scheduler.production.js
 *
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 */
var fy;
function wh() {
  return fy || (fy = 1, (function(c) {
    function f(q, H) {
      var V = q.length;
      q.push(H);
      t: for (; 0 < V; ) {
        var gt = V - 1 >>> 1, pt = q[gt];
        if (0 < h(pt, H))
          q[gt] = H, q[V] = pt, V = gt;
        else break t;
      }
    }
    function r(q) {
      return q.length === 0 ? null : q[0];
    }
    function s(q) {
      if (q.length === 0) return null;
      var H = q[0], V = q.pop();
      if (V !== H) {
        q[0] = V;
        t: for (var gt = 0, pt = q.length, m = pt >>> 1; gt < m; ) {
          var O = 2 * (gt + 1) - 1, R = q[O], Y = O + 1, W = q[Y];
          if (0 > h(R, V))
            Y < pt && 0 > h(W, R) ? (q[gt] = W, q[Y] = V, gt = Y) : (q[gt] = R, q[O] = V, gt = O);
          else if (Y < pt && 0 > h(W, V))
            q[gt] = W, q[Y] = V, gt = Y;
          else break t;
        }
      }
      return H;
    }
    function h(q, H) {
      var V = q.sortIndex - H.sortIndex;
      return V !== 0 ? V : q.id - H.id;
    }
    if (c.unstable_now = void 0, typeof performance == "object" && typeof performance.now == "function") {
      var g = performance;
      c.unstable_now = function() {
        return g.now();
      };
    } else {
      var N = Date, D = N.now();
      c.unstable_now = function() {
        return N.now() - D;
      };
    }
    var x = [], p = [], j = 1, E = null, C = 3, G = !1, K = !1, J = !1, B = !1, Q = typeof setTimeout == "function" ? setTimeout : null, xt = typeof clearTimeout == "function" ? clearTimeout : null, w = typeof setImmediate < "u" ? setImmediate : null;
    function nt(q) {
      for (var H = r(p); H !== null; ) {
        if (H.callback === null) s(p);
        else if (H.startTime <= q)
          s(p), H.sortIndex = H.expirationTime, f(x, H);
        else break;
        H = r(p);
      }
    }
    function Vt(q) {
      if (J = !1, nt(q), !K)
        if (r(x) !== null)
          K = !0, ut || (ut = !0, Yt());
        else {
          var H = r(p);
          H !== null && ue(Vt, H.startTime - q);
        }
    }
    var ut = !1, it = -1, I = 5, ae = -1;
    function Be() {
      return B ? !0 : !(c.unstable_now() - ae < I);
    }
    function he() {
      if (B = !1, ut) {
        var q = c.unstable_now();
        ae = q;
        var H = !0;
        try {
          t: {
            K = !1, J && (J = !1, xt(it), it = -1), G = !0;
            var V = C;
            try {
              e: {
                for (nt(q), E = r(x); E !== null && !(E.expirationTime > q && Be()); ) {
                  var gt = E.callback;
                  if (typeof gt == "function") {
                    E.callback = null, C = E.priorityLevel;
                    var pt = gt(
                      E.expirationTime <= q
                    );
                    if (q = c.unstable_now(), typeof pt == "function") {
                      E.callback = pt, nt(q), H = !0;
                      break e;
                    }
                    E === r(x) && s(x), nt(q);
                  } else s(x);
                  E = r(x);
                }
                if (E !== null) H = !0;
                else {
                  var m = r(p);
                  m !== null && ue(
                    Vt,
                    m.startTime - q
                  ), H = !1;
                }
              }
              break t;
            } finally {
              E = null, C = V, G = !1;
            }
            H = void 0;
          }
        } finally {
          H ? Yt() : ut = !1;
        }
      }
    }
    var Yt;
    if (typeof w == "function")
      Yt = function() {
        w(he);
      };
    else if (typeof MessageChannel < "u") {
      var Ze = new MessageChannel(), Ye = Ze.port2;
      Ze.port1.onmessage = he, Yt = function() {
        Ye.postMessage(null);
      };
    } else
      Yt = function() {
        Q(he, 0);
      };
    function ue(q, H) {
      it = Q(function() {
        q(c.unstable_now());
      }, H);
    }
    c.unstable_IdlePriority = 5, c.unstable_ImmediatePriority = 1, c.unstable_LowPriority = 4, c.unstable_NormalPriority = 3, c.unstable_Profiling = null, c.unstable_UserBlockingPriority = 2, c.unstable_cancelCallback = function(q) {
      q.callback = null;
    }, c.unstable_forceFrameRate = function(q) {
      0 > q || 125 < q ? console.error(
        "forceFrameRate takes a positive int between 0 and 125, forcing frame rates higher than 125 fps is not supported"
      ) : I = 0 < q ? Math.floor(1e3 / q) : 5;
    }, c.unstable_getCurrentPriorityLevel = function() {
      return C;
    }, c.unstable_next = function(q) {
      switch (C) {
        case 1:
        case 2:
        case 3:
          var H = 3;
          break;
        default:
          H = C;
      }
      var V = C;
      C = H;
      try {
        return q();
      } finally {
        C = V;
      }
    }, c.unstable_requestPaint = function() {
      B = !0;
    }, c.unstable_runWithPriority = function(q, H) {
      switch (q) {
        case 1:
        case 2:
        case 3:
        case 4:
        case 5:
          break;
        default:
          q = 3;
      }
      var V = C;
      C = q;
      try {
        return H();
      } finally {
        C = V;
      }
    }, c.unstable_scheduleCallback = function(q, H, V) {
      var gt = c.unstable_now();
      switch (typeof V == "object" && V !== null ? (V = V.delay, V = typeof V == "number" && 0 < V ? gt + V : gt) : V = gt, q) {
        case 1:
          var pt = -1;
          break;
        case 2:
          pt = 250;
          break;
        case 5:
          pt = 1073741823;
          break;
        case 4:
          pt = 1e4;
          break;
        default:
          pt = 5e3;
      }
      return pt = V + pt, q = {
        id: j++,
        callback: H,
        priorityLevel: q,
        startTime: V,
        expirationTime: pt,
        sortIndex: -1
      }, V > gt ? (q.sortIndex = V, f(p, q), r(x) === null && q === r(p) && (J ? (xt(it), it = -1) : J = !0, ue(Vt, V - gt))) : (q.sortIndex = pt, f(x, q), K || G || (K = !0, ut || (ut = !0, Yt()))), q;
    }, c.unstable_shouldYield = Be, c.unstable_wrapCallback = function(q) {
      var H = C;
      return function() {
        var V = C;
        C = H;
        try {
          return q.apply(this, arguments);
        } finally {
          C = V;
        }
      };
    };
  })(Jf)), Jf;
}
var sy;
function kh() {
  return sy || (sy = 1, Kf.exports = wh()), Kf.exports;
}
var wf = { exports: {} }, ee = {};
/**
 * @license React
 * react-dom.production.js
 *
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 */
var ry;
function $h() {
  if (ry) return ee;
  ry = 1;
  var c = us();
  function f(x) {
    var p = "https://react.dev/errors/" + x;
    if (1 < arguments.length) {
      p += "?args[]=" + encodeURIComponent(arguments[1]);
      for (var j = 2; j < arguments.length; j++)
        p += "&args[]=" + encodeURIComponent(arguments[j]);
    }
    return "Minified React error #" + x + "; visit " + p + " for the full message or use the non-minified dev environment for full errors and additional helpful warnings.";
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
  function g(x, p, j) {
    var E = 3 < arguments.length && arguments[3] !== void 0 ? arguments[3] : null;
    return {
      $$typeof: h,
      key: E == null ? null : "" + E,
      children: x,
      containerInfo: p,
      implementation: j
    };
  }
  var N = c.__CLIENT_INTERNALS_DO_NOT_USE_OR_WARN_USERS_THEY_CANNOT_UPGRADE;
  function D(x, p) {
    if (x === "font") return "";
    if (typeof p == "string")
      return p === "use-credentials" ? p : "";
  }
  return ee.__DOM_INTERNALS_DO_NOT_USE_OR_WARN_USERS_THEY_CANNOT_UPGRADE = s, ee.createPortal = function(x, p) {
    var j = 2 < arguments.length && arguments[2] !== void 0 ? arguments[2] : null;
    if (!p || p.nodeType !== 1 && p.nodeType !== 9 && p.nodeType !== 11)
      throw Error(f(299));
    return g(x, p, null, j);
  }, ee.flushSync = function(x) {
    var p = N.T, j = s.p;
    try {
      if (N.T = null, s.p = 2, x) return x();
    } finally {
      N.T = p, s.p = j, s.d.f();
    }
  }, ee.preconnect = function(x, p) {
    typeof x == "string" && (p ? (p = p.crossOrigin, p = typeof p == "string" ? p === "use-credentials" ? p : "" : void 0) : p = null, s.d.C(x, p));
  }, ee.prefetchDNS = function(x) {
    typeof x == "string" && s.d.D(x);
  }, ee.preinit = function(x, p) {
    if (typeof x == "string" && p && typeof p.as == "string") {
      var j = p.as, E = D(j, p.crossOrigin), C = typeof p.integrity == "string" ? p.integrity : void 0, G = typeof p.fetchPriority == "string" ? p.fetchPriority : void 0;
      j === "style" ? s.d.S(
        x,
        typeof p.precedence == "string" ? p.precedence : void 0,
        {
          crossOrigin: E,
          integrity: C,
          fetchPriority: G
        }
      ) : j === "script" && s.d.X(x, {
        crossOrigin: E,
        integrity: C,
        fetchPriority: G,
        nonce: typeof p.nonce == "string" ? p.nonce : void 0
      });
    }
  }, ee.preinitModule = function(x, p) {
    if (typeof x == "string")
      if (typeof p == "object" && p !== null) {
        if (p.as == null || p.as === "script") {
          var j = D(
            p.as,
            p.crossOrigin
          );
          s.d.M(x, {
            crossOrigin: j,
            integrity: typeof p.integrity == "string" ? p.integrity : void 0,
            nonce: typeof p.nonce == "string" ? p.nonce : void 0
          });
        }
      } else p == null && s.d.M(x);
  }, ee.preload = function(x, p) {
    if (typeof x == "string" && typeof p == "object" && p !== null && typeof p.as == "string") {
      var j = p.as, E = D(j, p.crossOrigin);
      s.d.L(x, j, {
        crossOrigin: E,
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
  }, ee.preloadModule = function(x, p) {
    if (typeof x == "string")
      if (p) {
        var j = D(p.as, p.crossOrigin);
        s.d.m(x, {
          as: typeof p.as == "string" && p.as !== "script" ? p.as : void 0,
          crossOrigin: j,
          integrity: typeof p.integrity == "string" ? p.integrity : void 0
        });
      } else s.d.m(x);
  }, ee.requestFormReset = function(x) {
    s.d.r(x);
  }, ee.unstable_batchedUpdates = function(x, p) {
    return x(p);
  }, ee.useFormState = function(x, p, j) {
    return N.H.useFormState(x, p, j);
  }, ee.useFormStatus = function() {
    return N.H.useHostTransitionStatus();
  }, ee.version = "19.2.5", ee;
}
var oy;
function Wh() {
  if (oy) return wf.exports;
  oy = 1;
  function c() {
    if (!(typeof __REACT_DEVTOOLS_GLOBAL_HOOK__ > "u" || typeof __REACT_DEVTOOLS_GLOBAL_HOOK__.checkDCE != "function"))
      try {
        __REACT_DEVTOOLS_GLOBAL_HOOK__.checkDCE(c);
      } catch (f) {
        console.error(f);
      }
  }
  return c(), wf.exports = $h(), wf.exports;
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
var dy;
function Fh() {
  if (dy) return un;
  dy = 1;
  var c = kh(), f = us(), r = Wh();
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
  function N(t) {
    if (t.tag === 13) {
      var e = t.memoizedState;
      if (e === null && (t = t.alternate, t !== null && (e = t.memoizedState)), e !== null) return e.dehydrated;
    }
    return null;
  }
  function D(t) {
    if (t.tag === 31) {
      var e = t.memoizedState;
      if (e === null && (t = t.alternate, t !== null && (e = t.memoizedState)), e !== null) return e.dehydrated;
    }
    return null;
  }
  function x(t) {
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
      var u = l.return;
      if (u === null) break;
      var n = u.alternate;
      if (n === null) {
        if (a = u.return, a !== null) {
          l = a;
          continue;
        }
        break;
      }
      if (u.child === n.child) {
        for (n = u.child; n; ) {
          if (n === l) return x(u), t;
          if (n === a) return x(u), e;
          n = n.sibling;
        }
        throw Error(s(188));
      }
      if (l.return !== a.return) l = u, a = n;
      else {
        for (var i = !1, o = u.child; o; ) {
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
        if (!i) {
          for (o = n.child; o; ) {
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
          if (!i) throw Error(s(189));
        }
      }
      if (l.alternate !== a) throw Error(s(190));
    }
    if (l.tag !== 3) throw Error(s(188));
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
  var E = Object.assign, C = Symbol.for("react.element"), G = Symbol.for("react.transitional.element"), K = Symbol.for("react.portal"), J = Symbol.for("react.fragment"), B = Symbol.for("react.strict_mode"), Q = Symbol.for("react.profiler"), xt = Symbol.for("react.consumer"), w = Symbol.for("react.context"), nt = Symbol.for("react.forward_ref"), Vt = Symbol.for("react.suspense"), ut = Symbol.for("react.suspense_list"), it = Symbol.for("react.memo"), I = Symbol.for("react.lazy"), ae = Symbol.for("react.activity"), Be = Symbol.for("react.memo_cache_sentinel"), he = Symbol.iterator;
  function Yt(t) {
    return t === null || typeof t != "object" ? null : (t = he && t[he] || t["@@iterator"], typeof t == "function" ? t : null);
  }
  var Ze = Symbol.for("react.client.reference");
  function Ye(t) {
    if (t == null) return null;
    if (typeof t == "function")
      return t.$$typeof === Ze ? null : t.displayName || t.name || null;
    if (typeof t == "string") return t;
    switch (t) {
      case J:
        return "Fragment";
      case Q:
        return "Profiler";
      case B:
        return "StrictMode";
      case Vt:
        return "Suspense";
      case ut:
        return "SuspenseList";
      case ae:
        return "Activity";
    }
    if (typeof t == "object")
      switch (t.$$typeof) {
        case K:
          return "Portal";
        case w:
          return t.displayName || "Context";
        case xt:
          return (t._context.displayName || "Context") + ".Consumer";
        case nt:
          var e = t.render;
          return t = t.displayName, t || (t = e.displayName || e.name || "", t = t !== "" ? "ForwardRef(" + t + ")" : "ForwardRef"), t;
        case it:
          return e = t.displayName || null, e !== null ? e : Ye(t.type) || "Memo";
        case I:
          e = t._payload, t = t._init;
          try {
            return Ye(t(e));
          } catch {
          }
      }
    return null;
  }
  var ue = Array.isArray, q = f.__CLIENT_INTERNALS_DO_NOT_USE_OR_WARN_USERS_THEY_CANNOT_UPGRADE, H = r.__DOM_INTERNALS_DO_NOT_USE_OR_WARN_USERS_THEY_CANNOT_UPGRADE, V = {
    pending: !1,
    data: null,
    method: null,
    action: null
  }, gt = [], pt = -1;
  function m(t) {
    return { current: t };
  }
  function O(t) {
    0 > pt || (t.current = gt[pt], gt[pt] = null, pt--);
  }
  function R(t, e) {
    pt++, gt[pt] = t.current, t.current = e;
  }
  var Y = m(null), W = m(null), et = m(null), dt = m(null);
  function Ut(t, e) {
    switch (R(et, e), R(W, t), R(Y, null), e.nodeType) {
      case 9:
      case 11:
        t = (t = e.documentElement) && (t = t.namespaceURI) ? Nd(t) : 0;
        break;
      default:
        if (t = e.tagName, e = e.namespaceURI)
          e = Nd(e), t = Od(e, t);
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
    O(Y), R(Y, t);
  }
  function Dt() {
    O(Y), O(W), O(et);
  }
  function bl(t) {
    t.memoizedState !== null && R(dt, t);
    var e = Y.current, l = Od(e, t.type);
    e !== l && (R(W, t), R(Y, l));
  }
  function Ve(t) {
    W.current === t && (O(Y), O(W)), dt.current === t && (O(dt), tn._currentValue = V);
  }
  var cu, ga;
  function Ke(t) {
    if (cu === void 0)
      try {
        throw Error();
      } catch (l) {
        var e = l.stack.trim().match(/\n( *(at )?)/);
        cu = e && e[1] || "", ga = -1 < l.stack.indexOf(`
    at`) ? " (<anonymous>)" : -1 < l.stack.indexOf("@") ? "@unknown:0:0" : "";
      }
    return `
` + cu + t + ga;
  }
  var Ie = !1;
  function fu(t, e) {
    if (!t || Ie) return "";
    Ie = !0;
    var l = Error.prepareStackTrace;
    Error.prepareStackTrace = void 0;
    try {
      var a = {
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
                } catch (A) {
                  var _ = A;
                }
                Reflect.construct(t, [], U);
              } else {
                try {
                  U.call();
                } catch (A) {
                  _ = A;
                }
                t.call(U.prototype);
              }
            } else {
              try {
                throw Error();
              } catch (A) {
                _ = A;
              }
              (U = t()) && typeof U.catch == "function" && U.catch(function() {
              });
            }
          } catch (A) {
            if (A && _ && typeof A.stack == "string")
              return [A.stack, _.stack];
          }
          return [null, null];
        }
      };
      a.DetermineComponentFrameRoot.displayName = "DetermineComponentFrameRoot";
      var u = Object.getOwnPropertyDescriptor(
        a.DetermineComponentFrameRoot,
        "name"
      );
      u && u.configurable && Object.defineProperty(
        a.DetermineComponentFrameRoot,
        "name",
        { value: "DetermineComponentFrameRoot" }
      );
      var n = a.DetermineComponentFrameRoot(), i = n[0], o = n[1];
      if (i && o) {
        var d = i.split(`
`), S = o.split(`
`);
        for (u = a = 0; a < d.length && !d[a].includes("DetermineComponentFrameRoot"); )
          a++;
        for (; u < S.length && !S[u].includes(
          "DetermineComponentFrameRoot"
        ); )
          u++;
        if (a === d.length || u === S.length)
          for (a = d.length - 1, u = S.length - 1; 1 <= a && 0 <= u && d[a] !== S[u]; )
            u--;
        for (; 1 <= a && 0 <= u; a--, u--)
          if (d[a] !== S[u]) {
            if (a !== 1 || u !== 1)
              do
                if (a--, u--, 0 > u || d[a] !== S[u]) {
                  var T = `
` + d[a].replace(" at new ", " at ");
                  return t.displayName && T.includes("<anonymous>") && (T = T.replace("<anonymous>", t.displayName)), T;
                }
              while (1 <= a && 0 <= u);
            break;
          }
      }
    } finally {
      Ie = !1, Error.prepareStackTrace = l;
    }
    return (l = t ? t.displayName || t.name : "") ? Ke(l) : "";
  }
  function Ni(t, e) {
    switch (t.tag) {
      case 26:
      case 27:
      case 5:
        return Ke(t.type);
      case 16:
        return Ke("Lazy");
      case 13:
        return t.child !== e && e !== null ? Ke("Suspense Fallback") : Ke("Suspense");
      case 19:
        return Ke("SuspenseList");
      case 0:
      case 15:
        return fu(t.type, !1);
      case 11:
        return fu(t.type.render, !1);
      case 1:
        return fu(t.type, !0);
      case 31:
        return Ke("Activity");
      default:
        return "";
    }
  }
  function su(t) {
    try {
      var e = "", l = null;
      do
        e += Ni(t, l), l = t, t = t.return;
      while (t);
      return e;
    } catch (a) {
      return `
Error generating stack: ` + a.message + `
` + a.stack;
    }
  }
  var Sl = Object.prototype.hasOwnProperty, ru = c.unstable_scheduleCallback, Z = c.unstable_cancelCallback, St = c.unstable_shouldYield, Lt = c.unstable_requestPaint, bt = c.unstable_now, te = c.unstable_getCurrentPriorityLevel, ou = c.unstable_ImmediatePriority, is = c.unstable_UserBlockingPriority, rn = c.unstable_NormalPriority, Ty = c.unstable_LowPriority, cs = c.unstable_IdlePriority, xy = c.log, qy = c.unstable_setDisableYieldValue, du = null, ve = null;
  function _l(t) {
    if (typeof xy == "function" && qy(t), ve && typeof ve.setStrictMode == "function")
      try {
        ve.setStrictMode(du, t);
      } catch {
      }
  }
  var ge = Math.clz32 ? Math.clz32 : My, Ny = Math.log, Oy = Math.LN2;
  function My(t) {
    return t >>>= 0, t === 0 ? 32 : 31 - (Ny(t) / Oy | 0) | 0;
  }
  var on = 256, dn = 262144, yn = 4194304;
  function Wl(t) {
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
  function mn(t, e, l) {
    var a = t.pendingLanes;
    if (a === 0) return 0;
    var u = 0, n = t.suspendedLanes, i = t.pingedLanes;
    t = t.warmLanes;
    var o = a & 134217727;
    return o !== 0 ? (a = o & ~n, a !== 0 ? u = Wl(a) : (i &= o, i !== 0 ? u = Wl(i) : l || (l = o & ~t, l !== 0 && (u = Wl(l))))) : (o = a & ~n, o !== 0 ? u = Wl(o) : i !== 0 ? u = Wl(i) : l || (l = a & ~t, l !== 0 && (u = Wl(l)))), u === 0 ? 0 : e !== 0 && e !== u && (e & n) === 0 && (n = u & -u, l = e & -e, n >= l || n === 32 && (l & 4194048) !== 0) ? e : u;
  }
  function yu(t, e) {
    return (t.pendingLanes & ~(t.suspendedLanes & ~t.pingedLanes) & e) === 0;
  }
  function Dy(t, e) {
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
  function fs() {
    var t = yn;
    return yn <<= 1, (yn & 62914560) === 0 && (yn = 4194304), t;
  }
  function Oi(t) {
    for (var e = [], l = 0; 31 > l; l++) e.push(t);
    return e;
  }
  function mu(t, e) {
    t.pendingLanes |= e, e !== 268435456 && (t.suspendedLanes = 0, t.pingedLanes = 0, t.warmLanes = 0);
  }
  function Uy(t, e, l, a, u, n) {
    var i = t.pendingLanes;
    t.pendingLanes = l, t.suspendedLanes = 0, t.pingedLanes = 0, t.warmLanes = 0, t.expiredLanes &= l, t.entangledLanes &= l, t.errorRecoveryDisabledLanes &= l, t.shellSuspendCounter = 0;
    var o = t.entanglements, d = t.expirationTimes, S = t.hiddenUpdates;
    for (l = i & ~l; 0 < l; ) {
      var T = 31 - ge(l), U = 1 << T;
      o[T] = 0, d[T] = -1;
      var _ = S[T];
      if (_ !== null)
        for (S[T] = null, T = 0; T < _.length; T++) {
          var A = _[T];
          A !== null && (A.lane &= -536870913);
        }
      l &= ~U;
    }
    a !== 0 && ss(t, a, 0), n !== 0 && u === 0 && t.tag !== 0 && (t.suspendedLanes |= n & ~(i & ~e));
  }
  function ss(t, e, l) {
    t.pendingLanes |= e, t.suspendedLanes &= ~e;
    var a = 31 - ge(e);
    t.entangledLanes |= e, t.entanglements[a] = t.entanglements[a] | 1073741824 | l & 261930;
  }
  function rs(t, e) {
    var l = t.entangledLanes |= e;
    for (t = t.entanglements; l; ) {
      var a = 31 - ge(l), u = 1 << a;
      u & e | t[a] & e && (t[a] |= e), l &= ~u;
    }
  }
  function os(t, e) {
    var l = e & -e;
    return l = (l & 42) !== 0 ? 1 : Mi(l), (l & (t.suspendedLanes | e)) !== 0 ? 0 : l;
  }
  function Mi(t) {
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
  function Di(t) {
    return t &= -t, 2 < t ? 8 < t ? (t & 134217727) !== 0 ? 32 : 268435456 : 8 : 2;
  }
  function ds() {
    var t = H.p;
    return t !== 0 ? t : (t = window.event, t === void 0 ? 32 : Id(t.type));
  }
  function ys(t, e) {
    var l = H.p;
    try {
      return H.p = t, e();
    } finally {
      H.p = l;
    }
  }
  var El = Math.random().toString(36).slice(2), $t = "__reactFiber$" + El, ce = "__reactProps$" + El, pa = "__reactContainer$" + El, Ui = "__reactEvents$" + El, Cy = "__reactListeners$" + El, jy = "__reactHandles$" + El, ms = "__reactResources$" + El, hu = "__reactMarker$" + El;
  function Ci(t) {
    delete t[$t], delete t[ce], delete t[Ui], delete t[Cy], delete t[jy];
  }
  function ba(t) {
    var e = t[$t];
    if (e) return e;
    for (var l = t.parentNode; l; ) {
      if (e = l[pa] || l[$t]) {
        if (l = e.alternate, e.child !== null || l !== null && l.child !== null)
          for (t = Hd(t); t !== null; ) {
            if (l = t[$t]) return l;
            t = Hd(t);
          }
        return e;
      }
      t = l, l = t.parentNode;
    }
    return null;
  }
  function Sa(t) {
    if (t = t[$t] || t[pa]) {
      var e = t.tag;
      if (e === 5 || e === 6 || e === 13 || e === 31 || e === 26 || e === 27 || e === 3)
        return t;
    }
    return null;
  }
  function vu(t) {
    var e = t.tag;
    if (e === 5 || e === 26 || e === 27 || e === 6) return t.stateNode;
    throw Error(s(33));
  }
  function _a(t) {
    var e = t[ms];
    return e || (e = t[ms] = { hoistableStyles: /* @__PURE__ */ new Map(), hoistableScripts: /* @__PURE__ */ new Map() }), e;
  }
  function wt(t) {
    t[hu] = !0;
  }
  var hs = /* @__PURE__ */ new Set(), vs = {};
  function Fl(t, e) {
    Ea(t, e), Ea(t + "Capture", e);
  }
  function Ea(t, e) {
    for (vs[t] = e, t = 0; t < e.length; t++)
      hs.add(e[t]);
  }
  var Ry = RegExp(
    "^[:A-Z_a-z\\u00C0-\\u00D6\\u00D8-\\u00F6\\u00F8-\\u02FF\\u0370-\\u037D\\u037F-\\u1FFF\\u200C-\\u200D\\u2070-\\u218F\\u2C00-\\u2FEF\\u3001-\\uD7FF\\uF900-\\uFDCF\\uFDF0-\\uFFFD][:A-Z_a-z\\u00C0-\\u00D6\\u00D8-\\u00F6\\u00F8-\\u02FF\\u0370-\\u037D\\u037F-\\u1FFF\\u200C-\\u200D\\u2070-\\u218F\\u2C00-\\u2FEF\\u3001-\\uD7FF\\uF900-\\uFDCF\\uFDF0-\\uFFFD\\-.0-9\\u00B7\\u0300-\\u036F\\u203F-\\u2040]*$"
  ), gs = {}, ps = {};
  function Hy(t) {
    return Sl.call(ps, t) ? !0 : Sl.call(gs, t) ? !1 : Ry.test(t) ? ps[t] = !0 : (gs[t] = !0, !1);
  }
  function hn(t, e, l) {
    if (Hy(e))
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
  function vn(t, e, l) {
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
  function Te(t) {
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
  function bs(t) {
    var e = t.type;
    return (t = t.nodeName) && t.toLowerCase() === "input" && (e === "checkbox" || e === "radio");
  }
  function By(t, e, l) {
    var a = Object.getOwnPropertyDescriptor(
      t.constructor.prototype,
      e
    );
    if (!t.hasOwnProperty(e) && typeof a < "u" && typeof a.get == "function" && typeof a.set == "function") {
      var u = a.get, n = a.set;
      return Object.defineProperty(t, e, {
        configurable: !0,
        get: function() {
          return u.call(this);
        },
        set: function(i) {
          l = "" + i, n.call(this, i);
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
  function ji(t) {
    if (!t._valueTracker) {
      var e = bs(t) ? "checked" : "value";
      t._valueTracker = By(
        t,
        e,
        "" + t[e]
      );
    }
  }
  function Ss(t) {
    if (!t) return !1;
    var e = t._valueTracker;
    if (!e) return !0;
    var l = e.getValue(), a = "";
    return t && (a = bs(t) ? t.checked ? "true" : "false" : t.value), t = a, t !== l ? (e.setValue(t), !0) : !1;
  }
  function gn(t) {
    if (t = t || (typeof document < "u" ? document : void 0), typeof t > "u") return null;
    try {
      return t.activeElement || t.body;
    } catch {
      return t.body;
    }
  }
  var Yy = /[\n"\\]/g;
  function xe(t) {
    return t.replace(
      Yy,
      function(e) {
        return "\\" + e.charCodeAt(0).toString(16) + " ";
      }
    );
  }
  function Ri(t, e, l, a, u, n, i, o) {
    t.name = "", i != null && typeof i != "function" && typeof i != "symbol" && typeof i != "boolean" ? t.type = i : t.removeAttribute("type"), e != null ? i === "number" ? (e === 0 && t.value === "" || t.value != e) && (t.value = "" + Te(e)) : t.value !== "" + Te(e) && (t.value = "" + Te(e)) : i !== "submit" && i !== "reset" || t.removeAttribute("value"), e != null ? Hi(t, i, Te(e)) : l != null ? Hi(t, i, Te(l)) : a != null && t.removeAttribute("value"), u == null && n != null && (t.defaultChecked = !!n), u != null && (t.checked = u && typeof u != "function" && typeof u != "symbol"), o != null && typeof o != "function" && typeof o != "symbol" && typeof o != "boolean" ? t.name = "" + Te(o) : t.removeAttribute("name");
  }
  function _s(t, e, l, a, u, n, i, o) {
    if (n != null && typeof n != "function" && typeof n != "symbol" && typeof n != "boolean" && (t.type = n), e != null || l != null) {
      if (!(n !== "submit" && n !== "reset" || e != null)) {
        ji(t);
        return;
      }
      l = l != null ? "" + Te(l) : "", e = e != null ? "" + Te(e) : l, o || e === t.value || (t.value = e), t.defaultValue = e;
    }
    a = a ?? u, a = typeof a != "function" && typeof a != "symbol" && !!a, t.checked = o ? t.checked : !!a, t.defaultChecked = !!a, i != null && typeof i != "function" && typeof i != "symbol" && typeof i != "boolean" && (t.name = i), ji(t);
  }
  function Hi(t, e, l) {
    e === "number" && gn(t.ownerDocument) === t || t.defaultValue === "" + l || (t.defaultValue = "" + l);
  }
  function Aa(t, e, l, a) {
    if (t = t.options, e) {
      e = {};
      for (var u = 0; u < l.length; u++)
        e["$" + l[u]] = !0;
      for (l = 0; l < t.length; l++)
        u = e.hasOwnProperty("$" + t[l].value), t[l].selected !== u && (t[l].selected = u), u && a && (t[l].defaultSelected = !0);
    } else {
      for (l = "" + Te(l), e = null, u = 0; u < t.length; u++) {
        if (t[u].value === l) {
          t[u].selected = !0, a && (t[u].defaultSelected = !0);
          return;
        }
        e !== null || t[u].disabled || (e = t[u]);
      }
      e !== null && (e.selected = !0);
    }
  }
  function Es(t, e, l) {
    if (e != null && (e = "" + Te(e), e !== t.value && (t.value = e), l == null)) {
      t.defaultValue !== e && (t.defaultValue = e);
      return;
    }
    t.defaultValue = l != null ? "" + Te(l) : "";
  }
  function As(t, e, l, a) {
    if (e == null) {
      if (a != null) {
        if (l != null) throw Error(s(92));
        if (ue(a)) {
          if (1 < a.length) throw Error(s(93));
          a = a[0];
        }
        l = a;
      }
      l == null && (l = ""), e = l;
    }
    l = Te(e), t.defaultValue = l, a = t.textContent, a === l && a !== "" && a !== null && (t.value = a), ji(t);
  }
  function za(t, e) {
    if (e) {
      var l = t.firstChild;
      if (l && l === t.lastChild && l.nodeType === 3) {
        l.nodeValue = e;
        return;
      }
    }
    t.textContent = e;
  }
  var Ly = new Set(
    "animationIterationCount aspectRatio borderImageOutset borderImageSlice borderImageWidth boxFlex boxFlexGroup boxOrdinalGroup columnCount columns flex flexGrow flexPositive flexShrink flexNegative flexOrder gridArea gridRow gridRowEnd gridRowSpan gridRowStart gridColumn gridColumnEnd gridColumnSpan gridColumnStart fontWeight lineClamp lineHeight opacity order orphans scale tabSize widows zIndex zoom fillOpacity floodOpacity stopOpacity strokeDasharray strokeDashoffset strokeMiterlimit strokeOpacity strokeWidth MozAnimationIterationCount MozBoxFlex MozBoxFlexGroup MozLineClamp msAnimationIterationCount msFlex msZoom msFlexGrow msFlexNegative msFlexOrder msFlexPositive msFlexShrink msGridColumn msGridColumnSpan msGridRow msGridRowSpan WebkitAnimationIterationCount WebkitBoxFlex WebKitBoxFlexGroup WebkitBoxOrdinalGroup WebkitColumnCount WebkitColumns WebkitFlex WebkitFlexGrow WebkitFlexPositive WebkitFlexShrink WebkitLineClamp".split(
      " "
    )
  );
  function zs(t, e, l) {
    var a = e.indexOf("--") === 0;
    l == null || typeof l == "boolean" || l === "" ? a ? t.setProperty(e, "") : e === "float" ? t.cssFloat = "" : t[e] = "" : a ? t.setProperty(e, l) : typeof l != "number" || l === 0 || Ly.has(e) ? e === "float" ? t.cssFloat = l : t[e] = ("" + l).trim() : t[e] = l + "px";
  }
  function Ts(t, e, l) {
    if (e != null && typeof e != "object")
      throw Error(s(62));
    if (t = t.style, l != null) {
      for (var a in l)
        !l.hasOwnProperty(a) || e != null && e.hasOwnProperty(a) || (a.indexOf("--") === 0 ? t.setProperty(a, "") : a === "float" ? t.cssFloat = "" : t[a] = "");
      for (var u in e)
        a = e[u], e.hasOwnProperty(u) && l[u] !== a && zs(t, u, a);
    } else
      for (var n in e)
        e.hasOwnProperty(n) && zs(t, n, e[n]);
  }
  function Bi(t) {
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
  var Gy = /* @__PURE__ */ new Map([
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
  ]), Qy = /^[\u0000-\u001F ]*j[\r\n\t]*a[\r\n\t]*v[\r\n\t]*a[\r\n\t]*s[\r\n\t]*c[\r\n\t]*r[\r\n\t]*i[\r\n\t]*p[\r\n\t]*t[\r\n\t]*:/i;
  function pn(t) {
    return Qy.test("" + t) ? "javascript:throw new Error('React has blocked a javascript: URL as a security precaution.')" : t;
  }
  function tl() {
  }
  var Yi = null;
  function Li(t) {
    return t = t.target || t.srcElement || window, t.correspondingUseElement && (t = t.correspondingUseElement), t.nodeType === 3 ? t.parentNode : t;
  }
  var Ta = null, xa = null;
  function xs(t) {
    var e = Sa(t);
    if (e && (t = e.stateNode)) {
      var l = t[ce] || null;
      t: switch (t = e.stateNode, e.type) {
        case "input":
          if (Ri(
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
              'input[name="' + xe(
                "" + e
              ) + '"][type="radio"]'
            ), e = 0; e < l.length; e++) {
              var a = l[e];
              if (a !== t && a.form === t.form) {
                var u = a[ce] || null;
                if (!u) throw Error(s(90));
                Ri(
                  a,
                  u.value,
                  u.defaultValue,
                  u.defaultValue,
                  u.checked,
                  u.defaultChecked,
                  u.type,
                  u.name
                );
              }
            }
            for (e = 0; e < l.length; e++)
              a = l[e], a.form === t.form && Ss(a);
          }
          break t;
        case "textarea":
          Es(t, l.value, l.defaultValue);
          break t;
        case "select":
          e = l.value, e != null && Aa(t, !!l.multiple, e, !1);
      }
    }
  }
  var Gi = !1;
  function qs(t, e, l) {
    if (Gi) return t(e, l);
    Gi = !0;
    try {
      var a = t(e);
      return a;
    } finally {
      if (Gi = !1, (Ta !== null || xa !== null) && (ni(), Ta && (e = Ta, t = xa, xa = Ta = null, xs(e), t)))
        for (e = 0; e < t.length; e++) xs(t[e]);
    }
  }
  function gu(t, e) {
    var l = t.stateNode;
    if (l === null) return null;
    var a = l[ce] || null;
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
  var el = !(typeof window > "u" || typeof window.document > "u" || typeof window.document.createElement > "u"), Qi = !1;
  if (el)
    try {
      var pu = {};
      Object.defineProperty(pu, "passive", {
        get: function() {
          Qi = !0;
        }
      }), window.addEventListener("test", pu, pu), window.removeEventListener("test", pu, pu);
    } catch {
      Qi = !1;
    }
  var Al = null, Xi = null, bn = null;
  function Ns() {
    if (bn) return bn;
    var t, e = Xi, l = e.length, a, u = "value" in Al ? Al.value : Al.textContent, n = u.length;
    for (t = 0; t < l && e[t] === u[t]; t++) ;
    var i = l - t;
    for (a = 1; a <= i && e[l - a] === u[n - a]; a++) ;
    return bn = u.slice(t, 1 < a ? 1 - a : void 0);
  }
  function Sn(t) {
    var e = t.keyCode;
    return "charCode" in t ? (t = t.charCode, t === 0 && e === 13 && (t = 13)) : t = e, t === 10 && (t = 13), 32 <= t || t === 13 ? t : 0;
  }
  function _n() {
    return !0;
  }
  function Os() {
    return !1;
  }
  function fe(t) {
    function e(l, a, u, n, i) {
      this._reactName = l, this._targetInst = u, this.type = a, this.nativeEvent = n, this.target = i, this.currentTarget = null;
      for (var o in t)
        t.hasOwnProperty(o) && (l = t[o], this[o] = l ? l(n) : n[o]);
      return this.isDefaultPrevented = (n.defaultPrevented != null ? n.defaultPrevented : n.returnValue === !1) ? _n : Os, this.isPropagationStopped = Os, this;
    }
    return E(e.prototype, {
      preventDefault: function() {
        this.defaultPrevented = !0;
        var l = this.nativeEvent;
        l && (l.preventDefault ? l.preventDefault() : typeof l.returnValue != "unknown" && (l.returnValue = !1), this.isDefaultPrevented = _n);
      },
      stopPropagation: function() {
        var l = this.nativeEvent;
        l && (l.stopPropagation ? l.stopPropagation() : typeof l.cancelBubble != "unknown" && (l.cancelBubble = !0), this.isPropagationStopped = _n);
      },
      persist: function() {
      },
      isPersistent: _n
    }), e;
  }
  var Il = {
    eventPhase: 0,
    bubbles: 0,
    cancelable: 0,
    timeStamp: function(t) {
      return t.timeStamp || Date.now();
    },
    defaultPrevented: 0,
    isTrusted: 0
  }, En = fe(Il), bu = E({}, Il, { view: 0, detail: 0 }), Xy = fe(bu), Zi, Vi, Su, An = E({}, bu, {
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
    getModifierState: Ji,
    button: 0,
    buttons: 0,
    relatedTarget: function(t) {
      return t.relatedTarget === void 0 ? t.fromElement === t.srcElement ? t.toElement : t.fromElement : t.relatedTarget;
    },
    movementX: function(t) {
      return "movementX" in t ? t.movementX : (t !== Su && (Su && t.type === "mousemove" ? (Zi = t.screenX - Su.screenX, Vi = t.screenY - Su.screenY) : Vi = Zi = 0, Su = t), Zi);
    },
    movementY: function(t) {
      return "movementY" in t ? t.movementY : Vi;
    }
  }), Ms = fe(An), Zy = E({}, An, { dataTransfer: 0 }), Vy = fe(Zy), Ky = E({}, bu, { relatedTarget: 0 }), Ki = fe(Ky), Jy = E({}, Il, {
    animationName: 0,
    elapsedTime: 0,
    pseudoElement: 0
  }), wy = fe(Jy), ky = E({}, Il, {
    clipboardData: function(t) {
      return "clipboardData" in t ? t.clipboardData : window.clipboardData;
    }
  }), $y = fe(ky), Wy = E({}, Il, { data: 0 }), Ds = fe(Wy), Fy = {
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
  }, Iy = {
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
  }, Py = {
    Alt: "altKey",
    Control: "ctrlKey",
    Meta: "metaKey",
    Shift: "shiftKey"
  };
  function tm(t) {
    var e = this.nativeEvent;
    return e.getModifierState ? e.getModifierState(t) : (t = Py[t]) ? !!e[t] : !1;
  }
  function Ji() {
    return tm;
  }
  var em = E({}, bu, {
    key: function(t) {
      if (t.key) {
        var e = Fy[t.key] || t.key;
        if (e !== "Unidentified") return e;
      }
      return t.type === "keypress" ? (t = Sn(t), t === 13 ? "Enter" : String.fromCharCode(t)) : t.type === "keydown" || t.type === "keyup" ? Iy[t.keyCode] || "Unidentified" : "";
    },
    code: 0,
    location: 0,
    ctrlKey: 0,
    shiftKey: 0,
    altKey: 0,
    metaKey: 0,
    repeat: 0,
    locale: 0,
    getModifierState: Ji,
    charCode: function(t) {
      return t.type === "keypress" ? Sn(t) : 0;
    },
    keyCode: function(t) {
      return t.type === "keydown" || t.type === "keyup" ? t.keyCode : 0;
    },
    which: function(t) {
      return t.type === "keypress" ? Sn(t) : t.type === "keydown" || t.type === "keyup" ? t.keyCode : 0;
    }
  }), lm = fe(em), am = E({}, An, {
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
  }), Us = fe(am), um = E({}, bu, {
    touches: 0,
    targetTouches: 0,
    changedTouches: 0,
    altKey: 0,
    metaKey: 0,
    ctrlKey: 0,
    shiftKey: 0,
    getModifierState: Ji
  }), nm = fe(um), im = E({}, Il, {
    propertyName: 0,
    elapsedTime: 0,
    pseudoElement: 0
  }), cm = fe(im), fm = E({}, An, {
    deltaX: function(t) {
      return "deltaX" in t ? t.deltaX : "wheelDeltaX" in t ? -t.wheelDeltaX : 0;
    },
    deltaY: function(t) {
      return "deltaY" in t ? t.deltaY : "wheelDeltaY" in t ? -t.wheelDeltaY : "wheelDelta" in t ? -t.wheelDelta : 0;
    },
    deltaZ: 0,
    deltaMode: 0
  }), sm = fe(fm), rm = E({}, Il, {
    newState: 0,
    oldState: 0
  }), om = fe(rm), dm = [9, 13, 27, 32], wi = el && "CompositionEvent" in window, _u = null;
  el && "documentMode" in document && (_u = document.documentMode);
  var ym = el && "TextEvent" in window && !_u, Cs = el && (!wi || _u && 8 < _u && 11 >= _u), js = " ", Rs = !1;
  function Hs(t, e) {
    switch (t) {
      case "keyup":
        return dm.indexOf(e.keyCode) !== -1;
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
  function Bs(t) {
    return t = t.detail, typeof t == "object" && "data" in t ? t.data : null;
  }
  var qa = !1;
  function mm(t, e) {
    switch (t) {
      case "compositionend":
        return Bs(e);
      case "keypress":
        return e.which !== 32 ? null : (Rs = !0, js);
      case "textInput":
        return t = e.data, t === js && Rs ? null : t;
      default:
        return null;
    }
  }
  function hm(t, e) {
    if (qa)
      return t === "compositionend" || !wi && Hs(t, e) ? (t = Ns(), bn = Xi = Al = null, qa = !1, t) : null;
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
        return Cs && e.locale !== "ko" ? null : e.data;
      default:
        return null;
    }
  }
  var vm = {
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
  function Ys(t) {
    var e = t && t.nodeName && t.nodeName.toLowerCase();
    return e === "input" ? !!vm[t.type] : e === "textarea";
  }
  function Ls(t, e, l, a) {
    Ta ? xa ? xa.push(a) : xa = [a] : Ta = a, e = di(e, "onChange"), 0 < e.length && (l = new En(
      "onChange",
      "change",
      null,
      l,
      a
    ), t.push({ event: l, listeners: e }));
  }
  var Eu = null, Au = null;
  function gm(t) {
    Ed(t, 0);
  }
  function zn(t) {
    var e = vu(t);
    if (Ss(e)) return t;
  }
  function Gs(t, e) {
    if (t === "change") return e;
  }
  var Qs = !1;
  if (el) {
    var ki;
    if (el) {
      var $i = "oninput" in document;
      if (!$i) {
        var Xs = document.createElement("div");
        Xs.setAttribute("oninput", "return;"), $i = typeof Xs.oninput == "function";
      }
      ki = $i;
    } else ki = !1;
    Qs = ki && (!document.documentMode || 9 < document.documentMode);
  }
  function Zs() {
    Eu && (Eu.detachEvent("onpropertychange", Vs), Au = Eu = null);
  }
  function Vs(t) {
    if (t.propertyName === "value" && zn(Au)) {
      var e = [];
      Ls(
        e,
        Au,
        t,
        Li(t)
      ), qs(gm, e);
    }
  }
  function pm(t, e, l) {
    t === "focusin" ? (Zs(), Eu = e, Au = l, Eu.attachEvent("onpropertychange", Vs)) : t === "focusout" && Zs();
  }
  function bm(t) {
    if (t === "selectionchange" || t === "keyup" || t === "keydown")
      return zn(Au);
  }
  function Sm(t, e) {
    if (t === "click") return zn(e);
  }
  function _m(t, e) {
    if (t === "input" || t === "change")
      return zn(e);
  }
  function Em(t, e) {
    return t === e && (t !== 0 || 1 / t === 1 / e) || t !== t && e !== e;
  }
  var pe = typeof Object.is == "function" ? Object.is : Em;
  function zu(t, e) {
    if (pe(t, e)) return !0;
    if (typeof t != "object" || t === null || typeof e != "object" || e === null)
      return !1;
    var l = Object.keys(t), a = Object.keys(e);
    if (l.length !== a.length) return !1;
    for (a = 0; a < l.length; a++) {
      var u = l[a];
      if (!Sl.call(e, u) || !pe(t[u], e[u]))
        return !1;
    }
    return !0;
  }
  function Ks(t) {
    for (; t && t.firstChild; ) t = t.firstChild;
    return t;
  }
  function Js(t, e) {
    var l = Ks(t);
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
      l = Ks(l);
    }
  }
  function ws(t, e) {
    return t && e ? t === e ? !0 : t && t.nodeType === 3 ? !1 : e && e.nodeType === 3 ? ws(t, e.parentNode) : "contains" in t ? t.contains(e) : t.compareDocumentPosition ? !!(t.compareDocumentPosition(e) & 16) : !1 : !1;
  }
  function ks(t) {
    t = t != null && t.ownerDocument != null && t.ownerDocument.defaultView != null ? t.ownerDocument.defaultView : window;
    for (var e = gn(t.document); e instanceof t.HTMLIFrameElement; ) {
      try {
        var l = typeof e.contentWindow.location.href == "string";
      } catch {
        l = !1;
      }
      if (l) t = e.contentWindow;
      else break;
      e = gn(t.document);
    }
    return e;
  }
  function Wi(t) {
    var e = t && t.nodeName && t.nodeName.toLowerCase();
    return e && (e === "input" && (t.type === "text" || t.type === "search" || t.type === "tel" || t.type === "url" || t.type === "password") || e === "textarea" || t.contentEditable === "true");
  }
  var Am = el && "documentMode" in document && 11 >= document.documentMode, Na = null, Fi = null, Tu = null, Ii = !1;
  function $s(t, e, l) {
    var a = l.window === l ? l.document : l.nodeType === 9 ? l : l.ownerDocument;
    Ii || Na == null || Na !== gn(a) || (a = Na, "selectionStart" in a && Wi(a) ? a = { start: a.selectionStart, end: a.selectionEnd } : (a = (a.ownerDocument && a.ownerDocument.defaultView || window).getSelection(), a = {
      anchorNode: a.anchorNode,
      anchorOffset: a.anchorOffset,
      focusNode: a.focusNode,
      focusOffset: a.focusOffset
    }), Tu && zu(Tu, a) || (Tu = a, a = di(Fi, "onSelect"), 0 < a.length && (e = new En(
      "onSelect",
      "select",
      null,
      e,
      l
    ), t.push({ event: e, listeners: a }), e.target = Na)));
  }
  function Pl(t, e) {
    var l = {};
    return l[t.toLowerCase()] = e.toLowerCase(), l["Webkit" + t] = "webkit" + e, l["Moz" + t] = "moz" + e, l;
  }
  var Oa = {
    animationend: Pl("Animation", "AnimationEnd"),
    animationiteration: Pl("Animation", "AnimationIteration"),
    animationstart: Pl("Animation", "AnimationStart"),
    transitionrun: Pl("Transition", "TransitionRun"),
    transitionstart: Pl("Transition", "TransitionStart"),
    transitioncancel: Pl("Transition", "TransitionCancel"),
    transitionend: Pl("Transition", "TransitionEnd")
  }, Pi = {}, Ws = {};
  el && (Ws = document.createElement("div").style, "AnimationEvent" in window || (delete Oa.animationend.animation, delete Oa.animationiteration.animation, delete Oa.animationstart.animation), "TransitionEvent" in window || delete Oa.transitionend.transition);
  function ta(t) {
    if (Pi[t]) return Pi[t];
    if (!Oa[t]) return t;
    var e = Oa[t], l;
    for (l in e)
      if (e.hasOwnProperty(l) && l in Ws)
        return Pi[t] = e[l];
    return t;
  }
  var Fs = ta("animationend"), Is = ta("animationiteration"), Ps = ta("animationstart"), zm = ta("transitionrun"), Tm = ta("transitionstart"), xm = ta("transitioncancel"), tr = ta("transitionend"), er = /* @__PURE__ */ new Map(), tc = "abort auxClick beforeToggle cancel canPlay canPlayThrough click close contextMenu copy cut drag dragEnd dragEnter dragExit dragLeave dragOver dragStart drop durationChange emptied encrypted ended error gotPointerCapture input invalid keyDown keyPress keyUp load loadedData loadedMetadata loadStart lostPointerCapture mouseDown mouseMove mouseOut mouseOver mouseUp paste pause play playing pointerCancel pointerDown pointerMove pointerOut pointerOver pointerUp progress rateChange reset resize seeked seeking stalled submit suspend timeUpdate touchCancel touchEnd touchStart volumeChange scroll toggle touchMove waiting wheel".split(
    " "
  );
  tc.push("scrollEnd");
  function Le(t, e) {
    er.set(t, e), Fl(e, [t]);
  }
  var Tn = typeof reportError == "function" ? reportError : function(t) {
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
  }, qe = [], Ma = 0, ec = 0;
  function xn() {
    for (var t = Ma, e = ec = Ma = 0; e < t; ) {
      var l = qe[e];
      qe[e++] = null;
      var a = qe[e];
      qe[e++] = null;
      var u = qe[e];
      qe[e++] = null;
      var n = qe[e];
      if (qe[e++] = null, a !== null && u !== null) {
        var i = a.pending;
        i === null ? u.next = u : (u.next = i.next, i.next = u), a.pending = u;
      }
      n !== 0 && lr(l, u, n);
    }
  }
  function qn(t, e, l, a) {
    qe[Ma++] = t, qe[Ma++] = e, qe[Ma++] = l, qe[Ma++] = a, ec |= a, t.lanes |= a, t = t.alternate, t !== null && (t.lanes |= a);
  }
  function lc(t, e, l, a) {
    return qn(t, e, l, a), Nn(t);
  }
  function ea(t, e) {
    return qn(t, null, null, e), Nn(t);
  }
  function lr(t, e, l) {
    t.lanes |= l;
    var a = t.alternate;
    a !== null && (a.lanes |= l);
    for (var u = !1, n = t.return; n !== null; )
      n.childLanes |= l, a = n.alternate, a !== null && (a.childLanes |= l), n.tag === 22 && (t = n.stateNode, t === null || t._visibility & 1 || (u = !0)), t = n, n = n.return;
    return t.tag === 3 ? (n = t.stateNode, u && e !== null && (u = 31 - ge(l), t = n.hiddenUpdates, a = t[u], a === null ? t[u] = [e] : a.push(e), e.lane = l | 536870912), n) : null;
  }
  function Nn(t) {
    if (50 < wu)
      throw wu = 0, df = null, Error(s(185));
    for (var e = t.return; e !== null; )
      t = e, e = t.return;
    return t.tag === 3 ? t.stateNode : null;
  }
  var Da = {};
  function qm(t, e, l, a) {
    this.tag = t, this.key = l, this.sibling = this.child = this.return = this.stateNode = this.type = this.elementType = null, this.index = 0, this.refCleanup = this.ref = null, this.pendingProps = e, this.dependencies = this.memoizedState = this.updateQueue = this.memoizedProps = null, this.mode = a, this.subtreeFlags = this.flags = 0, this.deletions = null, this.childLanes = this.lanes = 0, this.alternate = null;
  }
  function be(t, e, l, a) {
    return new qm(t, e, l, a);
  }
  function ac(t) {
    return t = t.prototype, !(!t || !t.isReactComponent);
  }
  function ll(t, e) {
    var l = t.alternate;
    return l === null ? (l = be(
      t.tag,
      e,
      t.key,
      t.mode
    ), l.elementType = t.elementType, l.type = t.type, l.stateNode = t.stateNode, l.alternate = t, t.alternate = l) : (l.pendingProps = e, l.type = t.type, l.flags = 0, l.subtreeFlags = 0, l.deletions = null), l.flags = t.flags & 65011712, l.childLanes = t.childLanes, l.lanes = t.lanes, l.child = t.child, l.memoizedProps = t.memoizedProps, l.memoizedState = t.memoizedState, l.updateQueue = t.updateQueue, e = t.dependencies, l.dependencies = e === null ? null : { lanes: e.lanes, firstContext: e.firstContext }, l.sibling = t.sibling, l.index = t.index, l.ref = t.ref, l.refCleanup = t.refCleanup, l;
  }
  function ar(t, e) {
    t.flags &= 65011714;
    var l = t.alternate;
    return l === null ? (t.childLanes = 0, t.lanes = e, t.child = null, t.subtreeFlags = 0, t.memoizedProps = null, t.memoizedState = null, t.updateQueue = null, t.dependencies = null, t.stateNode = null) : (t.childLanes = l.childLanes, t.lanes = l.lanes, t.child = l.child, t.subtreeFlags = 0, t.deletions = null, t.memoizedProps = l.memoizedProps, t.memoizedState = l.memoizedState, t.updateQueue = l.updateQueue, t.type = l.type, e = l.dependencies, t.dependencies = e === null ? null : {
      lanes: e.lanes,
      firstContext: e.firstContext
    }), t;
  }
  function On(t, e, l, a, u, n) {
    var i = 0;
    if (a = t, typeof t == "function") ac(t) && (i = 1);
    else if (typeof t == "string")
      i = Uh(
        t,
        l,
        Y.current
      ) ? 26 : t === "html" || t === "head" || t === "body" ? 27 : 5;
    else
      t: switch (t) {
        case ae:
          return t = be(31, l, e, u), t.elementType = ae, t.lanes = n, t;
        case J:
          return la(l.children, u, n, e);
        case B:
          i = 8, u |= 24;
          break;
        case Q:
          return t = be(12, l, e, u | 2), t.elementType = Q, t.lanes = n, t;
        case Vt:
          return t = be(13, l, e, u), t.elementType = Vt, t.lanes = n, t;
        case ut:
          return t = be(19, l, e, u), t.elementType = ut, t.lanes = n, t;
        default:
          if (typeof t == "object" && t !== null)
            switch (t.$$typeof) {
              case w:
                i = 10;
                break t;
              case xt:
                i = 9;
                break t;
              case nt:
                i = 11;
                break t;
              case it:
                i = 14;
                break t;
              case I:
                i = 16, a = null;
                break t;
            }
          i = 29, l = Error(
            s(130, t === null ? "null" : typeof t, "")
          ), a = null;
      }
    return e = be(i, l, e, u), e.elementType = t, e.type = a, e.lanes = n, e;
  }
  function la(t, e, l, a) {
    return t = be(7, t, a, e), t.lanes = l, t;
  }
  function uc(t, e, l) {
    return t = be(6, t, null, e), t.lanes = l, t;
  }
  function ur(t) {
    var e = be(18, null, null, 0);
    return e.stateNode = t, e;
  }
  function nc(t, e, l) {
    return e = be(
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
  var nr = /* @__PURE__ */ new WeakMap();
  function Ne(t, e) {
    if (typeof t == "object" && t !== null) {
      var l = nr.get(t);
      return l !== void 0 ? l : (e = {
        value: t,
        source: e,
        stack: su(e)
      }, nr.set(t, e), e);
    }
    return {
      value: t,
      source: e,
      stack: su(e)
    };
  }
  var Ua = [], Ca = 0, Mn = null, xu = 0, Oe = [], Me = 0, zl = null, Je = 1, we = "";
  function al(t, e) {
    Ua[Ca++] = xu, Ua[Ca++] = Mn, Mn = t, xu = e;
  }
  function ir(t, e, l) {
    Oe[Me++] = Je, Oe[Me++] = we, Oe[Me++] = zl, zl = t;
    var a = Je;
    t = we;
    var u = 32 - ge(a) - 1;
    a &= ~(1 << u), l += 1;
    var n = 32 - ge(e) + u;
    if (30 < n) {
      var i = u - u % 5;
      n = (a & (1 << i) - 1).toString(32), a >>= i, u -= i, Je = 1 << 32 - ge(e) + u | l << u | a, we = n + t;
    } else
      Je = 1 << n | l << u | a, we = t;
  }
  function ic(t) {
    t.return !== null && (al(t, 1), ir(t, 1, 0));
  }
  function cc(t) {
    for (; t === Mn; )
      Mn = Ua[--Ca], Ua[Ca] = null, xu = Ua[--Ca], Ua[Ca] = null;
    for (; t === zl; )
      zl = Oe[--Me], Oe[Me] = null, we = Oe[--Me], Oe[Me] = null, Je = Oe[--Me], Oe[Me] = null;
  }
  function cr(t, e) {
    Oe[Me++] = Je, Oe[Me++] = we, Oe[Me++] = zl, Je = e.id, we = e.overflow, zl = t;
  }
  var Wt = null, Nt = null, ot = !1, Tl = null, De = !1, fc = Error(s(519));
  function xl(t) {
    var e = Error(
      s(
        418,
        1 < arguments.length && arguments[1] !== void 0 && arguments[1] ? "text" : "HTML",
        ""
      )
    );
    throw qu(Ne(e, t)), fc;
  }
  function fr(t) {
    var e = t.stateNode, l = t.type, a = t.memoizedProps;
    switch (e[$t] = t, e[ce] = a, l) {
      case "dialog":
        ft("cancel", e), ft("close", e);
        break;
      case "iframe":
      case "object":
      case "embed":
        ft("load", e);
        break;
      case "video":
      case "audio":
        for (l = 0; l < $u.length; l++)
          ft($u[l], e);
        break;
      case "source":
        ft("error", e);
        break;
      case "img":
      case "image":
      case "link":
        ft("error", e), ft("load", e);
        break;
      case "details":
        ft("toggle", e);
        break;
      case "input":
        ft("invalid", e), _s(
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
        ft("invalid", e);
        break;
      case "textarea":
        ft("invalid", e), As(e, a.value, a.defaultValue, a.children);
    }
    l = a.children, typeof l != "string" && typeof l != "number" && typeof l != "bigint" || e.textContent === "" + l || a.suppressHydrationWarning === !0 || xd(e.textContent, l) ? (a.popover != null && (ft("beforetoggle", e), ft("toggle", e)), a.onScroll != null && ft("scroll", e), a.onScrollEnd != null && ft("scrollend", e), a.onClick != null && (e.onclick = tl), e = !0) : e = !1, e || xl(t, !0);
  }
  function sr(t) {
    for (Wt = t.return; Wt; )
      switch (Wt.tag) {
        case 5:
        case 31:
        case 13:
          De = !1;
          return;
        case 27:
        case 3:
          De = !0;
          return;
        default:
          Wt = Wt.return;
      }
  }
  function ja(t) {
    if (t !== Wt) return !1;
    if (!ot) return sr(t), ot = !0, !1;
    var e = t.tag, l;
    if ((l = e !== 3 && e !== 27) && ((l = e === 5) && (l = t.type, l = !(l !== "form" && l !== "button") || qf(t.type, t.memoizedProps)), l = !l), l && Nt && xl(t), sr(t), e === 13) {
      if (t = t.memoizedState, t = t !== null ? t.dehydrated : null, !t) throw Error(s(317));
      Nt = Rd(t);
    } else if (e === 31) {
      if (t = t.memoizedState, t = t !== null ? t.dehydrated : null, !t) throw Error(s(317));
      Nt = Rd(t);
    } else
      e === 27 ? (e = Nt, Gl(t.type) ? (t = Uf, Uf = null, Nt = t) : Nt = e) : Nt = Wt ? Ce(t.stateNode.nextSibling) : null;
    return !0;
  }
  function aa() {
    Nt = Wt = null, ot = !1;
  }
  function sc() {
    var t = Tl;
    return t !== null && (de === null ? de = t : de.push.apply(
      de,
      t
    ), Tl = null), t;
  }
  function qu(t) {
    Tl === null ? Tl = [t] : Tl.push(t);
  }
  var rc = m(null), ua = null, ul = null;
  function ql(t, e, l) {
    R(rc, e._currentValue), e._currentValue = l;
  }
  function nl(t) {
    t._currentValue = rc.current, O(rc);
  }
  function oc(t, e, l) {
    for (; t !== null; ) {
      var a = t.alternate;
      if ((t.childLanes & e) !== e ? (t.childLanes |= e, a !== null && (a.childLanes |= e)) : a !== null && (a.childLanes & e) !== e && (a.childLanes |= e), t === l) break;
      t = t.return;
    }
  }
  function dc(t, e, l, a) {
    var u = t.child;
    for (u !== null && (u.return = t); u !== null; ) {
      var n = u.dependencies;
      if (n !== null) {
        var i = u.child;
        n = n.firstContext;
        t: for (; n !== null; ) {
          var o = n;
          n = u;
          for (var d = 0; d < e.length; d++)
            if (o.context === e[d]) {
              n.lanes |= l, o = n.alternate, o !== null && (o.lanes |= l), oc(
                n.return,
                l,
                t
              ), a || (i = null);
              break t;
            }
          n = o.next;
        }
      } else if (u.tag === 18) {
        if (i = u.return, i === null) throw Error(s(341));
        i.lanes |= l, n = i.alternate, n !== null && (n.lanes |= l), oc(i, l, t), i = null;
      } else i = u.child;
      if (i !== null) i.return = u;
      else
        for (i = u; i !== null; ) {
          if (i === t) {
            i = null;
            break;
          }
          if (u = i.sibling, u !== null) {
            u.return = i.return, i = u;
            break;
          }
          i = i.return;
        }
      u = i;
    }
  }
  function Ra(t, e, l, a) {
    t = null;
    for (var u = e, n = !1; u !== null; ) {
      if (!n) {
        if ((u.flags & 524288) !== 0) n = !0;
        else if ((u.flags & 262144) !== 0) break;
      }
      if (u.tag === 10) {
        var i = u.alternate;
        if (i === null) throw Error(s(387));
        if (i = i.memoizedProps, i !== null) {
          var o = u.type;
          pe(u.pendingProps.value, i.value) || (t !== null ? t.push(o) : t = [o]);
        }
      } else if (u === dt.current) {
        if (i = u.alternate, i === null) throw Error(s(387));
        i.memoizedState.memoizedState !== u.memoizedState.memoizedState && (t !== null ? t.push(tn) : t = [tn]);
      }
      u = u.return;
    }
    t !== null && dc(
      e,
      t,
      l,
      a
    ), e.flags |= 262144;
  }
  function Dn(t) {
    for (t = t.firstContext; t !== null; ) {
      if (!pe(
        t.context._currentValue,
        t.memoizedValue
      ))
        return !0;
      t = t.next;
    }
    return !1;
  }
  function na(t) {
    ua = t, ul = null, t = t.dependencies, t !== null && (t.firstContext = null);
  }
  function Ft(t) {
    return rr(ua, t);
  }
  function Un(t, e) {
    return ua === null && na(t), rr(t, e);
  }
  function rr(t, e) {
    var l = e._currentValue;
    if (e = { context: e, memoizedValue: l, next: null }, ul === null) {
      if (t === null) throw Error(s(308));
      ul = e, t.dependencies = { lanes: 0, firstContext: e }, t.flags |= 524288;
    } else ul = ul.next = e;
    return l;
  }
  var Nm = typeof AbortController < "u" ? AbortController : function() {
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
  }, Om = c.unstable_scheduleCallback, Mm = c.unstable_NormalPriority, Gt = {
    $$typeof: w,
    Consumer: null,
    Provider: null,
    _currentValue: null,
    _currentValue2: null,
    _threadCount: 0
  };
  function yc() {
    return {
      controller: new Nm(),
      data: /* @__PURE__ */ new Map(),
      refCount: 0
    };
  }
  function Nu(t) {
    t.refCount--, t.refCount === 0 && Om(Mm, function() {
      t.controller.abort();
    });
  }
  var Ou = null, mc = 0, Ha = 0, Ba = null;
  function Dm(t, e) {
    if (Ou === null) {
      var l = Ou = [];
      mc = 0, Ha = pf(), Ba = {
        status: "pending",
        value: void 0,
        then: function(a) {
          l.push(a);
        }
      };
    }
    return mc++, e.then(or, or), e;
  }
  function or() {
    if (--mc === 0 && Ou !== null) {
      Ba !== null && (Ba.status = "fulfilled");
      var t = Ou;
      Ou = null, Ha = 0, Ba = null;
      for (var e = 0; e < t.length; e++) (0, t[e])();
    }
  }
  function Um(t, e) {
    var l = [], a = {
      status: "pending",
      value: null,
      reason: null,
      then: function(u) {
        l.push(u);
      }
    };
    return t.then(
      function() {
        a.status = "fulfilled", a.value = e;
        for (var u = 0; u < l.length; u++) (0, l[u])(e);
      },
      function(u) {
        for (a.status = "rejected", a.reason = u, u = 0; u < l.length; u++)
          (0, l[u])(void 0);
      }
    ), a;
  }
  var dr = q.S;
  q.S = function(t, e) {
    Wo = bt(), typeof e == "object" && e !== null && typeof e.then == "function" && Dm(t, e), dr !== null && dr(t, e);
  };
  var ia = m(null);
  function hc() {
    var t = ia.current;
    return t !== null ? t : qt.pooledCache;
  }
  function Cn(t, e) {
    e === null ? R(ia, ia.current) : R(ia, e.pool);
  }
  function yr() {
    var t = hc();
    return t === null ? null : { parent: Gt._currentValue, pool: t };
  }
  var Ya = Error(s(460)), vc = Error(s(474)), jn = Error(s(542)), Rn = { then: function() {
  } };
  function mr(t) {
    return t = t.status, t === "fulfilled" || t === "rejected";
  }
  function hr(t, e, l) {
    switch (l = t[l], l === void 0 ? t.push(e) : l !== e && (e.then(tl, tl), e = l), e.status) {
      case "fulfilled":
        return e.value;
      case "rejected":
        throw t = e.reason, gr(t), t;
      default:
        if (typeof e.status == "string") e.then(tl, tl);
        else {
          if (t = qt, t !== null && 100 < t.shellSuspendCounter)
            throw Error(s(482));
          t = e, t.status = "pending", t.then(
            function(a) {
              if (e.status === "pending") {
                var u = e;
                u.status = "fulfilled", u.value = a;
              }
            },
            function(a) {
              if (e.status === "pending") {
                var u = e;
                u.status = "rejected", u.reason = a;
              }
            }
          );
        }
        switch (e.status) {
          case "fulfilled":
            return e.value;
          case "rejected":
            throw t = e.reason, gr(t), t;
        }
        throw fa = e, Ya;
    }
  }
  function ca(t) {
    try {
      var e = t._init;
      return e(t._payload);
    } catch (l) {
      throw l !== null && typeof l == "object" && typeof l.then == "function" ? (fa = l, Ya) : l;
    }
  }
  var fa = null;
  function vr() {
    if (fa === null) throw Error(s(459));
    var t = fa;
    return fa = null, t;
  }
  function gr(t) {
    if (t === Ya || t === jn)
      throw Error(s(483));
  }
  var La = null, Mu = 0;
  function Hn(t) {
    var e = Mu;
    return Mu += 1, La === null && (La = []), hr(La, t, e);
  }
  function Du(t, e) {
    e = e.props.ref, t.ref = e !== void 0 ? e : null;
  }
  function Bn(t, e) {
    throw e.$$typeof === C ? Error(s(525)) : (t = Object.prototype.toString.call(e), Error(
      s(
        31,
        t === "[object Object]" ? "object with keys {" + Object.keys(e).join(", ") + "}" : t
      )
    ));
  }
  function pr(t) {
    function e(v, y) {
      if (t) {
        var b = v.deletions;
        b === null ? (v.deletions = [y], v.flags |= 16) : b.push(y);
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
    function u(v, y) {
      return v = ll(v, y), v.index = 0, v.sibling = null, v;
    }
    function n(v, y, b) {
      return v.index = b, t ? (b = v.alternate, b !== null ? (b = b.index, b < y ? (v.flags |= 67108866, y) : b) : (v.flags |= 67108866, y)) : (v.flags |= 1048576, y);
    }
    function i(v) {
      return t && v.alternate === null && (v.flags |= 67108866), v;
    }
    function o(v, y, b, M) {
      return y === null || y.tag !== 6 ? (y = uc(b, v.mode, M), y.return = v, y) : (y = u(y, b), y.return = v, y);
    }
    function d(v, y, b, M) {
      var k = b.type;
      return k === J ? T(
        v,
        y,
        b.props.children,
        M,
        b.key
      ) : y !== null && (y.elementType === k || typeof k == "object" && k !== null && k.$$typeof === I && ca(k) === y.type) ? (y = u(y, b.props), Du(y, b), y.return = v, y) : (y = On(
        b.type,
        b.key,
        b.props,
        null,
        v.mode,
        M
      ), Du(y, b), y.return = v, y);
    }
    function S(v, y, b, M) {
      return y === null || y.tag !== 4 || y.stateNode.containerInfo !== b.containerInfo || y.stateNode.implementation !== b.implementation ? (y = nc(b, v.mode, M), y.return = v, y) : (y = u(y, b.children || []), y.return = v, y);
    }
    function T(v, y, b, M, k) {
      return y === null || y.tag !== 7 ? (y = la(
        b,
        v.mode,
        M,
        k
      ), y.return = v, y) : (y = u(y, b), y.return = v, y);
    }
    function U(v, y, b) {
      if (typeof y == "string" && y !== "" || typeof y == "number" || typeof y == "bigint")
        return y = uc(
          "" + y,
          v.mode,
          b
        ), y.return = v, y;
      if (typeof y == "object" && y !== null) {
        switch (y.$$typeof) {
          case G:
            return b = On(
              y.type,
              y.key,
              y.props,
              null,
              v.mode,
              b
            ), Du(b, y), b.return = v, b;
          case K:
            return y = nc(
              y,
              v.mode,
              b
            ), y.return = v, y;
          case I:
            return y = ca(y), U(v, y, b);
        }
        if (ue(y) || Yt(y))
          return y = la(
            y,
            v.mode,
            b,
            null
          ), y.return = v, y;
        if (typeof y.then == "function")
          return U(v, Hn(y), b);
        if (y.$$typeof === w)
          return U(
            v,
            Un(v, y),
            b
          );
        Bn(v, y);
      }
      return null;
    }
    function _(v, y, b, M) {
      var k = y !== null ? y.key : null;
      if (typeof b == "string" && b !== "" || typeof b == "number" || typeof b == "bigint")
        return k !== null ? null : o(v, y, "" + b, M);
      if (typeof b == "object" && b !== null) {
        switch (b.$$typeof) {
          case G:
            return b.key === k ? d(v, y, b, M) : null;
          case K:
            return b.key === k ? S(v, y, b, M) : null;
          case I:
            return b = ca(b), _(v, y, b, M);
        }
        if (ue(b) || Yt(b))
          return k !== null ? null : T(v, y, b, M, null);
        if (typeof b.then == "function")
          return _(
            v,
            y,
            Hn(b),
            M
          );
        if (b.$$typeof === w)
          return _(
            v,
            y,
            Un(v, b),
            M
          );
        Bn(v, b);
      }
      return null;
    }
    function A(v, y, b, M, k) {
      if (typeof M == "string" && M !== "" || typeof M == "number" || typeof M == "bigint")
        return v = v.get(b) || null, o(y, v, "" + M, k);
      if (typeof M == "object" && M !== null) {
        switch (M.$$typeof) {
          case G:
            return v = v.get(
              M.key === null ? b : M.key
            ) || null, d(y, v, M, k);
          case K:
            return v = v.get(
              M.key === null ? b : M.key
            ) || null, S(y, v, M, k);
          case I:
            return M = ca(M), A(
              v,
              y,
              b,
              M,
              k
            );
        }
        if (ue(M) || Yt(M))
          return v = v.get(b) || null, T(y, v, M, k, null);
        if (typeof M.then == "function")
          return A(
            v,
            y,
            b,
            Hn(M),
            k
          );
        if (M.$$typeof === w)
          return A(
            v,
            y,
            b,
            Un(y, M),
            k
          );
        Bn(y, M);
      }
      return null;
    }
    function L(v, y, b, M) {
      for (var k = null, mt = null, X = y, at = y = 0, rt = null; X !== null && at < b.length; at++) {
        X.index > at ? (rt = X, X = null) : rt = X.sibling;
        var ht = _(
          v,
          X,
          b[at],
          M
        );
        if (ht === null) {
          X === null && (X = rt);
          break;
        }
        t && X && ht.alternate === null && e(v, X), y = n(ht, y, at), mt === null ? k = ht : mt.sibling = ht, mt = ht, X = rt;
      }
      if (at === b.length)
        return l(v, X), ot && al(v, at), k;
      if (X === null) {
        for (; at < b.length; at++)
          X = U(v, b[at], M), X !== null && (y = n(
            X,
            y,
            at
          ), mt === null ? k = X : mt.sibling = X, mt = X);
        return ot && al(v, at), k;
      }
      for (X = a(X); at < b.length; at++)
        rt = A(
          X,
          v,
          at,
          b[at],
          M
        ), rt !== null && (t && rt.alternate !== null && X.delete(
          rt.key === null ? at : rt.key
        ), y = n(
          rt,
          y,
          at
        ), mt === null ? k = rt : mt.sibling = rt, mt = rt);
      return t && X.forEach(function(Kl) {
        return e(v, Kl);
      }), ot && al(v, at), k;
    }
    function $(v, y, b, M) {
      if (b == null) throw Error(s(151));
      for (var k = null, mt = null, X = y, at = y = 0, rt = null, ht = b.next(); X !== null && !ht.done; at++, ht = b.next()) {
        X.index > at ? (rt = X, X = null) : rt = X.sibling;
        var Kl = _(v, X, ht.value, M);
        if (Kl === null) {
          X === null && (X = rt);
          break;
        }
        t && X && Kl.alternate === null && e(v, X), y = n(Kl, y, at), mt === null ? k = Kl : mt.sibling = Kl, mt = Kl, X = rt;
      }
      if (ht.done)
        return l(v, X), ot && al(v, at), k;
      if (X === null) {
        for (; !ht.done; at++, ht = b.next())
          ht = U(v, ht.value, M), ht !== null && (y = n(ht, y, at), mt === null ? k = ht : mt.sibling = ht, mt = ht);
        return ot && al(v, at), k;
      }
      for (X = a(X); !ht.done; at++, ht = b.next())
        ht = A(X, v, at, ht.value, M), ht !== null && (t && ht.alternate !== null && X.delete(ht.key === null ? at : ht.key), y = n(ht, y, at), mt === null ? k = ht : mt.sibling = ht, mt = ht);
      return t && X.forEach(function(Zh) {
        return e(v, Zh);
      }), ot && al(v, at), k;
    }
    function Tt(v, y, b, M) {
      if (typeof b == "object" && b !== null && b.type === J && b.key === null && (b = b.props.children), typeof b == "object" && b !== null) {
        switch (b.$$typeof) {
          case G:
            t: {
              for (var k = b.key; y !== null; ) {
                if (y.key === k) {
                  if (k = b.type, k === J) {
                    if (y.tag === 7) {
                      l(
                        v,
                        y.sibling
                      ), M = u(
                        y,
                        b.props.children
                      ), M.return = v, v = M;
                      break t;
                    }
                  } else if (y.elementType === k || typeof k == "object" && k !== null && k.$$typeof === I && ca(k) === y.type) {
                    l(
                      v,
                      y.sibling
                    ), M = u(y, b.props), Du(M, b), M.return = v, v = M;
                    break t;
                  }
                  l(v, y);
                  break;
                } else e(v, y);
                y = y.sibling;
              }
              b.type === J ? (M = la(
                b.props.children,
                v.mode,
                M,
                b.key
              ), M.return = v, v = M) : (M = On(
                b.type,
                b.key,
                b.props,
                null,
                v.mode,
                M
              ), Du(M, b), M.return = v, v = M);
            }
            return i(v);
          case K:
            t: {
              for (k = b.key; y !== null; ) {
                if (y.key === k)
                  if (y.tag === 4 && y.stateNode.containerInfo === b.containerInfo && y.stateNode.implementation === b.implementation) {
                    l(
                      v,
                      y.sibling
                    ), M = u(y, b.children || []), M.return = v, v = M;
                    break t;
                  } else {
                    l(v, y);
                    break;
                  }
                else e(v, y);
                y = y.sibling;
              }
              M = nc(b, v.mode, M), M.return = v, v = M;
            }
            return i(v);
          case I:
            return b = ca(b), Tt(
              v,
              y,
              b,
              M
            );
        }
        if (ue(b))
          return L(
            v,
            y,
            b,
            M
          );
        if (Yt(b)) {
          if (k = Yt(b), typeof k != "function") throw Error(s(150));
          return b = k.call(b), $(
            v,
            y,
            b,
            M
          );
        }
        if (typeof b.then == "function")
          return Tt(
            v,
            y,
            Hn(b),
            M
          );
        if (b.$$typeof === w)
          return Tt(
            v,
            y,
            Un(v, b),
            M
          );
        Bn(v, b);
      }
      return typeof b == "string" && b !== "" || typeof b == "number" || typeof b == "bigint" ? (b = "" + b, y !== null && y.tag === 6 ? (l(v, y.sibling), M = u(y, b), M.return = v, v = M) : (l(v, y), M = uc(b, v.mode, M), M.return = v, v = M), i(v)) : l(v, y);
    }
    return function(v, y, b, M) {
      try {
        Mu = 0;
        var k = Tt(
          v,
          y,
          b,
          M
        );
        return La = null, k;
      } catch (X) {
        if (X === Ya || X === jn) throw X;
        var mt = be(29, X, null, v.mode);
        return mt.lanes = M, mt.return = v, mt;
      } finally {
      }
    };
  }
  var sa = pr(!0), br = pr(!1), Nl = !1;
  function gc(t) {
    t.updateQueue = {
      baseState: t.memoizedState,
      firstBaseUpdate: null,
      lastBaseUpdate: null,
      shared: { pending: null, lanes: 0, hiddenCallbacks: null },
      callbacks: null
    };
  }
  function pc(t, e) {
    t = t.updateQueue, e.updateQueue === t && (e.updateQueue = {
      baseState: t.baseState,
      firstBaseUpdate: t.firstBaseUpdate,
      lastBaseUpdate: t.lastBaseUpdate,
      shared: t.shared,
      callbacks: null
    });
  }
  function Ol(t) {
    return { lane: t, tag: 0, payload: null, callback: null, next: null };
  }
  function Ml(t, e, l) {
    var a = t.updateQueue;
    if (a === null) return null;
    if (a = a.shared, (vt & 2) !== 0) {
      var u = a.pending;
      return u === null ? e.next = e : (e.next = u.next, u.next = e), a.pending = e, e = Nn(t), lr(t, null, l), e;
    }
    return qn(t, a, e, l), Nn(t);
  }
  function Uu(t, e, l) {
    if (e = e.updateQueue, e !== null && (e = e.shared, (l & 4194048) !== 0)) {
      var a = e.lanes;
      a &= t.pendingLanes, l |= a, e.lanes = l, rs(t, l);
    }
  }
  function bc(t, e) {
    var l = t.updateQueue, a = t.alternate;
    if (a !== null && (a = a.updateQueue, l === a)) {
      var u = null, n = null;
      if (l = l.firstBaseUpdate, l !== null) {
        do {
          var i = {
            lane: l.lane,
            tag: l.tag,
            payload: l.payload,
            callback: null,
            next: null
          };
          n === null ? u = n = i : n = n.next = i, l = l.next;
        } while (l !== null);
        n === null ? u = n = e : n = n.next = e;
      } else u = n = e;
      l = {
        baseState: a.baseState,
        firstBaseUpdate: u,
        lastBaseUpdate: n,
        shared: a.shared,
        callbacks: a.callbacks
      }, t.updateQueue = l;
      return;
    }
    t = l.lastBaseUpdate, t === null ? l.firstBaseUpdate = e : t.next = e, l.lastBaseUpdate = e;
  }
  var Sc = !1;
  function Cu() {
    if (Sc) {
      var t = Ba;
      if (t !== null) throw t;
    }
  }
  function ju(t, e, l, a) {
    Sc = !1;
    var u = t.updateQueue;
    Nl = !1;
    var n = u.firstBaseUpdate, i = u.lastBaseUpdate, o = u.shared.pending;
    if (o !== null) {
      u.shared.pending = null;
      var d = o, S = d.next;
      d.next = null, i === null ? n = S : i.next = S, i = d;
      var T = t.alternate;
      T !== null && (T = T.updateQueue, o = T.lastBaseUpdate, o !== i && (o === null ? T.firstBaseUpdate = S : o.next = S, T.lastBaseUpdate = d));
    }
    if (n !== null) {
      var U = u.baseState;
      i = 0, T = S = d = null, o = n;
      do {
        var _ = o.lane & -536870913, A = _ !== o.lane;
        if (A ? (st & _) === _ : (a & _) === _) {
          _ !== 0 && _ === Ha && (Sc = !0), T !== null && (T = T.next = {
            lane: 0,
            tag: o.tag,
            payload: o.payload,
            callback: null,
            next: null
          });
          t: {
            var L = t, $ = o;
            _ = e;
            var Tt = l;
            switch ($.tag) {
              case 1:
                if (L = $.payload, typeof L == "function") {
                  U = L.call(Tt, U, _);
                  break t;
                }
                U = L;
                break t;
              case 3:
                L.flags = L.flags & -65537 | 128;
              case 0:
                if (L = $.payload, _ = typeof L == "function" ? L.call(Tt, U, _) : L, _ == null) break t;
                U = E({}, U, _);
                break t;
              case 2:
                Nl = !0;
            }
          }
          _ = o.callback, _ !== null && (t.flags |= 64, A && (t.flags |= 8192), A = u.callbacks, A === null ? u.callbacks = [_] : A.push(_));
        } else
          A = {
            lane: _,
            tag: o.tag,
            payload: o.payload,
            callback: o.callback,
            next: null
          }, T === null ? (S = T = A, d = U) : T = T.next = A, i |= _;
        if (o = o.next, o === null) {
          if (o = u.shared.pending, o === null)
            break;
          A = o, o = A.next, A.next = null, u.lastBaseUpdate = A, u.shared.pending = null;
        }
      } while (!0);
      T === null && (d = U), u.baseState = d, u.firstBaseUpdate = S, u.lastBaseUpdate = T, n === null && (u.shared.lanes = 0), Rl |= i, t.lanes = i, t.memoizedState = U;
    }
  }
  function Sr(t, e) {
    if (typeof t != "function")
      throw Error(s(191, t));
    t.call(e);
  }
  function _r(t, e) {
    var l = t.callbacks;
    if (l !== null)
      for (t.callbacks = null, t = 0; t < l.length; t++)
        Sr(l[t], e);
  }
  var Ga = m(null), Yn = m(0);
  function Er(t, e) {
    t = ml, R(Yn, t), R(Ga, e), ml = t | e.baseLanes;
  }
  function _c() {
    R(Yn, ml), R(Ga, Ga.current);
  }
  function Ec() {
    ml = Yn.current, O(Ga), O(Yn);
  }
  var Se = m(null), Ue = null;
  function Dl(t) {
    var e = t.alternate;
    R(Ht, Ht.current & 1), R(Se, t), Ue === null && (e === null || Ga.current !== null || e.memoizedState !== null) && (Ue = t);
  }
  function Ac(t) {
    R(Ht, Ht.current), R(Se, t), Ue === null && (Ue = t);
  }
  function Ar(t) {
    t.tag === 22 ? (R(Ht, Ht.current), R(Se, t), Ue === null && (Ue = t)) : Ul();
  }
  function Ul() {
    R(Ht, Ht.current), R(Se, Se.current);
  }
  function _e(t) {
    O(Se), Ue === t && (Ue = null), O(Ht);
  }
  var Ht = m(0);
  function Ln(t) {
    for (var e = t; e !== null; ) {
      if (e.tag === 13) {
        var l = e.memoizedState;
        if (l !== null && (l = l.dehydrated, l === null || Mf(l) || Df(l)))
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
  var il = 0, lt = null, At = null, Qt = null, Gn = !1, Qa = !1, ra = !1, Qn = 0, Ru = 0, Xa = null, Cm = 0;
  function Ct() {
    throw Error(s(321));
  }
  function zc(t, e) {
    if (e === null) return !1;
    for (var l = 0; l < e.length && l < t.length; l++)
      if (!pe(t[l], e[l])) return !1;
    return !0;
  }
  function Tc(t, e, l, a, u, n) {
    return il = n, lt = e, e.memoizedState = null, e.updateQueue = null, e.lanes = 0, q.H = t === null || t.memoizedState === null ? io : Gc, ra = !1, n = l(a, u), ra = !1, Qa && (n = Tr(
      e,
      l,
      a,
      u
    )), zr(t), n;
  }
  function zr(t) {
    q.H = Yu;
    var e = At !== null && At.next !== null;
    if (il = 0, Qt = At = lt = null, Gn = !1, Ru = 0, Xa = null, e) throw Error(s(300));
    t === null || Xt || (t = t.dependencies, t !== null && Dn(t) && (Xt = !0));
  }
  function Tr(t, e, l, a) {
    lt = t;
    var u = 0;
    do {
      if (Qa && (Xa = null), Ru = 0, Qa = !1, 25 <= u) throw Error(s(301));
      if (u += 1, Qt = At = null, t.updateQueue != null) {
        var n = t.updateQueue;
        n.lastEffect = null, n.events = null, n.stores = null, n.memoCache != null && (n.memoCache.index = 0);
      }
      q.H = co, n = e(l, a);
    } while (Qa);
    return n;
  }
  function jm() {
    var t = q.H, e = t.useState()[0];
    return e = typeof e.then == "function" ? Hu(e) : e, t = t.useState()[0], (At !== null ? At.memoizedState : null) !== t && (lt.flags |= 1024), e;
  }
  function xc() {
    var t = Qn !== 0;
    return Qn = 0, t;
  }
  function qc(t, e, l) {
    e.updateQueue = t.updateQueue, e.flags &= -2053, t.lanes &= ~l;
  }
  function Nc(t) {
    if (Gn) {
      for (t = t.memoizedState; t !== null; ) {
        var e = t.queue;
        e !== null && (e.pending = null), t = t.next;
      }
      Gn = !1;
    }
    il = 0, Qt = At = lt = null, Qa = !1, Ru = Qn = 0, Xa = null;
  }
  function ne() {
    var t = {
      memoizedState: null,
      baseState: null,
      baseQueue: null,
      queue: null,
      next: null
    };
    return Qt === null ? lt.memoizedState = Qt = t : Qt = Qt.next = t, Qt;
  }
  function Bt() {
    if (At === null) {
      var t = lt.alternate;
      t = t !== null ? t.memoizedState : null;
    } else t = At.next;
    var e = Qt === null ? lt.memoizedState : Qt.next;
    if (e !== null)
      Qt = e, At = t;
    else {
      if (t === null)
        throw lt.alternate === null ? Error(s(467)) : Error(s(310));
      At = t, t = {
        memoizedState: At.memoizedState,
        baseState: At.baseState,
        baseQueue: At.baseQueue,
        queue: At.queue,
        next: null
      }, Qt === null ? lt.memoizedState = Qt = t : Qt = Qt.next = t;
    }
    return Qt;
  }
  function Xn() {
    return { lastEffect: null, events: null, stores: null, memoCache: null };
  }
  function Hu(t) {
    var e = Ru;
    return Ru += 1, Xa === null && (Xa = []), t = hr(Xa, t, e), e = lt, (Qt === null ? e.memoizedState : Qt.next) === null && (e = e.alternate, q.H = e === null || e.memoizedState === null ? io : Gc), t;
  }
  function Zn(t) {
    if (t !== null && typeof t == "object") {
      if (typeof t.then == "function") return Hu(t);
      if (t.$$typeof === w) return Ft(t);
    }
    throw Error(s(438, String(t)));
  }
  function Oc(t) {
    var e = null, l = lt.updateQueue;
    if (l !== null && (e = l.memoCache), e == null) {
      var a = lt.alternate;
      a !== null && (a = a.updateQueue, a !== null && (a = a.memoCache, a != null && (e = {
        data: a.data.map(function(u) {
          return u.slice();
        }),
        index: 0
      })));
    }
    if (e == null && (e = { data: [], index: 0 }), l === null && (l = Xn(), lt.updateQueue = l), l.memoCache = e, l = e.data[e.index], l === void 0)
      for (l = e.data[e.index] = Array(t), a = 0; a < t; a++)
        l[a] = Be;
    return e.index++, l;
  }
  function cl(t, e) {
    return typeof e == "function" ? e(t) : e;
  }
  function Vn(t) {
    var e = Bt();
    return Mc(e, At, t);
  }
  function Mc(t, e, l) {
    var a = t.queue;
    if (a === null) throw Error(s(311));
    a.lastRenderedReducer = l;
    var u = t.baseQueue, n = a.pending;
    if (n !== null) {
      if (u !== null) {
        var i = u.next;
        u.next = n.next, n.next = i;
      }
      e.baseQueue = u = n, a.pending = null;
    }
    if (n = t.baseState, u === null) t.memoizedState = n;
    else {
      e = u.next;
      var o = i = null, d = null, S = e, T = !1;
      do {
        var U = S.lane & -536870913;
        if (U !== S.lane ? (st & U) === U : (il & U) === U) {
          var _ = S.revertLane;
          if (_ === 0)
            d !== null && (d = d.next = {
              lane: 0,
              revertLane: 0,
              gesture: null,
              action: S.action,
              hasEagerState: S.hasEagerState,
              eagerState: S.eagerState,
              next: null
            }), U === Ha && (T = !0);
          else if ((il & _) === _) {
            S = S.next, _ === Ha && (T = !0);
            continue;
          } else
            U = {
              lane: 0,
              revertLane: S.revertLane,
              gesture: null,
              action: S.action,
              hasEagerState: S.hasEagerState,
              eagerState: S.eagerState,
              next: null
            }, d === null ? (o = d = U, i = n) : d = d.next = U, lt.lanes |= _, Rl |= _;
          U = S.action, ra && l(n, U), n = S.hasEagerState ? S.eagerState : l(n, U);
        } else
          _ = {
            lane: U,
            revertLane: S.revertLane,
            gesture: S.gesture,
            action: S.action,
            hasEagerState: S.hasEagerState,
            eagerState: S.eagerState,
            next: null
          }, d === null ? (o = d = _, i = n) : d = d.next = _, lt.lanes |= U, Rl |= U;
        S = S.next;
      } while (S !== null && S !== e);
      if (d === null ? i = n : d.next = o, !pe(n, t.memoizedState) && (Xt = !0, T && (l = Ba, l !== null)))
        throw l;
      t.memoizedState = n, t.baseState = i, t.baseQueue = d, a.lastRenderedState = n;
    }
    return u === null && (a.lanes = 0), [t.memoizedState, a.dispatch];
  }
  function Dc(t) {
    var e = Bt(), l = e.queue;
    if (l === null) throw Error(s(311));
    l.lastRenderedReducer = t;
    var a = l.dispatch, u = l.pending, n = e.memoizedState;
    if (u !== null) {
      l.pending = null;
      var i = u = u.next;
      do
        n = t(n, i.action), i = i.next;
      while (i !== u);
      pe(n, e.memoizedState) || (Xt = !0), e.memoizedState = n, e.baseQueue === null && (e.baseState = n), l.lastRenderedState = n;
    }
    return [n, a];
  }
  function xr(t, e, l) {
    var a = lt, u = Bt(), n = ot;
    if (n) {
      if (l === void 0) throw Error(s(407));
      l = l();
    } else l = e();
    var i = !pe(
      (At || u).memoizedState,
      l
    );
    if (i && (u.memoizedState = l, Xt = !0), u = u.queue, jc(Or.bind(null, a, u, t), [
      t
    ]), u.getSnapshot !== e || i || Qt !== null && Qt.memoizedState.tag & 1) {
      if (a.flags |= 2048, Za(
        9,
        { destroy: void 0 },
        Nr.bind(
          null,
          a,
          u,
          l,
          e
        ),
        null
      ), qt === null) throw Error(s(349));
      n || (il & 127) !== 0 || qr(a, e, l);
    }
    return l;
  }
  function qr(t, e, l) {
    t.flags |= 16384, t = { getSnapshot: e, value: l }, e = lt.updateQueue, e === null ? (e = Xn(), lt.updateQueue = e, e.stores = [t]) : (l = e.stores, l === null ? e.stores = [t] : l.push(t));
  }
  function Nr(t, e, l, a) {
    e.value = l, e.getSnapshot = a, Mr(e) && Dr(t);
  }
  function Or(t, e, l) {
    return l(function() {
      Mr(e) && Dr(t);
    });
  }
  function Mr(t) {
    var e = t.getSnapshot;
    t = t.value;
    try {
      var l = e();
      return !pe(t, l);
    } catch {
      return !0;
    }
  }
  function Dr(t) {
    var e = ea(t, 2);
    e !== null && ye(e, t, 2);
  }
  function Uc(t) {
    var e = ne();
    if (typeof t == "function") {
      var l = t;
      if (t = l(), ra) {
        _l(!0);
        try {
          l();
        } finally {
          _l(!1);
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
  function Ur(t, e, l, a) {
    return t.baseState = l, Mc(
      t,
      At,
      typeof a == "function" ? a : cl
    );
  }
  function Rm(t, e, l, a, u) {
    if (wn(t)) throw Error(s(485));
    if (t = e.action, t !== null) {
      var n = {
        payload: u,
        action: t,
        next: null,
        isTransition: !0,
        status: "pending",
        value: null,
        reason: null,
        listeners: [],
        then: function(i) {
          n.listeners.push(i);
        }
      };
      q.T !== null ? l(!0) : n.isTransition = !1, a(n), l = e.pending, l === null ? (n.next = e.pending = n, Cr(e, n)) : (n.next = l.next, e.pending = l.next = n);
    }
  }
  function Cr(t, e) {
    var l = e.action, a = e.payload, u = t.state;
    if (e.isTransition) {
      var n = q.T, i = {};
      q.T = i;
      try {
        var o = l(u, a), d = q.S;
        d !== null && d(i, o), jr(t, e, o);
      } catch (S) {
        Cc(t, e, S);
      } finally {
        n !== null && i.types !== null && (n.types = i.types), q.T = n;
      }
    } else
      try {
        n = l(u, a), jr(t, e, n);
      } catch (S) {
        Cc(t, e, S);
      }
  }
  function jr(t, e, l) {
    l !== null && typeof l == "object" && typeof l.then == "function" ? l.then(
      function(a) {
        Rr(t, e, a);
      },
      function(a) {
        return Cc(t, e, a);
      }
    ) : Rr(t, e, l);
  }
  function Rr(t, e, l) {
    e.status = "fulfilled", e.value = l, Hr(e), t.state = l, e = t.pending, e !== null && (l = e.next, l === e ? t.pending = null : (l = l.next, e.next = l, Cr(t, l)));
  }
  function Cc(t, e, l) {
    var a = t.pending;
    if (t.pending = null, a !== null) {
      a = a.next;
      do
        e.status = "rejected", e.reason = l, Hr(e), e = e.next;
      while (e !== a);
    }
    t.action = null;
  }
  function Hr(t) {
    t = t.listeners;
    for (var e = 0; e < t.length; e++) (0, t[e])();
  }
  function Br(t, e) {
    return e;
  }
  function Yr(t, e) {
    if (ot) {
      var l = qt.formState;
      if (l !== null) {
        t: {
          var a = lt;
          if (ot) {
            if (Nt) {
              e: {
                for (var u = Nt, n = De; u.nodeType !== 8; ) {
                  if (!n) {
                    u = null;
                    break e;
                  }
                  if (u = Ce(
                    u.nextSibling
                  ), u === null) {
                    u = null;
                    break e;
                  }
                }
                n = u.data, u = n === "F!" || n === "F" ? u : null;
              }
              if (u) {
                Nt = Ce(
                  u.nextSibling
                ), a = u.data === "F!";
                break t;
              }
            }
            xl(a);
          }
          a = !1;
        }
        a && (e = l[0]);
      }
    }
    return l = ne(), l.memoizedState = l.baseState = e, a = {
      pending: null,
      lanes: 0,
      dispatch: null,
      lastRenderedReducer: Br,
      lastRenderedState: e
    }, l.queue = a, l = ao.bind(
      null,
      lt,
      a
    ), a.dispatch = l, a = Uc(!1), n = Lc.bind(
      null,
      lt,
      !1,
      a.queue
    ), a = ne(), u = {
      state: e,
      dispatch: null,
      action: t,
      pending: null
    }, a.queue = u, l = Rm.bind(
      null,
      lt,
      u,
      n,
      l
    ), u.dispatch = l, a.memoizedState = t, [e, l, !1];
  }
  function Lr(t) {
    var e = Bt();
    return Gr(e, At, t);
  }
  function Gr(t, e, l) {
    if (e = Mc(
      t,
      e,
      Br
    )[0], t = Vn(cl)[0], typeof e == "object" && e !== null && typeof e.then == "function")
      try {
        var a = Hu(e);
      } catch (i) {
        throw i === Ya ? jn : i;
      }
    else a = e;
    e = Bt();
    var u = e.queue, n = u.dispatch;
    return l !== e.memoizedState && (lt.flags |= 2048, Za(
      9,
      { destroy: void 0 },
      Hm.bind(null, u, l),
      null
    )), [a, n, t];
  }
  function Hm(t, e) {
    t.action = e;
  }
  function Qr(t) {
    var e = Bt(), l = At;
    if (l !== null)
      return Gr(e, l, t);
    Bt(), e = e.memoizedState, l = Bt();
    var a = l.queue.dispatch;
    return l.memoizedState = t, [e, a, !1];
  }
  function Za(t, e, l, a) {
    return t = { tag: t, create: l, deps: a, inst: e, next: null }, e = lt.updateQueue, e === null && (e = Xn(), lt.updateQueue = e), l = e.lastEffect, l === null ? e.lastEffect = t.next = t : (a = l.next, l.next = t, t.next = a, e.lastEffect = t), t;
  }
  function Xr() {
    return Bt().memoizedState;
  }
  function Kn(t, e, l, a) {
    var u = ne();
    lt.flags |= t, u.memoizedState = Za(
      1 | e,
      { destroy: void 0 },
      l,
      a === void 0 ? null : a
    );
  }
  function Jn(t, e, l, a) {
    var u = Bt();
    a = a === void 0 ? null : a;
    var n = u.memoizedState.inst;
    At !== null && a !== null && zc(a, At.memoizedState.deps) ? u.memoizedState = Za(e, n, l, a) : (lt.flags |= t, u.memoizedState = Za(
      1 | e,
      n,
      l,
      a
    ));
  }
  function Zr(t, e) {
    Kn(8390656, 8, t, e);
  }
  function jc(t, e) {
    Jn(2048, 8, t, e);
  }
  function Bm(t) {
    lt.flags |= 4;
    var e = lt.updateQueue;
    if (e === null)
      e = Xn(), lt.updateQueue = e, e.events = [t];
    else {
      var l = e.events;
      l === null ? e.events = [t] : l.push(t);
    }
  }
  function Vr(t) {
    var e = Bt().memoizedState;
    return Bm({ ref: e, nextImpl: t }), function() {
      if ((vt & 2) !== 0) throw Error(s(440));
      return e.impl.apply(void 0, arguments);
    };
  }
  function Kr(t, e) {
    return Jn(4, 2, t, e);
  }
  function Jr(t, e) {
    return Jn(4, 4, t, e);
  }
  function wr(t, e) {
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
  function kr(t, e, l) {
    l = l != null ? l.concat([t]) : null, Jn(4, 4, wr.bind(null, e, t), l);
  }
  function Rc() {
  }
  function $r(t, e) {
    var l = Bt();
    e = e === void 0 ? null : e;
    var a = l.memoizedState;
    return e !== null && zc(e, a[1]) ? a[0] : (l.memoizedState = [t, e], t);
  }
  function Wr(t, e) {
    var l = Bt();
    e = e === void 0 ? null : e;
    var a = l.memoizedState;
    if (e !== null && zc(e, a[1]))
      return a[0];
    if (a = t(), ra) {
      _l(!0);
      try {
        t();
      } finally {
        _l(!1);
      }
    }
    return l.memoizedState = [a, e], a;
  }
  function Hc(t, e, l) {
    return l === void 0 || (il & 1073741824) !== 0 && (st & 261930) === 0 ? t.memoizedState = e : (t.memoizedState = l, t = Io(), lt.lanes |= t, Rl |= t, l);
  }
  function Fr(t, e, l, a) {
    return pe(l, e) ? l : Ga.current !== null ? (t = Hc(t, l, a), pe(t, e) || (Xt = !0), t) : (il & 42) === 0 || (il & 1073741824) !== 0 && (st & 261930) === 0 ? (Xt = !0, t.memoizedState = l) : (t = Io(), lt.lanes |= t, Rl |= t, e);
  }
  function Ir(t, e, l, a, u) {
    var n = H.p;
    H.p = n !== 0 && 8 > n ? n : 8;
    var i = q.T, o = {};
    q.T = o, Lc(t, !1, e, l);
    try {
      var d = u(), S = q.S;
      if (S !== null && S(o, d), d !== null && typeof d == "object" && typeof d.then == "function") {
        var T = Um(
          d,
          a
        );
        Bu(
          t,
          e,
          T,
          ze(t)
        );
      } else
        Bu(
          t,
          e,
          a,
          ze(t)
        );
    } catch (U) {
      Bu(
        t,
        e,
        { then: function() {
        }, status: "rejected", reason: U },
        ze()
      );
    } finally {
      H.p = n, i !== null && o.types !== null && (i.types = o.types), q.T = i;
    }
  }
  function Ym() {
  }
  function Bc(t, e, l, a) {
    if (t.tag !== 5) throw Error(s(476));
    var u = Pr(t).queue;
    Ir(
      t,
      u,
      e,
      V,
      l === null ? Ym : function() {
        return to(t), l(a);
      }
    );
  }
  function Pr(t) {
    var e = t.memoizedState;
    if (e !== null) return e;
    e = {
      memoizedState: V,
      baseState: V,
      baseQueue: null,
      queue: {
        pending: null,
        lanes: 0,
        dispatch: null,
        lastRenderedReducer: cl,
        lastRenderedState: V
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
  function to(t) {
    var e = Pr(t);
    e.next === null && (e = t.alternate.memoizedState), Bu(
      t,
      e.next.queue,
      {},
      ze()
    );
  }
  function Yc() {
    return Ft(tn);
  }
  function eo() {
    return Bt().memoizedState;
  }
  function lo() {
    return Bt().memoizedState;
  }
  function Lm(t) {
    for (var e = t.return; e !== null; ) {
      switch (e.tag) {
        case 24:
        case 3:
          var l = ze();
          t = Ol(l);
          var a = Ml(e, t, l);
          a !== null && (ye(a, e, l), Uu(a, e, l)), e = { cache: yc() }, t.payload = e;
          return;
      }
      e = e.return;
    }
  }
  function Gm(t, e, l) {
    var a = ze();
    l = {
      lane: a,
      revertLane: 0,
      gesture: null,
      action: l,
      hasEagerState: !1,
      eagerState: null,
      next: null
    }, wn(t) ? uo(e, l) : (l = lc(t, e, l, a), l !== null && (ye(l, t, a), no(l, e, a)));
  }
  function ao(t, e, l) {
    var a = ze();
    Bu(t, e, l, a);
  }
  function Bu(t, e, l, a) {
    var u = {
      lane: a,
      revertLane: 0,
      gesture: null,
      action: l,
      hasEagerState: !1,
      eagerState: null,
      next: null
    };
    if (wn(t)) uo(e, u);
    else {
      var n = t.alternate;
      if (t.lanes === 0 && (n === null || n.lanes === 0) && (n = e.lastRenderedReducer, n !== null))
        try {
          var i = e.lastRenderedState, o = n(i, l);
          if (u.hasEagerState = !0, u.eagerState = o, pe(o, i))
            return qn(t, e, u, 0), qt === null && xn(), !1;
        } catch {
        } finally {
        }
      if (l = lc(t, e, u, a), l !== null)
        return ye(l, t, a), no(l, e, a), !0;
    }
    return !1;
  }
  function Lc(t, e, l, a) {
    if (a = {
      lane: 2,
      revertLane: pf(),
      gesture: null,
      action: a,
      hasEagerState: !1,
      eagerState: null,
      next: null
    }, wn(t)) {
      if (e) throw Error(s(479));
    } else
      e = lc(
        t,
        l,
        a,
        2
      ), e !== null && ye(e, t, 2);
  }
  function wn(t) {
    var e = t.alternate;
    return t === lt || e !== null && e === lt;
  }
  function uo(t, e) {
    Qa = Gn = !0;
    var l = t.pending;
    l === null ? e.next = e : (e.next = l.next, l.next = e), t.pending = e;
  }
  function no(t, e, l) {
    if ((l & 4194048) !== 0) {
      var a = e.lanes;
      a &= t.pendingLanes, l |= a, e.lanes = l, rs(t, l);
    }
  }
  var Yu = {
    readContext: Ft,
    use: Zn,
    useCallback: Ct,
    useContext: Ct,
    useEffect: Ct,
    useImperativeHandle: Ct,
    useLayoutEffect: Ct,
    useInsertionEffect: Ct,
    useMemo: Ct,
    useReducer: Ct,
    useRef: Ct,
    useState: Ct,
    useDebugValue: Ct,
    useDeferredValue: Ct,
    useTransition: Ct,
    useSyncExternalStore: Ct,
    useId: Ct,
    useHostTransitionStatus: Ct,
    useFormState: Ct,
    useActionState: Ct,
    useOptimistic: Ct,
    useMemoCache: Ct,
    useCacheRefresh: Ct
  };
  Yu.useEffectEvent = Ct;
  var io = {
    readContext: Ft,
    use: Zn,
    useCallback: function(t, e) {
      return ne().memoizedState = [
        t,
        e === void 0 ? null : e
      ], t;
    },
    useContext: Ft,
    useEffect: Zr,
    useImperativeHandle: function(t, e, l) {
      l = l != null ? l.concat([t]) : null, Kn(
        4194308,
        4,
        wr.bind(null, e, t),
        l
      );
    },
    useLayoutEffect: function(t, e) {
      return Kn(4194308, 4, t, e);
    },
    useInsertionEffect: function(t, e) {
      Kn(4, 2, t, e);
    },
    useMemo: function(t, e) {
      var l = ne();
      e = e === void 0 ? null : e;
      var a = t();
      if (ra) {
        _l(!0);
        try {
          t();
        } finally {
          _l(!1);
        }
      }
      return l.memoizedState = [a, e], a;
    },
    useReducer: function(t, e, l) {
      var a = ne();
      if (l !== void 0) {
        var u = l(e);
        if (ra) {
          _l(!0);
          try {
            l(e);
          } finally {
            _l(!1);
          }
        }
      } else u = e;
      return a.memoizedState = a.baseState = u, t = {
        pending: null,
        lanes: 0,
        dispatch: null,
        lastRenderedReducer: t,
        lastRenderedState: u
      }, a.queue = t, t = t.dispatch = Gm.bind(
        null,
        lt,
        t
      ), [a.memoizedState, t];
    },
    useRef: function(t) {
      var e = ne();
      return t = { current: t }, e.memoizedState = t;
    },
    useState: function(t) {
      t = Uc(t);
      var e = t.queue, l = ao.bind(null, lt, e);
      return e.dispatch = l, [t.memoizedState, l];
    },
    useDebugValue: Rc,
    useDeferredValue: function(t, e) {
      var l = ne();
      return Hc(l, t, e);
    },
    useTransition: function() {
      var t = Uc(!1);
      return t = Ir.bind(
        null,
        lt,
        t.queue,
        !0,
        !1
      ), ne().memoizedState = t, [!1, t];
    },
    useSyncExternalStore: function(t, e, l) {
      var a = lt, u = ne();
      if (ot) {
        if (l === void 0)
          throw Error(s(407));
        l = l();
      } else {
        if (l = e(), qt === null)
          throw Error(s(349));
        (st & 127) !== 0 || qr(a, e, l);
      }
      u.memoizedState = l;
      var n = { value: l, getSnapshot: e };
      return u.queue = n, Zr(Or.bind(null, a, n, t), [
        t
      ]), a.flags |= 2048, Za(
        9,
        { destroy: void 0 },
        Nr.bind(
          null,
          a,
          n,
          l,
          e
        ),
        null
      ), l;
    },
    useId: function() {
      var t = ne(), e = qt.identifierPrefix;
      if (ot) {
        var l = we, a = Je;
        l = (a & ~(1 << 32 - ge(a) - 1)).toString(32) + l, e = "_" + e + "R_" + l, l = Qn++, 0 < l && (e += "H" + l.toString(32)), e += "_";
      } else
        l = Cm++, e = "_" + e + "r_" + l.toString(32) + "_";
      return t.memoizedState = e;
    },
    useHostTransitionStatus: Yc,
    useFormState: Yr,
    useActionState: Yr,
    useOptimistic: function(t) {
      var e = ne();
      e.memoizedState = e.baseState = t;
      var l = {
        pending: null,
        lanes: 0,
        dispatch: null,
        lastRenderedReducer: null,
        lastRenderedState: null
      };
      return e.queue = l, e = Lc.bind(
        null,
        lt,
        !0,
        l
      ), l.dispatch = e, [t, e];
    },
    useMemoCache: Oc,
    useCacheRefresh: function() {
      return ne().memoizedState = Lm.bind(
        null,
        lt
      );
    },
    useEffectEvent: function(t) {
      var e = ne(), l = { impl: t };
      return e.memoizedState = l, function() {
        if ((vt & 2) !== 0)
          throw Error(s(440));
        return l.impl.apply(void 0, arguments);
      };
    }
  }, Gc = {
    readContext: Ft,
    use: Zn,
    useCallback: $r,
    useContext: Ft,
    useEffect: jc,
    useImperativeHandle: kr,
    useInsertionEffect: Kr,
    useLayoutEffect: Jr,
    useMemo: Wr,
    useReducer: Vn,
    useRef: Xr,
    useState: function() {
      return Vn(cl);
    },
    useDebugValue: Rc,
    useDeferredValue: function(t, e) {
      var l = Bt();
      return Fr(
        l,
        At.memoizedState,
        t,
        e
      );
    },
    useTransition: function() {
      var t = Vn(cl)[0], e = Bt().memoizedState;
      return [
        typeof t == "boolean" ? t : Hu(t),
        e
      ];
    },
    useSyncExternalStore: xr,
    useId: eo,
    useHostTransitionStatus: Yc,
    useFormState: Lr,
    useActionState: Lr,
    useOptimistic: function(t, e) {
      var l = Bt();
      return Ur(l, At, t, e);
    },
    useMemoCache: Oc,
    useCacheRefresh: lo
  };
  Gc.useEffectEvent = Vr;
  var co = {
    readContext: Ft,
    use: Zn,
    useCallback: $r,
    useContext: Ft,
    useEffect: jc,
    useImperativeHandle: kr,
    useInsertionEffect: Kr,
    useLayoutEffect: Jr,
    useMemo: Wr,
    useReducer: Dc,
    useRef: Xr,
    useState: function() {
      return Dc(cl);
    },
    useDebugValue: Rc,
    useDeferredValue: function(t, e) {
      var l = Bt();
      return At === null ? Hc(l, t, e) : Fr(
        l,
        At.memoizedState,
        t,
        e
      );
    },
    useTransition: function() {
      var t = Dc(cl)[0], e = Bt().memoizedState;
      return [
        typeof t == "boolean" ? t : Hu(t),
        e
      ];
    },
    useSyncExternalStore: xr,
    useId: eo,
    useHostTransitionStatus: Yc,
    useFormState: Qr,
    useActionState: Qr,
    useOptimistic: function(t, e) {
      var l = Bt();
      return At !== null ? Ur(l, At, t, e) : (l.baseState = t, [t, l.queue.dispatch]);
    },
    useMemoCache: Oc,
    useCacheRefresh: lo
  };
  co.useEffectEvent = Vr;
  function Qc(t, e, l, a) {
    e = t.memoizedState, l = l(a, e), l = l == null ? e : E({}, e, l), t.memoizedState = l, t.lanes === 0 && (t.updateQueue.baseState = l);
  }
  var Xc = {
    enqueueSetState: function(t, e, l) {
      t = t._reactInternals;
      var a = ze(), u = Ol(a);
      u.payload = e, l != null && (u.callback = l), e = Ml(t, u, a), e !== null && (ye(e, t, a), Uu(e, t, a));
    },
    enqueueReplaceState: function(t, e, l) {
      t = t._reactInternals;
      var a = ze(), u = Ol(a);
      u.tag = 1, u.payload = e, l != null && (u.callback = l), e = Ml(t, u, a), e !== null && (ye(e, t, a), Uu(e, t, a));
    },
    enqueueForceUpdate: function(t, e) {
      t = t._reactInternals;
      var l = ze(), a = Ol(l);
      a.tag = 2, e != null && (a.callback = e), e = Ml(t, a, l), e !== null && (ye(e, t, l), Uu(e, t, l));
    }
  };
  function fo(t, e, l, a, u, n, i) {
    return t = t.stateNode, typeof t.shouldComponentUpdate == "function" ? t.shouldComponentUpdate(a, n, i) : e.prototype && e.prototype.isPureReactComponent ? !zu(l, a) || !zu(u, n) : !0;
  }
  function so(t, e, l, a) {
    t = e.state, typeof e.componentWillReceiveProps == "function" && e.componentWillReceiveProps(l, a), typeof e.UNSAFE_componentWillReceiveProps == "function" && e.UNSAFE_componentWillReceiveProps(l, a), e.state !== t && Xc.enqueueReplaceState(e, e.state, null);
  }
  function oa(t, e) {
    var l = e;
    if ("ref" in e) {
      l = {};
      for (var a in e)
        a !== "ref" && (l[a] = e[a]);
    }
    if (t = t.defaultProps) {
      l === e && (l = E({}, l));
      for (var u in t)
        l[u] === void 0 && (l[u] = t[u]);
    }
    return l;
  }
  function ro(t) {
    Tn(t);
  }
  function oo(t) {
    console.error(t);
  }
  function yo(t) {
    Tn(t);
  }
  function kn(t, e) {
    try {
      var l = t.onUncaughtError;
      l(e.value, { componentStack: e.stack });
    } catch (a) {
      setTimeout(function() {
        throw a;
      });
    }
  }
  function mo(t, e, l) {
    try {
      var a = t.onCaughtError;
      a(l.value, {
        componentStack: l.stack,
        errorBoundary: e.tag === 1 ? e.stateNode : null
      });
    } catch (u) {
      setTimeout(function() {
        throw u;
      });
    }
  }
  function Zc(t, e, l) {
    return l = Ol(l), l.tag = 3, l.payload = { element: null }, l.callback = function() {
      kn(t, e);
    }, l;
  }
  function ho(t) {
    return t = Ol(t), t.tag = 3, t;
  }
  function vo(t, e, l, a) {
    var u = l.type.getDerivedStateFromError;
    if (typeof u == "function") {
      var n = a.value;
      t.payload = function() {
        return u(n);
      }, t.callback = function() {
        mo(e, l, a);
      };
    }
    var i = l.stateNode;
    i !== null && typeof i.componentDidCatch == "function" && (t.callback = function() {
      mo(e, l, a), typeof u != "function" && (Hl === null ? Hl = /* @__PURE__ */ new Set([this]) : Hl.add(this));
      var o = a.stack;
      this.componentDidCatch(a.value, {
        componentStack: o !== null ? o : ""
      });
    });
  }
  function Qm(t, e, l, a, u) {
    if (l.flags |= 32768, a !== null && typeof a == "object" && typeof a.then == "function") {
      if (e = l.alternate, e !== null && Ra(
        e,
        l,
        u,
        !0
      ), l = Se.current, l !== null) {
        switch (l.tag) {
          case 31:
          case 13:
            return Ue === null ? ii() : l.alternate === null && jt === 0 && (jt = 3), l.flags &= -257, l.flags |= 65536, l.lanes = u, a === Rn ? l.flags |= 16384 : (e = l.updateQueue, e === null ? l.updateQueue = /* @__PURE__ */ new Set([a]) : e.add(a), hf(t, a, u)), !1;
          case 22:
            return l.flags |= 65536, a === Rn ? l.flags |= 16384 : (e = l.updateQueue, e === null ? (e = {
              transitions: null,
              markerInstances: null,
              retryQueue: /* @__PURE__ */ new Set([a])
            }, l.updateQueue = e) : (l = e.retryQueue, l === null ? e.retryQueue = /* @__PURE__ */ new Set([a]) : l.add(a)), hf(t, a, u)), !1;
        }
        throw Error(s(435, l.tag));
      }
      return hf(t, a, u), ii(), !1;
    }
    if (ot)
      return e = Se.current, e !== null ? ((e.flags & 65536) === 0 && (e.flags |= 256), e.flags |= 65536, e.lanes = u, a !== fc && (t = Error(s(422), { cause: a }), qu(Ne(t, l)))) : (a !== fc && (e = Error(s(423), {
        cause: a
      }), qu(
        Ne(e, l)
      )), t = t.current.alternate, t.flags |= 65536, u &= -u, t.lanes |= u, a = Ne(a, l), u = Zc(
        t.stateNode,
        a,
        u
      ), bc(t, u), jt !== 4 && (jt = 2)), !1;
    var n = Error(s(520), { cause: a });
    if (n = Ne(n, l), Ju === null ? Ju = [n] : Ju.push(n), jt !== 4 && (jt = 2), e === null) return !0;
    a = Ne(a, l), l = e;
    do {
      switch (l.tag) {
        case 3:
          return l.flags |= 65536, t = u & -u, l.lanes |= t, t = Zc(l.stateNode, a, t), bc(l, t), !1;
        case 1:
          if (e = l.type, n = l.stateNode, (l.flags & 128) === 0 && (typeof e.getDerivedStateFromError == "function" || n !== null && typeof n.componentDidCatch == "function" && (Hl === null || !Hl.has(n))))
            return l.flags |= 65536, u &= -u, l.lanes |= u, u = ho(u), vo(
              u,
              t,
              l,
              a
            ), bc(l, u), !1;
      }
      l = l.return;
    } while (l !== null);
    return !1;
  }
  var Vc = Error(s(461)), Xt = !1;
  function It(t, e, l, a) {
    e.child = t === null ? br(e, null, l, a) : sa(
      e,
      t.child,
      l,
      a
    );
  }
  function go(t, e, l, a, u) {
    l = l.render;
    var n = e.ref;
    if ("ref" in a) {
      var i = {};
      for (var o in a)
        o !== "ref" && (i[o] = a[o]);
    } else i = a;
    return na(e), a = Tc(
      t,
      e,
      l,
      i,
      n,
      u
    ), o = xc(), t !== null && !Xt ? (qc(t, e, u), fl(t, e, u)) : (ot && o && ic(e), e.flags |= 1, It(t, e, a, u), e.child);
  }
  function po(t, e, l, a, u) {
    if (t === null) {
      var n = l.type;
      return typeof n == "function" && !ac(n) && n.defaultProps === void 0 && l.compare === null ? (e.tag = 15, e.type = n, bo(
        t,
        e,
        n,
        a,
        u
      )) : (t = On(
        l.type,
        null,
        a,
        e,
        e.mode,
        u
      ), t.ref = e.ref, t.return = e, e.child = t);
    }
    if (n = t.child, !Ic(t, u)) {
      var i = n.memoizedProps;
      if (l = l.compare, l = l !== null ? l : zu, l(i, a) && t.ref === e.ref)
        return fl(t, e, u);
    }
    return e.flags |= 1, t = ll(n, a), t.ref = e.ref, t.return = e, e.child = t;
  }
  function bo(t, e, l, a, u) {
    if (t !== null) {
      var n = t.memoizedProps;
      if (zu(n, a) && t.ref === e.ref)
        if (Xt = !1, e.pendingProps = a = n, Ic(t, u))
          (t.flags & 131072) !== 0 && (Xt = !0);
        else
          return e.lanes = t.lanes, fl(t, e, u);
    }
    return Kc(
      t,
      e,
      l,
      a,
      u
    );
  }
  function So(t, e, l, a) {
    var u = a.children, n = t !== null ? t.memoizedState : null;
    if (t === null && e.stateNode === null && (e.stateNode = {
      _visibility: 1,
      _pendingMarkers: null,
      _retryCache: null,
      _transitions: null
    }), a.mode === "hidden") {
      if ((e.flags & 128) !== 0) {
        if (n = n !== null ? n.baseLanes | l : l, t !== null) {
          for (a = e.child = t.child, u = 0; a !== null; )
            u = u | a.lanes | a.childLanes, a = a.sibling;
          a = u & ~n;
        } else a = 0, e.child = null;
        return _o(
          t,
          e,
          n,
          l,
          a
        );
      }
      if ((l & 536870912) !== 0)
        e.memoizedState = { baseLanes: 0, cachePool: null }, t !== null && Cn(
          e,
          n !== null ? n.cachePool : null
        ), n !== null ? Er(e, n) : _c(), Ar(e);
      else
        return a = e.lanes = 536870912, _o(
          t,
          e,
          n !== null ? n.baseLanes | l : l,
          l,
          a
        );
    } else
      n !== null ? (Cn(e, n.cachePool), Er(e, n), Ul(), e.memoizedState = null) : (t !== null && Cn(e, null), _c(), Ul());
    return It(t, e, u, l), e.child;
  }
  function Lu(t, e) {
    return t !== null && t.tag === 22 || e.stateNode !== null || (e.stateNode = {
      _visibility: 1,
      _pendingMarkers: null,
      _retryCache: null,
      _transitions: null
    }), e.sibling;
  }
  function _o(t, e, l, a, u) {
    var n = hc();
    return n = n === null ? null : { parent: Gt._currentValue, pool: n }, e.memoizedState = {
      baseLanes: l,
      cachePool: n
    }, t !== null && Cn(e, null), _c(), Ar(e), t !== null && Ra(t, e, a, !0), e.childLanes = u, null;
  }
  function $n(t, e) {
    return e = Fn(
      { mode: e.mode, children: e.children },
      t.mode
    ), e.ref = t.ref, t.child = e, e.return = t, e;
  }
  function Eo(t, e, l) {
    return sa(e, t.child, null, l), t = $n(e, e.pendingProps), t.flags |= 2, _e(e), e.memoizedState = null, t;
  }
  function Xm(t, e, l) {
    var a = e.pendingProps, u = (e.flags & 128) !== 0;
    if (e.flags &= -129, t === null) {
      if (ot) {
        if (a.mode === "hidden")
          return t = $n(e, a), e.lanes = 536870912, Lu(null, t);
        if (Ac(e), (t = Nt) ? (t = jd(
          t,
          De
        ), t = t !== null && t.data === "&" ? t : null, t !== null && (e.memoizedState = {
          dehydrated: t,
          treeContext: zl !== null ? { id: Je, overflow: we } : null,
          retryLane: 536870912,
          hydrationErrors: null
        }, l = ur(t), l.return = e, e.child = l, Wt = e, Nt = null)) : t = null, t === null) throw xl(e);
        return e.lanes = 536870912, null;
      }
      return $n(e, a);
    }
    var n = t.memoizedState;
    if (n !== null) {
      var i = n.dehydrated;
      if (Ac(e), u)
        if (e.flags & 256)
          e.flags &= -257, e = Eo(
            t,
            e,
            l
          );
        else if (e.memoizedState !== null)
          e.child = t.child, e.flags |= 128, e = null;
        else throw Error(s(558));
      else if (Xt || Ra(t, e, l, !1), u = (l & t.childLanes) !== 0, Xt || u) {
        if (a = qt, a !== null && (i = os(a, l), i !== 0 && i !== n.retryLane))
          throw n.retryLane = i, ea(t, i), ye(a, t, i), Vc;
        ii(), e = Eo(
          t,
          e,
          l
        );
      } else
        t = n.treeContext, Nt = Ce(i.nextSibling), Wt = e, ot = !0, Tl = null, De = !1, t !== null && cr(e, t), e = $n(e, a), e.flags |= 4096;
      return e;
    }
    return t = ll(t.child, {
      mode: a.mode,
      children: a.children
    }), t.ref = e.ref, e.child = t, t.return = e, t;
  }
  function Wn(t, e) {
    var l = e.ref;
    if (l === null)
      t !== null && t.ref !== null && (e.flags |= 4194816);
    else {
      if (typeof l != "function" && typeof l != "object")
        throw Error(s(284));
      (t === null || t.ref !== l) && (e.flags |= 4194816);
    }
  }
  function Kc(t, e, l, a, u) {
    return na(e), l = Tc(
      t,
      e,
      l,
      a,
      void 0,
      u
    ), a = xc(), t !== null && !Xt ? (qc(t, e, u), fl(t, e, u)) : (ot && a && ic(e), e.flags |= 1, It(t, e, l, u), e.child);
  }
  function Ao(t, e, l, a, u, n) {
    return na(e), e.updateQueue = null, l = Tr(
      e,
      a,
      l,
      u
    ), zr(t), a = xc(), t !== null && !Xt ? (qc(t, e, n), fl(t, e, n)) : (ot && a && ic(e), e.flags |= 1, It(t, e, l, n), e.child);
  }
  function zo(t, e, l, a, u) {
    if (na(e), e.stateNode === null) {
      var n = Da, i = l.contextType;
      typeof i == "object" && i !== null && (n = Ft(i)), n = new l(a, n), e.memoizedState = n.state !== null && n.state !== void 0 ? n.state : null, n.updater = Xc, e.stateNode = n, n._reactInternals = e, n = e.stateNode, n.props = a, n.state = e.memoizedState, n.refs = {}, gc(e), i = l.contextType, n.context = typeof i == "object" && i !== null ? Ft(i) : Da, n.state = e.memoizedState, i = l.getDerivedStateFromProps, typeof i == "function" && (Qc(
        e,
        l,
        i,
        a
      ), n.state = e.memoizedState), typeof l.getDerivedStateFromProps == "function" || typeof n.getSnapshotBeforeUpdate == "function" || typeof n.UNSAFE_componentWillMount != "function" && typeof n.componentWillMount != "function" || (i = n.state, typeof n.componentWillMount == "function" && n.componentWillMount(), typeof n.UNSAFE_componentWillMount == "function" && n.UNSAFE_componentWillMount(), i !== n.state && Xc.enqueueReplaceState(n, n.state, null), ju(e, a, n, u), Cu(), n.state = e.memoizedState), typeof n.componentDidMount == "function" && (e.flags |= 4194308), a = !0;
    } else if (t === null) {
      n = e.stateNode;
      var o = e.memoizedProps, d = oa(l, o);
      n.props = d;
      var S = n.context, T = l.contextType;
      i = Da, typeof T == "object" && T !== null && (i = Ft(T));
      var U = l.getDerivedStateFromProps;
      T = typeof U == "function" || typeof n.getSnapshotBeforeUpdate == "function", o = e.pendingProps !== o, T || typeof n.UNSAFE_componentWillReceiveProps != "function" && typeof n.componentWillReceiveProps != "function" || (o || S !== i) && so(
        e,
        n,
        a,
        i
      ), Nl = !1;
      var _ = e.memoizedState;
      n.state = _, ju(e, a, n, u), Cu(), S = e.memoizedState, o || _ !== S || Nl ? (typeof U == "function" && (Qc(
        e,
        l,
        U,
        a
      ), S = e.memoizedState), (d = Nl || fo(
        e,
        l,
        d,
        a,
        _,
        S,
        i
      )) ? (T || typeof n.UNSAFE_componentWillMount != "function" && typeof n.componentWillMount != "function" || (typeof n.componentWillMount == "function" && n.componentWillMount(), typeof n.UNSAFE_componentWillMount == "function" && n.UNSAFE_componentWillMount()), typeof n.componentDidMount == "function" && (e.flags |= 4194308)) : (typeof n.componentDidMount == "function" && (e.flags |= 4194308), e.memoizedProps = a, e.memoizedState = S), n.props = a, n.state = S, n.context = i, a = d) : (typeof n.componentDidMount == "function" && (e.flags |= 4194308), a = !1);
    } else {
      n = e.stateNode, pc(t, e), i = e.memoizedProps, T = oa(l, i), n.props = T, U = e.pendingProps, _ = n.context, S = l.contextType, d = Da, typeof S == "object" && S !== null && (d = Ft(S)), o = l.getDerivedStateFromProps, (S = typeof o == "function" || typeof n.getSnapshotBeforeUpdate == "function") || typeof n.UNSAFE_componentWillReceiveProps != "function" && typeof n.componentWillReceiveProps != "function" || (i !== U || _ !== d) && so(
        e,
        n,
        a,
        d
      ), Nl = !1, _ = e.memoizedState, n.state = _, ju(e, a, n, u), Cu();
      var A = e.memoizedState;
      i !== U || _ !== A || Nl || t !== null && t.dependencies !== null && Dn(t.dependencies) ? (typeof o == "function" && (Qc(
        e,
        l,
        o,
        a
      ), A = e.memoizedState), (T = Nl || fo(
        e,
        l,
        T,
        a,
        _,
        A,
        d
      ) || t !== null && t.dependencies !== null && Dn(t.dependencies)) ? (S || typeof n.UNSAFE_componentWillUpdate != "function" && typeof n.componentWillUpdate != "function" || (typeof n.componentWillUpdate == "function" && n.componentWillUpdate(a, A, d), typeof n.UNSAFE_componentWillUpdate == "function" && n.UNSAFE_componentWillUpdate(
        a,
        A,
        d
      )), typeof n.componentDidUpdate == "function" && (e.flags |= 4), typeof n.getSnapshotBeforeUpdate == "function" && (e.flags |= 1024)) : (typeof n.componentDidUpdate != "function" || i === t.memoizedProps && _ === t.memoizedState || (e.flags |= 4), typeof n.getSnapshotBeforeUpdate != "function" || i === t.memoizedProps && _ === t.memoizedState || (e.flags |= 1024), e.memoizedProps = a, e.memoizedState = A), n.props = a, n.state = A, n.context = d, a = T) : (typeof n.componentDidUpdate != "function" || i === t.memoizedProps && _ === t.memoizedState || (e.flags |= 4), typeof n.getSnapshotBeforeUpdate != "function" || i === t.memoizedProps && _ === t.memoizedState || (e.flags |= 1024), a = !1);
    }
    return n = a, Wn(t, e), a = (e.flags & 128) !== 0, n || a ? (n = e.stateNode, l = a && typeof l.getDerivedStateFromError != "function" ? null : n.render(), e.flags |= 1, t !== null && a ? (e.child = sa(
      e,
      t.child,
      null,
      u
    ), e.child = sa(
      e,
      null,
      l,
      u
    )) : It(t, e, l, u), e.memoizedState = n.state, t = e.child) : t = fl(
      t,
      e,
      u
    ), t;
  }
  function To(t, e, l, a) {
    return aa(), e.flags |= 256, It(t, e, l, a), e.child;
  }
  var Jc = {
    dehydrated: null,
    treeContext: null,
    retryLane: 0,
    hydrationErrors: null
  };
  function wc(t) {
    return { baseLanes: t, cachePool: yr() };
  }
  function kc(t, e, l) {
    return t = t !== null ? t.childLanes & ~l : 0, e && (t |= Ae), t;
  }
  function xo(t, e, l) {
    var a = e.pendingProps, u = !1, n = (e.flags & 128) !== 0, i;
    if ((i = n) || (i = t !== null && t.memoizedState === null ? !1 : (Ht.current & 2) !== 0), i && (u = !0, e.flags &= -129), i = (e.flags & 32) !== 0, e.flags &= -33, t === null) {
      if (ot) {
        if (u ? Dl(e) : Ul(), (t = Nt) ? (t = jd(
          t,
          De
        ), t = t !== null && t.data !== "&" ? t : null, t !== null && (e.memoizedState = {
          dehydrated: t,
          treeContext: zl !== null ? { id: Je, overflow: we } : null,
          retryLane: 536870912,
          hydrationErrors: null
        }, l = ur(t), l.return = e, e.child = l, Wt = e, Nt = null)) : t = null, t === null) throw xl(e);
        return Df(t) ? e.lanes = 32 : e.lanes = 536870912, null;
      }
      var o = a.children;
      return a = a.fallback, u ? (Ul(), u = e.mode, o = Fn(
        { mode: "hidden", children: o },
        u
      ), a = la(
        a,
        u,
        l,
        null
      ), o.return = e, a.return = e, o.sibling = a, e.child = o, a = e.child, a.memoizedState = wc(l), a.childLanes = kc(
        t,
        i,
        l
      ), e.memoizedState = Jc, Lu(null, a)) : (Dl(e), $c(e, o));
    }
    var d = t.memoizedState;
    if (d !== null && (o = d.dehydrated, o !== null)) {
      if (n)
        e.flags & 256 ? (Dl(e), e.flags &= -257, e = Wc(
          t,
          e,
          l
        )) : e.memoizedState !== null ? (Ul(), e.child = t.child, e.flags |= 128, e = null) : (Ul(), o = a.fallback, u = e.mode, a = Fn(
          { mode: "visible", children: a.children },
          u
        ), o = la(
          o,
          u,
          l,
          null
        ), o.flags |= 2, a.return = e, o.return = e, a.sibling = o, e.child = a, sa(
          e,
          t.child,
          null,
          l
        ), a = e.child, a.memoizedState = wc(l), a.childLanes = kc(
          t,
          i,
          l
        ), e.memoizedState = Jc, e = Lu(null, a));
      else if (Dl(e), Df(o)) {
        if (i = o.nextSibling && o.nextSibling.dataset, i) var S = i.dgst;
        i = S, a = Error(s(419)), a.stack = "", a.digest = i, qu({ value: a, source: null, stack: null }), e = Wc(
          t,
          e,
          l
        );
      } else if (Xt || Ra(t, e, l, !1), i = (l & t.childLanes) !== 0, Xt || i) {
        if (i = qt, i !== null && (a = os(i, l), a !== 0 && a !== d.retryLane))
          throw d.retryLane = a, ea(t, a), ye(i, t, a), Vc;
        Mf(o) || ii(), e = Wc(
          t,
          e,
          l
        );
      } else
        Mf(o) ? (e.flags |= 192, e.child = t.child, e = null) : (t = d.treeContext, Nt = Ce(
          o.nextSibling
        ), Wt = e, ot = !0, Tl = null, De = !1, t !== null && cr(e, t), e = $c(
          e,
          a.children
        ), e.flags |= 4096);
      return e;
    }
    return u ? (Ul(), o = a.fallback, u = e.mode, d = t.child, S = d.sibling, a = ll(d, {
      mode: "hidden",
      children: a.children
    }), a.subtreeFlags = d.subtreeFlags & 65011712, S !== null ? o = ll(
      S,
      o
    ) : (o = la(
      o,
      u,
      l,
      null
    ), o.flags |= 2), o.return = e, a.return = e, a.sibling = o, e.child = a, Lu(null, a), a = e.child, o = t.child.memoizedState, o === null ? o = wc(l) : (u = o.cachePool, u !== null ? (d = Gt._currentValue, u = u.parent !== d ? { parent: d, pool: d } : u) : u = yr(), o = {
      baseLanes: o.baseLanes | l,
      cachePool: u
    }), a.memoizedState = o, a.childLanes = kc(
      t,
      i,
      l
    ), e.memoizedState = Jc, Lu(t.child, a)) : (Dl(e), l = t.child, t = l.sibling, l = ll(l, {
      mode: "visible",
      children: a.children
    }), l.return = e, l.sibling = null, t !== null && (i = e.deletions, i === null ? (e.deletions = [t], e.flags |= 16) : i.push(t)), e.child = l, e.memoizedState = null, l);
  }
  function $c(t, e) {
    return e = Fn(
      { mode: "visible", children: e },
      t.mode
    ), e.return = t, t.child = e;
  }
  function Fn(t, e) {
    return t = be(22, t, null, e), t.lanes = 0, t;
  }
  function Wc(t, e, l) {
    return sa(e, t.child, null, l), t = $c(
      e,
      e.pendingProps.children
    ), t.flags |= 2, e.memoizedState = null, t;
  }
  function qo(t, e, l) {
    t.lanes |= e;
    var a = t.alternate;
    a !== null && (a.lanes |= e), oc(t.return, e, l);
  }
  function Fc(t, e, l, a, u, n) {
    var i = t.memoizedState;
    i === null ? t.memoizedState = {
      isBackwards: e,
      rendering: null,
      renderingStartTime: 0,
      last: a,
      tail: l,
      tailMode: u,
      treeForkCount: n
    } : (i.isBackwards = e, i.rendering = null, i.renderingStartTime = 0, i.last = a, i.tail = l, i.tailMode = u, i.treeForkCount = n);
  }
  function No(t, e, l) {
    var a = e.pendingProps, u = a.revealOrder, n = a.tail;
    a = a.children;
    var i = Ht.current, o = (i & 2) !== 0;
    if (o ? (i = i & 1 | 2, e.flags |= 128) : i &= 1, R(Ht, i), It(t, e, a, l), a = ot ? xu : 0, !o && t !== null && (t.flags & 128) !== 0)
      t: for (t = e.child; t !== null; ) {
        if (t.tag === 13)
          t.memoizedState !== null && qo(t, l, e);
        else if (t.tag === 19)
          qo(t, l, e);
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
    switch (u) {
      case "forwards":
        for (l = e.child, u = null; l !== null; )
          t = l.alternate, t !== null && Ln(t) === null && (u = l), l = l.sibling;
        l = u, l === null ? (u = e.child, e.child = null) : (u = l.sibling, l.sibling = null), Fc(
          e,
          !1,
          u,
          l,
          n,
          a
        );
        break;
      case "backwards":
      case "unstable_legacy-backwards":
        for (l = null, u = e.child, e.child = null; u !== null; ) {
          if (t = u.alternate, t !== null && Ln(t) === null) {
            e.child = u;
            break;
          }
          t = u.sibling, u.sibling = l, l = u, u = t;
        }
        Fc(
          e,
          !0,
          l,
          null,
          n,
          a
        );
        break;
      case "together":
        Fc(
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
    if (t !== null && (e.dependencies = t.dependencies), Rl |= e.lanes, (l & e.childLanes) === 0)
      if (t !== null) {
        if (Ra(
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
  function Ic(t, e) {
    return (t.lanes & e) !== 0 ? !0 : (t = t.dependencies, !!(t !== null && Dn(t)));
  }
  function Zm(t, e, l) {
    switch (e.tag) {
      case 3:
        Ut(e, e.stateNode.containerInfo), ql(e, Gt, t.memoizedState.cache), aa();
        break;
      case 27:
      case 5:
        bl(e);
        break;
      case 4:
        Ut(e, e.stateNode.containerInfo);
        break;
      case 10:
        ql(
          e,
          e.type,
          e.memoizedProps.value
        );
        break;
      case 31:
        if (e.memoizedState !== null)
          return e.flags |= 128, Ac(e), null;
        break;
      case 13:
        var a = e.memoizedState;
        if (a !== null)
          return a.dehydrated !== null ? (Dl(e), e.flags |= 128, null) : (l & e.child.childLanes) !== 0 ? xo(t, e, l) : (Dl(e), t = fl(
            t,
            e,
            l
          ), t !== null ? t.sibling : null);
        Dl(e);
        break;
      case 19:
        var u = (t.flags & 128) !== 0;
        if (a = (l & e.childLanes) !== 0, a || (Ra(
          t,
          e,
          l,
          !1
        ), a = (l & e.childLanes) !== 0), u) {
          if (a)
            return No(
              t,
              e,
              l
            );
          e.flags |= 128;
        }
        if (u = e.memoizedState, u !== null && (u.rendering = null, u.tail = null, u.lastEffect = null), R(Ht, Ht.current), a) break;
        return null;
      case 22:
        return e.lanes = 0, So(
          t,
          e,
          l,
          e.pendingProps
        );
      case 24:
        ql(e, Gt, t.memoizedState.cache);
    }
    return fl(t, e, l);
  }
  function Oo(t, e, l) {
    if (t !== null)
      if (t.memoizedProps !== e.pendingProps)
        Xt = !0;
      else {
        if (!Ic(t, l) && (e.flags & 128) === 0)
          return Xt = !1, Zm(
            t,
            e,
            l
          );
        Xt = (t.flags & 131072) !== 0;
      }
    else
      Xt = !1, ot && (e.flags & 1048576) !== 0 && ir(e, xu, e.index);
    switch (e.lanes = 0, e.tag) {
      case 16:
        t: {
          var a = e.pendingProps;
          if (t = ca(e.elementType), e.type = t, typeof t == "function")
            ac(t) ? (a = oa(t, a), e.tag = 1, e = zo(
              null,
              e,
              t,
              a,
              l
            )) : (e.tag = 0, e = Kc(
              null,
              e,
              t,
              a,
              l
            ));
          else {
            if (t != null) {
              var u = t.$$typeof;
              if (u === nt) {
                e.tag = 11, e = go(
                  null,
                  e,
                  t,
                  a,
                  l
                );
                break t;
              } else if (u === it) {
                e.tag = 14, e = po(
                  null,
                  e,
                  t,
                  a,
                  l
                );
                break t;
              }
            }
            throw e = Ye(t) || t, Error(s(306, e, ""));
          }
        }
        return e;
      case 0:
        return Kc(
          t,
          e,
          e.type,
          e.pendingProps,
          l
        );
      case 1:
        return a = e.type, u = oa(
          a,
          e.pendingProps
        ), zo(
          t,
          e,
          a,
          u,
          l
        );
      case 3:
        t: {
          if (Ut(
            e,
            e.stateNode.containerInfo
          ), t === null) throw Error(s(387));
          a = e.pendingProps;
          var n = e.memoizedState;
          u = n.element, pc(t, e), ju(e, a, null, l);
          var i = e.memoizedState;
          if (a = i.cache, ql(e, Gt, a), a !== n.cache && dc(
            e,
            [Gt],
            l,
            !0
          ), Cu(), a = i.element, n.isDehydrated)
            if (n = {
              element: a,
              isDehydrated: !1,
              cache: i.cache
            }, e.updateQueue.baseState = n, e.memoizedState = n, e.flags & 256) {
              e = To(
                t,
                e,
                a,
                l
              );
              break t;
            } else if (a !== u) {
              u = Ne(
                Error(s(424)),
                e
              ), qu(u), e = To(
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
              for (Nt = Ce(t.firstChild), Wt = e, ot = !0, Tl = null, De = !0, l = br(
                e,
                null,
                a,
                l
              ), e.child = l; l; )
                l.flags = l.flags & -3 | 4096, l = l.sibling;
            }
          else {
            if (aa(), a === u) {
              e = fl(
                t,
                e,
                l
              );
              break t;
            }
            It(t, e, a, l);
          }
          e = e.child;
        }
        return e;
      case 26:
        return Wn(t, e), t === null ? (l = Gd(
          e.type,
          null,
          e.pendingProps,
          null
        )) ? e.memoizedState = l : ot || (l = e.type, t = e.pendingProps, a = yi(
          et.current
        ).createElement(l), a[$t] = e, a[ce] = t, Pt(a, l, t), wt(a), e.stateNode = a) : e.memoizedState = Gd(
          e.type,
          t.memoizedProps,
          e.pendingProps,
          t.memoizedState
        ), null;
      case 27:
        return bl(e), t === null && ot && (a = e.stateNode = Bd(
          e.type,
          e.pendingProps,
          et.current
        ), Wt = e, De = !0, u = Nt, Gl(e.type) ? (Uf = u, Nt = Ce(a.firstChild)) : Nt = u), It(
          t,
          e,
          e.pendingProps.children,
          l
        ), Wn(t, e), t === null && (e.flags |= 4194304), e.child;
      case 5:
        return t === null && ot && ((u = a = Nt) && (a = bh(
          a,
          e.type,
          e.pendingProps,
          De
        ), a !== null ? (e.stateNode = a, Wt = e, Nt = Ce(a.firstChild), De = !1, u = !0) : u = !1), u || xl(e)), bl(e), u = e.type, n = e.pendingProps, i = t !== null ? t.memoizedProps : null, a = n.children, qf(u, n) ? a = null : i !== null && qf(u, i) && (e.flags |= 32), e.memoizedState !== null && (u = Tc(
          t,
          e,
          jm,
          null,
          null,
          l
        ), tn._currentValue = u), Wn(t, e), It(t, e, a, l), e.child;
      case 6:
        return t === null && ot && ((t = l = Nt) && (l = Sh(
          l,
          e.pendingProps,
          De
        ), l !== null ? (e.stateNode = l, Wt = e, Nt = null, t = !0) : t = !1), t || xl(e)), null;
      case 13:
        return xo(t, e, l);
      case 4:
        return Ut(
          e,
          e.stateNode.containerInfo
        ), a = e.pendingProps, t === null ? e.child = sa(
          e,
          null,
          a,
          l
        ) : It(t, e, a, l), e.child;
      case 11:
        return go(
          t,
          e,
          e.type,
          e.pendingProps,
          l
        );
      case 7:
        return It(
          t,
          e,
          e.pendingProps,
          l
        ), e.child;
      case 8:
        return It(
          t,
          e,
          e.pendingProps.children,
          l
        ), e.child;
      case 12:
        return It(
          t,
          e,
          e.pendingProps.children,
          l
        ), e.child;
      case 10:
        return a = e.pendingProps, ql(e, e.type, a.value), It(t, e, a.children, l), e.child;
      case 9:
        return u = e.type._context, a = e.pendingProps.children, na(e), u = Ft(u), a = a(u), e.flags |= 1, It(t, e, a, l), e.child;
      case 14:
        return po(
          t,
          e,
          e.type,
          e.pendingProps,
          l
        );
      case 15:
        return bo(
          t,
          e,
          e.type,
          e.pendingProps,
          l
        );
      case 19:
        return No(t, e, l);
      case 31:
        return Xm(t, e, l);
      case 22:
        return So(
          t,
          e,
          l,
          e.pendingProps
        );
      case 24:
        return na(e), a = Ft(Gt), t === null ? (u = hc(), u === null && (u = qt, n = yc(), u.pooledCache = n, n.refCount++, n !== null && (u.pooledCacheLanes |= l), u = n), e.memoizedState = { parent: a, cache: u }, gc(e), ql(e, Gt, u)) : ((t.lanes & l) !== 0 && (pc(t, e), ju(e, null, null, l), Cu()), u = t.memoizedState, n = e.memoizedState, u.parent !== a ? (u = { parent: a, cache: a }, e.memoizedState = u, e.lanes === 0 && (e.memoizedState = e.updateQueue.baseState = u), ql(e, Gt, a)) : (a = n.cache, ql(e, Gt, a), a !== u.cache && dc(
          e,
          [Gt],
          l,
          !0
        ))), It(
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
  function Pc(t, e, l, a, u) {
    if ((e = (t.mode & 32) !== 0) && (e = !1), e) {
      if (t.flags |= 16777216, (u & 335544128) === u)
        if (t.stateNode.complete) t.flags |= 8192;
        else if (ld()) t.flags |= 8192;
        else
          throw fa = Rn, vc;
    } else t.flags &= -16777217;
  }
  function Mo(t, e) {
    if (e.type !== "stylesheet" || (e.state.loading & 4) !== 0)
      t.flags &= -16777217;
    else if (t.flags |= 16777216, !Kd(e))
      if (ld()) t.flags |= 8192;
      else
        throw fa = Rn, vc;
  }
  function In(t, e) {
    e !== null && (t.flags |= 4), t.flags & 16384 && (e = t.tag !== 22 ? fs() : 536870912, t.lanes |= e, wa |= e);
  }
  function Gu(t, e) {
    if (!ot)
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
  function Ot(t) {
    var e = t.alternate !== null && t.alternate.child === t.child, l = 0, a = 0;
    if (e)
      for (var u = t.child; u !== null; )
        l |= u.lanes | u.childLanes, a |= u.subtreeFlags & 65011712, a |= u.flags & 65011712, u.return = t, u = u.sibling;
    else
      for (u = t.child; u !== null; )
        l |= u.lanes | u.childLanes, a |= u.subtreeFlags, a |= u.flags, u.return = t, u = u.sibling;
    return t.subtreeFlags |= a, t.childLanes = l, e;
  }
  function Vm(t, e, l) {
    var a = e.pendingProps;
    switch (cc(e), e.tag) {
      case 16:
      case 15:
      case 0:
      case 11:
      case 7:
      case 8:
      case 12:
      case 9:
      case 14:
        return Ot(e), null;
      case 1:
        return Ot(e), null;
      case 3:
        return l = e.stateNode, a = null, t !== null && (a = t.memoizedState.cache), e.memoizedState.cache !== a && (e.flags |= 2048), nl(Gt), Dt(), l.pendingContext && (l.context = l.pendingContext, l.pendingContext = null), (t === null || t.child === null) && (ja(e) ? sl(e) : t === null || t.memoizedState.isDehydrated && (e.flags & 256) === 0 || (e.flags |= 1024, sc())), Ot(e), null;
      case 26:
        var u = e.type, n = e.memoizedState;
        return t === null ? (sl(e), n !== null ? (Ot(e), Mo(e, n)) : (Ot(e), Pc(
          e,
          u,
          null,
          a,
          l
        ))) : n ? n !== t.memoizedState ? (sl(e), Ot(e), Mo(e, n)) : (Ot(e), e.flags &= -16777217) : (t = t.memoizedProps, t !== a && sl(e), Ot(e), Pc(
          e,
          u,
          t,
          a,
          l
        )), null;
      case 27:
        if (Ve(e), l = et.current, u = e.type, t !== null && e.stateNode != null)
          t.memoizedProps !== a && sl(e);
        else {
          if (!a) {
            if (e.stateNode === null)
              throw Error(s(166));
            return Ot(e), null;
          }
          t = Y.current, ja(e) ? fr(e) : (t = Bd(u, a, l), e.stateNode = t, sl(e));
        }
        return Ot(e), null;
      case 5:
        if (Ve(e), u = e.type, t !== null && e.stateNode != null)
          t.memoizedProps !== a && sl(e);
        else {
          if (!a) {
            if (e.stateNode === null)
              throw Error(s(166));
            return Ot(e), null;
          }
          if (n = Y.current, ja(e))
            fr(e);
          else {
            var i = yi(
              et.current
            );
            switch (n) {
              case 1:
                n = i.createElementNS(
                  "http://www.w3.org/2000/svg",
                  u
                );
                break;
              case 2:
                n = i.createElementNS(
                  "http://www.w3.org/1998/Math/MathML",
                  u
                );
                break;
              default:
                switch (u) {
                  case "svg":
                    n = i.createElementNS(
                      "http://www.w3.org/2000/svg",
                      u
                    );
                    break;
                  case "math":
                    n = i.createElementNS(
                      "http://www.w3.org/1998/Math/MathML",
                      u
                    );
                    break;
                  case "script":
                    n = i.createElement("div"), n.innerHTML = "<script><\/script>", n = n.removeChild(
                      n.firstChild
                    );
                    break;
                  case "select":
                    n = typeof a.is == "string" ? i.createElement("select", {
                      is: a.is
                    }) : i.createElement("select"), a.multiple ? n.multiple = !0 : a.size && (n.size = a.size);
                    break;
                  default:
                    n = typeof a.is == "string" ? i.createElement(u, { is: a.is }) : i.createElement(u);
                }
            }
            n[$t] = e, n[ce] = a;
            t: for (i = e.child; i !== null; ) {
              if (i.tag === 5 || i.tag === 6)
                n.appendChild(i.stateNode);
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
            e.stateNode = n;
            t: switch (Pt(n, u, a), u) {
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
        return Ot(e), Pc(
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
          if (t = et.current, ja(e)) {
            if (t = e.stateNode, l = e.memoizedProps, a = null, u = Wt, u !== null)
              switch (u.tag) {
                case 27:
                case 5:
                  a = u.memoizedProps;
              }
            t[$t] = e, t = !!(t.nodeValue === l || a !== null && a.suppressHydrationWarning === !0 || xd(t.nodeValue, l)), t || xl(e, !0);
          } else
            t = yi(t).createTextNode(
              a
            ), t[$t] = e, e.stateNode = t;
        }
        return Ot(e), null;
      case 31:
        if (l = e.memoizedState, t === null || t.memoizedState !== null) {
          if (a = ja(e), l !== null) {
            if (t === null) {
              if (!a) throw Error(s(318));
              if (t = e.memoizedState, t = t !== null ? t.dehydrated : null, !t) throw Error(s(557));
              t[$t] = e;
            } else
              aa(), (e.flags & 128) === 0 && (e.memoizedState = null), e.flags |= 4;
            Ot(e), t = !1;
          } else
            l = sc(), t !== null && t.memoizedState !== null && (t.memoizedState.hydrationErrors = l), t = !0;
          if (!t)
            return e.flags & 256 ? (_e(e), e) : (_e(e), null);
          if ((e.flags & 128) !== 0)
            throw Error(s(558));
        }
        return Ot(e), null;
      case 13:
        if (a = e.memoizedState, t === null || t.memoizedState !== null && t.memoizedState.dehydrated !== null) {
          if (u = ja(e), a !== null && a.dehydrated !== null) {
            if (t === null) {
              if (!u) throw Error(s(318));
              if (u = e.memoizedState, u = u !== null ? u.dehydrated : null, !u) throw Error(s(317));
              u[$t] = e;
            } else
              aa(), (e.flags & 128) === 0 && (e.memoizedState = null), e.flags |= 4;
            Ot(e), u = !1;
          } else
            u = sc(), t !== null && t.memoizedState !== null && (t.memoizedState.hydrationErrors = u), u = !0;
          if (!u)
            return e.flags & 256 ? (_e(e), e) : (_e(e), null);
        }
        return _e(e), (e.flags & 128) !== 0 ? (e.lanes = l, e) : (l = a !== null, t = t !== null && t.memoizedState !== null, l && (a = e.child, u = null, a.alternate !== null && a.alternate.memoizedState !== null && a.alternate.memoizedState.cachePool !== null && (u = a.alternate.memoizedState.cachePool.pool), n = null, a.memoizedState !== null && a.memoizedState.cachePool !== null && (n = a.memoizedState.cachePool.pool), n !== u && (a.flags |= 2048)), l !== t && l && (e.child.flags |= 8192), In(e, e.updateQueue), Ot(e), null);
      case 4:
        return Dt(), t === null && Ef(e.stateNode.containerInfo), Ot(e), null;
      case 10:
        return nl(e.type), Ot(e), null;
      case 19:
        if (O(Ht), a = e.memoizedState, a === null) return Ot(e), null;
        if (u = (e.flags & 128) !== 0, n = a.rendering, n === null)
          if (u) Gu(a, !1);
          else {
            if (jt !== 0 || t !== null && (t.flags & 128) !== 0)
              for (t = e.child; t !== null; ) {
                if (n = Ln(t), n !== null) {
                  for (e.flags |= 128, Gu(a, !1), t = n.updateQueue, e.updateQueue = t, In(e, t), e.subtreeFlags = 0, t = l, l = e.child; l !== null; )
                    ar(l, t), l = l.sibling;
                  return R(
                    Ht,
                    Ht.current & 1 | 2
                  ), ot && al(e, a.treeForkCount), e.child;
                }
                t = t.sibling;
              }
            a.tail !== null && bt() > ai && (e.flags |= 128, u = !0, Gu(a, !1), e.lanes = 4194304);
          }
        else {
          if (!u)
            if (t = Ln(n), t !== null) {
              if (e.flags |= 128, u = !0, t = t.updateQueue, e.updateQueue = t, In(e, t), Gu(a, !0), a.tail === null && a.tailMode === "hidden" && !n.alternate && !ot)
                return Ot(e), null;
            } else
              2 * bt() - a.renderingStartTime > ai && l !== 536870912 && (e.flags |= 128, u = !0, Gu(a, !1), e.lanes = 4194304);
          a.isBackwards ? (n.sibling = e.child, e.child = n) : (t = a.last, t !== null ? t.sibling = n : e.child = n, a.last = n);
        }
        return a.tail !== null ? (t = a.tail, a.rendering = t, a.tail = t.sibling, a.renderingStartTime = bt(), t.sibling = null, l = Ht.current, R(
          Ht,
          u ? l & 1 | 2 : l & 1
        ), ot && al(e, a.treeForkCount), t) : (Ot(e), null);
      case 22:
      case 23:
        return _e(e), Ec(), a = e.memoizedState !== null, t !== null ? t.memoizedState !== null !== a && (e.flags |= 8192) : a && (e.flags |= 8192), a ? (l & 536870912) !== 0 && (e.flags & 128) === 0 && (Ot(e), e.subtreeFlags & 6 && (e.flags |= 8192)) : Ot(e), l = e.updateQueue, l !== null && In(e, l.retryQueue), l = null, t !== null && t.memoizedState !== null && t.memoizedState.cachePool !== null && (l = t.memoizedState.cachePool.pool), a = null, e.memoizedState !== null && e.memoizedState.cachePool !== null && (a = e.memoizedState.cachePool.pool), a !== l && (e.flags |= 2048), t !== null && O(ia), null;
      case 24:
        return l = null, t !== null && (l = t.memoizedState.cache), e.memoizedState.cache !== l && (e.flags |= 2048), nl(Gt), Ot(e), null;
      case 25:
        return null;
      case 30:
        return null;
    }
    throw Error(s(156, e.tag));
  }
  function Km(t, e) {
    switch (cc(e), e.tag) {
      case 1:
        return t = e.flags, t & 65536 ? (e.flags = t & -65537 | 128, e) : null;
      case 3:
        return nl(Gt), Dt(), t = e.flags, (t & 65536) !== 0 && (t & 128) === 0 ? (e.flags = t & -65537 | 128, e) : null;
      case 26:
      case 27:
      case 5:
        return Ve(e), null;
      case 31:
        if (e.memoizedState !== null) {
          if (_e(e), e.alternate === null)
            throw Error(s(340));
          aa();
        }
        return t = e.flags, t & 65536 ? (e.flags = t & -65537 | 128, e) : null;
      case 13:
        if (_e(e), t = e.memoizedState, t !== null && t.dehydrated !== null) {
          if (e.alternate === null)
            throw Error(s(340));
          aa();
        }
        return t = e.flags, t & 65536 ? (e.flags = t & -65537 | 128, e) : null;
      case 19:
        return O(Ht), null;
      case 4:
        return Dt(), null;
      case 10:
        return nl(e.type), null;
      case 22:
      case 23:
        return _e(e), Ec(), t !== null && O(ia), t = e.flags, t & 65536 ? (e.flags = t & -65537 | 128, e) : null;
      case 24:
        return nl(Gt), null;
      case 25:
        return null;
      default:
        return null;
    }
  }
  function Do(t, e) {
    switch (cc(e), e.tag) {
      case 3:
        nl(Gt), Dt();
        break;
      case 26:
      case 27:
      case 5:
        Ve(e);
        break;
      case 4:
        Dt();
        break;
      case 31:
        e.memoizedState !== null && _e(e);
        break;
      case 13:
        _e(e);
        break;
      case 19:
        O(Ht);
        break;
      case 10:
        nl(e.type);
        break;
      case 22:
      case 23:
        _e(e), Ec(), t !== null && O(ia);
        break;
      case 24:
        nl(Gt);
    }
  }
  function Qu(t, e) {
    try {
      var l = e.updateQueue, a = l !== null ? l.lastEffect : null;
      if (a !== null) {
        var u = a.next;
        l = u;
        do {
          if ((l.tag & t) === t) {
            a = void 0;
            var n = l.create, i = l.inst;
            a = n(), i.destroy = a;
          }
          l = l.next;
        } while (l !== u);
      }
    } catch (o) {
      Et(e, e.return, o);
    }
  }
  function Cl(t, e, l) {
    try {
      var a = e.updateQueue, u = a !== null ? a.lastEffect : null;
      if (u !== null) {
        var n = u.next;
        a = n;
        do {
          if ((a.tag & t) === t) {
            var i = a.inst, o = i.destroy;
            if (o !== void 0) {
              i.destroy = void 0, u = e;
              var d = l, S = o;
              try {
                S();
              } catch (T) {
                Et(
                  u,
                  d,
                  T
                );
              }
            }
          }
          a = a.next;
        } while (a !== n);
      }
    } catch (T) {
      Et(e, e.return, T);
    }
  }
  function Uo(t) {
    var e = t.updateQueue;
    if (e !== null) {
      var l = t.stateNode;
      try {
        _r(e, l);
      } catch (a) {
        Et(t, t.return, a);
      }
    }
  }
  function Co(t, e, l) {
    l.props = oa(
      t.type,
      t.memoizedProps
    ), l.state = t.memoizedState;
    try {
      l.componentWillUnmount();
    } catch (a) {
      Et(t, e, a);
    }
  }
  function Xu(t, e) {
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
    } catch (u) {
      Et(t, e, u);
    }
  }
  function ke(t, e) {
    var l = t.ref, a = t.refCleanup;
    if (l !== null)
      if (typeof a == "function")
        try {
          a();
        } catch (u) {
          Et(t, e, u);
        } finally {
          t.refCleanup = null, t = t.alternate, t != null && (t.refCleanup = null);
        }
      else if (typeof l == "function")
        try {
          l(null);
        } catch (u) {
          Et(t, e, u);
        }
      else l.current = null;
  }
  function jo(t) {
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
    } catch (u) {
      Et(t, t.return, u);
    }
  }
  function tf(t, e, l) {
    try {
      var a = t.stateNode;
      yh(a, t.type, l, e), a[ce] = e;
    } catch (u) {
      Et(t, t.return, u);
    }
  }
  function Ro(t) {
    return t.tag === 5 || t.tag === 3 || t.tag === 26 || t.tag === 27 && Gl(t.type) || t.tag === 4;
  }
  function ef(t) {
    t: for (; ; ) {
      for (; t.sibling === null; ) {
        if (t.return === null || Ro(t.return)) return null;
        t = t.return;
      }
      for (t.sibling.return = t.return, t = t.sibling; t.tag !== 5 && t.tag !== 6 && t.tag !== 18; ) {
        if (t.tag === 27 && Gl(t.type) || t.flags & 2 || t.child === null || t.tag === 4) continue t;
        t.child.return = t, t = t.child;
      }
      if (!(t.flags & 2)) return t.stateNode;
    }
  }
  function lf(t, e, l) {
    var a = t.tag;
    if (a === 5 || a === 6)
      t = t.stateNode, e ? (l.nodeType === 9 ? l.body : l.nodeName === "HTML" ? l.ownerDocument.body : l).insertBefore(t, e) : (e = l.nodeType === 9 ? l.body : l.nodeName === "HTML" ? l.ownerDocument.body : l, e.appendChild(t), l = l._reactRootContainer, l != null || e.onclick !== null || (e.onclick = tl));
    else if (a !== 4 && (a === 27 && Gl(t.type) && (l = t.stateNode, e = null), t = t.child, t !== null))
      for (lf(t, e, l), t = t.sibling; t !== null; )
        lf(t, e, l), t = t.sibling;
  }
  function Pn(t, e, l) {
    var a = t.tag;
    if (a === 5 || a === 6)
      t = t.stateNode, e ? l.insertBefore(t, e) : l.appendChild(t);
    else if (a !== 4 && (a === 27 && Gl(t.type) && (l = t.stateNode), t = t.child, t !== null))
      for (Pn(t, e, l), t = t.sibling; t !== null; )
        Pn(t, e, l), t = t.sibling;
  }
  function Ho(t) {
    var e = t.stateNode, l = t.memoizedProps;
    try {
      for (var a = t.type, u = e.attributes; u.length; )
        e.removeAttributeNode(u[0]);
      Pt(e, a, l), e[$t] = t, e[ce] = l;
    } catch (n) {
      Et(t, t.return, n);
    }
  }
  var rl = !1, Zt = !1, af = !1, Bo = typeof WeakSet == "function" ? WeakSet : Set, kt = null;
  function Jm(t, e) {
    if (t = t.containerInfo, Tf = Si, t = ks(t), Wi(t)) {
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
            var u = a.anchorOffset, n = a.focusNode;
            a = a.focusOffset;
            try {
              l.nodeType, n.nodeType;
            } catch {
              l = null;
              break t;
            }
            var i = 0, o = -1, d = -1, S = 0, T = 0, U = t, _ = null;
            e: for (; ; ) {
              for (var A; U !== l || u !== 0 && U.nodeType !== 3 || (o = i + u), U !== n || a !== 0 && U.nodeType !== 3 || (d = i + a), U.nodeType === 3 && (i += U.nodeValue.length), (A = U.firstChild) !== null; )
                _ = U, U = A;
              for (; ; ) {
                if (U === t) break e;
                if (_ === l && ++S === u && (o = i), _ === n && ++T === a && (d = i), (A = U.nextSibling) !== null) break;
                U = _, _ = U.parentNode;
              }
              U = A;
            }
            l = o === -1 || d === -1 ? null : { start: o, end: d };
          } else l = null;
        }
      l = l || { start: 0, end: 0 };
    } else l = null;
    for (xf = { focusedElem: t, selectionRange: l }, Si = !1, kt = e; kt !== null; )
      if (e = kt, t = e.child, (e.subtreeFlags & 1028) !== 0 && t !== null)
        t.return = e, kt = t;
      else
        for (; kt !== null; ) {
          switch (e = kt, n = e.alternate, t = e.flags, e.tag) {
            case 0:
              if ((t & 4) !== 0 && (t = e.updateQueue, t = t !== null ? t.events : null, t !== null))
                for (l = 0; l < t.length; l++)
                  u = t[l], u.ref.impl = u.nextImpl;
              break;
            case 11:
            case 15:
              break;
            case 1:
              if ((t & 1024) !== 0 && n !== null) {
                t = void 0, l = e, u = n.memoizedProps, n = n.memoizedState, a = l.stateNode;
                try {
                  var L = oa(
                    l.type,
                    u
                  );
                  t = a.getSnapshotBeforeUpdate(
                    L,
                    n
                  ), a.__reactInternalSnapshotBeforeUpdate = t;
                } catch ($) {
                  Et(
                    l,
                    l.return,
                    $
                  );
                }
              }
              break;
            case 3:
              if ((t & 1024) !== 0) {
                if (t = e.stateNode.containerInfo, l = t.nodeType, l === 9)
                  Of(t);
                else if (l === 1)
                  switch (t.nodeName) {
                    case "HEAD":
                    case "HTML":
                    case "BODY":
                      Of(t);
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
            t.return = e.return, kt = t;
            break;
          }
          kt = e.return;
        }
  }
  function Yo(t, e, l) {
    var a = l.flags;
    switch (l.tag) {
      case 0:
      case 11:
      case 15:
        dl(t, l), a & 4 && Qu(5, l);
        break;
      case 1:
        if (dl(t, l), a & 4)
          if (t = l.stateNode, e === null)
            try {
              t.componentDidMount();
            } catch (i) {
              Et(l, l.return, i);
            }
          else {
            var u = oa(
              l.type,
              e.memoizedProps
            );
            e = e.memoizedState;
            try {
              t.componentDidUpdate(
                u,
                e,
                t.__reactInternalSnapshotBeforeUpdate
              );
            } catch (i) {
              Et(
                l,
                l.return,
                i
              );
            }
          }
        a & 64 && Uo(l), a & 512 && Xu(l, l.return);
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
            _r(t, e);
          } catch (i) {
            Et(l, l.return, i);
          }
        }
        break;
      case 27:
        e === null && a & 4 && Ho(l);
      case 26:
      case 5:
        dl(t, l), e === null && a & 4 && jo(l), a & 512 && Xu(l, l.return);
        break;
      case 12:
        dl(t, l);
        break;
      case 31:
        dl(t, l), a & 4 && Qo(t, l);
        break;
      case 13:
        dl(t, l), a & 4 && Xo(t, l), a & 64 && (t = l.memoizedState, t !== null && (t = t.dehydrated, t !== null && (l = eh.bind(
          null,
          l
        ), _h(t, l))));
        break;
      case 22:
        if (a = l.memoizedState !== null || rl, !a) {
          e = e !== null && e.memoizedState !== null || Zt, u = rl;
          var n = Zt;
          rl = a, (Zt = e) && !n ? yl(
            t,
            l,
            (l.subtreeFlags & 8772) !== 0
          ) : dl(t, l), rl = u, Zt = n;
        }
        break;
      case 30:
        break;
      default:
        dl(t, l);
    }
  }
  function Lo(t) {
    var e = t.alternate;
    e !== null && (t.alternate = null, Lo(e)), t.child = null, t.deletions = null, t.sibling = null, t.tag === 5 && (e = t.stateNode, e !== null && Ci(e)), t.stateNode = null, t.return = null, t.dependencies = null, t.memoizedProps = null, t.memoizedState = null, t.pendingProps = null, t.stateNode = null, t.updateQueue = null;
  }
  var Mt = null, se = !1;
  function ol(t, e, l) {
    for (l = l.child; l !== null; )
      Go(t, e, l), l = l.sibling;
  }
  function Go(t, e, l) {
    if (ve && typeof ve.onCommitFiberUnmount == "function")
      try {
        ve.onCommitFiberUnmount(du, l);
      } catch {
      }
    switch (l.tag) {
      case 26:
        Zt || ke(l, e), ol(
          t,
          e,
          l
        ), l.memoizedState ? l.memoizedState.count-- : l.stateNode && (l = l.stateNode, l.parentNode.removeChild(l));
        break;
      case 27:
        Zt || ke(l, e);
        var a = Mt, u = se;
        Gl(l.type) && (Mt = l.stateNode, se = !1), ol(
          t,
          e,
          l
        ), Fu(l.stateNode), Mt = a, se = u;
        break;
      case 5:
        Zt || ke(l, e);
      case 6:
        if (a = Mt, u = se, Mt = null, ol(
          t,
          e,
          l
        ), Mt = a, se = u, Mt !== null)
          if (se)
            try {
              (Mt.nodeType === 9 ? Mt.body : Mt.nodeName === "HTML" ? Mt.ownerDocument.body : Mt).removeChild(l.stateNode);
            } catch (n) {
              Et(
                l,
                e,
                n
              );
            }
          else
            try {
              Mt.removeChild(l.stateNode);
            } catch (n) {
              Et(
                l,
                e,
                n
              );
            }
        break;
      case 18:
        Mt !== null && (se ? (t = Mt, Ud(
          t.nodeType === 9 ? t.body : t.nodeName === "HTML" ? t.ownerDocument.body : t,
          l.stateNode
        ), eu(t)) : Ud(Mt, l.stateNode));
        break;
      case 4:
        a = Mt, u = se, Mt = l.stateNode.containerInfo, se = !0, ol(
          t,
          e,
          l
        ), Mt = a, se = u;
        break;
      case 0:
      case 11:
      case 14:
      case 15:
        Cl(2, l, e), Zt || Cl(4, l, e), ol(
          t,
          e,
          l
        );
        break;
      case 1:
        Zt || (ke(l, e), a = l.stateNode, typeof a.componentWillUnmount == "function" && Co(
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
        Zt = (a = Zt) || l.memoizedState !== null, ol(
          t,
          e,
          l
        ), Zt = a;
        break;
      default:
        ol(
          t,
          e,
          l
        );
    }
  }
  function Qo(t, e) {
    if (e.memoizedState === null && (t = e.alternate, t !== null && (t = t.memoizedState, t !== null))) {
      t = t.dehydrated;
      try {
        eu(t);
      } catch (l) {
        Et(e, e.return, l);
      }
    }
  }
  function Xo(t, e) {
    if (e.memoizedState === null && (t = e.alternate, t !== null && (t = t.memoizedState, t !== null && (t = t.dehydrated, t !== null))))
      try {
        eu(t);
      } catch (l) {
        Et(e, e.return, l);
      }
  }
  function wm(t) {
    switch (t.tag) {
      case 31:
      case 13:
      case 19:
        var e = t.stateNode;
        return e === null && (e = t.stateNode = new Bo()), e;
      case 22:
        return t = t.stateNode, e = t._retryCache, e === null && (e = t._retryCache = new Bo()), e;
      default:
        throw Error(s(435, t.tag));
    }
  }
  function ti(t, e) {
    var l = wm(t);
    e.forEach(function(a) {
      if (!l.has(a)) {
        l.add(a);
        var u = lh.bind(null, t, a);
        a.then(u, u);
      }
    });
  }
  function re(t, e) {
    var l = e.deletions;
    if (l !== null)
      for (var a = 0; a < l.length; a++) {
        var u = l[a], n = t, i = e, o = i;
        t: for (; o !== null; ) {
          switch (o.tag) {
            case 27:
              if (Gl(o.type)) {
                Mt = o.stateNode, se = !1;
                break t;
              }
              break;
            case 5:
              Mt = o.stateNode, se = !1;
              break t;
            case 3:
            case 4:
              Mt = o.stateNode.containerInfo, se = !0;
              break t;
          }
          o = o.return;
        }
        if (Mt === null) throw Error(s(160));
        Go(n, i, u), Mt = null, se = !1, n = u.alternate, n !== null && (n.return = null), u.return = null;
      }
    if (e.subtreeFlags & 13886)
      for (e = e.child; e !== null; )
        Zo(e, t), e = e.sibling;
  }
  var Ge = null;
  function Zo(t, e) {
    var l = t.alternate, a = t.flags;
    switch (t.tag) {
      case 0:
      case 11:
      case 14:
      case 15:
        re(e, t), oe(t), a & 4 && (Cl(3, t, t.return), Qu(3, t), Cl(5, t, t.return));
        break;
      case 1:
        re(e, t), oe(t), a & 512 && (Zt || l === null || ke(l, l.return)), a & 64 && rl && (t = t.updateQueue, t !== null && (a = t.callbacks, a !== null && (l = t.shared.hiddenCallbacks, t.shared.hiddenCallbacks = l === null ? a : l.concat(a))));
        break;
      case 26:
        var u = Ge;
        if (re(e, t), oe(t), a & 512 && (Zt || l === null || ke(l, l.return)), a & 4) {
          var n = l !== null ? l.memoizedState : null;
          if (a = t.memoizedState, l === null)
            if (a === null)
              if (t.stateNode === null) {
                t: {
                  a = t.type, l = t.memoizedProps, u = u.ownerDocument || u;
                  e: switch (a) {
                    case "title":
                      n = u.getElementsByTagName("title")[0], (!n || n[hu] || n[$t] || n.namespaceURI === "http://www.w3.org/2000/svg" || n.hasAttribute("itemprop")) && (n = u.createElement(a), u.head.insertBefore(
                        n,
                        u.querySelector("head > title")
                      )), Pt(n, a, l), n[$t] = t, wt(n), a = n;
                      break t;
                    case "link":
                      var i = Zd(
                        "link",
                        "href",
                        u
                      ).get(a + (l.href || ""));
                      if (i) {
                        for (var o = 0; o < i.length; o++)
                          if (n = i[o], n.getAttribute("href") === (l.href == null || l.href === "" ? null : l.href) && n.getAttribute("rel") === (l.rel == null ? null : l.rel) && n.getAttribute("title") === (l.title == null ? null : l.title) && n.getAttribute("crossorigin") === (l.crossOrigin == null ? null : l.crossOrigin)) {
                            i.splice(o, 1);
                            break e;
                          }
                      }
                      n = u.createElement(a), Pt(n, a, l), u.head.appendChild(n);
                      break;
                    case "meta":
                      if (i = Zd(
                        "meta",
                        "content",
                        u
                      ).get(a + (l.content || ""))) {
                        for (o = 0; o < i.length; o++)
                          if (n = i[o], n.getAttribute("content") === (l.content == null ? null : "" + l.content) && n.getAttribute("name") === (l.name == null ? null : l.name) && n.getAttribute("property") === (l.property == null ? null : l.property) && n.getAttribute("http-equiv") === (l.httpEquiv == null ? null : l.httpEquiv) && n.getAttribute("charset") === (l.charSet == null ? null : l.charSet)) {
                            i.splice(o, 1);
                            break e;
                          }
                      }
                      n = u.createElement(a), Pt(n, a, l), u.head.appendChild(n);
                      break;
                    default:
                      throw Error(s(468, a));
                  }
                  n[$t] = t, wt(n), a = n;
                }
                t.stateNode = a;
              } else
                Vd(
                  u,
                  t.type,
                  t.stateNode
                );
            else
              t.stateNode = Xd(
                u,
                a,
                t.memoizedProps
              );
          else
            n !== a ? (n === null ? l.stateNode !== null && (l = l.stateNode, l.parentNode.removeChild(l)) : n.count--, a === null ? Vd(
              u,
              t.type,
              t.stateNode
            ) : Xd(
              u,
              a,
              t.memoizedProps
            )) : a === null && t.stateNode !== null && tf(
              t,
              t.memoizedProps,
              l.memoizedProps
            );
        }
        break;
      case 27:
        re(e, t), oe(t), a & 512 && (Zt || l === null || ke(l, l.return)), l !== null && a & 4 && tf(
          t,
          t.memoizedProps,
          l.memoizedProps
        );
        break;
      case 5:
        if (re(e, t), oe(t), a & 512 && (Zt || l === null || ke(l, l.return)), t.flags & 32) {
          u = t.stateNode;
          try {
            za(u, "");
          } catch (L) {
            Et(t, t.return, L);
          }
        }
        a & 4 && t.stateNode != null && (u = t.memoizedProps, tf(
          t,
          u,
          l !== null ? l.memoizedProps : u
        )), a & 1024 && (af = !0);
        break;
      case 6:
        if (re(e, t), oe(t), a & 4) {
          if (t.stateNode === null)
            throw Error(s(162));
          a = t.memoizedProps, l = t.stateNode;
          try {
            l.nodeValue = a;
          } catch (L) {
            Et(t, t.return, L);
          }
        }
        break;
      case 3:
        if (vi = null, u = Ge, Ge = mi(e.containerInfo), re(e, t), Ge = u, oe(t), a & 4 && l !== null && l.memoizedState.isDehydrated)
          try {
            eu(e.containerInfo);
          } catch (L) {
            Et(t, t.return, L);
          }
        af && (af = !1, Vo(t));
        break;
      case 4:
        a = Ge, Ge = mi(
          t.stateNode.containerInfo
        ), re(e, t), oe(t), Ge = a;
        break;
      case 12:
        re(e, t), oe(t);
        break;
      case 31:
        re(e, t), oe(t), a & 4 && (a = t.updateQueue, a !== null && (t.updateQueue = null, ti(t, a)));
        break;
      case 13:
        re(e, t), oe(t), t.child.flags & 8192 && t.memoizedState !== null != (l !== null && l.memoizedState !== null) && (li = bt()), a & 4 && (a = t.updateQueue, a !== null && (t.updateQueue = null, ti(t, a)));
        break;
      case 22:
        u = t.memoizedState !== null;
        var d = l !== null && l.memoizedState !== null, S = rl, T = Zt;
        if (rl = S || u, Zt = T || d, re(e, t), Zt = T, rl = S, oe(t), a & 8192)
          t: for (e = t.stateNode, e._visibility = u ? e._visibility & -2 : e._visibility | 1, u && (l === null || d || rl || Zt || da(t)), l = null, e = t; ; ) {
            if (e.tag === 5 || e.tag === 26) {
              if (l === null) {
                d = l = e;
                try {
                  if (n = d.stateNode, u)
                    i = n.style, typeof i.setProperty == "function" ? i.setProperty("display", "none", "important") : i.display = "none";
                  else {
                    o = d.stateNode;
                    var U = d.memoizedProps.style, _ = U != null && U.hasOwnProperty("display") ? U.display : null;
                    o.style.display = _ == null || typeof _ == "boolean" ? "" : ("" + _).trim();
                  }
                } catch (L) {
                  Et(d, d.return, L);
                }
              }
            } else if (e.tag === 6) {
              if (l === null) {
                d = e;
                try {
                  d.stateNode.nodeValue = u ? "" : d.memoizedProps;
                } catch (L) {
                  Et(d, d.return, L);
                }
              }
            } else if (e.tag === 18) {
              if (l === null) {
                d = e;
                try {
                  var A = d.stateNode;
                  u ? Cd(A, !0) : Cd(d.stateNode, !1);
                } catch (L) {
                  Et(d, d.return, L);
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
        a & 4 && (a = t.updateQueue, a !== null && (l = a.retryQueue, l !== null && (a.retryQueue = null, ti(t, l))));
        break;
      case 19:
        re(e, t), oe(t), a & 4 && (a = t.updateQueue, a !== null && (t.updateQueue = null, ti(t, a)));
        break;
      case 30:
        break;
      case 21:
        break;
      default:
        re(e, t), oe(t);
    }
  }
  function oe(t) {
    var e = t.flags;
    if (e & 2) {
      try {
        for (var l, a = t.return; a !== null; ) {
          if (Ro(a)) {
            l = a;
            break;
          }
          a = a.return;
        }
        if (l == null) throw Error(s(160));
        switch (l.tag) {
          case 27:
            var u = l.stateNode, n = ef(t);
            Pn(t, n, u);
            break;
          case 5:
            var i = l.stateNode;
            l.flags & 32 && (za(i, ""), l.flags &= -33);
            var o = ef(t);
            Pn(t, o, i);
            break;
          case 3:
          case 4:
            var d = l.stateNode.containerInfo, S = ef(t);
            lf(
              t,
              S,
              d
            );
            break;
          default:
            throw Error(s(161));
        }
      } catch (T) {
        Et(t, t.return, T);
      }
      t.flags &= -3;
    }
    e & 4096 && (t.flags &= -4097);
  }
  function Vo(t) {
    if (t.subtreeFlags & 1024)
      for (t = t.child; t !== null; ) {
        var e = t;
        Vo(e), e.tag === 5 && e.flags & 1024 && e.stateNode.reset(), t = t.sibling;
      }
  }
  function dl(t, e) {
    if (e.subtreeFlags & 8772)
      for (e = e.child; e !== null; )
        Yo(t, e.alternate, e), e = e.sibling;
  }
  function da(t) {
    for (t = t.child; t !== null; ) {
      var e = t;
      switch (e.tag) {
        case 0:
        case 11:
        case 14:
        case 15:
          Cl(4, e, e.return), da(e);
          break;
        case 1:
          ke(e, e.return);
          var l = e.stateNode;
          typeof l.componentWillUnmount == "function" && Co(
            e,
            e.return,
            l
          ), da(e);
          break;
        case 27:
          Fu(e.stateNode);
        case 26:
        case 5:
          ke(e, e.return), da(e);
          break;
        case 22:
          e.memoizedState === null && da(e);
          break;
        case 30:
          da(e);
          break;
        default:
          da(e);
      }
      t = t.sibling;
    }
  }
  function yl(t, e, l) {
    for (l = l && (e.subtreeFlags & 8772) !== 0, e = e.child; e !== null; ) {
      var a = e.alternate, u = t, n = e, i = n.flags;
      switch (n.tag) {
        case 0:
        case 11:
        case 15:
          yl(
            u,
            n,
            l
          ), Qu(4, n);
          break;
        case 1:
          if (yl(
            u,
            n,
            l
          ), a = n, u = a.stateNode, typeof u.componentDidMount == "function")
            try {
              u.componentDidMount();
            } catch (S) {
              Et(a, a.return, S);
            }
          if (a = n, u = a.updateQueue, u !== null) {
            var o = a.stateNode;
            try {
              var d = u.shared.hiddenCallbacks;
              if (d !== null)
                for (u.shared.hiddenCallbacks = null, u = 0; u < d.length; u++)
                  Sr(d[u], o);
            } catch (S) {
              Et(a, a.return, S);
            }
          }
          l && i & 64 && Uo(n), Xu(n, n.return);
          break;
        case 27:
          Ho(n);
        case 26:
        case 5:
          yl(
            u,
            n,
            l
          ), l && a === null && i & 4 && jo(n), Xu(n, n.return);
          break;
        case 12:
          yl(
            u,
            n,
            l
          );
          break;
        case 31:
          yl(
            u,
            n,
            l
          ), l && i & 4 && Qo(u, n);
          break;
        case 13:
          yl(
            u,
            n,
            l
          ), l && i & 4 && Xo(u, n);
          break;
        case 22:
          n.memoizedState === null && yl(
            u,
            n,
            l
          ), Xu(n, n.return);
          break;
        case 30:
          break;
        default:
          yl(
            u,
            n,
            l
          );
      }
      e = e.sibling;
    }
  }
  function uf(t, e) {
    var l = null;
    t !== null && t.memoizedState !== null && t.memoizedState.cachePool !== null && (l = t.memoizedState.cachePool.pool), t = null, e.memoizedState !== null && e.memoizedState.cachePool !== null && (t = e.memoizedState.cachePool.pool), t !== l && (t != null && t.refCount++, l != null && Nu(l));
  }
  function nf(t, e) {
    t = null, e.alternate !== null && (t = e.alternate.memoizedState.cache), e = e.memoizedState.cache, e !== t && (e.refCount++, t != null && Nu(t));
  }
  function Qe(t, e, l, a) {
    if (e.subtreeFlags & 10256)
      for (e = e.child; e !== null; )
        Ko(
          t,
          e,
          l,
          a
        ), e = e.sibling;
  }
  function Ko(t, e, l, a) {
    var u = e.flags;
    switch (e.tag) {
      case 0:
      case 11:
      case 15:
        Qe(
          t,
          e,
          l,
          a
        ), u & 2048 && Qu(9, e);
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
        ), u & 2048 && (t = null, e.alternate !== null && (t = e.alternate.memoizedState.cache), e = e.memoizedState.cache, e !== t && (e.refCount++, t != null && Nu(t)));
        break;
      case 12:
        if (u & 2048) {
          Qe(
            t,
            e,
            l,
            a
          ), t = e.stateNode;
          try {
            var n = e.memoizedProps, i = n.id, o = n.onPostCommit;
            typeof o == "function" && o(
              i,
              e.alternate === null ? "mount" : "update",
              t.passiveEffectDuration,
              -0
            );
          } catch (d) {
            Et(e, e.return, d);
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
        n = e.stateNode, i = e.alternate, e.memoizedState !== null ? n._visibility & 2 ? Qe(
          t,
          e,
          l,
          a
        ) : Zu(t, e) : n._visibility & 2 ? Qe(
          t,
          e,
          l,
          a
        ) : (n._visibility |= 2, Va(
          t,
          e,
          l,
          a,
          (e.subtreeFlags & 10256) !== 0 || !1
        )), u & 2048 && uf(i, e);
        break;
      case 24:
        Qe(
          t,
          e,
          l,
          a
        ), u & 2048 && nf(e.alternate, e);
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
  function Va(t, e, l, a, u) {
    for (u = u && ((e.subtreeFlags & 10256) !== 0 || !1), e = e.child; e !== null; ) {
      var n = t, i = e, o = l, d = a, S = i.flags;
      switch (i.tag) {
        case 0:
        case 11:
        case 15:
          Va(
            n,
            i,
            o,
            d,
            u
          ), Qu(8, i);
          break;
        case 23:
          break;
        case 22:
          var T = i.stateNode;
          i.memoizedState !== null ? T._visibility & 2 ? Va(
            n,
            i,
            o,
            d,
            u
          ) : Zu(
            n,
            i
          ) : (T._visibility |= 2, Va(
            n,
            i,
            o,
            d,
            u
          )), u && S & 2048 && uf(
            i.alternate,
            i
          );
          break;
        case 24:
          Va(
            n,
            i,
            o,
            d,
            u
          ), u && S & 2048 && nf(i.alternate, i);
          break;
        default:
          Va(
            n,
            i,
            o,
            d,
            u
          );
      }
      e = e.sibling;
    }
  }
  function Zu(t, e) {
    if (e.subtreeFlags & 10256)
      for (e = e.child; e !== null; ) {
        var l = t, a = e, u = a.flags;
        switch (a.tag) {
          case 22:
            Zu(l, a), u & 2048 && uf(
              a.alternate,
              a
            );
            break;
          case 24:
            Zu(l, a), u & 2048 && nf(a.alternate, a);
            break;
          default:
            Zu(l, a);
        }
        e = e.sibling;
      }
  }
  var Vu = 8192;
  function Ka(t, e, l) {
    if (t.subtreeFlags & Vu)
      for (t = t.child; t !== null; )
        Jo(
          t,
          e,
          l
        ), t = t.sibling;
  }
  function Jo(t, e, l) {
    switch (t.tag) {
      case 26:
        Ka(
          t,
          e,
          l
        ), t.flags & Vu && t.memoizedState !== null && Ch(
          l,
          Ge,
          t.memoizedState,
          t.memoizedProps
        );
        break;
      case 5:
        Ka(
          t,
          e,
          l
        );
        break;
      case 3:
      case 4:
        var a = Ge;
        Ge = mi(t.stateNode.containerInfo), Ka(
          t,
          e,
          l
        ), Ge = a;
        break;
      case 22:
        t.memoizedState === null && (a = t.alternate, a !== null && a.memoizedState !== null ? (a = Vu, Vu = 16777216, Ka(
          t,
          e,
          l
        ), Vu = a) : Ka(
          t,
          e,
          l
        ));
        break;
      default:
        Ka(
          t,
          e,
          l
        );
    }
  }
  function wo(t) {
    var e = t.alternate;
    if (e !== null && (t = e.child, t !== null)) {
      e.child = null;
      do
        e = t.sibling, t.sibling = null, t = e;
      while (t !== null);
    }
  }
  function Ku(t) {
    var e = t.deletions;
    if ((t.flags & 16) !== 0) {
      if (e !== null)
        for (var l = 0; l < e.length; l++) {
          var a = e[l];
          kt = a, $o(
            a,
            t
          );
        }
      wo(t);
    }
    if (t.subtreeFlags & 10256)
      for (t = t.child; t !== null; )
        ko(t), t = t.sibling;
  }
  function ko(t) {
    switch (t.tag) {
      case 0:
      case 11:
      case 15:
        Ku(t), t.flags & 2048 && Cl(9, t, t.return);
        break;
      case 3:
        Ku(t);
        break;
      case 12:
        Ku(t);
        break;
      case 22:
        var e = t.stateNode;
        t.memoizedState !== null && e._visibility & 2 && (t.return === null || t.return.tag !== 13) ? (e._visibility &= -3, ei(t)) : Ku(t);
        break;
      default:
        Ku(t);
    }
  }
  function ei(t) {
    var e = t.deletions;
    if ((t.flags & 16) !== 0) {
      if (e !== null)
        for (var l = 0; l < e.length; l++) {
          var a = e[l];
          kt = a, $o(
            a,
            t
          );
        }
      wo(t);
    }
    for (t = t.child; t !== null; ) {
      switch (e = t, e.tag) {
        case 0:
        case 11:
        case 15:
          Cl(8, e, e.return), ei(e);
          break;
        case 22:
          l = e.stateNode, l._visibility & 2 && (l._visibility &= -3, ei(e));
          break;
        default:
          ei(e);
      }
      t = t.sibling;
    }
  }
  function $o(t, e) {
    for (; kt !== null; ) {
      var l = kt;
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
          Nu(l.memoizedState.cache);
      }
      if (a = l.child, a !== null) a.return = l, kt = a;
      else
        t: for (l = t; kt !== null; ) {
          a = kt;
          var u = a.sibling, n = a.return;
          if (Lo(a), a === l) {
            kt = null;
            break t;
          }
          if (u !== null) {
            u.return = n, kt = u;
            break t;
          }
          kt = n;
        }
    }
  }
  var km = {
    getCacheForType: function(t) {
      var e = Ft(Gt), l = e.data.get(t);
      return l === void 0 && (l = t(), e.data.set(t, l)), l;
    },
    cacheSignal: function() {
      return Ft(Gt).controller.signal;
    }
  }, $m = typeof WeakMap == "function" ? WeakMap : Map, vt = 0, qt = null, ct = null, st = 0, _t = 0, Ee = null, jl = !1, Ja = !1, cf = !1, ml = 0, jt = 0, Rl = 0, ya = 0, ff = 0, Ae = 0, wa = 0, Ju = null, de = null, sf = !1, li = 0, Wo = 0, ai = 1 / 0, ui = null, Hl = null, Kt = 0, Bl = null, ka = null, hl = 0, rf = 0, of = null, Fo = null, wu = 0, df = null;
  function ze() {
    return (vt & 2) !== 0 && st !== 0 ? st & -st : q.T !== null ? pf() : ds();
  }
  function Io() {
    if (Ae === 0)
      if ((st & 536870912) === 0 || ot) {
        var t = dn;
        dn <<= 1, (dn & 3932160) === 0 && (dn = 262144), Ae = t;
      } else Ae = 536870912;
    return t = Se.current, t !== null && (t.flags |= 32), Ae;
  }
  function ye(t, e, l) {
    (t === qt && (_t === 2 || _t === 9) || t.cancelPendingCommit !== null) && ($a(t, 0), Yl(
      t,
      st,
      Ae,
      !1
    )), mu(t, l), ((vt & 2) === 0 || t !== qt) && (t === qt && ((vt & 2) === 0 && (ya |= l), jt === 4 && Yl(
      t,
      st,
      Ae,
      !1
    )), $e(t));
  }
  function Po(t, e, l) {
    if ((vt & 6) !== 0) throw Error(s(327));
    var a = !l && (e & 127) === 0 && (e & t.expiredLanes) === 0 || yu(t, e), u = a ? Im(t, e) : mf(t, e, !0), n = a;
    do {
      if (u === 0) {
        Ja && !a && Yl(t, e, 0, !1);
        break;
      } else {
        if (l = t.current.alternate, n && !Wm(l)) {
          u = mf(t, e, !1), n = !1;
          continue;
        }
        if (u === 2) {
          if (n = e, t.errorRecoveryDisabledLanes & n)
            var i = 0;
          else
            i = t.pendingLanes & -536870913, i = i !== 0 ? i : i & 536870912 ? 536870912 : 0;
          if (i !== 0) {
            e = i;
            t: {
              var o = t;
              u = Ju;
              var d = o.current.memoizedState.isDehydrated;
              if (d && ($a(o, i).flags |= 256), i = mf(
                o,
                i,
                !1
              ), i !== 2) {
                if (cf && !d) {
                  o.errorRecoveryDisabledLanes |= n, ya |= n, u = 4;
                  break t;
                }
                n = de, de = u, n !== null && (de === null ? de = n : de.push.apply(
                  de,
                  n
                ));
              }
              u = i;
            }
            if (n = !1, u !== 2) continue;
          }
        }
        if (u === 1) {
          $a(t, 0), Yl(t, e, 0, !0);
          break;
        }
        t: {
          switch (a = t, n = u, n) {
            case 0:
            case 1:
              throw Error(s(345));
            case 4:
              if ((e & 4194048) !== e) break;
            case 6:
              Yl(
                a,
                e,
                Ae,
                !jl
              );
              break t;
            case 2:
              de = null;
              break;
            case 3:
            case 5:
              break;
            default:
              throw Error(s(329));
          }
          if ((e & 62914560) === e && (u = li + 300 - bt(), 10 < u)) {
            if (Yl(
              a,
              e,
              Ae,
              !jl
            ), mn(a, 0, !0) !== 0) break t;
            hl = e, a.timeoutHandle = Md(
              td.bind(
                null,
                a,
                l,
                de,
                ui,
                sf,
                e,
                Ae,
                ya,
                wa,
                jl,
                n,
                "Throttled",
                -0,
                0
              ),
              u
            );
            break t;
          }
          td(
            a,
            l,
            de,
            ui,
            sf,
            e,
            Ae,
            ya,
            wa,
            jl,
            n,
            null,
            -0,
            0
          );
        }
      }
      break;
    } while (!0);
    $e(t);
  }
  function td(t, e, l, a, u, n, i, o, d, S, T, U, _, A) {
    if (t.timeoutHandle = -1, U = e.subtreeFlags, U & 8192 || (U & 16785408) === 16785408) {
      U = {
        stylesheets: null,
        count: 0,
        imgCount: 0,
        imgBytes: 0,
        suspenseyImages: [],
        waitingForImages: !0,
        waitingForViewTransition: !1,
        unsuspend: tl
      }, Jo(
        e,
        n,
        U
      );
      var L = (n & 62914560) === n ? li - bt() : (n & 4194048) === n ? Wo - bt() : 0;
      if (L = jh(
        U,
        L
      ), L !== null) {
        hl = n, t.cancelPendingCommit = L(
          fd.bind(
            null,
            t,
            e,
            n,
            l,
            a,
            u,
            i,
            o,
            d,
            T,
            U,
            null,
            _,
            A
          )
        ), Yl(t, n, i, !S);
        return;
      }
    }
    fd(
      t,
      e,
      n,
      l,
      a,
      u,
      i,
      o,
      d
    );
  }
  function Wm(t) {
    for (var e = t; ; ) {
      var l = e.tag;
      if ((l === 0 || l === 11 || l === 15) && e.flags & 16384 && (l = e.updateQueue, l !== null && (l = l.stores, l !== null)))
        for (var a = 0; a < l.length; a++) {
          var u = l[a], n = u.getSnapshot;
          u = u.value;
          try {
            if (!pe(n(), u)) return !1;
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
    e &= ~ff, e &= ~ya, t.suspendedLanes |= e, t.pingedLanes &= ~e, a && (t.warmLanes |= e), a = t.expirationTimes;
    for (var u = e; 0 < u; ) {
      var n = 31 - ge(u), i = 1 << n;
      a[n] = -1, u &= ~i;
    }
    l !== 0 && ss(t, l, e);
  }
  function ni() {
    return (vt & 6) === 0 ? (ku(0), !1) : !0;
  }
  function yf() {
    if (ct !== null) {
      if (_t === 0)
        var t = ct.return;
      else
        t = ct, ul = ua = null, Nc(t), La = null, Mu = 0, t = ct;
      for (; t !== null; )
        Do(t.alternate, t), t = t.return;
      ct = null;
    }
  }
  function $a(t, e) {
    var l = t.timeoutHandle;
    l !== -1 && (t.timeoutHandle = -1, vh(l)), l = t.cancelPendingCommit, l !== null && (t.cancelPendingCommit = null, l()), hl = 0, yf(), qt = t, ct = l = ll(t.current, null), st = e, _t = 0, Ee = null, jl = !1, Ja = yu(t, e), cf = !1, wa = Ae = ff = ya = Rl = jt = 0, de = Ju = null, sf = !1, (e & 8) !== 0 && (e |= e & 32);
    var a = t.entangledLanes;
    if (a !== 0)
      for (t = t.entanglements, a &= e; 0 < a; ) {
        var u = 31 - ge(a), n = 1 << u;
        e |= t[u], a &= ~n;
      }
    return ml = e, xn(), l;
  }
  function ed(t, e) {
    lt = null, q.H = Yu, e === Ya || e === jn ? (e = vr(), _t = 3) : e === vc ? (e = vr(), _t = 4) : _t = e === Vc ? 8 : e !== null && typeof e == "object" && typeof e.then == "function" ? 6 : 1, Ee = e, ct === null && (jt = 1, kn(
      t,
      Ne(e, t.current)
    ));
  }
  function ld() {
    var t = Se.current;
    return t === null ? !0 : (st & 4194048) === st ? Ue === null : (st & 62914560) === st || (st & 536870912) !== 0 ? t === Ue : !1;
  }
  function ad() {
    var t = q.H;
    return q.H = Yu, t === null ? Yu : t;
  }
  function ud() {
    var t = q.A;
    return q.A = km, t;
  }
  function ii() {
    jt = 4, jl || (st & 4194048) !== st && Se.current !== null || (Ja = !0), (Rl & 134217727) === 0 && (ya & 134217727) === 0 || qt === null || Yl(
      qt,
      st,
      Ae,
      !1
    );
  }
  function mf(t, e, l) {
    var a = vt;
    vt |= 2;
    var u = ad(), n = ud();
    (qt !== t || st !== e) && (ui = null, $a(t, e)), e = !1;
    var i = jt;
    t: do
      try {
        if (_t !== 0 && ct !== null) {
          var o = ct, d = Ee;
          switch (_t) {
            case 8:
              yf(), i = 6;
              break t;
            case 3:
            case 2:
            case 9:
            case 6:
              Se.current === null && (e = !0);
              var S = _t;
              if (_t = 0, Ee = null, Wa(t, o, d, S), l && Ja) {
                i = 0;
                break t;
              }
              break;
            default:
              S = _t, _t = 0, Ee = null, Wa(t, o, d, S);
          }
        }
        Fm(), i = jt;
        break;
      } catch (T) {
        ed(t, T);
      }
    while (!0);
    return e && t.shellSuspendCounter++, ul = ua = null, vt = a, q.H = u, q.A = n, ct === null && (qt = null, st = 0, xn()), i;
  }
  function Fm() {
    for (; ct !== null; ) nd(ct);
  }
  function Im(t, e) {
    var l = vt;
    vt |= 2;
    var a = ad(), u = ud();
    qt !== t || st !== e ? (ui = null, ai = bt() + 500, $a(t, e)) : Ja = yu(
      t,
      e
    );
    t: do
      try {
        if (_t !== 0 && ct !== null) {
          e = ct;
          var n = Ee;
          e: switch (_t) {
            case 1:
              _t = 0, Ee = null, Wa(t, e, n, 1);
              break;
            case 2:
            case 9:
              if (mr(n)) {
                _t = 0, Ee = null, id(e);
                break;
              }
              e = function() {
                _t !== 2 && _t !== 9 || qt !== t || (_t = 7), $e(t);
              }, n.then(e, e);
              break t;
            case 3:
              _t = 7;
              break t;
            case 4:
              _t = 5;
              break t;
            case 7:
              mr(n) ? (_t = 0, Ee = null, id(e)) : (_t = 0, Ee = null, Wa(t, e, n, 7));
              break;
            case 5:
              var i = null;
              switch (ct.tag) {
                case 26:
                  i = ct.memoizedState;
                case 5:
                case 27:
                  var o = ct;
                  if (i ? Kd(i) : o.stateNode.complete) {
                    _t = 0, Ee = null;
                    var d = o.sibling;
                    if (d !== null) ct = d;
                    else {
                      var S = o.return;
                      S !== null ? (ct = S, ci(S)) : ct = null;
                    }
                    break e;
                  }
              }
              _t = 0, Ee = null, Wa(t, e, n, 5);
              break;
            case 6:
              _t = 0, Ee = null, Wa(t, e, n, 6);
              break;
            case 8:
              yf(), jt = 6;
              break t;
            default:
              throw Error(s(462));
          }
        }
        Pm();
        break;
      } catch (T) {
        ed(t, T);
      }
    while (!0);
    return ul = ua = null, q.H = a, q.A = u, vt = l, ct !== null ? 0 : (qt = null, st = 0, xn(), jt);
  }
  function Pm() {
    for (; ct !== null && !St(); )
      nd(ct);
  }
  function nd(t) {
    var e = Oo(t.alternate, t, ml);
    t.memoizedProps = t.pendingProps, e === null ? ci(t) : ct = e;
  }
  function id(t) {
    var e = t, l = e.alternate;
    switch (e.tag) {
      case 15:
      case 0:
        e = Ao(
          l,
          e,
          e.pendingProps,
          e.type,
          void 0,
          st
        );
        break;
      case 11:
        e = Ao(
          l,
          e,
          e.pendingProps,
          e.type.render,
          e.ref,
          st
        );
        break;
      case 5:
        Nc(e);
      default:
        Do(l, e), e = ct = ar(e, ml), e = Oo(l, e, ml);
    }
    t.memoizedProps = t.pendingProps, e === null ? ci(t) : ct = e;
  }
  function Wa(t, e, l, a) {
    ul = ua = null, Nc(e), La = null, Mu = 0;
    var u = e.return;
    try {
      if (Qm(
        t,
        u,
        e,
        l,
        st
      )) {
        jt = 1, kn(
          t,
          Ne(l, t.current)
        ), ct = null;
        return;
      }
    } catch (n) {
      if (u !== null) throw ct = u, n;
      jt = 1, kn(
        t,
        Ne(l, t.current)
      ), ct = null;
      return;
    }
    e.flags & 32768 ? (ot || a === 1 ? t = !0 : Ja || (st & 536870912) !== 0 ? t = !1 : (jl = t = !0, (a === 2 || a === 9 || a === 3 || a === 6) && (a = Se.current, a !== null && a.tag === 13 && (a.flags |= 16384))), cd(e, t)) : ci(e);
  }
  function ci(t) {
    var e = t;
    do {
      if ((e.flags & 32768) !== 0) {
        cd(
          e,
          jl
        );
        return;
      }
      t = e.return;
      var l = Vm(
        e.alternate,
        e,
        ml
      );
      if (l !== null) {
        ct = l;
        return;
      }
      if (e = e.sibling, e !== null) {
        ct = e;
        return;
      }
      ct = e = t;
    } while (e !== null);
    jt === 0 && (jt = 5);
  }
  function cd(t, e) {
    do {
      var l = Km(t.alternate, t);
      if (l !== null) {
        l.flags &= 32767, ct = l;
        return;
      }
      if (l = t.return, l !== null && (l.flags |= 32768, l.subtreeFlags = 0, l.deletions = null), !e && (t = t.sibling, t !== null)) {
        ct = t;
        return;
      }
      ct = t = l;
    } while (t !== null);
    jt = 6, ct = null;
  }
  function fd(t, e, l, a, u, n, i, o, d) {
    t.cancelPendingCommit = null;
    do
      fi();
    while (Kt !== 0);
    if ((vt & 6) !== 0) throw Error(s(327));
    if (e !== null) {
      if (e === t.current) throw Error(s(177));
      if (n = e.lanes | e.childLanes, n |= ec, Uy(
        t,
        l,
        n,
        i,
        o,
        d
      ), t === qt && (ct = qt = null, st = 0), ka = e, Bl = t, hl = l, rf = n, of = u, Fo = a, (e.subtreeFlags & 10256) !== 0 || (e.flags & 10256) !== 0 ? (t.callbackNode = null, t.callbackPriority = 0, ah(rn, function() {
        return yd(), null;
      })) : (t.callbackNode = null, t.callbackPriority = 0), a = (e.flags & 13878) !== 0, (e.subtreeFlags & 13878) !== 0 || a) {
        a = q.T, q.T = null, u = H.p, H.p = 2, i = vt, vt |= 4;
        try {
          Jm(t, e, l);
        } finally {
          vt = i, H.p = u, q.T = a;
        }
      }
      Kt = 1, sd(), rd(), od();
    }
  }
  function sd() {
    if (Kt === 1) {
      Kt = 0;
      var t = Bl, e = ka, l = (e.flags & 13878) !== 0;
      if ((e.subtreeFlags & 13878) !== 0 || l) {
        l = q.T, q.T = null;
        var a = H.p;
        H.p = 2;
        var u = vt;
        vt |= 4;
        try {
          Zo(e, t);
          var n = xf, i = ks(t.containerInfo), o = n.focusedElem, d = n.selectionRange;
          if (i !== o && o && o.ownerDocument && ws(
            o.ownerDocument.documentElement,
            o
          )) {
            if (d !== null && Wi(o)) {
              var S = d.start, T = d.end;
              if (T === void 0 && (T = S), "selectionStart" in o)
                o.selectionStart = S, o.selectionEnd = Math.min(
                  T,
                  o.value.length
                );
              else {
                var U = o.ownerDocument || document, _ = U && U.defaultView || window;
                if (_.getSelection) {
                  var A = _.getSelection(), L = o.textContent.length, $ = Math.min(d.start, L), Tt = d.end === void 0 ? $ : Math.min(d.end, L);
                  !A.extend && $ > Tt && (i = Tt, Tt = $, $ = i);
                  var v = Js(
                    o,
                    $
                  ), y = Js(
                    o,
                    Tt
                  );
                  if (v && y && (A.rangeCount !== 1 || A.anchorNode !== v.node || A.anchorOffset !== v.offset || A.focusNode !== y.node || A.focusOffset !== y.offset)) {
                    var b = U.createRange();
                    b.setStart(v.node, v.offset), A.removeAllRanges(), $ > Tt ? (A.addRange(b), A.extend(y.node, y.offset)) : (b.setEnd(y.node, y.offset), A.addRange(b));
                  }
                }
              }
            }
            for (U = [], A = o; A = A.parentNode; )
              A.nodeType === 1 && U.push({
                element: A,
                left: A.scrollLeft,
                top: A.scrollTop
              });
            for (typeof o.focus == "function" && o.focus(), o = 0; o < U.length; o++) {
              var M = U[o];
              M.element.scrollLeft = M.left, M.element.scrollTop = M.top;
            }
          }
          Si = !!Tf, xf = Tf = null;
        } finally {
          vt = u, H.p = a, q.T = l;
        }
      }
      t.current = e, Kt = 2;
    }
  }
  function rd() {
    if (Kt === 2) {
      Kt = 0;
      var t = Bl, e = ka, l = (e.flags & 8772) !== 0;
      if ((e.subtreeFlags & 8772) !== 0 || l) {
        l = q.T, q.T = null;
        var a = H.p;
        H.p = 2;
        var u = vt;
        vt |= 4;
        try {
          Yo(t, e.alternate, e);
        } finally {
          vt = u, H.p = a, q.T = l;
        }
      }
      Kt = 3;
    }
  }
  function od() {
    if (Kt === 4 || Kt === 3) {
      Kt = 0, Lt();
      var t = Bl, e = ka, l = hl, a = Fo;
      (e.subtreeFlags & 10256) !== 0 || (e.flags & 10256) !== 0 ? Kt = 5 : (Kt = 0, ka = Bl = null, dd(t, t.pendingLanes));
      var u = t.pendingLanes;
      if (u === 0 && (Hl = null), Di(l), e = e.stateNode, ve && typeof ve.onCommitFiberRoot == "function")
        try {
          ve.onCommitFiberRoot(
            du,
            e,
            void 0,
            (e.current.flags & 128) === 128
          );
        } catch {
        }
      if (a !== null) {
        e = q.T, u = H.p, H.p = 2, q.T = null;
        try {
          for (var n = t.onRecoverableError, i = 0; i < a.length; i++) {
            var o = a[i];
            n(o.value, {
              componentStack: o.stack
            });
          }
        } finally {
          q.T = e, H.p = u;
        }
      }
      (hl & 3) !== 0 && fi(), $e(t), u = t.pendingLanes, (l & 261930) !== 0 && (u & 42) !== 0 ? t === df ? wu++ : (wu = 0, df = t) : wu = 0, ku(0);
    }
  }
  function dd(t, e) {
    (t.pooledCacheLanes &= e) === 0 && (e = t.pooledCache, e != null && (t.pooledCache = null, Nu(e)));
  }
  function fi() {
    return sd(), rd(), od(), yd();
  }
  function yd() {
    if (Kt !== 5) return !1;
    var t = Bl, e = rf;
    rf = 0;
    var l = Di(hl), a = q.T, u = H.p;
    try {
      H.p = 32 > l ? 32 : l, q.T = null, l = of, of = null;
      var n = Bl, i = hl;
      if (Kt = 0, ka = Bl = null, hl = 0, (vt & 6) !== 0) throw Error(s(331));
      var o = vt;
      if (vt |= 4, ko(n.current), Ko(
        n,
        n.current,
        i,
        l
      ), vt = o, ku(0, !1), ve && typeof ve.onPostCommitFiberRoot == "function")
        try {
          ve.onPostCommitFiberRoot(du, n);
        } catch {
        }
      return !0;
    } finally {
      H.p = u, q.T = a, dd(t, e);
    }
  }
  function md(t, e, l) {
    e = Ne(l, e), e = Zc(t.stateNode, e, 2), t = Ml(t, e, 2), t !== null && (mu(t, 2), $e(t));
  }
  function Et(t, e, l) {
    if (t.tag === 3)
      md(t, t, l);
    else
      for (; e !== null; ) {
        if (e.tag === 3) {
          md(
            e,
            t,
            l
          );
          break;
        } else if (e.tag === 1) {
          var a = e.stateNode;
          if (typeof e.type.getDerivedStateFromError == "function" || typeof a.componentDidCatch == "function" && (Hl === null || !Hl.has(a))) {
            t = Ne(l, t), l = ho(2), a = Ml(e, l, 2), a !== null && (vo(
              l,
              a,
              e,
              t
            ), mu(a, 2), $e(a));
            break;
          }
        }
        e = e.return;
      }
  }
  function hf(t, e, l) {
    var a = t.pingCache;
    if (a === null) {
      a = t.pingCache = new $m();
      var u = /* @__PURE__ */ new Set();
      a.set(e, u);
    } else
      u = a.get(e), u === void 0 && (u = /* @__PURE__ */ new Set(), a.set(e, u));
    u.has(l) || (cf = !0, u.add(l), t = th.bind(null, t, e, l), e.then(t, t));
  }
  function th(t, e, l) {
    var a = t.pingCache;
    a !== null && a.delete(e), t.pingedLanes |= t.suspendedLanes & l, t.warmLanes &= ~l, qt === t && (st & l) === l && (jt === 4 || jt === 3 && (st & 62914560) === st && 300 > bt() - li ? (vt & 2) === 0 && $a(t, 0) : ff |= l, wa === st && (wa = 0)), $e(t);
  }
  function hd(t, e) {
    e === 0 && (e = fs()), t = ea(t, e), t !== null && (mu(t, e), $e(t));
  }
  function eh(t) {
    var e = t.memoizedState, l = 0;
    e !== null && (l = e.retryLane), hd(t, l);
  }
  function lh(t, e) {
    var l = 0;
    switch (t.tag) {
      case 31:
      case 13:
        var a = t.stateNode, u = t.memoizedState;
        u !== null && (l = u.retryLane);
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
    a !== null && a.delete(e), hd(t, l);
  }
  function ah(t, e) {
    return ru(t, e);
  }
  var si = null, Fa = null, vf = !1, ri = !1, gf = !1, Ll = 0;
  function $e(t) {
    t !== Fa && t.next === null && (Fa === null ? si = Fa = t : Fa = Fa.next = t), ri = !0, vf || (vf = !0, nh());
  }
  function ku(t, e) {
    if (!gf && ri) {
      gf = !0;
      do
        for (var l = !1, a = si; a !== null; ) {
          if (t !== 0) {
            var u = a.pendingLanes;
            if (u === 0) var n = 0;
            else {
              var i = a.suspendedLanes, o = a.pingedLanes;
              n = (1 << 31 - ge(42 | t) + 1) - 1, n &= u & ~(i & ~o), n = n & 201326741 ? n & 201326741 | 1 : n ? n | 2 : 0;
            }
            n !== 0 && (l = !0, bd(a, n));
          } else
            n = st, n = mn(
              a,
              a === qt ? n : 0,
              a.cancelPendingCommit !== null || a.timeoutHandle !== -1
            ), (n & 3) === 0 || yu(a, n) || (l = !0, bd(a, n));
          a = a.next;
        }
      while (l);
      gf = !1;
    }
  }
  function uh() {
    vd();
  }
  function vd() {
    ri = vf = !1;
    var t = 0;
    Ll !== 0 && hh() && (t = Ll);
    for (var e = bt(), l = null, a = si; a !== null; ) {
      var u = a.next, n = gd(a, e);
      n === 0 ? (a.next = null, l === null ? si = u : l.next = u, u === null && (Fa = l)) : (l = a, (t !== 0 || (n & 3) !== 0) && (ri = !0)), a = u;
    }
    Kt !== 0 && Kt !== 5 || ku(t), Ll !== 0 && (Ll = 0);
  }
  function gd(t, e) {
    for (var l = t.suspendedLanes, a = t.pingedLanes, u = t.expirationTimes, n = t.pendingLanes & -62914561; 0 < n; ) {
      var i = 31 - ge(n), o = 1 << i, d = u[i];
      d === -1 ? ((o & l) === 0 || (o & a) !== 0) && (u[i] = Dy(o, e)) : d <= e && (t.expiredLanes |= o), n &= ~o;
    }
    if (e = qt, l = st, l = mn(
      t,
      t === e ? l : 0,
      t.cancelPendingCommit !== null || t.timeoutHandle !== -1
    ), a = t.callbackNode, l === 0 || t === e && (_t === 2 || _t === 9) || t.cancelPendingCommit !== null)
      return a !== null && a !== null && Z(a), t.callbackNode = null, t.callbackPriority = 0;
    if ((l & 3) === 0 || yu(t, l)) {
      if (e = l & -l, e === t.callbackPriority) return e;
      switch (a !== null && Z(a), Di(l)) {
        case 2:
        case 8:
          l = is;
          break;
        case 32:
          l = rn;
          break;
        case 268435456:
          l = cs;
          break;
        default:
          l = rn;
      }
      return a = pd.bind(null, t), l = ru(l, a), t.callbackPriority = e, t.callbackNode = l, e;
    }
    return a !== null && a !== null && Z(a), t.callbackPriority = 2, t.callbackNode = null, 2;
  }
  function pd(t, e) {
    if (Kt !== 0 && Kt !== 5)
      return t.callbackNode = null, t.callbackPriority = 0, null;
    var l = t.callbackNode;
    if (fi() && t.callbackNode !== l)
      return null;
    var a = st;
    return a = mn(
      t,
      t === qt ? a : 0,
      t.cancelPendingCommit !== null || t.timeoutHandle !== -1
    ), a === 0 ? null : (Po(t, a, e), gd(t, bt()), t.callbackNode != null && t.callbackNode === l ? pd.bind(null, t) : null);
  }
  function bd(t, e) {
    if (fi()) return null;
    Po(t, e, !0);
  }
  function nh() {
    gh(function() {
      (vt & 6) !== 0 ? ru(
        ou,
        uh
      ) : vd();
    });
  }
  function pf() {
    if (Ll === 0) {
      var t = Ha;
      t === 0 && (t = on, on <<= 1, (on & 261888) === 0 && (on = 256)), Ll = t;
    }
    return Ll;
  }
  function Sd(t) {
    return t == null || typeof t == "symbol" || typeof t == "boolean" ? null : typeof t == "function" ? t : pn("" + t);
  }
  function _d(t, e) {
    var l = e.ownerDocument.createElement("input");
    return l.name = e.name, l.value = e.value, t.id && l.setAttribute("form", t.id), e.parentNode.insertBefore(l, e), t = new FormData(t), l.parentNode.removeChild(l), t;
  }
  function ih(t, e, l, a, u) {
    if (e === "submit" && l && l.stateNode === u) {
      var n = Sd(
        (u[ce] || null).action
      ), i = a.submitter;
      i && (e = (e = i[ce] || null) ? Sd(e.formAction) : i.getAttribute("formAction"), e !== null && (n = e, i = null));
      var o = new En(
        "action",
        "action",
        null,
        a,
        u
      );
      t.push({
        event: o,
        listeners: [
          {
            instance: null,
            listener: function() {
              if (a.defaultPrevented) {
                if (Ll !== 0) {
                  var d = i ? _d(u, i) : new FormData(u);
                  Bc(
                    l,
                    {
                      pending: !0,
                      data: d,
                      method: u.method,
                      action: n
                    },
                    null,
                    d
                  );
                }
              } else
                typeof n == "function" && (o.preventDefault(), d = i ? _d(u, i) : new FormData(u), Bc(
                  l,
                  {
                    pending: !0,
                    data: d,
                    method: u.method,
                    action: n
                  },
                  n,
                  d
                ));
            },
            currentTarget: u
          }
        ]
      });
    }
  }
  for (var bf = 0; bf < tc.length; bf++) {
    var Sf = tc[bf], ch = Sf.toLowerCase(), fh = Sf[0].toUpperCase() + Sf.slice(1);
    Le(
      ch,
      "on" + fh
    );
  }
  Le(Fs, "onAnimationEnd"), Le(Is, "onAnimationIteration"), Le(Ps, "onAnimationStart"), Le("dblclick", "onDoubleClick"), Le("focusin", "onFocus"), Le("focusout", "onBlur"), Le(zm, "onTransitionRun"), Le(Tm, "onTransitionStart"), Le(xm, "onTransitionCancel"), Le(tr, "onTransitionEnd"), Ea("onMouseEnter", ["mouseout", "mouseover"]), Ea("onMouseLeave", ["mouseout", "mouseover"]), Ea("onPointerEnter", ["pointerout", "pointerover"]), Ea("onPointerLeave", ["pointerout", "pointerover"]), Fl(
    "onChange",
    "change click focusin focusout input keydown keyup selectionchange".split(" ")
  ), Fl(
    "onSelect",
    "focusout contextmenu dragend focusin keydown keyup mousedown mouseup selectionchange".split(
      " "
    )
  ), Fl("onBeforeInput", [
    "compositionend",
    "keypress",
    "textInput",
    "paste"
  ]), Fl(
    "onCompositionEnd",
    "compositionend focusout keydown keypress keyup mousedown".split(" ")
  ), Fl(
    "onCompositionStart",
    "compositionstart focusout keydown keypress keyup mousedown".split(" ")
  ), Fl(
    "onCompositionUpdate",
    "compositionupdate focusout keydown keypress keyup mousedown".split(" ")
  );
  var $u = "abort canplay canplaythrough durationchange emptied encrypted ended error loadeddata loadedmetadata loadstart pause play playing progress ratechange resize seeked seeking stalled suspend timeupdate volumechange waiting".split(
    " "
  ), sh = new Set(
    "beforetoggle cancel close invalid load scroll scrollend toggle".split(" ").concat($u)
  );
  function Ed(t, e) {
    e = (e & 4) !== 0;
    for (var l = 0; l < t.length; l++) {
      var a = t[l], u = a.event;
      a = a.listeners;
      t: {
        var n = void 0;
        if (e)
          for (var i = a.length - 1; 0 <= i; i--) {
            var o = a[i], d = o.instance, S = o.currentTarget;
            if (o = o.listener, d !== n && u.isPropagationStopped())
              break t;
            n = o, u.currentTarget = S;
            try {
              n(u);
            } catch (T) {
              Tn(T);
            }
            u.currentTarget = null, n = d;
          }
        else
          for (i = 0; i < a.length; i++) {
            if (o = a[i], d = o.instance, S = o.currentTarget, o = o.listener, d !== n && u.isPropagationStopped())
              break t;
            n = o, u.currentTarget = S;
            try {
              n(u);
            } catch (T) {
              Tn(T);
            }
            u.currentTarget = null, n = d;
          }
      }
    }
  }
  function ft(t, e) {
    var l = e[Ui];
    l === void 0 && (l = e[Ui] = /* @__PURE__ */ new Set());
    var a = t + "__bubble";
    l.has(a) || (Ad(e, t, 2, !1), l.add(a));
  }
  function _f(t, e, l) {
    var a = 0;
    e && (a |= 4), Ad(
      l,
      t,
      a,
      e
    );
  }
  var oi = "_reactListening" + Math.random().toString(36).slice(2);
  function Ef(t) {
    if (!t[oi]) {
      t[oi] = !0, hs.forEach(function(l) {
        l !== "selectionchange" && (sh.has(l) || _f(l, !1, t), _f(l, !0, t));
      });
      var e = t.nodeType === 9 ? t : t.ownerDocument;
      e === null || e[oi] || (e[oi] = !0, _f("selectionchange", !1, e));
    }
  }
  function Ad(t, e, l, a) {
    switch (Id(e)) {
      case 2:
        var u = Bh;
        break;
      case 8:
        u = Yh;
        break;
      default:
        u = Bf;
    }
    l = u.bind(
      null,
      e,
      l,
      t
    ), u = void 0, !Qi || e !== "touchstart" && e !== "touchmove" && e !== "wheel" || (u = !0), a ? u !== void 0 ? t.addEventListener(e, l, {
      capture: !0,
      passive: u
    }) : t.addEventListener(e, l, !0) : u !== void 0 ? t.addEventListener(e, l, {
      passive: u
    }) : t.addEventListener(e, l, !1);
  }
  function Af(t, e, l, a, u) {
    var n = a;
    if ((e & 1) === 0 && (e & 2) === 0 && a !== null)
      t: for (; ; ) {
        if (a === null) return;
        var i = a.tag;
        if (i === 3 || i === 4) {
          var o = a.stateNode.containerInfo;
          if (o === u) break;
          if (i === 4)
            for (i = a.return; i !== null; ) {
              var d = i.tag;
              if ((d === 3 || d === 4) && i.stateNode.containerInfo === u)
                return;
              i = i.return;
            }
          for (; o !== null; ) {
            if (i = ba(o), i === null) return;
            if (d = i.tag, d === 5 || d === 6 || d === 26 || d === 27) {
              a = n = i;
              continue t;
            }
            o = o.parentNode;
          }
        }
        a = a.return;
      }
    qs(function() {
      var S = n, T = Li(l), U = [];
      t: {
        var _ = er.get(t);
        if (_ !== void 0) {
          var A = En, L = t;
          switch (t) {
            case "keypress":
              if (Sn(l) === 0) break t;
            case "keydown":
            case "keyup":
              A = lm;
              break;
            case "focusin":
              L = "focus", A = Ki;
              break;
            case "focusout":
              L = "blur", A = Ki;
              break;
            case "beforeblur":
            case "afterblur":
              A = Ki;
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
              A = Ms;
              break;
            case "drag":
            case "dragend":
            case "dragenter":
            case "dragexit":
            case "dragleave":
            case "dragover":
            case "dragstart":
            case "drop":
              A = Vy;
              break;
            case "touchcancel":
            case "touchend":
            case "touchmove":
            case "touchstart":
              A = nm;
              break;
            case Fs:
            case Is:
            case Ps:
              A = wy;
              break;
            case tr:
              A = cm;
              break;
            case "scroll":
            case "scrollend":
              A = Xy;
              break;
            case "wheel":
              A = sm;
              break;
            case "copy":
            case "cut":
            case "paste":
              A = $y;
              break;
            case "gotpointercapture":
            case "lostpointercapture":
            case "pointercancel":
            case "pointerdown":
            case "pointermove":
            case "pointerout":
            case "pointerover":
            case "pointerup":
              A = Us;
              break;
            case "toggle":
            case "beforetoggle":
              A = om;
          }
          var $ = (e & 4) !== 0, Tt = !$ && (t === "scroll" || t === "scrollend"), v = $ ? _ !== null ? _ + "Capture" : null : _;
          $ = [];
          for (var y = S, b; y !== null; ) {
            var M = y;
            if (b = M.stateNode, M = M.tag, M !== 5 && M !== 26 && M !== 27 || b === null || v === null || (M = gu(y, v), M != null && $.push(
              Wu(y, M, b)
            )), Tt) break;
            y = y.return;
          }
          0 < $.length && (_ = new A(
            _,
            L,
            null,
            l,
            T
          ), U.push({ event: _, listeners: $ }));
        }
      }
      if ((e & 7) === 0) {
        t: {
          if (_ = t === "mouseover" || t === "pointerover", A = t === "mouseout" || t === "pointerout", _ && l !== Yi && (L = l.relatedTarget || l.fromElement) && (ba(L) || L[pa]))
            break t;
          if ((A || _) && (_ = T.window === T ? T : (_ = T.ownerDocument) ? _.defaultView || _.parentWindow : window, A ? (L = l.relatedTarget || l.toElement, A = S, L = L ? ba(L) : null, L !== null && (Tt = g(L), $ = L.tag, L !== Tt || $ !== 5 && $ !== 27 && $ !== 6) && (L = null)) : (A = null, L = S), A !== L)) {
            if ($ = Ms, M = "onMouseLeave", v = "onMouseEnter", y = "mouse", (t === "pointerout" || t === "pointerover") && ($ = Us, M = "onPointerLeave", v = "onPointerEnter", y = "pointer"), Tt = A == null ? _ : vu(A), b = L == null ? _ : vu(L), _ = new $(
              M,
              y + "leave",
              A,
              l,
              T
            ), _.target = Tt, _.relatedTarget = b, M = null, ba(T) === S && ($ = new $(
              v,
              y + "enter",
              L,
              l,
              T
            ), $.target = b, $.relatedTarget = Tt, M = $), Tt = M, A && L)
              e: {
                for ($ = rh, v = A, y = L, b = 0, M = v; M; M = $(M))
                  b++;
                M = 0;
                for (var k = y; k; k = $(k))
                  M++;
                for (; 0 < b - M; )
                  v = $(v), b--;
                for (; 0 < M - b; )
                  y = $(y), M--;
                for (; b--; ) {
                  if (v === y || y !== null && v === y.alternate) {
                    $ = v;
                    break e;
                  }
                  v = $(v), y = $(y);
                }
                $ = null;
              }
            else $ = null;
            A !== null && zd(
              U,
              _,
              A,
              $,
              !1
            ), L !== null && Tt !== null && zd(
              U,
              Tt,
              L,
              $,
              !0
            );
          }
        }
        t: {
          if (_ = S ? vu(S) : window, A = _.nodeName && _.nodeName.toLowerCase(), A === "select" || A === "input" && _.type === "file")
            var mt = Gs;
          else if (Ys(_))
            if (Qs)
              mt = _m;
            else {
              mt = bm;
              var X = pm;
            }
          else
            A = _.nodeName, !A || A.toLowerCase() !== "input" || _.type !== "checkbox" && _.type !== "radio" ? S && Bi(S.elementType) && (mt = Gs) : mt = Sm;
          if (mt && (mt = mt(t, S))) {
            Ls(
              U,
              mt,
              l,
              T
            );
            break t;
          }
          X && X(t, _, S), t === "focusout" && S && _.type === "number" && S.memoizedProps.value != null && Hi(_, "number", _.value);
        }
        switch (X = S ? vu(S) : window, t) {
          case "focusin":
            (Ys(X) || X.contentEditable === "true") && (Na = X, Fi = S, Tu = null);
            break;
          case "focusout":
            Tu = Fi = Na = null;
            break;
          case "mousedown":
            Ii = !0;
            break;
          case "contextmenu":
          case "mouseup":
          case "dragend":
            Ii = !1, $s(U, l, T);
            break;
          case "selectionchange":
            if (Am) break;
          case "keydown":
          case "keyup":
            $s(U, l, T);
        }
        var at;
        if (wi)
          t: {
            switch (t) {
              case "compositionstart":
                var rt = "onCompositionStart";
                break t;
              case "compositionend":
                rt = "onCompositionEnd";
                break t;
              case "compositionupdate":
                rt = "onCompositionUpdate";
                break t;
            }
            rt = void 0;
          }
        else
          qa ? Hs(t, l) && (rt = "onCompositionEnd") : t === "keydown" && l.keyCode === 229 && (rt = "onCompositionStart");
        rt && (Cs && l.locale !== "ko" && (qa || rt !== "onCompositionStart" ? rt === "onCompositionEnd" && qa && (at = Ns()) : (Al = T, Xi = "value" in Al ? Al.value : Al.textContent, qa = !0)), X = di(S, rt), 0 < X.length && (rt = new Ds(
          rt,
          t,
          null,
          l,
          T
        ), U.push({ event: rt, listeners: X }), at ? rt.data = at : (at = Bs(l), at !== null && (rt.data = at)))), (at = ym ? mm(t, l) : hm(t, l)) && (rt = di(S, "onBeforeInput"), 0 < rt.length && (X = new Ds(
          "onBeforeInput",
          "beforeinput",
          null,
          l,
          T
        ), U.push({
          event: X,
          listeners: rt
        }), X.data = at)), ih(
          U,
          t,
          S,
          l,
          T
        );
      }
      Ed(U, e);
    });
  }
  function Wu(t, e, l) {
    return {
      instance: t,
      listener: e,
      currentTarget: l
    };
  }
  function di(t, e) {
    for (var l = e + "Capture", a = []; t !== null; ) {
      var u = t, n = u.stateNode;
      if (u = u.tag, u !== 5 && u !== 26 && u !== 27 || n === null || (u = gu(t, l), u != null && a.unshift(
        Wu(t, u, n)
      ), u = gu(t, e), u != null && a.push(
        Wu(t, u, n)
      )), t.tag === 3) return a;
      t = t.return;
    }
    return [];
  }
  function rh(t) {
    if (t === null) return null;
    do
      t = t.return;
    while (t && t.tag !== 5 && t.tag !== 27);
    return t || null;
  }
  function zd(t, e, l, a, u) {
    for (var n = e._reactName, i = []; l !== null && l !== a; ) {
      var o = l, d = o.alternate, S = o.stateNode;
      if (o = o.tag, d !== null && d === a) break;
      o !== 5 && o !== 26 && o !== 27 || S === null || (d = S, u ? (S = gu(l, n), S != null && i.unshift(
        Wu(l, S, d)
      )) : u || (S = gu(l, n), S != null && i.push(
        Wu(l, S, d)
      ))), l = l.return;
    }
    i.length !== 0 && t.push({ event: e, listeners: i });
  }
  var oh = /\r\n?/g, dh = /\u0000|\uFFFD/g;
  function Td(t) {
    return (typeof t == "string" ? t : "" + t).replace(oh, `
`).replace(dh, "");
  }
  function xd(t, e) {
    return e = Td(e), Td(t) === e;
  }
  function zt(t, e, l, a, u, n) {
    switch (l) {
      case "children":
        typeof a == "string" ? e === "body" || e === "textarea" && a === "" || za(t, a) : (typeof a == "number" || typeof a == "bigint") && e !== "body" && za(t, "" + a);
        break;
      case "className":
        vn(t, "class", a);
        break;
      case "tabIndex":
        vn(t, "tabindex", a);
        break;
      case "dir":
      case "role":
      case "viewBox":
      case "width":
      case "height":
        vn(t, l, a);
        break;
      case "style":
        Ts(t, a, n);
        break;
      case "data":
        if (e !== "object") {
          vn(t, "data", a);
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
        a = pn("" + a), t.setAttribute(l, a);
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
          typeof n == "function" && (l === "formAction" ? (e !== "input" && zt(t, e, "name", u.name, u, null), zt(
            t,
            e,
            "formEncType",
            u.formEncType,
            u,
            null
          ), zt(
            t,
            e,
            "formMethod",
            u.formMethod,
            u,
            null
          ), zt(
            t,
            e,
            "formTarget",
            u.formTarget,
            u,
            null
          )) : (zt(t, e, "encType", u.encType, u, null), zt(t, e, "method", u.method, u, null), zt(t, e, "target", u.target, u, null)));
        if (a == null || typeof a == "symbol" || typeof a == "boolean") {
          t.removeAttribute(l);
          break;
        }
        a = pn("" + a), t.setAttribute(l, a);
        break;
      case "onClick":
        a != null && (t.onclick = tl);
        break;
      case "onScroll":
        a != null && ft("scroll", t);
        break;
      case "onScrollEnd":
        a != null && ft("scrollend", t);
        break;
      case "dangerouslySetInnerHTML":
        if (a != null) {
          if (typeof a != "object" || !("__html" in a))
            throw Error(s(61));
          if (l = a.__html, l != null) {
            if (u.children != null) throw Error(s(60));
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
        l = pn("" + a), t.setAttributeNS(
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
        ft("beforetoggle", t), ft("toggle", t), hn(t, "popover", a);
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
        hn(t, "is", a);
        break;
      case "innerText":
      case "textContent":
        break;
      default:
        (!(2 < l.length) || l[0] !== "o" && l[0] !== "O" || l[1] !== "n" && l[1] !== "N") && (l = Gy.get(l) || l, hn(t, l, a));
    }
  }
  function zf(t, e, l, a, u, n) {
    switch (l) {
      case "style":
        Ts(t, a, n);
        break;
      case "dangerouslySetInnerHTML":
        if (a != null) {
          if (typeof a != "object" || !("__html" in a))
            throw Error(s(61));
          if (l = a.__html, l != null) {
            if (u.children != null) throw Error(s(60));
            t.innerHTML = l;
          }
        }
        break;
      case "children":
        typeof a == "string" ? za(t, a) : (typeof a == "number" || typeof a == "bigint") && za(t, "" + a);
        break;
      case "onScroll":
        a != null && ft("scroll", t);
        break;
      case "onScrollEnd":
        a != null && ft("scrollend", t);
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
        if (!vs.hasOwnProperty(l))
          t: {
            if (l[0] === "o" && l[1] === "n" && (u = l.endsWith("Capture"), e = l.slice(2, u ? l.length - 7 : void 0), n = t[ce] || null, n = n != null ? n[l] : null, typeof n == "function" && t.removeEventListener(e, n, u), typeof a == "function")) {
              typeof n != "function" && n !== null && (l in t ? t[l] = null : t.hasAttribute(l) && t.removeAttribute(l)), t.addEventListener(e, a, u);
              break t;
            }
            l in t ? t[l] = a : a === !0 ? t.setAttribute(l, "") : hn(t, l, a);
          }
    }
  }
  function Pt(t, e, l) {
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
        ft("error", t), ft("load", t);
        var a = !1, u = !1, n;
        for (n in l)
          if (l.hasOwnProperty(n)) {
            var i = l[n];
            if (i != null)
              switch (n) {
                case "src":
                  a = !0;
                  break;
                case "srcSet":
                  u = !0;
                  break;
                case "children":
                case "dangerouslySetInnerHTML":
                  throw Error(s(137, e));
                default:
                  zt(t, e, n, i, l, null);
              }
          }
        u && zt(t, e, "srcSet", l.srcSet, l, null), a && zt(t, e, "src", l.src, l, null);
        return;
      case "input":
        ft("invalid", t);
        var o = n = i = u = null, d = null, S = null;
        for (a in l)
          if (l.hasOwnProperty(a)) {
            var T = l[a];
            if (T != null)
              switch (a) {
                case "name":
                  u = T;
                  break;
                case "type":
                  i = T;
                  break;
                case "checked":
                  d = T;
                  break;
                case "defaultChecked":
                  S = T;
                  break;
                case "value":
                  n = T;
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
                  zt(t, e, a, T, l, null);
              }
          }
        _s(
          t,
          n,
          o,
          d,
          S,
          i,
          u,
          !1
        );
        return;
      case "select":
        ft("invalid", t), a = i = n = null;
        for (u in l)
          if (l.hasOwnProperty(u) && (o = l[u], o != null))
            switch (u) {
              case "value":
                n = o;
                break;
              case "defaultValue":
                i = o;
                break;
              case "multiple":
                a = o;
              default:
                zt(t, e, u, o, l, null);
            }
        e = n, l = i, t.multiple = !!a, e != null ? Aa(t, !!a, e, !1) : l != null && Aa(t, !!a, l, !0);
        return;
      case "textarea":
        ft("invalid", t), n = u = a = null;
        for (i in l)
          if (l.hasOwnProperty(i) && (o = l[i], o != null))
            switch (i) {
              case "value":
                a = o;
                break;
              case "defaultValue":
                u = o;
                break;
              case "children":
                n = o;
                break;
              case "dangerouslySetInnerHTML":
                if (o != null) throw Error(s(91));
                break;
              default:
                zt(t, e, i, o, l, null);
            }
        As(t, a, u, n);
        return;
      case "option":
        for (d in l)
          if (l.hasOwnProperty(d) && (a = l[d], a != null))
            switch (d) {
              case "selected":
                t.selected = a && typeof a != "function" && typeof a != "symbol";
                break;
              default:
                zt(t, e, d, a, l, null);
            }
        return;
      case "dialog":
        ft("beforetoggle", t), ft("toggle", t), ft("cancel", t), ft("close", t);
        break;
      case "iframe":
      case "object":
        ft("load", t);
        break;
      case "video":
      case "audio":
        for (a = 0; a < $u.length; a++)
          ft($u[a], t);
        break;
      case "image":
        ft("error", t), ft("load", t);
        break;
      case "details":
        ft("toggle", t);
        break;
      case "embed":
      case "source":
      case "link":
        ft("error", t), ft("load", t);
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
        for (S in l)
          if (l.hasOwnProperty(S) && (a = l[S], a != null))
            switch (S) {
              case "children":
              case "dangerouslySetInnerHTML":
                throw Error(s(137, e));
              default:
                zt(t, e, S, a, l, null);
            }
        return;
      default:
        if (Bi(e)) {
          for (T in l)
            l.hasOwnProperty(T) && (a = l[T], a !== void 0 && zf(
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
      l.hasOwnProperty(o) && (a = l[o], a != null && zt(t, e, o, a, l, null));
  }
  function yh(t, e, l, a) {
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
        var u = null, n = null, i = null, o = null, d = null, S = null, T = null;
        for (A in l) {
          var U = l[A];
          if (l.hasOwnProperty(A) && U != null)
            switch (A) {
              case "checked":
                break;
              case "value":
                break;
              case "defaultValue":
                d = U;
              default:
                a.hasOwnProperty(A) || zt(t, e, A, null, a, U);
            }
        }
        for (var _ in a) {
          var A = a[_];
          if (U = l[_], a.hasOwnProperty(_) && (A != null || U != null))
            switch (_) {
              case "type":
                n = A;
                break;
              case "name":
                u = A;
                break;
              case "checked":
                S = A;
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
                A !== U && zt(
                  t,
                  e,
                  _,
                  A,
                  a,
                  U
                );
            }
        }
        Ri(
          t,
          i,
          o,
          d,
          S,
          T,
          n,
          u
        );
        return;
      case "select":
        A = i = o = _ = null;
        for (n in l)
          if (d = l[n], l.hasOwnProperty(n) && d != null)
            switch (n) {
              case "value":
                break;
              case "multiple":
                A = d;
              default:
                a.hasOwnProperty(n) || zt(
                  t,
                  e,
                  n,
                  null,
                  a,
                  d
                );
            }
        for (u in a)
          if (n = a[u], d = l[u], a.hasOwnProperty(u) && (n != null || d != null))
            switch (u) {
              case "value":
                _ = n;
                break;
              case "defaultValue":
                o = n;
                break;
              case "multiple":
                i = n;
              default:
                n !== d && zt(
                  t,
                  e,
                  u,
                  n,
                  a,
                  d
                );
            }
        e = o, l = i, a = A, _ != null ? Aa(t, !!l, _, !1) : !!a != !!l && (e != null ? Aa(t, !!l, e, !0) : Aa(t, !!l, l ? [] : "", !1));
        return;
      case "textarea":
        A = _ = null;
        for (o in l)
          if (u = l[o], l.hasOwnProperty(o) && u != null && !a.hasOwnProperty(o))
            switch (o) {
              case "value":
                break;
              case "children":
                break;
              default:
                zt(t, e, o, null, a, u);
            }
        for (i in a)
          if (u = a[i], n = l[i], a.hasOwnProperty(i) && (u != null || n != null))
            switch (i) {
              case "value":
                _ = u;
                break;
              case "defaultValue":
                A = u;
                break;
              case "children":
                break;
              case "dangerouslySetInnerHTML":
                if (u != null) throw Error(s(91));
                break;
              default:
                u !== n && zt(t, e, i, u, a, n);
            }
        Es(t, _, A);
        return;
      case "option":
        for (var L in l)
          if (_ = l[L], l.hasOwnProperty(L) && _ != null && !a.hasOwnProperty(L))
            switch (L) {
              case "selected":
                t.selected = !1;
                break;
              default:
                zt(
                  t,
                  e,
                  L,
                  null,
                  a,
                  _
                );
            }
        for (d in a)
          if (_ = a[d], A = l[d], a.hasOwnProperty(d) && _ !== A && (_ != null || A != null))
            switch (d) {
              case "selected":
                t.selected = _ && typeof _ != "function" && typeof _ != "symbol";
                break;
              default:
                zt(
                  t,
                  e,
                  d,
                  _,
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
        for (var $ in l)
          _ = l[$], l.hasOwnProperty($) && _ != null && !a.hasOwnProperty($) && zt(t, e, $, null, a, _);
        for (S in a)
          if (_ = a[S], A = l[S], a.hasOwnProperty(S) && _ !== A && (_ != null || A != null))
            switch (S) {
              case "children":
              case "dangerouslySetInnerHTML":
                if (_ != null)
                  throw Error(s(137, e));
                break;
              default:
                zt(
                  t,
                  e,
                  S,
                  _,
                  a,
                  A
                );
            }
        return;
      default:
        if (Bi(e)) {
          for (var Tt in l)
            _ = l[Tt], l.hasOwnProperty(Tt) && _ !== void 0 && !a.hasOwnProperty(Tt) && zf(
              t,
              e,
              Tt,
              void 0,
              a,
              _
            );
          for (T in a)
            _ = a[T], A = l[T], !a.hasOwnProperty(T) || _ === A || _ === void 0 && A === void 0 || zf(
              t,
              e,
              T,
              _,
              a,
              A
            );
          return;
        }
    }
    for (var v in l)
      _ = l[v], l.hasOwnProperty(v) && _ != null && !a.hasOwnProperty(v) && zt(t, e, v, null, a, _);
    for (U in a)
      _ = a[U], A = l[U], !a.hasOwnProperty(U) || _ === A || _ == null && A == null || zt(t, e, U, _, a, A);
  }
  function qd(t) {
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
  function mh() {
    if (typeof performance.getEntriesByType == "function") {
      for (var t = 0, e = 0, l = performance.getEntriesByType("resource"), a = 0; a < l.length; a++) {
        var u = l[a], n = u.transferSize, i = u.initiatorType, o = u.duration;
        if (n && o && qd(i)) {
          for (i = 0, o = u.responseEnd, a += 1; a < l.length; a++) {
            var d = l[a], S = d.startTime;
            if (S > o) break;
            var T = d.transferSize, U = d.initiatorType;
            T && qd(U) && (d = d.responseEnd, i += T * (d < o ? 1 : (o - S) / (d - S)));
          }
          if (--a, e += 8 * (n + i) / (u.duration / 1e3), t++, 10 < t) break;
        }
      }
      if (0 < t) return e / t / 1e6;
    }
    return navigator.connection && (t = navigator.connection.downlink, typeof t == "number") ? t : 5;
  }
  var Tf = null, xf = null;
  function yi(t) {
    return t.nodeType === 9 ? t : t.ownerDocument;
  }
  function Nd(t) {
    switch (t) {
      case "http://www.w3.org/2000/svg":
        return 1;
      case "http://www.w3.org/1998/Math/MathML":
        return 2;
      default:
        return 0;
    }
  }
  function Od(t, e) {
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
  function qf(t, e) {
    return t === "textarea" || t === "noscript" || typeof e.children == "string" || typeof e.children == "number" || typeof e.children == "bigint" || typeof e.dangerouslySetInnerHTML == "object" && e.dangerouslySetInnerHTML !== null && e.dangerouslySetInnerHTML.__html != null;
  }
  var Nf = null;
  function hh() {
    var t = window.event;
    return t && t.type === "popstate" ? t === Nf ? !1 : (Nf = t, !0) : (Nf = null, !1);
  }
  var Md = typeof setTimeout == "function" ? setTimeout : void 0, vh = typeof clearTimeout == "function" ? clearTimeout : void 0, Dd = typeof Promise == "function" ? Promise : void 0, gh = typeof queueMicrotask == "function" ? queueMicrotask : typeof Dd < "u" ? function(t) {
    return Dd.resolve(null).then(t).catch(ph);
  } : Md;
  function ph(t) {
    setTimeout(function() {
      throw t;
    });
  }
  function Gl(t) {
    return t === "head";
  }
  function Ud(t, e) {
    var l = e, a = 0;
    do {
      var u = l.nextSibling;
      if (t.removeChild(l), u && u.nodeType === 8)
        if (l = u.data, l === "/$" || l === "/&") {
          if (a === 0) {
            t.removeChild(u), eu(e);
            return;
          }
          a--;
        } else if (l === "$" || l === "$?" || l === "$~" || l === "$!" || l === "&")
          a++;
        else if (l === "html")
          Fu(t.ownerDocument.documentElement);
        else if (l === "head") {
          l = t.ownerDocument.head, Fu(l);
          for (var n = l.firstChild; n; ) {
            var i = n.nextSibling, o = n.nodeName;
            n[hu] || o === "SCRIPT" || o === "STYLE" || o === "LINK" && n.rel.toLowerCase() === "stylesheet" || l.removeChild(n), n = i;
          }
        } else
          l === "body" && Fu(t.ownerDocument.body);
      l = u;
    } while (l);
    eu(e);
  }
  function Cd(t, e) {
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
  function Of(t) {
    var e = t.firstChild;
    for (e && e.nodeType === 10 && (e = e.nextSibling); e; ) {
      var l = e;
      switch (e = e.nextSibling, l.nodeName) {
        case "HTML":
        case "HEAD":
        case "BODY":
          Of(l), Ci(l);
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
  function bh(t, e, l, a) {
    for (; t.nodeType === 1; ) {
      var u = l;
      if (t.nodeName.toLowerCase() !== e.toLowerCase()) {
        if (!a && (t.nodeName !== "INPUT" || t.type !== "hidden"))
          break;
      } else if (a) {
        if (!t[hu])
          switch (e) {
            case "meta":
              if (!t.hasAttribute("itemprop")) break;
              return t;
            case "link":
              if (n = t.getAttribute("rel"), n === "stylesheet" && t.hasAttribute("data-precedence"))
                break;
              if (n !== u.rel || t.getAttribute("href") !== (u.href == null || u.href === "" ? null : u.href) || t.getAttribute("crossorigin") !== (u.crossOrigin == null ? null : u.crossOrigin) || t.getAttribute("title") !== (u.title == null ? null : u.title))
                break;
              return t;
            case "style":
              if (t.hasAttribute("data-precedence")) break;
              return t;
            case "script":
              if (n = t.getAttribute("src"), (n !== (u.src == null ? null : u.src) || t.getAttribute("type") !== (u.type == null ? null : u.type) || t.getAttribute("crossorigin") !== (u.crossOrigin == null ? null : u.crossOrigin)) && n && t.hasAttribute("async") && !t.hasAttribute("itemprop"))
                break;
              return t;
            default:
              return t;
          }
      } else if (e === "input" && t.type === "hidden") {
        var n = u.name == null ? null : "" + u.name;
        if (u.type === "hidden" && t.getAttribute("name") === n)
          return t;
      } else return t;
      if (t = Ce(t.nextSibling), t === null) break;
    }
    return null;
  }
  function Sh(t, e, l) {
    if (e === "") return null;
    for (; t.nodeType !== 3; )
      if ((t.nodeType !== 1 || t.nodeName !== "INPUT" || t.type !== "hidden") && !l || (t = Ce(t.nextSibling), t === null)) return null;
    return t;
  }
  function jd(t, e) {
    for (; t.nodeType !== 8; )
      if ((t.nodeType !== 1 || t.nodeName !== "INPUT" || t.type !== "hidden") && !e || (t = Ce(t.nextSibling), t === null)) return null;
    return t;
  }
  function Mf(t) {
    return t.data === "$?" || t.data === "$~";
  }
  function Df(t) {
    return t.data === "$!" || t.data === "$?" && t.ownerDocument.readyState !== "loading";
  }
  function _h(t, e) {
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
  function Ce(t) {
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
  var Uf = null;
  function Rd(t) {
    t = t.nextSibling;
    for (var e = 0; t; ) {
      if (t.nodeType === 8) {
        var l = t.data;
        if (l === "/$" || l === "/&") {
          if (e === 0)
            return Ce(t.nextSibling);
          e--;
        } else
          l !== "$" && l !== "$!" && l !== "$?" && l !== "$~" && l !== "&" || e++;
      }
      t = t.nextSibling;
    }
    return null;
  }
  function Hd(t) {
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
  function Bd(t, e, l) {
    switch (e = yi(l), t) {
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
  function Fu(t) {
    for (var e = t.attributes; e.length; )
      t.removeAttributeNode(e[0]);
    Ci(t);
  }
  var je = /* @__PURE__ */ new Map(), Yd = /* @__PURE__ */ new Set();
  function mi(t) {
    return typeof t.getRootNode == "function" ? t.getRootNode() : t.nodeType === 9 ? t : t.ownerDocument;
  }
  var vl = H.d;
  H.d = {
    f: Eh,
    r: Ah,
    D: zh,
    C: Th,
    L: xh,
    m: qh,
    X: Oh,
    S: Nh,
    M: Mh
  };
  function Eh() {
    var t = vl.f(), e = ni();
    return t || e;
  }
  function Ah(t) {
    var e = Sa(t);
    e !== null && e.tag === 5 && e.type === "form" ? to(e) : vl.r(t);
  }
  var Ia = typeof document > "u" ? null : document;
  function Ld(t, e, l) {
    var a = Ia;
    if (a && typeof e == "string" && e) {
      var u = xe(e);
      u = 'link[rel="' + t + '"][href="' + u + '"]', typeof l == "string" && (u += '[crossorigin="' + l + '"]'), Yd.has(u) || (Yd.add(u), t = { rel: t, crossOrigin: l, href: e }, a.querySelector(u) === null && (e = a.createElement("link"), Pt(e, "link", t), wt(e), a.head.appendChild(e)));
    }
  }
  function zh(t) {
    vl.D(t), Ld("dns-prefetch", t, null);
  }
  function Th(t, e) {
    vl.C(t, e), Ld("preconnect", t, e);
  }
  function xh(t, e, l) {
    vl.L(t, e, l);
    var a = Ia;
    if (a && t && e) {
      var u = 'link[rel="preload"][as="' + xe(e) + '"]';
      e === "image" && l && l.imageSrcSet ? (u += '[imagesrcset="' + xe(
        l.imageSrcSet
      ) + '"]', typeof l.imageSizes == "string" && (u += '[imagesizes="' + xe(
        l.imageSizes
      ) + '"]')) : u += '[href="' + xe(t) + '"]';
      var n = u;
      switch (e) {
        case "style":
          n = Pa(t);
          break;
        case "script":
          n = tu(t);
      }
      je.has(n) || (t = E(
        {
          rel: "preload",
          href: e === "image" && l && l.imageSrcSet ? void 0 : t,
          as: e
        },
        l
      ), je.set(n, t), a.querySelector(u) !== null || e === "style" && a.querySelector(Iu(n)) || e === "script" && a.querySelector(Pu(n)) || (e = a.createElement("link"), Pt(e, "link", t), wt(e), a.head.appendChild(e)));
    }
  }
  function qh(t, e) {
    vl.m(t, e);
    var l = Ia;
    if (l && t) {
      var a = e && typeof e.as == "string" ? e.as : "script", u = 'link[rel="modulepreload"][as="' + xe(a) + '"][href="' + xe(t) + '"]', n = u;
      switch (a) {
        case "audioworklet":
        case "paintworklet":
        case "serviceworker":
        case "sharedworker":
        case "worker":
        case "script":
          n = tu(t);
      }
      if (!je.has(n) && (t = E({ rel: "modulepreload", href: t }, e), je.set(n, t), l.querySelector(u) === null)) {
        switch (a) {
          case "audioworklet":
          case "paintworklet":
          case "serviceworker":
          case "sharedworker":
          case "worker":
          case "script":
            if (l.querySelector(Pu(n)))
              return;
        }
        a = l.createElement("link"), Pt(a, "link", t), wt(a), l.head.appendChild(a);
      }
    }
  }
  function Nh(t, e, l) {
    vl.S(t, e, l);
    var a = Ia;
    if (a && t) {
      var u = _a(a).hoistableStyles, n = Pa(t);
      e = e || "default";
      var i = u.get(n);
      if (!i) {
        var o = { loading: 0, preload: null };
        if (i = a.querySelector(
          Iu(n)
        ))
          o.loading = 5;
        else {
          t = E(
            { rel: "stylesheet", href: t, "data-precedence": e },
            l
          ), (l = je.get(n)) && Cf(t, l);
          var d = i = a.createElement("link");
          wt(d), Pt(d, "link", t), d._p = new Promise(function(S, T) {
            d.onload = S, d.onerror = T;
          }), d.addEventListener("load", function() {
            o.loading |= 1;
          }), d.addEventListener("error", function() {
            o.loading |= 2;
          }), o.loading |= 4, hi(i, e, a);
        }
        i = {
          type: "stylesheet",
          instance: i,
          count: 1,
          state: o
        }, u.set(n, i);
      }
    }
  }
  function Oh(t, e) {
    vl.X(t, e);
    var l = Ia;
    if (l && t) {
      var a = _a(l).hoistableScripts, u = tu(t), n = a.get(u);
      n || (n = l.querySelector(Pu(u)), n || (t = E({ src: t, async: !0 }, e), (e = je.get(u)) && jf(t, e), n = l.createElement("script"), wt(n), Pt(n, "link", t), l.head.appendChild(n)), n = {
        type: "script",
        instance: n,
        count: 1,
        state: null
      }, a.set(u, n));
    }
  }
  function Mh(t, e) {
    vl.M(t, e);
    var l = Ia;
    if (l && t) {
      var a = _a(l).hoistableScripts, u = tu(t), n = a.get(u);
      n || (n = l.querySelector(Pu(u)), n || (t = E({ src: t, async: !0, type: "module" }, e), (e = je.get(u)) && jf(t, e), n = l.createElement("script"), wt(n), Pt(n, "link", t), l.head.appendChild(n)), n = {
        type: "script",
        instance: n,
        count: 1,
        state: null
      }, a.set(u, n));
    }
  }
  function Gd(t, e, l, a) {
    var u = (u = et.current) ? mi(u) : null;
    if (!u) throw Error(s(446));
    switch (t) {
      case "meta":
      case "title":
        return null;
      case "style":
        return typeof l.precedence == "string" && typeof l.href == "string" ? (e = Pa(l.href), l = _a(
          u
        ).hoistableStyles, a = l.get(e), a || (a = {
          type: "style",
          instance: null,
          count: 0,
          state: null
        }, l.set(e, a)), a) : { type: "void", instance: null, count: 0, state: null };
      case "link":
        if (l.rel === "stylesheet" && typeof l.href == "string" && typeof l.precedence == "string") {
          t = Pa(l.href);
          var n = _a(
            u
          ).hoistableStyles, i = n.get(t);
          if (i || (u = u.ownerDocument || u, i = {
            type: "stylesheet",
            instance: null,
            count: 0,
            state: { loading: 0, preload: null }
          }, n.set(t, i), (n = u.querySelector(
            Iu(t)
          )) && !n._p && (i.instance = n, i.state.loading = 5), je.has(t) || (l = {
            rel: "preload",
            as: "style",
            href: l.href,
            crossOrigin: l.crossOrigin,
            integrity: l.integrity,
            media: l.media,
            hrefLang: l.hrefLang,
            referrerPolicy: l.referrerPolicy
          }, je.set(t, l), n || Dh(
            u,
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
        return e = l.async, l = l.src, typeof l == "string" && e && typeof e != "function" && typeof e != "symbol" ? (e = tu(l), l = _a(
          u
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
  function Pa(t) {
    return 'href="' + xe(t) + '"';
  }
  function Iu(t) {
    return 'link[rel="stylesheet"][' + t + "]";
  }
  function Qd(t) {
    return E({}, t, {
      "data-precedence": t.precedence,
      precedence: null
    });
  }
  function Dh(t, e, l, a) {
    t.querySelector('link[rel="preload"][as="style"][' + e + "]") ? a.loading = 1 : (e = t.createElement("link"), a.preload = e, e.addEventListener("load", function() {
      return a.loading |= 1;
    }), e.addEventListener("error", function() {
      return a.loading |= 2;
    }), Pt(e, "link", l), wt(e), t.head.appendChild(e));
  }
  function tu(t) {
    return '[src="' + xe(t) + '"]';
  }
  function Pu(t) {
    return "script[async]" + t;
  }
  function Xd(t, e, l) {
    if (e.count++, e.instance === null)
      switch (e.type) {
        case "style":
          var a = t.querySelector(
            'style[data-href~="' + xe(l.href) + '"]'
          );
          if (a)
            return e.instance = a, wt(a), a;
          var u = E({}, l, {
            "data-href": l.href,
            "data-precedence": l.precedence,
            href: null,
            precedence: null
          });
          return a = (t.ownerDocument || t).createElement(
            "style"
          ), wt(a), Pt(a, "style", u), hi(a, l.precedence, t), e.instance = a;
        case "stylesheet":
          u = Pa(l.href);
          var n = t.querySelector(
            Iu(u)
          );
          if (n)
            return e.state.loading |= 4, e.instance = n, wt(n), n;
          a = Qd(l), (u = je.get(u)) && Cf(a, u), n = (t.ownerDocument || t).createElement("link"), wt(n);
          var i = n;
          return i._p = new Promise(function(o, d) {
            i.onload = o, i.onerror = d;
          }), Pt(n, "link", a), e.state.loading |= 4, hi(n, l.precedence, t), e.instance = n;
        case "script":
          return n = tu(l.src), (u = t.querySelector(
            Pu(n)
          )) ? (e.instance = u, wt(u), u) : (a = l, (u = je.get(n)) && (a = E({}, l), jf(a, u)), t = t.ownerDocument || t, u = t.createElement("script"), wt(u), Pt(u, "link", a), t.head.appendChild(u), e.instance = u);
        case "void":
          return null;
        default:
          throw Error(s(443, e.type));
      }
    else
      e.type === "stylesheet" && (e.state.loading & 4) === 0 && (a = e.instance, e.state.loading |= 4, hi(a, l.precedence, t));
    return e.instance;
  }
  function hi(t, e, l) {
    for (var a = l.querySelectorAll(
      'link[rel="stylesheet"][data-precedence],style[data-precedence]'
    ), u = a.length ? a[a.length - 1] : null, n = u, i = 0; i < a.length; i++) {
      var o = a[i];
      if (o.dataset.precedence === e) n = o;
      else if (n !== u) break;
    }
    n ? n.parentNode.insertBefore(t, n.nextSibling) : (e = l.nodeType === 9 ? l.head : l, e.insertBefore(t, e.firstChild));
  }
  function Cf(t, e) {
    t.crossOrigin == null && (t.crossOrigin = e.crossOrigin), t.referrerPolicy == null && (t.referrerPolicy = e.referrerPolicy), t.title == null && (t.title = e.title);
  }
  function jf(t, e) {
    t.crossOrigin == null && (t.crossOrigin = e.crossOrigin), t.referrerPolicy == null && (t.referrerPolicy = e.referrerPolicy), t.integrity == null && (t.integrity = e.integrity);
  }
  var vi = null;
  function Zd(t, e, l) {
    if (vi === null) {
      var a = /* @__PURE__ */ new Map(), u = vi = /* @__PURE__ */ new Map();
      u.set(l, a);
    } else
      u = vi, a = u.get(l), a || (a = /* @__PURE__ */ new Map(), u.set(l, a));
    if (a.has(t)) return a;
    for (a.set(t, null), l = l.getElementsByTagName(t), u = 0; u < l.length; u++) {
      var n = l[u];
      if (!(n[hu] || n[$t] || t === "link" && n.getAttribute("rel") === "stylesheet") && n.namespaceURI !== "http://www.w3.org/2000/svg") {
        var i = n.getAttribute(e) || "";
        i = t + i;
        var o = a.get(i);
        o ? o.push(n) : a.set(i, [n]);
      }
    }
    return a;
  }
  function Vd(t, e, l) {
    t = t.ownerDocument || t, t.head.insertBefore(
      l,
      e === "title" ? t.querySelector("head > title") : null
    );
  }
  function Uh(t, e, l) {
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
  function Kd(t) {
    return !(t.type === "stylesheet" && (t.state.loading & 3) === 0);
  }
  function Ch(t, e, l, a) {
    if (l.type === "stylesheet" && (typeof a.media != "string" || matchMedia(a.media).matches !== !1) && (l.state.loading & 4) === 0) {
      if (l.instance === null) {
        var u = Pa(a.href), n = e.querySelector(
          Iu(u)
        );
        if (n) {
          e = n._p, e !== null && typeof e == "object" && typeof e.then == "function" && (t.count++, t = gi.bind(t), e.then(t, t)), l.state.loading |= 4, l.instance = n, wt(n);
          return;
        }
        n = e.ownerDocument || e, a = Qd(a), (u = je.get(u)) && Cf(a, u), n = n.createElement("link"), wt(n);
        var i = n;
        i._p = new Promise(function(o, d) {
          i.onload = o, i.onerror = d;
        }), Pt(n, "link", a), l.instance = n;
      }
      t.stylesheets === null && (t.stylesheets = /* @__PURE__ */ new Map()), t.stylesheets.set(l, e), (e = l.state.preload) && (l.state.loading & 3) === 0 && (t.count++, l = gi.bind(t), e.addEventListener("load", l), e.addEventListener("error", l));
    }
  }
  var Rf = 0;
  function jh(t, e) {
    return t.stylesheets && t.count === 0 && bi(t, t.stylesheets), 0 < t.count || 0 < t.imgCount ? function(l) {
      var a = setTimeout(function() {
        if (t.stylesheets && bi(t, t.stylesheets), t.unsuspend) {
          var n = t.unsuspend;
          t.unsuspend = null, n();
        }
      }, 6e4 + e);
      0 < t.imgBytes && Rf === 0 && (Rf = 62500 * mh());
      var u = setTimeout(
        function() {
          if (t.waitingForImages = !1, t.count === 0 && (t.stylesheets && bi(t, t.stylesheets), t.unsuspend)) {
            var n = t.unsuspend;
            t.unsuspend = null, n();
          }
        },
        (t.imgBytes > Rf ? 50 : 800) + e
      );
      return t.unsuspend = l, function() {
        t.unsuspend = null, clearTimeout(a), clearTimeout(u);
      };
    } : null;
  }
  function gi() {
    if (this.count--, this.count === 0 && (this.imgCount === 0 || !this.waitingForImages)) {
      if (this.stylesheets) bi(this, this.stylesheets);
      else if (this.unsuspend) {
        var t = this.unsuspend;
        this.unsuspend = null, t();
      }
    }
  }
  var pi = null;
  function bi(t, e) {
    t.stylesheets = null, t.unsuspend !== null && (t.count++, pi = /* @__PURE__ */ new Map(), e.forEach(Rh, t), pi = null, gi.call(t));
  }
  function Rh(t, e) {
    if (!(e.state.loading & 4)) {
      var l = pi.get(t);
      if (l) var a = l.get(null);
      else {
        l = /* @__PURE__ */ new Map(), pi.set(t, l);
        for (var u = t.querySelectorAll(
          "link[data-precedence],style[data-precedence]"
        ), n = 0; n < u.length; n++) {
          var i = u[n];
          (i.nodeName === "LINK" || i.getAttribute("media") !== "not all") && (l.set(i.dataset.precedence, i), a = i);
        }
        a && l.set(null, a);
      }
      u = e.instance, i = u.getAttribute("data-precedence"), n = l.get(i) || a, n === a && l.set(null, u), l.set(i, u), this.count++, a = gi.bind(this), u.addEventListener("load", a), u.addEventListener("error", a), n ? n.parentNode.insertBefore(u, n.nextSibling) : (t = t.nodeType === 9 ? t.head : t, t.insertBefore(u, t.firstChild)), e.state.loading |= 4;
    }
  }
  var tn = {
    $$typeof: w,
    Provider: null,
    Consumer: null,
    _currentValue: V,
    _currentValue2: V,
    _threadCount: 0
  };
  function Hh(t, e, l, a, u, n, i, o, d) {
    this.tag = 1, this.containerInfo = t, this.pingCache = this.current = this.pendingChildren = null, this.timeoutHandle = -1, this.callbackNode = this.next = this.pendingContext = this.context = this.cancelPendingCommit = null, this.callbackPriority = 0, this.expirationTimes = Oi(-1), this.entangledLanes = this.shellSuspendCounter = this.errorRecoveryDisabledLanes = this.expiredLanes = this.warmLanes = this.pingedLanes = this.suspendedLanes = this.pendingLanes = 0, this.entanglements = Oi(0), this.hiddenUpdates = Oi(null), this.identifierPrefix = a, this.onUncaughtError = u, this.onCaughtError = n, this.onRecoverableError = i, this.pooledCache = null, this.pooledCacheLanes = 0, this.formState = d, this.incompleteTransitions = /* @__PURE__ */ new Map();
  }
  function Jd(t, e, l, a, u, n, i, o, d, S, T, U) {
    return t = new Hh(
      t,
      e,
      l,
      i,
      d,
      S,
      T,
      U,
      o
    ), e = 1, n === !0 && (e |= 24), n = be(3, null, null, e), t.current = n, n.stateNode = t, e = yc(), e.refCount++, t.pooledCache = e, e.refCount++, n.memoizedState = {
      element: a,
      isDehydrated: l,
      cache: e
    }, gc(n), t;
  }
  function wd(t) {
    return t ? (t = Da, t) : Da;
  }
  function kd(t, e, l, a, u, n) {
    u = wd(u), a.context === null ? a.context = u : a.pendingContext = u, a = Ol(e), a.payload = { element: l }, n = n === void 0 ? null : n, n !== null && (a.callback = n), l = Ml(t, a, e), l !== null && (ye(l, t, e), Uu(l, t, e));
  }
  function $d(t, e) {
    if (t = t.memoizedState, t !== null && t.dehydrated !== null) {
      var l = t.retryLane;
      t.retryLane = l !== 0 && l < e ? l : e;
    }
  }
  function Hf(t, e) {
    $d(t, e), (t = t.alternate) && $d(t, e);
  }
  function Wd(t) {
    if (t.tag === 13 || t.tag === 31) {
      var e = ea(t, 67108864);
      e !== null && ye(e, t, 67108864), Hf(t, 67108864);
    }
  }
  function Fd(t) {
    if (t.tag === 13 || t.tag === 31) {
      var e = ze();
      e = Mi(e);
      var l = ea(t, e);
      l !== null && ye(l, t, e), Hf(t, e);
    }
  }
  var Si = !0;
  function Bh(t, e, l, a) {
    var u = q.T;
    q.T = null;
    var n = H.p;
    try {
      H.p = 2, Bf(t, e, l, a);
    } finally {
      H.p = n, q.T = u;
    }
  }
  function Yh(t, e, l, a) {
    var u = q.T;
    q.T = null;
    var n = H.p;
    try {
      H.p = 8, Bf(t, e, l, a);
    } finally {
      H.p = n, q.T = u;
    }
  }
  function Bf(t, e, l, a) {
    if (Si) {
      var u = Yf(a);
      if (u === null)
        Af(
          t,
          e,
          a,
          _i,
          l
        ), Pd(t, a);
      else if (Gh(
        u,
        t,
        e,
        l,
        a
      ))
        a.stopPropagation();
      else if (Pd(t, a), e & 4 && -1 < Lh.indexOf(t)) {
        for (; u !== null; ) {
          var n = Sa(u);
          if (n !== null)
            switch (n.tag) {
              case 3:
                if (n = n.stateNode, n.current.memoizedState.isDehydrated) {
                  var i = Wl(n.pendingLanes);
                  if (i !== 0) {
                    var o = n;
                    for (o.pendingLanes |= 2, o.entangledLanes |= 2; i; ) {
                      var d = 1 << 31 - ge(i);
                      o.entanglements[1] |= d, i &= ~d;
                    }
                    $e(n), (vt & 6) === 0 && (ai = bt() + 500, ku(0));
                  }
                }
                break;
              case 31:
              case 13:
                o = ea(n, 2), o !== null && ye(o, n, 2), ni(), Hf(n, 2);
            }
          if (n = Yf(a), n === null && Af(
            t,
            e,
            a,
            _i,
            l
          ), n === u) break;
          u = n;
        }
        u !== null && a.stopPropagation();
      } else
        Af(
          t,
          e,
          a,
          null,
          l
        );
    }
  }
  function Yf(t) {
    return t = Li(t), Lf(t);
  }
  var _i = null;
  function Lf(t) {
    if (_i = null, t = ba(t), t !== null) {
      var e = g(t);
      if (e === null) t = null;
      else {
        var l = e.tag;
        if (l === 13) {
          if (t = N(e), t !== null) return t;
          t = null;
        } else if (l === 31) {
          if (t = D(e), t !== null) return t;
          t = null;
        } else if (l === 3) {
          if (e.stateNode.current.memoizedState.isDehydrated)
            return e.tag === 3 ? e.stateNode.containerInfo : null;
          t = null;
        } else e !== t && (t = null);
      }
    }
    return _i = t, null;
  }
  function Id(t) {
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
        switch (te()) {
          case ou:
            return 2;
          case is:
            return 8;
          case rn:
          case Ty:
            return 32;
          case cs:
            return 268435456;
          default:
            return 32;
        }
      default:
        return 32;
    }
  }
  var Gf = !1, Ql = null, Xl = null, Zl = null, en = /* @__PURE__ */ new Map(), ln = /* @__PURE__ */ new Map(), Vl = [], Lh = "mousedown mouseup touchcancel touchend touchstart auxclick dblclick pointercancel pointerdown pointerup dragend dragstart drop compositionend compositionstart keydown keypress keyup input textInput copy cut paste click change contextmenu reset".split(
    " "
  );
  function Pd(t, e) {
    switch (t) {
      case "focusin":
      case "focusout":
        Ql = null;
        break;
      case "dragenter":
      case "dragleave":
        Xl = null;
        break;
      case "mouseover":
      case "mouseout":
        Zl = null;
        break;
      case "pointerover":
      case "pointerout":
        en.delete(e.pointerId);
        break;
      case "gotpointercapture":
      case "lostpointercapture":
        ln.delete(e.pointerId);
    }
  }
  function an(t, e, l, a, u, n) {
    return t === null || t.nativeEvent !== n ? (t = {
      blockedOn: e,
      domEventName: l,
      eventSystemFlags: a,
      nativeEvent: n,
      targetContainers: [u]
    }, e !== null && (e = Sa(e), e !== null && Wd(e)), t) : (t.eventSystemFlags |= a, e = t.targetContainers, u !== null && e.indexOf(u) === -1 && e.push(u), t);
  }
  function Gh(t, e, l, a, u) {
    switch (e) {
      case "focusin":
        return Ql = an(
          Ql,
          t,
          e,
          l,
          a,
          u
        ), !0;
      case "dragenter":
        return Xl = an(
          Xl,
          t,
          e,
          l,
          a,
          u
        ), !0;
      case "mouseover":
        return Zl = an(
          Zl,
          t,
          e,
          l,
          a,
          u
        ), !0;
      case "pointerover":
        var n = u.pointerId;
        return en.set(
          n,
          an(
            en.get(n) || null,
            t,
            e,
            l,
            a,
            u
          )
        ), !0;
      case "gotpointercapture":
        return n = u.pointerId, ln.set(
          n,
          an(
            ln.get(n) || null,
            t,
            e,
            l,
            a,
            u
          )
        ), !0;
    }
    return !1;
  }
  function ty(t) {
    var e = ba(t.target);
    if (e !== null) {
      var l = g(e);
      if (l !== null) {
        if (e = l.tag, e === 13) {
          if (e = N(l), e !== null) {
            t.blockedOn = e, ys(t.priority, function() {
              Fd(l);
            });
            return;
          }
        } else if (e === 31) {
          if (e = D(l), e !== null) {
            t.blockedOn = e, ys(t.priority, function() {
              Fd(l);
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
  function Ei(t) {
    if (t.blockedOn !== null) return !1;
    for (var e = t.targetContainers; 0 < e.length; ) {
      var l = Yf(t.nativeEvent);
      if (l === null) {
        l = t.nativeEvent;
        var a = new l.constructor(
          l.type,
          l
        );
        Yi = a, l.target.dispatchEvent(a), Yi = null;
      } else
        return e = Sa(l), e !== null && Wd(e), t.blockedOn = l, !1;
      e.shift();
    }
    return !0;
  }
  function ey(t, e, l) {
    Ei(t) && l.delete(e);
  }
  function Qh() {
    Gf = !1, Ql !== null && Ei(Ql) && (Ql = null), Xl !== null && Ei(Xl) && (Xl = null), Zl !== null && Ei(Zl) && (Zl = null), en.forEach(ey), ln.forEach(ey);
  }
  function Ai(t, e) {
    t.blockedOn === e && (t.blockedOn = null, Gf || (Gf = !0, c.unstable_scheduleCallback(
      c.unstable_NormalPriority,
      Qh
    )));
  }
  var zi = null;
  function ly(t) {
    zi !== t && (zi = t, c.unstable_scheduleCallback(
      c.unstable_NormalPriority,
      function() {
        zi === t && (zi = null);
        for (var e = 0; e < t.length; e += 3) {
          var l = t[e], a = t[e + 1], u = t[e + 2];
          if (typeof a != "function") {
            if (Lf(a || l) === null)
              continue;
            break;
          }
          var n = Sa(l);
          n !== null && (t.splice(e, 3), e -= 3, Bc(
            n,
            {
              pending: !0,
              data: u,
              method: l.method,
              action: a
            },
            a,
            u
          ));
        }
      }
    ));
  }
  function eu(t) {
    function e(d) {
      return Ai(d, t);
    }
    Ql !== null && Ai(Ql, t), Xl !== null && Ai(Xl, t), Zl !== null && Ai(Zl, t), en.forEach(e), ln.forEach(e);
    for (var l = 0; l < Vl.length; l++) {
      var a = Vl[l];
      a.blockedOn === t && (a.blockedOn = null);
    }
    for (; 0 < Vl.length && (l = Vl[0], l.blockedOn === null); )
      ty(l), l.blockedOn === null && Vl.shift();
    if (l = (t.ownerDocument || t).$$reactFormReplay, l != null)
      for (a = 0; a < l.length; a += 3) {
        var u = l[a], n = l[a + 1], i = u[ce] || null;
        if (typeof n == "function")
          i || ly(l);
        else if (i) {
          var o = null;
          if (n && n.hasAttribute("formAction")) {
            if (u = n, i = n[ce] || null)
              o = i.formAction;
            else if (Lf(u) !== null) continue;
          } else o = i.action;
          typeof o == "function" ? l[a + 1] = o : (l.splice(a, 3), a -= 3), ly(l);
        }
      }
  }
  function ay() {
    function t(n) {
      n.canIntercept && n.info === "react-transition" && n.intercept({
        handler: function() {
          return new Promise(function(i) {
            return u = i;
          });
        },
        focusReset: "manual",
        scroll: "manual"
      });
    }
    function e() {
      u !== null && (u(), u = null), a || setTimeout(l, 20);
    }
    function l() {
      if (!a && !navigation.transition) {
        var n = navigation.currentEntry;
        n && n.url != null && navigation.navigate(n.url, {
          state: n.getState(),
          info: "react-transition",
          history: "replace"
        });
      }
    }
    if (typeof navigation == "object") {
      var a = !1, u = null;
      return navigation.addEventListener("navigate", t), navigation.addEventListener("navigatesuccess", e), navigation.addEventListener("navigateerror", e), setTimeout(l, 100), function() {
        a = !0, navigation.removeEventListener("navigate", t), navigation.removeEventListener("navigatesuccess", e), navigation.removeEventListener("navigateerror", e), u !== null && (u(), u = null);
      };
    }
  }
  function Qf(t) {
    this._internalRoot = t;
  }
  Ti.prototype.render = Qf.prototype.render = function(t) {
    var e = this._internalRoot;
    if (e === null) throw Error(s(409));
    var l = e.current, a = ze();
    kd(l, a, t, e, null, null);
  }, Ti.prototype.unmount = Qf.prototype.unmount = function() {
    var t = this._internalRoot;
    if (t !== null) {
      this._internalRoot = null;
      var e = t.containerInfo;
      kd(t.current, 2, null, t, null, null), ni(), e[pa] = null;
    }
  };
  function Ti(t) {
    this._internalRoot = t;
  }
  Ti.prototype.unstable_scheduleHydration = function(t) {
    if (t) {
      var e = ds();
      t = { blockedOn: null, target: t, priority: e };
      for (var l = 0; l < Vl.length && e !== 0 && e < Vl[l].priority; l++) ;
      Vl.splice(l, 0, t), l === 0 && ty(t);
    }
  };
  var uy = f.version;
  if (uy !== "19.2.5")
    throw Error(
      s(
        527,
        uy,
        "19.2.5"
      )
    );
  H.findDOMNode = function(t) {
    var e = t._reactInternals;
    if (e === void 0)
      throw typeof t.render == "function" ? Error(s(188)) : (t = Object.keys(t).join(","), Error(s(268, t)));
    return t = p(e), t = t !== null ? j(t) : null, t = t === null ? null : t.stateNode, t;
  };
  var Xh = {
    bundleType: 0,
    version: "19.2.5",
    rendererPackageName: "react-dom",
    currentDispatcherRef: q,
    reconcilerVersion: "19.2.5"
  };
  if (typeof __REACT_DEVTOOLS_GLOBAL_HOOK__ < "u") {
    var xi = __REACT_DEVTOOLS_GLOBAL_HOOK__;
    if (!xi.isDisabled && xi.supportsFiber)
      try {
        du = xi.inject(
          Xh
        ), ve = xi;
      } catch {
      }
  }
  return un.createRoot = function(t, e) {
    if (!h(t)) throw Error(s(299));
    var l = !1, a = "", u = ro, n = oo, i = yo;
    return e != null && (e.unstable_strictMode === !0 && (l = !0), e.identifierPrefix !== void 0 && (a = e.identifierPrefix), e.onUncaughtError !== void 0 && (u = e.onUncaughtError), e.onCaughtError !== void 0 && (n = e.onCaughtError), e.onRecoverableError !== void 0 && (i = e.onRecoverableError)), e = Jd(
      t,
      1,
      !1,
      null,
      null,
      l,
      a,
      null,
      u,
      n,
      i,
      ay
    ), t[pa] = e.current, Ef(t), new Qf(e);
  }, un.hydrateRoot = function(t, e, l) {
    if (!h(t)) throw Error(s(299));
    var a = !1, u = "", n = ro, i = oo, o = yo, d = null;
    return l != null && (l.unstable_strictMode === !0 && (a = !0), l.identifierPrefix !== void 0 && (u = l.identifierPrefix), l.onUncaughtError !== void 0 && (n = l.onUncaughtError), l.onCaughtError !== void 0 && (i = l.onCaughtError), l.onRecoverableError !== void 0 && (o = l.onRecoverableError), l.formState !== void 0 && (d = l.formState)), e = Jd(
      t,
      1,
      !0,
      e,
      l ?? null,
      a,
      u,
      d,
      n,
      i,
      o,
      ay
    ), e.context = wd(null), l = e.current, a = ze(), a = Mi(a), u = Ol(a), u.callback = null, Ml(l, u, a), l = a, e.current.lanes = l, mu(e, l), $e(e), t[pa] = e.current, Ef(t), new Ti(e);
  }, un.version = "19.2.5", un;
}
var yy;
function Ih() {
  if (yy) return Vf.exports;
  yy = 1;
  function c() {
    if (!(typeof __REACT_DEVTOOLS_GLOBAL_HOOK__ > "u" || typeof __REACT_DEVTOOLS_GLOBAL_HOOK__.checkDCE != "function"))
      try {
        __REACT_DEVTOOLS_GLOBAL_HOOK__.checkDCE(c);
      } catch (f) {
        console.error(f);
      }
  }
  return c(), Vf.exports = Fh(), Vf.exports;
}
var Ph = Ih(), kf = { exports: {} }, nn = {};
/**
 * @license React
 * react-jsx-runtime.production.js
 *
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 */
var my;
function tv() {
  if (my) return nn;
  my = 1;
  var c = Symbol.for("react.transitional.element"), f = Symbol.for("react.fragment");
  function r(s, h, g) {
    var N = null;
    if (g !== void 0 && (N = "" + g), h.key !== void 0 && (N = "" + h.key), "key" in h) {
      g = {};
      for (var D in h)
        D !== "key" && (g[D] = h[D]);
    } else g = h;
    return h = g.ref, {
      $$typeof: c,
      type: s,
      key: N,
      ref: h !== void 0 ? h : null,
      props: g
    };
  }
  return nn.Fragment = f, nn.jsx = r, nn.jsxs = r, nn;
}
var hy;
function ev() {
  return hy || (hy = 1, kf.exports = tv()), kf.exports;
}
var z = ev();
function lv(c) {
  return typeof c.questionId == "string";
}
function av(c) {
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
function qi(c) {
  return c >= "0" && c <= "9";
}
function Sy(c) {
  return c >= "a" && c <= "z" || c >= "A" && c <= "Z";
}
function nv(c) {
  return Sy(c) || qi(c);
}
function iv(c) {
  return c === " " || c === "	" || c === `
` || c === "\r" || c === "\f" || c === "\v";
}
function cv(c) {
  const f = [];
  let r = 0;
  const s = () => r >= c.length, h = (E = 0) => c.charAt(r + E), g = (E) => {
    if (r + E.length > c.length)
      return !1;
    for (let C = 0; C < E.length; C++)
      if (c.charAt(r + C) !== E.charAt(C))
        return !1;
    return r += E.length, !0;
  }, N = () => {
    for (; !s() && iv(h()); )
      r++;
  }, D = (E) => {
    for (; !s() && qi(h()); )
      r++;
    if (!s() && h() === ".")
      for (r++; !s() && qi(h()); )
        r++;
    const C = c.substring(E, r), G = parseFloat(C);
    return { kind: "Number", text: C, literal: G, position: E };
  }, x = (E, C) => {
    r++;
    let G = "";
    for (; !s() && h() !== C; ) {
      const K = h();
      if (K === "\\" && r + 1 < c.length) {
        const J = h(1), Q = {
          n: `
`,
          t: "	",
          r: "\r",
          "\\": "\\",
          "'": "'",
          '"': '"'
        }[J];
        if (Q === void 0)
          throw new Fe(`unknown escape '\\${J}'.`, r);
        G += Q, r += 2;
      } else
        G += K, r++;
    }
    if (s())
      throw new Fe("unterminated string literal.", E);
    return r++, { kind: "String", text: G, literal: G, position: E };
  }, p = (E) => {
    for (; !s(); ) {
      const G = h();
      if (G === "_" || G === "-" || nv(G))
        r++;
      else
        break;
    }
    const C = c.substring(E, r);
    return C === "true" ? { kind: "True", text: C, literal: !0, position: E } : C === "false" ? { kind: "False", text: C, literal: !1, position: E } : C === "null" ? { kind: "Null", text: C, literal: null, position: E } : { kind: "Identifier", text: C, literal: null, position: E };
  }, j = () => {
    const E = r, C = h();
    if (qi(C))
      return D(E);
    if (C === "'" || C === '"')
      return x(E, C);
    if (C === "_" || Sy(C))
      return p(E);
    switch (C) {
      case "(":
        return r++, { kind: "LParen", text: "(", literal: null, position: E };
      case ")":
        return r++, { kind: "RParen", text: ")", literal: null, position: E };
      case "[":
        return r++, { kind: "LBracket", text: "[", literal: null, position: E };
      case "]":
        return r++, { kind: "RBracket", text: "]", literal: null, position: E };
      case ",":
        return r++, { kind: "Comma", text: ",", literal: null, position: E };
      case ".":
        return r++, { kind: "Dot", text: ".", literal: null, position: E };
      case "=":
        if (g("==="))
          return { kind: "StrictEq", text: "===", literal: null, position: E };
        if (g("=="))
          return { kind: "Eq", text: "==", literal: null, position: E };
        throw new Fe("bare '=' is not a valid operator (use '==' or '===').", E);
      case "!":
        return g("!==") ? { kind: "StrictNotEq", text: "!==", literal: null, position: E } : g("!=") ? { kind: "NotEq", text: "!=", literal: null, position: E } : (r++, { kind: "Not", text: "!", literal: null, position: E });
      case "<":
        return g("<=") ? { kind: "LtEq", text: "<=", literal: null, position: E } : (r++, { kind: "Lt", text: "<", literal: null, position: E });
      case ">":
        return g(">=") ? { kind: "GtEq", text: ">=", literal: null, position: E } : (r++, { kind: "Gt", text: ">", literal: null, position: E });
      case "&":
        if (g("&&"))
          return { kind: "And", text: "&&", literal: null, position: E };
        throw new Fe("expected '&&'.", E);
      case "|":
        if (g("||"))
          return { kind: "Or", text: "||", literal: null, position: E };
        throw new Fe("expected '||'.", E);
    }
    throw new Fe(`unexpected character '${C}'.`, E);
  };
  for (; ; ) {
    if (N(), s())
      return f.push({ kind: "EndOfInput", text: "", literal: null, position: r }), f;
    f.push(j());
  }
}
function fv(c) {
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
    const Q = r();
    if (Q.kind !== B)
      throw new Fe(`expected ${B}, got '${Q.text}'.`, Q.position);
    return s(), Q;
  }, N = () => {
    let B = D();
    for (; h("Or"); )
      B = { kind: "BinaryOp", op: "||", left: B, right: D() };
    return B;
  }, D = () => {
    let B = x();
    for (; h("And"); )
      B = { kind: "BinaryOp", op: "&&", left: B, right: x() };
    return B;
  }, x = () => {
    let B = p();
    for (; ; ) {
      const Q = r().kind;
      let xt = null;
      if (Q === "Eq" || Q === "StrictEq" ? xt = "==" : (Q === "NotEq" || Q === "StrictNotEq") && (xt = "!="), xt === null)
        break;
      s(), B = { kind: "BinaryOp", op: xt, left: B, right: p() };
    }
    return B;
  }, p = () => {
    let B = j();
    for (; ; ) {
      const Q = r().kind;
      let xt = null;
      if (Q === "Lt" ? xt = "<" : Q === "Gt" ? xt = ">" : Q === "LtEq" ? xt = "<=" : Q === "GtEq" && (xt = ">="), xt === null)
        break;
      s(), B = { kind: "BinaryOp", op: xt, left: B, right: j() };
    }
    return B;
  }, j = () => h("Not") ? { kind: "UnaryOp", op: "!", operand: j() } : K(), E = () => {
    g("LBracket");
    const B = [];
    if (r().kind !== "RBracket")
      for (B.push(N()); h("Comma"); )
        B.push(N());
    return g("RBracket"), { kind: "Array", items: B };
  }, C = (B) => {
    let Q;
    if (h("Dot"))
      Q = g("Identifier").text;
    else if (h("LBracket")) {
      const xt = g("String");
      g("RBracket"), Q = xt.literal;
    } else
      throw new Fe("'answers' must be followed by .key or ['key'].", B);
    return { kind: "AnswersAccess", key: Q };
  }, G = () => {
    const B = s();
    if (B.text === "answers")
      return C(B.position);
    g("LParen");
    const Q = [];
    if (r().kind !== "RParen")
      for (Q.push(N()); h("Comma"); )
        Q.push(N());
    return g("RParen"), { kind: "Call", name: B.text, args: Q };
  }, K = () => {
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
        const Q = N();
        return g("RParen"), Q;
      }
      case "LBracket":
        return E();
      case "Identifier":
        return G();
      default:
        throw new Fe(`unexpected token '${B.text}'.`, B.position);
    }
  }, J = N();
  return g("EndOfInput"), J;
}
function pl(c) {
  return c === void 0 || c === null ? null : typeof c == "boolean" || typeof c == "number" || typeof c == "string" ? c : Array.isArray(c) ? c.map(pl) : null;
}
function va(c, f) {
  const r = pl(c), s = pl(f);
  if (r === null || s === null)
    return r === null && s === null;
  if (typeof r == "number" && typeof s == "number" || typeof r == "string" && typeof s == "string" || typeof r == "boolean" && typeof s == "boolean")
    return r === s;
  if (Array.isArray(r) && Array.isArray(s)) {
    if (r.length !== s.length)
      return !1;
    for (let h = 0; h < r.length; h++)
      if (!va(r[h], s[h]))
        return !1;
    return !0;
  }
  return !1;
}
function $l(c, f) {
  const r = pl(c), s = pl(f);
  if (typeof r == "number" && typeof s == "number" || typeof r == "string" && typeof s == "string")
    return r < s ? -1 : r > s ? 1 : 0;
  throw new Error("Comparison operators require two numbers or two strings.");
}
function uu(c) {
  const f = pl(c);
  return f === null ? !1 : typeof f == "boolean" ? f : typeof f == "number" ? f !== 0 : typeof f == "string" || Array.isArray(f) ? f.length > 0 : !0;
}
function He(c, f) {
  switch (c.kind) {
    case "Literal":
      return c.value;
    case "AnswersAccess":
      return yv(c.key, f);
    case "UnaryOp":
      return sv(c, f);
    case "BinaryOp":
      return rv(c, f);
    case "Call":
      return ov(c, f);
    case "Array":
      return c.items.map((r) => He(r, f));
  }
}
function sv(c, f) {
  const r = He(c.operand, f);
  if (c.op === "!")
    return !uu(r);
  throw new Error(`Unknown unary operator '${c.op}'.`);
}
function rv(c, f) {
  if (c.op === "&&") {
    const h = He(c.left, f);
    return uu(h) ? uu(He(c.right, f)) : !1;
  }
  if (c.op === "||") {
    const h = He(c.left, f);
    return uu(h) ? !0 : uu(He(c.right, f));
  }
  const r = He(c.left, f), s = He(c.right, f);
  switch (c.op) {
    case "==":
      return va(r, s);
    case "!=":
      return !va(r, s);
    case "<":
      return $l(r, s) < 0;
    case ">":
      return $l(r, s) > 0;
    case "<=":
      return $l(r, s) <= 0;
    case ">=":
      return $l(r, s) >= 0;
    default:
      throw new Error(`Unknown binary operator '${c.op}'.`);
  }
}
function ov(c, f) {
  switch (c.name) {
    case "has":
    case "isSet":
      return vy(c, f);
    case "isNotSet":
      return !vy(c, f);
    case "in":
      return dv(c, f);
    default:
      throw new Error(`Unknown function '${c.name}'.`);
  }
}
function vy(c, f) {
  if (c.args.length !== 1)
    throw new Error(`${c.name}() takes one argument.`);
  const r = c.args[0];
  if (!r)
    return !1;
  const s = He(r, f);
  return typeof s != "string" ? !1 : s in f && f[s] !== null && f[s] !== void 0;
}
function dv(c, f) {
  if (c.args.length !== 2)
    throw new Error("in() takes two arguments: in(value, [array]).");
  const r = c.args[0], s = c.args[1];
  if (!r || !s)
    return !1;
  const h = He(r, f), g = He(s, f);
  return Array.isArray(g) ? g.some((N) => va(h, N)) : !1;
}
function yv(c, f) {
  return c in f ? pl(f[c]) : null;
}
function mv(c) {
  const f = cv(c);
  return fv(f);
}
function hv(c, f) {
  try {
    const r = typeof c == "string" ? mv(c) : c;
    return uu(He(r, f));
  } catch {
    return !1;
  }
}
function vv(c, f) {
  var r;
  if (!c.logic)
    return null;
  for (const s of c.logic)
    if (ls(s.if, f))
      return ((r = s.then) == null ? void 0 : r.goto) ?? null;
  return null;
}
function ls(c, f) {
  try {
    return lv(c) ? pv(c, f) : av(c) ? gv(c, f) : uv(c) ? hv(c.expression, f) : !1;
  } catch {
    return !1;
  }
}
function gv(c, f) {
  return c.all && c.all.length > 0 ? c.all.every((r) => ls(r, f)) : c.any && c.any.length > 0 ? c.any.some((r) => ls(r, f)) : !1;
}
function pv(c, f) {
  const r = c.questionId in f && f[c.questionId] !== null && f[c.questionId] !== void 0;
  if (c.op === "isSet")
    return r;
  if (c.op === "isNotSet")
    return !r;
  if (c.value === void 0)
    return !1;
  const s = r ? pl(f[c.questionId]) : null, h = pl(c.value);
  return bv(c.op, s, h);
}
function bv(c, f, r) {
  switch (c) {
    case "==":
      return va(f, r);
    case "!=":
      return !va(f, r);
    case ">":
      return $l(f, r) > 0;
    case ">=":
      return $l(f, r) >= 0;
    case "<":
      return $l(f, r) < 0;
    case "<=":
      return $l(f, r) <= 0;
    case "in":
      return gy(r, f);
    case "notIn":
      return !gy(r, f);
    default:
      return !1;
  }
}
function gy(c, f) {
  return Array.isArray(c) ? c.some((r) => va(f, r)) : !1;
}
function cn(c, f, r) {
  const s = new Set(c.screens.map((D) => D.id)), h = c.screens.find((D) => D.id === f);
  if (h && (!h.questions || h.questions.length === 0) && !h.nextScreen)
    return { kind: "end" };
  const g = vv(c, r);
  if (g && g !== f && s.has(g))
    return { kind: "screen", screenId: g };
  if (h != null && h.nextScreen && h.nextScreen !== f && s.has(h.nextScreen))
    return { kind: "screen", screenId: h.nextScreen };
  const N = c.screens.findIndex((D) => D.id === f);
  if (N >= 0 && N + 1 < c.screens.length) {
    const D = c.screens[N + 1];
    if (D)
      return { kind: "screen", screenId: D.id };
  }
  return { kind: "end" };
}
function Sv(c, f, r, s) {
  const h = new Set(f.screens.map((g) => g.id));
  return c.nextScreen && h.has(c.nextScreen) ? { kind: "screen", screenId: c.nextScreen } : cn(f, r, s);
}
const yt = (c, f, r, s) => ({ questionId: c, code: f, message: r, ...s ? { params: s } : {} }), me = (c) => typeof c == "number" && Number.isFinite(c);
function $f(c) {
  if (!/^\d{4}-\d{2}-\d{2}$/.test(c))
    return null;
  const [f, r, s] = c.split("-").map((g) => Number.parseInt(g, 10)), h = new Date(Date.UTC(f, r - 1, s));
  return h.getUTCFullYear() !== f || h.getUTCMonth() !== r - 1 || h.getUTCDate() !== s ? null : h.getTime();
}
function Wf(c) {
  const f = Date.parse(c);
  return Number.isNaN(f) ? null : f;
}
function _y(c, f) {
  const r = c.id, s = [];
  switch (c.type) {
    case "text": {
      if (typeof f != "string") {
        s.push(yt(r, "type", "Text answer must be a JSON string."));
        break;
      }
      const h = c.minLength, g = c.maxLength, N = c.pattern;
      if (me(h) && f.length < h && s.push(yt(r, "minLength", `Answer length ${f.length} is less than minLength ${h}.`, { n: h, actual: f.length })), me(g) && f.length > g && s.push(yt(r, "maxLength", `Answer length ${f.length} exceeds maxLength ${g}.`, { n: g, actual: f.length })), typeof N == "string" && N.length > 0)
        try {
          new RegExp(N).test(f) || s.push(yt(r, "pattern", "Answer does not match the required pattern."));
        } catch {
        }
      break;
    }
    case "paragraph": {
      if (typeof f != "string") {
        s.push(yt(r, "type", "Paragraph answer must be a JSON string."));
        break;
      }
      const h = c.minLength, g = c.maxLength;
      me(h) && f.length < h && s.push(yt(r, "minLength", `Answer length ${f.length} is less than minLength ${h}.`, { n: h, actual: f.length })), me(g) && f.length > g && s.push(yt(r, "maxLength", `Answer length ${f.length} exceeds maxLength ${g}.`, { n: g, actual: f.length }));
      break;
    }
    case "number": {
      if (!me(f)) {
        s.push(yt(r, "type", "Number answer must be a JSON number."));
        break;
      }
      const h = c.min, g = c.max;
      me(h) && f < h && s.push(yt(r, "min", `Answer ${f} is less than min ${h}.`, { n: h })), me(g) && f > g && s.push(yt(r, "max", `Answer ${f} exceeds max ${g}.`, { n: g }));
      break;
    }
    case "rating": {
      if (!me(f)) {
        s.push(yt(r, "type", "Rating answer must be a JSON number."));
        break;
      }
      const h = me(c.max) ? c.max : 0;
      (f < 0 || f > h) && s.push(yt(r, "range", `Rating ${f} is outside 0..${h}.`, { min: 0, max: h })), c.allowHalf !== !0 && f !== Math.floor(f) && s.push(yt(r, "halfNotAllowed", "Rating does not allow half values."));
      break;
    }
    case "nps": {
      if (!me(f) || !Number.isInteger(f)) {
        s.push(yt(r, "type", "NPS answer must be a JSON number."));
        break;
      }
      const h = me(c.min) ? c.min : 0, g = me(c.max) ? c.max : 10;
      (f < h || f > g) && s.push(yt(r, "range", `NPS answer ${f} is outside ${h}..${g}.`, { min: h, max: g }));
      break;
    }
    case "singleChoice":
    case "dropdown":
    case "navigationList": {
      if (typeof f != "string") {
        s.push(yt(r, "type", "Choice answer must be a JSON string (option id)."));
        break;
      }
      if (c.optionsSource != null)
        break;
      (Array.isArray(c.options) ? c.options : []).some((g) => g.id === f) || s.push(yt(r, "invalidOption", `'${f}' is not a valid option id for this question.`, { option: f }));
      break;
    }
    case "multiChoice": {
      if (!Array.isArray(f)) {
        s.push(yt(r, "type", "MultiChoice answer must be a JSON array of option ids."));
        break;
      }
      const h = Array.isArray(c.options) ? c.options : [], g = new Set(h.map((j) => j.id)), N = [];
      let D = !1;
      for (const j of f) {
        if (typeof j != "string") {
          s.push(yt(r, "type", "Each MultiChoice array entry must be a string option id.")), D = !0;
          break;
        }
        N.push(j);
      }
      if (D)
        break;
      if (c.optionsSource == null)
        for (const j of N)
          g.has(j) || s.push(yt(r, "invalidOption", `'${j}' is not a valid option id for this question.`, { option: j }));
      const x = c.minSelected, p = c.maxSelected;
      me(x) && N.length < x && s.push(yt(r, "minSelected", `At least ${x} option(s) must be selected.`, { n: x })), me(p) && N.length > p && s.push(yt(r, "maxSelected", `At most ${p} option(s) may be selected.`, { n: p }));
      break;
    }
    case "date": {
      if (typeof f != "string") {
        s.push(yt(r, "type", "Date answer must be a JSON string in yyyy-MM-dd format."));
        break;
      }
      const h = $f(f);
      if (h === null) {
        s.push(yt(r, "invalidDate", `Date '${f}' is not yyyy-MM-dd.`));
        break;
      }
      const g = c.minDate, N = c.maxDate;
      if (typeof g == "string") {
        const D = $f(g);
        D !== null && h < D && s.push(yt(r, "minDate", `Date ${f} is before minDate ${g}.`, { min: g }));
      }
      if (typeof N == "string") {
        const D = $f(N);
        D !== null && h > D && s.push(yt(r, "maxDate", `Date ${f} is after maxDate ${N}.`, { max: N }));
      }
      break;
    }
    case "dateTime": {
      if (typeof f != "string") {
        s.push(yt(r, "type", "DateTime answer must be a JSON string in ISO 8601 format."));
        break;
      }
      const h = Wf(f);
      if (h === null) {
        s.push(yt(r, "invalidDateTime", `DateTime '${f}' is not valid ISO 8601.`));
        break;
      }
      const g = c.minDateTime, N = c.maxDateTime;
      if (typeof g == "string" && g.length > 0) {
        const D = Wf(g);
        D !== null && h < D && s.push(yt(r, "minDateTime", `DateTime is before minDateTime ${g}.`, { min: g }));
      }
      if (typeof N == "string" && N.length > 0) {
        const D = Wf(N);
        D !== null && h > D && s.push(yt(r, "maxDateTime", `DateTime is after maxDateTime ${N}.`, { max: N }));
      }
      break;
    }
    case "file": {
      (typeof f != "string" || f.length === 0) && s.push(yt(r, "empty", "Answer must be a non-empty file reference string."));
      break;
    }
    case "signature": {
      (typeof f != "string" || f.length === 0) && s.push(yt(r, "empty", "Answer must be a non-empty signature data url string."));
      break;
    }
    case "yesNo": {
      typeof f != "boolean" && s.push(yt(r, "type", "Yes/No answer must be a JSON boolean."));
      break;
    }
  }
  return s;
}
function _v(c, f) {
  const r = [];
  for (const s of c ?? []) {
    const h = s, g = h.id;
    if (typeof g != "string")
      continue;
    const N = f[g];
    N != null && r.push(..._y(h, N));
  }
  return r;
}
function Ff(c, f) {
  let r = c;
  for (const s of f.split(".")) {
    if (r === null || typeof r != "object")
      return;
    r = r[s];
  }
  return r;
}
function Ey(c) {
  const f = new URL(c.url);
  for (const [r, s] of Object.entries(c.queryParams ?? {}))
    f.searchParams.set(r, s);
  return f.toString();
}
function Ev(c, f) {
  const r = f.itemsPath ? Ff(c, f.itemsPath) : c;
  if (!Array.isArray(r))
    throw new Error(`optionsSource response is not an array${f.itemsPath ? ` at '${f.itemsPath}'` : ""}.`);
  const s = f.valuePath || "ID", h = f.labelPath || "Name", g = [];
  for (const N of r) {
    const D = Ff(N, s);
    if (D == null || D === "")
      continue;
    const x = Ff(N, h);
    g.push({
      id: String(D),
      label: x == null || x === "" ? String(D) : String(x)
    });
  }
  return g;
}
async function Av(c, f) {
  const r = (f == null ? void 0 : f.fetchImpl) ?? fetch, s = {};
  f != null && f.locale && (s["Accept-Language"] = f.locale), Object.assign(s, c.headers ?? {});
  const h = await r(Ey(c), {
    headers: s,
    ...f != null && f.signal ? { signal: f.signal } : {}
  });
  if (!h.ok)
    throw new Error(`optionsSource fetch failed: HTTP ${h.status}.`);
  return Ev(await h.json(), c);
}
class lu extends Error {
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
class py {
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
      throw new lu({
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
      throw new lu({
        status: f.status,
        code: "parse",
        message: `Empty body from ${f.url}`
      });
    try {
      return JSON.parse(r);
    } catch (s) {
      throw new lu({
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
      return new lu({
        status: f.status,
        code: h,
        message: `${r} ${s} → ${f.status}`
      });
    let N;
    try {
      N = JSON.parse(g);
    } catch {
      return new lu({
        status: f.status,
        code: h,
        message: `${r} ${s} → ${f.status}: ${g.slice(0, 200)}`,
        raw: g
      });
    }
    const D = N.Message ?? N.message, x = N.Errors ?? N.errors, p = Array.isArray(x) ? x.flatMap((j) => {
      const E = j.QuestionId ?? j.questionId, C = j.Message ?? j.message;
      return E && C ? [{ questionId: E, message: C }] : [];
    }) : void 0;
    return new lu({
      status: f.status,
      code: h,
      message: `${r} ${s} → ${f.status}${D ? ": " + D : ""}`,
      serverMessage: D,
      validationErrors: p && p.length > 0 ? p : void 0,
      raw: N
    });
  }
}
function by(c) {
  const f = c.trim().replace(/^#/, "");
  if (!/^[0-9a-fA-F]{3}$|^[0-9a-fA-F]{6}$|^[0-9a-fA-F]{8}$/.test(f)) return null;
  const r = f.length === 3 ? f.split("").map((s) => s + s).join("") : f.slice(0, 6);
  return [
    parseInt(r.slice(0, 2), 16),
    parseInt(r.slice(2, 4), 16),
    parseInt(r.slice(4, 6), 16)
  ];
}
function If([c, f, r]) {
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
function xv(c) {
  const f = {}, r = c != null && c.primaryColor ? by(c.primaryColor) : null;
  r && (f["--survey-primary"] = If(r), f["--survey-primary-hover"] = If(zv(r, 0.82)), f["--survey-primary-contrast"] = Tv(r) > 0.45 ? "#111111" : "#ffffff");
  const s = c != null && c.secondaryColor ? by(c.secondaryColor) : null;
  return s && (f["--survey-accent"] = If(s)), f;
}
const Ay = F.createContext(null), qv = Ay.Provider;
function le() {
  const c = F.useContext(Ay);
  if (!c)
    throw new Error(
      "useSurveyContext must be used inside <SurveyRenderer>. Question components rely on survey state from the enclosing provider."
    );
  return c;
}
function tt(c, f, r) {
  if (c == null) return "";
  if (typeof c == "string") return c;
  if (c[f]) return c[f];
  if (r && c[r]) return c[r];
  const s = Object.keys(c);
  return s.length > 0 ? c[s[0]] : "";
}
const zy = {
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
    no: "No"
  }
}, Nv = {
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
    no: "لا"
  }
}, Ov = { en: zy, ar: Nv };
function ma(c, f) {
  return f ? c.replace(
    /\{(\w+)\}/g,
    (r, s) => s in f ? String(f[s]) : r
  ) : c;
}
function Mv(c, f, r) {
  const s = { ...Ov, ...r ?? {} };
  return s[c] ?? (f ? s[f] : void 0) ?? s.en ?? zy;
}
const Dv = "adp-surveys", Uv = 1;
function Cv(c = {}) {
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
  const N = (D, x) => {
    const p = {
      source: Dv,
      version: Uv,
      type: D,
      payload: x
    };
    try {
      h.postMessage(p, g);
    } catch {
    }
  };
  return {
    loaded: () => N("survey:loaded", {}),
    screenChanged: (D) => N("survey:screen-changed", { screenId: D }),
    completed: (D) => N("survey:completed", D),
    error: (D) => N("survey:error", { message: D }),
    resize: (D) => N("survey:resize", { height: D })
  };
}
function ns(c) {
  return `adp-surveys:resume:${c}`;
}
function jv(c, f) {
  try {
    const r = c.getItem(ns(f));
    if (!r) return null;
    const s = JSON.parse(r);
    return !s || typeof s != "object" || !s.answers ? null : s;
  } catch {
    return null;
  }
}
function Rv(c, f, r) {
  try {
    const s = { ...r, savedAt: Date.now() };
    c.setItem(ns(f), JSON.stringify(s));
  } catch {
  }
}
function Hv(c, f) {
  try {
    c.removeItem(ns(f));
  } catch {
  }
}
const Pf = /* @__PURE__ */ new Map();
function Bv({
  question: c,
  Component: f
}) {
  const { locale: r, schema: s, ui: h } = le(), g = c.optionsSource, N = `${r}|${Ey(g)}`, [D, x] = F.useState(() => {
    const J = Pf.get(N);
    return J ? { status: "ready", options: J } : { status: "loading" };
  }), [p, j] = F.useState(0);
  F.useEffect(() => {
    const J = Pf.get(N);
    if (J) {
      x({ status: "ready", options: J });
      return;
    }
    let B = !1;
    return x({ status: "loading" }), Av(g, { locale: r }).then((Q) => {
      Pf.set(N, Q), B || x({ status: "ready", options: Q });
    }).catch((Q) => {
      B || x({ status: "error", message: Q.message ?? String(Q) });
    }), () => {
      B = !0;
    };
  }, [N, p]);
  const E = c.title, C = E ? /* @__PURE__ */ z.jsx("span", { className: "survey-question__label", children: tt(E, r, s.defaultLocale) }) : null;
  if (D.status === "loading")
    return /* @__PURE__ */ z.jsxs("div", { className: "survey-question survey-question--options-loading", role: "status", children: [
      C,
      /* @__PURE__ */ z.jsx("p", { className: "survey-question__options-status", children: h.loadingOptions })
    ] });
  if (D.status === "error")
    return /* @__PURE__ */ z.jsxs("div", { className: "survey-question survey-question--options-error", children: [
      C,
      /* @__PURE__ */ z.jsx("p", { className: "survey-question__options-status", role: "alert", children: h.optionsLoadError }),
      /* @__PURE__ */ z.jsx(
        "button",
        {
          type: "button",
          className: "survey-button survey-button--retry",
          onClick: () => j((J) => J + 1),
          children: h.retry
        }
      )
    ] });
  const G = c.type === "navigationList", K = {
    ...c,
    options: D.options.map((J) => ({
      id: J.id,
      label: { [r]: J.label },
      ...G && g.nextScreen ? { nextScreen: g.nextScreen } : {}
    }))
  };
  return /* @__PURE__ */ z.jsx(f, { question: K });
}
function Yv({
  question: c,
  registry: f
}) {
  const { ui: r } = le(), s = c.type, h = s ? f[s] : void 0;
  if (!h)
    return /* @__PURE__ */ z.jsx("div", { className: "survey-question survey-question--unknown", children: /* @__PURE__ */ z.jsxs("em", { children: [
      r.unsupportedQuestion,
      " ",
      String(s ?? "missing")
    ] }) });
  const g = Array.isArray(c.options) && c.options.length > 0;
  return c.optionsSource != null && !g ? /* @__PURE__ */ z.jsx(Bv, { question: c, Component: h }) : /* @__PURE__ */ z.jsx(h, { question: c });
}
function Lv({ question: c }) {
  const { locale: f, schema: r, answers: s, setAnswer: h } = le(), g = c.id, N = c.title, D = c.help, x = !!c.required, p = s[g] ?? "";
  return /* @__PURE__ */ z.jsxs("div", { className: "survey-question survey-question--text", children: [
    /* @__PURE__ */ z.jsxs("label", { className: "survey-question__label", htmlFor: `q-${g}`, children: [
      tt(N, f, r.defaultLocale),
      x && /* @__PURE__ */ z.jsx("span", { "aria-label": "required", className: "survey-question__required", children: " *" })
    ] }),
    D && /* @__PURE__ */ z.jsx("p", { className: "survey-question__help", children: tt(D, f, r.defaultLocale) }),
    /* @__PURE__ */ z.jsx(
      "input",
      {
        id: `q-${g}`,
        className: "survey-question__input",
        type: "text",
        value: p,
        required: x,
        onChange: (j) => h(g, j.target.value)
      }
    )
  ] });
}
function Gv({ question: c }) {
  const { locale: f, schema: r, answers: s, setAnswer: h } = le(), g = c.id, N = c.title, D = c.help, x = !!c.required, p = Number(c.min ?? 0), j = Number(c.max ?? 10), E = c.lowLabel, C = c.highLabel, G = s[g], K = [];
  for (let J = p; J <= j; J++) K.push(J);
  return /* @__PURE__ */ z.jsxs("fieldset", { className: "survey-question survey-question--nps", children: [
    /* @__PURE__ */ z.jsxs("legend", { className: "survey-question__label", children: [
      tt(N, f, r.defaultLocale),
      x && /* @__PURE__ */ z.jsx("span", { "aria-label": "required", className: "survey-question__required", children: " *" })
    ] }),
    D && /* @__PURE__ */ z.jsx("p", { className: "survey-question__help", children: tt(D, f, r.defaultLocale) }),
    /* @__PURE__ */ z.jsx("div", { className: "survey-question__nps-scale", role: "radiogroup", children: K.map((J) => {
      const B = G === J;
      return /* @__PURE__ */ z.jsx(
        "button",
        {
          type: "button",
          role: "radio",
          "aria-checked": B,
          className: "survey-question__nps-step" + (B ? " survey-question__nps-step--selected" : ""),
          onClick: () => h(g, J),
          children: J
        },
        J
      );
    }) }),
    (E || C) && /* @__PURE__ */ z.jsxs("div", { className: "survey-question__nps-labels", children: [
      /* @__PURE__ */ z.jsx("span", { children: E ? tt(E, f, r.defaultLocale) : "" }),
      /* @__PURE__ */ z.jsx("span", { children: C ? tt(C, f, r.defaultLocale) : "" })
    ] })
  ] });
}
function Qv({ question: c }) {
  const { locale: f, schema: r } = le(), s = c.id, h = c.title, g = c.help, N = c.options ?? [], D = (x, p) => {
    const j = {
      questionId: s,
      option: {
        id: p.id,
        nextScreen: p.nextScreen
      }
    };
    x.currentTarget.dispatchEvent(
      new CustomEvent("survey:navigationListSelect", {
        detail: j,
        bubbles: !0
      })
    );
  };
  return /* @__PURE__ */ z.jsxs("div", { className: "survey-question survey-question--navlist", children: [
    /* @__PURE__ */ z.jsx("div", { className: "survey-question__label", children: tt(h, f, r.defaultLocale) }),
    g && /* @__PURE__ */ z.jsx("p", { className: "survey-question__help", children: tt(g, f, r.defaultLocale) }),
    /* @__PURE__ */ z.jsx("ul", { className: "survey-navlist", role: "radiogroup", "aria-description": "Selecting an option navigates to the next screen.", children: N.map((x) => {
      const p = x.id, j = x.label;
      return /* @__PURE__ */ z.jsx("li", { className: "survey-navlist__row", children: /* @__PURE__ */ z.jsxs(
        "button",
        {
          type: "button",
          className: "survey-navlist__button",
          onClick: (E) => D(E, x),
          children: [
            /* @__PURE__ */ z.jsx("span", { className: "survey-navlist__label", children: tt(j, f, r.defaultLocale) }),
            /* @__PURE__ */ z.jsx("span", { "aria-hidden": "true", className: "survey-navlist__chevron", children: "›" })
          ]
        }
      ) }, p);
    }) })
  ] });
}
function Xv({ question: c }) {
  const { locale: f, schema: r, answers: s, setAnswer: h } = le(), g = c.id, N = c.title, D = c.help, x = c.placeholder, p = !!c.required, j = c.minLength, E = c.maxLength, C = s[g] ?? "";
  return /* @__PURE__ */ z.jsxs("div", { className: "survey-question survey-question--paragraph", children: [
    /* @__PURE__ */ z.jsxs("label", { className: "survey-question__label", htmlFor: `q-${g}`, children: [
      tt(N, f, r.defaultLocale),
      p && /* @__PURE__ */ z.jsx("span", { "aria-label": "required", className: "survey-question__required", children: " *" })
    ] }),
    D && /* @__PURE__ */ z.jsx("p", { className: "survey-question__help", children: tt(D, f, r.defaultLocale) }),
    /* @__PURE__ */ z.jsx(
      "textarea",
      {
        id: `q-${g}`,
        className: "survey-question__textarea",
        value: C,
        required: p,
        rows: 5,
        minLength: j,
        maxLength: E,
        placeholder: x ? tt(x, f, r.defaultLocale) : void 0,
        onChange: (G) => h(g, G.target.value)
      }
    )
  ] });
}
function Zv({ question: c }) {
  const { locale: f, schema: r, answers: s, setAnswer: h } = le(), g = c.id, N = c.title, D = c.help, x = !!c.required, p = c.min, j = c.max, E = c.step, C = c.unit, G = s[g], K = G == null ? "" : String(G);
  return /* @__PURE__ */ z.jsxs("div", { className: "survey-question survey-question--number", children: [
    /* @__PURE__ */ z.jsxs("label", { className: "survey-question__label", htmlFor: `q-${g}`, children: [
      tt(N, f, r.defaultLocale),
      x && /* @__PURE__ */ z.jsx("span", { "aria-label": "required", className: "survey-question__required", children: " *" })
    ] }),
    D && /* @__PURE__ */ z.jsx("p", { className: "survey-question__help", children: tt(D, f, r.defaultLocale) }),
    /* @__PURE__ */ z.jsxs("div", { className: "survey-question__number-wrap", children: [
      /* @__PURE__ */ z.jsx(
        "input",
        {
          id: `q-${g}`,
          className: "survey-question__input",
          type: "number",
          value: K,
          required: x,
          min: p,
          max: j,
          step: E,
          onChange: (J) => {
            const B = J.target.value;
            h(g, B === "" ? null : Number(B));
          }
        }
      ),
      C && /* @__PURE__ */ z.jsx("span", { className: "survey-question__unit", children: tt(C, f, r.defaultLocale) })
    ] })
  ] });
}
function Vv({ question: c }) {
  const { locale: f, schema: r, answers: s, setAnswer: h } = le(), g = c.id, N = c.title, D = c.help, x = !!c.required, p = Number(c.max ?? 5), j = s[g], E = [];
  for (let C = 1; C <= p; C++) E.push(C);
  return /* @__PURE__ */ z.jsxs("fieldset", { className: "survey-question survey-question--rating", children: [
    /* @__PURE__ */ z.jsxs("legend", { className: "survey-question__label", children: [
      tt(N, f, r.defaultLocale),
      x && /* @__PURE__ */ z.jsx("span", { "aria-label": "required", className: "survey-question__required", children: " *" })
    ] }),
    D && /* @__PURE__ */ z.jsx("p", { className: "survey-question__help", children: tt(D, f, r.defaultLocale) }),
    /* @__PURE__ */ z.jsx("div", { className: "survey-question__rating-scale", role: "radiogroup", children: E.map((C) => {
      const G = typeof j == "number" && C <= j;
      return /* @__PURE__ */ z.jsx(
        "button",
        {
          type: "button",
          role: "radio",
          "aria-checked": j === C,
          "aria-label": `${C}`,
          className: "survey-question__rating-star" + (G ? " survey-question__rating-star--selected" : ""),
          onClick: () => h(g, C),
          children: /* @__PURE__ */ z.jsx("span", { "aria-hidden": "true", children: "★" })
        },
        C
      );
    }) })
  ] });
}
function Kv({ question: c }) {
  const { locale: f, schema: r, answers: s, setAnswer: h } = le(), g = c.id, N = c.title, D = c.help, x = !!c.required, p = c.options ?? [], j = s[g];
  return /* @__PURE__ */ z.jsxs("fieldset", { className: "survey-question survey-question--single", children: [
    /* @__PURE__ */ z.jsxs("legend", { className: "survey-question__label", children: [
      tt(N, f, r.defaultLocale),
      x && /* @__PURE__ */ z.jsx("span", { "aria-label": "required", className: "survey-question__required", children: " *" })
    ] }),
    D && /* @__PURE__ */ z.jsx("p", { className: "survey-question__help", children: tt(D, f, r.defaultLocale) }),
    /* @__PURE__ */ z.jsx("div", { className: "survey-question__options", children: p.map((E) => /* @__PURE__ */ z.jsxs("label", { className: "survey-question__option", children: [
      /* @__PURE__ */ z.jsx(
        "input",
        {
          type: "radio",
          name: `q-${g}`,
          value: E.id,
          checked: j === E.id,
          onChange: () => h(g, E.id)
        }
      ),
      /* @__PURE__ */ z.jsx("span", { children: tt(E.label, f, r.defaultLocale) })
    ] }, E.id)) })
  ] });
}
function Jv({ question: c }) {
  const { locale: f, schema: r, answers: s, setAnswer: h } = le(), g = c.id, N = c.title, D = c.help, x = !!c.required, p = c.options ?? [], j = c.maxSelected, E = s[g] ?? [], C = (G) => {
    if (E.includes(G)) {
      h(g, E.filter((K) => K !== G));
      return;
    }
    j !== void 0 && E.length >= j || h(g, [...E, G]);
  };
  return /* @__PURE__ */ z.jsxs("fieldset", { className: "survey-question survey-question--multi", children: [
    /* @__PURE__ */ z.jsxs("legend", { className: "survey-question__label", children: [
      tt(N, f, r.defaultLocale),
      x && /* @__PURE__ */ z.jsx("span", { "aria-label": "required", className: "survey-question__required", children: " *" })
    ] }),
    D && /* @__PURE__ */ z.jsx("p", { className: "survey-question__help", children: tt(D, f, r.defaultLocale) }),
    /* @__PURE__ */ z.jsx("div", { className: "survey-question__options", children: p.map((G) => {
      const K = E.includes(G.id);
      return /* @__PURE__ */ z.jsxs("label", { className: "survey-question__option", children: [
        /* @__PURE__ */ z.jsx(
          "input",
          {
            type: "checkbox",
            checked: K,
            onChange: () => C(G.id)
          }
        ),
        /* @__PURE__ */ z.jsx("span", { children: tt(G.label, f, r.defaultLocale) })
      ] }, G.id);
    }) })
  ] });
}
function wv({ question: c }) {
  const { locale: f, schema: r, answers: s, setAnswer: h, ui: g } = le(), N = c.id, D = c.title, x = c.help, p = !!c.required, j = c.options ?? [], E = c.placeholder, C = s[N] ?? "", G = E ? tt(E, f, r.defaultLocale) : g.selectPlaceholder;
  return /* @__PURE__ */ z.jsxs("div", { className: "survey-question survey-question--dropdown", children: [
    /* @__PURE__ */ z.jsxs("label", { className: "survey-question__label", htmlFor: `q-${N}`, children: [
      tt(D, f, r.defaultLocale),
      p && /* @__PURE__ */ z.jsx("span", { "aria-label": "required", className: "survey-question__required", children: " *" })
    ] }),
    x && /* @__PURE__ */ z.jsx("p", { className: "survey-question__help", children: tt(x, f, r.defaultLocale) }),
    /* @__PURE__ */ z.jsxs(
      "select",
      {
        id: `q-${N}`,
        className: "survey-question__select",
        value: C,
        required: p,
        onChange: (K) => h(N, K.target.value || null),
        children: [
          /* @__PURE__ */ z.jsx("option", { value: "", children: G }),
          j.map((K) => /* @__PURE__ */ z.jsx("option", { value: K.id, children: tt(K.label, f, r.defaultLocale) }, K.id))
        ]
      }
    )
  ] });
}
function kv({ question: c }) {
  const { locale: f, schema: r, answers: s, setAnswer: h } = le(), g = c.id, N = c.title, D = c.help, x = !!c.required, p = c.minDate, j = c.maxDate, E = s[g] ?? "";
  return /* @__PURE__ */ z.jsxs("div", { className: "survey-question survey-question--date", children: [
    /* @__PURE__ */ z.jsxs("label", { className: "survey-question__label", htmlFor: `q-${g}`, children: [
      tt(N, f, r.defaultLocale),
      x && /* @__PURE__ */ z.jsx("span", { "aria-label": "required", className: "survey-question__required", children: " *" })
    ] }),
    D && /* @__PURE__ */ z.jsx("p", { className: "survey-question__help", children: tt(D, f, r.defaultLocale) }),
    /* @__PURE__ */ z.jsx(
      "input",
      {
        id: `q-${g}`,
        className: "survey-question__input",
        type: "date",
        value: E,
        required: x,
        min: p,
        max: j,
        onChange: (C) => h(g, C.target.value || null)
      }
    )
  ] });
}
function $v({ question: c }) {
  const { locale: f, schema: r, answers: s, setAnswer: h } = le(), g = c.id, N = c.title, D = c.help, x = !!c.required, p = c.minDateTime, j = c.maxDateTime, E = s[g] ?? "", C = (G) => {
    if (!G) return;
    const K = G.match(/^(\d{4}-\d{2}-\d{2}T\d{2}:\d{2})/);
    return (K == null ? void 0 : K[1]) ?? void 0;
  };
  return /* @__PURE__ */ z.jsxs("div", { className: "survey-question survey-question--datetime", children: [
    /* @__PURE__ */ z.jsxs("label", { className: "survey-question__label", htmlFor: `q-${g}`, children: [
      tt(N, f, r.defaultLocale),
      x && /* @__PURE__ */ z.jsx("span", { "aria-label": "required", className: "survey-question__required", children: " *" })
    ] }),
    D && /* @__PURE__ */ z.jsx("p", { className: "survey-question__help", children: tt(D, f, r.defaultLocale) }),
    /* @__PURE__ */ z.jsx(
      "input",
      {
        id: `q-${g}`,
        className: "survey-question__input",
        type: "datetime-local",
        value: C(E) ?? "",
        required: x,
        min: C(p),
        max: C(j),
        onChange: (G) => h(g, G.target.value || null)
      }
    )
  ] });
}
function Wv({ question: c }) {
  const { locale: f, schema: r, answers: s, setAnswer: h } = le(), g = c.id, N = c.title, D = c.help, x = !!c.required, p = c.acceptedTypes, j = F.useRef(null), E = s[g], C = p && p.length > 0 ? p.join(",") : void 0;
  return /* @__PURE__ */ z.jsxs("div", { className: "survey-question survey-question--file", children: [
    /* @__PURE__ */ z.jsxs("label", { className: "survey-question__label", htmlFor: `q-${g}`, children: [
      tt(N, f, r.defaultLocale),
      x && /* @__PURE__ */ z.jsx("span", { "aria-label": "required", className: "survey-question__required", children: " *" })
    ] }),
    D && /* @__PURE__ */ z.jsx("p", { className: "survey-question__help", children: tt(D, f, r.defaultLocale) }),
    /* @__PURE__ */ z.jsx(
      "input",
      {
        ref: j,
        id: `q-${g}`,
        className: "survey-question__file",
        type: "file",
        required: x,
        accept: C,
        onChange: (G) => {
          var K;
          const J = (K = G.target.files) == null ? void 0 : K[0];
          if (!J) {
            h(g, null);
            return;
          }
          h(g, { name: J.name, size: J.size, type: J.type });
        }
      }
    ),
    (E == null ? void 0 : E.name) && /* @__PURE__ */ z.jsxs("p", { className: "survey-question__file-name", children: [
      "Selected: ",
      E.name
    ] })
  ] });
}
const ts = 480, es = 160;
function Fv({ question: c }) {
  const { locale: f, schema: r, answers: s, setAnswer: h, ui: g } = le(), N = c.id, D = c.title, x = c.help, p = !!c.required, j = F.useRef(null), [E, C] = F.useState(!1), [G, K] = F.useState(!!s[N]), J = () => {
    var w;
    return ((w = j.current) == null ? void 0 : w.getContext("2d")) ?? null;
  }, B = (w) => {
    const nt = w.target.getBoundingClientRect();
    return {
      x: (w.clientX - nt.left) / nt.width * ts,
      y: (w.clientY - nt.top) / nt.height * es
    };
  }, Q = F.useCallback(() => {
    var w;
    const nt = (w = j.current) == null ? void 0 : w.toDataURL("image/png");
    nt && h(N, nt);
  }, [N, h]), xt = () => {
    const w = J();
    w && (w.clearRect(0, 0, ts, es), K(!1), h(N, null));
  };
  return F.useEffect(() => {
    const w = J();
    w && (w.lineWidth = 2, w.lineCap = "round", w.strokeStyle = "#111");
  }, []), /* @__PURE__ */ z.jsxs("div", { className: "survey-question survey-question--signature", children: [
    /* @__PURE__ */ z.jsxs("div", { className: "survey-question__label", children: [
      tt(D, f, r.defaultLocale),
      p && /* @__PURE__ */ z.jsx("span", { "aria-label": "required", className: "survey-question__required", children: " *" })
    ] }),
    x && /* @__PURE__ */ z.jsx("p", { className: "survey-question__help", children: tt(x, f, r.defaultLocale) }),
    /* @__PURE__ */ z.jsx(
      "canvas",
      {
        ref: j,
        className: "survey-question__signature-canvas",
        width: ts,
        height: es,
        role: "img",
        "aria-label": "signature pad",
        onPointerDown: (w) => {
          w.target.setPointerCapture(w.pointerId);
          const nt = J();
          if (!nt) return;
          const { x: Vt, y: ut } = B(w);
          nt.beginPath(), nt.moveTo(Vt, ut), C(!0);
        },
        onPointerMove: (w) => {
          if (!E) return;
          const nt = J();
          if (!nt) return;
          const { x: Vt, y: ut } = B(w);
          nt.lineTo(Vt, ut), nt.stroke(), K(!0);
        },
        onPointerUp: () => {
          C(!1), G && Q();
        }
      }
    ),
    /* @__PURE__ */ z.jsx("div", { className: "survey-question__signature-actions", children: /* @__PURE__ */ z.jsx("button", { type: "button", className: "survey-button survey-button--ghost", onClick: xt, children: g.clearSignature }) })
  ] });
}
function Iv({ question: c }) {
  const { locale: f, schema: r, answers: s, setAnswer: h, ui: g } = le(), N = c.id, D = c.title, x = c.help, p = !!c.required, j = c.yesLabel, E = c.noLabel, C = s[N], G = j ? tt(j, f, r.defaultLocale) : g.yes, K = E ? tt(E, f, r.defaultLocale) : g.no;
  return /* @__PURE__ */ z.jsxs("fieldset", { className: "survey-question survey-question--yesno", children: [
    /* @__PURE__ */ z.jsxs("legend", { className: "survey-question__label", children: [
      tt(D, f, r.defaultLocale),
      p && /* @__PURE__ */ z.jsx("span", { "aria-label": "required", className: "survey-question__required", children: " *" })
    ] }),
    x && /* @__PURE__ */ z.jsx("p", { className: "survey-question__help", children: tt(x, f, r.defaultLocale) }),
    /* @__PURE__ */ z.jsxs("div", { className: "survey-question__yesno", role: "radiogroup", children: [
      /* @__PURE__ */ z.jsx(
        "button",
        {
          type: "button",
          role: "radio",
          "aria-checked": C === !0,
          className: "survey-question__yesno-button" + (C === !0 ? " survey-question__yesno-button--selected" : ""),
          onClick: () => h(N, !0),
          children: G
        }
      ),
      /* @__PURE__ */ z.jsx(
        "button",
        {
          type: "button",
          role: "radio",
          "aria-checked": C === !1,
          className: "survey-question__yesno-button" + (C === !1 ? " survey-question__yesno-button--selected" : ""),
          onClick: () => h(N, !1),
          children: K
        }
      )
    ] })
  ] });
}
function Pv(c, f) {
  switch (c.code) {
    case "minLength":
      return ma(f.minLengthError, c.params);
    case "maxLength":
      return ma(f.maxLengthError, c.params);
    case "pattern":
      return f.patternError;
    case "min":
      return ma(f.minError, c.params);
    case "max":
      return ma(f.maxError, c.params);
    case "range":
      return ma(f.rangeError, c.params);
    case "minSelected":
      return ma(f.minSelectedError, c.params);
    case "maxSelected":
      return ma(f.maxSelectedError, c.params);
    default:
      return f.invalidAnswerError;
  }
}
const t0 = {
  text: Lv,
  paragraph: Xv,
  number: Zv,
  rating: Vv,
  nps: Gv,
  singleChoice: Kv,
  multiChoice: Jv,
  dropdown: wv,
  date: kv,
  dateTime: $v,
  file: Wv,
  signature: Fv,
  yesNo: Iv,
  navigationList: Qv
};
function e0(c, f, r) {
  const s = c.screens.find((h) => h.id === f);
  return !s || (s.questions ?? []).length > 0 ? !1 : cn(c, f, r).kind === "end";
}
function l0({
  schema: c,
  onSubmit: f,
  initialAnswers: r,
  locale: s,
  onScreenChange: h,
  onCompleted: g,
  registry: N,
  submissionMeta: D,
  uiLocales: x,
  resumeKey: p,
  storage: j,
  emitHostMessages: E,
  hostMessageOrigin: C,
  hostMessageTarget: G,
  activeScreenId: K
}) {
  var J, B;
  const Q = s ?? c.defaultLocale ?? "en", xt = N ?? t0, w = F.useMemo(
    () => Mv(Q, c.defaultLocale, x),
    [Q, c.defaultLocale, x]
  ), nt = j ?? (typeof globalThis < "u" ? globalThis.localStorage : void 0), Vt = F.useMemo(() => {
    var Z;
    if (!p || !nt) return null;
    const St = jv(nt, p);
    return St ? St.currentScreenId === null || c.screens.some((Lt) => Lt.id === St.currentScreenId) ? St : { ...St, currentScreenId: ((Z = c.screens[0]) == null ? void 0 : Z.id) ?? null } : null;
  }, []), [ut, it] = F.useState(() => ({
    ...r ?? {},
    ...(Vt == null ? void 0 : Vt.answers) ?? {}
  })), [I, ae] = F.useState(
    () => {
      var Z;
      return (Vt == null ? void 0 : Vt.currentScreenId) ?? ((Z = c.screens[0]) == null ? void 0 : Z.id) ?? null;
    }
  );
  F.useEffect(() => {
    if (c.screens.length === 0) {
      I !== null && ae(null);
      return;
    }
    I !== null && c.screens.some((Z) => Z.id === I) || ae(c.screens[0].id);
  }, [c, I]);
  const [Be, he] = F.useState(!1), [Yt, Ze] = F.useState(null), [Ye, ue] = F.useState(/* @__PURE__ */ new Set()), [q, H] = F.useState(/* @__PURE__ */ new Set()), [V, gt] = F.useState(!1), pt = F.useRef(void 0);
  F.useEffect(() => {
    K !== void 0 && pt.current !== K && (pt.current = K, !(K === null || V) && c.screens.some((Z) => Z.id === K) && (ue(/* @__PURE__ */ new Set()), ae(K)));
  }, [K, c, V]);
  const m = F.useRef((/* @__PURE__ */ new Date()).toISOString()), O = F.useRef(null);
  if (O.current === null) {
    const Z = {};
    G !== void 0 && (Z.target = G), C !== void 0 && (Z.targetOrigin = C), E !== void 0 && (Z.enabled = E), O.current = Cv(Z);
  }
  const R = F.useMemo(
    () => I ? c.screens.find((Z) => Z.id === I) ?? null : null,
    [c, I]
  );
  F.useEffect(() => {
    var Z;
    h == null || h(I), (Z = O.current) == null || Z.screenChanged(I);
  }, [I, h]);
  const Y = F.useRef(!1);
  F.useEffect(() => {
    var Z;
    Y.current || !I || (Y.current = !0, (Z = O.current) == null || Z.loaded());
  }, [I]), F.useEffect(() => {
    !p || !nt || V || Rv(nt, p, {
      answers: ut,
      currentScreenId: I,
      schemaVersion: c.version
    });
  }, [ut, I, p, nt, V, c.version]), F.useEffect(() => {
    V && p && nt && Hv(nt, p);
  }, [V, p, nt]), F.useEffect(() => {
    var Z;
    Yt && ((Z = O.current) == null || Z.error(Yt));
  }, [Yt]);
  const W = F.useCallback((Z, St) => {
    it((Lt) => ({ ...Lt, [Z]: St }));
  }, []), et = F.useCallback(
    (Z) => {
      Z !== null && (ue(/* @__PURE__ */ new Set()), H(/* @__PURE__ */ new Set()), ae(Z));
    },
    []
  ), dt = F.useCallback(
    (Z) => {
      if (!Z.required) return !1;
      const St = ut[Z.id];
      return !!(St == null || typeof St == "string" && St.trim() === "" || Array.isArray(St) && St.length === 0);
    },
    [ut]
  ), Ut = F.useCallback(async () => {
    var Z;
    he(!0), Ze(null);
    try {
      await f({
        schemaVersion: c.version ?? 0,
        answers: ut,
        meta: {
          startedAt: (D == null ? void 0 : D.startedAt) ?? m.current,
          completedAt: (D == null ? void 0 : D.completedAt) ?? (/* @__PURE__ */ new Date()).toISOString(),
          ...D ?? {}
        }
      }), gt(!0), g == null || g(I), (Z = O.current) == null || Z.completed({ screenId: I, answers: ut });
    } catch (St) {
      Ze(St.message ?? String(St));
    } finally {
      he(!1);
    }
  }, [c.version, ut, D, f, g, I]), Dt = F.useCallback(() => {
    if (!I) return;
    const Z = c.screens.find((te) => te.id === I), St = ((Z == null ? void 0 : Z.questions) ?? []).filter(dt).map((te) => te.id);
    if (St.length > 0) {
      ue(new Set(St));
      return;
    }
    const Lt = _v(Z == null ? void 0 : Z.questions, ut);
    if (Lt.length > 0) {
      H(new Set(Lt.map((te) => te.questionId)));
      return;
    }
    const bt = cn(c, I, ut);
    bt.kind === "end" ? Ut() : et(bt.screenId);
  }, [c, I, ut, dt, et, Ut]), bl = F.useRef(null);
  F.useEffect(() => {
    V || Be || !I || !R || bl.current === I || !(!R.questions || R.questions.length === 0) || cn(c, I, ut).kind === "end" && (bl.current = I, Ut());
  }, [I, R, V, Be, c, ut, Ut]);
  const Ve = F.useRef(null);
  F.useEffect(() => {
    const Z = Ve.current;
    if (!Z || typeof ResizeObserver > "u") return;
    const St = new ResizeObserver((Lt) => {
      var bt;
      const te = Lt[0];
      te && ((bt = O.current) == null || bt.resize(Math.ceil(te.contentRect.height)));
    });
    return St.observe(Z), () => St.disconnect();
  }, []), F.useEffect(() => {
    const Z = Ve.current;
    if (!Z) return;
    const St = (Lt) => {
      const bt = Lt.detail;
      if (!bt || !I) return;
      W(bt.questionId, bt.option.id);
      const te = { ...ut, [bt.questionId]: bt.option.id }, ou = Sv(
        bt.option,
        c,
        I,
        te
      );
      ou.kind === "end" ? Ut() : et(ou.screenId);
    };
    return Z.addEventListener("survey:navigationListSelect", St), () => Z.removeEventListener("survey:navigationListSelect", St);
  }, [ut, I, c, W, et, Ut]);
  const cu = F.useMemo(
    () => ({
      schema: c,
      locale: Q,
      direction: w.direction,
      ui: w.strings,
      answers: ut,
      setAnswer: W
    }),
    [c, Q, w, ut, W]
  ), ga = F.useMemo(() => xv(c.branding), [c.branding]), Ke = (J = c.branding) != null && J.logoUrl ? /* @__PURE__ */ z.jsx("div", { className: "survey-brand", children: /* @__PURE__ */ z.jsx(
    "img",
    {
      className: "survey-brand__logo",
      src: c.branding.logoUrl,
      alt: "",
      onError: (Z) => {
        Z.currentTarget.parentElement.style.display = "none";
      }
    }
  ) }) : null;
  if (V)
    return /* @__PURE__ */ z.jsxs(
      "div",
      {
        ref: Ve,
        className: "survey-root survey-root--done",
        dir: w.direction,
        lang: Q,
        style: ga,
        children: [
          Ke,
          /* @__PURE__ */ z.jsxs("div", { className: "survey-screen", children: [
            /* @__PURE__ */ z.jsx("h2", { className: "survey-screen__title", children: R != null && R.title ? tt(R.title, Q, c.defaultLocale) : w.strings.thankYou }),
            (R == null ? void 0 : R.description) && /* @__PURE__ */ z.jsx("p", { className: "survey-screen__description", children: tt(R.description, Q, c.defaultLocale) })
          ] })
        ]
      }
    );
  if (!R)
    return /* @__PURE__ */ z.jsx("div", { ref: Ve, className: "survey-root", dir: w.direction, lang: Q, style: ga, children: /* @__PURE__ */ z.jsx("div", { className: "survey-screen", children: /* @__PURE__ */ z.jsx("em", { children: w.strings.noScreens }) }) });
  const Ie = R.questions ?? [], fu = Ie.length > 0 && ((B = Ie[Ie.length - 1]) == null ? void 0 : B.type) === "navigationList", Ni = Ie.length === 0 && !R.nextScreen, su = !fu && !Ni, Sl = su && I !== null ? cn(c, I, ut) : null, ru = Sl !== null && (Sl.kind === "end" || Sl.kind === "screen" && e0(c, Sl.screenId, ut));
  return /* @__PURE__ */ z.jsx(qv, { value: cu, children: /* @__PURE__ */ z.jsxs("div", { ref: Ve, className: "survey-root", dir: w.direction, lang: Q, style: ga, children: [
    Ke,
    /* @__PURE__ */ z.jsxs("div", { className: "survey-screen", children: [
      R.title && /* @__PURE__ */ z.jsx("h2", { className: "survey-screen__title", children: tt(R.title, Q, c.defaultLocale) }),
      R.description && /* @__PURE__ */ z.jsx("p", { className: "survey-screen__description", children: tt(R.description, Q, c.defaultLocale) }),
      /* @__PURE__ */ z.jsx("div", { className: "survey-screen__questions", children: Ie.map((Z, St) => {
        const Lt = Z.id, bt = Lt !== void 0 && Ye.has(Lt) && dt(Z), te = !bt && Lt !== void 0 && q.has(Lt) && ut[Lt] != null ? _y(Z, ut[Lt])[0] ?? null : null;
        return /* @__PURE__ */ z.jsxs("div", { className: bt || te !== null ? "survey-question-slot survey-question-slot--invalid" : "survey-question-slot", children: [
          /* @__PURE__ */ z.jsx(Yv, { question: Z, registry: xt }),
          bt && /* @__PURE__ */ z.jsx("p", { className: "survey-question__required-error", role: "alert", children: w.strings.requiredError }),
          te && /* @__PURE__ */ z.jsx("p", { className: "survey-question__required-error", role: "alert", children: Pv(te, w.strings) })
        ] }, Lt ?? St);
      }) }),
      su && /* @__PURE__ */ z.jsx("div", { className: "survey-screen__actions", children: /* @__PURE__ */ z.jsx(
        "button",
        {
          type: "button",
          className: "survey-button survey-button--primary",
          disabled: Be,
          onClick: Dt,
          children: Be ? w.strings.submitting : ru ? w.strings.submit : w.strings.next
        }
      ) }),
      Yt && /* @__PURE__ */ z.jsxs("p", { className: "survey-screen__error", role: "alert", children: [
        w.strings.couldNotSubmit,
        " ",
        Yt
      ] })
    ] })
  ] }) });
}
const a0 = ".survey-root{--survey-primary: #2563eb;--survey-primary-hover: #1e40af;--survey-primary-contrast: #ffffff;--survey-accent: #f5b60c;font-family:system-ui,-apple-system,Segoe UI,Roboto,sans-serif;color:#111;max-width:640px;margin:0 auto;padding:32px 16px}.survey-brand{display:flex;margin-bottom:20px}.survey-brand__logo{height:28px;width:auto}.survey-screen{display:flex;flex-direction:column;gap:24px}.survey-screen__title{font-size:1.5rem;font-weight:600;margin:0}.survey-screen__description{color:#555;margin:0}.survey-screen__questions{display:flex;flex-direction:column;gap:24px}.survey-screen__actions{display:flex;justify-content:flex-end}.survey-screen__error{color:#b42318;background:#fef3f2;border:1px solid #fecdca;padding:12px 14px;border-radius:8px;margin:0}.survey-question-slot--invalid{border-inline-start:3px solid #b42318;padding-inline-start:10px}.survey-question__required-error{color:#b42318;font-size:.9rem;margin:4px 0 0}.survey-question{display:flex;flex-direction:column;gap:8px}.survey-question__label{font-weight:600;display:block}.survey-question__required{color:#b42318}.survey-question__help{margin:0;color:#666;font-size:.9rem}.survey-question__input{padding:10px 12px;border:1px solid #d0d5dd;border-radius:8px;font:inherit}.survey-question__input:focus-visible{outline:2px solid var(--survey-primary);outline-offset:1px;border-color:var(--survey-primary)}.survey-question--nps{border:none;padding:0;margin:0;display:flex;flex-direction:column;gap:8px}.survey-question__nps-scale{display:flex;gap:6px;flex-wrap:wrap}.survey-question__nps-step{min-width:40px;min-height:40px;padding:8px;border:1px solid #d0d5dd;border-radius:8px;background:#fff;font-weight:500;cursor:pointer}.survey-question__nps-step:hover{background:#f5f7fa}.survey-question__nps-step--selected{background:var(--survey-primary);border-color:var(--survey-primary);color:var(--survey-primary-contrast)}.survey-question__nps-labels{display:flex;justify-content:space-between;color:#555;font-size:.85rem}.survey-question--navlist{gap:12px}.survey-navlist{list-style:none;padding:0;margin:0;display:flex;flex-direction:column;gap:8px}.survey-navlist__row{margin:0}.survey-navlist__button{width:100%;display:flex;align-items:center;justify-content:space-between;padding:14px 16px;border:1px solid #d0d5dd;border-radius:10px;background:#fff;cursor:pointer;font:inherit;text-align:start}.survey-navlist__button:hover{background:#f5f7fa;border-color:var(--survey-primary)}.survey-navlist__chevron{font-size:1.5rem;color:#667085}.survey-root[dir=rtl] .survey-navlist__chevron{transform:scaleX(-1)}.survey-navlist__label{font-weight:500}.survey-question__textarea{padding:10px 12px;border:1px solid #d0d5dd;border-radius:8px;font:inherit;resize:vertical;min-height:96px}.survey-question__textarea:focus-visible{outline:2px solid var(--survey-primary);outline-offset:1px;border-color:var(--survey-primary)}.survey-question__number-wrap{display:flex;align-items:center;gap:8px}.survey-question__number-wrap .survey-question__input{flex:1}.survey-question__unit{color:#555;font-size:.9rem}.survey-question__rating-scale{display:flex;gap:4px}.survey-question__rating-star{background:transparent;border:none;cursor:pointer;font-size:1.8rem;line-height:1;color:#d0d5dd;padding:4px}.survey-question__rating-star:hover,.survey-question__rating-star--selected{color:var(--survey-accent)}.survey-question__options{display:flex;flex-direction:column;gap:8px}.survey-question__option{display:flex;align-items:center;gap:8px;padding:8px 12px;border:1px solid #d0d5dd;border-radius:8px;cursor:pointer}.survey-question__option:hover{background:#f5f7fa;border-color:var(--survey-primary)}.survey-question__option input{margin:0}.survey-question__select{padding:10px 12px;border:1px solid #d0d5dd;border-radius:8px;font:inherit;background:#fff}.survey-question__yesno{display:flex;gap:12px}.survey-question__yesno-button{flex:1;padding:14px 16px;border:1px solid #d0d5dd;border-radius:10px;background:#fff;font:inherit;font-weight:500;cursor:pointer}.survey-question__yesno-button:hover{background:#f5f7fa;border-color:var(--survey-primary)}.survey-question__yesno-button--selected{background:var(--survey-primary);border-color:var(--survey-primary);color:var(--survey-primary-contrast)}.survey-question__file{font:inherit}.survey-question__file-name{color:#555;font-size:.9rem;margin:0}.survey-question__signature-canvas{width:100%;max-width:480px;height:auto;aspect-ratio:3 / 1;border:1px dashed #d0d5dd;border-radius:8px;background:#fff;touch-action:none}.survey-question__signature-actions{display:flex;justify-content:flex-start;gap:8px}.survey-button{padding:10px 20px;border-radius:8px;border:1px solid transparent;cursor:pointer;font:inherit;font-weight:600}.survey-button--primary{background:var(--survey-primary);color:var(--survey-primary-contrast)}.survey-button--primary:hover{background:var(--survey-primary-hover)}.survey-button--ghost{background:#fff;color:#555;border-color:#d0d5dd}.survey-button--ghost:hover{background:#f5f7fa}.survey-button:disabled{opacity:.5;cursor:not-allowed}.survey-question__options-status{margin:6px 0;font-size:.9rem;color:var(--survey-muted, #667085)}.survey-question--options-error .survey-question__options-status{color:var(--survey-error, #b42318)}.survey-button--retry{background:transparent;color:var(--survey-primary, #4338ca);border:1px solid currentColor;padding:4px 14px;font-size:.85rem}";
var wl, nu, Xe, kl, iu, ha, fn, Jt, as, Jl, sn, au;
class u0 extends HTMLElement {
  constructor() {
    super();
    We(this, Jt);
    /** Schema-mode setter. Assigning this swaps the element into schema mode and
     *  re-renders with the new schema immediately. */
    We(this, wl, null);
    /** Schema-mode submit handler. In API mode the element manages this itself. */
    We(this, nu, null);
    We(this, Xe, null);
    We(this, kl, null);
    We(this, iu, null);
    We(this, ha, null);
    /** Builder-preview jump target. Assigning a screen id makes the renderer
     *  jump to that screen (answers preserved); the user can navigate freely
     *  afterwards. Mirrors the `active-screen-id` attribute; the property wins
     *  when both are set. */
    We(this, fn, null);
    We(this, sn, !1);
    this.attachShadow({ mode: "open" });
  }
  static get observedAttributes() {
    return ["instance-id", "api-base", "locale", "mode", "active-screen-id"];
  }
  // ─── Lifecycle ───────────────────────────────────────────────────────────
  connectedCallback() {
    if (this.shadowRoot) {
      if (!this.shadowRoot.querySelector("style[data-shift-survey]")) {
        const r = document.createElement("style");
        r.setAttribute("data-shift-survey", ""), r.textContent = a0, this.shadowRoot.appendChild(r);
      }
      Rt(this, kl) || (Re(this, kl, document.createElement("div")), Rt(this, kl).className = "shift-survey-mount", this.shadowRoot.appendChild(Rt(this, kl))), Rt(this, Xe) || Re(this, Xe, Ph.createRoot(Rt(this, kl))), ie(this, Jt, Jl).call(this), ie(this, Jt, as).call(this);
    }
  }
  disconnectedCallback() {
    queueMicrotask(() => {
      var r;
      if (!(this.isConnected || typeof window > "u")) {
        try {
          (r = Rt(this, Xe)) == null || r.unmount();
        } catch {
        }
        Re(this, Xe, null);
      }
    });
  }
  attributeChangedCallback(r, s, h) {
    s !== h && ((r === "instance-id" || r === "api-base") && (Re(this, iu, null), Re(this, ha, null), ie(this, Jt, as).call(this)), ie(this, Jt, Jl).call(this));
  }
  // ─── Properties ──────────────────────────────────────────────────────────
  get schema() {
    return Rt(this, wl);
  }
  set schema(r) {
    Re(this, wl, r), ie(this, Jt, Jl).call(this);
  }
  get onSubmit() {
    return Rt(this, nu);
  }
  set onSubmit(r) {
    Re(this, nu, r), ie(this, Jt, Jl).call(this);
  }
  get activeScreenId() {
    return Rt(this, fn) ?? this.getAttribute("active-screen-id");
  }
  set activeScreenId(r) {
    Re(this, fn, r), ie(this, Jt, Jl).call(this);
  }
}
wl = new WeakMap(), nu = new WeakMap(), Xe = new WeakMap(), kl = new WeakMap(), iu = new WeakMap(), ha = new WeakMap(), fn = new WeakMap(), Jt = new WeakSet(), // ─── Internals ───────────────────────────────────────────────────────────
as = function() {
  if (Rt(this, wl)) return;
  const r = this.getAttribute("instance-id");
  if (!r) return;
  const s = this.getAttribute("api-base");
  if (!s) return;
  new py({ baseUrl: s }).fetchSchema(r).then((g) => {
    Re(this, iu, g), ie(this, Jt, Jl).call(this);
  }).catch((g) => {
    Re(this, ha, g), ie(this, Jt, au).call(this, "survey:error", { message: g.message }), ie(this, Jt, Jl).call(this);
  });
}, Jl = function() {
  if (!Rt(this, Xe)) return;
  const r = this.getAttribute("api-base"), s = this.getAttribute("instance-id"), h = this.getAttribute("locale") ?? void 0, g = this.getAttribute("mode") === "agent", N = Rt(this, wl) ?? Rt(this, iu);
  if (Rt(this, ha) && !N) {
    Rt(this, Xe).render(
      F.createElement(
        "div",
        { className: "shift-survey-error", role: "alert" },
        Rt(this, ha).message
      )
    );
    return;
  }
  if (!N) {
    Rt(this, Xe).render(
      F.createElement("div", { className: "shift-survey-loading" }, "Loading…")
    );
    return;
  }
  const D = Rt(this, wl) ? Rt(this, nu) ?? ((p) => {
    ie(this, Jt, au).call(this, "survey:completed", { ...p });
  }) : async (p) => {
    if (!r || !s)
      throw new Error("shift-survey: API mode requires both instance-id and api-base attributes.");
    await new py({ baseUrl: r }).submitResponse(s, p);
  }, x = this.activeScreenId;
  Rt(this, Xe).render(
    F.createElement(l0, {
      schema: N,
      onSubmit: D,
      ...h ? { locale: h } : {},
      ...x ? { activeScreenId: x } : {},
      // Let the element be the resume key in API mode so two surveys on the
      // same host page don't clobber each other.
      ...s ? { resumeKey: s } : {},
      ...g ? { submissionMeta: { mode: "agent" } } : {},
      // CustomEvents are the web-component's channel; postMessage stays opt-in
      // via iframe auto-detect on the enclosing page (unchanged).
      onScreenChange: (p) => ie(this, Jt, au).call(this, "survey:screen-changed", { screenId: p }),
      onCompleted: (p) => ie(this, Jt, au).call(this, "survey:completed", { screenId: p })
    })
  ), Rt(this, sn) || (Re(this, sn, !0), ie(this, Jt, au).call(this, "survey:loaded", {}));
}, sn = new WeakMap(), au = function(r, s) {
  this.dispatchEvent(
    new CustomEvent(r, { detail: s, bubbles: !0, composed: !0 })
  );
};
function n0(c = "shift-survey") {
  typeof window > "u" || typeof customElements > "u" || customElements.get(c) || customElements.define(c, u0);
}
n0();
export {
  u0 as ShiftSurveyElement,
  n0 as registerShiftSurvey
};
//# sourceMappingURL=index.js.map
