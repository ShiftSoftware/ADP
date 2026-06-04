var Uh = Object.defineProperty;
var wd = (c) => {
  throw TypeError(c);
};
var Ch = (c, o, r) => o in c ? Uh(c, o, { enumerable: !0, configurable: !0, writable: !0, value: r }) : c[o] = r;
var oe = (c, o, r) => Ch(c, typeof o != "symbol" ? o + "" : o, r), Uf = (c, o, r) => o.has(c) || wd("Cannot " + r);
var Rl = (c, o, r) => (Uf(c, o, "read from private field"), r ? r.call(c) : o.get(c)), de = (c, o, r) => o.has(c) ? wd("Cannot add the same private member more than once") : o instanceof WeakSet ? o.add(c) : o.set(c, r), Lt = (c, o, r, s) => (Uf(c, o, "write to private field"), s ? s.call(c, r) : o.set(c, r), r), rt = (c, o, r) => (Uf(c, o, "access private method"), r);
var Cf = { exports: {} }, P = {};
/**
 * @license React
 * react.production.js
 *
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 */
var kd;
function jh() {
  if (kd) return P;
  kd = 1;
  var c = Symbol.for("react.transitional.element"), o = Symbol.for("react.portal"), r = Symbol.for("react.fragment"), s = Symbol.for("react.strict_mode"), T = Symbol.for("react.profiler"), E = Symbol.for("react.consumer"), C = Symbol.for("react.context"), U = Symbol.for("react.forward_ref"), N = Symbol.for("react.suspense"), b = Symbol.for("react.memo"), H = Symbol.for("react.lazy"), p = Symbol.for("react.activity"), D = Symbol.iterator;
  function X(v) {
    return v === null || typeof v != "object" ? null : (v = D && v[D] || v["@@iterator"], typeof v == "function" ? v : null);
  }
  var k = {
    isMounted: function() {
      return !1;
    },
    enqueueForceUpdate: function() {
    },
    enqueueReplaceState: function() {
    },
    enqueueSetState: function() {
    }
  }, K = Object.assign, G = {};
  function Z(v, O, R) {
    this.props = v, this.context = O, this.refs = G, this.updater = R || k;
  }
  Z.prototype.isReactComponent = {}, Z.prototype.setState = function(v, O) {
    if (typeof v != "object" && typeof v != "function" && v != null)
      throw Error(
        "takes an object of state variables to update or a function which returns an object of state variables."
      );
    this.updater.enqueueSetState(this, v, O, "setState");
  }, Z.prototype.forceUpdate = function(v) {
    this.updater.enqueueForceUpdate(this, v, "forceUpdate");
  };
  function ol() {
  }
  ol.prototype = Z.prototype;
  function W(v, O, R) {
    this.props = v, this.context = O, this.refs = G, this.updater = R || k;
  }
  var I = W.prototype = new ol();
  I.constructor = W, K(I, Z.prototype), I.isPureReactComponent = !0;
  var Il = Array.isArray;
  function $() {
  }
  var nl = { H: null, A: null, T: null, S: null }, Ql = Object.prototype.hasOwnProperty;
  function ot(v, O, R) {
    var L = R.ref;
    return {
      $$typeof: c,
      type: v,
      key: O,
      ref: L !== void 0 ? L : null,
      props: R
    };
  }
  function qt(v, O) {
    return ot(v.type, O, v.props);
  }
  function dt(v) {
    return typeof v == "object" && v !== null && v.$$typeof === c;
  }
  function Pl(v) {
    var O = { "=": "=0", ":": "=2" };
    return "$" + v.replace(/[=:]/g, function(R) {
      return O[R];
    });
  }
  var Qt = /\/+/g;
  function Jl(v, O) {
    return typeof v == "object" && v !== null && v.key != null ? Pl("" + v.key) : O.toString(36);
  }
  function yt(v) {
    switch (v.status) {
      case "fulfilled":
        return v.value;
      case "rejected":
        throw v.reason;
      default:
        switch (typeof v.status == "string" ? v.then($, $) : (v.status = "pending", v.then(
          function(O) {
            v.status === "pending" && (v.status = "fulfilled", v.value = O);
          },
          function(O) {
            v.status === "pending" && (v.status = "rejected", v.reason = O);
          }
        )), v.status) {
          case "fulfilled":
            return v.value;
          case "rejected":
            throw v.reason;
        }
    }
    throw v;
  }
  function q(v, O, R, L, F) {
    var ll = typeof v;
    (ll === "undefined" || ll === "boolean") && (v = null);
    var yl = !1;
    if (v === null) yl = !0;
    else
      switch (ll) {
        case "bigint":
        case "string":
        case "number":
          yl = !0;
          break;
        case "object":
          switch (v.$$typeof) {
            case c:
            case o:
              yl = !0;
              break;
            case H:
              return yl = v._init, q(
                yl(v._payload),
                O,
                R,
                L,
                F
              );
          }
      }
    if (yl)
      return F = F(v), yl = L === "" ? "." + Jl(v, 0) : L, Il(F) ? (R = "", yl != null && (R = yl.replace(Qt, "$&/") + "/"), q(F, O, R, "", function(Ze) {
        return Ze;
      })) : F != null && (dt(F) && (F = qt(
        F,
        R + (F.key == null || v && v.key === F.key ? "" : ("" + F.key).replace(
          Qt,
          "$&/"
        ) + "/") + yl
      )), O.push(F)), 1;
    yl = 0;
    var Ul = L === "" ? "." : L + ":";
    if (Il(v))
      for (var Nl = 0; Nl < v.length; Nl++)
        L = v[Nl], ll = Ul + Jl(L, Nl), yl += q(
          L,
          O,
          R,
          ll,
          F
        );
    else if (Nl = X(v), typeof Nl == "function")
      for (v = Nl.call(v), Nl = 0; !(L = v.next()).done; )
        L = L.value, ll = Ul + Jl(L, Nl++), yl += q(
          L,
          O,
          R,
          ll,
          F
        );
    else if (ll === "object") {
      if (typeof v.then == "function")
        return q(
          yt(v),
          O,
          R,
          L,
          F
        );
      throw O = String(v), Error(
        "Objects are not valid as a React child (found: " + (O === "[object Object]" ? "object with keys {" + Object.keys(v).join(", ") + "}" : O) + "). If you meant to render a collection of children, use an array instead."
      );
    }
    return yl;
  }
  function j(v, O, R) {
    if (v == null) return v;
    var L = [], F = 0;
    return q(v, L, "", "", function(ll) {
      return O.call(R, ll, F++);
    }), L;
  }
  function B(v) {
    if (v._status === -1) {
      var O = v._result;
      O = O(), O.then(
        function(R) {
          (v._status === 0 || v._status === -1) && (v._status = 1, v._result = R);
        },
        function(R) {
          (v._status === 0 || v._status === -1) && (v._status = 2, v._result = R);
        }
      ), v._status === -1 && (v._status = 0, v._result = O);
    }
    if (v._status === 1) return v._result.default;
    throw v._result;
  }
  var ml = typeof reportError == "function" ? reportError : function(v) {
    if (typeof window == "object" && typeof window.ErrorEvent == "function") {
      var O = new window.ErrorEvent("error", {
        bubbles: !0,
        cancelable: !0,
        message: typeof v == "object" && v !== null && typeof v.message == "string" ? String(v.message) : String(v),
        error: v
      });
      if (!window.dispatchEvent(O)) return;
    } else if (typeof process == "object" && typeof process.emit == "function") {
      process.emit("uncaughtException", v);
      return;
    }
    console.error(v);
  }, dl = {
    map: j,
    forEach: function(v, O, R) {
      j(
        v,
        function() {
          O.apply(this, arguments);
        },
        R
      );
    },
    count: function(v) {
      var O = 0;
      return j(v, function() {
        O++;
      }), O;
    },
    toArray: function(v) {
      return j(v, function(O) {
        return O;
      }) || [];
    },
    only: function(v) {
      if (!dt(v))
        throw Error(
          "React.Children.only expected to receive a single React element child."
        );
      return v;
    }
  };
  return P.Activity = p, P.Children = dl, P.Component = Z, P.Fragment = r, P.Profiler = T, P.PureComponent = W, P.StrictMode = s, P.Suspense = N, P.__CLIENT_INTERNALS_DO_NOT_USE_OR_WARN_USERS_THEY_CANNOT_UPGRADE = nl, P.__COMPILER_RUNTIME = {
    __proto__: null,
    c: function(v) {
      return nl.H.useMemoCache(v);
    }
  }, P.cache = function(v) {
    return function() {
      return v.apply(null, arguments);
    };
  }, P.cacheSignal = function() {
    return null;
  }, P.cloneElement = function(v, O, R) {
    if (v == null)
      throw Error(
        "The argument must be a React element, but you passed " + v + "."
      );
    var L = K({}, v.props), F = v.key;
    if (O != null)
      for (ll in O.key !== void 0 && (F = "" + O.key), O)
        !Ql.call(O, ll) || ll === "key" || ll === "__self" || ll === "__source" || ll === "ref" && O.ref === void 0 || (L[ll] = O[ll]);
    var ll = arguments.length - 2;
    if (ll === 1) L.children = R;
    else if (1 < ll) {
      for (var yl = Array(ll), Ul = 0; Ul < ll; Ul++)
        yl[Ul] = arguments[Ul + 2];
      L.children = yl;
    }
    return ot(v.type, F, L);
  }, P.createContext = function(v) {
    return v = {
      $$typeof: C,
      _currentValue: v,
      _currentValue2: v,
      _threadCount: 0,
      Provider: null,
      Consumer: null
    }, v.Provider = v, v.Consumer = {
      $$typeof: E,
      _context: v
    }, v;
  }, P.createElement = function(v, O, R) {
    var L, F = {}, ll = null;
    if (O != null)
      for (L in O.key !== void 0 && (ll = "" + O.key), O)
        Ql.call(O, L) && L !== "key" && L !== "__self" && L !== "__source" && (F[L] = O[L]);
    var yl = arguments.length - 2;
    if (yl === 1) F.children = R;
    else if (1 < yl) {
      for (var Ul = Array(yl), Nl = 0; Nl < yl; Nl++)
        Ul[Nl] = arguments[Nl + 2];
      F.children = Ul;
    }
    if (v && v.defaultProps)
      for (L in yl = v.defaultProps, yl)
        F[L] === void 0 && (F[L] = yl[L]);
    return ot(v, ll, F);
  }, P.createRef = function() {
    return { current: null };
  }, P.forwardRef = function(v) {
    return { $$typeof: U, render: v };
  }, P.isValidElement = dt, P.lazy = function(v) {
    return {
      $$typeof: H,
      _payload: { _status: -1, _result: v },
      _init: B
    };
  }, P.memo = function(v, O) {
    return {
      $$typeof: b,
      type: v,
      compare: O === void 0 ? null : O
    };
  }, P.startTransition = function(v) {
    var O = nl.T, R = {};
    nl.T = R;
    try {
      var L = v(), F = nl.S;
      F !== null && F(R, L), typeof L == "object" && L !== null && typeof L.then == "function" && L.then($, ml);
    } catch (ll) {
      ml(ll);
    } finally {
      O !== null && R.types !== null && (O.types = R.types), nl.T = O;
    }
  }, P.unstable_useCacheRefresh = function() {
    return nl.H.useCacheRefresh();
  }, P.use = function(v) {
    return nl.H.use(v);
  }, P.useActionState = function(v, O, R) {
    return nl.H.useActionState(v, O, R);
  }, P.useCallback = function(v, O) {
    return nl.H.useCallback(v, O);
  }, P.useContext = function(v) {
    return nl.H.useContext(v);
  }, P.useDebugValue = function() {
  }, P.useDeferredValue = function(v, O) {
    return nl.H.useDeferredValue(v, O);
  }, P.useEffect = function(v, O) {
    return nl.H.useEffect(v, O);
  }, P.useEffectEvent = function(v) {
    return nl.H.useEffectEvent(v);
  }, P.useId = function() {
    return nl.H.useId();
  }, P.useImperativeHandle = function(v, O, R) {
    return nl.H.useImperativeHandle(v, O, R);
  }, P.useInsertionEffect = function(v, O) {
    return nl.H.useInsertionEffect(v, O);
  }, P.useLayoutEffect = function(v, O) {
    return nl.H.useLayoutEffect(v, O);
  }, P.useMemo = function(v, O) {
    return nl.H.useMemo(v, O);
  }, P.useOptimistic = function(v, O) {
    return nl.H.useOptimistic(v, O);
  }, P.useReducer = function(v, O, R) {
    return nl.H.useReducer(v, O, R);
  }, P.useRef = function(v) {
    return nl.H.useRef(v);
  }, P.useState = function(v) {
    return nl.H.useState(v);
  }, P.useSyncExternalStore = function(v, O, R) {
    return nl.H.useSyncExternalStore(
      v,
      O,
      R
    );
  }, P.useTransition = function() {
    return nl.H.useTransition();
  }, P.version = "19.2.5", P;
}
var $d;
function Vf() {
  return $d || ($d = 1, Cf.exports = jh()), Cf.exports;
}
var al = Vf(), jf = { exports: {} }, wa = {}, Rf = { exports: {} }, Hf = {};
/**
 * @license React
 * scheduler.production.js
 *
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 */
var Wd;
function Rh() {
  return Wd || (Wd = 1, (function(c) {
    function o(q, j) {
      var B = q.length;
      q.push(j);
      l: for (; 0 < B; ) {
        var ml = B - 1 >>> 1, dl = q[ml];
        if (0 < T(dl, j))
          q[ml] = j, q[B] = dl, B = ml;
        else break l;
      }
    }
    function r(q) {
      return q.length === 0 ? null : q[0];
    }
    function s(q) {
      if (q.length === 0) return null;
      var j = q[0], B = q.pop();
      if (B !== j) {
        q[0] = B;
        l: for (var ml = 0, dl = q.length, v = dl >>> 1; ml < v; ) {
          var O = 2 * (ml + 1) - 1, R = q[O], L = O + 1, F = q[L];
          if (0 > T(R, B))
            L < dl && 0 > T(F, R) ? (q[ml] = F, q[L] = B, ml = L) : (q[ml] = R, q[O] = B, ml = O);
          else if (L < dl && 0 > T(F, B))
            q[ml] = F, q[L] = B, ml = L;
          else break l;
        }
      }
      return j;
    }
    function T(q, j) {
      var B = q.sortIndex - j.sortIndex;
      return B !== 0 ? B : q.id - j.id;
    }
    if (c.unstable_now = void 0, typeof performance == "object" && typeof performance.now == "function") {
      var E = performance;
      c.unstable_now = function() {
        return E.now();
      };
    } else {
      var C = Date, U = C.now();
      c.unstable_now = function() {
        return C.now() - U;
      };
    }
    var N = [], b = [], H = 1, p = null, D = 3, X = !1, k = !1, K = !1, G = !1, Z = typeof setTimeout == "function" ? setTimeout : null, ol = typeof clearTimeout == "function" ? clearTimeout : null, W = typeof setImmediate < "u" ? setImmediate : null;
    function I(q) {
      for (var j = r(b); j !== null; ) {
        if (j.callback === null) s(b);
        else if (j.startTime <= q)
          s(b), j.sortIndex = j.expirationTime, o(N, j);
        else break;
        j = r(b);
      }
    }
    function Il(q) {
      if (K = !1, I(q), !k)
        if (r(N) !== null)
          k = !0, $ || ($ = !0, Pl());
        else {
          var j = r(b);
          j !== null && yt(Il, j.startTime - q);
        }
    }
    var $ = !1, nl = -1, Ql = 5, ot = -1;
    function qt() {
      return G ? !0 : !(c.unstable_now() - ot < Ql);
    }
    function dt() {
      if (G = !1, $) {
        var q = c.unstable_now();
        ot = q;
        var j = !0;
        try {
          l: {
            k = !1, K && (K = !1, ol(nl), nl = -1), X = !0;
            var B = D;
            try {
              t: {
                for (I(q), p = r(N); p !== null && !(p.expirationTime > q && qt()); ) {
                  var ml = p.callback;
                  if (typeof ml == "function") {
                    p.callback = null, D = p.priorityLevel;
                    var dl = ml(
                      p.expirationTime <= q
                    );
                    if (q = c.unstable_now(), typeof dl == "function") {
                      p.callback = dl, I(q), j = !0;
                      break t;
                    }
                    p === r(N) && s(N), I(q);
                  } else s(N);
                  p = r(N);
                }
                if (p !== null) j = !0;
                else {
                  var v = r(b);
                  v !== null && yt(
                    Il,
                    v.startTime - q
                  ), j = !1;
                }
              }
              break l;
            } finally {
              p = null, D = B, X = !1;
            }
            j = void 0;
          }
        } finally {
          j ? Pl() : $ = !1;
        }
      }
    }
    var Pl;
    if (typeof W == "function")
      Pl = function() {
        W(dt);
      };
    else if (typeof MessageChannel < "u") {
      var Qt = new MessageChannel(), Jl = Qt.port2;
      Qt.port1.onmessage = dt, Pl = function() {
        Jl.postMessage(null);
      };
    } else
      Pl = function() {
        Z(dt, 0);
      };
    function yt(q, j) {
      nl = Z(function() {
        q(c.unstable_now());
      }, j);
    }
    c.unstable_IdlePriority = 5, c.unstable_ImmediatePriority = 1, c.unstable_LowPriority = 4, c.unstable_NormalPriority = 3, c.unstable_Profiling = null, c.unstable_UserBlockingPriority = 2, c.unstable_cancelCallback = function(q) {
      q.callback = null;
    }, c.unstable_forceFrameRate = function(q) {
      0 > q || 125 < q ? console.error(
        "forceFrameRate takes a positive int between 0 and 125, forcing frame rates higher than 125 fps is not supported"
      ) : Ql = 0 < q ? Math.floor(1e3 / q) : 5;
    }, c.unstable_getCurrentPriorityLevel = function() {
      return D;
    }, c.unstable_next = function(q) {
      switch (D) {
        case 1:
        case 2:
        case 3:
          var j = 3;
          break;
        default:
          j = D;
      }
      var B = D;
      D = j;
      try {
        return q();
      } finally {
        D = B;
      }
    }, c.unstable_requestPaint = function() {
      G = !0;
    }, c.unstable_runWithPriority = function(q, j) {
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
      var B = D;
      D = q;
      try {
        return j();
      } finally {
        D = B;
      }
    }, c.unstable_scheduleCallback = function(q, j, B) {
      var ml = c.unstable_now();
      switch (typeof B == "object" && B !== null ? (B = B.delay, B = typeof B == "number" && 0 < B ? ml + B : ml) : B = ml, q) {
        case 1:
          var dl = -1;
          break;
        case 2:
          dl = 250;
          break;
        case 5:
          dl = 1073741823;
          break;
        case 4:
          dl = 1e4;
          break;
        default:
          dl = 5e3;
      }
      return dl = B + dl, q = {
        id: H++,
        callback: j,
        priorityLevel: q,
        startTime: B,
        expirationTime: dl,
        sortIndex: -1
      }, B > ml ? (q.sortIndex = B, o(b, q), r(N) === null && q === r(b) && (K ? (ol(nl), nl = -1) : K = !0, yt(Il, B - ml))) : (q.sortIndex = dl, o(N, q), k || X || (k = !0, $ || ($ = !0, Pl()))), q;
    }, c.unstable_shouldYield = qt, c.unstable_wrapCallback = function(q) {
      var j = D;
      return function() {
        var B = D;
        D = j;
        try {
          return q.apply(this, arguments);
        } finally {
          D = B;
        }
      };
    };
  })(Hf)), Hf;
}
var Fd;
function Hh() {
  return Fd || (Fd = 1, Rf.exports = Rh()), Rf.exports;
}
var Bf = { exports: {} }, lt = {};
/**
 * @license React
 * react-dom.production.js
 *
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 */
var Id;
function Bh() {
  if (Id) return lt;
  Id = 1;
  var c = Vf();
  function o(N) {
    var b = "https://react.dev/errors/" + N;
    if (1 < arguments.length) {
      b += "?args[]=" + encodeURIComponent(arguments[1]);
      for (var H = 2; H < arguments.length; H++)
        b += "&args[]=" + encodeURIComponent(arguments[H]);
    }
    return "Minified React error #" + N + "; visit " + b + " for the full message or use the non-minified dev environment for full errors and additional helpful warnings.";
  }
  function r() {
  }
  var s = {
    d: {
      f: r,
      r: function() {
        throw Error(o(522));
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
  }, T = Symbol.for("react.portal");
  function E(N, b, H) {
    var p = 3 < arguments.length && arguments[3] !== void 0 ? arguments[3] : null;
    return {
      $$typeof: T,
      key: p == null ? null : "" + p,
      children: N,
      containerInfo: b,
      implementation: H
    };
  }
  var C = c.__CLIENT_INTERNALS_DO_NOT_USE_OR_WARN_USERS_THEY_CANNOT_UPGRADE;
  function U(N, b) {
    if (N === "font") return "";
    if (typeof b == "string")
      return b === "use-credentials" ? b : "";
  }
  return lt.__DOM_INTERNALS_DO_NOT_USE_OR_WARN_USERS_THEY_CANNOT_UPGRADE = s, lt.createPortal = function(N, b) {
    var H = 2 < arguments.length && arguments[2] !== void 0 ? arguments[2] : null;
    if (!b || b.nodeType !== 1 && b.nodeType !== 9 && b.nodeType !== 11)
      throw Error(o(299));
    return E(N, b, null, H);
  }, lt.flushSync = function(N) {
    var b = C.T, H = s.p;
    try {
      if (C.T = null, s.p = 2, N) return N();
    } finally {
      C.T = b, s.p = H, s.d.f();
    }
  }, lt.preconnect = function(N, b) {
    typeof N == "string" && (b ? (b = b.crossOrigin, b = typeof b == "string" ? b === "use-credentials" ? b : "" : void 0) : b = null, s.d.C(N, b));
  }, lt.prefetchDNS = function(N) {
    typeof N == "string" && s.d.D(N);
  }, lt.preinit = function(N, b) {
    if (typeof N == "string" && b && typeof b.as == "string") {
      var H = b.as, p = U(H, b.crossOrigin), D = typeof b.integrity == "string" ? b.integrity : void 0, X = typeof b.fetchPriority == "string" ? b.fetchPriority : void 0;
      H === "style" ? s.d.S(
        N,
        typeof b.precedence == "string" ? b.precedence : void 0,
        {
          crossOrigin: p,
          integrity: D,
          fetchPriority: X
        }
      ) : H === "script" && s.d.X(N, {
        crossOrigin: p,
        integrity: D,
        fetchPriority: X,
        nonce: typeof b.nonce == "string" ? b.nonce : void 0
      });
    }
  }, lt.preinitModule = function(N, b) {
    if (typeof N == "string")
      if (typeof b == "object" && b !== null) {
        if (b.as == null || b.as === "script") {
          var H = U(
            b.as,
            b.crossOrigin
          );
          s.d.M(N, {
            crossOrigin: H,
            integrity: typeof b.integrity == "string" ? b.integrity : void 0,
            nonce: typeof b.nonce == "string" ? b.nonce : void 0
          });
        }
      } else b == null && s.d.M(N);
  }, lt.preload = function(N, b) {
    if (typeof N == "string" && typeof b == "object" && b !== null && typeof b.as == "string") {
      var H = b.as, p = U(H, b.crossOrigin);
      s.d.L(N, H, {
        crossOrigin: p,
        integrity: typeof b.integrity == "string" ? b.integrity : void 0,
        nonce: typeof b.nonce == "string" ? b.nonce : void 0,
        type: typeof b.type == "string" ? b.type : void 0,
        fetchPriority: typeof b.fetchPriority == "string" ? b.fetchPriority : void 0,
        referrerPolicy: typeof b.referrerPolicy == "string" ? b.referrerPolicy : void 0,
        imageSrcSet: typeof b.imageSrcSet == "string" ? b.imageSrcSet : void 0,
        imageSizes: typeof b.imageSizes == "string" ? b.imageSizes : void 0,
        media: typeof b.media == "string" ? b.media : void 0
      });
    }
  }, lt.preloadModule = function(N, b) {
    if (typeof N == "string")
      if (b) {
        var H = U(b.as, b.crossOrigin);
        s.d.m(N, {
          as: typeof b.as == "string" && b.as !== "script" ? b.as : void 0,
          crossOrigin: H,
          integrity: typeof b.integrity == "string" ? b.integrity : void 0
        });
      } else s.d.m(N);
  }, lt.requestFormReset = function(N) {
    s.d.r(N);
  }, lt.unstable_batchedUpdates = function(N, b) {
    return N(b);
  }, lt.useFormState = function(N, b, H) {
    return C.H.useFormState(N, b, H);
  }, lt.useFormStatus = function() {
    return C.H.useHostTransitionStatus();
  }, lt.version = "19.2.5", lt;
}
var Pd;
function Yh() {
  if (Pd) return Bf.exports;
  Pd = 1;
  function c() {
    if (!(typeof __REACT_DEVTOOLS_GLOBAL_HOOK__ > "u" || typeof __REACT_DEVTOOLS_GLOBAL_HOOK__.checkDCE != "function"))
      try {
        __REACT_DEVTOOLS_GLOBAL_HOOK__.checkDCE(c);
      } catch (o) {
        console.error(o);
      }
  }
  return c(), Bf.exports = Bh(), Bf.exports;
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
var ly;
function Lh() {
  if (ly) return wa;
  ly = 1;
  var c = Hh(), o = Vf(), r = Yh();
  function s(l) {
    var t = "https://react.dev/errors/" + l;
    if (1 < arguments.length) {
      t += "?args[]=" + encodeURIComponent(arguments[1]);
      for (var e = 2; e < arguments.length; e++)
        t += "&args[]=" + encodeURIComponent(arguments[e]);
    }
    return "Minified React error #" + l + "; visit " + t + " for the full message or use the non-minified dev environment for full errors and additional helpful warnings.";
  }
  function T(l) {
    return !(!l || l.nodeType !== 1 && l.nodeType !== 9 && l.nodeType !== 11);
  }
  function E(l) {
    var t = l, e = l;
    if (l.alternate) for (; t.return; ) t = t.return;
    else {
      l = t;
      do
        t = l, (t.flags & 4098) !== 0 && (e = t.return), l = t.return;
      while (l);
    }
    return t.tag === 3 ? e : null;
  }
  function C(l) {
    if (l.tag === 13) {
      var t = l.memoizedState;
      if (t === null && (l = l.alternate, l !== null && (t = l.memoizedState)), t !== null) return t.dehydrated;
    }
    return null;
  }
  function U(l) {
    if (l.tag === 31) {
      var t = l.memoizedState;
      if (t === null && (l = l.alternate, l !== null && (t = l.memoizedState)), t !== null) return t.dehydrated;
    }
    return null;
  }
  function N(l) {
    if (E(l) !== l)
      throw Error(s(188));
  }
  function b(l) {
    var t = l.alternate;
    if (!t) {
      if (t = E(l), t === null) throw Error(s(188));
      return t !== l ? null : l;
    }
    for (var e = l, u = t; ; ) {
      var a = e.return;
      if (a === null) break;
      var n = a.alternate;
      if (n === null) {
        if (u = a.return, u !== null) {
          e = u;
          continue;
        }
        break;
      }
      if (a.child === n.child) {
        for (n = a.child; n; ) {
          if (n === e) return N(a), l;
          if (n === u) return N(a), t;
          n = n.sibling;
        }
        throw Error(s(188));
      }
      if (e.return !== u.return) e = a, u = n;
      else {
        for (var i = !1, f = a.child; f; ) {
          if (f === e) {
            i = !0, e = a, u = n;
            break;
          }
          if (f === u) {
            i = !0, u = a, e = n;
            break;
          }
          f = f.sibling;
        }
        if (!i) {
          for (f = n.child; f; ) {
            if (f === e) {
              i = !0, e = n, u = a;
              break;
            }
            if (f === u) {
              i = !0, u = n, e = a;
              break;
            }
            f = f.sibling;
          }
          if (!i) throw Error(s(189));
        }
      }
      if (e.alternate !== u) throw Error(s(190));
    }
    if (e.tag !== 3) throw Error(s(188));
    return e.stateNode.current === e ? l : t;
  }
  function H(l) {
    var t = l.tag;
    if (t === 5 || t === 26 || t === 27 || t === 6) return l;
    for (l = l.child; l !== null; ) {
      if (t = H(l), t !== null) return t;
      l = l.sibling;
    }
    return null;
  }
  var p = Object.assign, D = Symbol.for("react.element"), X = Symbol.for("react.transitional.element"), k = Symbol.for("react.portal"), K = Symbol.for("react.fragment"), G = Symbol.for("react.strict_mode"), Z = Symbol.for("react.profiler"), ol = Symbol.for("react.consumer"), W = Symbol.for("react.context"), I = Symbol.for("react.forward_ref"), Il = Symbol.for("react.suspense"), $ = Symbol.for("react.suspense_list"), nl = Symbol.for("react.memo"), Ql = Symbol.for("react.lazy"), ot = Symbol.for("react.activity"), qt = Symbol.for("react.memo_cache_sentinel"), dt = Symbol.iterator;
  function Pl(l) {
    return l === null || typeof l != "object" ? null : (l = dt && l[dt] || l["@@iterator"], typeof l == "function" ? l : null);
  }
  var Qt = Symbol.for("react.client.reference");
  function Jl(l) {
    if (l == null) return null;
    if (typeof l == "function")
      return l.$$typeof === Qt ? null : l.displayName || l.name || null;
    if (typeof l == "string") return l;
    switch (l) {
      case K:
        return "Fragment";
      case Z:
        return "Profiler";
      case G:
        return "StrictMode";
      case Il:
        return "Suspense";
      case $:
        return "SuspenseList";
      case ot:
        return "Activity";
    }
    if (typeof l == "object")
      switch (l.$$typeof) {
        case k:
          return "Portal";
        case W:
          return l.displayName || "Context";
        case ol:
          return (l._context.displayName || "Context") + ".Consumer";
        case I:
          var t = l.render;
          return l = l.displayName, l || (l = t.displayName || t.name || "", l = l !== "" ? "ForwardRef(" + l + ")" : "ForwardRef"), l;
        case nl:
          return t = l.displayName || null, t !== null ? t : Jl(l.type) || "Memo";
        case Ql:
          t = l._payload, l = l._init;
          try {
            return Jl(l(t));
          } catch {
          }
      }
    return null;
  }
  var yt = Array.isArray, q = o.__CLIENT_INTERNALS_DO_NOT_USE_OR_WARN_USERS_THEY_CANNOT_UPGRADE, j = r.__DOM_INTERNALS_DO_NOT_USE_OR_WARN_USERS_THEY_CANNOT_UPGRADE, B = {
    pending: !1,
    data: null,
    method: null,
    action: null
  }, ml = [], dl = -1;
  function v(l) {
    return { current: l };
  }
  function O(l) {
    0 > dl || (l.current = ml[dl], ml[dl] = null, dl--);
  }
  function R(l, t) {
    dl++, ml[dl] = l.current, l.current = t;
  }
  var L = v(null), F = v(null), ll = v(null), yl = v(null);
  function Ul(l, t) {
    switch (R(ll, t), R(F, l), R(L, null), t.nodeType) {
      case 9:
      case 11:
        l = (l = t.documentElement) && (l = l.namespaceURI) ? md(l) : 0;
        break;
      default:
        if (l = t.tagName, t = t.namespaceURI)
          t = md(t), l = gd(t, l);
        else
          switch (l) {
            case "svg":
              l = 1;
              break;
            case "math":
              l = 2;
              break;
            default:
              l = 0;
          }
    }
    O(L), R(L, l);
  }
  function Nl() {
    O(L), O(F), O(ll);
  }
  function Ze(l) {
    l.memoizedState !== null && R(yl, l);
    var t = L.current, e = gd(t, l.type);
    t !== e && (R(F, l), R(L, e));
  }
  function ou(l) {
    F.current === l && (O(L), O(F)), yl.current === l && (O(yl), Za._currentValue = B);
  }
  var J, bl;
  function Ol(l) {
    if (J === void 0)
      try {
        throw Error();
      } catch (e) {
        var t = e.stack.trim().match(/\n( *(at )?)/);
        J = t && t[1] || "", bl = -1 < e.stack.indexOf(`
    at`) ? " (<anonymous>)" : -1 < e.stack.indexOf("@") ? "@unknown:0:0" : "";
      }
    return `
` + J + l + bl;
  }
  var Hl = !1;
  function ve(l, t) {
    if (!l || Hl) return "";
    Hl = !0;
    var e = Error.prepareStackTrace;
    Error.prepareStackTrace = void 0;
    try {
      var u = {
        DetermineComponentFrameRoot: function() {
          try {
            if (t) {
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
                } catch (_) {
                  var S = _;
                }
                Reflect.construct(l, [], M);
              } else {
                try {
                  M.call();
                } catch (_) {
                  S = _;
                }
                l.call(M.prototype);
              }
            } else {
              try {
                throw Error();
              } catch (_) {
                S = _;
              }
              (M = l()) && typeof M.catch == "function" && M.catch(function() {
              });
            }
          } catch (_) {
            if (_ && S && typeof _.stack == "string")
              return [_.stack, S.stack];
          }
          return [null, null];
        }
      };
      u.DetermineComponentFrameRoot.displayName = "DetermineComponentFrameRoot";
      var a = Object.getOwnPropertyDescriptor(
        u.DetermineComponentFrameRoot,
        "name"
      );
      a && a.configurable && Object.defineProperty(
        u.DetermineComponentFrameRoot,
        "name",
        { value: "DetermineComponentFrameRoot" }
      );
      var n = u.DetermineComponentFrameRoot(), i = n[0], f = n[1];
      if (i && f) {
        var d = i.split(`
`), g = f.split(`
`);
        for (a = u = 0; u < d.length && !d[u].includes("DetermineComponentFrameRoot"); )
          u++;
        for (; a < g.length && !g[a].includes(
          "DetermineComponentFrameRoot"
        ); )
          a++;
        if (u === d.length || a === g.length)
          for (u = d.length - 1, a = g.length - 1; 1 <= u && 0 <= a && d[u] !== g[a]; )
            a--;
        for (; 1 <= u && 0 <= a; u--, a--)
          if (d[u] !== g[a]) {
            if (u !== 1 || a !== 1)
              do
                if (u--, a--, 0 > a || d[u] !== g[a]) {
                  var z = `
` + d[u].replace(" at new ", " at ");
                  return l.displayName && z.includes("<anonymous>") && (z = z.replace("<anonymous>", l.displayName)), z;
                }
              while (1 <= u && 0 <= a);
            break;
          }
      }
    } finally {
      Hl = !1, Error.prepareStackTrace = e;
    }
    return (e = l ? l.displayName || l.name : "") ? Ol(e) : "";
  }
  function Wa(l, t) {
    switch (l.tag) {
      case 26:
      case 27:
      case 5:
        return Ol(l.type);
      case 16:
        return Ol("Lazy");
      case 13:
        return l.child !== t && t !== null ? Ol("Suspense Fallback") : Ol("Suspense");
      case 19:
        return Ol("SuspenseList");
      case 0:
      case 15:
        return ve(l.type, !1);
      case 11:
        return ve(l.type.render, !1);
      case 1:
        return ve(l.type, !0);
      case 31:
        return Ol("Activity");
      default:
        return "";
    }
  }
  function Jf(l) {
    try {
      var t = "", e = null;
      do
        t += Wa(l, e), e = l, l = l.return;
      while (l);
      return t;
    } catch (u) {
      return `
Error generating stack: ` + u.message + `
` + u.stack;
    }
  }
  var mi = Object.prototype.hasOwnProperty, gi = c.unstable_scheduleCallback, bi = c.unstable_cancelCallback, ry = c.unstable_shouldYield, oy = c.unstable_requestPaint, vt = c.unstable_now, dy = c.unstable_getCurrentPriorityLevel, wf = c.unstable_ImmediatePriority, kf = c.unstable_UserBlockingPriority, Fa = c.unstable_NormalPriority, yy = c.unstable_LowPriority, $f = c.unstable_IdlePriority, vy = c.log, hy = c.unstable_setDisableYieldValue, ta = null, ht = null;
  function he(l) {
    if (typeof vy == "function" && hy(l), ht && typeof ht.setStrictMode == "function")
      try {
        ht.setStrictMode(ta, l);
      } catch {
      }
  }
  var mt = Math.clz32 ? Math.clz32 : by, my = Math.log, gy = Math.LN2;
  function by(l) {
    return l >>>= 0, l === 0 ? 32 : 31 - (my(l) / gy | 0) | 0;
  }
  var Ia = 256, Pa = 262144, ln = 4194304;
  function Ve(l) {
    var t = l & 42;
    if (t !== 0) return t;
    switch (l & -l) {
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
        return l & 261888;
      case 262144:
      case 524288:
      case 1048576:
      case 2097152:
        return l & 3932160;
      case 4194304:
      case 8388608:
      case 16777216:
      case 33554432:
        return l & 62914560;
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
        return l;
    }
  }
  function tn(l, t, e) {
    var u = l.pendingLanes;
    if (u === 0) return 0;
    var a = 0, n = l.suspendedLanes, i = l.pingedLanes;
    l = l.warmLanes;
    var f = u & 134217727;
    return f !== 0 ? (u = f & ~n, u !== 0 ? a = Ve(u) : (i &= f, i !== 0 ? a = Ve(i) : e || (e = f & ~l, e !== 0 && (a = Ve(e))))) : (f = u & ~n, f !== 0 ? a = Ve(f) : i !== 0 ? a = Ve(i) : e || (e = u & ~l, e !== 0 && (a = Ve(e)))), a === 0 ? 0 : t !== 0 && t !== a && (t & n) === 0 && (n = a & -a, e = t & -t, n >= e || n === 32 && (e & 4194048) !== 0) ? t : a;
  }
  function ea(l, t) {
    return (l.pendingLanes & ~(l.suspendedLanes & ~l.pingedLanes) & t) === 0;
  }
  function Sy(l, t) {
    switch (l) {
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
  function Wf() {
    var l = ln;
    return ln <<= 1, (ln & 62914560) === 0 && (ln = 4194304), l;
  }
  function Si(l) {
    for (var t = [], e = 0; 31 > e; e++) t.push(l);
    return t;
  }
  function ua(l, t) {
    l.pendingLanes |= t, t !== 268435456 && (l.suspendedLanes = 0, l.pingedLanes = 0, l.warmLanes = 0);
  }
  function py(l, t, e, u, a, n) {
    var i = l.pendingLanes;
    l.pendingLanes = e, l.suspendedLanes = 0, l.pingedLanes = 0, l.warmLanes = 0, l.expiredLanes &= e, l.entangledLanes &= e, l.errorRecoveryDisabledLanes &= e, l.shellSuspendCounter = 0;
    var f = l.entanglements, d = l.expirationTimes, g = l.hiddenUpdates;
    for (e = i & ~e; 0 < e; ) {
      var z = 31 - mt(e), M = 1 << z;
      f[z] = 0, d[z] = -1;
      var S = g[z];
      if (S !== null)
        for (g[z] = null, z = 0; z < S.length; z++) {
          var _ = S[z];
          _ !== null && (_.lane &= -536870913);
        }
      e &= ~M;
    }
    u !== 0 && Ff(l, u, 0), n !== 0 && a === 0 && l.tag !== 0 && (l.suspendedLanes |= n & ~(i & ~t));
  }
  function Ff(l, t, e) {
    l.pendingLanes |= t, l.suspendedLanes &= ~t;
    var u = 31 - mt(t);
    l.entangledLanes |= t, l.entanglements[u] = l.entanglements[u] | 1073741824 | e & 261930;
  }
  function If(l, t) {
    var e = l.entangledLanes |= t;
    for (l = l.entanglements; e; ) {
      var u = 31 - mt(e), a = 1 << u;
      a & t | l[u] & t && (l[u] |= t), e &= ~a;
    }
  }
  function Pf(l, t) {
    var e = t & -t;
    return e = (e & 42) !== 0 ? 1 : pi(e), (e & (l.suspendedLanes | t)) !== 0 ? 0 : e;
  }
  function pi(l) {
    switch (l) {
      case 2:
        l = 1;
        break;
      case 8:
        l = 4;
        break;
      case 32:
        l = 16;
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
        l = 128;
        break;
      case 268435456:
        l = 134217728;
        break;
      default:
        l = 0;
    }
    return l;
  }
  function _i(l) {
    return l &= -l, 2 < l ? 8 < l ? (l & 134217727) !== 0 ? 32 : 268435456 : 8 : 2;
  }
  function ls() {
    var l = j.p;
    return l !== 0 ? l : (l = window.event, l === void 0 ? 32 : Gd(l.type));
  }
  function ts(l, t) {
    var e = j.p;
    try {
      return j.p = l, t();
    } finally {
      j.p = e;
    }
  }
  var me = Math.random().toString(36).slice(2), wl = "__reactFiber$" + me, ut = "__reactProps$" + me, du = "__reactContainer$" + me, Ei = "__reactEvents$" + me, _y = "__reactListeners$" + me, Ey = "__reactHandles$" + me, es = "__reactResources$" + me, aa = "__reactMarker$" + me;
  function zi(l) {
    delete l[wl], delete l[ut], delete l[Ei], delete l[_y], delete l[Ey];
  }
  function yu(l) {
    var t = l[wl];
    if (t) return t;
    for (var e = l.parentNode; e; ) {
      if (t = e[du] || e[wl]) {
        if (e = t.alternate, t.child !== null || e !== null && e.child !== null)
          for (l = qd(l); l !== null; ) {
            if (e = l[wl]) return e;
            l = qd(l);
          }
        return t;
      }
      l = e, e = l.parentNode;
    }
    return null;
  }
  function vu(l) {
    if (l = l[wl] || l[du]) {
      var t = l.tag;
      if (t === 5 || t === 6 || t === 13 || t === 31 || t === 26 || t === 27 || t === 3)
        return l;
    }
    return null;
  }
  function na(l) {
    var t = l.tag;
    if (t === 5 || t === 26 || t === 27 || t === 6) return l.stateNode;
    throw Error(s(33));
  }
  function hu(l) {
    var t = l[es];
    return t || (t = l[es] = { hoistableStyles: /* @__PURE__ */ new Map(), hoistableScripts: /* @__PURE__ */ new Map() }), t;
  }
  function Zl(l) {
    l[aa] = !0;
  }
  var us = /* @__PURE__ */ new Set(), as = {};
  function Ke(l, t) {
    mu(l, t), mu(l + "Capture", t);
  }
  function mu(l, t) {
    for (as[l] = t, l = 0; l < t.length; l++)
      us.add(t[l]);
  }
  var zy = RegExp(
    "^[:A-Z_a-z\\u00C0-\\u00D6\\u00D8-\\u00F6\\u00F8-\\u02FF\\u0370-\\u037D\\u037F-\\u1FFF\\u200C-\\u200D\\u2070-\\u218F\\u2C00-\\u2FEF\\u3001-\\uD7FF\\uF900-\\uFDCF\\uFDF0-\\uFFFD][:A-Z_a-z\\u00C0-\\u00D6\\u00D8-\\u00F6\\u00F8-\\u02FF\\u0370-\\u037D\\u037F-\\u1FFF\\u200C-\\u200D\\u2070-\\u218F\\u2C00-\\u2FEF\\u3001-\\uD7FF\\uF900-\\uFDCF\\uFDF0-\\uFFFD\\-.0-9\\u00B7\\u0300-\\u036F\\u203F-\\u2040]*$"
  ), ns = {}, is = {};
  function qy(l) {
    return mi.call(is, l) ? !0 : mi.call(ns, l) ? !1 : zy.test(l) ? is[l] = !0 : (ns[l] = !0, !1);
  }
  function en(l, t, e) {
    if (qy(t))
      if (e === null) l.removeAttribute(t);
      else {
        switch (typeof e) {
          case "undefined":
          case "function":
          case "symbol":
            l.removeAttribute(t);
            return;
          case "boolean":
            var u = t.toLowerCase().slice(0, 5);
            if (u !== "data-" && u !== "aria-") {
              l.removeAttribute(t);
              return;
            }
        }
        l.setAttribute(t, "" + e);
      }
  }
  function un(l, t, e) {
    if (e === null) l.removeAttribute(t);
    else {
      switch (typeof e) {
        case "undefined":
        case "function":
        case "symbol":
        case "boolean":
          l.removeAttribute(t);
          return;
      }
      l.setAttribute(t, "" + e);
    }
  }
  function wt(l, t, e, u) {
    if (u === null) l.removeAttribute(e);
    else {
      switch (typeof u) {
        case "undefined":
        case "function":
        case "symbol":
        case "boolean":
          l.removeAttribute(e);
          return;
      }
      l.setAttributeNS(t, e, "" + u);
    }
  }
  function At(l) {
    switch (typeof l) {
      case "bigint":
      case "boolean":
      case "number":
      case "string":
      case "undefined":
        return l;
      case "object":
        return l;
      default:
        return "";
    }
  }
  function cs(l) {
    var t = l.type;
    return (l = l.nodeName) && l.toLowerCase() === "input" && (t === "checkbox" || t === "radio");
  }
  function Ay(l, t, e) {
    var u = Object.getOwnPropertyDescriptor(
      l.constructor.prototype,
      t
    );
    if (!l.hasOwnProperty(t) && typeof u < "u" && typeof u.get == "function" && typeof u.set == "function") {
      var a = u.get, n = u.set;
      return Object.defineProperty(l, t, {
        configurable: !0,
        get: function() {
          return a.call(this);
        },
        set: function(i) {
          e = "" + i, n.call(this, i);
        }
      }), Object.defineProperty(l, t, {
        enumerable: u.enumerable
      }), {
        getValue: function() {
          return e;
        },
        setValue: function(i) {
          e = "" + i;
        },
        stopTracking: function() {
          l._valueTracker = null, delete l[t];
        }
      };
    }
  }
  function qi(l) {
    if (!l._valueTracker) {
      var t = cs(l) ? "checked" : "value";
      l._valueTracker = Ay(
        l,
        t,
        "" + l[t]
      );
    }
  }
  function fs(l) {
    if (!l) return !1;
    var t = l._valueTracker;
    if (!t) return !0;
    var e = t.getValue(), u = "";
    return l && (u = cs(l) ? l.checked ? "true" : "false" : l.value), l = u, l !== e ? (t.setValue(l), !0) : !1;
  }
  function an(l) {
    if (l = l || (typeof document < "u" ? document : void 0), typeof l > "u") return null;
    try {
      return l.activeElement || l.body;
    } catch {
      return l.body;
    }
  }
  var Ty = /[\n"\\]/g;
  function Tt(l) {
    return l.replace(
      Ty,
      function(t) {
        return "\\" + t.charCodeAt(0).toString(16) + " ";
      }
    );
  }
  function Ai(l, t, e, u, a, n, i, f) {
    l.name = "", i != null && typeof i != "function" && typeof i != "symbol" && typeof i != "boolean" ? l.type = i : l.removeAttribute("type"), t != null ? i === "number" ? (t === 0 && l.value === "" || l.value != t) && (l.value = "" + At(t)) : l.value !== "" + At(t) && (l.value = "" + At(t)) : i !== "submit" && i !== "reset" || l.removeAttribute("value"), t != null ? Ti(l, i, At(t)) : e != null ? Ti(l, i, At(e)) : u != null && l.removeAttribute("value"), a == null && n != null && (l.defaultChecked = !!n), a != null && (l.checked = a && typeof a != "function" && typeof a != "symbol"), f != null && typeof f != "function" && typeof f != "symbol" && typeof f != "boolean" ? l.name = "" + At(f) : l.removeAttribute("name");
  }
  function ss(l, t, e, u, a, n, i, f) {
    if (n != null && typeof n != "function" && typeof n != "symbol" && typeof n != "boolean" && (l.type = n), t != null || e != null) {
      if (!(n !== "submit" && n !== "reset" || t != null)) {
        qi(l);
        return;
      }
      e = e != null ? "" + At(e) : "", t = t != null ? "" + At(t) : e, f || t === l.value || (l.value = t), l.defaultValue = t;
    }
    u = u ?? a, u = typeof u != "function" && typeof u != "symbol" && !!u, l.checked = f ? l.checked : !!u, l.defaultChecked = !!u, i != null && typeof i != "function" && typeof i != "symbol" && typeof i != "boolean" && (l.name = i), qi(l);
  }
  function Ti(l, t, e) {
    t === "number" && an(l.ownerDocument) === l || l.defaultValue === "" + e || (l.defaultValue = "" + e);
  }
  function gu(l, t, e, u) {
    if (l = l.options, t) {
      t = {};
      for (var a = 0; a < e.length; a++)
        t["$" + e[a]] = !0;
      for (e = 0; e < l.length; e++)
        a = t.hasOwnProperty("$" + l[e].value), l[e].selected !== a && (l[e].selected = a), a && u && (l[e].defaultSelected = !0);
    } else {
      for (e = "" + At(e), t = null, a = 0; a < l.length; a++) {
        if (l[a].value === e) {
          l[a].selected = !0, u && (l[a].defaultSelected = !0);
          return;
        }
        t !== null || l[a].disabled || (t = l[a]);
      }
      t !== null && (t.selected = !0);
    }
  }
  function rs(l, t, e) {
    if (t != null && (t = "" + At(t), t !== l.value && (l.value = t), e == null)) {
      l.defaultValue !== t && (l.defaultValue = t);
      return;
    }
    l.defaultValue = e != null ? "" + At(e) : "";
  }
  function os(l, t, e, u) {
    if (t == null) {
      if (u != null) {
        if (e != null) throw Error(s(92));
        if (yt(u)) {
          if (1 < u.length) throw Error(s(93));
          u = u[0];
        }
        e = u;
      }
      e == null && (e = ""), t = e;
    }
    e = At(t), l.defaultValue = e, u = l.textContent, u === e && u !== "" && u !== null && (l.value = u), qi(l);
  }
  function bu(l, t) {
    if (t) {
      var e = l.firstChild;
      if (e && e === l.lastChild && e.nodeType === 3) {
        e.nodeValue = t;
        return;
      }
    }
    l.textContent = t;
  }
  var xy = new Set(
    "animationIterationCount aspectRatio borderImageOutset borderImageSlice borderImageWidth boxFlex boxFlexGroup boxOrdinalGroup columnCount columns flex flexGrow flexPositive flexShrink flexNegative flexOrder gridArea gridRow gridRowEnd gridRowSpan gridRowStart gridColumn gridColumnEnd gridColumnSpan gridColumnStart fontWeight lineClamp lineHeight opacity order orphans scale tabSize widows zIndex zoom fillOpacity floodOpacity stopOpacity strokeDasharray strokeDashoffset strokeMiterlimit strokeOpacity strokeWidth MozAnimationIterationCount MozBoxFlex MozBoxFlexGroup MozLineClamp msAnimationIterationCount msFlex msZoom msFlexGrow msFlexNegative msFlexOrder msFlexPositive msFlexShrink msGridColumn msGridColumnSpan msGridRow msGridRowSpan WebkitAnimationIterationCount WebkitBoxFlex WebKitBoxFlexGroup WebkitBoxOrdinalGroup WebkitColumnCount WebkitColumns WebkitFlex WebkitFlexGrow WebkitFlexPositive WebkitFlexShrink WebkitLineClamp".split(
      " "
    )
  );
  function ds(l, t, e) {
    var u = t.indexOf("--") === 0;
    e == null || typeof e == "boolean" || e === "" ? u ? l.setProperty(t, "") : t === "float" ? l.cssFloat = "" : l[t] = "" : u ? l.setProperty(t, e) : typeof e != "number" || e === 0 || xy.has(t) ? t === "float" ? l.cssFloat = e : l[t] = ("" + e).trim() : l[t] = e + "px";
  }
  function ys(l, t, e) {
    if (t != null && typeof t != "object")
      throw Error(s(62));
    if (l = l.style, e != null) {
      for (var u in e)
        !e.hasOwnProperty(u) || t != null && t.hasOwnProperty(u) || (u.indexOf("--") === 0 ? l.setProperty(u, "") : u === "float" ? l.cssFloat = "" : l[u] = "");
      for (var a in t)
        u = t[a], t.hasOwnProperty(a) && e[a] !== u && ds(l, a, u);
    } else
      for (var n in t)
        t.hasOwnProperty(n) && ds(l, n, t[n]);
  }
  function xi(l) {
    if (l.indexOf("-") === -1) return !1;
    switch (l) {
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
  var Ny = /* @__PURE__ */ new Map([
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
  ]), Oy = /^[\u0000-\u001F ]*j[\r\n\t]*a[\r\n\t]*v[\r\n\t]*a[\r\n\t]*s[\r\n\t]*c[\r\n\t]*r[\r\n\t]*i[\r\n\t]*p[\r\n\t]*t[\r\n\t]*:/i;
  function nn(l) {
    return Oy.test("" + l) ? "javascript:throw new Error('React has blocked a javascript: URL as a security precaution.')" : l;
  }
  function kt() {
  }
  var Ni = null;
  function Oi(l) {
    return l = l.target || l.srcElement || window, l.correspondingUseElement && (l = l.correspondingUseElement), l.nodeType === 3 ? l.parentNode : l;
  }
  var Su = null, pu = null;
  function vs(l) {
    var t = vu(l);
    if (t && (l = t.stateNode)) {
      var e = l[ut] || null;
      l: switch (l = t.stateNode, t.type) {
        case "input":
          if (Ai(
            l,
            e.value,
            e.defaultValue,
            e.defaultValue,
            e.checked,
            e.defaultChecked,
            e.type,
            e.name
          ), t = e.name, e.type === "radio" && t != null) {
            for (e = l; e.parentNode; ) e = e.parentNode;
            for (e = e.querySelectorAll(
              'input[name="' + Tt(
                "" + t
              ) + '"][type="radio"]'
            ), t = 0; t < e.length; t++) {
              var u = e[t];
              if (u !== l && u.form === l.form) {
                var a = u[ut] || null;
                if (!a) throw Error(s(90));
                Ai(
                  u,
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
            for (t = 0; t < e.length; t++)
              u = e[t], u.form === l.form && fs(u);
          }
          break l;
        case "textarea":
          rs(l, e.value, e.defaultValue);
          break l;
        case "select":
          t = e.value, t != null && gu(l, !!e.multiple, t, !1);
      }
    }
  }
  var Mi = !1;
  function hs(l, t, e) {
    if (Mi) return l(t, e);
    Mi = !0;
    try {
      var u = l(t);
      return u;
    } finally {
      if (Mi = !1, (Su !== null || pu !== null) && (wn(), Su && (t = Su, l = pu, pu = Su = null, vs(t), l)))
        for (t = 0; t < l.length; t++) vs(l[t]);
    }
  }
  function ia(l, t) {
    var e = l.stateNode;
    if (e === null) return null;
    var u = e[ut] || null;
    if (u === null) return null;
    e = u[t];
    l: switch (t) {
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
        (u = !u.disabled) || (l = l.type, u = !(l === "button" || l === "input" || l === "select" || l === "textarea")), l = !u;
        break l;
      default:
        l = !1;
    }
    if (l) return null;
    if (e && typeof e != "function")
      throw Error(
        s(231, t, typeof e)
      );
    return e;
  }
  var $t = !(typeof window > "u" || typeof window.document > "u" || typeof window.document.createElement > "u"), Di = !1;
  if ($t)
    try {
      var ca = {};
      Object.defineProperty(ca, "passive", {
        get: function() {
          Di = !0;
        }
      }), window.addEventListener("test", ca, ca), window.removeEventListener("test", ca, ca);
    } catch {
      Di = !1;
    }
  var ge = null, Ui = null, cn = null;
  function ms() {
    if (cn) return cn;
    var l, t = Ui, e = t.length, u, a = "value" in ge ? ge.value : ge.textContent, n = a.length;
    for (l = 0; l < e && t[l] === a[l]; l++) ;
    var i = e - l;
    for (u = 1; u <= i && t[e - u] === a[n - u]; u++) ;
    return cn = a.slice(l, 1 < u ? 1 - u : void 0);
  }
  function fn(l) {
    var t = l.keyCode;
    return "charCode" in l ? (l = l.charCode, l === 0 && t === 13 && (l = 13)) : l = t, l === 10 && (l = 13), 32 <= l || l === 13 ? l : 0;
  }
  function sn() {
    return !0;
  }
  function gs() {
    return !1;
  }
  function at(l) {
    function t(e, u, a, n, i) {
      this._reactName = e, this._targetInst = a, this.type = u, this.nativeEvent = n, this.target = i, this.currentTarget = null;
      for (var f in l)
        l.hasOwnProperty(f) && (e = l[f], this[f] = e ? e(n) : n[f]);
      return this.isDefaultPrevented = (n.defaultPrevented != null ? n.defaultPrevented : n.returnValue === !1) ? sn : gs, this.isPropagationStopped = gs, this;
    }
    return p(t.prototype, {
      preventDefault: function() {
        this.defaultPrevented = !0;
        var e = this.nativeEvent;
        e && (e.preventDefault ? e.preventDefault() : typeof e.returnValue != "unknown" && (e.returnValue = !1), this.isDefaultPrevented = sn);
      },
      stopPropagation: function() {
        var e = this.nativeEvent;
        e && (e.stopPropagation ? e.stopPropagation() : typeof e.cancelBubble != "unknown" && (e.cancelBubble = !0), this.isPropagationStopped = sn);
      },
      persist: function() {
      },
      isPersistent: sn
    }), t;
  }
  var Je = {
    eventPhase: 0,
    bubbles: 0,
    cancelable: 0,
    timeStamp: function(l) {
      return l.timeStamp || Date.now();
    },
    defaultPrevented: 0,
    isTrusted: 0
  }, rn = at(Je), fa = p({}, Je, { view: 0, detail: 0 }), My = at(fa), Ci, ji, sa, on = p({}, fa, {
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
    getModifierState: Hi,
    button: 0,
    buttons: 0,
    relatedTarget: function(l) {
      return l.relatedTarget === void 0 ? l.fromElement === l.srcElement ? l.toElement : l.fromElement : l.relatedTarget;
    },
    movementX: function(l) {
      return "movementX" in l ? l.movementX : (l !== sa && (sa && l.type === "mousemove" ? (Ci = l.screenX - sa.screenX, ji = l.screenY - sa.screenY) : ji = Ci = 0, sa = l), Ci);
    },
    movementY: function(l) {
      return "movementY" in l ? l.movementY : ji;
    }
  }), bs = at(on), Dy = p({}, on, { dataTransfer: 0 }), Uy = at(Dy), Cy = p({}, fa, { relatedTarget: 0 }), Ri = at(Cy), jy = p({}, Je, {
    animationName: 0,
    elapsedTime: 0,
    pseudoElement: 0
  }), Ry = at(jy), Hy = p({}, Je, {
    clipboardData: function(l) {
      return "clipboardData" in l ? l.clipboardData : window.clipboardData;
    }
  }), By = at(Hy), Yy = p({}, Je, { data: 0 }), Ss = at(Yy), Ly = {
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
  }, Gy = {
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
  }, Qy = {
    Alt: "altKey",
    Control: "ctrlKey",
    Meta: "metaKey",
    Shift: "shiftKey"
  };
  function Xy(l) {
    var t = this.nativeEvent;
    return t.getModifierState ? t.getModifierState(l) : (l = Qy[l]) ? !!t[l] : !1;
  }
  function Hi() {
    return Xy;
  }
  var Zy = p({}, fa, {
    key: function(l) {
      if (l.key) {
        var t = Ly[l.key] || l.key;
        if (t !== "Unidentified") return t;
      }
      return l.type === "keypress" ? (l = fn(l), l === 13 ? "Enter" : String.fromCharCode(l)) : l.type === "keydown" || l.type === "keyup" ? Gy[l.keyCode] || "Unidentified" : "";
    },
    code: 0,
    location: 0,
    ctrlKey: 0,
    shiftKey: 0,
    altKey: 0,
    metaKey: 0,
    repeat: 0,
    locale: 0,
    getModifierState: Hi,
    charCode: function(l) {
      return l.type === "keypress" ? fn(l) : 0;
    },
    keyCode: function(l) {
      return l.type === "keydown" || l.type === "keyup" ? l.keyCode : 0;
    },
    which: function(l) {
      return l.type === "keypress" ? fn(l) : l.type === "keydown" || l.type === "keyup" ? l.keyCode : 0;
    }
  }), Vy = at(Zy), Ky = p({}, on, {
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
  }), ps = at(Ky), Jy = p({}, fa, {
    touches: 0,
    targetTouches: 0,
    changedTouches: 0,
    altKey: 0,
    metaKey: 0,
    ctrlKey: 0,
    shiftKey: 0,
    getModifierState: Hi
  }), wy = at(Jy), ky = p({}, Je, {
    propertyName: 0,
    elapsedTime: 0,
    pseudoElement: 0
  }), $y = at(ky), Wy = p({}, on, {
    deltaX: function(l) {
      return "deltaX" in l ? l.deltaX : "wheelDeltaX" in l ? -l.wheelDeltaX : 0;
    },
    deltaY: function(l) {
      return "deltaY" in l ? l.deltaY : "wheelDeltaY" in l ? -l.wheelDeltaY : "wheelDelta" in l ? -l.wheelDelta : 0;
    },
    deltaZ: 0,
    deltaMode: 0
  }), Fy = at(Wy), Iy = p({}, Je, {
    newState: 0,
    oldState: 0
  }), Py = at(Iy), lv = [9, 13, 27, 32], Bi = $t && "CompositionEvent" in window, ra = null;
  $t && "documentMode" in document && (ra = document.documentMode);
  var tv = $t && "TextEvent" in window && !ra, _s = $t && (!Bi || ra && 8 < ra && 11 >= ra), Es = " ", zs = !1;
  function qs(l, t) {
    switch (l) {
      case "keyup":
        return lv.indexOf(t.keyCode) !== -1;
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
  function As(l) {
    return l = l.detail, typeof l == "object" && "data" in l ? l.data : null;
  }
  var _u = !1;
  function ev(l, t) {
    switch (l) {
      case "compositionend":
        return As(t);
      case "keypress":
        return t.which !== 32 ? null : (zs = !0, Es);
      case "textInput":
        return l = t.data, l === Es && zs ? null : l;
      default:
        return null;
    }
  }
  function uv(l, t) {
    if (_u)
      return l === "compositionend" || !Bi && qs(l, t) ? (l = ms(), cn = Ui = ge = null, _u = !1, l) : null;
    switch (l) {
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
        return _s && t.locale !== "ko" ? null : t.data;
      default:
        return null;
    }
  }
  var av = {
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
  function Ts(l) {
    var t = l && l.nodeName && l.nodeName.toLowerCase();
    return t === "input" ? !!av[l.type] : t === "textarea";
  }
  function xs(l, t, e, u) {
    Su ? pu ? pu.push(u) : pu = [u] : Su = u, t = li(t, "onChange"), 0 < t.length && (e = new rn(
      "onChange",
      "change",
      null,
      e,
      u
    ), l.push({ event: e, listeners: t }));
  }
  var oa = null, da = null;
  function nv(l) {
    rd(l, 0);
  }
  function dn(l) {
    var t = na(l);
    if (fs(t)) return l;
  }
  function Ns(l, t) {
    if (l === "change") return t;
  }
  var Os = !1;
  if ($t) {
    var Yi;
    if ($t) {
      var Li = "oninput" in document;
      if (!Li) {
        var Ms = document.createElement("div");
        Ms.setAttribute("oninput", "return;"), Li = typeof Ms.oninput == "function";
      }
      Yi = Li;
    } else Yi = !1;
    Os = Yi && (!document.documentMode || 9 < document.documentMode);
  }
  function Ds() {
    oa && (oa.detachEvent("onpropertychange", Us), da = oa = null);
  }
  function Us(l) {
    if (l.propertyName === "value" && dn(da)) {
      var t = [];
      xs(
        t,
        da,
        l,
        Oi(l)
      ), hs(nv, t);
    }
  }
  function iv(l, t, e) {
    l === "focusin" ? (Ds(), oa = t, da = e, oa.attachEvent("onpropertychange", Us)) : l === "focusout" && Ds();
  }
  function cv(l) {
    if (l === "selectionchange" || l === "keyup" || l === "keydown")
      return dn(da);
  }
  function fv(l, t) {
    if (l === "click") return dn(t);
  }
  function sv(l, t) {
    if (l === "input" || l === "change")
      return dn(t);
  }
  function rv(l, t) {
    return l === t && (l !== 0 || 1 / l === 1 / t) || l !== l && t !== t;
  }
  var gt = typeof Object.is == "function" ? Object.is : rv;
  function ya(l, t) {
    if (gt(l, t)) return !0;
    if (typeof l != "object" || l === null || typeof t != "object" || t === null)
      return !1;
    var e = Object.keys(l), u = Object.keys(t);
    if (e.length !== u.length) return !1;
    for (u = 0; u < e.length; u++) {
      var a = e[u];
      if (!mi.call(t, a) || !gt(l[a], t[a]))
        return !1;
    }
    return !0;
  }
  function Cs(l) {
    for (; l && l.firstChild; ) l = l.firstChild;
    return l;
  }
  function js(l, t) {
    var e = Cs(l);
    l = 0;
    for (var u; e; ) {
      if (e.nodeType === 3) {
        if (u = l + e.textContent.length, l <= t && u >= t)
          return { node: e, offset: t - l };
        l = u;
      }
      l: {
        for (; e; ) {
          if (e.nextSibling) {
            e = e.nextSibling;
            break l;
          }
          e = e.parentNode;
        }
        e = void 0;
      }
      e = Cs(e);
    }
  }
  function Rs(l, t) {
    return l && t ? l === t ? !0 : l && l.nodeType === 3 ? !1 : t && t.nodeType === 3 ? Rs(l, t.parentNode) : "contains" in l ? l.contains(t) : l.compareDocumentPosition ? !!(l.compareDocumentPosition(t) & 16) : !1 : !1;
  }
  function Hs(l) {
    l = l != null && l.ownerDocument != null && l.ownerDocument.defaultView != null ? l.ownerDocument.defaultView : window;
    for (var t = an(l.document); t instanceof l.HTMLIFrameElement; ) {
      try {
        var e = typeof t.contentWindow.location.href == "string";
      } catch {
        e = !1;
      }
      if (e) l = t.contentWindow;
      else break;
      t = an(l.document);
    }
    return t;
  }
  function Gi(l) {
    var t = l && l.nodeName && l.nodeName.toLowerCase();
    return t && (t === "input" && (l.type === "text" || l.type === "search" || l.type === "tel" || l.type === "url" || l.type === "password") || t === "textarea" || l.contentEditable === "true");
  }
  var ov = $t && "documentMode" in document && 11 >= document.documentMode, Eu = null, Qi = null, va = null, Xi = !1;
  function Bs(l, t, e) {
    var u = e.window === e ? e.document : e.nodeType === 9 ? e : e.ownerDocument;
    Xi || Eu == null || Eu !== an(u) || (u = Eu, "selectionStart" in u && Gi(u) ? u = { start: u.selectionStart, end: u.selectionEnd } : (u = (u.ownerDocument && u.ownerDocument.defaultView || window).getSelection(), u = {
      anchorNode: u.anchorNode,
      anchorOffset: u.anchorOffset,
      focusNode: u.focusNode,
      focusOffset: u.focusOffset
    }), va && ya(va, u) || (va = u, u = li(Qi, "onSelect"), 0 < u.length && (t = new rn(
      "onSelect",
      "select",
      null,
      t,
      e
    ), l.push({ event: t, listeners: u }), t.target = Eu)));
  }
  function we(l, t) {
    var e = {};
    return e[l.toLowerCase()] = t.toLowerCase(), e["Webkit" + l] = "webkit" + t, e["Moz" + l] = "moz" + t, e;
  }
  var zu = {
    animationend: we("Animation", "AnimationEnd"),
    animationiteration: we("Animation", "AnimationIteration"),
    animationstart: we("Animation", "AnimationStart"),
    transitionrun: we("Transition", "TransitionRun"),
    transitionstart: we("Transition", "TransitionStart"),
    transitioncancel: we("Transition", "TransitionCancel"),
    transitionend: we("Transition", "TransitionEnd")
  }, Zi = {}, Ys = {};
  $t && (Ys = document.createElement("div").style, "AnimationEvent" in window || (delete zu.animationend.animation, delete zu.animationiteration.animation, delete zu.animationstart.animation), "TransitionEvent" in window || delete zu.transitionend.transition);
  function ke(l) {
    if (Zi[l]) return Zi[l];
    if (!zu[l]) return l;
    var t = zu[l], e;
    for (e in t)
      if (t.hasOwnProperty(e) && e in Ys)
        return Zi[l] = t[e];
    return l;
  }
  var Ls = ke("animationend"), Gs = ke("animationiteration"), Qs = ke("animationstart"), dv = ke("transitionrun"), yv = ke("transitionstart"), vv = ke("transitioncancel"), Xs = ke("transitionend"), Zs = /* @__PURE__ */ new Map(), Vi = "abort auxClick beforeToggle cancel canPlay canPlayThrough click close contextMenu copy cut drag dragEnd dragEnter dragExit dragLeave dragOver dragStart drop durationChange emptied encrypted ended error gotPointerCapture input invalid keyDown keyPress keyUp load loadedData loadedMetadata loadStart lostPointerCapture mouseDown mouseMove mouseOut mouseOver mouseUp paste pause play playing pointerCancel pointerDown pointerMove pointerOut pointerOver pointerUp progress rateChange reset resize seeked seeking stalled submit suspend timeUpdate touchCancel touchEnd touchStart volumeChange scroll toggle touchMove waiting wheel".split(
    " "
  );
  Vi.push("scrollEnd");
  function Ht(l, t) {
    Zs.set(l, t), Ke(t, [l]);
  }
  var yn = typeof reportError == "function" ? reportError : function(l) {
    if (typeof window == "object" && typeof window.ErrorEvent == "function") {
      var t = new window.ErrorEvent("error", {
        bubbles: !0,
        cancelable: !0,
        message: typeof l == "object" && l !== null && typeof l.message == "string" ? String(l.message) : String(l),
        error: l
      });
      if (!window.dispatchEvent(t)) return;
    } else if (typeof process == "object" && typeof process.emit == "function") {
      process.emit("uncaughtException", l);
      return;
    }
    console.error(l);
  }, xt = [], qu = 0, Ki = 0;
  function vn() {
    for (var l = qu, t = Ki = qu = 0; t < l; ) {
      var e = xt[t];
      xt[t++] = null;
      var u = xt[t];
      xt[t++] = null;
      var a = xt[t];
      xt[t++] = null;
      var n = xt[t];
      if (xt[t++] = null, u !== null && a !== null) {
        var i = u.pending;
        i === null ? a.next = a : (a.next = i.next, i.next = a), u.pending = a;
      }
      n !== 0 && Vs(e, a, n);
    }
  }
  function hn(l, t, e, u) {
    xt[qu++] = l, xt[qu++] = t, xt[qu++] = e, xt[qu++] = u, Ki |= u, l.lanes |= u, l = l.alternate, l !== null && (l.lanes |= u);
  }
  function Ji(l, t, e, u) {
    return hn(l, t, e, u), mn(l);
  }
  function $e(l, t) {
    return hn(l, null, null, t), mn(l);
  }
  function Vs(l, t, e) {
    l.lanes |= e;
    var u = l.alternate;
    u !== null && (u.lanes |= e);
    for (var a = !1, n = l.return; n !== null; )
      n.childLanes |= e, u = n.alternate, u !== null && (u.childLanes |= e), n.tag === 22 && (l = n.stateNode, l === null || l._visibility & 1 || (a = !0)), l = n, n = n.return;
    return l.tag === 3 ? (n = l.stateNode, a && t !== null && (a = 31 - mt(e), l = n.hiddenUpdates, u = l[a], u === null ? l[a] = [t] : u.push(t), t.lane = e | 536870912), n) : null;
  }
  function mn(l) {
    if (50 < Ha)
      throw Ha = 0, tf = null, Error(s(185));
    for (var t = l.return; t !== null; )
      l = t, t = l.return;
    return l.tag === 3 ? l.stateNode : null;
  }
  var Au = {};
  function hv(l, t, e, u) {
    this.tag = l, this.key = e, this.sibling = this.child = this.return = this.stateNode = this.type = this.elementType = null, this.index = 0, this.refCleanup = this.ref = null, this.pendingProps = t, this.dependencies = this.memoizedState = this.updateQueue = this.memoizedProps = null, this.mode = u, this.subtreeFlags = this.flags = 0, this.deletions = null, this.childLanes = this.lanes = 0, this.alternate = null;
  }
  function bt(l, t, e, u) {
    return new hv(l, t, e, u);
  }
  function wi(l) {
    return l = l.prototype, !(!l || !l.isReactComponent);
  }
  function Wt(l, t) {
    var e = l.alternate;
    return e === null ? (e = bt(
      l.tag,
      t,
      l.key,
      l.mode
    ), e.elementType = l.elementType, e.type = l.type, e.stateNode = l.stateNode, e.alternate = l, l.alternate = e) : (e.pendingProps = t, e.type = l.type, e.flags = 0, e.subtreeFlags = 0, e.deletions = null), e.flags = l.flags & 65011712, e.childLanes = l.childLanes, e.lanes = l.lanes, e.child = l.child, e.memoizedProps = l.memoizedProps, e.memoizedState = l.memoizedState, e.updateQueue = l.updateQueue, t = l.dependencies, e.dependencies = t === null ? null : { lanes: t.lanes, firstContext: t.firstContext }, e.sibling = l.sibling, e.index = l.index, e.ref = l.ref, e.refCleanup = l.refCleanup, e;
  }
  function Ks(l, t) {
    l.flags &= 65011714;
    var e = l.alternate;
    return e === null ? (l.childLanes = 0, l.lanes = t, l.child = null, l.subtreeFlags = 0, l.memoizedProps = null, l.memoizedState = null, l.updateQueue = null, l.dependencies = null, l.stateNode = null) : (l.childLanes = e.childLanes, l.lanes = e.lanes, l.child = e.child, l.subtreeFlags = 0, l.deletions = null, l.memoizedProps = e.memoizedProps, l.memoizedState = e.memoizedState, l.updateQueue = e.updateQueue, l.type = e.type, t = e.dependencies, l.dependencies = t === null ? null : {
      lanes: t.lanes,
      firstContext: t.firstContext
    }), l;
  }
  function gn(l, t, e, u, a, n) {
    var i = 0;
    if (u = l, typeof l == "function") wi(l) && (i = 1);
    else if (typeof l == "string")
      i = ph(
        l,
        e,
        L.current
      ) ? 26 : l === "html" || l === "head" || l === "body" ? 27 : 5;
    else
      l: switch (l) {
        case ot:
          return l = bt(31, e, t, a), l.elementType = ot, l.lanes = n, l;
        case K:
          return We(e.children, a, n, t);
        case G:
          i = 8, a |= 24;
          break;
        case Z:
          return l = bt(12, e, t, a | 2), l.elementType = Z, l.lanes = n, l;
        case Il:
          return l = bt(13, e, t, a), l.elementType = Il, l.lanes = n, l;
        case $:
          return l = bt(19, e, t, a), l.elementType = $, l.lanes = n, l;
        default:
          if (typeof l == "object" && l !== null)
            switch (l.$$typeof) {
              case W:
                i = 10;
                break l;
              case ol:
                i = 9;
                break l;
              case I:
                i = 11;
                break l;
              case nl:
                i = 14;
                break l;
              case Ql:
                i = 16, u = null;
                break l;
            }
          i = 29, e = Error(
            s(130, l === null ? "null" : typeof l, "")
          ), u = null;
      }
    return t = bt(i, e, t, a), t.elementType = l, t.type = u, t.lanes = n, t;
  }
  function We(l, t, e, u) {
    return l = bt(7, l, u, t), l.lanes = e, l;
  }
  function ki(l, t, e) {
    return l = bt(6, l, null, t), l.lanes = e, l;
  }
  function Js(l) {
    var t = bt(18, null, null, 0);
    return t.stateNode = l, t;
  }
  function $i(l, t, e) {
    return t = bt(
      4,
      l.children !== null ? l.children : [],
      l.key,
      t
    ), t.lanes = e, t.stateNode = {
      containerInfo: l.containerInfo,
      pendingChildren: null,
      implementation: l.implementation
    }, t;
  }
  var ws = /* @__PURE__ */ new WeakMap();
  function Nt(l, t) {
    if (typeof l == "object" && l !== null) {
      var e = ws.get(l);
      return e !== void 0 ? e : (t = {
        value: l,
        source: t,
        stack: Jf(t)
      }, ws.set(l, t), t);
    }
    return {
      value: l,
      source: t,
      stack: Jf(t)
    };
  }
  var Tu = [], xu = 0, bn = null, ha = 0, Ot = [], Mt = 0, be = null, Xt = 1, Zt = "";
  function Ft(l, t) {
    Tu[xu++] = ha, Tu[xu++] = bn, bn = l, ha = t;
  }
  function ks(l, t, e) {
    Ot[Mt++] = Xt, Ot[Mt++] = Zt, Ot[Mt++] = be, be = l;
    var u = Xt;
    l = Zt;
    var a = 32 - mt(u) - 1;
    u &= ~(1 << a), e += 1;
    var n = 32 - mt(t) + a;
    if (30 < n) {
      var i = a - a % 5;
      n = (u & (1 << i) - 1).toString(32), u >>= i, a -= i, Xt = 1 << 32 - mt(t) + a | e << a | u, Zt = n + l;
    } else
      Xt = 1 << n | e << a | u, Zt = l;
  }
  function Wi(l) {
    l.return !== null && (Ft(l, 1), ks(l, 1, 0));
  }
  function Fi(l) {
    for (; l === bn; )
      bn = Tu[--xu], Tu[xu] = null, ha = Tu[--xu], Tu[xu] = null;
    for (; l === be; )
      be = Ot[--Mt], Ot[Mt] = null, Zt = Ot[--Mt], Ot[Mt] = null, Xt = Ot[--Mt], Ot[Mt] = null;
  }
  function $s(l, t) {
    Ot[Mt++] = Xt, Ot[Mt++] = Zt, Ot[Mt++] = be, Xt = t.id, Zt = t.overflow, be = l;
  }
  var kl = null, Al = null, rl = !1, Se = null, Dt = !1, Ii = Error(s(519));
  function pe(l) {
    var t = Error(
      s(
        418,
        1 < arguments.length && arguments[1] !== void 0 && arguments[1] ? "text" : "HTML",
        ""
      )
    );
    throw ma(Nt(t, l)), Ii;
  }
  function Ws(l) {
    var t = l.stateNode, e = l.type, u = l.memoizedProps;
    switch (t[wl] = l, t[ut] = u, e) {
      case "dialog":
        cl("cancel", t), cl("close", t);
        break;
      case "iframe":
      case "object":
      case "embed":
        cl("load", t);
        break;
      case "video":
      case "audio":
        for (e = 0; e < Ya.length; e++)
          cl(Ya[e], t);
        break;
      case "source":
        cl("error", t);
        break;
      case "img":
      case "image":
      case "link":
        cl("error", t), cl("load", t);
        break;
      case "details":
        cl("toggle", t);
        break;
      case "input":
        cl("invalid", t), ss(
          t,
          u.value,
          u.defaultValue,
          u.checked,
          u.defaultChecked,
          u.type,
          u.name,
          !0
        );
        break;
      case "select":
        cl("invalid", t);
        break;
      case "textarea":
        cl("invalid", t), os(t, u.value, u.defaultValue, u.children);
    }
    e = u.children, typeof e != "string" && typeof e != "number" && typeof e != "bigint" || t.textContent === "" + e || u.suppressHydrationWarning === !0 || vd(t.textContent, e) ? (u.popover != null && (cl("beforetoggle", t), cl("toggle", t)), u.onScroll != null && cl("scroll", t), u.onScrollEnd != null && cl("scrollend", t), u.onClick != null && (t.onclick = kt), t = !0) : t = !1, t || pe(l, !0);
  }
  function Fs(l) {
    for (kl = l.return; kl; )
      switch (kl.tag) {
        case 5:
        case 31:
        case 13:
          Dt = !1;
          return;
        case 27:
        case 3:
          Dt = !0;
          return;
        default:
          kl = kl.return;
      }
  }
  function Nu(l) {
    if (l !== kl) return !1;
    if (!rl) return Fs(l), rl = !0, !1;
    var t = l.tag, e;
    if ((e = t !== 3 && t !== 27) && ((e = t === 5) && (e = l.type, e = !(e !== "form" && e !== "button") || gf(l.type, l.memoizedProps)), e = !e), e && Al && pe(l), Fs(l), t === 13) {
      if (l = l.memoizedState, l = l !== null ? l.dehydrated : null, !l) throw Error(s(317));
      Al = zd(l);
    } else if (t === 31) {
      if (l = l.memoizedState, l = l !== null ? l.dehydrated : null, !l) throw Error(s(317));
      Al = zd(l);
    } else
      t === 27 ? (t = Al, je(l.type) ? (l = Ef, Ef = null, Al = l) : Al = t) : Al = kl ? Ct(l.stateNode.nextSibling) : null;
    return !0;
  }
  function Fe() {
    Al = kl = null, rl = !1;
  }
  function Pi() {
    var l = Se;
    return l !== null && (ft === null ? ft = l : ft.push.apply(
      ft,
      l
    ), Se = null), l;
  }
  function ma(l) {
    Se === null ? Se = [l] : Se.push(l);
  }
  var lc = v(null), Ie = null, It = null;
  function _e(l, t, e) {
    R(lc, t._currentValue), t._currentValue = e;
  }
  function Pt(l) {
    l._currentValue = lc.current, O(lc);
  }
  function tc(l, t, e) {
    for (; l !== null; ) {
      var u = l.alternate;
      if ((l.childLanes & t) !== t ? (l.childLanes |= t, u !== null && (u.childLanes |= t)) : u !== null && (u.childLanes & t) !== t && (u.childLanes |= t), l === e) break;
      l = l.return;
    }
  }
  function ec(l, t, e, u) {
    var a = l.child;
    for (a !== null && (a.return = l); a !== null; ) {
      var n = a.dependencies;
      if (n !== null) {
        var i = a.child;
        n = n.firstContext;
        l: for (; n !== null; ) {
          var f = n;
          n = a;
          for (var d = 0; d < t.length; d++)
            if (f.context === t[d]) {
              n.lanes |= e, f = n.alternate, f !== null && (f.lanes |= e), tc(
                n.return,
                e,
                l
              ), u || (i = null);
              break l;
            }
          n = f.next;
        }
      } else if (a.tag === 18) {
        if (i = a.return, i === null) throw Error(s(341));
        i.lanes |= e, n = i.alternate, n !== null && (n.lanes |= e), tc(i, e, l), i = null;
      } else i = a.child;
      if (i !== null) i.return = a;
      else
        for (i = a; i !== null; ) {
          if (i === l) {
            i = null;
            break;
          }
          if (a = i.sibling, a !== null) {
            a.return = i.return, i = a;
            break;
          }
          i = i.return;
        }
      a = i;
    }
  }
  function Ou(l, t, e, u) {
    l = null;
    for (var a = t, n = !1; a !== null; ) {
      if (!n) {
        if ((a.flags & 524288) !== 0) n = !0;
        else if ((a.flags & 262144) !== 0) break;
      }
      if (a.tag === 10) {
        var i = a.alternate;
        if (i === null) throw Error(s(387));
        if (i = i.memoizedProps, i !== null) {
          var f = a.type;
          gt(a.pendingProps.value, i.value) || (l !== null ? l.push(f) : l = [f]);
        }
      } else if (a === yl.current) {
        if (i = a.alternate, i === null) throw Error(s(387));
        i.memoizedState.memoizedState !== a.memoizedState.memoizedState && (l !== null ? l.push(Za) : l = [Za]);
      }
      a = a.return;
    }
    l !== null && ec(
      t,
      l,
      e,
      u
    ), t.flags |= 262144;
  }
  function Sn(l) {
    for (l = l.firstContext; l !== null; ) {
      if (!gt(
        l.context._currentValue,
        l.memoizedValue
      ))
        return !0;
      l = l.next;
    }
    return !1;
  }
  function Pe(l) {
    Ie = l, It = null, l = l.dependencies, l !== null && (l.firstContext = null);
  }
  function $l(l) {
    return Is(Ie, l);
  }
  function pn(l, t) {
    return Ie === null && Pe(l), Is(l, t);
  }
  function Is(l, t) {
    var e = t._currentValue;
    if (t = { context: t, memoizedValue: e, next: null }, It === null) {
      if (l === null) throw Error(s(308));
      It = t, l.dependencies = { lanes: 0, firstContext: t }, l.flags |= 524288;
    } else It = It.next = t;
    return e;
  }
  var mv = typeof AbortController < "u" ? AbortController : function() {
    var l = [], t = this.signal = {
      aborted: !1,
      addEventListener: function(e, u) {
        l.push(u);
      }
    };
    this.abort = function() {
      t.aborted = !0, l.forEach(function(e) {
        return e();
      });
    };
  }, gv = c.unstable_scheduleCallback, bv = c.unstable_NormalPriority, Bl = {
    $$typeof: W,
    Consumer: null,
    Provider: null,
    _currentValue: null,
    _currentValue2: null,
    _threadCount: 0
  };
  function uc() {
    return {
      controller: new mv(),
      data: /* @__PURE__ */ new Map(),
      refCount: 0
    };
  }
  function ga(l) {
    l.refCount--, l.refCount === 0 && gv(bv, function() {
      l.controller.abort();
    });
  }
  var ba = null, ac = 0, Mu = 0, Du = null;
  function Sv(l, t) {
    if (ba === null) {
      var e = ba = [];
      ac = 0, Mu = ff(), Du = {
        status: "pending",
        value: void 0,
        then: function(u) {
          e.push(u);
        }
      };
    }
    return ac++, t.then(Ps, Ps), t;
  }
  function Ps() {
    if (--ac === 0 && ba !== null) {
      Du !== null && (Du.status = "fulfilled");
      var l = ba;
      ba = null, Mu = 0, Du = null;
      for (var t = 0; t < l.length; t++) (0, l[t])();
    }
  }
  function pv(l, t) {
    var e = [], u = {
      status: "pending",
      value: null,
      reason: null,
      then: function(a) {
        e.push(a);
      }
    };
    return l.then(
      function() {
        u.status = "fulfilled", u.value = t;
        for (var a = 0; a < e.length; a++) (0, e[a])(t);
      },
      function(a) {
        for (u.status = "rejected", u.reason = a, a = 0; a < e.length; a++)
          (0, e[a])(void 0);
      }
    ), u;
  }
  var lr = q.S;
  q.S = function(l, t) {
    Yo = vt(), typeof t == "object" && t !== null && typeof t.then == "function" && Sv(l, t), lr !== null && lr(l, t);
  };
  var lu = v(null);
  function nc() {
    var l = lu.current;
    return l !== null ? l : ql.pooledCache;
  }
  function _n(l, t) {
    t === null ? R(lu, lu.current) : R(lu, t.pool);
  }
  function tr() {
    var l = nc();
    return l === null ? null : { parent: Bl._currentValue, pool: l };
  }
  var Uu = Error(s(460)), ic = Error(s(474)), En = Error(s(542)), zn = { then: function() {
  } };
  function er(l) {
    return l = l.status, l === "fulfilled" || l === "rejected";
  }
  function ur(l, t, e) {
    switch (e = l[e], e === void 0 ? l.push(t) : e !== t && (t.then(kt, kt), t = e), t.status) {
      case "fulfilled":
        return t.value;
      case "rejected":
        throw l = t.reason, nr(l), l;
      default:
        if (typeof t.status == "string") t.then(kt, kt);
        else {
          if (l = ql, l !== null && 100 < l.shellSuspendCounter)
            throw Error(s(482));
          l = t, l.status = "pending", l.then(
            function(u) {
              if (t.status === "pending") {
                var a = t;
                a.status = "fulfilled", a.value = u;
              }
            },
            function(u) {
              if (t.status === "pending") {
                var a = t;
                a.status = "rejected", a.reason = u;
              }
            }
          );
        }
        switch (t.status) {
          case "fulfilled":
            return t.value;
          case "rejected":
            throw l = t.reason, nr(l), l;
        }
        throw eu = t, Uu;
    }
  }
  function tu(l) {
    try {
      var t = l._init;
      return t(l._payload);
    } catch (e) {
      throw e !== null && typeof e == "object" && typeof e.then == "function" ? (eu = e, Uu) : e;
    }
  }
  var eu = null;
  function ar() {
    if (eu === null) throw Error(s(459));
    var l = eu;
    return eu = null, l;
  }
  function nr(l) {
    if (l === Uu || l === En)
      throw Error(s(483));
  }
  var Cu = null, Sa = 0;
  function qn(l) {
    var t = Sa;
    return Sa += 1, Cu === null && (Cu = []), ur(Cu, l, t);
  }
  function pa(l, t) {
    t = t.props.ref, l.ref = t !== void 0 ? t : null;
  }
  function An(l, t) {
    throw t.$$typeof === D ? Error(s(525)) : (l = Object.prototype.toString.call(t), Error(
      s(
        31,
        l === "[object Object]" ? "object with keys {" + Object.keys(t).join(", ") + "}" : l
      )
    ));
  }
  function ir(l) {
    function t(h, y) {
      if (l) {
        var m = h.deletions;
        m === null ? (h.deletions = [y], h.flags |= 16) : m.push(y);
      }
    }
    function e(h, y) {
      if (!l) return null;
      for (; y !== null; )
        t(h, y), y = y.sibling;
      return null;
    }
    function u(h) {
      for (var y = /* @__PURE__ */ new Map(); h !== null; )
        h.key !== null ? y.set(h.key, h) : y.set(h.index, h), h = h.sibling;
      return y;
    }
    function a(h, y) {
      return h = Wt(h, y), h.index = 0, h.sibling = null, h;
    }
    function n(h, y, m) {
      return h.index = m, l ? (m = h.alternate, m !== null ? (m = m.index, m < y ? (h.flags |= 67108866, y) : m) : (h.flags |= 67108866, y)) : (h.flags |= 1048576, y);
    }
    function i(h) {
      return l && h.alternate === null && (h.flags |= 67108866), h;
    }
    function f(h, y, m, A) {
      return y === null || y.tag !== 6 ? (y = ki(m, h.mode, A), y.return = h, y) : (y = a(y, m), y.return = h, y);
    }
    function d(h, y, m, A) {
      var V = m.type;
      return V === K ? z(
        h,
        y,
        m.props.children,
        A,
        m.key
      ) : y !== null && (y.elementType === V || typeof V == "object" && V !== null && V.$$typeof === Ql && tu(V) === y.type) ? (y = a(y, m.props), pa(y, m), y.return = h, y) : (y = gn(
        m.type,
        m.key,
        m.props,
        null,
        h.mode,
        A
      ), pa(y, m), y.return = h, y);
    }
    function g(h, y, m, A) {
      return y === null || y.tag !== 4 || y.stateNode.containerInfo !== m.containerInfo || y.stateNode.implementation !== m.implementation ? (y = $i(m, h.mode, A), y.return = h, y) : (y = a(y, m.children || []), y.return = h, y);
    }
    function z(h, y, m, A, V) {
      return y === null || y.tag !== 7 ? (y = We(
        m,
        h.mode,
        A,
        V
      ), y.return = h, y) : (y = a(y, m), y.return = h, y);
    }
    function M(h, y, m) {
      if (typeof y == "string" && y !== "" || typeof y == "number" || typeof y == "bigint")
        return y = ki(
          "" + y,
          h.mode,
          m
        ), y.return = h, y;
      if (typeof y == "object" && y !== null) {
        switch (y.$$typeof) {
          case X:
            return m = gn(
              y.type,
              y.key,
              y.props,
              null,
              h.mode,
              m
            ), pa(m, y), m.return = h, m;
          case k:
            return y = $i(
              y,
              h.mode,
              m
            ), y.return = h, y;
          case Ql:
            return y = tu(y), M(h, y, m);
        }
        if (yt(y) || Pl(y))
          return y = We(
            y,
            h.mode,
            m,
            null
          ), y.return = h, y;
        if (typeof y.then == "function")
          return M(h, qn(y), m);
        if (y.$$typeof === W)
          return M(
            h,
            pn(h, y),
            m
          );
        An(h, y);
      }
      return null;
    }
    function S(h, y, m, A) {
      var V = y !== null ? y.key : null;
      if (typeof m == "string" && m !== "" || typeof m == "number" || typeof m == "bigint")
        return V !== null ? null : f(h, y, "" + m, A);
      if (typeof m == "object" && m !== null) {
        switch (m.$$typeof) {
          case X:
            return m.key === V ? d(h, y, m, A) : null;
          case k:
            return m.key === V ? g(h, y, m, A) : null;
          case Ql:
            return m = tu(m), S(h, y, m, A);
        }
        if (yt(m) || Pl(m))
          return V !== null ? null : z(h, y, m, A, null);
        if (typeof m.then == "function")
          return S(
            h,
            y,
            qn(m),
            A
          );
        if (m.$$typeof === W)
          return S(
            h,
            y,
            pn(h, m),
            A
          );
        An(h, m);
      }
      return null;
    }
    function _(h, y, m, A, V) {
      if (typeof A == "string" && A !== "" || typeof A == "number" || typeof A == "bigint")
        return h = h.get(m) || null, f(y, h, "" + A, V);
      if (typeof A == "object" && A !== null) {
        switch (A.$$typeof) {
          case X:
            return h = h.get(
              A.key === null ? m : A.key
            ) || null, d(y, h, A, V);
          case k:
            return h = h.get(
              A.key === null ? m : A.key
            ) || null, g(y, h, A, V);
          case Ql:
            return A = tu(A), _(
              h,
              y,
              m,
              A,
              V
            );
        }
        if (yt(A) || Pl(A))
          return h = h.get(m) || null, z(y, h, A, V, null);
        if (typeof A.then == "function")
          return _(
            h,
            y,
            m,
            qn(A),
            V
          );
        if (A.$$typeof === W)
          return _(
            h,
            y,
            m,
            pn(y, A),
            V
          );
        An(y, A);
      }
      return null;
    }
    function Y(h, y, m, A) {
      for (var V = null, vl = null, Q = y, ul = y = 0, sl = null; Q !== null && ul < m.length; ul++) {
        Q.index > ul ? (sl = Q, Q = null) : sl = Q.sibling;
        var hl = S(
          h,
          Q,
          m[ul],
          A
        );
        if (hl === null) {
          Q === null && (Q = sl);
          break;
        }
        l && Q && hl.alternate === null && t(h, Q), y = n(hl, y, ul), vl === null ? V = hl : vl.sibling = hl, vl = hl, Q = sl;
      }
      if (ul === m.length)
        return e(h, Q), rl && Ft(h, ul), V;
      if (Q === null) {
        for (; ul < m.length; ul++)
          Q = M(h, m[ul], A), Q !== null && (y = n(
            Q,
            y,
            ul
          ), vl === null ? V = Q : vl.sibling = Q, vl = Q);
        return rl && Ft(h, ul), V;
      }
      for (Q = u(Q); ul < m.length; ul++)
        sl = _(
          Q,
          h,
          ul,
          m[ul],
          A
        ), sl !== null && (l && sl.alternate !== null && Q.delete(
          sl.key === null ? ul : sl.key
        ), y = n(
          sl,
          y,
          ul
        ), vl === null ? V = sl : vl.sibling = sl, vl = sl);
      return l && Q.forEach(function(Le) {
        return t(h, Le);
      }), rl && Ft(h, ul), V;
    }
    function w(h, y, m, A) {
      if (m == null) throw Error(s(151));
      for (var V = null, vl = null, Q = y, ul = y = 0, sl = null, hl = m.next(); Q !== null && !hl.done; ul++, hl = m.next()) {
        Q.index > ul ? (sl = Q, Q = null) : sl = Q.sibling;
        var Le = S(h, Q, hl.value, A);
        if (Le === null) {
          Q === null && (Q = sl);
          break;
        }
        l && Q && Le.alternate === null && t(h, Q), y = n(Le, y, ul), vl === null ? V = Le : vl.sibling = Le, vl = Le, Q = sl;
      }
      if (hl.done)
        return e(h, Q), rl && Ft(h, ul), V;
      if (Q === null) {
        for (; !hl.done; ul++, hl = m.next())
          hl = M(h, hl.value, A), hl !== null && (y = n(hl, y, ul), vl === null ? V = hl : vl.sibling = hl, vl = hl);
        return rl && Ft(h, ul), V;
      }
      for (Q = u(Q); !hl.done; ul++, hl = m.next())
        hl = _(Q, h, ul, hl.value, A), hl !== null && (l && hl.alternate !== null && Q.delete(hl.key === null ? ul : hl.key), y = n(hl, y, ul), vl === null ? V = hl : vl.sibling = hl, vl = hl);
      return l && Q.forEach(function(Dh) {
        return t(h, Dh);
      }), rl && Ft(h, ul), V;
    }
    function zl(h, y, m, A) {
      if (typeof m == "object" && m !== null && m.type === K && m.key === null && (m = m.props.children), typeof m == "object" && m !== null) {
        switch (m.$$typeof) {
          case X:
            l: {
              for (var V = m.key; y !== null; ) {
                if (y.key === V) {
                  if (V = m.type, V === K) {
                    if (y.tag === 7) {
                      e(
                        h,
                        y.sibling
                      ), A = a(
                        y,
                        m.props.children
                      ), A.return = h, h = A;
                      break l;
                    }
                  } else if (y.elementType === V || typeof V == "object" && V !== null && V.$$typeof === Ql && tu(V) === y.type) {
                    e(
                      h,
                      y.sibling
                    ), A = a(y, m.props), pa(A, m), A.return = h, h = A;
                    break l;
                  }
                  e(h, y);
                  break;
                } else t(h, y);
                y = y.sibling;
              }
              m.type === K ? (A = We(
                m.props.children,
                h.mode,
                A,
                m.key
              ), A.return = h, h = A) : (A = gn(
                m.type,
                m.key,
                m.props,
                null,
                h.mode,
                A
              ), pa(A, m), A.return = h, h = A);
            }
            return i(h);
          case k:
            l: {
              for (V = m.key; y !== null; ) {
                if (y.key === V)
                  if (y.tag === 4 && y.stateNode.containerInfo === m.containerInfo && y.stateNode.implementation === m.implementation) {
                    e(
                      h,
                      y.sibling
                    ), A = a(y, m.children || []), A.return = h, h = A;
                    break l;
                  } else {
                    e(h, y);
                    break;
                  }
                else t(h, y);
                y = y.sibling;
              }
              A = $i(m, h.mode, A), A.return = h, h = A;
            }
            return i(h);
          case Ql:
            return m = tu(m), zl(
              h,
              y,
              m,
              A
            );
        }
        if (yt(m))
          return Y(
            h,
            y,
            m,
            A
          );
        if (Pl(m)) {
          if (V = Pl(m), typeof V != "function") throw Error(s(150));
          return m = V.call(m), w(
            h,
            y,
            m,
            A
          );
        }
        if (typeof m.then == "function")
          return zl(
            h,
            y,
            qn(m),
            A
          );
        if (m.$$typeof === W)
          return zl(
            h,
            y,
            pn(h, m),
            A
          );
        An(h, m);
      }
      return typeof m == "string" && m !== "" || typeof m == "number" || typeof m == "bigint" ? (m = "" + m, y !== null && y.tag === 6 ? (e(h, y.sibling), A = a(y, m), A.return = h, h = A) : (e(h, y), A = ki(m, h.mode, A), A.return = h, h = A), i(h)) : e(h, y);
    }
    return function(h, y, m, A) {
      try {
        Sa = 0;
        var V = zl(
          h,
          y,
          m,
          A
        );
        return Cu = null, V;
      } catch (Q) {
        if (Q === Uu || Q === En) throw Q;
        var vl = bt(29, Q, null, h.mode);
        return vl.lanes = A, vl.return = h, vl;
      } finally {
      }
    };
  }
  var uu = ir(!0), cr = ir(!1), Ee = !1;
  function cc(l) {
    l.updateQueue = {
      baseState: l.memoizedState,
      firstBaseUpdate: null,
      lastBaseUpdate: null,
      shared: { pending: null, lanes: 0, hiddenCallbacks: null },
      callbacks: null
    };
  }
  function fc(l, t) {
    l = l.updateQueue, t.updateQueue === l && (t.updateQueue = {
      baseState: l.baseState,
      firstBaseUpdate: l.firstBaseUpdate,
      lastBaseUpdate: l.lastBaseUpdate,
      shared: l.shared,
      callbacks: null
    });
  }
  function ze(l) {
    return { lane: l, tag: 0, payload: null, callback: null, next: null };
  }
  function qe(l, t, e) {
    var u = l.updateQueue;
    if (u === null) return null;
    if (u = u.shared, (gl & 2) !== 0) {
      var a = u.pending;
      return a === null ? t.next = t : (t.next = a.next, a.next = t), u.pending = t, t = mn(l), Vs(l, null, e), t;
    }
    return hn(l, u, t, e), mn(l);
  }
  function _a(l, t, e) {
    if (t = t.updateQueue, t !== null && (t = t.shared, (e & 4194048) !== 0)) {
      var u = t.lanes;
      u &= l.pendingLanes, e |= u, t.lanes = e, If(l, e);
    }
  }
  function sc(l, t) {
    var e = l.updateQueue, u = l.alternate;
    if (u !== null && (u = u.updateQueue, e === u)) {
      var a = null, n = null;
      if (e = e.firstBaseUpdate, e !== null) {
        do {
          var i = {
            lane: e.lane,
            tag: e.tag,
            payload: e.payload,
            callback: null,
            next: null
          };
          n === null ? a = n = i : n = n.next = i, e = e.next;
        } while (e !== null);
        n === null ? a = n = t : n = n.next = t;
      } else a = n = t;
      e = {
        baseState: u.baseState,
        firstBaseUpdate: a,
        lastBaseUpdate: n,
        shared: u.shared,
        callbacks: u.callbacks
      }, l.updateQueue = e;
      return;
    }
    l = e.lastBaseUpdate, l === null ? e.firstBaseUpdate = t : l.next = t, e.lastBaseUpdate = t;
  }
  var rc = !1;
  function Ea() {
    if (rc) {
      var l = Du;
      if (l !== null) throw l;
    }
  }
  function za(l, t, e, u) {
    rc = !1;
    var a = l.updateQueue;
    Ee = !1;
    var n = a.firstBaseUpdate, i = a.lastBaseUpdate, f = a.shared.pending;
    if (f !== null) {
      a.shared.pending = null;
      var d = f, g = d.next;
      d.next = null, i === null ? n = g : i.next = g, i = d;
      var z = l.alternate;
      z !== null && (z = z.updateQueue, f = z.lastBaseUpdate, f !== i && (f === null ? z.firstBaseUpdate = g : f.next = g, z.lastBaseUpdate = d));
    }
    if (n !== null) {
      var M = a.baseState;
      i = 0, z = g = d = null, f = n;
      do {
        var S = f.lane & -536870913, _ = S !== f.lane;
        if (_ ? (fl & S) === S : (u & S) === S) {
          S !== 0 && S === Mu && (rc = !0), z !== null && (z = z.next = {
            lane: 0,
            tag: f.tag,
            payload: f.payload,
            callback: null,
            next: null
          });
          l: {
            var Y = l, w = f;
            S = t;
            var zl = e;
            switch (w.tag) {
              case 1:
                if (Y = w.payload, typeof Y == "function") {
                  M = Y.call(zl, M, S);
                  break l;
                }
                M = Y;
                break l;
              case 3:
                Y.flags = Y.flags & -65537 | 128;
              case 0:
                if (Y = w.payload, S = typeof Y == "function" ? Y.call(zl, M, S) : Y, S == null) break l;
                M = p({}, M, S);
                break l;
              case 2:
                Ee = !0;
            }
          }
          S = f.callback, S !== null && (l.flags |= 64, _ && (l.flags |= 8192), _ = a.callbacks, _ === null ? a.callbacks = [S] : _.push(S));
        } else
          _ = {
            lane: S,
            tag: f.tag,
            payload: f.payload,
            callback: f.callback,
            next: null
          }, z === null ? (g = z = _, d = M) : z = z.next = _, i |= S;
        if (f = f.next, f === null) {
          if (f = a.shared.pending, f === null)
            break;
          _ = f, f = _.next, _.next = null, a.lastBaseUpdate = _, a.shared.pending = null;
        }
      } while (!0);
      z === null && (d = M), a.baseState = d, a.firstBaseUpdate = g, a.lastBaseUpdate = z, n === null && (a.shared.lanes = 0), Oe |= i, l.lanes = i, l.memoizedState = M;
    }
  }
  function fr(l, t) {
    if (typeof l != "function")
      throw Error(s(191, l));
    l.call(t);
  }
  function sr(l, t) {
    var e = l.callbacks;
    if (e !== null)
      for (l.callbacks = null, l = 0; l < e.length; l++)
        fr(e[l], t);
  }
  var ju = v(null), Tn = v(0);
  function rr(l, t) {
    l = fe, R(Tn, l), R(ju, t), fe = l | t.baseLanes;
  }
  function oc() {
    R(Tn, fe), R(ju, ju.current);
  }
  function dc() {
    fe = Tn.current, O(ju), O(Tn);
  }
  var St = v(null), Ut = null;
  function Ae(l) {
    var t = l.alternate;
    R(Cl, Cl.current & 1), R(St, l), Ut === null && (t === null || ju.current !== null || t.memoizedState !== null) && (Ut = l);
  }
  function yc(l) {
    R(Cl, Cl.current), R(St, l), Ut === null && (Ut = l);
  }
  function or(l) {
    l.tag === 22 ? (R(Cl, Cl.current), R(St, l), Ut === null && (Ut = l)) : Te();
  }
  function Te() {
    R(Cl, Cl.current), R(St, St.current);
  }
  function pt(l) {
    O(St), Ut === l && (Ut = null), O(Cl);
  }
  var Cl = v(0);
  function xn(l) {
    for (var t = l; t !== null; ) {
      if (t.tag === 13) {
        var e = t.memoizedState;
        if (e !== null && (e = e.dehydrated, e === null || pf(e) || _f(e)))
          return t;
      } else if (t.tag === 19 && (t.memoizedProps.revealOrder === "forwards" || t.memoizedProps.revealOrder === "backwards" || t.memoizedProps.revealOrder === "unstable_legacy-backwards" || t.memoizedProps.revealOrder === "together")) {
        if ((t.flags & 128) !== 0) return t;
      } else if (t.child !== null) {
        t.child.return = t, t = t.child;
        continue;
      }
      if (t === l) break;
      for (; t.sibling === null; ) {
        if (t.return === null || t.return === l) return null;
        t = t.return;
      }
      t.sibling.return = t.return, t = t.sibling;
    }
    return null;
  }
  var le = 0, el = null, _l = null, Yl = null, Nn = !1, Ru = !1, au = !1, On = 0, qa = 0, Hu = null, _v = 0;
  function Ml() {
    throw Error(s(321));
  }
  function vc(l, t) {
    if (t === null) return !1;
    for (var e = 0; e < t.length && e < l.length; e++)
      if (!gt(l[e], t[e])) return !1;
    return !0;
  }
  function hc(l, t, e, u, a, n) {
    return le = n, el = t, t.memoizedState = null, t.updateQueue = null, t.lanes = 0, q.H = l === null || l.memoizedState === null ? kr : Mc, au = !1, n = e(u, a), au = !1, Ru && (n = yr(
      t,
      e,
      u,
      a
    )), dr(l), n;
  }
  function dr(l) {
    q.H = xa;
    var t = _l !== null && _l.next !== null;
    if (le = 0, Yl = _l = el = null, Nn = !1, qa = 0, Hu = null, t) throw Error(s(300));
    l === null || Ll || (l = l.dependencies, l !== null && Sn(l) && (Ll = !0));
  }
  function yr(l, t, e, u) {
    el = l;
    var a = 0;
    do {
      if (Ru && (Hu = null), qa = 0, Ru = !1, 25 <= a) throw Error(s(301));
      if (a += 1, Yl = _l = null, l.updateQueue != null) {
        var n = l.updateQueue;
        n.lastEffect = null, n.events = null, n.stores = null, n.memoCache != null && (n.memoCache.index = 0);
      }
      q.H = $r, n = t(e, u);
    } while (Ru);
    return n;
  }
  function Ev() {
    var l = q.H, t = l.useState()[0];
    return t = typeof t.then == "function" ? Aa(t) : t, l = l.useState()[0], (_l !== null ? _l.memoizedState : null) !== l && (el.flags |= 1024), t;
  }
  function mc() {
    var l = On !== 0;
    return On = 0, l;
  }
  function gc(l, t, e) {
    t.updateQueue = l.updateQueue, t.flags &= -2053, l.lanes &= ~e;
  }
  function bc(l) {
    if (Nn) {
      for (l = l.memoizedState; l !== null; ) {
        var t = l.queue;
        t !== null && (t.pending = null), l = l.next;
      }
      Nn = !1;
    }
    le = 0, Yl = _l = el = null, Ru = !1, qa = On = 0, Hu = null;
  }
  function tt() {
    var l = {
      memoizedState: null,
      baseState: null,
      baseQueue: null,
      queue: null,
      next: null
    };
    return Yl === null ? el.memoizedState = Yl = l : Yl = Yl.next = l, Yl;
  }
  function jl() {
    if (_l === null) {
      var l = el.alternate;
      l = l !== null ? l.memoizedState : null;
    } else l = _l.next;
    var t = Yl === null ? el.memoizedState : Yl.next;
    if (t !== null)
      Yl = t, _l = l;
    else {
      if (l === null)
        throw el.alternate === null ? Error(s(467)) : Error(s(310));
      _l = l, l = {
        memoizedState: _l.memoizedState,
        baseState: _l.baseState,
        baseQueue: _l.baseQueue,
        queue: _l.queue,
        next: null
      }, Yl === null ? el.memoizedState = Yl = l : Yl = Yl.next = l;
    }
    return Yl;
  }
  function Mn() {
    return { lastEffect: null, events: null, stores: null, memoCache: null };
  }
  function Aa(l) {
    var t = qa;
    return qa += 1, Hu === null && (Hu = []), l = ur(Hu, l, t), t = el, (Yl === null ? t.memoizedState : Yl.next) === null && (t = t.alternate, q.H = t === null || t.memoizedState === null ? kr : Mc), l;
  }
  function Dn(l) {
    if (l !== null && typeof l == "object") {
      if (typeof l.then == "function") return Aa(l);
      if (l.$$typeof === W) return $l(l);
    }
    throw Error(s(438, String(l)));
  }
  function Sc(l) {
    var t = null, e = el.updateQueue;
    if (e !== null && (t = e.memoCache), t == null) {
      var u = el.alternate;
      u !== null && (u = u.updateQueue, u !== null && (u = u.memoCache, u != null && (t = {
        data: u.data.map(function(a) {
          return a.slice();
        }),
        index: 0
      })));
    }
    if (t == null && (t = { data: [], index: 0 }), e === null && (e = Mn(), el.updateQueue = e), e.memoCache = t, e = t.data[t.index], e === void 0)
      for (e = t.data[t.index] = Array(l), u = 0; u < l; u++)
        e[u] = qt;
    return t.index++, e;
  }
  function te(l, t) {
    return typeof t == "function" ? t(l) : t;
  }
  function Un(l) {
    var t = jl();
    return pc(t, _l, l);
  }
  function pc(l, t, e) {
    var u = l.queue;
    if (u === null) throw Error(s(311));
    u.lastRenderedReducer = e;
    var a = l.baseQueue, n = u.pending;
    if (n !== null) {
      if (a !== null) {
        var i = a.next;
        a.next = n.next, n.next = i;
      }
      t.baseQueue = a = n, u.pending = null;
    }
    if (n = l.baseState, a === null) l.memoizedState = n;
    else {
      t = a.next;
      var f = i = null, d = null, g = t, z = !1;
      do {
        var M = g.lane & -536870913;
        if (M !== g.lane ? (fl & M) === M : (le & M) === M) {
          var S = g.revertLane;
          if (S === 0)
            d !== null && (d = d.next = {
              lane: 0,
              revertLane: 0,
              gesture: null,
              action: g.action,
              hasEagerState: g.hasEagerState,
              eagerState: g.eagerState,
              next: null
            }), M === Mu && (z = !0);
          else if ((le & S) === S) {
            g = g.next, S === Mu && (z = !0);
            continue;
          } else
            M = {
              lane: 0,
              revertLane: g.revertLane,
              gesture: null,
              action: g.action,
              hasEagerState: g.hasEagerState,
              eagerState: g.eagerState,
              next: null
            }, d === null ? (f = d = M, i = n) : d = d.next = M, el.lanes |= S, Oe |= S;
          M = g.action, au && e(n, M), n = g.hasEagerState ? g.eagerState : e(n, M);
        } else
          S = {
            lane: M,
            revertLane: g.revertLane,
            gesture: g.gesture,
            action: g.action,
            hasEagerState: g.hasEagerState,
            eagerState: g.eagerState,
            next: null
          }, d === null ? (f = d = S, i = n) : d = d.next = S, el.lanes |= M, Oe |= M;
        g = g.next;
      } while (g !== null && g !== t);
      if (d === null ? i = n : d.next = f, !gt(n, l.memoizedState) && (Ll = !0, z && (e = Du, e !== null)))
        throw e;
      l.memoizedState = n, l.baseState = i, l.baseQueue = d, u.lastRenderedState = n;
    }
    return a === null && (u.lanes = 0), [l.memoizedState, u.dispatch];
  }
  function _c(l) {
    var t = jl(), e = t.queue;
    if (e === null) throw Error(s(311));
    e.lastRenderedReducer = l;
    var u = e.dispatch, a = e.pending, n = t.memoizedState;
    if (a !== null) {
      e.pending = null;
      var i = a = a.next;
      do
        n = l(n, i.action), i = i.next;
      while (i !== a);
      gt(n, t.memoizedState) || (Ll = !0), t.memoizedState = n, t.baseQueue === null && (t.baseState = n), e.lastRenderedState = n;
    }
    return [n, u];
  }
  function vr(l, t, e) {
    var u = el, a = jl(), n = rl;
    if (n) {
      if (e === void 0) throw Error(s(407));
      e = e();
    } else e = t();
    var i = !gt(
      (_l || a).memoizedState,
      e
    );
    if (i && (a.memoizedState = e, Ll = !0), a = a.queue, qc(gr.bind(null, u, a, l), [
      l
    ]), a.getSnapshot !== t || i || Yl !== null && Yl.memoizedState.tag & 1) {
      if (u.flags |= 2048, Bu(
        9,
        { destroy: void 0 },
        mr.bind(
          null,
          u,
          a,
          e,
          t
        ),
        null
      ), ql === null) throw Error(s(349));
      n || (le & 127) !== 0 || hr(u, t, e);
    }
    return e;
  }
  function hr(l, t, e) {
    l.flags |= 16384, l = { getSnapshot: t, value: e }, t = el.updateQueue, t === null ? (t = Mn(), el.updateQueue = t, t.stores = [l]) : (e = t.stores, e === null ? t.stores = [l] : e.push(l));
  }
  function mr(l, t, e, u) {
    t.value = e, t.getSnapshot = u, br(t) && Sr(l);
  }
  function gr(l, t, e) {
    return e(function() {
      br(t) && Sr(l);
    });
  }
  function br(l) {
    var t = l.getSnapshot;
    l = l.value;
    try {
      var e = t();
      return !gt(l, e);
    } catch {
      return !0;
    }
  }
  function Sr(l) {
    var t = $e(l, 2);
    t !== null && st(t, l, 2);
  }
  function Ec(l) {
    var t = tt();
    if (typeof l == "function") {
      var e = l;
      if (l = e(), au) {
        he(!0);
        try {
          e();
        } finally {
          he(!1);
        }
      }
    }
    return t.memoizedState = t.baseState = l, t.queue = {
      pending: null,
      lanes: 0,
      dispatch: null,
      lastRenderedReducer: te,
      lastRenderedState: l
    }, t;
  }
  function pr(l, t, e, u) {
    return l.baseState = e, pc(
      l,
      _l,
      typeof u == "function" ? u : te
    );
  }
  function zv(l, t, e, u, a) {
    if (Rn(l)) throw Error(s(485));
    if (l = t.action, l !== null) {
      var n = {
        payload: a,
        action: l,
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
      q.T !== null ? e(!0) : n.isTransition = !1, u(n), e = t.pending, e === null ? (n.next = t.pending = n, _r(t, n)) : (n.next = e.next, t.pending = e.next = n);
    }
  }
  function _r(l, t) {
    var e = t.action, u = t.payload, a = l.state;
    if (t.isTransition) {
      var n = q.T, i = {};
      q.T = i;
      try {
        var f = e(a, u), d = q.S;
        d !== null && d(i, f), Er(l, t, f);
      } catch (g) {
        zc(l, t, g);
      } finally {
        n !== null && i.types !== null && (n.types = i.types), q.T = n;
      }
    } else
      try {
        n = e(a, u), Er(l, t, n);
      } catch (g) {
        zc(l, t, g);
      }
  }
  function Er(l, t, e) {
    e !== null && typeof e == "object" && typeof e.then == "function" ? e.then(
      function(u) {
        zr(l, t, u);
      },
      function(u) {
        return zc(l, t, u);
      }
    ) : zr(l, t, e);
  }
  function zr(l, t, e) {
    t.status = "fulfilled", t.value = e, qr(t), l.state = e, t = l.pending, t !== null && (e = t.next, e === t ? l.pending = null : (e = e.next, t.next = e, _r(l, e)));
  }
  function zc(l, t, e) {
    var u = l.pending;
    if (l.pending = null, u !== null) {
      u = u.next;
      do
        t.status = "rejected", t.reason = e, qr(t), t = t.next;
      while (t !== u);
    }
    l.action = null;
  }
  function qr(l) {
    l = l.listeners;
    for (var t = 0; t < l.length; t++) (0, l[t])();
  }
  function Ar(l, t) {
    return t;
  }
  function Tr(l, t) {
    if (rl) {
      var e = ql.formState;
      if (e !== null) {
        l: {
          var u = el;
          if (rl) {
            if (Al) {
              t: {
                for (var a = Al, n = Dt; a.nodeType !== 8; ) {
                  if (!n) {
                    a = null;
                    break t;
                  }
                  if (a = Ct(
                    a.nextSibling
                  ), a === null) {
                    a = null;
                    break t;
                  }
                }
                n = a.data, a = n === "F!" || n === "F" ? a : null;
              }
              if (a) {
                Al = Ct(
                  a.nextSibling
                ), u = a.data === "F!";
                break l;
              }
            }
            pe(u);
          }
          u = !1;
        }
        u && (t = e[0]);
      }
    }
    return e = tt(), e.memoizedState = e.baseState = t, u = {
      pending: null,
      lanes: 0,
      dispatch: null,
      lastRenderedReducer: Ar,
      lastRenderedState: t
    }, e.queue = u, e = Kr.bind(
      null,
      el,
      u
    ), u.dispatch = e, u = Ec(!1), n = Oc.bind(
      null,
      el,
      !1,
      u.queue
    ), u = tt(), a = {
      state: t,
      dispatch: null,
      action: l,
      pending: null
    }, u.queue = a, e = zv.bind(
      null,
      el,
      a,
      n,
      e
    ), a.dispatch = e, u.memoizedState = l, [t, e, !1];
  }
  function xr(l) {
    var t = jl();
    return Nr(t, _l, l);
  }
  function Nr(l, t, e) {
    if (t = pc(
      l,
      t,
      Ar
    )[0], l = Un(te)[0], typeof t == "object" && t !== null && typeof t.then == "function")
      try {
        var u = Aa(t);
      } catch (i) {
        throw i === Uu ? En : i;
      }
    else u = t;
    t = jl();
    var a = t.queue, n = a.dispatch;
    return e !== t.memoizedState && (el.flags |= 2048, Bu(
      9,
      { destroy: void 0 },
      qv.bind(null, a, e),
      null
    )), [u, n, l];
  }
  function qv(l, t) {
    l.action = t;
  }
  function Or(l) {
    var t = jl(), e = _l;
    if (e !== null)
      return Nr(t, e, l);
    jl(), t = t.memoizedState, e = jl();
    var u = e.queue.dispatch;
    return e.memoizedState = l, [t, u, !1];
  }
  function Bu(l, t, e, u) {
    return l = { tag: l, create: e, deps: u, inst: t, next: null }, t = el.updateQueue, t === null && (t = Mn(), el.updateQueue = t), e = t.lastEffect, e === null ? t.lastEffect = l.next = l : (u = e.next, e.next = l, l.next = u, t.lastEffect = l), l;
  }
  function Mr() {
    return jl().memoizedState;
  }
  function Cn(l, t, e, u) {
    var a = tt();
    el.flags |= l, a.memoizedState = Bu(
      1 | t,
      { destroy: void 0 },
      e,
      u === void 0 ? null : u
    );
  }
  function jn(l, t, e, u) {
    var a = jl();
    u = u === void 0 ? null : u;
    var n = a.memoizedState.inst;
    _l !== null && u !== null && vc(u, _l.memoizedState.deps) ? a.memoizedState = Bu(t, n, e, u) : (el.flags |= l, a.memoizedState = Bu(
      1 | t,
      n,
      e,
      u
    ));
  }
  function Dr(l, t) {
    Cn(8390656, 8, l, t);
  }
  function qc(l, t) {
    jn(2048, 8, l, t);
  }
  function Av(l) {
    el.flags |= 4;
    var t = el.updateQueue;
    if (t === null)
      t = Mn(), el.updateQueue = t, t.events = [l];
    else {
      var e = t.events;
      e === null ? t.events = [l] : e.push(l);
    }
  }
  function Ur(l) {
    var t = jl().memoizedState;
    return Av({ ref: t, nextImpl: l }), function() {
      if ((gl & 2) !== 0) throw Error(s(440));
      return t.impl.apply(void 0, arguments);
    };
  }
  function Cr(l, t) {
    return jn(4, 2, l, t);
  }
  function jr(l, t) {
    return jn(4, 4, l, t);
  }
  function Rr(l, t) {
    if (typeof t == "function") {
      l = l();
      var e = t(l);
      return function() {
        typeof e == "function" ? e() : t(null);
      };
    }
    if (t != null)
      return l = l(), t.current = l, function() {
        t.current = null;
      };
  }
  function Hr(l, t, e) {
    e = e != null ? e.concat([l]) : null, jn(4, 4, Rr.bind(null, t, l), e);
  }
  function Ac() {
  }
  function Br(l, t) {
    var e = jl();
    t = t === void 0 ? null : t;
    var u = e.memoizedState;
    return t !== null && vc(t, u[1]) ? u[0] : (e.memoizedState = [l, t], l);
  }
  function Yr(l, t) {
    var e = jl();
    t = t === void 0 ? null : t;
    var u = e.memoizedState;
    if (t !== null && vc(t, u[1]))
      return u[0];
    if (u = l(), au) {
      he(!0);
      try {
        l();
      } finally {
        he(!1);
      }
    }
    return e.memoizedState = [u, t], u;
  }
  function Tc(l, t, e) {
    return e === void 0 || (le & 1073741824) !== 0 && (fl & 261930) === 0 ? l.memoizedState = t : (l.memoizedState = e, l = Go(), el.lanes |= l, Oe |= l, e);
  }
  function Lr(l, t, e, u) {
    return gt(e, t) ? e : ju.current !== null ? (l = Tc(l, e, u), gt(l, t) || (Ll = !0), l) : (le & 42) === 0 || (le & 1073741824) !== 0 && (fl & 261930) === 0 ? (Ll = !0, l.memoizedState = e) : (l = Go(), el.lanes |= l, Oe |= l, t);
  }
  function Gr(l, t, e, u, a) {
    var n = j.p;
    j.p = n !== 0 && 8 > n ? n : 8;
    var i = q.T, f = {};
    q.T = f, Oc(l, !1, t, e);
    try {
      var d = a(), g = q.S;
      if (g !== null && g(f, d), d !== null && typeof d == "object" && typeof d.then == "function") {
        var z = pv(
          d,
          u
        );
        Ta(
          l,
          t,
          z,
          zt(l)
        );
      } else
        Ta(
          l,
          t,
          u,
          zt(l)
        );
    } catch (M) {
      Ta(
        l,
        t,
        { then: function() {
        }, status: "rejected", reason: M },
        zt()
      );
    } finally {
      j.p = n, i !== null && f.types !== null && (i.types = f.types), q.T = i;
    }
  }
  function Tv() {
  }
  function xc(l, t, e, u) {
    if (l.tag !== 5) throw Error(s(476));
    var a = Qr(l).queue;
    Gr(
      l,
      a,
      t,
      B,
      e === null ? Tv : function() {
        return Xr(l), e(u);
      }
    );
  }
  function Qr(l) {
    var t = l.memoizedState;
    if (t !== null) return t;
    t = {
      memoizedState: B,
      baseState: B,
      baseQueue: null,
      queue: {
        pending: null,
        lanes: 0,
        dispatch: null,
        lastRenderedReducer: te,
        lastRenderedState: B
      },
      next: null
    };
    var e = {};
    return t.next = {
      memoizedState: e,
      baseState: e,
      baseQueue: null,
      queue: {
        pending: null,
        lanes: 0,
        dispatch: null,
        lastRenderedReducer: te,
        lastRenderedState: e
      },
      next: null
    }, l.memoizedState = t, l = l.alternate, l !== null && (l.memoizedState = t), t;
  }
  function Xr(l) {
    var t = Qr(l);
    t.next === null && (t = l.alternate.memoizedState), Ta(
      l,
      t.next.queue,
      {},
      zt()
    );
  }
  function Nc() {
    return $l(Za);
  }
  function Zr() {
    return jl().memoizedState;
  }
  function Vr() {
    return jl().memoizedState;
  }
  function xv(l) {
    for (var t = l.return; t !== null; ) {
      switch (t.tag) {
        case 24:
        case 3:
          var e = zt();
          l = ze(e);
          var u = qe(t, l, e);
          u !== null && (st(u, t, e), _a(u, t, e)), t = { cache: uc() }, l.payload = t;
          return;
      }
      t = t.return;
    }
  }
  function Nv(l, t, e) {
    var u = zt();
    e = {
      lane: u,
      revertLane: 0,
      gesture: null,
      action: e,
      hasEagerState: !1,
      eagerState: null,
      next: null
    }, Rn(l) ? Jr(t, e) : (e = Ji(l, t, e, u), e !== null && (st(e, l, u), wr(e, t, u)));
  }
  function Kr(l, t, e) {
    var u = zt();
    Ta(l, t, e, u);
  }
  function Ta(l, t, e, u) {
    var a = {
      lane: u,
      revertLane: 0,
      gesture: null,
      action: e,
      hasEagerState: !1,
      eagerState: null,
      next: null
    };
    if (Rn(l)) Jr(t, a);
    else {
      var n = l.alternate;
      if (l.lanes === 0 && (n === null || n.lanes === 0) && (n = t.lastRenderedReducer, n !== null))
        try {
          var i = t.lastRenderedState, f = n(i, e);
          if (a.hasEagerState = !0, a.eagerState = f, gt(f, i))
            return hn(l, t, a, 0), ql === null && vn(), !1;
        } catch {
        } finally {
        }
      if (e = Ji(l, t, a, u), e !== null)
        return st(e, l, u), wr(e, t, u), !0;
    }
    return !1;
  }
  function Oc(l, t, e, u) {
    if (u = {
      lane: 2,
      revertLane: ff(),
      gesture: null,
      action: u,
      hasEagerState: !1,
      eagerState: null,
      next: null
    }, Rn(l)) {
      if (t) throw Error(s(479));
    } else
      t = Ji(
        l,
        e,
        u,
        2
      ), t !== null && st(t, l, 2);
  }
  function Rn(l) {
    var t = l.alternate;
    return l === el || t !== null && t === el;
  }
  function Jr(l, t) {
    Ru = Nn = !0;
    var e = l.pending;
    e === null ? t.next = t : (t.next = e.next, e.next = t), l.pending = t;
  }
  function wr(l, t, e) {
    if ((e & 4194048) !== 0) {
      var u = t.lanes;
      u &= l.pendingLanes, e |= u, t.lanes = e, If(l, e);
    }
  }
  var xa = {
    readContext: $l,
    use: Dn,
    useCallback: Ml,
    useContext: Ml,
    useEffect: Ml,
    useImperativeHandle: Ml,
    useLayoutEffect: Ml,
    useInsertionEffect: Ml,
    useMemo: Ml,
    useReducer: Ml,
    useRef: Ml,
    useState: Ml,
    useDebugValue: Ml,
    useDeferredValue: Ml,
    useTransition: Ml,
    useSyncExternalStore: Ml,
    useId: Ml,
    useHostTransitionStatus: Ml,
    useFormState: Ml,
    useActionState: Ml,
    useOptimistic: Ml,
    useMemoCache: Ml,
    useCacheRefresh: Ml
  };
  xa.useEffectEvent = Ml;
  var kr = {
    readContext: $l,
    use: Dn,
    useCallback: function(l, t) {
      return tt().memoizedState = [
        l,
        t === void 0 ? null : t
      ], l;
    },
    useContext: $l,
    useEffect: Dr,
    useImperativeHandle: function(l, t, e) {
      e = e != null ? e.concat([l]) : null, Cn(
        4194308,
        4,
        Rr.bind(null, t, l),
        e
      );
    },
    useLayoutEffect: function(l, t) {
      return Cn(4194308, 4, l, t);
    },
    useInsertionEffect: function(l, t) {
      Cn(4, 2, l, t);
    },
    useMemo: function(l, t) {
      var e = tt();
      t = t === void 0 ? null : t;
      var u = l();
      if (au) {
        he(!0);
        try {
          l();
        } finally {
          he(!1);
        }
      }
      return e.memoizedState = [u, t], u;
    },
    useReducer: function(l, t, e) {
      var u = tt();
      if (e !== void 0) {
        var a = e(t);
        if (au) {
          he(!0);
          try {
            e(t);
          } finally {
            he(!1);
          }
        }
      } else a = t;
      return u.memoizedState = u.baseState = a, l = {
        pending: null,
        lanes: 0,
        dispatch: null,
        lastRenderedReducer: l,
        lastRenderedState: a
      }, u.queue = l, l = l.dispatch = Nv.bind(
        null,
        el,
        l
      ), [u.memoizedState, l];
    },
    useRef: function(l) {
      var t = tt();
      return l = { current: l }, t.memoizedState = l;
    },
    useState: function(l) {
      l = Ec(l);
      var t = l.queue, e = Kr.bind(null, el, t);
      return t.dispatch = e, [l.memoizedState, e];
    },
    useDebugValue: Ac,
    useDeferredValue: function(l, t) {
      var e = tt();
      return Tc(e, l, t);
    },
    useTransition: function() {
      var l = Ec(!1);
      return l = Gr.bind(
        null,
        el,
        l.queue,
        !0,
        !1
      ), tt().memoizedState = l, [!1, l];
    },
    useSyncExternalStore: function(l, t, e) {
      var u = el, a = tt();
      if (rl) {
        if (e === void 0)
          throw Error(s(407));
        e = e();
      } else {
        if (e = t(), ql === null)
          throw Error(s(349));
        (fl & 127) !== 0 || hr(u, t, e);
      }
      a.memoizedState = e;
      var n = { value: e, getSnapshot: t };
      return a.queue = n, Dr(gr.bind(null, u, n, l), [
        l
      ]), u.flags |= 2048, Bu(
        9,
        { destroy: void 0 },
        mr.bind(
          null,
          u,
          n,
          e,
          t
        ),
        null
      ), e;
    },
    useId: function() {
      var l = tt(), t = ql.identifierPrefix;
      if (rl) {
        var e = Zt, u = Xt;
        e = (u & ~(1 << 32 - mt(u) - 1)).toString(32) + e, t = "_" + t + "R_" + e, e = On++, 0 < e && (t += "H" + e.toString(32)), t += "_";
      } else
        e = _v++, t = "_" + t + "r_" + e.toString(32) + "_";
      return l.memoizedState = t;
    },
    useHostTransitionStatus: Nc,
    useFormState: Tr,
    useActionState: Tr,
    useOptimistic: function(l) {
      var t = tt();
      t.memoizedState = t.baseState = l;
      var e = {
        pending: null,
        lanes: 0,
        dispatch: null,
        lastRenderedReducer: null,
        lastRenderedState: null
      };
      return t.queue = e, t = Oc.bind(
        null,
        el,
        !0,
        e
      ), e.dispatch = t, [l, t];
    },
    useMemoCache: Sc,
    useCacheRefresh: function() {
      return tt().memoizedState = xv.bind(
        null,
        el
      );
    },
    useEffectEvent: function(l) {
      var t = tt(), e = { impl: l };
      return t.memoizedState = e, function() {
        if ((gl & 2) !== 0)
          throw Error(s(440));
        return e.impl.apply(void 0, arguments);
      };
    }
  }, Mc = {
    readContext: $l,
    use: Dn,
    useCallback: Br,
    useContext: $l,
    useEffect: qc,
    useImperativeHandle: Hr,
    useInsertionEffect: Cr,
    useLayoutEffect: jr,
    useMemo: Yr,
    useReducer: Un,
    useRef: Mr,
    useState: function() {
      return Un(te);
    },
    useDebugValue: Ac,
    useDeferredValue: function(l, t) {
      var e = jl();
      return Lr(
        e,
        _l.memoizedState,
        l,
        t
      );
    },
    useTransition: function() {
      var l = Un(te)[0], t = jl().memoizedState;
      return [
        typeof l == "boolean" ? l : Aa(l),
        t
      ];
    },
    useSyncExternalStore: vr,
    useId: Zr,
    useHostTransitionStatus: Nc,
    useFormState: xr,
    useActionState: xr,
    useOptimistic: function(l, t) {
      var e = jl();
      return pr(e, _l, l, t);
    },
    useMemoCache: Sc,
    useCacheRefresh: Vr
  };
  Mc.useEffectEvent = Ur;
  var $r = {
    readContext: $l,
    use: Dn,
    useCallback: Br,
    useContext: $l,
    useEffect: qc,
    useImperativeHandle: Hr,
    useInsertionEffect: Cr,
    useLayoutEffect: jr,
    useMemo: Yr,
    useReducer: _c,
    useRef: Mr,
    useState: function() {
      return _c(te);
    },
    useDebugValue: Ac,
    useDeferredValue: function(l, t) {
      var e = jl();
      return _l === null ? Tc(e, l, t) : Lr(
        e,
        _l.memoizedState,
        l,
        t
      );
    },
    useTransition: function() {
      var l = _c(te)[0], t = jl().memoizedState;
      return [
        typeof l == "boolean" ? l : Aa(l),
        t
      ];
    },
    useSyncExternalStore: vr,
    useId: Zr,
    useHostTransitionStatus: Nc,
    useFormState: Or,
    useActionState: Or,
    useOptimistic: function(l, t) {
      var e = jl();
      return _l !== null ? pr(e, _l, l, t) : (e.baseState = l, [l, e.queue.dispatch]);
    },
    useMemoCache: Sc,
    useCacheRefresh: Vr
  };
  $r.useEffectEvent = Ur;
  function Dc(l, t, e, u) {
    t = l.memoizedState, e = e(u, t), e = e == null ? t : p({}, t, e), l.memoizedState = e, l.lanes === 0 && (l.updateQueue.baseState = e);
  }
  var Uc = {
    enqueueSetState: function(l, t, e) {
      l = l._reactInternals;
      var u = zt(), a = ze(u);
      a.payload = t, e != null && (a.callback = e), t = qe(l, a, u), t !== null && (st(t, l, u), _a(t, l, u));
    },
    enqueueReplaceState: function(l, t, e) {
      l = l._reactInternals;
      var u = zt(), a = ze(u);
      a.tag = 1, a.payload = t, e != null && (a.callback = e), t = qe(l, a, u), t !== null && (st(t, l, u), _a(t, l, u));
    },
    enqueueForceUpdate: function(l, t) {
      l = l._reactInternals;
      var e = zt(), u = ze(e);
      u.tag = 2, t != null && (u.callback = t), t = qe(l, u, e), t !== null && (st(t, l, e), _a(t, l, e));
    }
  };
  function Wr(l, t, e, u, a, n, i) {
    return l = l.stateNode, typeof l.shouldComponentUpdate == "function" ? l.shouldComponentUpdate(u, n, i) : t.prototype && t.prototype.isPureReactComponent ? !ya(e, u) || !ya(a, n) : !0;
  }
  function Fr(l, t, e, u) {
    l = t.state, typeof t.componentWillReceiveProps == "function" && t.componentWillReceiveProps(e, u), typeof t.UNSAFE_componentWillReceiveProps == "function" && t.UNSAFE_componentWillReceiveProps(e, u), t.state !== l && Uc.enqueueReplaceState(t, t.state, null);
  }
  function nu(l, t) {
    var e = t;
    if ("ref" in t) {
      e = {};
      for (var u in t)
        u !== "ref" && (e[u] = t[u]);
    }
    if (l = l.defaultProps) {
      e === t && (e = p({}, e));
      for (var a in l)
        e[a] === void 0 && (e[a] = l[a]);
    }
    return e;
  }
  function Ir(l) {
    yn(l);
  }
  function Pr(l) {
    console.error(l);
  }
  function lo(l) {
    yn(l);
  }
  function Hn(l, t) {
    try {
      var e = l.onUncaughtError;
      e(t.value, { componentStack: t.stack });
    } catch (u) {
      setTimeout(function() {
        throw u;
      });
    }
  }
  function to(l, t, e) {
    try {
      var u = l.onCaughtError;
      u(e.value, {
        componentStack: e.stack,
        errorBoundary: t.tag === 1 ? t.stateNode : null
      });
    } catch (a) {
      setTimeout(function() {
        throw a;
      });
    }
  }
  function Cc(l, t, e) {
    return e = ze(e), e.tag = 3, e.payload = { element: null }, e.callback = function() {
      Hn(l, t);
    }, e;
  }
  function eo(l) {
    return l = ze(l), l.tag = 3, l;
  }
  function uo(l, t, e, u) {
    var a = e.type.getDerivedStateFromError;
    if (typeof a == "function") {
      var n = u.value;
      l.payload = function() {
        return a(n);
      }, l.callback = function() {
        to(t, e, u);
      };
    }
    var i = e.stateNode;
    i !== null && typeof i.componentDidCatch == "function" && (l.callback = function() {
      to(t, e, u), typeof a != "function" && (Me === null ? Me = /* @__PURE__ */ new Set([this]) : Me.add(this));
      var f = u.stack;
      this.componentDidCatch(u.value, {
        componentStack: f !== null ? f : ""
      });
    });
  }
  function Ov(l, t, e, u, a) {
    if (e.flags |= 32768, u !== null && typeof u == "object" && typeof u.then == "function") {
      if (t = e.alternate, t !== null && Ou(
        t,
        e,
        a,
        !0
      ), e = St.current, e !== null) {
        switch (e.tag) {
          case 31:
          case 13:
            return Ut === null ? kn() : e.alternate === null && Dl === 0 && (Dl = 3), e.flags &= -257, e.flags |= 65536, e.lanes = a, u === zn ? e.flags |= 16384 : (t = e.updateQueue, t === null ? e.updateQueue = /* @__PURE__ */ new Set([u]) : t.add(u), af(l, u, a)), !1;
          case 22:
            return e.flags |= 65536, u === zn ? e.flags |= 16384 : (t = e.updateQueue, t === null ? (t = {
              transitions: null,
              markerInstances: null,
              retryQueue: /* @__PURE__ */ new Set([u])
            }, e.updateQueue = t) : (e = t.retryQueue, e === null ? t.retryQueue = /* @__PURE__ */ new Set([u]) : e.add(u)), af(l, u, a)), !1;
        }
        throw Error(s(435, e.tag));
      }
      return af(l, u, a), kn(), !1;
    }
    if (rl)
      return t = St.current, t !== null ? ((t.flags & 65536) === 0 && (t.flags |= 256), t.flags |= 65536, t.lanes = a, u !== Ii && (l = Error(s(422), { cause: u }), ma(Nt(l, e)))) : (u !== Ii && (t = Error(s(423), {
        cause: u
      }), ma(
        Nt(t, e)
      )), l = l.current.alternate, l.flags |= 65536, a &= -a, l.lanes |= a, u = Nt(u, e), a = Cc(
        l.stateNode,
        u,
        a
      ), sc(l, a), Dl !== 4 && (Dl = 2)), !1;
    var n = Error(s(520), { cause: u });
    if (n = Nt(n, e), Ra === null ? Ra = [n] : Ra.push(n), Dl !== 4 && (Dl = 2), t === null) return !0;
    u = Nt(u, e), e = t;
    do {
      switch (e.tag) {
        case 3:
          return e.flags |= 65536, l = a & -a, e.lanes |= l, l = Cc(e.stateNode, u, l), sc(e, l), !1;
        case 1:
          if (t = e.type, n = e.stateNode, (e.flags & 128) === 0 && (typeof t.getDerivedStateFromError == "function" || n !== null && typeof n.componentDidCatch == "function" && (Me === null || !Me.has(n))))
            return e.flags |= 65536, a &= -a, e.lanes |= a, a = eo(a), uo(
              a,
              l,
              e,
              u
            ), sc(e, a), !1;
      }
      e = e.return;
    } while (e !== null);
    return !1;
  }
  var jc = Error(s(461)), Ll = !1;
  function Wl(l, t, e, u) {
    t.child = l === null ? cr(t, null, e, u) : uu(
      t,
      l.child,
      e,
      u
    );
  }
  function ao(l, t, e, u, a) {
    e = e.render;
    var n = t.ref;
    if ("ref" in u) {
      var i = {};
      for (var f in u)
        f !== "ref" && (i[f] = u[f]);
    } else i = u;
    return Pe(t), u = hc(
      l,
      t,
      e,
      i,
      n,
      a
    ), f = mc(), l !== null && !Ll ? (gc(l, t, a), ee(l, t, a)) : (rl && f && Wi(t), t.flags |= 1, Wl(l, t, u, a), t.child);
  }
  function no(l, t, e, u, a) {
    if (l === null) {
      var n = e.type;
      return typeof n == "function" && !wi(n) && n.defaultProps === void 0 && e.compare === null ? (t.tag = 15, t.type = n, io(
        l,
        t,
        n,
        u,
        a
      )) : (l = gn(
        e.type,
        null,
        u,
        t,
        t.mode,
        a
      ), l.ref = t.ref, l.return = t, t.child = l);
    }
    if (n = l.child, !Xc(l, a)) {
      var i = n.memoizedProps;
      if (e = e.compare, e = e !== null ? e : ya, e(i, u) && l.ref === t.ref)
        return ee(l, t, a);
    }
    return t.flags |= 1, l = Wt(n, u), l.ref = t.ref, l.return = t, t.child = l;
  }
  function io(l, t, e, u, a) {
    if (l !== null) {
      var n = l.memoizedProps;
      if (ya(n, u) && l.ref === t.ref)
        if (Ll = !1, t.pendingProps = u = n, Xc(l, a))
          (l.flags & 131072) !== 0 && (Ll = !0);
        else
          return t.lanes = l.lanes, ee(l, t, a);
    }
    return Rc(
      l,
      t,
      e,
      u,
      a
    );
  }
  function co(l, t, e, u) {
    var a = u.children, n = l !== null ? l.memoizedState : null;
    if (l === null && t.stateNode === null && (t.stateNode = {
      _visibility: 1,
      _pendingMarkers: null,
      _retryCache: null,
      _transitions: null
    }), u.mode === "hidden") {
      if ((t.flags & 128) !== 0) {
        if (n = n !== null ? n.baseLanes | e : e, l !== null) {
          for (u = t.child = l.child, a = 0; u !== null; )
            a = a | u.lanes | u.childLanes, u = u.sibling;
          u = a & ~n;
        } else u = 0, t.child = null;
        return fo(
          l,
          t,
          n,
          e,
          u
        );
      }
      if ((e & 536870912) !== 0)
        t.memoizedState = { baseLanes: 0, cachePool: null }, l !== null && _n(
          t,
          n !== null ? n.cachePool : null
        ), n !== null ? rr(t, n) : oc(), or(t);
      else
        return u = t.lanes = 536870912, fo(
          l,
          t,
          n !== null ? n.baseLanes | e : e,
          e,
          u
        );
    } else
      n !== null ? (_n(t, n.cachePool), rr(t, n), Te(), t.memoizedState = null) : (l !== null && _n(t, null), oc(), Te());
    return Wl(l, t, a, e), t.child;
  }
  function Na(l, t) {
    return l !== null && l.tag === 22 || t.stateNode !== null || (t.stateNode = {
      _visibility: 1,
      _pendingMarkers: null,
      _retryCache: null,
      _transitions: null
    }), t.sibling;
  }
  function fo(l, t, e, u, a) {
    var n = nc();
    return n = n === null ? null : { parent: Bl._currentValue, pool: n }, t.memoizedState = {
      baseLanes: e,
      cachePool: n
    }, l !== null && _n(t, null), oc(), or(t), l !== null && Ou(l, t, u, !0), t.childLanes = a, null;
  }
  function Bn(l, t) {
    return t = Ln(
      { mode: t.mode, children: t.children },
      l.mode
    ), t.ref = l.ref, l.child = t, t.return = l, t;
  }
  function so(l, t, e) {
    return uu(t, l.child, null, e), l = Bn(t, t.pendingProps), l.flags |= 2, pt(t), t.memoizedState = null, l;
  }
  function Mv(l, t, e) {
    var u = t.pendingProps, a = (t.flags & 128) !== 0;
    if (t.flags &= -129, l === null) {
      if (rl) {
        if (u.mode === "hidden")
          return l = Bn(t, u), t.lanes = 536870912, Na(null, l);
        if (yc(t), (l = Al) ? (l = Ed(
          l,
          Dt
        ), l = l !== null && l.data === "&" ? l : null, l !== null && (t.memoizedState = {
          dehydrated: l,
          treeContext: be !== null ? { id: Xt, overflow: Zt } : null,
          retryLane: 536870912,
          hydrationErrors: null
        }, e = Js(l), e.return = t, t.child = e, kl = t, Al = null)) : l = null, l === null) throw pe(t);
        return t.lanes = 536870912, null;
      }
      return Bn(t, u);
    }
    var n = l.memoizedState;
    if (n !== null) {
      var i = n.dehydrated;
      if (yc(t), a)
        if (t.flags & 256)
          t.flags &= -257, t = so(
            l,
            t,
            e
          );
        else if (t.memoizedState !== null)
          t.child = l.child, t.flags |= 128, t = null;
        else throw Error(s(558));
      else if (Ll || Ou(l, t, e, !1), a = (e & l.childLanes) !== 0, Ll || a) {
        if (u = ql, u !== null && (i = Pf(u, e), i !== 0 && i !== n.retryLane))
          throw n.retryLane = i, $e(l, i), st(u, l, i), jc;
        kn(), t = so(
          l,
          t,
          e
        );
      } else
        l = n.treeContext, Al = Ct(i.nextSibling), kl = t, rl = !0, Se = null, Dt = !1, l !== null && $s(t, l), t = Bn(t, u), t.flags |= 4096;
      return t;
    }
    return l = Wt(l.child, {
      mode: u.mode,
      children: u.children
    }), l.ref = t.ref, t.child = l, l.return = t, l;
  }
  function Yn(l, t) {
    var e = t.ref;
    if (e === null)
      l !== null && l.ref !== null && (t.flags |= 4194816);
    else {
      if (typeof e != "function" && typeof e != "object")
        throw Error(s(284));
      (l === null || l.ref !== e) && (t.flags |= 4194816);
    }
  }
  function Rc(l, t, e, u, a) {
    return Pe(t), e = hc(
      l,
      t,
      e,
      u,
      void 0,
      a
    ), u = mc(), l !== null && !Ll ? (gc(l, t, a), ee(l, t, a)) : (rl && u && Wi(t), t.flags |= 1, Wl(l, t, e, a), t.child);
  }
  function ro(l, t, e, u, a, n) {
    return Pe(t), t.updateQueue = null, e = yr(
      t,
      u,
      e,
      a
    ), dr(l), u = mc(), l !== null && !Ll ? (gc(l, t, n), ee(l, t, n)) : (rl && u && Wi(t), t.flags |= 1, Wl(l, t, e, n), t.child);
  }
  function oo(l, t, e, u, a) {
    if (Pe(t), t.stateNode === null) {
      var n = Au, i = e.contextType;
      typeof i == "object" && i !== null && (n = $l(i)), n = new e(u, n), t.memoizedState = n.state !== null && n.state !== void 0 ? n.state : null, n.updater = Uc, t.stateNode = n, n._reactInternals = t, n = t.stateNode, n.props = u, n.state = t.memoizedState, n.refs = {}, cc(t), i = e.contextType, n.context = typeof i == "object" && i !== null ? $l(i) : Au, n.state = t.memoizedState, i = e.getDerivedStateFromProps, typeof i == "function" && (Dc(
        t,
        e,
        i,
        u
      ), n.state = t.memoizedState), typeof e.getDerivedStateFromProps == "function" || typeof n.getSnapshotBeforeUpdate == "function" || typeof n.UNSAFE_componentWillMount != "function" && typeof n.componentWillMount != "function" || (i = n.state, typeof n.componentWillMount == "function" && n.componentWillMount(), typeof n.UNSAFE_componentWillMount == "function" && n.UNSAFE_componentWillMount(), i !== n.state && Uc.enqueueReplaceState(n, n.state, null), za(t, u, n, a), Ea(), n.state = t.memoizedState), typeof n.componentDidMount == "function" && (t.flags |= 4194308), u = !0;
    } else if (l === null) {
      n = t.stateNode;
      var f = t.memoizedProps, d = nu(e, f);
      n.props = d;
      var g = n.context, z = e.contextType;
      i = Au, typeof z == "object" && z !== null && (i = $l(z));
      var M = e.getDerivedStateFromProps;
      z = typeof M == "function" || typeof n.getSnapshotBeforeUpdate == "function", f = t.pendingProps !== f, z || typeof n.UNSAFE_componentWillReceiveProps != "function" && typeof n.componentWillReceiveProps != "function" || (f || g !== i) && Fr(
        t,
        n,
        u,
        i
      ), Ee = !1;
      var S = t.memoizedState;
      n.state = S, za(t, u, n, a), Ea(), g = t.memoizedState, f || S !== g || Ee ? (typeof M == "function" && (Dc(
        t,
        e,
        M,
        u
      ), g = t.memoizedState), (d = Ee || Wr(
        t,
        e,
        d,
        u,
        S,
        g,
        i
      )) ? (z || typeof n.UNSAFE_componentWillMount != "function" && typeof n.componentWillMount != "function" || (typeof n.componentWillMount == "function" && n.componentWillMount(), typeof n.UNSAFE_componentWillMount == "function" && n.UNSAFE_componentWillMount()), typeof n.componentDidMount == "function" && (t.flags |= 4194308)) : (typeof n.componentDidMount == "function" && (t.flags |= 4194308), t.memoizedProps = u, t.memoizedState = g), n.props = u, n.state = g, n.context = i, u = d) : (typeof n.componentDidMount == "function" && (t.flags |= 4194308), u = !1);
    } else {
      n = t.stateNode, fc(l, t), i = t.memoizedProps, z = nu(e, i), n.props = z, M = t.pendingProps, S = n.context, g = e.contextType, d = Au, typeof g == "object" && g !== null && (d = $l(g)), f = e.getDerivedStateFromProps, (g = typeof f == "function" || typeof n.getSnapshotBeforeUpdate == "function") || typeof n.UNSAFE_componentWillReceiveProps != "function" && typeof n.componentWillReceiveProps != "function" || (i !== M || S !== d) && Fr(
        t,
        n,
        u,
        d
      ), Ee = !1, S = t.memoizedState, n.state = S, za(t, u, n, a), Ea();
      var _ = t.memoizedState;
      i !== M || S !== _ || Ee || l !== null && l.dependencies !== null && Sn(l.dependencies) ? (typeof f == "function" && (Dc(
        t,
        e,
        f,
        u
      ), _ = t.memoizedState), (z = Ee || Wr(
        t,
        e,
        z,
        u,
        S,
        _,
        d
      ) || l !== null && l.dependencies !== null && Sn(l.dependencies)) ? (g || typeof n.UNSAFE_componentWillUpdate != "function" && typeof n.componentWillUpdate != "function" || (typeof n.componentWillUpdate == "function" && n.componentWillUpdate(u, _, d), typeof n.UNSAFE_componentWillUpdate == "function" && n.UNSAFE_componentWillUpdate(
        u,
        _,
        d
      )), typeof n.componentDidUpdate == "function" && (t.flags |= 4), typeof n.getSnapshotBeforeUpdate == "function" && (t.flags |= 1024)) : (typeof n.componentDidUpdate != "function" || i === l.memoizedProps && S === l.memoizedState || (t.flags |= 4), typeof n.getSnapshotBeforeUpdate != "function" || i === l.memoizedProps && S === l.memoizedState || (t.flags |= 1024), t.memoizedProps = u, t.memoizedState = _), n.props = u, n.state = _, n.context = d, u = z) : (typeof n.componentDidUpdate != "function" || i === l.memoizedProps && S === l.memoizedState || (t.flags |= 4), typeof n.getSnapshotBeforeUpdate != "function" || i === l.memoizedProps && S === l.memoizedState || (t.flags |= 1024), u = !1);
    }
    return n = u, Yn(l, t), u = (t.flags & 128) !== 0, n || u ? (n = t.stateNode, e = u && typeof e.getDerivedStateFromError != "function" ? null : n.render(), t.flags |= 1, l !== null && u ? (t.child = uu(
      t,
      l.child,
      null,
      a
    ), t.child = uu(
      t,
      null,
      e,
      a
    )) : Wl(l, t, e, a), t.memoizedState = n.state, l = t.child) : l = ee(
      l,
      t,
      a
    ), l;
  }
  function yo(l, t, e, u) {
    return Fe(), t.flags |= 256, Wl(l, t, e, u), t.child;
  }
  var Hc = {
    dehydrated: null,
    treeContext: null,
    retryLane: 0,
    hydrationErrors: null
  };
  function Bc(l) {
    return { baseLanes: l, cachePool: tr() };
  }
  function Yc(l, t, e) {
    return l = l !== null ? l.childLanes & ~e : 0, t && (l |= Et), l;
  }
  function vo(l, t, e) {
    var u = t.pendingProps, a = !1, n = (t.flags & 128) !== 0, i;
    if ((i = n) || (i = l !== null && l.memoizedState === null ? !1 : (Cl.current & 2) !== 0), i && (a = !0, t.flags &= -129), i = (t.flags & 32) !== 0, t.flags &= -33, l === null) {
      if (rl) {
        if (a ? Ae(t) : Te(), (l = Al) ? (l = Ed(
          l,
          Dt
        ), l = l !== null && l.data !== "&" ? l : null, l !== null && (t.memoizedState = {
          dehydrated: l,
          treeContext: be !== null ? { id: Xt, overflow: Zt } : null,
          retryLane: 536870912,
          hydrationErrors: null
        }, e = Js(l), e.return = t, t.child = e, kl = t, Al = null)) : l = null, l === null) throw pe(t);
        return _f(l) ? t.lanes = 32 : t.lanes = 536870912, null;
      }
      var f = u.children;
      return u = u.fallback, a ? (Te(), a = t.mode, f = Ln(
        { mode: "hidden", children: f },
        a
      ), u = We(
        u,
        a,
        e,
        null
      ), f.return = t, u.return = t, f.sibling = u, t.child = f, u = t.child, u.memoizedState = Bc(e), u.childLanes = Yc(
        l,
        i,
        e
      ), t.memoizedState = Hc, Na(null, u)) : (Ae(t), Lc(t, f));
    }
    var d = l.memoizedState;
    if (d !== null && (f = d.dehydrated, f !== null)) {
      if (n)
        t.flags & 256 ? (Ae(t), t.flags &= -257, t = Gc(
          l,
          t,
          e
        )) : t.memoizedState !== null ? (Te(), t.child = l.child, t.flags |= 128, t = null) : (Te(), f = u.fallback, a = t.mode, u = Ln(
          { mode: "visible", children: u.children },
          a
        ), f = We(
          f,
          a,
          e,
          null
        ), f.flags |= 2, u.return = t, f.return = t, u.sibling = f, t.child = u, uu(
          t,
          l.child,
          null,
          e
        ), u = t.child, u.memoizedState = Bc(e), u.childLanes = Yc(
          l,
          i,
          e
        ), t.memoizedState = Hc, t = Na(null, u));
      else if (Ae(t), _f(f)) {
        if (i = f.nextSibling && f.nextSibling.dataset, i) var g = i.dgst;
        i = g, u = Error(s(419)), u.stack = "", u.digest = i, ma({ value: u, source: null, stack: null }), t = Gc(
          l,
          t,
          e
        );
      } else if (Ll || Ou(l, t, e, !1), i = (e & l.childLanes) !== 0, Ll || i) {
        if (i = ql, i !== null && (u = Pf(i, e), u !== 0 && u !== d.retryLane))
          throw d.retryLane = u, $e(l, u), st(i, l, u), jc;
        pf(f) || kn(), t = Gc(
          l,
          t,
          e
        );
      } else
        pf(f) ? (t.flags |= 192, t.child = l.child, t = null) : (l = d.treeContext, Al = Ct(
          f.nextSibling
        ), kl = t, rl = !0, Se = null, Dt = !1, l !== null && $s(t, l), t = Lc(
          t,
          u.children
        ), t.flags |= 4096);
      return t;
    }
    return a ? (Te(), f = u.fallback, a = t.mode, d = l.child, g = d.sibling, u = Wt(d, {
      mode: "hidden",
      children: u.children
    }), u.subtreeFlags = d.subtreeFlags & 65011712, g !== null ? f = Wt(
      g,
      f
    ) : (f = We(
      f,
      a,
      e,
      null
    ), f.flags |= 2), f.return = t, u.return = t, u.sibling = f, t.child = u, Na(null, u), u = t.child, f = l.child.memoizedState, f === null ? f = Bc(e) : (a = f.cachePool, a !== null ? (d = Bl._currentValue, a = a.parent !== d ? { parent: d, pool: d } : a) : a = tr(), f = {
      baseLanes: f.baseLanes | e,
      cachePool: a
    }), u.memoizedState = f, u.childLanes = Yc(
      l,
      i,
      e
    ), t.memoizedState = Hc, Na(l.child, u)) : (Ae(t), e = l.child, l = e.sibling, e = Wt(e, {
      mode: "visible",
      children: u.children
    }), e.return = t, e.sibling = null, l !== null && (i = t.deletions, i === null ? (t.deletions = [l], t.flags |= 16) : i.push(l)), t.child = e, t.memoizedState = null, e);
  }
  function Lc(l, t) {
    return t = Ln(
      { mode: "visible", children: t },
      l.mode
    ), t.return = l, l.child = t;
  }
  function Ln(l, t) {
    return l = bt(22, l, null, t), l.lanes = 0, l;
  }
  function Gc(l, t, e) {
    return uu(t, l.child, null, e), l = Lc(
      t,
      t.pendingProps.children
    ), l.flags |= 2, t.memoizedState = null, l;
  }
  function ho(l, t, e) {
    l.lanes |= t;
    var u = l.alternate;
    u !== null && (u.lanes |= t), tc(l.return, t, e);
  }
  function Qc(l, t, e, u, a, n) {
    var i = l.memoizedState;
    i === null ? l.memoizedState = {
      isBackwards: t,
      rendering: null,
      renderingStartTime: 0,
      last: u,
      tail: e,
      tailMode: a,
      treeForkCount: n
    } : (i.isBackwards = t, i.rendering = null, i.renderingStartTime = 0, i.last = u, i.tail = e, i.tailMode = a, i.treeForkCount = n);
  }
  function mo(l, t, e) {
    var u = t.pendingProps, a = u.revealOrder, n = u.tail;
    u = u.children;
    var i = Cl.current, f = (i & 2) !== 0;
    if (f ? (i = i & 1 | 2, t.flags |= 128) : i &= 1, R(Cl, i), Wl(l, t, u, e), u = rl ? ha : 0, !f && l !== null && (l.flags & 128) !== 0)
      l: for (l = t.child; l !== null; ) {
        if (l.tag === 13)
          l.memoizedState !== null && ho(l, e, t);
        else if (l.tag === 19)
          ho(l, e, t);
        else if (l.child !== null) {
          l.child.return = l, l = l.child;
          continue;
        }
        if (l === t) break l;
        for (; l.sibling === null; ) {
          if (l.return === null || l.return === t)
            break l;
          l = l.return;
        }
        l.sibling.return = l.return, l = l.sibling;
      }
    switch (a) {
      case "forwards":
        for (e = t.child, a = null; e !== null; )
          l = e.alternate, l !== null && xn(l) === null && (a = e), e = e.sibling;
        e = a, e === null ? (a = t.child, t.child = null) : (a = e.sibling, e.sibling = null), Qc(
          t,
          !1,
          a,
          e,
          n,
          u
        );
        break;
      case "backwards":
      case "unstable_legacy-backwards":
        for (e = null, a = t.child, t.child = null; a !== null; ) {
          if (l = a.alternate, l !== null && xn(l) === null) {
            t.child = a;
            break;
          }
          l = a.sibling, a.sibling = e, e = a, a = l;
        }
        Qc(
          t,
          !0,
          e,
          null,
          n,
          u
        );
        break;
      case "together":
        Qc(
          t,
          !1,
          null,
          null,
          void 0,
          u
        );
        break;
      default:
        t.memoizedState = null;
    }
    return t.child;
  }
  function ee(l, t, e) {
    if (l !== null && (t.dependencies = l.dependencies), Oe |= t.lanes, (e & t.childLanes) === 0)
      if (l !== null) {
        if (Ou(
          l,
          t,
          e,
          !1
        ), (e & t.childLanes) === 0)
          return null;
      } else return null;
    if (l !== null && t.child !== l.child)
      throw Error(s(153));
    if (t.child !== null) {
      for (l = t.child, e = Wt(l, l.pendingProps), t.child = e, e.return = t; l.sibling !== null; )
        l = l.sibling, e = e.sibling = Wt(l, l.pendingProps), e.return = t;
      e.sibling = null;
    }
    return t.child;
  }
  function Xc(l, t) {
    return (l.lanes & t) !== 0 ? !0 : (l = l.dependencies, !!(l !== null && Sn(l)));
  }
  function Dv(l, t, e) {
    switch (t.tag) {
      case 3:
        Ul(t, t.stateNode.containerInfo), _e(t, Bl, l.memoizedState.cache), Fe();
        break;
      case 27:
      case 5:
        Ze(t);
        break;
      case 4:
        Ul(t, t.stateNode.containerInfo);
        break;
      case 10:
        _e(
          t,
          t.type,
          t.memoizedProps.value
        );
        break;
      case 31:
        if (t.memoizedState !== null)
          return t.flags |= 128, yc(t), null;
        break;
      case 13:
        var u = t.memoizedState;
        if (u !== null)
          return u.dehydrated !== null ? (Ae(t), t.flags |= 128, null) : (e & t.child.childLanes) !== 0 ? vo(l, t, e) : (Ae(t), l = ee(
            l,
            t,
            e
          ), l !== null ? l.sibling : null);
        Ae(t);
        break;
      case 19:
        var a = (l.flags & 128) !== 0;
        if (u = (e & t.childLanes) !== 0, u || (Ou(
          l,
          t,
          e,
          !1
        ), u = (e & t.childLanes) !== 0), a) {
          if (u)
            return mo(
              l,
              t,
              e
            );
          t.flags |= 128;
        }
        if (a = t.memoizedState, a !== null && (a.rendering = null, a.tail = null, a.lastEffect = null), R(Cl, Cl.current), u) break;
        return null;
      case 22:
        return t.lanes = 0, co(
          l,
          t,
          e,
          t.pendingProps
        );
      case 24:
        _e(t, Bl, l.memoizedState.cache);
    }
    return ee(l, t, e);
  }
  function go(l, t, e) {
    if (l !== null)
      if (l.memoizedProps !== t.pendingProps)
        Ll = !0;
      else {
        if (!Xc(l, e) && (t.flags & 128) === 0)
          return Ll = !1, Dv(
            l,
            t,
            e
          );
        Ll = (l.flags & 131072) !== 0;
      }
    else
      Ll = !1, rl && (t.flags & 1048576) !== 0 && ks(t, ha, t.index);
    switch (t.lanes = 0, t.tag) {
      case 16:
        l: {
          var u = t.pendingProps;
          if (l = tu(t.elementType), t.type = l, typeof l == "function")
            wi(l) ? (u = nu(l, u), t.tag = 1, t = oo(
              null,
              t,
              l,
              u,
              e
            )) : (t.tag = 0, t = Rc(
              null,
              t,
              l,
              u,
              e
            ));
          else {
            if (l != null) {
              var a = l.$$typeof;
              if (a === I) {
                t.tag = 11, t = ao(
                  null,
                  t,
                  l,
                  u,
                  e
                );
                break l;
              } else if (a === nl) {
                t.tag = 14, t = no(
                  null,
                  t,
                  l,
                  u,
                  e
                );
                break l;
              }
            }
            throw t = Jl(l) || l, Error(s(306, t, ""));
          }
        }
        return t;
      case 0:
        return Rc(
          l,
          t,
          t.type,
          t.pendingProps,
          e
        );
      case 1:
        return u = t.type, a = nu(
          u,
          t.pendingProps
        ), oo(
          l,
          t,
          u,
          a,
          e
        );
      case 3:
        l: {
          if (Ul(
            t,
            t.stateNode.containerInfo
          ), l === null) throw Error(s(387));
          u = t.pendingProps;
          var n = t.memoizedState;
          a = n.element, fc(l, t), za(t, u, null, e);
          var i = t.memoizedState;
          if (u = i.cache, _e(t, Bl, u), u !== n.cache && ec(
            t,
            [Bl],
            e,
            !0
          ), Ea(), u = i.element, n.isDehydrated)
            if (n = {
              element: u,
              isDehydrated: !1,
              cache: i.cache
            }, t.updateQueue.baseState = n, t.memoizedState = n, t.flags & 256) {
              t = yo(
                l,
                t,
                u,
                e
              );
              break l;
            } else if (u !== a) {
              a = Nt(
                Error(s(424)),
                t
              ), ma(a), t = yo(
                l,
                t,
                u,
                e
              );
              break l;
            } else {
              switch (l = t.stateNode.containerInfo, l.nodeType) {
                case 9:
                  l = l.body;
                  break;
                default:
                  l = l.nodeName === "HTML" ? l.ownerDocument.body : l;
              }
              for (Al = Ct(l.firstChild), kl = t, rl = !0, Se = null, Dt = !0, e = cr(
                t,
                null,
                u,
                e
              ), t.child = e; e; )
                e.flags = e.flags & -3 | 4096, e = e.sibling;
            }
          else {
            if (Fe(), u === a) {
              t = ee(
                l,
                t,
                e
              );
              break l;
            }
            Wl(l, t, u, e);
          }
          t = t.child;
        }
        return t;
      case 26:
        return Yn(l, t), l === null ? (e = Nd(
          t.type,
          null,
          t.pendingProps,
          null
        )) ? t.memoizedState = e : rl || (e = t.type, l = t.pendingProps, u = ti(
          ll.current
        ).createElement(e), u[wl] = t, u[ut] = l, Fl(u, e, l), Zl(u), t.stateNode = u) : t.memoizedState = Nd(
          t.type,
          l.memoizedProps,
          t.pendingProps,
          l.memoizedState
        ), null;
      case 27:
        return Ze(t), l === null && rl && (u = t.stateNode = Ad(
          t.type,
          t.pendingProps,
          ll.current
        ), kl = t, Dt = !0, a = Al, je(t.type) ? (Ef = a, Al = Ct(u.firstChild)) : Al = a), Wl(
          l,
          t,
          t.pendingProps.children,
          e
        ), Yn(l, t), l === null && (t.flags |= 4194304), t.child;
      case 5:
        return l === null && rl && ((a = u = Al) && (u = ch(
          u,
          t.type,
          t.pendingProps,
          Dt
        ), u !== null ? (t.stateNode = u, kl = t, Al = Ct(u.firstChild), Dt = !1, a = !0) : a = !1), a || pe(t)), Ze(t), a = t.type, n = t.pendingProps, i = l !== null ? l.memoizedProps : null, u = n.children, gf(a, n) ? u = null : i !== null && gf(a, i) && (t.flags |= 32), t.memoizedState !== null && (a = hc(
          l,
          t,
          Ev,
          null,
          null,
          e
        ), Za._currentValue = a), Yn(l, t), Wl(l, t, u, e), t.child;
      case 6:
        return l === null && rl && ((l = e = Al) && (e = fh(
          e,
          t.pendingProps,
          Dt
        ), e !== null ? (t.stateNode = e, kl = t, Al = null, l = !0) : l = !1), l || pe(t)), null;
      case 13:
        return vo(l, t, e);
      case 4:
        return Ul(
          t,
          t.stateNode.containerInfo
        ), u = t.pendingProps, l === null ? t.child = uu(
          t,
          null,
          u,
          e
        ) : Wl(l, t, u, e), t.child;
      case 11:
        return ao(
          l,
          t,
          t.type,
          t.pendingProps,
          e
        );
      case 7:
        return Wl(
          l,
          t,
          t.pendingProps,
          e
        ), t.child;
      case 8:
        return Wl(
          l,
          t,
          t.pendingProps.children,
          e
        ), t.child;
      case 12:
        return Wl(
          l,
          t,
          t.pendingProps.children,
          e
        ), t.child;
      case 10:
        return u = t.pendingProps, _e(t, t.type, u.value), Wl(l, t, u.children, e), t.child;
      case 9:
        return a = t.type._context, u = t.pendingProps.children, Pe(t), a = $l(a), u = u(a), t.flags |= 1, Wl(l, t, u, e), t.child;
      case 14:
        return no(
          l,
          t,
          t.type,
          t.pendingProps,
          e
        );
      case 15:
        return io(
          l,
          t,
          t.type,
          t.pendingProps,
          e
        );
      case 19:
        return mo(l, t, e);
      case 31:
        return Mv(l, t, e);
      case 22:
        return co(
          l,
          t,
          e,
          t.pendingProps
        );
      case 24:
        return Pe(t), u = $l(Bl), l === null ? (a = nc(), a === null && (a = ql, n = uc(), a.pooledCache = n, n.refCount++, n !== null && (a.pooledCacheLanes |= e), a = n), t.memoizedState = { parent: u, cache: a }, cc(t), _e(t, Bl, a)) : ((l.lanes & e) !== 0 && (fc(l, t), za(t, null, null, e), Ea()), a = l.memoizedState, n = t.memoizedState, a.parent !== u ? (a = { parent: u, cache: u }, t.memoizedState = a, t.lanes === 0 && (t.memoizedState = t.updateQueue.baseState = a), _e(t, Bl, u)) : (u = n.cache, _e(t, Bl, u), u !== a.cache && ec(
          t,
          [Bl],
          e,
          !0
        ))), Wl(
          l,
          t,
          t.pendingProps.children,
          e
        ), t.child;
      case 29:
        throw t.pendingProps;
    }
    throw Error(s(156, t.tag));
  }
  function ue(l) {
    l.flags |= 4;
  }
  function Zc(l, t, e, u, a) {
    if ((t = (l.mode & 32) !== 0) && (t = !1), t) {
      if (l.flags |= 16777216, (a & 335544128) === a)
        if (l.stateNode.complete) l.flags |= 8192;
        else if (Vo()) l.flags |= 8192;
        else
          throw eu = zn, ic;
    } else l.flags &= -16777217;
  }
  function bo(l, t) {
    if (t.type !== "stylesheet" || (t.state.loading & 4) !== 0)
      l.flags &= -16777217;
    else if (l.flags |= 16777216, !Cd(t))
      if (Vo()) l.flags |= 8192;
      else
        throw eu = zn, ic;
  }
  function Gn(l, t) {
    t !== null && (l.flags |= 4), l.flags & 16384 && (t = l.tag !== 22 ? Wf() : 536870912, l.lanes |= t, Qu |= t);
  }
  function Oa(l, t) {
    if (!rl)
      switch (l.tailMode) {
        case "hidden":
          t = l.tail;
          for (var e = null; t !== null; )
            t.alternate !== null && (e = t), t = t.sibling;
          e === null ? l.tail = null : e.sibling = null;
          break;
        case "collapsed":
          e = l.tail;
          for (var u = null; e !== null; )
            e.alternate !== null && (u = e), e = e.sibling;
          u === null ? t || l.tail === null ? l.tail = null : l.tail.sibling = null : u.sibling = null;
      }
  }
  function Tl(l) {
    var t = l.alternate !== null && l.alternate.child === l.child, e = 0, u = 0;
    if (t)
      for (var a = l.child; a !== null; )
        e |= a.lanes | a.childLanes, u |= a.subtreeFlags & 65011712, u |= a.flags & 65011712, a.return = l, a = a.sibling;
    else
      for (a = l.child; a !== null; )
        e |= a.lanes | a.childLanes, u |= a.subtreeFlags, u |= a.flags, a.return = l, a = a.sibling;
    return l.subtreeFlags |= u, l.childLanes = e, t;
  }
  function Uv(l, t, e) {
    var u = t.pendingProps;
    switch (Fi(t), t.tag) {
      case 16:
      case 15:
      case 0:
      case 11:
      case 7:
      case 8:
      case 12:
      case 9:
      case 14:
        return Tl(t), null;
      case 1:
        return Tl(t), null;
      case 3:
        return e = t.stateNode, u = null, l !== null && (u = l.memoizedState.cache), t.memoizedState.cache !== u && (t.flags |= 2048), Pt(Bl), Nl(), e.pendingContext && (e.context = e.pendingContext, e.pendingContext = null), (l === null || l.child === null) && (Nu(t) ? ue(t) : l === null || l.memoizedState.isDehydrated && (t.flags & 256) === 0 || (t.flags |= 1024, Pi())), Tl(t), null;
      case 26:
        var a = t.type, n = t.memoizedState;
        return l === null ? (ue(t), n !== null ? (Tl(t), bo(t, n)) : (Tl(t), Zc(
          t,
          a,
          null,
          u,
          e
        ))) : n ? n !== l.memoizedState ? (ue(t), Tl(t), bo(t, n)) : (Tl(t), t.flags &= -16777217) : (l = l.memoizedProps, l !== u && ue(t), Tl(t), Zc(
          t,
          a,
          l,
          u,
          e
        )), null;
      case 27:
        if (ou(t), e = ll.current, a = t.type, l !== null && t.stateNode != null)
          l.memoizedProps !== u && ue(t);
        else {
          if (!u) {
            if (t.stateNode === null)
              throw Error(s(166));
            return Tl(t), null;
          }
          l = L.current, Nu(t) ? Ws(t) : (l = Ad(a, u, e), t.stateNode = l, ue(t));
        }
        return Tl(t), null;
      case 5:
        if (ou(t), a = t.type, l !== null && t.stateNode != null)
          l.memoizedProps !== u && ue(t);
        else {
          if (!u) {
            if (t.stateNode === null)
              throw Error(s(166));
            return Tl(t), null;
          }
          if (n = L.current, Nu(t))
            Ws(t);
          else {
            var i = ti(
              ll.current
            );
            switch (n) {
              case 1:
                n = i.createElementNS(
                  "http://www.w3.org/2000/svg",
                  a
                );
                break;
              case 2:
                n = i.createElementNS(
                  "http://www.w3.org/1998/Math/MathML",
                  a
                );
                break;
              default:
                switch (a) {
                  case "svg":
                    n = i.createElementNS(
                      "http://www.w3.org/2000/svg",
                      a
                    );
                    break;
                  case "math":
                    n = i.createElementNS(
                      "http://www.w3.org/1998/Math/MathML",
                      a
                    );
                    break;
                  case "script":
                    n = i.createElement("div"), n.innerHTML = "<script><\/script>", n = n.removeChild(
                      n.firstChild
                    );
                    break;
                  case "select":
                    n = typeof u.is == "string" ? i.createElement("select", {
                      is: u.is
                    }) : i.createElement("select"), u.multiple ? n.multiple = !0 : u.size && (n.size = u.size);
                    break;
                  default:
                    n = typeof u.is == "string" ? i.createElement(a, { is: u.is }) : i.createElement(a);
                }
            }
            n[wl] = t, n[ut] = u;
            l: for (i = t.child; i !== null; ) {
              if (i.tag === 5 || i.tag === 6)
                n.appendChild(i.stateNode);
              else if (i.tag !== 4 && i.tag !== 27 && i.child !== null) {
                i.child.return = i, i = i.child;
                continue;
              }
              if (i === t) break l;
              for (; i.sibling === null; ) {
                if (i.return === null || i.return === t)
                  break l;
                i = i.return;
              }
              i.sibling.return = i.return, i = i.sibling;
            }
            t.stateNode = n;
            l: switch (Fl(n, a, u), a) {
              case "button":
              case "input":
              case "select":
              case "textarea":
                u = !!u.autoFocus;
                break l;
              case "img":
                u = !0;
                break l;
              default:
                u = !1;
            }
            u && ue(t);
          }
        }
        return Tl(t), Zc(
          t,
          t.type,
          l === null ? null : l.memoizedProps,
          t.pendingProps,
          e
        ), null;
      case 6:
        if (l && t.stateNode != null)
          l.memoizedProps !== u && ue(t);
        else {
          if (typeof u != "string" && t.stateNode === null)
            throw Error(s(166));
          if (l = ll.current, Nu(t)) {
            if (l = t.stateNode, e = t.memoizedProps, u = null, a = kl, a !== null)
              switch (a.tag) {
                case 27:
                case 5:
                  u = a.memoizedProps;
              }
            l[wl] = t, l = !!(l.nodeValue === e || u !== null && u.suppressHydrationWarning === !0 || vd(l.nodeValue, e)), l || pe(t, !0);
          } else
            l = ti(l).createTextNode(
              u
            ), l[wl] = t, t.stateNode = l;
        }
        return Tl(t), null;
      case 31:
        if (e = t.memoizedState, l === null || l.memoizedState !== null) {
          if (u = Nu(t), e !== null) {
            if (l === null) {
              if (!u) throw Error(s(318));
              if (l = t.memoizedState, l = l !== null ? l.dehydrated : null, !l) throw Error(s(557));
              l[wl] = t;
            } else
              Fe(), (t.flags & 128) === 0 && (t.memoizedState = null), t.flags |= 4;
            Tl(t), l = !1;
          } else
            e = Pi(), l !== null && l.memoizedState !== null && (l.memoizedState.hydrationErrors = e), l = !0;
          if (!l)
            return t.flags & 256 ? (pt(t), t) : (pt(t), null);
          if ((t.flags & 128) !== 0)
            throw Error(s(558));
        }
        return Tl(t), null;
      case 13:
        if (u = t.memoizedState, l === null || l.memoizedState !== null && l.memoizedState.dehydrated !== null) {
          if (a = Nu(t), u !== null && u.dehydrated !== null) {
            if (l === null) {
              if (!a) throw Error(s(318));
              if (a = t.memoizedState, a = a !== null ? a.dehydrated : null, !a) throw Error(s(317));
              a[wl] = t;
            } else
              Fe(), (t.flags & 128) === 0 && (t.memoizedState = null), t.flags |= 4;
            Tl(t), a = !1;
          } else
            a = Pi(), l !== null && l.memoizedState !== null && (l.memoizedState.hydrationErrors = a), a = !0;
          if (!a)
            return t.flags & 256 ? (pt(t), t) : (pt(t), null);
        }
        return pt(t), (t.flags & 128) !== 0 ? (t.lanes = e, t) : (e = u !== null, l = l !== null && l.memoizedState !== null, e && (u = t.child, a = null, u.alternate !== null && u.alternate.memoizedState !== null && u.alternate.memoizedState.cachePool !== null && (a = u.alternate.memoizedState.cachePool.pool), n = null, u.memoizedState !== null && u.memoizedState.cachePool !== null && (n = u.memoizedState.cachePool.pool), n !== a && (u.flags |= 2048)), e !== l && e && (t.child.flags |= 8192), Gn(t, t.updateQueue), Tl(t), null);
      case 4:
        return Nl(), l === null && df(t.stateNode.containerInfo), Tl(t), null;
      case 10:
        return Pt(t.type), Tl(t), null;
      case 19:
        if (O(Cl), u = t.memoizedState, u === null) return Tl(t), null;
        if (a = (t.flags & 128) !== 0, n = u.rendering, n === null)
          if (a) Oa(u, !1);
          else {
            if (Dl !== 0 || l !== null && (l.flags & 128) !== 0)
              for (l = t.child; l !== null; ) {
                if (n = xn(l), n !== null) {
                  for (t.flags |= 128, Oa(u, !1), l = n.updateQueue, t.updateQueue = l, Gn(t, l), t.subtreeFlags = 0, l = e, e = t.child; e !== null; )
                    Ks(e, l), e = e.sibling;
                  return R(
                    Cl,
                    Cl.current & 1 | 2
                  ), rl && Ft(t, u.treeForkCount), t.child;
                }
                l = l.sibling;
              }
            u.tail !== null && vt() > Kn && (t.flags |= 128, a = !0, Oa(u, !1), t.lanes = 4194304);
          }
        else {
          if (!a)
            if (l = xn(n), l !== null) {
              if (t.flags |= 128, a = !0, l = l.updateQueue, t.updateQueue = l, Gn(t, l), Oa(u, !0), u.tail === null && u.tailMode === "hidden" && !n.alternate && !rl)
                return Tl(t), null;
            } else
              2 * vt() - u.renderingStartTime > Kn && e !== 536870912 && (t.flags |= 128, a = !0, Oa(u, !1), t.lanes = 4194304);
          u.isBackwards ? (n.sibling = t.child, t.child = n) : (l = u.last, l !== null ? l.sibling = n : t.child = n, u.last = n);
        }
        return u.tail !== null ? (l = u.tail, u.rendering = l, u.tail = l.sibling, u.renderingStartTime = vt(), l.sibling = null, e = Cl.current, R(
          Cl,
          a ? e & 1 | 2 : e & 1
        ), rl && Ft(t, u.treeForkCount), l) : (Tl(t), null);
      case 22:
      case 23:
        return pt(t), dc(), u = t.memoizedState !== null, l !== null ? l.memoizedState !== null !== u && (t.flags |= 8192) : u && (t.flags |= 8192), u ? (e & 536870912) !== 0 && (t.flags & 128) === 0 && (Tl(t), t.subtreeFlags & 6 && (t.flags |= 8192)) : Tl(t), e = t.updateQueue, e !== null && Gn(t, e.retryQueue), e = null, l !== null && l.memoizedState !== null && l.memoizedState.cachePool !== null && (e = l.memoizedState.cachePool.pool), u = null, t.memoizedState !== null && t.memoizedState.cachePool !== null && (u = t.memoizedState.cachePool.pool), u !== e && (t.flags |= 2048), l !== null && O(lu), null;
      case 24:
        return e = null, l !== null && (e = l.memoizedState.cache), t.memoizedState.cache !== e && (t.flags |= 2048), Pt(Bl), Tl(t), null;
      case 25:
        return null;
      case 30:
        return null;
    }
    throw Error(s(156, t.tag));
  }
  function Cv(l, t) {
    switch (Fi(t), t.tag) {
      case 1:
        return l = t.flags, l & 65536 ? (t.flags = l & -65537 | 128, t) : null;
      case 3:
        return Pt(Bl), Nl(), l = t.flags, (l & 65536) !== 0 && (l & 128) === 0 ? (t.flags = l & -65537 | 128, t) : null;
      case 26:
      case 27:
      case 5:
        return ou(t), null;
      case 31:
        if (t.memoizedState !== null) {
          if (pt(t), t.alternate === null)
            throw Error(s(340));
          Fe();
        }
        return l = t.flags, l & 65536 ? (t.flags = l & -65537 | 128, t) : null;
      case 13:
        if (pt(t), l = t.memoizedState, l !== null && l.dehydrated !== null) {
          if (t.alternate === null)
            throw Error(s(340));
          Fe();
        }
        return l = t.flags, l & 65536 ? (t.flags = l & -65537 | 128, t) : null;
      case 19:
        return O(Cl), null;
      case 4:
        return Nl(), null;
      case 10:
        return Pt(t.type), null;
      case 22:
      case 23:
        return pt(t), dc(), l !== null && O(lu), l = t.flags, l & 65536 ? (t.flags = l & -65537 | 128, t) : null;
      case 24:
        return Pt(Bl), null;
      case 25:
        return null;
      default:
        return null;
    }
  }
  function So(l, t) {
    switch (Fi(t), t.tag) {
      case 3:
        Pt(Bl), Nl();
        break;
      case 26:
      case 27:
      case 5:
        ou(t);
        break;
      case 4:
        Nl();
        break;
      case 31:
        t.memoizedState !== null && pt(t);
        break;
      case 13:
        pt(t);
        break;
      case 19:
        O(Cl);
        break;
      case 10:
        Pt(t.type);
        break;
      case 22:
      case 23:
        pt(t), dc(), l !== null && O(lu);
        break;
      case 24:
        Pt(Bl);
    }
  }
  function Ma(l, t) {
    try {
      var e = t.updateQueue, u = e !== null ? e.lastEffect : null;
      if (u !== null) {
        var a = u.next;
        e = a;
        do {
          if ((e.tag & l) === l) {
            u = void 0;
            var n = e.create, i = e.inst;
            u = n(), i.destroy = u;
          }
          e = e.next;
        } while (e !== a);
      }
    } catch (f) {
      pl(t, t.return, f);
    }
  }
  function xe(l, t, e) {
    try {
      var u = t.updateQueue, a = u !== null ? u.lastEffect : null;
      if (a !== null) {
        var n = a.next;
        u = n;
        do {
          if ((u.tag & l) === l) {
            var i = u.inst, f = i.destroy;
            if (f !== void 0) {
              i.destroy = void 0, a = t;
              var d = e, g = f;
              try {
                g();
              } catch (z) {
                pl(
                  a,
                  d,
                  z
                );
              }
            }
          }
          u = u.next;
        } while (u !== n);
      }
    } catch (z) {
      pl(t, t.return, z);
    }
  }
  function po(l) {
    var t = l.updateQueue;
    if (t !== null) {
      var e = l.stateNode;
      try {
        sr(t, e);
      } catch (u) {
        pl(l, l.return, u);
      }
    }
  }
  function _o(l, t, e) {
    e.props = nu(
      l.type,
      l.memoizedProps
    ), e.state = l.memoizedState;
    try {
      e.componentWillUnmount();
    } catch (u) {
      pl(l, t, u);
    }
  }
  function Da(l, t) {
    try {
      var e = l.ref;
      if (e !== null) {
        switch (l.tag) {
          case 26:
          case 27:
          case 5:
            var u = l.stateNode;
            break;
          case 30:
            u = l.stateNode;
            break;
          default:
            u = l.stateNode;
        }
        typeof e == "function" ? l.refCleanup = e(u) : e.current = u;
      }
    } catch (a) {
      pl(l, t, a);
    }
  }
  function Vt(l, t) {
    var e = l.ref, u = l.refCleanup;
    if (e !== null)
      if (typeof u == "function")
        try {
          u();
        } catch (a) {
          pl(l, t, a);
        } finally {
          l.refCleanup = null, l = l.alternate, l != null && (l.refCleanup = null);
        }
      else if (typeof e == "function")
        try {
          e(null);
        } catch (a) {
          pl(l, t, a);
        }
      else e.current = null;
  }
  function Eo(l) {
    var t = l.type, e = l.memoizedProps, u = l.stateNode;
    try {
      l: switch (t) {
        case "button":
        case "input":
        case "select":
        case "textarea":
          e.autoFocus && u.focus();
          break l;
        case "img":
          e.src ? u.src = e.src : e.srcSet && (u.srcset = e.srcSet);
      }
    } catch (a) {
      pl(l, l.return, a);
    }
  }
  function Vc(l, t, e) {
    try {
      var u = l.stateNode;
      th(u, l.type, e, t), u[ut] = t;
    } catch (a) {
      pl(l, l.return, a);
    }
  }
  function zo(l) {
    return l.tag === 5 || l.tag === 3 || l.tag === 26 || l.tag === 27 && je(l.type) || l.tag === 4;
  }
  function Kc(l) {
    l: for (; ; ) {
      for (; l.sibling === null; ) {
        if (l.return === null || zo(l.return)) return null;
        l = l.return;
      }
      for (l.sibling.return = l.return, l = l.sibling; l.tag !== 5 && l.tag !== 6 && l.tag !== 18; ) {
        if (l.tag === 27 && je(l.type) || l.flags & 2 || l.child === null || l.tag === 4) continue l;
        l.child.return = l, l = l.child;
      }
      if (!(l.flags & 2)) return l.stateNode;
    }
  }
  function Jc(l, t, e) {
    var u = l.tag;
    if (u === 5 || u === 6)
      l = l.stateNode, t ? (e.nodeType === 9 ? e.body : e.nodeName === "HTML" ? e.ownerDocument.body : e).insertBefore(l, t) : (t = e.nodeType === 9 ? e.body : e.nodeName === "HTML" ? e.ownerDocument.body : e, t.appendChild(l), e = e._reactRootContainer, e != null || t.onclick !== null || (t.onclick = kt));
    else if (u !== 4 && (u === 27 && je(l.type) && (e = l.stateNode, t = null), l = l.child, l !== null))
      for (Jc(l, t, e), l = l.sibling; l !== null; )
        Jc(l, t, e), l = l.sibling;
  }
  function Qn(l, t, e) {
    var u = l.tag;
    if (u === 5 || u === 6)
      l = l.stateNode, t ? e.insertBefore(l, t) : e.appendChild(l);
    else if (u !== 4 && (u === 27 && je(l.type) && (e = l.stateNode), l = l.child, l !== null))
      for (Qn(l, t, e), l = l.sibling; l !== null; )
        Qn(l, t, e), l = l.sibling;
  }
  function qo(l) {
    var t = l.stateNode, e = l.memoizedProps;
    try {
      for (var u = l.type, a = t.attributes; a.length; )
        t.removeAttributeNode(a[0]);
      Fl(t, u, e), t[wl] = l, t[ut] = e;
    } catch (n) {
      pl(l, l.return, n);
    }
  }
  var ae = !1, Gl = !1, wc = !1, Ao = typeof WeakSet == "function" ? WeakSet : Set, Vl = null;
  function jv(l, t) {
    if (l = l.containerInfo, hf = fi, l = Hs(l), Gi(l)) {
      if ("selectionStart" in l)
        var e = {
          start: l.selectionStart,
          end: l.selectionEnd
        };
      else
        l: {
          e = (e = l.ownerDocument) && e.defaultView || window;
          var u = e.getSelection && e.getSelection();
          if (u && u.rangeCount !== 0) {
            e = u.anchorNode;
            var a = u.anchorOffset, n = u.focusNode;
            u = u.focusOffset;
            try {
              e.nodeType, n.nodeType;
            } catch {
              e = null;
              break l;
            }
            var i = 0, f = -1, d = -1, g = 0, z = 0, M = l, S = null;
            t: for (; ; ) {
              for (var _; M !== e || a !== 0 && M.nodeType !== 3 || (f = i + a), M !== n || u !== 0 && M.nodeType !== 3 || (d = i + u), M.nodeType === 3 && (i += M.nodeValue.length), (_ = M.firstChild) !== null; )
                S = M, M = _;
              for (; ; ) {
                if (M === l) break t;
                if (S === e && ++g === a && (f = i), S === n && ++z === u && (d = i), (_ = M.nextSibling) !== null) break;
                M = S, S = M.parentNode;
              }
              M = _;
            }
            e = f === -1 || d === -1 ? null : { start: f, end: d };
          } else e = null;
        }
      e = e || { start: 0, end: 0 };
    } else e = null;
    for (mf = { focusedElem: l, selectionRange: e }, fi = !1, Vl = t; Vl !== null; )
      if (t = Vl, l = t.child, (t.subtreeFlags & 1028) !== 0 && l !== null)
        l.return = t, Vl = l;
      else
        for (; Vl !== null; ) {
          switch (t = Vl, n = t.alternate, l = t.flags, t.tag) {
            case 0:
              if ((l & 4) !== 0 && (l = t.updateQueue, l = l !== null ? l.events : null, l !== null))
                for (e = 0; e < l.length; e++)
                  a = l[e], a.ref.impl = a.nextImpl;
              break;
            case 11:
            case 15:
              break;
            case 1:
              if ((l & 1024) !== 0 && n !== null) {
                l = void 0, e = t, a = n.memoizedProps, n = n.memoizedState, u = e.stateNode;
                try {
                  var Y = nu(
                    e.type,
                    a
                  );
                  l = u.getSnapshotBeforeUpdate(
                    Y,
                    n
                  ), u.__reactInternalSnapshotBeforeUpdate = l;
                } catch (w) {
                  pl(
                    e,
                    e.return,
                    w
                  );
                }
              }
              break;
            case 3:
              if ((l & 1024) !== 0) {
                if (l = t.stateNode.containerInfo, e = l.nodeType, e === 9)
                  Sf(l);
                else if (e === 1)
                  switch (l.nodeName) {
                    case "HEAD":
                    case "HTML":
                    case "BODY":
                      Sf(l);
                      break;
                    default:
                      l.textContent = "";
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
              if ((l & 1024) !== 0) throw Error(s(163));
          }
          if (l = t.sibling, l !== null) {
            l.return = t.return, Vl = l;
            break;
          }
          Vl = t.return;
        }
  }
  function To(l, t, e) {
    var u = e.flags;
    switch (e.tag) {
      case 0:
      case 11:
      case 15:
        ie(l, e), u & 4 && Ma(5, e);
        break;
      case 1:
        if (ie(l, e), u & 4)
          if (l = e.stateNode, t === null)
            try {
              l.componentDidMount();
            } catch (i) {
              pl(e, e.return, i);
            }
          else {
            var a = nu(
              e.type,
              t.memoizedProps
            );
            t = t.memoizedState;
            try {
              l.componentDidUpdate(
                a,
                t,
                l.__reactInternalSnapshotBeforeUpdate
              );
            } catch (i) {
              pl(
                e,
                e.return,
                i
              );
            }
          }
        u & 64 && po(e), u & 512 && Da(e, e.return);
        break;
      case 3:
        if (ie(l, e), u & 64 && (l = e.updateQueue, l !== null)) {
          if (t = null, e.child !== null)
            switch (e.child.tag) {
              case 27:
              case 5:
                t = e.child.stateNode;
                break;
              case 1:
                t = e.child.stateNode;
            }
          try {
            sr(l, t);
          } catch (i) {
            pl(e, e.return, i);
          }
        }
        break;
      case 27:
        t === null && u & 4 && qo(e);
      case 26:
      case 5:
        ie(l, e), t === null && u & 4 && Eo(e), u & 512 && Da(e, e.return);
        break;
      case 12:
        ie(l, e);
        break;
      case 31:
        ie(l, e), u & 4 && Oo(l, e);
        break;
      case 13:
        ie(l, e), u & 4 && Mo(l, e), u & 64 && (l = e.memoizedState, l !== null && (l = l.dehydrated, l !== null && (e = Zv.bind(
          null,
          e
        ), sh(l, e))));
        break;
      case 22:
        if (u = e.memoizedState !== null || ae, !u) {
          t = t !== null && t.memoizedState !== null || Gl, a = ae;
          var n = Gl;
          ae = u, (Gl = t) && !n ? ce(
            l,
            e,
            (e.subtreeFlags & 8772) !== 0
          ) : ie(l, e), ae = a, Gl = n;
        }
        break;
      case 30:
        break;
      default:
        ie(l, e);
    }
  }
  function xo(l) {
    var t = l.alternate;
    t !== null && (l.alternate = null, xo(t)), l.child = null, l.deletions = null, l.sibling = null, l.tag === 5 && (t = l.stateNode, t !== null && zi(t)), l.stateNode = null, l.return = null, l.dependencies = null, l.memoizedProps = null, l.memoizedState = null, l.pendingProps = null, l.stateNode = null, l.updateQueue = null;
  }
  var xl = null, nt = !1;
  function ne(l, t, e) {
    for (e = e.child; e !== null; )
      No(l, t, e), e = e.sibling;
  }
  function No(l, t, e) {
    if (ht && typeof ht.onCommitFiberUnmount == "function")
      try {
        ht.onCommitFiberUnmount(ta, e);
      } catch {
      }
    switch (e.tag) {
      case 26:
        Gl || Vt(e, t), ne(
          l,
          t,
          e
        ), e.memoizedState ? e.memoizedState.count-- : e.stateNode && (e = e.stateNode, e.parentNode.removeChild(e));
        break;
      case 27:
        Gl || Vt(e, t);
        var u = xl, a = nt;
        je(e.type) && (xl = e.stateNode, nt = !1), ne(
          l,
          t,
          e
        ), Ga(e.stateNode), xl = u, nt = a;
        break;
      case 5:
        Gl || Vt(e, t);
      case 6:
        if (u = xl, a = nt, xl = null, ne(
          l,
          t,
          e
        ), xl = u, nt = a, xl !== null)
          if (nt)
            try {
              (xl.nodeType === 9 ? xl.body : xl.nodeName === "HTML" ? xl.ownerDocument.body : xl).removeChild(e.stateNode);
            } catch (n) {
              pl(
                e,
                t,
                n
              );
            }
          else
            try {
              xl.removeChild(e.stateNode);
            } catch (n) {
              pl(
                e,
                t,
                n
              );
            }
        break;
      case 18:
        xl !== null && (nt ? (l = xl, pd(
          l.nodeType === 9 ? l.body : l.nodeName === "HTML" ? l.ownerDocument.body : l,
          e.stateNode
        ), $u(l)) : pd(xl, e.stateNode));
        break;
      case 4:
        u = xl, a = nt, xl = e.stateNode.containerInfo, nt = !0, ne(
          l,
          t,
          e
        ), xl = u, nt = a;
        break;
      case 0:
      case 11:
      case 14:
      case 15:
        xe(2, e, t), Gl || xe(4, e, t), ne(
          l,
          t,
          e
        );
        break;
      case 1:
        Gl || (Vt(e, t), u = e.stateNode, typeof u.componentWillUnmount == "function" && _o(
          e,
          t,
          u
        )), ne(
          l,
          t,
          e
        );
        break;
      case 21:
        ne(
          l,
          t,
          e
        );
        break;
      case 22:
        Gl = (u = Gl) || e.memoizedState !== null, ne(
          l,
          t,
          e
        ), Gl = u;
        break;
      default:
        ne(
          l,
          t,
          e
        );
    }
  }
  function Oo(l, t) {
    if (t.memoizedState === null && (l = t.alternate, l !== null && (l = l.memoizedState, l !== null))) {
      l = l.dehydrated;
      try {
        $u(l);
      } catch (e) {
        pl(t, t.return, e);
      }
    }
  }
  function Mo(l, t) {
    if (t.memoizedState === null && (l = t.alternate, l !== null && (l = l.memoizedState, l !== null && (l = l.dehydrated, l !== null))))
      try {
        $u(l);
      } catch (e) {
        pl(t, t.return, e);
      }
  }
  function Rv(l) {
    switch (l.tag) {
      case 31:
      case 13:
      case 19:
        var t = l.stateNode;
        return t === null && (t = l.stateNode = new Ao()), t;
      case 22:
        return l = l.stateNode, t = l._retryCache, t === null && (t = l._retryCache = new Ao()), t;
      default:
        throw Error(s(435, l.tag));
    }
  }
  function Xn(l, t) {
    var e = Rv(l);
    t.forEach(function(u) {
      if (!e.has(u)) {
        e.add(u);
        var a = Vv.bind(null, l, u);
        u.then(a, a);
      }
    });
  }
  function it(l, t) {
    var e = t.deletions;
    if (e !== null)
      for (var u = 0; u < e.length; u++) {
        var a = e[u], n = l, i = t, f = i;
        l: for (; f !== null; ) {
          switch (f.tag) {
            case 27:
              if (je(f.type)) {
                xl = f.stateNode, nt = !1;
                break l;
              }
              break;
            case 5:
              xl = f.stateNode, nt = !1;
              break l;
            case 3:
            case 4:
              xl = f.stateNode.containerInfo, nt = !0;
              break l;
          }
          f = f.return;
        }
        if (xl === null) throw Error(s(160));
        No(n, i, a), xl = null, nt = !1, n = a.alternate, n !== null && (n.return = null), a.return = null;
      }
    if (t.subtreeFlags & 13886)
      for (t = t.child; t !== null; )
        Do(t, l), t = t.sibling;
  }
  var Bt = null;
  function Do(l, t) {
    var e = l.alternate, u = l.flags;
    switch (l.tag) {
      case 0:
      case 11:
      case 14:
      case 15:
        it(t, l), ct(l), u & 4 && (xe(3, l, l.return), Ma(3, l), xe(5, l, l.return));
        break;
      case 1:
        it(t, l), ct(l), u & 512 && (Gl || e === null || Vt(e, e.return)), u & 64 && ae && (l = l.updateQueue, l !== null && (u = l.callbacks, u !== null && (e = l.shared.hiddenCallbacks, l.shared.hiddenCallbacks = e === null ? u : e.concat(u))));
        break;
      case 26:
        var a = Bt;
        if (it(t, l), ct(l), u & 512 && (Gl || e === null || Vt(e, e.return)), u & 4) {
          var n = e !== null ? e.memoizedState : null;
          if (u = l.memoizedState, e === null)
            if (u === null)
              if (l.stateNode === null) {
                l: {
                  u = l.type, e = l.memoizedProps, a = a.ownerDocument || a;
                  t: switch (u) {
                    case "title":
                      n = a.getElementsByTagName("title")[0], (!n || n[aa] || n[wl] || n.namespaceURI === "http://www.w3.org/2000/svg" || n.hasAttribute("itemprop")) && (n = a.createElement(u), a.head.insertBefore(
                        n,
                        a.querySelector("head > title")
                      )), Fl(n, u, e), n[wl] = l, Zl(n), u = n;
                      break l;
                    case "link":
                      var i = Dd(
                        "link",
                        "href",
                        a
                      ).get(u + (e.href || ""));
                      if (i) {
                        for (var f = 0; f < i.length; f++)
                          if (n = i[f], n.getAttribute("href") === (e.href == null || e.href === "" ? null : e.href) && n.getAttribute("rel") === (e.rel == null ? null : e.rel) && n.getAttribute("title") === (e.title == null ? null : e.title) && n.getAttribute("crossorigin") === (e.crossOrigin == null ? null : e.crossOrigin)) {
                            i.splice(f, 1);
                            break t;
                          }
                      }
                      n = a.createElement(u), Fl(n, u, e), a.head.appendChild(n);
                      break;
                    case "meta":
                      if (i = Dd(
                        "meta",
                        "content",
                        a
                      ).get(u + (e.content || ""))) {
                        for (f = 0; f < i.length; f++)
                          if (n = i[f], n.getAttribute("content") === (e.content == null ? null : "" + e.content) && n.getAttribute("name") === (e.name == null ? null : e.name) && n.getAttribute("property") === (e.property == null ? null : e.property) && n.getAttribute("http-equiv") === (e.httpEquiv == null ? null : e.httpEquiv) && n.getAttribute("charset") === (e.charSet == null ? null : e.charSet)) {
                            i.splice(f, 1);
                            break t;
                          }
                      }
                      n = a.createElement(u), Fl(n, u, e), a.head.appendChild(n);
                      break;
                    default:
                      throw Error(s(468, u));
                  }
                  n[wl] = l, Zl(n), u = n;
                }
                l.stateNode = u;
              } else
                Ud(
                  a,
                  l.type,
                  l.stateNode
                );
            else
              l.stateNode = Md(
                a,
                u,
                l.memoizedProps
              );
          else
            n !== u ? (n === null ? e.stateNode !== null && (e = e.stateNode, e.parentNode.removeChild(e)) : n.count--, u === null ? Ud(
              a,
              l.type,
              l.stateNode
            ) : Md(
              a,
              u,
              l.memoizedProps
            )) : u === null && l.stateNode !== null && Vc(
              l,
              l.memoizedProps,
              e.memoizedProps
            );
        }
        break;
      case 27:
        it(t, l), ct(l), u & 512 && (Gl || e === null || Vt(e, e.return)), e !== null && u & 4 && Vc(
          l,
          l.memoizedProps,
          e.memoizedProps
        );
        break;
      case 5:
        if (it(t, l), ct(l), u & 512 && (Gl || e === null || Vt(e, e.return)), l.flags & 32) {
          a = l.stateNode;
          try {
            bu(a, "");
          } catch (Y) {
            pl(l, l.return, Y);
          }
        }
        u & 4 && l.stateNode != null && (a = l.memoizedProps, Vc(
          l,
          a,
          e !== null ? e.memoizedProps : a
        )), u & 1024 && (wc = !0);
        break;
      case 6:
        if (it(t, l), ct(l), u & 4) {
          if (l.stateNode === null)
            throw Error(s(162));
          u = l.memoizedProps, e = l.stateNode;
          try {
            e.nodeValue = u;
          } catch (Y) {
            pl(l, l.return, Y);
          }
        }
        break;
      case 3:
        if (ai = null, a = Bt, Bt = ei(t.containerInfo), it(t, l), Bt = a, ct(l), u & 4 && e !== null && e.memoizedState.isDehydrated)
          try {
            $u(t.containerInfo);
          } catch (Y) {
            pl(l, l.return, Y);
          }
        wc && (wc = !1, Uo(l));
        break;
      case 4:
        u = Bt, Bt = ei(
          l.stateNode.containerInfo
        ), it(t, l), ct(l), Bt = u;
        break;
      case 12:
        it(t, l), ct(l);
        break;
      case 31:
        it(t, l), ct(l), u & 4 && (u = l.updateQueue, u !== null && (l.updateQueue = null, Xn(l, u)));
        break;
      case 13:
        it(t, l), ct(l), l.child.flags & 8192 && l.memoizedState !== null != (e !== null && e.memoizedState !== null) && (Vn = vt()), u & 4 && (u = l.updateQueue, u !== null && (l.updateQueue = null, Xn(l, u)));
        break;
      case 22:
        a = l.memoizedState !== null;
        var d = e !== null && e.memoizedState !== null, g = ae, z = Gl;
        if (ae = g || a, Gl = z || d, it(t, l), Gl = z, ae = g, ct(l), u & 8192)
          l: for (t = l.stateNode, t._visibility = a ? t._visibility & -2 : t._visibility | 1, a && (e === null || d || ae || Gl || iu(l)), e = null, t = l; ; ) {
            if (t.tag === 5 || t.tag === 26) {
              if (e === null) {
                d = e = t;
                try {
                  if (n = d.stateNode, a)
                    i = n.style, typeof i.setProperty == "function" ? i.setProperty("display", "none", "important") : i.display = "none";
                  else {
                    f = d.stateNode;
                    var M = d.memoizedProps.style, S = M != null && M.hasOwnProperty("display") ? M.display : null;
                    f.style.display = S == null || typeof S == "boolean" ? "" : ("" + S).trim();
                  }
                } catch (Y) {
                  pl(d, d.return, Y);
                }
              }
            } else if (t.tag === 6) {
              if (e === null) {
                d = t;
                try {
                  d.stateNode.nodeValue = a ? "" : d.memoizedProps;
                } catch (Y) {
                  pl(d, d.return, Y);
                }
              }
            } else if (t.tag === 18) {
              if (e === null) {
                d = t;
                try {
                  var _ = d.stateNode;
                  a ? _d(_, !0) : _d(d.stateNode, !1);
                } catch (Y) {
                  pl(d, d.return, Y);
                }
              }
            } else if ((t.tag !== 22 && t.tag !== 23 || t.memoizedState === null || t === l) && t.child !== null) {
              t.child.return = t, t = t.child;
              continue;
            }
            if (t === l) break l;
            for (; t.sibling === null; ) {
              if (t.return === null || t.return === l) break l;
              e === t && (e = null), t = t.return;
            }
            e === t && (e = null), t.sibling.return = t.return, t = t.sibling;
          }
        u & 4 && (u = l.updateQueue, u !== null && (e = u.retryQueue, e !== null && (u.retryQueue = null, Xn(l, e))));
        break;
      case 19:
        it(t, l), ct(l), u & 4 && (u = l.updateQueue, u !== null && (l.updateQueue = null, Xn(l, u)));
        break;
      case 30:
        break;
      case 21:
        break;
      default:
        it(t, l), ct(l);
    }
  }
  function ct(l) {
    var t = l.flags;
    if (t & 2) {
      try {
        for (var e, u = l.return; u !== null; ) {
          if (zo(u)) {
            e = u;
            break;
          }
          u = u.return;
        }
        if (e == null) throw Error(s(160));
        switch (e.tag) {
          case 27:
            var a = e.stateNode, n = Kc(l);
            Qn(l, n, a);
            break;
          case 5:
            var i = e.stateNode;
            e.flags & 32 && (bu(i, ""), e.flags &= -33);
            var f = Kc(l);
            Qn(l, f, i);
            break;
          case 3:
          case 4:
            var d = e.stateNode.containerInfo, g = Kc(l);
            Jc(
              l,
              g,
              d
            );
            break;
          default:
            throw Error(s(161));
        }
      } catch (z) {
        pl(l, l.return, z);
      }
      l.flags &= -3;
    }
    t & 4096 && (l.flags &= -4097);
  }
  function Uo(l) {
    if (l.subtreeFlags & 1024)
      for (l = l.child; l !== null; ) {
        var t = l;
        Uo(t), t.tag === 5 && t.flags & 1024 && t.stateNode.reset(), l = l.sibling;
      }
  }
  function ie(l, t) {
    if (t.subtreeFlags & 8772)
      for (t = t.child; t !== null; )
        To(l, t.alternate, t), t = t.sibling;
  }
  function iu(l) {
    for (l = l.child; l !== null; ) {
      var t = l;
      switch (t.tag) {
        case 0:
        case 11:
        case 14:
        case 15:
          xe(4, t, t.return), iu(t);
          break;
        case 1:
          Vt(t, t.return);
          var e = t.stateNode;
          typeof e.componentWillUnmount == "function" && _o(
            t,
            t.return,
            e
          ), iu(t);
          break;
        case 27:
          Ga(t.stateNode);
        case 26:
        case 5:
          Vt(t, t.return), iu(t);
          break;
        case 22:
          t.memoizedState === null && iu(t);
          break;
        case 30:
          iu(t);
          break;
        default:
          iu(t);
      }
      l = l.sibling;
    }
  }
  function ce(l, t, e) {
    for (e = e && (t.subtreeFlags & 8772) !== 0, t = t.child; t !== null; ) {
      var u = t.alternate, a = l, n = t, i = n.flags;
      switch (n.tag) {
        case 0:
        case 11:
        case 15:
          ce(
            a,
            n,
            e
          ), Ma(4, n);
          break;
        case 1:
          if (ce(
            a,
            n,
            e
          ), u = n, a = u.stateNode, typeof a.componentDidMount == "function")
            try {
              a.componentDidMount();
            } catch (g) {
              pl(u, u.return, g);
            }
          if (u = n, a = u.updateQueue, a !== null) {
            var f = u.stateNode;
            try {
              var d = a.shared.hiddenCallbacks;
              if (d !== null)
                for (a.shared.hiddenCallbacks = null, a = 0; a < d.length; a++)
                  fr(d[a], f);
            } catch (g) {
              pl(u, u.return, g);
            }
          }
          e && i & 64 && po(n), Da(n, n.return);
          break;
        case 27:
          qo(n);
        case 26:
        case 5:
          ce(
            a,
            n,
            e
          ), e && u === null && i & 4 && Eo(n), Da(n, n.return);
          break;
        case 12:
          ce(
            a,
            n,
            e
          );
          break;
        case 31:
          ce(
            a,
            n,
            e
          ), e && i & 4 && Oo(a, n);
          break;
        case 13:
          ce(
            a,
            n,
            e
          ), e && i & 4 && Mo(a, n);
          break;
        case 22:
          n.memoizedState === null && ce(
            a,
            n,
            e
          ), Da(n, n.return);
          break;
        case 30:
          break;
        default:
          ce(
            a,
            n,
            e
          );
      }
      t = t.sibling;
    }
  }
  function kc(l, t) {
    var e = null;
    l !== null && l.memoizedState !== null && l.memoizedState.cachePool !== null && (e = l.memoizedState.cachePool.pool), l = null, t.memoizedState !== null && t.memoizedState.cachePool !== null && (l = t.memoizedState.cachePool.pool), l !== e && (l != null && l.refCount++, e != null && ga(e));
  }
  function $c(l, t) {
    l = null, t.alternate !== null && (l = t.alternate.memoizedState.cache), t = t.memoizedState.cache, t !== l && (t.refCount++, l != null && ga(l));
  }
  function Yt(l, t, e, u) {
    if (t.subtreeFlags & 10256)
      for (t = t.child; t !== null; )
        Co(
          l,
          t,
          e,
          u
        ), t = t.sibling;
  }
  function Co(l, t, e, u) {
    var a = t.flags;
    switch (t.tag) {
      case 0:
      case 11:
      case 15:
        Yt(
          l,
          t,
          e,
          u
        ), a & 2048 && Ma(9, t);
        break;
      case 1:
        Yt(
          l,
          t,
          e,
          u
        );
        break;
      case 3:
        Yt(
          l,
          t,
          e,
          u
        ), a & 2048 && (l = null, t.alternate !== null && (l = t.alternate.memoizedState.cache), t = t.memoizedState.cache, t !== l && (t.refCount++, l != null && ga(l)));
        break;
      case 12:
        if (a & 2048) {
          Yt(
            l,
            t,
            e,
            u
          ), l = t.stateNode;
          try {
            var n = t.memoizedProps, i = n.id, f = n.onPostCommit;
            typeof f == "function" && f(
              i,
              t.alternate === null ? "mount" : "update",
              l.passiveEffectDuration,
              -0
            );
          } catch (d) {
            pl(t, t.return, d);
          }
        } else
          Yt(
            l,
            t,
            e,
            u
          );
        break;
      case 31:
        Yt(
          l,
          t,
          e,
          u
        );
        break;
      case 13:
        Yt(
          l,
          t,
          e,
          u
        );
        break;
      case 23:
        break;
      case 22:
        n = t.stateNode, i = t.alternate, t.memoizedState !== null ? n._visibility & 2 ? Yt(
          l,
          t,
          e,
          u
        ) : Ua(l, t) : n._visibility & 2 ? Yt(
          l,
          t,
          e,
          u
        ) : (n._visibility |= 2, Yu(
          l,
          t,
          e,
          u,
          (t.subtreeFlags & 10256) !== 0 || !1
        )), a & 2048 && kc(i, t);
        break;
      case 24:
        Yt(
          l,
          t,
          e,
          u
        ), a & 2048 && $c(t.alternate, t);
        break;
      default:
        Yt(
          l,
          t,
          e,
          u
        );
    }
  }
  function Yu(l, t, e, u, a) {
    for (a = a && ((t.subtreeFlags & 10256) !== 0 || !1), t = t.child; t !== null; ) {
      var n = l, i = t, f = e, d = u, g = i.flags;
      switch (i.tag) {
        case 0:
        case 11:
        case 15:
          Yu(
            n,
            i,
            f,
            d,
            a
          ), Ma(8, i);
          break;
        case 23:
          break;
        case 22:
          var z = i.stateNode;
          i.memoizedState !== null ? z._visibility & 2 ? Yu(
            n,
            i,
            f,
            d,
            a
          ) : Ua(
            n,
            i
          ) : (z._visibility |= 2, Yu(
            n,
            i,
            f,
            d,
            a
          )), a && g & 2048 && kc(
            i.alternate,
            i
          );
          break;
        case 24:
          Yu(
            n,
            i,
            f,
            d,
            a
          ), a && g & 2048 && $c(i.alternate, i);
          break;
        default:
          Yu(
            n,
            i,
            f,
            d,
            a
          );
      }
      t = t.sibling;
    }
  }
  function Ua(l, t) {
    if (t.subtreeFlags & 10256)
      for (t = t.child; t !== null; ) {
        var e = l, u = t, a = u.flags;
        switch (u.tag) {
          case 22:
            Ua(e, u), a & 2048 && kc(
              u.alternate,
              u
            );
            break;
          case 24:
            Ua(e, u), a & 2048 && $c(u.alternate, u);
            break;
          default:
            Ua(e, u);
        }
        t = t.sibling;
      }
  }
  var Ca = 8192;
  function Lu(l, t, e) {
    if (l.subtreeFlags & Ca)
      for (l = l.child; l !== null; )
        jo(
          l,
          t,
          e
        ), l = l.sibling;
  }
  function jo(l, t, e) {
    switch (l.tag) {
      case 26:
        Lu(
          l,
          t,
          e
        ), l.flags & Ca && l.memoizedState !== null && _h(
          e,
          Bt,
          l.memoizedState,
          l.memoizedProps
        );
        break;
      case 5:
        Lu(
          l,
          t,
          e
        );
        break;
      case 3:
      case 4:
        var u = Bt;
        Bt = ei(l.stateNode.containerInfo), Lu(
          l,
          t,
          e
        ), Bt = u;
        break;
      case 22:
        l.memoizedState === null && (u = l.alternate, u !== null && u.memoizedState !== null ? (u = Ca, Ca = 16777216, Lu(
          l,
          t,
          e
        ), Ca = u) : Lu(
          l,
          t,
          e
        ));
        break;
      default:
        Lu(
          l,
          t,
          e
        );
    }
  }
  function Ro(l) {
    var t = l.alternate;
    if (t !== null && (l = t.child, l !== null)) {
      t.child = null;
      do
        t = l.sibling, l.sibling = null, l = t;
      while (l !== null);
    }
  }
  function ja(l) {
    var t = l.deletions;
    if ((l.flags & 16) !== 0) {
      if (t !== null)
        for (var e = 0; e < t.length; e++) {
          var u = t[e];
          Vl = u, Bo(
            u,
            l
          );
        }
      Ro(l);
    }
    if (l.subtreeFlags & 10256)
      for (l = l.child; l !== null; )
        Ho(l), l = l.sibling;
  }
  function Ho(l) {
    switch (l.tag) {
      case 0:
      case 11:
      case 15:
        ja(l), l.flags & 2048 && xe(9, l, l.return);
        break;
      case 3:
        ja(l);
        break;
      case 12:
        ja(l);
        break;
      case 22:
        var t = l.stateNode;
        l.memoizedState !== null && t._visibility & 2 && (l.return === null || l.return.tag !== 13) ? (t._visibility &= -3, Zn(l)) : ja(l);
        break;
      default:
        ja(l);
    }
  }
  function Zn(l) {
    var t = l.deletions;
    if ((l.flags & 16) !== 0) {
      if (t !== null)
        for (var e = 0; e < t.length; e++) {
          var u = t[e];
          Vl = u, Bo(
            u,
            l
          );
        }
      Ro(l);
    }
    for (l = l.child; l !== null; ) {
      switch (t = l, t.tag) {
        case 0:
        case 11:
        case 15:
          xe(8, t, t.return), Zn(t);
          break;
        case 22:
          e = t.stateNode, e._visibility & 2 && (e._visibility &= -3, Zn(t));
          break;
        default:
          Zn(t);
      }
      l = l.sibling;
    }
  }
  function Bo(l, t) {
    for (; Vl !== null; ) {
      var e = Vl;
      switch (e.tag) {
        case 0:
        case 11:
        case 15:
          xe(8, e, t);
          break;
        case 23:
        case 22:
          if (e.memoizedState !== null && e.memoizedState.cachePool !== null) {
            var u = e.memoizedState.cachePool.pool;
            u != null && u.refCount++;
          }
          break;
        case 24:
          ga(e.memoizedState.cache);
      }
      if (u = e.child, u !== null) u.return = e, Vl = u;
      else
        l: for (e = l; Vl !== null; ) {
          u = Vl;
          var a = u.sibling, n = u.return;
          if (xo(u), u === e) {
            Vl = null;
            break l;
          }
          if (a !== null) {
            a.return = n, Vl = a;
            break l;
          }
          Vl = n;
        }
    }
  }
  var Hv = {
    getCacheForType: function(l) {
      var t = $l(Bl), e = t.data.get(l);
      return e === void 0 && (e = l(), t.data.set(l, e)), e;
    },
    cacheSignal: function() {
      return $l(Bl).controller.signal;
    }
  }, Bv = typeof WeakMap == "function" ? WeakMap : Map, gl = 0, ql = null, il = null, fl = 0, Sl = 0, _t = null, Ne = !1, Gu = !1, Wc = !1, fe = 0, Dl = 0, Oe = 0, cu = 0, Fc = 0, Et = 0, Qu = 0, Ra = null, ft = null, Ic = !1, Vn = 0, Yo = 0, Kn = 1 / 0, Jn = null, Me = null, Xl = 0, De = null, Xu = null, se = 0, Pc = 0, lf = null, Lo = null, Ha = 0, tf = null;
  function zt() {
    return (gl & 2) !== 0 && fl !== 0 ? fl & -fl : q.T !== null ? ff() : ls();
  }
  function Go() {
    if (Et === 0)
      if ((fl & 536870912) === 0 || rl) {
        var l = Pa;
        Pa <<= 1, (Pa & 3932160) === 0 && (Pa = 262144), Et = l;
      } else Et = 536870912;
    return l = St.current, l !== null && (l.flags |= 32), Et;
  }
  function st(l, t, e) {
    (l === ql && (Sl === 2 || Sl === 9) || l.cancelPendingCommit !== null) && (Zu(l, 0), Ue(
      l,
      fl,
      Et,
      !1
    )), ua(l, e), ((gl & 2) === 0 || l !== ql) && (l === ql && ((gl & 2) === 0 && (cu |= e), Dl === 4 && Ue(
      l,
      fl,
      Et,
      !1
    )), Kt(l));
  }
  function Qo(l, t, e) {
    if ((gl & 6) !== 0) throw Error(s(327));
    var u = !e && (t & 127) === 0 && (t & l.expiredLanes) === 0 || ea(l, t), a = u ? Gv(l, t) : uf(l, t, !0), n = u;
    do {
      if (a === 0) {
        Gu && !u && Ue(l, t, 0, !1);
        break;
      } else {
        if (e = l.current.alternate, n && !Yv(e)) {
          a = uf(l, t, !1), n = !1;
          continue;
        }
        if (a === 2) {
          if (n = t, l.errorRecoveryDisabledLanes & n)
            var i = 0;
          else
            i = l.pendingLanes & -536870913, i = i !== 0 ? i : i & 536870912 ? 536870912 : 0;
          if (i !== 0) {
            t = i;
            l: {
              var f = l;
              a = Ra;
              var d = f.current.memoizedState.isDehydrated;
              if (d && (Zu(f, i).flags |= 256), i = uf(
                f,
                i,
                !1
              ), i !== 2) {
                if (Wc && !d) {
                  f.errorRecoveryDisabledLanes |= n, cu |= n, a = 4;
                  break l;
                }
                n = ft, ft = a, n !== null && (ft === null ? ft = n : ft.push.apply(
                  ft,
                  n
                ));
              }
              a = i;
            }
            if (n = !1, a !== 2) continue;
          }
        }
        if (a === 1) {
          Zu(l, 0), Ue(l, t, 0, !0);
          break;
        }
        l: {
          switch (u = l, n = a, n) {
            case 0:
            case 1:
              throw Error(s(345));
            case 4:
              if ((t & 4194048) !== t) break;
            case 6:
              Ue(
                u,
                t,
                Et,
                !Ne
              );
              break l;
            case 2:
              ft = null;
              break;
            case 3:
            case 5:
              break;
            default:
              throw Error(s(329));
          }
          if ((t & 62914560) === t && (a = Vn + 300 - vt(), 10 < a)) {
            if (Ue(
              u,
              t,
              Et,
              !Ne
            ), tn(u, 0, !0) !== 0) break l;
            se = t, u.timeoutHandle = bd(
              Xo.bind(
                null,
                u,
                e,
                ft,
                Jn,
                Ic,
                t,
                Et,
                cu,
                Qu,
                Ne,
                n,
                "Throttled",
                -0,
                0
              ),
              a
            );
            break l;
          }
          Xo(
            u,
            e,
            ft,
            Jn,
            Ic,
            t,
            Et,
            cu,
            Qu,
            Ne,
            n,
            null,
            -0,
            0
          );
        }
      }
      break;
    } while (!0);
    Kt(l);
  }
  function Xo(l, t, e, u, a, n, i, f, d, g, z, M, S, _) {
    if (l.timeoutHandle = -1, M = t.subtreeFlags, M & 8192 || (M & 16785408) === 16785408) {
      M = {
        stylesheets: null,
        count: 0,
        imgCount: 0,
        imgBytes: 0,
        suspenseyImages: [],
        waitingForImages: !0,
        waitingForViewTransition: !1,
        unsuspend: kt
      }, jo(
        t,
        n,
        M
      );
      var Y = (n & 62914560) === n ? Vn - vt() : (n & 4194048) === n ? Yo - vt() : 0;
      if (Y = Eh(
        M,
        Y
      ), Y !== null) {
        se = n, l.cancelPendingCommit = Y(
          Wo.bind(
            null,
            l,
            t,
            n,
            e,
            u,
            a,
            i,
            f,
            d,
            z,
            M,
            null,
            S,
            _
          )
        ), Ue(l, n, i, !g);
        return;
      }
    }
    Wo(
      l,
      t,
      n,
      e,
      u,
      a,
      i,
      f,
      d
    );
  }
  function Yv(l) {
    for (var t = l; ; ) {
      var e = t.tag;
      if ((e === 0 || e === 11 || e === 15) && t.flags & 16384 && (e = t.updateQueue, e !== null && (e = e.stores, e !== null)))
        for (var u = 0; u < e.length; u++) {
          var a = e[u], n = a.getSnapshot;
          a = a.value;
          try {
            if (!gt(n(), a)) return !1;
          } catch {
            return !1;
          }
        }
      if (e = t.child, t.subtreeFlags & 16384 && e !== null)
        e.return = t, t = e;
      else {
        if (t === l) break;
        for (; t.sibling === null; ) {
          if (t.return === null || t.return === l) return !0;
          t = t.return;
        }
        t.sibling.return = t.return, t = t.sibling;
      }
    }
    return !0;
  }
  function Ue(l, t, e, u) {
    t &= ~Fc, t &= ~cu, l.suspendedLanes |= t, l.pingedLanes &= ~t, u && (l.warmLanes |= t), u = l.expirationTimes;
    for (var a = t; 0 < a; ) {
      var n = 31 - mt(a), i = 1 << n;
      u[n] = -1, a &= ~i;
    }
    e !== 0 && Ff(l, e, t);
  }
  function wn() {
    return (gl & 6) === 0 ? (Ba(0), !1) : !0;
  }
  function ef() {
    if (il !== null) {
      if (Sl === 0)
        var l = il.return;
      else
        l = il, It = Ie = null, bc(l), Cu = null, Sa = 0, l = il;
      for (; l !== null; )
        So(l.alternate, l), l = l.return;
      il = null;
    }
  }
  function Zu(l, t) {
    var e = l.timeoutHandle;
    e !== -1 && (l.timeoutHandle = -1, ah(e)), e = l.cancelPendingCommit, e !== null && (l.cancelPendingCommit = null, e()), se = 0, ef(), ql = l, il = e = Wt(l.current, null), fl = t, Sl = 0, _t = null, Ne = !1, Gu = ea(l, t), Wc = !1, Qu = Et = Fc = cu = Oe = Dl = 0, ft = Ra = null, Ic = !1, (t & 8) !== 0 && (t |= t & 32);
    var u = l.entangledLanes;
    if (u !== 0)
      for (l = l.entanglements, u &= t; 0 < u; ) {
        var a = 31 - mt(u), n = 1 << a;
        t |= l[a], u &= ~n;
      }
    return fe = t, vn(), e;
  }
  function Zo(l, t) {
    el = null, q.H = xa, t === Uu || t === En ? (t = ar(), Sl = 3) : t === ic ? (t = ar(), Sl = 4) : Sl = t === jc ? 8 : t !== null && typeof t == "object" && typeof t.then == "function" ? 6 : 1, _t = t, il === null && (Dl = 1, Hn(
      l,
      Nt(t, l.current)
    ));
  }
  function Vo() {
    var l = St.current;
    return l === null ? !0 : (fl & 4194048) === fl ? Ut === null : (fl & 62914560) === fl || (fl & 536870912) !== 0 ? l === Ut : !1;
  }
  function Ko() {
    var l = q.H;
    return q.H = xa, l === null ? xa : l;
  }
  function Jo() {
    var l = q.A;
    return q.A = Hv, l;
  }
  function kn() {
    Dl = 4, Ne || (fl & 4194048) !== fl && St.current !== null || (Gu = !0), (Oe & 134217727) === 0 && (cu & 134217727) === 0 || ql === null || Ue(
      ql,
      fl,
      Et,
      !1
    );
  }
  function uf(l, t, e) {
    var u = gl;
    gl |= 2;
    var a = Ko(), n = Jo();
    (ql !== l || fl !== t) && (Jn = null, Zu(l, t)), t = !1;
    var i = Dl;
    l: do
      try {
        if (Sl !== 0 && il !== null) {
          var f = il, d = _t;
          switch (Sl) {
            case 8:
              ef(), i = 6;
              break l;
            case 3:
            case 2:
            case 9:
            case 6:
              St.current === null && (t = !0);
              var g = Sl;
              if (Sl = 0, _t = null, Vu(l, f, d, g), e && Gu) {
                i = 0;
                break l;
              }
              break;
            default:
              g = Sl, Sl = 0, _t = null, Vu(l, f, d, g);
          }
        }
        Lv(), i = Dl;
        break;
      } catch (z) {
        Zo(l, z);
      }
    while (!0);
    return t && l.shellSuspendCounter++, It = Ie = null, gl = u, q.H = a, q.A = n, il === null && (ql = null, fl = 0, vn()), i;
  }
  function Lv() {
    for (; il !== null; ) wo(il);
  }
  function Gv(l, t) {
    var e = gl;
    gl |= 2;
    var u = Ko(), a = Jo();
    ql !== l || fl !== t ? (Jn = null, Kn = vt() + 500, Zu(l, t)) : Gu = ea(
      l,
      t
    );
    l: do
      try {
        if (Sl !== 0 && il !== null) {
          t = il;
          var n = _t;
          t: switch (Sl) {
            case 1:
              Sl = 0, _t = null, Vu(l, t, n, 1);
              break;
            case 2:
            case 9:
              if (er(n)) {
                Sl = 0, _t = null, ko(t);
                break;
              }
              t = function() {
                Sl !== 2 && Sl !== 9 || ql !== l || (Sl = 7), Kt(l);
              }, n.then(t, t);
              break l;
            case 3:
              Sl = 7;
              break l;
            case 4:
              Sl = 5;
              break l;
            case 7:
              er(n) ? (Sl = 0, _t = null, ko(t)) : (Sl = 0, _t = null, Vu(l, t, n, 7));
              break;
            case 5:
              var i = null;
              switch (il.tag) {
                case 26:
                  i = il.memoizedState;
                case 5:
                case 27:
                  var f = il;
                  if (i ? Cd(i) : f.stateNode.complete) {
                    Sl = 0, _t = null;
                    var d = f.sibling;
                    if (d !== null) il = d;
                    else {
                      var g = f.return;
                      g !== null ? (il = g, $n(g)) : il = null;
                    }
                    break t;
                  }
              }
              Sl = 0, _t = null, Vu(l, t, n, 5);
              break;
            case 6:
              Sl = 0, _t = null, Vu(l, t, n, 6);
              break;
            case 8:
              ef(), Dl = 6;
              break l;
            default:
              throw Error(s(462));
          }
        }
        Qv();
        break;
      } catch (z) {
        Zo(l, z);
      }
    while (!0);
    return It = Ie = null, q.H = u, q.A = a, gl = e, il !== null ? 0 : (ql = null, fl = 0, vn(), Dl);
  }
  function Qv() {
    for (; il !== null && !ry(); )
      wo(il);
  }
  function wo(l) {
    var t = go(l.alternate, l, fe);
    l.memoizedProps = l.pendingProps, t === null ? $n(l) : il = t;
  }
  function ko(l) {
    var t = l, e = t.alternate;
    switch (t.tag) {
      case 15:
      case 0:
        t = ro(
          e,
          t,
          t.pendingProps,
          t.type,
          void 0,
          fl
        );
        break;
      case 11:
        t = ro(
          e,
          t,
          t.pendingProps,
          t.type.render,
          t.ref,
          fl
        );
        break;
      case 5:
        bc(t);
      default:
        So(e, t), t = il = Ks(t, fe), t = go(e, t, fe);
    }
    l.memoizedProps = l.pendingProps, t === null ? $n(l) : il = t;
  }
  function Vu(l, t, e, u) {
    It = Ie = null, bc(t), Cu = null, Sa = 0;
    var a = t.return;
    try {
      if (Ov(
        l,
        a,
        t,
        e,
        fl
      )) {
        Dl = 1, Hn(
          l,
          Nt(e, l.current)
        ), il = null;
        return;
      }
    } catch (n) {
      if (a !== null) throw il = a, n;
      Dl = 1, Hn(
        l,
        Nt(e, l.current)
      ), il = null;
      return;
    }
    t.flags & 32768 ? (rl || u === 1 ? l = !0 : Gu || (fl & 536870912) !== 0 ? l = !1 : (Ne = l = !0, (u === 2 || u === 9 || u === 3 || u === 6) && (u = St.current, u !== null && u.tag === 13 && (u.flags |= 16384))), $o(t, l)) : $n(t);
  }
  function $n(l) {
    var t = l;
    do {
      if ((t.flags & 32768) !== 0) {
        $o(
          t,
          Ne
        );
        return;
      }
      l = t.return;
      var e = Uv(
        t.alternate,
        t,
        fe
      );
      if (e !== null) {
        il = e;
        return;
      }
      if (t = t.sibling, t !== null) {
        il = t;
        return;
      }
      il = t = l;
    } while (t !== null);
    Dl === 0 && (Dl = 5);
  }
  function $o(l, t) {
    do {
      var e = Cv(l.alternate, l);
      if (e !== null) {
        e.flags &= 32767, il = e;
        return;
      }
      if (e = l.return, e !== null && (e.flags |= 32768, e.subtreeFlags = 0, e.deletions = null), !t && (l = l.sibling, l !== null)) {
        il = l;
        return;
      }
      il = l = e;
    } while (l !== null);
    Dl = 6, il = null;
  }
  function Wo(l, t, e, u, a, n, i, f, d) {
    l.cancelPendingCommit = null;
    do
      Wn();
    while (Xl !== 0);
    if ((gl & 6) !== 0) throw Error(s(327));
    if (t !== null) {
      if (t === l.current) throw Error(s(177));
      if (n = t.lanes | t.childLanes, n |= Ki, py(
        l,
        e,
        n,
        i,
        f,
        d
      ), l === ql && (il = ql = null, fl = 0), Xu = t, De = l, se = e, Pc = n, lf = a, Lo = u, (t.subtreeFlags & 10256) !== 0 || (t.flags & 10256) !== 0 ? (l.callbackNode = null, l.callbackPriority = 0, Kv(Fa, function() {
        return td(), null;
      })) : (l.callbackNode = null, l.callbackPriority = 0), u = (t.flags & 13878) !== 0, (t.subtreeFlags & 13878) !== 0 || u) {
        u = q.T, q.T = null, a = j.p, j.p = 2, i = gl, gl |= 4;
        try {
          jv(l, t, e);
        } finally {
          gl = i, j.p = a, q.T = u;
        }
      }
      Xl = 1, Fo(), Io(), Po();
    }
  }
  function Fo() {
    if (Xl === 1) {
      Xl = 0;
      var l = De, t = Xu, e = (t.flags & 13878) !== 0;
      if ((t.subtreeFlags & 13878) !== 0 || e) {
        e = q.T, q.T = null;
        var u = j.p;
        j.p = 2;
        var a = gl;
        gl |= 4;
        try {
          Do(t, l);
          var n = mf, i = Hs(l.containerInfo), f = n.focusedElem, d = n.selectionRange;
          if (i !== f && f && f.ownerDocument && Rs(
            f.ownerDocument.documentElement,
            f
          )) {
            if (d !== null && Gi(f)) {
              var g = d.start, z = d.end;
              if (z === void 0 && (z = g), "selectionStart" in f)
                f.selectionStart = g, f.selectionEnd = Math.min(
                  z,
                  f.value.length
                );
              else {
                var M = f.ownerDocument || document, S = M && M.defaultView || window;
                if (S.getSelection) {
                  var _ = S.getSelection(), Y = f.textContent.length, w = Math.min(d.start, Y), zl = d.end === void 0 ? w : Math.min(d.end, Y);
                  !_.extend && w > zl && (i = zl, zl = w, w = i);
                  var h = js(
                    f,
                    w
                  ), y = js(
                    f,
                    zl
                  );
                  if (h && y && (_.rangeCount !== 1 || _.anchorNode !== h.node || _.anchorOffset !== h.offset || _.focusNode !== y.node || _.focusOffset !== y.offset)) {
                    var m = M.createRange();
                    m.setStart(h.node, h.offset), _.removeAllRanges(), w > zl ? (_.addRange(m), _.extend(y.node, y.offset)) : (m.setEnd(y.node, y.offset), _.addRange(m));
                  }
                }
              }
            }
            for (M = [], _ = f; _ = _.parentNode; )
              _.nodeType === 1 && M.push({
                element: _,
                left: _.scrollLeft,
                top: _.scrollTop
              });
            for (typeof f.focus == "function" && f.focus(), f = 0; f < M.length; f++) {
              var A = M[f];
              A.element.scrollLeft = A.left, A.element.scrollTop = A.top;
            }
          }
          fi = !!hf, mf = hf = null;
        } finally {
          gl = a, j.p = u, q.T = e;
        }
      }
      l.current = t, Xl = 2;
    }
  }
  function Io() {
    if (Xl === 2) {
      Xl = 0;
      var l = De, t = Xu, e = (t.flags & 8772) !== 0;
      if ((t.subtreeFlags & 8772) !== 0 || e) {
        e = q.T, q.T = null;
        var u = j.p;
        j.p = 2;
        var a = gl;
        gl |= 4;
        try {
          To(l, t.alternate, t);
        } finally {
          gl = a, j.p = u, q.T = e;
        }
      }
      Xl = 3;
    }
  }
  function Po() {
    if (Xl === 4 || Xl === 3) {
      Xl = 0, oy();
      var l = De, t = Xu, e = se, u = Lo;
      (t.subtreeFlags & 10256) !== 0 || (t.flags & 10256) !== 0 ? Xl = 5 : (Xl = 0, Xu = De = null, ld(l, l.pendingLanes));
      var a = l.pendingLanes;
      if (a === 0 && (Me = null), _i(e), t = t.stateNode, ht && typeof ht.onCommitFiberRoot == "function")
        try {
          ht.onCommitFiberRoot(
            ta,
            t,
            void 0,
            (t.current.flags & 128) === 128
          );
        } catch {
        }
      if (u !== null) {
        t = q.T, a = j.p, j.p = 2, q.T = null;
        try {
          for (var n = l.onRecoverableError, i = 0; i < u.length; i++) {
            var f = u[i];
            n(f.value, {
              componentStack: f.stack
            });
          }
        } finally {
          q.T = t, j.p = a;
        }
      }
      (se & 3) !== 0 && Wn(), Kt(l), a = l.pendingLanes, (e & 261930) !== 0 && (a & 42) !== 0 ? l === tf ? Ha++ : (Ha = 0, tf = l) : Ha = 0, Ba(0);
    }
  }
  function ld(l, t) {
    (l.pooledCacheLanes &= t) === 0 && (t = l.pooledCache, t != null && (l.pooledCache = null, ga(t)));
  }
  function Wn() {
    return Fo(), Io(), Po(), td();
  }
  function td() {
    if (Xl !== 5) return !1;
    var l = De, t = Pc;
    Pc = 0;
    var e = _i(se), u = q.T, a = j.p;
    try {
      j.p = 32 > e ? 32 : e, q.T = null, e = lf, lf = null;
      var n = De, i = se;
      if (Xl = 0, Xu = De = null, se = 0, (gl & 6) !== 0) throw Error(s(331));
      var f = gl;
      if (gl |= 4, Ho(n.current), Co(
        n,
        n.current,
        i,
        e
      ), gl = f, Ba(0, !1), ht && typeof ht.onPostCommitFiberRoot == "function")
        try {
          ht.onPostCommitFiberRoot(ta, n);
        } catch {
        }
      return !0;
    } finally {
      j.p = a, q.T = u, ld(l, t);
    }
  }
  function ed(l, t, e) {
    t = Nt(e, t), t = Cc(l.stateNode, t, 2), l = qe(l, t, 2), l !== null && (ua(l, 2), Kt(l));
  }
  function pl(l, t, e) {
    if (l.tag === 3)
      ed(l, l, e);
    else
      for (; t !== null; ) {
        if (t.tag === 3) {
          ed(
            t,
            l,
            e
          );
          break;
        } else if (t.tag === 1) {
          var u = t.stateNode;
          if (typeof t.type.getDerivedStateFromError == "function" || typeof u.componentDidCatch == "function" && (Me === null || !Me.has(u))) {
            l = Nt(e, l), e = eo(2), u = qe(t, e, 2), u !== null && (uo(
              e,
              u,
              t,
              l
            ), ua(u, 2), Kt(u));
            break;
          }
        }
        t = t.return;
      }
  }
  function af(l, t, e) {
    var u = l.pingCache;
    if (u === null) {
      u = l.pingCache = new Bv();
      var a = /* @__PURE__ */ new Set();
      u.set(t, a);
    } else
      a = u.get(t), a === void 0 && (a = /* @__PURE__ */ new Set(), u.set(t, a));
    a.has(e) || (Wc = !0, a.add(e), l = Xv.bind(null, l, t, e), t.then(l, l));
  }
  function Xv(l, t, e) {
    var u = l.pingCache;
    u !== null && u.delete(t), l.pingedLanes |= l.suspendedLanes & e, l.warmLanes &= ~e, ql === l && (fl & e) === e && (Dl === 4 || Dl === 3 && (fl & 62914560) === fl && 300 > vt() - Vn ? (gl & 2) === 0 && Zu(l, 0) : Fc |= e, Qu === fl && (Qu = 0)), Kt(l);
  }
  function ud(l, t) {
    t === 0 && (t = Wf()), l = $e(l, t), l !== null && (ua(l, t), Kt(l));
  }
  function Zv(l) {
    var t = l.memoizedState, e = 0;
    t !== null && (e = t.retryLane), ud(l, e);
  }
  function Vv(l, t) {
    var e = 0;
    switch (l.tag) {
      case 31:
      case 13:
        var u = l.stateNode, a = l.memoizedState;
        a !== null && (e = a.retryLane);
        break;
      case 19:
        u = l.stateNode;
        break;
      case 22:
        u = l.stateNode._retryCache;
        break;
      default:
        throw Error(s(314));
    }
    u !== null && u.delete(t), ud(l, e);
  }
  function Kv(l, t) {
    return gi(l, t);
  }
  var Fn = null, Ku = null, nf = !1, In = !1, cf = !1, Ce = 0;
  function Kt(l) {
    l !== Ku && l.next === null && (Ku === null ? Fn = Ku = l : Ku = Ku.next = l), In = !0, nf || (nf = !0, wv());
  }
  function Ba(l, t) {
    if (!cf && In) {
      cf = !0;
      do
        for (var e = !1, u = Fn; u !== null; ) {
          if (l !== 0) {
            var a = u.pendingLanes;
            if (a === 0) var n = 0;
            else {
              var i = u.suspendedLanes, f = u.pingedLanes;
              n = (1 << 31 - mt(42 | l) + 1) - 1, n &= a & ~(i & ~f), n = n & 201326741 ? n & 201326741 | 1 : n ? n | 2 : 0;
            }
            n !== 0 && (e = !0, cd(u, n));
          } else
            n = fl, n = tn(
              u,
              u === ql ? n : 0,
              u.cancelPendingCommit !== null || u.timeoutHandle !== -1
            ), (n & 3) === 0 || ea(u, n) || (e = !0, cd(u, n));
          u = u.next;
        }
      while (e);
      cf = !1;
    }
  }
  function Jv() {
    ad();
  }
  function ad() {
    In = nf = !1;
    var l = 0;
    Ce !== 0 && uh() && (l = Ce);
    for (var t = vt(), e = null, u = Fn; u !== null; ) {
      var a = u.next, n = nd(u, t);
      n === 0 ? (u.next = null, e === null ? Fn = a : e.next = a, a === null && (Ku = e)) : (e = u, (l !== 0 || (n & 3) !== 0) && (In = !0)), u = a;
    }
    Xl !== 0 && Xl !== 5 || Ba(l), Ce !== 0 && (Ce = 0);
  }
  function nd(l, t) {
    for (var e = l.suspendedLanes, u = l.pingedLanes, a = l.expirationTimes, n = l.pendingLanes & -62914561; 0 < n; ) {
      var i = 31 - mt(n), f = 1 << i, d = a[i];
      d === -1 ? ((f & e) === 0 || (f & u) !== 0) && (a[i] = Sy(f, t)) : d <= t && (l.expiredLanes |= f), n &= ~f;
    }
    if (t = ql, e = fl, e = tn(
      l,
      l === t ? e : 0,
      l.cancelPendingCommit !== null || l.timeoutHandle !== -1
    ), u = l.callbackNode, e === 0 || l === t && (Sl === 2 || Sl === 9) || l.cancelPendingCommit !== null)
      return u !== null && u !== null && bi(u), l.callbackNode = null, l.callbackPriority = 0;
    if ((e & 3) === 0 || ea(l, e)) {
      if (t = e & -e, t === l.callbackPriority) return t;
      switch (u !== null && bi(u), _i(e)) {
        case 2:
        case 8:
          e = kf;
          break;
        case 32:
          e = Fa;
          break;
        case 268435456:
          e = $f;
          break;
        default:
          e = Fa;
      }
      return u = id.bind(null, l), e = gi(e, u), l.callbackPriority = t, l.callbackNode = e, t;
    }
    return u !== null && u !== null && bi(u), l.callbackPriority = 2, l.callbackNode = null, 2;
  }
  function id(l, t) {
    if (Xl !== 0 && Xl !== 5)
      return l.callbackNode = null, l.callbackPriority = 0, null;
    var e = l.callbackNode;
    if (Wn() && l.callbackNode !== e)
      return null;
    var u = fl;
    return u = tn(
      l,
      l === ql ? u : 0,
      l.cancelPendingCommit !== null || l.timeoutHandle !== -1
    ), u === 0 ? null : (Qo(l, u, t), nd(l, vt()), l.callbackNode != null && l.callbackNode === e ? id.bind(null, l) : null);
  }
  function cd(l, t) {
    if (Wn()) return null;
    Qo(l, t, !0);
  }
  function wv() {
    nh(function() {
      (gl & 6) !== 0 ? gi(
        wf,
        Jv
      ) : ad();
    });
  }
  function ff() {
    if (Ce === 0) {
      var l = Mu;
      l === 0 && (l = Ia, Ia <<= 1, (Ia & 261888) === 0 && (Ia = 256)), Ce = l;
    }
    return Ce;
  }
  function fd(l) {
    return l == null || typeof l == "symbol" || typeof l == "boolean" ? null : typeof l == "function" ? l : nn("" + l);
  }
  function sd(l, t) {
    var e = t.ownerDocument.createElement("input");
    return e.name = t.name, e.value = t.value, l.id && e.setAttribute("form", l.id), t.parentNode.insertBefore(e, t), l = new FormData(l), e.parentNode.removeChild(e), l;
  }
  function kv(l, t, e, u, a) {
    if (t === "submit" && e && e.stateNode === a) {
      var n = fd(
        (a[ut] || null).action
      ), i = u.submitter;
      i && (t = (t = i[ut] || null) ? fd(t.formAction) : i.getAttribute("formAction"), t !== null && (n = t, i = null));
      var f = new rn(
        "action",
        "action",
        null,
        u,
        a
      );
      l.push({
        event: f,
        listeners: [
          {
            instance: null,
            listener: function() {
              if (u.defaultPrevented) {
                if (Ce !== 0) {
                  var d = i ? sd(a, i) : new FormData(a);
                  xc(
                    e,
                    {
                      pending: !0,
                      data: d,
                      method: a.method,
                      action: n
                    },
                    null,
                    d
                  );
                }
              } else
                typeof n == "function" && (f.preventDefault(), d = i ? sd(a, i) : new FormData(a), xc(
                  e,
                  {
                    pending: !0,
                    data: d,
                    method: a.method,
                    action: n
                  },
                  n,
                  d
                ));
            },
            currentTarget: a
          }
        ]
      });
    }
  }
  for (var sf = 0; sf < Vi.length; sf++) {
    var rf = Vi[sf], $v = rf.toLowerCase(), Wv = rf[0].toUpperCase() + rf.slice(1);
    Ht(
      $v,
      "on" + Wv
    );
  }
  Ht(Ls, "onAnimationEnd"), Ht(Gs, "onAnimationIteration"), Ht(Qs, "onAnimationStart"), Ht("dblclick", "onDoubleClick"), Ht("focusin", "onFocus"), Ht("focusout", "onBlur"), Ht(dv, "onTransitionRun"), Ht(yv, "onTransitionStart"), Ht(vv, "onTransitionCancel"), Ht(Xs, "onTransitionEnd"), mu("onMouseEnter", ["mouseout", "mouseover"]), mu("onMouseLeave", ["mouseout", "mouseover"]), mu("onPointerEnter", ["pointerout", "pointerover"]), mu("onPointerLeave", ["pointerout", "pointerover"]), Ke(
    "onChange",
    "change click focusin focusout input keydown keyup selectionchange".split(" ")
  ), Ke(
    "onSelect",
    "focusout contextmenu dragend focusin keydown keyup mousedown mouseup selectionchange".split(
      " "
    )
  ), Ke("onBeforeInput", [
    "compositionend",
    "keypress",
    "textInput",
    "paste"
  ]), Ke(
    "onCompositionEnd",
    "compositionend focusout keydown keypress keyup mousedown".split(" ")
  ), Ke(
    "onCompositionStart",
    "compositionstart focusout keydown keypress keyup mousedown".split(" ")
  ), Ke(
    "onCompositionUpdate",
    "compositionupdate focusout keydown keypress keyup mousedown".split(" ")
  );
  var Ya = "abort canplay canplaythrough durationchange emptied encrypted ended error loadeddata loadedmetadata loadstart pause play playing progress ratechange resize seeked seeking stalled suspend timeupdate volumechange waiting".split(
    " "
  ), Fv = new Set(
    "beforetoggle cancel close invalid load scroll scrollend toggle".split(" ").concat(Ya)
  );
  function rd(l, t) {
    t = (t & 4) !== 0;
    for (var e = 0; e < l.length; e++) {
      var u = l[e], a = u.event;
      u = u.listeners;
      l: {
        var n = void 0;
        if (t)
          for (var i = u.length - 1; 0 <= i; i--) {
            var f = u[i], d = f.instance, g = f.currentTarget;
            if (f = f.listener, d !== n && a.isPropagationStopped())
              break l;
            n = f, a.currentTarget = g;
            try {
              n(a);
            } catch (z) {
              yn(z);
            }
            a.currentTarget = null, n = d;
          }
        else
          for (i = 0; i < u.length; i++) {
            if (f = u[i], d = f.instance, g = f.currentTarget, f = f.listener, d !== n && a.isPropagationStopped())
              break l;
            n = f, a.currentTarget = g;
            try {
              n(a);
            } catch (z) {
              yn(z);
            }
            a.currentTarget = null, n = d;
          }
      }
    }
  }
  function cl(l, t) {
    var e = t[Ei];
    e === void 0 && (e = t[Ei] = /* @__PURE__ */ new Set());
    var u = l + "__bubble";
    e.has(u) || (od(t, l, 2, !1), e.add(u));
  }
  function of(l, t, e) {
    var u = 0;
    t && (u |= 4), od(
      e,
      l,
      u,
      t
    );
  }
  var Pn = "_reactListening" + Math.random().toString(36).slice(2);
  function df(l) {
    if (!l[Pn]) {
      l[Pn] = !0, us.forEach(function(e) {
        e !== "selectionchange" && (Fv.has(e) || of(e, !1, l), of(e, !0, l));
      });
      var t = l.nodeType === 9 ? l : l.ownerDocument;
      t === null || t[Pn] || (t[Pn] = !0, of("selectionchange", !1, t));
    }
  }
  function od(l, t, e, u) {
    switch (Gd(t)) {
      case 2:
        var a = Ah;
        break;
      case 8:
        a = Th;
        break;
      default:
        a = xf;
    }
    e = a.bind(
      null,
      t,
      e,
      l
    ), a = void 0, !Di || t !== "touchstart" && t !== "touchmove" && t !== "wheel" || (a = !0), u ? a !== void 0 ? l.addEventListener(t, e, {
      capture: !0,
      passive: a
    }) : l.addEventListener(t, e, !0) : a !== void 0 ? l.addEventListener(t, e, {
      passive: a
    }) : l.addEventListener(t, e, !1);
  }
  function yf(l, t, e, u, a) {
    var n = u;
    if ((t & 1) === 0 && (t & 2) === 0 && u !== null)
      l: for (; ; ) {
        if (u === null) return;
        var i = u.tag;
        if (i === 3 || i === 4) {
          var f = u.stateNode.containerInfo;
          if (f === a) break;
          if (i === 4)
            for (i = u.return; i !== null; ) {
              var d = i.tag;
              if ((d === 3 || d === 4) && i.stateNode.containerInfo === a)
                return;
              i = i.return;
            }
          for (; f !== null; ) {
            if (i = yu(f), i === null) return;
            if (d = i.tag, d === 5 || d === 6 || d === 26 || d === 27) {
              u = n = i;
              continue l;
            }
            f = f.parentNode;
          }
        }
        u = u.return;
      }
    hs(function() {
      var g = n, z = Oi(e), M = [];
      l: {
        var S = Zs.get(l);
        if (S !== void 0) {
          var _ = rn, Y = l;
          switch (l) {
            case "keypress":
              if (fn(e) === 0) break l;
            case "keydown":
            case "keyup":
              _ = Vy;
              break;
            case "focusin":
              Y = "focus", _ = Ri;
              break;
            case "focusout":
              Y = "blur", _ = Ri;
              break;
            case "beforeblur":
            case "afterblur":
              _ = Ri;
              break;
            case "click":
              if (e.button === 2) break l;
            case "auxclick":
            case "dblclick":
            case "mousedown":
            case "mousemove":
            case "mouseup":
            case "mouseout":
            case "mouseover":
            case "contextmenu":
              _ = bs;
              break;
            case "drag":
            case "dragend":
            case "dragenter":
            case "dragexit":
            case "dragleave":
            case "dragover":
            case "dragstart":
            case "drop":
              _ = Uy;
              break;
            case "touchcancel":
            case "touchend":
            case "touchmove":
            case "touchstart":
              _ = wy;
              break;
            case Ls:
            case Gs:
            case Qs:
              _ = Ry;
              break;
            case Xs:
              _ = $y;
              break;
            case "scroll":
            case "scrollend":
              _ = My;
              break;
            case "wheel":
              _ = Fy;
              break;
            case "copy":
            case "cut":
            case "paste":
              _ = By;
              break;
            case "gotpointercapture":
            case "lostpointercapture":
            case "pointercancel":
            case "pointerdown":
            case "pointermove":
            case "pointerout":
            case "pointerover":
            case "pointerup":
              _ = ps;
              break;
            case "toggle":
            case "beforetoggle":
              _ = Py;
          }
          var w = (t & 4) !== 0, zl = !w && (l === "scroll" || l === "scrollend"), h = w ? S !== null ? S + "Capture" : null : S;
          w = [];
          for (var y = g, m; y !== null; ) {
            var A = y;
            if (m = A.stateNode, A = A.tag, A !== 5 && A !== 26 && A !== 27 || m === null || h === null || (A = ia(y, h), A != null && w.push(
              La(y, A, m)
            )), zl) break;
            y = y.return;
          }
          0 < w.length && (S = new _(
            S,
            Y,
            null,
            e,
            z
          ), M.push({ event: S, listeners: w }));
        }
      }
      if ((t & 7) === 0) {
        l: {
          if (S = l === "mouseover" || l === "pointerover", _ = l === "mouseout" || l === "pointerout", S && e !== Ni && (Y = e.relatedTarget || e.fromElement) && (yu(Y) || Y[du]))
            break l;
          if ((_ || S) && (S = z.window === z ? z : (S = z.ownerDocument) ? S.defaultView || S.parentWindow : window, _ ? (Y = e.relatedTarget || e.toElement, _ = g, Y = Y ? yu(Y) : null, Y !== null && (zl = E(Y), w = Y.tag, Y !== zl || w !== 5 && w !== 27 && w !== 6) && (Y = null)) : (_ = null, Y = g), _ !== Y)) {
            if (w = bs, A = "onMouseLeave", h = "onMouseEnter", y = "mouse", (l === "pointerout" || l === "pointerover") && (w = ps, A = "onPointerLeave", h = "onPointerEnter", y = "pointer"), zl = _ == null ? S : na(_), m = Y == null ? S : na(Y), S = new w(
              A,
              y + "leave",
              _,
              e,
              z
            ), S.target = zl, S.relatedTarget = m, A = null, yu(z) === g && (w = new w(
              h,
              y + "enter",
              Y,
              e,
              z
            ), w.target = m, w.relatedTarget = zl, A = w), zl = A, _ && Y)
              t: {
                for (w = Iv, h = _, y = Y, m = 0, A = h; A; A = w(A))
                  m++;
                A = 0;
                for (var V = y; V; V = w(V))
                  A++;
                for (; 0 < m - A; )
                  h = w(h), m--;
                for (; 0 < A - m; )
                  y = w(y), A--;
                for (; m--; ) {
                  if (h === y || y !== null && h === y.alternate) {
                    w = h;
                    break t;
                  }
                  h = w(h), y = w(y);
                }
                w = null;
              }
            else w = null;
            _ !== null && dd(
              M,
              S,
              _,
              w,
              !1
            ), Y !== null && zl !== null && dd(
              M,
              zl,
              Y,
              w,
              !0
            );
          }
        }
        l: {
          if (S = g ? na(g) : window, _ = S.nodeName && S.nodeName.toLowerCase(), _ === "select" || _ === "input" && S.type === "file")
            var vl = Ns;
          else if (Ts(S))
            if (Os)
              vl = sv;
            else {
              vl = cv;
              var Q = iv;
            }
          else
            _ = S.nodeName, !_ || _.toLowerCase() !== "input" || S.type !== "checkbox" && S.type !== "radio" ? g && xi(g.elementType) && (vl = Ns) : vl = fv;
          if (vl && (vl = vl(l, g))) {
            xs(
              M,
              vl,
              e,
              z
            );
            break l;
          }
          Q && Q(l, S, g), l === "focusout" && g && S.type === "number" && g.memoizedProps.value != null && Ti(S, "number", S.value);
        }
        switch (Q = g ? na(g) : window, l) {
          case "focusin":
            (Ts(Q) || Q.contentEditable === "true") && (Eu = Q, Qi = g, va = null);
            break;
          case "focusout":
            va = Qi = Eu = null;
            break;
          case "mousedown":
            Xi = !0;
            break;
          case "contextmenu":
          case "mouseup":
          case "dragend":
            Xi = !1, Bs(M, e, z);
            break;
          case "selectionchange":
            if (ov) break;
          case "keydown":
          case "keyup":
            Bs(M, e, z);
        }
        var ul;
        if (Bi)
          l: {
            switch (l) {
              case "compositionstart":
                var sl = "onCompositionStart";
                break l;
              case "compositionend":
                sl = "onCompositionEnd";
                break l;
              case "compositionupdate":
                sl = "onCompositionUpdate";
                break l;
            }
            sl = void 0;
          }
        else
          _u ? qs(l, e) && (sl = "onCompositionEnd") : l === "keydown" && e.keyCode === 229 && (sl = "onCompositionStart");
        sl && (_s && e.locale !== "ko" && (_u || sl !== "onCompositionStart" ? sl === "onCompositionEnd" && _u && (ul = ms()) : (ge = z, Ui = "value" in ge ? ge.value : ge.textContent, _u = !0)), Q = li(g, sl), 0 < Q.length && (sl = new Ss(
          sl,
          l,
          null,
          e,
          z
        ), M.push({ event: sl, listeners: Q }), ul ? sl.data = ul : (ul = As(e), ul !== null && (sl.data = ul)))), (ul = tv ? ev(l, e) : uv(l, e)) && (sl = li(g, "onBeforeInput"), 0 < sl.length && (Q = new Ss(
          "onBeforeInput",
          "beforeinput",
          null,
          e,
          z
        ), M.push({
          event: Q,
          listeners: sl
        }), Q.data = ul)), kv(
          M,
          l,
          g,
          e,
          z
        );
      }
      rd(M, t);
    });
  }
  function La(l, t, e) {
    return {
      instance: l,
      listener: t,
      currentTarget: e
    };
  }
  function li(l, t) {
    for (var e = t + "Capture", u = []; l !== null; ) {
      var a = l, n = a.stateNode;
      if (a = a.tag, a !== 5 && a !== 26 && a !== 27 || n === null || (a = ia(l, e), a != null && u.unshift(
        La(l, a, n)
      ), a = ia(l, t), a != null && u.push(
        La(l, a, n)
      )), l.tag === 3) return u;
      l = l.return;
    }
    return [];
  }
  function Iv(l) {
    if (l === null) return null;
    do
      l = l.return;
    while (l && l.tag !== 5 && l.tag !== 27);
    return l || null;
  }
  function dd(l, t, e, u, a) {
    for (var n = t._reactName, i = []; e !== null && e !== u; ) {
      var f = e, d = f.alternate, g = f.stateNode;
      if (f = f.tag, d !== null && d === u) break;
      f !== 5 && f !== 26 && f !== 27 || g === null || (d = g, a ? (g = ia(e, n), g != null && i.unshift(
        La(e, g, d)
      )) : a || (g = ia(e, n), g != null && i.push(
        La(e, g, d)
      ))), e = e.return;
    }
    i.length !== 0 && l.push({ event: t, listeners: i });
  }
  var Pv = /\r\n?/g, lh = /\u0000|\uFFFD/g;
  function yd(l) {
    return (typeof l == "string" ? l : "" + l).replace(Pv, `
`).replace(lh, "");
  }
  function vd(l, t) {
    return t = yd(t), yd(l) === t;
  }
  function El(l, t, e, u, a, n) {
    switch (e) {
      case "children":
        typeof u == "string" ? t === "body" || t === "textarea" && u === "" || bu(l, u) : (typeof u == "number" || typeof u == "bigint") && t !== "body" && bu(l, "" + u);
        break;
      case "className":
        un(l, "class", u);
        break;
      case "tabIndex":
        un(l, "tabindex", u);
        break;
      case "dir":
      case "role":
      case "viewBox":
      case "width":
      case "height":
        un(l, e, u);
        break;
      case "style":
        ys(l, u, n);
        break;
      case "data":
        if (t !== "object") {
          un(l, "data", u);
          break;
        }
      case "src":
      case "href":
        if (u === "" && (t !== "a" || e !== "href")) {
          l.removeAttribute(e);
          break;
        }
        if (u == null || typeof u == "function" || typeof u == "symbol" || typeof u == "boolean") {
          l.removeAttribute(e);
          break;
        }
        u = nn("" + u), l.setAttribute(e, u);
        break;
      case "action":
      case "formAction":
        if (typeof u == "function") {
          l.setAttribute(
            e,
            "javascript:throw new Error('A React form was unexpectedly submitted. If you called form.submit() manually, consider using form.requestSubmit() instead. If you\\'re trying to use event.stopPropagation() in a submit event handler, consider also calling event.preventDefault().')"
          );
          break;
        } else
          typeof n == "function" && (e === "formAction" ? (t !== "input" && El(l, t, "name", a.name, a, null), El(
            l,
            t,
            "formEncType",
            a.formEncType,
            a,
            null
          ), El(
            l,
            t,
            "formMethod",
            a.formMethod,
            a,
            null
          ), El(
            l,
            t,
            "formTarget",
            a.formTarget,
            a,
            null
          )) : (El(l, t, "encType", a.encType, a, null), El(l, t, "method", a.method, a, null), El(l, t, "target", a.target, a, null)));
        if (u == null || typeof u == "symbol" || typeof u == "boolean") {
          l.removeAttribute(e);
          break;
        }
        u = nn("" + u), l.setAttribute(e, u);
        break;
      case "onClick":
        u != null && (l.onclick = kt);
        break;
      case "onScroll":
        u != null && cl("scroll", l);
        break;
      case "onScrollEnd":
        u != null && cl("scrollend", l);
        break;
      case "dangerouslySetInnerHTML":
        if (u != null) {
          if (typeof u != "object" || !("__html" in u))
            throw Error(s(61));
          if (e = u.__html, e != null) {
            if (a.children != null) throw Error(s(60));
            l.innerHTML = e;
          }
        }
        break;
      case "multiple":
        l.multiple = u && typeof u != "function" && typeof u != "symbol";
        break;
      case "muted":
        l.muted = u && typeof u != "function" && typeof u != "symbol";
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
        if (u == null || typeof u == "function" || typeof u == "boolean" || typeof u == "symbol") {
          l.removeAttribute("xlink:href");
          break;
        }
        e = nn("" + u), l.setAttributeNS(
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
        u != null && typeof u != "function" && typeof u != "symbol" ? l.setAttribute(e, "" + u) : l.removeAttribute(e);
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
        u && typeof u != "function" && typeof u != "symbol" ? l.setAttribute(e, "") : l.removeAttribute(e);
        break;
      case "capture":
      case "download":
        u === !0 ? l.setAttribute(e, "") : u !== !1 && u != null && typeof u != "function" && typeof u != "symbol" ? l.setAttribute(e, u) : l.removeAttribute(e);
        break;
      case "cols":
      case "rows":
      case "size":
      case "span":
        u != null && typeof u != "function" && typeof u != "symbol" && !isNaN(u) && 1 <= u ? l.setAttribute(e, u) : l.removeAttribute(e);
        break;
      case "rowSpan":
      case "start":
        u == null || typeof u == "function" || typeof u == "symbol" || isNaN(u) ? l.removeAttribute(e) : l.setAttribute(e, u);
        break;
      case "popover":
        cl("beforetoggle", l), cl("toggle", l), en(l, "popover", u);
        break;
      case "xlinkActuate":
        wt(
          l,
          "http://www.w3.org/1999/xlink",
          "xlink:actuate",
          u
        );
        break;
      case "xlinkArcrole":
        wt(
          l,
          "http://www.w3.org/1999/xlink",
          "xlink:arcrole",
          u
        );
        break;
      case "xlinkRole":
        wt(
          l,
          "http://www.w3.org/1999/xlink",
          "xlink:role",
          u
        );
        break;
      case "xlinkShow":
        wt(
          l,
          "http://www.w3.org/1999/xlink",
          "xlink:show",
          u
        );
        break;
      case "xlinkTitle":
        wt(
          l,
          "http://www.w3.org/1999/xlink",
          "xlink:title",
          u
        );
        break;
      case "xlinkType":
        wt(
          l,
          "http://www.w3.org/1999/xlink",
          "xlink:type",
          u
        );
        break;
      case "xmlBase":
        wt(
          l,
          "http://www.w3.org/XML/1998/namespace",
          "xml:base",
          u
        );
        break;
      case "xmlLang":
        wt(
          l,
          "http://www.w3.org/XML/1998/namespace",
          "xml:lang",
          u
        );
        break;
      case "xmlSpace":
        wt(
          l,
          "http://www.w3.org/XML/1998/namespace",
          "xml:space",
          u
        );
        break;
      case "is":
        en(l, "is", u);
        break;
      case "innerText":
      case "textContent":
        break;
      default:
        (!(2 < e.length) || e[0] !== "o" && e[0] !== "O" || e[1] !== "n" && e[1] !== "N") && (e = Ny.get(e) || e, en(l, e, u));
    }
  }
  function vf(l, t, e, u, a, n) {
    switch (e) {
      case "style":
        ys(l, u, n);
        break;
      case "dangerouslySetInnerHTML":
        if (u != null) {
          if (typeof u != "object" || !("__html" in u))
            throw Error(s(61));
          if (e = u.__html, e != null) {
            if (a.children != null) throw Error(s(60));
            l.innerHTML = e;
          }
        }
        break;
      case "children":
        typeof u == "string" ? bu(l, u) : (typeof u == "number" || typeof u == "bigint") && bu(l, "" + u);
        break;
      case "onScroll":
        u != null && cl("scroll", l);
        break;
      case "onScrollEnd":
        u != null && cl("scrollend", l);
        break;
      case "onClick":
        u != null && (l.onclick = kt);
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
        if (!as.hasOwnProperty(e))
          l: {
            if (e[0] === "o" && e[1] === "n" && (a = e.endsWith("Capture"), t = e.slice(2, a ? e.length - 7 : void 0), n = l[ut] || null, n = n != null ? n[e] : null, typeof n == "function" && l.removeEventListener(t, n, a), typeof u == "function")) {
              typeof n != "function" && n !== null && (e in l ? l[e] = null : l.hasAttribute(e) && l.removeAttribute(e)), l.addEventListener(t, u, a);
              break l;
            }
            e in l ? l[e] = u : u === !0 ? l.setAttribute(e, "") : en(l, e, u);
          }
    }
  }
  function Fl(l, t, e) {
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
        cl("error", l), cl("load", l);
        var u = !1, a = !1, n;
        for (n in e)
          if (e.hasOwnProperty(n)) {
            var i = e[n];
            if (i != null)
              switch (n) {
                case "src":
                  u = !0;
                  break;
                case "srcSet":
                  a = !0;
                  break;
                case "children":
                case "dangerouslySetInnerHTML":
                  throw Error(s(137, t));
                default:
                  El(l, t, n, i, e, null);
              }
          }
        a && El(l, t, "srcSet", e.srcSet, e, null), u && El(l, t, "src", e.src, e, null);
        return;
      case "input":
        cl("invalid", l);
        var f = n = i = a = null, d = null, g = null;
        for (u in e)
          if (e.hasOwnProperty(u)) {
            var z = e[u];
            if (z != null)
              switch (u) {
                case "name":
                  a = z;
                  break;
                case "type":
                  i = z;
                  break;
                case "checked":
                  d = z;
                  break;
                case "defaultChecked":
                  g = z;
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
                    throw Error(s(137, t));
                  break;
                default:
                  El(l, t, u, z, e, null);
              }
          }
        ss(
          l,
          n,
          f,
          d,
          g,
          i,
          a,
          !1
        );
        return;
      case "select":
        cl("invalid", l), u = i = n = null;
        for (a in e)
          if (e.hasOwnProperty(a) && (f = e[a], f != null))
            switch (a) {
              case "value":
                n = f;
                break;
              case "defaultValue":
                i = f;
                break;
              case "multiple":
                u = f;
              default:
                El(l, t, a, f, e, null);
            }
        t = n, e = i, l.multiple = !!u, t != null ? gu(l, !!u, t, !1) : e != null && gu(l, !!u, e, !0);
        return;
      case "textarea":
        cl("invalid", l), n = a = u = null;
        for (i in e)
          if (e.hasOwnProperty(i) && (f = e[i], f != null))
            switch (i) {
              case "value":
                u = f;
                break;
              case "defaultValue":
                a = f;
                break;
              case "children":
                n = f;
                break;
              case "dangerouslySetInnerHTML":
                if (f != null) throw Error(s(91));
                break;
              default:
                El(l, t, i, f, e, null);
            }
        os(l, u, a, n);
        return;
      case "option":
        for (d in e)
          if (e.hasOwnProperty(d) && (u = e[d], u != null))
            switch (d) {
              case "selected":
                l.selected = u && typeof u != "function" && typeof u != "symbol";
                break;
              default:
                El(l, t, d, u, e, null);
            }
        return;
      case "dialog":
        cl("beforetoggle", l), cl("toggle", l), cl("cancel", l), cl("close", l);
        break;
      case "iframe":
      case "object":
        cl("load", l);
        break;
      case "video":
      case "audio":
        for (u = 0; u < Ya.length; u++)
          cl(Ya[u], l);
        break;
      case "image":
        cl("error", l), cl("load", l);
        break;
      case "details":
        cl("toggle", l);
        break;
      case "embed":
      case "source":
      case "link":
        cl("error", l), cl("load", l);
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
        for (g in e)
          if (e.hasOwnProperty(g) && (u = e[g], u != null))
            switch (g) {
              case "children":
              case "dangerouslySetInnerHTML":
                throw Error(s(137, t));
              default:
                El(l, t, g, u, e, null);
            }
        return;
      default:
        if (xi(t)) {
          for (z in e)
            e.hasOwnProperty(z) && (u = e[z], u !== void 0 && vf(
              l,
              t,
              z,
              u,
              e,
              void 0
            ));
          return;
        }
    }
    for (f in e)
      e.hasOwnProperty(f) && (u = e[f], u != null && El(l, t, f, u, e, null));
  }
  function th(l, t, e, u) {
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
        var a = null, n = null, i = null, f = null, d = null, g = null, z = null;
        for (_ in e) {
          var M = e[_];
          if (e.hasOwnProperty(_) && M != null)
            switch (_) {
              case "checked":
                break;
              case "value":
                break;
              case "defaultValue":
                d = M;
              default:
                u.hasOwnProperty(_) || El(l, t, _, null, u, M);
            }
        }
        for (var S in u) {
          var _ = u[S];
          if (M = e[S], u.hasOwnProperty(S) && (_ != null || M != null))
            switch (S) {
              case "type":
                n = _;
                break;
              case "name":
                a = _;
                break;
              case "checked":
                g = _;
                break;
              case "defaultChecked":
                z = _;
                break;
              case "value":
                i = _;
                break;
              case "defaultValue":
                f = _;
                break;
              case "children":
              case "dangerouslySetInnerHTML":
                if (_ != null)
                  throw Error(s(137, t));
                break;
              default:
                _ !== M && El(
                  l,
                  t,
                  S,
                  _,
                  u,
                  M
                );
            }
        }
        Ai(
          l,
          i,
          f,
          d,
          g,
          z,
          n,
          a
        );
        return;
      case "select":
        _ = i = f = S = null;
        for (n in e)
          if (d = e[n], e.hasOwnProperty(n) && d != null)
            switch (n) {
              case "value":
                break;
              case "multiple":
                _ = d;
              default:
                u.hasOwnProperty(n) || El(
                  l,
                  t,
                  n,
                  null,
                  u,
                  d
                );
            }
        for (a in u)
          if (n = u[a], d = e[a], u.hasOwnProperty(a) && (n != null || d != null))
            switch (a) {
              case "value":
                S = n;
                break;
              case "defaultValue":
                f = n;
                break;
              case "multiple":
                i = n;
              default:
                n !== d && El(
                  l,
                  t,
                  a,
                  n,
                  u,
                  d
                );
            }
        t = f, e = i, u = _, S != null ? gu(l, !!e, S, !1) : !!u != !!e && (t != null ? gu(l, !!e, t, !0) : gu(l, !!e, e ? [] : "", !1));
        return;
      case "textarea":
        _ = S = null;
        for (f in e)
          if (a = e[f], e.hasOwnProperty(f) && a != null && !u.hasOwnProperty(f))
            switch (f) {
              case "value":
                break;
              case "children":
                break;
              default:
                El(l, t, f, null, u, a);
            }
        for (i in u)
          if (a = u[i], n = e[i], u.hasOwnProperty(i) && (a != null || n != null))
            switch (i) {
              case "value":
                S = a;
                break;
              case "defaultValue":
                _ = a;
                break;
              case "children":
                break;
              case "dangerouslySetInnerHTML":
                if (a != null) throw Error(s(91));
                break;
              default:
                a !== n && El(l, t, i, a, u, n);
            }
        rs(l, S, _);
        return;
      case "option":
        for (var Y in e)
          if (S = e[Y], e.hasOwnProperty(Y) && S != null && !u.hasOwnProperty(Y))
            switch (Y) {
              case "selected":
                l.selected = !1;
                break;
              default:
                El(
                  l,
                  t,
                  Y,
                  null,
                  u,
                  S
                );
            }
        for (d in u)
          if (S = u[d], _ = e[d], u.hasOwnProperty(d) && S !== _ && (S != null || _ != null))
            switch (d) {
              case "selected":
                l.selected = S && typeof S != "function" && typeof S != "symbol";
                break;
              default:
                El(
                  l,
                  t,
                  d,
                  S,
                  u,
                  _
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
        for (var w in e)
          S = e[w], e.hasOwnProperty(w) && S != null && !u.hasOwnProperty(w) && El(l, t, w, null, u, S);
        for (g in u)
          if (S = u[g], _ = e[g], u.hasOwnProperty(g) && S !== _ && (S != null || _ != null))
            switch (g) {
              case "children":
              case "dangerouslySetInnerHTML":
                if (S != null)
                  throw Error(s(137, t));
                break;
              default:
                El(
                  l,
                  t,
                  g,
                  S,
                  u,
                  _
                );
            }
        return;
      default:
        if (xi(t)) {
          for (var zl in e)
            S = e[zl], e.hasOwnProperty(zl) && S !== void 0 && !u.hasOwnProperty(zl) && vf(
              l,
              t,
              zl,
              void 0,
              u,
              S
            );
          for (z in u)
            S = u[z], _ = e[z], !u.hasOwnProperty(z) || S === _ || S === void 0 && _ === void 0 || vf(
              l,
              t,
              z,
              S,
              u,
              _
            );
          return;
        }
    }
    for (var h in e)
      S = e[h], e.hasOwnProperty(h) && S != null && !u.hasOwnProperty(h) && El(l, t, h, null, u, S);
    for (M in u)
      S = u[M], _ = e[M], !u.hasOwnProperty(M) || S === _ || S == null && _ == null || El(l, t, M, S, u, _);
  }
  function hd(l) {
    switch (l) {
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
  function eh() {
    if (typeof performance.getEntriesByType == "function") {
      for (var l = 0, t = 0, e = performance.getEntriesByType("resource"), u = 0; u < e.length; u++) {
        var a = e[u], n = a.transferSize, i = a.initiatorType, f = a.duration;
        if (n && f && hd(i)) {
          for (i = 0, f = a.responseEnd, u += 1; u < e.length; u++) {
            var d = e[u], g = d.startTime;
            if (g > f) break;
            var z = d.transferSize, M = d.initiatorType;
            z && hd(M) && (d = d.responseEnd, i += z * (d < f ? 1 : (f - g) / (d - g)));
          }
          if (--u, t += 8 * (n + i) / (a.duration / 1e3), l++, 10 < l) break;
        }
      }
      if (0 < l) return t / l / 1e6;
    }
    return navigator.connection && (l = navigator.connection.downlink, typeof l == "number") ? l : 5;
  }
  var hf = null, mf = null;
  function ti(l) {
    return l.nodeType === 9 ? l : l.ownerDocument;
  }
  function md(l) {
    switch (l) {
      case "http://www.w3.org/2000/svg":
        return 1;
      case "http://www.w3.org/1998/Math/MathML":
        return 2;
      default:
        return 0;
    }
  }
  function gd(l, t) {
    if (l === 0)
      switch (t) {
        case "svg":
          return 1;
        case "math":
          return 2;
        default:
          return 0;
      }
    return l === 1 && t === "foreignObject" ? 0 : l;
  }
  function gf(l, t) {
    return l === "textarea" || l === "noscript" || typeof t.children == "string" || typeof t.children == "number" || typeof t.children == "bigint" || typeof t.dangerouslySetInnerHTML == "object" && t.dangerouslySetInnerHTML !== null && t.dangerouslySetInnerHTML.__html != null;
  }
  var bf = null;
  function uh() {
    var l = window.event;
    return l && l.type === "popstate" ? l === bf ? !1 : (bf = l, !0) : (bf = null, !1);
  }
  var bd = typeof setTimeout == "function" ? setTimeout : void 0, ah = typeof clearTimeout == "function" ? clearTimeout : void 0, Sd = typeof Promise == "function" ? Promise : void 0, nh = typeof queueMicrotask == "function" ? queueMicrotask : typeof Sd < "u" ? function(l) {
    return Sd.resolve(null).then(l).catch(ih);
  } : bd;
  function ih(l) {
    setTimeout(function() {
      throw l;
    });
  }
  function je(l) {
    return l === "head";
  }
  function pd(l, t) {
    var e = t, u = 0;
    do {
      var a = e.nextSibling;
      if (l.removeChild(e), a && a.nodeType === 8)
        if (e = a.data, e === "/$" || e === "/&") {
          if (u === 0) {
            l.removeChild(a), $u(t);
            return;
          }
          u--;
        } else if (e === "$" || e === "$?" || e === "$~" || e === "$!" || e === "&")
          u++;
        else if (e === "html")
          Ga(l.ownerDocument.documentElement);
        else if (e === "head") {
          e = l.ownerDocument.head, Ga(e);
          for (var n = e.firstChild; n; ) {
            var i = n.nextSibling, f = n.nodeName;
            n[aa] || f === "SCRIPT" || f === "STYLE" || f === "LINK" && n.rel.toLowerCase() === "stylesheet" || e.removeChild(n), n = i;
          }
        } else
          e === "body" && Ga(l.ownerDocument.body);
      e = a;
    } while (e);
    $u(t);
  }
  function _d(l, t) {
    var e = l;
    l = 0;
    do {
      var u = e.nextSibling;
      if (e.nodeType === 1 ? t ? (e._stashedDisplay = e.style.display, e.style.display = "none") : (e.style.display = e._stashedDisplay || "", e.getAttribute("style") === "" && e.removeAttribute("style")) : e.nodeType === 3 && (t ? (e._stashedText = e.nodeValue, e.nodeValue = "") : e.nodeValue = e._stashedText || ""), u && u.nodeType === 8)
        if (e = u.data, e === "/$") {
          if (l === 0) break;
          l--;
        } else
          e !== "$" && e !== "$?" && e !== "$~" && e !== "$!" || l++;
      e = u;
    } while (e);
  }
  function Sf(l) {
    var t = l.firstChild;
    for (t && t.nodeType === 10 && (t = t.nextSibling); t; ) {
      var e = t;
      switch (t = t.nextSibling, e.nodeName) {
        case "HTML":
        case "HEAD":
        case "BODY":
          Sf(e), zi(e);
          continue;
        case "SCRIPT":
        case "STYLE":
          continue;
        case "LINK":
          if (e.rel.toLowerCase() === "stylesheet") continue;
      }
      l.removeChild(e);
    }
  }
  function ch(l, t, e, u) {
    for (; l.nodeType === 1; ) {
      var a = e;
      if (l.nodeName.toLowerCase() !== t.toLowerCase()) {
        if (!u && (l.nodeName !== "INPUT" || l.type !== "hidden"))
          break;
      } else if (u) {
        if (!l[aa])
          switch (t) {
            case "meta":
              if (!l.hasAttribute("itemprop")) break;
              return l;
            case "link":
              if (n = l.getAttribute("rel"), n === "stylesheet" && l.hasAttribute("data-precedence"))
                break;
              if (n !== a.rel || l.getAttribute("href") !== (a.href == null || a.href === "" ? null : a.href) || l.getAttribute("crossorigin") !== (a.crossOrigin == null ? null : a.crossOrigin) || l.getAttribute("title") !== (a.title == null ? null : a.title))
                break;
              return l;
            case "style":
              if (l.hasAttribute("data-precedence")) break;
              return l;
            case "script":
              if (n = l.getAttribute("src"), (n !== (a.src == null ? null : a.src) || l.getAttribute("type") !== (a.type == null ? null : a.type) || l.getAttribute("crossorigin") !== (a.crossOrigin == null ? null : a.crossOrigin)) && n && l.hasAttribute("async") && !l.hasAttribute("itemprop"))
                break;
              return l;
            default:
              return l;
          }
      } else if (t === "input" && l.type === "hidden") {
        var n = a.name == null ? null : "" + a.name;
        if (a.type === "hidden" && l.getAttribute("name") === n)
          return l;
      } else return l;
      if (l = Ct(l.nextSibling), l === null) break;
    }
    return null;
  }
  function fh(l, t, e) {
    if (t === "") return null;
    for (; l.nodeType !== 3; )
      if ((l.nodeType !== 1 || l.nodeName !== "INPUT" || l.type !== "hidden") && !e || (l = Ct(l.nextSibling), l === null)) return null;
    return l;
  }
  function Ed(l, t) {
    for (; l.nodeType !== 8; )
      if ((l.nodeType !== 1 || l.nodeName !== "INPUT" || l.type !== "hidden") && !t || (l = Ct(l.nextSibling), l === null)) return null;
    return l;
  }
  function pf(l) {
    return l.data === "$?" || l.data === "$~";
  }
  function _f(l) {
    return l.data === "$!" || l.data === "$?" && l.ownerDocument.readyState !== "loading";
  }
  function sh(l, t) {
    var e = l.ownerDocument;
    if (l.data === "$~") l._reactRetry = t;
    else if (l.data !== "$?" || e.readyState !== "loading")
      t();
    else {
      var u = function() {
        t(), e.removeEventListener("DOMContentLoaded", u);
      };
      e.addEventListener("DOMContentLoaded", u), l._reactRetry = u;
    }
  }
  function Ct(l) {
    for (; l != null; l = l.nextSibling) {
      var t = l.nodeType;
      if (t === 1 || t === 3) break;
      if (t === 8) {
        if (t = l.data, t === "$" || t === "$!" || t === "$?" || t === "$~" || t === "&" || t === "F!" || t === "F")
          break;
        if (t === "/$" || t === "/&") return null;
      }
    }
    return l;
  }
  var Ef = null;
  function zd(l) {
    l = l.nextSibling;
    for (var t = 0; l; ) {
      if (l.nodeType === 8) {
        var e = l.data;
        if (e === "/$" || e === "/&") {
          if (t === 0)
            return Ct(l.nextSibling);
          t--;
        } else
          e !== "$" && e !== "$!" && e !== "$?" && e !== "$~" && e !== "&" || t++;
      }
      l = l.nextSibling;
    }
    return null;
  }
  function qd(l) {
    l = l.previousSibling;
    for (var t = 0; l; ) {
      if (l.nodeType === 8) {
        var e = l.data;
        if (e === "$" || e === "$!" || e === "$?" || e === "$~" || e === "&") {
          if (t === 0) return l;
          t--;
        } else e !== "/$" && e !== "/&" || t++;
      }
      l = l.previousSibling;
    }
    return null;
  }
  function Ad(l, t, e) {
    switch (t = ti(e), l) {
      case "html":
        if (l = t.documentElement, !l) throw Error(s(452));
        return l;
      case "head":
        if (l = t.head, !l) throw Error(s(453));
        return l;
      case "body":
        if (l = t.body, !l) throw Error(s(454));
        return l;
      default:
        throw Error(s(451));
    }
  }
  function Ga(l) {
    for (var t = l.attributes; t.length; )
      l.removeAttributeNode(t[0]);
    zi(l);
  }
  var jt = /* @__PURE__ */ new Map(), Td = /* @__PURE__ */ new Set();
  function ei(l) {
    return typeof l.getRootNode == "function" ? l.getRootNode() : l.nodeType === 9 ? l : l.ownerDocument;
  }
  var re = j.d;
  j.d = {
    f: rh,
    r: oh,
    D: dh,
    C: yh,
    L: vh,
    m: hh,
    X: gh,
    S: mh,
    M: bh
  };
  function rh() {
    var l = re.f(), t = wn();
    return l || t;
  }
  function oh(l) {
    var t = vu(l);
    t !== null && t.tag === 5 && t.type === "form" ? Xr(t) : re.r(l);
  }
  var Ju = typeof document > "u" ? null : document;
  function xd(l, t, e) {
    var u = Ju;
    if (u && typeof t == "string" && t) {
      var a = Tt(t);
      a = 'link[rel="' + l + '"][href="' + a + '"]', typeof e == "string" && (a += '[crossorigin="' + e + '"]'), Td.has(a) || (Td.add(a), l = { rel: l, crossOrigin: e, href: t }, u.querySelector(a) === null && (t = u.createElement("link"), Fl(t, "link", l), Zl(t), u.head.appendChild(t)));
    }
  }
  function dh(l) {
    re.D(l), xd("dns-prefetch", l, null);
  }
  function yh(l, t) {
    re.C(l, t), xd("preconnect", l, t);
  }
  function vh(l, t, e) {
    re.L(l, t, e);
    var u = Ju;
    if (u && l && t) {
      var a = 'link[rel="preload"][as="' + Tt(t) + '"]';
      t === "image" && e && e.imageSrcSet ? (a += '[imagesrcset="' + Tt(
        e.imageSrcSet
      ) + '"]', typeof e.imageSizes == "string" && (a += '[imagesizes="' + Tt(
        e.imageSizes
      ) + '"]')) : a += '[href="' + Tt(l) + '"]';
      var n = a;
      switch (t) {
        case "style":
          n = wu(l);
          break;
        case "script":
          n = ku(l);
      }
      jt.has(n) || (l = p(
        {
          rel: "preload",
          href: t === "image" && e && e.imageSrcSet ? void 0 : l,
          as: t
        },
        e
      ), jt.set(n, l), u.querySelector(a) !== null || t === "style" && u.querySelector(Qa(n)) || t === "script" && u.querySelector(Xa(n)) || (t = u.createElement("link"), Fl(t, "link", l), Zl(t), u.head.appendChild(t)));
    }
  }
  function hh(l, t) {
    re.m(l, t);
    var e = Ju;
    if (e && l) {
      var u = t && typeof t.as == "string" ? t.as : "script", a = 'link[rel="modulepreload"][as="' + Tt(u) + '"][href="' + Tt(l) + '"]', n = a;
      switch (u) {
        case "audioworklet":
        case "paintworklet":
        case "serviceworker":
        case "sharedworker":
        case "worker":
        case "script":
          n = ku(l);
      }
      if (!jt.has(n) && (l = p({ rel: "modulepreload", href: l }, t), jt.set(n, l), e.querySelector(a) === null)) {
        switch (u) {
          case "audioworklet":
          case "paintworklet":
          case "serviceworker":
          case "sharedworker":
          case "worker":
          case "script":
            if (e.querySelector(Xa(n)))
              return;
        }
        u = e.createElement("link"), Fl(u, "link", l), Zl(u), e.head.appendChild(u);
      }
    }
  }
  function mh(l, t, e) {
    re.S(l, t, e);
    var u = Ju;
    if (u && l) {
      var a = hu(u).hoistableStyles, n = wu(l);
      t = t || "default";
      var i = a.get(n);
      if (!i) {
        var f = { loading: 0, preload: null };
        if (i = u.querySelector(
          Qa(n)
        ))
          f.loading = 5;
        else {
          l = p(
            { rel: "stylesheet", href: l, "data-precedence": t },
            e
          ), (e = jt.get(n)) && zf(l, e);
          var d = i = u.createElement("link");
          Zl(d), Fl(d, "link", l), d._p = new Promise(function(g, z) {
            d.onload = g, d.onerror = z;
          }), d.addEventListener("load", function() {
            f.loading |= 1;
          }), d.addEventListener("error", function() {
            f.loading |= 2;
          }), f.loading |= 4, ui(i, t, u);
        }
        i = {
          type: "stylesheet",
          instance: i,
          count: 1,
          state: f
        }, a.set(n, i);
      }
    }
  }
  function gh(l, t) {
    re.X(l, t);
    var e = Ju;
    if (e && l) {
      var u = hu(e).hoistableScripts, a = ku(l), n = u.get(a);
      n || (n = e.querySelector(Xa(a)), n || (l = p({ src: l, async: !0 }, t), (t = jt.get(a)) && qf(l, t), n = e.createElement("script"), Zl(n), Fl(n, "link", l), e.head.appendChild(n)), n = {
        type: "script",
        instance: n,
        count: 1,
        state: null
      }, u.set(a, n));
    }
  }
  function bh(l, t) {
    re.M(l, t);
    var e = Ju;
    if (e && l) {
      var u = hu(e).hoistableScripts, a = ku(l), n = u.get(a);
      n || (n = e.querySelector(Xa(a)), n || (l = p({ src: l, async: !0, type: "module" }, t), (t = jt.get(a)) && qf(l, t), n = e.createElement("script"), Zl(n), Fl(n, "link", l), e.head.appendChild(n)), n = {
        type: "script",
        instance: n,
        count: 1,
        state: null
      }, u.set(a, n));
    }
  }
  function Nd(l, t, e, u) {
    var a = (a = ll.current) ? ei(a) : null;
    if (!a) throw Error(s(446));
    switch (l) {
      case "meta":
      case "title":
        return null;
      case "style":
        return typeof e.precedence == "string" && typeof e.href == "string" ? (t = wu(e.href), e = hu(
          a
        ).hoistableStyles, u = e.get(t), u || (u = {
          type: "style",
          instance: null,
          count: 0,
          state: null
        }, e.set(t, u)), u) : { type: "void", instance: null, count: 0, state: null };
      case "link":
        if (e.rel === "stylesheet" && typeof e.href == "string" && typeof e.precedence == "string") {
          l = wu(e.href);
          var n = hu(
            a
          ).hoistableStyles, i = n.get(l);
          if (i || (a = a.ownerDocument || a, i = {
            type: "stylesheet",
            instance: null,
            count: 0,
            state: { loading: 0, preload: null }
          }, n.set(l, i), (n = a.querySelector(
            Qa(l)
          )) && !n._p && (i.instance = n, i.state.loading = 5), jt.has(l) || (e = {
            rel: "preload",
            as: "style",
            href: e.href,
            crossOrigin: e.crossOrigin,
            integrity: e.integrity,
            media: e.media,
            hrefLang: e.hrefLang,
            referrerPolicy: e.referrerPolicy
          }, jt.set(l, e), n || Sh(
            a,
            l,
            e,
            i.state
          ))), t && u === null)
            throw Error(s(528, ""));
          return i;
        }
        if (t && u !== null)
          throw Error(s(529, ""));
        return null;
      case "script":
        return t = e.async, e = e.src, typeof e == "string" && t && typeof t != "function" && typeof t != "symbol" ? (t = ku(e), e = hu(
          a
        ).hoistableScripts, u = e.get(t), u || (u = {
          type: "script",
          instance: null,
          count: 0,
          state: null
        }, e.set(t, u)), u) : { type: "void", instance: null, count: 0, state: null };
      default:
        throw Error(s(444, l));
    }
  }
  function wu(l) {
    return 'href="' + Tt(l) + '"';
  }
  function Qa(l) {
    return 'link[rel="stylesheet"][' + l + "]";
  }
  function Od(l) {
    return p({}, l, {
      "data-precedence": l.precedence,
      precedence: null
    });
  }
  function Sh(l, t, e, u) {
    l.querySelector('link[rel="preload"][as="style"][' + t + "]") ? u.loading = 1 : (t = l.createElement("link"), u.preload = t, t.addEventListener("load", function() {
      return u.loading |= 1;
    }), t.addEventListener("error", function() {
      return u.loading |= 2;
    }), Fl(t, "link", e), Zl(t), l.head.appendChild(t));
  }
  function ku(l) {
    return '[src="' + Tt(l) + '"]';
  }
  function Xa(l) {
    return "script[async]" + l;
  }
  function Md(l, t, e) {
    if (t.count++, t.instance === null)
      switch (t.type) {
        case "style":
          var u = l.querySelector(
            'style[data-href~="' + Tt(e.href) + '"]'
          );
          if (u)
            return t.instance = u, Zl(u), u;
          var a = p({}, e, {
            "data-href": e.href,
            "data-precedence": e.precedence,
            href: null,
            precedence: null
          });
          return u = (l.ownerDocument || l).createElement(
            "style"
          ), Zl(u), Fl(u, "style", a), ui(u, e.precedence, l), t.instance = u;
        case "stylesheet":
          a = wu(e.href);
          var n = l.querySelector(
            Qa(a)
          );
          if (n)
            return t.state.loading |= 4, t.instance = n, Zl(n), n;
          u = Od(e), (a = jt.get(a)) && zf(u, a), n = (l.ownerDocument || l).createElement("link"), Zl(n);
          var i = n;
          return i._p = new Promise(function(f, d) {
            i.onload = f, i.onerror = d;
          }), Fl(n, "link", u), t.state.loading |= 4, ui(n, e.precedence, l), t.instance = n;
        case "script":
          return n = ku(e.src), (a = l.querySelector(
            Xa(n)
          )) ? (t.instance = a, Zl(a), a) : (u = e, (a = jt.get(n)) && (u = p({}, e), qf(u, a)), l = l.ownerDocument || l, a = l.createElement("script"), Zl(a), Fl(a, "link", u), l.head.appendChild(a), t.instance = a);
        case "void":
          return null;
        default:
          throw Error(s(443, t.type));
      }
    else
      t.type === "stylesheet" && (t.state.loading & 4) === 0 && (u = t.instance, t.state.loading |= 4, ui(u, e.precedence, l));
    return t.instance;
  }
  function ui(l, t, e) {
    for (var u = e.querySelectorAll(
      'link[rel="stylesheet"][data-precedence],style[data-precedence]'
    ), a = u.length ? u[u.length - 1] : null, n = a, i = 0; i < u.length; i++) {
      var f = u[i];
      if (f.dataset.precedence === t) n = f;
      else if (n !== a) break;
    }
    n ? n.parentNode.insertBefore(l, n.nextSibling) : (t = e.nodeType === 9 ? e.head : e, t.insertBefore(l, t.firstChild));
  }
  function zf(l, t) {
    l.crossOrigin == null && (l.crossOrigin = t.crossOrigin), l.referrerPolicy == null && (l.referrerPolicy = t.referrerPolicy), l.title == null && (l.title = t.title);
  }
  function qf(l, t) {
    l.crossOrigin == null && (l.crossOrigin = t.crossOrigin), l.referrerPolicy == null && (l.referrerPolicy = t.referrerPolicy), l.integrity == null && (l.integrity = t.integrity);
  }
  var ai = null;
  function Dd(l, t, e) {
    if (ai === null) {
      var u = /* @__PURE__ */ new Map(), a = ai = /* @__PURE__ */ new Map();
      a.set(e, u);
    } else
      a = ai, u = a.get(e), u || (u = /* @__PURE__ */ new Map(), a.set(e, u));
    if (u.has(l)) return u;
    for (u.set(l, null), e = e.getElementsByTagName(l), a = 0; a < e.length; a++) {
      var n = e[a];
      if (!(n[aa] || n[wl] || l === "link" && n.getAttribute("rel") === "stylesheet") && n.namespaceURI !== "http://www.w3.org/2000/svg") {
        var i = n.getAttribute(t) || "";
        i = l + i;
        var f = u.get(i);
        f ? f.push(n) : u.set(i, [n]);
      }
    }
    return u;
  }
  function Ud(l, t, e) {
    l = l.ownerDocument || l, l.head.insertBefore(
      e,
      t === "title" ? l.querySelector("head > title") : null
    );
  }
  function ph(l, t, e) {
    if (e === 1 || t.itemProp != null) return !1;
    switch (l) {
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
            return l = t.disabled, typeof t.precedence == "string" && l == null;
          default:
            return !0;
        }
      case "script":
        if (t.async && typeof t.async != "function" && typeof t.async != "symbol" && !t.onLoad && !t.onError && t.src && typeof t.src == "string")
          return !0;
    }
    return !1;
  }
  function Cd(l) {
    return !(l.type === "stylesheet" && (l.state.loading & 3) === 0);
  }
  function _h(l, t, e, u) {
    if (e.type === "stylesheet" && (typeof u.media != "string" || matchMedia(u.media).matches !== !1) && (e.state.loading & 4) === 0) {
      if (e.instance === null) {
        var a = wu(u.href), n = t.querySelector(
          Qa(a)
        );
        if (n) {
          t = n._p, t !== null && typeof t == "object" && typeof t.then == "function" && (l.count++, l = ni.bind(l), t.then(l, l)), e.state.loading |= 4, e.instance = n, Zl(n);
          return;
        }
        n = t.ownerDocument || t, u = Od(u), (a = jt.get(a)) && zf(u, a), n = n.createElement("link"), Zl(n);
        var i = n;
        i._p = new Promise(function(f, d) {
          i.onload = f, i.onerror = d;
        }), Fl(n, "link", u), e.instance = n;
      }
      l.stylesheets === null && (l.stylesheets = /* @__PURE__ */ new Map()), l.stylesheets.set(e, t), (t = e.state.preload) && (e.state.loading & 3) === 0 && (l.count++, e = ni.bind(l), t.addEventListener("load", e), t.addEventListener("error", e));
    }
  }
  var Af = 0;
  function Eh(l, t) {
    return l.stylesheets && l.count === 0 && ci(l, l.stylesheets), 0 < l.count || 0 < l.imgCount ? function(e) {
      var u = setTimeout(function() {
        if (l.stylesheets && ci(l, l.stylesheets), l.unsuspend) {
          var n = l.unsuspend;
          l.unsuspend = null, n();
        }
      }, 6e4 + t);
      0 < l.imgBytes && Af === 0 && (Af = 62500 * eh());
      var a = setTimeout(
        function() {
          if (l.waitingForImages = !1, l.count === 0 && (l.stylesheets && ci(l, l.stylesheets), l.unsuspend)) {
            var n = l.unsuspend;
            l.unsuspend = null, n();
          }
        },
        (l.imgBytes > Af ? 50 : 800) + t
      );
      return l.unsuspend = e, function() {
        l.unsuspend = null, clearTimeout(u), clearTimeout(a);
      };
    } : null;
  }
  function ni() {
    if (this.count--, this.count === 0 && (this.imgCount === 0 || !this.waitingForImages)) {
      if (this.stylesheets) ci(this, this.stylesheets);
      else if (this.unsuspend) {
        var l = this.unsuspend;
        this.unsuspend = null, l();
      }
    }
  }
  var ii = null;
  function ci(l, t) {
    l.stylesheets = null, l.unsuspend !== null && (l.count++, ii = /* @__PURE__ */ new Map(), t.forEach(zh, l), ii = null, ni.call(l));
  }
  function zh(l, t) {
    if (!(t.state.loading & 4)) {
      var e = ii.get(l);
      if (e) var u = e.get(null);
      else {
        e = /* @__PURE__ */ new Map(), ii.set(l, e);
        for (var a = l.querySelectorAll(
          "link[data-precedence],style[data-precedence]"
        ), n = 0; n < a.length; n++) {
          var i = a[n];
          (i.nodeName === "LINK" || i.getAttribute("media") !== "not all") && (e.set(i.dataset.precedence, i), u = i);
        }
        u && e.set(null, u);
      }
      a = t.instance, i = a.getAttribute("data-precedence"), n = e.get(i) || u, n === u && e.set(null, a), e.set(i, a), this.count++, u = ni.bind(this), a.addEventListener("load", u), a.addEventListener("error", u), n ? n.parentNode.insertBefore(a, n.nextSibling) : (l = l.nodeType === 9 ? l.head : l, l.insertBefore(a, l.firstChild)), t.state.loading |= 4;
    }
  }
  var Za = {
    $$typeof: W,
    Provider: null,
    Consumer: null,
    _currentValue: B,
    _currentValue2: B,
    _threadCount: 0
  };
  function qh(l, t, e, u, a, n, i, f, d) {
    this.tag = 1, this.containerInfo = l, this.pingCache = this.current = this.pendingChildren = null, this.timeoutHandle = -1, this.callbackNode = this.next = this.pendingContext = this.context = this.cancelPendingCommit = null, this.callbackPriority = 0, this.expirationTimes = Si(-1), this.entangledLanes = this.shellSuspendCounter = this.errorRecoveryDisabledLanes = this.expiredLanes = this.warmLanes = this.pingedLanes = this.suspendedLanes = this.pendingLanes = 0, this.entanglements = Si(0), this.hiddenUpdates = Si(null), this.identifierPrefix = u, this.onUncaughtError = a, this.onCaughtError = n, this.onRecoverableError = i, this.pooledCache = null, this.pooledCacheLanes = 0, this.formState = d, this.incompleteTransitions = /* @__PURE__ */ new Map();
  }
  function jd(l, t, e, u, a, n, i, f, d, g, z, M) {
    return l = new qh(
      l,
      t,
      e,
      i,
      d,
      g,
      z,
      M,
      f
    ), t = 1, n === !0 && (t |= 24), n = bt(3, null, null, t), l.current = n, n.stateNode = l, t = uc(), t.refCount++, l.pooledCache = t, t.refCount++, n.memoizedState = {
      element: u,
      isDehydrated: e,
      cache: t
    }, cc(n), l;
  }
  function Rd(l) {
    return l ? (l = Au, l) : Au;
  }
  function Hd(l, t, e, u, a, n) {
    a = Rd(a), u.context === null ? u.context = a : u.pendingContext = a, u = ze(t), u.payload = { element: e }, n = n === void 0 ? null : n, n !== null && (u.callback = n), e = qe(l, u, t), e !== null && (st(e, l, t), _a(e, l, t));
  }
  function Bd(l, t) {
    if (l = l.memoizedState, l !== null && l.dehydrated !== null) {
      var e = l.retryLane;
      l.retryLane = e !== 0 && e < t ? e : t;
    }
  }
  function Tf(l, t) {
    Bd(l, t), (l = l.alternate) && Bd(l, t);
  }
  function Yd(l) {
    if (l.tag === 13 || l.tag === 31) {
      var t = $e(l, 67108864);
      t !== null && st(t, l, 67108864), Tf(l, 67108864);
    }
  }
  function Ld(l) {
    if (l.tag === 13 || l.tag === 31) {
      var t = zt();
      t = pi(t);
      var e = $e(l, t);
      e !== null && st(e, l, t), Tf(l, t);
    }
  }
  var fi = !0;
  function Ah(l, t, e, u) {
    var a = q.T;
    q.T = null;
    var n = j.p;
    try {
      j.p = 2, xf(l, t, e, u);
    } finally {
      j.p = n, q.T = a;
    }
  }
  function Th(l, t, e, u) {
    var a = q.T;
    q.T = null;
    var n = j.p;
    try {
      j.p = 8, xf(l, t, e, u);
    } finally {
      j.p = n, q.T = a;
    }
  }
  function xf(l, t, e, u) {
    if (fi) {
      var a = Nf(u);
      if (a === null)
        yf(
          l,
          t,
          u,
          si,
          e
        ), Qd(l, u);
      else if (Nh(
        a,
        l,
        t,
        e,
        u
      ))
        u.stopPropagation();
      else if (Qd(l, u), t & 4 && -1 < xh.indexOf(l)) {
        for (; a !== null; ) {
          var n = vu(a);
          if (n !== null)
            switch (n.tag) {
              case 3:
                if (n = n.stateNode, n.current.memoizedState.isDehydrated) {
                  var i = Ve(n.pendingLanes);
                  if (i !== 0) {
                    var f = n;
                    for (f.pendingLanes |= 2, f.entangledLanes |= 2; i; ) {
                      var d = 1 << 31 - mt(i);
                      f.entanglements[1] |= d, i &= ~d;
                    }
                    Kt(n), (gl & 6) === 0 && (Kn = vt() + 500, Ba(0));
                  }
                }
                break;
              case 31:
              case 13:
                f = $e(n, 2), f !== null && st(f, n, 2), wn(), Tf(n, 2);
            }
          if (n = Nf(u), n === null && yf(
            l,
            t,
            u,
            si,
            e
          ), n === a) break;
          a = n;
        }
        a !== null && u.stopPropagation();
      } else
        yf(
          l,
          t,
          u,
          null,
          e
        );
    }
  }
  function Nf(l) {
    return l = Oi(l), Of(l);
  }
  var si = null;
  function Of(l) {
    if (si = null, l = yu(l), l !== null) {
      var t = E(l);
      if (t === null) l = null;
      else {
        var e = t.tag;
        if (e === 13) {
          if (l = C(t), l !== null) return l;
          l = null;
        } else if (e === 31) {
          if (l = U(t), l !== null) return l;
          l = null;
        } else if (e === 3) {
          if (t.stateNode.current.memoizedState.isDehydrated)
            return t.tag === 3 ? t.stateNode.containerInfo : null;
          l = null;
        } else t !== l && (l = null);
      }
    }
    return si = l, null;
  }
  function Gd(l) {
    switch (l) {
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
        switch (dy()) {
          case wf:
            return 2;
          case kf:
            return 8;
          case Fa:
          case yy:
            return 32;
          case $f:
            return 268435456;
          default:
            return 32;
        }
      default:
        return 32;
    }
  }
  var Mf = !1, Re = null, He = null, Be = null, Va = /* @__PURE__ */ new Map(), Ka = /* @__PURE__ */ new Map(), Ye = [], xh = "mousedown mouseup touchcancel touchend touchstart auxclick dblclick pointercancel pointerdown pointerup dragend dragstart drop compositionend compositionstart keydown keypress keyup input textInput copy cut paste click change contextmenu reset".split(
    " "
  );
  function Qd(l, t) {
    switch (l) {
      case "focusin":
      case "focusout":
        Re = null;
        break;
      case "dragenter":
      case "dragleave":
        He = null;
        break;
      case "mouseover":
      case "mouseout":
        Be = null;
        break;
      case "pointerover":
      case "pointerout":
        Va.delete(t.pointerId);
        break;
      case "gotpointercapture":
      case "lostpointercapture":
        Ka.delete(t.pointerId);
    }
  }
  function Ja(l, t, e, u, a, n) {
    return l === null || l.nativeEvent !== n ? (l = {
      blockedOn: t,
      domEventName: e,
      eventSystemFlags: u,
      nativeEvent: n,
      targetContainers: [a]
    }, t !== null && (t = vu(t), t !== null && Yd(t)), l) : (l.eventSystemFlags |= u, t = l.targetContainers, a !== null && t.indexOf(a) === -1 && t.push(a), l);
  }
  function Nh(l, t, e, u, a) {
    switch (t) {
      case "focusin":
        return Re = Ja(
          Re,
          l,
          t,
          e,
          u,
          a
        ), !0;
      case "dragenter":
        return He = Ja(
          He,
          l,
          t,
          e,
          u,
          a
        ), !0;
      case "mouseover":
        return Be = Ja(
          Be,
          l,
          t,
          e,
          u,
          a
        ), !0;
      case "pointerover":
        var n = a.pointerId;
        return Va.set(
          n,
          Ja(
            Va.get(n) || null,
            l,
            t,
            e,
            u,
            a
          )
        ), !0;
      case "gotpointercapture":
        return n = a.pointerId, Ka.set(
          n,
          Ja(
            Ka.get(n) || null,
            l,
            t,
            e,
            u,
            a
          )
        ), !0;
    }
    return !1;
  }
  function Xd(l) {
    var t = yu(l.target);
    if (t !== null) {
      var e = E(t);
      if (e !== null) {
        if (t = e.tag, t === 13) {
          if (t = C(e), t !== null) {
            l.blockedOn = t, ts(l.priority, function() {
              Ld(e);
            });
            return;
          }
        } else if (t === 31) {
          if (t = U(e), t !== null) {
            l.blockedOn = t, ts(l.priority, function() {
              Ld(e);
            });
            return;
          }
        } else if (t === 3 && e.stateNode.current.memoizedState.isDehydrated) {
          l.blockedOn = e.tag === 3 ? e.stateNode.containerInfo : null;
          return;
        }
      }
    }
    l.blockedOn = null;
  }
  function ri(l) {
    if (l.blockedOn !== null) return !1;
    for (var t = l.targetContainers; 0 < t.length; ) {
      var e = Nf(l.nativeEvent);
      if (e === null) {
        e = l.nativeEvent;
        var u = new e.constructor(
          e.type,
          e
        );
        Ni = u, e.target.dispatchEvent(u), Ni = null;
      } else
        return t = vu(e), t !== null && Yd(t), l.blockedOn = e, !1;
      t.shift();
    }
    return !0;
  }
  function Zd(l, t, e) {
    ri(l) && e.delete(t);
  }
  function Oh() {
    Mf = !1, Re !== null && ri(Re) && (Re = null), He !== null && ri(He) && (He = null), Be !== null && ri(Be) && (Be = null), Va.forEach(Zd), Ka.forEach(Zd);
  }
  function oi(l, t) {
    l.blockedOn === t && (l.blockedOn = null, Mf || (Mf = !0, c.unstable_scheduleCallback(
      c.unstable_NormalPriority,
      Oh
    )));
  }
  var di = null;
  function Vd(l) {
    di !== l && (di = l, c.unstable_scheduleCallback(
      c.unstable_NormalPriority,
      function() {
        di === l && (di = null);
        for (var t = 0; t < l.length; t += 3) {
          var e = l[t], u = l[t + 1], a = l[t + 2];
          if (typeof u != "function") {
            if (Of(u || e) === null)
              continue;
            break;
          }
          var n = vu(e);
          n !== null && (l.splice(t, 3), t -= 3, xc(
            n,
            {
              pending: !0,
              data: a,
              method: e.method,
              action: u
            },
            u,
            a
          ));
        }
      }
    ));
  }
  function $u(l) {
    function t(d) {
      return oi(d, l);
    }
    Re !== null && oi(Re, l), He !== null && oi(He, l), Be !== null && oi(Be, l), Va.forEach(t), Ka.forEach(t);
    for (var e = 0; e < Ye.length; e++) {
      var u = Ye[e];
      u.blockedOn === l && (u.blockedOn = null);
    }
    for (; 0 < Ye.length && (e = Ye[0], e.blockedOn === null); )
      Xd(e), e.blockedOn === null && Ye.shift();
    if (e = (l.ownerDocument || l).$$reactFormReplay, e != null)
      for (u = 0; u < e.length; u += 3) {
        var a = e[u], n = e[u + 1], i = a[ut] || null;
        if (typeof n == "function")
          i || Vd(e);
        else if (i) {
          var f = null;
          if (n && n.hasAttribute("formAction")) {
            if (a = n, i = n[ut] || null)
              f = i.formAction;
            else if (Of(a) !== null) continue;
          } else f = i.action;
          typeof f == "function" ? e[u + 1] = f : (e.splice(u, 3), u -= 3), Vd(e);
        }
      }
  }
  function Kd() {
    function l(n) {
      n.canIntercept && n.info === "react-transition" && n.intercept({
        handler: function() {
          return new Promise(function(i) {
            return a = i;
          });
        },
        focusReset: "manual",
        scroll: "manual"
      });
    }
    function t() {
      a !== null && (a(), a = null), u || setTimeout(e, 20);
    }
    function e() {
      if (!u && !navigation.transition) {
        var n = navigation.currentEntry;
        n && n.url != null && navigation.navigate(n.url, {
          state: n.getState(),
          info: "react-transition",
          history: "replace"
        });
      }
    }
    if (typeof navigation == "object") {
      var u = !1, a = null;
      return navigation.addEventListener("navigate", l), navigation.addEventListener("navigatesuccess", t), navigation.addEventListener("navigateerror", t), setTimeout(e, 100), function() {
        u = !0, navigation.removeEventListener("navigate", l), navigation.removeEventListener("navigatesuccess", t), navigation.removeEventListener("navigateerror", t), a !== null && (a(), a = null);
      };
    }
  }
  function Df(l) {
    this._internalRoot = l;
  }
  yi.prototype.render = Df.prototype.render = function(l) {
    var t = this._internalRoot;
    if (t === null) throw Error(s(409));
    var e = t.current, u = zt();
    Hd(e, u, l, t, null, null);
  }, yi.prototype.unmount = Df.prototype.unmount = function() {
    var l = this._internalRoot;
    if (l !== null) {
      this._internalRoot = null;
      var t = l.containerInfo;
      Hd(l.current, 2, null, l, null, null), wn(), t[du] = null;
    }
  };
  function yi(l) {
    this._internalRoot = l;
  }
  yi.prototype.unstable_scheduleHydration = function(l) {
    if (l) {
      var t = ls();
      l = { blockedOn: null, target: l, priority: t };
      for (var e = 0; e < Ye.length && t !== 0 && t < Ye[e].priority; e++) ;
      Ye.splice(e, 0, l), e === 0 && Xd(l);
    }
  };
  var Jd = o.version;
  if (Jd !== "19.2.5")
    throw Error(
      s(
        527,
        Jd,
        "19.2.5"
      )
    );
  j.findDOMNode = function(l) {
    var t = l._reactInternals;
    if (t === void 0)
      throw typeof l.render == "function" ? Error(s(188)) : (l = Object.keys(l).join(","), Error(s(268, l)));
    return l = b(t), l = l !== null ? H(l) : null, l = l === null ? null : l.stateNode, l;
  };
  var Mh = {
    bundleType: 0,
    version: "19.2.5",
    rendererPackageName: "react-dom",
    currentDispatcherRef: q,
    reconcilerVersion: "19.2.5"
  };
  if (typeof __REACT_DEVTOOLS_GLOBAL_HOOK__ < "u") {
    var vi = __REACT_DEVTOOLS_GLOBAL_HOOK__;
    if (!vi.isDisabled && vi.supportsFiber)
      try {
        ta = vi.inject(
          Mh
        ), ht = vi;
      } catch {
      }
  }
  return wa.createRoot = function(l, t) {
    if (!T(l)) throw Error(s(299));
    var e = !1, u = "", a = Ir, n = Pr, i = lo;
    return t != null && (t.unstable_strictMode === !0 && (e = !0), t.identifierPrefix !== void 0 && (u = t.identifierPrefix), t.onUncaughtError !== void 0 && (a = t.onUncaughtError), t.onCaughtError !== void 0 && (n = t.onCaughtError), t.onRecoverableError !== void 0 && (i = t.onRecoverableError)), t = jd(
      l,
      1,
      !1,
      null,
      null,
      e,
      u,
      null,
      a,
      n,
      i,
      Kd
    ), l[du] = t.current, df(l), new Df(t);
  }, wa.hydrateRoot = function(l, t, e) {
    if (!T(l)) throw Error(s(299));
    var u = !1, a = "", n = Ir, i = Pr, f = lo, d = null;
    return e != null && (e.unstable_strictMode === !0 && (u = !0), e.identifierPrefix !== void 0 && (a = e.identifierPrefix), e.onUncaughtError !== void 0 && (n = e.onUncaughtError), e.onCaughtError !== void 0 && (i = e.onCaughtError), e.onRecoverableError !== void 0 && (f = e.onRecoverableError), e.formState !== void 0 && (d = e.formState)), t = jd(
      l,
      1,
      !0,
      t,
      e ?? null,
      u,
      a,
      d,
      n,
      i,
      f,
      Kd
    ), t.context = Rd(null), e = t.current, u = zt(), u = pi(u), a = ze(u), a.callback = null, qe(e, a, u), e = u, t.current.lanes = e, ua(t, e), Kt(t), l[du] = t.current, df(l), new yi(t);
  }, wa.version = "19.2.5", wa;
}
var ty;
function Gh() {
  if (ty) return jf.exports;
  ty = 1;
  function c() {
    if (!(typeof __REACT_DEVTOOLS_GLOBAL_HOOK__ > "u" || typeof __REACT_DEVTOOLS_GLOBAL_HOOK__.checkDCE != "function"))
      try {
        __REACT_DEVTOOLS_GLOBAL_HOOK__.checkDCE(c);
      } catch (o) {
        console.error(o);
      }
  }
  return c(), jf.exports = Lh(), jf.exports;
}
var Qh = Gh(), Yf = { exports: {} }, ka = {};
/**
 * @license React
 * react-jsx-runtime.production.js
 *
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 */
var ey;
function Xh() {
  if (ey) return ka;
  ey = 1;
  var c = Symbol.for("react.transitional.element"), o = Symbol.for("react.fragment");
  function r(s, T, E) {
    var C = null;
    if (E !== void 0 && (C = "" + E), T.key !== void 0 && (C = "" + T.key), "key" in T) {
      E = {};
      for (var U in T)
        U !== "key" && (E[U] = T[U]);
    } else E = T;
    return T = E.ref, {
      $$typeof: c,
      type: s,
      key: C,
      ref: T !== void 0 ? T : null,
      props: E
    };
  }
  return ka.Fragment = o, ka.jsx = r, ka.jsxs = r, ka;
}
var uy;
function Zh() {
  return uy || (uy = 1, Yf.exports = Xh()), Yf.exports;
}
var x = Zh();
function Vh(c) {
  return typeof c.questionId == "string";
}
function Kh(c) {
  const o = c;
  return Array.isArray(o.all) || Array.isArray(o.any);
}
function Jh(c) {
  return typeof c.expression == "string";
}
class Jt extends Error {
  constructor(r, s) {
    super(`Expression syntax error at column ${s}: ${r}`);
    oe(this, "position");
    this.position = s, this.name = "ExpressionSyntaxError";
  }
}
function hi(c) {
  return c >= "0" && c <= "9";
}
function cy(c) {
  return c >= "a" && c <= "z" || c >= "A" && c <= "Z";
}
function wh(c) {
  return cy(c) || hi(c);
}
function kh(c) {
  return c === " " || c === "	" || c === `
` || c === "\r" || c === "\f" || c === "\v";
}
function $h(c) {
  const o = [];
  let r = 0;
  const s = () => r >= c.length, T = (p = 0) => c.charAt(r + p), E = (p) => {
    if (r + p.length > c.length)
      return !1;
    for (let D = 0; D < p.length; D++)
      if (c.charAt(r + D) !== p.charAt(D))
        return !1;
    return r += p.length, !0;
  }, C = () => {
    for (; !s() && kh(T()); )
      r++;
  }, U = (p) => {
    for (; !s() && hi(T()); )
      r++;
    if (!s() && T() === ".")
      for (r++; !s() && hi(T()); )
        r++;
    const D = c.substring(p, r), X = parseFloat(D);
    return { kind: "Number", text: D, literal: X, position: p };
  }, N = (p, D) => {
    r++;
    let X = "";
    for (; !s() && T() !== D; ) {
      const k = T();
      if (k === "\\" && r + 1 < c.length) {
        const K = T(1), Z = {
          n: `
`,
          t: "	",
          r: "\r",
          "\\": "\\",
          "'": "'",
          '"': '"'
        }[K];
        if (Z === void 0)
          throw new Jt(`unknown escape '\\${K}'.`, r);
        X += Z, r += 2;
      } else
        X += k, r++;
    }
    if (s())
      throw new Jt("unterminated string literal.", p);
    return r++, { kind: "String", text: X, literal: X, position: p };
  }, b = (p) => {
    for (; !s(); ) {
      const X = T();
      if (X === "_" || X === "-" || wh(X))
        r++;
      else
        break;
    }
    const D = c.substring(p, r);
    return D === "true" ? { kind: "True", text: D, literal: !0, position: p } : D === "false" ? { kind: "False", text: D, literal: !1, position: p } : D === "null" ? { kind: "Null", text: D, literal: null, position: p } : { kind: "Identifier", text: D, literal: null, position: p };
  }, H = () => {
    const p = r, D = T();
    if (hi(D))
      return U(p);
    if (D === "'" || D === '"')
      return N(p, D);
    if (D === "_" || cy(D))
      return b(p);
    switch (D) {
      case "(":
        return r++, { kind: "LParen", text: "(", literal: null, position: p };
      case ")":
        return r++, { kind: "RParen", text: ")", literal: null, position: p };
      case "[":
        return r++, { kind: "LBracket", text: "[", literal: null, position: p };
      case "]":
        return r++, { kind: "RBracket", text: "]", literal: null, position: p };
      case ",":
        return r++, { kind: "Comma", text: ",", literal: null, position: p };
      case ".":
        return r++, { kind: "Dot", text: ".", literal: null, position: p };
      case "=":
        if (E("==="))
          return { kind: "StrictEq", text: "===", literal: null, position: p };
        if (E("=="))
          return { kind: "Eq", text: "==", literal: null, position: p };
        throw new Jt("bare '=' is not a valid operator (use '==' or '===').", p);
      case "!":
        return E("!==") ? { kind: "StrictNotEq", text: "!==", literal: null, position: p } : E("!=") ? { kind: "NotEq", text: "!=", literal: null, position: p } : (r++, { kind: "Not", text: "!", literal: null, position: p });
      case "<":
        return E("<=") ? { kind: "LtEq", text: "<=", literal: null, position: p } : (r++, { kind: "Lt", text: "<", literal: null, position: p });
      case ">":
        return E(">=") ? { kind: "GtEq", text: ">=", literal: null, position: p } : (r++, { kind: "Gt", text: ">", literal: null, position: p });
      case "&":
        if (E("&&"))
          return { kind: "And", text: "&&", literal: null, position: p };
        throw new Jt("expected '&&'.", p);
      case "|":
        if (E("||"))
          return { kind: "Or", text: "||", literal: null, position: p };
        throw new Jt("expected '||'.", p);
    }
    throw new Jt(`unexpected character '${D}'.`, p);
  };
  for (; ; ) {
    if (C(), s())
      return o.push({ kind: "EndOfInput", text: "", literal: null, position: r }), o;
    o.push(H());
  }
}
function Wh(c) {
  let o = 0;
  const r = () => {
    const G = c[o];
    if (!G)
      throw new Jt("unexpected end of tokens.", 0);
    return G;
  }, s = () => {
    const G = r();
    return G.kind !== "EndOfInput" && o++, G;
  }, T = (G) => r().kind !== G ? !1 : (s(), !0), E = (G) => {
    const Z = r();
    if (Z.kind !== G)
      throw new Jt(`expected ${G}, got '${Z.text}'.`, Z.position);
    return s(), Z;
  }, C = () => {
    let G = U();
    for (; T("Or"); )
      G = { kind: "BinaryOp", op: "||", left: G, right: U() };
    return G;
  }, U = () => {
    let G = N();
    for (; T("And"); )
      G = { kind: "BinaryOp", op: "&&", left: G, right: N() };
    return G;
  }, N = () => {
    let G = b();
    for (; ; ) {
      const Z = r().kind;
      let ol = null;
      if (Z === "Eq" || Z === "StrictEq" ? ol = "==" : (Z === "NotEq" || Z === "StrictNotEq") && (ol = "!="), ol === null)
        break;
      s(), G = { kind: "BinaryOp", op: ol, left: G, right: b() };
    }
    return G;
  }, b = () => {
    let G = H();
    for (; ; ) {
      const Z = r().kind;
      let ol = null;
      if (Z === "Lt" ? ol = "<" : Z === "Gt" ? ol = ">" : Z === "LtEq" ? ol = "<=" : Z === "GtEq" && (ol = ">="), ol === null)
        break;
      s(), G = { kind: "BinaryOp", op: ol, left: G, right: H() };
    }
    return G;
  }, H = () => T("Not") ? { kind: "UnaryOp", op: "!", operand: H() } : k(), p = () => {
    E("LBracket");
    const G = [];
    if (r().kind !== "RBracket")
      for (G.push(C()); T("Comma"); )
        G.push(C());
    return E("RBracket"), { kind: "Array", items: G };
  }, D = (G) => {
    let Z;
    if (T("Dot"))
      Z = E("Identifier").text;
    else if (T("LBracket")) {
      const ol = E("String");
      E("RBracket"), Z = ol.literal;
    } else
      throw new Jt("'answers' must be followed by .key or ['key'].", G);
    return { kind: "AnswersAccess", key: Z };
  }, X = () => {
    const G = s();
    if (G.text === "answers")
      return D(G.position);
    E("LParen");
    const Z = [];
    if (r().kind !== "RParen")
      for (Z.push(C()); T("Comma"); )
        Z.push(C());
    return E("RParen"), { kind: "Call", name: G.text, args: Z };
  }, k = () => {
    const G = r();
    switch (G.kind) {
      case "Number":
      case "String":
      case "True":
      case "False":
      case "Null":
        return s(), { kind: "Literal", value: G.literal };
      case "LParen": {
        s();
        const Z = C();
        return E("RParen"), Z;
      }
      case "LBracket":
        return p();
      case "Identifier":
        return X();
      default:
        throw new Jt(`unexpected token '${G.text}'.`, G.position);
    }
  }, K = C();
  return E("EndOfInput"), K;
}
function ye(c) {
  return c === void 0 || c === null ? null : typeof c == "boolean" || typeof c == "number" || typeof c == "string" ? c : Array.isArray(c) ? c.map(ye) : null;
}
function ru(c, o) {
  const r = ye(c), s = ye(o);
  if (r === null || s === null)
    return r === null && s === null;
  if (typeof r == "number" && typeof s == "number" || typeof r == "string" && typeof s == "string" || typeof r == "boolean" && typeof s == "boolean")
    return r === s;
  if (Array.isArray(r) && Array.isArray(s)) {
    if (r.length !== s.length)
      return !1;
    for (let T = 0; T < r.length; T++)
      if (!ru(r[T], s[T]))
        return !1;
    return !0;
  }
  return !1;
}
function Xe(c, o) {
  const r = ye(c), s = ye(o);
  if (typeof r == "number" && typeof s == "number" || typeof r == "string" && typeof s == "string")
    return r < s ? -1 : r > s ? 1 : 0;
  throw new Error("Comparison operators require two numbers or two strings.");
}
function Iu(c) {
  const o = ye(c);
  return o === null ? !1 : typeof o == "boolean" ? o : typeof o == "number" ? o !== 0 : typeof o == "string" || Array.isArray(o) ? o.length > 0 : !0;
}
function Rt(c, o) {
  switch (c.kind) {
    case "Literal":
      return c.value;
    case "AnswersAccess":
      return tm(c.key, o);
    case "UnaryOp":
      return Fh(c, o);
    case "BinaryOp":
      return Ih(c, o);
    case "Call":
      return Ph(c, o);
    case "Array":
      return c.items.map((r) => Rt(r, o));
  }
}
function Fh(c, o) {
  const r = Rt(c.operand, o);
  if (c.op === "!")
    return !Iu(r);
  throw new Error(`Unknown unary operator '${c.op}'.`);
}
function Ih(c, o) {
  if (c.op === "&&") {
    const T = Rt(c.left, o);
    return Iu(T) ? Iu(Rt(c.right, o)) : !1;
  }
  if (c.op === "||") {
    const T = Rt(c.left, o);
    return Iu(T) ? !0 : Iu(Rt(c.right, o));
  }
  const r = Rt(c.left, o), s = Rt(c.right, o);
  switch (c.op) {
    case "==":
      return ru(r, s);
    case "!=":
      return !ru(r, s);
    case "<":
      return Xe(r, s) < 0;
    case ">":
      return Xe(r, s) > 0;
    case "<=":
      return Xe(r, s) <= 0;
    case ">=":
      return Xe(r, s) >= 0;
    default:
      throw new Error(`Unknown binary operator '${c.op}'.`);
  }
}
function Ph(c, o) {
  switch (c.name) {
    case "has":
    case "isSet":
      return ay(c, o);
    case "isNotSet":
      return !ay(c, o);
    case "in":
      return lm(c, o);
    default:
      throw new Error(`Unknown function '${c.name}'.`);
  }
}
function ay(c, o) {
  if (c.args.length !== 1)
    throw new Error(`${c.name}() takes one argument.`);
  const r = c.args[0];
  if (!r)
    return !1;
  const s = Rt(r, o);
  return typeof s != "string" ? !1 : s in o && o[s] !== null && o[s] !== void 0;
}
function lm(c, o) {
  if (c.args.length !== 2)
    throw new Error("in() takes two arguments: in(value, [array]).");
  const r = c.args[0], s = c.args[1];
  if (!r || !s)
    return !1;
  const T = Rt(r, o), E = Rt(s, o);
  return Array.isArray(E) ? E.some((C) => ru(T, C)) : !1;
}
function tm(c, o) {
  return c in o ? ye(o[c]) : null;
}
function em(c) {
  const o = $h(c);
  return Wh(o);
}
function um(c, o) {
  try {
    const r = typeof c == "string" ? em(c) : c;
    return Iu(Rt(r, o));
  } catch {
    return !1;
  }
}
function am(c, o) {
  var r;
  if (!c.logic)
    return null;
  for (const s of c.logic)
    if (Qf(s.if, o))
      return ((r = s.then) == null ? void 0 : r.goto) ?? null;
  return null;
}
function Qf(c, o) {
  try {
    return Vh(c) ? im(c, o) : Kh(c) ? nm(c, o) : Jh(c) ? um(c.expression, o) : !1;
  } catch {
    return !1;
  }
}
function nm(c, o) {
  return c.all && c.all.length > 0 ? c.all.every((r) => Qf(r, o)) : c.any && c.any.length > 0 ? c.any.some((r) => Qf(r, o)) : !1;
}
function im(c, o) {
  const r = c.questionId in o && o[c.questionId] !== null && o[c.questionId] !== void 0;
  if (c.op === "isSet")
    return r;
  if (c.op === "isNotSet")
    return !r;
  if (c.value === void 0)
    return !1;
  const s = r ? ye(o[c.questionId]) : null, T = ye(c.value);
  return cm(c.op, s, T);
}
function cm(c, o, r) {
  switch (c) {
    case "==":
      return ru(o, r);
    case "!=":
      return !ru(o, r);
    case ">":
      return Xe(o, r) > 0;
    case ">=":
      return Xe(o, r) >= 0;
    case "<":
      return Xe(o, r) < 0;
    case "<=":
      return Xe(o, r) <= 0;
    case "in":
      return ny(r, o);
    case "notIn":
      return !ny(r, o);
    default:
      return !1;
  }
}
function ny(c, o) {
  return Array.isArray(c) ? c.some((r) => ru(o, r)) : !1;
}
function Xf(c, o, r) {
  const s = new Set(c.screens.map((U) => U.id)), T = am(c, r);
  if (T && T !== o && s.has(T))
    return { kind: "screen", screenId: T };
  const E = c.screens.find((U) => U.id === o);
  if (E != null && E.nextScreen && E.nextScreen !== o && s.has(E.nextScreen))
    return { kind: "screen", screenId: E.nextScreen };
  if (E && (!E.questions || E.questions.length === 0) && !E.nextScreen)
    return { kind: "end" };
  const C = c.screens.findIndex((U) => U.id === o);
  if (C >= 0 && C + 1 < c.screens.length) {
    const U = c.screens[C + 1];
    if (U)
      return { kind: "screen", screenId: U.id };
  }
  return { kind: "end" };
}
function fm(c, o, r, s) {
  const T = new Set(o.screens.map((E) => E.id));
  return c.nextScreen && T.has(c.nextScreen) ? { kind: "screen", screenId: c.nextScreen } : Xf(o, r, s);
}
class Wu extends Error {
  constructor(r) {
    super(r.message);
    oe(this, "status");
    oe(this, "code");
    oe(this, "serverMessage");
    oe(this, "validationErrors");
    oe(this, "raw");
    this.name = "SurveyClientError", this.status = r.status, this.code = r.code, this.serverMessage = r.serverMessage, this.validationErrors = r.validationErrors, this.raw = r.raw;
  }
}
class iy {
  constructor(o) {
    oe(this, "baseUrl");
    oe(this, "fetchFn");
    this.baseUrl = o.baseUrl.replace(/\/+$/, "");
    const r = o.fetch ?? globalThis.fetch;
    if (!r)
      throw new Error("SurveyClient: no fetch available. Provide options.fetch or run in an environment with a global fetch.");
    this.fetchFn = r.bind(globalThis);
  }
  async fetchSchema(o) {
    const r = await this.send("GET", `/SurveyInstances/${encodeURIComponent(o)}/schema`);
    return this.readJson(r);
  }
  async getStatus(o) {
    const r = await this.send("GET", `/SurveyInstances/${encodeURIComponent(o)}/status`), s = await this.readJson(r);
    return {
      status: String(s.Status ?? s.status ?? "Pending"),
      schemaVersion: Number(s.SchemaVersion ?? s.schemaVersion ?? 0),
      triggeredAt: s.TriggeredAt ?? s.triggeredAt
    };
  }
  async submitResponse(o, r) {
    await this.send("POST", `/SurveyInstances/${encodeURIComponent(o)}/responses`, r);
  }
  async send(o, r, s) {
    let T;
    try {
      T = await this.fetchFn(`${this.baseUrl}${r}`, {
        method: o,
        headers: s === void 0 ? void 0 : { "Content-Type": "application/json" },
        body: s === void 0 ? void 0 : JSON.stringify(s)
      });
    } catch (E) {
      throw new Wu({
        status: 0,
        code: "network",
        message: `Network error calling ${o} ${r}: ${E.message ?? E}`
      });
    }
    if (!T.ok)
      throw await this.toError(T, o, r);
    return T;
  }
  async readJson(o) {
    const r = await o.text();
    if (!r)
      throw new Wu({
        status: o.status,
        code: "parse",
        message: `Empty body from ${o.url}`
      });
    try {
      return JSON.parse(r);
    } catch (s) {
      throw new Wu({
        status: o.status,
        code: "parse",
        message: `Could not parse JSON from ${o.url}: ${s.message}`,
        raw: r
      });
    }
  }
  async toError(o, r, s) {
    const T = o.status === 404 ? "notFound" : o.status === 410 ? "gone" : o.status === 409 ? "conflict" : o.status === 400 ? "badRequest" : (o.status >= 500, "server"), E = await o.text();
    if (!E)
      return new Wu({
        status: o.status,
        code: T,
        message: `${r} ${s} → ${o.status}`
      });
    let C;
    try {
      C = JSON.parse(E);
    } catch {
      return new Wu({
        status: o.status,
        code: T,
        message: `${r} ${s} → ${o.status}: ${E.slice(0, 200)}`,
        raw: E
      });
    }
    const U = C.Message ?? C.message, N = C.Errors ?? C.errors, b = Array.isArray(N) ? N.flatMap((H) => {
      const p = H.QuestionId ?? H.questionId, D = H.Message ?? H.message;
      return p && D ? [{ questionId: p, message: D }] : [];
    }) : void 0;
    return new Wu({
      status: o.status,
      code: T,
      message: `${r} ${s} → ${o.status}${U ? ": " + U : ""}`,
      serverMessage: U,
      validationErrors: b && b.length > 0 ? b : void 0,
      raw: C
    });
  }
}
const fy = al.createContext(null), sm = fy.Provider;
function et() {
  const c = al.useContext(fy);
  if (!c)
    throw new Error(
      "useSurveyContext must be used inside <SurveyRenderer>. Question components rely on survey state from the enclosing provider."
    );
  return c;
}
function tl(c, o, r) {
  if (c == null) return "";
  if (typeof c == "string") return c;
  if (c[o]) return c[o];
  if (r && c[r]) return c[r];
  const s = Object.keys(c);
  return s.length > 0 ? c[s[0]] : "";
}
const sy = {
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
    yes: "Yes",
    no: "No"
  }
}, rm = {
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
    yes: "نعم",
    no: "لا"
  }
}, om = { en: sy, ar: rm };
function dm(c, o, r) {
  const s = { ...om, ...r ?? {} };
  return s[c] ?? (o ? s[o] : void 0) ?? s.en ?? sy;
}
const ym = "adp-surveys", vm = 1;
function hm(c = {}) {
  const o = typeof window < "u", r = o && window.parent !== window, s = c.enabled ?? r, T = c.target ?? (o ? window.parent : null), E = c.targetOrigin ?? "*";
  if (!s || !T)
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
  const C = (U, N) => {
    const b = {
      source: ym,
      version: vm,
      type: U,
      payload: N
    };
    try {
      T.postMessage(b, E);
    } catch {
    }
  };
  return {
    loaded: () => C("survey:loaded", {}),
    screenChanged: (U) => C("survey:screen-changed", { screenId: U }),
    completed: (U) => C("survey:completed", U),
    error: (U) => C("survey:error", { message: U }),
    resize: (U) => C("survey:resize", { height: U })
  };
}
function Kf(c) {
  return `adp-surveys:resume:${c}`;
}
function mm(c, o) {
  try {
    const r = c.getItem(Kf(o));
    if (!r) return null;
    const s = JSON.parse(r);
    return !s || typeof s != "object" || !s.answers ? null : s;
  } catch {
    return null;
  }
}
function gm(c, o, r) {
  try {
    const s = { ...r, savedAt: Date.now() };
    c.setItem(Kf(o), JSON.stringify(s));
  } catch {
  }
}
function bm(c, o) {
  try {
    c.removeItem(Kf(o));
  } catch {
  }
}
function Sm({
  question: c,
  registry: o
}) {
  const { ui: r } = et(), s = c.type, T = s ? o[s] : void 0;
  return T ? /* @__PURE__ */ x.jsx(T, { question: c }) : /* @__PURE__ */ x.jsx("div", { className: "survey-question survey-question--unknown", children: /* @__PURE__ */ x.jsxs("em", { children: [
    r.unsupportedQuestion,
    " ",
    String(s ?? "missing")
  ] }) });
}
function pm({ question: c }) {
  const { locale: o, schema: r, answers: s, setAnswer: T } = et(), E = c.id, C = c.title, U = c.help, N = !!c.required, b = s[E] ?? "";
  return /* @__PURE__ */ x.jsxs("div", { className: "survey-question survey-question--text", children: [
    /* @__PURE__ */ x.jsxs("label", { className: "survey-question__label", htmlFor: `q-${E}`, children: [
      tl(C, o, r.defaultLocale),
      N && /* @__PURE__ */ x.jsx("span", { "aria-label": "required", className: "survey-question__required", children: " *" })
    ] }),
    U && /* @__PURE__ */ x.jsx("p", { className: "survey-question__help", children: tl(U, o, r.defaultLocale) }),
    /* @__PURE__ */ x.jsx(
      "input",
      {
        id: `q-${E}`,
        className: "survey-question__input",
        type: "text",
        value: b,
        required: N,
        onChange: (H) => T(E, H.target.value)
      }
    )
  ] });
}
function _m({ question: c }) {
  const { locale: o, schema: r, answers: s, setAnswer: T } = et(), E = c.id, C = c.title, U = c.help, N = !!c.required, b = Number(c.min ?? 0), H = Number(c.max ?? 10), p = c.lowLabel, D = c.highLabel, X = s[E], k = [];
  for (let K = b; K <= H; K++) k.push(K);
  return /* @__PURE__ */ x.jsxs("fieldset", { className: "survey-question survey-question--nps", children: [
    /* @__PURE__ */ x.jsxs("legend", { className: "survey-question__label", children: [
      tl(C, o, r.defaultLocale),
      N && /* @__PURE__ */ x.jsx("span", { "aria-label": "required", className: "survey-question__required", children: " *" })
    ] }),
    U && /* @__PURE__ */ x.jsx("p", { className: "survey-question__help", children: tl(U, o, r.defaultLocale) }),
    /* @__PURE__ */ x.jsx("div", { className: "survey-question__nps-scale", role: "radiogroup", children: k.map((K) => {
      const G = X === K;
      return /* @__PURE__ */ x.jsx(
        "button",
        {
          type: "button",
          role: "radio",
          "aria-checked": G,
          className: "survey-question__nps-step" + (G ? " survey-question__nps-step--selected" : ""),
          onClick: () => T(E, K),
          children: K
        },
        K
      );
    }) }),
    (p || D) && /* @__PURE__ */ x.jsxs("div", { className: "survey-question__nps-labels", children: [
      /* @__PURE__ */ x.jsx("span", { children: p ? tl(p, o, r.defaultLocale) : "" }),
      /* @__PURE__ */ x.jsx("span", { children: D ? tl(D, o, r.defaultLocale) : "" })
    ] })
  ] });
}
function Em({ question: c }) {
  const { locale: o, schema: r } = et(), s = c.id, T = c.title, E = c.help, C = c.options ?? [], U = (N, b) => {
    const H = {
      questionId: s,
      option: {
        id: b.id,
        nextScreen: b.nextScreen
      }
    };
    N.currentTarget.dispatchEvent(
      new CustomEvent("survey:navigationListSelect", {
        detail: H,
        bubbles: !0
      })
    );
  };
  return /* @__PURE__ */ x.jsxs("div", { className: "survey-question survey-question--navlist", children: [
    /* @__PURE__ */ x.jsx("div", { className: "survey-question__label", children: tl(T, o, r.defaultLocale) }),
    E && /* @__PURE__ */ x.jsx("p", { className: "survey-question__help", children: tl(E, o, r.defaultLocale) }),
    /* @__PURE__ */ x.jsx("ul", { className: "survey-navlist", role: "radiogroup", "aria-description": "Selecting an option navigates to the next screen.", children: C.map((N) => {
      const b = N.id, H = N.label;
      return /* @__PURE__ */ x.jsx("li", { className: "survey-navlist__row", children: /* @__PURE__ */ x.jsxs(
        "button",
        {
          type: "button",
          className: "survey-navlist__button",
          onClick: (p) => U(p, N),
          children: [
            /* @__PURE__ */ x.jsx("span", { className: "survey-navlist__label", children: tl(H, o, r.defaultLocale) }),
            /* @__PURE__ */ x.jsx("span", { "aria-hidden": "true", className: "survey-navlist__chevron", children: "›" })
          ]
        }
      ) }, b);
    }) })
  ] });
}
function zm({ question: c }) {
  const { locale: o, schema: r, answers: s, setAnswer: T } = et(), E = c.id, C = c.title, U = c.help, N = c.placeholder, b = !!c.required, H = c.minLength, p = c.maxLength, D = s[E] ?? "";
  return /* @__PURE__ */ x.jsxs("div", { className: "survey-question survey-question--paragraph", children: [
    /* @__PURE__ */ x.jsxs("label", { className: "survey-question__label", htmlFor: `q-${E}`, children: [
      tl(C, o, r.defaultLocale),
      b && /* @__PURE__ */ x.jsx("span", { "aria-label": "required", className: "survey-question__required", children: " *" })
    ] }),
    U && /* @__PURE__ */ x.jsx("p", { className: "survey-question__help", children: tl(U, o, r.defaultLocale) }),
    /* @__PURE__ */ x.jsx(
      "textarea",
      {
        id: `q-${E}`,
        className: "survey-question__textarea",
        value: D,
        required: b,
        rows: 5,
        minLength: H,
        maxLength: p,
        placeholder: N ? tl(N, o, r.defaultLocale) : void 0,
        onChange: (X) => T(E, X.target.value)
      }
    )
  ] });
}
function qm({ question: c }) {
  const { locale: o, schema: r, answers: s, setAnswer: T } = et(), E = c.id, C = c.title, U = c.help, N = !!c.required, b = c.min, H = c.max, p = c.step, D = c.unit, X = s[E], k = X == null ? "" : String(X);
  return /* @__PURE__ */ x.jsxs("div", { className: "survey-question survey-question--number", children: [
    /* @__PURE__ */ x.jsxs("label", { className: "survey-question__label", htmlFor: `q-${E}`, children: [
      tl(C, o, r.defaultLocale),
      N && /* @__PURE__ */ x.jsx("span", { "aria-label": "required", className: "survey-question__required", children: " *" })
    ] }),
    U && /* @__PURE__ */ x.jsx("p", { className: "survey-question__help", children: tl(U, o, r.defaultLocale) }),
    /* @__PURE__ */ x.jsxs("div", { className: "survey-question__number-wrap", children: [
      /* @__PURE__ */ x.jsx(
        "input",
        {
          id: `q-${E}`,
          className: "survey-question__input",
          type: "number",
          value: k,
          required: N,
          min: b,
          max: H,
          step: p,
          onChange: (K) => {
            const G = K.target.value;
            T(E, G === "" ? null : Number(G));
          }
        }
      ),
      D && /* @__PURE__ */ x.jsx("span", { className: "survey-question__unit", children: tl(D, o, r.defaultLocale) })
    ] })
  ] });
}
function Am({ question: c }) {
  const { locale: o, schema: r, answers: s, setAnswer: T } = et(), E = c.id, C = c.title, U = c.help, N = !!c.required, b = Number(c.max ?? 5), H = s[E], p = [];
  for (let D = 1; D <= b; D++) p.push(D);
  return /* @__PURE__ */ x.jsxs("fieldset", { className: "survey-question survey-question--rating", children: [
    /* @__PURE__ */ x.jsxs("legend", { className: "survey-question__label", children: [
      tl(C, o, r.defaultLocale),
      N && /* @__PURE__ */ x.jsx("span", { "aria-label": "required", className: "survey-question__required", children: " *" })
    ] }),
    U && /* @__PURE__ */ x.jsx("p", { className: "survey-question__help", children: tl(U, o, r.defaultLocale) }),
    /* @__PURE__ */ x.jsx("div", { className: "survey-question__rating-scale", role: "radiogroup", children: p.map((D) => {
      const X = typeof H == "number" && D <= H;
      return /* @__PURE__ */ x.jsx(
        "button",
        {
          type: "button",
          role: "radio",
          "aria-checked": H === D,
          "aria-label": `${D}`,
          className: "survey-question__rating-star" + (X ? " survey-question__rating-star--selected" : ""),
          onClick: () => T(E, D),
          children: /* @__PURE__ */ x.jsx("span", { "aria-hidden": "true", children: "★" })
        },
        D
      );
    }) })
  ] });
}
function Tm({ question: c }) {
  const { locale: o, schema: r, answers: s, setAnswer: T } = et(), E = c.id, C = c.title, U = c.help, N = !!c.required, b = c.options ?? [], H = s[E];
  return /* @__PURE__ */ x.jsxs("fieldset", { className: "survey-question survey-question--single", children: [
    /* @__PURE__ */ x.jsxs("legend", { className: "survey-question__label", children: [
      tl(C, o, r.defaultLocale),
      N && /* @__PURE__ */ x.jsx("span", { "aria-label": "required", className: "survey-question__required", children: " *" })
    ] }),
    U && /* @__PURE__ */ x.jsx("p", { className: "survey-question__help", children: tl(U, o, r.defaultLocale) }),
    /* @__PURE__ */ x.jsx("div", { className: "survey-question__options", children: b.map((p) => /* @__PURE__ */ x.jsxs("label", { className: "survey-question__option", children: [
      /* @__PURE__ */ x.jsx(
        "input",
        {
          type: "radio",
          name: `q-${E}`,
          value: p.id,
          checked: H === p.id,
          onChange: () => T(E, p.id)
        }
      ),
      /* @__PURE__ */ x.jsx("span", { children: tl(p.label, o, r.defaultLocale) })
    ] }, p.id)) })
  ] });
}
function xm({ question: c }) {
  const { locale: o, schema: r, answers: s, setAnswer: T } = et(), E = c.id, C = c.title, U = c.help, N = !!c.required, b = c.options ?? [], H = c.maxSelected, p = s[E] ?? [], D = (X) => {
    if (p.includes(X)) {
      T(E, p.filter((k) => k !== X));
      return;
    }
    H !== void 0 && p.length >= H || T(E, [...p, X]);
  };
  return /* @__PURE__ */ x.jsxs("fieldset", { className: "survey-question survey-question--multi", children: [
    /* @__PURE__ */ x.jsxs("legend", { className: "survey-question__label", children: [
      tl(C, o, r.defaultLocale),
      N && /* @__PURE__ */ x.jsx("span", { "aria-label": "required", className: "survey-question__required", children: " *" })
    ] }),
    U && /* @__PURE__ */ x.jsx("p", { className: "survey-question__help", children: tl(U, o, r.defaultLocale) }),
    /* @__PURE__ */ x.jsx("div", { className: "survey-question__options", children: b.map((X) => {
      const k = p.includes(X.id);
      return /* @__PURE__ */ x.jsxs("label", { className: "survey-question__option", children: [
        /* @__PURE__ */ x.jsx(
          "input",
          {
            type: "checkbox",
            checked: k,
            onChange: () => D(X.id)
          }
        ),
        /* @__PURE__ */ x.jsx("span", { children: tl(X.label, o, r.defaultLocale) })
      ] }, X.id);
    }) })
  ] });
}
function Nm({ question: c }) {
  const { locale: o, schema: r, answers: s, setAnswer: T, ui: E } = et(), C = c.id, U = c.title, N = c.help, b = !!c.required, H = c.options ?? [], p = c.placeholder, D = s[C] ?? "", X = p ? tl(p, o, r.defaultLocale) : E.selectPlaceholder;
  return /* @__PURE__ */ x.jsxs("div", { className: "survey-question survey-question--dropdown", children: [
    /* @__PURE__ */ x.jsxs("label", { className: "survey-question__label", htmlFor: `q-${C}`, children: [
      tl(U, o, r.defaultLocale),
      b && /* @__PURE__ */ x.jsx("span", { "aria-label": "required", className: "survey-question__required", children: " *" })
    ] }),
    N && /* @__PURE__ */ x.jsx("p", { className: "survey-question__help", children: tl(N, o, r.defaultLocale) }),
    /* @__PURE__ */ x.jsxs(
      "select",
      {
        id: `q-${C}`,
        className: "survey-question__select",
        value: D,
        required: b,
        onChange: (k) => T(C, k.target.value || null),
        children: [
          /* @__PURE__ */ x.jsx("option", { value: "", children: X }),
          H.map((k) => /* @__PURE__ */ x.jsx("option", { value: k.id, children: tl(k.label, o, r.defaultLocale) }, k.id))
        ]
      }
    )
  ] });
}
function Om({ question: c }) {
  const { locale: o, schema: r, answers: s, setAnswer: T } = et(), E = c.id, C = c.title, U = c.help, N = !!c.required, b = c.minDate, H = c.maxDate, p = s[E] ?? "";
  return /* @__PURE__ */ x.jsxs("div", { className: "survey-question survey-question--date", children: [
    /* @__PURE__ */ x.jsxs("label", { className: "survey-question__label", htmlFor: `q-${E}`, children: [
      tl(C, o, r.defaultLocale),
      N && /* @__PURE__ */ x.jsx("span", { "aria-label": "required", className: "survey-question__required", children: " *" })
    ] }),
    U && /* @__PURE__ */ x.jsx("p", { className: "survey-question__help", children: tl(U, o, r.defaultLocale) }),
    /* @__PURE__ */ x.jsx(
      "input",
      {
        id: `q-${E}`,
        className: "survey-question__input",
        type: "date",
        value: p,
        required: N,
        min: b,
        max: H,
        onChange: (D) => T(E, D.target.value || null)
      }
    )
  ] });
}
function Mm({ question: c }) {
  const { locale: o, schema: r, answers: s, setAnswer: T } = et(), E = c.id, C = c.title, U = c.help, N = !!c.required, b = c.minDateTime, H = c.maxDateTime, p = s[E] ?? "", D = (X) => {
    if (!X) return;
    const k = X.match(/^(\d{4}-\d{2}-\d{2}T\d{2}:\d{2})/);
    return (k == null ? void 0 : k[1]) ?? void 0;
  };
  return /* @__PURE__ */ x.jsxs("div", { className: "survey-question survey-question--datetime", children: [
    /* @__PURE__ */ x.jsxs("label", { className: "survey-question__label", htmlFor: `q-${E}`, children: [
      tl(C, o, r.defaultLocale),
      N && /* @__PURE__ */ x.jsx("span", { "aria-label": "required", className: "survey-question__required", children: " *" })
    ] }),
    U && /* @__PURE__ */ x.jsx("p", { className: "survey-question__help", children: tl(U, o, r.defaultLocale) }),
    /* @__PURE__ */ x.jsx(
      "input",
      {
        id: `q-${E}`,
        className: "survey-question__input",
        type: "datetime-local",
        value: D(p) ?? "",
        required: N,
        min: D(b),
        max: D(H),
        onChange: (X) => T(E, X.target.value || null)
      }
    )
  ] });
}
function Dm({ question: c }) {
  const { locale: o, schema: r, answers: s, setAnswer: T } = et(), E = c.id, C = c.title, U = c.help, N = !!c.required, b = c.acceptedTypes, H = al.useRef(null), p = s[E], D = b && b.length > 0 ? b.join(",") : void 0;
  return /* @__PURE__ */ x.jsxs("div", { className: "survey-question survey-question--file", children: [
    /* @__PURE__ */ x.jsxs("label", { className: "survey-question__label", htmlFor: `q-${E}`, children: [
      tl(C, o, r.defaultLocale),
      N && /* @__PURE__ */ x.jsx("span", { "aria-label": "required", className: "survey-question__required", children: " *" })
    ] }),
    U && /* @__PURE__ */ x.jsx("p", { className: "survey-question__help", children: tl(U, o, r.defaultLocale) }),
    /* @__PURE__ */ x.jsx(
      "input",
      {
        ref: H,
        id: `q-${E}`,
        className: "survey-question__file",
        type: "file",
        required: N,
        accept: D,
        onChange: (X) => {
          var k;
          const K = (k = X.target.files) == null ? void 0 : k[0];
          if (!K) {
            T(E, null);
            return;
          }
          T(E, { name: K.name, size: K.size, type: K.type });
        }
      }
    ),
    (p == null ? void 0 : p.name) && /* @__PURE__ */ x.jsxs("p", { className: "survey-question__file-name", children: [
      "Selected: ",
      p.name
    ] })
  ] });
}
const Lf = 480, Gf = 160;
function Um({ question: c }) {
  const { locale: o, schema: r, answers: s, setAnswer: T, ui: E } = et(), C = c.id, U = c.title, N = c.help, b = !!c.required, H = al.useRef(null), [p, D] = al.useState(!1), [X, k] = al.useState(!!s[C]), K = () => {
    var W;
    return ((W = H.current) == null ? void 0 : W.getContext("2d")) ?? null;
  }, G = (W) => {
    const I = W.target.getBoundingClientRect();
    return {
      x: (W.clientX - I.left) / I.width * Lf,
      y: (W.clientY - I.top) / I.height * Gf
    };
  }, Z = al.useCallback(() => {
    var W;
    const I = (W = H.current) == null ? void 0 : W.toDataURL("image/png");
    I && T(C, I);
  }, [C, T]), ol = () => {
    const W = K();
    W && (W.clearRect(0, 0, Lf, Gf), k(!1), T(C, null));
  };
  return al.useEffect(() => {
    const W = K();
    W && (W.lineWidth = 2, W.lineCap = "round", W.strokeStyle = "#111");
  }, []), /* @__PURE__ */ x.jsxs("div", { className: "survey-question survey-question--signature", children: [
    /* @__PURE__ */ x.jsxs("div", { className: "survey-question__label", children: [
      tl(U, o, r.defaultLocale),
      b && /* @__PURE__ */ x.jsx("span", { "aria-label": "required", className: "survey-question__required", children: " *" })
    ] }),
    N && /* @__PURE__ */ x.jsx("p", { className: "survey-question__help", children: tl(N, o, r.defaultLocale) }),
    /* @__PURE__ */ x.jsx(
      "canvas",
      {
        ref: H,
        className: "survey-question__signature-canvas",
        width: Lf,
        height: Gf,
        role: "img",
        "aria-label": "signature pad",
        onPointerDown: (W) => {
          W.target.setPointerCapture(W.pointerId);
          const I = K();
          if (!I) return;
          const { x: Il, y: $ } = G(W);
          I.beginPath(), I.moveTo(Il, $), D(!0);
        },
        onPointerMove: (W) => {
          if (!p) return;
          const I = K();
          if (!I) return;
          const { x: Il, y: $ } = G(W);
          I.lineTo(Il, $), I.stroke(), k(!0);
        },
        onPointerUp: () => {
          D(!1), X && Z();
        }
      }
    ),
    /* @__PURE__ */ x.jsx("div", { className: "survey-question__signature-actions", children: /* @__PURE__ */ x.jsx("button", { type: "button", className: "survey-button survey-button--ghost", onClick: ol, children: E.clearSignature }) })
  ] });
}
function Cm({ question: c }) {
  const { locale: o, schema: r, answers: s, setAnswer: T, ui: E } = et(), C = c.id, U = c.title, N = c.help, b = !!c.required, H = c.yesLabel, p = c.noLabel, D = s[C], X = H ? tl(H, o, r.defaultLocale) : E.yes, k = p ? tl(p, o, r.defaultLocale) : E.no;
  return /* @__PURE__ */ x.jsxs("fieldset", { className: "survey-question survey-question--yesno", children: [
    /* @__PURE__ */ x.jsxs("legend", { className: "survey-question__label", children: [
      tl(U, o, r.defaultLocale),
      b && /* @__PURE__ */ x.jsx("span", { "aria-label": "required", className: "survey-question__required", children: " *" })
    ] }),
    N && /* @__PURE__ */ x.jsx("p", { className: "survey-question__help", children: tl(N, o, r.defaultLocale) }),
    /* @__PURE__ */ x.jsxs("div", { className: "survey-question__yesno", role: "radiogroup", children: [
      /* @__PURE__ */ x.jsx(
        "button",
        {
          type: "button",
          role: "radio",
          "aria-checked": D === !0,
          className: "survey-question__yesno-button" + (D === !0 ? " survey-question__yesno-button--selected" : ""),
          onClick: () => T(C, !0),
          children: X
        }
      ),
      /* @__PURE__ */ x.jsx(
        "button",
        {
          type: "button",
          role: "radio",
          "aria-checked": D === !1,
          className: "survey-question__yesno-button" + (D === !1 ? " survey-question__yesno-button--selected" : ""),
          onClick: () => T(C, !1),
          children: k
        }
      )
    ] })
  ] });
}
const jm = {
  text: pm,
  paragraph: zm,
  number: qm,
  rating: Am,
  nps: _m,
  singleChoice: Tm,
  multiChoice: xm,
  dropdown: Nm,
  date: Om,
  dateTime: Mm,
  file: Dm,
  signature: Um,
  yesNo: Cm,
  navigationList: Em
};
function Rm({
  schema: c,
  onSubmit: o,
  initialAnswers: r,
  locale: s,
  onScreenChange: T,
  onCompleted: E,
  registry: C,
  submissionMeta: U,
  uiLocales: N,
  resumeKey: b,
  storage: H,
  emitHostMessages: p,
  hostMessageOrigin: D,
  hostMessageTarget: X
}) {
  var k;
  const K = s ?? c.defaultLocale ?? "en", G = C ?? jm, Z = al.useMemo(
    () => dm(K, c.defaultLocale, N),
    [K, c.defaultLocale, N]
  ), ol = H ?? (typeof globalThis < "u" ? globalThis.localStorage : void 0), W = al.useMemo(() => {
    var J;
    if (!b || !ol) return null;
    const bl = mm(ol, b);
    return bl ? bl.currentScreenId === null || c.screens.some((Ol) => Ol.id === bl.currentScreenId) ? bl : { ...bl, currentScreenId: ((J = c.screens[0]) == null ? void 0 : J.id) ?? null } : null;
  }, []), [I, Il] = al.useState(() => ({
    ...r ?? {},
    ...(W == null ? void 0 : W.answers) ?? {}
  })), [$, nl] = al.useState(
    () => {
      var J;
      return (W == null ? void 0 : W.currentScreenId) ?? ((J = c.screens[0]) == null ? void 0 : J.id) ?? null;
    }
  );
  al.useEffect(() => {
    if (c.screens.length === 0) {
      $ !== null && nl(null);
      return;
    }
    $ !== null && c.screens.some((J) => J.id === $) || nl(c.screens[0].id);
  }, [c, $]);
  const [Ql, ot] = al.useState(!1), [qt, dt] = al.useState(null), [Pl, Qt] = al.useState(/* @__PURE__ */ new Set()), [Jl, yt] = al.useState(!1), q = al.useRef((/* @__PURE__ */ new Date()).toISOString()), j = al.useRef(null);
  if (j.current === null) {
    const J = {};
    X !== void 0 && (J.target = X), D !== void 0 && (J.targetOrigin = D), p !== void 0 && (J.enabled = p), j.current = hm(J);
  }
  const B = al.useMemo(
    () => $ ? c.screens.find((J) => J.id === $) ?? null : null,
    [c, $]
  );
  al.useEffect(() => {
    var J;
    T == null || T($), (J = j.current) == null || J.screenChanged($);
  }, [$, T]);
  const ml = al.useRef(!1);
  al.useEffect(() => {
    var J;
    ml.current || !$ || (ml.current = !0, (J = j.current) == null || J.loaded());
  }, [$]), al.useEffect(() => {
    !b || !ol || Jl || gm(ol, b, {
      answers: I,
      currentScreenId: $,
      schemaVersion: c.version
    });
  }, [I, $, b, ol, Jl, c.version]), al.useEffect(() => {
    Jl && b && ol && bm(ol, b);
  }, [Jl, b, ol]), al.useEffect(() => {
    var J;
    qt && ((J = j.current) == null || J.error(qt));
  }, [qt]);
  const dl = al.useCallback((J, bl) => {
    Il((Ol) => ({ ...Ol, [J]: bl }));
  }, []), v = al.useCallback(
    (J) => {
      J !== null && (Qt(/* @__PURE__ */ new Set()), nl(J));
    },
    []
  ), O = al.useCallback(
    (J) => {
      if (!J.required) return !1;
      const bl = I[J.id];
      return !!(bl == null || typeof bl == "string" && bl.trim() === "" || Array.isArray(bl) && bl.length === 0);
    },
    [I]
  ), R = al.useCallback(async () => {
    var J;
    ot(!0), dt(null);
    try {
      await o({
        schemaVersion: c.version ?? 0,
        answers: I,
        meta: {
          startedAt: (U == null ? void 0 : U.startedAt) ?? q.current,
          completedAt: (U == null ? void 0 : U.completedAt) ?? (/* @__PURE__ */ new Date()).toISOString(),
          ...U ?? {}
        }
      }), yt(!0), E == null || E($), (J = j.current) == null || J.completed({ screenId: $, answers: I });
    } catch (bl) {
      dt(bl.message ?? String(bl));
    } finally {
      ot(!1);
    }
  }, [c.version, I, U, o, E, $]), L = al.useCallback(() => {
    if (!$) return;
    const J = c.screens.find((Hl) => Hl.id === $), bl = ((J == null ? void 0 : J.questions) ?? []).filter(O).map((Hl) => Hl.id);
    if (bl.length > 0) {
      Qt(new Set(bl));
      return;
    }
    const Ol = Xf(c, $, I);
    Ol.kind === "end" ? R() : v(Ol.screenId);
  }, [c, $, I, O, v, R]), F = al.useRef(null);
  al.useEffect(() => {
    Jl || Ql || !$ || !B || F.current === $ || !(!B.questions || B.questions.length === 0) || Xf(c, $, I).kind === "end" && (F.current = $, R());
  }, [$, B, Jl, Ql, c, I, R]);
  const ll = al.useRef(null);
  al.useEffect(() => {
    const J = ll.current;
    if (!J || typeof ResizeObserver > "u") return;
    const bl = new ResizeObserver((Ol) => {
      var Hl;
      const ve = Ol[0];
      ve && ((Hl = j.current) == null || Hl.resize(Math.ceil(ve.contentRect.height)));
    });
    return bl.observe(J), () => bl.disconnect();
  }, []), al.useEffect(() => {
    const J = ll.current;
    if (!J) return;
    const bl = (Ol) => {
      const Hl = Ol.detail;
      if (!Hl || !$) return;
      dl(Hl.questionId, Hl.option.id);
      const ve = { ...I, [Hl.questionId]: Hl.option.id }, Wa = fm(
        Hl.option,
        c,
        $,
        ve
      );
      Wa.kind === "end" ? R() : v(Wa.screenId);
    };
    return J.addEventListener("survey:navigationListSelect", bl), () => J.removeEventListener("survey:navigationListSelect", bl);
  }, [I, $, c, dl, v, R]);
  const yl = al.useMemo(
    () => ({
      schema: c,
      locale: K,
      direction: Z.direction,
      ui: Z.strings,
      answers: I,
      setAnswer: dl
    }),
    [c, K, Z, I, dl]
  );
  if (Jl)
    return /* @__PURE__ */ x.jsx(
      "div",
      {
        ref: ll,
        className: "survey-root survey-root--done",
        dir: Z.direction,
        lang: K,
        children: /* @__PURE__ */ x.jsxs("div", { className: "survey-screen", children: [
          /* @__PURE__ */ x.jsx("h2", { className: "survey-screen__title", children: B != null && B.title ? tl(B.title, K, c.defaultLocale) : Z.strings.thankYou }),
          (B == null ? void 0 : B.description) && /* @__PURE__ */ x.jsx("p", { className: "survey-screen__description", children: tl(B.description, K, c.defaultLocale) })
        ] })
      }
    );
  if (!B)
    return /* @__PURE__ */ x.jsx("div", { ref: ll, className: "survey-root", dir: Z.direction, lang: K, children: /* @__PURE__ */ x.jsx("div", { className: "survey-screen", children: /* @__PURE__ */ x.jsx("em", { children: Z.strings.noScreens }) }) });
  const Ul = B.questions ?? [], Nl = Ul.length > 0 && ((k = Ul[Ul.length - 1]) == null ? void 0 : k.type) === "navigationList", Ze = Ul.length === 0 && !B.nextScreen, ou = !Nl && !Ze;
  return /* @__PURE__ */ x.jsx(sm, { value: yl, children: /* @__PURE__ */ x.jsx("div", { ref: ll, className: "survey-root", dir: Z.direction, lang: K, children: /* @__PURE__ */ x.jsxs("div", { className: "survey-screen", children: [
    B.title && /* @__PURE__ */ x.jsx("h2", { className: "survey-screen__title", children: tl(B.title, K, c.defaultLocale) }),
    B.description && /* @__PURE__ */ x.jsx("p", { className: "survey-screen__description", children: tl(B.description, K, c.defaultLocale) }),
    /* @__PURE__ */ x.jsx("div", { className: "survey-screen__questions", children: Ul.map((J, bl) => {
      const Ol = J.id, Hl = Ol !== void 0 && Pl.has(Ol) && O(J);
      return /* @__PURE__ */ x.jsxs("div", { className: Hl ? "survey-question-slot survey-question-slot--invalid" : "survey-question-slot", children: [
        /* @__PURE__ */ x.jsx(Sm, { question: J, registry: G }),
        Hl && /* @__PURE__ */ x.jsx("p", { className: "survey-question__required-error", role: "alert", children: Z.strings.requiredError })
      ] }, Ol ?? bl);
    }) }),
    ou && /* @__PURE__ */ x.jsx("div", { className: "survey-screen__actions", children: /* @__PURE__ */ x.jsx(
      "button",
      {
        type: "button",
        className: "survey-button survey-button--primary",
        disabled: Ql,
        onClick: L,
        children: Ql ? Z.strings.submitting : Z.strings.next
      }
    ) }),
    qt && /* @__PURE__ */ x.jsxs("p", { className: "survey-screen__error", role: "alert", children: [
      Z.strings.couldNotSubmit,
      " ",
      qt
    ] })
  ] }) }) });
}
const Hm = ".survey-root{font-family:system-ui,-apple-system,Segoe UI,Roboto,sans-serif;color:#111;max-width:640px;margin:0 auto;padding:32px 16px}.survey-screen{display:flex;flex-direction:column;gap:24px}.survey-screen__title{font-size:1.5rem;font-weight:600;margin:0}.survey-screen__description{color:#555;margin:0}.survey-screen__questions{display:flex;flex-direction:column;gap:24px}.survey-screen__actions{display:flex;justify-content:flex-end}.survey-screen__error{color:#b42318;background:#fef3f2;border:1px solid #fecdca;padding:12px 14px;border-radius:8px;margin:0}.survey-question-slot--invalid{border-inline-start:3px solid #b42318;padding-inline-start:10px}.survey-question__required-error{color:#b42318;font-size:.9rem;margin:4px 0 0}.survey-question{display:flex;flex-direction:column;gap:8px}.survey-question__label{font-weight:600;display:block}.survey-question__required{color:#b42318}.survey-question__help{margin:0;color:#666;font-size:.9rem}.survey-question__input{padding:10px 12px;border:1px solid #d0d5dd;border-radius:8px;font:inherit}.survey-question__input:focus-visible{outline:2px solid #2563eb;outline-offset:1px;border-color:#2563eb}.survey-question--nps{border:none;padding:0;margin:0;display:flex;flex-direction:column;gap:8px}.survey-question__nps-scale{display:flex;gap:6px;flex-wrap:wrap}.survey-question__nps-step{min-width:40px;min-height:40px;padding:8px;border:1px solid #d0d5dd;border-radius:8px;background:#fff;font-weight:500;cursor:pointer}.survey-question__nps-step:hover{background:#f5f7fa}.survey-question__nps-step--selected{background:#2563eb;border-color:#2563eb;color:#fff}.survey-question__nps-labels{display:flex;justify-content:space-between;color:#555;font-size:.85rem}.survey-question--navlist{gap:12px}.survey-navlist{list-style:none;padding:0;margin:0;display:flex;flex-direction:column;gap:8px}.survey-navlist__row{margin:0}.survey-navlist__button{width:100%;display:flex;align-items:center;justify-content:space-between;padding:14px 16px;border:1px solid #d0d5dd;border-radius:10px;background:#fff;cursor:pointer;font:inherit;text-align:start}.survey-navlist__button:hover{background:#f5f7fa;border-color:#2563eb}.survey-navlist__chevron{font-size:1.5rem;color:#667085}.survey-root[dir=rtl] .survey-navlist__chevron{transform:scaleX(-1)}.survey-navlist__label{font-weight:500}.survey-question__textarea{padding:10px 12px;border:1px solid #d0d5dd;border-radius:8px;font:inherit;resize:vertical;min-height:96px}.survey-question__textarea:focus-visible{outline:2px solid #2563eb;outline-offset:1px;border-color:#2563eb}.survey-question__number-wrap{display:flex;align-items:center;gap:8px}.survey-question__number-wrap .survey-question__input{flex:1}.survey-question__unit{color:#555;font-size:.9rem}.survey-question__rating-scale{display:flex;gap:4px}.survey-question__rating-star{background:transparent;border:none;cursor:pointer;font-size:1.8rem;line-height:1;color:#d0d5dd;padding:4px}.survey-question__rating-star:hover,.survey-question__rating-star--selected{color:#f5b60c}.survey-question__options{display:flex;flex-direction:column;gap:8px}.survey-question__option{display:flex;align-items:center;gap:8px;padding:8px 12px;border:1px solid #d0d5dd;border-radius:8px;cursor:pointer}.survey-question__option:hover{background:#f5f7fa;border-color:#2563eb}.survey-question__option input{margin:0}.survey-question__select{padding:10px 12px;border:1px solid #d0d5dd;border-radius:8px;font:inherit;background:#fff}.survey-question__yesno{display:flex;gap:12px}.survey-question__yesno-button{flex:1;padding:14px 16px;border:1px solid #d0d5dd;border-radius:10px;background:#fff;font:inherit;font-weight:500;cursor:pointer}.survey-question__yesno-button:hover{background:#f5f7fa;border-color:#2563eb}.survey-question__yesno-button--selected{background:#2563eb;border-color:#2563eb;color:#fff}.survey-question__file{font:inherit}.survey-question__file-name{color:#555;font-size:.9rem;margin:0}.survey-question__signature-canvas{width:100%;max-width:480px;height:auto;aspect-ratio:3 / 1;border:1px dashed #d0d5dd;border-radius:8px;background:#fff;touch-action:none}.survey-question__signature-actions{display:flex;justify-content:flex-start;gap:8px}.survey-button{padding:10px 20px;border-radius:8px;border:1px solid transparent;cursor:pointer;font:inherit;font-weight:600}.survey-button--primary{background:#2563eb;color:#fff}.survey-button--primary:hover{background:#1e40af}.survey-button--ghost{background:#fff;color:#555;border-color:#d0d5dd}.survey-button--ghost:hover{background:#f5f7fa}.survey-button:disabled{opacity:.5;cursor:not-allowed}";
var Ge, Pu, Gt, Qe, la, su, Kl, Zf, fu, $a, Fu;
class Bm extends HTMLElement {
  constructor() {
    super();
    de(this, Kl);
    /** Schema-mode setter. Assigning this swaps the element into schema mode and
     *  re-renders with the new schema immediately. */
    de(this, Ge, null);
    /** Schema-mode submit handler. In API mode the element manages this itself. */
    de(this, Pu, null);
    de(this, Gt, null);
    de(this, Qe, null);
    de(this, la, null);
    de(this, su, null);
    de(this, $a, !1);
    this.attachShadow({ mode: "open" });
  }
  static get observedAttributes() {
    return ["instance-id", "api-base", "locale", "mode"];
  }
  // ─── Lifecycle ───────────────────────────────────────────────────────────
  connectedCallback() {
    if (this.shadowRoot) {
      if (!this.shadowRoot.querySelector("style[data-shift-survey]")) {
        const r = document.createElement("style");
        r.setAttribute("data-shift-survey", ""), r.textContent = Hm, this.shadowRoot.appendChild(r);
      }
      Rl(this, Qe) || (Lt(this, Qe, document.createElement("div")), Rl(this, Qe).className = "shift-survey-mount", this.shadowRoot.appendChild(Rl(this, Qe))), Rl(this, Gt) || Lt(this, Gt, Qh.createRoot(Rl(this, Qe))), rt(this, Kl, fu).call(this), rt(this, Kl, Zf).call(this);
    }
  }
  disconnectedCallback() {
    queueMicrotask(() => {
      var r;
      if (!(this.isConnected || typeof window > "u")) {
        try {
          (r = Rl(this, Gt)) == null || r.unmount();
        } catch {
        }
        Lt(this, Gt, null);
      }
    });
  }
  attributeChangedCallback(r, s, T) {
    s !== T && ((r === "instance-id" || r === "api-base") && (Lt(this, la, null), Lt(this, su, null), rt(this, Kl, Zf).call(this)), rt(this, Kl, fu).call(this));
  }
  // ─── Properties ──────────────────────────────────────────────────────────
  get schema() {
    return Rl(this, Ge);
  }
  set schema(r) {
    Lt(this, Ge, r), rt(this, Kl, fu).call(this);
  }
  get onSubmit() {
    return Rl(this, Pu);
  }
  set onSubmit(r) {
    Lt(this, Pu, r), rt(this, Kl, fu).call(this);
  }
}
Ge = new WeakMap(), Pu = new WeakMap(), Gt = new WeakMap(), Qe = new WeakMap(), la = new WeakMap(), su = new WeakMap(), Kl = new WeakSet(), // ─── Internals ───────────────────────────────────────────────────────────
Zf = function() {
  if (Rl(this, Ge)) return;
  const r = this.getAttribute("instance-id");
  if (!r) return;
  const s = this.getAttribute("api-base");
  if (!s) return;
  new iy({ baseUrl: s }).fetchSchema(r).then((E) => {
    Lt(this, la, E), rt(this, Kl, fu).call(this);
  }).catch((E) => {
    Lt(this, su, E), rt(this, Kl, Fu).call(this, "survey:error", { message: E.message }), rt(this, Kl, fu).call(this);
  });
}, fu = function() {
  if (!Rl(this, Gt)) return;
  const r = this.getAttribute("api-base"), s = this.getAttribute("instance-id"), T = this.getAttribute("locale") ?? void 0, E = this.getAttribute("mode") === "agent", C = Rl(this, Ge) ?? Rl(this, la);
  if (Rl(this, su) && !C) {
    Rl(this, Gt).render(
      al.createElement(
        "div",
        { className: "shift-survey-error", role: "alert" },
        Rl(this, su).message
      )
    );
    return;
  }
  if (!C) {
    Rl(this, Gt).render(
      al.createElement("div", { className: "shift-survey-loading" }, "Loading…")
    );
    return;
  }
  const U = Rl(this, Ge) ? Rl(this, Pu) ?? ((N) => {
    rt(this, Kl, Fu).call(this, "survey:completed", { ...N });
  }) : async (N) => {
    if (!r || !s)
      throw new Error("shift-survey: API mode requires both instance-id and api-base attributes.");
    await new iy({ baseUrl: r }).submitResponse(s, N);
  };
  Rl(this, Gt).render(
    al.createElement(Rm, {
      schema: C,
      onSubmit: U,
      ...T ? { locale: T } : {},
      // Let the element be the resume key in API mode so two surveys on the
      // same host page don't clobber each other.
      ...s ? { resumeKey: s } : {},
      ...E ? { submissionMeta: { mode: "agent" } } : {},
      // CustomEvents are the web-component's channel; postMessage stays opt-in
      // via iframe auto-detect on the enclosing page (unchanged).
      onScreenChange: (N) => rt(this, Kl, Fu).call(this, "survey:screen-changed", { screenId: N }),
      onCompleted: (N) => rt(this, Kl, Fu).call(this, "survey:completed", { screenId: N })
    })
  ), Rl(this, $a) || (Lt(this, $a, !0), rt(this, Kl, Fu).call(this, "survey:loaded", {}));
}, $a = new WeakMap(), Fu = function(r, s) {
  this.dispatchEvent(
    new CustomEvent(r, { detail: s, bubbles: !0, composed: !0 })
  );
};
function Ym(c = "shift-survey") {
  typeof window > "u" || typeof customElements > "u" || customElements.get(c) || customElements.define(c, Bm);
}
Ym();
export {
  Bm as ShiftSurveyElement,
  Ym as registerShiftSurvey
};
//# sourceMappingURL=index.js.map
