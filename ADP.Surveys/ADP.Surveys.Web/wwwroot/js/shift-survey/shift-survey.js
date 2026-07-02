var Qh = Object.defineProperty;
var ey = (c) => {
  throw TypeError(c);
};
var Xh = (c, r, o) => r in c ? Qh(c, r, { enumerable: !0, configurable: !0, writable: !0, value: o }) : c[r] = o;
var pe = (c, r, o) => Xh(c, typeof r != "symbol" ? r + "" : r, o), Lf = (c, r, o) => r.has(c) || ey("Cannot " + o);
var Rt = (c, r, o) => (Lf(c, r, "read from private field"), o ? o.call(c) : r.get(c)), Fl = (c, r, o) => r.has(c) ? ey("Cannot add the same private member more than once") : r instanceof WeakSet ? r.add(c) : r.set(c, o), Hl = (c, r, o, s) => (Lf(c, r, "write to private field"), s ? s.call(c, o) : r.set(c, o), o), il = (c, r, o) => (Lf(c, r, "access private method"), o);
var Gf = { exports: {} }, I = {};
/**
 * @license React
 * react.production.js
 *
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 */
var ay;
function Zh() {
  if (ay) return I;
  ay = 1;
  var c = Symbol.for("react.transitional.element"), r = Symbol.for("react.portal"), o = Symbol.for("react.fragment"), s = Symbol.for("react.strict_mode"), p = Symbol.for("react.profiler"), v = Symbol.for("react.consumer"), D = Symbol.for("react.context"), U = Symbol.for("react.forward_ref"), O = Symbol.for("react.suspense"), g = Symbol.for("react.memo"), R = Symbol.for("react.lazy"), E = Symbol.for("react.activity"), C = Symbol.iterator;
  function Q(m) {
    return m === null || typeof m != "object" ? null : (m = C && m[C] || m["@@iterator"], typeof m == "function" ? m : null);
  }
  var w = {
    isMounted: function() {
      return !1;
    },
    enqueueForceUpdate: function() {
    },
    enqueueReplaceState: function() {
    },
    enqueueSetState: function() {
    }
  }, W = Object.assign, L = {};
  function V(m, x, j) {
    this.props = m, this.context = x, this.refs = L, this.updater = j || w;
  }
  V.prototype.isReactComponent = {}, V.prototype.setState = function(m, x) {
    if (typeof m != "object" && typeof m != "function" && m != null)
      throw Error(
        "takes an object of state variables to update or a function which returns an object of state variables."
      );
    this.updater.enqueueSetState(this, m, x, "setState");
  }, V.prototype.forceUpdate = function(m) {
    this.updater.enqueueForceUpdate(this, m, "forceUpdate");
  };
  function Tt() {
  }
  Tt.prototype = V.prototype;
  function J(m, x, j) {
    this.props = m, this.context = x, this.refs = L, this.updater = j || w;
  }
  var ut = J.prototype = new Tt();
  ut.constructor = J, W(ut, V.prototype), ut.isPureReactComponent = !0;
  var Zt = Array.isArray;
  function nt() {
  }
  var it = { H: null, A: null, T: null, S: null }, F = Object.prototype.hasOwnProperty;
  function al(m, x, j) {
    var B = j.ref;
    return {
      $$typeof: c,
      type: m,
      key: x,
      ref: B !== void 0 ? B : null,
      props: j
    };
  }
  function Yl(m, x) {
    return al(m.type, x, m.props);
  }
  function vl(m) {
    return typeof m == "object" && m !== null && m.$$typeof === c;
  }
  function Yt(m) {
    var x = { "=": "=0", ":": "=2" };
    return "$" + m.replace(/[=:]/g, function(j) {
      return x[j];
    });
  }
  var Vl = /\/+/g;
  function Ll(m, x) {
    return typeof m == "object" && m !== null && m.key != null ? Yt("" + m.key) : x.toString(36);
  }
  function ul(m) {
    switch (m.status) {
      case "fulfilled":
        return m.value;
      case "rejected":
        throw m.reason;
      default:
        switch (typeof m.status == "string" ? m.then(nt, nt) : (m.status = "pending", m.then(
          function(x) {
            m.status === "pending" && (m.status = "fulfilled", m.value = x);
          },
          function(x) {
            m.status === "pending" && (m.status = "rejected", m.reason = x);
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
  function T(m, x, j, B, $) {
    var lt = typeof m;
    (lt === "undefined" || lt === "boolean") && (m = null);
    var dt = !1;
    if (m === null) dt = !0;
    else
      switch (lt) {
        case "bigint":
        case "string":
        case "number":
          dt = !0;
          break;
        case "object":
          switch (m.$$typeof) {
            case c:
            case r:
              dt = !0;
              break;
            case R:
              return dt = m._init, T(
                dt(m._payload),
                x,
                j,
                B,
                $
              );
          }
      }
    if (dt)
      return $ = $(m), dt = B === "" ? "." + Ll(m, 0) : B, Zt($) ? (j = "", dt != null && (j = dt.replace(Vl, "$&/") + "/"), T($, x, j, "", function(Se) {
        return Se;
      })) : $ != null && (vl($) && ($ = Yl(
        $,
        j + ($.key == null || m && m.key === $.key ? "" : ("" + $.key).replace(
          Vl,
          "$&/"
        ) + "/") + dt
      )), x.push($)), 1;
    dt = 0;
    var Dt = B === "" ? "." : B + ":";
    if (Zt(m))
      for (var Mt = 0; Mt < m.length; Mt++)
        B = m[Mt], lt = Dt + Ll(B, Mt), dt += T(
          B,
          x,
          j,
          lt,
          $
        );
    else if (Mt = Q(m), typeof Mt == "function")
      for (m = Mt.call(m), Mt = 0; !(B = m.next()).done; )
        B = B.value, lt = Dt + Ll(B, Mt++), dt += T(
          B,
          x,
          j,
          lt,
          $
        );
    else if (lt === "object") {
      if (typeof m.then == "function")
        return T(
          ul(m),
          x,
          j,
          B,
          $
        );
      throw x = String(m), Error(
        "Objects are not valid as a React child (found: " + (x === "[object Object]" ? "object with keys {" + Object.keys(m).join(", ") + "}" : x) + "). If you meant to render a collection of children, use an array instead."
      );
    }
    return dt;
  }
  function H(m, x, j) {
    if (m == null) return m;
    var B = [], $ = 0;
    return T(m, B, "", "", function(lt) {
      return x.call(j, lt, $++);
    }), B;
  }
  function Z(m) {
    if (m._status === -1) {
      var x = m._result;
      x = x(), x.then(
        function(j) {
          (m._status === 0 || m._status === -1) && (m._status = 1, m._result = j);
        },
        function(j) {
          (m._status === 0 || m._status === -1) && (m._status = 2, m._result = j);
        }
      ), m._status === -1 && (m._status = 0, m._result = x);
    }
    if (m._status === 1) return m._result.default;
    throw m._result;
  }
  var pt = typeof reportError == "function" ? reportError : function(m) {
    if (typeof window == "object" && typeof window.ErrorEvent == "function") {
      var x = new window.ErrorEvent("error", {
        bubbles: !0,
        cancelable: !0,
        message: typeof m == "object" && m !== null && typeof m.message == "string" ? String(m.message) : String(m),
        error: m
      });
      if (!window.dispatchEvent(x)) return;
    } else if (typeof process == "object" && typeof process.emit == "function") {
      process.emit("uncaughtException", m);
      return;
    }
    console.error(m);
  }, bt = {
    map: H,
    forEach: function(m, x, j) {
      H(
        m,
        function() {
          x.apply(this, arguments);
        },
        j
      );
    },
    count: function(m) {
      var x = 0;
      return H(m, function() {
        x++;
      }), x;
    },
    toArray: function(m) {
      return H(m, function(x) {
        return x;
      }) || [];
    },
    only: function(m) {
      if (!vl(m))
        throw Error(
          "React.Children.only expected to receive a single React element child."
        );
      return m;
    }
  };
  return I.Activity = E, I.Children = bt, I.Component = V, I.Fragment = o, I.Profiler = p, I.PureComponent = J, I.StrictMode = s, I.Suspense = O, I.__CLIENT_INTERNALS_DO_NOT_USE_OR_WARN_USERS_THEY_CANNOT_UPGRADE = it, I.__COMPILER_RUNTIME = {
    __proto__: null,
    c: function(m) {
      return it.H.useMemoCache(m);
    }
  }, I.cache = function(m) {
    return function() {
      return m.apply(null, arguments);
    };
  }, I.cacheSignal = function() {
    return null;
  }, I.cloneElement = function(m, x, j) {
    if (m == null)
      throw Error(
        "The argument must be a React element, but you passed " + m + "."
      );
    var B = W({}, m.props), $ = m.key;
    if (x != null)
      for (lt in x.key !== void 0 && ($ = "" + x.key), x)
        !F.call(x, lt) || lt === "key" || lt === "__self" || lt === "__source" || lt === "ref" && x.ref === void 0 || (B[lt] = x[lt]);
    var lt = arguments.length - 2;
    if (lt === 1) B.children = j;
    else if (1 < lt) {
      for (var dt = Array(lt), Dt = 0; Dt < lt; Dt++)
        dt[Dt] = arguments[Dt + 2];
      B.children = dt;
    }
    return al(m.type, $, B);
  }, I.createContext = function(m) {
    return m = {
      $$typeof: D,
      _currentValue: m,
      _currentValue2: m,
      _threadCount: 0,
      Provider: null,
      Consumer: null
    }, m.Provider = m, m.Consumer = {
      $$typeof: v,
      _context: m
    }, m;
  }, I.createElement = function(m, x, j) {
    var B, $ = {}, lt = null;
    if (x != null)
      for (B in x.key !== void 0 && (lt = "" + x.key), x)
        F.call(x, B) && B !== "key" && B !== "__self" && B !== "__source" && ($[B] = x[B]);
    var dt = arguments.length - 2;
    if (dt === 1) $.children = j;
    else if (1 < dt) {
      for (var Dt = Array(dt), Mt = 0; Mt < dt; Mt++)
        Dt[Mt] = arguments[Mt + 2];
      $.children = Dt;
    }
    if (m && m.defaultProps)
      for (B in dt = m.defaultProps, dt)
        $[B] === void 0 && ($[B] = dt[B]);
    return al(m, lt, $);
  }, I.createRef = function() {
    return { current: null };
  }, I.forwardRef = function(m) {
    return { $$typeof: U, render: m };
  }, I.isValidElement = vl, I.lazy = function(m) {
    return {
      $$typeof: R,
      _payload: { _status: -1, _result: m },
      _init: Z
    };
  }, I.memo = function(m, x) {
    return {
      $$typeof: g,
      type: m,
      compare: x === void 0 ? null : x
    };
  }, I.startTransition = function(m) {
    var x = it.T, j = {};
    it.T = j;
    try {
      var B = m(), $ = it.S;
      $ !== null && $(j, B), typeof B == "object" && B !== null && typeof B.then == "function" && B.then(nt, pt);
    } catch (lt) {
      pt(lt);
    } finally {
      x !== null && j.types !== null && (x.types = j.types), it.T = x;
    }
  }, I.unstable_useCacheRefresh = function() {
    return it.H.useCacheRefresh();
  }, I.use = function(m) {
    return it.H.use(m);
  }, I.useActionState = function(m, x, j) {
    return it.H.useActionState(m, x, j);
  }, I.useCallback = function(m, x) {
    return it.H.useCallback(m, x);
  }, I.useContext = function(m) {
    return it.H.useContext(m);
  }, I.useDebugValue = function() {
  }, I.useDeferredValue = function(m, x) {
    return it.H.useDeferredValue(m, x);
  }, I.useEffect = function(m, x) {
    return it.H.useEffect(m, x);
  }, I.useEffectEvent = function(m) {
    return it.H.useEffectEvent(m);
  }, I.useId = function() {
    return it.H.useId();
  }, I.useImperativeHandle = function(m, x, j) {
    return it.H.useImperativeHandle(m, x, j);
  }, I.useInsertionEffect = function(m, x) {
    return it.H.useInsertionEffect(m, x);
  }, I.useLayoutEffect = function(m, x) {
    return it.H.useLayoutEffect(m, x);
  }, I.useMemo = function(m, x) {
    return it.H.useMemo(m, x);
  }, I.useOptimistic = function(m, x) {
    return it.H.useOptimistic(m, x);
  }, I.useReducer = function(m, x, j) {
    return it.H.useReducer(m, x, j);
  }, I.useRef = function(m) {
    return it.H.useRef(m);
  }, I.useState = function(m) {
    return it.H.useState(m);
  }, I.useSyncExternalStore = function(m, x, j) {
    return it.H.useSyncExternalStore(
      m,
      x,
      j
    );
  }, I.useTransition = function() {
    return it.H.useTransition();
  }, I.version = "19.2.5", I;
}
var uy;
function ts() {
  return uy || (uy = 1, Gf.exports = Zh()), Gf.exports;
}
var P = ts(), Qf = { exports: {} }, ln = {}, Xf = { exports: {} }, Zf = {};
/**
 * @license React
 * scheduler.production.js
 *
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 */
var ny;
function Vh() {
  return ny || (ny = 1, (function(c) {
    function r(T, H) {
      var Z = T.length;
      T.push(H);
      t: for (; 0 < Z; ) {
        var pt = Z - 1 >>> 1, bt = T[pt];
        if (0 < p(bt, H))
          T[pt] = H, T[Z] = bt, Z = pt;
        else break t;
      }
    }
    function o(T) {
      return T.length === 0 ? null : T[0];
    }
    function s(T) {
      if (T.length === 0) return null;
      var H = T[0], Z = T.pop();
      if (Z !== H) {
        T[0] = Z;
        t: for (var pt = 0, bt = T.length, m = bt >>> 1; pt < m; ) {
          var x = 2 * (pt + 1) - 1, j = T[x], B = x + 1, $ = T[B];
          if (0 > p(j, Z))
            B < bt && 0 > p($, j) ? (T[pt] = $, T[B] = Z, pt = B) : (T[pt] = j, T[x] = Z, pt = x);
          else if (B < bt && 0 > p($, Z))
            T[pt] = $, T[B] = Z, pt = B;
          else break t;
        }
      }
      return H;
    }
    function p(T, H) {
      var Z = T.sortIndex - H.sortIndex;
      return Z !== 0 ? Z : T.id - H.id;
    }
    if (c.unstable_now = void 0, typeof performance == "object" && typeof performance.now == "function") {
      var v = performance;
      c.unstable_now = function() {
        return v.now();
      };
    } else {
      var D = Date, U = D.now();
      c.unstable_now = function() {
        return D.now() - U;
      };
    }
    var O = [], g = [], R = 1, E = null, C = 3, Q = !1, w = !1, W = !1, L = !1, V = typeof setTimeout == "function" ? setTimeout : null, Tt = typeof clearTimeout == "function" ? clearTimeout : null, J = typeof setImmediate < "u" ? setImmediate : null;
    function ut(T) {
      for (var H = o(g); H !== null; ) {
        if (H.callback === null) s(g);
        else if (H.startTime <= T)
          s(g), H.sortIndex = H.expirationTime, r(O, H);
        else break;
        H = o(g);
      }
    }
    function Zt(T) {
      if (W = !1, ut(T), !w)
        if (o(O) !== null)
          w = !0, nt || (nt = !0, Yt());
        else {
          var H = o(g);
          H !== null && ul(Zt, H.startTime - T);
        }
    }
    var nt = !1, it = -1, F = 5, al = -1;
    function Yl() {
      return L ? !0 : !(c.unstable_now() - al < F);
    }
    function vl() {
      if (L = !1, nt) {
        var T = c.unstable_now();
        al = T;
        var H = !0;
        try {
          t: {
            w = !1, W && (W = !1, Tt(it), it = -1), Q = !0;
            var Z = C;
            try {
              l: {
                for (ut(T), E = o(O); E !== null && !(E.expirationTime > T && Yl()); ) {
                  var pt = E.callback;
                  if (typeof pt == "function") {
                    E.callback = null, C = E.priorityLevel;
                    var bt = pt(
                      E.expirationTime <= T
                    );
                    if (T = c.unstable_now(), typeof bt == "function") {
                      E.callback = bt, ut(T), H = !0;
                      break l;
                    }
                    E === o(O) && s(O), ut(T);
                  } else s(O);
                  E = o(O);
                }
                if (E !== null) H = !0;
                else {
                  var m = o(g);
                  m !== null && ul(
                    Zt,
                    m.startTime - T
                  ), H = !1;
                }
              }
              break t;
            } finally {
              E = null, C = Z, Q = !1;
            }
            H = void 0;
          }
        } finally {
          H ? Yt() : nt = !1;
        }
      }
    }
    var Yt;
    if (typeof J == "function")
      Yt = function() {
        J(vl);
      };
    else if (typeof MessageChannel < "u") {
      var Vl = new MessageChannel(), Ll = Vl.port2;
      Vl.port1.onmessage = vl, Yt = function() {
        Ll.postMessage(null);
      };
    } else
      Yt = function() {
        V(vl, 0);
      };
    function ul(T, H) {
      it = V(function() {
        T(c.unstable_now());
      }, H);
    }
    c.unstable_IdlePriority = 5, c.unstable_ImmediatePriority = 1, c.unstable_LowPriority = 4, c.unstable_NormalPriority = 3, c.unstable_Profiling = null, c.unstable_UserBlockingPriority = 2, c.unstable_cancelCallback = function(T) {
      T.callback = null;
    }, c.unstable_forceFrameRate = function(T) {
      0 > T || 125 < T ? console.error(
        "forceFrameRate takes a positive int between 0 and 125, forcing frame rates higher than 125 fps is not supported"
      ) : F = 0 < T ? Math.floor(1e3 / T) : 5;
    }, c.unstable_getCurrentPriorityLevel = function() {
      return C;
    }, c.unstable_next = function(T) {
      switch (C) {
        case 1:
        case 2:
        case 3:
          var H = 3;
          break;
        default:
          H = C;
      }
      var Z = C;
      C = H;
      try {
        return T();
      } finally {
        C = Z;
      }
    }, c.unstable_requestPaint = function() {
      L = !0;
    }, c.unstable_runWithPriority = function(T, H) {
      switch (T) {
        case 1:
        case 2:
        case 3:
        case 4:
        case 5:
          break;
        default:
          T = 3;
      }
      var Z = C;
      C = T;
      try {
        return H();
      } finally {
        C = Z;
      }
    }, c.unstable_scheduleCallback = function(T, H, Z) {
      var pt = c.unstable_now();
      switch (typeof Z == "object" && Z !== null ? (Z = Z.delay, Z = typeof Z == "number" && 0 < Z ? pt + Z : pt) : Z = pt, T) {
        case 1:
          var bt = -1;
          break;
        case 2:
          bt = 250;
          break;
        case 5:
          bt = 1073741823;
          break;
        case 4:
          bt = 1e4;
          break;
        default:
          bt = 5e3;
      }
      return bt = Z + bt, T = {
        id: R++,
        callback: H,
        priorityLevel: T,
        startTime: Z,
        expirationTime: bt,
        sortIndex: -1
      }, Z > pt ? (T.sortIndex = Z, r(g, T), o(O) === null && T === o(g) && (W ? (Tt(it), it = -1) : W = !0, ul(Zt, Z - pt))) : (T.sortIndex = bt, r(O, T), w || Q || (w = !0, nt || (nt = !0, Yt()))), T;
    }, c.unstable_shouldYield = Yl, c.unstable_wrapCallback = function(T) {
      var H = C;
      return function() {
        var Z = C;
        C = H;
        try {
          return T.apply(this, arguments);
        } finally {
          C = Z;
        }
      };
    };
  })(Zf)), Zf;
}
var iy;
function Kh() {
  return iy || (iy = 1, Xf.exports = Vh()), Xf.exports;
}
var Vf = { exports: {} }, el = {};
/**
 * @license React
 * react-dom.production.js
 *
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 */
var cy;
function Jh() {
  if (cy) return el;
  cy = 1;
  var c = ts();
  function r(O) {
    var g = "https://react.dev/errors/" + O;
    if (1 < arguments.length) {
      g += "?args[]=" + encodeURIComponent(arguments[1]);
      for (var R = 2; R < arguments.length; R++)
        g += "&args[]=" + encodeURIComponent(arguments[R]);
    }
    return "Minified React error #" + O + "; visit " + g + " for the full message or use the non-minified dev environment for full errors and additional helpful warnings.";
  }
  function o() {
  }
  var s = {
    d: {
      f: o,
      r: function() {
        throw Error(r(522));
      },
      D: o,
      C: o,
      L: o,
      m: o,
      X: o,
      S: o,
      M: o
    },
    p: 0,
    findDOMNode: null
  }, p = Symbol.for("react.portal");
  function v(O, g, R) {
    var E = 3 < arguments.length && arguments[3] !== void 0 ? arguments[3] : null;
    return {
      $$typeof: p,
      key: E == null ? null : "" + E,
      children: O,
      containerInfo: g,
      implementation: R
    };
  }
  var D = c.__CLIENT_INTERNALS_DO_NOT_USE_OR_WARN_USERS_THEY_CANNOT_UPGRADE;
  function U(O, g) {
    if (O === "font") return "";
    if (typeof g == "string")
      return g === "use-credentials" ? g : "";
  }
  return el.__DOM_INTERNALS_DO_NOT_USE_OR_WARN_USERS_THEY_CANNOT_UPGRADE = s, el.createPortal = function(O, g) {
    var R = 2 < arguments.length && arguments[2] !== void 0 ? arguments[2] : null;
    if (!g || g.nodeType !== 1 && g.nodeType !== 9 && g.nodeType !== 11)
      throw Error(r(299));
    return v(O, g, null, R);
  }, el.flushSync = function(O) {
    var g = D.T, R = s.p;
    try {
      if (D.T = null, s.p = 2, O) return O();
    } finally {
      D.T = g, s.p = R, s.d.f();
    }
  }, el.preconnect = function(O, g) {
    typeof O == "string" && (g ? (g = g.crossOrigin, g = typeof g == "string" ? g === "use-credentials" ? g : "" : void 0) : g = null, s.d.C(O, g));
  }, el.prefetchDNS = function(O) {
    typeof O == "string" && s.d.D(O);
  }, el.preinit = function(O, g) {
    if (typeof O == "string" && g && typeof g.as == "string") {
      var R = g.as, E = U(R, g.crossOrigin), C = typeof g.integrity == "string" ? g.integrity : void 0, Q = typeof g.fetchPriority == "string" ? g.fetchPriority : void 0;
      R === "style" ? s.d.S(
        O,
        typeof g.precedence == "string" ? g.precedence : void 0,
        {
          crossOrigin: E,
          integrity: C,
          fetchPriority: Q
        }
      ) : R === "script" && s.d.X(O, {
        crossOrigin: E,
        integrity: C,
        fetchPriority: Q,
        nonce: typeof g.nonce == "string" ? g.nonce : void 0
      });
    }
  }, el.preinitModule = function(O, g) {
    if (typeof O == "string")
      if (typeof g == "object" && g !== null) {
        if (g.as == null || g.as === "script") {
          var R = U(
            g.as,
            g.crossOrigin
          );
          s.d.M(O, {
            crossOrigin: R,
            integrity: typeof g.integrity == "string" ? g.integrity : void 0,
            nonce: typeof g.nonce == "string" ? g.nonce : void 0
          });
        }
      } else g == null && s.d.M(O);
  }, el.preload = function(O, g) {
    if (typeof O == "string" && typeof g == "object" && g !== null && typeof g.as == "string") {
      var R = g.as, E = U(R, g.crossOrigin);
      s.d.L(O, R, {
        crossOrigin: E,
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
  }, el.preloadModule = function(O, g) {
    if (typeof O == "string")
      if (g) {
        var R = U(g.as, g.crossOrigin);
        s.d.m(O, {
          as: typeof g.as == "string" && g.as !== "script" ? g.as : void 0,
          crossOrigin: R,
          integrity: typeof g.integrity == "string" ? g.integrity : void 0
        });
      } else s.d.m(O);
  }, el.requestFormReset = function(O) {
    s.d.r(O);
  }, el.unstable_batchedUpdates = function(O, g) {
    return O(g);
  }, el.useFormState = function(O, g, R) {
    return D.H.useFormState(O, g, R);
  }, el.useFormStatus = function() {
    return D.H.useHostTransitionStatus();
  }, el.version = "19.2.5", el;
}
var fy;
function wh() {
  if (fy) return Vf.exports;
  fy = 1;
  function c() {
    if (!(typeof __REACT_DEVTOOLS_GLOBAL_HOOK__ > "u" || typeof __REACT_DEVTOOLS_GLOBAL_HOOK__.checkDCE != "function"))
      try {
        __REACT_DEVTOOLS_GLOBAL_HOOK__.checkDCE(c);
      } catch (r) {
        console.error(r);
      }
  }
  return c(), Vf.exports = Jh(), Vf.exports;
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
var sy;
function kh() {
  if (sy) return ln;
  sy = 1;
  var c = Kh(), r = ts(), o = wh();
  function s(t) {
    var l = "https://react.dev/errors/" + t;
    if (1 < arguments.length) {
      l += "?args[]=" + encodeURIComponent(arguments[1]);
      for (var e = 2; e < arguments.length; e++)
        l += "&args[]=" + encodeURIComponent(arguments[e]);
    }
    return "Minified React error #" + t + "; visit " + l + " for the full message or use the non-minified dev environment for full errors and additional helpful warnings.";
  }
  function p(t) {
    return !(!t || t.nodeType !== 1 && t.nodeType !== 9 && t.nodeType !== 11);
  }
  function v(t) {
    var l = t, e = t;
    if (t.alternate) for (; l.return; ) l = l.return;
    else {
      t = l;
      do
        l = t, (l.flags & 4098) !== 0 && (e = l.return), t = l.return;
      while (t);
    }
    return l.tag === 3 ? e : null;
  }
  function D(t) {
    if (t.tag === 13) {
      var l = t.memoizedState;
      if (l === null && (t = t.alternate, t !== null && (l = t.memoizedState)), l !== null) return l.dehydrated;
    }
    return null;
  }
  function U(t) {
    if (t.tag === 31) {
      var l = t.memoizedState;
      if (l === null && (t = t.alternate, t !== null && (l = t.memoizedState)), l !== null) return l.dehydrated;
    }
    return null;
  }
  function O(t) {
    if (v(t) !== t)
      throw Error(s(188));
  }
  function g(t) {
    var l = t.alternate;
    if (!l) {
      if (l = v(t), l === null) throw Error(s(188));
      return l !== t ? null : t;
    }
    for (var e = t, a = l; ; ) {
      var u = e.return;
      if (u === null) break;
      var n = u.alternate;
      if (n === null) {
        if (a = u.return, a !== null) {
          e = a;
          continue;
        }
        break;
      }
      if (u.child === n.child) {
        for (n = u.child; n; ) {
          if (n === e) return O(u), t;
          if (n === a) return O(u), l;
          n = n.sibling;
        }
        throw Error(s(188));
      }
      if (e.return !== a.return) e = u, a = n;
      else {
        for (var i = !1, f = u.child; f; ) {
          if (f === e) {
            i = !0, e = u, a = n;
            break;
          }
          if (f === a) {
            i = !0, a = u, e = n;
            break;
          }
          f = f.sibling;
        }
        if (!i) {
          for (f = n.child; f; ) {
            if (f === e) {
              i = !0, e = n, a = u;
              break;
            }
            if (f === a) {
              i = !0, a = n, e = u;
              break;
            }
            f = f.sibling;
          }
          if (!i) throw Error(s(189));
        }
      }
      if (e.alternate !== a) throw Error(s(190));
    }
    if (e.tag !== 3) throw Error(s(188));
    return e.stateNode.current === e ? t : l;
  }
  function R(t) {
    var l = t.tag;
    if (l === 5 || l === 26 || l === 27 || l === 6) return t;
    for (t = t.child; t !== null; ) {
      if (l = R(t), l !== null) return l;
      t = t.sibling;
    }
    return null;
  }
  var E = Object.assign, C = Symbol.for("react.element"), Q = Symbol.for("react.transitional.element"), w = Symbol.for("react.portal"), W = Symbol.for("react.fragment"), L = Symbol.for("react.strict_mode"), V = Symbol.for("react.profiler"), Tt = Symbol.for("react.consumer"), J = Symbol.for("react.context"), ut = Symbol.for("react.forward_ref"), Zt = Symbol.for("react.suspense"), nt = Symbol.for("react.suspense_list"), it = Symbol.for("react.memo"), F = Symbol.for("react.lazy"), al = Symbol.for("react.activity"), Yl = Symbol.for("react.memo_cache_sentinel"), vl = Symbol.iterator;
  function Yt(t) {
    return t === null || typeof t != "object" ? null : (t = vl && t[vl] || t["@@iterator"], typeof t == "function" ? t : null);
  }
  var Vl = Symbol.for("react.client.reference");
  function Ll(t) {
    if (t == null) return null;
    if (typeof t == "function")
      return t.$$typeof === Vl ? null : t.displayName || t.name || null;
    if (typeof t == "string") return t;
    switch (t) {
      case W:
        return "Fragment";
      case V:
        return "Profiler";
      case L:
        return "StrictMode";
      case Zt:
        return "Suspense";
      case nt:
        return "SuspenseList";
      case al:
        return "Activity";
    }
    if (typeof t == "object")
      switch (t.$$typeof) {
        case w:
          return "Portal";
        case J:
          return t.displayName || "Context";
        case Tt:
          return (t._context.displayName || "Context") + ".Consumer";
        case ut:
          var l = t.render;
          return t = t.displayName, t || (t = l.displayName || l.name || "", t = t !== "" ? "ForwardRef(" + t + ")" : "ForwardRef"), t;
        case it:
          return l = t.displayName || null, l !== null ? l : Ll(t.type) || "Memo";
        case F:
          l = t._payload, t = t._init;
          try {
            return Ll(t(l));
          } catch {
          }
      }
    return null;
  }
  var ul = Array.isArray, T = r.__CLIENT_INTERNALS_DO_NOT_USE_OR_WARN_USERS_THEY_CANNOT_UPGRADE, H = o.__DOM_INTERNALS_DO_NOT_USE_OR_WARN_USERS_THEY_CANNOT_UPGRADE, Z = {
    pending: !1,
    data: null,
    method: null,
    action: null
  }, pt = [], bt = -1;
  function m(t) {
    return { current: t };
  }
  function x(t) {
    0 > bt || (t.current = pt[bt], pt[bt] = null, bt--);
  }
  function j(t, l) {
    bt++, pt[bt] = t.current, t.current = l;
  }
  var B = m(null), $ = m(null), lt = m(null), dt = m(null);
  function Dt(t, l) {
    switch (j(lt, l), j($, t), j(B, null), l.nodeType) {
      case 9:
      case 11:
        t = (t = l.documentElement) && (t = t.namespaceURI) ? Td(t) : 0;
        break;
      default:
        if (t = l.tagName, l = l.namespaceURI)
          l = Td(l), t = xd(l, t);
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
    x(B), j(B, t);
  }
  function Mt() {
    x(B), x($), x(lt);
  }
  function Se(t) {
    t.memoizedState !== null && j(dt, t);
    var l = B.current, e = xd(l, t.type);
    l !== e && (j($, t), j(B, e));
  }
  function Kl(t) {
    $.current === t && (x(B), x($)), dt.current === t && (x(dt), Fu._currentValue = Z);
  }
  var cu, ga;
  function Jl(t) {
    if (cu === void 0)
      try {
        throw Error();
      } catch (e) {
        var l = e.stack.trim().match(/\n( *(at )?)/);
        cu = l && l[1] || "", ga = -1 < e.stack.indexOf(`
    at`) ? " (<anonymous>)" : -1 < e.stack.indexOf("@") ? "@unknown:0:0" : "";
      }
    return `
` + cu + t + ga;
  }
  var Pl = !1;
  function fu(t, l) {
    if (!t || Pl) return "";
    Pl = !0;
    var e = Error.prepareStackTrace;
    Error.prepareStackTrace = void 0;
    try {
      var a = {
        DetermineComponentFrameRoot: function() {
          try {
            if (l) {
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
                  var _ = A;
                }
                Reflect.construct(t, [], M);
              } else {
                try {
                  M.call();
                } catch (A) {
                  _ = A;
                }
                t.call(M.prototype);
              }
            } else {
              try {
                throw Error();
              } catch (A) {
                _ = A;
              }
              (M = t()) && typeof M.catch == "function" && M.catch(function() {
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
      var n = a.DetermineComponentFrameRoot(), i = n[0], f = n[1];
      if (i && f) {
        var d = i.split(`
`), S = f.split(`
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
                  var z = `
` + d[a].replace(" at new ", " at ");
                  return t.displayName && z.includes("<anonymous>") && (z = z.replace("<anonymous>", t.displayName)), z;
                }
              while (1 <= a && 0 <= u);
            break;
          }
      }
    } finally {
      Pl = !1, Error.prepareStackTrace = e;
    }
    return (e = t ? t.displayName || t.name : "") ? Jl(e) : "";
  }
  function Ti(t, l) {
    switch (t.tag) {
      case 26:
      case 27:
      case 5:
        return Jl(t.type);
      case 16:
        return Jl("Lazy");
      case 13:
        return t.child !== l && l !== null ? Jl("Suspense Fallback") : Jl("Suspense");
      case 19:
        return Jl("SuspenseList");
      case 0:
      case 15:
        return fu(t.type, !1);
      case 11:
        return fu(t.type.render, !1);
      case 1:
        return fu(t.type, !0);
      case 31:
        return Jl("Activity");
      default:
        return "";
    }
  }
  function nn(t) {
    try {
      var l = "", e = null;
      do
        l += Ti(t, e), e = t, t = t.return;
      while (t);
      return l;
    } catch (a) {
      return `
Error generating stack: ` + a.message + `
` + a.stack;
    }
  }
  var X = Object.prototype.hasOwnProperty, vt = c.unstable_scheduleCallback, Ut = c.unstable_cancelCallback, Jt = c.unstable_shouldYield, tl = c.unstable_requestPaint, ll = c.unstable_now, _y = c.unstable_getCurrentPriorityLevel, es = c.unstable_ImmediatePriority, as = c.unstable_UserBlockingPriority, cn = c.unstable_NormalPriority, Ey = c.unstable_LowPriority, us = c.unstable_IdlePriority, Ay = c.log, zy = c.unstable_setDisableYieldValue, su = null, gl = null;
  function _e(t) {
    if (typeof Ay == "function" && zy(t), gl && typeof gl.setStrictMode == "function")
      try {
        gl.setStrictMode(su, t);
      } catch {
      }
  }
  var pl = Math.clz32 ? Math.clz32 : qy, Ty = Math.log, xy = Math.LN2;
  function qy(t) {
    return t >>>= 0, t === 0 ? 32 : 31 - (Ty(t) / xy | 0) | 0;
  }
  var fn = 256, sn = 262144, rn = 4194304;
  function We(t) {
    var l = t & 42;
    if (l !== 0) return l;
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
  function on(t, l, e) {
    var a = t.pendingLanes;
    if (a === 0) return 0;
    var u = 0, n = t.suspendedLanes, i = t.pingedLanes;
    t = t.warmLanes;
    var f = a & 134217727;
    return f !== 0 ? (a = f & ~n, a !== 0 ? u = We(a) : (i &= f, i !== 0 ? u = We(i) : e || (e = f & ~t, e !== 0 && (u = We(e))))) : (f = a & ~n, f !== 0 ? u = We(f) : i !== 0 ? u = We(i) : e || (e = a & ~t, e !== 0 && (u = We(e)))), u === 0 ? 0 : l !== 0 && l !== u && (l & n) === 0 && (n = u & -u, e = l & -l, n >= e || n === 32 && (e & 4194048) !== 0) ? l : u;
  }
  function ru(t, l) {
    return (t.pendingLanes & ~(t.suspendedLanes & ~t.pingedLanes) & l) === 0;
  }
  function Ny(t, l) {
    switch (t) {
      case 1:
      case 2:
      case 4:
      case 8:
      case 64:
        return l + 250;
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
        return l + 5e3;
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
  function ns() {
    var t = rn;
    return rn <<= 1, (rn & 62914560) === 0 && (rn = 4194304), t;
  }
  function xi(t) {
    for (var l = [], e = 0; 31 > e; e++) l.push(t);
    return l;
  }
  function ou(t, l) {
    t.pendingLanes |= l, l !== 268435456 && (t.suspendedLanes = 0, t.pingedLanes = 0, t.warmLanes = 0);
  }
  function Oy(t, l, e, a, u, n) {
    var i = t.pendingLanes;
    t.pendingLanes = e, t.suspendedLanes = 0, t.pingedLanes = 0, t.warmLanes = 0, t.expiredLanes &= e, t.entangledLanes &= e, t.errorRecoveryDisabledLanes &= e, t.shellSuspendCounter = 0;
    var f = t.entanglements, d = t.expirationTimes, S = t.hiddenUpdates;
    for (e = i & ~e; 0 < e; ) {
      var z = 31 - pl(e), M = 1 << z;
      f[z] = 0, d[z] = -1;
      var _ = S[z];
      if (_ !== null)
        for (S[z] = null, z = 0; z < _.length; z++) {
          var A = _[z];
          A !== null && (A.lane &= -536870913);
        }
      e &= ~M;
    }
    a !== 0 && is(t, a, 0), n !== 0 && u === 0 && t.tag !== 0 && (t.suspendedLanes |= n & ~(i & ~l));
  }
  function is(t, l, e) {
    t.pendingLanes |= l, t.suspendedLanes &= ~l;
    var a = 31 - pl(l);
    t.entangledLanes |= l, t.entanglements[a] = t.entanglements[a] | 1073741824 | e & 261930;
  }
  function cs(t, l) {
    var e = t.entangledLanes |= l;
    for (t = t.entanglements; e; ) {
      var a = 31 - pl(e), u = 1 << a;
      u & l | t[a] & l && (t[a] |= l), e &= ~u;
    }
  }
  function fs(t, l) {
    var e = l & -l;
    return e = (e & 42) !== 0 ? 1 : qi(e), (e & (t.suspendedLanes | l)) !== 0 ? 0 : e;
  }
  function qi(t) {
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
  function Ni(t) {
    return t &= -t, 2 < t ? 8 < t ? (t & 134217727) !== 0 ? 32 : 268435456 : 8 : 2;
  }
  function ss() {
    var t = H.p;
    return t !== 0 ? t : (t = window.event, t === void 0 ? 32 : $d(t.type));
  }
  function rs(t, l) {
    var e = H.p;
    try {
      return H.p = t, l();
    } finally {
      H.p = e;
    }
  }
  var Ee = Math.random().toString(36).slice(2), $t = "__reactFiber$" + Ee, fl = "__reactProps$" + Ee, pa = "__reactContainer$" + Ee, Oi = "__reactEvents$" + Ee, My = "__reactListeners$" + Ee, Dy = "__reactHandles$" + Ee, os = "__reactResources$" + Ee, du = "__reactMarker$" + Ee;
  function Mi(t) {
    delete t[$t], delete t[fl], delete t[Oi], delete t[My], delete t[Dy];
  }
  function ba(t) {
    var l = t[$t];
    if (l) return l;
    for (var e = t.parentNode; e; ) {
      if (l = e[pa] || e[$t]) {
        if (e = l.alternate, l.child !== null || e !== null && e.child !== null)
          for (t = Cd(t); t !== null; ) {
            if (e = t[$t]) return e;
            t = Cd(t);
          }
        return l;
      }
      t = e, e = t.parentNode;
    }
    return null;
  }
  function Sa(t) {
    if (t = t[$t] || t[pa]) {
      var l = t.tag;
      if (l === 5 || l === 6 || l === 13 || l === 31 || l === 26 || l === 27 || l === 3)
        return t;
    }
    return null;
  }
  function yu(t) {
    var l = t.tag;
    if (l === 5 || l === 26 || l === 27 || l === 6) return t.stateNode;
    throw Error(s(33));
  }
  function _a(t) {
    var l = t[os];
    return l || (l = t[os] = { hoistableStyles: /* @__PURE__ */ new Map(), hoistableScripts: /* @__PURE__ */ new Map() }), l;
  }
  function wt(t) {
    t[du] = !0;
  }
  var ds = /* @__PURE__ */ new Set(), ys = {};
  function Fe(t, l) {
    Ea(t, l), Ea(t + "Capture", l);
  }
  function Ea(t, l) {
    for (ys[t] = l, t = 0; t < l.length; t++)
      ds.add(l[t]);
  }
  var Uy = RegExp(
    "^[:A-Z_a-z\\u00C0-\\u00D6\\u00D8-\\u00F6\\u00F8-\\u02FF\\u0370-\\u037D\\u037F-\\u1FFF\\u200C-\\u200D\\u2070-\\u218F\\u2C00-\\u2FEF\\u3001-\\uD7FF\\uF900-\\uFDCF\\uFDF0-\\uFFFD][:A-Z_a-z\\u00C0-\\u00D6\\u00D8-\\u00F6\\u00F8-\\u02FF\\u0370-\\u037D\\u037F-\\u1FFF\\u200C-\\u200D\\u2070-\\u218F\\u2C00-\\u2FEF\\u3001-\\uD7FF\\uF900-\\uFDCF\\uFDF0-\\uFFFD\\-.0-9\\u00B7\\u0300-\\u036F\\u203F-\\u2040]*$"
  ), ms = {}, hs = {};
  function Cy(t) {
    return X.call(hs, t) ? !0 : X.call(ms, t) ? !1 : Uy.test(t) ? hs[t] = !0 : (ms[t] = !0, !1);
  }
  function dn(t, l, e) {
    if (Cy(l))
      if (e === null) t.removeAttribute(l);
      else {
        switch (typeof e) {
          case "undefined":
          case "function":
          case "symbol":
            t.removeAttribute(l);
            return;
          case "boolean":
            var a = l.toLowerCase().slice(0, 5);
            if (a !== "data-" && a !== "aria-") {
              t.removeAttribute(l);
              return;
            }
        }
        t.setAttribute(l, "" + e);
      }
  }
  function yn(t, l, e) {
    if (e === null) t.removeAttribute(l);
    else {
      switch (typeof e) {
        case "undefined":
        case "function":
        case "symbol":
        case "boolean":
          t.removeAttribute(l);
          return;
      }
      t.setAttribute(l, "" + e);
    }
  }
  function te(t, l, e, a) {
    if (a === null) t.removeAttribute(e);
    else {
      switch (typeof a) {
        case "undefined":
        case "function":
        case "symbol":
        case "boolean":
          t.removeAttribute(e);
          return;
      }
      t.setAttributeNS(l, e, "" + a);
    }
  }
  function xl(t) {
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
  function vs(t) {
    var l = t.type;
    return (t = t.nodeName) && t.toLowerCase() === "input" && (l === "checkbox" || l === "radio");
  }
  function jy(t, l, e) {
    var a = Object.getOwnPropertyDescriptor(
      t.constructor.prototype,
      l
    );
    if (!t.hasOwnProperty(l) && typeof a < "u" && typeof a.get == "function" && typeof a.set == "function") {
      var u = a.get, n = a.set;
      return Object.defineProperty(t, l, {
        configurable: !0,
        get: function() {
          return u.call(this);
        },
        set: function(i) {
          e = "" + i, n.call(this, i);
        }
      }), Object.defineProperty(t, l, {
        enumerable: a.enumerable
      }), {
        getValue: function() {
          return e;
        },
        setValue: function(i) {
          e = "" + i;
        },
        stopTracking: function() {
          t._valueTracker = null, delete t[l];
        }
      };
    }
  }
  function Di(t) {
    if (!t._valueTracker) {
      var l = vs(t) ? "checked" : "value";
      t._valueTracker = jy(
        t,
        l,
        "" + t[l]
      );
    }
  }
  function gs(t) {
    if (!t) return !1;
    var l = t._valueTracker;
    if (!l) return !0;
    var e = l.getValue(), a = "";
    return t && (a = vs(t) ? t.checked ? "true" : "false" : t.value), t = a, t !== e ? (l.setValue(t), !0) : !1;
  }
  function mn(t) {
    if (t = t || (typeof document < "u" ? document : void 0), typeof t > "u") return null;
    try {
      return t.activeElement || t.body;
    } catch {
      return t.body;
    }
  }
  var Ry = /[\n"\\]/g;
  function ql(t) {
    return t.replace(
      Ry,
      function(l) {
        return "\\" + l.charCodeAt(0).toString(16) + " ";
      }
    );
  }
  function Ui(t, l, e, a, u, n, i, f) {
    t.name = "", i != null && typeof i != "function" && typeof i != "symbol" && typeof i != "boolean" ? t.type = i : t.removeAttribute("type"), l != null ? i === "number" ? (l === 0 && t.value === "" || t.value != l) && (t.value = "" + xl(l)) : t.value !== "" + xl(l) && (t.value = "" + xl(l)) : i !== "submit" && i !== "reset" || t.removeAttribute("value"), l != null ? Ci(t, i, xl(l)) : e != null ? Ci(t, i, xl(e)) : a != null && t.removeAttribute("value"), u == null && n != null && (t.defaultChecked = !!n), u != null && (t.checked = u && typeof u != "function" && typeof u != "symbol"), f != null && typeof f != "function" && typeof f != "symbol" && typeof f != "boolean" ? t.name = "" + xl(f) : t.removeAttribute("name");
  }
  function ps(t, l, e, a, u, n, i, f) {
    if (n != null && typeof n != "function" && typeof n != "symbol" && typeof n != "boolean" && (t.type = n), l != null || e != null) {
      if (!(n !== "submit" && n !== "reset" || l != null)) {
        Di(t);
        return;
      }
      e = e != null ? "" + xl(e) : "", l = l != null ? "" + xl(l) : e, f || l === t.value || (t.value = l), t.defaultValue = l;
    }
    a = a ?? u, a = typeof a != "function" && typeof a != "symbol" && !!a, t.checked = f ? t.checked : !!a, t.defaultChecked = !!a, i != null && typeof i != "function" && typeof i != "symbol" && typeof i != "boolean" && (t.name = i), Di(t);
  }
  function Ci(t, l, e) {
    l === "number" && mn(t.ownerDocument) === t || t.defaultValue === "" + e || (t.defaultValue = "" + e);
  }
  function Aa(t, l, e, a) {
    if (t = t.options, l) {
      l = {};
      for (var u = 0; u < e.length; u++)
        l["$" + e[u]] = !0;
      for (e = 0; e < t.length; e++)
        u = l.hasOwnProperty("$" + t[e].value), t[e].selected !== u && (t[e].selected = u), u && a && (t[e].defaultSelected = !0);
    } else {
      for (e = "" + xl(e), l = null, u = 0; u < t.length; u++) {
        if (t[u].value === e) {
          t[u].selected = !0, a && (t[u].defaultSelected = !0);
          return;
        }
        l !== null || t[u].disabled || (l = t[u]);
      }
      l !== null && (l.selected = !0);
    }
  }
  function bs(t, l, e) {
    if (l != null && (l = "" + xl(l), l !== t.value && (t.value = l), e == null)) {
      t.defaultValue !== l && (t.defaultValue = l);
      return;
    }
    t.defaultValue = e != null ? "" + xl(e) : "";
  }
  function Ss(t, l, e, a) {
    if (l == null) {
      if (a != null) {
        if (e != null) throw Error(s(92));
        if (ul(a)) {
          if (1 < a.length) throw Error(s(93));
          a = a[0];
        }
        e = a;
      }
      e == null && (e = ""), l = e;
    }
    e = xl(l), t.defaultValue = e, a = t.textContent, a === e && a !== "" && a !== null && (t.value = a), Di(t);
  }
  function za(t, l) {
    if (l) {
      var e = t.firstChild;
      if (e && e === t.lastChild && e.nodeType === 3) {
        e.nodeValue = l;
        return;
      }
    }
    t.textContent = l;
  }
  var Hy = new Set(
    "animationIterationCount aspectRatio borderImageOutset borderImageSlice borderImageWidth boxFlex boxFlexGroup boxOrdinalGroup columnCount columns flex flexGrow flexPositive flexShrink flexNegative flexOrder gridArea gridRow gridRowEnd gridRowSpan gridRowStart gridColumn gridColumnEnd gridColumnSpan gridColumnStart fontWeight lineClamp lineHeight opacity order orphans scale tabSize widows zIndex zoom fillOpacity floodOpacity stopOpacity strokeDasharray strokeDashoffset strokeMiterlimit strokeOpacity strokeWidth MozAnimationIterationCount MozBoxFlex MozBoxFlexGroup MozLineClamp msAnimationIterationCount msFlex msZoom msFlexGrow msFlexNegative msFlexOrder msFlexPositive msFlexShrink msGridColumn msGridColumnSpan msGridRow msGridRowSpan WebkitAnimationIterationCount WebkitBoxFlex WebKitBoxFlexGroup WebkitBoxOrdinalGroup WebkitColumnCount WebkitColumns WebkitFlex WebkitFlexGrow WebkitFlexPositive WebkitFlexShrink WebkitLineClamp".split(
      " "
    )
  );
  function _s(t, l, e) {
    var a = l.indexOf("--") === 0;
    e == null || typeof e == "boolean" || e === "" ? a ? t.setProperty(l, "") : l === "float" ? t.cssFloat = "" : t[l] = "" : a ? t.setProperty(l, e) : typeof e != "number" || e === 0 || Hy.has(l) ? l === "float" ? t.cssFloat = e : t[l] = ("" + e).trim() : t[l] = e + "px";
  }
  function Es(t, l, e) {
    if (l != null && typeof l != "object")
      throw Error(s(62));
    if (t = t.style, e != null) {
      for (var a in e)
        !e.hasOwnProperty(a) || l != null && l.hasOwnProperty(a) || (a.indexOf("--") === 0 ? t.setProperty(a, "") : a === "float" ? t.cssFloat = "" : t[a] = "");
      for (var u in l)
        a = l[u], l.hasOwnProperty(u) && e[u] !== a && _s(t, u, a);
    } else
      for (var n in l)
        l.hasOwnProperty(n) && _s(t, n, l[n]);
  }
  function ji(t) {
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
  var By = /* @__PURE__ */ new Map([
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
  ]), Yy = /^[\u0000-\u001F ]*j[\r\n\t]*a[\r\n\t]*v[\r\n\t]*a[\r\n\t]*s[\r\n\t]*c[\r\n\t]*r[\r\n\t]*i[\r\n\t]*p[\r\n\t]*t[\r\n\t]*:/i;
  function hn(t) {
    return Yy.test("" + t) ? "javascript:throw new Error('React has blocked a javascript: URL as a security precaution.')" : t;
  }
  function le() {
  }
  var Ri = null;
  function Hi(t) {
    return t = t.target || t.srcElement || window, t.correspondingUseElement && (t = t.correspondingUseElement), t.nodeType === 3 ? t.parentNode : t;
  }
  var Ta = null, xa = null;
  function As(t) {
    var l = Sa(t);
    if (l && (t = l.stateNode)) {
      var e = t[fl] || null;
      t: switch (t = l.stateNode, l.type) {
        case "input":
          if (Ui(
            t,
            e.value,
            e.defaultValue,
            e.defaultValue,
            e.checked,
            e.defaultChecked,
            e.type,
            e.name
          ), l = e.name, e.type === "radio" && l != null) {
            for (e = t; e.parentNode; ) e = e.parentNode;
            for (e = e.querySelectorAll(
              'input[name="' + ql(
                "" + l
              ) + '"][type="radio"]'
            ), l = 0; l < e.length; l++) {
              var a = e[l];
              if (a !== t && a.form === t.form) {
                var u = a[fl] || null;
                if (!u) throw Error(s(90));
                Ui(
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
            for (l = 0; l < e.length; l++)
              a = e[l], a.form === t.form && gs(a);
          }
          break t;
        case "textarea":
          bs(t, e.value, e.defaultValue);
          break t;
        case "select":
          l = e.value, l != null && Aa(t, !!e.multiple, l, !1);
      }
    }
  }
  var Bi = !1;
  function zs(t, l, e) {
    if (Bi) return t(l, e);
    Bi = !0;
    try {
      var a = t(l);
      return a;
    } finally {
      if (Bi = !1, (Ta !== null || xa !== null) && (ei(), Ta && (l = Ta, t = xa, xa = Ta = null, As(l), t)))
        for (l = 0; l < t.length; l++) As(t[l]);
    }
  }
  function mu(t, l) {
    var e = t.stateNode;
    if (e === null) return null;
    var a = e[fl] || null;
    if (a === null) return null;
    e = a[l];
    t: switch (l) {
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
    if (e && typeof e != "function")
      throw Error(
        s(231, l, typeof e)
      );
    return e;
  }
  var ee = !(typeof window > "u" || typeof window.document > "u" || typeof window.document.createElement > "u"), Yi = !1;
  if (ee)
    try {
      var hu = {};
      Object.defineProperty(hu, "passive", {
        get: function() {
          Yi = !0;
        }
      }), window.addEventListener("test", hu, hu), window.removeEventListener("test", hu, hu);
    } catch {
      Yi = !1;
    }
  var Ae = null, Li = null, vn = null;
  function Ts() {
    if (vn) return vn;
    var t, l = Li, e = l.length, a, u = "value" in Ae ? Ae.value : Ae.textContent, n = u.length;
    for (t = 0; t < e && l[t] === u[t]; t++) ;
    var i = e - t;
    for (a = 1; a <= i && l[e - a] === u[n - a]; a++) ;
    return vn = u.slice(t, 1 < a ? 1 - a : void 0);
  }
  function gn(t) {
    var l = t.keyCode;
    return "charCode" in t ? (t = t.charCode, t === 0 && l === 13 && (t = 13)) : t = l, t === 10 && (t = 13), 32 <= t || t === 13 ? t : 0;
  }
  function pn() {
    return !0;
  }
  function xs() {
    return !1;
  }
  function sl(t) {
    function l(e, a, u, n, i) {
      this._reactName = e, this._targetInst = u, this.type = a, this.nativeEvent = n, this.target = i, this.currentTarget = null;
      for (var f in t)
        t.hasOwnProperty(f) && (e = t[f], this[f] = e ? e(n) : n[f]);
      return this.isDefaultPrevented = (n.defaultPrevented != null ? n.defaultPrevented : n.returnValue === !1) ? pn : xs, this.isPropagationStopped = xs, this;
    }
    return E(l.prototype, {
      preventDefault: function() {
        this.defaultPrevented = !0;
        var e = this.nativeEvent;
        e && (e.preventDefault ? e.preventDefault() : typeof e.returnValue != "unknown" && (e.returnValue = !1), this.isDefaultPrevented = pn);
      },
      stopPropagation: function() {
        var e = this.nativeEvent;
        e && (e.stopPropagation ? e.stopPropagation() : typeof e.cancelBubble != "unknown" && (e.cancelBubble = !0), this.isPropagationStopped = pn);
      },
      persist: function() {
      },
      isPersistent: pn
    }), l;
  }
  var Ie = {
    eventPhase: 0,
    bubbles: 0,
    cancelable: 0,
    timeStamp: function(t) {
      return t.timeStamp || Date.now();
    },
    defaultPrevented: 0,
    isTrusted: 0
  }, bn = sl(Ie), vu = E({}, Ie, { view: 0, detail: 0 }), Ly = sl(vu), Gi, Qi, gu, Sn = E({}, vu, {
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
    getModifierState: Zi,
    button: 0,
    buttons: 0,
    relatedTarget: function(t) {
      return t.relatedTarget === void 0 ? t.fromElement === t.srcElement ? t.toElement : t.fromElement : t.relatedTarget;
    },
    movementX: function(t) {
      return "movementX" in t ? t.movementX : (t !== gu && (gu && t.type === "mousemove" ? (Gi = t.screenX - gu.screenX, Qi = t.screenY - gu.screenY) : Qi = Gi = 0, gu = t), Gi);
    },
    movementY: function(t) {
      return "movementY" in t ? t.movementY : Qi;
    }
  }), qs = sl(Sn), Gy = E({}, Sn, { dataTransfer: 0 }), Qy = sl(Gy), Xy = E({}, vu, { relatedTarget: 0 }), Xi = sl(Xy), Zy = E({}, Ie, {
    animationName: 0,
    elapsedTime: 0,
    pseudoElement: 0
  }), Vy = sl(Zy), Ky = E({}, Ie, {
    clipboardData: function(t) {
      return "clipboardData" in t ? t.clipboardData : window.clipboardData;
    }
  }), Jy = sl(Ky), wy = E({}, Ie, { data: 0 }), Ns = sl(wy), ky = {
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
  }, $y = {
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
  }, Wy = {
    Alt: "altKey",
    Control: "ctrlKey",
    Meta: "metaKey",
    Shift: "shiftKey"
  };
  function Fy(t) {
    var l = this.nativeEvent;
    return l.getModifierState ? l.getModifierState(t) : (t = Wy[t]) ? !!l[t] : !1;
  }
  function Zi() {
    return Fy;
  }
  var Iy = E({}, vu, {
    key: function(t) {
      if (t.key) {
        var l = ky[t.key] || t.key;
        if (l !== "Unidentified") return l;
      }
      return t.type === "keypress" ? (t = gn(t), t === 13 ? "Enter" : String.fromCharCode(t)) : t.type === "keydown" || t.type === "keyup" ? $y[t.keyCode] || "Unidentified" : "";
    },
    code: 0,
    location: 0,
    ctrlKey: 0,
    shiftKey: 0,
    altKey: 0,
    metaKey: 0,
    repeat: 0,
    locale: 0,
    getModifierState: Zi,
    charCode: function(t) {
      return t.type === "keypress" ? gn(t) : 0;
    },
    keyCode: function(t) {
      return t.type === "keydown" || t.type === "keyup" ? t.keyCode : 0;
    },
    which: function(t) {
      return t.type === "keypress" ? gn(t) : t.type === "keydown" || t.type === "keyup" ? t.keyCode : 0;
    }
  }), Py = sl(Iy), tm = E({}, Sn, {
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
  }), Os = sl(tm), lm = E({}, vu, {
    touches: 0,
    targetTouches: 0,
    changedTouches: 0,
    altKey: 0,
    metaKey: 0,
    ctrlKey: 0,
    shiftKey: 0,
    getModifierState: Zi
  }), em = sl(lm), am = E({}, Ie, {
    propertyName: 0,
    elapsedTime: 0,
    pseudoElement: 0
  }), um = sl(am), nm = E({}, Sn, {
    deltaX: function(t) {
      return "deltaX" in t ? t.deltaX : "wheelDeltaX" in t ? -t.wheelDeltaX : 0;
    },
    deltaY: function(t) {
      return "deltaY" in t ? t.deltaY : "wheelDeltaY" in t ? -t.wheelDeltaY : "wheelDelta" in t ? -t.wheelDelta : 0;
    },
    deltaZ: 0,
    deltaMode: 0
  }), im = sl(nm), cm = E({}, Ie, {
    newState: 0,
    oldState: 0
  }), fm = sl(cm), sm = [9, 13, 27, 32], Vi = ee && "CompositionEvent" in window, pu = null;
  ee && "documentMode" in document && (pu = document.documentMode);
  var rm = ee && "TextEvent" in window && !pu, Ms = ee && (!Vi || pu && 8 < pu && 11 >= pu), Ds = " ", Us = !1;
  function Cs(t, l) {
    switch (t) {
      case "keyup":
        return sm.indexOf(l.keyCode) !== -1;
      case "keydown":
        return l.keyCode !== 229;
      case "keypress":
      case "mousedown":
      case "focusout":
        return !0;
      default:
        return !1;
    }
  }
  function js(t) {
    return t = t.detail, typeof t == "object" && "data" in t ? t.data : null;
  }
  var qa = !1;
  function om(t, l) {
    switch (t) {
      case "compositionend":
        return js(l);
      case "keypress":
        return l.which !== 32 ? null : (Us = !0, Ds);
      case "textInput":
        return t = l.data, t === Ds && Us ? null : t;
      default:
        return null;
    }
  }
  function dm(t, l) {
    if (qa)
      return t === "compositionend" || !Vi && Cs(t, l) ? (t = Ts(), vn = Li = Ae = null, qa = !1, t) : null;
    switch (t) {
      case "paste":
        return null;
      case "keypress":
        if (!(l.ctrlKey || l.altKey || l.metaKey) || l.ctrlKey && l.altKey) {
          if (l.char && 1 < l.char.length)
            return l.char;
          if (l.which) return String.fromCharCode(l.which);
        }
        return null;
      case "compositionend":
        return Ms && l.locale !== "ko" ? null : l.data;
      default:
        return null;
    }
  }
  var ym = {
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
  function Rs(t) {
    var l = t && t.nodeName && t.nodeName.toLowerCase();
    return l === "input" ? !!ym[t.type] : l === "textarea";
  }
  function Hs(t, l, e, a) {
    Ta ? xa ? xa.push(a) : xa = [a] : Ta = a, l = si(l, "onChange"), 0 < l.length && (e = new bn(
      "onChange",
      "change",
      null,
      e,
      a
    ), t.push({ event: e, listeners: l }));
  }
  var bu = null, Su = null;
  function mm(t) {
    bd(t, 0);
  }
  function _n(t) {
    var l = yu(t);
    if (gs(l)) return t;
  }
  function Bs(t, l) {
    if (t === "change") return l;
  }
  var Ys = !1;
  if (ee) {
    var Ki;
    if (ee) {
      var Ji = "oninput" in document;
      if (!Ji) {
        var Ls = document.createElement("div");
        Ls.setAttribute("oninput", "return;"), Ji = typeof Ls.oninput == "function";
      }
      Ki = Ji;
    } else Ki = !1;
    Ys = Ki && (!document.documentMode || 9 < document.documentMode);
  }
  function Gs() {
    bu && (bu.detachEvent("onpropertychange", Qs), Su = bu = null);
  }
  function Qs(t) {
    if (t.propertyName === "value" && _n(Su)) {
      var l = [];
      Hs(
        l,
        Su,
        t,
        Hi(t)
      ), zs(mm, l);
    }
  }
  function hm(t, l, e) {
    t === "focusin" ? (Gs(), bu = l, Su = e, bu.attachEvent("onpropertychange", Qs)) : t === "focusout" && Gs();
  }
  function vm(t) {
    if (t === "selectionchange" || t === "keyup" || t === "keydown")
      return _n(Su);
  }
  function gm(t, l) {
    if (t === "click") return _n(l);
  }
  function pm(t, l) {
    if (t === "input" || t === "change")
      return _n(l);
  }
  function bm(t, l) {
    return t === l && (t !== 0 || 1 / t === 1 / l) || t !== t && l !== l;
  }
  var bl = typeof Object.is == "function" ? Object.is : bm;
  function _u(t, l) {
    if (bl(t, l)) return !0;
    if (typeof t != "object" || t === null || typeof l != "object" || l === null)
      return !1;
    var e = Object.keys(t), a = Object.keys(l);
    if (e.length !== a.length) return !1;
    for (a = 0; a < e.length; a++) {
      var u = e[a];
      if (!X.call(l, u) || !bl(t[u], l[u]))
        return !1;
    }
    return !0;
  }
  function Xs(t) {
    for (; t && t.firstChild; ) t = t.firstChild;
    return t;
  }
  function Zs(t, l) {
    var e = Xs(t);
    t = 0;
    for (var a; e; ) {
      if (e.nodeType === 3) {
        if (a = t + e.textContent.length, t <= l && a >= l)
          return { node: e, offset: l - t };
        t = a;
      }
      t: {
        for (; e; ) {
          if (e.nextSibling) {
            e = e.nextSibling;
            break t;
          }
          e = e.parentNode;
        }
        e = void 0;
      }
      e = Xs(e);
    }
  }
  function Vs(t, l) {
    return t && l ? t === l ? !0 : t && t.nodeType === 3 ? !1 : l && l.nodeType === 3 ? Vs(t, l.parentNode) : "contains" in t ? t.contains(l) : t.compareDocumentPosition ? !!(t.compareDocumentPosition(l) & 16) : !1 : !1;
  }
  function Ks(t) {
    t = t != null && t.ownerDocument != null && t.ownerDocument.defaultView != null ? t.ownerDocument.defaultView : window;
    for (var l = mn(t.document); l instanceof t.HTMLIFrameElement; ) {
      try {
        var e = typeof l.contentWindow.location.href == "string";
      } catch {
        e = !1;
      }
      if (e) t = l.contentWindow;
      else break;
      l = mn(t.document);
    }
    return l;
  }
  function wi(t) {
    var l = t && t.nodeName && t.nodeName.toLowerCase();
    return l && (l === "input" && (t.type === "text" || t.type === "search" || t.type === "tel" || t.type === "url" || t.type === "password") || l === "textarea" || t.contentEditable === "true");
  }
  var Sm = ee && "documentMode" in document && 11 >= document.documentMode, Na = null, ki = null, Eu = null, $i = !1;
  function Js(t, l, e) {
    var a = e.window === e ? e.document : e.nodeType === 9 ? e : e.ownerDocument;
    $i || Na == null || Na !== mn(a) || (a = Na, "selectionStart" in a && wi(a) ? a = { start: a.selectionStart, end: a.selectionEnd } : (a = (a.ownerDocument && a.ownerDocument.defaultView || window).getSelection(), a = {
      anchorNode: a.anchorNode,
      anchorOffset: a.anchorOffset,
      focusNode: a.focusNode,
      focusOffset: a.focusOffset
    }), Eu && _u(Eu, a) || (Eu = a, a = si(ki, "onSelect"), 0 < a.length && (l = new bn(
      "onSelect",
      "select",
      null,
      l,
      e
    ), t.push({ event: l, listeners: a }), l.target = Na)));
  }
  function Pe(t, l) {
    var e = {};
    return e[t.toLowerCase()] = l.toLowerCase(), e["Webkit" + t] = "webkit" + l, e["Moz" + t] = "moz" + l, e;
  }
  var Oa = {
    animationend: Pe("Animation", "AnimationEnd"),
    animationiteration: Pe("Animation", "AnimationIteration"),
    animationstart: Pe("Animation", "AnimationStart"),
    transitionrun: Pe("Transition", "TransitionRun"),
    transitionstart: Pe("Transition", "TransitionStart"),
    transitioncancel: Pe("Transition", "TransitionCancel"),
    transitionend: Pe("Transition", "TransitionEnd")
  }, Wi = {}, ws = {};
  ee && (ws = document.createElement("div").style, "AnimationEvent" in window || (delete Oa.animationend.animation, delete Oa.animationiteration.animation, delete Oa.animationstart.animation), "TransitionEvent" in window || delete Oa.transitionend.transition);
  function ta(t) {
    if (Wi[t]) return Wi[t];
    if (!Oa[t]) return t;
    var l = Oa[t], e;
    for (e in l)
      if (l.hasOwnProperty(e) && e in ws)
        return Wi[t] = l[e];
    return t;
  }
  var ks = ta("animationend"), $s = ta("animationiteration"), Ws = ta("animationstart"), _m = ta("transitionrun"), Em = ta("transitionstart"), Am = ta("transitioncancel"), Fs = ta("transitionend"), Is = /* @__PURE__ */ new Map(), Fi = "abort auxClick beforeToggle cancel canPlay canPlayThrough click close contextMenu copy cut drag dragEnd dragEnter dragExit dragLeave dragOver dragStart drop durationChange emptied encrypted ended error gotPointerCapture input invalid keyDown keyPress keyUp load loadedData loadedMetadata loadStart lostPointerCapture mouseDown mouseMove mouseOut mouseOver mouseUp paste pause play playing pointerCancel pointerDown pointerMove pointerOut pointerOver pointerUp progress rateChange reset resize seeked seeking stalled submit suspend timeUpdate touchCancel touchEnd touchStart volumeChange scroll toggle touchMove waiting wheel".split(
    " "
  );
  Fi.push("scrollEnd");
  function Gl(t, l) {
    Is.set(t, l), Fe(l, [t]);
  }
  var En = typeof reportError == "function" ? reportError : function(t) {
    if (typeof window == "object" && typeof window.ErrorEvent == "function") {
      var l = new window.ErrorEvent("error", {
        bubbles: !0,
        cancelable: !0,
        message: typeof t == "object" && t !== null && typeof t.message == "string" ? String(t.message) : String(t),
        error: t
      });
      if (!window.dispatchEvent(l)) return;
    } else if (typeof process == "object" && typeof process.emit == "function") {
      process.emit("uncaughtException", t);
      return;
    }
    console.error(t);
  }, Nl = [], Ma = 0, Ii = 0;
  function An() {
    for (var t = Ma, l = Ii = Ma = 0; l < t; ) {
      var e = Nl[l];
      Nl[l++] = null;
      var a = Nl[l];
      Nl[l++] = null;
      var u = Nl[l];
      Nl[l++] = null;
      var n = Nl[l];
      if (Nl[l++] = null, a !== null && u !== null) {
        var i = a.pending;
        i === null ? u.next = u : (u.next = i.next, i.next = u), a.pending = u;
      }
      n !== 0 && Ps(e, u, n);
    }
  }
  function zn(t, l, e, a) {
    Nl[Ma++] = t, Nl[Ma++] = l, Nl[Ma++] = e, Nl[Ma++] = a, Ii |= a, t.lanes |= a, t = t.alternate, t !== null && (t.lanes |= a);
  }
  function Pi(t, l, e, a) {
    return zn(t, l, e, a), Tn(t);
  }
  function la(t, l) {
    return zn(t, null, null, l), Tn(t);
  }
  function Ps(t, l, e) {
    t.lanes |= e;
    var a = t.alternate;
    a !== null && (a.lanes |= e);
    for (var u = !1, n = t.return; n !== null; )
      n.childLanes |= e, a = n.alternate, a !== null && (a.childLanes |= e), n.tag === 22 && (t = n.stateNode, t === null || t._visibility & 1 || (u = !0)), t = n, n = n.return;
    return t.tag === 3 ? (n = t.stateNode, u && l !== null && (u = 31 - pl(e), t = n.hiddenUpdates, a = t[u], a === null ? t[u] = [l] : a.push(l), l.lane = e | 536870912), n) : null;
  }
  function Tn(t) {
    if (50 < Vu)
      throw Vu = 0, sf = null, Error(s(185));
    for (var l = t.return; l !== null; )
      t = l, l = t.return;
    return t.tag === 3 ? t.stateNode : null;
  }
  var Da = {};
  function zm(t, l, e, a) {
    this.tag = t, this.key = e, this.sibling = this.child = this.return = this.stateNode = this.type = this.elementType = null, this.index = 0, this.refCleanup = this.ref = null, this.pendingProps = l, this.dependencies = this.memoizedState = this.updateQueue = this.memoizedProps = null, this.mode = a, this.subtreeFlags = this.flags = 0, this.deletions = null, this.childLanes = this.lanes = 0, this.alternate = null;
  }
  function Sl(t, l, e, a) {
    return new zm(t, l, e, a);
  }
  function tc(t) {
    return t = t.prototype, !(!t || !t.isReactComponent);
  }
  function ae(t, l) {
    var e = t.alternate;
    return e === null ? (e = Sl(
      t.tag,
      l,
      t.key,
      t.mode
    ), e.elementType = t.elementType, e.type = t.type, e.stateNode = t.stateNode, e.alternate = t, t.alternate = e) : (e.pendingProps = l, e.type = t.type, e.flags = 0, e.subtreeFlags = 0, e.deletions = null), e.flags = t.flags & 65011712, e.childLanes = t.childLanes, e.lanes = t.lanes, e.child = t.child, e.memoizedProps = t.memoizedProps, e.memoizedState = t.memoizedState, e.updateQueue = t.updateQueue, l = t.dependencies, e.dependencies = l === null ? null : { lanes: l.lanes, firstContext: l.firstContext }, e.sibling = t.sibling, e.index = t.index, e.ref = t.ref, e.refCleanup = t.refCleanup, e;
  }
  function tr(t, l) {
    t.flags &= 65011714;
    var e = t.alternate;
    return e === null ? (t.childLanes = 0, t.lanes = l, t.child = null, t.subtreeFlags = 0, t.memoizedProps = null, t.memoizedState = null, t.updateQueue = null, t.dependencies = null, t.stateNode = null) : (t.childLanes = e.childLanes, t.lanes = e.lanes, t.child = e.child, t.subtreeFlags = 0, t.deletions = null, t.memoizedProps = e.memoizedProps, t.memoizedState = e.memoizedState, t.updateQueue = e.updateQueue, t.type = e.type, l = e.dependencies, t.dependencies = l === null ? null : {
      lanes: l.lanes,
      firstContext: l.firstContext
    }), t;
  }
  function xn(t, l, e, a, u, n) {
    var i = 0;
    if (a = t, typeof t == "function") tc(t) && (i = 1);
    else if (typeof t == "string")
      i = Oh(
        t,
        e,
        B.current
      ) ? 26 : t === "html" || t === "head" || t === "body" ? 27 : 5;
    else
      t: switch (t) {
        case al:
          return t = Sl(31, e, l, u), t.elementType = al, t.lanes = n, t;
        case W:
          return ea(e.children, u, n, l);
        case L:
          i = 8, u |= 24;
          break;
        case V:
          return t = Sl(12, e, l, u | 2), t.elementType = V, t.lanes = n, t;
        case Zt:
          return t = Sl(13, e, l, u), t.elementType = Zt, t.lanes = n, t;
        case nt:
          return t = Sl(19, e, l, u), t.elementType = nt, t.lanes = n, t;
        default:
          if (typeof t == "object" && t !== null)
            switch (t.$$typeof) {
              case J:
                i = 10;
                break t;
              case Tt:
                i = 9;
                break t;
              case ut:
                i = 11;
                break t;
              case it:
                i = 14;
                break t;
              case F:
                i = 16, a = null;
                break t;
            }
          i = 29, e = Error(
            s(130, t === null ? "null" : typeof t, "")
          ), a = null;
      }
    return l = Sl(i, e, l, u), l.elementType = t, l.type = a, l.lanes = n, l;
  }
  function ea(t, l, e, a) {
    return t = Sl(7, t, a, l), t.lanes = e, t;
  }
  function lc(t, l, e) {
    return t = Sl(6, t, null, l), t.lanes = e, t;
  }
  function lr(t) {
    var l = Sl(18, null, null, 0);
    return l.stateNode = t, l;
  }
  function ec(t, l, e) {
    return l = Sl(
      4,
      t.children !== null ? t.children : [],
      t.key,
      l
    ), l.lanes = e, l.stateNode = {
      containerInfo: t.containerInfo,
      pendingChildren: null,
      implementation: t.implementation
    }, l;
  }
  var er = /* @__PURE__ */ new WeakMap();
  function Ol(t, l) {
    if (typeof t == "object" && t !== null) {
      var e = er.get(t);
      return e !== void 0 ? e : (l = {
        value: t,
        source: l,
        stack: nn(l)
      }, er.set(t, l), l);
    }
    return {
      value: t,
      source: l,
      stack: nn(l)
    };
  }
  var Ua = [], Ca = 0, qn = null, Au = 0, Ml = [], Dl = 0, ze = null, wl = 1, kl = "";
  function ue(t, l) {
    Ua[Ca++] = Au, Ua[Ca++] = qn, qn = t, Au = l;
  }
  function ar(t, l, e) {
    Ml[Dl++] = wl, Ml[Dl++] = kl, Ml[Dl++] = ze, ze = t;
    var a = wl;
    t = kl;
    var u = 32 - pl(a) - 1;
    a &= ~(1 << u), e += 1;
    var n = 32 - pl(l) + u;
    if (30 < n) {
      var i = u - u % 5;
      n = (a & (1 << i) - 1).toString(32), a >>= i, u -= i, wl = 1 << 32 - pl(l) + u | e << u | a, kl = n + t;
    } else
      wl = 1 << n | e << u | a, kl = t;
  }
  function ac(t) {
    t.return !== null && (ue(t, 1), ar(t, 1, 0));
  }
  function uc(t) {
    for (; t === qn; )
      qn = Ua[--Ca], Ua[Ca] = null, Au = Ua[--Ca], Ua[Ca] = null;
    for (; t === ze; )
      ze = Ml[--Dl], Ml[Dl] = null, kl = Ml[--Dl], Ml[Dl] = null, wl = Ml[--Dl], Ml[Dl] = null;
  }
  function ur(t, l) {
    Ml[Dl++] = wl, Ml[Dl++] = kl, Ml[Dl++] = ze, wl = l.id, kl = l.overflow, ze = t;
  }
  var Wt = null, qt = null, ot = !1, Te = null, Ul = !1, nc = Error(s(519));
  function xe(t) {
    var l = Error(
      s(
        418,
        1 < arguments.length && arguments[1] !== void 0 && arguments[1] ? "text" : "HTML",
        ""
      )
    );
    throw zu(Ol(l, t)), nc;
  }
  function nr(t) {
    var l = t.stateNode, e = t.type, a = t.memoizedProps;
    switch (l[$t] = t, l[fl] = a, e) {
      case "dialog":
        ft("cancel", l), ft("close", l);
        break;
      case "iframe":
      case "object":
      case "embed":
        ft("load", l);
        break;
      case "video":
      case "audio":
        for (e = 0; e < Ju.length; e++)
          ft(Ju[e], l);
        break;
      case "source":
        ft("error", l);
        break;
      case "img":
      case "image":
      case "link":
        ft("error", l), ft("load", l);
        break;
      case "details":
        ft("toggle", l);
        break;
      case "input":
        ft("invalid", l), ps(
          l,
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
        ft("invalid", l);
        break;
      case "textarea":
        ft("invalid", l), Ss(l, a.value, a.defaultValue, a.children);
    }
    e = a.children, typeof e != "string" && typeof e != "number" && typeof e != "bigint" || l.textContent === "" + e || a.suppressHydrationWarning === !0 || Ad(l.textContent, e) ? (a.popover != null && (ft("beforetoggle", l), ft("toggle", l)), a.onScroll != null && ft("scroll", l), a.onScrollEnd != null && ft("scrollend", l), a.onClick != null && (l.onclick = le), l = !0) : l = !1, l || xe(t, !0);
  }
  function ir(t) {
    for (Wt = t.return; Wt; )
      switch (Wt.tag) {
        case 5:
        case 31:
        case 13:
          Ul = !1;
          return;
        case 27:
        case 3:
          Ul = !0;
          return;
        default:
          Wt = Wt.return;
      }
  }
  function ja(t) {
    if (t !== Wt) return !1;
    if (!ot) return ir(t), ot = !0, !1;
    var l = t.tag, e;
    if ((e = l !== 3 && l !== 27) && ((e = l === 5) && (e = t.type, e = !(e !== "form" && e !== "button") || zf(t.type, t.memoizedProps)), e = !e), e && qt && xe(t), ir(t), l === 13) {
      if (t = t.memoizedState, t = t !== null ? t.dehydrated : null, !t) throw Error(s(317));
      qt = Ud(t);
    } else if (l === 31) {
      if (t = t.memoizedState, t = t !== null ? t.dehydrated : null, !t) throw Error(s(317));
      qt = Ud(t);
    } else
      l === 27 ? (l = qt, Ge(t.type) ? (t = Of, Of = null, qt = t) : qt = l) : qt = Wt ? jl(t.stateNode.nextSibling) : null;
    return !0;
  }
  function aa() {
    qt = Wt = null, ot = !1;
  }
  function ic() {
    var t = Te;
    return t !== null && (yl === null ? yl = t : yl.push.apply(
      yl,
      t
    ), Te = null), t;
  }
  function zu(t) {
    Te === null ? Te = [t] : Te.push(t);
  }
  var cc = m(null), ua = null, ne = null;
  function qe(t, l, e) {
    j(cc, l._currentValue), l._currentValue = e;
  }
  function ie(t) {
    t._currentValue = cc.current, x(cc);
  }
  function fc(t, l, e) {
    for (; t !== null; ) {
      var a = t.alternate;
      if ((t.childLanes & l) !== l ? (t.childLanes |= l, a !== null && (a.childLanes |= l)) : a !== null && (a.childLanes & l) !== l && (a.childLanes |= l), t === e) break;
      t = t.return;
    }
  }
  function sc(t, l, e, a) {
    var u = t.child;
    for (u !== null && (u.return = t); u !== null; ) {
      var n = u.dependencies;
      if (n !== null) {
        var i = u.child;
        n = n.firstContext;
        t: for (; n !== null; ) {
          var f = n;
          n = u;
          for (var d = 0; d < l.length; d++)
            if (f.context === l[d]) {
              n.lanes |= e, f = n.alternate, f !== null && (f.lanes |= e), fc(
                n.return,
                e,
                t
              ), a || (i = null);
              break t;
            }
          n = f.next;
        }
      } else if (u.tag === 18) {
        if (i = u.return, i === null) throw Error(s(341));
        i.lanes |= e, n = i.alternate, n !== null && (n.lanes |= e), fc(i, e, t), i = null;
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
  function Ra(t, l, e, a) {
    t = null;
    for (var u = l, n = !1; u !== null; ) {
      if (!n) {
        if ((u.flags & 524288) !== 0) n = !0;
        else if ((u.flags & 262144) !== 0) break;
      }
      if (u.tag === 10) {
        var i = u.alternate;
        if (i === null) throw Error(s(387));
        if (i = i.memoizedProps, i !== null) {
          var f = u.type;
          bl(u.pendingProps.value, i.value) || (t !== null ? t.push(f) : t = [f]);
        }
      } else if (u === dt.current) {
        if (i = u.alternate, i === null) throw Error(s(387));
        i.memoizedState.memoizedState !== u.memoizedState.memoizedState && (t !== null ? t.push(Fu) : t = [Fu]);
      }
      u = u.return;
    }
    t !== null && sc(
      l,
      t,
      e,
      a
    ), l.flags |= 262144;
  }
  function Nn(t) {
    for (t = t.firstContext; t !== null; ) {
      if (!bl(
        t.context._currentValue,
        t.memoizedValue
      ))
        return !0;
      t = t.next;
    }
    return !1;
  }
  function na(t) {
    ua = t, ne = null, t = t.dependencies, t !== null && (t.firstContext = null);
  }
  function Ft(t) {
    return cr(ua, t);
  }
  function On(t, l) {
    return ua === null && na(t), cr(t, l);
  }
  function cr(t, l) {
    var e = l._currentValue;
    if (l = { context: l, memoizedValue: e, next: null }, ne === null) {
      if (t === null) throw Error(s(308));
      ne = l, t.dependencies = { lanes: 0, firstContext: l }, t.flags |= 524288;
    } else ne = ne.next = l;
    return e;
  }
  var Tm = typeof AbortController < "u" ? AbortController : function() {
    var t = [], l = this.signal = {
      aborted: !1,
      addEventListener: function(e, a) {
        t.push(a);
      }
    };
    this.abort = function() {
      l.aborted = !0, t.forEach(function(e) {
        return e();
      });
    };
  }, xm = c.unstable_scheduleCallback, qm = c.unstable_NormalPriority, Lt = {
    $$typeof: J,
    Consumer: null,
    Provider: null,
    _currentValue: null,
    _currentValue2: null,
    _threadCount: 0
  };
  function rc() {
    return {
      controller: new Tm(),
      data: /* @__PURE__ */ new Map(),
      refCount: 0
    };
  }
  function Tu(t) {
    t.refCount--, t.refCount === 0 && xm(qm, function() {
      t.controller.abort();
    });
  }
  var xu = null, oc = 0, Ha = 0, Ba = null;
  function Nm(t, l) {
    if (xu === null) {
      var e = xu = [];
      oc = 0, Ha = hf(), Ba = {
        status: "pending",
        value: void 0,
        then: function(a) {
          e.push(a);
        }
      };
    }
    return oc++, l.then(fr, fr), l;
  }
  function fr() {
    if (--oc === 0 && xu !== null) {
      Ba !== null && (Ba.status = "fulfilled");
      var t = xu;
      xu = null, Ha = 0, Ba = null;
      for (var l = 0; l < t.length; l++) (0, t[l])();
    }
  }
  function Om(t, l) {
    var e = [], a = {
      status: "pending",
      value: null,
      reason: null,
      then: function(u) {
        e.push(u);
      }
    };
    return t.then(
      function() {
        a.status = "fulfilled", a.value = l;
        for (var u = 0; u < e.length; u++) (0, e[u])(l);
      },
      function(u) {
        for (a.status = "rejected", a.reason = u, u = 0; u < e.length; u++)
          (0, e[u])(void 0);
      }
    ), a;
  }
  var sr = T.S;
  T.S = function(t, l) {
    wo = ll(), typeof l == "object" && l !== null && typeof l.then == "function" && Nm(t, l), sr !== null && sr(t, l);
  };
  var ia = m(null);
  function dc() {
    var t = ia.current;
    return t !== null ? t : xt.pooledCache;
  }
  function Mn(t, l) {
    l === null ? j(ia, ia.current) : j(ia, l.pool);
  }
  function rr() {
    var t = dc();
    return t === null ? null : { parent: Lt._currentValue, pool: t };
  }
  var Ya = Error(s(460)), yc = Error(s(474)), Dn = Error(s(542)), Un = { then: function() {
  } };
  function or(t) {
    return t = t.status, t === "fulfilled" || t === "rejected";
  }
  function dr(t, l, e) {
    switch (e = t[e], e === void 0 ? t.push(l) : e !== l && (l.then(le, le), l = e), l.status) {
      case "fulfilled":
        return l.value;
      case "rejected":
        throw t = l.reason, mr(t), t;
      default:
        if (typeof l.status == "string") l.then(le, le);
        else {
          if (t = xt, t !== null && 100 < t.shellSuspendCounter)
            throw Error(s(482));
          t = l, t.status = "pending", t.then(
            function(a) {
              if (l.status === "pending") {
                var u = l;
                u.status = "fulfilled", u.value = a;
              }
            },
            function(a) {
              if (l.status === "pending") {
                var u = l;
                u.status = "rejected", u.reason = a;
              }
            }
          );
        }
        switch (l.status) {
          case "fulfilled":
            return l.value;
          case "rejected":
            throw t = l.reason, mr(t), t;
        }
        throw fa = l, Ya;
    }
  }
  function ca(t) {
    try {
      var l = t._init;
      return l(t._payload);
    } catch (e) {
      throw e !== null && typeof e == "object" && typeof e.then == "function" ? (fa = e, Ya) : e;
    }
  }
  var fa = null;
  function yr() {
    if (fa === null) throw Error(s(459));
    var t = fa;
    return fa = null, t;
  }
  function mr(t) {
    if (t === Ya || t === Dn)
      throw Error(s(483));
  }
  var La = null, qu = 0;
  function Cn(t) {
    var l = qu;
    return qu += 1, La === null && (La = []), dr(La, t, l);
  }
  function Nu(t, l) {
    l = l.props.ref, t.ref = l !== void 0 ? l : null;
  }
  function jn(t, l) {
    throw l.$$typeof === C ? Error(s(525)) : (t = Object.prototype.toString.call(l), Error(
      s(
        31,
        t === "[object Object]" ? "object with keys {" + Object.keys(l).join(", ") + "}" : t
      )
    ));
  }
  function hr(t) {
    function l(h, y) {
      if (t) {
        var b = h.deletions;
        b === null ? (h.deletions = [y], h.flags |= 16) : b.push(y);
      }
    }
    function e(h, y) {
      if (!t) return null;
      for (; y !== null; )
        l(h, y), y = y.sibling;
      return null;
    }
    function a(h) {
      for (var y = /* @__PURE__ */ new Map(); h !== null; )
        h.key !== null ? y.set(h.key, h) : y.set(h.index, h), h = h.sibling;
      return y;
    }
    function u(h, y) {
      return h = ae(h, y), h.index = 0, h.sibling = null, h;
    }
    function n(h, y, b) {
      return h.index = b, t ? (b = h.alternate, b !== null ? (b = b.index, b < y ? (h.flags |= 67108866, y) : b) : (h.flags |= 67108866, y)) : (h.flags |= 1048576, y);
    }
    function i(h) {
      return t && h.alternate === null && (h.flags |= 67108866), h;
    }
    function f(h, y, b, q) {
      return y === null || y.tag !== 6 ? (y = lc(b, h.mode, q), y.return = h, y) : (y = u(y, b), y.return = h, y);
    }
    function d(h, y, b, q) {
      var K = b.type;
      return K === W ? z(
        h,
        y,
        b.props.children,
        q,
        b.key
      ) : y !== null && (y.elementType === K || typeof K == "object" && K !== null && K.$$typeof === F && ca(K) === y.type) ? (y = u(y, b.props), Nu(y, b), y.return = h, y) : (y = xn(
        b.type,
        b.key,
        b.props,
        null,
        h.mode,
        q
      ), Nu(y, b), y.return = h, y);
    }
    function S(h, y, b, q) {
      return y === null || y.tag !== 4 || y.stateNode.containerInfo !== b.containerInfo || y.stateNode.implementation !== b.implementation ? (y = ec(b, h.mode, q), y.return = h, y) : (y = u(y, b.children || []), y.return = h, y);
    }
    function z(h, y, b, q, K) {
      return y === null || y.tag !== 7 ? (y = ea(
        b,
        h.mode,
        q,
        K
      ), y.return = h, y) : (y = u(y, b), y.return = h, y);
    }
    function M(h, y, b) {
      if (typeof y == "string" && y !== "" || typeof y == "number" || typeof y == "bigint")
        return y = lc(
          "" + y,
          h.mode,
          b
        ), y.return = h, y;
      if (typeof y == "object" && y !== null) {
        switch (y.$$typeof) {
          case Q:
            return b = xn(
              y.type,
              y.key,
              y.props,
              null,
              h.mode,
              b
            ), Nu(b, y), b.return = h, b;
          case w:
            return y = ec(
              y,
              h.mode,
              b
            ), y.return = h, y;
          case F:
            return y = ca(y), M(h, y, b);
        }
        if (ul(y) || Yt(y))
          return y = ea(
            y,
            h.mode,
            b,
            null
          ), y.return = h, y;
        if (typeof y.then == "function")
          return M(h, Cn(y), b);
        if (y.$$typeof === J)
          return M(
            h,
            On(h, y),
            b
          );
        jn(h, y);
      }
      return null;
    }
    function _(h, y, b, q) {
      var K = y !== null ? y.key : null;
      if (typeof b == "string" && b !== "" || typeof b == "number" || typeof b == "bigint")
        return K !== null ? null : f(h, y, "" + b, q);
      if (typeof b == "object" && b !== null) {
        switch (b.$$typeof) {
          case Q:
            return b.key === K ? d(h, y, b, q) : null;
          case w:
            return b.key === K ? S(h, y, b, q) : null;
          case F:
            return b = ca(b), _(h, y, b, q);
        }
        if (ul(b) || Yt(b))
          return K !== null ? null : z(h, y, b, q, null);
        if (typeof b.then == "function")
          return _(
            h,
            y,
            Cn(b),
            q
          );
        if (b.$$typeof === J)
          return _(
            h,
            y,
            On(h, b),
            q
          );
        jn(h, b);
      }
      return null;
    }
    function A(h, y, b, q, K) {
      if (typeof q == "string" && q !== "" || typeof q == "number" || typeof q == "bigint")
        return h = h.get(b) || null, f(y, h, "" + q, K);
      if (typeof q == "object" && q !== null) {
        switch (q.$$typeof) {
          case Q:
            return h = h.get(
              q.key === null ? b : q.key
            ) || null, d(y, h, q, K);
          case w:
            return h = h.get(
              q.key === null ? b : q.key
            ) || null, S(y, h, q, K);
          case F:
            return q = ca(q), A(
              h,
              y,
              b,
              q,
              K
            );
        }
        if (ul(q) || Yt(q))
          return h = h.get(b) || null, z(y, h, q, K, null);
        if (typeof q.then == "function")
          return A(
            h,
            y,
            b,
            Cn(q),
            K
          );
        if (q.$$typeof === J)
          return A(
            h,
            y,
            b,
            On(y, q),
            K
          );
        jn(y, q);
      }
      return null;
    }
    function Y(h, y, b, q) {
      for (var K = null, mt = null, G = y, at = y = 0, rt = null; G !== null && at < b.length; at++) {
        G.index > at ? (rt = G, G = null) : rt = G.sibling;
        var ht = _(
          h,
          G,
          b[at],
          q
        );
        if (ht === null) {
          G === null && (G = rt);
          break;
        }
        t && G && ht.alternate === null && l(h, G), y = n(ht, y, at), mt === null ? K = ht : mt.sibling = ht, mt = ht, G = rt;
      }
      if (at === b.length)
        return e(h, G), ot && ue(h, at), K;
      if (G === null) {
        for (; at < b.length; at++)
          G = M(h, b[at], q), G !== null && (y = n(
            G,
            y,
            at
          ), mt === null ? K = G : mt.sibling = G, mt = G);
        return ot && ue(h, at), K;
      }
      for (G = a(G); at < b.length; at++)
        rt = A(
          G,
          h,
          at,
          b[at],
          q
        ), rt !== null && (t && rt.alternate !== null && G.delete(
          rt.key === null ? at : rt.key
        ), y = n(
          rt,
          y,
          at
        ), mt === null ? K = rt : mt.sibling = rt, mt = rt);
      return t && G.forEach(function(Ke) {
        return l(h, Ke);
      }), ot && ue(h, at), K;
    }
    function k(h, y, b, q) {
      if (b == null) throw Error(s(151));
      for (var K = null, mt = null, G = y, at = y = 0, rt = null, ht = b.next(); G !== null && !ht.done; at++, ht = b.next()) {
        G.index > at ? (rt = G, G = null) : rt = G.sibling;
        var Ke = _(h, G, ht.value, q);
        if (Ke === null) {
          G === null && (G = rt);
          break;
        }
        t && G && Ke.alternate === null && l(h, G), y = n(Ke, y, at), mt === null ? K = Ke : mt.sibling = Ke, mt = Ke, G = rt;
      }
      if (ht.done)
        return e(h, G), ot && ue(h, at), K;
      if (G === null) {
        for (; !ht.done; at++, ht = b.next())
          ht = M(h, ht.value, q), ht !== null && (y = n(ht, y, at), mt === null ? K = ht : mt.sibling = ht, mt = ht);
        return ot && ue(h, at), K;
      }
      for (G = a(G); !ht.done; at++, ht = b.next())
        ht = A(G, h, at, ht.value, q), ht !== null && (t && ht.alternate !== null && G.delete(ht.key === null ? at : ht.key), y = n(ht, y, at), mt === null ? K = ht : mt.sibling = ht, mt = ht);
      return t && G.forEach(function(Gh) {
        return l(h, Gh);
      }), ot && ue(h, at), K;
    }
    function zt(h, y, b, q) {
      if (typeof b == "object" && b !== null && b.type === W && b.key === null && (b = b.props.children), typeof b == "object" && b !== null) {
        switch (b.$$typeof) {
          case Q:
            t: {
              for (var K = b.key; y !== null; ) {
                if (y.key === K) {
                  if (K = b.type, K === W) {
                    if (y.tag === 7) {
                      e(
                        h,
                        y.sibling
                      ), q = u(
                        y,
                        b.props.children
                      ), q.return = h, h = q;
                      break t;
                    }
                  } else if (y.elementType === K || typeof K == "object" && K !== null && K.$$typeof === F && ca(K) === y.type) {
                    e(
                      h,
                      y.sibling
                    ), q = u(y, b.props), Nu(q, b), q.return = h, h = q;
                    break t;
                  }
                  e(h, y);
                  break;
                } else l(h, y);
                y = y.sibling;
              }
              b.type === W ? (q = ea(
                b.props.children,
                h.mode,
                q,
                b.key
              ), q.return = h, h = q) : (q = xn(
                b.type,
                b.key,
                b.props,
                null,
                h.mode,
                q
              ), Nu(q, b), q.return = h, h = q);
            }
            return i(h);
          case w:
            t: {
              for (K = b.key; y !== null; ) {
                if (y.key === K)
                  if (y.tag === 4 && y.stateNode.containerInfo === b.containerInfo && y.stateNode.implementation === b.implementation) {
                    e(
                      h,
                      y.sibling
                    ), q = u(y, b.children || []), q.return = h, h = q;
                    break t;
                  } else {
                    e(h, y);
                    break;
                  }
                else l(h, y);
                y = y.sibling;
              }
              q = ec(b, h.mode, q), q.return = h, h = q;
            }
            return i(h);
          case F:
            return b = ca(b), zt(
              h,
              y,
              b,
              q
            );
        }
        if (ul(b))
          return Y(
            h,
            y,
            b,
            q
          );
        if (Yt(b)) {
          if (K = Yt(b), typeof K != "function") throw Error(s(150));
          return b = K.call(b), k(
            h,
            y,
            b,
            q
          );
        }
        if (typeof b.then == "function")
          return zt(
            h,
            y,
            Cn(b),
            q
          );
        if (b.$$typeof === J)
          return zt(
            h,
            y,
            On(h, b),
            q
          );
        jn(h, b);
      }
      return typeof b == "string" && b !== "" || typeof b == "number" || typeof b == "bigint" ? (b = "" + b, y !== null && y.tag === 6 ? (e(h, y.sibling), q = u(y, b), q.return = h, h = q) : (e(h, y), q = lc(b, h.mode, q), q.return = h, h = q), i(h)) : e(h, y);
    }
    return function(h, y, b, q) {
      try {
        qu = 0;
        var K = zt(
          h,
          y,
          b,
          q
        );
        return La = null, K;
      } catch (G) {
        if (G === Ya || G === Dn) throw G;
        var mt = Sl(29, G, null, h.mode);
        return mt.lanes = q, mt.return = h, mt;
      } finally {
      }
    };
  }
  var sa = hr(!0), vr = hr(!1), Ne = !1;
  function mc(t) {
    t.updateQueue = {
      baseState: t.memoizedState,
      firstBaseUpdate: null,
      lastBaseUpdate: null,
      shared: { pending: null, lanes: 0, hiddenCallbacks: null },
      callbacks: null
    };
  }
  function hc(t, l) {
    t = t.updateQueue, l.updateQueue === t && (l.updateQueue = {
      baseState: t.baseState,
      firstBaseUpdate: t.firstBaseUpdate,
      lastBaseUpdate: t.lastBaseUpdate,
      shared: t.shared,
      callbacks: null
    });
  }
  function Oe(t) {
    return { lane: t, tag: 0, payload: null, callback: null, next: null };
  }
  function Me(t, l, e) {
    var a = t.updateQueue;
    if (a === null) return null;
    if (a = a.shared, (gt & 2) !== 0) {
      var u = a.pending;
      return u === null ? l.next = l : (l.next = u.next, u.next = l), a.pending = l, l = Tn(t), Ps(t, null, e), l;
    }
    return zn(t, a, l, e), Tn(t);
  }
  function Ou(t, l, e) {
    if (l = l.updateQueue, l !== null && (l = l.shared, (e & 4194048) !== 0)) {
      var a = l.lanes;
      a &= t.pendingLanes, e |= a, l.lanes = e, cs(t, e);
    }
  }
  function vc(t, l) {
    var e = t.updateQueue, a = t.alternate;
    if (a !== null && (a = a.updateQueue, e === a)) {
      var u = null, n = null;
      if (e = e.firstBaseUpdate, e !== null) {
        do {
          var i = {
            lane: e.lane,
            tag: e.tag,
            payload: e.payload,
            callback: null,
            next: null
          };
          n === null ? u = n = i : n = n.next = i, e = e.next;
        } while (e !== null);
        n === null ? u = n = l : n = n.next = l;
      } else u = n = l;
      e = {
        baseState: a.baseState,
        firstBaseUpdate: u,
        lastBaseUpdate: n,
        shared: a.shared,
        callbacks: a.callbacks
      }, t.updateQueue = e;
      return;
    }
    t = e.lastBaseUpdate, t === null ? e.firstBaseUpdate = l : t.next = l, e.lastBaseUpdate = l;
  }
  var gc = !1;
  function Mu() {
    if (gc) {
      var t = Ba;
      if (t !== null) throw t;
    }
  }
  function Du(t, l, e, a) {
    gc = !1;
    var u = t.updateQueue;
    Ne = !1;
    var n = u.firstBaseUpdate, i = u.lastBaseUpdate, f = u.shared.pending;
    if (f !== null) {
      u.shared.pending = null;
      var d = f, S = d.next;
      d.next = null, i === null ? n = S : i.next = S, i = d;
      var z = t.alternate;
      z !== null && (z = z.updateQueue, f = z.lastBaseUpdate, f !== i && (f === null ? z.firstBaseUpdate = S : f.next = S, z.lastBaseUpdate = d));
    }
    if (n !== null) {
      var M = u.baseState;
      i = 0, z = S = d = null, f = n;
      do {
        var _ = f.lane & -536870913, A = _ !== f.lane;
        if (A ? (st & _) === _ : (a & _) === _) {
          _ !== 0 && _ === Ha && (gc = !0), z !== null && (z = z.next = {
            lane: 0,
            tag: f.tag,
            payload: f.payload,
            callback: null,
            next: null
          });
          t: {
            var Y = t, k = f;
            _ = l;
            var zt = e;
            switch (k.tag) {
              case 1:
                if (Y = k.payload, typeof Y == "function") {
                  M = Y.call(zt, M, _);
                  break t;
                }
                M = Y;
                break t;
              case 3:
                Y.flags = Y.flags & -65537 | 128;
              case 0:
                if (Y = k.payload, _ = typeof Y == "function" ? Y.call(zt, M, _) : Y, _ == null) break t;
                M = E({}, M, _);
                break t;
              case 2:
                Ne = !0;
            }
          }
          _ = f.callback, _ !== null && (t.flags |= 64, A && (t.flags |= 8192), A = u.callbacks, A === null ? u.callbacks = [_] : A.push(_));
        } else
          A = {
            lane: _,
            tag: f.tag,
            payload: f.payload,
            callback: f.callback,
            next: null
          }, z === null ? (S = z = A, d = M) : z = z.next = A, i |= _;
        if (f = f.next, f === null) {
          if (f = u.shared.pending, f === null)
            break;
          A = f, f = A.next, A.next = null, u.lastBaseUpdate = A, u.shared.pending = null;
        }
      } while (!0);
      z === null && (d = M), u.baseState = d, u.firstBaseUpdate = S, u.lastBaseUpdate = z, n === null && (u.shared.lanes = 0), Re |= i, t.lanes = i, t.memoizedState = M;
    }
  }
  function gr(t, l) {
    if (typeof t != "function")
      throw Error(s(191, t));
    t.call(l);
  }
  function pr(t, l) {
    var e = t.callbacks;
    if (e !== null)
      for (t.callbacks = null, t = 0; t < e.length; t++)
        gr(e[t], l);
  }
  var Ga = m(null), Rn = m(0);
  function br(t, l) {
    t = he, j(Rn, t), j(Ga, l), he = t | l.baseLanes;
  }
  function pc() {
    j(Rn, he), j(Ga, Ga.current);
  }
  function bc() {
    he = Rn.current, x(Ga), x(Rn);
  }
  var _l = m(null), Cl = null;
  function De(t) {
    var l = t.alternate;
    j(Ht, Ht.current & 1), j(_l, t), Cl === null && (l === null || Ga.current !== null || l.memoizedState !== null) && (Cl = t);
  }
  function Sc(t) {
    j(Ht, Ht.current), j(_l, t), Cl === null && (Cl = t);
  }
  function Sr(t) {
    t.tag === 22 ? (j(Ht, Ht.current), j(_l, t), Cl === null && (Cl = t)) : Ue();
  }
  function Ue() {
    j(Ht, Ht.current), j(_l, _l.current);
  }
  function El(t) {
    x(_l), Cl === t && (Cl = null), x(Ht);
  }
  var Ht = m(0);
  function Hn(t) {
    for (var l = t; l !== null; ) {
      if (l.tag === 13) {
        var e = l.memoizedState;
        if (e !== null && (e = e.dehydrated, e === null || qf(e) || Nf(e)))
          return l;
      } else if (l.tag === 19 && (l.memoizedProps.revealOrder === "forwards" || l.memoizedProps.revealOrder === "backwards" || l.memoizedProps.revealOrder === "unstable_legacy-backwards" || l.memoizedProps.revealOrder === "together")) {
        if ((l.flags & 128) !== 0) return l;
      } else if (l.child !== null) {
        l.child.return = l, l = l.child;
        continue;
      }
      if (l === t) break;
      for (; l.sibling === null; ) {
        if (l.return === null || l.return === t) return null;
        l = l.return;
      }
      l.sibling.return = l.return, l = l.sibling;
    }
    return null;
  }
  var ce = 0, et = null, Et = null, Gt = null, Bn = !1, Qa = !1, ra = !1, Yn = 0, Uu = 0, Xa = null, Mm = 0;
  function Ct() {
    throw Error(s(321));
  }
  function _c(t, l) {
    if (l === null) return !1;
    for (var e = 0; e < l.length && e < t.length; e++)
      if (!bl(t[e], l[e])) return !1;
    return !0;
  }
  function Ec(t, l, e, a, u, n) {
    return ce = n, et = l, l.memoizedState = null, l.updateQueue = null, l.lanes = 0, T.H = t === null || t.memoizedState === null ? ao : Bc, ra = !1, n = e(a, u), ra = !1, Qa && (n = Er(
      l,
      e,
      a,
      u
    )), _r(t), n;
  }
  function _r(t) {
    T.H = Ru;
    var l = Et !== null && Et.next !== null;
    if (ce = 0, Gt = Et = et = null, Bn = !1, Uu = 0, Xa = null, l) throw Error(s(300));
    t === null || Qt || (t = t.dependencies, t !== null && Nn(t) && (Qt = !0));
  }
  function Er(t, l, e, a) {
    et = t;
    var u = 0;
    do {
      if (Qa && (Xa = null), Uu = 0, Qa = !1, 25 <= u) throw Error(s(301));
      if (u += 1, Gt = Et = null, t.updateQueue != null) {
        var n = t.updateQueue;
        n.lastEffect = null, n.events = null, n.stores = null, n.memoCache != null && (n.memoCache.index = 0);
      }
      T.H = uo, n = l(e, a);
    } while (Qa);
    return n;
  }
  function Dm() {
    var t = T.H, l = t.useState()[0];
    return l = typeof l.then == "function" ? Cu(l) : l, t = t.useState()[0], (Et !== null ? Et.memoizedState : null) !== t && (et.flags |= 1024), l;
  }
  function Ac() {
    var t = Yn !== 0;
    return Yn = 0, t;
  }
  function zc(t, l, e) {
    l.updateQueue = t.updateQueue, l.flags &= -2053, t.lanes &= ~e;
  }
  function Tc(t) {
    if (Bn) {
      for (t = t.memoizedState; t !== null; ) {
        var l = t.queue;
        l !== null && (l.pending = null), t = t.next;
      }
      Bn = !1;
    }
    ce = 0, Gt = Et = et = null, Qa = !1, Uu = Yn = 0, Xa = null;
  }
  function nl() {
    var t = {
      memoizedState: null,
      baseState: null,
      baseQueue: null,
      queue: null,
      next: null
    };
    return Gt === null ? et.memoizedState = Gt = t : Gt = Gt.next = t, Gt;
  }
  function Bt() {
    if (Et === null) {
      var t = et.alternate;
      t = t !== null ? t.memoizedState : null;
    } else t = Et.next;
    var l = Gt === null ? et.memoizedState : Gt.next;
    if (l !== null)
      Gt = l, Et = t;
    else {
      if (t === null)
        throw et.alternate === null ? Error(s(467)) : Error(s(310));
      Et = t, t = {
        memoizedState: Et.memoizedState,
        baseState: Et.baseState,
        baseQueue: Et.baseQueue,
        queue: Et.queue,
        next: null
      }, Gt === null ? et.memoizedState = Gt = t : Gt = Gt.next = t;
    }
    return Gt;
  }
  function Ln() {
    return { lastEffect: null, events: null, stores: null, memoCache: null };
  }
  function Cu(t) {
    var l = Uu;
    return Uu += 1, Xa === null && (Xa = []), t = dr(Xa, t, l), l = et, (Gt === null ? l.memoizedState : Gt.next) === null && (l = l.alternate, T.H = l === null || l.memoizedState === null ? ao : Bc), t;
  }
  function Gn(t) {
    if (t !== null && typeof t == "object") {
      if (typeof t.then == "function") return Cu(t);
      if (t.$$typeof === J) return Ft(t);
    }
    throw Error(s(438, String(t)));
  }
  function xc(t) {
    var l = null, e = et.updateQueue;
    if (e !== null && (l = e.memoCache), l == null) {
      var a = et.alternate;
      a !== null && (a = a.updateQueue, a !== null && (a = a.memoCache, a != null && (l = {
        data: a.data.map(function(u) {
          return u.slice();
        }),
        index: 0
      })));
    }
    if (l == null && (l = { data: [], index: 0 }), e === null && (e = Ln(), et.updateQueue = e), e.memoCache = l, e = l.data[l.index], e === void 0)
      for (e = l.data[l.index] = Array(t), a = 0; a < t; a++)
        e[a] = Yl;
    return l.index++, e;
  }
  function fe(t, l) {
    return typeof l == "function" ? l(t) : l;
  }
  function Qn(t) {
    var l = Bt();
    return qc(l, Et, t);
  }
  function qc(t, l, e) {
    var a = t.queue;
    if (a === null) throw Error(s(311));
    a.lastRenderedReducer = e;
    var u = t.baseQueue, n = a.pending;
    if (n !== null) {
      if (u !== null) {
        var i = u.next;
        u.next = n.next, n.next = i;
      }
      l.baseQueue = u = n, a.pending = null;
    }
    if (n = t.baseState, u === null) t.memoizedState = n;
    else {
      l = u.next;
      var f = i = null, d = null, S = l, z = !1;
      do {
        var M = S.lane & -536870913;
        if (M !== S.lane ? (st & M) === M : (ce & M) === M) {
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
            }), M === Ha && (z = !0);
          else if ((ce & _) === _) {
            S = S.next, _ === Ha && (z = !0);
            continue;
          } else
            M = {
              lane: 0,
              revertLane: S.revertLane,
              gesture: null,
              action: S.action,
              hasEagerState: S.hasEagerState,
              eagerState: S.eagerState,
              next: null
            }, d === null ? (f = d = M, i = n) : d = d.next = M, et.lanes |= _, Re |= _;
          M = S.action, ra && e(n, M), n = S.hasEagerState ? S.eagerState : e(n, M);
        } else
          _ = {
            lane: M,
            revertLane: S.revertLane,
            gesture: S.gesture,
            action: S.action,
            hasEagerState: S.hasEagerState,
            eagerState: S.eagerState,
            next: null
          }, d === null ? (f = d = _, i = n) : d = d.next = _, et.lanes |= M, Re |= M;
        S = S.next;
      } while (S !== null && S !== l);
      if (d === null ? i = n : d.next = f, !bl(n, t.memoizedState) && (Qt = !0, z && (e = Ba, e !== null)))
        throw e;
      t.memoizedState = n, t.baseState = i, t.baseQueue = d, a.lastRenderedState = n;
    }
    return u === null && (a.lanes = 0), [t.memoizedState, a.dispatch];
  }
  function Nc(t) {
    var l = Bt(), e = l.queue;
    if (e === null) throw Error(s(311));
    e.lastRenderedReducer = t;
    var a = e.dispatch, u = e.pending, n = l.memoizedState;
    if (u !== null) {
      e.pending = null;
      var i = u = u.next;
      do
        n = t(n, i.action), i = i.next;
      while (i !== u);
      bl(n, l.memoizedState) || (Qt = !0), l.memoizedState = n, l.baseQueue === null && (l.baseState = n), e.lastRenderedState = n;
    }
    return [n, a];
  }
  function Ar(t, l, e) {
    var a = et, u = Bt(), n = ot;
    if (n) {
      if (e === void 0) throw Error(s(407));
      e = e();
    } else e = l();
    var i = !bl(
      (Et || u).memoizedState,
      e
    );
    if (i && (u.memoizedState = e, Qt = !0), u = u.queue, Dc(xr.bind(null, a, u, t), [
      t
    ]), u.getSnapshot !== l || i || Gt !== null && Gt.memoizedState.tag & 1) {
      if (a.flags |= 2048, Za(
        9,
        { destroy: void 0 },
        Tr.bind(
          null,
          a,
          u,
          e,
          l
        ),
        null
      ), xt === null) throw Error(s(349));
      n || (ce & 127) !== 0 || zr(a, l, e);
    }
    return e;
  }
  function zr(t, l, e) {
    t.flags |= 16384, t = { getSnapshot: l, value: e }, l = et.updateQueue, l === null ? (l = Ln(), et.updateQueue = l, l.stores = [t]) : (e = l.stores, e === null ? l.stores = [t] : e.push(t));
  }
  function Tr(t, l, e, a) {
    l.value = e, l.getSnapshot = a, qr(l) && Nr(t);
  }
  function xr(t, l, e) {
    return e(function() {
      qr(l) && Nr(t);
    });
  }
  function qr(t) {
    var l = t.getSnapshot;
    t = t.value;
    try {
      var e = l();
      return !bl(t, e);
    } catch {
      return !0;
    }
  }
  function Nr(t) {
    var l = la(t, 2);
    l !== null && ml(l, t, 2);
  }
  function Oc(t) {
    var l = nl();
    if (typeof t == "function") {
      var e = t;
      if (t = e(), ra) {
        _e(!0);
        try {
          e();
        } finally {
          _e(!1);
        }
      }
    }
    return l.memoizedState = l.baseState = t, l.queue = {
      pending: null,
      lanes: 0,
      dispatch: null,
      lastRenderedReducer: fe,
      lastRenderedState: t
    }, l;
  }
  function Or(t, l, e, a) {
    return t.baseState = e, qc(
      t,
      Et,
      typeof a == "function" ? a : fe
    );
  }
  function Um(t, l, e, a, u) {
    if (Vn(t)) throw Error(s(485));
    if (t = l.action, t !== null) {
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
      T.T !== null ? e(!0) : n.isTransition = !1, a(n), e = l.pending, e === null ? (n.next = l.pending = n, Mr(l, n)) : (n.next = e.next, l.pending = e.next = n);
    }
  }
  function Mr(t, l) {
    var e = l.action, a = l.payload, u = t.state;
    if (l.isTransition) {
      var n = T.T, i = {};
      T.T = i;
      try {
        var f = e(u, a), d = T.S;
        d !== null && d(i, f), Dr(t, l, f);
      } catch (S) {
        Mc(t, l, S);
      } finally {
        n !== null && i.types !== null && (n.types = i.types), T.T = n;
      }
    } else
      try {
        n = e(u, a), Dr(t, l, n);
      } catch (S) {
        Mc(t, l, S);
      }
  }
  function Dr(t, l, e) {
    e !== null && typeof e == "object" && typeof e.then == "function" ? e.then(
      function(a) {
        Ur(t, l, a);
      },
      function(a) {
        return Mc(t, l, a);
      }
    ) : Ur(t, l, e);
  }
  function Ur(t, l, e) {
    l.status = "fulfilled", l.value = e, Cr(l), t.state = e, l = t.pending, l !== null && (e = l.next, e === l ? t.pending = null : (e = e.next, l.next = e, Mr(t, e)));
  }
  function Mc(t, l, e) {
    var a = t.pending;
    if (t.pending = null, a !== null) {
      a = a.next;
      do
        l.status = "rejected", l.reason = e, Cr(l), l = l.next;
      while (l !== a);
    }
    t.action = null;
  }
  function Cr(t) {
    t = t.listeners;
    for (var l = 0; l < t.length; l++) (0, t[l])();
  }
  function jr(t, l) {
    return l;
  }
  function Rr(t, l) {
    if (ot) {
      var e = xt.formState;
      if (e !== null) {
        t: {
          var a = et;
          if (ot) {
            if (qt) {
              l: {
                for (var u = qt, n = Ul; u.nodeType !== 8; ) {
                  if (!n) {
                    u = null;
                    break l;
                  }
                  if (u = jl(
                    u.nextSibling
                  ), u === null) {
                    u = null;
                    break l;
                  }
                }
                n = u.data, u = n === "F!" || n === "F" ? u : null;
              }
              if (u) {
                qt = jl(
                  u.nextSibling
                ), a = u.data === "F!";
                break t;
              }
            }
            xe(a);
          }
          a = !1;
        }
        a && (l = e[0]);
      }
    }
    return e = nl(), e.memoizedState = e.baseState = l, a = {
      pending: null,
      lanes: 0,
      dispatch: null,
      lastRenderedReducer: jr,
      lastRenderedState: l
    }, e.queue = a, e = to.bind(
      null,
      et,
      a
    ), a.dispatch = e, a = Oc(!1), n = Hc.bind(
      null,
      et,
      !1,
      a.queue
    ), a = nl(), u = {
      state: l,
      dispatch: null,
      action: t,
      pending: null
    }, a.queue = u, e = Um.bind(
      null,
      et,
      u,
      n,
      e
    ), u.dispatch = e, a.memoizedState = t, [l, e, !1];
  }
  function Hr(t) {
    var l = Bt();
    return Br(l, Et, t);
  }
  function Br(t, l, e) {
    if (l = qc(
      t,
      l,
      jr
    )[0], t = Qn(fe)[0], typeof l == "object" && l !== null && typeof l.then == "function")
      try {
        var a = Cu(l);
      } catch (i) {
        throw i === Ya ? Dn : i;
      }
    else a = l;
    l = Bt();
    var u = l.queue, n = u.dispatch;
    return e !== l.memoizedState && (et.flags |= 2048, Za(
      9,
      { destroy: void 0 },
      Cm.bind(null, u, e),
      null
    )), [a, n, t];
  }
  function Cm(t, l) {
    t.action = l;
  }
  function Yr(t) {
    var l = Bt(), e = Et;
    if (e !== null)
      return Br(l, e, t);
    Bt(), l = l.memoizedState, e = Bt();
    var a = e.queue.dispatch;
    return e.memoizedState = t, [l, a, !1];
  }
  function Za(t, l, e, a) {
    return t = { tag: t, create: e, deps: a, inst: l, next: null }, l = et.updateQueue, l === null && (l = Ln(), et.updateQueue = l), e = l.lastEffect, e === null ? l.lastEffect = t.next = t : (a = e.next, e.next = t, t.next = a, l.lastEffect = t), t;
  }
  function Lr() {
    return Bt().memoizedState;
  }
  function Xn(t, l, e, a) {
    var u = nl();
    et.flags |= t, u.memoizedState = Za(
      1 | l,
      { destroy: void 0 },
      e,
      a === void 0 ? null : a
    );
  }
  function Zn(t, l, e, a) {
    var u = Bt();
    a = a === void 0 ? null : a;
    var n = u.memoizedState.inst;
    Et !== null && a !== null && _c(a, Et.memoizedState.deps) ? u.memoizedState = Za(l, n, e, a) : (et.flags |= t, u.memoizedState = Za(
      1 | l,
      n,
      e,
      a
    ));
  }
  function Gr(t, l) {
    Xn(8390656, 8, t, l);
  }
  function Dc(t, l) {
    Zn(2048, 8, t, l);
  }
  function jm(t) {
    et.flags |= 4;
    var l = et.updateQueue;
    if (l === null)
      l = Ln(), et.updateQueue = l, l.events = [t];
    else {
      var e = l.events;
      e === null ? l.events = [t] : e.push(t);
    }
  }
  function Qr(t) {
    var l = Bt().memoizedState;
    return jm({ ref: l, nextImpl: t }), function() {
      if ((gt & 2) !== 0) throw Error(s(440));
      return l.impl.apply(void 0, arguments);
    };
  }
  function Xr(t, l) {
    return Zn(4, 2, t, l);
  }
  function Zr(t, l) {
    return Zn(4, 4, t, l);
  }
  function Vr(t, l) {
    if (typeof l == "function") {
      t = t();
      var e = l(t);
      return function() {
        typeof e == "function" ? e() : l(null);
      };
    }
    if (l != null)
      return t = t(), l.current = t, function() {
        l.current = null;
      };
  }
  function Kr(t, l, e) {
    e = e != null ? e.concat([t]) : null, Zn(4, 4, Vr.bind(null, l, t), e);
  }
  function Uc() {
  }
  function Jr(t, l) {
    var e = Bt();
    l = l === void 0 ? null : l;
    var a = e.memoizedState;
    return l !== null && _c(l, a[1]) ? a[0] : (e.memoizedState = [t, l], t);
  }
  function wr(t, l) {
    var e = Bt();
    l = l === void 0 ? null : l;
    var a = e.memoizedState;
    if (l !== null && _c(l, a[1]))
      return a[0];
    if (a = t(), ra) {
      _e(!0);
      try {
        t();
      } finally {
        _e(!1);
      }
    }
    return e.memoizedState = [a, l], a;
  }
  function Cc(t, l, e) {
    return e === void 0 || (ce & 1073741824) !== 0 && (st & 261930) === 0 ? t.memoizedState = l : (t.memoizedState = e, t = $o(), et.lanes |= t, Re |= t, e);
  }
  function kr(t, l, e, a) {
    return bl(e, l) ? e : Ga.current !== null ? (t = Cc(t, e, a), bl(t, l) || (Qt = !0), t) : (ce & 42) === 0 || (ce & 1073741824) !== 0 && (st & 261930) === 0 ? (Qt = !0, t.memoizedState = e) : (t = $o(), et.lanes |= t, Re |= t, l);
  }
  function $r(t, l, e, a, u) {
    var n = H.p;
    H.p = n !== 0 && 8 > n ? n : 8;
    var i = T.T, f = {};
    T.T = f, Hc(t, !1, l, e);
    try {
      var d = u(), S = T.S;
      if (S !== null && S(f, d), d !== null && typeof d == "object" && typeof d.then == "function") {
        var z = Om(
          d,
          a
        );
        ju(
          t,
          l,
          z,
          Tl(t)
        );
      } else
        ju(
          t,
          l,
          a,
          Tl(t)
        );
    } catch (M) {
      ju(
        t,
        l,
        { then: function() {
        }, status: "rejected", reason: M },
        Tl()
      );
    } finally {
      H.p = n, i !== null && f.types !== null && (i.types = f.types), T.T = i;
    }
  }
  function Rm() {
  }
  function jc(t, l, e, a) {
    if (t.tag !== 5) throw Error(s(476));
    var u = Wr(t).queue;
    $r(
      t,
      u,
      l,
      Z,
      e === null ? Rm : function() {
        return Fr(t), e(a);
      }
    );
  }
  function Wr(t) {
    var l = t.memoizedState;
    if (l !== null) return l;
    l = {
      memoizedState: Z,
      baseState: Z,
      baseQueue: null,
      queue: {
        pending: null,
        lanes: 0,
        dispatch: null,
        lastRenderedReducer: fe,
        lastRenderedState: Z
      },
      next: null
    };
    var e = {};
    return l.next = {
      memoizedState: e,
      baseState: e,
      baseQueue: null,
      queue: {
        pending: null,
        lanes: 0,
        dispatch: null,
        lastRenderedReducer: fe,
        lastRenderedState: e
      },
      next: null
    }, t.memoizedState = l, t = t.alternate, t !== null && (t.memoizedState = l), l;
  }
  function Fr(t) {
    var l = Wr(t);
    l.next === null && (l = t.alternate.memoizedState), ju(
      t,
      l.next.queue,
      {},
      Tl()
    );
  }
  function Rc() {
    return Ft(Fu);
  }
  function Ir() {
    return Bt().memoizedState;
  }
  function Pr() {
    return Bt().memoizedState;
  }
  function Hm(t) {
    for (var l = t.return; l !== null; ) {
      switch (l.tag) {
        case 24:
        case 3:
          var e = Tl();
          t = Oe(e);
          var a = Me(l, t, e);
          a !== null && (ml(a, l, e), Ou(a, l, e)), l = { cache: rc() }, t.payload = l;
          return;
      }
      l = l.return;
    }
  }
  function Bm(t, l, e) {
    var a = Tl();
    e = {
      lane: a,
      revertLane: 0,
      gesture: null,
      action: e,
      hasEagerState: !1,
      eagerState: null,
      next: null
    }, Vn(t) ? lo(l, e) : (e = Pi(t, l, e, a), e !== null && (ml(e, t, a), eo(e, l, a)));
  }
  function to(t, l, e) {
    var a = Tl();
    ju(t, l, e, a);
  }
  function ju(t, l, e, a) {
    var u = {
      lane: a,
      revertLane: 0,
      gesture: null,
      action: e,
      hasEagerState: !1,
      eagerState: null,
      next: null
    };
    if (Vn(t)) lo(l, u);
    else {
      var n = t.alternate;
      if (t.lanes === 0 && (n === null || n.lanes === 0) && (n = l.lastRenderedReducer, n !== null))
        try {
          var i = l.lastRenderedState, f = n(i, e);
          if (u.hasEagerState = !0, u.eagerState = f, bl(f, i))
            return zn(t, l, u, 0), xt === null && An(), !1;
        } catch {
        } finally {
        }
      if (e = Pi(t, l, u, a), e !== null)
        return ml(e, t, a), eo(e, l, a), !0;
    }
    return !1;
  }
  function Hc(t, l, e, a) {
    if (a = {
      lane: 2,
      revertLane: hf(),
      gesture: null,
      action: a,
      hasEagerState: !1,
      eagerState: null,
      next: null
    }, Vn(t)) {
      if (l) throw Error(s(479));
    } else
      l = Pi(
        t,
        e,
        a,
        2
      ), l !== null && ml(l, t, 2);
  }
  function Vn(t) {
    var l = t.alternate;
    return t === et || l !== null && l === et;
  }
  function lo(t, l) {
    Qa = Bn = !0;
    var e = t.pending;
    e === null ? l.next = l : (l.next = e.next, e.next = l), t.pending = l;
  }
  function eo(t, l, e) {
    if ((e & 4194048) !== 0) {
      var a = l.lanes;
      a &= t.pendingLanes, e |= a, l.lanes = e, cs(t, e);
    }
  }
  var Ru = {
    readContext: Ft,
    use: Gn,
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
  Ru.useEffectEvent = Ct;
  var ao = {
    readContext: Ft,
    use: Gn,
    useCallback: function(t, l) {
      return nl().memoizedState = [
        t,
        l === void 0 ? null : l
      ], t;
    },
    useContext: Ft,
    useEffect: Gr,
    useImperativeHandle: function(t, l, e) {
      e = e != null ? e.concat([t]) : null, Xn(
        4194308,
        4,
        Vr.bind(null, l, t),
        e
      );
    },
    useLayoutEffect: function(t, l) {
      return Xn(4194308, 4, t, l);
    },
    useInsertionEffect: function(t, l) {
      Xn(4, 2, t, l);
    },
    useMemo: function(t, l) {
      var e = nl();
      l = l === void 0 ? null : l;
      var a = t();
      if (ra) {
        _e(!0);
        try {
          t();
        } finally {
          _e(!1);
        }
      }
      return e.memoizedState = [a, l], a;
    },
    useReducer: function(t, l, e) {
      var a = nl();
      if (e !== void 0) {
        var u = e(l);
        if (ra) {
          _e(!0);
          try {
            e(l);
          } finally {
            _e(!1);
          }
        }
      } else u = l;
      return a.memoizedState = a.baseState = u, t = {
        pending: null,
        lanes: 0,
        dispatch: null,
        lastRenderedReducer: t,
        lastRenderedState: u
      }, a.queue = t, t = t.dispatch = Bm.bind(
        null,
        et,
        t
      ), [a.memoizedState, t];
    },
    useRef: function(t) {
      var l = nl();
      return t = { current: t }, l.memoizedState = t;
    },
    useState: function(t) {
      t = Oc(t);
      var l = t.queue, e = to.bind(null, et, l);
      return l.dispatch = e, [t.memoizedState, e];
    },
    useDebugValue: Uc,
    useDeferredValue: function(t, l) {
      var e = nl();
      return Cc(e, t, l);
    },
    useTransition: function() {
      var t = Oc(!1);
      return t = $r.bind(
        null,
        et,
        t.queue,
        !0,
        !1
      ), nl().memoizedState = t, [!1, t];
    },
    useSyncExternalStore: function(t, l, e) {
      var a = et, u = nl();
      if (ot) {
        if (e === void 0)
          throw Error(s(407));
        e = e();
      } else {
        if (e = l(), xt === null)
          throw Error(s(349));
        (st & 127) !== 0 || zr(a, l, e);
      }
      u.memoizedState = e;
      var n = { value: e, getSnapshot: l };
      return u.queue = n, Gr(xr.bind(null, a, n, t), [
        t
      ]), a.flags |= 2048, Za(
        9,
        { destroy: void 0 },
        Tr.bind(
          null,
          a,
          n,
          e,
          l
        ),
        null
      ), e;
    },
    useId: function() {
      var t = nl(), l = xt.identifierPrefix;
      if (ot) {
        var e = kl, a = wl;
        e = (a & ~(1 << 32 - pl(a) - 1)).toString(32) + e, l = "_" + l + "R_" + e, e = Yn++, 0 < e && (l += "H" + e.toString(32)), l += "_";
      } else
        e = Mm++, l = "_" + l + "r_" + e.toString(32) + "_";
      return t.memoizedState = l;
    },
    useHostTransitionStatus: Rc,
    useFormState: Rr,
    useActionState: Rr,
    useOptimistic: function(t) {
      var l = nl();
      l.memoizedState = l.baseState = t;
      var e = {
        pending: null,
        lanes: 0,
        dispatch: null,
        lastRenderedReducer: null,
        lastRenderedState: null
      };
      return l.queue = e, l = Hc.bind(
        null,
        et,
        !0,
        e
      ), e.dispatch = l, [t, l];
    },
    useMemoCache: xc,
    useCacheRefresh: function() {
      return nl().memoizedState = Hm.bind(
        null,
        et
      );
    },
    useEffectEvent: function(t) {
      var l = nl(), e = { impl: t };
      return l.memoizedState = e, function() {
        if ((gt & 2) !== 0)
          throw Error(s(440));
        return e.impl.apply(void 0, arguments);
      };
    }
  }, Bc = {
    readContext: Ft,
    use: Gn,
    useCallback: Jr,
    useContext: Ft,
    useEffect: Dc,
    useImperativeHandle: Kr,
    useInsertionEffect: Xr,
    useLayoutEffect: Zr,
    useMemo: wr,
    useReducer: Qn,
    useRef: Lr,
    useState: function() {
      return Qn(fe);
    },
    useDebugValue: Uc,
    useDeferredValue: function(t, l) {
      var e = Bt();
      return kr(
        e,
        Et.memoizedState,
        t,
        l
      );
    },
    useTransition: function() {
      var t = Qn(fe)[0], l = Bt().memoizedState;
      return [
        typeof t == "boolean" ? t : Cu(t),
        l
      ];
    },
    useSyncExternalStore: Ar,
    useId: Ir,
    useHostTransitionStatus: Rc,
    useFormState: Hr,
    useActionState: Hr,
    useOptimistic: function(t, l) {
      var e = Bt();
      return Or(e, Et, t, l);
    },
    useMemoCache: xc,
    useCacheRefresh: Pr
  };
  Bc.useEffectEvent = Qr;
  var uo = {
    readContext: Ft,
    use: Gn,
    useCallback: Jr,
    useContext: Ft,
    useEffect: Dc,
    useImperativeHandle: Kr,
    useInsertionEffect: Xr,
    useLayoutEffect: Zr,
    useMemo: wr,
    useReducer: Nc,
    useRef: Lr,
    useState: function() {
      return Nc(fe);
    },
    useDebugValue: Uc,
    useDeferredValue: function(t, l) {
      var e = Bt();
      return Et === null ? Cc(e, t, l) : kr(
        e,
        Et.memoizedState,
        t,
        l
      );
    },
    useTransition: function() {
      var t = Nc(fe)[0], l = Bt().memoizedState;
      return [
        typeof t == "boolean" ? t : Cu(t),
        l
      ];
    },
    useSyncExternalStore: Ar,
    useId: Ir,
    useHostTransitionStatus: Rc,
    useFormState: Yr,
    useActionState: Yr,
    useOptimistic: function(t, l) {
      var e = Bt();
      return Et !== null ? Or(e, Et, t, l) : (e.baseState = t, [t, e.queue.dispatch]);
    },
    useMemoCache: xc,
    useCacheRefresh: Pr
  };
  uo.useEffectEvent = Qr;
  function Yc(t, l, e, a) {
    l = t.memoizedState, e = e(a, l), e = e == null ? l : E({}, l, e), t.memoizedState = e, t.lanes === 0 && (t.updateQueue.baseState = e);
  }
  var Lc = {
    enqueueSetState: function(t, l, e) {
      t = t._reactInternals;
      var a = Tl(), u = Oe(a);
      u.payload = l, e != null && (u.callback = e), l = Me(t, u, a), l !== null && (ml(l, t, a), Ou(l, t, a));
    },
    enqueueReplaceState: function(t, l, e) {
      t = t._reactInternals;
      var a = Tl(), u = Oe(a);
      u.tag = 1, u.payload = l, e != null && (u.callback = e), l = Me(t, u, a), l !== null && (ml(l, t, a), Ou(l, t, a));
    },
    enqueueForceUpdate: function(t, l) {
      t = t._reactInternals;
      var e = Tl(), a = Oe(e);
      a.tag = 2, l != null && (a.callback = l), l = Me(t, a, e), l !== null && (ml(l, t, e), Ou(l, t, e));
    }
  };
  function no(t, l, e, a, u, n, i) {
    return t = t.stateNode, typeof t.shouldComponentUpdate == "function" ? t.shouldComponentUpdate(a, n, i) : l.prototype && l.prototype.isPureReactComponent ? !_u(e, a) || !_u(u, n) : !0;
  }
  function io(t, l, e, a) {
    t = l.state, typeof l.componentWillReceiveProps == "function" && l.componentWillReceiveProps(e, a), typeof l.UNSAFE_componentWillReceiveProps == "function" && l.UNSAFE_componentWillReceiveProps(e, a), l.state !== t && Lc.enqueueReplaceState(l, l.state, null);
  }
  function oa(t, l) {
    var e = l;
    if ("ref" in l) {
      e = {};
      for (var a in l)
        a !== "ref" && (e[a] = l[a]);
    }
    if (t = t.defaultProps) {
      e === l && (e = E({}, e));
      for (var u in t)
        e[u] === void 0 && (e[u] = t[u]);
    }
    return e;
  }
  function co(t) {
    En(t);
  }
  function fo(t) {
    console.error(t);
  }
  function so(t) {
    En(t);
  }
  function Kn(t, l) {
    try {
      var e = t.onUncaughtError;
      e(l.value, { componentStack: l.stack });
    } catch (a) {
      setTimeout(function() {
        throw a;
      });
    }
  }
  function ro(t, l, e) {
    try {
      var a = t.onCaughtError;
      a(e.value, {
        componentStack: e.stack,
        errorBoundary: l.tag === 1 ? l.stateNode : null
      });
    } catch (u) {
      setTimeout(function() {
        throw u;
      });
    }
  }
  function Gc(t, l, e) {
    return e = Oe(e), e.tag = 3, e.payload = { element: null }, e.callback = function() {
      Kn(t, l);
    }, e;
  }
  function oo(t) {
    return t = Oe(t), t.tag = 3, t;
  }
  function yo(t, l, e, a) {
    var u = e.type.getDerivedStateFromError;
    if (typeof u == "function") {
      var n = a.value;
      t.payload = function() {
        return u(n);
      }, t.callback = function() {
        ro(l, e, a);
      };
    }
    var i = e.stateNode;
    i !== null && typeof i.componentDidCatch == "function" && (t.callback = function() {
      ro(l, e, a), typeof u != "function" && (He === null ? He = /* @__PURE__ */ new Set([this]) : He.add(this));
      var f = a.stack;
      this.componentDidCatch(a.value, {
        componentStack: f !== null ? f : ""
      });
    });
  }
  function Ym(t, l, e, a, u) {
    if (e.flags |= 32768, a !== null && typeof a == "object" && typeof a.then == "function") {
      if (l = e.alternate, l !== null && Ra(
        l,
        e,
        u,
        !0
      ), e = _l.current, e !== null) {
        switch (e.tag) {
          case 31:
          case 13:
            return Cl === null ? ai() : e.alternate === null && jt === 0 && (jt = 3), e.flags &= -257, e.flags |= 65536, e.lanes = u, a === Un ? e.flags |= 16384 : (l = e.updateQueue, l === null ? e.updateQueue = /* @__PURE__ */ new Set([a]) : l.add(a), df(t, a, u)), !1;
          case 22:
            return e.flags |= 65536, a === Un ? e.flags |= 16384 : (l = e.updateQueue, l === null ? (l = {
              transitions: null,
              markerInstances: null,
              retryQueue: /* @__PURE__ */ new Set([a])
            }, e.updateQueue = l) : (e = l.retryQueue, e === null ? l.retryQueue = /* @__PURE__ */ new Set([a]) : e.add(a)), df(t, a, u)), !1;
        }
        throw Error(s(435, e.tag));
      }
      return df(t, a, u), ai(), !1;
    }
    if (ot)
      return l = _l.current, l !== null ? ((l.flags & 65536) === 0 && (l.flags |= 256), l.flags |= 65536, l.lanes = u, a !== nc && (t = Error(s(422), { cause: a }), zu(Ol(t, e)))) : (a !== nc && (l = Error(s(423), {
        cause: a
      }), zu(
        Ol(l, e)
      )), t = t.current.alternate, t.flags |= 65536, u &= -u, t.lanes |= u, a = Ol(a, e), u = Gc(
        t.stateNode,
        a,
        u
      ), vc(t, u), jt !== 4 && (jt = 2)), !1;
    var n = Error(s(520), { cause: a });
    if (n = Ol(n, e), Zu === null ? Zu = [n] : Zu.push(n), jt !== 4 && (jt = 2), l === null) return !0;
    a = Ol(a, e), e = l;
    do {
      switch (e.tag) {
        case 3:
          return e.flags |= 65536, t = u & -u, e.lanes |= t, t = Gc(e.stateNode, a, t), vc(e, t), !1;
        case 1:
          if (l = e.type, n = e.stateNode, (e.flags & 128) === 0 && (typeof l.getDerivedStateFromError == "function" || n !== null && typeof n.componentDidCatch == "function" && (He === null || !He.has(n))))
            return e.flags |= 65536, u &= -u, e.lanes |= u, u = oo(u), yo(
              u,
              t,
              e,
              a
            ), vc(e, u), !1;
      }
      e = e.return;
    } while (e !== null);
    return !1;
  }
  var Qc = Error(s(461)), Qt = !1;
  function It(t, l, e, a) {
    l.child = t === null ? vr(l, null, e, a) : sa(
      l,
      t.child,
      e,
      a
    );
  }
  function mo(t, l, e, a, u) {
    e = e.render;
    var n = l.ref;
    if ("ref" in a) {
      var i = {};
      for (var f in a)
        f !== "ref" && (i[f] = a[f]);
    } else i = a;
    return na(l), a = Ec(
      t,
      l,
      e,
      i,
      n,
      u
    ), f = Ac(), t !== null && !Qt ? (zc(t, l, u), se(t, l, u)) : (ot && f && ac(l), l.flags |= 1, It(t, l, a, u), l.child);
  }
  function ho(t, l, e, a, u) {
    if (t === null) {
      var n = e.type;
      return typeof n == "function" && !tc(n) && n.defaultProps === void 0 && e.compare === null ? (l.tag = 15, l.type = n, vo(
        t,
        l,
        n,
        a,
        u
      )) : (t = xn(
        e.type,
        null,
        a,
        l,
        l.mode,
        u
      ), t.ref = l.ref, t.return = l, l.child = t);
    }
    if (n = t.child, !$c(t, u)) {
      var i = n.memoizedProps;
      if (e = e.compare, e = e !== null ? e : _u, e(i, a) && t.ref === l.ref)
        return se(t, l, u);
    }
    return l.flags |= 1, t = ae(n, a), t.ref = l.ref, t.return = l, l.child = t;
  }
  function vo(t, l, e, a, u) {
    if (t !== null) {
      var n = t.memoizedProps;
      if (_u(n, a) && t.ref === l.ref)
        if (Qt = !1, l.pendingProps = a = n, $c(t, u))
          (t.flags & 131072) !== 0 && (Qt = !0);
        else
          return l.lanes = t.lanes, se(t, l, u);
    }
    return Xc(
      t,
      l,
      e,
      a,
      u
    );
  }
  function go(t, l, e, a) {
    var u = a.children, n = t !== null ? t.memoizedState : null;
    if (t === null && l.stateNode === null && (l.stateNode = {
      _visibility: 1,
      _pendingMarkers: null,
      _retryCache: null,
      _transitions: null
    }), a.mode === "hidden") {
      if ((l.flags & 128) !== 0) {
        if (n = n !== null ? n.baseLanes | e : e, t !== null) {
          for (a = l.child = t.child, u = 0; a !== null; )
            u = u | a.lanes | a.childLanes, a = a.sibling;
          a = u & ~n;
        } else a = 0, l.child = null;
        return po(
          t,
          l,
          n,
          e,
          a
        );
      }
      if ((e & 536870912) !== 0)
        l.memoizedState = { baseLanes: 0, cachePool: null }, t !== null && Mn(
          l,
          n !== null ? n.cachePool : null
        ), n !== null ? br(l, n) : pc(), Sr(l);
      else
        return a = l.lanes = 536870912, po(
          t,
          l,
          n !== null ? n.baseLanes | e : e,
          e,
          a
        );
    } else
      n !== null ? (Mn(l, n.cachePool), br(l, n), Ue(), l.memoizedState = null) : (t !== null && Mn(l, null), pc(), Ue());
    return It(t, l, u, e), l.child;
  }
  function Hu(t, l) {
    return t !== null && t.tag === 22 || l.stateNode !== null || (l.stateNode = {
      _visibility: 1,
      _pendingMarkers: null,
      _retryCache: null,
      _transitions: null
    }), l.sibling;
  }
  function po(t, l, e, a, u) {
    var n = dc();
    return n = n === null ? null : { parent: Lt._currentValue, pool: n }, l.memoizedState = {
      baseLanes: e,
      cachePool: n
    }, t !== null && Mn(l, null), pc(), Sr(l), t !== null && Ra(t, l, a, !0), l.childLanes = u, null;
  }
  function Jn(t, l) {
    return l = kn(
      { mode: l.mode, children: l.children },
      t.mode
    ), l.ref = t.ref, t.child = l, l.return = t, l;
  }
  function bo(t, l, e) {
    return sa(l, t.child, null, e), t = Jn(l, l.pendingProps), t.flags |= 2, El(l), l.memoizedState = null, t;
  }
  function Lm(t, l, e) {
    var a = l.pendingProps, u = (l.flags & 128) !== 0;
    if (l.flags &= -129, t === null) {
      if (ot) {
        if (a.mode === "hidden")
          return t = Jn(l, a), l.lanes = 536870912, Hu(null, t);
        if (Sc(l), (t = qt) ? (t = Dd(
          t,
          Ul
        ), t = t !== null && t.data === "&" ? t : null, t !== null && (l.memoizedState = {
          dehydrated: t,
          treeContext: ze !== null ? { id: wl, overflow: kl } : null,
          retryLane: 536870912,
          hydrationErrors: null
        }, e = lr(t), e.return = l, l.child = e, Wt = l, qt = null)) : t = null, t === null) throw xe(l);
        return l.lanes = 536870912, null;
      }
      return Jn(l, a);
    }
    var n = t.memoizedState;
    if (n !== null) {
      var i = n.dehydrated;
      if (Sc(l), u)
        if (l.flags & 256)
          l.flags &= -257, l = bo(
            t,
            l,
            e
          );
        else if (l.memoizedState !== null)
          l.child = t.child, l.flags |= 128, l = null;
        else throw Error(s(558));
      else if (Qt || Ra(t, l, e, !1), u = (e & t.childLanes) !== 0, Qt || u) {
        if (a = xt, a !== null && (i = fs(a, e), i !== 0 && i !== n.retryLane))
          throw n.retryLane = i, la(t, i), ml(a, t, i), Qc;
        ai(), l = bo(
          t,
          l,
          e
        );
      } else
        t = n.treeContext, qt = jl(i.nextSibling), Wt = l, ot = !0, Te = null, Ul = !1, t !== null && ur(l, t), l = Jn(l, a), l.flags |= 4096;
      return l;
    }
    return t = ae(t.child, {
      mode: a.mode,
      children: a.children
    }), t.ref = l.ref, l.child = t, t.return = l, t;
  }
  function wn(t, l) {
    var e = l.ref;
    if (e === null)
      t !== null && t.ref !== null && (l.flags |= 4194816);
    else {
      if (typeof e != "function" && typeof e != "object")
        throw Error(s(284));
      (t === null || t.ref !== e) && (l.flags |= 4194816);
    }
  }
  function Xc(t, l, e, a, u) {
    return na(l), e = Ec(
      t,
      l,
      e,
      a,
      void 0,
      u
    ), a = Ac(), t !== null && !Qt ? (zc(t, l, u), se(t, l, u)) : (ot && a && ac(l), l.flags |= 1, It(t, l, e, u), l.child);
  }
  function So(t, l, e, a, u, n) {
    return na(l), l.updateQueue = null, e = Er(
      l,
      a,
      e,
      u
    ), _r(t), a = Ac(), t !== null && !Qt ? (zc(t, l, n), se(t, l, n)) : (ot && a && ac(l), l.flags |= 1, It(t, l, e, n), l.child);
  }
  function _o(t, l, e, a, u) {
    if (na(l), l.stateNode === null) {
      var n = Da, i = e.contextType;
      typeof i == "object" && i !== null && (n = Ft(i)), n = new e(a, n), l.memoizedState = n.state !== null && n.state !== void 0 ? n.state : null, n.updater = Lc, l.stateNode = n, n._reactInternals = l, n = l.stateNode, n.props = a, n.state = l.memoizedState, n.refs = {}, mc(l), i = e.contextType, n.context = typeof i == "object" && i !== null ? Ft(i) : Da, n.state = l.memoizedState, i = e.getDerivedStateFromProps, typeof i == "function" && (Yc(
        l,
        e,
        i,
        a
      ), n.state = l.memoizedState), typeof e.getDerivedStateFromProps == "function" || typeof n.getSnapshotBeforeUpdate == "function" || typeof n.UNSAFE_componentWillMount != "function" && typeof n.componentWillMount != "function" || (i = n.state, typeof n.componentWillMount == "function" && n.componentWillMount(), typeof n.UNSAFE_componentWillMount == "function" && n.UNSAFE_componentWillMount(), i !== n.state && Lc.enqueueReplaceState(n, n.state, null), Du(l, a, n, u), Mu(), n.state = l.memoizedState), typeof n.componentDidMount == "function" && (l.flags |= 4194308), a = !0;
    } else if (t === null) {
      n = l.stateNode;
      var f = l.memoizedProps, d = oa(e, f);
      n.props = d;
      var S = n.context, z = e.contextType;
      i = Da, typeof z == "object" && z !== null && (i = Ft(z));
      var M = e.getDerivedStateFromProps;
      z = typeof M == "function" || typeof n.getSnapshotBeforeUpdate == "function", f = l.pendingProps !== f, z || typeof n.UNSAFE_componentWillReceiveProps != "function" && typeof n.componentWillReceiveProps != "function" || (f || S !== i) && io(
        l,
        n,
        a,
        i
      ), Ne = !1;
      var _ = l.memoizedState;
      n.state = _, Du(l, a, n, u), Mu(), S = l.memoizedState, f || _ !== S || Ne ? (typeof M == "function" && (Yc(
        l,
        e,
        M,
        a
      ), S = l.memoizedState), (d = Ne || no(
        l,
        e,
        d,
        a,
        _,
        S,
        i
      )) ? (z || typeof n.UNSAFE_componentWillMount != "function" && typeof n.componentWillMount != "function" || (typeof n.componentWillMount == "function" && n.componentWillMount(), typeof n.UNSAFE_componentWillMount == "function" && n.UNSAFE_componentWillMount()), typeof n.componentDidMount == "function" && (l.flags |= 4194308)) : (typeof n.componentDidMount == "function" && (l.flags |= 4194308), l.memoizedProps = a, l.memoizedState = S), n.props = a, n.state = S, n.context = i, a = d) : (typeof n.componentDidMount == "function" && (l.flags |= 4194308), a = !1);
    } else {
      n = l.stateNode, hc(t, l), i = l.memoizedProps, z = oa(e, i), n.props = z, M = l.pendingProps, _ = n.context, S = e.contextType, d = Da, typeof S == "object" && S !== null && (d = Ft(S)), f = e.getDerivedStateFromProps, (S = typeof f == "function" || typeof n.getSnapshotBeforeUpdate == "function") || typeof n.UNSAFE_componentWillReceiveProps != "function" && typeof n.componentWillReceiveProps != "function" || (i !== M || _ !== d) && io(
        l,
        n,
        a,
        d
      ), Ne = !1, _ = l.memoizedState, n.state = _, Du(l, a, n, u), Mu();
      var A = l.memoizedState;
      i !== M || _ !== A || Ne || t !== null && t.dependencies !== null && Nn(t.dependencies) ? (typeof f == "function" && (Yc(
        l,
        e,
        f,
        a
      ), A = l.memoizedState), (z = Ne || no(
        l,
        e,
        z,
        a,
        _,
        A,
        d
      ) || t !== null && t.dependencies !== null && Nn(t.dependencies)) ? (S || typeof n.UNSAFE_componentWillUpdate != "function" && typeof n.componentWillUpdate != "function" || (typeof n.componentWillUpdate == "function" && n.componentWillUpdate(a, A, d), typeof n.UNSAFE_componentWillUpdate == "function" && n.UNSAFE_componentWillUpdate(
        a,
        A,
        d
      )), typeof n.componentDidUpdate == "function" && (l.flags |= 4), typeof n.getSnapshotBeforeUpdate == "function" && (l.flags |= 1024)) : (typeof n.componentDidUpdate != "function" || i === t.memoizedProps && _ === t.memoizedState || (l.flags |= 4), typeof n.getSnapshotBeforeUpdate != "function" || i === t.memoizedProps && _ === t.memoizedState || (l.flags |= 1024), l.memoizedProps = a, l.memoizedState = A), n.props = a, n.state = A, n.context = d, a = z) : (typeof n.componentDidUpdate != "function" || i === t.memoizedProps && _ === t.memoizedState || (l.flags |= 4), typeof n.getSnapshotBeforeUpdate != "function" || i === t.memoizedProps && _ === t.memoizedState || (l.flags |= 1024), a = !1);
    }
    return n = a, wn(t, l), a = (l.flags & 128) !== 0, n || a ? (n = l.stateNode, e = a && typeof e.getDerivedStateFromError != "function" ? null : n.render(), l.flags |= 1, t !== null && a ? (l.child = sa(
      l,
      t.child,
      null,
      u
    ), l.child = sa(
      l,
      null,
      e,
      u
    )) : It(t, l, e, u), l.memoizedState = n.state, t = l.child) : t = se(
      t,
      l,
      u
    ), t;
  }
  function Eo(t, l, e, a) {
    return aa(), l.flags |= 256, It(t, l, e, a), l.child;
  }
  var Zc = {
    dehydrated: null,
    treeContext: null,
    retryLane: 0,
    hydrationErrors: null
  };
  function Vc(t) {
    return { baseLanes: t, cachePool: rr() };
  }
  function Kc(t, l, e) {
    return t = t !== null ? t.childLanes & ~e : 0, l && (t |= zl), t;
  }
  function Ao(t, l, e) {
    var a = l.pendingProps, u = !1, n = (l.flags & 128) !== 0, i;
    if ((i = n) || (i = t !== null && t.memoizedState === null ? !1 : (Ht.current & 2) !== 0), i && (u = !0, l.flags &= -129), i = (l.flags & 32) !== 0, l.flags &= -33, t === null) {
      if (ot) {
        if (u ? De(l) : Ue(), (t = qt) ? (t = Dd(
          t,
          Ul
        ), t = t !== null && t.data !== "&" ? t : null, t !== null && (l.memoizedState = {
          dehydrated: t,
          treeContext: ze !== null ? { id: wl, overflow: kl } : null,
          retryLane: 536870912,
          hydrationErrors: null
        }, e = lr(t), e.return = l, l.child = e, Wt = l, qt = null)) : t = null, t === null) throw xe(l);
        return Nf(t) ? l.lanes = 32 : l.lanes = 536870912, null;
      }
      var f = a.children;
      return a = a.fallback, u ? (Ue(), u = l.mode, f = kn(
        { mode: "hidden", children: f },
        u
      ), a = ea(
        a,
        u,
        e,
        null
      ), f.return = l, a.return = l, f.sibling = a, l.child = f, a = l.child, a.memoizedState = Vc(e), a.childLanes = Kc(
        t,
        i,
        e
      ), l.memoizedState = Zc, Hu(null, a)) : (De(l), Jc(l, f));
    }
    var d = t.memoizedState;
    if (d !== null && (f = d.dehydrated, f !== null)) {
      if (n)
        l.flags & 256 ? (De(l), l.flags &= -257, l = wc(
          t,
          l,
          e
        )) : l.memoizedState !== null ? (Ue(), l.child = t.child, l.flags |= 128, l = null) : (Ue(), f = a.fallback, u = l.mode, a = kn(
          { mode: "visible", children: a.children },
          u
        ), f = ea(
          f,
          u,
          e,
          null
        ), f.flags |= 2, a.return = l, f.return = l, a.sibling = f, l.child = a, sa(
          l,
          t.child,
          null,
          e
        ), a = l.child, a.memoizedState = Vc(e), a.childLanes = Kc(
          t,
          i,
          e
        ), l.memoizedState = Zc, l = Hu(null, a));
      else if (De(l), Nf(f)) {
        if (i = f.nextSibling && f.nextSibling.dataset, i) var S = i.dgst;
        i = S, a = Error(s(419)), a.stack = "", a.digest = i, zu({ value: a, source: null, stack: null }), l = wc(
          t,
          l,
          e
        );
      } else if (Qt || Ra(t, l, e, !1), i = (e & t.childLanes) !== 0, Qt || i) {
        if (i = xt, i !== null && (a = fs(i, e), a !== 0 && a !== d.retryLane))
          throw d.retryLane = a, la(t, a), ml(i, t, a), Qc;
        qf(f) || ai(), l = wc(
          t,
          l,
          e
        );
      } else
        qf(f) ? (l.flags |= 192, l.child = t.child, l = null) : (t = d.treeContext, qt = jl(
          f.nextSibling
        ), Wt = l, ot = !0, Te = null, Ul = !1, t !== null && ur(l, t), l = Jc(
          l,
          a.children
        ), l.flags |= 4096);
      return l;
    }
    return u ? (Ue(), f = a.fallback, u = l.mode, d = t.child, S = d.sibling, a = ae(d, {
      mode: "hidden",
      children: a.children
    }), a.subtreeFlags = d.subtreeFlags & 65011712, S !== null ? f = ae(
      S,
      f
    ) : (f = ea(
      f,
      u,
      e,
      null
    ), f.flags |= 2), f.return = l, a.return = l, a.sibling = f, l.child = a, Hu(null, a), a = l.child, f = t.child.memoizedState, f === null ? f = Vc(e) : (u = f.cachePool, u !== null ? (d = Lt._currentValue, u = u.parent !== d ? { parent: d, pool: d } : u) : u = rr(), f = {
      baseLanes: f.baseLanes | e,
      cachePool: u
    }), a.memoizedState = f, a.childLanes = Kc(
      t,
      i,
      e
    ), l.memoizedState = Zc, Hu(t.child, a)) : (De(l), e = t.child, t = e.sibling, e = ae(e, {
      mode: "visible",
      children: a.children
    }), e.return = l, e.sibling = null, t !== null && (i = l.deletions, i === null ? (l.deletions = [t], l.flags |= 16) : i.push(t)), l.child = e, l.memoizedState = null, e);
  }
  function Jc(t, l) {
    return l = kn(
      { mode: "visible", children: l },
      t.mode
    ), l.return = t, t.child = l;
  }
  function kn(t, l) {
    return t = Sl(22, t, null, l), t.lanes = 0, t;
  }
  function wc(t, l, e) {
    return sa(l, t.child, null, e), t = Jc(
      l,
      l.pendingProps.children
    ), t.flags |= 2, l.memoizedState = null, t;
  }
  function zo(t, l, e) {
    t.lanes |= l;
    var a = t.alternate;
    a !== null && (a.lanes |= l), fc(t.return, l, e);
  }
  function kc(t, l, e, a, u, n) {
    var i = t.memoizedState;
    i === null ? t.memoizedState = {
      isBackwards: l,
      rendering: null,
      renderingStartTime: 0,
      last: a,
      tail: e,
      tailMode: u,
      treeForkCount: n
    } : (i.isBackwards = l, i.rendering = null, i.renderingStartTime = 0, i.last = a, i.tail = e, i.tailMode = u, i.treeForkCount = n);
  }
  function To(t, l, e) {
    var a = l.pendingProps, u = a.revealOrder, n = a.tail;
    a = a.children;
    var i = Ht.current, f = (i & 2) !== 0;
    if (f ? (i = i & 1 | 2, l.flags |= 128) : i &= 1, j(Ht, i), It(t, l, a, e), a = ot ? Au : 0, !f && t !== null && (t.flags & 128) !== 0)
      t: for (t = l.child; t !== null; ) {
        if (t.tag === 13)
          t.memoizedState !== null && zo(t, e, l);
        else if (t.tag === 19)
          zo(t, e, l);
        else if (t.child !== null) {
          t.child.return = t, t = t.child;
          continue;
        }
        if (t === l) break t;
        for (; t.sibling === null; ) {
          if (t.return === null || t.return === l)
            break t;
          t = t.return;
        }
        t.sibling.return = t.return, t = t.sibling;
      }
    switch (u) {
      case "forwards":
        for (e = l.child, u = null; e !== null; )
          t = e.alternate, t !== null && Hn(t) === null && (u = e), e = e.sibling;
        e = u, e === null ? (u = l.child, l.child = null) : (u = e.sibling, e.sibling = null), kc(
          l,
          !1,
          u,
          e,
          n,
          a
        );
        break;
      case "backwards":
      case "unstable_legacy-backwards":
        for (e = null, u = l.child, l.child = null; u !== null; ) {
          if (t = u.alternate, t !== null && Hn(t) === null) {
            l.child = u;
            break;
          }
          t = u.sibling, u.sibling = e, e = u, u = t;
        }
        kc(
          l,
          !0,
          e,
          null,
          n,
          a
        );
        break;
      case "together":
        kc(
          l,
          !1,
          null,
          null,
          void 0,
          a
        );
        break;
      default:
        l.memoizedState = null;
    }
    return l.child;
  }
  function se(t, l, e) {
    if (t !== null && (l.dependencies = t.dependencies), Re |= l.lanes, (e & l.childLanes) === 0)
      if (t !== null) {
        if (Ra(
          t,
          l,
          e,
          !1
        ), (e & l.childLanes) === 0)
          return null;
      } else return null;
    if (t !== null && l.child !== t.child)
      throw Error(s(153));
    if (l.child !== null) {
      for (t = l.child, e = ae(t, t.pendingProps), l.child = e, e.return = l; t.sibling !== null; )
        t = t.sibling, e = e.sibling = ae(t, t.pendingProps), e.return = l;
      e.sibling = null;
    }
    return l.child;
  }
  function $c(t, l) {
    return (t.lanes & l) !== 0 ? !0 : (t = t.dependencies, !!(t !== null && Nn(t)));
  }
  function Gm(t, l, e) {
    switch (l.tag) {
      case 3:
        Dt(l, l.stateNode.containerInfo), qe(l, Lt, t.memoizedState.cache), aa();
        break;
      case 27:
      case 5:
        Se(l);
        break;
      case 4:
        Dt(l, l.stateNode.containerInfo);
        break;
      case 10:
        qe(
          l,
          l.type,
          l.memoizedProps.value
        );
        break;
      case 31:
        if (l.memoizedState !== null)
          return l.flags |= 128, Sc(l), null;
        break;
      case 13:
        var a = l.memoizedState;
        if (a !== null)
          return a.dehydrated !== null ? (De(l), l.flags |= 128, null) : (e & l.child.childLanes) !== 0 ? Ao(t, l, e) : (De(l), t = se(
            t,
            l,
            e
          ), t !== null ? t.sibling : null);
        De(l);
        break;
      case 19:
        var u = (t.flags & 128) !== 0;
        if (a = (e & l.childLanes) !== 0, a || (Ra(
          t,
          l,
          e,
          !1
        ), a = (e & l.childLanes) !== 0), u) {
          if (a)
            return To(
              t,
              l,
              e
            );
          l.flags |= 128;
        }
        if (u = l.memoizedState, u !== null && (u.rendering = null, u.tail = null, u.lastEffect = null), j(Ht, Ht.current), a) break;
        return null;
      case 22:
        return l.lanes = 0, go(
          t,
          l,
          e,
          l.pendingProps
        );
      case 24:
        qe(l, Lt, t.memoizedState.cache);
    }
    return se(t, l, e);
  }
  function xo(t, l, e) {
    if (t !== null)
      if (t.memoizedProps !== l.pendingProps)
        Qt = !0;
      else {
        if (!$c(t, e) && (l.flags & 128) === 0)
          return Qt = !1, Gm(
            t,
            l,
            e
          );
        Qt = (t.flags & 131072) !== 0;
      }
    else
      Qt = !1, ot && (l.flags & 1048576) !== 0 && ar(l, Au, l.index);
    switch (l.lanes = 0, l.tag) {
      case 16:
        t: {
          var a = l.pendingProps;
          if (t = ca(l.elementType), l.type = t, typeof t == "function")
            tc(t) ? (a = oa(t, a), l.tag = 1, l = _o(
              null,
              l,
              t,
              a,
              e
            )) : (l.tag = 0, l = Xc(
              null,
              l,
              t,
              a,
              e
            ));
          else {
            if (t != null) {
              var u = t.$$typeof;
              if (u === ut) {
                l.tag = 11, l = mo(
                  null,
                  l,
                  t,
                  a,
                  e
                );
                break t;
              } else if (u === it) {
                l.tag = 14, l = ho(
                  null,
                  l,
                  t,
                  a,
                  e
                );
                break t;
              }
            }
            throw l = Ll(t) || t, Error(s(306, l, ""));
          }
        }
        return l;
      case 0:
        return Xc(
          t,
          l,
          l.type,
          l.pendingProps,
          e
        );
      case 1:
        return a = l.type, u = oa(
          a,
          l.pendingProps
        ), _o(
          t,
          l,
          a,
          u,
          e
        );
      case 3:
        t: {
          if (Dt(
            l,
            l.stateNode.containerInfo
          ), t === null) throw Error(s(387));
          a = l.pendingProps;
          var n = l.memoizedState;
          u = n.element, hc(t, l), Du(l, a, null, e);
          var i = l.memoizedState;
          if (a = i.cache, qe(l, Lt, a), a !== n.cache && sc(
            l,
            [Lt],
            e,
            !0
          ), Mu(), a = i.element, n.isDehydrated)
            if (n = {
              element: a,
              isDehydrated: !1,
              cache: i.cache
            }, l.updateQueue.baseState = n, l.memoizedState = n, l.flags & 256) {
              l = Eo(
                t,
                l,
                a,
                e
              );
              break t;
            } else if (a !== u) {
              u = Ol(
                Error(s(424)),
                l
              ), zu(u), l = Eo(
                t,
                l,
                a,
                e
              );
              break t;
            } else {
              switch (t = l.stateNode.containerInfo, t.nodeType) {
                case 9:
                  t = t.body;
                  break;
                default:
                  t = t.nodeName === "HTML" ? t.ownerDocument.body : t;
              }
              for (qt = jl(t.firstChild), Wt = l, ot = !0, Te = null, Ul = !0, e = vr(
                l,
                null,
                a,
                e
              ), l.child = e; e; )
                e.flags = e.flags & -3 | 4096, e = e.sibling;
            }
          else {
            if (aa(), a === u) {
              l = se(
                t,
                l,
                e
              );
              break t;
            }
            It(t, l, a, e);
          }
          l = l.child;
        }
        return l;
      case 26:
        return wn(t, l), t === null ? (e = Bd(
          l.type,
          null,
          l.pendingProps,
          null
        )) ? l.memoizedState = e : ot || (e = l.type, t = l.pendingProps, a = ri(
          lt.current
        ).createElement(e), a[$t] = l, a[fl] = t, Pt(a, e, t), wt(a), l.stateNode = a) : l.memoizedState = Bd(
          l.type,
          t.memoizedProps,
          l.pendingProps,
          t.memoizedState
        ), null;
      case 27:
        return Se(l), t === null && ot && (a = l.stateNode = jd(
          l.type,
          l.pendingProps,
          lt.current
        ), Wt = l, Ul = !0, u = qt, Ge(l.type) ? (Of = u, qt = jl(a.firstChild)) : qt = u), It(
          t,
          l,
          l.pendingProps.children,
          e
        ), wn(t, l), t === null && (l.flags |= 4194304), l.child;
      case 5:
        return t === null && ot && ((u = a = qt) && (a = vh(
          a,
          l.type,
          l.pendingProps,
          Ul
        ), a !== null ? (l.stateNode = a, Wt = l, qt = jl(a.firstChild), Ul = !1, u = !0) : u = !1), u || xe(l)), Se(l), u = l.type, n = l.pendingProps, i = t !== null ? t.memoizedProps : null, a = n.children, zf(u, n) ? a = null : i !== null && zf(u, i) && (l.flags |= 32), l.memoizedState !== null && (u = Ec(
          t,
          l,
          Dm,
          null,
          null,
          e
        ), Fu._currentValue = u), wn(t, l), It(t, l, a, e), l.child;
      case 6:
        return t === null && ot && ((t = e = qt) && (e = gh(
          e,
          l.pendingProps,
          Ul
        ), e !== null ? (l.stateNode = e, Wt = l, qt = null, t = !0) : t = !1), t || xe(l)), null;
      case 13:
        return Ao(t, l, e);
      case 4:
        return Dt(
          l,
          l.stateNode.containerInfo
        ), a = l.pendingProps, t === null ? l.child = sa(
          l,
          null,
          a,
          e
        ) : It(t, l, a, e), l.child;
      case 11:
        return mo(
          t,
          l,
          l.type,
          l.pendingProps,
          e
        );
      case 7:
        return It(
          t,
          l,
          l.pendingProps,
          e
        ), l.child;
      case 8:
        return It(
          t,
          l,
          l.pendingProps.children,
          e
        ), l.child;
      case 12:
        return It(
          t,
          l,
          l.pendingProps.children,
          e
        ), l.child;
      case 10:
        return a = l.pendingProps, qe(l, l.type, a.value), It(t, l, a.children, e), l.child;
      case 9:
        return u = l.type._context, a = l.pendingProps.children, na(l), u = Ft(u), a = a(u), l.flags |= 1, It(t, l, a, e), l.child;
      case 14:
        return ho(
          t,
          l,
          l.type,
          l.pendingProps,
          e
        );
      case 15:
        return vo(
          t,
          l,
          l.type,
          l.pendingProps,
          e
        );
      case 19:
        return To(t, l, e);
      case 31:
        return Lm(t, l, e);
      case 22:
        return go(
          t,
          l,
          e,
          l.pendingProps
        );
      case 24:
        return na(l), a = Ft(Lt), t === null ? (u = dc(), u === null && (u = xt, n = rc(), u.pooledCache = n, n.refCount++, n !== null && (u.pooledCacheLanes |= e), u = n), l.memoizedState = { parent: a, cache: u }, mc(l), qe(l, Lt, u)) : ((t.lanes & e) !== 0 && (hc(t, l), Du(l, null, null, e), Mu()), u = t.memoizedState, n = l.memoizedState, u.parent !== a ? (u = { parent: a, cache: a }, l.memoizedState = u, l.lanes === 0 && (l.memoizedState = l.updateQueue.baseState = u), qe(l, Lt, a)) : (a = n.cache, qe(l, Lt, a), a !== u.cache && sc(
          l,
          [Lt],
          e,
          !0
        ))), It(
          t,
          l,
          l.pendingProps.children,
          e
        ), l.child;
      case 29:
        throw l.pendingProps;
    }
    throw Error(s(156, l.tag));
  }
  function re(t) {
    t.flags |= 4;
  }
  function Wc(t, l, e, a, u) {
    if ((l = (t.mode & 32) !== 0) && (l = !1), l) {
      if (t.flags |= 16777216, (u & 335544128) === u)
        if (t.stateNode.complete) t.flags |= 8192;
        else if (Po()) t.flags |= 8192;
        else
          throw fa = Un, yc;
    } else t.flags &= -16777217;
  }
  function qo(t, l) {
    if (l.type !== "stylesheet" || (l.state.loading & 4) !== 0)
      t.flags &= -16777217;
    else if (t.flags |= 16777216, !Xd(l))
      if (Po()) t.flags |= 8192;
      else
        throw fa = Un, yc;
  }
  function $n(t, l) {
    l !== null && (t.flags |= 4), t.flags & 16384 && (l = t.tag !== 22 ? ns() : 536870912, t.lanes |= l, wa |= l);
  }
  function Bu(t, l) {
    if (!ot)
      switch (t.tailMode) {
        case "hidden":
          l = t.tail;
          for (var e = null; l !== null; )
            l.alternate !== null && (e = l), l = l.sibling;
          e === null ? t.tail = null : e.sibling = null;
          break;
        case "collapsed":
          e = t.tail;
          for (var a = null; e !== null; )
            e.alternate !== null && (a = e), e = e.sibling;
          a === null ? l || t.tail === null ? t.tail = null : t.tail.sibling = null : a.sibling = null;
      }
  }
  function Nt(t) {
    var l = t.alternate !== null && t.alternate.child === t.child, e = 0, a = 0;
    if (l)
      for (var u = t.child; u !== null; )
        e |= u.lanes | u.childLanes, a |= u.subtreeFlags & 65011712, a |= u.flags & 65011712, u.return = t, u = u.sibling;
    else
      for (u = t.child; u !== null; )
        e |= u.lanes | u.childLanes, a |= u.subtreeFlags, a |= u.flags, u.return = t, u = u.sibling;
    return t.subtreeFlags |= a, t.childLanes = e, l;
  }
  function Qm(t, l, e) {
    var a = l.pendingProps;
    switch (uc(l), l.tag) {
      case 16:
      case 15:
      case 0:
      case 11:
      case 7:
      case 8:
      case 12:
      case 9:
      case 14:
        return Nt(l), null;
      case 1:
        return Nt(l), null;
      case 3:
        return e = l.stateNode, a = null, t !== null && (a = t.memoizedState.cache), l.memoizedState.cache !== a && (l.flags |= 2048), ie(Lt), Mt(), e.pendingContext && (e.context = e.pendingContext, e.pendingContext = null), (t === null || t.child === null) && (ja(l) ? re(l) : t === null || t.memoizedState.isDehydrated && (l.flags & 256) === 0 || (l.flags |= 1024, ic())), Nt(l), null;
      case 26:
        var u = l.type, n = l.memoizedState;
        return t === null ? (re(l), n !== null ? (Nt(l), qo(l, n)) : (Nt(l), Wc(
          l,
          u,
          null,
          a,
          e
        ))) : n ? n !== t.memoizedState ? (re(l), Nt(l), qo(l, n)) : (Nt(l), l.flags &= -16777217) : (t = t.memoizedProps, t !== a && re(l), Nt(l), Wc(
          l,
          u,
          t,
          a,
          e
        )), null;
      case 27:
        if (Kl(l), e = lt.current, u = l.type, t !== null && l.stateNode != null)
          t.memoizedProps !== a && re(l);
        else {
          if (!a) {
            if (l.stateNode === null)
              throw Error(s(166));
            return Nt(l), null;
          }
          t = B.current, ja(l) ? nr(l) : (t = jd(u, a, e), l.stateNode = t, re(l));
        }
        return Nt(l), null;
      case 5:
        if (Kl(l), u = l.type, t !== null && l.stateNode != null)
          t.memoizedProps !== a && re(l);
        else {
          if (!a) {
            if (l.stateNode === null)
              throw Error(s(166));
            return Nt(l), null;
          }
          if (n = B.current, ja(l))
            nr(l);
          else {
            var i = ri(
              lt.current
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
            n[$t] = l, n[fl] = a;
            t: for (i = l.child; i !== null; ) {
              if (i.tag === 5 || i.tag === 6)
                n.appendChild(i.stateNode);
              else if (i.tag !== 4 && i.tag !== 27 && i.child !== null) {
                i.child.return = i, i = i.child;
                continue;
              }
              if (i === l) break t;
              for (; i.sibling === null; ) {
                if (i.return === null || i.return === l)
                  break t;
                i = i.return;
              }
              i.sibling.return = i.return, i = i.sibling;
            }
            l.stateNode = n;
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
            a && re(l);
          }
        }
        return Nt(l), Wc(
          l,
          l.type,
          t === null ? null : t.memoizedProps,
          l.pendingProps,
          e
        ), null;
      case 6:
        if (t && l.stateNode != null)
          t.memoizedProps !== a && re(l);
        else {
          if (typeof a != "string" && l.stateNode === null)
            throw Error(s(166));
          if (t = lt.current, ja(l)) {
            if (t = l.stateNode, e = l.memoizedProps, a = null, u = Wt, u !== null)
              switch (u.tag) {
                case 27:
                case 5:
                  a = u.memoizedProps;
              }
            t[$t] = l, t = !!(t.nodeValue === e || a !== null && a.suppressHydrationWarning === !0 || Ad(t.nodeValue, e)), t || xe(l, !0);
          } else
            t = ri(t).createTextNode(
              a
            ), t[$t] = l, l.stateNode = t;
        }
        return Nt(l), null;
      case 31:
        if (e = l.memoizedState, t === null || t.memoizedState !== null) {
          if (a = ja(l), e !== null) {
            if (t === null) {
              if (!a) throw Error(s(318));
              if (t = l.memoizedState, t = t !== null ? t.dehydrated : null, !t) throw Error(s(557));
              t[$t] = l;
            } else
              aa(), (l.flags & 128) === 0 && (l.memoizedState = null), l.flags |= 4;
            Nt(l), t = !1;
          } else
            e = ic(), t !== null && t.memoizedState !== null && (t.memoizedState.hydrationErrors = e), t = !0;
          if (!t)
            return l.flags & 256 ? (El(l), l) : (El(l), null);
          if ((l.flags & 128) !== 0)
            throw Error(s(558));
        }
        return Nt(l), null;
      case 13:
        if (a = l.memoizedState, t === null || t.memoizedState !== null && t.memoizedState.dehydrated !== null) {
          if (u = ja(l), a !== null && a.dehydrated !== null) {
            if (t === null) {
              if (!u) throw Error(s(318));
              if (u = l.memoizedState, u = u !== null ? u.dehydrated : null, !u) throw Error(s(317));
              u[$t] = l;
            } else
              aa(), (l.flags & 128) === 0 && (l.memoizedState = null), l.flags |= 4;
            Nt(l), u = !1;
          } else
            u = ic(), t !== null && t.memoizedState !== null && (t.memoizedState.hydrationErrors = u), u = !0;
          if (!u)
            return l.flags & 256 ? (El(l), l) : (El(l), null);
        }
        return El(l), (l.flags & 128) !== 0 ? (l.lanes = e, l) : (e = a !== null, t = t !== null && t.memoizedState !== null, e && (a = l.child, u = null, a.alternate !== null && a.alternate.memoizedState !== null && a.alternate.memoizedState.cachePool !== null && (u = a.alternate.memoizedState.cachePool.pool), n = null, a.memoizedState !== null && a.memoizedState.cachePool !== null && (n = a.memoizedState.cachePool.pool), n !== u && (a.flags |= 2048)), e !== t && e && (l.child.flags |= 8192), $n(l, l.updateQueue), Nt(l), null);
      case 4:
        return Mt(), t === null && bf(l.stateNode.containerInfo), Nt(l), null;
      case 10:
        return ie(l.type), Nt(l), null;
      case 19:
        if (x(Ht), a = l.memoizedState, a === null) return Nt(l), null;
        if (u = (l.flags & 128) !== 0, n = a.rendering, n === null)
          if (u) Bu(a, !1);
          else {
            if (jt !== 0 || t !== null && (t.flags & 128) !== 0)
              for (t = l.child; t !== null; ) {
                if (n = Hn(t), n !== null) {
                  for (l.flags |= 128, Bu(a, !1), t = n.updateQueue, l.updateQueue = t, $n(l, t), l.subtreeFlags = 0, t = e, e = l.child; e !== null; )
                    tr(e, t), e = e.sibling;
                  return j(
                    Ht,
                    Ht.current & 1 | 2
                  ), ot && ue(l, a.treeForkCount), l.child;
                }
                t = t.sibling;
              }
            a.tail !== null && ll() > ti && (l.flags |= 128, u = !0, Bu(a, !1), l.lanes = 4194304);
          }
        else {
          if (!u)
            if (t = Hn(n), t !== null) {
              if (l.flags |= 128, u = !0, t = t.updateQueue, l.updateQueue = t, $n(l, t), Bu(a, !0), a.tail === null && a.tailMode === "hidden" && !n.alternate && !ot)
                return Nt(l), null;
            } else
              2 * ll() - a.renderingStartTime > ti && e !== 536870912 && (l.flags |= 128, u = !0, Bu(a, !1), l.lanes = 4194304);
          a.isBackwards ? (n.sibling = l.child, l.child = n) : (t = a.last, t !== null ? t.sibling = n : l.child = n, a.last = n);
        }
        return a.tail !== null ? (t = a.tail, a.rendering = t, a.tail = t.sibling, a.renderingStartTime = ll(), t.sibling = null, e = Ht.current, j(
          Ht,
          u ? e & 1 | 2 : e & 1
        ), ot && ue(l, a.treeForkCount), t) : (Nt(l), null);
      case 22:
      case 23:
        return El(l), bc(), a = l.memoizedState !== null, t !== null ? t.memoizedState !== null !== a && (l.flags |= 8192) : a && (l.flags |= 8192), a ? (e & 536870912) !== 0 && (l.flags & 128) === 0 && (Nt(l), l.subtreeFlags & 6 && (l.flags |= 8192)) : Nt(l), e = l.updateQueue, e !== null && $n(l, e.retryQueue), e = null, t !== null && t.memoizedState !== null && t.memoizedState.cachePool !== null && (e = t.memoizedState.cachePool.pool), a = null, l.memoizedState !== null && l.memoizedState.cachePool !== null && (a = l.memoizedState.cachePool.pool), a !== e && (l.flags |= 2048), t !== null && x(ia), null;
      case 24:
        return e = null, t !== null && (e = t.memoizedState.cache), l.memoizedState.cache !== e && (l.flags |= 2048), ie(Lt), Nt(l), null;
      case 25:
        return null;
      case 30:
        return null;
    }
    throw Error(s(156, l.tag));
  }
  function Xm(t, l) {
    switch (uc(l), l.tag) {
      case 1:
        return t = l.flags, t & 65536 ? (l.flags = t & -65537 | 128, l) : null;
      case 3:
        return ie(Lt), Mt(), t = l.flags, (t & 65536) !== 0 && (t & 128) === 0 ? (l.flags = t & -65537 | 128, l) : null;
      case 26:
      case 27:
      case 5:
        return Kl(l), null;
      case 31:
        if (l.memoizedState !== null) {
          if (El(l), l.alternate === null)
            throw Error(s(340));
          aa();
        }
        return t = l.flags, t & 65536 ? (l.flags = t & -65537 | 128, l) : null;
      case 13:
        if (El(l), t = l.memoizedState, t !== null && t.dehydrated !== null) {
          if (l.alternate === null)
            throw Error(s(340));
          aa();
        }
        return t = l.flags, t & 65536 ? (l.flags = t & -65537 | 128, l) : null;
      case 19:
        return x(Ht), null;
      case 4:
        return Mt(), null;
      case 10:
        return ie(l.type), null;
      case 22:
      case 23:
        return El(l), bc(), t !== null && x(ia), t = l.flags, t & 65536 ? (l.flags = t & -65537 | 128, l) : null;
      case 24:
        return ie(Lt), null;
      case 25:
        return null;
      default:
        return null;
    }
  }
  function No(t, l) {
    switch (uc(l), l.tag) {
      case 3:
        ie(Lt), Mt();
        break;
      case 26:
      case 27:
      case 5:
        Kl(l);
        break;
      case 4:
        Mt();
        break;
      case 31:
        l.memoizedState !== null && El(l);
        break;
      case 13:
        El(l);
        break;
      case 19:
        x(Ht);
        break;
      case 10:
        ie(l.type);
        break;
      case 22:
      case 23:
        El(l), bc(), t !== null && x(ia);
        break;
      case 24:
        ie(Lt);
    }
  }
  function Yu(t, l) {
    try {
      var e = l.updateQueue, a = e !== null ? e.lastEffect : null;
      if (a !== null) {
        var u = a.next;
        e = u;
        do {
          if ((e.tag & t) === t) {
            a = void 0;
            var n = e.create, i = e.inst;
            a = n(), i.destroy = a;
          }
          e = e.next;
        } while (e !== u);
      }
    } catch (f) {
      _t(l, l.return, f);
    }
  }
  function Ce(t, l, e) {
    try {
      var a = l.updateQueue, u = a !== null ? a.lastEffect : null;
      if (u !== null) {
        var n = u.next;
        a = n;
        do {
          if ((a.tag & t) === t) {
            var i = a.inst, f = i.destroy;
            if (f !== void 0) {
              i.destroy = void 0, u = l;
              var d = e, S = f;
              try {
                S();
              } catch (z) {
                _t(
                  u,
                  d,
                  z
                );
              }
            }
          }
          a = a.next;
        } while (a !== n);
      }
    } catch (z) {
      _t(l, l.return, z);
    }
  }
  function Oo(t) {
    var l = t.updateQueue;
    if (l !== null) {
      var e = t.stateNode;
      try {
        pr(l, e);
      } catch (a) {
        _t(t, t.return, a);
      }
    }
  }
  function Mo(t, l, e) {
    e.props = oa(
      t.type,
      t.memoizedProps
    ), e.state = t.memoizedState;
    try {
      e.componentWillUnmount();
    } catch (a) {
      _t(t, l, a);
    }
  }
  function Lu(t, l) {
    try {
      var e = t.ref;
      if (e !== null) {
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
        typeof e == "function" ? t.refCleanup = e(a) : e.current = a;
      }
    } catch (u) {
      _t(t, l, u);
    }
  }
  function $l(t, l) {
    var e = t.ref, a = t.refCleanup;
    if (e !== null)
      if (typeof a == "function")
        try {
          a();
        } catch (u) {
          _t(t, l, u);
        } finally {
          t.refCleanup = null, t = t.alternate, t != null && (t.refCleanup = null);
        }
      else if (typeof e == "function")
        try {
          e(null);
        } catch (u) {
          _t(t, l, u);
        }
      else e.current = null;
  }
  function Do(t) {
    var l = t.type, e = t.memoizedProps, a = t.stateNode;
    try {
      t: switch (l) {
        case "button":
        case "input":
        case "select":
        case "textarea":
          e.autoFocus && a.focus();
          break t;
        case "img":
          e.src ? a.src = e.src : e.srcSet && (a.srcset = e.srcSet);
      }
    } catch (u) {
      _t(t, t.return, u);
    }
  }
  function Fc(t, l, e) {
    try {
      var a = t.stateNode;
      rh(a, t.type, e, l), a[fl] = l;
    } catch (u) {
      _t(t, t.return, u);
    }
  }
  function Uo(t) {
    return t.tag === 5 || t.tag === 3 || t.tag === 26 || t.tag === 27 && Ge(t.type) || t.tag === 4;
  }
  function Ic(t) {
    t: for (; ; ) {
      for (; t.sibling === null; ) {
        if (t.return === null || Uo(t.return)) return null;
        t = t.return;
      }
      for (t.sibling.return = t.return, t = t.sibling; t.tag !== 5 && t.tag !== 6 && t.tag !== 18; ) {
        if (t.tag === 27 && Ge(t.type) || t.flags & 2 || t.child === null || t.tag === 4) continue t;
        t.child.return = t, t = t.child;
      }
      if (!(t.flags & 2)) return t.stateNode;
    }
  }
  function Pc(t, l, e) {
    var a = t.tag;
    if (a === 5 || a === 6)
      t = t.stateNode, l ? (e.nodeType === 9 ? e.body : e.nodeName === "HTML" ? e.ownerDocument.body : e).insertBefore(t, l) : (l = e.nodeType === 9 ? e.body : e.nodeName === "HTML" ? e.ownerDocument.body : e, l.appendChild(t), e = e._reactRootContainer, e != null || l.onclick !== null || (l.onclick = le));
    else if (a !== 4 && (a === 27 && Ge(t.type) && (e = t.stateNode, l = null), t = t.child, t !== null))
      for (Pc(t, l, e), t = t.sibling; t !== null; )
        Pc(t, l, e), t = t.sibling;
  }
  function Wn(t, l, e) {
    var a = t.tag;
    if (a === 5 || a === 6)
      t = t.stateNode, l ? e.insertBefore(t, l) : e.appendChild(t);
    else if (a !== 4 && (a === 27 && Ge(t.type) && (e = t.stateNode), t = t.child, t !== null))
      for (Wn(t, l, e), t = t.sibling; t !== null; )
        Wn(t, l, e), t = t.sibling;
  }
  function Co(t) {
    var l = t.stateNode, e = t.memoizedProps;
    try {
      for (var a = t.type, u = l.attributes; u.length; )
        l.removeAttributeNode(u[0]);
      Pt(l, a, e), l[$t] = t, l[fl] = e;
    } catch (n) {
      _t(t, t.return, n);
    }
  }
  var oe = !1, Xt = !1, tf = !1, jo = typeof WeakSet == "function" ? WeakSet : Set, kt = null;
  function Zm(t, l) {
    if (t = t.containerInfo, Ef = gi, t = Ks(t), wi(t)) {
      if ("selectionStart" in t)
        var e = {
          start: t.selectionStart,
          end: t.selectionEnd
        };
      else
        t: {
          e = (e = t.ownerDocument) && e.defaultView || window;
          var a = e.getSelection && e.getSelection();
          if (a && a.rangeCount !== 0) {
            e = a.anchorNode;
            var u = a.anchorOffset, n = a.focusNode;
            a = a.focusOffset;
            try {
              e.nodeType, n.nodeType;
            } catch {
              e = null;
              break t;
            }
            var i = 0, f = -1, d = -1, S = 0, z = 0, M = t, _ = null;
            l: for (; ; ) {
              for (var A; M !== e || u !== 0 && M.nodeType !== 3 || (f = i + u), M !== n || a !== 0 && M.nodeType !== 3 || (d = i + a), M.nodeType === 3 && (i += M.nodeValue.length), (A = M.firstChild) !== null; )
                _ = M, M = A;
              for (; ; ) {
                if (M === t) break l;
                if (_ === e && ++S === u && (f = i), _ === n && ++z === a && (d = i), (A = M.nextSibling) !== null) break;
                M = _, _ = M.parentNode;
              }
              M = A;
            }
            e = f === -1 || d === -1 ? null : { start: f, end: d };
          } else e = null;
        }
      e = e || { start: 0, end: 0 };
    } else e = null;
    for (Af = { focusedElem: t, selectionRange: e }, gi = !1, kt = l; kt !== null; )
      if (l = kt, t = l.child, (l.subtreeFlags & 1028) !== 0 && t !== null)
        t.return = l, kt = t;
      else
        for (; kt !== null; ) {
          switch (l = kt, n = l.alternate, t = l.flags, l.tag) {
            case 0:
              if ((t & 4) !== 0 && (t = l.updateQueue, t = t !== null ? t.events : null, t !== null))
                for (e = 0; e < t.length; e++)
                  u = t[e], u.ref.impl = u.nextImpl;
              break;
            case 11:
            case 15:
              break;
            case 1:
              if ((t & 1024) !== 0 && n !== null) {
                t = void 0, e = l, u = n.memoizedProps, n = n.memoizedState, a = e.stateNode;
                try {
                  var Y = oa(
                    e.type,
                    u
                  );
                  t = a.getSnapshotBeforeUpdate(
                    Y,
                    n
                  ), a.__reactInternalSnapshotBeforeUpdate = t;
                } catch (k) {
                  _t(
                    e,
                    e.return,
                    k
                  );
                }
              }
              break;
            case 3:
              if ((t & 1024) !== 0) {
                if (t = l.stateNode.containerInfo, e = t.nodeType, e === 9)
                  xf(t);
                else if (e === 1)
                  switch (t.nodeName) {
                    case "HEAD":
                    case "HTML":
                    case "BODY":
                      xf(t);
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
          if (t = l.sibling, t !== null) {
            t.return = l.return, kt = t;
            break;
          }
          kt = l.return;
        }
  }
  function Ro(t, l, e) {
    var a = e.flags;
    switch (e.tag) {
      case 0:
      case 11:
      case 15:
        ye(t, e), a & 4 && Yu(5, e);
        break;
      case 1:
        if (ye(t, e), a & 4)
          if (t = e.stateNode, l === null)
            try {
              t.componentDidMount();
            } catch (i) {
              _t(e, e.return, i);
            }
          else {
            var u = oa(
              e.type,
              l.memoizedProps
            );
            l = l.memoizedState;
            try {
              t.componentDidUpdate(
                u,
                l,
                t.__reactInternalSnapshotBeforeUpdate
              );
            } catch (i) {
              _t(
                e,
                e.return,
                i
              );
            }
          }
        a & 64 && Oo(e), a & 512 && Lu(e, e.return);
        break;
      case 3:
        if (ye(t, e), a & 64 && (t = e.updateQueue, t !== null)) {
          if (l = null, e.child !== null)
            switch (e.child.tag) {
              case 27:
              case 5:
                l = e.child.stateNode;
                break;
              case 1:
                l = e.child.stateNode;
            }
          try {
            pr(t, l);
          } catch (i) {
            _t(e, e.return, i);
          }
        }
        break;
      case 27:
        l === null && a & 4 && Co(e);
      case 26:
      case 5:
        ye(t, e), l === null && a & 4 && Do(e), a & 512 && Lu(e, e.return);
        break;
      case 12:
        ye(t, e);
        break;
      case 31:
        ye(t, e), a & 4 && Yo(t, e);
        break;
      case 13:
        ye(t, e), a & 4 && Lo(t, e), a & 64 && (t = e.memoizedState, t !== null && (t = t.dehydrated, t !== null && (e = Im.bind(
          null,
          e
        ), ph(t, e))));
        break;
      case 22:
        if (a = e.memoizedState !== null || oe, !a) {
          l = l !== null && l.memoizedState !== null || Xt, u = oe;
          var n = Xt;
          oe = a, (Xt = l) && !n ? me(
            t,
            e,
            (e.subtreeFlags & 8772) !== 0
          ) : ye(t, e), oe = u, Xt = n;
        }
        break;
      case 30:
        break;
      default:
        ye(t, e);
    }
  }
  function Ho(t) {
    var l = t.alternate;
    l !== null && (t.alternate = null, Ho(l)), t.child = null, t.deletions = null, t.sibling = null, t.tag === 5 && (l = t.stateNode, l !== null && Mi(l)), t.stateNode = null, t.return = null, t.dependencies = null, t.memoizedProps = null, t.memoizedState = null, t.pendingProps = null, t.stateNode = null, t.updateQueue = null;
  }
  var Ot = null, rl = !1;
  function de(t, l, e) {
    for (e = e.child; e !== null; )
      Bo(t, l, e), e = e.sibling;
  }
  function Bo(t, l, e) {
    if (gl && typeof gl.onCommitFiberUnmount == "function")
      try {
        gl.onCommitFiberUnmount(su, e);
      } catch {
      }
    switch (e.tag) {
      case 26:
        Xt || $l(e, l), de(
          t,
          l,
          e
        ), e.memoizedState ? e.memoizedState.count-- : e.stateNode && (e = e.stateNode, e.parentNode.removeChild(e));
        break;
      case 27:
        Xt || $l(e, l);
        var a = Ot, u = rl;
        Ge(e.type) && (Ot = e.stateNode, rl = !1), de(
          t,
          l,
          e
        ), ku(e.stateNode), Ot = a, rl = u;
        break;
      case 5:
        Xt || $l(e, l);
      case 6:
        if (a = Ot, u = rl, Ot = null, de(
          t,
          l,
          e
        ), Ot = a, rl = u, Ot !== null)
          if (rl)
            try {
              (Ot.nodeType === 9 ? Ot.body : Ot.nodeName === "HTML" ? Ot.ownerDocument.body : Ot).removeChild(e.stateNode);
            } catch (n) {
              _t(
                e,
                l,
                n
              );
            }
          else
            try {
              Ot.removeChild(e.stateNode);
            } catch (n) {
              _t(
                e,
                l,
                n
              );
            }
        break;
      case 18:
        Ot !== null && (rl ? (t = Ot, Od(
          t.nodeType === 9 ? t.body : t.nodeName === "HTML" ? t.ownerDocument.body : t,
          e.stateNode
        ), lu(t)) : Od(Ot, e.stateNode));
        break;
      case 4:
        a = Ot, u = rl, Ot = e.stateNode.containerInfo, rl = !0, de(
          t,
          l,
          e
        ), Ot = a, rl = u;
        break;
      case 0:
      case 11:
      case 14:
      case 15:
        Ce(2, e, l), Xt || Ce(4, e, l), de(
          t,
          l,
          e
        );
        break;
      case 1:
        Xt || ($l(e, l), a = e.stateNode, typeof a.componentWillUnmount == "function" && Mo(
          e,
          l,
          a
        )), de(
          t,
          l,
          e
        );
        break;
      case 21:
        de(
          t,
          l,
          e
        );
        break;
      case 22:
        Xt = (a = Xt) || e.memoizedState !== null, de(
          t,
          l,
          e
        ), Xt = a;
        break;
      default:
        de(
          t,
          l,
          e
        );
    }
  }
  function Yo(t, l) {
    if (l.memoizedState === null && (t = l.alternate, t !== null && (t = t.memoizedState, t !== null))) {
      t = t.dehydrated;
      try {
        lu(t);
      } catch (e) {
        _t(l, l.return, e);
      }
    }
  }
  function Lo(t, l) {
    if (l.memoizedState === null && (t = l.alternate, t !== null && (t = t.memoizedState, t !== null && (t = t.dehydrated, t !== null))))
      try {
        lu(t);
      } catch (e) {
        _t(l, l.return, e);
      }
  }
  function Vm(t) {
    switch (t.tag) {
      case 31:
      case 13:
      case 19:
        var l = t.stateNode;
        return l === null && (l = t.stateNode = new jo()), l;
      case 22:
        return t = t.stateNode, l = t._retryCache, l === null && (l = t._retryCache = new jo()), l;
      default:
        throw Error(s(435, t.tag));
    }
  }
  function Fn(t, l) {
    var e = Vm(t);
    l.forEach(function(a) {
      if (!e.has(a)) {
        e.add(a);
        var u = Pm.bind(null, t, a);
        a.then(u, u);
      }
    });
  }
  function ol(t, l) {
    var e = l.deletions;
    if (e !== null)
      for (var a = 0; a < e.length; a++) {
        var u = e[a], n = t, i = l, f = i;
        t: for (; f !== null; ) {
          switch (f.tag) {
            case 27:
              if (Ge(f.type)) {
                Ot = f.stateNode, rl = !1;
                break t;
              }
              break;
            case 5:
              Ot = f.stateNode, rl = !1;
              break t;
            case 3:
            case 4:
              Ot = f.stateNode.containerInfo, rl = !0;
              break t;
          }
          f = f.return;
        }
        if (Ot === null) throw Error(s(160));
        Bo(n, i, u), Ot = null, rl = !1, n = u.alternate, n !== null && (n.return = null), u.return = null;
      }
    if (l.subtreeFlags & 13886)
      for (l = l.child; l !== null; )
        Go(l, t), l = l.sibling;
  }
  var Ql = null;
  function Go(t, l) {
    var e = t.alternate, a = t.flags;
    switch (t.tag) {
      case 0:
      case 11:
      case 14:
      case 15:
        ol(l, t), dl(t), a & 4 && (Ce(3, t, t.return), Yu(3, t), Ce(5, t, t.return));
        break;
      case 1:
        ol(l, t), dl(t), a & 512 && (Xt || e === null || $l(e, e.return)), a & 64 && oe && (t = t.updateQueue, t !== null && (a = t.callbacks, a !== null && (e = t.shared.hiddenCallbacks, t.shared.hiddenCallbacks = e === null ? a : e.concat(a))));
        break;
      case 26:
        var u = Ql;
        if (ol(l, t), dl(t), a & 512 && (Xt || e === null || $l(e, e.return)), a & 4) {
          var n = e !== null ? e.memoizedState : null;
          if (a = t.memoizedState, e === null)
            if (a === null)
              if (t.stateNode === null) {
                t: {
                  a = t.type, e = t.memoizedProps, u = u.ownerDocument || u;
                  l: switch (a) {
                    case "title":
                      n = u.getElementsByTagName("title")[0], (!n || n[du] || n[$t] || n.namespaceURI === "http://www.w3.org/2000/svg" || n.hasAttribute("itemprop")) && (n = u.createElement(a), u.head.insertBefore(
                        n,
                        u.querySelector("head > title")
                      )), Pt(n, a, e), n[$t] = t, wt(n), a = n;
                      break t;
                    case "link":
                      var i = Gd(
                        "link",
                        "href",
                        u
                      ).get(a + (e.href || ""));
                      if (i) {
                        for (var f = 0; f < i.length; f++)
                          if (n = i[f], n.getAttribute("href") === (e.href == null || e.href === "" ? null : e.href) && n.getAttribute("rel") === (e.rel == null ? null : e.rel) && n.getAttribute("title") === (e.title == null ? null : e.title) && n.getAttribute("crossorigin") === (e.crossOrigin == null ? null : e.crossOrigin)) {
                            i.splice(f, 1);
                            break l;
                          }
                      }
                      n = u.createElement(a), Pt(n, a, e), u.head.appendChild(n);
                      break;
                    case "meta":
                      if (i = Gd(
                        "meta",
                        "content",
                        u
                      ).get(a + (e.content || ""))) {
                        for (f = 0; f < i.length; f++)
                          if (n = i[f], n.getAttribute("content") === (e.content == null ? null : "" + e.content) && n.getAttribute("name") === (e.name == null ? null : e.name) && n.getAttribute("property") === (e.property == null ? null : e.property) && n.getAttribute("http-equiv") === (e.httpEquiv == null ? null : e.httpEquiv) && n.getAttribute("charset") === (e.charSet == null ? null : e.charSet)) {
                            i.splice(f, 1);
                            break l;
                          }
                      }
                      n = u.createElement(a), Pt(n, a, e), u.head.appendChild(n);
                      break;
                    default:
                      throw Error(s(468, a));
                  }
                  n[$t] = t, wt(n), a = n;
                }
                t.stateNode = a;
              } else
                Qd(
                  u,
                  t.type,
                  t.stateNode
                );
            else
              t.stateNode = Ld(
                u,
                a,
                t.memoizedProps
              );
          else
            n !== a ? (n === null ? e.stateNode !== null && (e = e.stateNode, e.parentNode.removeChild(e)) : n.count--, a === null ? Qd(
              u,
              t.type,
              t.stateNode
            ) : Ld(
              u,
              a,
              t.memoizedProps
            )) : a === null && t.stateNode !== null && Fc(
              t,
              t.memoizedProps,
              e.memoizedProps
            );
        }
        break;
      case 27:
        ol(l, t), dl(t), a & 512 && (Xt || e === null || $l(e, e.return)), e !== null && a & 4 && Fc(
          t,
          t.memoizedProps,
          e.memoizedProps
        );
        break;
      case 5:
        if (ol(l, t), dl(t), a & 512 && (Xt || e === null || $l(e, e.return)), t.flags & 32) {
          u = t.stateNode;
          try {
            za(u, "");
          } catch (Y) {
            _t(t, t.return, Y);
          }
        }
        a & 4 && t.stateNode != null && (u = t.memoizedProps, Fc(
          t,
          u,
          e !== null ? e.memoizedProps : u
        )), a & 1024 && (tf = !0);
        break;
      case 6:
        if (ol(l, t), dl(t), a & 4) {
          if (t.stateNode === null)
            throw Error(s(162));
          a = t.memoizedProps, e = t.stateNode;
          try {
            e.nodeValue = a;
          } catch (Y) {
            _t(t, t.return, Y);
          }
        }
        break;
      case 3:
        if (yi = null, u = Ql, Ql = oi(l.containerInfo), ol(l, t), Ql = u, dl(t), a & 4 && e !== null && e.memoizedState.isDehydrated)
          try {
            lu(l.containerInfo);
          } catch (Y) {
            _t(t, t.return, Y);
          }
        tf && (tf = !1, Qo(t));
        break;
      case 4:
        a = Ql, Ql = oi(
          t.stateNode.containerInfo
        ), ol(l, t), dl(t), Ql = a;
        break;
      case 12:
        ol(l, t), dl(t);
        break;
      case 31:
        ol(l, t), dl(t), a & 4 && (a = t.updateQueue, a !== null && (t.updateQueue = null, Fn(t, a)));
        break;
      case 13:
        ol(l, t), dl(t), t.child.flags & 8192 && t.memoizedState !== null != (e !== null && e.memoizedState !== null) && (Pn = ll()), a & 4 && (a = t.updateQueue, a !== null && (t.updateQueue = null, Fn(t, a)));
        break;
      case 22:
        u = t.memoizedState !== null;
        var d = e !== null && e.memoizedState !== null, S = oe, z = Xt;
        if (oe = S || u, Xt = z || d, ol(l, t), Xt = z, oe = S, dl(t), a & 8192)
          t: for (l = t.stateNode, l._visibility = u ? l._visibility & -2 : l._visibility | 1, u && (e === null || d || oe || Xt || da(t)), e = null, l = t; ; ) {
            if (l.tag === 5 || l.tag === 26) {
              if (e === null) {
                d = e = l;
                try {
                  if (n = d.stateNode, u)
                    i = n.style, typeof i.setProperty == "function" ? i.setProperty("display", "none", "important") : i.display = "none";
                  else {
                    f = d.stateNode;
                    var M = d.memoizedProps.style, _ = M != null && M.hasOwnProperty("display") ? M.display : null;
                    f.style.display = _ == null || typeof _ == "boolean" ? "" : ("" + _).trim();
                  }
                } catch (Y) {
                  _t(d, d.return, Y);
                }
              }
            } else if (l.tag === 6) {
              if (e === null) {
                d = l;
                try {
                  d.stateNode.nodeValue = u ? "" : d.memoizedProps;
                } catch (Y) {
                  _t(d, d.return, Y);
                }
              }
            } else if (l.tag === 18) {
              if (e === null) {
                d = l;
                try {
                  var A = d.stateNode;
                  u ? Md(A, !0) : Md(d.stateNode, !1);
                } catch (Y) {
                  _t(d, d.return, Y);
                }
              }
            } else if ((l.tag !== 22 && l.tag !== 23 || l.memoizedState === null || l === t) && l.child !== null) {
              l.child.return = l, l = l.child;
              continue;
            }
            if (l === t) break t;
            for (; l.sibling === null; ) {
              if (l.return === null || l.return === t) break t;
              e === l && (e = null), l = l.return;
            }
            e === l && (e = null), l.sibling.return = l.return, l = l.sibling;
          }
        a & 4 && (a = t.updateQueue, a !== null && (e = a.retryQueue, e !== null && (a.retryQueue = null, Fn(t, e))));
        break;
      case 19:
        ol(l, t), dl(t), a & 4 && (a = t.updateQueue, a !== null && (t.updateQueue = null, Fn(t, a)));
        break;
      case 30:
        break;
      case 21:
        break;
      default:
        ol(l, t), dl(t);
    }
  }
  function dl(t) {
    var l = t.flags;
    if (l & 2) {
      try {
        for (var e, a = t.return; a !== null; ) {
          if (Uo(a)) {
            e = a;
            break;
          }
          a = a.return;
        }
        if (e == null) throw Error(s(160));
        switch (e.tag) {
          case 27:
            var u = e.stateNode, n = Ic(t);
            Wn(t, n, u);
            break;
          case 5:
            var i = e.stateNode;
            e.flags & 32 && (za(i, ""), e.flags &= -33);
            var f = Ic(t);
            Wn(t, f, i);
            break;
          case 3:
          case 4:
            var d = e.stateNode.containerInfo, S = Ic(t);
            Pc(
              t,
              S,
              d
            );
            break;
          default:
            throw Error(s(161));
        }
      } catch (z) {
        _t(t, t.return, z);
      }
      t.flags &= -3;
    }
    l & 4096 && (t.flags &= -4097);
  }
  function Qo(t) {
    if (t.subtreeFlags & 1024)
      for (t = t.child; t !== null; ) {
        var l = t;
        Qo(l), l.tag === 5 && l.flags & 1024 && l.stateNode.reset(), t = t.sibling;
      }
  }
  function ye(t, l) {
    if (l.subtreeFlags & 8772)
      for (l = l.child; l !== null; )
        Ro(t, l.alternate, l), l = l.sibling;
  }
  function da(t) {
    for (t = t.child; t !== null; ) {
      var l = t;
      switch (l.tag) {
        case 0:
        case 11:
        case 14:
        case 15:
          Ce(4, l, l.return), da(l);
          break;
        case 1:
          $l(l, l.return);
          var e = l.stateNode;
          typeof e.componentWillUnmount == "function" && Mo(
            l,
            l.return,
            e
          ), da(l);
          break;
        case 27:
          ku(l.stateNode);
        case 26:
        case 5:
          $l(l, l.return), da(l);
          break;
        case 22:
          l.memoizedState === null && da(l);
          break;
        case 30:
          da(l);
          break;
        default:
          da(l);
      }
      t = t.sibling;
    }
  }
  function me(t, l, e) {
    for (e = e && (l.subtreeFlags & 8772) !== 0, l = l.child; l !== null; ) {
      var a = l.alternate, u = t, n = l, i = n.flags;
      switch (n.tag) {
        case 0:
        case 11:
        case 15:
          me(
            u,
            n,
            e
          ), Yu(4, n);
          break;
        case 1:
          if (me(
            u,
            n,
            e
          ), a = n, u = a.stateNode, typeof u.componentDidMount == "function")
            try {
              u.componentDidMount();
            } catch (S) {
              _t(a, a.return, S);
            }
          if (a = n, u = a.updateQueue, u !== null) {
            var f = a.stateNode;
            try {
              var d = u.shared.hiddenCallbacks;
              if (d !== null)
                for (u.shared.hiddenCallbacks = null, u = 0; u < d.length; u++)
                  gr(d[u], f);
            } catch (S) {
              _t(a, a.return, S);
            }
          }
          e && i & 64 && Oo(n), Lu(n, n.return);
          break;
        case 27:
          Co(n);
        case 26:
        case 5:
          me(
            u,
            n,
            e
          ), e && a === null && i & 4 && Do(n), Lu(n, n.return);
          break;
        case 12:
          me(
            u,
            n,
            e
          );
          break;
        case 31:
          me(
            u,
            n,
            e
          ), e && i & 4 && Yo(u, n);
          break;
        case 13:
          me(
            u,
            n,
            e
          ), e && i & 4 && Lo(u, n);
          break;
        case 22:
          n.memoizedState === null && me(
            u,
            n,
            e
          ), Lu(n, n.return);
          break;
        case 30:
          break;
        default:
          me(
            u,
            n,
            e
          );
      }
      l = l.sibling;
    }
  }
  function lf(t, l) {
    var e = null;
    t !== null && t.memoizedState !== null && t.memoizedState.cachePool !== null && (e = t.memoizedState.cachePool.pool), t = null, l.memoizedState !== null && l.memoizedState.cachePool !== null && (t = l.memoizedState.cachePool.pool), t !== e && (t != null && t.refCount++, e != null && Tu(e));
  }
  function ef(t, l) {
    t = null, l.alternate !== null && (t = l.alternate.memoizedState.cache), l = l.memoizedState.cache, l !== t && (l.refCount++, t != null && Tu(t));
  }
  function Xl(t, l, e, a) {
    if (l.subtreeFlags & 10256)
      for (l = l.child; l !== null; )
        Xo(
          t,
          l,
          e,
          a
        ), l = l.sibling;
  }
  function Xo(t, l, e, a) {
    var u = l.flags;
    switch (l.tag) {
      case 0:
      case 11:
      case 15:
        Xl(
          t,
          l,
          e,
          a
        ), u & 2048 && Yu(9, l);
        break;
      case 1:
        Xl(
          t,
          l,
          e,
          a
        );
        break;
      case 3:
        Xl(
          t,
          l,
          e,
          a
        ), u & 2048 && (t = null, l.alternate !== null && (t = l.alternate.memoizedState.cache), l = l.memoizedState.cache, l !== t && (l.refCount++, t != null && Tu(t)));
        break;
      case 12:
        if (u & 2048) {
          Xl(
            t,
            l,
            e,
            a
          ), t = l.stateNode;
          try {
            var n = l.memoizedProps, i = n.id, f = n.onPostCommit;
            typeof f == "function" && f(
              i,
              l.alternate === null ? "mount" : "update",
              t.passiveEffectDuration,
              -0
            );
          } catch (d) {
            _t(l, l.return, d);
          }
        } else
          Xl(
            t,
            l,
            e,
            a
          );
        break;
      case 31:
        Xl(
          t,
          l,
          e,
          a
        );
        break;
      case 13:
        Xl(
          t,
          l,
          e,
          a
        );
        break;
      case 23:
        break;
      case 22:
        n = l.stateNode, i = l.alternate, l.memoizedState !== null ? n._visibility & 2 ? Xl(
          t,
          l,
          e,
          a
        ) : Gu(t, l) : n._visibility & 2 ? Xl(
          t,
          l,
          e,
          a
        ) : (n._visibility |= 2, Va(
          t,
          l,
          e,
          a,
          (l.subtreeFlags & 10256) !== 0 || !1
        )), u & 2048 && lf(i, l);
        break;
      case 24:
        Xl(
          t,
          l,
          e,
          a
        ), u & 2048 && ef(l.alternate, l);
        break;
      default:
        Xl(
          t,
          l,
          e,
          a
        );
    }
  }
  function Va(t, l, e, a, u) {
    for (u = u && ((l.subtreeFlags & 10256) !== 0 || !1), l = l.child; l !== null; ) {
      var n = t, i = l, f = e, d = a, S = i.flags;
      switch (i.tag) {
        case 0:
        case 11:
        case 15:
          Va(
            n,
            i,
            f,
            d,
            u
          ), Yu(8, i);
          break;
        case 23:
          break;
        case 22:
          var z = i.stateNode;
          i.memoizedState !== null ? z._visibility & 2 ? Va(
            n,
            i,
            f,
            d,
            u
          ) : Gu(
            n,
            i
          ) : (z._visibility |= 2, Va(
            n,
            i,
            f,
            d,
            u
          )), u && S & 2048 && lf(
            i.alternate,
            i
          );
          break;
        case 24:
          Va(
            n,
            i,
            f,
            d,
            u
          ), u && S & 2048 && ef(i.alternate, i);
          break;
        default:
          Va(
            n,
            i,
            f,
            d,
            u
          );
      }
      l = l.sibling;
    }
  }
  function Gu(t, l) {
    if (l.subtreeFlags & 10256)
      for (l = l.child; l !== null; ) {
        var e = t, a = l, u = a.flags;
        switch (a.tag) {
          case 22:
            Gu(e, a), u & 2048 && lf(
              a.alternate,
              a
            );
            break;
          case 24:
            Gu(e, a), u & 2048 && ef(a.alternate, a);
            break;
          default:
            Gu(e, a);
        }
        l = l.sibling;
      }
  }
  var Qu = 8192;
  function Ka(t, l, e) {
    if (t.subtreeFlags & Qu)
      for (t = t.child; t !== null; )
        Zo(
          t,
          l,
          e
        ), t = t.sibling;
  }
  function Zo(t, l, e) {
    switch (t.tag) {
      case 26:
        Ka(
          t,
          l,
          e
        ), t.flags & Qu && t.memoizedState !== null && Mh(
          e,
          Ql,
          t.memoizedState,
          t.memoizedProps
        );
        break;
      case 5:
        Ka(
          t,
          l,
          e
        );
        break;
      case 3:
      case 4:
        var a = Ql;
        Ql = oi(t.stateNode.containerInfo), Ka(
          t,
          l,
          e
        ), Ql = a;
        break;
      case 22:
        t.memoizedState === null && (a = t.alternate, a !== null && a.memoizedState !== null ? (a = Qu, Qu = 16777216, Ka(
          t,
          l,
          e
        ), Qu = a) : Ka(
          t,
          l,
          e
        ));
        break;
      default:
        Ka(
          t,
          l,
          e
        );
    }
  }
  function Vo(t) {
    var l = t.alternate;
    if (l !== null && (t = l.child, t !== null)) {
      l.child = null;
      do
        l = t.sibling, t.sibling = null, t = l;
      while (t !== null);
    }
  }
  function Xu(t) {
    var l = t.deletions;
    if ((t.flags & 16) !== 0) {
      if (l !== null)
        for (var e = 0; e < l.length; e++) {
          var a = l[e];
          kt = a, Jo(
            a,
            t
          );
        }
      Vo(t);
    }
    if (t.subtreeFlags & 10256)
      for (t = t.child; t !== null; )
        Ko(t), t = t.sibling;
  }
  function Ko(t) {
    switch (t.tag) {
      case 0:
      case 11:
      case 15:
        Xu(t), t.flags & 2048 && Ce(9, t, t.return);
        break;
      case 3:
        Xu(t);
        break;
      case 12:
        Xu(t);
        break;
      case 22:
        var l = t.stateNode;
        t.memoizedState !== null && l._visibility & 2 && (t.return === null || t.return.tag !== 13) ? (l._visibility &= -3, In(t)) : Xu(t);
        break;
      default:
        Xu(t);
    }
  }
  function In(t) {
    var l = t.deletions;
    if ((t.flags & 16) !== 0) {
      if (l !== null)
        for (var e = 0; e < l.length; e++) {
          var a = l[e];
          kt = a, Jo(
            a,
            t
          );
        }
      Vo(t);
    }
    for (t = t.child; t !== null; ) {
      switch (l = t, l.tag) {
        case 0:
        case 11:
        case 15:
          Ce(8, l, l.return), In(l);
          break;
        case 22:
          e = l.stateNode, e._visibility & 2 && (e._visibility &= -3, In(l));
          break;
        default:
          In(l);
      }
      t = t.sibling;
    }
  }
  function Jo(t, l) {
    for (; kt !== null; ) {
      var e = kt;
      switch (e.tag) {
        case 0:
        case 11:
        case 15:
          Ce(8, e, l);
          break;
        case 23:
        case 22:
          if (e.memoizedState !== null && e.memoizedState.cachePool !== null) {
            var a = e.memoizedState.cachePool.pool;
            a != null && a.refCount++;
          }
          break;
        case 24:
          Tu(e.memoizedState.cache);
      }
      if (a = e.child, a !== null) a.return = e, kt = a;
      else
        t: for (e = t; kt !== null; ) {
          a = kt;
          var u = a.sibling, n = a.return;
          if (Ho(a), a === e) {
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
  var Km = {
    getCacheForType: function(t) {
      var l = Ft(Lt), e = l.data.get(t);
      return e === void 0 && (e = t(), l.data.set(t, e)), e;
    },
    cacheSignal: function() {
      return Ft(Lt).controller.signal;
    }
  }, Jm = typeof WeakMap == "function" ? WeakMap : Map, gt = 0, xt = null, ct = null, st = 0, St = 0, Al = null, je = !1, Ja = !1, af = !1, he = 0, jt = 0, Re = 0, ya = 0, uf = 0, zl = 0, wa = 0, Zu = null, yl = null, nf = !1, Pn = 0, wo = 0, ti = 1 / 0, li = null, He = null, Vt = 0, Be = null, ka = null, ve = 0, cf = 0, ff = null, ko = null, Vu = 0, sf = null;
  function Tl() {
    return (gt & 2) !== 0 && st !== 0 ? st & -st : T.T !== null ? hf() : ss();
  }
  function $o() {
    if (zl === 0)
      if ((st & 536870912) === 0 || ot) {
        var t = sn;
        sn <<= 1, (sn & 3932160) === 0 && (sn = 262144), zl = t;
      } else zl = 536870912;
    return t = _l.current, t !== null && (t.flags |= 32), zl;
  }
  function ml(t, l, e) {
    (t === xt && (St === 2 || St === 9) || t.cancelPendingCommit !== null) && ($a(t, 0), Ye(
      t,
      st,
      zl,
      !1
    )), ou(t, e), ((gt & 2) === 0 || t !== xt) && (t === xt && ((gt & 2) === 0 && (ya |= e), jt === 4 && Ye(
      t,
      st,
      zl,
      !1
    )), Wl(t));
  }
  function Wo(t, l, e) {
    if ((gt & 6) !== 0) throw Error(s(327));
    var a = !e && (l & 127) === 0 && (l & t.expiredLanes) === 0 || ru(t, l), u = a ? $m(t, l) : of(t, l, !0), n = a;
    do {
      if (u === 0) {
        Ja && !a && Ye(t, l, 0, !1);
        break;
      } else {
        if (e = t.current.alternate, n && !wm(e)) {
          u = of(t, l, !1), n = !1;
          continue;
        }
        if (u === 2) {
          if (n = l, t.errorRecoveryDisabledLanes & n)
            var i = 0;
          else
            i = t.pendingLanes & -536870913, i = i !== 0 ? i : i & 536870912 ? 536870912 : 0;
          if (i !== 0) {
            l = i;
            t: {
              var f = t;
              u = Zu;
              var d = f.current.memoizedState.isDehydrated;
              if (d && ($a(f, i).flags |= 256), i = of(
                f,
                i,
                !1
              ), i !== 2) {
                if (af && !d) {
                  f.errorRecoveryDisabledLanes |= n, ya |= n, u = 4;
                  break t;
                }
                n = yl, yl = u, n !== null && (yl === null ? yl = n : yl.push.apply(
                  yl,
                  n
                ));
              }
              u = i;
            }
            if (n = !1, u !== 2) continue;
          }
        }
        if (u === 1) {
          $a(t, 0), Ye(t, l, 0, !0);
          break;
        }
        t: {
          switch (a = t, n = u, n) {
            case 0:
            case 1:
              throw Error(s(345));
            case 4:
              if ((l & 4194048) !== l) break;
            case 6:
              Ye(
                a,
                l,
                zl,
                !je
              );
              break t;
            case 2:
              yl = null;
              break;
            case 3:
            case 5:
              break;
            default:
              throw Error(s(329));
          }
          if ((l & 62914560) === l && (u = Pn + 300 - ll(), 10 < u)) {
            if (Ye(
              a,
              l,
              zl,
              !je
            ), on(a, 0, !0) !== 0) break t;
            ve = l, a.timeoutHandle = qd(
              Fo.bind(
                null,
                a,
                e,
                yl,
                li,
                nf,
                l,
                zl,
                ya,
                wa,
                je,
                n,
                "Throttled",
                -0,
                0
              ),
              u
            );
            break t;
          }
          Fo(
            a,
            e,
            yl,
            li,
            nf,
            l,
            zl,
            ya,
            wa,
            je,
            n,
            null,
            -0,
            0
          );
        }
      }
      break;
    } while (!0);
    Wl(t);
  }
  function Fo(t, l, e, a, u, n, i, f, d, S, z, M, _, A) {
    if (t.timeoutHandle = -1, M = l.subtreeFlags, M & 8192 || (M & 16785408) === 16785408) {
      M = {
        stylesheets: null,
        count: 0,
        imgCount: 0,
        imgBytes: 0,
        suspenseyImages: [],
        waitingForImages: !0,
        waitingForViewTransition: !1,
        unsuspend: le
      }, Zo(
        l,
        n,
        M
      );
      var Y = (n & 62914560) === n ? Pn - ll() : (n & 4194048) === n ? wo - ll() : 0;
      if (Y = Dh(
        M,
        Y
      ), Y !== null) {
        ve = n, t.cancelPendingCommit = Y(
          nd.bind(
            null,
            t,
            l,
            n,
            e,
            a,
            u,
            i,
            f,
            d,
            z,
            M,
            null,
            _,
            A
          )
        ), Ye(t, n, i, !S);
        return;
      }
    }
    nd(
      t,
      l,
      n,
      e,
      a,
      u,
      i,
      f,
      d
    );
  }
  function wm(t) {
    for (var l = t; ; ) {
      var e = l.tag;
      if ((e === 0 || e === 11 || e === 15) && l.flags & 16384 && (e = l.updateQueue, e !== null && (e = e.stores, e !== null)))
        for (var a = 0; a < e.length; a++) {
          var u = e[a], n = u.getSnapshot;
          u = u.value;
          try {
            if (!bl(n(), u)) return !1;
          } catch {
            return !1;
          }
        }
      if (e = l.child, l.subtreeFlags & 16384 && e !== null)
        e.return = l, l = e;
      else {
        if (l === t) break;
        for (; l.sibling === null; ) {
          if (l.return === null || l.return === t) return !0;
          l = l.return;
        }
        l.sibling.return = l.return, l = l.sibling;
      }
    }
    return !0;
  }
  function Ye(t, l, e, a) {
    l &= ~uf, l &= ~ya, t.suspendedLanes |= l, t.pingedLanes &= ~l, a && (t.warmLanes |= l), a = t.expirationTimes;
    for (var u = l; 0 < u; ) {
      var n = 31 - pl(u), i = 1 << n;
      a[n] = -1, u &= ~i;
    }
    e !== 0 && is(t, e, l);
  }
  function ei() {
    return (gt & 6) === 0 ? (Ku(0), !1) : !0;
  }
  function rf() {
    if (ct !== null) {
      if (St === 0)
        var t = ct.return;
      else
        t = ct, ne = ua = null, Tc(t), La = null, qu = 0, t = ct;
      for (; t !== null; )
        No(t.alternate, t), t = t.return;
      ct = null;
    }
  }
  function $a(t, l) {
    var e = t.timeoutHandle;
    e !== -1 && (t.timeoutHandle = -1, yh(e)), e = t.cancelPendingCommit, e !== null && (t.cancelPendingCommit = null, e()), ve = 0, rf(), xt = t, ct = e = ae(t.current, null), st = l, St = 0, Al = null, je = !1, Ja = ru(t, l), af = !1, wa = zl = uf = ya = Re = jt = 0, yl = Zu = null, nf = !1, (l & 8) !== 0 && (l |= l & 32);
    var a = t.entangledLanes;
    if (a !== 0)
      for (t = t.entanglements, a &= l; 0 < a; ) {
        var u = 31 - pl(a), n = 1 << u;
        l |= t[u], a &= ~n;
      }
    return he = l, An(), e;
  }
  function Io(t, l) {
    et = null, T.H = Ru, l === Ya || l === Dn ? (l = yr(), St = 3) : l === yc ? (l = yr(), St = 4) : St = l === Qc ? 8 : l !== null && typeof l == "object" && typeof l.then == "function" ? 6 : 1, Al = l, ct === null && (jt = 1, Kn(
      t,
      Ol(l, t.current)
    ));
  }
  function Po() {
    var t = _l.current;
    return t === null ? !0 : (st & 4194048) === st ? Cl === null : (st & 62914560) === st || (st & 536870912) !== 0 ? t === Cl : !1;
  }
  function td() {
    var t = T.H;
    return T.H = Ru, t === null ? Ru : t;
  }
  function ld() {
    var t = T.A;
    return T.A = Km, t;
  }
  function ai() {
    jt = 4, je || (st & 4194048) !== st && _l.current !== null || (Ja = !0), (Re & 134217727) === 0 && (ya & 134217727) === 0 || xt === null || Ye(
      xt,
      st,
      zl,
      !1
    );
  }
  function of(t, l, e) {
    var a = gt;
    gt |= 2;
    var u = td(), n = ld();
    (xt !== t || st !== l) && (li = null, $a(t, l)), l = !1;
    var i = jt;
    t: do
      try {
        if (St !== 0 && ct !== null) {
          var f = ct, d = Al;
          switch (St) {
            case 8:
              rf(), i = 6;
              break t;
            case 3:
            case 2:
            case 9:
            case 6:
              _l.current === null && (l = !0);
              var S = St;
              if (St = 0, Al = null, Wa(t, f, d, S), e && Ja) {
                i = 0;
                break t;
              }
              break;
            default:
              S = St, St = 0, Al = null, Wa(t, f, d, S);
          }
        }
        km(), i = jt;
        break;
      } catch (z) {
        Io(t, z);
      }
    while (!0);
    return l && t.shellSuspendCounter++, ne = ua = null, gt = a, T.H = u, T.A = n, ct === null && (xt = null, st = 0, An()), i;
  }
  function km() {
    for (; ct !== null; ) ed(ct);
  }
  function $m(t, l) {
    var e = gt;
    gt |= 2;
    var a = td(), u = ld();
    xt !== t || st !== l ? (li = null, ti = ll() + 500, $a(t, l)) : Ja = ru(
      t,
      l
    );
    t: do
      try {
        if (St !== 0 && ct !== null) {
          l = ct;
          var n = Al;
          l: switch (St) {
            case 1:
              St = 0, Al = null, Wa(t, l, n, 1);
              break;
            case 2:
            case 9:
              if (or(n)) {
                St = 0, Al = null, ad(l);
                break;
              }
              l = function() {
                St !== 2 && St !== 9 || xt !== t || (St = 7), Wl(t);
              }, n.then(l, l);
              break t;
            case 3:
              St = 7;
              break t;
            case 4:
              St = 5;
              break t;
            case 7:
              or(n) ? (St = 0, Al = null, ad(l)) : (St = 0, Al = null, Wa(t, l, n, 7));
              break;
            case 5:
              var i = null;
              switch (ct.tag) {
                case 26:
                  i = ct.memoizedState;
                case 5:
                case 27:
                  var f = ct;
                  if (i ? Xd(i) : f.stateNode.complete) {
                    St = 0, Al = null;
                    var d = f.sibling;
                    if (d !== null) ct = d;
                    else {
                      var S = f.return;
                      S !== null ? (ct = S, ui(S)) : ct = null;
                    }
                    break l;
                  }
              }
              St = 0, Al = null, Wa(t, l, n, 5);
              break;
            case 6:
              St = 0, Al = null, Wa(t, l, n, 6);
              break;
            case 8:
              rf(), jt = 6;
              break t;
            default:
              throw Error(s(462));
          }
        }
        Wm();
        break;
      } catch (z) {
        Io(t, z);
      }
    while (!0);
    return ne = ua = null, T.H = a, T.A = u, gt = e, ct !== null ? 0 : (xt = null, st = 0, An(), jt);
  }
  function Wm() {
    for (; ct !== null && !Jt(); )
      ed(ct);
  }
  function ed(t) {
    var l = xo(t.alternate, t, he);
    t.memoizedProps = t.pendingProps, l === null ? ui(t) : ct = l;
  }
  function ad(t) {
    var l = t, e = l.alternate;
    switch (l.tag) {
      case 15:
      case 0:
        l = So(
          e,
          l,
          l.pendingProps,
          l.type,
          void 0,
          st
        );
        break;
      case 11:
        l = So(
          e,
          l,
          l.pendingProps,
          l.type.render,
          l.ref,
          st
        );
        break;
      case 5:
        Tc(l);
      default:
        No(e, l), l = ct = tr(l, he), l = xo(e, l, he);
    }
    t.memoizedProps = t.pendingProps, l === null ? ui(t) : ct = l;
  }
  function Wa(t, l, e, a) {
    ne = ua = null, Tc(l), La = null, qu = 0;
    var u = l.return;
    try {
      if (Ym(
        t,
        u,
        l,
        e,
        st
      )) {
        jt = 1, Kn(
          t,
          Ol(e, t.current)
        ), ct = null;
        return;
      }
    } catch (n) {
      if (u !== null) throw ct = u, n;
      jt = 1, Kn(
        t,
        Ol(e, t.current)
      ), ct = null;
      return;
    }
    l.flags & 32768 ? (ot || a === 1 ? t = !0 : Ja || (st & 536870912) !== 0 ? t = !1 : (je = t = !0, (a === 2 || a === 9 || a === 3 || a === 6) && (a = _l.current, a !== null && a.tag === 13 && (a.flags |= 16384))), ud(l, t)) : ui(l);
  }
  function ui(t) {
    var l = t;
    do {
      if ((l.flags & 32768) !== 0) {
        ud(
          l,
          je
        );
        return;
      }
      t = l.return;
      var e = Qm(
        l.alternate,
        l,
        he
      );
      if (e !== null) {
        ct = e;
        return;
      }
      if (l = l.sibling, l !== null) {
        ct = l;
        return;
      }
      ct = l = t;
    } while (l !== null);
    jt === 0 && (jt = 5);
  }
  function ud(t, l) {
    do {
      var e = Xm(t.alternate, t);
      if (e !== null) {
        e.flags &= 32767, ct = e;
        return;
      }
      if (e = t.return, e !== null && (e.flags |= 32768, e.subtreeFlags = 0, e.deletions = null), !l && (t = t.sibling, t !== null)) {
        ct = t;
        return;
      }
      ct = t = e;
    } while (t !== null);
    jt = 6, ct = null;
  }
  function nd(t, l, e, a, u, n, i, f, d) {
    t.cancelPendingCommit = null;
    do
      ni();
    while (Vt !== 0);
    if ((gt & 6) !== 0) throw Error(s(327));
    if (l !== null) {
      if (l === t.current) throw Error(s(177));
      if (n = l.lanes | l.childLanes, n |= Ii, Oy(
        t,
        e,
        n,
        i,
        f,
        d
      ), t === xt && (ct = xt = null, st = 0), ka = l, Be = t, ve = e, cf = n, ff = u, ko = a, (l.subtreeFlags & 10256) !== 0 || (l.flags & 10256) !== 0 ? (t.callbackNode = null, t.callbackPriority = 0, th(cn, function() {
        return rd(), null;
      })) : (t.callbackNode = null, t.callbackPriority = 0), a = (l.flags & 13878) !== 0, (l.subtreeFlags & 13878) !== 0 || a) {
        a = T.T, T.T = null, u = H.p, H.p = 2, i = gt, gt |= 4;
        try {
          Zm(t, l, e);
        } finally {
          gt = i, H.p = u, T.T = a;
        }
      }
      Vt = 1, id(), cd(), fd();
    }
  }
  function id() {
    if (Vt === 1) {
      Vt = 0;
      var t = Be, l = ka, e = (l.flags & 13878) !== 0;
      if ((l.subtreeFlags & 13878) !== 0 || e) {
        e = T.T, T.T = null;
        var a = H.p;
        H.p = 2;
        var u = gt;
        gt |= 4;
        try {
          Go(l, t);
          var n = Af, i = Ks(t.containerInfo), f = n.focusedElem, d = n.selectionRange;
          if (i !== f && f && f.ownerDocument && Vs(
            f.ownerDocument.documentElement,
            f
          )) {
            if (d !== null && wi(f)) {
              var S = d.start, z = d.end;
              if (z === void 0 && (z = S), "selectionStart" in f)
                f.selectionStart = S, f.selectionEnd = Math.min(
                  z,
                  f.value.length
                );
              else {
                var M = f.ownerDocument || document, _ = M && M.defaultView || window;
                if (_.getSelection) {
                  var A = _.getSelection(), Y = f.textContent.length, k = Math.min(d.start, Y), zt = d.end === void 0 ? k : Math.min(d.end, Y);
                  !A.extend && k > zt && (i = zt, zt = k, k = i);
                  var h = Zs(
                    f,
                    k
                  ), y = Zs(
                    f,
                    zt
                  );
                  if (h && y && (A.rangeCount !== 1 || A.anchorNode !== h.node || A.anchorOffset !== h.offset || A.focusNode !== y.node || A.focusOffset !== y.offset)) {
                    var b = M.createRange();
                    b.setStart(h.node, h.offset), A.removeAllRanges(), k > zt ? (A.addRange(b), A.extend(y.node, y.offset)) : (b.setEnd(y.node, y.offset), A.addRange(b));
                  }
                }
              }
            }
            for (M = [], A = f; A = A.parentNode; )
              A.nodeType === 1 && M.push({
                element: A,
                left: A.scrollLeft,
                top: A.scrollTop
              });
            for (typeof f.focus == "function" && f.focus(), f = 0; f < M.length; f++) {
              var q = M[f];
              q.element.scrollLeft = q.left, q.element.scrollTop = q.top;
            }
          }
          gi = !!Ef, Af = Ef = null;
        } finally {
          gt = u, H.p = a, T.T = e;
        }
      }
      t.current = l, Vt = 2;
    }
  }
  function cd() {
    if (Vt === 2) {
      Vt = 0;
      var t = Be, l = ka, e = (l.flags & 8772) !== 0;
      if ((l.subtreeFlags & 8772) !== 0 || e) {
        e = T.T, T.T = null;
        var a = H.p;
        H.p = 2;
        var u = gt;
        gt |= 4;
        try {
          Ro(t, l.alternate, l);
        } finally {
          gt = u, H.p = a, T.T = e;
        }
      }
      Vt = 3;
    }
  }
  function fd() {
    if (Vt === 4 || Vt === 3) {
      Vt = 0, tl();
      var t = Be, l = ka, e = ve, a = ko;
      (l.subtreeFlags & 10256) !== 0 || (l.flags & 10256) !== 0 ? Vt = 5 : (Vt = 0, ka = Be = null, sd(t, t.pendingLanes));
      var u = t.pendingLanes;
      if (u === 0 && (He = null), Ni(e), l = l.stateNode, gl && typeof gl.onCommitFiberRoot == "function")
        try {
          gl.onCommitFiberRoot(
            su,
            l,
            void 0,
            (l.current.flags & 128) === 128
          );
        } catch {
        }
      if (a !== null) {
        l = T.T, u = H.p, H.p = 2, T.T = null;
        try {
          for (var n = t.onRecoverableError, i = 0; i < a.length; i++) {
            var f = a[i];
            n(f.value, {
              componentStack: f.stack
            });
          }
        } finally {
          T.T = l, H.p = u;
        }
      }
      (ve & 3) !== 0 && ni(), Wl(t), u = t.pendingLanes, (e & 261930) !== 0 && (u & 42) !== 0 ? t === sf ? Vu++ : (Vu = 0, sf = t) : Vu = 0, Ku(0);
    }
  }
  function sd(t, l) {
    (t.pooledCacheLanes &= l) === 0 && (l = t.pooledCache, l != null && (t.pooledCache = null, Tu(l)));
  }
  function ni() {
    return id(), cd(), fd(), rd();
  }
  function rd() {
    if (Vt !== 5) return !1;
    var t = Be, l = cf;
    cf = 0;
    var e = Ni(ve), a = T.T, u = H.p;
    try {
      H.p = 32 > e ? 32 : e, T.T = null, e = ff, ff = null;
      var n = Be, i = ve;
      if (Vt = 0, ka = Be = null, ve = 0, (gt & 6) !== 0) throw Error(s(331));
      var f = gt;
      if (gt |= 4, Ko(n.current), Xo(
        n,
        n.current,
        i,
        e
      ), gt = f, Ku(0, !1), gl && typeof gl.onPostCommitFiberRoot == "function")
        try {
          gl.onPostCommitFiberRoot(su, n);
        } catch {
        }
      return !0;
    } finally {
      H.p = u, T.T = a, sd(t, l);
    }
  }
  function od(t, l, e) {
    l = Ol(e, l), l = Gc(t.stateNode, l, 2), t = Me(t, l, 2), t !== null && (ou(t, 2), Wl(t));
  }
  function _t(t, l, e) {
    if (t.tag === 3)
      od(t, t, e);
    else
      for (; l !== null; ) {
        if (l.tag === 3) {
          od(
            l,
            t,
            e
          );
          break;
        } else if (l.tag === 1) {
          var a = l.stateNode;
          if (typeof l.type.getDerivedStateFromError == "function" || typeof a.componentDidCatch == "function" && (He === null || !He.has(a))) {
            t = Ol(e, t), e = oo(2), a = Me(l, e, 2), a !== null && (yo(
              e,
              a,
              l,
              t
            ), ou(a, 2), Wl(a));
            break;
          }
        }
        l = l.return;
      }
  }
  function df(t, l, e) {
    var a = t.pingCache;
    if (a === null) {
      a = t.pingCache = new Jm();
      var u = /* @__PURE__ */ new Set();
      a.set(l, u);
    } else
      u = a.get(l), u === void 0 && (u = /* @__PURE__ */ new Set(), a.set(l, u));
    u.has(e) || (af = !0, u.add(e), t = Fm.bind(null, t, l, e), l.then(t, t));
  }
  function Fm(t, l, e) {
    var a = t.pingCache;
    a !== null && a.delete(l), t.pingedLanes |= t.suspendedLanes & e, t.warmLanes &= ~e, xt === t && (st & e) === e && (jt === 4 || jt === 3 && (st & 62914560) === st && 300 > ll() - Pn ? (gt & 2) === 0 && $a(t, 0) : uf |= e, wa === st && (wa = 0)), Wl(t);
  }
  function dd(t, l) {
    l === 0 && (l = ns()), t = la(t, l), t !== null && (ou(t, l), Wl(t));
  }
  function Im(t) {
    var l = t.memoizedState, e = 0;
    l !== null && (e = l.retryLane), dd(t, e);
  }
  function Pm(t, l) {
    var e = 0;
    switch (t.tag) {
      case 31:
      case 13:
        var a = t.stateNode, u = t.memoizedState;
        u !== null && (e = u.retryLane);
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
    a !== null && a.delete(l), dd(t, e);
  }
  function th(t, l) {
    return vt(t, l);
  }
  var ii = null, Fa = null, yf = !1, ci = !1, mf = !1, Le = 0;
  function Wl(t) {
    t !== Fa && t.next === null && (Fa === null ? ii = Fa = t : Fa = Fa.next = t), ci = !0, yf || (yf = !0, eh());
  }
  function Ku(t, l) {
    if (!mf && ci) {
      mf = !0;
      do
        for (var e = !1, a = ii; a !== null; ) {
          if (t !== 0) {
            var u = a.pendingLanes;
            if (u === 0) var n = 0;
            else {
              var i = a.suspendedLanes, f = a.pingedLanes;
              n = (1 << 31 - pl(42 | t) + 1) - 1, n &= u & ~(i & ~f), n = n & 201326741 ? n & 201326741 | 1 : n ? n | 2 : 0;
            }
            n !== 0 && (e = !0, vd(a, n));
          } else
            n = st, n = on(
              a,
              a === xt ? n : 0,
              a.cancelPendingCommit !== null || a.timeoutHandle !== -1
            ), (n & 3) === 0 || ru(a, n) || (e = !0, vd(a, n));
          a = a.next;
        }
      while (e);
      mf = !1;
    }
  }
  function lh() {
    yd();
  }
  function yd() {
    ci = yf = !1;
    var t = 0;
    Le !== 0 && dh() && (t = Le);
    for (var l = ll(), e = null, a = ii; a !== null; ) {
      var u = a.next, n = md(a, l);
      n === 0 ? (a.next = null, e === null ? ii = u : e.next = u, u === null && (Fa = e)) : (e = a, (t !== 0 || (n & 3) !== 0) && (ci = !0)), a = u;
    }
    Vt !== 0 && Vt !== 5 || Ku(t), Le !== 0 && (Le = 0);
  }
  function md(t, l) {
    for (var e = t.suspendedLanes, a = t.pingedLanes, u = t.expirationTimes, n = t.pendingLanes & -62914561; 0 < n; ) {
      var i = 31 - pl(n), f = 1 << i, d = u[i];
      d === -1 ? ((f & e) === 0 || (f & a) !== 0) && (u[i] = Ny(f, l)) : d <= l && (t.expiredLanes |= f), n &= ~f;
    }
    if (l = xt, e = st, e = on(
      t,
      t === l ? e : 0,
      t.cancelPendingCommit !== null || t.timeoutHandle !== -1
    ), a = t.callbackNode, e === 0 || t === l && (St === 2 || St === 9) || t.cancelPendingCommit !== null)
      return a !== null && a !== null && Ut(a), t.callbackNode = null, t.callbackPriority = 0;
    if ((e & 3) === 0 || ru(t, e)) {
      if (l = e & -e, l === t.callbackPriority) return l;
      switch (a !== null && Ut(a), Ni(e)) {
        case 2:
        case 8:
          e = as;
          break;
        case 32:
          e = cn;
          break;
        case 268435456:
          e = us;
          break;
        default:
          e = cn;
      }
      return a = hd.bind(null, t), e = vt(e, a), t.callbackPriority = l, t.callbackNode = e, l;
    }
    return a !== null && a !== null && Ut(a), t.callbackPriority = 2, t.callbackNode = null, 2;
  }
  function hd(t, l) {
    if (Vt !== 0 && Vt !== 5)
      return t.callbackNode = null, t.callbackPriority = 0, null;
    var e = t.callbackNode;
    if (ni() && t.callbackNode !== e)
      return null;
    var a = st;
    return a = on(
      t,
      t === xt ? a : 0,
      t.cancelPendingCommit !== null || t.timeoutHandle !== -1
    ), a === 0 ? null : (Wo(t, a, l), md(t, ll()), t.callbackNode != null && t.callbackNode === e ? hd.bind(null, t) : null);
  }
  function vd(t, l) {
    if (ni()) return null;
    Wo(t, l, !0);
  }
  function eh() {
    mh(function() {
      (gt & 6) !== 0 ? vt(
        es,
        lh
      ) : yd();
    });
  }
  function hf() {
    if (Le === 0) {
      var t = Ha;
      t === 0 && (t = fn, fn <<= 1, (fn & 261888) === 0 && (fn = 256)), Le = t;
    }
    return Le;
  }
  function gd(t) {
    return t == null || typeof t == "symbol" || typeof t == "boolean" ? null : typeof t == "function" ? t : hn("" + t);
  }
  function pd(t, l) {
    var e = l.ownerDocument.createElement("input");
    return e.name = l.name, e.value = l.value, t.id && e.setAttribute("form", t.id), l.parentNode.insertBefore(e, l), t = new FormData(t), e.parentNode.removeChild(e), t;
  }
  function ah(t, l, e, a, u) {
    if (l === "submit" && e && e.stateNode === u) {
      var n = gd(
        (u[fl] || null).action
      ), i = a.submitter;
      i && (l = (l = i[fl] || null) ? gd(l.formAction) : i.getAttribute("formAction"), l !== null && (n = l, i = null));
      var f = new bn(
        "action",
        "action",
        null,
        a,
        u
      );
      t.push({
        event: f,
        listeners: [
          {
            instance: null,
            listener: function() {
              if (a.defaultPrevented) {
                if (Le !== 0) {
                  var d = i ? pd(u, i) : new FormData(u);
                  jc(
                    e,
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
                typeof n == "function" && (f.preventDefault(), d = i ? pd(u, i) : new FormData(u), jc(
                  e,
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
  for (var vf = 0; vf < Fi.length; vf++) {
    var gf = Fi[vf], uh = gf.toLowerCase(), nh = gf[0].toUpperCase() + gf.slice(1);
    Gl(
      uh,
      "on" + nh
    );
  }
  Gl(ks, "onAnimationEnd"), Gl($s, "onAnimationIteration"), Gl(Ws, "onAnimationStart"), Gl("dblclick", "onDoubleClick"), Gl("focusin", "onFocus"), Gl("focusout", "onBlur"), Gl(_m, "onTransitionRun"), Gl(Em, "onTransitionStart"), Gl(Am, "onTransitionCancel"), Gl(Fs, "onTransitionEnd"), Ea("onMouseEnter", ["mouseout", "mouseover"]), Ea("onMouseLeave", ["mouseout", "mouseover"]), Ea("onPointerEnter", ["pointerout", "pointerover"]), Ea("onPointerLeave", ["pointerout", "pointerover"]), Fe(
    "onChange",
    "change click focusin focusout input keydown keyup selectionchange".split(" ")
  ), Fe(
    "onSelect",
    "focusout contextmenu dragend focusin keydown keyup mousedown mouseup selectionchange".split(
      " "
    )
  ), Fe("onBeforeInput", [
    "compositionend",
    "keypress",
    "textInput",
    "paste"
  ]), Fe(
    "onCompositionEnd",
    "compositionend focusout keydown keypress keyup mousedown".split(" ")
  ), Fe(
    "onCompositionStart",
    "compositionstart focusout keydown keypress keyup mousedown".split(" ")
  ), Fe(
    "onCompositionUpdate",
    "compositionupdate focusout keydown keypress keyup mousedown".split(" ")
  );
  var Ju = "abort canplay canplaythrough durationchange emptied encrypted ended error loadeddata loadedmetadata loadstart pause play playing progress ratechange resize seeked seeking stalled suspend timeupdate volumechange waiting".split(
    " "
  ), ih = new Set(
    "beforetoggle cancel close invalid load scroll scrollend toggle".split(" ").concat(Ju)
  );
  function bd(t, l) {
    l = (l & 4) !== 0;
    for (var e = 0; e < t.length; e++) {
      var a = t[e], u = a.event;
      a = a.listeners;
      t: {
        var n = void 0;
        if (l)
          for (var i = a.length - 1; 0 <= i; i--) {
            var f = a[i], d = f.instance, S = f.currentTarget;
            if (f = f.listener, d !== n && u.isPropagationStopped())
              break t;
            n = f, u.currentTarget = S;
            try {
              n(u);
            } catch (z) {
              En(z);
            }
            u.currentTarget = null, n = d;
          }
        else
          for (i = 0; i < a.length; i++) {
            if (f = a[i], d = f.instance, S = f.currentTarget, f = f.listener, d !== n && u.isPropagationStopped())
              break t;
            n = f, u.currentTarget = S;
            try {
              n(u);
            } catch (z) {
              En(z);
            }
            u.currentTarget = null, n = d;
          }
      }
    }
  }
  function ft(t, l) {
    var e = l[Oi];
    e === void 0 && (e = l[Oi] = /* @__PURE__ */ new Set());
    var a = t + "__bubble";
    e.has(a) || (Sd(l, t, 2, !1), e.add(a));
  }
  function pf(t, l, e) {
    var a = 0;
    l && (a |= 4), Sd(
      e,
      t,
      a,
      l
    );
  }
  var fi = "_reactListening" + Math.random().toString(36).slice(2);
  function bf(t) {
    if (!t[fi]) {
      t[fi] = !0, ds.forEach(function(e) {
        e !== "selectionchange" && (ih.has(e) || pf(e, !1, t), pf(e, !0, t));
      });
      var l = t.nodeType === 9 ? t : t.ownerDocument;
      l === null || l[fi] || (l[fi] = !0, pf("selectionchange", !1, l));
    }
  }
  function Sd(t, l, e, a) {
    switch ($d(l)) {
      case 2:
        var u = jh;
        break;
      case 8:
        u = Rh;
        break;
      default:
        u = jf;
    }
    e = u.bind(
      null,
      l,
      e,
      t
    ), u = void 0, !Yi || l !== "touchstart" && l !== "touchmove" && l !== "wheel" || (u = !0), a ? u !== void 0 ? t.addEventListener(l, e, {
      capture: !0,
      passive: u
    }) : t.addEventListener(l, e, !0) : u !== void 0 ? t.addEventListener(l, e, {
      passive: u
    }) : t.addEventListener(l, e, !1);
  }
  function Sf(t, l, e, a, u) {
    var n = a;
    if ((l & 1) === 0 && (l & 2) === 0 && a !== null)
      t: for (; ; ) {
        if (a === null) return;
        var i = a.tag;
        if (i === 3 || i === 4) {
          var f = a.stateNode.containerInfo;
          if (f === u) break;
          if (i === 4)
            for (i = a.return; i !== null; ) {
              var d = i.tag;
              if ((d === 3 || d === 4) && i.stateNode.containerInfo === u)
                return;
              i = i.return;
            }
          for (; f !== null; ) {
            if (i = ba(f), i === null) return;
            if (d = i.tag, d === 5 || d === 6 || d === 26 || d === 27) {
              a = n = i;
              continue t;
            }
            f = f.parentNode;
          }
        }
        a = a.return;
      }
    zs(function() {
      var S = n, z = Hi(e), M = [];
      t: {
        var _ = Is.get(t);
        if (_ !== void 0) {
          var A = bn, Y = t;
          switch (t) {
            case "keypress":
              if (gn(e) === 0) break t;
            case "keydown":
            case "keyup":
              A = Py;
              break;
            case "focusin":
              Y = "focus", A = Xi;
              break;
            case "focusout":
              Y = "blur", A = Xi;
              break;
            case "beforeblur":
            case "afterblur":
              A = Xi;
              break;
            case "click":
              if (e.button === 2) break t;
            case "auxclick":
            case "dblclick":
            case "mousedown":
            case "mousemove":
            case "mouseup":
            case "mouseout":
            case "mouseover":
            case "contextmenu":
              A = qs;
              break;
            case "drag":
            case "dragend":
            case "dragenter":
            case "dragexit":
            case "dragleave":
            case "dragover":
            case "dragstart":
            case "drop":
              A = Qy;
              break;
            case "touchcancel":
            case "touchend":
            case "touchmove":
            case "touchstart":
              A = em;
              break;
            case ks:
            case $s:
            case Ws:
              A = Vy;
              break;
            case Fs:
              A = um;
              break;
            case "scroll":
            case "scrollend":
              A = Ly;
              break;
            case "wheel":
              A = im;
              break;
            case "copy":
            case "cut":
            case "paste":
              A = Jy;
              break;
            case "gotpointercapture":
            case "lostpointercapture":
            case "pointercancel":
            case "pointerdown":
            case "pointermove":
            case "pointerout":
            case "pointerover":
            case "pointerup":
              A = Os;
              break;
            case "toggle":
            case "beforetoggle":
              A = fm;
          }
          var k = (l & 4) !== 0, zt = !k && (t === "scroll" || t === "scrollend"), h = k ? _ !== null ? _ + "Capture" : null : _;
          k = [];
          for (var y = S, b; y !== null; ) {
            var q = y;
            if (b = q.stateNode, q = q.tag, q !== 5 && q !== 26 && q !== 27 || b === null || h === null || (q = mu(y, h), q != null && k.push(
              wu(y, q, b)
            )), zt) break;
            y = y.return;
          }
          0 < k.length && (_ = new A(
            _,
            Y,
            null,
            e,
            z
          ), M.push({ event: _, listeners: k }));
        }
      }
      if ((l & 7) === 0) {
        t: {
          if (_ = t === "mouseover" || t === "pointerover", A = t === "mouseout" || t === "pointerout", _ && e !== Ri && (Y = e.relatedTarget || e.fromElement) && (ba(Y) || Y[pa]))
            break t;
          if ((A || _) && (_ = z.window === z ? z : (_ = z.ownerDocument) ? _.defaultView || _.parentWindow : window, A ? (Y = e.relatedTarget || e.toElement, A = S, Y = Y ? ba(Y) : null, Y !== null && (zt = v(Y), k = Y.tag, Y !== zt || k !== 5 && k !== 27 && k !== 6) && (Y = null)) : (A = null, Y = S), A !== Y)) {
            if (k = qs, q = "onMouseLeave", h = "onMouseEnter", y = "mouse", (t === "pointerout" || t === "pointerover") && (k = Os, q = "onPointerLeave", h = "onPointerEnter", y = "pointer"), zt = A == null ? _ : yu(A), b = Y == null ? _ : yu(Y), _ = new k(
              q,
              y + "leave",
              A,
              e,
              z
            ), _.target = zt, _.relatedTarget = b, q = null, ba(z) === S && (k = new k(
              h,
              y + "enter",
              Y,
              e,
              z
            ), k.target = b, k.relatedTarget = zt, q = k), zt = q, A && Y)
              l: {
                for (k = ch, h = A, y = Y, b = 0, q = h; q; q = k(q))
                  b++;
                q = 0;
                for (var K = y; K; K = k(K))
                  q++;
                for (; 0 < b - q; )
                  h = k(h), b--;
                for (; 0 < q - b; )
                  y = k(y), q--;
                for (; b--; ) {
                  if (h === y || y !== null && h === y.alternate) {
                    k = h;
                    break l;
                  }
                  h = k(h), y = k(y);
                }
                k = null;
              }
            else k = null;
            A !== null && _d(
              M,
              _,
              A,
              k,
              !1
            ), Y !== null && zt !== null && _d(
              M,
              zt,
              Y,
              k,
              !0
            );
          }
        }
        t: {
          if (_ = S ? yu(S) : window, A = _.nodeName && _.nodeName.toLowerCase(), A === "select" || A === "input" && _.type === "file")
            var mt = Bs;
          else if (Rs(_))
            if (Ys)
              mt = pm;
            else {
              mt = vm;
              var G = hm;
            }
          else
            A = _.nodeName, !A || A.toLowerCase() !== "input" || _.type !== "checkbox" && _.type !== "radio" ? S && ji(S.elementType) && (mt = Bs) : mt = gm;
          if (mt && (mt = mt(t, S))) {
            Hs(
              M,
              mt,
              e,
              z
            );
            break t;
          }
          G && G(t, _, S), t === "focusout" && S && _.type === "number" && S.memoizedProps.value != null && Ci(_, "number", _.value);
        }
        switch (G = S ? yu(S) : window, t) {
          case "focusin":
            (Rs(G) || G.contentEditable === "true") && (Na = G, ki = S, Eu = null);
            break;
          case "focusout":
            Eu = ki = Na = null;
            break;
          case "mousedown":
            $i = !0;
            break;
          case "contextmenu":
          case "mouseup":
          case "dragend":
            $i = !1, Js(M, e, z);
            break;
          case "selectionchange":
            if (Sm) break;
          case "keydown":
          case "keyup":
            Js(M, e, z);
        }
        var at;
        if (Vi)
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
          qa ? Cs(t, e) && (rt = "onCompositionEnd") : t === "keydown" && e.keyCode === 229 && (rt = "onCompositionStart");
        rt && (Ms && e.locale !== "ko" && (qa || rt !== "onCompositionStart" ? rt === "onCompositionEnd" && qa && (at = Ts()) : (Ae = z, Li = "value" in Ae ? Ae.value : Ae.textContent, qa = !0)), G = si(S, rt), 0 < G.length && (rt = new Ns(
          rt,
          t,
          null,
          e,
          z
        ), M.push({ event: rt, listeners: G }), at ? rt.data = at : (at = js(e), at !== null && (rt.data = at)))), (at = rm ? om(t, e) : dm(t, e)) && (rt = si(S, "onBeforeInput"), 0 < rt.length && (G = new Ns(
          "onBeforeInput",
          "beforeinput",
          null,
          e,
          z
        ), M.push({
          event: G,
          listeners: rt
        }), G.data = at)), ah(
          M,
          t,
          S,
          e,
          z
        );
      }
      bd(M, l);
    });
  }
  function wu(t, l, e) {
    return {
      instance: t,
      listener: l,
      currentTarget: e
    };
  }
  function si(t, l) {
    for (var e = l + "Capture", a = []; t !== null; ) {
      var u = t, n = u.stateNode;
      if (u = u.tag, u !== 5 && u !== 26 && u !== 27 || n === null || (u = mu(t, e), u != null && a.unshift(
        wu(t, u, n)
      ), u = mu(t, l), u != null && a.push(
        wu(t, u, n)
      )), t.tag === 3) return a;
      t = t.return;
    }
    return [];
  }
  function ch(t) {
    if (t === null) return null;
    do
      t = t.return;
    while (t && t.tag !== 5 && t.tag !== 27);
    return t || null;
  }
  function _d(t, l, e, a, u) {
    for (var n = l._reactName, i = []; e !== null && e !== a; ) {
      var f = e, d = f.alternate, S = f.stateNode;
      if (f = f.tag, d !== null && d === a) break;
      f !== 5 && f !== 26 && f !== 27 || S === null || (d = S, u ? (S = mu(e, n), S != null && i.unshift(
        wu(e, S, d)
      )) : u || (S = mu(e, n), S != null && i.push(
        wu(e, S, d)
      ))), e = e.return;
    }
    i.length !== 0 && t.push({ event: l, listeners: i });
  }
  var fh = /\r\n?/g, sh = /\u0000|\uFFFD/g;
  function Ed(t) {
    return (typeof t == "string" ? t : "" + t).replace(fh, `
`).replace(sh, "");
  }
  function Ad(t, l) {
    return l = Ed(l), Ed(t) === l;
  }
  function At(t, l, e, a, u, n) {
    switch (e) {
      case "children":
        typeof a == "string" ? l === "body" || l === "textarea" && a === "" || za(t, a) : (typeof a == "number" || typeof a == "bigint") && l !== "body" && za(t, "" + a);
        break;
      case "className":
        yn(t, "class", a);
        break;
      case "tabIndex":
        yn(t, "tabindex", a);
        break;
      case "dir":
      case "role":
      case "viewBox":
      case "width":
      case "height":
        yn(t, e, a);
        break;
      case "style":
        Es(t, a, n);
        break;
      case "data":
        if (l !== "object") {
          yn(t, "data", a);
          break;
        }
      case "src":
      case "href":
        if (a === "" && (l !== "a" || e !== "href")) {
          t.removeAttribute(e);
          break;
        }
        if (a == null || typeof a == "function" || typeof a == "symbol" || typeof a == "boolean") {
          t.removeAttribute(e);
          break;
        }
        a = hn("" + a), t.setAttribute(e, a);
        break;
      case "action":
      case "formAction":
        if (typeof a == "function") {
          t.setAttribute(
            e,
            "javascript:throw new Error('A React form was unexpectedly submitted. If you called form.submit() manually, consider using form.requestSubmit() instead. If you\\'re trying to use event.stopPropagation() in a submit event handler, consider also calling event.preventDefault().')"
          );
          break;
        } else
          typeof n == "function" && (e === "formAction" ? (l !== "input" && At(t, l, "name", u.name, u, null), At(
            t,
            l,
            "formEncType",
            u.formEncType,
            u,
            null
          ), At(
            t,
            l,
            "formMethod",
            u.formMethod,
            u,
            null
          ), At(
            t,
            l,
            "formTarget",
            u.formTarget,
            u,
            null
          )) : (At(t, l, "encType", u.encType, u, null), At(t, l, "method", u.method, u, null), At(t, l, "target", u.target, u, null)));
        if (a == null || typeof a == "symbol" || typeof a == "boolean") {
          t.removeAttribute(e);
          break;
        }
        a = hn("" + a), t.setAttribute(e, a);
        break;
      case "onClick":
        a != null && (t.onclick = le);
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
          if (e = a.__html, e != null) {
            if (u.children != null) throw Error(s(60));
            t.innerHTML = e;
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
        e = hn("" + a), t.setAttributeNS(
          "http://www.w3.org/1999/xlink",
          "xlink:href",
          e
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
        a != null && typeof a != "function" && typeof a != "symbol" ? t.setAttribute(e, "" + a) : t.removeAttribute(e);
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
        a && typeof a != "function" && typeof a != "symbol" ? t.setAttribute(e, "") : t.removeAttribute(e);
        break;
      case "capture":
      case "download":
        a === !0 ? t.setAttribute(e, "") : a !== !1 && a != null && typeof a != "function" && typeof a != "symbol" ? t.setAttribute(e, a) : t.removeAttribute(e);
        break;
      case "cols":
      case "rows":
      case "size":
      case "span":
        a != null && typeof a != "function" && typeof a != "symbol" && !isNaN(a) && 1 <= a ? t.setAttribute(e, a) : t.removeAttribute(e);
        break;
      case "rowSpan":
      case "start":
        a == null || typeof a == "function" || typeof a == "symbol" || isNaN(a) ? t.removeAttribute(e) : t.setAttribute(e, a);
        break;
      case "popover":
        ft("beforetoggle", t), ft("toggle", t), dn(t, "popover", a);
        break;
      case "xlinkActuate":
        te(
          t,
          "http://www.w3.org/1999/xlink",
          "xlink:actuate",
          a
        );
        break;
      case "xlinkArcrole":
        te(
          t,
          "http://www.w3.org/1999/xlink",
          "xlink:arcrole",
          a
        );
        break;
      case "xlinkRole":
        te(
          t,
          "http://www.w3.org/1999/xlink",
          "xlink:role",
          a
        );
        break;
      case "xlinkShow":
        te(
          t,
          "http://www.w3.org/1999/xlink",
          "xlink:show",
          a
        );
        break;
      case "xlinkTitle":
        te(
          t,
          "http://www.w3.org/1999/xlink",
          "xlink:title",
          a
        );
        break;
      case "xlinkType":
        te(
          t,
          "http://www.w3.org/1999/xlink",
          "xlink:type",
          a
        );
        break;
      case "xmlBase":
        te(
          t,
          "http://www.w3.org/XML/1998/namespace",
          "xml:base",
          a
        );
        break;
      case "xmlLang":
        te(
          t,
          "http://www.w3.org/XML/1998/namespace",
          "xml:lang",
          a
        );
        break;
      case "xmlSpace":
        te(
          t,
          "http://www.w3.org/XML/1998/namespace",
          "xml:space",
          a
        );
        break;
      case "is":
        dn(t, "is", a);
        break;
      case "innerText":
      case "textContent":
        break;
      default:
        (!(2 < e.length) || e[0] !== "o" && e[0] !== "O" || e[1] !== "n" && e[1] !== "N") && (e = By.get(e) || e, dn(t, e, a));
    }
  }
  function _f(t, l, e, a, u, n) {
    switch (e) {
      case "style":
        Es(t, a, n);
        break;
      case "dangerouslySetInnerHTML":
        if (a != null) {
          if (typeof a != "object" || !("__html" in a))
            throw Error(s(61));
          if (e = a.__html, e != null) {
            if (u.children != null) throw Error(s(60));
            t.innerHTML = e;
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
        a != null && (t.onclick = le);
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
        if (!ys.hasOwnProperty(e))
          t: {
            if (e[0] === "o" && e[1] === "n" && (u = e.endsWith("Capture"), l = e.slice(2, u ? e.length - 7 : void 0), n = t[fl] || null, n = n != null ? n[e] : null, typeof n == "function" && t.removeEventListener(l, n, u), typeof a == "function")) {
              typeof n != "function" && n !== null && (e in t ? t[e] = null : t.hasAttribute(e) && t.removeAttribute(e)), t.addEventListener(l, a, u);
              break t;
            }
            e in t ? t[e] = a : a === !0 ? t.setAttribute(e, "") : dn(t, e, a);
          }
    }
  }
  function Pt(t, l, e) {
    switch (l) {
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
        for (n in e)
          if (e.hasOwnProperty(n)) {
            var i = e[n];
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
                  throw Error(s(137, l));
                default:
                  At(t, l, n, i, e, null);
              }
          }
        u && At(t, l, "srcSet", e.srcSet, e, null), a && At(t, l, "src", e.src, e, null);
        return;
      case "input":
        ft("invalid", t);
        var f = n = i = u = null, d = null, S = null;
        for (a in e)
          if (e.hasOwnProperty(a)) {
            var z = e[a];
            if (z != null)
              switch (a) {
                case "name":
                  u = z;
                  break;
                case "type":
                  i = z;
                  break;
                case "checked":
                  d = z;
                  break;
                case "defaultChecked":
                  S = z;
                  break;
                case "value":
                  n = z;
                  break;
                case "defaultValue":
                  f = z;
                  break;
                case "children":
                case "dangerouslySetInnerHTML":
                  if (z != null)
                    throw Error(s(137, l));
                  break;
                default:
                  At(t, l, a, z, e, null);
              }
          }
        ps(
          t,
          n,
          f,
          d,
          S,
          i,
          u,
          !1
        );
        return;
      case "select":
        ft("invalid", t), a = i = n = null;
        for (u in e)
          if (e.hasOwnProperty(u) && (f = e[u], f != null))
            switch (u) {
              case "value":
                n = f;
                break;
              case "defaultValue":
                i = f;
                break;
              case "multiple":
                a = f;
              default:
                At(t, l, u, f, e, null);
            }
        l = n, e = i, t.multiple = !!a, l != null ? Aa(t, !!a, l, !1) : e != null && Aa(t, !!a, e, !0);
        return;
      case "textarea":
        ft("invalid", t), n = u = a = null;
        for (i in e)
          if (e.hasOwnProperty(i) && (f = e[i], f != null))
            switch (i) {
              case "value":
                a = f;
                break;
              case "defaultValue":
                u = f;
                break;
              case "children":
                n = f;
                break;
              case "dangerouslySetInnerHTML":
                if (f != null) throw Error(s(91));
                break;
              default:
                At(t, l, i, f, e, null);
            }
        Ss(t, a, u, n);
        return;
      case "option":
        for (d in e)
          if (e.hasOwnProperty(d) && (a = e[d], a != null))
            switch (d) {
              case "selected":
                t.selected = a && typeof a != "function" && typeof a != "symbol";
                break;
              default:
                At(t, l, d, a, e, null);
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
        for (a = 0; a < Ju.length; a++)
          ft(Ju[a], t);
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
        for (S in e)
          if (e.hasOwnProperty(S) && (a = e[S], a != null))
            switch (S) {
              case "children":
              case "dangerouslySetInnerHTML":
                throw Error(s(137, l));
              default:
                At(t, l, S, a, e, null);
            }
        return;
      default:
        if (ji(l)) {
          for (z in e)
            e.hasOwnProperty(z) && (a = e[z], a !== void 0 && _f(
              t,
              l,
              z,
              a,
              e,
              void 0
            ));
          return;
        }
    }
    for (f in e)
      e.hasOwnProperty(f) && (a = e[f], a != null && At(t, l, f, a, e, null));
  }
  function rh(t, l, e, a) {
    switch (l) {
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
        var u = null, n = null, i = null, f = null, d = null, S = null, z = null;
        for (A in e) {
          var M = e[A];
          if (e.hasOwnProperty(A) && M != null)
            switch (A) {
              case "checked":
                break;
              case "value":
                break;
              case "defaultValue":
                d = M;
              default:
                a.hasOwnProperty(A) || At(t, l, A, null, a, M);
            }
        }
        for (var _ in a) {
          var A = a[_];
          if (M = e[_], a.hasOwnProperty(_) && (A != null || M != null))
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
                z = A;
                break;
              case "value":
                i = A;
                break;
              case "defaultValue":
                f = A;
                break;
              case "children":
              case "dangerouslySetInnerHTML":
                if (A != null)
                  throw Error(s(137, l));
                break;
              default:
                A !== M && At(
                  t,
                  l,
                  _,
                  A,
                  a,
                  M
                );
            }
        }
        Ui(
          t,
          i,
          f,
          d,
          S,
          z,
          n,
          u
        );
        return;
      case "select":
        A = i = f = _ = null;
        for (n in e)
          if (d = e[n], e.hasOwnProperty(n) && d != null)
            switch (n) {
              case "value":
                break;
              case "multiple":
                A = d;
              default:
                a.hasOwnProperty(n) || At(
                  t,
                  l,
                  n,
                  null,
                  a,
                  d
                );
            }
        for (u in a)
          if (n = a[u], d = e[u], a.hasOwnProperty(u) && (n != null || d != null))
            switch (u) {
              case "value":
                _ = n;
                break;
              case "defaultValue":
                f = n;
                break;
              case "multiple":
                i = n;
              default:
                n !== d && At(
                  t,
                  l,
                  u,
                  n,
                  a,
                  d
                );
            }
        l = f, e = i, a = A, _ != null ? Aa(t, !!e, _, !1) : !!a != !!e && (l != null ? Aa(t, !!e, l, !0) : Aa(t, !!e, e ? [] : "", !1));
        return;
      case "textarea":
        A = _ = null;
        for (f in e)
          if (u = e[f], e.hasOwnProperty(f) && u != null && !a.hasOwnProperty(f))
            switch (f) {
              case "value":
                break;
              case "children":
                break;
              default:
                At(t, l, f, null, a, u);
            }
        for (i in a)
          if (u = a[i], n = e[i], a.hasOwnProperty(i) && (u != null || n != null))
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
                u !== n && At(t, l, i, u, a, n);
            }
        bs(t, _, A);
        return;
      case "option":
        for (var Y in e)
          if (_ = e[Y], e.hasOwnProperty(Y) && _ != null && !a.hasOwnProperty(Y))
            switch (Y) {
              case "selected":
                t.selected = !1;
                break;
              default:
                At(
                  t,
                  l,
                  Y,
                  null,
                  a,
                  _
                );
            }
        for (d in a)
          if (_ = a[d], A = e[d], a.hasOwnProperty(d) && _ !== A && (_ != null || A != null))
            switch (d) {
              case "selected":
                t.selected = _ && typeof _ != "function" && typeof _ != "symbol";
                break;
              default:
                At(
                  t,
                  l,
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
        for (var k in e)
          _ = e[k], e.hasOwnProperty(k) && _ != null && !a.hasOwnProperty(k) && At(t, l, k, null, a, _);
        for (S in a)
          if (_ = a[S], A = e[S], a.hasOwnProperty(S) && _ !== A && (_ != null || A != null))
            switch (S) {
              case "children":
              case "dangerouslySetInnerHTML":
                if (_ != null)
                  throw Error(s(137, l));
                break;
              default:
                At(
                  t,
                  l,
                  S,
                  _,
                  a,
                  A
                );
            }
        return;
      default:
        if (ji(l)) {
          for (var zt in e)
            _ = e[zt], e.hasOwnProperty(zt) && _ !== void 0 && !a.hasOwnProperty(zt) && _f(
              t,
              l,
              zt,
              void 0,
              a,
              _
            );
          for (z in a)
            _ = a[z], A = e[z], !a.hasOwnProperty(z) || _ === A || _ === void 0 && A === void 0 || _f(
              t,
              l,
              z,
              _,
              a,
              A
            );
          return;
        }
    }
    for (var h in e)
      _ = e[h], e.hasOwnProperty(h) && _ != null && !a.hasOwnProperty(h) && At(t, l, h, null, a, _);
    for (M in a)
      _ = a[M], A = e[M], !a.hasOwnProperty(M) || _ === A || _ == null && A == null || At(t, l, M, _, a, A);
  }
  function zd(t) {
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
  function oh() {
    if (typeof performance.getEntriesByType == "function") {
      for (var t = 0, l = 0, e = performance.getEntriesByType("resource"), a = 0; a < e.length; a++) {
        var u = e[a], n = u.transferSize, i = u.initiatorType, f = u.duration;
        if (n && f && zd(i)) {
          for (i = 0, f = u.responseEnd, a += 1; a < e.length; a++) {
            var d = e[a], S = d.startTime;
            if (S > f) break;
            var z = d.transferSize, M = d.initiatorType;
            z && zd(M) && (d = d.responseEnd, i += z * (d < f ? 1 : (f - S) / (d - S)));
          }
          if (--a, l += 8 * (n + i) / (u.duration / 1e3), t++, 10 < t) break;
        }
      }
      if (0 < t) return l / t / 1e6;
    }
    return navigator.connection && (t = navigator.connection.downlink, typeof t == "number") ? t : 5;
  }
  var Ef = null, Af = null;
  function ri(t) {
    return t.nodeType === 9 ? t : t.ownerDocument;
  }
  function Td(t) {
    switch (t) {
      case "http://www.w3.org/2000/svg":
        return 1;
      case "http://www.w3.org/1998/Math/MathML":
        return 2;
      default:
        return 0;
    }
  }
  function xd(t, l) {
    if (t === 0)
      switch (l) {
        case "svg":
          return 1;
        case "math":
          return 2;
        default:
          return 0;
      }
    return t === 1 && l === "foreignObject" ? 0 : t;
  }
  function zf(t, l) {
    return t === "textarea" || t === "noscript" || typeof l.children == "string" || typeof l.children == "number" || typeof l.children == "bigint" || typeof l.dangerouslySetInnerHTML == "object" && l.dangerouslySetInnerHTML !== null && l.dangerouslySetInnerHTML.__html != null;
  }
  var Tf = null;
  function dh() {
    var t = window.event;
    return t && t.type === "popstate" ? t === Tf ? !1 : (Tf = t, !0) : (Tf = null, !1);
  }
  var qd = typeof setTimeout == "function" ? setTimeout : void 0, yh = typeof clearTimeout == "function" ? clearTimeout : void 0, Nd = typeof Promise == "function" ? Promise : void 0, mh = typeof queueMicrotask == "function" ? queueMicrotask : typeof Nd < "u" ? function(t) {
    return Nd.resolve(null).then(t).catch(hh);
  } : qd;
  function hh(t) {
    setTimeout(function() {
      throw t;
    });
  }
  function Ge(t) {
    return t === "head";
  }
  function Od(t, l) {
    var e = l, a = 0;
    do {
      var u = e.nextSibling;
      if (t.removeChild(e), u && u.nodeType === 8)
        if (e = u.data, e === "/$" || e === "/&") {
          if (a === 0) {
            t.removeChild(u), lu(l);
            return;
          }
          a--;
        } else if (e === "$" || e === "$?" || e === "$~" || e === "$!" || e === "&")
          a++;
        else if (e === "html")
          ku(t.ownerDocument.documentElement);
        else if (e === "head") {
          e = t.ownerDocument.head, ku(e);
          for (var n = e.firstChild; n; ) {
            var i = n.nextSibling, f = n.nodeName;
            n[du] || f === "SCRIPT" || f === "STYLE" || f === "LINK" && n.rel.toLowerCase() === "stylesheet" || e.removeChild(n), n = i;
          }
        } else
          e === "body" && ku(t.ownerDocument.body);
      e = u;
    } while (e);
    lu(l);
  }
  function Md(t, l) {
    var e = t;
    t = 0;
    do {
      var a = e.nextSibling;
      if (e.nodeType === 1 ? l ? (e._stashedDisplay = e.style.display, e.style.display = "none") : (e.style.display = e._stashedDisplay || "", e.getAttribute("style") === "" && e.removeAttribute("style")) : e.nodeType === 3 && (l ? (e._stashedText = e.nodeValue, e.nodeValue = "") : e.nodeValue = e._stashedText || ""), a && a.nodeType === 8)
        if (e = a.data, e === "/$") {
          if (t === 0) break;
          t--;
        } else
          e !== "$" && e !== "$?" && e !== "$~" && e !== "$!" || t++;
      e = a;
    } while (e);
  }
  function xf(t) {
    var l = t.firstChild;
    for (l && l.nodeType === 10 && (l = l.nextSibling); l; ) {
      var e = l;
      switch (l = l.nextSibling, e.nodeName) {
        case "HTML":
        case "HEAD":
        case "BODY":
          xf(e), Mi(e);
          continue;
        case "SCRIPT":
        case "STYLE":
          continue;
        case "LINK":
          if (e.rel.toLowerCase() === "stylesheet") continue;
      }
      t.removeChild(e);
    }
  }
  function vh(t, l, e, a) {
    for (; t.nodeType === 1; ) {
      var u = e;
      if (t.nodeName.toLowerCase() !== l.toLowerCase()) {
        if (!a && (t.nodeName !== "INPUT" || t.type !== "hidden"))
          break;
      } else if (a) {
        if (!t[du])
          switch (l) {
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
      } else if (l === "input" && t.type === "hidden") {
        var n = u.name == null ? null : "" + u.name;
        if (u.type === "hidden" && t.getAttribute("name") === n)
          return t;
      } else return t;
      if (t = jl(t.nextSibling), t === null) break;
    }
    return null;
  }
  function gh(t, l, e) {
    if (l === "") return null;
    for (; t.nodeType !== 3; )
      if ((t.nodeType !== 1 || t.nodeName !== "INPUT" || t.type !== "hidden") && !e || (t = jl(t.nextSibling), t === null)) return null;
    return t;
  }
  function Dd(t, l) {
    for (; t.nodeType !== 8; )
      if ((t.nodeType !== 1 || t.nodeName !== "INPUT" || t.type !== "hidden") && !l || (t = jl(t.nextSibling), t === null)) return null;
    return t;
  }
  function qf(t) {
    return t.data === "$?" || t.data === "$~";
  }
  function Nf(t) {
    return t.data === "$!" || t.data === "$?" && t.ownerDocument.readyState !== "loading";
  }
  function ph(t, l) {
    var e = t.ownerDocument;
    if (t.data === "$~") t._reactRetry = l;
    else if (t.data !== "$?" || e.readyState !== "loading")
      l();
    else {
      var a = function() {
        l(), e.removeEventListener("DOMContentLoaded", a);
      };
      e.addEventListener("DOMContentLoaded", a), t._reactRetry = a;
    }
  }
  function jl(t) {
    for (; t != null; t = t.nextSibling) {
      var l = t.nodeType;
      if (l === 1 || l === 3) break;
      if (l === 8) {
        if (l = t.data, l === "$" || l === "$!" || l === "$?" || l === "$~" || l === "&" || l === "F!" || l === "F")
          break;
        if (l === "/$" || l === "/&") return null;
      }
    }
    return t;
  }
  var Of = null;
  function Ud(t) {
    t = t.nextSibling;
    for (var l = 0; t; ) {
      if (t.nodeType === 8) {
        var e = t.data;
        if (e === "/$" || e === "/&") {
          if (l === 0)
            return jl(t.nextSibling);
          l--;
        } else
          e !== "$" && e !== "$!" && e !== "$?" && e !== "$~" && e !== "&" || l++;
      }
      t = t.nextSibling;
    }
    return null;
  }
  function Cd(t) {
    t = t.previousSibling;
    for (var l = 0; t; ) {
      if (t.nodeType === 8) {
        var e = t.data;
        if (e === "$" || e === "$!" || e === "$?" || e === "$~" || e === "&") {
          if (l === 0) return t;
          l--;
        } else e !== "/$" && e !== "/&" || l++;
      }
      t = t.previousSibling;
    }
    return null;
  }
  function jd(t, l, e) {
    switch (l = ri(e), t) {
      case "html":
        if (t = l.documentElement, !t) throw Error(s(452));
        return t;
      case "head":
        if (t = l.head, !t) throw Error(s(453));
        return t;
      case "body":
        if (t = l.body, !t) throw Error(s(454));
        return t;
      default:
        throw Error(s(451));
    }
  }
  function ku(t) {
    for (var l = t.attributes; l.length; )
      t.removeAttributeNode(l[0]);
    Mi(t);
  }
  var Rl = /* @__PURE__ */ new Map(), Rd = /* @__PURE__ */ new Set();
  function oi(t) {
    return typeof t.getRootNode == "function" ? t.getRootNode() : t.nodeType === 9 ? t : t.ownerDocument;
  }
  var ge = H.d;
  H.d = {
    f: bh,
    r: Sh,
    D: _h,
    C: Eh,
    L: Ah,
    m: zh,
    X: xh,
    S: Th,
    M: qh
  };
  function bh() {
    var t = ge.f(), l = ei();
    return t || l;
  }
  function Sh(t) {
    var l = Sa(t);
    l !== null && l.tag === 5 && l.type === "form" ? Fr(l) : ge.r(t);
  }
  var Ia = typeof document > "u" ? null : document;
  function Hd(t, l, e) {
    var a = Ia;
    if (a && typeof l == "string" && l) {
      var u = ql(l);
      u = 'link[rel="' + t + '"][href="' + u + '"]', typeof e == "string" && (u += '[crossorigin="' + e + '"]'), Rd.has(u) || (Rd.add(u), t = { rel: t, crossOrigin: e, href: l }, a.querySelector(u) === null && (l = a.createElement("link"), Pt(l, "link", t), wt(l), a.head.appendChild(l)));
    }
  }
  function _h(t) {
    ge.D(t), Hd("dns-prefetch", t, null);
  }
  function Eh(t, l) {
    ge.C(t, l), Hd("preconnect", t, l);
  }
  function Ah(t, l, e) {
    ge.L(t, l, e);
    var a = Ia;
    if (a && t && l) {
      var u = 'link[rel="preload"][as="' + ql(l) + '"]';
      l === "image" && e && e.imageSrcSet ? (u += '[imagesrcset="' + ql(
        e.imageSrcSet
      ) + '"]', typeof e.imageSizes == "string" && (u += '[imagesizes="' + ql(
        e.imageSizes
      ) + '"]')) : u += '[href="' + ql(t) + '"]';
      var n = u;
      switch (l) {
        case "style":
          n = Pa(t);
          break;
        case "script":
          n = tu(t);
      }
      Rl.has(n) || (t = E(
        {
          rel: "preload",
          href: l === "image" && e && e.imageSrcSet ? void 0 : t,
          as: l
        },
        e
      ), Rl.set(n, t), a.querySelector(u) !== null || l === "style" && a.querySelector($u(n)) || l === "script" && a.querySelector(Wu(n)) || (l = a.createElement("link"), Pt(l, "link", t), wt(l), a.head.appendChild(l)));
    }
  }
  function zh(t, l) {
    ge.m(t, l);
    var e = Ia;
    if (e && t) {
      var a = l && typeof l.as == "string" ? l.as : "script", u = 'link[rel="modulepreload"][as="' + ql(a) + '"][href="' + ql(t) + '"]', n = u;
      switch (a) {
        case "audioworklet":
        case "paintworklet":
        case "serviceworker":
        case "sharedworker":
        case "worker":
        case "script":
          n = tu(t);
      }
      if (!Rl.has(n) && (t = E({ rel: "modulepreload", href: t }, l), Rl.set(n, t), e.querySelector(u) === null)) {
        switch (a) {
          case "audioworklet":
          case "paintworklet":
          case "serviceworker":
          case "sharedworker":
          case "worker":
          case "script":
            if (e.querySelector(Wu(n)))
              return;
        }
        a = e.createElement("link"), Pt(a, "link", t), wt(a), e.head.appendChild(a);
      }
    }
  }
  function Th(t, l, e) {
    ge.S(t, l, e);
    var a = Ia;
    if (a && t) {
      var u = _a(a).hoistableStyles, n = Pa(t);
      l = l || "default";
      var i = u.get(n);
      if (!i) {
        var f = { loading: 0, preload: null };
        if (i = a.querySelector(
          $u(n)
        ))
          f.loading = 5;
        else {
          t = E(
            { rel: "stylesheet", href: t, "data-precedence": l },
            e
          ), (e = Rl.get(n)) && Mf(t, e);
          var d = i = a.createElement("link");
          wt(d), Pt(d, "link", t), d._p = new Promise(function(S, z) {
            d.onload = S, d.onerror = z;
          }), d.addEventListener("load", function() {
            f.loading |= 1;
          }), d.addEventListener("error", function() {
            f.loading |= 2;
          }), f.loading |= 4, di(i, l, a);
        }
        i = {
          type: "stylesheet",
          instance: i,
          count: 1,
          state: f
        }, u.set(n, i);
      }
    }
  }
  function xh(t, l) {
    ge.X(t, l);
    var e = Ia;
    if (e && t) {
      var a = _a(e).hoistableScripts, u = tu(t), n = a.get(u);
      n || (n = e.querySelector(Wu(u)), n || (t = E({ src: t, async: !0 }, l), (l = Rl.get(u)) && Df(t, l), n = e.createElement("script"), wt(n), Pt(n, "link", t), e.head.appendChild(n)), n = {
        type: "script",
        instance: n,
        count: 1,
        state: null
      }, a.set(u, n));
    }
  }
  function qh(t, l) {
    ge.M(t, l);
    var e = Ia;
    if (e && t) {
      var a = _a(e).hoistableScripts, u = tu(t), n = a.get(u);
      n || (n = e.querySelector(Wu(u)), n || (t = E({ src: t, async: !0, type: "module" }, l), (l = Rl.get(u)) && Df(t, l), n = e.createElement("script"), wt(n), Pt(n, "link", t), e.head.appendChild(n)), n = {
        type: "script",
        instance: n,
        count: 1,
        state: null
      }, a.set(u, n));
    }
  }
  function Bd(t, l, e, a) {
    var u = (u = lt.current) ? oi(u) : null;
    if (!u) throw Error(s(446));
    switch (t) {
      case "meta":
      case "title":
        return null;
      case "style":
        return typeof e.precedence == "string" && typeof e.href == "string" ? (l = Pa(e.href), e = _a(
          u
        ).hoistableStyles, a = e.get(l), a || (a = {
          type: "style",
          instance: null,
          count: 0,
          state: null
        }, e.set(l, a)), a) : { type: "void", instance: null, count: 0, state: null };
      case "link":
        if (e.rel === "stylesheet" && typeof e.href == "string" && typeof e.precedence == "string") {
          t = Pa(e.href);
          var n = _a(
            u
          ).hoistableStyles, i = n.get(t);
          if (i || (u = u.ownerDocument || u, i = {
            type: "stylesheet",
            instance: null,
            count: 0,
            state: { loading: 0, preload: null }
          }, n.set(t, i), (n = u.querySelector(
            $u(t)
          )) && !n._p && (i.instance = n, i.state.loading = 5), Rl.has(t) || (e = {
            rel: "preload",
            as: "style",
            href: e.href,
            crossOrigin: e.crossOrigin,
            integrity: e.integrity,
            media: e.media,
            hrefLang: e.hrefLang,
            referrerPolicy: e.referrerPolicy
          }, Rl.set(t, e), n || Nh(
            u,
            t,
            e,
            i.state
          ))), l && a === null)
            throw Error(s(528, ""));
          return i;
        }
        if (l && a !== null)
          throw Error(s(529, ""));
        return null;
      case "script":
        return l = e.async, e = e.src, typeof e == "string" && l && typeof l != "function" && typeof l != "symbol" ? (l = tu(e), e = _a(
          u
        ).hoistableScripts, a = e.get(l), a || (a = {
          type: "script",
          instance: null,
          count: 0,
          state: null
        }, e.set(l, a)), a) : { type: "void", instance: null, count: 0, state: null };
      default:
        throw Error(s(444, t));
    }
  }
  function Pa(t) {
    return 'href="' + ql(t) + '"';
  }
  function $u(t) {
    return 'link[rel="stylesheet"][' + t + "]";
  }
  function Yd(t) {
    return E({}, t, {
      "data-precedence": t.precedence,
      precedence: null
    });
  }
  function Nh(t, l, e, a) {
    t.querySelector('link[rel="preload"][as="style"][' + l + "]") ? a.loading = 1 : (l = t.createElement("link"), a.preload = l, l.addEventListener("load", function() {
      return a.loading |= 1;
    }), l.addEventListener("error", function() {
      return a.loading |= 2;
    }), Pt(l, "link", e), wt(l), t.head.appendChild(l));
  }
  function tu(t) {
    return '[src="' + ql(t) + '"]';
  }
  function Wu(t) {
    return "script[async]" + t;
  }
  function Ld(t, l, e) {
    if (l.count++, l.instance === null)
      switch (l.type) {
        case "style":
          var a = t.querySelector(
            'style[data-href~="' + ql(e.href) + '"]'
          );
          if (a)
            return l.instance = a, wt(a), a;
          var u = E({}, e, {
            "data-href": e.href,
            "data-precedence": e.precedence,
            href: null,
            precedence: null
          });
          return a = (t.ownerDocument || t).createElement(
            "style"
          ), wt(a), Pt(a, "style", u), di(a, e.precedence, t), l.instance = a;
        case "stylesheet":
          u = Pa(e.href);
          var n = t.querySelector(
            $u(u)
          );
          if (n)
            return l.state.loading |= 4, l.instance = n, wt(n), n;
          a = Yd(e), (u = Rl.get(u)) && Mf(a, u), n = (t.ownerDocument || t).createElement("link"), wt(n);
          var i = n;
          return i._p = new Promise(function(f, d) {
            i.onload = f, i.onerror = d;
          }), Pt(n, "link", a), l.state.loading |= 4, di(n, e.precedence, t), l.instance = n;
        case "script":
          return n = tu(e.src), (u = t.querySelector(
            Wu(n)
          )) ? (l.instance = u, wt(u), u) : (a = e, (u = Rl.get(n)) && (a = E({}, e), Df(a, u)), t = t.ownerDocument || t, u = t.createElement("script"), wt(u), Pt(u, "link", a), t.head.appendChild(u), l.instance = u);
        case "void":
          return null;
        default:
          throw Error(s(443, l.type));
      }
    else
      l.type === "stylesheet" && (l.state.loading & 4) === 0 && (a = l.instance, l.state.loading |= 4, di(a, e.precedence, t));
    return l.instance;
  }
  function di(t, l, e) {
    for (var a = e.querySelectorAll(
      'link[rel="stylesheet"][data-precedence],style[data-precedence]'
    ), u = a.length ? a[a.length - 1] : null, n = u, i = 0; i < a.length; i++) {
      var f = a[i];
      if (f.dataset.precedence === l) n = f;
      else if (n !== u) break;
    }
    n ? n.parentNode.insertBefore(t, n.nextSibling) : (l = e.nodeType === 9 ? e.head : e, l.insertBefore(t, l.firstChild));
  }
  function Mf(t, l) {
    t.crossOrigin == null && (t.crossOrigin = l.crossOrigin), t.referrerPolicy == null && (t.referrerPolicy = l.referrerPolicy), t.title == null && (t.title = l.title);
  }
  function Df(t, l) {
    t.crossOrigin == null && (t.crossOrigin = l.crossOrigin), t.referrerPolicy == null && (t.referrerPolicy = l.referrerPolicy), t.integrity == null && (t.integrity = l.integrity);
  }
  var yi = null;
  function Gd(t, l, e) {
    if (yi === null) {
      var a = /* @__PURE__ */ new Map(), u = yi = /* @__PURE__ */ new Map();
      u.set(e, a);
    } else
      u = yi, a = u.get(e), a || (a = /* @__PURE__ */ new Map(), u.set(e, a));
    if (a.has(t)) return a;
    for (a.set(t, null), e = e.getElementsByTagName(t), u = 0; u < e.length; u++) {
      var n = e[u];
      if (!(n[du] || n[$t] || t === "link" && n.getAttribute("rel") === "stylesheet") && n.namespaceURI !== "http://www.w3.org/2000/svg") {
        var i = n.getAttribute(l) || "";
        i = t + i;
        var f = a.get(i);
        f ? f.push(n) : a.set(i, [n]);
      }
    }
    return a;
  }
  function Qd(t, l, e) {
    t = t.ownerDocument || t, t.head.insertBefore(
      e,
      l === "title" ? t.querySelector("head > title") : null
    );
  }
  function Oh(t, l, e) {
    if (e === 1 || l.itemProp != null) return !1;
    switch (t) {
      case "meta":
      case "title":
        return !0;
      case "style":
        if (typeof l.precedence != "string" || typeof l.href != "string" || l.href === "")
          break;
        return !0;
      case "link":
        if (typeof l.rel != "string" || typeof l.href != "string" || l.href === "" || l.onLoad || l.onError)
          break;
        switch (l.rel) {
          case "stylesheet":
            return t = l.disabled, typeof l.precedence == "string" && t == null;
          default:
            return !0;
        }
      case "script":
        if (l.async && typeof l.async != "function" && typeof l.async != "symbol" && !l.onLoad && !l.onError && l.src && typeof l.src == "string")
          return !0;
    }
    return !1;
  }
  function Xd(t) {
    return !(t.type === "stylesheet" && (t.state.loading & 3) === 0);
  }
  function Mh(t, l, e, a) {
    if (e.type === "stylesheet" && (typeof a.media != "string" || matchMedia(a.media).matches !== !1) && (e.state.loading & 4) === 0) {
      if (e.instance === null) {
        var u = Pa(a.href), n = l.querySelector(
          $u(u)
        );
        if (n) {
          l = n._p, l !== null && typeof l == "object" && typeof l.then == "function" && (t.count++, t = mi.bind(t), l.then(t, t)), e.state.loading |= 4, e.instance = n, wt(n);
          return;
        }
        n = l.ownerDocument || l, a = Yd(a), (u = Rl.get(u)) && Mf(a, u), n = n.createElement("link"), wt(n);
        var i = n;
        i._p = new Promise(function(f, d) {
          i.onload = f, i.onerror = d;
        }), Pt(n, "link", a), e.instance = n;
      }
      t.stylesheets === null && (t.stylesheets = /* @__PURE__ */ new Map()), t.stylesheets.set(e, l), (l = e.state.preload) && (e.state.loading & 3) === 0 && (t.count++, e = mi.bind(t), l.addEventListener("load", e), l.addEventListener("error", e));
    }
  }
  var Uf = 0;
  function Dh(t, l) {
    return t.stylesheets && t.count === 0 && vi(t, t.stylesheets), 0 < t.count || 0 < t.imgCount ? function(e) {
      var a = setTimeout(function() {
        if (t.stylesheets && vi(t, t.stylesheets), t.unsuspend) {
          var n = t.unsuspend;
          t.unsuspend = null, n();
        }
      }, 6e4 + l);
      0 < t.imgBytes && Uf === 0 && (Uf = 62500 * oh());
      var u = setTimeout(
        function() {
          if (t.waitingForImages = !1, t.count === 0 && (t.stylesheets && vi(t, t.stylesheets), t.unsuspend)) {
            var n = t.unsuspend;
            t.unsuspend = null, n();
          }
        },
        (t.imgBytes > Uf ? 50 : 800) + l
      );
      return t.unsuspend = e, function() {
        t.unsuspend = null, clearTimeout(a), clearTimeout(u);
      };
    } : null;
  }
  function mi() {
    if (this.count--, this.count === 0 && (this.imgCount === 0 || !this.waitingForImages)) {
      if (this.stylesheets) vi(this, this.stylesheets);
      else if (this.unsuspend) {
        var t = this.unsuspend;
        this.unsuspend = null, t();
      }
    }
  }
  var hi = null;
  function vi(t, l) {
    t.stylesheets = null, t.unsuspend !== null && (t.count++, hi = /* @__PURE__ */ new Map(), l.forEach(Uh, t), hi = null, mi.call(t));
  }
  function Uh(t, l) {
    if (!(l.state.loading & 4)) {
      var e = hi.get(t);
      if (e) var a = e.get(null);
      else {
        e = /* @__PURE__ */ new Map(), hi.set(t, e);
        for (var u = t.querySelectorAll(
          "link[data-precedence],style[data-precedence]"
        ), n = 0; n < u.length; n++) {
          var i = u[n];
          (i.nodeName === "LINK" || i.getAttribute("media") !== "not all") && (e.set(i.dataset.precedence, i), a = i);
        }
        a && e.set(null, a);
      }
      u = l.instance, i = u.getAttribute("data-precedence"), n = e.get(i) || a, n === a && e.set(null, u), e.set(i, u), this.count++, a = mi.bind(this), u.addEventListener("load", a), u.addEventListener("error", a), n ? n.parentNode.insertBefore(u, n.nextSibling) : (t = t.nodeType === 9 ? t.head : t, t.insertBefore(u, t.firstChild)), l.state.loading |= 4;
    }
  }
  var Fu = {
    $$typeof: J,
    Provider: null,
    Consumer: null,
    _currentValue: Z,
    _currentValue2: Z,
    _threadCount: 0
  };
  function Ch(t, l, e, a, u, n, i, f, d) {
    this.tag = 1, this.containerInfo = t, this.pingCache = this.current = this.pendingChildren = null, this.timeoutHandle = -1, this.callbackNode = this.next = this.pendingContext = this.context = this.cancelPendingCommit = null, this.callbackPriority = 0, this.expirationTimes = xi(-1), this.entangledLanes = this.shellSuspendCounter = this.errorRecoveryDisabledLanes = this.expiredLanes = this.warmLanes = this.pingedLanes = this.suspendedLanes = this.pendingLanes = 0, this.entanglements = xi(0), this.hiddenUpdates = xi(null), this.identifierPrefix = a, this.onUncaughtError = u, this.onCaughtError = n, this.onRecoverableError = i, this.pooledCache = null, this.pooledCacheLanes = 0, this.formState = d, this.incompleteTransitions = /* @__PURE__ */ new Map();
  }
  function Zd(t, l, e, a, u, n, i, f, d, S, z, M) {
    return t = new Ch(
      t,
      l,
      e,
      i,
      d,
      S,
      z,
      M,
      f
    ), l = 1, n === !0 && (l |= 24), n = Sl(3, null, null, l), t.current = n, n.stateNode = t, l = rc(), l.refCount++, t.pooledCache = l, l.refCount++, n.memoizedState = {
      element: a,
      isDehydrated: e,
      cache: l
    }, mc(n), t;
  }
  function Vd(t) {
    return t ? (t = Da, t) : Da;
  }
  function Kd(t, l, e, a, u, n) {
    u = Vd(u), a.context === null ? a.context = u : a.pendingContext = u, a = Oe(l), a.payload = { element: e }, n = n === void 0 ? null : n, n !== null && (a.callback = n), e = Me(t, a, l), e !== null && (ml(e, t, l), Ou(e, t, l));
  }
  function Jd(t, l) {
    if (t = t.memoizedState, t !== null && t.dehydrated !== null) {
      var e = t.retryLane;
      t.retryLane = e !== 0 && e < l ? e : l;
    }
  }
  function Cf(t, l) {
    Jd(t, l), (t = t.alternate) && Jd(t, l);
  }
  function wd(t) {
    if (t.tag === 13 || t.tag === 31) {
      var l = la(t, 67108864);
      l !== null && ml(l, t, 67108864), Cf(t, 67108864);
    }
  }
  function kd(t) {
    if (t.tag === 13 || t.tag === 31) {
      var l = Tl();
      l = qi(l);
      var e = la(t, l);
      e !== null && ml(e, t, l), Cf(t, l);
    }
  }
  var gi = !0;
  function jh(t, l, e, a) {
    var u = T.T;
    T.T = null;
    var n = H.p;
    try {
      H.p = 2, jf(t, l, e, a);
    } finally {
      H.p = n, T.T = u;
    }
  }
  function Rh(t, l, e, a) {
    var u = T.T;
    T.T = null;
    var n = H.p;
    try {
      H.p = 8, jf(t, l, e, a);
    } finally {
      H.p = n, T.T = u;
    }
  }
  function jf(t, l, e, a) {
    if (gi) {
      var u = Rf(a);
      if (u === null)
        Sf(
          t,
          l,
          a,
          pi,
          e
        ), Wd(t, a);
      else if (Bh(
        u,
        t,
        l,
        e,
        a
      ))
        a.stopPropagation();
      else if (Wd(t, a), l & 4 && -1 < Hh.indexOf(t)) {
        for (; u !== null; ) {
          var n = Sa(u);
          if (n !== null)
            switch (n.tag) {
              case 3:
                if (n = n.stateNode, n.current.memoizedState.isDehydrated) {
                  var i = We(n.pendingLanes);
                  if (i !== 0) {
                    var f = n;
                    for (f.pendingLanes |= 2, f.entangledLanes |= 2; i; ) {
                      var d = 1 << 31 - pl(i);
                      f.entanglements[1] |= d, i &= ~d;
                    }
                    Wl(n), (gt & 6) === 0 && (ti = ll() + 500, Ku(0));
                  }
                }
                break;
              case 31:
              case 13:
                f = la(n, 2), f !== null && ml(f, n, 2), ei(), Cf(n, 2);
            }
          if (n = Rf(a), n === null && Sf(
            t,
            l,
            a,
            pi,
            e
          ), n === u) break;
          u = n;
        }
        u !== null && a.stopPropagation();
      } else
        Sf(
          t,
          l,
          a,
          null,
          e
        );
    }
  }
  function Rf(t) {
    return t = Hi(t), Hf(t);
  }
  var pi = null;
  function Hf(t) {
    if (pi = null, t = ba(t), t !== null) {
      var l = v(t);
      if (l === null) t = null;
      else {
        var e = l.tag;
        if (e === 13) {
          if (t = D(l), t !== null) return t;
          t = null;
        } else if (e === 31) {
          if (t = U(l), t !== null) return t;
          t = null;
        } else if (e === 3) {
          if (l.stateNode.current.memoizedState.isDehydrated)
            return l.tag === 3 ? l.stateNode.containerInfo : null;
          t = null;
        } else l !== t && (t = null);
      }
    }
    return pi = t, null;
  }
  function $d(t) {
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
        switch (_y()) {
          case es:
            return 2;
          case as:
            return 8;
          case cn:
          case Ey:
            return 32;
          case us:
            return 268435456;
          default:
            return 32;
        }
      default:
        return 32;
    }
  }
  var Bf = !1, Qe = null, Xe = null, Ze = null, Iu = /* @__PURE__ */ new Map(), Pu = /* @__PURE__ */ new Map(), Ve = [], Hh = "mousedown mouseup touchcancel touchend touchstart auxclick dblclick pointercancel pointerdown pointerup dragend dragstart drop compositionend compositionstart keydown keypress keyup input textInput copy cut paste click change contextmenu reset".split(
    " "
  );
  function Wd(t, l) {
    switch (t) {
      case "focusin":
      case "focusout":
        Qe = null;
        break;
      case "dragenter":
      case "dragleave":
        Xe = null;
        break;
      case "mouseover":
      case "mouseout":
        Ze = null;
        break;
      case "pointerover":
      case "pointerout":
        Iu.delete(l.pointerId);
        break;
      case "gotpointercapture":
      case "lostpointercapture":
        Pu.delete(l.pointerId);
    }
  }
  function tn(t, l, e, a, u, n) {
    return t === null || t.nativeEvent !== n ? (t = {
      blockedOn: l,
      domEventName: e,
      eventSystemFlags: a,
      nativeEvent: n,
      targetContainers: [u]
    }, l !== null && (l = Sa(l), l !== null && wd(l)), t) : (t.eventSystemFlags |= a, l = t.targetContainers, u !== null && l.indexOf(u) === -1 && l.push(u), t);
  }
  function Bh(t, l, e, a, u) {
    switch (l) {
      case "focusin":
        return Qe = tn(
          Qe,
          t,
          l,
          e,
          a,
          u
        ), !0;
      case "dragenter":
        return Xe = tn(
          Xe,
          t,
          l,
          e,
          a,
          u
        ), !0;
      case "mouseover":
        return Ze = tn(
          Ze,
          t,
          l,
          e,
          a,
          u
        ), !0;
      case "pointerover":
        var n = u.pointerId;
        return Iu.set(
          n,
          tn(
            Iu.get(n) || null,
            t,
            l,
            e,
            a,
            u
          )
        ), !0;
      case "gotpointercapture":
        return n = u.pointerId, Pu.set(
          n,
          tn(
            Pu.get(n) || null,
            t,
            l,
            e,
            a,
            u
          )
        ), !0;
    }
    return !1;
  }
  function Fd(t) {
    var l = ba(t.target);
    if (l !== null) {
      var e = v(l);
      if (e !== null) {
        if (l = e.tag, l === 13) {
          if (l = D(e), l !== null) {
            t.blockedOn = l, rs(t.priority, function() {
              kd(e);
            });
            return;
          }
        } else if (l === 31) {
          if (l = U(e), l !== null) {
            t.blockedOn = l, rs(t.priority, function() {
              kd(e);
            });
            return;
          }
        } else if (l === 3 && e.stateNode.current.memoizedState.isDehydrated) {
          t.blockedOn = e.tag === 3 ? e.stateNode.containerInfo : null;
          return;
        }
      }
    }
    t.blockedOn = null;
  }
  function bi(t) {
    if (t.blockedOn !== null) return !1;
    for (var l = t.targetContainers; 0 < l.length; ) {
      var e = Rf(t.nativeEvent);
      if (e === null) {
        e = t.nativeEvent;
        var a = new e.constructor(
          e.type,
          e
        );
        Ri = a, e.target.dispatchEvent(a), Ri = null;
      } else
        return l = Sa(e), l !== null && wd(l), t.blockedOn = e, !1;
      l.shift();
    }
    return !0;
  }
  function Id(t, l, e) {
    bi(t) && e.delete(l);
  }
  function Yh() {
    Bf = !1, Qe !== null && bi(Qe) && (Qe = null), Xe !== null && bi(Xe) && (Xe = null), Ze !== null && bi(Ze) && (Ze = null), Iu.forEach(Id), Pu.forEach(Id);
  }
  function Si(t, l) {
    t.blockedOn === l && (t.blockedOn = null, Bf || (Bf = !0, c.unstable_scheduleCallback(
      c.unstable_NormalPriority,
      Yh
    )));
  }
  var _i = null;
  function Pd(t) {
    _i !== t && (_i = t, c.unstable_scheduleCallback(
      c.unstable_NormalPriority,
      function() {
        _i === t && (_i = null);
        for (var l = 0; l < t.length; l += 3) {
          var e = t[l], a = t[l + 1], u = t[l + 2];
          if (typeof a != "function") {
            if (Hf(a || e) === null)
              continue;
            break;
          }
          var n = Sa(e);
          n !== null && (t.splice(l, 3), l -= 3, jc(
            n,
            {
              pending: !0,
              data: u,
              method: e.method,
              action: a
            },
            a,
            u
          ));
        }
      }
    ));
  }
  function lu(t) {
    function l(d) {
      return Si(d, t);
    }
    Qe !== null && Si(Qe, t), Xe !== null && Si(Xe, t), Ze !== null && Si(Ze, t), Iu.forEach(l), Pu.forEach(l);
    for (var e = 0; e < Ve.length; e++) {
      var a = Ve[e];
      a.blockedOn === t && (a.blockedOn = null);
    }
    for (; 0 < Ve.length && (e = Ve[0], e.blockedOn === null); )
      Fd(e), e.blockedOn === null && Ve.shift();
    if (e = (t.ownerDocument || t).$$reactFormReplay, e != null)
      for (a = 0; a < e.length; a += 3) {
        var u = e[a], n = e[a + 1], i = u[fl] || null;
        if (typeof n == "function")
          i || Pd(e);
        else if (i) {
          var f = null;
          if (n && n.hasAttribute("formAction")) {
            if (u = n, i = n[fl] || null)
              f = i.formAction;
            else if (Hf(u) !== null) continue;
          } else f = i.action;
          typeof f == "function" ? e[a + 1] = f : (e.splice(a, 3), a -= 3), Pd(e);
        }
      }
  }
  function ty() {
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
    function l() {
      u !== null && (u(), u = null), a || setTimeout(e, 20);
    }
    function e() {
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
      return navigation.addEventListener("navigate", t), navigation.addEventListener("navigatesuccess", l), navigation.addEventListener("navigateerror", l), setTimeout(e, 100), function() {
        a = !0, navigation.removeEventListener("navigate", t), navigation.removeEventListener("navigatesuccess", l), navigation.removeEventListener("navigateerror", l), u !== null && (u(), u = null);
      };
    }
  }
  function Yf(t) {
    this._internalRoot = t;
  }
  Ei.prototype.render = Yf.prototype.render = function(t) {
    var l = this._internalRoot;
    if (l === null) throw Error(s(409));
    var e = l.current, a = Tl();
    Kd(e, a, t, l, null, null);
  }, Ei.prototype.unmount = Yf.prototype.unmount = function() {
    var t = this._internalRoot;
    if (t !== null) {
      this._internalRoot = null;
      var l = t.containerInfo;
      Kd(t.current, 2, null, t, null, null), ei(), l[pa] = null;
    }
  };
  function Ei(t) {
    this._internalRoot = t;
  }
  Ei.prototype.unstable_scheduleHydration = function(t) {
    if (t) {
      var l = ss();
      t = { blockedOn: null, target: t, priority: l };
      for (var e = 0; e < Ve.length && l !== 0 && l < Ve[e].priority; e++) ;
      Ve.splice(e, 0, t), e === 0 && Fd(t);
    }
  };
  var ly = r.version;
  if (ly !== "19.2.5")
    throw Error(
      s(
        527,
        ly,
        "19.2.5"
      )
    );
  H.findDOMNode = function(t) {
    var l = t._reactInternals;
    if (l === void 0)
      throw typeof t.render == "function" ? Error(s(188)) : (t = Object.keys(t).join(","), Error(s(268, t)));
    return t = g(l), t = t !== null ? R(t) : null, t = t === null ? null : t.stateNode, t;
  };
  var Lh = {
    bundleType: 0,
    version: "19.2.5",
    rendererPackageName: "react-dom",
    currentDispatcherRef: T,
    reconcilerVersion: "19.2.5"
  };
  if (typeof __REACT_DEVTOOLS_GLOBAL_HOOK__ < "u") {
    var Ai = __REACT_DEVTOOLS_GLOBAL_HOOK__;
    if (!Ai.isDisabled && Ai.supportsFiber)
      try {
        su = Ai.inject(
          Lh
        ), gl = Ai;
      } catch {
      }
  }
  return ln.createRoot = function(t, l) {
    if (!p(t)) throw Error(s(299));
    var e = !1, a = "", u = co, n = fo, i = so;
    return l != null && (l.unstable_strictMode === !0 && (e = !0), l.identifierPrefix !== void 0 && (a = l.identifierPrefix), l.onUncaughtError !== void 0 && (u = l.onUncaughtError), l.onCaughtError !== void 0 && (n = l.onCaughtError), l.onRecoverableError !== void 0 && (i = l.onRecoverableError)), l = Zd(
      t,
      1,
      !1,
      null,
      null,
      e,
      a,
      null,
      u,
      n,
      i,
      ty
    ), t[pa] = l.current, bf(t), new Yf(l);
  }, ln.hydrateRoot = function(t, l, e) {
    if (!p(t)) throw Error(s(299));
    var a = !1, u = "", n = co, i = fo, f = so, d = null;
    return e != null && (e.unstable_strictMode === !0 && (a = !0), e.identifierPrefix !== void 0 && (u = e.identifierPrefix), e.onUncaughtError !== void 0 && (n = e.onUncaughtError), e.onCaughtError !== void 0 && (i = e.onCaughtError), e.onRecoverableError !== void 0 && (f = e.onRecoverableError), e.formState !== void 0 && (d = e.formState)), l = Zd(
      t,
      1,
      !0,
      l,
      e ?? null,
      a,
      u,
      d,
      n,
      i,
      f,
      ty
    ), l.context = Vd(null), e = l.current, a = Tl(), a = qi(a), u = Oe(a), u.callback = null, Me(e, u, a), e = a, l.current.lanes = e, ou(l, e), Wl(l), t[pa] = l.current, bf(t), new Ei(l);
  }, ln.version = "19.2.5", ln;
}
var ry;
function $h() {
  if (ry) return Qf.exports;
  ry = 1;
  function c() {
    if (!(typeof __REACT_DEVTOOLS_GLOBAL_HOOK__ > "u" || typeof __REACT_DEVTOOLS_GLOBAL_HOOK__.checkDCE != "function"))
      try {
        __REACT_DEVTOOLS_GLOBAL_HOOK__.checkDCE(c);
      } catch (r) {
        console.error(r);
      }
  }
  return c(), Qf.exports = kh(), Qf.exports;
}
var Wh = $h(), Kf = { exports: {} }, en = {};
/**
 * @license React
 * react-jsx-runtime.production.js
 *
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 */
var oy;
function Fh() {
  if (oy) return en;
  oy = 1;
  var c = Symbol.for("react.transitional.element"), r = Symbol.for("react.fragment");
  function o(s, p, v) {
    var D = null;
    if (v !== void 0 && (D = "" + v), p.key !== void 0 && (D = "" + p.key), "key" in p) {
      v = {};
      for (var U in p)
        U !== "key" && (v[U] = p[U]);
    } else v = p;
    return p = v.ref, {
      $$typeof: c,
      type: s,
      key: D,
      ref: p !== void 0 ? p : null,
      props: v
    };
  }
  return en.Fragment = r, en.jsx = o, en.jsxs = o, en;
}
var dy;
function Ih() {
  return dy || (dy = 1, Kf.exports = Fh()), Kf.exports;
}
var N = Ih();
function Ph(c) {
  return typeof c.questionId == "string";
}
function tv(c) {
  const r = c;
  return Array.isArray(r.all) || Array.isArray(r.any);
}
function lv(c) {
  return typeof c.expression == "string";
}
class Il extends Error {
  constructor(o, s) {
    super(`Expression syntax error at column ${s}: ${o}`);
    pe(this, "position");
    this.position = s, this.name = "ExpressionSyntaxError";
  }
}
function zi(c) {
  return c >= "0" && c <= "9";
}
function gy(c) {
  return c >= "a" && c <= "z" || c >= "A" && c <= "Z";
}
function ev(c) {
  return gy(c) || zi(c);
}
function av(c) {
  return c === " " || c === "	" || c === `
` || c === "\r" || c === "\f" || c === "\v";
}
function uv(c) {
  const r = [];
  let o = 0;
  const s = () => o >= c.length, p = (E = 0) => c.charAt(o + E), v = (E) => {
    if (o + E.length > c.length)
      return !1;
    for (let C = 0; C < E.length; C++)
      if (c.charAt(o + C) !== E.charAt(C))
        return !1;
    return o += E.length, !0;
  }, D = () => {
    for (; !s() && av(p()); )
      o++;
  }, U = (E) => {
    for (; !s() && zi(p()); )
      o++;
    if (!s() && p() === ".")
      for (o++; !s() && zi(p()); )
        o++;
    const C = c.substring(E, o), Q = parseFloat(C);
    return { kind: "Number", text: C, literal: Q, position: E };
  }, O = (E, C) => {
    o++;
    let Q = "";
    for (; !s() && p() !== C; ) {
      const w = p();
      if (w === "\\" && o + 1 < c.length) {
        const W = p(1), V = {
          n: `
`,
          t: "	",
          r: "\r",
          "\\": "\\",
          "'": "'",
          '"': '"'
        }[W];
        if (V === void 0)
          throw new Il(`unknown escape '\\${W}'.`, o);
        Q += V, o += 2;
      } else
        Q += w, o++;
    }
    if (s())
      throw new Il("unterminated string literal.", E);
    return o++, { kind: "String", text: Q, literal: Q, position: E };
  }, g = (E) => {
    for (; !s(); ) {
      const Q = p();
      if (Q === "_" || Q === "-" || ev(Q))
        o++;
      else
        break;
    }
    const C = c.substring(E, o);
    return C === "true" ? { kind: "True", text: C, literal: !0, position: E } : C === "false" ? { kind: "False", text: C, literal: !1, position: E } : C === "null" ? { kind: "Null", text: C, literal: null, position: E } : { kind: "Identifier", text: C, literal: null, position: E };
  }, R = () => {
    const E = o, C = p();
    if (zi(C))
      return U(E);
    if (C === "'" || C === '"')
      return O(E, C);
    if (C === "_" || gy(C))
      return g(E);
    switch (C) {
      case "(":
        return o++, { kind: "LParen", text: "(", literal: null, position: E };
      case ")":
        return o++, { kind: "RParen", text: ")", literal: null, position: E };
      case "[":
        return o++, { kind: "LBracket", text: "[", literal: null, position: E };
      case "]":
        return o++, { kind: "RBracket", text: "]", literal: null, position: E };
      case ",":
        return o++, { kind: "Comma", text: ",", literal: null, position: E };
      case ".":
        return o++, { kind: "Dot", text: ".", literal: null, position: E };
      case "=":
        if (v("==="))
          return { kind: "StrictEq", text: "===", literal: null, position: E };
        if (v("=="))
          return { kind: "Eq", text: "==", literal: null, position: E };
        throw new Il("bare '=' is not a valid operator (use '==' or '===').", E);
      case "!":
        return v("!==") ? { kind: "StrictNotEq", text: "!==", literal: null, position: E } : v("!=") ? { kind: "NotEq", text: "!=", literal: null, position: E } : (o++, { kind: "Not", text: "!", literal: null, position: E });
      case "<":
        return v("<=") ? { kind: "LtEq", text: "<=", literal: null, position: E } : (o++, { kind: "Lt", text: "<", literal: null, position: E });
      case ">":
        return v(">=") ? { kind: "GtEq", text: ">=", literal: null, position: E } : (o++, { kind: "Gt", text: ">", literal: null, position: E });
      case "&":
        if (v("&&"))
          return { kind: "And", text: "&&", literal: null, position: E };
        throw new Il("expected '&&'.", E);
      case "|":
        if (v("||"))
          return { kind: "Or", text: "||", literal: null, position: E };
        throw new Il("expected '||'.", E);
    }
    throw new Il(`unexpected character '${C}'.`, E);
  };
  for (; ; ) {
    if (D(), s())
      return r.push({ kind: "EndOfInput", text: "", literal: null, position: o }), r;
    r.push(R());
  }
}
function nv(c) {
  let r = 0;
  const o = () => {
    const L = c[r];
    if (!L)
      throw new Il("unexpected end of tokens.", 0);
    return L;
  }, s = () => {
    const L = o();
    return L.kind !== "EndOfInput" && r++, L;
  }, p = (L) => o().kind !== L ? !1 : (s(), !0), v = (L) => {
    const V = o();
    if (V.kind !== L)
      throw new Il(`expected ${L}, got '${V.text}'.`, V.position);
    return s(), V;
  }, D = () => {
    let L = U();
    for (; p("Or"); )
      L = { kind: "BinaryOp", op: "||", left: L, right: U() };
    return L;
  }, U = () => {
    let L = O();
    for (; p("And"); )
      L = { kind: "BinaryOp", op: "&&", left: L, right: O() };
    return L;
  }, O = () => {
    let L = g();
    for (; ; ) {
      const V = o().kind;
      let Tt = null;
      if (V === "Eq" || V === "StrictEq" ? Tt = "==" : (V === "NotEq" || V === "StrictNotEq") && (Tt = "!="), Tt === null)
        break;
      s(), L = { kind: "BinaryOp", op: Tt, left: L, right: g() };
    }
    return L;
  }, g = () => {
    let L = R();
    for (; ; ) {
      const V = o().kind;
      let Tt = null;
      if (V === "Lt" ? Tt = "<" : V === "Gt" ? Tt = ">" : V === "LtEq" ? Tt = "<=" : V === "GtEq" && (Tt = ">="), Tt === null)
        break;
      s(), L = { kind: "BinaryOp", op: Tt, left: L, right: R() };
    }
    return L;
  }, R = () => p("Not") ? { kind: "UnaryOp", op: "!", operand: R() } : w(), E = () => {
    v("LBracket");
    const L = [];
    if (o().kind !== "RBracket")
      for (L.push(D()); p("Comma"); )
        L.push(D());
    return v("RBracket"), { kind: "Array", items: L };
  }, C = (L) => {
    let V;
    if (p("Dot"))
      V = v("Identifier").text;
    else if (p("LBracket")) {
      const Tt = v("String");
      v("RBracket"), V = Tt.literal;
    } else
      throw new Il("'answers' must be followed by .key or ['key'].", L);
    return { kind: "AnswersAccess", key: V };
  }, Q = () => {
    const L = s();
    if (L.text === "answers")
      return C(L.position);
    v("LParen");
    const V = [];
    if (o().kind !== "RParen")
      for (V.push(D()); p("Comma"); )
        V.push(D());
    return v("RParen"), { kind: "Call", name: L.text, args: V };
  }, w = () => {
    const L = o();
    switch (L.kind) {
      case "Number":
      case "String":
      case "True":
      case "False":
      case "Null":
        return s(), { kind: "Literal", value: L.literal };
      case "LParen": {
        s();
        const V = D();
        return v("RParen"), V;
      }
      case "LBracket":
        return E();
      case "Identifier":
        return Q();
      default:
        throw new Il(`unexpected token '${L.text}'.`, L.position);
    }
  }, W = D();
  return v("EndOfInput"), W;
}
function be(c) {
  return c === void 0 || c === null ? null : typeof c == "boolean" || typeof c == "number" || typeof c == "string" ? c : Array.isArray(c) ? c.map(be) : null;
}
function va(c, r) {
  const o = be(c), s = be(r);
  if (o === null || s === null)
    return o === null && s === null;
  if (typeof o == "number" && typeof s == "number" || typeof o == "string" && typeof s == "string" || typeof o == "boolean" && typeof s == "boolean")
    return o === s;
  if (Array.isArray(o) && Array.isArray(s)) {
    if (o.length !== s.length)
      return !1;
    for (let p = 0; p < o.length; p++)
      if (!va(o[p], s[p]))
        return !1;
    return !0;
  }
  return !1;
}
function $e(c, r) {
  const o = be(c), s = be(r);
  if (typeof o == "number" && typeof s == "number" || typeof o == "string" && typeof s == "string")
    return o < s ? -1 : o > s ? 1 : 0;
  throw new Error("Comparison operators require two numbers or two strings.");
}
function uu(c) {
  const r = be(c);
  return r === null ? !1 : typeof r == "boolean" ? r : typeof r == "number" ? r !== 0 : typeof r == "string" || Array.isArray(r) ? r.length > 0 : !0;
}
function Bl(c, r) {
  switch (c.kind) {
    case "Literal":
      return c.value;
    case "AnswersAccess":
      return rv(c.key, r);
    case "UnaryOp":
      return iv(c, r);
    case "BinaryOp":
      return cv(c, r);
    case "Call":
      return fv(c, r);
    case "Array":
      return c.items.map((o) => Bl(o, r));
  }
}
function iv(c, r) {
  const o = Bl(c.operand, r);
  if (c.op === "!")
    return !uu(o);
  throw new Error(`Unknown unary operator '${c.op}'.`);
}
function cv(c, r) {
  if (c.op === "&&") {
    const p = Bl(c.left, r);
    return uu(p) ? uu(Bl(c.right, r)) : !1;
  }
  if (c.op === "||") {
    const p = Bl(c.left, r);
    return uu(p) ? !0 : uu(Bl(c.right, r));
  }
  const o = Bl(c.left, r), s = Bl(c.right, r);
  switch (c.op) {
    case "==":
      return va(o, s);
    case "!=":
      return !va(o, s);
    case "<":
      return $e(o, s) < 0;
    case ">":
      return $e(o, s) > 0;
    case "<=":
      return $e(o, s) <= 0;
    case ">=":
      return $e(o, s) >= 0;
    default:
      throw new Error(`Unknown binary operator '${c.op}'.`);
  }
}
function fv(c, r) {
  switch (c.name) {
    case "has":
    case "isSet":
      return yy(c, r);
    case "isNotSet":
      return !yy(c, r);
    case "in":
      return sv(c, r);
    default:
      throw new Error(`Unknown function '${c.name}'.`);
  }
}
function yy(c, r) {
  if (c.args.length !== 1)
    throw new Error(`${c.name}() takes one argument.`);
  const o = c.args[0];
  if (!o)
    return !1;
  const s = Bl(o, r);
  return typeof s != "string" ? !1 : s in r && r[s] !== null && r[s] !== void 0;
}
function sv(c, r) {
  if (c.args.length !== 2)
    throw new Error("in() takes two arguments: in(value, [array]).");
  const o = c.args[0], s = c.args[1];
  if (!o || !s)
    return !1;
  const p = Bl(o, r), v = Bl(s, r);
  return Array.isArray(v) ? v.some((D) => va(p, D)) : !1;
}
function rv(c, r) {
  return c in r ? be(r[c]) : null;
}
function ov(c) {
  const r = uv(c);
  return nv(r);
}
function dv(c, r) {
  try {
    const o = typeof c == "string" ? ov(c) : c;
    return uu(Bl(o, r));
  } catch {
    return !1;
  }
}
function yv(c, r) {
  var o;
  if (!c.logic)
    return null;
  for (const s of c.logic)
    if (Ff(s.if, r))
      return ((o = s.then) == null ? void 0 : o.goto) ?? null;
  return null;
}
function Ff(c, r) {
  try {
    return Ph(c) ? hv(c, r) : tv(c) ? mv(c, r) : lv(c) ? dv(c.expression, r) : !1;
  } catch {
    return !1;
  }
}
function mv(c, r) {
  return c.all && c.all.length > 0 ? c.all.every((o) => Ff(o, r)) : c.any && c.any.length > 0 ? c.any.some((o) => Ff(o, r)) : !1;
}
function hv(c, r) {
  const o = c.questionId in r && r[c.questionId] !== null && r[c.questionId] !== void 0;
  if (c.op === "isSet")
    return o;
  if (c.op === "isNotSet")
    return !o;
  if (c.value === void 0)
    return !1;
  const s = o ? be(r[c.questionId]) : null, p = be(c.value);
  return vv(c.op, s, p);
}
function vv(c, r, o) {
  switch (c) {
    case "==":
      return va(r, o);
    case "!=":
      return !va(r, o);
    case ">":
      return $e(r, o) > 0;
    case ">=":
      return $e(r, o) >= 0;
    case "<":
      return $e(r, o) < 0;
    case "<=":
      return $e(r, o) <= 0;
    case "in":
      return my(o, r);
    case "notIn":
      return !my(o, r);
    default:
      return !1;
  }
}
function my(c, r) {
  return Array.isArray(c) ? c.some((o) => va(r, o)) : !1;
}
function If(c, r, o) {
  const s = new Set(c.screens.map((U) => U.id)), p = yv(c, o);
  if (p && p !== r && s.has(p))
    return { kind: "screen", screenId: p };
  const v = c.screens.find((U) => U.id === r);
  if (v != null && v.nextScreen && v.nextScreen !== r && s.has(v.nextScreen))
    return { kind: "screen", screenId: v.nextScreen };
  if (v && (!v.questions || v.questions.length === 0) && !v.nextScreen)
    return { kind: "end" };
  const D = c.screens.findIndex((U) => U.id === r);
  if (D >= 0 && D + 1 < c.screens.length) {
    const U = c.screens[D + 1];
    if (U)
      return { kind: "screen", screenId: U.id };
  }
  return { kind: "end" };
}
function gv(c, r, o, s) {
  const p = new Set(r.screens.map((v) => v.id));
  return c.nextScreen && p.has(c.nextScreen) ? { kind: "screen", screenId: c.nextScreen } : If(r, o, s);
}
const yt = (c, r, o, s) => ({ questionId: c, code: r, message: o, ...s ? { params: s } : {} }), hl = (c) => typeof c == "number" && Number.isFinite(c);
function Jf(c) {
  if (!/^\d{4}-\d{2}-\d{2}$/.test(c))
    return null;
  const [r, o, s] = c.split("-").map((v) => Number.parseInt(v, 10)), p = new Date(Date.UTC(r, o - 1, s));
  return p.getUTCFullYear() !== r || p.getUTCMonth() !== o - 1 || p.getUTCDate() !== s ? null : p.getTime();
}
function wf(c) {
  const r = Date.parse(c);
  return Number.isNaN(r) ? null : r;
}
function py(c, r) {
  const o = c.id, s = [];
  switch (c.type) {
    case "text": {
      if (typeof r != "string") {
        s.push(yt(o, "type", "Text answer must be a JSON string."));
        break;
      }
      const p = c.minLength, v = c.maxLength, D = c.pattern;
      if (hl(p) && r.length < p && s.push(yt(o, "minLength", `Answer length ${r.length} is less than minLength ${p}.`, { n: p, actual: r.length })), hl(v) && r.length > v && s.push(yt(o, "maxLength", `Answer length ${r.length} exceeds maxLength ${v}.`, { n: v, actual: r.length })), typeof D == "string" && D.length > 0)
        try {
          new RegExp(D).test(r) || s.push(yt(o, "pattern", "Answer does not match the required pattern."));
        } catch {
        }
      break;
    }
    case "paragraph": {
      if (typeof r != "string") {
        s.push(yt(o, "type", "Paragraph answer must be a JSON string."));
        break;
      }
      const p = c.minLength, v = c.maxLength;
      hl(p) && r.length < p && s.push(yt(o, "minLength", `Answer length ${r.length} is less than minLength ${p}.`, { n: p, actual: r.length })), hl(v) && r.length > v && s.push(yt(o, "maxLength", `Answer length ${r.length} exceeds maxLength ${v}.`, { n: v, actual: r.length }));
      break;
    }
    case "number": {
      if (!hl(r)) {
        s.push(yt(o, "type", "Number answer must be a JSON number."));
        break;
      }
      const p = c.min, v = c.max;
      hl(p) && r < p && s.push(yt(o, "min", `Answer ${r} is less than min ${p}.`, { n: p })), hl(v) && r > v && s.push(yt(o, "max", `Answer ${r} exceeds max ${v}.`, { n: v }));
      break;
    }
    case "rating": {
      if (!hl(r)) {
        s.push(yt(o, "type", "Rating answer must be a JSON number."));
        break;
      }
      const p = hl(c.max) ? c.max : 0;
      (r < 0 || r > p) && s.push(yt(o, "range", `Rating ${r} is outside 0..${p}.`, { min: 0, max: p })), c.allowHalf !== !0 && r !== Math.floor(r) && s.push(yt(o, "halfNotAllowed", "Rating does not allow half values."));
      break;
    }
    case "nps": {
      if (!hl(r) || !Number.isInteger(r)) {
        s.push(yt(o, "type", "NPS answer must be a JSON number."));
        break;
      }
      const p = hl(c.min) ? c.min : 0, v = hl(c.max) ? c.max : 10;
      (r < p || r > v) && s.push(yt(o, "range", `NPS answer ${r} is outside ${p}..${v}.`, { min: p, max: v }));
      break;
    }
    case "singleChoice":
    case "dropdown":
    case "navigationList": {
      if (typeof r != "string") {
        s.push(yt(o, "type", "Choice answer must be a JSON string (option id)."));
        break;
      }
      (Array.isArray(c.options) ? c.options : []).some((v) => v.id === r) || s.push(yt(o, "invalidOption", `'${r}' is not a valid option id for this question.`, { option: r }));
      break;
    }
    case "multiChoice": {
      if (!Array.isArray(r)) {
        s.push(yt(o, "type", "MultiChoice answer must be a JSON array of option ids."));
        break;
      }
      const p = Array.isArray(c.options) ? c.options : [], v = new Set(p.map((R) => R.id)), D = [];
      let U = !1;
      for (const R of r) {
        if (typeof R != "string") {
          s.push(yt(o, "type", "Each MultiChoice array entry must be a string option id.")), U = !0;
          break;
        }
        D.push(R);
      }
      if (U)
        break;
      for (const R of D)
        v.has(R) || s.push(yt(o, "invalidOption", `'${R}' is not a valid option id for this question.`, { option: R }));
      const O = c.minSelected, g = c.maxSelected;
      hl(O) && D.length < O && s.push(yt(o, "minSelected", `At least ${O} option(s) must be selected.`, { n: O })), hl(g) && D.length > g && s.push(yt(o, "maxSelected", `At most ${g} option(s) may be selected.`, { n: g }));
      break;
    }
    case "date": {
      if (typeof r != "string") {
        s.push(yt(o, "type", "Date answer must be a JSON string in yyyy-MM-dd format."));
        break;
      }
      const p = Jf(r);
      if (p === null) {
        s.push(yt(o, "invalidDate", `Date '${r}' is not yyyy-MM-dd.`));
        break;
      }
      const v = c.minDate, D = c.maxDate;
      if (typeof v == "string") {
        const U = Jf(v);
        U !== null && p < U && s.push(yt(o, "minDate", `Date ${r} is before minDate ${v}.`, { min: v }));
      }
      if (typeof D == "string") {
        const U = Jf(D);
        U !== null && p > U && s.push(yt(o, "maxDate", `Date ${r} is after maxDate ${D}.`, { max: D }));
      }
      break;
    }
    case "dateTime": {
      if (typeof r != "string") {
        s.push(yt(o, "type", "DateTime answer must be a JSON string in ISO 8601 format."));
        break;
      }
      const p = wf(r);
      if (p === null) {
        s.push(yt(o, "invalidDateTime", `DateTime '${r}' is not valid ISO 8601.`));
        break;
      }
      const v = c.minDateTime, D = c.maxDateTime;
      if (typeof v == "string" && v.length > 0) {
        const U = wf(v);
        U !== null && p < U && s.push(yt(o, "minDateTime", `DateTime is before minDateTime ${v}.`, { min: v }));
      }
      if (typeof D == "string" && D.length > 0) {
        const U = wf(D);
        U !== null && p > U && s.push(yt(o, "maxDateTime", `DateTime is after maxDateTime ${D}.`, { max: D }));
      }
      break;
    }
    case "file": {
      (typeof r != "string" || r.length === 0) && s.push(yt(o, "empty", "Answer must be a non-empty file reference string."));
      break;
    }
    case "signature": {
      (typeof r != "string" || r.length === 0) && s.push(yt(o, "empty", "Answer must be a non-empty signature data url string."));
      break;
    }
    case "yesNo": {
      typeof r != "boolean" && s.push(yt(o, "type", "Yes/No answer must be a JSON boolean."));
      break;
    }
  }
  return s;
}
function pv(c, r) {
  const o = [];
  for (const s of c ?? []) {
    const p = s, v = p.id;
    if (typeof v != "string")
      continue;
    const D = r[v];
    D != null && o.push(...py(p, D));
  }
  return o;
}
class eu extends Error {
  constructor(o) {
    super(o.message);
    pe(this, "status");
    pe(this, "code");
    pe(this, "serverMessage");
    pe(this, "validationErrors");
    pe(this, "raw");
    this.name = "SurveyClientError", this.status = o.status, this.code = o.code, this.serverMessage = o.serverMessage, this.validationErrors = o.validationErrors, this.raw = o.raw;
  }
}
class hy {
  constructor(r) {
    pe(this, "baseUrl");
    pe(this, "fetchFn");
    this.baseUrl = r.baseUrl.replace(/\/+$/, "");
    const o = r.fetch ?? globalThis.fetch;
    if (!o)
      throw new Error("SurveyClient: no fetch available. Provide options.fetch or run in an environment with a global fetch.");
    this.fetchFn = o.bind(globalThis);
  }
  async fetchSchema(r) {
    const o = await this.send("GET", `/SurveyInstances/${encodeURIComponent(r)}/schema`);
    return this.readJson(o);
  }
  async getStatus(r) {
    const o = await this.send("GET", `/SurveyInstances/${encodeURIComponent(r)}/status`), s = await this.readJson(o);
    return {
      status: String(s.Status ?? s.status ?? "Pending"),
      schemaVersion: Number(s.SchemaVersion ?? s.schemaVersion ?? 0),
      triggeredAt: s.TriggeredAt ?? s.triggeredAt
    };
  }
  async submitResponse(r, o) {
    await this.send("POST", `/SurveyInstances/${encodeURIComponent(r)}/responses`, o);
  }
  async send(r, o, s) {
    let p;
    try {
      p = await this.fetchFn(`${this.baseUrl}${o}`, {
        method: r,
        headers: s === void 0 ? void 0 : { "Content-Type": "application/json" },
        body: s === void 0 ? void 0 : JSON.stringify(s)
      });
    } catch (v) {
      throw new eu({
        status: 0,
        code: "network",
        message: `Network error calling ${r} ${o}: ${v.message ?? v}`
      });
    }
    if (!p.ok)
      throw await this.toError(p, r, o);
    return p;
  }
  async readJson(r) {
    const o = await r.text();
    if (!o)
      throw new eu({
        status: r.status,
        code: "parse",
        message: `Empty body from ${r.url}`
      });
    try {
      return JSON.parse(o);
    } catch (s) {
      throw new eu({
        status: r.status,
        code: "parse",
        message: `Could not parse JSON from ${r.url}: ${s.message}`,
        raw: o
      });
    }
  }
  async toError(r, o, s) {
    const p = r.status === 404 ? "notFound" : r.status === 410 ? "gone" : r.status === 409 ? "conflict" : r.status === 400 ? "badRequest" : (r.status >= 500, "server"), v = await r.text();
    if (!v)
      return new eu({
        status: r.status,
        code: p,
        message: `${o} ${s} → ${r.status}`
      });
    let D;
    try {
      D = JSON.parse(v);
    } catch {
      return new eu({
        status: r.status,
        code: p,
        message: `${o} ${s} → ${r.status}: ${v.slice(0, 200)}`,
        raw: v
      });
    }
    const U = D.Message ?? D.message, O = D.Errors ?? D.errors, g = Array.isArray(O) ? O.flatMap((R) => {
      const E = R.QuestionId ?? R.questionId, C = R.Message ?? R.message;
      return E && C ? [{ questionId: E, message: C }] : [];
    }) : void 0;
    return new eu({
      status: r.status,
      code: p,
      message: `${o} ${s} → ${r.status}${U ? ": " + U : ""}`,
      serverMessage: U,
      validationErrors: g && g.length > 0 ? g : void 0,
      raw: D
    });
  }
}
function vy(c) {
  const r = c.trim().replace(/^#/, "");
  if (!/^[0-9a-fA-F]{3}$|^[0-9a-fA-F]{6}$|^[0-9a-fA-F]{8}$/.test(r)) return null;
  const o = r.length === 3 ? r.split("").map((s) => s + s).join("") : r.slice(0, 6);
  return [
    parseInt(o.slice(0, 2), 16),
    parseInt(o.slice(2, 4), 16),
    parseInt(o.slice(4, 6), 16)
  ];
}
function kf([c, r, o]) {
  const s = (p) => Math.max(0, Math.min(255, Math.round(p))).toString(16).padStart(2, "0");
  return `#${s(c)}${s(r)}${s(o)}`;
}
function bv(c, r) {
  return [c[0] * r, c[1] * r, c[2] * r];
}
function Sv([c, r, o]) {
  const s = (p) => {
    const v = p / 255;
    return v <= 0.03928 ? v / 12.92 : Math.pow((v + 0.055) / 1.055, 2.4);
  };
  return 0.2126 * s(c) + 0.7152 * s(r) + 0.0722 * s(o);
}
function _v(c) {
  const r = {}, o = c != null && c.primaryColor ? vy(c.primaryColor) : null;
  o && (r["--survey-primary"] = kf(o), r["--survey-primary-hover"] = kf(bv(o, 0.82)), r["--survey-primary-contrast"] = Sv(o) > 0.45 ? "#111111" : "#ffffff");
  const s = c != null && c.secondaryColor ? vy(c.secondaryColor) : null;
  return s && (r["--survey-accent"] = kf(s)), r;
}
const by = P.createContext(null), Ev = by.Provider;
function cl() {
  const c = P.useContext(by);
  if (!c)
    throw new Error(
      "useSurveyContext must be used inside <SurveyRenderer>. Question components rely on survey state from the enclosing provider."
    );
  return c;
}
function tt(c, r, o) {
  if (c == null) return "";
  if (typeof c == "string") return c;
  if (c[r]) return c[r];
  if (o && c[o]) return c[o];
  const s = Object.keys(c);
  return s.length > 0 ? c[s[0]] : "";
}
const Sy = {
  direction: "ltr",
  strings: {
    next: "Next",
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
    yes: "Yes",
    no: "No"
  }
}, Av = {
  direction: "rtl",
  strings: {
    next: "التالي",
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
    yes: "نعم",
    no: "لا"
  }
}, zv = { en: Sy, ar: Av };
function ma(c, r) {
  return r ? c.replace(
    /\{(\w+)\}/g,
    (o, s) => s in r ? String(r[s]) : o
  ) : c;
}
function Tv(c, r, o) {
  const s = { ...zv, ...o ?? {} };
  return s[c] ?? (r ? s[r] : void 0) ?? s.en ?? Sy;
}
const xv = "adp-surveys", qv = 1;
function Nv(c = {}) {
  const r = typeof window < "u", o = r && window.parent !== window, s = c.enabled ?? o, p = c.target ?? (r ? window.parent : null), v = c.targetOrigin ?? "*";
  if (!s || !p)
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
  const D = (U, O) => {
    const g = {
      source: xv,
      version: qv,
      type: U,
      payload: O
    };
    try {
      p.postMessage(g, v);
    } catch {
    }
  };
  return {
    loaded: () => D("survey:loaded", {}),
    screenChanged: (U) => D("survey:screen-changed", { screenId: U }),
    completed: (U) => D("survey:completed", U),
    error: (U) => D("survey:error", { message: U }),
    resize: (U) => D("survey:resize", { height: U })
  };
}
function ls(c) {
  return `adp-surveys:resume:${c}`;
}
function Ov(c, r) {
  try {
    const o = c.getItem(ls(r));
    if (!o) return null;
    const s = JSON.parse(o);
    return !s || typeof s != "object" || !s.answers ? null : s;
  } catch {
    return null;
  }
}
function Mv(c, r, o) {
  try {
    const s = { ...o, savedAt: Date.now() };
    c.setItem(ls(r), JSON.stringify(s));
  } catch {
  }
}
function Dv(c, r) {
  try {
    c.removeItem(ls(r));
  } catch {
  }
}
function Uv({
  question: c,
  registry: r
}) {
  const { ui: o } = cl(), s = c.type, p = s ? r[s] : void 0;
  return p ? /* @__PURE__ */ N.jsx(p, { question: c }) : /* @__PURE__ */ N.jsx("div", { className: "survey-question survey-question--unknown", children: /* @__PURE__ */ N.jsxs("em", { children: [
    o.unsupportedQuestion,
    " ",
    String(s ?? "missing")
  ] }) });
}
function Cv({ question: c }) {
  const { locale: r, schema: o, answers: s, setAnswer: p } = cl(), v = c.id, D = c.title, U = c.help, O = !!c.required, g = s[v] ?? "";
  return /* @__PURE__ */ N.jsxs("div", { className: "survey-question survey-question--text", children: [
    /* @__PURE__ */ N.jsxs("label", { className: "survey-question__label", htmlFor: `q-${v}`, children: [
      tt(D, r, o.defaultLocale),
      O && /* @__PURE__ */ N.jsx("span", { "aria-label": "required", className: "survey-question__required", children: " *" })
    ] }),
    U && /* @__PURE__ */ N.jsx("p", { className: "survey-question__help", children: tt(U, r, o.defaultLocale) }),
    /* @__PURE__ */ N.jsx(
      "input",
      {
        id: `q-${v}`,
        className: "survey-question__input",
        type: "text",
        value: g,
        required: O,
        onChange: (R) => p(v, R.target.value)
      }
    )
  ] });
}
function jv({ question: c }) {
  const { locale: r, schema: o, answers: s, setAnswer: p } = cl(), v = c.id, D = c.title, U = c.help, O = !!c.required, g = Number(c.min ?? 0), R = Number(c.max ?? 10), E = c.lowLabel, C = c.highLabel, Q = s[v], w = [];
  for (let W = g; W <= R; W++) w.push(W);
  return /* @__PURE__ */ N.jsxs("fieldset", { className: "survey-question survey-question--nps", children: [
    /* @__PURE__ */ N.jsxs("legend", { className: "survey-question__label", children: [
      tt(D, r, o.defaultLocale),
      O && /* @__PURE__ */ N.jsx("span", { "aria-label": "required", className: "survey-question__required", children: " *" })
    ] }),
    U && /* @__PURE__ */ N.jsx("p", { className: "survey-question__help", children: tt(U, r, o.defaultLocale) }),
    /* @__PURE__ */ N.jsx("div", { className: "survey-question__nps-scale", role: "radiogroup", children: w.map((W) => {
      const L = Q === W;
      return /* @__PURE__ */ N.jsx(
        "button",
        {
          type: "button",
          role: "radio",
          "aria-checked": L,
          className: "survey-question__nps-step" + (L ? " survey-question__nps-step--selected" : ""),
          onClick: () => p(v, W),
          children: W
        },
        W
      );
    }) }),
    (E || C) && /* @__PURE__ */ N.jsxs("div", { className: "survey-question__nps-labels", children: [
      /* @__PURE__ */ N.jsx("span", { children: E ? tt(E, r, o.defaultLocale) : "" }),
      /* @__PURE__ */ N.jsx("span", { children: C ? tt(C, r, o.defaultLocale) : "" })
    ] })
  ] });
}
function Rv({ question: c }) {
  const { locale: r, schema: o } = cl(), s = c.id, p = c.title, v = c.help, D = c.options ?? [], U = (O, g) => {
    const R = {
      questionId: s,
      option: {
        id: g.id,
        nextScreen: g.nextScreen
      }
    };
    O.currentTarget.dispatchEvent(
      new CustomEvent("survey:navigationListSelect", {
        detail: R,
        bubbles: !0
      })
    );
  };
  return /* @__PURE__ */ N.jsxs("div", { className: "survey-question survey-question--navlist", children: [
    /* @__PURE__ */ N.jsx("div", { className: "survey-question__label", children: tt(p, r, o.defaultLocale) }),
    v && /* @__PURE__ */ N.jsx("p", { className: "survey-question__help", children: tt(v, r, o.defaultLocale) }),
    /* @__PURE__ */ N.jsx("ul", { className: "survey-navlist", role: "radiogroup", "aria-description": "Selecting an option navigates to the next screen.", children: D.map((O) => {
      const g = O.id, R = O.label;
      return /* @__PURE__ */ N.jsx("li", { className: "survey-navlist__row", children: /* @__PURE__ */ N.jsxs(
        "button",
        {
          type: "button",
          className: "survey-navlist__button",
          onClick: (E) => U(E, O),
          children: [
            /* @__PURE__ */ N.jsx("span", { className: "survey-navlist__label", children: tt(R, r, o.defaultLocale) }),
            /* @__PURE__ */ N.jsx("span", { "aria-hidden": "true", className: "survey-navlist__chevron", children: "›" })
          ]
        }
      ) }, g);
    }) })
  ] });
}
function Hv({ question: c }) {
  const { locale: r, schema: o, answers: s, setAnswer: p } = cl(), v = c.id, D = c.title, U = c.help, O = c.placeholder, g = !!c.required, R = c.minLength, E = c.maxLength, C = s[v] ?? "";
  return /* @__PURE__ */ N.jsxs("div", { className: "survey-question survey-question--paragraph", children: [
    /* @__PURE__ */ N.jsxs("label", { className: "survey-question__label", htmlFor: `q-${v}`, children: [
      tt(D, r, o.defaultLocale),
      g && /* @__PURE__ */ N.jsx("span", { "aria-label": "required", className: "survey-question__required", children: " *" })
    ] }),
    U && /* @__PURE__ */ N.jsx("p", { className: "survey-question__help", children: tt(U, r, o.defaultLocale) }),
    /* @__PURE__ */ N.jsx(
      "textarea",
      {
        id: `q-${v}`,
        className: "survey-question__textarea",
        value: C,
        required: g,
        rows: 5,
        minLength: R,
        maxLength: E,
        placeholder: O ? tt(O, r, o.defaultLocale) : void 0,
        onChange: (Q) => p(v, Q.target.value)
      }
    )
  ] });
}
function Bv({ question: c }) {
  const { locale: r, schema: o, answers: s, setAnswer: p } = cl(), v = c.id, D = c.title, U = c.help, O = !!c.required, g = c.min, R = c.max, E = c.step, C = c.unit, Q = s[v], w = Q == null ? "" : String(Q);
  return /* @__PURE__ */ N.jsxs("div", { className: "survey-question survey-question--number", children: [
    /* @__PURE__ */ N.jsxs("label", { className: "survey-question__label", htmlFor: `q-${v}`, children: [
      tt(D, r, o.defaultLocale),
      O && /* @__PURE__ */ N.jsx("span", { "aria-label": "required", className: "survey-question__required", children: " *" })
    ] }),
    U && /* @__PURE__ */ N.jsx("p", { className: "survey-question__help", children: tt(U, r, o.defaultLocale) }),
    /* @__PURE__ */ N.jsxs("div", { className: "survey-question__number-wrap", children: [
      /* @__PURE__ */ N.jsx(
        "input",
        {
          id: `q-${v}`,
          className: "survey-question__input",
          type: "number",
          value: w,
          required: O,
          min: g,
          max: R,
          step: E,
          onChange: (W) => {
            const L = W.target.value;
            p(v, L === "" ? null : Number(L));
          }
        }
      ),
      C && /* @__PURE__ */ N.jsx("span", { className: "survey-question__unit", children: tt(C, r, o.defaultLocale) })
    ] })
  ] });
}
function Yv({ question: c }) {
  const { locale: r, schema: o, answers: s, setAnswer: p } = cl(), v = c.id, D = c.title, U = c.help, O = !!c.required, g = Number(c.max ?? 5), R = s[v], E = [];
  for (let C = 1; C <= g; C++) E.push(C);
  return /* @__PURE__ */ N.jsxs("fieldset", { className: "survey-question survey-question--rating", children: [
    /* @__PURE__ */ N.jsxs("legend", { className: "survey-question__label", children: [
      tt(D, r, o.defaultLocale),
      O && /* @__PURE__ */ N.jsx("span", { "aria-label": "required", className: "survey-question__required", children: " *" })
    ] }),
    U && /* @__PURE__ */ N.jsx("p", { className: "survey-question__help", children: tt(U, r, o.defaultLocale) }),
    /* @__PURE__ */ N.jsx("div", { className: "survey-question__rating-scale", role: "radiogroup", children: E.map((C) => {
      const Q = typeof R == "number" && C <= R;
      return /* @__PURE__ */ N.jsx(
        "button",
        {
          type: "button",
          role: "radio",
          "aria-checked": R === C,
          "aria-label": `${C}`,
          className: "survey-question__rating-star" + (Q ? " survey-question__rating-star--selected" : ""),
          onClick: () => p(v, C),
          children: /* @__PURE__ */ N.jsx("span", { "aria-hidden": "true", children: "★" })
        },
        C
      );
    }) })
  ] });
}
function Lv({ question: c }) {
  const { locale: r, schema: o, answers: s, setAnswer: p } = cl(), v = c.id, D = c.title, U = c.help, O = !!c.required, g = c.options ?? [], R = s[v];
  return /* @__PURE__ */ N.jsxs("fieldset", { className: "survey-question survey-question--single", children: [
    /* @__PURE__ */ N.jsxs("legend", { className: "survey-question__label", children: [
      tt(D, r, o.defaultLocale),
      O && /* @__PURE__ */ N.jsx("span", { "aria-label": "required", className: "survey-question__required", children: " *" })
    ] }),
    U && /* @__PURE__ */ N.jsx("p", { className: "survey-question__help", children: tt(U, r, o.defaultLocale) }),
    /* @__PURE__ */ N.jsx("div", { className: "survey-question__options", children: g.map((E) => /* @__PURE__ */ N.jsxs("label", { className: "survey-question__option", children: [
      /* @__PURE__ */ N.jsx(
        "input",
        {
          type: "radio",
          name: `q-${v}`,
          value: E.id,
          checked: R === E.id,
          onChange: () => p(v, E.id)
        }
      ),
      /* @__PURE__ */ N.jsx("span", { children: tt(E.label, r, o.defaultLocale) })
    ] }, E.id)) })
  ] });
}
function Gv({ question: c }) {
  const { locale: r, schema: o, answers: s, setAnswer: p } = cl(), v = c.id, D = c.title, U = c.help, O = !!c.required, g = c.options ?? [], R = c.maxSelected, E = s[v] ?? [], C = (Q) => {
    if (E.includes(Q)) {
      p(v, E.filter((w) => w !== Q));
      return;
    }
    R !== void 0 && E.length >= R || p(v, [...E, Q]);
  };
  return /* @__PURE__ */ N.jsxs("fieldset", { className: "survey-question survey-question--multi", children: [
    /* @__PURE__ */ N.jsxs("legend", { className: "survey-question__label", children: [
      tt(D, r, o.defaultLocale),
      O && /* @__PURE__ */ N.jsx("span", { "aria-label": "required", className: "survey-question__required", children: " *" })
    ] }),
    U && /* @__PURE__ */ N.jsx("p", { className: "survey-question__help", children: tt(U, r, o.defaultLocale) }),
    /* @__PURE__ */ N.jsx("div", { className: "survey-question__options", children: g.map((Q) => {
      const w = E.includes(Q.id);
      return /* @__PURE__ */ N.jsxs("label", { className: "survey-question__option", children: [
        /* @__PURE__ */ N.jsx(
          "input",
          {
            type: "checkbox",
            checked: w,
            onChange: () => C(Q.id)
          }
        ),
        /* @__PURE__ */ N.jsx("span", { children: tt(Q.label, r, o.defaultLocale) })
      ] }, Q.id);
    }) })
  ] });
}
function Qv({ question: c }) {
  const { locale: r, schema: o, answers: s, setAnswer: p, ui: v } = cl(), D = c.id, U = c.title, O = c.help, g = !!c.required, R = c.options ?? [], E = c.placeholder, C = s[D] ?? "", Q = E ? tt(E, r, o.defaultLocale) : v.selectPlaceholder;
  return /* @__PURE__ */ N.jsxs("div", { className: "survey-question survey-question--dropdown", children: [
    /* @__PURE__ */ N.jsxs("label", { className: "survey-question__label", htmlFor: `q-${D}`, children: [
      tt(U, r, o.defaultLocale),
      g && /* @__PURE__ */ N.jsx("span", { "aria-label": "required", className: "survey-question__required", children: " *" })
    ] }),
    O && /* @__PURE__ */ N.jsx("p", { className: "survey-question__help", children: tt(O, r, o.defaultLocale) }),
    /* @__PURE__ */ N.jsxs(
      "select",
      {
        id: `q-${D}`,
        className: "survey-question__select",
        value: C,
        required: g,
        onChange: (w) => p(D, w.target.value || null),
        children: [
          /* @__PURE__ */ N.jsx("option", { value: "", children: Q }),
          R.map((w) => /* @__PURE__ */ N.jsx("option", { value: w.id, children: tt(w.label, r, o.defaultLocale) }, w.id))
        ]
      }
    )
  ] });
}
function Xv({ question: c }) {
  const { locale: r, schema: o, answers: s, setAnswer: p } = cl(), v = c.id, D = c.title, U = c.help, O = !!c.required, g = c.minDate, R = c.maxDate, E = s[v] ?? "";
  return /* @__PURE__ */ N.jsxs("div", { className: "survey-question survey-question--date", children: [
    /* @__PURE__ */ N.jsxs("label", { className: "survey-question__label", htmlFor: `q-${v}`, children: [
      tt(D, r, o.defaultLocale),
      O && /* @__PURE__ */ N.jsx("span", { "aria-label": "required", className: "survey-question__required", children: " *" })
    ] }),
    U && /* @__PURE__ */ N.jsx("p", { className: "survey-question__help", children: tt(U, r, o.defaultLocale) }),
    /* @__PURE__ */ N.jsx(
      "input",
      {
        id: `q-${v}`,
        className: "survey-question__input",
        type: "date",
        value: E,
        required: O,
        min: g,
        max: R,
        onChange: (C) => p(v, C.target.value || null)
      }
    )
  ] });
}
function Zv({ question: c }) {
  const { locale: r, schema: o, answers: s, setAnswer: p } = cl(), v = c.id, D = c.title, U = c.help, O = !!c.required, g = c.minDateTime, R = c.maxDateTime, E = s[v] ?? "", C = (Q) => {
    if (!Q) return;
    const w = Q.match(/^(\d{4}-\d{2}-\d{2}T\d{2}:\d{2})/);
    return (w == null ? void 0 : w[1]) ?? void 0;
  };
  return /* @__PURE__ */ N.jsxs("div", { className: "survey-question survey-question--datetime", children: [
    /* @__PURE__ */ N.jsxs("label", { className: "survey-question__label", htmlFor: `q-${v}`, children: [
      tt(D, r, o.defaultLocale),
      O && /* @__PURE__ */ N.jsx("span", { "aria-label": "required", className: "survey-question__required", children: " *" })
    ] }),
    U && /* @__PURE__ */ N.jsx("p", { className: "survey-question__help", children: tt(U, r, o.defaultLocale) }),
    /* @__PURE__ */ N.jsx(
      "input",
      {
        id: `q-${v}`,
        className: "survey-question__input",
        type: "datetime-local",
        value: C(E) ?? "",
        required: O,
        min: C(g),
        max: C(R),
        onChange: (Q) => p(v, Q.target.value || null)
      }
    )
  ] });
}
function Vv({ question: c }) {
  const { locale: r, schema: o, answers: s, setAnswer: p } = cl(), v = c.id, D = c.title, U = c.help, O = !!c.required, g = c.acceptedTypes, R = P.useRef(null), E = s[v], C = g && g.length > 0 ? g.join(",") : void 0;
  return /* @__PURE__ */ N.jsxs("div", { className: "survey-question survey-question--file", children: [
    /* @__PURE__ */ N.jsxs("label", { className: "survey-question__label", htmlFor: `q-${v}`, children: [
      tt(D, r, o.defaultLocale),
      O && /* @__PURE__ */ N.jsx("span", { "aria-label": "required", className: "survey-question__required", children: " *" })
    ] }),
    U && /* @__PURE__ */ N.jsx("p", { className: "survey-question__help", children: tt(U, r, o.defaultLocale) }),
    /* @__PURE__ */ N.jsx(
      "input",
      {
        ref: R,
        id: `q-${v}`,
        className: "survey-question__file",
        type: "file",
        required: O,
        accept: C,
        onChange: (Q) => {
          var w;
          const W = (w = Q.target.files) == null ? void 0 : w[0];
          if (!W) {
            p(v, null);
            return;
          }
          p(v, { name: W.name, size: W.size, type: W.type });
        }
      }
    ),
    (E == null ? void 0 : E.name) && /* @__PURE__ */ N.jsxs("p", { className: "survey-question__file-name", children: [
      "Selected: ",
      E.name
    ] })
  ] });
}
const $f = 480, Wf = 160;
function Kv({ question: c }) {
  const { locale: r, schema: o, answers: s, setAnswer: p, ui: v } = cl(), D = c.id, U = c.title, O = c.help, g = !!c.required, R = P.useRef(null), [E, C] = P.useState(!1), [Q, w] = P.useState(!!s[D]), W = () => {
    var J;
    return ((J = R.current) == null ? void 0 : J.getContext("2d")) ?? null;
  }, L = (J) => {
    const ut = J.target.getBoundingClientRect();
    return {
      x: (J.clientX - ut.left) / ut.width * $f,
      y: (J.clientY - ut.top) / ut.height * Wf
    };
  }, V = P.useCallback(() => {
    var J;
    const ut = (J = R.current) == null ? void 0 : J.toDataURL("image/png");
    ut && p(D, ut);
  }, [D, p]), Tt = () => {
    const J = W();
    J && (J.clearRect(0, 0, $f, Wf), w(!1), p(D, null));
  };
  return P.useEffect(() => {
    const J = W();
    J && (J.lineWidth = 2, J.lineCap = "round", J.strokeStyle = "#111");
  }, []), /* @__PURE__ */ N.jsxs("div", { className: "survey-question survey-question--signature", children: [
    /* @__PURE__ */ N.jsxs("div", { className: "survey-question__label", children: [
      tt(U, r, o.defaultLocale),
      g && /* @__PURE__ */ N.jsx("span", { "aria-label": "required", className: "survey-question__required", children: " *" })
    ] }),
    O && /* @__PURE__ */ N.jsx("p", { className: "survey-question__help", children: tt(O, r, o.defaultLocale) }),
    /* @__PURE__ */ N.jsx(
      "canvas",
      {
        ref: R,
        className: "survey-question__signature-canvas",
        width: $f,
        height: Wf,
        role: "img",
        "aria-label": "signature pad",
        onPointerDown: (J) => {
          J.target.setPointerCapture(J.pointerId);
          const ut = W();
          if (!ut) return;
          const { x: Zt, y: nt } = L(J);
          ut.beginPath(), ut.moveTo(Zt, nt), C(!0);
        },
        onPointerMove: (J) => {
          if (!E) return;
          const ut = W();
          if (!ut) return;
          const { x: Zt, y: nt } = L(J);
          ut.lineTo(Zt, nt), ut.stroke(), w(!0);
        },
        onPointerUp: () => {
          C(!1), Q && V();
        }
      }
    ),
    /* @__PURE__ */ N.jsx("div", { className: "survey-question__signature-actions", children: /* @__PURE__ */ N.jsx("button", { type: "button", className: "survey-button survey-button--ghost", onClick: Tt, children: v.clearSignature }) })
  ] });
}
function Jv({ question: c }) {
  const { locale: r, schema: o, answers: s, setAnswer: p, ui: v } = cl(), D = c.id, U = c.title, O = c.help, g = !!c.required, R = c.yesLabel, E = c.noLabel, C = s[D], Q = R ? tt(R, r, o.defaultLocale) : v.yes, w = E ? tt(E, r, o.defaultLocale) : v.no;
  return /* @__PURE__ */ N.jsxs("fieldset", { className: "survey-question survey-question--yesno", children: [
    /* @__PURE__ */ N.jsxs("legend", { className: "survey-question__label", children: [
      tt(U, r, o.defaultLocale),
      g && /* @__PURE__ */ N.jsx("span", { "aria-label": "required", className: "survey-question__required", children: " *" })
    ] }),
    O && /* @__PURE__ */ N.jsx("p", { className: "survey-question__help", children: tt(O, r, o.defaultLocale) }),
    /* @__PURE__ */ N.jsxs("div", { className: "survey-question__yesno", role: "radiogroup", children: [
      /* @__PURE__ */ N.jsx(
        "button",
        {
          type: "button",
          role: "radio",
          "aria-checked": C === !0,
          className: "survey-question__yesno-button" + (C === !0 ? " survey-question__yesno-button--selected" : ""),
          onClick: () => p(D, !0),
          children: Q
        }
      ),
      /* @__PURE__ */ N.jsx(
        "button",
        {
          type: "button",
          role: "radio",
          "aria-checked": C === !1,
          className: "survey-question__yesno-button" + (C === !1 ? " survey-question__yesno-button--selected" : ""),
          onClick: () => p(D, !1),
          children: w
        }
      )
    ] })
  ] });
}
function wv(c, r) {
  switch (c.code) {
    case "minLength":
      return ma(r.minLengthError, c.params);
    case "maxLength":
      return ma(r.maxLengthError, c.params);
    case "pattern":
      return r.patternError;
    case "min":
      return ma(r.minError, c.params);
    case "max":
      return ma(r.maxError, c.params);
    case "range":
      return ma(r.rangeError, c.params);
    case "minSelected":
      return ma(r.minSelectedError, c.params);
    case "maxSelected":
      return ma(r.maxSelectedError, c.params);
    default:
      return r.invalidAnswerError;
  }
}
const kv = {
  text: Cv,
  paragraph: Hv,
  number: Bv,
  rating: Yv,
  nps: jv,
  singleChoice: Lv,
  multiChoice: Gv,
  dropdown: Qv,
  date: Xv,
  dateTime: Zv,
  file: Vv,
  signature: Kv,
  yesNo: Jv,
  navigationList: Rv
};
function $v({
  schema: c,
  onSubmit: r,
  initialAnswers: o,
  locale: s,
  onScreenChange: p,
  onCompleted: v,
  registry: D,
  submissionMeta: U,
  uiLocales: O,
  resumeKey: g,
  storage: R,
  emitHostMessages: E,
  hostMessageOrigin: C,
  hostMessageTarget: Q,
  activeScreenId: w
}) {
  var W, L;
  const V = s ?? c.defaultLocale ?? "en", Tt = D ?? kv, J = P.useMemo(
    () => Tv(V, c.defaultLocale, O),
    [V, c.defaultLocale, O]
  ), ut = R ?? (typeof globalThis < "u" ? globalThis.localStorage : void 0), Zt = P.useMemo(() => {
    var X;
    if (!g || !ut) return null;
    const vt = Ov(ut, g);
    return vt ? vt.currentScreenId === null || c.screens.some((Ut) => Ut.id === vt.currentScreenId) ? vt : { ...vt, currentScreenId: ((X = c.screens[0]) == null ? void 0 : X.id) ?? null } : null;
  }, []), [nt, it] = P.useState(() => ({
    ...o ?? {},
    ...(Zt == null ? void 0 : Zt.answers) ?? {}
  })), [F, al] = P.useState(
    () => {
      var X;
      return (Zt == null ? void 0 : Zt.currentScreenId) ?? ((X = c.screens[0]) == null ? void 0 : X.id) ?? null;
    }
  );
  P.useEffect(() => {
    if (c.screens.length === 0) {
      F !== null && al(null);
      return;
    }
    F !== null && c.screens.some((X) => X.id === F) || al(c.screens[0].id);
  }, [c, F]);
  const [Yl, vl] = P.useState(!1), [Yt, Vl] = P.useState(null), [Ll, ul] = P.useState(/* @__PURE__ */ new Set()), [T, H] = P.useState(/* @__PURE__ */ new Set()), [Z, pt] = P.useState(!1), bt = P.useRef(void 0);
  P.useEffect(() => {
    w !== void 0 && bt.current !== w && (bt.current = w, !(w === null || Z) && c.screens.some((X) => X.id === w) && (ul(/* @__PURE__ */ new Set()), al(w)));
  }, [w, c, Z]);
  const m = P.useRef((/* @__PURE__ */ new Date()).toISOString()), x = P.useRef(null);
  if (x.current === null) {
    const X = {};
    Q !== void 0 && (X.target = Q), C !== void 0 && (X.targetOrigin = C), E !== void 0 && (X.enabled = E), x.current = Nv(X);
  }
  const j = P.useMemo(
    () => F ? c.screens.find((X) => X.id === F) ?? null : null,
    [c, F]
  );
  P.useEffect(() => {
    var X;
    p == null || p(F), (X = x.current) == null || X.screenChanged(F);
  }, [F, p]);
  const B = P.useRef(!1);
  P.useEffect(() => {
    var X;
    B.current || !F || (B.current = !0, (X = x.current) == null || X.loaded());
  }, [F]), P.useEffect(() => {
    !g || !ut || Z || Mv(ut, g, {
      answers: nt,
      currentScreenId: F,
      schemaVersion: c.version
    });
  }, [nt, F, g, ut, Z, c.version]), P.useEffect(() => {
    Z && g && ut && Dv(ut, g);
  }, [Z, g, ut]), P.useEffect(() => {
    var X;
    Yt && ((X = x.current) == null || X.error(Yt));
  }, [Yt]);
  const $ = P.useCallback((X, vt) => {
    it((Ut) => ({ ...Ut, [X]: vt }));
  }, []), lt = P.useCallback(
    (X) => {
      X !== null && (ul(/* @__PURE__ */ new Set()), H(/* @__PURE__ */ new Set()), al(X));
    },
    []
  ), dt = P.useCallback(
    (X) => {
      if (!X.required) return !1;
      const vt = nt[X.id];
      return !!(vt == null || typeof vt == "string" && vt.trim() === "" || Array.isArray(vt) && vt.length === 0);
    },
    [nt]
  ), Dt = P.useCallback(async () => {
    var X;
    vl(!0), Vl(null);
    try {
      await r({
        schemaVersion: c.version ?? 0,
        answers: nt,
        meta: {
          startedAt: (U == null ? void 0 : U.startedAt) ?? m.current,
          completedAt: (U == null ? void 0 : U.completedAt) ?? (/* @__PURE__ */ new Date()).toISOString(),
          ...U ?? {}
        }
      }), pt(!0), v == null || v(F), (X = x.current) == null || X.completed({ screenId: F, answers: nt });
    } catch (vt) {
      Vl(vt.message ?? String(vt));
    } finally {
      vl(!1);
    }
  }, [c.version, nt, U, r, v, F]), Mt = P.useCallback(() => {
    if (!F) return;
    const X = c.screens.find((tl) => tl.id === F), vt = ((X == null ? void 0 : X.questions) ?? []).filter(dt).map((tl) => tl.id);
    if (vt.length > 0) {
      ul(new Set(vt));
      return;
    }
    const Ut = pv(X == null ? void 0 : X.questions, nt);
    if (Ut.length > 0) {
      H(new Set(Ut.map((tl) => tl.questionId)));
      return;
    }
    const Jt = If(c, F, nt);
    Jt.kind === "end" ? Dt() : lt(Jt.screenId);
  }, [c, F, nt, dt, lt, Dt]), Se = P.useRef(null);
  P.useEffect(() => {
    Z || Yl || !F || !j || Se.current === F || !(!j.questions || j.questions.length === 0) || If(c, F, nt).kind === "end" && (Se.current = F, Dt());
  }, [F, j, Z, Yl, c, nt, Dt]);
  const Kl = P.useRef(null);
  P.useEffect(() => {
    const X = Kl.current;
    if (!X || typeof ResizeObserver > "u") return;
    const vt = new ResizeObserver((Ut) => {
      var Jt;
      const tl = Ut[0];
      tl && ((Jt = x.current) == null || Jt.resize(Math.ceil(tl.contentRect.height)));
    });
    return vt.observe(X), () => vt.disconnect();
  }, []), P.useEffect(() => {
    const X = Kl.current;
    if (!X) return;
    const vt = (Ut) => {
      const Jt = Ut.detail;
      if (!Jt || !F) return;
      $(Jt.questionId, Jt.option.id);
      const tl = { ...nt, [Jt.questionId]: Jt.option.id }, ll = gv(
        Jt.option,
        c,
        F,
        tl
      );
      ll.kind === "end" ? Dt() : lt(ll.screenId);
    };
    return X.addEventListener("survey:navigationListSelect", vt), () => X.removeEventListener("survey:navigationListSelect", vt);
  }, [nt, F, c, $, lt, Dt]);
  const cu = P.useMemo(
    () => ({
      schema: c,
      locale: V,
      direction: J.direction,
      ui: J.strings,
      answers: nt,
      setAnswer: $
    }),
    [c, V, J, nt, $]
  ), ga = P.useMemo(() => _v(c.branding), [c.branding]), Jl = (W = c.branding) != null && W.logoUrl ? /* @__PURE__ */ N.jsx("div", { className: "survey-brand", children: /* @__PURE__ */ N.jsx(
    "img",
    {
      className: "survey-brand__logo",
      src: c.branding.logoUrl,
      alt: "",
      onError: (X) => {
        X.currentTarget.parentElement.style.display = "none";
      }
    }
  ) }) : null;
  if (Z)
    return /* @__PURE__ */ N.jsxs(
      "div",
      {
        ref: Kl,
        className: "survey-root survey-root--done",
        dir: J.direction,
        lang: V,
        style: ga,
        children: [
          Jl,
          /* @__PURE__ */ N.jsxs("div", { className: "survey-screen", children: [
            /* @__PURE__ */ N.jsx("h2", { className: "survey-screen__title", children: j != null && j.title ? tt(j.title, V, c.defaultLocale) : J.strings.thankYou }),
            (j == null ? void 0 : j.description) && /* @__PURE__ */ N.jsx("p", { className: "survey-screen__description", children: tt(j.description, V, c.defaultLocale) })
          ] })
        ]
      }
    );
  if (!j)
    return /* @__PURE__ */ N.jsx("div", { ref: Kl, className: "survey-root", dir: J.direction, lang: V, style: ga, children: /* @__PURE__ */ N.jsx("div", { className: "survey-screen", children: /* @__PURE__ */ N.jsx("em", { children: J.strings.noScreens }) }) });
  const Pl = j.questions ?? [], fu = Pl.length > 0 && ((L = Pl[Pl.length - 1]) == null ? void 0 : L.type) === "navigationList", Ti = Pl.length === 0 && !j.nextScreen, nn = !fu && !Ti;
  return /* @__PURE__ */ N.jsx(Ev, { value: cu, children: /* @__PURE__ */ N.jsxs("div", { ref: Kl, className: "survey-root", dir: J.direction, lang: V, style: ga, children: [
    Jl,
    /* @__PURE__ */ N.jsxs("div", { className: "survey-screen", children: [
      j.title && /* @__PURE__ */ N.jsx("h2", { className: "survey-screen__title", children: tt(j.title, V, c.defaultLocale) }),
      j.description && /* @__PURE__ */ N.jsx("p", { className: "survey-screen__description", children: tt(j.description, V, c.defaultLocale) }),
      /* @__PURE__ */ N.jsx("div", { className: "survey-screen__questions", children: Pl.map((X, vt) => {
        const Ut = X.id, Jt = Ut !== void 0 && Ll.has(Ut) && dt(X), tl = !Jt && Ut !== void 0 && T.has(Ut) && nt[Ut] != null ? py(X, nt[Ut])[0] ?? null : null;
        return /* @__PURE__ */ N.jsxs("div", { className: Jt || tl !== null ? "survey-question-slot survey-question-slot--invalid" : "survey-question-slot", children: [
          /* @__PURE__ */ N.jsx(Uv, { question: X, registry: Tt }),
          Jt && /* @__PURE__ */ N.jsx("p", { className: "survey-question__required-error", role: "alert", children: J.strings.requiredError }),
          tl && /* @__PURE__ */ N.jsx("p", { className: "survey-question__required-error", role: "alert", children: wv(tl, J.strings) })
        ] }, Ut ?? vt);
      }) }),
      nn && /* @__PURE__ */ N.jsx("div", { className: "survey-screen__actions", children: /* @__PURE__ */ N.jsx(
        "button",
        {
          type: "button",
          className: "survey-button survey-button--primary",
          disabled: Yl,
          onClick: Mt,
          children: Yl ? J.strings.submitting : J.strings.next
        }
      ) }),
      Yt && /* @__PURE__ */ N.jsxs("p", { className: "survey-screen__error", role: "alert", children: [
        J.strings.couldNotSubmit,
        " ",
        Yt
      ] })
    ] })
  ] }) });
}
const Wv = ".survey-root{--survey-primary: #2563eb;--survey-primary-hover: #1e40af;--survey-primary-contrast: #ffffff;--survey-accent: #f5b60c;font-family:system-ui,-apple-system,Segoe UI,Roboto,sans-serif;color:#111;max-width:640px;margin:0 auto;padding:32px 16px}.survey-brand{display:flex;margin-bottom:20px}.survey-brand__logo{height:28px;width:auto}.survey-screen{display:flex;flex-direction:column;gap:24px}.survey-screen__title{font-size:1.5rem;font-weight:600;margin:0}.survey-screen__description{color:#555;margin:0}.survey-screen__questions{display:flex;flex-direction:column;gap:24px}.survey-screen__actions{display:flex;justify-content:flex-end}.survey-screen__error{color:#b42318;background:#fef3f2;border:1px solid #fecdca;padding:12px 14px;border-radius:8px;margin:0}.survey-question-slot--invalid{border-inline-start:3px solid #b42318;padding-inline-start:10px}.survey-question__required-error{color:#b42318;font-size:.9rem;margin:4px 0 0}.survey-question{display:flex;flex-direction:column;gap:8px}.survey-question__label{font-weight:600;display:block}.survey-question__required{color:#b42318}.survey-question__help{margin:0;color:#666;font-size:.9rem}.survey-question__input{padding:10px 12px;border:1px solid #d0d5dd;border-radius:8px;font:inherit}.survey-question__input:focus-visible{outline:2px solid var(--survey-primary);outline-offset:1px;border-color:var(--survey-primary)}.survey-question--nps{border:none;padding:0;margin:0;display:flex;flex-direction:column;gap:8px}.survey-question__nps-scale{display:flex;gap:6px;flex-wrap:wrap}.survey-question__nps-step{min-width:40px;min-height:40px;padding:8px;border:1px solid #d0d5dd;border-radius:8px;background:#fff;font-weight:500;cursor:pointer}.survey-question__nps-step:hover{background:#f5f7fa}.survey-question__nps-step--selected{background:var(--survey-primary);border-color:var(--survey-primary);color:var(--survey-primary-contrast)}.survey-question__nps-labels{display:flex;justify-content:space-between;color:#555;font-size:.85rem}.survey-question--navlist{gap:12px}.survey-navlist{list-style:none;padding:0;margin:0;display:flex;flex-direction:column;gap:8px}.survey-navlist__row{margin:0}.survey-navlist__button{width:100%;display:flex;align-items:center;justify-content:space-between;padding:14px 16px;border:1px solid #d0d5dd;border-radius:10px;background:#fff;cursor:pointer;font:inherit;text-align:start}.survey-navlist__button:hover{background:#f5f7fa;border-color:var(--survey-primary)}.survey-navlist__chevron{font-size:1.5rem;color:#667085}.survey-root[dir=rtl] .survey-navlist__chevron{transform:scaleX(-1)}.survey-navlist__label{font-weight:500}.survey-question__textarea{padding:10px 12px;border:1px solid #d0d5dd;border-radius:8px;font:inherit;resize:vertical;min-height:96px}.survey-question__textarea:focus-visible{outline:2px solid var(--survey-primary);outline-offset:1px;border-color:var(--survey-primary)}.survey-question__number-wrap{display:flex;align-items:center;gap:8px}.survey-question__number-wrap .survey-question__input{flex:1}.survey-question__unit{color:#555;font-size:.9rem}.survey-question__rating-scale{display:flex;gap:4px}.survey-question__rating-star{background:transparent;border:none;cursor:pointer;font-size:1.8rem;line-height:1;color:#d0d5dd;padding:4px}.survey-question__rating-star:hover,.survey-question__rating-star--selected{color:var(--survey-accent)}.survey-question__options{display:flex;flex-direction:column;gap:8px}.survey-question__option{display:flex;align-items:center;gap:8px;padding:8px 12px;border:1px solid #d0d5dd;border-radius:8px;cursor:pointer}.survey-question__option:hover{background:#f5f7fa;border-color:var(--survey-primary)}.survey-question__option input{margin:0}.survey-question__select{padding:10px 12px;border:1px solid #d0d5dd;border-radius:8px;font:inherit;background:#fff}.survey-question__yesno{display:flex;gap:12px}.survey-question__yesno-button{flex:1;padding:14px 16px;border:1px solid #d0d5dd;border-radius:10px;background:#fff;font:inherit;font-weight:500;cursor:pointer}.survey-question__yesno-button:hover{background:#f5f7fa;border-color:var(--survey-primary)}.survey-question__yesno-button--selected{background:var(--survey-primary);border-color:var(--survey-primary);color:var(--survey-primary-contrast)}.survey-question__file{font:inherit}.survey-question__file-name{color:#555;font-size:.9rem;margin:0}.survey-question__signature-canvas{width:100%;max-width:480px;height:auto;aspect-ratio:3 / 1;border:1px dashed #d0d5dd;border-radius:8px;background:#fff;touch-action:none}.survey-question__signature-actions{display:flex;justify-content:flex-start;gap:8px}.survey-button{padding:10px 20px;border-radius:8px;border:1px solid transparent;cursor:pointer;font:inherit;font-weight:600}.survey-button--primary{background:var(--survey-primary);color:var(--survey-primary-contrast)}.survey-button--primary:hover{background:var(--survey-primary-hover)}.survey-button--ghost{background:#fff;color:#555;border-color:#d0d5dd}.survey-button--ghost:hover{background:#f5f7fa}.survey-button:disabled{opacity:.5;cursor:not-allowed}";
var we, nu, Zl, ke, iu, ha, an, Kt, Pf, Je, un, au;
class Fv extends HTMLElement {
  constructor() {
    super();
    Fl(this, Kt);
    /** Schema-mode setter. Assigning this swaps the element into schema mode and
     *  re-renders with the new schema immediately. */
    Fl(this, we, null);
    /** Schema-mode submit handler. In API mode the element manages this itself. */
    Fl(this, nu, null);
    Fl(this, Zl, null);
    Fl(this, ke, null);
    Fl(this, iu, null);
    Fl(this, ha, null);
    /** Builder-preview jump target. Assigning a screen id makes the renderer
     *  jump to that screen (answers preserved); the user can navigate freely
     *  afterwards. Mirrors the `active-screen-id` attribute; the property wins
     *  when both are set. */
    Fl(this, an, null);
    Fl(this, un, !1);
    this.attachShadow({ mode: "open" });
  }
  static get observedAttributes() {
    return ["instance-id", "api-base", "locale", "mode", "active-screen-id"];
  }
  // ─── Lifecycle ───────────────────────────────────────────────────────────
  connectedCallback() {
    if (this.shadowRoot) {
      if (!this.shadowRoot.querySelector("style[data-shift-survey]")) {
        const o = document.createElement("style");
        o.setAttribute("data-shift-survey", ""), o.textContent = Wv, this.shadowRoot.appendChild(o);
      }
      Rt(this, ke) || (Hl(this, ke, document.createElement("div")), Rt(this, ke).className = "shift-survey-mount", this.shadowRoot.appendChild(Rt(this, ke))), Rt(this, Zl) || Hl(this, Zl, Wh.createRoot(Rt(this, ke))), il(this, Kt, Je).call(this), il(this, Kt, Pf).call(this);
    }
  }
  disconnectedCallback() {
    queueMicrotask(() => {
      var o;
      if (!(this.isConnected || typeof window > "u")) {
        try {
          (o = Rt(this, Zl)) == null || o.unmount();
        } catch {
        }
        Hl(this, Zl, null);
      }
    });
  }
  attributeChangedCallback(o, s, p) {
    s !== p && ((o === "instance-id" || o === "api-base") && (Hl(this, iu, null), Hl(this, ha, null), il(this, Kt, Pf).call(this)), il(this, Kt, Je).call(this));
  }
  // ─── Properties ──────────────────────────────────────────────────────────
  get schema() {
    return Rt(this, we);
  }
  set schema(o) {
    Hl(this, we, o), il(this, Kt, Je).call(this);
  }
  get onSubmit() {
    return Rt(this, nu);
  }
  set onSubmit(o) {
    Hl(this, nu, o), il(this, Kt, Je).call(this);
  }
  get activeScreenId() {
    return Rt(this, an) ?? this.getAttribute("active-screen-id");
  }
  set activeScreenId(o) {
    Hl(this, an, o), il(this, Kt, Je).call(this);
  }
}
we = new WeakMap(), nu = new WeakMap(), Zl = new WeakMap(), ke = new WeakMap(), iu = new WeakMap(), ha = new WeakMap(), an = new WeakMap(), Kt = new WeakSet(), // ─── Internals ───────────────────────────────────────────────────────────
Pf = function() {
  if (Rt(this, we)) return;
  const o = this.getAttribute("instance-id");
  if (!o) return;
  const s = this.getAttribute("api-base");
  if (!s) return;
  new hy({ baseUrl: s }).fetchSchema(o).then((v) => {
    Hl(this, iu, v), il(this, Kt, Je).call(this);
  }).catch((v) => {
    Hl(this, ha, v), il(this, Kt, au).call(this, "survey:error", { message: v.message }), il(this, Kt, Je).call(this);
  });
}, Je = function() {
  if (!Rt(this, Zl)) return;
  const o = this.getAttribute("api-base"), s = this.getAttribute("instance-id"), p = this.getAttribute("locale") ?? void 0, v = this.getAttribute("mode") === "agent", D = Rt(this, we) ?? Rt(this, iu);
  if (Rt(this, ha) && !D) {
    Rt(this, Zl).render(
      P.createElement(
        "div",
        { className: "shift-survey-error", role: "alert" },
        Rt(this, ha).message
      )
    );
    return;
  }
  if (!D) {
    Rt(this, Zl).render(
      P.createElement("div", { className: "shift-survey-loading" }, "Loading…")
    );
    return;
  }
  const U = Rt(this, we) ? Rt(this, nu) ?? ((g) => {
    il(this, Kt, au).call(this, "survey:completed", { ...g });
  }) : async (g) => {
    if (!o || !s)
      throw new Error("shift-survey: API mode requires both instance-id and api-base attributes.");
    await new hy({ baseUrl: o }).submitResponse(s, g);
  }, O = this.activeScreenId;
  Rt(this, Zl).render(
    P.createElement($v, {
      schema: D,
      onSubmit: U,
      ...p ? { locale: p } : {},
      ...O ? { activeScreenId: O } : {},
      // Let the element be the resume key in API mode so two surveys on the
      // same host page don't clobber each other.
      ...s ? { resumeKey: s } : {},
      ...v ? { submissionMeta: { mode: "agent" } } : {},
      // CustomEvents are the web-component's channel; postMessage stays opt-in
      // via iframe auto-detect on the enclosing page (unchanged).
      onScreenChange: (g) => il(this, Kt, au).call(this, "survey:screen-changed", { screenId: g }),
      onCompleted: (g) => il(this, Kt, au).call(this, "survey:completed", { screenId: g })
    })
  ), Rt(this, un) || (Hl(this, un, !0), il(this, Kt, au).call(this, "survey:loaded", {}));
}, un = new WeakMap(), au = function(o, s) {
  this.dispatchEvent(
    new CustomEvent(o, { detail: s, bubbles: !0, composed: !0 })
  );
};
function Iv(c = "shift-survey") {
  typeof window > "u" || typeof customElements > "u" || customElements.get(c) || customElements.define(c, Fv);
}
Iv();
export {
  Fv as ShiftSurveyElement,
  Iv as registerShiftSurvey
};
//# sourceMappingURL=index.js.map
