var Jh = Object.defineProperty;
var iy = (c) => {
  throw TypeError(c);
};
var Kh = (c, f, r) => f in c ? Jh(c, f, { enumerable: !0, configurable: !0, writable: !0, value: r }) : c[f] = r;
var gl = (c, f, r) => Kh(c, typeof f != "symbol" ? f + "" : f, r), Vf = (c, f, r) => f.has(c) || iy("Cannot " + r);
var Dt = (c, f, r) => (Vf(c, f, "read from private field"), r ? r.call(c) : f.get(c)), Ve = (c, f, r) => f.has(c) ? iy("Cannot add the same private member more than once") : f instanceof WeakSet ? f.add(c) : f.set(c, r), xe = (c, f, r, s) => (Vf(c, f, "write to private field"), s ? s.call(c, r) : f.set(c, r), r), le = (c, f, r) => (Vf(c, f, "access private method"), r);
var Jf = { exports: {} }, I = {};
/**
 * @license React
 * react.production.js
 *
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 */
var cy;
function wh() {
  if (cy) return I;
  cy = 1;
  var c = Symbol.for("react.transitional.element"), f = Symbol.for("react.portal"), r = Symbol.for("react.fragment"), s = Symbol.for("react.strict_mode"), h = Symbol.for("react.profiler"), g = Symbol.for("react.consumer"), N = Symbol.for("react.context"), D = Symbol.for("react.forward_ref"), q = Symbol.for("react.suspense"), p = Symbol.for("react.memo"), j = Symbol.for("react.lazy"), E = Symbol.for("react.activity"), C = Symbol.iterator;
  function G(m) {
    return m === null || typeof m != "object" ? null : (m = C && m[C] || m["@@iterator"], typeof m == "function" ? m : null);
  }
  var J = {
    isMounted: function() {
      return !1;
    },
    enqueueForceUpdate: function() {
    },
    enqueueReplaceState: function() {
    },
    enqueueSetState: function() {
    }
  }, K = Object.assign, Y = {};
  function w(m, U, R) {
    this.props = m, this.context = U, this.refs = Y, this.updater = R || J;
  }
  w.prototype.isReactComponent = {}, w.prototype.setState = function(m, U) {
    if (typeof m != "object" && typeof m != "function" && m != null)
      throw Error(
        "takes an object of state variables to update or a function which returns an object of state variables."
      );
    this.updater.enqueueSetState(this, m, U, "setState");
  }, w.prototype.forceUpdate = function(m) {
    this.updater.enqueueForceUpdate(this, m, "forceUpdate");
  };
  function ct() {
  }
  ct.prototype = w.prototype;
  function et(m, U, R) {
    this.props = m, this.context = U, this.refs = Y, this.updater = R || J;
  }
  var tt = et.prototype = new ct();
  tt.constructor = et, K(tt, w.prototype), tt.isPureReactComponent = !0;
  var Ut = Array.isArray;
  function Ct() {
  }
  var Z = { H: null, A: null, T: null, S: null }, ee = Object.prototype.hasOwnProperty;
  function lt(m, U, R) {
    var H = R.ref;
    return {
      $$typeof: c,
      type: m,
      key: U,
      ref: H !== void 0 ? H : null,
      props: R
    };
  }
  function Le(m, U) {
    return lt(m.type, U, m.props);
  }
  function ne(m) {
    return typeof m == "object" && m !== null && m.$$typeof === c;
  }
  function kt(m) {
    var U = { "=": "=0", ":": "=2" };
    return "$" + m.replace(/[=:]/g, function(R) {
      return U[R];
    });
  }
  var he = /\/+/g;
  function qe(m, U) {
    return typeof m == "object" && m !== null && m.key != null ? kt("" + m.key) : U.toString(36);
  }
  function ve(m) {
    switch (m.status) {
      case "fulfilled":
        return m.value;
      case "rejected":
        throw m.reason;
      default:
        switch (typeof m.status == "string" ? m.then(Ct, Ct) : (m.status = "pending", m.then(
          function(U) {
            m.status === "pending" && (m.status = "fulfilled", m.value = U);
          },
          function(U) {
            m.status === "pending" && (m.status = "rejected", m.reason = U);
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
  function z(m, U, R, H, F) {
    var at = typeof m;
    (at === "undefined" || at === "boolean") && (m = null);
    var dt = !1;
    if (m === null) dt = !0;
    else
      switch (at) {
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
              return dt = m._init, z(
                dt(m._payload),
                U,
                R,
                H,
                F
              );
          }
      }
    if (dt)
      return F = F(m), dt = H === "" ? "." + qe(m, 0) : H, Ut(F) ? (R = "", dt != null && (R = dt.replace(he, "$&/") + "/"), z(F, U, R, "", function(Wl) {
        return Wl;
      })) : F != null && (ne(F) && (F = Le(
        F,
        R + (F.key == null || m && m.key === F.key ? "" : ("" + F.key).replace(
          he,
          "$&/"
        ) + "/") + dt
      )), U.push(F)), 1;
    dt = 0;
    var Zt = H === "" ? "." : H + ":";
    if (Ut(m))
      for (var At = 0; At < m.length; At++)
        H = m[At], at = Zt + qe(H, At), dt += z(
          H,
          U,
          R,
          at,
          F
        );
    else if (At = G(m), typeof At == "function")
      for (m = At.call(m), At = 0; !(H = m.next()).done; )
        H = H.value, at = Zt + qe(H, At++), dt += z(
          H,
          U,
          R,
          at,
          F
        );
    else if (at === "object") {
      if (typeof m.then == "function")
        return z(
          ve(m),
          U,
          R,
          H,
          F
        );
      throw U = String(m), Error(
        "Objects are not valid as a React child (found: " + (U === "[object Object]" ? "object with keys {" + Object.keys(m).join(", ") + "}" : U) + "). If you meant to render a collection of children, use an array instead."
      );
    }
    return dt;
  }
  function B(m, U, R) {
    if (m == null) return m;
    var H = [], F = 0;
    return z(m, H, "", "", function(at) {
      return U.call(R, at, F++);
    }), H;
  }
  function k(m) {
    if (m._status === -1) {
      var U = m._result;
      U = U(), U.then(
        function(R) {
          (m._status === 0 || m._status === -1) && (m._status = 1, m._result = R);
        },
        function(R) {
          (m._status === 0 || m._status === -1) && (m._status = 2, m._result = R);
        }
      ), m._status === -1 && (m._status = 0, m._result = U);
    }
    if (m._status === 1) return m._result.default;
    throw m._result;
  }
  var it = typeof reportError == "function" ? reportError : function(m) {
    if (typeof window == "object" && typeof window.ErrorEvent == "function") {
      var U = new window.ErrorEvent("error", {
        bubbles: !0,
        cancelable: !0,
        message: typeof m == "object" && m !== null && typeof m.message == "string" ? String(m.message) : String(m),
        error: m
      });
      if (!window.dispatchEvent(U)) return;
    } else if (typeof process == "object" && typeof process.emit == "function") {
      process.emit("uncaughtException", m);
      return;
    }
    console.error(m);
  }, St = {
    map: B,
    forEach: function(m, U, R) {
      B(
        m,
        function() {
          U.apply(this, arguments);
        },
        R
      );
    },
    count: function(m) {
      var U = 0;
      return B(m, function() {
        U++;
      }), U;
    },
    toArray: function(m) {
      return B(m, function(U) {
        return U;
      }) || [];
    },
    only: function(m) {
      if (!ne(m))
        throw Error(
          "React.Children.only expected to receive a single React element child."
        );
      return m;
    }
  };
  return I.Activity = E, I.Children = St, I.Component = w, I.Fragment = r, I.Profiler = h, I.PureComponent = et, I.StrictMode = s, I.Suspense = q, I.__CLIENT_INTERNALS_DO_NOT_USE_OR_WARN_USERS_THEY_CANNOT_UPGRADE = Z, I.__COMPILER_RUNTIME = {
    __proto__: null,
    c: function(m) {
      return Z.H.useMemoCache(m);
    }
  }, I.cache = function(m) {
    return function() {
      return m.apply(null, arguments);
    };
  }, I.cacheSignal = function() {
    return null;
  }, I.cloneElement = function(m, U, R) {
    if (m == null)
      throw Error(
        "The argument must be a React element, but you passed " + m + "."
      );
    var H = K({}, m.props), F = m.key;
    if (U != null)
      for (at in U.key !== void 0 && (F = "" + U.key), U)
        !ee.call(U, at) || at === "key" || at === "__self" || at === "__source" || at === "ref" && U.ref === void 0 || (H[at] = U[at]);
    var at = arguments.length - 2;
    if (at === 1) H.children = R;
    else if (1 < at) {
      for (var dt = Array(at), Zt = 0; Zt < at; Zt++)
        dt[Zt] = arguments[Zt + 2];
      H.children = dt;
    }
    return lt(m.type, F, H);
  }, I.createContext = function(m) {
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
  }, I.createElement = function(m, U, R) {
    var H, F = {}, at = null;
    if (U != null)
      for (H in U.key !== void 0 && (at = "" + U.key), U)
        ee.call(U, H) && H !== "key" && H !== "__self" && H !== "__source" && (F[H] = U[H]);
    var dt = arguments.length - 2;
    if (dt === 1) F.children = R;
    else if (1 < dt) {
      for (var Zt = Array(dt), At = 0; At < dt; At++)
        Zt[At] = arguments[At + 2];
      F.children = Zt;
    }
    if (m && m.defaultProps)
      for (H in dt = m.defaultProps, dt)
        F[H] === void 0 && (F[H] = dt[H]);
    return lt(m, at, F);
  }, I.createRef = function() {
    return { current: null };
  }, I.forwardRef = function(m) {
    return { $$typeof: D, render: m };
  }, I.isValidElement = ne, I.lazy = function(m) {
    return {
      $$typeof: j,
      _payload: { _status: -1, _result: m },
      _init: k
    };
  }, I.memo = function(m, U) {
    return {
      $$typeof: p,
      type: m,
      compare: U === void 0 ? null : U
    };
  }, I.startTransition = function(m) {
    var U = Z.T, R = {};
    Z.T = R;
    try {
      var H = m(), F = Z.S;
      F !== null && F(R, H), typeof H == "object" && H !== null && typeof H.then == "function" && H.then(Ct, it);
    } catch (at) {
      it(at);
    } finally {
      U !== null && R.types !== null && (U.types = R.types), Z.T = U;
    }
  }, I.unstable_useCacheRefresh = function() {
    return Z.H.useCacheRefresh();
  }, I.use = function(m) {
    return Z.H.use(m);
  }, I.useActionState = function(m, U, R) {
    return Z.H.useActionState(m, U, R);
  }, I.useCallback = function(m, U) {
    return Z.H.useCallback(m, U);
  }, I.useContext = function(m) {
    return Z.H.useContext(m);
  }, I.useDebugValue = function() {
  }, I.useDeferredValue = function(m, U) {
    return Z.H.useDeferredValue(m, U);
  }, I.useEffect = function(m, U) {
    return Z.H.useEffect(m, U);
  }, I.useEffectEvent = function(m) {
    return Z.H.useEffectEvent(m);
  }, I.useId = function() {
    return Z.H.useId();
  }, I.useImperativeHandle = function(m, U, R) {
    return Z.H.useImperativeHandle(m, U, R);
  }, I.useInsertionEffect = function(m, U) {
    return Z.H.useInsertionEffect(m, U);
  }, I.useLayoutEffect = function(m, U) {
    return Z.H.useLayoutEffect(m, U);
  }, I.useMemo = function(m, U) {
    return Z.H.useMemo(m, U);
  }, I.useOptimistic = function(m, U) {
    return Z.H.useOptimistic(m, U);
  }, I.useReducer = function(m, U, R) {
    return Z.H.useReducer(m, U, R);
  }, I.useRef = function(m) {
    return Z.H.useRef(m);
  }, I.useState = function(m) {
    return Z.H.useState(m);
  }, I.useSyncExternalStore = function(m, U, R) {
    return Z.H.useSyncExternalStore(
      m,
      U,
      R
    );
  }, I.useTransition = function() {
    return Z.H.useTransition();
  }, I.version = "19.2.5", I;
}
var fy;
function is() {
  return fy || (fy = 1, Jf.exports = wh()), Jf.exports;
}
var W = is(), Kf = { exports: {} }, un = {}, wf = { exports: {} }, kf = {};
/**
 * @license React
 * scheduler.production.js
 *
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 */
var sy;
function kh() {
  return sy || (sy = 1, (function(c) {
    function f(z, B) {
      var k = z.length;
      z.push(B);
      t: for (; 0 < k; ) {
        var it = k - 1 >>> 1, St = z[it];
        if (0 < h(St, B))
          z[it] = B, z[k] = St, k = it;
        else break t;
      }
    }
    function r(z) {
      return z.length === 0 ? null : z[0];
    }
    function s(z) {
      if (z.length === 0) return null;
      var B = z[0], k = z.pop();
      if (k !== B) {
        z[0] = k;
        t: for (var it = 0, St = z.length, m = St >>> 1; it < m; ) {
          var U = 2 * (it + 1) - 1, R = z[U], H = U + 1, F = z[H];
          if (0 > h(R, k))
            H < St && 0 > h(F, R) ? (z[it] = F, z[H] = k, it = H) : (z[it] = R, z[U] = k, it = U);
          else if (H < St && 0 > h(F, k))
            z[it] = F, z[H] = k, it = H;
          else break t;
        }
      }
      return B;
    }
    function h(z, B) {
      var k = z.sortIndex - B.sortIndex;
      return k !== 0 ? k : z.id - B.id;
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
    var q = [], p = [], j = 1, E = null, C = 3, G = !1, J = !1, K = !1, Y = !1, w = typeof setTimeout == "function" ? setTimeout : null, ct = typeof clearTimeout == "function" ? clearTimeout : null, et = typeof setImmediate < "u" ? setImmediate : null;
    function tt(z) {
      for (var B = r(p); B !== null; ) {
        if (B.callback === null) s(p);
        else if (B.startTime <= z)
          s(p), B.sortIndex = B.expirationTime, f(q, B);
        else break;
        B = r(p);
      }
    }
    function Ut(z) {
      if (K = !1, tt(z), !J)
        if (r(q) !== null)
          J = !0, Ct || (Ct = !0, kt());
        else {
          var B = r(p);
          B !== null && ve(Ut, B.startTime - z);
        }
    }
    var Ct = !1, Z = -1, ee = 5, lt = -1;
    function Le() {
      return Y ? !0 : !(c.unstable_now() - lt < ee);
    }
    function ne() {
      if (Y = !1, Ct) {
        var z = c.unstable_now();
        lt = z;
        var B = !0;
        try {
          t: {
            J = !1, K && (K = !1, ct(Z), Z = -1), G = !0;
            var k = C;
            try {
              e: {
                for (tt(z), E = r(q); E !== null && !(E.expirationTime > z && Le()); ) {
                  var it = E.callback;
                  if (typeof it == "function") {
                    E.callback = null, C = E.priorityLevel;
                    var St = it(
                      E.expirationTime <= z
                    );
                    if (z = c.unstable_now(), typeof St == "function") {
                      E.callback = St, tt(z), B = !0;
                      break e;
                    }
                    E === r(q) && s(q), tt(z);
                  } else s(q);
                  E = r(q);
                }
                if (E !== null) B = !0;
                else {
                  var m = r(p);
                  m !== null && ve(
                    Ut,
                    m.startTime - z
                  ), B = !1;
                }
              }
              break t;
            } finally {
              E = null, C = k, G = !1;
            }
            B = void 0;
          }
        } finally {
          B ? kt() : Ct = !1;
        }
      }
    }
    var kt;
    if (typeof et == "function")
      kt = function() {
        et(ne);
      };
    else if (typeof MessageChannel < "u") {
      var he = new MessageChannel(), qe = he.port2;
      he.port1.onmessage = ne, kt = function() {
        qe.postMessage(null);
      };
    } else
      kt = function() {
        w(ne, 0);
      };
    function ve(z, B) {
      Z = w(function() {
        z(c.unstable_now());
      }, B);
    }
    c.unstable_IdlePriority = 5, c.unstable_ImmediatePriority = 1, c.unstable_LowPriority = 4, c.unstable_NormalPriority = 3, c.unstable_Profiling = null, c.unstable_UserBlockingPriority = 2, c.unstable_cancelCallback = function(z) {
      z.callback = null;
    }, c.unstable_forceFrameRate = function(z) {
      0 > z || 125 < z ? console.error(
        "forceFrameRate takes a positive int between 0 and 125, forcing frame rates higher than 125 fps is not supported"
      ) : ee = 0 < z ? Math.floor(1e3 / z) : 5;
    }, c.unstable_getCurrentPriorityLevel = function() {
      return C;
    }, c.unstable_next = function(z) {
      switch (C) {
        case 1:
        case 2:
        case 3:
          var B = 3;
          break;
        default:
          B = C;
      }
      var k = C;
      C = B;
      try {
        return z();
      } finally {
        C = k;
      }
    }, c.unstable_requestPaint = function() {
      Y = !0;
    }, c.unstable_runWithPriority = function(z, B) {
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
      var k = C;
      C = z;
      try {
        return B();
      } finally {
        C = k;
      }
    }, c.unstable_scheduleCallback = function(z, B, k) {
      var it = c.unstable_now();
      switch (typeof k == "object" && k !== null ? (k = k.delay, k = typeof k == "number" && 0 < k ? it + k : it) : k = it, z) {
        case 1:
          var St = -1;
          break;
        case 2:
          St = 250;
          break;
        case 5:
          St = 1073741823;
          break;
        case 4:
          St = 1e4;
          break;
        default:
          St = 5e3;
      }
      return St = k + St, z = {
        id: j++,
        callback: B,
        priorityLevel: z,
        startTime: k,
        expirationTime: St,
        sortIndex: -1
      }, k > it ? (z.sortIndex = k, f(p, z), r(q) === null && z === r(p) && (K ? (ct(Z), Z = -1) : K = !0, ve(Ut, k - it))) : (z.sortIndex = St, f(q, z), J || G || (J = !0, Ct || (Ct = !0, kt()))), z;
    }, c.unstable_shouldYield = Le, c.unstable_wrapCallback = function(z) {
      var B = C;
      return function() {
        var k = C;
        C = B;
        try {
          return z.apply(this, arguments);
        } finally {
          C = k;
        }
      };
    };
  })(kf)), kf;
}
var ry;
function $h() {
  return ry || (ry = 1, wf.exports = kh()), wf.exports;
}
var $f = { exports: {} }, ae = {};
/**
 * @license React
 * react-dom.production.js
 *
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 */
var oy;
function Wh() {
  if (oy) return ae;
  oy = 1;
  var c = is();
  function f(q) {
    var p = "https://react.dev/errors/" + q;
    if (1 < arguments.length) {
      p += "?args[]=" + encodeURIComponent(arguments[1]);
      for (var j = 2; j < arguments.length; j++)
        p += "&args[]=" + encodeURIComponent(arguments[j]);
    }
    return "Minified React error #" + q + "; visit " + p + " for the full message or use the non-minified dev environment for full errors and additional helpful warnings.";
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
  function g(q, p, j) {
    var E = 3 < arguments.length && arguments[3] !== void 0 ? arguments[3] : null;
    return {
      $$typeof: h,
      key: E == null ? null : "" + E,
      children: q,
      containerInfo: p,
      implementation: j
    };
  }
  var N = c.__CLIENT_INTERNALS_DO_NOT_USE_OR_WARN_USERS_THEY_CANNOT_UPGRADE;
  function D(q, p) {
    if (q === "font") return "";
    if (typeof p == "string")
      return p === "use-credentials" ? p : "";
  }
  return ae.__DOM_INTERNALS_DO_NOT_USE_OR_WARN_USERS_THEY_CANNOT_UPGRADE = s, ae.createPortal = function(q, p) {
    var j = 2 < arguments.length && arguments[2] !== void 0 ? arguments[2] : null;
    if (!p || p.nodeType !== 1 && p.nodeType !== 9 && p.nodeType !== 11)
      throw Error(f(299));
    return g(q, p, null, j);
  }, ae.flushSync = function(q) {
    var p = N.T, j = s.p;
    try {
      if (N.T = null, s.p = 2, q) return q();
    } finally {
      N.T = p, s.p = j, s.d.f();
    }
  }, ae.preconnect = function(q, p) {
    typeof q == "string" && (p ? (p = p.crossOrigin, p = typeof p == "string" ? p === "use-credentials" ? p : "" : void 0) : p = null, s.d.C(q, p));
  }, ae.prefetchDNS = function(q) {
    typeof q == "string" && s.d.D(q);
  }, ae.preinit = function(q, p) {
    if (typeof q == "string" && p && typeof p.as == "string") {
      var j = p.as, E = D(j, p.crossOrigin), C = typeof p.integrity == "string" ? p.integrity : void 0, G = typeof p.fetchPriority == "string" ? p.fetchPriority : void 0;
      j === "style" ? s.d.S(
        q,
        typeof p.precedence == "string" ? p.precedence : void 0,
        {
          crossOrigin: E,
          integrity: C,
          fetchPriority: G
        }
      ) : j === "script" && s.d.X(q, {
        crossOrigin: E,
        integrity: C,
        fetchPriority: G,
        nonce: typeof p.nonce == "string" ? p.nonce : void 0
      });
    }
  }, ae.preinitModule = function(q, p) {
    if (typeof q == "string")
      if (typeof p == "object" && p !== null) {
        if (p.as == null || p.as === "script") {
          var j = D(
            p.as,
            p.crossOrigin
          );
          s.d.M(q, {
            crossOrigin: j,
            integrity: typeof p.integrity == "string" ? p.integrity : void 0,
            nonce: typeof p.nonce == "string" ? p.nonce : void 0
          });
        }
      } else p == null && s.d.M(q);
  }, ae.preload = function(q, p) {
    if (typeof q == "string" && typeof p == "object" && p !== null && typeof p.as == "string") {
      var j = p.as, E = D(j, p.crossOrigin);
      s.d.L(q, j, {
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
  }, ae.preloadModule = function(q, p) {
    if (typeof q == "string")
      if (p) {
        var j = D(p.as, p.crossOrigin);
        s.d.m(q, {
          as: typeof p.as == "string" && p.as !== "script" ? p.as : void 0,
          crossOrigin: j,
          integrity: typeof p.integrity == "string" ? p.integrity : void 0
        });
      } else s.d.m(q);
  }, ae.requestFormReset = function(q) {
    s.d.r(q);
  }, ae.unstable_batchedUpdates = function(q, p) {
    return q(p);
  }, ae.useFormState = function(q, p, j) {
    return N.H.useFormState(q, p, j);
  }, ae.useFormStatus = function() {
    return N.H.useHostTransitionStatus();
  }, ae.version = "19.2.5", ae;
}
var dy;
function Fh() {
  if (dy) return $f.exports;
  dy = 1;
  function c() {
    if (!(typeof __REACT_DEVTOOLS_GLOBAL_HOOK__ > "u" || typeof __REACT_DEVTOOLS_GLOBAL_HOOK__.checkDCE != "function"))
      try {
        __REACT_DEVTOOLS_GLOBAL_HOOK__.checkDCE(c);
      } catch (f) {
        console.error(f);
      }
  }
  return c(), $f.exports = Wh(), $f.exports;
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
var yy;
function Ih() {
  if (yy) return un;
  yy = 1;
  var c = $h(), f = is(), r = Fh();
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
  function q(t) {
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
          if (n === l) return q(u), t;
          if (n === a) return q(u), e;
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
  var E = Object.assign, C = Symbol.for("react.element"), G = Symbol.for("react.transitional.element"), J = Symbol.for("react.portal"), K = Symbol.for("react.fragment"), Y = Symbol.for("react.strict_mode"), w = Symbol.for("react.profiler"), ct = Symbol.for("react.consumer"), et = Symbol.for("react.context"), tt = Symbol.for("react.forward_ref"), Ut = Symbol.for("react.suspense"), Ct = Symbol.for("react.suspense_list"), Z = Symbol.for("react.memo"), ee = Symbol.for("react.lazy"), lt = Symbol.for("react.activity"), Le = Symbol.for("react.memo_cache_sentinel"), ne = Symbol.iterator;
  function kt(t) {
    return t === null || typeof t != "object" ? null : (t = ne && t[ne] || t["@@iterator"], typeof t == "function" ? t : null);
  }
  var he = Symbol.for("react.client.reference");
  function qe(t) {
    if (t == null) return null;
    if (typeof t == "function")
      return t.$$typeof === he ? null : t.displayName || t.name || null;
    if (typeof t == "string") return t;
    switch (t) {
      case K:
        return "Fragment";
      case w:
        return "Profiler";
      case Y:
        return "StrictMode";
      case Ut:
        return "Suspense";
      case Ct:
        return "SuspenseList";
      case lt:
        return "Activity";
    }
    if (typeof t == "object")
      switch (t.$$typeof) {
        case J:
          return "Portal";
        case et:
          return t.displayName || "Context";
        case ct:
          return (t._context.displayName || "Context") + ".Consumer";
        case tt:
          var e = t.render;
          return t = t.displayName, t || (t = e.displayName || e.name || "", t = t !== "" ? "ForwardRef(" + t + ")" : "ForwardRef"), t;
        case Z:
          return e = t.displayName || null, e !== null ? e : qe(t.type) || "Memo";
        case ee:
          e = t._payload, t = t._init;
          try {
            return qe(t(e));
          } catch {
          }
      }
    return null;
  }
  var ve = Array.isArray, z = f.__CLIENT_INTERNALS_DO_NOT_USE_OR_WARN_USERS_THEY_CANNOT_UPGRADE, B = r.__DOM_INTERNALS_DO_NOT_USE_OR_WARN_USERS_THEY_CANNOT_UPGRADE, k = {
    pending: !1,
    data: null,
    method: null,
    action: null
  }, it = [], St = -1;
  function m(t) {
    return { current: t };
  }
  function U(t) {
    0 > St || (t.current = it[St], it[St] = null, St--);
  }
  function R(t, e) {
    St++, it[St] = t.current, t.current = e;
  }
  var H = m(null), F = m(null), at = m(null), dt = m(null);
  function Zt(t, e) {
    switch (R(at, e), R(F, t), R(H, null), e.nodeType) {
      case 9:
      case 11:
        t = (t = e.documentElement) && (t = t.namespaceURI) ? Od(t) : 0;
        break;
      default:
        if (t = e.tagName, e = e.namespaceURI)
          e = Od(e), t = Md(e, t);
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
    U(H), R(H, t);
  }
  function At() {
    U(H), U(F), U(at);
  }
  function Wl(t) {
    t.memoizedState !== null && R(dt, t);
    var e = H.current, l = Md(e, t.type);
    e !== l && (R(F, t), R(H, l));
  }
  function Fl(t) {
    F.current === t && (U(H), U(F)), dt.current === t && (U(dt), tn._currentValue = k);
  }
  var Fe, rn;
  function Ge(t) {
    if (Fe === void 0)
      try {
        throw Error();
      } catch (l) {
        var e = l.stack.trim().match(/\n( *(at )?)/);
        Fe = e && e[1] || "", rn = -1 < l.stack.indexOf(`
    at`) ? " (<anonymous>)" : -1 < l.stack.indexOf("@") ? "@unknown:0:0" : "";
      }
    return `
` + Fe + t + rn;
  }
  var pa = !1;
  function Ie(t, e) {
    if (!t || pa) return "";
    pa = !0;
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
                  var x = `
` + d[a].replace(" at new ", " at ");
                  return t.displayName && x.includes("<anonymous>") && (x = x.replace("<anonymous>", t.displayName)), x;
                }
              while (1 <= a && 0 <= u);
            break;
          }
      }
    } finally {
      pa = !1, Error.prepareStackTrace = l;
    }
    return (l = t ? t.displayName || t.name : "") ? Ge(l) : "";
  }
  function Mi(t, e) {
    switch (t.tag) {
      case 26:
      case 27:
      case 5:
        return Ge(t.type);
      case 16:
        return Ge("Lazy");
      case 13:
        return t.child !== e && e !== null ? Ge("Suspense Fallback") : Ge("Suspense");
      case 19:
        return Ge("SuspenseList");
      case 0:
      case 15:
        return Ie(t.type, !1);
      case 11:
        return Ie(t.type.render, !1);
      case 1:
        return Ie(t.type, !0);
      case 31:
        return Ge("Activity");
      default:
        return "";
    }
  }
  function on(t) {
    try {
      var e = "", l = null;
      do
        e += Mi(t, l), l = t, t = t.return;
      while (t);
      return e;
    } catch (a) {
      return `
Error generating stack: ` + a.message + `
` + a.stack;
    }
  }
  var ba = Object.prototype.hasOwnProperty, Sl = c.unstable_scheduleCallback, ru = c.unstable_cancelCallback, X = c.unstable_shouldYield, pt = c.unstable_requestPaint, ht = c.unstable_now, Jt = c.unstable_getCurrentPriorityLevel, $t = c.unstable_ImmediatePriority, ou = c.unstable_UserBlockingPriority, dn = c.unstable_NormalPriority, xy = c.unstable_LowPriority, fs = c.unstable_IdlePriority, qy = c.log, Ny = c.unstable_setDisableYieldValue, du = null, ge = null;
  function _l(t) {
    if (typeof qy == "function" && Ny(t), ge && typeof ge.setStrictMode == "function")
      try {
        ge.setStrictMode(du, t);
      } catch {
      }
  }
  var pe = Math.clz32 ? Math.clz32 : Dy, Oy = Math.log, My = Math.LN2;
  function Dy(t) {
    return t >>>= 0, t === 0 ? 32 : 31 - (Oy(t) / My | 0) | 0;
  }
  var yn = 256, mn = 262144, hn = 4194304;
  function Il(t) {
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
  function vn(t, e, l) {
    var a = t.pendingLanes;
    if (a === 0) return 0;
    var u = 0, n = t.suspendedLanes, i = t.pingedLanes;
    t = t.warmLanes;
    var o = a & 134217727;
    return o !== 0 ? (a = o & ~n, a !== 0 ? u = Il(a) : (i &= o, i !== 0 ? u = Il(i) : l || (l = o & ~t, l !== 0 && (u = Il(l))))) : (o = a & ~n, o !== 0 ? u = Il(o) : i !== 0 ? u = Il(i) : l || (l = a & ~t, l !== 0 && (u = Il(l)))), u === 0 ? 0 : e !== 0 && e !== u && (e & n) === 0 && (n = u & -u, l = e & -e, n >= l || n === 32 && (l & 4194048) !== 0) ? e : u;
  }
  function yu(t, e) {
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
  function ss() {
    var t = hn;
    return hn <<= 1, (hn & 62914560) === 0 && (hn = 4194304), t;
  }
  function Di(t) {
    for (var e = [], l = 0; 31 > l; l++) e.push(t);
    return e;
  }
  function mu(t, e) {
    t.pendingLanes |= e, e !== 268435456 && (t.suspendedLanes = 0, t.pingedLanes = 0, t.warmLanes = 0);
  }
  function Cy(t, e, l, a, u, n) {
    var i = t.pendingLanes;
    t.pendingLanes = l, t.suspendedLanes = 0, t.pingedLanes = 0, t.warmLanes = 0, t.expiredLanes &= l, t.entangledLanes &= l, t.errorRecoveryDisabledLanes &= l, t.shellSuspendCounter = 0;
    var o = t.entanglements, d = t.expirationTimes, S = t.hiddenUpdates;
    for (l = i & ~l; 0 < l; ) {
      var x = 31 - pe(l), M = 1 << x;
      o[x] = 0, d[x] = -1;
      var _ = S[x];
      if (_ !== null)
        for (S[x] = null, x = 0; x < _.length; x++) {
          var A = _[x];
          A !== null && (A.lane &= -536870913);
        }
      l &= ~M;
    }
    a !== 0 && rs(t, a, 0), n !== 0 && u === 0 && t.tag !== 0 && (t.suspendedLanes |= n & ~(i & ~e));
  }
  function rs(t, e, l) {
    t.pendingLanes |= e, t.suspendedLanes &= ~e;
    var a = 31 - pe(e);
    t.entangledLanes |= e, t.entanglements[a] = t.entanglements[a] | 1073741824 | l & 261930;
  }
  function os(t, e) {
    var l = t.entangledLanes |= e;
    for (t = t.entanglements; l; ) {
      var a = 31 - pe(l), u = 1 << a;
      u & e | t[a] & e && (t[a] |= e), l &= ~u;
    }
  }
  function ds(t, e) {
    var l = e & -e;
    return l = (l & 42) !== 0 ? 1 : Ui(l), (l & (t.suspendedLanes | e)) !== 0 ? 0 : l;
  }
  function Ui(t) {
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
  function Ci(t) {
    return t &= -t, 2 < t ? 8 < t ? (t & 134217727) !== 0 ? 32 : 268435456 : 8 : 2;
  }
  function ys() {
    var t = B.p;
    return t !== 0 ? t : (t = window.event, t === void 0 ? 32 : Pd(t.type));
  }
  function ms(t, e) {
    var l = B.p;
    try {
      return B.p = t, e();
    } finally {
      B.p = l;
    }
  }
  var El = Math.random().toString(36).slice(2), Wt = "__reactFiber$" + El, ce = "__reactProps$" + El, Sa = "__reactContainer$" + El, ji = "__reactEvents$" + El, jy = "__reactListeners$" + El, Ry = "__reactHandles$" + El, hs = "__reactResources$" + El, hu = "__reactMarker$" + El;
  function Ri(t) {
    delete t[Wt], delete t[ce], delete t[ji], delete t[jy], delete t[Ry];
  }
  function _a(t) {
    var e = t[Wt];
    if (e) return e;
    for (var l = t.parentNode; l; ) {
      if (e = l[Sa] || l[Wt]) {
        if (l = e.alternate, e.child !== null || l !== null && l.child !== null)
          for (t = Bd(t); t !== null; ) {
            if (l = t[Wt]) return l;
            t = Bd(t);
          }
        return e;
      }
      t = l, l = t.parentNode;
    }
    return null;
  }
  function Ea(t) {
    if (t = t[Wt] || t[Sa]) {
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
  function Aa(t) {
    var e = t[hs];
    return e || (e = t[hs] = { hoistableStyles: /* @__PURE__ */ new Map(), hoistableScripts: /* @__PURE__ */ new Map() }), e;
  }
  function Kt(t) {
    t[hu] = !0;
  }
  var vs = /* @__PURE__ */ new Set(), gs = {};
  function Pl(t, e) {
    Ta(t, e), Ta(t + "Capture", e);
  }
  function Ta(t, e) {
    for (gs[t] = e, t = 0; t < e.length; t++)
      vs.add(e[t]);
  }
  var Hy = RegExp(
    "^[:A-Z_a-z\\u00C0-\\u00D6\\u00D8-\\u00F6\\u00F8-\\u02FF\\u0370-\\u037D\\u037F-\\u1FFF\\u200C-\\u200D\\u2070-\\u218F\\u2C00-\\u2FEF\\u3001-\\uD7FF\\uF900-\\uFDCF\\uFDF0-\\uFFFD][:A-Z_a-z\\u00C0-\\u00D6\\u00D8-\\u00F6\\u00F8-\\u02FF\\u0370-\\u037D\\u037F-\\u1FFF\\u200C-\\u200D\\u2070-\\u218F\\u2C00-\\u2FEF\\u3001-\\uD7FF\\uF900-\\uFDCF\\uFDF0-\\uFFFD\\-.0-9\\u00B7\\u0300-\\u036F\\u203F-\\u2040]*$"
  ), ps = {}, bs = {};
  function By(t) {
    return ba.call(bs, t) ? !0 : ba.call(ps, t) ? !1 : Hy.test(t) ? bs[t] = !0 : (ps[t] = !0, !1);
  }
  function gn(t, e, l) {
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
  function pn(t, e, l) {
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
  function Ne(t) {
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
  function Ss(t) {
    var e = t.type;
    return (t = t.nodeName) && t.toLowerCase() === "input" && (e === "checkbox" || e === "radio");
  }
  function Yy(t, e, l) {
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
  function Hi(t) {
    if (!t._valueTracker) {
      var e = Ss(t) ? "checked" : "value";
      t._valueTracker = Yy(
        t,
        e,
        "" + t[e]
      );
    }
  }
  function _s(t) {
    if (!t) return !1;
    var e = t._valueTracker;
    if (!e) return !0;
    var l = e.getValue(), a = "";
    return t && (a = Ss(t) ? t.checked ? "true" : "false" : t.value), t = a, t !== l ? (e.setValue(t), !0) : !1;
  }
  function bn(t) {
    if (t = t || (typeof document < "u" ? document : void 0), typeof t > "u") return null;
    try {
      return t.activeElement || t.body;
    } catch {
      return t.body;
    }
  }
  var Ly = /[\n"\\]/g;
  function Oe(t) {
    return t.replace(
      Ly,
      function(e) {
        return "\\" + e.charCodeAt(0).toString(16) + " ";
      }
    );
  }
  function Bi(t, e, l, a, u, n, i, o) {
    t.name = "", i != null && typeof i != "function" && typeof i != "symbol" && typeof i != "boolean" ? t.type = i : t.removeAttribute("type"), e != null ? i === "number" ? (e === 0 && t.value === "" || t.value != e) && (t.value = "" + Ne(e)) : t.value !== "" + Ne(e) && (t.value = "" + Ne(e)) : i !== "submit" && i !== "reset" || t.removeAttribute("value"), e != null ? Yi(t, i, Ne(e)) : l != null ? Yi(t, i, Ne(l)) : a != null && t.removeAttribute("value"), u == null && n != null && (t.defaultChecked = !!n), u != null && (t.checked = u && typeof u != "function" && typeof u != "symbol"), o != null && typeof o != "function" && typeof o != "symbol" && typeof o != "boolean" ? t.name = "" + Ne(o) : t.removeAttribute("name");
  }
  function Es(t, e, l, a, u, n, i, o) {
    if (n != null && typeof n != "function" && typeof n != "symbol" && typeof n != "boolean" && (t.type = n), e != null || l != null) {
      if (!(n !== "submit" && n !== "reset" || e != null)) {
        Hi(t);
        return;
      }
      l = l != null ? "" + Ne(l) : "", e = e != null ? "" + Ne(e) : l, o || e === t.value || (t.value = e), t.defaultValue = e;
    }
    a = a ?? u, a = typeof a != "function" && typeof a != "symbol" && !!a, t.checked = o ? t.checked : !!a, t.defaultChecked = !!a, i != null && typeof i != "function" && typeof i != "symbol" && typeof i != "boolean" && (t.name = i), Hi(t);
  }
  function Yi(t, e, l) {
    e === "number" && bn(t.ownerDocument) === t || t.defaultValue === "" + l || (t.defaultValue = "" + l);
  }
  function za(t, e, l, a) {
    if (t = t.options, e) {
      e = {};
      for (var u = 0; u < l.length; u++)
        e["$" + l[u]] = !0;
      for (l = 0; l < t.length; l++)
        u = e.hasOwnProperty("$" + t[l].value), t[l].selected !== u && (t[l].selected = u), u && a && (t[l].defaultSelected = !0);
    } else {
      for (l = "" + Ne(l), e = null, u = 0; u < t.length; u++) {
        if (t[u].value === l) {
          t[u].selected = !0, a && (t[u].defaultSelected = !0);
          return;
        }
        e !== null || t[u].disabled || (e = t[u]);
      }
      e !== null && (e.selected = !0);
    }
  }
  function As(t, e, l) {
    if (e != null && (e = "" + Ne(e), e !== t.value && (t.value = e), l == null)) {
      t.defaultValue !== e && (t.defaultValue = e);
      return;
    }
    t.defaultValue = l != null ? "" + Ne(l) : "";
  }
  function Ts(t, e, l, a) {
    if (e == null) {
      if (a != null) {
        if (l != null) throw Error(s(92));
        if (ve(a)) {
          if (1 < a.length) throw Error(s(93));
          a = a[0];
        }
        l = a;
      }
      l == null && (l = ""), e = l;
    }
    l = Ne(e), t.defaultValue = l, a = t.textContent, a === l && a !== "" && a !== null && (t.value = a), Hi(t);
  }
  function xa(t, e) {
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
  function zs(t, e, l) {
    var a = e.indexOf("--") === 0;
    l == null || typeof l == "boolean" || l === "" ? a ? t.setProperty(e, "") : e === "float" ? t.cssFloat = "" : t[e] = "" : a ? t.setProperty(e, l) : typeof l != "number" || l === 0 || Gy.has(e) ? e === "float" ? t.cssFloat = l : t[e] = ("" + l).trim() : t[e] = l + "px";
  }
  function xs(t, e, l) {
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
  function Li(t) {
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
  function Sn(t) {
    return Xy.test("" + t) ? "javascript:throw new Error('React has blocked a javascript: URL as a security precaution.')" : t;
  }
  function tl() {
  }
  var Gi = null;
  function Qi(t) {
    return t = t.target || t.srcElement || window, t.correspondingUseElement && (t = t.correspondingUseElement), t.nodeType === 3 ? t.parentNode : t;
  }
  var qa = null, Na = null;
  function qs(t) {
    var e = Ea(t);
    if (e && (t = e.stateNode)) {
      var l = t[ce] || null;
      t: switch (t = e.stateNode, e.type) {
        case "input":
          if (Bi(
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
              'input[name="' + Oe(
                "" + e
              ) + '"][type="radio"]'
            ), e = 0; e < l.length; e++) {
              var a = l[e];
              if (a !== t && a.form === t.form) {
                var u = a[ce] || null;
                if (!u) throw Error(s(90));
                Bi(
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
              a = l[e], a.form === t.form && _s(a);
          }
          break t;
        case "textarea":
          As(t, l.value, l.defaultValue);
          break t;
        case "select":
          e = l.value, e != null && za(t, !!l.multiple, e, !1);
      }
    }
  }
  var Xi = !1;
  function Ns(t, e, l) {
    if (Xi) return t(e, l);
    Xi = !0;
    try {
      var a = t(e);
      return a;
    } finally {
      if (Xi = !1, (qa !== null || Na !== null) && (ci(), qa && (e = qa, t = Na, Na = qa = null, qs(e), t)))
        for (e = 0; e < t.length; e++) qs(t[e]);
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
  var el = !(typeof window > "u" || typeof window.document > "u" || typeof window.document.createElement > "u"), Zi = !1;
  if (el)
    try {
      var pu = {};
      Object.defineProperty(pu, "passive", {
        get: function() {
          Zi = !0;
        }
      }), window.addEventListener("test", pu, pu), window.removeEventListener("test", pu, pu);
    } catch {
      Zi = !1;
    }
  var Al = null, Vi = null, _n = null;
  function Os() {
    if (_n) return _n;
    var t, e = Vi, l = e.length, a, u = "value" in Al ? Al.value : Al.textContent, n = u.length;
    for (t = 0; t < l && e[t] === u[t]; t++) ;
    var i = l - t;
    for (a = 1; a <= i && e[l - a] === u[n - a]; a++) ;
    return _n = u.slice(t, 1 < a ? 1 - a : void 0);
  }
  function En(t) {
    var e = t.keyCode;
    return "charCode" in t ? (t = t.charCode, t === 0 && e === 13 && (t = 13)) : t = e, t === 10 && (t = 13), 32 <= t || t === 13 ? t : 0;
  }
  function An() {
    return !0;
  }
  function Ms() {
    return !1;
  }
  function fe(t) {
    function e(l, a, u, n, i) {
      this._reactName = l, this._targetInst = u, this.type = a, this.nativeEvent = n, this.target = i, this.currentTarget = null;
      for (var o in t)
        t.hasOwnProperty(o) && (l = t[o], this[o] = l ? l(n) : n[o]);
      return this.isDefaultPrevented = (n.defaultPrevented != null ? n.defaultPrevented : n.returnValue === !1) ? An : Ms, this.isPropagationStopped = Ms, this;
    }
    return E(e.prototype, {
      preventDefault: function() {
        this.defaultPrevented = !0;
        var l = this.nativeEvent;
        l && (l.preventDefault ? l.preventDefault() : typeof l.returnValue != "unknown" && (l.returnValue = !1), this.isDefaultPrevented = An);
      },
      stopPropagation: function() {
        var l = this.nativeEvent;
        l && (l.stopPropagation ? l.stopPropagation() : typeof l.cancelBubble != "unknown" && (l.cancelBubble = !0), this.isPropagationStopped = An);
      },
      persist: function() {
      },
      isPersistent: An
    }), e;
  }
  var ta = {
    eventPhase: 0,
    bubbles: 0,
    cancelable: 0,
    timeStamp: function(t) {
      return t.timeStamp || Date.now();
    },
    defaultPrevented: 0,
    isTrusted: 0
  }, Tn = fe(ta), bu = E({}, ta, { view: 0, detail: 0 }), Zy = fe(bu), Ji, Ki, Su, zn = E({}, bu, {
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
    getModifierState: ki,
    button: 0,
    buttons: 0,
    relatedTarget: function(t) {
      return t.relatedTarget === void 0 ? t.fromElement === t.srcElement ? t.toElement : t.fromElement : t.relatedTarget;
    },
    movementX: function(t) {
      return "movementX" in t ? t.movementX : (t !== Su && (Su && t.type === "mousemove" ? (Ji = t.screenX - Su.screenX, Ki = t.screenY - Su.screenY) : Ki = Ji = 0, Su = t), Ji);
    },
    movementY: function(t) {
      return "movementY" in t ? t.movementY : Ki;
    }
  }), Ds = fe(zn), Vy = E({}, zn, { dataTransfer: 0 }), Jy = fe(Vy), Ky = E({}, bu, { relatedTarget: 0 }), wi = fe(Ky), wy = E({}, ta, {
    animationName: 0,
    elapsedTime: 0,
    pseudoElement: 0
  }), ky = fe(wy), $y = E({}, ta, {
    clipboardData: function(t) {
      return "clipboardData" in t ? t.clipboardData : window.clipboardData;
    }
  }), Wy = fe($y), Fy = E({}, ta, { data: 0 }), Us = fe(Fy), Iy = {
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
  function ki() {
    return em;
  }
  var lm = E({}, bu, {
    key: function(t) {
      if (t.key) {
        var e = Iy[t.key] || t.key;
        if (e !== "Unidentified") return e;
      }
      return t.type === "keypress" ? (t = En(t), t === 13 ? "Enter" : String.fromCharCode(t)) : t.type === "keydown" || t.type === "keyup" ? Py[t.keyCode] || "Unidentified" : "";
    },
    code: 0,
    location: 0,
    ctrlKey: 0,
    shiftKey: 0,
    altKey: 0,
    metaKey: 0,
    repeat: 0,
    locale: 0,
    getModifierState: ki,
    charCode: function(t) {
      return t.type === "keypress" ? En(t) : 0;
    },
    keyCode: function(t) {
      return t.type === "keydown" || t.type === "keyup" ? t.keyCode : 0;
    },
    which: function(t) {
      return t.type === "keypress" ? En(t) : t.type === "keydown" || t.type === "keyup" ? t.keyCode : 0;
    }
  }), am = fe(lm), um = E({}, zn, {
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
  }), Cs = fe(um), nm = E({}, bu, {
    touches: 0,
    targetTouches: 0,
    changedTouches: 0,
    altKey: 0,
    metaKey: 0,
    ctrlKey: 0,
    shiftKey: 0,
    getModifierState: ki
  }), im = fe(nm), cm = E({}, ta, {
    propertyName: 0,
    elapsedTime: 0,
    pseudoElement: 0
  }), fm = fe(cm), sm = E({}, zn, {
    deltaX: function(t) {
      return "deltaX" in t ? t.deltaX : "wheelDeltaX" in t ? -t.wheelDeltaX : 0;
    },
    deltaY: function(t) {
      return "deltaY" in t ? t.deltaY : "wheelDeltaY" in t ? -t.wheelDeltaY : "wheelDelta" in t ? -t.wheelDelta : 0;
    },
    deltaZ: 0,
    deltaMode: 0
  }), rm = fe(sm), om = E({}, ta, {
    newState: 0,
    oldState: 0
  }), dm = fe(om), ym = [9, 13, 27, 32], $i = el && "CompositionEvent" in window, _u = null;
  el && "documentMode" in document && (_u = document.documentMode);
  var mm = el && "TextEvent" in window && !_u, js = el && (!$i || _u && 8 < _u && 11 >= _u), Rs = " ", Hs = !1;
  function Bs(t, e) {
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
  function Ys(t) {
    return t = t.detail, typeof t == "object" && "data" in t ? t.data : null;
  }
  var Oa = !1;
  function hm(t, e) {
    switch (t) {
      case "compositionend":
        return Ys(e);
      case "keypress":
        return e.which !== 32 ? null : (Hs = !0, Rs);
      case "textInput":
        return t = e.data, t === Rs && Hs ? null : t;
      default:
        return null;
    }
  }
  function vm(t, e) {
    if (Oa)
      return t === "compositionend" || !$i && Bs(t, e) ? (t = Os(), _n = Vi = Al = null, Oa = !1, t) : null;
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
        return js && e.locale !== "ko" ? null : e.data;
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
  function Ls(t) {
    var e = t && t.nodeName && t.nodeName.toLowerCase();
    return e === "input" ? !!gm[t.type] : e === "textarea";
  }
  function Gs(t, e, l, a) {
    qa ? Na ? Na.push(a) : Na = [a] : qa = a, e = mi(e, "onChange"), 0 < e.length && (l = new Tn(
      "onChange",
      "change",
      null,
      l,
      a
    ), t.push({ event: l, listeners: e }));
  }
  var Eu = null, Au = null;
  function pm(t) {
    Ad(t, 0);
  }
  function xn(t) {
    var e = vu(t);
    if (_s(e)) return t;
  }
  function Qs(t, e) {
    if (t === "change") return e;
  }
  var Xs = !1;
  if (el) {
    var Wi;
    if (el) {
      var Fi = "oninput" in document;
      if (!Fi) {
        var Zs = document.createElement("div");
        Zs.setAttribute("oninput", "return;"), Fi = typeof Zs.oninput == "function";
      }
      Wi = Fi;
    } else Wi = !1;
    Xs = Wi && (!document.documentMode || 9 < document.documentMode);
  }
  function Vs() {
    Eu && (Eu.detachEvent("onpropertychange", Js), Au = Eu = null);
  }
  function Js(t) {
    if (t.propertyName === "value" && xn(Au)) {
      var e = [];
      Gs(
        e,
        Au,
        t,
        Qi(t)
      ), Ns(pm, e);
    }
  }
  function bm(t, e, l) {
    t === "focusin" ? (Vs(), Eu = e, Au = l, Eu.attachEvent("onpropertychange", Js)) : t === "focusout" && Vs();
  }
  function Sm(t) {
    if (t === "selectionchange" || t === "keyup" || t === "keydown")
      return xn(Au);
  }
  function _m(t, e) {
    if (t === "click") return xn(e);
  }
  function Em(t, e) {
    if (t === "input" || t === "change")
      return xn(e);
  }
  function Am(t, e) {
    return t === e && (t !== 0 || 1 / t === 1 / e) || t !== t && e !== e;
  }
  var be = typeof Object.is == "function" ? Object.is : Am;
  function Tu(t, e) {
    if (be(t, e)) return !0;
    if (typeof t != "object" || t === null || typeof e != "object" || e === null)
      return !1;
    var l = Object.keys(t), a = Object.keys(e);
    if (l.length !== a.length) return !1;
    for (a = 0; a < l.length; a++) {
      var u = l[a];
      if (!ba.call(e, u) || !be(t[u], e[u]))
        return !1;
    }
    return !0;
  }
  function Ks(t) {
    for (; t && t.firstChild; ) t = t.firstChild;
    return t;
  }
  function ws(t, e) {
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
  function ks(t, e) {
    return t && e ? t === e ? !0 : t && t.nodeType === 3 ? !1 : e && e.nodeType === 3 ? ks(t, e.parentNode) : "contains" in t ? t.contains(e) : t.compareDocumentPosition ? !!(t.compareDocumentPosition(e) & 16) : !1 : !1;
  }
  function $s(t) {
    t = t != null && t.ownerDocument != null && t.ownerDocument.defaultView != null ? t.ownerDocument.defaultView : window;
    for (var e = bn(t.document); e instanceof t.HTMLIFrameElement; ) {
      try {
        var l = typeof e.contentWindow.location.href == "string";
      } catch {
        l = !1;
      }
      if (l) t = e.contentWindow;
      else break;
      e = bn(t.document);
    }
    return e;
  }
  function Ii(t) {
    var e = t && t.nodeName && t.nodeName.toLowerCase();
    return e && (e === "input" && (t.type === "text" || t.type === "search" || t.type === "tel" || t.type === "url" || t.type === "password") || e === "textarea" || t.contentEditable === "true");
  }
  var Tm = el && "documentMode" in document && 11 >= document.documentMode, Ma = null, Pi = null, zu = null, tc = !1;
  function Ws(t, e, l) {
    var a = l.window === l ? l.document : l.nodeType === 9 ? l : l.ownerDocument;
    tc || Ma == null || Ma !== bn(a) || (a = Ma, "selectionStart" in a && Ii(a) ? a = { start: a.selectionStart, end: a.selectionEnd } : (a = (a.ownerDocument && a.ownerDocument.defaultView || window).getSelection(), a = {
      anchorNode: a.anchorNode,
      anchorOffset: a.anchorOffset,
      focusNode: a.focusNode,
      focusOffset: a.focusOffset
    }), zu && Tu(zu, a) || (zu = a, a = mi(Pi, "onSelect"), 0 < a.length && (e = new Tn(
      "onSelect",
      "select",
      null,
      e,
      l
    ), t.push({ event: e, listeners: a }), e.target = Ma)));
  }
  function ea(t, e) {
    var l = {};
    return l[t.toLowerCase()] = e.toLowerCase(), l["Webkit" + t] = "webkit" + e, l["Moz" + t] = "moz" + e, l;
  }
  var Da = {
    animationend: ea("Animation", "AnimationEnd"),
    animationiteration: ea("Animation", "AnimationIteration"),
    animationstart: ea("Animation", "AnimationStart"),
    transitionrun: ea("Transition", "TransitionRun"),
    transitionstart: ea("Transition", "TransitionStart"),
    transitioncancel: ea("Transition", "TransitionCancel"),
    transitionend: ea("Transition", "TransitionEnd")
  }, ec = {}, Fs = {};
  el && (Fs = document.createElement("div").style, "AnimationEvent" in window || (delete Da.animationend.animation, delete Da.animationiteration.animation, delete Da.animationstart.animation), "TransitionEvent" in window || delete Da.transitionend.transition);
  function la(t) {
    if (ec[t]) return ec[t];
    if (!Da[t]) return t;
    var e = Da[t], l;
    for (l in e)
      if (e.hasOwnProperty(l) && l in Fs)
        return ec[t] = e[l];
    return t;
  }
  var Is = la("animationend"), Ps = la("animationiteration"), tr = la("animationstart"), zm = la("transitionrun"), xm = la("transitionstart"), qm = la("transitioncancel"), er = la("transitionend"), lr = /* @__PURE__ */ new Map(), lc = "abort auxClick beforeToggle cancel canPlay canPlayThrough click close contextMenu copy cut drag dragEnd dragEnter dragExit dragLeave dragOver dragStart drop durationChange emptied encrypted ended error gotPointerCapture input invalid keyDown keyPress keyUp load loadedData loadedMetadata loadStart lostPointerCapture mouseDown mouseMove mouseOut mouseOver mouseUp paste pause play playing pointerCancel pointerDown pointerMove pointerOut pointerOver pointerUp progress rateChange reset resize seeked seeking stalled submit suspend timeUpdate touchCancel touchEnd touchStart volumeChange scroll toggle touchMove waiting wheel".split(
    " "
  );
  lc.push("scrollEnd");
  function Qe(t, e) {
    lr.set(t, e), Pl(e, [t]);
  }
  var qn = typeof reportError == "function" ? reportError : function(t) {
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
  }, Me = [], Ua = 0, ac = 0;
  function Nn() {
    for (var t = Ua, e = ac = Ua = 0; e < t; ) {
      var l = Me[e];
      Me[e++] = null;
      var a = Me[e];
      Me[e++] = null;
      var u = Me[e];
      Me[e++] = null;
      var n = Me[e];
      if (Me[e++] = null, a !== null && u !== null) {
        var i = a.pending;
        i === null ? u.next = u : (u.next = i.next, i.next = u), a.pending = u;
      }
      n !== 0 && ar(l, u, n);
    }
  }
  function On(t, e, l, a) {
    Me[Ua++] = t, Me[Ua++] = e, Me[Ua++] = l, Me[Ua++] = a, ac |= a, t.lanes |= a, t = t.alternate, t !== null && (t.lanes |= a);
  }
  function uc(t, e, l, a) {
    return On(t, e, l, a), Mn(t);
  }
  function aa(t, e) {
    return On(t, null, null, e), Mn(t);
  }
  function ar(t, e, l) {
    t.lanes |= l;
    var a = t.alternate;
    a !== null && (a.lanes |= l);
    for (var u = !1, n = t.return; n !== null; )
      n.childLanes |= l, a = n.alternate, a !== null && (a.childLanes |= l), n.tag === 22 && (t = n.stateNode, t === null || t._visibility & 1 || (u = !0)), t = n, n = n.return;
    return t.tag === 3 ? (n = t.stateNode, u && e !== null && (u = 31 - pe(l), t = n.hiddenUpdates, a = t[u], a === null ? t[u] = [e] : a.push(e), e.lane = l | 536870912), n) : null;
  }
  function Mn(t) {
    if (50 < wu)
      throw wu = 0, mf = null, Error(s(185));
    for (var e = t.return; e !== null; )
      t = e, e = t.return;
    return t.tag === 3 ? t.stateNode : null;
  }
  var Ca = {};
  function Nm(t, e, l, a) {
    this.tag = t, this.key = l, this.sibling = this.child = this.return = this.stateNode = this.type = this.elementType = null, this.index = 0, this.refCleanup = this.ref = null, this.pendingProps = e, this.dependencies = this.memoizedState = this.updateQueue = this.memoizedProps = null, this.mode = a, this.subtreeFlags = this.flags = 0, this.deletions = null, this.childLanes = this.lanes = 0, this.alternate = null;
  }
  function Se(t, e, l, a) {
    return new Nm(t, e, l, a);
  }
  function nc(t) {
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
  function ur(t, e) {
    t.flags &= 65011714;
    var l = t.alternate;
    return l === null ? (t.childLanes = 0, t.lanes = e, t.child = null, t.subtreeFlags = 0, t.memoizedProps = null, t.memoizedState = null, t.updateQueue = null, t.dependencies = null, t.stateNode = null) : (t.childLanes = l.childLanes, t.lanes = l.lanes, t.child = l.child, t.subtreeFlags = 0, t.deletions = null, t.memoizedProps = l.memoizedProps, t.memoizedState = l.memoizedState, t.updateQueue = l.updateQueue, t.type = l.type, e = l.dependencies, t.dependencies = e === null ? null : {
      lanes: e.lanes,
      firstContext: e.firstContext
    }), t;
  }
  function Dn(t, e, l, a, u, n) {
    var i = 0;
    if (a = t, typeof t == "function") nc(t) && (i = 1);
    else if (typeof t == "string")
      i = Ch(
        t,
        l,
        H.current
      ) ? 26 : t === "html" || t === "head" || t === "body" ? 27 : 5;
    else
      t: switch (t) {
        case lt:
          return t = Se(31, l, e, u), t.elementType = lt, t.lanes = n, t;
        case K:
          return ua(l.children, u, n, e);
        case Y:
          i = 8, u |= 24;
          break;
        case w:
          return t = Se(12, l, e, u | 2), t.elementType = w, t.lanes = n, t;
        case Ut:
          return t = Se(13, l, e, u), t.elementType = Ut, t.lanes = n, t;
        case Ct:
          return t = Se(19, l, e, u), t.elementType = Ct, t.lanes = n, t;
        default:
          if (typeof t == "object" && t !== null)
            switch (t.$$typeof) {
              case et:
                i = 10;
                break t;
              case ct:
                i = 9;
                break t;
              case tt:
                i = 11;
                break t;
              case Z:
                i = 14;
                break t;
              case ee:
                i = 16, a = null;
                break t;
            }
          i = 29, l = Error(
            s(130, t === null ? "null" : typeof t, "")
          ), a = null;
      }
    return e = Se(i, l, e, u), e.elementType = t, e.type = a, e.lanes = n, e;
  }
  function ua(t, e, l, a) {
    return t = Se(7, t, a, e), t.lanes = l, t;
  }
  function ic(t, e, l) {
    return t = Se(6, t, null, e), t.lanes = l, t;
  }
  function nr(t) {
    var e = Se(18, null, null, 0);
    return e.stateNode = t, e;
  }
  function cc(t, e, l) {
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
  var ir = /* @__PURE__ */ new WeakMap();
  function De(t, e) {
    if (typeof t == "object" && t !== null) {
      var l = ir.get(t);
      return l !== void 0 ? l : (e = {
        value: t,
        source: e,
        stack: on(e)
      }, ir.set(t, e), e);
    }
    return {
      value: t,
      source: e,
      stack: on(e)
    };
  }
  var ja = [], Ra = 0, Un = null, xu = 0, Ue = [], Ce = 0, Tl = null, Ke = 1, we = "";
  function al(t, e) {
    ja[Ra++] = xu, ja[Ra++] = Un, Un = t, xu = e;
  }
  function cr(t, e, l) {
    Ue[Ce++] = Ke, Ue[Ce++] = we, Ue[Ce++] = Tl, Tl = t;
    var a = Ke;
    t = we;
    var u = 32 - pe(a) - 1;
    a &= ~(1 << u), l += 1;
    var n = 32 - pe(e) + u;
    if (30 < n) {
      var i = u - u % 5;
      n = (a & (1 << i) - 1).toString(32), a >>= i, u -= i, Ke = 1 << 32 - pe(e) + u | l << u | a, we = n + t;
    } else
      Ke = 1 << n | l << u | a, we = t;
  }
  function fc(t) {
    t.return !== null && (al(t, 1), cr(t, 1, 0));
  }
  function sc(t) {
    for (; t === Un; )
      Un = ja[--Ra], ja[Ra] = null, xu = ja[--Ra], ja[Ra] = null;
    for (; t === Tl; )
      Tl = Ue[--Ce], Ue[Ce] = null, we = Ue[--Ce], Ue[Ce] = null, Ke = Ue[--Ce], Ue[Ce] = null;
  }
  function fr(t, e) {
    Ue[Ce++] = Ke, Ue[Ce++] = we, Ue[Ce++] = Tl, Ke = e.id, we = e.overflow, Tl = t;
  }
  var Ft = null, Nt = null, yt = !1, zl = null, je = !1, rc = Error(s(519));
  function xl(t) {
    var e = Error(
      s(
        418,
        1 < arguments.length && arguments[1] !== void 0 && arguments[1] ? "text" : "HTML",
        ""
      )
    );
    throw qu(De(e, t)), rc;
  }
  function sr(t) {
    var e = t.stateNode, l = t.type, a = t.memoizedProps;
    switch (e[Wt] = t, e[ce] = a, l) {
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
        for (l = 0; l < $u.length; l++)
          st($u[l], e);
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
        st("invalid", e), Es(
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
        st("invalid", e);
        break;
      case "textarea":
        st("invalid", e), Ts(e, a.value, a.defaultValue, a.children);
    }
    l = a.children, typeof l != "string" && typeof l != "number" && typeof l != "bigint" || e.textContent === "" + l || a.suppressHydrationWarning === !0 || qd(e.textContent, l) ? (a.popover != null && (st("beforetoggle", e), st("toggle", e)), a.onScroll != null && st("scroll", e), a.onScrollEnd != null && st("scrollend", e), a.onClick != null && (e.onclick = tl), e = !0) : e = !1, e || xl(t, !0);
  }
  function rr(t) {
    for (Ft = t.return; Ft; )
      switch (Ft.tag) {
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
          Ft = Ft.return;
      }
  }
  function Ha(t) {
    if (t !== Ft) return !1;
    if (!yt) return rr(t), yt = !0, !1;
    var e = t.tag, l;
    if ((l = e !== 3 && e !== 27) && ((l = e === 5) && (l = t.type, l = !(l !== "form" && l !== "button") || Of(t.type, t.memoizedProps)), l = !l), l && Nt && xl(t), rr(t), e === 13) {
      if (t = t.memoizedState, t = t !== null ? t.dehydrated : null, !t) throw Error(s(317));
      Nt = Hd(t);
    } else if (e === 31) {
      if (t = t.memoizedState, t = t !== null ? t.dehydrated : null, !t) throw Error(s(317));
      Nt = Hd(t);
    } else
      e === 27 ? (e = Nt, Gl(t.type) ? (t = jf, jf = null, Nt = t) : Nt = e) : Nt = Ft ? He(t.stateNode.nextSibling) : null;
    return !0;
  }
  function na() {
    Nt = Ft = null, yt = !1;
  }
  function oc() {
    var t = zl;
    return t !== null && (de === null ? de = t : de.push.apply(
      de,
      t
    ), zl = null), t;
  }
  function qu(t) {
    zl === null ? zl = [t] : zl.push(t);
  }
  var dc = m(null), ia = null, ul = null;
  function ql(t, e, l) {
    R(dc, e._currentValue), e._currentValue = l;
  }
  function nl(t) {
    t._currentValue = dc.current, U(dc);
  }
  function yc(t, e, l) {
    for (; t !== null; ) {
      var a = t.alternate;
      if ((t.childLanes & e) !== e ? (t.childLanes |= e, a !== null && (a.childLanes |= e)) : a !== null && (a.childLanes & e) !== e && (a.childLanes |= e), t === l) break;
      t = t.return;
    }
  }
  function mc(t, e, l, a) {
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
              n.lanes |= l, o = n.alternate, o !== null && (o.lanes |= l), yc(
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
        i.lanes |= l, n = i.alternate, n !== null && (n.lanes |= l), yc(i, l, t), i = null;
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
  function Ba(t, e, l, a) {
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
          be(u.pendingProps.value, i.value) || (t !== null ? t.push(o) : t = [o]);
        }
      } else if (u === dt.current) {
        if (i = u.alternate, i === null) throw Error(s(387));
        i.memoizedState.memoizedState !== u.memoizedState.memoizedState && (t !== null ? t.push(tn) : t = [tn]);
      }
      u = u.return;
    }
    t !== null && mc(
      e,
      t,
      l,
      a
    ), e.flags |= 262144;
  }
  function Cn(t) {
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
  function ca(t) {
    ia = t, ul = null, t = t.dependencies, t !== null && (t.firstContext = null);
  }
  function It(t) {
    return or(ia, t);
  }
  function jn(t, e) {
    return ia === null && ca(t), or(t, e);
  }
  function or(t, e) {
    var l = e._currentValue;
    if (e = { context: e, memoizedValue: l, next: null }, ul === null) {
      if (t === null) throw Error(s(308));
      ul = e, t.dependencies = { lanes: 0, firstContext: e }, t.flags |= 524288;
    } else ul = ul.next = e;
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
  }, Mm = c.unstable_scheduleCallback, Dm = c.unstable_NormalPriority, Yt = {
    $$typeof: et,
    Consumer: null,
    Provider: null,
    _currentValue: null,
    _currentValue2: null,
    _threadCount: 0
  };
  function hc() {
    return {
      controller: new Om(),
      data: /* @__PURE__ */ new Map(),
      refCount: 0
    };
  }
  function Nu(t) {
    t.refCount--, t.refCount === 0 && Mm(Dm, function() {
      t.controller.abort();
    });
  }
  var Ou = null, vc = 0, Ya = 0, La = null;
  function Um(t, e) {
    if (Ou === null) {
      var l = Ou = [];
      vc = 0, Ya = Sf(), La = {
        status: "pending",
        value: void 0,
        then: function(a) {
          l.push(a);
        }
      };
    }
    return vc++, e.then(dr, dr), e;
  }
  function dr() {
    if (--vc === 0 && Ou !== null) {
      La !== null && (La.status = "fulfilled");
      var t = Ou;
      Ou = null, Ya = 0, La = null;
      for (var e = 0; e < t.length; e++) (0, t[e])();
    }
  }
  function Cm(t, e) {
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
  var yr = z.S;
  z.S = function(t, e) {
    Fo = ht(), typeof e == "object" && e !== null && typeof e.then == "function" && Um(t, e), yr !== null && yr(t, e);
  };
  var fa = m(null);
  function gc() {
    var t = fa.current;
    return t !== null ? t : qt.pooledCache;
  }
  function Rn(t, e) {
    e === null ? R(fa, fa.current) : R(fa, e.pool);
  }
  function mr() {
    var t = gc();
    return t === null ? null : { parent: Yt._currentValue, pool: t };
  }
  var Ga = Error(s(460)), pc = Error(s(474)), Hn = Error(s(542)), Bn = { then: function() {
  } };
  function hr(t) {
    return t = t.status, t === "fulfilled" || t === "rejected";
  }
  function vr(t, e, l) {
    switch (l = t[l], l === void 0 ? t.push(e) : l !== e && (e.then(tl, tl), e = l), e.status) {
      case "fulfilled":
        return e.value;
      case "rejected":
        throw t = e.reason, pr(t), t;
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
            throw t = e.reason, pr(t), t;
        }
        throw ra = e, Ga;
    }
  }
  function sa(t) {
    try {
      var e = t._init;
      return e(t._payload);
    } catch (l) {
      throw l !== null && typeof l == "object" && typeof l.then == "function" ? (ra = l, Ga) : l;
    }
  }
  var ra = null;
  function gr() {
    if (ra === null) throw Error(s(459));
    var t = ra;
    return ra = null, t;
  }
  function pr(t) {
    if (t === Ga || t === Hn)
      throw Error(s(483));
  }
  var Qa = null, Mu = 0;
  function Yn(t) {
    var e = Mu;
    return Mu += 1, Qa === null && (Qa = []), vr(Qa, t, e);
  }
  function Du(t, e) {
    e = e.props.ref, t.ref = e !== void 0 ? e : null;
  }
  function Ln(t, e) {
    throw e.$$typeof === C ? Error(s(525)) : (t = Object.prototype.toString.call(e), Error(
      s(
        31,
        t === "[object Object]" ? "object with keys {" + Object.keys(e).join(", ") + "}" : t
      )
    ));
  }
  function br(t) {
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
    function o(v, y, b, O) {
      return y === null || y.tag !== 6 ? (y = ic(b, v.mode, O), y.return = v, y) : (y = u(y, b), y.return = v, y);
    }
    function d(v, y, b, O) {
      var V = b.type;
      return V === K ? x(
        v,
        y,
        b.props.children,
        O,
        b.key
      ) : y !== null && (y.elementType === V || typeof V == "object" && V !== null && V.$$typeof === ee && sa(V) === y.type) ? (y = u(y, b.props), Du(y, b), y.return = v, y) : (y = Dn(
        b.type,
        b.key,
        b.props,
        null,
        v.mode,
        O
      ), Du(y, b), y.return = v, y);
    }
    function S(v, y, b, O) {
      return y === null || y.tag !== 4 || y.stateNode.containerInfo !== b.containerInfo || y.stateNode.implementation !== b.implementation ? (y = cc(b, v.mode, O), y.return = v, y) : (y = u(y, b.children || []), y.return = v, y);
    }
    function x(v, y, b, O, V) {
      return y === null || y.tag !== 7 ? (y = ua(
        b,
        v.mode,
        O,
        V
      ), y.return = v, y) : (y = u(y, b), y.return = v, y);
    }
    function M(v, y, b) {
      if (typeof y == "string" && y !== "" || typeof y == "number" || typeof y == "bigint")
        return y = ic(
          "" + y,
          v.mode,
          b
        ), y.return = v, y;
      if (typeof y == "object" && y !== null) {
        switch (y.$$typeof) {
          case G:
            return b = Dn(
              y.type,
              y.key,
              y.props,
              null,
              v.mode,
              b
            ), Du(b, y), b.return = v, b;
          case J:
            return y = cc(
              y,
              v.mode,
              b
            ), y.return = v, y;
          case ee:
            return y = sa(y), M(v, y, b);
        }
        if (ve(y) || kt(y))
          return y = ua(
            y,
            v.mode,
            b,
            null
          ), y.return = v, y;
        if (typeof y.then == "function")
          return M(v, Yn(y), b);
        if (y.$$typeof === et)
          return M(
            v,
            jn(v, y),
            b
          );
        Ln(v, y);
      }
      return null;
    }
    function _(v, y, b, O) {
      var V = y !== null ? y.key : null;
      if (typeof b == "string" && b !== "" || typeof b == "number" || typeof b == "bigint")
        return V !== null ? null : o(v, y, "" + b, O);
      if (typeof b == "object" && b !== null) {
        switch (b.$$typeof) {
          case G:
            return b.key === V ? d(v, y, b, O) : null;
          case J:
            return b.key === V ? S(v, y, b, O) : null;
          case ee:
            return b = sa(b), _(v, y, b, O);
        }
        if (ve(b) || kt(b))
          return V !== null ? null : x(v, y, b, O, null);
        if (typeof b.then == "function")
          return _(
            v,
            y,
            Yn(b),
            O
          );
        if (b.$$typeof === et)
          return _(
            v,
            y,
            jn(v, b),
            O
          );
        Ln(v, b);
      }
      return null;
    }
    function A(v, y, b, O, V) {
      if (typeof O == "string" && O !== "" || typeof O == "number" || typeof O == "bigint")
        return v = v.get(b) || null, o(y, v, "" + O, V);
      if (typeof O == "object" && O !== null) {
        switch (O.$$typeof) {
          case G:
            return v = v.get(
              O.key === null ? b : O.key
            ) || null, d(y, v, O, V);
          case J:
            return v = v.get(
              O.key === null ? b : O.key
            ) || null, S(y, v, O, V);
          case ee:
            return O = sa(O), A(
              v,
              y,
              b,
              O,
              V
            );
        }
        if (ve(O) || kt(O))
          return v = v.get(b) || null, x(y, v, O, V, null);
        if (typeof O.then == "function")
          return A(
            v,
            y,
            b,
            Yn(O),
            V
          );
        if (O.$$typeof === et)
          return A(
            v,
            y,
            b,
            jn(y, O),
            V
          );
        Ln(y, O);
      }
      return null;
    }
    function L(v, y, b, O) {
      for (var V = null, vt = null, Q = y, nt = y = 0, ot = null; Q !== null && nt < b.length; nt++) {
        Q.index > nt ? (ot = Q, Q = null) : ot = Q.sibling;
        var gt = _(
          v,
          Q,
          b[nt],
          O
        );
        if (gt === null) {
          Q === null && (Q = ot);
          break;
        }
        t && Q && gt.alternate === null && e(v, Q), y = n(gt, y, nt), vt === null ? V = gt : vt.sibling = gt, vt = gt, Q = ot;
      }
      if (nt === b.length)
        return l(v, Q), yt && al(v, nt), V;
      if (Q === null) {
        for (; nt < b.length; nt++)
          Q = M(v, b[nt], O), Q !== null && (y = n(
            Q,
            y,
            nt
          ), vt === null ? V = Q : vt.sibling = Q, vt = Q);
        return yt && al(v, nt), V;
      }
      for (Q = a(Q); nt < b.length; nt++)
        ot = A(
          Q,
          v,
          nt,
          b[nt],
          O
        ), ot !== null && (t && ot.alternate !== null && Q.delete(
          ot.key === null ? nt : ot.key
        ), y = n(
          ot,
          y,
          nt
        ), vt === null ? V = ot : vt.sibling = ot, vt = ot);
      return t && Q.forEach(function(Jl) {
        return e(v, Jl);
      }), yt && al(v, nt), V;
    }
    function $(v, y, b, O) {
      if (b == null) throw Error(s(151));
      for (var V = null, vt = null, Q = y, nt = y = 0, ot = null, gt = b.next(); Q !== null && !gt.done; nt++, gt = b.next()) {
        Q.index > nt ? (ot = Q, Q = null) : ot = Q.sibling;
        var Jl = _(v, Q, gt.value, O);
        if (Jl === null) {
          Q === null && (Q = ot);
          break;
        }
        t && Q && Jl.alternate === null && e(v, Q), y = n(Jl, y, nt), vt === null ? V = Jl : vt.sibling = Jl, vt = Jl, Q = ot;
      }
      if (gt.done)
        return l(v, Q), yt && al(v, nt), V;
      if (Q === null) {
        for (; !gt.done; nt++, gt = b.next())
          gt = M(v, gt.value, O), gt !== null && (y = n(gt, y, nt), vt === null ? V = gt : vt.sibling = gt, vt = gt);
        return yt && al(v, nt), V;
      }
      for (Q = a(Q); !gt.done; nt++, gt = b.next())
        gt = A(Q, v, nt, gt.value, O), gt !== null && (t && gt.alternate !== null && Q.delete(gt.key === null ? nt : gt.key), y = n(gt, y, nt), vt === null ? V = gt : vt.sibling = gt, vt = gt);
      return t && Q.forEach(function(Vh) {
        return e(v, Vh);
      }), yt && al(v, nt), V;
    }
    function xt(v, y, b, O) {
      if (typeof b == "object" && b !== null && b.type === K && b.key === null && (b = b.props.children), typeof b == "object" && b !== null) {
        switch (b.$$typeof) {
          case G:
            t: {
              for (var V = b.key; y !== null; ) {
                if (y.key === V) {
                  if (V = b.type, V === K) {
                    if (y.tag === 7) {
                      l(
                        v,
                        y.sibling
                      ), O = u(
                        y,
                        b.props.children
                      ), O.return = v, v = O;
                      break t;
                    }
                  } else if (y.elementType === V || typeof V == "object" && V !== null && V.$$typeof === ee && sa(V) === y.type) {
                    l(
                      v,
                      y.sibling
                    ), O = u(y, b.props), Du(O, b), O.return = v, v = O;
                    break t;
                  }
                  l(v, y);
                  break;
                } else e(v, y);
                y = y.sibling;
              }
              b.type === K ? (O = ua(
                b.props.children,
                v.mode,
                O,
                b.key
              ), O.return = v, v = O) : (O = Dn(
                b.type,
                b.key,
                b.props,
                null,
                v.mode,
                O
              ), Du(O, b), O.return = v, v = O);
            }
            return i(v);
          case J:
            t: {
              for (V = b.key; y !== null; ) {
                if (y.key === V)
                  if (y.tag === 4 && y.stateNode.containerInfo === b.containerInfo && y.stateNode.implementation === b.implementation) {
                    l(
                      v,
                      y.sibling
                    ), O = u(y, b.children || []), O.return = v, v = O;
                    break t;
                  } else {
                    l(v, y);
                    break;
                  }
                else e(v, y);
                y = y.sibling;
              }
              O = cc(b, v.mode, O), O.return = v, v = O;
            }
            return i(v);
          case ee:
            return b = sa(b), xt(
              v,
              y,
              b,
              O
            );
        }
        if (ve(b))
          return L(
            v,
            y,
            b,
            O
          );
        if (kt(b)) {
          if (V = kt(b), typeof V != "function") throw Error(s(150));
          return b = V.call(b), $(
            v,
            y,
            b,
            O
          );
        }
        if (typeof b.then == "function")
          return xt(
            v,
            y,
            Yn(b),
            O
          );
        if (b.$$typeof === et)
          return xt(
            v,
            y,
            jn(v, b),
            O
          );
        Ln(v, b);
      }
      return typeof b == "string" && b !== "" || typeof b == "number" || typeof b == "bigint" ? (b = "" + b, y !== null && y.tag === 6 ? (l(v, y.sibling), O = u(y, b), O.return = v, v = O) : (l(v, y), O = ic(b, v.mode, O), O.return = v, v = O), i(v)) : l(v, y);
    }
    return function(v, y, b, O) {
      try {
        Mu = 0;
        var V = xt(
          v,
          y,
          b,
          O
        );
        return Qa = null, V;
      } catch (Q) {
        if (Q === Ga || Q === Hn) throw Q;
        var vt = Se(29, Q, null, v.mode);
        return vt.lanes = O, vt.return = v, vt;
      } finally {
      }
    };
  }
  var oa = br(!0), Sr = br(!1), Nl = !1;
  function bc(t) {
    t.updateQueue = {
      baseState: t.memoizedState,
      firstBaseUpdate: null,
      lastBaseUpdate: null,
      shared: { pending: null, lanes: 0, hiddenCallbacks: null },
      callbacks: null
    };
  }
  function Sc(t, e) {
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
    if (a = a.shared, (bt & 2) !== 0) {
      var u = a.pending;
      return u === null ? e.next = e : (e.next = u.next, u.next = e), a.pending = e, e = Mn(t), ar(t, null, l), e;
    }
    return On(t, a, e, l), Mn(t);
  }
  function Uu(t, e, l) {
    if (e = e.updateQueue, e !== null && (e = e.shared, (l & 4194048) !== 0)) {
      var a = e.lanes;
      a &= t.pendingLanes, l |= a, e.lanes = l, os(t, l);
    }
  }
  function _c(t, e) {
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
  var Ec = !1;
  function Cu() {
    if (Ec) {
      var t = La;
      if (t !== null) throw t;
    }
  }
  function ju(t, e, l, a) {
    Ec = !1;
    var u = t.updateQueue;
    Nl = !1;
    var n = u.firstBaseUpdate, i = u.lastBaseUpdate, o = u.shared.pending;
    if (o !== null) {
      u.shared.pending = null;
      var d = o, S = d.next;
      d.next = null, i === null ? n = S : i.next = S, i = d;
      var x = t.alternate;
      x !== null && (x = x.updateQueue, o = x.lastBaseUpdate, o !== i && (o === null ? x.firstBaseUpdate = S : o.next = S, x.lastBaseUpdate = d));
    }
    if (n !== null) {
      var M = u.baseState;
      i = 0, x = S = d = null, o = n;
      do {
        var _ = o.lane & -536870913, A = _ !== o.lane;
        if (A ? (rt & _) === _ : (a & _) === _) {
          _ !== 0 && _ === Ya && (Ec = !0), x !== null && (x = x.next = {
            lane: 0,
            tag: o.tag,
            payload: o.payload,
            callback: null,
            next: null
          });
          t: {
            var L = t, $ = o;
            _ = e;
            var xt = l;
            switch ($.tag) {
              case 1:
                if (L = $.payload, typeof L == "function") {
                  M = L.call(xt, M, _);
                  break t;
                }
                M = L;
                break t;
              case 3:
                L.flags = L.flags & -65537 | 128;
              case 0:
                if (L = $.payload, _ = typeof L == "function" ? L.call(xt, M, _) : L, _ == null) break t;
                M = E({}, M, _);
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
          }, x === null ? (S = x = A, d = M) : x = x.next = A, i |= _;
        if (o = o.next, o === null) {
          if (o = u.shared.pending, o === null)
            break;
          A = o, o = A.next, A.next = null, u.lastBaseUpdate = A, u.shared.pending = null;
        }
      } while (!0);
      x === null && (d = M), u.baseState = d, u.firstBaseUpdate = S, u.lastBaseUpdate = x, n === null && (u.shared.lanes = 0), Rl |= i, t.lanes = i, t.memoizedState = M;
    }
  }
  function _r(t, e) {
    if (typeof t != "function")
      throw Error(s(191, t));
    t.call(e);
  }
  function Er(t, e) {
    var l = t.callbacks;
    if (l !== null)
      for (t.callbacks = null, t = 0; t < l.length; t++)
        _r(l[t], e);
  }
  var Xa = m(null), Gn = m(0);
  function Ar(t, e) {
    t = ml, R(Gn, t), R(Xa, e), ml = t | e.baseLanes;
  }
  function Ac() {
    R(Gn, ml), R(Xa, Xa.current);
  }
  function Tc() {
    ml = Gn.current, U(Xa), U(Gn);
  }
  var _e = m(null), Re = null;
  function Dl(t) {
    var e = t.alternate;
    R(Ht, Ht.current & 1), R(_e, t), Re === null && (e === null || Xa.current !== null || e.memoizedState !== null) && (Re = t);
  }
  function zc(t) {
    R(Ht, Ht.current), R(_e, t), Re === null && (Re = t);
  }
  function Tr(t) {
    t.tag === 22 ? (R(Ht, Ht.current), R(_e, t), Re === null && (Re = t)) : Ul();
  }
  function Ul() {
    R(Ht, Ht.current), R(_e, _e.current);
  }
  function Ee(t) {
    U(_e), Re === t && (Re = null), U(Ht);
  }
  var Ht = m(0);
  function Qn(t) {
    for (var e = t; e !== null; ) {
      if (e.tag === 13) {
        var l = e.memoizedState;
        if (l !== null && (l = l.dehydrated, l === null || Uf(l) || Cf(l)))
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
  var il = 0, ut = null, Tt = null, Lt = null, Xn = !1, Za = !1, da = !1, Zn = 0, Ru = 0, Va = null, jm = 0;
  function jt() {
    throw Error(s(321));
  }
  function xc(t, e) {
    if (e === null) return !1;
    for (var l = 0; l < e.length && l < t.length; l++)
      if (!be(t[l], e[l])) return !1;
    return !0;
  }
  function qc(t, e, l, a, u, n) {
    return il = n, ut = e, e.memoizedState = null, e.updateQueue = null, e.lanes = 0, z.H = t === null || t.memoizedState === null ? co : Xc, da = !1, n = l(a, u), da = !1, Za && (n = xr(
      e,
      l,
      a,
      u
    )), zr(t), n;
  }
  function zr(t) {
    z.H = Yu;
    var e = Tt !== null && Tt.next !== null;
    if (il = 0, Lt = Tt = ut = null, Xn = !1, Ru = 0, Va = null, e) throw Error(s(300));
    t === null || Gt || (t = t.dependencies, t !== null && Cn(t) && (Gt = !0));
  }
  function xr(t, e, l, a) {
    ut = t;
    var u = 0;
    do {
      if (Za && (Va = null), Ru = 0, Za = !1, 25 <= u) throw Error(s(301));
      if (u += 1, Lt = Tt = null, t.updateQueue != null) {
        var n = t.updateQueue;
        n.lastEffect = null, n.events = null, n.stores = null, n.memoCache != null && (n.memoCache.index = 0);
      }
      z.H = fo, n = e(l, a);
    } while (Za);
    return n;
  }
  function Rm() {
    var t = z.H, e = t.useState()[0];
    return e = typeof e.then == "function" ? Hu(e) : e, t = t.useState()[0], (Tt !== null ? Tt.memoizedState : null) !== t && (ut.flags |= 1024), e;
  }
  function Nc() {
    var t = Zn !== 0;
    return Zn = 0, t;
  }
  function Oc(t, e, l) {
    e.updateQueue = t.updateQueue, e.flags &= -2053, t.lanes &= ~l;
  }
  function Mc(t) {
    if (Xn) {
      for (t = t.memoizedState; t !== null; ) {
        var e = t.queue;
        e !== null && (e.pending = null), t = t.next;
      }
      Xn = !1;
    }
    il = 0, Lt = Tt = ut = null, Za = !1, Ru = Zn = 0, Va = null;
  }
  function ie() {
    var t = {
      memoizedState: null,
      baseState: null,
      baseQueue: null,
      queue: null,
      next: null
    };
    return Lt === null ? ut.memoizedState = Lt = t : Lt = Lt.next = t, Lt;
  }
  function Bt() {
    if (Tt === null) {
      var t = ut.alternate;
      t = t !== null ? t.memoizedState : null;
    } else t = Tt.next;
    var e = Lt === null ? ut.memoizedState : Lt.next;
    if (e !== null)
      Lt = e, Tt = t;
    else {
      if (t === null)
        throw ut.alternate === null ? Error(s(467)) : Error(s(310));
      Tt = t, t = {
        memoizedState: Tt.memoizedState,
        baseState: Tt.baseState,
        baseQueue: Tt.baseQueue,
        queue: Tt.queue,
        next: null
      }, Lt === null ? ut.memoizedState = Lt = t : Lt = Lt.next = t;
    }
    return Lt;
  }
  function Vn() {
    return { lastEffect: null, events: null, stores: null, memoCache: null };
  }
  function Hu(t) {
    var e = Ru;
    return Ru += 1, Va === null && (Va = []), t = vr(Va, t, e), e = ut, (Lt === null ? e.memoizedState : Lt.next) === null && (e = e.alternate, z.H = e === null || e.memoizedState === null ? co : Xc), t;
  }
  function Jn(t) {
    if (t !== null && typeof t == "object") {
      if (typeof t.then == "function") return Hu(t);
      if (t.$$typeof === et) return It(t);
    }
    throw Error(s(438, String(t)));
  }
  function Dc(t) {
    var e = null, l = ut.updateQueue;
    if (l !== null && (e = l.memoCache), e == null) {
      var a = ut.alternate;
      a !== null && (a = a.updateQueue, a !== null && (a = a.memoCache, a != null && (e = {
        data: a.data.map(function(u) {
          return u.slice();
        }),
        index: 0
      })));
    }
    if (e == null && (e = { data: [], index: 0 }), l === null && (l = Vn(), ut.updateQueue = l), l.memoCache = e, l = e.data[e.index], l === void 0)
      for (l = e.data[e.index] = Array(t), a = 0; a < t; a++)
        l[a] = Le;
    return e.index++, l;
  }
  function cl(t, e) {
    return typeof e == "function" ? e(t) : e;
  }
  function Kn(t) {
    var e = Bt();
    return Uc(e, Tt, t);
  }
  function Uc(t, e, l) {
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
      var o = i = null, d = null, S = e, x = !1;
      do {
        var M = S.lane & -536870913;
        if (M !== S.lane ? (rt & M) === M : (il & M) === M) {
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
            }), M === Ya && (x = !0);
          else if ((il & _) === _) {
            S = S.next, _ === Ya && (x = !0);
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
            }, d === null ? (o = d = M, i = n) : d = d.next = M, ut.lanes |= _, Rl |= _;
          M = S.action, da && l(n, M), n = S.hasEagerState ? S.eagerState : l(n, M);
        } else
          _ = {
            lane: M,
            revertLane: S.revertLane,
            gesture: S.gesture,
            action: S.action,
            hasEagerState: S.hasEagerState,
            eagerState: S.eagerState,
            next: null
          }, d === null ? (o = d = _, i = n) : d = d.next = _, ut.lanes |= M, Rl |= M;
        S = S.next;
      } while (S !== null && S !== e);
      if (d === null ? i = n : d.next = o, !be(n, t.memoizedState) && (Gt = !0, x && (l = La, l !== null)))
        throw l;
      t.memoizedState = n, t.baseState = i, t.baseQueue = d, a.lastRenderedState = n;
    }
    return u === null && (a.lanes = 0), [t.memoizedState, a.dispatch];
  }
  function Cc(t) {
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
      be(n, e.memoizedState) || (Gt = !0), e.memoizedState = n, e.baseQueue === null && (e.baseState = n), l.lastRenderedState = n;
    }
    return [n, a];
  }
  function qr(t, e, l) {
    var a = ut, u = Bt(), n = yt;
    if (n) {
      if (l === void 0) throw Error(s(407));
      l = l();
    } else l = e();
    var i = !be(
      (Tt || u).memoizedState,
      l
    );
    if (i && (u.memoizedState = l, Gt = !0), u = u.queue, Hc(Mr.bind(null, a, u, t), [
      t
    ]), u.getSnapshot !== e || i || Lt !== null && Lt.memoizedState.tag & 1) {
      if (a.flags |= 2048, Ja(
        9,
        { destroy: void 0 },
        Or.bind(
          null,
          a,
          u,
          l,
          e
        ),
        null
      ), qt === null) throw Error(s(349));
      n || (il & 127) !== 0 || Nr(a, e, l);
    }
    return l;
  }
  function Nr(t, e, l) {
    t.flags |= 16384, t = { getSnapshot: e, value: l }, e = ut.updateQueue, e === null ? (e = Vn(), ut.updateQueue = e, e.stores = [t]) : (l = e.stores, l === null ? e.stores = [t] : l.push(t));
  }
  function Or(t, e, l, a) {
    e.value = l, e.getSnapshot = a, Dr(e) && Ur(t);
  }
  function Mr(t, e, l) {
    return l(function() {
      Dr(e) && Ur(t);
    });
  }
  function Dr(t) {
    var e = t.getSnapshot;
    t = t.value;
    try {
      var l = e();
      return !be(t, l);
    } catch {
      return !0;
    }
  }
  function Ur(t) {
    var e = aa(t, 2);
    e !== null && ye(e, t, 2);
  }
  function jc(t) {
    var e = ie();
    if (typeof t == "function") {
      var l = t;
      if (t = l(), da) {
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
  function Cr(t, e, l, a) {
    return t.baseState = l, Uc(
      t,
      Tt,
      typeof a == "function" ? a : cl
    );
  }
  function Hm(t, e, l, a, u) {
    if ($n(t)) throw Error(s(485));
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
      z.T !== null ? l(!0) : n.isTransition = !1, a(n), l = e.pending, l === null ? (n.next = e.pending = n, jr(e, n)) : (n.next = l.next, e.pending = l.next = n);
    }
  }
  function jr(t, e) {
    var l = e.action, a = e.payload, u = t.state;
    if (e.isTransition) {
      var n = z.T, i = {};
      z.T = i;
      try {
        var o = l(u, a), d = z.S;
        d !== null && d(i, o), Rr(t, e, o);
      } catch (S) {
        Rc(t, e, S);
      } finally {
        n !== null && i.types !== null && (n.types = i.types), z.T = n;
      }
    } else
      try {
        n = l(u, a), Rr(t, e, n);
      } catch (S) {
        Rc(t, e, S);
      }
  }
  function Rr(t, e, l) {
    l !== null && typeof l == "object" && typeof l.then == "function" ? l.then(
      function(a) {
        Hr(t, e, a);
      },
      function(a) {
        return Rc(t, e, a);
      }
    ) : Hr(t, e, l);
  }
  function Hr(t, e, l) {
    e.status = "fulfilled", e.value = l, Br(e), t.state = l, e = t.pending, e !== null && (l = e.next, l === e ? t.pending = null : (l = l.next, e.next = l, jr(t, l)));
  }
  function Rc(t, e, l) {
    var a = t.pending;
    if (t.pending = null, a !== null) {
      a = a.next;
      do
        e.status = "rejected", e.reason = l, Br(e), e = e.next;
      while (e !== a);
    }
    t.action = null;
  }
  function Br(t) {
    t = t.listeners;
    for (var e = 0; e < t.length; e++) (0, t[e])();
  }
  function Yr(t, e) {
    return e;
  }
  function Lr(t, e) {
    if (yt) {
      var l = qt.formState;
      if (l !== null) {
        t: {
          var a = ut;
          if (yt) {
            if (Nt) {
              e: {
                for (var u = Nt, n = je; u.nodeType !== 8; ) {
                  if (!n) {
                    u = null;
                    break e;
                  }
                  if (u = He(
                    u.nextSibling
                  ), u === null) {
                    u = null;
                    break e;
                  }
                }
                n = u.data, u = n === "F!" || n === "F" ? u : null;
              }
              if (u) {
                Nt = He(
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
    return l = ie(), l.memoizedState = l.baseState = e, a = {
      pending: null,
      lanes: 0,
      dispatch: null,
      lastRenderedReducer: Yr,
      lastRenderedState: e
    }, l.queue = a, l = uo.bind(
      null,
      ut,
      a
    ), a.dispatch = l, a = jc(!1), n = Qc.bind(
      null,
      ut,
      !1,
      a.queue
    ), a = ie(), u = {
      state: e,
      dispatch: null,
      action: t,
      pending: null
    }, a.queue = u, l = Hm.bind(
      null,
      ut,
      u,
      n,
      l
    ), u.dispatch = l, a.memoizedState = t, [e, l, !1];
  }
  function Gr(t) {
    var e = Bt();
    return Qr(e, Tt, t);
  }
  function Qr(t, e, l) {
    if (e = Uc(
      t,
      e,
      Yr
    )[0], t = Kn(cl)[0], typeof e == "object" && e !== null && typeof e.then == "function")
      try {
        var a = Hu(e);
      } catch (i) {
        throw i === Ga ? Hn : i;
      }
    else a = e;
    e = Bt();
    var u = e.queue, n = u.dispatch;
    return l !== e.memoizedState && (ut.flags |= 2048, Ja(
      9,
      { destroy: void 0 },
      Bm.bind(null, u, l),
      null
    )), [a, n, t];
  }
  function Bm(t, e) {
    t.action = e;
  }
  function Xr(t) {
    var e = Bt(), l = Tt;
    if (l !== null)
      return Qr(e, l, t);
    Bt(), e = e.memoizedState, l = Bt();
    var a = l.queue.dispatch;
    return l.memoizedState = t, [e, a, !1];
  }
  function Ja(t, e, l, a) {
    return t = { tag: t, create: l, deps: a, inst: e, next: null }, e = ut.updateQueue, e === null && (e = Vn(), ut.updateQueue = e), l = e.lastEffect, l === null ? e.lastEffect = t.next = t : (a = l.next, l.next = t, t.next = a, e.lastEffect = t), t;
  }
  function Zr() {
    return Bt().memoizedState;
  }
  function wn(t, e, l, a) {
    var u = ie();
    ut.flags |= t, u.memoizedState = Ja(
      1 | e,
      { destroy: void 0 },
      l,
      a === void 0 ? null : a
    );
  }
  function kn(t, e, l, a) {
    var u = Bt();
    a = a === void 0 ? null : a;
    var n = u.memoizedState.inst;
    Tt !== null && a !== null && xc(a, Tt.memoizedState.deps) ? u.memoizedState = Ja(e, n, l, a) : (ut.flags |= t, u.memoizedState = Ja(
      1 | e,
      n,
      l,
      a
    ));
  }
  function Vr(t, e) {
    wn(8390656, 8, t, e);
  }
  function Hc(t, e) {
    kn(2048, 8, t, e);
  }
  function Ym(t) {
    ut.flags |= 4;
    var e = ut.updateQueue;
    if (e === null)
      e = Vn(), ut.updateQueue = e, e.events = [t];
    else {
      var l = e.events;
      l === null ? e.events = [t] : l.push(t);
    }
  }
  function Jr(t) {
    var e = Bt().memoizedState;
    return Ym({ ref: e, nextImpl: t }), function() {
      if ((bt & 2) !== 0) throw Error(s(440));
      return e.impl.apply(void 0, arguments);
    };
  }
  function Kr(t, e) {
    return kn(4, 2, t, e);
  }
  function wr(t, e) {
    return kn(4, 4, t, e);
  }
  function kr(t, e) {
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
  function $r(t, e, l) {
    l = l != null ? l.concat([t]) : null, kn(4, 4, kr.bind(null, e, t), l);
  }
  function Bc() {
  }
  function Wr(t, e) {
    var l = Bt();
    e = e === void 0 ? null : e;
    var a = l.memoizedState;
    return e !== null && xc(e, a[1]) ? a[0] : (l.memoizedState = [t, e], t);
  }
  function Fr(t, e) {
    var l = Bt();
    e = e === void 0 ? null : e;
    var a = l.memoizedState;
    if (e !== null && xc(e, a[1]))
      return a[0];
    if (a = t(), da) {
      _l(!0);
      try {
        t();
      } finally {
        _l(!1);
      }
    }
    return l.memoizedState = [a, e], a;
  }
  function Yc(t, e, l) {
    return l === void 0 || (il & 1073741824) !== 0 && (rt & 261930) === 0 ? t.memoizedState = e : (t.memoizedState = l, t = Po(), ut.lanes |= t, Rl |= t, l);
  }
  function Ir(t, e, l, a) {
    return be(l, e) ? l : Xa.current !== null ? (t = Yc(t, l, a), be(t, e) || (Gt = !0), t) : (il & 42) === 0 || (il & 1073741824) !== 0 && (rt & 261930) === 0 ? (Gt = !0, t.memoizedState = l) : (t = Po(), ut.lanes |= t, Rl |= t, e);
  }
  function Pr(t, e, l, a, u) {
    var n = B.p;
    B.p = n !== 0 && 8 > n ? n : 8;
    var i = z.T, o = {};
    z.T = o, Qc(t, !1, e, l);
    try {
      var d = u(), S = z.S;
      if (S !== null && S(o, d), d !== null && typeof d == "object" && typeof d.then == "function") {
        var x = Cm(
          d,
          a
        );
        Bu(
          t,
          e,
          x,
          ze(t)
        );
      } else
        Bu(
          t,
          e,
          a,
          ze(t)
        );
    } catch (M) {
      Bu(
        t,
        e,
        { then: function() {
        }, status: "rejected", reason: M },
        ze()
      );
    } finally {
      B.p = n, i !== null && o.types !== null && (i.types = o.types), z.T = i;
    }
  }
  function Lm() {
  }
  function Lc(t, e, l, a) {
    if (t.tag !== 5) throw Error(s(476));
    var u = to(t).queue;
    Pr(
      t,
      u,
      e,
      k,
      l === null ? Lm : function() {
        return eo(t), l(a);
      }
    );
  }
  function to(t) {
    var e = t.memoizedState;
    if (e !== null) return e;
    e = {
      memoizedState: k,
      baseState: k,
      baseQueue: null,
      queue: {
        pending: null,
        lanes: 0,
        dispatch: null,
        lastRenderedReducer: cl,
        lastRenderedState: k
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
  function eo(t) {
    var e = to(t);
    e.next === null && (e = t.alternate.memoizedState), Bu(
      t,
      e.next.queue,
      {},
      ze()
    );
  }
  function Gc() {
    return It(tn);
  }
  function lo() {
    return Bt().memoizedState;
  }
  function ao() {
    return Bt().memoizedState;
  }
  function Gm(t) {
    for (var e = t.return; e !== null; ) {
      switch (e.tag) {
        case 24:
        case 3:
          var l = ze();
          t = Ol(l);
          var a = Ml(e, t, l);
          a !== null && (ye(a, e, l), Uu(a, e, l)), e = { cache: hc() }, t.payload = e;
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
    }, $n(t) ? no(e, l) : (l = uc(t, e, l, a), l !== null && (ye(l, t, a), io(l, e, a)));
  }
  function uo(t, e, l) {
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
    if ($n(t)) no(e, u);
    else {
      var n = t.alternate;
      if (t.lanes === 0 && (n === null || n.lanes === 0) && (n = e.lastRenderedReducer, n !== null))
        try {
          var i = e.lastRenderedState, o = n(i, l);
          if (u.hasEagerState = !0, u.eagerState = o, be(o, i))
            return On(t, e, u, 0), qt === null && Nn(), !1;
        } catch {
        } finally {
        }
      if (l = uc(t, e, u, a), l !== null)
        return ye(l, t, a), io(l, e, a), !0;
    }
    return !1;
  }
  function Qc(t, e, l, a) {
    if (a = {
      lane: 2,
      revertLane: Sf(),
      gesture: null,
      action: a,
      hasEagerState: !1,
      eagerState: null,
      next: null
    }, $n(t)) {
      if (e) throw Error(s(479));
    } else
      e = uc(
        t,
        l,
        a,
        2
      ), e !== null && ye(e, t, 2);
  }
  function $n(t) {
    var e = t.alternate;
    return t === ut || e !== null && e === ut;
  }
  function no(t, e) {
    Za = Xn = !0;
    var l = t.pending;
    l === null ? e.next = e : (e.next = l.next, l.next = e), t.pending = e;
  }
  function io(t, e, l) {
    if ((l & 4194048) !== 0) {
      var a = e.lanes;
      a &= t.pendingLanes, l |= a, e.lanes = l, os(t, l);
    }
  }
  var Yu = {
    readContext: It,
    use: Jn,
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
  Yu.useEffectEvent = jt;
  var co = {
    readContext: It,
    use: Jn,
    useCallback: function(t, e) {
      return ie().memoizedState = [
        t,
        e === void 0 ? null : e
      ], t;
    },
    useContext: It,
    useEffect: Vr,
    useImperativeHandle: function(t, e, l) {
      l = l != null ? l.concat([t]) : null, wn(
        4194308,
        4,
        kr.bind(null, e, t),
        l
      );
    },
    useLayoutEffect: function(t, e) {
      return wn(4194308, 4, t, e);
    },
    useInsertionEffect: function(t, e) {
      wn(4, 2, t, e);
    },
    useMemo: function(t, e) {
      var l = ie();
      e = e === void 0 ? null : e;
      var a = t();
      if (da) {
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
      var a = ie();
      if (l !== void 0) {
        var u = l(e);
        if (da) {
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
      }, a.queue = t, t = t.dispatch = Qm.bind(
        null,
        ut,
        t
      ), [a.memoizedState, t];
    },
    useRef: function(t) {
      var e = ie();
      return t = { current: t }, e.memoizedState = t;
    },
    useState: function(t) {
      t = jc(t);
      var e = t.queue, l = uo.bind(null, ut, e);
      return e.dispatch = l, [t.memoizedState, l];
    },
    useDebugValue: Bc,
    useDeferredValue: function(t, e) {
      var l = ie();
      return Yc(l, t, e);
    },
    useTransition: function() {
      var t = jc(!1);
      return t = Pr.bind(
        null,
        ut,
        t.queue,
        !0,
        !1
      ), ie().memoizedState = t, [!1, t];
    },
    useSyncExternalStore: function(t, e, l) {
      var a = ut, u = ie();
      if (yt) {
        if (l === void 0)
          throw Error(s(407));
        l = l();
      } else {
        if (l = e(), qt === null)
          throw Error(s(349));
        (rt & 127) !== 0 || Nr(a, e, l);
      }
      u.memoizedState = l;
      var n = { value: l, getSnapshot: e };
      return u.queue = n, Vr(Mr.bind(null, a, n, t), [
        t
      ]), a.flags |= 2048, Ja(
        9,
        { destroy: void 0 },
        Or.bind(
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
      var t = ie(), e = qt.identifierPrefix;
      if (yt) {
        var l = we, a = Ke;
        l = (a & ~(1 << 32 - pe(a) - 1)).toString(32) + l, e = "_" + e + "R_" + l, l = Zn++, 0 < l && (e += "H" + l.toString(32)), e += "_";
      } else
        l = jm++, e = "_" + e + "r_" + l.toString(32) + "_";
      return t.memoizedState = e;
    },
    useHostTransitionStatus: Gc,
    useFormState: Lr,
    useActionState: Lr,
    useOptimistic: function(t) {
      var e = ie();
      e.memoizedState = e.baseState = t;
      var l = {
        pending: null,
        lanes: 0,
        dispatch: null,
        lastRenderedReducer: null,
        lastRenderedState: null
      };
      return e.queue = l, e = Qc.bind(
        null,
        ut,
        !0,
        l
      ), l.dispatch = e, [t, e];
    },
    useMemoCache: Dc,
    useCacheRefresh: function() {
      return ie().memoizedState = Gm.bind(
        null,
        ut
      );
    },
    useEffectEvent: function(t) {
      var e = ie(), l = { impl: t };
      return e.memoizedState = l, function() {
        if ((bt & 2) !== 0)
          throw Error(s(440));
        return l.impl.apply(void 0, arguments);
      };
    }
  }, Xc = {
    readContext: It,
    use: Jn,
    useCallback: Wr,
    useContext: It,
    useEffect: Hc,
    useImperativeHandle: $r,
    useInsertionEffect: Kr,
    useLayoutEffect: wr,
    useMemo: Fr,
    useReducer: Kn,
    useRef: Zr,
    useState: function() {
      return Kn(cl);
    },
    useDebugValue: Bc,
    useDeferredValue: function(t, e) {
      var l = Bt();
      return Ir(
        l,
        Tt.memoizedState,
        t,
        e
      );
    },
    useTransition: function() {
      var t = Kn(cl)[0], e = Bt().memoizedState;
      return [
        typeof t == "boolean" ? t : Hu(t),
        e
      ];
    },
    useSyncExternalStore: qr,
    useId: lo,
    useHostTransitionStatus: Gc,
    useFormState: Gr,
    useActionState: Gr,
    useOptimistic: function(t, e) {
      var l = Bt();
      return Cr(l, Tt, t, e);
    },
    useMemoCache: Dc,
    useCacheRefresh: ao
  };
  Xc.useEffectEvent = Jr;
  var fo = {
    readContext: It,
    use: Jn,
    useCallback: Wr,
    useContext: It,
    useEffect: Hc,
    useImperativeHandle: $r,
    useInsertionEffect: Kr,
    useLayoutEffect: wr,
    useMemo: Fr,
    useReducer: Cc,
    useRef: Zr,
    useState: function() {
      return Cc(cl);
    },
    useDebugValue: Bc,
    useDeferredValue: function(t, e) {
      var l = Bt();
      return Tt === null ? Yc(l, t, e) : Ir(
        l,
        Tt.memoizedState,
        t,
        e
      );
    },
    useTransition: function() {
      var t = Cc(cl)[0], e = Bt().memoizedState;
      return [
        typeof t == "boolean" ? t : Hu(t),
        e
      ];
    },
    useSyncExternalStore: qr,
    useId: lo,
    useHostTransitionStatus: Gc,
    useFormState: Xr,
    useActionState: Xr,
    useOptimistic: function(t, e) {
      var l = Bt();
      return Tt !== null ? Cr(l, Tt, t, e) : (l.baseState = t, [t, l.queue.dispatch]);
    },
    useMemoCache: Dc,
    useCacheRefresh: ao
  };
  fo.useEffectEvent = Jr;
  function Zc(t, e, l, a) {
    e = t.memoizedState, l = l(a, e), l = l == null ? e : E({}, e, l), t.memoizedState = l, t.lanes === 0 && (t.updateQueue.baseState = l);
  }
  var Vc = {
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
  function so(t, e, l, a, u, n, i) {
    return t = t.stateNode, typeof t.shouldComponentUpdate == "function" ? t.shouldComponentUpdate(a, n, i) : e.prototype && e.prototype.isPureReactComponent ? !Tu(l, a) || !Tu(u, n) : !0;
  }
  function ro(t, e, l, a) {
    t = e.state, typeof e.componentWillReceiveProps == "function" && e.componentWillReceiveProps(l, a), typeof e.UNSAFE_componentWillReceiveProps == "function" && e.UNSAFE_componentWillReceiveProps(l, a), e.state !== t && Vc.enqueueReplaceState(e, e.state, null);
  }
  function ya(t, e) {
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
  function oo(t) {
    qn(t);
  }
  function yo(t) {
    console.error(t);
  }
  function mo(t) {
    qn(t);
  }
  function Wn(t, e) {
    try {
      var l = t.onUncaughtError;
      l(e.value, { componentStack: e.stack });
    } catch (a) {
      setTimeout(function() {
        throw a;
      });
    }
  }
  function ho(t, e, l) {
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
  function Jc(t, e, l) {
    return l = Ol(l), l.tag = 3, l.payload = { element: null }, l.callback = function() {
      Wn(t, e);
    }, l;
  }
  function vo(t) {
    return t = Ol(t), t.tag = 3, t;
  }
  function go(t, e, l, a) {
    var u = l.type.getDerivedStateFromError;
    if (typeof u == "function") {
      var n = a.value;
      t.payload = function() {
        return u(n);
      }, t.callback = function() {
        ho(e, l, a);
      };
    }
    var i = l.stateNode;
    i !== null && typeof i.componentDidCatch == "function" && (t.callback = function() {
      ho(e, l, a), typeof u != "function" && (Hl === null ? Hl = /* @__PURE__ */ new Set([this]) : Hl.add(this));
      var o = a.stack;
      this.componentDidCatch(a.value, {
        componentStack: o !== null ? o : ""
      });
    });
  }
  function Xm(t, e, l, a, u) {
    if (l.flags |= 32768, a !== null && typeof a == "object" && typeof a.then == "function") {
      if (e = l.alternate, e !== null && Ba(
        e,
        l,
        u,
        !0
      ), l = _e.current, l !== null) {
        switch (l.tag) {
          case 31:
          case 13:
            return Re === null ? fi() : l.alternate === null && Rt === 0 && (Rt = 3), l.flags &= -257, l.flags |= 65536, l.lanes = u, a === Bn ? l.flags |= 16384 : (e = l.updateQueue, e === null ? l.updateQueue = /* @__PURE__ */ new Set([a]) : e.add(a), gf(t, a, u)), !1;
          case 22:
            return l.flags |= 65536, a === Bn ? l.flags |= 16384 : (e = l.updateQueue, e === null ? (e = {
              transitions: null,
              markerInstances: null,
              retryQueue: /* @__PURE__ */ new Set([a])
            }, l.updateQueue = e) : (l = e.retryQueue, l === null ? e.retryQueue = /* @__PURE__ */ new Set([a]) : l.add(a)), gf(t, a, u)), !1;
        }
        throw Error(s(435, l.tag));
      }
      return gf(t, a, u), fi(), !1;
    }
    if (yt)
      return e = _e.current, e !== null ? ((e.flags & 65536) === 0 && (e.flags |= 256), e.flags |= 65536, e.lanes = u, a !== rc && (t = Error(s(422), { cause: a }), qu(De(t, l)))) : (a !== rc && (e = Error(s(423), {
        cause: a
      }), qu(
        De(e, l)
      )), t = t.current.alternate, t.flags |= 65536, u &= -u, t.lanes |= u, a = De(a, l), u = Jc(
        t.stateNode,
        a,
        u
      ), _c(t, u), Rt !== 4 && (Rt = 2)), !1;
    var n = Error(s(520), { cause: a });
    if (n = De(n, l), Ku === null ? Ku = [n] : Ku.push(n), Rt !== 4 && (Rt = 2), e === null) return !0;
    a = De(a, l), l = e;
    do {
      switch (l.tag) {
        case 3:
          return l.flags |= 65536, t = u & -u, l.lanes |= t, t = Jc(l.stateNode, a, t), _c(l, t), !1;
        case 1:
          if (e = l.type, n = l.stateNode, (l.flags & 128) === 0 && (typeof e.getDerivedStateFromError == "function" || n !== null && typeof n.componentDidCatch == "function" && (Hl === null || !Hl.has(n))))
            return l.flags |= 65536, u &= -u, l.lanes |= u, u = vo(u), go(
              u,
              t,
              l,
              a
            ), _c(l, u), !1;
      }
      l = l.return;
    } while (l !== null);
    return !1;
  }
  var Kc = Error(s(461)), Gt = !1;
  function Pt(t, e, l, a) {
    e.child = t === null ? Sr(e, null, l, a) : oa(
      e,
      t.child,
      l,
      a
    );
  }
  function po(t, e, l, a, u) {
    l = l.render;
    var n = e.ref;
    if ("ref" in a) {
      var i = {};
      for (var o in a)
        o !== "ref" && (i[o] = a[o]);
    } else i = a;
    return ca(e), a = qc(
      t,
      e,
      l,
      i,
      n,
      u
    ), o = Nc(), t !== null && !Gt ? (Oc(t, e, u), fl(t, e, u)) : (yt && o && fc(e), e.flags |= 1, Pt(t, e, a, u), e.child);
  }
  function bo(t, e, l, a, u) {
    if (t === null) {
      var n = l.type;
      return typeof n == "function" && !nc(n) && n.defaultProps === void 0 && l.compare === null ? (e.tag = 15, e.type = n, So(
        t,
        e,
        n,
        a,
        u
      )) : (t = Dn(
        l.type,
        null,
        a,
        e,
        e.mode,
        u
      ), t.ref = e.ref, t.return = e, e.child = t);
    }
    if (n = t.child, !tf(t, u)) {
      var i = n.memoizedProps;
      if (l = l.compare, l = l !== null ? l : Tu, l(i, a) && t.ref === e.ref)
        return fl(t, e, u);
    }
    return e.flags |= 1, t = ll(n, a), t.ref = e.ref, t.return = e, e.child = t;
  }
  function So(t, e, l, a, u) {
    if (t !== null) {
      var n = t.memoizedProps;
      if (Tu(n, a) && t.ref === e.ref)
        if (Gt = !1, e.pendingProps = a = n, tf(t, u))
          (t.flags & 131072) !== 0 && (Gt = !0);
        else
          return e.lanes = t.lanes, fl(t, e, u);
    }
    return wc(
      t,
      e,
      l,
      a,
      u
    );
  }
  function _o(t, e, l, a) {
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
        return Eo(
          t,
          e,
          n,
          l,
          a
        );
      }
      if ((l & 536870912) !== 0)
        e.memoizedState = { baseLanes: 0, cachePool: null }, t !== null && Rn(
          e,
          n !== null ? n.cachePool : null
        ), n !== null ? Ar(e, n) : Ac(), Tr(e);
      else
        return a = e.lanes = 536870912, Eo(
          t,
          e,
          n !== null ? n.baseLanes | l : l,
          l,
          a
        );
    } else
      n !== null ? (Rn(e, n.cachePool), Ar(e, n), Ul(), e.memoizedState = null) : (t !== null && Rn(e, null), Ac(), Ul());
    return Pt(t, e, u, l), e.child;
  }
  function Lu(t, e) {
    return t !== null && t.tag === 22 || e.stateNode !== null || (e.stateNode = {
      _visibility: 1,
      _pendingMarkers: null,
      _retryCache: null,
      _transitions: null
    }), e.sibling;
  }
  function Eo(t, e, l, a, u) {
    var n = gc();
    return n = n === null ? null : { parent: Yt._currentValue, pool: n }, e.memoizedState = {
      baseLanes: l,
      cachePool: n
    }, t !== null && Rn(e, null), Ac(), Tr(e), t !== null && Ba(t, e, a, !0), e.childLanes = u, null;
  }
  function Fn(t, e) {
    return e = Pn(
      { mode: e.mode, children: e.children },
      t.mode
    ), e.ref = t.ref, t.child = e, e.return = t, e;
  }
  function Ao(t, e, l) {
    return oa(e, t.child, null, l), t = Fn(e, e.pendingProps), t.flags |= 2, Ee(e), e.memoizedState = null, t;
  }
  function Zm(t, e, l) {
    var a = e.pendingProps, u = (e.flags & 128) !== 0;
    if (e.flags &= -129, t === null) {
      if (yt) {
        if (a.mode === "hidden")
          return t = Fn(e, a), e.lanes = 536870912, Lu(null, t);
        if (zc(e), (t = Nt) ? (t = Rd(
          t,
          je
        ), t = t !== null && t.data === "&" ? t : null, t !== null && (e.memoizedState = {
          dehydrated: t,
          treeContext: Tl !== null ? { id: Ke, overflow: we } : null,
          retryLane: 536870912,
          hydrationErrors: null
        }, l = nr(t), l.return = e, e.child = l, Ft = e, Nt = null)) : t = null, t === null) throw xl(e);
        return e.lanes = 536870912, null;
      }
      return Fn(e, a);
    }
    var n = t.memoizedState;
    if (n !== null) {
      var i = n.dehydrated;
      if (zc(e), u)
        if (e.flags & 256)
          e.flags &= -257, e = Ao(
            t,
            e,
            l
          );
        else if (e.memoizedState !== null)
          e.child = t.child, e.flags |= 128, e = null;
        else throw Error(s(558));
      else if (Gt || Ba(t, e, l, !1), u = (l & t.childLanes) !== 0, Gt || u) {
        if (a = qt, a !== null && (i = ds(a, l), i !== 0 && i !== n.retryLane))
          throw n.retryLane = i, aa(t, i), ye(a, t, i), Kc;
        fi(), e = Ao(
          t,
          e,
          l
        );
      } else
        t = n.treeContext, Nt = He(i.nextSibling), Ft = e, yt = !0, zl = null, je = !1, t !== null && fr(e, t), e = Fn(e, a), e.flags |= 4096;
      return e;
    }
    return t = ll(t.child, {
      mode: a.mode,
      children: a.children
    }), t.ref = e.ref, e.child = t, t.return = e, t;
  }
  function In(t, e) {
    var l = e.ref;
    if (l === null)
      t !== null && t.ref !== null && (e.flags |= 4194816);
    else {
      if (typeof l != "function" && typeof l != "object")
        throw Error(s(284));
      (t === null || t.ref !== l) && (e.flags |= 4194816);
    }
  }
  function wc(t, e, l, a, u) {
    return ca(e), l = qc(
      t,
      e,
      l,
      a,
      void 0,
      u
    ), a = Nc(), t !== null && !Gt ? (Oc(t, e, u), fl(t, e, u)) : (yt && a && fc(e), e.flags |= 1, Pt(t, e, l, u), e.child);
  }
  function To(t, e, l, a, u, n) {
    return ca(e), e.updateQueue = null, l = xr(
      e,
      a,
      l,
      u
    ), zr(t), a = Nc(), t !== null && !Gt ? (Oc(t, e, n), fl(t, e, n)) : (yt && a && fc(e), e.flags |= 1, Pt(t, e, l, n), e.child);
  }
  function zo(t, e, l, a, u) {
    if (ca(e), e.stateNode === null) {
      var n = Ca, i = l.contextType;
      typeof i == "object" && i !== null && (n = It(i)), n = new l(a, n), e.memoizedState = n.state !== null && n.state !== void 0 ? n.state : null, n.updater = Vc, e.stateNode = n, n._reactInternals = e, n = e.stateNode, n.props = a, n.state = e.memoizedState, n.refs = {}, bc(e), i = l.contextType, n.context = typeof i == "object" && i !== null ? It(i) : Ca, n.state = e.memoizedState, i = l.getDerivedStateFromProps, typeof i == "function" && (Zc(
        e,
        l,
        i,
        a
      ), n.state = e.memoizedState), typeof l.getDerivedStateFromProps == "function" || typeof n.getSnapshotBeforeUpdate == "function" || typeof n.UNSAFE_componentWillMount != "function" && typeof n.componentWillMount != "function" || (i = n.state, typeof n.componentWillMount == "function" && n.componentWillMount(), typeof n.UNSAFE_componentWillMount == "function" && n.UNSAFE_componentWillMount(), i !== n.state && Vc.enqueueReplaceState(n, n.state, null), ju(e, a, n, u), Cu(), n.state = e.memoizedState), typeof n.componentDidMount == "function" && (e.flags |= 4194308), a = !0;
    } else if (t === null) {
      n = e.stateNode;
      var o = e.memoizedProps, d = ya(l, o);
      n.props = d;
      var S = n.context, x = l.contextType;
      i = Ca, typeof x == "object" && x !== null && (i = It(x));
      var M = l.getDerivedStateFromProps;
      x = typeof M == "function" || typeof n.getSnapshotBeforeUpdate == "function", o = e.pendingProps !== o, x || typeof n.UNSAFE_componentWillReceiveProps != "function" && typeof n.componentWillReceiveProps != "function" || (o || S !== i) && ro(
        e,
        n,
        a,
        i
      ), Nl = !1;
      var _ = e.memoizedState;
      n.state = _, ju(e, a, n, u), Cu(), S = e.memoizedState, o || _ !== S || Nl ? (typeof M == "function" && (Zc(
        e,
        l,
        M,
        a
      ), S = e.memoizedState), (d = Nl || so(
        e,
        l,
        d,
        a,
        _,
        S,
        i
      )) ? (x || typeof n.UNSAFE_componentWillMount != "function" && typeof n.componentWillMount != "function" || (typeof n.componentWillMount == "function" && n.componentWillMount(), typeof n.UNSAFE_componentWillMount == "function" && n.UNSAFE_componentWillMount()), typeof n.componentDidMount == "function" && (e.flags |= 4194308)) : (typeof n.componentDidMount == "function" && (e.flags |= 4194308), e.memoizedProps = a, e.memoizedState = S), n.props = a, n.state = S, n.context = i, a = d) : (typeof n.componentDidMount == "function" && (e.flags |= 4194308), a = !1);
    } else {
      n = e.stateNode, Sc(t, e), i = e.memoizedProps, x = ya(l, i), n.props = x, M = e.pendingProps, _ = n.context, S = l.contextType, d = Ca, typeof S == "object" && S !== null && (d = It(S)), o = l.getDerivedStateFromProps, (S = typeof o == "function" || typeof n.getSnapshotBeforeUpdate == "function") || typeof n.UNSAFE_componentWillReceiveProps != "function" && typeof n.componentWillReceiveProps != "function" || (i !== M || _ !== d) && ro(
        e,
        n,
        a,
        d
      ), Nl = !1, _ = e.memoizedState, n.state = _, ju(e, a, n, u), Cu();
      var A = e.memoizedState;
      i !== M || _ !== A || Nl || t !== null && t.dependencies !== null && Cn(t.dependencies) ? (typeof o == "function" && (Zc(
        e,
        l,
        o,
        a
      ), A = e.memoizedState), (x = Nl || so(
        e,
        l,
        x,
        a,
        _,
        A,
        d
      ) || t !== null && t.dependencies !== null && Cn(t.dependencies)) ? (S || typeof n.UNSAFE_componentWillUpdate != "function" && typeof n.componentWillUpdate != "function" || (typeof n.componentWillUpdate == "function" && n.componentWillUpdate(a, A, d), typeof n.UNSAFE_componentWillUpdate == "function" && n.UNSAFE_componentWillUpdate(
        a,
        A,
        d
      )), typeof n.componentDidUpdate == "function" && (e.flags |= 4), typeof n.getSnapshotBeforeUpdate == "function" && (e.flags |= 1024)) : (typeof n.componentDidUpdate != "function" || i === t.memoizedProps && _ === t.memoizedState || (e.flags |= 4), typeof n.getSnapshotBeforeUpdate != "function" || i === t.memoizedProps && _ === t.memoizedState || (e.flags |= 1024), e.memoizedProps = a, e.memoizedState = A), n.props = a, n.state = A, n.context = d, a = x) : (typeof n.componentDidUpdate != "function" || i === t.memoizedProps && _ === t.memoizedState || (e.flags |= 4), typeof n.getSnapshotBeforeUpdate != "function" || i === t.memoizedProps && _ === t.memoizedState || (e.flags |= 1024), a = !1);
    }
    return n = a, In(t, e), a = (e.flags & 128) !== 0, n || a ? (n = e.stateNode, l = a && typeof l.getDerivedStateFromError != "function" ? null : n.render(), e.flags |= 1, t !== null && a ? (e.child = oa(
      e,
      t.child,
      null,
      u
    ), e.child = oa(
      e,
      null,
      l,
      u
    )) : Pt(t, e, l, u), e.memoizedState = n.state, t = e.child) : t = fl(
      t,
      e,
      u
    ), t;
  }
  function xo(t, e, l, a) {
    return na(), e.flags |= 256, Pt(t, e, l, a), e.child;
  }
  var kc = {
    dehydrated: null,
    treeContext: null,
    retryLane: 0,
    hydrationErrors: null
  };
  function $c(t) {
    return { baseLanes: t, cachePool: mr() };
  }
  function Wc(t, e, l) {
    return t = t !== null ? t.childLanes & ~l : 0, e && (t |= Te), t;
  }
  function qo(t, e, l) {
    var a = e.pendingProps, u = !1, n = (e.flags & 128) !== 0, i;
    if ((i = n) || (i = t !== null && t.memoizedState === null ? !1 : (Ht.current & 2) !== 0), i && (u = !0, e.flags &= -129), i = (e.flags & 32) !== 0, e.flags &= -33, t === null) {
      if (yt) {
        if (u ? Dl(e) : Ul(), (t = Nt) ? (t = Rd(
          t,
          je
        ), t = t !== null && t.data !== "&" ? t : null, t !== null && (e.memoizedState = {
          dehydrated: t,
          treeContext: Tl !== null ? { id: Ke, overflow: we } : null,
          retryLane: 536870912,
          hydrationErrors: null
        }, l = nr(t), l.return = e, e.child = l, Ft = e, Nt = null)) : t = null, t === null) throw xl(e);
        return Cf(t) ? e.lanes = 32 : e.lanes = 536870912, null;
      }
      var o = a.children;
      return a = a.fallback, u ? (Ul(), u = e.mode, o = Pn(
        { mode: "hidden", children: o },
        u
      ), a = ua(
        a,
        u,
        l,
        null
      ), o.return = e, a.return = e, o.sibling = a, e.child = o, a = e.child, a.memoizedState = $c(l), a.childLanes = Wc(
        t,
        i,
        l
      ), e.memoizedState = kc, Lu(null, a)) : (Dl(e), Fc(e, o));
    }
    var d = t.memoizedState;
    if (d !== null && (o = d.dehydrated, o !== null)) {
      if (n)
        e.flags & 256 ? (Dl(e), e.flags &= -257, e = Ic(
          t,
          e,
          l
        )) : e.memoizedState !== null ? (Ul(), e.child = t.child, e.flags |= 128, e = null) : (Ul(), o = a.fallback, u = e.mode, a = Pn(
          { mode: "visible", children: a.children },
          u
        ), o = ua(
          o,
          u,
          l,
          null
        ), o.flags |= 2, a.return = e, o.return = e, a.sibling = o, e.child = a, oa(
          e,
          t.child,
          null,
          l
        ), a = e.child, a.memoizedState = $c(l), a.childLanes = Wc(
          t,
          i,
          l
        ), e.memoizedState = kc, e = Lu(null, a));
      else if (Dl(e), Cf(o)) {
        if (i = o.nextSibling && o.nextSibling.dataset, i) var S = i.dgst;
        i = S, a = Error(s(419)), a.stack = "", a.digest = i, qu({ value: a, source: null, stack: null }), e = Ic(
          t,
          e,
          l
        );
      } else if (Gt || Ba(t, e, l, !1), i = (l & t.childLanes) !== 0, Gt || i) {
        if (i = qt, i !== null && (a = ds(i, l), a !== 0 && a !== d.retryLane))
          throw d.retryLane = a, aa(t, a), ye(i, t, a), Kc;
        Uf(o) || fi(), e = Ic(
          t,
          e,
          l
        );
      } else
        Uf(o) ? (e.flags |= 192, e.child = t.child, e = null) : (t = d.treeContext, Nt = He(
          o.nextSibling
        ), Ft = e, yt = !0, zl = null, je = !1, t !== null && fr(e, t), e = Fc(
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
    ) : (o = ua(
      o,
      u,
      l,
      null
    ), o.flags |= 2), o.return = e, a.return = e, a.sibling = o, e.child = a, Lu(null, a), a = e.child, o = t.child.memoizedState, o === null ? o = $c(l) : (u = o.cachePool, u !== null ? (d = Yt._currentValue, u = u.parent !== d ? { parent: d, pool: d } : u) : u = mr(), o = {
      baseLanes: o.baseLanes | l,
      cachePool: u
    }), a.memoizedState = o, a.childLanes = Wc(
      t,
      i,
      l
    ), e.memoizedState = kc, Lu(t.child, a)) : (Dl(e), l = t.child, t = l.sibling, l = ll(l, {
      mode: "visible",
      children: a.children
    }), l.return = e, l.sibling = null, t !== null && (i = e.deletions, i === null ? (e.deletions = [t], e.flags |= 16) : i.push(t)), e.child = l, e.memoizedState = null, l);
  }
  function Fc(t, e) {
    return e = Pn(
      { mode: "visible", children: e },
      t.mode
    ), e.return = t, t.child = e;
  }
  function Pn(t, e) {
    return t = Se(22, t, null, e), t.lanes = 0, t;
  }
  function Ic(t, e, l) {
    return oa(e, t.child, null, l), t = Fc(
      e,
      e.pendingProps.children
    ), t.flags |= 2, e.memoizedState = null, t;
  }
  function No(t, e, l) {
    t.lanes |= e;
    var a = t.alternate;
    a !== null && (a.lanes |= e), yc(t.return, e, l);
  }
  function Pc(t, e, l, a, u, n) {
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
  function Oo(t, e, l) {
    var a = e.pendingProps, u = a.revealOrder, n = a.tail;
    a = a.children;
    var i = Ht.current, o = (i & 2) !== 0;
    if (o ? (i = i & 1 | 2, e.flags |= 128) : i &= 1, R(Ht, i), Pt(t, e, a, l), a = yt ? xu : 0, !o && t !== null && (t.flags & 128) !== 0)
      t: for (t = e.child; t !== null; ) {
        if (t.tag === 13)
          t.memoizedState !== null && No(t, l, e);
        else if (t.tag === 19)
          No(t, l, e);
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
          t = l.alternate, t !== null && Qn(t) === null && (u = l), l = l.sibling;
        l = u, l === null ? (u = e.child, e.child = null) : (u = l.sibling, l.sibling = null), Pc(
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
          if (t = u.alternate, t !== null && Qn(t) === null) {
            e.child = u;
            break;
          }
          t = u.sibling, u.sibling = l, l = u, u = t;
        }
        Pc(
          e,
          !0,
          l,
          null,
          n,
          a
        );
        break;
      case "together":
        Pc(
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
        if (Ba(
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
  function tf(t, e) {
    return (t.lanes & e) !== 0 ? !0 : (t = t.dependencies, !!(t !== null && Cn(t)));
  }
  function Vm(t, e, l) {
    switch (e.tag) {
      case 3:
        Zt(e, e.stateNode.containerInfo), ql(e, Yt, t.memoizedState.cache), na();
        break;
      case 27:
      case 5:
        Wl(e);
        break;
      case 4:
        Zt(e, e.stateNode.containerInfo);
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
          return e.flags |= 128, zc(e), null;
        break;
      case 13:
        var a = e.memoizedState;
        if (a !== null)
          return a.dehydrated !== null ? (Dl(e), e.flags |= 128, null) : (l & e.child.childLanes) !== 0 ? qo(t, e, l) : (Dl(e), t = fl(
            t,
            e,
            l
          ), t !== null ? t.sibling : null);
        Dl(e);
        break;
      case 19:
        var u = (t.flags & 128) !== 0;
        if (a = (l & e.childLanes) !== 0, a || (Ba(
          t,
          e,
          l,
          !1
        ), a = (l & e.childLanes) !== 0), u) {
          if (a)
            return Oo(
              t,
              e,
              l
            );
          e.flags |= 128;
        }
        if (u = e.memoizedState, u !== null && (u.rendering = null, u.tail = null, u.lastEffect = null), R(Ht, Ht.current), a) break;
        return null;
      case 22:
        return e.lanes = 0, _o(
          t,
          e,
          l,
          e.pendingProps
        );
      case 24:
        ql(e, Yt, t.memoizedState.cache);
    }
    return fl(t, e, l);
  }
  function Mo(t, e, l) {
    if (t !== null)
      if (t.memoizedProps !== e.pendingProps)
        Gt = !0;
      else {
        if (!tf(t, l) && (e.flags & 128) === 0)
          return Gt = !1, Vm(
            t,
            e,
            l
          );
        Gt = (t.flags & 131072) !== 0;
      }
    else
      Gt = !1, yt && (e.flags & 1048576) !== 0 && cr(e, xu, e.index);
    switch (e.lanes = 0, e.tag) {
      case 16:
        t: {
          var a = e.pendingProps;
          if (t = sa(e.elementType), e.type = t, typeof t == "function")
            nc(t) ? (a = ya(t, a), e.tag = 1, e = zo(
              null,
              e,
              t,
              a,
              l
            )) : (e.tag = 0, e = wc(
              null,
              e,
              t,
              a,
              l
            ));
          else {
            if (t != null) {
              var u = t.$$typeof;
              if (u === tt) {
                e.tag = 11, e = po(
                  null,
                  e,
                  t,
                  a,
                  l
                );
                break t;
              } else if (u === Z) {
                e.tag = 14, e = bo(
                  null,
                  e,
                  t,
                  a,
                  l
                );
                break t;
              }
            }
            throw e = qe(t) || t, Error(s(306, e, ""));
          }
        }
        return e;
      case 0:
        return wc(
          t,
          e,
          e.type,
          e.pendingProps,
          l
        );
      case 1:
        return a = e.type, u = ya(
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
          if (Zt(
            e,
            e.stateNode.containerInfo
          ), t === null) throw Error(s(387));
          a = e.pendingProps;
          var n = e.memoizedState;
          u = n.element, Sc(t, e), ju(e, a, null, l);
          var i = e.memoizedState;
          if (a = i.cache, ql(e, Yt, a), a !== n.cache && mc(
            e,
            [Yt],
            l,
            !0
          ), Cu(), a = i.element, n.isDehydrated)
            if (n = {
              element: a,
              isDehydrated: !1,
              cache: i.cache
            }, e.updateQueue.baseState = n, e.memoizedState = n, e.flags & 256) {
              e = xo(
                t,
                e,
                a,
                l
              );
              break t;
            } else if (a !== u) {
              u = De(
                Error(s(424)),
                e
              ), qu(u), e = xo(
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
              for (Nt = He(t.firstChild), Ft = e, yt = !0, zl = null, je = !0, l = Sr(
                e,
                null,
                a,
                l
              ), e.child = l; l; )
                l.flags = l.flags & -3 | 4096, l = l.sibling;
            }
          else {
            if (na(), a === u) {
              e = fl(
                t,
                e,
                l
              );
              break t;
            }
            Pt(t, e, a, l);
          }
          e = e.child;
        }
        return e;
      case 26:
        return In(t, e), t === null ? (l = Qd(
          e.type,
          null,
          e.pendingProps,
          null
        )) ? e.memoizedState = l : yt || (l = e.type, t = e.pendingProps, a = hi(
          at.current
        ).createElement(l), a[Wt] = e, a[ce] = t, te(a, l, t), Kt(a), e.stateNode = a) : e.memoizedState = Qd(
          e.type,
          t.memoizedProps,
          e.pendingProps,
          t.memoizedState
        ), null;
      case 27:
        return Wl(e), t === null && yt && (a = e.stateNode = Yd(
          e.type,
          e.pendingProps,
          at.current
        ), Ft = e, je = !0, u = Nt, Gl(e.type) ? (jf = u, Nt = He(a.firstChild)) : Nt = u), Pt(
          t,
          e,
          e.pendingProps.children,
          l
        ), In(t, e), t === null && (e.flags |= 4194304), e.child;
      case 5:
        return t === null && yt && ((u = a = Nt) && (a = Sh(
          a,
          e.type,
          e.pendingProps,
          je
        ), a !== null ? (e.stateNode = a, Ft = e, Nt = He(a.firstChild), je = !1, u = !0) : u = !1), u || xl(e)), Wl(e), u = e.type, n = e.pendingProps, i = t !== null ? t.memoizedProps : null, a = n.children, Of(u, n) ? a = null : i !== null && Of(u, i) && (e.flags |= 32), e.memoizedState !== null && (u = qc(
          t,
          e,
          Rm,
          null,
          null,
          l
        ), tn._currentValue = u), In(t, e), Pt(t, e, a, l), e.child;
      case 6:
        return t === null && yt && ((t = l = Nt) && (l = _h(
          l,
          e.pendingProps,
          je
        ), l !== null ? (e.stateNode = l, Ft = e, Nt = null, t = !0) : t = !1), t || xl(e)), null;
      case 13:
        return qo(t, e, l);
      case 4:
        return Zt(
          e,
          e.stateNode.containerInfo
        ), a = e.pendingProps, t === null ? e.child = oa(
          e,
          null,
          a,
          l
        ) : Pt(t, e, a, l), e.child;
      case 11:
        return po(
          t,
          e,
          e.type,
          e.pendingProps,
          l
        );
      case 7:
        return Pt(
          t,
          e,
          e.pendingProps,
          l
        ), e.child;
      case 8:
        return Pt(
          t,
          e,
          e.pendingProps.children,
          l
        ), e.child;
      case 12:
        return Pt(
          t,
          e,
          e.pendingProps.children,
          l
        ), e.child;
      case 10:
        return a = e.pendingProps, ql(e, e.type, a.value), Pt(t, e, a.children, l), e.child;
      case 9:
        return u = e.type._context, a = e.pendingProps.children, ca(e), u = It(u), a = a(u), e.flags |= 1, Pt(t, e, a, l), e.child;
      case 14:
        return bo(
          t,
          e,
          e.type,
          e.pendingProps,
          l
        );
      case 15:
        return So(
          t,
          e,
          e.type,
          e.pendingProps,
          l
        );
      case 19:
        return Oo(t, e, l);
      case 31:
        return Zm(t, e, l);
      case 22:
        return _o(
          t,
          e,
          l,
          e.pendingProps
        );
      case 24:
        return ca(e), a = It(Yt), t === null ? (u = gc(), u === null && (u = qt, n = hc(), u.pooledCache = n, n.refCount++, n !== null && (u.pooledCacheLanes |= l), u = n), e.memoizedState = { parent: a, cache: u }, bc(e), ql(e, Yt, u)) : ((t.lanes & l) !== 0 && (Sc(t, e), ju(e, null, null, l), Cu()), u = t.memoizedState, n = e.memoizedState, u.parent !== a ? (u = { parent: a, cache: a }, e.memoizedState = u, e.lanes === 0 && (e.memoizedState = e.updateQueue.baseState = u), ql(e, Yt, a)) : (a = n.cache, ql(e, Yt, a), a !== u.cache && mc(
          e,
          [Yt],
          l,
          !0
        ))), Pt(
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
  function ef(t, e, l, a, u) {
    if ((e = (t.mode & 32) !== 0) && (e = !1), e) {
      if (t.flags |= 16777216, (u & 335544128) === u)
        if (t.stateNode.complete) t.flags |= 8192;
        else if (ad()) t.flags |= 8192;
        else
          throw ra = Bn, pc;
    } else t.flags &= -16777217;
  }
  function Do(t, e) {
    if (e.type !== "stylesheet" || (e.state.loading & 4) !== 0)
      t.flags &= -16777217;
    else if (t.flags |= 16777216, !Kd(e))
      if (ad()) t.flags |= 8192;
      else
        throw ra = Bn, pc;
  }
  function ti(t, e) {
    e !== null && (t.flags |= 4), t.flags & 16384 && (e = t.tag !== 22 ? ss() : 536870912, t.lanes |= e, $a |= e);
  }
  function Gu(t, e) {
    if (!yt)
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
  function Jm(t, e, l) {
    var a = e.pendingProps;
    switch (sc(e), e.tag) {
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
        return l = e.stateNode, a = null, t !== null && (a = t.memoizedState.cache), e.memoizedState.cache !== a && (e.flags |= 2048), nl(Yt), At(), l.pendingContext && (l.context = l.pendingContext, l.pendingContext = null), (t === null || t.child === null) && (Ha(e) ? sl(e) : t === null || t.memoizedState.isDehydrated && (e.flags & 256) === 0 || (e.flags |= 1024, oc())), Ot(e), null;
      case 26:
        var u = e.type, n = e.memoizedState;
        return t === null ? (sl(e), n !== null ? (Ot(e), Do(e, n)) : (Ot(e), ef(
          e,
          u,
          null,
          a,
          l
        ))) : n ? n !== t.memoizedState ? (sl(e), Ot(e), Do(e, n)) : (Ot(e), e.flags &= -16777217) : (t = t.memoizedProps, t !== a && sl(e), Ot(e), ef(
          e,
          u,
          t,
          a,
          l
        )), null;
      case 27:
        if (Fl(e), l = at.current, u = e.type, t !== null && e.stateNode != null)
          t.memoizedProps !== a && sl(e);
        else {
          if (!a) {
            if (e.stateNode === null)
              throw Error(s(166));
            return Ot(e), null;
          }
          t = H.current, Ha(e) ? sr(e) : (t = Yd(u, a, l), e.stateNode = t, sl(e));
        }
        return Ot(e), null;
      case 5:
        if (Fl(e), u = e.type, t !== null && e.stateNode != null)
          t.memoizedProps !== a && sl(e);
        else {
          if (!a) {
            if (e.stateNode === null)
              throw Error(s(166));
            return Ot(e), null;
          }
          if (n = H.current, Ha(e))
            sr(e);
          else {
            var i = hi(
              at.current
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
            n[Wt] = e, n[ce] = a;
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
            t: switch (te(n, u, a), u) {
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
        return Ot(e), ef(
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
          if (t = at.current, Ha(e)) {
            if (t = e.stateNode, l = e.memoizedProps, a = null, u = Ft, u !== null)
              switch (u.tag) {
                case 27:
                case 5:
                  a = u.memoizedProps;
              }
            t[Wt] = e, t = !!(t.nodeValue === l || a !== null && a.suppressHydrationWarning === !0 || qd(t.nodeValue, l)), t || xl(e, !0);
          } else
            t = hi(t).createTextNode(
              a
            ), t[Wt] = e, e.stateNode = t;
        }
        return Ot(e), null;
      case 31:
        if (l = e.memoizedState, t === null || t.memoizedState !== null) {
          if (a = Ha(e), l !== null) {
            if (t === null) {
              if (!a) throw Error(s(318));
              if (t = e.memoizedState, t = t !== null ? t.dehydrated : null, !t) throw Error(s(557));
              t[Wt] = e;
            } else
              na(), (e.flags & 128) === 0 && (e.memoizedState = null), e.flags |= 4;
            Ot(e), t = !1;
          } else
            l = oc(), t !== null && t.memoizedState !== null && (t.memoizedState.hydrationErrors = l), t = !0;
          if (!t)
            return e.flags & 256 ? (Ee(e), e) : (Ee(e), null);
          if ((e.flags & 128) !== 0)
            throw Error(s(558));
        }
        return Ot(e), null;
      case 13:
        if (a = e.memoizedState, t === null || t.memoizedState !== null && t.memoizedState.dehydrated !== null) {
          if (u = Ha(e), a !== null && a.dehydrated !== null) {
            if (t === null) {
              if (!u) throw Error(s(318));
              if (u = e.memoizedState, u = u !== null ? u.dehydrated : null, !u) throw Error(s(317));
              u[Wt] = e;
            } else
              na(), (e.flags & 128) === 0 && (e.memoizedState = null), e.flags |= 4;
            Ot(e), u = !1;
          } else
            u = oc(), t !== null && t.memoizedState !== null && (t.memoizedState.hydrationErrors = u), u = !0;
          if (!u)
            return e.flags & 256 ? (Ee(e), e) : (Ee(e), null);
        }
        return Ee(e), (e.flags & 128) !== 0 ? (e.lanes = l, e) : (l = a !== null, t = t !== null && t.memoizedState !== null, l && (a = e.child, u = null, a.alternate !== null && a.alternate.memoizedState !== null && a.alternate.memoizedState.cachePool !== null && (u = a.alternate.memoizedState.cachePool.pool), n = null, a.memoizedState !== null && a.memoizedState.cachePool !== null && (n = a.memoizedState.cachePool.pool), n !== u && (a.flags |= 2048)), l !== t && l && (e.child.flags |= 8192), ti(e, e.updateQueue), Ot(e), null);
      case 4:
        return At(), t === null && Tf(e.stateNode.containerInfo), Ot(e), null;
      case 10:
        return nl(e.type), Ot(e), null;
      case 19:
        if (U(Ht), a = e.memoizedState, a === null) return Ot(e), null;
        if (u = (e.flags & 128) !== 0, n = a.rendering, n === null)
          if (u) Gu(a, !1);
          else {
            if (Rt !== 0 || t !== null && (t.flags & 128) !== 0)
              for (t = e.child; t !== null; ) {
                if (n = Qn(t), n !== null) {
                  for (e.flags |= 128, Gu(a, !1), t = n.updateQueue, e.updateQueue = t, ti(e, t), e.subtreeFlags = 0, t = l, l = e.child; l !== null; )
                    ur(l, t), l = l.sibling;
                  return R(
                    Ht,
                    Ht.current & 1 | 2
                  ), yt && al(e, a.treeForkCount), e.child;
                }
                t = t.sibling;
              }
            a.tail !== null && ht() > ni && (e.flags |= 128, u = !0, Gu(a, !1), e.lanes = 4194304);
          }
        else {
          if (!u)
            if (t = Qn(n), t !== null) {
              if (e.flags |= 128, u = !0, t = t.updateQueue, e.updateQueue = t, ti(e, t), Gu(a, !0), a.tail === null && a.tailMode === "hidden" && !n.alternate && !yt)
                return Ot(e), null;
            } else
              2 * ht() - a.renderingStartTime > ni && l !== 536870912 && (e.flags |= 128, u = !0, Gu(a, !1), e.lanes = 4194304);
          a.isBackwards ? (n.sibling = e.child, e.child = n) : (t = a.last, t !== null ? t.sibling = n : e.child = n, a.last = n);
        }
        return a.tail !== null ? (t = a.tail, a.rendering = t, a.tail = t.sibling, a.renderingStartTime = ht(), t.sibling = null, l = Ht.current, R(
          Ht,
          u ? l & 1 | 2 : l & 1
        ), yt && al(e, a.treeForkCount), t) : (Ot(e), null);
      case 22:
      case 23:
        return Ee(e), Tc(), a = e.memoizedState !== null, t !== null ? t.memoizedState !== null !== a && (e.flags |= 8192) : a && (e.flags |= 8192), a ? (l & 536870912) !== 0 && (e.flags & 128) === 0 && (Ot(e), e.subtreeFlags & 6 && (e.flags |= 8192)) : Ot(e), l = e.updateQueue, l !== null && ti(e, l.retryQueue), l = null, t !== null && t.memoizedState !== null && t.memoizedState.cachePool !== null && (l = t.memoizedState.cachePool.pool), a = null, e.memoizedState !== null && e.memoizedState.cachePool !== null && (a = e.memoizedState.cachePool.pool), a !== l && (e.flags |= 2048), t !== null && U(fa), null;
      case 24:
        return l = null, t !== null && (l = t.memoizedState.cache), e.memoizedState.cache !== l && (e.flags |= 2048), nl(Yt), Ot(e), null;
      case 25:
        return null;
      case 30:
        return null;
    }
    throw Error(s(156, e.tag));
  }
  function Km(t, e) {
    switch (sc(e), e.tag) {
      case 1:
        return t = e.flags, t & 65536 ? (e.flags = t & -65537 | 128, e) : null;
      case 3:
        return nl(Yt), At(), t = e.flags, (t & 65536) !== 0 && (t & 128) === 0 ? (e.flags = t & -65537 | 128, e) : null;
      case 26:
      case 27:
      case 5:
        return Fl(e), null;
      case 31:
        if (e.memoizedState !== null) {
          if (Ee(e), e.alternate === null)
            throw Error(s(340));
          na();
        }
        return t = e.flags, t & 65536 ? (e.flags = t & -65537 | 128, e) : null;
      case 13:
        if (Ee(e), t = e.memoizedState, t !== null && t.dehydrated !== null) {
          if (e.alternate === null)
            throw Error(s(340));
          na();
        }
        return t = e.flags, t & 65536 ? (e.flags = t & -65537 | 128, e) : null;
      case 19:
        return U(Ht), null;
      case 4:
        return At(), null;
      case 10:
        return nl(e.type), null;
      case 22:
      case 23:
        return Ee(e), Tc(), t !== null && U(fa), t = e.flags, t & 65536 ? (e.flags = t & -65537 | 128, e) : null;
      case 24:
        return nl(Yt), null;
      case 25:
        return null;
      default:
        return null;
    }
  }
  function Uo(t, e) {
    switch (sc(e), e.tag) {
      case 3:
        nl(Yt), At();
        break;
      case 26:
      case 27:
      case 5:
        Fl(e);
        break;
      case 4:
        At();
        break;
      case 31:
        e.memoizedState !== null && Ee(e);
        break;
      case 13:
        Ee(e);
        break;
      case 19:
        U(Ht);
        break;
      case 10:
        nl(e.type);
        break;
      case 22:
      case 23:
        Ee(e), Tc(), t !== null && U(fa);
        break;
      case 24:
        nl(Yt);
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
              } catch (x) {
                Et(
                  u,
                  d,
                  x
                );
              }
            }
          }
          a = a.next;
        } while (a !== n);
      }
    } catch (x) {
      Et(e, e.return, x);
    }
  }
  function Co(t) {
    var e = t.updateQueue;
    if (e !== null) {
      var l = t.stateNode;
      try {
        Er(e, l);
      } catch (a) {
        Et(t, t.return, a);
      }
    }
  }
  function jo(t, e, l) {
    l.props = ya(
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
  function Ro(t) {
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
  function lf(t, e, l) {
    try {
      var a = t.stateNode;
      mh(a, t.type, l, e), a[ce] = e;
    } catch (u) {
      Et(t, t.return, u);
    }
  }
  function Ho(t) {
    return t.tag === 5 || t.tag === 3 || t.tag === 26 || t.tag === 27 && Gl(t.type) || t.tag === 4;
  }
  function af(t) {
    t: for (; ; ) {
      for (; t.sibling === null; ) {
        if (t.return === null || Ho(t.return)) return null;
        t = t.return;
      }
      for (t.sibling.return = t.return, t = t.sibling; t.tag !== 5 && t.tag !== 6 && t.tag !== 18; ) {
        if (t.tag === 27 && Gl(t.type) || t.flags & 2 || t.child === null || t.tag === 4) continue t;
        t.child.return = t, t = t.child;
      }
      if (!(t.flags & 2)) return t.stateNode;
    }
  }
  function uf(t, e, l) {
    var a = t.tag;
    if (a === 5 || a === 6)
      t = t.stateNode, e ? (l.nodeType === 9 ? l.body : l.nodeName === "HTML" ? l.ownerDocument.body : l).insertBefore(t, e) : (e = l.nodeType === 9 ? l.body : l.nodeName === "HTML" ? l.ownerDocument.body : l, e.appendChild(t), l = l._reactRootContainer, l != null || e.onclick !== null || (e.onclick = tl));
    else if (a !== 4 && (a === 27 && Gl(t.type) && (l = t.stateNode, e = null), t = t.child, t !== null))
      for (uf(t, e, l), t = t.sibling; t !== null; )
        uf(t, e, l), t = t.sibling;
  }
  function ei(t, e, l) {
    var a = t.tag;
    if (a === 5 || a === 6)
      t = t.stateNode, e ? l.insertBefore(t, e) : l.appendChild(t);
    else if (a !== 4 && (a === 27 && Gl(t.type) && (l = t.stateNode), t = t.child, t !== null))
      for (ei(t, e, l), t = t.sibling; t !== null; )
        ei(t, e, l), t = t.sibling;
  }
  function Bo(t) {
    var e = t.stateNode, l = t.memoizedProps;
    try {
      for (var a = t.type, u = e.attributes; u.length; )
        e.removeAttributeNode(u[0]);
      te(e, a, l), e[Wt] = t, e[ce] = l;
    } catch (n) {
      Et(t, t.return, n);
    }
  }
  var rl = !1, Qt = !1, nf = !1, Yo = typeof WeakSet == "function" ? WeakSet : Set, wt = null;
  function wm(t, e) {
    if (t = t.containerInfo, qf = Ei, t = $s(t), Ii(t)) {
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
            var i = 0, o = -1, d = -1, S = 0, x = 0, M = t, _ = null;
            e: for (; ; ) {
              for (var A; M !== l || u !== 0 && M.nodeType !== 3 || (o = i + u), M !== n || a !== 0 && M.nodeType !== 3 || (d = i + a), M.nodeType === 3 && (i += M.nodeValue.length), (A = M.firstChild) !== null; )
                _ = M, M = A;
              for (; ; ) {
                if (M === t) break e;
                if (_ === l && ++S === u && (o = i), _ === n && ++x === a && (d = i), (A = M.nextSibling) !== null) break;
                M = _, _ = M.parentNode;
              }
              M = A;
            }
            l = o === -1 || d === -1 ? null : { start: o, end: d };
          } else l = null;
        }
      l = l || { start: 0, end: 0 };
    } else l = null;
    for (Nf = { focusedElem: t, selectionRange: l }, Ei = !1, wt = e; wt !== null; )
      if (e = wt, t = e.child, (e.subtreeFlags & 1028) !== 0 && t !== null)
        t.return = e, wt = t;
      else
        for (; wt !== null; ) {
          switch (e = wt, n = e.alternate, t = e.flags, e.tag) {
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
                  var L = ya(
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
                  Df(t);
                else if (l === 1)
                  switch (t.nodeName) {
                    case "HEAD":
                    case "HTML":
                    case "BODY":
                      Df(t);
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
            t.return = e.return, wt = t;
            break;
          }
          wt = e.return;
        }
  }
  function Lo(t, e, l) {
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
            var u = ya(
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
        a & 64 && Co(l), a & 512 && Xu(l, l.return);
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
            Er(t, e);
          } catch (i) {
            Et(l, l.return, i);
          }
        }
        break;
      case 27:
        e === null && a & 4 && Bo(l);
      case 26:
      case 5:
        dl(t, l), e === null && a & 4 && Ro(l), a & 512 && Xu(l, l.return);
        break;
      case 12:
        dl(t, l);
        break;
      case 31:
        dl(t, l), a & 4 && Xo(t, l);
        break;
      case 13:
        dl(t, l), a & 4 && Zo(t, l), a & 64 && (t = l.memoizedState, t !== null && (t = t.dehydrated, t !== null && (l = lh.bind(
          null,
          l
        ), Eh(t, l))));
        break;
      case 22:
        if (a = l.memoizedState !== null || rl, !a) {
          e = e !== null && e.memoizedState !== null || Qt, u = rl;
          var n = Qt;
          rl = a, (Qt = e) && !n ? yl(
            t,
            l,
            (l.subtreeFlags & 8772) !== 0
          ) : dl(t, l), rl = u, Qt = n;
        }
        break;
      case 30:
        break;
      default:
        dl(t, l);
    }
  }
  function Go(t) {
    var e = t.alternate;
    e !== null && (t.alternate = null, Go(e)), t.child = null, t.deletions = null, t.sibling = null, t.tag === 5 && (e = t.stateNode, e !== null && Ri(e)), t.stateNode = null, t.return = null, t.dependencies = null, t.memoizedProps = null, t.memoizedState = null, t.pendingProps = null, t.stateNode = null, t.updateQueue = null;
  }
  var Mt = null, se = !1;
  function ol(t, e, l) {
    for (l = l.child; l !== null; )
      Qo(t, e, l), l = l.sibling;
  }
  function Qo(t, e, l) {
    if (ge && typeof ge.onCommitFiberUnmount == "function")
      try {
        ge.onCommitFiberUnmount(du, l);
      } catch {
      }
    switch (l.tag) {
      case 26:
        Qt || ke(l, e), ol(
          t,
          e,
          l
        ), l.memoizedState ? l.memoizedState.count-- : l.stateNode && (l = l.stateNode, l.parentNode.removeChild(l));
        break;
      case 27:
        Qt || ke(l, e);
        var a = Mt, u = se;
        Gl(l.type) && (Mt = l.stateNode, se = !1), ol(
          t,
          e,
          l
        ), Fu(l.stateNode), Mt = a, se = u;
        break;
      case 5:
        Qt || ke(l, e);
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
        Mt !== null && (se ? (t = Mt, Cd(
          t.nodeType === 9 ? t.body : t.nodeName === "HTML" ? t.ownerDocument.body : t,
          l.stateNode
        ), au(t)) : Cd(Mt, l.stateNode));
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
        Cl(2, l, e), Qt || Cl(4, l, e), ol(
          t,
          e,
          l
        );
        break;
      case 1:
        Qt || (ke(l, e), a = l.stateNode, typeof a.componentWillUnmount == "function" && jo(
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
        Qt = (a = Qt) || l.memoizedState !== null, ol(
          t,
          e,
          l
        ), Qt = a;
        break;
      default:
        ol(
          t,
          e,
          l
        );
    }
  }
  function Xo(t, e) {
    if (e.memoizedState === null && (t = e.alternate, t !== null && (t = t.memoizedState, t !== null))) {
      t = t.dehydrated;
      try {
        au(t);
      } catch (l) {
        Et(e, e.return, l);
      }
    }
  }
  function Zo(t, e) {
    if (e.memoizedState === null && (t = e.alternate, t !== null && (t = t.memoizedState, t !== null && (t = t.dehydrated, t !== null))))
      try {
        au(t);
      } catch (l) {
        Et(e, e.return, l);
      }
  }
  function km(t) {
    switch (t.tag) {
      case 31:
      case 13:
      case 19:
        var e = t.stateNode;
        return e === null && (e = t.stateNode = new Yo()), e;
      case 22:
        return t = t.stateNode, e = t._retryCache, e === null && (e = t._retryCache = new Yo()), e;
      default:
        throw Error(s(435, t.tag));
    }
  }
  function li(t, e) {
    var l = km(t);
    e.forEach(function(a) {
      if (!l.has(a)) {
        l.add(a);
        var u = ah.bind(null, t, a);
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
        Qo(n, i, u), Mt = null, se = !1, n = u.alternate, n !== null && (n.return = null), u.return = null;
      }
    if (e.subtreeFlags & 13886)
      for (e = e.child; e !== null; )
        Vo(e, t), e = e.sibling;
  }
  var Xe = null;
  function Vo(t, e) {
    var l = t.alternate, a = t.flags;
    switch (t.tag) {
      case 0:
      case 11:
      case 14:
      case 15:
        re(e, t), oe(t), a & 4 && (Cl(3, t, t.return), Qu(3, t), Cl(5, t, t.return));
        break;
      case 1:
        re(e, t), oe(t), a & 512 && (Qt || l === null || ke(l, l.return)), a & 64 && rl && (t = t.updateQueue, t !== null && (a = t.callbacks, a !== null && (l = t.shared.hiddenCallbacks, t.shared.hiddenCallbacks = l === null ? a : l.concat(a))));
        break;
      case 26:
        var u = Xe;
        if (re(e, t), oe(t), a & 512 && (Qt || l === null || ke(l, l.return)), a & 4) {
          var n = l !== null ? l.memoizedState : null;
          if (a = t.memoizedState, l === null)
            if (a === null)
              if (t.stateNode === null) {
                t: {
                  a = t.type, l = t.memoizedProps, u = u.ownerDocument || u;
                  e: switch (a) {
                    case "title":
                      n = u.getElementsByTagName("title")[0], (!n || n[hu] || n[Wt] || n.namespaceURI === "http://www.w3.org/2000/svg" || n.hasAttribute("itemprop")) && (n = u.createElement(a), u.head.insertBefore(
                        n,
                        u.querySelector("head > title")
                      )), te(n, a, l), n[Wt] = t, Kt(n), a = n;
                      break t;
                    case "link":
                      var i = Vd(
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
                      n = u.createElement(a), te(n, a, l), u.head.appendChild(n);
                      break;
                    case "meta":
                      if (i = Vd(
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
                      n = u.createElement(a), te(n, a, l), u.head.appendChild(n);
                      break;
                    default:
                      throw Error(s(468, a));
                  }
                  n[Wt] = t, Kt(n), a = n;
                }
                t.stateNode = a;
              } else
                Jd(
                  u,
                  t.type,
                  t.stateNode
                );
            else
              t.stateNode = Zd(
                u,
                a,
                t.memoizedProps
              );
          else
            n !== a ? (n === null ? l.stateNode !== null && (l = l.stateNode, l.parentNode.removeChild(l)) : n.count--, a === null ? Jd(
              u,
              t.type,
              t.stateNode
            ) : Zd(
              u,
              a,
              t.memoizedProps
            )) : a === null && t.stateNode !== null && lf(
              t,
              t.memoizedProps,
              l.memoizedProps
            );
        }
        break;
      case 27:
        re(e, t), oe(t), a & 512 && (Qt || l === null || ke(l, l.return)), l !== null && a & 4 && lf(
          t,
          t.memoizedProps,
          l.memoizedProps
        );
        break;
      case 5:
        if (re(e, t), oe(t), a & 512 && (Qt || l === null || ke(l, l.return)), t.flags & 32) {
          u = t.stateNode;
          try {
            xa(u, "");
          } catch (L) {
            Et(t, t.return, L);
          }
        }
        a & 4 && t.stateNode != null && (u = t.memoizedProps, lf(
          t,
          u,
          l !== null ? l.memoizedProps : u
        )), a & 1024 && (nf = !0);
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
        if (pi = null, u = Xe, Xe = vi(e.containerInfo), re(e, t), Xe = u, oe(t), a & 4 && l !== null && l.memoizedState.isDehydrated)
          try {
            au(e.containerInfo);
          } catch (L) {
            Et(t, t.return, L);
          }
        nf && (nf = !1, Jo(t));
        break;
      case 4:
        a = Xe, Xe = vi(
          t.stateNode.containerInfo
        ), re(e, t), oe(t), Xe = a;
        break;
      case 12:
        re(e, t), oe(t);
        break;
      case 31:
        re(e, t), oe(t), a & 4 && (a = t.updateQueue, a !== null && (t.updateQueue = null, li(t, a)));
        break;
      case 13:
        re(e, t), oe(t), t.child.flags & 8192 && t.memoizedState !== null != (l !== null && l.memoizedState !== null) && (ui = ht()), a & 4 && (a = t.updateQueue, a !== null && (t.updateQueue = null, li(t, a)));
        break;
      case 22:
        u = t.memoizedState !== null;
        var d = l !== null && l.memoizedState !== null, S = rl, x = Qt;
        if (rl = S || u, Qt = x || d, re(e, t), Qt = x, rl = S, oe(t), a & 8192)
          t: for (e = t.stateNode, e._visibility = u ? e._visibility & -2 : e._visibility | 1, u && (l === null || d || rl || Qt || ma(t)), l = null, e = t; ; ) {
            if (e.tag === 5 || e.tag === 26) {
              if (l === null) {
                d = l = e;
                try {
                  if (n = d.stateNode, u)
                    i = n.style, typeof i.setProperty == "function" ? i.setProperty("display", "none", "important") : i.display = "none";
                  else {
                    o = d.stateNode;
                    var M = d.memoizedProps.style, _ = M != null && M.hasOwnProperty("display") ? M.display : null;
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
                  u ? jd(A, !0) : jd(d.stateNode, !1);
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
        a & 4 && (a = t.updateQueue, a !== null && (l = a.retryQueue, l !== null && (a.retryQueue = null, li(t, l))));
        break;
      case 19:
        re(e, t), oe(t), a & 4 && (a = t.updateQueue, a !== null && (t.updateQueue = null, li(t, a)));
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
          if (Ho(a)) {
            l = a;
            break;
          }
          a = a.return;
        }
        if (l == null) throw Error(s(160));
        switch (l.tag) {
          case 27:
            var u = l.stateNode, n = af(t);
            ei(t, n, u);
            break;
          case 5:
            var i = l.stateNode;
            l.flags & 32 && (xa(i, ""), l.flags &= -33);
            var o = af(t);
            ei(t, o, i);
            break;
          case 3:
          case 4:
            var d = l.stateNode.containerInfo, S = af(t);
            uf(
              t,
              S,
              d
            );
            break;
          default:
            throw Error(s(161));
        }
      } catch (x) {
        Et(t, t.return, x);
      }
      t.flags &= -3;
    }
    e & 4096 && (t.flags &= -4097);
  }
  function Jo(t) {
    if (t.subtreeFlags & 1024)
      for (t = t.child; t !== null; ) {
        var e = t;
        Jo(e), e.tag === 5 && e.flags & 1024 && e.stateNode.reset(), t = t.sibling;
      }
  }
  function dl(t, e) {
    if (e.subtreeFlags & 8772)
      for (e = e.child; e !== null; )
        Lo(t, e.alternate, e), e = e.sibling;
  }
  function ma(t) {
    for (t = t.child; t !== null; ) {
      var e = t;
      switch (e.tag) {
        case 0:
        case 11:
        case 14:
        case 15:
          Cl(4, e, e.return), ma(e);
          break;
        case 1:
          ke(e, e.return);
          var l = e.stateNode;
          typeof l.componentWillUnmount == "function" && jo(
            e,
            e.return,
            l
          ), ma(e);
          break;
        case 27:
          Fu(e.stateNode);
        case 26:
        case 5:
          ke(e, e.return), ma(e);
          break;
        case 22:
          e.memoizedState === null && ma(e);
          break;
        case 30:
          ma(e);
          break;
        default:
          ma(e);
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
                  _r(d[u], o);
            } catch (S) {
              Et(a, a.return, S);
            }
          }
          l && i & 64 && Co(n), Xu(n, n.return);
          break;
        case 27:
          Bo(n);
        case 26:
        case 5:
          yl(
            u,
            n,
            l
          ), l && a === null && i & 4 && Ro(n), Xu(n, n.return);
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
          ), l && i & 4 && Xo(u, n);
          break;
        case 13:
          yl(
            u,
            n,
            l
          ), l && i & 4 && Zo(u, n);
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
  function cf(t, e) {
    var l = null;
    t !== null && t.memoizedState !== null && t.memoizedState.cachePool !== null && (l = t.memoizedState.cachePool.pool), t = null, e.memoizedState !== null && e.memoizedState.cachePool !== null && (t = e.memoizedState.cachePool.pool), t !== l && (t != null && t.refCount++, l != null && Nu(l));
  }
  function ff(t, e) {
    t = null, e.alternate !== null && (t = e.alternate.memoizedState.cache), e = e.memoizedState.cache, e !== t && (e.refCount++, t != null && Nu(t));
  }
  function Ze(t, e, l, a) {
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
        Ze(
          t,
          e,
          l,
          a
        ), u & 2048 && Qu(9, e);
        break;
      case 1:
        Ze(
          t,
          e,
          l,
          a
        );
        break;
      case 3:
        Ze(
          t,
          e,
          l,
          a
        ), u & 2048 && (t = null, e.alternate !== null && (t = e.alternate.memoizedState.cache), e = e.memoizedState.cache, e !== t && (e.refCount++, t != null && Nu(t)));
        break;
      case 12:
        if (u & 2048) {
          Ze(
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
          Ze(
            t,
            e,
            l,
            a
          );
        break;
      case 31:
        Ze(
          t,
          e,
          l,
          a
        );
        break;
      case 13:
        Ze(
          t,
          e,
          l,
          a
        );
        break;
      case 23:
        break;
      case 22:
        n = e.stateNode, i = e.alternate, e.memoizedState !== null ? n._visibility & 2 ? Ze(
          t,
          e,
          l,
          a
        ) : Zu(t, e) : n._visibility & 2 ? Ze(
          t,
          e,
          l,
          a
        ) : (n._visibility |= 2, Ka(
          t,
          e,
          l,
          a,
          (e.subtreeFlags & 10256) !== 0 || !1
        )), u & 2048 && cf(i, e);
        break;
      case 24:
        Ze(
          t,
          e,
          l,
          a
        ), u & 2048 && ff(e.alternate, e);
        break;
      default:
        Ze(
          t,
          e,
          l,
          a
        );
    }
  }
  function Ka(t, e, l, a, u) {
    for (u = u && ((e.subtreeFlags & 10256) !== 0 || !1), e = e.child; e !== null; ) {
      var n = t, i = e, o = l, d = a, S = i.flags;
      switch (i.tag) {
        case 0:
        case 11:
        case 15:
          Ka(
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
          var x = i.stateNode;
          i.memoizedState !== null ? x._visibility & 2 ? Ka(
            n,
            i,
            o,
            d,
            u
          ) : Zu(
            n,
            i
          ) : (x._visibility |= 2, Ka(
            n,
            i,
            o,
            d,
            u
          )), u && S & 2048 && cf(
            i.alternate,
            i
          );
          break;
        case 24:
          Ka(
            n,
            i,
            o,
            d,
            u
          ), u && S & 2048 && ff(i.alternate, i);
          break;
        default:
          Ka(
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
            Zu(l, a), u & 2048 && cf(
              a.alternate,
              a
            );
            break;
          case 24:
            Zu(l, a), u & 2048 && ff(a.alternate, a);
            break;
          default:
            Zu(l, a);
        }
        e = e.sibling;
      }
  }
  var Vu = 8192;
  function wa(t, e, l) {
    if (t.subtreeFlags & Vu)
      for (t = t.child; t !== null; )
        wo(
          t,
          e,
          l
        ), t = t.sibling;
  }
  function wo(t, e, l) {
    switch (t.tag) {
      case 26:
        wa(
          t,
          e,
          l
        ), t.flags & Vu && t.memoizedState !== null && jh(
          l,
          Xe,
          t.memoizedState,
          t.memoizedProps
        );
        break;
      case 5:
        wa(
          t,
          e,
          l
        );
        break;
      case 3:
      case 4:
        var a = Xe;
        Xe = vi(t.stateNode.containerInfo), wa(
          t,
          e,
          l
        ), Xe = a;
        break;
      case 22:
        t.memoizedState === null && (a = t.alternate, a !== null && a.memoizedState !== null ? (a = Vu, Vu = 16777216, wa(
          t,
          e,
          l
        ), Vu = a) : wa(
          t,
          e,
          l
        ));
        break;
      default:
        wa(
          t,
          e,
          l
        );
    }
  }
  function ko(t) {
    var e = t.alternate;
    if (e !== null && (t = e.child, t !== null)) {
      e.child = null;
      do
        e = t.sibling, t.sibling = null, t = e;
      while (t !== null);
    }
  }
  function Ju(t) {
    var e = t.deletions;
    if ((t.flags & 16) !== 0) {
      if (e !== null)
        for (var l = 0; l < e.length; l++) {
          var a = e[l];
          wt = a, Wo(
            a,
            t
          );
        }
      ko(t);
    }
    if (t.subtreeFlags & 10256)
      for (t = t.child; t !== null; )
        $o(t), t = t.sibling;
  }
  function $o(t) {
    switch (t.tag) {
      case 0:
      case 11:
      case 15:
        Ju(t), t.flags & 2048 && Cl(9, t, t.return);
        break;
      case 3:
        Ju(t);
        break;
      case 12:
        Ju(t);
        break;
      case 22:
        var e = t.stateNode;
        t.memoizedState !== null && e._visibility & 2 && (t.return === null || t.return.tag !== 13) ? (e._visibility &= -3, ai(t)) : Ju(t);
        break;
      default:
        Ju(t);
    }
  }
  function ai(t) {
    var e = t.deletions;
    if ((t.flags & 16) !== 0) {
      if (e !== null)
        for (var l = 0; l < e.length; l++) {
          var a = e[l];
          wt = a, Wo(
            a,
            t
          );
        }
      ko(t);
    }
    for (t = t.child; t !== null; ) {
      switch (e = t, e.tag) {
        case 0:
        case 11:
        case 15:
          Cl(8, e, e.return), ai(e);
          break;
        case 22:
          l = e.stateNode, l._visibility & 2 && (l._visibility &= -3, ai(e));
          break;
        default:
          ai(e);
      }
      t = t.sibling;
    }
  }
  function Wo(t, e) {
    for (; wt !== null; ) {
      var l = wt;
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
      if (a = l.child, a !== null) a.return = l, wt = a;
      else
        t: for (l = t; wt !== null; ) {
          a = wt;
          var u = a.sibling, n = a.return;
          if (Go(a), a === l) {
            wt = null;
            break t;
          }
          if (u !== null) {
            u.return = n, wt = u;
            break t;
          }
          wt = n;
        }
    }
  }
  var $m = {
    getCacheForType: function(t) {
      var e = It(Yt), l = e.data.get(t);
      return l === void 0 && (l = t(), e.data.set(t, l)), l;
    },
    cacheSignal: function() {
      return It(Yt).controller.signal;
    }
  }, Wm = typeof WeakMap == "function" ? WeakMap : Map, bt = 0, qt = null, ft = null, rt = 0, _t = 0, Ae = null, jl = !1, ka = !1, sf = !1, ml = 0, Rt = 0, Rl = 0, ha = 0, rf = 0, Te = 0, $a = 0, Ku = null, de = null, of = !1, ui = 0, Fo = 0, ni = 1 / 0, ii = null, Hl = null, Vt = 0, Bl = null, Wa = null, hl = 0, df = 0, yf = null, Io = null, wu = 0, mf = null;
  function ze() {
    return (bt & 2) !== 0 && rt !== 0 ? rt & -rt : z.T !== null ? Sf() : ys();
  }
  function Po() {
    if (Te === 0)
      if ((rt & 536870912) === 0 || yt) {
        var t = mn;
        mn <<= 1, (mn & 3932160) === 0 && (mn = 262144), Te = t;
      } else Te = 536870912;
    return t = _e.current, t !== null && (t.flags |= 32), Te;
  }
  function ye(t, e, l) {
    (t === qt && (_t === 2 || _t === 9) || t.cancelPendingCommit !== null) && (Fa(t, 0), Yl(
      t,
      rt,
      Te,
      !1
    )), mu(t, l), ((bt & 2) === 0 || t !== qt) && (t === qt && ((bt & 2) === 0 && (ha |= l), Rt === 4 && Yl(
      t,
      rt,
      Te,
      !1
    )), $e(t));
  }
  function td(t, e, l) {
    if ((bt & 6) !== 0) throw Error(s(327));
    var a = !l && (e & 127) === 0 && (e & t.expiredLanes) === 0 || yu(t, e), u = a ? Pm(t, e) : vf(t, e, !0), n = a;
    do {
      if (u === 0) {
        ka && !a && Yl(t, e, 0, !1);
        break;
      } else {
        if (l = t.current.alternate, n && !Fm(l)) {
          u = vf(t, e, !1), n = !1;
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
              u = Ku;
              var d = o.current.memoizedState.isDehydrated;
              if (d && (Fa(o, i).flags |= 256), i = vf(
                o,
                i,
                !1
              ), i !== 2) {
                if (sf && !d) {
                  o.errorRecoveryDisabledLanes |= n, ha |= n, u = 4;
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
          Fa(t, 0), Yl(t, e, 0, !0);
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
                Te,
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
          if ((e & 62914560) === e && (u = ui + 300 - ht(), 10 < u)) {
            if (Yl(
              a,
              e,
              Te,
              !jl
            ), vn(a, 0, !0) !== 0) break t;
            hl = e, a.timeoutHandle = Dd(
              ed.bind(
                null,
                a,
                l,
                de,
                ii,
                of,
                e,
                Te,
                ha,
                $a,
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
          ed(
            a,
            l,
            de,
            ii,
            of,
            e,
            Te,
            ha,
            $a,
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
  function ed(t, e, l, a, u, n, i, o, d, S, x, M, _, A) {
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
      }, wo(
        e,
        n,
        M
      );
      var L = (n & 62914560) === n ? ui - ht() : (n & 4194048) === n ? Fo - ht() : 0;
      if (L = Rh(
        M,
        L
      ), L !== null) {
        hl = n, t.cancelPendingCommit = L(
          sd.bind(
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
            x,
            M,
            null,
            _,
            A
          )
        ), Yl(t, n, i, !S);
        return;
      }
    }
    sd(
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
  function Fm(t) {
    for (var e = t; ; ) {
      var l = e.tag;
      if ((l === 0 || l === 11 || l === 15) && e.flags & 16384 && (l = e.updateQueue, l !== null && (l = l.stores, l !== null)))
        for (var a = 0; a < l.length; a++) {
          var u = l[a], n = u.getSnapshot;
          u = u.value;
          try {
            if (!be(n(), u)) return !1;
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
    e &= ~rf, e &= ~ha, t.suspendedLanes |= e, t.pingedLanes &= ~e, a && (t.warmLanes |= e), a = t.expirationTimes;
    for (var u = e; 0 < u; ) {
      var n = 31 - pe(u), i = 1 << n;
      a[n] = -1, u &= ~i;
    }
    l !== 0 && rs(t, l, e);
  }
  function ci() {
    return (bt & 6) === 0 ? (ku(0), !1) : !0;
  }
  function hf() {
    if (ft !== null) {
      if (_t === 0)
        var t = ft.return;
      else
        t = ft, ul = ia = null, Mc(t), Qa = null, Mu = 0, t = ft;
      for (; t !== null; )
        Uo(t.alternate, t), t = t.return;
      ft = null;
    }
  }
  function Fa(t, e) {
    var l = t.timeoutHandle;
    l !== -1 && (t.timeoutHandle = -1, gh(l)), l = t.cancelPendingCommit, l !== null && (t.cancelPendingCommit = null, l()), hl = 0, hf(), qt = t, ft = l = ll(t.current, null), rt = e, _t = 0, Ae = null, jl = !1, ka = yu(t, e), sf = !1, $a = Te = rf = ha = Rl = Rt = 0, de = Ku = null, of = !1, (e & 8) !== 0 && (e |= e & 32);
    var a = t.entangledLanes;
    if (a !== 0)
      for (t = t.entanglements, a &= e; 0 < a; ) {
        var u = 31 - pe(a), n = 1 << u;
        e |= t[u], a &= ~n;
      }
    return ml = e, Nn(), l;
  }
  function ld(t, e) {
    ut = null, z.H = Yu, e === Ga || e === Hn ? (e = gr(), _t = 3) : e === pc ? (e = gr(), _t = 4) : _t = e === Kc ? 8 : e !== null && typeof e == "object" && typeof e.then == "function" ? 6 : 1, Ae = e, ft === null && (Rt = 1, Wn(
      t,
      De(e, t.current)
    ));
  }
  function ad() {
    var t = _e.current;
    return t === null ? !0 : (rt & 4194048) === rt ? Re === null : (rt & 62914560) === rt || (rt & 536870912) !== 0 ? t === Re : !1;
  }
  function ud() {
    var t = z.H;
    return z.H = Yu, t === null ? Yu : t;
  }
  function nd() {
    var t = z.A;
    return z.A = $m, t;
  }
  function fi() {
    Rt = 4, jl || (rt & 4194048) !== rt && _e.current !== null || (ka = !0), (Rl & 134217727) === 0 && (ha & 134217727) === 0 || qt === null || Yl(
      qt,
      rt,
      Te,
      !1
    );
  }
  function vf(t, e, l) {
    var a = bt;
    bt |= 2;
    var u = ud(), n = nd();
    (qt !== t || rt !== e) && (ii = null, Fa(t, e)), e = !1;
    var i = Rt;
    t: do
      try {
        if (_t !== 0 && ft !== null) {
          var o = ft, d = Ae;
          switch (_t) {
            case 8:
              hf(), i = 6;
              break t;
            case 3:
            case 2:
            case 9:
            case 6:
              _e.current === null && (e = !0);
              var S = _t;
              if (_t = 0, Ae = null, Ia(t, o, d, S), l && ka) {
                i = 0;
                break t;
              }
              break;
            default:
              S = _t, _t = 0, Ae = null, Ia(t, o, d, S);
          }
        }
        Im(), i = Rt;
        break;
      } catch (x) {
        ld(t, x);
      }
    while (!0);
    return e && t.shellSuspendCounter++, ul = ia = null, bt = a, z.H = u, z.A = n, ft === null && (qt = null, rt = 0, Nn()), i;
  }
  function Im() {
    for (; ft !== null; ) id(ft);
  }
  function Pm(t, e) {
    var l = bt;
    bt |= 2;
    var a = ud(), u = nd();
    qt !== t || rt !== e ? (ii = null, ni = ht() + 500, Fa(t, e)) : ka = yu(
      t,
      e
    );
    t: do
      try {
        if (_t !== 0 && ft !== null) {
          e = ft;
          var n = Ae;
          e: switch (_t) {
            case 1:
              _t = 0, Ae = null, Ia(t, e, n, 1);
              break;
            case 2:
            case 9:
              if (hr(n)) {
                _t = 0, Ae = null, cd(e);
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
              hr(n) ? (_t = 0, Ae = null, cd(e)) : (_t = 0, Ae = null, Ia(t, e, n, 7));
              break;
            case 5:
              var i = null;
              switch (ft.tag) {
                case 26:
                  i = ft.memoizedState;
                case 5:
                case 27:
                  var o = ft;
                  if (i ? Kd(i) : o.stateNode.complete) {
                    _t = 0, Ae = null;
                    var d = o.sibling;
                    if (d !== null) ft = d;
                    else {
                      var S = o.return;
                      S !== null ? (ft = S, si(S)) : ft = null;
                    }
                    break e;
                  }
              }
              _t = 0, Ae = null, Ia(t, e, n, 5);
              break;
            case 6:
              _t = 0, Ae = null, Ia(t, e, n, 6);
              break;
            case 8:
              hf(), Rt = 6;
              break t;
            default:
              throw Error(s(462));
          }
        }
        th();
        break;
      } catch (x) {
        ld(t, x);
      }
    while (!0);
    return ul = ia = null, z.H = a, z.A = u, bt = l, ft !== null ? 0 : (qt = null, rt = 0, Nn(), Rt);
  }
  function th() {
    for (; ft !== null && !X(); )
      id(ft);
  }
  function id(t) {
    var e = Mo(t.alternate, t, ml);
    t.memoizedProps = t.pendingProps, e === null ? si(t) : ft = e;
  }
  function cd(t) {
    var e = t, l = e.alternate;
    switch (e.tag) {
      case 15:
      case 0:
        e = To(
          l,
          e,
          e.pendingProps,
          e.type,
          void 0,
          rt
        );
        break;
      case 11:
        e = To(
          l,
          e,
          e.pendingProps,
          e.type.render,
          e.ref,
          rt
        );
        break;
      case 5:
        Mc(e);
      default:
        Uo(l, e), e = ft = ur(e, ml), e = Mo(l, e, ml);
    }
    t.memoizedProps = t.pendingProps, e === null ? si(t) : ft = e;
  }
  function Ia(t, e, l, a) {
    ul = ia = null, Mc(e), Qa = null, Mu = 0;
    var u = e.return;
    try {
      if (Xm(
        t,
        u,
        e,
        l,
        rt
      )) {
        Rt = 1, Wn(
          t,
          De(l, t.current)
        ), ft = null;
        return;
      }
    } catch (n) {
      if (u !== null) throw ft = u, n;
      Rt = 1, Wn(
        t,
        De(l, t.current)
      ), ft = null;
      return;
    }
    e.flags & 32768 ? (yt || a === 1 ? t = !0 : ka || (rt & 536870912) !== 0 ? t = !1 : (jl = t = !0, (a === 2 || a === 9 || a === 3 || a === 6) && (a = _e.current, a !== null && a.tag === 13 && (a.flags |= 16384))), fd(e, t)) : si(e);
  }
  function si(t) {
    var e = t;
    do {
      if ((e.flags & 32768) !== 0) {
        fd(
          e,
          jl
        );
        return;
      }
      t = e.return;
      var l = Jm(
        e.alternate,
        e,
        ml
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
    Rt === 0 && (Rt = 5);
  }
  function fd(t, e) {
    do {
      var l = Km(t.alternate, t);
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
    Rt = 6, ft = null;
  }
  function sd(t, e, l, a, u, n, i, o, d) {
    t.cancelPendingCommit = null;
    do
      ri();
    while (Vt !== 0);
    if ((bt & 6) !== 0) throw Error(s(327));
    if (e !== null) {
      if (e === t.current) throw Error(s(177));
      if (n = e.lanes | e.childLanes, n |= ac, Cy(
        t,
        l,
        n,
        i,
        o,
        d
      ), t === qt && (ft = qt = null, rt = 0), Wa = e, Bl = t, hl = l, df = n, yf = u, Io = a, (e.subtreeFlags & 10256) !== 0 || (e.flags & 10256) !== 0 ? (t.callbackNode = null, t.callbackPriority = 0, uh(dn, function() {
        return md(), null;
      })) : (t.callbackNode = null, t.callbackPriority = 0), a = (e.flags & 13878) !== 0, (e.subtreeFlags & 13878) !== 0 || a) {
        a = z.T, z.T = null, u = B.p, B.p = 2, i = bt, bt |= 4;
        try {
          wm(t, e, l);
        } finally {
          bt = i, B.p = u, z.T = a;
        }
      }
      Vt = 1, rd(), od(), dd();
    }
  }
  function rd() {
    if (Vt === 1) {
      Vt = 0;
      var t = Bl, e = Wa, l = (e.flags & 13878) !== 0;
      if ((e.subtreeFlags & 13878) !== 0 || l) {
        l = z.T, z.T = null;
        var a = B.p;
        B.p = 2;
        var u = bt;
        bt |= 4;
        try {
          Vo(e, t);
          var n = Nf, i = $s(t.containerInfo), o = n.focusedElem, d = n.selectionRange;
          if (i !== o && o && o.ownerDocument && ks(
            o.ownerDocument.documentElement,
            o
          )) {
            if (d !== null && Ii(o)) {
              var S = d.start, x = d.end;
              if (x === void 0 && (x = S), "selectionStart" in o)
                o.selectionStart = S, o.selectionEnd = Math.min(
                  x,
                  o.value.length
                );
              else {
                var M = o.ownerDocument || document, _ = M && M.defaultView || window;
                if (_.getSelection) {
                  var A = _.getSelection(), L = o.textContent.length, $ = Math.min(d.start, L), xt = d.end === void 0 ? $ : Math.min(d.end, L);
                  !A.extend && $ > xt && (i = xt, xt = $, $ = i);
                  var v = ws(
                    o,
                    $
                  ), y = ws(
                    o,
                    xt
                  );
                  if (v && y && (A.rangeCount !== 1 || A.anchorNode !== v.node || A.anchorOffset !== v.offset || A.focusNode !== y.node || A.focusOffset !== y.offset)) {
                    var b = M.createRange();
                    b.setStart(v.node, v.offset), A.removeAllRanges(), $ > xt ? (A.addRange(b), A.extend(y.node, y.offset)) : (b.setEnd(y.node, y.offset), A.addRange(b));
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
          Ei = !!qf, Nf = qf = null;
        } finally {
          bt = u, B.p = a, z.T = l;
        }
      }
      t.current = e, Vt = 2;
    }
  }
  function od() {
    if (Vt === 2) {
      Vt = 0;
      var t = Bl, e = Wa, l = (e.flags & 8772) !== 0;
      if ((e.subtreeFlags & 8772) !== 0 || l) {
        l = z.T, z.T = null;
        var a = B.p;
        B.p = 2;
        var u = bt;
        bt |= 4;
        try {
          Lo(t, e.alternate, e);
        } finally {
          bt = u, B.p = a, z.T = l;
        }
      }
      Vt = 3;
    }
  }
  function dd() {
    if (Vt === 4 || Vt === 3) {
      Vt = 0, pt();
      var t = Bl, e = Wa, l = hl, a = Io;
      (e.subtreeFlags & 10256) !== 0 || (e.flags & 10256) !== 0 ? Vt = 5 : (Vt = 0, Wa = Bl = null, yd(t, t.pendingLanes));
      var u = t.pendingLanes;
      if (u === 0 && (Hl = null), Ci(l), e = e.stateNode, ge && typeof ge.onCommitFiberRoot == "function")
        try {
          ge.onCommitFiberRoot(
            du,
            e,
            void 0,
            (e.current.flags & 128) === 128
          );
        } catch {
        }
      if (a !== null) {
        e = z.T, u = B.p, B.p = 2, z.T = null;
        try {
          for (var n = t.onRecoverableError, i = 0; i < a.length; i++) {
            var o = a[i];
            n(o.value, {
              componentStack: o.stack
            });
          }
        } finally {
          z.T = e, B.p = u;
        }
      }
      (hl & 3) !== 0 && ri(), $e(t), u = t.pendingLanes, (l & 261930) !== 0 && (u & 42) !== 0 ? t === mf ? wu++ : (wu = 0, mf = t) : wu = 0, ku(0);
    }
  }
  function yd(t, e) {
    (t.pooledCacheLanes &= e) === 0 && (e = t.pooledCache, e != null && (t.pooledCache = null, Nu(e)));
  }
  function ri() {
    return rd(), od(), dd(), md();
  }
  function md() {
    if (Vt !== 5) return !1;
    var t = Bl, e = df;
    df = 0;
    var l = Ci(hl), a = z.T, u = B.p;
    try {
      B.p = 32 > l ? 32 : l, z.T = null, l = yf, yf = null;
      var n = Bl, i = hl;
      if (Vt = 0, Wa = Bl = null, hl = 0, (bt & 6) !== 0) throw Error(s(331));
      var o = bt;
      if (bt |= 4, $o(n.current), Ko(
        n,
        n.current,
        i,
        l
      ), bt = o, ku(0, !1), ge && typeof ge.onPostCommitFiberRoot == "function")
        try {
          ge.onPostCommitFiberRoot(du, n);
        } catch {
        }
      return !0;
    } finally {
      B.p = u, z.T = a, yd(t, e);
    }
  }
  function hd(t, e, l) {
    e = De(l, e), e = Jc(t.stateNode, e, 2), t = Ml(t, e, 2), t !== null && (mu(t, 2), $e(t));
  }
  function Et(t, e, l) {
    if (t.tag === 3)
      hd(t, t, l);
    else
      for (; e !== null; ) {
        if (e.tag === 3) {
          hd(
            e,
            t,
            l
          );
          break;
        } else if (e.tag === 1) {
          var a = e.stateNode;
          if (typeof e.type.getDerivedStateFromError == "function" || typeof a.componentDidCatch == "function" && (Hl === null || !Hl.has(a))) {
            t = De(l, t), l = vo(2), a = Ml(e, l, 2), a !== null && (go(
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
  function gf(t, e, l) {
    var a = t.pingCache;
    if (a === null) {
      a = t.pingCache = new Wm();
      var u = /* @__PURE__ */ new Set();
      a.set(e, u);
    } else
      u = a.get(e), u === void 0 && (u = /* @__PURE__ */ new Set(), a.set(e, u));
    u.has(l) || (sf = !0, u.add(l), t = eh.bind(null, t, e, l), e.then(t, t));
  }
  function eh(t, e, l) {
    var a = t.pingCache;
    a !== null && a.delete(e), t.pingedLanes |= t.suspendedLanes & l, t.warmLanes &= ~l, qt === t && (rt & l) === l && (Rt === 4 || Rt === 3 && (rt & 62914560) === rt && 300 > ht() - ui ? (bt & 2) === 0 && Fa(t, 0) : rf |= l, $a === rt && ($a = 0)), $e(t);
  }
  function vd(t, e) {
    e === 0 && (e = ss()), t = aa(t, e), t !== null && (mu(t, e), $e(t));
  }
  function lh(t) {
    var e = t.memoizedState, l = 0;
    e !== null && (l = e.retryLane), vd(t, l);
  }
  function ah(t, e) {
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
    a !== null && a.delete(e), vd(t, l);
  }
  function uh(t, e) {
    return Sl(t, e);
  }
  var oi = null, Pa = null, pf = !1, di = !1, bf = !1, Ll = 0;
  function $e(t) {
    t !== Pa && t.next === null && (Pa === null ? oi = Pa = t : Pa = Pa.next = t), di = !0, pf || (pf = !0, ih());
  }
  function ku(t, e) {
    if (!bf && di) {
      bf = !0;
      do
        for (var l = !1, a = oi; a !== null; ) {
          if (t !== 0) {
            var u = a.pendingLanes;
            if (u === 0) var n = 0;
            else {
              var i = a.suspendedLanes, o = a.pingedLanes;
              n = (1 << 31 - pe(42 | t) + 1) - 1, n &= u & ~(i & ~o), n = n & 201326741 ? n & 201326741 | 1 : n ? n | 2 : 0;
            }
            n !== 0 && (l = !0, Sd(a, n));
          } else
            n = rt, n = vn(
              a,
              a === qt ? n : 0,
              a.cancelPendingCommit !== null || a.timeoutHandle !== -1
            ), (n & 3) === 0 || yu(a, n) || (l = !0, Sd(a, n));
          a = a.next;
        }
      while (l);
      bf = !1;
    }
  }
  function nh() {
    gd();
  }
  function gd() {
    di = pf = !1;
    var t = 0;
    Ll !== 0 && vh() && (t = Ll);
    for (var e = ht(), l = null, a = oi; a !== null; ) {
      var u = a.next, n = pd(a, e);
      n === 0 ? (a.next = null, l === null ? oi = u : l.next = u, u === null && (Pa = l)) : (l = a, (t !== 0 || (n & 3) !== 0) && (di = !0)), a = u;
    }
    Vt !== 0 && Vt !== 5 || ku(t), Ll !== 0 && (Ll = 0);
  }
  function pd(t, e) {
    for (var l = t.suspendedLanes, a = t.pingedLanes, u = t.expirationTimes, n = t.pendingLanes & -62914561; 0 < n; ) {
      var i = 31 - pe(n), o = 1 << i, d = u[i];
      d === -1 ? ((o & l) === 0 || (o & a) !== 0) && (u[i] = Uy(o, e)) : d <= e && (t.expiredLanes |= o), n &= ~o;
    }
    if (e = qt, l = rt, l = vn(
      t,
      t === e ? l : 0,
      t.cancelPendingCommit !== null || t.timeoutHandle !== -1
    ), a = t.callbackNode, l === 0 || t === e && (_t === 2 || _t === 9) || t.cancelPendingCommit !== null)
      return a !== null && a !== null && ru(a), t.callbackNode = null, t.callbackPriority = 0;
    if ((l & 3) === 0 || yu(t, l)) {
      if (e = l & -l, e === t.callbackPriority) return e;
      switch (a !== null && ru(a), Ci(l)) {
        case 2:
        case 8:
          l = ou;
          break;
        case 32:
          l = dn;
          break;
        case 268435456:
          l = fs;
          break;
        default:
          l = dn;
      }
      return a = bd.bind(null, t), l = Sl(l, a), t.callbackPriority = e, t.callbackNode = l, e;
    }
    return a !== null && a !== null && ru(a), t.callbackPriority = 2, t.callbackNode = null, 2;
  }
  function bd(t, e) {
    if (Vt !== 0 && Vt !== 5)
      return t.callbackNode = null, t.callbackPriority = 0, null;
    var l = t.callbackNode;
    if (ri() && t.callbackNode !== l)
      return null;
    var a = rt;
    return a = vn(
      t,
      t === qt ? a : 0,
      t.cancelPendingCommit !== null || t.timeoutHandle !== -1
    ), a === 0 ? null : (td(t, a, e), pd(t, ht()), t.callbackNode != null && t.callbackNode === l ? bd.bind(null, t) : null);
  }
  function Sd(t, e) {
    if (ri()) return null;
    td(t, e, !0);
  }
  function ih() {
    ph(function() {
      (bt & 6) !== 0 ? Sl(
        $t,
        nh
      ) : gd();
    });
  }
  function Sf() {
    if (Ll === 0) {
      var t = Ya;
      t === 0 && (t = yn, yn <<= 1, (yn & 261888) === 0 && (yn = 256)), Ll = t;
    }
    return Ll;
  }
  function _d(t) {
    return t == null || typeof t == "symbol" || typeof t == "boolean" ? null : typeof t == "function" ? t : Sn("" + t);
  }
  function Ed(t, e) {
    var l = e.ownerDocument.createElement("input");
    return l.name = e.name, l.value = e.value, t.id && l.setAttribute("form", t.id), e.parentNode.insertBefore(l, e), t = new FormData(t), l.parentNode.removeChild(l), t;
  }
  function ch(t, e, l, a, u) {
    if (e === "submit" && l && l.stateNode === u) {
      var n = _d(
        (u[ce] || null).action
      ), i = a.submitter;
      i && (e = (e = i[ce] || null) ? _d(e.formAction) : i.getAttribute("formAction"), e !== null && (n = e, i = null));
      var o = new Tn(
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
                  var d = i ? Ed(u, i) : new FormData(u);
                  Lc(
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
                typeof n == "function" && (o.preventDefault(), d = i ? Ed(u, i) : new FormData(u), Lc(
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
  for (var _f = 0; _f < lc.length; _f++) {
    var Ef = lc[_f], fh = Ef.toLowerCase(), sh = Ef[0].toUpperCase() + Ef.slice(1);
    Qe(
      fh,
      "on" + sh
    );
  }
  Qe(Is, "onAnimationEnd"), Qe(Ps, "onAnimationIteration"), Qe(tr, "onAnimationStart"), Qe("dblclick", "onDoubleClick"), Qe("focusin", "onFocus"), Qe("focusout", "onBlur"), Qe(zm, "onTransitionRun"), Qe(xm, "onTransitionStart"), Qe(qm, "onTransitionCancel"), Qe(er, "onTransitionEnd"), Ta("onMouseEnter", ["mouseout", "mouseover"]), Ta("onMouseLeave", ["mouseout", "mouseover"]), Ta("onPointerEnter", ["pointerout", "pointerover"]), Ta("onPointerLeave", ["pointerout", "pointerover"]), Pl(
    "onChange",
    "change click focusin focusout input keydown keyup selectionchange".split(" ")
  ), Pl(
    "onSelect",
    "focusout contextmenu dragend focusin keydown keyup mousedown mouseup selectionchange".split(
      " "
    )
  ), Pl("onBeforeInput", [
    "compositionend",
    "keypress",
    "textInput",
    "paste"
  ]), Pl(
    "onCompositionEnd",
    "compositionend focusout keydown keypress keyup mousedown".split(" ")
  ), Pl(
    "onCompositionStart",
    "compositionstart focusout keydown keypress keyup mousedown".split(" ")
  ), Pl(
    "onCompositionUpdate",
    "compositionupdate focusout keydown keypress keyup mousedown".split(" ")
  );
  var $u = "abort canplay canplaythrough durationchange emptied encrypted ended error loadeddata loadedmetadata loadstart pause play playing progress ratechange resize seeked seeking stalled suspend timeupdate volumechange waiting".split(
    " "
  ), rh = new Set(
    "beforetoggle cancel close invalid load scroll scrollend toggle".split(" ").concat($u)
  );
  function Ad(t, e) {
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
            } catch (x) {
              qn(x);
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
            } catch (x) {
              qn(x);
            }
            u.currentTarget = null, n = d;
          }
      }
    }
  }
  function st(t, e) {
    var l = e[ji];
    l === void 0 && (l = e[ji] = /* @__PURE__ */ new Set());
    var a = t + "__bubble";
    l.has(a) || (Td(e, t, 2, !1), l.add(a));
  }
  function Af(t, e, l) {
    var a = 0;
    e && (a |= 4), Td(
      l,
      t,
      a,
      e
    );
  }
  var yi = "_reactListening" + Math.random().toString(36).slice(2);
  function Tf(t) {
    if (!t[yi]) {
      t[yi] = !0, vs.forEach(function(l) {
        l !== "selectionchange" && (rh.has(l) || Af(l, !1, t), Af(l, !0, t));
      });
      var e = t.nodeType === 9 ? t : t.ownerDocument;
      e === null || e[yi] || (e[yi] = !0, Af("selectionchange", !1, e));
    }
  }
  function Td(t, e, l, a) {
    switch (Pd(e)) {
      case 2:
        var u = Yh;
        break;
      case 8:
        u = Lh;
        break;
      default:
        u = Lf;
    }
    l = u.bind(
      null,
      e,
      l,
      t
    ), u = void 0, !Zi || e !== "touchstart" && e !== "touchmove" && e !== "wheel" || (u = !0), a ? u !== void 0 ? t.addEventListener(e, l, {
      capture: !0,
      passive: u
    }) : t.addEventListener(e, l, !0) : u !== void 0 ? t.addEventListener(e, l, {
      passive: u
    }) : t.addEventListener(e, l, !1);
  }
  function zf(t, e, l, a, u) {
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
            if (i = _a(o), i === null) return;
            if (d = i.tag, d === 5 || d === 6 || d === 26 || d === 27) {
              a = n = i;
              continue t;
            }
            o = o.parentNode;
          }
        }
        a = a.return;
      }
    Ns(function() {
      var S = n, x = Qi(l), M = [];
      t: {
        var _ = lr.get(t);
        if (_ !== void 0) {
          var A = Tn, L = t;
          switch (t) {
            case "keypress":
              if (En(l) === 0) break t;
            case "keydown":
            case "keyup":
              A = am;
              break;
            case "focusin":
              L = "focus", A = wi;
              break;
            case "focusout":
              L = "blur", A = wi;
              break;
            case "beforeblur":
            case "afterblur":
              A = wi;
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
              A = Ds;
              break;
            case "drag":
            case "dragend":
            case "dragenter":
            case "dragexit":
            case "dragleave":
            case "dragover":
            case "dragstart":
            case "drop":
              A = Jy;
              break;
            case "touchcancel":
            case "touchend":
            case "touchmove":
            case "touchstart":
              A = im;
              break;
            case Is:
            case Ps:
            case tr:
              A = ky;
              break;
            case er:
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
              A = Cs;
              break;
            case "toggle":
            case "beforetoggle":
              A = dm;
          }
          var $ = (e & 4) !== 0, xt = !$ && (t === "scroll" || t === "scrollend"), v = $ ? _ !== null ? _ + "Capture" : null : _;
          $ = [];
          for (var y = S, b; y !== null; ) {
            var O = y;
            if (b = O.stateNode, O = O.tag, O !== 5 && O !== 26 && O !== 27 || b === null || v === null || (O = gu(y, v), O != null && $.push(
              Wu(y, O, b)
            )), xt) break;
            y = y.return;
          }
          0 < $.length && (_ = new A(
            _,
            L,
            null,
            l,
            x
          ), M.push({ event: _, listeners: $ }));
        }
      }
      if ((e & 7) === 0) {
        t: {
          if (_ = t === "mouseover" || t === "pointerover", A = t === "mouseout" || t === "pointerout", _ && l !== Gi && (L = l.relatedTarget || l.fromElement) && (_a(L) || L[Sa]))
            break t;
          if ((A || _) && (_ = x.window === x ? x : (_ = x.ownerDocument) ? _.defaultView || _.parentWindow : window, A ? (L = l.relatedTarget || l.toElement, A = S, L = L ? _a(L) : null, L !== null && (xt = g(L), $ = L.tag, L !== xt || $ !== 5 && $ !== 27 && $ !== 6) && (L = null)) : (A = null, L = S), A !== L)) {
            if ($ = Ds, O = "onMouseLeave", v = "onMouseEnter", y = "mouse", (t === "pointerout" || t === "pointerover") && ($ = Cs, O = "onPointerLeave", v = "onPointerEnter", y = "pointer"), xt = A == null ? _ : vu(A), b = L == null ? _ : vu(L), _ = new $(
              O,
              y + "leave",
              A,
              l,
              x
            ), _.target = xt, _.relatedTarget = b, O = null, _a(x) === S && ($ = new $(
              v,
              y + "enter",
              L,
              l,
              x
            ), $.target = b, $.relatedTarget = xt, O = $), xt = O, A && L)
              e: {
                for ($ = oh, v = A, y = L, b = 0, O = v; O; O = $(O))
                  b++;
                O = 0;
                for (var V = y; V; V = $(V))
                  O++;
                for (; 0 < b - O; )
                  v = $(v), b--;
                for (; 0 < O - b; )
                  y = $(y), O--;
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
              M,
              _,
              A,
              $,
              !1
            ), L !== null && xt !== null && zd(
              M,
              xt,
              L,
              $,
              !0
            );
          }
        }
        t: {
          if (_ = S ? vu(S) : window, A = _.nodeName && _.nodeName.toLowerCase(), A === "select" || A === "input" && _.type === "file")
            var vt = Qs;
          else if (Ls(_))
            if (Xs)
              vt = Em;
            else {
              vt = Sm;
              var Q = bm;
            }
          else
            A = _.nodeName, !A || A.toLowerCase() !== "input" || _.type !== "checkbox" && _.type !== "radio" ? S && Li(S.elementType) && (vt = Qs) : vt = _m;
          if (vt && (vt = vt(t, S))) {
            Gs(
              M,
              vt,
              l,
              x
            );
            break t;
          }
          Q && Q(t, _, S), t === "focusout" && S && _.type === "number" && S.memoizedProps.value != null && Yi(_, "number", _.value);
        }
        switch (Q = S ? vu(S) : window, t) {
          case "focusin":
            (Ls(Q) || Q.contentEditable === "true") && (Ma = Q, Pi = S, zu = null);
            break;
          case "focusout":
            zu = Pi = Ma = null;
            break;
          case "mousedown":
            tc = !0;
            break;
          case "contextmenu":
          case "mouseup":
          case "dragend":
            tc = !1, Ws(M, l, x);
            break;
          case "selectionchange":
            if (Tm) break;
          case "keydown":
          case "keyup":
            Ws(M, l, x);
        }
        var nt;
        if ($i)
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
          Oa ? Bs(t, l) && (ot = "onCompositionEnd") : t === "keydown" && l.keyCode === 229 && (ot = "onCompositionStart");
        ot && (js && l.locale !== "ko" && (Oa || ot !== "onCompositionStart" ? ot === "onCompositionEnd" && Oa && (nt = Os()) : (Al = x, Vi = "value" in Al ? Al.value : Al.textContent, Oa = !0)), Q = mi(S, ot), 0 < Q.length && (ot = new Us(
          ot,
          t,
          null,
          l,
          x
        ), M.push({ event: ot, listeners: Q }), nt ? ot.data = nt : (nt = Ys(l), nt !== null && (ot.data = nt)))), (nt = mm ? hm(t, l) : vm(t, l)) && (ot = mi(S, "onBeforeInput"), 0 < ot.length && (Q = new Us(
          "onBeforeInput",
          "beforeinput",
          null,
          l,
          x
        ), M.push({
          event: Q,
          listeners: ot
        }), Q.data = nt)), ch(
          M,
          t,
          S,
          l,
          x
        );
      }
      Ad(M, e);
    });
  }
  function Wu(t, e, l) {
    return {
      instance: t,
      listener: e,
      currentTarget: l
    };
  }
  function mi(t, e) {
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
  function oh(t) {
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
  var dh = /\r\n?/g, yh = /\u0000|\uFFFD/g;
  function xd(t) {
    return (typeof t == "string" ? t : "" + t).replace(dh, `
`).replace(yh, "");
  }
  function qd(t, e) {
    return e = xd(e), xd(t) === e;
  }
  function zt(t, e, l, a, u, n) {
    switch (l) {
      case "children":
        typeof a == "string" ? e === "body" || e === "textarea" && a === "" || xa(t, a) : (typeof a == "number" || typeof a == "bigint") && e !== "body" && xa(t, "" + a);
        break;
      case "className":
        pn(t, "class", a);
        break;
      case "tabIndex":
        pn(t, "tabindex", a);
        break;
      case "dir":
      case "role":
      case "viewBox":
      case "width":
      case "height":
        pn(t, l, a);
        break;
      case "style":
        xs(t, a, n);
        break;
      case "data":
        if (e !== "object") {
          pn(t, "data", a);
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
        a = Sn("" + a), t.setAttribute(l, a);
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
        a = Sn("" + a), t.setAttribute(l, a);
        break;
      case "onClick":
        a != null && (t.onclick = tl);
        break;
      case "onScroll":
        a != null && st("scroll", t);
        break;
      case "onScrollEnd":
        a != null && st("scrollend", t);
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
        l = Sn("" + a), t.setAttributeNS(
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
        st("beforetoggle", t), st("toggle", t), gn(t, "popover", a);
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
        gn(t, "is", a);
        break;
      case "innerText":
      case "textContent":
        break;
      default:
        (!(2 < l.length) || l[0] !== "o" && l[0] !== "O" || l[1] !== "n" && l[1] !== "N") && (l = Qy.get(l) || l, gn(t, l, a));
    }
  }
  function xf(t, e, l, a, u, n) {
    switch (l) {
      case "style":
        xs(t, a, n);
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
        typeof a == "string" ? xa(t, a) : (typeof a == "number" || typeof a == "bigint") && xa(t, "" + a);
        break;
      case "onScroll":
        a != null && st("scroll", t);
        break;
      case "onScrollEnd":
        a != null && st("scrollend", t);
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
        if (!gs.hasOwnProperty(l))
          t: {
            if (l[0] === "o" && l[1] === "n" && (u = l.endsWith("Capture"), e = l.slice(2, u ? l.length - 7 : void 0), n = t[ce] || null, n = n != null ? n[l] : null, typeof n == "function" && t.removeEventListener(e, n, u), typeof a == "function")) {
              typeof n != "function" && n !== null && (l in t ? t[l] = null : t.hasAttribute(l) && t.removeAttribute(l)), t.addEventListener(e, a, u);
              break t;
            }
            l in t ? t[l] = a : a === !0 ? t.setAttribute(l, "") : gn(t, l, a);
          }
    }
  }
  function te(t, e, l) {
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
        st("invalid", t);
        var o = n = i = u = null, d = null, S = null;
        for (a in l)
          if (l.hasOwnProperty(a)) {
            var x = l[a];
            if (x != null)
              switch (a) {
                case "name":
                  u = x;
                  break;
                case "type":
                  i = x;
                  break;
                case "checked":
                  d = x;
                  break;
                case "defaultChecked":
                  S = x;
                  break;
                case "value":
                  n = x;
                  break;
                case "defaultValue":
                  o = x;
                  break;
                case "children":
                case "dangerouslySetInnerHTML":
                  if (x != null)
                    throw Error(s(137, e));
                  break;
                default:
                  zt(t, e, a, x, l, null);
              }
          }
        Es(
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
        st("invalid", t), a = i = n = null;
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
        e = n, l = i, t.multiple = !!a, e != null ? za(t, !!a, e, !1) : l != null && za(t, !!a, l, !0);
        return;
      case "textarea":
        st("invalid", t), n = u = a = null;
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
        Ts(t, a, u, n);
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
        st("beforetoggle", t), st("toggle", t), st("cancel", t), st("close", t);
        break;
      case "iframe":
      case "object":
        st("load", t);
        break;
      case "video":
      case "audio":
        for (a = 0; a < $u.length; a++)
          st($u[a], t);
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
        if (Li(e)) {
          for (x in l)
            l.hasOwnProperty(x) && (a = l[x], a !== void 0 && xf(
              t,
              e,
              x,
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
        var u = null, n = null, i = null, o = null, d = null, S = null, x = null;
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
                a.hasOwnProperty(A) || zt(t, e, A, null, a, M);
            }
        }
        for (var _ in a) {
          var A = a[_];
          if (M = l[_], a.hasOwnProperty(_) && (A != null || M != null))
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
                x = A;
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
                A !== M && zt(
                  t,
                  e,
                  _,
                  A,
                  a,
                  M
                );
            }
        }
        Bi(
          t,
          i,
          o,
          d,
          S,
          x,
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
        e = o, l = i, a = A, _ != null ? za(t, !!l, _, !1) : !!a != !!l && (e != null ? za(t, !!l, e, !0) : za(t, !!l, l ? [] : "", !1));
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
        As(t, _, A);
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
        if (Li(e)) {
          for (var xt in l)
            _ = l[xt], l.hasOwnProperty(xt) && _ !== void 0 && !a.hasOwnProperty(xt) && xf(
              t,
              e,
              xt,
              void 0,
              a,
              _
            );
          for (x in a)
            _ = a[x], A = l[x], !a.hasOwnProperty(x) || _ === A || _ === void 0 && A === void 0 || xf(
              t,
              e,
              x,
              _,
              a,
              A
            );
          return;
        }
    }
    for (var v in l)
      _ = l[v], l.hasOwnProperty(v) && _ != null && !a.hasOwnProperty(v) && zt(t, e, v, null, a, _);
    for (M in a)
      _ = a[M], A = l[M], !a.hasOwnProperty(M) || _ === A || _ == null && A == null || zt(t, e, M, _, a, A);
  }
  function Nd(t) {
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
        var u = l[a], n = u.transferSize, i = u.initiatorType, o = u.duration;
        if (n && o && Nd(i)) {
          for (i = 0, o = u.responseEnd, a += 1; a < l.length; a++) {
            var d = l[a], S = d.startTime;
            if (S > o) break;
            var x = d.transferSize, M = d.initiatorType;
            x && Nd(M) && (d = d.responseEnd, i += x * (d < o ? 1 : (o - S) / (d - S)));
          }
          if (--a, e += 8 * (n + i) / (u.duration / 1e3), t++, 10 < t) break;
        }
      }
      if (0 < t) return e / t / 1e6;
    }
    return navigator.connection && (t = navigator.connection.downlink, typeof t == "number") ? t : 5;
  }
  var qf = null, Nf = null;
  function hi(t) {
    return t.nodeType === 9 ? t : t.ownerDocument;
  }
  function Od(t) {
    switch (t) {
      case "http://www.w3.org/2000/svg":
        return 1;
      case "http://www.w3.org/1998/Math/MathML":
        return 2;
      default:
        return 0;
    }
  }
  function Md(t, e) {
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
  function Of(t, e) {
    return t === "textarea" || t === "noscript" || typeof e.children == "string" || typeof e.children == "number" || typeof e.children == "bigint" || typeof e.dangerouslySetInnerHTML == "object" && e.dangerouslySetInnerHTML !== null && e.dangerouslySetInnerHTML.__html != null;
  }
  var Mf = null;
  function vh() {
    var t = window.event;
    return t && t.type === "popstate" ? t === Mf ? !1 : (Mf = t, !0) : (Mf = null, !1);
  }
  var Dd = typeof setTimeout == "function" ? setTimeout : void 0, gh = typeof clearTimeout == "function" ? clearTimeout : void 0, Ud = typeof Promise == "function" ? Promise : void 0, ph = typeof queueMicrotask == "function" ? queueMicrotask : typeof Ud < "u" ? function(t) {
    return Ud.resolve(null).then(t).catch(bh);
  } : Dd;
  function bh(t) {
    setTimeout(function() {
      throw t;
    });
  }
  function Gl(t) {
    return t === "head";
  }
  function Cd(t, e) {
    var l = e, a = 0;
    do {
      var u = l.nextSibling;
      if (t.removeChild(l), u && u.nodeType === 8)
        if (l = u.data, l === "/$" || l === "/&") {
          if (a === 0) {
            t.removeChild(u), au(e);
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
    au(e);
  }
  function jd(t, e) {
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
  function Df(t) {
    var e = t.firstChild;
    for (e && e.nodeType === 10 && (e = e.nextSibling); e; ) {
      var l = e;
      switch (e = e.nextSibling, l.nodeName) {
        case "HTML":
        case "HEAD":
        case "BODY":
          Df(l), Ri(l);
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
      if (t = He(t.nextSibling), t === null) break;
    }
    return null;
  }
  function _h(t, e, l) {
    if (e === "") return null;
    for (; t.nodeType !== 3; )
      if ((t.nodeType !== 1 || t.nodeName !== "INPUT" || t.type !== "hidden") && !l || (t = He(t.nextSibling), t === null)) return null;
    return t;
  }
  function Rd(t, e) {
    for (; t.nodeType !== 8; )
      if ((t.nodeType !== 1 || t.nodeName !== "INPUT" || t.type !== "hidden") && !e || (t = He(t.nextSibling), t === null)) return null;
    return t;
  }
  function Uf(t) {
    return t.data === "$?" || t.data === "$~";
  }
  function Cf(t) {
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
  function He(t) {
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
  var jf = null;
  function Hd(t) {
    t = t.nextSibling;
    for (var e = 0; t; ) {
      if (t.nodeType === 8) {
        var l = t.data;
        if (l === "/$" || l === "/&") {
          if (e === 0)
            return He(t.nextSibling);
          e--;
        } else
          l !== "$" && l !== "$!" && l !== "$?" && l !== "$~" && l !== "&" || e++;
      }
      t = t.nextSibling;
    }
    return null;
  }
  function Bd(t) {
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
  function Yd(t, e, l) {
    switch (e = hi(l), t) {
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
    Ri(t);
  }
  var Be = /* @__PURE__ */ new Map(), Ld = /* @__PURE__ */ new Set();
  function vi(t) {
    return typeof t.getRootNode == "function" ? t.getRootNode() : t.nodeType === 9 ? t : t.ownerDocument;
  }
  var vl = B.d;
  B.d = {
    f: Ah,
    r: Th,
    D: zh,
    C: xh,
    L: qh,
    m: Nh,
    X: Mh,
    S: Oh,
    M: Dh
  };
  function Ah() {
    var t = vl.f(), e = ci();
    return t || e;
  }
  function Th(t) {
    var e = Ea(t);
    e !== null && e.tag === 5 && e.type === "form" ? eo(e) : vl.r(t);
  }
  var tu = typeof document > "u" ? null : document;
  function Gd(t, e, l) {
    var a = tu;
    if (a && typeof e == "string" && e) {
      var u = Oe(e);
      u = 'link[rel="' + t + '"][href="' + u + '"]', typeof l == "string" && (u += '[crossorigin="' + l + '"]'), Ld.has(u) || (Ld.add(u), t = { rel: t, crossOrigin: l, href: e }, a.querySelector(u) === null && (e = a.createElement("link"), te(e, "link", t), Kt(e), a.head.appendChild(e)));
    }
  }
  function zh(t) {
    vl.D(t), Gd("dns-prefetch", t, null);
  }
  function xh(t, e) {
    vl.C(t, e), Gd("preconnect", t, e);
  }
  function qh(t, e, l) {
    vl.L(t, e, l);
    var a = tu;
    if (a && t && e) {
      var u = 'link[rel="preload"][as="' + Oe(e) + '"]';
      e === "image" && l && l.imageSrcSet ? (u += '[imagesrcset="' + Oe(
        l.imageSrcSet
      ) + '"]', typeof l.imageSizes == "string" && (u += '[imagesizes="' + Oe(
        l.imageSizes
      ) + '"]')) : u += '[href="' + Oe(t) + '"]';
      var n = u;
      switch (e) {
        case "style":
          n = eu(t);
          break;
        case "script":
          n = lu(t);
      }
      Be.has(n) || (t = E(
        {
          rel: "preload",
          href: e === "image" && l && l.imageSrcSet ? void 0 : t,
          as: e
        },
        l
      ), Be.set(n, t), a.querySelector(u) !== null || e === "style" && a.querySelector(Iu(n)) || e === "script" && a.querySelector(Pu(n)) || (e = a.createElement("link"), te(e, "link", t), Kt(e), a.head.appendChild(e)));
    }
  }
  function Nh(t, e) {
    vl.m(t, e);
    var l = tu;
    if (l && t) {
      var a = e && typeof e.as == "string" ? e.as : "script", u = 'link[rel="modulepreload"][as="' + Oe(a) + '"][href="' + Oe(t) + '"]', n = u;
      switch (a) {
        case "audioworklet":
        case "paintworklet":
        case "serviceworker":
        case "sharedworker":
        case "worker":
        case "script":
          n = lu(t);
      }
      if (!Be.has(n) && (t = E({ rel: "modulepreload", href: t }, e), Be.set(n, t), l.querySelector(u) === null)) {
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
        a = l.createElement("link"), te(a, "link", t), Kt(a), l.head.appendChild(a);
      }
    }
  }
  function Oh(t, e, l) {
    vl.S(t, e, l);
    var a = tu;
    if (a && t) {
      var u = Aa(a).hoistableStyles, n = eu(t);
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
          ), (l = Be.get(n)) && Rf(t, l);
          var d = i = a.createElement("link");
          Kt(d), te(d, "link", t), d._p = new Promise(function(S, x) {
            d.onload = S, d.onerror = x;
          }), d.addEventListener("load", function() {
            o.loading |= 1;
          }), d.addEventListener("error", function() {
            o.loading |= 2;
          }), o.loading |= 4, gi(i, e, a);
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
  function Mh(t, e) {
    vl.X(t, e);
    var l = tu;
    if (l && t) {
      var a = Aa(l).hoistableScripts, u = lu(t), n = a.get(u);
      n || (n = l.querySelector(Pu(u)), n || (t = E({ src: t, async: !0 }, e), (e = Be.get(u)) && Hf(t, e), n = l.createElement("script"), Kt(n), te(n, "link", t), l.head.appendChild(n)), n = {
        type: "script",
        instance: n,
        count: 1,
        state: null
      }, a.set(u, n));
    }
  }
  function Dh(t, e) {
    vl.M(t, e);
    var l = tu;
    if (l && t) {
      var a = Aa(l).hoistableScripts, u = lu(t), n = a.get(u);
      n || (n = l.querySelector(Pu(u)), n || (t = E({ src: t, async: !0, type: "module" }, e), (e = Be.get(u)) && Hf(t, e), n = l.createElement("script"), Kt(n), te(n, "link", t), l.head.appendChild(n)), n = {
        type: "script",
        instance: n,
        count: 1,
        state: null
      }, a.set(u, n));
    }
  }
  function Qd(t, e, l, a) {
    var u = (u = at.current) ? vi(u) : null;
    if (!u) throw Error(s(446));
    switch (t) {
      case "meta":
      case "title":
        return null;
      case "style":
        return typeof l.precedence == "string" && typeof l.href == "string" ? (e = eu(l.href), l = Aa(
          u
        ).hoistableStyles, a = l.get(e), a || (a = {
          type: "style",
          instance: null,
          count: 0,
          state: null
        }, l.set(e, a)), a) : { type: "void", instance: null, count: 0, state: null };
      case "link":
        if (l.rel === "stylesheet" && typeof l.href == "string" && typeof l.precedence == "string") {
          t = eu(l.href);
          var n = Aa(
            u
          ).hoistableStyles, i = n.get(t);
          if (i || (u = u.ownerDocument || u, i = {
            type: "stylesheet",
            instance: null,
            count: 0,
            state: { loading: 0, preload: null }
          }, n.set(t, i), (n = u.querySelector(
            Iu(t)
          )) && !n._p && (i.instance = n, i.state.loading = 5), Be.has(t) || (l = {
            rel: "preload",
            as: "style",
            href: l.href,
            crossOrigin: l.crossOrigin,
            integrity: l.integrity,
            media: l.media,
            hrefLang: l.hrefLang,
            referrerPolicy: l.referrerPolicy
          }, Be.set(t, l), n || Uh(
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
        return e = l.async, l = l.src, typeof l == "string" && e && typeof e != "function" && typeof e != "symbol" ? (e = lu(l), l = Aa(
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
  function eu(t) {
    return 'href="' + Oe(t) + '"';
  }
  function Iu(t) {
    return 'link[rel="stylesheet"][' + t + "]";
  }
  function Xd(t) {
    return E({}, t, {
      "data-precedence": t.precedence,
      precedence: null
    });
  }
  function Uh(t, e, l, a) {
    t.querySelector('link[rel="preload"][as="style"][' + e + "]") ? a.loading = 1 : (e = t.createElement("link"), a.preload = e, e.addEventListener("load", function() {
      return a.loading |= 1;
    }), e.addEventListener("error", function() {
      return a.loading |= 2;
    }), te(e, "link", l), Kt(e), t.head.appendChild(e));
  }
  function lu(t) {
    return '[src="' + Oe(t) + '"]';
  }
  function Pu(t) {
    return "script[async]" + t;
  }
  function Zd(t, e, l) {
    if (e.count++, e.instance === null)
      switch (e.type) {
        case "style":
          var a = t.querySelector(
            'style[data-href~="' + Oe(l.href) + '"]'
          );
          if (a)
            return e.instance = a, Kt(a), a;
          var u = E({}, l, {
            "data-href": l.href,
            "data-precedence": l.precedence,
            href: null,
            precedence: null
          });
          return a = (t.ownerDocument || t).createElement(
            "style"
          ), Kt(a), te(a, "style", u), gi(a, l.precedence, t), e.instance = a;
        case "stylesheet":
          u = eu(l.href);
          var n = t.querySelector(
            Iu(u)
          );
          if (n)
            return e.state.loading |= 4, e.instance = n, Kt(n), n;
          a = Xd(l), (u = Be.get(u)) && Rf(a, u), n = (t.ownerDocument || t).createElement("link"), Kt(n);
          var i = n;
          return i._p = new Promise(function(o, d) {
            i.onload = o, i.onerror = d;
          }), te(n, "link", a), e.state.loading |= 4, gi(n, l.precedence, t), e.instance = n;
        case "script":
          return n = lu(l.src), (u = t.querySelector(
            Pu(n)
          )) ? (e.instance = u, Kt(u), u) : (a = l, (u = Be.get(n)) && (a = E({}, l), Hf(a, u)), t = t.ownerDocument || t, u = t.createElement("script"), Kt(u), te(u, "link", a), t.head.appendChild(u), e.instance = u);
        case "void":
          return null;
        default:
          throw Error(s(443, e.type));
      }
    else
      e.type === "stylesheet" && (e.state.loading & 4) === 0 && (a = e.instance, e.state.loading |= 4, gi(a, l.precedence, t));
    return e.instance;
  }
  function gi(t, e, l) {
    for (var a = l.querySelectorAll(
      'link[rel="stylesheet"][data-precedence],style[data-precedence]'
    ), u = a.length ? a[a.length - 1] : null, n = u, i = 0; i < a.length; i++) {
      var o = a[i];
      if (o.dataset.precedence === e) n = o;
      else if (n !== u) break;
    }
    n ? n.parentNode.insertBefore(t, n.nextSibling) : (e = l.nodeType === 9 ? l.head : l, e.insertBefore(t, e.firstChild));
  }
  function Rf(t, e) {
    t.crossOrigin == null && (t.crossOrigin = e.crossOrigin), t.referrerPolicy == null && (t.referrerPolicy = e.referrerPolicy), t.title == null && (t.title = e.title);
  }
  function Hf(t, e) {
    t.crossOrigin == null && (t.crossOrigin = e.crossOrigin), t.referrerPolicy == null && (t.referrerPolicy = e.referrerPolicy), t.integrity == null && (t.integrity = e.integrity);
  }
  var pi = null;
  function Vd(t, e, l) {
    if (pi === null) {
      var a = /* @__PURE__ */ new Map(), u = pi = /* @__PURE__ */ new Map();
      u.set(l, a);
    } else
      u = pi, a = u.get(l), a || (a = /* @__PURE__ */ new Map(), u.set(l, a));
    if (a.has(t)) return a;
    for (a.set(t, null), l = l.getElementsByTagName(t), u = 0; u < l.length; u++) {
      var n = l[u];
      if (!(n[hu] || n[Wt] || t === "link" && n.getAttribute("rel") === "stylesheet") && n.namespaceURI !== "http://www.w3.org/2000/svg") {
        var i = n.getAttribute(e) || "";
        i = t + i;
        var o = a.get(i);
        o ? o.push(n) : a.set(i, [n]);
      }
    }
    return a;
  }
  function Jd(t, e, l) {
    t = t.ownerDocument || t, t.head.insertBefore(
      l,
      e === "title" ? t.querySelector("head > title") : null
    );
  }
  function Ch(t, e, l) {
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
  function jh(t, e, l, a) {
    if (l.type === "stylesheet" && (typeof a.media != "string" || matchMedia(a.media).matches !== !1) && (l.state.loading & 4) === 0) {
      if (l.instance === null) {
        var u = eu(a.href), n = e.querySelector(
          Iu(u)
        );
        if (n) {
          e = n._p, e !== null && typeof e == "object" && typeof e.then == "function" && (t.count++, t = bi.bind(t), e.then(t, t)), l.state.loading |= 4, l.instance = n, Kt(n);
          return;
        }
        n = e.ownerDocument || e, a = Xd(a), (u = Be.get(u)) && Rf(a, u), n = n.createElement("link"), Kt(n);
        var i = n;
        i._p = new Promise(function(o, d) {
          i.onload = o, i.onerror = d;
        }), te(n, "link", a), l.instance = n;
      }
      t.stylesheets === null && (t.stylesheets = /* @__PURE__ */ new Map()), t.stylesheets.set(l, e), (e = l.state.preload) && (l.state.loading & 3) === 0 && (t.count++, l = bi.bind(t), e.addEventListener("load", l), e.addEventListener("error", l));
    }
  }
  var Bf = 0;
  function Rh(t, e) {
    return t.stylesheets && t.count === 0 && _i(t, t.stylesheets), 0 < t.count || 0 < t.imgCount ? function(l) {
      var a = setTimeout(function() {
        if (t.stylesheets && _i(t, t.stylesheets), t.unsuspend) {
          var n = t.unsuspend;
          t.unsuspend = null, n();
        }
      }, 6e4 + e);
      0 < t.imgBytes && Bf === 0 && (Bf = 62500 * hh());
      var u = setTimeout(
        function() {
          if (t.waitingForImages = !1, t.count === 0 && (t.stylesheets && _i(t, t.stylesheets), t.unsuspend)) {
            var n = t.unsuspend;
            t.unsuspend = null, n();
          }
        },
        (t.imgBytes > Bf ? 50 : 800) + e
      );
      return t.unsuspend = l, function() {
        t.unsuspend = null, clearTimeout(a), clearTimeout(u);
      };
    } : null;
  }
  function bi() {
    if (this.count--, this.count === 0 && (this.imgCount === 0 || !this.waitingForImages)) {
      if (this.stylesheets) _i(this, this.stylesheets);
      else if (this.unsuspend) {
        var t = this.unsuspend;
        this.unsuspend = null, t();
      }
    }
  }
  var Si = null;
  function _i(t, e) {
    t.stylesheets = null, t.unsuspend !== null && (t.count++, Si = /* @__PURE__ */ new Map(), e.forEach(Hh, t), Si = null, bi.call(t));
  }
  function Hh(t, e) {
    if (!(e.state.loading & 4)) {
      var l = Si.get(t);
      if (l) var a = l.get(null);
      else {
        l = /* @__PURE__ */ new Map(), Si.set(t, l);
        for (var u = t.querySelectorAll(
          "link[data-precedence],style[data-precedence]"
        ), n = 0; n < u.length; n++) {
          var i = u[n];
          (i.nodeName === "LINK" || i.getAttribute("media") !== "not all") && (l.set(i.dataset.precedence, i), a = i);
        }
        a && l.set(null, a);
      }
      u = e.instance, i = u.getAttribute("data-precedence"), n = l.get(i) || a, n === a && l.set(null, u), l.set(i, u), this.count++, a = bi.bind(this), u.addEventListener("load", a), u.addEventListener("error", a), n ? n.parentNode.insertBefore(u, n.nextSibling) : (t = t.nodeType === 9 ? t.head : t, t.insertBefore(u, t.firstChild)), e.state.loading |= 4;
    }
  }
  var tn = {
    $$typeof: et,
    Provider: null,
    Consumer: null,
    _currentValue: k,
    _currentValue2: k,
    _threadCount: 0
  };
  function Bh(t, e, l, a, u, n, i, o, d) {
    this.tag = 1, this.containerInfo = t, this.pingCache = this.current = this.pendingChildren = null, this.timeoutHandle = -1, this.callbackNode = this.next = this.pendingContext = this.context = this.cancelPendingCommit = null, this.callbackPriority = 0, this.expirationTimes = Di(-1), this.entangledLanes = this.shellSuspendCounter = this.errorRecoveryDisabledLanes = this.expiredLanes = this.warmLanes = this.pingedLanes = this.suspendedLanes = this.pendingLanes = 0, this.entanglements = Di(0), this.hiddenUpdates = Di(null), this.identifierPrefix = a, this.onUncaughtError = u, this.onCaughtError = n, this.onRecoverableError = i, this.pooledCache = null, this.pooledCacheLanes = 0, this.formState = d, this.incompleteTransitions = /* @__PURE__ */ new Map();
  }
  function wd(t, e, l, a, u, n, i, o, d, S, x, M) {
    return t = new Bh(
      t,
      e,
      l,
      i,
      d,
      S,
      x,
      M,
      o
    ), e = 1, n === !0 && (e |= 24), n = Se(3, null, null, e), t.current = n, n.stateNode = t, e = hc(), e.refCount++, t.pooledCache = e, e.refCount++, n.memoizedState = {
      element: a,
      isDehydrated: l,
      cache: e
    }, bc(n), t;
  }
  function kd(t) {
    return t ? (t = Ca, t) : Ca;
  }
  function $d(t, e, l, a, u, n) {
    u = kd(u), a.context === null ? a.context = u : a.pendingContext = u, a = Ol(e), a.payload = { element: l }, n = n === void 0 ? null : n, n !== null && (a.callback = n), l = Ml(t, a, e), l !== null && (ye(l, t, e), Uu(l, t, e));
  }
  function Wd(t, e) {
    if (t = t.memoizedState, t !== null && t.dehydrated !== null) {
      var l = t.retryLane;
      t.retryLane = l !== 0 && l < e ? l : e;
    }
  }
  function Yf(t, e) {
    Wd(t, e), (t = t.alternate) && Wd(t, e);
  }
  function Fd(t) {
    if (t.tag === 13 || t.tag === 31) {
      var e = aa(t, 67108864);
      e !== null && ye(e, t, 67108864), Yf(t, 67108864);
    }
  }
  function Id(t) {
    if (t.tag === 13 || t.tag === 31) {
      var e = ze();
      e = Ui(e);
      var l = aa(t, e);
      l !== null && ye(l, t, e), Yf(t, e);
    }
  }
  var Ei = !0;
  function Yh(t, e, l, a) {
    var u = z.T;
    z.T = null;
    var n = B.p;
    try {
      B.p = 2, Lf(t, e, l, a);
    } finally {
      B.p = n, z.T = u;
    }
  }
  function Lh(t, e, l, a) {
    var u = z.T;
    z.T = null;
    var n = B.p;
    try {
      B.p = 8, Lf(t, e, l, a);
    } finally {
      B.p = n, z.T = u;
    }
  }
  function Lf(t, e, l, a) {
    if (Ei) {
      var u = Gf(a);
      if (u === null)
        zf(
          t,
          e,
          a,
          Ai,
          l
        ), ty(t, a);
      else if (Qh(
        u,
        t,
        e,
        l,
        a
      ))
        a.stopPropagation();
      else if (ty(t, a), e & 4 && -1 < Gh.indexOf(t)) {
        for (; u !== null; ) {
          var n = Ea(u);
          if (n !== null)
            switch (n.tag) {
              case 3:
                if (n = n.stateNode, n.current.memoizedState.isDehydrated) {
                  var i = Il(n.pendingLanes);
                  if (i !== 0) {
                    var o = n;
                    for (o.pendingLanes |= 2, o.entangledLanes |= 2; i; ) {
                      var d = 1 << 31 - pe(i);
                      o.entanglements[1] |= d, i &= ~d;
                    }
                    $e(n), (bt & 6) === 0 && (ni = ht() + 500, ku(0));
                  }
                }
                break;
              case 31:
              case 13:
                o = aa(n, 2), o !== null && ye(o, n, 2), ci(), Yf(n, 2);
            }
          if (n = Gf(a), n === null && zf(
            t,
            e,
            a,
            Ai,
            l
          ), n === u) break;
          u = n;
        }
        u !== null && a.stopPropagation();
      } else
        zf(
          t,
          e,
          a,
          null,
          l
        );
    }
  }
  function Gf(t) {
    return t = Qi(t), Qf(t);
  }
  var Ai = null;
  function Qf(t) {
    if (Ai = null, t = _a(t), t !== null) {
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
    return Ai = t, null;
  }
  function Pd(t) {
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
        switch (Jt()) {
          case $t:
            return 2;
          case ou:
            return 8;
          case dn:
          case xy:
            return 32;
          case fs:
            return 268435456;
          default:
            return 32;
        }
      default:
        return 32;
    }
  }
  var Xf = !1, Ql = null, Xl = null, Zl = null, en = /* @__PURE__ */ new Map(), ln = /* @__PURE__ */ new Map(), Vl = [], Gh = "mousedown mouseup touchcancel touchend touchstart auxclick dblclick pointercancel pointerdown pointerup dragend dragstart drop compositionend compositionstart keydown keypress keyup input textInput copy cut paste click change contextmenu reset".split(
    " "
  );
  function ty(t, e) {
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
    }, e !== null && (e = Ea(e), e !== null && Fd(e)), t) : (t.eventSystemFlags |= a, e = t.targetContainers, u !== null && e.indexOf(u) === -1 && e.push(u), t);
  }
  function Qh(t, e, l, a, u) {
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
  function ey(t) {
    var e = _a(t.target);
    if (e !== null) {
      var l = g(e);
      if (l !== null) {
        if (e = l.tag, e === 13) {
          if (e = N(l), e !== null) {
            t.blockedOn = e, ms(t.priority, function() {
              Id(l);
            });
            return;
          }
        } else if (e === 31) {
          if (e = D(l), e !== null) {
            t.blockedOn = e, ms(t.priority, function() {
              Id(l);
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
  function Ti(t) {
    if (t.blockedOn !== null) return !1;
    for (var e = t.targetContainers; 0 < e.length; ) {
      var l = Gf(t.nativeEvent);
      if (l === null) {
        l = t.nativeEvent;
        var a = new l.constructor(
          l.type,
          l
        );
        Gi = a, l.target.dispatchEvent(a), Gi = null;
      } else
        return e = Ea(l), e !== null && Fd(e), t.blockedOn = l, !1;
      e.shift();
    }
    return !0;
  }
  function ly(t, e, l) {
    Ti(t) && l.delete(e);
  }
  function Xh() {
    Xf = !1, Ql !== null && Ti(Ql) && (Ql = null), Xl !== null && Ti(Xl) && (Xl = null), Zl !== null && Ti(Zl) && (Zl = null), en.forEach(ly), ln.forEach(ly);
  }
  function zi(t, e) {
    t.blockedOn === e && (t.blockedOn = null, Xf || (Xf = !0, c.unstable_scheduleCallback(
      c.unstable_NormalPriority,
      Xh
    )));
  }
  var xi = null;
  function ay(t) {
    xi !== t && (xi = t, c.unstable_scheduleCallback(
      c.unstable_NormalPriority,
      function() {
        xi === t && (xi = null);
        for (var e = 0; e < t.length; e += 3) {
          var l = t[e], a = t[e + 1], u = t[e + 2];
          if (typeof a != "function") {
            if (Qf(a || l) === null)
              continue;
            break;
          }
          var n = Ea(l);
          n !== null && (t.splice(e, 3), e -= 3, Lc(
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
  function au(t) {
    function e(d) {
      return zi(d, t);
    }
    Ql !== null && zi(Ql, t), Xl !== null && zi(Xl, t), Zl !== null && zi(Zl, t), en.forEach(e), ln.forEach(e);
    for (var l = 0; l < Vl.length; l++) {
      var a = Vl[l];
      a.blockedOn === t && (a.blockedOn = null);
    }
    for (; 0 < Vl.length && (l = Vl[0], l.blockedOn === null); )
      ey(l), l.blockedOn === null && Vl.shift();
    if (l = (t.ownerDocument || t).$$reactFormReplay, l != null)
      for (a = 0; a < l.length; a += 3) {
        var u = l[a], n = l[a + 1], i = u[ce] || null;
        if (typeof n == "function")
          i || ay(l);
        else if (i) {
          var o = null;
          if (n && n.hasAttribute("formAction")) {
            if (u = n, i = n[ce] || null)
              o = i.formAction;
            else if (Qf(u) !== null) continue;
          } else o = i.action;
          typeof o == "function" ? l[a + 1] = o : (l.splice(a, 3), a -= 3), ay(l);
        }
      }
  }
  function uy() {
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
  function Zf(t) {
    this._internalRoot = t;
  }
  qi.prototype.render = Zf.prototype.render = function(t) {
    var e = this._internalRoot;
    if (e === null) throw Error(s(409));
    var l = e.current, a = ze();
    $d(l, a, t, e, null, null);
  }, qi.prototype.unmount = Zf.prototype.unmount = function() {
    var t = this._internalRoot;
    if (t !== null) {
      this._internalRoot = null;
      var e = t.containerInfo;
      $d(t.current, 2, null, t, null, null), ci(), e[Sa] = null;
    }
  };
  function qi(t) {
    this._internalRoot = t;
  }
  qi.prototype.unstable_scheduleHydration = function(t) {
    if (t) {
      var e = ys();
      t = { blockedOn: null, target: t, priority: e };
      for (var l = 0; l < Vl.length && e !== 0 && e < Vl[l].priority; l++) ;
      Vl.splice(l, 0, t), l === 0 && ey(t);
    }
  };
  var ny = f.version;
  if (ny !== "19.2.5")
    throw Error(
      s(
        527,
        ny,
        "19.2.5"
      )
    );
  B.findDOMNode = function(t) {
    var e = t._reactInternals;
    if (e === void 0)
      throw typeof t.render == "function" ? Error(s(188)) : (t = Object.keys(t).join(","), Error(s(268, t)));
    return t = p(e), t = t !== null ? j(t) : null, t = t === null ? null : t.stateNode, t;
  };
  var Zh = {
    bundleType: 0,
    version: "19.2.5",
    rendererPackageName: "react-dom",
    currentDispatcherRef: z,
    reconcilerVersion: "19.2.5"
  };
  if (typeof __REACT_DEVTOOLS_GLOBAL_HOOK__ < "u") {
    var Ni = __REACT_DEVTOOLS_GLOBAL_HOOK__;
    if (!Ni.isDisabled && Ni.supportsFiber)
      try {
        du = Ni.inject(
          Zh
        ), ge = Ni;
      } catch {
      }
  }
  return un.createRoot = function(t, e) {
    if (!h(t)) throw Error(s(299));
    var l = !1, a = "", u = oo, n = yo, i = mo;
    return e != null && (e.unstable_strictMode === !0 && (l = !0), e.identifierPrefix !== void 0 && (a = e.identifierPrefix), e.onUncaughtError !== void 0 && (u = e.onUncaughtError), e.onCaughtError !== void 0 && (n = e.onCaughtError), e.onRecoverableError !== void 0 && (i = e.onRecoverableError)), e = wd(
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
      uy
    ), t[Sa] = e.current, Tf(t), new Zf(e);
  }, un.hydrateRoot = function(t, e, l) {
    if (!h(t)) throw Error(s(299));
    var a = !1, u = "", n = oo, i = yo, o = mo, d = null;
    return l != null && (l.unstable_strictMode === !0 && (a = !0), l.identifierPrefix !== void 0 && (u = l.identifierPrefix), l.onUncaughtError !== void 0 && (n = l.onUncaughtError), l.onCaughtError !== void 0 && (i = l.onCaughtError), l.onRecoverableError !== void 0 && (o = l.onRecoverableError), l.formState !== void 0 && (d = l.formState)), e = wd(
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
      uy
    ), e.context = kd(null), l = e.current, a = ze(), a = Ui(a), u = Ol(a), u.callback = null, Ml(l, u, a), l = a, e.current.lanes = l, mu(e, l), $e(e), t[Sa] = e.current, Tf(t), new qi(e);
  }, un.version = "19.2.5", un;
}
var my;
function Ph() {
  if (my) return Kf.exports;
  my = 1;
  function c() {
    if (!(typeof __REACT_DEVTOOLS_GLOBAL_HOOK__ > "u" || typeof __REACT_DEVTOOLS_GLOBAL_HOOK__.checkDCE != "function"))
      try {
        __REACT_DEVTOOLS_GLOBAL_HOOK__.checkDCE(c);
      } catch (f) {
        console.error(f);
      }
  }
  return c(), Kf.exports = Ih(), Kf.exports;
}
var tv = Ph(), Wf = { exports: {} }, nn = {};
/**
 * @license React
 * react-jsx-runtime.production.js
 *
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 */
var hy;
function ev() {
  if (hy) return nn;
  hy = 1;
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
var vy;
function lv() {
  return vy || (vy = 1, Wf.exports = ev()), Wf.exports;
}
var T = lv();
function av(c) {
  return typeof c.questionId == "string";
}
function uv(c) {
  const f = c;
  return Array.isArray(f.all) || Array.isArray(f.any);
}
function nv(c) {
  return typeof c.expression == "string";
}
class We extends Error {
  constructor(r, s) {
    super(`Expression syntax error at column ${s}: ${r}`);
    gl(this, "position");
    this.position = s, this.name = "ExpressionSyntaxError";
  }
}
function Oi(c) {
  return c >= "0" && c <= "9";
}
function _y(c) {
  return c >= "a" && c <= "z" || c >= "A" && c <= "Z";
}
function iv(c) {
  return _y(c) || Oi(c);
}
function cv(c) {
  return c === " " || c === "	" || c === `
` || c === "\r" || c === "\f" || c === "\v";
}
function fv(c) {
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
    for (; !s() && cv(h()); )
      r++;
  }, D = (E) => {
    for (; !s() && Oi(h()); )
      r++;
    if (!s() && h() === ".")
      for (r++; !s() && Oi(h()); )
        r++;
    const C = c.substring(E, r), G = parseFloat(C);
    return { kind: "Number", text: C, literal: G, position: E };
  }, q = (E, C) => {
    r++;
    let G = "";
    for (; !s() && h() !== C; ) {
      const J = h();
      if (J === "\\" && r + 1 < c.length) {
        const K = h(1), w = {
          n: `
`,
          t: "	",
          r: "\r",
          "\\": "\\",
          "'": "'",
          '"': '"'
        }[K];
        if (w === void 0)
          throw new We(`unknown escape '\\${K}'.`, r);
        G += w, r += 2;
      } else
        G += J, r++;
    }
    if (s())
      throw new We("unterminated string literal.", E);
    return r++, { kind: "String", text: G, literal: G, position: E };
  }, p = (E) => {
    for (; !s(); ) {
      const G = h();
      if (G === "_" || G === "-" || iv(G))
        r++;
      else
        break;
    }
    const C = c.substring(E, r);
    return C === "true" ? { kind: "True", text: C, literal: !0, position: E } : C === "false" ? { kind: "False", text: C, literal: !1, position: E } : C === "null" ? { kind: "Null", text: C, literal: null, position: E } : { kind: "Identifier", text: C, literal: null, position: E };
  }, j = () => {
    const E = r, C = h();
    if (Oi(C))
      return D(E);
    if (C === "'" || C === '"')
      return q(E, C);
    if (C === "_" || _y(C))
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
        throw new We("bare '=' is not a valid operator (use '==' or '===').", E);
      case "!":
        return g("!==") ? { kind: "StrictNotEq", text: "!==", literal: null, position: E } : g("!=") ? { kind: "NotEq", text: "!=", literal: null, position: E } : (r++, { kind: "Not", text: "!", literal: null, position: E });
      case "<":
        return g("<=") ? { kind: "LtEq", text: "<=", literal: null, position: E } : (r++, { kind: "Lt", text: "<", literal: null, position: E });
      case ">":
        return g(">=") ? { kind: "GtEq", text: ">=", literal: null, position: E } : (r++, { kind: "Gt", text: ">", literal: null, position: E });
      case "&":
        if (g("&&"))
          return { kind: "And", text: "&&", literal: null, position: E };
        throw new We("expected '&&'.", E);
      case "|":
        if (g("||"))
          return { kind: "Or", text: "||", literal: null, position: E };
        throw new We("expected '||'.", E);
    }
    throw new We(`unexpected character '${C}'.`, E);
  };
  for (; ; ) {
    if (N(), s())
      return f.push({ kind: "EndOfInput", text: "", literal: null, position: r }), f;
    f.push(j());
  }
}
function sv(c) {
  let f = 0;
  const r = () => {
    const Y = c[f];
    if (!Y)
      throw new We("unexpected end of tokens.", 0);
    return Y;
  }, s = () => {
    const Y = r();
    return Y.kind !== "EndOfInput" && f++, Y;
  }, h = (Y) => r().kind !== Y ? !1 : (s(), !0), g = (Y) => {
    const w = r();
    if (w.kind !== Y)
      throw new We(`expected ${Y}, got '${w.text}'.`, w.position);
    return s(), w;
  }, N = () => {
    let Y = D();
    for (; h("Or"); )
      Y = { kind: "BinaryOp", op: "||", left: Y, right: D() };
    return Y;
  }, D = () => {
    let Y = q();
    for (; h("And"); )
      Y = { kind: "BinaryOp", op: "&&", left: Y, right: q() };
    return Y;
  }, q = () => {
    let Y = p();
    for (; ; ) {
      const w = r().kind;
      let ct = null;
      if (w === "Eq" || w === "StrictEq" ? ct = "==" : (w === "NotEq" || w === "StrictNotEq") && (ct = "!="), ct === null)
        break;
      s(), Y = { kind: "BinaryOp", op: ct, left: Y, right: p() };
    }
    return Y;
  }, p = () => {
    let Y = j();
    for (; ; ) {
      const w = r().kind;
      let ct = null;
      if (w === "Lt" ? ct = "<" : w === "Gt" ? ct = ">" : w === "LtEq" ? ct = "<=" : w === "GtEq" && (ct = ">="), ct === null)
        break;
      s(), Y = { kind: "BinaryOp", op: ct, left: Y, right: j() };
    }
    return Y;
  }, j = () => h("Not") ? { kind: "UnaryOp", op: "!", operand: j() } : J(), E = () => {
    g("LBracket");
    const Y = [];
    if (r().kind !== "RBracket")
      for (Y.push(N()); h("Comma"); )
        Y.push(N());
    return g("RBracket"), { kind: "Array", items: Y };
  }, C = (Y) => {
    let w;
    if (h("Dot"))
      w = g("Identifier").text;
    else if (h("LBracket")) {
      const ct = g("String");
      g("RBracket"), w = ct.literal;
    } else
      throw new We("'answers' must be followed by .key or ['key'].", Y);
    return { kind: "AnswersAccess", key: w };
  }, G = () => {
    const Y = s();
    if (Y.text === "answers")
      return C(Y.position);
    g("LParen");
    const w = [];
    if (r().kind !== "RParen")
      for (w.push(N()); h("Comma"); )
        w.push(N());
    return g("RParen"), { kind: "Call", name: Y.text, args: w };
  }, J = () => {
    const Y = r();
    switch (Y.kind) {
      case "Number":
      case "String":
      case "True":
      case "False":
      case "Null":
        return s(), { kind: "Literal", value: Y.literal };
      case "LParen": {
        s();
        const w = N();
        return g("RParen"), w;
      }
      case "LBracket":
        return E();
      case "Identifier":
        return G();
      default:
        throw new We(`unexpected token '${Y.text}'.`, Y.position);
    }
  }, K = N();
  return g("EndOfInput"), K;
}
function bl(c) {
  return c === void 0 || c === null ? null : typeof c == "boolean" || typeof c == "number" || typeof c == "string" ? c : Array.isArray(c) ? c.map(bl) : null;
}
function ga(c, f) {
  const r = bl(c), s = bl(f);
  if (r === null || s === null)
    return r === null && s === null;
  if (typeof r == "number" && typeof s == "number" || typeof r == "string" && typeof s == "string" || typeof r == "boolean" && typeof s == "boolean")
    return r === s;
  if (Array.isArray(r) && Array.isArray(s)) {
    if (r.length !== s.length)
      return !1;
    for (let h = 0; h < r.length; h++)
      if (!ga(r[h], s[h]))
        return !1;
    return !0;
  }
  return !1;
}
function $l(c, f) {
  const r = bl(c), s = bl(f);
  if (typeof r == "number" && typeof s == "number" || typeof r == "string" && typeof s == "string")
    return r < s ? -1 : r > s ? 1 : 0;
  throw new Error("Comparison operators require two numbers or two strings.");
}
function iu(c) {
  const f = bl(c);
  return f === null ? !1 : typeof f == "boolean" ? f : typeof f == "number" ? f !== 0 : typeof f == "string" || Array.isArray(f) ? f.length > 0 : !0;
}
function Ye(c, f) {
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
      return c.items.map((r) => Ye(r, f));
  }
}
function rv(c, f) {
  const r = Ye(c.operand, f);
  if (c.op === "!")
    return !iu(r);
  throw new Error(`Unknown unary operator '${c.op}'.`);
}
function ov(c, f) {
  if (c.op === "&&") {
    const h = Ye(c.left, f);
    return iu(h) ? iu(Ye(c.right, f)) : !1;
  }
  if (c.op === "||") {
    const h = Ye(c.left, f);
    return iu(h) ? !0 : iu(Ye(c.right, f));
  }
  const r = Ye(c.left, f), s = Ye(c.right, f);
  switch (c.op) {
    case "==":
      return ga(r, s);
    case "!=":
      return !ga(r, s);
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
function dv(c, f) {
  switch (c.name) {
    case "has":
    case "isSet":
      return gy(c, f);
    case "isNotSet":
      return !gy(c, f);
    case "in":
      return yv(c, f);
    default:
      throw new Error(`Unknown function '${c.name}'.`);
  }
}
function gy(c, f) {
  if (c.args.length !== 1)
    throw new Error(`${c.name}() takes one argument.`);
  const r = c.args[0];
  if (!r)
    return !1;
  const s = Ye(r, f);
  return typeof s != "string" ? !1 : s in f && f[s] !== null && f[s] !== void 0;
}
function yv(c, f) {
  if (c.args.length !== 2)
    throw new Error("in() takes two arguments: in(value, [array]).");
  const r = c.args[0], s = c.args[1];
  if (!r || !s)
    return !1;
  const h = Ye(r, f), g = Ye(s, f);
  return Array.isArray(g) ? g.some((N) => ga(h, N)) : !1;
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
    return iu(Ye(r, f));
  } catch {
    return !1;
  }
}
function gv(c, f) {
  var r;
  if (!c.logic)
    return null;
  for (const s of c.logic)
    if (us(s.if, f))
      return ((r = s.then) == null ? void 0 : r.goto) ?? null;
  return null;
}
function us(c, f) {
  try {
    return av(c) ? bv(c, f) : uv(c) ? pv(c, f) : nv(c) ? vv(c.expression, f) : !1;
  } catch {
    return !1;
  }
}
function pv(c, f) {
  return c.all && c.all.length > 0 ? c.all.every((r) => us(r, f)) : c.any && c.any.length > 0 ? c.any.some((r) => us(r, f)) : !1;
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
      return ga(f, r);
    case "!=":
      return !ga(f, r);
    case ">":
      return $l(f, r) > 0;
    case ">=":
      return $l(f, r) >= 0;
    case "<":
      return $l(f, r) < 0;
    case "<=":
      return $l(f, r) <= 0;
    case "in":
      return py(r, f);
    case "notIn":
      return !py(r, f);
    default:
      return !1;
  }
}
function py(c, f) {
  return Array.isArray(c) ? c.some((r) => ga(f, r)) : !1;
}
function cn(c, f, r) {
  const s = new Set(c.screens.map((D) => D.id)), h = c.screens.find((D) => D.id === f);
  if (h && (!h.questions || h.questions.length === 0) && !h.nextScreen)
    return { kind: "end" };
  const g = gv(c, r);
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
function _v(c, f, r, s) {
  const h = new Set(f.screens.map((g) => g.id));
  return c.nextScreen && h.has(c.nextScreen) ? { kind: "screen", screenId: c.nextScreen } : cn(f, r, s);
}
const mt = (c, f, r, s) => ({ questionId: c, code: f, message: r, ...s ? { params: s } : {} }), me = (c) => typeof c == "number" && Number.isFinite(c);
function Ff(c) {
  if (!/^\d{4}-\d{2}-\d{2}$/.test(c))
    return null;
  const [f, r, s] = c.split("-").map((g) => Number.parseInt(g, 10)), h = new Date(Date.UTC(f, r - 1, s));
  return h.getUTCFullYear() !== f || h.getUTCMonth() !== r - 1 || h.getUTCDate() !== s ? null : h.getTime();
}
function If(c) {
  const f = Date.parse(c);
  return Number.isNaN(f) ? null : f;
}
function Ey(c, f) {
  const r = c.id, s = [];
  switch (c.type) {
    case "text": {
      if (typeof f != "string") {
        s.push(mt(r, "type", "Text answer must be a JSON string."));
        break;
      }
      const h = c.minLength, g = c.maxLength, N = c.pattern;
      if (me(h) && f.length < h && s.push(mt(r, "minLength", `Answer length ${f.length} is less than minLength ${h}.`, { n: h, actual: f.length })), me(g) && f.length > g && s.push(mt(r, "maxLength", `Answer length ${f.length} exceeds maxLength ${g}.`, { n: g, actual: f.length })), typeof N == "string" && N.length > 0)
        try {
          new RegExp(N).test(f) || s.push(mt(r, "pattern", "Answer does not match the required pattern."));
        } catch {
        }
      break;
    }
    case "paragraph": {
      if (typeof f != "string") {
        s.push(mt(r, "type", "Paragraph answer must be a JSON string."));
        break;
      }
      const h = c.minLength, g = c.maxLength;
      me(h) && f.length < h && s.push(mt(r, "minLength", `Answer length ${f.length} is less than minLength ${h}.`, { n: h, actual: f.length })), me(g) && f.length > g && s.push(mt(r, "maxLength", `Answer length ${f.length} exceeds maxLength ${g}.`, { n: g, actual: f.length }));
      break;
    }
    case "number": {
      if (!me(f)) {
        s.push(mt(r, "type", "Number answer must be a JSON number."));
        break;
      }
      const h = c.min, g = c.max;
      me(h) && f < h && s.push(mt(r, "min", `Answer ${f} is less than min ${h}.`, { n: h })), me(g) && f > g && s.push(mt(r, "max", `Answer ${f} exceeds max ${g}.`, { n: g }));
      break;
    }
    case "rating": {
      if (!me(f)) {
        s.push(mt(r, "type", "Rating answer must be a JSON number."));
        break;
      }
      const h = me(c.max) ? c.max : 0;
      (f < 0 || f > h) && s.push(mt(r, "range", `Rating ${f} is outside 0..${h}.`, { min: 0, max: h })), c.allowHalf !== !0 && f !== Math.floor(f) && s.push(mt(r, "halfNotAllowed", "Rating does not allow half values."));
      break;
    }
    case "nps": {
      if (!me(f) || !Number.isInteger(f)) {
        s.push(mt(r, "type", "NPS answer must be a JSON number."));
        break;
      }
      const h = me(c.min) ? c.min : 0, g = me(c.max) ? c.max : 10;
      (f < h || f > g) && s.push(mt(r, "range", `NPS answer ${f} is outside ${h}..${g}.`, { min: h, max: g }));
      break;
    }
    case "singleChoice":
    case "dropdown":
    case "navigationList": {
      if (typeof f != "string") {
        s.push(mt(r, "type", "Choice answer must be a JSON string (option id)."));
        break;
      }
      if (c.optionsSource != null)
        break;
      (Array.isArray(c.options) ? c.options : []).some((g) => g.id === f) || s.push(mt(r, "invalidOption", `'${f}' is not a valid option id for this question.`, { option: f }));
      break;
    }
    case "multiChoice": {
      if (!Array.isArray(f)) {
        s.push(mt(r, "type", "MultiChoice answer must be a JSON array of option ids."));
        break;
      }
      const h = Array.isArray(c.options) ? c.options : [], g = new Set(h.map((j) => j.id)), N = [];
      let D = !1;
      for (const j of f) {
        if (typeof j != "string") {
          s.push(mt(r, "type", "Each MultiChoice array entry must be a string option id.")), D = !0;
          break;
        }
        N.push(j);
      }
      if (D)
        break;
      if (c.optionsSource == null)
        for (const j of N)
          g.has(j) || s.push(mt(r, "invalidOption", `'${j}' is not a valid option id for this question.`, { option: j }));
      const q = c.minSelected, p = c.maxSelected;
      me(q) && N.length < q && s.push(mt(r, "minSelected", `At least ${q} option(s) must be selected.`, { n: q })), me(p) && N.length > p && s.push(mt(r, "maxSelected", `At most ${p} option(s) may be selected.`, { n: p }));
      break;
    }
    case "date": {
      if (typeof f != "string") {
        s.push(mt(r, "type", "Date answer must be a JSON string in yyyy-MM-dd format."));
        break;
      }
      const h = Ff(f);
      if (h === null) {
        s.push(mt(r, "invalidDate", `Date '${f}' is not yyyy-MM-dd.`));
        break;
      }
      const g = c.minDate, N = c.maxDate;
      if (typeof g == "string") {
        const D = Ff(g);
        D !== null && h < D && s.push(mt(r, "minDate", `Date ${f} is before minDate ${g}.`, { min: g }));
      }
      if (typeof N == "string") {
        const D = Ff(N);
        D !== null && h > D && s.push(mt(r, "maxDate", `Date ${f} is after maxDate ${N}.`, { max: N }));
      }
      break;
    }
    case "dateTime": {
      if (typeof f != "string") {
        s.push(mt(r, "type", "DateTime answer must be a JSON string in ISO 8601 format."));
        break;
      }
      const h = If(f);
      if (h === null) {
        s.push(mt(r, "invalidDateTime", `DateTime '${f}' is not valid ISO 8601.`));
        break;
      }
      const g = c.minDateTime, N = c.maxDateTime;
      if (typeof g == "string" && g.length > 0) {
        const D = If(g);
        D !== null && h < D && s.push(mt(r, "minDateTime", `DateTime is before minDateTime ${g}.`, { min: g }));
      }
      if (typeof N == "string" && N.length > 0) {
        const D = If(N);
        D !== null && h > D && s.push(mt(r, "maxDateTime", `DateTime is after maxDateTime ${N}.`, { max: N }));
      }
      break;
    }
    case "file": {
      (typeof f != "string" || f.length === 0) && s.push(mt(r, "empty", "Answer must be a non-empty file reference string."));
      break;
    }
    case "signature": {
      (typeof f != "string" || f.length === 0) && s.push(mt(r, "empty", "Answer must be a non-empty signature data url string."));
      break;
    }
    case "yesNo": {
      typeof f != "boolean" && s.push(mt(r, "type", "Yes/No answer must be a JSON boolean."));
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
    const N = f[g];
    N != null && r.push(...Ey(h, N));
  }
  return r;
}
function Pf(c, f) {
  let r = c;
  for (const s of f.split(".")) {
    if (r === null || typeof r != "object")
      return;
    r = r[s];
  }
  return r;
}
function Ay(c) {
  const f = new URL(c.url);
  for (const [r, s] of Object.entries(c.queryParams ?? {}))
    f.searchParams.set(r, s);
  return f.toString();
}
function Av(c, f) {
  const r = f.itemsPath ? Pf(c, f.itemsPath) : c;
  if (!Array.isArray(r))
    throw new Error(`optionsSource response is not an array${f.itemsPath ? ` at '${f.itemsPath}'` : ""}.`);
  const s = f.valuePath || "ID", h = f.labelPath || "Name", g = [];
  for (const N of r) {
    const D = Pf(N, s);
    if (D == null || D === "")
      continue;
    const q = Pf(N, h);
    g.push({
      id: String(D),
      label: q == null || q === "" ? String(D) : String(q)
    });
  }
  return g;
}
async function Tv(c, f) {
  const r = (f == null ? void 0 : f.fetchImpl) ?? fetch, s = {};
  f != null && f.locale && (s["Accept-Language"] = f.locale), Object.assign(s, c.headers ?? {});
  const h = await r(Ay(c), {
    headers: s,
    ...f != null && f.signal ? { signal: f.signal } : {}
  });
  if (!h.ok)
    throw new Error(`optionsSource fetch failed: HTTP ${h.status}.`);
  return Av(await h.json(), c);
}
class uu extends Error {
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
class by {
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
      throw new uu({
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
      throw new uu({
        status: f.status,
        code: "parse",
        message: `Empty body from ${f.url}`
      });
    try {
      return JSON.parse(r);
    } catch (s) {
      throw new uu({
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
      return new uu({
        status: f.status,
        code: h,
        message: `${r} ${s} → ${f.status}`
      });
    let N;
    try {
      N = JSON.parse(g);
    } catch {
      return new uu({
        status: f.status,
        code: h,
        message: `${r} ${s} → ${f.status}: ${g.slice(0, 200)}`,
        raw: g
      });
    }
    const D = N.Message ?? N.message, q = N.Errors ?? N.errors, p = Array.isArray(q) ? q.flatMap((j) => {
      const E = j.QuestionId ?? j.questionId, C = j.Message ?? j.message;
      return E && C ? [{ questionId: E, message: C }] : [];
    }) : void 0;
    return new uu({
      status: f.status,
      code: h,
      message: `${r} ${s} → ${f.status}${D ? ": " + D : ""}`,
      serverMessage: D,
      validationErrors: p && p.length > 0 ? p : void 0,
      raw: N
    });
  }
}
function Sy(c) {
  const f = c.trim().replace(/^#/, "");
  if (!/^[0-9a-fA-F]{3}$|^[0-9a-fA-F]{6}$|^[0-9a-fA-F]{8}$/.test(f)) return null;
  const r = f.length === 3 ? f.split("").map((s) => s + s).join("") : f.slice(0, 6);
  return [
    parseInt(r.slice(0, 2), 16),
    parseInt(r.slice(2, 4), 16),
    parseInt(r.slice(4, 6), 16)
  ];
}
function ts([c, f, r]) {
  const s = (h) => Math.max(0, Math.min(255, Math.round(h))).toString(16).padStart(2, "0");
  return `#${s(c)}${s(f)}${s(r)}`;
}
function zv(c, f) {
  return [c[0] * f, c[1] * f, c[2] * f];
}
function xv([c, f, r]) {
  const s = (h) => {
    const g = h / 255;
    return g <= 0.03928 ? g / 12.92 : Math.pow((g + 0.055) / 1.055, 2.4);
  };
  return 0.2126 * s(c) + 0.7152 * s(f) + 0.0722 * s(r);
}
function qv(c) {
  const f = {}, r = c != null && c.primaryColor ? Sy(c.primaryColor) : null;
  r && (f["--survey-primary"] = ts(r), f["--survey-primary-hover"] = ts(zv(r, 0.82)), f["--survey-primary-contrast"] = xv(r) > 0.45 ? "#111111" : "#ffffff");
  const s = c != null && c.secondaryColor ? Sy(c.secondaryColor) : null;
  return s && (f["--survey-accent"] = ts(s)), f;
}
const Ty = W.createContext(null), Nv = Ty.Provider;
function ue() {
  const c = W.useContext(Ty);
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
    no: "No",
    fileRecordedName: "Recorded file details: {name}"
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
    fileRecordedName: "تم تسجيل تفاصيل الملف: {name}"
  }
}, Mv = { en: zy, ar: Ov };
function Kl(c, f) {
  return f ? c.replace(
    /\{(\w+)\}/g,
    (r, s) => s in f ? String(f[s]) : r
  ) : c;
}
function Dv(c, f, r) {
  const s = { ...Mv, ...r ?? {} };
  return s[c] ?? (f ? s[f] : void 0) ?? s.en ?? zy;
}
const Uv = "adp-surveys", Cv = 1;
function jv(c = {}) {
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
  const N = (D, q) => {
    const p = {
      source: Uv,
      version: Cv,
      type: D,
      payload: q
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
function cs(c) {
  return `adp-surveys:resume:${c}`;
}
function Rv(c, f) {
  try {
    const r = c.getItem(cs(f));
    if (!r) return null;
    const s = JSON.parse(r);
    return !s || typeof s != "object" || !s.answers ? null : s;
  } catch {
    return null;
  }
}
function Hv(c, f, r) {
  try {
    const s = { ...r, savedAt: Date.now() };
    c.setItem(cs(f), JSON.stringify(s));
  } catch {
  }
}
function Bv(c, f) {
  try {
    c.removeItem(cs(f));
  } catch {
  }
}
const es = /* @__PURE__ */ new Map();
function Yv({
  question: c,
  Component: f
}) {
  const { locale: r, schema: s, ui: h } = ue(), g = c.optionsSource, N = `${r}|${Ay(g)}`, [D, q] = W.useState(() => {
    const K = es.get(N);
    return K ? { status: "ready", options: K } : { status: "loading" };
  }), [p, j] = W.useState(0);
  W.useEffect(() => {
    const K = es.get(N);
    if (K) {
      q({ status: "ready", options: K });
      return;
    }
    let Y = !1;
    return q({ status: "loading" }), Tv(g, { locale: r }).then((w) => {
      es.set(N, w), Y || q({ status: "ready", options: w });
    }).catch((w) => {
      Y || q({ status: "error", message: w.message ?? String(w) });
    }), () => {
      Y = !0;
    };
  }, [N, p]);
  const E = c.title, C = E ? /* @__PURE__ */ T.jsx("span", { className: "survey-question__label", children: P(E, r, s.defaultLocale) }) : null;
  if (D.status === "loading")
    return /* @__PURE__ */ T.jsxs("div", { className: "survey-question survey-question--options-loading", role: "status", children: [
      C,
      /* @__PURE__ */ T.jsx("p", { className: "survey-question__options-status", children: h.loadingOptions })
    ] });
  if (D.status === "error")
    return /* @__PURE__ */ T.jsxs("div", { className: "survey-question survey-question--options-error", children: [
      C,
      /* @__PURE__ */ T.jsx("p", { className: "survey-question__options-status", role: "alert", children: h.optionsLoadError }),
      /* @__PURE__ */ T.jsx(
        "button",
        {
          type: "button",
          className: "survey-button survey-button--retry",
          onClick: () => j((K) => K + 1),
          children: h.retry
        }
      )
    ] });
  const G = c.type === "navigationList", J = {
    ...c,
    options: D.options.map((K) => ({
      id: K.id,
      label: { [r]: K.label },
      ...G && g.nextScreen ? { nextScreen: g.nextScreen } : {}
    }))
  };
  return /* @__PURE__ */ T.jsx(f, { question: J });
}
function Lv({
  question: c,
  registry: f
}) {
  const { ui: r } = ue(), s = c.type, h = s ? f[s] : void 0;
  if (!h)
    return /* @__PURE__ */ T.jsx("div", { className: "survey-question survey-question--unknown", children: /* @__PURE__ */ T.jsxs("em", { children: [
      r.unsupportedQuestion,
      " ",
      String(s ?? "missing")
    ] }) });
  const g = Array.isArray(c.options) && c.options.length > 0;
  return c.optionsSource != null && !g ? /* @__PURE__ */ T.jsx(Yv, { question: c, Component: h }) : /* @__PURE__ */ T.jsx(h, { question: c });
}
function Gv({ question: c }) {
  const { locale: f, schema: r, answers: s, setAnswer: h } = ue(), g = c.id, N = c.title, D = c.help, q = !!c.required, p = s[g] ?? "";
  return /* @__PURE__ */ T.jsxs("div", { className: "survey-question survey-question--text", children: [
    /* @__PURE__ */ T.jsxs("label", { className: "survey-question__label", htmlFor: `q-${g}`, children: [
      P(N, f, r.defaultLocale),
      q && /* @__PURE__ */ T.jsx("span", { "aria-label": "required", className: "survey-question__required", children: " *" })
    ] }),
    D && /* @__PURE__ */ T.jsx("p", { className: "survey-question__help", children: P(D, f, r.defaultLocale) }),
    /* @__PURE__ */ T.jsx(
      "input",
      {
        id: `q-${g}`,
        className: "survey-question__input",
        type: "text",
        value: p,
        required: q,
        onChange: (j) => h(g, j.target.value)
      }
    )
  ] });
}
function Qv({ question: c }) {
  const { locale: f, schema: r, answers: s, setAnswer: h } = ue(), g = c.id, N = c.title, D = c.help, q = !!c.required, p = Number(c.min ?? 0), j = Number(c.max ?? 10), E = c.lowLabel, C = c.highLabel, G = s[g], J = [];
  for (let K = p; K <= j; K++) J.push(K);
  return /* @__PURE__ */ T.jsxs("fieldset", { className: "survey-question survey-question--nps", children: [
    /* @__PURE__ */ T.jsxs("legend", { className: "survey-question__label", children: [
      P(N, f, r.defaultLocale),
      q && /* @__PURE__ */ T.jsx("span", { "aria-label": "required", className: "survey-question__required", children: " *" })
    ] }),
    D && /* @__PURE__ */ T.jsx("p", { className: "survey-question__help", children: P(D, f, r.defaultLocale) }),
    /* @__PURE__ */ T.jsx("div", { className: "survey-question__nps-scale", role: "radiogroup", children: J.map((K) => {
      const Y = G === K;
      return /* @__PURE__ */ T.jsx(
        "button",
        {
          type: "button",
          role: "radio",
          "aria-checked": Y,
          className: "survey-question__nps-step" + (Y ? " survey-question__nps-step--selected" : ""),
          onClick: () => h(g, K),
          children: K
        },
        K
      );
    }) }),
    (E || C) && /* @__PURE__ */ T.jsxs("div", { className: "survey-question__nps-labels", children: [
      /* @__PURE__ */ T.jsx("span", { children: E ? P(E, f, r.defaultLocale) : "" }),
      /* @__PURE__ */ T.jsx("span", { children: C ? P(C, f, r.defaultLocale) : "" })
    ] })
  ] });
}
function Xv({ question: c }) {
  const { locale: f, schema: r } = ue(), s = c.id, h = c.title, g = c.help, N = c.options ?? [], D = (q, p) => {
    const j = {
      questionId: s,
      option: {
        id: p.id,
        nextScreen: p.nextScreen
      }
    };
    q.currentTarget.dispatchEvent(
      new CustomEvent("survey:navigationListSelect", {
        detail: j,
        bubbles: !0
      })
    );
  };
  return /* @__PURE__ */ T.jsxs("div", { className: "survey-question survey-question--navlist", children: [
    /* @__PURE__ */ T.jsx("div", { className: "survey-question__label", children: P(h, f, r.defaultLocale) }),
    g && /* @__PURE__ */ T.jsx("p", { className: "survey-question__help", children: P(g, f, r.defaultLocale) }),
    /* @__PURE__ */ T.jsx("ul", { className: "survey-navlist", role: "radiogroup", "aria-description": "Selecting an option navigates to the next screen.", children: N.map((q) => {
      const p = q.id, j = q.label;
      return /* @__PURE__ */ T.jsx("li", { className: "survey-navlist__row", children: /* @__PURE__ */ T.jsxs(
        "button",
        {
          type: "button",
          className: "survey-navlist__button",
          onClick: (E) => D(E, q),
          children: [
            /* @__PURE__ */ T.jsx("span", { className: "survey-navlist__label", children: P(j, f, r.defaultLocale) }),
            /* @__PURE__ */ T.jsx("span", { "aria-hidden": "true", className: "survey-navlist__chevron", children: "›" })
          ]
        }
      ) }, p);
    }) })
  ] });
}
function Zv({ question: c }) {
  const { locale: f, schema: r, answers: s, setAnswer: h } = ue(), g = c.id, N = c.title, D = c.help, q = c.placeholder, p = !!c.required, j = c.minLength, E = c.maxLength, C = s[g] ?? "";
  return /* @__PURE__ */ T.jsxs("div", { className: "survey-question survey-question--paragraph", children: [
    /* @__PURE__ */ T.jsxs("label", { className: "survey-question__label", htmlFor: `q-${g}`, children: [
      P(N, f, r.defaultLocale),
      p && /* @__PURE__ */ T.jsx("span", { "aria-label": "required", className: "survey-question__required", children: " *" })
    ] }),
    D && /* @__PURE__ */ T.jsx("p", { className: "survey-question__help", children: P(D, f, r.defaultLocale) }),
    /* @__PURE__ */ T.jsx(
      "textarea",
      {
        id: `q-${g}`,
        className: "survey-question__textarea",
        value: C,
        required: p,
        rows: 5,
        minLength: j,
        maxLength: E,
        placeholder: q ? P(q, f, r.defaultLocale) : void 0,
        onChange: (G) => h(g, G.target.value)
      }
    )
  ] });
}
function Vv({ question: c }) {
  const { locale: f, schema: r, answers: s, setAnswer: h } = ue(), g = c.id, N = c.title, D = c.help, q = !!c.required, p = c.min, j = c.max, E = c.step, C = c.unit, G = s[g], J = G == null ? "" : String(G);
  return /* @__PURE__ */ T.jsxs("div", { className: "survey-question survey-question--number", children: [
    /* @__PURE__ */ T.jsxs("label", { className: "survey-question__label", htmlFor: `q-${g}`, children: [
      P(N, f, r.defaultLocale),
      q && /* @__PURE__ */ T.jsx("span", { "aria-label": "required", className: "survey-question__required", children: " *" })
    ] }),
    D && /* @__PURE__ */ T.jsx("p", { className: "survey-question__help", children: P(D, f, r.defaultLocale) }),
    /* @__PURE__ */ T.jsxs("div", { className: "survey-question__number-wrap", children: [
      /* @__PURE__ */ T.jsx(
        "input",
        {
          id: `q-${g}`,
          className: "survey-question__input",
          type: "number",
          value: J,
          required: q,
          min: p,
          max: j,
          step: E,
          onChange: (K) => {
            const Y = K.target.value;
            h(g, Y === "" ? null : Number(Y));
          }
        }
      ),
      C && /* @__PURE__ */ T.jsx("span", { className: "survey-question__unit", children: P(C, f, r.defaultLocale) })
    ] })
  ] });
}
function Jv({ question: c }) {
  const { locale: f, schema: r, answers: s, setAnswer: h } = ue(), g = c.id, N = c.title, D = c.help, q = !!c.required, p = Number(c.max ?? 5), j = s[g], E = [];
  for (let C = 1; C <= p; C++) E.push(C);
  return /* @__PURE__ */ T.jsxs("fieldset", { className: "survey-question survey-question--rating", children: [
    /* @__PURE__ */ T.jsxs("legend", { className: "survey-question__label", children: [
      P(N, f, r.defaultLocale),
      q && /* @__PURE__ */ T.jsx("span", { "aria-label": "required", className: "survey-question__required", children: " *" })
    ] }),
    D && /* @__PURE__ */ T.jsx("p", { className: "survey-question__help", children: P(D, f, r.defaultLocale) }),
    /* @__PURE__ */ T.jsx("div", { className: "survey-question__rating-scale", role: "radiogroup", children: E.map((C) => {
      const G = typeof j == "number" && C <= j;
      return /* @__PURE__ */ T.jsx(
        "button",
        {
          type: "button",
          role: "radio",
          "aria-checked": j === C,
          "aria-label": `${C}`,
          className: "survey-question__rating-star" + (G ? " survey-question__rating-star--selected" : ""),
          onClick: () => h(g, C),
          children: /* @__PURE__ */ T.jsx("span", { "aria-hidden": "true", children: "★" })
        },
        C
      );
    }) })
  ] });
}
function Kv({ question: c }) {
  const { locale: f, schema: r, answers: s, setAnswer: h } = ue(), g = c.id, N = c.title, D = c.help, q = !!c.required, p = c.options ?? [], j = s[g];
  return /* @__PURE__ */ T.jsxs("fieldset", { className: "survey-question survey-question--single", children: [
    /* @__PURE__ */ T.jsxs("legend", { className: "survey-question__label", children: [
      P(N, f, r.defaultLocale),
      q && /* @__PURE__ */ T.jsx("span", { "aria-label": "required", className: "survey-question__required", children: " *" })
    ] }),
    D && /* @__PURE__ */ T.jsx("p", { className: "survey-question__help", children: P(D, f, r.defaultLocale) }),
    /* @__PURE__ */ T.jsx("div", { className: "survey-question__options", children: p.map((E) => /* @__PURE__ */ T.jsxs("label", { className: "survey-question__option", children: [
      /* @__PURE__ */ T.jsx(
        "input",
        {
          type: "radio",
          name: `q-${g}`,
          value: E.id,
          checked: j === E.id,
          onChange: () => h(g, E.id)
        }
      ),
      /* @__PURE__ */ T.jsx("span", { children: P(E.label, f, r.defaultLocale) })
    ] }, E.id)) })
  ] });
}
function wv({ question: c }) {
  const { locale: f, schema: r, answers: s, setAnswer: h } = ue(), g = c.id, N = c.title, D = c.help, q = !!c.required, p = c.options ?? [], j = c.maxSelected, E = s[g] ?? [], C = (G) => {
    if (E.includes(G)) {
      h(g, E.filter((J) => J !== G));
      return;
    }
    j !== void 0 && E.length >= j || h(g, [...E, G]);
  };
  return /* @__PURE__ */ T.jsxs("fieldset", { className: "survey-question survey-question--multi", children: [
    /* @__PURE__ */ T.jsxs("legend", { className: "survey-question__label", children: [
      P(N, f, r.defaultLocale),
      q && /* @__PURE__ */ T.jsx("span", { "aria-label": "required", className: "survey-question__required", children: " *" })
    ] }),
    D && /* @__PURE__ */ T.jsx("p", { className: "survey-question__help", children: P(D, f, r.defaultLocale) }),
    /* @__PURE__ */ T.jsx("div", { className: "survey-question__options", children: p.map((G) => {
      const J = E.includes(G.id);
      return /* @__PURE__ */ T.jsxs("label", { className: "survey-question__option", children: [
        /* @__PURE__ */ T.jsx(
          "input",
          {
            type: "checkbox",
            checked: J,
            onChange: () => C(G.id)
          }
        ),
        /* @__PURE__ */ T.jsx("span", { children: P(G.label, f, r.defaultLocale) })
      ] }, G.id);
    }) })
  ] });
}
function kv({ question: c }) {
  const { locale: f, schema: r, answers: s, setAnswer: h, ui: g } = ue(), N = c.id, D = c.title, q = c.help, p = !!c.required, j = c.options ?? [], E = c.placeholder, C = s[N] ?? "", G = E ? P(E, f, r.defaultLocale) : g.selectPlaceholder;
  return /* @__PURE__ */ T.jsxs("div", { className: "survey-question survey-question--dropdown", children: [
    /* @__PURE__ */ T.jsxs("label", { className: "survey-question__label", htmlFor: `q-${N}`, children: [
      P(D, f, r.defaultLocale),
      p && /* @__PURE__ */ T.jsx("span", { "aria-label": "required", className: "survey-question__required", children: " *" })
    ] }),
    q && /* @__PURE__ */ T.jsx("p", { className: "survey-question__help", children: P(q, f, r.defaultLocale) }),
    /* @__PURE__ */ T.jsxs(
      "select",
      {
        id: `q-${N}`,
        className: "survey-question__select",
        value: C,
        required: p,
        onChange: (J) => h(N, J.target.value || null),
        children: [
          /* @__PURE__ */ T.jsx("option", { value: "", children: G }),
          j.map((J) => /* @__PURE__ */ T.jsx("option", { value: J.id, children: P(J.label, f, r.defaultLocale) }, J.id))
        ]
      }
    )
  ] });
}
function $v({ question: c }) {
  const { locale: f, schema: r, answers: s, setAnswer: h } = ue(), g = c.id, N = c.title, D = c.help, q = !!c.required, p = c.minDate, j = c.maxDate, E = s[g] ?? "";
  return /* @__PURE__ */ T.jsxs("div", { className: "survey-question survey-question--date", children: [
    /* @__PURE__ */ T.jsxs("label", { className: "survey-question__label", htmlFor: `q-${g}`, children: [
      P(N, f, r.defaultLocale),
      q && /* @__PURE__ */ T.jsx("span", { "aria-label": "required", className: "survey-question__required", children: " *" })
    ] }),
    D && /* @__PURE__ */ T.jsx("p", { className: "survey-question__help", children: P(D, f, r.defaultLocale) }),
    /* @__PURE__ */ T.jsx(
      "input",
      {
        id: `q-${g}`,
        className: "survey-question__input",
        type: "date",
        value: E,
        required: q,
        min: p,
        max: j,
        onChange: (C) => h(g, C.target.value || null)
      }
    )
  ] });
}
function Wv({ question: c }) {
  const { locale: f, schema: r, answers: s, setAnswer: h } = ue(), g = c.id, N = c.title, D = c.help, q = !!c.required, p = c.minDateTime, j = c.maxDateTime, E = s[g] ?? "", C = (G) => {
    if (!G) return;
    const J = G.match(/^(\d{4}-\d{2}-\d{2}T\d{2}:\d{2})/);
    return (J == null ? void 0 : J[1]) ?? void 0;
  };
  return /* @__PURE__ */ T.jsxs("div", { className: "survey-question survey-question--datetime", children: [
    /* @__PURE__ */ T.jsxs("label", { className: "survey-question__label", htmlFor: `q-${g}`, children: [
      P(N, f, r.defaultLocale),
      q && /* @__PURE__ */ T.jsx("span", { "aria-label": "required", className: "survey-question__required", children: " *" })
    ] }),
    D && /* @__PURE__ */ T.jsx("p", { className: "survey-question__help", children: P(D, f, r.defaultLocale) }),
    /* @__PURE__ */ T.jsx(
      "input",
      {
        id: `q-${g}`,
        className: "survey-question__input",
        type: "datetime-local",
        value: C(E) ?? "",
        required: q,
        min: C(p),
        max: C(j),
        onChange: (G) => h(g, G.target.value || null)
      }
    )
  ] });
}
function Fv({ question: c }) {
  const { locale: f, schema: r, answers: s, setAnswer: h, ui: g } = ue(), N = c.id, D = c.title, q = c.help, p = !!c.required, j = c.acceptedTypes, E = W.useRef(null), C = s[N], G = j && j.length > 0 ? j.join(",") : void 0;
  return /* @__PURE__ */ T.jsxs("div", { className: "survey-question survey-question--file", children: [
    /* @__PURE__ */ T.jsxs("label", { className: "survey-question__label", htmlFor: `q-${N}`, children: [
      P(D, f, r.defaultLocale),
      p && /* @__PURE__ */ T.jsx("span", { "aria-label": "required", className: "survey-question__required", children: " *" })
    ] }),
    q && /* @__PURE__ */ T.jsx("p", { className: "survey-question__help", children: P(q, f, r.defaultLocale) }),
    /* @__PURE__ */ T.jsx(
      "input",
      {
        ref: E,
        id: `q-${N}`,
        className: "survey-question__file",
        type: "file",
        required: p,
        accept: G,
        onChange: (J) => {
          var K;
          const Y = (K = J.target.files) == null ? void 0 : K[0];
          if (!Y) {
            h(N, null);
            return;
          }
          h(N, { name: Y.name, size: Y.size, type: Y.type });
        }
      }
    ),
    (C == null ? void 0 : C.name) && /* @__PURE__ */ T.jsx("p", { className: "survey-question__file-name", children: Kl(g.fileRecordedName, { name: C.name }) })
  ] });
}
const ls = 480, as = 160;
function Iv({ question: c }) {
  const { locale: f, schema: r, answers: s, setAnswer: h, ui: g } = ue(), N = c.id, D = c.title, q = c.help, p = !!c.required, j = W.useRef(null), [E, C] = W.useState(!1), [G, J] = W.useState(!!s[N]), K = () => {
    var et;
    return ((et = j.current) == null ? void 0 : et.getContext("2d")) ?? null;
  }, Y = (et) => {
    const tt = et.target.getBoundingClientRect();
    return {
      x: (et.clientX - tt.left) / tt.width * ls,
      y: (et.clientY - tt.top) / tt.height * as
    };
  }, w = W.useCallback(() => {
    var et;
    const tt = (et = j.current) == null ? void 0 : et.toDataURL("image/png");
    tt && h(N, tt);
  }, [N, h]), ct = () => {
    const et = K();
    et && (et.clearRect(0, 0, ls, as), J(!1), h(N, null));
  };
  return W.useEffect(() => {
    const et = K();
    et && (et.lineWidth = 2, et.lineCap = "round", et.strokeStyle = "#111");
  }, []), /* @__PURE__ */ T.jsxs("div", { className: "survey-question survey-question--signature", children: [
    /* @__PURE__ */ T.jsxs("div", { className: "survey-question__label", children: [
      P(D, f, r.defaultLocale),
      p && /* @__PURE__ */ T.jsx("span", { "aria-label": "required", className: "survey-question__required", children: " *" })
    ] }),
    q && /* @__PURE__ */ T.jsx("p", { className: "survey-question__help", children: P(q, f, r.defaultLocale) }),
    /* @__PURE__ */ T.jsx(
      "canvas",
      {
        ref: j,
        className: "survey-question__signature-canvas",
        width: ls,
        height: as,
        role: "img",
        "aria-label": "signature pad",
        onPointerDown: (et) => {
          et.target.setPointerCapture(et.pointerId);
          const tt = K();
          if (!tt) return;
          const { x: Ut, y: Ct } = Y(et);
          tt.beginPath(), tt.moveTo(Ut, Ct), C(!0);
        },
        onPointerMove: (et) => {
          if (!E) return;
          const tt = K();
          if (!tt) return;
          const { x: Ut, y: Ct } = Y(et);
          tt.lineTo(Ut, Ct), tt.stroke(), J(!0);
        },
        onPointerUp: () => {
          C(!1), G && w();
        }
      }
    ),
    /* @__PURE__ */ T.jsx("div", { className: "survey-question__signature-actions", children: /* @__PURE__ */ T.jsx("button", { type: "button", className: "survey-button survey-button--ghost", onClick: ct, children: g.clearSignature }) })
  ] });
}
function Pv({ question: c }) {
  const { locale: f, schema: r, answers: s, setAnswer: h, ui: g } = ue(), N = c.id, D = c.title, q = c.help, p = !!c.required, j = c.yesLabel, E = c.noLabel, C = s[N], G = j ? P(j, f, r.defaultLocale) : g.yes, J = E ? P(E, f, r.defaultLocale) : g.no;
  return /* @__PURE__ */ T.jsxs("fieldset", { className: "survey-question survey-question--yesno", children: [
    /* @__PURE__ */ T.jsxs("legend", { className: "survey-question__label", children: [
      P(D, f, r.defaultLocale),
      p && /* @__PURE__ */ T.jsx("span", { "aria-label": "required", className: "survey-question__required", children: " *" })
    ] }),
    q && /* @__PURE__ */ T.jsx("p", { className: "survey-question__help", children: P(q, f, r.defaultLocale) }),
    /* @__PURE__ */ T.jsxs("div", { className: "survey-question__yesno", role: "radiogroup", children: [
      /* @__PURE__ */ T.jsx(
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
      /* @__PURE__ */ T.jsx(
        "button",
        {
          type: "button",
          role: "radio",
          "aria-checked": C === !1,
          className: "survey-question__yesno-button" + (C === !1 ? " survey-question__yesno-button--selected" : ""),
          onClick: () => h(N, !1),
          children: J
        }
      )
    ] })
  ] });
}
function t0(c, f) {
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
const e0 = {
  text: Gv,
  paragraph: Zv,
  number: Vv,
  rating: Jv,
  nps: Qv,
  singleChoice: Kv,
  multiChoice: wv,
  dropdown: kv,
  date: $v,
  dateTime: Wv,
  file: Fv,
  signature: Iv,
  yesNo: Pv,
  navigationList: Xv
};
function l0(c, f, r) {
  const s = c.screens.find((h) => h.id === f);
  return !s || (s.questions ?? []).length > 0 ? !1 : cn(c, f, r).kind === "end";
}
function a0({
  schema: c,
  onSubmit: f,
  initialAnswers: r,
  locale: s,
  onScreenChange: h,
  onCompleted: g,
  registry: N,
  submissionMeta: D,
  uiLocales: q,
  resumeKey: p,
  storage: j,
  emitHostMessages: E,
  hostMessageOrigin: C,
  hostMessageTarget: G,
  activeScreenId: J,
  activeScreenJumpToken: K
}) {
  var Y, w;
  const ct = s ?? c.defaultLocale ?? "en", et = N ?? e0, tt = W.useMemo(
    () => Dv(ct, c.defaultLocale, q),
    [ct, c.defaultLocale, q]
  ), Ut = j ?? (typeof globalThis < "u" ? globalThis.localStorage : void 0), Ct = W.useMemo(() => {
    var X;
    if (!p || !Ut) return null;
    const pt = Rv(Ut, p);
    return pt ? pt.currentScreenId === null || c.screens.some((ht) => ht.id === pt.currentScreenId) ? pt : { ...pt, currentScreenId: ((X = c.screens[0]) == null ? void 0 : X.id) ?? null } : null;
  }, []), [Z, ee] = W.useState(() => ({
    ...r ?? {},
    ...(Ct == null ? void 0 : Ct.answers) ?? {}
  })), [lt, Le] = W.useState(
    () => {
      var X;
      return (Ct == null ? void 0 : Ct.currentScreenId) ?? ((X = c.screens[0]) == null ? void 0 : X.id) ?? null;
    }
  );
  W.useEffect(() => {
    if (c.screens.length === 0) {
      lt !== null && Le(null);
      return;
    }
    lt !== null && c.screens.some((X) => X.id === lt) || Le(c.screens[0].id);
  }, [c, lt]);
  const [ne, kt] = W.useState(!1), [he, qe] = W.useState(null), [ve, z] = W.useState(/* @__PURE__ */ new Set()), [B, k] = W.useState(/* @__PURE__ */ new Set()), [it, St] = W.useState(!1), m = W.useRef(void 0);
  W.useEffect(() => {
    if (J === void 0) return;
    const X = `${K ?? ""}:${J ?? ""}`;
    m.current !== X && (m.current = X, !(J === null || it) && c.screens.some((pt) => pt.id === J) && (z(/* @__PURE__ */ new Set()), Le(J)));
  }, [J, K, c, it]);
  const U = W.useRef((/* @__PURE__ */ new Date()).toISOString()), R = W.useRef(null);
  if (R.current === null) {
    const X = {};
    G !== void 0 && (X.target = G), C !== void 0 && (X.targetOrigin = C), E !== void 0 && (X.enabled = E), R.current = jv(X);
  }
  const H = W.useMemo(
    () => lt ? c.screens.find((X) => X.id === lt) ?? null : null,
    [c, lt]
  );
  W.useEffect(() => {
    var X;
    h == null || h(lt), (X = R.current) == null || X.screenChanged(lt);
  }, [lt, h]);
  const F = W.useRef(!1);
  W.useEffect(() => {
    var X;
    F.current || !lt || (F.current = !0, (X = R.current) == null || X.loaded());
  }, [lt]), W.useEffect(() => {
    !p || !Ut || it || Hv(Ut, p, {
      answers: Z,
      currentScreenId: lt,
      schemaVersion: c.version
    });
  }, [Z, lt, p, Ut, it, c.version]), W.useEffect(() => {
    it && p && Ut && Bv(Ut, p);
  }, [it, p, Ut]), W.useEffect(() => {
    var X;
    he && ((X = R.current) == null || X.error(he));
  }, [he]);
  const at = W.useCallback((X, pt) => {
    ee((ht) => ({ ...ht, [X]: pt }));
  }, []), dt = W.useCallback(
    (X) => {
      X !== null && (z(/* @__PURE__ */ new Set()), k(/* @__PURE__ */ new Set()), Le(X));
    },
    []
  ), Zt = W.useCallback(
    (X) => {
      if (!X.required) return !1;
      const pt = Z[X.id];
      return !!(pt == null || typeof pt == "string" && pt.trim() === "" || Array.isArray(pt) && pt.length === 0);
    },
    [Z]
  ), At = W.useCallback(async () => {
    var X;
    kt(!0), qe(null);
    try {
      await f({
        schemaVersion: c.version ?? 0,
        answers: Z,
        meta: {
          startedAt: (D == null ? void 0 : D.startedAt) ?? U.current,
          completedAt: (D == null ? void 0 : D.completedAt) ?? (/* @__PURE__ */ new Date()).toISOString(),
          ...D ?? {}
        }
      }), St(!0), g == null || g(lt), (X = R.current) == null || X.completed({ screenId: lt, answers: Z });
    } catch (pt) {
      qe(pt.message ?? String(pt));
    } finally {
      kt(!1);
    }
  }, [c.version, Z, D, f, g, lt]), Wl = W.useCallback(() => {
    if (!lt) return;
    const X = c.screens.find(($t) => $t.id === lt), pt = ((X == null ? void 0 : X.questions) ?? []).filter(Zt).map(($t) => $t.id);
    if (pt.length > 0) {
      z(new Set(pt));
      return;
    }
    const ht = Ev(X == null ? void 0 : X.questions, Z);
    if (ht.length > 0) {
      k(new Set(ht.map(($t) => $t.questionId)));
      return;
    }
    const Jt = cn(c, lt, Z);
    Jt.kind === "end" ? At() : dt(Jt.screenId);
  }, [c, lt, Z, Zt, dt, At]), Fl = W.useRef(null);
  W.useEffect(() => {
    it || ne || !lt || !H || Fl.current === lt || !(!H.questions || H.questions.length === 0) || cn(c, lt, Z).kind === "end" && (Fl.current = lt, At());
  }, [lt, H, it, ne, c, Z, At]);
  const Fe = W.useRef(null);
  W.useEffect(() => {
    const X = Fe.current;
    if (!X || typeof ResizeObserver > "u") return;
    const pt = new ResizeObserver((ht) => {
      var Jt;
      const $t = ht[0];
      $t && ((Jt = R.current) == null || Jt.resize(Math.ceil($t.contentRect.height)));
    });
    return pt.observe(X), () => pt.disconnect();
  }, []), W.useEffect(() => {
    const X = Fe.current;
    if (!X) return;
    const pt = (ht) => {
      const Jt = ht.detail;
      if (!Jt || !lt) return;
      at(Jt.questionId, Jt.option.id);
      const $t = { ...Z, [Jt.questionId]: Jt.option.id }, ou = _v(
        Jt.option,
        c,
        lt,
        $t
      );
      ou.kind === "end" ? At() : dt(ou.screenId);
    };
    return X.addEventListener("survey:navigationListSelect", pt), () => X.removeEventListener("survey:navigationListSelect", pt);
  }, [Z, lt, c, at, dt, At]);
  const rn = W.useMemo(
    () => ({
      schema: c,
      locale: ct,
      direction: tt.direction,
      ui: tt.strings,
      answers: Z,
      setAnswer: at
    }),
    [c, ct, tt, Z, at]
  ), Ge = W.useMemo(() => qv(c.branding), [c.branding]), pa = (Y = c.branding) != null && Y.logoUrl ? /* @__PURE__ */ T.jsx("div", { className: "survey-brand", children: /* @__PURE__ */ T.jsx(
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
  if (it)
    return /* @__PURE__ */ T.jsxs(
      "div",
      {
        ref: Fe,
        className: "survey-root survey-root--done",
        dir: tt.direction,
        lang: ct,
        style: Ge,
        children: [
          pa,
          /* @__PURE__ */ T.jsxs("div", { className: "survey-screen", children: [
            /* @__PURE__ */ T.jsx("h2", { className: "survey-screen__title", children: H != null && H.title ? P(H.title, ct, c.defaultLocale) : tt.strings.thankYou }),
            (H == null ? void 0 : H.description) && /* @__PURE__ */ T.jsx("p", { className: "survey-screen__description", children: P(H.description, ct, c.defaultLocale) })
          ] })
        ]
      }
    );
  if (!H)
    return /* @__PURE__ */ T.jsx("div", { ref: Fe, className: "survey-root", dir: tt.direction, lang: ct, style: Ge, children: /* @__PURE__ */ T.jsx("div", { className: "survey-screen", children: /* @__PURE__ */ T.jsx("em", { children: tt.strings.noScreens }) }) });
  const Ie = H.questions ?? [], Mi = Ie.length > 0 && ((w = Ie[Ie.length - 1]) == null ? void 0 : w.type) === "navigationList", on = Ie.length === 0 && !H.nextScreen, ba = !Mi && !on, Sl = ba && lt !== null ? cn(c, lt, Z) : null, ru = Sl !== null && (Sl.kind === "end" || Sl.kind === "screen" && l0(c, Sl.screenId, Z));
  return /* @__PURE__ */ T.jsx(Nv, { value: rn, children: /* @__PURE__ */ T.jsxs("div", { ref: Fe, className: "survey-root", dir: tt.direction, lang: ct, style: Ge, children: [
    pa,
    /* @__PURE__ */ T.jsxs("div", { className: "survey-screen", children: [
      H.title && /* @__PURE__ */ T.jsx("h2", { className: "survey-screen__title", children: P(H.title, ct, c.defaultLocale) }),
      H.description && /* @__PURE__ */ T.jsx("p", { className: "survey-screen__description", children: P(H.description, ct, c.defaultLocale) }),
      /* @__PURE__ */ T.jsx("div", { className: "survey-screen__questions", children: Ie.map((X, pt) => {
        const ht = X.id, Jt = ht !== void 0 && ve.has(ht) && Zt(X), $t = !Jt && ht !== void 0 && B.has(ht) && Z[ht] != null ? Ey(X, Z[ht])[0] ?? null : null;
        return /* @__PURE__ */ T.jsxs("div", { className: Jt || $t !== null ? "survey-question-slot survey-question-slot--invalid" : "survey-question-slot", children: [
          /* @__PURE__ */ T.jsx(Lv, { question: X, registry: et }),
          Jt && /* @__PURE__ */ T.jsx("p", { className: "survey-question__required-error", role: "alert", children: tt.strings.requiredError }),
          $t && /* @__PURE__ */ T.jsx("p", { className: "survey-question__required-error", role: "alert", children: t0($t, tt.strings) })
        ] }, ht ?? pt);
      }) }),
      ba && /* @__PURE__ */ T.jsx("div", { className: "survey-screen__actions", children: /* @__PURE__ */ T.jsx(
        "button",
        {
          type: "button",
          className: "survey-button survey-button--primary",
          disabled: ne,
          onClick: Wl,
          children: ne ? tt.strings.submitting : ru ? tt.strings.submit : tt.strings.next
        }
      ) }),
      he && /* @__PURE__ */ T.jsxs("p", { className: "survey-screen__error", role: "alert", children: [
        tt.strings.couldNotSubmit,
        " ",
        he
      ] })
    ] })
  ] }) });
}
const u0 = ".survey-root{--survey-primary: #2563eb;--survey-primary-hover: #1e40af;--survey-primary-contrast: #ffffff;--survey-accent: #f5b60c;font-family:system-ui,-apple-system,Segoe UI,Roboto,sans-serif;color:#111;max-width:640px;margin:0 auto;padding:32px 16px}.survey-brand{display:flex;margin-bottom:20px}.survey-brand__logo{height:28px;width:auto}.survey-screen{display:flex;flex-direction:column;gap:24px}.survey-screen__title{font-size:1.5rem;font-weight:600;margin:0}.survey-screen__description{color:#555;margin:0}.survey-screen__questions{display:flex;flex-direction:column;gap:24px}.survey-screen__actions{display:flex;justify-content:flex-end}.survey-screen__error{color:#b42318;background:#fef3f2;border:1px solid #fecdca;padding:12px 14px;border-radius:8px;margin:0}.survey-question-slot--invalid{border-inline-start:3px solid #b42318;padding-inline-start:10px}.survey-question__required-error{color:#b42318;font-size:.9rem;margin:4px 0 0}.survey-question{display:flex;flex-direction:column;gap:8px}.survey-question__label{font-weight:600;display:block}.survey-question__required{color:#b42318}.survey-question__help{margin:0;color:#666;font-size:.9rem}.survey-question__input{padding:10px 12px;border:1px solid #d0d5dd;border-radius:8px;font:inherit}.survey-question__input:focus-visible{outline:2px solid var(--survey-primary);outline-offset:1px;border-color:var(--survey-primary)}.survey-question--nps{border:none;padding:0;margin:0;display:flex;flex-direction:column;gap:8px}.survey-question__nps-scale{display:flex;gap:6px;flex-wrap:wrap}.survey-question__nps-step{min-width:40px;min-height:40px;padding:8px;border:1px solid #d0d5dd;border-radius:8px;background:#fff;font-weight:500;cursor:pointer}.survey-question__nps-step:hover{background:#f5f7fa}.survey-question__nps-step--selected{background:var(--survey-primary);border-color:var(--survey-primary);color:var(--survey-primary-contrast)}.survey-question__nps-labels{display:flex;justify-content:space-between;color:#555;font-size:.85rem}.survey-question--navlist{gap:12px}.survey-navlist{list-style:none;padding:0;margin:0;display:flex;flex-direction:column;gap:8px}.survey-navlist__row{margin:0}.survey-navlist__button{width:100%;display:flex;align-items:center;justify-content:space-between;padding:14px 16px;border:1px solid #d0d5dd;border-radius:10px;background:#fff;cursor:pointer;font:inherit;text-align:start}.survey-navlist__button:hover{background:#f5f7fa;border-color:var(--survey-primary)}.survey-navlist__chevron{font-size:1.5rem;color:#667085}.survey-root[dir=rtl] .survey-navlist__chevron{transform:scaleX(-1)}.survey-navlist__label{font-weight:500}.survey-question__textarea{padding:10px 12px;border:1px solid #d0d5dd;border-radius:8px;font:inherit;resize:vertical;min-height:96px}.survey-question__textarea:focus-visible{outline:2px solid var(--survey-primary);outline-offset:1px;border-color:var(--survey-primary)}.survey-question__number-wrap{display:flex;align-items:center;gap:8px}.survey-question__number-wrap .survey-question__input{flex:1}.survey-question__unit{color:#555;font-size:.9rem}.survey-question__rating-scale{display:flex;gap:4px}.survey-question__rating-star{background:transparent;border:none;cursor:pointer;font-size:1.8rem;line-height:1;color:#d0d5dd;padding:4px}.survey-question__rating-star:hover,.survey-question__rating-star--selected{color:var(--survey-accent)}.survey-question__options{display:flex;flex-direction:column;gap:8px}.survey-question__option{display:flex;align-items:center;gap:8px;padding:8px 12px;border:1px solid #d0d5dd;border-radius:8px;cursor:pointer}.survey-question__option:hover{background:#f5f7fa;border-color:var(--survey-primary)}.survey-question__option input{margin:0}.survey-question__select{padding:10px 12px;border:1px solid #d0d5dd;border-radius:8px;font:inherit;background:#fff}.survey-question__yesno{display:flex;gap:12px}.survey-question__yesno-button{flex:1;padding:14px 16px;border:1px solid #d0d5dd;border-radius:10px;background:#fff;font:inherit;font-weight:500;cursor:pointer}.survey-question__yesno-button:hover{background:#f5f7fa;border-color:var(--survey-primary)}.survey-question__yesno-button--selected{background:var(--survey-primary);border-color:var(--survey-primary);color:var(--survey-primary-contrast)}.survey-question__file{font:inherit}.survey-question__file-name{color:#555;font-size:.9rem;margin:0}.survey-question__signature-canvas{width:100%;max-width:480px;height:auto;aspect-ratio:3 / 1;border:1px dashed #d0d5dd;border-radius:8px;background:#fff;touch-action:none}.survey-question__signature-actions{display:flex;justify-content:flex-start;gap:8px}.survey-button{padding:10px 20px;border-radius:8px;border:1px solid transparent;cursor:pointer;font:inherit;font-weight:600}.survey-button--primary{background:var(--survey-primary);color:var(--survey-primary-contrast)}.survey-button--primary:hover{background:var(--survey-primary-hover)}.survey-button--ghost{background:#fff;color:#555;border-color:#d0d5dd}.survey-button--ghost:hover{background:#f5f7fa}.survey-button:disabled{opacity:.5;cursor:not-allowed}.survey-question__options-status{margin:6px 0;font-size:.9rem;color:var(--survey-muted, #667085)}.survey-question--options-error .survey-question__options-status{color:var(--survey-error, #b42318)}.survey-button--retry{background:transparent;color:var(--survey-primary, #4338ca);border:1px solid currentColor;padding:4px 14px;font-size:.85rem}";
var wl, cu, Je, kl, fu, va, fn, su, Xt, ns, pl, sn, nu;
class n0 extends HTMLElement {
  constructor() {
    super();
    Ve(this, Xt);
    /** Schema-mode setter. Assigning this swaps the element into schema mode and
     *  re-renders with the new schema immediately. */
    Ve(this, wl, null);
    /** Schema-mode submit handler. In API mode the element manages this itself. */
    Ve(this, cu, null);
    Ve(this, Je, null);
    Ve(this, kl, null);
    Ve(this, fu, null);
    Ve(this, va, null);
    /** Builder-preview jump target. Assigning a screen id makes the renderer
     *  jump to that screen (answers preserved); the user can navigate freely
     *  afterwards. Mirrors the `active-screen-id` attribute; the property wins
     *  when both are set. */
    Ve(this, fn, null);
    /** Bump to re-issue a jump to the screen already set on {@link activeScreenId}.
     *  Property-only (no attribute) — it is a transient signal, not page state. */
    Ve(this, su, 0);
    Ve(this, sn, !1);
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
        r.setAttribute("data-shift-survey", ""), r.textContent = u0, this.shadowRoot.appendChild(r);
      }
      Dt(this, kl) || (xe(this, kl, document.createElement("div")), Dt(this, kl).className = "shift-survey-mount", this.shadowRoot.appendChild(Dt(this, kl))), Dt(this, Je) || xe(this, Je, tv.createRoot(Dt(this, kl))), le(this, Xt, pl).call(this), le(this, Xt, ns).call(this);
    }
  }
  disconnectedCallback() {
    queueMicrotask(() => {
      var r;
      if (!(this.isConnected || typeof window > "u")) {
        try {
          (r = Dt(this, Je)) == null || r.unmount();
        } catch {
        }
        xe(this, Je, null);
      }
    });
  }
  attributeChangedCallback(r, s, h) {
    s !== h && ((r === "instance-id" || r === "api-base") && (xe(this, fu, null), xe(this, va, null), le(this, Xt, ns).call(this)), le(this, Xt, pl).call(this));
  }
  // ─── Properties ──────────────────────────────────────────────────────────
  get schema() {
    return Dt(this, wl);
  }
  set schema(r) {
    xe(this, wl, r), le(this, Xt, pl).call(this);
  }
  get onSubmit() {
    return Dt(this, cu);
  }
  set onSubmit(r) {
    xe(this, cu, r), le(this, Xt, pl).call(this);
  }
  get activeScreenId() {
    return Dt(this, fn) ?? this.getAttribute("active-screen-id");
  }
  set activeScreenId(r) {
    xe(this, fn, r), le(this, Xt, pl).call(this);
  }
  get activeScreenJumpToken() {
    return Dt(this, su);
  }
  set activeScreenJumpToken(r) {
    xe(this, su, r), le(this, Xt, pl).call(this);
  }
}
wl = new WeakMap(), cu = new WeakMap(), Je = new WeakMap(), kl = new WeakMap(), fu = new WeakMap(), va = new WeakMap(), fn = new WeakMap(), su = new WeakMap(), Xt = new WeakSet(), // ─── Internals ───────────────────────────────────────────────────────────
ns = function() {
  if (Dt(this, wl)) return;
  const r = this.getAttribute("instance-id");
  if (!r) return;
  const s = this.getAttribute("api-base");
  if (!s) return;
  new by({ baseUrl: s }).fetchSchema(r).then((g) => {
    xe(this, fu, g), le(this, Xt, pl).call(this);
  }).catch((g) => {
    xe(this, va, g), le(this, Xt, nu).call(this, "survey:error", { message: g.message }), le(this, Xt, pl).call(this);
  });
}, pl = function() {
  if (!Dt(this, Je)) return;
  const r = this.getAttribute("api-base"), s = this.getAttribute("instance-id"), h = this.getAttribute("locale") ?? void 0, g = this.getAttribute("mode") === "agent", N = Dt(this, wl) ?? Dt(this, fu);
  if (Dt(this, va) && !N) {
    Dt(this, Je).render(
      W.createElement(
        "div",
        { className: "shift-survey-error", role: "alert" },
        Dt(this, va).message
      )
    );
    return;
  }
  if (!N) {
    Dt(this, Je).render(
      W.createElement("div", { className: "shift-survey-loading" }, "Loading…")
    );
    return;
  }
  const D = Dt(this, wl) ? Dt(this, cu) ?? ((p) => {
    le(this, Xt, nu).call(this, "survey:completed", { ...p });
  }) : async (p) => {
    if (!r || !s)
      throw new Error("shift-survey: API mode requires both instance-id and api-base attributes.");
    await new by({ baseUrl: r }).submitResponse(s, p);
  }, q = this.activeScreenId;
  Dt(this, Je).render(
    W.createElement(a0, {
      schema: N,
      onSubmit: D,
      ...h ? { locale: h } : {},
      ...q ? { activeScreenId: q, activeScreenJumpToken: Dt(this, su) } : {},
      // Let the element be the resume key in API mode so two surveys on the
      // same host page don't clobber each other.
      ...s ? { resumeKey: s } : {},
      ...g ? { submissionMeta: { mode: "agent" } } : {},
      // CustomEvents are the web-component's channel; postMessage stays opt-in
      // via iframe auto-detect on the enclosing page (unchanged).
      onScreenChange: (p) => le(this, Xt, nu).call(this, "survey:screen-changed", { screenId: p }),
      onCompleted: (p) => le(this, Xt, nu).call(this, "survey:completed", { screenId: p })
    })
  ), Dt(this, sn) || (xe(this, sn, !0), le(this, Xt, nu).call(this, "survey:loaded", {}));
}, sn = new WeakMap(), nu = function(r, s) {
  this.dispatchEvent(
    new CustomEvent(r, { detail: s, bubbles: !0, composed: !0 })
  );
};
function i0(c = "shift-survey") {
  typeof window > "u" || typeof customElements > "u" || customElements.get(c) || customElements.define(c, n0);
}
i0();
export {
  n0 as ShiftSurveyElement,
  i0 as registerShiftSurvey
};
//# sourceMappingURL=index.js.map
