var jh = Object.defineProperty;
var kd = (c) => {
  throw TypeError(c);
};
var Rh = (c, o, r) => o in c ? jh(c, o, { enumerable: !0, configurable: !0, writable: !0, value: r }) : c[o] = r;
var ye = (c, o, r) => Rh(c, typeof o != "symbol" ? o + "" : o, r), Cf = (c, o, r) => o.has(c) || kd("Cannot " + r);
var Ct = (c, o, r) => (Cf(c, o, "read from private field"), r ? r.call(c) : o.get(c)), ve = (c, o, r) => o.has(c) ? kd("Cannot add the same private member more than once") : o instanceof WeakSet ? o.add(c) : o.set(c, r), Yl = (c, o, r, s) => (Cf(c, o, "write to private field"), s ? s.call(c, r) : o.set(c, r), r), dl = (c, o, r) => (Cf(c, o, "access private method"), r);
var jf = { exports: {} }, P = {};
/**
 * @license React
 * react.production.js
 *
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 */
var $d;
function Hh() {
  if ($d) return P;
  $d = 1;
  var c = Symbol.for("react.transitional.element"), o = Symbol.for("react.portal"), r = Symbol.for("react.fragment"), s = Symbol.for("react.strict_mode"), q = Symbol.for("react.profiler"), E = Symbol.for("react.consumer"), C = Symbol.for("react.context"), U = Symbol.for("react.forward_ref"), O = Symbol.for("react.suspense"), b = Symbol.for("react.memo"), j = Symbol.for("react.lazy"), p = Symbol.for("react.activity"), D = Symbol.iterator;
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
  }, F = Object.assign, B = {};
  function W(v, N, R) {
    this.props = v, this.context = N, this.refs = B, this.updater = R || k;
  }
  W.prototype.isReactComponent = {}, W.prototype.setState = function(v, N) {
    if (typeof v != "object" && typeof v != "function" && v != null)
      throw Error(
        "takes an object of state variables to update or a function which returns an object of state variables."
      );
    this.updater.enqueueSetState(this, v, N, "setState");
  }, W.prototype.forceUpdate = function(v) {
    this.updater.enqueueForceUpdate(this, v, "forceUpdate");
  };
  function nt() {
  }
  nt.prototype = W.prototype;
  function w(v, N, R) {
    this.props = v, this.context = N, this.refs = B, this.updater = R || k;
  }
  var dt = w.prototype = new nt();
  dt.constructor = w, F(dt, W.prototype), dt.isPureReactComponent = !0;
  var gt = Array.isArray;
  function jt() {
  }
  var L = { H: null, A: null, T: null, S: null }, Xt = Object.prototype.hasOwnProperty;
  function el(v, N, R) {
    var Y = R.ref;
    return {
      $$typeof: c,
      type: v,
      key: N,
      ref: Y !== void 0 ? Y : null,
      props: R
    };
  }
  function Jl(v, N) {
    return el(v.type, N, v.props);
  }
  function Pt(v) {
    return typeof v == "object" && v !== null && v.$$typeof === c;
  }
  function Jt(v) {
    var N = { "=": "=0", ":": "=2" };
    return "$" + v.replace(/[=:]/g, function(R) {
      return N[R];
    });
  }
  var wl = /\/+/g;
  function zl(v, N) {
    return typeof v == "object" && v !== null && v.key != null ? Jt("" + v.key) : N.toString(36);
  }
  function Lt(v) {
    switch (v.status) {
      case "fulfilled":
        return v.value;
      case "rejected":
        throw v.reason;
      default:
        switch (typeof v.status == "string" ? v.then(jt, jt) : (v.status = "pending", v.then(
          function(N) {
            v.status === "pending" && (v.status = "fulfilled", v.value = N);
          },
          function(N) {
            v.status === "pending" && (v.status = "rejected", v.reason = N);
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
  function A(v, N, R, Y, I) {
    var at = typeof v;
    (at === "undefined" || at === "boolean") && (v = null);
    var ft = !1;
    if (v === null) ft = !0;
    else
      switch (at) {
        case "bigint":
        case "string":
        case "number":
          ft = !0;
          break;
        case "object":
          switch (v.$$typeof) {
            case c:
            case o:
              ft = !0;
              break;
            case j:
              return ft = v._init, A(
                ft(v._payload),
                N,
                R,
                Y,
                I
              );
          }
      }
    if (ft)
      return I = I(v), ft = Y === "" ? "." + zl(v, 0) : Y, gt(I) ? (R = "", ft != null && (R = ft.replace(wl, "$&/") + "/"), A(I, N, R, "", function(me) {
        return me;
      })) : I != null && (Pt(I) && (I = Jl(
        I,
        R + (I.key == null || v && v.key === I.key ? "" : ("" + I.key).replace(
          wl,
          "$&/"
        ) + "/") + ft
      )), N.push(I)), 1;
    ft = 0;
    var wt = Y === "" ? "." : Y + ":";
    if (gt(v))
      for (var qt = 0; qt < v.length; qt++)
        Y = v[qt], at = wt + zl(Y, qt), ft += A(
          Y,
          N,
          R,
          at,
          I
        );
    else if (qt = X(v), typeof qt == "function")
      for (v = qt.call(v), qt = 0; !(Y = v.next()).done; )
        Y = Y.value, at = wt + zl(Y, qt++), ft += A(
          Y,
          N,
          R,
          at,
          I
        );
    else if (at === "object") {
      if (typeof v.then == "function")
        return A(
          Lt(v),
          N,
          R,
          Y,
          I
        );
      throw N = String(v), Error(
        "Objects are not valid as a React child (found: " + (N === "[object Object]" ? "object with keys {" + Object.keys(v).join(", ") + "}" : N) + "). If you meant to render a collection of children, use an array instead."
      );
    }
    return ft;
  }
  function H(v, N, R) {
    if (v == null) return v;
    var Y = [], I = 0;
    return A(v, Y, "", "", function(at) {
      return N.call(R, at, I++);
    }), Y;
  }
  function Z(v) {
    if (v._status === -1) {
      var N = v._result;
      N = N(), N.then(
        function(R) {
          (v._status === 0 || v._status === -1) && (v._status = 1, v._result = R);
        },
        function(R) {
          (v._status === 0 || v._status === -1) && (v._status = 2, v._result = R);
        }
      ), v._status === -1 && (v._status = 0, v._result = N);
    }
    if (v._status === 1) return v._result.default;
    throw v._result;
  }
  var $ = typeof reportError == "function" ? reportError : function(v) {
    if (typeof window == "object" && typeof window.ErrorEvent == "function") {
      var N = new window.ErrorEvent("error", {
        bubbles: !0,
        cancelable: !0,
        message: typeof v == "object" && v !== null && typeof v.message == "string" ? String(v.message) : String(v),
        error: v
      });
      if (!window.dispatchEvent(N)) return;
    } else if (typeof process == "object" && typeof process.emit == "function") {
      process.emit("uncaughtException", v);
      return;
    }
    console.error(v);
  }, bt = {
    map: H,
    forEach: function(v, N, R) {
      H(
        v,
        function() {
          N.apply(this, arguments);
        },
        R
      );
    },
    count: function(v) {
      var N = 0;
      return H(v, function() {
        N++;
      }), N;
    },
    toArray: function(v) {
      return H(v, function(N) {
        return N;
      }) || [];
    },
    only: function(v) {
      if (!Pt(v))
        throw Error(
          "React.Children.only expected to receive a single React element child."
        );
      return v;
    }
  };
  return P.Activity = p, P.Children = bt, P.Component = W, P.Fragment = r, P.Profiler = q, P.PureComponent = w, P.StrictMode = s, P.Suspense = O, P.__CLIENT_INTERNALS_DO_NOT_USE_OR_WARN_USERS_THEY_CANNOT_UPGRADE = L, P.__COMPILER_RUNTIME = {
    __proto__: null,
    c: function(v) {
      return L.H.useMemoCache(v);
    }
  }, P.cache = function(v) {
    return function() {
      return v.apply(null, arguments);
    };
  }, P.cacheSignal = function() {
    return null;
  }, P.cloneElement = function(v, N, R) {
    if (v == null)
      throw Error(
        "The argument must be a React element, but you passed " + v + "."
      );
    var Y = F({}, v.props), I = v.key;
    if (N != null)
      for (at in N.key !== void 0 && (I = "" + N.key), N)
        !Xt.call(N, at) || at === "key" || at === "__self" || at === "__source" || at === "ref" && N.ref === void 0 || (Y[at] = N[at]);
    var at = arguments.length - 2;
    if (at === 1) Y.children = R;
    else if (1 < at) {
      for (var ft = Array(at), wt = 0; wt < at; wt++)
        ft[wt] = arguments[wt + 2];
      Y.children = ft;
    }
    return el(v.type, I, Y);
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
  }, P.createElement = function(v, N, R) {
    var Y, I = {}, at = null;
    if (N != null)
      for (Y in N.key !== void 0 && (at = "" + N.key), N)
        Xt.call(N, Y) && Y !== "key" && Y !== "__self" && Y !== "__source" && (I[Y] = N[Y]);
    var ft = arguments.length - 2;
    if (ft === 1) I.children = R;
    else if (1 < ft) {
      for (var wt = Array(ft), qt = 0; qt < ft; qt++)
        wt[qt] = arguments[qt + 2];
      I.children = wt;
    }
    if (v && v.defaultProps)
      for (Y in ft = v.defaultProps, ft)
        I[Y] === void 0 && (I[Y] = ft[Y]);
    return el(v, at, I);
  }, P.createRef = function() {
    return { current: null };
  }, P.forwardRef = function(v) {
    return { $$typeof: U, render: v };
  }, P.isValidElement = Pt, P.lazy = function(v) {
    return {
      $$typeof: j,
      _payload: { _status: -1, _result: v },
      _init: Z
    };
  }, P.memo = function(v, N) {
    return {
      $$typeof: b,
      type: v,
      compare: N === void 0 ? null : N
    };
  }, P.startTransition = function(v) {
    var N = L.T, R = {};
    L.T = R;
    try {
      var Y = v(), I = L.S;
      I !== null && I(R, Y), typeof Y == "object" && Y !== null && typeof Y.then == "function" && Y.then(jt, $);
    } catch (at) {
      $(at);
    } finally {
      N !== null && R.types !== null && (N.types = R.types), L.T = N;
    }
  }, P.unstable_useCacheRefresh = function() {
    return L.H.useCacheRefresh();
  }, P.use = function(v) {
    return L.H.use(v);
  }, P.useActionState = function(v, N, R) {
    return L.H.useActionState(v, N, R);
  }, P.useCallback = function(v, N) {
    return L.H.useCallback(v, N);
  }, P.useContext = function(v) {
    return L.H.useContext(v);
  }, P.useDebugValue = function() {
  }, P.useDeferredValue = function(v, N) {
    return L.H.useDeferredValue(v, N);
  }, P.useEffect = function(v, N) {
    return L.H.useEffect(v, N);
  }, P.useEffectEvent = function(v) {
    return L.H.useEffectEvent(v);
  }, P.useId = function() {
    return L.H.useId();
  }, P.useImperativeHandle = function(v, N, R) {
    return L.H.useImperativeHandle(v, N, R);
  }, P.useInsertionEffect = function(v, N) {
    return L.H.useInsertionEffect(v, N);
  }, P.useLayoutEffect = function(v, N) {
    return L.H.useLayoutEffect(v, N);
  }, P.useMemo = function(v, N) {
    return L.H.useMemo(v, N);
  }, P.useOptimistic = function(v, N) {
    return L.H.useOptimistic(v, N);
  }, P.useReducer = function(v, N, R) {
    return L.H.useReducer(v, N, R);
  }, P.useRef = function(v) {
    return L.H.useRef(v);
  }, P.useState = function(v) {
    return L.H.useState(v);
  }, P.useSyncExternalStore = function(v, N, R) {
    return L.H.useSyncExternalStore(
      v,
      N,
      R
    );
  }, P.useTransition = function() {
    return L.H.useTransition();
  }, P.version = "19.2.5", P;
}
var Wd;
function Jf() {
  return Wd || (Wd = 1, jf.exports = Hh()), jf.exports;
}
var ut = Jf(), Rf = { exports: {} }, Wa = {}, Hf = { exports: {} }, Bf = {};
/**
 * @license React
 * scheduler.production.js
 *
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 */
var Fd;
function Bh() {
  return Fd || (Fd = 1, (function(c) {
    function o(A, H) {
      var Z = A.length;
      A.push(H);
      t: for (; 0 < Z; ) {
        var $ = Z - 1 >>> 1, bt = A[$];
        if (0 < q(bt, H))
          A[$] = H, A[Z] = bt, Z = $;
        else break t;
      }
    }
    function r(A) {
      return A.length === 0 ? null : A[0];
    }
    function s(A) {
      if (A.length === 0) return null;
      var H = A[0], Z = A.pop();
      if (Z !== H) {
        A[0] = Z;
        t: for (var $ = 0, bt = A.length, v = bt >>> 1; $ < v; ) {
          var N = 2 * ($ + 1) - 1, R = A[N], Y = N + 1, I = A[Y];
          if (0 > q(R, Z))
            Y < bt && 0 > q(I, R) ? (A[$] = I, A[Y] = Z, $ = Y) : (A[$] = R, A[N] = Z, $ = N);
          else if (Y < bt && 0 > q(I, Z))
            A[$] = I, A[Y] = Z, $ = Y;
          else break t;
        }
      }
      return H;
    }
    function q(A, H) {
      var Z = A.sortIndex - H.sortIndex;
      return Z !== 0 ? Z : A.id - H.id;
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
    var O = [], b = [], j = 1, p = null, D = 3, X = !1, k = !1, F = !1, B = !1, W = typeof setTimeout == "function" ? setTimeout : null, nt = typeof clearTimeout == "function" ? clearTimeout : null, w = typeof setImmediate < "u" ? setImmediate : null;
    function dt(A) {
      for (var H = r(b); H !== null; ) {
        if (H.callback === null) s(b);
        else if (H.startTime <= A)
          s(b), H.sortIndex = H.expirationTime, o(O, H);
        else break;
        H = r(b);
      }
    }
    function gt(A) {
      if (F = !1, dt(A), !k)
        if (r(O) !== null)
          k = !0, jt || (jt = !0, Jt());
        else {
          var H = r(b);
          H !== null && Lt(gt, H.startTime - A);
        }
    }
    var jt = !1, L = -1, Xt = 5, el = -1;
    function Jl() {
      return B ? !0 : !(c.unstable_now() - el < Xt);
    }
    function Pt() {
      if (B = !1, jt) {
        var A = c.unstable_now();
        el = A;
        var H = !0;
        try {
          t: {
            k = !1, F && (F = !1, nt(L), L = -1), X = !0;
            var Z = D;
            try {
              l: {
                for (dt(A), p = r(O); p !== null && !(p.expirationTime > A && Jl()); ) {
                  var $ = p.callback;
                  if (typeof $ == "function") {
                    p.callback = null, D = p.priorityLevel;
                    var bt = $(
                      p.expirationTime <= A
                    );
                    if (A = c.unstable_now(), typeof bt == "function") {
                      p.callback = bt, dt(A), H = !0;
                      break l;
                    }
                    p === r(O) && s(O), dt(A);
                  } else s(O);
                  p = r(O);
                }
                if (p !== null) H = !0;
                else {
                  var v = r(b);
                  v !== null && Lt(
                    gt,
                    v.startTime - A
                  ), H = !1;
                }
              }
              break t;
            } finally {
              p = null, D = Z, X = !1;
            }
            H = void 0;
          }
        } finally {
          H ? Jt() : jt = !1;
        }
      }
    }
    var Jt;
    if (typeof w == "function")
      Jt = function() {
        w(Pt);
      };
    else if (typeof MessageChannel < "u") {
      var wl = new MessageChannel(), zl = wl.port2;
      wl.port1.onmessage = Pt, Jt = function() {
        zl.postMessage(null);
      };
    } else
      Jt = function() {
        W(Pt, 0);
      };
    function Lt(A, H) {
      L = W(function() {
        A(c.unstable_now());
      }, H);
    }
    c.unstable_IdlePriority = 5, c.unstable_ImmediatePriority = 1, c.unstable_LowPriority = 4, c.unstable_NormalPriority = 3, c.unstable_Profiling = null, c.unstable_UserBlockingPriority = 2, c.unstable_cancelCallback = function(A) {
      A.callback = null;
    }, c.unstable_forceFrameRate = function(A) {
      0 > A || 125 < A ? console.error(
        "forceFrameRate takes a positive int between 0 and 125, forcing frame rates higher than 125 fps is not supported"
      ) : Xt = 0 < A ? Math.floor(1e3 / A) : 5;
    }, c.unstable_getCurrentPriorityLevel = function() {
      return D;
    }, c.unstable_next = function(A) {
      switch (D) {
        case 1:
        case 2:
        case 3:
          var H = 3;
          break;
        default:
          H = D;
      }
      var Z = D;
      D = H;
      try {
        return A();
      } finally {
        D = Z;
      }
    }, c.unstable_requestPaint = function() {
      B = !0;
    }, c.unstable_runWithPriority = function(A, H) {
      switch (A) {
        case 1:
        case 2:
        case 3:
        case 4:
        case 5:
          break;
        default:
          A = 3;
      }
      var Z = D;
      D = A;
      try {
        return H();
      } finally {
        D = Z;
      }
    }, c.unstable_scheduleCallback = function(A, H, Z) {
      var $ = c.unstable_now();
      switch (typeof Z == "object" && Z !== null ? (Z = Z.delay, Z = typeof Z == "number" && 0 < Z ? $ + Z : $) : Z = $, A) {
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
      return bt = Z + bt, A = {
        id: j++,
        callback: H,
        priorityLevel: A,
        startTime: Z,
        expirationTime: bt,
        sortIndex: -1
      }, Z > $ ? (A.sortIndex = Z, o(b, A), r(O) === null && A === r(b) && (F ? (nt(L), L = -1) : F = !0, Lt(gt, Z - $))) : (A.sortIndex = bt, o(O, A), k || X || (k = !0, jt || (jt = !0, Jt()))), A;
    }, c.unstable_shouldYield = Jl, c.unstable_wrapCallback = function(A) {
      var H = D;
      return function() {
        var Z = D;
        D = H;
        try {
          return A.apply(this, arguments);
        } finally {
          D = Z;
        }
      };
    };
  })(Bf)), Bf;
}
var Id;
function Yh() {
  return Id || (Id = 1, Hf.exports = Bh()), Hf.exports;
}
var Yf = { exports: {} }, ll = {};
/**
 * @license React
 * react-dom.production.js
 *
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 */
var Pd;
function Lh() {
  if (Pd) return ll;
  Pd = 1;
  var c = Jf();
  function o(O) {
    var b = "https://react.dev/errors/" + O;
    if (1 < arguments.length) {
      b += "?args[]=" + encodeURIComponent(arguments[1]);
      for (var j = 2; j < arguments.length; j++)
        b += "&args[]=" + encodeURIComponent(arguments[j]);
    }
    return "Minified React error #" + O + "; visit " + b + " for the full message or use the non-minified dev environment for full errors and additional helpful warnings.";
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
  }, q = Symbol.for("react.portal");
  function E(O, b, j) {
    var p = 3 < arguments.length && arguments[3] !== void 0 ? arguments[3] : null;
    return {
      $$typeof: q,
      key: p == null ? null : "" + p,
      children: O,
      containerInfo: b,
      implementation: j
    };
  }
  var C = c.__CLIENT_INTERNALS_DO_NOT_USE_OR_WARN_USERS_THEY_CANNOT_UPGRADE;
  function U(O, b) {
    if (O === "font") return "";
    if (typeof b == "string")
      return b === "use-credentials" ? b : "";
  }
  return ll.__DOM_INTERNALS_DO_NOT_USE_OR_WARN_USERS_THEY_CANNOT_UPGRADE = s, ll.createPortal = function(O, b) {
    var j = 2 < arguments.length && arguments[2] !== void 0 ? arguments[2] : null;
    if (!b || b.nodeType !== 1 && b.nodeType !== 9 && b.nodeType !== 11)
      throw Error(o(299));
    return E(O, b, null, j);
  }, ll.flushSync = function(O) {
    var b = C.T, j = s.p;
    try {
      if (C.T = null, s.p = 2, O) return O();
    } finally {
      C.T = b, s.p = j, s.d.f();
    }
  }, ll.preconnect = function(O, b) {
    typeof O == "string" && (b ? (b = b.crossOrigin, b = typeof b == "string" ? b === "use-credentials" ? b : "" : void 0) : b = null, s.d.C(O, b));
  }, ll.prefetchDNS = function(O) {
    typeof O == "string" && s.d.D(O);
  }, ll.preinit = function(O, b) {
    if (typeof O == "string" && b && typeof b.as == "string") {
      var j = b.as, p = U(j, b.crossOrigin), D = typeof b.integrity == "string" ? b.integrity : void 0, X = typeof b.fetchPriority == "string" ? b.fetchPriority : void 0;
      j === "style" ? s.d.S(
        O,
        typeof b.precedence == "string" ? b.precedence : void 0,
        {
          crossOrigin: p,
          integrity: D,
          fetchPriority: X
        }
      ) : j === "script" && s.d.X(O, {
        crossOrigin: p,
        integrity: D,
        fetchPriority: X,
        nonce: typeof b.nonce == "string" ? b.nonce : void 0
      });
    }
  }, ll.preinitModule = function(O, b) {
    if (typeof O == "string")
      if (typeof b == "object" && b !== null) {
        if (b.as == null || b.as === "script") {
          var j = U(
            b.as,
            b.crossOrigin
          );
          s.d.M(O, {
            crossOrigin: j,
            integrity: typeof b.integrity == "string" ? b.integrity : void 0,
            nonce: typeof b.nonce == "string" ? b.nonce : void 0
          });
        }
      } else b == null && s.d.M(O);
  }, ll.preload = function(O, b) {
    if (typeof O == "string" && typeof b == "object" && b !== null && typeof b.as == "string") {
      var j = b.as, p = U(j, b.crossOrigin);
      s.d.L(O, j, {
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
  }, ll.preloadModule = function(O, b) {
    if (typeof O == "string")
      if (b) {
        var j = U(b.as, b.crossOrigin);
        s.d.m(O, {
          as: typeof b.as == "string" && b.as !== "script" ? b.as : void 0,
          crossOrigin: j,
          integrity: typeof b.integrity == "string" ? b.integrity : void 0
        });
      } else s.d.m(O);
  }, ll.requestFormReset = function(O) {
    s.d.r(O);
  }, ll.unstable_batchedUpdates = function(O, b) {
    return O(b);
  }, ll.useFormState = function(O, b, j) {
    return C.H.useFormState(O, b, j);
  }, ll.useFormStatus = function() {
    return C.H.useHostTransitionStatus();
  }, ll.version = "19.2.5", ll;
}
var ty;
function Gh() {
  if (ty) return Yf.exports;
  ty = 1;
  function c() {
    if (!(typeof __REACT_DEVTOOLS_GLOBAL_HOOK__ > "u" || typeof __REACT_DEVTOOLS_GLOBAL_HOOK__.checkDCE != "function"))
      try {
        __REACT_DEVTOOLS_GLOBAL_HOOK__.checkDCE(c);
      } catch (o) {
        console.error(o);
      }
  }
  return c(), Yf.exports = Lh(), Yf.exports;
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
function Qh() {
  if (ly) return Wa;
  ly = 1;
  var c = Yh(), o = Jf(), r = Gh();
  function s(t) {
    var l = "https://react.dev/errors/" + t;
    if (1 < arguments.length) {
      l += "?args[]=" + encodeURIComponent(arguments[1]);
      for (var e = 2; e < arguments.length; e++)
        l += "&args[]=" + encodeURIComponent(arguments[e]);
    }
    return "Minified React error #" + t + "; visit " + l + " for the full message or use the non-minified dev environment for full errors and additional helpful warnings.";
  }
  function q(t) {
    return !(!t || t.nodeType !== 1 && t.nodeType !== 9 && t.nodeType !== 11);
  }
  function E(t) {
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
  function C(t) {
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
    if (E(t) !== t)
      throw Error(s(188));
  }
  function b(t) {
    var l = t.alternate;
    if (!l) {
      if (l = E(t), l === null) throw Error(s(188));
      return l !== t ? null : t;
    }
    for (var e = t, u = l; ; ) {
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
          if (n === e) return O(a), t;
          if (n === u) return O(a), l;
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
    return e.stateNode.current === e ? t : l;
  }
  function j(t) {
    var l = t.tag;
    if (l === 5 || l === 26 || l === 27 || l === 6) return t;
    for (t = t.child; t !== null; ) {
      if (l = j(t), l !== null) return l;
      t = t.sibling;
    }
    return null;
  }
  var p = Object.assign, D = Symbol.for("react.element"), X = Symbol.for("react.transitional.element"), k = Symbol.for("react.portal"), F = Symbol.for("react.fragment"), B = Symbol.for("react.strict_mode"), W = Symbol.for("react.profiler"), nt = Symbol.for("react.consumer"), w = Symbol.for("react.context"), dt = Symbol.for("react.forward_ref"), gt = Symbol.for("react.suspense"), jt = Symbol.for("react.suspense_list"), L = Symbol.for("react.memo"), Xt = Symbol.for("react.lazy"), el = Symbol.for("react.activity"), Jl = Symbol.for("react.memo_cache_sentinel"), Pt = Symbol.iterator;
  function Jt(t) {
    return t === null || typeof t != "object" ? null : (t = Pt && t[Pt] || t["@@iterator"], typeof t == "function" ? t : null);
  }
  var wl = Symbol.for("react.client.reference");
  function zl(t) {
    if (t == null) return null;
    if (typeof t == "function")
      return t.$$typeof === wl ? null : t.displayName || t.name || null;
    if (typeof t == "string") return t;
    switch (t) {
      case F:
        return "Fragment";
      case W:
        return "Profiler";
      case B:
        return "StrictMode";
      case gt:
        return "Suspense";
      case jt:
        return "SuspenseList";
      case el:
        return "Activity";
    }
    if (typeof t == "object")
      switch (t.$$typeof) {
        case k:
          return "Portal";
        case w:
          return t.displayName || "Context";
        case nt:
          return (t._context.displayName || "Context") + ".Consumer";
        case dt:
          var l = t.render;
          return t = t.displayName, t || (t = l.displayName || l.name || "", t = t !== "" ? "ForwardRef(" + t + ")" : "ForwardRef"), t;
        case L:
          return l = t.displayName || null, l !== null ? l : zl(t.type) || "Memo";
        case Xt:
          l = t._payload, t = t._init;
          try {
            return zl(t(l));
          } catch {
          }
      }
    return null;
  }
  var Lt = Array.isArray, A = o.__CLIENT_INTERNALS_DO_NOT_USE_OR_WARN_USERS_THEY_CANNOT_UPGRADE, H = r.__DOM_INTERNALS_DO_NOT_USE_OR_WARN_USERS_THEY_CANNOT_UPGRADE, Z = {
    pending: !1,
    data: null,
    method: null,
    action: null
  }, $ = [], bt = -1;
  function v(t) {
    return { current: t };
  }
  function N(t) {
    0 > bt || (t.current = $[bt], $[bt] = null, bt--);
  }
  function R(t, l) {
    bt++, $[bt] = t.current, t.current = l;
  }
  var Y = v(null), I = v(null), at = v(null), ft = v(null);
  function wt(t, l) {
    switch (R(at, l), R(I, t), R(Y, null), l.nodeType) {
      case 9:
      case 11:
        t = (t = l.documentElement) && (t = t.namespaceURI) ? gd(t) : 0;
        break;
      default:
        if (t = l.tagName, l = l.namespaceURI)
          l = gd(l), t = bd(l, t);
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
    N(Y), R(Y, t);
  }
  function qt() {
    N(Y), N(I), N(at);
  }
  function me(t) {
    t.memoizedState !== null && R(ft, t);
    var l = Y.current, e = bd(l, t.type);
    l !== e && (R(I, t), R(Y, e));
  }
  function Gl(t) {
    I.current === t && (N(Y), N(I)), ft.current === t && (N(ft), Ja._currentValue = Z);
  }
  var ua, Pa;
  function kl(t) {
    if (ua === void 0)
      try {
        throw Error();
      } catch (e) {
        var l = e.stack.trim().match(/\n( *(at )?)/);
        ua = l && l[1] || "", Pa = -1 < e.stack.indexOf(`
    at`) ? " (<anonymous>)" : -1 < e.stack.indexOf("@") ? "@unknown:0:0" : "";
      }
    return `
` + ua + t + Pa;
  }
  var K = !1;
  function ht(t, l) {
    if (!t || K) return "";
    K = !0;
    var e = Error.prepareStackTrace;
    Error.prepareStackTrace = void 0;
    try {
      var u = {
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
                } catch (_) {
                  var S = _;
                }
                Reflect.construct(t, [], M);
              } else {
                try {
                  M.call();
                } catch (_) {
                  S = _;
                }
                t.call(M.prototype);
              }
            } else {
              try {
                throw Error();
              } catch (_) {
                S = _;
              }
              (M = t()) && typeof M.catch == "function" && M.catch(function() {
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
                  return t.displayName && z.includes("<anonymous>") && (z = z.replace("<anonymous>", t.displayName)), z;
                }
              while (1 <= u && 0 <= a);
            break;
          }
      }
    } finally {
      K = !1, Error.prepareStackTrace = e;
    }
    return (e = t ? t.displayName || t.name : "") ? kl(e) : "";
  }
  function tl(t, l) {
    switch (t.tag) {
      case 26:
      case 27:
      case 5:
        return kl(t.type);
      case 16:
        return kl("Lazy");
      case 13:
        return t.child !== l && l !== null ? kl("Suspense Fallback") : kl("Suspense");
      case 19:
        return kl("SuspenseList");
      case 0:
      case 15:
        return ht(t.type, !1);
      case 11:
        return ht(t.type.render, !1);
      case 1:
        return ht(t.type, !0);
      case 31:
        return kl("Activity");
      default:
        return "";
    }
  }
  function Gt(t) {
    try {
      var l = "", e = null;
      do
        l += tl(t, e), e = t, t = t.return;
      while (t);
      return l;
    } catch (u) {
      return `
Error generating stack: ` + u.message + `
` + u.stack;
    }
  }
  var ge = Object.prototype.hasOwnProperty, yu = c.unstable_scheduleCallback, Si = c.unstable_cancelCallback, dy = c.unstable_shouldYield, yy = c.unstable_requestPaint, yl = c.unstable_now, vy = c.unstable_getCurrentPriorityLevel, kf = c.unstable_ImmediatePriority, $f = c.unstable_UserBlockingPriority, tn = c.unstable_NormalPriority, hy = c.unstable_LowPriority, Wf = c.unstable_IdlePriority, my = c.log, gy = c.unstable_setDisableYieldValue, aa = null, vl = null;
  function be(t) {
    if (typeof my == "function" && gy(t), vl && typeof vl.setStrictMode == "function")
      try {
        vl.setStrictMode(aa, t);
      } catch {
      }
  }
  var hl = Math.clz32 ? Math.clz32 : py, by = Math.log, Sy = Math.LN2;
  function py(t) {
    return t >>>= 0, t === 0 ? 32 : 31 - (by(t) / Sy | 0) | 0;
  }
  var ln = 256, en = 262144, un = 4194304;
  function Je(t) {
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
  function an(t, l, e) {
    var u = t.pendingLanes;
    if (u === 0) return 0;
    var a = 0, n = t.suspendedLanes, i = t.pingedLanes;
    t = t.warmLanes;
    var f = u & 134217727;
    return f !== 0 ? (u = f & ~n, u !== 0 ? a = Je(u) : (i &= f, i !== 0 ? a = Je(i) : e || (e = f & ~t, e !== 0 && (a = Je(e))))) : (f = u & ~n, f !== 0 ? a = Je(f) : i !== 0 ? a = Je(i) : e || (e = u & ~t, e !== 0 && (a = Je(e)))), a === 0 ? 0 : l !== 0 && l !== a && (l & n) === 0 && (n = a & -a, e = l & -l, n >= e || n === 32 && (e & 4194048) !== 0) ? l : a;
  }
  function na(t, l) {
    return (t.pendingLanes & ~(t.suspendedLanes & ~t.pingedLanes) & l) === 0;
  }
  function _y(t, l) {
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
  function Ff() {
    var t = un;
    return un <<= 1, (un & 62914560) === 0 && (un = 4194304), t;
  }
  function pi(t) {
    for (var l = [], e = 0; 31 > e; e++) l.push(t);
    return l;
  }
  function ia(t, l) {
    t.pendingLanes |= l, l !== 268435456 && (t.suspendedLanes = 0, t.pingedLanes = 0, t.warmLanes = 0);
  }
  function Ey(t, l, e, u, a, n) {
    var i = t.pendingLanes;
    t.pendingLanes = e, t.suspendedLanes = 0, t.pingedLanes = 0, t.warmLanes = 0, t.expiredLanes &= e, t.entangledLanes &= e, t.errorRecoveryDisabledLanes &= e, t.shellSuspendCounter = 0;
    var f = t.entanglements, d = t.expirationTimes, g = t.hiddenUpdates;
    for (e = i & ~e; 0 < e; ) {
      var z = 31 - hl(e), M = 1 << z;
      f[z] = 0, d[z] = -1;
      var S = g[z];
      if (S !== null)
        for (g[z] = null, z = 0; z < S.length; z++) {
          var _ = S[z];
          _ !== null && (_.lane &= -536870913);
        }
      e &= ~M;
    }
    u !== 0 && If(t, u, 0), n !== 0 && a === 0 && t.tag !== 0 && (t.suspendedLanes |= n & ~(i & ~l));
  }
  function If(t, l, e) {
    t.pendingLanes |= l, t.suspendedLanes &= ~l;
    var u = 31 - hl(l);
    t.entangledLanes |= l, t.entanglements[u] = t.entanglements[u] | 1073741824 | e & 261930;
  }
  function Pf(t, l) {
    var e = t.entangledLanes |= l;
    for (t = t.entanglements; e; ) {
      var u = 31 - hl(e), a = 1 << u;
      a & l | t[u] & l && (t[u] |= l), e &= ~a;
    }
  }
  function ts(t, l) {
    var e = l & -l;
    return e = (e & 42) !== 0 ? 1 : _i(e), (e & (t.suspendedLanes | l)) !== 0 ? 0 : e;
  }
  function _i(t) {
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
  function Ei(t) {
    return t &= -t, 2 < t ? 8 < t ? (t & 134217727) !== 0 ? 32 : 268435456 : 8 : 2;
  }
  function ls() {
    var t = H.p;
    return t !== 0 ? t : (t = window.event, t === void 0 ? 32 : Qd(t.type));
  }
  function es(t, l) {
    var e = H.p;
    try {
      return H.p = t, l();
    } finally {
      H.p = e;
    }
  }
  var Se = Math.random().toString(36).slice(2), kt = "__reactFiber$" + Se, nl = "__reactProps$" + Se, vu = "__reactContainer$" + Se, zi = "__reactEvents$" + Se, zy = "__reactListeners$" + Se, Ay = "__reactHandles$" + Se, us = "__reactResources$" + Se, ca = "__reactMarker$" + Se;
  function Ai(t) {
    delete t[kt], delete t[nl], delete t[zi], delete t[zy], delete t[Ay];
  }
  function hu(t) {
    var l = t[kt];
    if (l) return l;
    for (var e = t.parentNode; e; ) {
      if (l = e[vu] || e[kt]) {
        if (e = l.alternate, l.child !== null || e !== null && e.child !== null)
          for (t = qd(t); t !== null; ) {
            if (e = t[kt]) return e;
            t = qd(t);
          }
        return l;
      }
      t = e, e = t.parentNode;
    }
    return null;
  }
  function mu(t) {
    if (t = t[kt] || t[vu]) {
      var l = t.tag;
      if (l === 5 || l === 6 || l === 13 || l === 31 || l === 26 || l === 27 || l === 3)
        return t;
    }
    return null;
  }
  function fa(t) {
    var l = t.tag;
    if (l === 5 || l === 26 || l === 27 || l === 6) return t.stateNode;
    throw Error(s(33));
  }
  function gu(t) {
    var l = t[us];
    return l || (l = t[us] = { hoistableStyles: /* @__PURE__ */ new Map(), hoistableScripts: /* @__PURE__ */ new Map() }), l;
  }
  function Zt(t) {
    t[ca] = !0;
  }
  var as = /* @__PURE__ */ new Set(), ns = {};
  function we(t, l) {
    bu(t, l), bu(t + "Capture", l);
  }
  function bu(t, l) {
    for (ns[t] = l, t = 0; t < l.length; t++)
      as.add(l[t]);
  }
  var qy = RegExp(
    "^[:A-Z_a-z\\u00C0-\\u00D6\\u00D8-\\u00F6\\u00F8-\\u02FF\\u0370-\\u037D\\u037F-\\u1FFF\\u200C-\\u200D\\u2070-\\u218F\\u2C00-\\u2FEF\\u3001-\\uD7FF\\uF900-\\uFDCF\\uFDF0-\\uFFFD][:A-Z_a-z\\u00C0-\\u00D6\\u00D8-\\u00F6\\u00F8-\\u02FF\\u0370-\\u037D\\u037F-\\u1FFF\\u200C-\\u200D\\u2070-\\u218F\\u2C00-\\u2FEF\\u3001-\\uD7FF\\uF900-\\uFDCF\\uFDF0-\\uFFFD\\-.0-9\\u00B7\\u0300-\\u036F\\u203F-\\u2040]*$"
  ), is = {}, cs = {};
  function Ty(t) {
    return ge.call(cs, t) ? !0 : ge.call(is, t) ? !1 : qy.test(t) ? cs[t] = !0 : (is[t] = !0, !1);
  }
  function nn(t, l, e) {
    if (Ty(l))
      if (e === null) t.removeAttribute(l);
      else {
        switch (typeof e) {
          case "undefined":
          case "function":
          case "symbol":
            t.removeAttribute(l);
            return;
          case "boolean":
            var u = l.toLowerCase().slice(0, 5);
            if (u !== "data-" && u !== "aria-") {
              t.removeAttribute(l);
              return;
            }
        }
        t.setAttribute(l, "" + e);
      }
  }
  function cn(t, l, e) {
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
  function $l(t, l, e, u) {
    if (u === null) t.removeAttribute(e);
    else {
      switch (typeof u) {
        case "undefined":
        case "function":
        case "symbol":
        case "boolean":
          t.removeAttribute(e);
          return;
      }
      t.setAttributeNS(l, e, "" + u);
    }
  }
  function Al(t) {
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
  function fs(t) {
    var l = t.type;
    return (t = t.nodeName) && t.toLowerCase() === "input" && (l === "checkbox" || l === "radio");
  }
  function xy(t, l, e) {
    var u = Object.getOwnPropertyDescriptor(
      t.constructor.prototype,
      l
    );
    if (!t.hasOwnProperty(l) && typeof u < "u" && typeof u.get == "function" && typeof u.set == "function") {
      var a = u.get, n = u.set;
      return Object.defineProperty(t, l, {
        configurable: !0,
        get: function() {
          return a.call(this);
        },
        set: function(i) {
          e = "" + i, n.call(this, i);
        }
      }), Object.defineProperty(t, l, {
        enumerable: u.enumerable
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
  function qi(t) {
    if (!t._valueTracker) {
      var l = fs(t) ? "checked" : "value";
      t._valueTracker = xy(
        t,
        l,
        "" + t[l]
      );
    }
  }
  function ss(t) {
    if (!t) return !1;
    var l = t._valueTracker;
    if (!l) return !0;
    var e = l.getValue(), u = "";
    return t && (u = fs(t) ? t.checked ? "true" : "false" : t.value), t = u, t !== e ? (l.setValue(t), !0) : !1;
  }
  function fn(t) {
    if (t = t || (typeof document < "u" ? document : void 0), typeof t > "u") return null;
    try {
      return t.activeElement || t.body;
    } catch {
      return t.body;
    }
  }
  var Ny = /[\n"\\]/g;
  function ql(t) {
    return t.replace(
      Ny,
      function(l) {
        return "\\" + l.charCodeAt(0).toString(16) + " ";
      }
    );
  }
  function Ti(t, l, e, u, a, n, i, f) {
    t.name = "", i != null && typeof i != "function" && typeof i != "symbol" && typeof i != "boolean" ? t.type = i : t.removeAttribute("type"), l != null ? i === "number" ? (l === 0 && t.value === "" || t.value != l) && (t.value = "" + Al(l)) : t.value !== "" + Al(l) && (t.value = "" + Al(l)) : i !== "submit" && i !== "reset" || t.removeAttribute("value"), l != null ? xi(t, i, Al(l)) : e != null ? xi(t, i, Al(e)) : u != null && t.removeAttribute("value"), a == null && n != null && (t.defaultChecked = !!n), a != null && (t.checked = a && typeof a != "function" && typeof a != "symbol"), f != null && typeof f != "function" && typeof f != "symbol" && typeof f != "boolean" ? t.name = "" + Al(f) : t.removeAttribute("name");
  }
  function rs(t, l, e, u, a, n, i, f) {
    if (n != null && typeof n != "function" && typeof n != "symbol" && typeof n != "boolean" && (t.type = n), l != null || e != null) {
      if (!(n !== "submit" && n !== "reset" || l != null)) {
        qi(t);
        return;
      }
      e = e != null ? "" + Al(e) : "", l = l != null ? "" + Al(l) : e, f || l === t.value || (t.value = l), t.defaultValue = l;
    }
    u = u ?? a, u = typeof u != "function" && typeof u != "symbol" && !!u, t.checked = f ? t.checked : !!u, t.defaultChecked = !!u, i != null && typeof i != "function" && typeof i != "symbol" && typeof i != "boolean" && (t.name = i), qi(t);
  }
  function xi(t, l, e) {
    l === "number" && fn(t.ownerDocument) === t || t.defaultValue === "" + e || (t.defaultValue = "" + e);
  }
  function Su(t, l, e, u) {
    if (t = t.options, l) {
      l = {};
      for (var a = 0; a < e.length; a++)
        l["$" + e[a]] = !0;
      for (e = 0; e < t.length; e++)
        a = l.hasOwnProperty("$" + t[e].value), t[e].selected !== a && (t[e].selected = a), a && u && (t[e].defaultSelected = !0);
    } else {
      for (e = "" + Al(e), l = null, a = 0; a < t.length; a++) {
        if (t[a].value === e) {
          t[a].selected = !0, u && (t[a].defaultSelected = !0);
          return;
        }
        l !== null || t[a].disabled || (l = t[a]);
      }
      l !== null && (l.selected = !0);
    }
  }
  function os(t, l, e) {
    if (l != null && (l = "" + Al(l), l !== t.value && (t.value = l), e == null)) {
      t.defaultValue !== l && (t.defaultValue = l);
      return;
    }
    t.defaultValue = e != null ? "" + Al(e) : "";
  }
  function ds(t, l, e, u) {
    if (l == null) {
      if (u != null) {
        if (e != null) throw Error(s(92));
        if (Lt(u)) {
          if (1 < u.length) throw Error(s(93));
          u = u[0];
        }
        e = u;
      }
      e == null && (e = ""), l = e;
    }
    e = Al(l), t.defaultValue = e, u = t.textContent, u === e && u !== "" && u !== null && (t.value = u), qi(t);
  }
  function pu(t, l) {
    if (l) {
      var e = t.firstChild;
      if (e && e === t.lastChild && e.nodeType === 3) {
        e.nodeValue = l;
        return;
      }
    }
    t.textContent = l;
  }
  var Oy = new Set(
    "animationIterationCount aspectRatio borderImageOutset borderImageSlice borderImageWidth boxFlex boxFlexGroup boxOrdinalGroup columnCount columns flex flexGrow flexPositive flexShrink flexNegative flexOrder gridArea gridRow gridRowEnd gridRowSpan gridRowStart gridColumn gridColumnEnd gridColumnSpan gridColumnStart fontWeight lineClamp lineHeight opacity order orphans scale tabSize widows zIndex zoom fillOpacity floodOpacity stopOpacity strokeDasharray strokeDashoffset strokeMiterlimit strokeOpacity strokeWidth MozAnimationIterationCount MozBoxFlex MozBoxFlexGroup MozLineClamp msAnimationIterationCount msFlex msZoom msFlexGrow msFlexNegative msFlexOrder msFlexPositive msFlexShrink msGridColumn msGridColumnSpan msGridRow msGridRowSpan WebkitAnimationIterationCount WebkitBoxFlex WebKitBoxFlexGroup WebkitBoxOrdinalGroup WebkitColumnCount WebkitColumns WebkitFlex WebkitFlexGrow WebkitFlexPositive WebkitFlexShrink WebkitLineClamp".split(
      " "
    )
  );
  function ys(t, l, e) {
    var u = l.indexOf("--") === 0;
    e == null || typeof e == "boolean" || e === "" ? u ? t.setProperty(l, "") : l === "float" ? t.cssFloat = "" : t[l] = "" : u ? t.setProperty(l, e) : typeof e != "number" || e === 0 || Oy.has(l) ? l === "float" ? t.cssFloat = e : t[l] = ("" + e).trim() : t[l] = e + "px";
  }
  function vs(t, l, e) {
    if (l != null && typeof l != "object")
      throw Error(s(62));
    if (t = t.style, e != null) {
      for (var u in e)
        !e.hasOwnProperty(u) || l != null && l.hasOwnProperty(u) || (u.indexOf("--") === 0 ? t.setProperty(u, "") : u === "float" ? t.cssFloat = "" : t[u] = "");
      for (var a in l)
        u = l[a], l.hasOwnProperty(a) && e[a] !== u && ys(t, a, u);
    } else
      for (var n in l)
        l.hasOwnProperty(n) && ys(t, n, l[n]);
  }
  function Ni(t) {
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
  var My = /* @__PURE__ */ new Map([
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
  ]), Dy = /^[\u0000-\u001F ]*j[\r\n\t]*a[\r\n\t]*v[\r\n\t]*a[\r\n\t]*s[\r\n\t]*c[\r\n\t]*r[\r\n\t]*i[\r\n\t]*p[\r\n\t]*t[\r\n\t]*:/i;
  function sn(t) {
    return Dy.test("" + t) ? "javascript:throw new Error('React has blocked a javascript: URL as a security precaution.')" : t;
  }
  function Wl() {
  }
  var Oi = null;
  function Mi(t) {
    return t = t.target || t.srcElement || window, t.correspondingUseElement && (t = t.correspondingUseElement), t.nodeType === 3 ? t.parentNode : t;
  }
  var _u = null, Eu = null;
  function hs(t) {
    var l = mu(t);
    if (l && (t = l.stateNode)) {
      var e = t[nl] || null;
      t: switch (t = l.stateNode, l.type) {
        case "input":
          if (Ti(
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
              var u = e[l];
              if (u !== t && u.form === t.form) {
                var a = u[nl] || null;
                if (!a) throw Error(s(90));
                Ti(
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
            for (l = 0; l < e.length; l++)
              u = e[l], u.form === t.form && ss(u);
          }
          break t;
        case "textarea":
          os(t, e.value, e.defaultValue);
          break t;
        case "select":
          l = e.value, l != null && Su(t, !!e.multiple, l, !1);
      }
    }
  }
  var Di = !1;
  function ms(t, l, e) {
    if (Di) return t(l, e);
    Di = !0;
    try {
      var u = t(l);
      return u;
    } finally {
      if (Di = !1, (_u !== null || Eu !== null) && (Wn(), _u && (l = _u, t = Eu, Eu = _u = null, hs(l), t)))
        for (l = 0; l < t.length; l++) hs(t[l]);
    }
  }
  function sa(t, l) {
    var e = t.stateNode;
    if (e === null) return null;
    var u = e[nl] || null;
    if (u === null) return null;
    e = u[l];
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
        (u = !u.disabled) || (t = t.type, u = !(t === "button" || t === "input" || t === "select" || t === "textarea")), t = !u;
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
  var Fl = !(typeof window > "u" || typeof window.document > "u" || typeof window.document.createElement > "u"), Ui = !1;
  if (Fl)
    try {
      var ra = {};
      Object.defineProperty(ra, "passive", {
        get: function() {
          Ui = !0;
        }
      }), window.addEventListener("test", ra, ra), window.removeEventListener("test", ra, ra);
    } catch {
      Ui = !1;
    }
  var pe = null, Ci = null, rn = null;
  function gs() {
    if (rn) return rn;
    var t, l = Ci, e = l.length, u, a = "value" in pe ? pe.value : pe.textContent, n = a.length;
    for (t = 0; t < e && l[t] === a[t]; t++) ;
    var i = e - t;
    for (u = 1; u <= i && l[e - u] === a[n - u]; u++) ;
    return rn = a.slice(t, 1 < u ? 1 - u : void 0);
  }
  function on(t) {
    var l = t.keyCode;
    return "charCode" in t ? (t = t.charCode, t === 0 && l === 13 && (t = 13)) : t = l, t === 10 && (t = 13), 32 <= t || t === 13 ? t : 0;
  }
  function dn() {
    return !0;
  }
  function bs() {
    return !1;
  }
  function il(t) {
    function l(e, u, a, n, i) {
      this._reactName = e, this._targetInst = a, this.type = u, this.nativeEvent = n, this.target = i, this.currentTarget = null;
      for (var f in t)
        t.hasOwnProperty(f) && (e = t[f], this[f] = e ? e(n) : n[f]);
      return this.isDefaultPrevented = (n.defaultPrevented != null ? n.defaultPrevented : n.returnValue === !1) ? dn : bs, this.isPropagationStopped = bs, this;
    }
    return p(l.prototype, {
      preventDefault: function() {
        this.defaultPrevented = !0;
        var e = this.nativeEvent;
        e && (e.preventDefault ? e.preventDefault() : typeof e.returnValue != "unknown" && (e.returnValue = !1), this.isDefaultPrevented = dn);
      },
      stopPropagation: function() {
        var e = this.nativeEvent;
        e && (e.stopPropagation ? e.stopPropagation() : typeof e.cancelBubble != "unknown" && (e.cancelBubble = !0), this.isPropagationStopped = dn);
      },
      persist: function() {
      },
      isPersistent: dn
    }), l;
  }
  var ke = {
    eventPhase: 0,
    bubbles: 0,
    cancelable: 0,
    timeStamp: function(t) {
      return t.timeStamp || Date.now();
    },
    defaultPrevented: 0,
    isTrusted: 0
  }, yn = il(ke), oa = p({}, ke, { view: 0, detail: 0 }), Uy = il(oa), ji, Ri, da, vn = p({}, oa, {
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
    getModifierState: Bi,
    button: 0,
    buttons: 0,
    relatedTarget: function(t) {
      return t.relatedTarget === void 0 ? t.fromElement === t.srcElement ? t.toElement : t.fromElement : t.relatedTarget;
    },
    movementX: function(t) {
      return "movementX" in t ? t.movementX : (t !== da && (da && t.type === "mousemove" ? (ji = t.screenX - da.screenX, Ri = t.screenY - da.screenY) : Ri = ji = 0, da = t), ji);
    },
    movementY: function(t) {
      return "movementY" in t ? t.movementY : Ri;
    }
  }), Ss = il(vn), Cy = p({}, vn, { dataTransfer: 0 }), jy = il(Cy), Ry = p({}, oa, { relatedTarget: 0 }), Hi = il(Ry), Hy = p({}, ke, {
    animationName: 0,
    elapsedTime: 0,
    pseudoElement: 0
  }), By = il(Hy), Yy = p({}, ke, {
    clipboardData: function(t) {
      return "clipboardData" in t ? t.clipboardData : window.clipboardData;
    }
  }), Ly = il(Yy), Gy = p({}, ke, { data: 0 }), ps = il(Gy), Qy = {
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
  }, Xy = {
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
  }, Zy = {
    Alt: "altKey",
    Control: "ctrlKey",
    Meta: "metaKey",
    Shift: "shiftKey"
  };
  function Vy(t) {
    var l = this.nativeEvent;
    return l.getModifierState ? l.getModifierState(t) : (t = Zy[t]) ? !!l[t] : !1;
  }
  function Bi() {
    return Vy;
  }
  var Ky = p({}, oa, {
    key: function(t) {
      if (t.key) {
        var l = Qy[t.key] || t.key;
        if (l !== "Unidentified") return l;
      }
      return t.type === "keypress" ? (t = on(t), t === 13 ? "Enter" : String.fromCharCode(t)) : t.type === "keydown" || t.type === "keyup" ? Xy[t.keyCode] || "Unidentified" : "";
    },
    code: 0,
    location: 0,
    ctrlKey: 0,
    shiftKey: 0,
    altKey: 0,
    metaKey: 0,
    repeat: 0,
    locale: 0,
    getModifierState: Bi,
    charCode: function(t) {
      return t.type === "keypress" ? on(t) : 0;
    },
    keyCode: function(t) {
      return t.type === "keydown" || t.type === "keyup" ? t.keyCode : 0;
    },
    which: function(t) {
      return t.type === "keypress" ? on(t) : t.type === "keydown" || t.type === "keyup" ? t.keyCode : 0;
    }
  }), Jy = il(Ky), wy = p({}, vn, {
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
  }), _s = il(wy), ky = p({}, oa, {
    touches: 0,
    targetTouches: 0,
    changedTouches: 0,
    altKey: 0,
    metaKey: 0,
    ctrlKey: 0,
    shiftKey: 0,
    getModifierState: Bi
  }), $y = il(ky), Wy = p({}, ke, {
    propertyName: 0,
    elapsedTime: 0,
    pseudoElement: 0
  }), Fy = il(Wy), Iy = p({}, vn, {
    deltaX: function(t) {
      return "deltaX" in t ? t.deltaX : "wheelDeltaX" in t ? -t.wheelDeltaX : 0;
    },
    deltaY: function(t) {
      return "deltaY" in t ? t.deltaY : "wheelDeltaY" in t ? -t.wheelDeltaY : "wheelDelta" in t ? -t.wheelDelta : 0;
    },
    deltaZ: 0,
    deltaMode: 0
  }), Py = il(Iy), tv = p({}, ke, {
    newState: 0,
    oldState: 0
  }), lv = il(tv), ev = [9, 13, 27, 32], Yi = Fl && "CompositionEvent" in window, ya = null;
  Fl && "documentMode" in document && (ya = document.documentMode);
  var uv = Fl && "TextEvent" in window && !ya, Es = Fl && (!Yi || ya && 8 < ya && 11 >= ya), zs = " ", As = !1;
  function qs(t, l) {
    switch (t) {
      case "keyup":
        return ev.indexOf(l.keyCode) !== -1;
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
  function Ts(t) {
    return t = t.detail, typeof t == "object" && "data" in t ? t.data : null;
  }
  var zu = !1;
  function av(t, l) {
    switch (t) {
      case "compositionend":
        return Ts(l);
      case "keypress":
        return l.which !== 32 ? null : (As = !0, zs);
      case "textInput":
        return t = l.data, t === zs && As ? null : t;
      default:
        return null;
    }
  }
  function nv(t, l) {
    if (zu)
      return t === "compositionend" || !Yi && qs(t, l) ? (t = gs(), rn = Ci = pe = null, zu = !1, t) : null;
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
        return Es && l.locale !== "ko" ? null : l.data;
      default:
        return null;
    }
  }
  var iv = {
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
  function xs(t) {
    var l = t && t.nodeName && t.nodeName.toLowerCase();
    return l === "input" ? !!iv[t.type] : l === "textarea";
  }
  function Ns(t, l, e, u) {
    _u ? Eu ? Eu.push(u) : Eu = [u] : _u = u, l = ui(l, "onChange"), 0 < l.length && (e = new yn(
      "onChange",
      "change",
      null,
      e,
      u
    ), t.push({ event: e, listeners: l }));
  }
  var va = null, ha = null;
  function cv(t) {
    od(t, 0);
  }
  function hn(t) {
    var l = fa(t);
    if (ss(l)) return t;
  }
  function Os(t, l) {
    if (t === "change") return l;
  }
  var Ms = !1;
  if (Fl) {
    var Li;
    if (Fl) {
      var Gi = "oninput" in document;
      if (!Gi) {
        var Ds = document.createElement("div");
        Ds.setAttribute("oninput", "return;"), Gi = typeof Ds.oninput == "function";
      }
      Li = Gi;
    } else Li = !1;
    Ms = Li && (!document.documentMode || 9 < document.documentMode);
  }
  function Us() {
    va && (va.detachEvent("onpropertychange", Cs), ha = va = null);
  }
  function Cs(t) {
    if (t.propertyName === "value" && hn(ha)) {
      var l = [];
      Ns(
        l,
        ha,
        t,
        Mi(t)
      ), ms(cv, l);
    }
  }
  function fv(t, l, e) {
    t === "focusin" ? (Us(), va = l, ha = e, va.attachEvent("onpropertychange", Cs)) : t === "focusout" && Us();
  }
  function sv(t) {
    if (t === "selectionchange" || t === "keyup" || t === "keydown")
      return hn(ha);
  }
  function rv(t, l) {
    if (t === "click") return hn(l);
  }
  function ov(t, l) {
    if (t === "input" || t === "change")
      return hn(l);
  }
  function dv(t, l) {
    return t === l && (t !== 0 || 1 / t === 1 / l) || t !== t && l !== l;
  }
  var ml = typeof Object.is == "function" ? Object.is : dv;
  function ma(t, l) {
    if (ml(t, l)) return !0;
    if (typeof t != "object" || t === null || typeof l != "object" || l === null)
      return !1;
    var e = Object.keys(t), u = Object.keys(l);
    if (e.length !== u.length) return !1;
    for (u = 0; u < e.length; u++) {
      var a = e[u];
      if (!ge.call(l, a) || !ml(t[a], l[a]))
        return !1;
    }
    return !0;
  }
  function js(t) {
    for (; t && t.firstChild; ) t = t.firstChild;
    return t;
  }
  function Rs(t, l) {
    var e = js(t);
    t = 0;
    for (var u; e; ) {
      if (e.nodeType === 3) {
        if (u = t + e.textContent.length, t <= l && u >= l)
          return { node: e, offset: l - t };
        t = u;
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
      e = js(e);
    }
  }
  function Hs(t, l) {
    return t && l ? t === l ? !0 : t && t.nodeType === 3 ? !1 : l && l.nodeType === 3 ? Hs(t, l.parentNode) : "contains" in t ? t.contains(l) : t.compareDocumentPosition ? !!(t.compareDocumentPosition(l) & 16) : !1 : !1;
  }
  function Bs(t) {
    t = t != null && t.ownerDocument != null && t.ownerDocument.defaultView != null ? t.ownerDocument.defaultView : window;
    for (var l = fn(t.document); l instanceof t.HTMLIFrameElement; ) {
      try {
        var e = typeof l.contentWindow.location.href == "string";
      } catch {
        e = !1;
      }
      if (e) t = l.contentWindow;
      else break;
      l = fn(t.document);
    }
    return l;
  }
  function Qi(t) {
    var l = t && t.nodeName && t.nodeName.toLowerCase();
    return l && (l === "input" && (t.type === "text" || t.type === "search" || t.type === "tel" || t.type === "url" || t.type === "password") || l === "textarea" || t.contentEditable === "true");
  }
  var yv = Fl && "documentMode" in document && 11 >= document.documentMode, Au = null, Xi = null, ga = null, Zi = !1;
  function Ys(t, l, e) {
    var u = e.window === e ? e.document : e.nodeType === 9 ? e : e.ownerDocument;
    Zi || Au == null || Au !== fn(u) || (u = Au, "selectionStart" in u && Qi(u) ? u = { start: u.selectionStart, end: u.selectionEnd } : (u = (u.ownerDocument && u.ownerDocument.defaultView || window).getSelection(), u = {
      anchorNode: u.anchorNode,
      anchorOffset: u.anchorOffset,
      focusNode: u.focusNode,
      focusOffset: u.focusOffset
    }), ga && ma(ga, u) || (ga = u, u = ui(Xi, "onSelect"), 0 < u.length && (l = new yn(
      "onSelect",
      "select",
      null,
      l,
      e
    ), t.push({ event: l, listeners: u }), l.target = Au)));
  }
  function $e(t, l) {
    var e = {};
    return e[t.toLowerCase()] = l.toLowerCase(), e["Webkit" + t] = "webkit" + l, e["Moz" + t] = "moz" + l, e;
  }
  var qu = {
    animationend: $e("Animation", "AnimationEnd"),
    animationiteration: $e("Animation", "AnimationIteration"),
    animationstart: $e("Animation", "AnimationStart"),
    transitionrun: $e("Transition", "TransitionRun"),
    transitionstart: $e("Transition", "TransitionStart"),
    transitioncancel: $e("Transition", "TransitionCancel"),
    transitionend: $e("Transition", "TransitionEnd")
  }, Vi = {}, Ls = {};
  Fl && (Ls = document.createElement("div").style, "AnimationEvent" in window || (delete qu.animationend.animation, delete qu.animationiteration.animation, delete qu.animationstart.animation), "TransitionEvent" in window || delete qu.transitionend.transition);
  function We(t) {
    if (Vi[t]) return Vi[t];
    if (!qu[t]) return t;
    var l = qu[t], e;
    for (e in l)
      if (l.hasOwnProperty(e) && e in Ls)
        return Vi[t] = l[e];
    return t;
  }
  var Gs = We("animationend"), Qs = We("animationiteration"), Xs = We("animationstart"), vv = We("transitionrun"), hv = We("transitionstart"), mv = We("transitioncancel"), Zs = We("transitionend"), Vs = /* @__PURE__ */ new Map(), Ki = "abort auxClick beforeToggle cancel canPlay canPlayThrough click close contextMenu copy cut drag dragEnd dragEnter dragExit dragLeave dragOver dragStart drop durationChange emptied encrypted ended error gotPointerCapture input invalid keyDown keyPress keyUp load loadedData loadedMetadata loadStart lostPointerCapture mouseDown mouseMove mouseOut mouseOver mouseUp paste pause play playing pointerCancel pointerDown pointerMove pointerOut pointerOver pointerUp progress rateChange reset resize seeked seeking stalled submit suspend timeUpdate touchCancel touchEnd touchStart volumeChange scroll toggle touchMove waiting wheel".split(
    " "
  );
  Ki.push("scrollEnd");
  function Rl(t, l) {
    Vs.set(t, l), we(l, [t]);
  }
  var mn = typeof reportError == "function" ? reportError : function(t) {
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
  }, Tl = [], Tu = 0, Ji = 0;
  function gn() {
    for (var t = Tu, l = Ji = Tu = 0; l < t; ) {
      var e = Tl[l];
      Tl[l++] = null;
      var u = Tl[l];
      Tl[l++] = null;
      var a = Tl[l];
      Tl[l++] = null;
      var n = Tl[l];
      if (Tl[l++] = null, u !== null && a !== null) {
        var i = u.pending;
        i === null ? a.next = a : (a.next = i.next, i.next = a), u.pending = a;
      }
      n !== 0 && Ks(e, a, n);
    }
  }
  function bn(t, l, e, u) {
    Tl[Tu++] = t, Tl[Tu++] = l, Tl[Tu++] = e, Tl[Tu++] = u, Ji |= u, t.lanes |= u, t = t.alternate, t !== null && (t.lanes |= u);
  }
  function wi(t, l, e, u) {
    return bn(t, l, e, u), Sn(t);
  }
  function Fe(t, l) {
    return bn(t, null, null, l), Sn(t);
  }
  function Ks(t, l, e) {
    t.lanes |= e;
    var u = t.alternate;
    u !== null && (u.lanes |= e);
    for (var a = !1, n = t.return; n !== null; )
      n.childLanes |= e, u = n.alternate, u !== null && (u.childLanes |= e), n.tag === 22 && (t = n.stateNode, t === null || t._visibility & 1 || (a = !0)), t = n, n = n.return;
    return t.tag === 3 ? (n = t.stateNode, a && l !== null && (a = 31 - hl(e), t = n.hiddenUpdates, u = t[a], u === null ? t[a] = [l] : u.push(l), l.lane = e | 536870912), n) : null;
  }
  function Sn(t) {
    if (50 < La)
      throw La = 0, ef = null, Error(s(185));
    for (var l = t.return; l !== null; )
      t = l, l = t.return;
    return t.tag === 3 ? t.stateNode : null;
  }
  var xu = {};
  function gv(t, l, e, u) {
    this.tag = t, this.key = e, this.sibling = this.child = this.return = this.stateNode = this.type = this.elementType = null, this.index = 0, this.refCleanup = this.ref = null, this.pendingProps = l, this.dependencies = this.memoizedState = this.updateQueue = this.memoizedProps = null, this.mode = u, this.subtreeFlags = this.flags = 0, this.deletions = null, this.childLanes = this.lanes = 0, this.alternate = null;
  }
  function gl(t, l, e, u) {
    return new gv(t, l, e, u);
  }
  function ki(t) {
    return t = t.prototype, !(!t || !t.isReactComponent);
  }
  function Il(t, l) {
    var e = t.alternate;
    return e === null ? (e = gl(
      t.tag,
      l,
      t.key,
      t.mode
    ), e.elementType = t.elementType, e.type = t.type, e.stateNode = t.stateNode, e.alternate = t, t.alternate = e) : (e.pendingProps = l, e.type = t.type, e.flags = 0, e.subtreeFlags = 0, e.deletions = null), e.flags = t.flags & 65011712, e.childLanes = t.childLanes, e.lanes = t.lanes, e.child = t.child, e.memoizedProps = t.memoizedProps, e.memoizedState = t.memoizedState, e.updateQueue = t.updateQueue, l = t.dependencies, e.dependencies = l === null ? null : { lanes: l.lanes, firstContext: l.firstContext }, e.sibling = t.sibling, e.index = t.index, e.ref = t.ref, e.refCleanup = t.refCleanup, e;
  }
  function Js(t, l) {
    t.flags &= 65011714;
    var e = t.alternate;
    return e === null ? (t.childLanes = 0, t.lanes = l, t.child = null, t.subtreeFlags = 0, t.memoizedProps = null, t.memoizedState = null, t.updateQueue = null, t.dependencies = null, t.stateNode = null) : (t.childLanes = e.childLanes, t.lanes = e.lanes, t.child = e.child, t.subtreeFlags = 0, t.deletions = null, t.memoizedProps = e.memoizedProps, t.memoizedState = e.memoizedState, t.updateQueue = e.updateQueue, t.type = e.type, l = e.dependencies, t.dependencies = l === null ? null : {
      lanes: l.lanes,
      firstContext: l.firstContext
    }), t;
  }
  function pn(t, l, e, u, a, n) {
    var i = 0;
    if (u = t, typeof t == "function") ki(t) && (i = 1);
    else if (typeof t == "string")
      i = Eh(
        t,
        e,
        Y.current
      ) ? 26 : t === "html" || t === "head" || t === "body" ? 27 : 5;
    else
      t: switch (t) {
        case el:
          return t = gl(31, e, l, a), t.elementType = el, t.lanes = n, t;
        case F:
          return Ie(e.children, a, n, l);
        case B:
          i = 8, a |= 24;
          break;
        case W:
          return t = gl(12, e, l, a | 2), t.elementType = W, t.lanes = n, t;
        case gt:
          return t = gl(13, e, l, a), t.elementType = gt, t.lanes = n, t;
        case jt:
          return t = gl(19, e, l, a), t.elementType = jt, t.lanes = n, t;
        default:
          if (typeof t == "object" && t !== null)
            switch (t.$$typeof) {
              case w:
                i = 10;
                break t;
              case nt:
                i = 9;
                break t;
              case dt:
                i = 11;
                break t;
              case L:
                i = 14;
                break t;
              case Xt:
                i = 16, u = null;
                break t;
            }
          i = 29, e = Error(
            s(130, t === null ? "null" : typeof t, "")
          ), u = null;
      }
    return l = gl(i, e, l, a), l.elementType = t, l.type = u, l.lanes = n, l;
  }
  function Ie(t, l, e, u) {
    return t = gl(7, t, u, l), t.lanes = e, t;
  }
  function $i(t, l, e) {
    return t = gl(6, t, null, l), t.lanes = e, t;
  }
  function ws(t) {
    var l = gl(18, null, null, 0);
    return l.stateNode = t, l;
  }
  function Wi(t, l, e) {
    return l = gl(
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
  var ks = /* @__PURE__ */ new WeakMap();
  function xl(t, l) {
    if (typeof t == "object" && t !== null) {
      var e = ks.get(t);
      return e !== void 0 ? e : (l = {
        value: t,
        source: l,
        stack: Gt(l)
      }, ks.set(t, l), l);
    }
    return {
      value: t,
      source: l,
      stack: Gt(l)
    };
  }
  var Nu = [], Ou = 0, _n = null, ba = 0, Nl = [], Ol = 0, _e = null, Ql = 1, Xl = "";
  function Pl(t, l) {
    Nu[Ou++] = ba, Nu[Ou++] = _n, _n = t, ba = l;
  }
  function $s(t, l, e) {
    Nl[Ol++] = Ql, Nl[Ol++] = Xl, Nl[Ol++] = _e, _e = t;
    var u = Ql;
    t = Xl;
    var a = 32 - hl(u) - 1;
    u &= ~(1 << a), e += 1;
    var n = 32 - hl(l) + a;
    if (30 < n) {
      var i = a - a % 5;
      n = (u & (1 << i) - 1).toString(32), u >>= i, a -= i, Ql = 1 << 32 - hl(l) + a | e << a | u, Xl = n + t;
    } else
      Ql = 1 << n | e << a | u, Xl = t;
  }
  function Fi(t) {
    t.return !== null && (Pl(t, 1), $s(t, 1, 0));
  }
  function Ii(t) {
    for (; t === _n; )
      _n = Nu[--Ou], Nu[Ou] = null, ba = Nu[--Ou], Nu[Ou] = null;
    for (; t === _e; )
      _e = Nl[--Ol], Nl[Ol] = null, Xl = Nl[--Ol], Nl[Ol] = null, Ql = Nl[--Ol], Nl[Ol] = null;
  }
  function Ws(t, l) {
    Nl[Ol++] = Ql, Nl[Ol++] = Xl, Nl[Ol++] = _e, Ql = l.id, Xl = l.overflow, _e = t;
  }
  var $t = null, Tt = null, ot = !1, Ee = null, Ml = !1, Pi = Error(s(519));
  function ze(t) {
    var l = Error(
      s(
        418,
        1 < arguments.length && arguments[1] !== void 0 && arguments[1] ? "text" : "HTML",
        ""
      )
    );
    throw Sa(xl(l, t)), Pi;
  }
  function Fs(t) {
    var l = t.stateNode, e = t.type, u = t.memoizedProps;
    switch (l[kt] = t, l[nl] = u, e) {
      case "dialog":
        ct("cancel", l), ct("close", l);
        break;
      case "iframe":
      case "object":
      case "embed":
        ct("load", l);
        break;
      case "video":
      case "audio":
        for (e = 0; e < Qa.length; e++)
          ct(Qa[e], l);
        break;
      case "source":
        ct("error", l);
        break;
      case "img":
      case "image":
      case "link":
        ct("error", l), ct("load", l);
        break;
      case "details":
        ct("toggle", l);
        break;
      case "input":
        ct("invalid", l), rs(
          l,
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
        ct("invalid", l);
        break;
      case "textarea":
        ct("invalid", l), ds(l, u.value, u.defaultValue, u.children);
    }
    e = u.children, typeof e != "string" && typeof e != "number" && typeof e != "bigint" || l.textContent === "" + e || u.suppressHydrationWarning === !0 || hd(l.textContent, e) ? (u.popover != null && (ct("beforetoggle", l), ct("toggle", l)), u.onScroll != null && ct("scroll", l), u.onScrollEnd != null && ct("scrollend", l), u.onClick != null && (l.onclick = Wl), l = !0) : l = !1, l || ze(t, !0);
  }
  function Is(t) {
    for ($t = t.return; $t; )
      switch ($t.tag) {
        case 5:
        case 31:
        case 13:
          Ml = !1;
          return;
        case 27:
        case 3:
          Ml = !0;
          return;
        default:
          $t = $t.return;
      }
  }
  function Mu(t) {
    if (t !== $t) return !1;
    if (!ot) return Is(t), ot = !0, !1;
    var l = t.tag, e;
    if ((e = l !== 3 && l !== 27) && ((e = l === 5) && (e = t.type, e = !(e !== "form" && e !== "button") || bf(t.type, t.memoizedProps)), e = !e), e && Tt && ze(t), Is(t), l === 13) {
      if (t = t.memoizedState, t = t !== null ? t.dehydrated : null, !t) throw Error(s(317));
      Tt = Ad(t);
    } else if (l === 31) {
      if (t = t.memoizedState, t = t !== null ? t.dehydrated : null, !t) throw Error(s(317));
      Tt = Ad(t);
    } else
      l === 27 ? (l = Tt, Be(t.type) ? (t = zf, zf = null, Tt = t) : Tt = l) : Tt = $t ? Ul(t.stateNode.nextSibling) : null;
    return !0;
  }
  function Pe() {
    Tt = $t = null, ot = !1;
  }
  function tc() {
    var t = Ee;
    return t !== null && (rl === null ? rl = t : rl.push.apply(
      rl,
      t
    ), Ee = null), t;
  }
  function Sa(t) {
    Ee === null ? Ee = [t] : Ee.push(t);
  }
  var lc = v(null), tu = null, te = null;
  function Ae(t, l, e) {
    R(lc, l._currentValue), l._currentValue = e;
  }
  function le(t) {
    t._currentValue = lc.current, N(lc);
  }
  function ec(t, l, e) {
    for (; t !== null; ) {
      var u = t.alternate;
      if ((t.childLanes & l) !== l ? (t.childLanes |= l, u !== null && (u.childLanes |= l)) : u !== null && (u.childLanes & l) !== l && (u.childLanes |= l), t === e) break;
      t = t.return;
    }
  }
  function uc(t, l, e, u) {
    var a = t.child;
    for (a !== null && (a.return = t); a !== null; ) {
      var n = a.dependencies;
      if (n !== null) {
        var i = a.child;
        n = n.firstContext;
        t: for (; n !== null; ) {
          var f = n;
          n = a;
          for (var d = 0; d < l.length; d++)
            if (f.context === l[d]) {
              n.lanes |= e, f = n.alternate, f !== null && (f.lanes |= e), ec(
                n.return,
                e,
                t
              ), u || (i = null);
              break t;
            }
          n = f.next;
        }
      } else if (a.tag === 18) {
        if (i = a.return, i === null) throw Error(s(341));
        i.lanes |= e, n = i.alternate, n !== null && (n.lanes |= e), ec(i, e, t), i = null;
      } else i = a.child;
      if (i !== null) i.return = a;
      else
        for (i = a; i !== null; ) {
          if (i === t) {
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
  function Du(t, l, e, u) {
    t = null;
    for (var a = l, n = !1; a !== null; ) {
      if (!n) {
        if ((a.flags & 524288) !== 0) n = !0;
        else if ((a.flags & 262144) !== 0) break;
      }
      if (a.tag === 10) {
        var i = a.alternate;
        if (i === null) throw Error(s(387));
        if (i = i.memoizedProps, i !== null) {
          var f = a.type;
          ml(a.pendingProps.value, i.value) || (t !== null ? t.push(f) : t = [f]);
        }
      } else if (a === ft.current) {
        if (i = a.alternate, i === null) throw Error(s(387));
        i.memoizedState.memoizedState !== a.memoizedState.memoizedState && (t !== null ? t.push(Ja) : t = [Ja]);
      }
      a = a.return;
    }
    t !== null && uc(
      l,
      t,
      e,
      u
    ), l.flags |= 262144;
  }
  function En(t) {
    for (t = t.firstContext; t !== null; ) {
      if (!ml(
        t.context._currentValue,
        t.memoizedValue
      ))
        return !0;
      t = t.next;
    }
    return !1;
  }
  function lu(t) {
    tu = t, te = null, t = t.dependencies, t !== null && (t.firstContext = null);
  }
  function Wt(t) {
    return Ps(tu, t);
  }
  function zn(t, l) {
    return tu === null && lu(t), Ps(t, l);
  }
  function Ps(t, l) {
    var e = l._currentValue;
    if (l = { context: l, memoizedValue: e, next: null }, te === null) {
      if (t === null) throw Error(s(308));
      te = l, t.dependencies = { lanes: 0, firstContext: l }, t.flags |= 524288;
    } else te = te.next = l;
    return e;
  }
  var bv = typeof AbortController < "u" ? AbortController : function() {
    var t = [], l = this.signal = {
      aborted: !1,
      addEventListener: function(e, u) {
        t.push(u);
      }
    };
    this.abort = function() {
      l.aborted = !0, t.forEach(function(e) {
        return e();
      });
    };
  }, Sv = c.unstable_scheduleCallback, pv = c.unstable_NormalPriority, Rt = {
    $$typeof: w,
    Consumer: null,
    Provider: null,
    _currentValue: null,
    _currentValue2: null,
    _threadCount: 0
  };
  function ac() {
    return {
      controller: new bv(),
      data: /* @__PURE__ */ new Map(),
      refCount: 0
    };
  }
  function pa(t) {
    t.refCount--, t.refCount === 0 && Sv(pv, function() {
      t.controller.abort();
    });
  }
  var _a = null, nc = 0, Uu = 0, Cu = null;
  function _v(t, l) {
    if (_a === null) {
      var e = _a = [];
      nc = 0, Uu = sf(), Cu = {
        status: "pending",
        value: void 0,
        then: function(u) {
          e.push(u);
        }
      };
    }
    return nc++, l.then(tr, tr), l;
  }
  function tr() {
    if (--nc === 0 && _a !== null) {
      Cu !== null && (Cu.status = "fulfilled");
      var t = _a;
      _a = null, Uu = 0, Cu = null;
      for (var l = 0; l < t.length; l++) (0, t[l])();
    }
  }
  function Ev(t, l) {
    var e = [], u = {
      status: "pending",
      value: null,
      reason: null,
      then: function(a) {
        e.push(a);
      }
    };
    return t.then(
      function() {
        u.status = "fulfilled", u.value = l;
        for (var a = 0; a < e.length; a++) (0, e[a])(l);
      },
      function(a) {
        for (u.status = "rejected", u.reason = a, a = 0; a < e.length; a++)
          (0, e[a])(void 0);
      }
    ), u;
  }
  var lr = A.S;
  A.S = function(t, l) {
    Lo = yl(), typeof l == "object" && l !== null && typeof l.then == "function" && _v(t, l), lr !== null && lr(t, l);
  };
  var eu = v(null);
  function ic() {
    var t = eu.current;
    return t !== null ? t : At.pooledCache;
  }
  function An(t, l) {
    l === null ? R(eu, eu.current) : R(eu, l.pool);
  }
  function er() {
    var t = ic();
    return t === null ? null : { parent: Rt._currentValue, pool: t };
  }
  var ju = Error(s(460)), cc = Error(s(474)), qn = Error(s(542)), Tn = { then: function() {
  } };
  function ur(t) {
    return t = t.status, t === "fulfilled" || t === "rejected";
  }
  function ar(t, l, e) {
    switch (e = t[e], e === void 0 ? t.push(l) : e !== l && (l.then(Wl, Wl), l = e), l.status) {
      case "fulfilled":
        return l.value;
      case "rejected":
        throw t = l.reason, ir(t), t;
      default:
        if (typeof l.status == "string") l.then(Wl, Wl);
        else {
          if (t = At, t !== null && 100 < t.shellSuspendCounter)
            throw Error(s(482));
          t = l, t.status = "pending", t.then(
            function(u) {
              if (l.status === "pending") {
                var a = l;
                a.status = "fulfilled", a.value = u;
              }
            },
            function(u) {
              if (l.status === "pending") {
                var a = l;
                a.status = "rejected", a.reason = u;
              }
            }
          );
        }
        switch (l.status) {
          case "fulfilled":
            return l.value;
          case "rejected":
            throw t = l.reason, ir(t), t;
        }
        throw au = l, ju;
    }
  }
  function uu(t) {
    try {
      var l = t._init;
      return l(t._payload);
    } catch (e) {
      throw e !== null && typeof e == "object" && typeof e.then == "function" ? (au = e, ju) : e;
    }
  }
  var au = null;
  function nr() {
    if (au === null) throw Error(s(459));
    var t = au;
    return au = null, t;
  }
  function ir(t) {
    if (t === ju || t === qn)
      throw Error(s(483));
  }
  var Ru = null, Ea = 0;
  function xn(t) {
    var l = Ea;
    return Ea += 1, Ru === null && (Ru = []), ar(Ru, t, l);
  }
  function za(t, l) {
    l = l.props.ref, t.ref = l !== void 0 ? l : null;
  }
  function Nn(t, l) {
    throw l.$$typeof === D ? Error(s(525)) : (t = Object.prototype.toString.call(l), Error(
      s(
        31,
        t === "[object Object]" ? "object with keys {" + Object.keys(l).join(", ") + "}" : t
      )
    ));
  }
  function cr(t) {
    function l(h, y) {
      if (t) {
        var m = h.deletions;
        m === null ? (h.deletions = [y], h.flags |= 16) : m.push(y);
      }
    }
    function e(h, y) {
      if (!t) return null;
      for (; y !== null; )
        l(h, y), y = y.sibling;
      return null;
    }
    function u(h) {
      for (var y = /* @__PURE__ */ new Map(); h !== null; )
        h.key !== null ? y.set(h.key, h) : y.set(h.index, h), h = h.sibling;
      return y;
    }
    function a(h, y) {
      return h = Il(h, y), h.index = 0, h.sibling = null, h;
    }
    function n(h, y, m) {
      return h.index = m, t ? (m = h.alternate, m !== null ? (m = m.index, m < y ? (h.flags |= 67108866, y) : m) : (h.flags |= 67108866, y)) : (h.flags |= 1048576, y);
    }
    function i(h) {
      return t && h.alternate === null && (h.flags |= 67108866), h;
    }
    function f(h, y, m, T) {
      return y === null || y.tag !== 6 ? (y = $i(m, h.mode, T), y.return = h, y) : (y = a(y, m), y.return = h, y);
    }
    function d(h, y, m, T) {
      var V = m.type;
      return V === F ? z(
        h,
        y,
        m.props.children,
        T,
        m.key
      ) : y !== null && (y.elementType === V || typeof V == "object" && V !== null && V.$$typeof === Xt && uu(V) === y.type) ? (y = a(y, m.props), za(y, m), y.return = h, y) : (y = pn(
        m.type,
        m.key,
        m.props,
        null,
        h.mode,
        T
      ), za(y, m), y.return = h, y);
    }
    function g(h, y, m, T) {
      return y === null || y.tag !== 4 || y.stateNode.containerInfo !== m.containerInfo || y.stateNode.implementation !== m.implementation ? (y = Wi(m, h.mode, T), y.return = h, y) : (y = a(y, m.children || []), y.return = h, y);
    }
    function z(h, y, m, T, V) {
      return y === null || y.tag !== 7 ? (y = Ie(
        m,
        h.mode,
        T,
        V
      ), y.return = h, y) : (y = a(y, m), y.return = h, y);
    }
    function M(h, y, m) {
      if (typeof y == "string" && y !== "" || typeof y == "number" || typeof y == "bigint")
        return y = $i(
          "" + y,
          h.mode,
          m
        ), y.return = h, y;
      if (typeof y == "object" && y !== null) {
        switch (y.$$typeof) {
          case X:
            return m = pn(
              y.type,
              y.key,
              y.props,
              null,
              h.mode,
              m
            ), za(m, y), m.return = h, m;
          case k:
            return y = Wi(
              y,
              h.mode,
              m
            ), y.return = h, y;
          case Xt:
            return y = uu(y), M(h, y, m);
        }
        if (Lt(y) || Jt(y))
          return y = Ie(
            y,
            h.mode,
            m,
            null
          ), y.return = h, y;
        if (typeof y.then == "function")
          return M(h, xn(y), m);
        if (y.$$typeof === w)
          return M(
            h,
            zn(h, y),
            m
          );
        Nn(h, y);
      }
      return null;
    }
    function S(h, y, m, T) {
      var V = y !== null ? y.key : null;
      if (typeof m == "string" && m !== "" || typeof m == "number" || typeof m == "bigint")
        return V !== null ? null : f(h, y, "" + m, T);
      if (typeof m == "object" && m !== null) {
        switch (m.$$typeof) {
          case X:
            return m.key === V ? d(h, y, m, T) : null;
          case k:
            return m.key === V ? g(h, y, m, T) : null;
          case Xt:
            return m = uu(m), S(h, y, m, T);
        }
        if (Lt(m) || Jt(m))
          return V !== null ? null : z(h, y, m, T, null);
        if (typeof m.then == "function")
          return S(
            h,
            y,
            xn(m),
            T
          );
        if (m.$$typeof === w)
          return S(
            h,
            y,
            zn(h, m),
            T
          );
        Nn(h, m);
      }
      return null;
    }
    function _(h, y, m, T, V) {
      if (typeof T == "string" && T !== "" || typeof T == "number" || typeof T == "bigint")
        return h = h.get(m) || null, f(y, h, "" + T, V);
      if (typeof T == "object" && T !== null) {
        switch (T.$$typeof) {
          case X:
            return h = h.get(
              T.key === null ? m : T.key
            ) || null, d(y, h, T, V);
          case k:
            return h = h.get(
              T.key === null ? m : T.key
            ) || null, g(y, h, T, V);
          case Xt:
            return T = uu(T), _(
              h,
              y,
              m,
              T,
              V
            );
        }
        if (Lt(T) || Jt(T))
          return h = h.get(m) || null, z(y, h, T, V, null);
        if (typeof T.then == "function")
          return _(
            h,
            y,
            m,
            xn(T),
            V
          );
        if (T.$$typeof === w)
          return _(
            h,
            y,
            m,
            zn(y, T),
            V
          );
        Nn(y, T);
      }
      return null;
    }
    function G(h, y, m, T) {
      for (var V = null, yt = null, Q = y, et = y = 0, rt = null; Q !== null && et < m.length; et++) {
        Q.index > et ? (rt = Q, Q = null) : rt = Q.sibling;
        var vt = S(
          h,
          Q,
          m[et],
          T
        );
        if (vt === null) {
          Q === null && (Q = rt);
          break;
        }
        t && Q && vt.alternate === null && l(h, Q), y = n(vt, y, et), yt === null ? V = vt : yt.sibling = vt, yt = vt, Q = rt;
      }
      if (et === m.length)
        return e(h, Q), ot && Pl(h, et), V;
      if (Q === null) {
        for (; et < m.length; et++)
          Q = M(h, m[et], T), Q !== null && (y = n(
            Q,
            y,
            et
          ), yt === null ? V = Q : yt.sibling = Q, yt = Q);
        return ot && Pl(h, et), V;
      }
      for (Q = u(Q); et < m.length; et++)
        rt = _(
          Q,
          h,
          et,
          m[et],
          T
        ), rt !== null && (t && rt.alternate !== null && Q.delete(
          rt.key === null ? et : rt.key
        ), y = n(
          rt,
          y,
          et
        ), yt === null ? V = rt : yt.sibling = rt, yt = rt);
      return t && Q.forEach(function(Xe) {
        return l(h, Xe);
      }), ot && Pl(h, et), V;
    }
    function J(h, y, m, T) {
      if (m == null) throw Error(s(151));
      for (var V = null, yt = null, Q = y, et = y = 0, rt = null, vt = m.next(); Q !== null && !vt.done; et++, vt = m.next()) {
        Q.index > et ? (rt = Q, Q = null) : rt = Q.sibling;
        var Xe = S(h, Q, vt.value, T);
        if (Xe === null) {
          Q === null && (Q = rt);
          break;
        }
        t && Q && Xe.alternate === null && l(h, Q), y = n(Xe, y, et), yt === null ? V = Xe : yt.sibling = Xe, yt = Xe, Q = rt;
      }
      if (vt.done)
        return e(h, Q), ot && Pl(h, et), V;
      if (Q === null) {
        for (; !vt.done; et++, vt = m.next())
          vt = M(h, vt.value, T), vt !== null && (y = n(vt, y, et), yt === null ? V = vt : yt.sibling = vt, yt = vt);
        return ot && Pl(h, et), V;
      }
      for (Q = u(Q); !vt.done; et++, vt = m.next())
        vt = _(Q, h, et, vt.value, T), vt !== null && (t && vt.alternate !== null && Q.delete(vt.key === null ? et : vt.key), y = n(vt, y, et), yt === null ? V = vt : yt.sibling = vt, yt = vt);
      return t && Q.forEach(function(Ch) {
        return l(h, Ch);
      }), ot && Pl(h, et), V;
    }
    function zt(h, y, m, T) {
      if (typeof m == "object" && m !== null && m.type === F && m.key === null && (m = m.props.children), typeof m == "object" && m !== null) {
        switch (m.$$typeof) {
          case X:
            t: {
              for (var V = m.key; y !== null; ) {
                if (y.key === V) {
                  if (V = m.type, V === F) {
                    if (y.tag === 7) {
                      e(
                        h,
                        y.sibling
                      ), T = a(
                        y,
                        m.props.children
                      ), T.return = h, h = T;
                      break t;
                    }
                  } else if (y.elementType === V || typeof V == "object" && V !== null && V.$$typeof === Xt && uu(V) === y.type) {
                    e(
                      h,
                      y.sibling
                    ), T = a(y, m.props), za(T, m), T.return = h, h = T;
                    break t;
                  }
                  e(h, y);
                  break;
                } else l(h, y);
                y = y.sibling;
              }
              m.type === F ? (T = Ie(
                m.props.children,
                h.mode,
                T,
                m.key
              ), T.return = h, h = T) : (T = pn(
                m.type,
                m.key,
                m.props,
                null,
                h.mode,
                T
              ), za(T, m), T.return = h, h = T);
            }
            return i(h);
          case k:
            t: {
              for (V = m.key; y !== null; ) {
                if (y.key === V)
                  if (y.tag === 4 && y.stateNode.containerInfo === m.containerInfo && y.stateNode.implementation === m.implementation) {
                    e(
                      h,
                      y.sibling
                    ), T = a(y, m.children || []), T.return = h, h = T;
                    break t;
                  } else {
                    e(h, y);
                    break;
                  }
                else l(h, y);
                y = y.sibling;
              }
              T = Wi(m, h.mode, T), T.return = h, h = T;
            }
            return i(h);
          case Xt:
            return m = uu(m), zt(
              h,
              y,
              m,
              T
            );
        }
        if (Lt(m))
          return G(
            h,
            y,
            m,
            T
          );
        if (Jt(m)) {
          if (V = Jt(m), typeof V != "function") throw Error(s(150));
          return m = V.call(m), J(
            h,
            y,
            m,
            T
          );
        }
        if (typeof m.then == "function")
          return zt(
            h,
            y,
            xn(m),
            T
          );
        if (m.$$typeof === w)
          return zt(
            h,
            y,
            zn(h, m),
            T
          );
        Nn(h, m);
      }
      return typeof m == "string" && m !== "" || typeof m == "number" || typeof m == "bigint" ? (m = "" + m, y !== null && y.tag === 6 ? (e(h, y.sibling), T = a(y, m), T.return = h, h = T) : (e(h, y), T = $i(m, h.mode, T), T.return = h, h = T), i(h)) : e(h, y);
    }
    return function(h, y, m, T) {
      try {
        Ea = 0;
        var V = zt(
          h,
          y,
          m,
          T
        );
        return Ru = null, V;
      } catch (Q) {
        if (Q === ju || Q === qn) throw Q;
        var yt = gl(29, Q, null, h.mode);
        return yt.lanes = T, yt.return = h, yt;
      } finally {
      }
    };
  }
  var nu = cr(!0), fr = cr(!1), qe = !1;
  function fc(t) {
    t.updateQueue = {
      baseState: t.memoizedState,
      firstBaseUpdate: null,
      lastBaseUpdate: null,
      shared: { pending: null, lanes: 0, hiddenCallbacks: null },
      callbacks: null
    };
  }
  function sc(t, l) {
    t = t.updateQueue, l.updateQueue === t && (l.updateQueue = {
      baseState: t.baseState,
      firstBaseUpdate: t.firstBaseUpdate,
      lastBaseUpdate: t.lastBaseUpdate,
      shared: t.shared,
      callbacks: null
    });
  }
  function Te(t) {
    return { lane: t, tag: 0, payload: null, callback: null, next: null };
  }
  function xe(t, l, e) {
    var u = t.updateQueue;
    if (u === null) return null;
    if (u = u.shared, (mt & 2) !== 0) {
      var a = u.pending;
      return a === null ? l.next = l : (l.next = a.next, a.next = l), u.pending = l, l = Sn(t), Ks(t, null, e), l;
    }
    return bn(t, u, l, e), Sn(t);
  }
  function Aa(t, l, e) {
    if (l = l.updateQueue, l !== null && (l = l.shared, (e & 4194048) !== 0)) {
      var u = l.lanes;
      u &= t.pendingLanes, e |= u, l.lanes = e, Pf(t, e);
    }
  }
  function rc(t, l) {
    var e = t.updateQueue, u = t.alternate;
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
        n === null ? a = n = l : n = n.next = l;
      } else a = n = l;
      e = {
        baseState: u.baseState,
        firstBaseUpdate: a,
        lastBaseUpdate: n,
        shared: u.shared,
        callbacks: u.callbacks
      }, t.updateQueue = e;
      return;
    }
    t = e.lastBaseUpdate, t === null ? e.firstBaseUpdate = l : t.next = l, e.lastBaseUpdate = l;
  }
  var oc = !1;
  function qa() {
    if (oc) {
      var t = Cu;
      if (t !== null) throw t;
    }
  }
  function Ta(t, l, e, u) {
    oc = !1;
    var a = t.updateQueue;
    qe = !1;
    var n = a.firstBaseUpdate, i = a.lastBaseUpdate, f = a.shared.pending;
    if (f !== null) {
      a.shared.pending = null;
      var d = f, g = d.next;
      d.next = null, i === null ? n = g : i.next = g, i = d;
      var z = t.alternate;
      z !== null && (z = z.updateQueue, f = z.lastBaseUpdate, f !== i && (f === null ? z.firstBaseUpdate = g : f.next = g, z.lastBaseUpdate = d));
    }
    if (n !== null) {
      var M = a.baseState;
      i = 0, z = g = d = null, f = n;
      do {
        var S = f.lane & -536870913, _ = S !== f.lane;
        if (_ ? (st & S) === S : (u & S) === S) {
          S !== 0 && S === Uu && (oc = !0), z !== null && (z = z.next = {
            lane: 0,
            tag: f.tag,
            payload: f.payload,
            callback: null,
            next: null
          });
          t: {
            var G = t, J = f;
            S = l;
            var zt = e;
            switch (J.tag) {
              case 1:
                if (G = J.payload, typeof G == "function") {
                  M = G.call(zt, M, S);
                  break t;
                }
                M = G;
                break t;
              case 3:
                G.flags = G.flags & -65537 | 128;
              case 0:
                if (G = J.payload, S = typeof G == "function" ? G.call(zt, M, S) : G, S == null) break t;
                M = p({}, M, S);
                break t;
              case 2:
                qe = !0;
            }
          }
          S = f.callback, S !== null && (t.flags |= 64, _ && (t.flags |= 8192), _ = a.callbacks, _ === null ? a.callbacks = [S] : _.push(S));
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
      z === null && (d = M), a.baseState = d, a.firstBaseUpdate = g, a.lastBaseUpdate = z, n === null && (a.shared.lanes = 0), Ue |= i, t.lanes = i, t.memoizedState = M;
    }
  }
  function sr(t, l) {
    if (typeof t != "function")
      throw Error(s(191, t));
    t.call(l);
  }
  function rr(t, l) {
    var e = t.callbacks;
    if (e !== null)
      for (t.callbacks = null, t = 0; t < e.length; t++)
        sr(e[t], l);
  }
  var Hu = v(null), On = v(0);
  function or(t, l) {
    t = re, R(On, t), R(Hu, l), re = t | l.baseLanes;
  }
  function dc() {
    R(On, re), R(Hu, Hu.current);
  }
  function yc() {
    re = On.current, N(Hu), N(On);
  }
  var bl = v(null), Dl = null;
  function Ne(t) {
    var l = t.alternate;
    R(Dt, Dt.current & 1), R(bl, t), Dl === null && (l === null || Hu.current !== null || l.memoizedState !== null) && (Dl = t);
  }
  function vc(t) {
    R(Dt, Dt.current), R(bl, t), Dl === null && (Dl = t);
  }
  function dr(t) {
    t.tag === 22 ? (R(Dt, Dt.current), R(bl, t), Dl === null && (Dl = t)) : Oe();
  }
  function Oe() {
    R(Dt, Dt.current), R(bl, bl.current);
  }
  function Sl(t) {
    N(bl), Dl === t && (Dl = null), N(Dt);
  }
  var Dt = v(0);
  function Mn(t) {
    for (var l = t; l !== null; ) {
      if (l.tag === 13) {
        var e = l.memoizedState;
        if (e !== null && (e = e.dehydrated, e === null || _f(e) || Ef(e)))
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
  var ee = 0, lt = null, _t = null, Ht = null, Dn = !1, Bu = !1, iu = !1, Un = 0, xa = 0, Yu = null, zv = 0;
  function Ot() {
    throw Error(s(321));
  }
  function hc(t, l) {
    if (l === null) return !1;
    for (var e = 0; e < l.length && e < t.length; e++)
      if (!ml(t[e], l[e])) return !1;
    return !0;
  }
  function mc(t, l, e, u, a, n) {
    return ee = n, lt = l, l.memoizedState = null, l.updateQueue = null, l.lanes = 0, A.H = t === null || t.memoizedState === null ? $r : Dc, iu = !1, n = e(u, a), iu = !1, Bu && (n = vr(
      l,
      e,
      u,
      a
    )), yr(t), n;
  }
  function yr(t) {
    A.H = Ma;
    var l = _t !== null && _t.next !== null;
    if (ee = 0, Ht = _t = lt = null, Dn = !1, xa = 0, Yu = null, l) throw Error(s(300));
    t === null || Bt || (t = t.dependencies, t !== null && En(t) && (Bt = !0));
  }
  function vr(t, l, e, u) {
    lt = t;
    var a = 0;
    do {
      if (Bu && (Yu = null), xa = 0, Bu = !1, 25 <= a) throw Error(s(301));
      if (a += 1, Ht = _t = null, t.updateQueue != null) {
        var n = t.updateQueue;
        n.lastEffect = null, n.events = null, n.stores = null, n.memoCache != null && (n.memoCache.index = 0);
      }
      A.H = Wr, n = l(e, u);
    } while (Bu);
    return n;
  }
  function Av() {
    var t = A.H, l = t.useState()[0];
    return l = typeof l.then == "function" ? Na(l) : l, t = t.useState()[0], (_t !== null ? _t.memoizedState : null) !== t && (lt.flags |= 1024), l;
  }
  function gc() {
    var t = Un !== 0;
    return Un = 0, t;
  }
  function bc(t, l, e) {
    l.updateQueue = t.updateQueue, l.flags &= -2053, t.lanes &= ~e;
  }
  function Sc(t) {
    if (Dn) {
      for (t = t.memoizedState; t !== null; ) {
        var l = t.queue;
        l !== null && (l.pending = null), t = t.next;
      }
      Dn = !1;
    }
    ee = 0, Ht = _t = lt = null, Bu = !1, xa = Un = 0, Yu = null;
  }
  function ul() {
    var t = {
      memoizedState: null,
      baseState: null,
      baseQueue: null,
      queue: null,
      next: null
    };
    return Ht === null ? lt.memoizedState = Ht = t : Ht = Ht.next = t, Ht;
  }
  function Ut() {
    if (_t === null) {
      var t = lt.alternate;
      t = t !== null ? t.memoizedState : null;
    } else t = _t.next;
    var l = Ht === null ? lt.memoizedState : Ht.next;
    if (l !== null)
      Ht = l, _t = t;
    else {
      if (t === null)
        throw lt.alternate === null ? Error(s(467)) : Error(s(310));
      _t = t, t = {
        memoizedState: _t.memoizedState,
        baseState: _t.baseState,
        baseQueue: _t.baseQueue,
        queue: _t.queue,
        next: null
      }, Ht === null ? lt.memoizedState = Ht = t : Ht = Ht.next = t;
    }
    return Ht;
  }
  function Cn() {
    return { lastEffect: null, events: null, stores: null, memoCache: null };
  }
  function Na(t) {
    var l = xa;
    return xa += 1, Yu === null && (Yu = []), t = ar(Yu, t, l), l = lt, (Ht === null ? l.memoizedState : Ht.next) === null && (l = l.alternate, A.H = l === null || l.memoizedState === null ? $r : Dc), t;
  }
  function jn(t) {
    if (t !== null && typeof t == "object") {
      if (typeof t.then == "function") return Na(t);
      if (t.$$typeof === w) return Wt(t);
    }
    throw Error(s(438, String(t)));
  }
  function pc(t) {
    var l = null, e = lt.updateQueue;
    if (e !== null && (l = e.memoCache), l == null) {
      var u = lt.alternate;
      u !== null && (u = u.updateQueue, u !== null && (u = u.memoCache, u != null && (l = {
        data: u.data.map(function(a) {
          return a.slice();
        }),
        index: 0
      })));
    }
    if (l == null && (l = { data: [], index: 0 }), e === null && (e = Cn(), lt.updateQueue = e), e.memoCache = l, e = l.data[l.index], e === void 0)
      for (e = l.data[l.index] = Array(t), u = 0; u < t; u++)
        e[u] = Jl;
    return l.index++, e;
  }
  function ue(t, l) {
    return typeof l == "function" ? l(t) : l;
  }
  function Rn(t) {
    var l = Ut();
    return _c(l, _t, t);
  }
  function _c(t, l, e) {
    var u = t.queue;
    if (u === null) throw Error(s(311));
    u.lastRenderedReducer = e;
    var a = t.baseQueue, n = u.pending;
    if (n !== null) {
      if (a !== null) {
        var i = a.next;
        a.next = n.next, n.next = i;
      }
      l.baseQueue = a = n, u.pending = null;
    }
    if (n = t.baseState, a === null) t.memoizedState = n;
    else {
      l = a.next;
      var f = i = null, d = null, g = l, z = !1;
      do {
        var M = g.lane & -536870913;
        if (M !== g.lane ? (st & M) === M : (ee & M) === M) {
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
            }), M === Uu && (z = !0);
          else if ((ee & S) === S) {
            g = g.next, S === Uu && (z = !0);
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
            }, d === null ? (f = d = M, i = n) : d = d.next = M, lt.lanes |= S, Ue |= S;
          M = g.action, iu && e(n, M), n = g.hasEagerState ? g.eagerState : e(n, M);
        } else
          S = {
            lane: M,
            revertLane: g.revertLane,
            gesture: g.gesture,
            action: g.action,
            hasEagerState: g.hasEagerState,
            eagerState: g.eagerState,
            next: null
          }, d === null ? (f = d = S, i = n) : d = d.next = S, lt.lanes |= M, Ue |= M;
        g = g.next;
      } while (g !== null && g !== l);
      if (d === null ? i = n : d.next = f, !ml(n, t.memoizedState) && (Bt = !0, z && (e = Cu, e !== null)))
        throw e;
      t.memoizedState = n, t.baseState = i, t.baseQueue = d, u.lastRenderedState = n;
    }
    return a === null && (u.lanes = 0), [t.memoizedState, u.dispatch];
  }
  function Ec(t) {
    var l = Ut(), e = l.queue;
    if (e === null) throw Error(s(311));
    e.lastRenderedReducer = t;
    var u = e.dispatch, a = e.pending, n = l.memoizedState;
    if (a !== null) {
      e.pending = null;
      var i = a = a.next;
      do
        n = t(n, i.action), i = i.next;
      while (i !== a);
      ml(n, l.memoizedState) || (Bt = !0), l.memoizedState = n, l.baseQueue === null && (l.baseState = n), e.lastRenderedState = n;
    }
    return [n, u];
  }
  function hr(t, l, e) {
    var u = lt, a = Ut(), n = ot;
    if (n) {
      if (e === void 0) throw Error(s(407));
      e = e();
    } else e = l();
    var i = !ml(
      (_t || a).memoizedState,
      e
    );
    if (i && (a.memoizedState = e, Bt = !0), a = a.queue, qc(br.bind(null, u, a, t), [
      t
    ]), a.getSnapshot !== l || i || Ht !== null && Ht.memoizedState.tag & 1) {
      if (u.flags |= 2048, Lu(
        9,
        { destroy: void 0 },
        gr.bind(
          null,
          u,
          a,
          e,
          l
        ),
        null
      ), At === null) throw Error(s(349));
      n || (ee & 127) !== 0 || mr(u, l, e);
    }
    return e;
  }
  function mr(t, l, e) {
    t.flags |= 16384, t = { getSnapshot: l, value: e }, l = lt.updateQueue, l === null ? (l = Cn(), lt.updateQueue = l, l.stores = [t]) : (e = l.stores, e === null ? l.stores = [t] : e.push(t));
  }
  function gr(t, l, e, u) {
    l.value = e, l.getSnapshot = u, Sr(l) && pr(t);
  }
  function br(t, l, e) {
    return e(function() {
      Sr(l) && pr(t);
    });
  }
  function Sr(t) {
    var l = t.getSnapshot;
    t = t.value;
    try {
      var e = l();
      return !ml(t, e);
    } catch {
      return !0;
    }
  }
  function pr(t) {
    var l = Fe(t, 2);
    l !== null && ol(l, t, 2);
  }
  function zc(t) {
    var l = ul();
    if (typeof t == "function") {
      var e = t;
      if (t = e(), iu) {
        be(!0);
        try {
          e();
        } finally {
          be(!1);
        }
      }
    }
    return l.memoizedState = l.baseState = t, l.queue = {
      pending: null,
      lanes: 0,
      dispatch: null,
      lastRenderedReducer: ue,
      lastRenderedState: t
    }, l;
  }
  function _r(t, l, e, u) {
    return t.baseState = e, _c(
      t,
      _t,
      typeof u == "function" ? u : ue
    );
  }
  function qv(t, l, e, u, a) {
    if (Yn(t)) throw Error(s(485));
    if (t = l.action, t !== null) {
      var n = {
        payload: a,
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
      A.T !== null ? e(!0) : n.isTransition = !1, u(n), e = l.pending, e === null ? (n.next = l.pending = n, Er(l, n)) : (n.next = e.next, l.pending = e.next = n);
    }
  }
  function Er(t, l) {
    var e = l.action, u = l.payload, a = t.state;
    if (l.isTransition) {
      var n = A.T, i = {};
      A.T = i;
      try {
        var f = e(a, u), d = A.S;
        d !== null && d(i, f), zr(t, l, f);
      } catch (g) {
        Ac(t, l, g);
      } finally {
        n !== null && i.types !== null && (n.types = i.types), A.T = n;
      }
    } else
      try {
        n = e(a, u), zr(t, l, n);
      } catch (g) {
        Ac(t, l, g);
      }
  }
  function zr(t, l, e) {
    e !== null && typeof e == "object" && typeof e.then == "function" ? e.then(
      function(u) {
        Ar(t, l, u);
      },
      function(u) {
        return Ac(t, l, u);
      }
    ) : Ar(t, l, e);
  }
  function Ar(t, l, e) {
    l.status = "fulfilled", l.value = e, qr(l), t.state = e, l = t.pending, l !== null && (e = l.next, e === l ? t.pending = null : (e = e.next, l.next = e, Er(t, e)));
  }
  function Ac(t, l, e) {
    var u = t.pending;
    if (t.pending = null, u !== null) {
      u = u.next;
      do
        l.status = "rejected", l.reason = e, qr(l), l = l.next;
      while (l !== u);
    }
    t.action = null;
  }
  function qr(t) {
    t = t.listeners;
    for (var l = 0; l < t.length; l++) (0, t[l])();
  }
  function Tr(t, l) {
    return l;
  }
  function xr(t, l) {
    if (ot) {
      var e = At.formState;
      if (e !== null) {
        t: {
          var u = lt;
          if (ot) {
            if (Tt) {
              l: {
                for (var a = Tt, n = Ml; a.nodeType !== 8; ) {
                  if (!n) {
                    a = null;
                    break l;
                  }
                  if (a = Ul(
                    a.nextSibling
                  ), a === null) {
                    a = null;
                    break l;
                  }
                }
                n = a.data, a = n === "F!" || n === "F" ? a : null;
              }
              if (a) {
                Tt = Ul(
                  a.nextSibling
                ), u = a.data === "F!";
                break t;
              }
            }
            ze(u);
          }
          u = !1;
        }
        u && (l = e[0]);
      }
    }
    return e = ul(), e.memoizedState = e.baseState = l, u = {
      pending: null,
      lanes: 0,
      dispatch: null,
      lastRenderedReducer: Tr,
      lastRenderedState: l
    }, e.queue = u, e = Jr.bind(
      null,
      lt,
      u
    ), u.dispatch = e, u = zc(!1), n = Mc.bind(
      null,
      lt,
      !1,
      u.queue
    ), u = ul(), a = {
      state: l,
      dispatch: null,
      action: t,
      pending: null
    }, u.queue = a, e = qv.bind(
      null,
      lt,
      a,
      n,
      e
    ), a.dispatch = e, u.memoizedState = t, [l, e, !1];
  }
  function Nr(t) {
    var l = Ut();
    return Or(l, _t, t);
  }
  function Or(t, l, e) {
    if (l = _c(
      t,
      l,
      Tr
    )[0], t = Rn(ue)[0], typeof l == "object" && l !== null && typeof l.then == "function")
      try {
        var u = Na(l);
      } catch (i) {
        throw i === ju ? qn : i;
      }
    else u = l;
    l = Ut();
    var a = l.queue, n = a.dispatch;
    return e !== l.memoizedState && (lt.flags |= 2048, Lu(
      9,
      { destroy: void 0 },
      Tv.bind(null, a, e),
      null
    )), [u, n, t];
  }
  function Tv(t, l) {
    t.action = l;
  }
  function Mr(t) {
    var l = Ut(), e = _t;
    if (e !== null)
      return Or(l, e, t);
    Ut(), l = l.memoizedState, e = Ut();
    var u = e.queue.dispatch;
    return e.memoizedState = t, [l, u, !1];
  }
  function Lu(t, l, e, u) {
    return t = { tag: t, create: e, deps: u, inst: l, next: null }, l = lt.updateQueue, l === null && (l = Cn(), lt.updateQueue = l), e = l.lastEffect, e === null ? l.lastEffect = t.next = t : (u = e.next, e.next = t, t.next = u, l.lastEffect = t), t;
  }
  function Dr() {
    return Ut().memoizedState;
  }
  function Hn(t, l, e, u) {
    var a = ul();
    lt.flags |= t, a.memoizedState = Lu(
      1 | l,
      { destroy: void 0 },
      e,
      u === void 0 ? null : u
    );
  }
  function Bn(t, l, e, u) {
    var a = Ut();
    u = u === void 0 ? null : u;
    var n = a.memoizedState.inst;
    _t !== null && u !== null && hc(u, _t.memoizedState.deps) ? a.memoizedState = Lu(l, n, e, u) : (lt.flags |= t, a.memoizedState = Lu(
      1 | l,
      n,
      e,
      u
    ));
  }
  function Ur(t, l) {
    Hn(8390656, 8, t, l);
  }
  function qc(t, l) {
    Bn(2048, 8, t, l);
  }
  function xv(t) {
    lt.flags |= 4;
    var l = lt.updateQueue;
    if (l === null)
      l = Cn(), lt.updateQueue = l, l.events = [t];
    else {
      var e = l.events;
      e === null ? l.events = [t] : e.push(t);
    }
  }
  function Cr(t) {
    var l = Ut().memoizedState;
    return xv({ ref: l, nextImpl: t }), function() {
      if ((mt & 2) !== 0) throw Error(s(440));
      return l.impl.apply(void 0, arguments);
    };
  }
  function jr(t, l) {
    return Bn(4, 2, t, l);
  }
  function Rr(t, l) {
    return Bn(4, 4, t, l);
  }
  function Hr(t, l) {
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
  function Br(t, l, e) {
    e = e != null ? e.concat([t]) : null, Bn(4, 4, Hr.bind(null, l, t), e);
  }
  function Tc() {
  }
  function Yr(t, l) {
    var e = Ut();
    l = l === void 0 ? null : l;
    var u = e.memoizedState;
    return l !== null && hc(l, u[1]) ? u[0] : (e.memoizedState = [t, l], t);
  }
  function Lr(t, l) {
    var e = Ut();
    l = l === void 0 ? null : l;
    var u = e.memoizedState;
    if (l !== null && hc(l, u[1]))
      return u[0];
    if (u = t(), iu) {
      be(!0);
      try {
        t();
      } finally {
        be(!1);
      }
    }
    return e.memoizedState = [u, l], u;
  }
  function xc(t, l, e) {
    return e === void 0 || (ee & 1073741824) !== 0 && (st & 261930) === 0 ? t.memoizedState = l : (t.memoizedState = e, t = Qo(), lt.lanes |= t, Ue |= t, e);
  }
  function Gr(t, l, e, u) {
    return ml(e, l) ? e : Hu.current !== null ? (t = xc(t, e, u), ml(t, l) || (Bt = !0), t) : (ee & 42) === 0 || (ee & 1073741824) !== 0 && (st & 261930) === 0 ? (Bt = !0, t.memoizedState = e) : (t = Qo(), lt.lanes |= t, Ue |= t, l);
  }
  function Qr(t, l, e, u, a) {
    var n = H.p;
    H.p = n !== 0 && 8 > n ? n : 8;
    var i = A.T, f = {};
    A.T = f, Mc(t, !1, l, e);
    try {
      var d = a(), g = A.S;
      if (g !== null && g(f, d), d !== null && typeof d == "object" && typeof d.then == "function") {
        var z = Ev(
          d,
          u
        );
        Oa(
          t,
          l,
          z,
          El(t)
        );
      } else
        Oa(
          t,
          l,
          u,
          El(t)
        );
    } catch (M) {
      Oa(
        t,
        l,
        { then: function() {
        }, status: "rejected", reason: M },
        El()
      );
    } finally {
      H.p = n, i !== null && f.types !== null && (i.types = f.types), A.T = i;
    }
  }
  function Nv() {
  }
  function Nc(t, l, e, u) {
    if (t.tag !== 5) throw Error(s(476));
    var a = Xr(t).queue;
    Qr(
      t,
      a,
      l,
      Z,
      e === null ? Nv : function() {
        return Zr(t), e(u);
      }
    );
  }
  function Xr(t) {
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
        lastRenderedReducer: ue,
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
        lastRenderedReducer: ue,
        lastRenderedState: e
      },
      next: null
    }, t.memoizedState = l, t = t.alternate, t !== null && (t.memoizedState = l), l;
  }
  function Zr(t) {
    var l = Xr(t);
    l.next === null && (l = t.alternate.memoizedState), Oa(
      t,
      l.next.queue,
      {},
      El()
    );
  }
  function Oc() {
    return Wt(Ja);
  }
  function Vr() {
    return Ut().memoizedState;
  }
  function Kr() {
    return Ut().memoizedState;
  }
  function Ov(t) {
    for (var l = t.return; l !== null; ) {
      switch (l.tag) {
        case 24:
        case 3:
          var e = El();
          t = Te(e);
          var u = xe(l, t, e);
          u !== null && (ol(u, l, e), Aa(u, l, e)), l = { cache: ac() }, t.payload = l;
          return;
      }
      l = l.return;
    }
  }
  function Mv(t, l, e) {
    var u = El();
    e = {
      lane: u,
      revertLane: 0,
      gesture: null,
      action: e,
      hasEagerState: !1,
      eagerState: null,
      next: null
    }, Yn(t) ? wr(l, e) : (e = wi(t, l, e, u), e !== null && (ol(e, t, u), kr(e, l, u)));
  }
  function Jr(t, l, e) {
    var u = El();
    Oa(t, l, e, u);
  }
  function Oa(t, l, e, u) {
    var a = {
      lane: u,
      revertLane: 0,
      gesture: null,
      action: e,
      hasEagerState: !1,
      eagerState: null,
      next: null
    };
    if (Yn(t)) wr(l, a);
    else {
      var n = t.alternate;
      if (t.lanes === 0 && (n === null || n.lanes === 0) && (n = l.lastRenderedReducer, n !== null))
        try {
          var i = l.lastRenderedState, f = n(i, e);
          if (a.hasEagerState = !0, a.eagerState = f, ml(f, i))
            return bn(t, l, a, 0), At === null && gn(), !1;
        } catch {
        } finally {
        }
      if (e = wi(t, l, a, u), e !== null)
        return ol(e, t, u), kr(e, l, u), !0;
    }
    return !1;
  }
  function Mc(t, l, e, u) {
    if (u = {
      lane: 2,
      revertLane: sf(),
      gesture: null,
      action: u,
      hasEagerState: !1,
      eagerState: null,
      next: null
    }, Yn(t)) {
      if (l) throw Error(s(479));
    } else
      l = wi(
        t,
        e,
        u,
        2
      ), l !== null && ol(l, t, 2);
  }
  function Yn(t) {
    var l = t.alternate;
    return t === lt || l !== null && l === lt;
  }
  function wr(t, l) {
    Bu = Dn = !0;
    var e = t.pending;
    e === null ? l.next = l : (l.next = e.next, e.next = l), t.pending = l;
  }
  function kr(t, l, e) {
    if ((e & 4194048) !== 0) {
      var u = l.lanes;
      u &= t.pendingLanes, e |= u, l.lanes = e, Pf(t, e);
    }
  }
  var Ma = {
    readContext: Wt,
    use: jn,
    useCallback: Ot,
    useContext: Ot,
    useEffect: Ot,
    useImperativeHandle: Ot,
    useLayoutEffect: Ot,
    useInsertionEffect: Ot,
    useMemo: Ot,
    useReducer: Ot,
    useRef: Ot,
    useState: Ot,
    useDebugValue: Ot,
    useDeferredValue: Ot,
    useTransition: Ot,
    useSyncExternalStore: Ot,
    useId: Ot,
    useHostTransitionStatus: Ot,
    useFormState: Ot,
    useActionState: Ot,
    useOptimistic: Ot,
    useMemoCache: Ot,
    useCacheRefresh: Ot
  };
  Ma.useEffectEvent = Ot;
  var $r = {
    readContext: Wt,
    use: jn,
    useCallback: function(t, l) {
      return ul().memoizedState = [
        t,
        l === void 0 ? null : l
      ], t;
    },
    useContext: Wt,
    useEffect: Ur,
    useImperativeHandle: function(t, l, e) {
      e = e != null ? e.concat([t]) : null, Hn(
        4194308,
        4,
        Hr.bind(null, l, t),
        e
      );
    },
    useLayoutEffect: function(t, l) {
      return Hn(4194308, 4, t, l);
    },
    useInsertionEffect: function(t, l) {
      Hn(4, 2, t, l);
    },
    useMemo: function(t, l) {
      var e = ul();
      l = l === void 0 ? null : l;
      var u = t();
      if (iu) {
        be(!0);
        try {
          t();
        } finally {
          be(!1);
        }
      }
      return e.memoizedState = [u, l], u;
    },
    useReducer: function(t, l, e) {
      var u = ul();
      if (e !== void 0) {
        var a = e(l);
        if (iu) {
          be(!0);
          try {
            e(l);
          } finally {
            be(!1);
          }
        }
      } else a = l;
      return u.memoizedState = u.baseState = a, t = {
        pending: null,
        lanes: 0,
        dispatch: null,
        lastRenderedReducer: t,
        lastRenderedState: a
      }, u.queue = t, t = t.dispatch = Mv.bind(
        null,
        lt,
        t
      ), [u.memoizedState, t];
    },
    useRef: function(t) {
      var l = ul();
      return t = { current: t }, l.memoizedState = t;
    },
    useState: function(t) {
      t = zc(t);
      var l = t.queue, e = Jr.bind(null, lt, l);
      return l.dispatch = e, [t.memoizedState, e];
    },
    useDebugValue: Tc,
    useDeferredValue: function(t, l) {
      var e = ul();
      return xc(e, t, l);
    },
    useTransition: function() {
      var t = zc(!1);
      return t = Qr.bind(
        null,
        lt,
        t.queue,
        !0,
        !1
      ), ul().memoizedState = t, [!1, t];
    },
    useSyncExternalStore: function(t, l, e) {
      var u = lt, a = ul();
      if (ot) {
        if (e === void 0)
          throw Error(s(407));
        e = e();
      } else {
        if (e = l(), At === null)
          throw Error(s(349));
        (st & 127) !== 0 || mr(u, l, e);
      }
      a.memoizedState = e;
      var n = { value: e, getSnapshot: l };
      return a.queue = n, Ur(br.bind(null, u, n, t), [
        t
      ]), u.flags |= 2048, Lu(
        9,
        { destroy: void 0 },
        gr.bind(
          null,
          u,
          n,
          e,
          l
        ),
        null
      ), e;
    },
    useId: function() {
      var t = ul(), l = At.identifierPrefix;
      if (ot) {
        var e = Xl, u = Ql;
        e = (u & ~(1 << 32 - hl(u) - 1)).toString(32) + e, l = "_" + l + "R_" + e, e = Un++, 0 < e && (l += "H" + e.toString(32)), l += "_";
      } else
        e = zv++, l = "_" + l + "r_" + e.toString(32) + "_";
      return t.memoizedState = l;
    },
    useHostTransitionStatus: Oc,
    useFormState: xr,
    useActionState: xr,
    useOptimistic: function(t) {
      var l = ul();
      l.memoizedState = l.baseState = t;
      var e = {
        pending: null,
        lanes: 0,
        dispatch: null,
        lastRenderedReducer: null,
        lastRenderedState: null
      };
      return l.queue = e, l = Mc.bind(
        null,
        lt,
        !0,
        e
      ), e.dispatch = l, [t, l];
    },
    useMemoCache: pc,
    useCacheRefresh: function() {
      return ul().memoizedState = Ov.bind(
        null,
        lt
      );
    },
    useEffectEvent: function(t) {
      var l = ul(), e = { impl: t };
      return l.memoizedState = e, function() {
        if ((mt & 2) !== 0)
          throw Error(s(440));
        return e.impl.apply(void 0, arguments);
      };
    }
  }, Dc = {
    readContext: Wt,
    use: jn,
    useCallback: Yr,
    useContext: Wt,
    useEffect: qc,
    useImperativeHandle: Br,
    useInsertionEffect: jr,
    useLayoutEffect: Rr,
    useMemo: Lr,
    useReducer: Rn,
    useRef: Dr,
    useState: function() {
      return Rn(ue);
    },
    useDebugValue: Tc,
    useDeferredValue: function(t, l) {
      var e = Ut();
      return Gr(
        e,
        _t.memoizedState,
        t,
        l
      );
    },
    useTransition: function() {
      var t = Rn(ue)[0], l = Ut().memoizedState;
      return [
        typeof t == "boolean" ? t : Na(t),
        l
      ];
    },
    useSyncExternalStore: hr,
    useId: Vr,
    useHostTransitionStatus: Oc,
    useFormState: Nr,
    useActionState: Nr,
    useOptimistic: function(t, l) {
      var e = Ut();
      return _r(e, _t, t, l);
    },
    useMemoCache: pc,
    useCacheRefresh: Kr
  };
  Dc.useEffectEvent = Cr;
  var Wr = {
    readContext: Wt,
    use: jn,
    useCallback: Yr,
    useContext: Wt,
    useEffect: qc,
    useImperativeHandle: Br,
    useInsertionEffect: jr,
    useLayoutEffect: Rr,
    useMemo: Lr,
    useReducer: Ec,
    useRef: Dr,
    useState: function() {
      return Ec(ue);
    },
    useDebugValue: Tc,
    useDeferredValue: function(t, l) {
      var e = Ut();
      return _t === null ? xc(e, t, l) : Gr(
        e,
        _t.memoizedState,
        t,
        l
      );
    },
    useTransition: function() {
      var t = Ec(ue)[0], l = Ut().memoizedState;
      return [
        typeof t == "boolean" ? t : Na(t),
        l
      ];
    },
    useSyncExternalStore: hr,
    useId: Vr,
    useHostTransitionStatus: Oc,
    useFormState: Mr,
    useActionState: Mr,
    useOptimistic: function(t, l) {
      var e = Ut();
      return _t !== null ? _r(e, _t, t, l) : (e.baseState = t, [t, e.queue.dispatch]);
    },
    useMemoCache: pc,
    useCacheRefresh: Kr
  };
  Wr.useEffectEvent = Cr;
  function Uc(t, l, e, u) {
    l = t.memoizedState, e = e(u, l), e = e == null ? l : p({}, l, e), t.memoizedState = e, t.lanes === 0 && (t.updateQueue.baseState = e);
  }
  var Cc = {
    enqueueSetState: function(t, l, e) {
      t = t._reactInternals;
      var u = El(), a = Te(u);
      a.payload = l, e != null && (a.callback = e), l = xe(t, a, u), l !== null && (ol(l, t, u), Aa(l, t, u));
    },
    enqueueReplaceState: function(t, l, e) {
      t = t._reactInternals;
      var u = El(), a = Te(u);
      a.tag = 1, a.payload = l, e != null && (a.callback = e), l = xe(t, a, u), l !== null && (ol(l, t, u), Aa(l, t, u));
    },
    enqueueForceUpdate: function(t, l) {
      t = t._reactInternals;
      var e = El(), u = Te(e);
      u.tag = 2, l != null && (u.callback = l), l = xe(t, u, e), l !== null && (ol(l, t, e), Aa(l, t, e));
    }
  };
  function Fr(t, l, e, u, a, n, i) {
    return t = t.stateNode, typeof t.shouldComponentUpdate == "function" ? t.shouldComponentUpdate(u, n, i) : l.prototype && l.prototype.isPureReactComponent ? !ma(e, u) || !ma(a, n) : !0;
  }
  function Ir(t, l, e, u) {
    t = l.state, typeof l.componentWillReceiveProps == "function" && l.componentWillReceiveProps(e, u), typeof l.UNSAFE_componentWillReceiveProps == "function" && l.UNSAFE_componentWillReceiveProps(e, u), l.state !== t && Cc.enqueueReplaceState(l, l.state, null);
  }
  function cu(t, l) {
    var e = l;
    if ("ref" in l) {
      e = {};
      for (var u in l)
        u !== "ref" && (e[u] = l[u]);
    }
    if (t = t.defaultProps) {
      e === l && (e = p({}, e));
      for (var a in t)
        e[a] === void 0 && (e[a] = t[a]);
    }
    return e;
  }
  function Pr(t) {
    mn(t);
  }
  function to(t) {
    console.error(t);
  }
  function lo(t) {
    mn(t);
  }
  function Ln(t, l) {
    try {
      var e = t.onUncaughtError;
      e(l.value, { componentStack: l.stack });
    } catch (u) {
      setTimeout(function() {
        throw u;
      });
    }
  }
  function eo(t, l, e) {
    try {
      var u = t.onCaughtError;
      u(e.value, {
        componentStack: e.stack,
        errorBoundary: l.tag === 1 ? l.stateNode : null
      });
    } catch (a) {
      setTimeout(function() {
        throw a;
      });
    }
  }
  function jc(t, l, e) {
    return e = Te(e), e.tag = 3, e.payload = { element: null }, e.callback = function() {
      Ln(t, l);
    }, e;
  }
  function uo(t) {
    return t = Te(t), t.tag = 3, t;
  }
  function ao(t, l, e, u) {
    var a = e.type.getDerivedStateFromError;
    if (typeof a == "function") {
      var n = u.value;
      t.payload = function() {
        return a(n);
      }, t.callback = function() {
        eo(l, e, u);
      };
    }
    var i = e.stateNode;
    i !== null && typeof i.componentDidCatch == "function" && (t.callback = function() {
      eo(l, e, u), typeof a != "function" && (Ce === null ? Ce = /* @__PURE__ */ new Set([this]) : Ce.add(this));
      var f = u.stack;
      this.componentDidCatch(u.value, {
        componentStack: f !== null ? f : ""
      });
    });
  }
  function Dv(t, l, e, u, a) {
    if (e.flags |= 32768, u !== null && typeof u == "object" && typeof u.then == "function") {
      if (l = e.alternate, l !== null && Du(
        l,
        e,
        a,
        !0
      ), e = bl.current, e !== null) {
        switch (e.tag) {
          case 31:
          case 13:
            return Dl === null ? Fn() : e.alternate === null && Mt === 0 && (Mt = 3), e.flags &= -257, e.flags |= 65536, e.lanes = a, u === Tn ? e.flags |= 16384 : (l = e.updateQueue, l === null ? e.updateQueue = /* @__PURE__ */ new Set([u]) : l.add(u), nf(t, u, a)), !1;
          case 22:
            return e.flags |= 65536, u === Tn ? e.flags |= 16384 : (l = e.updateQueue, l === null ? (l = {
              transitions: null,
              markerInstances: null,
              retryQueue: /* @__PURE__ */ new Set([u])
            }, e.updateQueue = l) : (e = l.retryQueue, e === null ? l.retryQueue = /* @__PURE__ */ new Set([u]) : e.add(u)), nf(t, u, a)), !1;
        }
        throw Error(s(435, e.tag));
      }
      return nf(t, u, a), Fn(), !1;
    }
    if (ot)
      return l = bl.current, l !== null ? ((l.flags & 65536) === 0 && (l.flags |= 256), l.flags |= 65536, l.lanes = a, u !== Pi && (t = Error(s(422), { cause: u }), Sa(xl(t, e)))) : (u !== Pi && (l = Error(s(423), {
        cause: u
      }), Sa(
        xl(l, e)
      )), t = t.current.alternate, t.flags |= 65536, a &= -a, t.lanes |= a, u = xl(u, e), a = jc(
        t.stateNode,
        u,
        a
      ), rc(t, a), Mt !== 4 && (Mt = 2)), !1;
    var n = Error(s(520), { cause: u });
    if (n = xl(n, e), Ya === null ? Ya = [n] : Ya.push(n), Mt !== 4 && (Mt = 2), l === null) return !0;
    u = xl(u, e), e = l;
    do {
      switch (e.tag) {
        case 3:
          return e.flags |= 65536, t = a & -a, e.lanes |= t, t = jc(e.stateNode, u, t), rc(e, t), !1;
        case 1:
          if (l = e.type, n = e.stateNode, (e.flags & 128) === 0 && (typeof l.getDerivedStateFromError == "function" || n !== null && typeof n.componentDidCatch == "function" && (Ce === null || !Ce.has(n))))
            return e.flags |= 65536, a &= -a, e.lanes |= a, a = uo(a), ao(
              a,
              t,
              e,
              u
            ), rc(e, a), !1;
      }
      e = e.return;
    } while (e !== null);
    return !1;
  }
  var Rc = Error(s(461)), Bt = !1;
  function Ft(t, l, e, u) {
    l.child = t === null ? fr(l, null, e, u) : nu(
      l,
      t.child,
      e,
      u
    );
  }
  function no(t, l, e, u, a) {
    e = e.render;
    var n = l.ref;
    if ("ref" in u) {
      var i = {};
      for (var f in u)
        f !== "ref" && (i[f] = u[f]);
    } else i = u;
    return lu(l), u = mc(
      t,
      l,
      e,
      i,
      n,
      a
    ), f = gc(), t !== null && !Bt ? (bc(t, l, a), ae(t, l, a)) : (ot && f && Fi(l), l.flags |= 1, Ft(t, l, u, a), l.child);
  }
  function io(t, l, e, u, a) {
    if (t === null) {
      var n = e.type;
      return typeof n == "function" && !ki(n) && n.defaultProps === void 0 && e.compare === null ? (l.tag = 15, l.type = n, co(
        t,
        l,
        n,
        u,
        a
      )) : (t = pn(
        e.type,
        null,
        u,
        l,
        l.mode,
        a
      ), t.ref = l.ref, t.return = l, l.child = t);
    }
    if (n = t.child, !Zc(t, a)) {
      var i = n.memoizedProps;
      if (e = e.compare, e = e !== null ? e : ma, e(i, u) && t.ref === l.ref)
        return ae(t, l, a);
    }
    return l.flags |= 1, t = Il(n, u), t.ref = l.ref, t.return = l, l.child = t;
  }
  function co(t, l, e, u, a) {
    if (t !== null) {
      var n = t.memoizedProps;
      if (ma(n, u) && t.ref === l.ref)
        if (Bt = !1, l.pendingProps = u = n, Zc(t, a))
          (t.flags & 131072) !== 0 && (Bt = !0);
        else
          return l.lanes = t.lanes, ae(t, l, a);
    }
    return Hc(
      t,
      l,
      e,
      u,
      a
    );
  }
  function fo(t, l, e, u) {
    var a = u.children, n = t !== null ? t.memoizedState : null;
    if (t === null && l.stateNode === null && (l.stateNode = {
      _visibility: 1,
      _pendingMarkers: null,
      _retryCache: null,
      _transitions: null
    }), u.mode === "hidden") {
      if ((l.flags & 128) !== 0) {
        if (n = n !== null ? n.baseLanes | e : e, t !== null) {
          for (u = l.child = t.child, a = 0; u !== null; )
            a = a | u.lanes | u.childLanes, u = u.sibling;
          u = a & ~n;
        } else u = 0, l.child = null;
        return so(
          t,
          l,
          n,
          e,
          u
        );
      }
      if ((e & 536870912) !== 0)
        l.memoizedState = { baseLanes: 0, cachePool: null }, t !== null && An(
          l,
          n !== null ? n.cachePool : null
        ), n !== null ? or(l, n) : dc(), dr(l);
      else
        return u = l.lanes = 536870912, so(
          t,
          l,
          n !== null ? n.baseLanes | e : e,
          e,
          u
        );
    } else
      n !== null ? (An(l, n.cachePool), or(l, n), Oe(), l.memoizedState = null) : (t !== null && An(l, null), dc(), Oe());
    return Ft(t, l, a, e), l.child;
  }
  function Da(t, l) {
    return t !== null && t.tag === 22 || l.stateNode !== null || (l.stateNode = {
      _visibility: 1,
      _pendingMarkers: null,
      _retryCache: null,
      _transitions: null
    }), l.sibling;
  }
  function so(t, l, e, u, a) {
    var n = ic();
    return n = n === null ? null : { parent: Rt._currentValue, pool: n }, l.memoizedState = {
      baseLanes: e,
      cachePool: n
    }, t !== null && An(l, null), dc(), dr(l), t !== null && Du(t, l, u, !0), l.childLanes = a, null;
  }
  function Gn(t, l) {
    return l = Xn(
      { mode: l.mode, children: l.children },
      t.mode
    ), l.ref = t.ref, t.child = l, l.return = t, l;
  }
  function ro(t, l, e) {
    return nu(l, t.child, null, e), t = Gn(l, l.pendingProps), t.flags |= 2, Sl(l), l.memoizedState = null, t;
  }
  function Uv(t, l, e) {
    var u = l.pendingProps, a = (l.flags & 128) !== 0;
    if (l.flags &= -129, t === null) {
      if (ot) {
        if (u.mode === "hidden")
          return t = Gn(l, u), l.lanes = 536870912, Da(null, t);
        if (vc(l), (t = Tt) ? (t = zd(
          t,
          Ml
        ), t = t !== null && t.data === "&" ? t : null, t !== null && (l.memoizedState = {
          dehydrated: t,
          treeContext: _e !== null ? { id: Ql, overflow: Xl } : null,
          retryLane: 536870912,
          hydrationErrors: null
        }, e = ws(t), e.return = l, l.child = e, $t = l, Tt = null)) : t = null, t === null) throw ze(l);
        return l.lanes = 536870912, null;
      }
      return Gn(l, u);
    }
    var n = t.memoizedState;
    if (n !== null) {
      var i = n.dehydrated;
      if (vc(l), a)
        if (l.flags & 256)
          l.flags &= -257, l = ro(
            t,
            l,
            e
          );
        else if (l.memoizedState !== null)
          l.child = t.child, l.flags |= 128, l = null;
        else throw Error(s(558));
      else if (Bt || Du(t, l, e, !1), a = (e & t.childLanes) !== 0, Bt || a) {
        if (u = At, u !== null && (i = ts(u, e), i !== 0 && i !== n.retryLane))
          throw n.retryLane = i, Fe(t, i), ol(u, t, i), Rc;
        Fn(), l = ro(
          t,
          l,
          e
        );
      } else
        t = n.treeContext, Tt = Ul(i.nextSibling), $t = l, ot = !0, Ee = null, Ml = !1, t !== null && Ws(l, t), l = Gn(l, u), l.flags |= 4096;
      return l;
    }
    return t = Il(t.child, {
      mode: u.mode,
      children: u.children
    }), t.ref = l.ref, l.child = t, t.return = l, t;
  }
  function Qn(t, l) {
    var e = l.ref;
    if (e === null)
      t !== null && t.ref !== null && (l.flags |= 4194816);
    else {
      if (typeof e != "function" && typeof e != "object")
        throw Error(s(284));
      (t === null || t.ref !== e) && (l.flags |= 4194816);
    }
  }
  function Hc(t, l, e, u, a) {
    return lu(l), e = mc(
      t,
      l,
      e,
      u,
      void 0,
      a
    ), u = gc(), t !== null && !Bt ? (bc(t, l, a), ae(t, l, a)) : (ot && u && Fi(l), l.flags |= 1, Ft(t, l, e, a), l.child);
  }
  function oo(t, l, e, u, a, n) {
    return lu(l), l.updateQueue = null, e = vr(
      l,
      u,
      e,
      a
    ), yr(t), u = gc(), t !== null && !Bt ? (bc(t, l, n), ae(t, l, n)) : (ot && u && Fi(l), l.flags |= 1, Ft(t, l, e, n), l.child);
  }
  function yo(t, l, e, u, a) {
    if (lu(l), l.stateNode === null) {
      var n = xu, i = e.contextType;
      typeof i == "object" && i !== null && (n = Wt(i)), n = new e(u, n), l.memoizedState = n.state !== null && n.state !== void 0 ? n.state : null, n.updater = Cc, l.stateNode = n, n._reactInternals = l, n = l.stateNode, n.props = u, n.state = l.memoizedState, n.refs = {}, fc(l), i = e.contextType, n.context = typeof i == "object" && i !== null ? Wt(i) : xu, n.state = l.memoizedState, i = e.getDerivedStateFromProps, typeof i == "function" && (Uc(
        l,
        e,
        i,
        u
      ), n.state = l.memoizedState), typeof e.getDerivedStateFromProps == "function" || typeof n.getSnapshotBeforeUpdate == "function" || typeof n.UNSAFE_componentWillMount != "function" && typeof n.componentWillMount != "function" || (i = n.state, typeof n.componentWillMount == "function" && n.componentWillMount(), typeof n.UNSAFE_componentWillMount == "function" && n.UNSAFE_componentWillMount(), i !== n.state && Cc.enqueueReplaceState(n, n.state, null), Ta(l, u, n, a), qa(), n.state = l.memoizedState), typeof n.componentDidMount == "function" && (l.flags |= 4194308), u = !0;
    } else if (t === null) {
      n = l.stateNode;
      var f = l.memoizedProps, d = cu(e, f);
      n.props = d;
      var g = n.context, z = e.contextType;
      i = xu, typeof z == "object" && z !== null && (i = Wt(z));
      var M = e.getDerivedStateFromProps;
      z = typeof M == "function" || typeof n.getSnapshotBeforeUpdate == "function", f = l.pendingProps !== f, z || typeof n.UNSAFE_componentWillReceiveProps != "function" && typeof n.componentWillReceiveProps != "function" || (f || g !== i) && Ir(
        l,
        n,
        u,
        i
      ), qe = !1;
      var S = l.memoizedState;
      n.state = S, Ta(l, u, n, a), qa(), g = l.memoizedState, f || S !== g || qe ? (typeof M == "function" && (Uc(
        l,
        e,
        M,
        u
      ), g = l.memoizedState), (d = qe || Fr(
        l,
        e,
        d,
        u,
        S,
        g,
        i
      )) ? (z || typeof n.UNSAFE_componentWillMount != "function" && typeof n.componentWillMount != "function" || (typeof n.componentWillMount == "function" && n.componentWillMount(), typeof n.UNSAFE_componentWillMount == "function" && n.UNSAFE_componentWillMount()), typeof n.componentDidMount == "function" && (l.flags |= 4194308)) : (typeof n.componentDidMount == "function" && (l.flags |= 4194308), l.memoizedProps = u, l.memoizedState = g), n.props = u, n.state = g, n.context = i, u = d) : (typeof n.componentDidMount == "function" && (l.flags |= 4194308), u = !1);
    } else {
      n = l.stateNode, sc(t, l), i = l.memoizedProps, z = cu(e, i), n.props = z, M = l.pendingProps, S = n.context, g = e.contextType, d = xu, typeof g == "object" && g !== null && (d = Wt(g)), f = e.getDerivedStateFromProps, (g = typeof f == "function" || typeof n.getSnapshotBeforeUpdate == "function") || typeof n.UNSAFE_componentWillReceiveProps != "function" && typeof n.componentWillReceiveProps != "function" || (i !== M || S !== d) && Ir(
        l,
        n,
        u,
        d
      ), qe = !1, S = l.memoizedState, n.state = S, Ta(l, u, n, a), qa();
      var _ = l.memoizedState;
      i !== M || S !== _ || qe || t !== null && t.dependencies !== null && En(t.dependencies) ? (typeof f == "function" && (Uc(
        l,
        e,
        f,
        u
      ), _ = l.memoizedState), (z = qe || Fr(
        l,
        e,
        z,
        u,
        S,
        _,
        d
      ) || t !== null && t.dependencies !== null && En(t.dependencies)) ? (g || typeof n.UNSAFE_componentWillUpdate != "function" && typeof n.componentWillUpdate != "function" || (typeof n.componentWillUpdate == "function" && n.componentWillUpdate(u, _, d), typeof n.UNSAFE_componentWillUpdate == "function" && n.UNSAFE_componentWillUpdate(
        u,
        _,
        d
      )), typeof n.componentDidUpdate == "function" && (l.flags |= 4), typeof n.getSnapshotBeforeUpdate == "function" && (l.flags |= 1024)) : (typeof n.componentDidUpdate != "function" || i === t.memoizedProps && S === t.memoizedState || (l.flags |= 4), typeof n.getSnapshotBeforeUpdate != "function" || i === t.memoizedProps && S === t.memoizedState || (l.flags |= 1024), l.memoizedProps = u, l.memoizedState = _), n.props = u, n.state = _, n.context = d, u = z) : (typeof n.componentDidUpdate != "function" || i === t.memoizedProps && S === t.memoizedState || (l.flags |= 4), typeof n.getSnapshotBeforeUpdate != "function" || i === t.memoizedProps && S === t.memoizedState || (l.flags |= 1024), u = !1);
    }
    return n = u, Qn(t, l), u = (l.flags & 128) !== 0, n || u ? (n = l.stateNode, e = u && typeof e.getDerivedStateFromError != "function" ? null : n.render(), l.flags |= 1, t !== null && u ? (l.child = nu(
      l,
      t.child,
      null,
      a
    ), l.child = nu(
      l,
      null,
      e,
      a
    )) : Ft(t, l, e, a), l.memoizedState = n.state, t = l.child) : t = ae(
      t,
      l,
      a
    ), t;
  }
  function vo(t, l, e, u) {
    return Pe(), l.flags |= 256, Ft(t, l, e, u), l.child;
  }
  var Bc = {
    dehydrated: null,
    treeContext: null,
    retryLane: 0,
    hydrationErrors: null
  };
  function Yc(t) {
    return { baseLanes: t, cachePool: er() };
  }
  function Lc(t, l, e) {
    return t = t !== null ? t.childLanes & ~e : 0, l && (t |= _l), t;
  }
  function ho(t, l, e) {
    var u = l.pendingProps, a = !1, n = (l.flags & 128) !== 0, i;
    if ((i = n) || (i = t !== null && t.memoizedState === null ? !1 : (Dt.current & 2) !== 0), i && (a = !0, l.flags &= -129), i = (l.flags & 32) !== 0, l.flags &= -33, t === null) {
      if (ot) {
        if (a ? Ne(l) : Oe(), (t = Tt) ? (t = zd(
          t,
          Ml
        ), t = t !== null && t.data !== "&" ? t : null, t !== null && (l.memoizedState = {
          dehydrated: t,
          treeContext: _e !== null ? { id: Ql, overflow: Xl } : null,
          retryLane: 536870912,
          hydrationErrors: null
        }, e = ws(t), e.return = l, l.child = e, $t = l, Tt = null)) : t = null, t === null) throw ze(l);
        return Ef(t) ? l.lanes = 32 : l.lanes = 536870912, null;
      }
      var f = u.children;
      return u = u.fallback, a ? (Oe(), a = l.mode, f = Xn(
        { mode: "hidden", children: f },
        a
      ), u = Ie(
        u,
        a,
        e,
        null
      ), f.return = l, u.return = l, f.sibling = u, l.child = f, u = l.child, u.memoizedState = Yc(e), u.childLanes = Lc(
        t,
        i,
        e
      ), l.memoizedState = Bc, Da(null, u)) : (Ne(l), Gc(l, f));
    }
    var d = t.memoizedState;
    if (d !== null && (f = d.dehydrated, f !== null)) {
      if (n)
        l.flags & 256 ? (Ne(l), l.flags &= -257, l = Qc(
          t,
          l,
          e
        )) : l.memoizedState !== null ? (Oe(), l.child = t.child, l.flags |= 128, l = null) : (Oe(), f = u.fallback, a = l.mode, u = Xn(
          { mode: "visible", children: u.children },
          a
        ), f = Ie(
          f,
          a,
          e,
          null
        ), f.flags |= 2, u.return = l, f.return = l, u.sibling = f, l.child = u, nu(
          l,
          t.child,
          null,
          e
        ), u = l.child, u.memoizedState = Yc(e), u.childLanes = Lc(
          t,
          i,
          e
        ), l.memoizedState = Bc, l = Da(null, u));
      else if (Ne(l), Ef(f)) {
        if (i = f.nextSibling && f.nextSibling.dataset, i) var g = i.dgst;
        i = g, u = Error(s(419)), u.stack = "", u.digest = i, Sa({ value: u, source: null, stack: null }), l = Qc(
          t,
          l,
          e
        );
      } else if (Bt || Du(t, l, e, !1), i = (e & t.childLanes) !== 0, Bt || i) {
        if (i = At, i !== null && (u = ts(i, e), u !== 0 && u !== d.retryLane))
          throw d.retryLane = u, Fe(t, u), ol(i, t, u), Rc;
        _f(f) || Fn(), l = Qc(
          t,
          l,
          e
        );
      } else
        _f(f) ? (l.flags |= 192, l.child = t.child, l = null) : (t = d.treeContext, Tt = Ul(
          f.nextSibling
        ), $t = l, ot = !0, Ee = null, Ml = !1, t !== null && Ws(l, t), l = Gc(
          l,
          u.children
        ), l.flags |= 4096);
      return l;
    }
    return a ? (Oe(), f = u.fallback, a = l.mode, d = t.child, g = d.sibling, u = Il(d, {
      mode: "hidden",
      children: u.children
    }), u.subtreeFlags = d.subtreeFlags & 65011712, g !== null ? f = Il(
      g,
      f
    ) : (f = Ie(
      f,
      a,
      e,
      null
    ), f.flags |= 2), f.return = l, u.return = l, u.sibling = f, l.child = u, Da(null, u), u = l.child, f = t.child.memoizedState, f === null ? f = Yc(e) : (a = f.cachePool, a !== null ? (d = Rt._currentValue, a = a.parent !== d ? { parent: d, pool: d } : a) : a = er(), f = {
      baseLanes: f.baseLanes | e,
      cachePool: a
    }), u.memoizedState = f, u.childLanes = Lc(
      t,
      i,
      e
    ), l.memoizedState = Bc, Da(t.child, u)) : (Ne(l), e = t.child, t = e.sibling, e = Il(e, {
      mode: "visible",
      children: u.children
    }), e.return = l, e.sibling = null, t !== null && (i = l.deletions, i === null ? (l.deletions = [t], l.flags |= 16) : i.push(t)), l.child = e, l.memoizedState = null, e);
  }
  function Gc(t, l) {
    return l = Xn(
      { mode: "visible", children: l },
      t.mode
    ), l.return = t, t.child = l;
  }
  function Xn(t, l) {
    return t = gl(22, t, null, l), t.lanes = 0, t;
  }
  function Qc(t, l, e) {
    return nu(l, t.child, null, e), t = Gc(
      l,
      l.pendingProps.children
    ), t.flags |= 2, l.memoizedState = null, t;
  }
  function mo(t, l, e) {
    t.lanes |= l;
    var u = t.alternate;
    u !== null && (u.lanes |= l), ec(t.return, l, e);
  }
  function Xc(t, l, e, u, a, n) {
    var i = t.memoizedState;
    i === null ? t.memoizedState = {
      isBackwards: l,
      rendering: null,
      renderingStartTime: 0,
      last: u,
      tail: e,
      tailMode: a,
      treeForkCount: n
    } : (i.isBackwards = l, i.rendering = null, i.renderingStartTime = 0, i.last = u, i.tail = e, i.tailMode = a, i.treeForkCount = n);
  }
  function go(t, l, e) {
    var u = l.pendingProps, a = u.revealOrder, n = u.tail;
    u = u.children;
    var i = Dt.current, f = (i & 2) !== 0;
    if (f ? (i = i & 1 | 2, l.flags |= 128) : i &= 1, R(Dt, i), Ft(t, l, u, e), u = ot ? ba : 0, !f && t !== null && (t.flags & 128) !== 0)
      t: for (t = l.child; t !== null; ) {
        if (t.tag === 13)
          t.memoizedState !== null && mo(t, e, l);
        else if (t.tag === 19)
          mo(t, e, l);
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
    switch (a) {
      case "forwards":
        for (e = l.child, a = null; e !== null; )
          t = e.alternate, t !== null && Mn(t) === null && (a = e), e = e.sibling;
        e = a, e === null ? (a = l.child, l.child = null) : (a = e.sibling, e.sibling = null), Xc(
          l,
          !1,
          a,
          e,
          n,
          u
        );
        break;
      case "backwards":
      case "unstable_legacy-backwards":
        for (e = null, a = l.child, l.child = null; a !== null; ) {
          if (t = a.alternate, t !== null && Mn(t) === null) {
            l.child = a;
            break;
          }
          t = a.sibling, a.sibling = e, e = a, a = t;
        }
        Xc(
          l,
          !0,
          e,
          null,
          n,
          u
        );
        break;
      case "together":
        Xc(
          l,
          !1,
          null,
          null,
          void 0,
          u
        );
        break;
      default:
        l.memoizedState = null;
    }
    return l.child;
  }
  function ae(t, l, e) {
    if (t !== null && (l.dependencies = t.dependencies), Ue |= l.lanes, (e & l.childLanes) === 0)
      if (t !== null) {
        if (Du(
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
      for (t = l.child, e = Il(t, t.pendingProps), l.child = e, e.return = l; t.sibling !== null; )
        t = t.sibling, e = e.sibling = Il(t, t.pendingProps), e.return = l;
      e.sibling = null;
    }
    return l.child;
  }
  function Zc(t, l) {
    return (t.lanes & l) !== 0 ? !0 : (t = t.dependencies, !!(t !== null && En(t)));
  }
  function Cv(t, l, e) {
    switch (l.tag) {
      case 3:
        wt(l, l.stateNode.containerInfo), Ae(l, Rt, t.memoizedState.cache), Pe();
        break;
      case 27:
      case 5:
        me(l);
        break;
      case 4:
        wt(l, l.stateNode.containerInfo);
        break;
      case 10:
        Ae(
          l,
          l.type,
          l.memoizedProps.value
        );
        break;
      case 31:
        if (l.memoizedState !== null)
          return l.flags |= 128, vc(l), null;
        break;
      case 13:
        var u = l.memoizedState;
        if (u !== null)
          return u.dehydrated !== null ? (Ne(l), l.flags |= 128, null) : (e & l.child.childLanes) !== 0 ? ho(t, l, e) : (Ne(l), t = ae(
            t,
            l,
            e
          ), t !== null ? t.sibling : null);
        Ne(l);
        break;
      case 19:
        var a = (t.flags & 128) !== 0;
        if (u = (e & l.childLanes) !== 0, u || (Du(
          t,
          l,
          e,
          !1
        ), u = (e & l.childLanes) !== 0), a) {
          if (u)
            return go(
              t,
              l,
              e
            );
          l.flags |= 128;
        }
        if (a = l.memoizedState, a !== null && (a.rendering = null, a.tail = null, a.lastEffect = null), R(Dt, Dt.current), u) break;
        return null;
      case 22:
        return l.lanes = 0, fo(
          t,
          l,
          e,
          l.pendingProps
        );
      case 24:
        Ae(l, Rt, t.memoizedState.cache);
    }
    return ae(t, l, e);
  }
  function bo(t, l, e) {
    if (t !== null)
      if (t.memoizedProps !== l.pendingProps)
        Bt = !0;
      else {
        if (!Zc(t, e) && (l.flags & 128) === 0)
          return Bt = !1, Cv(
            t,
            l,
            e
          );
        Bt = (t.flags & 131072) !== 0;
      }
    else
      Bt = !1, ot && (l.flags & 1048576) !== 0 && $s(l, ba, l.index);
    switch (l.lanes = 0, l.tag) {
      case 16:
        t: {
          var u = l.pendingProps;
          if (t = uu(l.elementType), l.type = t, typeof t == "function")
            ki(t) ? (u = cu(t, u), l.tag = 1, l = yo(
              null,
              l,
              t,
              u,
              e
            )) : (l.tag = 0, l = Hc(
              null,
              l,
              t,
              u,
              e
            ));
          else {
            if (t != null) {
              var a = t.$$typeof;
              if (a === dt) {
                l.tag = 11, l = no(
                  null,
                  l,
                  t,
                  u,
                  e
                );
                break t;
              } else if (a === L) {
                l.tag = 14, l = io(
                  null,
                  l,
                  t,
                  u,
                  e
                );
                break t;
              }
            }
            throw l = zl(t) || t, Error(s(306, l, ""));
          }
        }
        return l;
      case 0:
        return Hc(
          t,
          l,
          l.type,
          l.pendingProps,
          e
        );
      case 1:
        return u = l.type, a = cu(
          u,
          l.pendingProps
        ), yo(
          t,
          l,
          u,
          a,
          e
        );
      case 3:
        t: {
          if (wt(
            l,
            l.stateNode.containerInfo
          ), t === null) throw Error(s(387));
          u = l.pendingProps;
          var n = l.memoizedState;
          a = n.element, sc(t, l), Ta(l, u, null, e);
          var i = l.memoizedState;
          if (u = i.cache, Ae(l, Rt, u), u !== n.cache && uc(
            l,
            [Rt],
            e,
            !0
          ), qa(), u = i.element, n.isDehydrated)
            if (n = {
              element: u,
              isDehydrated: !1,
              cache: i.cache
            }, l.updateQueue.baseState = n, l.memoizedState = n, l.flags & 256) {
              l = vo(
                t,
                l,
                u,
                e
              );
              break t;
            } else if (u !== a) {
              a = xl(
                Error(s(424)),
                l
              ), Sa(a), l = vo(
                t,
                l,
                u,
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
              for (Tt = Ul(t.firstChild), $t = l, ot = !0, Ee = null, Ml = !0, e = fr(
                l,
                null,
                u,
                e
              ), l.child = e; e; )
                e.flags = e.flags & -3 | 4096, e = e.sibling;
            }
          else {
            if (Pe(), u === a) {
              l = ae(
                t,
                l,
                e
              );
              break t;
            }
            Ft(t, l, u, e);
          }
          l = l.child;
        }
        return l;
      case 26:
        return Qn(t, l), t === null ? (e = Od(
          l.type,
          null,
          l.pendingProps,
          null
        )) ? l.memoizedState = e : ot || (e = l.type, t = l.pendingProps, u = ai(
          at.current
        ).createElement(e), u[kt] = l, u[nl] = t, It(u, e, t), Zt(u), l.stateNode = u) : l.memoizedState = Od(
          l.type,
          t.memoizedProps,
          l.pendingProps,
          t.memoizedState
        ), null;
      case 27:
        return me(l), t === null && ot && (u = l.stateNode = Td(
          l.type,
          l.pendingProps,
          at.current
        ), $t = l, Ml = !0, a = Tt, Be(l.type) ? (zf = a, Tt = Ul(u.firstChild)) : Tt = a), Ft(
          t,
          l,
          l.pendingProps.children,
          e
        ), Qn(t, l), t === null && (l.flags |= 4194304), l.child;
      case 5:
        return t === null && ot && ((a = u = Tt) && (u = sh(
          u,
          l.type,
          l.pendingProps,
          Ml
        ), u !== null ? (l.stateNode = u, $t = l, Tt = Ul(u.firstChild), Ml = !1, a = !0) : a = !1), a || ze(l)), me(l), a = l.type, n = l.pendingProps, i = t !== null ? t.memoizedProps : null, u = n.children, bf(a, n) ? u = null : i !== null && bf(a, i) && (l.flags |= 32), l.memoizedState !== null && (a = mc(
          t,
          l,
          Av,
          null,
          null,
          e
        ), Ja._currentValue = a), Qn(t, l), Ft(t, l, u, e), l.child;
      case 6:
        return t === null && ot && ((t = e = Tt) && (e = rh(
          e,
          l.pendingProps,
          Ml
        ), e !== null ? (l.stateNode = e, $t = l, Tt = null, t = !0) : t = !1), t || ze(l)), null;
      case 13:
        return ho(t, l, e);
      case 4:
        return wt(
          l,
          l.stateNode.containerInfo
        ), u = l.pendingProps, t === null ? l.child = nu(
          l,
          null,
          u,
          e
        ) : Ft(t, l, u, e), l.child;
      case 11:
        return no(
          t,
          l,
          l.type,
          l.pendingProps,
          e
        );
      case 7:
        return Ft(
          t,
          l,
          l.pendingProps,
          e
        ), l.child;
      case 8:
        return Ft(
          t,
          l,
          l.pendingProps.children,
          e
        ), l.child;
      case 12:
        return Ft(
          t,
          l,
          l.pendingProps.children,
          e
        ), l.child;
      case 10:
        return u = l.pendingProps, Ae(l, l.type, u.value), Ft(t, l, u.children, e), l.child;
      case 9:
        return a = l.type._context, u = l.pendingProps.children, lu(l), a = Wt(a), u = u(a), l.flags |= 1, Ft(t, l, u, e), l.child;
      case 14:
        return io(
          t,
          l,
          l.type,
          l.pendingProps,
          e
        );
      case 15:
        return co(
          t,
          l,
          l.type,
          l.pendingProps,
          e
        );
      case 19:
        return go(t, l, e);
      case 31:
        return Uv(t, l, e);
      case 22:
        return fo(
          t,
          l,
          e,
          l.pendingProps
        );
      case 24:
        return lu(l), u = Wt(Rt), t === null ? (a = ic(), a === null && (a = At, n = ac(), a.pooledCache = n, n.refCount++, n !== null && (a.pooledCacheLanes |= e), a = n), l.memoizedState = { parent: u, cache: a }, fc(l), Ae(l, Rt, a)) : ((t.lanes & e) !== 0 && (sc(t, l), Ta(l, null, null, e), qa()), a = t.memoizedState, n = l.memoizedState, a.parent !== u ? (a = { parent: u, cache: u }, l.memoizedState = a, l.lanes === 0 && (l.memoizedState = l.updateQueue.baseState = a), Ae(l, Rt, u)) : (u = n.cache, Ae(l, Rt, u), u !== a.cache && uc(
          l,
          [Rt],
          e,
          !0
        ))), Ft(
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
  function ne(t) {
    t.flags |= 4;
  }
  function Vc(t, l, e, u, a) {
    if ((l = (t.mode & 32) !== 0) && (l = !1), l) {
      if (t.flags |= 16777216, (a & 335544128) === a)
        if (t.stateNode.complete) t.flags |= 8192;
        else if (Ko()) t.flags |= 8192;
        else
          throw au = Tn, cc;
    } else t.flags &= -16777217;
  }
  function So(t, l) {
    if (l.type !== "stylesheet" || (l.state.loading & 4) !== 0)
      t.flags &= -16777217;
    else if (t.flags |= 16777216, !jd(l))
      if (Ko()) t.flags |= 8192;
      else
        throw au = Tn, cc;
  }
  function Zn(t, l) {
    l !== null && (t.flags |= 4), t.flags & 16384 && (l = t.tag !== 22 ? Ff() : 536870912, t.lanes |= l, Zu |= l);
  }
  function Ua(t, l) {
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
          for (var u = null; e !== null; )
            e.alternate !== null && (u = e), e = e.sibling;
          u === null ? l || t.tail === null ? t.tail = null : t.tail.sibling = null : u.sibling = null;
      }
  }
  function xt(t) {
    var l = t.alternate !== null && t.alternate.child === t.child, e = 0, u = 0;
    if (l)
      for (var a = t.child; a !== null; )
        e |= a.lanes | a.childLanes, u |= a.subtreeFlags & 65011712, u |= a.flags & 65011712, a.return = t, a = a.sibling;
    else
      for (a = t.child; a !== null; )
        e |= a.lanes | a.childLanes, u |= a.subtreeFlags, u |= a.flags, a.return = t, a = a.sibling;
    return t.subtreeFlags |= u, t.childLanes = e, l;
  }
  function jv(t, l, e) {
    var u = l.pendingProps;
    switch (Ii(l), l.tag) {
      case 16:
      case 15:
      case 0:
      case 11:
      case 7:
      case 8:
      case 12:
      case 9:
      case 14:
        return xt(l), null;
      case 1:
        return xt(l), null;
      case 3:
        return e = l.stateNode, u = null, t !== null && (u = t.memoizedState.cache), l.memoizedState.cache !== u && (l.flags |= 2048), le(Rt), qt(), e.pendingContext && (e.context = e.pendingContext, e.pendingContext = null), (t === null || t.child === null) && (Mu(l) ? ne(l) : t === null || t.memoizedState.isDehydrated && (l.flags & 256) === 0 || (l.flags |= 1024, tc())), xt(l), null;
      case 26:
        var a = l.type, n = l.memoizedState;
        return t === null ? (ne(l), n !== null ? (xt(l), So(l, n)) : (xt(l), Vc(
          l,
          a,
          null,
          u,
          e
        ))) : n ? n !== t.memoizedState ? (ne(l), xt(l), So(l, n)) : (xt(l), l.flags &= -16777217) : (t = t.memoizedProps, t !== u && ne(l), xt(l), Vc(
          l,
          a,
          t,
          u,
          e
        )), null;
      case 27:
        if (Gl(l), e = at.current, a = l.type, t !== null && l.stateNode != null)
          t.memoizedProps !== u && ne(l);
        else {
          if (!u) {
            if (l.stateNode === null)
              throw Error(s(166));
            return xt(l), null;
          }
          t = Y.current, Mu(l) ? Fs(l) : (t = Td(a, u, e), l.stateNode = t, ne(l));
        }
        return xt(l), null;
      case 5:
        if (Gl(l), a = l.type, t !== null && l.stateNode != null)
          t.memoizedProps !== u && ne(l);
        else {
          if (!u) {
            if (l.stateNode === null)
              throw Error(s(166));
            return xt(l), null;
          }
          if (n = Y.current, Mu(l))
            Fs(l);
          else {
            var i = ai(
              at.current
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
            n[kt] = l, n[nl] = u;
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
            t: switch (It(n, a, u), a) {
              case "button":
              case "input":
              case "select":
              case "textarea":
                u = !!u.autoFocus;
                break t;
              case "img":
                u = !0;
                break t;
              default:
                u = !1;
            }
            u && ne(l);
          }
        }
        return xt(l), Vc(
          l,
          l.type,
          t === null ? null : t.memoizedProps,
          l.pendingProps,
          e
        ), null;
      case 6:
        if (t && l.stateNode != null)
          t.memoizedProps !== u && ne(l);
        else {
          if (typeof u != "string" && l.stateNode === null)
            throw Error(s(166));
          if (t = at.current, Mu(l)) {
            if (t = l.stateNode, e = l.memoizedProps, u = null, a = $t, a !== null)
              switch (a.tag) {
                case 27:
                case 5:
                  u = a.memoizedProps;
              }
            t[kt] = l, t = !!(t.nodeValue === e || u !== null && u.suppressHydrationWarning === !0 || hd(t.nodeValue, e)), t || ze(l, !0);
          } else
            t = ai(t).createTextNode(
              u
            ), t[kt] = l, l.stateNode = t;
        }
        return xt(l), null;
      case 31:
        if (e = l.memoizedState, t === null || t.memoizedState !== null) {
          if (u = Mu(l), e !== null) {
            if (t === null) {
              if (!u) throw Error(s(318));
              if (t = l.memoizedState, t = t !== null ? t.dehydrated : null, !t) throw Error(s(557));
              t[kt] = l;
            } else
              Pe(), (l.flags & 128) === 0 && (l.memoizedState = null), l.flags |= 4;
            xt(l), t = !1;
          } else
            e = tc(), t !== null && t.memoizedState !== null && (t.memoizedState.hydrationErrors = e), t = !0;
          if (!t)
            return l.flags & 256 ? (Sl(l), l) : (Sl(l), null);
          if ((l.flags & 128) !== 0)
            throw Error(s(558));
        }
        return xt(l), null;
      case 13:
        if (u = l.memoizedState, t === null || t.memoizedState !== null && t.memoizedState.dehydrated !== null) {
          if (a = Mu(l), u !== null && u.dehydrated !== null) {
            if (t === null) {
              if (!a) throw Error(s(318));
              if (a = l.memoizedState, a = a !== null ? a.dehydrated : null, !a) throw Error(s(317));
              a[kt] = l;
            } else
              Pe(), (l.flags & 128) === 0 && (l.memoizedState = null), l.flags |= 4;
            xt(l), a = !1;
          } else
            a = tc(), t !== null && t.memoizedState !== null && (t.memoizedState.hydrationErrors = a), a = !0;
          if (!a)
            return l.flags & 256 ? (Sl(l), l) : (Sl(l), null);
        }
        return Sl(l), (l.flags & 128) !== 0 ? (l.lanes = e, l) : (e = u !== null, t = t !== null && t.memoizedState !== null, e && (u = l.child, a = null, u.alternate !== null && u.alternate.memoizedState !== null && u.alternate.memoizedState.cachePool !== null && (a = u.alternate.memoizedState.cachePool.pool), n = null, u.memoizedState !== null && u.memoizedState.cachePool !== null && (n = u.memoizedState.cachePool.pool), n !== a && (u.flags |= 2048)), e !== t && e && (l.child.flags |= 8192), Zn(l, l.updateQueue), xt(l), null);
      case 4:
        return qt(), t === null && yf(l.stateNode.containerInfo), xt(l), null;
      case 10:
        return le(l.type), xt(l), null;
      case 19:
        if (N(Dt), u = l.memoizedState, u === null) return xt(l), null;
        if (a = (l.flags & 128) !== 0, n = u.rendering, n === null)
          if (a) Ua(u, !1);
          else {
            if (Mt !== 0 || t !== null && (t.flags & 128) !== 0)
              for (t = l.child; t !== null; ) {
                if (n = Mn(t), n !== null) {
                  for (l.flags |= 128, Ua(u, !1), t = n.updateQueue, l.updateQueue = t, Zn(l, t), l.subtreeFlags = 0, t = e, e = l.child; e !== null; )
                    Js(e, t), e = e.sibling;
                  return R(
                    Dt,
                    Dt.current & 1 | 2
                  ), ot && Pl(l, u.treeForkCount), l.child;
                }
                t = t.sibling;
              }
            u.tail !== null && yl() > kn && (l.flags |= 128, a = !0, Ua(u, !1), l.lanes = 4194304);
          }
        else {
          if (!a)
            if (t = Mn(n), t !== null) {
              if (l.flags |= 128, a = !0, t = t.updateQueue, l.updateQueue = t, Zn(l, t), Ua(u, !0), u.tail === null && u.tailMode === "hidden" && !n.alternate && !ot)
                return xt(l), null;
            } else
              2 * yl() - u.renderingStartTime > kn && e !== 536870912 && (l.flags |= 128, a = !0, Ua(u, !1), l.lanes = 4194304);
          u.isBackwards ? (n.sibling = l.child, l.child = n) : (t = u.last, t !== null ? t.sibling = n : l.child = n, u.last = n);
        }
        return u.tail !== null ? (t = u.tail, u.rendering = t, u.tail = t.sibling, u.renderingStartTime = yl(), t.sibling = null, e = Dt.current, R(
          Dt,
          a ? e & 1 | 2 : e & 1
        ), ot && Pl(l, u.treeForkCount), t) : (xt(l), null);
      case 22:
      case 23:
        return Sl(l), yc(), u = l.memoizedState !== null, t !== null ? t.memoizedState !== null !== u && (l.flags |= 8192) : u && (l.flags |= 8192), u ? (e & 536870912) !== 0 && (l.flags & 128) === 0 && (xt(l), l.subtreeFlags & 6 && (l.flags |= 8192)) : xt(l), e = l.updateQueue, e !== null && Zn(l, e.retryQueue), e = null, t !== null && t.memoizedState !== null && t.memoizedState.cachePool !== null && (e = t.memoizedState.cachePool.pool), u = null, l.memoizedState !== null && l.memoizedState.cachePool !== null && (u = l.memoizedState.cachePool.pool), u !== e && (l.flags |= 2048), t !== null && N(eu), null;
      case 24:
        return e = null, t !== null && (e = t.memoizedState.cache), l.memoizedState.cache !== e && (l.flags |= 2048), le(Rt), xt(l), null;
      case 25:
        return null;
      case 30:
        return null;
    }
    throw Error(s(156, l.tag));
  }
  function Rv(t, l) {
    switch (Ii(l), l.tag) {
      case 1:
        return t = l.flags, t & 65536 ? (l.flags = t & -65537 | 128, l) : null;
      case 3:
        return le(Rt), qt(), t = l.flags, (t & 65536) !== 0 && (t & 128) === 0 ? (l.flags = t & -65537 | 128, l) : null;
      case 26:
      case 27:
      case 5:
        return Gl(l), null;
      case 31:
        if (l.memoizedState !== null) {
          if (Sl(l), l.alternate === null)
            throw Error(s(340));
          Pe();
        }
        return t = l.flags, t & 65536 ? (l.flags = t & -65537 | 128, l) : null;
      case 13:
        if (Sl(l), t = l.memoizedState, t !== null && t.dehydrated !== null) {
          if (l.alternate === null)
            throw Error(s(340));
          Pe();
        }
        return t = l.flags, t & 65536 ? (l.flags = t & -65537 | 128, l) : null;
      case 19:
        return N(Dt), null;
      case 4:
        return qt(), null;
      case 10:
        return le(l.type), null;
      case 22:
      case 23:
        return Sl(l), yc(), t !== null && N(eu), t = l.flags, t & 65536 ? (l.flags = t & -65537 | 128, l) : null;
      case 24:
        return le(Rt), null;
      case 25:
        return null;
      default:
        return null;
    }
  }
  function po(t, l) {
    switch (Ii(l), l.tag) {
      case 3:
        le(Rt), qt();
        break;
      case 26:
      case 27:
      case 5:
        Gl(l);
        break;
      case 4:
        qt();
        break;
      case 31:
        l.memoizedState !== null && Sl(l);
        break;
      case 13:
        Sl(l);
        break;
      case 19:
        N(Dt);
        break;
      case 10:
        le(l.type);
        break;
      case 22:
      case 23:
        Sl(l), yc(), t !== null && N(eu);
        break;
      case 24:
        le(Rt);
    }
  }
  function Ca(t, l) {
    try {
      var e = l.updateQueue, u = e !== null ? e.lastEffect : null;
      if (u !== null) {
        var a = u.next;
        e = a;
        do {
          if ((e.tag & t) === t) {
            u = void 0;
            var n = e.create, i = e.inst;
            u = n(), i.destroy = u;
          }
          e = e.next;
        } while (e !== a);
      }
    } catch (f) {
      pt(l, l.return, f);
    }
  }
  function Me(t, l, e) {
    try {
      var u = l.updateQueue, a = u !== null ? u.lastEffect : null;
      if (a !== null) {
        var n = a.next;
        u = n;
        do {
          if ((u.tag & t) === t) {
            var i = u.inst, f = i.destroy;
            if (f !== void 0) {
              i.destroy = void 0, a = l;
              var d = e, g = f;
              try {
                g();
              } catch (z) {
                pt(
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
      pt(l, l.return, z);
    }
  }
  function _o(t) {
    var l = t.updateQueue;
    if (l !== null) {
      var e = t.stateNode;
      try {
        rr(l, e);
      } catch (u) {
        pt(t, t.return, u);
      }
    }
  }
  function Eo(t, l, e) {
    e.props = cu(
      t.type,
      t.memoizedProps
    ), e.state = t.memoizedState;
    try {
      e.componentWillUnmount();
    } catch (u) {
      pt(t, l, u);
    }
  }
  function ja(t, l) {
    try {
      var e = t.ref;
      if (e !== null) {
        switch (t.tag) {
          case 26:
          case 27:
          case 5:
            var u = t.stateNode;
            break;
          case 30:
            u = t.stateNode;
            break;
          default:
            u = t.stateNode;
        }
        typeof e == "function" ? t.refCleanup = e(u) : e.current = u;
      }
    } catch (a) {
      pt(t, l, a);
    }
  }
  function Zl(t, l) {
    var e = t.ref, u = t.refCleanup;
    if (e !== null)
      if (typeof u == "function")
        try {
          u();
        } catch (a) {
          pt(t, l, a);
        } finally {
          t.refCleanup = null, t = t.alternate, t != null && (t.refCleanup = null);
        }
      else if (typeof e == "function")
        try {
          e(null);
        } catch (a) {
          pt(t, l, a);
        }
      else e.current = null;
  }
  function zo(t) {
    var l = t.type, e = t.memoizedProps, u = t.stateNode;
    try {
      t: switch (l) {
        case "button":
        case "input":
        case "select":
        case "textarea":
          e.autoFocus && u.focus();
          break t;
        case "img":
          e.src ? u.src = e.src : e.srcSet && (u.srcset = e.srcSet);
      }
    } catch (a) {
      pt(t, t.return, a);
    }
  }
  function Kc(t, l, e) {
    try {
      var u = t.stateNode;
      uh(u, t.type, e, l), u[nl] = l;
    } catch (a) {
      pt(t, t.return, a);
    }
  }
  function Ao(t) {
    return t.tag === 5 || t.tag === 3 || t.tag === 26 || t.tag === 27 && Be(t.type) || t.tag === 4;
  }
  function Jc(t) {
    t: for (; ; ) {
      for (; t.sibling === null; ) {
        if (t.return === null || Ao(t.return)) return null;
        t = t.return;
      }
      for (t.sibling.return = t.return, t = t.sibling; t.tag !== 5 && t.tag !== 6 && t.tag !== 18; ) {
        if (t.tag === 27 && Be(t.type) || t.flags & 2 || t.child === null || t.tag === 4) continue t;
        t.child.return = t, t = t.child;
      }
      if (!(t.flags & 2)) return t.stateNode;
    }
  }
  function wc(t, l, e) {
    var u = t.tag;
    if (u === 5 || u === 6)
      t = t.stateNode, l ? (e.nodeType === 9 ? e.body : e.nodeName === "HTML" ? e.ownerDocument.body : e).insertBefore(t, l) : (l = e.nodeType === 9 ? e.body : e.nodeName === "HTML" ? e.ownerDocument.body : e, l.appendChild(t), e = e._reactRootContainer, e != null || l.onclick !== null || (l.onclick = Wl));
    else if (u !== 4 && (u === 27 && Be(t.type) && (e = t.stateNode, l = null), t = t.child, t !== null))
      for (wc(t, l, e), t = t.sibling; t !== null; )
        wc(t, l, e), t = t.sibling;
  }
  function Vn(t, l, e) {
    var u = t.tag;
    if (u === 5 || u === 6)
      t = t.stateNode, l ? e.insertBefore(t, l) : e.appendChild(t);
    else if (u !== 4 && (u === 27 && Be(t.type) && (e = t.stateNode), t = t.child, t !== null))
      for (Vn(t, l, e), t = t.sibling; t !== null; )
        Vn(t, l, e), t = t.sibling;
  }
  function qo(t) {
    var l = t.stateNode, e = t.memoizedProps;
    try {
      for (var u = t.type, a = l.attributes; a.length; )
        l.removeAttributeNode(a[0]);
      It(l, u, e), l[kt] = t, l[nl] = e;
    } catch (n) {
      pt(t, t.return, n);
    }
  }
  var ie = !1, Yt = !1, kc = !1, To = typeof WeakSet == "function" ? WeakSet : Set, Vt = null;
  function Hv(t, l) {
    if (t = t.containerInfo, mf = oi, t = Bs(t), Qi(t)) {
      if ("selectionStart" in t)
        var e = {
          start: t.selectionStart,
          end: t.selectionEnd
        };
      else
        t: {
          e = (e = t.ownerDocument) && e.defaultView || window;
          var u = e.getSelection && e.getSelection();
          if (u && u.rangeCount !== 0) {
            e = u.anchorNode;
            var a = u.anchorOffset, n = u.focusNode;
            u = u.focusOffset;
            try {
              e.nodeType, n.nodeType;
            } catch {
              e = null;
              break t;
            }
            var i = 0, f = -1, d = -1, g = 0, z = 0, M = t, S = null;
            l: for (; ; ) {
              for (var _; M !== e || a !== 0 && M.nodeType !== 3 || (f = i + a), M !== n || u !== 0 && M.nodeType !== 3 || (d = i + u), M.nodeType === 3 && (i += M.nodeValue.length), (_ = M.firstChild) !== null; )
                S = M, M = _;
              for (; ; ) {
                if (M === t) break l;
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
    for (gf = { focusedElem: t, selectionRange: e }, oi = !1, Vt = l; Vt !== null; )
      if (l = Vt, t = l.child, (l.subtreeFlags & 1028) !== 0 && t !== null)
        t.return = l, Vt = t;
      else
        for (; Vt !== null; ) {
          switch (l = Vt, n = l.alternate, t = l.flags, l.tag) {
            case 0:
              if ((t & 4) !== 0 && (t = l.updateQueue, t = t !== null ? t.events : null, t !== null))
                for (e = 0; e < t.length; e++)
                  a = t[e], a.ref.impl = a.nextImpl;
              break;
            case 11:
            case 15:
              break;
            case 1:
              if ((t & 1024) !== 0 && n !== null) {
                t = void 0, e = l, a = n.memoizedProps, n = n.memoizedState, u = e.stateNode;
                try {
                  var G = cu(
                    e.type,
                    a
                  );
                  t = u.getSnapshotBeforeUpdate(
                    G,
                    n
                  ), u.__reactInternalSnapshotBeforeUpdate = t;
                } catch (J) {
                  pt(
                    e,
                    e.return,
                    J
                  );
                }
              }
              break;
            case 3:
              if ((t & 1024) !== 0) {
                if (t = l.stateNode.containerInfo, e = t.nodeType, e === 9)
                  pf(t);
                else if (e === 1)
                  switch (t.nodeName) {
                    case "HEAD":
                    case "HTML":
                    case "BODY":
                      pf(t);
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
            t.return = l.return, Vt = t;
            break;
          }
          Vt = l.return;
        }
  }
  function xo(t, l, e) {
    var u = e.flags;
    switch (e.tag) {
      case 0:
      case 11:
      case 15:
        fe(t, e), u & 4 && Ca(5, e);
        break;
      case 1:
        if (fe(t, e), u & 4)
          if (t = e.stateNode, l === null)
            try {
              t.componentDidMount();
            } catch (i) {
              pt(e, e.return, i);
            }
          else {
            var a = cu(
              e.type,
              l.memoizedProps
            );
            l = l.memoizedState;
            try {
              t.componentDidUpdate(
                a,
                l,
                t.__reactInternalSnapshotBeforeUpdate
              );
            } catch (i) {
              pt(
                e,
                e.return,
                i
              );
            }
          }
        u & 64 && _o(e), u & 512 && ja(e, e.return);
        break;
      case 3:
        if (fe(t, e), u & 64 && (t = e.updateQueue, t !== null)) {
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
            rr(t, l);
          } catch (i) {
            pt(e, e.return, i);
          }
        }
        break;
      case 27:
        l === null && u & 4 && qo(e);
      case 26:
      case 5:
        fe(t, e), l === null && u & 4 && zo(e), u & 512 && ja(e, e.return);
        break;
      case 12:
        fe(t, e);
        break;
      case 31:
        fe(t, e), u & 4 && Mo(t, e);
        break;
      case 13:
        fe(t, e), u & 4 && Do(t, e), u & 64 && (t = e.memoizedState, t !== null && (t = t.dehydrated, t !== null && (e = Kv.bind(
          null,
          e
        ), oh(t, e))));
        break;
      case 22:
        if (u = e.memoizedState !== null || ie, !u) {
          l = l !== null && l.memoizedState !== null || Yt, a = ie;
          var n = Yt;
          ie = u, (Yt = l) && !n ? se(
            t,
            e,
            (e.subtreeFlags & 8772) !== 0
          ) : fe(t, e), ie = a, Yt = n;
        }
        break;
      case 30:
        break;
      default:
        fe(t, e);
    }
  }
  function No(t) {
    var l = t.alternate;
    l !== null && (t.alternate = null, No(l)), t.child = null, t.deletions = null, t.sibling = null, t.tag === 5 && (l = t.stateNode, l !== null && Ai(l)), t.stateNode = null, t.return = null, t.dependencies = null, t.memoizedProps = null, t.memoizedState = null, t.pendingProps = null, t.stateNode = null, t.updateQueue = null;
  }
  var Nt = null, cl = !1;
  function ce(t, l, e) {
    for (e = e.child; e !== null; )
      Oo(t, l, e), e = e.sibling;
  }
  function Oo(t, l, e) {
    if (vl && typeof vl.onCommitFiberUnmount == "function")
      try {
        vl.onCommitFiberUnmount(aa, e);
      } catch {
      }
    switch (e.tag) {
      case 26:
        Yt || Zl(e, l), ce(
          t,
          l,
          e
        ), e.memoizedState ? e.memoizedState.count-- : e.stateNode && (e = e.stateNode, e.parentNode.removeChild(e));
        break;
      case 27:
        Yt || Zl(e, l);
        var u = Nt, a = cl;
        Be(e.type) && (Nt = e.stateNode, cl = !1), ce(
          t,
          l,
          e
        ), Za(e.stateNode), Nt = u, cl = a;
        break;
      case 5:
        Yt || Zl(e, l);
      case 6:
        if (u = Nt, a = cl, Nt = null, ce(
          t,
          l,
          e
        ), Nt = u, cl = a, Nt !== null)
          if (cl)
            try {
              (Nt.nodeType === 9 ? Nt.body : Nt.nodeName === "HTML" ? Nt.ownerDocument.body : Nt).removeChild(e.stateNode);
            } catch (n) {
              pt(
                e,
                l,
                n
              );
            }
          else
            try {
              Nt.removeChild(e.stateNode);
            } catch (n) {
              pt(
                e,
                l,
                n
              );
            }
        break;
      case 18:
        Nt !== null && (cl ? (t = Nt, _d(
          t.nodeType === 9 ? t.body : t.nodeName === "HTML" ? t.ownerDocument.body : t,
          e.stateNode
        ), Fu(t)) : _d(Nt, e.stateNode));
        break;
      case 4:
        u = Nt, a = cl, Nt = e.stateNode.containerInfo, cl = !0, ce(
          t,
          l,
          e
        ), Nt = u, cl = a;
        break;
      case 0:
      case 11:
      case 14:
      case 15:
        Me(2, e, l), Yt || Me(4, e, l), ce(
          t,
          l,
          e
        );
        break;
      case 1:
        Yt || (Zl(e, l), u = e.stateNode, typeof u.componentWillUnmount == "function" && Eo(
          e,
          l,
          u
        )), ce(
          t,
          l,
          e
        );
        break;
      case 21:
        ce(
          t,
          l,
          e
        );
        break;
      case 22:
        Yt = (u = Yt) || e.memoizedState !== null, ce(
          t,
          l,
          e
        ), Yt = u;
        break;
      default:
        ce(
          t,
          l,
          e
        );
    }
  }
  function Mo(t, l) {
    if (l.memoizedState === null && (t = l.alternate, t !== null && (t = t.memoizedState, t !== null))) {
      t = t.dehydrated;
      try {
        Fu(t);
      } catch (e) {
        pt(l, l.return, e);
      }
    }
  }
  function Do(t, l) {
    if (l.memoizedState === null && (t = l.alternate, t !== null && (t = t.memoizedState, t !== null && (t = t.dehydrated, t !== null))))
      try {
        Fu(t);
      } catch (e) {
        pt(l, l.return, e);
      }
  }
  function Bv(t) {
    switch (t.tag) {
      case 31:
      case 13:
      case 19:
        var l = t.stateNode;
        return l === null && (l = t.stateNode = new To()), l;
      case 22:
        return t = t.stateNode, l = t._retryCache, l === null && (l = t._retryCache = new To()), l;
      default:
        throw Error(s(435, t.tag));
    }
  }
  function Kn(t, l) {
    var e = Bv(t);
    l.forEach(function(u) {
      if (!e.has(u)) {
        e.add(u);
        var a = Jv.bind(null, t, u);
        u.then(a, a);
      }
    });
  }
  function fl(t, l) {
    var e = l.deletions;
    if (e !== null)
      for (var u = 0; u < e.length; u++) {
        var a = e[u], n = t, i = l, f = i;
        t: for (; f !== null; ) {
          switch (f.tag) {
            case 27:
              if (Be(f.type)) {
                Nt = f.stateNode, cl = !1;
                break t;
              }
              break;
            case 5:
              Nt = f.stateNode, cl = !1;
              break t;
            case 3:
            case 4:
              Nt = f.stateNode.containerInfo, cl = !0;
              break t;
          }
          f = f.return;
        }
        if (Nt === null) throw Error(s(160));
        Oo(n, i, a), Nt = null, cl = !1, n = a.alternate, n !== null && (n.return = null), a.return = null;
      }
    if (l.subtreeFlags & 13886)
      for (l = l.child; l !== null; )
        Uo(l, t), l = l.sibling;
  }
  var Hl = null;
  function Uo(t, l) {
    var e = t.alternate, u = t.flags;
    switch (t.tag) {
      case 0:
      case 11:
      case 14:
      case 15:
        fl(l, t), sl(t), u & 4 && (Me(3, t, t.return), Ca(3, t), Me(5, t, t.return));
        break;
      case 1:
        fl(l, t), sl(t), u & 512 && (Yt || e === null || Zl(e, e.return)), u & 64 && ie && (t = t.updateQueue, t !== null && (u = t.callbacks, u !== null && (e = t.shared.hiddenCallbacks, t.shared.hiddenCallbacks = e === null ? u : e.concat(u))));
        break;
      case 26:
        var a = Hl;
        if (fl(l, t), sl(t), u & 512 && (Yt || e === null || Zl(e, e.return)), u & 4) {
          var n = e !== null ? e.memoizedState : null;
          if (u = t.memoizedState, e === null)
            if (u === null)
              if (t.stateNode === null) {
                t: {
                  u = t.type, e = t.memoizedProps, a = a.ownerDocument || a;
                  l: switch (u) {
                    case "title":
                      n = a.getElementsByTagName("title")[0], (!n || n[ca] || n[kt] || n.namespaceURI === "http://www.w3.org/2000/svg" || n.hasAttribute("itemprop")) && (n = a.createElement(u), a.head.insertBefore(
                        n,
                        a.querySelector("head > title")
                      )), It(n, u, e), n[kt] = t, Zt(n), u = n;
                      break t;
                    case "link":
                      var i = Ud(
                        "link",
                        "href",
                        a
                      ).get(u + (e.href || ""));
                      if (i) {
                        for (var f = 0; f < i.length; f++)
                          if (n = i[f], n.getAttribute("href") === (e.href == null || e.href === "" ? null : e.href) && n.getAttribute("rel") === (e.rel == null ? null : e.rel) && n.getAttribute("title") === (e.title == null ? null : e.title) && n.getAttribute("crossorigin") === (e.crossOrigin == null ? null : e.crossOrigin)) {
                            i.splice(f, 1);
                            break l;
                          }
                      }
                      n = a.createElement(u), It(n, u, e), a.head.appendChild(n);
                      break;
                    case "meta":
                      if (i = Ud(
                        "meta",
                        "content",
                        a
                      ).get(u + (e.content || ""))) {
                        for (f = 0; f < i.length; f++)
                          if (n = i[f], n.getAttribute("content") === (e.content == null ? null : "" + e.content) && n.getAttribute("name") === (e.name == null ? null : e.name) && n.getAttribute("property") === (e.property == null ? null : e.property) && n.getAttribute("http-equiv") === (e.httpEquiv == null ? null : e.httpEquiv) && n.getAttribute("charset") === (e.charSet == null ? null : e.charSet)) {
                            i.splice(f, 1);
                            break l;
                          }
                      }
                      n = a.createElement(u), It(n, u, e), a.head.appendChild(n);
                      break;
                    default:
                      throw Error(s(468, u));
                  }
                  n[kt] = t, Zt(n), u = n;
                }
                t.stateNode = u;
              } else
                Cd(
                  a,
                  t.type,
                  t.stateNode
                );
            else
              t.stateNode = Dd(
                a,
                u,
                t.memoizedProps
              );
          else
            n !== u ? (n === null ? e.stateNode !== null && (e = e.stateNode, e.parentNode.removeChild(e)) : n.count--, u === null ? Cd(
              a,
              t.type,
              t.stateNode
            ) : Dd(
              a,
              u,
              t.memoizedProps
            )) : u === null && t.stateNode !== null && Kc(
              t,
              t.memoizedProps,
              e.memoizedProps
            );
        }
        break;
      case 27:
        fl(l, t), sl(t), u & 512 && (Yt || e === null || Zl(e, e.return)), e !== null && u & 4 && Kc(
          t,
          t.memoizedProps,
          e.memoizedProps
        );
        break;
      case 5:
        if (fl(l, t), sl(t), u & 512 && (Yt || e === null || Zl(e, e.return)), t.flags & 32) {
          a = t.stateNode;
          try {
            pu(a, "");
          } catch (G) {
            pt(t, t.return, G);
          }
        }
        u & 4 && t.stateNode != null && (a = t.memoizedProps, Kc(
          t,
          a,
          e !== null ? e.memoizedProps : a
        )), u & 1024 && (kc = !0);
        break;
      case 6:
        if (fl(l, t), sl(t), u & 4) {
          if (t.stateNode === null)
            throw Error(s(162));
          u = t.memoizedProps, e = t.stateNode;
          try {
            e.nodeValue = u;
          } catch (G) {
            pt(t, t.return, G);
          }
        }
        break;
      case 3:
        if (ci = null, a = Hl, Hl = ni(l.containerInfo), fl(l, t), Hl = a, sl(t), u & 4 && e !== null && e.memoizedState.isDehydrated)
          try {
            Fu(l.containerInfo);
          } catch (G) {
            pt(t, t.return, G);
          }
        kc && (kc = !1, Co(t));
        break;
      case 4:
        u = Hl, Hl = ni(
          t.stateNode.containerInfo
        ), fl(l, t), sl(t), Hl = u;
        break;
      case 12:
        fl(l, t), sl(t);
        break;
      case 31:
        fl(l, t), sl(t), u & 4 && (u = t.updateQueue, u !== null && (t.updateQueue = null, Kn(t, u)));
        break;
      case 13:
        fl(l, t), sl(t), t.child.flags & 8192 && t.memoizedState !== null != (e !== null && e.memoizedState !== null) && (wn = yl()), u & 4 && (u = t.updateQueue, u !== null && (t.updateQueue = null, Kn(t, u)));
        break;
      case 22:
        a = t.memoizedState !== null;
        var d = e !== null && e.memoizedState !== null, g = ie, z = Yt;
        if (ie = g || a, Yt = z || d, fl(l, t), Yt = z, ie = g, sl(t), u & 8192)
          t: for (l = t.stateNode, l._visibility = a ? l._visibility & -2 : l._visibility | 1, a && (e === null || d || ie || Yt || fu(t)), e = null, l = t; ; ) {
            if (l.tag === 5 || l.tag === 26) {
              if (e === null) {
                d = e = l;
                try {
                  if (n = d.stateNode, a)
                    i = n.style, typeof i.setProperty == "function" ? i.setProperty("display", "none", "important") : i.display = "none";
                  else {
                    f = d.stateNode;
                    var M = d.memoizedProps.style, S = M != null && M.hasOwnProperty("display") ? M.display : null;
                    f.style.display = S == null || typeof S == "boolean" ? "" : ("" + S).trim();
                  }
                } catch (G) {
                  pt(d, d.return, G);
                }
              }
            } else if (l.tag === 6) {
              if (e === null) {
                d = l;
                try {
                  d.stateNode.nodeValue = a ? "" : d.memoizedProps;
                } catch (G) {
                  pt(d, d.return, G);
                }
              }
            } else if (l.tag === 18) {
              if (e === null) {
                d = l;
                try {
                  var _ = d.stateNode;
                  a ? Ed(_, !0) : Ed(d.stateNode, !1);
                } catch (G) {
                  pt(d, d.return, G);
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
        u & 4 && (u = t.updateQueue, u !== null && (e = u.retryQueue, e !== null && (u.retryQueue = null, Kn(t, e))));
        break;
      case 19:
        fl(l, t), sl(t), u & 4 && (u = t.updateQueue, u !== null && (t.updateQueue = null, Kn(t, u)));
        break;
      case 30:
        break;
      case 21:
        break;
      default:
        fl(l, t), sl(t);
    }
  }
  function sl(t) {
    var l = t.flags;
    if (l & 2) {
      try {
        for (var e, u = t.return; u !== null; ) {
          if (Ao(u)) {
            e = u;
            break;
          }
          u = u.return;
        }
        if (e == null) throw Error(s(160));
        switch (e.tag) {
          case 27:
            var a = e.stateNode, n = Jc(t);
            Vn(t, n, a);
            break;
          case 5:
            var i = e.stateNode;
            e.flags & 32 && (pu(i, ""), e.flags &= -33);
            var f = Jc(t);
            Vn(t, f, i);
            break;
          case 3:
          case 4:
            var d = e.stateNode.containerInfo, g = Jc(t);
            wc(
              t,
              g,
              d
            );
            break;
          default:
            throw Error(s(161));
        }
      } catch (z) {
        pt(t, t.return, z);
      }
      t.flags &= -3;
    }
    l & 4096 && (t.flags &= -4097);
  }
  function Co(t) {
    if (t.subtreeFlags & 1024)
      for (t = t.child; t !== null; ) {
        var l = t;
        Co(l), l.tag === 5 && l.flags & 1024 && l.stateNode.reset(), t = t.sibling;
      }
  }
  function fe(t, l) {
    if (l.subtreeFlags & 8772)
      for (l = l.child; l !== null; )
        xo(t, l.alternate, l), l = l.sibling;
  }
  function fu(t) {
    for (t = t.child; t !== null; ) {
      var l = t;
      switch (l.tag) {
        case 0:
        case 11:
        case 14:
        case 15:
          Me(4, l, l.return), fu(l);
          break;
        case 1:
          Zl(l, l.return);
          var e = l.stateNode;
          typeof e.componentWillUnmount == "function" && Eo(
            l,
            l.return,
            e
          ), fu(l);
          break;
        case 27:
          Za(l.stateNode);
        case 26:
        case 5:
          Zl(l, l.return), fu(l);
          break;
        case 22:
          l.memoizedState === null && fu(l);
          break;
        case 30:
          fu(l);
          break;
        default:
          fu(l);
      }
      t = t.sibling;
    }
  }
  function se(t, l, e) {
    for (e = e && (l.subtreeFlags & 8772) !== 0, l = l.child; l !== null; ) {
      var u = l.alternate, a = t, n = l, i = n.flags;
      switch (n.tag) {
        case 0:
        case 11:
        case 15:
          se(
            a,
            n,
            e
          ), Ca(4, n);
          break;
        case 1:
          if (se(
            a,
            n,
            e
          ), u = n, a = u.stateNode, typeof a.componentDidMount == "function")
            try {
              a.componentDidMount();
            } catch (g) {
              pt(u, u.return, g);
            }
          if (u = n, a = u.updateQueue, a !== null) {
            var f = u.stateNode;
            try {
              var d = a.shared.hiddenCallbacks;
              if (d !== null)
                for (a.shared.hiddenCallbacks = null, a = 0; a < d.length; a++)
                  sr(d[a], f);
            } catch (g) {
              pt(u, u.return, g);
            }
          }
          e && i & 64 && _o(n), ja(n, n.return);
          break;
        case 27:
          qo(n);
        case 26:
        case 5:
          se(
            a,
            n,
            e
          ), e && u === null && i & 4 && zo(n), ja(n, n.return);
          break;
        case 12:
          se(
            a,
            n,
            e
          );
          break;
        case 31:
          se(
            a,
            n,
            e
          ), e && i & 4 && Mo(a, n);
          break;
        case 13:
          se(
            a,
            n,
            e
          ), e && i & 4 && Do(a, n);
          break;
        case 22:
          n.memoizedState === null && se(
            a,
            n,
            e
          ), ja(n, n.return);
          break;
        case 30:
          break;
        default:
          se(
            a,
            n,
            e
          );
      }
      l = l.sibling;
    }
  }
  function $c(t, l) {
    var e = null;
    t !== null && t.memoizedState !== null && t.memoizedState.cachePool !== null && (e = t.memoizedState.cachePool.pool), t = null, l.memoizedState !== null && l.memoizedState.cachePool !== null && (t = l.memoizedState.cachePool.pool), t !== e && (t != null && t.refCount++, e != null && pa(e));
  }
  function Wc(t, l) {
    t = null, l.alternate !== null && (t = l.alternate.memoizedState.cache), l = l.memoizedState.cache, l !== t && (l.refCount++, t != null && pa(t));
  }
  function Bl(t, l, e, u) {
    if (l.subtreeFlags & 10256)
      for (l = l.child; l !== null; )
        jo(
          t,
          l,
          e,
          u
        ), l = l.sibling;
  }
  function jo(t, l, e, u) {
    var a = l.flags;
    switch (l.tag) {
      case 0:
      case 11:
      case 15:
        Bl(
          t,
          l,
          e,
          u
        ), a & 2048 && Ca(9, l);
        break;
      case 1:
        Bl(
          t,
          l,
          e,
          u
        );
        break;
      case 3:
        Bl(
          t,
          l,
          e,
          u
        ), a & 2048 && (t = null, l.alternate !== null && (t = l.alternate.memoizedState.cache), l = l.memoizedState.cache, l !== t && (l.refCount++, t != null && pa(t)));
        break;
      case 12:
        if (a & 2048) {
          Bl(
            t,
            l,
            e,
            u
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
            pt(l, l.return, d);
          }
        } else
          Bl(
            t,
            l,
            e,
            u
          );
        break;
      case 31:
        Bl(
          t,
          l,
          e,
          u
        );
        break;
      case 13:
        Bl(
          t,
          l,
          e,
          u
        );
        break;
      case 23:
        break;
      case 22:
        n = l.stateNode, i = l.alternate, l.memoizedState !== null ? n._visibility & 2 ? Bl(
          t,
          l,
          e,
          u
        ) : Ra(t, l) : n._visibility & 2 ? Bl(
          t,
          l,
          e,
          u
        ) : (n._visibility |= 2, Gu(
          t,
          l,
          e,
          u,
          (l.subtreeFlags & 10256) !== 0 || !1
        )), a & 2048 && $c(i, l);
        break;
      case 24:
        Bl(
          t,
          l,
          e,
          u
        ), a & 2048 && Wc(l.alternate, l);
        break;
      default:
        Bl(
          t,
          l,
          e,
          u
        );
    }
  }
  function Gu(t, l, e, u, a) {
    for (a = a && ((l.subtreeFlags & 10256) !== 0 || !1), l = l.child; l !== null; ) {
      var n = t, i = l, f = e, d = u, g = i.flags;
      switch (i.tag) {
        case 0:
        case 11:
        case 15:
          Gu(
            n,
            i,
            f,
            d,
            a
          ), Ca(8, i);
          break;
        case 23:
          break;
        case 22:
          var z = i.stateNode;
          i.memoizedState !== null ? z._visibility & 2 ? Gu(
            n,
            i,
            f,
            d,
            a
          ) : Ra(
            n,
            i
          ) : (z._visibility |= 2, Gu(
            n,
            i,
            f,
            d,
            a
          )), a && g & 2048 && $c(
            i.alternate,
            i
          );
          break;
        case 24:
          Gu(
            n,
            i,
            f,
            d,
            a
          ), a && g & 2048 && Wc(i.alternate, i);
          break;
        default:
          Gu(
            n,
            i,
            f,
            d,
            a
          );
      }
      l = l.sibling;
    }
  }
  function Ra(t, l) {
    if (l.subtreeFlags & 10256)
      for (l = l.child; l !== null; ) {
        var e = t, u = l, a = u.flags;
        switch (u.tag) {
          case 22:
            Ra(e, u), a & 2048 && $c(
              u.alternate,
              u
            );
            break;
          case 24:
            Ra(e, u), a & 2048 && Wc(u.alternate, u);
            break;
          default:
            Ra(e, u);
        }
        l = l.sibling;
      }
  }
  var Ha = 8192;
  function Qu(t, l, e) {
    if (t.subtreeFlags & Ha)
      for (t = t.child; t !== null; )
        Ro(
          t,
          l,
          e
        ), t = t.sibling;
  }
  function Ro(t, l, e) {
    switch (t.tag) {
      case 26:
        Qu(
          t,
          l,
          e
        ), t.flags & Ha && t.memoizedState !== null && zh(
          e,
          Hl,
          t.memoizedState,
          t.memoizedProps
        );
        break;
      case 5:
        Qu(
          t,
          l,
          e
        );
        break;
      case 3:
      case 4:
        var u = Hl;
        Hl = ni(t.stateNode.containerInfo), Qu(
          t,
          l,
          e
        ), Hl = u;
        break;
      case 22:
        t.memoizedState === null && (u = t.alternate, u !== null && u.memoizedState !== null ? (u = Ha, Ha = 16777216, Qu(
          t,
          l,
          e
        ), Ha = u) : Qu(
          t,
          l,
          e
        ));
        break;
      default:
        Qu(
          t,
          l,
          e
        );
    }
  }
  function Ho(t) {
    var l = t.alternate;
    if (l !== null && (t = l.child, t !== null)) {
      l.child = null;
      do
        l = t.sibling, t.sibling = null, t = l;
      while (t !== null);
    }
  }
  function Ba(t) {
    var l = t.deletions;
    if ((t.flags & 16) !== 0) {
      if (l !== null)
        for (var e = 0; e < l.length; e++) {
          var u = l[e];
          Vt = u, Yo(
            u,
            t
          );
        }
      Ho(t);
    }
    if (t.subtreeFlags & 10256)
      for (t = t.child; t !== null; )
        Bo(t), t = t.sibling;
  }
  function Bo(t) {
    switch (t.tag) {
      case 0:
      case 11:
      case 15:
        Ba(t), t.flags & 2048 && Me(9, t, t.return);
        break;
      case 3:
        Ba(t);
        break;
      case 12:
        Ba(t);
        break;
      case 22:
        var l = t.stateNode;
        t.memoizedState !== null && l._visibility & 2 && (t.return === null || t.return.tag !== 13) ? (l._visibility &= -3, Jn(t)) : Ba(t);
        break;
      default:
        Ba(t);
    }
  }
  function Jn(t) {
    var l = t.deletions;
    if ((t.flags & 16) !== 0) {
      if (l !== null)
        for (var e = 0; e < l.length; e++) {
          var u = l[e];
          Vt = u, Yo(
            u,
            t
          );
        }
      Ho(t);
    }
    for (t = t.child; t !== null; ) {
      switch (l = t, l.tag) {
        case 0:
        case 11:
        case 15:
          Me(8, l, l.return), Jn(l);
          break;
        case 22:
          e = l.stateNode, e._visibility & 2 && (e._visibility &= -3, Jn(l));
          break;
        default:
          Jn(l);
      }
      t = t.sibling;
    }
  }
  function Yo(t, l) {
    for (; Vt !== null; ) {
      var e = Vt;
      switch (e.tag) {
        case 0:
        case 11:
        case 15:
          Me(8, e, l);
          break;
        case 23:
        case 22:
          if (e.memoizedState !== null && e.memoizedState.cachePool !== null) {
            var u = e.memoizedState.cachePool.pool;
            u != null && u.refCount++;
          }
          break;
        case 24:
          pa(e.memoizedState.cache);
      }
      if (u = e.child, u !== null) u.return = e, Vt = u;
      else
        t: for (e = t; Vt !== null; ) {
          u = Vt;
          var a = u.sibling, n = u.return;
          if (No(u), u === e) {
            Vt = null;
            break t;
          }
          if (a !== null) {
            a.return = n, Vt = a;
            break t;
          }
          Vt = n;
        }
    }
  }
  var Yv = {
    getCacheForType: function(t) {
      var l = Wt(Rt), e = l.data.get(t);
      return e === void 0 && (e = t(), l.data.set(t, e)), e;
    },
    cacheSignal: function() {
      return Wt(Rt).controller.signal;
    }
  }, Lv = typeof WeakMap == "function" ? WeakMap : Map, mt = 0, At = null, it = null, st = 0, St = 0, pl = null, De = !1, Xu = !1, Fc = !1, re = 0, Mt = 0, Ue = 0, su = 0, Ic = 0, _l = 0, Zu = 0, Ya = null, rl = null, Pc = !1, wn = 0, Lo = 0, kn = 1 / 0, $n = null, Ce = null, Qt = 0, je = null, Vu = null, oe = 0, tf = 0, lf = null, Go = null, La = 0, ef = null;
  function El() {
    return (mt & 2) !== 0 && st !== 0 ? st & -st : A.T !== null ? sf() : ls();
  }
  function Qo() {
    if (_l === 0)
      if ((st & 536870912) === 0 || ot) {
        var t = en;
        en <<= 1, (en & 3932160) === 0 && (en = 262144), _l = t;
      } else _l = 536870912;
    return t = bl.current, t !== null && (t.flags |= 32), _l;
  }
  function ol(t, l, e) {
    (t === At && (St === 2 || St === 9) || t.cancelPendingCommit !== null) && (Ku(t, 0), Re(
      t,
      st,
      _l,
      !1
    )), ia(t, e), ((mt & 2) === 0 || t !== At) && (t === At && ((mt & 2) === 0 && (su |= e), Mt === 4 && Re(
      t,
      st,
      _l,
      !1
    )), Vl(t));
  }
  function Xo(t, l, e) {
    if ((mt & 6) !== 0) throw Error(s(327));
    var u = !e && (l & 127) === 0 && (l & t.expiredLanes) === 0 || na(t, l), a = u ? Xv(t, l) : af(t, l, !0), n = u;
    do {
      if (a === 0) {
        Xu && !u && Re(t, l, 0, !1);
        break;
      } else {
        if (e = t.current.alternate, n && !Gv(e)) {
          a = af(t, l, !1), n = !1;
          continue;
        }
        if (a === 2) {
          if (n = l, t.errorRecoveryDisabledLanes & n)
            var i = 0;
          else
            i = t.pendingLanes & -536870913, i = i !== 0 ? i : i & 536870912 ? 536870912 : 0;
          if (i !== 0) {
            l = i;
            t: {
              var f = t;
              a = Ya;
              var d = f.current.memoizedState.isDehydrated;
              if (d && (Ku(f, i).flags |= 256), i = af(
                f,
                i,
                !1
              ), i !== 2) {
                if (Fc && !d) {
                  f.errorRecoveryDisabledLanes |= n, su |= n, a = 4;
                  break t;
                }
                n = rl, rl = a, n !== null && (rl === null ? rl = n : rl.push.apply(
                  rl,
                  n
                ));
              }
              a = i;
            }
            if (n = !1, a !== 2) continue;
          }
        }
        if (a === 1) {
          Ku(t, 0), Re(t, l, 0, !0);
          break;
        }
        t: {
          switch (u = t, n = a, n) {
            case 0:
            case 1:
              throw Error(s(345));
            case 4:
              if ((l & 4194048) !== l) break;
            case 6:
              Re(
                u,
                l,
                _l,
                !De
              );
              break t;
            case 2:
              rl = null;
              break;
            case 3:
            case 5:
              break;
            default:
              throw Error(s(329));
          }
          if ((l & 62914560) === l && (a = wn + 300 - yl(), 10 < a)) {
            if (Re(
              u,
              l,
              _l,
              !De
            ), an(u, 0, !0) !== 0) break t;
            oe = l, u.timeoutHandle = Sd(
              Zo.bind(
                null,
                u,
                e,
                rl,
                $n,
                Pc,
                l,
                _l,
                su,
                Zu,
                De,
                n,
                "Throttled",
                -0,
                0
              ),
              a
            );
            break t;
          }
          Zo(
            u,
            e,
            rl,
            $n,
            Pc,
            l,
            _l,
            su,
            Zu,
            De,
            n,
            null,
            -0,
            0
          );
        }
      }
      break;
    } while (!0);
    Vl(t);
  }
  function Zo(t, l, e, u, a, n, i, f, d, g, z, M, S, _) {
    if (t.timeoutHandle = -1, M = l.subtreeFlags, M & 8192 || (M & 16785408) === 16785408) {
      M = {
        stylesheets: null,
        count: 0,
        imgCount: 0,
        imgBytes: 0,
        suspenseyImages: [],
        waitingForImages: !0,
        waitingForViewTransition: !1,
        unsuspend: Wl
      }, Ro(
        l,
        n,
        M
      );
      var G = (n & 62914560) === n ? wn - yl() : (n & 4194048) === n ? Lo - yl() : 0;
      if (G = Ah(
        M,
        G
      ), G !== null) {
        oe = n, t.cancelPendingCommit = G(
          Fo.bind(
            null,
            t,
            l,
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
        ), Re(t, n, i, !g);
        return;
      }
    }
    Fo(
      t,
      l,
      n,
      e,
      u,
      a,
      i,
      f,
      d
    );
  }
  function Gv(t) {
    for (var l = t; ; ) {
      var e = l.tag;
      if ((e === 0 || e === 11 || e === 15) && l.flags & 16384 && (e = l.updateQueue, e !== null && (e = e.stores, e !== null)))
        for (var u = 0; u < e.length; u++) {
          var a = e[u], n = a.getSnapshot;
          a = a.value;
          try {
            if (!ml(n(), a)) return !1;
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
  function Re(t, l, e, u) {
    l &= ~Ic, l &= ~su, t.suspendedLanes |= l, t.pingedLanes &= ~l, u && (t.warmLanes |= l), u = t.expirationTimes;
    for (var a = l; 0 < a; ) {
      var n = 31 - hl(a), i = 1 << n;
      u[n] = -1, a &= ~i;
    }
    e !== 0 && If(t, e, l);
  }
  function Wn() {
    return (mt & 6) === 0 ? (Ga(0), !1) : !0;
  }
  function uf() {
    if (it !== null) {
      if (St === 0)
        var t = it.return;
      else
        t = it, te = tu = null, Sc(t), Ru = null, Ea = 0, t = it;
      for (; t !== null; )
        po(t.alternate, t), t = t.return;
      it = null;
    }
  }
  function Ku(t, l) {
    var e = t.timeoutHandle;
    e !== -1 && (t.timeoutHandle = -1, ih(e)), e = t.cancelPendingCommit, e !== null && (t.cancelPendingCommit = null, e()), oe = 0, uf(), At = t, it = e = Il(t.current, null), st = l, St = 0, pl = null, De = !1, Xu = na(t, l), Fc = !1, Zu = _l = Ic = su = Ue = Mt = 0, rl = Ya = null, Pc = !1, (l & 8) !== 0 && (l |= l & 32);
    var u = t.entangledLanes;
    if (u !== 0)
      for (t = t.entanglements, u &= l; 0 < u; ) {
        var a = 31 - hl(u), n = 1 << a;
        l |= t[a], u &= ~n;
      }
    return re = l, gn(), e;
  }
  function Vo(t, l) {
    lt = null, A.H = Ma, l === ju || l === qn ? (l = nr(), St = 3) : l === cc ? (l = nr(), St = 4) : St = l === Rc ? 8 : l !== null && typeof l == "object" && typeof l.then == "function" ? 6 : 1, pl = l, it === null && (Mt = 1, Ln(
      t,
      xl(l, t.current)
    ));
  }
  function Ko() {
    var t = bl.current;
    return t === null ? !0 : (st & 4194048) === st ? Dl === null : (st & 62914560) === st || (st & 536870912) !== 0 ? t === Dl : !1;
  }
  function Jo() {
    var t = A.H;
    return A.H = Ma, t === null ? Ma : t;
  }
  function wo() {
    var t = A.A;
    return A.A = Yv, t;
  }
  function Fn() {
    Mt = 4, De || (st & 4194048) !== st && bl.current !== null || (Xu = !0), (Ue & 134217727) === 0 && (su & 134217727) === 0 || At === null || Re(
      At,
      st,
      _l,
      !1
    );
  }
  function af(t, l, e) {
    var u = mt;
    mt |= 2;
    var a = Jo(), n = wo();
    (At !== t || st !== l) && ($n = null, Ku(t, l)), l = !1;
    var i = Mt;
    t: do
      try {
        if (St !== 0 && it !== null) {
          var f = it, d = pl;
          switch (St) {
            case 8:
              uf(), i = 6;
              break t;
            case 3:
            case 2:
            case 9:
            case 6:
              bl.current === null && (l = !0);
              var g = St;
              if (St = 0, pl = null, Ju(t, f, d, g), e && Xu) {
                i = 0;
                break t;
              }
              break;
            default:
              g = St, St = 0, pl = null, Ju(t, f, d, g);
          }
        }
        Qv(), i = Mt;
        break;
      } catch (z) {
        Vo(t, z);
      }
    while (!0);
    return l && t.shellSuspendCounter++, te = tu = null, mt = u, A.H = a, A.A = n, it === null && (At = null, st = 0, gn()), i;
  }
  function Qv() {
    for (; it !== null; ) ko(it);
  }
  function Xv(t, l) {
    var e = mt;
    mt |= 2;
    var u = Jo(), a = wo();
    At !== t || st !== l ? ($n = null, kn = yl() + 500, Ku(t, l)) : Xu = na(
      t,
      l
    );
    t: do
      try {
        if (St !== 0 && it !== null) {
          l = it;
          var n = pl;
          l: switch (St) {
            case 1:
              St = 0, pl = null, Ju(t, l, n, 1);
              break;
            case 2:
            case 9:
              if (ur(n)) {
                St = 0, pl = null, $o(l);
                break;
              }
              l = function() {
                St !== 2 && St !== 9 || At !== t || (St = 7), Vl(t);
              }, n.then(l, l);
              break t;
            case 3:
              St = 7;
              break t;
            case 4:
              St = 5;
              break t;
            case 7:
              ur(n) ? (St = 0, pl = null, $o(l)) : (St = 0, pl = null, Ju(t, l, n, 7));
              break;
            case 5:
              var i = null;
              switch (it.tag) {
                case 26:
                  i = it.memoizedState;
                case 5:
                case 27:
                  var f = it;
                  if (i ? jd(i) : f.stateNode.complete) {
                    St = 0, pl = null;
                    var d = f.sibling;
                    if (d !== null) it = d;
                    else {
                      var g = f.return;
                      g !== null ? (it = g, In(g)) : it = null;
                    }
                    break l;
                  }
              }
              St = 0, pl = null, Ju(t, l, n, 5);
              break;
            case 6:
              St = 0, pl = null, Ju(t, l, n, 6);
              break;
            case 8:
              uf(), Mt = 6;
              break t;
            default:
              throw Error(s(462));
          }
        }
        Zv();
        break;
      } catch (z) {
        Vo(t, z);
      }
    while (!0);
    return te = tu = null, A.H = u, A.A = a, mt = e, it !== null ? 0 : (At = null, st = 0, gn(), Mt);
  }
  function Zv() {
    for (; it !== null && !dy(); )
      ko(it);
  }
  function ko(t) {
    var l = bo(t.alternate, t, re);
    t.memoizedProps = t.pendingProps, l === null ? In(t) : it = l;
  }
  function $o(t) {
    var l = t, e = l.alternate;
    switch (l.tag) {
      case 15:
      case 0:
        l = oo(
          e,
          l,
          l.pendingProps,
          l.type,
          void 0,
          st
        );
        break;
      case 11:
        l = oo(
          e,
          l,
          l.pendingProps,
          l.type.render,
          l.ref,
          st
        );
        break;
      case 5:
        Sc(l);
      default:
        po(e, l), l = it = Js(l, re), l = bo(e, l, re);
    }
    t.memoizedProps = t.pendingProps, l === null ? In(t) : it = l;
  }
  function Ju(t, l, e, u) {
    te = tu = null, Sc(l), Ru = null, Ea = 0;
    var a = l.return;
    try {
      if (Dv(
        t,
        a,
        l,
        e,
        st
      )) {
        Mt = 1, Ln(
          t,
          xl(e, t.current)
        ), it = null;
        return;
      }
    } catch (n) {
      if (a !== null) throw it = a, n;
      Mt = 1, Ln(
        t,
        xl(e, t.current)
      ), it = null;
      return;
    }
    l.flags & 32768 ? (ot || u === 1 ? t = !0 : Xu || (st & 536870912) !== 0 ? t = !1 : (De = t = !0, (u === 2 || u === 9 || u === 3 || u === 6) && (u = bl.current, u !== null && u.tag === 13 && (u.flags |= 16384))), Wo(l, t)) : In(l);
  }
  function In(t) {
    var l = t;
    do {
      if ((l.flags & 32768) !== 0) {
        Wo(
          l,
          De
        );
        return;
      }
      t = l.return;
      var e = jv(
        l.alternate,
        l,
        re
      );
      if (e !== null) {
        it = e;
        return;
      }
      if (l = l.sibling, l !== null) {
        it = l;
        return;
      }
      it = l = t;
    } while (l !== null);
    Mt === 0 && (Mt = 5);
  }
  function Wo(t, l) {
    do {
      var e = Rv(t.alternate, t);
      if (e !== null) {
        e.flags &= 32767, it = e;
        return;
      }
      if (e = t.return, e !== null && (e.flags |= 32768, e.subtreeFlags = 0, e.deletions = null), !l && (t = t.sibling, t !== null)) {
        it = t;
        return;
      }
      it = t = e;
    } while (t !== null);
    Mt = 6, it = null;
  }
  function Fo(t, l, e, u, a, n, i, f, d) {
    t.cancelPendingCommit = null;
    do
      Pn();
    while (Qt !== 0);
    if ((mt & 6) !== 0) throw Error(s(327));
    if (l !== null) {
      if (l === t.current) throw Error(s(177));
      if (n = l.lanes | l.childLanes, n |= Ji, Ey(
        t,
        e,
        n,
        i,
        f,
        d
      ), t === At && (it = At = null, st = 0), Vu = l, je = t, oe = e, tf = n, lf = a, Go = u, (l.subtreeFlags & 10256) !== 0 || (l.flags & 10256) !== 0 ? (t.callbackNode = null, t.callbackPriority = 0, wv(tn, function() {
        return ed(), null;
      })) : (t.callbackNode = null, t.callbackPriority = 0), u = (l.flags & 13878) !== 0, (l.subtreeFlags & 13878) !== 0 || u) {
        u = A.T, A.T = null, a = H.p, H.p = 2, i = mt, mt |= 4;
        try {
          Hv(t, l, e);
        } finally {
          mt = i, H.p = a, A.T = u;
        }
      }
      Qt = 1, Io(), Po(), td();
    }
  }
  function Io() {
    if (Qt === 1) {
      Qt = 0;
      var t = je, l = Vu, e = (l.flags & 13878) !== 0;
      if ((l.subtreeFlags & 13878) !== 0 || e) {
        e = A.T, A.T = null;
        var u = H.p;
        H.p = 2;
        var a = mt;
        mt |= 4;
        try {
          Uo(l, t);
          var n = gf, i = Bs(t.containerInfo), f = n.focusedElem, d = n.selectionRange;
          if (i !== f && f && f.ownerDocument && Hs(
            f.ownerDocument.documentElement,
            f
          )) {
            if (d !== null && Qi(f)) {
              var g = d.start, z = d.end;
              if (z === void 0 && (z = g), "selectionStart" in f)
                f.selectionStart = g, f.selectionEnd = Math.min(
                  z,
                  f.value.length
                );
              else {
                var M = f.ownerDocument || document, S = M && M.defaultView || window;
                if (S.getSelection) {
                  var _ = S.getSelection(), G = f.textContent.length, J = Math.min(d.start, G), zt = d.end === void 0 ? J : Math.min(d.end, G);
                  !_.extend && J > zt && (i = zt, zt = J, J = i);
                  var h = Rs(
                    f,
                    J
                  ), y = Rs(
                    f,
                    zt
                  );
                  if (h && y && (_.rangeCount !== 1 || _.anchorNode !== h.node || _.anchorOffset !== h.offset || _.focusNode !== y.node || _.focusOffset !== y.offset)) {
                    var m = M.createRange();
                    m.setStart(h.node, h.offset), _.removeAllRanges(), J > zt ? (_.addRange(m), _.extend(y.node, y.offset)) : (m.setEnd(y.node, y.offset), _.addRange(m));
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
              var T = M[f];
              T.element.scrollLeft = T.left, T.element.scrollTop = T.top;
            }
          }
          oi = !!mf, gf = mf = null;
        } finally {
          mt = a, H.p = u, A.T = e;
        }
      }
      t.current = l, Qt = 2;
    }
  }
  function Po() {
    if (Qt === 2) {
      Qt = 0;
      var t = je, l = Vu, e = (l.flags & 8772) !== 0;
      if ((l.subtreeFlags & 8772) !== 0 || e) {
        e = A.T, A.T = null;
        var u = H.p;
        H.p = 2;
        var a = mt;
        mt |= 4;
        try {
          xo(t, l.alternate, l);
        } finally {
          mt = a, H.p = u, A.T = e;
        }
      }
      Qt = 3;
    }
  }
  function td() {
    if (Qt === 4 || Qt === 3) {
      Qt = 0, yy();
      var t = je, l = Vu, e = oe, u = Go;
      (l.subtreeFlags & 10256) !== 0 || (l.flags & 10256) !== 0 ? Qt = 5 : (Qt = 0, Vu = je = null, ld(t, t.pendingLanes));
      var a = t.pendingLanes;
      if (a === 0 && (Ce = null), Ei(e), l = l.stateNode, vl && typeof vl.onCommitFiberRoot == "function")
        try {
          vl.onCommitFiberRoot(
            aa,
            l,
            void 0,
            (l.current.flags & 128) === 128
          );
        } catch {
        }
      if (u !== null) {
        l = A.T, a = H.p, H.p = 2, A.T = null;
        try {
          for (var n = t.onRecoverableError, i = 0; i < u.length; i++) {
            var f = u[i];
            n(f.value, {
              componentStack: f.stack
            });
          }
        } finally {
          A.T = l, H.p = a;
        }
      }
      (oe & 3) !== 0 && Pn(), Vl(t), a = t.pendingLanes, (e & 261930) !== 0 && (a & 42) !== 0 ? t === ef ? La++ : (La = 0, ef = t) : La = 0, Ga(0);
    }
  }
  function ld(t, l) {
    (t.pooledCacheLanes &= l) === 0 && (l = t.pooledCache, l != null && (t.pooledCache = null, pa(l)));
  }
  function Pn() {
    return Io(), Po(), td(), ed();
  }
  function ed() {
    if (Qt !== 5) return !1;
    var t = je, l = tf;
    tf = 0;
    var e = Ei(oe), u = A.T, a = H.p;
    try {
      H.p = 32 > e ? 32 : e, A.T = null, e = lf, lf = null;
      var n = je, i = oe;
      if (Qt = 0, Vu = je = null, oe = 0, (mt & 6) !== 0) throw Error(s(331));
      var f = mt;
      if (mt |= 4, Bo(n.current), jo(
        n,
        n.current,
        i,
        e
      ), mt = f, Ga(0, !1), vl && typeof vl.onPostCommitFiberRoot == "function")
        try {
          vl.onPostCommitFiberRoot(aa, n);
        } catch {
        }
      return !0;
    } finally {
      H.p = a, A.T = u, ld(t, l);
    }
  }
  function ud(t, l, e) {
    l = xl(e, l), l = jc(t.stateNode, l, 2), t = xe(t, l, 2), t !== null && (ia(t, 2), Vl(t));
  }
  function pt(t, l, e) {
    if (t.tag === 3)
      ud(t, t, e);
    else
      for (; l !== null; ) {
        if (l.tag === 3) {
          ud(
            l,
            t,
            e
          );
          break;
        } else if (l.tag === 1) {
          var u = l.stateNode;
          if (typeof l.type.getDerivedStateFromError == "function" || typeof u.componentDidCatch == "function" && (Ce === null || !Ce.has(u))) {
            t = xl(e, t), e = uo(2), u = xe(l, e, 2), u !== null && (ao(
              e,
              u,
              l,
              t
            ), ia(u, 2), Vl(u));
            break;
          }
        }
        l = l.return;
      }
  }
  function nf(t, l, e) {
    var u = t.pingCache;
    if (u === null) {
      u = t.pingCache = new Lv();
      var a = /* @__PURE__ */ new Set();
      u.set(l, a);
    } else
      a = u.get(l), a === void 0 && (a = /* @__PURE__ */ new Set(), u.set(l, a));
    a.has(e) || (Fc = !0, a.add(e), t = Vv.bind(null, t, l, e), l.then(t, t));
  }
  function Vv(t, l, e) {
    var u = t.pingCache;
    u !== null && u.delete(l), t.pingedLanes |= t.suspendedLanes & e, t.warmLanes &= ~e, At === t && (st & e) === e && (Mt === 4 || Mt === 3 && (st & 62914560) === st && 300 > yl() - wn ? (mt & 2) === 0 && Ku(t, 0) : Ic |= e, Zu === st && (Zu = 0)), Vl(t);
  }
  function ad(t, l) {
    l === 0 && (l = Ff()), t = Fe(t, l), t !== null && (ia(t, l), Vl(t));
  }
  function Kv(t) {
    var l = t.memoizedState, e = 0;
    l !== null && (e = l.retryLane), ad(t, e);
  }
  function Jv(t, l) {
    var e = 0;
    switch (t.tag) {
      case 31:
      case 13:
        var u = t.stateNode, a = t.memoizedState;
        a !== null && (e = a.retryLane);
        break;
      case 19:
        u = t.stateNode;
        break;
      case 22:
        u = t.stateNode._retryCache;
        break;
      default:
        throw Error(s(314));
    }
    u !== null && u.delete(l), ad(t, e);
  }
  function wv(t, l) {
    return yu(t, l);
  }
  var ti = null, wu = null, cf = !1, li = !1, ff = !1, He = 0;
  function Vl(t) {
    t !== wu && t.next === null && (wu === null ? ti = wu = t : wu = wu.next = t), li = !0, cf || (cf = !0, $v());
  }
  function Ga(t, l) {
    if (!ff && li) {
      ff = !0;
      do
        for (var e = !1, u = ti; u !== null; ) {
          if (t !== 0) {
            var a = u.pendingLanes;
            if (a === 0) var n = 0;
            else {
              var i = u.suspendedLanes, f = u.pingedLanes;
              n = (1 << 31 - hl(42 | t) + 1) - 1, n &= a & ~(i & ~f), n = n & 201326741 ? n & 201326741 | 1 : n ? n | 2 : 0;
            }
            n !== 0 && (e = !0, fd(u, n));
          } else
            n = st, n = an(
              u,
              u === At ? n : 0,
              u.cancelPendingCommit !== null || u.timeoutHandle !== -1
            ), (n & 3) === 0 || na(u, n) || (e = !0, fd(u, n));
          u = u.next;
        }
      while (e);
      ff = !1;
    }
  }
  function kv() {
    nd();
  }
  function nd() {
    li = cf = !1;
    var t = 0;
    He !== 0 && nh() && (t = He);
    for (var l = yl(), e = null, u = ti; u !== null; ) {
      var a = u.next, n = id(u, l);
      n === 0 ? (u.next = null, e === null ? ti = a : e.next = a, a === null && (wu = e)) : (e = u, (t !== 0 || (n & 3) !== 0) && (li = !0)), u = a;
    }
    Qt !== 0 && Qt !== 5 || Ga(t), He !== 0 && (He = 0);
  }
  function id(t, l) {
    for (var e = t.suspendedLanes, u = t.pingedLanes, a = t.expirationTimes, n = t.pendingLanes & -62914561; 0 < n; ) {
      var i = 31 - hl(n), f = 1 << i, d = a[i];
      d === -1 ? ((f & e) === 0 || (f & u) !== 0) && (a[i] = _y(f, l)) : d <= l && (t.expiredLanes |= f), n &= ~f;
    }
    if (l = At, e = st, e = an(
      t,
      t === l ? e : 0,
      t.cancelPendingCommit !== null || t.timeoutHandle !== -1
    ), u = t.callbackNode, e === 0 || t === l && (St === 2 || St === 9) || t.cancelPendingCommit !== null)
      return u !== null && u !== null && Si(u), t.callbackNode = null, t.callbackPriority = 0;
    if ((e & 3) === 0 || na(t, e)) {
      if (l = e & -e, l === t.callbackPriority) return l;
      switch (u !== null && Si(u), Ei(e)) {
        case 2:
        case 8:
          e = $f;
          break;
        case 32:
          e = tn;
          break;
        case 268435456:
          e = Wf;
          break;
        default:
          e = tn;
      }
      return u = cd.bind(null, t), e = yu(e, u), t.callbackPriority = l, t.callbackNode = e, l;
    }
    return u !== null && u !== null && Si(u), t.callbackPriority = 2, t.callbackNode = null, 2;
  }
  function cd(t, l) {
    if (Qt !== 0 && Qt !== 5)
      return t.callbackNode = null, t.callbackPriority = 0, null;
    var e = t.callbackNode;
    if (Pn() && t.callbackNode !== e)
      return null;
    var u = st;
    return u = an(
      t,
      t === At ? u : 0,
      t.cancelPendingCommit !== null || t.timeoutHandle !== -1
    ), u === 0 ? null : (Xo(t, u, l), id(t, yl()), t.callbackNode != null && t.callbackNode === e ? cd.bind(null, t) : null);
  }
  function fd(t, l) {
    if (Pn()) return null;
    Xo(t, l, !0);
  }
  function $v() {
    ch(function() {
      (mt & 6) !== 0 ? yu(
        kf,
        kv
      ) : nd();
    });
  }
  function sf() {
    if (He === 0) {
      var t = Uu;
      t === 0 && (t = ln, ln <<= 1, (ln & 261888) === 0 && (ln = 256)), He = t;
    }
    return He;
  }
  function sd(t) {
    return t == null || typeof t == "symbol" || typeof t == "boolean" ? null : typeof t == "function" ? t : sn("" + t);
  }
  function rd(t, l) {
    var e = l.ownerDocument.createElement("input");
    return e.name = l.name, e.value = l.value, t.id && e.setAttribute("form", t.id), l.parentNode.insertBefore(e, l), t = new FormData(t), e.parentNode.removeChild(e), t;
  }
  function Wv(t, l, e, u, a) {
    if (l === "submit" && e && e.stateNode === a) {
      var n = sd(
        (a[nl] || null).action
      ), i = u.submitter;
      i && (l = (l = i[nl] || null) ? sd(l.formAction) : i.getAttribute("formAction"), l !== null && (n = l, i = null));
      var f = new yn(
        "action",
        "action",
        null,
        u,
        a
      );
      t.push({
        event: f,
        listeners: [
          {
            instance: null,
            listener: function() {
              if (u.defaultPrevented) {
                if (He !== 0) {
                  var d = i ? rd(a, i) : new FormData(a);
                  Nc(
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
                typeof n == "function" && (f.preventDefault(), d = i ? rd(a, i) : new FormData(a), Nc(
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
  for (var rf = 0; rf < Ki.length; rf++) {
    var of = Ki[rf], Fv = of.toLowerCase(), Iv = of[0].toUpperCase() + of.slice(1);
    Rl(
      Fv,
      "on" + Iv
    );
  }
  Rl(Gs, "onAnimationEnd"), Rl(Qs, "onAnimationIteration"), Rl(Xs, "onAnimationStart"), Rl("dblclick", "onDoubleClick"), Rl("focusin", "onFocus"), Rl("focusout", "onBlur"), Rl(vv, "onTransitionRun"), Rl(hv, "onTransitionStart"), Rl(mv, "onTransitionCancel"), Rl(Zs, "onTransitionEnd"), bu("onMouseEnter", ["mouseout", "mouseover"]), bu("onMouseLeave", ["mouseout", "mouseover"]), bu("onPointerEnter", ["pointerout", "pointerover"]), bu("onPointerLeave", ["pointerout", "pointerover"]), we(
    "onChange",
    "change click focusin focusout input keydown keyup selectionchange".split(" ")
  ), we(
    "onSelect",
    "focusout contextmenu dragend focusin keydown keyup mousedown mouseup selectionchange".split(
      " "
    )
  ), we("onBeforeInput", [
    "compositionend",
    "keypress",
    "textInput",
    "paste"
  ]), we(
    "onCompositionEnd",
    "compositionend focusout keydown keypress keyup mousedown".split(" ")
  ), we(
    "onCompositionStart",
    "compositionstart focusout keydown keypress keyup mousedown".split(" ")
  ), we(
    "onCompositionUpdate",
    "compositionupdate focusout keydown keypress keyup mousedown".split(" ")
  );
  var Qa = "abort canplay canplaythrough durationchange emptied encrypted ended error loadeddata loadedmetadata loadstart pause play playing progress ratechange resize seeked seeking stalled suspend timeupdate volumechange waiting".split(
    " "
  ), Pv = new Set(
    "beforetoggle cancel close invalid load scroll scrollend toggle".split(" ").concat(Qa)
  );
  function od(t, l) {
    l = (l & 4) !== 0;
    for (var e = 0; e < t.length; e++) {
      var u = t[e], a = u.event;
      u = u.listeners;
      t: {
        var n = void 0;
        if (l)
          for (var i = u.length - 1; 0 <= i; i--) {
            var f = u[i], d = f.instance, g = f.currentTarget;
            if (f = f.listener, d !== n && a.isPropagationStopped())
              break t;
            n = f, a.currentTarget = g;
            try {
              n(a);
            } catch (z) {
              mn(z);
            }
            a.currentTarget = null, n = d;
          }
        else
          for (i = 0; i < u.length; i++) {
            if (f = u[i], d = f.instance, g = f.currentTarget, f = f.listener, d !== n && a.isPropagationStopped())
              break t;
            n = f, a.currentTarget = g;
            try {
              n(a);
            } catch (z) {
              mn(z);
            }
            a.currentTarget = null, n = d;
          }
      }
    }
  }
  function ct(t, l) {
    var e = l[zi];
    e === void 0 && (e = l[zi] = /* @__PURE__ */ new Set());
    var u = t + "__bubble";
    e.has(u) || (dd(l, t, 2, !1), e.add(u));
  }
  function df(t, l, e) {
    var u = 0;
    l && (u |= 4), dd(
      e,
      t,
      u,
      l
    );
  }
  var ei = "_reactListening" + Math.random().toString(36).slice(2);
  function yf(t) {
    if (!t[ei]) {
      t[ei] = !0, as.forEach(function(e) {
        e !== "selectionchange" && (Pv.has(e) || df(e, !1, t), df(e, !0, t));
      });
      var l = t.nodeType === 9 ? t : t.ownerDocument;
      l === null || l[ei] || (l[ei] = !0, df("selectionchange", !1, l));
    }
  }
  function dd(t, l, e, u) {
    switch (Qd(l)) {
      case 2:
        var a = xh;
        break;
      case 8:
        a = Nh;
        break;
      default:
        a = Nf;
    }
    e = a.bind(
      null,
      l,
      e,
      t
    ), a = void 0, !Ui || l !== "touchstart" && l !== "touchmove" && l !== "wheel" || (a = !0), u ? a !== void 0 ? t.addEventListener(l, e, {
      capture: !0,
      passive: a
    }) : t.addEventListener(l, e, !0) : a !== void 0 ? t.addEventListener(l, e, {
      passive: a
    }) : t.addEventListener(l, e, !1);
  }
  function vf(t, l, e, u, a) {
    var n = u;
    if ((l & 1) === 0 && (l & 2) === 0 && u !== null)
      t: for (; ; ) {
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
            if (i = hu(f), i === null) return;
            if (d = i.tag, d === 5 || d === 6 || d === 26 || d === 27) {
              u = n = i;
              continue t;
            }
            f = f.parentNode;
          }
        }
        u = u.return;
      }
    ms(function() {
      var g = n, z = Mi(e), M = [];
      t: {
        var S = Vs.get(t);
        if (S !== void 0) {
          var _ = yn, G = t;
          switch (t) {
            case "keypress":
              if (on(e) === 0) break t;
            case "keydown":
            case "keyup":
              _ = Jy;
              break;
            case "focusin":
              G = "focus", _ = Hi;
              break;
            case "focusout":
              G = "blur", _ = Hi;
              break;
            case "beforeblur":
            case "afterblur":
              _ = Hi;
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
              _ = Ss;
              break;
            case "drag":
            case "dragend":
            case "dragenter":
            case "dragexit":
            case "dragleave":
            case "dragover":
            case "dragstart":
            case "drop":
              _ = jy;
              break;
            case "touchcancel":
            case "touchend":
            case "touchmove":
            case "touchstart":
              _ = $y;
              break;
            case Gs:
            case Qs:
            case Xs:
              _ = By;
              break;
            case Zs:
              _ = Fy;
              break;
            case "scroll":
            case "scrollend":
              _ = Uy;
              break;
            case "wheel":
              _ = Py;
              break;
            case "copy":
            case "cut":
            case "paste":
              _ = Ly;
              break;
            case "gotpointercapture":
            case "lostpointercapture":
            case "pointercancel":
            case "pointerdown":
            case "pointermove":
            case "pointerout":
            case "pointerover":
            case "pointerup":
              _ = _s;
              break;
            case "toggle":
            case "beforetoggle":
              _ = lv;
          }
          var J = (l & 4) !== 0, zt = !J && (t === "scroll" || t === "scrollend"), h = J ? S !== null ? S + "Capture" : null : S;
          J = [];
          for (var y = g, m; y !== null; ) {
            var T = y;
            if (m = T.stateNode, T = T.tag, T !== 5 && T !== 26 && T !== 27 || m === null || h === null || (T = sa(y, h), T != null && J.push(
              Xa(y, T, m)
            )), zt) break;
            y = y.return;
          }
          0 < J.length && (S = new _(
            S,
            G,
            null,
            e,
            z
          ), M.push({ event: S, listeners: J }));
        }
      }
      if ((l & 7) === 0) {
        t: {
          if (S = t === "mouseover" || t === "pointerover", _ = t === "mouseout" || t === "pointerout", S && e !== Oi && (G = e.relatedTarget || e.fromElement) && (hu(G) || G[vu]))
            break t;
          if ((_ || S) && (S = z.window === z ? z : (S = z.ownerDocument) ? S.defaultView || S.parentWindow : window, _ ? (G = e.relatedTarget || e.toElement, _ = g, G = G ? hu(G) : null, G !== null && (zt = E(G), J = G.tag, G !== zt || J !== 5 && J !== 27 && J !== 6) && (G = null)) : (_ = null, G = g), _ !== G)) {
            if (J = Ss, T = "onMouseLeave", h = "onMouseEnter", y = "mouse", (t === "pointerout" || t === "pointerover") && (J = _s, T = "onPointerLeave", h = "onPointerEnter", y = "pointer"), zt = _ == null ? S : fa(_), m = G == null ? S : fa(G), S = new J(
              T,
              y + "leave",
              _,
              e,
              z
            ), S.target = zt, S.relatedTarget = m, T = null, hu(z) === g && (J = new J(
              h,
              y + "enter",
              G,
              e,
              z
            ), J.target = m, J.relatedTarget = zt, T = J), zt = T, _ && G)
              l: {
                for (J = th, h = _, y = G, m = 0, T = h; T; T = J(T))
                  m++;
                T = 0;
                for (var V = y; V; V = J(V))
                  T++;
                for (; 0 < m - T; )
                  h = J(h), m--;
                for (; 0 < T - m; )
                  y = J(y), T--;
                for (; m--; ) {
                  if (h === y || y !== null && h === y.alternate) {
                    J = h;
                    break l;
                  }
                  h = J(h), y = J(y);
                }
                J = null;
              }
            else J = null;
            _ !== null && yd(
              M,
              S,
              _,
              J,
              !1
            ), G !== null && zt !== null && yd(
              M,
              zt,
              G,
              J,
              !0
            );
          }
        }
        t: {
          if (S = g ? fa(g) : window, _ = S.nodeName && S.nodeName.toLowerCase(), _ === "select" || _ === "input" && S.type === "file")
            var yt = Os;
          else if (xs(S))
            if (Ms)
              yt = ov;
            else {
              yt = sv;
              var Q = fv;
            }
          else
            _ = S.nodeName, !_ || _.toLowerCase() !== "input" || S.type !== "checkbox" && S.type !== "radio" ? g && Ni(g.elementType) && (yt = Os) : yt = rv;
          if (yt && (yt = yt(t, g))) {
            Ns(
              M,
              yt,
              e,
              z
            );
            break t;
          }
          Q && Q(t, S, g), t === "focusout" && g && S.type === "number" && g.memoizedProps.value != null && xi(S, "number", S.value);
        }
        switch (Q = g ? fa(g) : window, t) {
          case "focusin":
            (xs(Q) || Q.contentEditable === "true") && (Au = Q, Xi = g, ga = null);
            break;
          case "focusout":
            ga = Xi = Au = null;
            break;
          case "mousedown":
            Zi = !0;
            break;
          case "contextmenu":
          case "mouseup":
          case "dragend":
            Zi = !1, Ys(M, e, z);
            break;
          case "selectionchange":
            if (yv) break;
          case "keydown":
          case "keyup":
            Ys(M, e, z);
        }
        var et;
        if (Yi)
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
          zu ? qs(t, e) && (rt = "onCompositionEnd") : t === "keydown" && e.keyCode === 229 && (rt = "onCompositionStart");
        rt && (Es && e.locale !== "ko" && (zu || rt !== "onCompositionStart" ? rt === "onCompositionEnd" && zu && (et = gs()) : (pe = z, Ci = "value" in pe ? pe.value : pe.textContent, zu = !0)), Q = ui(g, rt), 0 < Q.length && (rt = new ps(
          rt,
          t,
          null,
          e,
          z
        ), M.push({ event: rt, listeners: Q }), et ? rt.data = et : (et = Ts(e), et !== null && (rt.data = et)))), (et = uv ? av(t, e) : nv(t, e)) && (rt = ui(g, "onBeforeInput"), 0 < rt.length && (Q = new ps(
          "onBeforeInput",
          "beforeinput",
          null,
          e,
          z
        ), M.push({
          event: Q,
          listeners: rt
        }), Q.data = et)), Wv(
          M,
          t,
          g,
          e,
          z
        );
      }
      od(M, l);
    });
  }
  function Xa(t, l, e) {
    return {
      instance: t,
      listener: l,
      currentTarget: e
    };
  }
  function ui(t, l) {
    for (var e = l + "Capture", u = []; t !== null; ) {
      var a = t, n = a.stateNode;
      if (a = a.tag, a !== 5 && a !== 26 && a !== 27 || n === null || (a = sa(t, e), a != null && u.unshift(
        Xa(t, a, n)
      ), a = sa(t, l), a != null && u.push(
        Xa(t, a, n)
      )), t.tag === 3) return u;
      t = t.return;
    }
    return [];
  }
  function th(t) {
    if (t === null) return null;
    do
      t = t.return;
    while (t && t.tag !== 5 && t.tag !== 27);
    return t || null;
  }
  function yd(t, l, e, u, a) {
    for (var n = l._reactName, i = []; e !== null && e !== u; ) {
      var f = e, d = f.alternate, g = f.stateNode;
      if (f = f.tag, d !== null && d === u) break;
      f !== 5 && f !== 26 && f !== 27 || g === null || (d = g, a ? (g = sa(e, n), g != null && i.unshift(
        Xa(e, g, d)
      )) : a || (g = sa(e, n), g != null && i.push(
        Xa(e, g, d)
      ))), e = e.return;
    }
    i.length !== 0 && t.push({ event: l, listeners: i });
  }
  var lh = /\r\n?/g, eh = /\u0000|\uFFFD/g;
  function vd(t) {
    return (typeof t == "string" ? t : "" + t).replace(lh, `
`).replace(eh, "");
  }
  function hd(t, l) {
    return l = vd(l), vd(t) === l;
  }
  function Et(t, l, e, u, a, n) {
    switch (e) {
      case "children":
        typeof u == "string" ? l === "body" || l === "textarea" && u === "" || pu(t, u) : (typeof u == "number" || typeof u == "bigint") && l !== "body" && pu(t, "" + u);
        break;
      case "className":
        cn(t, "class", u);
        break;
      case "tabIndex":
        cn(t, "tabindex", u);
        break;
      case "dir":
      case "role":
      case "viewBox":
      case "width":
      case "height":
        cn(t, e, u);
        break;
      case "style":
        vs(t, u, n);
        break;
      case "data":
        if (l !== "object") {
          cn(t, "data", u);
          break;
        }
      case "src":
      case "href":
        if (u === "" && (l !== "a" || e !== "href")) {
          t.removeAttribute(e);
          break;
        }
        if (u == null || typeof u == "function" || typeof u == "symbol" || typeof u == "boolean") {
          t.removeAttribute(e);
          break;
        }
        u = sn("" + u), t.setAttribute(e, u);
        break;
      case "action":
      case "formAction":
        if (typeof u == "function") {
          t.setAttribute(
            e,
            "javascript:throw new Error('A React form was unexpectedly submitted. If you called form.submit() manually, consider using form.requestSubmit() instead. If you\\'re trying to use event.stopPropagation() in a submit event handler, consider also calling event.preventDefault().')"
          );
          break;
        } else
          typeof n == "function" && (e === "formAction" ? (l !== "input" && Et(t, l, "name", a.name, a, null), Et(
            t,
            l,
            "formEncType",
            a.formEncType,
            a,
            null
          ), Et(
            t,
            l,
            "formMethod",
            a.formMethod,
            a,
            null
          ), Et(
            t,
            l,
            "formTarget",
            a.formTarget,
            a,
            null
          )) : (Et(t, l, "encType", a.encType, a, null), Et(t, l, "method", a.method, a, null), Et(t, l, "target", a.target, a, null)));
        if (u == null || typeof u == "symbol" || typeof u == "boolean") {
          t.removeAttribute(e);
          break;
        }
        u = sn("" + u), t.setAttribute(e, u);
        break;
      case "onClick":
        u != null && (t.onclick = Wl);
        break;
      case "onScroll":
        u != null && ct("scroll", t);
        break;
      case "onScrollEnd":
        u != null && ct("scrollend", t);
        break;
      case "dangerouslySetInnerHTML":
        if (u != null) {
          if (typeof u != "object" || !("__html" in u))
            throw Error(s(61));
          if (e = u.__html, e != null) {
            if (a.children != null) throw Error(s(60));
            t.innerHTML = e;
          }
        }
        break;
      case "multiple":
        t.multiple = u && typeof u != "function" && typeof u != "symbol";
        break;
      case "muted":
        t.muted = u && typeof u != "function" && typeof u != "symbol";
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
          t.removeAttribute("xlink:href");
          break;
        }
        e = sn("" + u), t.setAttributeNS(
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
        u != null && typeof u != "function" && typeof u != "symbol" ? t.setAttribute(e, "" + u) : t.removeAttribute(e);
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
        u && typeof u != "function" && typeof u != "symbol" ? t.setAttribute(e, "") : t.removeAttribute(e);
        break;
      case "capture":
      case "download":
        u === !0 ? t.setAttribute(e, "") : u !== !1 && u != null && typeof u != "function" && typeof u != "symbol" ? t.setAttribute(e, u) : t.removeAttribute(e);
        break;
      case "cols":
      case "rows":
      case "size":
      case "span":
        u != null && typeof u != "function" && typeof u != "symbol" && !isNaN(u) && 1 <= u ? t.setAttribute(e, u) : t.removeAttribute(e);
        break;
      case "rowSpan":
      case "start":
        u == null || typeof u == "function" || typeof u == "symbol" || isNaN(u) ? t.removeAttribute(e) : t.setAttribute(e, u);
        break;
      case "popover":
        ct("beforetoggle", t), ct("toggle", t), nn(t, "popover", u);
        break;
      case "xlinkActuate":
        $l(
          t,
          "http://www.w3.org/1999/xlink",
          "xlink:actuate",
          u
        );
        break;
      case "xlinkArcrole":
        $l(
          t,
          "http://www.w3.org/1999/xlink",
          "xlink:arcrole",
          u
        );
        break;
      case "xlinkRole":
        $l(
          t,
          "http://www.w3.org/1999/xlink",
          "xlink:role",
          u
        );
        break;
      case "xlinkShow":
        $l(
          t,
          "http://www.w3.org/1999/xlink",
          "xlink:show",
          u
        );
        break;
      case "xlinkTitle":
        $l(
          t,
          "http://www.w3.org/1999/xlink",
          "xlink:title",
          u
        );
        break;
      case "xlinkType":
        $l(
          t,
          "http://www.w3.org/1999/xlink",
          "xlink:type",
          u
        );
        break;
      case "xmlBase":
        $l(
          t,
          "http://www.w3.org/XML/1998/namespace",
          "xml:base",
          u
        );
        break;
      case "xmlLang":
        $l(
          t,
          "http://www.w3.org/XML/1998/namespace",
          "xml:lang",
          u
        );
        break;
      case "xmlSpace":
        $l(
          t,
          "http://www.w3.org/XML/1998/namespace",
          "xml:space",
          u
        );
        break;
      case "is":
        nn(t, "is", u);
        break;
      case "innerText":
      case "textContent":
        break;
      default:
        (!(2 < e.length) || e[0] !== "o" && e[0] !== "O" || e[1] !== "n" && e[1] !== "N") && (e = My.get(e) || e, nn(t, e, u));
    }
  }
  function hf(t, l, e, u, a, n) {
    switch (e) {
      case "style":
        vs(t, u, n);
        break;
      case "dangerouslySetInnerHTML":
        if (u != null) {
          if (typeof u != "object" || !("__html" in u))
            throw Error(s(61));
          if (e = u.__html, e != null) {
            if (a.children != null) throw Error(s(60));
            t.innerHTML = e;
          }
        }
        break;
      case "children":
        typeof u == "string" ? pu(t, u) : (typeof u == "number" || typeof u == "bigint") && pu(t, "" + u);
        break;
      case "onScroll":
        u != null && ct("scroll", t);
        break;
      case "onScrollEnd":
        u != null && ct("scrollend", t);
        break;
      case "onClick":
        u != null && (t.onclick = Wl);
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
        if (!ns.hasOwnProperty(e))
          t: {
            if (e[0] === "o" && e[1] === "n" && (a = e.endsWith("Capture"), l = e.slice(2, a ? e.length - 7 : void 0), n = t[nl] || null, n = n != null ? n[e] : null, typeof n == "function" && t.removeEventListener(l, n, a), typeof u == "function")) {
              typeof n != "function" && n !== null && (e in t ? t[e] = null : t.hasAttribute(e) && t.removeAttribute(e)), t.addEventListener(l, u, a);
              break t;
            }
            e in t ? t[e] = u : u === !0 ? t.setAttribute(e, "") : nn(t, e, u);
          }
    }
  }
  function It(t, l, e) {
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
        ct("error", t), ct("load", t);
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
                  throw Error(s(137, l));
                default:
                  Et(t, l, n, i, e, null);
              }
          }
        a && Et(t, l, "srcSet", e.srcSet, e, null), u && Et(t, l, "src", e.src, e, null);
        return;
      case "input":
        ct("invalid", t);
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
                    throw Error(s(137, l));
                  break;
                default:
                  Et(t, l, u, z, e, null);
              }
          }
        rs(
          t,
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
        ct("invalid", t), u = i = n = null;
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
                Et(t, l, a, f, e, null);
            }
        l = n, e = i, t.multiple = !!u, l != null ? Su(t, !!u, l, !1) : e != null && Su(t, !!u, e, !0);
        return;
      case "textarea":
        ct("invalid", t), n = a = u = null;
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
                Et(t, l, i, f, e, null);
            }
        ds(t, u, a, n);
        return;
      case "option":
        for (d in e)
          if (e.hasOwnProperty(d) && (u = e[d], u != null))
            switch (d) {
              case "selected":
                t.selected = u && typeof u != "function" && typeof u != "symbol";
                break;
              default:
                Et(t, l, d, u, e, null);
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
        for (u = 0; u < Qa.length; u++)
          ct(Qa[u], t);
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
        for (g in e)
          if (e.hasOwnProperty(g) && (u = e[g], u != null))
            switch (g) {
              case "children":
              case "dangerouslySetInnerHTML":
                throw Error(s(137, l));
              default:
                Et(t, l, g, u, e, null);
            }
        return;
      default:
        if (Ni(l)) {
          for (z in e)
            e.hasOwnProperty(z) && (u = e[z], u !== void 0 && hf(
              t,
              l,
              z,
              u,
              e,
              void 0
            ));
          return;
        }
    }
    for (f in e)
      e.hasOwnProperty(f) && (u = e[f], u != null && Et(t, l, f, u, e, null));
  }
  function uh(t, l, e, u) {
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
                u.hasOwnProperty(_) || Et(t, l, _, null, u, M);
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
                  throw Error(s(137, l));
                break;
              default:
                _ !== M && Et(
                  t,
                  l,
                  S,
                  _,
                  u,
                  M
                );
            }
        }
        Ti(
          t,
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
                u.hasOwnProperty(n) || Et(
                  t,
                  l,
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
                n !== d && Et(
                  t,
                  l,
                  a,
                  n,
                  u,
                  d
                );
            }
        l = f, e = i, u = _, S != null ? Su(t, !!e, S, !1) : !!u != !!e && (l != null ? Su(t, !!e, l, !0) : Su(t, !!e, e ? [] : "", !1));
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
                Et(t, l, f, null, u, a);
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
                a !== n && Et(t, l, i, a, u, n);
            }
        os(t, S, _);
        return;
      case "option":
        for (var G in e)
          if (S = e[G], e.hasOwnProperty(G) && S != null && !u.hasOwnProperty(G))
            switch (G) {
              case "selected":
                t.selected = !1;
                break;
              default:
                Et(
                  t,
                  l,
                  G,
                  null,
                  u,
                  S
                );
            }
        for (d in u)
          if (S = u[d], _ = e[d], u.hasOwnProperty(d) && S !== _ && (S != null || _ != null))
            switch (d) {
              case "selected":
                t.selected = S && typeof S != "function" && typeof S != "symbol";
                break;
              default:
                Et(
                  t,
                  l,
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
        for (var J in e)
          S = e[J], e.hasOwnProperty(J) && S != null && !u.hasOwnProperty(J) && Et(t, l, J, null, u, S);
        for (g in u)
          if (S = u[g], _ = e[g], u.hasOwnProperty(g) && S !== _ && (S != null || _ != null))
            switch (g) {
              case "children":
              case "dangerouslySetInnerHTML":
                if (S != null)
                  throw Error(s(137, l));
                break;
              default:
                Et(
                  t,
                  l,
                  g,
                  S,
                  u,
                  _
                );
            }
        return;
      default:
        if (Ni(l)) {
          for (var zt in e)
            S = e[zt], e.hasOwnProperty(zt) && S !== void 0 && !u.hasOwnProperty(zt) && hf(
              t,
              l,
              zt,
              void 0,
              u,
              S
            );
          for (z in u)
            S = u[z], _ = e[z], !u.hasOwnProperty(z) || S === _ || S === void 0 && _ === void 0 || hf(
              t,
              l,
              z,
              S,
              u,
              _
            );
          return;
        }
    }
    for (var h in e)
      S = e[h], e.hasOwnProperty(h) && S != null && !u.hasOwnProperty(h) && Et(t, l, h, null, u, S);
    for (M in u)
      S = u[M], _ = e[M], !u.hasOwnProperty(M) || S === _ || S == null && _ == null || Et(t, l, M, S, u, _);
  }
  function md(t) {
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
  function ah() {
    if (typeof performance.getEntriesByType == "function") {
      for (var t = 0, l = 0, e = performance.getEntriesByType("resource"), u = 0; u < e.length; u++) {
        var a = e[u], n = a.transferSize, i = a.initiatorType, f = a.duration;
        if (n && f && md(i)) {
          for (i = 0, f = a.responseEnd, u += 1; u < e.length; u++) {
            var d = e[u], g = d.startTime;
            if (g > f) break;
            var z = d.transferSize, M = d.initiatorType;
            z && md(M) && (d = d.responseEnd, i += z * (d < f ? 1 : (f - g) / (d - g)));
          }
          if (--u, l += 8 * (n + i) / (a.duration / 1e3), t++, 10 < t) break;
        }
      }
      if (0 < t) return l / t / 1e6;
    }
    return navigator.connection && (t = navigator.connection.downlink, typeof t == "number") ? t : 5;
  }
  var mf = null, gf = null;
  function ai(t) {
    return t.nodeType === 9 ? t : t.ownerDocument;
  }
  function gd(t) {
    switch (t) {
      case "http://www.w3.org/2000/svg":
        return 1;
      case "http://www.w3.org/1998/Math/MathML":
        return 2;
      default:
        return 0;
    }
  }
  function bd(t, l) {
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
  function bf(t, l) {
    return t === "textarea" || t === "noscript" || typeof l.children == "string" || typeof l.children == "number" || typeof l.children == "bigint" || typeof l.dangerouslySetInnerHTML == "object" && l.dangerouslySetInnerHTML !== null && l.dangerouslySetInnerHTML.__html != null;
  }
  var Sf = null;
  function nh() {
    var t = window.event;
    return t && t.type === "popstate" ? t === Sf ? !1 : (Sf = t, !0) : (Sf = null, !1);
  }
  var Sd = typeof setTimeout == "function" ? setTimeout : void 0, ih = typeof clearTimeout == "function" ? clearTimeout : void 0, pd = typeof Promise == "function" ? Promise : void 0, ch = typeof queueMicrotask == "function" ? queueMicrotask : typeof pd < "u" ? function(t) {
    return pd.resolve(null).then(t).catch(fh);
  } : Sd;
  function fh(t) {
    setTimeout(function() {
      throw t;
    });
  }
  function Be(t) {
    return t === "head";
  }
  function _d(t, l) {
    var e = l, u = 0;
    do {
      var a = e.nextSibling;
      if (t.removeChild(e), a && a.nodeType === 8)
        if (e = a.data, e === "/$" || e === "/&") {
          if (u === 0) {
            t.removeChild(a), Fu(l);
            return;
          }
          u--;
        } else if (e === "$" || e === "$?" || e === "$~" || e === "$!" || e === "&")
          u++;
        else if (e === "html")
          Za(t.ownerDocument.documentElement);
        else if (e === "head") {
          e = t.ownerDocument.head, Za(e);
          for (var n = e.firstChild; n; ) {
            var i = n.nextSibling, f = n.nodeName;
            n[ca] || f === "SCRIPT" || f === "STYLE" || f === "LINK" && n.rel.toLowerCase() === "stylesheet" || e.removeChild(n), n = i;
          }
        } else
          e === "body" && Za(t.ownerDocument.body);
      e = a;
    } while (e);
    Fu(l);
  }
  function Ed(t, l) {
    var e = t;
    t = 0;
    do {
      var u = e.nextSibling;
      if (e.nodeType === 1 ? l ? (e._stashedDisplay = e.style.display, e.style.display = "none") : (e.style.display = e._stashedDisplay || "", e.getAttribute("style") === "" && e.removeAttribute("style")) : e.nodeType === 3 && (l ? (e._stashedText = e.nodeValue, e.nodeValue = "") : e.nodeValue = e._stashedText || ""), u && u.nodeType === 8)
        if (e = u.data, e === "/$") {
          if (t === 0) break;
          t--;
        } else
          e !== "$" && e !== "$?" && e !== "$~" && e !== "$!" || t++;
      e = u;
    } while (e);
  }
  function pf(t) {
    var l = t.firstChild;
    for (l && l.nodeType === 10 && (l = l.nextSibling); l; ) {
      var e = l;
      switch (l = l.nextSibling, e.nodeName) {
        case "HTML":
        case "HEAD":
        case "BODY":
          pf(e), Ai(e);
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
  function sh(t, l, e, u) {
    for (; t.nodeType === 1; ) {
      var a = e;
      if (t.nodeName.toLowerCase() !== l.toLowerCase()) {
        if (!u && (t.nodeName !== "INPUT" || t.type !== "hidden"))
          break;
      } else if (u) {
        if (!t[ca])
          switch (l) {
            case "meta":
              if (!t.hasAttribute("itemprop")) break;
              return t;
            case "link":
              if (n = t.getAttribute("rel"), n === "stylesheet" && t.hasAttribute("data-precedence"))
                break;
              if (n !== a.rel || t.getAttribute("href") !== (a.href == null || a.href === "" ? null : a.href) || t.getAttribute("crossorigin") !== (a.crossOrigin == null ? null : a.crossOrigin) || t.getAttribute("title") !== (a.title == null ? null : a.title))
                break;
              return t;
            case "style":
              if (t.hasAttribute("data-precedence")) break;
              return t;
            case "script":
              if (n = t.getAttribute("src"), (n !== (a.src == null ? null : a.src) || t.getAttribute("type") !== (a.type == null ? null : a.type) || t.getAttribute("crossorigin") !== (a.crossOrigin == null ? null : a.crossOrigin)) && n && t.hasAttribute("async") && !t.hasAttribute("itemprop"))
                break;
              return t;
            default:
              return t;
          }
      } else if (l === "input" && t.type === "hidden") {
        var n = a.name == null ? null : "" + a.name;
        if (a.type === "hidden" && t.getAttribute("name") === n)
          return t;
      } else return t;
      if (t = Ul(t.nextSibling), t === null) break;
    }
    return null;
  }
  function rh(t, l, e) {
    if (l === "") return null;
    for (; t.nodeType !== 3; )
      if ((t.nodeType !== 1 || t.nodeName !== "INPUT" || t.type !== "hidden") && !e || (t = Ul(t.nextSibling), t === null)) return null;
    return t;
  }
  function zd(t, l) {
    for (; t.nodeType !== 8; )
      if ((t.nodeType !== 1 || t.nodeName !== "INPUT" || t.type !== "hidden") && !l || (t = Ul(t.nextSibling), t === null)) return null;
    return t;
  }
  function _f(t) {
    return t.data === "$?" || t.data === "$~";
  }
  function Ef(t) {
    return t.data === "$!" || t.data === "$?" && t.ownerDocument.readyState !== "loading";
  }
  function oh(t, l) {
    var e = t.ownerDocument;
    if (t.data === "$~") t._reactRetry = l;
    else if (t.data !== "$?" || e.readyState !== "loading")
      l();
    else {
      var u = function() {
        l(), e.removeEventListener("DOMContentLoaded", u);
      };
      e.addEventListener("DOMContentLoaded", u), t._reactRetry = u;
    }
  }
  function Ul(t) {
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
  var zf = null;
  function Ad(t) {
    t = t.nextSibling;
    for (var l = 0; t; ) {
      if (t.nodeType === 8) {
        var e = t.data;
        if (e === "/$" || e === "/&") {
          if (l === 0)
            return Ul(t.nextSibling);
          l--;
        } else
          e !== "$" && e !== "$!" && e !== "$?" && e !== "$~" && e !== "&" || l++;
      }
      t = t.nextSibling;
    }
    return null;
  }
  function qd(t) {
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
  function Td(t, l, e) {
    switch (l = ai(e), t) {
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
  function Za(t) {
    for (var l = t.attributes; l.length; )
      t.removeAttributeNode(l[0]);
    Ai(t);
  }
  var Cl = /* @__PURE__ */ new Map(), xd = /* @__PURE__ */ new Set();
  function ni(t) {
    return typeof t.getRootNode == "function" ? t.getRootNode() : t.nodeType === 9 ? t : t.ownerDocument;
  }
  var de = H.d;
  H.d = {
    f: dh,
    r: yh,
    D: vh,
    C: hh,
    L: mh,
    m: gh,
    X: Sh,
    S: bh,
    M: ph
  };
  function dh() {
    var t = de.f(), l = Wn();
    return t || l;
  }
  function yh(t) {
    var l = mu(t);
    l !== null && l.tag === 5 && l.type === "form" ? Zr(l) : de.r(t);
  }
  var ku = typeof document > "u" ? null : document;
  function Nd(t, l, e) {
    var u = ku;
    if (u && typeof l == "string" && l) {
      var a = ql(l);
      a = 'link[rel="' + t + '"][href="' + a + '"]', typeof e == "string" && (a += '[crossorigin="' + e + '"]'), xd.has(a) || (xd.add(a), t = { rel: t, crossOrigin: e, href: l }, u.querySelector(a) === null && (l = u.createElement("link"), It(l, "link", t), Zt(l), u.head.appendChild(l)));
    }
  }
  function vh(t) {
    de.D(t), Nd("dns-prefetch", t, null);
  }
  function hh(t, l) {
    de.C(t, l), Nd("preconnect", t, l);
  }
  function mh(t, l, e) {
    de.L(t, l, e);
    var u = ku;
    if (u && t && l) {
      var a = 'link[rel="preload"][as="' + ql(l) + '"]';
      l === "image" && e && e.imageSrcSet ? (a += '[imagesrcset="' + ql(
        e.imageSrcSet
      ) + '"]', typeof e.imageSizes == "string" && (a += '[imagesizes="' + ql(
        e.imageSizes
      ) + '"]')) : a += '[href="' + ql(t) + '"]';
      var n = a;
      switch (l) {
        case "style":
          n = $u(t);
          break;
        case "script":
          n = Wu(t);
      }
      Cl.has(n) || (t = p(
        {
          rel: "preload",
          href: l === "image" && e && e.imageSrcSet ? void 0 : t,
          as: l
        },
        e
      ), Cl.set(n, t), u.querySelector(a) !== null || l === "style" && u.querySelector(Va(n)) || l === "script" && u.querySelector(Ka(n)) || (l = u.createElement("link"), It(l, "link", t), Zt(l), u.head.appendChild(l)));
    }
  }
  function gh(t, l) {
    de.m(t, l);
    var e = ku;
    if (e && t) {
      var u = l && typeof l.as == "string" ? l.as : "script", a = 'link[rel="modulepreload"][as="' + ql(u) + '"][href="' + ql(t) + '"]', n = a;
      switch (u) {
        case "audioworklet":
        case "paintworklet":
        case "serviceworker":
        case "sharedworker":
        case "worker":
        case "script":
          n = Wu(t);
      }
      if (!Cl.has(n) && (t = p({ rel: "modulepreload", href: t }, l), Cl.set(n, t), e.querySelector(a) === null)) {
        switch (u) {
          case "audioworklet":
          case "paintworklet":
          case "serviceworker":
          case "sharedworker":
          case "worker":
          case "script":
            if (e.querySelector(Ka(n)))
              return;
        }
        u = e.createElement("link"), It(u, "link", t), Zt(u), e.head.appendChild(u);
      }
    }
  }
  function bh(t, l, e) {
    de.S(t, l, e);
    var u = ku;
    if (u && t) {
      var a = gu(u).hoistableStyles, n = $u(t);
      l = l || "default";
      var i = a.get(n);
      if (!i) {
        var f = { loading: 0, preload: null };
        if (i = u.querySelector(
          Va(n)
        ))
          f.loading = 5;
        else {
          t = p(
            { rel: "stylesheet", href: t, "data-precedence": l },
            e
          ), (e = Cl.get(n)) && Af(t, e);
          var d = i = u.createElement("link");
          Zt(d), It(d, "link", t), d._p = new Promise(function(g, z) {
            d.onload = g, d.onerror = z;
          }), d.addEventListener("load", function() {
            f.loading |= 1;
          }), d.addEventListener("error", function() {
            f.loading |= 2;
          }), f.loading |= 4, ii(i, l, u);
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
  function Sh(t, l) {
    de.X(t, l);
    var e = ku;
    if (e && t) {
      var u = gu(e).hoistableScripts, a = Wu(t), n = u.get(a);
      n || (n = e.querySelector(Ka(a)), n || (t = p({ src: t, async: !0 }, l), (l = Cl.get(a)) && qf(t, l), n = e.createElement("script"), Zt(n), It(n, "link", t), e.head.appendChild(n)), n = {
        type: "script",
        instance: n,
        count: 1,
        state: null
      }, u.set(a, n));
    }
  }
  function ph(t, l) {
    de.M(t, l);
    var e = ku;
    if (e && t) {
      var u = gu(e).hoistableScripts, a = Wu(t), n = u.get(a);
      n || (n = e.querySelector(Ka(a)), n || (t = p({ src: t, async: !0, type: "module" }, l), (l = Cl.get(a)) && qf(t, l), n = e.createElement("script"), Zt(n), It(n, "link", t), e.head.appendChild(n)), n = {
        type: "script",
        instance: n,
        count: 1,
        state: null
      }, u.set(a, n));
    }
  }
  function Od(t, l, e, u) {
    var a = (a = at.current) ? ni(a) : null;
    if (!a) throw Error(s(446));
    switch (t) {
      case "meta":
      case "title":
        return null;
      case "style":
        return typeof e.precedence == "string" && typeof e.href == "string" ? (l = $u(e.href), e = gu(
          a
        ).hoistableStyles, u = e.get(l), u || (u = {
          type: "style",
          instance: null,
          count: 0,
          state: null
        }, e.set(l, u)), u) : { type: "void", instance: null, count: 0, state: null };
      case "link":
        if (e.rel === "stylesheet" && typeof e.href == "string" && typeof e.precedence == "string") {
          t = $u(e.href);
          var n = gu(
            a
          ).hoistableStyles, i = n.get(t);
          if (i || (a = a.ownerDocument || a, i = {
            type: "stylesheet",
            instance: null,
            count: 0,
            state: { loading: 0, preload: null }
          }, n.set(t, i), (n = a.querySelector(
            Va(t)
          )) && !n._p && (i.instance = n, i.state.loading = 5), Cl.has(t) || (e = {
            rel: "preload",
            as: "style",
            href: e.href,
            crossOrigin: e.crossOrigin,
            integrity: e.integrity,
            media: e.media,
            hrefLang: e.hrefLang,
            referrerPolicy: e.referrerPolicy
          }, Cl.set(t, e), n || _h(
            a,
            t,
            e,
            i.state
          ))), l && u === null)
            throw Error(s(528, ""));
          return i;
        }
        if (l && u !== null)
          throw Error(s(529, ""));
        return null;
      case "script":
        return l = e.async, e = e.src, typeof e == "string" && l && typeof l != "function" && typeof l != "symbol" ? (l = Wu(e), e = gu(
          a
        ).hoistableScripts, u = e.get(l), u || (u = {
          type: "script",
          instance: null,
          count: 0,
          state: null
        }, e.set(l, u)), u) : { type: "void", instance: null, count: 0, state: null };
      default:
        throw Error(s(444, t));
    }
  }
  function $u(t) {
    return 'href="' + ql(t) + '"';
  }
  function Va(t) {
    return 'link[rel="stylesheet"][' + t + "]";
  }
  function Md(t) {
    return p({}, t, {
      "data-precedence": t.precedence,
      precedence: null
    });
  }
  function _h(t, l, e, u) {
    t.querySelector('link[rel="preload"][as="style"][' + l + "]") ? u.loading = 1 : (l = t.createElement("link"), u.preload = l, l.addEventListener("load", function() {
      return u.loading |= 1;
    }), l.addEventListener("error", function() {
      return u.loading |= 2;
    }), It(l, "link", e), Zt(l), t.head.appendChild(l));
  }
  function Wu(t) {
    return '[src="' + ql(t) + '"]';
  }
  function Ka(t) {
    return "script[async]" + t;
  }
  function Dd(t, l, e) {
    if (l.count++, l.instance === null)
      switch (l.type) {
        case "style":
          var u = t.querySelector(
            'style[data-href~="' + ql(e.href) + '"]'
          );
          if (u)
            return l.instance = u, Zt(u), u;
          var a = p({}, e, {
            "data-href": e.href,
            "data-precedence": e.precedence,
            href: null,
            precedence: null
          });
          return u = (t.ownerDocument || t).createElement(
            "style"
          ), Zt(u), It(u, "style", a), ii(u, e.precedence, t), l.instance = u;
        case "stylesheet":
          a = $u(e.href);
          var n = t.querySelector(
            Va(a)
          );
          if (n)
            return l.state.loading |= 4, l.instance = n, Zt(n), n;
          u = Md(e), (a = Cl.get(a)) && Af(u, a), n = (t.ownerDocument || t).createElement("link"), Zt(n);
          var i = n;
          return i._p = new Promise(function(f, d) {
            i.onload = f, i.onerror = d;
          }), It(n, "link", u), l.state.loading |= 4, ii(n, e.precedence, t), l.instance = n;
        case "script":
          return n = Wu(e.src), (a = t.querySelector(
            Ka(n)
          )) ? (l.instance = a, Zt(a), a) : (u = e, (a = Cl.get(n)) && (u = p({}, e), qf(u, a)), t = t.ownerDocument || t, a = t.createElement("script"), Zt(a), It(a, "link", u), t.head.appendChild(a), l.instance = a);
        case "void":
          return null;
        default:
          throw Error(s(443, l.type));
      }
    else
      l.type === "stylesheet" && (l.state.loading & 4) === 0 && (u = l.instance, l.state.loading |= 4, ii(u, e.precedence, t));
    return l.instance;
  }
  function ii(t, l, e) {
    for (var u = e.querySelectorAll(
      'link[rel="stylesheet"][data-precedence],style[data-precedence]'
    ), a = u.length ? u[u.length - 1] : null, n = a, i = 0; i < u.length; i++) {
      var f = u[i];
      if (f.dataset.precedence === l) n = f;
      else if (n !== a) break;
    }
    n ? n.parentNode.insertBefore(t, n.nextSibling) : (l = e.nodeType === 9 ? e.head : e, l.insertBefore(t, l.firstChild));
  }
  function Af(t, l) {
    t.crossOrigin == null && (t.crossOrigin = l.crossOrigin), t.referrerPolicy == null && (t.referrerPolicy = l.referrerPolicy), t.title == null && (t.title = l.title);
  }
  function qf(t, l) {
    t.crossOrigin == null && (t.crossOrigin = l.crossOrigin), t.referrerPolicy == null && (t.referrerPolicy = l.referrerPolicy), t.integrity == null && (t.integrity = l.integrity);
  }
  var ci = null;
  function Ud(t, l, e) {
    if (ci === null) {
      var u = /* @__PURE__ */ new Map(), a = ci = /* @__PURE__ */ new Map();
      a.set(e, u);
    } else
      a = ci, u = a.get(e), u || (u = /* @__PURE__ */ new Map(), a.set(e, u));
    if (u.has(t)) return u;
    for (u.set(t, null), e = e.getElementsByTagName(t), a = 0; a < e.length; a++) {
      var n = e[a];
      if (!(n[ca] || n[kt] || t === "link" && n.getAttribute("rel") === "stylesheet") && n.namespaceURI !== "http://www.w3.org/2000/svg") {
        var i = n.getAttribute(l) || "";
        i = t + i;
        var f = u.get(i);
        f ? f.push(n) : u.set(i, [n]);
      }
    }
    return u;
  }
  function Cd(t, l, e) {
    t = t.ownerDocument || t, t.head.insertBefore(
      e,
      l === "title" ? t.querySelector("head > title") : null
    );
  }
  function Eh(t, l, e) {
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
  function jd(t) {
    return !(t.type === "stylesheet" && (t.state.loading & 3) === 0);
  }
  function zh(t, l, e, u) {
    if (e.type === "stylesheet" && (typeof u.media != "string" || matchMedia(u.media).matches !== !1) && (e.state.loading & 4) === 0) {
      if (e.instance === null) {
        var a = $u(u.href), n = l.querySelector(
          Va(a)
        );
        if (n) {
          l = n._p, l !== null && typeof l == "object" && typeof l.then == "function" && (t.count++, t = fi.bind(t), l.then(t, t)), e.state.loading |= 4, e.instance = n, Zt(n);
          return;
        }
        n = l.ownerDocument || l, u = Md(u), (a = Cl.get(a)) && Af(u, a), n = n.createElement("link"), Zt(n);
        var i = n;
        i._p = new Promise(function(f, d) {
          i.onload = f, i.onerror = d;
        }), It(n, "link", u), e.instance = n;
      }
      t.stylesheets === null && (t.stylesheets = /* @__PURE__ */ new Map()), t.stylesheets.set(e, l), (l = e.state.preload) && (e.state.loading & 3) === 0 && (t.count++, e = fi.bind(t), l.addEventListener("load", e), l.addEventListener("error", e));
    }
  }
  var Tf = 0;
  function Ah(t, l) {
    return t.stylesheets && t.count === 0 && ri(t, t.stylesheets), 0 < t.count || 0 < t.imgCount ? function(e) {
      var u = setTimeout(function() {
        if (t.stylesheets && ri(t, t.stylesheets), t.unsuspend) {
          var n = t.unsuspend;
          t.unsuspend = null, n();
        }
      }, 6e4 + l);
      0 < t.imgBytes && Tf === 0 && (Tf = 62500 * ah());
      var a = setTimeout(
        function() {
          if (t.waitingForImages = !1, t.count === 0 && (t.stylesheets && ri(t, t.stylesheets), t.unsuspend)) {
            var n = t.unsuspend;
            t.unsuspend = null, n();
          }
        },
        (t.imgBytes > Tf ? 50 : 800) + l
      );
      return t.unsuspend = e, function() {
        t.unsuspend = null, clearTimeout(u), clearTimeout(a);
      };
    } : null;
  }
  function fi() {
    if (this.count--, this.count === 0 && (this.imgCount === 0 || !this.waitingForImages)) {
      if (this.stylesheets) ri(this, this.stylesheets);
      else if (this.unsuspend) {
        var t = this.unsuspend;
        this.unsuspend = null, t();
      }
    }
  }
  var si = null;
  function ri(t, l) {
    t.stylesheets = null, t.unsuspend !== null && (t.count++, si = /* @__PURE__ */ new Map(), l.forEach(qh, t), si = null, fi.call(t));
  }
  function qh(t, l) {
    if (!(l.state.loading & 4)) {
      var e = si.get(t);
      if (e) var u = e.get(null);
      else {
        e = /* @__PURE__ */ new Map(), si.set(t, e);
        for (var a = t.querySelectorAll(
          "link[data-precedence],style[data-precedence]"
        ), n = 0; n < a.length; n++) {
          var i = a[n];
          (i.nodeName === "LINK" || i.getAttribute("media") !== "not all") && (e.set(i.dataset.precedence, i), u = i);
        }
        u && e.set(null, u);
      }
      a = l.instance, i = a.getAttribute("data-precedence"), n = e.get(i) || u, n === u && e.set(null, a), e.set(i, a), this.count++, u = fi.bind(this), a.addEventListener("load", u), a.addEventListener("error", u), n ? n.parentNode.insertBefore(a, n.nextSibling) : (t = t.nodeType === 9 ? t.head : t, t.insertBefore(a, t.firstChild)), l.state.loading |= 4;
    }
  }
  var Ja = {
    $$typeof: w,
    Provider: null,
    Consumer: null,
    _currentValue: Z,
    _currentValue2: Z,
    _threadCount: 0
  };
  function Th(t, l, e, u, a, n, i, f, d) {
    this.tag = 1, this.containerInfo = t, this.pingCache = this.current = this.pendingChildren = null, this.timeoutHandle = -1, this.callbackNode = this.next = this.pendingContext = this.context = this.cancelPendingCommit = null, this.callbackPriority = 0, this.expirationTimes = pi(-1), this.entangledLanes = this.shellSuspendCounter = this.errorRecoveryDisabledLanes = this.expiredLanes = this.warmLanes = this.pingedLanes = this.suspendedLanes = this.pendingLanes = 0, this.entanglements = pi(0), this.hiddenUpdates = pi(null), this.identifierPrefix = u, this.onUncaughtError = a, this.onCaughtError = n, this.onRecoverableError = i, this.pooledCache = null, this.pooledCacheLanes = 0, this.formState = d, this.incompleteTransitions = /* @__PURE__ */ new Map();
  }
  function Rd(t, l, e, u, a, n, i, f, d, g, z, M) {
    return t = new Th(
      t,
      l,
      e,
      i,
      d,
      g,
      z,
      M,
      f
    ), l = 1, n === !0 && (l |= 24), n = gl(3, null, null, l), t.current = n, n.stateNode = t, l = ac(), l.refCount++, t.pooledCache = l, l.refCount++, n.memoizedState = {
      element: u,
      isDehydrated: e,
      cache: l
    }, fc(n), t;
  }
  function Hd(t) {
    return t ? (t = xu, t) : xu;
  }
  function Bd(t, l, e, u, a, n) {
    a = Hd(a), u.context === null ? u.context = a : u.pendingContext = a, u = Te(l), u.payload = { element: e }, n = n === void 0 ? null : n, n !== null && (u.callback = n), e = xe(t, u, l), e !== null && (ol(e, t, l), Aa(e, t, l));
  }
  function Yd(t, l) {
    if (t = t.memoizedState, t !== null && t.dehydrated !== null) {
      var e = t.retryLane;
      t.retryLane = e !== 0 && e < l ? e : l;
    }
  }
  function xf(t, l) {
    Yd(t, l), (t = t.alternate) && Yd(t, l);
  }
  function Ld(t) {
    if (t.tag === 13 || t.tag === 31) {
      var l = Fe(t, 67108864);
      l !== null && ol(l, t, 67108864), xf(t, 67108864);
    }
  }
  function Gd(t) {
    if (t.tag === 13 || t.tag === 31) {
      var l = El();
      l = _i(l);
      var e = Fe(t, l);
      e !== null && ol(e, t, l), xf(t, l);
    }
  }
  var oi = !0;
  function xh(t, l, e, u) {
    var a = A.T;
    A.T = null;
    var n = H.p;
    try {
      H.p = 2, Nf(t, l, e, u);
    } finally {
      H.p = n, A.T = a;
    }
  }
  function Nh(t, l, e, u) {
    var a = A.T;
    A.T = null;
    var n = H.p;
    try {
      H.p = 8, Nf(t, l, e, u);
    } finally {
      H.p = n, A.T = a;
    }
  }
  function Nf(t, l, e, u) {
    if (oi) {
      var a = Of(u);
      if (a === null)
        vf(
          t,
          l,
          u,
          di,
          e
        ), Xd(t, u);
      else if (Mh(
        a,
        t,
        l,
        e,
        u
      ))
        u.stopPropagation();
      else if (Xd(t, u), l & 4 && -1 < Oh.indexOf(t)) {
        for (; a !== null; ) {
          var n = mu(a);
          if (n !== null)
            switch (n.tag) {
              case 3:
                if (n = n.stateNode, n.current.memoizedState.isDehydrated) {
                  var i = Je(n.pendingLanes);
                  if (i !== 0) {
                    var f = n;
                    for (f.pendingLanes |= 2, f.entangledLanes |= 2; i; ) {
                      var d = 1 << 31 - hl(i);
                      f.entanglements[1] |= d, i &= ~d;
                    }
                    Vl(n), (mt & 6) === 0 && (kn = yl() + 500, Ga(0));
                  }
                }
                break;
              case 31:
              case 13:
                f = Fe(n, 2), f !== null && ol(f, n, 2), Wn(), xf(n, 2);
            }
          if (n = Of(u), n === null && vf(
            t,
            l,
            u,
            di,
            e
          ), n === a) break;
          a = n;
        }
        a !== null && u.stopPropagation();
      } else
        vf(
          t,
          l,
          u,
          null,
          e
        );
    }
  }
  function Of(t) {
    return t = Mi(t), Mf(t);
  }
  var di = null;
  function Mf(t) {
    if (di = null, t = hu(t), t !== null) {
      var l = E(t);
      if (l === null) t = null;
      else {
        var e = l.tag;
        if (e === 13) {
          if (t = C(l), t !== null) return t;
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
    return di = t, null;
  }
  function Qd(t) {
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
        switch (vy()) {
          case kf:
            return 2;
          case $f:
            return 8;
          case tn:
          case hy:
            return 32;
          case Wf:
            return 268435456;
          default:
            return 32;
        }
      default:
        return 32;
    }
  }
  var Df = !1, Ye = null, Le = null, Ge = null, wa = /* @__PURE__ */ new Map(), ka = /* @__PURE__ */ new Map(), Qe = [], Oh = "mousedown mouseup touchcancel touchend touchstart auxclick dblclick pointercancel pointerdown pointerup dragend dragstart drop compositionend compositionstart keydown keypress keyup input textInput copy cut paste click change contextmenu reset".split(
    " "
  );
  function Xd(t, l) {
    switch (t) {
      case "focusin":
      case "focusout":
        Ye = null;
        break;
      case "dragenter":
      case "dragleave":
        Le = null;
        break;
      case "mouseover":
      case "mouseout":
        Ge = null;
        break;
      case "pointerover":
      case "pointerout":
        wa.delete(l.pointerId);
        break;
      case "gotpointercapture":
      case "lostpointercapture":
        ka.delete(l.pointerId);
    }
  }
  function $a(t, l, e, u, a, n) {
    return t === null || t.nativeEvent !== n ? (t = {
      blockedOn: l,
      domEventName: e,
      eventSystemFlags: u,
      nativeEvent: n,
      targetContainers: [a]
    }, l !== null && (l = mu(l), l !== null && Ld(l)), t) : (t.eventSystemFlags |= u, l = t.targetContainers, a !== null && l.indexOf(a) === -1 && l.push(a), t);
  }
  function Mh(t, l, e, u, a) {
    switch (l) {
      case "focusin":
        return Ye = $a(
          Ye,
          t,
          l,
          e,
          u,
          a
        ), !0;
      case "dragenter":
        return Le = $a(
          Le,
          t,
          l,
          e,
          u,
          a
        ), !0;
      case "mouseover":
        return Ge = $a(
          Ge,
          t,
          l,
          e,
          u,
          a
        ), !0;
      case "pointerover":
        var n = a.pointerId;
        return wa.set(
          n,
          $a(
            wa.get(n) || null,
            t,
            l,
            e,
            u,
            a
          )
        ), !0;
      case "gotpointercapture":
        return n = a.pointerId, ka.set(
          n,
          $a(
            ka.get(n) || null,
            t,
            l,
            e,
            u,
            a
          )
        ), !0;
    }
    return !1;
  }
  function Zd(t) {
    var l = hu(t.target);
    if (l !== null) {
      var e = E(l);
      if (e !== null) {
        if (l = e.tag, l === 13) {
          if (l = C(e), l !== null) {
            t.blockedOn = l, es(t.priority, function() {
              Gd(e);
            });
            return;
          }
        } else if (l === 31) {
          if (l = U(e), l !== null) {
            t.blockedOn = l, es(t.priority, function() {
              Gd(e);
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
  function yi(t) {
    if (t.blockedOn !== null) return !1;
    for (var l = t.targetContainers; 0 < l.length; ) {
      var e = Of(t.nativeEvent);
      if (e === null) {
        e = t.nativeEvent;
        var u = new e.constructor(
          e.type,
          e
        );
        Oi = u, e.target.dispatchEvent(u), Oi = null;
      } else
        return l = mu(e), l !== null && Ld(l), t.blockedOn = e, !1;
      l.shift();
    }
    return !0;
  }
  function Vd(t, l, e) {
    yi(t) && e.delete(l);
  }
  function Dh() {
    Df = !1, Ye !== null && yi(Ye) && (Ye = null), Le !== null && yi(Le) && (Le = null), Ge !== null && yi(Ge) && (Ge = null), wa.forEach(Vd), ka.forEach(Vd);
  }
  function vi(t, l) {
    t.blockedOn === l && (t.blockedOn = null, Df || (Df = !0, c.unstable_scheduleCallback(
      c.unstable_NormalPriority,
      Dh
    )));
  }
  var hi = null;
  function Kd(t) {
    hi !== t && (hi = t, c.unstable_scheduleCallback(
      c.unstable_NormalPriority,
      function() {
        hi === t && (hi = null);
        for (var l = 0; l < t.length; l += 3) {
          var e = t[l], u = t[l + 1], a = t[l + 2];
          if (typeof u != "function") {
            if (Mf(u || e) === null)
              continue;
            break;
          }
          var n = mu(e);
          n !== null && (t.splice(l, 3), l -= 3, Nc(
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
  function Fu(t) {
    function l(d) {
      return vi(d, t);
    }
    Ye !== null && vi(Ye, t), Le !== null && vi(Le, t), Ge !== null && vi(Ge, t), wa.forEach(l), ka.forEach(l);
    for (var e = 0; e < Qe.length; e++) {
      var u = Qe[e];
      u.blockedOn === t && (u.blockedOn = null);
    }
    for (; 0 < Qe.length && (e = Qe[0], e.blockedOn === null); )
      Zd(e), e.blockedOn === null && Qe.shift();
    if (e = (t.ownerDocument || t).$$reactFormReplay, e != null)
      for (u = 0; u < e.length; u += 3) {
        var a = e[u], n = e[u + 1], i = a[nl] || null;
        if (typeof n == "function")
          i || Kd(e);
        else if (i) {
          var f = null;
          if (n && n.hasAttribute("formAction")) {
            if (a = n, i = n[nl] || null)
              f = i.formAction;
            else if (Mf(a) !== null) continue;
          } else f = i.action;
          typeof f == "function" ? e[u + 1] = f : (e.splice(u, 3), u -= 3), Kd(e);
        }
      }
  }
  function Jd() {
    function t(n) {
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
    function l() {
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
      return navigation.addEventListener("navigate", t), navigation.addEventListener("navigatesuccess", l), navigation.addEventListener("navigateerror", l), setTimeout(e, 100), function() {
        u = !0, navigation.removeEventListener("navigate", t), navigation.removeEventListener("navigatesuccess", l), navigation.removeEventListener("navigateerror", l), a !== null && (a(), a = null);
      };
    }
  }
  function Uf(t) {
    this._internalRoot = t;
  }
  mi.prototype.render = Uf.prototype.render = function(t) {
    var l = this._internalRoot;
    if (l === null) throw Error(s(409));
    var e = l.current, u = El();
    Bd(e, u, t, l, null, null);
  }, mi.prototype.unmount = Uf.prototype.unmount = function() {
    var t = this._internalRoot;
    if (t !== null) {
      this._internalRoot = null;
      var l = t.containerInfo;
      Bd(t.current, 2, null, t, null, null), Wn(), l[vu] = null;
    }
  };
  function mi(t) {
    this._internalRoot = t;
  }
  mi.prototype.unstable_scheduleHydration = function(t) {
    if (t) {
      var l = ls();
      t = { blockedOn: null, target: t, priority: l };
      for (var e = 0; e < Qe.length && l !== 0 && l < Qe[e].priority; e++) ;
      Qe.splice(e, 0, t), e === 0 && Zd(t);
    }
  };
  var wd = o.version;
  if (wd !== "19.2.5")
    throw Error(
      s(
        527,
        wd,
        "19.2.5"
      )
    );
  H.findDOMNode = function(t) {
    var l = t._reactInternals;
    if (l === void 0)
      throw typeof t.render == "function" ? Error(s(188)) : (t = Object.keys(t).join(","), Error(s(268, t)));
    return t = b(l), t = t !== null ? j(t) : null, t = t === null ? null : t.stateNode, t;
  };
  var Uh = {
    bundleType: 0,
    version: "19.2.5",
    rendererPackageName: "react-dom",
    currentDispatcherRef: A,
    reconcilerVersion: "19.2.5"
  };
  if (typeof __REACT_DEVTOOLS_GLOBAL_HOOK__ < "u") {
    var gi = __REACT_DEVTOOLS_GLOBAL_HOOK__;
    if (!gi.isDisabled && gi.supportsFiber)
      try {
        aa = gi.inject(
          Uh
        ), vl = gi;
      } catch {
      }
  }
  return Wa.createRoot = function(t, l) {
    if (!q(t)) throw Error(s(299));
    var e = !1, u = "", a = Pr, n = to, i = lo;
    return l != null && (l.unstable_strictMode === !0 && (e = !0), l.identifierPrefix !== void 0 && (u = l.identifierPrefix), l.onUncaughtError !== void 0 && (a = l.onUncaughtError), l.onCaughtError !== void 0 && (n = l.onCaughtError), l.onRecoverableError !== void 0 && (i = l.onRecoverableError)), l = Rd(
      t,
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
      Jd
    ), t[vu] = l.current, yf(t), new Uf(l);
  }, Wa.hydrateRoot = function(t, l, e) {
    if (!q(t)) throw Error(s(299));
    var u = !1, a = "", n = Pr, i = to, f = lo, d = null;
    return e != null && (e.unstable_strictMode === !0 && (u = !0), e.identifierPrefix !== void 0 && (a = e.identifierPrefix), e.onUncaughtError !== void 0 && (n = e.onUncaughtError), e.onCaughtError !== void 0 && (i = e.onCaughtError), e.onRecoverableError !== void 0 && (f = e.onRecoverableError), e.formState !== void 0 && (d = e.formState)), l = Rd(
      t,
      1,
      !0,
      l,
      e ?? null,
      u,
      a,
      d,
      n,
      i,
      f,
      Jd
    ), l.context = Hd(null), e = l.current, u = El(), u = _i(u), a = Te(u), a.callback = null, xe(e, a, u), e = u, l.current.lanes = e, ia(l, e), Vl(l), t[vu] = l.current, yf(t), new mi(l);
  }, Wa.version = "19.2.5", Wa;
}
var ey;
function Xh() {
  if (ey) return Rf.exports;
  ey = 1;
  function c() {
    if (!(typeof __REACT_DEVTOOLS_GLOBAL_HOOK__ > "u" || typeof __REACT_DEVTOOLS_GLOBAL_HOOK__.checkDCE != "function"))
      try {
        __REACT_DEVTOOLS_GLOBAL_HOOK__.checkDCE(c);
      } catch (o) {
        console.error(o);
      }
  }
  return c(), Rf.exports = Qh(), Rf.exports;
}
var Zh = Xh(), Lf = { exports: {} }, Fa = {};
/**
 * @license React
 * react-jsx-runtime.production.js
 *
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 */
var uy;
function Vh() {
  if (uy) return Fa;
  uy = 1;
  var c = Symbol.for("react.transitional.element"), o = Symbol.for("react.fragment");
  function r(s, q, E) {
    var C = null;
    if (E !== void 0 && (C = "" + E), q.key !== void 0 && (C = "" + q.key), "key" in q) {
      E = {};
      for (var U in q)
        U !== "key" && (E[U] = q[U]);
    } else E = q;
    return q = E.ref, {
      $$typeof: c,
      type: s,
      key: C,
      ref: q !== void 0 ? q : null,
      props: E
    };
  }
  return Fa.Fragment = o, Fa.jsx = r, Fa.jsxs = r, Fa;
}
var ay;
function Kh() {
  return ay || (ay = 1, Lf.exports = Vh()), Lf.exports;
}
var x = Kh();
function Jh(c) {
  return typeof c.questionId == "string";
}
function wh(c) {
  const o = c;
  return Array.isArray(o.all) || Array.isArray(o.any);
}
function kh(c) {
  return typeof c.expression == "string";
}
class Kl extends Error {
  constructor(r, s) {
    super(`Expression syntax error at column ${s}: ${r}`);
    ye(this, "position");
    this.position = s, this.name = "ExpressionSyntaxError";
  }
}
function bi(c) {
  return c >= "0" && c <= "9";
}
function sy(c) {
  return c >= "a" && c <= "z" || c >= "A" && c <= "Z";
}
function $h(c) {
  return sy(c) || bi(c);
}
function Wh(c) {
  return c === " " || c === "	" || c === `
` || c === "\r" || c === "\f" || c === "\v";
}
function Fh(c) {
  const o = [];
  let r = 0;
  const s = () => r >= c.length, q = (p = 0) => c.charAt(r + p), E = (p) => {
    if (r + p.length > c.length)
      return !1;
    for (let D = 0; D < p.length; D++)
      if (c.charAt(r + D) !== p.charAt(D))
        return !1;
    return r += p.length, !0;
  }, C = () => {
    for (; !s() && Wh(q()); )
      r++;
  }, U = (p) => {
    for (; !s() && bi(q()); )
      r++;
    if (!s() && q() === ".")
      for (r++; !s() && bi(q()); )
        r++;
    const D = c.substring(p, r), X = parseFloat(D);
    return { kind: "Number", text: D, literal: X, position: p };
  }, O = (p, D) => {
    r++;
    let X = "";
    for (; !s() && q() !== D; ) {
      const k = q();
      if (k === "\\" && r + 1 < c.length) {
        const F = q(1), W = {
          n: `
`,
          t: "	",
          r: "\r",
          "\\": "\\",
          "'": "'",
          '"': '"'
        }[F];
        if (W === void 0)
          throw new Kl(`unknown escape '\\${F}'.`, r);
        X += W, r += 2;
      } else
        X += k, r++;
    }
    if (s())
      throw new Kl("unterminated string literal.", p);
    return r++, { kind: "String", text: X, literal: X, position: p };
  }, b = (p) => {
    for (; !s(); ) {
      const X = q();
      if (X === "_" || X === "-" || $h(X))
        r++;
      else
        break;
    }
    const D = c.substring(p, r);
    return D === "true" ? { kind: "True", text: D, literal: !0, position: p } : D === "false" ? { kind: "False", text: D, literal: !1, position: p } : D === "null" ? { kind: "Null", text: D, literal: null, position: p } : { kind: "Identifier", text: D, literal: null, position: p };
  }, j = () => {
    const p = r, D = q();
    if (bi(D))
      return U(p);
    if (D === "'" || D === '"')
      return O(p, D);
    if (D === "_" || sy(D))
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
        throw new Kl("bare '=' is not a valid operator (use '==' or '===').", p);
      case "!":
        return E("!==") ? { kind: "StrictNotEq", text: "!==", literal: null, position: p } : E("!=") ? { kind: "NotEq", text: "!=", literal: null, position: p } : (r++, { kind: "Not", text: "!", literal: null, position: p });
      case "<":
        return E("<=") ? { kind: "LtEq", text: "<=", literal: null, position: p } : (r++, { kind: "Lt", text: "<", literal: null, position: p });
      case ">":
        return E(">=") ? { kind: "GtEq", text: ">=", literal: null, position: p } : (r++, { kind: "Gt", text: ">", literal: null, position: p });
      case "&":
        if (E("&&"))
          return { kind: "And", text: "&&", literal: null, position: p };
        throw new Kl("expected '&&'.", p);
      case "|":
        if (E("||"))
          return { kind: "Or", text: "||", literal: null, position: p };
        throw new Kl("expected '||'.", p);
    }
    throw new Kl(`unexpected character '${D}'.`, p);
  };
  for (; ; ) {
    if (C(), s())
      return o.push({ kind: "EndOfInput", text: "", literal: null, position: r }), o;
    o.push(j());
  }
}
function Ih(c) {
  let o = 0;
  const r = () => {
    const B = c[o];
    if (!B)
      throw new Kl("unexpected end of tokens.", 0);
    return B;
  }, s = () => {
    const B = r();
    return B.kind !== "EndOfInput" && o++, B;
  }, q = (B) => r().kind !== B ? !1 : (s(), !0), E = (B) => {
    const W = r();
    if (W.kind !== B)
      throw new Kl(`expected ${B}, got '${W.text}'.`, W.position);
    return s(), W;
  }, C = () => {
    let B = U();
    for (; q("Or"); )
      B = { kind: "BinaryOp", op: "||", left: B, right: U() };
    return B;
  }, U = () => {
    let B = O();
    for (; q("And"); )
      B = { kind: "BinaryOp", op: "&&", left: B, right: O() };
    return B;
  }, O = () => {
    let B = b();
    for (; ; ) {
      const W = r().kind;
      let nt = null;
      if (W === "Eq" || W === "StrictEq" ? nt = "==" : (W === "NotEq" || W === "StrictNotEq") && (nt = "!="), nt === null)
        break;
      s(), B = { kind: "BinaryOp", op: nt, left: B, right: b() };
    }
    return B;
  }, b = () => {
    let B = j();
    for (; ; ) {
      const W = r().kind;
      let nt = null;
      if (W === "Lt" ? nt = "<" : W === "Gt" ? nt = ">" : W === "LtEq" ? nt = "<=" : W === "GtEq" && (nt = ">="), nt === null)
        break;
      s(), B = { kind: "BinaryOp", op: nt, left: B, right: j() };
    }
    return B;
  }, j = () => q("Not") ? { kind: "UnaryOp", op: "!", operand: j() } : k(), p = () => {
    E("LBracket");
    const B = [];
    if (r().kind !== "RBracket")
      for (B.push(C()); q("Comma"); )
        B.push(C());
    return E("RBracket"), { kind: "Array", items: B };
  }, D = (B) => {
    let W;
    if (q("Dot"))
      W = E("Identifier").text;
    else if (q("LBracket")) {
      const nt = E("String");
      E("RBracket"), W = nt.literal;
    } else
      throw new Kl("'answers' must be followed by .key or ['key'].", B);
    return { kind: "AnswersAccess", key: W };
  }, X = () => {
    const B = s();
    if (B.text === "answers")
      return D(B.position);
    E("LParen");
    const W = [];
    if (r().kind !== "RParen")
      for (W.push(C()); q("Comma"); )
        W.push(C());
    return E("RParen"), { kind: "Call", name: B.text, args: W };
  }, k = () => {
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
        const W = C();
        return E("RParen"), W;
      }
      case "LBracket":
        return p();
      case "Identifier":
        return X();
      default:
        throw new Kl(`unexpected token '${B.text}'.`, B.position);
    }
  }, F = C();
  return E("EndOfInput"), F;
}
function he(c) {
  return c === void 0 || c === null ? null : typeof c == "boolean" || typeof c == "number" || typeof c == "string" ? c : Array.isArray(c) ? c.map(he) : null;
}
function du(c, o) {
  const r = he(c), s = he(o);
  if (r === null || s === null)
    return r === null && s === null;
  if (typeof r == "number" && typeof s == "number" || typeof r == "string" && typeof s == "string" || typeof r == "boolean" && typeof s == "boolean")
    return r === s;
  if (Array.isArray(r) && Array.isArray(s)) {
    if (r.length !== s.length)
      return !1;
    for (let q = 0; q < r.length; q++)
      if (!du(r[q], s[q]))
        return !1;
    return !0;
  }
  return !1;
}
function Ke(c, o) {
  const r = he(c), s = he(o);
  if (typeof r == "number" && typeof s == "number" || typeof r == "string" && typeof s == "string")
    return r < s ? -1 : r > s ? 1 : 0;
  throw new Error("Comparison operators require two numbers or two strings.");
}
function ta(c) {
  const o = he(c);
  return o === null ? !1 : typeof o == "boolean" ? o : typeof o == "number" ? o !== 0 : typeof o == "string" || Array.isArray(o) ? o.length > 0 : !0;
}
function jl(c, o) {
  switch (c.kind) {
    case "Literal":
      return c.value;
    case "AnswersAccess":
      return um(c.key, o);
    case "UnaryOp":
      return Ph(c, o);
    case "BinaryOp":
      return tm(c, o);
    case "Call":
      return lm(c, o);
    case "Array":
      return c.items.map((r) => jl(r, o));
  }
}
function Ph(c, o) {
  const r = jl(c.operand, o);
  if (c.op === "!")
    return !ta(r);
  throw new Error(`Unknown unary operator '${c.op}'.`);
}
function tm(c, o) {
  if (c.op === "&&") {
    const q = jl(c.left, o);
    return ta(q) ? ta(jl(c.right, o)) : !1;
  }
  if (c.op === "||") {
    const q = jl(c.left, o);
    return ta(q) ? !0 : ta(jl(c.right, o));
  }
  const r = jl(c.left, o), s = jl(c.right, o);
  switch (c.op) {
    case "==":
      return du(r, s);
    case "!=":
      return !du(r, s);
    case "<":
      return Ke(r, s) < 0;
    case ">":
      return Ke(r, s) > 0;
    case "<=":
      return Ke(r, s) <= 0;
    case ">=":
      return Ke(r, s) >= 0;
    default:
      throw new Error(`Unknown binary operator '${c.op}'.`);
  }
}
function lm(c, o) {
  switch (c.name) {
    case "has":
    case "isSet":
      return ny(c, o);
    case "isNotSet":
      return !ny(c, o);
    case "in":
      return em(c, o);
    default:
      throw new Error(`Unknown function '${c.name}'.`);
  }
}
function ny(c, o) {
  if (c.args.length !== 1)
    throw new Error(`${c.name}() takes one argument.`);
  const r = c.args[0];
  if (!r)
    return !1;
  const s = jl(r, o);
  return typeof s != "string" ? !1 : s in o && o[s] !== null && o[s] !== void 0;
}
function em(c, o) {
  if (c.args.length !== 2)
    throw new Error("in() takes two arguments: in(value, [array]).");
  const r = c.args[0], s = c.args[1];
  if (!r || !s)
    return !1;
  const q = jl(r, o), E = jl(s, o);
  return Array.isArray(E) ? E.some((C) => du(q, C)) : !1;
}
function um(c, o) {
  return c in o ? he(o[c]) : null;
}
function am(c) {
  const o = Fh(c);
  return Ih(o);
}
function nm(c, o) {
  try {
    const r = typeof c == "string" ? am(c) : c;
    return ta(jl(r, o));
  } catch {
    return !1;
  }
}
function im(c, o) {
  var r;
  if (!c.logic)
    return null;
  for (const s of c.logic)
    if (Zf(s.if, o))
      return ((r = s.then) == null ? void 0 : r.goto) ?? null;
  return null;
}
function Zf(c, o) {
  try {
    return Jh(c) ? fm(c, o) : wh(c) ? cm(c, o) : kh(c) ? nm(c.expression, o) : !1;
  } catch {
    return !1;
  }
}
function cm(c, o) {
  return c.all && c.all.length > 0 ? c.all.every((r) => Zf(r, o)) : c.any && c.any.length > 0 ? c.any.some((r) => Zf(r, o)) : !1;
}
function fm(c, o) {
  const r = c.questionId in o && o[c.questionId] !== null && o[c.questionId] !== void 0;
  if (c.op === "isSet")
    return r;
  if (c.op === "isNotSet")
    return !r;
  if (c.value === void 0)
    return !1;
  const s = r ? he(o[c.questionId]) : null, q = he(c.value);
  return sm(c.op, s, q);
}
function sm(c, o, r) {
  switch (c) {
    case "==":
      return du(o, r);
    case "!=":
      return !du(o, r);
    case ">":
      return Ke(o, r) > 0;
    case ">=":
      return Ke(o, r) >= 0;
    case "<":
      return Ke(o, r) < 0;
    case "<=":
      return Ke(o, r) <= 0;
    case "in":
      return iy(r, o);
    case "notIn":
      return !iy(r, o);
    default:
      return !1;
  }
}
function iy(c, o) {
  return Array.isArray(c) ? c.some((r) => du(o, r)) : !1;
}
function Vf(c, o, r) {
  const s = new Set(c.screens.map((U) => U.id)), q = im(c, r);
  if (q && q !== o && s.has(q))
    return { kind: "screen", screenId: q };
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
function rm(c, o, r, s) {
  const q = new Set(o.screens.map((E) => E.id));
  return c.nextScreen && q.has(c.nextScreen) ? { kind: "screen", screenId: c.nextScreen } : Vf(o, r, s);
}
class Iu extends Error {
  constructor(r) {
    super(r.message);
    ye(this, "status");
    ye(this, "code");
    ye(this, "serverMessage");
    ye(this, "validationErrors");
    ye(this, "raw");
    this.name = "SurveyClientError", this.status = r.status, this.code = r.code, this.serverMessage = r.serverMessage, this.validationErrors = r.validationErrors, this.raw = r.raw;
  }
}
class cy {
  constructor(o) {
    ye(this, "baseUrl");
    ye(this, "fetchFn");
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
    let q;
    try {
      q = await this.fetchFn(`${this.baseUrl}${r}`, {
        method: o,
        headers: s === void 0 ? void 0 : { "Content-Type": "application/json" },
        body: s === void 0 ? void 0 : JSON.stringify(s)
      });
    } catch (E) {
      throw new Iu({
        status: 0,
        code: "network",
        message: `Network error calling ${o} ${r}: ${E.message ?? E}`
      });
    }
    if (!q.ok)
      throw await this.toError(q, o, r);
    return q;
  }
  async readJson(o) {
    const r = await o.text();
    if (!r)
      throw new Iu({
        status: o.status,
        code: "parse",
        message: `Empty body from ${o.url}`
      });
    try {
      return JSON.parse(r);
    } catch (s) {
      throw new Iu({
        status: o.status,
        code: "parse",
        message: `Could not parse JSON from ${o.url}: ${s.message}`,
        raw: r
      });
    }
  }
  async toError(o, r, s) {
    const q = o.status === 404 ? "notFound" : o.status === 410 ? "gone" : o.status === 409 ? "conflict" : o.status === 400 ? "badRequest" : (o.status >= 500, "server"), E = await o.text();
    if (!E)
      return new Iu({
        status: o.status,
        code: q,
        message: `${r} ${s} → ${o.status}`
      });
    let C;
    try {
      C = JSON.parse(E);
    } catch {
      return new Iu({
        status: o.status,
        code: q,
        message: `${r} ${s} → ${o.status}: ${E.slice(0, 200)}`,
        raw: E
      });
    }
    const U = C.Message ?? C.message, O = C.Errors ?? C.errors, b = Array.isArray(O) ? O.flatMap((j) => {
      const p = j.QuestionId ?? j.questionId, D = j.Message ?? j.message;
      return p && D ? [{ questionId: p, message: D }] : [];
    }) : void 0;
    return new Iu({
      status: o.status,
      code: q,
      message: `${r} ${s} → ${o.status}${U ? ": " + U : ""}`,
      serverMessage: U,
      validationErrors: b && b.length > 0 ? b : void 0,
      raw: C
    });
  }
}
function fy(c) {
  const o = c.trim().replace(/^#/, "");
  if (!/^[0-9a-fA-F]{3}$|^[0-9a-fA-F]{6}$|^[0-9a-fA-F]{8}$/.test(o)) return null;
  const r = o.length === 3 ? o.split("").map((s) => s + s).join("") : o.slice(0, 6);
  return [
    parseInt(r.slice(0, 2), 16),
    parseInt(r.slice(2, 4), 16),
    parseInt(r.slice(4, 6), 16)
  ];
}
function Gf([c, o, r]) {
  const s = (q) => Math.max(0, Math.min(255, Math.round(q))).toString(16).padStart(2, "0");
  return `#${s(c)}${s(o)}${s(r)}`;
}
function om(c, o) {
  return [c[0] * o, c[1] * o, c[2] * o];
}
function dm([c, o, r]) {
  const s = (q) => {
    const E = q / 255;
    return E <= 0.03928 ? E / 12.92 : Math.pow((E + 0.055) / 1.055, 2.4);
  };
  return 0.2126 * s(c) + 0.7152 * s(o) + 0.0722 * s(r);
}
function ym(c) {
  const o = {}, r = c != null && c.primaryColor ? fy(c.primaryColor) : null;
  r && (o["--survey-primary"] = Gf(r), o["--survey-primary-hover"] = Gf(om(r, 0.82)), o["--survey-primary-contrast"] = dm(r) > 0.45 ? "#111111" : "#ffffff");
  const s = c != null && c.secondaryColor ? fy(c.secondaryColor) : null;
  return s && (o["--survey-accent"] = Gf(s)), o;
}
const ry = ut.createContext(null), vm = ry.Provider;
function al() {
  const c = ut.useContext(ry);
  if (!c)
    throw new Error(
      "useSurveyContext must be used inside <SurveyRenderer>. Question components rely on survey state from the enclosing provider."
    );
  return c;
}
function tt(c, o, r) {
  if (c == null) return "";
  if (typeof c == "string") return c;
  if (c[o]) return c[o];
  if (r && c[r]) return c[r];
  const s = Object.keys(c);
  return s.length > 0 ? c[s[0]] : "";
}
const oy = {
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
}, hm = {
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
}, mm = { en: oy, ar: hm };
function gm(c, o, r) {
  const s = { ...mm, ...r ?? {} };
  return s[c] ?? (o ? s[o] : void 0) ?? s.en ?? oy;
}
const bm = "adp-surveys", Sm = 1;
function pm(c = {}) {
  const o = typeof window < "u", r = o && window.parent !== window, s = c.enabled ?? r, q = c.target ?? (o ? window.parent : null), E = c.targetOrigin ?? "*";
  if (!s || !q)
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
  const C = (U, O) => {
    const b = {
      source: bm,
      version: Sm,
      type: U,
      payload: O
    };
    try {
      q.postMessage(b, E);
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
function wf(c) {
  return `adp-surveys:resume:${c}`;
}
function _m(c, o) {
  try {
    const r = c.getItem(wf(o));
    if (!r) return null;
    const s = JSON.parse(r);
    return !s || typeof s != "object" || !s.answers ? null : s;
  } catch {
    return null;
  }
}
function Em(c, o, r) {
  try {
    const s = { ...r, savedAt: Date.now() };
    c.setItem(wf(o), JSON.stringify(s));
  } catch {
  }
}
function zm(c, o) {
  try {
    c.removeItem(wf(o));
  } catch {
  }
}
function Am({
  question: c,
  registry: o
}) {
  const { ui: r } = al(), s = c.type, q = s ? o[s] : void 0;
  return q ? /* @__PURE__ */ x.jsx(q, { question: c }) : /* @__PURE__ */ x.jsx("div", { className: "survey-question survey-question--unknown", children: /* @__PURE__ */ x.jsxs("em", { children: [
    r.unsupportedQuestion,
    " ",
    String(s ?? "missing")
  ] }) });
}
function qm({ question: c }) {
  const { locale: o, schema: r, answers: s, setAnswer: q } = al(), E = c.id, C = c.title, U = c.help, O = !!c.required, b = s[E] ?? "";
  return /* @__PURE__ */ x.jsxs("div", { className: "survey-question survey-question--text", children: [
    /* @__PURE__ */ x.jsxs("label", { className: "survey-question__label", htmlFor: `q-${E}`, children: [
      tt(C, o, r.defaultLocale),
      O && /* @__PURE__ */ x.jsx("span", { "aria-label": "required", className: "survey-question__required", children: " *" })
    ] }),
    U && /* @__PURE__ */ x.jsx("p", { className: "survey-question__help", children: tt(U, o, r.defaultLocale) }),
    /* @__PURE__ */ x.jsx(
      "input",
      {
        id: `q-${E}`,
        className: "survey-question__input",
        type: "text",
        value: b,
        required: O,
        onChange: (j) => q(E, j.target.value)
      }
    )
  ] });
}
function Tm({ question: c }) {
  const { locale: o, schema: r, answers: s, setAnswer: q } = al(), E = c.id, C = c.title, U = c.help, O = !!c.required, b = Number(c.min ?? 0), j = Number(c.max ?? 10), p = c.lowLabel, D = c.highLabel, X = s[E], k = [];
  for (let F = b; F <= j; F++) k.push(F);
  return /* @__PURE__ */ x.jsxs("fieldset", { className: "survey-question survey-question--nps", children: [
    /* @__PURE__ */ x.jsxs("legend", { className: "survey-question__label", children: [
      tt(C, o, r.defaultLocale),
      O && /* @__PURE__ */ x.jsx("span", { "aria-label": "required", className: "survey-question__required", children: " *" })
    ] }),
    U && /* @__PURE__ */ x.jsx("p", { className: "survey-question__help", children: tt(U, o, r.defaultLocale) }),
    /* @__PURE__ */ x.jsx("div", { className: "survey-question__nps-scale", role: "radiogroup", children: k.map((F) => {
      const B = X === F;
      return /* @__PURE__ */ x.jsx(
        "button",
        {
          type: "button",
          role: "radio",
          "aria-checked": B,
          className: "survey-question__nps-step" + (B ? " survey-question__nps-step--selected" : ""),
          onClick: () => q(E, F),
          children: F
        },
        F
      );
    }) }),
    (p || D) && /* @__PURE__ */ x.jsxs("div", { className: "survey-question__nps-labels", children: [
      /* @__PURE__ */ x.jsx("span", { children: p ? tt(p, o, r.defaultLocale) : "" }),
      /* @__PURE__ */ x.jsx("span", { children: D ? tt(D, o, r.defaultLocale) : "" })
    ] })
  ] });
}
function xm({ question: c }) {
  const { locale: o, schema: r } = al(), s = c.id, q = c.title, E = c.help, C = c.options ?? [], U = (O, b) => {
    const j = {
      questionId: s,
      option: {
        id: b.id,
        nextScreen: b.nextScreen
      }
    };
    O.currentTarget.dispatchEvent(
      new CustomEvent("survey:navigationListSelect", {
        detail: j,
        bubbles: !0
      })
    );
  };
  return /* @__PURE__ */ x.jsxs("div", { className: "survey-question survey-question--navlist", children: [
    /* @__PURE__ */ x.jsx("div", { className: "survey-question__label", children: tt(q, o, r.defaultLocale) }),
    E && /* @__PURE__ */ x.jsx("p", { className: "survey-question__help", children: tt(E, o, r.defaultLocale) }),
    /* @__PURE__ */ x.jsx("ul", { className: "survey-navlist", role: "radiogroup", "aria-description": "Selecting an option navigates to the next screen.", children: C.map((O) => {
      const b = O.id, j = O.label;
      return /* @__PURE__ */ x.jsx("li", { className: "survey-navlist__row", children: /* @__PURE__ */ x.jsxs(
        "button",
        {
          type: "button",
          className: "survey-navlist__button",
          onClick: (p) => U(p, O),
          children: [
            /* @__PURE__ */ x.jsx("span", { className: "survey-navlist__label", children: tt(j, o, r.defaultLocale) }),
            /* @__PURE__ */ x.jsx("span", { "aria-hidden": "true", className: "survey-navlist__chevron", children: "›" })
          ]
        }
      ) }, b);
    }) })
  ] });
}
function Nm({ question: c }) {
  const { locale: o, schema: r, answers: s, setAnswer: q } = al(), E = c.id, C = c.title, U = c.help, O = c.placeholder, b = !!c.required, j = c.minLength, p = c.maxLength, D = s[E] ?? "";
  return /* @__PURE__ */ x.jsxs("div", { className: "survey-question survey-question--paragraph", children: [
    /* @__PURE__ */ x.jsxs("label", { className: "survey-question__label", htmlFor: `q-${E}`, children: [
      tt(C, o, r.defaultLocale),
      b && /* @__PURE__ */ x.jsx("span", { "aria-label": "required", className: "survey-question__required", children: " *" })
    ] }),
    U && /* @__PURE__ */ x.jsx("p", { className: "survey-question__help", children: tt(U, o, r.defaultLocale) }),
    /* @__PURE__ */ x.jsx(
      "textarea",
      {
        id: `q-${E}`,
        className: "survey-question__textarea",
        value: D,
        required: b,
        rows: 5,
        minLength: j,
        maxLength: p,
        placeholder: O ? tt(O, o, r.defaultLocale) : void 0,
        onChange: (X) => q(E, X.target.value)
      }
    )
  ] });
}
function Om({ question: c }) {
  const { locale: o, schema: r, answers: s, setAnswer: q } = al(), E = c.id, C = c.title, U = c.help, O = !!c.required, b = c.min, j = c.max, p = c.step, D = c.unit, X = s[E], k = X == null ? "" : String(X);
  return /* @__PURE__ */ x.jsxs("div", { className: "survey-question survey-question--number", children: [
    /* @__PURE__ */ x.jsxs("label", { className: "survey-question__label", htmlFor: `q-${E}`, children: [
      tt(C, o, r.defaultLocale),
      O && /* @__PURE__ */ x.jsx("span", { "aria-label": "required", className: "survey-question__required", children: " *" })
    ] }),
    U && /* @__PURE__ */ x.jsx("p", { className: "survey-question__help", children: tt(U, o, r.defaultLocale) }),
    /* @__PURE__ */ x.jsxs("div", { className: "survey-question__number-wrap", children: [
      /* @__PURE__ */ x.jsx(
        "input",
        {
          id: `q-${E}`,
          className: "survey-question__input",
          type: "number",
          value: k,
          required: O,
          min: b,
          max: j,
          step: p,
          onChange: (F) => {
            const B = F.target.value;
            q(E, B === "" ? null : Number(B));
          }
        }
      ),
      D && /* @__PURE__ */ x.jsx("span", { className: "survey-question__unit", children: tt(D, o, r.defaultLocale) })
    ] })
  ] });
}
function Mm({ question: c }) {
  const { locale: o, schema: r, answers: s, setAnswer: q } = al(), E = c.id, C = c.title, U = c.help, O = !!c.required, b = Number(c.max ?? 5), j = s[E], p = [];
  for (let D = 1; D <= b; D++) p.push(D);
  return /* @__PURE__ */ x.jsxs("fieldset", { className: "survey-question survey-question--rating", children: [
    /* @__PURE__ */ x.jsxs("legend", { className: "survey-question__label", children: [
      tt(C, o, r.defaultLocale),
      O && /* @__PURE__ */ x.jsx("span", { "aria-label": "required", className: "survey-question__required", children: " *" })
    ] }),
    U && /* @__PURE__ */ x.jsx("p", { className: "survey-question__help", children: tt(U, o, r.defaultLocale) }),
    /* @__PURE__ */ x.jsx("div", { className: "survey-question__rating-scale", role: "radiogroup", children: p.map((D) => {
      const X = typeof j == "number" && D <= j;
      return /* @__PURE__ */ x.jsx(
        "button",
        {
          type: "button",
          role: "radio",
          "aria-checked": j === D,
          "aria-label": `${D}`,
          className: "survey-question__rating-star" + (X ? " survey-question__rating-star--selected" : ""),
          onClick: () => q(E, D),
          children: /* @__PURE__ */ x.jsx("span", { "aria-hidden": "true", children: "★" })
        },
        D
      );
    }) })
  ] });
}
function Dm({ question: c }) {
  const { locale: o, schema: r, answers: s, setAnswer: q } = al(), E = c.id, C = c.title, U = c.help, O = !!c.required, b = c.options ?? [], j = s[E];
  return /* @__PURE__ */ x.jsxs("fieldset", { className: "survey-question survey-question--single", children: [
    /* @__PURE__ */ x.jsxs("legend", { className: "survey-question__label", children: [
      tt(C, o, r.defaultLocale),
      O && /* @__PURE__ */ x.jsx("span", { "aria-label": "required", className: "survey-question__required", children: " *" })
    ] }),
    U && /* @__PURE__ */ x.jsx("p", { className: "survey-question__help", children: tt(U, o, r.defaultLocale) }),
    /* @__PURE__ */ x.jsx("div", { className: "survey-question__options", children: b.map((p) => /* @__PURE__ */ x.jsxs("label", { className: "survey-question__option", children: [
      /* @__PURE__ */ x.jsx(
        "input",
        {
          type: "radio",
          name: `q-${E}`,
          value: p.id,
          checked: j === p.id,
          onChange: () => q(E, p.id)
        }
      ),
      /* @__PURE__ */ x.jsx("span", { children: tt(p.label, o, r.defaultLocale) })
    ] }, p.id)) })
  ] });
}
function Um({ question: c }) {
  const { locale: o, schema: r, answers: s, setAnswer: q } = al(), E = c.id, C = c.title, U = c.help, O = !!c.required, b = c.options ?? [], j = c.maxSelected, p = s[E] ?? [], D = (X) => {
    if (p.includes(X)) {
      q(E, p.filter((k) => k !== X));
      return;
    }
    j !== void 0 && p.length >= j || q(E, [...p, X]);
  };
  return /* @__PURE__ */ x.jsxs("fieldset", { className: "survey-question survey-question--multi", children: [
    /* @__PURE__ */ x.jsxs("legend", { className: "survey-question__label", children: [
      tt(C, o, r.defaultLocale),
      O && /* @__PURE__ */ x.jsx("span", { "aria-label": "required", className: "survey-question__required", children: " *" })
    ] }),
    U && /* @__PURE__ */ x.jsx("p", { className: "survey-question__help", children: tt(U, o, r.defaultLocale) }),
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
        /* @__PURE__ */ x.jsx("span", { children: tt(X.label, o, r.defaultLocale) })
      ] }, X.id);
    }) })
  ] });
}
function Cm({ question: c }) {
  const { locale: o, schema: r, answers: s, setAnswer: q, ui: E } = al(), C = c.id, U = c.title, O = c.help, b = !!c.required, j = c.options ?? [], p = c.placeholder, D = s[C] ?? "", X = p ? tt(p, o, r.defaultLocale) : E.selectPlaceholder;
  return /* @__PURE__ */ x.jsxs("div", { className: "survey-question survey-question--dropdown", children: [
    /* @__PURE__ */ x.jsxs("label", { className: "survey-question__label", htmlFor: `q-${C}`, children: [
      tt(U, o, r.defaultLocale),
      b && /* @__PURE__ */ x.jsx("span", { "aria-label": "required", className: "survey-question__required", children: " *" })
    ] }),
    O && /* @__PURE__ */ x.jsx("p", { className: "survey-question__help", children: tt(O, o, r.defaultLocale) }),
    /* @__PURE__ */ x.jsxs(
      "select",
      {
        id: `q-${C}`,
        className: "survey-question__select",
        value: D,
        required: b,
        onChange: (k) => q(C, k.target.value || null),
        children: [
          /* @__PURE__ */ x.jsx("option", { value: "", children: X }),
          j.map((k) => /* @__PURE__ */ x.jsx("option", { value: k.id, children: tt(k.label, o, r.defaultLocale) }, k.id))
        ]
      }
    )
  ] });
}
function jm({ question: c }) {
  const { locale: o, schema: r, answers: s, setAnswer: q } = al(), E = c.id, C = c.title, U = c.help, O = !!c.required, b = c.minDate, j = c.maxDate, p = s[E] ?? "";
  return /* @__PURE__ */ x.jsxs("div", { className: "survey-question survey-question--date", children: [
    /* @__PURE__ */ x.jsxs("label", { className: "survey-question__label", htmlFor: `q-${E}`, children: [
      tt(C, o, r.defaultLocale),
      O && /* @__PURE__ */ x.jsx("span", { "aria-label": "required", className: "survey-question__required", children: " *" })
    ] }),
    U && /* @__PURE__ */ x.jsx("p", { className: "survey-question__help", children: tt(U, o, r.defaultLocale) }),
    /* @__PURE__ */ x.jsx(
      "input",
      {
        id: `q-${E}`,
        className: "survey-question__input",
        type: "date",
        value: p,
        required: O,
        min: b,
        max: j,
        onChange: (D) => q(E, D.target.value || null)
      }
    )
  ] });
}
function Rm({ question: c }) {
  const { locale: o, schema: r, answers: s, setAnswer: q } = al(), E = c.id, C = c.title, U = c.help, O = !!c.required, b = c.minDateTime, j = c.maxDateTime, p = s[E] ?? "", D = (X) => {
    if (!X) return;
    const k = X.match(/^(\d{4}-\d{2}-\d{2}T\d{2}:\d{2})/);
    return (k == null ? void 0 : k[1]) ?? void 0;
  };
  return /* @__PURE__ */ x.jsxs("div", { className: "survey-question survey-question--datetime", children: [
    /* @__PURE__ */ x.jsxs("label", { className: "survey-question__label", htmlFor: `q-${E}`, children: [
      tt(C, o, r.defaultLocale),
      O && /* @__PURE__ */ x.jsx("span", { "aria-label": "required", className: "survey-question__required", children: " *" })
    ] }),
    U && /* @__PURE__ */ x.jsx("p", { className: "survey-question__help", children: tt(U, o, r.defaultLocale) }),
    /* @__PURE__ */ x.jsx(
      "input",
      {
        id: `q-${E}`,
        className: "survey-question__input",
        type: "datetime-local",
        value: D(p) ?? "",
        required: O,
        min: D(b),
        max: D(j),
        onChange: (X) => q(E, X.target.value || null)
      }
    )
  ] });
}
function Hm({ question: c }) {
  const { locale: o, schema: r, answers: s, setAnswer: q } = al(), E = c.id, C = c.title, U = c.help, O = !!c.required, b = c.acceptedTypes, j = ut.useRef(null), p = s[E], D = b && b.length > 0 ? b.join(",") : void 0;
  return /* @__PURE__ */ x.jsxs("div", { className: "survey-question survey-question--file", children: [
    /* @__PURE__ */ x.jsxs("label", { className: "survey-question__label", htmlFor: `q-${E}`, children: [
      tt(C, o, r.defaultLocale),
      O && /* @__PURE__ */ x.jsx("span", { "aria-label": "required", className: "survey-question__required", children: " *" })
    ] }),
    U && /* @__PURE__ */ x.jsx("p", { className: "survey-question__help", children: tt(U, o, r.defaultLocale) }),
    /* @__PURE__ */ x.jsx(
      "input",
      {
        ref: j,
        id: `q-${E}`,
        className: "survey-question__file",
        type: "file",
        required: O,
        accept: D,
        onChange: (X) => {
          var k;
          const F = (k = X.target.files) == null ? void 0 : k[0];
          if (!F) {
            q(E, null);
            return;
          }
          q(E, { name: F.name, size: F.size, type: F.type });
        }
      }
    ),
    (p == null ? void 0 : p.name) && /* @__PURE__ */ x.jsxs("p", { className: "survey-question__file-name", children: [
      "Selected: ",
      p.name
    ] })
  ] });
}
const Qf = 480, Xf = 160;
function Bm({ question: c }) {
  const { locale: o, schema: r, answers: s, setAnswer: q, ui: E } = al(), C = c.id, U = c.title, O = c.help, b = !!c.required, j = ut.useRef(null), [p, D] = ut.useState(!1), [X, k] = ut.useState(!!s[C]), F = () => {
    var w;
    return ((w = j.current) == null ? void 0 : w.getContext("2d")) ?? null;
  }, B = (w) => {
    const dt = w.target.getBoundingClientRect();
    return {
      x: (w.clientX - dt.left) / dt.width * Qf,
      y: (w.clientY - dt.top) / dt.height * Xf
    };
  }, W = ut.useCallback(() => {
    var w;
    const dt = (w = j.current) == null ? void 0 : w.toDataURL("image/png");
    dt && q(C, dt);
  }, [C, q]), nt = () => {
    const w = F();
    w && (w.clearRect(0, 0, Qf, Xf), k(!1), q(C, null));
  };
  return ut.useEffect(() => {
    const w = F();
    w && (w.lineWidth = 2, w.lineCap = "round", w.strokeStyle = "#111");
  }, []), /* @__PURE__ */ x.jsxs("div", { className: "survey-question survey-question--signature", children: [
    /* @__PURE__ */ x.jsxs("div", { className: "survey-question__label", children: [
      tt(U, o, r.defaultLocale),
      b && /* @__PURE__ */ x.jsx("span", { "aria-label": "required", className: "survey-question__required", children: " *" })
    ] }),
    O && /* @__PURE__ */ x.jsx("p", { className: "survey-question__help", children: tt(O, o, r.defaultLocale) }),
    /* @__PURE__ */ x.jsx(
      "canvas",
      {
        ref: j,
        className: "survey-question__signature-canvas",
        width: Qf,
        height: Xf,
        role: "img",
        "aria-label": "signature pad",
        onPointerDown: (w) => {
          w.target.setPointerCapture(w.pointerId);
          const dt = F();
          if (!dt) return;
          const { x: gt, y: jt } = B(w);
          dt.beginPath(), dt.moveTo(gt, jt), D(!0);
        },
        onPointerMove: (w) => {
          if (!p) return;
          const dt = F();
          if (!dt) return;
          const { x: gt, y: jt } = B(w);
          dt.lineTo(gt, jt), dt.stroke(), k(!0);
        },
        onPointerUp: () => {
          D(!1), X && W();
        }
      }
    ),
    /* @__PURE__ */ x.jsx("div", { className: "survey-question__signature-actions", children: /* @__PURE__ */ x.jsx("button", { type: "button", className: "survey-button survey-button--ghost", onClick: nt, children: E.clearSignature }) })
  ] });
}
function Ym({ question: c }) {
  const { locale: o, schema: r, answers: s, setAnswer: q, ui: E } = al(), C = c.id, U = c.title, O = c.help, b = !!c.required, j = c.yesLabel, p = c.noLabel, D = s[C], X = j ? tt(j, o, r.defaultLocale) : E.yes, k = p ? tt(p, o, r.defaultLocale) : E.no;
  return /* @__PURE__ */ x.jsxs("fieldset", { className: "survey-question survey-question--yesno", children: [
    /* @__PURE__ */ x.jsxs("legend", { className: "survey-question__label", children: [
      tt(U, o, r.defaultLocale),
      b && /* @__PURE__ */ x.jsx("span", { "aria-label": "required", className: "survey-question__required", children: " *" })
    ] }),
    O && /* @__PURE__ */ x.jsx("p", { className: "survey-question__help", children: tt(O, o, r.defaultLocale) }),
    /* @__PURE__ */ x.jsxs("div", { className: "survey-question__yesno", role: "radiogroup", children: [
      /* @__PURE__ */ x.jsx(
        "button",
        {
          type: "button",
          role: "radio",
          "aria-checked": D === !0,
          className: "survey-question__yesno-button" + (D === !0 ? " survey-question__yesno-button--selected" : ""),
          onClick: () => q(C, !0),
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
          onClick: () => q(C, !1),
          children: k
        }
      )
    ] })
  ] });
}
const Lm = {
  text: qm,
  paragraph: Nm,
  number: Om,
  rating: Mm,
  nps: Tm,
  singleChoice: Dm,
  multiChoice: Um,
  dropdown: Cm,
  date: jm,
  dateTime: Rm,
  file: Hm,
  signature: Bm,
  yesNo: Ym,
  navigationList: xm
};
function Gm({
  schema: c,
  onSubmit: o,
  initialAnswers: r,
  locale: s,
  onScreenChange: q,
  onCompleted: E,
  registry: C,
  submissionMeta: U,
  uiLocales: O,
  resumeKey: b,
  storage: j,
  emitHostMessages: p,
  hostMessageOrigin: D,
  hostMessageTarget: X
}) {
  var k, F;
  const B = s ?? c.defaultLocale ?? "en", W = C ?? Lm, nt = ut.useMemo(
    () => gm(B, c.defaultLocale, O),
    [B, c.defaultLocale, O]
  ), w = j ?? (typeof globalThis < "u" ? globalThis.localStorage : void 0), dt = ut.useMemo(() => {
    var K;
    if (!b || !w) return null;
    const ht = _m(w, b);
    return ht ? ht.currentScreenId === null || c.screens.some((tl) => tl.id === ht.currentScreenId) ? ht : { ...ht, currentScreenId: ((K = c.screens[0]) == null ? void 0 : K.id) ?? null } : null;
  }, []), [gt, jt] = ut.useState(() => ({
    ...r ?? {},
    ...(dt == null ? void 0 : dt.answers) ?? {}
  })), [L, Xt] = ut.useState(
    () => {
      var K;
      return (dt == null ? void 0 : dt.currentScreenId) ?? ((K = c.screens[0]) == null ? void 0 : K.id) ?? null;
    }
  );
  ut.useEffect(() => {
    if (c.screens.length === 0) {
      L !== null && Xt(null);
      return;
    }
    L !== null && c.screens.some((K) => K.id === L) || Xt(c.screens[0].id);
  }, [c, L]);
  const [el, Jl] = ut.useState(!1), [Pt, Jt] = ut.useState(null), [wl, zl] = ut.useState(/* @__PURE__ */ new Set()), [Lt, A] = ut.useState(!1), H = ut.useRef((/* @__PURE__ */ new Date()).toISOString()), Z = ut.useRef(null);
  if (Z.current === null) {
    const K = {};
    X !== void 0 && (K.target = X), D !== void 0 && (K.targetOrigin = D), p !== void 0 && (K.enabled = p), Z.current = pm(K);
  }
  const $ = ut.useMemo(
    () => L ? c.screens.find((K) => K.id === L) ?? null : null,
    [c, L]
  );
  ut.useEffect(() => {
    var K;
    q == null || q(L), (K = Z.current) == null || K.screenChanged(L);
  }, [L, q]);
  const bt = ut.useRef(!1);
  ut.useEffect(() => {
    var K;
    bt.current || !L || (bt.current = !0, (K = Z.current) == null || K.loaded());
  }, [L]), ut.useEffect(() => {
    !b || !w || Lt || Em(w, b, {
      answers: gt,
      currentScreenId: L,
      schemaVersion: c.version
    });
  }, [gt, L, b, w, Lt, c.version]), ut.useEffect(() => {
    Lt && b && w && zm(w, b);
  }, [Lt, b, w]), ut.useEffect(() => {
    var K;
    Pt && ((K = Z.current) == null || K.error(Pt));
  }, [Pt]);
  const v = ut.useCallback((K, ht) => {
    jt((tl) => ({ ...tl, [K]: ht }));
  }, []), N = ut.useCallback(
    (K) => {
      K !== null && (zl(/* @__PURE__ */ new Set()), Xt(K));
    },
    []
  ), R = ut.useCallback(
    (K) => {
      if (!K.required) return !1;
      const ht = gt[K.id];
      return !!(ht == null || typeof ht == "string" && ht.trim() === "" || Array.isArray(ht) && ht.length === 0);
    },
    [gt]
  ), Y = ut.useCallback(async () => {
    var K;
    Jl(!0), Jt(null);
    try {
      await o({
        schemaVersion: c.version ?? 0,
        answers: gt,
        meta: {
          startedAt: (U == null ? void 0 : U.startedAt) ?? H.current,
          completedAt: (U == null ? void 0 : U.completedAt) ?? (/* @__PURE__ */ new Date()).toISOString(),
          ...U ?? {}
        }
      }), A(!0), E == null || E(L), (K = Z.current) == null || K.completed({ screenId: L, answers: gt });
    } catch (ht) {
      Jt(ht.message ?? String(ht));
    } finally {
      Jl(!1);
    }
  }, [c.version, gt, U, o, E, L]), I = ut.useCallback(() => {
    if (!L) return;
    const K = c.screens.find((Gt) => Gt.id === L), ht = ((K == null ? void 0 : K.questions) ?? []).filter(R).map((Gt) => Gt.id);
    if (ht.length > 0) {
      zl(new Set(ht));
      return;
    }
    const tl = Vf(c, L, gt);
    tl.kind === "end" ? Y() : N(tl.screenId);
  }, [c, L, gt, R, N, Y]), at = ut.useRef(null);
  ut.useEffect(() => {
    Lt || el || !L || !$ || at.current === L || !(!$.questions || $.questions.length === 0) || Vf(c, L, gt).kind === "end" && (at.current = L, Y());
  }, [L, $, Lt, el, c, gt, Y]);
  const ft = ut.useRef(null);
  ut.useEffect(() => {
    const K = ft.current;
    if (!K || typeof ResizeObserver > "u") return;
    const ht = new ResizeObserver((tl) => {
      var Gt;
      const ge = tl[0];
      ge && ((Gt = Z.current) == null || Gt.resize(Math.ceil(ge.contentRect.height)));
    });
    return ht.observe(K), () => ht.disconnect();
  }, []), ut.useEffect(() => {
    const K = ft.current;
    if (!K) return;
    const ht = (tl) => {
      const Gt = tl.detail;
      if (!Gt || !L) return;
      v(Gt.questionId, Gt.option.id);
      const ge = { ...gt, [Gt.questionId]: Gt.option.id }, yu = rm(
        Gt.option,
        c,
        L,
        ge
      );
      yu.kind === "end" ? Y() : N(yu.screenId);
    };
    return K.addEventListener("survey:navigationListSelect", ht), () => K.removeEventListener("survey:navigationListSelect", ht);
  }, [gt, L, c, v, N, Y]);
  const wt = ut.useMemo(
    () => ({
      schema: c,
      locale: B,
      direction: nt.direction,
      ui: nt.strings,
      answers: gt,
      setAnswer: v
    }),
    [c, B, nt, gt, v]
  ), qt = ut.useMemo(() => ym(c.branding), [c.branding]), me = (k = c.branding) != null && k.logoUrl ? /* @__PURE__ */ x.jsx("div", { className: "survey-brand", children: /* @__PURE__ */ x.jsx(
    "img",
    {
      className: "survey-brand__logo",
      src: c.branding.logoUrl,
      alt: "",
      onError: (K) => {
        K.currentTarget.parentElement.style.display = "none";
      }
    }
  ) }) : null;
  if (Lt)
    return /* @__PURE__ */ x.jsxs(
      "div",
      {
        ref: ft,
        className: "survey-root survey-root--done",
        dir: nt.direction,
        lang: B,
        style: qt,
        children: [
          me,
          /* @__PURE__ */ x.jsxs("div", { className: "survey-screen", children: [
            /* @__PURE__ */ x.jsx("h2", { className: "survey-screen__title", children: $ != null && $.title ? tt($.title, B, c.defaultLocale) : nt.strings.thankYou }),
            ($ == null ? void 0 : $.description) && /* @__PURE__ */ x.jsx("p", { className: "survey-screen__description", children: tt($.description, B, c.defaultLocale) })
          ] })
        ]
      }
    );
  if (!$)
    return /* @__PURE__ */ x.jsx("div", { ref: ft, className: "survey-root", dir: nt.direction, lang: B, style: qt, children: /* @__PURE__ */ x.jsx("div", { className: "survey-screen", children: /* @__PURE__ */ x.jsx("em", { children: nt.strings.noScreens }) }) });
  const Gl = $.questions ?? [], ua = Gl.length > 0 && ((F = Gl[Gl.length - 1]) == null ? void 0 : F.type) === "navigationList", Pa = Gl.length === 0 && !$.nextScreen, kl = !ua && !Pa;
  return /* @__PURE__ */ x.jsx(vm, { value: wt, children: /* @__PURE__ */ x.jsxs("div", { ref: ft, className: "survey-root", dir: nt.direction, lang: B, style: qt, children: [
    me,
    /* @__PURE__ */ x.jsxs("div", { className: "survey-screen", children: [
      $.title && /* @__PURE__ */ x.jsx("h2", { className: "survey-screen__title", children: tt($.title, B, c.defaultLocale) }),
      $.description && /* @__PURE__ */ x.jsx("p", { className: "survey-screen__description", children: tt($.description, B, c.defaultLocale) }),
      /* @__PURE__ */ x.jsx("div", { className: "survey-screen__questions", children: Gl.map((K, ht) => {
        const tl = K.id, Gt = tl !== void 0 && wl.has(tl) && R(K);
        return /* @__PURE__ */ x.jsxs("div", { className: Gt ? "survey-question-slot survey-question-slot--invalid" : "survey-question-slot", children: [
          /* @__PURE__ */ x.jsx(Am, { question: K, registry: W }),
          Gt && /* @__PURE__ */ x.jsx("p", { className: "survey-question__required-error", role: "alert", children: nt.strings.requiredError })
        ] }, tl ?? ht);
      }) }),
      kl && /* @__PURE__ */ x.jsx("div", { className: "survey-screen__actions", children: /* @__PURE__ */ x.jsx(
        "button",
        {
          type: "button",
          className: "survey-button survey-button--primary",
          disabled: el,
          onClick: I,
          children: el ? nt.strings.submitting : nt.strings.next
        }
      ) }),
      Pt && /* @__PURE__ */ x.jsxs("p", { className: "survey-screen__error", role: "alert", children: [
        nt.strings.couldNotSubmit,
        " ",
        Pt
      ] })
    ] })
  ] }) });
}
const Qm = ".survey-root{--survey-primary: #2563eb;--survey-primary-hover: #1e40af;--survey-primary-contrast: #ffffff;--survey-accent: #f5b60c;font-family:system-ui,-apple-system,Segoe UI,Roboto,sans-serif;color:#111;max-width:640px;margin:0 auto;padding:32px 16px}.survey-brand{display:flex;margin-bottom:20px}.survey-brand__logo{height:28px;width:auto}.survey-screen{display:flex;flex-direction:column;gap:24px}.survey-screen__title{font-size:1.5rem;font-weight:600;margin:0}.survey-screen__description{color:#555;margin:0}.survey-screen__questions{display:flex;flex-direction:column;gap:24px}.survey-screen__actions{display:flex;justify-content:flex-end}.survey-screen__error{color:#b42318;background:#fef3f2;border:1px solid #fecdca;padding:12px 14px;border-radius:8px;margin:0}.survey-question-slot--invalid{border-inline-start:3px solid #b42318;padding-inline-start:10px}.survey-question__required-error{color:#b42318;font-size:.9rem;margin:4px 0 0}.survey-question{display:flex;flex-direction:column;gap:8px}.survey-question__label{font-weight:600;display:block}.survey-question__required{color:#b42318}.survey-question__help{margin:0;color:#666;font-size:.9rem}.survey-question__input{padding:10px 12px;border:1px solid #d0d5dd;border-radius:8px;font:inherit}.survey-question__input:focus-visible{outline:2px solid var(--survey-primary);outline-offset:1px;border-color:var(--survey-primary)}.survey-question--nps{border:none;padding:0;margin:0;display:flex;flex-direction:column;gap:8px}.survey-question__nps-scale{display:flex;gap:6px;flex-wrap:wrap}.survey-question__nps-step{min-width:40px;min-height:40px;padding:8px;border:1px solid #d0d5dd;border-radius:8px;background:#fff;font-weight:500;cursor:pointer}.survey-question__nps-step:hover{background:#f5f7fa}.survey-question__nps-step--selected{background:var(--survey-primary);border-color:var(--survey-primary);color:var(--survey-primary-contrast)}.survey-question__nps-labels{display:flex;justify-content:space-between;color:#555;font-size:.85rem}.survey-question--navlist{gap:12px}.survey-navlist{list-style:none;padding:0;margin:0;display:flex;flex-direction:column;gap:8px}.survey-navlist__row{margin:0}.survey-navlist__button{width:100%;display:flex;align-items:center;justify-content:space-between;padding:14px 16px;border:1px solid #d0d5dd;border-radius:10px;background:#fff;cursor:pointer;font:inherit;text-align:start}.survey-navlist__button:hover{background:#f5f7fa;border-color:var(--survey-primary)}.survey-navlist__chevron{font-size:1.5rem;color:#667085}.survey-root[dir=rtl] .survey-navlist__chevron{transform:scaleX(-1)}.survey-navlist__label{font-weight:500}.survey-question__textarea{padding:10px 12px;border:1px solid #d0d5dd;border-radius:8px;font:inherit;resize:vertical;min-height:96px}.survey-question__textarea:focus-visible{outline:2px solid var(--survey-primary);outline-offset:1px;border-color:var(--survey-primary)}.survey-question__number-wrap{display:flex;align-items:center;gap:8px}.survey-question__number-wrap .survey-question__input{flex:1}.survey-question__unit{color:#555;font-size:.9rem}.survey-question__rating-scale{display:flex;gap:4px}.survey-question__rating-star{background:transparent;border:none;cursor:pointer;font-size:1.8rem;line-height:1;color:#d0d5dd;padding:4px}.survey-question__rating-star:hover,.survey-question__rating-star--selected{color:var(--survey-accent)}.survey-question__options{display:flex;flex-direction:column;gap:8px}.survey-question__option{display:flex;align-items:center;gap:8px;padding:8px 12px;border:1px solid #d0d5dd;border-radius:8px;cursor:pointer}.survey-question__option:hover{background:#f5f7fa;border-color:var(--survey-primary)}.survey-question__option input{margin:0}.survey-question__select{padding:10px 12px;border:1px solid #d0d5dd;border-radius:8px;font:inherit;background:#fff}.survey-question__yesno{display:flex;gap:12px}.survey-question__yesno-button{flex:1;padding:14px 16px;border:1px solid #d0d5dd;border-radius:10px;background:#fff;font:inherit;font-weight:500;cursor:pointer}.survey-question__yesno-button:hover{background:#f5f7fa;border-color:var(--survey-primary)}.survey-question__yesno-button--selected{background:var(--survey-primary);border-color:var(--survey-primary);color:var(--survey-primary-contrast)}.survey-question__file{font:inherit}.survey-question__file-name{color:#555;font-size:.9rem;margin:0}.survey-question__signature-canvas{width:100%;max-width:480px;height:auto;aspect-ratio:3 / 1;border:1px dashed #d0d5dd;border-radius:8px;background:#fff;touch-action:none}.survey-question__signature-actions{display:flex;justify-content:flex-start;gap:8px}.survey-button{padding:10px 20px;border-radius:8px;border:1px solid transparent;cursor:pointer;font:inherit;font-weight:600}.survey-button--primary{background:var(--survey-primary);color:var(--survey-primary-contrast)}.survey-button--primary:hover{background:var(--survey-primary-hover)}.survey-button--ghost{background:#fff;color:#555;border-color:#d0d5dd}.survey-button--ghost:hover{background:#f5f7fa}.survey-button:disabled{opacity:.5;cursor:not-allowed}";
var Ze, la, Ll, Ve, ea, ou, Kt, Kf, ru, Ia, Pu;
class Xm extends HTMLElement {
  constructor() {
    super();
    ve(this, Kt);
    /** Schema-mode setter. Assigning this swaps the element into schema mode and
     *  re-renders with the new schema immediately. */
    ve(this, Ze, null);
    /** Schema-mode submit handler. In API mode the element manages this itself. */
    ve(this, la, null);
    ve(this, Ll, null);
    ve(this, Ve, null);
    ve(this, ea, null);
    ve(this, ou, null);
    ve(this, Ia, !1);
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
        r.setAttribute("data-shift-survey", ""), r.textContent = Qm, this.shadowRoot.appendChild(r);
      }
      Ct(this, Ve) || (Yl(this, Ve, document.createElement("div")), Ct(this, Ve).className = "shift-survey-mount", this.shadowRoot.appendChild(Ct(this, Ve))), Ct(this, Ll) || Yl(this, Ll, Zh.createRoot(Ct(this, Ve))), dl(this, Kt, ru).call(this), dl(this, Kt, Kf).call(this);
    }
  }
  disconnectedCallback() {
    queueMicrotask(() => {
      var r;
      if (!(this.isConnected || typeof window > "u")) {
        try {
          (r = Ct(this, Ll)) == null || r.unmount();
        } catch {
        }
        Yl(this, Ll, null);
      }
    });
  }
  attributeChangedCallback(r, s, q) {
    s !== q && ((r === "instance-id" || r === "api-base") && (Yl(this, ea, null), Yl(this, ou, null), dl(this, Kt, Kf).call(this)), dl(this, Kt, ru).call(this));
  }
  // ─── Properties ──────────────────────────────────────────────────────────
  get schema() {
    return Ct(this, Ze);
  }
  set schema(r) {
    Yl(this, Ze, r), dl(this, Kt, ru).call(this);
  }
  get onSubmit() {
    return Ct(this, la);
  }
  set onSubmit(r) {
    Yl(this, la, r), dl(this, Kt, ru).call(this);
  }
}
Ze = new WeakMap(), la = new WeakMap(), Ll = new WeakMap(), Ve = new WeakMap(), ea = new WeakMap(), ou = new WeakMap(), Kt = new WeakSet(), // ─── Internals ───────────────────────────────────────────────────────────
Kf = function() {
  if (Ct(this, Ze)) return;
  const r = this.getAttribute("instance-id");
  if (!r) return;
  const s = this.getAttribute("api-base");
  if (!s) return;
  new cy({ baseUrl: s }).fetchSchema(r).then((E) => {
    Yl(this, ea, E), dl(this, Kt, ru).call(this);
  }).catch((E) => {
    Yl(this, ou, E), dl(this, Kt, Pu).call(this, "survey:error", { message: E.message }), dl(this, Kt, ru).call(this);
  });
}, ru = function() {
  if (!Ct(this, Ll)) return;
  const r = this.getAttribute("api-base"), s = this.getAttribute("instance-id"), q = this.getAttribute("locale") ?? void 0, E = this.getAttribute("mode") === "agent", C = Ct(this, Ze) ?? Ct(this, ea);
  if (Ct(this, ou) && !C) {
    Ct(this, Ll).render(
      ut.createElement(
        "div",
        { className: "shift-survey-error", role: "alert" },
        Ct(this, ou).message
      )
    );
    return;
  }
  if (!C) {
    Ct(this, Ll).render(
      ut.createElement("div", { className: "shift-survey-loading" }, "Loading…")
    );
    return;
  }
  const U = Ct(this, Ze) ? Ct(this, la) ?? ((O) => {
    dl(this, Kt, Pu).call(this, "survey:completed", { ...O });
  }) : async (O) => {
    if (!r || !s)
      throw new Error("shift-survey: API mode requires both instance-id and api-base attributes.");
    await new cy({ baseUrl: r }).submitResponse(s, O);
  };
  Ct(this, Ll).render(
    ut.createElement(Gm, {
      schema: C,
      onSubmit: U,
      ...q ? { locale: q } : {},
      // Let the element be the resume key in API mode so two surveys on the
      // same host page don't clobber each other.
      ...s ? { resumeKey: s } : {},
      ...E ? { submissionMeta: { mode: "agent" } } : {},
      // CustomEvents are the web-component's channel; postMessage stays opt-in
      // via iframe auto-detect on the enclosing page (unchanged).
      onScreenChange: (O) => dl(this, Kt, Pu).call(this, "survey:screen-changed", { screenId: O }),
      onCompleted: (O) => dl(this, Kt, Pu).call(this, "survey:completed", { screenId: O })
    })
  ), Ct(this, Ia) || (Yl(this, Ia, !0), dl(this, Kt, Pu).call(this, "survey:loaded", {}));
}, Ia = new WeakMap(), Pu = function(r, s) {
  this.dispatchEvent(
    new CustomEvent(r, { detail: s, bubbles: !0, composed: !0 })
  );
};
function Zm(c = "shift-survey") {
  typeof window > "u" || typeof customElements > "u" || customElements.get(c) || customElements.define(c, Xm);
}
Zm();
export {
  Xm as ShiftSurveyElement,
  Zm as registerShiftSurvey
};
//# sourceMappingURL=index.js.map
