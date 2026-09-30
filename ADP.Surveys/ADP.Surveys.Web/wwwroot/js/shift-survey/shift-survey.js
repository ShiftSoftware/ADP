var zb = Object.defineProperty;
var h0 = (n) => {
  throw TypeError(n);
};
var Tb = (n, i, s) => i in n ? zb(n, i, { enumerable: !0, configurable: !0, writable: !0, value: s }) : n[i] = s;
var La = (n, i, s) => Tb(n, typeof i != "symbol" ? i + "" : i, s), _c = (n, i, s) => i.has(n) || h0("Cannot " + s);
var Ue = (n, i, s) => (_c(n, i, "read from private field"), s ? s.call(n) : i.get(n)), na = (n, i, s) => i.has(n) ? h0("Cannot add the same private member more than once") : i instanceof WeakSet ? i.add(n) : i.set(n, s), Lt = (n, i, s, r) => (_c(n, i, "write to private field"), r ? r.call(n, s) : i.set(n, s), s), pt = (n, i, s) => (_c(n, i, "access private method"), s);
typeof window < "u" && (window.adpWebComponentsFonts ?? (window.adpWebComponentsFonts = !1));
var xc = { exports: {} }, ue = {};
/**
 * @license React
 * react.production.js
 *
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 */
var m0;
function Eb() {
  if (m0) return ue;
  m0 = 1;
  var n = Symbol.for("react.transitional.element"), i = Symbol.for("react.portal"), s = Symbol.for("react.fragment"), r = Symbol.for("react.strict_mode"), c = Symbol.for("react.profiler"), f = Symbol.for("react.consumer"), d = Symbol.for("react.context"), h = Symbol.for("react.forward_ref"), g = Symbol.for("react.suspense"), v = Symbol.for("react.memo"), y = Symbol.for("react.lazy"), m = Symbol.for("react.activity"), _ = Symbol.iterator;
  function x(k) {
    return k === null || typeof k != "object" ? null : (k = _ && k[_] || k["@@iterator"], typeof k == "function" ? k : null);
  }
  var T = {
    isMounted: function() {
      return !1;
    },
    enqueueForceUpdate: function() {
    },
    enqueueReplaceState: function() {
    },
    enqueueSetState: function() {
    }
  }, A = Object.assign, z = {};
  function q(k, Y, K) {
    this.props = k, this.context = Y, this.refs = z, this.updater = K || T;
  }
  q.prototype.isReactComponent = {}, q.prototype.setState = function(k, Y) {
    if (typeof k != "object" && typeof k != "function" && k != null)
      throw Error(
        "takes an object of state variables to update or a function which returns an object of state variables."
      );
    this.updater.enqueueSetState(this, k, Y, "setState");
  }, q.prototype.forceUpdate = function(k) {
    this.updater.enqueueForceUpdate(this, k, "forceUpdate");
  };
  function $() {
  }
  $.prototype = q.prototype;
  function j(k, Y, K) {
    this.props = k, this.context = Y, this.refs = z, this.updater = K || T;
  }
  var L = j.prototype = new $();
  L.constructor = j, A(L, q.prototype), L.isPureReactComponent = !0;
  var V = Array.isArray;
  function J() {
  }
  var Z = { H: null, A: null, T: null, S: null }, F = Object.prototype.hasOwnProperty;
  function Te(k, Y, K) {
    var I = K.ref;
    return {
      $$typeof: n,
      type: k,
      key: Y,
      ref: I !== void 0 ? I : null,
      props: K
    };
  }
  function Se(k, Y) {
    return Te(k.type, Y, k.props);
  }
  function oe(k) {
    return typeof k == "object" && k !== null && k.$$typeof === n;
  }
  function Ye(k) {
    var Y = { "=": "=0", ":": "=2" };
    return "$" + k.replace(/[=:]/g, function(K) {
      return Y[K];
    });
  }
  var ba = /\/+/g;
  function It(k, Y) {
    return typeof k == "object" && k !== null && k.key != null ? Ye("" + k.key) : Y.toString(36);
  }
  function Ke(k) {
    switch (k.status) {
      case "fulfilled":
        return k.value;
      case "rejected":
        throw k.reason;
      default:
        switch (typeof k.status == "string" ? k.then(J, J) : (k.status = "pending", k.then(
          function(Y) {
            k.status === "pending" && (k.status = "fulfilled", k.value = Y);
          },
          function(Y) {
            k.status === "pending" && (k.status = "rejected", k.reason = Y);
          }
        )), k.status) {
          case "fulfilled":
            return k.value;
          case "rejected":
            throw k.reason;
        }
    }
    throw k;
  }
  function R(k, Y, K, I, se) {
    var fe = typeof k;
    (fe === "undefined" || fe === "boolean") && (k = null);
    var xe = !1;
    if (k === null) xe = !0;
    else
      switch (fe) {
        case "bigint":
        case "string":
        case "number":
          xe = !0;
          break;
        case "object":
          switch (k.$$typeof) {
            case n:
            case i:
              xe = !0;
              break;
            case y:
              return xe = k._init, R(
                xe(k._payload),
                Y,
                K,
                I,
                se
              );
          }
      }
    if (xe)
      return se = se(k), xe = I === "" ? "." + It(k, 0) : I, V(se) ? (K = "", xe != null && (K = xe.replace(ba, "$&/") + "/"), R(se, Y, K, "", function(ra) {
        return ra;
      })) : se != null && (oe(se) && (se = Se(
        se,
        K + (se.key == null || k && k.key === se.key ? "" : ("" + se.key).replace(
          ba,
          "$&/"
        ) + "/") + xe
      )), Y.push(se)), 1;
    xe = 0;
    var Je = I === "" ? "." : I + ":";
    if (V(k))
      for (var He = 0; He < k.length; He++)
        I = k[He], fe = Je + It(I, He), xe += R(
          I,
          Y,
          K,
          fe,
          se
        );
    else if (He = x(k), typeof He == "function")
      for (k = He.call(k), He = 0; !(I = k.next()).done; )
        I = I.value, fe = Je + It(I, He++), xe += R(
          I,
          Y,
          K,
          fe,
          se
        );
    else if (fe === "object") {
      if (typeof k.then == "function")
        return R(
          Ke(k),
          Y,
          K,
          I,
          se
        );
      throw Y = String(k), Error(
        "Objects are not valid as a React child (found: " + (Y === "[object Object]" ? "object with keys {" + Object.keys(k).join(", ") + "}" : Y) + "). If you meant to render a collection of children, use an array instead."
      );
    }
    return xe;
  }
  function Q(k, Y, K) {
    if (k == null) return k;
    var I = [], se = 0;
    return R(k, I, "", "", function(fe) {
      return Y.call(K, fe, se++);
    }), I;
  }
  function le(k) {
    if (k._status === -1) {
      var Y = k._result;
      Y = Y(), Y.then(
        function(K) {
          (k._status === 0 || k._status === -1) && (k._status = 1, k._result = K);
        },
        function(K) {
          (k._status === 0 || k._status === -1) && (k._status = 2, k._result = K);
        }
      ), k._status === -1 && (k._status = 0, k._result = Y);
    }
    if (k._status === 1) return k._result.default;
    throw k._result;
  }
  var W = typeof reportError == "function" ? reportError : function(k) {
    if (typeof window == "object" && typeof window.ErrorEvent == "function") {
      var Y = new window.ErrorEvent("error", {
        bubbles: !0,
        cancelable: !0,
        message: typeof k == "object" && k !== null && typeof k.message == "string" ? String(k.message) : String(k),
        error: k
      });
      if (!window.dispatchEvent(Y)) return;
    } else if (typeof process == "object" && typeof process.emit == "function") {
      process.emit("uncaughtException", k);
      return;
    }
    console.error(k);
  }, ye = {
    map: Q,
    forEach: function(k, Y, K) {
      Q(
        k,
        function() {
          Y.apply(this, arguments);
        },
        K
      );
    },
    count: function(k) {
      var Y = 0;
      return Q(k, function() {
        Y++;
      }), Y;
    },
    toArray: function(k) {
      return Q(k, function(Y) {
        return Y;
      }) || [];
    },
    only: function(k) {
      if (!oe(k))
        throw Error(
          "React.Children.only expected to receive a single React element child."
        );
      return k;
    }
  };
  return ue.Activity = m, ue.Children = ye, ue.Component = q, ue.Fragment = s, ue.Profiler = c, ue.PureComponent = j, ue.StrictMode = r, ue.Suspense = g, ue.__CLIENT_INTERNALS_DO_NOT_USE_OR_WARN_USERS_THEY_CANNOT_UPGRADE = Z, ue.__COMPILER_RUNTIME = {
    __proto__: null,
    c: function(k) {
      return Z.H.useMemoCache(k);
    }
  }, ue.cache = function(k) {
    return function() {
      return k.apply(null, arguments);
    };
  }, ue.cacheSignal = function() {
    return null;
  }, ue.cloneElement = function(k, Y, K) {
    if (k == null)
      throw Error(
        "The argument must be a React element, but you passed " + k + "."
      );
    var I = A({}, k.props), se = k.key;
    if (Y != null)
      for (fe in Y.key !== void 0 && (se = "" + Y.key), Y)
        !F.call(Y, fe) || fe === "key" || fe === "__self" || fe === "__source" || fe === "ref" && Y.ref === void 0 || (I[fe] = Y[fe]);
    var fe = arguments.length - 2;
    if (fe === 1) I.children = K;
    else if (1 < fe) {
      for (var xe = Array(fe), Je = 0; Je < fe; Je++)
        xe[Je] = arguments[Je + 2];
      I.children = xe;
    }
    return Te(k.type, se, I);
  }, ue.createContext = function(k) {
    return k = {
      $$typeof: d,
      _currentValue: k,
      _currentValue2: k,
      _threadCount: 0,
      Provider: null,
      Consumer: null
    }, k.Provider = k, k.Consumer = {
      $$typeof: f,
      _context: k
    }, k;
  }, ue.createElement = function(k, Y, K) {
    var I, se = {}, fe = null;
    if (Y != null)
      for (I in Y.key !== void 0 && (fe = "" + Y.key), Y)
        F.call(Y, I) && I !== "key" && I !== "__self" && I !== "__source" && (se[I] = Y[I]);
    var xe = arguments.length - 2;
    if (xe === 1) se.children = K;
    else if (1 < xe) {
      for (var Je = Array(xe), He = 0; He < xe; He++)
        Je[He] = arguments[He + 2];
      se.children = Je;
    }
    if (k && k.defaultProps)
      for (I in xe = k.defaultProps, xe)
        se[I] === void 0 && (se[I] = xe[I]);
    return Te(k, fe, se);
  }, ue.createRef = function() {
    return { current: null };
  }, ue.forwardRef = function(k) {
    return { $$typeof: h, render: k };
  }, ue.isValidElement = oe, ue.lazy = function(k) {
    return {
      $$typeof: y,
      _payload: { _status: -1, _result: k },
      _init: le
    };
  }, ue.memo = function(k, Y) {
    return {
      $$typeof: v,
      type: k,
      compare: Y === void 0 ? null : Y
    };
  }, ue.startTransition = function(k) {
    var Y = Z.T, K = {};
    Z.T = K;
    try {
      var I = k(), se = Z.S;
      se !== null && se(K, I), typeof I == "object" && I !== null && typeof I.then == "function" && I.then(J, W);
    } catch (fe) {
      W(fe);
    } finally {
      Y !== null && K.types !== null && (Y.types = K.types), Z.T = Y;
    }
  }, ue.unstable_useCacheRefresh = function() {
    return Z.H.useCacheRefresh();
  }, ue.use = function(k) {
    return Z.H.use(k);
  }, ue.useActionState = function(k, Y, K) {
    return Z.H.useActionState(k, Y, K);
  }, ue.useCallback = function(k, Y) {
    return Z.H.useCallback(k, Y);
  }, ue.useContext = function(k) {
    return Z.H.useContext(k);
  }, ue.useDebugValue = function() {
  }, ue.useDeferredValue = function(k, Y) {
    return Z.H.useDeferredValue(k, Y);
  }, ue.useEffect = function(k, Y) {
    return Z.H.useEffect(k, Y);
  }, ue.useEffectEvent = function(k) {
    return Z.H.useEffectEvent(k);
  }, ue.useId = function() {
    return Z.H.useId();
  }, ue.useImperativeHandle = function(k, Y, K) {
    return Z.H.useImperativeHandle(k, Y, K);
  }, ue.useInsertionEffect = function(k, Y) {
    return Z.H.useInsertionEffect(k, Y);
  }, ue.useLayoutEffect = function(k, Y) {
    return Z.H.useLayoutEffect(k, Y);
  }, ue.useMemo = function(k, Y) {
    return Z.H.useMemo(k, Y);
  }, ue.useOptimistic = function(k, Y) {
    return Z.H.useOptimistic(k, Y);
  }, ue.useReducer = function(k, Y, K) {
    return Z.H.useReducer(k, Y, K);
  }, ue.useRef = function(k) {
    return Z.H.useRef(k);
  }, ue.useState = function(k) {
    return Z.H.useState(k);
  }, ue.useSyncExternalStore = function(k, Y, K) {
    return Z.H.useSyncExternalStore(
      k,
      Y,
      K
    );
  }, ue.useTransition = function() {
    return Z.H.useTransition();
  }, ue.version = "19.2.5", ue;
}
var v0;
function lf() {
  return v0 || (v0 = 1, xc.exports = Eb()), xc.exports;
}
var ee = lf(), wc = { exports: {} }, ss = {}, kc = { exports: {} }, Sc = {};
/**
 * @license React
 * scheduler.production.js
 *
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 */
var g0;
function Ab() {
  return g0 || (g0 = 1, (function(n) {
    function i(R, Q) {
      var le = R.length;
      R.push(Q);
      e: for (; 0 < le; ) {
        var W = le - 1 >>> 1, ye = R[W];
        if (0 < c(ye, Q))
          R[W] = Q, R[le] = ye, le = W;
        else break e;
      }
    }
    function s(R) {
      return R.length === 0 ? null : R[0];
    }
    function r(R) {
      if (R.length === 0) return null;
      var Q = R[0], le = R.pop();
      if (le !== Q) {
        R[0] = le;
        e: for (var W = 0, ye = R.length, k = ye >>> 1; W < k; ) {
          var Y = 2 * (W + 1) - 1, K = R[Y], I = Y + 1, se = R[I];
          if (0 > c(K, le))
            I < ye && 0 > c(se, K) ? (R[W] = se, R[I] = le, W = I) : (R[W] = K, R[Y] = le, W = Y);
          else if (I < ye && 0 > c(se, le))
            R[W] = se, R[I] = le, W = I;
          else break e;
        }
      }
      return Q;
    }
    function c(R, Q) {
      var le = R.sortIndex - Q.sortIndex;
      return le !== 0 ? le : R.id - Q.id;
    }
    if (n.unstable_now = void 0, typeof performance == "object" && typeof performance.now == "function") {
      var f = performance;
      n.unstable_now = function() {
        return f.now();
      };
    } else {
      var d = Date, h = d.now();
      n.unstable_now = function() {
        return d.now() - h;
      };
    }
    var g = [], v = [], y = 1, m = null, _ = 3, x = !1, T = !1, A = !1, z = !1, q = typeof setTimeout == "function" ? setTimeout : null, $ = typeof clearTimeout == "function" ? clearTimeout : null, j = typeof setImmediate < "u" ? setImmediate : null;
    function L(R) {
      for (var Q = s(v); Q !== null; ) {
        if (Q.callback === null) r(v);
        else if (Q.startTime <= R)
          r(v), Q.sortIndex = Q.expirationTime, i(g, Q);
        else break;
        Q = s(v);
      }
    }
    function V(R) {
      if (A = !1, L(R), !T)
        if (s(g) !== null)
          T = !0, J || (J = !0, Ye());
        else {
          var Q = s(v);
          Q !== null && Ke(V, Q.startTime - R);
        }
    }
    var J = !1, Z = -1, F = 5, Te = -1;
    function Se() {
      return z ? !0 : !(n.unstable_now() - Te < F);
    }
    function oe() {
      if (z = !1, J) {
        var R = n.unstable_now();
        Te = R;
        var Q = !0;
        try {
          e: {
            T = !1, A && (A = !1, $(Z), Z = -1), x = !0;
            var le = _;
            try {
              t: {
                for (L(R), m = s(g); m !== null && !(m.expirationTime > R && Se()); ) {
                  var W = m.callback;
                  if (typeof W == "function") {
                    m.callback = null, _ = m.priorityLevel;
                    var ye = W(
                      m.expirationTime <= R
                    );
                    if (R = n.unstable_now(), typeof ye == "function") {
                      m.callback = ye, L(R), Q = !0;
                      break t;
                    }
                    m === s(g) && r(g), L(R);
                  } else r(g);
                  m = s(g);
                }
                if (m !== null) Q = !0;
                else {
                  var k = s(v);
                  k !== null && Ke(
                    V,
                    k.startTime - R
                  ), Q = !1;
                }
              }
              break e;
            } finally {
              m = null, _ = le, x = !1;
            }
            Q = void 0;
          }
        } finally {
          Q ? Ye() : J = !1;
        }
      }
    }
    var Ye;
    if (typeof j == "function")
      Ye = function() {
        j(oe);
      };
    else if (typeof MessageChannel < "u") {
      var ba = new MessageChannel(), It = ba.port2;
      ba.port1.onmessage = oe, Ye = function() {
        It.postMessage(null);
      };
    } else
      Ye = function() {
        q(oe, 0);
      };
    function Ke(R, Q) {
      Z = q(function() {
        R(n.unstable_now());
      }, Q);
    }
    n.unstable_IdlePriority = 5, n.unstable_ImmediatePriority = 1, n.unstable_LowPriority = 4, n.unstable_NormalPriority = 3, n.unstable_Profiling = null, n.unstable_UserBlockingPriority = 2, n.unstable_cancelCallback = function(R) {
      R.callback = null;
    }, n.unstable_forceFrameRate = function(R) {
      0 > R || 125 < R ? console.error(
        "forceFrameRate takes a positive int between 0 and 125, forcing frame rates higher than 125 fps is not supported"
      ) : F = 0 < R ? Math.floor(1e3 / R) : 5;
    }, n.unstable_getCurrentPriorityLevel = function() {
      return _;
    }, n.unstable_next = function(R) {
      switch (_) {
        case 1:
        case 2:
        case 3:
          var Q = 3;
          break;
        default:
          Q = _;
      }
      var le = _;
      _ = Q;
      try {
        return R();
      } finally {
        _ = le;
      }
    }, n.unstable_requestPaint = function() {
      z = !0;
    }, n.unstable_runWithPriority = function(R, Q) {
      switch (R) {
        case 1:
        case 2:
        case 3:
        case 4:
        case 5:
          break;
        default:
          R = 3;
      }
      var le = _;
      _ = R;
      try {
        return Q();
      } finally {
        _ = le;
      }
    }, n.unstable_scheduleCallback = function(R, Q, le) {
      var W = n.unstable_now();
      switch (typeof le == "object" && le !== null ? (le = le.delay, le = typeof le == "number" && 0 < le ? W + le : W) : le = W, R) {
        case 1:
          var ye = -1;
          break;
        case 2:
          ye = 250;
          break;
        case 5:
          ye = 1073741823;
          break;
        case 4:
          ye = 1e4;
          break;
        default:
          ye = 5e3;
      }
      return ye = le + ye, R = {
        id: y++,
        callback: Q,
        priorityLevel: R,
        startTime: le,
        expirationTime: ye,
        sortIndex: -1
      }, le > W ? (R.sortIndex = le, i(v, R), s(g) === null && R === s(v) && (A ? ($(Z), Z = -1) : A = !0, Ke(V, le - W))) : (R.sortIndex = ye, i(g, R), T || x || (T = !0, J || (J = !0, Ye()))), R;
    }, n.unstable_shouldYield = Se, n.unstable_wrapCallback = function(R) {
      var Q = _;
      return function() {
        var le = _;
        _ = Q;
        try {
          return R.apply(this, arguments);
        } finally {
          _ = le;
        }
      };
    };
  })(Sc)), Sc;
}
var b0;
function Ob() {
  return b0 || (b0 = 1, kc.exports = Ab()), kc.exports;
}
var zc = { exports: {} }, gt = {};
/**
 * @license React
 * react-dom.production.js
 *
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 */
var y0;
function Db() {
  if (y0) return gt;
  y0 = 1;
  var n = lf();
  function i(g) {
    var v = "https://react.dev/errors/" + g;
    if (1 < arguments.length) {
      v += "?args[]=" + encodeURIComponent(arguments[1]);
      for (var y = 2; y < arguments.length; y++)
        v += "&args[]=" + encodeURIComponent(arguments[y]);
    }
    return "Minified React error #" + g + "; visit " + v + " for the full message or use the non-minified dev environment for full errors and additional helpful warnings.";
  }
  function s() {
  }
  var r = {
    d: {
      f: s,
      r: function() {
        throw Error(i(522));
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
  }, c = Symbol.for("react.portal");
  function f(g, v, y) {
    var m = 3 < arguments.length && arguments[3] !== void 0 ? arguments[3] : null;
    return {
      $$typeof: c,
      key: m == null ? null : "" + m,
      children: g,
      containerInfo: v,
      implementation: y
    };
  }
  var d = n.__CLIENT_INTERNALS_DO_NOT_USE_OR_WARN_USERS_THEY_CANNOT_UPGRADE;
  function h(g, v) {
    if (g === "font") return "";
    if (typeof v == "string")
      return v === "use-credentials" ? v : "";
  }
  return gt.__DOM_INTERNALS_DO_NOT_USE_OR_WARN_USERS_THEY_CANNOT_UPGRADE = r, gt.createPortal = function(g, v) {
    var y = 2 < arguments.length && arguments[2] !== void 0 ? arguments[2] : null;
    if (!v || v.nodeType !== 1 && v.nodeType !== 9 && v.nodeType !== 11)
      throw Error(i(299));
    return f(g, v, null, y);
  }, gt.flushSync = function(g) {
    var v = d.T, y = r.p;
    try {
      if (d.T = null, r.p = 2, g) return g();
    } finally {
      d.T = v, r.p = y, r.d.f();
    }
  }, gt.preconnect = function(g, v) {
    typeof g == "string" && (v ? (v = v.crossOrigin, v = typeof v == "string" ? v === "use-credentials" ? v : "" : void 0) : v = null, r.d.C(g, v));
  }, gt.prefetchDNS = function(g) {
    typeof g == "string" && r.d.D(g);
  }, gt.preinit = function(g, v) {
    if (typeof g == "string" && v && typeof v.as == "string") {
      var y = v.as, m = h(y, v.crossOrigin), _ = typeof v.integrity == "string" ? v.integrity : void 0, x = typeof v.fetchPriority == "string" ? v.fetchPriority : void 0;
      y === "style" ? r.d.S(
        g,
        typeof v.precedence == "string" ? v.precedence : void 0,
        {
          crossOrigin: m,
          integrity: _,
          fetchPriority: x
        }
      ) : y === "script" && r.d.X(g, {
        crossOrigin: m,
        integrity: _,
        fetchPriority: x,
        nonce: typeof v.nonce == "string" ? v.nonce : void 0
      });
    }
  }, gt.preinitModule = function(g, v) {
    if (typeof g == "string")
      if (typeof v == "object" && v !== null) {
        if (v.as == null || v.as === "script") {
          var y = h(
            v.as,
            v.crossOrigin
          );
          r.d.M(g, {
            crossOrigin: y,
            integrity: typeof v.integrity == "string" ? v.integrity : void 0,
            nonce: typeof v.nonce == "string" ? v.nonce : void 0
          });
        }
      } else v == null && r.d.M(g);
  }, gt.preload = function(g, v) {
    if (typeof g == "string" && typeof v == "object" && v !== null && typeof v.as == "string") {
      var y = v.as, m = h(y, v.crossOrigin);
      r.d.L(g, y, {
        crossOrigin: m,
        integrity: typeof v.integrity == "string" ? v.integrity : void 0,
        nonce: typeof v.nonce == "string" ? v.nonce : void 0,
        type: typeof v.type == "string" ? v.type : void 0,
        fetchPriority: typeof v.fetchPriority == "string" ? v.fetchPriority : void 0,
        referrerPolicy: typeof v.referrerPolicy == "string" ? v.referrerPolicy : void 0,
        imageSrcSet: typeof v.imageSrcSet == "string" ? v.imageSrcSet : void 0,
        imageSizes: typeof v.imageSizes == "string" ? v.imageSizes : void 0,
        media: typeof v.media == "string" ? v.media : void 0
      });
    }
  }, gt.preloadModule = function(g, v) {
    if (typeof g == "string")
      if (v) {
        var y = h(v.as, v.crossOrigin);
        r.d.m(g, {
          as: typeof v.as == "string" && v.as !== "script" ? v.as : void 0,
          crossOrigin: y,
          integrity: typeof v.integrity == "string" ? v.integrity : void 0
        });
      } else r.d.m(g);
  }, gt.requestFormReset = function(g) {
    r.d.r(g);
  }, gt.unstable_batchedUpdates = function(g, v) {
    return g(v);
  }, gt.useFormState = function(g, v, y) {
    return d.H.useFormState(g, v, y);
  }, gt.useFormStatus = function() {
    return d.H.useHostTransitionStatus();
  }, gt.version = "19.2.5", gt;
}
var _0;
function Cb() {
  if (_0) return zc.exports;
  _0 = 1;
  function n() {
    if (!(typeof __REACT_DEVTOOLS_GLOBAL_HOOK__ > "u" || typeof __REACT_DEVTOOLS_GLOBAL_HOOK__.checkDCE != "function"))
      try {
        __REACT_DEVTOOLS_GLOBAL_HOOK__.checkDCE(n);
      } catch (i) {
        console.error(i);
      }
  }
  return n(), zc.exports = Db(), zc.exports;
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
var x0;
function Nb() {
  if (x0) return ss;
  x0 = 1;
  var n = Ob(), i = lf(), s = Cb();
  function r(e) {
    var t = "https://react.dev/errors/" + e;
    if (1 < arguments.length) {
      t += "?args[]=" + encodeURIComponent(arguments[1]);
      for (var a = 2; a < arguments.length; a++)
        t += "&args[]=" + encodeURIComponent(arguments[a]);
    }
    return "Minified React error #" + e + "; visit " + t + " for the full message or use the non-minified dev environment for full errors and additional helpful warnings.";
  }
  function c(e) {
    return !(!e || e.nodeType !== 1 && e.nodeType !== 9 && e.nodeType !== 11);
  }
  function f(e) {
    var t = e, a = e;
    if (e.alternate) for (; t.return; ) t = t.return;
    else {
      e = t;
      do
        t = e, (t.flags & 4098) !== 0 && (a = t.return), e = t.return;
      while (e);
    }
    return t.tag === 3 ? a : null;
  }
  function d(e) {
    if (e.tag === 13) {
      var t = e.memoizedState;
      if (t === null && (e = e.alternate, e !== null && (t = e.memoizedState)), t !== null) return t.dehydrated;
    }
    return null;
  }
  function h(e) {
    if (e.tag === 31) {
      var t = e.memoizedState;
      if (t === null && (e = e.alternate, e !== null && (t = e.memoizedState)), t !== null) return t.dehydrated;
    }
    return null;
  }
  function g(e) {
    if (f(e) !== e)
      throw Error(r(188));
  }
  function v(e) {
    var t = e.alternate;
    if (!t) {
      if (t = f(e), t === null) throw Error(r(188));
      return t !== e ? null : e;
    }
    for (var a = e, l = t; ; ) {
      var o = a.return;
      if (o === null) break;
      var u = o.alternate;
      if (u === null) {
        if (l = o.return, l !== null) {
          a = l;
          continue;
        }
        break;
      }
      if (o.child === u.child) {
        for (u = o.child; u; ) {
          if (u === a) return g(o), e;
          if (u === l) return g(o), t;
          u = u.sibling;
        }
        throw Error(r(188));
      }
      if (a.return !== l.return) a = o, l = u;
      else {
        for (var p = !1, b = o.child; b; ) {
          if (b === a) {
            p = !0, a = o, l = u;
            break;
          }
          if (b === l) {
            p = !0, l = o, a = u;
            break;
          }
          b = b.sibling;
        }
        if (!p) {
          for (b = u.child; b; ) {
            if (b === a) {
              p = !0, a = u, l = o;
              break;
            }
            if (b === l) {
              p = !0, l = u, a = o;
              break;
            }
            b = b.sibling;
          }
          if (!p) throw Error(r(189));
        }
      }
      if (a.alternate !== l) throw Error(r(190));
    }
    if (a.tag !== 3) throw Error(r(188));
    return a.stateNode.current === a ? e : t;
  }
  function y(e) {
    var t = e.tag;
    if (t === 5 || t === 26 || t === 27 || t === 6) return e;
    for (e = e.child; e !== null; ) {
      if (t = y(e), t !== null) return t;
      e = e.sibling;
    }
    return null;
  }
  var m = Object.assign, _ = Symbol.for("react.element"), x = Symbol.for("react.transitional.element"), T = Symbol.for("react.portal"), A = Symbol.for("react.fragment"), z = Symbol.for("react.strict_mode"), q = Symbol.for("react.profiler"), $ = Symbol.for("react.consumer"), j = Symbol.for("react.context"), L = Symbol.for("react.forward_ref"), V = Symbol.for("react.suspense"), J = Symbol.for("react.suspense_list"), Z = Symbol.for("react.memo"), F = Symbol.for("react.lazy"), Te = Symbol.for("react.activity"), Se = Symbol.for("react.memo_cache_sentinel"), oe = Symbol.iterator;
  function Ye(e) {
    return e === null || typeof e != "object" ? null : (e = oe && e[oe] || e["@@iterator"], typeof e == "function" ? e : null);
  }
  var ba = Symbol.for("react.client.reference");
  function It(e) {
    if (e == null) return null;
    if (typeof e == "function")
      return e.$$typeof === ba ? null : e.displayName || e.name || null;
    if (typeof e == "string") return e;
    switch (e) {
      case A:
        return "Fragment";
      case q:
        return "Profiler";
      case z:
        return "StrictMode";
      case V:
        return "Suspense";
      case J:
        return "SuspenseList";
      case Te:
        return "Activity";
    }
    if (typeof e == "object")
      switch (e.$$typeof) {
        case T:
          return "Portal";
        case j:
          return e.displayName || "Context";
        case $:
          return (e._context.displayName || "Context") + ".Consumer";
        case L:
          var t = e.render;
          return e = e.displayName, e || (e = t.displayName || t.name || "", e = e !== "" ? "ForwardRef(" + e + ")" : "ForwardRef"), e;
        case Z:
          return t = e.displayName || null, t !== null ? t : It(e.type) || "Memo";
        case F:
          t = e._payload, e = e._init;
          try {
            return It(e(t));
          } catch {
          }
      }
    return null;
  }
  var Ke = Array.isArray, R = i.__CLIENT_INTERNALS_DO_NOT_USE_OR_WARN_USERS_THEY_CANNOT_UPGRADE, Q = s.__DOM_INTERNALS_DO_NOT_USE_OR_WARN_USERS_THEY_CANNOT_UPGRADE, le = {
    pending: !1,
    data: null,
    method: null,
    action: null
  }, W = [], ye = -1;
  function k(e) {
    return { current: e };
  }
  function Y(e) {
    0 > ye || (e.current = W[ye], W[ye] = null, ye--);
  }
  function K(e, t) {
    ye++, W[ye] = e.current, e.current = t;
  }
  var I = k(null), se = k(null), fe = k(null), xe = k(null);
  function Je(e, t) {
    switch (K(fe, t), K(se, e), K(I, null), t.nodeType) {
      case 9:
      case 11:
        e = (e = t.documentElement) && (e = e.namespaceURI) ? $h(e) : 0;
        break;
      default:
        if (e = t.tagName, t = t.namespaceURI)
          t = $h(t), e = Uh(t, e);
        else
          switch (e) {
            case "svg":
              e = 1;
              break;
            case "math":
              e = 2;
              break;
            default:
              e = 0;
          }
    }
    Y(I), K(I, e);
  }
  function He() {
    Y(I), Y(se), Y(fe);
  }
  function ra(e) {
    e.memoizedState !== null && K(xe, e);
    var t = I.current, a = Uh(t, e.type);
    t !== a && (K(se, e), K(I, a));
  }
  function yt(e) {
    se.current === e && (Y(I), Y(se)), xe.current === e && (Y(xe), as._currentValue = le);
  }
  var dl, pl;
  function ya(e) {
    if (dl === void 0)
      try {
        throw Error();
      } catch (a) {
        var t = a.stack.trim().match(/\n( *(at )?)/);
        dl = t && t[1] || "", pl = -1 < a.stack.indexOf(`
    at`) ? " (<anonymous>)" : -1 < a.stack.indexOf("@") ? "@unknown:0:0" : "";
      }
    return `
` + dl + e + pl;
  }
  var Pt = !1;
  function mt(e, t) {
    if (!e || Pt) return "";
    Pt = !0;
    var a = Error.prepareStackTrace;
    Error.prepareStackTrace = void 0;
    try {
      var l = {
        DetermineComponentFrameRoot: function() {
          try {
            if (t) {
              var B = function() {
                throw Error();
              };
              if (Object.defineProperty(B.prototype, "props", {
                set: function() {
                  throw Error();
                }
              }), typeof Reflect == "object" && Reflect.construct) {
                try {
                  Reflect.construct(B, []);
                } catch (M) {
                  var C = M;
                }
                Reflect.construct(e, [], B);
              } else {
                try {
                  B.call();
                } catch (M) {
                  C = M;
                }
                e.call(B.prototype);
              }
            } else {
              try {
                throw Error();
              } catch (M) {
                C = M;
              }
              (B = e()) && typeof B.catch == "function" && B.catch(function() {
              });
            }
          } catch (M) {
            if (M && C && typeof M.stack == "string")
              return [M.stack, C.stack];
          }
          return [null, null];
        }
      };
      l.DetermineComponentFrameRoot.displayName = "DetermineComponentFrameRoot";
      var o = Object.getOwnPropertyDescriptor(
        l.DetermineComponentFrameRoot,
        "name"
      );
      o && o.configurable && Object.defineProperty(
        l.DetermineComponentFrameRoot,
        "name",
        { value: "DetermineComponentFrameRoot" }
      );
      var u = l.DetermineComponentFrameRoot(), p = u[0], b = u[1];
      if (p && b) {
        var w = p.split(`
`), D = b.split(`
`);
        for (o = l = 0; l < w.length && !w[l].includes("DetermineComponentFrameRoot"); )
          l++;
        for (; o < D.length && !D[o].includes(
          "DetermineComponentFrameRoot"
        ); )
          o++;
        if (l === w.length || o === D.length)
          for (l = w.length - 1, o = D.length - 1; 1 <= l && 0 <= o && w[l] !== D[o]; )
            o--;
        for (; 1 <= l && 0 <= o; l--, o--)
          if (w[l] !== D[o]) {
            if (l !== 1 || o !== 1)
              do
                if (l--, o--, 0 > o || w[l] !== D[o]) {
                  var U = `
` + w[l].replace(" at new ", " at ");
                  return e.displayName && U.includes("<anonymous>") && (U = U.replace("<anonymous>", e.displayName)), U;
                }
              while (1 <= l && 0 <= o);
            break;
          }
      }
    } finally {
      Pt = !1, Error.prepareStackTrace = a;
    }
    return (a = e ? e.displayName || e.name : "") ? ya(a) : "";
  }
  function xs(e, t) {
    switch (e.tag) {
      case 26:
      case 27:
      case 5:
        return ya(e.type);
      case 16:
        return ya("Lazy");
      case 13:
        return e.child !== t && t !== null ? ya("Suspense Fallback") : ya("Suspense");
      case 19:
        return ya("SuspenseList");
      case 0:
      case 15:
        return mt(e.type, !1);
      case 11:
        return mt(e.type.render, !1);
      case 1:
        return mt(e.type, !0);
      case 31:
        return ya("Activity");
      default:
        return "";
    }
  }
  function vt(e) {
    try {
      var t = "", a = null;
      do
        t += xs(e, a), a = e, e = e.return;
      while (e);
      return t;
    } catch (l) {
      return `
Error generating stack: ` + l.message + `
` + l.stack;
    }
  }
  var Cn = Object.prototype.hasOwnProperty, hl = n.unstable_scheduleCallback, ml = n.unstable_cancelCallback, ws = n.unstable_shouldYield, fi = n.unstable_requestPaint, Me = n.unstable_now, ks = n.unstable_getCurrentPriorityLevel, Nn = n.unstable_ImmediatePriority, Mn = n.unstable_UserBlockingPriority, di = n.unstable_NormalPriority, so = n.unstable_LowPriority, pi = n.unstable_IdlePriority, Qa = n.log, ro = n.unstable_setDisableYieldValue, Za = null, tt = null;
  function oa(e) {
    if (typeof Qa == "function" && ro(e), tt && typeof tt.setStrictMode == "function")
      try {
        tt.setStrictMode(Za, e);
      } catch {
      }
  }
  var rt = Math.clz32 ? Math.clz32 : zs, Ss = Math.log, oo = Math.LN2;
  function zs(e) {
    return e >>>= 0, e === 0 ? 32 : 31 - (Ss(e) / oo | 0) | 0;
  }
  var hi = 256, qn = 262144, _a = 4194304;
  function ua(e) {
    var t = e & 42;
    if (t !== 0) return t;
    switch (e & -e) {
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
        return e & 261888;
      case 262144:
      case 524288:
      case 1048576:
      case 2097152:
        return e & 3932160;
      case 4194304:
      case 8388608:
      case 16777216:
      case 33554432:
        return e & 62914560;
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
        return e;
    }
  }
  function Ka(e, t, a) {
    var l = e.pendingLanes;
    if (l === 0) return 0;
    var o = 0, u = e.suspendedLanes, p = e.pingedLanes;
    e = e.warmLanes;
    var b = l & 134217727;
    return b !== 0 ? (l = b & ~u, l !== 0 ? o = ua(l) : (p &= b, p !== 0 ? o = ua(p) : a || (a = b & ~e, a !== 0 && (o = ua(a))))) : (b = l & ~u, b !== 0 ? o = ua(b) : p !== 0 ? o = ua(p) : a || (a = l & ~e, a !== 0 && (o = ua(a)))), o === 0 ? 0 : t !== 0 && t !== o && (t & u) === 0 && (u = o & -o, a = t & -t, u >= a || u === 32 && (a & 4194048) !== 0) ? t : o;
  }
  function ca(e, t) {
    return (e.pendingLanes & ~(e.suspendedLanes & ~e.pingedLanes) & t) === 0;
  }
  function Ts(e, t) {
    switch (e) {
      case 1:
      case 2:
      case 4:
      case 8:
      case 64:
        return t + 250;
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
        return t + 5e3;
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
  function jn() {
    var e = _a;
    return _a <<= 1, (_a & 62914560) === 0 && (_a = 4194304), e;
  }
  function vl(e) {
    for (var t = [], a = 0; 31 > a; a++) t.push(e);
    return t;
  }
  function G(e, t) {
    e.pendingLanes |= t, t !== 268435456 && (e.suspendedLanes = 0, e.pingedLanes = 0, e.warmLanes = 0);
  }
  function ae(e, t, a, l, o, u) {
    var p = e.pendingLanes;
    e.pendingLanes = a, e.suspendedLanes = 0, e.pingedLanes = 0, e.warmLanes = 0, e.expiredLanes &= a, e.entangledLanes &= a, e.errorRecoveryDisabledLanes &= a, e.shellSuspendCounter = 0;
    var b = e.entanglements, w = e.expirationTimes, D = e.hiddenUpdates;
    for (a = p & ~a; 0 < a; ) {
      var U = 31 - rt(a), B = 1 << U;
      b[U] = 0, w[U] = -1;
      var C = D[U];
      if (C !== null)
        for (D[U] = null, U = 0; U < C.length; U++) {
          var M = C[U];
          M !== null && (M.lane &= -536870913);
        }
      a &= ~B;
    }
    l !== 0 && _e(e, l, 0), u !== 0 && o === 0 && e.tag !== 0 && (e.suspendedLanes |= u & ~(p & ~t));
  }
  function _e(e, t, a) {
    e.pendingLanes |= t, e.suspendedLanes &= ~t;
    var l = 31 - rt(t);
    e.entangledLanes |= t, e.entanglements[l] = e.entanglements[l] | 1073741824 | a & 261930;
  }
  function Re(e, t) {
    var a = e.entangledLanes |= t;
    for (e = e.entanglements; a; ) {
      var l = 31 - rt(a), o = 1 << l;
      o & t | e[l] & t && (e[l] |= t), a &= ~o;
    }
  }
  function Fe(e, t) {
    var a = t & -t;
    return a = (a & 42) !== 0 ? 1 : mi(a), (a & (e.suspendedLanes | t)) !== 0 ? 0 : a;
  }
  function mi(e) {
    switch (e) {
      case 2:
        e = 1;
        break;
      case 8:
        e = 4;
        break;
      case 32:
        e = 16;
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
        e = 128;
        break;
      case 268435456:
        e = 134217728;
        break;
      default:
        e = 0;
    }
    return e;
  }
  function uo(e) {
    return e &= -e, 2 < e ? 8 < e ? (e & 134217727) !== 0 ? 32 : 268435456 : 8 : 2;
  }
  function wf() {
    var e = Q.p;
    return e !== 0 ? e : (e = window.event, e === void 0 ? 32 : r0(e.type));
  }
  function kf(e, t) {
    var a = Q.p;
    try {
      return Q.p = e, t();
    } finally {
      Q.p = a;
    }
  }
  var Ja = Math.random().toString(36).slice(2), ot = "__reactFiber$" + Ja, wt = "__reactProps$" + Ja, vi = "__reactContainer$" + Ja, co = "__reactEvents$" + Ja, hv = "__reactListeners$" + Ja, mv = "__reactHandles$" + Ja, Sf = "__reactResources$" + Ja, gl = "__reactMarker$" + Ja;
  function fo(e) {
    delete e[ot], delete e[wt], delete e[co], delete e[hv], delete e[mv];
  }
  function gi(e) {
    var t = e[ot];
    if (t) return t;
    for (var a = e.parentNode; a; ) {
      if (t = a[vi] || a[ot]) {
        if (a = t.alternate, t.child !== null || a !== null && a.child !== null)
          for (e = Gh(e); e !== null; ) {
            if (a = e[ot]) return a;
            e = Gh(e);
          }
        return t;
      }
      e = a, a = e.parentNode;
    }
    return null;
  }
  function bi(e) {
    if (e = e[ot] || e[vi]) {
      var t = e.tag;
      if (t === 5 || t === 6 || t === 13 || t === 31 || t === 26 || t === 27 || t === 3)
        return e;
    }
    return null;
  }
  function bl(e) {
    var t = e.tag;
    if (t === 5 || t === 26 || t === 27 || t === 6) return e.stateNode;
    throw Error(r(33));
  }
  function yi(e) {
    var t = e[Sf];
    return t || (t = e[Sf] = { hoistableStyles: /* @__PURE__ */ new Map(), hoistableScripts: /* @__PURE__ */ new Map() }), t;
  }
  function it(e) {
    e[gl] = !0;
  }
  var zf = /* @__PURE__ */ new Set(), Tf = {};
  function Rn(e, t) {
    _i(e, t), _i(e + "Capture", t);
  }
  function _i(e, t) {
    for (Tf[e] = t, e = 0; e < t.length; e++)
      zf.add(t[e]);
  }
  var vv = RegExp(
    "^[:A-Z_a-z\\u00C0-\\u00D6\\u00D8-\\u00F6\\u00F8-\\u02FF\\u0370-\\u037D\\u037F-\\u1FFF\\u200C-\\u200D\\u2070-\\u218F\\u2C00-\\u2FEF\\u3001-\\uD7FF\\uF900-\\uFDCF\\uFDF0-\\uFFFD][:A-Z_a-z\\u00C0-\\u00D6\\u00D8-\\u00F6\\u00F8-\\u02FF\\u0370-\\u037D\\u037F-\\u1FFF\\u200C-\\u200D\\u2070-\\u218F\\u2C00-\\u2FEF\\u3001-\\uD7FF\\uF900-\\uFDCF\\uFDF0-\\uFFFD\\-.0-9\\u00B7\\u0300-\\u036F\\u203F-\\u2040]*$"
  ), Ef = {}, Af = {};
  function gv(e) {
    return Cn.call(Af, e) ? !0 : Cn.call(Ef, e) ? !1 : vv.test(e) ? Af[e] = !0 : (Ef[e] = !0, !1);
  }
  function Es(e, t, a) {
    if (gv(t))
      if (a === null) e.removeAttribute(t);
      else {
        switch (typeof a) {
          case "undefined":
          case "function":
          case "symbol":
            e.removeAttribute(t);
            return;
          case "boolean":
            var l = t.toLowerCase().slice(0, 5);
            if (l !== "data-" && l !== "aria-") {
              e.removeAttribute(t);
              return;
            }
        }
        e.setAttribute(t, "" + a);
      }
  }
  function As(e, t, a) {
    if (a === null) e.removeAttribute(t);
    else {
      switch (typeof a) {
        case "undefined":
        case "function":
        case "symbol":
        case "boolean":
          e.removeAttribute(t);
          return;
      }
      e.setAttribute(t, "" + a);
    }
  }
  function xa(e, t, a, l) {
    if (l === null) e.removeAttribute(a);
    else {
      switch (typeof l) {
        case "undefined":
        case "function":
        case "symbol":
        case "boolean":
          e.removeAttribute(a);
          return;
      }
      e.setAttributeNS(t, a, "" + l);
    }
  }
  function Yt(e) {
    switch (typeof e) {
      case "bigint":
      case "boolean":
      case "number":
      case "string":
      case "undefined":
        return e;
      case "object":
        return e;
      default:
        return "";
    }
  }
  function Of(e) {
    var t = e.type;
    return (e = e.nodeName) && e.toLowerCase() === "input" && (t === "checkbox" || t === "radio");
  }
  function bv(e, t, a) {
    var l = Object.getOwnPropertyDescriptor(
      e.constructor.prototype,
      t
    );
    if (!e.hasOwnProperty(t) && typeof l < "u" && typeof l.get == "function" && typeof l.set == "function") {
      var o = l.get, u = l.set;
      return Object.defineProperty(e, t, {
        configurable: !0,
        get: function() {
          return o.call(this);
        },
        set: function(p) {
          a = "" + p, u.call(this, p);
        }
      }), Object.defineProperty(e, t, {
        enumerable: l.enumerable
      }), {
        getValue: function() {
          return a;
        },
        setValue: function(p) {
          a = "" + p;
        },
        stopTracking: function() {
          e._valueTracker = null, delete e[t];
        }
      };
    }
  }
  function po(e) {
    if (!e._valueTracker) {
      var t = Of(e) ? "checked" : "value";
      e._valueTracker = bv(
        e,
        t,
        "" + e[t]
      );
    }
  }
  function Df(e) {
    if (!e) return !1;
    var t = e._valueTracker;
    if (!t) return !0;
    var a = t.getValue(), l = "";
    return e && (l = Of(e) ? e.checked ? "true" : "false" : e.value), e = l, e !== a ? (t.setValue(e), !0) : !1;
  }
  function Os(e) {
    if (e = e || (typeof document < "u" ? document : void 0), typeof e > "u") return null;
    try {
      return e.activeElement || e.body;
    } catch {
      return e.body;
    }
  }
  var yv = /[\n"\\]/g;
  function Bt(e) {
    return e.replace(
      yv,
      function(t) {
        return "\\" + t.charCodeAt(0).toString(16) + " ";
      }
    );
  }
  function ho(e, t, a, l, o, u, p, b) {
    e.name = "", p != null && typeof p != "function" && typeof p != "symbol" && typeof p != "boolean" ? e.type = p : e.removeAttribute("type"), t != null ? p === "number" ? (t === 0 && e.value === "" || e.value != t) && (e.value = "" + Yt(t)) : e.value !== "" + Yt(t) && (e.value = "" + Yt(t)) : p !== "submit" && p !== "reset" || e.removeAttribute("value"), t != null ? mo(e, p, Yt(t)) : a != null ? mo(e, p, Yt(a)) : l != null && e.removeAttribute("value"), o == null && u != null && (e.defaultChecked = !!u), o != null && (e.checked = o && typeof o != "function" && typeof o != "symbol"), b != null && typeof b != "function" && typeof b != "symbol" && typeof b != "boolean" ? e.name = "" + Yt(b) : e.removeAttribute("name");
  }
  function Cf(e, t, a, l, o, u, p, b) {
    if (u != null && typeof u != "function" && typeof u != "symbol" && typeof u != "boolean" && (e.type = u), t != null || a != null) {
      if (!(u !== "submit" && u !== "reset" || t != null)) {
        po(e);
        return;
      }
      a = a != null ? "" + Yt(a) : "", t = t != null ? "" + Yt(t) : a, b || t === e.value || (e.value = t), e.defaultValue = t;
    }
    l = l ?? o, l = typeof l != "function" && typeof l != "symbol" && !!l, e.checked = b ? e.checked : !!l, e.defaultChecked = !!l, p != null && typeof p != "function" && typeof p != "symbol" && typeof p != "boolean" && (e.name = p), po(e);
  }
  function mo(e, t, a) {
    t === "number" && Os(e.ownerDocument) === e || e.defaultValue === "" + a || (e.defaultValue = "" + a);
  }
  function xi(e, t, a, l) {
    if (e = e.options, t) {
      t = {};
      for (var o = 0; o < a.length; o++)
        t["$" + a[o]] = !0;
      for (a = 0; a < e.length; a++)
        o = t.hasOwnProperty("$" + e[a].value), e[a].selected !== o && (e[a].selected = o), o && l && (e[a].defaultSelected = !0);
    } else {
      for (a = "" + Yt(a), t = null, o = 0; o < e.length; o++) {
        if (e[o].value === a) {
          e[o].selected = !0, l && (e[o].defaultSelected = !0);
          return;
        }
        t !== null || e[o].disabled || (t = e[o]);
      }
      t !== null && (t.selected = !0);
    }
  }
  function Nf(e, t, a) {
    if (t != null && (t = "" + Yt(t), t !== e.value && (e.value = t), a == null)) {
      e.defaultValue !== t && (e.defaultValue = t);
      return;
    }
    e.defaultValue = a != null ? "" + Yt(a) : "";
  }
  function Mf(e, t, a, l) {
    if (t == null) {
      if (l != null) {
        if (a != null) throw Error(r(92));
        if (Ke(l)) {
          if (1 < l.length) throw Error(r(93));
          l = l[0];
        }
        a = l;
      }
      a == null && (a = ""), t = a;
    }
    a = Yt(t), e.defaultValue = a, l = e.textContent, l === a && l !== "" && l !== null && (e.value = l), po(e);
  }
  function wi(e, t) {
    if (t) {
      var a = e.firstChild;
      if (a && a === e.lastChild && a.nodeType === 3) {
        a.nodeValue = t;
        return;
      }
    }
    e.textContent = t;
  }
  var _v = new Set(
    "animationIterationCount aspectRatio borderImageOutset borderImageSlice borderImageWidth boxFlex boxFlexGroup boxOrdinalGroup columnCount columns flex flexGrow flexPositive flexShrink flexNegative flexOrder gridArea gridRow gridRowEnd gridRowSpan gridRowStart gridColumn gridColumnEnd gridColumnSpan gridColumnStart fontWeight lineClamp lineHeight opacity order orphans scale tabSize widows zIndex zoom fillOpacity floodOpacity stopOpacity strokeDasharray strokeDashoffset strokeMiterlimit strokeOpacity strokeWidth MozAnimationIterationCount MozBoxFlex MozBoxFlexGroup MozLineClamp msAnimationIterationCount msFlex msZoom msFlexGrow msFlexNegative msFlexOrder msFlexPositive msFlexShrink msGridColumn msGridColumnSpan msGridRow msGridRowSpan WebkitAnimationIterationCount WebkitBoxFlex WebKitBoxFlexGroup WebkitBoxOrdinalGroup WebkitColumnCount WebkitColumns WebkitFlex WebkitFlexGrow WebkitFlexPositive WebkitFlexShrink WebkitLineClamp".split(
      " "
    )
  );
  function qf(e, t, a) {
    var l = t.indexOf("--") === 0;
    a == null || typeof a == "boolean" || a === "" ? l ? e.setProperty(t, "") : t === "float" ? e.cssFloat = "" : e[t] = "" : l ? e.setProperty(t, a) : typeof a != "number" || a === 0 || _v.has(t) ? t === "float" ? e.cssFloat = a : e[t] = ("" + a).trim() : e[t] = a + "px";
  }
  function jf(e, t, a) {
    if (t != null && typeof t != "object")
      throw Error(r(62));
    if (e = e.style, a != null) {
      for (var l in a)
        !a.hasOwnProperty(l) || t != null && t.hasOwnProperty(l) || (l.indexOf("--") === 0 ? e.setProperty(l, "") : l === "float" ? e.cssFloat = "" : e[l] = "");
      for (var o in t)
        l = t[o], t.hasOwnProperty(o) && a[o] !== l && qf(e, o, l);
    } else
      for (var u in t)
        t.hasOwnProperty(u) && qf(e, u, t[u]);
  }
  function vo(e) {
    if (e.indexOf("-") === -1) return !1;
    switch (e) {
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
  var xv = /* @__PURE__ */ new Map([
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
  ]), wv = /^[\u0000-\u001F ]*j[\r\n\t]*a[\r\n\t]*v[\r\n\t]*a[\r\n\t]*s[\r\n\t]*c[\r\n\t]*r[\r\n\t]*i[\r\n\t]*p[\r\n\t]*t[\r\n\t]*:/i;
  function Ds(e) {
    return wv.test("" + e) ? "javascript:throw new Error('React has blocked a javascript: URL as a security precaution.')" : e;
  }
  function wa() {
  }
  var go = null;
  function bo(e) {
    return e = e.target || e.srcElement || window, e.correspondingUseElement && (e = e.correspondingUseElement), e.nodeType === 3 ? e.parentNode : e;
  }
  var ki = null, Si = null;
  function Rf(e) {
    var t = bi(e);
    if (t && (e = t.stateNode)) {
      var a = e[wt] || null;
      e: switch (e = t.stateNode, t.type) {
        case "input":
          if (ho(
            e,
            a.value,
            a.defaultValue,
            a.defaultValue,
            a.checked,
            a.defaultChecked,
            a.type,
            a.name
          ), t = a.name, a.type === "radio" && t != null) {
            for (a = e; a.parentNode; ) a = a.parentNode;
            for (a = a.querySelectorAll(
              'input[name="' + Bt(
                "" + t
              ) + '"][type="radio"]'
            ), t = 0; t < a.length; t++) {
              var l = a[t];
              if (l !== e && l.form === e.form) {
                var o = l[wt] || null;
                if (!o) throw Error(r(90));
                ho(
                  l,
                  o.value,
                  o.defaultValue,
                  o.defaultValue,
                  o.checked,
                  o.defaultChecked,
                  o.type,
                  o.name
                );
              }
            }
            for (t = 0; t < a.length; t++)
              l = a[t], l.form === e.form && Df(l);
          }
          break e;
        case "textarea":
          Nf(e, a.value, a.defaultValue);
          break e;
        case "select":
          t = a.value, t != null && xi(e, !!a.multiple, t, !1);
      }
    }
  }
  var yo = !1;
  function $f(e, t, a) {
    if (yo) return e(t, a);
    yo = !0;
    try {
      var l = e(t);
      return l;
    } finally {
      if (yo = !1, (ki !== null || Si !== null) && (gr(), ki && (t = ki, e = Si, Si = ki = null, Rf(t), e)))
        for (t = 0; t < e.length; t++) Rf(e[t]);
    }
  }
  function yl(e, t) {
    var a = e.stateNode;
    if (a === null) return null;
    var l = a[wt] || null;
    if (l === null) return null;
    a = l[t];
    e: switch (t) {
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
        (l = !l.disabled) || (e = e.type, l = !(e === "button" || e === "input" || e === "select" || e === "textarea")), e = !l;
        break e;
      default:
        e = !1;
    }
    if (e) return null;
    if (a && typeof a != "function")
      throw Error(
        r(231, t, typeof a)
      );
    return a;
  }
  var ka = !(typeof window > "u" || typeof window.document > "u" || typeof window.document.createElement > "u"), _o = !1;
  if (ka)
    try {
      var _l = {};
      Object.defineProperty(_l, "passive", {
        get: function() {
          _o = !0;
        }
      }), window.addEventListener("test", _l, _l), window.removeEventListener("test", _l, _l);
    } catch {
      _o = !1;
    }
  var Fa = null, xo = null, Cs = null;
  function Uf() {
    if (Cs) return Cs;
    var e, t = xo, a = t.length, l, o = "value" in Fa ? Fa.value : Fa.textContent, u = o.length;
    for (e = 0; e < a && t[e] === o[e]; e++) ;
    var p = a - e;
    for (l = 1; l <= p && t[a - l] === o[u - l]; l++) ;
    return Cs = o.slice(e, 1 < l ? 1 - l : void 0);
  }
  function Ns(e) {
    var t = e.keyCode;
    return "charCode" in e ? (e = e.charCode, e === 0 && t === 13 && (e = 13)) : e = t, e === 10 && (e = 13), 32 <= e || e === 13 ? e : 0;
  }
  function Ms() {
    return !0;
  }
  function Lf() {
    return !1;
  }
  function kt(e) {
    function t(a, l, o, u, p) {
      this._reactName = a, this._targetInst = o, this.type = l, this.nativeEvent = u, this.target = p, this.currentTarget = null;
      for (var b in e)
        e.hasOwnProperty(b) && (a = e[b], this[b] = a ? a(u) : u[b]);
      return this.isDefaultPrevented = (u.defaultPrevented != null ? u.defaultPrevented : u.returnValue === !1) ? Ms : Lf, this.isPropagationStopped = Lf, this;
    }
    return m(t.prototype, {
      preventDefault: function() {
        this.defaultPrevented = !0;
        var a = this.nativeEvent;
        a && (a.preventDefault ? a.preventDefault() : typeof a.returnValue != "unknown" && (a.returnValue = !1), this.isDefaultPrevented = Ms);
      },
      stopPropagation: function() {
        var a = this.nativeEvent;
        a && (a.stopPropagation ? a.stopPropagation() : typeof a.cancelBubble != "unknown" && (a.cancelBubble = !0), this.isPropagationStopped = Ms);
      },
      persist: function() {
      },
      isPersistent: Ms
    }), t;
  }
  var $n = {
    eventPhase: 0,
    bubbles: 0,
    cancelable: 0,
    timeStamp: function(e) {
      return e.timeStamp || Date.now();
    },
    defaultPrevented: 0,
    isTrusted: 0
  }, qs = kt($n), xl = m({}, $n, { view: 0, detail: 0 }), kv = kt(xl), wo, ko, wl, js = m({}, xl, {
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
    getModifierState: zo,
    button: 0,
    buttons: 0,
    relatedTarget: function(e) {
      return e.relatedTarget === void 0 ? e.fromElement === e.srcElement ? e.toElement : e.fromElement : e.relatedTarget;
    },
    movementX: function(e) {
      return "movementX" in e ? e.movementX : (e !== wl && (wl && e.type === "mousemove" ? (wo = e.screenX - wl.screenX, ko = e.screenY - wl.screenY) : ko = wo = 0, wl = e), wo);
    },
    movementY: function(e) {
      return "movementY" in e ? e.movementY : ko;
    }
  }), Hf = kt(js), Sv = m({}, js, { dataTransfer: 0 }), zv = kt(Sv), Tv = m({}, xl, { relatedTarget: 0 }), So = kt(Tv), Ev = m({}, $n, {
    animationName: 0,
    elapsedTime: 0,
    pseudoElement: 0
  }), Av = kt(Ev), Ov = m({}, $n, {
    clipboardData: function(e) {
      return "clipboardData" in e ? e.clipboardData : window.clipboardData;
    }
  }), Dv = kt(Ov), Cv = m({}, $n, { data: 0 }), Yf = kt(Cv), Nv = {
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
  }, Mv = {
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
  }, qv = {
    Alt: "altKey",
    Control: "ctrlKey",
    Meta: "metaKey",
    Shift: "shiftKey"
  };
  function jv(e) {
    var t = this.nativeEvent;
    return t.getModifierState ? t.getModifierState(e) : (e = qv[e]) ? !!t[e] : !1;
  }
  function zo() {
    return jv;
  }
  var Rv = m({}, xl, {
    key: function(e) {
      if (e.key) {
        var t = Nv[e.key] || e.key;
        if (t !== "Unidentified") return t;
      }
      return e.type === "keypress" ? (e = Ns(e), e === 13 ? "Enter" : String.fromCharCode(e)) : e.type === "keydown" || e.type === "keyup" ? Mv[e.keyCode] || "Unidentified" : "";
    },
    code: 0,
    location: 0,
    ctrlKey: 0,
    shiftKey: 0,
    altKey: 0,
    metaKey: 0,
    repeat: 0,
    locale: 0,
    getModifierState: zo,
    charCode: function(e) {
      return e.type === "keypress" ? Ns(e) : 0;
    },
    keyCode: function(e) {
      return e.type === "keydown" || e.type === "keyup" ? e.keyCode : 0;
    },
    which: function(e) {
      return e.type === "keypress" ? Ns(e) : e.type === "keydown" || e.type === "keyup" ? e.keyCode : 0;
    }
  }), $v = kt(Rv), Uv = m({}, js, {
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
  }), Bf = kt(Uv), Lv = m({}, xl, {
    touches: 0,
    targetTouches: 0,
    changedTouches: 0,
    altKey: 0,
    metaKey: 0,
    ctrlKey: 0,
    shiftKey: 0,
    getModifierState: zo
  }), Hv = kt(Lv), Yv = m({}, $n, {
    propertyName: 0,
    elapsedTime: 0,
    pseudoElement: 0
  }), Bv = kt(Yv), Vv = m({}, js, {
    deltaX: function(e) {
      return "deltaX" in e ? e.deltaX : "wheelDeltaX" in e ? -e.wheelDeltaX : 0;
    },
    deltaY: function(e) {
      return "deltaY" in e ? e.deltaY : "wheelDeltaY" in e ? -e.wheelDeltaY : "wheelDelta" in e ? -e.wheelDelta : 0;
    },
    deltaZ: 0,
    deltaMode: 0
  }), Xv = kt(Vv), Gv = m({}, $n, {
    newState: 0,
    oldState: 0
  }), Qv = kt(Gv), Zv = [9, 13, 27, 32], To = ka && "CompositionEvent" in window, kl = null;
  ka && "documentMode" in document && (kl = document.documentMode);
  var Kv = ka && "TextEvent" in window && !kl, Vf = ka && (!To || kl && 8 < kl && 11 >= kl), Xf = " ", Gf = !1;
  function Qf(e, t) {
    switch (e) {
      case "keyup":
        return Zv.indexOf(t.keyCode) !== -1;
      case "keydown":
        return t.keyCode !== 229;
      case "keypress":
      case "mousedown":
      case "focusout":
        return !0;
      default:
        return !1;
    }
  }
  function Zf(e) {
    return e = e.detail, typeof e == "object" && "data" in e ? e.data : null;
  }
  var zi = !1;
  function Jv(e, t) {
    switch (e) {
      case "compositionend":
        return Zf(t);
      case "keypress":
        return t.which !== 32 ? null : (Gf = !0, Xf);
      case "textInput":
        return e = t.data, e === Xf && Gf ? null : e;
      default:
        return null;
    }
  }
  function Fv(e, t) {
    if (zi)
      return e === "compositionend" || !To && Qf(e, t) ? (e = Uf(), Cs = xo = Fa = null, zi = !1, e) : null;
    switch (e) {
      case "paste":
        return null;
      case "keypress":
        if (!(t.ctrlKey || t.altKey || t.metaKey) || t.ctrlKey && t.altKey) {
          if (t.char && 1 < t.char.length)
            return t.char;
          if (t.which) return String.fromCharCode(t.which);
        }
        return null;
      case "compositionend":
        return Vf && t.locale !== "ko" ? null : t.data;
      default:
        return null;
    }
  }
  var Wv = {
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
  function Kf(e) {
    var t = e && e.nodeName && e.nodeName.toLowerCase();
    return t === "input" ? !!Wv[e.type] : t === "textarea";
  }
  function Jf(e, t, a, l) {
    ki ? Si ? Si.push(l) : Si = [l] : ki = l, t = Sr(t, "onChange"), 0 < t.length && (a = new qs(
      "onChange",
      "change",
      null,
      a,
      l
    ), e.push({ event: a, listeners: t }));
  }
  var Sl = null, zl = null;
  function Iv(e) {
    Ch(e, 0);
  }
  function Rs(e) {
    var t = bl(e);
    if (Df(t)) return e;
  }
  function Ff(e, t) {
    if (e === "change") return t;
  }
  var Wf = !1;
  if (ka) {
    var Eo;
    if (ka) {
      var Ao = "oninput" in document;
      if (!Ao) {
        var If = document.createElement("div");
        If.setAttribute("oninput", "return;"), Ao = typeof If.oninput == "function";
      }
      Eo = Ao;
    } else Eo = !1;
    Wf = Eo && (!document.documentMode || 9 < document.documentMode);
  }
  function Pf() {
    Sl && (Sl.detachEvent("onpropertychange", ed), zl = Sl = null);
  }
  function ed(e) {
    if (e.propertyName === "value" && Rs(zl)) {
      var t = [];
      Jf(
        t,
        zl,
        e,
        bo(e)
      ), $f(Iv, t);
    }
  }
  function Pv(e, t, a) {
    e === "focusin" ? (Pf(), Sl = t, zl = a, Sl.attachEvent("onpropertychange", ed)) : e === "focusout" && Pf();
  }
  function eg(e) {
    if (e === "selectionchange" || e === "keyup" || e === "keydown")
      return Rs(zl);
  }
  function tg(e, t) {
    if (e === "click") return Rs(t);
  }
  function ag(e, t) {
    if (e === "input" || e === "change")
      return Rs(t);
  }
  function ng(e, t) {
    return e === t && (e !== 0 || 1 / e === 1 / t) || e !== e && t !== t;
  }
  var Nt = typeof Object.is == "function" ? Object.is : ng;
  function Tl(e, t) {
    if (Nt(e, t)) return !0;
    if (typeof e != "object" || e === null || typeof t != "object" || t === null)
      return !1;
    var a = Object.keys(e), l = Object.keys(t);
    if (a.length !== l.length) return !1;
    for (l = 0; l < a.length; l++) {
      var o = a[l];
      if (!Cn.call(t, o) || !Nt(e[o], t[o]))
        return !1;
    }
    return !0;
  }
  function td(e) {
    for (; e && e.firstChild; ) e = e.firstChild;
    return e;
  }
  function ad(e, t) {
    var a = td(e);
    e = 0;
    for (var l; a; ) {
      if (a.nodeType === 3) {
        if (l = e + a.textContent.length, e <= t && l >= t)
          return { node: a, offset: t - e };
        e = l;
      }
      e: {
        for (; a; ) {
          if (a.nextSibling) {
            a = a.nextSibling;
            break e;
          }
          a = a.parentNode;
        }
        a = void 0;
      }
      a = td(a);
    }
  }
  function nd(e, t) {
    return e && t ? e === t ? !0 : e && e.nodeType === 3 ? !1 : t && t.nodeType === 3 ? nd(e, t.parentNode) : "contains" in e ? e.contains(t) : e.compareDocumentPosition ? !!(e.compareDocumentPosition(t) & 16) : !1 : !1;
  }
  function id(e) {
    e = e != null && e.ownerDocument != null && e.ownerDocument.defaultView != null ? e.ownerDocument.defaultView : window;
    for (var t = Os(e.document); t instanceof e.HTMLIFrameElement; ) {
      try {
        var a = typeof t.contentWindow.location.href == "string";
      } catch {
        a = !1;
      }
      if (a) e = t.contentWindow;
      else break;
      t = Os(e.document);
    }
    return t;
  }
  function Oo(e) {
    var t = e && e.nodeName && e.nodeName.toLowerCase();
    return t && (t === "input" && (e.type === "text" || e.type === "search" || e.type === "tel" || e.type === "url" || e.type === "password") || t === "textarea" || e.contentEditable === "true");
  }
  var ig = ka && "documentMode" in document && 11 >= document.documentMode, Ti = null, Do = null, El = null, Co = !1;
  function ld(e, t, a) {
    var l = a.window === a ? a.document : a.nodeType === 9 ? a : a.ownerDocument;
    Co || Ti == null || Ti !== Os(l) || (l = Ti, "selectionStart" in l && Oo(l) ? l = { start: l.selectionStart, end: l.selectionEnd } : (l = (l.ownerDocument && l.ownerDocument.defaultView || window).getSelection(), l = {
      anchorNode: l.anchorNode,
      anchorOffset: l.anchorOffset,
      focusNode: l.focusNode,
      focusOffset: l.focusOffset
    }), El && Tl(El, l) || (El = l, l = Sr(Do, "onSelect"), 0 < l.length && (t = new qs(
      "onSelect",
      "select",
      null,
      t,
      a
    ), e.push({ event: t, listeners: l }), t.target = Ti)));
  }
  function Un(e, t) {
    var a = {};
    return a[e.toLowerCase()] = t.toLowerCase(), a["Webkit" + e] = "webkit" + t, a["Moz" + e] = "moz" + t, a;
  }
  var Ei = {
    animationend: Un("Animation", "AnimationEnd"),
    animationiteration: Un("Animation", "AnimationIteration"),
    animationstart: Un("Animation", "AnimationStart"),
    transitionrun: Un("Transition", "TransitionRun"),
    transitionstart: Un("Transition", "TransitionStart"),
    transitioncancel: Un("Transition", "TransitionCancel"),
    transitionend: Un("Transition", "TransitionEnd")
  }, No = {}, sd = {};
  ka && (sd = document.createElement("div").style, "AnimationEvent" in window || (delete Ei.animationend.animation, delete Ei.animationiteration.animation, delete Ei.animationstart.animation), "TransitionEvent" in window || delete Ei.transitionend.transition);
  function Ln(e) {
    if (No[e]) return No[e];
    if (!Ei[e]) return e;
    var t = Ei[e], a;
    for (a in t)
      if (t.hasOwnProperty(a) && a in sd)
        return No[e] = t[a];
    return e;
  }
  var rd = Ln("animationend"), od = Ln("animationiteration"), ud = Ln("animationstart"), lg = Ln("transitionrun"), sg = Ln("transitionstart"), rg = Ln("transitioncancel"), cd = Ln("transitionend"), fd = /* @__PURE__ */ new Map(), Mo = "abort auxClick beforeToggle cancel canPlay canPlayThrough click close contextMenu copy cut drag dragEnd dragEnter dragExit dragLeave dragOver dragStart drop durationChange emptied encrypted ended error gotPointerCapture input invalid keyDown keyPress keyUp load loadedData loadedMetadata loadStart lostPointerCapture mouseDown mouseMove mouseOut mouseOver mouseUp paste pause play playing pointerCancel pointerDown pointerMove pointerOut pointerOver pointerUp progress rateChange reset resize seeked seeking stalled submit suspend timeUpdate touchCancel touchEnd touchStart volumeChange scroll toggle touchMove waiting wheel".split(
    " "
  );
  Mo.push("scrollEnd");
  function ea(e, t) {
    fd.set(e, t), Rn(t, [e]);
  }
  var $s = typeof reportError == "function" ? reportError : function(e) {
    if (typeof window == "object" && typeof window.ErrorEvent == "function") {
      var t = new window.ErrorEvent("error", {
        bubbles: !0,
        cancelable: !0,
        message: typeof e == "object" && e !== null && typeof e.message == "string" ? String(e.message) : String(e),
        error: e
      });
      if (!window.dispatchEvent(t)) return;
    } else if (typeof process == "object" && typeof process.emit == "function") {
      process.emit("uncaughtException", e);
      return;
    }
    console.error(e);
  }, Vt = [], Ai = 0, qo = 0;
  function Us() {
    for (var e = Ai, t = qo = Ai = 0; t < e; ) {
      var a = Vt[t];
      Vt[t++] = null;
      var l = Vt[t];
      Vt[t++] = null;
      var o = Vt[t];
      Vt[t++] = null;
      var u = Vt[t];
      if (Vt[t++] = null, l !== null && o !== null) {
        var p = l.pending;
        p === null ? o.next = o : (o.next = p.next, p.next = o), l.pending = o;
      }
      u !== 0 && dd(a, o, u);
    }
  }
  function Ls(e, t, a, l) {
    Vt[Ai++] = e, Vt[Ai++] = t, Vt[Ai++] = a, Vt[Ai++] = l, qo |= l, e.lanes |= l, e = e.alternate, e !== null && (e.lanes |= l);
  }
  function jo(e, t, a, l) {
    return Ls(e, t, a, l), Hs(e);
  }
  function Hn(e, t) {
    return Ls(e, null, null, t), Hs(e);
  }
  function dd(e, t, a) {
    e.lanes |= a;
    var l = e.alternate;
    l !== null && (l.lanes |= a);
    for (var o = !1, u = e.return; u !== null; )
      u.childLanes |= a, l = u.alternate, l !== null && (l.childLanes |= a), u.tag === 22 && (e = u.stateNode, e === null || e._visibility & 1 || (o = !0)), e = u, u = u.return;
    return e.tag === 3 ? (u = e.stateNode, o && t !== null && (o = 31 - rt(a), e = u.hiddenUpdates, l = e[o], l === null ? e[o] = [t] : l.push(t), t.lane = a | 536870912), u) : null;
  }
  function Hs(e) {
    if (50 < Jl)
      throw Jl = 0, Xu = null, Error(r(185));
    for (var t = e.return; t !== null; )
      e = t, t = e.return;
    return e.tag === 3 ? e.stateNode : null;
  }
  var Oi = {};
  function og(e, t, a, l) {
    this.tag = e, this.key = a, this.sibling = this.child = this.return = this.stateNode = this.type = this.elementType = null, this.index = 0, this.refCleanup = this.ref = null, this.pendingProps = t, this.dependencies = this.memoizedState = this.updateQueue = this.memoizedProps = null, this.mode = l, this.subtreeFlags = this.flags = 0, this.deletions = null, this.childLanes = this.lanes = 0, this.alternate = null;
  }
  function Mt(e, t, a, l) {
    return new og(e, t, a, l);
  }
  function Ro(e) {
    return e = e.prototype, !(!e || !e.isReactComponent);
  }
  function Sa(e, t) {
    var a = e.alternate;
    return a === null ? (a = Mt(
      e.tag,
      t,
      e.key,
      e.mode
    ), a.elementType = e.elementType, a.type = e.type, a.stateNode = e.stateNode, a.alternate = e, e.alternate = a) : (a.pendingProps = t, a.type = e.type, a.flags = 0, a.subtreeFlags = 0, a.deletions = null), a.flags = e.flags & 65011712, a.childLanes = e.childLanes, a.lanes = e.lanes, a.child = e.child, a.memoizedProps = e.memoizedProps, a.memoizedState = e.memoizedState, a.updateQueue = e.updateQueue, t = e.dependencies, a.dependencies = t === null ? null : { lanes: t.lanes, firstContext: t.firstContext }, a.sibling = e.sibling, a.index = e.index, a.ref = e.ref, a.refCleanup = e.refCleanup, a;
  }
  function pd(e, t) {
    e.flags &= 65011714;
    var a = e.alternate;
    return a === null ? (e.childLanes = 0, e.lanes = t, e.child = null, e.subtreeFlags = 0, e.memoizedProps = null, e.memoizedState = null, e.updateQueue = null, e.dependencies = null, e.stateNode = null) : (e.childLanes = a.childLanes, e.lanes = a.lanes, e.child = a.child, e.subtreeFlags = 0, e.deletions = null, e.memoizedProps = a.memoizedProps, e.memoizedState = a.memoizedState, e.updateQueue = a.updateQueue, e.type = a.type, t = a.dependencies, e.dependencies = t === null ? null : {
      lanes: t.lanes,
      firstContext: t.firstContext
    }), e;
  }
  function Ys(e, t, a, l, o, u) {
    var p = 0;
    if (l = e, typeof e == "function") Ro(e) && (p = 1);
    else if (typeof e == "string")
      p = pb(
        e,
        a,
        I.current
      ) ? 26 : e === "html" || e === "head" || e === "body" ? 27 : 5;
    else
      e: switch (e) {
        case Te:
          return e = Mt(31, a, t, o), e.elementType = Te, e.lanes = u, e;
        case A:
          return Yn(a.children, o, u, t);
        case z:
          p = 8, o |= 24;
          break;
        case q:
          return e = Mt(12, a, t, o | 2), e.elementType = q, e.lanes = u, e;
        case V:
          return e = Mt(13, a, t, o), e.elementType = V, e.lanes = u, e;
        case J:
          return e = Mt(19, a, t, o), e.elementType = J, e.lanes = u, e;
        default:
          if (typeof e == "object" && e !== null)
            switch (e.$$typeof) {
              case j:
                p = 10;
                break e;
              case $:
                p = 9;
                break e;
              case L:
                p = 11;
                break e;
              case Z:
                p = 14;
                break e;
              case F:
                p = 16, l = null;
                break e;
            }
          p = 29, a = Error(
            r(130, e === null ? "null" : typeof e, "")
          ), l = null;
      }
    return t = Mt(p, a, t, o), t.elementType = e, t.type = l, t.lanes = u, t;
  }
  function Yn(e, t, a, l) {
    return e = Mt(7, e, l, t), e.lanes = a, e;
  }
  function $o(e, t, a) {
    return e = Mt(6, e, null, t), e.lanes = a, e;
  }
  function hd(e) {
    var t = Mt(18, null, null, 0);
    return t.stateNode = e, t;
  }
  function Uo(e, t, a) {
    return t = Mt(
      4,
      e.children !== null ? e.children : [],
      e.key,
      t
    ), t.lanes = a, t.stateNode = {
      containerInfo: e.containerInfo,
      pendingChildren: null,
      implementation: e.implementation
    }, t;
  }
  var md = /* @__PURE__ */ new WeakMap();
  function Xt(e, t) {
    if (typeof e == "object" && e !== null) {
      var a = md.get(e);
      return a !== void 0 ? a : (t = {
        value: e,
        source: t,
        stack: vt(t)
      }, md.set(e, t), t);
    }
    return {
      value: e,
      source: t,
      stack: vt(t)
    };
  }
  var Di = [], Ci = 0, Bs = null, Al = 0, Gt = [], Qt = 0, Wa = null, fa = 1, da = "";
  function za(e, t) {
    Di[Ci++] = Al, Di[Ci++] = Bs, Bs = e, Al = t;
  }
  function vd(e, t, a) {
    Gt[Qt++] = fa, Gt[Qt++] = da, Gt[Qt++] = Wa, Wa = e;
    var l = fa;
    e = da;
    var o = 32 - rt(l) - 1;
    l &= ~(1 << o), a += 1;
    var u = 32 - rt(t) + o;
    if (30 < u) {
      var p = o - o % 5;
      u = (l & (1 << p) - 1).toString(32), l >>= p, o -= p, fa = 1 << 32 - rt(t) + o | a << o | l, da = u + e;
    } else
      fa = 1 << u | a << o | l, da = e;
  }
  function Lo(e) {
    e.return !== null && (za(e, 1), vd(e, 1, 0));
  }
  function Ho(e) {
    for (; e === Bs; )
      Bs = Di[--Ci], Di[Ci] = null, Al = Di[--Ci], Di[Ci] = null;
    for (; e === Wa; )
      Wa = Gt[--Qt], Gt[Qt] = null, da = Gt[--Qt], Gt[Qt] = null, fa = Gt[--Qt], Gt[Qt] = null;
  }
  function gd(e, t) {
    Gt[Qt++] = fa, Gt[Qt++] = da, Gt[Qt++] = Wa, fa = t.id, da = t.overflow, Wa = e;
  }
  var ut = null, qe = null, ge = !1, Ia = null, Zt = !1, Yo = Error(r(519));
  function Pa(e) {
    var t = Error(
      r(
        418,
        1 < arguments.length && arguments[1] !== void 0 && arguments[1] ? "text" : "HTML",
        ""
      )
    );
    throw Ol(Xt(t, e)), Yo;
  }
  function bd(e) {
    var t = e.stateNode, a = e.type, l = e.memoizedProps;
    switch (t[ot] = e, t[wt] = l, a) {
      case "dialog":
        he("cancel", t), he("close", t);
        break;
      case "iframe":
      case "object":
      case "embed":
        he("load", t);
        break;
      case "video":
      case "audio":
        for (a = 0; a < Wl.length; a++)
          he(Wl[a], t);
        break;
      case "source":
        he("error", t);
        break;
      case "img":
      case "image":
      case "link":
        he("error", t), he("load", t);
        break;
      case "details":
        he("toggle", t);
        break;
      case "input":
        he("invalid", t), Cf(
          t,
          l.value,
          l.defaultValue,
          l.checked,
          l.defaultChecked,
          l.type,
          l.name,
          !0
        );
        break;
      case "select":
        he("invalid", t);
        break;
      case "textarea":
        he("invalid", t), Mf(t, l.value, l.defaultValue, l.children);
    }
    a = l.children, typeof a != "string" && typeof a != "number" && typeof a != "bigint" || t.textContent === "" + a || l.suppressHydrationWarning === !0 || jh(t.textContent, a) ? (l.popover != null && (he("beforetoggle", t), he("toggle", t)), l.onScroll != null && he("scroll", t), l.onScrollEnd != null && he("scrollend", t), l.onClick != null && (t.onclick = wa), t = !0) : t = !1, t || Pa(e, !0);
  }
  function yd(e) {
    for (ut = e.return; ut; )
      switch (ut.tag) {
        case 5:
        case 31:
        case 13:
          Zt = !1;
          return;
        case 27:
        case 3:
          Zt = !0;
          return;
        default:
          ut = ut.return;
      }
  }
  function Ni(e) {
    if (e !== ut) return !1;
    if (!ge) return yd(e), ge = !0, !1;
    var t = e.tag, a;
    if ((a = t !== 3 && t !== 27) && ((a = t === 5) && (a = e.type, a = !(a !== "form" && a !== "button") || lc(e.type, e.memoizedProps)), a = !a), a && qe && Pa(e), yd(e), t === 13) {
      if (e = e.memoizedState, e = e !== null ? e.dehydrated : null, !e) throw Error(r(317));
      qe = Xh(e);
    } else if (t === 31) {
      if (e = e.memoizedState, e = e !== null ? e.dehydrated : null, !e) throw Error(r(317));
      qe = Xh(e);
    } else
      t === 27 ? (t = qe, hn(e.type) ? (e = cc, cc = null, qe = e) : qe = t) : qe = ut ? Jt(e.stateNode.nextSibling) : null;
    return !0;
  }
  function Bn() {
    qe = ut = null, ge = !1;
  }
  function Bo() {
    var e = Ia;
    return e !== null && (Et === null ? Et = e : Et.push.apply(
      Et,
      e
    ), Ia = null), e;
  }
  function Ol(e) {
    Ia === null ? Ia = [e] : Ia.push(e);
  }
  var Vo = k(null), Vn = null, Ta = null;
  function en(e, t, a) {
    K(Vo, t._currentValue), t._currentValue = a;
  }
  function Ea(e) {
    e._currentValue = Vo.current, Y(Vo);
  }
  function Xo(e, t, a) {
    for (; e !== null; ) {
      var l = e.alternate;
      if ((e.childLanes & t) !== t ? (e.childLanes |= t, l !== null && (l.childLanes |= t)) : l !== null && (l.childLanes & t) !== t && (l.childLanes |= t), e === a) break;
      e = e.return;
    }
  }
  function Go(e, t, a, l) {
    var o = e.child;
    for (o !== null && (o.return = e); o !== null; ) {
      var u = o.dependencies;
      if (u !== null) {
        var p = o.child;
        u = u.firstContext;
        e: for (; u !== null; ) {
          var b = u;
          u = o;
          for (var w = 0; w < t.length; w++)
            if (b.context === t[w]) {
              u.lanes |= a, b = u.alternate, b !== null && (b.lanes |= a), Xo(
                u.return,
                a,
                e
              ), l || (p = null);
              break e;
            }
          u = b.next;
        }
      } else if (o.tag === 18) {
        if (p = o.return, p === null) throw Error(r(341));
        p.lanes |= a, u = p.alternate, u !== null && (u.lanes |= a), Xo(p, a, e), p = null;
      } else p = o.child;
      if (p !== null) p.return = o;
      else
        for (p = o; p !== null; ) {
          if (p === e) {
            p = null;
            break;
          }
          if (o = p.sibling, o !== null) {
            o.return = p.return, p = o;
            break;
          }
          p = p.return;
        }
      o = p;
    }
  }
  function Mi(e, t, a, l) {
    e = null;
    for (var o = t, u = !1; o !== null; ) {
      if (!u) {
        if ((o.flags & 524288) !== 0) u = !0;
        else if ((o.flags & 262144) !== 0) break;
      }
      if (o.tag === 10) {
        var p = o.alternate;
        if (p === null) throw Error(r(387));
        if (p = p.memoizedProps, p !== null) {
          var b = o.type;
          Nt(o.pendingProps.value, p.value) || (e !== null ? e.push(b) : e = [b]);
        }
      } else if (o === xe.current) {
        if (p = o.alternate, p === null) throw Error(r(387));
        p.memoizedState.memoizedState !== o.memoizedState.memoizedState && (e !== null ? e.push(as) : e = [as]);
      }
      o = o.return;
    }
    e !== null && Go(
      t,
      e,
      a,
      l
    ), t.flags |= 262144;
  }
  function Vs(e) {
    for (e = e.firstContext; e !== null; ) {
      if (!Nt(
        e.context._currentValue,
        e.memoizedValue
      ))
        return !0;
      e = e.next;
    }
    return !1;
  }
  function Xn(e) {
    Vn = e, Ta = null, e = e.dependencies, e !== null && (e.firstContext = null);
  }
  function ct(e) {
    return _d(Vn, e);
  }
  function Xs(e, t) {
    return Vn === null && Xn(e), _d(e, t);
  }
  function _d(e, t) {
    var a = t._currentValue;
    if (t = { context: t, memoizedValue: a, next: null }, Ta === null) {
      if (e === null) throw Error(r(308));
      Ta = t, e.dependencies = { lanes: 0, firstContext: t }, e.flags |= 524288;
    } else Ta = Ta.next = t;
    return a;
  }
  var ug = typeof AbortController < "u" ? AbortController : function() {
    var e = [], t = this.signal = {
      aborted: !1,
      addEventListener: function(a, l) {
        e.push(l);
      }
    };
    this.abort = function() {
      t.aborted = !0, e.forEach(function(a) {
        return a();
      });
    };
  }, cg = n.unstable_scheduleCallback, fg = n.unstable_NormalPriority, We = {
    $$typeof: j,
    Consumer: null,
    Provider: null,
    _currentValue: null,
    _currentValue2: null,
    _threadCount: 0
  };
  function Qo() {
    return {
      controller: new ug(),
      data: /* @__PURE__ */ new Map(),
      refCount: 0
    };
  }
  function Dl(e) {
    e.refCount--, e.refCount === 0 && cg(fg, function() {
      e.controller.abort();
    });
  }
  var Cl = null, Zo = 0, qi = 0, ji = null;
  function dg(e, t) {
    if (Cl === null) {
      var a = Cl = [];
      Zo = 0, qi = Fu(), ji = {
        status: "pending",
        value: void 0,
        then: function(l) {
          a.push(l);
        }
      };
    }
    return Zo++, t.then(xd, xd), t;
  }
  function xd() {
    if (--Zo === 0 && Cl !== null) {
      ji !== null && (ji.status = "fulfilled");
      var e = Cl;
      Cl = null, qi = 0, ji = null;
      for (var t = 0; t < e.length; t++) (0, e[t])();
    }
  }
  function pg(e, t) {
    var a = [], l = {
      status: "pending",
      value: null,
      reason: null,
      then: function(o) {
        a.push(o);
      }
    };
    return e.then(
      function() {
        l.status = "fulfilled", l.value = t;
        for (var o = 0; o < a.length; o++) (0, a[o])(t);
      },
      function(o) {
        for (l.status = "rejected", l.reason = o, o = 0; o < a.length; o++)
          (0, a[o])(void 0);
      }
    ), l;
  }
  var wd = R.S;
  R.S = function(e, t) {
    lh = Me(), typeof t == "object" && t !== null && typeof t.then == "function" && dg(e, t), wd !== null && wd(e, t);
  };
  var Gn = k(null);
  function Ko() {
    var e = Gn.current;
    return e !== null ? e : Ne.pooledCache;
  }
  function Gs(e, t) {
    t === null ? K(Gn, Gn.current) : K(Gn, t.pool);
  }
  function kd() {
    var e = Ko();
    return e === null ? null : { parent: We._currentValue, pool: e };
  }
  var Ri = Error(r(460)), Jo = Error(r(474)), Qs = Error(r(542)), Zs = { then: function() {
  } };
  function Sd(e) {
    return e = e.status, e === "fulfilled" || e === "rejected";
  }
  function zd(e, t, a) {
    switch (a = e[a], a === void 0 ? e.push(t) : a !== t && (t.then(wa, wa), t = a), t.status) {
      case "fulfilled":
        return t.value;
      case "rejected":
        throw e = t.reason, Ed(e), e;
      default:
        if (typeof t.status == "string") t.then(wa, wa);
        else {
          if (e = Ne, e !== null && 100 < e.shellSuspendCounter)
            throw Error(r(482));
          e = t, e.status = "pending", e.then(
            function(l) {
              if (t.status === "pending") {
                var o = t;
                o.status = "fulfilled", o.value = l;
              }
            },
            function(l) {
              if (t.status === "pending") {
                var o = t;
                o.status = "rejected", o.reason = l;
              }
            }
          );
        }
        switch (t.status) {
          case "fulfilled":
            return t.value;
          case "rejected":
            throw e = t.reason, Ed(e), e;
        }
        throw Zn = t, Ri;
    }
  }
  function Qn(e) {
    try {
      var t = e._init;
      return t(e._payload);
    } catch (a) {
      throw a !== null && typeof a == "object" && typeof a.then == "function" ? (Zn = a, Ri) : a;
    }
  }
  var Zn = null;
  function Td() {
    if (Zn === null) throw Error(r(459));
    var e = Zn;
    return Zn = null, e;
  }
  function Ed(e) {
    if (e === Ri || e === Qs)
      throw Error(r(483));
  }
  var $i = null, Nl = 0;
  function Ks(e) {
    var t = Nl;
    return Nl += 1, $i === null && ($i = []), zd($i, e, t);
  }
  function Ml(e, t) {
    t = t.props.ref, e.ref = t !== void 0 ? t : null;
  }
  function Js(e, t) {
    throw t.$$typeof === _ ? Error(r(525)) : (e = Object.prototype.toString.call(t), Error(
      r(
        31,
        e === "[object Object]" ? "object with keys {" + Object.keys(t).join(", ") + "}" : e
      )
    ));
  }
  function Ad(e) {
    function t(E, S) {
      if (e) {
        var O = E.deletions;
        O === null ? (E.deletions = [S], E.flags |= 16) : O.push(S);
      }
    }
    function a(E, S) {
      if (!e) return null;
      for (; S !== null; )
        t(E, S), S = S.sibling;
      return null;
    }
    function l(E) {
      for (var S = /* @__PURE__ */ new Map(); E !== null; )
        E.key !== null ? S.set(E.key, E) : S.set(E.index, E), E = E.sibling;
      return S;
    }
    function o(E, S) {
      return E = Sa(E, S), E.index = 0, E.sibling = null, E;
    }
    function u(E, S, O) {
      return E.index = O, e ? (O = E.alternate, O !== null ? (O = O.index, O < S ? (E.flags |= 67108866, S) : O) : (E.flags |= 67108866, S)) : (E.flags |= 1048576, S);
    }
    function p(E) {
      return e && E.alternate === null && (E.flags |= 67108866), E;
    }
    function b(E, S, O, H) {
      return S === null || S.tag !== 6 ? (S = $o(O, E.mode, H), S.return = E, S) : (S = o(S, O), S.return = E, S);
    }
    function w(E, S, O, H) {
      var ne = O.type;
      return ne === A ? U(
        E,
        S,
        O.props.children,
        H,
        O.key
      ) : S !== null && (S.elementType === ne || typeof ne == "object" && ne !== null && ne.$$typeof === F && Qn(ne) === S.type) ? (S = o(S, O.props), Ml(S, O), S.return = E, S) : (S = Ys(
        O.type,
        O.key,
        O.props,
        null,
        E.mode,
        H
      ), Ml(S, O), S.return = E, S);
    }
    function D(E, S, O, H) {
      return S === null || S.tag !== 4 || S.stateNode.containerInfo !== O.containerInfo || S.stateNode.implementation !== O.implementation ? (S = Uo(O, E.mode, H), S.return = E, S) : (S = o(S, O.children || []), S.return = E, S);
    }
    function U(E, S, O, H, ne) {
      return S === null || S.tag !== 7 ? (S = Yn(
        O,
        E.mode,
        H,
        ne
      ), S.return = E, S) : (S = o(S, O), S.return = E, S);
    }
    function B(E, S, O) {
      if (typeof S == "string" && S !== "" || typeof S == "number" || typeof S == "bigint")
        return S = $o(
          "" + S,
          E.mode,
          O
        ), S.return = E, S;
      if (typeof S == "object" && S !== null) {
        switch (S.$$typeof) {
          case x:
            return O = Ys(
              S.type,
              S.key,
              S.props,
              null,
              E.mode,
              O
            ), Ml(O, S), O.return = E, O;
          case T:
            return S = Uo(
              S,
              E.mode,
              O
            ), S.return = E, S;
          case F:
            return S = Qn(S), B(E, S, O);
        }
        if (Ke(S) || Ye(S))
          return S = Yn(
            S,
            E.mode,
            O,
            null
          ), S.return = E, S;
        if (typeof S.then == "function")
          return B(E, Ks(S), O);
        if (S.$$typeof === j)
          return B(
            E,
            Xs(E, S),
            O
          );
        Js(E, S);
      }
      return null;
    }
    function C(E, S, O, H) {
      var ne = S !== null ? S.key : null;
      if (typeof O == "string" && O !== "" || typeof O == "number" || typeof O == "bigint")
        return ne !== null ? null : b(E, S, "" + O, H);
      if (typeof O == "object" && O !== null) {
        switch (O.$$typeof) {
          case x:
            return O.key === ne ? w(E, S, O, H) : null;
          case T:
            return O.key === ne ? D(E, S, O, H) : null;
          case F:
            return O = Qn(O), C(E, S, O, H);
        }
        if (Ke(O) || Ye(O))
          return ne !== null ? null : U(E, S, O, H, null);
        if (typeof O.then == "function")
          return C(
            E,
            S,
            Ks(O),
            H
          );
        if (O.$$typeof === j)
          return C(
            E,
            S,
            Xs(E, O),
            H
          );
        Js(E, O);
      }
      return null;
    }
    function M(E, S, O, H, ne) {
      if (typeof H == "string" && H !== "" || typeof H == "number" || typeof H == "bigint")
        return E = E.get(O) || null, b(S, E, "" + H, ne);
      if (typeof H == "object" && H !== null) {
        switch (H.$$typeof) {
          case x:
            return E = E.get(
              H.key === null ? O : H.key
            ) || null, w(S, E, H, ne);
          case T:
            return E = E.get(
              H.key === null ? O : H.key
            ) || null, D(S, E, H, ne);
          case F:
            return H = Qn(H), M(
              E,
              S,
              O,
              H,
              ne
            );
        }
        if (Ke(H) || Ye(H))
          return E = E.get(O) || null, U(S, E, H, ne, null);
        if (typeof H.then == "function")
          return M(
            E,
            S,
            O,
            Ks(H),
            ne
          );
        if (H.$$typeof === j)
          return M(
            E,
            S,
            O,
            Xs(S, H),
            ne
          );
        Js(S, H);
      }
      return null;
    }
    function P(E, S, O, H) {
      for (var ne = null, we = null, te = S, de = S = 0, ve = null; te !== null && de < O.length; de++) {
        te.index > de ? (ve = te, te = null) : ve = te.sibling;
        var ke = C(
          E,
          te,
          O[de],
          H
        );
        if (ke === null) {
          te === null && (te = ve);
          break;
        }
        e && te && ke.alternate === null && t(E, te), S = u(ke, S, de), we === null ? ne = ke : we.sibling = ke, we = ke, te = ve;
      }
      if (de === O.length)
        return a(E, te), ge && za(E, de), ne;
      if (te === null) {
        for (; de < O.length; de++)
          te = B(E, O[de], H), te !== null && (S = u(
            te,
            S,
            de
          ), we === null ? ne = te : we.sibling = te, we = te);
        return ge && za(E, de), ne;
      }
      for (te = l(te); de < O.length; de++)
        ve = M(
          te,
          E,
          de,
          O[de],
          H
        ), ve !== null && (e && ve.alternate !== null && te.delete(
          ve.key === null ? de : ve.key
        ), S = u(
          ve,
          S,
          de
        ), we === null ? ne = ve : we.sibling = ve, we = ve);
      return e && te.forEach(function(yn) {
        return t(E, yn);
      }), ge && za(E, de), ne;
    }
    function ie(E, S, O, H) {
      if (O == null) throw Error(r(151));
      for (var ne = null, we = null, te = S, de = S = 0, ve = null, ke = O.next(); te !== null && !ke.done; de++, ke = O.next()) {
        te.index > de ? (ve = te, te = null) : ve = te.sibling;
        var yn = C(E, te, ke.value, H);
        if (yn === null) {
          te === null && (te = ve);
          break;
        }
        e && te && yn.alternate === null && t(E, te), S = u(yn, S, de), we === null ? ne = yn : we.sibling = yn, we = yn, te = ve;
      }
      if (ke.done)
        return a(E, te), ge && za(E, de), ne;
      if (te === null) {
        for (; !ke.done; de++, ke = O.next())
          ke = B(E, ke.value, H), ke !== null && (S = u(ke, S, de), we === null ? ne = ke : we.sibling = ke, we = ke);
        return ge && za(E, de), ne;
      }
      for (te = l(te); !ke.done; de++, ke = O.next())
        ke = M(te, E, de, ke.value, H), ke !== null && (e && ke.alternate !== null && te.delete(ke.key === null ? de : ke.key), S = u(ke, S, de), we === null ? ne = ke : we.sibling = ke, we = ke);
      return e && te.forEach(function(Sb) {
        return t(E, Sb);
      }), ge && za(E, de), ne;
    }
    function Ce(E, S, O, H) {
      if (typeof O == "object" && O !== null && O.type === A && O.key === null && (O = O.props.children), typeof O == "object" && O !== null) {
        switch (O.$$typeof) {
          case x:
            e: {
              for (var ne = O.key; S !== null; ) {
                if (S.key === ne) {
                  if (ne = O.type, ne === A) {
                    if (S.tag === 7) {
                      a(
                        E,
                        S.sibling
                      ), H = o(
                        S,
                        O.props.children
                      ), H.return = E, E = H;
                      break e;
                    }
                  } else if (S.elementType === ne || typeof ne == "object" && ne !== null && ne.$$typeof === F && Qn(ne) === S.type) {
                    a(
                      E,
                      S.sibling
                    ), H = o(S, O.props), Ml(H, O), H.return = E, E = H;
                    break e;
                  }
                  a(E, S);
                  break;
                } else t(E, S);
                S = S.sibling;
              }
              O.type === A ? (H = Yn(
                O.props.children,
                E.mode,
                H,
                O.key
              ), H.return = E, E = H) : (H = Ys(
                O.type,
                O.key,
                O.props,
                null,
                E.mode,
                H
              ), Ml(H, O), H.return = E, E = H);
            }
            return p(E);
          case T:
            e: {
              for (ne = O.key; S !== null; ) {
                if (S.key === ne)
                  if (S.tag === 4 && S.stateNode.containerInfo === O.containerInfo && S.stateNode.implementation === O.implementation) {
                    a(
                      E,
                      S.sibling
                    ), H = o(S, O.children || []), H.return = E, E = H;
                    break e;
                  } else {
                    a(E, S);
                    break;
                  }
                else t(E, S);
                S = S.sibling;
              }
              H = Uo(O, E.mode, H), H.return = E, E = H;
            }
            return p(E);
          case F:
            return O = Qn(O), Ce(
              E,
              S,
              O,
              H
            );
        }
        if (Ke(O))
          return P(
            E,
            S,
            O,
            H
          );
        if (Ye(O)) {
          if (ne = Ye(O), typeof ne != "function") throw Error(r(150));
          return O = ne.call(O), ie(
            E,
            S,
            O,
            H
          );
        }
        if (typeof O.then == "function")
          return Ce(
            E,
            S,
            Ks(O),
            H
          );
        if (O.$$typeof === j)
          return Ce(
            E,
            S,
            Xs(E, O),
            H
          );
        Js(E, O);
      }
      return typeof O == "string" && O !== "" || typeof O == "number" || typeof O == "bigint" ? (O = "" + O, S !== null && S.tag === 6 ? (a(E, S.sibling), H = o(S, O), H.return = E, E = H) : (a(E, S), H = $o(O, E.mode, H), H.return = E, E = H), p(E)) : a(E, S);
    }
    return function(E, S, O, H) {
      try {
        Nl = 0;
        var ne = Ce(
          E,
          S,
          O,
          H
        );
        return $i = null, ne;
      } catch (te) {
        if (te === Ri || te === Qs) throw te;
        var we = Mt(29, te, null, E.mode);
        return we.lanes = H, we.return = E, we;
      } finally {
      }
    };
  }
  var Kn = Ad(!0), Od = Ad(!1), tn = !1;
  function Fo(e) {
    e.updateQueue = {
      baseState: e.memoizedState,
      firstBaseUpdate: null,
      lastBaseUpdate: null,
      shared: { pending: null, lanes: 0, hiddenCallbacks: null },
      callbacks: null
    };
  }
  function Wo(e, t) {
    e = e.updateQueue, t.updateQueue === e && (t.updateQueue = {
      baseState: e.baseState,
      firstBaseUpdate: e.firstBaseUpdate,
      lastBaseUpdate: e.lastBaseUpdate,
      shared: e.shared,
      callbacks: null
    });
  }
  function an(e) {
    return { lane: e, tag: 0, payload: null, callback: null, next: null };
  }
  function nn(e, t, a) {
    var l = e.updateQueue;
    if (l === null) return null;
    if (l = l.shared, (ze & 2) !== 0) {
      var o = l.pending;
      return o === null ? t.next = t : (t.next = o.next, o.next = t), l.pending = t, t = Hs(e), dd(e, null, a), t;
    }
    return Ls(e, l, t, a), Hs(e);
  }
  function ql(e, t, a) {
    if (t = t.updateQueue, t !== null && (t = t.shared, (a & 4194048) !== 0)) {
      var l = t.lanes;
      l &= e.pendingLanes, a |= l, t.lanes = a, Re(e, a);
    }
  }
  function Io(e, t) {
    var a = e.updateQueue, l = e.alternate;
    if (l !== null && (l = l.updateQueue, a === l)) {
      var o = null, u = null;
      if (a = a.firstBaseUpdate, a !== null) {
        do {
          var p = {
            lane: a.lane,
            tag: a.tag,
            payload: a.payload,
            callback: null,
            next: null
          };
          u === null ? o = u = p : u = u.next = p, a = a.next;
        } while (a !== null);
        u === null ? o = u = t : u = u.next = t;
      } else o = u = t;
      a = {
        baseState: l.baseState,
        firstBaseUpdate: o,
        lastBaseUpdate: u,
        shared: l.shared,
        callbacks: l.callbacks
      }, e.updateQueue = a;
      return;
    }
    e = a.lastBaseUpdate, e === null ? a.firstBaseUpdate = t : e.next = t, a.lastBaseUpdate = t;
  }
  var Po = !1;
  function jl() {
    if (Po) {
      var e = ji;
      if (e !== null) throw e;
    }
  }
  function Rl(e, t, a, l) {
    Po = !1;
    var o = e.updateQueue;
    tn = !1;
    var u = o.firstBaseUpdate, p = o.lastBaseUpdate, b = o.shared.pending;
    if (b !== null) {
      o.shared.pending = null;
      var w = b, D = w.next;
      w.next = null, p === null ? u = D : p.next = D, p = w;
      var U = e.alternate;
      U !== null && (U = U.updateQueue, b = U.lastBaseUpdate, b !== p && (b === null ? U.firstBaseUpdate = D : b.next = D, U.lastBaseUpdate = w));
    }
    if (u !== null) {
      var B = o.baseState;
      p = 0, U = D = w = null, b = u;
      do {
        var C = b.lane & -536870913, M = C !== b.lane;
        if (M ? (me & C) === C : (l & C) === C) {
          C !== 0 && C === qi && (Po = !0), U !== null && (U = U.next = {
            lane: 0,
            tag: b.tag,
            payload: b.payload,
            callback: null,
            next: null
          });
          e: {
            var P = e, ie = b;
            C = t;
            var Ce = a;
            switch (ie.tag) {
              case 1:
                if (P = ie.payload, typeof P == "function") {
                  B = P.call(Ce, B, C);
                  break e;
                }
                B = P;
                break e;
              case 3:
                P.flags = P.flags & -65537 | 128;
              case 0:
                if (P = ie.payload, C = typeof P == "function" ? P.call(Ce, B, C) : P, C == null) break e;
                B = m({}, B, C);
                break e;
              case 2:
                tn = !0;
            }
          }
          C = b.callback, C !== null && (e.flags |= 64, M && (e.flags |= 8192), M = o.callbacks, M === null ? o.callbacks = [C] : M.push(C));
        } else
          M = {
            lane: C,
            tag: b.tag,
            payload: b.payload,
            callback: b.callback,
            next: null
          }, U === null ? (D = U = M, w = B) : U = U.next = M, p |= C;
        if (b = b.next, b === null) {
          if (b = o.shared.pending, b === null)
            break;
          M = b, b = M.next, M.next = null, o.lastBaseUpdate = M, o.shared.pending = null;
        }
      } while (!0);
      U === null && (w = B), o.baseState = w, o.firstBaseUpdate = D, o.lastBaseUpdate = U, u === null && (o.shared.lanes = 0), un |= p, e.lanes = p, e.memoizedState = B;
    }
  }
  function Dd(e, t) {
    if (typeof e != "function")
      throw Error(r(191, e));
    e.call(t);
  }
  function Cd(e, t) {
    var a = e.callbacks;
    if (a !== null)
      for (e.callbacks = null, e = 0; e < a.length; e++)
        Dd(a[e], t);
  }
  var Ui = k(null), Fs = k(0);
  function Nd(e, t) {
    e = Ra, K(Fs, e), K(Ui, t), Ra = e | t.baseLanes;
  }
  function eu() {
    K(Fs, Ra), K(Ui, Ui.current);
  }
  function tu() {
    Ra = Fs.current, Y(Ui), Y(Fs);
  }
  var qt = k(null), Kt = null;
  function ln(e) {
    var t = e.alternate;
    K(Ge, Ge.current & 1), K(qt, e), Kt === null && (t === null || Ui.current !== null || t.memoizedState !== null) && (Kt = e);
  }
  function au(e) {
    K(Ge, Ge.current), K(qt, e), Kt === null && (Kt = e);
  }
  function Md(e) {
    e.tag === 22 ? (K(Ge, Ge.current), K(qt, e), Kt === null && (Kt = e)) : sn();
  }
  function sn() {
    K(Ge, Ge.current), K(qt, qt.current);
  }
  function jt(e) {
    Y(qt), Kt === e && (Kt = null), Y(Ge);
  }
  var Ge = k(0);
  function Ws(e) {
    for (var t = e; t !== null; ) {
      if (t.tag === 13) {
        var a = t.memoizedState;
        if (a !== null && (a = a.dehydrated, a === null || oc(a) || uc(a)))
          return t;
      } else if (t.tag === 19 && (t.memoizedProps.revealOrder === "forwards" || t.memoizedProps.revealOrder === "backwards" || t.memoizedProps.revealOrder === "unstable_legacy-backwards" || t.memoizedProps.revealOrder === "together")) {
        if ((t.flags & 128) !== 0) return t;
      } else if (t.child !== null) {
        t.child.return = t, t = t.child;
        continue;
      }
      if (t === e) break;
      for (; t.sibling === null; ) {
        if (t.return === null || t.return === e) return null;
        t = t.return;
      }
      t.sibling.return = t.return, t = t.sibling;
    }
    return null;
  }
  var Aa = 0, ce = null, Oe = null, Ie = null, Is = !1, Li = !1, Jn = !1, Ps = 0, $l = 0, Hi = null, hg = 0;
  function Be() {
    throw Error(r(321));
  }
  function nu(e, t) {
    if (t === null) return !1;
    for (var a = 0; a < t.length && a < e.length; a++)
      if (!Nt(e[a], t[a])) return !1;
    return !0;
  }
  function iu(e, t, a, l, o, u) {
    return Aa = u, ce = t, t.memoizedState = null, t.updateQueue = null, t.lanes = 0, R.H = e === null || e.memoizedState === null ? vp : yu, Jn = !1, u = a(l, o), Jn = !1, Li && (u = jd(
      t,
      a,
      l,
      o
    )), qd(e), u;
  }
  function qd(e) {
    R.H = Hl;
    var t = Oe !== null && Oe.next !== null;
    if (Aa = 0, Ie = Oe = ce = null, Is = !1, $l = 0, Hi = null, t) throw Error(r(300));
    e === null || Pe || (e = e.dependencies, e !== null && Vs(e) && (Pe = !0));
  }
  function jd(e, t, a, l) {
    ce = e;
    var o = 0;
    do {
      if (Li && (Hi = null), $l = 0, Li = !1, 25 <= o) throw Error(r(301));
      if (o += 1, Ie = Oe = null, e.updateQueue != null) {
        var u = e.updateQueue;
        u.lastEffect = null, u.events = null, u.stores = null, u.memoCache != null && (u.memoCache.index = 0);
      }
      R.H = gp, u = t(a, l);
    } while (Li);
    return u;
  }
  function mg() {
    var e = R.H, t = e.useState()[0];
    return t = typeof t.then == "function" ? Ul(t) : t, e = e.useState()[0], (Oe !== null ? Oe.memoizedState : null) !== e && (ce.flags |= 1024), t;
  }
  function lu() {
    var e = Ps !== 0;
    return Ps = 0, e;
  }
  function su(e, t, a) {
    t.updateQueue = e.updateQueue, t.flags &= -2053, e.lanes &= ~a;
  }
  function ru(e) {
    if (Is) {
      for (e = e.memoizedState; e !== null; ) {
        var t = e.queue;
        t !== null && (t.pending = null), e = e.next;
      }
      Is = !1;
    }
    Aa = 0, Ie = Oe = ce = null, Li = !1, $l = Ps = 0, Hi = null;
  }
  function _t() {
    var e = {
      memoizedState: null,
      baseState: null,
      baseQueue: null,
      queue: null,
      next: null
    };
    return Ie === null ? ce.memoizedState = Ie = e : Ie = Ie.next = e, Ie;
  }
  function Qe() {
    if (Oe === null) {
      var e = ce.alternate;
      e = e !== null ? e.memoizedState : null;
    } else e = Oe.next;
    var t = Ie === null ? ce.memoizedState : Ie.next;
    if (t !== null)
      Ie = t, Oe = e;
    else {
      if (e === null)
        throw ce.alternate === null ? Error(r(467)) : Error(r(310));
      Oe = e, e = {
        memoizedState: Oe.memoizedState,
        baseState: Oe.baseState,
        baseQueue: Oe.baseQueue,
        queue: Oe.queue,
        next: null
      }, Ie === null ? ce.memoizedState = Ie = e : Ie = Ie.next = e;
    }
    return Ie;
  }
  function er() {
    return { lastEffect: null, events: null, stores: null, memoCache: null };
  }
  function Ul(e) {
    var t = $l;
    return $l += 1, Hi === null && (Hi = []), e = zd(Hi, e, t), t = ce, (Ie === null ? t.memoizedState : Ie.next) === null && (t = t.alternate, R.H = t === null || t.memoizedState === null ? vp : yu), e;
  }
  function tr(e) {
    if (e !== null && typeof e == "object") {
      if (typeof e.then == "function") return Ul(e);
      if (e.$$typeof === j) return ct(e);
    }
    throw Error(r(438, String(e)));
  }
  function ou(e) {
    var t = null, a = ce.updateQueue;
    if (a !== null && (t = a.memoCache), t == null) {
      var l = ce.alternate;
      l !== null && (l = l.updateQueue, l !== null && (l = l.memoCache, l != null && (t = {
        data: l.data.map(function(o) {
          return o.slice();
        }),
        index: 0
      })));
    }
    if (t == null && (t = { data: [], index: 0 }), a === null && (a = er(), ce.updateQueue = a), a.memoCache = t, a = t.data[t.index], a === void 0)
      for (a = t.data[t.index] = Array(e), l = 0; l < e; l++)
        a[l] = Se;
    return t.index++, a;
  }
  function Oa(e, t) {
    return typeof t == "function" ? t(e) : t;
  }
  function ar(e) {
    var t = Qe();
    return uu(t, Oe, e);
  }
  function uu(e, t, a) {
    var l = e.queue;
    if (l === null) throw Error(r(311));
    l.lastRenderedReducer = a;
    var o = e.baseQueue, u = l.pending;
    if (u !== null) {
      if (o !== null) {
        var p = o.next;
        o.next = u.next, u.next = p;
      }
      t.baseQueue = o = u, l.pending = null;
    }
    if (u = e.baseState, o === null) e.memoizedState = u;
    else {
      t = o.next;
      var b = p = null, w = null, D = t, U = !1;
      do {
        var B = D.lane & -536870913;
        if (B !== D.lane ? (me & B) === B : (Aa & B) === B) {
          var C = D.revertLane;
          if (C === 0)
            w !== null && (w = w.next = {
              lane: 0,
              revertLane: 0,
              gesture: null,
              action: D.action,
              hasEagerState: D.hasEagerState,
              eagerState: D.eagerState,
              next: null
            }), B === qi && (U = !0);
          else if ((Aa & C) === C) {
            D = D.next, C === qi && (U = !0);
            continue;
          } else
            B = {
              lane: 0,
              revertLane: D.revertLane,
              gesture: null,
              action: D.action,
              hasEagerState: D.hasEagerState,
              eagerState: D.eagerState,
              next: null
            }, w === null ? (b = w = B, p = u) : w = w.next = B, ce.lanes |= C, un |= C;
          B = D.action, Jn && a(u, B), u = D.hasEagerState ? D.eagerState : a(u, B);
        } else
          C = {
            lane: B,
            revertLane: D.revertLane,
            gesture: D.gesture,
            action: D.action,
            hasEagerState: D.hasEagerState,
            eagerState: D.eagerState,
            next: null
          }, w === null ? (b = w = C, p = u) : w = w.next = C, ce.lanes |= B, un |= B;
        D = D.next;
      } while (D !== null && D !== t);
      if (w === null ? p = u : w.next = b, !Nt(u, e.memoizedState) && (Pe = !0, U && (a = ji, a !== null)))
        throw a;
      e.memoizedState = u, e.baseState = p, e.baseQueue = w, l.lastRenderedState = u;
    }
    return o === null && (l.lanes = 0), [e.memoizedState, l.dispatch];
  }
  function cu(e) {
    var t = Qe(), a = t.queue;
    if (a === null) throw Error(r(311));
    a.lastRenderedReducer = e;
    var l = a.dispatch, o = a.pending, u = t.memoizedState;
    if (o !== null) {
      a.pending = null;
      var p = o = o.next;
      do
        u = e(u, p.action), p = p.next;
      while (p !== o);
      Nt(u, t.memoizedState) || (Pe = !0), t.memoizedState = u, t.baseQueue === null && (t.baseState = u), a.lastRenderedState = u;
    }
    return [u, l];
  }
  function Rd(e, t, a) {
    var l = ce, o = Qe(), u = ge;
    if (u) {
      if (a === void 0) throw Error(r(407));
      a = a();
    } else a = t();
    var p = !Nt(
      (Oe || o).memoizedState,
      a
    );
    if (p && (o.memoizedState = a, Pe = !0), o = o.queue, pu(Ld.bind(null, l, o, e), [
      e
    ]), o.getSnapshot !== t || p || Ie !== null && Ie.memoizedState.tag & 1) {
      if (l.flags |= 2048, Yi(
        9,
        { destroy: void 0 },
        Ud.bind(
          null,
          l,
          o,
          a,
          t
        ),
        null
      ), Ne === null) throw Error(r(349));
      u || (Aa & 127) !== 0 || $d(l, t, a);
    }
    return a;
  }
  function $d(e, t, a) {
    e.flags |= 16384, e = { getSnapshot: t, value: a }, t = ce.updateQueue, t === null ? (t = er(), ce.updateQueue = t, t.stores = [e]) : (a = t.stores, a === null ? t.stores = [e] : a.push(e));
  }
  function Ud(e, t, a, l) {
    t.value = a, t.getSnapshot = l, Hd(t) && Yd(e);
  }
  function Ld(e, t, a) {
    return a(function() {
      Hd(t) && Yd(e);
    });
  }
  function Hd(e) {
    var t = e.getSnapshot;
    e = e.value;
    try {
      var a = t();
      return !Nt(e, a);
    } catch {
      return !0;
    }
  }
  function Yd(e) {
    var t = Hn(e, 2);
    t !== null && At(t, e, 2);
  }
  function fu(e) {
    var t = _t();
    if (typeof e == "function") {
      var a = e;
      if (e = a(), Jn) {
        oa(!0);
        try {
          a();
        } finally {
          oa(!1);
        }
      }
    }
    return t.memoizedState = t.baseState = e, t.queue = {
      pending: null,
      lanes: 0,
      dispatch: null,
      lastRenderedReducer: Oa,
      lastRenderedState: e
    }, t;
  }
  function Bd(e, t, a, l) {
    return e.baseState = a, uu(
      e,
      Oe,
      typeof l == "function" ? l : Oa
    );
  }
  function vg(e, t, a, l, o) {
    if (lr(e)) throw Error(r(485));
    if (e = t.action, e !== null) {
      var u = {
        payload: o,
        action: e,
        next: null,
        isTransition: !0,
        status: "pending",
        value: null,
        reason: null,
        listeners: [],
        then: function(p) {
          u.listeners.push(p);
        }
      };
      R.T !== null ? a(!0) : u.isTransition = !1, l(u), a = t.pending, a === null ? (u.next = t.pending = u, Vd(t, u)) : (u.next = a.next, t.pending = a.next = u);
    }
  }
  function Vd(e, t) {
    var a = t.action, l = t.payload, o = e.state;
    if (t.isTransition) {
      var u = R.T, p = {};
      R.T = p;
      try {
        var b = a(o, l), w = R.S;
        w !== null && w(p, b), Xd(e, t, b);
      } catch (D) {
        du(e, t, D);
      } finally {
        u !== null && p.types !== null && (u.types = p.types), R.T = u;
      }
    } else
      try {
        u = a(o, l), Xd(e, t, u);
      } catch (D) {
        du(e, t, D);
      }
  }
  function Xd(e, t, a) {
    a !== null && typeof a == "object" && typeof a.then == "function" ? a.then(
      function(l) {
        Gd(e, t, l);
      },
      function(l) {
        return du(e, t, l);
      }
    ) : Gd(e, t, a);
  }
  function Gd(e, t, a) {
    t.status = "fulfilled", t.value = a, Qd(t), e.state = a, t = e.pending, t !== null && (a = t.next, a === t ? e.pending = null : (a = a.next, t.next = a, Vd(e, a)));
  }
  function du(e, t, a) {
    var l = e.pending;
    if (e.pending = null, l !== null) {
      l = l.next;
      do
        t.status = "rejected", t.reason = a, Qd(t), t = t.next;
      while (t !== l);
    }
    e.action = null;
  }
  function Qd(e) {
    e = e.listeners;
    for (var t = 0; t < e.length; t++) (0, e[t])();
  }
  function Zd(e, t) {
    return t;
  }
  function Kd(e, t) {
    if (ge) {
      var a = Ne.formState;
      if (a !== null) {
        e: {
          var l = ce;
          if (ge) {
            if (qe) {
              t: {
                for (var o = qe, u = Zt; o.nodeType !== 8; ) {
                  if (!u) {
                    o = null;
                    break t;
                  }
                  if (o = Jt(
                    o.nextSibling
                  ), o === null) {
                    o = null;
                    break t;
                  }
                }
                u = o.data, o = u === "F!" || u === "F" ? o : null;
              }
              if (o) {
                qe = Jt(
                  o.nextSibling
                ), l = o.data === "F!";
                break e;
              }
            }
            Pa(l);
          }
          l = !1;
        }
        l && (t = a[0]);
      }
    }
    return a = _t(), a.memoizedState = a.baseState = t, l = {
      pending: null,
      lanes: 0,
      dispatch: null,
      lastRenderedReducer: Zd,
      lastRenderedState: t
    }, a.queue = l, a = pp.bind(
      null,
      ce,
      l
    ), l.dispatch = a, l = fu(!1), u = bu.bind(
      null,
      ce,
      !1,
      l.queue
    ), l = _t(), o = {
      state: t,
      dispatch: null,
      action: e,
      pending: null
    }, l.queue = o, a = vg.bind(
      null,
      ce,
      o,
      u,
      a
    ), o.dispatch = a, l.memoizedState = e, [t, a, !1];
  }
  function Jd(e) {
    var t = Qe();
    return Fd(t, Oe, e);
  }
  function Fd(e, t, a) {
    if (t = uu(
      e,
      t,
      Zd
    )[0], e = ar(Oa)[0], typeof t == "object" && t !== null && typeof t.then == "function")
      try {
        var l = Ul(t);
      } catch (p) {
        throw p === Ri ? Qs : p;
      }
    else l = t;
    t = Qe();
    var o = t.queue, u = o.dispatch;
    return a !== t.memoizedState && (ce.flags |= 2048, Yi(
      9,
      { destroy: void 0 },
      gg.bind(null, o, a),
      null
    )), [l, u, e];
  }
  function gg(e, t) {
    e.action = t;
  }
  function Wd(e) {
    var t = Qe(), a = Oe;
    if (a !== null)
      return Fd(t, a, e);
    Qe(), t = t.memoizedState, a = Qe();
    var l = a.queue.dispatch;
    return a.memoizedState = e, [t, l, !1];
  }
  function Yi(e, t, a, l) {
    return e = { tag: e, create: a, deps: l, inst: t, next: null }, t = ce.updateQueue, t === null && (t = er(), ce.updateQueue = t), a = t.lastEffect, a === null ? t.lastEffect = e.next = e : (l = a.next, a.next = e, e.next = l, t.lastEffect = e), e;
  }
  function Id() {
    return Qe().memoizedState;
  }
  function nr(e, t, a, l) {
    var o = _t();
    ce.flags |= e, o.memoizedState = Yi(
      1 | t,
      { destroy: void 0 },
      a,
      l === void 0 ? null : l
    );
  }
  function ir(e, t, a, l) {
    var o = Qe();
    l = l === void 0 ? null : l;
    var u = o.memoizedState.inst;
    Oe !== null && l !== null && nu(l, Oe.memoizedState.deps) ? o.memoizedState = Yi(t, u, a, l) : (ce.flags |= e, o.memoizedState = Yi(
      1 | t,
      u,
      a,
      l
    ));
  }
  function Pd(e, t) {
    nr(8390656, 8, e, t);
  }
  function pu(e, t) {
    ir(2048, 8, e, t);
  }
  function bg(e) {
    ce.flags |= 4;
    var t = ce.updateQueue;
    if (t === null)
      t = er(), ce.updateQueue = t, t.events = [e];
    else {
      var a = t.events;
      a === null ? t.events = [e] : a.push(e);
    }
  }
  function ep(e) {
    var t = Qe().memoizedState;
    return bg({ ref: t, nextImpl: e }), function() {
      if ((ze & 2) !== 0) throw Error(r(440));
      return t.impl.apply(void 0, arguments);
    };
  }
  function tp(e, t) {
    return ir(4, 2, e, t);
  }
  function ap(e, t) {
    return ir(4, 4, e, t);
  }
  function np(e, t) {
    if (typeof t == "function") {
      e = e();
      var a = t(e);
      return function() {
        typeof a == "function" ? a() : t(null);
      };
    }
    if (t != null)
      return e = e(), t.current = e, function() {
        t.current = null;
      };
  }
  function ip(e, t, a) {
    a = a != null ? a.concat([e]) : null, ir(4, 4, np.bind(null, t, e), a);
  }
  function hu() {
  }
  function lp(e, t) {
    var a = Qe();
    t = t === void 0 ? null : t;
    var l = a.memoizedState;
    return t !== null && nu(t, l[1]) ? l[0] : (a.memoizedState = [e, t], e);
  }
  function sp(e, t) {
    var a = Qe();
    t = t === void 0 ? null : t;
    var l = a.memoizedState;
    if (t !== null && nu(t, l[1]))
      return l[0];
    if (l = e(), Jn) {
      oa(!0);
      try {
        e();
      } finally {
        oa(!1);
      }
    }
    return a.memoizedState = [l, t], l;
  }
  function mu(e, t, a) {
    return a === void 0 || (Aa & 1073741824) !== 0 && (me & 261930) === 0 ? e.memoizedState = t : (e.memoizedState = a, e = rh(), ce.lanes |= e, un |= e, a);
  }
  function rp(e, t, a, l) {
    return Nt(a, t) ? a : Ui.current !== null ? (e = mu(e, a, l), Nt(e, t) || (Pe = !0), e) : (Aa & 42) === 0 || (Aa & 1073741824) !== 0 && (me & 261930) === 0 ? (Pe = !0, e.memoizedState = a) : (e = rh(), ce.lanes |= e, un |= e, t);
  }
  function op(e, t, a, l, o) {
    var u = Q.p;
    Q.p = u !== 0 && 8 > u ? u : 8;
    var p = R.T, b = {};
    R.T = b, bu(e, !1, t, a);
    try {
      var w = o(), D = R.S;
      if (D !== null && D(b, w), w !== null && typeof w == "object" && typeof w.then == "function") {
        var U = pg(
          w,
          l
        );
        Ll(
          e,
          t,
          U,
          Ut(e)
        );
      } else
        Ll(
          e,
          t,
          l,
          Ut(e)
        );
    } catch (B) {
      Ll(
        e,
        t,
        { then: function() {
        }, status: "rejected", reason: B },
        Ut()
      );
    } finally {
      Q.p = u, p !== null && b.types !== null && (p.types = b.types), R.T = p;
    }
  }
  function yg() {
  }
  function vu(e, t, a, l) {
    if (e.tag !== 5) throw Error(r(476));
    var o = up(e).queue;
    op(
      e,
      o,
      t,
      le,
      a === null ? yg : function() {
        return cp(e), a(l);
      }
    );
  }
  function up(e) {
    var t = e.memoizedState;
    if (t !== null) return t;
    t = {
      memoizedState: le,
      baseState: le,
      baseQueue: null,
      queue: {
        pending: null,
        lanes: 0,
        dispatch: null,
        lastRenderedReducer: Oa,
        lastRenderedState: le
      },
      next: null
    };
    var a = {};
    return t.next = {
      memoizedState: a,
      baseState: a,
      baseQueue: null,
      queue: {
        pending: null,
        lanes: 0,
        dispatch: null,
        lastRenderedReducer: Oa,
        lastRenderedState: a
      },
      next: null
    }, e.memoizedState = t, e = e.alternate, e !== null && (e.memoizedState = t), t;
  }
  function cp(e) {
    var t = up(e);
    t.next === null && (t = e.alternate.memoizedState), Ll(
      e,
      t.next.queue,
      {},
      Ut()
    );
  }
  function gu() {
    return ct(as);
  }
  function fp() {
    return Qe().memoizedState;
  }
  function dp() {
    return Qe().memoizedState;
  }
  function _g(e) {
    for (var t = e.return; t !== null; ) {
      switch (t.tag) {
        case 24:
        case 3:
          var a = Ut();
          e = an(a);
          var l = nn(t, e, a);
          l !== null && (At(l, t, a), ql(l, t, a)), t = { cache: Qo() }, e.payload = t;
          return;
      }
      t = t.return;
    }
  }
  function xg(e, t, a) {
    var l = Ut();
    a = {
      lane: l,
      revertLane: 0,
      gesture: null,
      action: a,
      hasEagerState: !1,
      eagerState: null,
      next: null
    }, lr(e) ? hp(t, a) : (a = jo(e, t, a, l), a !== null && (At(a, e, l), mp(a, t, l)));
  }
  function pp(e, t, a) {
    var l = Ut();
    Ll(e, t, a, l);
  }
  function Ll(e, t, a, l) {
    var o = {
      lane: l,
      revertLane: 0,
      gesture: null,
      action: a,
      hasEagerState: !1,
      eagerState: null,
      next: null
    };
    if (lr(e)) hp(t, o);
    else {
      var u = e.alternate;
      if (e.lanes === 0 && (u === null || u.lanes === 0) && (u = t.lastRenderedReducer, u !== null))
        try {
          var p = t.lastRenderedState, b = u(p, a);
          if (o.hasEagerState = !0, o.eagerState = b, Nt(b, p))
            return Ls(e, t, o, 0), Ne === null && Us(), !1;
        } catch {
        } finally {
        }
      if (a = jo(e, t, o, l), a !== null)
        return At(a, e, l), mp(a, t, l), !0;
    }
    return !1;
  }
  function bu(e, t, a, l) {
    if (l = {
      lane: 2,
      revertLane: Fu(),
      gesture: null,
      action: l,
      hasEagerState: !1,
      eagerState: null,
      next: null
    }, lr(e)) {
      if (t) throw Error(r(479));
    } else
      t = jo(
        e,
        a,
        l,
        2
      ), t !== null && At(t, e, 2);
  }
  function lr(e) {
    var t = e.alternate;
    return e === ce || t !== null && t === ce;
  }
  function hp(e, t) {
    Li = Is = !0;
    var a = e.pending;
    a === null ? t.next = t : (t.next = a.next, a.next = t), e.pending = t;
  }
  function mp(e, t, a) {
    if ((a & 4194048) !== 0) {
      var l = t.lanes;
      l &= e.pendingLanes, a |= l, t.lanes = a, Re(e, a);
    }
  }
  var Hl = {
    readContext: ct,
    use: tr,
    useCallback: Be,
    useContext: Be,
    useEffect: Be,
    useImperativeHandle: Be,
    useLayoutEffect: Be,
    useInsertionEffect: Be,
    useMemo: Be,
    useReducer: Be,
    useRef: Be,
    useState: Be,
    useDebugValue: Be,
    useDeferredValue: Be,
    useTransition: Be,
    useSyncExternalStore: Be,
    useId: Be,
    useHostTransitionStatus: Be,
    useFormState: Be,
    useActionState: Be,
    useOptimistic: Be,
    useMemoCache: Be,
    useCacheRefresh: Be
  };
  Hl.useEffectEvent = Be;
  var vp = {
    readContext: ct,
    use: tr,
    useCallback: function(e, t) {
      return _t().memoizedState = [
        e,
        t === void 0 ? null : t
      ], e;
    },
    useContext: ct,
    useEffect: Pd,
    useImperativeHandle: function(e, t, a) {
      a = a != null ? a.concat([e]) : null, nr(
        4194308,
        4,
        np.bind(null, t, e),
        a
      );
    },
    useLayoutEffect: function(e, t) {
      return nr(4194308, 4, e, t);
    },
    useInsertionEffect: function(e, t) {
      nr(4, 2, e, t);
    },
    useMemo: function(e, t) {
      var a = _t();
      t = t === void 0 ? null : t;
      var l = e();
      if (Jn) {
        oa(!0);
        try {
          e();
        } finally {
          oa(!1);
        }
      }
      return a.memoizedState = [l, t], l;
    },
    useReducer: function(e, t, a) {
      var l = _t();
      if (a !== void 0) {
        var o = a(t);
        if (Jn) {
          oa(!0);
          try {
            a(t);
          } finally {
            oa(!1);
          }
        }
      } else o = t;
      return l.memoizedState = l.baseState = o, e = {
        pending: null,
        lanes: 0,
        dispatch: null,
        lastRenderedReducer: e,
        lastRenderedState: o
      }, l.queue = e, e = e.dispatch = xg.bind(
        null,
        ce,
        e
      ), [l.memoizedState, e];
    },
    useRef: function(e) {
      var t = _t();
      return e = { current: e }, t.memoizedState = e;
    },
    useState: function(e) {
      e = fu(e);
      var t = e.queue, a = pp.bind(null, ce, t);
      return t.dispatch = a, [e.memoizedState, a];
    },
    useDebugValue: hu,
    useDeferredValue: function(e, t) {
      var a = _t();
      return mu(a, e, t);
    },
    useTransition: function() {
      var e = fu(!1);
      return e = op.bind(
        null,
        ce,
        e.queue,
        !0,
        !1
      ), _t().memoizedState = e, [!1, e];
    },
    useSyncExternalStore: function(e, t, a) {
      var l = ce, o = _t();
      if (ge) {
        if (a === void 0)
          throw Error(r(407));
        a = a();
      } else {
        if (a = t(), Ne === null)
          throw Error(r(349));
        (me & 127) !== 0 || $d(l, t, a);
      }
      o.memoizedState = a;
      var u = { value: a, getSnapshot: t };
      return o.queue = u, Pd(Ld.bind(null, l, u, e), [
        e
      ]), l.flags |= 2048, Yi(
        9,
        { destroy: void 0 },
        Ud.bind(
          null,
          l,
          u,
          a,
          t
        ),
        null
      ), a;
    },
    useId: function() {
      var e = _t(), t = Ne.identifierPrefix;
      if (ge) {
        var a = da, l = fa;
        a = (l & ~(1 << 32 - rt(l) - 1)).toString(32) + a, t = "_" + t + "R_" + a, a = Ps++, 0 < a && (t += "H" + a.toString(32)), t += "_";
      } else
        a = hg++, t = "_" + t + "r_" + a.toString(32) + "_";
      return e.memoizedState = t;
    },
    useHostTransitionStatus: gu,
    useFormState: Kd,
    useActionState: Kd,
    useOptimistic: function(e) {
      var t = _t();
      t.memoizedState = t.baseState = e;
      var a = {
        pending: null,
        lanes: 0,
        dispatch: null,
        lastRenderedReducer: null,
        lastRenderedState: null
      };
      return t.queue = a, t = bu.bind(
        null,
        ce,
        !0,
        a
      ), a.dispatch = t, [e, t];
    },
    useMemoCache: ou,
    useCacheRefresh: function() {
      return _t().memoizedState = _g.bind(
        null,
        ce
      );
    },
    useEffectEvent: function(e) {
      var t = _t(), a = { impl: e };
      return t.memoizedState = a, function() {
        if ((ze & 2) !== 0)
          throw Error(r(440));
        return a.impl.apply(void 0, arguments);
      };
    }
  }, yu = {
    readContext: ct,
    use: tr,
    useCallback: lp,
    useContext: ct,
    useEffect: pu,
    useImperativeHandle: ip,
    useInsertionEffect: tp,
    useLayoutEffect: ap,
    useMemo: sp,
    useReducer: ar,
    useRef: Id,
    useState: function() {
      return ar(Oa);
    },
    useDebugValue: hu,
    useDeferredValue: function(e, t) {
      var a = Qe();
      return rp(
        a,
        Oe.memoizedState,
        e,
        t
      );
    },
    useTransition: function() {
      var e = ar(Oa)[0], t = Qe().memoizedState;
      return [
        typeof e == "boolean" ? e : Ul(e),
        t
      ];
    },
    useSyncExternalStore: Rd,
    useId: fp,
    useHostTransitionStatus: gu,
    useFormState: Jd,
    useActionState: Jd,
    useOptimistic: function(e, t) {
      var a = Qe();
      return Bd(a, Oe, e, t);
    },
    useMemoCache: ou,
    useCacheRefresh: dp
  };
  yu.useEffectEvent = ep;
  var gp = {
    readContext: ct,
    use: tr,
    useCallback: lp,
    useContext: ct,
    useEffect: pu,
    useImperativeHandle: ip,
    useInsertionEffect: tp,
    useLayoutEffect: ap,
    useMemo: sp,
    useReducer: cu,
    useRef: Id,
    useState: function() {
      return cu(Oa);
    },
    useDebugValue: hu,
    useDeferredValue: function(e, t) {
      var a = Qe();
      return Oe === null ? mu(a, e, t) : rp(
        a,
        Oe.memoizedState,
        e,
        t
      );
    },
    useTransition: function() {
      var e = cu(Oa)[0], t = Qe().memoizedState;
      return [
        typeof e == "boolean" ? e : Ul(e),
        t
      ];
    },
    useSyncExternalStore: Rd,
    useId: fp,
    useHostTransitionStatus: gu,
    useFormState: Wd,
    useActionState: Wd,
    useOptimistic: function(e, t) {
      var a = Qe();
      return Oe !== null ? Bd(a, Oe, e, t) : (a.baseState = e, [e, a.queue.dispatch]);
    },
    useMemoCache: ou,
    useCacheRefresh: dp
  };
  gp.useEffectEvent = ep;
  function _u(e, t, a, l) {
    t = e.memoizedState, a = a(l, t), a = a == null ? t : m({}, t, a), e.memoizedState = a, e.lanes === 0 && (e.updateQueue.baseState = a);
  }
  var xu = {
    enqueueSetState: function(e, t, a) {
      e = e._reactInternals;
      var l = Ut(), o = an(l);
      o.payload = t, a != null && (o.callback = a), t = nn(e, o, l), t !== null && (At(t, e, l), ql(t, e, l));
    },
    enqueueReplaceState: function(e, t, a) {
      e = e._reactInternals;
      var l = Ut(), o = an(l);
      o.tag = 1, o.payload = t, a != null && (o.callback = a), t = nn(e, o, l), t !== null && (At(t, e, l), ql(t, e, l));
    },
    enqueueForceUpdate: function(e, t) {
      e = e._reactInternals;
      var a = Ut(), l = an(a);
      l.tag = 2, t != null && (l.callback = t), t = nn(e, l, a), t !== null && (At(t, e, a), ql(t, e, a));
    }
  };
  function bp(e, t, a, l, o, u, p) {
    return e = e.stateNode, typeof e.shouldComponentUpdate == "function" ? e.shouldComponentUpdate(l, u, p) : t.prototype && t.prototype.isPureReactComponent ? !Tl(a, l) || !Tl(o, u) : !0;
  }
  function yp(e, t, a, l) {
    e = t.state, typeof t.componentWillReceiveProps == "function" && t.componentWillReceiveProps(a, l), typeof t.UNSAFE_componentWillReceiveProps == "function" && t.UNSAFE_componentWillReceiveProps(a, l), t.state !== e && xu.enqueueReplaceState(t, t.state, null);
  }
  function Fn(e, t) {
    var a = t;
    if ("ref" in t) {
      a = {};
      for (var l in t)
        l !== "ref" && (a[l] = t[l]);
    }
    if (e = e.defaultProps) {
      a === t && (a = m({}, a));
      for (var o in e)
        a[o] === void 0 && (a[o] = e[o]);
    }
    return a;
  }
  function _p(e) {
    $s(e);
  }
  function xp(e) {
    console.error(e);
  }
  function wp(e) {
    $s(e);
  }
  function sr(e, t) {
    try {
      var a = e.onUncaughtError;
      a(t.value, { componentStack: t.stack });
    } catch (l) {
      setTimeout(function() {
        throw l;
      });
    }
  }
  function kp(e, t, a) {
    try {
      var l = e.onCaughtError;
      l(a.value, {
        componentStack: a.stack,
        errorBoundary: t.tag === 1 ? t.stateNode : null
      });
    } catch (o) {
      setTimeout(function() {
        throw o;
      });
    }
  }
  function wu(e, t, a) {
    return a = an(a), a.tag = 3, a.payload = { element: null }, a.callback = function() {
      sr(e, t);
    }, a;
  }
  function Sp(e) {
    return e = an(e), e.tag = 3, e;
  }
  function zp(e, t, a, l) {
    var o = a.type.getDerivedStateFromError;
    if (typeof o == "function") {
      var u = l.value;
      e.payload = function() {
        return o(u);
      }, e.callback = function() {
        kp(t, a, l);
      };
    }
    var p = a.stateNode;
    p !== null && typeof p.componentDidCatch == "function" && (e.callback = function() {
      kp(t, a, l), typeof o != "function" && (cn === null ? cn = /* @__PURE__ */ new Set([this]) : cn.add(this));
      var b = l.stack;
      this.componentDidCatch(l.value, {
        componentStack: b !== null ? b : ""
      });
    });
  }
  function wg(e, t, a, l, o) {
    if (a.flags |= 32768, l !== null && typeof l == "object" && typeof l.then == "function") {
      if (t = a.alternate, t !== null && Mi(
        t,
        a,
        o,
        !0
      ), a = qt.current, a !== null) {
        switch (a.tag) {
          case 31:
          case 13:
            return Kt === null ? br() : a.alternate === null && Ve === 0 && (Ve = 3), a.flags &= -257, a.flags |= 65536, a.lanes = o, l === Zs ? a.flags |= 16384 : (t = a.updateQueue, t === null ? a.updateQueue = /* @__PURE__ */ new Set([l]) : t.add(l), Zu(e, l, o)), !1;
          case 22:
            return a.flags |= 65536, l === Zs ? a.flags |= 16384 : (t = a.updateQueue, t === null ? (t = {
              transitions: null,
              markerInstances: null,
              retryQueue: /* @__PURE__ */ new Set([l])
            }, a.updateQueue = t) : (a = t.retryQueue, a === null ? t.retryQueue = /* @__PURE__ */ new Set([l]) : a.add(l)), Zu(e, l, o)), !1;
        }
        throw Error(r(435, a.tag));
      }
      return Zu(e, l, o), br(), !1;
    }
    if (ge)
      return t = qt.current, t !== null ? ((t.flags & 65536) === 0 && (t.flags |= 256), t.flags |= 65536, t.lanes = o, l !== Yo && (e = Error(r(422), { cause: l }), Ol(Xt(e, a)))) : (l !== Yo && (t = Error(r(423), {
        cause: l
      }), Ol(
        Xt(t, a)
      )), e = e.current.alternate, e.flags |= 65536, o &= -o, e.lanes |= o, l = Xt(l, a), o = wu(
        e.stateNode,
        l,
        o
      ), Io(e, o), Ve !== 4 && (Ve = 2)), !1;
    var u = Error(r(520), { cause: l });
    if (u = Xt(u, a), Kl === null ? Kl = [u] : Kl.push(u), Ve !== 4 && (Ve = 2), t === null) return !0;
    l = Xt(l, a), a = t;
    do {
      switch (a.tag) {
        case 3:
          return a.flags |= 65536, e = o & -o, a.lanes |= e, e = wu(a.stateNode, l, e), Io(a, e), !1;
        case 1:
          if (t = a.type, u = a.stateNode, (a.flags & 128) === 0 && (typeof t.getDerivedStateFromError == "function" || u !== null && typeof u.componentDidCatch == "function" && (cn === null || !cn.has(u))))
            return a.flags |= 65536, o &= -o, a.lanes |= o, o = Sp(o), zp(
              o,
              e,
              a,
              l
            ), Io(a, o), !1;
      }
      a = a.return;
    } while (a !== null);
    return !1;
  }
  var ku = Error(r(461)), Pe = !1;
  function ft(e, t, a, l) {
    t.child = e === null ? Od(t, null, a, l) : Kn(
      t,
      e.child,
      a,
      l
    );
  }
  function Tp(e, t, a, l, o) {
    a = a.render;
    var u = t.ref;
    if ("ref" in l) {
      var p = {};
      for (var b in l)
        b !== "ref" && (p[b] = l[b]);
    } else p = l;
    return Xn(t), l = iu(
      e,
      t,
      a,
      p,
      u,
      o
    ), b = lu(), e !== null && !Pe ? (su(e, t, o), Da(e, t, o)) : (ge && b && Lo(t), t.flags |= 1, ft(e, t, l, o), t.child);
  }
  function Ep(e, t, a, l, o) {
    if (e === null) {
      var u = a.type;
      return typeof u == "function" && !Ro(u) && u.defaultProps === void 0 && a.compare === null ? (t.tag = 15, t.type = u, Ap(
        e,
        t,
        u,
        l,
        o
      )) : (e = Ys(
        a.type,
        null,
        l,
        t,
        t.mode,
        o
      ), e.ref = t.ref, e.return = t, t.child = e);
    }
    if (u = e.child, !Cu(e, o)) {
      var p = u.memoizedProps;
      if (a = a.compare, a = a !== null ? a : Tl, a(p, l) && e.ref === t.ref)
        return Da(e, t, o);
    }
    return t.flags |= 1, e = Sa(u, l), e.ref = t.ref, e.return = t, t.child = e;
  }
  function Ap(e, t, a, l, o) {
    if (e !== null) {
      var u = e.memoizedProps;
      if (Tl(u, l) && e.ref === t.ref)
        if (Pe = !1, t.pendingProps = l = u, Cu(e, o))
          (e.flags & 131072) !== 0 && (Pe = !0);
        else
          return t.lanes = e.lanes, Da(e, t, o);
    }
    return Su(
      e,
      t,
      a,
      l,
      o
    );
  }
  function Op(e, t, a, l) {
    var o = l.children, u = e !== null ? e.memoizedState : null;
    if (e === null && t.stateNode === null && (t.stateNode = {
      _visibility: 1,
      _pendingMarkers: null,
      _retryCache: null,
      _transitions: null
    }), l.mode === "hidden") {
      if ((t.flags & 128) !== 0) {
        if (u = u !== null ? u.baseLanes | a : a, e !== null) {
          for (l = t.child = e.child, o = 0; l !== null; )
            o = o | l.lanes | l.childLanes, l = l.sibling;
          l = o & ~u;
        } else l = 0, t.child = null;
        return Dp(
          e,
          t,
          u,
          a,
          l
        );
      }
      if ((a & 536870912) !== 0)
        t.memoizedState = { baseLanes: 0, cachePool: null }, e !== null && Gs(
          t,
          u !== null ? u.cachePool : null
        ), u !== null ? Nd(t, u) : eu(), Md(t);
      else
        return l = t.lanes = 536870912, Dp(
          e,
          t,
          u !== null ? u.baseLanes | a : a,
          a,
          l
        );
    } else
      u !== null ? (Gs(t, u.cachePool), Nd(t, u), sn(), t.memoizedState = null) : (e !== null && Gs(t, null), eu(), sn());
    return ft(e, t, o, a), t.child;
  }
  function Yl(e, t) {
    return e !== null && e.tag === 22 || t.stateNode !== null || (t.stateNode = {
      _visibility: 1,
      _pendingMarkers: null,
      _retryCache: null,
      _transitions: null
    }), t.sibling;
  }
  function Dp(e, t, a, l, o) {
    var u = Ko();
    return u = u === null ? null : { parent: We._currentValue, pool: u }, t.memoizedState = {
      baseLanes: a,
      cachePool: u
    }, e !== null && Gs(t, null), eu(), Md(t), e !== null && Mi(e, t, l, !0), t.childLanes = o, null;
  }
  function rr(e, t) {
    return t = ur(
      { mode: t.mode, children: t.children },
      e.mode
    ), t.ref = e.ref, e.child = t, t.return = e, t;
  }
  function Cp(e, t, a) {
    return Kn(t, e.child, null, a), e = rr(t, t.pendingProps), e.flags |= 2, jt(t), t.memoizedState = null, e;
  }
  function kg(e, t, a) {
    var l = t.pendingProps, o = (t.flags & 128) !== 0;
    if (t.flags &= -129, e === null) {
      if (ge) {
        if (l.mode === "hidden")
          return e = rr(t, l), t.lanes = 536870912, Yl(null, e);
        if (au(t), (e = qe) ? (e = Vh(
          e,
          Zt
        ), e = e !== null && e.data === "&" ? e : null, e !== null && (t.memoizedState = {
          dehydrated: e,
          treeContext: Wa !== null ? { id: fa, overflow: da } : null,
          retryLane: 536870912,
          hydrationErrors: null
        }, a = hd(e), a.return = t, t.child = a, ut = t, qe = null)) : e = null, e === null) throw Pa(t);
        return t.lanes = 536870912, null;
      }
      return rr(t, l);
    }
    var u = e.memoizedState;
    if (u !== null) {
      var p = u.dehydrated;
      if (au(t), o)
        if (t.flags & 256)
          t.flags &= -257, t = Cp(
            e,
            t,
            a
          );
        else if (t.memoizedState !== null)
          t.child = e.child, t.flags |= 128, t = null;
        else throw Error(r(558));
      else if (Pe || Mi(e, t, a, !1), o = (a & e.childLanes) !== 0, Pe || o) {
        if (l = Ne, l !== null && (p = Fe(l, a), p !== 0 && p !== u.retryLane))
          throw u.retryLane = p, Hn(e, p), At(l, e, p), ku;
        br(), t = Cp(
          e,
          t,
          a
        );
      } else
        e = u.treeContext, qe = Jt(p.nextSibling), ut = t, ge = !0, Ia = null, Zt = !1, e !== null && gd(t, e), t = rr(t, l), t.flags |= 4096;
      return t;
    }
    return e = Sa(e.child, {
      mode: l.mode,
      children: l.children
    }), e.ref = t.ref, t.child = e, e.return = t, e;
  }
  function or(e, t) {
    var a = t.ref;
    if (a === null)
      e !== null && e.ref !== null && (t.flags |= 4194816);
    else {
      if (typeof a != "function" && typeof a != "object")
        throw Error(r(284));
      (e === null || e.ref !== a) && (t.flags |= 4194816);
    }
  }
  function Su(e, t, a, l, o) {
    return Xn(t), a = iu(
      e,
      t,
      a,
      l,
      void 0,
      o
    ), l = lu(), e !== null && !Pe ? (su(e, t, o), Da(e, t, o)) : (ge && l && Lo(t), t.flags |= 1, ft(e, t, a, o), t.child);
  }
  function Np(e, t, a, l, o, u) {
    return Xn(t), t.updateQueue = null, a = jd(
      t,
      l,
      a,
      o
    ), qd(e), l = lu(), e !== null && !Pe ? (su(e, t, u), Da(e, t, u)) : (ge && l && Lo(t), t.flags |= 1, ft(e, t, a, u), t.child);
  }
  function Mp(e, t, a, l, o) {
    if (Xn(t), t.stateNode === null) {
      var u = Oi, p = a.contextType;
      typeof p == "object" && p !== null && (u = ct(p)), u = new a(l, u), t.memoizedState = u.state !== null && u.state !== void 0 ? u.state : null, u.updater = xu, t.stateNode = u, u._reactInternals = t, u = t.stateNode, u.props = l, u.state = t.memoizedState, u.refs = {}, Fo(t), p = a.contextType, u.context = typeof p == "object" && p !== null ? ct(p) : Oi, u.state = t.memoizedState, p = a.getDerivedStateFromProps, typeof p == "function" && (_u(
        t,
        a,
        p,
        l
      ), u.state = t.memoizedState), typeof a.getDerivedStateFromProps == "function" || typeof u.getSnapshotBeforeUpdate == "function" || typeof u.UNSAFE_componentWillMount != "function" && typeof u.componentWillMount != "function" || (p = u.state, typeof u.componentWillMount == "function" && u.componentWillMount(), typeof u.UNSAFE_componentWillMount == "function" && u.UNSAFE_componentWillMount(), p !== u.state && xu.enqueueReplaceState(u, u.state, null), Rl(t, l, u, o), jl(), u.state = t.memoizedState), typeof u.componentDidMount == "function" && (t.flags |= 4194308), l = !0;
    } else if (e === null) {
      u = t.stateNode;
      var b = t.memoizedProps, w = Fn(a, b);
      u.props = w;
      var D = u.context, U = a.contextType;
      p = Oi, typeof U == "object" && U !== null && (p = ct(U));
      var B = a.getDerivedStateFromProps;
      U = typeof B == "function" || typeof u.getSnapshotBeforeUpdate == "function", b = t.pendingProps !== b, U || typeof u.UNSAFE_componentWillReceiveProps != "function" && typeof u.componentWillReceiveProps != "function" || (b || D !== p) && yp(
        t,
        u,
        l,
        p
      ), tn = !1;
      var C = t.memoizedState;
      u.state = C, Rl(t, l, u, o), jl(), D = t.memoizedState, b || C !== D || tn ? (typeof B == "function" && (_u(
        t,
        a,
        B,
        l
      ), D = t.memoizedState), (w = tn || bp(
        t,
        a,
        w,
        l,
        C,
        D,
        p
      )) ? (U || typeof u.UNSAFE_componentWillMount != "function" && typeof u.componentWillMount != "function" || (typeof u.componentWillMount == "function" && u.componentWillMount(), typeof u.UNSAFE_componentWillMount == "function" && u.UNSAFE_componentWillMount()), typeof u.componentDidMount == "function" && (t.flags |= 4194308)) : (typeof u.componentDidMount == "function" && (t.flags |= 4194308), t.memoizedProps = l, t.memoizedState = D), u.props = l, u.state = D, u.context = p, l = w) : (typeof u.componentDidMount == "function" && (t.flags |= 4194308), l = !1);
    } else {
      u = t.stateNode, Wo(e, t), p = t.memoizedProps, U = Fn(a, p), u.props = U, B = t.pendingProps, C = u.context, D = a.contextType, w = Oi, typeof D == "object" && D !== null && (w = ct(D)), b = a.getDerivedStateFromProps, (D = typeof b == "function" || typeof u.getSnapshotBeforeUpdate == "function") || typeof u.UNSAFE_componentWillReceiveProps != "function" && typeof u.componentWillReceiveProps != "function" || (p !== B || C !== w) && yp(
        t,
        u,
        l,
        w
      ), tn = !1, C = t.memoizedState, u.state = C, Rl(t, l, u, o), jl();
      var M = t.memoizedState;
      p !== B || C !== M || tn || e !== null && e.dependencies !== null && Vs(e.dependencies) ? (typeof b == "function" && (_u(
        t,
        a,
        b,
        l
      ), M = t.memoizedState), (U = tn || bp(
        t,
        a,
        U,
        l,
        C,
        M,
        w
      ) || e !== null && e.dependencies !== null && Vs(e.dependencies)) ? (D || typeof u.UNSAFE_componentWillUpdate != "function" && typeof u.componentWillUpdate != "function" || (typeof u.componentWillUpdate == "function" && u.componentWillUpdate(l, M, w), typeof u.UNSAFE_componentWillUpdate == "function" && u.UNSAFE_componentWillUpdate(
        l,
        M,
        w
      )), typeof u.componentDidUpdate == "function" && (t.flags |= 4), typeof u.getSnapshotBeforeUpdate == "function" && (t.flags |= 1024)) : (typeof u.componentDidUpdate != "function" || p === e.memoizedProps && C === e.memoizedState || (t.flags |= 4), typeof u.getSnapshotBeforeUpdate != "function" || p === e.memoizedProps && C === e.memoizedState || (t.flags |= 1024), t.memoizedProps = l, t.memoizedState = M), u.props = l, u.state = M, u.context = w, l = U) : (typeof u.componentDidUpdate != "function" || p === e.memoizedProps && C === e.memoizedState || (t.flags |= 4), typeof u.getSnapshotBeforeUpdate != "function" || p === e.memoizedProps && C === e.memoizedState || (t.flags |= 1024), l = !1);
    }
    return u = l, or(e, t), l = (t.flags & 128) !== 0, u || l ? (u = t.stateNode, a = l && typeof a.getDerivedStateFromError != "function" ? null : u.render(), t.flags |= 1, e !== null && l ? (t.child = Kn(
      t,
      e.child,
      null,
      o
    ), t.child = Kn(
      t,
      null,
      a,
      o
    )) : ft(e, t, a, o), t.memoizedState = u.state, e = t.child) : e = Da(
      e,
      t,
      o
    ), e;
  }
  function qp(e, t, a, l) {
    return Bn(), t.flags |= 256, ft(e, t, a, l), t.child;
  }
  var zu = {
    dehydrated: null,
    treeContext: null,
    retryLane: 0,
    hydrationErrors: null
  };
  function Tu(e) {
    return { baseLanes: e, cachePool: kd() };
  }
  function Eu(e, t, a) {
    return e = e !== null ? e.childLanes & ~a : 0, t && (e |= $t), e;
  }
  function jp(e, t, a) {
    var l = t.pendingProps, o = !1, u = (t.flags & 128) !== 0, p;
    if ((p = u) || (p = e !== null && e.memoizedState === null ? !1 : (Ge.current & 2) !== 0), p && (o = !0, t.flags &= -129), p = (t.flags & 32) !== 0, t.flags &= -33, e === null) {
      if (ge) {
        if (o ? ln(t) : sn(), (e = qe) ? (e = Vh(
          e,
          Zt
        ), e = e !== null && e.data !== "&" ? e : null, e !== null && (t.memoizedState = {
          dehydrated: e,
          treeContext: Wa !== null ? { id: fa, overflow: da } : null,
          retryLane: 536870912,
          hydrationErrors: null
        }, a = hd(e), a.return = t, t.child = a, ut = t, qe = null)) : e = null, e === null) throw Pa(t);
        return uc(e) ? t.lanes = 32 : t.lanes = 536870912, null;
      }
      var b = l.children;
      return l = l.fallback, o ? (sn(), o = t.mode, b = ur(
        { mode: "hidden", children: b },
        o
      ), l = Yn(
        l,
        o,
        a,
        null
      ), b.return = t, l.return = t, b.sibling = l, t.child = b, l = t.child, l.memoizedState = Tu(a), l.childLanes = Eu(
        e,
        p,
        a
      ), t.memoizedState = zu, Yl(null, l)) : (ln(t), Au(t, b));
    }
    var w = e.memoizedState;
    if (w !== null && (b = w.dehydrated, b !== null)) {
      if (u)
        t.flags & 256 ? (ln(t), t.flags &= -257, t = Ou(
          e,
          t,
          a
        )) : t.memoizedState !== null ? (sn(), t.child = e.child, t.flags |= 128, t = null) : (sn(), b = l.fallback, o = t.mode, l = ur(
          { mode: "visible", children: l.children },
          o
        ), b = Yn(
          b,
          o,
          a,
          null
        ), b.flags |= 2, l.return = t, b.return = t, l.sibling = b, t.child = l, Kn(
          t,
          e.child,
          null,
          a
        ), l = t.child, l.memoizedState = Tu(a), l.childLanes = Eu(
          e,
          p,
          a
        ), t.memoizedState = zu, t = Yl(null, l));
      else if (ln(t), uc(b)) {
        if (p = b.nextSibling && b.nextSibling.dataset, p) var D = p.dgst;
        p = D, l = Error(r(419)), l.stack = "", l.digest = p, Ol({ value: l, source: null, stack: null }), t = Ou(
          e,
          t,
          a
        );
      } else if (Pe || Mi(e, t, a, !1), p = (a & e.childLanes) !== 0, Pe || p) {
        if (p = Ne, p !== null && (l = Fe(p, a), l !== 0 && l !== w.retryLane))
          throw w.retryLane = l, Hn(e, l), At(p, e, l), ku;
        oc(b) || br(), t = Ou(
          e,
          t,
          a
        );
      } else
        oc(b) ? (t.flags |= 192, t.child = e.child, t = null) : (e = w.treeContext, qe = Jt(
          b.nextSibling
        ), ut = t, ge = !0, Ia = null, Zt = !1, e !== null && gd(t, e), t = Au(
          t,
          l.children
        ), t.flags |= 4096);
      return t;
    }
    return o ? (sn(), b = l.fallback, o = t.mode, w = e.child, D = w.sibling, l = Sa(w, {
      mode: "hidden",
      children: l.children
    }), l.subtreeFlags = w.subtreeFlags & 65011712, D !== null ? b = Sa(
      D,
      b
    ) : (b = Yn(
      b,
      o,
      a,
      null
    ), b.flags |= 2), b.return = t, l.return = t, l.sibling = b, t.child = l, Yl(null, l), l = t.child, b = e.child.memoizedState, b === null ? b = Tu(a) : (o = b.cachePool, o !== null ? (w = We._currentValue, o = o.parent !== w ? { parent: w, pool: w } : o) : o = kd(), b = {
      baseLanes: b.baseLanes | a,
      cachePool: o
    }), l.memoizedState = b, l.childLanes = Eu(
      e,
      p,
      a
    ), t.memoizedState = zu, Yl(e.child, l)) : (ln(t), a = e.child, e = a.sibling, a = Sa(a, {
      mode: "visible",
      children: l.children
    }), a.return = t, a.sibling = null, e !== null && (p = t.deletions, p === null ? (t.deletions = [e], t.flags |= 16) : p.push(e)), t.child = a, t.memoizedState = null, a);
  }
  function Au(e, t) {
    return t = ur(
      { mode: "visible", children: t },
      e.mode
    ), t.return = e, e.child = t;
  }
  function ur(e, t) {
    return e = Mt(22, e, null, t), e.lanes = 0, e;
  }
  function Ou(e, t, a) {
    return Kn(t, e.child, null, a), e = Au(
      t,
      t.pendingProps.children
    ), e.flags |= 2, t.memoizedState = null, e;
  }
  function Rp(e, t, a) {
    e.lanes |= t;
    var l = e.alternate;
    l !== null && (l.lanes |= t), Xo(e.return, t, a);
  }
  function Du(e, t, a, l, o, u) {
    var p = e.memoizedState;
    p === null ? e.memoizedState = {
      isBackwards: t,
      rendering: null,
      renderingStartTime: 0,
      last: l,
      tail: a,
      tailMode: o,
      treeForkCount: u
    } : (p.isBackwards = t, p.rendering = null, p.renderingStartTime = 0, p.last = l, p.tail = a, p.tailMode = o, p.treeForkCount = u);
  }
  function $p(e, t, a) {
    var l = t.pendingProps, o = l.revealOrder, u = l.tail;
    l = l.children;
    var p = Ge.current, b = (p & 2) !== 0;
    if (b ? (p = p & 1 | 2, t.flags |= 128) : p &= 1, K(Ge, p), ft(e, t, l, a), l = ge ? Al : 0, !b && e !== null && (e.flags & 128) !== 0)
      e: for (e = t.child; e !== null; ) {
        if (e.tag === 13)
          e.memoizedState !== null && Rp(e, a, t);
        else if (e.tag === 19)
          Rp(e, a, t);
        else if (e.child !== null) {
          e.child.return = e, e = e.child;
          continue;
        }
        if (e === t) break e;
        for (; e.sibling === null; ) {
          if (e.return === null || e.return === t)
            break e;
          e = e.return;
        }
        e.sibling.return = e.return, e = e.sibling;
      }
    switch (o) {
      case "forwards":
        for (a = t.child, o = null; a !== null; )
          e = a.alternate, e !== null && Ws(e) === null && (o = a), a = a.sibling;
        a = o, a === null ? (o = t.child, t.child = null) : (o = a.sibling, a.sibling = null), Du(
          t,
          !1,
          o,
          a,
          u,
          l
        );
        break;
      case "backwards":
      case "unstable_legacy-backwards":
        for (a = null, o = t.child, t.child = null; o !== null; ) {
          if (e = o.alternate, e !== null && Ws(e) === null) {
            t.child = o;
            break;
          }
          e = o.sibling, o.sibling = a, a = o, o = e;
        }
        Du(
          t,
          !0,
          a,
          null,
          u,
          l
        );
        break;
      case "together":
        Du(
          t,
          !1,
          null,
          null,
          void 0,
          l
        );
        break;
      default:
        t.memoizedState = null;
    }
    return t.child;
  }
  function Da(e, t, a) {
    if (e !== null && (t.dependencies = e.dependencies), un |= t.lanes, (a & t.childLanes) === 0)
      if (e !== null) {
        if (Mi(
          e,
          t,
          a,
          !1
        ), (a & t.childLanes) === 0)
          return null;
      } else return null;
    if (e !== null && t.child !== e.child)
      throw Error(r(153));
    if (t.child !== null) {
      for (e = t.child, a = Sa(e, e.pendingProps), t.child = a, a.return = t; e.sibling !== null; )
        e = e.sibling, a = a.sibling = Sa(e, e.pendingProps), a.return = t;
      a.sibling = null;
    }
    return t.child;
  }
  function Cu(e, t) {
    return (e.lanes & t) !== 0 ? !0 : (e = e.dependencies, !!(e !== null && Vs(e)));
  }
  function Sg(e, t, a) {
    switch (t.tag) {
      case 3:
        Je(t, t.stateNode.containerInfo), en(t, We, e.memoizedState.cache), Bn();
        break;
      case 27:
      case 5:
        ra(t);
        break;
      case 4:
        Je(t, t.stateNode.containerInfo);
        break;
      case 10:
        en(
          t,
          t.type,
          t.memoizedProps.value
        );
        break;
      case 31:
        if (t.memoizedState !== null)
          return t.flags |= 128, au(t), null;
        break;
      case 13:
        var l = t.memoizedState;
        if (l !== null)
          return l.dehydrated !== null ? (ln(t), t.flags |= 128, null) : (a & t.child.childLanes) !== 0 ? jp(e, t, a) : (ln(t), e = Da(
            e,
            t,
            a
          ), e !== null ? e.sibling : null);
        ln(t);
        break;
      case 19:
        var o = (e.flags & 128) !== 0;
        if (l = (a & t.childLanes) !== 0, l || (Mi(
          e,
          t,
          a,
          !1
        ), l = (a & t.childLanes) !== 0), o) {
          if (l)
            return $p(
              e,
              t,
              a
            );
          t.flags |= 128;
        }
        if (o = t.memoizedState, o !== null && (o.rendering = null, o.tail = null, o.lastEffect = null), K(Ge, Ge.current), l) break;
        return null;
      case 22:
        return t.lanes = 0, Op(
          e,
          t,
          a,
          t.pendingProps
        );
      case 24:
        en(t, We, e.memoizedState.cache);
    }
    return Da(e, t, a);
  }
  function Up(e, t, a) {
    if (e !== null)
      if (e.memoizedProps !== t.pendingProps)
        Pe = !0;
      else {
        if (!Cu(e, a) && (t.flags & 128) === 0)
          return Pe = !1, Sg(
            e,
            t,
            a
          );
        Pe = (e.flags & 131072) !== 0;
      }
    else
      Pe = !1, ge && (t.flags & 1048576) !== 0 && vd(t, Al, t.index);
    switch (t.lanes = 0, t.tag) {
      case 16:
        e: {
          var l = t.pendingProps;
          if (e = Qn(t.elementType), t.type = e, typeof e == "function")
            Ro(e) ? (l = Fn(e, l), t.tag = 1, t = Mp(
              null,
              t,
              e,
              l,
              a
            )) : (t.tag = 0, t = Su(
              null,
              t,
              e,
              l,
              a
            ));
          else {
            if (e != null) {
              var o = e.$$typeof;
              if (o === L) {
                t.tag = 11, t = Tp(
                  null,
                  t,
                  e,
                  l,
                  a
                );
                break e;
              } else if (o === Z) {
                t.tag = 14, t = Ep(
                  null,
                  t,
                  e,
                  l,
                  a
                );
                break e;
              }
            }
            throw t = It(e) || e, Error(r(306, t, ""));
          }
        }
        return t;
      case 0:
        return Su(
          e,
          t,
          t.type,
          t.pendingProps,
          a
        );
      case 1:
        return l = t.type, o = Fn(
          l,
          t.pendingProps
        ), Mp(
          e,
          t,
          l,
          o,
          a
        );
      case 3:
        e: {
          if (Je(
            t,
            t.stateNode.containerInfo
          ), e === null) throw Error(r(387));
          l = t.pendingProps;
          var u = t.memoizedState;
          o = u.element, Wo(e, t), Rl(t, l, null, a);
          var p = t.memoizedState;
          if (l = p.cache, en(t, We, l), l !== u.cache && Go(
            t,
            [We],
            a,
            !0
          ), jl(), l = p.element, u.isDehydrated)
            if (u = {
              element: l,
              isDehydrated: !1,
              cache: p.cache
            }, t.updateQueue.baseState = u, t.memoizedState = u, t.flags & 256) {
              t = qp(
                e,
                t,
                l,
                a
              );
              break e;
            } else if (l !== o) {
              o = Xt(
                Error(r(424)),
                t
              ), Ol(o), t = qp(
                e,
                t,
                l,
                a
              );
              break e;
            } else {
              switch (e = t.stateNode.containerInfo, e.nodeType) {
                case 9:
                  e = e.body;
                  break;
                default:
                  e = e.nodeName === "HTML" ? e.ownerDocument.body : e;
              }
              for (qe = Jt(e.firstChild), ut = t, ge = !0, Ia = null, Zt = !0, a = Od(
                t,
                null,
                l,
                a
              ), t.child = a; a; )
                a.flags = a.flags & -3 | 4096, a = a.sibling;
            }
          else {
            if (Bn(), l === o) {
              t = Da(
                e,
                t,
                a
              );
              break e;
            }
            ft(e, t, l, a);
          }
          t = t.child;
        }
        return t;
      case 26:
        return or(e, t), e === null ? (a = Jh(
          t.type,
          null,
          t.pendingProps,
          null
        )) ? t.memoizedState = a : ge || (a = t.type, e = t.pendingProps, l = zr(
          fe.current
        ).createElement(a), l[ot] = t, l[wt] = e, dt(l, a, e), it(l), t.stateNode = l) : t.memoizedState = Jh(
          t.type,
          e.memoizedProps,
          t.pendingProps,
          e.memoizedState
        ), null;
      case 27:
        return ra(t), e === null && ge && (l = t.stateNode = Qh(
          t.type,
          t.pendingProps,
          fe.current
        ), ut = t, Zt = !0, o = qe, hn(t.type) ? (cc = o, qe = Jt(l.firstChild)) : qe = o), ft(
          e,
          t,
          t.pendingProps.children,
          a
        ), or(e, t), e === null && (t.flags |= 4194304), t.child;
      case 5:
        return e === null && ge && ((o = l = qe) && (l = eb(
          l,
          t.type,
          t.pendingProps,
          Zt
        ), l !== null ? (t.stateNode = l, ut = t, qe = Jt(l.firstChild), Zt = !1, o = !0) : o = !1), o || Pa(t)), ra(t), o = t.type, u = t.pendingProps, p = e !== null ? e.memoizedProps : null, l = u.children, lc(o, u) ? l = null : p !== null && lc(o, p) && (t.flags |= 32), t.memoizedState !== null && (o = iu(
          e,
          t,
          mg,
          null,
          null,
          a
        ), as._currentValue = o), or(e, t), ft(e, t, l, a), t.child;
      case 6:
        return e === null && ge && ((e = a = qe) && (a = tb(
          a,
          t.pendingProps,
          Zt
        ), a !== null ? (t.stateNode = a, ut = t, qe = null, e = !0) : e = !1), e || Pa(t)), null;
      case 13:
        return jp(e, t, a);
      case 4:
        return Je(
          t,
          t.stateNode.containerInfo
        ), l = t.pendingProps, e === null ? t.child = Kn(
          t,
          null,
          l,
          a
        ) : ft(e, t, l, a), t.child;
      case 11:
        return Tp(
          e,
          t,
          t.type,
          t.pendingProps,
          a
        );
      case 7:
        return ft(
          e,
          t,
          t.pendingProps,
          a
        ), t.child;
      case 8:
        return ft(
          e,
          t,
          t.pendingProps.children,
          a
        ), t.child;
      case 12:
        return ft(
          e,
          t,
          t.pendingProps.children,
          a
        ), t.child;
      case 10:
        return l = t.pendingProps, en(t, t.type, l.value), ft(e, t, l.children, a), t.child;
      case 9:
        return o = t.type._context, l = t.pendingProps.children, Xn(t), o = ct(o), l = l(o), t.flags |= 1, ft(e, t, l, a), t.child;
      case 14:
        return Ep(
          e,
          t,
          t.type,
          t.pendingProps,
          a
        );
      case 15:
        return Ap(
          e,
          t,
          t.type,
          t.pendingProps,
          a
        );
      case 19:
        return $p(e, t, a);
      case 31:
        return kg(e, t, a);
      case 22:
        return Op(
          e,
          t,
          a,
          t.pendingProps
        );
      case 24:
        return Xn(t), l = ct(We), e === null ? (o = Ko(), o === null && (o = Ne, u = Qo(), o.pooledCache = u, u.refCount++, u !== null && (o.pooledCacheLanes |= a), o = u), t.memoizedState = { parent: l, cache: o }, Fo(t), en(t, We, o)) : ((e.lanes & a) !== 0 && (Wo(e, t), Rl(t, null, null, a), jl()), o = e.memoizedState, u = t.memoizedState, o.parent !== l ? (o = { parent: l, cache: l }, t.memoizedState = o, t.lanes === 0 && (t.memoizedState = t.updateQueue.baseState = o), en(t, We, l)) : (l = u.cache, en(t, We, l), l !== o.cache && Go(
          t,
          [We],
          a,
          !0
        ))), ft(
          e,
          t,
          t.pendingProps.children,
          a
        ), t.child;
      case 29:
        throw t.pendingProps;
    }
    throw Error(r(156, t.tag));
  }
  function Ca(e) {
    e.flags |= 4;
  }
  function Nu(e, t, a, l, o) {
    if ((t = (e.mode & 32) !== 0) && (t = !1), t) {
      if (e.flags |= 16777216, (o & 335544128) === o)
        if (e.stateNode.complete) e.flags |= 8192;
        else if (fh()) e.flags |= 8192;
        else
          throw Zn = Zs, Jo;
    } else e.flags &= -16777217;
  }
  function Lp(e, t) {
    if (t.type !== "stylesheet" || (t.state.loading & 4) !== 0)
      e.flags &= -16777217;
    else if (e.flags |= 16777216, !e0(t))
      if (fh()) e.flags |= 8192;
      else
        throw Zn = Zs, Jo;
  }
  function cr(e, t) {
    t !== null && (e.flags |= 4), e.flags & 16384 && (t = e.tag !== 22 ? jn() : 536870912, e.lanes |= t, Gi |= t);
  }
  function Bl(e, t) {
    if (!ge)
      switch (e.tailMode) {
        case "hidden":
          t = e.tail;
          for (var a = null; t !== null; )
            t.alternate !== null && (a = t), t = t.sibling;
          a === null ? e.tail = null : a.sibling = null;
          break;
        case "collapsed":
          a = e.tail;
          for (var l = null; a !== null; )
            a.alternate !== null && (l = a), a = a.sibling;
          l === null ? t || e.tail === null ? e.tail = null : e.tail.sibling = null : l.sibling = null;
      }
  }
  function je(e) {
    var t = e.alternate !== null && e.alternate.child === e.child, a = 0, l = 0;
    if (t)
      for (var o = e.child; o !== null; )
        a |= o.lanes | o.childLanes, l |= o.subtreeFlags & 65011712, l |= o.flags & 65011712, o.return = e, o = o.sibling;
    else
      for (o = e.child; o !== null; )
        a |= o.lanes | o.childLanes, l |= o.subtreeFlags, l |= o.flags, o.return = e, o = o.sibling;
    return e.subtreeFlags |= l, e.childLanes = a, t;
  }
  function zg(e, t, a) {
    var l = t.pendingProps;
    switch (Ho(t), t.tag) {
      case 16:
      case 15:
      case 0:
      case 11:
      case 7:
      case 8:
      case 12:
      case 9:
      case 14:
        return je(t), null;
      case 1:
        return je(t), null;
      case 3:
        return a = t.stateNode, l = null, e !== null && (l = e.memoizedState.cache), t.memoizedState.cache !== l && (t.flags |= 2048), Ea(We), He(), a.pendingContext && (a.context = a.pendingContext, a.pendingContext = null), (e === null || e.child === null) && (Ni(t) ? Ca(t) : e === null || e.memoizedState.isDehydrated && (t.flags & 256) === 0 || (t.flags |= 1024, Bo())), je(t), null;
      case 26:
        var o = t.type, u = t.memoizedState;
        return e === null ? (Ca(t), u !== null ? (je(t), Lp(t, u)) : (je(t), Nu(
          t,
          o,
          null,
          l,
          a
        ))) : u ? u !== e.memoizedState ? (Ca(t), je(t), Lp(t, u)) : (je(t), t.flags &= -16777217) : (e = e.memoizedProps, e !== l && Ca(t), je(t), Nu(
          t,
          o,
          e,
          l,
          a
        )), null;
      case 27:
        if (yt(t), a = fe.current, o = t.type, e !== null && t.stateNode != null)
          e.memoizedProps !== l && Ca(t);
        else {
          if (!l) {
            if (t.stateNode === null)
              throw Error(r(166));
            return je(t), null;
          }
          e = I.current, Ni(t) ? bd(t) : (e = Qh(o, l, a), t.stateNode = e, Ca(t));
        }
        return je(t), null;
      case 5:
        if (yt(t), o = t.type, e !== null && t.stateNode != null)
          e.memoizedProps !== l && Ca(t);
        else {
          if (!l) {
            if (t.stateNode === null)
              throw Error(r(166));
            return je(t), null;
          }
          if (u = I.current, Ni(t))
            bd(t);
          else {
            var p = zr(
              fe.current
            );
            switch (u) {
              case 1:
                u = p.createElementNS(
                  "http://www.w3.org/2000/svg",
                  o
                );
                break;
              case 2:
                u = p.createElementNS(
                  "http://www.w3.org/1998/Math/MathML",
                  o
                );
                break;
              default:
                switch (o) {
                  case "svg":
                    u = p.createElementNS(
                      "http://www.w3.org/2000/svg",
                      o
                    );
                    break;
                  case "math":
                    u = p.createElementNS(
                      "http://www.w3.org/1998/Math/MathML",
                      o
                    );
                    break;
                  case "script":
                    u = p.createElement("div"), u.innerHTML = "<script><\/script>", u = u.removeChild(
                      u.firstChild
                    );
                    break;
                  case "select":
                    u = typeof l.is == "string" ? p.createElement("select", {
                      is: l.is
                    }) : p.createElement("select"), l.multiple ? u.multiple = !0 : l.size && (u.size = l.size);
                    break;
                  default:
                    u = typeof l.is == "string" ? p.createElement(o, { is: l.is }) : p.createElement(o);
                }
            }
            u[ot] = t, u[wt] = l;
            e: for (p = t.child; p !== null; ) {
              if (p.tag === 5 || p.tag === 6)
                u.appendChild(p.stateNode);
              else if (p.tag !== 4 && p.tag !== 27 && p.child !== null) {
                p.child.return = p, p = p.child;
                continue;
              }
              if (p === t) break e;
              for (; p.sibling === null; ) {
                if (p.return === null || p.return === t)
                  break e;
                p = p.return;
              }
              p.sibling.return = p.return, p = p.sibling;
            }
            t.stateNode = u;
            e: switch (dt(u, o, l), o) {
              case "button":
              case "input":
              case "select":
              case "textarea":
                l = !!l.autoFocus;
                break e;
              case "img":
                l = !0;
                break e;
              default:
                l = !1;
            }
            l && Ca(t);
          }
        }
        return je(t), Nu(
          t,
          t.type,
          e === null ? null : e.memoizedProps,
          t.pendingProps,
          a
        ), null;
      case 6:
        if (e && t.stateNode != null)
          e.memoizedProps !== l && Ca(t);
        else {
          if (typeof l != "string" && t.stateNode === null)
            throw Error(r(166));
          if (e = fe.current, Ni(t)) {
            if (e = t.stateNode, a = t.memoizedProps, l = null, o = ut, o !== null)
              switch (o.tag) {
                case 27:
                case 5:
                  l = o.memoizedProps;
              }
            e[ot] = t, e = !!(e.nodeValue === a || l !== null && l.suppressHydrationWarning === !0 || jh(e.nodeValue, a)), e || Pa(t, !0);
          } else
            e = zr(e).createTextNode(
              l
            ), e[ot] = t, t.stateNode = e;
        }
        return je(t), null;
      case 31:
        if (a = t.memoizedState, e === null || e.memoizedState !== null) {
          if (l = Ni(t), a !== null) {
            if (e === null) {
              if (!l) throw Error(r(318));
              if (e = t.memoizedState, e = e !== null ? e.dehydrated : null, !e) throw Error(r(557));
              e[ot] = t;
            } else
              Bn(), (t.flags & 128) === 0 && (t.memoizedState = null), t.flags |= 4;
            je(t), e = !1;
          } else
            a = Bo(), e !== null && e.memoizedState !== null && (e.memoizedState.hydrationErrors = a), e = !0;
          if (!e)
            return t.flags & 256 ? (jt(t), t) : (jt(t), null);
          if ((t.flags & 128) !== 0)
            throw Error(r(558));
        }
        return je(t), null;
      case 13:
        if (l = t.memoizedState, e === null || e.memoizedState !== null && e.memoizedState.dehydrated !== null) {
          if (o = Ni(t), l !== null && l.dehydrated !== null) {
            if (e === null) {
              if (!o) throw Error(r(318));
              if (o = t.memoizedState, o = o !== null ? o.dehydrated : null, !o) throw Error(r(317));
              o[ot] = t;
            } else
              Bn(), (t.flags & 128) === 0 && (t.memoizedState = null), t.flags |= 4;
            je(t), o = !1;
          } else
            o = Bo(), e !== null && e.memoizedState !== null && (e.memoizedState.hydrationErrors = o), o = !0;
          if (!o)
            return t.flags & 256 ? (jt(t), t) : (jt(t), null);
        }
        return jt(t), (t.flags & 128) !== 0 ? (t.lanes = a, t) : (a = l !== null, e = e !== null && e.memoizedState !== null, a && (l = t.child, o = null, l.alternate !== null && l.alternate.memoizedState !== null && l.alternate.memoizedState.cachePool !== null && (o = l.alternate.memoizedState.cachePool.pool), u = null, l.memoizedState !== null && l.memoizedState.cachePool !== null && (u = l.memoizedState.cachePool.pool), u !== o && (l.flags |= 2048)), a !== e && a && (t.child.flags |= 8192), cr(t, t.updateQueue), je(t), null);
      case 4:
        return He(), e === null && ec(t.stateNode.containerInfo), je(t), null;
      case 10:
        return Ea(t.type), je(t), null;
      case 19:
        if (Y(Ge), l = t.memoizedState, l === null) return je(t), null;
        if (o = (t.flags & 128) !== 0, u = l.rendering, u === null)
          if (o) Bl(l, !1);
          else {
            if (Ve !== 0 || e !== null && (e.flags & 128) !== 0)
              for (e = t.child; e !== null; ) {
                if (u = Ws(e), u !== null) {
                  for (t.flags |= 128, Bl(l, !1), e = u.updateQueue, t.updateQueue = e, cr(t, e), t.subtreeFlags = 0, e = a, a = t.child; a !== null; )
                    pd(a, e), a = a.sibling;
                  return K(
                    Ge,
                    Ge.current & 1 | 2
                  ), ge && za(t, l.treeForkCount), t.child;
                }
                e = e.sibling;
              }
            l.tail !== null && Me() > mr && (t.flags |= 128, o = !0, Bl(l, !1), t.lanes = 4194304);
          }
        else {
          if (!o)
            if (e = Ws(u), e !== null) {
              if (t.flags |= 128, o = !0, e = e.updateQueue, t.updateQueue = e, cr(t, e), Bl(l, !0), l.tail === null && l.tailMode === "hidden" && !u.alternate && !ge)
                return je(t), null;
            } else
              2 * Me() - l.renderingStartTime > mr && a !== 536870912 && (t.flags |= 128, o = !0, Bl(l, !1), t.lanes = 4194304);
          l.isBackwards ? (u.sibling = t.child, t.child = u) : (e = l.last, e !== null ? e.sibling = u : t.child = u, l.last = u);
        }
        return l.tail !== null ? (e = l.tail, l.rendering = e, l.tail = e.sibling, l.renderingStartTime = Me(), e.sibling = null, a = Ge.current, K(
          Ge,
          o ? a & 1 | 2 : a & 1
        ), ge && za(t, l.treeForkCount), e) : (je(t), null);
      case 22:
      case 23:
        return jt(t), tu(), l = t.memoizedState !== null, e !== null ? e.memoizedState !== null !== l && (t.flags |= 8192) : l && (t.flags |= 8192), l ? (a & 536870912) !== 0 && (t.flags & 128) === 0 && (je(t), t.subtreeFlags & 6 && (t.flags |= 8192)) : je(t), a = t.updateQueue, a !== null && cr(t, a.retryQueue), a = null, e !== null && e.memoizedState !== null && e.memoizedState.cachePool !== null && (a = e.memoizedState.cachePool.pool), l = null, t.memoizedState !== null && t.memoizedState.cachePool !== null && (l = t.memoizedState.cachePool.pool), l !== a && (t.flags |= 2048), e !== null && Y(Gn), null;
      case 24:
        return a = null, e !== null && (a = e.memoizedState.cache), t.memoizedState.cache !== a && (t.flags |= 2048), Ea(We), je(t), null;
      case 25:
        return null;
      case 30:
        return null;
    }
    throw Error(r(156, t.tag));
  }
  function Tg(e, t) {
    switch (Ho(t), t.tag) {
      case 1:
        return e = t.flags, e & 65536 ? (t.flags = e & -65537 | 128, t) : null;
      case 3:
        return Ea(We), He(), e = t.flags, (e & 65536) !== 0 && (e & 128) === 0 ? (t.flags = e & -65537 | 128, t) : null;
      case 26:
      case 27:
      case 5:
        return yt(t), null;
      case 31:
        if (t.memoizedState !== null) {
          if (jt(t), t.alternate === null)
            throw Error(r(340));
          Bn();
        }
        return e = t.flags, e & 65536 ? (t.flags = e & -65537 | 128, t) : null;
      case 13:
        if (jt(t), e = t.memoizedState, e !== null && e.dehydrated !== null) {
          if (t.alternate === null)
            throw Error(r(340));
          Bn();
        }
        return e = t.flags, e & 65536 ? (t.flags = e & -65537 | 128, t) : null;
      case 19:
        return Y(Ge), null;
      case 4:
        return He(), null;
      case 10:
        return Ea(t.type), null;
      case 22:
      case 23:
        return jt(t), tu(), e !== null && Y(Gn), e = t.flags, e & 65536 ? (t.flags = e & -65537 | 128, t) : null;
      case 24:
        return Ea(We), null;
      case 25:
        return null;
      default:
        return null;
    }
  }
  function Hp(e, t) {
    switch (Ho(t), t.tag) {
      case 3:
        Ea(We), He();
        break;
      case 26:
      case 27:
      case 5:
        yt(t);
        break;
      case 4:
        He();
        break;
      case 31:
        t.memoizedState !== null && jt(t);
        break;
      case 13:
        jt(t);
        break;
      case 19:
        Y(Ge);
        break;
      case 10:
        Ea(t.type);
        break;
      case 22:
      case 23:
        jt(t), tu(), e !== null && Y(Gn);
        break;
      case 24:
        Ea(We);
    }
  }
  function Vl(e, t) {
    try {
      var a = t.updateQueue, l = a !== null ? a.lastEffect : null;
      if (l !== null) {
        var o = l.next;
        a = o;
        do {
          if ((a.tag & e) === e) {
            l = void 0;
            var u = a.create, p = a.inst;
            l = u(), p.destroy = l;
          }
          a = a.next;
        } while (a !== o);
      }
    } catch (b) {
      Ae(t, t.return, b);
    }
  }
  function rn(e, t, a) {
    try {
      var l = t.updateQueue, o = l !== null ? l.lastEffect : null;
      if (o !== null) {
        var u = o.next;
        l = u;
        do {
          if ((l.tag & e) === e) {
            var p = l.inst, b = p.destroy;
            if (b !== void 0) {
              p.destroy = void 0, o = t;
              var w = a, D = b;
              try {
                D();
              } catch (U) {
                Ae(
                  o,
                  w,
                  U
                );
              }
            }
          }
          l = l.next;
        } while (l !== u);
      }
    } catch (U) {
      Ae(t, t.return, U);
    }
  }
  function Yp(e) {
    var t = e.updateQueue;
    if (t !== null) {
      var a = e.stateNode;
      try {
        Cd(t, a);
      } catch (l) {
        Ae(e, e.return, l);
      }
    }
  }
  function Bp(e, t, a) {
    a.props = Fn(
      e.type,
      e.memoizedProps
    ), a.state = e.memoizedState;
    try {
      a.componentWillUnmount();
    } catch (l) {
      Ae(e, t, l);
    }
  }
  function Xl(e, t) {
    try {
      var a = e.ref;
      if (a !== null) {
        switch (e.tag) {
          case 26:
          case 27:
          case 5:
            var l = e.stateNode;
            break;
          case 30:
            l = e.stateNode;
            break;
          default:
            l = e.stateNode;
        }
        typeof a == "function" ? e.refCleanup = a(l) : a.current = l;
      }
    } catch (o) {
      Ae(e, t, o);
    }
  }
  function pa(e, t) {
    var a = e.ref, l = e.refCleanup;
    if (a !== null)
      if (typeof l == "function")
        try {
          l();
        } catch (o) {
          Ae(e, t, o);
        } finally {
          e.refCleanup = null, e = e.alternate, e != null && (e.refCleanup = null);
        }
      else if (typeof a == "function")
        try {
          a(null);
        } catch (o) {
          Ae(e, t, o);
        }
      else a.current = null;
  }
  function Vp(e) {
    var t = e.type, a = e.memoizedProps, l = e.stateNode;
    try {
      e: switch (t) {
        case "button":
        case "input":
        case "select":
        case "textarea":
          a.autoFocus && l.focus();
          break e;
        case "img":
          a.src ? l.src = a.src : a.srcSet && (l.srcset = a.srcSet);
      }
    } catch (o) {
      Ae(e, e.return, o);
    }
  }
  function Mu(e, t, a) {
    try {
      var l = e.stateNode;
      Kg(l, e.type, a, t), l[wt] = t;
    } catch (o) {
      Ae(e, e.return, o);
    }
  }
  function Xp(e) {
    return e.tag === 5 || e.tag === 3 || e.tag === 26 || e.tag === 27 && hn(e.type) || e.tag === 4;
  }
  function qu(e) {
    e: for (; ; ) {
      for (; e.sibling === null; ) {
        if (e.return === null || Xp(e.return)) return null;
        e = e.return;
      }
      for (e.sibling.return = e.return, e = e.sibling; e.tag !== 5 && e.tag !== 6 && e.tag !== 18; ) {
        if (e.tag === 27 && hn(e.type) || e.flags & 2 || e.child === null || e.tag === 4) continue e;
        e.child.return = e, e = e.child;
      }
      if (!(e.flags & 2)) return e.stateNode;
    }
  }
  function ju(e, t, a) {
    var l = e.tag;
    if (l === 5 || l === 6)
      e = e.stateNode, t ? (a.nodeType === 9 ? a.body : a.nodeName === "HTML" ? a.ownerDocument.body : a).insertBefore(e, t) : (t = a.nodeType === 9 ? a.body : a.nodeName === "HTML" ? a.ownerDocument.body : a, t.appendChild(e), a = a._reactRootContainer, a != null || t.onclick !== null || (t.onclick = wa));
    else if (l !== 4 && (l === 27 && hn(e.type) && (a = e.stateNode, t = null), e = e.child, e !== null))
      for (ju(e, t, a), e = e.sibling; e !== null; )
        ju(e, t, a), e = e.sibling;
  }
  function fr(e, t, a) {
    var l = e.tag;
    if (l === 5 || l === 6)
      e = e.stateNode, t ? a.insertBefore(e, t) : a.appendChild(e);
    else if (l !== 4 && (l === 27 && hn(e.type) && (a = e.stateNode), e = e.child, e !== null))
      for (fr(e, t, a), e = e.sibling; e !== null; )
        fr(e, t, a), e = e.sibling;
  }
  function Gp(e) {
    var t = e.stateNode, a = e.memoizedProps;
    try {
      for (var l = e.type, o = t.attributes; o.length; )
        t.removeAttributeNode(o[0]);
      dt(t, l, a), t[ot] = e, t[wt] = a;
    } catch (u) {
      Ae(e, e.return, u);
    }
  }
  var Na = !1, et = !1, Ru = !1, Qp = typeof WeakSet == "function" ? WeakSet : Set, lt = null;
  function Eg(e, t) {
    if (e = e.containerInfo, nc = Nr, e = id(e), Oo(e)) {
      if ("selectionStart" in e)
        var a = {
          start: e.selectionStart,
          end: e.selectionEnd
        };
      else
        e: {
          a = (a = e.ownerDocument) && a.defaultView || window;
          var l = a.getSelection && a.getSelection();
          if (l && l.rangeCount !== 0) {
            a = l.anchorNode;
            var o = l.anchorOffset, u = l.focusNode;
            l = l.focusOffset;
            try {
              a.nodeType, u.nodeType;
            } catch {
              a = null;
              break e;
            }
            var p = 0, b = -1, w = -1, D = 0, U = 0, B = e, C = null;
            t: for (; ; ) {
              for (var M; B !== a || o !== 0 && B.nodeType !== 3 || (b = p + o), B !== u || l !== 0 && B.nodeType !== 3 || (w = p + l), B.nodeType === 3 && (p += B.nodeValue.length), (M = B.firstChild) !== null; )
                C = B, B = M;
              for (; ; ) {
                if (B === e) break t;
                if (C === a && ++D === o && (b = p), C === u && ++U === l && (w = p), (M = B.nextSibling) !== null) break;
                B = C, C = B.parentNode;
              }
              B = M;
            }
            a = b === -1 || w === -1 ? null : { start: b, end: w };
          } else a = null;
        }
      a = a || { start: 0, end: 0 };
    } else a = null;
    for (ic = { focusedElem: e, selectionRange: a }, Nr = !1, lt = t; lt !== null; )
      if (t = lt, e = t.child, (t.subtreeFlags & 1028) !== 0 && e !== null)
        e.return = t, lt = e;
      else
        for (; lt !== null; ) {
          switch (t = lt, u = t.alternate, e = t.flags, t.tag) {
            case 0:
              if ((e & 4) !== 0 && (e = t.updateQueue, e = e !== null ? e.events : null, e !== null))
                for (a = 0; a < e.length; a++)
                  o = e[a], o.ref.impl = o.nextImpl;
              break;
            case 11:
            case 15:
              break;
            case 1:
              if ((e & 1024) !== 0 && u !== null) {
                e = void 0, a = t, o = u.memoizedProps, u = u.memoizedState, l = a.stateNode;
                try {
                  var P = Fn(
                    a.type,
                    o
                  );
                  e = l.getSnapshotBeforeUpdate(
                    P,
                    u
                  ), l.__reactInternalSnapshotBeforeUpdate = e;
                } catch (ie) {
                  Ae(
                    a,
                    a.return,
                    ie
                  );
                }
              }
              break;
            case 3:
              if ((e & 1024) !== 0) {
                if (e = t.stateNode.containerInfo, a = e.nodeType, a === 9)
                  rc(e);
                else if (a === 1)
                  switch (e.nodeName) {
                    case "HEAD":
                    case "HTML":
                    case "BODY":
                      rc(e);
                      break;
                    default:
                      e.textContent = "";
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
              if ((e & 1024) !== 0) throw Error(r(163));
          }
          if (e = t.sibling, e !== null) {
            e.return = t.return, lt = e;
            break;
          }
          lt = t.return;
        }
  }
  function Zp(e, t, a) {
    var l = a.flags;
    switch (a.tag) {
      case 0:
      case 11:
      case 15:
        qa(e, a), l & 4 && Vl(5, a);
        break;
      case 1:
        if (qa(e, a), l & 4)
          if (e = a.stateNode, t === null)
            try {
              e.componentDidMount();
            } catch (p) {
              Ae(a, a.return, p);
            }
          else {
            var o = Fn(
              a.type,
              t.memoizedProps
            );
            t = t.memoizedState;
            try {
              e.componentDidUpdate(
                o,
                t,
                e.__reactInternalSnapshotBeforeUpdate
              );
            } catch (p) {
              Ae(
                a,
                a.return,
                p
              );
            }
          }
        l & 64 && Yp(a), l & 512 && Xl(a, a.return);
        break;
      case 3:
        if (qa(e, a), l & 64 && (e = a.updateQueue, e !== null)) {
          if (t = null, a.child !== null)
            switch (a.child.tag) {
              case 27:
              case 5:
                t = a.child.stateNode;
                break;
              case 1:
                t = a.child.stateNode;
            }
          try {
            Cd(e, t);
          } catch (p) {
            Ae(a, a.return, p);
          }
        }
        break;
      case 27:
        t === null && l & 4 && Gp(a);
      case 26:
      case 5:
        qa(e, a), t === null && l & 4 && Vp(a), l & 512 && Xl(a, a.return);
        break;
      case 12:
        qa(e, a);
        break;
      case 31:
        qa(e, a), l & 4 && Fp(e, a);
        break;
      case 13:
        qa(e, a), l & 4 && Wp(e, a), l & 64 && (e = a.memoizedState, e !== null && (e = e.dehydrated, e !== null && (a = Rg.bind(
          null,
          a
        ), ab(e, a))));
        break;
      case 22:
        if (l = a.memoizedState !== null || Na, !l) {
          t = t !== null && t.memoizedState !== null || et, o = Na;
          var u = et;
          Na = l, (et = t) && !u ? ja(
            e,
            a,
            (a.subtreeFlags & 8772) !== 0
          ) : qa(e, a), Na = o, et = u;
        }
        break;
      case 30:
        break;
      default:
        qa(e, a);
    }
  }
  function Kp(e) {
    var t = e.alternate;
    t !== null && (e.alternate = null, Kp(t)), e.child = null, e.deletions = null, e.sibling = null, e.tag === 5 && (t = e.stateNode, t !== null && fo(t)), e.stateNode = null, e.return = null, e.dependencies = null, e.memoizedProps = null, e.memoizedState = null, e.pendingProps = null, e.stateNode = null, e.updateQueue = null;
  }
  var $e = null, St = !1;
  function Ma(e, t, a) {
    for (a = a.child; a !== null; )
      Jp(e, t, a), a = a.sibling;
  }
  function Jp(e, t, a) {
    if (tt && typeof tt.onCommitFiberUnmount == "function")
      try {
        tt.onCommitFiberUnmount(Za, a);
      } catch {
      }
    switch (a.tag) {
      case 26:
        et || pa(a, t), Ma(
          e,
          t,
          a
        ), a.memoizedState ? a.memoizedState.count-- : a.stateNode && (a = a.stateNode, a.parentNode.removeChild(a));
        break;
      case 27:
        et || pa(a, t);
        var l = $e, o = St;
        hn(a.type) && ($e = a.stateNode, St = !1), Ma(
          e,
          t,
          a
        ), Pl(a.stateNode), $e = l, St = o;
        break;
      case 5:
        et || pa(a, t);
      case 6:
        if (l = $e, o = St, $e = null, Ma(
          e,
          t,
          a
        ), $e = l, St = o, $e !== null)
          if (St)
            try {
              ($e.nodeType === 9 ? $e.body : $e.nodeName === "HTML" ? $e.ownerDocument.body : $e).removeChild(a.stateNode);
            } catch (u) {
              Ae(
                a,
                t,
                u
              );
            }
          else
            try {
              $e.removeChild(a.stateNode);
            } catch (u) {
              Ae(
                a,
                t,
                u
              );
            }
        break;
      case 18:
        $e !== null && (St ? (e = $e, Yh(
          e.nodeType === 9 ? e.body : e.nodeName === "HTML" ? e.ownerDocument.body : e,
          a.stateNode
        ), Pi(e)) : Yh($e, a.stateNode));
        break;
      case 4:
        l = $e, o = St, $e = a.stateNode.containerInfo, St = !0, Ma(
          e,
          t,
          a
        ), $e = l, St = o;
        break;
      case 0:
      case 11:
      case 14:
      case 15:
        rn(2, a, t), et || rn(4, a, t), Ma(
          e,
          t,
          a
        );
        break;
      case 1:
        et || (pa(a, t), l = a.stateNode, typeof l.componentWillUnmount == "function" && Bp(
          a,
          t,
          l
        )), Ma(
          e,
          t,
          a
        );
        break;
      case 21:
        Ma(
          e,
          t,
          a
        );
        break;
      case 22:
        et = (l = et) || a.memoizedState !== null, Ma(
          e,
          t,
          a
        ), et = l;
        break;
      default:
        Ma(
          e,
          t,
          a
        );
    }
  }
  function Fp(e, t) {
    if (t.memoizedState === null && (e = t.alternate, e !== null && (e = e.memoizedState, e !== null))) {
      e = e.dehydrated;
      try {
        Pi(e);
      } catch (a) {
        Ae(t, t.return, a);
      }
    }
  }
  function Wp(e, t) {
    if (t.memoizedState === null && (e = t.alternate, e !== null && (e = e.memoizedState, e !== null && (e = e.dehydrated, e !== null))))
      try {
        Pi(e);
      } catch (a) {
        Ae(t, t.return, a);
      }
  }
  function Ag(e) {
    switch (e.tag) {
      case 31:
      case 13:
      case 19:
        var t = e.stateNode;
        return t === null && (t = e.stateNode = new Qp()), t;
      case 22:
        return e = e.stateNode, t = e._retryCache, t === null && (t = e._retryCache = new Qp()), t;
      default:
        throw Error(r(435, e.tag));
    }
  }
  function dr(e, t) {
    var a = Ag(e);
    t.forEach(function(l) {
      if (!a.has(l)) {
        a.add(l);
        var o = $g.bind(null, e, l);
        l.then(o, o);
      }
    });
  }
  function zt(e, t) {
    var a = t.deletions;
    if (a !== null)
      for (var l = 0; l < a.length; l++) {
        var o = a[l], u = e, p = t, b = p;
        e: for (; b !== null; ) {
          switch (b.tag) {
            case 27:
              if (hn(b.type)) {
                $e = b.stateNode, St = !1;
                break e;
              }
              break;
            case 5:
              $e = b.stateNode, St = !1;
              break e;
            case 3:
            case 4:
              $e = b.stateNode.containerInfo, St = !0;
              break e;
          }
          b = b.return;
        }
        if ($e === null) throw Error(r(160));
        Jp(u, p, o), $e = null, St = !1, u = o.alternate, u !== null && (u.return = null), o.return = null;
      }
    if (t.subtreeFlags & 13886)
      for (t = t.child; t !== null; )
        Ip(t, e), t = t.sibling;
  }
  var ta = null;
  function Ip(e, t) {
    var a = e.alternate, l = e.flags;
    switch (e.tag) {
      case 0:
      case 11:
      case 14:
      case 15:
        zt(t, e), Tt(e), l & 4 && (rn(3, e, e.return), Vl(3, e), rn(5, e, e.return));
        break;
      case 1:
        zt(t, e), Tt(e), l & 512 && (et || a === null || pa(a, a.return)), l & 64 && Na && (e = e.updateQueue, e !== null && (l = e.callbacks, l !== null && (a = e.shared.hiddenCallbacks, e.shared.hiddenCallbacks = a === null ? l : a.concat(l))));
        break;
      case 26:
        var o = ta;
        if (zt(t, e), Tt(e), l & 512 && (et || a === null || pa(a, a.return)), l & 4) {
          var u = a !== null ? a.memoizedState : null;
          if (l = e.memoizedState, a === null)
            if (l === null)
              if (e.stateNode === null) {
                e: {
                  l = e.type, a = e.memoizedProps, o = o.ownerDocument || o;
                  t: switch (l) {
                    case "title":
                      u = o.getElementsByTagName("title")[0], (!u || u[gl] || u[ot] || u.namespaceURI === "http://www.w3.org/2000/svg" || u.hasAttribute("itemprop")) && (u = o.createElement(l), o.head.insertBefore(
                        u,
                        o.querySelector("head > title")
                      )), dt(u, l, a), u[ot] = e, it(u), l = u;
                      break e;
                    case "link":
                      var p = Ih(
                        "link",
                        "href",
                        o
                      ).get(l + (a.href || ""));
                      if (p) {
                        for (var b = 0; b < p.length; b++)
                          if (u = p[b], u.getAttribute("href") === (a.href == null || a.href === "" ? null : a.href) && u.getAttribute("rel") === (a.rel == null ? null : a.rel) && u.getAttribute("title") === (a.title == null ? null : a.title) && u.getAttribute("crossorigin") === (a.crossOrigin == null ? null : a.crossOrigin)) {
                            p.splice(b, 1);
                            break t;
                          }
                      }
                      u = o.createElement(l), dt(u, l, a), o.head.appendChild(u);
                      break;
                    case "meta":
                      if (p = Ih(
                        "meta",
                        "content",
                        o
                      ).get(l + (a.content || ""))) {
                        for (b = 0; b < p.length; b++)
                          if (u = p[b], u.getAttribute("content") === (a.content == null ? null : "" + a.content) && u.getAttribute("name") === (a.name == null ? null : a.name) && u.getAttribute("property") === (a.property == null ? null : a.property) && u.getAttribute("http-equiv") === (a.httpEquiv == null ? null : a.httpEquiv) && u.getAttribute("charset") === (a.charSet == null ? null : a.charSet)) {
                            p.splice(b, 1);
                            break t;
                          }
                      }
                      u = o.createElement(l), dt(u, l, a), o.head.appendChild(u);
                      break;
                    default:
                      throw Error(r(468, l));
                  }
                  u[ot] = e, it(u), l = u;
                }
                e.stateNode = l;
              } else
                Ph(
                  o,
                  e.type,
                  e.stateNode
                );
            else
              e.stateNode = Wh(
                o,
                l,
                e.memoizedProps
              );
          else
            u !== l ? (u === null ? a.stateNode !== null && (a = a.stateNode, a.parentNode.removeChild(a)) : u.count--, l === null ? Ph(
              o,
              e.type,
              e.stateNode
            ) : Wh(
              o,
              l,
              e.memoizedProps
            )) : l === null && e.stateNode !== null && Mu(
              e,
              e.memoizedProps,
              a.memoizedProps
            );
        }
        break;
      case 27:
        zt(t, e), Tt(e), l & 512 && (et || a === null || pa(a, a.return)), a !== null && l & 4 && Mu(
          e,
          e.memoizedProps,
          a.memoizedProps
        );
        break;
      case 5:
        if (zt(t, e), Tt(e), l & 512 && (et || a === null || pa(a, a.return)), e.flags & 32) {
          o = e.stateNode;
          try {
            wi(o, "");
          } catch (P) {
            Ae(e, e.return, P);
          }
        }
        l & 4 && e.stateNode != null && (o = e.memoizedProps, Mu(
          e,
          o,
          a !== null ? a.memoizedProps : o
        )), l & 1024 && (Ru = !0);
        break;
      case 6:
        if (zt(t, e), Tt(e), l & 4) {
          if (e.stateNode === null)
            throw Error(r(162));
          l = e.memoizedProps, a = e.stateNode;
          try {
            a.nodeValue = l;
          } catch (P) {
            Ae(e, e.return, P);
          }
        }
        break;
      case 3:
        if (Ar = null, o = ta, ta = Tr(t.containerInfo), zt(t, e), ta = o, Tt(e), l & 4 && a !== null && a.memoizedState.isDehydrated)
          try {
            Pi(t.containerInfo);
          } catch (P) {
            Ae(e, e.return, P);
          }
        Ru && (Ru = !1, Pp(e));
        break;
      case 4:
        l = ta, ta = Tr(
          e.stateNode.containerInfo
        ), zt(t, e), Tt(e), ta = l;
        break;
      case 12:
        zt(t, e), Tt(e);
        break;
      case 31:
        zt(t, e), Tt(e), l & 4 && (l = e.updateQueue, l !== null && (e.updateQueue = null, dr(e, l)));
        break;
      case 13:
        zt(t, e), Tt(e), e.child.flags & 8192 && e.memoizedState !== null != (a !== null && a.memoizedState !== null) && (hr = Me()), l & 4 && (l = e.updateQueue, l !== null && (e.updateQueue = null, dr(e, l)));
        break;
      case 22:
        o = e.memoizedState !== null;
        var w = a !== null && a.memoizedState !== null, D = Na, U = et;
        if (Na = D || o, et = U || w, zt(t, e), et = U, Na = D, Tt(e), l & 8192)
          e: for (t = e.stateNode, t._visibility = o ? t._visibility & -2 : t._visibility | 1, o && (a === null || w || Na || et || Wn(e)), a = null, t = e; ; ) {
            if (t.tag === 5 || t.tag === 26) {
              if (a === null) {
                w = a = t;
                try {
                  if (u = w.stateNode, o)
                    p = u.style, typeof p.setProperty == "function" ? p.setProperty("display", "none", "important") : p.display = "none";
                  else {
                    b = w.stateNode;
                    var B = w.memoizedProps.style, C = B != null && B.hasOwnProperty("display") ? B.display : null;
                    b.style.display = C == null || typeof C == "boolean" ? "" : ("" + C).trim();
                  }
                } catch (P) {
                  Ae(w, w.return, P);
                }
              }
            } else if (t.tag === 6) {
              if (a === null) {
                w = t;
                try {
                  w.stateNode.nodeValue = o ? "" : w.memoizedProps;
                } catch (P) {
                  Ae(w, w.return, P);
                }
              }
            } else if (t.tag === 18) {
              if (a === null) {
                w = t;
                try {
                  var M = w.stateNode;
                  o ? Bh(M, !0) : Bh(w.stateNode, !1);
                } catch (P) {
                  Ae(w, w.return, P);
                }
              }
            } else if ((t.tag !== 22 && t.tag !== 23 || t.memoizedState === null || t === e) && t.child !== null) {
              t.child.return = t, t = t.child;
              continue;
            }
            if (t === e) break e;
            for (; t.sibling === null; ) {
              if (t.return === null || t.return === e) break e;
              a === t && (a = null), t = t.return;
            }
            a === t && (a = null), t.sibling.return = t.return, t = t.sibling;
          }
        l & 4 && (l = e.updateQueue, l !== null && (a = l.retryQueue, a !== null && (l.retryQueue = null, dr(e, a))));
        break;
      case 19:
        zt(t, e), Tt(e), l & 4 && (l = e.updateQueue, l !== null && (e.updateQueue = null, dr(e, l)));
        break;
      case 30:
        break;
      case 21:
        break;
      default:
        zt(t, e), Tt(e);
    }
  }
  function Tt(e) {
    var t = e.flags;
    if (t & 2) {
      try {
        for (var a, l = e.return; l !== null; ) {
          if (Xp(l)) {
            a = l;
            break;
          }
          l = l.return;
        }
        if (a == null) throw Error(r(160));
        switch (a.tag) {
          case 27:
            var o = a.stateNode, u = qu(e);
            fr(e, u, o);
            break;
          case 5:
            var p = a.stateNode;
            a.flags & 32 && (wi(p, ""), a.flags &= -33);
            var b = qu(e);
            fr(e, b, p);
            break;
          case 3:
          case 4:
            var w = a.stateNode.containerInfo, D = qu(e);
            ju(
              e,
              D,
              w
            );
            break;
          default:
            throw Error(r(161));
        }
      } catch (U) {
        Ae(e, e.return, U);
      }
      e.flags &= -3;
    }
    t & 4096 && (e.flags &= -4097);
  }
  function Pp(e) {
    if (e.subtreeFlags & 1024)
      for (e = e.child; e !== null; ) {
        var t = e;
        Pp(t), t.tag === 5 && t.flags & 1024 && t.stateNode.reset(), e = e.sibling;
      }
  }
  function qa(e, t) {
    if (t.subtreeFlags & 8772)
      for (t = t.child; t !== null; )
        Zp(e, t.alternate, t), t = t.sibling;
  }
  function Wn(e) {
    for (e = e.child; e !== null; ) {
      var t = e;
      switch (t.tag) {
        case 0:
        case 11:
        case 14:
        case 15:
          rn(4, t, t.return), Wn(t);
          break;
        case 1:
          pa(t, t.return);
          var a = t.stateNode;
          typeof a.componentWillUnmount == "function" && Bp(
            t,
            t.return,
            a
          ), Wn(t);
          break;
        case 27:
          Pl(t.stateNode);
        case 26:
        case 5:
          pa(t, t.return), Wn(t);
          break;
        case 22:
          t.memoizedState === null && Wn(t);
          break;
        case 30:
          Wn(t);
          break;
        default:
          Wn(t);
      }
      e = e.sibling;
    }
  }
  function ja(e, t, a) {
    for (a = a && (t.subtreeFlags & 8772) !== 0, t = t.child; t !== null; ) {
      var l = t.alternate, o = e, u = t, p = u.flags;
      switch (u.tag) {
        case 0:
        case 11:
        case 15:
          ja(
            o,
            u,
            a
          ), Vl(4, u);
          break;
        case 1:
          if (ja(
            o,
            u,
            a
          ), l = u, o = l.stateNode, typeof o.componentDidMount == "function")
            try {
              o.componentDidMount();
            } catch (D) {
              Ae(l, l.return, D);
            }
          if (l = u, o = l.updateQueue, o !== null) {
            var b = l.stateNode;
            try {
              var w = o.shared.hiddenCallbacks;
              if (w !== null)
                for (o.shared.hiddenCallbacks = null, o = 0; o < w.length; o++)
                  Dd(w[o], b);
            } catch (D) {
              Ae(l, l.return, D);
            }
          }
          a && p & 64 && Yp(u), Xl(u, u.return);
          break;
        case 27:
          Gp(u);
        case 26:
        case 5:
          ja(
            o,
            u,
            a
          ), a && l === null && p & 4 && Vp(u), Xl(u, u.return);
          break;
        case 12:
          ja(
            o,
            u,
            a
          );
          break;
        case 31:
          ja(
            o,
            u,
            a
          ), a && p & 4 && Fp(o, u);
          break;
        case 13:
          ja(
            o,
            u,
            a
          ), a && p & 4 && Wp(o, u);
          break;
        case 22:
          u.memoizedState === null && ja(
            o,
            u,
            a
          ), Xl(u, u.return);
          break;
        case 30:
          break;
        default:
          ja(
            o,
            u,
            a
          );
      }
      t = t.sibling;
    }
  }
  function $u(e, t) {
    var a = null;
    e !== null && e.memoizedState !== null && e.memoizedState.cachePool !== null && (a = e.memoizedState.cachePool.pool), e = null, t.memoizedState !== null && t.memoizedState.cachePool !== null && (e = t.memoizedState.cachePool.pool), e !== a && (e != null && e.refCount++, a != null && Dl(a));
  }
  function Uu(e, t) {
    e = null, t.alternate !== null && (e = t.alternate.memoizedState.cache), t = t.memoizedState.cache, t !== e && (t.refCount++, e != null && Dl(e));
  }
  function aa(e, t, a, l) {
    if (t.subtreeFlags & 10256)
      for (t = t.child; t !== null; )
        eh(
          e,
          t,
          a,
          l
        ), t = t.sibling;
  }
  function eh(e, t, a, l) {
    var o = t.flags;
    switch (t.tag) {
      case 0:
      case 11:
      case 15:
        aa(
          e,
          t,
          a,
          l
        ), o & 2048 && Vl(9, t);
        break;
      case 1:
        aa(
          e,
          t,
          a,
          l
        );
        break;
      case 3:
        aa(
          e,
          t,
          a,
          l
        ), o & 2048 && (e = null, t.alternate !== null && (e = t.alternate.memoizedState.cache), t = t.memoizedState.cache, t !== e && (t.refCount++, e != null && Dl(e)));
        break;
      case 12:
        if (o & 2048) {
          aa(
            e,
            t,
            a,
            l
          ), e = t.stateNode;
          try {
            var u = t.memoizedProps, p = u.id, b = u.onPostCommit;
            typeof b == "function" && b(
              p,
              t.alternate === null ? "mount" : "update",
              e.passiveEffectDuration,
              -0
            );
          } catch (w) {
            Ae(t, t.return, w);
          }
        } else
          aa(
            e,
            t,
            a,
            l
          );
        break;
      case 31:
        aa(
          e,
          t,
          a,
          l
        );
        break;
      case 13:
        aa(
          e,
          t,
          a,
          l
        );
        break;
      case 23:
        break;
      case 22:
        u = t.stateNode, p = t.alternate, t.memoizedState !== null ? u._visibility & 2 ? aa(
          e,
          t,
          a,
          l
        ) : Gl(e, t) : u._visibility & 2 ? aa(
          e,
          t,
          a,
          l
        ) : (u._visibility |= 2, Bi(
          e,
          t,
          a,
          l,
          (t.subtreeFlags & 10256) !== 0 || !1
        )), o & 2048 && $u(p, t);
        break;
      case 24:
        aa(
          e,
          t,
          a,
          l
        ), o & 2048 && Uu(t.alternate, t);
        break;
      default:
        aa(
          e,
          t,
          a,
          l
        );
    }
  }
  function Bi(e, t, a, l, o) {
    for (o = o && ((t.subtreeFlags & 10256) !== 0 || !1), t = t.child; t !== null; ) {
      var u = e, p = t, b = a, w = l, D = p.flags;
      switch (p.tag) {
        case 0:
        case 11:
        case 15:
          Bi(
            u,
            p,
            b,
            w,
            o
          ), Vl(8, p);
          break;
        case 23:
          break;
        case 22:
          var U = p.stateNode;
          p.memoizedState !== null ? U._visibility & 2 ? Bi(
            u,
            p,
            b,
            w,
            o
          ) : Gl(
            u,
            p
          ) : (U._visibility |= 2, Bi(
            u,
            p,
            b,
            w,
            o
          )), o && D & 2048 && $u(
            p.alternate,
            p
          );
          break;
        case 24:
          Bi(
            u,
            p,
            b,
            w,
            o
          ), o && D & 2048 && Uu(p.alternate, p);
          break;
        default:
          Bi(
            u,
            p,
            b,
            w,
            o
          );
      }
      t = t.sibling;
    }
  }
  function Gl(e, t) {
    if (t.subtreeFlags & 10256)
      for (t = t.child; t !== null; ) {
        var a = e, l = t, o = l.flags;
        switch (l.tag) {
          case 22:
            Gl(a, l), o & 2048 && $u(
              l.alternate,
              l
            );
            break;
          case 24:
            Gl(a, l), o & 2048 && Uu(l.alternate, l);
            break;
          default:
            Gl(a, l);
        }
        t = t.sibling;
      }
  }
  var Ql = 8192;
  function Vi(e, t, a) {
    if (e.subtreeFlags & Ql)
      for (e = e.child; e !== null; )
        th(
          e,
          t,
          a
        ), e = e.sibling;
  }
  function th(e, t, a) {
    switch (e.tag) {
      case 26:
        Vi(
          e,
          t,
          a
        ), e.flags & Ql && e.memoizedState !== null && hb(
          a,
          ta,
          e.memoizedState,
          e.memoizedProps
        );
        break;
      case 5:
        Vi(
          e,
          t,
          a
        );
        break;
      case 3:
      case 4:
        var l = ta;
        ta = Tr(e.stateNode.containerInfo), Vi(
          e,
          t,
          a
        ), ta = l;
        break;
      case 22:
        e.memoizedState === null && (l = e.alternate, l !== null && l.memoizedState !== null ? (l = Ql, Ql = 16777216, Vi(
          e,
          t,
          a
        ), Ql = l) : Vi(
          e,
          t,
          a
        ));
        break;
      default:
        Vi(
          e,
          t,
          a
        );
    }
  }
  function ah(e) {
    var t = e.alternate;
    if (t !== null && (e = t.child, e !== null)) {
      t.child = null;
      do
        t = e.sibling, e.sibling = null, e = t;
      while (e !== null);
    }
  }
  function Zl(e) {
    var t = e.deletions;
    if ((e.flags & 16) !== 0) {
      if (t !== null)
        for (var a = 0; a < t.length; a++) {
          var l = t[a];
          lt = l, ih(
            l,
            e
          );
        }
      ah(e);
    }
    if (e.subtreeFlags & 10256)
      for (e = e.child; e !== null; )
        nh(e), e = e.sibling;
  }
  function nh(e) {
    switch (e.tag) {
      case 0:
      case 11:
      case 15:
        Zl(e), e.flags & 2048 && rn(9, e, e.return);
        break;
      case 3:
        Zl(e);
        break;
      case 12:
        Zl(e);
        break;
      case 22:
        var t = e.stateNode;
        e.memoizedState !== null && t._visibility & 2 && (e.return === null || e.return.tag !== 13) ? (t._visibility &= -3, pr(e)) : Zl(e);
        break;
      default:
        Zl(e);
    }
  }
  function pr(e) {
    var t = e.deletions;
    if ((e.flags & 16) !== 0) {
      if (t !== null)
        for (var a = 0; a < t.length; a++) {
          var l = t[a];
          lt = l, ih(
            l,
            e
          );
        }
      ah(e);
    }
    for (e = e.child; e !== null; ) {
      switch (t = e, t.tag) {
        case 0:
        case 11:
        case 15:
          rn(8, t, t.return), pr(t);
          break;
        case 22:
          a = t.stateNode, a._visibility & 2 && (a._visibility &= -3, pr(t));
          break;
        default:
          pr(t);
      }
      e = e.sibling;
    }
  }
  function ih(e, t) {
    for (; lt !== null; ) {
      var a = lt;
      switch (a.tag) {
        case 0:
        case 11:
        case 15:
          rn(8, a, t);
          break;
        case 23:
        case 22:
          if (a.memoizedState !== null && a.memoizedState.cachePool !== null) {
            var l = a.memoizedState.cachePool.pool;
            l != null && l.refCount++;
          }
          break;
        case 24:
          Dl(a.memoizedState.cache);
      }
      if (l = a.child, l !== null) l.return = a, lt = l;
      else
        e: for (a = e; lt !== null; ) {
          l = lt;
          var o = l.sibling, u = l.return;
          if (Kp(l), l === a) {
            lt = null;
            break e;
          }
          if (o !== null) {
            o.return = u, lt = o;
            break e;
          }
          lt = u;
        }
    }
  }
  var Og = {
    getCacheForType: function(e) {
      var t = ct(We), a = t.data.get(e);
      return a === void 0 && (a = e(), t.data.set(e, a)), a;
    },
    cacheSignal: function() {
      return ct(We).controller.signal;
    }
  }, Dg = typeof WeakMap == "function" ? WeakMap : Map, ze = 0, Ne = null, pe = null, me = 0, Ee = 0, Rt = null, on = !1, Xi = !1, Lu = !1, Ra = 0, Ve = 0, un = 0, In = 0, Hu = 0, $t = 0, Gi = 0, Kl = null, Et = null, Yu = !1, hr = 0, lh = 0, mr = 1 / 0, vr = null, cn = null, at = 0, fn = null, Qi = null, $a = 0, Bu = 0, Vu = null, sh = null, Jl = 0, Xu = null;
  function Ut() {
    return (ze & 2) !== 0 && me !== 0 ? me & -me : R.T !== null ? Fu() : wf();
  }
  function rh() {
    if ($t === 0)
      if ((me & 536870912) === 0 || ge) {
        var e = qn;
        qn <<= 1, (qn & 3932160) === 0 && (qn = 262144), $t = e;
      } else $t = 536870912;
    return e = qt.current, e !== null && (e.flags |= 32), $t;
  }
  function At(e, t, a) {
    (e === Ne && (Ee === 2 || Ee === 9) || e.cancelPendingCommit !== null) && (Zi(e, 0), dn(
      e,
      me,
      $t,
      !1
    )), G(e, a), ((ze & 2) === 0 || e !== Ne) && (e === Ne && ((ze & 2) === 0 && (In |= a), Ve === 4 && dn(
      e,
      me,
      $t,
      !1
    )), ha(e));
  }
  function oh(e, t, a) {
    if ((ze & 6) !== 0) throw Error(r(327));
    var l = !a && (t & 127) === 0 && (t & e.expiredLanes) === 0 || ca(e, t), o = l ? Mg(e, t) : Qu(e, t, !0), u = l;
    do {
      if (o === 0) {
        Xi && !l && dn(e, t, 0, !1);
        break;
      } else {
        if (a = e.current.alternate, u && !Cg(a)) {
          o = Qu(e, t, !1), u = !1;
          continue;
        }
        if (o === 2) {
          if (u = t, e.errorRecoveryDisabledLanes & u)
            var p = 0;
          else
            p = e.pendingLanes & -536870913, p = p !== 0 ? p : p & 536870912 ? 536870912 : 0;
          if (p !== 0) {
            t = p;
            e: {
              var b = e;
              o = Kl;
              var w = b.current.memoizedState.isDehydrated;
              if (w && (Zi(b, p).flags |= 256), p = Qu(
                b,
                p,
                !1
              ), p !== 2) {
                if (Lu && !w) {
                  b.errorRecoveryDisabledLanes |= u, In |= u, o = 4;
                  break e;
                }
                u = Et, Et = o, u !== null && (Et === null ? Et = u : Et.push.apply(
                  Et,
                  u
                ));
              }
              o = p;
            }
            if (u = !1, o !== 2) continue;
          }
        }
        if (o === 1) {
          Zi(e, 0), dn(e, t, 0, !0);
          break;
        }
        e: {
          switch (l = e, u = o, u) {
            case 0:
            case 1:
              throw Error(r(345));
            case 4:
              if ((t & 4194048) !== t) break;
            case 6:
              dn(
                l,
                t,
                $t,
                !on
              );
              break e;
            case 2:
              Et = null;
              break;
            case 3:
            case 5:
              break;
            default:
              throw Error(r(329));
          }
          if ((t & 62914560) === t && (o = hr + 300 - Me(), 10 < o)) {
            if (dn(
              l,
              t,
              $t,
              !on
            ), Ka(l, 0, !0) !== 0) break e;
            $a = t, l.timeoutHandle = Lh(
              uh.bind(
                null,
                l,
                a,
                Et,
                vr,
                Yu,
                t,
                $t,
                In,
                Gi,
                on,
                u,
                "Throttled",
                -0,
                0
              ),
              o
            );
            break e;
          }
          uh(
            l,
            a,
            Et,
            vr,
            Yu,
            t,
            $t,
            In,
            Gi,
            on,
            u,
            null,
            -0,
            0
          );
        }
      }
      break;
    } while (!0);
    ha(e);
  }
  function uh(e, t, a, l, o, u, p, b, w, D, U, B, C, M) {
    if (e.timeoutHandle = -1, B = t.subtreeFlags, B & 8192 || (B & 16785408) === 16785408) {
      B = {
        stylesheets: null,
        count: 0,
        imgCount: 0,
        imgBytes: 0,
        suspenseyImages: [],
        waitingForImages: !0,
        waitingForViewTransition: !1,
        unsuspend: wa
      }, th(
        t,
        u,
        B
      );
      var P = (u & 62914560) === u ? hr - Me() : (u & 4194048) === u ? lh - Me() : 0;
      if (P = mb(
        B,
        P
      ), P !== null) {
        $a = u, e.cancelPendingCommit = P(
          gh.bind(
            null,
            e,
            t,
            u,
            a,
            l,
            o,
            p,
            b,
            w,
            U,
            B,
            null,
            C,
            M
          )
        ), dn(e, u, p, !D);
        return;
      }
    }
    gh(
      e,
      t,
      u,
      a,
      l,
      o,
      p,
      b,
      w
    );
  }
  function Cg(e) {
    for (var t = e; ; ) {
      var a = t.tag;
      if ((a === 0 || a === 11 || a === 15) && t.flags & 16384 && (a = t.updateQueue, a !== null && (a = a.stores, a !== null)))
        for (var l = 0; l < a.length; l++) {
          var o = a[l], u = o.getSnapshot;
          o = o.value;
          try {
            if (!Nt(u(), o)) return !1;
          } catch {
            return !1;
          }
        }
      if (a = t.child, t.subtreeFlags & 16384 && a !== null)
        a.return = t, t = a;
      else {
        if (t === e) break;
        for (; t.sibling === null; ) {
          if (t.return === null || t.return === e) return !0;
          t = t.return;
        }
        t.sibling.return = t.return, t = t.sibling;
      }
    }
    return !0;
  }
  function dn(e, t, a, l) {
    t &= ~Hu, t &= ~In, e.suspendedLanes |= t, e.pingedLanes &= ~t, l && (e.warmLanes |= t), l = e.expirationTimes;
    for (var o = t; 0 < o; ) {
      var u = 31 - rt(o), p = 1 << u;
      l[u] = -1, o &= ~p;
    }
    a !== 0 && _e(e, a, t);
  }
  function gr() {
    return (ze & 6) === 0 ? (Fl(0), !1) : !0;
  }
  function Gu() {
    if (pe !== null) {
      if (Ee === 0)
        var e = pe.return;
      else
        e = pe, Ta = Vn = null, ru(e), $i = null, Nl = 0, e = pe;
      for (; e !== null; )
        Hp(e.alternate, e), e = e.return;
      pe = null;
    }
  }
  function Zi(e, t) {
    var a = e.timeoutHandle;
    a !== -1 && (e.timeoutHandle = -1, Wg(a)), a = e.cancelPendingCommit, a !== null && (e.cancelPendingCommit = null, a()), $a = 0, Gu(), Ne = e, pe = a = Sa(e.current, null), me = t, Ee = 0, Rt = null, on = !1, Xi = ca(e, t), Lu = !1, Gi = $t = Hu = In = un = Ve = 0, Et = Kl = null, Yu = !1, (t & 8) !== 0 && (t |= t & 32);
    var l = e.entangledLanes;
    if (l !== 0)
      for (e = e.entanglements, l &= t; 0 < l; ) {
        var o = 31 - rt(l), u = 1 << o;
        t |= e[o], l &= ~u;
      }
    return Ra = t, Us(), a;
  }
  function ch(e, t) {
    ce = null, R.H = Hl, t === Ri || t === Qs ? (t = Td(), Ee = 3) : t === Jo ? (t = Td(), Ee = 4) : Ee = t === ku ? 8 : t !== null && typeof t == "object" && typeof t.then == "function" ? 6 : 1, Rt = t, pe === null && (Ve = 1, sr(
      e,
      Xt(t, e.current)
    ));
  }
  function fh() {
    var e = qt.current;
    return e === null ? !0 : (me & 4194048) === me ? Kt === null : (me & 62914560) === me || (me & 536870912) !== 0 ? e === Kt : !1;
  }
  function dh() {
    var e = R.H;
    return R.H = Hl, e === null ? Hl : e;
  }
  function ph() {
    var e = R.A;
    return R.A = Og, e;
  }
  function br() {
    Ve = 4, on || (me & 4194048) !== me && qt.current !== null || (Xi = !0), (un & 134217727) === 0 && (In & 134217727) === 0 || Ne === null || dn(
      Ne,
      me,
      $t,
      !1
    );
  }
  function Qu(e, t, a) {
    var l = ze;
    ze |= 2;
    var o = dh(), u = ph();
    (Ne !== e || me !== t) && (vr = null, Zi(e, t)), t = !1;
    var p = Ve;
    e: do
      try {
        if (Ee !== 0 && pe !== null) {
          var b = pe, w = Rt;
          switch (Ee) {
            case 8:
              Gu(), p = 6;
              break e;
            case 3:
            case 2:
            case 9:
            case 6:
              qt.current === null && (t = !0);
              var D = Ee;
              if (Ee = 0, Rt = null, Ki(e, b, w, D), a && Xi) {
                p = 0;
                break e;
              }
              break;
            default:
              D = Ee, Ee = 0, Rt = null, Ki(e, b, w, D);
          }
        }
        Ng(), p = Ve;
        break;
      } catch (U) {
        ch(e, U);
      }
    while (!0);
    return t && e.shellSuspendCounter++, Ta = Vn = null, ze = l, R.H = o, R.A = u, pe === null && (Ne = null, me = 0, Us()), p;
  }
  function Ng() {
    for (; pe !== null; ) hh(pe);
  }
  function Mg(e, t) {
    var a = ze;
    ze |= 2;
    var l = dh(), o = ph();
    Ne !== e || me !== t ? (vr = null, mr = Me() + 500, Zi(e, t)) : Xi = ca(
      e,
      t
    );
    e: do
      try {
        if (Ee !== 0 && pe !== null) {
          t = pe;
          var u = Rt;
          t: switch (Ee) {
            case 1:
              Ee = 0, Rt = null, Ki(e, t, u, 1);
              break;
            case 2:
            case 9:
              if (Sd(u)) {
                Ee = 0, Rt = null, mh(t);
                break;
              }
              t = function() {
                Ee !== 2 && Ee !== 9 || Ne !== e || (Ee = 7), ha(e);
              }, u.then(t, t);
              break e;
            case 3:
              Ee = 7;
              break e;
            case 4:
              Ee = 5;
              break e;
            case 7:
              Sd(u) ? (Ee = 0, Rt = null, mh(t)) : (Ee = 0, Rt = null, Ki(e, t, u, 7));
              break;
            case 5:
              var p = null;
              switch (pe.tag) {
                case 26:
                  p = pe.memoizedState;
                case 5:
                case 27:
                  var b = pe;
                  if (p ? e0(p) : b.stateNode.complete) {
                    Ee = 0, Rt = null;
                    var w = b.sibling;
                    if (w !== null) pe = w;
                    else {
                      var D = b.return;
                      D !== null ? (pe = D, yr(D)) : pe = null;
                    }
                    break t;
                  }
              }
              Ee = 0, Rt = null, Ki(e, t, u, 5);
              break;
            case 6:
              Ee = 0, Rt = null, Ki(e, t, u, 6);
              break;
            case 8:
              Gu(), Ve = 6;
              break e;
            default:
              throw Error(r(462));
          }
        }
        qg();
        break;
      } catch (U) {
        ch(e, U);
      }
    while (!0);
    return Ta = Vn = null, R.H = l, R.A = o, ze = a, pe !== null ? 0 : (Ne = null, me = 0, Us(), Ve);
  }
  function qg() {
    for (; pe !== null && !ws(); )
      hh(pe);
  }
  function hh(e) {
    var t = Up(e.alternate, e, Ra);
    e.memoizedProps = e.pendingProps, t === null ? yr(e) : pe = t;
  }
  function mh(e) {
    var t = e, a = t.alternate;
    switch (t.tag) {
      case 15:
      case 0:
        t = Np(
          a,
          t,
          t.pendingProps,
          t.type,
          void 0,
          me
        );
        break;
      case 11:
        t = Np(
          a,
          t,
          t.pendingProps,
          t.type.render,
          t.ref,
          me
        );
        break;
      case 5:
        ru(t);
      default:
        Hp(a, t), t = pe = pd(t, Ra), t = Up(a, t, Ra);
    }
    e.memoizedProps = e.pendingProps, t === null ? yr(e) : pe = t;
  }
  function Ki(e, t, a, l) {
    Ta = Vn = null, ru(t), $i = null, Nl = 0;
    var o = t.return;
    try {
      if (wg(
        e,
        o,
        t,
        a,
        me
      )) {
        Ve = 1, sr(
          e,
          Xt(a, e.current)
        ), pe = null;
        return;
      }
    } catch (u) {
      if (o !== null) throw pe = o, u;
      Ve = 1, sr(
        e,
        Xt(a, e.current)
      ), pe = null;
      return;
    }
    t.flags & 32768 ? (ge || l === 1 ? e = !0 : Xi || (me & 536870912) !== 0 ? e = !1 : (on = e = !0, (l === 2 || l === 9 || l === 3 || l === 6) && (l = qt.current, l !== null && l.tag === 13 && (l.flags |= 16384))), vh(t, e)) : yr(t);
  }
  function yr(e) {
    var t = e;
    do {
      if ((t.flags & 32768) !== 0) {
        vh(
          t,
          on
        );
        return;
      }
      e = t.return;
      var a = zg(
        t.alternate,
        t,
        Ra
      );
      if (a !== null) {
        pe = a;
        return;
      }
      if (t = t.sibling, t !== null) {
        pe = t;
        return;
      }
      pe = t = e;
    } while (t !== null);
    Ve === 0 && (Ve = 5);
  }
  function vh(e, t) {
    do {
      var a = Tg(e.alternate, e);
      if (a !== null) {
        a.flags &= 32767, pe = a;
        return;
      }
      if (a = e.return, a !== null && (a.flags |= 32768, a.subtreeFlags = 0, a.deletions = null), !t && (e = e.sibling, e !== null)) {
        pe = e;
        return;
      }
      pe = e = a;
    } while (e !== null);
    Ve = 6, pe = null;
  }
  function gh(e, t, a, l, o, u, p, b, w) {
    e.cancelPendingCommit = null;
    do
      _r();
    while (at !== 0);
    if ((ze & 6) !== 0) throw Error(r(327));
    if (t !== null) {
      if (t === e.current) throw Error(r(177));
      if (u = t.lanes | t.childLanes, u |= qo, ae(
        e,
        a,
        u,
        p,
        b,
        w
      ), e === Ne && (pe = Ne = null, me = 0), Qi = t, fn = e, $a = a, Bu = u, Vu = o, sh = l, (t.subtreeFlags & 10256) !== 0 || (t.flags & 10256) !== 0 ? (e.callbackNode = null, e.callbackPriority = 0, Ug(di, function() {
        return wh(), null;
      })) : (e.callbackNode = null, e.callbackPriority = 0), l = (t.flags & 13878) !== 0, (t.subtreeFlags & 13878) !== 0 || l) {
        l = R.T, R.T = null, o = Q.p, Q.p = 2, p = ze, ze |= 4;
        try {
          Eg(e, t, a);
        } finally {
          ze = p, Q.p = o, R.T = l;
        }
      }
      at = 1, bh(), yh(), _h();
    }
  }
  function bh() {
    if (at === 1) {
      at = 0;
      var e = fn, t = Qi, a = (t.flags & 13878) !== 0;
      if ((t.subtreeFlags & 13878) !== 0 || a) {
        a = R.T, R.T = null;
        var l = Q.p;
        Q.p = 2;
        var o = ze;
        ze |= 4;
        try {
          Ip(t, e);
          var u = ic, p = id(e.containerInfo), b = u.focusedElem, w = u.selectionRange;
          if (p !== b && b && b.ownerDocument && nd(
            b.ownerDocument.documentElement,
            b
          )) {
            if (w !== null && Oo(b)) {
              var D = w.start, U = w.end;
              if (U === void 0 && (U = D), "selectionStart" in b)
                b.selectionStart = D, b.selectionEnd = Math.min(
                  U,
                  b.value.length
                );
              else {
                var B = b.ownerDocument || document, C = B && B.defaultView || window;
                if (C.getSelection) {
                  var M = C.getSelection(), P = b.textContent.length, ie = Math.min(w.start, P), Ce = w.end === void 0 ? ie : Math.min(w.end, P);
                  !M.extend && ie > Ce && (p = Ce, Ce = ie, ie = p);
                  var E = ad(
                    b,
                    ie
                  ), S = ad(
                    b,
                    Ce
                  );
                  if (E && S && (M.rangeCount !== 1 || M.anchorNode !== E.node || M.anchorOffset !== E.offset || M.focusNode !== S.node || M.focusOffset !== S.offset)) {
                    var O = B.createRange();
                    O.setStart(E.node, E.offset), M.removeAllRanges(), ie > Ce ? (M.addRange(O), M.extend(S.node, S.offset)) : (O.setEnd(S.node, S.offset), M.addRange(O));
                  }
                }
              }
            }
            for (B = [], M = b; M = M.parentNode; )
              M.nodeType === 1 && B.push({
                element: M,
                left: M.scrollLeft,
                top: M.scrollTop
              });
            for (typeof b.focus == "function" && b.focus(), b = 0; b < B.length; b++) {
              var H = B[b];
              H.element.scrollLeft = H.left, H.element.scrollTop = H.top;
            }
          }
          Nr = !!nc, ic = nc = null;
        } finally {
          ze = o, Q.p = l, R.T = a;
        }
      }
      e.current = t, at = 2;
    }
  }
  function yh() {
    if (at === 2) {
      at = 0;
      var e = fn, t = Qi, a = (t.flags & 8772) !== 0;
      if ((t.subtreeFlags & 8772) !== 0 || a) {
        a = R.T, R.T = null;
        var l = Q.p;
        Q.p = 2;
        var o = ze;
        ze |= 4;
        try {
          Zp(e, t.alternate, t);
        } finally {
          ze = o, Q.p = l, R.T = a;
        }
      }
      at = 3;
    }
  }
  function _h() {
    if (at === 4 || at === 3) {
      at = 0, fi();
      var e = fn, t = Qi, a = $a, l = sh;
      (t.subtreeFlags & 10256) !== 0 || (t.flags & 10256) !== 0 ? at = 5 : (at = 0, Qi = fn = null, xh(e, e.pendingLanes));
      var o = e.pendingLanes;
      if (o === 0 && (cn = null), uo(a), t = t.stateNode, tt && typeof tt.onCommitFiberRoot == "function")
        try {
          tt.onCommitFiberRoot(
            Za,
            t,
            void 0,
            (t.current.flags & 128) === 128
          );
        } catch {
        }
      if (l !== null) {
        t = R.T, o = Q.p, Q.p = 2, R.T = null;
        try {
          for (var u = e.onRecoverableError, p = 0; p < l.length; p++) {
            var b = l[p];
            u(b.value, {
              componentStack: b.stack
            });
          }
        } finally {
          R.T = t, Q.p = o;
        }
      }
      ($a & 3) !== 0 && _r(), ha(e), o = e.pendingLanes, (a & 261930) !== 0 && (o & 42) !== 0 ? e === Xu ? Jl++ : (Jl = 0, Xu = e) : Jl = 0, Fl(0);
    }
  }
  function xh(e, t) {
    (e.pooledCacheLanes &= t) === 0 && (t = e.pooledCache, t != null && (e.pooledCache = null, Dl(t)));
  }
  function _r() {
    return bh(), yh(), _h(), wh();
  }
  function wh() {
    if (at !== 5) return !1;
    var e = fn, t = Bu;
    Bu = 0;
    var a = uo($a), l = R.T, o = Q.p;
    try {
      Q.p = 32 > a ? 32 : a, R.T = null, a = Vu, Vu = null;
      var u = fn, p = $a;
      if (at = 0, Qi = fn = null, $a = 0, (ze & 6) !== 0) throw Error(r(331));
      var b = ze;
      if (ze |= 4, nh(u.current), eh(
        u,
        u.current,
        p,
        a
      ), ze = b, Fl(0, !1), tt && typeof tt.onPostCommitFiberRoot == "function")
        try {
          tt.onPostCommitFiberRoot(Za, u);
        } catch {
        }
      return !0;
    } finally {
      Q.p = o, R.T = l, xh(e, t);
    }
  }
  function kh(e, t, a) {
    t = Xt(a, t), t = wu(e.stateNode, t, 2), e = nn(e, t, 2), e !== null && (G(e, 2), ha(e));
  }
  function Ae(e, t, a) {
    if (e.tag === 3)
      kh(e, e, a);
    else
      for (; t !== null; ) {
        if (t.tag === 3) {
          kh(
            t,
            e,
            a
          );
          break;
        } else if (t.tag === 1) {
          var l = t.stateNode;
          if (typeof t.type.getDerivedStateFromError == "function" || typeof l.componentDidCatch == "function" && (cn === null || !cn.has(l))) {
            e = Xt(a, e), a = Sp(2), l = nn(t, a, 2), l !== null && (zp(
              a,
              l,
              t,
              e
            ), G(l, 2), ha(l));
            break;
          }
        }
        t = t.return;
      }
  }
  function Zu(e, t, a) {
    var l = e.pingCache;
    if (l === null) {
      l = e.pingCache = new Dg();
      var o = /* @__PURE__ */ new Set();
      l.set(t, o);
    } else
      o = l.get(t), o === void 0 && (o = /* @__PURE__ */ new Set(), l.set(t, o));
    o.has(a) || (Lu = !0, o.add(a), e = jg.bind(null, e, t, a), t.then(e, e));
  }
  function jg(e, t, a) {
    var l = e.pingCache;
    l !== null && l.delete(t), e.pingedLanes |= e.suspendedLanes & a, e.warmLanes &= ~a, Ne === e && (me & a) === a && (Ve === 4 || Ve === 3 && (me & 62914560) === me && 300 > Me() - hr ? (ze & 2) === 0 && Zi(e, 0) : Hu |= a, Gi === me && (Gi = 0)), ha(e);
  }
  function Sh(e, t) {
    t === 0 && (t = jn()), e = Hn(e, t), e !== null && (G(e, t), ha(e));
  }
  function Rg(e) {
    var t = e.memoizedState, a = 0;
    t !== null && (a = t.retryLane), Sh(e, a);
  }
  function $g(e, t) {
    var a = 0;
    switch (e.tag) {
      case 31:
      case 13:
        var l = e.stateNode, o = e.memoizedState;
        o !== null && (a = o.retryLane);
        break;
      case 19:
        l = e.stateNode;
        break;
      case 22:
        l = e.stateNode._retryCache;
        break;
      default:
        throw Error(r(314));
    }
    l !== null && l.delete(t), Sh(e, a);
  }
  function Ug(e, t) {
    return hl(e, t);
  }
  var xr = null, Ji = null, Ku = !1, wr = !1, Ju = !1, pn = 0;
  function ha(e) {
    e !== Ji && e.next === null && (Ji === null ? xr = Ji = e : Ji = Ji.next = e), wr = !0, Ku || (Ku = !0, Hg());
  }
  function Fl(e, t) {
    if (!Ju && wr) {
      Ju = !0;
      do
        for (var a = !1, l = xr; l !== null; ) {
          if (e !== 0) {
            var o = l.pendingLanes;
            if (o === 0) var u = 0;
            else {
              var p = l.suspendedLanes, b = l.pingedLanes;
              u = (1 << 31 - rt(42 | e) + 1) - 1, u &= o & ~(p & ~b), u = u & 201326741 ? u & 201326741 | 1 : u ? u | 2 : 0;
            }
            u !== 0 && (a = !0, Ah(l, u));
          } else
            u = me, u = Ka(
              l,
              l === Ne ? u : 0,
              l.cancelPendingCommit !== null || l.timeoutHandle !== -1
            ), (u & 3) === 0 || ca(l, u) || (a = !0, Ah(l, u));
          l = l.next;
        }
      while (a);
      Ju = !1;
    }
  }
  function Lg() {
    zh();
  }
  function zh() {
    wr = Ku = !1;
    var e = 0;
    pn !== 0 && Fg() && (e = pn);
    for (var t = Me(), a = null, l = xr; l !== null; ) {
      var o = l.next, u = Th(l, t);
      u === 0 ? (l.next = null, a === null ? xr = o : a.next = o, o === null && (Ji = a)) : (a = l, (e !== 0 || (u & 3) !== 0) && (wr = !0)), l = o;
    }
    at !== 0 && at !== 5 || Fl(e), pn !== 0 && (pn = 0);
  }
  function Th(e, t) {
    for (var a = e.suspendedLanes, l = e.pingedLanes, o = e.expirationTimes, u = e.pendingLanes & -62914561; 0 < u; ) {
      var p = 31 - rt(u), b = 1 << p, w = o[p];
      w === -1 ? ((b & a) === 0 || (b & l) !== 0) && (o[p] = Ts(b, t)) : w <= t && (e.expiredLanes |= b), u &= ~b;
    }
    if (t = Ne, a = me, a = Ka(
      e,
      e === t ? a : 0,
      e.cancelPendingCommit !== null || e.timeoutHandle !== -1
    ), l = e.callbackNode, a === 0 || e === t && (Ee === 2 || Ee === 9) || e.cancelPendingCommit !== null)
      return l !== null && l !== null && ml(l), e.callbackNode = null, e.callbackPriority = 0;
    if ((a & 3) === 0 || ca(e, a)) {
      if (t = a & -a, t === e.callbackPriority) return t;
      switch (l !== null && ml(l), uo(a)) {
        case 2:
        case 8:
          a = Mn;
          break;
        case 32:
          a = di;
          break;
        case 268435456:
          a = pi;
          break;
        default:
          a = di;
      }
      return l = Eh.bind(null, e), a = hl(a, l), e.callbackPriority = t, e.callbackNode = a, t;
    }
    return l !== null && l !== null && ml(l), e.callbackPriority = 2, e.callbackNode = null, 2;
  }
  function Eh(e, t) {
    if (at !== 0 && at !== 5)
      return e.callbackNode = null, e.callbackPriority = 0, null;
    var a = e.callbackNode;
    if (_r() && e.callbackNode !== a)
      return null;
    var l = me;
    return l = Ka(
      e,
      e === Ne ? l : 0,
      e.cancelPendingCommit !== null || e.timeoutHandle !== -1
    ), l === 0 ? null : (oh(e, l, t), Th(e, Me()), e.callbackNode != null && e.callbackNode === a ? Eh.bind(null, e) : null);
  }
  function Ah(e, t) {
    if (_r()) return null;
    oh(e, t, !0);
  }
  function Hg() {
    Ig(function() {
      (ze & 6) !== 0 ? hl(
        Nn,
        Lg
      ) : zh();
    });
  }
  function Fu() {
    if (pn === 0) {
      var e = qi;
      e === 0 && (e = hi, hi <<= 1, (hi & 261888) === 0 && (hi = 256)), pn = e;
    }
    return pn;
  }
  function Oh(e) {
    return e == null || typeof e == "symbol" || typeof e == "boolean" ? null : typeof e == "function" ? e : Ds("" + e);
  }
  function Dh(e, t) {
    var a = t.ownerDocument.createElement("input");
    return a.name = t.name, a.value = t.value, e.id && a.setAttribute("form", e.id), t.parentNode.insertBefore(a, t), e = new FormData(e), a.parentNode.removeChild(a), e;
  }
  function Yg(e, t, a, l, o) {
    if (t === "submit" && a && a.stateNode === o) {
      var u = Oh(
        (o[wt] || null).action
      ), p = l.submitter;
      p && (t = (t = p[wt] || null) ? Oh(t.formAction) : p.getAttribute("formAction"), t !== null && (u = t, p = null));
      var b = new qs(
        "action",
        "action",
        null,
        l,
        o
      );
      e.push({
        event: b,
        listeners: [
          {
            instance: null,
            listener: function() {
              if (l.defaultPrevented) {
                if (pn !== 0) {
                  var w = p ? Dh(o, p) : new FormData(o);
                  vu(
                    a,
                    {
                      pending: !0,
                      data: w,
                      method: o.method,
                      action: u
                    },
                    null,
                    w
                  );
                }
              } else
                typeof u == "function" && (b.preventDefault(), w = p ? Dh(o, p) : new FormData(o), vu(
                  a,
                  {
                    pending: !0,
                    data: w,
                    method: o.method,
                    action: u
                  },
                  u,
                  w
                ));
            },
            currentTarget: o
          }
        ]
      });
    }
  }
  for (var Wu = 0; Wu < Mo.length; Wu++) {
    var Iu = Mo[Wu], Bg = Iu.toLowerCase(), Vg = Iu[0].toUpperCase() + Iu.slice(1);
    ea(
      Bg,
      "on" + Vg
    );
  }
  ea(rd, "onAnimationEnd"), ea(od, "onAnimationIteration"), ea(ud, "onAnimationStart"), ea("dblclick", "onDoubleClick"), ea("focusin", "onFocus"), ea("focusout", "onBlur"), ea(lg, "onTransitionRun"), ea(sg, "onTransitionStart"), ea(rg, "onTransitionCancel"), ea(cd, "onTransitionEnd"), _i("onMouseEnter", ["mouseout", "mouseover"]), _i("onMouseLeave", ["mouseout", "mouseover"]), _i("onPointerEnter", ["pointerout", "pointerover"]), _i("onPointerLeave", ["pointerout", "pointerover"]), Rn(
    "onChange",
    "change click focusin focusout input keydown keyup selectionchange".split(" ")
  ), Rn(
    "onSelect",
    "focusout contextmenu dragend focusin keydown keyup mousedown mouseup selectionchange".split(
      " "
    )
  ), Rn("onBeforeInput", [
    "compositionend",
    "keypress",
    "textInput",
    "paste"
  ]), Rn(
    "onCompositionEnd",
    "compositionend focusout keydown keypress keyup mousedown".split(" ")
  ), Rn(
    "onCompositionStart",
    "compositionstart focusout keydown keypress keyup mousedown".split(" ")
  ), Rn(
    "onCompositionUpdate",
    "compositionupdate focusout keydown keypress keyup mousedown".split(" ")
  );
  var Wl = "abort canplay canplaythrough durationchange emptied encrypted ended error loadeddata loadedmetadata loadstart pause play playing progress ratechange resize seeked seeking stalled suspend timeupdate volumechange waiting".split(
    " "
  ), Xg = new Set(
    "beforetoggle cancel close invalid load scroll scrollend toggle".split(" ").concat(Wl)
  );
  function Ch(e, t) {
    t = (t & 4) !== 0;
    for (var a = 0; a < e.length; a++) {
      var l = e[a], o = l.event;
      l = l.listeners;
      e: {
        var u = void 0;
        if (t)
          for (var p = l.length - 1; 0 <= p; p--) {
            var b = l[p], w = b.instance, D = b.currentTarget;
            if (b = b.listener, w !== u && o.isPropagationStopped())
              break e;
            u = b, o.currentTarget = D;
            try {
              u(o);
            } catch (U) {
              $s(U);
            }
            o.currentTarget = null, u = w;
          }
        else
          for (p = 0; p < l.length; p++) {
            if (b = l[p], w = b.instance, D = b.currentTarget, b = b.listener, w !== u && o.isPropagationStopped())
              break e;
            u = b, o.currentTarget = D;
            try {
              u(o);
            } catch (U) {
              $s(U);
            }
            o.currentTarget = null, u = w;
          }
      }
    }
  }
  function he(e, t) {
    var a = t[co];
    a === void 0 && (a = t[co] = /* @__PURE__ */ new Set());
    var l = e + "__bubble";
    a.has(l) || (Nh(t, e, 2, !1), a.add(l));
  }
  function Pu(e, t, a) {
    var l = 0;
    t && (l |= 4), Nh(
      a,
      e,
      l,
      t
    );
  }
  var kr = "_reactListening" + Math.random().toString(36).slice(2);
  function ec(e) {
    if (!e[kr]) {
      e[kr] = !0, zf.forEach(function(a) {
        a !== "selectionchange" && (Xg.has(a) || Pu(a, !1, e), Pu(a, !0, e));
      });
      var t = e.nodeType === 9 ? e : e.ownerDocument;
      t === null || t[kr] || (t[kr] = !0, Pu("selectionchange", !1, t));
    }
  }
  function Nh(e, t, a, l) {
    switch (r0(t)) {
      case 2:
        var o = bb;
        break;
      case 8:
        o = yb;
        break;
      default:
        o = mc;
    }
    a = o.bind(
      null,
      t,
      a,
      e
    ), o = void 0, !_o || t !== "touchstart" && t !== "touchmove" && t !== "wheel" || (o = !0), l ? o !== void 0 ? e.addEventListener(t, a, {
      capture: !0,
      passive: o
    }) : e.addEventListener(t, a, !0) : o !== void 0 ? e.addEventListener(t, a, {
      passive: o
    }) : e.addEventListener(t, a, !1);
  }
  function tc(e, t, a, l, o) {
    var u = l;
    if ((t & 1) === 0 && (t & 2) === 0 && l !== null)
      e: for (; ; ) {
        if (l === null) return;
        var p = l.tag;
        if (p === 3 || p === 4) {
          var b = l.stateNode.containerInfo;
          if (b === o) break;
          if (p === 4)
            for (p = l.return; p !== null; ) {
              var w = p.tag;
              if ((w === 3 || w === 4) && p.stateNode.containerInfo === o)
                return;
              p = p.return;
            }
          for (; b !== null; ) {
            if (p = gi(b), p === null) return;
            if (w = p.tag, w === 5 || w === 6 || w === 26 || w === 27) {
              l = u = p;
              continue e;
            }
            b = b.parentNode;
          }
        }
        l = l.return;
      }
    $f(function() {
      var D = u, U = bo(a), B = [];
      e: {
        var C = fd.get(e);
        if (C !== void 0) {
          var M = qs, P = e;
          switch (e) {
            case "keypress":
              if (Ns(a) === 0) break e;
            case "keydown":
            case "keyup":
              M = $v;
              break;
            case "focusin":
              P = "focus", M = So;
              break;
            case "focusout":
              P = "blur", M = So;
              break;
            case "beforeblur":
            case "afterblur":
              M = So;
              break;
            case "click":
              if (a.button === 2) break e;
            case "auxclick":
            case "dblclick":
            case "mousedown":
            case "mousemove":
            case "mouseup":
            case "mouseout":
            case "mouseover":
            case "contextmenu":
              M = Hf;
              break;
            case "drag":
            case "dragend":
            case "dragenter":
            case "dragexit":
            case "dragleave":
            case "dragover":
            case "dragstart":
            case "drop":
              M = zv;
              break;
            case "touchcancel":
            case "touchend":
            case "touchmove":
            case "touchstart":
              M = Hv;
              break;
            case rd:
            case od:
            case ud:
              M = Av;
              break;
            case cd:
              M = Bv;
              break;
            case "scroll":
            case "scrollend":
              M = kv;
              break;
            case "wheel":
              M = Xv;
              break;
            case "copy":
            case "cut":
            case "paste":
              M = Dv;
              break;
            case "gotpointercapture":
            case "lostpointercapture":
            case "pointercancel":
            case "pointerdown":
            case "pointermove":
            case "pointerout":
            case "pointerover":
            case "pointerup":
              M = Bf;
              break;
            case "toggle":
            case "beforetoggle":
              M = Qv;
          }
          var ie = (t & 4) !== 0, Ce = !ie && (e === "scroll" || e === "scrollend"), E = ie ? C !== null ? C + "Capture" : null : C;
          ie = [];
          for (var S = D, O; S !== null; ) {
            var H = S;
            if (O = H.stateNode, H = H.tag, H !== 5 && H !== 26 && H !== 27 || O === null || E === null || (H = yl(S, E), H != null && ie.push(
              Il(S, H, O)
            )), Ce) break;
            S = S.return;
          }
          0 < ie.length && (C = new M(
            C,
            P,
            null,
            a,
            U
          ), B.push({ event: C, listeners: ie }));
        }
      }
      if ((t & 7) === 0) {
        e: {
          if (C = e === "mouseover" || e === "pointerover", M = e === "mouseout" || e === "pointerout", C && a !== go && (P = a.relatedTarget || a.fromElement) && (gi(P) || P[vi]))
            break e;
          if ((M || C) && (C = U.window === U ? U : (C = U.ownerDocument) ? C.defaultView || C.parentWindow : window, M ? (P = a.relatedTarget || a.toElement, M = D, P = P ? gi(P) : null, P !== null && (Ce = f(P), ie = P.tag, P !== Ce || ie !== 5 && ie !== 27 && ie !== 6) && (P = null)) : (M = null, P = D), M !== P)) {
            if (ie = Hf, H = "onMouseLeave", E = "onMouseEnter", S = "mouse", (e === "pointerout" || e === "pointerover") && (ie = Bf, H = "onPointerLeave", E = "onPointerEnter", S = "pointer"), Ce = M == null ? C : bl(M), O = P == null ? C : bl(P), C = new ie(
              H,
              S + "leave",
              M,
              a,
              U
            ), C.target = Ce, C.relatedTarget = O, H = null, gi(U) === D && (ie = new ie(
              E,
              S + "enter",
              P,
              a,
              U
            ), ie.target = O, ie.relatedTarget = Ce, H = ie), Ce = H, M && P)
              t: {
                for (ie = Gg, E = M, S = P, O = 0, H = E; H; H = ie(H))
                  O++;
                H = 0;
                for (var ne = S; ne; ne = ie(ne))
                  H++;
                for (; 0 < O - H; )
                  E = ie(E), O--;
                for (; 0 < H - O; )
                  S = ie(S), H--;
                for (; O--; ) {
                  if (E === S || S !== null && E === S.alternate) {
                    ie = E;
                    break t;
                  }
                  E = ie(E), S = ie(S);
                }
                ie = null;
              }
            else ie = null;
            M !== null && Mh(
              B,
              C,
              M,
              ie,
              !1
            ), P !== null && Ce !== null && Mh(
              B,
              Ce,
              P,
              ie,
              !0
            );
          }
        }
        e: {
          if (C = D ? bl(D) : window, M = C.nodeName && C.nodeName.toLowerCase(), M === "select" || M === "input" && C.type === "file")
            var we = Ff;
          else if (Kf(C))
            if (Wf)
              we = ag;
            else {
              we = eg;
              var te = Pv;
            }
          else
            M = C.nodeName, !M || M.toLowerCase() !== "input" || C.type !== "checkbox" && C.type !== "radio" ? D && vo(D.elementType) && (we = Ff) : we = tg;
          if (we && (we = we(e, D))) {
            Jf(
              B,
              we,
              a,
              U
            );
            break e;
          }
          te && te(e, C, D), e === "focusout" && D && C.type === "number" && D.memoizedProps.value != null && mo(C, "number", C.value);
        }
        switch (te = D ? bl(D) : window, e) {
          case "focusin":
            (Kf(te) || te.contentEditable === "true") && (Ti = te, Do = D, El = null);
            break;
          case "focusout":
            El = Do = Ti = null;
            break;
          case "mousedown":
            Co = !0;
            break;
          case "contextmenu":
          case "mouseup":
          case "dragend":
            Co = !1, ld(B, a, U);
            break;
          case "selectionchange":
            if (ig) break;
          case "keydown":
          case "keyup":
            ld(B, a, U);
        }
        var de;
        if (To)
          e: {
            switch (e) {
              case "compositionstart":
                var ve = "onCompositionStart";
                break e;
              case "compositionend":
                ve = "onCompositionEnd";
                break e;
              case "compositionupdate":
                ve = "onCompositionUpdate";
                break e;
            }
            ve = void 0;
          }
        else
          zi ? Qf(e, a) && (ve = "onCompositionEnd") : e === "keydown" && a.keyCode === 229 && (ve = "onCompositionStart");
        ve && (Vf && a.locale !== "ko" && (zi || ve !== "onCompositionStart" ? ve === "onCompositionEnd" && zi && (de = Uf()) : (Fa = U, xo = "value" in Fa ? Fa.value : Fa.textContent, zi = !0)), te = Sr(D, ve), 0 < te.length && (ve = new Yf(
          ve,
          e,
          null,
          a,
          U
        ), B.push({ event: ve, listeners: te }), de ? ve.data = de : (de = Zf(a), de !== null && (ve.data = de)))), (de = Kv ? Jv(e, a) : Fv(e, a)) && (ve = Sr(D, "onBeforeInput"), 0 < ve.length && (te = new Yf(
          "onBeforeInput",
          "beforeinput",
          null,
          a,
          U
        ), B.push({
          event: te,
          listeners: ve
        }), te.data = de)), Yg(
          B,
          e,
          D,
          a,
          U
        );
      }
      Ch(B, t);
    });
  }
  function Il(e, t, a) {
    return {
      instance: e,
      listener: t,
      currentTarget: a
    };
  }
  function Sr(e, t) {
    for (var a = t + "Capture", l = []; e !== null; ) {
      var o = e, u = o.stateNode;
      if (o = o.tag, o !== 5 && o !== 26 && o !== 27 || u === null || (o = yl(e, a), o != null && l.unshift(
        Il(e, o, u)
      ), o = yl(e, t), o != null && l.push(
        Il(e, o, u)
      )), e.tag === 3) return l;
      e = e.return;
    }
    return [];
  }
  function Gg(e) {
    if (e === null) return null;
    do
      e = e.return;
    while (e && e.tag !== 5 && e.tag !== 27);
    return e || null;
  }
  function Mh(e, t, a, l, o) {
    for (var u = t._reactName, p = []; a !== null && a !== l; ) {
      var b = a, w = b.alternate, D = b.stateNode;
      if (b = b.tag, w !== null && w === l) break;
      b !== 5 && b !== 26 && b !== 27 || D === null || (w = D, o ? (D = yl(a, u), D != null && p.unshift(
        Il(a, D, w)
      )) : o || (D = yl(a, u), D != null && p.push(
        Il(a, D, w)
      ))), a = a.return;
    }
    p.length !== 0 && e.push({ event: t, listeners: p });
  }
  var Qg = /\r\n?/g, Zg = /\u0000|\uFFFD/g;
  function qh(e) {
    return (typeof e == "string" ? e : "" + e).replace(Qg, `
`).replace(Zg, "");
  }
  function jh(e, t) {
    return t = qh(t), qh(e) === t;
  }
  function De(e, t, a, l, o, u) {
    switch (a) {
      case "children":
        typeof l == "string" ? t === "body" || t === "textarea" && l === "" || wi(e, l) : (typeof l == "number" || typeof l == "bigint") && t !== "body" && wi(e, "" + l);
        break;
      case "className":
        As(e, "class", l);
        break;
      case "tabIndex":
        As(e, "tabindex", l);
        break;
      case "dir":
      case "role":
      case "viewBox":
      case "width":
      case "height":
        As(e, a, l);
        break;
      case "style":
        jf(e, l, u);
        break;
      case "data":
        if (t !== "object") {
          As(e, "data", l);
          break;
        }
      case "src":
      case "href":
        if (l === "" && (t !== "a" || a !== "href")) {
          e.removeAttribute(a);
          break;
        }
        if (l == null || typeof l == "function" || typeof l == "symbol" || typeof l == "boolean") {
          e.removeAttribute(a);
          break;
        }
        l = Ds("" + l), e.setAttribute(a, l);
        break;
      case "action":
      case "formAction":
        if (typeof l == "function") {
          e.setAttribute(
            a,
            "javascript:throw new Error('A React form was unexpectedly submitted. If you called form.submit() manually, consider using form.requestSubmit() instead. If you\\'re trying to use event.stopPropagation() in a submit event handler, consider also calling event.preventDefault().')"
          );
          break;
        } else
          typeof u == "function" && (a === "formAction" ? (t !== "input" && De(e, t, "name", o.name, o, null), De(
            e,
            t,
            "formEncType",
            o.formEncType,
            o,
            null
          ), De(
            e,
            t,
            "formMethod",
            o.formMethod,
            o,
            null
          ), De(
            e,
            t,
            "formTarget",
            o.formTarget,
            o,
            null
          )) : (De(e, t, "encType", o.encType, o, null), De(e, t, "method", o.method, o, null), De(e, t, "target", o.target, o, null)));
        if (l == null || typeof l == "symbol" || typeof l == "boolean") {
          e.removeAttribute(a);
          break;
        }
        l = Ds("" + l), e.setAttribute(a, l);
        break;
      case "onClick":
        l != null && (e.onclick = wa);
        break;
      case "onScroll":
        l != null && he("scroll", e);
        break;
      case "onScrollEnd":
        l != null && he("scrollend", e);
        break;
      case "dangerouslySetInnerHTML":
        if (l != null) {
          if (typeof l != "object" || !("__html" in l))
            throw Error(r(61));
          if (a = l.__html, a != null) {
            if (o.children != null) throw Error(r(60));
            e.innerHTML = a;
          }
        }
        break;
      case "multiple":
        e.multiple = l && typeof l != "function" && typeof l != "symbol";
        break;
      case "muted":
        e.muted = l && typeof l != "function" && typeof l != "symbol";
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
        if (l == null || typeof l == "function" || typeof l == "boolean" || typeof l == "symbol") {
          e.removeAttribute("xlink:href");
          break;
        }
        a = Ds("" + l), e.setAttributeNS(
          "http://www.w3.org/1999/xlink",
          "xlink:href",
          a
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
        l != null && typeof l != "function" && typeof l != "symbol" ? e.setAttribute(a, "" + l) : e.removeAttribute(a);
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
        l && typeof l != "function" && typeof l != "symbol" ? e.setAttribute(a, "") : e.removeAttribute(a);
        break;
      case "capture":
      case "download":
        l === !0 ? e.setAttribute(a, "") : l !== !1 && l != null && typeof l != "function" && typeof l != "symbol" ? e.setAttribute(a, l) : e.removeAttribute(a);
        break;
      case "cols":
      case "rows":
      case "size":
      case "span":
        l != null && typeof l != "function" && typeof l != "symbol" && !isNaN(l) && 1 <= l ? e.setAttribute(a, l) : e.removeAttribute(a);
        break;
      case "rowSpan":
      case "start":
        l == null || typeof l == "function" || typeof l == "symbol" || isNaN(l) ? e.removeAttribute(a) : e.setAttribute(a, l);
        break;
      case "popover":
        he("beforetoggle", e), he("toggle", e), Es(e, "popover", l);
        break;
      case "xlinkActuate":
        xa(
          e,
          "http://www.w3.org/1999/xlink",
          "xlink:actuate",
          l
        );
        break;
      case "xlinkArcrole":
        xa(
          e,
          "http://www.w3.org/1999/xlink",
          "xlink:arcrole",
          l
        );
        break;
      case "xlinkRole":
        xa(
          e,
          "http://www.w3.org/1999/xlink",
          "xlink:role",
          l
        );
        break;
      case "xlinkShow":
        xa(
          e,
          "http://www.w3.org/1999/xlink",
          "xlink:show",
          l
        );
        break;
      case "xlinkTitle":
        xa(
          e,
          "http://www.w3.org/1999/xlink",
          "xlink:title",
          l
        );
        break;
      case "xlinkType":
        xa(
          e,
          "http://www.w3.org/1999/xlink",
          "xlink:type",
          l
        );
        break;
      case "xmlBase":
        xa(
          e,
          "http://www.w3.org/XML/1998/namespace",
          "xml:base",
          l
        );
        break;
      case "xmlLang":
        xa(
          e,
          "http://www.w3.org/XML/1998/namespace",
          "xml:lang",
          l
        );
        break;
      case "xmlSpace":
        xa(
          e,
          "http://www.w3.org/XML/1998/namespace",
          "xml:space",
          l
        );
        break;
      case "is":
        Es(e, "is", l);
        break;
      case "innerText":
      case "textContent":
        break;
      default:
        (!(2 < a.length) || a[0] !== "o" && a[0] !== "O" || a[1] !== "n" && a[1] !== "N") && (a = xv.get(a) || a, Es(e, a, l));
    }
  }
  function ac(e, t, a, l, o, u) {
    switch (a) {
      case "style":
        jf(e, l, u);
        break;
      case "dangerouslySetInnerHTML":
        if (l != null) {
          if (typeof l != "object" || !("__html" in l))
            throw Error(r(61));
          if (a = l.__html, a != null) {
            if (o.children != null) throw Error(r(60));
            e.innerHTML = a;
          }
        }
        break;
      case "children":
        typeof l == "string" ? wi(e, l) : (typeof l == "number" || typeof l == "bigint") && wi(e, "" + l);
        break;
      case "onScroll":
        l != null && he("scroll", e);
        break;
      case "onScrollEnd":
        l != null && he("scrollend", e);
        break;
      case "onClick":
        l != null && (e.onclick = wa);
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
        if (!Tf.hasOwnProperty(a))
          e: {
            if (a[0] === "o" && a[1] === "n" && (o = a.endsWith("Capture"), t = a.slice(2, o ? a.length - 7 : void 0), u = e[wt] || null, u = u != null ? u[a] : null, typeof u == "function" && e.removeEventListener(t, u, o), typeof l == "function")) {
              typeof u != "function" && u !== null && (a in e ? e[a] = null : e.hasAttribute(a) && e.removeAttribute(a)), e.addEventListener(t, l, o);
              break e;
            }
            a in e ? e[a] = l : l === !0 ? e.setAttribute(a, "") : Es(e, a, l);
          }
    }
  }
  function dt(e, t, a) {
    switch (t) {
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
        he("error", e), he("load", e);
        var l = !1, o = !1, u;
        for (u in a)
          if (a.hasOwnProperty(u)) {
            var p = a[u];
            if (p != null)
              switch (u) {
                case "src":
                  l = !0;
                  break;
                case "srcSet":
                  o = !0;
                  break;
                case "children":
                case "dangerouslySetInnerHTML":
                  throw Error(r(137, t));
                default:
                  De(e, t, u, p, a, null);
              }
          }
        o && De(e, t, "srcSet", a.srcSet, a, null), l && De(e, t, "src", a.src, a, null);
        return;
      case "input":
        he("invalid", e);
        var b = u = p = o = null, w = null, D = null;
        for (l in a)
          if (a.hasOwnProperty(l)) {
            var U = a[l];
            if (U != null)
              switch (l) {
                case "name":
                  o = U;
                  break;
                case "type":
                  p = U;
                  break;
                case "checked":
                  w = U;
                  break;
                case "defaultChecked":
                  D = U;
                  break;
                case "value":
                  u = U;
                  break;
                case "defaultValue":
                  b = U;
                  break;
                case "children":
                case "dangerouslySetInnerHTML":
                  if (U != null)
                    throw Error(r(137, t));
                  break;
                default:
                  De(e, t, l, U, a, null);
              }
          }
        Cf(
          e,
          u,
          b,
          w,
          D,
          p,
          o,
          !1
        );
        return;
      case "select":
        he("invalid", e), l = p = u = null;
        for (o in a)
          if (a.hasOwnProperty(o) && (b = a[o], b != null))
            switch (o) {
              case "value":
                u = b;
                break;
              case "defaultValue":
                p = b;
                break;
              case "multiple":
                l = b;
              default:
                De(e, t, o, b, a, null);
            }
        t = u, a = p, e.multiple = !!l, t != null ? xi(e, !!l, t, !1) : a != null && xi(e, !!l, a, !0);
        return;
      case "textarea":
        he("invalid", e), u = o = l = null;
        for (p in a)
          if (a.hasOwnProperty(p) && (b = a[p], b != null))
            switch (p) {
              case "value":
                l = b;
                break;
              case "defaultValue":
                o = b;
                break;
              case "children":
                u = b;
                break;
              case "dangerouslySetInnerHTML":
                if (b != null) throw Error(r(91));
                break;
              default:
                De(e, t, p, b, a, null);
            }
        Mf(e, l, o, u);
        return;
      case "option":
        for (w in a)
          if (a.hasOwnProperty(w) && (l = a[w], l != null))
            switch (w) {
              case "selected":
                e.selected = l && typeof l != "function" && typeof l != "symbol";
                break;
              default:
                De(e, t, w, l, a, null);
            }
        return;
      case "dialog":
        he("beforetoggle", e), he("toggle", e), he("cancel", e), he("close", e);
        break;
      case "iframe":
      case "object":
        he("load", e);
        break;
      case "video":
      case "audio":
        for (l = 0; l < Wl.length; l++)
          he(Wl[l], e);
        break;
      case "image":
        he("error", e), he("load", e);
        break;
      case "details":
        he("toggle", e);
        break;
      case "embed":
      case "source":
      case "link":
        he("error", e), he("load", e);
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
        for (D in a)
          if (a.hasOwnProperty(D) && (l = a[D], l != null))
            switch (D) {
              case "children":
              case "dangerouslySetInnerHTML":
                throw Error(r(137, t));
              default:
                De(e, t, D, l, a, null);
            }
        return;
      default:
        if (vo(t)) {
          for (U in a)
            a.hasOwnProperty(U) && (l = a[U], l !== void 0 && ac(
              e,
              t,
              U,
              l,
              a,
              void 0
            ));
          return;
        }
    }
    for (b in a)
      a.hasOwnProperty(b) && (l = a[b], l != null && De(e, t, b, l, a, null));
  }
  function Kg(e, t, a, l) {
    switch (t) {
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
        var o = null, u = null, p = null, b = null, w = null, D = null, U = null;
        for (M in a) {
          var B = a[M];
          if (a.hasOwnProperty(M) && B != null)
            switch (M) {
              case "checked":
                break;
              case "value":
                break;
              case "defaultValue":
                w = B;
              default:
                l.hasOwnProperty(M) || De(e, t, M, null, l, B);
            }
        }
        for (var C in l) {
          var M = l[C];
          if (B = a[C], l.hasOwnProperty(C) && (M != null || B != null))
            switch (C) {
              case "type":
                u = M;
                break;
              case "name":
                o = M;
                break;
              case "checked":
                D = M;
                break;
              case "defaultChecked":
                U = M;
                break;
              case "value":
                p = M;
                break;
              case "defaultValue":
                b = M;
                break;
              case "children":
              case "dangerouslySetInnerHTML":
                if (M != null)
                  throw Error(r(137, t));
                break;
              default:
                M !== B && De(
                  e,
                  t,
                  C,
                  M,
                  l,
                  B
                );
            }
        }
        ho(
          e,
          p,
          b,
          w,
          D,
          U,
          u,
          o
        );
        return;
      case "select":
        M = p = b = C = null;
        for (u in a)
          if (w = a[u], a.hasOwnProperty(u) && w != null)
            switch (u) {
              case "value":
                break;
              case "multiple":
                M = w;
              default:
                l.hasOwnProperty(u) || De(
                  e,
                  t,
                  u,
                  null,
                  l,
                  w
                );
            }
        for (o in l)
          if (u = l[o], w = a[o], l.hasOwnProperty(o) && (u != null || w != null))
            switch (o) {
              case "value":
                C = u;
                break;
              case "defaultValue":
                b = u;
                break;
              case "multiple":
                p = u;
              default:
                u !== w && De(
                  e,
                  t,
                  o,
                  u,
                  l,
                  w
                );
            }
        t = b, a = p, l = M, C != null ? xi(e, !!a, C, !1) : !!l != !!a && (t != null ? xi(e, !!a, t, !0) : xi(e, !!a, a ? [] : "", !1));
        return;
      case "textarea":
        M = C = null;
        for (b in a)
          if (o = a[b], a.hasOwnProperty(b) && o != null && !l.hasOwnProperty(b))
            switch (b) {
              case "value":
                break;
              case "children":
                break;
              default:
                De(e, t, b, null, l, o);
            }
        for (p in l)
          if (o = l[p], u = a[p], l.hasOwnProperty(p) && (o != null || u != null))
            switch (p) {
              case "value":
                C = o;
                break;
              case "defaultValue":
                M = o;
                break;
              case "children":
                break;
              case "dangerouslySetInnerHTML":
                if (o != null) throw Error(r(91));
                break;
              default:
                o !== u && De(e, t, p, o, l, u);
            }
        Nf(e, C, M);
        return;
      case "option":
        for (var P in a)
          if (C = a[P], a.hasOwnProperty(P) && C != null && !l.hasOwnProperty(P))
            switch (P) {
              case "selected":
                e.selected = !1;
                break;
              default:
                De(
                  e,
                  t,
                  P,
                  null,
                  l,
                  C
                );
            }
        for (w in l)
          if (C = l[w], M = a[w], l.hasOwnProperty(w) && C !== M && (C != null || M != null))
            switch (w) {
              case "selected":
                e.selected = C && typeof C != "function" && typeof C != "symbol";
                break;
              default:
                De(
                  e,
                  t,
                  w,
                  C,
                  l,
                  M
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
        for (var ie in a)
          C = a[ie], a.hasOwnProperty(ie) && C != null && !l.hasOwnProperty(ie) && De(e, t, ie, null, l, C);
        for (D in l)
          if (C = l[D], M = a[D], l.hasOwnProperty(D) && C !== M && (C != null || M != null))
            switch (D) {
              case "children":
              case "dangerouslySetInnerHTML":
                if (C != null)
                  throw Error(r(137, t));
                break;
              default:
                De(
                  e,
                  t,
                  D,
                  C,
                  l,
                  M
                );
            }
        return;
      default:
        if (vo(t)) {
          for (var Ce in a)
            C = a[Ce], a.hasOwnProperty(Ce) && C !== void 0 && !l.hasOwnProperty(Ce) && ac(
              e,
              t,
              Ce,
              void 0,
              l,
              C
            );
          for (U in l)
            C = l[U], M = a[U], !l.hasOwnProperty(U) || C === M || C === void 0 && M === void 0 || ac(
              e,
              t,
              U,
              C,
              l,
              M
            );
          return;
        }
    }
    for (var E in a)
      C = a[E], a.hasOwnProperty(E) && C != null && !l.hasOwnProperty(E) && De(e, t, E, null, l, C);
    for (B in l)
      C = l[B], M = a[B], !l.hasOwnProperty(B) || C === M || C == null && M == null || De(e, t, B, C, l, M);
  }
  function Rh(e) {
    switch (e) {
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
  function Jg() {
    if (typeof performance.getEntriesByType == "function") {
      for (var e = 0, t = 0, a = performance.getEntriesByType("resource"), l = 0; l < a.length; l++) {
        var o = a[l], u = o.transferSize, p = o.initiatorType, b = o.duration;
        if (u && b && Rh(p)) {
          for (p = 0, b = o.responseEnd, l += 1; l < a.length; l++) {
            var w = a[l], D = w.startTime;
            if (D > b) break;
            var U = w.transferSize, B = w.initiatorType;
            U && Rh(B) && (w = w.responseEnd, p += U * (w < b ? 1 : (b - D) / (w - D)));
          }
          if (--l, t += 8 * (u + p) / (o.duration / 1e3), e++, 10 < e) break;
        }
      }
      if (0 < e) return t / e / 1e6;
    }
    return navigator.connection && (e = navigator.connection.downlink, typeof e == "number") ? e : 5;
  }
  var nc = null, ic = null;
  function zr(e) {
    return e.nodeType === 9 ? e : e.ownerDocument;
  }
  function $h(e) {
    switch (e) {
      case "http://www.w3.org/2000/svg":
        return 1;
      case "http://www.w3.org/1998/Math/MathML":
        return 2;
      default:
        return 0;
    }
  }
  function Uh(e, t) {
    if (e === 0)
      switch (t) {
        case "svg":
          return 1;
        case "math":
          return 2;
        default:
          return 0;
      }
    return e === 1 && t === "foreignObject" ? 0 : e;
  }
  function lc(e, t) {
    return e === "textarea" || e === "noscript" || typeof t.children == "string" || typeof t.children == "number" || typeof t.children == "bigint" || typeof t.dangerouslySetInnerHTML == "object" && t.dangerouslySetInnerHTML !== null && t.dangerouslySetInnerHTML.__html != null;
  }
  var sc = null;
  function Fg() {
    var e = window.event;
    return e && e.type === "popstate" ? e === sc ? !1 : (sc = e, !0) : (sc = null, !1);
  }
  var Lh = typeof setTimeout == "function" ? setTimeout : void 0, Wg = typeof clearTimeout == "function" ? clearTimeout : void 0, Hh = typeof Promise == "function" ? Promise : void 0, Ig = typeof queueMicrotask == "function" ? queueMicrotask : typeof Hh < "u" ? function(e) {
    return Hh.resolve(null).then(e).catch(Pg);
  } : Lh;
  function Pg(e) {
    setTimeout(function() {
      throw e;
    });
  }
  function hn(e) {
    return e === "head";
  }
  function Yh(e, t) {
    var a = t, l = 0;
    do {
      var o = a.nextSibling;
      if (e.removeChild(a), o && o.nodeType === 8)
        if (a = o.data, a === "/$" || a === "/&") {
          if (l === 0) {
            e.removeChild(o), Pi(t);
            return;
          }
          l--;
        } else if (a === "$" || a === "$?" || a === "$~" || a === "$!" || a === "&")
          l++;
        else if (a === "html")
          Pl(e.ownerDocument.documentElement);
        else if (a === "head") {
          a = e.ownerDocument.head, Pl(a);
          for (var u = a.firstChild; u; ) {
            var p = u.nextSibling, b = u.nodeName;
            u[gl] || b === "SCRIPT" || b === "STYLE" || b === "LINK" && u.rel.toLowerCase() === "stylesheet" || a.removeChild(u), u = p;
          }
        } else
          a === "body" && Pl(e.ownerDocument.body);
      a = o;
    } while (a);
    Pi(t);
  }
  function Bh(e, t) {
    var a = e;
    e = 0;
    do {
      var l = a.nextSibling;
      if (a.nodeType === 1 ? t ? (a._stashedDisplay = a.style.display, a.style.display = "none") : (a.style.display = a._stashedDisplay || "", a.getAttribute("style") === "" && a.removeAttribute("style")) : a.nodeType === 3 && (t ? (a._stashedText = a.nodeValue, a.nodeValue = "") : a.nodeValue = a._stashedText || ""), l && l.nodeType === 8)
        if (a = l.data, a === "/$") {
          if (e === 0) break;
          e--;
        } else
          a !== "$" && a !== "$?" && a !== "$~" && a !== "$!" || e++;
      a = l;
    } while (a);
  }
  function rc(e) {
    var t = e.firstChild;
    for (t && t.nodeType === 10 && (t = t.nextSibling); t; ) {
      var a = t;
      switch (t = t.nextSibling, a.nodeName) {
        case "HTML":
        case "HEAD":
        case "BODY":
          rc(a), fo(a);
          continue;
        case "SCRIPT":
        case "STYLE":
          continue;
        case "LINK":
          if (a.rel.toLowerCase() === "stylesheet") continue;
      }
      e.removeChild(a);
    }
  }
  function eb(e, t, a, l) {
    for (; e.nodeType === 1; ) {
      var o = a;
      if (e.nodeName.toLowerCase() !== t.toLowerCase()) {
        if (!l && (e.nodeName !== "INPUT" || e.type !== "hidden"))
          break;
      } else if (l) {
        if (!e[gl])
          switch (t) {
            case "meta":
              if (!e.hasAttribute("itemprop")) break;
              return e;
            case "link":
              if (u = e.getAttribute("rel"), u === "stylesheet" && e.hasAttribute("data-precedence"))
                break;
              if (u !== o.rel || e.getAttribute("href") !== (o.href == null || o.href === "" ? null : o.href) || e.getAttribute("crossorigin") !== (o.crossOrigin == null ? null : o.crossOrigin) || e.getAttribute("title") !== (o.title == null ? null : o.title))
                break;
              return e;
            case "style":
              if (e.hasAttribute("data-precedence")) break;
              return e;
            case "script":
              if (u = e.getAttribute("src"), (u !== (o.src == null ? null : o.src) || e.getAttribute("type") !== (o.type == null ? null : o.type) || e.getAttribute("crossorigin") !== (o.crossOrigin == null ? null : o.crossOrigin)) && u && e.hasAttribute("async") && !e.hasAttribute("itemprop"))
                break;
              return e;
            default:
              return e;
          }
      } else if (t === "input" && e.type === "hidden") {
        var u = o.name == null ? null : "" + o.name;
        if (o.type === "hidden" && e.getAttribute("name") === u)
          return e;
      } else return e;
      if (e = Jt(e.nextSibling), e === null) break;
    }
    return null;
  }
  function tb(e, t, a) {
    if (t === "") return null;
    for (; e.nodeType !== 3; )
      if ((e.nodeType !== 1 || e.nodeName !== "INPUT" || e.type !== "hidden") && !a || (e = Jt(e.nextSibling), e === null)) return null;
    return e;
  }
  function Vh(e, t) {
    for (; e.nodeType !== 8; )
      if ((e.nodeType !== 1 || e.nodeName !== "INPUT" || e.type !== "hidden") && !t || (e = Jt(e.nextSibling), e === null)) return null;
    return e;
  }
  function oc(e) {
    return e.data === "$?" || e.data === "$~";
  }
  function uc(e) {
    return e.data === "$!" || e.data === "$?" && e.ownerDocument.readyState !== "loading";
  }
  function ab(e, t) {
    var a = e.ownerDocument;
    if (e.data === "$~") e._reactRetry = t;
    else if (e.data !== "$?" || a.readyState !== "loading")
      t();
    else {
      var l = function() {
        t(), a.removeEventListener("DOMContentLoaded", l);
      };
      a.addEventListener("DOMContentLoaded", l), e._reactRetry = l;
    }
  }
  function Jt(e) {
    for (; e != null; e = e.nextSibling) {
      var t = e.nodeType;
      if (t === 1 || t === 3) break;
      if (t === 8) {
        if (t = e.data, t === "$" || t === "$!" || t === "$?" || t === "$~" || t === "&" || t === "F!" || t === "F")
          break;
        if (t === "/$" || t === "/&") return null;
      }
    }
    return e;
  }
  var cc = null;
  function Xh(e) {
    e = e.nextSibling;
    for (var t = 0; e; ) {
      if (e.nodeType === 8) {
        var a = e.data;
        if (a === "/$" || a === "/&") {
          if (t === 0)
            return Jt(e.nextSibling);
          t--;
        } else
          a !== "$" && a !== "$!" && a !== "$?" && a !== "$~" && a !== "&" || t++;
      }
      e = e.nextSibling;
    }
    return null;
  }
  function Gh(e) {
    e = e.previousSibling;
    for (var t = 0; e; ) {
      if (e.nodeType === 8) {
        var a = e.data;
        if (a === "$" || a === "$!" || a === "$?" || a === "$~" || a === "&") {
          if (t === 0) return e;
          t--;
        } else a !== "/$" && a !== "/&" || t++;
      }
      e = e.previousSibling;
    }
    return null;
  }
  function Qh(e, t, a) {
    switch (t = zr(a), e) {
      case "html":
        if (e = t.documentElement, !e) throw Error(r(452));
        return e;
      case "head":
        if (e = t.head, !e) throw Error(r(453));
        return e;
      case "body":
        if (e = t.body, !e) throw Error(r(454));
        return e;
      default:
        throw Error(r(451));
    }
  }
  function Pl(e) {
    for (var t = e.attributes; t.length; )
      e.removeAttributeNode(t[0]);
    fo(e);
  }
  var Ft = /* @__PURE__ */ new Map(), Zh = /* @__PURE__ */ new Set();
  function Tr(e) {
    return typeof e.getRootNode == "function" ? e.getRootNode() : e.nodeType === 9 ? e : e.ownerDocument;
  }
  var Ua = Q.d;
  Q.d = {
    f: nb,
    r: ib,
    D: lb,
    C: sb,
    L: rb,
    m: ob,
    X: cb,
    S: ub,
    M: fb
  };
  function nb() {
    var e = Ua.f(), t = gr();
    return e || t;
  }
  function ib(e) {
    var t = bi(e);
    t !== null && t.tag === 5 && t.type === "form" ? cp(t) : Ua.r(e);
  }
  var Fi = typeof document > "u" ? null : document;
  function Kh(e, t, a) {
    var l = Fi;
    if (l && typeof t == "string" && t) {
      var o = Bt(t);
      o = 'link[rel="' + e + '"][href="' + o + '"]', typeof a == "string" && (o += '[crossorigin="' + a + '"]'), Zh.has(o) || (Zh.add(o), e = { rel: e, crossOrigin: a, href: t }, l.querySelector(o) === null && (t = l.createElement("link"), dt(t, "link", e), it(t), l.head.appendChild(t)));
    }
  }
  function lb(e) {
    Ua.D(e), Kh("dns-prefetch", e, null);
  }
  function sb(e, t) {
    Ua.C(e, t), Kh("preconnect", e, t);
  }
  function rb(e, t, a) {
    Ua.L(e, t, a);
    var l = Fi;
    if (l && e && t) {
      var o = 'link[rel="preload"][as="' + Bt(t) + '"]';
      t === "image" && a && a.imageSrcSet ? (o += '[imagesrcset="' + Bt(
        a.imageSrcSet
      ) + '"]', typeof a.imageSizes == "string" && (o += '[imagesizes="' + Bt(
        a.imageSizes
      ) + '"]')) : o += '[href="' + Bt(e) + '"]';
      var u = o;
      switch (t) {
        case "style":
          u = Wi(e);
          break;
        case "script":
          u = Ii(e);
      }
      Ft.has(u) || (e = m(
        {
          rel: "preload",
          href: t === "image" && a && a.imageSrcSet ? void 0 : e,
          as: t
        },
        a
      ), Ft.set(u, e), l.querySelector(o) !== null || t === "style" && l.querySelector(es(u)) || t === "script" && l.querySelector(ts(u)) || (t = l.createElement("link"), dt(t, "link", e), it(t), l.head.appendChild(t)));
    }
  }
  function ob(e, t) {
    Ua.m(e, t);
    var a = Fi;
    if (a && e) {
      var l = t && typeof t.as == "string" ? t.as : "script", o = 'link[rel="modulepreload"][as="' + Bt(l) + '"][href="' + Bt(e) + '"]', u = o;
      switch (l) {
        case "audioworklet":
        case "paintworklet":
        case "serviceworker":
        case "sharedworker":
        case "worker":
        case "script":
          u = Ii(e);
      }
      if (!Ft.has(u) && (e = m({ rel: "modulepreload", href: e }, t), Ft.set(u, e), a.querySelector(o) === null)) {
        switch (l) {
          case "audioworklet":
          case "paintworklet":
          case "serviceworker":
          case "sharedworker":
          case "worker":
          case "script":
            if (a.querySelector(ts(u)))
              return;
        }
        l = a.createElement("link"), dt(l, "link", e), it(l), a.head.appendChild(l);
      }
    }
  }
  function ub(e, t, a) {
    Ua.S(e, t, a);
    var l = Fi;
    if (l && e) {
      var o = yi(l).hoistableStyles, u = Wi(e);
      t = t || "default";
      var p = o.get(u);
      if (!p) {
        var b = { loading: 0, preload: null };
        if (p = l.querySelector(
          es(u)
        ))
          b.loading = 5;
        else {
          e = m(
            { rel: "stylesheet", href: e, "data-precedence": t },
            a
          ), (a = Ft.get(u)) && fc(e, a);
          var w = p = l.createElement("link");
          it(w), dt(w, "link", e), w._p = new Promise(function(D, U) {
            w.onload = D, w.onerror = U;
          }), w.addEventListener("load", function() {
            b.loading |= 1;
          }), w.addEventListener("error", function() {
            b.loading |= 2;
          }), b.loading |= 4, Er(p, t, l);
        }
        p = {
          type: "stylesheet",
          instance: p,
          count: 1,
          state: b
        }, o.set(u, p);
      }
    }
  }
  function cb(e, t) {
    Ua.X(e, t);
    var a = Fi;
    if (a && e) {
      var l = yi(a).hoistableScripts, o = Ii(e), u = l.get(o);
      u || (u = a.querySelector(ts(o)), u || (e = m({ src: e, async: !0 }, t), (t = Ft.get(o)) && dc(e, t), u = a.createElement("script"), it(u), dt(u, "link", e), a.head.appendChild(u)), u = {
        type: "script",
        instance: u,
        count: 1,
        state: null
      }, l.set(o, u));
    }
  }
  function fb(e, t) {
    Ua.M(e, t);
    var a = Fi;
    if (a && e) {
      var l = yi(a).hoistableScripts, o = Ii(e), u = l.get(o);
      u || (u = a.querySelector(ts(o)), u || (e = m({ src: e, async: !0, type: "module" }, t), (t = Ft.get(o)) && dc(e, t), u = a.createElement("script"), it(u), dt(u, "link", e), a.head.appendChild(u)), u = {
        type: "script",
        instance: u,
        count: 1,
        state: null
      }, l.set(o, u));
    }
  }
  function Jh(e, t, a, l) {
    var o = (o = fe.current) ? Tr(o) : null;
    if (!o) throw Error(r(446));
    switch (e) {
      case "meta":
      case "title":
        return null;
      case "style":
        return typeof a.precedence == "string" && typeof a.href == "string" ? (t = Wi(a.href), a = yi(
          o
        ).hoistableStyles, l = a.get(t), l || (l = {
          type: "style",
          instance: null,
          count: 0,
          state: null
        }, a.set(t, l)), l) : { type: "void", instance: null, count: 0, state: null };
      case "link":
        if (a.rel === "stylesheet" && typeof a.href == "string" && typeof a.precedence == "string") {
          e = Wi(a.href);
          var u = yi(
            o
          ).hoistableStyles, p = u.get(e);
          if (p || (o = o.ownerDocument || o, p = {
            type: "stylesheet",
            instance: null,
            count: 0,
            state: { loading: 0, preload: null }
          }, u.set(e, p), (u = o.querySelector(
            es(e)
          )) && !u._p && (p.instance = u, p.state.loading = 5), Ft.has(e) || (a = {
            rel: "preload",
            as: "style",
            href: a.href,
            crossOrigin: a.crossOrigin,
            integrity: a.integrity,
            media: a.media,
            hrefLang: a.hrefLang,
            referrerPolicy: a.referrerPolicy
          }, Ft.set(e, a), u || db(
            o,
            e,
            a,
            p.state
          ))), t && l === null)
            throw Error(r(528, ""));
          return p;
        }
        if (t && l !== null)
          throw Error(r(529, ""));
        return null;
      case "script":
        return t = a.async, a = a.src, typeof a == "string" && t && typeof t != "function" && typeof t != "symbol" ? (t = Ii(a), a = yi(
          o
        ).hoistableScripts, l = a.get(t), l || (l = {
          type: "script",
          instance: null,
          count: 0,
          state: null
        }, a.set(t, l)), l) : { type: "void", instance: null, count: 0, state: null };
      default:
        throw Error(r(444, e));
    }
  }
  function Wi(e) {
    return 'href="' + Bt(e) + '"';
  }
  function es(e) {
    return 'link[rel="stylesheet"][' + e + "]";
  }
  function Fh(e) {
    return m({}, e, {
      "data-precedence": e.precedence,
      precedence: null
    });
  }
  function db(e, t, a, l) {
    e.querySelector('link[rel="preload"][as="style"][' + t + "]") ? l.loading = 1 : (t = e.createElement("link"), l.preload = t, t.addEventListener("load", function() {
      return l.loading |= 1;
    }), t.addEventListener("error", function() {
      return l.loading |= 2;
    }), dt(t, "link", a), it(t), e.head.appendChild(t));
  }
  function Ii(e) {
    return '[src="' + Bt(e) + '"]';
  }
  function ts(e) {
    return "script[async]" + e;
  }
  function Wh(e, t, a) {
    if (t.count++, t.instance === null)
      switch (t.type) {
        case "style":
          var l = e.querySelector(
            'style[data-href~="' + Bt(a.href) + '"]'
          );
          if (l)
            return t.instance = l, it(l), l;
          var o = m({}, a, {
            "data-href": a.href,
            "data-precedence": a.precedence,
            href: null,
            precedence: null
          });
          return l = (e.ownerDocument || e).createElement(
            "style"
          ), it(l), dt(l, "style", o), Er(l, a.precedence, e), t.instance = l;
        case "stylesheet":
          o = Wi(a.href);
          var u = e.querySelector(
            es(o)
          );
          if (u)
            return t.state.loading |= 4, t.instance = u, it(u), u;
          l = Fh(a), (o = Ft.get(o)) && fc(l, o), u = (e.ownerDocument || e).createElement("link"), it(u);
          var p = u;
          return p._p = new Promise(function(b, w) {
            p.onload = b, p.onerror = w;
          }), dt(u, "link", l), t.state.loading |= 4, Er(u, a.precedence, e), t.instance = u;
        case "script":
          return u = Ii(a.src), (o = e.querySelector(
            ts(u)
          )) ? (t.instance = o, it(o), o) : (l = a, (o = Ft.get(u)) && (l = m({}, a), dc(l, o)), e = e.ownerDocument || e, o = e.createElement("script"), it(o), dt(o, "link", l), e.head.appendChild(o), t.instance = o);
        case "void":
          return null;
        default:
          throw Error(r(443, t.type));
      }
    else
      t.type === "stylesheet" && (t.state.loading & 4) === 0 && (l = t.instance, t.state.loading |= 4, Er(l, a.precedence, e));
    return t.instance;
  }
  function Er(e, t, a) {
    for (var l = a.querySelectorAll(
      'link[rel="stylesheet"][data-precedence],style[data-precedence]'
    ), o = l.length ? l[l.length - 1] : null, u = o, p = 0; p < l.length; p++) {
      var b = l[p];
      if (b.dataset.precedence === t) u = b;
      else if (u !== o) break;
    }
    u ? u.parentNode.insertBefore(e, u.nextSibling) : (t = a.nodeType === 9 ? a.head : a, t.insertBefore(e, t.firstChild));
  }
  function fc(e, t) {
    e.crossOrigin == null && (e.crossOrigin = t.crossOrigin), e.referrerPolicy == null && (e.referrerPolicy = t.referrerPolicy), e.title == null && (e.title = t.title);
  }
  function dc(e, t) {
    e.crossOrigin == null && (e.crossOrigin = t.crossOrigin), e.referrerPolicy == null && (e.referrerPolicy = t.referrerPolicy), e.integrity == null && (e.integrity = t.integrity);
  }
  var Ar = null;
  function Ih(e, t, a) {
    if (Ar === null) {
      var l = /* @__PURE__ */ new Map(), o = Ar = /* @__PURE__ */ new Map();
      o.set(a, l);
    } else
      o = Ar, l = o.get(a), l || (l = /* @__PURE__ */ new Map(), o.set(a, l));
    if (l.has(e)) return l;
    for (l.set(e, null), a = a.getElementsByTagName(e), o = 0; o < a.length; o++) {
      var u = a[o];
      if (!(u[gl] || u[ot] || e === "link" && u.getAttribute("rel") === "stylesheet") && u.namespaceURI !== "http://www.w3.org/2000/svg") {
        var p = u.getAttribute(t) || "";
        p = e + p;
        var b = l.get(p);
        b ? b.push(u) : l.set(p, [u]);
      }
    }
    return l;
  }
  function Ph(e, t, a) {
    e = e.ownerDocument || e, e.head.insertBefore(
      a,
      t === "title" ? e.querySelector("head > title") : null
    );
  }
  function pb(e, t, a) {
    if (a === 1 || t.itemProp != null) return !1;
    switch (e) {
      case "meta":
      case "title":
        return !0;
      case "style":
        if (typeof t.precedence != "string" || typeof t.href != "string" || t.href === "")
          break;
        return !0;
      case "link":
        if (typeof t.rel != "string" || typeof t.href != "string" || t.href === "" || t.onLoad || t.onError)
          break;
        switch (t.rel) {
          case "stylesheet":
            return e = t.disabled, typeof t.precedence == "string" && e == null;
          default:
            return !0;
        }
      case "script":
        if (t.async && typeof t.async != "function" && typeof t.async != "symbol" && !t.onLoad && !t.onError && t.src && typeof t.src == "string")
          return !0;
    }
    return !1;
  }
  function e0(e) {
    return !(e.type === "stylesheet" && (e.state.loading & 3) === 0);
  }
  function hb(e, t, a, l) {
    if (a.type === "stylesheet" && (typeof l.media != "string" || matchMedia(l.media).matches !== !1) && (a.state.loading & 4) === 0) {
      if (a.instance === null) {
        var o = Wi(l.href), u = t.querySelector(
          es(o)
        );
        if (u) {
          t = u._p, t !== null && typeof t == "object" && typeof t.then == "function" && (e.count++, e = Or.bind(e), t.then(e, e)), a.state.loading |= 4, a.instance = u, it(u);
          return;
        }
        u = t.ownerDocument || t, l = Fh(l), (o = Ft.get(o)) && fc(l, o), u = u.createElement("link"), it(u);
        var p = u;
        p._p = new Promise(function(b, w) {
          p.onload = b, p.onerror = w;
        }), dt(u, "link", l), a.instance = u;
      }
      e.stylesheets === null && (e.stylesheets = /* @__PURE__ */ new Map()), e.stylesheets.set(a, t), (t = a.state.preload) && (a.state.loading & 3) === 0 && (e.count++, a = Or.bind(e), t.addEventListener("load", a), t.addEventListener("error", a));
    }
  }
  var pc = 0;
  function mb(e, t) {
    return e.stylesheets && e.count === 0 && Cr(e, e.stylesheets), 0 < e.count || 0 < e.imgCount ? function(a) {
      var l = setTimeout(function() {
        if (e.stylesheets && Cr(e, e.stylesheets), e.unsuspend) {
          var u = e.unsuspend;
          e.unsuspend = null, u();
        }
      }, 6e4 + t);
      0 < e.imgBytes && pc === 0 && (pc = 62500 * Jg());
      var o = setTimeout(
        function() {
          if (e.waitingForImages = !1, e.count === 0 && (e.stylesheets && Cr(e, e.stylesheets), e.unsuspend)) {
            var u = e.unsuspend;
            e.unsuspend = null, u();
          }
        },
        (e.imgBytes > pc ? 50 : 800) + t
      );
      return e.unsuspend = a, function() {
        e.unsuspend = null, clearTimeout(l), clearTimeout(o);
      };
    } : null;
  }
  function Or() {
    if (this.count--, this.count === 0 && (this.imgCount === 0 || !this.waitingForImages)) {
      if (this.stylesheets) Cr(this, this.stylesheets);
      else if (this.unsuspend) {
        var e = this.unsuspend;
        this.unsuspend = null, e();
      }
    }
  }
  var Dr = null;
  function Cr(e, t) {
    e.stylesheets = null, e.unsuspend !== null && (e.count++, Dr = /* @__PURE__ */ new Map(), t.forEach(vb, e), Dr = null, Or.call(e));
  }
  function vb(e, t) {
    if (!(t.state.loading & 4)) {
      var a = Dr.get(e);
      if (a) var l = a.get(null);
      else {
        a = /* @__PURE__ */ new Map(), Dr.set(e, a);
        for (var o = e.querySelectorAll(
          "link[data-precedence],style[data-precedence]"
        ), u = 0; u < o.length; u++) {
          var p = o[u];
          (p.nodeName === "LINK" || p.getAttribute("media") !== "not all") && (a.set(p.dataset.precedence, p), l = p);
        }
        l && a.set(null, l);
      }
      o = t.instance, p = o.getAttribute("data-precedence"), u = a.get(p) || l, u === l && a.set(null, o), a.set(p, o), this.count++, l = Or.bind(this), o.addEventListener("load", l), o.addEventListener("error", l), u ? u.parentNode.insertBefore(o, u.nextSibling) : (e = e.nodeType === 9 ? e.head : e, e.insertBefore(o, e.firstChild)), t.state.loading |= 4;
    }
  }
  var as = {
    $$typeof: j,
    Provider: null,
    Consumer: null,
    _currentValue: le,
    _currentValue2: le,
    _threadCount: 0
  };
  function gb(e, t, a, l, o, u, p, b, w) {
    this.tag = 1, this.containerInfo = e, this.pingCache = this.current = this.pendingChildren = null, this.timeoutHandle = -1, this.callbackNode = this.next = this.pendingContext = this.context = this.cancelPendingCommit = null, this.callbackPriority = 0, this.expirationTimes = vl(-1), this.entangledLanes = this.shellSuspendCounter = this.errorRecoveryDisabledLanes = this.expiredLanes = this.warmLanes = this.pingedLanes = this.suspendedLanes = this.pendingLanes = 0, this.entanglements = vl(0), this.hiddenUpdates = vl(null), this.identifierPrefix = l, this.onUncaughtError = o, this.onCaughtError = u, this.onRecoverableError = p, this.pooledCache = null, this.pooledCacheLanes = 0, this.formState = w, this.incompleteTransitions = /* @__PURE__ */ new Map();
  }
  function t0(e, t, a, l, o, u, p, b, w, D, U, B) {
    return e = new gb(
      e,
      t,
      a,
      p,
      w,
      D,
      U,
      B,
      b
    ), t = 1, u === !0 && (t |= 24), u = Mt(3, null, null, t), e.current = u, u.stateNode = e, t = Qo(), t.refCount++, e.pooledCache = t, t.refCount++, u.memoizedState = {
      element: l,
      isDehydrated: a,
      cache: t
    }, Fo(u), e;
  }
  function a0(e) {
    return e ? (e = Oi, e) : Oi;
  }
  function n0(e, t, a, l, o, u) {
    o = a0(o), l.context === null ? l.context = o : l.pendingContext = o, l = an(t), l.payload = { element: a }, u = u === void 0 ? null : u, u !== null && (l.callback = u), a = nn(e, l, t), a !== null && (At(a, e, t), ql(a, e, t));
  }
  function i0(e, t) {
    if (e = e.memoizedState, e !== null && e.dehydrated !== null) {
      var a = e.retryLane;
      e.retryLane = a !== 0 && a < t ? a : t;
    }
  }
  function hc(e, t) {
    i0(e, t), (e = e.alternate) && i0(e, t);
  }
  function l0(e) {
    if (e.tag === 13 || e.tag === 31) {
      var t = Hn(e, 67108864);
      t !== null && At(t, e, 67108864), hc(e, 67108864);
    }
  }
  function s0(e) {
    if (e.tag === 13 || e.tag === 31) {
      var t = Ut();
      t = mi(t);
      var a = Hn(e, t);
      a !== null && At(a, e, t), hc(e, t);
    }
  }
  var Nr = !0;
  function bb(e, t, a, l) {
    var o = R.T;
    R.T = null;
    var u = Q.p;
    try {
      Q.p = 2, mc(e, t, a, l);
    } finally {
      Q.p = u, R.T = o;
    }
  }
  function yb(e, t, a, l) {
    var o = R.T;
    R.T = null;
    var u = Q.p;
    try {
      Q.p = 8, mc(e, t, a, l);
    } finally {
      Q.p = u, R.T = o;
    }
  }
  function mc(e, t, a, l) {
    if (Nr) {
      var o = vc(l);
      if (o === null)
        tc(
          e,
          t,
          l,
          Mr,
          a
        ), o0(e, l);
      else if (xb(
        o,
        e,
        t,
        a,
        l
      ))
        l.stopPropagation();
      else if (o0(e, l), t & 4 && -1 < _b.indexOf(e)) {
        for (; o !== null; ) {
          var u = bi(o);
          if (u !== null)
            switch (u.tag) {
              case 3:
                if (u = u.stateNode, u.current.memoizedState.isDehydrated) {
                  var p = ua(u.pendingLanes);
                  if (p !== 0) {
                    var b = u;
                    for (b.pendingLanes |= 2, b.entangledLanes |= 2; p; ) {
                      var w = 1 << 31 - rt(p);
                      b.entanglements[1] |= w, p &= ~w;
                    }
                    ha(u), (ze & 6) === 0 && (mr = Me() + 500, Fl(0));
                  }
                }
                break;
              case 31:
              case 13:
                b = Hn(u, 2), b !== null && At(b, u, 2), gr(), hc(u, 2);
            }
          if (u = vc(l), u === null && tc(
            e,
            t,
            l,
            Mr,
            a
          ), u === o) break;
          o = u;
        }
        o !== null && l.stopPropagation();
      } else
        tc(
          e,
          t,
          l,
          null,
          a
        );
    }
  }
  function vc(e) {
    return e = bo(e), gc(e);
  }
  var Mr = null;
  function gc(e) {
    if (Mr = null, e = gi(e), e !== null) {
      var t = f(e);
      if (t === null) e = null;
      else {
        var a = t.tag;
        if (a === 13) {
          if (e = d(t), e !== null) return e;
          e = null;
        } else if (a === 31) {
          if (e = h(t), e !== null) return e;
          e = null;
        } else if (a === 3) {
          if (t.stateNode.current.memoizedState.isDehydrated)
            return t.tag === 3 ? t.stateNode.containerInfo : null;
          e = null;
        } else t !== e && (e = null);
      }
    }
    return Mr = e, null;
  }
  function r0(e) {
    switch (e) {
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
        switch (ks()) {
          case Nn:
            return 2;
          case Mn:
            return 8;
          case di:
          case so:
            return 32;
          case pi:
            return 268435456;
          default:
            return 32;
        }
      default:
        return 32;
    }
  }
  var bc = !1, mn = null, vn = null, gn = null, ns = /* @__PURE__ */ new Map(), is = /* @__PURE__ */ new Map(), bn = [], _b = "mousedown mouseup touchcancel touchend touchstart auxclick dblclick pointercancel pointerdown pointerup dragend dragstart drop compositionend compositionstart keydown keypress keyup input textInput copy cut paste click change contextmenu reset".split(
    " "
  );
  function o0(e, t) {
    switch (e) {
      case "focusin":
      case "focusout":
        mn = null;
        break;
      case "dragenter":
      case "dragleave":
        vn = null;
        break;
      case "mouseover":
      case "mouseout":
        gn = null;
        break;
      case "pointerover":
      case "pointerout":
        ns.delete(t.pointerId);
        break;
      case "gotpointercapture":
      case "lostpointercapture":
        is.delete(t.pointerId);
    }
  }
  function ls(e, t, a, l, o, u) {
    return e === null || e.nativeEvent !== u ? (e = {
      blockedOn: t,
      domEventName: a,
      eventSystemFlags: l,
      nativeEvent: u,
      targetContainers: [o]
    }, t !== null && (t = bi(t), t !== null && l0(t)), e) : (e.eventSystemFlags |= l, t = e.targetContainers, o !== null && t.indexOf(o) === -1 && t.push(o), e);
  }
  function xb(e, t, a, l, o) {
    switch (t) {
      case "focusin":
        return mn = ls(
          mn,
          e,
          t,
          a,
          l,
          o
        ), !0;
      case "dragenter":
        return vn = ls(
          vn,
          e,
          t,
          a,
          l,
          o
        ), !0;
      case "mouseover":
        return gn = ls(
          gn,
          e,
          t,
          a,
          l,
          o
        ), !0;
      case "pointerover":
        var u = o.pointerId;
        return ns.set(
          u,
          ls(
            ns.get(u) || null,
            e,
            t,
            a,
            l,
            o
          )
        ), !0;
      case "gotpointercapture":
        return u = o.pointerId, is.set(
          u,
          ls(
            is.get(u) || null,
            e,
            t,
            a,
            l,
            o
          )
        ), !0;
    }
    return !1;
  }
  function u0(e) {
    var t = gi(e.target);
    if (t !== null) {
      var a = f(t);
      if (a !== null) {
        if (t = a.tag, t === 13) {
          if (t = d(a), t !== null) {
            e.blockedOn = t, kf(e.priority, function() {
              s0(a);
            });
            return;
          }
        } else if (t === 31) {
          if (t = h(a), t !== null) {
            e.blockedOn = t, kf(e.priority, function() {
              s0(a);
            });
            return;
          }
        } else if (t === 3 && a.stateNode.current.memoizedState.isDehydrated) {
          e.blockedOn = a.tag === 3 ? a.stateNode.containerInfo : null;
          return;
        }
      }
    }
    e.blockedOn = null;
  }
  function qr(e) {
    if (e.blockedOn !== null) return !1;
    for (var t = e.targetContainers; 0 < t.length; ) {
      var a = vc(e.nativeEvent);
      if (a === null) {
        a = e.nativeEvent;
        var l = new a.constructor(
          a.type,
          a
        );
        go = l, a.target.dispatchEvent(l), go = null;
      } else
        return t = bi(a), t !== null && l0(t), e.blockedOn = a, !1;
      t.shift();
    }
    return !0;
  }
  function c0(e, t, a) {
    qr(e) && a.delete(t);
  }
  function wb() {
    bc = !1, mn !== null && qr(mn) && (mn = null), vn !== null && qr(vn) && (vn = null), gn !== null && qr(gn) && (gn = null), ns.forEach(c0), is.forEach(c0);
  }
  function jr(e, t) {
    e.blockedOn === t && (e.blockedOn = null, bc || (bc = !0, n.unstable_scheduleCallback(
      n.unstable_NormalPriority,
      wb
    )));
  }
  var Rr = null;
  function f0(e) {
    Rr !== e && (Rr = e, n.unstable_scheduleCallback(
      n.unstable_NormalPriority,
      function() {
        Rr === e && (Rr = null);
        for (var t = 0; t < e.length; t += 3) {
          var a = e[t], l = e[t + 1], o = e[t + 2];
          if (typeof l != "function") {
            if (gc(l || a) === null)
              continue;
            break;
          }
          var u = bi(a);
          u !== null && (e.splice(t, 3), t -= 3, vu(
            u,
            {
              pending: !0,
              data: o,
              method: a.method,
              action: l
            },
            l,
            o
          ));
        }
      }
    ));
  }
  function Pi(e) {
    function t(w) {
      return jr(w, e);
    }
    mn !== null && jr(mn, e), vn !== null && jr(vn, e), gn !== null && jr(gn, e), ns.forEach(t), is.forEach(t);
    for (var a = 0; a < bn.length; a++) {
      var l = bn[a];
      l.blockedOn === e && (l.blockedOn = null);
    }
    for (; 0 < bn.length && (a = bn[0], a.blockedOn === null); )
      u0(a), a.blockedOn === null && bn.shift();
    if (a = (e.ownerDocument || e).$$reactFormReplay, a != null)
      for (l = 0; l < a.length; l += 3) {
        var o = a[l], u = a[l + 1], p = o[wt] || null;
        if (typeof u == "function")
          p || f0(a);
        else if (p) {
          var b = null;
          if (u && u.hasAttribute("formAction")) {
            if (o = u, p = u[wt] || null)
              b = p.formAction;
            else if (gc(o) !== null) continue;
          } else b = p.action;
          typeof b == "function" ? a[l + 1] = b : (a.splice(l, 3), l -= 3), f0(a);
        }
      }
  }
  function d0() {
    function e(u) {
      u.canIntercept && u.info === "react-transition" && u.intercept({
        handler: function() {
          return new Promise(function(p) {
            return o = p;
          });
        },
        focusReset: "manual",
        scroll: "manual"
      });
    }
    function t() {
      o !== null && (o(), o = null), l || setTimeout(a, 20);
    }
    function a() {
      if (!l && !navigation.transition) {
        var u = navigation.currentEntry;
        u && u.url != null && navigation.navigate(u.url, {
          state: u.getState(),
          info: "react-transition",
          history: "replace"
        });
      }
    }
    if (typeof navigation == "object") {
      var l = !1, o = null;
      return navigation.addEventListener("navigate", e), navigation.addEventListener("navigatesuccess", t), navigation.addEventListener("navigateerror", t), setTimeout(a, 100), function() {
        l = !0, navigation.removeEventListener("navigate", e), navigation.removeEventListener("navigatesuccess", t), navigation.removeEventListener("navigateerror", t), o !== null && (o(), o = null);
      };
    }
  }
  function yc(e) {
    this._internalRoot = e;
  }
  $r.prototype.render = yc.prototype.render = function(e) {
    var t = this._internalRoot;
    if (t === null) throw Error(r(409));
    var a = t.current, l = Ut();
    n0(a, l, e, t, null, null);
  }, $r.prototype.unmount = yc.prototype.unmount = function() {
    var e = this._internalRoot;
    if (e !== null) {
      this._internalRoot = null;
      var t = e.containerInfo;
      n0(e.current, 2, null, e, null, null), gr(), t[vi] = null;
    }
  };
  function $r(e) {
    this._internalRoot = e;
  }
  $r.prototype.unstable_scheduleHydration = function(e) {
    if (e) {
      var t = wf();
      e = { blockedOn: null, target: e, priority: t };
      for (var a = 0; a < bn.length && t !== 0 && t < bn[a].priority; a++) ;
      bn.splice(a, 0, e), a === 0 && u0(e);
    }
  };
  var p0 = i.version;
  if (p0 !== "19.2.5")
    throw Error(
      r(
        527,
        p0,
        "19.2.5"
      )
    );
  Q.findDOMNode = function(e) {
    var t = e._reactInternals;
    if (t === void 0)
      throw typeof e.render == "function" ? Error(r(188)) : (e = Object.keys(e).join(","), Error(r(268, e)));
    return e = v(t), e = e !== null ? y(e) : null, e = e === null ? null : e.stateNode, e;
  };
  var kb = {
    bundleType: 0,
    version: "19.2.5",
    rendererPackageName: "react-dom",
    currentDispatcherRef: R,
    reconcilerVersion: "19.2.5"
  };
  if (typeof __REACT_DEVTOOLS_GLOBAL_HOOK__ < "u") {
    var Ur = __REACT_DEVTOOLS_GLOBAL_HOOK__;
    if (!Ur.isDisabled && Ur.supportsFiber)
      try {
        Za = Ur.inject(
          kb
        ), tt = Ur;
      } catch {
      }
  }
  return ss.createRoot = function(e, t) {
    if (!c(e)) throw Error(r(299));
    var a = !1, l = "", o = _p, u = xp, p = wp;
    return t != null && (t.unstable_strictMode === !0 && (a = !0), t.identifierPrefix !== void 0 && (l = t.identifierPrefix), t.onUncaughtError !== void 0 && (o = t.onUncaughtError), t.onCaughtError !== void 0 && (u = t.onCaughtError), t.onRecoverableError !== void 0 && (p = t.onRecoverableError)), t = t0(
      e,
      1,
      !1,
      null,
      null,
      a,
      l,
      null,
      o,
      u,
      p,
      d0
    ), e[vi] = t.current, ec(e), new yc(t);
  }, ss.hydrateRoot = function(e, t, a) {
    if (!c(e)) throw Error(r(299));
    var l = !1, o = "", u = _p, p = xp, b = wp, w = null;
    return a != null && (a.unstable_strictMode === !0 && (l = !0), a.identifierPrefix !== void 0 && (o = a.identifierPrefix), a.onUncaughtError !== void 0 && (u = a.onUncaughtError), a.onCaughtError !== void 0 && (p = a.onCaughtError), a.onRecoverableError !== void 0 && (b = a.onRecoverableError), a.formState !== void 0 && (w = a.formState)), t = t0(
      e,
      1,
      !0,
      t,
      a ?? null,
      l,
      o,
      w,
      u,
      p,
      b,
      d0
    ), t.context = a0(null), a = t.current, l = Ut(), l = mi(l), o = an(l), o.callback = null, nn(a, o, l), a = l, t.current.lanes = a, G(t, a), ha(t), e[vi] = t.current, ec(e), new $r(t);
  }, ss.version = "19.2.5", ss;
}
var w0;
function Mb() {
  if (w0) return wc.exports;
  w0 = 1;
  function n() {
    if (!(typeof __REACT_DEVTOOLS_GLOBAL_HOOK__ > "u" || typeof __REACT_DEVTOOLS_GLOBAL_HOOK__.checkDCE != "function"))
      try {
        __REACT_DEVTOOLS_GLOBAL_HOOK__.checkDCE(n);
      } catch (i) {
        console.error(i);
      }
  }
  return n(), wc.exports = Nb(), wc.exports;
}
var qb = Mb(), Tc = { exports: {} }, rs = {};
/**
 * @license React
 * react-jsx-runtime.production.js
 *
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 */
var k0;
function jb() {
  if (k0) return rs;
  k0 = 1;
  var n = Symbol.for("react.transitional.element"), i = Symbol.for("react.fragment");
  function s(r, c, f) {
    var d = null;
    if (f !== void 0 && (d = "" + f), c.key !== void 0 && (d = "" + c.key), "key" in c) {
      f = {};
      for (var h in c)
        h !== "key" && (f[h] = c[h]);
    } else f = c;
    return c = f.ref, {
      $$typeof: n,
      type: r,
      key: d,
      ref: c !== void 0 ? c : null,
      props: f
    };
  }
  return rs.Fragment = i, rs.jsx = s, rs.jsxs = s, rs;
}
var S0;
function Rb() {
  return S0 || (S0 = 1, Tc.exports = jb()), Tc.exports;
}
var N = Rb();
function $b(n) {
  return typeof n.questionId == "string";
}
function Ub(n) {
  const i = n;
  return Array.isArray(i.all) || Array.isArray(i.any);
}
function Lb(n) {
  return typeof n.expression == "string";
}
class va extends Error {
  constructor(s, r) {
    super(`Expression syntax error at column ${r}: ${s}`);
    La(this, "position");
    this.position = r, this.name = "ExpressionSyntaxError";
  }
}
function Yr(n) {
  return n >= "0" && n <= "9";
}
function xm(n) {
  return n >= "a" && n <= "z" || n >= "A" && n <= "Z";
}
function Hb(n) {
  return xm(n) || Yr(n);
}
function Yb(n) {
  return n === " " || n === "	" || n === `
` || n === "\r" || n === "\f" || n === "\v";
}
function Bb(n) {
  const i = [];
  let s = 0;
  const r = () => s >= n.length, c = (m = 0) => n.charAt(s + m), f = (m) => {
    if (s + m.length > n.length)
      return !1;
    for (let _ = 0; _ < m.length; _++)
      if (n.charAt(s + _) !== m.charAt(_))
        return !1;
    return s += m.length, !0;
  }, d = () => {
    for (; !r() && Yb(c()); )
      s++;
  }, h = (m) => {
    for (; !r() && Yr(c()); )
      s++;
    if (!r() && c() === ".")
      for (s++; !r() && Yr(c()); )
        s++;
    const _ = n.substring(m, s), x = parseFloat(_);
    return { kind: "Number", text: _, literal: x, position: m };
  }, g = (m, _) => {
    s++;
    let x = "";
    for (; !r() && c() !== _; ) {
      const T = c();
      if (T === "\\" && s + 1 < n.length) {
        const A = c(1), q = {
          n: `
`,
          t: "	",
          r: "\r",
          "\\": "\\",
          "'": "'",
          '"': '"'
        }[A];
        if (q === void 0)
          throw new va(`unknown escape '\\${A}'.`, s);
        x += q, s += 2;
      } else
        x += T, s++;
    }
    if (r())
      throw new va("unterminated string literal.", m);
    return s++, { kind: "String", text: x, literal: x, position: m };
  }, v = (m) => {
    for (; !r(); ) {
      const x = c();
      if (x === "_" || x === "-" || Hb(x))
        s++;
      else
        break;
    }
    const _ = n.substring(m, s);
    return _ === "true" ? { kind: "True", text: _, literal: !0, position: m } : _ === "false" ? { kind: "False", text: _, literal: !1, position: m } : _ === "null" ? { kind: "Null", text: _, literal: null, position: m } : { kind: "Identifier", text: _, literal: null, position: m };
  }, y = () => {
    const m = s, _ = c();
    if (Yr(_))
      return h(m);
    if (_ === "'" || _ === '"')
      return g(m, _);
    if (_ === "_" || xm(_))
      return v(m);
    switch (_) {
      case "(":
        return s++, { kind: "LParen", text: "(", literal: null, position: m };
      case ")":
        return s++, { kind: "RParen", text: ")", literal: null, position: m };
      case "[":
        return s++, { kind: "LBracket", text: "[", literal: null, position: m };
      case "]":
        return s++, { kind: "RBracket", text: "]", literal: null, position: m };
      case ",":
        return s++, { kind: "Comma", text: ",", literal: null, position: m };
      case ".":
        return s++, { kind: "Dot", text: ".", literal: null, position: m };
      case "=":
        if (f("==="))
          return { kind: "StrictEq", text: "===", literal: null, position: m };
        if (f("=="))
          return { kind: "Eq", text: "==", literal: null, position: m };
        throw new va("bare '=' is not a valid operator (use '==' or '===').", m);
      case "!":
        return f("!==") ? { kind: "StrictNotEq", text: "!==", literal: null, position: m } : f("!=") ? { kind: "NotEq", text: "!=", literal: null, position: m } : (s++, { kind: "Not", text: "!", literal: null, position: m });
      case "<":
        return f("<=") ? { kind: "LtEq", text: "<=", literal: null, position: m } : (s++, { kind: "Lt", text: "<", literal: null, position: m });
      case ">":
        return f(">=") ? { kind: "GtEq", text: ">=", literal: null, position: m } : (s++, { kind: "Gt", text: ">", literal: null, position: m });
      case "&":
        if (f("&&"))
          return { kind: "And", text: "&&", literal: null, position: m };
        throw new va("expected '&&'.", m);
      case "|":
        if (f("||"))
          return { kind: "Or", text: "||", literal: null, position: m };
        throw new va("expected '||'.", m);
    }
    throw new va(`unexpected character '${_}'.`, m);
  };
  for (; ; ) {
    if (d(), r())
      return i.push({ kind: "EndOfInput", text: "", literal: null, position: s }), i;
    i.push(y());
  }
}
function Vb(n) {
  let i = 0;
  const s = () => {
    const z = n[i];
    if (!z)
      throw new va("unexpected end of tokens.", 0);
    return z;
  }, r = () => {
    const z = s();
    return z.kind !== "EndOfInput" && i++, z;
  }, c = (z) => s().kind !== z ? !1 : (r(), !0), f = (z) => {
    const q = s();
    if (q.kind !== z)
      throw new va(`expected ${z}, got '${q.text}'.`, q.position);
    return r(), q;
  }, d = () => {
    let z = h();
    for (; c("Or"); )
      z = { kind: "BinaryOp", op: "||", left: z, right: h() };
    return z;
  }, h = () => {
    let z = g();
    for (; c("And"); )
      z = { kind: "BinaryOp", op: "&&", left: z, right: g() };
    return z;
  }, g = () => {
    let z = v();
    for (; ; ) {
      const q = s().kind;
      let $ = null;
      if (q === "Eq" || q === "StrictEq" ? $ = "==" : (q === "NotEq" || q === "StrictNotEq") && ($ = "!="), $ === null)
        break;
      r(), z = { kind: "BinaryOp", op: $, left: z, right: v() };
    }
    return z;
  }, v = () => {
    let z = y();
    for (; ; ) {
      const q = s().kind;
      let $ = null;
      if (q === "Lt" ? $ = "<" : q === "Gt" ? $ = ">" : q === "LtEq" ? $ = "<=" : q === "GtEq" && ($ = ">="), $ === null)
        break;
      r(), z = { kind: "BinaryOp", op: $, left: z, right: y() };
    }
    return z;
  }, y = () => c("Not") ? { kind: "UnaryOp", op: "!", operand: y() } : T(), m = () => {
    f("LBracket");
    const z = [];
    if (s().kind !== "RBracket")
      for (z.push(d()); c("Comma"); )
        z.push(d());
    return f("RBracket"), { kind: "Array", items: z };
  }, _ = (z) => {
    let q;
    if (c("Dot"))
      q = f("Identifier").text;
    else if (c("LBracket")) {
      const $ = f("String");
      f("RBracket"), q = $.literal;
    } else
      throw new va("'answers' must be followed by .key or ['key'].", z);
    return { kind: "AnswersAccess", key: q };
  }, x = () => {
    const z = r();
    if (z.text === "answers")
      return _(z.position);
    f("LParen");
    const q = [];
    if (s().kind !== "RParen")
      for (q.push(d()); c("Comma"); )
        q.push(d());
    return f("RParen"), { kind: "Call", name: z.text, args: q };
  }, T = () => {
    const z = s();
    switch (z.kind) {
      case "Number":
      case "String":
      case "True":
      case "False":
      case "Null":
        return r(), { kind: "Literal", value: z.literal };
      case "LParen": {
        r();
        const q = d();
        return f("RParen"), q;
      }
      case "LBracket":
        return m();
      case "Identifier":
        return x();
      default:
        throw new va(`unexpected token '${z.text}'.`, z.position);
    }
  }, A = d();
  return f("EndOfInput"), A;
}
function Ga(n) {
  return n === void 0 || n === null ? null : typeof n == "boolean" || typeof n == "number" || typeof n == "string" ? n : Array.isArray(n) ? n.map(Ga) : null;
}
function si(n, i) {
  const s = Ga(n), r = Ga(i);
  if (s === null || r === null)
    return s === null && r === null;
  if (typeof s == "number" && typeof r == "number" || typeof s == "string" && typeof r == "string" || typeof s == "boolean" && typeof r == "boolean")
    return s === r;
  if (Array.isArray(s) && Array.isArray(r)) {
    if (s.length !== r.length)
      return !1;
    for (let c = 0; c < s.length; c++)
      if (!si(s[c], r[c]))
        return !1;
    return !0;
  }
  return !1;
}
function Sn(n, i) {
  const s = Ga(n), r = Ga(i);
  if (typeof s == "number" && typeof r == "number" || typeof s == "string" && typeof r == "string")
    return s < r ? -1 : s > r ? 1 : 0;
  throw new Error("Comparison operators require two numbers or two strings.");
}
function sl(n) {
  const i = Ga(n);
  return i === null ? !1 : typeof i == "boolean" ? i : typeof i == "number" ? i !== 0 : typeof i == "string" || Array.isArray(i) ? i.length > 0 : !0;
}
function Wt(n, i) {
  switch (n.kind) {
    case "Literal":
      return n.value;
    case "AnswersAccess":
      return Kb(n.key, i);
    case "UnaryOp":
      return Xb(n, i);
    case "BinaryOp":
      return Gb(n, i);
    case "Call":
      return Qb(n, i);
    case "Array":
      return n.items.map((s) => Wt(s, i));
  }
}
function Xb(n, i) {
  const s = Wt(n.operand, i);
  if (n.op === "!")
    return !sl(s);
  throw new Error(`Unknown unary operator '${n.op}'.`);
}
function Gb(n, i) {
  if (n.op === "&&") {
    const c = Wt(n.left, i);
    return sl(c) ? sl(Wt(n.right, i)) : !1;
  }
  if (n.op === "||") {
    const c = Wt(n.left, i);
    return sl(c) ? !0 : sl(Wt(n.right, i));
  }
  const s = Wt(n.left, i), r = Wt(n.right, i);
  switch (n.op) {
    case "==":
      return si(s, r);
    case "!=":
      return !si(s, r);
    case "<":
      return Sn(s, r) < 0;
    case ">":
      return Sn(s, r) > 0;
    case "<=":
      return Sn(s, r) <= 0;
    case ">=":
      return Sn(s, r) >= 0;
    default:
      throw new Error(`Unknown binary operator '${n.op}'.`);
  }
}
function Qb(n, i) {
  switch (n.name) {
    case "has":
    case "isSet":
      return z0(n, i);
    case "isNotSet":
      return !z0(n, i);
    case "in":
      return Zb(n, i);
    default:
      throw new Error(`Unknown function '${n.name}'.`);
  }
}
function z0(n, i) {
  if (n.args.length !== 1)
    throw new Error(`${n.name}() takes one argument.`);
  const s = n.args[0];
  if (!s)
    return !1;
  const r = Wt(s, i);
  return typeof r != "string" ? !1 : r in i && i[r] !== null && i[r] !== void 0;
}
function Zb(n, i) {
  if (n.args.length !== 2)
    throw new Error("in() takes two arguments: in(value, [array]).");
  const s = n.args[0], r = n.args[1];
  if (!s || !r)
    return !1;
  const c = Wt(s, i), f = Wt(r, i);
  return Array.isArray(f) ? f.some((d) => si(c, d)) : !1;
}
function Kb(n, i) {
  return n in i ? Ga(i[n]) : null;
}
function Jb(n) {
  const i = Bb(n);
  return Vb(i);
}
function Fb(n, i) {
  try {
    const s = typeof n == "string" ? Jb(n) : n;
    return sl(Wt(s, i));
  } catch {
    return !1;
  }
}
function Wb(n, i) {
  var s;
  if (!n.logic)
    return null;
  for (const r of n.logic)
    if (Uc(r.if, i))
      return ((s = r.then) == null ? void 0 : s.goto) ?? null;
  return null;
}
function Uc(n, i) {
  try {
    return $b(n) ? Pb(n, i) : Ub(n) ? Ib(n, i) : Lb(n) ? Fb(n.expression, i) : !1;
  } catch {
    return !1;
  }
}
function Ib(n, i) {
  return n.all && n.all.length > 0 ? n.all.every((s) => Uc(s, i)) : n.any && n.any.length > 0 ? n.any.some((s) => Uc(s, i)) : !1;
}
function Pb(n, i) {
  const s = n.questionId in i && i[n.questionId] !== null && i[n.questionId] !== void 0;
  if (n.op === "isSet")
    return s;
  if (n.op === "isNotSet")
    return !s;
  if (n.value === void 0)
    return !1;
  const r = s ? Ga(i[n.questionId]) : null, c = Ga(n.value);
  return ey(n.op, r, c);
}
function ey(n, i, s) {
  switch (n) {
    case "==":
      return si(i, s);
    case "!=":
      return !si(i, s);
    case ">":
      return Sn(i, s) > 0;
    case ">=":
      return Sn(i, s) >= 0;
    case "<":
      return Sn(i, s) < 0;
    case "<=":
      return Sn(i, s) <= 0;
    case "in":
      return T0(s, i);
    case "notIn":
      return !T0(s, i);
    default:
      return !1;
  }
}
function T0(n, i) {
  return Array.isArray(n) ? n.some((s) => si(i, s)) : !1;
}
function ol(n, i, s) {
  const r = new Set(n.screens.map((h) => h.id)), c = n.screens.find((h) => h.id === i);
  if (c && (!c.questions || c.questions.length === 0) && !c.nextScreen)
    return { kind: "end" };
  const f = Wb(n, s);
  if (f && f !== i && r.has(f))
    return { kind: "screen", screenId: f };
  if (c != null && c.nextScreen && c.nextScreen !== i && r.has(c.nextScreen))
    return { kind: "screen", screenId: c.nextScreen };
  const d = n.screens.findIndex((h) => h.id === i);
  if (d >= 0 && d + 1 < n.screens.length) {
    const h = n.screens[d + 1];
    if (h)
      return { kind: "screen", screenId: h.id };
  }
  return { kind: "end" };
}
function ty(n, i, s, r) {
  const c = new Set(i.screens.map((f) => f.id));
  return n.nextScreen && c.has(n.nextScreen) ? { kind: "screen", screenId: n.nextScreen } : ol(i, s, r);
}
function ay(n, i, s) {
  var d;
  const r = [], c = /* @__PURE__ */ new Set();
  let f = (d = n.screens[0]) == null ? void 0 : d.id;
  for (; f !== void 0 && !c.has(f); ) {
    if (f === s)
      return r;
    c.add(f), r.push(f);
    const h = ny(n, f, i);
    if (h.kind === "end")
      return null;
    f = h.screenId;
  }
  return null;
}
function ny(n, i, s) {
  var f;
  const r = new Set(n.screens.map((d) => d.id)), c = n.screens.find((d) => d.id === i);
  for (const d of (c == null ? void 0 : c.questions) ?? []) {
    if (d.type !== "navigationList")
      continue;
    const h = s[d.id];
    if (typeof h != "string")
      continue;
    const v = (d.options ?? []).find((m) => m.id === h);
    if (v != null && v.nextScreen && r.has(v.nextScreen))
      return { kind: "screen", screenId: v.nextScreen };
    const y = (f = d.optionsSource) == null ? void 0 : f.nextScreen;
    if (!v && y && r.has(y))
      return { kind: "screen", screenId: y };
  }
  return ol(n, i, s);
}
const be = (n, i, s, r) => ({ questionId: n, code: i, message: s, ...r ? { params: r } : {} }), Ot = (n) => typeof n == "number" && Number.isFinite(n);
function Ec(n) {
  if (!/^\d{4}-\d{2}-\d{2}$/.test(n))
    return null;
  const [i, s, r] = n.split("-").map((f) => Number.parseInt(f, 10)), c = new Date(Date.UTC(i, s - 1, r));
  return c.getUTCFullYear() !== i || c.getUTCMonth() !== s - 1 || c.getUTCDate() !== r ? null : c.getTime();
}
function iy(n) {
  const i = n.match(/^(\d{4})-(\d{2})-(\d{2})T(\d{2}):(\d{2})$/);
  if (!i)
    return !1;
  const [s = 0, r = 0, c = 0, f = 0, d = 0] = i.slice(1).map(Number), h = new Date(Date.UTC(s, r - 1, c));
  return h.getUTCMonth() === r - 1 && h.getUTCDate() === c && f < 24 && d < 60;
}
function Ac(n) {
  const i = Date.parse(n);
  return Number.isNaN(i) ? null : i;
}
function wm(n, i) {
  const s = n.id, r = [];
  switch (n.type) {
    case "text": {
      if (typeof i != "string") {
        r.push(be(s, "type", "Text answer must be a JSON string."));
        break;
      }
      const c = n.minLength, f = n.maxLength, d = n.pattern;
      if (Ot(c) && i.length < c && r.push(be(s, "minLength", `Answer length ${i.length} is less than minLength ${c}.`, { n: c, actual: i.length })), Ot(f) && i.length > f && r.push(be(s, "maxLength", `Answer length ${i.length} exceeds maxLength ${f}.`, { n: f, actual: i.length })), typeof d == "string" && d.length > 0)
        try {
          new RegExp(d).test(i) || r.push(be(s, "pattern", "Answer does not match the required pattern."));
        } catch {
        }
      break;
    }
    case "paragraph": {
      if (typeof i != "string") {
        r.push(be(s, "type", "Paragraph answer must be a JSON string."));
        break;
      }
      const c = n.minLength, f = n.maxLength;
      Ot(c) && i.length < c && r.push(be(s, "minLength", `Answer length ${i.length} is less than minLength ${c}.`, { n: c, actual: i.length })), Ot(f) && i.length > f && r.push(be(s, "maxLength", `Answer length ${i.length} exceeds maxLength ${f}.`, { n: f, actual: i.length }));
      break;
    }
    case "number": {
      if (!Ot(i)) {
        r.push(be(s, "type", "Number answer must be a JSON number."));
        break;
      }
      const c = n.min, f = n.max;
      Ot(c) && i < c && r.push(be(s, "min", `Answer ${i} is less than min ${c}.`, { n: c })), Ot(f) && i > f && r.push(be(s, "max", `Answer ${i} exceeds max ${f}.`, { n: f }));
      break;
    }
    case "rating": {
      if (!Ot(i)) {
        r.push(be(s, "type", "Rating answer must be a JSON number."));
        break;
      }
      const c = Ot(n.max) ? n.max : 0;
      (i < 0 || i > c) && r.push(be(s, "range", `Rating ${i} is outside 0..${c}.`, { min: 0, max: c })), n.allowHalf !== !0 && i !== Math.floor(i) && r.push(be(s, "halfNotAllowed", "Rating does not allow half values."));
      break;
    }
    case "nps": {
      if (!Ot(i) || !Number.isInteger(i)) {
        r.push(be(s, "type", "NPS answer must be a JSON number."));
        break;
      }
      const c = Ot(n.min) ? n.min : 0, f = Ot(n.max) ? n.max : 10;
      (i < c || i > f) && r.push(be(s, "range", `NPS answer ${i} is outside ${c}..${f}.`, { min: c, max: f }));
      break;
    }
    case "singleChoice":
    case "dropdown":
    case "navigationList": {
      if (typeof i != "string") {
        r.push(be(s, "type", "Choice answer must be a JSON string (option id)."));
        break;
      }
      if (n.optionsSource != null)
        break;
      (Array.isArray(n.options) ? n.options : []).some((f) => f.id === i) || r.push(be(s, "invalidOption", `'${i}' is not a valid option id for this question.`, { option: i }));
      break;
    }
    case "multiChoice": {
      if (!Array.isArray(i)) {
        r.push(be(s, "type", "MultiChoice answer must be a JSON array of option ids."));
        break;
      }
      const c = Array.isArray(n.options) ? n.options : [], f = new Set(c.map((y) => y.id)), d = [];
      let h = !1;
      for (const y of i) {
        if (typeof y != "string") {
          r.push(be(s, "type", "Each MultiChoice array entry must be a string option id.")), h = !0;
          break;
        }
        d.push(y);
      }
      if (h)
        break;
      if (n.optionsSource == null)
        for (const y of d)
          f.has(y) || r.push(be(s, "invalidOption", `'${y}' is not a valid option id for this question.`, { option: y }));
      const g = n.minSelected, v = n.maxSelected;
      Ot(g) && d.length < g && r.push(be(s, "minSelected", `At least ${g} option(s) must be selected.`, { n: g })), Ot(v) && d.length > v && r.push(be(s, "maxSelected", `At most ${v} option(s) may be selected.`, { n: v }));
      break;
    }
    case "date": {
      if (typeof i != "string") {
        r.push(be(s, "type", "Date answer must be a JSON string in yyyy-MM-dd format."));
        break;
      }
      const c = Ec(i);
      if (c === null) {
        r.push(be(s, "invalidDate", `Date '${i}' is not yyyy-MM-dd.`));
        break;
      }
      const f = n.minDate, d = n.maxDate;
      if (typeof f == "string") {
        const h = Ec(f);
        h !== null && c < h && r.push(be(s, "minDate", `Date ${i} is before minDate ${f}.`, { min: f }));
      }
      if (typeof d == "string") {
        const h = Ec(d);
        h !== null && c > h && r.push(be(s, "maxDate", `Date ${i} is after maxDate ${d}.`, { max: d }));
      }
      break;
    }
    case "dateTime": {
      if (typeof i != "string") {
        r.push(be(s, "type", "DateTime answer must be a JSON string in ISO 8601 format."));
        break;
      }
      const c = Ac(i);
      if (c === null) {
        r.push(be(s, "invalidDateTime", `DateTime '${i}' is not valid ISO 8601.`));
        break;
      }
      const f = n.minDateTime, d = n.maxDateTime;
      if (typeof f == "string" && f.length > 0) {
        const h = Ac(f);
        h !== null && c < h && r.push(be(s, "minDateTime", `DateTime is before minDateTime ${f}.`, { min: f }));
      }
      if (typeof d == "string" && d.length > 0) {
        const h = Ac(d);
        h !== null && c > h && r.push(be(s, "maxDateTime", `DateTime is after maxDateTime ${d}.`, { max: d }));
      }
      break;
    }
    case "file": {
      (typeof i != "string" || i.length === 0) && r.push(be(s, "empty", "Answer must be a non-empty file reference string."));
      break;
    }
    case "signature": {
      (typeof i != "string" || i.length === 0) && r.push(be(s, "empty", "Answer must be a non-empty signature data url string."));
      break;
    }
    case "yesNo": {
      typeof i != "boolean" && r.push(be(s, "type", "Yes/No answer must be a JSON boolean."));
      break;
    }
    case "bookingSlot": {
      (typeof i != "string" || !iy(i)) && r.push(be(s, "invalidBookingSlot", "Booking slot answer must be a JSON string in yyyy-MM-ddTHH:mm format."));
      break;
    }
  }
  return r;
}
function ly(n, i) {
  const s = [];
  for (const r of n ?? []) {
    const c = r, f = c.id;
    if (typeof f != "string")
      continue;
    const d = i[f];
    d != null && s.push(...wm(c, d));
  }
  return s;
}
const sy = /\{\{\s*([A-Za-z_][A-Za-z0-9_.\-]*)\s*(?:\|([^}]*))?\}\}/g, Lc = "answers.", Hc = ".label";
function ry(n) {
  return typeof n == "string" && n.includes("{{");
}
function oy(n) {
  return typeof n == "string" && n.startsWith(Lc) && n.length > Lc.length;
}
function uy(n) {
  if (!oy(n))
    return null;
  let i = n.slice(Lc.length);
  return i.endsWith(Hc) && (i = i.slice(0, -Hc.length)), i.length === 0 ? null : i;
}
function cy(n, i) {
  switch (i) {
    case "url":
    case "formBody":
      return encodeURIComponent(n);
    case "jsonBody":
      return JSON.stringify(n).slice(1, -1);
    case "headerValue":
      return n.replace(/[\r\n]/g, "");
    default:
      return n;
  }
}
function fy(n) {
  const i = (n ?? "").split(";")[0].trim().toLowerCase();
  return i === "application/json" || i.endsWith("+json") ? "jsonBody" : i === "application/x-www-form-urlencoded" ? "formBody" : "rawBody";
}
function ps(n, i, s, r = "copy") {
  return ry(n) ? n.replace(sy, (c, f, d) => {
    const h = d == null ? void 0 : d.trim(), g = i.value(f, s) ?? (h || null) ?? i.fallback(f, s);
    return g === null ? r === "copy" ? c : "" : cy(g, r);
  }) : n;
}
function sf(n, i, s) {
  if (n == null)
    return null;
  if (typeof n == "string")
    return n || null;
  const r = n[i];
  if (r)
    return r;
  if (s && n[s])
    return n[s];
  for (const c of Object.keys(n))
    if (n[c])
      return n[c];
  return null;
}
function dy(n, i) {
  for (const s of n.screens)
    for (const r of s.questions ?? [])
      if (r && r.id === i)
        return r;
}
function rf(n) {
  if (n == null)
    return null;
  if (typeof n == "string")
    return n.length === 0 ? null : n;
  if (typeof n == "number" || typeof n == "boolean")
    return String(n);
  if (Array.isArray(n)) {
    const i = n.map(rf).filter((s) => s !== null);
    return i.length === 0 ? null : i.join(", ");
  }
  if (typeof n == "object") {
    const i = n.name;
    return typeof i == "string" && i.length > 0 ? i : JSON.stringify(n);
  }
  return String(n);
}
function E0(n, i, s, r) {
  const c = n == null ? void 0 : n.find((f) => f.id === i);
  return c ? sf(c.label, s, r) : null;
}
function py(n, i, s, r) {
  var h, g;
  const c = rf(i);
  if (c === null || !n)
    return c;
  const f = r.schema.defaultLocale, d = (v) => {
    var y;
    return E0(n.options, v, s, f) ?? E0(n.id ? (y = r.lookupOptions) == null ? void 0 : y.call(r, n.id) : void 0, v, s, f) ?? v;
  };
  switch (n.type) {
    case "singleChoice":
    case "dropdown":
    case "navigationList":
      return d(String(i));
    case "multiChoice":
      return Array.isArray(i) ? i.map((v) => d(String(v))).join(", ") : d(String(i));
    case "yesNo": {
      const v = i === !0 || i === "true";
      return sf(n[v ? "yesLabel" : "noLabel"], s, f) ?? (v ? ((h = r.yesNoLabels) == null ? void 0 : h.yes) ?? "Yes" : ((g = r.yesNoLabels) == null ? void 0 : g.no) ?? "No");
    }
    case "date":
    case "dateTime":
    case "bookingSlot": {
      const v = new Date(c);
      if (Number.isNaN(v.getTime()))
        return c;
      try {
        return n.type === "date" ? new Intl.DateTimeFormat(s, { dateStyle: "medium" }).format(v) : new Intl.DateTimeFormat(s, { dateStyle: "medium", timeStyle: "short" }).format(v);
      } catch {
        return c;
      }
    }
    default:
      return c;
  }
}
function hy(n) {
  const { schema: i, answers: s } = n;
  return {
    value(r, c) {
      const f = uy(r);
      if (f === null)
        return null;
      const d = s[f];
      return r.endsWith(Hc) ? py(dy(i, f), d, c, n) : rf(d);
    },
    fallback(r, c) {
      var d;
      const f = (d = i.variables) == null ? void 0 : d.find((h) => {
        var g;
        return ((g = h.name) == null ? void 0 : g.trim()) === r;
      });
      return sf(f == null ? void 0 : f.fallback, c, i.defaultLocale);
    }
  };
}
const my = [
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
function vy(n) {
  return n === null || typeof n != "object" || Array.isArray(n) ? !1 : Object.values(n).every((i) => typeof i == "string");
}
function gy(n, i) {
  let s = !1;
  const r = {};
  for (const [c, f] of Object.entries(n)) {
    const d = ps(f, i, c, "copy");
    d !== f && (s = !0), r[c] = d;
  }
  return s ? r : n;
}
function Yc(n, i) {
  let s = null;
  for (const r of my) {
    const c = n[r];
    if (!vy(c))
      continue;
    const f = gy(c, i);
    f !== c && (s ?? (s = { ...n }), s[r] = f);
  }
  return s ?? n;
}
function by(n, i) {
  let s = Yc(n, i);
  const r = n.options;
  if (Array.isArray(r)) {
    let c = !1;
    const f = r.map((d) => {
      if (d === null || typeof d != "object")
        return d;
      const h = Yc(d, i);
      return h !== d && (c = !0), h;
    });
    c && (s = { ...s, options: f });
  }
  return s;
}
function yy(n, i) {
  let s = Yc(n, i);
  const r = n.questions;
  if (Array.isArray(r)) {
    let c = !1;
    const f = r.map((d) => {
      if (d === null || typeof d != "object")
        return d;
      const h = by(d, i);
      return h !== d && (c = !0), h;
    });
    c && (s = { ...s, questions: f });
  }
  return s;
}
function _y(n, i, s) {
  let r = null;
  const c = (g, v) => {
    r ?? (r = { ...n }), r[g] = v;
  }, f = ps(n.url, i, s, "url");
  if (f !== n.url && c("url", f), n.body != null) {
    const g = ps(n.body, i, s, fy(km(n)));
    g !== n.body && c("body", g);
  }
  const d = A0(n.queryParams, i, s, "queryValue");
  d !== n.queryParams && c("queryParams", d);
  const h = A0(n.headers, i, s, "headerValue");
  return h !== n.headers && c("headers", h), r ?? n;
}
function km(n) {
  var s;
  const i = (s = n.contentType) == null ? void 0 : s.trim();
  return i || (n.body != null ? "application/json" : void 0);
}
function A0(n, i, s, r) {
  if (!n)
    return n;
  let c = null;
  for (const [f, d] of Object.entries(n)) {
    const h = ps(d, i, s, r);
    h !== d && (c ?? (c = { ...n }), c[f] = h);
  }
  return c ?? n;
}
function Oc(n, i) {
  let s = n;
  for (const r of i.split(".")) {
    if (s === null || typeof s != "object")
      return;
    s = s[r];
  }
  return s;
}
function Sm(n) {
  const i = new URL(n.url);
  for (const [s, r] of Object.entries(n.queryParams ?? {}))
    i.searchParams.set(s, r);
  return i.toString();
}
function xy(n, i) {
  const s = i.itemsPath ? Oc(n, i.itemsPath) : n;
  if (!Array.isArray(s))
    throw new Error(`optionsSource response is not an array${i.itemsPath ? ` at '${i.itemsPath}'` : ""}.`);
  const r = i.valuePath || "ID", c = i.labelPath || "Name", f = [];
  for (const d of s) {
    const h = Oc(d, r);
    if (h == null || h === "")
      continue;
    const g = Oc(d, c);
    f.push({
      id: String(h),
      label: g == null || g === "" ? String(h) : String(g)
    });
  }
  return f;
}
function Zr(n) {
  var i;
  return ((i = n.method) == null ? void 0 : i.trim().toUpperCase()) === "POST" ? "POST" : "GET";
}
function zm(n, i) {
  const s = {};
  i && (s["Accept-Language"] = i);
  const r = Zr(n) === "POST" && n.body != null ? km(n) : void 0;
  r && (s["Content-Type"] = r);
  for (const [c, f] of Object.entries(n.headers ?? {})) {
    for (const d of Object.keys(s))
      d.toLowerCase() === c.toLowerCase() && delete s[d];
    s[c] = f;
  }
  return s;
}
function wy(n, i) {
  const s = zm(n, i), r = Object.keys(s).sort().map((f) => `${f}=${s[f]}`).join(`
`), c = Zr(n) === "POST" && n.body != null ? n.body : "";
  return `${Zr(n)} ${Sm(n)}
${r}
${c}`;
}
async function ky(n, i) {
  const s = (i == null ? void 0 : i.fetchImpl) ?? fetch, r = Zr(n), c = await s(Sm(n), {
    method: r,
    headers: zm(n, i == null ? void 0 : i.locale),
    ...r === "POST" && n.body != null ? { body: n.body } : {},
    ...i != null && i.signal ? { signal: i.signal } : {}
  });
  if (!c.ok)
    throw new Error(`optionsSource fetch failed: HTTP ${c.status}.`);
  return xy(await c.json(), n);
}
class el extends Error {
  constructor(s) {
    super(s.message);
    La(this, "status");
    La(this, "code");
    La(this, "serverMessage");
    La(this, "validationErrors");
    La(this, "raw");
    this.name = "SurveyClientError", this.status = s.status, this.code = s.code, this.serverMessage = s.serverMessage, this.validationErrors = s.validationErrors, this.raw = s.raw;
  }
}
class O0 {
  constructor(i) {
    La(this, "baseUrl");
    La(this, "fetchFn");
    this.baseUrl = i.baseUrl.replace(/\/+$/, "");
    const s = i.fetch ?? globalThis.fetch;
    if (!s)
      throw new Error("SurveyClient: no fetch available. Provide options.fetch or run in an environment with a global fetch.");
    this.fetchFn = s.bind(globalThis);
  }
  async fetchSchema(i) {
    const s = await this.send("GET", `/SurveyInstances/${encodeURIComponent(i)}/schema`);
    return this.readJson(s);
  }
  async getStatus(i) {
    const s = await this.send("GET", `/SurveyInstances/${encodeURIComponent(i)}/status`), r = await this.readJson(s);
    return {
      status: String(r.Status ?? r.status ?? "Pending"),
      schemaVersion: Number(r.SchemaVersion ?? r.schemaVersion ?? 0),
      triggeredAt: r.TriggeredAt ?? r.triggeredAt
    };
  }
  async submitResponse(i, s) {
    await this.send("POST", `/SurveyInstances/${encodeURIComponent(i)}/responses`, s);
  }
  async send(i, s, r) {
    let c;
    try {
      c = await this.fetchFn(`${this.baseUrl}${s}`, {
        method: i,
        headers: r === void 0 ? void 0 : { "Content-Type": "application/json" },
        body: r === void 0 ? void 0 : JSON.stringify(r)
      });
    } catch (f) {
      throw new el({
        status: 0,
        code: "network",
        message: `Network error calling ${i} ${s}: ${f.message ?? f}`
      });
    }
    if (!c.ok)
      throw await this.toError(c, i, s);
    return c;
  }
  async readJson(i) {
    const s = await i.text();
    if (!s)
      throw new el({
        status: i.status,
        code: "parse",
        message: `Empty body from ${i.url}`
      });
    try {
      return JSON.parse(s);
    } catch (r) {
      throw new el({
        status: i.status,
        code: "parse",
        message: `Could not parse JSON from ${i.url}: ${r.message}`,
        raw: s
      });
    }
  }
  async toError(i, s, r) {
    const c = i.status === 404 ? "notFound" : i.status === 410 ? "gone" : i.status === 409 ? "conflict" : i.status === 400 ? "badRequest" : (i.status >= 500, "server"), f = await i.text();
    if (!f)
      return new el({
        status: i.status,
        code: c,
        message: `${s} ${r} → ${i.status}`
      });
    let d;
    try {
      d = JSON.parse(f);
    } catch {
      return new el({
        status: i.status,
        code: c,
        message: `${s} ${r} → ${i.status}: ${f.slice(0, 200)}`,
        raw: f
      });
    }
    const h = d.Message ?? d.message, g = d.Errors ?? d.errors, v = Array.isArray(g) ? g.flatMap((y) => {
      const m = y.QuestionId ?? y.questionId, _ = y.Message ?? y.message;
      return m && _ ? [{ questionId: m, message: _ }] : [];
    }) : void 0;
    return new el({
      status: i.status,
      code: c,
      message: `${s} ${r} → ${i.status}${h ? ": " + h : ""}`,
      serverMessage: h,
      validationErrors: v && v.length > 0 ? v : void 0,
      raw: d
    });
  }
}
/*!
 * Built by ShiftSoftware
 * Copyright (c)
 */
(async function() {
  const n = "https://fonts.googleapis.com/css2?family=Manrope:wght@200..800&family=Noto+Kufi+Arabic:wght@100..900&family=Nunito:ital,wght@0,200..1000;1,200..1000&display=swap";
  if (window.adpWebComponentsFonts !== !1 && !document.querySelector(`link[href="${n}"]`)) {
    const i = document.createElement("link");
    i.rel = "stylesheet", i.href = n, document.head.appendChild(i), console.log("✅ Manrope, Noto Kufi Arabic and Nunito fonts loaded globally.");
  }
  window.blazorInvoke || (window.blazorInvoke = async function(i, s, ...r) {
    var c;
    const f = document.querySelector(i);
    if (!f) return void console.error(`Element with selector "${i}" not found.`);
    const d = f.tagName.toLowerCase();
    if (d.includes("-") && !customElements.get(d) && await customElements.whenDefined(d), await ((c = f.componentOnReady) === null || c === void 0 ? void 0 : c.call(f)), typeof f[s] == "function") try {
      return await f[s](...r);
    } catch (h) {
      console.error(`Error invoking function "${s}" on element "${i}":`, h);
    }
    else console.error(`Function "${s}" not found on the element.`);
  }, window.blazorInvokeSet = async function(i, s, r) {
    const c = document.querySelector(i);
    if (c) try {
      return c[s] = r;
    } catch (f) {
      console.error(`Setting field ${s} failed to set value: ${r}:`, f);
    }
    else console.error(`Element with selector "${i}" not found.`);
  }, console.log("Global blazorInvoke initialized."));
})();
function Tm(n, i, s) {
  const r = typeof HTMLElement < "u" ? HTMLElement.prototype : null;
  for (; n && n !== r; ) {
    const c = Object.getOwnPropertyDescriptor(n, i);
    if (c && (!s || c.get)) return c;
    n = Object.getPrototypeOf(n);
  }
}
var os, Sy = (n, i) => {
  var s;
  Object.entries((s = i.o.t) != null ? s : {}).map((([r, [c]]) => {
    if (31 & c || 32 & c) {
      const f = n[r], d = Tm(Object.getPrototypeOf(n), r, !0) || Object.getOwnPropertyDescriptor(n, r);
      d && Object.defineProperty(n, r, { get() {
        return d.get.call(this);
      }, set(h) {
        d.set.call(this, h);
      }, configurable: !0, enumerable: !0 }), i.l.has(r) ? n[r] = i.l.get(r) : f !== void 0 && (n[r] = f);
    }
  }));
}, An = (n) => {
  if (n.__stencil__getHostRef) return n.__stencil__getHostRef();
}, D0 = (n, i) => i in n, ri = (n, i) => (0, console.error)(n, i), Br = /* @__PURE__ */ new Map(), C0 = "http://www.w3.org/1999/xlink", Xe = typeof window < "u" ? window : {}, of = Xe.HTMLElement || class {
}, st = { i: 0, u: "", jmp: (n) => n(), raf: (n) => requestAnimationFrame(n), ael: (n, i, s, r) => n.addEventListener(i, s, r), rel: (n, i, s, r) => n.removeEventListener(i, s, r), ce: (n, i) => new CustomEvent(n, i) }, uf = (() => {
  try {
    return !!Xe.document.adoptedStyleSheets && (new CSSStyleSheet(), typeof new CSSStyleSheet().replaceSync == "function");
  } catch {
  }
  return !1;
})(), Bc = !!uf && (!!Xe.document && Object.getOwnPropertyDescriptor(Xe.document.adoptedStyleSheets, "length").writable), Vc = !1, N0 = [], Em = [], zy = (n, i) => (s) => {
  n.push(s), Vc || (Vc = !0, 4 & st.i ? cf(Xc) : st.raf(Xc));
}, M0 = (n) => {
  for (let i = 0; i < n.length; i++) try {
    n[i](performance.now());
  } catch (s) {
    ri(s);
  }
  n.length = 0;
}, Xc = () => {
  M0(N0), M0(Em), (Vc = N0.length > 0) && st.raf(Xc);
}, cf = (n) => Promise.resolve(void 0).then(n), Ty = zy(Em);
function Ey(n) {
  const i = { mode: "open" };
  i.delegatesFocus = !!(16 & n.i);
  const s = this.attachShadow(i);
  os === void 0 && (os = null), os && (Bc ? s.adoptedStyleSheets.push(os) : s.adoptedStyleSheets = [...s.adoptedStyleSheets, os]);
}
var Am = (n) => {
  const i = Gc(n, "childNodes");
  n.tagName && n.tagName.includes("-") && n["s-cr"] && n.tagName !== "SLOT-FB" && ff(i, n.tagName).forEach(((r) => {
    r.nodeType === 1 && r.tagName === "SLOT-FB" && (r.hidden = !!Ay(r, ys(r), !1).length);
  }));
  let s = 0;
  for (s = 0; s < i.length; s++) {
    const r = i[s];
    r.nodeType === 1 && Gc(r, "childNodes").length && Am(r);
  }
};
function ff(n, i, s) {
  let r, c = 0, f = [];
  for (; c < n.length; c++) {
    if (r = n[c], r["s-sr"] && (!i || r["s-hn"] === i) && (s === void 0 || ys(r) === s) && (f.push(r), s !== void 0)) return f;
    f = [...f, ...ff(r.childNodes, i, s)];
  }
  return f;
}
var Ay = (n, i, s = !0) => {
  const r = [];
  (s && n["s-sr"] || !n["s-sr"]) && r.push(n);
  let c = n;
  for (; c = c.nextSibling; ) ys(c) !== i || !s && c["s-sr"] || r.push(c);
  return r;
}, q0 = (n, i) => n.nodeType === 1 ? n.getAttribute("slot") === null && i === "" || n.getAttribute("slot") === i : n["s-sn"] === i || i === "", ys = (n) => typeof n["s-sn"] == "string" ? n["s-sn"] : n.nodeType === 1 && n.getAttribute("slot") || void 0;
function Gc(n, i) {
  if ("__" + i in n) {
    const s = n["__" + i];
    return typeof s != "function" ? s : s.bind(n);
  }
  return typeof n[i] != "function" ? n[i] : n[i].bind(n);
}
var Qc, oi, zn = /* @__PURE__ */ new WeakMap(), Om = (n) => "sc-" + n.p, df = (n) => (n = typeof n) == "object" || n === "function", X = (n, i, ...s) => {
  let r = null, c = null, f = null, d = !1, h = !1;
  const g = [], v = (m) => {
    for (let _ = 0; _ < m.length; _++) r = m[_], Array.isArray(r) ? v(r) : r != null && typeof r != "boolean" && ((d = typeof n != "function" && !df(r)) && (r += ""), d && h ? g[g.length - 1].h += r : g.push(d ? Kr(null, r) : r), h = d);
  };
  if (v(s), i) {
    i.key && (c = i.key), i.name && (f = i.name);
    {
      const m = i.className || i.class;
      m && (i.class = typeof m != "object" ? m : Object.keys(m).filter(((_) => m[_])).join(" "));
    }
  }
  if (typeof n == "function") return n(i === null ? {} : i, g, Oy);
  const y = Kr(n, null);
  return y.v = i, g.length > 0 && (y.m = g), y.$ = c, y.j = f, y;
}, Kr = (n, i) => ({ i: 0, O: n, h: i ?? null, S: null, m: null, v: null, $: null, j: null }), to = {}, Oy = { forEach: (n, i) => n.map(j0).forEach(i), map: (n, i) => n.map(j0).map(i).map(Dy) }, j0 = (n) => ({ vattrs: n.v, vchildren: n.m, vkey: n.$, vname: n.j, vtag: n.O, vtext: n.h }), Dy = (n) => {
  if (typeof n.vtag == "function") {
    const s = { ...n.vattrs };
    return n.vkey && (s.key = n.vkey), n.vname && (s.name = n.vname), X(n.vtag, s, ...n.vchildren || []);
  }
  const i = Kr(n.vtag, n.vtext);
  return i.v = n.vattrs, i.m = n.vchildren, i.$ = n.vkey, i.j = n.vname, i;
}, Dm = (n) => {
  if (!n) return;
  const i = Object.keys(n);
  if (i.length === 0) return;
  let s = !1;
  for (const c of i) {
    if (s) break;
    for (const f of n[c]) if (typeof f == "string") {
      s = !0;
      break;
    }
  }
  if (!s) return n;
  const r = {};
  for (const c of i) r[c] = n[c].map(((f) => typeof f == "string" ? { [f]: 0 } : f));
  return r;
}, Cm = (n, i) => n == null || df(n) ? n : 4 & i ? n !== "false" && (n === "" || !!n) : 2 & i ? typeof n == "string" ? parseFloat(n) : typeof n == "number" ? n : NaN : 1 & i ? n + "" : n, Tn = (n, i) => {
  const s = n;
  return { emit: (r) => Nm(s, i, { bubbles: !0, composed: !0, cancelable: !0, detail: r }) };
}, Nm = (n, i, s) => {
  const r = st.ce(i, s);
  return n.dispatchEvent(r), r;
}, R0 = (n, i, s, r, c, f) => {
  if (s === r) return;
  let d = D0(n, i), h = i.toLowerCase();
  if (i === "class") {
    const g = n.classList, v = $0(s);
    let y = $0(r);
    g.remove(...v.filter(((m) => m && !y.includes(m)))), g.add(...y.filter(((m) => m && !v.includes(m))));
  } else if (i === "style") {
    for (const g in s) r && r[g] != null || (g.includes("-") ? n.style.removeProperty(g) : n.style[g] = "");
    for (const g in r) s && r[g] === s[g] || (g.includes("-") ? n.style.setProperty(g, r[g]) : n.style[g] = r[g]);
  } else if (i !== "key") {
    if (i === "ref") r && My(r, n);
    else if (n.__lookupSetter__(i) || i[0] !== "o" || i[1] !== "n") {
      if (i[0] === "a" && i.startsWith("attr:")) {
        const g = i.slice(5);
        let v;
        {
          const y = An(n);
          if (y && y.o && y.o.t) {
            const m = y.o.t[g];
            m && m[1] && (v = m[1]);
          }
        }
        return v || (v = g.replace(/([a-z0-9])([A-Z])/g, "$1-$2").toLowerCase()), void (r == null || r === !1 ? r === !1 && n.getAttribute(v) !== "" || n.removeAttribute(v) : n.setAttribute(v, r === !0 ? "" : r));
      }
      if (i[0] === "p" && i.startsWith("prop:")) {
        const g = i.slice(5);
        try {
          n[g] = r;
        } catch {
        }
        return;
      }
      {
        const g = df(r);
        if ((d || g && r !== null) && !c) try {
          if (n.tagName.includes("-")) n[i] !== r && (n[i] = r);
          else {
            const y = r ?? "";
            i === "list" ? d = !1 : s != null && n[i] === y || (typeof n.__lookupSetter__(i) == "function" ? n[i] = y : n.setAttribute(i, y));
          }
        } catch {
        }
        let v = !1;
        h !== (h = h.replace(/^xlink\:?/, "")) && (i = h, v = !0), r == null || r === !1 ? r === !1 && n.getAttribute(i) !== "" || (v ? n.removeAttributeNS(C0, i) : n.removeAttribute(i)) : (!d || 4 & f || c) && !g && n.nodeType === 1 && (r = r === !0 ? "" : r, v ? n.setAttributeNS(C0, i, r) : n.setAttribute(i, r));
      }
    } else if (i = i[2] === "-" ? i.slice(3) : D0(Xe, h) ? h.slice(2) : h[2] + i.slice(3), s || r) {
      const g = i.endsWith(Mm);
      i = i.replace(Ny, ""), s && st.rel(n, i, s, g), r && st.ael(n, i, r, g);
    }
  }
}, Cy = /\s/, $0 = (n) => (typeof n == "object" && n && "baseVal" in n && (n = n.baseVal), n && typeof n == "string" ? n.split(Cy) : []), Mm = "Capture", Ny = RegExp(Mm + "$"), Zc = (n, i, s) => {
  const r = i.S.nodeType === 11 && i.S.host ? i.S.host : i.S, c = n && n.v || {}, f = i.v || {};
  for (const d of U0(Object.keys(c))) d in f || R0(r, d, c[d], void 0, s, i.i);
  for (const d of U0(Object.keys(f))) R0(r, d, c[d], f[d], s, i.i);
};
function U0(n) {
  return n.includes("ref") ? [...n.filter(((i) => i !== "ref")), "ref"] : n;
}
var hs = !1, Jr = !1, ao = !1, Ct = !1, Kc = [], Jc = [], Fr = (n, i, s) => {
  var r;
  const c = i.m[s];
  let f, d, h, g = 0;
  if (hs || (ao = !0, c.O === "slot" && (c.i |= c.m ? 2 : 1)), c.h != null) f = c.S = Xe.document.createTextNode(c.h);
  else if (1 & c.i) f = c.S = Xe.document.createTextNode(""), Zc(null, c, Ct);
  else {
    if (Ct || (Ct = c.O === "svg"), !Xe.document) throw Error("You are trying to render a Stencil component in an environment that doesn't support the DOM.");
    if (f = c.S = Xe.document.createElementNS(Ct ? "http://www.w3.org/2000/svg" : "http://www.w3.org/1999/xhtml", !hs && 2 & c.i ? "slot-fb" : c.O), Ct && c.O === "foreignObject" && (Ct = !1), Zc(null, c, Ct), c.m) {
      const v = c.O === "template" ? f.content : f;
      for (g = 0; g < c.m.length; ++g) d = Fr(n, c, g), d && v.appendChild(d);
    }
    c.O === "svg" ? Ct = !1 : f.tagName === "foreignObject" && (Ct = !0);
  }
  return f["s-hn"] = oi, 3 & c.i && (f["s-sr"] = !0, f["s-cr"] = Qc, f["s-sn"] = c.j || "", f["s-rf"] = (r = c.v) == null ? void 0 : r.ref, (function(v) {
    if (v.assignedElements || v.assignedNodes || !v["s-sr"]) return;
    const y = (m) => (function(_) {
      const x = [], T = this["s-sn"];
      _ != null && _.flatten && console.error(`
          Flattening is not supported for Stencil non-shadow slots.
          You can use \`.childNodes\` to nested slot fallback content.
          If you have a particular use case, please open an issue on the Stencil repo.
        `);
      const A = this["s-cr"].parentElement;
      return (A.__childNodes ? A.childNodes : ((z) => {
        const q = [];
        for (let $ = 0; $ < z.length; $++) {
          const j = z[$]["s-nr"] || void 0;
          j && j.isConnected && q.push(j);
        }
        return q;
      })(A.childNodes)).forEach(((z) => {
        T === ys(z) && x.push(z);
      })), m ? x.filter(((z) => z.nodeType === 1)) : x;
    }).bind(v);
    v.assignedElements = y(!0), v.assignedNodes = y(!1);
  })(f), h = n && n.m && n.m[s], h && h.O === c.O && n.S && qm(n.S)), f;
}, qm = (n) => {
  st.i |= 1;
  const i = n.closest(oi.toLowerCase());
  if (i != null) {
    const s = Array.from(i.__childNodes || i.childNodes).find(((c) => c["s-cr"])), r = Array.from(n.__childNodes || n.childNodes);
    for (const c of s ? r.reverse() : r) c["s-sh"] != null && (sa(i, c, s ?? null), c["s-sh"] = void 0, ao = !0);
  }
  st.i &= -2;
}, Wr = (n, i) => {
  st.i |= 1;
  const s = Array.from(n.__childNodes || n.childNodes);
  if (n["s-sr"]) {
    let r = n;
    for (; r = r.nextSibling; ) r && r["s-sn"] === n["s-sn"] && r["s-sh"] === oi && s.push(r);
  }
  for (let r = s.length - 1; r >= 0; r--) {
    const c = s[r];
    c["s-hn"] !== oi && c["s-ol"] && (sa(ms(c).parentNode, c, ms(c)), c["s-ol"].remove(), c["s-ol"] = void 0, c["s-sh"] = void 0, ao = !0), i && Wr(c, i);
  }
  st.i &= -2;
}, L0 = (n, i, s, r, c, f) => {
  let d, h = n["s-cr"] && n["s-cr"].parentNode || n;
  for (h.shadowRoot && h.tagName === oi && (h = h.shadowRoot), s.O === "template" && (h = h.content); c <= f; ++c) r[c] && (d = Fr(null, s, c), d && (r[c].S = d, sa(h, d, ms(i))));
}, H0 = (n, i, s) => {
  for (let r = i; r <= s; ++r) {
    const c = n[r];
    if (c) {
      const f = c.S;
      Rm(c), f && (Jr = !0, f["s-ol"] ? f["s-ol"].remove() : Wr(f, !0), f.remove());
    }
  }
}, Lr = (n, i, s = !1) => n.O === i.O && (n.O === "slot" ? n.j === i.j : s ? (s && !n.$ && i.$ && (n.$ = i.$), !0) : n.$ === i.$), ms = (n) => n && n["s-ol"] || n, nl = (n, i, s = !1) => {
  const r = i.S = n.S, c = n.m, f = i.m, d = i.O, h = i.h;
  let g;
  h == null ? (Ct = d === "svg" || d !== "foreignObject" && Ct, d !== "slot" || hs || n.j !== i.j && (i.S["s-sn"] = i.j || "", qm(i.S.parentElement)), Zc(n, i, Ct), c !== null && f !== null ? ((v, y, m, _, x = !1) => {
    let T, A, z = 0, q = 0, $ = 0, j = 0, L = y.length - 1, V = y[0], J = y[L], Z = _.length - 1, F = _[0], Te = _[Z];
    const Se = m.O === "template" ? v.content : v;
    for (; z <= L && q <= Z; ) if (V == null) V = y[++z];
    else if (J == null) J = y[--L];
    else if (F == null) F = _[++q];
    else if (Te == null) Te = _[--Z];
    else if (Lr(V, F, x)) nl(V, F, x), V = y[++z], F = _[++q];
    else if (Lr(J, Te, x)) nl(J, Te, x), J = y[--L], Te = _[--Z];
    else if (Lr(V, Te, x)) V.O !== "slot" && Te.O !== "slot" || Wr(V.S.parentNode, !1), nl(V, Te, x), sa(Se, V.S, J.S.nextSibling), V = y[++z], Te = _[--Z];
    else if (Lr(J, F, x)) V.O !== "slot" && Te.O !== "slot" || Wr(J.S.parentNode, !1), nl(J, F, x), sa(Se, J.S, V.S), J = y[--L], F = _[++q];
    else {
      for ($ = -1, j = z; j <= L; ++j) if (y[j] && y[j].$ !== null && y[j].$ === F.$) {
        $ = j;
        break;
      }
      $ >= 0 ? (A = y[$], A.O !== F.O ? T = Fr(y && y[q], m, $) : (nl(A, F, x), y[$] = void 0, T = A.S), F = _[++q]) : (T = Fr(y && y[q], m, q), F = _[++q]), T && sa(ms(V.S).parentNode, T, ms(V.S));
    }
    z > L ? L0(v, _[Z + 1] == null ? null : _[Z + 1].S, m, _, q, Z) : q > Z && H0(y, z, L);
  })(r, c, i, f, s) : f !== null ? (n.h !== null && (r.textContent = ""), L0(r, null, i, f, 0, f.length - 1)) : !s && c !== null && H0(c, 0, c.length - 1), Ct && d === "svg" && (Ct = !1)) : (g = r["s-cr"]) ? g.parentNode.textContent = h : n.h !== h && (r.data = h);
}, Va = [], jm = (n) => {
  let i, s, r;
  const c = n.__childNodes || n.childNodes;
  for (const f of c) {
    if (f["s-sr"] && (i = f["s-cr"]) && i.parentNode) {
      s = i.parentNode.__childNodes || i.parentNode.childNodes;
      const d = f["s-sn"];
      for (r = s.length - 1; r >= 0; r--) if (i = s[r], !(i["s-cn"] || i["s-nr"] || i["s-hn"] === f["s-hn"] || i["s-sh"] && i["s-sh"] === f["s-hn"])) if (q0(i, d)) {
        let h = Va.find(((g) => g.k === i));
        Jr = !0, i["s-sn"] = i["s-sn"] || d, h ? (h.k["s-sh"] = f["s-hn"], h.N = f) : (i["s-sh"] = f["s-hn"], Va.push({ N: f, k: i })), i["s-sr"] && Va.map(((g) => {
          q0(g.k, i["s-sn"]) && (h = Va.find(((v) => v.k === i)), h && !g.N && (g.N = h.N));
        }));
      } else Va.some(((h) => h.k === i)) || Va.push({ k: i });
    }
    f.nodeType === 1 && jm(f);
  }
}, Rm = (n) => {
  n.v && n.v.ref && Kc.push((() => n.v.ref(null))), n.m && n.m.map(Rm);
}, My = (n, i) => {
  Jc.push((() => n(i)));
}, sa = (n, i, s, r) => {
  if (typeof i["s-sn"] == "string") {
    n.insertBefore(i, s);
    const { slotNode: c } = (function(f, d) {
      var h;
      if (!(d = d || ((h = f["s-ol"]) == null ? void 0 : h.parentElement))) return { slotNode: null, slotName: "" };
      const g = f["s-sn"] = ys(f) || "";
      return { slotNode: ff(Gc(d, "childNodes"), d.tagName, g)[0], slotName: g };
    })(i);
    return c && !r && (function(f) {
      f.dispatchEvent(new CustomEvent("slotchange", { bubbles: !1, cancelable: !1, composed: !1 }));
    })(c), i;
  }
  return n.__insertBefore ? n.__insertBefore(i, s) : n == null ? void 0 : n.insertBefore(i, s);
}, qy = (n, i, s = !1) => {
  var r, c, f, d, h;
  const g = n.$hostElement$, v = n.o, y = n.M || Kr(null, null), m = ((_) => _ && _.O === to)(i) ? i : X(null, null, i);
  if (oi = g.tagName, v.A && (m.v = m.v || {}, v.A.forEach((([_, x]) => {
    m.v[x] = g[_];
  }))), s && m.v) for (const _ of Object.keys(m.v)) g.hasAttribute(_) && !["key", "ref", "style", "class"].includes(_) && (m.v[_] = g[_]);
  if (m.O = null, m.i |= 4, n.M = m, m.S = y.S = g.shadowRoot || g, hs = !(!(1 & v.i) || 128 & v.i), Qc = g["s-cr"], Jr = !1, nl(y, m, s), st.i |= 1, ao) {
    jm(m.S);
    for (const _ of Va) {
      const x = _.k;
      if (!x["s-ol"] && Xe.document) {
        const T = Xe.document.createTextNode("");
        T["s-nr"] = x, sa(x.parentNode, x["s-ol"] = T, x, s);
      }
    }
    for (const _ of Va) {
      const x = _.k, T = _.N;
      if (x.nodeType === 1 && s && (x["s-ih"] = (r = x.hidden) != null && r), T) {
        const A = T.parentNode;
        let z = T.nextSibling;
        if (z && z.nodeType === 1) {
          let q = (c = x["s-ol"]) == null ? void 0 : c.previousSibling;
          for (; q; ) {
            let $ = (f = q["s-nr"]) != null ? f : null;
            if ($ && $["s-sn"] === x["s-sn"] && A === ($.__parentNode || $.parentNode)) {
              for ($ = $.nextSibling; $ === x || $ != null && $["s-sr"]; ) $ = $ == null ? void 0 : $.nextSibling;
              if (!$ || !$["s-nr"]) {
                z = $;
                break;
              }
            }
            q = q.previousSibling;
          }
        }
        if ((!z && A !== (x.__parentNode || x.parentNode) || (x.__nextSibling || x.nextSibling) !== z) && x !== z) {
          if (sa(A, x, z, s), x.nodeType === 8 && x.nodeValue.startsWith("s-nt-")) {
            const q = Xe.document.createTextNode(x.nodeValue.replace(/^s-nt-/, ""));
            q["s-hn"] = x["s-hn"], q["s-sn"] = x["s-sn"], q["s-sh"] = x["s-sh"], q["s-sr"] = x["s-sr"], q["s-ol"] = x["s-ol"], q["s-ol"]["s-nr"] = q, sa(x.parentNode, q, x, s), x.parentNode.removeChild(x);
          }
          x.nodeType === 1 && x.tagName !== "SLOT-FB" && (x.hidden = (d = x["s-ih"]) != null && d);
        }
        x && typeof T["s-rf"] == "function" && T["s-rf"](T);
      } else x.nodeType === 1 && (x.hidden = !0);
    }
  }
  if (Jr && Am(m.S), st.i &= -2, Va.length = 0, !hs && !(1 & v.i) && g["s-cr"]) {
    const _ = m.S.__childNodes || m.S.childNodes;
    for (const x of _) if (x["s-hn"] !== oi && !x["s-sh"]) {
      if (s && x["s-ih"] == null && (x["s-ih"] = (h = x.hidden) != null && h), x.nodeType === 1) x.hidden = !0;
      else if (x.nodeType === 3 && x.nodeValue.trim()) {
        const T = Xe.document.createComment("s-nt-" + x.nodeValue);
        T["s-sn"] = x["s-sn"], sa(x.parentNode, T, x, s), x.parentNode.removeChild(x);
      }
    }
  }
  Qc = void 0, Kc.forEach(((_) => _())), Kc.length = 0, Jc.forEach(((_) => _())), Jc.length = 0;
}, $m = (n, i) => {
  if (i && !n._ && i["s-p"]) {
    const s = i["s-p"].push(new Promise(((r) => n._ = () => {
      i["s-p"].splice(s - 1, 1), r();
    })));
  }
}, pf = (n, i) => {
  if (n.i |= 16, 4 & n.i) return void (n.i |= 512);
  $m(n, n.C);
  const s = () => jy(n, i);
  if (!i) return Ty(s);
  queueMicrotask((() => {
    s();
  }));
}, jy = (n, i) => {
  const s = n.$hostElement$, r = s;
  if (!r) throw Error(`Can't render component <${s.tagName.toLowerCase()} /> with invalid Stencil runtime! Make sure this imported component is compiled with a \`externalRuntime: true\` flag. For more information, please refer to https://stenciljs.com/docs/custom-elements#externalruntime`);
  let c;
  return c = ds(r, i ? "componentWillLoad" : "componentWillUpdate", void 0, s), c = Y0(c, (() => ds(r, "componentWillRender", void 0, s))), Y0(c, (() => $y(n, r, i)));
}, Y0 = (n, i) => Ry(n) ? n.then(i).catch(((s) => {
  console.error(s), i();
})) : i(), Ry = (n) => n instanceof Promise || n && n.then && typeof n.then == "function", $y = async (n, i, s) => {
  var r;
  const c = n.$hostElement$, f = c["s-rc"];
  s && ((d) => {
    const h = d.o, g = d.$hostElement$, v = h.i, y = ((m, _) => {
      var x, T, A;
      const z = Om(_), q = Br.get(z);
      if (!Xe.document) return z;
      if (m = m.nodeType === 11 ? m : Xe.document, q) if (typeof q == "string") {
        let $, j = zn.get(m = m.head || m);
        if (j || zn.set(m, j = /* @__PURE__ */ new Set()), !j.has(z)) {
          $ = Xe.document.createElement("style"), $.textContent = q;
          const L = (x = st.L) != null ? x : (function() {
            var V, J, Z;
            return (Z = (J = (V = Xe.document.head) == null ? void 0 : V.querySelector('meta[name="csp-nonce"]')) == null ? void 0 : J.getAttribute("content")) != null ? Z : void 0;
          })();
          if (L != null && $.setAttribute("nonce", L), !(1 & _.i)) if (m.nodeName === "HEAD") {
            const V = m.querySelectorAll("link[rel=preconnect]"), J = V.length > 0 ? V[V.length - 1].nextSibling : m.querySelector("style");
            m.insertBefore($, (J == null ? void 0 : J.parentNode) === m ? J : null);
          } else if ("host" in m) if (uf) {
            const V = new ((T = m.defaultView) != null ? T : m.ownerDocument.defaultView).CSSStyleSheet();
            V.replaceSync(q), Bc ? m.adoptedStyleSheets.unshift(V) : m.adoptedStyleSheets = [V, ...m.adoptedStyleSheets];
          } else {
            const V = m.querySelector("style");
            V ? V.textContent = q + V.textContent : m.prepend($);
          }
          else m.append($);
          1 & _.i && m.insertBefore($, null), 4 & _.i && ($.textContent += "slot-fb{display:contents}slot-fb[hidden]{display:none}"), j && j.add(z);
        }
      } else {
        let $ = zn.get(m);
        if ($ || zn.set(m, $ = /* @__PURE__ */ new Set()), !$.has(z)) {
          const j = (A = m.defaultView) != null ? A : m.ownerDocument.defaultView;
          let L;
          if (q.constructor === j.CSSStyleSheet) L = q;
          else {
            L = new j.CSSStyleSheet();
            for (let V = 0; V < q.cssRules.length; V++) L.insertRule(q.cssRules[V].cssText, V);
          }
          Bc ? m.adoptedStyleSheets.push(L) : m.adoptedStyleSheets = [...m.adoptedStyleSheets, L], $.add(z);
        }
      }
      return z;
    })(g.shadowRoot ? g.shadowRoot : g.getRootNode(), h);
    10 & v && (g["s-sc"] = y, g.classList.add(y + "-h"));
  })(n), Uy(n, i, c, s), f && (f.map(((d) => d())), c["s-rc"] = void 0);
  {
    const d = (r = c["s-p"]) != null ? r : [], h = () => Ly(n);
    d.length === 0 ? h() : (Promise.all(d).then(h).catch(h), n.i |= 4, d.length = 0);
  }
}, Uy = (n, i, s, r) => {
  try {
    i = i.render(), n.i &= -17, n.i |= 2, qy(n, i, r);
  } catch (c) {
    ri(c, n.$hostElement$);
  }
  return null;
}, Ly = (n) => {
  const i = n.$hostElement$, s = i, r = n.C;
  ds(s, "componentDidRender", void 0, i), 64 & n.i ? ds(s, "componentDidUpdate", void 0, i) : (n.i |= 64, Yy(i), ds(s, "componentDidLoad", void 0, i), n.F(i), r || Hy()), n._ && (n._(), n._ = void 0), 512 & n.i && cf((() => pf(n, !1))), n.i &= -517;
}, Hy = () => {
  cf((() => Nm(Xe, "appload", { detail: { namespace: "shift-components" } })));
}, ds = (n, i, s, r) => {
  if (n && n[i]) try {
    return n[i](s);
  } catch (c) {
    ri(c, r);
  }
}, Yy = (n) => n.classList.add("hydrated"), B0 = (n, i, s, r) => {
  const c = An(n);
  if (!c) return;
  const f = n, d = c.l.get(i), h = c.i, g = f;
  if (!((s = Cm(s, r.t[i][0])) === d || Number.isNaN(d) && Number.isNaN(s))) {
    if (c.l.set(i, s), r.R) {
      const v = r.R[i];
      v && v.map(((y) => {
        try {
          const [[m, _]] = Object.entries(y);
          (128 & h || 1 & _) && (g ? g[m](s, d, i) : c.T.push((() => {
            c.D[m](s, d, i);
          })));
        } catch (m) {
          ri(m, f);
        }
      }));
    }
    if (2 & h) {
      if (g.componentShouldUpdate && g.componentShouldUpdate(s, d, i) === !1 && !(16 & h)) return;
      16 & h || pf(c, !1);
    }
  }
}, By = (n, i) => {
  var s, r;
  const c = n.prototype;
  {
    n.watchers && !i.R && (i.R = Dm(n.watchers)), n.deserializers && !i.H && (i.H = n.deserializers), n.serializers && !i.P && (i.P = n.serializers);
    const f = Object.entries((s = i.t) != null ? s : {});
    f.map((([d, [h]]) => {
      if (31 & h || 32 & h) {
        const { get: g, set: v } = Tm(c, d) || {};
        g && (i.t[d][0] |= 2048), v && (i.t[d][0] |= 4096), Object.defineProperty(c, d, { get() {
          return g ? g.apply(this) : ((y, m) => An(this).l.get(m))(0, d);
        }, configurable: !0, enumerable: !0 }), Object.defineProperty(c, d, { set(y) {
          const m = An(this);
          if (m) {
            if (v) return (32 & h ? this[d] : m.$hostElement$[d]) === void 0 && m.l.get(d) && (y = m.l.get(d)), v.call(this, Cm(y, h)), void B0(this, d, y = 32 & h ? this[d] : m.$hostElement$[d], i);
            B0(this, d, y, i);
          }
        } });
      }
    }));
    {
      const d = /* @__PURE__ */ new Map();
      c.attributeChangedCallback = function(h, g, v) {
        st.jmp((() => {
          var y;
          const m = d.get(h), _ = An(this);
          if (this.hasOwnProperty(m), c.hasOwnProperty(m) && typeof this[m] == "number" && this[m] == v) return;
          if (m == null) {
            const q = _ == null ? void 0 : _.i;
            if (_ && q && !(8 & q) && v !== g) {
              const $ = this, j = (y = i.R) == null ? void 0 : y[h];
              j == null || j.forEach(((L) => {
                const [[V, J]] = Object.entries(L);
                $[V] != null && (128 & q || 1 & J) && $[V].call($, v, g, h);
              }));
            }
            return;
          }
          const x = f.find((([q]) => q === m)), T = x && 4 & x[1][0], A = T && v === null && this[m] === void 0;
          T && (v = v !== null && v !== "false");
          const z = Object.getOwnPropertyDescriptor(c, m);
          A || v == this[m] || z.get && !z.set || (this[m] = v);
        }));
      }, n.observedAttributes = Array.from(/* @__PURE__ */ new Set([...Object.keys((r = i.R) != null ? r : {}), ...f.filter((([h, g]) => 31 & g[0])).map((([h, g]) => {
        var v;
        const y = g[1] || h;
        return d.set(y, h), 512 & g[0] && ((v = i.A) == null || v.push([h, y])), y;
      }))]));
    }
  }
  return n;
}, hf = (n, i) => {
  const s = { i: i[0], p: i[1] };
  try {
    s.t = i[2], s.U = i[3], s.R = Dm(n.R), s.H = n.H, s.P = n.P, s.A = [];
    const r = n.prototype.connectedCallback, c = n.prototype.disconnectedCallback;
    return Object.assign(n.prototype, { __hasHostListenerAttached: !1, __registerHost() {
      ((f, d) => {
        const h = { i: 0, $hostElement$: f, o: d, l: /* @__PURE__ */ new Map(), W: /* @__PURE__ */ new Map() };
        h.B = new Promise(((v) => h.F = v)), f["s-p"] = [], f["s-rc"] = [];
        const g = h;
        f.__stencil__getHostRef = () => g, 512 & d.i && Sy(f, h);
      })(this, s);
    }, connectedCallback() {
      if (!this.__hasHostListenerAttached) {
        const f = An(this);
        if (!f) return;
        V0(this, f, s.U), this.__hasHostListenerAttached = !0;
      }
      ((f) => {
        if (!(1 & st.i)) {
          const d = An(f);
          if (!d) return;
          const h = d.o, g = () => {
          };
          if (1 & d.i) V0(f, d, h.U), d != null && d.D || d != null && d.B && d.B.then((() => {
          }));
          else {
            d.i |= 1, 12 & h.i && ((v) => {
              if (!Xe.document) return;
              const y = v["s-cr"] = Xe.document.createComment("");
              y["s-cn"] = !0, sa(v, y, v.firstChild);
            })(f);
            {
              let v = f;
              for (; v = v.parentNode || v.host; ) if (v["s-p"]) {
                $m(d, d.C = v);
                break;
              }
            }
            h.t && Object.entries(h.t).map((([v, [y]]) => {
              if (31 & y && Object.prototype.hasOwnProperty.call(f, v)) {
                const m = f[v];
                delete f[v], f[v] = m;
              }
            })), (async (v, y, m) => {
              let _;
              try {
                if (!(32 & y.i) && (y.i |= 32, _ = v.constructor, customElements.whenDefined(v.localName).then((() => y.i |= 128)), _ && _.style)) {
                  let A;
                  typeof _.style == "string" && (A = _.style);
                  const z = Om(m);
                  if (!Br.has(z)) {
                    const q = () => {
                    };
                    (($, j, L) => {
                      let V = Br.get($);
                      uf && L ? (V = V || new CSSStyleSheet(), typeof V == "string" ? V = j : V.replaceSync(j)) : V = j, Br.set($, V);
                    })(z, A, !!(1 & m.i)), q();
                  }
                }
                const x = y.C, T = () => pf(y, !0);
                x && x["s-rc"] ? x["s-rc"].push(T) : T();
              } catch (x) {
                ri(x, v), y._ && (y._(), y._ = void 0), y.F && y.F(v);
              }
            })(f, d, h);
          }
          g();
        }
      })(this), r && r.call(this);
    }, disconnectedCallback() {
      (async (f) => {
        if (!(1 & st.i)) {
          const d = An(f);
          d != null && d.V && (d.V.map(((h) => h())), d.V = void 0);
        }
        zn.has(f) && zn.delete(f), f.shadowRoot && zn.has(f.shadowRoot) && zn.delete(f.shadowRoot);
      })(this), c && c.call(this);
    }, __attachShadow() {
      if (this.shadowRoot) {
        if (this.shadowRoot.mode !== "open") throw Error(`Unable to re-use existing shadow root for ${s.p}! Mode is set to ${this.shadowRoot.mode} but Stencil only supports open shadow roots.`);
      } else Ey.call(this, s);
    } }), Object.defineProperty(n, "is", { value: s.p, configurable: !0 }), By(n, s);
  } catch (r) {
    return ri(r), n;
  }
}, V0 = (n, i, s) => {
  s && Xe.document && s.map((([r, c, f]) => {
    const d = Xy(Xe.document, n, r), h = Vy(i, f), g = Gy(r);
    st.ael(d, c, h, g), (i.V = i.V || []).push((() => st.rel(d, c, h, g)));
  }));
}, Vy = (n, i) => (s) => {
  try {
    n.$hostElement$[i](s);
  } catch (r) {
    ri(r, n.$hostElement$);
  }
}, Xy = (n, i, s) => 4 & s ? n : i, Gy = (n) => ({ passive: !!(1 & n), capture: !!(2 & n) });
/*!
 * Built by ShiftSoftware
 * Copyright (c)
 */
function Qy(n) {
  return n && n.__esModule && Object.prototype.hasOwnProperty.call(n, "default") ? n.default : n;
}
var X0, G0, Q0, Z0, K0, ti = (function() {
  if (G0) return X0;
  function n(x) {
    this._maxSize = x, this.clear();
  }
  G0 = 1, n.prototype.clear = function() {
    this._size = 0, this._values = /* @__PURE__ */ Object.create(null);
  }, n.prototype.get = function(x) {
    return this._values[x];
  }, n.prototype.set = function(x, T) {
    return this._size >= this._maxSize && this.clear(), x in this._values || this._size++, this._values[x] = T;
  };
  var i = /[^.^\]^[]+|(?=\[\]|\.\.)/g, s = /^\d+$/, r = /^\d/, c = /[~`!#$%\^&*+=\-\[\]\\';,/{}|\\":<>\?]/g, f = /^\s*(['"]?)(.*?)(\1)\s*$/, d = new n(512), h = new n(512), g = new n(512);
  function v(x) {
    return d.get(x) || d.set(x, y(x).map((function(T) {
      return T.replace(f, "$2");
    })));
  }
  function y(x) {
    return x.match(i) || [""];
  }
  function m(x) {
    return typeof x == "string" && x && ["'", '"'].indexOf(x.charAt(0)) !== -1;
  }
  function _(x) {
    return !m(x) && ((function(T) {
      return T.match(r) && !T.match(s);
    })(x) || (function(T) {
      return c.test(T);
    })(x));
  }
  return X0 = { Cache: n, split: y, normalizePath: v, setter: function(x) {
    var T = v(x);
    return h.get(x) || h.set(x, (function(A, z) {
      for (var q = 0, $ = T.length, j = A; q < $ - 1; ) {
        var L = T[q];
        if (L === "__proto__" || L === "constructor" || L === "prototype") return A;
        j = j[T[q++]];
      }
      j[T[q]] = z;
    }));
  }, getter: function(x, T) {
    var A = v(x);
    return g.get(x) || g.set(x, (function(z) {
      for (var q = 0, $ = A.length; q < $; ) {
        if (z == null && T) return;
        z = z[A[q++]];
      }
      return z;
    }));
  }, join: function(x) {
    return x.reduce((function(T, A) {
      return T + (m(A) || s.test(A) ? "[" + A + "]" : (T ? "." : "") + A);
    }), "");
  }, forEach: function(x, T, A) {
    (function(z, q, $) {
      var j, L, V, J, Z = z.length;
      for (L = 0; L < Z; L++) (j = z[L]) && (_(j) && (j = '"' + j + '"'), V = !(J = m(j)) && /^\d+$/.test(j), q.call($, j, J, V, L, z));
    })(Array.isArray(x) ? x : y(x), T, A);
  } };
})(), Dc = (function() {
  if (Z0) return Q0;
  Z0 = 1;
  const n = /[A-Z\xc0-\xd6\xd8-\xde]?[a-z\xdf-\xf6\xf8-\xff]+(?:['’](?:d|ll|m|re|s|t|ve))?(?=[\xac\xb1\xd7\xf7\x00-\x2f\x3a-\x40\x5b-\x60\x7b-\xbf\u2000-\u206f \t\x0b\f\xa0\ufeff\n\r\u2028\u2029\u1680\u180e\u2000\u2001\u2002\u2003\u2004\u2005\u2006\u2007\u2008\u2009\u200a\u202f\u205f\u3000]|[A-Z\xc0-\xd6\xd8-\xde]|$)|(?:[A-Z\xc0-\xd6\xd8-\xde]|[^\ud800-\udfff\xac\xb1\xd7\xf7\x00-\x2f\x3a-\x40\x5b-\x60\x7b-\xbf\u2000-\u206f \t\x0b\f\xa0\ufeff\n\r\u2028\u2029\u1680\u180e\u2000\u2001\u2002\u2003\u2004\u2005\u2006\u2007\u2008\u2009\u200a\u202f\u205f\u3000\d+\u2700-\u27bfa-z\xdf-\xf6\xf8-\xffA-Z\xc0-\xd6\xd8-\xde])+(?:['’](?:D|LL|M|RE|S|T|VE))?(?=[\xac\xb1\xd7\xf7\x00-\x2f\x3a-\x40\x5b-\x60\x7b-\xbf\u2000-\u206f \t\x0b\f\xa0\ufeff\n\r\u2028\u2029\u1680\u180e\u2000\u2001\u2002\u2003\u2004\u2005\u2006\u2007\u2008\u2009\u200a\u202f\u205f\u3000]|[A-Z\xc0-\xd6\xd8-\xde](?:[a-z\xdf-\xf6\xf8-\xff]|[^\ud800-\udfff\xac\xb1\xd7\xf7\x00-\x2f\x3a-\x40\x5b-\x60\x7b-\xbf\u2000-\u206f \t\x0b\f\xa0\ufeff\n\r\u2028\u2029\u1680\u180e\u2000\u2001\u2002\u2003\u2004\u2005\u2006\u2007\u2008\u2009\u200a\u202f\u205f\u3000\d+\u2700-\u27bfa-z\xdf-\xf6\xf8-\xffA-Z\xc0-\xd6\xd8-\xde])|$)|[A-Z\xc0-\xd6\xd8-\xde]?(?:[a-z\xdf-\xf6\xf8-\xff]|[^\ud800-\udfff\xac\xb1\xd7\xf7\x00-\x2f\x3a-\x40\x5b-\x60\x7b-\xbf\u2000-\u206f \t\x0b\f\xa0\ufeff\n\r\u2028\u2029\u1680\u180e\u2000\u2001\u2002\u2003\u2004\u2005\u2006\u2007\u2008\u2009\u200a\u202f\u205f\u3000\d+\u2700-\u27bfa-z\xdf-\xf6\xf8-\xffA-Z\xc0-\xd6\xd8-\xde])+(?:['’](?:d|ll|m|re|s|t|ve))?|[A-Z\xc0-\xd6\xd8-\xde]+(?:['’](?:D|LL|M|RE|S|T|VE))?|\d*(?:1ST|2ND|3RD|(?![123])\dTH)(?=\b|[a-z_])|\d*(?:1st|2nd|3rd|(?![123])\dth)(?=\b|[A-Z_])|\d+|(?:[\u2700-\u27bf]|(?:\ud83c[\udde6-\uddff]){2}|[\ud800-\udbff][\udc00-\udfff])[\ufe0e\ufe0f]?(?:[\u0300-\u036f\ufe20-\ufe2f\u20d0-\u20ff]|\ud83c[\udffb-\udfff])?(?:\u200d(?:[^\ud800-\udfff]|(?:\ud83c[\udde6-\uddff]){2}|[\ud800-\udbff][\udc00-\udfff])[\ufe0e\ufe0f]?(?:[\u0300-\u036f\ufe20-\ufe2f\u20d0-\u20ff]|\ud83c[\udffb-\udfff])?)*/g, i = (f) => f.match(n) || [], s = (f) => f[0].toUpperCase() + f.slice(1), r = (f, d) => i(f).join(d).toLowerCase(), c = (f) => i(f).reduce(((d, h) => `${d}${d ? h[0].toUpperCase() + h.slice(1).toLowerCase() : h.toLowerCase()}`), "");
  return Q0 = { words: i, upperFirst: s, camelCase: c, pascalCase: (f) => s(c(f)), snakeCase: (f) => r(f, "_"), kebabCase: (f) => r(f, "-"), sentenceCase: (f) => s(r(f, " ")), titleCase: (f) => i(f).map(s).join(" ") };
})(), Hr = { exports: {} }, Zy = (function() {
  if (K0) return Hr.exports;
  function n(i, s) {
    var r = i.length, c = new Array(r), f = {}, d = r, h = (function(y) {
      for (var m = /* @__PURE__ */ new Map(), _ = 0, x = y.length; _ < x; _++) {
        var T = y[_];
        m.has(T[0]) || m.set(T[0], /* @__PURE__ */ new Set()), m.has(T[1]) || m.set(T[1], /* @__PURE__ */ new Set()), m.get(T[0]).add(T[1]);
      }
      return m;
    })(s), g = (function(y) {
      for (var m = /* @__PURE__ */ new Map(), _ = 0, x = y.length; _ < x; _++) m.set(y[_], _);
      return m;
    })(i);
    for (s.forEach((function(y) {
      if (!g.has(y[0]) || !g.has(y[1])) throw new Error("Unknown node. There is an unknown node in the supplied edges.");
    })); d--; ) f[d] || v(i[d], d, /* @__PURE__ */ new Set());
    return c;
    function v(y, m, _) {
      if (_.has(y)) {
        var x;
        try {
          x = ", node was:" + JSON.stringify(y);
        } catch {
          x = "";
        }
        throw new Error("Cyclic dependency" + x);
      }
      if (!g.has(y)) throw new Error("Found unknown node. Make sure to provided all involved nodes. Unknown node: " + JSON.stringify(y));
      if (!f[m]) {
        f[m] = !0;
        var T = h.get(y) || /* @__PURE__ */ new Set();
        if (m = (T = Array.from(T)).length) {
          _.add(y);
          do {
            var A = T[--m];
            v(A, g.get(A), _);
          } while (m);
          _.delete(y);
        }
        c[--r] = y;
      }
    }
  }
  return K0 = 1, Hr.exports = function(i) {
    return n((function(s) {
      for (var r = /* @__PURE__ */ new Set(), c = 0, f = s.length; c < f; c++) {
        var d = s[c];
        r.add(d[0]), r.add(d[1]);
      }
      return Array.from(r);
    })(i), i);
  }, Hr.exports.array = n, Hr.exports;
})(), Ky = Qy(Zy);
const Jy = Object.prototype.toString, Fy = Error.prototype.toString, Wy = RegExp.prototype.toString, Iy = typeof Symbol < "u" ? Symbol.prototype.toString : () => "", Py = /^Symbol\((.*)\)(.*)$/;
function J0(n, i = !1) {
  if (n == null || n === !0 || n === !1) return "" + n;
  const s = typeof n;
  if (s === "number") return (function(c) {
    return c != +c ? "NaN" : c === 0 && 1 / c < 0 ? "-0" : "" + c;
  })(n);
  if (s === "string") return i ? `"${n}"` : n;
  if (s === "function") return "[Function " + (n.name || "anonymous") + "]";
  if (s === "symbol") return Iy.call(n).replace(Py, "Symbol($1)");
  const r = Jy.call(n).slice(8, -1);
  return r === "Date" ? isNaN(n.getTime()) ? "" + n : n.toISOString(n) : r === "Error" || n instanceof Error ? "[" + Fy.call(n) + "]" : r === "RegExp" ? Wy.call(n) : null;
}
function On(n, i) {
  let s = J0(n, i);
  return s !== null ? s : JSON.stringify(n, (function(r, c) {
    let f = J0(this[r], i);
    return f !== null ? f : c;
  }), 2);
}
function Um(n) {
  return n == null ? [] : [].concat(n);
}
let Lm, Hm, Ym, e_ = /\$\{\s*(\w+)\s*\}/g;
Lm = Symbol.toStringTag;
let F0 = class {
  constructor(i, s, r, c) {
    this.name = void 0, this.message = void 0, this.value = void 0, this.path = void 0, this.type = void 0, this.params = void 0, this.errors = void 0, this.inner = void 0, this[Lm] = "Error", this.name = "ValidationError", this.value = s, this.path = r, this.type = c, this.errors = [], this.inner = [], Um(i).forEach(((f) => {
      Ht.isError(f) ? (this.errors.push(...f.errors), this.inner.push(...f.inner.length ? f.inner : [f])) : this.errors.push(f);
    })), this.message = this.errors.length > 1 ? `${this.errors.length} errors occurred` : this.errors[0];
  }
};
Hm = Symbol.hasInstance, Ym = Symbol.toStringTag;
let Ht = class Bm extends Error {
  static formatError(i, s) {
    return s = Object.assign({}, s, { path: s.label || s.path || "this", originalPath: s.path }), typeof i == "string" ? i.replace(e_, ((r, c) => On(s[c]))) : typeof i == "function" ? i(s) : i;
  }
  static isError(i) {
    return i && i.name === "ValidationError";
  }
  constructor(i, s, r, c, f) {
    const d = new F0(i, s, r, c);
    if (f) return d;
    super(), this.value = void 0, this.path = void 0, this.type = void 0, this.params = void 0, this.errors = [], this.inner = [], this[Ym] = "Error", this.name = d.name, this.message = d.message, this.type = d.type, this.value = d.value, this.path = d.path, this.errors = d.errors, this.inner = d.inner, Error.captureStackTrace && Error.captureStackTrace(this, Bm);
  }
  static [Hm](i) {
    return F0[Symbol.hasInstance](i) || super[Symbol.hasInstance](i);
  }
}, ma = { default: "${path} is invalid", required: "${path} is a required field", defined: "${path} must be defined", notNull: "${path} cannot be null", oneOf: "${path} must be one of the following values: ${values}", notOneOf: "${path} must not be one of the following values: ${values}", notType: ({ path: n, type: i, value: s, originalValue: r }) => {
  const c = r != null && r !== s ? ` (cast from the value \`${On(r, !0)}\`).` : ".";
  return i !== "mixed" ? `${n} must be a \`${i}\` type, but the final value was: \`${On(s, !0)}\`` + c : `${n} must match the configured type. The validated value was: \`${On(s, !0)}\`` + c;
} }, Dt = { length: "${path} must be exactly ${length} characters", min: "${path} must be at least ${min} characters", max: "${path} must be at most ${max} characters", matches: '${path} must match the following: "${regex}"', email: "${path} must be a valid email", url: "${path} must be a valid URL", uuid: "${path} must be a valid UUID", datetime: "${path} must be a valid ISO date-time", datetime_precision: "${path} must be a valid ISO date-time with a sub-second precision of exactly ${precision} digits", datetime_offset: '${path} must be a valid ISO date-time with UTC "Z" timezone', trim: "${path} must be a trimmed string", lowercase: "${path} must be a lowercase string", uppercase: "${path} must be a upper case string" }, t_ = { min: "${path} must be greater than or equal to ${min}", max: "${path} must be less than or equal to ${max}", lessThan: "${path} must be less than ${less}", moreThan: "${path} must be greater than ${more}", positive: "${path} must be a positive number", negative: "${path} must be a negative number", integer: "${path} must be an integer" }, Fc = { min: "${path} field must be later than ${min}", max: "${path} field must be at earlier than ${max}" }, Vr = { noUnknown: "${path} field has unspecified keys: ${unknown}", exact: "${path} object contains unknown properties: ${properties}" }, a_ = { notType: (n) => {
  const { path: i, value: s, spec: r } = n, c = r.types.length;
  if (Array.isArray(s)) {
    if (s.length < c) return `${i} tuple value has too few items, expected a length of ${c} but got ${s.length} for value: \`${On(s, !0)}\``;
    if (s.length > c) return `${i} tuple value has too many items, expected a length of ${c} but got ${s.length} for value: \`${On(s, !0)}\``;
  }
  return Ht.formatError(ma.notType, n);
} };
Object.assign(/* @__PURE__ */ Object.create(null), { mixed: ma, string: Dt, number: t_, date: Fc, object: Vr, array: { min: "${path} field must have at least ${min} items", max: "${path} field must have less than or equal to ${max} items", length: "${path} must have ${length} items" }, boolean: { isValue: "${path} field must be ${value}" }, tuple: a_ });
const mf = (n) => n && n.__isYupSchema__;
class Ir {
  static fromOptions(i, s) {
    if (!s.then && !s.otherwise) throw new TypeError("either `then:` or `otherwise:` is required for `when()` conditions");
    let { is: r, then: c, otherwise: f } = s, d = typeof r == "function" ? r : (...h) => h.every(((g) => g === r));
    return new Ir(i, ((h, g) => {
      var v;
      let y = d(...h) ? c : f;
      return (v = y == null ? void 0 : y(g)) != null ? v : g;
    }));
  }
  constructor(i, s) {
    this.fn = void 0, this.refs = i, this.refs = i, this.fn = s;
  }
  resolve(i, s) {
    let r = this.refs.map(((f) => f.getValue(s == null ? void 0 : s.value, s == null ? void 0 : s.parent, s == null ? void 0 : s.context))), c = this.fn(r, i, s);
    if (c === void 0 || c === i) return i;
    if (!mf(c)) throw new TypeError("conditions must return a schema object");
    return c.resolve(s);
  }
}
let ui = class {
  constructor(i, s = {}) {
    if (this.key = void 0, this.isContext = void 0, this.isValue = void 0, this.isSibling = void 0, this.path = void 0, this.getter = void 0, this.map = void 0, typeof i != "string") throw new TypeError("ref must be a string, got: " + i);
    if (this.key = i.trim(), i === "") throw new TypeError("ref must be a non-empty string");
    this.isContext = this.key[0] === "$", this.isValue = this.key[0] === ".", this.isSibling = !this.isContext && !this.isValue, this.path = this.key.slice((this.isContext ? "$" : this.isValue ? "." : "").length), this.getter = this.path && ti.getter(this.path, !0), this.map = s.map;
  }
  getValue(i, s, r) {
    let c = this.isContext ? r : this.isValue ? i : s;
    return this.getter && (c = this.getter(c || {})), this.map && (c = this.map(c)), c;
  }
  cast(i, s) {
    return this.getValue(i, s == null ? void 0 : s.parent, s == null ? void 0 : s.context);
  }
  resolve() {
    return this;
  }
  describe() {
    return { type: "ref", key: this.key };
  }
  toString() {
    return `Ref(${this.key})`;
  }
  static isRef(i) {
    return i && i.__isYupRef;
  }
};
ui.prototype.__isYupRef = !0;
const ai = (n) => n == null;
function tl(n) {
  function i({ value: s, path: r = "", options: c, originalValue: f, schema: d }, h, g) {
    const { name: v, test: y, params: m, message: _, skipAbsent: x } = n;
    let { parent: T, context: A, abortEarly: z = d.spec.abortEarly, disableStackTrace: q = d.spec.disableStackTrace } = c;
    const $ = { value: s, parent: T, context: A };
    function j(Se = {}) {
      const oe = Vm(Object.assign({ value: s, originalValue: f, label: d.spec.label, path: Se.path || r, spec: d.spec, disableStackTrace: Se.disableStackTrace || q }, m, Se.params), $), Ye = new Ht(Ht.formatError(Se.message || _, oe), s, oe.path, Se.type || v, oe.disableStackTrace);
      return Ye.params = oe, Ye;
    }
    const L = z ? h : g;
    let V = { path: r, parent: T, type: v, from: c.from, createError: j, resolve: (Se) => Xm(Se, $), options: c, originalValue: f, schema: d };
    const J = (Se) => {
      Ht.isError(Se) ? L(Se) : Se ? g(null) : L(j());
    }, Z = (Se) => {
      Ht.isError(Se) ? L(Se) : h(Se);
    };
    if (x && ai(s)) return J(!0);
    let F;
    try {
      var Te;
      if (F = y.call(V, s, V), typeof ((Te = F) == null ? void 0 : Te.then) == "function") {
        if (c.sync) throw new Error(`Validation test of type: "${V.type}" returned a Promise during a synchronous validate. This test will finish after the validate call has returned`);
        return Promise.resolve(F).then(J, Z);
      }
    } catch (Se) {
      return void Z(Se);
    }
    J(F);
  }
  return i.OPTIONS = n, i;
}
function Vm(n, i) {
  if (!n) return n;
  for (const s of Object.keys(n)) n[s] = Xm(n[s], i);
  return n;
}
function Xm(n, i) {
  return ui.isRef(n) ? n.getValue(i.value, i.parent, i.context) : n;
}
function n_(n, i, s, r = s) {
  let c, f, d;
  return i ? (ti.forEach(i, ((h, g, v) => {
    let y = g ? h.slice(1, h.length - 1) : h, m = (n = n.resolve({ context: r, parent: c, value: s })).type === "tuple", _ = v ? parseInt(y, 10) : 0;
    if (n.innerType || m) {
      if (m && !v) throw new Error(`Yup.reach cannot implicitly index into a tuple type. the path part "${d}" must contain an index to the tuple element, e.g. "${d}[0]"`);
      if (s && _ >= s.length) throw new Error(`Yup.reach cannot resolve an array item at index: ${h}, in the path: ${i}. because there is no value at that index. `);
      c = s, s = s && s[_], n = m ? n.spec.types[_] : n.innerType;
    }
    if (!v) {
      if (!n.fields || !n.fields[y]) throw new Error(`The schema does not contain the path: ${i}. (failed at: ${d} which is a type: "${n.type}")`);
      c = s, s = s && s[y], n = n.fields[y];
    }
    f = y, d = g ? "[" + h + "]" : "." + h;
  })), { schema: n, parent: c, parentPath: f }) : { parent: c, parentPath: i, schema: n };
}
let W0 = class Gm extends Set {
  describe() {
    const i = [];
    for (const s of this.values()) i.push(ui.isRef(s) ? s.describe() : s);
    return i;
  }
  resolveAll(i) {
    let s = [];
    for (const r of this.values()) s.push(i(r));
    return s;
  }
  clone() {
    return new Gm(this.values());
  }
  merge(i, s) {
    const r = this.clone();
    return i.forEach(((c) => r.add(c))), s.forEach(((c) => r.delete(c))), r;
  }
};
function rl(n, i = /* @__PURE__ */ new Map()) {
  if (mf(n) || !n || typeof n != "object") return n;
  if (i.has(n)) return i.get(n);
  let s;
  if (n instanceof Date) s = new Date(n.getTime()), i.set(n, s);
  else if (n instanceof RegExp) s = new RegExp(n), i.set(n, s);
  else if (Array.isArray(n)) {
    s = new Array(n.length), i.set(n, s);
    for (let r = 0; r < n.length; r++) s[r] = rl(n[r], i);
  } else if (n instanceof Map) {
    s = /* @__PURE__ */ new Map(), i.set(n, s);
    for (const [r, c] of n.entries()) s.set(r, rl(c, i));
  } else if (n instanceof Set) {
    s = /* @__PURE__ */ new Set(), i.set(n, s);
    for (const r of n) s.add(rl(r, i));
  } else {
    if (!(n instanceof Object)) throw Error(`Unable to clone ${n}`);
    s = {}, i.set(n, s);
    for (const [r, c] of Object.entries(n)) s[r] = rl(c, i);
  }
  return s;
}
function i_(n) {
  if (n == null || !n.length) return;
  const i = [];
  let s = "", r = !1, c = !1;
  for (let f = 0; f < n.length; f++) {
    const d = n[f];
    d !== "[" || c ? d !== "]" || c ? d !== '"' ? d !== "." || r || c ? s += d : s && (i.push(s), s = "") : c = !c : (s && (/^\d+$/.test(s) ? i.push(s) : i.push(s.replace(/^"|"$/g, "")), s = ""), r = !1) : (s && (i.push(...s.split(".").filter(Boolean)), s = ""), r = !0);
  }
  return s && i.push(...s.split(".").filter(Boolean)), i;
}
function Qm(n, i) {
  var s;
  if (((s = n.inner) == null || !s.length) && n.errors.length) return (function(c, f) {
    const d = f ? `${f}.${c.path}` : c.path;
    return c.errors.map(((h) => ({ message: h, path: i_(d) })));
  })(n, i);
  const r = i ? `${i}.${n.path}` : n.path;
  return n.inner.flatMap(((c) => Qm(c, r)));
}
let ga = class {
  constructor(i) {
    this.type = void 0, this.deps = [], this.tests = void 0, this.transforms = void 0, this.conditions = [], this._mutate = void 0, this.internalTests = {}, this._whitelist = new W0(), this._blacklist = new W0(), this.exclusiveTests = /* @__PURE__ */ Object.create(null), this._typeCheck = void 0, this.spec = void 0, this.tests = [], this.transforms = [], this.withMutation((() => {
      this.typeError(ma.notType);
    })), this.type = i.type, this._typeCheck = i.check, this.spec = Object.assign({ strip: !1, strict: !1, abortEarly: !0, recursive: !0, disableStackTrace: !1, nullable: !1, optional: !0, coerce: !0 }, i == null ? void 0 : i.spec), this.withMutation(((s) => {
      s.nonNullable();
    }));
  }
  get _type() {
    return this.type;
  }
  clone(i) {
    if (this._mutate) return i && Object.assign(this.spec, i), this;
    const s = Object.create(Object.getPrototypeOf(this));
    return s.type = this.type, s._typeCheck = this._typeCheck, s._whitelist = this._whitelist.clone(), s._blacklist = this._blacklist.clone(), s.internalTests = Object.assign({}, this.internalTests), s.exclusiveTests = Object.assign({}, this.exclusiveTests), s.deps = [...this.deps], s.conditions = [...this.conditions], s.tests = [...this.tests], s.transforms = [...this.transforms], s.spec = rl(Object.assign({}, this.spec, i)), s;
  }
  label(i) {
    let s = this.clone();
    return s.spec.label = i, s;
  }
  meta(...i) {
    if (i.length === 0) return this.spec.meta;
    let s = this.clone();
    return s.spec.meta = Object.assign(s.spec.meta || {}, i[0]), s;
  }
  withMutation(i) {
    let s = this._mutate;
    this._mutate = !0;
    let r = i(this);
    return this._mutate = s, r;
  }
  concat(i) {
    if (!i || i === this) return this;
    if (i.type !== this.type && this.type !== "mixed") throw new TypeError(`You cannot \`concat()\` schema's of different types: ${this.type} and ${i.type}`);
    let s = this, r = i.clone();
    const c = Object.assign({}, s.spec, r.spec);
    return r.spec = c, r.internalTests = Object.assign({}, s.internalTests, r.internalTests), r._whitelist = s._whitelist.merge(i._whitelist, i._blacklist), r._blacklist = s._blacklist.merge(i._blacklist, i._whitelist), r.tests = s.tests, r.exclusiveTests = s.exclusiveTests, r.withMutation(((f) => {
      i.tests.forEach(((d) => {
        f.test(d.OPTIONS);
      }));
    })), r.transforms = [...s.transforms, ...r.transforms], r;
  }
  isType(i) {
    return i == null ? !(!this.spec.nullable || i !== null) || !(!this.spec.optional || i !== void 0) : this._typeCheck(i);
  }
  resolve(i) {
    let s = this;
    if (s.conditions.length) {
      let r = s.conditions;
      s = s.clone(), s.conditions = [], s = r.reduce(((c, f) => f.resolve(c, i)), s), s = s.resolve(i);
    }
    return s;
  }
  resolveOptions(i) {
    var s, r, c, f;
    return Object.assign({}, i, { from: i.from || [], strict: (s = i.strict) != null ? s : this.spec.strict, abortEarly: (r = i.abortEarly) != null ? r : this.spec.abortEarly, recursive: (c = i.recursive) != null ? c : this.spec.recursive, disableStackTrace: (f = i.disableStackTrace) != null ? f : this.spec.disableStackTrace });
  }
  cast(i, s = {}) {
    let r = this.resolve(Object.assign({ value: i }, s)), c = s.assert === "ignore-optionality", f = r._cast(i, s);
    if (s.assert !== !1 && !r.isType(f)) {
      if (c && ai(f)) return f;
      let d = On(i), h = On(f);
      throw new TypeError(`The value of ${s.path || "field"} could not be cast to a value that satisfies the schema type: "${r.type}". 

attempted value: ${d} 
` + (h !== d ? `result of cast: ${h}` : ""));
    }
    return f;
  }
  _cast(i, s) {
    let r = i === void 0 ? i : this.transforms.reduce(((c, f) => f.call(this, c, i, this)), i);
    return r === void 0 && (r = this.getDefault(s)), r;
  }
  _validate(i, s = {}, r, c) {
    let { path: f, originalValue: d = i, strict: h = this.spec.strict } = s, g = i;
    h || (g = this._cast(g, Object.assign({ assert: !1 }, s)));
    let v = [];
    for (let y of Object.values(this.internalTests)) y && v.push(y);
    this.runTests({ path: f, value: g, originalValue: d, options: s, tests: v }, r, ((y) => {
      if (y.length) return c(y, g);
      this.runTests({ path: f, value: g, originalValue: d, options: s, tests: this.tests }, r, c);
    }));
  }
  runTests(i, s, r) {
    let c = !1, { tests: f, value: d, originalValue: h, path: g, options: v } = i, y = (A) => {
      c || (c = !0, s(A, d));
    }, m = (A) => {
      c || (c = !0, r(A, d));
    }, _ = f.length, x = [];
    if (!_) return m([]);
    let T = { value: d, originalValue: h, path: g, options: v, schema: this };
    for (let A = 0; A < f.length; A++) (0, f[A])(T, y, (function(z) {
      z && (Array.isArray(z) ? x.push(...z) : x.push(z)), --_ <= 0 && m(x);
    }));
  }
  asNestedTest({ key: i, index: s, parent: r, parentPath: c, originalParent: f, options: d }) {
    const h = i ?? s;
    if (h == null) throw TypeError("Must include `key` or `index` for nested validations");
    const g = typeof h == "number";
    let v = r[h];
    const y = Object.assign({}, d, { strict: !0, parent: r, value: v, originalValue: f[h], key: void 0, [g ? "index" : "key"]: h, path: g || h.includes(".") ? `${c || ""}[${g ? h : `"${h}"`}]` : (c ? `${c}.` : "") + i });
    return (m, _, x) => this.resolve(y)._validate(v, y, _, x);
  }
  validate(i, s) {
    var r;
    let c = this.resolve(Object.assign({}, s, { value: i })), f = (r = s == null ? void 0 : s.disableStackTrace) != null ? r : c.spec.disableStackTrace;
    return new Promise(((d, h) => c._validate(i, s, ((g, v) => {
      Ht.isError(g) && (g.value = v), h(g);
    }), ((g, v) => {
      g.length ? h(new Ht(g, v, void 0, void 0, f)) : d(v);
    }))));
  }
  validateSync(i, s) {
    var r;
    let c, f = this.resolve(Object.assign({}, s, { value: i })), d = (r = s == null ? void 0 : s.disableStackTrace) != null ? r : f.spec.disableStackTrace;
    return f._validate(i, Object.assign({}, s, { sync: !0 }), ((h, g) => {
      throw Ht.isError(h) && (h.value = g), h;
    }), ((h, g) => {
      if (h.length) throw new Ht(h, i, void 0, void 0, d);
      c = g;
    })), c;
  }
  isValid(i, s) {
    return this.validate(i, s).then((() => !0), ((r) => {
      if (Ht.isError(r)) return !1;
      throw r;
    }));
  }
  isValidSync(i, s) {
    try {
      return this.validateSync(i, s), !0;
    } catch (r) {
      if (Ht.isError(r)) return !1;
      throw r;
    }
  }
  _getDefault(i) {
    let s = this.spec.default;
    return s == null ? s : typeof s == "function" ? s.call(this, i) : rl(s);
  }
  getDefault(i) {
    return this.resolve(i || {})._getDefault(i);
  }
  default(i) {
    return arguments.length === 0 ? this._getDefault() : this.clone({ default: i });
  }
  strict(i = !0) {
    return this.clone({ strict: i });
  }
  nullability(i, s) {
    const r = this.clone({ nullable: i });
    return r.internalTests.nullable = tl({ message: s, name: "nullable", test(c) {
      return c !== null || this.schema.spec.nullable;
    } }), r;
  }
  optionality(i, s) {
    const r = this.clone({ optional: i });
    return r.internalTests.optionality = tl({ message: s, name: "optionality", test(c) {
      return c !== void 0 || this.schema.spec.optional;
    } }), r;
  }
  optional() {
    return this.optionality(!0);
  }
  defined(i = ma.defined) {
    return this.optionality(!1, i);
  }
  nullable() {
    return this.nullability(!0);
  }
  nonNullable(i = ma.notNull) {
    return this.nullability(!1, i);
  }
  required(i = ma.required) {
    return this.clone().withMutation(((s) => s.nonNullable(i).defined(i)));
  }
  notRequired() {
    return this.clone().withMutation(((i) => i.nullable().optional()));
  }
  transform(i) {
    let s = this.clone();
    return s.transforms.push(i), s;
  }
  test(...i) {
    let s;
    if (s = i.length === 1 ? typeof i[0] == "function" ? { test: i[0] } : i[0] : i.length === 2 ? { name: i[0], test: i[1] } : { name: i[0], message: i[1], test: i[2] }, s.message === void 0 && (s.message = ma.default), typeof s.test != "function") throw new TypeError("`test` is a required parameters");
    let r = this.clone(), c = tl(s), f = s.exclusive || s.name && r.exclusiveTests[s.name] === !0;
    if (s.exclusive && !s.name) throw new TypeError("Exclusive tests must provide a unique `name` identifying the test");
    return s.name && (r.exclusiveTests[s.name] = !!s.exclusive), r.tests = r.tests.filter(((d) => !(d.OPTIONS.name === s.name && (f || d.OPTIONS.test === c.OPTIONS.test)))), r.tests.push(c), r;
  }
  when(i, s) {
    Array.isArray(i) || typeof i == "string" || (s = i, i = ".");
    let r = this.clone(), c = Um(i).map(((f) => new ui(f)));
    return c.forEach(((f) => {
      f.isSibling && r.deps.push(f.key);
    })), r.conditions.push(typeof s == "function" ? new Ir(c, s) : Ir.fromOptions(c, s)), r;
  }
  typeError(i) {
    let s = this.clone();
    return s.internalTests.typeError = tl({ message: i, name: "typeError", skipAbsent: !0, test(r) {
      return !!this.schema._typeCheck(r) || this.createError({ params: { type: this.schema.type } });
    } }), s;
  }
  oneOf(i, s = ma.oneOf) {
    let r = this.clone();
    return i.forEach(((c) => {
      r._whitelist.add(c), r._blacklist.delete(c);
    })), r.internalTests.whiteList = tl({ message: s, name: "oneOf", skipAbsent: !0, test(c) {
      let f = this.schema._whitelist, d = f.resolveAll(this.resolve);
      return !!d.includes(c) || this.createError({ params: { values: Array.from(f).join(", "), resolved: d } });
    } }), r;
  }
  notOneOf(i, s = ma.notOneOf) {
    let r = this.clone();
    return i.forEach(((c) => {
      r._blacklist.add(c), r._whitelist.delete(c);
    })), r.internalTests.blacklist = tl({ message: s, name: "notOneOf", test(c) {
      let f = this.schema._blacklist, d = f.resolveAll(this.resolve);
      return !d.includes(c) || this.createError({ params: { values: Array.from(f).join(", "), resolved: d } });
    } }), r;
  }
  strip(i = !0) {
    let s = this.clone();
    return s.spec.strip = i, s;
  }
  describe(i) {
    const s = (i ? this.resolve(i) : this).clone(), { label: r, meta: c, optional: f, nullable: d } = s.spec;
    return { meta: c, label: r, optional: f, nullable: d, default: s.getDefault(i), type: s.type, oneOf: s._whitelist.describe(), notOneOf: s._blacklist.describe(), tests: s.tests.filter(((h, g, v) => v.findIndex(((y) => y.OPTIONS.name === h.OPTIONS.name)) === g)).map(((h) => {
      const g = h.OPTIONS.params && i ? Vm(Object.assign({}, h.OPTIONS.params), i) : h.OPTIONS.params;
      return { name: h.OPTIONS.name, params: g };
    })) };
  }
  get "~standard"() {
    const i = this;
    return { version: 1, vendor: "yup", async validate(s) {
      try {
        return { value: await i.validate(s, { abortEarly: !1 }) };
      } catch (r) {
        if (r instanceof Ht) return { issues: Qm(r) };
        throw r;
      }
    } };
  }
};
ga.prototype.__isYupSchema__ = !0;
for (const n of ["validate", "validateSync"]) ga.prototype[`${n}At`] = function(i, s, r = {}) {
  const { parent: c, parentPath: f, schema: d } = n_(this, i, s, r.context);
  return d[n](c && c[f], Object.assign({}, r, { parent: c, path: i }));
};
for (const n of ["equals", "is"]) ga.prototype[n] = ga.prototype.oneOf;
for (const n of ["not", "nope"]) ga.prototype[n] = ga.prototype.notOneOf;
const l_ = /^(\d{4}|[+-]\d{6})(?:-?(\d{2})(?:-?(\d{2}))?)?(?:[ T]?(\d{2}):?(\d{2})(?::?(\d{2})(?:[,.](\d{1,}))?)?(?:(Z)|([+-])(\d{2})(?::?(\d{2}))?)?)?$/;
function Wc(n) {
  var i, s;
  const r = l_.exec(n);
  return r ? { year: Ha(r[1]), month: Ha(r[2], 1) - 1, day: Ha(r[3], 1), hour: Ha(r[4]), minute: Ha(r[5]), second: Ha(r[6]), millisecond: r[7] ? Ha(r[7].substring(0, 3)) : 0, precision: (i = (s = r[7]) == null ? void 0 : s.length) != null ? i : void 0, z: r[8] || void 0, plusMinus: r[9] || void 0, hourOffset: Ha(r[10]), minuteOffset: Ha(r[11]) } : null;
}
function Ha(n, i = 0) {
  return Number(n) || i;
}
let s_ = /^[a-zA-Z0-9.!#$%&'*+\/=?^_`{|}~-]+@[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?(?:\.[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?)*$/, r_ = /^((https?|ftp):)?\/\/(((([a-z]|\d|-|\.|_|~|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])|(%[\da-f]{2})|[!\$&'\(\)\*\+,;=]|:)*@)?(((\d|[1-9]\d|1\d\d|2[0-4]\d|25[0-5])\.(\d|[1-9]\d|1\d\d|2[0-4]\d|25[0-5])\.(\d|[1-9]\d|1\d\d|2[0-4]\d|25[0-5])\.(\d|[1-9]\d|1\d\d|2[0-4]\d|25[0-5]))|((([a-z]|\d|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])|(([a-z]|\d|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])([a-z]|\d|-|\.|_|~|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])*([a-z]|\d|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])))\.)+(([a-z]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])|(([a-z]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])([a-z]|\d|-|\.|_|~|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])*([a-z]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])))\.?)(:\d*)?)(\/((([a-z]|\d|-|\.|_|~|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])|(%[\da-f]{2})|[!\$&'\(\)\*\+,;=]|:|@)+(\/(([a-z]|\d|-|\.|_|~|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])|(%[\da-f]{2})|[!\$&'\(\)\*\+,;=]|:|@)*)*)?)?(\?((([a-z]|\d|-|\.|_|~|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])|(%[\da-f]{2})|[!\$&'\(\)\*\+,;=]|:|@)|[\uE000-\uF8FF]|\/|\?)*)?(\#((([a-z]|\d|-|\.|_|~|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])|(%[\da-f]{2})|[!\$&'\(\)\*\+,;=]|:|@)|\/|\?)*)?$/i, o_ = /^(?:[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}|00000000-0000-0000-0000-000000000000)$/i, u_ = new RegExp("^\\d{4}-\\d{2}-\\d{2}T\\d{2}:\\d{2}:\\d{2}(\\.\\d+)?(([+-]\\d{2}(:?\\d{2})?)|Z)$"), c_ = (n) => ai(n) || n === n.trim(), f_ = {}.toString();
function vf() {
  return new Zm();
}
class Zm extends ga {
  constructor() {
    super({ type: "string", check: (i) => (i instanceof String && (i = i.valueOf()), typeof i == "string") }), this.withMutation((() => {
      this.transform(((i, s, r) => {
        if (!r.spec.coerce || r.isType(i) || Array.isArray(i)) return i;
        const c = i != null && i.toString ? i.toString() : i;
        return c === f_ ? i : c;
      }));
    }));
  }
  required(i) {
    return super.required(i).withMutation(((s) => s.test({ message: i || ma.required, name: "required", skipAbsent: !0, test: (r) => !!r.length })));
  }
  notRequired() {
    return super.notRequired().withMutation(((i) => (i.tests = i.tests.filter(((s) => s.OPTIONS.name !== "required")), i)));
  }
  length(i, s = Dt.length) {
    return this.test({ message: s, name: "length", exclusive: !0, params: { length: i }, skipAbsent: !0, test(r) {
      return r.length === this.resolve(i);
    } });
  }
  min(i, s = Dt.min) {
    return this.test({ message: s, name: "min", exclusive: !0, params: { min: i }, skipAbsent: !0, test(r) {
      return r.length >= this.resolve(i);
    } });
  }
  max(i, s = Dt.max) {
    return this.test({ name: "max", exclusive: !0, message: s, params: { max: i }, skipAbsent: !0, test(r) {
      return r.length <= this.resolve(i);
    } });
  }
  matches(i, s) {
    let r, c, f = !1;
    return s && (typeof s == "object" ? { excludeEmptyString: f = !1, message: r, name: c } = s : r = s), this.test({ name: c || "matches", message: r || Dt.matches, params: { regex: i }, skipAbsent: !0, test: (d) => d === "" && f || d.search(i) !== -1 });
  }
  email(i = Dt.email) {
    return this.matches(s_, { name: "email", message: i, excludeEmptyString: !0 });
  }
  url(i = Dt.url) {
    return this.matches(r_, { name: "url", message: i, excludeEmptyString: !0 });
  }
  uuid(i = Dt.uuid) {
    return this.matches(o_, { name: "uuid", message: i, excludeEmptyString: !1 });
  }
  datetime(i) {
    let s, r, c = "";
    return i && (typeof i == "object" ? { message: c = "", allowOffset: s = !1, precision: r } = i : c = i), this.matches(u_, { name: "datetime", message: c || Dt.datetime, excludeEmptyString: !0 }).test({ name: "datetime_offset", message: c || Dt.datetime_offset, params: { allowOffset: s }, skipAbsent: !0, test: (f) => {
      if (!f || s) return !0;
      const d = Wc(f);
      return !!d && !!d.z;
    } }).test({ name: "datetime_precision", message: c || Dt.datetime_precision, params: { precision: r }, skipAbsent: !0, test: (f) => {
      if (!f || r == null) return !0;
      const d = Wc(f);
      return !!d && d.precision === r;
    } });
  }
  ensure() {
    return this.default("").transform(((i) => i === null ? "" : i));
  }
  trim(i = Dt.trim) {
    return this.transform(((s) => s != null ? s.trim() : s)).test({ message: i, name: "trim", test: c_ });
  }
  lowercase(i = Dt.lowercase) {
    return this.transform(((s) => ai(s) ? s : s.toLowerCase())).test({ message: i, name: "string_case", exclusive: !0, skipAbsent: !0, test: (s) => ai(s) || s === s.toLowerCase() });
  }
  uppercase(i = Dt.uppercase) {
    return this.transform(((s) => ai(s) ? s : s.toUpperCase())).test({ message: i, name: "string_case", exclusive: !0, skipAbsent: !0, test: (s) => ai(s) || s === s.toUpperCase() });
  }
}
vf.prototype = Zm.prototype;
let d_ = /* @__PURE__ */ new Date("");
class gf extends ga {
  constructor() {
    super({ type: "date", check: (i) => Object.prototype.toString.call(i) === "[object Date]" && !isNaN(i.getTime()) }), this.withMutation((() => {
      this.transform(((i, s, r) => !r.spec.coerce || r.isType(i) || i === null ? i : (i = (function(c) {
        const f = Wc(c);
        if (!f) return Date.parse ? Date.parse(c) : Number.NaN;
        if (f.z === void 0 && f.plusMinus === void 0) return new Date(f.year, f.month, f.day, f.hour, f.minute, f.second, f.millisecond).valueOf();
        let d = 0;
        return f.z !== "Z" && f.plusMinus !== void 0 && (d = 60 * f.hourOffset + f.minuteOffset, f.plusMinus === "+" && (d = 0 - d)), Date.UTC(f.year, f.month, f.day, f.hour, f.minute + d, f.second, f.millisecond);
      })(i), isNaN(i) ? gf.INVALID_DATE : new Date(i))));
    }));
  }
  prepareParam(i, s) {
    let r;
    if (ui.isRef(i)) r = i;
    else {
      let c = this.cast(i);
      if (!this._typeCheck(c)) throw new TypeError(`\`${s}\` must be a Date or a value that can be \`cast()\` to a Date`);
      r = c;
    }
    return r;
  }
  min(i, s = Fc.min) {
    let r = this.prepareParam(i, "min");
    return this.test({ message: s, name: "min", exclusive: !0, params: { min: i }, skipAbsent: !0, test(c) {
      return c >= this.resolve(r);
    } });
  }
  max(i, s = Fc.max) {
    let r = this.prepareParam(i, "max");
    return this.test({ message: s, name: "max", exclusive: !0, params: { max: i }, skipAbsent: !0, test(c) {
      return c <= this.resolve(r);
    } });
  }
}
function I0(n, i) {
  let s = 1 / 0;
  return n.some(((r, c) => {
    var f;
    if ((f = i.path) != null && f.includes(r)) return s = c, !0;
  })), s;
}
function Km(n) {
  return (i, s) => I0(n, i) - I0(n, s);
}
gf.INVALID_DATE = d_;
const p_ = (n, i, s) => {
  if (typeof n != "string") return n;
  let r = n;
  try {
    r = JSON.parse(n);
  } catch {
  }
  return s.isType(r) ? r : n;
};
function Xr(n) {
  if ("fields" in n) {
    const i = {};
    for (const [s, r] of Object.entries(n.fields)) i[s] = Xr(r);
    return n.setFields(i);
  }
  if (n.type === "array") {
    const i = n.optional();
    return i.innerType && (i.innerType = Xr(i.innerType)), i;
  }
  return n.type === "tuple" ? n.optional().clone({ types: n.spec.types.map(Xr) }) : "optional" in n ? n.optional() : n;
}
let P0 = (n) => Object.prototype.toString.call(n) === "[object Object]";
function em(n, i) {
  let s = Object.keys(n.fields);
  return Object.keys(i).filter(((r) => s.indexOf(r) === -1));
}
const h_ = Km([]);
function _s(n) {
  return new Jm(n);
}
let Jm = class extends ga {
  constructor(i) {
    super({ type: "object", check: (s) => P0(s) || typeof s == "function" }), this.fields = /* @__PURE__ */ Object.create(null), this._sortErrors = h_, this._nodes = [], this._excludedEdges = [], this.withMutation((() => {
      i && this.shape(i);
    }));
  }
  _cast(i, s = {}) {
    var r;
    let c = super._cast(i, s);
    if (c === void 0) return this.getDefault(s);
    if (!this._typeCheck(c)) return c;
    let f = this.fields, d = (r = s.stripUnknown) != null ? r : this.spec.noUnknown, h = [].concat(this._nodes, Object.keys(c).filter(((m) => !this._nodes.includes(m)))), g = {}, v = Object.assign({}, s, { parent: g, __validating: s.__validating || !1 }), y = !1;
    for (const m of h) {
      let _ = f[m], x = m in c;
      if (_) {
        let T, A = c[m];
        v.path = (s.path ? `${s.path}.` : "") + m, _ = _.resolve({ value: A, context: s.context, parent: g });
        let z = _ instanceof ga ? _.spec : void 0, q = z == null ? void 0 : z.strict;
        if (z != null && z.strip) {
          y = y || m in c;
          continue;
        }
        T = s.__validating && q ? c[m] : _.cast(c[m], v), T !== void 0 && (g[m] = T);
      } else x && !d && (g[m] = c[m]);
      x === m in g && g[m] === c[m] || (y = !0);
    }
    return y ? g : c;
  }
  _validate(i, s = {}, r, c) {
    let { from: f = [], originalValue: d = i, recursive: h = this.spec.recursive } = s;
    s.from = [{ schema: this, value: d }, ...f], s.__validating = !0, s.originalValue = d, super._validate(i, s, r, ((g, v) => {
      if (!h || !P0(v)) return void c(g, v);
      d = d || v;
      let y = [];
      for (let m of this._nodes) {
        let _ = this.fields[m];
        _ && !ui.isRef(_) && y.push(_.asNestedTest({ options: s, key: m, parent: v, parentPath: s.path, originalParent: d }));
      }
      this.runTests({ tests: y, value: v, originalValue: d, options: s }, r, ((m) => {
        c(m.sort(this._sortErrors).concat(g), v);
      }));
    }));
  }
  clone(i) {
    const s = super.clone(i);
    return s.fields = Object.assign({}, this.fields), s._nodes = this._nodes, s._excludedEdges = this._excludedEdges, s._sortErrors = this._sortErrors, s;
  }
  concat(i) {
    let s = super.concat(i), r = s.fields;
    for (let [c, f] of Object.entries(this.fields)) {
      const d = r[c];
      r[c] = d === void 0 ? f : d;
    }
    return s.withMutation(((c) => c.setFields(r, [...this._excludedEdges, ...i._excludedEdges])));
  }
  _getDefault(i) {
    if ("default" in this.spec) return super._getDefault(i);
    if (!this._nodes.length) return;
    let s = {};
    return this._nodes.forEach(((r) => {
      var c;
      const f = this.fields[r];
      let d = i;
      (c = d) != null && c.value && (d = Object.assign({}, d, { parent: d.value, value: d.value[r] })), s[r] = f && "getDefault" in f ? f.getDefault(d) : void 0;
    })), s;
  }
  setFields(i, s) {
    let r = this.clone();
    return r.fields = i, r._nodes = (function(c, f = []) {
      let d = [], h = /* @__PURE__ */ new Set(), g = new Set(f.map((([y, m]) => `${y}-${m}`)));
      function v(y, m) {
        let _ = ti.split(y)[0];
        h.add(_), g.has(`${m}-${_}`) || d.push([m, _]);
      }
      for (const y of Object.keys(c)) {
        let m = c[y];
        h.add(y), ui.isRef(m) && m.isSibling ? v(m.path, y) : mf(m) && "deps" in m && m.deps.forEach(((_) => v(_, y)));
      }
      return Ky.array(Array.from(h), d).reverse();
    })(i, s), r._sortErrors = Km(Object.keys(i)), s && (r._excludedEdges = s), r;
  }
  shape(i, s = []) {
    return this.clone().withMutation(((r) => {
      let c = r._excludedEdges;
      return s.length && (Array.isArray(s[0]) || (s = [s]), c = [...r._excludedEdges, ...s]), r.setFields(Object.assign(r.fields, i), c);
    }));
  }
  partial() {
    const i = {};
    for (const [s, r] of Object.entries(this.fields)) i[s] = "optional" in r && r.optional instanceof Function ? r.optional() : r;
    return this.setFields(i);
  }
  deepPartial() {
    return Xr(this);
  }
  pick(i) {
    const s = {};
    for (const r of i) this.fields[r] && (s[r] = this.fields[r]);
    return this.setFields(s, this._excludedEdges.filter((([r, c]) => i.includes(r) && i.includes(c))));
  }
  omit(i) {
    const s = [];
    for (const r of Object.keys(this.fields)) i.includes(r) || s.push(r);
    return this.pick(s);
  }
  from(i, s, r) {
    let c = ti.getter(i, !0);
    return this.transform(((f) => {
      if (!f) return f;
      let d = f;
      return ((h, g) => {
        const v = [...ti.normalizePath(g)];
        if (v.length === 1) return v[0] in h;
        let y = v.pop(), m = ti.getter(ti.join(v), !0)(h);
        return !(!m || !(y in m));
      })(f, i) && (d = Object.assign({}, f), r || delete d[i], d[s] = c(f)), d;
    }));
  }
  json() {
    return this.transform(p_);
  }
  exact(i) {
    return this.test({ name: "exact", exclusive: !0, message: i || Vr.exact, test(s) {
      if (s == null) return !0;
      const r = em(this.schema, s);
      return r.length === 0 || this.createError({ params: { properties: r.join(", ") } });
    } });
  }
  stripUnknown() {
    return this.clone({ noUnknown: !0 });
  }
  noUnknown(i = !0, s = Vr.noUnknown) {
    typeof i != "boolean" && (s = i, i = !0);
    let r = this.test({ name: "noUnknown", exclusive: !0, message: s, test(c) {
      if (c == null) return !0;
      const f = em(this.schema, c);
      return !i || f.length === 0 || this.createError({ params: { unknown: f.join(", ") } });
    } });
    return r.spec.noUnknown = i, r;
  }
  unknown(i = !0, s = Vr.noUnknown) {
    return this.noUnknown(!i, s);
  }
  transformKeys(i) {
    return this.transform(((s) => {
      if (!s) return s;
      const r = {};
      for (const c of Object.keys(s)) r[i(c)] = s[c];
      return r;
    }));
  }
  camelCase() {
    return this.transformKeys(Dc.camelCase);
  }
  snakeCase() {
    return this.transformKeys(Dc.snakeCase);
  }
  constantCase() {
    return this.transformKeys(((i) => Dc.snakeCase(i).toUpperCase()));
  }
  describe(i) {
    const s = (i ? this.resolve(i) : this).clone(), r = super.describe(i);
    r.fields = {};
    for (const [f, d] of Object.entries(s.fields)) {
      var c;
      let h = i;
      (c = h) != null && c.value && (h = Object.assign({}, h, { parent: h.value, value: h.value[f] })), r.fields[f] = d.describe(h);
    }
    return r;
  }
};
_s.prototype = Jm.prototype;
/*!
 * Built by ShiftSoftware
 * Copyright (c)
 */
function no(n) {
  const i = n.reduce(((s, r) => (s[r] = vf().required().default(""), s)), {});
  return _s(i);
}
const Fm = no(["lang", "direction", "language", "noData", "noRecords", "notInRecords"]), Wm = no(["noBaseUrl", "invalidVin", "vinNumberRequired", "partNumberRequired", "wrongResponseFormat", "noPartsFound", "noServiceAvailable", "wrongFormStructure", "wildCard", "requestFailedPleaseTryAgainLater"]), m_ = no(["wildCard"]), v_ = _s({ errors: m_ }).concat(no(["reCaptchaIsRequired", "noSelectOptions", "close", "inputValueIsIncorrect", "submit"]));
_s({ errors: Wm }).concat(Fm);
_s({}).concat(Fm).concat(v_).concat(Wm);
/*!
 * Built by ShiftSoftware
 * Copyright (c)
 */
function tm(n, i) {
  if (i) return i.split(".").reduce(((s, r) => s == null ? void 0 : s[r]), n);
}
/*!
 * Built by ShiftSoftware
 * Copyright (c)
 */
const Im = (n) => `${n}-label`, g_ = (n) => `${n}-format`, b_ = (n) => `${n}-require`, y_ = (n) => `${n}-size`, __ = (n) => `${n}-max`, x_ = (n) => `${n}-upload`, Pm = (n) => `${n}-placeholder`, w_ = (n) => ({ label: Im(n), placeholder: Pm(n) }), k_ = (n) => `$${n}Required`, Xa = { label: Im, format: g_, require: b_, condition: k_, placeholder: Pm, meta: w_, size: y_, max: __, upload: x_ }, S_ = (n, i, s) => {
  var r, c, f, d, h, g, v, y, m, _, x, T, A, z, q, $, j, L, V, J;
  const [Z, F] = (c = (r = n == null ? void 0 : n.form) === null || r === void 0 ? void 0 : r.getFormLocale()) !== null && c !== void 0 ? c : [], Te = ((d = (f = n == null ? void 0 : n.localization) === null || f === void 0 ? void 0 : f[F]) === null || d === void 0 ? void 0 : d.label) || tm(Z, i == null ? void 0 : i.label) || (i == null ? void 0 : i.label), Se = ((g = (h = n == null ? void 0 : n.localization) === null || h === void 0 ? void 0 : h[F]) === null || g === void 0 ? void 0 : g.placeholder) || tm(Z, i == null ? void 0 : i.placeholder) || (i == null ? void 0 : i.placeholder);
  let oe = "";
  return s != null && s.endsWith(Xa.format("")) && (oe = (y = (v = n == null ? void 0 : n.localization) === null || v === void 0 ? void 0 : v[F]) === null || y === void 0 ? void 0 : y.format), s != null && s.endsWith(Xa.size("")) ? oe = (_ = (m = n == null ? void 0 : n.localization) === null || m === void 0 ? void 0 : m[F]) === null || _ === void 0 ? void 0 : _.size : s != null && s.endsWith(Xa.upload("")) ? oe = (T = (x = n == null ? void 0 : n.localization) === null || x === void 0 ? void 0 : x[F]) === null || T === void 0 ? void 0 : T.failure : s != null && s.endsWith(Xa.require("")) ? oe = (z = (A = n == null ? void 0 : n.localization) === null || A === void 0 ? void 0 : A[F]) === null || z === void 0 ? void 0 : z.require : s === "minMessage" ? oe = n == null ? void 0 : n.withSlots((($ = (q = n == null ? void 0 : n.localization) === null || q === void 0 ? void 0 : q[F]) === null || $ === void 0 ? void 0 : $.minMessage) || "Min date is $minDate$") : s === "maxMessage" ? oe = n == null ? void 0 : n.withSlots(((L = (j = n == null ? void 0 : n.localization) === null || j === void 0 ? void 0 : j[F]) === null || L === void 0 ? void 0 : L.maxMessage) || "Max date is $maxDate$") : s === "betweenMessage" && (oe = n == null ? void 0 : n.withSlots(((J = (V = n == null ? void 0 : n.localization) === null || V === void 0 ? void 0 : V[F]) === null || J === void 0 ? void 0 : J.betweenMessage) || "Must be between $minDate$ and $maxDate$")), { label: Te, placeholder: Se, errorTextMessage: oe || Z[s] || s };
};
/*!
 * Built by ShiftSoftware
 * Copyright (c)
 */
async function am(n, ...i) {
  var s;
  if (n) {
    if (typeof n != "string") return await n(...i);
    if (this != null && this.blazorRef) return await ((s = this.blazorRef) === null || s === void 0 ? void 0 : s.invokeMethodAsync(n, ...i));
  }
}
/*!
 * Built by ShiftSoftware
 * Copyright (c)
 */
const ev = (n) => X("svg", Object.assign({ fill: "none" }, n, { "stroke-width": "2", viewBox: "0 0 24 24", stroke: "currentColor", "stroke-linecap": "round", "stroke-linejoin": "round", xmlns: "http://www.w3.org/2000/svg" }), X("path", { d: "m15 18-6-6 6-6" })), z_ = (n) => X("svg", Object.assign({ fill: "none" }, n, { "stroke-width": "2", viewBox: "0 0 24 24", stroke: "currentColor", "stroke-linecap": "round", "stroke-linejoin": "round", xmlns: "http://www.w3.org/2000/svg" }), X("path", { d: "m9 18 6-6-6-6" }));
/*!
 * Built by ShiftSoftware
 * Copyright (c)
 */
const T_ = /^(\d{4})-(\d{2})-(\d{2})$/, E_ = /^(\d{4})-(\d{2})$/, li = (n, i = 2) => String(n).padStart(i, "0");
function io(n, i) {
  return i === 2 ? ((s) => s % 4 == 0 && s % 100 != 0 || s % 400 == 0)(n) ? 29 : 28 : [4, 6, 9, 11].includes(i) ? 30 : 31;
}
function ci(n, i, s) {
  return `${li(n, 4)}-${li(i)}-${li(s)}`;
}
function xt(n) {
  if (typeof n != "string") return null;
  const i = T_.exec(n.trim());
  if (!i) return null;
  const [s, r, c] = i.slice(1).map(Number);
  return s < 1 || r < 1 || r > 12 || c < 1 || c > io(s, r) ? null : ci(s, r, c);
}
function nm(n) {
  if (typeof n != "string") return null;
  const i = E_.exec(n.trim());
  if (!i) return null;
  const [s, r] = i.slice(1).map(Number);
  return s < 1 || r < 1 || r > 12 ? null : `${li(s, 4)}-${li(r)}`;
}
function Dn(n) {
  return n.split("-").map(Number);
}
function bt(n) {
  return n.split("-").map(Number);
}
function bf(n) {
  const [i, s, r] = Dn(n), c = s <= 2 ? i - 1 : i, f = Math.floor(c / 400), d = c - 400 * f, h = Math.floor((153 * (s + (s > 2 ? -3 : 9)) + 2) / 5) + r - 1;
  return 146097 * f + (365 * d + Math.floor(d / 4) - Math.floor(d / 100) + h) - 719468;
}
function tv(n) {
  const i = n + 719468, s = Math.floor(i / 146097), r = i - 146097 * s, c = Math.floor((r - Math.floor(r / 1460) + Math.floor(r / 36524) - Math.floor(r / 146096)) / 365), f = r - (365 * c + Math.floor(c / 4) - Math.floor(c / 100)), d = Math.floor((5 * f + 2) / 153), h = d < 10 ? d + 3 : d - 9;
  return ci(c + 400 * s + (h <= 2 ? 1 : 0), h, f - Math.floor((153 * d + 2) / 5) + 1);
}
const vs = (n) => ((bf(n) + 4) % 7 + 7) % 7, ni = (n, i) => tv(bf(n) + i), Le = (n) => n.slice(0, 7);
function av(n, i) {
  const [s, r] = bt(n), c = 12 * s + (r - 1) + i;
  return `${li(Math.floor(c / 12), 4)}-${li(c % 12 + 1)}`;
}
function Ic(n, i) {
  const [, , s] = Dn(n), [r, c] = bt(av(Le(n), i));
  return ci(r, c, Math.min(s, io(r, c)));
}
const im = (n, i) => Ic(n, 12 * i), il = (n) => `${n}-01`;
function Gr(n) {
  const [i, s] = bt(n);
  return ci(i, s, io(i, s));
}
const A_ = (n, i) => {
  const s = typeof n == "string" && n.trim() !== "" ? Number(n) : n;
  return typeof s == "number" && Number.isInteger(s) ? (s % 7 + 7) % 7 : i;
};
function yf(n, i) {
  return ni(n, -(vs(n) - i + 7) % 7);
}
const O_ = (n, i) => ni(yf(n, i), 6);
function D_(n, i) {
  const s = bf(yf(il(n), i));
  return Array.from({ length: 42 }, ((r, c) => {
    const f = tv(s + c);
    return { date: f, inMonth: Le(f) === n };
  }));
}
function lm(n, i, s) {
  return i && n < i ? i : s && n > s ? s : n;
}
function C_(n, i, s) {
  return i && n < Le(i) ? Le(i) : s && n > Le(s) ? Le(s) : n;
}
const nv = (n = /* @__PURE__ */ new Date()) => ci(n.getFullYear(), n.getMonth() + 1, n.getDate()), sm = { en: { language: "en", direction: "ltr", weekStartsOn: 1, numerals: "latn", strings: { calendar: "Calendar", previousMonth: "Previous month", nextMonth: "Next month", today: "Today", chooseMonth: "Choose month", chooseYear: "Choose year", previousYear: "Previous year", nextYear: "Next year", previousYears: "Previous 12 years", nextYears: "Next 12 years", dateLabel: "{weekday}, {day} {month} {year}", months: ["January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December"], monthsInDate: ["January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December"], weekdays: ["Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday"], weekdaysShort: ["Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat"], weekdaysNarrow: ["S", "M", "T", "W", "T", "F", "S"] } }, ar: { language: "ar", direction: "rtl", weekStartsOn: 6, numerals: "arab", strings: { calendar: "التقويم", previousMonth: "الشهر السابق", nextMonth: "الشهر التالي", today: "اليوم", chooseMonth: "اختر الشهر", chooseYear: "اختر السنة", previousYear: "السنة السابقة", nextYear: "السنة التالية", previousYears: "الأعوام الاثنا عشر السابقة", nextYears: "الأعوام الاثنا عشر التالية", dateLabel: "{weekday}، {day} {month} {year}", months: ["كانون الثاني", "شباط", "آذار", "نيسان", "أيار", "حزيران", "تموز", "آب", "أيلول", "تشرين الأول", "تشرين الثاني", "كانون الأول"], monthsInDate: ["كانون الثاني", "شباط", "آذار", "نيسان", "أيار", "حزيران", "تموز", "آب", "أيلول", "تشرين الأول", "تشرين الثاني", "كانون الأول"], weekdays: ["الأحد", "الاثنين", "الثلاثاء", "الأربعاء", "الخميس", "الجمعة", "السبت"], weekdaysShort: ["أحد", "إثنين", "ثلاثاء", "أربعاء", "خميس", "جمعة", "سبت"], weekdaysNarrow: ["ح", "ن", "ث", "ر", "خ", "ج", "س"] } }, ku: { language: "ku", direction: "rtl", weekStartsOn: 6, numerals: "arab", strings: { calendar: "ڕۆژژمێر", previousMonth: "مانگی پێشوو", nextMonth: "مانگی داهاتوو", today: "ئەمڕۆ", chooseMonth: "مانگ هەڵبژێرە", chooseYear: "ساڵ هەڵبژێرە", previousYear: "ساڵی پێشوو", nextYear: "ساڵی داهاتوو", previousYears: "دوازدە ساڵی پێشوو", nextYears: "دوازدە ساڵی داهاتوو", dateLabel: "{weekday}، {day}ی {month}ی {year}", months: ["کانوونی دووەم", "شوبات", "ئازار", "نیسان", "ئایار", "حوزەیران", "تەمووز", "ئاب", "ئەیلوول", "تشرینی یەکەم", "تشرینی دووەم", "کانوونی یەکەم"], monthsInDate: ["کانوونی دووەم", "شوبات", "ئازار", "نیسان", "ئایار", "حوزەیران", "تەمووز", "ئاب", "ئەیلوول", "تشرینی یەکەم", "تشرینی دووەم", "کانوونی یەکەم"], weekdays: ["یەکشەممە", "دووشەممە", "سێشەممە", "چوارشەممە", "پێنجشەممە", "هەینی", "شەممە"], weekdaysShort: ["یەک", "دوو", "سێ", "چوار", "پێنج", "هەینی", "شەممە"], weekdaysNarrow: ["ی", "د", "س", "چ", "پ", "ه", "ش"] } }, ru: { language: "ru", direction: "ltr", weekStartsOn: 1, numerals: "latn", strings: { calendar: "Календарь", previousMonth: "Предыдущий месяц", nextMonth: "Следующий месяц", today: "Сегодня", chooseMonth: "Выбрать месяц", chooseYear: "Выбрать год", previousYear: "Предыдущий год", nextYear: "Следующий год", previousYears: "Предыдущие 12 лет", nextYears: "Следующие 12 лет", dateLabel: "{weekday}, {day} {month} {year}", months: ["Январь", "Февраль", "Март", "Апрель", "Май", "Июнь", "Июль", "Август", "Сентябрь", "Октябрь", "Ноябрь", "Декабрь"], monthsInDate: ["января", "февраля", "марта", "апреля", "мая", "июня", "июля", "августа", "сентября", "октября", "ноября", "декабря"], weekdays: ["воскресенье", "понедельник", "вторник", "среда", "четверг", "пятница", "суббота"], weekdaysShort: ["Вс", "Пн", "Вт", "Ср", "Чт", "Пт", "Сб"], weekdaysNarrow: ["В", "П", "В", "С", "Ч", "П", "С"] } } }, _f = (n) => {
  var i;
  return (i = sm[n]) !== null && i !== void 0 ? i : sm.en;
};
function nt(n, i) {
  const s = String(n);
  return i === "arab" ? s.replace(/[0-9]/g, ((r) => String.fromCharCode(1632 + Number(r)))) : s;
}
function N_(n, i, s) {
  const [r, c] = bt(n);
  return `${i.strings.months[c - 1]} ${nt(r, s)}`;
}
function Pr(n, i, s) {
  const [r, c, f] = Dn(n), d = { weekday: i.strings.weekdays[vs(n)], day: nt(f, s), month: i.strings.monthsInDate[c - 1], year: nt(r, s) };
  return i.strings.dateLabel.replace(/\{(\w+)\}/g, ((h, g) => {
    var v;
    return (v = d[g]) !== null && v !== void 0 ? v : "";
  }));
}
/*!
 * Built by ShiftSoftware
 * Copyright (c)
 */
function M_(n) {
  return iv(n).map(((i) => parseInt(i, 10))).filter(((i) => Number.isInteger(i) && i >= 0 && i <= 6));
}
function rm(n) {
  return iv(n).filter(((i) => /^\d{4}-\d{2}-\d{2}$/.test(i)));
}
function iv(n) {
  if (n == null || n === "") return [];
  if (Array.isArray(n)) return n.map(((s) => `${s}`.trim())).filter(Boolean);
  const i = `${n}`.trim();
  if (i.startsWith("[")) try {
    const s = JSON.parse(i);
    if (Array.isArray(s)) return s.map(((r) => `${r}`.trim())).filter(Boolean);
  } catch {
  }
  return i.split(/[\s,]+/).map(((s) => s.trim())).filter(Boolean);
}
/*!
 * Built by ShiftSoftware
 * Copyright (c)
 */
function lv(n) {
  const i = n.enabledDates, s = i != null && !(typeof i == "string" && i.trim() === "");
  return { min: xt(n.min), max: xt(n.max), disabledDates: new Set(rm(n.disabledDates).map(xt).filter(Boolean)), disabledWeekdays: new Set(M_(n.disabledWeekdays)), enabledDates: s ? new Set(rm(i).map(xt).filter(Boolean)) : null, isDateDisabled: typeof n.isDateDisabled == "function" ? n.isDateDisabled : null };
}
const Ya = (n, i) => (!i.min || n >= i.min) && (!i.max || n <= i.max);
function Pc(n, i) {
  if (!Ya(n, i)) return "range";
  if (i.disabledDates.has(n)) return "disabled-date";
  if (i.disabledWeekdays.has(vs(n))) return "disabled-weekday";
  if (i.enabledDates && !i.enabledDates.has(n)) return "not-enabled";
  if (i.isDateDisabled) try {
    if (i.isDateDisabled(n)) return "rule";
  } catch {
    return "rule";
  }
  return null;
}
const q_ = (n) => n === null ? null : n === "range" || n === "not-enabled" ? "unavailable" : "closed";
/*!
 * Built by ShiftSoftware
 * Copyright (c)
 */
const om = { en: { times: "Times", noTimes: "No times available on this day", loadingTimes: "Loading times…", timesCount: "{count} slots", am: "AM", pm: "PM", bookingCalendar: "Date and time", idle: "Choose a branch first", loading: "Loading available days…", empty: "No times available at this branch", error: "Couldn't load available times", retry: "Try again", pickDay: "Pick a day", pickTime: "Pick a time", selected: "{date} at {time}", dayTimes: "Slots available: {count}", required: "Please choose a date and time", changeDate: "Change date", dayTitle: "{weekday}, {day} {month}", monthsShort: ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"], timesOn: "Times on {date}", chooseSlot: "Choose a date and time", slotLabel: "{weekday}, {day} {month} {year}, {time}", timesCountOne: "1 slot", daySummary: "{count} · {from} – {to}", emptyTitle: "No available slots" }, ar: { times: "الأوقات", noTimes: "لا توجد أوقات متاحة في هذا اليوم", loadingTimes: "جارٍ تحميل الأوقات…", timesCount: "{count} مواعيد", am: "ص", pm: "م", bookingCalendar: "التاريخ والوقت", idle: "اختر الفرع أولاً", loading: "جارٍ تحميل الأيام المتاحة…", empty: "لا توجد أوقات متاحة في هذا الفرع", error: "تعذّر تحميل الأوقات المتاحة", retry: "أعد المحاولة", pickDay: "اختر اليوم", pickTime: "اختر الوقت", selected: "{date}، الساعة {time}", dayTimes: "المواعيد المتاحة: {count}", required: "الرجاء اختيار التاريخ والوقت", changeDate: "تغيير التاريخ", dayTitle: "{weekday}، {day} {month}", monthsShort: ["كانون الثاني", "شباط", "آذار", "نيسان", "أيار", "حزيران", "تموز", "آب", "أيلول", "تشرين الأول", "تشرين الثاني", "كانون الأول"], timesOn: "الأوقات في {date}", chooseSlot: "اختر التاريخ والوقت", slotLabel: "{weekday}، {day} {month} {year}، {time}", timesCountOne: "موعد واحد", daySummary: "{count} · {from} – {to}", emptyTitle: "لا توجد مواعيد متاحة" }, ku: { times: "کاتەکان", noTimes: "هیچ کاتێکی بەردەست نییە لەم ڕۆژەدا", loadingTimes: "کاتەکان باردەکرێن…", timesCount: "{count} کات", am: "پ.ن", pm: "د.ن", bookingCalendar: "بەروار و کات", idle: "سەرەتا لقێک هەڵبژێرە", loading: "ڕۆژە بەردەستەکان باردەکرێن…", empty: "هیچ کاتێکی بەردەست نییە لەم لقەدا", error: "نەتوانرا کاتە بەردەستەکان باربکرێن", retry: "دووبارە هەوڵ بدە", pickDay: "ڕۆژێک هەڵبژێرە", pickTime: "کاتێک هەڵبژێرە", selected: "{date}، کاتژمێر {time}", dayTimes: "کاتە بەردەستەکان: {count}", required: "تکایە بەروار و کات هەڵبژێرە", changeDate: "گۆڕینی بەروار", dayTitle: "{weekday}، {day}ی {month}", monthsShort: ["کانوونی دووەم", "شوبات", "ئازار", "نیسان", "ئایار", "حوزەیران", "تەمووز", "ئاب", "ئەیلوول", "تشرینی یەکەم", "تشرینی دووەم", "کانوونی یەکەم"], timesOn: "کاتەکان لە {date}", chooseSlot: "بەروار و کات هەڵبژێرە", slotLabel: "{weekday}، {day}ی {month} {year}، {time}", timesCountOne: "یەک کات", daySummary: "{count} · {from} – {to}", emptyTitle: "هیچ کاتێکی بەردەست نییە" }, ru: { times: "Время", noTimes: "В этот день нет свободного времени", loadingTimes: "Загрузка времени…", timesCount: "Слотов: {count}", am: "AM", pm: "PM", bookingCalendar: "Дата и время", idle: "Сначала выберите филиал", loading: "Загрузка свободных дней…", empty: "В этом филиале нет свободного времени", error: "Не удалось загрузить свободное время", retry: "Повторить", pickDay: "Выберите день", pickTime: "Выберите время", selected: "{date}, {time}", dayTimes: "Свободных слотов: {count}", required: "Выберите дату и время", changeDate: "Изменить дату", dayTitle: "{weekday}, {day} {month}", monthsShort: ["янв.", "февр.", "мар.", "апр.", "мая", "июн.", "июл.", "авг.", "сент.", "окт.", "нояб.", "дек."], timesOn: "Время на {date}", chooseSlot: "Выберите дату и время", slotLabel: "{weekday}, {day} {month} {year}, {time}", timesCountOne: "1 слот", daySummary: "{count} · {from} – {to}", emptyTitle: "Нет свободных слотов" } }, sv = (n) => {
  var i;
  return (i = om[n]) !== null && i !== void 0 ? i : om.en;
}, j_ = /^([01]\d|2[0-3]):([0-5]\d)$/, R_ = (n) => typeof n == "string" && j_.test(n);
function um(n) {
  let i = [];
  if (Array.isArray(n)) i = n;
  else if (typeof n == "string" && n.trim()) {
    const s = n.trim();
    if (s.startsWith("[")) try {
      const r = JSON.parse(s);
      Array.isArray(r) && (i = r);
    } catch {
      i = [];
    }
    else i = s.split(/[\s,]+/);
  }
  return [...new Set(i.map(((s) => String(s).trim())).filter(R_))].sort();
}
function ll(n, i, s, r) {
  const [c, f] = n.split(":").map(Number);
  return i !== "h12" ? nt(n, s) : `${nt(`${c % 12 || 12}:${String(f).padStart(2, "0")}`, s)} ${c < 12 ? r.am : r.pm}`;
}
const Pn = (n, i) => n.replace(/\{(\w+)\}/g, ((s, r) => {
  var c;
  return String((c = i[r]) !== null && c !== void 0 ? c : "");
})), $_ = hf(class extends of {
  constructor(n) {
    super(), n !== !1 && this.__registerHost(), this.__attachShadow(), this.timeChange = Tn(this, "timeChange"), this.times = [], this.value = "", this.hourCycle = "h23", this.language = "en", this.loading = !1, this.skeletonCount = 8, this.disabled = !1, this.colorScheme = "light", this.size = "md", this.shown = { key: "empty", kind: "empty", times: [] }, this.leaving = null, this.focusTime = "", this.loaded = !1, this.pendingFocus = !1, this.observed = null, this.onSettled = (i) => {
      i.target === i.currentTarget && (this.leaving = null);
    };
  }
  componentWillLoad() {
    this.shown = this.layerFor();
  }
  componentDidLoad() {
    this.loaded = !0;
  }
  componentDidRender() {
    this.watchHeight(), this.pendingFocus && (this.pendingFocus = !1, this.focusChip());
  }
  disconnectedCallback() {
    var n;
    clearTimeout(this.settleTimer), (n = this.observer) === null || n === void 0 || n.disconnect(), this.observed = null;
  }
  onContentChange() {
    const n = this.layerFor();
    if (n.key === this.shown.key) return;
    clearTimeout(this.settleTimer);
    const i = this.loaded && !(typeof window < "u" && (!((s = window.matchMedia) === null || s === void 0) && s.call(window, "(prefers-reduced-motion: reduce)").matches));
    var s;
    this.leaving = i ? this.shown : null, this.shown = n, n.times.includes(this.focusTime) || (this.focusTime = ""), i && (this.settleTimer = setTimeout((() => this.leaving = null), 1500));
  }
  async setFocus() {
    this.focusChip();
  }
  get locale() {
    return _f(this.language);
  }
  get strings() {
    return sv(this.locale.language);
  }
  get digits() {
    return this.numerals === "latn" || this.numerals === "arab" ? this.numerals : this.locale.numerals;
  }
  layerFor() {
    if (this.loading) return { key: "loading", kind: "loading", times: [] };
    const n = um(this.times);
    return n.length ? { key: `times:${n.join(",")}`, kind: "times", times: n } : { key: "empty", kind: "empty", times: n };
  }
  get blocked() {
    return new Set(um(this.disabledTimes));
  }
  tabStop(n) {
    var i, s;
    const r = this.blocked;
    return n.includes(this.focusTime) ? this.focusTime : n.includes(this.value) ? this.value : (s = (i = n.find(((c) => !r.has(c)))) !== null && i !== void 0 ? i : n[0]) !== null && s !== void 0 ? s : "";
  }
  watchHeight() {
    var n, i;
    const s = (n = this.el.shadowRoot) === null || n === void 0 ? void 0 : n.querySelector(".ts-layer[data-current]");
    s && s !== this.observed && typeof ResizeObserver < "u" && ((i = this.observer) === null || i === void 0 || i.disconnect(), this.observer = new ResizeObserver((() => this.syncHeight())), this.observer.observe(s), this.observed = s, this.syncHeight());
  }
  syncHeight() {
    var n;
    const i = (n = this.el.shadowRoot) === null || n === void 0 ? void 0 : n.querySelector(".ts-frame");
    i && this.observed && this.observed.getClientRects().length && (i.style.setProperty("--_rows", `${this.observed.getBoundingClientRect().height}px`), i.hasAttribute("data-measured") || requestAnimationFrame((() => i.setAttribute("data-measured", ""))));
  }
  columns() {
    var n;
    const i = (n = this.el.shadowRoot) === null || n === void 0 ? void 0 : n.querySelector(".ts-layer[data-current] .ts-grid"), s = i && typeof getComputedStyle == "function" ? getComputedStyle(i).gridTemplateColumns : "";
    return s && s !== "none" ? s.trim().split(/\s+/).length : 1;
  }
  focusChip() {
    var n, i;
    (i = (n = this.el.shadowRoot) === null || n === void 0 ? void 0 : n.querySelector('.ts-layer[data-current] [tabindex="0"]')) === null || i === void 0 || i.focus({ preventScroll: !0 });
  }
  select(n) {
    this.disabled || this.loading || this.blocked.has(n) || (this.focusTime = n, this.value = n, this.timeChange.emit({ value: n }));
  }
  onKeyDown(n, i) {
    if (this.disabled || n.altKey || n.ctrlKey || n.metaKey) return;
    const s = this.tabStop(i);
    if (n.key === "Enter" || n.key === " ") return n.preventDefault(), void this.select(s);
    const r = this.blocked, c = (function(f, d, { count: h, columns: g, rtl: v, skip: y = () => !1 }) {
      if (!h) return null;
      const m = Math.max(1, g), _ = v ? "ArrowLeft" : "ArrowRight", x = v ? "ArrowRight" : "ArrowLeft", T = (q, $) => {
        for (let j = q; j >= 0 && j < h; j += $) if (!y(j)) return j;
        return null;
      }, A = Math.min(h - 1, Math.max(0, f));
      let z;
      if (d === _) z = T(A + 1, 1);
      else if (d === x) z = T(A - 1, -1);
      else if (d === "ArrowDown") z = T(A + m, m);
      else if (d === "ArrowUp") z = T(A - m, -m);
      else if (d === "Home") z = T(0, 1);
      else {
        if (d !== "End") return null;
        z = T(h - 1, -1);
      }
      return z ?? A;
    })(i.indexOf(s), n.key, { count: i.length, columns: this.columns(), rtl: this.locale.direction === "rtl", skip: (f) => r.has(i[f]) });
    c !== null && (n.preventDefault(), this.focusTime = i[c], this.pendingFocus = !0);
  }
  renderTimes(n, i) {
    const s = this.blocked, r = i ? this.tabStop(n.times) : "", c = i && !!this.leaving;
    return X("div", { class: "ts-grid", part: "grid", onKeyDown: i ? (f) => this.onKeyDown(f, n.times) : void 0 }, n.times.map(((f, d) => {
      const h = f === this.value, g = this.disabled || s.has(f), v = ["time", h && "time-selected", g && "time-disabled"].filter(Boolean).join(" ");
      return X("button", { key: f, type: "button", role: "radio", class: "ts-chip", part: v, "data-time": f, "data-selected": h ? "" : void 0, "aria-checked": h ? "true" : "false", "aria-disabled": g ? "true" : void 0, tabindex: i && !this.disabled && f === r ? "0" : "-1", style: c ? { animationDelay: 24 * Math.min(d, 11) + "ms" } : void 0, onClick: i ? () => this.select(f) : void 0 }, ll(f, this.hourCycle, this.digits, this.strings));
    })));
  }
  renderLayer(n, i) {
    const s = Math.max(1, Math.min(48, Math.round(Number(this.skeletonCount) || 8)));
    return X("div", { key: `layer-${n.key}`, class: "ts-layer", "data-kind": n.kind, "data-current": i ? "" : void 0, "data-phase": i ? this.leaving ? "enter" : "rest" : "leave", "aria-hidden": i ? void 0 : "true", inert: !i || void 0, onAnimationEnd: i ? void 0 : this.onSettled }, n.kind === "times" && this.renderTimes(n, i), n.kind === "loading" && X("div", { class: "ts-grid", part: "grid", "aria-hidden": "true" }, Array.from({ length: s }, (() => X("span", { class: "ts-chip ts-skeleton", part: "skeleton" })))), n.kind === "empty" && X("p", { class: "ts-empty", part: "empty" }, this.emptyText || this.strings.noTimes));
  }
  render() {
    const n = this.locale, i = this.strings, s = this.shown, r = s.kind === "loading" ? i.loadingTimes : s.kind === "empty" ? this.emptyText || i.noTimes : Pn(i.timesCount, { count: s.times.length });
    return X(to, { key: "537908a71df961916aff5a283dbaef18f2742f5e" }, X("div", { key: `${this.appearance || "vanilla"}-${this.size}`, class: "ts-root", part: "root", dir: n.direction, lang: n.language === "ku" ? "ckb" : n.language, role: "radiogroup", "aria-label": this.label || i.times, "aria-busy": this.loading ? "true" : void 0, "aria-disabled": this.disabled ? "true" : void 0, "data-kind": s.kind }, X("div", { key: "f9aa12405cbe7807409b47bbd184d1c8d36bb4c0", class: "ts-frame", "data-switching": this.leaving ? "" : void 0 }, this.leaving && this.renderLayer(this.leaving, !1), this.renderLayer(s, !0)), X("span", { key: "57b7009800724a2082a8a96443de07b64473ceb2", class: "ts-sr", "aria-live": "polite" }, r)));
  }
  get el() {
    return this;
  }
  static get watchers() {
    return { times: [{ onContentChange: 0 }], loading: [{ onContentChange: 0 }] };
  }
  static get style() {
    return '*,:after,:before{--tw-border-spacing-x:0;--tw-border-spacing-y:0;--tw-translate-x:0;--tw-translate-y:0;--tw-rotate:0;--tw-skew-x:0;--tw-skew-y:0;--tw-scale-x:1;--tw-scale-y:1;--tw-scroll-snap-strictness:proximity;--tw-ring-offset-width:0px;--tw-ring-offset-color:#fff;--tw-ring-color:rgba(59,130,246,.5);--tw-ring-offset-shadow:0 0 #0000;--tw-ring-shadow:0 0 #0000;--tw-shadow:0 0 #0000;--tw-shadow-colored:0 0 #0000;border:0 solid #e5e7eb;box-sizing:border-box}::backdrop{--tw-border-spacing-x:0;--tw-border-spacing-y:0;--tw-translate-x:0;--tw-translate-y:0;--tw-rotate:0;--tw-skew-x:0;--tw-skew-y:0;--tw-scale-x:1;--tw-scale-y:1;--tw-scroll-snap-strictness:proximity;--tw-ring-offset-width:0px;--tw-ring-offset-color:#fff;--tw-ring-color:rgba(59,130,246,.5);--tw-ring-offset-shadow:0 0 #0000;--tw-ring-shadow:0 0 #0000;--tw-shadow:0 0 #0000;--tw-shadow-colored:0 0 #0000;}.collapse{visibility:collapse}.fixed{position:fixed}.absolute{position:absolute}.relative{position:relative}.block{display:block}.table{display:table}.grid{display:grid}.hidden{display:none}.border-collapse{border-collapse:collapse}.transform{transform:translate(var(--tw-translate-x),var(--tw-translate-y)) rotate(var(--tw-rotate)) skewX(var(--tw-skew-x)) skewY(var(--tw-skew-y)) scaleX(var(--tw-scale-x)) scaleY(var(--tw-scale-y))}.resize{resize:both}.flex-wrap{flex-wrap:wrap}.border{border-width:1px}.uppercase{text-transform:uppercase}.underline{text-decoration-line:underline}.line-through{text-decoration-line:line-through}.shadow{--tw-shadow:0 1px 3px 0 rgba(0,0,0,.1),0 1px 2px -1px rgba(0,0,0,.1);--tw-shadow-colored:0 1px 3px 0 var(--tw-shadow-color),0 1px 2px -1px var(--tw-shadow-color);box-shadow:var(--tw-ring-offset-shadow,0 0 #0000),var(--tw-ring-shadow,0 0 #0000),var(--tw-shadow)}.outline{outline-style:solid}.ring{--tw-ring-offset-shadow:var(--tw-ring-inset) 0 0 0 var(--tw-ring-offset-width) var(--tw-ring-offset-color);--tw-ring-shadow:var(--tw-ring-inset) 0 0 0 calc(3px + var(--tw-ring-offset-width)) var(--tw-ring-color);box-shadow:var(--tw-ring-offset-shadow),var(--tw-ring-shadow),var(--tw-shadow,0 0 #0000)}.transition{transition-duration:.15s;transition-property:color,background-color,border-color,text-decoration-color,fill,stroke,opacity,box-shadow,transform,filter,backdrop-filter;transition-timing-function:cubic-bezier(.4,0,.2,1)}:host{-webkit-text-size-adjust:100%;-webkit-tap-highlight-color:transparent;font-feature-settings:normal;--_surface:var(--shift-time-slots-surface,var(--shift-surface,var(--_ap-surface,#fff)));--_ink:var(--shift-time-slots-ink,var(--shift-ink,var(--_ap-ink,#1e293b)));--_muted:var(--shift-time-slots-muted,var(--shift-ink-muted,var(--_ap-ink-muted,#64748b)));--_disabled-ink:var(--shift-time-slots-disabled-ink,var(--shift-ink-disabled,var(--_ap-ink-disabled,#cbd5e1)));--_line:var(--shift-time-slots-line,var(--shift-line,var(--_ap-line,#e2e8f0)));--_hover-line:var(--shift-time-slots-hover-line,var(--shift-line-strong,var(--_ap-hover-line,#94a3b8)));--_hover-bg:var(--shift-time-slots-hover-bg,var(--_ap-hover-bg,#f1f5f9));--_hover-ink:var(--shift-time-slots-hover-ink,var(--_ap-hover-ink,#1e293b));--_accent:var(--shift-time-slots-accent,var(--shift-accent,var(--_ap-accent,#1e293b)));--_on-accent:var(--shift-time-slots-on-accent,var(--shift-on-accent,var(--_ap-on-accent,#fff)));--_skeleton:var(--shift-time-slots-skeleton,var(--shift-accent-tint,var(--_ap-accent-tint,#f1f5f9)));--_focus:var(--shift-time-slots-focus-ring,var(--shift-focus,var(--_ap-focus,#1e293b)));--_focus-width:var(--shift-time-slots-focus-ring-width,var(--_ap-focus-width,2px));--_focus-offset:var(--shift-time-slots-focus-ring-offset,var(--_ap-focus-offset,2px));--_halo:var(--shift-time-slots-focus-halo,var(--_ap-focus-halo,transparent));--_halo-width:var(--shift-time-slots-focus-halo-width,var(--_ap-focus-halo-width,0px));--_shadow:var(--shift-time-slots-shadow,var(--shift-shadow-control,var(--_ap-shadow-control,none)));--_blocked-decoration:var(--shift-time-slots-blocked-decoration,var(--_ap-blocked-decoration,line-through));--_blocked-opacity:var(--shift-time-slots-blocked-opacity,var(--_ap-blocked-opacity,1));--_radius:var(--shift-time-slots-radius,var(--shift-radius-sm,var(--_ap-radius-sm,8px)));--_border-width:var(--shift-time-slots-border-width,var(--shift-border-width,var(--_ap-border-width,1px)));--_font-latin:var(--shift-time-slots-font-family,var(--shift-font-family,var(--_ap-font,"Nunito",system-ui,sans-serif)));--_font-arabic:var(--shift-time-slots-font-family-arabic,var(--shift-font-family-arabic,"Noto Kufi Arabic",var(--_font-latin)));--_font-size:var(--shift-time-slots-font-size,var(--shift-font-size,calc(var(--_ap-font-size, 14px) + var(--_sz-font, 0px))));--_empty-size:var(--shift-time-slots-empty-font-size,var(--shift-font-size-sm,calc(var(--_ap-font-size-sm, 12px) + var(--_sz-font, 0px))));--_weight:var(--shift-time-slots-font-weight,var(--shift-weight,var(--_ap-weight,400)));--_selected-weight:var(--shift-time-slots-selected-font-weight,var(--shift-weight,var(--_ap-weight,400)));--_line-height:var(--shift-time-slots-line-height,1.43);--_chip-height:var(--shift-time-slots-chip-height,var(--shift-control-height,calc(var(--_ap-control-height, 44px)*var(--_sz-control, 1))));--_min-column:var(--shift-time-slots-min-column,calc(84px*var(--_sz-control, 1)));--_gap:var(--shift-time-slots-gap,var(--shift-gap,calc(var(--_ap-gap, 4px)*2*var(--_sz-space, 1))));--_chip-padding:var(--shift-time-slots-chip-padding-inline,calc(8px*var(--_sz-space, 1)));--_empty-padding:var(--shift-time-slots-empty-padding-block,calc(24px*var(--_sz-space, 1)));--_rise:var(--shift-time-slots-rise-distance,8px);--_settle:var(--shift-time-slots-settle,var(--shift-settle,480ms));--_ease:var(--shift-time-slots-ease,var(--shift-ease,ease));--_ring:max(calc(var(--_focus-width) + var(--_focus-offset)),var(--_halo-width),2px);--_shadow-room:var(--shift-time-slots-shadow-room,6px);color:var(--_ink);display:block;font-family:ui-sans-serif,system-ui,sans-serif,Apple Color Emoji,Segoe UI Emoji,Segoe UI Symbol,Noto Color Emoji;font-family:var(--_font-latin);font-size:var(--_font-size);font-variation-settings:normal;line-height:1.5;line-height:var(--_line-height);-moz-tab-size:4;-o-tab-size:4;tab-size:4}hr{border-top-width:1px;color:inherit;height:0}abbr:where([title]){-webkit-text-decoration:underline dotted;text-decoration:underline dotted}h1,h2,h3,h4,h5,h6{font-size:inherit;font-weight:inherit}a{color:inherit;text-decoration:inherit}b,strong{font-weight:bolder}code,kbd,pre,samp{font-feature-settings:normal;font-family:ui-monospace,SFMono-Regular,Menlo,Monaco,Consolas,Liberation Mono,Courier New,monospace;font-size:1em;font-variation-settings:normal}small{font-size:80%}sub,sup{font-size:75%;line-height:0;position:relative;vertical-align:baseline}sub{bottom:-.25em}sup{top:-.5em}table{border-collapse:collapse;border-color:inherit;text-indent:0}button,input,optgroup,select,textarea{font-feature-settings:inherit;color:inherit;font-family:inherit;font-size:100%;font-variation-settings:inherit;font-weight:inherit;letter-spacing:inherit;line-height:inherit;margin:0;padding:0}button,select{text-transform:none}button,input:where([type=button]),input:where([type=reset]),input:where([type=submit]){-webkit-appearance:button;background-color:transparent;background-image:none}:-moz-focusring{outline:auto}:-moz-ui-invalid{box-shadow:none}progress{vertical-align:baseline}::-webkit-inner-spin-button,::-webkit-outer-spin-button{height:auto}[type=search]{-webkit-appearance:textfield;outline-offset:-2px}::-webkit-search-decoration{-webkit-appearance:none}::-webkit-file-upload-button{-webkit-appearance:button;font:inherit}summary{display:list-item}blockquote,dd,dl,fieldset,figure,h1,h2,h3,h4,h5,h6,hr,p,pre{margin:0}fieldset,legend{padding:0}menu,ol,ul{list-style:none;margin:0;padding:0}dialog{padding:0}textarea{resize:vertical}input::-moz-placeholder,textarea::-moz-placeholder{color:#9ca3af;opacity:1}input::placeholder,textarea::placeholder{color:#9ca3af;opacity:1}[role=button],button{cursor:pointer}:disabled{cursor:default}audio,canvas,embed,iframe,img,object,svg,video{display:block;vertical-align:middle}img,video{height:auto;max-width:100%}[hidden]:where(:not([hidden=until-found])){display:none}:host([appearance=vanilla]){--_ap-font:"Nunito",system-ui,sans-serif;--_ap-font-size:14px;--_ap-font-size-sm:12px;--_ap-label-size:12px;--_ap-label-transform:uppercase;--_ap-label-tracking:0.04em;--_ap-label-weight:600;--_ap-weight:400;--_ap-weight-strong:600;--_ap-radius-sm:8px;--_ap-radius-md:8px;--_ap-radius-lg:12px;--_ap-border-width:1px;--_ap-panel-border-width:1px;--_ap-control-height:44px;--_ap-cell-size:40px;--_ap-cell-round:0;--_ap-gap:4px;--_ap-focus-width:2px;--_ap-focus-offset:2px;--_ap-focus-halo-width:0px;--_ap-today-ring-width:1px;--_ap-today-stroke:0px;--_ap-today-decoration:none;--_ap-blocked-decoration:line-through;--_ap-blocked-opacity:1;--_ap-surface:#fff;--_ap-surface-muted:#f8fafc;--_ap-panel-surface:#fff;--_ap-ink:#1e293b;--_ap-ink-muted:#64748b;--_ap-ink-disabled:#cbd5e1;--_ap-line:#e2e8f0;--_ap-line-strong:#94a3b8;--_ap-panel-line:#e2e8f0;--_ap-cell-line:transparent;--_ap-accent:#1e293b;--_ap-on-accent:#fff;--_ap-accent-tint:#f1f5f9;--_ap-focus:#1e293b;--_ap-focus-halo:transparent;--_ap-focus-inner:#fff;--_ap-danger:#dc2626;--_ap-shadow-panel:0 10px 30px -12px rgba(15,23,42,.25);--_ap-shadow-control:none;--_ap-hover-bg:#f1f5f9;--_ap-hover-line:#94a3b8;--_ap-hover-ink:#1e293b;--_ap-today-ring:#64748b;--_ap-today-dot:transparent}:host([appearance=material]){--_ap-font:"Nunito",system-ui,sans-serif;--_ap-font-size:14px;--_ap-font-size-sm:12px;--_ap-label-size:12px;--_ap-label-transform:none;--_ap-label-tracking:0em;--_ap-label-weight:500;--_ap-weight:400;--_ap-weight-strong:600;--_ap-radius-sm:999px;--_ap-radius-md:4px;--_ap-radius-lg:4px;--_ap-border-width:0px;--_ap-panel-border-width:1px;--_ap-control-height:48px;--_ap-cell-size:40px;--_ap-cell-round:1;--_ap-gap:2px;--_ap-focus-width:2px;--_ap-focus-offset:2px;--_ap-focus-halo-width:0px;--_ap-today-ring-width:1px;--_ap-today-stroke:0px;--_ap-today-decoration:none;--_ap-blocked-decoration:none;--_ap-blocked-opacity:1;--_ap-surface:#fff;--_ap-surface-muted:#f5f5f5;--_ap-panel-surface:#fff;--_ap-ink:rgba(0,0,0,.87);--_ap-ink-muted:rgba(0,0,0,.54);--_ap-ink-disabled:rgba(0,0,0,.38);--_ap-line:rgba(0,0,0,.12);--_ap-line-strong:rgba(0,0,0,.42);--_ap-panel-line:rgba(0,0,0,.12);--_ap-cell-line:transparent;--_ap-accent:#1976d2;--_ap-on-accent:#fff;--_ap-accent-tint:rgba(25,118,210,.08);--_ap-focus:#1976d2;--_ap-focus-halo:transparent;--_ap-focus-inner:#fff;--_ap-danger:#d32f2f;--_ap-shadow-panel:0 5px 5px -3px rgba(0,0,0,.2),0 8px 10px 1px rgba(0,0,0,.14),0 3px 14px 2px rgba(0,0,0,.12);--_ap-shadow-control:0 2px 1px -1px rgba(0,0,0,.2),0 1px 1px 0 rgba(0,0,0,.14),0 1px 3px 0 rgba(0,0,0,.12);--_ap-hover-bg:rgba(0,0,0,.04);--_ap-hover-line:rgba(0,0,0,.12);--_ap-hover-ink:rgba(0,0,0,.87);--_ap-today-ring:#1976d2;--_ap-today-dot:transparent}:host([appearance=soft]){--_ap-font:"Nunito",system-ui,sans-serif;--_ap-font-size:15px;--_ap-font-size-sm:13px;--_ap-label-size:13px;--_ap-label-transform:none;--_ap-label-tracking:0em;--_ap-label-weight:600;--_ap-weight:400;--_ap-weight-strong:600;--_ap-radius-sm:12px;--_ap-radius-md:14px;--_ap-radius-lg:20px;--_ap-border-width:0px;--_ap-panel-border-width:0px;--_ap-control-height:48px;--_ap-cell-size:44px;--_ap-cell-round:0;--_ap-gap:6px;--_ap-focus-width:1px;--_ap-focus-offset:0px;--_ap-focus-halo-width:4px;--_ap-today-ring-width:0px;--_ap-today-stroke:0px;--_ap-today-decoration:none;--_ap-blocked-decoration:none;--_ap-blocked-opacity:0.5;--_ap-surface:#f1f5f9;--_ap-surface-muted:#f8fafc;--_ap-panel-surface:#f1f5f9;--_ap-ink:#1e293b;--_ap-ink-muted:#475569;--_ap-ink-disabled:#cbd5e1;--_ap-line:transparent;--_ap-line-strong:#cbd5e1;--_ap-panel-line:transparent;--_ap-cell-line:transparent;--_ap-accent:#1e293b;--_ap-on-accent:#fff;--_ap-accent-tint:#e2e8f0;--_ap-focus:#1e293b;--_ap-focus-halo:#e2e8f0;--_ap-focus-inner:#e2e8f0;--_ap-danger:#dc2626;--_ap-shadow-panel:0 16px 40px -16px rgba(15,23,42,.2);--_ap-shadow-control:0 1px 2px 0 rgba(15,23,42,.06),0 8px 24px -12px rgba(15,23,42,.18);--_ap-hover-bg:#e2e8f0;--_ap-hover-line:transparent;--_ap-hover-ink:#1e293b;--_ap-today-ring:transparent;--_ap-today-dot:#1e293b}:host([appearance=sharp]){--_ap-font:"Manrope",system-ui,sans-serif;--_ap-font-size:13px;--_ap-font-size-sm:12px;--_ap-label-size:11px;--_ap-label-transform:uppercase;--_ap-label-tracking:0.06em;--_ap-label-weight:700;--_ap-weight:400;--_ap-weight-strong:600;--_ap-radius-sm:2px;--_ap-radius-md:2px;--_ap-radius-lg:4px;--_ap-border-width:1px;--_ap-panel-border-width:1px;--_ap-control-height:36px;--_ap-cell-size:34px;--_ap-cell-round:0;--_ap-gap:2px;--_ap-focus-width:2px;--_ap-focus-offset:0px;--_ap-focus-halo-width:0px;--_ap-today-ring-width:0px;--_ap-today-stroke:0.6px;--_ap-today-decoration:underline;--_ap-blocked-decoration:line-through;--_ap-blocked-opacity:1;--_ap-surface:#fff;--_ap-surface-muted:#f1f5f9;--_ap-panel-surface:#fff;--_ap-ink:#0f172a;--_ap-ink-muted:#475569;--_ap-ink-disabled:#94a3b8;--_ap-line:#94a3b8;--_ap-line-strong:#0f172a;--_ap-panel-line:#0f172a;--_ap-cell-line:#94a3b8;--_ap-accent:#0f172a;--_ap-on-accent:#fff;--_ap-accent-tint:#f1f5f9;--_ap-focus:#0f172a;--_ap-focus-halo:transparent;--_ap-focus-inner:#fff;--_ap-danger:#b91c1c;--_ap-shadow-panel:none;--_ap-shadow-control:none;--_ap-hover-bg:#f1f5f9;--_ap-hover-line:#94a3b8;--_ap-hover-ink:#0f172a;--_ap-today-ring:transparent;--_ap-today-dot:transparent}:host([appearance=minimal]){--_ap-font:"Manrope",system-ui,sans-serif;--_ap-font-size:14px;--_ap-font-size-sm:12px;--_ap-label-size:12px;--_ap-label-transform:none;--_ap-label-tracking:0em;--_ap-label-weight:400;--_ap-weight:400;--_ap-weight-strong:600;--_ap-radius-sm:999px;--_ap-radius-md:6px;--_ap-radius-lg:8px;--_ap-border-width:0px;--_ap-panel-border-width:1px;--_ap-control-height:44px;--_ap-cell-size:40px;--_ap-cell-round:1;--_ap-gap:4px;--_ap-focus-width:2px;--_ap-focus-offset:2px;--_ap-focus-halo-width:0px;--_ap-today-ring-width:0px;--_ap-today-stroke:0px;--_ap-today-decoration:none;--_ap-blocked-decoration:none;--_ap-blocked-opacity:1;--_ap-surface:transparent;--_ap-surface-muted:transparent;--_ap-panel-surface:transparent;--_ap-ink:#0f172a;--_ap-ink-muted:#64748b;--_ap-ink-disabled:#cbd5e1;--_ap-line:transparent;--_ap-line-strong:#cbd5e1;--_ap-panel-line:transparent;--_ap-cell-line:transparent;--_ap-accent:#0f172a;--_ap-on-accent:#fff;--_ap-accent-tint:#f1f5f9;--_ap-focus:#0f172a;--_ap-focus-halo:transparent;--_ap-focus-inner:#fff;--_ap-danger:#dc2626;--_ap-shadow-panel:none;--_ap-shadow-control:none;--_ap-hover-bg:transparent;--_ap-hover-line:transparent;--_ap-hover-ink:#0f172a;--_ap-today-ring:transparent;--_ap-today-dot:#0f172a}:host([color-scheme=light]){--_ap-tone-neutral-bg:#e8ecef;--_ap-tone-neutral-ink:#46535e;--_ap-tone-info-bg:#e8f1f8;--_ap-tone-info-ink:#275e8f;--_ap-tone-success-bg:#e8f4ed;--_ap-tone-success-ink:#216b44;--_ap-tone-warning-bg:#fff7e6;--_ap-tone-warning-ink:#8a5a12;--_ap-tone-danger-bg:#fff1f0;--_ap-tone-danger-ink:#9b2c2c;color-scheme:light}:host(:not([appearance])[color-scheme=dark]),:host([appearance=vanilla][color-scheme=dark]){--_ap-surface:#0f172a;--_ap-surface-muted:#1e293b;--_ap-panel-surface:#0f172a;--_ap-ink:#f1f5f9;--_ap-ink-muted:#94a3b8;--_ap-ink-disabled:#475569;--_ap-line:#334155;--_ap-line-strong:#64748b;--_ap-panel-line:#334155;--_ap-cell-line:transparent;--_ap-accent:#f1f5f9;--_ap-on-accent:#0f172a;--_ap-accent-tint:#1e293b;--_ap-focus:#f1f5f9;--_ap-focus-halo:transparent;--_ap-focus-inner:#0f172a;--_ap-danger:#f87171;--_ap-shadow-panel:0 10px 30px -12px rgba(0,0,0,.6);--_ap-shadow-control:none;--_ap-hover-bg:#1e293b;--_ap-hover-line:#64748b;--_ap-hover-ink:#f1f5f9;--_ap-today-ring:#64748b;--_ap-today-dot:transparent}:host([appearance=material][color-scheme=dark]){--_ap-surface:#1e1e1e;--_ap-surface-muted:#2c2c2c;--_ap-panel-surface:#2c2c2c;--_ap-ink:hsla(0,0%,100%,.87);--_ap-ink-muted:hsla(0,0%,100%,.6);--_ap-ink-disabled:hsla(0,0%,100%,.38);--_ap-line:hsla(0,0%,100%,.12);--_ap-line-strong:hsla(0,0%,100%,.42);--_ap-panel-line:hsla(0,0%,100%,.12);--_ap-cell-line:transparent;--_ap-accent:#90caf9;--_ap-on-accent:#0d1b2a;--_ap-accent-tint:rgba(144,202,249,.12);--_ap-focus:#90caf9;--_ap-focus-halo:transparent;--_ap-focus-inner:#0d1b2a;--_ap-danger:#f48fb1;--_ap-shadow-panel:0 5px 5px -3px rgba(0,0,0,.2),0 8px 10px 1px rgba(0,0,0,.14),0 3px 14px 2px rgba(0,0,0,.12);--_ap-shadow-control:0 2px 1px -1px rgba(0,0,0,.4),0 1px 1px 0 rgba(0,0,0,.28),0 1px 3px 0 rgba(0,0,0,.24);--_ap-hover-bg:hsla(0,0%,100%,.08);--_ap-hover-line:hsla(0,0%,100%,.12);--_ap-hover-ink:hsla(0,0%,100%,.87);--_ap-today-ring:#90caf9;--_ap-today-dot:transparent}:host([appearance=soft][color-scheme=dark]){--_ap-surface:#1e293b;--_ap-surface-muted:#0f172a;--_ap-panel-surface:#1e293b;--_ap-ink:#f1f5f9;--_ap-ink-muted:#94a3b8;--_ap-ink-disabled:#475569;--_ap-line:transparent;--_ap-line-strong:#475569;--_ap-panel-line:transparent;--_ap-cell-line:transparent;--_ap-accent:#f1f5f9;--_ap-on-accent:#0f172a;--_ap-accent-tint:#334155;--_ap-focus:#f1f5f9;--_ap-focus-halo:#334155;--_ap-focus-inner:#334155;--_ap-danger:#f87171;--_ap-shadow-panel:0 16px 40px -16px rgba(0,0,0,.55);--_ap-shadow-control:0 1px 2px 0 rgba(0,0,0,.4),0 8px 24px -12px rgba(0,0,0,.5);--_ap-hover-bg:#334155;--_ap-hover-line:transparent;--_ap-hover-ink:#f1f5f9;--_ap-today-ring:transparent;--_ap-today-dot:#f1f5f9}:host([appearance=sharp][color-scheme=dark]){--_ap-surface:#0b0f17;--_ap-surface-muted:#0f172a;--_ap-panel-surface:#0b0f17;--_ap-ink:#f8fafc;--_ap-ink-muted:#94a3b8;--_ap-ink-disabled:#475569;--_ap-line:#475569;--_ap-line-strong:#e2e8f0;--_ap-panel-line:#e2e8f0;--_ap-cell-line:#475569;--_ap-accent:#f8fafc;--_ap-on-accent:#020617;--_ap-accent-tint:#1e293b;--_ap-focus:#f8fafc;--_ap-focus-halo:transparent;--_ap-focus-inner:#020617;--_ap-danger:#f87171;--_ap-shadow-panel:none;--_ap-shadow-control:none;--_ap-hover-bg:#1e293b;--_ap-hover-line:#475569;--_ap-hover-ink:#f8fafc;--_ap-today-ring:transparent;--_ap-today-dot:transparent}:host([appearance=minimal][color-scheme=dark]){--_ap-surface:transparent;--_ap-surface-muted:transparent;--_ap-panel-surface:transparent;--_ap-ink:#f1f5f9;--_ap-ink-muted:#94a3b8;--_ap-ink-disabled:#475569;--_ap-line:transparent;--_ap-line-strong:#475569;--_ap-panel-line:#334155;--_ap-cell-line:transparent;--_ap-accent:#f1f5f9;--_ap-on-accent:#0f172a;--_ap-accent-tint:#1e293b;--_ap-focus:#f1f5f9;--_ap-focus-halo:transparent;--_ap-focus-inner:#0f172a;--_ap-danger:#f87171;--_ap-shadow-panel:none;--_ap-shadow-control:none;--_ap-hover-bg:transparent;--_ap-hover-line:transparent;--_ap-hover-ink:#f1f5f9;--_ap-today-ring:transparent;--_ap-today-dot:#f1f5f9}:host([color-scheme=dark]){--_ap-tone-neutral-bg:#334155;--_ap-tone-neutral-ink:#e2e8f0;--_ap-tone-info-bg:#1e3a5f;--_ap-tone-info-ink:#bfdbfe;--_ap-tone-success-bg:#14532d;--_ap-tone-success-ink:#bbf7d0;--_ap-tone-warning-bg:#4a3212;--_ap-tone-warning-ink:#fde68a;--_ap-tone-danger-bg:#4c1d1d;--_ap-tone-danger-ink:#fecaca;color-scheme:dark}@media (prefers-color-scheme:dark){:host(:not([appearance])[color-scheme=auto]),:host([appearance=vanilla][color-scheme=auto]){--_ap-surface:#0f172a;--_ap-surface-muted:#1e293b;--_ap-panel-surface:#0f172a;--_ap-ink:#f1f5f9;--_ap-ink-muted:#94a3b8;--_ap-ink-disabled:#475569;--_ap-line:#334155;--_ap-line-strong:#64748b;--_ap-panel-line:#334155;--_ap-cell-line:transparent;--_ap-accent:#f1f5f9;--_ap-on-accent:#0f172a;--_ap-accent-tint:#1e293b;--_ap-focus:#f1f5f9;--_ap-focus-halo:transparent;--_ap-focus-inner:#0f172a;--_ap-danger:#f87171;--_ap-shadow-panel:0 10px 30px -12px rgba(0,0,0,.6);--_ap-shadow-control:none;--_ap-hover-bg:#1e293b;--_ap-hover-line:#64748b;--_ap-hover-ink:#f1f5f9;--_ap-today-ring:#64748b;--_ap-today-dot:transparent}:host([appearance=material][color-scheme=auto]){--_ap-surface:#1e1e1e;--_ap-surface-muted:#2c2c2c;--_ap-panel-surface:#2c2c2c;--_ap-ink:hsla(0,0%,100%,.87);--_ap-ink-muted:hsla(0,0%,100%,.6);--_ap-ink-disabled:hsla(0,0%,100%,.38);--_ap-line:hsla(0,0%,100%,.12);--_ap-line-strong:hsla(0,0%,100%,.42);--_ap-panel-line:hsla(0,0%,100%,.12);--_ap-cell-line:transparent;--_ap-accent:#90caf9;--_ap-on-accent:#0d1b2a;--_ap-accent-tint:rgba(144,202,249,.12);--_ap-focus:#90caf9;--_ap-focus-halo:transparent;--_ap-focus-inner:#0d1b2a;--_ap-danger:#f48fb1;--_ap-shadow-panel:0 5px 5px -3px rgba(0,0,0,.2),0 8px 10px 1px rgba(0,0,0,.14),0 3px 14px 2px rgba(0,0,0,.12);--_ap-shadow-control:0 2px 1px -1px rgba(0,0,0,.4),0 1px 1px 0 rgba(0,0,0,.28),0 1px 3px 0 rgba(0,0,0,.24);--_ap-hover-bg:hsla(0,0%,100%,.08);--_ap-hover-line:hsla(0,0%,100%,.12);--_ap-hover-ink:hsla(0,0%,100%,.87);--_ap-today-ring:#90caf9;--_ap-today-dot:transparent}:host([appearance=soft][color-scheme=auto]){--_ap-surface:#1e293b;--_ap-surface-muted:#0f172a;--_ap-panel-surface:#1e293b;--_ap-ink:#f1f5f9;--_ap-ink-muted:#94a3b8;--_ap-ink-disabled:#475569;--_ap-line:transparent;--_ap-line-strong:#475569;--_ap-panel-line:transparent;--_ap-cell-line:transparent;--_ap-accent:#f1f5f9;--_ap-on-accent:#0f172a;--_ap-accent-tint:#334155;--_ap-focus:#f1f5f9;--_ap-focus-halo:#334155;--_ap-focus-inner:#334155;--_ap-danger:#f87171;--_ap-shadow-panel:0 16px 40px -16px rgba(0,0,0,.55);--_ap-shadow-control:0 1px 2px 0 rgba(0,0,0,.4),0 8px 24px -12px rgba(0,0,0,.5);--_ap-hover-bg:#334155;--_ap-hover-line:transparent;--_ap-hover-ink:#f1f5f9;--_ap-today-ring:transparent;--_ap-today-dot:#f1f5f9}:host([appearance=sharp][color-scheme=auto]){--_ap-surface:#0b0f17;--_ap-surface-muted:#0f172a;--_ap-panel-surface:#0b0f17;--_ap-ink:#f8fafc;--_ap-ink-muted:#94a3b8;--_ap-ink-disabled:#475569;--_ap-line:#475569;--_ap-line-strong:#e2e8f0;--_ap-panel-line:#e2e8f0;--_ap-cell-line:#475569;--_ap-accent:#f8fafc;--_ap-on-accent:#020617;--_ap-accent-tint:#1e293b;--_ap-focus:#f8fafc;--_ap-focus-halo:transparent;--_ap-focus-inner:#020617;--_ap-danger:#f87171;--_ap-shadow-panel:none;--_ap-shadow-control:none;--_ap-hover-bg:#1e293b;--_ap-hover-line:#475569;--_ap-hover-ink:#f8fafc;--_ap-today-ring:transparent;--_ap-today-dot:transparent}:host([appearance=minimal][color-scheme=auto]){--_ap-surface:transparent;--_ap-surface-muted:transparent;--_ap-panel-surface:transparent;--_ap-ink:#f1f5f9;--_ap-ink-muted:#94a3b8;--_ap-ink-disabled:#475569;--_ap-line:transparent;--_ap-line-strong:#475569;--_ap-panel-line:#334155;--_ap-cell-line:transparent;--_ap-accent:#f1f5f9;--_ap-on-accent:#0f172a;--_ap-accent-tint:#1e293b;--_ap-focus:#f1f5f9;--_ap-focus-halo:transparent;--_ap-focus-inner:#0f172a;--_ap-danger:#f87171;--_ap-shadow-panel:none;--_ap-shadow-control:none;--_ap-hover-bg:transparent;--_ap-hover-line:transparent;--_ap-hover-ink:#f1f5f9;--_ap-today-ring:transparent;--_ap-today-dot:#f1f5f9}:host([color-scheme=auto]){--_ap-tone-neutral-bg:#334155;--_ap-tone-neutral-ink:#e2e8f0;--_ap-tone-info-bg:#1e3a5f;--_ap-tone-info-ink:#bfdbfe;--_ap-tone-success-bg:#14532d;--_ap-tone-success-ink:#bbf7d0;--_ap-tone-warning-bg:#4a3212;--_ap-tone-warning-ink:#fde68a;--_ap-tone-danger-bg:#4c1d1d;--_ap-tone-danger-ink:#fecaca;color-scheme:dark}}:host([size=xs]){--_sz-control:0.75;--_sz-font:-2px;--_sz-space:0.5;--_sz-nav:24px;--_sz-icon:12px;--_sz-width:300px;--_sz-min-width:260px;--_sz-min-floor:0px}:host([size=sm]){--_sz-control:0.85;--_sz-font:-1px;--_sz-space:0.75;--_sz-nav:28px;--_sz-icon:14px;--_sz-width:340px;--_sz-min-width:280px;--_sz-min-floor:0px}:host([size=md]){--_sz-control:1;--_sz-font:0px;--_sz-space:1;--_sz-nav:32px;--_sz-icon:16px;--_sz-width:380px;--_sz-min-width:300px;--_sz-min-floor:0px}:host([size=lg]){--_sz-control:1.15;--_sz-font:2px;--_sz-space:1.25;--_sz-nav:36px;--_sz-icon:18px;--_sz-width:440px;--_sz-min-width:340px;--_sz-min-floor:340px}:host([size=xl]){--_sz-control:1.3;--_sz-font:4px;--_sz-space:1.5;--_sz-nav:40px;--_sz-icon:20px;--_sz-width:500px;--_sz-min-width:380px;--_sz-min-floor:380px}@media (pointer:coarse){:host([appearance=sharp]){--_ap-cell-size:44px}}.ts-root{color:var(--_ink);font-family:var(--_font-latin);inline-size:100%;letter-spacing:normal;text-align:start;text-transform:none}.ts-root:lang(ar),.ts-root:lang(ckb){font-family:var(--_font-arabic)}.ts-frame{display:grid;grid-template-rows:var(--_rows,auto);margin:calc(var(--_ring)*-1);margin-block-end:calc((var(--_ring) + var(--_shadow-room))*-1);overflow:hidden;overflow:clip;padding:var(--_ring);padding-block-end:calc(var(--_ring) + var(--_shadow-room))}.ts-frame[data-measured]{transition:grid-template-rows var(--_settle) var(--_ease)}.ts-layer{align-self:start;grid-area:1/1;min-inline-size:0}.ts-layer[data-phase=leave]{animation:ts-fade-out calc(var(--_settle) - 60ms) var(--_ease) both;pointer-events:none}.ts-layer[data-phase=enter]:not([data-kind=times]){animation:ts-fade-in var(--_settle) var(--_ease) both}.ts-grid{display:grid;gap:var(--_gap);grid-template-columns:repeat(auto-fill,minmax(min(var(--_min-column),100%),1fr))}.ts-chip{align-items:center;background-color:var(--_surface);border:var(--_border-width) solid var(--_line);border-radius:var(--_radius);box-shadow:var(--_shadow);color:var(--_ink);cursor:pointer;display:inline-flex;font-size:var(--_font-size);font-variant-numeric:tabular-nums;font-weight:var(--_weight);justify-content:center;line-height:var(--_line-height);min-block-size:var(--_chip-height);min-inline-size:0;padding:0 var(--_chip-padding);transition:background-color calc(var(--_settle)/2) var(--_ease),border-color calc(var(--_settle)/2) var(--_ease),color calc(var(--_settle)/2) var(--_ease),transform calc(var(--_settle)/2) var(--_ease);-webkit-user-select:none;-moz-user-select:none;user-select:none;white-space:nowrap}.ts-layer[data-phase=enter] .ts-chip{animation:ts-rise var(--_settle) cubic-bezier(.16,1,.3,1) backwards}.ts-chip:hover{background-color:var(--_hover-bg);border-color:var(--_hover-line);color:var(--_hover-ink)}.ts-chip:focus-visible{box-shadow:0 0 0 var(--_halo-width) var(--_halo);outline:var(--_focus-width) solid var(--_focus);outline-offset:var(--_focus-offset)}.ts-chip[data-selected],.ts-chip[data-selected]:hover{background-color:var(--_accent);border-color:var(--_accent);color:var(--_on-accent);font-weight:var(--_selected-weight)}.ts-chip[aria-disabled=true]{color:var(--_disabled-ink);cursor:not-allowed;opacity:var(--_blocked-opacity);-webkit-text-decoration:var(--_blocked-decoration);text-decoration:var(--_blocked-decoration)}.ts-chip[aria-disabled=true]:hover{background-color:var(--_surface);border-color:var(--_line);color:var(--_disabled-ink)}.ts-chip[aria-disabled=true]:active{transform:none}.ts-skeleton{background-color:var(--_skeleton);background-image:linear-gradient(100deg,transparent 0,color-mix(in srgb,var(--_ink) 7%,transparent) 50%,transparent 100%);background-repeat:no-repeat;background-size:45% 100%;border-color:transparent;box-shadow:none;cursor:default}.ts-layer[data-phase=enter] .ts-skeleton,.ts-skeleton{animation:ts-sheen 1.4s linear infinite}.ts-empty{color:var(--_muted);font-size:var(--_empty-size);margin:0;padding-block:var(--_empty-padding);text-align:center}.ts-sr{clip:rect(0,0,0,0);block-size:1px;border-width:0;inline-size:1px;margin:-1px;overflow:hidden;padding:0;position:absolute;white-space:nowrap}@keyframes ts-rise{0%{opacity:0;transform:translateY(var(--_rise))}to{opacity:1;transform:none}}@keyframes ts-fade-in{0%{opacity:0}}@keyframes ts-fade-out{to{opacity:0}}@keyframes ts-sheen{0%{background-position:-100% 0}to{background-position:200% 0}}@media (pointer:coarse){.ts-chip{min-block-size:max(var(--_chip-height),44px)}}@media (prefers-reduced-motion:reduce){.ts-frame[data-measured]{transition:none}.ts-layer[data-phase=enter] .ts-chip,.ts-layer[data-phase=enter] .ts-skeleton,.ts-layer[data-phase],.ts-skeleton{animation:none}.ts-chip{transition:none}}.pointer-events-none{pointer-events:none}.static{position:static}.left-0{left:0}.top-0{top:0}.flex{display:flex}.h-full{height:100%}.min-h-\\[150px\\]{min-height:150px}.w-full{width:100%}.items-center{align-items:center}.justify-center{justify-content:center}.opacity-0{opacity:0}.transition-opacity{transition-duration:.15s;transition-property:opacity;transition-timing-function:cubic-bezier(.4,0,.2,1)}.duration-500{transition-duration:.5s}.bottom-4{bottom:1rem}.bottom-\\[88px\\]{bottom:88px}.left-1\\/2{left:50%}.z-10{z-index:10}.z-\\[9999\\]{z-index:9999}.m-0{margin:0}.aspect-video{aspect-ratio:16/9}.size-8{height:2rem;width:2rem}.size-\\[22px\\]{height:22px;width:22px}.size-\\[32px\\]{height:32px;width:32px}.size-full{height:100%;width:100%}.h-\\[100dvh\\]{height:100dvh}.h-\\[60px\\]{height:60px}.min-h-full{min-height:100%}.w-\\[100dvw\\]{width:100dvw}.w-\\[100px\\]{width:100px}.min-w-\\[140px\\]{min-width:140px}.min-w-full{min-width:100%}.-translate-x-1\\/2{--tw-translate-x:-50%;transform:translate(var(--tw-translate-x),var(--tw-translate-y)) rotate(var(--tw-rotate)) skewX(var(--tw-skew-x)) skewY(var(--tw-skew-y)) scaleX(var(--tw-scale-x)) scaleY(var(--tw-scale-y))}.animate-spin{animation:spin 1s linear infinite}.cursor-pointer{cursor:pointer}.justify-between{justify-content:space-between}.gap-2{gap:.5rem}.whitespace-nowrap{white-space:nowrap}.rounded-full{border-radius:9999px}.rounded-lg{border-radius:.5rem}.border-slate-500{--tw-border-opacity:1;border-color:rgb(100 116 139/var(--tw-border-opacity,1))}.border-slate-600{--tw-border-opacity:1;border-color:rgb(71 85 105/var(--tw-border-opacity,1))}.bg-black{--tw-bg-opacity:1;background-color:rgb(0 0 0/var(--tw-bg-opacity,1))}.bg-black\\/30{background-color:rgba(0,0,0,.3)}.bg-black\\/55{background-color:rgba(0,0,0,.55)}.bg-slate-100{--tw-bg-opacity:1;background-color:rgb(241 245 249/var(--tw-bg-opacity,1))}.bg-white{--tw-bg-opacity:1;background-color:rgb(255 255 255/var(--tw-bg-opacity,1))}.object-cover{object-fit:cover}.object-center{object-position:center}.p-1{padding:.25rem}.p-\\[16px\\]{padding:16px}.px-3{padding-left:.75rem;padding-right:.75rem}.px-6{padding-left:1.5rem;padding-right:1.5rem}.py-1{padding-bottom:.25rem;padding-top:.25rem}.py-\\[10px\\]{padding-bottom:10px;padding-top:10px}.text-center{text-align:center}.text-\\[13px\\]{font-size:13px}.text-\\[15px\\]{font-size:15px}.text-\\[18px\\]{font-size:18px}.font-medium{font-weight:500}.leading-\\[1\\.3\\]{line-height:1.3}.text-slate-100{--tw-text-opacity:1;color:rgb(241 245 249/var(--tw-text-opacity,1))}.text-slate-500{--tw-text-opacity:1;color:rgb(100 116 139/var(--tw-text-opacity,1))}.text-slate-600{--tw-text-opacity:1;color:rgb(71 85 105/var(--tw-text-opacity,1))}.text-white{--tw-text-opacity:1;color:rgb(255 255 255/var(--tw-text-opacity,1))}.shadow,.shadow-lg{box-shadow:var(--tw-ring-offset-shadow,0 0 #0000),var(--tw-ring-shadow,0 0 #0000),var(--tw-shadow)}.shadow-lg{--tw-shadow:0 10px 15px -3px rgba(0,0,0,.1),0 4px 6px -4px rgba(0,0,0,.1);--tw-shadow-colored:0 10px 15px -3px var(--tw-shadow-color),0 4px 6px -4px var(--tw-shadow-color)}.shadow-md{--tw-shadow:0 4px 6px -1px rgba(0,0,0,.1),0 2px 4px -2px rgba(0,0,0,.1);--tw-shadow-colored:0 4px 6px -1px var(--tw-shadow-color),0 2px 4px -2px var(--tw-shadow-color);box-shadow:var(--tw-ring-offset-shadow,0 0 #0000),var(--tw-ring-shadow,0 0 #0000),var(--tw-shadow)}.outline-none{outline:2px solid transparent;outline-offset:2px}.filter{filter:var(--tw-blur) var(--tw-brightness) var(--tw-contrast) var(--tw-grayscale) var(--tw-hue-rotate) var(--tw-invert) var(--tw-saturate) var(--tw-sepia) var(--tw-drop-shadow)}.transition-all{transition-duration:.15s;transition-property:all;transition-timing-function:cubic-bezier(.4,0,.2,1)}.transition-colors{transition-duration:.15s;transition-property:color,background-color,border-color,text-decoration-color,fill,stroke;transition-timing-function:cubic-bezier(.4,0,.2,1)}.duration-300{transition-duration:.3s}.hover\\:border-slate-700:hover{--tw-border-opacity:1;border-color:rgb(51 65 85/var(--tw-border-opacity,1))}.hover\\:bg-slate-300:hover{--tw-bg-opacity:1;background-color:rgb(203 213 225/var(--tw-bg-opacity,1))}.hover\\:text-slate-700:hover{--tw-text-opacity:1;color:rgb(51 65 85/var(--tw-text-opacity,1))}.disabled\\:bg-white\\/75:disabled{background-color:hsla(0,0%,100%,.75)}@media (min-width:768px){.md\\:relative{position:relative}.md\\:h-auto{height:auto}.md\\:w-\\[600px\\]{width:600px}.md\\:\\!translate-x-0{--tw-translate-x:0px!important;transform:translate(var(--tw-translate-x),var(--tw-translate-y)) rotate(var(--tw-rotate)) skewX(var(--tw-skew-x)) skewY(var(--tw-skew-y)) scaleX(var(--tw-scale-x)) scaleY(var(--tw-scale-y))!important}.md\\:overflow-hidden{overflow:hidden}.md\\:rounded-lg{border-radius:.5rem}.md\\:border-none{border-style:none}.md\\:bg-white{--tw-bg-opacity:1;background-color:rgb(255 255 255/var(--tw-bg-opacity,1))}.md\\:py-\\[8px\\]{padding-bottom:8px;padding-top:8px}.md\\:text-\\[24px\\]{font-size:24px}.md\\:text-black{--tw-text-opacity:1;color:rgb(0 0 0/var(--tw-text-opacity,1))}.md\\:\\!opacity-100{opacity:1!important}.md\\:transition-all{transition-duration:.15s;transition-property:all;transition-timing-function:cubic-bezier(.4,0,.2,1)}.md\\:duration-300{transition-duration:.3s}.md\\:hover\\:bg-slate-100:hover{--tw-bg-opacity:1;background-color:rgb(241 245 249/var(--tw-bg-opacity,1))}}.visible{visibility:visible}.contents{display:contents}';
  }
}, [1, "shift-time-slots", { times: [1], value: [1537], disabledTimes: [1, "disabled-times"], hourCycle: [1, "hour-cycle"], language: [1], numerals: [1], label: [1], emptyText: [1, "empty-text"], loading: [516], skeletonCount: [2, "skeleton-count"], disabled: [516], appearance: [513], colorScheme: [513, "color-scheme"], size: [513], shown: [32], leaving: [32], focusTime: [32], setFocus: [64] }, void 0, { times: [{ onContentChange: 0 }], loading: [{ onContentChange: 0 }] }]);
function rv() {
  typeof customElements < "u" && ["shift-time-slots"].forEach(((n) => {
    n === "shift-time-slots" && (customElements.get(n) || customElements.define(n, $_));
  }));
}
rv();
/*!
 * Built by ShiftSoftware
 * Copyright (c)
 */
const U_ = /^\d{4}-\d{2}-\d{2}$/;
function L_(n) {
  if (n && U_.test(n)) {
    const i = /* @__PURE__ */ new Date(`${n}T00:00:00.000Z`);
    if (!Number.isNaN(i.getTime()) && i.toISOString().slice(0, 10) === n) return i;
  }
  return /* @__PURE__ */ new Date();
}
function H_(n) {
  return L_(n).toISOString().slice(0, 10);
}
/*!
 * Built by ShiftSoftware
 * Copyright (c)
 */
const ia = 12, xn = (n, i) => 12 * n + i - 1, cm = (n) => [Math.floor(n / 12), n % 12 + 1], En = (n) => Number(n.slice(0, 4)), lo = (n, i) => !!n && !!i && n > i;
function fm(n, i, s, r) {
  const c = `${String(n).padStart(4, "0")}-${String(i).padStart(2, "0")}`;
  return !!lo(s, r) || !!r && ci(n, i, 1) > r || !!s && Gr(c) < s;
}
function dm(n, i, s) {
  return !!lo(i, s) || !!s && n > En(s) || !!i && n < En(i);
}
function ef(n, i, s) {
  return [i ? n === "months" ? xn(En(i), Number(i.slice(5, 7))) : En(i) : -1 / 0, s ? n === "months" ? xn(En(s), Number(s.slice(5, 7))) : En(s) : 1 / 0];
}
function al(n, i, s, r, c) {
  if (lo(r, c)) return !1;
  const [f, d] = ef(n, r, c), h = n === "months" ? xn(i, 1) : i;
  return s < 0 ? h > f : h + ia - 1 < d;
}
const Y_ = ["neutral", "info", "success", "warning", "danger"], B_ = { months: { base: "month", selected: "month-selected", today: "month-today", disabled: "month-disabled" }, years: { base: "year", selected: "year-selected", today: "year-today", disabled: "year-disabled" } };
function pm(n) {
  if (!n) return {};
  if (typeof n != "string") return n;
  try {
    const i = JSON.parse(n);
    return i && typeof i == "object" && !Array.isArray(i) ? i : {};
  } catch {
    return {};
  }
}
const hm = { days: 0, months: 1, years: 2 }, us = (n) => n.view === "days" ? "days" : Qr(n), Qr = (n) => n.view === "days" ? `d${n.month}` : n.view === "months" ? `m${n.year}` : `y${n.start}`;
function Cc(n, i) {
  if (!n || typeof getComputedStyle != "function") return 0;
  const s = getComputedStyle(n).transform, r = s && s !== "none" ? s.match(/matrix\(([^)]+)\)/) : null;
  if (!r) return 0;
  const c = r[1].split(",").map(Number);
  return (i === "x" ? c[4] : c[5]) || 0;
}
const V_ = hf(class extends of {
  constructor(n) {
    super(), n !== !1 && this.__registerHost(), this.__attachShadow(), this.dateChange = Tn(this, "dateChange"), this.monthChange = Tn(this, "monthChange"), this.viewChange = Tn(this, "viewChange"), this.pickerChange = Tn(this, "pickerChange"), this.value = "", this.highlightToday = !1, this.language = "en", this.disabled = !1, this.busy = !1, this.disableViews = !1, this.showToday = !0, this.colorScheme = "light", this.size = "md", this.view = "days", this.pageYear = 0, this.yearsStart = 0, this.focusDate = "", this.focusIndex = 0, this.leavingPage = null, this.leavingTitle = null, this.leavingYear = null, this.leavingMonthName = null, this.leavingView = null, this.tip = { text: "", shown: !1 }, this.shownMonth = "", this.emitMonthChange = !1, this.pendingFocus = null, this.returnView = "days", this.monthsOpener = "month-button", this.observedHeader = null, this.loaded = !1, this.headingId = `shift-calendar-heading-${Math.random().toString(36).slice(2, 10)}`, this.onSettled = (i, s) => {
      i.target === i.currentTarget && (s === "page" && (this.leavingPage = null), s === "title" && (this.leavingTitle = null), s === "year" && (this.leavingYear = null), s === "month" && (this.leavingMonthName = null), s === "view" && (this.leavingView = null));
    }, this.onRootKeyDown = (i) => {
      i.key === "Escape" && this.view !== "days" && (i.preventDefault(), i.stopPropagation(), this.closeView());
    }, this.tipCell = null, this.onGridPointerOver = (i) => {
      var s, r;
      i.pointerType === "mouse" && this.showTip((r = (s = i.target) === null || s === void 0 ? void 0 : s.closest) === null || r === void 0 ? void 0 : r.call(s, ".cal-day"));
    }, this.onGridFocusIn = (i) => {
      var s, r, c;
      const f = (r = (s = i.target) === null || s === void 0 ? void 0 : s.closest) === null || r === void 0 ? void 0 : r.call(s, ".cal-day");
      !((c = f == null ? void 0 : f.matches) === null || c === void 0) && c.call(f, ":focus-visible") ? this.showTip(f) : this.hideTip();
    }, this.onGridLeave = () => this.hideTip();
  }
  componentWillLoad() {
    var n;
    const i = this.clampToRange((n = nm(this.month)) !== null && n !== void 0 ? n : this.openingMonth());
    this.shownMonth = i, this.month = i, this.pageYear = bt(i)[0], this.focusDate = this.focusFor(i);
  }
  componentDidLoad() {
    this.loaded = !0;
  }
  componentDidRender() {
    var n, i;
    if (this.watchHeader(), this.placeTip(), !this.pendingFocus) return;
    const s = this.pendingFocus;
    this.pendingFocus = null, s === "grid" ? this.focusCell() : (i = (n = this.el.shadowRoot) === null || n === void 0 ? void 0 : n.querySelector(`.cal-title[data-current] [part~="${s}"]`)) === null || i === void 0 || i.focus({ preventScroll: !0 });
  }
  disconnectedCallback() {
    var n;
    clearTimeout(this.settleTimer), (n = this.headerObserver) === null || n === void 0 || n.disconnect(), this.observedHeader = null;
  }
  watchHeader() {
    var n, i, s;
    const r = (n = this.el.shadowRoot) === null || n === void 0 ? void 0 : n.querySelector(".cal-header");
    r && r !== this.observedHeader && typeof ResizeObserver < "u" && ((i = this.headerObserver) === null || i === void 0 || i.disconnect(), this.headerObserver = new ResizeObserver((() => this.fitTodayLabel())), this.headerObserver.observe(r), this.observedHeader = r, this.fitTodayLabel(), (s = document.fonts) === null || s === void 0 || s.ready.then((() => this.fitTodayLabel())));
  }
  fitTodayLabel() {
    var n;
    const i = (n = this.el.shadowRoot) === null || n === void 0 ? void 0 : n.querySelector(".cal-root"), s = i == null ? void 0 : i.querySelector(".cal-header"), r = i == null ? void 0 : i.querySelector(".cal-measure"), c = i == null ? void 0 : i.querySelector('.cal-nav [part="today"]');
    if (!(i && s && r && c)) return;
    const f = (m) => m.getBoundingClientRect().width, d = Math.max(...Array.from(r.querySelectorAll(".cal-measure-title")).map(f)), h = f(r.querySelector(".cal-measure-today")), g = f(i.querySelector(".cal-nav")) - f(c) + h, v = parseFloat(getComputedStyle(i.querySelector(".cal-title-band")).marginInlineStart) || 0, y = parseFloat(getComputedStyle(s).columnGap) || 0;
    i.toggleAttribute("data-today-label", d + v + y + g <= f(s));
  }
  onMonthChange(n) {
    var i;
    const s = this.clampToRange((i = nm(n)) !== null && i !== void 0 ? i : this.shownMonth || this.openingMonth());
    if (s !== n) return void (this.month = s);
    const r = this.shownMonth, c = this.emitMonthChange;
    this.emitMonthChange = !1, s !== r && (this.view === "days" && this.animateFrom(Object.assign(Object.assign({}, this.snapshot()), { month: r })), this.shownMonth = s, Le(this.focusDate) !== s && (this.focusDate = this.focusFor(s, this.focusDate)), c && this.monthChange.emit({ month: s, firstDate: il(s), lastDate: Gr(s) }));
  }
  onValueChange(n) {
    const i = xt(n);
    i && (Le(i) !== this.month && this.showMonth(Le(i)), Ya(i, this.bounds) && (this.focusDate = i));
  }
  onRangeChange() {
    var n;
    this.showMonth(this.clampToRange((n = this.month) !== null && n !== void 0 ? n : this.shownMonth)), this.focusDate = this.focusFor(this.month, this.focusDate);
  }
  onDisableViews(n) {
    n && this.view !== "days" && this.setView("days");
  }
  async setFocus(n) {
    const i = xt(n);
    if (i && this.view === "days" && Le(i) === this.month && Ya(i, this.bounds) && i !== this.focusDate) return this.focusDate = i, void (this.pendingFocus = "grid");
    this.focusCell();
  }
  get minDate() {
    return xt(this.min);
  }
  get maxDate() {
    return xt(this.max);
  }
  get bounds() {
    return { min: this.minDate, max: this.maxDate };
  }
  get rangeIsEmpty() {
    return !!this.minDate && !!this.maxDate && this.minDate > this.maxDate;
  }
  clampToRange(n) {
    return this.rangeIsEmpty ? n : C_(n, this.minDate, this.maxDate);
  }
  get todayDate() {
    return xt(this.today) ? H_(this.today) : nv();
  }
  get locale() {
    return _f(this.language);
  }
  get rtl() {
    return this.locale.direction === "rtl";
  }
  get weekStart() {
    return A_(this.weekStartsOn, this.locale.weekStartsOn);
  }
  get digits() {
    return this.numerals === "latn" || this.numerals === "arab" ? this.numerals : this.locale.numerals;
  }
  availability() {
    return lv({ min: this.min, max: this.max, disabledDates: this.disabledDates, disabledWeekdays: this.disabledWeekdays, enabledDates: this.enabledDates, isDateDisabled: this.isDateDisabled });
  }
  snapshot() {
    return { view: this.view, month: this.month, year: this.pageYear, start: this.yearsStart };
  }
  openingMonth() {
    const n = xt(this.value);
    return Le(n ?? this.todayDate);
  }
  focusFor(n, i) {
    const s = xt(this.value), r = this.todayDate, [c, f] = bt(n);
    if (this.rangeIsEmpty) return il(n);
    let d;
    d = s && Le(s) === n && Ya(s, this.bounds) ? s : Le(r) === n && Ya(r, this.bounds) ? r : i ? ci(c, f, Math.min(Dn(i)[2], io(c, f))) : il(n);
    const h = this.minDate && this.minDate > il(n) ? this.minDate : il(n), g = this.maxDate && this.maxDate < Gr(n) ? this.maxDate : Gr(n);
    return lm(d, h, g);
  }
  showMonth(n) {
    n !== this.month && (this.emitMonthChange = !0, this.month = n, this.emitMonthChange = !1);
  }
  animateFrom(n) {
    const i = this.snapshot(), s = this.el.shadowRoot;
    if (clearTimeout(this.settleTimer), !this.loaded || typeof window < "u" && ((r = window.matchMedia) === null || r === void 0 ? void 0 : r.call(window, "(prefers-reduced-motion: reduce)").matches)) this.leavingPage = this.leavingTitle = this.leavingView = this.leavingYear = this.leavingMonthName = null;
    else {
      var r;
      if (us(n) !== us(i)) this.leavingTitle = { snapshot: n, offset: Cc(s == null ? void 0 : s.querySelector(".cal-title[data-current]"), "y"), travel: 1 }, this.leavingYear = this.leavingMonthName = null;
      else if (i.view === "days") {
        const [c, f] = bt(n.month), [d, h] = bt(i.month), g = (v) => Cc(s == null ? void 0 : s.querySelector(`.cal-title[data-current] [data-slot="${v}"] .cal-slot-layer[data-current]`), "y");
        c !== d && (this.leavingYear = { value: c, offset: g("year") }), f !== h && (this.leavingMonthName = { value: f, offset: g("month") });
      }
      if (n.view !== i.view) this.leavingView = n, this.leavingPage = null;
      else if (Qr(n) !== Qr(i)) {
        const c = n.view === "days" ? i.month > n.month : n.view === "months" ? i.year > n.year : i.start > n.start;
        this.leavingPage = { snapshot: n, offset: Cc(s == null ? void 0 : s.querySelector(".cal-page[data-current]"), "x"), travel: c ? 1 : -1 };
      }
      this.settleTimer = setTimeout((() => this.leavingPage = this.leavingTitle = this.leavingView = this.leavingYear = this.leavingMonthName = null), 1500);
    }
  }
  setView(n, i) {
    if (n === this.view) return;
    const s = this.snapshot();
    this.view = n, this.animateFrom(s), i && (this.pendingFocus = i), this.viewChange.emit({ view: n });
  }
  openMonths() {
    const [n, i] = bt(this.month);
    this.monthsOpener = "month-button", this.pageYear = n, this.focusIndex = this.clampIndex("months", xn(n, i)), this.setView("months", "grid");
  }
  openYears() {
    const n = this.view === "months" ? this.pageYear : bt(this.month)[0];
    this.returnView = this.view, this.yearsStart = ((i) => i - 6)(n), this.focusIndex = this.clampIndex("years", n), this.setView("years", "grid");
  }
  closeView() {
    if (this.view !== "months") return this.returnView === "months" ? (this.focusIndex = this.clampIndex("months", xn(this.pageYear, bt(this.month)[1])), void this.setView("months", "year-button")) : void this.setView("days", "year-button");
    this.setView("days", this.monthsOpener);
  }
  clampIndex(n, i) {
    const [s, r] = ef(n, this.minDate, this.maxDate);
    return Math.min(r, Math.max(s, i));
  }
  chooseYear(n) {
    if (this.disabled || dm(n, this.minDate, this.maxDate)) return;
    const i = this.snapshot(), s = this.clampToRange(`${String(n).padStart(4, "0")}-${this.month.slice(5, 7)}`);
    this.pageYear = n, this.focusIndex = this.clampIndex("months", xn(n, bt(s)[1])), this.monthsOpener = "year-button", this.view = "months", this.showMonth(s), this.animateFrom(i), this.pendingFocus = "grid", this.viewChange.emit({ view: "months" });
  }
  chooseMonth(n) {
    const [i, s] = cm(n);
    if (this.disabled || fm(i, s, this.minDate, this.maxDate)) return;
    const r = this.snapshot();
    this.view = "days", this.showMonth(`${String(i).padStart(4, "0")}-${String(s).padStart(2, "0")}`), this.focusDate = this.focusFor(this.month, this.focusDate), this.animateFrom(r), this.pendingFocus = "month-button", this.viewChange.emit({ view: "days" });
  }
  focusCell() {
    var n;
    const i = (n = this.el.shadowRoot) === null || n === void 0 ? void 0 : n.querySelector('.cal-view[data-current] .cal-page[data-current] [tabindex="0"]');
    i == null || i.focus({ preventScroll: !0 });
  }
  select(n, i) {
    this.disabled || this.busy || Le(n) !== this.month || Pc(n, i) || (this.value = n, this.dateChange.emit({ value: n }), this.pickerChange.emit({ value: n, label: Pr(n, this.locale, this.digits), complete: !0 }));
  }
  page(n) {
    if (this.disabled || this.rangeIsEmpty) return;
    if (this.view === "days") return void this.showMonth(this.clampToRange(av(this.month, n)));
    const i = this.snapshot();
    if (this.view === "months") {
      if (!al("months", this.pageYear, n, this.minDate, this.maxDate)) return;
      this.pageYear += n, this.focusIndex = this.clampIndex("months", this.focusIndex + n * ia);
    } else {
      if (!al("years", this.yearsStart, n, this.minDate, this.maxDate)) return;
      this.yearsStart += n * ia, this.focusIndex = this.clampIndex("years", this.focusIndex + n * ia);
    }
    this.animateFrom(i);
  }
  goToToday() {
    const n = this.todayDate;
    if (!this.disabled && Ya(n, this.bounds)) {
      if (this.view !== "days") {
        const i = this.snapshot();
        this.view = "days", this.showMonth(Le(n)), this.animateFrom(i), this.viewChange.emit({ view: "days" });
      } else this.showMonth(Le(n));
      this.focusDate = n, this.pendingFocus = "grid", this.select(n, this.availability());
    }
  }
  onDaysKeyDown(n, i) {
    if (this.disabled || n.altKey || n.ctrlKey || n.metaKey) return;
    if (n.key === "Enter" || n.key === " ") return n.preventDefault(), void this.select(this.focusDate, i);
    if (this.rangeIsEmpty) return;
    const s = (function(r, { key: c, shiftKey: f }, d) {
      let h;
      switch (c) {
        case (d.rtl ? "ArrowLeft" : "ArrowRight"):
          h = ni(r, 1);
          break;
        case (d.rtl ? "ArrowRight" : "ArrowLeft"):
          h = ni(r, -1);
          break;
        case "ArrowDown":
          h = ni(r, 7);
          break;
        case "ArrowUp":
          h = ni(r, -7);
          break;
        case "Home":
          h = yf(r, d.weekStartsOn);
          break;
        case "End":
          h = O_(r, d.weekStartsOn);
          break;
        case "PageUp":
          h = f ? im(r, -1) : Ic(r, -1);
          break;
        case "PageDown":
          h = f ? im(r, 1) : Ic(r, 1);
          break;
        default:
          return null;
      }
      return lm(h, d.min, d.max);
    })(this.focusDate, n, { rtl: this.rtl, weekStartsOn: this.weekStart, min: this.minDate, max: this.maxDate });
    s && (n.preventDefault(), Le(s) !== this.month && this.showMonth(Le(s)), this.focusDate = s, this.pendingFocus = "grid");
  }
  onPickerKeyDown(n, i) {
    if (this.disabled || n.altKey || n.ctrlKey || n.metaKey) return;
    if (n.key === "Enter" || n.key === " ") return n.preventDefault(), void (i === "months" ? this.chooseMonth(this.focusIndex) : this.chooseYear(this.focusIndex));
    const s = i === "months" ? xn(this.pageYear, 1) : this.yearsStart, [r, c] = ef(i, this.minDate, this.maxDate), f = (function(h, g, v) {
      const y = ((h - v.pageStart) % 3 + 3) % 3;
      let m;
      switch (g) {
        case (v.rtl ? "ArrowLeft" : "ArrowRight"):
          m = h + 1;
          break;
        case (v.rtl ? "ArrowRight" : "ArrowLeft"):
          m = h - 1;
          break;
        case "ArrowDown":
          m = h + 3;
          break;
        case "ArrowUp":
          m = h - 3;
          break;
        case "Home":
          m = h - y;
          break;
        case "End":
          m = h + (2 - y);
          break;
        case "PageUp":
          m = h - ia;
          break;
        case "PageDown":
          m = h + ia;
          break;
        default:
          return null;
      }
      return Math.min(v.high, Math.max(v.low, m));
    })(this.focusIndex, n.key, { rtl: this.rtl, pageStart: s, low: r, high: c });
    if (f === null) return;
    n.preventDefault();
    const d = (function(h, g, v) {
      if (h === "months") return Math.floor(g / 12);
      let y = v;
      for (; g < y; ) y -= ia;
      for (; g > y + ia - 1; ) y += ia;
      return y;
    })(i, f, i === "months" ? this.pageYear : this.yearsStart);
    if (d !== (i === "months" ? this.pageYear : this.yearsStart)) {
      const h = this.snapshot();
      i === "months" ? this.pageYear = d : this.yearsStart = d, this.animateFrom(h);
    }
    this.focusIndex = f, this.pendingFocus = "grid";
  }
  showTip(n) {
    var i;
    const s = n != null && n.dataset.date ? (i = pm(this.dayMeta)[n.dataset.date]) === null || i === void 0 ? void 0 : i.tooltip : "";
    if (!n || !s || n.getAttribute("aria-disabled") === "true") return this.hideTip();
    this.tipCell = n, this.tip = { text: String(s), shown: !0 };
  }
  hideTip() {
    this.tipCell = null, this.tip.shown && (this.tip = Object.assign(Object.assign({}, this.tip), { shown: !1 }));
  }
  placeTip() {
    var n, i;
    const s = (n = this.el.shadowRoot) === null || n === void 0 ? void 0 : n.querySelector(".cal-tip"), r = (i = this.el.shadowRoot) === null || i === void 0 ? void 0 : i.querySelector(".cal-root"), c = this.tipCell;
    if (!(s && r && c && this.tip.shown && c.isConnected)) return;
    const f = r.getBoundingClientRect(), d = c.getBoundingClientRect(), h = s.offsetWidth / 2, g = Math.min(Math.max(d.left + d.width / 2 - f.left, h + 4), f.width - h - 4), v = d.top - f.top - s.offsetHeight - 6 < 0;
    s.style.setProperty("--_tip-x", `${g}px`), s.style.setProperty("--_tip-y", (v ? d.bottom - f.top + 6 : d.top - f.top - s.offsetHeight - 6) + "px");
  }
  onCellClick(n, i) {
    this.hideTip(), !this.disabled && Le(n) === this.month && Ya(n, i) && (this.focusDate = n, this.select(n, i));
  }
  titleText(n) {
    const i = this.locale.strings, [s, r] = bt(n.month);
    return n.view === "days" ? `${i.months[r - 1]} ${nt(s, this.digits)}` : n.view === "months" ? nt(n.year, this.digits) : `${nt(n.start, this.digits)}–${nt(n.start + ia - 1, this.digits)}`;
  }
  renderTitle(n, i) {
    var s, r;
    const c = !i, f = this.locale.strings, [d, h] = bt(n.month), g = (function(T, A) {
      if (lo(T, A)) return { month: !1, year: !1 };
      const z = !!T && !!A && En(T) === En(A);
      return { month: !(T && A && Le(T) === Le(A)), year: !z };
    })(this.minDate, this.maxDate), v = !this.disableViews && !this.disabled, y = n.view === "months" ? n.year : d, m = !c || void 0, _ = (T, A) => X("button", { type: "button", class: "cal-title-button cal-year", part: A ? "year-button" : void 0, tabindex: c && A ? void 0 : "-1", disabled: !v || !g.year, "aria-label": `${f.chooseYear}, ${nt(T, this.digits)}`, onClick: () => this.openYears() }, nt(T, this.digits)), x = (T, A, z, q) => {
      var $;
      return X("span", { class: "cal-slot", "data-slot": T }, z && z.value !== A && X("span", { key: `${T}-${z.value}`, class: "cal-slot-layer", "data-phase": "leave", style: { "--_offset": `${z.offset}px` }, "aria-hidden": "true", inert: !0 }, q(z.value, !1)), X("span", { key: `${T}-${A}`, class: "cal-slot-layer", "data-current": "", "data-phase": z && z.value !== A ? "enter" : "rest", style: { "--_offset": `${($ = z == null ? void 0 : z.offset) !== null && $ !== void 0 ? $ : 0}px` }, onAnimationEnd: (j) => this.onSettled(j, T) }, q(A, !0)));
    };
    return X("div", { key: `title-${us(n)}`, class: "cal-title", part: "heading", "data-current": c ? "" : void 0, "data-phase": i ? "leave" : this.leavingTitle ? "enter" : "rest", style: { "--_offset": `${(r = (s = i ?? this.leavingTitle) === null || s === void 0 ? void 0 : s.offset) !== null && r !== void 0 ? r : 0}px` }, "aria-hidden": c ? void 0 : "true", inert: m, onAnimationEnd: c ? (T) => this.onSettled(T, "title") : void 0 }, n.view === "days" && [x("year", d, c ? this.leavingYear : null, _), x("month", h, c ? this.leavingMonthName : null, ((T, A) => X("button", { type: "button", class: "cal-title-button", part: A ? "month-button" : void 0, tabindex: c && A ? void 0 : "-1", disabled: !v || !g.month, "aria-label": `${f.chooseMonth}, ${f.months[T - 1]}`, onClick: () => this.openMonths() }, f.months[T - 1])))], n.view === "months" && _(y, !0), n.view === "years" && X("span", { class: "cal-title-text" }, this.titleText(n)));
  }
  pageAttributes(n, i, s) {
    var r, c;
    const f = i ?? this.leavingPage;
    return { key: `page-${Qr(n)}`, class: "cal-page", "data-current": i ? void 0 : "", "data-phase": i ? "leave" : this.leavingPage ? "enter" : "rest", "data-travel": f ? f.travel > 0 ? "forward" : "back" : void 0, style: { "--_offset": `${(r = f == null ? void 0 : f.offset) !== null && r !== void 0 ? r : 0}px`, "--_travel": String((c = f == null ? void 0 : f.travel) !== null && c !== void 0 ? c : 1) }, "aria-hidden": i ? "true" : void 0, inert: !!i || void 0, onAnimationEnd: s && !i ? (d) => this.onSettled(d, "page") : void 0 };
  }
  renderDaysPage(n, i, s, r, c) {
    const f = xt(this.value), d = this.todayDate, h = D_(n.month, this.weekStart), g = Array.from({ length: 6 }, ((y, m) => h.slice(7 * m, 7 * m + 7))), v = c && !r;
    return X("div", Object.assign({}, this.pageAttributes(n, r, c)), g.map(((y) => X("div", { role: "row", class: "cal-week", part: "week" }, y.map((({ date: m, inMonth: _ }) => {
      const x = _ ? Pc(m, i) : null, T = q_(x), A = _ && m === f, z = _ && m === d, q = z && this.highlightToday, $ = v && _ && !this.disabled && Ya(m, i), j = _ ? s[m] : void 0, L = j != null && j.badge ? String(j.badge) : "", V = j != null && j.tone && Y_.includes(j.tone) ? j.tone : "neutral", J = j != null && j.description ? `, ${j.description}` : "", Z = !_ || !!x || this.disabled, F = ["day", !_ && "day-outside", A && "day-selected", q && "day-today", Z && "day-disabled", T === "closed" && "day-closed"].filter(Boolean).join(" ");
      return X("div", { role: "gridcell", class: { "cal-day": !0, "has-badge": !!L }, part: F, "data-date": _ ? m : void 0, "data-outside": _ ? void 0 : "", "data-kind": _ ? T || "open" : void 0, "data-reason": x || void 0, "data-selected": A ? "" : void 0, "data-today": q ? "" : void 0, tabindex: $ ? m === this.focusDate ? "0" : "-1" : void 0, "aria-selected": A ? "true" : "false", "aria-disabled": Z ? "true" : void 0, "aria-current": z ? "date" : void 0, onClick: v ? () => this.onCellClick(m, i) : void 0 }, X("span", { class: "cal-day-number", part: "day-number", "aria-hidden": "true" }, nt(Dn(m)[2], this.digits)), X("span", { class: "cal-badge", part: "badge", "data-tone": V, "data-empty": L ? void 0 : "", "aria-hidden": "true" }, nt(L, this.digits)), X("span", { class: "cal-sr" }, Pr(m, this.locale, this.digits) + (_ ? J : "")));
    }))))));
  }
  renderPickerPage(n, i, s, r) {
    const c = this.locale.strings, f = this.todayDate, [d, h] = Dn(f), [g, v] = bt(this.month), y = r && !s, m = Array.from({ length: ia }, n === "months" ? (x, T) => xn(i.year, T + 1) : (x, T) => i.start + T), _ = Array.from({ length: 4 }, ((x, T) => m.slice(3 * T, 3 * T + 3)));
    return X("div", Object.assign({}, this.pageAttributes(i, s, r)), _.map(((x) => X("div", { role: "row", class: "cal-picker-row" }, x.map(((T) => {
      const [A, z] = n === "months" ? cm(T) : [T, 0], q = n === "months" ? fm(A, z, this.minDate, this.maxDate) : dm(A, this.minDate, this.maxDate), $ = n === "months" ? A === g && z === v : A === g, j = (n === "months" ? A === d && z === h : A === d) && this.highlightToday, L = q || this.disabled, V = n === "months" ? c.months[z - 1] : nt(A, this.digits), J = B_[n], Z = [J.base, $ && J.selected, j && J.today, L && J.disabled].filter(Boolean).join(" ");
      return X("div", { role: "gridcell", class: "cal-pick", part: Z, "data-index": String(T), "data-selected": $ ? "" : void 0, "data-today": j ? "" : void 0, "data-kind": L ? "unavailable" : "open", tabindex: y && !this.disabled ? T === this.focusIndex ? "0" : "-1" : void 0, "aria-selected": $ ? "true" : "false", "aria-disabled": L ? "true" : void 0, onClick: y ? () => n === "months" ? this.chooseMonth(T) : this.chooseYear(T) : void 0 }, X("span", { class: "cal-pick-text" }, V));
    }))))));
  }
  renderView(n, i, s, r) {
    const c = this.locale.strings, f = Array.from({ length: 7 }, ((g, v) => (this.weekStart + v) % 7)), d = r && this.leavingPage && this.leavingPage.snapshot.view === n.view ? this.leavingPage : null, h = (g, v) => g.view === "days" ? this.renderDaysPage(g, i, s, v, r) : this.renderPickerPage(g.view, g, v, r);
    return X("div", { key: `view-${n.view}`, class: "cal-view", "data-view": n.view, "data-current": r ? "" : void 0, "data-phase": r ? this.leavingView ? "enter" : "rest" : "leave", "data-shift": this.leavingView ? hm[this.view] > hm[this.leavingView.view] ? "deeper" : "shallower" : void 0, "aria-hidden": r ? void 0 : "true", inert: !r || void 0, onAnimationEnd: r ? (g) => this.onSettled(g, "view") : void 0 }, X("div", { role: "grid", class: "cal-grid", part: n.view === "days" ? "grid" : n.view, "aria-labelledby": r ? this.headingId : void 0, "aria-disabled": this.disabled ? "true" : void 0, "aria-busy": this.busy && n.view === "days" ? "true" : void 0, onKeyDown: r ? (g) => n.view === "days" ? this.onDaysKeyDown(g, i) : this.onPickerKeyDown(g, n.view) : void 0 }, n.view === "days" && X("div", { role: "row", class: "cal-weekdays", part: "weekdays" }, f.map(((g) => X("div", { role: "columnheader", class: "cal-weekday", part: "weekday" }, X("span", { class: "cal-weekday-short", "aria-hidden": "true" }, c.weekdaysShort[g]), X("span", { class: "cal-weekday-narrow", "aria-hidden": "true" }, c.weekdaysNarrow[g]), X("span", { class: "cal-sr" }, c.weekdays[g]))))), X("div", { class: "cal-stage" }, d && h(d.snapshot, d), h(n, null))));
  }
  render() {
    const n = this.locale, i = n.strings, s = this.snapshot(), r = this.availability(), c = pm(this.dayMeta), f = !this.disabled && !this.rangeIsEmpty, [d, h] = this.view === "days" ? [f && !(this.minDate && this.month <= Le(this.minDate)), f && !(this.maxDate && this.month >= Le(this.maxDate))] : this.view === "months" ? [f && al("months", this.pageYear, -1, this.minDate, this.maxDate), f && al("months", this.pageYear, 1, this.minDate, this.maxDate)] : [f && al("years", this.yearsStart, -1, this.minDate, this.maxDate), f && al("years", this.yearsStart, 1, this.minDate, this.maxDate)], [g, v] = this.view === "days" ? [i.previousMonth, i.nextMonth] : this.view === "months" ? [i.previousYear, i.nextYear] : [i.previousYears, i.nextYears], y = !this.disabled && Ya(this.todayDate, this.bounds), m = this.leavingTitle && us(this.leavingTitle.snapshot) !== us(s) ? this.leavingTitle : null, _ = this.leavingView && this.leavingView.view !== this.view ? this.leavingView : null;
    return X(to, { key: "63228582c88f6da3a8b05c7c7d53e381f1832772" }, X("div", { key: `${this.appearance || "vanilla"}-${this.size}`, class: "cal-root", part: "root", dir: n.direction, lang: n.language === "ku" ? "ckb" : n.language, role: "group", "aria-label": this.label || i.calendar, "aria-disabled": this.disabled ? "true" : void 0, "data-busy": this.busy ? "" : void 0, "data-view": this.view, onKeyDown: this.onRootKeyDown }, X("div", { key: "d4811b5a613ca16c02c430f4c473441fd50c8bd1", class: "cal-header", part: "header" }, X("div", { key: "6e55d0cff0aba4de6678dd90f413d908da215ddd", class: "cal-title-band" }, m && this.renderTitle(m.snapshot, m), this.renderTitle(s, null)), X("div", { key: "c18af2326fcffb1c178b00e54aa4620ef46a1b47", class: "cal-measure", "aria-hidden": "true" }, i.months.map(((x) => X("div", { class: "cal-title cal-measure-title" }, X("span", { class: "cal-title-button cal-year" }, nt(bt(this.month)[0], this.digits)), X("span", { class: "cal-title-button" }, x)))), X("span", { key: "d8819541affa28da297d8f1106b3cc44e3e327ff", class: "cal-button cal-today cal-measure-today" }, X("svg", { key: "c43194b3107ad5e37e05a91b0c97bd4b44130528", class: "cal-today-icon", viewBox: "0 0 16 16", focusable: "false" }), X("span", { key: "816ced19112dc5f0010cf308927207306247663c", class: "cal-today-text" }, i.today))), X("span", { key: "83eb90adbcef563da0a44c2d430803c9e3467bdc", class: "cal-sr", id: this.headingId, "aria-live": "polite" }, this.titleText(s)), X("div", { key: "7719ae8b3c520253b67203f18b2dfac94b8e354e", class: "cal-nav", part: "nav" }, X("button", { key: "113532b244d9ca91251bb9584d5dc9dd8cc8eb73", type: "button", class: "cal-button cal-today", part: "today", "aria-label": i.today, "aria-hidden": this.showToday ? void 0 : "true", "data-hidden": this.showToday ? void 0 : "", tabindex: this.showToday ? void 0 : "-1", disabled: !y || !this.showToday, onClick: () => this.goToToday() }, X("svg", { key: "7c244900ad5a1f2f539a2bb18f5472158b5c487e", class: "cal-today-icon", viewBox: "0 0 16 16", "aria-hidden": "true", focusable: "false" }, X("circle", { key: "755682486d720eedb20d9a98e98a146188881b55", cx: "8", cy: "8", r: "5.5", fill: "none", stroke: "currentColor", "stroke-width": "1.5" }), X("circle", { key: "e101b257ebcc7a730585e5a907d6ea4ddb935912", cx: "8", cy: "8", r: "2", fill: "currentColor" })), X("span", { key: "df291683173b9ee3480ba1be7557d145806ce76b", class: "cal-today-text", "aria-hidden": "true" }, i.today)), X("button", { key: "633581787fa881fae0fb03543aca97b8a4fc7260", type: "button", class: "cal-button cal-step", part: "prev", "aria-label": g, disabled: !d, onClick: () => this.page(-1) }, X(ev, { key: "83fd9acdfee3994ac6a39a0fcb66538a745800a1", class: "cal-step-icon" })), X("button", { key: "b6eec0a863d7c1ad1233bcb5f5afc81479bb4b3d", type: "button", class: "cal-button cal-step", part: "next", "aria-label": v, disabled: !h, onClick: () => this.page(1) }, X(z_, { key: "2a442fab880263cd371fb562ee149a2837992a5a", class: "cal-step-icon" })))), X("div", { key: "bd4b39a55ffdff8eb94493fd7cca850d15942b97", class: "cal-body", "data-switching": _ ? "" : void 0, onPointerOver: this.onGridPointerOver, onPointerLeave: this.onGridLeave, onFocusin: this.onGridFocusIn, onFocusout: this.onGridLeave }, _ && this.renderView(_, r, c, !1), this.renderView(s, r, c, !0)), X("span", { key: "adabfa987a4620bc993e0fe5bfdac5cb7f78db0a", class: "cal-tip", part: "tooltip", "aria-hidden": "true", "data-shown": this.tip.shown ? "" : void 0 }, this.tip.text)));
  }
  get el() {
    return this;
  }
  static get watchers() {
    return { month: [{ onMonthChange: 0 }], value: [{ onValueChange: 0 }], min: [{ onRangeChange: 0 }], max: [{ onRangeChange: 0 }], disableViews: [{ onDisableViews: 0 }] };
  }
  static get style() {
    return '*,:after,:before{--tw-border-spacing-x:0;--tw-border-spacing-y:0;--tw-translate-x:0;--tw-translate-y:0;--tw-rotate:0;--tw-skew-x:0;--tw-skew-y:0;--tw-scale-x:1;--tw-scale-y:1;--tw-scroll-snap-strictness:proximity;--tw-ring-offset-width:0px;--tw-ring-offset-color:#fff;--tw-ring-color:rgba(59,130,246,.5);--tw-ring-offset-shadow:0 0 #0000;--tw-ring-shadow:0 0 #0000;--tw-shadow:0 0 #0000;--tw-shadow-colored:0 0 #0000;border:0 solid #e5e7eb;box-sizing:border-box}::backdrop{--tw-border-spacing-x:0;--tw-border-spacing-y:0;--tw-translate-x:0;--tw-translate-y:0;--tw-rotate:0;--tw-skew-x:0;--tw-skew-y:0;--tw-scale-x:1;--tw-scale-y:1;--tw-scroll-snap-strictness:proximity;--tw-ring-offset-width:0px;--tw-ring-offset-color:#fff;--tw-ring-color:rgba(59,130,246,.5);--tw-ring-offset-shadow:0 0 #0000;--tw-ring-shadow:0 0 #0000;--tw-shadow:0 0 #0000;--tw-shadow-colored:0 0 #0000;}.collapse{visibility:collapse}.absolute{position:absolute}.relative{position:relative}.block{display:block}.inline{display:inline}.flex{display:flex}.table{display:table}.grid{display:grid}.hidden{display:none}.border-collapse{border-collapse:collapse}.transform{transform:translate(var(--tw-translate-x),var(--tw-translate-y)) rotate(var(--tw-rotate)) skewX(var(--tw-skew-x)) skewY(var(--tw-skew-y)) scaleX(var(--tw-scale-x)) scaleY(var(--tw-scale-y))}.resize{resize:both}.border{border-width:1px}.uppercase{text-transform:uppercase}.underline{text-decoration-line:underline}.line-through{text-decoration-line:line-through}.shadow{--tw-shadow:0 1px 3px 0 rgba(0,0,0,.1),0 1px 2px -1px rgba(0,0,0,.1);--tw-shadow-colored:0 1px 3px 0 var(--tw-shadow-color),0 1px 2px -1px var(--tw-shadow-color);box-shadow:var(--tw-ring-offset-shadow,0 0 #0000),var(--tw-ring-shadow,0 0 #0000),var(--tw-shadow)}.outline{outline-style:solid}.ring{--tw-ring-offset-shadow:var(--tw-ring-inset) 0 0 0 var(--tw-ring-offset-width) var(--tw-ring-offset-color);--tw-ring-shadow:var(--tw-ring-inset) 0 0 0 calc(3px + var(--tw-ring-offset-width)) var(--tw-ring-color);box-shadow:var(--tw-ring-offset-shadow),var(--tw-ring-shadow),var(--tw-shadow,0 0 #0000)}.transition{transition-duration:.15s;transition-property:color,background-color,border-color,text-decoration-color,fill,stroke,opacity,box-shadow,transform,filter,backdrop-filter;transition-timing-function:cubic-bezier(.4,0,.2,1)}.container{width:100%}@media (min-width:640px){.container{max-width:640px}}@media (min-width:768px){.container{max-width:768px}}@media (min-width:1024px){.container{max-width:1024px}}@media (min-width:1280px){.container{max-width:1280px}}@media (min-width:1536px){.container{max-width:1536px}}:host{-webkit-text-size-adjust:100%;-webkit-tap-highlight-color:transparent;font-feature-settings:normal;--_surface:var(--shift-calendar-surface,var(--shift-surface,var(--_ap-panel-surface,#fff)));--_ink:var(--shift-calendar-ink,var(--shift-ink,var(--_ap-ink,#1e293b)));--_muted:var(--shift-calendar-muted,var(--shift-ink-muted,var(--_ap-ink-muted,#64748b)));--_disabled-ink:var(--shift-calendar-disabled-ink,var(--shift-ink-disabled,var(--_ap-ink-disabled,#cbd5e1)));--_outside-ink:var(--shift-calendar-outside-ink,var(--shift-ink-disabled,var(--_ap-ink-disabled,#cbd5e1)));--_line:var(--shift-calendar-line,var(--shift-line,var(--_ap-line,#e2e8f0)));--_panel-line:var(--shift-calendar-panel-line,var(--_ap-panel-line,#e2e8f0));--_cell-line:var(--shift-calendar-day-line,var(--_ap-cell-line,transparent));--_accent:var(--shift-calendar-accent,var(--shift-accent,var(--_ap-accent,#1e293b)));--_on-accent:var(--shift-calendar-on-accent,var(--shift-on-accent,var(--_ap-on-accent,#fff)));--_accent-tint:var(--shift-calendar-accent-tint,var(--shift-accent-tint,var(--_ap-accent-tint,#f1f5f9)));--_focus:var(--shift-calendar-focus-ring,var(--shift-focus,var(--_ap-focus,#1e293b)));--_focus-width:var(--shift-calendar-focus-ring-width,var(--_ap-focus-width,2px));--_focus-offset:var(--shift-calendar-focus-ring-offset,var(--_ap-focus-offset,2px));--_halo:var(--shift-calendar-focus-halo,var(--_ap-focus-halo,transparent));--_halo-width:var(--shift-calendar-focus-halo-width,var(--_ap-focus-halo-width,0px));--_focus-inner:var(--shift-calendar-focus-inner,var(--_ap-focus-inner,#fff));--_day-ink:var(--shift-calendar-day-ink,var(--shift-ink,var(--_ap-ink,#1e293b)));--_hover-bg:var(--shift-calendar-day-hover-bg,var(--_ap-hover-bg,#f1f5f9));--_hover-line:var(--shift-calendar-day-hover-line,var(--shift-line-strong,var(--_ap-hover-line,#94a3b8)));--_hover-ink:var(--shift-calendar-day-hover-ink,var(--_ap-hover-ink,#1e293b));--_today-ring:var(--shift-calendar-today-ring,var(--_ap-today-ring,#64748b));--_today-width:var(--shift-calendar-today-ring-width,var(--_ap-today-ring-width,1px));--_today-dot:var(--shift-calendar-today-dot,var(--_ap-today-dot,transparent));--_today-stroke:var(--shift-calendar-today-mark-stroke,var(--_ap-today-stroke,0px));--_today-decoration:var(--shift-calendar-today-mark-decoration,var(--_ap-today-decoration,none));--_blocked-decoration:var(--shift-calendar-blocked-decoration,var(--_ap-blocked-decoration,line-through));--_blocked-opacity:var(--shift-calendar-blocked-opacity,var(--_ap-blocked-opacity,1));--_nav-ink:var(--shift-calendar-nav-ink,var(--shift-ink,var(--_ap-ink,#1e293b)));--_nav-border:var(--shift-calendar-nav-border,var(--shift-line,var(--_ap-line,#e2e8f0)));--_nav-hover:var(--shift-calendar-nav-hover-bg,var(--_ap-hover-bg,#f1f5f9));--_heading-ink:var(--shift-calendar-heading-ink,var(--shift-ink,var(--_ap-ink,#1e293b)));--_shadow:var(--shift-calendar-shadow,var(--shift-shadow-control,var(--_ap-shadow-control,none)));--_tone-neutral-bg:var(--shift-calendar-tone-neutral-bg,var(--_ap-tone-neutral-bg,#e8ecef));--_tone-neutral-ink:var(--shift-calendar-tone-neutral-ink,var(--_ap-tone-neutral-ink,#46535e));--_tone-info-bg:var(--shift-calendar-tone-info-bg,var(--_ap-tone-info-bg,#e8f1f8));--_tone-info-ink:var(--shift-calendar-tone-info-ink,var(--_ap-tone-info-ink,#275e8f));--_tone-success-bg:var(--shift-calendar-tone-success-bg,var(--_ap-tone-success-bg,#e8f4ed));--_tone-success-ink:var(--shift-calendar-tone-success-ink,var(--_ap-tone-success-ink,#216b44));--_tone-warning-bg:var(--shift-calendar-tone-warning-bg,var(--_ap-tone-warning-bg,#fff7e6));--_tone-warning-ink:var(--shift-calendar-tone-warning-ink,var(--_ap-tone-warning-ink,#8a5a12));--_tone-danger-bg:var(--shift-calendar-tone-danger-bg,var(--_ap-tone-danger-bg,#fff1f0));--_tone-danger-ink:var(--shift-calendar-tone-danger-ink,var(--_ap-tone-danger-ink,#9b2c2c));--_radius:var(--shift-calendar-radius,var(--shift-radius-lg,var(--_ap-radius-lg,12px)));--_day-radius:var(--shift-calendar-day-radius,var(--shift-radius-sm,var(--_ap-radius-sm,8px)));--_title-radius:var(--shift-calendar-title-radius,var(--shift-radius-md,var(--_ap-radius-md,8px)));--_button-radius:var(--shift-calendar-button-radius,var(--shift-radius-md,var(--_ap-radius-md,8px)));--_badge-radius:var(--shift-calendar-badge-radius,999px);--_border-width:var(--shift-calendar-border-width,var(--shift-border-width,var(--_ap-border-width,1px)));--_panel-border-width:var(--shift-calendar-panel-border-width,var(--_ap-panel-border-width,1px));--_font-latin:var(--shift-calendar-font-family,var(--shift-font-family,var(--_ap-font,"Nunito",system-ui,sans-serif)));--_font-arabic:var(--shift-calendar-font-family-arabic,var(--shift-font-family-arabic,"Noto Kufi Arabic",var(--_font-latin)));--_font-size:var(--shift-calendar-font-size,var(--shift-font-size,calc(var(--_ap-font-size, 14px) + var(--_sz-font, 0px))));--_heading-size:var(--shift-calendar-heading-font-size,calc(var(--shift-font-size, calc(var(--_ap-font-size, 14px) + var(--_sz-font, 0px))) + 1px));--_heading-weight:var(--shift-calendar-heading-font-weight,700);--_weekday-size:var(--shift-calendar-weekday-font-size,calc(var(--shift-font-size-sm, calc(var(--_ap-font-size-sm, 12px) + var(--_sz-font, 0px))) - 1px));--_weekday-weight:var(--shift-calendar-weekday-font-weight,700);--_weekday-transform:var(--shift-calendar-weekday-transform,none);--_weekday-tracking:var(--shift-calendar-weekday-tracking,normal);--_day-font-size:var(--shift-calendar-day-font-size,var(--shift-font-size,calc(var(--_ap-font-size, 14px) + var(--_sz-font, 0px))));--_day-weight:var(--shift-calendar-day-font-weight,var(--shift-weight-strong,var(--_ap-weight-strong,600)));--_disabled-weight:var(--shift-calendar-disabled-font-weight,var(--shift-weight,var(--_ap-weight,400)));--_today-size:var(--shift-calendar-today-font-size,var(--shift-font-size-sm,calc(var(--_ap-font-size-sm, 12px) + var(--_sz-font, 0px))));--_today-weight-button:var(--shift-calendar-today-font-weight,var(--shift-weight-strong,var(--_ap-weight-strong,600)));--_badge-size:var(--shift-calendar-badge-font-size,calc(10px + var(--_sz-font, 0px)/2));--_badge-weight:var(--shift-calendar-badge-font-weight,700);--_badge-line-height:var(--shift-calendar-badge-line-height,12px);--_line-height:var(--shift-calendar-line-height,1.35);--_heading-line-height:var(--shift-calendar-heading-line-height,1.2);--_weekday-line-height:var(--shift-calendar-weekday-line-height,calc(16px + var(--_sz-font, 0px)));--_cell:var(--shift-calendar-day-size,var(--shift-cell-size,calc(var(--_ap-cell-size, 40px)*var(--_sz-control, 1))));--_round:var(--shift-calendar-day-round,var(--_ap-cell-round,0));--_day-max-width:var(--shift-calendar-day-max-inline-size,none);--_gap:var(--shift-calendar-gap,var(--shift-gap,calc(var(--_ap-gap, 4px)*var(--_sz-space, 1))));--_nav-size:var(--shift-calendar-nav-size,var(--_sz-nav,32px));--_nav-icon:var(--shift-calendar-nav-icon-size,var(--_sz-icon,16px));--_today-icon:var(--shift-calendar-today-icon-size,calc(var(--_sz-icon, 16px) - 2px));--_min-width:var(--shift-calendar-min-inline-size,var(--_sz-min-width,300px));--_max-width:var(--shift-calendar-max-inline-size,var(--_sz-width,380px));--_padding:var(--shift-calendar-padding,calc(12px*var(--_sz-space, 1)));--_padding-top:var(--shift-calendar-padding-top,calc(12px*var(--_sz-space, 1)));--_section-gap:var(--shift-calendar-section-gap,calc(8px*var(--_sz-space, 1)));--_header-gap:var(--shift-calendar-header-gap,max(calc(8px*var(--_sz-space, 1)),calc(var(--_ring) + 2px)));--_nav-gap:var(--shift-calendar-nav-gap,max(calc(4px*var(--_sz-space, 1)),calc(var(--_ring) + 2px)));--_button-gap:var(--shift-calendar-button-gap,calc(6px*var(--_sz-space, 1)));--_title-gap:var(--shift-calendar-title-gap,2px);--_title-padding:var(--shift-calendar-title-padding-inline,2px);--_today-padding:var(--shift-calendar-today-padding-inline,10px);--_weekday-padding:var(--shift-calendar-weekday-padding-block,2px);--_badge-padding:var(--shift-calendar-badge-padding-inline,4px);--_badge-inset:var(--shift-calendar-badge-inset,3px);--_badge-shift:var(--shift-calendar-badge-shift,5px);--_dot-size:var(--shift-calendar-today-dot-size,4px);--_tip-surface:var(--shift-calendar-tooltip-surface,var(--_ap-tooltip-surface,#1e293b));--_tip-ink:var(--shift-calendar-tooltip-ink,var(--_ap-tooltip-ink,#f8fafc));--_tip-size:var(--shift-calendar-tooltip-font-size,var(--shift-font-size-sm,calc(var(--_ap-font-size-sm, 12px) + var(--_sz-font, 0px))));--_tip-radius:var(--shift-calendar-tooltip-radius,6px);--_settle:var(--shift-calendar-settle,var(--shift-settle,480ms));--_ease:var(--shift-calendar-ease,var(--shift-ease,ease));--_ring:max(calc(var(--_focus-width) + var(--_focus-offset)),var(--_halo-width),2px);--_weekday-row:calc(var(--_weekday-line-height) + var(--_weekday-padding)*2);color:var(--_ink);display:block;font-family:ui-sans-serif,system-ui,sans-serif,Apple Color Emoji,Segoe UI Emoji,Segoe UI Symbol,Noto Color Emoji;font-family:var(--_font-latin);font-size:var(--_font-size);font-variation-settings:normal;line-height:1.5;line-height:var(--_line-height);-moz-tab-size:4;-o-tab-size:4;tab-size:4}hr{border-top-width:1px;color:inherit;height:0}abbr:where([title]){-webkit-text-decoration:underline dotted;text-decoration:underline dotted}h1,h2,h3,h4,h5,h6{font-size:inherit;font-weight:inherit}a{color:inherit;text-decoration:inherit}b,strong{font-weight:bolder}code,kbd,pre,samp{font-feature-settings:normal;font-family:ui-monospace,SFMono-Regular,Menlo,Monaco,Consolas,Liberation Mono,Courier New,monospace;font-size:1em;font-variation-settings:normal}small{font-size:80%}sub,sup{font-size:75%;line-height:0;position:relative;vertical-align:baseline}sub{bottom:-.25em}sup{top:-.5em}table{border-collapse:collapse;border-color:inherit;text-indent:0}button,input,optgroup,select,textarea{font-feature-settings:inherit;color:inherit;font-family:inherit;font-size:100%;font-variation-settings:inherit;font-weight:inherit;letter-spacing:inherit;line-height:inherit;margin:0;padding:0}button,select{text-transform:none}button,input:where([type=button]),input:where([type=reset]),input:where([type=submit]){-webkit-appearance:button;background-color:transparent;background-image:none}:-moz-focusring{outline:auto}:-moz-ui-invalid{box-shadow:none}progress{vertical-align:baseline}::-webkit-inner-spin-button,::-webkit-outer-spin-button{height:auto}[type=search]{-webkit-appearance:textfield;outline-offset:-2px}::-webkit-search-decoration{-webkit-appearance:none}::-webkit-file-upload-button{-webkit-appearance:button;font:inherit}summary{display:list-item}blockquote,dd,dl,fieldset,figure,h1,h2,h3,h4,h5,h6,hr,p,pre{margin:0}fieldset,legend{padding:0}menu,ol,ul{list-style:none;margin:0;padding:0}dialog{padding:0}textarea{resize:vertical}input::-moz-placeholder,textarea::-moz-placeholder{color:#9ca3af;opacity:1}input::placeholder,textarea::placeholder{color:#9ca3af;opacity:1}[role=button],button{cursor:pointer}:disabled{cursor:default}audio,canvas,embed,iframe,img,object,svg,video{display:block;vertical-align:middle}img,video{height:auto;max-width:100%}[hidden]:where(:not([hidden=until-found])){display:none}:host([appearance=vanilla]){--_ap-font:"Nunito",system-ui,sans-serif;--_ap-font-size:14px;--_ap-font-size-sm:12px;--_ap-label-size:12px;--_ap-label-transform:uppercase;--_ap-label-tracking:0.04em;--_ap-label-weight:600;--_ap-weight:400;--_ap-weight-strong:600;--_ap-radius-sm:8px;--_ap-radius-md:8px;--_ap-radius-lg:12px;--_ap-border-width:1px;--_ap-panel-border-width:1px;--_ap-control-height:44px;--_ap-cell-size:40px;--_ap-cell-round:0;--_ap-gap:4px;--_ap-focus-width:2px;--_ap-focus-offset:2px;--_ap-focus-halo-width:0px;--_ap-today-ring-width:1px;--_ap-today-stroke:0px;--_ap-today-decoration:none;--_ap-blocked-decoration:line-through;--_ap-blocked-opacity:1;--_ap-surface:#fff;--_ap-surface-muted:#f8fafc;--_ap-panel-surface:#fff;--_ap-ink:#1e293b;--_ap-ink-muted:#64748b;--_ap-ink-disabled:#cbd5e1;--_ap-line:#e2e8f0;--_ap-line-strong:#94a3b8;--_ap-panel-line:#e2e8f0;--_ap-cell-line:transparent;--_ap-accent:#1e293b;--_ap-on-accent:#fff;--_ap-accent-tint:#f1f5f9;--_ap-focus:#1e293b;--_ap-focus-halo:transparent;--_ap-focus-inner:#fff;--_ap-danger:#dc2626;--_ap-shadow-panel:0 10px 30px -12px rgba(15,23,42,.25);--_ap-shadow-control:none;--_ap-hover-bg:#f1f5f9;--_ap-hover-line:#94a3b8;--_ap-hover-ink:#1e293b;--_ap-today-ring:#64748b;--_ap-today-dot:transparent}:host([appearance=material]){--_ap-font:"Nunito",system-ui,sans-serif;--_ap-font-size:14px;--_ap-font-size-sm:12px;--_ap-label-size:12px;--_ap-label-transform:none;--_ap-label-tracking:0em;--_ap-label-weight:500;--_ap-weight:400;--_ap-weight-strong:600;--_ap-radius-sm:999px;--_ap-radius-md:4px;--_ap-radius-lg:4px;--_ap-border-width:0px;--_ap-panel-border-width:1px;--_ap-control-height:48px;--_ap-cell-size:40px;--_ap-cell-round:1;--_ap-gap:2px;--_ap-focus-width:2px;--_ap-focus-offset:2px;--_ap-focus-halo-width:0px;--_ap-today-ring-width:1px;--_ap-today-stroke:0px;--_ap-today-decoration:none;--_ap-blocked-decoration:none;--_ap-blocked-opacity:1;--_ap-surface:#fff;--_ap-surface-muted:#f5f5f5;--_ap-panel-surface:#fff;--_ap-ink:rgba(0,0,0,.87);--_ap-ink-muted:rgba(0,0,0,.54);--_ap-ink-disabled:rgba(0,0,0,.38);--_ap-line:rgba(0,0,0,.12);--_ap-line-strong:rgba(0,0,0,.42);--_ap-panel-line:rgba(0,0,0,.12);--_ap-cell-line:transparent;--_ap-accent:#1976d2;--_ap-on-accent:#fff;--_ap-accent-tint:rgba(25,118,210,.08);--_ap-focus:#1976d2;--_ap-focus-halo:transparent;--_ap-focus-inner:#fff;--_ap-danger:#d32f2f;--_ap-shadow-panel:0 5px 5px -3px rgba(0,0,0,.2),0 8px 10px 1px rgba(0,0,0,.14),0 3px 14px 2px rgba(0,0,0,.12);--_ap-shadow-control:0 2px 1px -1px rgba(0,0,0,.2),0 1px 1px 0 rgba(0,0,0,.14),0 1px 3px 0 rgba(0,0,0,.12);--_ap-hover-bg:rgba(0,0,0,.04);--_ap-hover-line:rgba(0,0,0,.12);--_ap-hover-ink:rgba(0,0,0,.87);--_ap-today-ring:#1976d2;--_ap-today-dot:transparent}:host([appearance=soft]){--_ap-font:"Nunito",system-ui,sans-serif;--_ap-font-size:15px;--_ap-font-size-sm:13px;--_ap-label-size:13px;--_ap-label-transform:none;--_ap-label-tracking:0em;--_ap-label-weight:600;--_ap-weight:400;--_ap-weight-strong:600;--_ap-radius-sm:12px;--_ap-radius-md:14px;--_ap-radius-lg:20px;--_ap-border-width:0px;--_ap-panel-border-width:0px;--_ap-control-height:48px;--_ap-cell-size:44px;--_ap-cell-round:0;--_ap-gap:6px;--_ap-focus-width:1px;--_ap-focus-offset:0px;--_ap-focus-halo-width:4px;--_ap-today-ring-width:0px;--_ap-today-stroke:0px;--_ap-today-decoration:none;--_ap-blocked-decoration:none;--_ap-blocked-opacity:0.5;--_ap-surface:#f1f5f9;--_ap-surface-muted:#f8fafc;--_ap-panel-surface:#f1f5f9;--_ap-ink:#1e293b;--_ap-ink-muted:#475569;--_ap-ink-disabled:#cbd5e1;--_ap-line:transparent;--_ap-line-strong:#cbd5e1;--_ap-panel-line:transparent;--_ap-cell-line:transparent;--_ap-accent:#1e293b;--_ap-on-accent:#fff;--_ap-accent-tint:#e2e8f0;--_ap-focus:#1e293b;--_ap-focus-halo:#e2e8f0;--_ap-focus-inner:#e2e8f0;--_ap-danger:#dc2626;--_ap-shadow-panel:0 16px 40px -16px rgba(15,23,42,.2);--_ap-shadow-control:0 1px 2px 0 rgba(15,23,42,.06),0 8px 24px -12px rgba(15,23,42,.18);--_ap-hover-bg:#e2e8f0;--_ap-hover-line:transparent;--_ap-hover-ink:#1e293b;--_ap-today-ring:transparent;--_ap-today-dot:#1e293b}:host([appearance=sharp]){--_ap-font:"Manrope",system-ui,sans-serif;--_ap-font-size:13px;--_ap-font-size-sm:12px;--_ap-label-size:11px;--_ap-label-transform:uppercase;--_ap-label-tracking:0.06em;--_ap-label-weight:700;--_ap-weight:400;--_ap-weight-strong:600;--_ap-radius-sm:2px;--_ap-radius-md:2px;--_ap-radius-lg:4px;--_ap-border-width:1px;--_ap-panel-border-width:1px;--_ap-control-height:36px;--_ap-cell-size:34px;--_ap-cell-round:0;--_ap-gap:2px;--_ap-focus-width:2px;--_ap-focus-offset:0px;--_ap-focus-halo-width:0px;--_ap-today-ring-width:0px;--_ap-today-stroke:0.6px;--_ap-today-decoration:underline;--_ap-blocked-decoration:line-through;--_ap-blocked-opacity:1;--_ap-surface:#fff;--_ap-surface-muted:#f1f5f9;--_ap-panel-surface:#fff;--_ap-ink:#0f172a;--_ap-ink-muted:#475569;--_ap-ink-disabled:#94a3b8;--_ap-line:#94a3b8;--_ap-line-strong:#0f172a;--_ap-panel-line:#0f172a;--_ap-cell-line:#94a3b8;--_ap-accent:#0f172a;--_ap-on-accent:#fff;--_ap-accent-tint:#f1f5f9;--_ap-focus:#0f172a;--_ap-focus-halo:transparent;--_ap-focus-inner:#fff;--_ap-danger:#b91c1c;--_ap-shadow-panel:none;--_ap-shadow-control:none;--_ap-hover-bg:#f1f5f9;--_ap-hover-line:#94a3b8;--_ap-hover-ink:#0f172a;--_ap-today-ring:transparent;--_ap-today-dot:transparent}:host([appearance=minimal]){--_ap-font:"Manrope",system-ui,sans-serif;--_ap-font-size:14px;--_ap-font-size-sm:12px;--_ap-label-size:12px;--_ap-label-transform:none;--_ap-label-tracking:0em;--_ap-label-weight:400;--_ap-weight:400;--_ap-weight-strong:600;--_ap-radius-sm:999px;--_ap-radius-md:6px;--_ap-radius-lg:8px;--_ap-border-width:0px;--_ap-panel-border-width:1px;--_ap-control-height:44px;--_ap-cell-size:40px;--_ap-cell-round:1;--_ap-gap:4px;--_ap-focus-width:2px;--_ap-focus-offset:2px;--_ap-focus-halo-width:0px;--_ap-today-ring-width:0px;--_ap-today-stroke:0px;--_ap-today-decoration:none;--_ap-blocked-decoration:none;--_ap-blocked-opacity:1;--_ap-surface:transparent;--_ap-surface-muted:transparent;--_ap-panel-surface:transparent;--_ap-ink:#0f172a;--_ap-ink-muted:#64748b;--_ap-ink-disabled:#cbd5e1;--_ap-line:transparent;--_ap-line-strong:#cbd5e1;--_ap-panel-line:transparent;--_ap-cell-line:transparent;--_ap-accent:#0f172a;--_ap-on-accent:#fff;--_ap-accent-tint:#f1f5f9;--_ap-focus:#0f172a;--_ap-focus-halo:transparent;--_ap-focus-inner:#fff;--_ap-danger:#dc2626;--_ap-shadow-panel:none;--_ap-shadow-control:none;--_ap-hover-bg:transparent;--_ap-hover-line:transparent;--_ap-hover-ink:#0f172a;--_ap-today-ring:transparent;--_ap-today-dot:#0f172a}:host([color-scheme=light]){--_ap-tone-neutral-bg:#e8ecef;--_ap-tone-neutral-ink:#46535e;--_ap-tone-info-bg:#e8f1f8;--_ap-tone-info-ink:#275e8f;--_ap-tone-success-bg:#e8f4ed;--_ap-tone-success-ink:#216b44;--_ap-tone-warning-bg:#fff7e6;--_ap-tone-warning-ink:#8a5a12;--_ap-tone-danger-bg:#fff1f0;--_ap-tone-danger-ink:#9b2c2c;color-scheme:light}:host(:not([appearance])[color-scheme=dark]),:host([appearance=vanilla][color-scheme=dark]){--_ap-surface:#0f172a;--_ap-surface-muted:#1e293b;--_ap-panel-surface:#0f172a;--_ap-ink:#f1f5f9;--_ap-ink-muted:#94a3b8;--_ap-ink-disabled:#475569;--_ap-line:#334155;--_ap-line-strong:#64748b;--_ap-panel-line:#334155;--_ap-cell-line:transparent;--_ap-accent:#f1f5f9;--_ap-on-accent:#0f172a;--_ap-accent-tint:#1e293b;--_ap-focus:#f1f5f9;--_ap-focus-halo:transparent;--_ap-focus-inner:#0f172a;--_ap-danger:#f87171;--_ap-shadow-panel:0 10px 30px -12px rgba(0,0,0,.6);--_ap-shadow-control:none;--_ap-hover-bg:#1e293b;--_ap-hover-line:#64748b;--_ap-hover-ink:#f1f5f9;--_ap-today-ring:#64748b;--_ap-today-dot:transparent}:host([appearance=material][color-scheme=dark]){--_ap-surface:#1e1e1e;--_ap-surface-muted:#2c2c2c;--_ap-panel-surface:#2c2c2c;--_ap-ink:hsla(0,0%,100%,.87);--_ap-ink-muted:hsla(0,0%,100%,.6);--_ap-ink-disabled:hsla(0,0%,100%,.38);--_ap-line:hsla(0,0%,100%,.12);--_ap-line-strong:hsla(0,0%,100%,.42);--_ap-panel-line:hsla(0,0%,100%,.12);--_ap-cell-line:transparent;--_ap-accent:#90caf9;--_ap-on-accent:#0d1b2a;--_ap-accent-tint:rgba(144,202,249,.12);--_ap-focus:#90caf9;--_ap-focus-halo:transparent;--_ap-focus-inner:#0d1b2a;--_ap-danger:#f48fb1;--_ap-shadow-panel:0 5px 5px -3px rgba(0,0,0,.2),0 8px 10px 1px rgba(0,0,0,.14),0 3px 14px 2px rgba(0,0,0,.12);--_ap-shadow-control:0 2px 1px -1px rgba(0,0,0,.4),0 1px 1px 0 rgba(0,0,0,.28),0 1px 3px 0 rgba(0,0,0,.24);--_ap-hover-bg:hsla(0,0%,100%,.08);--_ap-hover-line:hsla(0,0%,100%,.12);--_ap-hover-ink:hsla(0,0%,100%,.87);--_ap-today-ring:#90caf9;--_ap-today-dot:transparent}:host([appearance=soft][color-scheme=dark]){--_ap-surface:#1e293b;--_ap-surface-muted:#0f172a;--_ap-panel-surface:#1e293b;--_ap-ink:#f1f5f9;--_ap-ink-muted:#94a3b8;--_ap-ink-disabled:#475569;--_ap-line:transparent;--_ap-line-strong:#475569;--_ap-panel-line:transparent;--_ap-cell-line:transparent;--_ap-accent:#f1f5f9;--_ap-on-accent:#0f172a;--_ap-accent-tint:#334155;--_ap-focus:#f1f5f9;--_ap-focus-halo:#334155;--_ap-focus-inner:#334155;--_ap-danger:#f87171;--_ap-shadow-panel:0 16px 40px -16px rgba(0,0,0,.55);--_ap-shadow-control:0 1px 2px 0 rgba(0,0,0,.4),0 8px 24px -12px rgba(0,0,0,.5);--_ap-hover-bg:#334155;--_ap-hover-line:transparent;--_ap-hover-ink:#f1f5f9;--_ap-today-ring:transparent;--_ap-today-dot:#f1f5f9}:host([appearance=sharp][color-scheme=dark]){--_ap-surface:#0b0f17;--_ap-surface-muted:#0f172a;--_ap-panel-surface:#0b0f17;--_ap-ink:#f8fafc;--_ap-ink-muted:#94a3b8;--_ap-ink-disabled:#475569;--_ap-line:#475569;--_ap-line-strong:#e2e8f0;--_ap-panel-line:#e2e8f0;--_ap-cell-line:#475569;--_ap-accent:#f8fafc;--_ap-on-accent:#020617;--_ap-accent-tint:#1e293b;--_ap-focus:#f8fafc;--_ap-focus-halo:transparent;--_ap-focus-inner:#020617;--_ap-danger:#f87171;--_ap-shadow-panel:none;--_ap-shadow-control:none;--_ap-hover-bg:#1e293b;--_ap-hover-line:#475569;--_ap-hover-ink:#f8fafc;--_ap-today-ring:transparent;--_ap-today-dot:transparent}:host([appearance=minimal][color-scheme=dark]){--_ap-surface:transparent;--_ap-surface-muted:transparent;--_ap-panel-surface:transparent;--_ap-ink:#f1f5f9;--_ap-ink-muted:#94a3b8;--_ap-ink-disabled:#475569;--_ap-line:transparent;--_ap-line-strong:#475569;--_ap-panel-line:#334155;--_ap-cell-line:transparent;--_ap-accent:#f1f5f9;--_ap-on-accent:#0f172a;--_ap-accent-tint:#1e293b;--_ap-focus:#f1f5f9;--_ap-focus-halo:transparent;--_ap-focus-inner:#0f172a;--_ap-danger:#f87171;--_ap-shadow-panel:none;--_ap-shadow-control:none;--_ap-hover-bg:transparent;--_ap-hover-line:transparent;--_ap-hover-ink:#f1f5f9;--_ap-today-ring:transparent;--_ap-today-dot:#f1f5f9}:host([color-scheme=dark]){--_ap-tone-neutral-bg:#334155;--_ap-tone-neutral-ink:#e2e8f0;--_ap-tone-info-bg:#1e3a5f;--_ap-tone-info-ink:#bfdbfe;--_ap-tone-success-bg:#14532d;--_ap-tone-success-ink:#bbf7d0;--_ap-tone-warning-bg:#4a3212;--_ap-tone-warning-ink:#fde68a;--_ap-tone-danger-bg:#4c1d1d;--_ap-tone-danger-ink:#fecaca;color-scheme:dark}@media (prefers-color-scheme:dark){:host(:not([appearance])[color-scheme=auto]),:host([appearance=vanilla][color-scheme=auto]){--_ap-surface:#0f172a;--_ap-surface-muted:#1e293b;--_ap-panel-surface:#0f172a;--_ap-ink:#f1f5f9;--_ap-ink-muted:#94a3b8;--_ap-ink-disabled:#475569;--_ap-line:#334155;--_ap-line-strong:#64748b;--_ap-panel-line:#334155;--_ap-cell-line:transparent;--_ap-accent:#f1f5f9;--_ap-on-accent:#0f172a;--_ap-accent-tint:#1e293b;--_ap-focus:#f1f5f9;--_ap-focus-halo:transparent;--_ap-focus-inner:#0f172a;--_ap-danger:#f87171;--_ap-shadow-panel:0 10px 30px -12px rgba(0,0,0,.6);--_ap-shadow-control:none;--_ap-hover-bg:#1e293b;--_ap-hover-line:#64748b;--_ap-hover-ink:#f1f5f9;--_ap-today-ring:#64748b;--_ap-today-dot:transparent}:host([appearance=material][color-scheme=auto]){--_ap-surface:#1e1e1e;--_ap-surface-muted:#2c2c2c;--_ap-panel-surface:#2c2c2c;--_ap-ink:hsla(0,0%,100%,.87);--_ap-ink-muted:hsla(0,0%,100%,.6);--_ap-ink-disabled:hsla(0,0%,100%,.38);--_ap-line:hsla(0,0%,100%,.12);--_ap-line-strong:hsla(0,0%,100%,.42);--_ap-panel-line:hsla(0,0%,100%,.12);--_ap-cell-line:transparent;--_ap-accent:#90caf9;--_ap-on-accent:#0d1b2a;--_ap-accent-tint:rgba(144,202,249,.12);--_ap-focus:#90caf9;--_ap-focus-halo:transparent;--_ap-focus-inner:#0d1b2a;--_ap-danger:#f48fb1;--_ap-shadow-panel:0 5px 5px -3px rgba(0,0,0,.2),0 8px 10px 1px rgba(0,0,0,.14),0 3px 14px 2px rgba(0,0,0,.12);--_ap-shadow-control:0 2px 1px -1px rgba(0,0,0,.4),0 1px 1px 0 rgba(0,0,0,.28),0 1px 3px 0 rgba(0,0,0,.24);--_ap-hover-bg:hsla(0,0%,100%,.08);--_ap-hover-line:hsla(0,0%,100%,.12);--_ap-hover-ink:hsla(0,0%,100%,.87);--_ap-today-ring:#90caf9;--_ap-today-dot:transparent}:host([appearance=soft][color-scheme=auto]){--_ap-surface:#1e293b;--_ap-surface-muted:#0f172a;--_ap-panel-surface:#1e293b;--_ap-ink:#f1f5f9;--_ap-ink-muted:#94a3b8;--_ap-ink-disabled:#475569;--_ap-line:transparent;--_ap-line-strong:#475569;--_ap-panel-line:transparent;--_ap-cell-line:transparent;--_ap-accent:#f1f5f9;--_ap-on-accent:#0f172a;--_ap-accent-tint:#334155;--_ap-focus:#f1f5f9;--_ap-focus-halo:#334155;--_ap-focus-inner:#334155;--_ap-danger:#f87171;--_ap-shadow-panel:0 16px 40px -16px rgba(0,0,0,.55);--_ap-shadow-control:0 1px 2px 0 rgba(0,0,0,.4),0 8px 24px -12px rgba(0,0,0,.5);--_ap-hover-bg:#334155;--_ap-hover-line:transparent;--_ap-hover-ink:#f1f5f9;--_ap-today-ring:transparent;--_ap-today-dot:#f1f5f9}:host([appearance=sharp][color-scheme=auto]){--_ap-surface:#0b0f17;--_ap-surface-muted:#0f172a;--_ap-panel-surface:#0b0f17;--_ap-ink:#f8fafc;--_ap-ink-muted:#94a3b8;--_ap-ink-disabled:#475569;--_ap-line:#475569;--_ap-line-strong:#e2e8f0;--_ap-panel-line:#e2e8f0;--_ap-cell-line:#475569;--_ap-accent:#f8fafc;--_ap-on-accent:#020617;--_ap-accent-tint:#1e293b;--_ap-focus:#f8fafc;--_ap-focus-halo:transparent;--_ap-focus-inner:#020617;--_ap-danger:#f87171;--_ap-shadow-panel:none;--_ap-shadow-control:none;--_ap-hover-bg:#1e293b;--_ap-hover-line:#475569;--_ap-hover-ink:#f8fafc;--_ap-today-ring:transparent;--_ap-today-dot:transparent}:host([appearance=minimal][color-scheme=auto]){--_ap-surface:transparent;--_ap-surface-muted:transparent;--_ap-panel-surface:transparent;--_ap-ink:#f1f5f9;--_ap-ink-muted:#94a3b8;--_ap-ink-disabled:#475569;--_ap-line:transparent;--_ap-line-strong:#475569;--_ap-panel-line:#334155;--_ap-cell-line:transparent;--_ap-accent:#f1f5f9;--_ap-on-accent:#0f172a;--_ap-accent-tint:#1e293b;--_ap-focus:#f1f5f9;--_ap-focus-halo:transparent;--_ap-focus-inner:#0f172a;--_ap-danger:#f87171;--_ap-shadow-panel:none;--_ap-shadow-control:none;--_ap-hover-bg:transparent;--_ap-hover-line:transparent;--_ap-hover-ink:#f1f5f9;--_ap-today-ring:transparent;--_ap-today-dot:#f1f5f9}:host([color-scheme=auto]){--_ap-tone-neutral-bg:#334155;--_ap-tone-neutral-ink:#e2e8f0;--_ap-tone-info-bg:#1e3a5f;--_ap-tone-info-ink:#bfdbfe;--_ap-tone-success-bg:#14532d;--_ap-tone-success-ink:#bbf7d0;--_ap-tone-warning-bg:#4a3212;--_ap-tone-warning-ink:#fde68a;--_ap-tone-danger-bg:#4c1d1d;--_ap-tone-danger-ink:#fecaca;color-scheme:dark}}:host([size=xs]){--_sz-control:0.75;--_sz-font:-2px;--_sz-space:0.5;--_sz-nav:24px;--_sz-icon:12px;--_sz-width:300px;--_sz-min-width:260px;--_sz-min-floor:0px}:host([size=sm]){--_sz-control:0.85;--_sz-font:-1px;--_sz-space:0.75;--_sz-nav:28px;--_sz-icon:14px;--_sz-width:340px;--_sz-min-width:280px;--_sz-min-floor:0px}:host([size=md]){--_sz-control:1;--_sz-font:0px;--_sz-space:1;--_sz-nav:32px;--_sz-icon:16px;--_sz-width:380px;--_sz-min-width:300px;--_sz-min-floor:0px}:host([size=lg]){--_sz-control:1.15;--_sz-font:2px;--_sz-space:1.25;--_sz-nav:36px;--_sz-icon:18px;--_sz-width:440px;--_sz-min-width:340px;--_sz-min-floor:340px}:host([size=xl]){--_sz-control:1.3;--_sz-font:4px;--_sz-space:1.5;--_sz-nav:40px;--_sz-icon:20px;--_sz-width:500px;--_sz-min-width:380px;--_sz-min-floor:380px}@media (pointer:coarse){:host([appearance=sharp]){--_ap-cell-size:44px}}.cal-root{--_dir:1;background:var(--_surface);border:var(--_panel-border-width) solid var(--_panel-line);border-radius:var(--_radius);box-shadow:var(--_shadow);color:var(--_ink);container:shift-calendar/inline-size;display:flex;flex-direction:column;font-family:var(--_font-latin);gap:var(--_section-gap);inline-size:100%;letter-spacing:normal;max-inline-size:var(--_max-width);min-inline-size:min(var(--_min-width),max(100%,var(--_sz-min-floor,0px)));padding:var(--_padding-top) var(--_padding) var(--_padding);position:relative;text-align:start;text-transform:none;white-space:normal;word-spacing:normal}.cal-root:lang(ar),.cal-root:lang(ckb){font-family:var(--_font-arabic)}.cal-root[dir=rtl]{--_dir:-1}.cal-header{align-items:center;block-size:var(--_nav-size);display:flex;gap:var(--_header-gap)}.cal-title-band{display:grid;flex:1 1 auto;margin-inline-start:calc(var(--_title-padding)*-1);overflow:hidden;overflow:clip}.cal-title,.cal-title-band{block-size:100%;min-inline-size:0}.cal-title{align-items:center;display:flex;gap:var(--_title-gap);grid-area:1/1}.cal-slot{block-size:100%;display:grid;min-inline-size:0;overflow:hidden;overflow:clip}.cal-slot-layer{align-items:center;block-size:100%;display:flex;grid-area:1/1}.cal-slot-layer[data-phase=enter]{animation:cal-title-enter var(--_settle) var(--_ease) both}.cal-slot-layer[data-phase=leave]{animation:cal-title-leave var(--_settle) var(--_ease) both}.cal-title-button,.cal-title-text{align-items:center;block-size:100%;border-radius:var(--_title-radius);color:var(--_heading-ink);display:inline-flex;font-size:min(var(--_heading-size),5.4cqi);font-variant-numeric:tabular-nums;font-weight:var(--_heading-weight);line-height:var(--_heading-line-height);padding-inline:var(--_title-padding);transition:background-color calc(var(--_settle)/2) var(--_ease);-webkit-user-select:none;-moz-user-select:none;user-select:none;white-space:nowrap}.cal-title-button:enabled:hover{background-color:var(--_hover-bg)}.cal-title-button:disabled{cursor:default}.cal-title-button:focus-visible{outline:var(--_focus-width) solid var(--_focus);outline-offset:calc(var(--_focus-width)*-1)}.cal-nav{align-items:center;display:flex;flex:0 0 auto;gap:var(--_nav-gap)}.cal-button{align-items:center;block-size:var(--_nav-size);border:var(--_border-width) solid var(--_nav-border);border-radius:var(--_button-radius);color:var(--_nav-ink);display:inline-flex;gap:var(--_button-gap);justify-content:center;min-inline-size:var(--_nav-size);transition:background-color calc(var(--_settle)/2) var(--_ease),border-color calc(var(--_settle)/2) var(--_ease),opacity calc(var(--_settle)/2) var(--_ease)}.cal-button:enabled:hover{background-color:var(--_nav-hover)}.cal-button:disabled{cursor:not-allowed;opacity:.4}.cal-button:focus-visible{box-shadow:0 0 0 var(--_halo-width) var(--_halo);outline:var(--_focus-width) solid var(--_focus);outline-offset:var(--_focus-offset)}.cal-day:focus-visible,.cal-pick:focus-visible{--_focus-shadow:inset 0 0 0 var(--_focus-width) var(--_focus),inset 0 0 0 max(calc(var(--_focus-width)*2),var(--_halo-width)) var(--_focus-inner);outline:none}.cal-step-icon{block-size:var(--_nav-icon);inline-size:var(--_nav-icon)}.cal-root[dir=rtl] .cal-step-icon{transform:scaleX(-1)}.cal-today{padding-inline:0}.cal-today[data-hidden]{opacity:0;pointer-events:none;transition:opacity var(--_settle) var(--_ease),visibility 0s linear var(--_settle);visibility:hidden}.cal-today-icon{block-size:var(--_today-icon);inline-size:var(--_today-icon)}.cal-today-text{display:none;font-size:var(--_today-size);font-weight:var(--_today-weight-button);white-space:nowrap}.cal-body{block-size:calc(var(--_weekday-row) + var(--_section-gap) + var(--_cell)*6 + var(--_gap)*5);display:grid}.cal-view{grid-area:1/1;min-block-size:0}.cal-view[data-phase=leave]{animation:cal-view-leave var(--_settle) var(--_ease) both;pointer-events:none}.cal-grid{block-size:100%;display:flex;flex-direction:column;gap:var(--_section-gap)}.cal-week,.cal-weekdays{display:grid;gap:var(--_gap);grid-template-columns:repeat(7,minmax(0,1fr))}.cal-weekday{color:var(--_muted);font-size:var(--_weekday-size);font-weight:var(--_weekday-weight);letter-spacing:var(--_weekday-tracking);line-height:var(--_weekday-line-height);overflow:hidden;padding-block:var(--_weekday-padding);text-align:center;text-overflow:ellipsis;text-transform:var(--_weekday-transform);-webkit-user-select:none;-moz-user-select:none;user-select:none;white-space:nowrap}.cal-weekday-short{display:none}.cal-stage{display:grid;flex:1 1 auto;margin:calc(var(--_ring)*-1);min-block-size:0;overflow:hidden;overflow:clip;padding:var(--_ring)}.cal-page{display:flex;flex-direction:column;gap:var(--_gap);grid-area:1/1;min-block-size:0}.cal-page[data-phase=leave]{animation:cal-page-leave var(--_settle) var(--_ease) both;pointer-events:none}.cal-picker-row{display:grid;flex:1 1 0;gap:var(--_gap);grid-template-columns:repeat(3,minmax(0,1fr));min-block-size:0}.cal-title[data-phase=enter]{animation:cal-title-enter var(--_settle) var(--_ease) both}.cal-title[data-phase=leave]{animation:cal-title-leave var(--_settle) var(--_ease) both}@keyframes cal-title-enter{0%{opacity:.9;transform:translateY(calc(var(--_offset, 0px) + 100%))}to{opacity:1;transform:none}}@keyframes cal-title-leave{0%{transform:translateY(var(--_offset,0))}to{opacity:.9;transform:translateY(-100%)}}.cal-page[data-phase=enter]{animation:cal-page-enter var(--_settle) var(--_ease) both}@keyframes cal-page-enter{0%{transform:translateX(calc(var(--_offset, 0px) + (100% + var(--_ring)*2)*var(--_travel, 1)*var(--_dir)))}to{transform:none}}@keyframes cal-page-leave{0%{transform:translateX(var(--_offset,0))}to{transform:translateX(calc((100% + var(--_ring)*2)*var(--_travel, 1)*var(--_dir)*-1))}}.cal-body[data-switching]{overflow:hidden;overflow:clip}.cal-view[data-shift=deeper]{--_view-from:-100%;--_view-to:100%}.cal-view[data-shift=shallower]{--_view-from:100%;--_view-to:-100%}.cal-view[data-phase=enter]{animation:cal-view-enter var(--_settle) var(--_ease) both}@keyframes cal-view-enter{0%{opacity:.9;transform:translateY(var(--_view-from,-100%))}to{opacity:1;transform:none}}@keyframes cal-view-leave{to{opacity:.9;transform:translateY(var(--_view-to,100%))}}.cal-day,.cal-pick{--_today-shadow:0 0 transparent;--_focus-shadow:0 0 transparent;align-items:center;border:var(--_border-width) solid transparent;border-radius:var(--_day-radius);box-shadow:var(--_today-shadow),var(--_focus-shadow);color:var(--_day-ink);cursor:pointer;display:flex;font-size:var(--_day-font-size);font-variant-numeric:tabular-nums;font-weight:var(--_day-weight);justify-content:center;min-inline-size:0;position:relative;transition:background-color calc(var(--_settle)/2) var(--_ease),border-color calc(var(--_settle)/2) var(--_ease),color calc(var(--_settle)/2) var(--_ease),transform calc(var(--_settle)/2) var(--_ease),box-shadow calc(var(--_settle)/2) var(--_ease);-webkit-user-select:none;-moz-user-select:none;user-select:none}.cal-day{--_column:calc(100cqi/7 - var(--_gap)*6/7);--_circle:min(var(--_cell),var(--_column));align-self:center;block-size:calc(var(--_cell) - (var(--_cell) - var(--_circle))*var(--_round));inline-size:calc(100% - (100% - var(--_circle))*var(--_round));justify-self:center;max-inline-size:var(--_day-max-width)}.cal-pick{overflow-wrap:anywhere;padding-inline:var(--_badge-padding);text-align:center}.cal-day-number,.cal-pick-text{line-height:1;transition:transform calc(var(--_settle)/2) var(--_ease)}.cal-day[data-kind=open],.cal-pick[data-kind=open]{border-color:var(--_cell-line)}.cal-day[data-kind=open]:hover,.cal-pick[data-kind=open]:hover{background-color:var(--_hover-bg);border-color:var(--_hover-line);color:var(--_hover-ink)}.cal-day[aria-disabled=true],.cal-pick[aria-disabled=true]{color:var(--_disabled-ink);cursor:not-allowed;font-weight:var(--_disabled-weight)}.cal-day[data-kind=closed]{opacity:var(--_blocked-opacity)}.cal-day[data-kind=closed] .cal-day-number{-webkit-text-decoration:var(--_blocked-decoration);text-decoration:var(--_blocked-decoration)}.cal-day[data-outside]{color:var(--_outside-ink);cursor:default}.cal-day[data-today],.cal-pick[data-today]{--_today-shadow:inset 0 0 0 var(--_today-width) var(--_today-ring);-webkit-text-stroke:var(--_today-stroke) currentColor}.cal-day[data-today] .cal-day-number,.cal-pick[data-today] .cal-pick-text{-webkit-text-decoration:var(--_today-decoration);text-decoration:var(--_today-decoration)}.cal-day[data-today]:after,.cal-pick[data-today]:after{background:var(--_today-dot);block-size:var(--_dot-size);border-radius:50%;content:"";inline-size:var(--_dot-size);inset-block-end:calc(var(--_dot-size) + 1px);inset-inline-start:50%;position:absolute;translate:calc(-50%*var(--_dir)) 0}.cal-day.has-badge[data-today]:after{inset-block-end:auto;inset-block-start:var(--_badge-inset)}.cal-day[data-selected],.cal-pick[data-selected]{--_today-shadow:0 0 transparent;-webkit-text-stroke:0 transparent}.cal-day[data-selected],.cal-day[data-selected]:hover,.cal-pick[data-selected],.cal-pick[data-selected]:hover{background-color:var(--_accent);border-color:var(--_accent);color:var(--_on-accent)}.cal-day[data-selected] .cal-day-number,.cal-pick[data-selected] .cal-pick-text{text-decoration:none}.cal-day[data-selected]:after,.cal-pick[data-selected]:after{background:transparent}.cal-day[data-selected][aria-disabled=true]{opacity:.55}.cal-day.has-badge .cal-day-number{transform:translateY(calc(var(--_badge-shift)*-1))}.cal-badge{background:var(--_tone-bg);border-radius:var(--_badge-radius);color:var(--_tone-ink);font-size:var(--_badge-size);font-weight:var(--_badge-weight);inset-block-end:var(--_badge-inset);inset-inline-start:50%;line-height:var(--_badge-line-height);max-inline-size:calc(100% - var(--_badge-inset)*2);overflow:hidden;padding-inline:var(--_badge-padding);position:absolute;text-overflow:clip;transition:opacity var(--_settle) var(--_ease);translate:calc(-50%*var(--_dir)) 0;white-space:nowrap}.cal-badge[data-empty]{opacity:0}.cal-badge[data-tone=neutral]{--_tone-bg:var(--_tone-neutral-bg);--_tone-ink:var(--_tone-neutral-ink)}.cal-badge[data-tone=info]{--_tone-bg:var(--_tone-info-bg);--_tone-ink:var(--_tone-info-ink)}.cal-badge[data-tone=success]{--_tone-bg:var(--_tone-success-bg);--_tone-ink:var(--_tone-success-ink)}.cal-badge[data-tone=warning]{--_tone-bg:var(--_tone-warning-bg);--_tone-ink:var(--_tone-warning-ink)}.cal-badge[data-tone=danger]{--_tone-bg:var(--_tone-danger-bg);--_tone-ink:var(--_tone-danger-ink)}.cal-day[data-selected] .cal-badge{background:var(--_on-accent);color:var(--_accent)}.cal-tip{background:var(--_tip-surface);border-radius:var(--_tip-radius);color:var(--_tip-ink);font-size:var(--_tip-size);font-variant-numeric:tabular-nums;font-weight:600;inset-block-start:0;left:0;line-height:1.3;max-inline-size:calc(100% - 8px);opacity:0;padding:4px 8px;pointer-events:none;position:absolute;transform:translate(calc(var(--_tip-x, 0px) - 50%),var(--_tip-y,0));transition:opacity calc(var(--_settle)/2) var(--_ease),transform calc(var(--_settle)/2) var(--_ease);white-space:nowrap;z-index:2}.cal-tip[data-shown]{opacity:1}.cal-root[data-busy] .cal-day[data-kind]{cursor:progress;opacity:.45}.cal-sr{clip:rect(0,0,0,0);block-size:1px;border-width:0;inline-size:1px;margin:-1px;overflow:hidden;padding:0;position:absolute;white-space:nowrap}.cal-root[data-today-label] .cal-today{padding-inline:var(--_today-padding)}.cal-root[data-today-label] .cal-today-text{display:inline}.cal-measure{block-size:0;inline-size:0;inset-block-start:0;inset-inline-start:0;overflow:hidden;pointer-events:none;position:absolute;visibility:hidden}.cal-measure .cal-measure-today,.cal-measure-title{inline-size:-moz-max-content;inline-size:max-content}.cal-measure .cal-measure-today{padding-inline:var(--_today-padding)}.cal-measure .cal-measure-today .cal-today-text{display:inline}@container shift-calendar (min-width: 340px){.cal-weekday-short{display:inline}.cal-weekday-narrow{display:none}}@media (prefers-reduced-motion:reduce){.cal-page[data-phase],.cal-slot-layer[data-phase],.cal-title[data-phase],.cal-view[data-phase]{animation:none}.cal-badge,.cal-button,.cal-day,.cal-day-number,.cal-pick,.cal-pick-text,.cal-tip,.cal-title-button{transition:none}}.pointer-events-none{pointer-events:none}.static{position:static}.left-0{left:0}.top-0{top:0}.h-full{height:100%}.min-h-\\[150px\\]{min-height:150px}.w-full{width:100%}.items-center{align-items:center}.justify-center{justify-content:center}.opacity-0{opacity:0}.transition-opacity{transition-duration:.15s;transition-property:opacity;transition-timing-function:cubic-bezier(.4,0,.2,1)}.duration-500{transition-duration:.5s}.fixed{position:fixed}.bottom-4{bottom:1rem}.bottom-\\[88px\\]{bottom:88px}.left-1\\/2{left:50%}.z-10{z-index:10}.z-\\[9999\\]{z-index:9999}.m-0{margin:0}.aspect-video{aspect-ratio:16/9}.size-8{height:2rem;width:2rem}.size-\\[22px\\]{height:22px;width:22px}.size-\\[32px\\]{height:32px;width:32px}.size-full{height:100%;width:100%}.h-\\[100dvh\\]{height:100dvh}.h-\\[60px\\]{height:60px}.min-h-full{min-height:100%}.w-\\[100dvw\\]{width:100dvw}.w-\\[100px\\]{width:100px}.min-w-\\[140px\\]{min-width:140px}.min-w-full{min-width:100%}.-translate-x-1\\/2{--tw-translate-x:-50%;transform:translate(var(--tw-translate-x),var(--tw-translate-y)) rotate(var(--tw-rotate)) skewX(var(--tw-skew-x)) skewY(var(--tw-skew-y)) scaleX(var(--tw-scale-x)) scaleY(var(--tw-scale-y))}.animate-spin{animation:spin 1s linear infinite}.cursor-pointer{cursor:pointer}.justify-between{justify-content:space-between}.gap-2{gap:.5rem}.whitespace-nowrap{white-space:nowrap}.rounded-full{border-radius:9999px}.rounded-lg{border-radius:.5rem}.border-slate-500{--tw-border-opacity:1;border-color:rgb(100 116 139/var(--tw-border-opacity,1))}.border-slate-600{--tw-border-opacity:1;border-color:rgb(71 85 105/var(--tw-border-opacity,1))}.bg-black{--tw-bg-opacity:1;background-color:rgb(0 0 0/var(--tw-bg-opacity,1))}.bg-black\\/30{background-color:rgba(0,0,0,.3)}.bg-black\\/55{background-color:rgba(0,0,0,.55)}.bg-slate-100{--tw-bg-opacity:1;background-color:rgb(241 245 249/var(--tw-bg-opacity,1))}.bg-white{--tw-bg-opacity:1;background-color:rgb(255 255 255/var(--tw-bg-opacity,1))}.object-cover{object-fit:cover}.object-center{object-position:center}.p-1{padding:.25rem}.p-\\[16px\\]{padding:16px}.px-3{padding-left:.75rem;padding-right:.75rem}.px-6{padding-left:1.5rem;padding-right:1.5rem}.py-1{padding-bottom:.25rem;padding-top:.25rem}.py-\\[10px\\]{padding-bottom:10px;padding-top:10px}.text-center{text-align:center}.text-\\[13px\\]{font-size:13px}.text-\\[15px\\]{font-size:15px}.text-\\[18px\\]{font-size:18px}.font-medium{font-weight:500}.leading-\\[1\\.3\\]{line-height:1.3}.text-slate-100{--tw-text-opacity:1;color:rgb(241 245 249/var(--tw-text-opacity,1))}.text-slate-500{--tw-text-opacity:1;color:rgb(100 116 139/var(--tw-text-opacity,1))}.text-slate-600{--tw-text-opacity:1;color:rgb(71 85 105/var(--tw-text-opacity,1))}.text-white{--tw-text-opacity:1;color:rgb(255 255 255/var(--tw-text-opacity,1))}.shadow,.shadow-lg{box-shadow:var(--tw-ring-offset-shadow,0 0 #0000),var(--tw-ring-shadow,0 0 #0000),var(--tw-shadow)}.shadow-lg{--tw-shadow:0 10px 15px -3px rgba(0,0,0,.1),0 4px 6px -4px rgba(0,0,0,.1);--tw-shadow-colored:0 10px 15px -3px var(--tw-shadow-color),0 4px 6px -4px var(--tw-shadow-color)}.shadow-md{--tw-shadow:0 4px 6px -1px rgba(0,0,0,.1),0 2px 4px -2px rgba(0,0,0,.1);--tw-shadow-colored:0 4px 6px -1px var(--tw-shadow-color),0 2px 4px -2px var(--tw-shadow-color);box-shadow:var(--tw-ring-offset-shadow,0 0 #0000),var(--tw-ring-shadow,0 0 #0000),var(--tw-shadow)}.outline-none{outline:2px solid transparent;outline-offset:2px}.filter{filter:var(--tw-blur) var(--tw-brightness) var(--tw-contrast) var(--tw-grayscale) var(--tw-hue-rotate) var(--tw-invert) var(--tw-saturate) var(--tw-sepia) var(--tw-drop-shadow)}.transition-all{transition-duration:.15s;transition-property:all;transition-timing-function:cubic-bezier(.4,0,.2,1)}.transition-colors{transition-duration:.15s;transition-property:color,background-color,border-color,text-decoration-color,fill,stroke;transition-timing-function:cubic-bezier(.4,0,.2,1)}.duration-300{transition-duration:.3s}.hover\\:border-slate-700:hover{--tw-border-opacity:1;border-color:rgb(51 65 85/var(--tw-border-opacity,1))}.hover\\:bg-slate-300:hover{--tw-bg-opacity:1;background-color:rgb(203 213 225/var(--tw-bg-opacity,1))}.hover\\:text-slate-700:hover{--tw-text-opacity:1;color:rgb(51 65 85/var(--tw-text-opacity,1))}.disabled\\:bg-white\\/75:disabled{background-color:hsla(0,0%,100%,.75)}@media (min-width:768px){.md\\:relative{position:relative}.md\\:h-auto{height:auto}.md\\:w-\\[600px\\]{width:600px}.md\\:\\!translate-x-0{--tw-translate-x:0px!important;transform:translate(var(--tw-translate-x),var(--tw-translate-y)) rotate(var(--tw-rotate)) skewX(var(--tw-skew-x)) skewY(var(--tw-skew-y)) scaleX(var(--tw-scale-x)) scaleY(var(--tw-scale-y))!important}.md\\:overflow-hidden{overflow:hidden}.md\\:rounded-lg{border-radius:.5rem}.md\\:border-none{border-style:none}.md\\:bg-white{--tw-bg-opacity:1;background-color:rgb(255 255 255/var(--tw-bg-opacity,1))}.md\\:py-\\[8px\\]{padding-bottom:8px;padding-top:8px}.md\\:text-\\[24px\\]{font-size:24px}.md\\:text-black{--tw-text-opacity:1;color:rgb(0 0 0/var(--tw-text-opacity,1))}.md\\:\\!opacity-100{opacity:1!important}.md\\:transition-all{transition-duration:.15s;transition-property:all;transition-timing-function:cubic-bezier(.4,0,.2,1)}.md\\:duration-300{transition-duration:.3s}.md\\:hover\\:bg-slate-100:hover{--tw-bg-opacity:1;background-color:rgb(241 245 249/var(--tw-bg-opacity,1))}}.visible{visibility:visible}.contents{display:contents}';
  }
}, [1, "shift-calendar", { value: [1537], min: [1], max: [1], month: [1537], disabledDates: [1, "disabled-dates"], disabledWeekdays: [1, "disabled-weekdays"], enabledDates: [1, "enabled-dates"], isDateDisabled: [16], dayMeta: [1, "day-meta"], today: [1], highlightToday: [4, "highlight-today"], language: [1], weekStartsOn: [2, "week-starts-on"], numerals: [1], label: [1], disabled: [516], busy: [516], disableViews: [4, "disable-views"], showToday: [4, "show-today"], appearance: [513], colorScheme: [513, "color-scheme"], size: [513], view: [32], pageYear: [32], yearsStart: [32], focusDate: [32], focusIndex: [32], leavingPage: [32], leavingTitle: [32], leavingYear: [32], leavingMonthName: [32], leavingView: [32], tip: [32], setFocus: [64] }, void 0, { month: [{ onMonthChange: 0 }], value: [{ onValueChange: 0 }], min: [{ onRangeChange: 0 }], max: [{ onRangeChange: 0 }], disableViews: [{ onDisableViews: 0 }] }]);
function ov() {
  typeof customElements < "u" && ["shift-calendar"].forEach(((n) => {
    n === "shift-calendar" && (customElements.get(n) || customElements.define(n, V_));
  }));
}
ov();
/*!
 * Built by ShiftSoftware
 * Copyright (c)
 */
const X_ = /^(\d{4}-\d{2}-\d{2})\s+(\d{1,2}):(\d{2})\s*([AaPp][Mm])$/, eo = (n) => String(n).padStart(2, "0");
function uv(n) {
  if (typeof n != "string") return null;
  const i = n.trim().match(X_);
  if (!i || !xt(i[1])) return null;
  const s = Number(i[2]), r = Number(i[3]);
  if (s < 1 || s > 12 || r > 59) return null;
  const c = s % 12 + (i[4].toUpperCase() === "PM" ? 12 : 0);
  return { date: i[1], time: `${eo(c)}:${eo(r)}` };
}
const cs = (n) => {
  const i = uv(n);
  return i ? `${i.date}T${i.time}` : "";
};
function mm(n) {
  return !!(n != null && n.url && n.branchId && n.departmentId && n.brandId);
}
const vm = /* @__PURE__ */ new Map(), G_ = /^[+-](0\d|1[0-4]):[0-5]\d$/, tf = /yyyy|MM|dd|HH|hh|mm|ss|tt/g, gm = /^(\d{4})-(\d{2})-(\d{2})[T ](\d{2}):(\d{2})(?::\d{2}(?:\.\d+)?)?(Z|[+-]\d{2}:?\d{2})?$/, cv = /^([+-])(\d{2}):?(\d{2})$/, fs = (n) => String(n).padStart(2, "0"), af = (n) => {
  if (n === "Z") return 0;
  const i = cv.exec(n || "");
  return i ? (i[1] === "-" ? -1 : 1) * (60 * Number(i[2]) + Number(i[3])) : null;
}, fv = (n) => !n || n.trim().toLowerCase() === "iso", Nc = (n, i, s, r, c) => `${n}-${fs(i)}-${fs(s)}T${fs(r)}:${fs(c)}`;
function bm(n, i, s) {
  const r = /^(\d{4})-(\d{2})-(\d{2})T(\d{2}):(\d{2})$/.exec(n || "");
  if (!r) return "";
  if (fv(i)) return af(s) === null ? `${n}:00` : (function(d, h) {
    return /^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}$/.test(d || "") ? `${d}:00${h && G_.test(h) ? h : (function(g) {
      const [v, y] = g.split("T"), [m, _, x] = v.split("-").map(Number), [T, A] = y.split(":").map(Number), z = -new Date(m, _ - 1, x, T, A).getTimezoneOffset(), q = Math.abs(z);
      return `${z < 0 ? "-" : "+"}${eo(Math.floor(q / 60))}:${eo(q % 60)}`;
    })(d)}` : "";
  })(n, s);
  const c = Number(r[4]), f = { yyyy: r[1], MM: r[2], dd: r[3], HH: r[4], hh: fs(c % 12 || 12), mm: r[5], ss: "00", tt: c < 12 ? "AM" : "PM" };
  return i.replace(tf, ((d) => f[d]));
}
const Q_ = ["idle", "loading", "empty", "error", "pickDay", "pickTime", "selected", "invalid"], ym = ["branchId", "departmentId", "brandId"], Z_ = hf(class extends of {
  constructor(n) {
    super(), n !== !1 && this.__registerHost(), this.__attachShadow(), this.slotChange = Tn(this, "slotChange"), this.pickerChange = Tn(this, "pickerChange"), this.pickerStatus = Tn(this, "pickerStatus"), this.name = "bookingCalendar", this.slotCounts = !0, this.dayTooltips = !0, this.fewSlots = 3, this.showToday = !0, this.hourCycle = "h23", this.valueFormat = "iso", this.value = "", this.showLabel = !1, this.showStatus = !1, this.showEmptyState = !0, this.isRequired = !1, this.isDisabled = !1, this.language = "en", this.localization = {}, this.colorScheme = "light", this.size = "md", this.status = "idle", this.availability = null, this.selectedDate = "", this.selectedRaw = "", this.view = "days", this.leavingTime = null, this.announced = !1, this.loader = /* @__PURE__ */ (function(i = (s, r) => fetch(s, r)) {
      let s = null;
      const r = () => {
        s == null || s.abort(), s = null;
      };
      return { load: async function(c, f = {}) {
        r();
        const d = (function(_, x) {
          var T;
          if (!mm(_)) return null;
          const A = (T = xt(x)) !== null && T !== void 0 ? T : nv(), z = new URLSearchParams({ from: A, to: ni(A, 30), branchId: String(_.branchId), departmentId: String(_.departmentId), brandId: String(_.brandId) });
          return `${_.url}${_.url.includes("?") ? "&" : "?"}${z}`;
        })(c, f.today);
        if (!d) return { status: "cancelled" };
        const h = f.language || "en", g = ((_, x) => `${x} ${_}`)(d, h), v = vm.get(g);
        if (v) return v;
        const y = new AbortController();
        let m;
        s = y;
        try {
          const _ = await i(d, { signal: y.signal, headers: { "Accept-Language": h } });
          if (_.ok) {
            const x = (function(A) {
              var z, q, $;
              if (!Array.isArray(A)) return null;
              const j = /* @__PURE__ */ new Map();
              for (const L of A) {
                const V = xt((q = (z = L == null ? void 0 : L.Date) === null || z === void 0 ? void 0 : z.slice) === null || q === void 0 ? void 0 : q.call(z, 0, 10));
                if (!V || !Array.isArray(L.Times)) continue;
                const J = ($ = j.get(V)) !== null && $ !== void 0 ? $ : /* @__PURE__ */ new Map();
                for (const Z of L.Times) {
                  const F = uv(Z);
                  F && F.date === V && !J.has(F.time) && J.set(F.time, String(Z).trim());
                }
                j.set(V, J);
              }
              return [...j].filter((([, L]) => L.size)).sort((([L], [V]) => L.localeCompare(V))).map((([L, V]) => ({ date: L, times: [...V].sort((([J], [Z]) => J.localeCompare(Z))).map((([J, Z]) => ({ time: J, raw: Z }))) })));
            })(await _.json().catch((() => {
            }))), T = x && (function(A) {
              return A.length ? { days: A, enabledDates: A.map(((z) => z.date)), slotCounts: Object.fromEntries(A.map(((z) => [z.date, z.times.length]))), first: A[0].date, last: A[A.length - 1].date } : null;
            })(x);
            m = x ? T ? { status: "ready", availability: T } : { status: "empty" } : { status: "error", error: { kind: "payload" } };
          } else m = { status: "error", error: { kind: "http", status: _.status } };
        } catch {
          m = y.signal.aborted ? { status: "cancelled" } : { status: "error", error: { kind: "network" } };
        }
        return y.signal.aborted ? { status: "cancelled" } : (s === y && (s = null), m.status !== "ready" && m.status !== "empty" || vm.set(g, m), m);
      }, cancel: r };
    })(), this.syncQueued = !1, this.loadWanted = !1, this.current = "", this.identity = { branchId: "", departmentId: "", brandId: "" }, this.valueTouched = !1, this.lastEmit = `
`, this.pendingFocus = null, this.frameKey = "", this.observedRoot = null, this.labelId = `shift-booking-calendar-label-${Math.random().toString(36).slice(2, 10)}`, this.titleId = `shift-booking-calendar-title-${Math.random().toString(36).slice(2, 10)}`, this.emitted = "", this.completing = !1, this.statusText = "", this.detached = !1, this.onDateChange = (i) => {
      var s, r;
      i.stopPropagation();
      const c = i.detail.value, f = (s = this.availability) === null || s === void 0 ? void 0 : s.days.find(((g) => g.date === c));
      if (this.status !== "ready" || !f || !this.isOpen(c)) return;
      const d = this.selectedRaw ? f.times.find(((g) => g.time === this.timeOf(this.selectedRaw))) : void 0, h = (r = d == null ? void 0 : d.raw) !== null && r !== void 0 ? r : "";
      this.selectedDate = c, h !== this.selectedRaw && (this.selectedRaw = h, this.current = h ? cs(h) : "", this.emitPicker(), d && this.commit(h)), this.openTimes();
    }, this.stop = (i) => i.stopPropagation(), this.onMonthChange = (i) => {
      i.stopPropagation(), this.month = i.detail.month;
    }, this.onTimeChange = (i) => {
      var s;
      i.stopPropagation();
      const r = (s = this.day) === null || s === void 0 ? void 0 : s.times.find(((f) => f.time === i.detail.value));
      if (!r) return;
      if (r.raw === this.selectedRaw) return this.completing = !0, void this.emitPicker();
      const c = this.timeText();
      this.completing = !0, this.selectedRaw = r.raw, this.current = cs(r.raw), this.emitPicker(), this.rollTime(c), this.commit(r.raw);
    }, this.onTimeSettled = (i) => {
      i.target === i.currentTarget && (this.leavingTime = null);
    }, this.onTimesKeyDown = (i) => {
      i.key === "Escape" ? this.view === "times" && (i.preventDefault(), i.stopPropagation(), this.closeTimes(!0)) : requestAnimationFrame((() => {
        var s, r, c;
        const f = (r = (s = this.slotsEl) === null || s === void 0 ? void 0 : s.shadowRoot) === null || r === void 0 ? void 0 : r.activeElement;
        !((c = f == null ? void 0 : f.classList) === null || c === void 0) && c.contains("ts-chip") && this.reveal(f, !1);
      }));
    }, this.onTimesFocus = (i) => {
      var s;
      const r = i.composedPath()[0];
      !((s = r == null ? void 0 : r.classList) === null || s === void 0) && s.contains("ts-chip") && this.reveal(r, !1);
    };
  }
  connectedCallback() {
    this.detached && (this.detached = !1, this.watchFrame(), this.status === "loading" && this.queueSync(!0));
  }
  componentWillLoad() {
    var n;
    (n = this.form) === null || n === void 0 || n.subscribe(this.name, this), this.current = this.read(this.value) || this.read(this.defaultValue), this.identity = this.ids(), this.emitted = this.value || "", this.load();
  }
  componentDidLoad() {
    this.emitStatus(), this.current && this.emitPicker();
  }
  componentDidRender() {
    var n, i;
    this.watchFrame();
    const s = this.pendingFocus;
    if (this.pendingFocus = null, s === "day" && ((n = this.calendarEl) === null || n === void 0 || n.setFocus()), s === "times") {
      const r = this.slotsEl;
      (i = r == null ? void 0 : r.componentOnReady) === null || i === void 0 || i.call(r).then((() => {
        var c;
        this.view === "times" && (this.reveal((c = r.shadowRoot) === null || c === void 0 ? void 0 : c.querySelector("[data-selected]"), !0), r.setFocus());
      }));
    }
  }
  disconnectedCallback() {
    var n, i;
    this.detached = !0, this.loader.cancel(), (n = this.form) === null || n === void 0 || n.unsubscribe(this.name), clearTimeout(this.timeTimer), (i = this.frameObserver) === null || i === void 0 || i.disconnect(), this.observedRoot = null;
  }
  onTargetChange() {
    this.closeTimes(), this.queueSync(!0);
  }
  onRulesChange() {
    this.selectedDate && !this.isOpen(this.selectedDate) && (this.selectedRaw = "", this.selectedDate = "", this.closeTimes());
  }
  onStatusChange() {
    this.emitStatus();
  }
  onLanguageChange() {
    this.emitStatus(), this.emitPicker();
  }
  onFormatChange() {
    this.emitPicker();
  }
  onValueChange(n) {
    if ((n || "") === this.emitted) return;
    const i = this.read(n);
    if (this.emitted = n || "", this.valueTouched = !0, this.queueSync(!1), i !== this.current) {
      if (this.current = i, !i) return this.lastEmit = `
`, this.selectedRaw = "", this.selectedDate = "", void this.closeTimes();
      this.applyPreset();
    }
  }
  async refresh() {
    this.load();
  }
  async clear() {
    this.reset("");
  }
  async setBlazorRef(n) {
    this.blazorRef = n, await this.reportCurrent();
  }
  onChangeCallbackChange() {
    typeof this.changeCallback == "function" && this.reportCurrent();
  }
  async reportCurrent() {
    const n = this.slotLabel(this.current);
    n && await am.bind(this)(this.changeCallback, bm(this.current, this.valueFormat, this.offset()), n, !1);
  }
  async getValueLabel() {
    return this.slotLabel(this.current);
  }
  async setFocus() {
    var n, i;
    this.view === "times" ? await ((n = this.slotsEl) === null || n === void 0 ? void 0 : n.setFocus()) : await ((i = this.calendarEl) === null || i === void 0 ? void 0 : i.setFocus());
  }
  reset(n) {
    var i;
    this.defaultValue = (i = n) !== null && i !== void 0 ? i : "", this.current = this.read(this.defaultValue), this.selectedRaw = "", this.selectedDate = "", this.closeTimes(), this.applyPreset();
  }
  getValue() {
    return this.current;
  }
  validate() {
    const n = (i) => i.required(Xa.require(this.name));
    return vf().meta(Xa.meta(this.name)).when(Xa.condition(this.name), { is: !0, then: n, otherwise: (i) => this.isRequired ? n(i) : i.optional() });
  }
  get locale() {
    return _f(this.language);
  }
  get strings() {
    return sv(this.locale.language);
  }
  get calendarEl() {
    var n;
    return (n = this.el.shadowRoot) === null || n === void 0 ? void 0 : n.querySelector("shift-calendar");
  }
  get slotsEl() {
    var n;
    return (n = this.el.shadowRoot) === null || n === void 0 ? void 0 : n.querySelector("shift-time-slots");
  }
  get day() {
    var n;
    return (n = this.availability) === null || n === void 0 ? void 0 : n.days.find(((i) => i.date === this.selectedDate));
  }
  isOpen(n) {
    var i;
    const s = lv({ disabledWeekdays: this.disabledWeekdays, disabledDates: this.disabledDates });
    return !!(!((i = this.availability) === null || i === void 0) && i.days.some(((r) => r.date === n))) && !Pc(n, s);
  }
  firstOpenDay() {
    var n;
    return (n = this.availability) === null || n === void 0 ? void 0 : n.days.find(((i) => this.isOpen(i.date)));
  }
  read(n) {
    return (function(i, s, r) {
      const c = (i ?? "").trim();
      if (!c) return "";
      const f = gm.exec(c);
      if (f) {
        const [d, h, g, v, y] = f.slice(1, 6).map(Number), m = f[6] ? af(f[6]) : null, _ = af(r);
        if (m === null || _ === null || m === _) return Nc(d, h, g, v, y);
        const x = /* @__PURE__ */ new Date(0);
        return x.setUTCFullYear(d, h - 1, g), x.setUTCHours(v, y + _ - m), Nc(x.getUTCFullYear(), x.getUTCMonth() + 1, x.getUTCDate(), x.getUTCHours(), x.getUTCMinutes());
      }
      return fv(s) ? "" : (function(d, h) {
        var g, v, y, m;
        const _ = (g = h.match(tf)) !== null && g !== void 0 ? g : [], x = h.split(tf).map(((J, Z) => {
          return J.replace(/[.*+?^${}()|[\]\\]/g, "\\$&") + (_[Z] ? (F = _[Z]) === "yyyy" ? "(\\d{4})" : F === "tt" ? "([AaPp][Mm])" : "(\\d{1,2})" : "");
          var F;
        })).join(""), T = new RegExp(`^${x}$`).exec(d.trim());
        if (!T) return "";
        const A = (J) => _.includes(J) ? T[_.indexOf(J) + 1] : void 0, z = /p/i.test((v = A("tt")) !== null && v !== void 0 ? v : ""), q = A("HH") !== void 0 ? Number(A("HH")) : Number((y = A("hh")) !== null && y !== void 0 ? y : 0) % 12 + (z ? 12 : 0), [$, j, L, V] = [Number(A("yyyy")), Number(A("MM")), Number(A("dd")), Number((m = A("mm")) !== null && m !== void 0 ? m : 0)];
        return !$ || j < 1 || j > 12 || L < 1 || L > 31 || q > 23 || V > 59 ? "" : Nc($, j, L, q, V);
      })(c, s);
    })(n || "", this.valueFormat, this.offset());
  }
  offset() {
    return this.utcOffset || (function(n) {
      var i;
      const s = (i = gm.exec((n ?? "").trim())) === null || i === void 0 ? void 0 : i[6];
      if (!s) return "";
      if (s === "Z") return "+00:00";
      const r = cv.exec(s);
      return r ? `${r[1]}${r[2]}:${r[3]}` : "";
    })(this.value) || void 0;
  }
  ids() {
    return Object.fromEntries(ym.map(((n) => [n, this[n] ? String(this[n]) : ""])));
  }
  queueSync(n) {
    this.loadWanted || (this.loadWanted = n), this.syncQueued || (this.syncQueued = !0, queueMicrotask((() => {
      this.syncQueued = !1, this.checkIdentity(), this.loadWanted && (this.loadWanted = !1, this.load());
    })));
  }
  checkIdentity() {
    const n = this.ids(), i = ym.some(((r) => this.identity[r] && n[r] !== this.identity[r])), s = this.valueTouched;
    this.identity = n, this.valueTouched = !1, i && !s && this.current && (this.current = "", this.defaultValue = "", this.selectedRaw = "", this.selectedDate = "", this.closeTimes(), this.emitPicker());
  }
  async load() {
    const n = { url: this.calendarApi, branchId: this.branchId, departmentId: this.departmentId, brandId: this.brandId };
    if (!mm(n)) return this.loader.cancel(), this.status = "idle", this.availability = null, this.selectedDate = "", void this.closeTimes();
    this.status = "loading", this.selectedRaw = "", this.selectedDate = "", this.closeTimes();
    const i = await this.loader.load(n, { today: this.today, language: this.language });
    if (i.status !== "cancelled") {
      if (i.status !== "ready") return this.availability = null, this.selectedDate = "", void (this.status = i.status);
      this.availability = i.availability, this.firstOpenDay() ? (this.applyPreset(), this.status = "ready") : this.status = "empty";
    }
  }
  applyPreset() {
    var n, i, s;
    if (this.availability) {
      const r = this.current, c = r ? this.availability.days.find(((d) => r.startsWith(d.date) && this.isOpen(d.date))) : void 0, f = c ?? this.firstOpenDay();
      this.selectedDate = (n = c == null ? void 0 : c.date) !== null && n !== void 0 ? n : "", this.selectedRaw = (s = (i = c == null ? void 0 : c.times.find(((d) => cs(d.raw) === r))) === null || i === void 0 ? void 0 : i.raw) !== null && s !== void 0 ? s : "", f && (this.month = Le(f.date)), this.selectedRaw && (this.view = "times");
    }
    this.emitPicker();
  }
  emitPicker() {
    const n = bm(this.current, this.valueFormat, this.offset()), i = this.slotLabel(this.current), s = this.completing, r = `${n}
${i}`;
    this.completing = !1, (r !== this.lastEmit || s) && (this.lastEmit = r, this.emitted = n, this.value = n, this.pickerChange.emit({ value: n, label: i, complete: s }), am.bind(this)(this.changeCallback, n, i, s));
  }
  emitStatus() {
    const n = this.status === "loading" || this.status === "error" ? this.strings[this.status] : "";
    n !== this.statusText && (this.statusText = n, this.pickerStatus.emit({ text: n, busy: this.status === "loading" }));
  }
  slotLabel(n) {
    if (!n) return "";
    const i = this.locale, [s, r] = n.split("T"), [c, f, d] = Dn(s);
    return Pn(this.strings.slotLabel, { weekday: i.strings.weekdaysShort[vs(s)], day: nt(d, i.numerals), month: this.strings.monthsShort[f - 1], year: nt(c, i.numerals), time: ll(r, this.hourCycle, i.numerals, this.strings) });
  }
  openTimes() {
    clearTimeout(this.timeTimer), this.leavingTime = null, this.announced = !0, this.view = "times", this.pendingFocus = "times";
  }
  closeTimes(n = this.focusIsInTimes()) {
    this.view === "times" && (clearTimeout(this.timeTimer), this.leavingTime = null, this.announced = !0, this.view = "days", n && (this.pendingFocus = "day"));
  }
  focusIsInTimes() {
    var n, i, s;
    const r = (n = this.el.shadowRoot) === null || n === void 0 ? void 0 : n.activeElement;
    return !!r && !!(!((s = (i = this.el.shadowRoot) === null || i === void 0 ? void 0 : i.querySelector(".bc-times")) === null || s === void 0) && s.contains(r));
  }
  rollTime(n) {
    var i;
    clearTimeout(this.timeTimer), this.view !== "times" || n === this.timeText() || !((i = window.matchMedia) === null || i === void 0) && i.call(window, "(prefers-reduced-motion: reduce)").matches ? this.leavingTime = null : (this.leavingTime = n, this.timeTimer = setTimeout((() => this.leavingTime = null), 1500));
  }
  reveal(n, i) {
    var s;
    const r = (s = this.el.shadowRoot) === null || s === void 0 ? void 0 : s.querySelector(".bc-times-scroll");
    if (!r) return;
    if (!n) return void (i && (r.scrollTop = 0));
    const c = r.getBoundingClientRect(), f = n.getBoundingClientRect(), d = f.top - c.top + r.scrollTop, h = r.clientHeight;
    i ? r.scrollTop = Math.max(0, d - (h - f.height) / 2) : f.top < c.top ? r.scrollTop = d : f.bottom > c.bottom && (r.scrollTop = d + f.height - h);
  }
  commit(n) {
    var i;
    const s = cs(n);
    this.slotChange.emit({ date: n.split(" ")[0], raw: n, value: s }), (i = this.form) === null || i === void 0 || i.validateForm(this.name, s);
  }
  timeOf(n) {
    return cs(n).slice(11);
  }
  timeText() {
    return this.selectedRaw && this.selectedRaw.startsWith(this.selectedDate) ? `· ${ll(this.timeOf(this.selectedRaw), this.hourCycle, this.locale.numerals, this.strings)}` : "";
  }
  dayTitle(n) {
    if (!n) return "";
    const i = this.locale, [, s, r] = Dn(n);
    return Pn(this.strings.dayTitle, { weekday: i.strings.weekdaysShort[vs(n)], day: nt(r, i.numerals), month: this.strings.monthsShort[s - 1] });
  }
  watchFrame() {
    var n, i, s;
    const r = (i = (n = this.calendarEl) === null || n === void 0 ? void 0 : n.shadowRoot) === null || i === void 0 ? void 0 : i.querySelector(".cal-root");
    r && r !== this.observedRoot && typeof ResizeObserver < "u" && ((s = this.frameObserver) === null || s === void 0 || s.disconnect(), this.frameObserver = new ResizeObserver((() => this.syncFrame())), this.frameObserver.observe(r), this.observedRoot = r), this.syncFrame();
  }
  syncFrame() {
    var n, i;
    const s = (n = this.calendarEl) === null || n === void 0 ? void 0 : n.shadowRoot, r = s == null ? void 0 : s.querySelector(".cal-root"), c = s == null ? void 0 : s.querySelector(".cal-header"), f = s == null ? void 0 : s.querySelector(".cal-title-band"), d = s == null ? void 0 : s.querySelector(".cal-title[data-current] .cal-title-button");
    if (!(r && c && f && d && typeof getComputedStyle == "function" && typeof r.getClientRects == "function" && r.getClientRects().length)) return;
    const h = getComputedStyle(r), g = getComputedStyle(d), v = { "--_f-border": `${h.borderTopWidth} ${h.borderRightWidth} ${h.borderBottomWidth} ${h.borderLeftWidth}`, "--_f-radius": `${h.borderTopLeftRadius} ${h.borderTopRightRadius} ${h.borderBottomRightRadius} ${h.borderBottomLeftRadius}`, "--_f-padding": `${h.paddingTop} ${h.paddingRight} ${h.paddingBottom} ${h.paddingLeft}`, "--_f-travel": h.paddingLeft, "--_f-gap": h.rowGap, "--_f-head": `${c.offsetHeight}px`, "--_f-top": `${c.offsetTop + c.offsetHeight + (parseFloat(h.rowGap) || 0)}px`, "--_f-head-gap": getComputedStyle(c).columnGap, "--_f-band": getComputedStyle(f).marginInlineStart, "--_f-title-font": g.fontFamily, "--_f-title-size": g.fontSize, "--_f-title-weight": g.fontWeight, "--_f-title-line": g.lineHeight, "--_f-title-padding": g.paddingInlineStart }, y = JSON.stringify(v), m = (i = this.el.shadowRoot) === null || i === void 0 ? void 0 : i.querySelector(".bc-box");
    if (m && y !== this.frameKey) {
      this.frameKey = y;
      for (const [_, x] of Object.entries(v)) m.style.setProperty(_, x);
    }
  }
  daySummary(n) {
    if (!(n != null && n.times.length)) return "";
    const i = this.strings, s = this.locale.numerals, r = n.times.length;
    return Pn(i.daySummary, { count: Pn(r === 1 ? i.timesCountOne : i.timesCount, { count: nt(r, s) }), from: ll(n.times[0].time, this.hourCycle, s, i), to: ll(n.times[r - 1].time, this.hourCycle, s, i) });
  }
  get dayMeta() {
    if (this.availability) return Object.fromEntries(this.availability.days.map(((n) => {
      const i = this.daySummary(n);
      return [n.date, { badge: this.slotCounts ? String(n.times.length) : void 0, tone: n.times.length <= this.fewSlots ? "warning" : "neutral", tooltip: this.dayTooltips ? i : void 0, description: i }];
    })));
  }
  message(n) {
    return this.status !== "ready" ? this.status : n ? "invalid" : this.selectedRaw ? "selected" : this.view === "times" ? "pickTime" : "pickDay";
  }
  messageText(n, i) {
    const s = this.strings, r = this.locale;
    return n === "invalid" ? i : n === "selected" ? this.selectedRaw ? Pn(s.selected, { date: Pr(this.selectedRaw.slice(0, 10), r, r.numerals), time: ll(this.timeOf(this.selectedRaw), this.hourCycle, r.numerals, s) }) : "" : s[n];
  }
  renderTimeSlot() {
    const n = this.timeText(), i = this.leavingTime !== null && this.leavingTime !== n ? this.leavingTime : null;
    return X("span", { class: "bc-slot", part: "times-time" }, i && X("span", { key: `time-${i}`, class: "bc-slot-layer", "data-phase": "leave", "aria-hidden": "true" }, i), X("span", { key: `time-${n}`, class: "bc-slot-layer", "data-phase": i !== null ? "enter" : "rest", onAnimationEnd: i !== null ? this.onTimeSettled : void 0 }, n));
  }
  render() {
    var n, i, s;
    const r = this.locale, c = this.strings, f = (n = this.form) === null || n === void 0 ? void 0 : n.getInputState(this.name), d = this.form ? S_(this, f == null ? void 0 : f.meta, f == null ? void 0 : f.errorMessage) : null, h = (d != null && d.label && d.label !== Xa.label(this.name) ? d.label : "") || ((s = (i = this.localization) === null || i === void 0 ? void 0 : i[this.language]) === null || s === void 0 ? void 0 : s.label) || this.label || c.bookingCalendar, g = !!(f != null && f.isRequired) || this.isRequired, v = !!(f != null && f.isError), y = d != null && d.errorTextMessage && d.errorTextMessage !== Xa.require(this.name) ? d.errorTextMessage : c.required, m = this.isDisabled || !!(f != null && f.disabled), _ = this.status === "ready", x = this.view === "times", T = this.message(v), A = this.day, z = this.selectedDate ? Pr(this.selectedDate, r, r.numerals) : "", q = z ? Pn(c.timesOn, { date: z }) : c.times, $ = this.status === "empty" && this.showEmptyState, j = this.announced ? x ? q : this.month ? N_(this.month, r, r.numerals) : "" : "";
    return X(to, { key: "a6ad1debee32122425a030be976f86d72db46489", translate: "no" }, X("div", { key: "035f01b65caf1acbf97d07897309ea73a684318c", class: { "bc-root": !0, [this.wrapperClass]: !!this.wrapperClass }, part: "root", id: this.wrapperId, dir: r.direction, lang: r.language === "ku" ? "ckb" : r.language, role: "group", "aria-labelledby": this.labelId, "aria-disabled": m ? "true" : void 0, "data-status": this.status, "data-view": this.view }, X("div", { key: "db911835f58f0ae52ac3f0759efdf19fcd75f702", class: "bc-collapse", "data-open": this.showLabel ? "" : void 0, "aria-hidden": this.showLabel ? void 0 : "true" }, X("div", { key: "f81770366b40db7452ae75d3c2e5d65cd11757b2", class: "bc-collapse-body" }, X("div", { key: "2403f3e077a2e2a814ff06a47d9c191510a6799e", class: "bc-label", part: "label", id: this.labelId }, h, X("span", { key: "56e1ab70e35094cdc82cbd1a6b1ca7dee36dbb8f", class: "bc-required", part: "required", "aria-hidden": "true", "data-hidden": g ? void 0 : "" }, "*")))), X("div", { key: "974d8f3156aa6b663c6baf57914e793976a105b3", class: "bc-box", part: "box", "data-view": this.view }, X("shift-calendar", { key: "0652e125d6cb78f4153642651e7af2a1fd6ab0ab", part: "calendar", label: h, language: this.language, today: this.today, appearance: this.appearance, colorScheme: this.colorScheme, size: this.size, value: this.selectedDate, month: this.month, min: _ ? this.availability.first : void 0, max: _ ? this.availability.last : void 0, enabledDates: _ ? this.availability.enabledDates : [], disabledDates: this.disabledDates, disabledWeekdays: this.disabledWeekdays, dayMeta: _ ? this.dayMeta : void 0, showToday: this.showToday, busy: this.status === "loading", disabled: m, "aria-hidden": x ? "true" : void 0, inert: !!x || void 0, onDateChange: this.onDateChange, onMonthChange: this.onMonthChange, onPickerChange: this.stop }), X("div", { key: "a3c5cb7341ed3fa37c2b951ff72aa92a06aed625", class: "bc-empty", part: "empty", "aria-hidden": "true", "data-shown": $ ? "" : void 0 }, X("svg", { key: "ba5c09ff0870ec34ed140ad5272b287edd95db5e", class: "bc-empty-art", viewBox: "0 0 48 48", focusable: "false" }, X("rect", { key: "3c2ccaf3dce19544d1173ec23f7d0b086541564e", x: "7", y: "10", width: "34", height: "31", rx: "5", fill: "none", stroke: "currentColor", "stroke-width": "2" }), X("path", { key: "b184af7a452c7c4855ba9004e29f797abcc799c6", d: "M7 19h34M16 6v8M32 6v8", fill: "none", stroke: "currentColor", "stroke-width": "2", "stroke-linecap": "round" }), X("circle", { key: "829b95cc112ad2f52b880469408a238ce27193c3", cx: "24", cy: "30", r: "6", fill: "none", stroke: "currentColor", "stroke-width": "1.6", opacity: "0.55" }), X("path", { key: "e3f88a548f40881775412bf5bf9316bf75564fc1", d: "M24 27v3l2 1.5", fill: "none", stroke: "currentColor", "stroke-width": "1.6", "stroke-linecap": "round", opacity: "0.55" }), X("path", { key: "1f207a335b7461efa1a6d77762ab0a717f0da2c6", d: "M11 38 37 13", fill: "none", stroke: "currentColor", "stroke-width": "2", "stroke-linecap": "round" })), X("span", { key: "46dce6b3317776d1a90089853c22edd2bfcd1fae", class: "bc-empty-text" }, c.emptyTitle)), X("div", { key: "8c0d1bf4b20b4d35561942a8773377752e0334ae", class: "bc-times", part: "times", role: "group", "aria-labelledby": this.titleId, "aria-hidden": x ? void 0 : "true", inert: !x || void 0, onKeyDown: this.onTimesKeyDown, onFocusin: this.onTimesFocus }, X("div", { key: "4aa78fac3ce01ada7bca2720c9abbbba4fa943e5", class: "bc-times-header", part: "times-header" }, X("div", { key: "9159d6dfbc843b8e43f4cca68f55da3833636636", class: "bc-title-band" }, X("div", { key: "0dd8b3b05f98ae5c1b6bc0183c8aeaaead164157", class: "bc-title", part: "times-heading", id: this.titleId }, X("span", { key: "03b87d2f503ded828bbfff1f738598ce4d30620a", class: "bc-title-line" }, X("span", { key: "16288e4bff4c786d3aa22e81c0167900f5c88081", class: "bc-title-day" }, this.dayTitle(this.selectedDate)), this.renderTimeSlot()), X("span", { key: "707dfae77774193655d21bb9e343944623cb9933", class: "bc-title-summary", part: "times-summary" }, this.daySummary(A)))), X("div", { key: "f9e27fe6ac80d0f2ef7fc1edbc4bbabd27d24e65", class: "bc-back-band" }, X("button", { key: "7ec1952891397a07fc8239e7ed39ea23ac96ec97", type: "button", class: "bc-back", part: "back", "aria-label": c.changeDate, onClick: () => this.closeTimes(!0) }, X(ev, { key: "f9f818deb2b55d77008aa9f366fd783cda2d3b3c", class: "bc-back-icon" }), X("span", { key: "f479d8088cc285570283e4c131d870798d66729b", class: "bc-back-text", "aria-hidden": "true" }, c.changeDate)))), X("div", { key: "af46accd00166e49a3813c4d4f5866b582a1041a", class: "bc-times-body", part: "times-body" }, X("div", { key: "db09bc2d19f2f31aced0eef056858957f4012e3b", class: "bc-times-scroll" }, X("shift-time-slots", { key: `slots-${this.selectedDate}`, part: "slots", times: A ? A.times.map(((L) => L.time)) : [], value: this.selectedRaw && A ? this.timeOf(this.selectedRaw) : "", label: q, hourCycle: this.hourCycle, language: this.language, appearance: this.appearance, colorScheme: this.colorScheme, size: this.size, disabled: m, onTimeChange: this.onTimeChange }))))), X("span", { key: "5591933f9594973bde77d13854b3789af4c44a1f", class: "bc-sr", "aria-live": "polite" }, j), X("span", { key: "6e4b3a35a3afde165d6f7b0edc19f9cce687acbb", class: "bc-sr", "aria-live": "polite" }, $ ? c.emptyTitle : ""), X("div", { key: "44ae6fb94787a85c16c4b063c3ff8c4cd6d1d5c6", class: "bc-collapse", "data-open": this.showStatus ? "" : void 0, "aria-hidden": this.showStatus ? void 0 : "true", inert: !this.showStatus || void 0 }, X("div", { key: "3b1c4192fa7c6b59a17994d2daec087ea77ea017", class: "bc-collapse-body" }, X("div", { key: "2abaa3c05d5d447c3476b88c43020172cd563880", class: "bc-status", part: "status", "aria-live": "polite" }, Q_.map(((L) => X("div", { key: L, class: "bc-message", "data-message": L, "data-active": L === T ? "" : void 0, "aria-hidden": L === T ? void 0 : "true", inert: L !== T || void 0 }, L === "loading" && X("span", { class: "bc-spinner", "aria-hidden": "true" }), X("span", { class: L === "invalid" ? "bc-message-text bc-error-text" : "bc-message-text", part: L === "invalid" ? "error" : L === "error" ? "status-text status-error" : "status-text", role: L === "invalid" && L === T ? "alert" : void 0 }, this.messageText(L, y)), L === "error" && X("button", { type: "button", class: "bc-retry", part: "retry", onClick: () => this.load() }, c.retry)))))))));
  }
  static get delegatesFocus() {
    return !0;
  }
  get el() {
    return this;
  }
  static get watchers() {
    return { calendarApi: [{ onTargetChange: 0 }], branchId: [{ onTargetChange: 0 }], departmentId: [{ onTargetChange: 0 }], brandId: [{ onTargetChange: 0 }], today: [{ onTargetChange: 0 }], disabledWeekdays: [{ onRulesChange: 0 }], disabledDates: [{ onRulesChange: 0 }], status: [{ onStatusChange: 0 }], language: [{ onLanguageChange: 0 }], valueFormat: [{ onFormatChange: 0 }], utcOffset: [{ onFormatChange: 0 }], hourCycle: [{ onFormatChange: 0 }], value: [{ onValueChange: 0 }], changeCallback: [{ onChangeCallbackChange: 0 }] };
  }
  static get style() {
    return '*,:after,:before{--tw-border-spacing-x:0;--tw-border-spacing-y:0;--tw-translate-x:0;--tw-translate-y:0;--tw-rotate:0;--tw-skew-x:0;--tw-skew-y:0;--tw-scale-x:1;--tw-scale-y:1;--tw-scroll-snap-strictness:proximity;--tw-ring-offset-width:0px;--tw-ring-offset-color:#fff;--tw-ring-color:rgba(59,130,246,.5);--tw-ring-offset-shadow:0 0 #0000;--tw-ring-shadow:0 0 #0000;--tw-shadow:0 0 #0000;--tw-shadow-colored:0 0 #0000;border:0 solid #e5e7eb;box-sizing:border-box}::backdrop{--tw-border-spacing-x:0;--tw-border-spacing-y:0;--tw-translate-x:0;--tw-translate-y:0;--tw-rotate:0;--tw-skew-x:0;--tw-skew-y:0;--tw-scale-x:1;--tw-scale-y:1;--tw-scroll-snap-strictness:proximity;--tw-ring-offset-width:0px;--tw-ring-offset-color:#fff;--tw-ring-color:rgba(59,130,246,.5);--tw-ring-offset-shadow:0 0 #0000;--tw-ring-shadow:0 0 #0000;--tw-shadow:0 0 #0000;--tw-shadow-colored:0 0 #0000;}.visible{visibility:visible}.collapse{visibility:collapse}.absolute{position:absolute}.relative{position:relative}.block{display:block}.inline{display:inline}.flex{display:flex}.table{display:table}.grid{display:grid}.hidden{display:none}.border-collapse{border-collapse:collapse}.transform{transform:translate(var(--tw-translate-x),var(--tw-translate-y)) rotate(var(--tw-rotate)) skewX(var(--tw-skew-x)) skewY(var(--tw-skew-y)) scaleX(var(--tw-scale-x)) scaleY(var(--tw-scale-y))}.resize{resize:both}.flex-wrap{flex-wrap:wrap}.border{border-width:1px}.uppercase{text-transform:uppercase}.underline{text-decoration-line:underline}.shadow{--tw-shadow:0 1px 3px 0 rgba(0,0,0,.1),0 1px 2px -1px rgba(0,0,0,.1);--tw-shadow-colored:0 1px 3px 0 var(--tw-shadow-color),0 1px 2px -1px var(--tw-shadow-color);box-shadow:var(--tw-ring-offset-shadow,0 0 #0000),var(--tw-ring-shadow,0 0 #0000),var(--tw-shadow)}.outline{outline-style:solid}.ring{--tw-ring-offset-shadow:var(--tw-ring-inset) 0 0 0 var(--tw-ring-offset-width) var(--tw-ring-offset-color);--tw-ring-shadow:var(--tw-ring-inset) 0 0 0 calc(3px + var(--tw-ring-offset-width)) var(--tw-ring-color);box-shadow:var(--tw-ring-offset-shadow),var(--tw-ring-shadow),var(--tw-shadow,0 0 #0000)}.transition{transition-duration:.15s;transition-property:color,background-color,border-color,text-decoration-color,fill,stroke,opacity,box-shadow,transform,filter,backdrop-filter;transition-timing-function:cubic-bezier(.4,0,.2,1)}.container{width:100%}@media (min-width:640px){.container{max-width:640px}}@media (min-width:768px){.container{max-width:768px}}@media (min-width:1024px){.container{max-width:1024px}}@media (min-width:1280px){.container{max-width:1280px}}@media (min-width:1536px){.container{max-width:1536px}}:host{-webkit-text-size-adjust:100%;-webkit-tap-highlight-color:transparent;font-feature-settings:normal;--_ink:var(--shift-booking-calendar-ink,var(--shift-ink,var(--_ap-ink,#1e293b)));--_heading-ink:var(--shift-booking-calendar-heading-ink,var(--shift-ink,var(--_ap-ink,#1e293b)));--_muted:var(--shift-booking-calendar-muted,var(--shift-ink-muted,var(--_ap-ink-muted,#64748b)));--_danger:var(--shift-booking-calendar-danger,var(--shift-danger,var(--_ap-danger,#dc2626)));--_line:var(--shift-booking-calendar-line,var(--shift-line,var(--_ap-line,#e2e8f0)));--_hover-bg:var(--shift-booking-calendar-hover-bg,var(--_ap-hover-bg,#f1f5f9));--_focus:var(--shift-booking-calendar-focus-ring,var(--shift-focus,var(--_ap-focus,#1e293b)));--_focus-width:var(--shift-booking-calendar-focus-ring-width,var(--_ap-focus-width,2px));--_focus-offset:var(--shift-booking-calendar-focus-ring-offset,var(--_ap-focus-offset,2px));--_halo:var(--shift-booking-calendar-focus-halo,var(--_ap-focus-halo,transparent));--_halo-width:var(--shift-booking-calendar-focus-halo-width,var(--_ap-focus-halo-width,0px));--_font-latin:var(--shift-booking-calendar-font-family,var(--shift-font-family,var(--_ap-font,"Nunito",system-ui,sans-serif)));--_font-arabic:var(--shift-booking-calendar-font-family-arabic,var(--shift-font-family-arabic,"Noto Kufi Arabic",var(--_font-latin)));--_font-size:var(--shift-booking-calendar-font-size,var(--shift-font-size,calc(var(--_ap-font-size, 14px) + var(--_sz-font, 0px))));--_small-size:var(--shift-booking-calendar-status-font-size,var(--shift-font-size-sm,calc(var(--_ap-font-size-sm, 12px) + var(--_sz-font, 0px))));--_label-size:var(--shift-booking-calendar-label-font-size,var(--shift-label-size,calc(var(--_ap-label-size, 12px) + var(--_sz-font, 0px))));--_label-transform:var(--shift-booking-calendar-label-transform,var(--shift-label-transform,var(--_ap-label-transform,uppercase)));--_label-tracking:var(--shift-booking-calendar-label-tracking,var(--shift-label-tracking,var(--_ap-label-tracking,0.04em)));--_label-weight:var(--shift-booking-calendar-label-font-weight,var(--shift-weight-strong,var(--_ap-label-weight,600)));--_status-weight:var(--shift-booking-calendar-status-font-weight,var(--shift-weight,var(--_ap-weight,400)));--_back-weight:var(--shift-booking-calendar-back-font-weight,var(--shift-weight-strong,var(--_ap-weight-strong,600)));--_line-height:var(--shift-booking-calendar-line-height,1.4);--_radius:var(--shift-booking-calendar-button-radius,var(--shift-radius-md,var(--_ap-radius-md,8px)));--_border-width:var(--shift-booking-calendar-border-width,var(--shift-border-width,var(--_ap-border-width,1px)));--_max-width:var(--shift-booking-calendar-max-inline-size,var(--_sz-width,380px));--_min-width:var(--shift-booking-calendar-min-inline-size,var(--_sz-min-width,300px));--_gap:var(--shift-booking-calendar-gap,calc(12px*var(--_sz-space, 1)));--_status-height:var(--shift-booking-calendar-status-min-block-size,calc(32px*var(--_sz-control, 1)));--_spinner:var(--shift-booking-calendar-spinner-size,var(--_sz-icon,16px));--_icon:var(--shift-booking-calendar-back-icon-size,var(--_sz-icon,16px));--_back-gap:var(--shift-booking-calendar-back-gap,calc(6px*var(--_sz-space, 1)));--_back-padding:var(--shift-booking-calendar-back-padding-inline,10px);--_empty-veil:var(--shift-booking-calendar-empty-veil,color-mix(in srgb,var(--shift-surface,var(--_ap-panel-surface,#fff)) 72%,transparent));--_empty-art:var(--shift-booking-calendar-empty-art-size,calc(56px*var(--_sz-control, 1)));--_settle:var(--shift-booking-calendar-settle,var(--shift-settle,480ms));--_ease:var(--shift-booking-calendar-ease,var(--shift-ease,ease));color:var(--_ink);display:block;font-family:ui-sans-serif,system-ui,sans-serif,Apple Color Emoji,Segoe UI Emoji,Segoe UI Symbol,Noto Color Emoji;font-family:var(--_font-latin);font-size:var(--_font-size);font-variation-settings:normal;line-height:1.5;line-height:var(--_line-height);-moz-tab-size:4;-o-tab-size:4;tab-size:4}hr{border-top-width:1px;color:inherit;height:0}abbr:where([title]){-webkit-text-decoration:underline dotted;text-decoration:underline dotted}h1,h2,h3,h4,h5,h6{font-size:inherit;font-weight:inherit}a{color:inherit;text-decoration:inherit}b,strong{font-weight:bolder}code,kbd,pre,samp{font-feature-settings:normal;font-family:ui-monospace,SFMono-Regular,Menlo,Monaco,Consolas,Liberation Mono,Courier New,monospace;font-size:1em;font-variation-settings:normal}small{font-size:80%}sub,sup{font-size:75%;line-height:0;position:relative;vertical-align:baseline}sub{bottom:-.25em}sup{top:-.5em}table{border-collapse:collapse;border-color:inherit;text-indent:0}button,input,optgroup,select,textarea{font-feature-settings:inherit;color:inherit;font-family:inherit;font-size:100%;font-variation-settings:inherit;font-weight:inherit;letter-spacing:inherit;line-height:inherit;margin:0;padding:0}button,select{text-transform:none}button,input:where([type=button]),input:where([type=reset]),input:where([type=submit]){-webkit-appearance:button;background-color:transparent;background-image:none}:-moz-focusring{outline:auto}:-moz-ui-invalid{box-shadow:none}progress{vertical-align:baseline}::-webkit-inner-spin-button,::-webkit-outer-spin-button{height:auto}[type=search]{-webkit-appearance:textfield;outline-offset:-2px}::-webkit-search-decoration{-webkit-appearance:none}::-webkit-file-upload-button{-webkit-appearance:button;font:inherit}summary{display:list-item}blockquote,dd,dl,fieldset,figure,h1,h2,h3,h4,h5,h6,hr,p,pre{margin:0}fieldset,legend{padding:0}menu,ol,ul{list-style:none;margin:0;padding:0}dialog{padding:0}textarea{resize:vertical}input::-moz-placeholder,textarea::-moz-placeholder{color:#9ca3af;opacity:1}input::placeholder,textarea::placeholder{color:#9ca3af;opacity:1}[role=button],button{cursor:pointer}:disabled{cursor:default}audio,canvas,embed,iframe,img,object,svg,video{display:block;vertical-align:middle}img,video{height:auto;max-width:100%}[hidden]:where(:not([hidden=until-found])){display:none}:host([appearance=vanilla]){--_ap-font:"Nunito",system-ui,sans-serif;--_ap-font-size:14px;--_ap-font-size-sm:12px;--_ap-label-size:12px;--_ap-label-transform:uppercase;--_ap-label-tracking:0.04em;--_ap-label-weight:600;--_ap-weight:400;--_ap-weight-strong:600;--_ap-radius-sm:8px;--_ap-radius-md:8px;--_ap-radius-lg:12px;--_ap-border-width:1px;--_ap-panel-border-width:1px;--_ap-control-height:44px;--_ap-cell-size:40px;--_ap-cell-round:0;--_ap-gap:4px;--_ap-focus-width:2px;--_ap-focus-offset:2px;--_ap-focus-halo-width:0px;--_ap-today-ring-width:1px;--_ap-today-stroke:0px;--_ap-today-decoration:none;--_ap-blocked-decoration:line-through;--_ap-blocked-opacity:1;--_ap-surface:#fff;--_ap-surface-muted:#f8fafc;--_ap-panel-surface:#fff;--_ap-ink:#1e293b;--_ap-ink-muted:#64748b;--_ap-ink-disabled:#cbd5e1;--_ap-line:#e2e8f0;--_ap-line-strong:#94a3b8;--_ap-panel-line:#e2e8f0;--_ap-cell-line:transparent;--_ap-accent:#1e293b;--_ap-on-accent:#fff;--_ap-accent-tint:#f1f5f9;--_ap-focus:#1e293b;--_ap-focus-halo:transparent;--_ap-focus-inner:#fff;--_ap-danger:#dc2626;--_ap-shadow-panel:0 10px 30px -12px rgba(15,23,42,.25);--_ap-shadow-control:none;--_ap-hover-bg:#f1f5f9;--_ap-hover-line:#94a3b8;--_ap-hover-ink:#1e293b;--_ap-today-ring:#64748b;--_ap-today-dot:transparent}:host([appearance=material]){--_ap-font:"Nunito",system-ui,sans-serif;--_ap-font-size:14px;--_ap-font-size-sm:12px;--_ap-label-size:12px;--_ap-label-transform:none;--_ap-label-tracking:0em;--_ap-label-weight:500;--_ap-weight:400;--_ap-weight-strong:600;--_ap-radius-sm:999px;--_ap-radius-md:4px;--_ap-radius-lg:4px;--_ap-border-width:0px;--_ap-panel-border-width:1px;--_ap-control-height:48px;--_ap-cell-size:40px;--_ap-cell-round:1;--_ap-gap:2px;--_ap-focus-width:2px;--_ap-focus-offset:2px;--_ap-focus-halo-width:0px;--_ap-today-ring-width:1px;--_ap-today-stroke:0px;--_ap-today-decoration:none;--_ap-blocked-decoration:none;--_ap-blocked-opacity:1;--_ap-surface:#fff;--_ap-surface-muted:#f5f5f5;--_ap-panel-surface:#fff;--_ap-ink:rgba(0,0,0,.87);--_ap-ink-muted:rgba(0,0,0,.54);--_ap-ink-disabled:rgba(0,0,0,.38);--_ap-line:rgba(0,0,0,.12);--_ap-line-strong:rgba(0,0,0,.42);--_ap-panel-line:rgba(0,0,0,.12);--_ap-cell-line:transparent;--_ap-accent:#1976d2;--_ap-on-accent:#fff;--_ap-accent-tint:rgba(25,118,210,.08);--_ap-focus:#1976d2;--_ap-focus-halo:transparent;--_ap-focus-inner:#fff;--_ap-danger:#d32f2f;--_ap-shadow-panel:0 5px 5px -3px rgba(0,0,0,.2),0 8px 10px 1px rgba(0,0,0,.14),0 3px 14px 2px rgba(0,0,0,.12);--_ap-shadow-control:0 2px 1px -1px rgba(0,0,0,.2),0 1px 1px 0 rgba(0,0,0,.14),0 1px 3px 0 rgba(0,0,0,.12);--_ap-hover-bg:rgba(0,0,0,.04);--_ap-hover-line:rgba(0,0,0,.12);--_ap-hover-ink:rgba(0,0,0,.87);--_ap-today-ring:#1976d2;--_ap-today-dot:transparent}:host([appearance=soft]){--_ap-font:"Nunito",system-ui,sans-serif;--_ap-font-size:15px;--_ap-font-size-sm:13px;--_ap-label-size:13px;--_ap-label-transform:none;--_ap-label-tracking:0em;--_ap-label-weight:600;--_ap-weight:400;--_ap-weight-strong:600;--_ap-radius-sm:12px;--_ap-radius-md:14px;--_ap-radius-lg:20px;--_ap-border-width:0px;--_ap-panel-border-width:0px;--_ap-control-height:48px;--_ap-cell-size:44px;--_ap-cell-round:0;--_ap-gap:6px;--_ap-focus-width:1px;--_ap-focus-offset:0px;--_ap-focus-halo-width:4px;--_ap-today-ring-width:0px;--_ap-today-stroke:0px;--_ap-today-decoration:none;--_ap-blocked-decoration:none;--_ap-blocked-opacity:0.5;--_ap-surface:#f1f5f9;--_ap-surface-muted:#f8fafc;--_ap-panel-surface:#f1f5f9;--_ap-ink:#1e293b;--_ap-ink-muted:#475569;--_ap-ink-disabled:#cbd5e1;--_ap-line:transparent;--_ap-line-strong:#cbd5e1;--_ap-panel-line:transparent;--_ap-cell-line:transparent;--_ap-accent:#1e293b;--_ap-on-accent:#fff;--_ap-accent-tint:#e2e8f0;--_ap-focus:#1e293b;--_ap-focus-halo:#e2e8f0;--_ap-focus-inner:#e2e8f0;--_ap-danger:#dc2626;--_ap-shadow-panel:0 16px 40px -16px rgba(15,23,42,.2);--_ap-shadow-control:0 1px 2px 0 rgba(15,23,42,.06),0 8px 24px -12px rgba(15,23,42,.18);--_ap-hover-bg:#e2e8f0;--_ap-hover-line:transparent;--_ap-hover-ink:#1e293b;--_ap-today-ring:transparent;--_ap-today-dot:#1e293b}:host([appearance=sharp]){--_ap-font:"Manrope",system-ui,sans-serif;--_ap-font-size:13px;--_ap-font-size-sm:12px;--_ap-label-size:11px;--_ap-label-transform:uppercase;--_ap-label-tracking:0.06em;--_ap-label-weight:700;--_ap-weight:400;--_ap-weight-strong:600;--_ap-radius-sm:2px;--_ap-radius-md:2px;--_ap-radius-lg:4px;--_ap-border-width:1px;--_ap-panel-border-width:1px;--_ap-control-height:36px;--_ap-cell-size:34px;--_ap-cell-round:0;--_ap-gap:2px;--_ap-focus-width:2px;--_ap-focus-offset:0px;--_ap-focus-halo-width:0px;--_ap-today-ring-width:0px;--_ap-today-stroke:0.6px;--_ap-today-decoration:underline;--_ap-blocked-decoration:line-through;--_ap-blocked-opacity:1;--_ap-surface:#fff;--_ap-surface-muted:#f1f5f9;--_ap-panel-surface:#fff;--_ap-ink:#0f172a;--_ap-ink-muted:#475569;--_ap-ink-disabled:#94a3b8;--_ap-line:#94a3b8;--_ap-line-strong:#0f172a;--_ap-panel-line:#0f172a;--_ap-cell-line:#94a3b8;--_ap-accent:#0f172a;--_ap-on-accent:#fff;--_ap-accent-tint:#f1f5f9;--_ap-focus:#0f172a;--_ap-focus-halo:transparent;--_ap-focus-inner:#fff;--_ap-danger:#b91c1c;--_ap-shadow-panel:none;--_ap-shadow-control:none;--_ap-hover-bg:#f1f5f9;--_ap-hover-line:#94a3b8;--_ap-hover-ink:#0f172a;--_ap-today-ring:transparent;--_ap-today-dot:transparent}:host([appearance=minimal]){--_ap-font:"Manrope",system-ui,sans-serif;--_ap-font-size:14px;--_ap-font-size-sm:12px;--_ap-label-size:12px;--_ap-label-transform:none;--_ap-label-tracking:0em;--_ap-label-weight:400;--_ap-weight:400;--_ap-weight-strong:600;--_ap-radius-sm:999px;--_ap-radius-md:6px;--_ap-radius-lg:8px;--_ap-border-width:0px;--_ap-panel-border-width:1px;--_ap-control-height:44px;--_ap-cell-size:40px;--_ap-cell-round:1;--_ap-gap:4px;--_ap-focus-width:2px;--_ap-focus-offset:2px;--_ap-focus-halo-width:0px;--_ap-today-ring-width:0px;--_ap-today-stroke:0px;--_ap-today-decoration:none;--_ap-blocked-decoration:none;--_ap-blocked-opacity:1;--_ap-surface:transparent;--_ap-surface-muted:transparent;--_ap-panel-surface:transparent;--_ap-ink:#0f172a;--_ap-ink-muted:#64748b;--_ap-ink-disabled:#cbd5e1;--_ap-line:transparent;--_ap-line-strong:#cbd5e1;--_ap-panel-line:transparent;--_ap-cell-line:transparent;--_ap-accent:#0f172a;--_ap-on-accent:#fff;--_ap-accent-tint:#f1f5f9;--_ap-focus:#0f172a;--_ap-focus-halo:transparent;--_ap-focus-inner:#fff;--_ap-danger:#dc2626;--_ap-shadow-panel:none;--_ap-shadow-control:none;--_ap-hover-bg:transparent;--_ap-hover-line:transparent;--_ap-hover-ink:#0f172a;--_ap-today-ring:transparent;--_ap-today-dot:#0f172a}:host([color-scheme=light]){--_ap-tone-neutral-bg:#e8ecef;--_ap-tone-neutral-ink:#46535e;--_ap-tone-info-bg:#e8f1f8;--_ap-tone-info-ink:#275e8f;--_ap-tone-success-bg:#e8f4ed;--_ap-tone-success-ink:#216b44;--_ap-tone-warning-bg:#fff7e6;--_ap-tone-warning-ink:#8a5a12;--_ap-tone-danger-bg:#fff1f0;--_ap-tone-danger-ink:#9b2c2c;color-scheme:light}:host(:not([appearance])[color-scheme=dark]),:host([appearance=vanilla][color-scheme=dark]){--_ap-surface:#0f172a;--_ap-surface-muted:#1e293b;--_ap-panel-surface:#0f172a;--_ap-ink:#f1f5f9;--_ap-ink-muted:#94a3b8;--_ap-ink-disabled:#475569;--_ap-line:#334155;--_ap-line-strong:#64748b;--_ap-panel-line:#334155;--_ap-cell-line:transparent;--_ap-accent:#f1f5f9;--_ap-on-accent:#0f172a;--_ap-accent-tint:#1e293b;--_ap-focus:#f1f5f9;--_ap-focus-halo:transparent;--_ap-focus-inner:#0f172a;--_ap-danger:#f87171;--_ap-shadow-panel:0 10px 30px -12px rgba(0,0,0,.6);--_ap-shadow-control:none;--_ap-hover-bg:#1e293b;--_ap-hover-line:#64748b;--_ap-hover-ink:#f1f5f9;--_ap-today-ring:#64748b;--_ap-today-dot:transparent}:host([appearance=material][color-scheme=dark]){--_ap-surface:#1e1e1e;--_ap-surface-muted:#2c2c2c;--_ap-panel-surface:#2c2c2c;--_ap-ink:hsla(0,0%,100%,.87);--_ap-ink-muted:hsla(0,0%,100%,.6);--_ap-ink-disabled:hsla(0,0%,100%,.38);--_ap-line:hsla(0,0%,100%,.12);--_ap-line-strong:hsla(0,0%,100%,.42);--_ap-panel-line:hsla(0,0%,100%,.12);--_ap-cell-line:transparent;--_ap-accent:#90caf9;--_ap-on-accent:#0d1b2a;--_ap-accent-tint:rgba(144,202,249,.12);--_ap-focus:#90caf9;--_ap-focus-halo:transparent;--_ap-focus-inner:#0d1b2a;--_ap-danger:#f48fb1;--_ap-shadow-panel:0 5px 5px -3px rgba(0,0,0,.2),0 8px 10px 1px rgba(0,0,0,.14),0 3px 14px 2px rgba(0,0,0,.12);--_ap-shadow-control:0 2px 1px -1px rgba(0,0,0,.4),0 1px 1px 0 rgba(0,0,0,.28),0 1px 3px 0 rgba(0,0,0,.24);--_ap-hover-bg:hsla(0,0%,100%,.08);--_ap-hover-line:hsla(0,0%,100%,.12);--_ap-hover-ink:hsla(0,0%,100%,.87);--_ap-today-ring:#90caf9;--_ap-today-dot:transparent}:host([appearance=soft][color-scheme=dark]){--_ap-surface:#1e293b;--_ap-surface-muted:#0f172a;--_ap-panel-surface:#1e293b;--_ap-ink:#f1f5f9;--_ap-ink-muted:#94a3b8;--_ap-ink-disabled:#475569;--_ap-line:transparent;--_ap-line-strong:#475569;--_ap-panel-line:transparent;--_ap-cell-line:transparent;--_ap-accent:#f1f5f9;--_ap-on-accent:#0f172a;--_ap-accent-tint:#334155;--_ap-focus:#f1f5f9;--_ap-focus-halo:#334155;--_ap-focus-inner:#334155;--_ap-danger:#f87171;--_ap-shadow-panel:0 16px 40px -16px rgba(0,0,0,.55);--_ap-shadow-control:0 1px 2px 0 rgba(0,0,0,.4),0 8px 24px -12px rgba(0,0,0,.5);--_ap-hover-bg:#334155;--_ap-hover-line:transparent;--_ap-hover-ink:#f1f5f9;--_ap-today-ring:transparent;--_ap-today-dot:#f1f5f9}:host([appearance=sharp][color-scheme=dark]){--_ap-surface:#0b0f17;--_ap-surface-muted:#0f172a;--_ap-panel-surface:#0b0f17;--_ap-ink:#f8fafc;--_ap-ink-muted:#94a3b8;--_ap-ink-disabled:#475569;--_ap-line:#475569;--_ap-line-strong:#e2e8f0;--_ap-panel-line:#e2e8f0;--_ap-cell-line:#475569;--_ap-accent:#f8fafc;--_ap-on-accent:#020617;--_ap-accent-tint:#1e293b;--_ap-focus:#f8fafc;--_ap-focus-halo:transparent;--_ap-focus-inner:#020617;--_ap-danger:#f87171;--_ap-shadow-panel:none;--_ap-shadow-control:none;--_ap-hover-bg:#1e293b;--_ap-hover-line:#475569;--_ap-hover-ink:#f8fafc;--_ap-today-ring:transparent;--_ap-today-dot:transparent}:host([appearance=minimal][color-scheme=dark]){--_ap-surface:transparent;--_ap-surface-muted:transparent;--_ap-panel-surface:transparent;--_ap-ink:#f1f5f9;--_ap-ink-muted:#94a3b8;--_ap-ink-disabled:#475569;--_ap-line:transparent;--_ap-line-strong:#475569;--_ap-panel-line:#334155;--_ap-cell-line:transparent;--_ap-accent:#f1f5f9;--_ap-on-accent:#0f172a;--_ap-accent-tint:#1e293b;--_ap-focus:#f1f5f9;--_ap-focus-halo:transparent;--_ap-focus-inner:#0f172a;--_ap-danger:#f87171;--_ap-shadow-panel:none;--_ap-shadow-control:none;--_ap-hover-bg:transparent;--_ap-hover-line:transparent;--_ap-hover-ink:#f1f5f9;--_ap-today-ring:transparent;--_ap-today-dot:#f1f5f9}:host([color-scheme=dark]){--_ap-tone-neutral-bg:#334155;--_ap-tone-neutral-ink:#e2e8f0;--_ap-tone-info-bg:#1e3a5f;--_ap-tone-info-ink:#bfdbfe;--_ap-tone-success-bg:#14532d;--_ap-tone-success-ink:#bbf7d0;--_ap-tone-warning-bg:#4a3212;--_ap-tone-warning-ink:#fde68a;--_ap-tone-danger-bg:#4c1d1d;--_ap-tone-danger-ink:#fecaca;color-scheme:dark}@media (prefers-color-scheme:dark){:host(:not([appearance])[color-scheme=auto]),:host([appearance=vanilla][color-scheme=auto]){--_ap-surface:#0f172a;--_ap-surface-muted:#1e293b;--_ap-panel-surface:#0f172a;--_ap-ink:#f1f5f9;--_ap-ink-muted:#94a3b8;--_ap-ink-disabled:#475569;--_ap-line:#334155;--_ap-line-strong:#64748b;--_ap-panel-line:#334155;--_ap-cell-line:transparent;--_ap-accent:#f1f5f9;--_ap-on-accent:#0f172a;--_ap-accent-tint:#1e293b;--_ap-focus:#f1f5f9;--_ap-focus-halo:transparent;--_ap-focus-inner:#0f172a;--_ap-danger:#f87171;--_ap-shadow-panel:0 10px 30px -12px rgba(0,0,0,.6);--_ap-shadow-control:none;--_ap-hover-bg:#1e293b;--_ap-hover-line:#64748b;--_ap-hover-ink:#f1f5f9;--_ap-today-ring:#64748b;--_ap-today-dot:transparent}:host([appearance=material][color-scheme=auto]){--_ap-surface:#1e1e1e;--_ap-surface-muted:#2c2c2c;--_ap-panel-surface:#2c2c2c;--_ap-ink:hsla(0,0%,100%,.87);--_ap-ink-muted:hsla(0,0%,100%,.6);--_ap-ink-disabled:hsla(0,0%,100%,.38);--_ap-line:hsla(0,0%,100%,.12);--_ap-line-strong:hsla(0,0%,100%,.42);--_ap-panel-line:hsla(0,0%,100%,.12);--_ap-cell-line:transparent;--_ap-accent:#90caf9;--_ap-on-accent:#0d1b2a;--_ap-accent-tint:rgba(144,202,249,.12);--_ap-focus:#90caf9;--_ap-focus-halo:transparent;--_ap-focus-inner:#0d1b2a;--_ap-danger:#f48fb1;--_ap-shadow-panel:0 5px 5px -3px rgba(0,0,0,.2),0 8px 10px 1px rgba(0,0,0,.14),0 3px 14px 2px rgba(0,0,0,.12);--_ap-shadow-control:0 2px 1px -1px rgba(0,0,0,.4),0 1px 1px 0 rgba(0,0,0,.28),0 1px 3px 0 rgba(0,0,0,.24);--_ap-hover-bg:hsla(0,0%,100%,.08);--_ap-hover-line:hsla(0,0%,100%,.12);--_ap-hover-ink:hsla(0,0%,100%,.87);--_ap-today-ring:#90caf9;--_ap-today-dot:transparent}:host([appearance=soft][color-scheme=auto]){--_ap-surface:#1e293b;--_ap-surface-muted:#0f172a;--_ap-panel-surface:#1e293b;--_ap-ink:#f1f5f9;--_ap-ink-muted:#94a3b8;--_ap-ink-disabled:#475569;--_ap-line:transparent;--_ap-line-strong:#475569;--_ap-panel-line:transparent;--_ap-cell-line:transparent;--_ap-accent:#f1f5f9;--_ap-on-accent:#0f172a;--_ap-accent-tint:#334155;--_ap-focus:#f1f5f9;--_ap-focus-halo:#334155;--_ap-focus-inner:#334155;--_ap-danger:#f87171;--_ap-shadow-panel:0 16px 40px -16px rgba(0,0,0,.55);--_ap-shadow-control:0 1px 2px 0 rgba(0,0,0,.4),0 8px 24px -12px rgba(0,0,0,.5);--_ap-hover-bg:#334155;--_ap-hover-line:transparent;--_ap-hover-ink:#f1f5f9;--_ap-today-ring:transparent;--_ap-today-dot:#f1f5f9}:host([appearance=sharp][color-scheme=auto]){--_ap-surface:#0b0f17;--_ap-surface-muted:#0f172a;--_ap-panel-surface:#0b0f17;--_ap-ink:#f8fafc;--_ap-ink-muted:#94a3b8;--_ap-ink-disabled:#475569;--_ap-line:#475569;--_ap-line-strong:#e2e8f0;--_ap-panel-line:#e2e8f0;--_ap-cell-line:#475569;--_ap-accent:#f8fafc;--_ap-on-accent:#020617;--_ap-accent-tint:#1e293b;--_ap-focus:#f8fafc;--_ap-focus-halo:transparent;--_ap-focus-inner:#020617;--_ap-danger:#f87171;--_ap-shadow-panel:none;--_ap-shadow-control:none;--_ap-hover-bg:#1e293b;--_ap-hover-line:#475569;--_ap-hover-ink:#f8fafc;--_ap-today-ring:transparent;--_ap-today-dot:transparent}:host([appearance=minimal][color-scheme=auto]){--_ap-surface:transparent;--_ap-surface-muted:transparent;--_ap-panel-surface:transparent;--_ap-ink:#f1f5f9;--_ap-ink-muted:#94a3b8;--_ap-ink-disabled:#475569;--_ap-line:transparent;--_ap-line-strong:#475569;--_ap-panel-line:#334155;--_ap-cell-line:transparent;--_ap-accent:#f1f5f9;--_ap-on-accent:#0f172a;--_ap-accent-tint:#1e293b;--_ap-focus:#f1f5f9;--_ap-focus-halo:transparent;--_ap-focus-inner:#0f172a;--_ap-danger:#f87171;--_ap-shadow-panel:none;--_ap-shadow-control:none;--_ap-hover-bg:transparent;--_ap-hover-line:transparent;--_ap-hover-ink:#f1f5f9;--_ap-today-ring:transparent;--_ap-today-dot:#f1f5f9}:host([color-scheme=auto]){--_ap-tone-neutral-bg:#334155;--_ap-tone-neutral-ink:#e2e8f0;--_ap-tone-info-bg:#1e3a5f;--_ap-tone-info-ink:#bfdbfe;--_ap-tone-success-bg:#14532d;--_ap-tone-success-ink:#bbf7d0;--_ap-tone-warning-bg:#4a3212;--_ap-tone-warning-ink:#fde68a;--_ap-tone-danger-bg:#4c1d1d;--_ap-tone-danger-ink:#fecaca;color-scheme:dark}}:host([size=xs]){--_sz-control:0.75;--_sz-font:-2px;--_sz-space:0.5;--_sz-nav:24px;--_sz-icon:12px;--_sz-width:300px;--_sz-min-width:260px;--_sz-min-floor:0px}:host([size=sm]){--_sz-control:0.85;--_sz-font:-1px;--_sz-space:0.75;--_sz-nav:28px;--_sz-icon:14px;--_sz-width:340px;--_sz-min-width:280px;--_sz-min-floor:0px}:host([size=md]){--_sz-control:1;--_sz-font:0px;--_sz-space:1;--_sz-nav:32px;--_sz-icon:16px;--_sz-width:380px;--_sz-min-width:300px;--_sz-min-floor:0px}:host([size=lg]){--_sz-control:1.15;--_sz-font:2px;--_sz-space:1.25;--_sz-nav:36px;--_sz-icon:18px;--_sz-width:440px;--_sz-min-width:340px;--_sz-min-floor:340px}:host([size=xl]){--_sz-control:1.3;--_sz-font:4px;--_sz-space:1.5;--_sz-nav:40px;--_sz-icon:20px;--_sz-width:500px;--_sz-min-width:380px;--_sz-min-floor:380px}@media (pointer:coarse){:host([appearance=sharp]){--_ap-cell-size:44px}}.bc-root{--_bc-dir:1;--_bc-settle:var(--_settle);--_bc-ease:var(--_ease);--_ring:max(calc(var(--_focus-width) + var(--_focus-offset)),var(--_halo-width),2px);color:var(--_ink);display:flex;flex-direction:column;font-family:var(--_font-latin);inline-size:100%;letter-spacing:normal;max-inline-size:var(--_max-width);text-align:start;text-transform:none}.bc-root[dir=rtl]{--_bc-dir:-1}.bc-root:lang(ar),.bc-root:lang(ckb){font-family:var(--_font-arabic)}.bc-collapse{display:grid;grid-template-rows:0fr;opacity:0;transition:grid-template-rows var(--_settle) var(--_ease),opacity calc(var(--_settle) - 60ms) var(--_ease),visibility 0s linear var(--_settle);visibility:hidden}.bc-collapse[data-open]{grid-template-rows:1fr;opacity:1;transition:grid-template-rows var(--_settle) var(--_ease),opacity var(--_settle) var(--_ease),visibility 0s;visibility:visible}.bc-collapse-body{min-block-size:0;overflow:hidden}.bc-label{color:var(--_ink);font-size:var(--_label-size);font-weight:var(--_label-weight);letter-spacing:var(--_label-tracking);margin-block-end:calc(var(--_gap)*.66);text-transform:var(--_label-transform)}.bc-root:lang(ar) .bc-label,.bc-root:lang(ckb) .bc-label{letter-spacing:normal}.bc-required{color:var(--_danger);margin-inline-start:2px;transition:opacity var(--_settle) var(--_ease)}.bc-required[data-hidden]{opacity:0}.bc-box{display:grid;min-inline-size:min(var(--_min-width),max(100%,var(--_sz-min-floor,0px)));position:relative}.bc-box shift-calendar::part(root){max-inline-size:none;min-inline-size:0;overflow:hidden;overflow:clip}.bc-box shift-calendar::part(heading),.bc-box shift-calendar::part(nav){transition:transform var(--_bc-settle) var(--_bc-ease),opacity var(--_bc-settle) var(--_bc-ease)}.bc-box shift-calendar::part(grid){transition:transform var(--_bc-settle) var(--_bc-ease)}.bc-box[data-view=times] shift-calendar::part(heading),.bc-box[data-view=times] shift-calendar::part(nav){opacity:.9;transform:translateY(-100%)}.bc-box[data-view=times] shift-calendar::part(grid){transform:translateX(calc((100% + var(--_f-travel, 12px))*var(--_bc-dir)*-1))}.bc-box shift-calendar::part(header){clip-path:inset(-50vh -50vw);transition:clip-path 0s linear var(--_bc-settle)}.bc-box[data-view=times] shift-calendar::part(header){clip-path:inset(0 -50vw);transition:clip-path 0s}.bc-empty{align-items:center;background:var(--_empty-veil);border-end-end-radius:var(--_f-radius,12px);border-end-start-radius:var(--_f-radius,12px);color:var(--_muted);display:flex;flex-direction:column;gap:calc(8px*var(--_sz-space, 1));inset:var(--_f-top,52px) 0 0;justify-content:center;opacity:0;padding:var(--_gap);pointer-events:none;position:absolute;text-align:center;transition:opacity var(--_settle) var(--_ease),visibility 0s linear var(--_settle);visibility:hidden}.bc-empty[data-shown]{opacity:1;transition:opacity var(--_settle) var(--_ease),visibility 0s;visibility:visible}.bc-empty-art{block-size:var(--_empty-art);inline-size:var(--_empty-art)}.bc-empty-text{color:var(--_ink);font-size:var(--_font-size);font-weight:var(--_back-weight)}.bc-times{border-color:transparent;border-radius:var(--_f-radius,12px);border-style:solid;border-width:var(--_f-border,1px);container:bc-times/inline-size;gap:var(--_f-gap,8px);inset:0;overflow:hidden;overflow:clip;padding:var(--_f-padding,12px);pointer-events:none;pointer-events:auto;transition:visibility 0s linear var(--_settle);transition:visibility 0s;visibility:hidden;visibility:visible}.bc-box[data-view=times] .bc-empty,.bc-times{display:flex;flex-direction:column;position:absolute}.bc-box[data-view=times] .bc-empty{align-items:center;background:var(--_empty-veil);border-end-end-radius:var(--_f-radius,12px);border-end-start-radius:var(--_f-radius,12px);color:var(--_muted);gap:calc(8px*var(--_sz-space, 1));inset:var(--_f-top,52px) 0 0;justify-content:center;opacity:0;padding:var(--_gap);pointer-events:none;text-align:center;transition:opacity var(--_settle) var(--_ease),visibility 0s linear var(--_settle);visibility:hidden}.bc-times-header{align-items:center;block-size:var(--_f-head,32px);display:flex;flex:none;gap:var(--_f-head-gap,8px)}.bc-title-band{display:grid;flex:1 1 auto;margin-inline-start:var(--_f-band,-2px);overflow:hidden;overflow:clip}.bc-title,.bc-title-band{block-size:100%;min-inline-size:0}.bc-title{align-items:flex-start;color:var(--_heading-ink);display:flex;flex-direction:column;font-family:var(--_f-title-font,inherit);font-size:var(--_f-title-size,15px);font-variant-numeric:tabular-nums;font-weight:var(--_f-title-weight,700);grid-area:1/1;justify-content:center;line-height:var(--_f-title-line,1.2);opacity:.9;padding-inline:var(--_f-title-padding,2px);transform:translateY(100%);transition:transform var(--_settle) var(--_ease),opacity var(--_settle) var(--_ease);white-space:nowrap}.bc-title-line{align-items:center;display:flex;gap:.35em;max-inline-size:100%}.bc-title-day,.bc-title-line{min-inline-size:0}.bc-title-day,.bc-title-summary{overflow:hidden;text-overflow:ellipsis}.bc-title-summary{color:var(--_muted);font-size:calc(var(--_small-size) - 1px);font-weight:var(--_status-weight);line-height:1.2;max-inline-size:100%}:host([size=sm]) .bc-title-summary,:host([size=xs]) .bc-title-summary{display:none}.bc-slot{block-size:100%;display:grid;flex:none;overflow:hidden;overflow:clip}.bc-slot-layer{align-items:center;block-size:100%;display:flex;grid-area:1/1}.bc-slot-layer[data-phase=enter]{animation:bc-roll-enter var(--_settle) var(--_ease) both}.bc-slot-layer[data-phase=leave]{animation:bc-roll-leave var(--_settle) var(--_ease) both}.bc-back-band{block-size:100%;clip-path:inset(0 -50vw);display:flex;flex:none;transition:clip-path 0s}.bc-box[data-view=times] .bc-back-band{clip-path:inset(-50vh -50vw);transition:clip-path 0s linear var(--_settle)}.bc-back{align-items:center;block-size:100%;border:var(--_border-width) solid var(--_line);border-radius:var(--_radius);color:var(--_ink);display:inline-flex;gap:var(--_back-gap);justify-content:center;min-inline-size:var(--_f-head,32px);opacity:.9;padding-inline:0;transform:translateY(100%);transition:transform var(--_settle) var(--_ease),opacity var(--_settle) var(--_ease),background-color calc(var(--_settle)/2) var(--_ease)}.bc-back:hover{background-color:var(--_hover-bg)}.bc-back:focus-visible{box-shadow:0 0 0 var(--_halo-width) var(--_halo);outline:var(--_focus-width) solid var(--_focus);outline-offset:var(--_focus-offset)}.bc-box[data-view=times] .bc-back,.bc-box[data-view=times] .bc-title{opacity:1;transform:none}.bc-back-icon{block-size:var(--_icon);inline-size:var(--_icon)}.bc-root[dir=rtl] .bc-back-icon{transform:scaleX(-1)}.bc-back-text{display:none;font-size:var(--_small-size);font-weight:var(--_back-weight);white-space:nowrap}@container bc-times (min-width: 300px){.bc-back{padding-inline:var(--_back-padding)}.bc-back-text{display:inline}}.bc-times-body{display:flex;flex:1 1 auto;flex-direction:column;min-block-size:0;transform:translateX(calc((100% + var(--_f-travel, 12px))*var(--_bc-dir)));transition:transform var(--_settle) var(--_ease)}.bc-box[data-view=times] .bc-times-body{transform:none}.bc-times-scroll{flex:1 1 auto;margin:calc(var(--_ring)*-1);min-block-size:0;overflow-x:hidden;overflow-y:auto;overscroll-behavior:contain;padding:var(--_ring);padding-block-end:calc(var(--_ring) + var(--shift-booking-calendar-scroll-end-room, 6px));scrollbar-width:thin}.bc-status{align-items:center;display:grid;margin-block-start:calc(var(--_gap)*.66);min-block-size:var(--_status-height)}.bc-message{align-items:center;color:var(--_muted);display:flex;flex-wrap:wrap;font-size:var(--_small-size);font-weight:var(--_status-weight);gap:calc(var(--_gap)*.66);grid-area:1/1;opacity:0;transition:opacity calc(var(--_settle) - 60ms) var(--_ease),visibility 0s linear var(--_settle);visibility:hidden}.bc-message[data-active]{opacity:1;transition:opacity var(--_settle) var(--_ease),visibility 0s;visibility:visible}.bc-message[data-message=selected]{color:var(--_ink)}.bc-message[data-message=error],.bc-message[data-message=invalid]{color:var(--_danger)}.bc-spinner{animation:bc-spin .9s linear infinite;block-size:var(--_spinner);border:2px solid color-mix(in srgb,var(--_muted) 30%,transparent);border-block-start-color:var(--_muted);border-radius:50%;inline-size:var(--_spinner)}.bc-message:not([data-active]) .bc-spinner{animation-play-state:paused}.bc-retry{align-items:center;border:var(--_border-width) solid var(--_line);border-radius:var(--_radius);color:var(--_ink);display:inline-flex;font-size:var(--_small-size);font-weight:var(--_label-weight);min-block-size:var(--_status-height);padding-inline:calc(12px*var(--_sz-space, 1));transition:background-color calc(var(--_settle)/2) var(--_ease)}.bc-retry:hover{background-color:var(--_hover-bg)}.bc-retry:focus-visible{outline:var(--_focus-width) solid var(--_focus);outline-offset:var(--_focus-offset)}.bc-sr{clip:rect(0,0,0,0);block-size:1px;border-width:0;inline-size:1px;margin:-1px;overflow:hidden;padding:0;position:absolute;white-space:nowrap}@media (pointer:coarse){.bc-retry,.bc-status{min-block-size:max(var(--_status-height),44px)}}@keyframes bc-spin{to{transform:rotate(1turn)}}@keyframes bc-roll-enter{0%{opacity:.9;transform:translateY(100%)}to{opacity:1;transform:none}}@keyframes bc-roll-leave{to{opacity:.9;transform:translateY(-100%)}}@media (prefers-reduced-motion:reduce){.bc-back,.bc-back-band,.bc-box shift-calendar::part(grid),.bc-box shift-calendar::part(header),.bc-box shift-calendar::part(heading),.bc-box shift-calendar::part(nav),.bc-box[data-view=times] .bc-back-band,.bc-box[data-view=times] .bc-times,.bc-collapse,.bc-collapse[data-open],.bc-empty,.bc-empty[data-shown],.bc-message,.bc-message[data-active],.bc-required,.bc-retry,.bc-times,.bc-times-body,.bc-title{transition:none}.bc-box[data-view=times] shift-calendar::part(header){transition:none}.bc-slot-layer[data-phase],.bc-spinner{animation:none}}.pointer-events-none{pointer-events:none}.static{position:static}.left-0{left:0}.top-0{top:0}.h-full{height:100%}.min-h-\\[150px\\]{min-height:150px}.w-full{width:100%}.items-center{align-items:center}.justify-center{justify-content:center}.opacity-0{opacity:0}.transition-opacity{transition-duration:.15s;transition-property:opacity;transition-timing-function:cubic-bezier(.4,0,.2,1)}.duration-500{transition-duration:.5s}.fixed{position:fixed}.bottom-4{bottom:1rem}.bottom-\\[88px\\]{bottom:88px}.left-1\\/2{left:50%}.z-10{z-index:10}.z-\\[9999\\]{z-index:9999}.m-0{margin:0}.aspect-video{aspect-ratio:16/9}.size-8{height:2rem;width:2rem}.size-\\[22px\\]{height:22px;width:22px}.size-\\[32px\\]{height:32px;width:32px}.size-full{height:100%;width:100%}.h-\\[100dvh\\]{height:100dvh}.h-\\[60px\\]{height:60px}.min-h-full{min-height:100%}.w-\\[100dvw\\]{width:100dvw}.w-\\[100px\\]{width:100px}.min-w-\\[140px\\]{min-width:140px}.min-w-full{min-width:100%}.-translate-x-1\\/2{--tw-translate-x:-50%;transform:translate(var(--tw-translate-x),var(--tw-translate-y)) rotate(var(--tw-rotate)) skewX(var(--tw-skew-x)) skewY(var(--tw-skew-y)) scaleX(var(--tw-scale-x)) scaleY(var(--tw-scale-y))}.animate-spin{animation:spin 1s linear infinite}.cursor-pointer{cursor:pointer}.justify-between{justify-content:space-between}.gap-2{gap:.5rem}.whitespace-nowrap{white-space:nowrap}.rounded-full{border-radius:9999px}.rounded-lg{border-radius:.5rem}.border-slate-500{--tw-border-opacity:1;border-color:rgb(100 116 139/var(--tw-border-opacity,1))}.border-slate-600{--tw-border-opacity:1;border-color:rgb(71 85 105/var(--tw-border-opacity,1))}.bg-black{--tw-bg-opacity:1;background-color:rgb(0 0 0/var(--tw-bg-opacity,1))}.bg-black\\/30{background-color:rgba(0,0,0,.3)}.bg-black\\/55{background-color:rgba(0,0,0,.55)}.bg-slate-100{--tw-bg-opacity:1;background-color:rgb(241 245 249/var(--tw-bg-opacity,1))}.bg-white{--tw-bg-opacity:1;background-color:rgb(255 255 255/var(--tw-bg-opacity,1))}.object-cover{object-fit:cover}.object-center{object-position:center}.p-1{padding:.25rem}.p-\\[16px\\]{padding:16px}.px-3{padding-left:.75rem;padding-right:.75rem}.px-6{padding-left:1.5rem;padding-right:1.5rem}.py-1{padding-bottom:.25rem;padding-top:.25rem}.py-\\[10px\\]{padding-bottom:10px;padding-top:10px}.text-center{text-align:center}.text-\\[13px\\]{font-size:13px}.text-\\[15px\\]{font-size:15px}.text-\\[18px\\]{font-size:18px}.font-medium{font-weight:500}.leading-\\[1\\.3\\]{line-height:1.3}.text-slate-100{--tw-text-opacity:1;color:rgb(241 245 249/var(--tw-text-opacity,1))}.text-slate-500{--tw-text-opacity:1;color:rgb(100 116 139/var(--tw-text-opacity,1))}.text-slate-600{--tw-text-opacity:1;color:rgb(71 85 105/var(--tw-text-opacity,1))}.text-white{--tw-text-opacity:1;color:rgb(255 255 255/var(--tw-text-opacity,1))}.shadow,.shadow-lg{box-shadow:var(--tw-ring-offset-shadow,0 0 #0000),var(--tw-ring-shadow,0 0 #0000),var(--tw-shadow)}.shadow-lg{--tw-shadow:0 10px 15px -3px rgba(0,0,0,.1),0 4px 6px -4px rgba(0,0,0,.1);--tw-shadow-colored:0 10px 15px -3px var(--tw-shadow-color),0 4px 6px -4px var(--tw-shadow-color)}.shadow-md{--tw-shadow:0 4px 6px -1px rgba(0,0,0,.1),0 2px 4px -2px rgba(0,0,0,.1);--tw-shadow-colored:0 4px 6px -1px var(--tw-shadow-color),0 2px 4px -2px var(--tw-shadow-color);box-shadow:var(--tw-ring-offset-shadow,0 0 #0000),var(--tw-ring-shadow,0 0 #0000),var(--tw-shadow)}.outline-none{outline:2px solid transparent;outline-offset:2px}.filter{filter:var(--tw-blur) var(--tw-brightness) var(--tw-contrast) var(--tw-grayscale) var(--tw-hue-rotate) var(--tw-invert) var(--tw-saturate) var(--tw-sepia) var(--tw-drop-shadow)}.transition-all{transition-duration:.15s;transition-property:all;transition-timing-function:cubic-bezier(.4,0,.2,1)}.transition-colors{transition-duration:.15s;transition-property:color,background-color,border-color,text-decoration-color,fill,stroke;transition-timing-function:cubic-bezier(.4,0,.2,1)}.duration-300{transition-duration:.3s}.hover\\:border-slate-700:hover{--tw-border-opacity:1;border-color:rgb(51 65 85/var(--tw-border-opacity,1))}.hover\\:bg-slate-300:hover{--tw-bg-opacity:1;background-color:rgb(203 213 225/var(--tw-bg-opacity,1))}.hover\\:text-slate-700:hover{--tw-text-opacity:1;color:rgb(51 65 85/var(--tw-text-opacity,1))}.disabled\\:bg-white\\/75:disabled{background-color:hsla(0,0%,100%,.75)}@media (min-width:768px){.md\\:relative{position:relative}.md\\:h-auto{height:auto}.md\\:w-\\[600px\\]{width:600px}.md\\:\\!translate-x-0{--tw-translate-x:0px!important;transform:translate(var(--tw-translate-x),var(--tw-translate-y)) rotate(var(--tw-rotate)) skewX(var(--tw-skew-x)) skewY(var(--tw-skew-y)) scaleX(var(--tw-scale-x)) scaleY(var(--tw-scale-y))!important}.md\\:overflow-hidden{overflow:hidden}.md\\:rounded-lg{border-radius:.5rem}.md\\:border-none{border-style:none}.md\\:bg-white{--tw-bg-opacity:1;background-color:rgb(255 255 255/var(--tw-bg-opacity,1))}.md\\:py-\\[8px\\]{padding-bottom:8px;padding-top:8px}.md\\:text-\\[24px\\]{font-size:24px}.md\\:text-black{--tw-text-opacity:1;color:rgb(0 0 0/var(--tw-text-opacity,1))}.md\\:\\!opacity-100{opacity:1!important}.md\\:transition-all{transition-duration:.15s;transition-property:all;transition-timing-function:cubic-bezier(.4,0,.2,1)}.md\\:duration-300{transition-duration:.3s}.md\\:hover\\:bg-slate-100:hover{--tw-bg-opacity:1;background-color:rgb(241 245 249/var(--tw-bg-opacity,1))}}.contents{display:contents}';
  }
}, [17, "shift-booking-calendar", { name: [513], form: [16], calendarApi: [1, "calendar-api"], branchId: [1, "branch-id"], departmentId: [1, "department-id"], brandId: [1, "brand-id"], today: [1], disabledWeekdays: [1, "disabled-weekdays"], disabledDates: [1, "disabled-dates"], slotCounts: [4, "slot-counts"], dayTooltips: [4, "day-tooltips"], fewSlots: [2, "few-slots"], showToday: [4, "show-today"], hourCycle: [1, "hour-cycle"], utcOffset: [1, "utc-offset"], valueFormat: [1, "value-format"], value: [1025], showLabel: [4, "show-label"], showStatus: [4, "show-status"], showEmptyState: [4, "show-empty-state"], label: [1], wrapperId: [1, "wrapper-id"], wrapperClass: [1, "wrapper-class"], isRequired: [4, "is-required"], isDisabled: [4, "is-disabled"], language: [1], localization: [16], defaultValue: [1025, "default-value"], appearance: [513], colorScheme: [513, "color-scheme"], size: [513], changeCallback: [1, "change-callback"], status: [32], availability: [32], selectedDate: [32], selectedRaw: [32], month: [32], view: [32], leavingTime: [32], announced: [32], blazorRef: [32], refresh: [64], clear: [64], setBlazorRef: [64], getValueLabel: [64], setFocus: [64] }, void 0, { calendarApi: [{ onTargetChange: 0 }], branchId: [{ onTargetChange: 0 }], departmentId: [{ onTargetChange: 0 }], brandId: [{ onTargetChange: 0 }], today: [{ onTargetChange: 0 }], disabledWeekdays: [{ onRulesChange: 0 }], disabledDates: [{ onRulesChange: 0 }], status: [{ onStatusChange: 0 }], language: [{ onLanguageChange: 0 }], valueFormat: [{ onFormatChange: 0 }], utcOffset: [{ onFormatChange: 0 }], hourCycle: [{ onFormatChange: 0 }], value: [{ onValueChange: 0 }], changeCallback: [{ onChangeCallbackChange: 0 }] }]);
function K_() {
  typeof customElements < "u" && ["shift-booking-calendar", "shift-calendar", "shift-time-slots"].forEach(((n) => {
    switch (n) {
      case "shift-booking-calendar":
        customElements.get(n) || customElements.define(n, Z_);
        break;
      case "shift-calendar":
        customElements.get(n) || ov();
        break;
      case "shift-time-slots":
        customElements.get(n) || rv();
    }
  }));
}
K_();
function _m(n) {
  const i = n.trim().replace(/^#/, "");
  if (!/^[0-9a-fA-F]{3}$|^[0-9a-fA-F]{6}$|^[0-9a-fA-F]{8}$/.test(i)) return null;
  const s = i.length === 3 ? i.split("").map((r) => r + r).join("") : i.slice(0, 6);
  return [
    parseInt(s.slice(0, 2), 16),
    parseInt(s.slice(2, 4), 16),
    parseInt(s.slice(4, 6), 16)
  ];
}
function Mc([n, i, s]) {
  const r = (c) => Math.max(0, Math.min(255, Math.round(c))).toString(16).padStart(2, "0");
  return `#${r(n)}${r(i)}${r(s)}`;
}
function J_(n, i) {
  return [n[0] * i, n[1] * i, n[2] * i];
}
function F_([n, i, s]) {
  const r = (c) => {
    const f = c / 255;
    return f <= 0.03928 ? f / 12.92 : Math.pow((f + 0.055) / 1.055, 2.4);
  };
  return 0.2126 * r(n) + 0.7152 * r(i) + 0.0722 * r(s);
}
function W_(n) {
  const i = {}, s = n != null && n.primaryColor ? _m(n.primaryColor) : null;
  s && (i["--survey-primary"] = Mc(s), i["--survey-primary-hover"] = Mc(J_(s, 0.82)), i["--survey-primary-contrast"] = F_(s) > 0.45 ? "#111111" : "#ffffff");
  const r = n != null && n.secondaryColor ? _m(n.secondaryColor) : null;
  return r && (i["--survey-accent"] = Mc(r)), i;
}
const dv = ee.createContext(null), I_ = dv.Provider;
function ht() {
  const n = ee.useContext(dv);
  if (!n)
    throw new Error(
      "useSurveyContext must be used inside <SurveyRenderer>. Question components rely on survey state from the enclosing provider."
    );
  return n;
}
function re(n, i, s) {
  if (n == null) return "";
  if (typeof n == "string") return n;
  if (n[i]) return n[i];
  if (s && n[s]) return n[s];
  const r = Object.keys(n);
  return r.length > 0 ? n[r[0]] : "";
}
const pv = {
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
}, P_ = {
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
}, e1 = {
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
}, t1 = {
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
}, a1 = { en: pv, ar: P_, ku: e1, ru: t1 }, n1 = {
  en: "English",
  ar: "العربية",
  ku: "کوردی",
  ru: "Русский",
  tr: "Türkçe",
  fa: "فارسی"
};
function i1(n) {
  const i = n1[n];
  if (i) return i;
  try {
    const s = new Intl.DisplayNames([n], { type: "language" }).of(n);
    if (s && s !== n) return s;
  } catch {
  }
  return n;
}
function _n(n, i) {
  return i ? n.replace(
    /\{(\w+)\}/g,
    (s, r) => r in i ? String(i[r]) : s
  ) : n;
}
function l1(n, i, s) {
  const r = { ...a1, ...s ?? {} };
  return r[n] ?? (i ? r[i] : void 0) ?? r.en ?? pv;
}
const s1 = "adp-surveys", r1 = 1;
function o1(n = {}) {
  const i = typeof window < "u", s = i && window.parent !== window, r = n.enabled ?? s, c = n.target ?? (i ? window.parent : null), f = n.targetOrigin ?? "*";
  if (!r || !c)
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
  const d = (h, g) => {
    const v = {
      source: s1,
      version: r1,
      type: h,
      payload: g
    };
    try {
      c.postMessage(v, f);
    } catch {
    }
  };
  return {
    loaded: () => d("survey:loaded", {}),
    screenChanged: (h) => d("survey:screen-changed", { screenId: h }),
    completed: (h) => d("survey:completed", h),
    error: (h) => d("survey:error", { message: h }),
    resize: (h) => d("survey:resize", { height: h })
  };
}
function xf(n) {
  return `adp-surveys:resume:${n}`;
}
function u1(n, i) {
  try {
    const s = n.getItem(xf(i));
    if (!s) return null;
    const r = JSON.parse(s);
    return !r || typeof r != "object" || !r.answers ? null : r;
  } catch {
    return null;
  }
}
function c1(n, i, s) {
  try {
    const r = { ...s, savedAt: Date.now() };
    n.setItem(xf(i), JSON.stringify(r));
  } catch {
  }
}
function f1(n, i) {
  try {
    n.removeItem(xf(i));
  } catch {
  }
}
const qc = /* @__PURE__ */ new Map();
function d1({
  question: n,
  Component: i
}) {
  const { locale: s, schema: r, ui: c, answerContext: f, registerSourcedOptions: d } = ht(), h = _y(n.optionsSource, f, s), g = n.id, v = `${s}|${wy(h, s)}`, [y, m] = ee.useState(() => {
    const $ = qc.get(v);
    return $ ? { status: "ready", options: $ } : { status: "loading" };
  }), [_, x] = ee.useState(0);
  ee.useEffect(() => {
    const $ = qc.get(v);
    if ($) {
      m({ status: "ready", options: $ });
      return;
    }
    let j = !1;
    return m({ status: "loading" }), ky(h, { locale: s }).then((L) => {
      qc.set(v, L), j || m({ status: "ready", options: L });
    }).catch((L) => {
      j || m({ status: "error", message: L.message ?? String(L) });
    }), () => {
      j = !0;
    };
  }, [v, _]), ee.useEffect(() => {
    y.status === "ready" && g && d(g, y.options);
  }, [y, g, d]);
  const T = n.title, A = T ? /* @__PURE__ */ N.jsx("span", { className: "survey-question__label", children: re(T, s, r.defaultLocale) }) : null;
  if (y.status === "loading")
    return /* @__PURE__ */ N.jsxs("div", { className: "survey-question survey-question--options-loading", role: "status", children: [
      A,
      /* @__PURE__ */ N.jsx("p", { className: "survey-question__options-status", children: c.loadingOptions })
    ] });
  if (y.status === "error")
    return /* @__PURE__ */ N.jsxs("div", { className: "survey-question survey-question--options-error", children: [
      A,
      /* @__PURE__ */ N.jsx("p", { className: "survey-question__options-status", role: "alert", children: c.optionsLoadError }),
      /* @__PURE__ */ N.jsx(
        "button",
        {
          type: "button",
          className: "survey-button survey-button--retry",
          onClick: () => x(($) => $ + 1),
          children: c.retry
        }
      )
    ] });
  const z = n.type === "navigationList", q = {
    ...n,
    options: y.options.map(($) => ({
      id: $.id,
      label: { [s]: $.label },
      ...z && h.nextScreen ? { nextScreen: h.nextScreen } : {}
    }))
  };
  return /* @__PURE__ */ N.jsx(i, { question: q });
}
function p1({
  question: n,
  registry: i
}) {
  const { ui: s } = ht(), r = n.type, c = r ? i[r] : void 0;
  if (!c)
    return /* @__PURE__ */ N.jsx("div", { className: "survey-question survey-question--unknown", children: /* @__PURE__ */ N.jsxs("em", { children: [
      s.unsupportedQuestion,
      " ",
      String(r ?? "missing")
    ] }) });
  const f = Array.isArray(n.options) && n.options.length > 0;
  return n.optionsSource != null && !f ? /* @__PURE__ */ N.jsx(d1, { question: n, Component: c }) : /* @__PURE__ */ N.jsx(c, { question: n });
}
function h1({ question: n }) {
  const { locale: i, schema: s, answers: r, setAnswer: c } = ht(), f = n.id, d = n.title, h = n.help, g = !!n.required, v = r[f] ?? "";
  return /* @__PURE__ */ N.jsxs("div", { className: "survey-question survey-question--text", children: [
    /* @__PURE__ */ N.jsxs("label", { className: "survey-question__label", htmlFor: `q-${f}`, children: [
      re(d, i, s.defaultLocale),
      g && /* @__PURE__ */ N.jsx("span", { "aria-label": "required", className: "survey-question__required", children: " *" })
    ] }),
    h && /* @__PURE__ */ N.jsx("p", { className: "survey-question__help", children: re(h, i, s.defaultLocale) }),
    /* @__PURE__ */ N.jsx(
      "input",
      {
        id: `q-${f}`,
        className: "survey-question__input",
        type: "text",
        value: v,
        required: g,
        onChange: (y) => c(f, y.target.value)
      }
    )
  ] });
}
function m1({ question: n }) {
  const { locale: i, schema: s, answers: r, setAnswer: c } = ht(), f = n.id, d = n.title, h = n.help, g = !!n.required, v = Number(n.min ?? 0), y = Number(n.max ?? 10), m = n.lowLabel, _ = n.highLabel, x = r[f], T = [];
  for (let A = v; A <= y; A++) T.push(A);
  return /* @__PURE__ */ N.jsxs("fieldset", { className: "survey-question survey-question--nps", children: [
    /* @__PURE__ */ N.jsxs("legend", { className: "survey-question__label", children: [
      re(d, i, s.defaultLocale),
      g && /* @__PURE__ */ N.jsx("span", { "aria-label": "required", className: "survey-question__required", children: " *" })
    ] }),
    h && /* @__PURE__ */ N.jsx("p", { className: "survey-question__help", children: re(h, i, s.defaultLocale) }),
    /* @__PURE__ */ N.jsx("div", { className: "survey-question__nps-scale", role: "radiogroup", children: T.map((A) => {
      const z = x === A;
      return /* @__PURE__ */ N.jsx(
        "button",
        {
          type: "button",
          role: "radio",
          "aria-checked": z,
          className: "survey-question__nps-step" + (z ? " survey-question__nps-step--selected" : ""),
          onClick: () => c(f, A),
          children: A
        },
        A
      );
    }) }),
    (m || _) && /* @__PURE__ */ N.jsxs("div", { className: "survey-question__nps-labels", children: [
      /* @__PURE__ */ N.jsx("span", { children: m ? re(m, i, s.defaultLocale) : "" }),
      /* @__PURE__ */ N.jsx("span", { children: _ ? re(_, i, s.defaultLocale) : "" })
    ] })
  ] });
}
function v1({ question: n }) {
  const { locale: i, schema: s, answers: r } = ht(), c = n.id, f = r[c], d = n.title, h = n.help, g = n.options ?? [], v = (y, m) => {
    const _ = {
      questionId: c,
      option: {
        id: m.id,
        nextScreen: m.nextScreen
      }
    };
    y.currentTarget.dispatchEvent(
      new CustomEvent("survey:navigationListSelect", {
        detail: _,
        bubbles: !0
      })
    );
  };
  return /* @__PURE__ */ N.jsxs("div", { className: "survey-question survey-question--navlist", children: [
    /* @__PURE__ */ N.jsx("div", { className: "survey-question__label", children: re(d, i, s.defaultLocale) }),
    h && /* @__PURE__ */ N.jsx("p", { className: "survey-question__help", children: re(h, i, s.defaultLocale) }),
    /* @__PURE__ */ N.jsx("ul", { className: "survey-navlist", role: "radiogroup", "aria-description": "Selecting an option navigates to the next screen.", children: g.map((y) => {
      const m = y.id, _ = y.label, x = f === m;
      return /* @__PURE__ */ N.jsx("li", { className: "survey-navlist__row", children: /* @__PURE__ */ N.jsxs(
        "button",
        {
          type: "button",
          className: x ? "survey-navlist__button survey-navlist__button--selected" : "survey-navlist__button",
          "aria-pressed": x,
          onClick: (T) => v(T, y),
          children: [
            /* @__PURE__ */ N.jsx("span", { className: "survey-navlist__label", children: re(_, i, s.defaultLocale) }),
            /* @__PURE__ */ N.jsx("span", { "aria-hidden": "true", className: "survey-navlist__chevron", children: "›" })
          ]
        }
      ) }, m);
    }) })
  ] });
}
function g1({ question: n }) {
  const { locale: i, schema: s, answers: r, setAnswer: c } = ht(), f = n.id, d = n.title, h = n.help, g = n.placeholder, v = !!n.required, y = n.minLength, m = n.maxLength, _ = r[f] ?? "";
  return /* @__PURE__ */ N.jsxs("div", { className: "survey-question survey-question--paragraph", children: [
    /* @__PURE__ */ N.jsxs("label", { className: "survey-question__label", htmlFor: `q-${f}`, children: [
      re(d, i, s.defaultLocale),
      v && /* @__PURE__ */ N.jsx("span", { "aria-label": "required", className: "survey-question__required", children: " *" })
    ] }),
    h && /* @__PURE__ */ N.jsx("p", { className: "survey-question__help", children: re(h, i, s.defaultLocale) }),
    /* @__PURE__ */ N.jsx(
      "textarea",
      {
        id: `q-${f}`,
        className: "survey-question__textarea",
        value: _,
        required: v,
        rows: 5,
        minLength: y,
        maxLength: m,
        placeholder: g ? re(g, i, s.defaultLocale) : void 0,
        onChange: (x) => c(f, x.target.value)
      }
    )
  ] });
}
function b1({ question: n }) {
  const { locale: i, schema: s, answers: r, setAnswer: c } = ht(), f = n.id, d = n.title, h = n.help, g = !!n.required, v = n.min, y = n.max, m = n.step, _ = n.unit, x = r[f], T = x == null ? "" : String(x);
  return /* @__PURE__ */ N.jsxs("div", { className: "survey-question survey-question--number", children: [
    /* @__PURE__ */ N.jsxs("label", { className: "survey-question__label", htmlFor: `q-${f}`, children: [
      re(d, i, s.defaultLocale),
      g && /* @__PURE__ */ N.jsx("span", { "aria-label": "required", className: "survey-question__required", children: " *" })
    ] }),
    h && /* @__PURE__ */ N.jsx("p", { className: "survey-question__help", children: re(h, i, s.defaultLocale) }),
    /* @__PURE__ */ N.jsxs("div", { className: "survey-question__number-wrap", children: [
      /* @__PURE__ */ N.jsx(
        "input",
        {
          id: `q-${f}`,
          className: "survey-question__input",
          type: "number",
          value: T,
          required: g,
          min: v,
          max: y,
          step: m,
          onChange: (A) => {
            const z = A.target.value;
            c(f, z === "" ? null : Number(z));
          }
        }
      ),
      _ && /* @__PURE__ */ N.jsx("span", { className: "survey-question__unit", children: re(_, i, s.defaultLocale) })
    ] })
  ] });
}
function y1({ question: n }) {
  const { locale: i, schema: s, answers: r, setAnswer: c } = ht(), f = n.id, d = n.title, h = n.help, g = !!n.required, v = Number(n.max ?? 5), y = r[f], m = [];
  for (let _ = 1; _ <= v; _++) m.push(_);
  return /* @__PURE__ */ N.jsxs("fieldset", { className: "survey-question survey-question--rating", children: [
    /* @__PURE__ */ N.jsxs("legend", { className: "survey-question__label", children: [
      re(d, i, s.defaultLocale),
      g && /* @__PURE__ */ N.jsx("span", { "aria-label": "required", className: "survey-question__required", children: " *" })
    ] }),
    h && /* @__PURE__ */ N.jsx("p", { className: "survey-question__help", children: re(h, i, s.defaultLocale) }),
    /* @__PURE__ */ N.jsx("div", { className: "survey-question__rating-scale", role: "radiogroup", children: m.map((_) => {
      const x = typeof y == "number" && _ <= y;
      return /* @__PURE__ */ N.jsx(
        "button",
        {
          type: "button",
          role: "radio",
          "aria-checked": y === _,
          "aria-label": `${_}`,
          className: "survey-question__rating-star" + (x ? " survey-question__rating-star--selected" : ""),
          onClick: () => c(f, _),
          children: /* @__PURE__ */ N.jsx("span", { "aria-hidden": "true", children: "★" })
        },
        _
      );
    }) })
  ] });
}
function _1({ question: n }) {
  const { locale: i, schema: s, answers: r, setAnswer: c } = ht(), f = n.id, d = n.title, h = n.help, g = !!n.required, v = n.options ?? [], y = r[f];
  return /* @__PURE__ */ N.jsxs("fieldset", { className: "survey-question survey-question--single", children: [
    /* @__PURE__ */ N.jsxs("legend", { className: "survey-question__label", children: [
      re(d, i, s.defaultLocale),
      g && /* @__PURE__ */ N.jsx("span", { "aria-label": "required", className: "survey-question__required", children: " *" })
    ] }),
    h && /* @__PURE__ */ N.jsx("p", { className: "survey-question__help", children: re(h, i, s.defaultLocale) }),
    /* @__PURE__ */ N.jsx("div", { className: "survey-question__options", children: v.map((m) => /* @__PURE__ */ N.jsxs("label", { className: "survey-question__option", children: [
      /* @__PURE__ */ N.jsx(
        "input",
        {
          type: "radio",
          name: `q-${f}`,
          value: m.id,
          checked: y === m.id,
          onChange: () => c(f, m.id)
        }
      ),
      /* @__PURE__ */ N.jsx("span", { children: re(m.label, i, s.defaultLocale) })
    ] }, m.id)) })
  ] });
}
function x1({ question: n }) {
  const { locale: i, schema: s, answers: r, setAnswer: c } = ht(), f = n.id, d = n.title, h = n.help, g = !!n.required, v = n.options ?? [], y = n.maxSelected, m = r[f] ?? [], _ = (x) => {
    if (m.includes(x)) {
      c(f, m.filter((T) => T !== x));
      return;
    }
    y !== void 0 && m.length >= y || c(f, [...m, x]);
  };
  return /* @__PURE__ */ N.jsxs("fieldset", { className: "survey-question survey-question--multi", children: [
    /* @__PURE__ */ N.jsxs("legend", { className: "survey-question__label", children: [
      re(d, i, s.defaultLocale),
      g && /* @__PURE__ */ N.jsx("span", { "aria-label": "required", className: "survey-question__required", children: " *" })
    ] }),
    h && /* @__PURE__ */ N.jsx("p", { className: "survey-question__help", children: re(h, i, s.defaultLocale) }),
    /* @__PURE__ */ N.jsx("div", { className: "survey-question__options", children: v.map((x) => {
      const T = m.includes(x.id);
      return /* @__PURE__ */ N.jsxs("label", { className: "survey-question__option", children: [
        /* @__PURE__ */ N.jsx(
          "input",
          {
            type: "checkbox",
            checked: T,
            onChange: () => _(x.id)
          }
        ),
        /* @__PURE__ */ N.jsx("span", { children: re(x.label, i, s.defaultLocale) })
      ] }, x.id);
    }) })
  ] });
}
function w1({ question: n }) {
  const { locale: i, schema: s, answers: r, setAnswer: c, ui: f } = ht(), d = n.id, h = n.title, g = n.help, v = !!n.required, y = n.options ?? [], m = n.placeholder, _ = r[d] ?? "", x = m ? re(m, i, s.defaultLocale) : f.selectPlaceholder;
  return /* @__PURE__ */ N.jsxs("div", { className: "survey-question survey-question--dropdown", children: [
    /* @__PURE__ */ N.jsxs("label", { className: "survey-question__label", htmlFor: `q-${d}`, children: [
      re(h, i, s.defaultLocale),
      v && /* @__PURE__ */ N.jsx("span", { "aria-label": "required", className: "survey-question__required", children: " *" })
    ] }),
    g && /* @__PURE__ */ N.jsx("p", { className: "survey-question__help", children: re(g, i, s.defaultLocale) }),
    /* @__PURE__ */ N.jsxs(
      "select",
      {
        id: `q-${d}`,
        className: "survey-question__select",
        value: _,
        required: v,
        onChange: (T) => c(d, T.target.value || null),
        children: [
          /* @__PURE__ */ N.jsx("option", { value: "", children: x }),
          y.map((T) => /* @__PURE__ */ N.jsx("option", { value: T.id, children: re(T.label, i, s.defaultLocale) }, T.id))
        ]
      }
    )
  ] });
}
function k1({ question: n }) {
  const { locale: i, schema: s, answers: r, setAnswer: c } = ht(), f = n.id, d = n.title, h = n.help, g = !!n.required, v = n.minDate, y = n.maxDate, m = r[f] ?? "";
  return /* @__PURE__ */ N.jsxs("div", { className: "survey-question survey-question--date", children: [
    /* @__PURE__ */ N.jsxs("label", { className: "survey-question__label", htmlFor: `q-${f}`, children: [
      re(d, i, s.defaultLocale),
      g && /* @__PURE__ */ N.jsx("span", { "aria-label": "required", className: "survey-question__required", children: " *" })
    ] }),
    h && /* @__PURE__ */ N.jsx("p", { className: "survey-question__help", children: re(h, i, s.defaultLocale) }),
    /* @__PURE__ */ N.jsx(
      "input",
      {
        id: `q-${f}`,
        className: "survey-question__input",
        type: "date",
        value: m,
        required: g,
        min: v,
        max: y,
        onChange: (_) => c(f, _.target.value || null)
      }
    )
  ] });
}
function S1({ question: n }) {
  const { locale: i, schema: s, answers: r, setAnswer: c } = ht(), f = n.id, d = n.title, h = n.help, g = !!n.required, v = n.minDateTime, y = n.maxDateTime, m = r[f] ?? "", _ = (x) => {
    if (!x) return;
    const T = x.match(/^(\d{4}-\d{2}-\d{2}T\d{2}:\d{2})/);
    return (T == null ? void 0 : T[1]) ?? void 0;
  };
  return /* @__PURE__ */ N.jsxs("div", { className: "survey-question survey-question--datetime", children: [
    /* @__PURE__ */ N.jsxs("label", { className: "survey-question__label", htmlFor: `q-${f}`, children: [
      re(d, i, s.defaultLocale),
      g && /* @__PURE__ */ N.jsx("span", { "aria-label": "required", className: "survey-question__required", children: " *" })
    ] }),
    h && /* @__PURE__ */ N.jsx("p", { className: "survey-question__help", children: re(h, i, s.defaultLocale) }),
    /* @__PURE__ */ N.jsx(
      "input",
      {
        id: `q-${f}`,
        className: "survey-question__input",
        type: "datetime-local",
        value: _(m) ?? "",
        required: g,
        min: _(v),
        max: _(y),
        onChange: (x) => c(f, x.target.value || null)
      }
    )
  ] });
}
function z1({ question: n }) {
  const { locale: i, schema: s, answers: r, setAnswer: c, ui: f } = ht(), d = n.id, h = n.title, g = n.help, v = !!n.required, y = n.acceptedTypes, m = ee.useRef(null), _ = r[d], x = y && y.length > 0 ? y.join(",") : void 0;
  return /* @__PURE__ */ N.jsxs("div", { className: "survey-question survey-question--file", children: [
    /* @__PURE__ */ N.jsxs("label", { className: "survey-question__label", htmlFor: `q-${d}`, children: [
      re(h, i, s.defaultLocale),
      v && /* @__PURE__ */ N.jsx("span", { "aria-label": "required", className: "survey-question__required", children: " *" })
    ] }),
    g && /* @__PURE__ */ N.jsx("p", { className: "survey-question__help", children: re(g, i, s.defaultLocale) }),
    /* @__PURE__ */ N.jsx(
      "input",
      {
        ref: m,
        id: `q-${d}`,
        className: "survey-question__file",
        type: "file",
        required: v,
        accept: x,
        onChange: (T) => {
          var A;
          const z = (A = T.target.files) == null ? void 0 : A[0];
          if (!z) {
            c(d, null);
            return;
          }
          c(d, { name: z.name, size: z.size, type: z.type });
        }
      }
    ),
    (_ == null ? void 0 : _.name) && /* @__PURE__ */ N.jsx("p", { className: "survey-question__file-name", children: _n(f.fileRecordedName, { name: _.name }) })
  ] });
}
const jc = 480, Rc = 160;
function T1({ question: n }) {
  const { locale: i, schema: s, answers: r, setAnswer: c, ui: f } = ht(), d = n.id, h = n.title, g = n.help, v = !!n.required, y = ee.useRef(null), [m, _] = ee.useState(!1), [x, T] = ee.useState(!!r[d]), A = () => {
    var j;
    return ((j = y.current) == null ? void 0 : j.getContext("2d")) ?? null;
  }, z = (j) => {
    const L = j.target.getBoundingClientRect();
    return {
      x: (j.clientX - L.left) / L.width * jc,
      y: (j.clientY - L.top) / L.height * Rc
    };
  }, q = ee.useCallback(() => {
    var j;
    const L = (j = y.current) == null ? void 0 : j.toDataURL("image/png");
    L && c(d, L);
  }, [d, c]), $ = () => {
    const j = A();
    j && (j.clearRect(0, 0, jc, Rc), T(!1), c(d, null));
  };
  return ee.useEffect(() => {
    const j = A();
    j && (j.lineWidth = 2, j.lineCap = "round", j.strokeStyle = "#111");
  }, []), /* @__PURE__ */ N.jsxs("div", { className: "survey-question survey-question--signature", children: [
    /* @__PURE__ */ N.jsxs("div", { className: "survey-question__label", children: [
      re(h, i, s.defaultLocale),
      v && /* @__PURE__ */ N.jsx("span", { "aria-label": "required", className: "survey-question__required", children: " *" })
    ] }),
    g && /* @__PURE__ */ N.jsx("p", { className: "survey-question__help", children: re(g, i, s.defaultLocale) }),
    /* @__PURE__ */ N.jsx(
      "canvas",
      {
        ref: y,
        className: "survey-question__signature-canvas",
        width: jc,
        height: Rc,
        role: "img",
        "aria-label": "signature pad",
        onPointerDown: (j) => {
          j.target.setPointerCapture(j.pointerId);
          const L = A();
          if (!L) return;
          const { x: V, y: J } = z(j);
          L.beginPath(), L.moveTo(V, J), _(!0);
        },
        onPointerMove: (j) => {
          if (!m) return;
          const L = A();
          if (!L) return;
          const { x: V, y: J } = z(j);
          L.lineTo(V, J), L.stroke(), T(!0);
        },
        onPointerUp: () => {
          _(!1), x && q();
        }
      }
    ),
    /* @__PURE__ */ N.jsx("div", { className: "survey-question__signature-actions", children: /* @__PURE__ */ N.jsx("button", { type: "button", className: "survey-button survey-button--ghost", onClick: $, children: f.clearSignature }) })
  ] });
}
function E1({ question: n }) {
  const { locale: i, schema: s, answers: r, setAnswer: c, ui: f } = ht(), d = n.id, h = n.title, g = n.help, v = !!n.required, y = n.yesLabel, m = n.noLabel, _ = r[d], x = y ? re(y, i, s.defaultLocale) : f.yes, T = m ? re(m, i, s.defaultLocale) : f.no;
  return /* @__PURE__ */ N.jsxs("fieldset", { className: "survey-question survey-question--yesno", children: [
    /* @__PURE__ */ N.jsxs("legend", { className: "survey-question__label", children: [
      re(h, i, s.defaultLocale),
      v && /* @__PURE__ */ N.jsx("span", { "aria-label": "required", className: "survey-question__required", children: " *" })
    ] }),
    g && /* @__PURE__ */ N.jsx("p", { className: "survey-question__help", children: re(g, i, s.defaultLocale) }),
    /* @__PURE__ */ N.jsxs("div", { className: "survey-question__yesno", role: "radiogroup", children: [
      /* @__PURE__ */ N.jsx(
        "button",
        {
          type: "button",
          role: "radio",
          "aria-checked": _ === !0,
          className: "survey-question__yesno-button" + (_ === !0 ? " survey-question__yesno-button--selected" : ""),
          onClick: () => c(d, !0),
          children: x
        }
      ),
      /* @__PURE__ */ N.jsx(
        "button",
        {
          type: "button",
          role: "radio",
          "aria-checked": _ === !1,
          className: "survey-question__yesno-button" + (_ === !1 ? " survey-question__yesno-button--selected" : ""),
          onClick: () => c(d, !1),
          children: T
        }
      )
    ] })
  ] });
}
const A1 = ["en", "ar", "ku", "ru"], $c = /* @__PURE__ */ new WeakMap();
function O1({ question: n }) {
  const { locale: i, schema: s, answers: r, setAnswer: c, answerContext: f } = ht(), d = n.id, h = n.title, g = n.help, v = !!n.required, y = r[d] ?? "", m = (V, J = "queryValue") => ps(String(n[V] ?? ""), f, i, J).trim(), _ = m("calendarApi", "url"), x = m("branchId"), T = m("departmentId"), A = m("brandId"), z = [_, x, T, A].join(`
`), q = A1.includes(i.slice(0, 2)) ? i.slice(0, 2) : "en", $ = re(h, i, s.defaultLocale), j = $c.get(s) ?? /* @__PURE__ */ new Map();
  $c.has(s) || $c.set(s, j), ee.useEffect(() => {
    const V = j.get(d);
    y && V !== void 0 && V !== z && (j.delete(d), c(d, null));
  }, [z]);
  const L = ee.useRef(null);
  return ee.useEffect(() => {
    const V = L.current;
    V && (V.calendarApi = _, V.branchId = x, V.departmentId = T, V.brandId = A, V.language = q, V.label = $, V.isRequired = v, V.showStatus = !0, V.defaultValue = j.get(d) === z ? y : "");
  }, [_, x, T, A, q, $, v]), ee.useEffect(() => {
    const V = L.current;
    if (!V) return;
    const J = (Z) => {
      const F = Z.detail;
      j.set(d, z), c(d, (F == null ? void 0 : F.value) || null);
    };
    return V.addEventListener("slotChange", J), () => V.removeEventListener("slotChange", J);
  }, [d, z, c]), /* @__PURE__ */ N.jsxs("div", { className: "survey-question survey-question--booking-slot", children: [
    /* @__PURE__ */ N.jsxs("div", { className: "survey-question__label", "aria-hidden": "true", children: [
      $,
      v && /* @__PURE__ */ N.jsx("span", { className: "survey-question__required", children: " *" })
    ] }),
    g && /* @__PURE__ */ N.jsx("p", { className: "survey-question__help", children: re(g, i, s.defaultLocale) }),
    /* @__PURE__ */ N.jsx("shift-booking-calendar", { id: `q-${d}`, ref: L })
  ] });
}
function D1(n, i) {
  switch (n.code) {
    case "minLength":
      return _n(i.minLengthError, n.params);
    case "maxLength":
      return _n(i.maxLengthError, n.params);
    case "pattern":
      return i.patternError;
    case "min":
      return _n(i.minError, n.params);
    case "max":
      return _n(i.maxError, n.params);
    case "range":
      return _n(i.rangeError, n.params);
    case "minSelected":
      return _n(i.minSelectedError, n.params);
    case "maxSelected":
      return _n(i.maxSelectedError, n.params);
    default:
      return i.invalidAnswerError;
  }
}
const C1 = {
  text: h1,
  paragraph: g1,
  number: b1,
  rating: y1,
  nps: m1,
  singleChoice: _1,
  multiChoice: x1,
  dropdown: w1,
  date: k1,
  dateTime: S1,
  bookingSlot: O1,
  file: z1,
  signature: T1,
  yesNo: E1,
  navigationList: v1
};
function N1(n, i, s) {
  const r = n.screens.find((c) => c.id === i);
  return !r || (r.questions ?? []).length > 0 ? !1 : ol(n, i, s).kind === "end";
}
function M1({
  schema: n,
  onSubmit: i,
  initialAnswers: s,
  locale: r,
  onLocaleChange: c,
  showLocalePicker: f,
  onScreenChange: d,
  onCompleted: h,
  registry: g,
  submissionMeta: v,
  uiLocales: y,
  resumeKey: m,
  storage: _,
  emitHostMessages: x,
  hostMessageOrigin: T,
  hostMessageTarget: A,
  activeScreenId: z,
  activeScreenJumpToken: q
}) {
  var $, j, L;
  const [V, J] = ee.useState(null), Z = r ?? n.defaultLocale ?? "en", F = V !== null && ((($ = n.locales) == null ? void 0 : $.includes(V)) ?? !1) ? V : Z, Te = ee.useRef(Z);
  ee.useEffect(() => {
    Te.current !== Z && (Te.current = Z, J(null));
  }, [Z]);
  const Se = g ?? C1, oe = ee.useMemo(
    () => l1(F, n.defaultLocale, y),
    [F, n.defaultLocale, y]
  ), Ye = n.locales ?? [], ba = f ?? Ye.length > 1, It = ee.useCallback(
    (G) => {
      J(G), c == null || c(G);
    },
    [c]
  ), Ke = _ ?? (typeof globalThis < "u" ? globalThis.localStorage : void 0), R = ee.useMemo(() => {
    var G;
    if (!m || !Ke) return null;
    const ae = u1(Ke, m);
    return ae ? ae.currentScreenId === null || n.screens.some((_e) => _e.id === ae.currentScreenId) ? !ae.history && ae.currentScreenId ? { ...ae, history: ay(n, ae.answers, ae.currentScreenId) ?? [] } : ae : { ...ae, currentScreenId: ((G = n.screens[0]) == null ? void 0 : G.id) ?? null, history: [] } : null;
  }, []), [Q, le] = ee.useState(() => ({
    ...s ?? {},
    ...(R == null ? void 0 : R.answers) ?? {}
  })), [W, ye] = ee.useState(
    () => {
      var G;
      return (R == null ? void 0 : R.currentScreenId) ?? ((G = n.screens[0]) == null ? void 0 : G.id) ?? null;
    }
  ), [k, Y] = ee.useState(() => (R == null ? void 0 : R.history) ?? []);
  ee.useEffect(() => {
    if (n.screens.length === 0) {
      W !== null && ye(null);
      return;
    }
    W !== null && n.screens.some((G) => G.id === W) || ye(n.screens[0].id);
  }, [n, W]);
  const [K, I] = ee.useState(!1), [se, fe] = ee.useState(null), [xe, Je] = ee.useState(/* @__PURE__ */ new Set()), [He, ra] = ee.useState(/* @__PURE__ */ new Set()), [yt, dl] = ee.useState(!1), pl = ee.useRef(void 0);
  ee.useEffect(() => {
    if (z === void 0) return;
    const G = `${q ?? ""}:${z ?? ""}`;
    pl.current !== G && (pl.current = G, !(z === null || yt) && n.screens.some((ae) => ae.id === z) && (Je(/* @__PURE__ */ new Set()), ra(/* @__PURE__ */ new Set()), Y((ae) => {
      const _e = ae.indexOf(z);
      return _e >= 0 ? ae.slice(0, _e) : W && W !== z ? [...ae, W] : ae;
    }), ye(z)));
  }, [z, q, n, yt, W]);
  const ya = ee.useRef((/* @__PURE__ */ new Date()).toISOString()), Pt = ee.useRef(null);
  if (Pt.current === null) {
    const G = {};
    A !== void 0 && (G.target = A), T !== void 0 && (G.targetOrigin = T), x !== void 0 && (G.enabled = x), Pt.current = o1(G);
  }
  const mt = ee.useMemo(
    () => W ? n.screens.find((G) => G.id === W) ?? null : null,
    [n, W]
  ), xs = ee.useMemo(() => {
    const G = /* @__PURE__ */ new Map();
    for (const ae of n.screens)
      for (const _e of ae.questions ?? []) {
        const Re = _e.id;
        typeof Re == "string" && G.set(Re, ae.id);
      }
    return G;
  }, [n]), vt = ee.useMemo(() => {
    const G = new Set(k);
    W && G.add(W);
    const ae = {};
    for (const [_e, Re] of Object.entries(Q)) {
      const Fe = xs.get(_e);
      (Fe === void 0 || G.has(Fe)) && (ae[_e] = Re);
    }
    return ae;
  }, [Q, k, W, xs]), Cn = ee.useRef(/* @__PURE__ */ new Map()), [hl, ml] = ee.useState(0), ws = ee.useCallback((G, ae) => {
    Cn.current.get(G) !== ae && (Cn.current.set(G, ae), ml((_e) => _e + 1));
  }, []), fi = ee.useMemo(
    () => hy({
      schema: n,
      answers: vt,
      lookupOptions: (G) => Cn.current.get(G),
      yesNoLabels: { yes: oe.strings.yes, no: oe.strings.no }
    }),
    // sourcedVersion is the change signal for the ref-held map.
    // eslint-disable-next-line react-hooks/exhaustive-deps
    [n, vt, oe.strings.yes, oe.strings.no, hl]
  ), Me = ee.useMemo(
    () => mt ? yy(mt, fi) : null,
    [mt, fi]
  );
  ee.useEffect(() => {
    var G;
    d == null || d(W), (G = Pt.current) == null || G.screenChanged(W);
  }, [W, d]);
  const ks = ee.useRef(!1);
  ee.useEffect(() => {
    var G;
    ks.current || !W || (ks.current = !0, (G = Pt.current) == null || G.loaded());
  }, [W]), ee.useEffect(() => {
    !m || !Ke || yt || c1(Ke, m, {
      answers: Q,
      currentScreenId: W,
      history: [...k],
      schemaVersion: n.version
    });
  }, [Q, W, k, m, Ke, yt, n.version]), ee.useEffect(() => {
    yt && m && Ke && f1(Ke, m);
  }, [yt, m, Ke]), ee.useEffect(() => {
    var G;
    se && ((G = Pt.current) == null || G.error(se));
  }, [se]);
  const Nn = ee.useCallback((G, ae) => {
    le((_e) => ({ ..._e, [G]: ae }));
  }, []), Mn = ee.useCallback(
    (G) => {
      G !== null && (Je(/* @__PURE__ */ new Set()), ra(/* @__PURE__ */ new Set()), W && W !== G && Y((ae) => [...ae, W]), ye(G));
    },
    [W]
  ), di = ee.useCallback(() => {
    let G = k.length - 1;
    for (; G >= 0 && !n.screens.some((_e) => _e.id === k[G]); ) G--;
    if (G < 0) return;
    const ae = k[G];
    Je(/* @__PURE__ */ new Set()), ra(/* @__PURE__ */ new Set()), fe(null), Y(k.slice(0, G)), ye(ae);
  }, [k, n]), so = k.some((G) => n.screens.some((ae) => ae.id === G)), pi = ee.useCallback(
    (G) => {
      if (!G.required) return !1;
      const ae = Q[G.id];
      return !!(ae == null || typeof ae == "string" && ae.trim() === "" || Array.isArray(ae) && ae.length === 0);
    },
    [Q]
  ), Qa = ee.useCallback(async () => {
    var G;
    I(!0), fe(null);
    try {
      await i({
        schemaVersion: n.version ?? 0,
        answers: vt,
        meta: {
          startedAt: (v == null ? void 0 : v.startedAt) ?? ya.current,
          completedAt: (v == null ? void 0 : v.completedAt) ?? (/* @__PURE__ */ new Date()).toISOString(),
          ...v ?? {}
        }
      }), dl(!0), h == null || h(W), (G = Pt.current) == null || G.completed({ screenId: W, answers: vt });
    } catch (ae) {
      fe(ae.message ?? String(ae));
    } finally {
      I(!1);
    }
  }, [n.version, vt, v, i, h, W]), ro = ee.useCallback(() => {
    if (!W) return;
    const G = n.screens.find((Fe) => Fe.id === W), ae = ((G == null ? void 0 : G.questions) ?? []).filter(pi).map((Fe) => Fe.id);
    if (ae.length > 0) {
      Je(new Set(ae));
      return;
    }
    const _e = ly(G == null ? void 0 : G.questions, Q);
    if (_e.length > 0) {
      ra(new Set(_e.map((Fe) => Fe.questionId)));
      return;
    }
    const Re = ol(n, W, vt);
    Re.kind === "end" ? Qa() : Mn(Re.screenId);
  }, [n, W, Q, vt, pi, Mn, Qa]), Za = ee.useRef(null);
  ee.useEffect(() => {
    yt || K || !W || !mt || Za.current === W || !(!mt.questions || mt.questions.length === 0) || ol(n, W, vt).kind === "end" && (Za.current = W, Qa());
  }, [W, mt, yt, K, n, vt, Qa]);
  const tt = ee.useRef(null);
  ee.useEffect(() => {
    const G = tt.current;
    if (!G || typeof ResizeObserver > "u") return;
    const ae = new ResizeObserver((_e) => {
      var Re;
      const Fe = _e[0];
      Fe && ((Re = Pt.current) == null || Re.resize(Math.ceil(Fe.contentRect.height)));
    });
    return ae.observe(G), () => ae.disconnect();
  }, []), ee.useEffect(() => {
    const G = tt.current;
    if (!G) return;
    const ae = (_e) => {
      const Re = _e.detail;
      if (!Re || !W) return;
      Nn(Re.questionId, Re.option.id);
      const Fe = { ...vt, [Re.questionId]: Re.option.id }, mi = ty(
        Re.option,
        n,
        W,
        Fe
      );
      mi.kind === "end" ? Qa() : Mn(mi.screenId);
    };
    return G.addEventListener("survey:navigationListSelect", ae), () => G.removeEventListener("survey:navigationListSelect", ae);
  }, [vt, W, n, Nn, Mn, Qa]);
  const oa = ee.useMemo(
    () => ({
      schema: n,
      locale: F,
      direction: oe.direction,
      ui: oe.strings,
      answers: Q,
      setAnswer: Nn,
      answerContext: fi,
      registerSourcedOptions: ws
    }),
    [n, F, oe, Q, Nn, fi, ws]
  ), rt = ee.useMemo(() => W_(n.branding), [n.branding]), Ss = (j = n.branding) != null && j.logoUrl ? /* @__PURE__ */ N.jsx("div", { className: "survey-brand", children: /* @__PURE__ */ N.jsx(
    "img",
    {
      className: "survey-brand__logo",
      src: n.branding.logoUrl,
      alt: "",
      onError: (G) => {
        G.currentTarget.parentElement.style.display = "none";
      }
    }
  ) }) : null, oo = Ye.includes(F) ? Ye : [F, ...Ye], zs = ba ? /* @__PURE__ */ N.jsxs("label", { className: "survey-locale", children: [
    /* @__PURE__ */ N.jsx("span", { className: "survey-locale__icon", "aria-hidden": "true", children: /* @__PURE__ */ N.jsxs("svg", { viewBox: "0 0 24 24", width: "18", height: "18", fill: "none", stroke: "currentColor", strokeWidth: "1.8", children: [
      /* @__PURE__ */ N.jsx("circle", { cx: "12", cy: "12", r: "9" }),
      /* @__PURE__ */ N.jsx("path", { d: "M3 12h18M12 3a15 15 0 0 1 0 18a15 15 0 0 1 0-18" })
    ] }) }),
    /* @__PURE__ */ N.jsx("span", { className: "survey-locale__code", "aria-hidden": "true", children: F.toUpperCase() }),
    /* @__PURE__ */ N.jsx(
      "select",
      {
        className: "survey-locale__select",
        "aria-label": oe.strings.language,
        value: F,
        onChange: (G) => It(G.target.value),
        children: oo.map((G) => /* @__PURE__ */ N.jsx("option", { value: G, children: i1(G) }, G))
      }
    )
  ] }) : null, hi = (mt == null ? void 0 : mt.questions) ?? [], qn = mt !== null && hi.length === 0 && !mt.nextScreen, _a = !yt && so && !K && !qn, ua = n.screens.length > 1 ? /* @__PURE__ */ N.jsx(
    "button",
    {
      type: "button",
      className: _a ? "survey-back" : "survey-back survey-back--hidden",
      "aria-label": oe.strings.back,
      "aria-hidden": !_a,
      tabIndex: _a ? void 0 : -1,
      onClick: di,
      children: /* @__PURE__ */ N.jsx(
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
          children: /* @__PURE__ */ N.jsx("path", { d: "M15 5l-7 7 7 7" })
        }
      )
    }
  ) : null, Ka = Ss || zs || ua ? /* @__PURE__ */ N.jsxs("div", { className: _a ? "survey-chrome survey-chrome--back" : "survey-chrome", children: [
    /* @__PURE__ */ N.jsx("div", { className: "survey-chrome__start", children: ua }),
    Ss,
    /* @__PURE__ */ N.jsx("div", { className: "survey-chrome__end", children: zs })
  ] }) : null;
  if (yt)
    return /* @__PURE__ */ N.jsxs(
      "div",
      {
        ref: tt,
        className: "survey-root survey-root--done",
        dir: oe.direction,
        lang: F,
        style: rt,
        children: [
          Ka,
          /* @__PURE__ */ N.jsxs("div", { className: "survey-screen", children: [
            /* @__PURE__ */ N.jsx("h2", { className: "survey-screen__title", children: Me != null && Me.title ? re(Me.title, F, n.defaultLocale) : oe.strings.thankYou }),
            (Me == null ? void 0 : Me.description) && /* @__PURE__ */ N.jsx("p", { className: "survey-screen__description", children: re(Me.description, F, n.defaultLocale) })
          ] })
        ]
      }
    );
  if (!mt || !Me)
    return /* @__PURE__ */ N.jsxs("div", { ref: tt, className: "survey-root", dir: oe.direction, lang: F, style: rt, children: [
      Ka,
      /* @__PURE__ */ N.jsx("div", { className: "survey-screen", children: /* @__PURE__ */ N.jsx("em", { children: oe.strings.noScreens }) })
    ] });
  const ca = Me.questions ?? [], Ts = !(ca.length > 0 && ((L = ca[ca.length - 1]) == null ? void 0 : L.type) === "navigationList") && !qn, jn = Ts && W !== null ? ol(n, W, vt) : null, vl = jn !== null && (jn.kind === "end" || jn.kind === "screen" && N1(n, jn.screenId, vt));
  return /* @__PURE__ */ N.jsx(I_, { value: oa, children: /* @__PURE__ */ N.jsxs("div", { ref: tt, className: "survey-root", dir: oe.direction, lang: F, style: rt, children: [
    Ka,
    /* @__PURE__ */ N.jsxs("div", { className: "survey-screen", children: [
      Me.title && /* @__PURE__ */ N.jsx("h2", { className: "survey-screen__title", children: re(Me.title, F, n.defaultLocale) }),
      Me.description && /* @__PURE__ */ N.jsx("p", { className: "survey-screen__description", children: re(Me.description, F, n.defaultLocale) }),
      /* @__PURE__ */ N.jsx("div", { className: "survey-screen__questions", children: ca.map((G, ae) => {
        const _e = G.id, Re = _e !== void 0 && xe.has(_e) && pi(G), Fe = !Re && _e !== void 0 && He.has(_e) && Q[_e] != null ? wm(G, Q[_e])[0] ?? null : null;
        return /* @__PURE__ */ N.jsxs("div", { className: Re || Fe !== null ? "survey-question-slot survey-question-slot--invalid" : "survey-question-slot", children: [
          /* @__PURE__ */ N.jsx(p1, { question: G, registry: Se }),
          Re && /* @__PURE__ */ N.jsx("p", { className: "survey-question__required-error", role: "alert", children: oe.strings.requiredError }),
          Fe && /* @__PURE__ */ N.jsx("p", { className: "survey-question__required-error", role: "alert", children: D1(Fe, oe.strings) })
        ] }, _e ?? ae);
      }) }),
      Ts && /* @__PURE__ */ N.jsx("div", { className: "survey-screen__actions", children: /* @__PURE__ */ N.jsx(
        "button",
        {
          type: "button",
          className: "survey-button survey-button--primary",
          disabled: K,
          onClick: ro,
          children: K ? oe.strings.submitting : vl ? oe.strings.submit : oe.strings.next
        }
      ) }),
      se && /* @__PURE__ */ N.jsxs("p", { className: "survey-screen__error", role: "alert", children: [
        oe.strings.couldNotSubmit,
        " ",
        se
      ] })
    ] })
  ] }) });
}
const q1 = '.survey-root{--survey-primary: #2563eb;--survey-primary-hover: #1e40af;--survey-primary-contrast: #ffffff;--survey-accent: #f5b60c;--survey-font: system-ui, -apple-system, "Segoe UI", Roboto, sans-serif;font-family:var(--survey-font);color:#111;max-width:640px;margin:0 auto;padding:24px 16px 32px}.survey-chrome{display:flex;align-items:center;min-height:40px;margin-bottom:20px}.survey-chrome__start{flex:none;width:0;margin-inline-end:0;transition:width .25s ease,margin .25s ease}.survey-chrome--back .survey-chrome__start{width:32px;margin-inline-end:8px}.survey-chrome__end{flex:none;margin-inline-start:auto;padding-inline-start:8px}.survey-brand{display:flex;flex:0 1 auto;min-width:0}.survey-brand__logo{height:28px;width:auto;max-width:100%;object-fit:contain}.survey-back{width:40px;height:40px;display:inline-flex;align-items:center;justify-content:center;border:0;border-radius:999px;background:transparent;color:#344054;cursor:pointer;margin-inline-start:-8px;opacity:1;transform:none;transition:opacity .25s ease,transform .25s ease,background-color .15s ease,visibility 0s linear 0s}.survey-back--hidden{opacity:0;transform:translate(-12px);visibility:hidden;pointer-events:none;transition:opacity .2s ease,transform .2s ease,visibility 0s linear .2s}.survey-root[dir=rtl] .survey-back--hidden{transform:translate(12px)}.survey-back:hover{background:#f2f4f7}.survey-back:focus-visible{outline:2px solid var(--survey-primary, #2563eb);outline-offset:1px}.survey-root[dir=rtl] .survey-back__icon{transform:scaleX(-1)}@media(prefers-reduced-motion:reduce){.survey-chrome__start,.survey-back{transition:none}}.survey-locale{position:relative;display:inline-flex;align-items:center;gap:4px;min-height:40px;padding:0 4px;border-radius:6px;color:#6b7280;cursor:pointer}.survey-locale:hover{background:#f2f4f7}.survey-locale:focus-within{outline:2px solid var(--survey-primary, #2563eb);outline-offset:1px}.survey-locale__icon{display:inline-flex;flex:none}.survey-locale__code{font-size:.8rem;font-weight:600;letter-spacing:.02em;line-height:1}.survey-locale__select{position:absolute;top:0;right:0;bottom:0;left:0;width:100%;height:100%;opacity:0;-moz-appearance:none;appearance:none;-webkit-appearance:none;border:0;margin:0;padding:0;font:inherit;cursor:pointer}.survey-screen{display:flex;flex-direction:column;gap:24px}.survey-screen__title{font-size:1.5rem;font-weight:600;margin:0}.survey-screen__description{color:#555;margin:0}.survey-screen__questions{display:flex;flex-direction:column;gap:24px}.survey-screen__actions{display:flex;justify-content:flex-end}.survey-screen__error{color:#b42318;background:#fef3f2;border:1px solid #fecdca;padding:12px 14px;border-radius:8px;margin:0}.survey-question-slot--invalid{border-inline-start:3px solid #b42318;padding-inline-start:10px}.survey-question__required-error{color:#b42318;font-size:.9rem;margin:4px 0 0}.survey-question{display:flex;flex-direction:column;gap:8px}.survey-question__label{font-weight:600;display:block}.survey-question__required{color:#b42318}.survey-question__help{margin:0;color:#666;font-size:.9rem}.survey-question__input{padding:10px 12px;border:1px solid #d0d5dd;border-radius:8px;font:inherit}.survey-question__input:focus-visible{outline:2px solid var(--survey-primary);outline-offset:1px;border-color:var(--survey-primary)}.survey-question--nps{border:none;padding:0;margin:0;display:flex;flex-direction:column;gap:8px}.survey-question__nps-scale{display:flex;gap:6px;flex-wrap:wrap}.survey-question__nps-step{min-width:40px;min-height:40px;padding:8px;border:1px solid #d0d5dd;border-radius:8px;background:#fff;font-weight:500;cursor:pointer}.survey-question__nps-step:hover{background:#f5f7fa}.survey-question__nps-step--selected{background:var(--survey-primary);border-color:var(--survey-primary);color:var(--survey-primary-contrast)}.survey-question__nps-labels{display:flex;justify-content:space-between;color:#555;font-size:.85rem}.survey-question--navlist{gap:12px}.survey-navlist{list-style:none;padding:0;margin:0;display:flex;flex-direction:column;gap:8px}.survey-navlist__row{margin:0}.survey-navlist__button{width:100%;display:flex;align-items:center;justify-content:space-between;padding:14px 16px;border:1px solid #d0d5dd;border-radius:10px;background:#fff;cursor:pointer;font:inherit;text-align:start}.survey-navlist__button:hover{background:#f5f7fa;border-color:var(--survey-primary)}.survey-navlist__button--selected{border-color:var(--survey-primary);box-shadow:inset 0 0 0 1px var(--survey-primary)}.survey-navlist__chevron{font-size:1.5rem;color:#667085}.survey-root[dir=rtl] .survey-navlist__chevron{transform:scaleX(-1)}.survey-navlist__label{font-weight:500}.survey-question__textarea{padding:10px 12px;border:1px solid #d0d5dd;border-radius:8px;font:inherit;resize:vertical;min-height:96px}.survey-question__textarea:focus-visible{outline:2px solid var(--survey-primary);outline-offset:1px;border-color:var(--survey-primary)}.survey-question__number-wrap{display:flex;align-items:center;gap:8px}.survey-question__number-wrap .survey-question__input{flex:1}.survey-question__unit{color:#555;font-size:.9rem}.survey-question__rating-scale{display:flex;gap:4px}.survey-question__rating-star{background:transparent;border:none;cursor:pointer;font-size:1.8rem;line-height:1;color:#d0d5dd;padding:4px}.survey-question__rating-star:hover,.survey-question__rating-star--selected{color:var(--survey-accent)}.survey-question__options{display:flex;flex-direction:column;gap:8px}.survey-question__option{display:flex;align-items:center;gap:8px;padding:8px 12px;border:1px solid #d0d5dd;border-radius:8px;cursor:pointer}.survey-question__option:hover{background:#f5f7fa;border-color:var(--survey-primary)}.survey-question__option input{margin:0}.survey-question__select{padding:10px 12px;border:1px solid #d0d5dd;border-radius:8px;font:inherit;background:#fff}.survey-question__yesno{display:flex;gap:12px}.survey-question__yesno-button{flex:1;padding:14px 16px;border:1px solid #d0d5dd;border-radius:10px;background:#fff;font:inherit;font-weight:500;cursor:pointer}.survey-question__yesno-button:hover{background:#f5f7fa;border-color:var(--survey-primary)}.survey-question__yesno-button--selected{background:var(--survey-primary);border-color:var(--survey-primary);color:var(--survey-primary-contrast)}.survey-question__file{font:inherit}.survey-question__file-name{color:#555;font-size:.9rem;margin:0}.survey-question__signature-canvas{width:100%;max-width:480px;height:auto;aspect-ratio:3 / 1;border:1px dashed #d0d5dd;border-radius:8px;background:#fff;touch-action:none}.survey-question__signature-actions{display:flex;justify-content:flex-start;gap:8px}.survey-question--booking-slot shift-booking-calendar{--shift-font-family: var(--survey-font);--shift-ink: #111;--shift-accent: var(--survey-primary);--shift-on-accent: var(--survey-primary-contrast);--shift-focus: var(--survey-primary);--shift-danger: #b42318}.survey-button{padding:10px 20px;border-radius:8px;border:1px solid transparent;cursor:pointer;font:inherit;font-weight:600}.survey-button--primary{background:var(--survey-primary);color:var(--survey-primary-contrast)}.survey-button--primary:hover{background:var(--survey-primary-hover)}.survey-button--ghost{background:#fff;color:#555;border-color:#d0d5dd}.survey-button--ghost:hover{background:#f5f7fa}.survey-button:disabled{opacity:.5;cursor:not-allowed}.survey-question__options-status{margin:6px 0;font-size:.9rem;color:var(--survey-muted, #667085)}.survey-question--options-error .survey-question__options-status{color:var(--survey-error, #b42318)}.survey-button--retry{background:transparent;color:var(--survey-primary, #4338ca);border:1px solid currentColor;padding:4px 14px;font-size:.85rem}';
var wn, ul, la, kn, cl, ii, gs, fl, Ze, nf, Ba, bs, ei;
class j1 extends HTMLElement {
  constructor() {
    super();
    na(this, Ze);
    /** Schema-mode setter. Assigning this swaps the element into schema mode and
     *  re-renders with the new schema immediately. */
    na(this, wn, null);
    /** Schema-mode submit handler. In API mode the element manages this itself. */
    na(this, ul, null);
    na(this, la, null);
    na(this, kn, null);
    na(this, cl, null);
    na(this, ii, null);
    /** Builder-preview jump target. Assigning a screen id makes the renderer
     *  jump to that screen (answers preserved); the user can navigate freely
     *  afterwards. Mirrors the `active-screen-id` attribute; the property wins
     *  when both are set. */
    na(this, gs, null);
    /** Bump to re-issue a jump to the screen already set on {@link activeScreenId}.
     *  Property-only (no attribute) — it is a transient signal, not page state. */
    na(this, fl, 0);
    na(this, bs, !1);
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
        s.setAttribute("data-shift-survey", ""), s.textContent = q1, this.shadowRoot.appendChild(s);
      }
      Ue(this, kn) || (Lt(this, kn, document.createElement("div")), Ue(this, kn).className = "shift-survey-mount", this.shadowRoot.appendChild(Ue(this, kn))), Ue(this, la) || Lt(this, la, qb.createRoot(Ue(this, kn))), pt(this, Ze, Ba).call(this), pt(this, Ze, nf).call(this);
    }
  }
  disconnectedCallback() {
    queueMicrotask(() => {
      var s;
      if (!(this.isConnected || typeof window > "u")) {
        try {
          (s = Ue(this, la)) == null || s.unmount();
        } catch {
        }
        Lt(this, la, null);
      }
    });
  }
  attributeChangedCallback(s, r, c) {
    r !== c && ((s === "instance-id" || s === "api-base") && (Lt(this, cl, null), Lt(this, ii, null), pt(this, Ze, nf).call(this)), pt(this, Ze, Ba).call(this));
  }
  // ─── Properties ──────────────────────────────────────────────────────────
  get schema() {
    return Ue(this, wn);
  }
  set schema(s) {
    Lt(this, wn, s), pt(this, Ze, Ba).call(this);
  }
  get onSubmit() {
    return Ue(this, ul);
  }
  set onSubmit(s) {
    Lt(this, ul, s), pt(this, Ze, Ba).call(this);
  }
  get activeScreenId() {
    return Ue(this, gs) ?? this.getAttribute("active-screen-id");
  }
  set activeScreenId(s) {
    Lt(this, gs, s), pt(this, Ze, Ba).call(this);
  }
  get activeScreenJumpToken() {
    return Ue(this, fl);
  }
  set activeScreenJumpToken(s) {
    Lt(this, fl, s), pt(this, Ze, Ba).call(this);
  }
}
wn = new WeakMap(), ul = new WeakMap(), la = new WeakMap(), kn = new WeakMap(), cl = new WeakMap(), ii = new WeakMap(), gs = new WeakMap(), fl = new WeakMap(), Ze = new WeakSet(), // ─── Internals ───────────────────────────────────────────────────────────
nf = function() {
  if (Ue(this, wn)) return;
  const s = this.getAttribute("instance-id");
  if (!s) return;
  const r = this.getAttribute("api-base");
  if (!r) return;
  new O0({ baseUrl: r }).fetchSchema(s).then((f) => {
    Lt(this, cl, f), pt(this, Ze, Ba).call(this);
  }).catch((f) => {
    Lt(this, ii, f), pt(this, Ze, ei).call(this, "survey:error", { message: f.message }), pt(this, Ze, Ba).call(this);
  });
}, Ba = function() {
  if (!Ue(this, la)) return;
  const s = this.getAttribute("api-base"), r = this.getAttribute("instance-id"), c = this.getAttribute("locale") ?? void 0, f = this.getAttribute("mode") === "agent", d = this.getAttribute("locale-picker"), h = d === null ? void 0 : d !== "false" && d !== "off", g = Ue(this, wn) ?? Ue(this, cl);
  if (Ue(this, ii) && !g) {
    Ue(this, la).render(
      ee.createElement(
        "div",
        { className: "shift-survey-error", role: "alert" },
        Ue(this, ii).message
      )
    );
    return;
  }
  if (!g) {
    Ue(this, la).render(
      ee.createElement("div", { className: "shift-survey-loading" }, "Loading…")
    );
    return;
  }
  const v = Ue(this, wn) ? Ue(this, ul) ?? ((m) => {
    pt(this, Ze, ei).call(this, "survey:completed", { ...m });
  }) : async (m) => {
    if (!s || !r)
      throw new Error("shift-survey: API mode requires both instance-id and api-base attributes.");
    await new O0({ baseUrl: s }).submitResponse(r, m);
  }, y = this.activeScreenId;
  Ue(this, la).render(
    ee.createElement(M1, {
      schema: g,
      onSubmit: v,
      ...c ? { locale: c } : {},
      ...h === void 0 ? {} : { showLocalePicker: h },
      // Mirror the respondent's pick back onto the element so a host can read it
      // (and so the attribute stays an accurate description of what is showing).
      onLocaleChange: (m) => {
        this.getAttribute("locale") !== m && this.setAttribute("locale", m), pt(this, Ze, ei).call(this, "survey:locale-changed", { locale: m });
      },
      ...y ? { activeScreenId: y, activeScreenJumpToken: Ue(this, fl) } : {},
      // Let the element be the resume key in API mode so two surveys on the
      // same host page don't clobber each other.
      ...r ? { resumeKey: r } : {},
      ...f ? { submissionMeta: { mode: "agent" } } : {},
      // CustomEvents are the web-component's channel; postMessage stays opt-in
      // via iframe auto-detect on the enclosing page (unchanged).
      onScreenChange: (m) => pt(this, Ze, ei).call(this, "survey:screen-changed", { screenId: m }),
      onCompleted: (m) => pt(this, Ze, ei).call(this, "survey:completed", { screenId: m })
    })
  ), Ue(this, bs) || (Lt(this, bs, !0), pt(this, Ze, ei).call(this, "survey:loaded", {}));
}, bs = new WeakMap(), ei = function(s, r) {
  this.dispatchEvent(
    new CustomEvent(s, { detail: r, bubbles: !0, composed: !0 })
  );
};
function R1(n = "shift-survey") {
  typeof window > "u" || typeof customElements > "u" || customElements.get(n) || customElements.define(n, j1);
}
R1();
export {
  j1 as ShiftSurveyElement,
  R1 as registerShiftSurvey
};
//# sourceMappingURL=shift-survey.js.map
