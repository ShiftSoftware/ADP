var Uh = Object.defineProperty;
var Jd = (c) => {
  throw TypeError(c);
};
var Ch = (c, o, r) => o in c ? Uh(c, o, { enumerable: !0, configurable: !0, writable: !0, value: r }) : c[o] = r;
var oe = (c, o, r) => Ch(c, typeof o != "symbol" ? o + "" : o, r), Df = (c, o, r) => o.has(c) || Jd("Cannot " + r);
var Cl = (c, o, r) => (Df(c, o, "read from private field"), r ? r.call(c) : o.get(c)), de = (c, o, r) => o.has(c) ? Jd("Cannot add the same private member more than once") : o instanceof WeakSet ? o.add(c) : o.set(c, r), Yt = (c, o, r, s) => (Df(c, o, "write to private field"), s ? s.call(c, r) : o.set(c, r), r), ct = (c, o, r) => (Df(c, o, "access private method"), r);
var Uf = { exports: {} }, F = {};
/**
 * @license React
 * react.production.js
 *
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 */
var wd;
function jh() {
  if (wd) return F;
  wd = 1;
  var c = Symbol.for("react.transitional.element"), o = Symbol.for("react.portal"), r = Symbol.for("react.fragment"), s = Symbol.for("react.strict_mode"), T = Symbol.for("react.profiler"), z = Symbol.for("react.consumer"), C = Symbol.for("react.context"), U = Symbol.for("react.forward_ref"), x = Symbol.for("react.suspense"), b = Symbol.for("react.memo"), j = Symbol.for("react.lazy"), p = Symbol.for("react.activity"), D = Symbol.iterator;
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
  }, K = Object.assign, L = {};
  function Z(v, O, R) {
    this.props = v, this.context = O, this.refs = L, this.updater = R || k;
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
    this.props = v, this.context = O, this.refs = L, this.updater = R || k;
  }
  var ll = W.prototype = new ol();
  ll.constructor = W, K(ll, Z.prototype), ll.isPureReactComponent = !0;
  var Wl = Array.isArray;
  function I() {
  }
  var al = { H: null, A: null, T: null, S: null }, Yl = Object.prototype.hasOwnProperty;
  function ft(v, O, R) {
    var Y = R.ref;
    return {
      $$typeof: c,
      type: v,
      key: O,
      ref: Y !== void 0 ? Y : null,
      props: R
    };
  }
  function _t(v, O) {
    return ft(v.type, O, v.props);
  }
  function st(v) {
    return typeof v == "object" && v !== null && v.$$typeof === c;
  }
  function Nl(v) {
    var O = { "=": "=0", ":": "=2" };
    return "$" + v.replace(/[=:]/g, function(R) {
      return O[R];
    });
  }
  var Jt = /\/+/g;
  function jt(v, O) {
    return typeof v == "object" && v !== null && v.key != null ? Nl("" + v.key) : O.toString(36);
  }
  function Gl(v) {
    switch (v.status) {
      case "fulfilled":
        return v.value;
      case "rejected":
        throw v.reason;
      default:
        switch (typeof v.status == "string" ? v.then(I, I) : (v.status = "pending", v.then(
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
  function _(v, O, R, Y, $) {
    var ul = typeof v;
    (ul === "undefined" || ul === "boolean") && (v = null);
    var vl = !1;
    if (v === null) vl = !0;
    else
      switch (ul) {
        case "bigint":
        case "string":
        case "number":
          vl = !0;
          break;
        case "object":
          switch (v.$$typeof) {
            case c:
            case o:
              vl = !0;
              break;
            case j:
              return vl = v._init, _(
                vl(v._payload),
                O,
                R,
                Y,
                $
              );
          }
      }
    if (vl)
      return $ = $(v), vl = Y === "" ? "." + jt(v, 0) : Y, Wl($) ? (R = "", vl != null && (R = vl.replace(Jt, "$&/") + "/"), _($, O, R, "", function(Tl) {
        return Tl;
      })) : $ != null && (st($) && ($ = _t(
        $,
        R + ($.key == null || v && v.key === $.key ? "" : ("" + $.key).replace(
          Jt,
          "$&/"
        ) + "/") + vl
      )), O.push($)), 1;
    vl = 0;
    var Vl = Y === "" ? "." : Y + ":";
    if (Wl(v))
      for (var G = 0; G < v.length; G++)
        Y = v[G], ul = Vl + jt(Y, G), vl += _(
          Y,
          O,
          R,
          ul,
          $
        );
    else if (G = X(v), typeof G == "function")
      for (v = G.call(v), G = 0; !(Y = v.next()).done; )
        Y = Y.value, ul = Vl + jt(Y, G++), vl += _(
          Y,
          O,
          R,
          ul,
          $
        );
    else if (ul === "object") {
      if (typeof v.then == "function")
        return _(
          Gl(v),
          O,
          R,
          Y,
          $
        );
      throw O = String(v), Error(
        "Objects are not valid as a React child (found: " + (O === "[object Object]" ? "object with keys {" + Object.keys(v).join(", ") + "}" : O) + "). If you meant to render a collection of children, use an array instead."
      );
    }
    return vl;
  }
  function H(v, O, R) {
    if (v == null) return v;
    var Y = [], $ = 0;
    return _(v, Y, "", "", function(ul) {
      return O.call(R, ul, $++);
    }), Y;
  }
  function J(v) {
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
  var yl = typeof reportError == "function" ? reportError : function(v) {
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
    map: H,
    forEach: function(v, O, R) {
      H(
        v,
        function() {
          O.apply(this, arguments);
        },
        R
      );
    },
    count: function(v) {
      var O = 0;
      return H(v, function() {
        O++;
      }), O;
    },
    toArray: function(v) {
      return H(v, function(O) {
        return O;
      }) || [];
    },
    only: function(v) {
      if (!st(v))
        throw Error(
          "React.Children.only expected to receive a single React element child."
        );
      return v;
    }
  };
  return F.Activity = p, F.Children = dl, F.Component = Z, F.Fragment = r, F.Profiler = T, F.PureComponent = W, F.StrictMode = s, F.Suspense = x, F.__CLIENT_INTERNALS_DO_NOT_USE_OR_WARN_USERS_THEY_CANNOT_UPGRADE = al, F.__COMPILER_RUNTIME = {
    __proto__: null,
    c: function(v) {
      return al.H.useMemoCache(v);
    }
  }, F.cache = function(v) {
    return function() {
      return v.apply(null, arguments);
    };
  }, F.cacheSignal = function() {
    return null;
  }, F.cloneElement = function(v, O, R) {
    if (v == null)
      throw Error(
        "The argument must be a React element, but you passed " + v + "."
      );
    var Y = K({}, v.props), $ = v.key;
    if (O != null)
      for (ul in O.key !== void 0 && ($ = "" + O.key), O)
        !Yl.call(O, ul) || ul === "key" || ul === "__self" || ul === "__source" || ul === "ref" && O.ref === void 0 || (Y[ul] = O[ul]);
    var ul = arguments.length - 2;
    if (ul === 1) Y.children = R;
    else if (1 < ul) {
      for (var vl = Array(ul), Vl = 0; Vl < ul; Vl++)
        vl[Vl] = arguments[Vl + 2];
      Y.children = vl;
    }
    return ft(v.type, $, Y);
  }, F.createContext = function(v) {
    return v = {
      $$typeof: C,
      _currentValue: v,
      _currentValue2: v,
      _threadCount: 0,
      Provider: null,
      Consumer: null
    }, v.Provider = v, v.Consumer = {
      $$typeof: z,
      _context: v
    }, v;
  }, F.createElement = function(v, O, R) {
    var Y, $ = {}, ul = null;
    if (O != null)
      for (Y in O.key !== void 0 && (ul = "" + O.key), O)
        Yl.call(O, Y) && Y !== "key" && Y !== "__self" && Y !== "__source" && ($[Y] = O[Y]);
    var vl = arguments.length - 2;
    if (vl === 1) $.children = R;
    else if (1 < vl) {
      for (var Vl = Array(vl), G = 0; G < vl; G++)
        Vl[G] = arguments[G + 2];
      $.children = Vl;
    }
    if (v && v.defaultProps)
      for (Y in vl = v.defaultProps, vl)
        $[Y] === void 0 && ($[Y] = vl[Y]);
    return ft(v, ul, $);
  }, F.createRef = function() {
    return { current: null };
  }, F.forwardRef = function(v) {
    return { $$typeof: U, render: v };
  }, F.isValidElement = st, F.lazy = function(v) {
    return {
      $$typeof: j,
      _payload: { _status: -1, _result: v },
      _init: J
    };
  }, F.memo = function(v, O) {
    return {
      $$typeof: b,
      type: v,
      compare: O === void 0 ? null : O
    };
  }, F.startTransition = function(v) {
    var O = al.T, R = {};
    al.T = R;
    try {
      var Y = v(), $ = al.S;
      $ !== null && $(R, Y), typeof Y == "object" && Y !== null && typeof Y.then == "function" && Y.then(I, yl);
    } catch (ul) {
      yl(ul);
    } finally {
      O !== null && R.types !== null && (O.types = R.types), al.T = O;
    }
  }, F.unstable_useCacheRefresh = function() {
    return al.H.useCacheRefresh();
  }, F.use = function(v) {
    return al.H.use(v);
  }, F.useActionState = function(v, O, R) {
    return al.H.useActionState(v, O, R);
  }, F.useCallback = function(v, O) {
    return al.H.useCallback(v, O);
  }, F.useContext = function(v) {
    return al.H.useContext(v);
  }, F.useDebugValue = function() {
  }, F.useDeferredValue = function(v, O) {
    return al.H.useDeferredValue(v, O);
  }, F.useEffect = function(v, O) {
    return al.H.useEffect(v, O);
  }, F.useEffectEvent = function(v) {
    return al.H.useEffectEvent(v);
  }, F.useId = function() {
    return al.H.useId();
  }, F.useImperativeHandle = function(v, O, R) {
    return al.H.useImperativeHandle(v, O, R);
  }, F.useInsertionEffect = function(v, O) {
    return al.H.useInsertionEffect(v, O);
  }, F.useLayoutEffect = function(v, O) {
    return al.H.useLayoutEffect(v, O);
  }, F.useMemo = function(v, O) {
    return al.H.useMemo(v, O);
  }, F.useOptimistic = function(v, O) {
    return al.H.useOptimistic(v, O);
  }, F.useReducer = function(v, O, R) {
    return al.H.useReducer(v, O, R);
  }, F.useRef = function(v) {
    return al.H.useRef(v);
  }, F.useState = function(v) {
    return al.H.useState(v);
  }, F.useSyncExternalStore = function(v, O, R) {
    return al.H.useSyncExternalStore(
      v,
      O,
      R
    );
  }, F.useTransition = function() {
    return al.H.useTransition();
  }, F.version = "19.2.5", F;
}
var kd;
function Zf() {
  return kd || (kd = 1, Uf.exports = jh()), Uf.exports;
}
var cl = Zf(), Cf = { exports: {} }, Ka = {}, jf = { exports: {} }, Rf = {};
/**
 * @license React
 * scheduler.production.js
 *
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 */
var $d;
function Rh() {
  return $d || ($d = 1, (function(c) {
    function o(_, H) {
      var J = _.length;
      _.push(H);
      l: for (; 0 < J; ) {
        var yl = J - 1 >>> 1, dl = _[yl];
        if (0 < T(dl, H))
          _[yl] = H, _[J] = dl, J = yl;
        else break l;
      }
    }
    function r(_) {
      return _.length === 0 ? null : _[0];
    }
    function s(_) {
      if (_.length === 0) return null;
      var H = _[0], J = _.pop();
      if (J !== H) {
        _[0] = J;
        l: for (var yl = 0, dl = _.length, v = dl >>> 1; yl < v; ) {
          var O = 2 * (yl + 1) - 1, R = _[O], Y = O + 1, $ = _[Y];
          if (0 > T(R, J))
            Y < dl && 0 > T($, R) ? (_[yl] = $, _[Y] = J, yl = Y) : (_[yl] = R, _[O] = J, yl = O);
          else if (Y < dl && 0 > T($, J))
            _[yl] = $, _[Y] = J, yl = Y;
          else break l;
        }
      }
      return H;
    }
    function T(_, H) {
      var J = _.sortIndex - H.sortIndex;
      return J !== 0 ? J : _.id - H.id;
    }
    if (c.unstable_now = void 0, typeof performance == "object" && typeof performance.now == "function") {
      var z = performance;
      c.unstable_now = function() {
        return z.now();
      };
    } else {
      var C = Date, U = C.now();
      c.unstable_now = function() {
        return C.now() - U;
      };
    }
    var x = [], b = [], j = 1, p = null, D = 3, X = !1, k = !1, K = !1, L = !1, Z = typeof setTimeout == "function" ? setTimeout : null, ol = typeof clearTimeout == "function" ? clearTimeout : null, W = typeof setImmediate < "u" ? setImmediate : null;
    function ll(_) {
      for (var H = r(b); H !== null; ) {
        if (H.callback === null) s(b);
        else if (H.startTime <= _)
          s(b), H.sortIndex = H.expirationTime, o(x, H);
        else break;
        H = r(b);
      }
    }
    function Wl(_) {
      if (K = !1, ll(_), !k)
        if (r(x) !== null)
          k = !0, I || (I = !0, Nl());
        else {
          var H = r(b);
          H !== null && Gl(Wl, H.startTime - _);
        }
    }
    var I = !1, al = -1, Yl = 5, ft = -1;
    function _t() {
      return L ? !0 : !(c.unstable_now() - ft < Yl);
    }
    function st() {
      if (L = !1, I) {
        var _ = c.unstable_now();
        ft = _;
        var H = !0;
        try {
          l: {
            k = !1, K && (K = !1, ol(al), al = -1), X = !0;
            var J = D;
            try {
              t: {
                for (ll(_), p = r(x); p !== null && !(p.expirationTime > _ && _t()); ) {
                  var yl = p.callback;
                  if (typeof yl == "function") {
                    p.callback = null, D = p.priorityLevel;
                    var dl = yl(
                      p.expirationTime <= _
                    );
                    if (_ = c.unstable_now(), typeof dl == "function") {
                      p.callback = dl, ll(_), H = !0;
                      break t;
                    }
                    p === r(x) && s(x), ll(_);
                  } else s(x);
                  p = r(x);
                }
                if (p !== null) H = !0;
                else {
                  var v = r(b);
                  v !== null && Gl(
                    Wl,
                    v.startTime - _
                  ), H = !1;
                }
              }
              break l;
            } finally {
              p = null, D = J, X = !1;
            }
            H = void 0;
          }
        } finally {
          H ? Nl() : I = !1;
        }
      }
    }
    var Nl;
    if (typeof W == "function")
      Nl = function() {
        W(st);
      };
    else if (typeof MessageChannel < "u") {
      var Jt = new MessageChannel(), jt = Jt.port2;
      Jt.port1.onmessage = st, Nl = function() {
        jt.postMessage(null);
      };
    } else
      Nl = function() {
        Z(st, 0);
      };
    function Gl(_, H) {
      al = Z(function() {
        _(c.unstable_now());
      }, H);
    }
    c.unstable_IdlePriority = 5, c.unstable_ImmediatePriority = 1, c.unstable_LowPriority = 4, c.unstable_NormalPriority = 3, c.unstable_Profiling = null, c.unstable_UserBlockingPriority = 2, c.unstable_cancelCallback = function(_) {
      _.callback = null;
    }, c.unstable_forceFrameRate = function(_) {
      0 > _ || 125 < _ ? console.error(
        "forceFrameRate takes a positive int between 0 and 125, forcing frame rates higher than 125 fps is not supported"
      ) : Yl = 0 < _ ? Math.floor(1e3 / _) : 5;
    }, c.unstable_getCurrentPriorityLevel = function() {
      return D;
    }, c.unstable_next = function(_) {
      switch (D) {
        case 1:
        case 2:
        case 3:
          var H = 3;
          break;
        default:
          H = D;
      }
      var J = D;
      D = H;
      try {
        return _();
      } finally {
        D = J;
      }
    }, c.unstable_requestPaint = function() {
      L = !0;
    }, c.unstable_runWithPriority = function(_, H) {
      switch (_) {
        case 1:
        case 2:
        case 3:
        case 4:
        case 5:
          break;
        default:
          _ = 3;
      }
      var J = D;
      D = _;
      try {
        return H();
      } finally {
        D = J;
      }
    }, c.unstable_scheduleCallback = function(_, H, J) {
      var yl = c.unstable_now();
      switch (typeof J == "object" && J !== null ? (J = J.delay, J = typeof J == "number" && 0 < J ? yl + J : yl) : J = yl, _) {
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
      return dl = J + dl, _ = {
        id: j++,
        callback: H,
        priorityLevel: _,
        startTime: J,
        expirationTime: dl,
        sortIndex: -1
      }, J > yl ? (_.sortIndex = J, o(b, _), r(x) === null && _ === r(b) && (K ? (ol(al), al = -1) : K = !0, Gl(Wl, J - yl))) : (_.sortIndex = dl, o(x, _), k || X || (k = !0, I || (I = !0, Nl()))), _;
    }, c.unstable_shouldYield = _t, c.unstable_wrapCallback = function(_) {
      var H = D;
      return function() {
        var J = D;
        D = H;
        try {
          return _.apply(this, arguments);
        } finally {
          D = J;
        }
      };
    };
  })(Rf)), Rf;
}
var Wd;
function Hh() {
  return Wd || (Wd = 1, jf.exports = Rh()), jf.exports;
}
var Hf = { exports: {} }, Fl = {};
/**
 * @license React
 * react-dom.production.js
 *
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 */
var Fd;
function Bh() {
  if (Fd) return Fl;
  Fd = 1;
  var c = Zf();
  function o(x) {
    var b = "https://react.dev/errors/" + x;
    if (1 < arguments.length) {
      b += "?args[]=" + encodeURIComponent(arguments[1]);
      for (var j = 2; j < arguments.length; j++)
        b += "&args[]=" + encodeURIComponent(arguments[j]);
    }
    return "Minified React error #" + x + "; visit " + b + " for the full message or use the non-minified dev environment for full errors and additional helpful warnings.";
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
  function z(x, b, j) {
    var p = 3 < arguments.length && arguments[3] !== void 0 ? arguments[3] : null;
    return {
      $$typeof: T,
      key: p == null ? null : "" + p,
      children: x,
      containerInfo: b,
      implementation: j
    };
  }
  var C = c.__CLIENT_INTERNALS_DO_NOT_USE_OR_WARN_USERS_THEY_CANNOT_UPGRADE;
  function U(x, b) {
    if (x === "font") return "";
    if (typeof b == "string")
      return b === "use-credentials" ? b : "";
  }
  return Fl.__DOM_INTERNALS_DO_NOT_USE_OR_WARN_USERS_THEY_CANNOT_UPGRADE = s, Fl.createPortal = function(x, b) {
    var j = 2 < arguments.length && arguments[2] !== void 0 ? arguments[2] : null;
    if (!b || b.nodeType !== 1 && b.nodeType !== 9 && b.nodeType !== 11)
      throw Error(o(299));
    return z(x, b, null, j);
  }, Fl.flushSync = function(x) {
    var b = C.T, j = s.p;
    try {
      if (C.T = null, s.p = 2, x) return x();
    } finally {
      C.T = b, s.p = j, s.d.f();
    }
  }, Fl.preconnect = function(x, b) {
    typeof x == "string" && (b ? (b = b.crossOrigin, b = typeof b == "string" ? b === "use-credentials" ? b : "" : void 0) : b = null, s.d.C(x, b));
  }, Fl.prefetchDNS = function(x) {
    typeof x == "string" && s.d.D(x);
  }, Fl.preinit = function(x, b) {
    if (typeof x == "string" && b && typeof b.as == "string") {
      var j = b.as, p = U(j, b.crossOrigin), D = typeof b.integrity == "string" ? b.integrity : void 0, X = typeof b.fetchPriority == "string" ? b.fetchPriority : void 0;
      j === "style" ? s.d.S(
        x,
        typeof b.precedence == "string" ? b.precedence : void 0,
        {
          crossOrigin: p,
          integrity: D,
          fetchPriority: X
        }
      ) : j === "script" && s.d.X(x, {
        crossOrigin: p,
        integrity: D,
        fetchPriority: X,
        nonce: typeof b.nonce == "string" ? b.nonce : void 0
      });
    }
  }, Fl.preinitModule = function(x, b) {
    if (typeof x == "string")
      if (typeof b == "object" && b !== null) {
        if (b.as == null || b.as === "script") {
          var j = U(
            b.as,
            b.crossOrigin
          );
          s.d.M(x, {
            crossOrigin: j,
            integrity: typeof b.integrity == "string" ? b.integrity : void 0,
            nonce: typeof b.nonce == "string" ? b.nonce : void 0
          });
        }
      } else b == null && s.d.M(x);
  }, Fl.preload = function(x, b) {
    if (typeof x == "string" && typeof b == "object" && b !== null && typeof b.as == "string") {
      var j = b.as, p = U(j, b.crossOrigin);
      s.d.L(x, j, {
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
  }, Fl.preloadModule = function(x, b) {
    if (typeof x == "string")
      if (b) {
        var j = U(b.as, b.crossOrigin);
        s.d.m(x, {
          as: typeof b.as == "string" && b.as !== "script" ? b.as : void 0,
          crossOrigin: j,
          integrity: typeof b.integrity == "string" ? b.integrity : void 0
        });
      } else s.d.m(x);
  }, Fl.requestFormReset = function(x) {
    s.d.r(x);
  }, Fl.unstable_batchedUpdates = function(x, b) {
    return x(b);
  }, Fl.useFormState = function(x, b, j) {
    return C.H.useFormState(x, b, j);
  }, Fl.useFormStatus = function() {
    return C.H.useHostTransitionStatus();
  }, Fl.version = "19.2.5", Fl;
}
var Id;
function Yh() {
  if (Id) return Hf.exports;
  Id = 1;
  function c() {
    if (!(typeof __REACT_DEVTOOLS_GLOBAL_HOOK__ > "u" || typeof __REACT_DEVTOOLS_GLOBAL_HOOK__.checkDCE != "function"))
      try {
        __REACT_DEVTOOLS_GLOBAL_HOOK__.checkDCE(c);
      } catch (o) {
        console.error(o);
      }
  }
  return c(), Hf.exports = Bh(), Hf.exports;
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
var Pd;
function Gh() {
  if (Pd) return Ka;
  Pd = 1;
  var c = Hh(), o = Zf(), r = Yh();
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
  function z(l) {
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
  function x(l) {
    if (z(l) !== l)
      throw Error(s(188));
  }
  function b(l) {
    var t = l.alternate;
    if (!t) {
      if (t = z(l), t === null) throw Error(s(188));
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
          if (n === e) return x(a), l;
          if (n === u) return x(a), t;
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
  function j(l) {
    var t = l.tag;
    if (t === 5 || t === 26 || t === 27 || t === 6) return l;
    for (l = l.child; l !== null; ) {
      if (t = j(l), t !== null) return t;
      l = l.sibling;
    }
    return null;
  }
  var p = Object.assign, D = Symbol.for("react.element"), X = Symbol.for("react.transitional.element"), k = Symbol.for("react.portal"), K = Symbol.for("react.fragment"), L = Symbol.for("react.strict_mode"), Z = Symbol.for("react.profiler"), ol = Symbol.for("react.consumer"), W = Symbol.for("react.context"), ll = Symbol.for("react.forward_ref"), Wl = Symbol.for("react.suspense"), I = Symbol.for("react.suspense_list"), al = Symbol.for("react.memo"), Yl = Symbol.for("react.lazy"), ft = Symbol.for("react.activity"), _t = Symbol.for("react.memo_cache_sentinel"), st = Symbol.iterator;
  function Nl(l) {
    return l === null || typeof l != "object" ? null : (l = st && l[st] || l["@@iterator"], typeof l == "function" ? l : null);
  }
  var Jt = Symbol.for("react.client.reference");
  function jt(l) {
    if (l == null) return null;
    if (typeof l == "function")
      return l.$$typeof === Jt ? null : l.displayName || l.name || null;
    if (typeof l == "string") return l;
    switch (l) {
      case K:
        return "Fragment";
      case Z:
        return "Profiler";
      case L:
        return "StrictMode";
      case Wl:
        return "Suspense";
      case I:
        return "SuspenseList";
      case ft:
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
        case ll:
          var t = l.render;
          return l = l.displayName, l || (l = t.displayName || t.name || "", l = l !== "" ? "ForwardRef(" + l + ")" : "ForwardRef"), l;
        case al:
          return t = l.displayName || null, t !== null ? t : jt(l.type) || "Memo";
        case Yl:
          t = l._payload, l = l._init;
          try {
            return jt(l(t));
          } catch {
          }
      }
    return null;
  }
  var Gl = Array.isArray, _ = o.__CLIENT_INTERNALS_DO_NOT_USE_OR_WARN_USERS_THEY_CANNOT_UPGRADE, H = r.__DOM_INTERNALS_DO_NOT_USE_OR_WARN_USERS_THEY_CANNOT_UPGRADE, J = {
    pending: !1,
    data: null,
    method: null,
    action: null
  }, yl = [], dl = -1;
  function v(l) {
    return { current: l };
  }
  function O(l) {
    0 > dl || (l.current = yl[dl], yl[dl] = null, dl--);
  }
  function R(l, t) {
    dl++, yl[dl] = l.current, l.current = t;
  }
  var Y = v(null), $ = v(null), ul = v(null), vl = v(null);
  function Vl(l, t) {
    switch (R(ul, t), R($, l), R(Y, null), t.nodeType) {
      case 9:
      case 11:
        l = (l = t.documentElement) && (l = l.namespaceURI) ? hd(l) : 0;
        break;
      default:
        if (l = t.tagName, t = t.namespaceURI)
          t = hd(t), l = md(t, l);
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
    O(Y), R(Y, l);
  }
  function G() {
    O(Y), O($), O(ul);
  }
  function Tl(l) {
    l.memoizedState !== null && R(vl, l);
    var t = Y.current, e = md(t, l.type);
    t !== e && (R($, l), R(Y, e));
  }
  function Et(l) {
    $.current === l && (O(Y), O($)), vl.current === l && (O(vl), Qa._currentValue = J);
  }
  var rt, Xe;
  function Lt(l) {
    if (rt === void 0)
      try {
        throw Error();
      } catch (e) {
        var t = e.stack.trim().match(/\n( *(at )?)/);
        rt = t && t[1] || "", Xe = -1 < e.stack.indexOf(`
    at`) ? " (<anonymous>)" : -1 < e.stack.indexOf("@") ? "@unknown:0:0" : "";
      }
    return `
` + rt + l + Xe;
  }
  var yi = !1;
  function vi(l, t) {
    if (!l || yi) return "";
    yi = !0;
    var e = Error.prepareStackTrace;
    Error.prepareStackTrace = void 0;
    try {
      var u = {
        DetermineComponentFrameRoot: function() {
          try {
            if (t) {
              var N = function() {
                throw Error();
              };
              if (Object.defineProperty(N.prototype, "props", {
                set: function() {
                  throw Error();
                }
              }), typeof Reflect == "object" && Reflect.construct) {
                try {
                  Reflect.construct(N, []);
                } catch (E) {
                  var S = E;
                }
                Reflect.construct(l, [], N);
              } else {
                try {
                  N.call();
                } catch (E) {
                  S = E;
                }
                l.call(N.prototype);
              }
            } else {
              try {
                throw Error();
              } catch (E) {
                S = E;
              }
              (N = l()) && typeof N.catch == "function" && N.catch(function() {
              });
            }
          } catch (E) {
            if (E && S && typeof E.stack == "string")
              return [E.stack, S.stack];
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
                  var A = `
` + d[u].replace(" at new ", " at ");
                  return l.displayName && A.includes("<anonymous>") && (A = A.replace("<anonymous>", l.displayName)), A;
                }
              while (1 <= u && 0 <= a);
            break;
          }
      }
    } finally {
      yi = !1, Error.prepareStackTrace = e;
    }
    return (e = l ? l.displayName || l.name : "") ? Lt(e) : "";
  }
  function sy(l, t) {
    switch (l.tag) {
      case 26:
      case 27:
      case 5:
        return Lt(l.type);
      case 16:
        return Lt("Lazy");
      case 13:
        return l.child !== t && t !== null ? Lt("Suspense Fallback") : Lt("Suspense");
      case 19:
        return Lt("SuspenseList");
      case 0:
      case 15:
        return vi(l.type, !1);
      case 11:
        return vi(l.type.render, !1);
      case 1:
        return vi(l.type, !0);
      case 31:
        return Lt("Activity");
      default:
        return "";
    }
  }
  function Kf(l) {
    try {
      var t = "", e = null;
      do
        t += sy(l, e), e = l, l = l.return;
      while (l);
      return t;
    } catch (u) {
      return `
Error generating stack: ` + u.message + `
` + u.stack;
    }
  }
  var hi = Object.prototype.hasOwnProperty, mi = c.unstable_scheduleCallback, gi = c.unstable_cancelCallback, ry = c.unstable_shouldYield, oy = c.unstable_requestPaint, ot = c.unstable_now, dy = c.unstable_getCurrentPriorityLevel, Jf = c.unstable_ImmediatePriority, wf = c.unstable_UserBlockingPriority, ka = c.unstable_NormalPriority, yy = c.unstable_LowPriority, kf = c.unstable_IdlePriority, vy = c.log, hy = c.unstable_setDisableYieldValue, Pu = null, dt = null;
  function ve(l) {
    if (typeof vy == "function" && hy(l), dt && typeof dt.setStrictMode == "function")
      try {
        dt.setStrictMode(Pu, l);
      } catch {
      }
  }
  var yt = Math.clz32 ? Math.clz32 : by, my = Math.log, gy = Math.LN2;
  function by(l) {
    return l >>>= 0, l === 0 ? 32 : 31 - (my(l) / gy | 0) | 0;
  }
  var $a = 256, Wa = 262144, Fa = 4194304;
  function Ze(l) {
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
  function Ia(l, t, e) {
    var u = l.pendingLanes;
    if (u === 0) return 0;
    var a = 0, n = l.suspendedLanes, i = l.pingedLanes;
    l = l.warmLanes;
    var f = u & 134217727;
    return f !== 0 ? (u = f & ~n, u !== 0 ? a = Ze(u) : (i &= f, i !== 0 ? a = Ze(i) : e || (e = f & ~l, e !== 0 && (a = Ze(e))))) : (f = u & ~n, f !== 0 ? a = Ze(f) : i !== 0 ? a = Ze(i) : e || (e = u & ~l, e !== 0 && (a = Ze(e)))), a === 0 ? 0 : t !== 0 && t !== a && (t & n) === 0 && (n = a & -a, e = t & -t, n >= e || n === 32 && (e & 4194048) !== 0) ? t : a;
  }
  function la(l, t) {
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
  function $f() {
    var l = Fa;
    return Fa <<= 1, (Fa & 62914560) === 0 && (Fa = 4194304), l;
  }
  function bi(l) {
    for (var t = [], e = 0; 31 > e; e++) t.push(l);
    return t;
  }
  function ta(l, t) {
    l.pendingLanes |= t, t !== 268435456 && (l.suspendedLanes = 0, l.pingedLanes = 0, l.warmLanes = 0);
  }
  function py(l, t, e, u, a, n) {
    var i = l.pendingLanes;
    l.pendingLanes = e, l.suspendedLanes = 0, l.pingedLanes = 0, l.warmLanes = 0, l.expiredLanes &= e, l.entangledLanes &= e, l.errorRecoveryDisabledLanes &= e, l.shellSuspendCounter = 0;
    var f = l.entanglements, d = l.expirationTimes, g = l.hiddenUpdates;
    for (e = i & ~e; 0 < e; ) {
      var A = 31 - yt(e), N = 1 << A;
      f[A] = 0, d[A] = -1;
      var S = g[A];
      if (S !== null)
        for (g[A] = null, A = 0; A < S.length; A++) {
          var E = S[A];
          E !== null && (E.lane &= -536870913);
        }
      e &= ~N;
    }
    u !== 0 && Wf(l, u, 0), n !== 0 && a === 0 && l.tag !== 0 && (l.suspendedLanes |= n & ~(i & ~t));
  }
  function Wf(l, t, e) {
    l.pendingLanes |= t, l.suspendedLanes &= ~t;
    var u = 31 - yt(t);
    l.entangledLanes |= t, l.entanglements[u] = l.entanglements[u] | 1073741824 | e & 261930;
  }
  function Ff(l, t) {
    var e = l.entangledLanes |= t;
    for (l = l.entanglements; e; ) {
      var u = 31 - yt(e), a = 1 << u;
      a & t | l[u] & t && (l[u] |= t), e &= ~a;
    }
  }
  function If(l, t) {
    var e = t & -t;
    return e = (e & 42) !== 0 ? 1 : Si(e), (e & (l.suspendedLanes | t)) !== 0 ? 0 : e;
  }
  function Si(l) {
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
  function pi(l) {
    return l &= -l, 2 < l ? 8 < l ? (l & 134217727) !== 0 ? 32 : 268435456 : 8 : 2;
  }
  function Pf() {
    var l = H.p;
    return l !== 0 ? l : (l = window.event, l === void 0 ? 32 : Gd(l.type));
  }
  function ls(l, t) {
    var e = H.p;
    try {
      return H.p = l, t();
    } finally {
      H.p = e;
    }
  }
  var he = Math.random().toString(36).slice(2), Kl = "__reactFiber$" + he, lt = "__reactProps$" + he, ru = "__reactContainer$" + he, _i = "__reactEvents$" + he, _y = "__reactListeners$" + he, Ey = "__reactHandles$" + he, ts = "__reactResources$" + he, ea = "__reactMarker$" + he;
  function Ei(l) {
    delete l[Kl], delete l[lt], delete l[_i], delete l[_y], delete l[Ey];
  }
  function ou(l) {
    var t = l[Kl];
    if (t) return t;
    for (var e = l.parentNode; e; ) {
      if (t = e[ru] || e[Kl]) {
        if (e = t.alternate, t.child !== null || e !== null && e.child !== null)
          for (l = zd(l); l !== null; ) {
            if (e = l[Kl]) return e;
            l = zd(l);
          }
        return t;
      }
      l = e, e = l.parentNode;
    }
    return null;
  }
  function du(l) {
    if (l = l[Kl] || l[ru]) {
      var t = l.tag;
      if (t === 5 || t === 6 || t === 13 || t === 31 || t === 26 || t === 27 || t === 3)
        return l;
    }
    return null;
  }
  function ua(l) {
    var t = l.tag;
    if (t === 5 || t === 26 || t === 27 || t === 6) return l.stateNode;
    throw Error(s(33));
  }
  function yu(l) {
    var t = l[ts];
    return t || (t = l[ts] = { hoistableStyles: /* @__PURE__ */ new Map(), hoistableScripts: /* @__PURE__ */ new Map() }), t;
  }
  function Ql(l) {
    l[ea] = !0;
  }
  var es = /* @__PURE__ */ new Set(), us = {};
  function Ve(l, t) {
    vu(l, t), vu(l + "Capture", t);
  }
  function vu(l, t) {
    for (us[l] = t, l = 0; l < t.length; l++)
      es.add(t[l]);
  }
  var zy = RegExp(
    "^[:A-Z_a-z\\u00C0-\\u00D6\\u00D8-\\u00F6\\u00F8-\\u02FF\\u0370-\\u037D\\u037F-\\u1FFF\\u200C-\\u200D\\u2070-\\u218F\\u2C00-\\u2FEF\\u3001-\\uD7FF\\uF900-\\uFDCF\\uFDF0-\\uFFFD][:A-Z_a-z\\u00C0-\\u00D6\\u00D8-\\u00F6\\u00F8-\\u02FF\\u0370-\\u037D\\u037F-\\u1FFF\\u200C-\\u200D\\u2070-\\u218F\\u2C00-\\u2FEF\\u3001-\\uD7FF\\uF900-\\uFDCF\\uFDF0-\\uFFFD\\-.0-9\\u00B7\\u0300-\\u036F\\u203F-\\u2040]*$"
  ), as = {}, ns = {};
  function Ay(l) {
    return hi.call(ns, l) ? !0 : hi.call(as, l) ? !1 : zy.test(l) ? ns[l] = !0 : (as[l] = !0, !1);
  }
  function Pa(l, t, e) {
    if (Ay(t))
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
  function ln(l, t, e) {
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
  function zt(l) {
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
  function is(l) {
    var t = l.type;
    return (l = l.nodeName) && l.toLowerCase() === "input" && (t === "checkbox" || t === "radio");
  }
  function qy(l, t, e) {
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
  function zi(l) {
    if (!l._valueTracker) {
      var t = is(l) ? "checked" : "value";
      l._valueTracker = qy(
        l,
        t,
        "" + l[t]
      );
    }
  }
  function cs(l) {
    if (!l) return !1;
    var t = l._valueTracker;
    if (!t) return !0;
    var e = t.getValue(), u = "";
    return l && (u = is(l) ? l.checked ? "true" : "false" : l.value), l = u, l !== e ? (t.setValue(l), !0) : !1;
  }
  function tn(l) {
    if (l = l || (typeof document < "u" ? document : void 0), typeof l > "u") return null;
    try {
      return l.activeElement || l.body;
    } catch {
      return l.body;
    }
  }
  var Ty = /[\n"\\]/g;
  function At(l) {
    return l.replace(
      Ty,
      function(t) {
        return "\\" + t.charCodeAt(0).toString(16) + " ";
      }
    );
  }
  function Ai(l, t, e, u, a, n, i, f) {
    l.name = "", i != null && typeof i != "function" && typeof i != "symbol" && typeof i != "boolean" ? l.type = i : l.removeAttribute("type"), t != null ? i === "number" ? (t === 0 && l.value === "" || l.value != t) && (l.value = "" + zt(t)) : l.value !== "" + zt(t) && (l.value = "" + zt(t)) : i !== "submit" && i !== "reset" || l.removeAttribute("value"), t != null ? qi(l, i, zt(t)) : e != null ? qi(l, i, zt(e)) : u != null && l.removeAttribute("value"), a == null && n != null && (l.defaultChecked = !!n), a != null && (l.checked = a && typeof a != "function" && typeof a != "symbol"), f != null && typeof f != "function" && typeof f != "symbol" && typeof f != "boolean" ? l.name = "" + zt(f) : l.removeAttribute("name");
  }
  function fs(l, t, e, u, a, n, i, f) {
    if (n != null && typeof n != "function" && typeof n != "symbol" && typeof n != "boolean" && (l.type = n), t != null || e != null) {
      if (!(n !== "submit" && n !== "reset" || t != null)) {
        zi(l);
        return;
      }
      e = e != null ? "" + zt(e) : "", t = t != null ? "" + zt(t) : e, f || t === l.value || (l.value = t), l.defaultValue = t;
    }
    u = u ?? a, u = typeof u != "function" && typeof u != "symbol" && !!u, l.checked = f ? l.checked : !!u, l.defaultChecked = !!u, i != null && typeof i != "function" && typeof i != "symbol" && typeof i != "boolean" && (l.name = i), zi(l);
  }
  function qi(l, t, e) {
    t === "number" && tn(l.ownerDocument) === l || l.defaultValue === "" + e || (l.defaultValue = "" + e);
  }
  function hu(l, t, e, u) {
    if (l = l.options, t) {
      t = {};
      for (var a = 0; a < e.length; a++)
        t["$" + e[a]] = !0;
      for (e = 0; e < l.length; e++)
        a = t.hasOwnProperty("$" + l[e].value), l[e].selected !== a && (l[e].selected = a), a && u && (l[e].defaultSelected = !0);
    } else {
      for (e = "" + zt(e), t = null, a = 0; a < l.length; a++) {
        if (l[a].value === e) {
          l[a].selected = !0, u && (l[a].defaultSelected = !0);
          return;
        }
        t !== null || l[a].disabled || (t = l[a]);
      }
      t !== null && (t.selected = !0);
    }
  }
  function ss(l, t, e) {
    if (t != null && (t = "" + zt(t), t !== l.value && (l.value = t), e == null)) {
      l.defaultValue !== t && (l.defaultValue = t);
      return;
    }
    l.defaultValue = e != null ? "" + zt(e) : "";
  }
  function rs(l, t, e, u) {
    if (t == null) {
      if (u != null) {
        if (e != null) throw Error(s(92));
        if (Gl(u)) {
          if (1 < u.length) throw Error(s(93));
          u = u[0];
        }
        e = u;
      }
      e == null && (e = ""), t = e;
    }
    e = zt(t), l.defaultValue = e, u = l.textContent, u === e && u !== "" && u !== null && (l.value = u), zi(l);
  }
  function mu(l, t) {
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
  function os(l, t, e) {
    var u = t.indexOf("--") === 0;
    e == null || typeof e == "boolean" || e === "" ? u ? l.setProperty(t, "") : t === "float" ? l.cssFloat = "" : l[t] = "" : u ? l.setProperty(t, e) : typeof e != "number" || e === 0 || xy.has(t) ? t === "float" ? l.cssFloat = e : l[t] = ("" + e).trim() : l[t] = e + "px";
  }
  function ds(l, t, e) {
    if (t != null && typeof t != "object")
      throw Error(s(62));
    if (l = l.style, e != null) {
      for (var u in e)
        !e.hasOwnProperty(u) || t != null && t.hasOwnProperty(u) || (u.indexOf("--") === 0 ? l.setProperty(u, "") : u === "float" ? l.cssFloat = "" : l[u] = "");
      for (var a in t)
        u = t[a], t.hasOwnProperty(a) && e[a] !== u && os(l, a, u);
    } else
      for (var n in t)
        t.hasOwnProperty(n) && os(l, n, t[n]);
  }
  function Ti(l) {
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
  function en(l) {
    return Oy.test("" + l) ? "javascript:throw new Error('React has blocked a javascript: URL as a security precaution.')" : l;
  }
  function kt() {
  }
  var xi = null;
  function Ni(l) {
    return l = l.target || l.srcElement || window, l.correspondingUseElement && (l = l.correspondingUseElement), l.nodeType === 3 ? l.parentNode : l;
  }
  var gu = null, bu = null;
  function ys(l) {
    var t = du(l);
    if (t && (l = t.stateNode)) {
      var e = l[lt] || null;
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
              'input[name="' + At(
                "" + t
              ) + '"][type="radio"]'
            ), t = 0; t < e.length; t++) {
              var u = e[t];
              if (u !== l && u.form === l.form) {
                var a = u[lt] || null;
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
              u = e[t], u.form === l.form && cs(u);
          }
          break l;
        case "textarea":
          ss(l, e.value, e.defaultValue);
          break l;
        case "select":
          t = e.value, t != null && hu(l, !!e.multiple, t, !1);
      }
    }
  }
  var Oi = !1;
  function vs(l, t, e) {
    if (Oi) return l(t, e);
    Oi = !0;
    try {
      var u = l(t);
      return u;
    } finally {
      if (Oi = !1, (gu !== null || bu !== null) && (Vn(), gu && (t = gu, l = bu, bu = gu = null, ys(t), l)))
        for (t = 0; t < l.length; t++) ys(l[t]);
    }
  }
  function aa(l, t) {
    var e = l.stateNode;
    if (e === null) return null;
    var u = e[lt] || null;
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
  var $t = !(typeof window > "u" || typeof window.document > "u" || typeof window.document.createElement > "u"), Mi = !1;
  if ($t)
    try {
      var na = {};
      Object.defineProperty(na, "passive", {
        get: function() {
          Mi = !0;
        }
      }), window.addEventListener("test", na, na), window.removeEventListener("test", na, na);
    } catch {
      Mi = !1;
    }
  var me = null, Di = null, un = null;
  function hs() {
    if (un) return un;
    var l, t = Di, e = t.length, u, a = "value" in me ? me.value : me.textContent, n = a.length;
    for (l = 0; l < e && t[l] === a[l]; l++) ;
    var i = e - l;
    for (u = 1; u <= i && t[e - u] === a[n - u]; u++) ;
    return un = a.slice(l, 1 < u ? 1 - u : void 0);
  }
  function an(l) {
    var t = l.keyCode;
    return "charCode" in l ? (l = l.charCode, l === 0 && t === 13 && (l = 13)) : l = t, l === 10 && (l = 13), 32 <= l || l === 13 ? l : 0;
  }
  function nn() {
    return !0;
  }
  function ms() {
    return !1;
  }
  function tt(l) {
    function t(e, u, a, n, i) {
      this._reactName = e, this._targetInst = a, this.type = u, this.nativeEvent = n, this.target = i, this.currentTarget = null;
      for (var f in l)
        l.hasOwnProperty(f) && (e = l[f], this[f] = e ? e(n) : n[f]);
      return this.isDefaultPrevented = (n.defaultPrevented != null ? n.defaultPrevented : n.returnValue === !1) ? nn : ms, this.isPropagationStopped = ms, this;
    }
    return p(t.prototype, {
      preventDefault: function() {
        this.defaultPrevented = !0;
        var e = this.nativeEvent;
        e && (e.preventDefault ? e.preventDefault() : typeof e.returnValue != "unknown" && (e.returnValue = !1), this.isDefaultPrevented = nn);
      },
      stopPropagation: function() {
        var e = this.nativeEvent;
        e && (e.stopPropagation ? e.stopPropagation() : typeof e.cancelBubble != "unknown" && (e.cancelBubble = !0), this.isPropagationStopped = nn);
      },
      persist: function() {
      },
      isPersistent: nn
    }), t;
  }
  var Ke = {
    eventPhase: 0,
    bubbles: 0,
    cancelable: 0,
    timeStamp: function(l) {
      return l.timeStamp || Date.now();
    },
    defaultPrevented: 0,
    isTrusted: 0
  }, cn = tt(Ke), ia = p({}, Ke, { view: 0, detail: 0 }), My = tt(ia), Ui, Ci, ca, fn = p({}, ia, {
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
    getModifierState: Ri,
    button: 0,
    buttons: 0,
    relatedTarget: function(l) {
      return l.relatedTarget === void 0 ? l.fromElement === l.srcElement ? l.toElement : l.fromElement : l.relatedTarget;
    },
    movementX: function(l) {
      return "movementX" in l ? l.movementX : (l !== ca && (ca && l.type === "mousemove" ? (Ui = l.screenX - ca.screenX, Ci = l.screenY - ca.screenY) : Ci = Ui = 0, ca = l), Ui);
    },
    movementY: function(l) {
      return "movementY" in l ? l.movementY : Ci;
    }
  }), gs = tt(fn), Dy = p({}, fn, { dataTransfer: 0 }), Uy = tt(Dy), Cy = p({}, ia, { relatedTarget: 0 }), ji = tt(Cy), jy = p({}, Ke, {
    animationName: 0,
    elapsedTime: 0,
    pseudoElement: 0
  }), Ry = tt(jy), Hy = p({}, Ke, {
    clipboardData: function(l) {
      return "clipboardData" in l ? l.clipboardData : window.clipboardData;
    }
  }), By = tt(Hy), Yy = p({}, Ke, { data: 0 }), bs = tt(Yy), Gy = {
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
  }, Ly = {
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
  function Ri() {
    return Xy;
  }
  var Zy = p({}, ia, {
    key: function(l) {
      if (l.key) {
        var t = Gy[l.key] || l.key;
        if (t !== "Unidentified") return t;
      }
      return l.type === "keypress" ? (l = an(l), l === 13 ? "Enter" : String.fromCharCode(l)) : l.type === "keydown" || l.type === "keyup" ? Ly[l.keyCode] || "Unidentified" : "";
    },
    code: 0,
    location: 0,
    ctrlKey: 0,
    shiftKey: 0,
    altKey: 0,
    metaKey: 0,
    repeat: 0,
    locale: 0,
    getModifierState: Ri,
    charCode: function(l) {
      return l.type === "keypress" ? an(l) : 0;
    },
    keyCode: function(l) {
      return l.type === "keydown" || l.type === "keyup" ? l.keyCode : 0;
    },
    which: function(l) {
      return l.type === "keypress" ? an(l) : l.type === "keydown" || l.type === "keyup" ? l.keyCode : 0;
    }
  }), Vy = tt(Zy), Ky = p({}, fn, {
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
  }), Ss = tt(Ky), Jy = p({}, ia, {
    touches: 0,
    targetTouches: 0,
    changedTouches: 0,
    altKey: 0,
    metaKey: 0,
    ctrlKey: 0,
    shiftKey: 0,
    getModifierState: Ri
  }), wy = tt(Jy), ky = p({}, Ke, {
    propertyName: 0,
    elapsedTime: 0,
    pseudoElement: 0
  }), $y = tt(ky), Wy = p({}, fn, {
    deltaX: function(l) {
      return "deltaX" in l ? l.deltaX : "wheelDeltaX" in l ? -l.wheelDeltaX : 0;
    },
    deltaY: function(l) {
      return "deltaY" in l ? l.deltaY : "wheelDeltaY" in l ? -l.wheelDeltaY : "wheelDelta" in l ? -l.wheelDelta : 0;
    },
    deltaZ: 0,
    deltaMode: 0
  }), Fy = tt(Wy), Iy = p({}, Ke, {
    newState: 0,
    oldState: 0
  }), Py = tt(Iy), lv = [9, 13, 27, 32], Hi = $t && "CompositionEvent" in window, fa = null;
  $t && "documentMode" in document && (fa = document.documentMode);
  var tv = $t && "TextEvent" in window && !fa, ps = $t && (!Hi || fa && 8 < fa && 11 >= fa), _s = " ", Es = !1;
  function zs(l, t) {
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
  var Su = !1;
  function ev(l, t) {
    switch (l) {
      case "compositionend":
        return As(t);
      case "keypress":
        return t.which !== 32 ? null : (Es = !0, _s);
      case "textInput":
        return l = t.data, l === _s && Es ? null : l;
      default:
        return null;
    }
  }
  function uv(l, t) {
    if (Su)
      return l === "compositionend" || !Hi && zs(l, t) ? (l = hs(), un = Di = me = null, Su = !1, l) : null;
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
        return ps && t.locale !== "ko" ? null : t.data;
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
  function qs(l) {
    var t = l && l.nodeName && l.nodeName.toLowerCase();
    return t === "input" ? !!av[l.type] : t === "textarea";
  }
  function Ts(l, t, e, u) {
    gu ? bu ? bu.push(u) : bu = [u] : gu = u, t = Fn(t, "onChange"), 0 < t.length && (e = new cn(
      "onChange",
      "change",
      null,
      e,
      u
    ), l.push({ event: e, listeners: t }));
  }
  var sa = null, ra = null;
  function nv(l) {
    sd(l, 0);
  }
  function sn(l) {
    var t = ua(l);
    if (cs(t)) return l;
  }
  function xs(l, t) {
    if (l === "change") return t;
  }
  var Ns = !1;
  if ($t) {
    var Bi;
    if ($t) {
      var Yi = "oninput" in document;
      if (!Yi) {
        var Os = document.createElement("div");
        Os.setAttribute("oninput", "return;"), Yi = typeof Os.oninput == "function";
      }
      Bi = Yi;
    } else Bi = !1;
    Ns = Bi && (!document.documentMode || 9 < document.documentMode);
  }
  function Ms() {
    sa && (sa.detachEvent("onpropertychange", Ds), ra = sa = null);
  }
  function Ds(l) {
    if (l.propertyName === "value" && sn(ra)) {
      var t = [];
      Ts(
        t,
        ra,
        l,
        Ni(l)
      ), vs(nv, t);
    }
  }
  function iv(l, t, e) {
    l === "focusin" ? (Ms(), sa = t, ra = e, sa.attachEvent("onpropertychange", Ds)) : l === "focusout" && Ms();
  }
  function cv(l) {
    if (l === "selectionchange" || l === "keyup" || l === "keydown")
      return sn(ra);
  }
  function fv(l, t) {
    if (l === "click") return sn(t);
  }
  function sv(l, t) {
    if (l === "input" || l === "change")
      return sn(t);
  }
  function rv(l, t) {
    return l === t && (l !== 0 || 1 / l === 1 / t) || l !== l && t !== t;
  }
  var vt = typeof Object.is == "function" ? Object.is : rv;
  function oa(l, t) {
    if (vt(l, t)) return !0;
    if (typeof l != "object" || l === null || typeof t != "object" || t === null)
      return !1;
    var e = Object.keys(l), u = Object.keys(t);
    if (e.length !== u.length) return !1;
    for (u = 0; u < e.length; u++) {
      var a = e[u];
      if (!hi.call(t, a) || !vt(l[a], t[a]))
        return !1;
    }
    return !0;
  }
  function Us(l) {
    for (; l && l.firstChild; ) l = l.firstChild;
    return l;
  }
  function Cs(l, t) {
    var e = Us(l);
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
      e = Us(e);
    }
  }
  function js(l, t) {
    return l && t ? l === t ? !0 : l && l.nodeType === 3 ? !1 : t && t.nodeType === 3 ? js(l, t.parentNode) : "contains" in l ? l.contains(t) : l.compareDocumentPosition ? !!(l.compareDocumentPosition(t) & 16) : !1 : !1;
  }
  function Rs(l) {
    l = l != null && l.ownerDocument != null && l.ownerDocument.defaultView != null ? l.ownerDocument.defaultView : window;
    for (var t = tn(l.document); t instanceof l.HTMLIFrameElement; ) {
      try {
        var e = typeof t.contentWindow.location.href == "string";
      } catch {
        e = !1;
      }
      if (e) l = t.contentWindow;
      else break;
      t = tn(l.document);
    }
    return t;
  }
  function Gi(l) {
    var t = l && l.nodeName && l.nodeName.toLowerCase();
    return t && (t === "input" && (l.type === "text" || l.type === "search" || l.type === "tel" || l.type === "url" || l.type === "password") || t === "textarea" || l.contentEditable === "true");
  }
  var ov = $t && "documentMode" in document && 11 >= document.documentMode, pu = null, Li = null, da = null, Qi = !1;
  function Hs(l, t, e) {
    var u = e.window === e ? e.document : e.nodeType === 9 ? e : e.ownerDocument;
    Qi || pu == null || pu !== tn(u) || (u = pu, "selectionStart" in u && Gi(u) ? u = { start: u.selectionStart, end: u.selectionEnd } : (u = (u.ownerDocument && u.ownerDocument.defaultView || window).getSelection(), u = {
      anchorNode: u.anchorNode,
      anchorOffset: u.anchorOffset,
      focusNode: u.focusNode,
      focusOffset: u.focusOffset
    }), da && oa(da, u) || (da = u, u = Fn(Li, "onSelect"), 0 < u.length && (t = new cn(
      "onSelect",
      "select",
      null,
      t,
      e
    ), l.push({ event: t, listeners: u }), t.target = pu)));
  }
  function Je(l, t) {
    var e = {};
    return e[l.toLowerCase()] = t.toLowerCase(), e["Webkit" + l] = "webkit" + t, e["Moz" + l] = "moz" + t, e;
  }
  var _u = {
    animationend: Je("Animation", "AnimationEnd"),
    animationiteration: Je("Animation", "AnimationIteration"),
    animationstart: Je("Animation", "AnimationStart"),
    transitionrun: Je("Transition", "TransitionRun"),
    transitionstart: Je("Transition", "TransitionStart"),
    transitioncancel: Je("Transition", "TransitionCancel"),
    transitionend: Je("Transition", "TransitionEnd")
  }, Xi = {}, Bs = {};
  $t && (Bs = document.createElement("div").style, "AnimationEvent" in window || (delete _u.animationend.animation, delete _u.animationiteration.animation, delete _u.animationstart.animation), "TransitionEvent" in window || delete _u.transitionend.transition);
  function we(l) {
    if (Xi[l]) return Xi[l];
    if (!_u[l]) return l;
    var t = _u[l], e;
    for (e in t)
      if (t.hasOwnProperty(e) && e in Bs)
        return Xi[l] = t[e];
    return l;
  }
  var Ys = we("animationend"), Gs = we("animationiteration"), Ls = we("animationstart"), dv = we("transitionrun"), yv = we("transitionstart"), vv = we("transitioncancel"), Qs = we("transitionend"), Xs = /* @__PURE__ */ new Map(), Zi = "abort auxClick beforeToggle cancel canPlay canPlayThrough click close contextMenu copy cut drag dragEnd dragEnter dragExit dragLeave dragOver dragStart drop durationChange emptied encrypted ended error gotPointerCapture input invalid keyDown keyPress keyUp load loadedData loadedMetadata loadStart lostPointerCapture mouseDown mouseMove mouseOut mouseOver mouseUp paste pause play playing pointerCancel pointerDown pointerMove pointerOut pointerOver pointerUp progress rateChange reset resize seeked seeking stalled submit suspend timeUpdate touchCancel touchEnd touchStart volumeChange scroll toggle touchMove waiting wheel".split(
    " "
  );
  Zi.push("scrollEnd");
  function Rt(l, t) {
    Xs.set(l, t), Ve(t, [l]);
  }
  var rn = typeof reportError == "function" ? reportError : function(l) {
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
  }, qt = [], Eu = 0, Vi = 0;
  function on() {
    for (var l = Eu, t = Vi = Eu = 0; t < l; ) {
      var e = qt[t];
      qt[t++] = null;
      var u = qt[t];
      qt[t++] = null;
      var a = qt[t];
      qt[t++] = null;
      var n = qt[t];
      if (qt[t++] = null, u !== null && a !== null) {
        var i = u.pending;
        i === null ? a.next = a : (a.next = i.next, i.next = a), u.pending = a;
      }
      n !== 0 && Zs(e, a, n);
    }
  }
  function dn(l, t, e, u) {
    qt[Eu++] = l, qt[Eu++] = t, qt[Eu++] = e, qt[Eu++] = u, Vi |= u, l.lanes |= u, l = l.alternate, l !== null && (l.lanes |= u);
  }
  function Ki(l, t, e, u) {
    return dn(l, t, e, u), yn(l);
  }
  function ke(l, t) {
    return dn(l, null, null, t), yn(l);
  }
  function Zs(l, t, e) {
    l.lanes |= e;
    var u = l.alternate;
    u !== null && (u.lanes |= e);
    for (var a = !1, n = l.return; n !== null; )
      n.childLanes |= e, u = n.alternate, u !== null && (u.childLanes |= e), n.tag === 22 && (l = n.stateNode, l === null || l._visibility & 1 || (a = !0)), l = n, n = n.return;
    return l.tag === 3 ? (n = l.stateNode, a && t !== null && (a = 31 - yt(e), l = n.hiddenUpdates, u = l[a], u === null ? l[a] = [t] : u.push(t), t.lane = e | 536870912), n) : null;
  }
  function yn(l) {
    if (50 < ja)
      throw ja = 0, lf = null, Error(s(185));
    for (var t = l.return; t !== null; )
      l = t, t = l.return;
    return l.tag === 3 ? l.stateNode : null;
  }
  var zu = {};
  function hv(l, t, e, u) {
    this.tag = l, this.key = e, this.sibling = this.child = this.return = this.stateNode = this.type = this.elementType = null, this.index = 0, this.refCleanup = this.ref = null, this.pendingProps = t, this.dependencies = this.memoizedState = this.updateQueue = this.memoizedProps = null, this.mode = u, this.subtreeFlags = this.flags = 0, this.deletions = null, this.childLanes = this.lanes = 0, this.alternate = null;
  }
  function ht(l, t, e, u) {
    return new hv(l, t, e, u);
  }
  function Ji(l) {
    return l = l.prototype, !(!l || !l.isReactComponent);
  }
  function Wt(l, t) {
    var e = l.alternate;
    return e === null ? (e = ht(
      l.tag,
      t,
      l.key,
      l.mode
    ), e.elementType = l.elementType, e.type = l.type, e.stateNode = l.stateNode, e.alternate = l, l.alternate = e) : (e.pendingProps = t, e.type = l.type, e.flags = 0, e.subtreeFlags = 0, e.deletions = null), e.flags = l.flags & 65011712, e.childLanes = l.childLanes, e.lanes = l.lanes, e.child = l.child, e.memoizedProps = l.memoizedProps, e.memoizedState = l.memoizedState, e.updateQueue = l.updateQueue, t = l.dependencies, e.dependencies = t === null ? null : { lanes: t.lanes, firstContext: t.firstContext }, e.sibling = l.sibling, e.index = l.index, e.ref = l.ref, e.refCleanup = l.refCleanup, e;
  }
  function Vs(l, t) {
    l.flags &= 65011714;
    var e = l.alternate;
    return e === null ? (l.childLanes = 0, l.lanes = t, l.child = null, l.subtreeFlags = 0, l.memoizedProps = null, l.memoizedState = null, l.updateQueue = null, l.dependencies = null, l.stateNode = null) : (l.childLanes = e.childLanes, l.lanes = e.lanes, l.child = e.child, l.subtreeFlags = 0, l.deletions = null, l.memoizedProps = e.memoizedProps, l.memoizedState = e.memoizedState, l.updateQueue = e.updateQueue, l.type = e.type, t = e.dependencies, l.dependencies = t === null ? null : {
      lanes: t.lanes,
      firstContext: t.firstContext
    }), l;
  }
  function vn(l, t, e, u, a, n) {
    var i = 0;
    if (u = l, typeof l == "function") Ji(l) && (i = 1);
    else if (typeof l == "string")
      i = ph(
        l,
        e,
        Y.current
      ) ? 26 : l === "html" || l === "head" || l === "body" ? 27 : 5;
    else
      l: switch (l) {
        case ft:
          return l = ht(31, e, t, a), l.elementType = ft, l.lanes = n, l;
        case K:
          return $e(e.children, a, n, t);
        case L:
          i = 8, a |= 24;
          break;
        case Z:
          return l = ht(12, e, t, a | 2), l.elementType = Z, l.lanes = n, l;
        case Wl:
          return l = ht(13, e, t, a), l.elementType = Wl, l.lanes = n, l;
        case I:
          return l = ht(19, e, t, a), l.elementType = I, l.lanes = n, l;
        default:
          if (typeof l == "object" && l !== null)
            switch (l.$$typeof) {
              case W:
                i = 10;
                break l;
              case ol:
                i = 9;
                break l;
              case ll:
                i = 11;
                break l;
              case al:
                i = 14;
                break l;
              case Yl:
                i = 16, u = null;
                break l;
            }
          i = 29, e = Error(
            s(130, l === null ? "null" : typeof l, "")
          ), u = null;
      }
    return t = ht(i, e, t, a), t.elementType = l, t.type = u, t.lanes = n, t;
  }
  function $e(l, t, e, u) {
    return l = ht(7, l, u, t), l.lanes = e, l;
  }
  function wi(l, t, e) {
    return l = ht(6, l, null, t), l.lanes = e, l;
  }
  function Ks(l) {
    var t = ht(18, null, null, 0);
    return t.stateNode = l, t;
  }
  function ki(l, t, e) {
    return t = ht(
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
  var Js = /* @__PURE__ */ new WeakMap();
  function Tt(l, t) {
    if (typeof l == "object" && l !== null) {
      var e = Js.get(l);
      return e !== void 0 ? e : (t = {
        value: l,
        source: t,
        stack: Kf(t)
      }, Js.set(l, t), t);
    }
    return {
      value: l,
      source: t,
      stack: Kf(t)
    };
  }
  var Au = [], qu = 0, hn = null, ya = 0, xt = [], Nt = 0, ge = null, Qt = 1, Xt = "";
  function Ft(l, t) {
    Au[qu++] = ya, Au[qu++] = hn, hn = l, ya = t;
  }
  function ws(l, t, e) {
    xt[Nt++] = Qt, xt[Nt++] = Xt, xt[Nt++] = ge, ge = l;
    var u = Qt;
    l = Xt;
    var a = 32 - yt(u) - 1;
    u &= ~(1 << a), e += 1;
    var n = 32 - yt(t) + a;
    if (30 < n) {
      var i = a - a % 5;
      n = (u & (1 << i) - 1).toString(32), u >>= i, a -= i, Qt = 1 << 32 - yt(t) + a | e << a | u, Xt = n + l;
    } else
      Qt = 1 << n | e << a | u, Xt = l;
  }
  function $i(l) {
    l.return !== null && (Ft(l, 1), ws(l, 1, 0));
  }
  function Wi(l) {
    for (; l === hn; )
      hn = Au[--qu], Au[qu] = null, ya = Au[--qu], Au[qu] = null;
    for (; l === ge; )
      ge = xt[--Nt], xt[Nt] = null, Xt = xt[--Nt], xt[Nt] = null, Qt = xt[--Nt], xt[Nt] = null;
  }
  function ks(l, t) {
    xt[Nt++] = Qt, xt[Nt++] = Xt, xt[Nt++] = ge, Qt = t.id, Xt = t.overflow, ge = l;
  }
  var Jl = null, Al = null, rl = !1, be = null, Ot = !1, Fi = Error(s(519));
  function Se(l) {
    var t = Error(
      s(
        418,
        1 < arguments.length && arguments[1] !== void 0 && arguments[1] ? "text" : "HTML",
        ""
      )
    );
    throw va(Tt(t, l)), Fi;
  }
  function $s(l) {
    var t = l.stateNode, e = l.type, u = l.memoizedProps;
    switch (t[Kl] = l, t[lt] = u, e) {
      case "dialog":
        il("cancel", t), il("close", t);
        break;
      case "iframe":
      case "object":
      case "embed":
        il("load", t);
        break;
      case "video":
      case "audio":
        for (e = 0; e < Ha.length; e++)
          il(Ha[e], t);
        break;
      case "source":
        il("error", t);
        break;
      case "img":
      case "image":
      case "link":
        il("error", t), il("load", t);
        break;
      case "details":
        il("toggle", t);
        break;
      case "input":
        il("invalid", t), fs(
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
        il("invalid", t);
        break;
      case "textarea":
        il("invalid", t), rs(t, u.value, u.defaultValue, u.children);
    }
    e = u.children, typeof e != "string" && typeof e != "number" && typeof e != "bigint" || t.textContent === "" + e || u.suppressHydrationWarning === !0 || yd(t.textContent, e) ? (u.popover != null && (il("beforetoggle", t), il("toggle", t)), u.onScroll != null && il("scroll", t), u.onScrollEnd != null && il("scrollend", t), u.onClick != null && (t.onclick = kt), t = !0) : t = !1, t || Se(l, !0);
  }
  function Ws(l) {
    for (Jl = l.return; Jl; )
      switch (Jl.tag) {
        case 5:
        case 31:
        case 13:
          Ot = !1;
          return;
        case 27:
        case 3:
          Ot = !0;
          return;
        default:
          Jl = Jl.return;
      }
  }
  function Tu(l) {
    if (l !== Jl) return !1;
    if (!rl) return Ws(l), rl = !0, !1;
    var t = l.tag, e;
    if ((e = t !== 3 && t !== 27) && ((e = t === 5) && (e = l.type, e = !(e !== "form" && e !== "button") || mf(l.type, l.memoizedProps)), e = !e), e && Al && Se(l), Ws(l), t === 13) {
      if (l = l.memoizedState, l = l !== null ? l.dehydrated : null, !l) throw Error(s(317));
      Al = Ed(l);
    } else if (t === 31) {
      if (l = l.memoizedState, l = l !== null ? l.dehydrated : null, !l) throw Error(s(317));
      Al = Ed(l);
    } else
      t === 27 ? (t = Al, Ce(l.type) ? (l = _f, _f = null, Al = l) : Al = t) : Al = Jl ? Dt(l.stateNode.nextSibling) : null;
    return !0;
  }
  function We() {
    Al = Jl = null, rl = !1;
  }
  function Ii() {
    var l = be;
    return l !== null && (nt === null ? nt = l : nt.push.apply(
      nt,
      l
    ), be = null), l;
  }
  function va(l) {
    be === null ? be = [l] : be.push(l);
  }
  var Pi = v(null), Fe = null, It = null;
  function pe(l, t, e) {
    R(Pi, t._currentValue), t._currentValue = e;
  }
  function Pt(l) {
    l._currentValue = Pi.current, O(Pi);
  }
  function lc(l, t, e) {
    for (; l !== null; ) {
      var u = l.alternate;
      if ((l.childLanes & t) !== t ? (l.childLanes |= t, u !== null && (u.childLanes |= t)) : u !== null && (u.childLanes & t) !== t && (u.childLanes |= t), l === e) break;
      l = l.return;
    }
  }
  function tc(l, t, e, u) {
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
              n.lanes |= e, f = n.alternate, f !== null && (f.lanes |= e), lc(
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
        i.lanes |= e, n = i.alternate, n !== null && (n.lanes |= e), lc(i, e, l), i = null;
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
  function xu(l, t, e, u) {
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
          vt(a.pendingProps.value, i.value) || (l !== null ? l.push(f) : l = [f]);
        }
      } else if (a === vl.current) {
        if (i = a.alternate, i === null) throw Error(s(387));
        i.memoizedState.memoizedState !== a.memoizedState.memoizedState && (l !== null ? l.push(Qa) : l = [Qa]);
      }
      a = a.return;
    }
    l !== null && tc(
      t,
      l,
      e,
      u
    ), t.flags |= 262144;
  }
  function mn(l) {
    for (l = l.firstContext; l !== null; ) {
      if (!vt(
        l.context._currentValue,
        l.memoizedValue
      ))
        return !0;
      l = l.next;
    }
    return !1;
  }
  function Ie(l) {
    Fe = l, It = null, l = l.dependencies, l !== null && (l.firstContext = null);
  }
  function wl(l) {
    return Fs(Fe, l);
  }
  function gn(l, t) {
    return Fe === null && Ie(l), Fs(l, t);
  }
  function Fs(l, t) {
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
  }, gv = c.unstable_scheduleCallback, bv = c.unstable_NormalPriority, jl = {
    $$typeof: W,
    Consumer: null,
    Provider: null,
    _currentValue: null,
    _currentValue2: null,
    _threadCount: 0
  };
  function ec() {
    return {
      controller: new mv(),
      data: /* @__PURE__ */ new Map(),
      refCount: 0
    };
  }
  function ha(l) {
    l.refCount--, l.refCount === 0 && gv(bv, function() {
      l.controller.abort();
    });
  }
  var ma = null, uc = 0, Nu = 0, Ou = null;
  function Sv(l, t) {
    if (ma === null) {
      var e = ma = [];
      uc = 0, Nu = cf(), Ou = {
        status: "pending",
        value: void 0,
        then: function(u) {
          e.push(u);
        }
      };
    }
    return uc++, t.then(Is, Is), t;
  }
  function Is() {
    if (--uc === 0 && ma !== null) {
      Ou !== null && (Ou.status = "fulfilled");
      var l = ma;
      ma = null, Nu = 0, Ou = null;
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
  var Ps = _.S;
  _.S = function(l, t) {
    Bo = ot(), typeof t == "object" && t !== null && typeof t.then == "function" && Sv(l, t), Ps !== null && Ps(l, t);
  };
  var Pe = v(null);
  function ac() {
    var l = Pe.current;
    return l !== null ? l : zl.pooledCache;
  }
  function bn(l, t) {
    t === null ? R(Pe, Pe.current) : R(Pe, t.pool);
  }
  function lr() {
    var l = ac();
    return l === null ? null : { parent: jl._currentValue, pool: l };
  }
  var Mu = Error(s(460)), nc = Error(s(474)), Sn = Error(s(542)), pn = { then: function() {
  } };
  function tr(l) {
    return l = l.status, l === "fulfilled" || l === "rejected";
  }
  function er(l, t, e) {
    switch (e = l[e], e === void 0 ? l.push(t) : e !== t && (t.then(kt, kt), t = e), t.status) {
      case "fulfilled":
        return t.value;
      case "rejected":
        throw l = t.reason, ar(l), l;
      default:
        if (typeof t.status == "string") t.then(kt, kt);
        else {
          if (l = zl, l !== null && 100 < l.shellSuspendCounter)
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
            throw l = t.reason, ar(l), l;
        }
        throw tu = t, Mu;
    }
  }
  function lu(l) {
    try {
      var t = l._init;
      return t(l._payload);
    } catch (e) {
      throw e !== null && typeof e == "object" && typeof e.then == "function" ? (tu = e, Mu) : e;
    }
  }
  var tu = null;
  function ur() {
    if (tu === null) throw Error(s(459));
    var l = tu;
    return tu = null, l;
  }
  function ar(l) {
    if (l === Mu || l === Sn)
      throw Error(s(483));
  }
  var Du = null, ga = 0;
  function _n(l) {
    var t = ga;
    return ga += 1, Du === null && (Du = []), er(Du, l, t);
  }
  function ba(l, t) {
    t = t.props.ref, l.ref = t !== void 0 ? t : null;
  }
  function En(l, t) {
    throw t.$$typeof === D ? Error(s(525)) : (l = Object.prototype.toString.call(t), Error(
      s(
        31,
        l === "[object Object]" ? "object with keys {" + Object.keys(t).join(", ") + "}" : l
      )
    ));
  }
  function nr(l) {
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
    function f(h, y, m, q) {
      return y === null || y.tag !== 6 ? (y = wi(m, h.mode, q), y.return = h, y) : (y = a(y, m), y.return = h, y);
    }
    function d(h, y, m, q) {
      var V = m.type;
      return V === K ? A(
        h,
        y,
        m.props.children,
        q,
        m.key
      ) : y !== null && (y.elementType === V || typeof V == "object" && V !== null && V.$$typeof === Yl && lu(V) === y.type) ? (y = a(y, m.props), ba(y, m), y.return = h, y) : (y = vn(
        m.type,
        m.key,
        m.props,
        null,
        h.mode,
        q
      ), ba(y, m), y.return = h, y);
    }
    function g(h, y, m, q) {
      return y === null || y.tag !== 4 || y.stateNode.containerInfo !== m.containerInfo || y.stateNode.implementation !== m.implementation ? (y = ki(m, h.mode, q), y.return = h, y) : (y = a(y, m.children || []), y.return = h, y);
    }
    function A(h, y, m, q, V) {
      return y === null || y.tag !== 7 ? (y = $e(
        m,
        h.mode,
        q,
        V
      ), y.return = h, y) : (y = a(y, m), y.return = h, y);
    }
    function N(h, y, m) {
      if (typeof y == "string" && y !== "" || typeof y == "number" || typeof y == "bigint")
        return y = wi(
          "" + y,
          h.mode,
          m
        ), y.return = h, y;
      if (typeof y == "object" && y !== null) {
        switch (y.$$typeof) {
          case X:
            return m = vn(
              y.type,
              y.key,
              y.props,
              null,
              h.mode,
              m
            ), ba(m, y), m.return = h, m;
          case k:
            return y = ki(
              y,
              h.mode,
              m
            ), y.return = h, y;
          case Yl:
            return y = lu(y), N(h, y, m);
        }
        if (Gl(y) || Nl(y))
          return y = $e(
            y,
            h.mode,
            m,
            null
          ), y.return = h, y;
        if (typeof y.then == "function")
          return N(h, _n(y), m);
        if (y.$$typeof === W)
          return N(
            h,
            gn(h, y),
            m
          );
        En(h, y);
      }
      return null;
    }
    function S(h, y, m, q) {
      var V = y !== null ? y.key : null;
      if (typeof m == "string" && m !== "" || typeof m == "number" || typeof m == "bigint")
        return V !== null ? null : f(h, y, "" + m, q);
      if (typeof m == "object" && m !== null) {
        switch (m.$$typeof) {
          case X:
            return m.key === V ? d(h, y, m, q) : null;
          case k:
            return m.key === V ? g(h, y, m, q) : null;
          case Yl:
            return m = lu(m), S(h, y, m, q);
        }
        if (Gl(m) || Nl(m))
          return V !== null ? null : A(h, y, m, q, null);
        if (typeof m.then == "function")
          return S(
            h,
            y,
            _n(m),
            q
          );
        if (m.$$typeof === W)
          return S(
            h,
            y,
            gn(h, m),
            q
          );
        En(h, m);
      }
      return null;
    }
    function E(h, y, m, q, V) {
      if (typeof q == "string" && q !== "" || typeof q == "number" || typeof q == "bigint")
        return h = h.get(m) || null, f(y, h, "" + q, V);
      if (typeof q == "object" && q !== null) {
        switch (q.$$typeof) {
          case X:
            return h = h.get(
              q.key === null ? m : q.key
            ) || null, d(y, h, q, V);
          case k:
            return h = h.get(
              q.key === null ? m : q.key
            ) || null, g(y, h, q, V);
          case Yl:
            return q = lu(q), E(
              h,
              y,
              m,
              q,
              V
            );
        }
        if (Gl(q) || Nl(q))
          return h = h.get(m) || null, A(y, h, q, V, null);
        if (typeof q.then == "function")
          return E(
            h,
            y,
            m,
            _n(q),
            V
          );
        if (q.$$typeof === W)
          return E(
            h,
            y,
            m,
            gn(y, q),
            V
          );
        En(y, q);
      }
      return null;
    }
    function B(h, y, m, q) {
      for (var V = null, hl = null, Q = y, el = y = 0, sl = null; Q !== null && el < m.length; el++) {
        Q.index > el ? (sl = Q, Q = null) : sl = Q.sibling;
        var ml = S(
          h,
          Q,
          m[el],
          q
        );
        if (ml === null) {
          Q === null && (Q = sl);
          break;
        }
        l && Q && ml.alternate === null && t(h, Q), y = n(ml, y, el), hl === null ? V = ml : hl.sibling = ml, hl = ml, Q = sl;
      }
      if (el === m.length)
        return e(h, Q), rl && Ft(h, el), V;
      if (Q === null) {
        for (; el < m.length; el++)
          Q = N(h, m[el], q), Q !== null && (y = n(
            Q,
            y,
            el
          ), hl === null ? V = Q : hl.sibling = Q, hl = Q);
        return rl && Ft(h, el), V;
      }
      for (Q = u(Q); el < m.length; el++)
        sl = E(
          Q,
          h,
          el,
          m[el],
          q
        ), sl !== null && (l && sl.alternate !== null && Q.delete(
          sl.key === null ? el : sl.key
        ), y = n(
          sl,
          y,
          el
        ), hl === null ? V = sl : hl.sibling = sl, hl = sl);
      return l && Q.forEach(function(Ye) {
        return t(h, Ye);
      }), rl && Ft(h, el), V;
    }
    function w(h, y, m, q) {
      if (m == null) throw Error(s(151));
      for (var V = null, hl = null, Q = y, el = y = 0, sl = null, ml = m.next(); Q !== null && !ml.done; el++, ml = m.next()) {
        Q.index > el ? (sl = Q, Q = null) : sl = Q.sibling;
        var Ye = S(h, Q, ml.value, q);
        if (Ye === null) {
          Q === null && (Q = sl);
          break;
        }
        l && Q && Ye.alternate === null && t(h, Q), y = n(Ye, y, el), hl === null ? V = Ye : hl.sibling = Ye, hl = Ye, Q = sl;
      }
      if (ml.done)
        return e(h, Q), rl && Ft(h, el), V;
      if (Q === null) {
        for (; !ml.done; el++, ml = m.next())
          ml = N(h, ml.value, q), ml !== null && (y = n(ml, y, el), hl === null ? V = ml : hl.sibling = ml, hl = ml);
        return rl && Ft(h, el), V;
      }
      for (Q = u(Q); !ml.done; el++, ml = m.next())
        ml = E(Q, h, el, ml.value, q), ml !== null && (l && ml.alternate !== null && Q.delete(ml.key === null ? el : ml.key), y = n(ml, y, el), hl === null ? V = ml : hl.sibling = ml, hl = ml);
      return l && Q.forEach(function(Dh) {
        return t(h, Dh);
      }), rl && Ft(h, el), V;
    }
    function El(h, y, m, q) {
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
                      ), q = a(
                        y,
                        m.props.children
                      ), q.return = h, h = q;
                      break l;
                    }
                  } else if (y.elementType === V || typeof V == "object" && V !== null && V.$$typeof === Yl && lu(V) === y.type) {
                    e(
                      h,
                      y.sibling
                    ), q = a(y, m.props), ba(q, m), q.return = h, h = q;
                    break l;
                  }
                  e(h, y);
                  break;
                } else t(h, y);
                y = y.sibling;
              }
              m.type === K ? (q = $e(
                m.props.children,
                h.mode,
                q,
                m.key
              ), q.return = h, h = q) : (q = vn(
                m.type,
                m.key,
                m.props,
                null,
                h.mode,
                q
              ), ba(q, m), q.return = h, h = q);
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
                    ), q = a(y, m.children || []), q.return = h, h = q;
                    break l;
                  } else {
                    e(h, y);
                    break;
                  }
                else t(h, y);
                y = y.sibling;
              }
              q = ki(m, h.mode, q), q.return = h, h = q;
            }
            return i(h);
          case Yl:
            return m = lu(m), El(
              h,
              y,
              m,
              q
            );
        }
        if (Gl(m))
          return B(
            h,
            y,
            m,
            q
          );
        if (Nl(m)) {
          if (V = Nl(m), typeof V != "function") throw Error(s(150));
          return m = V.call(m), w(
            h,
            y,
            m,
            q
          );
        }
        if (typeof m.then == "function")
          return El(
            h,
            y,
            _n(m),
            q
          );
        if (m.$$typeof === W)
          return El(
            h,
            y,
            gn(h, m),
            q
          );
        En(h, m);
      }
      return typeof m == "string" && m !== "" || typeof m == "number" || typeof m == "bigint" ? (m = "" + m, y !== null && y.tag === 6 ? (e(h, y.sibling), q = a(y, m), q.return = h, h = q) : (e(h, y), q = wi(m, h.mode, q), q.return = h, h = q), i(h)) : e(h, y);
    }
    return function(h, y, m, q) {
      try {
        ga = 0;
        var V = El(
          h,
          y,
          m,
          q
        );
        return Du = null, V;
      } catch (Q) {
        if (Q === Mu || Q === Sn) throw Q;
        var hl = ht(29, Q, null, h.mode);
        return hl.lanes = q, hl.return = h, hl;
      } finally {
      }
    };
  }
  var eu = nr(!0), ir = nr(!1), _e = !1;
  function ic(l) {
    l.updateQueue = {
      baseState: l.memoizedState,
      firstBaseUpdate: null,
      lastBaseUpdate: null,
      shared: { pending: null, lanes: 0, hiddenCallbacks: null },
      callbacks: null
    };
  }
  function cc(l, t) {
    l = l.updateQueue, t.updateQueue === l && (t.updateQueue = {
      baseState: l.baseState,
      firstBaseUpdate: l.firstBaseUpdate,
      lastBaseUpdate: l.lastBaseUpdate,
      shared: l.shared,
      callbacks: null
    });
  }
  function Ee(l) {
    return { lane: l, tag: 0, payload: null, callback: null, next: null };
  }
  function ze(l, t, e) {
    var u = l.updateQueue;
    if (u === null) return null;
    if (u = u.shared, (gl & 2) !== 0) {
      var a = u.pending;
      return a === null ? t.next = t : (t.next = a.next, a.next = t), u.pending = t, t = yn(l), Zs(l, null, e), t;
    }
    return dn(l, u, t, e), yn(l);
  }
  function Sa(l, t, e) {
    if (t = t.updateQueue, t !== null && (t = t.shared, (e & 4194048) !== 0)) {
      var u = t.lanes;
      u &= l.pendingLanes, e |= u, t.lanes = e, Ff(l, e);
    }
  }
  function fc(l, t) {
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
  var sc = !1;
  function pa() {
    if (sc) {
      var l = Ou;
      if (l !== null) throw l;
    }
  }
  function _a(l, t, e, u) {
    sc = !1;
    var a = l.updateQueue;
    _e = !1;
    var n = a.firstBaseUpdate, i = a.lastBaseUpdate, f = a.shared.pending;
    if (f !== null) {
      a.shared.pending = null;
      var d = f, g = d.next;
      d.next = null, i === null ? n = g : i.next = g, i = d;
      var A = l.alternate;
      A !== null && (A = A.updateQueue, f = A.lastBaseUpdate, f !== i && (f === null ? A.firstBaseUpdate = g : f.next = g, A.lastBaseUpdate = d));
    }
    if (n !== null) {
      var N = a.baseState;
      i = 0, A = g = d = null, f = n;
      do {
        var S = f.lane & -536870913, E = S !== f.lane;
        if (E ? (fl & S) === S : (u & S) === S) {
          S !== 0 && S === Nu && (sc = !0), A !== null && (A = A.next = {
            lane: 0,
            tag: f.tag,
            payload: f.payload,
            callback: null,
            next: null
          });
          l: {
            var B = l, w = f;
            S = t;
            var El = e;
            switch (w.tag) {
              case 1:
                if (B = w.payload, typeof B == "function") {
                  N = B.call(El, N, S);
                  break l;
                }
                N = B;
                break l;
              case 3:
                B.flags = B.flags & -65537 | 128;
              case 0:
                if (B = w.payload, S = typeof B == "function" ? B.call(El, N, S) : B, S == null) break l;
                N = p({}, N, S);
                break l;
              case 2:
                _e = !0;
            }
          }
          S = f.callback, S !== null && (l.flags |= 64, E && (l.flags |= 8192), E = a.callbacks, E === null ? a.callbacks = [S] : E.push(S));
        } else
          E = {
            lane: S,
            tag: f.tag,
            payload: f.payload,
            callback: f.callback,
            next: null
          }, A === null ? (g = A = E, d = N) : A = A.next = E, i |= S;
        if (f = f.next, f === null) {
          if (f = a.shared.pending, f === null)
            break;
          E = f, f = E.next, E.next = null, a.lastBaseUpdate = E, a.shared.pending = null;
        }
      } while (!0);
      A === null && (d = N), a.baseState = d, a.firstBaseUpdate = g, a.lastBaseUpdate = A, n === null && (a.shared.lanes = 0), Ne |= i, l.lanes = i, l.memoizedState = N;
    }
  }
  function cr(l, t) {
    if (typeof l != "function")
      throw Error(s(191, l));
    l.call(t);
  }
  function fr(l, t) {
    var e = l.callbacks;
    if (e !== null)
      for (l.callbacks = null, l = 0; l < e.length; l++)
        cr(e[l], t);
  }
  var Uu = v(null), zn = v(0);
  function sr(l, t) {
    l = fe, R(zn, l), R(Uu, t), fe = l | t.baseLanes;
  }
  function rc() {
    R(zn, fe), R(Uu, Uu.current);
  }
  function oc() {
    fe = zn.current, O(Uu), O(zn);
  }
  var mt = v(null), Mt = null;
  function Ae(l) {
    var t = l.alternate;
    R(Dl, Dl.current & 1), R(mt, l), Mt === null && (t === null || Uu.current !== null || t.memoizedState !== null) && (Mt = l);
  }
  function dc(l) {
    R(Dl, Dl.current), R(mt, l), Mt === null && (Mt = l);
  }
  function rr(l) {
    l.tag === 22 ? (R(Dl, Dl.current), R(mt, l), Mt === null && (Mt = l)) : qe();
  }
  function qe() {
    R(Dl, Dl.current), R(mt, mt.current);
  }
  function gt(l) {
    O(mt), Mt === l && (Mt = null), O(Dl);
  }
  var Dl = v(0);
  function An(l) {
    for (var t = l; t !== null; ) {
      if (t.tag === 13) {
        var e = t.memoizedState;
        if (e !== null && (e = e.dehydrated, e === null || Sf(e) || pf(e)))
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
  var le = 0, tl = null, pl = null, Rl = null, qn = !1, Cu = !1, uu = !1, Tn = 0, Ea = 0, ju = null, _v = 0;
  function Ol() {
    throw Error(s(321));
  }
  function yc(l, t) {
    if (t === null) return !1;
    for (var e = 0; e < t.length && e < l.length; e++)
      if (!vt(l[e], t[e])) return !1;
    return !0;
  }
  function vc(l, t, e, u, a, n) {
    return le = n, tl = t, t.memoizedState = null, t.updateQueue = null, t.lanes = 0, _.H = l === null || l.memoizedState === null ? wr : Oc, uu = !1, n = e(u, a), uu = !1, Cu && (n = dr(
      t,
      e,
      u,
      a
    )), or(l), n;
  }
  function or(l) {
    _.H = qa;
    var t = pl !== null && pl.next !== null;
    if (le = 0, Rl = pl = tl = null, qn = !1, Ea = 0, ju = null, t) throw Error(s(300));
    l === null || Hl || (l = l.dependencies, l !== null && mn(l) && (Hl = !0));
  }
  function dr(l, t, e, u) {
    tl = l;
    var a = 0;
    do {
      if (Cu && (ju = null), Ea = 0, Cu = !1, 25 <= a) throw Error(s(301));
      if (a += 1, Rl = pl = null, l.updateQueue != null) {
        var n = l.updateQueue;
        n.lastEffect = null, n.events = null, n.stores = null, n.memoCache != null && (n.memoCache.index = 0);
      }
      _.H = kr, n = t(e, u);
    } while (Cu);
    return n;
  }
  function Ev() {
    var l = _.H, t = l.useState()[0];
    return t = typeof t.then == "function" ? za(t) : t, l = l.useState()[0], (pl !== null ? pl.memoizedState : null) !== l && (tl.flags |= 1024), t;
  }
  function hc() {
    var l = Tn !== 0;
    return Tn = 0, l;
  }
  function mc(l, t, e) {
    t.updateQueue = l.updateQueue, t.flags &= -2053, l.lanes &= ~e;
  }
  function gc(l) {
    if (qn) {
      for (l = l.memoizedState; l !== null; ) {
        var t = l.queue;
        t !== null && (t.pending = null), l = l.next;
      }
      qn = !1;
    }
    le = 0, Rl = pl = tl = null, Cu = !1, Ea = Tn = 0, ju = null;
  }
  function Il() {
    var l = {
      memoizedState: null,
      baseState: null,
      baseQueue: null,
      queue: null,
      next: null
    };
    return Rl === null ? tl.memoizedState = Rl = l : Rl = Rl.next = l, Rl;
  }
  function Ul() {
    if (pl === null) {
      var l = tl.alternate;
      l = l !== null ? l.memoizedState : null;
    } else l = pl.next;
    var t = Rl === null ? tl.memoizedState : Rl.next;
    if (t !== null)
      Rl = t, pl = l;
    else {
      if (l === null)
        throw tl.alternate === null ? Error(s(467)) : Error(s(310));
      pl = l, l = {
        memoizedState: pl.memoizedState,
        baseState: pl.baseState,
        baseQueue: pl.baseQueue,
        queue: pl.queue,
        next: null
      }, Rl === null ? tl.memoizedState = Rl = l : Rl = Rl.next = l;
    }
    return Rl;
  }
  function xn() {
    return { lastEffect: null, events: null, stores: null, memoCache: null };
  }
  function za(l) {
    var t = Ea;
    return Ea += 1, ju === null && (ju = []), l = er(ju, l, t), t = tl, (Rl === null ? t.memoizedState : Rl.next) === null && (t = t.alternate, _.H = t === null || t.memoizedState === null ? wr : Oc), l;
  }
  function Nn(l) {
    if (l !== null && typeof l == "object") {
      if (typeof l.then == "function") return za(l);
      if (l.$$typeof === W) return wl(l);
    }
    throw Error(s(438, String(l)));
  }
  function bc(l) {
    var t = null, e = tl.updateQueue;
    if (e !== null && (t = e.memoCache), t == null) {
      var u = tl.alternate;
      u !== null && (u = u.updateQueue, u !== null && (u = u.memoCache, u != null && (t = {
        data: u.data.map(function(a) {
          return a.slice();
        }),
        index: 0
      })));
    }
    if (t == null && (t = { data: [], index: 0 }), e === null && (e = xn(), tl.updateQueue = e), e.memoCache = t, e = t.data[t.index], e === void 0)
      for (e = t.data[t.index] = Array(l), u = 0; u < l; u++)
        e[u] = _t;
    return t.index++, e;
  }
  function te(l, t) {
    return typeof t == "function" ? t(l) : t;
  }
  function On(l) {
    var t = Ul();
    return Sc(t, pl, l);
  }
  function Sc(l, t, e) {
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
      var f = i = null, d = null, g = t, A = !1;
      do {
        var N = g.lane & -536870913;
        if (N !== g.lane ? (fl & N) === N : (le & N) === N) {
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
            }), N === Nu && (A = !0);
          else if ((le & S) === S) {
            g = g.next, S === Nu && (A = !0);
            continue;
          } else
            N = {
              lane: 0,
              revertLane: g.revertLane,
              gesture: null,
              action: g.action,
              hasEagerState: g.hasEagerState,
              eagerState: g.eagerState,
              next: null
            }, d === null ? (f = d = N, i = n) : d = d.next = N, tl.lanes |= S, Ne |= S;
          N = g.action, uu && e(n, N), n = g.hasEagerState ? g.eagerState : e(n, N);
        } else
          S = {
            lane: N,
            revertLane: g.revertLane,
            gesture: g.gesture,
            action: g.action,
            hasEagerState: g.hasEagerState,
            eagerState: g.eagerState,
            next: null
          }, d === null ? (f = d = S, i = n) : d = d.next = S, tl.lanes |= N, Ne |= N;
        g = g.next;
      } while (g !== null && g !== t);
      if (d === null ? i = n : d.next = f, !vt(n, l.memoizedState) && (Hl = !0, A && (e = Ou, e !== null)))
        throw e;
      l.memoizedState = n, l.baseState = i, l.baseQueue = d, u.lastRenderedState = n;
    }
    return a === null && (u.lanes = 0), [l.memoizedState, u.dispatch];
  }
  function pc(l) {
    var t = Ul(), e = t.queue;
    if (e === null) throw Error(s(311));
    e.lastRenderedReducer = l;
    var u = e.dispatch, a = e.pending, n = t.memoizedState;
    if (a !== null) {
      e.pending = null;
      var i = a = a.next;
      do
        n = l(n, i.action), i = i.next;
      while (i !== a);
      vt(n, t.memoizedState) || (Hl = !0), t.memoizedState = n, t.baseQueue === null && (t.baseState = n), e.lastRenderedState = n;
    }
    return [n, u];
  }
  function yr(l, t, e) {
    var u = tl, a = Ul(), n = rl;
    if (n) {
      if (e === void 0) throw Error(s(407));
      e = e();
    } else e = t();
    var i = !vt(
      (pl || a).memoizedState,
      e
    );
    if (i && (a.memoizedState = e, Hl = !0), a = a.queue, zc(mr.bind(null, u, a, l), [
      l
    ]), a.getSnapshot !== t || i || Rl !== null && Rl.memoizedState.tag & 1) {
      if (u.flags |= 2048, Ru(
        9,
        { destroy: void 0 },
        hr.bind(
          null,
          u,
          a,
          e,
          t
        ),
        null
      ), zl === null) throw Error(s(349));
      n || (le & 127) !== 0 || vr(u, t, e);
    }
    return e;
  }
  function vr(l, t, e) {
    l.flags |= 16384, l = { getSnapshot: t, value: e }, t = tl.updateQueue, t === null ? (t = xn(), tl.updateQueue = t, t.stores = [l]) : (e = t.stores, e === null ? t.stores = [l] : e.push(l));
  }
  function hr(l, t, e, u) {
    t.value = e, t.getSnapshot = u, gr(t) && br(l);
  }
  function mr(l, t, e) {
    return e(function() {
      gr(t) && br(l);
    });
  }
  function gr(l) {
    var t = l.getSnapshot;
    l = l.value;
    try {
      var e = t();
      return !vt(l, e);
    } catch {
      return !0;
    }
  }
  function br(l) {
    var t = ke(l, 2);
    t !== null && it(t, l, 2);
  }
  function _c(l) {
    var t = Il();
    if (typeof l == "function") {
      var e = l;
      if (l = e(), uu) {
        ve(!0);
        try {
          e();
        } finally {
          ve(!1);
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
  function Sr(l, t, e, u) {
    return l.baseState = e, Sc(
      l,
      pl,
      typeof u == "function" ? u : te
    );
  }
  function zv(l, t, e, u, a) {
    if (Un(l)) throw Error(s(485));
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
      _.T !== null ? e(!0) : n.isTransition = !1, u(n), e = t.pending, e === null ? (n.next = t.pending = n, pr(t, n)) : (n.next = e.next, t.pending = e.next = n);
    }
  }
  function pr(l, t) {
    var e = t.action, u = t.payload, a = l.state;
    if (t.isTransition) {
      var n = _.T, i = {};
      _.T = i;
      try {
        var f = e(a, u), d = _.S;
        d !== null && d(i, f), _r(l, t, f);
      } catch (g) {
        Ec(l, t, g);
      } finally {
        n !== null && i.types !== null && (n.types = i.types), _.T = n;
      }
    } else
      try {
        n = e(a, u), _r(l, t, n);
      } catch (g) {
        Ec(l, t, g);
      }
  }
  function _r(l, t, e) {
    e !== null && typeof e == "object" && typeof e.then == "function" ? e.then(
      function(u) {
        Er(l, t, u);
      },
      function(u) {
        return Ec(l, t, u);
      }
    ) : Er(l, t, e);
  }
  function Er(l, t, e) {
    t.status = "fulfilled", t.value = e, zr(t), l.state = e, t = l.pending, t !== null && (e = t.next, e === t ? l.pending = null : (e = e.next, t.next = e, pr(l, e)));
  }
  function Ec(l, t, e) {
    var u = l.pending;
    if (l.pending = null, u !== null) {
      u = u.next;
      do
        t.status = "rejected", t.reason = e, zr(t), t = t.next;
      while (t !== u);
    }
    l.action = null;
  }
  function zr(l) {
    l = l.listeners;
    for (var t = 0; t < l.length; t++) (0, l[t])();
  }
  function Ar(l, t) {
    return t;
  }
  function qr(l, t) {
    if (rl) {
      var e = zl.formState;
      if (e !== null) {
        l: {
          var u = tl;
          if (rl) {
            if (Al) {
              t: {
                for (var a = Al, n = Ot; a.nodeType !== 8; ) {
                  if (!n) {
                    a = null;
                    break t;
                  }
                  if (a = Dt(
                    a.nextSibling
                  ), a === null) {
                    a = null;
                    break t;
                  }
                }
                n = a.data, a = n === "F!" || n === "F" ? a : null;
              }
              if (a) {
                Al = Dt(
                  a.nextSibling
                ), u = a.data === "F!";
                break l;
              }
            }
            Se(u);
          }
          u = !1;
        }
        u && (t = e[0]);
      }
    }
    return e = Il(), e.memoizedState = e.baseState = t, u = {
      pending: null,
      lanes: 0,
      dispatch: null,
      lastRenderedReducer: Ar,
      lastRenderedState: t
    }, e.queue = u, e = Vr.bind(
      null,
      tl,
      u
    ), u.dispatch = e, u = _c(!1), n = Nc.bind(
      null,
      tl,
      !1,
      u.queue
    ), u = Il(), a = {
      state: t,
      dispatch: null,
      action: l,
      pending: null
    }, u.queue = a, e = zv.bind(
      null,
      tl,
      a,
      n,
      e
    ), a.dispatch = e, u.memoizedState = l, [t, e, !1];
  }
  function Tr(l) {
    var t = Ul();
    return xr(t, pl, l);
  }
  function xr(l, t, e) {
    if (t = Sc(
      l,
      t,
      Ar
    )[0], l = On(te)[0], typeof t == "object" && t !== null && typeof t.then == "function")
      try {
        var u = za(t);
      } catch (i) {
        throw i === Mu ? Sn : i;
      }
    else u = t;
    t = Ul();
    var a = t.queue, n = a.dispatch;
    return e !== t.memoizedState && (tl.flags |= 2048, Ru(
      9,
      { destroy: void 0 },
      Av.bind(null, a, e),
      null
    )), [u, n, l];
  }
  function Av(l, t) {
    l.action = t;
  }
  function Nr(l) {
    var t = Ul(), e = pl;
    if (e !== null)
      return xr(t, e, l);
    Ul(), t = t.memoizedState, e = Ul();
    var u = e.queue.dispatch;
    return e.memoizedState = l, [t, u, !1];
  }
  function Ru(l, t, e, u) {
    return l = { tag: l, create: e, deps: u, inst: t, next: null }, t = tl.updateQueue, t === null && (t = xn(), tl.updateQueue = t), e = t.lastEffect, e === null ? t.lastEffect = l.next = l : (u = e.next, e.next = l, l.next = u, t.lastEffect = l), l;
  }
  function Or() {
    return Ul().memoizedState;
  }
  function Mn(l, t, e, u) {
    var a = Il();
    tl.flags |= l, a.memoizedState = Ru(
      1 | t,
      { destroy: void 0 },
      e,
      u === void 0 ? null : u
    );
  }
  function Dn(l, t, e, u) {
    var a = Ul();
    u = u === void 0 ? null : u;
    var n = a.memoizedState.inst;
    pl !== null && u !== null && yc(u, pl.memoizedState.deps) ? a.memoizedState = Ru(t, n, e, u) : (tl.flags |= l, a.memoizedState = Ru(
      1 | t,
      n,
      e,
      u
    ));
  }
  function Mr(l, t) {
    Mn(8390656, 8, l, t);
  }
  function zc(l, t) {
    Dn(2048, 8, l, t);
  }
  function qv(l) {
    tl.flags |= 4;
    var t = tl.updateQueue;
    if (t === null)
      t = xn(), tl.updateQueue = t, t.events = [l];
    else {
      var e = t.events;
      e === null ? t.events = [l] : e.push(l);
    }
  }
  function Dr(l) {
    var t = Ul().memoizedState;
    return qv({ ref: t, nextImpl: l }), function() {
      if ((gl & 2) !== 0) throw Error(s(440));
      return t.impl.apply(void 0, arguments);
    };
  }
  function Ur(l, t) {
    return Dn(4, 2, l, t);
  }
  function Cr(l, t) {
    return Dn(4, 4, l, t);
  }
  function jr(l, t) {
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
  function Rr(l, t, e) {
    e = e != null ? e.concat([l]) : null, Dn(4, 4, jr.bind(null, t, l), e);
  }
  function Ac() {
  }
  function Hr(l, t) {
    var e = Ul();
    t = t === void 0 ? null : t;
    var u = e.memoizedState;
    return t !== null && yc(t, u[1]) ? u[0] : (e.memoizedState = [l, t], l);
  }
  function Br(l, t) {
    var e = Ul();
    t = t === void 0 ? null : t;
    var u = e.memoizedState;
    if (t !== null && yc(t, u[1]))
      return u[0];
    if (u = l(), uu) {
      ve(!0);
      try {
        l();
      } finally {
        ve(!1);
      }
    }
    return e.memoizedState = [u, t], u;
  }
  function qc(l, t, e) {
    return e === void 0 || (le & 1073741824) !== 0 && (fl & 261930) === 0 ? l.memoizedState = t : (l.memoizedState = e, l = Go(), tl.lanes |= l, Ne |= l, e);
  }
  function Yr(l, t, e, u) {
    return vt(e, t) ? e : Uu.current !== null ? (l = qc(l, e, u), vt(l, t) || (Hl = !0), l) : (le & 42) === 0 || (le & 1073741824) !== 0 && (fl & 261930) === 0 ? (Hl = !0, l.memoizedState = e) : (l = Go(), tl.lanes |= l, Ne |= l, t);
  }
  function Gr(l, t, e, u, a) {
    var n = H.p;
    H.p = n !== 0 && 8 > n ? n : 8;
    var i = _.T, f = {};
    _.T = f, Nc(l, !1, t, e);
    try {
      var d = a(), g = _.S;
      if (g !== null && g(f, d), d !== null && typeof d == "object" && typeof d.then == "function") {
        var A = pv(
          d,
          u
        );
        Aa(
          l,
          t,
          A,
          pt(l)
        );
      } else
        Aa(
          l,
          t,
          u,
          pt(l)
        );
    } catch (N) {
      Aa(
        l,
        t,
        { then: function() {
        }, status: "rejected", reason: N },
        pt()
      );
    } finally {
      H.p = n, i !== null && f.types !== null && (i.types = f.types), _.T = i;
    }
  }
  function Tv() {
  }
  function Tc(l, t, e, u) {
    if (l.tag !== 5) throw Error(s(476));
    var a = Lr(l).queue;
    Gr(
      l,
      a,
      t,
      J,
      e === null ? Tv : function() {
        return Qr(l), e(u);
      }
    );
  }
  function Lr(l) {
    var t = l.memoizedState;
    if (t !== null) return t;
    t = {
      memoizedState: J,
      baseState: J,
      baseQueue: null,
      queue: {
        pending: null,
        lanes: 0,
        dispatch: null,
        lastRenderedReducer: te,
        lastRenderedState: J
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
  function Qr(l) {
    var t = Lr(l);
    t.next === null && (t = l.alternate.memoizedState), Aa(
      l,
      t.next.queue,
      {},
      pt()
    );
  }
  function xc() {
    return wl(Qa);
  }
  function Xr() {
    return Ul().memoizedState;
  }
  function Zr() {
    return Ul().memoizedState;
  }
  function xv(l) {
    for (var t = l.return; t !== null; ) {
      switch (t.tag) {
        case 24:
        case 3:
          var e = pt();
          l = Ee(e);
          var u = ze(t, l, e);
          u !== null && (it(u, t, e), Sa(u, t, e)), t = { cache: ec() }, l.payload = t;
          return;
      }
      t = t.return;
    }
  }
  function Nv(l, t, e) {
    var u = pt();
    e = {
      lane: u,
      revertLane: 0,
      gesture: null,
      action: e,
      hasEagerState: !1,
      eagerState: null,
      next: null
    }, Un(l) ? Kr(t, e) : (e = Ki(l, t, e, u), e !== null && (it(e, l, u), Jr(e, t, u)));
  }
  function Vr(l, t, e) {
    var u = pt();
    Aa(l, t, e, u);
  }
  function Aa(l, t, e, u) {
    var a = {
      lane: u,
      revertLane: 0,
      gesture: null,
      action: e,
      hasEagerState: !1,
      eagerState: null,
      next: null
    };
    if (Un(l)) Kr(t, a);
    else {
      var n = l.alternate;
      if (l.lanes === 0 && (n === null || n.lanes === 0) && (n = t.lastRenderedReducer, n !== null))
        try {
          var i = t.lastRenderedState, f = n(i, e);
          if (a.hasEagerState = !0, a.eagerState = f, vt(f, i))
            return dn(l, t, a, 0), zl === null && on(), !1;
        } catch {
        } finally {
        }
      if (e = Ki(l, t, a, u), e !== null)
        return it(e, l, u), Jr(e, t, u), !0;
    }
    return !1;
  }
  function Nc(l, t, e, u) {
    if (u = {
      lane: 2,
      revertLane: cf(),
      gesture: null,
      action: u,
      hasEagerState: !1,
      eagerState: null,
      next: null
    }, Un(l)) {
      if (t) throw Error(s(479));
    } else
      t = Ki(
        l,
        e,
        u,
        2
      ), t !== null && it(t, l, 2);
  }
  function Un(l) {
    var t = l.alternate;
    return l === tl || t !== null && t === tl;
  }
  function Kr(l, t) {
    Cu = qn = !0;
    var e = l.pending;
    e === null ? t.next = t : (t.next = e.next, e.next = t), l.pending = t;
  }
  function Jr(l, t, e) {
    if ((e & 4194048) !== 0) {
      var u = t.lanes;
      u &= l.pendingLanes, e |= u, t.lanes = e, Ff(l, e);
    }
  }
  var qa = {
    readContext: wl,
    use: Nn,
    useCallback: Ol,
    useContext: Ol,
    useEffect: Ol,
    useImperativeHandle: Ol,
    useLayoutEffect: Ol,
    useInsertionEffect: Ol,
    useMemo: Ol,
    useReducer: Ol,
    useRef: Ol,
    useState: Ol,
    useDebugValue: Ol,
    useDeferredValue: Ol,
    useTransition: Ol,
    useSyncExternalStore: Ol,
    useId: Ol,
    useHostTransitionStatus: Ol,
    useFormState: Ol,
    useActionState: Ol,
    useOptimistic: Ol,
    useMemoCache: Ol,
    useCacheRefresh: Ol
  };
  qa.useEffectEvent = Ol;
  var wr = {
    readContext: wl,
    use: Nn,
    useCallback: function(l, t) {
      return Il().memoizedState = [
        l,
        t === void 0 ? null : t
      ], l;
    },
    useContext: wl,
    useEffect: Mr,
    useImperativeHandle: function(l, t, e) {
      e = e != null ? e.concat([l]) : null, Mn(
        4194308,
        4,
        jr.bind(null, t, l),
        e
      );
    },
    useLayoutEffect: function(l, t) {
      return Mn(4194308, 4, l, t);
    },
    useInsertionEffect: function(l, t) {
      Mn(4, 2, l, t);
    },
    useMemo: function(l, t) {
      var e = Il();
      t = t === void 0 ? null : t;
      var u = l();
      if (uu) {
        ve(!0);
        try {
          l();
        } finally {
          ve(!1);
        }
      }
      return e.memoizedState = [u, t], u;
    },
    useReducer: function(l, t, e) {
      var u = Il();
      if (e !== void 0) {
        var a = e(t);
        if (uu) {
          ve(!0);
          try {
            e(t);
          } finally {
            ve(!1);
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
        tl,
        l
      ), [u.memoizedState, l];
    },
    useRef: function(l) {
      var t = Il();
      return l = { current: l }, t.memoizedState = l;
    },
    useState: function(l) {
      l = _c(l);
      var t = l.queue, e = Vr.bind(null, tl, t);
      return t.dispatch = e, [l.memoizedState, e];
    },
    useDebugValue: Ac,
    useDeferredValue: function(l, t) {
      var e = Il();
      return qc(e, l, t);
    },
    useTransition: function() {
      var l = _c(!1);
      return l = Gr.bind(
        null,
        tl,
        l.queue,
        !0,
        !1
      ), Il().memoizedState = l, [!1, l];
    },
    useSyncExternalStore: function(l, t, e) {
      var u = tl, a = Il();
      if (rl) {
        if (e === void 0)
          throw Error(s(407));
        e = e();
      } else {
        if (e = t(), zl === null)
          throw Error(s(349));
        (fl & 127) !== 0 || vr(u, t, e);
      }
      a.memoizedState = e;
      var n = { value: e, getSnapshot: t };
      return a.queue = n, Mr(mr.bind(null, u, n, l), [
        l
      ]), u.flags |= 2048, Ru(
        9,
        { destroy: void 0 },
        hr.bind(
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
      var l = Il(), t = zl.identifierPrefix;
      if (rl) {
        var e = Xt, u = Qt;
        e = (u & ~(1 << 32 - yt(u) - 1)).toString(32) + e, t = "_" + t + "R_" + e, e = Tn++, 0 < e && (t += "H" + e.toString(32)), t += "_";
      } else
        e = _v++, t = "_" + t + "r_" + e.toString(32) + "_";
      return l.memoizedState = t;
    },
    useHostTransitionStatus: xc,
    useFormState: qr,
    useActionState: qr,
    useOptimistic: function(l) {
      var t = Il();
      t.memoizedState = t.baseState = l;
      var e = {
        pending: null,
        lanes: 0,
        dispatch: null,
        lastRenderedReducer: null,
        lastRenderedState: null
      };
      return t.queue = e, t = Nc.bind(
        null,
        tl,
        !0,
        e
      ), e.dispatch = t, [l, t];
    },
    useMemoCache: bc,
    useCacheRefresh: function() {
      return Il().memoizedState = xv.bind(
        null,
        tl
      );
    },
    useEffectEvent: function(l) {
      var t = Il(), e = { impl: l };
      return t.memoizedState = e, function() {
        if ((gl & 2) !== 0)
          throw Error(s(440));
        return e.impl.apply(void 0, arguments);
      };
    }
  }, Oc = {
    readContext: wl,
    use: Nn,
    useCallback: Hr,
    useContext: wl,
    useEffect: zc,
    useImperativeHandle: Rr,
    useInsertionEffect: Ur,
    useLayoutEffect: Cr,
    useMemo: Br,
    useReducer: On,
    useRef: Or,
    useState: function() {
      return On(te);
    },
    useDebugValue: Ac,
    useDeferredValue: function(l, t) {
      var e = Ul();
      return Yr(
        e,
        pl.memoizedState,
        l,
        t
      );
    },
    useTransition: function() {
      var l = On(te)[0], t = Ul().memoizedState;
      return [
        typeof l == "boolean" ? l : za(l),
        t
      ];
    },
    useSyncExternalStore: yr,
    useId: Xr,
    useHostTransitionStatus: xc,
    useFormState: Tr,
    useActionState: Tr,
    useOptimistic: function(l, t) {
      var e = Ul();
      return Sr(e, pl, l, t);
    },
    useMemoCache: bc,
    useCacheRefresh: Zr
  };
  Oc.useEffectEvent = Dr;
  var kr = {
    readContext: wl,
    use: Nn,
    useCallback: Hr,
    useContext: wl,
    useEffect: zc,
    useImperativeHandle: Rr,
    useInsertionEffect: Ur,
    useLayoutEffect: Cr,
    useMemo: Br,
    useReducer: pc,
    useRef: Or,
    useState: function() {
      return pc(te);
    },
    useDebugValue: Ac,
    useDeferredValue: function(l, t) {
      var e = Ul();
      return pl === null ? qc(e, l, t) : Yr(
        e,
        pl.memoizedState,
        l,
        t
      );
    },
    useTransition: function() {
      var l = pc(te)[0], t = Ul().memoizedState;
      return [
        typeof l == "boolean" ? l : za(l),
        t
      ];
    },
    useSyncExternalStore: yr,
    useId: Xr,
    useHostTransitionStatus: xc,
    useFormState: Nr,
    useActionState: Nr,
    useOptimistic: function(l, t) {
      var e = Ul();
      return pl !== null ? Sr(e, pl, l, t) : (e.baseState = l, [l, e.queue.dispatch]);
    },
    useMemoCache: bc,
    useCacheRefresh: Zr
  };
  kr.useEffectEvent = Dr;
  function Mc(l, t, e, u) {
    t = l.memoizedState, e = e(u, t), e = e == null ? t : p({}, t, e), l.memoizedState = e, l.lanes === 0 && (l.updateQueue.baseState = e);
  }
  var Dc = {
    enqueueSetState: function(l, t, e) {
      l = l._reactInternals;
      var u = pt(), a = Ee(u);
      a.payload = t, e != null && (a.callback = e), t = ze(l, a, u), t !== null && (it(t, l, u), Sa(t, l, u));
    },
    enqueueReplaceState: function(l, t, e) {
      l = l._reactInternals;
      var u = pt(), a = Ee(u);
      a.tag = 1, a.payload = t, e != null && (a.callback = e), t = ze(l, a, u), t !== null && (it(t, l, u), Sa(t, l, u));
    },
    enqueueForceUpdate: function(l, t) {
      l = l._reactInternals;
      var e = pt(), u = Ee(e);
      u.tag = 2, t != null && (u.callback = t), t = ze(l, u, e), t !== null && (it(t, l, e), Sa(t, l, e));
    }
  };
  function $r(l, t, e, u, a, n, i) {
    return l = l.stateNode, typeof l.shouldComponentUpdate == "function" ? l.shouldComponentUpdate(u, n, i) : t.prototype && t.prototype.isPureReactComponent ? !oa(e, u) || !oa(a, n) : !0;
  }
  function Wr(l, t, e, u) {
    l = t.state, typeof t.componentWillReceiveProps == "function" && t.componentWillReceiveProps(e, u), typeof t.UNSAFE_componentWillReceiveProps == "function" && t.UNSAFE_componentWillReceiveProps(e, u), t.state !== l && Dc.enqueueReplaceState(t, t.state, null);
  }
  function au(l, t) {
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
  function Fr(l) {
    rn(l);
  }
  function Ir(l) {
    console.error(l);
  }
  function Pr(l) {
    rn(l);
  }
  function Cn(l, t) {
    try {
      var e = l.onUncaughtError;
      e(t.value, { componentStack: t.stack });
    } catch (u) {
      setTimeout(function() {
        throw u;
      });
    }
  }
  function lo(l, t, e) {
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
  function Uc(l, t, e) {
    return e = Ee(e), e.tag = 3, e.payload = { element: null }, e.callback = function() {
      Cn(l, t);
    }, e;
  }
  function to(l) {
    return l = Ee(l), l.tag = 3, l;
  }
  function eo(l, t, e, u) {
    var a = e.type.getDerivedStateFromError;
    if (typeof a == "function") {
      var n = u.value;
      l.payload = function() {
        return a(n);
      }, l.callback = function() {
        lo(t, e, u);
      };
    }
    var i = e.stateNode;
    i !== null && typeof i.componentDidCatch == "function" && (l.callback = function() {
      lo(t, e, u), typeof a != "function" && (Oe === null ? Oe = /* @__PURE__ */ new Set([this]) : Oe.add(this));
      var f = u.stack;
      this.componentDidCatch(u.value, {
        componentStack: f !== null ? f : ""
      });
    });
  }
  function Ov(l, t, e, u, a) {
    if (e.flags |= 32768, u !== null && typeof u == "object" && typeof u.then == "function") {
      if (t = e.alternate, t !== null && xu(
        t,
        e,
        a,
        !0
      ), e = mt.current, e !== null) {
        switch (e.tag) {
          case 31:
          case 13:
            return Mt === null ? Kn() : e.alternate === null && Ml === 0 && (Ml = 3), e.flags &= -257, e.flags |= 65536, e.lanes = a, u === pn ? e.flags |= 16384 : (t = e.updateQueue, t === null ? e.updateQueue = /* @__PURE__ */ new Set([u]) : t.add(u), uf(l, u, a)), !1;
          case 22:
            return e.flags |= 65536, u === pn ? e.flags |= 16384 : (t = e.updateQueue, t === null ? (t = {
              transitions: null,
              markerInstances: null,
              retryQueue: /* @__PURE__ */ new Set([u])
            }, e.updateQueue = t) : (e = t.retryQueue, e === null ? t.retryQueue = /* @__PURE__ */ new Set([u]) : e.add(u)), uf(l, u, a)), !1;
        }
        throw Error(s(435, e.tag));
      }
      return uf(l, u, a), Kn(), !1;
    }
    if (rl)
      return t = mt.current, t !== null ? ((t.flags & 65536) === 0 && (t.flags |= 256), t.flags |= 65536, t.lanes = a, u !== Fi && (l = Error(s(422), { cause: u }), va(Tt(l, e)))) : (u !== Fi && (t = Error(s(423), {
        cause: u
      }), va(
        Tt(t, e)
      )), l = l.current.alternate, l.flags |= 65536, a &= -a, l.lanes |= a, u = Tt(u, e), a = Uc(
        l.stateNode,
        u,
        a
      ), fc(l, a), Ml !== 4 && (Ml = 2)), !1;
    var n = Error(s(520), { cause: u });
    if (n = Tt(n, e), Ca === null ? Ca = [n] : Ca.push(n), Ml !== 4 && (Ml = 2), t === null) return !0;
    u = Tt(u, e), e = t;
    do {
      switch (e.tag) {
        case 3:
          return e.flags |= 65536, l = a & -a, e.lanes |= l, l = Uc(e.stateNode, u, l), fc(e, l), !1;
        case 1:
          if (t = e.type, n = e.stateNode, (e.flags & 128) === 0 && (typeof t.getDerivedStateFromError == "function" || n !== null && typeof n.componentDidCatch == "function" && (Oe === null || !Oe.has(n))))
            return e.flags |= 65536, a &= -a, e.lanes |= a, a = to(a), eo(
              a,
              l,
              e,
              u
            ), fc(e, a), !1;
      }
      e = e.return;
    } while (e !== null);
    return !1;
  }
  var Cc = Error(s(461)), Hl = !1;
  function kl(l, t, e, u) {
    t.child = l === null ? ir(t, null, e, u) : eu(
      t,
      l.child,
      e,
      u
    );
  }
  function uo(l, t, e, u, a) {
    e = e.render;
    var n = t.ref;
    if ("ref" in u) {
      var i = {};
      for (var f in u)
        f !== "ref" && (i[f] = u[f]);
    } else i = u;
    return Ie(t), u = vc(
      l,
      t,
      e,
      i,
      n,
      a
    ), f = hc(), l !== null && !Hl ? (mc(l, t, a), ee(l, t, a)) : (rl && f && $i(t), t.flags |= 1, kl(l, t, u, a), t.child);
  }
  function ao(l, t, e, u, a) {
    if (l === null) {
      var n = e.type;
      return typeof n == "function" && !Ji(n) && n.defaultProps === void 0 && e.compare === null ? (t.tag = 15, t.type = n, no(
        l,
        t,
        n,
        u,
        a
      )) : (l = vn(
        e.type,
        null,
        u,
        t,
        t.mode,
        a
      ), l.ref = t.ref, l.return = t, t.child = l);
    }
    if (n = l.child, !Qc(l, a)) {
      var i = n.memoizedProps;
      if (e = e.compare, e = e !== null ? e : oa, e(i, u) && l.ref === t.ref)
        return ee(l, t, a);
    }
    return t.flags |= 1, l = Wt(n, u), l.ref = t.ref, l.return = t, t.child = l;
  }
  function no(l, t, e, u, a) {
    if (l !== null) {
      var n = l.memoizedProps;
      if (oa(n, u) && l.ref === t.ref)
        if (Hl = !1, t.pendingProps = u = n, Qc(l, a))
          (l.flags & 131072) !== 0 && (Hl = !0);
        else
          return t.lanes = l.lanes, ee(l, t, a);
    }
    return jc(
      l,
      t,
      e,
      u,
      a
    );
  }
  function io(l, t, e, u) {
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
        return co(
          l,
          t,
          n,
          e,
          u
        );
      }
      if ((e & 536870912) !== 0)
        t.memoizedState = { baseLanes: 0, cachePool: null }, l !== null && bn(
          t,
          n !== null ? n.cachePool : null
        ), n !== null ? sr(t, n) : rc(), rr(t);
      else
        return u = t.lanes = 536870912, co(
          l,
          t,
          n !== null ? n.baseLanes | e : e,
          e,
          u
        );
    } else
      n !== null ? (bn(t, n.cachePool), sr(t, n), qe(), t.memoizedState = null) : (l !== null && bn(t, null), rc(), qe());
    return kl(l, t, a, e), t.child;
  }
  function Ta(l, t) {
    return l !== null && l.tag === 22 || t.stateNode !== null || (t.stateNode = {
      _visibility: 1,
      _pendingMarkers: null,
      _retryCache: null,
      _transitions: null
    }), t.sibling;
  }
  function co(l, t, e, u, a) {
    var n = ac();
    return n = n === null ? null : { parent: jl._currentValue, pool: n }, t.memoizedState = {
      baseLanes: e,
      cachePool: n
    }, l !== null && bn(t, null), rc(), rr(t), l !== null && xu(l, t, u, !0), t.childLanes = a, null;
  }
  function jn(l, t) {
    return t = Hn(
      { mode: t.mode, children: t.children },
      l.mode
    ), t.ref = l.ref, l.child = t, t.return = l, t;
  }
  function fo(l, t, e) {
    return eu(t, l.child, null, e), l = jn(t, t.pendingProps), l.flags |= 2, gt(t), t.memoizedState = null, l;
  }
  function Mv(l, t, e) {
    var u = t.pendingProps, a = (t.flags & 128) !== 0;
    if (t.flags &= -129, l === null) {
      if (rl) {
        if (u.mode === "hidden")
          return l = jn(t, u), t.lanes = 536870912, Ta(null, l);
        if (dc(t), (l = Al) ? (l = _d(
          l,
          Ot
        ), l = l !== null && l.data === "&" ? l : null, l !== null && (t.memoizedState = {
          dehydrated: l,
          treeContext: ge !== null ? { id: Qt, overflow: Xt } : null,
          retryLane: 536870912,
          hydrationErrors: null
        }, e = Ks(l), e.return = t, t.child = e, Jl = t, Al = null)) : l = null, l === null) throw Se(t);
        return t.lanes = 536870912, null;
      }
      return jn(t, u);
    }
    var n = l.memoizedState;
    if (n !== null) {
      var i = n.dehydrated;
      if (dc(t), a)
        if (t.flags & 256)
          t.flags &= -257, t = fo(
            l,
            t,
            e
          );
        else if (t.memoizedState !== null)
          t.child = l.child, t.flags |= 128, t = null;
        else throw Error(s(558));
      else if (Hl || xu(l, t, e, !1), a = (e & l.childLanes) !== 0, Hl || a) {
        if (u = zl, u !== null && (i = If(u, e), i !== 0 && i !== n.retryLane))
          throw n.retryLane = i, ke(l, i), it(u, l, i), Cc;
        Kn(), t = fo(
          l,
          t,
          e
        );
      } else
        l = n.treeContext, Al = Dt(i.nextSibling), Jl = t, rl = !0, be = null, Ot = !1, l !== null && ks(t, l), t = jn(t, u), t.flags |= 4096;
      return t;
    }
    return l = Wt(l.child, {
      mode: u.mode,
      children: u.children
    }), l.ref = t.ref, t.child = l, l.return = t, l;
  }
  function Rn(l, t) {
    var e = t.ref;
    if (e === null)
      l !== null && l.ref !== null && (t.flags |= 4194816);
    else {
      if (typeof e != "function" && typeof e != "object")
        throw Error(s(284));
      (l === null || l.ref !== e) && (t.flags |= 4194816);
    }
  }
  function jc(l, t, e, u, a) {
    return Ie(t), e = vc(
      l,
      t,
      e,
      u,
      void 0,
      a
    ), u = hc(), l !== null && !Hl ? (mc(l, t, a), ee(l, t, a)) : (rl && u && $i(t), t.flags |= 1, kl(l, t, e, a), t.child);
  }
  function so(l, t, e, u, a, n) {
    return Ie(t), t.updateQueue = null, e = dr(
      t,
      u,
      e,
      a
    ), or(l), u = hc(), l !== null && !Hl ? (mc(l, t, n), ee(l, t, n)) : (rl && u && $i(t), t.flags |= 1, kl(l, t, e, n), t.child);
  }
  function ro(l, t, e, u, a) {
    if (Ie(t), t.stateNode === null) {
      var n = zu, i = e.contextType;
      typeof i == "object" && i !== null && (n = wl(i)), n = new e(u, n), t.memoizedState = n.state !== null && n.state !== void 0 ? n.state : null, n.updater = Dc, t.stateNode = n, n._reactInternals = t, n = t.stateNode, n.props = u, n.state = t.memoizedState, n.refs = {}, ic(t), i = e.contextType, n.context = typeof i == "object" && i !== null ? wl(i) : zu, n.state = t.memoizedState, i = e.getDerivedStateFromProps, typeof i == "function" && (Mc(
        t,
        e,
        i,
        u
      ), n.state = t.memoizedState), typeof e.getDerivedStateFromProps == "function" || typeof n.getSnapshotBeforeUpdate == "function" || typeof n.UNSAFE_componentWillMount != "function" && typeof n.componentWillMount != "function" || (i = n.state, typeof n.componentWillMount == "function" && n.componentWillMount(), typeof n.UNSAFE_componentWillMount == "function" && n.UNSAFE_componentWillMount(), i !== n.state && Dc.enqueueReplaceState(n, n.state, null), _a(t, u, n, a), pa(), n.state = t.memoizedState), typeof n.componentDidMount == "function" && (t.flags |= 4194308), u = !0;
    } else if (l === null) {
      n = t.stateNode;
      var f = t.memoizedProps, d = au(e, f);
      n.props = d;
      var g = n.context, A = e.contextType;
      i = zu, typeof A == "object" && A !== null && (i = wl(A));
      var N = e.getDerivedStateFromProps;
      A = typeof N == "function" || typeof n.getSnapshotBeforeUpdate == "function", f = t.pendingProps !== f, A || typeof n.UNSAFE_componentWillReceiveProps != "function" && typeof n.componentWillReceiveProps != "function" || (f || g !== i) && Wr(
        t,
        n,
        u,
        i
      ), _e = !1;
      var S = t.memoizedState;
      n.state = S, _a(t, u, n, a), pa(), g = t.memoizedState, f || S !== g || _e ? (typeof N == "function" && (Mc(
        t,
        e,
        N,
        u
      ), g = t.memoizedState), (d = _e || $r(
        t,
        e,
        d,
        u,
        S,
        g,
        i
      )) ? (A || typeof n.UNSAFE_componentWillMount != "function" && typeof n.componentWillMount != "function" || (typeof n.componentWillMount == "function" && n.componentWillMount(), typeof n.UNSAFE_componentWillMount == "function" && n.UNSAFE_componentWillMount()), typeof n.componentDidMount == "function" && (t.flags |= 4194308)) : (typeof n.componentDidMount == "function" && (t.flags |= 4194308), t.memoizedProps = u, t.memoizedState = g), n.props = u, n.state = g, n.context = i, u = d) : (typeof n.componentDidMount == "function" && (t.flags |= 4194308), u = !1);
    } else {
      n = t.stateNode, cc(l, t), i = t.memoizedProps, A = au(e, i), n.props = A, N = t.pendingProps, S = n.context, g = e.contextType, d = zu, typeof g == "object" && g !== null && (d = wl(g)), f = e.getDerivedStateFromProps, (g = typeof f == "function" || typeof n.getSnapshotBeforeUpdate == "function") || typeof n.UNSAFE_componentWillReceiveProps != "function" && typeof n.componentWillReceiveProps != "function" || (i !== N || S !== d) && Wr(
        t,
        n,
        u,
        d
      ), _e = !1, S = t.memoizedState, n.state = S, _a(t, u, n, a), pa();
      var E = t.memoizedState;
      i !== N || S !== E || _e || l !== null && l.dependencies !== null && mn(l.dependencies) ? (typeof f == "function" && (Mc(
        t,
        e,
        f,
        u
      ), E = t.memoizedState), (A = _e || $r(
        t,
        e,
        A,
        u,
        S,
        E,
        d
      ) || l !== null && l.dependencies !== null && mn(l.dependencies)) ? (g || typeof n.UNSAFE_componentWillUpdate != "function" && typeof n.componentWillUpdate != "function" || (typeof n.componentWillUpdate == "function" && n.componentWillUpdate(u, E, d), typeof n.UNSAFE_componentWillUpdate == "function" && n.UNSAFE_componentWillUpdate(
        u,
        E,
        d
      )), typeof n.componentDidUpdate == "function" && (t.flags |= 4), typeof n.getSnapshotBeforeUpdate == "function" && (t.flags |= 1024)) : (typeof n.componentDidUpdate != "function" || i === l.memoizedProps && S === l.memoizedState || (t.flags |= 4), typeof n.getSnapshotBeforeUpdate != "function" || i === l.memoizedProps && S === l.memoizedState || (t.flags |= 1024), t.memoizedProps = u, t.memoizedState = E), n.props = u, n.state = E, n.context = d, u = A) : (typeof n.componentDidUpdate != "function" || i === l.memoizedProps && S === l.memoizedState || (t.flags |= 4), typeof n.getSnapshotBeforeUpdate != "function" || i === l.memoizedProps && S === l.memoizedState || (t.flags |= 1024), u = !1);
    }
    return n = u, Rn(l, t), u = (t.flags & 128) !== 0, n || u ? (n = t.stateNode, e = u && typeof e.getDerivedStateFromError != "function" ? null : n.render(), t.flags |= 1, l !== null && u ? (t.child = eu(
      t,
      l.child,
      null,
      a
    ), t.child = eu(
      t,
      null,
      e,
      a
    )) : kl(l, t, e, a), t.memoizedState = n.state, l = t.child) : l = ee(
      l,
      t,
      a
    ), l;
  }
  function oo(l, t, e, u) {
    return We(), t.flags |= 256, kl(l, t, e, u), t.child;
  }
  var Rc = {
    dehydrated: null,
    treeContext: null,
    retryLane: 0,
    hydrationErrors: null
  };
  function Hc(l) {
    return { baseLanes: l, cachePool: lr() };
  }
  function Bc(l, t, e) {
    return l = l !== null ? l.childLanes & ~e : 0, t && (l |= St), l;
  }
  function yo(l, t, e) {
    var u = t.pendingProps, a = !1, n = (t.flags & 128) !== 0, i;
    if ((i = n) || (i = l !== null && l.memoizedState === null ? !1 : (Dl.current & 2) !== 0), i && (a = !0, t.flags &= -129), i = (t.flags & 32) !== 0, t.flags &= -33, l === null) {
      if (rl) {
        if (a ? Ae(t) : qe(), (l = Al) ? (l = _d(
          l,
          Ot
        ), l = l !== null && l.data !== "&" ? l : null, l !== null && (t.memoizedState = {
          dehydrated: l,
          treeContext: ge !== null ? { id: Qt, overflow: Xt } : null,
          retryLane: 536870912,
          hydrationErrors: null
        }, e = Ks(l), e.return = t, t.child = e, Jl = t, Al = null)) : l = null, l === null) throw Se(t);
        return pf(l) ? t.lanes = 32 : t.lanes = 536870912, null;
      }
      var f = u.children;
      return u = u.fallback, a ? (qe(), a = t.mode, f = Hn(
        { mode: "hidden", children: f },
        a
      ), u = $e(
        u,
        a,
        e,
        null
      ), f.return = t, u.return = t, f.sibling = u, t.child = f, u = t.child, u.memoizedState = Hc(e), u.childLanes = Bc(
        l,
        i,
        e
      ), t.memoizedState = Rc, Ta(null, u)) : (Ae(t), Yc(t, f));
    }
    var d = l.memoizedState;
    if (d !== null && (f = d.dehydrated, f !== null)) {
      if (n)
        t.flags & 256 ? (Ae(t), t.flags &= -257, t = Gc(
          l,
          t,
          e
        )) : t.memoizedState !== null ? (qe(), t.child = l.child, t.flags |= 128, t = null) : (qe(), f = u.fallback, a = t.mode, u = Hn(
          { mode: "visible", children: u.children },
          a
        ), f = $e(
          f,
          a,
          e,
          null
        ), f.flags |= 2, u.return = t, f.return = t, u.sibling = f, t.child = u, eu(
          t,
          l.child,
          null,
          e
        ), u = t.child, u.memoizedState = Hc(e), u.childLanes = Bc(
          l,
          i,
          e
        ), t.memoizedState = Rc, t = Ta(null, u));
      else if (Ae(t), pf(f)) {
        if (i = f.nextSibling && f.nextSibling.dataset, i) var g = i.dgst;
        i = g, u = Error(s(419)), u.stack = "", u.digest = i, va({ value: u, source: null, stack: null }), t = Gc(
          l,
          t,
          e
        );
      } else if (Hl || xu(l, t, e, !1), i = (e & l.childLanes) !== 0, Hl || i) {
        if (i = zl, i !== null && (u = If(i, e), u !== 0 && u !== d.retryLane))
          throw d.retryLane = u, ke(l, u), it(i, l, u), Cc;
        Sf(f) || Kn(), t = Gc(
          l,
          t,
          e
        );
      } else
        Sf(f) ? (t.flags |= 192, t.child = l.child, t = null) : (l = d.treeContext, Al = Dt(
          f.nextSibling
        ), Jl = t, rl = !0, be = null, Ot = !1, l !== null && ks(t, l), t = Yc(
          t,
          u.children
        ), t.flags |= 4096);
      return t;
    }
    return a ? (qe(), f = u.fallback, a = t.mode, d = l.child, g = d.sibling, u = Wt(d, {
      mode: "hidden",
      children: u.children
    }), u.subtreeFlags = d.subtreeFlags & 65011712, g !== null ? f = Wt(
      g,
      f
    ) : (f = $e(
      f,
      a,
      e,
      null
    ), f.flags |= 2), f.return = t, u.return = t, u.sibling = f, t.child = u, Ta(null, u), u = t.child, f = l.child.memoizedState, f === null ? f = Hc(e) : (a = f.cachePool, a !== null ? (d = jl._currentValue, a = a.parent !== d ? { parent: d, pool: d } : a) : a = lr(), f = {
      baseLanes: f.baseLanes | e,
      cachePool: a
    }), u.memoizedState = f, u.childLanes = Bc(
      l,
      i,
      e
    ), t.memoizedState = Rc, Ta(l.child, u)) : (Ae(t), e = l.child, l = e.sibling, e = Wt(e, {
      mode: "visible",
      children: u.children
    }), e.return = t, e.sibling = null, l !== null && (i = t.deletions, i === null ? (t.deletions = [l], t.flags |= 16) : i.push(l)), t.child = e, t.memoizedState = null, e);
  }
  function Yc(l, t) {
    return t = Hn(
      { mode: "visible", children: t },
      l.mode
    ), t.return = l, l.child = t;
  }
  function Hn(l, t) {
    return l = ht(22, l, null, t), l.lanes = 0, l;
  }
  function Gc(l, t, e) {
    return eu(t, l.child, null, e), l = Yc(
      t,
      t.pendingProps.children
    ), l.flags |= 2, t.memoizedState = null, l;
  }
  function vo(l, t, e) {
    l.lanes |= t;
    var u = l.alternate;
    u !== null && (u.lanes |= t), lc(l.return, t, e);
  }
  function Lc(l, t, e, u, a, n) {
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
  function ho(l, t, e) {
    var u = t.pendingProps, a = u.revealOrder, n = u.tail;
    u = u.children;
    var i = Dl.current, f = (i & 2) !== 0;
    if (f ? (i = i & 1 | 2, t.flags |= 128) : i &= 1, R(Dl, i), kl(l, t, u, e), u = rl ? ya : 0, !f && l !== null && (l.flags & 128) !== 0)
      l: for (l = t.child; l !== null; ) {
        if (l.tag === 13)
          l.memoizedState !== null && vo(l, e, t);
        else if (l.tag === 19)
          vo(l, e, t);
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
          l = e.alternate, l !== null && An(l) === null && (a = e), e = e.sibling;
        e = a, e === null ? (a = t.child, t.child = null) : (a = e.sibling, e.sibling = null), Lc(
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
          if (l = a.alternate, l !== null && An(l) === null) {
            t.child = a;
            break;
          }
          l = a.sibling, a.sibling = e, e = a, a = l;
        }
        Lc(
          t,
          !0,
          e,
          null,
          n,
          u
        );
        break;
      case "together":
        Lc(
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
    if (l !== null && (t.dependencies = l.dependencies), Ne |= t.lanes, (e & t.childLanes) === 0)
      if (l !== null) {
        if (xu(
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
  function Qc(l, t) {
    return (l.lanes & t) !== 0 ? !0 : (l = l.dependencies, !!(l !== null && mn(l)));
  }
  function Dv(l, t, e) {
    switch (t.tag) {
      case 3:
        Vl(t, t.stateNode.containerInfo), pe(t, jl, l.memoizedState.cache), We();
        break;
      case 27:
      case 5:
        Tl(t);
        break;
      case 4:
        Vl(t, t.stateNode.containerInfo);
        break;
      case 10:
        pe(
          t,
          t.type,
          t.memoizedProps.value
        );
        break;
      case 31:
        if (t.memoizedState !== null)
          return t.flags |= 128, dc(t), null;
        break;
      case 13:
        var u = t.memoizedState;
        if (u !== null)
          return u.dehydrated !== null ? (Ae(t), t.flags |= 128, null) : (e & t.child.childLanes) !== 0 ? yo(l, t, e) : (Ae(t), l = ee(
            l,
            t,
            e
          ), l !== null ? l.sibling : null);
        Ae(t);
        break;
      case 19:
        var a = (l.flags & 128) !== 0;
        if (u = (e & t.childLanes) !== 0, u || (xu(
          l,
          t,
          e,
          !1
        ), u = (e & t.childLanes) !== 0), a) {
          if (u)
            return ho(
              l,
              t,
              e
            );
          t.flags |= 128;
        }
        if (a = t.memoizedState, a !== null && (a.rendering = null, a.tail = null, a.lastEffect = null), R(Dl, Dl.current), u) break;
        return null;
      case 22:
        return t.lanes = 0, io(
          l,
          t,
          e,
          t.pendingProps
        );
      case 24:
        pe(t, jl, l.memoizedState.cache);
    }
    return ee(l, t, e);
  }
  function mo(l, t, e) {
    if (l !== null)
      if (l.memoizedProps !== t.pendingProps)
        Hl = !0;
      else {
        if (!Qc(l, e) && (t.flags & 128) === 0)
          return Hl = !1, Dv(
            l,
            t,
            e
          );
        Hl = (l.flags & 131072) !== 0;
      }
    else
      Hl = !1, rl && (t.flags & 1048576) !== 0 && ws(t, ya, t.index);
    switch (t.lanes = 0, t.tag) {
      case 16:
        l: {
          var u = t.pendingProps;
          if (l = lu(t.elementType), t.type = l, typeof l == "function")
            Ji(l) ? (u = au(l, u), t.tag = 1, t = ro(
              null,
              t,
              l,
              u,
              e
            )) : (t.tag = 0, t = jc(
              null,
              t,
              l,
              u,
              e
            ));
          else {
            if (l != null) {
              var a = l.$$typeof;
              if (a === ll) {
                t.tag = 11, t = uo(
                  null,
                  t,
                  l,
                  u,
                  e
                );
                break l;
              } else if (a === al) {
                t.tag = 14, t = ao(
                  null,
                  t,
                  l,
                  u,
                  e
                );
                break l;
              }
            }
            throw t = jt(l) || l, Error(s(306, t, ""));
          }
        }
        return t;
      case 0:
        return jc(
          l,
          t,
          t.type,
          t.pendingProps,
          e
        );
      case 1:
        return u = t.type, a = au(
          u,
          t.pendingProps
        ), ro(
          l,
          t,
          u,
          a,
          e
        );
      case 3:
        l: {
          if (Vl(
            t,
            t.stateNode.containerInfo
          ), l === null) throw Error(s(387));
          u = t.pendingProps;
          var n = t.memoizedState;
          a = n.element, cc(l, t), _a(t, u, null, e);
          var i = t.memoizedState;
          if (u = i.cache, pe(t, jl, u), u !== n.cache && tc(
            t,
            [jl],
            e,
            !0
          ), pa(), u = i.element, n.isDehydrated)
            if (n = {
              element: u,
              isDehydrated: !1,
              cache: i.cache
            }, t.updateQueue.baseState = n, t.memoizedState = n, t.flags & 256) {
              t = oo(
                l,
                t,
                u,
                e
              );
              break l;
            } else if (u !== a) {
              a = Tt(
                Error(s(424)),
                t
              ), va(a), t = oo(
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
              for (Al = Dt(l.firstChild), Jl = t, rl = !0, be = null, Ot = !0, e = ir(
                t,
                null,
                u,
                e
              ), t.child = e; e; )
                e.flags = e.flags & -3 | 4096, e = e.sibling;
            }
          else {
            if (We(), u === a) {
              t = ee(
                l,
                t,
                e
              );
              break l;
            }
            kl(l, t, u, e);
          }
          t = t.child;
        }
        return t;
      case 26:
        return Rn(l, t), l === null ? (e = xd(
          t.type,
          null,
          t.pendingProps,
          null
        )) ? t.memoizedState = e : rl || (e = t.type, l = t.pendingProps, u = In(
          ul.current
        ).createElement(e), u[Kl] = t, u[lt] = l, $l(u, e, l), Ql(u), t.stateNode = u) : t.memoizedState = xd(
          t.type,
          l.memoizedProps,
          t.pendingProps,
          l.memoizedState
        ), null;
      case 27:
        return Tl(t), l === null && rl && (u = t.stateNode = Ad(
          t.type,
          t.pendingProps,
          ul.current
        ), Jl = t, Ot = !0, a = Al, Ce(t.type) ? (_f = a, Al = Dt(u.firstChild)) : Al = a), kl(
          l,
          t,
          t.pendingProps.children,
          e
        ), Rn(l, t), l === null && (t.flags |= 4194304), t.child;
      case 5:
        return l === null && rl && ((a = u = Al) && (u = ch(
          u,
          t.type,
          t.pendingProps,
          Ot
        ), u !== null ? (t.stateNode = u, Jl = t, Al = Dt(u.firstChild), Ot = !1, a = !0) : a = !1), a || Se(t)), Tl(t), a = t.type, n = t.pendingProps, i = l !== null ? l.memoizedProps : null, u = n.children, mf(a, n) ? u = null : i !== null && mf(a, i) && (t.flags |= 32), t.memoizedState !== null && (a = vc(
          l,
          t,
          Ev,
          null,
          null,
          e
        ), Qa._currentValue = a), Rn(l, t), kl(l, t, u, e), t.child;
      case 6:
        return l === null && rl && ((l = e = Al) && (e = fh(
          e,
          t.pendingProps,
          Ot
        ), e !== null ? (t.stateNode = e, Jl = t, Al = null, l = !0) : l = !1), l || Se(t)), null;
      case 13:
        return yo(l, t, e);
      case 4:
        return Vl(
          t,
          t.stateNode.containerInfo
        ), u = t.pendingProps, l === null ? t.child = eu(
          t,
          null,
          u,
          e
        ) : kl(l, t, u, e), t.child;
      case 11:
        return uo(
          l,
          t,
          t.type,
          t.pendingProps,
          e
        );
      case 7:
        return kl(
          l,
          t,
          t.pendingProps,
          e
        ), t.child;
      case 8:
        return kl(
          l,
          t,
          t.pendingProps.children,
          e
        ), t.child;
      case 12:
        return kl(
          l,
          t,
          t.pendingProps.children,
          e
        ), t.child;
      case 10:
        return u = t.pendingProps, pe(t, t.type, u.value), kl(l, t, u.children, e), t.child;
      case 9:
        return a = t.type._context, u = t.pendingProps.children, Ie(t), a = wl(a), u = u(a), t.flags |= 1, kl(l, t, u, e), t.child;
      case 14:
        return ao(
          l,
          t,
          t.type,
          t.pendingProps,
          e
        );
      case 15:
        return no(
          l,
          t,
          t.type,
          t.pendingProps,
          e
        );
      case 19:
        return ho(l, t, e);
      case 31:
        return Mv(l, t, e);
      case 22:
        return io(
          l,
          t,
          e,
          t.pendingProps
        );
      case 24:
        return Ie(t), u = wl(jl), l === null ? (a = ac(), a === null && (a = zl, n = ec(), a.pooledCache = n, n.refCount++, n !== null && (a.pooledCacheLanes |= e), a = n), t.memoizedState = { parent: u, cache: a }, ic(t), pe(t, jl, a)) : ((l.lanes & e) !== 0 && (cc(l, t), _a(t, null, null, e), pa()), a = l.memoizedState, n = t.memoizedState, a.parent !== u ? (a = { parent: u, cache: u }, t.memoizedState = a, t.lanes === 0 && (t.memoizedState = t.updateQueue.baseState = a), pe(t, jl, u)) : (u = n.cache, pe(t, jl, u), u !== a.cache && tc(
          t,
          [jl],
          e,
          !0
        ))), kl(
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
  function Xc(l, t, e, u, a) {
    if ((t = (l.mode & 32) !== 0) && (t = !1), t) {
      if (l.flags |= 16777216, (a & 335544128) === a)
        if (l.stateNode.complete) l.flags |= 8192;
        else if (Zo()) l.flags |= 8192;
        else
          throw tu = pn, nc;
    } else l.flags &= -16777217;
  }
  function go(l, t) {
    if (t.type !== "stylesheet" || (t.state.loading & 4) !== 0)
      l.flags &= -16777217;
    else if (l.flags |= 16777216, !Ud(t))
      if (Zo()) l.flags |= 8192;
      else
        throw tu = pn, nc;
  }
  function Bn(l, t) {
    t !== null && (l.flags |= 4), l.flags & 16384 && (t = l.tag !== 22 ? $f() : 536870912, l.lanes |= t, Gu |= t);
  }
  function xa(l, t) {
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
  function ql(l) {
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
    switch (Wi(t), t.tag) {
      case 16:
      case 15:
      case 0:
      case 11:
      case 7:
      case 8:
      case 12:
      case 9:
      case 14:
        return ql(t), null;
      case 1:
        return ql(t), null;
      case 3:
        return e = t.stateNode, u = null, l !== null && (u = l.memoizedState.cache), t.memoizedState.cache !== u && (t.flags |= 2048), Pt(jl), G(), e.pendingContext && (e.context = e.pendingContext, e.pendingContext = null), (l === null || l.child === null) && (Tu(t) ? ue(t) : l === null || l.memoizedState.isDehydrated && (t.flags & 256) === 0 || (t.flags |= 1024, Ii())), ql(t), null;
      case 26:
        var a = t.type, n = t.memoizedState;
        return l === null ? (ue(t), n !== null ? (ql(t), go(t, n)) : (ql(t), Xc(
          t,
          a,
          null,
          u,
          e
        ))) : n ? n !== l.memoizedState ? (ue(t), ql(t), go(t, n)) : (ql(t), t.flags &= -16777217) : (l = l.memoizedProps, l !== u && ue(t), ql(t), Xc(
          t,
          a,
          l,
          u,
          e
        )), null;
      case 27:
        if (Et(t), e = ul.current, a = t.type, l !== null && t.stateNode != null)
          l.memoizedProps !== u && ue(t);
        else {
          if (!u) {
            if (t.stateNode === null)
              throw Error(s(166));
            return ql(t), null;
          }
          l = Y.current, Tu(t) ? $s(t) : (l = Ad(a, u, e), t.stateNode = l, ue(t));
        }
        return ql(t), null;
      case 5:
        if (Et(t), a = t.type, l !== null && t.stateNode != null)
          l.memoizedProps !== u && ue(t);
        else {
          if (!u) {
            if (t.stateNode === null)
              throw Error(s(166));
            return ql(t), null;
          }
          if (n = Y.current, Tu(t))
            $s(t);
          else {
            var i = In(
              ul.current
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
            n[Kl] = t, n[lt] = u;
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
            l: switch ($l(n, a, u), a) {
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
        return ql(t), Xc(
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
          if (l = ul.current, Tu(t)) {
            if (l = t.stateNode, e = t.memoizedProps, u = null, a = Jl, a !== null)
              switch (a.tag) {
                case 27:
                case 5:
                  u = a.memoizedProps;
              }
            l[Kl] = t, l = !!(l.nodeValue === e || u !== null && u.suppressHydrationWarning === !0 || yd(l.nodeValue, e)), l || Se(t, !0);
          } else
            l = In(l).createTextNode(
              u
            ), l[Kl] = t, t.stateNode = l;
        }
        return ql(t), null;
      case 31:
        if (e = t.memoizedState, l === null || l.memoizedState !== null) {
          if (u = Tu(t), e !== null) {
            if (l === null) {
              if (!u) throw Error(s(318));
              if (l = t.memoizedState, l = l !== null ? l.dehydrated : null, !l) throw Error(s(557));
              l[Kl] = t;
            } else
              We(), (t.flags & 128) === 0 && (t.memoizedState = null), t.flags |= 4;
            ql(t), l = !1;
          } else
            e = Ii(), l !== null && l.memoizedState !== null && (l.memoizedState.hydrationErrors = e), l = !0;
          if (!l)
            return t.flags & 256 ? (gt(t), t) : (gt(t), null);
          if ((t.flags & 128) !== 0)
            throw Error(s(558));
        }
        return ql(t), null;
      case 13:
        if (u = t.memoizedState, l === null || l.memoizedState !== null && l.memoizedState.dehydrated !== null) {
          if (a = Tu(t), u !== null && u.dehydrated !== null) {
            if (l === null) {
              if (!a) throw Error(s(318));
              if (a = t.memoizedState, a = a !== null ? a.dehydrated : null, !a) throw Error(s(317));
              a[Kl] = t;
            } else
              We(), (t.flags & 128) === 0 && (t.memoizedState = null), t.flags |= 4;
            ql(t), a = !1;
          } else
            a = Ii(), l !== null && l.memoizedState !== null && (l.memoizedState.hydrationErrors = a), a = !0;
          if (!a)
            return t.flags & 256 ? (gt(t), t) : (gt(t), null);
        }
        return gt(t), (t.flags & 128) !== 0 ? (t.lanes = e, t) : (e = u !== null, l = l !== null && l.memoizedState !== null, e && (u = t.child, a = null, u.alternate !== null && u.alternate.memoizedState !== null && u.alternate.memoizedState.cachePool !== null && (a = u.alternate.memoizedState.cachePool.pool), n = null, u.memoizedState !== null && u.memoizedState.cachePool !== null && (n = u.memoizedState.cachePool.pool), n !== a && (u.flags |= 2048)), e !== l && e && (t.child.flags |= 8192), Bn(t, t.updateQueue), ql(t), null);
      case 4:
        return G(), l === null && of(t.stateNode.containerInfo), ql(t), null;
      case 10:
        return Pt(t.type), ql(t), null;
      case 19:
        if (O(Dl), u = t.memoizedState, u === null) return ql(t), null;
        if (a = (t.flags & 128) !== 0, n = u.rendering, n === null)
          if (a) xa(u, !1);
          else {
            if (Ml !== 0 || l !== null && (l.flags & 128) !== 0)
              for (l = t.child; l !== null; ) {
                if (n = An(l), n !== null) {
                  for (t.flags |= 128, xa(u, !1), l = n.updateQueue, t.updateQueue = l, Bn(t, l), t.subtreeFlags = 0, l = e, e = t.child; e !== null; )
                    Vs(e, l), e = e.sibling;
                  return R(
                    Dl,
                    Dl.current & 1 | 2
                  ), rl && Ft(t, u.treeForkCount), t.child;
                }
                l = l.sibling;
              }
            u.tail !== null && ot() > Xn && (t.flags |= 128, a = !0, xa(u, !1), t.lanes = 4194304);
          }
        else {
          if (!a)
            if (l = An(n), l !== null) {
              if (t.flags |= 128, a = !0, l = l.updateQueue, t.updateQueue = l, Bn(t, l), xa(u, !0), u.tail === null && u.tailMode === "hidden" && !n.alternate && !rl)
                return ql(t), null;
            } else
              2 * ot() - u.renderingStartTime > Xn && e !== 536870912 && (t.flags |= 128, a = !0, xa(u, !1), t.lanes = 4194304);
          u.isBackwards ? (n.sibling = t.child, t.child = n) : (l = u.last, l !== null ? l.sibling = n : t.child = n, u.last = n);
        }
        return u.tail !== null ? (l = u.tail, u.rendering = l, u.tail = l.sibling, u.renderingStartTime = ot(), l.sibling = null, e = Dl.current, R(
          Dl,
          a ? e & 1 | 2 : e & 1
        ), rl && Ft(t, u.treeForkCount), l) : (ql(t), null);
      case 22:
      case 23:
        return gt(t), oc(), u = t.memoizedState !== null, l !== null ? l.memoizedState !== null !== u && (t.flags |= 8192) : u && (t.flags |= 8192), u ? (e & 536870912) !== 0 && (t.flags & 128) === 0 && (ql(t), t.subtreeFlags & 6 && (t.flags |= 8192)) : ql(t), e = t.updateQueue, e !== null && Bn(t, e.retryQueue), e = null, l !== null && l.memoizedState !== null && l.memoizedState.cachePool !== null && (e = l.memoizedState.cachePool.pool), u = null, t.memoizedState !== null && t.memoizedState.cachePool !== null && (u = t.memoizedState.cachePool.pool), u !== e && (t.flags |= 2048), l !== null && O(Pe), null;
      case 24:
        return e = null, l !== null && (e = l.memoizedState.cache), t.memoizedState.cache !== e && (t.flags |= 2048), Pt(jl), ql(t), null;
      case 25:
        return null;
      case 30:
        return null;
    }
    throw Error(s(156, t.tag));
  }
  function Cv(l, t) {
    switch (Wi(t), t.tag) {
      case 1:
        return l = t.flags, l & 65536 ? (t.flags = l & -65537 | 128, t) : null;
      case 3:
        return Pt(jl), G(), l = t.flags, (l & 65536) !== 0 && (l & 128) === 0 ? (t.flags = l & -65537 | 128, t) : null;
      case 26:
      case 27:
      case 5:
        return Et(t), null;
      case 31:
        if (t.memoizedState !== null) {
          if (gt(t), t.alternate === null)
            throw Error(s(340));
          We();
        }
        return l = t.flags, l & 65536 ? (t.flags = l & -65537 | 128, t) : null;
      case 13:
        if (gt(t), l = t.memoizedState, l !== null && l.dehydrated !== null) {
          if (t.alternate === null)
            throw Error(s(340));
          We();
        }
        return l = t.flags, l & 65536 ? (t.flags = l & -65537 | 128, t) : null;
      case 19:
        return O(Dl), null;
      case 4:
        return G(), null;
      case 10:
        return Pt(t.type), null;
      case 22:
      case 23:
        return gt(t), oc(), l !== null && O(Pe), l = t.flags, l & 65536 ? (t.flags = l & -65537 | 128, t) : null;
      case 24:
        return Pt(jl), null;
      case 25:
        return null;
      default:
        return null;
    }
  }
  function bo(l, t) {
    switch (Wi(t), t.tag) {
      case 3:
        Pt(jl), G();
        break;
      case 26:
      case 27:
      case 5:
        Et(t);
        break;
      case 4:
        G();
        break;
      case 31:
        t.memoizedState !== null && gt(t);
        break;
      case 13:
        gt(t);
        break;
      case 19:
        O(Dl);
        break;
      case 10:
        Pt(t.type);
        break;
      case 22:
      case 23:
        gt(t), oc(), l !== null && O(Pe);
        break;
      case 24:
        Pt(jl);
    }
  }
  function Na(l, t) {
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
      Sl(t, t.return, f);
    }
  }
  function Te(l, t, e) {
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
              } catch (A) {
                Sl(
                  a,
                  d,
                  A
                );
              }
            }
          }
          u = u.next;
        } while (u !== n);
      }
    } catch (A) {
      Sl(t, t.return, A);
    }
  }
  function So(l) {
    var t = l.updateQueue;
    if (t !== null) {
      var e = l.stateNode;
      try {
        fr(t, e);
      } catch (u) {
        Sl(l, l.return, u);
      }
    }
  }
  function po(l, t, e) {
    e.props = au(
      l.type,
      l.memoizedProps
    ), e.state = l.memoizedState;
    try {
      e.componentWillUnmount();
    } catch (u) {
      Sl(l, t, u);
    }
  }
  function Oa(l, t) {
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
      Sl(l, t, a);
    }
  }
  function Zt(l, t) {
    var e = l.ref, u = l.refCleanup;
    if (e !== null)
      if (typeof u == "function")
        try {
          u();
        } catch (a) {
          Sl(l, t, a);
        } finally {
          l.refCleanup = null, l = l.alternate, l != null && (l.refCleanup = null);
        }
      else if (typeof e == "function")
        try {
          e(null);
        } catch (a) {
          Sl(l, t, a);
        }
      else e.current = null;
  }
  function _o(l) {
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
      Sl(l, l.return, a);
    }
  }
  function Zc(l, t, e) {
    try {
      var u = l.stateNode;
      th(u, l.type, e, t), u[lt] = t;
    } catch (a) {
      Sl(l, l.return, a);
    }
  }
  function Eo(l) {
    return l.tag === 5 || l.tag === 3 || l.tag === 26 || l.tag === 27 && Ce(l.type) || l.tag === 4;
  }
  function Vc(l) {
    l: for (; ; ) {
      for (; l.sibling === null; ) {
        if (l.return === null || Eo(l.return)) return null;
        l = l.return;
      }
      for (l.sibling.return = l.return, l = l.sibling; l.tag !== 5 && l.tag !== 6 && l.tag !== 18; ) {
        if (l.tag === 27 && Ce(l.type) || l.flags & 2 || l.child === null || l.tag === 4) continue l;
        l.child.return = l, l = l.child;
      }
      if (!(l.flags & 2)) return l.stateNode;
    }
  }
  function Kc(l, t, e) {
    var u = l.tag;
    if (u === 5 || u === 6)
      l = l.stateNode, t ? (e.nodeType === 9 ? e.body : e.nodeName === "HTML" ? e.ownerDocument.body : e).insertBefore(l, t) : (t = e.nodeType === 9 ? e.body : e.nodeName === "HTML" ? e.ownerDocument.body : e, t.appendChild(l), e = e._reactRootContainer, e != null || t.onclick !== null || (t.onclick = kt));
    else if (u !== 4 && (u === 27 && Ce(l.type) && (e = l.stateNode, t = null), l = l.child, l !== null))
      for (Kc(l, t, e), l = l.sibling; l !== null; )
        Kc(l, t, e), l = l.sibling;
  }
  function Yn(l, t, e) {
    var u = l.tag;
    if (u === 5 || u === 6)
      l = l.stateNode, t ? e.insertBefore(l, t) : e.appendChild(l);
    else if (u !== 4 && (u === 27 && Ce(l.type) && (e = l.stateNode), l = l.child, l !== null))
      for (Yn(l, t, e), l = l.sibling; l !== null; )
        Yn(l, t, e), l = l.sibling;
  }
  function zo(l) {
    var t = l.stateNode, e = l.memoizedProps;
    try {
      for (var u = l.type, a = t.attributes; a.length; )
        t.removeAttributeNode(a[0]);
      $l(t, u, e), t[Kl] = l, t[lt] = e;
    } catch (n) {
      Sl(l, l.return, n);
    }
  }
  var ae = !1, Bl = !1, Jc = !1, Ao = typeof WeakSet == "function" ? WeakSet : Set, Xl = null;
  function jv(l, t) {
    if (l = l.containerInfo, vf = ni, l = Rs(l), Gi(l)) {
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
            var i = 0, f = -1, d = -1, g = 0, A = 0, N = l, S = null;
            t: for (; ; ) {
              for (var E; N !== e || a !== 0 && N.nodeType !== 3 || (f = i + a), N !== n || u !== 0 && N.nodeType !== 3 || (d = i + u), N.nodeType === 3 && (i += N.nodeValue.length), (E = N.firstChild) !== null; )
                S = N, N = E;
              for (; ; ) {
                if (N === l) break t;
                if (S === e && ++g === a && (f = i), S === n && ++A === u && (d = i), (E = N.nextSibling) !== null) break;
                N = S, S = N.parentNode;
              }
              N = E;
            }
            e = f === -1 || d === -1 ? null : { start: f, end: d };
          } else e = null;
        }
      e = e || { start: 0, end: 0 };
    } else e = null;
    for (hf = { focusedElem: l, selectionRange: e }, ni = !1, Xl = t; Xl !== null; )
      if (t = Xl, l = t.child, (t.subtreeFlags & 1028) !== 0 && l !== null)
        l.return = t, Xl = l;
      else
        for (; Xl !== null; ) {
          switch (t = Xl, n = t.alternate, l = t.flags, t.tag) {
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
                  var B = au(
                    e.type,
                    a
                  );
                  l = u.getSnapshotBeforeUpdate(
                    B,
                    n
                  ), u.__reactInternalSnapshotBeforeUpdate = l;
                } catch (w) {
                  Sl(
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
                  bf(l);
                else if (e === 1)
                  switch (l.nodeName) {
                    case "HEAD":
                    case "HTML":
                    case "BODY":
                      bf(l);
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
            l.return = t.return, Xl = l;
            break;
          }
          Xl = t.return;
        }
  }
  function qo(l, t, e) {
    var u = e.flags;
    switch (e.tag) {
      case 0:
      case 11:
      case 15:
        ie(l, e), u & 4 && Na(5, e);
        break;
      case 1:
        if (ie(l, e), u & 4)
          if (l = e.stateNode, t === null)
            try {
              l.componentDidMount();
            } catch (i) {
              Sl(e, e.return, i);
            }
          else {
            var a = au(
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
              Sl(
                e,
                e.return,
                i
              );
            }
          }
        u & 64 && So(e), u & 512 && Oa(e, e.return);
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
            fr(l, t);
          } catch (i) {
            Sl(e, e.return, i);
          }
        }
        break;
      case 27:
        t === null && u & 4 && zo(e);
      case 26:
      case 5:
        ie(l, e), t === null && u & 4 && _o(e), u & 512 && Oa(e, e.return);
        break;
      case 12:
        ie(l, e);
        break;
      case 31:
        ie(l, e), u & 4 && No(l, e);
        break;
      case 13:
        ie(l, e), u & 4 && Oo(l, e), u & 64 && (l = e.memoizedState, l !== null && (l = l.dehydrated, l !== null && (e = Zv.bind(
          null,
          e
        ), sh(l, e))));
        break;
      case 22:
        if (u = e.memoizedState !== null || ae, !u) {
          t = t !== null && t.memoizedState !== null || Bl, a = ae;
          var n = Bl;
          ae = u, (Bl = t) && !n ? ce(
            l,
            e,
            (e.subtreeFlags & 8772) !== 0
          ) : ie(l, e), ae = a, Bl = n;
        }
        break;
      case 30:
        break;
      default:
        ie(l, e);
    }
  }
  function To(l) {
    var t = l.alternate;
    t !== null && (l.alternate = null, To(t)), l.child = null, l.deletions = null, l.sibling = null, l.tag === 5 && (t = l.stateNode, t !== null && Ei(t)), l.stateNode = null, l.return = null, l.dependencies = null, l.memoizedProps = null, l.memoizedState = null, l.pendingProps = null, l.stateNode = null, l.updateQueue = null;
  }
  var xl = null, et = !1;
  function ne(l, t, e) {
    for (e = e.child; e !== null; )
      xo(l, t, e), e = e.sibling;
  }
  function xo(l, t, e) {
    if (dt && typeof dt.onCommitFiberUnmount == "function")
      try {
        dt.onCommitFiberUnmount(Pu, e);
      } catch {
      }
    switch (e.tag) {
      case 26:
        Bl || Zt(e, t), ne(
          l,
          t,
          e
        ), e.memoizedState ? e.memoizedState.count-- : e.stateNode && (e = e.stateNode, e.parentNode.removeChild(e));
        break;
      case 27:
        Bl || Zt(e, t);
        var u = xl, a = et;
        Ce(e.type) && (xl = e.stateNode, et = !1), ne(
          l,
          t,
          e
        ), Ya(e.stateNode), xl = u, et = a;
        break;
      case 5:
        Bl || Zt(e, t);
      case 6:
        if (u = xl, a = et, xl = null, ne(
          l,
          t,
          e
        ), xl = u, et = a, xl !== null)
          if (et)
            try {
              (xl.nodeType === 9 ? xl.body : xl.nodeName === "HTML" ? xl.ownerDocument.body : xl).removeChild(e.stateNode);
            } catch (n) {
              Sl(
                e,
                t,
                n
              );
            }
          else
            try {
              xl.removeChild(e.stateNode);
            } catch (n) {
              Sl(
                e,
                t,
                n
              );
            }
        break;
      case 18:
        xl !== null && (et ? (l = xl, Sd(
          l.nodeType === 9 ? l.body : l.nodeName === "HTML" ? l.ownerDocument.body : l,
          e.stateNode
        ), wu(l)) : Sd(xl, e.stateNode));
        break;
      case 4:
        u = xl, a = et, xl = e.stateNode.containerInfo, et = !0, ne(
          l,
          t,
          e
        ), xl = u, et = a;
        break;
      case 0:
      case 11:
      case 14:
      case 15:
        Te(2, e, t), Bl || Te(4, e, t), ne(
          l,
          t,
          e
        );
        break;
      case 1:
        Bl || (Zt(e, t), u = e.stateNode, typeof u.componentWillUnmount == "function" && po(
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
        Bl = (u = Bl) || e.memoizedState !== null, ne(
          l,
          t,
          e
        ), Bl = u;
        break;
      default:
        ne(
          l,
          t,
          e
        );
    }
  }
  function No(l, t) {
    if (t.memoizedState === null && (l = t.alternate, l !== null && (l = l.memoizedState, l !== null))) {
      l = l.dehydrated;
      try {
        wu(l);
      } catch (e) {
        Sl(t, t.return, e);
      }
    }
  }
  function Oo(l, t) {
    if (t.memoizedState === null && (l = t.alternate, l !== null && (l = l.memoizedState, l !== null && (l = l.dehydrated, l !== null))))
      try {
        wu(l);
      } catch (e) {
        Sl(t, t.return, e);
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
  function Gn(l, t) {
    var e = Rv(l);
    t.forEach(function(u) {
      if (!e.has(u)) {
        e.add(u);
        var a = Vv.bind(null, l, u);
        u.then(a, a);
      }
    });
  }
  function ut(l, t) {
    var e = t.deletions;
    if (e !== null)
      for (var u = 0; u < e.length; u++) {
        var a = e[u], n = l, i = t, f = i;
        l: for (; f !== null; ) {
          switch (f.tag) {
            case 27:
              if (Ce(f.type)) {
                xl = f.stateNode, et = !1;
                break l;
              }
              break;
            case 5:
              xl = f.stateNode, et = !1;
              break l;
            case 3:
            case 4:
              xl = f.stateNode.containerInfo, et = !0;
              break l;
          }
          f = f.return;
        }
        if (xl === null) throw Error(s(160));
        xo(n, i, a), xl = null, et = !1, n = a.alternate, n !== null && (n.return = null), a.return = null;
      }
    if (t.subtreeFlags & 13886)
      for (t = t.child; t !== null; )
        Mo(t, l), t = t.sibling;
  }
  var Ht = null;
  function Mo(l, t) {
    var e = l.alternate, u = l.flags;
    switch (l.tag) {
      case 0:
      case 11:
      case 14:
      case 15:
        ut(t, l), at(l), u & 4 && (Te(3, l, l.return), Na(3, l), Te(5, l, l.return));
        break;
      case 1:
        ut(t, l), at(l), u & 512 && (Bl || e === null || Zt(e, e.return)), u & 64 && ae && (l = l.updateQueue, l !== null && (u = l.callbacks, u !== null && (e = l.shared.hiddenCallbacks, l.shared.hiddenCallbacks = e === null ? u : e.concat(u))));
        break;
      case 26:
        var a = Ht;
        if (ut(t, l), at(l), u & 512 && (Bl || e === null || Zt(e, e.return)), u & 4) {
          var n = e !== null ? e.memoizedState : null;
          if (u = l.memoizedState, e === null)
            if (u === null)
              if (l.stateNode === null) {
                l: {
                  u = l.type, e = l.memoizedProps, a = a.ownerDocument || a;
                  t: switch (u) {
                    case "title":
                      n = a.getElementsByTagName("title")[0], (!n || n[ea] || n[Kl] || n.namespaceURI === "http://www.w3.org/2000/svg" || n.hasAttribute("itemprop")) && (n = a.createElement(u), a.head.insertBefore(
                        n,
                        a.querySelector("head > title")
                      )), $l(n, u, e), n[Kl] = l, Ql(n), u = n;
                      break l;
                    case "link":
                      var i = Md(
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
                      n = a.createElement(u), $l(n, u, e), a.head.appendChild(n);
                      break;
                    case "meta":
                      if (i = Md(
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
                      n = a.createElement(u), $l(n, u, e), a.head.appendChild(n);
                      break;
                    default:
                      throw Error(s(468, u));
                  }
                  n[Kl] = l, Ql(n), u = n;
                }
                l.stateNode = u;
              } else
                Dd(
                  a,
                  l.type,
                  l.stateNode
                );
            else
              l.stateNode = Od(
                a,
                u,
                l.memoizedProps
              );
          else
            n !== u ? (n === null ? e.stateNode !== null && (e = e.stateNode, e.parentNode.removeChild(e)) : n.count--, u === null ? Dd(
              a,
              l.type,
              l.stateNode
            ) : Od(
              a,
              u,
              l.memoizedProps
            )) : u === null && l.stateNode !== null && Zc(
              l,
              l.memoizedProps,
              e.memoizedProps
            );
        }
        break;
      case 27:
        ut(t, l), at(l), u & 512 && (Bl || e === null || Zt(e, e.return)), e !== null && u & 4 && Zc(
          l,
          l.memoizedProps,
          e.memoizedProps
        );
        break;
      case 5:
        if (ut(t, l), at(l), u & 512 && (Bl || e === null || Zt(e, e.return)), l.flags & 32) {
          a = l.stateNode;
          try {
            mu(a, "");
          } catch (B) {
            Sl(l, l.return, B);
          }
        }
        u & 4 && l.stateNode != null && (a = l.memoizedProps, Zc(
          l,
          a,
          e !== null ? e.memoizedProps : a
        )), u & 1024 && (Jc = !0);
        break;
      case 6:
        if (ut(t, l), at(l), u & 4) {
          if (l.stateNode === null)
            throw Error(s(162));
          u = l.memoizedProps, e = l.stateNode;
          try {
            e.nodeValue = u;
          } catch (B) {
            Sl(l, l.return, B);
          }
        }
        break;
      case 3:
        if (ti = null, a = Ht, Ht = Pn(t.containerInfo), ut(t, l), Ht = a, at(l), u & 4 && e !== null && e.memoizedState.isDehydrated)
          try {
            wu(t.containerInfo);
          } catch (B) {
            Sl(l, l.return, B);
          }
        Jc && (Jc = !1, Do(l));
        break;
      case 4:
        u = Ht, Ht = Pn(
          l.stateNode.containerInfo
        ), ut(t, l), at(l), Ht = u;
        break;
      case 12:
        ut(t, l), at(l);
        break;
      case 31:
        ut(t, l), at(l), u & 4 && (u = l.updateQueue, u !== null && (l.updateQueue = null, Gn(l, u)));
        break;
      case 13:
        ut(t, l), at(l), l.child.flags & 8192 && l.memoizedState !== null != (e !== null && e.memoizedState !== null) && (Qn = ot()), u & 4 && (u = l.updateQueue, u !== null && (l.updateQueue = null, Gn(l, u)));
        break;
      case 22:
        a = l.memoizedState !== null;
        var d = e !== null && e.memoizedState !== null, g = ae, A = Bl;
        if (ae = g || a, Bl = A || d, ut(t, l), Bl = A, ae = g, at(l), u & 8192)
          l: for (t = l.stateNode, t._visibility = a ? t._visibility & -2 : t._visibility | 1, a && (e === null || d || ae || Bl || nu(l)), e = null, t = l; ; ) {
            if (t.tag === 5 || t.tag === 26) {
              if (e === null) {
                d = e = t;
                try {
                  if (n = d.stateNode, a)
                    i = n.style, typeof i.setProperty == "function" ? i.setProperty("display", "none", "important") : i.display = "none";
                  else {
                    f = d.stateNode;
                    var N = d.memoizedProps.style, S = N != null && N.hasOwnProperty("display") ? N.display : null;
                    f.style.display = S == null || typeof S == "boolean" ? "" : ("" + S).trim();
                  }
                } catch (B) {
                  Sl(d, d.return, B);
                }
              }
            } else if (t.tag === 6) {
              if (e === null) {
                d = t;
                try {
                  d.stateNode.nodeValue = a ? "" : d.memoizedProps;
                } catch (B) {
                  Sl(d, d.return, B);
                }
              }
            } else if (t.tag === 18) {
              if (e === null) {
                d = t;
                try {
                  var E = d.stateNode;
                  a ? pd(E, !0) : pd(d.stateNode, !1);
                } catch (B) {
                  Sl(d, d.return, B);
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
        u & 4 && (u = l.updateQueue, u !== null && (e = u.retryQueue, e !== null && (u.retryQueue = null, Gn(l, e))));
        break;
      case 19:
        ut(t, l), at(l), u & 4 && (u = l.updateQueue, u !== null && (l.updateQueue = null, Gn(l, u)));
        break;
      case 30:
        break;
      case 21:
        break;
      default:
        ut(t, l), at(l);
    }
  }
  function at(l) {
    var t = l.flags;
    if (t & 2) {
      try {
        for (var e, u = l.return; u !== null; ) {
          if (Eo(u)) {
            e = u;
            break;
          }
          u = u.return;
        }
        if (e == null) throw Error(s(160));
        switch (e.tag) {
          case 27:
            var a = e.stateNode, n = Vc(l);
            Yn(l, n, a);
            break;
          case 5:
            var i = e.stateNode;
            e.flags & 32 && (mu(i, ""), e.flags &= -33);
            var f = Vc(l);
            Yn(l, f, i);
            break;
          case 3:
          case 4:
            var d = e.stateNode.containerInfo, g = Vc(l);
            Kc(
              l,
              g,
              d
            );
            break;
          default:
            throw Error(s(161));
        }
      } catch (A) {
        Sl(l, l.return, A);
      }
      l.flags &= -3;
    }
    t & 4096 && (l.flags &= -4097);
  }
  function Do(l) {
    if (l.subtreeFlags & 1024)
      for (l = l.child; l !== null; ) {
        var t = l;
        Do(t), t.tag === 5 && t.flags & 1024 && t.stateNode.reset(), l = l.sibling;
      }
  }
  function ie(l, t) {
    if (t.subtreeFlags & 8772)
      for (t = t.child; t !== null; )
        qo(l, t.alternate, t), t = t.sibling;
  }
  function nu(l) {
    for (l = l.child; l !== null; ) {
      var t = l;
      switch (t.tag) {
        case 0:
        case 11:
        case 14:
        case 15:
          Te(4, t, t.return), nu(t);
          break;
        case 1:
          Zt(t, t.return);
          var e = t.stateNode;
          typeof e.componentWillUnmount == "function" && po(
            t,
            t.return,
            e
          ), nu(t);
          break;
        case 27:
          Ya(t.stateNode);
        case 26:
        case 5:
          Zt(t, t.return), nu(t);
          break;
        case 22:
          t.memoizedState === null && nu(t);
          break;
        case 30:
          nu(t);
          break;
        default:
          nu(t);
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
          ), Na(4, n);
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
              Sl(u, u.return, g);
            }
          if (u = n, a = u.updateQueue, a !== null) {
            var f = u.stateNode;
            try {
              var d = a.shared.hiddenCallbacks;
              if (d !== null)
                for (a.shared.hiddenCallbacks = null, a = 0; a < d.length; a++)
                  cr(d[a], f);
            } catch (g) {
              Sl(u, u.return, g);
            }
          }
          e && i & 64 && So(n), Oa(n, n.return);
          break;
        case 27:
          zo(n);
        case 26:
        case 5:
          ce(
            a,
            n,
            e
          ), e && u === null && i & 4 && _o(n), Oa(n, n.return);
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
          ), e && i & 4 && No(a, n);
          break;
        case 13:
          ce(
            a,
            n,
            e
          ), e && i & 4 && Oo(a, n);
          break;
        case 22:
          n.memoizedState === null && ce(
            a,
            n,
            e
          ), Oa(n, n.return);
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
  function wc(l, t) {
    var e = null;
    l !== null && l.memoizedState !== null && l.memoizedState.cachePool !== null && (e = l.memoizedState.cachePool.pool), l = null, t.memoizedState !== null && t.memoizedState.cachePool !== null && (l = t.memoizedState.cachePool.pool), l !== e && (l != null && l.refCount++, e != null && ha(e));
  }
  function kc(l, t) {
    l = null, t.alternate !== null && (l = t.alternate.memoizedState.cache), t = t.memoizedState.cache, t !== l && (t.refCount++, l != null && ha(l));
  }
  function Bt(l, t, e, u) {
    if (t.subtreeFlags & 10256)
      for (t = t.child; t !== null; )
        Uo(
          l,
          t,
          e,
          u
        ), t = t.sibling;
  }
  function Uo(l, t, e, u) {
    var a = t.flags;
    switch (t.tag) {
      case 0:
      case 11:
      case 15:
        Bt(
          l,
          t,
          e,
          u
        ), a & 2048 && Na(9, t);
        break;
      case 1:
        Bt(
          l,
          t,
          e,
          u
        );
        break;
      case 3:
        Bt(
          l,
          t,
          e,
          u
        ), a & 2048 && (l = null, t.alternate !== null && (l = t.alternate.memoizedState.cache), t = t.memoizedState.cache, t !== l && (t.refCount++, l != null && ha(l)));
        break;
      case 12:
        if (a & 2048) {
          Bt(
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
            Sl(t, t.return, d);
          }
        } else
          Bt(
            l,
            t,
            e,
            u
          );
        break;
      case 31:
        Bt(
          l,
          t,
          e,
          u
        );
        break;
      case 13:
        Bt(
          l,
          t,
          e,
          u
        );
        break;
      case 23:
        break;
      case 22:
        n = t.stateNode, i = t.alternate, t.memoizedState !== null ? n._visibility & 2 ? Bt(
          l,
          t,
          e,
          u
        ) : Ma(l, t) : n._visibility & 2 ? Bt(
          l,
          t,
          e,
          u
        ) : (n._visibility |= 2, Hu(
          l,
          t,
          e,
          u,
          (t.subtreeFlags & 10256) !== 0 || !1
        )), a & 2048 && wc(i, t);
        break;
      case 24:
        Bt(
          l,
          t,
          e,
          u
        ), a & 2048 && kc(t.alternate, t);
        break;
      default:
        Bt(
          l,
          t,
          e,
          u
        );
    }
  }
  function Hu(l, t, e, u, a) {
    for (a = a && ((t.subtreeFlags & 10256) !== 0 || !1), t = t.child; t !== null; ) {
      var n = l, i = t, f = e, d = u, g = i.flags;
      switch (i.tag) {
        case 0:
        case 11:
        case 15:
          Hu(
            n,
            i,
            f,
            d,
            a
          ), Na(8, i);
          break;
        case 23:
          break;
        case 22:
          var A = i.stateNode;
          i.memoizedState !== null ? A._visibility & 2 ? Hu(
            n,
            i,
            f,
            d,
            a
          ) : Ma(
            n,
            i
          ) : (A._visibility |= 2, Hu(
            n,
            i,
            f,
            d,
            a
          )), a && g & 2048 && wc(
            i.alternate,
            i
          );
          break;
        case 24:
          Hu(
            n,
            i,
            f,
            d,
            a
          ), a && g & 2048 && kc(i.alternate, i);
          break;
        default:
          Hu(
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
  function Ma(l, t) {
    if (t.subtreeFlags & 10256)
      for (t = t.child; t !== null; ) {
        var e = l, u = t, a = u.flags;
        switch (u.tag) {
          case 22:
            Ma(e, u), a & 2048 && wc(
              u.alternate,
              u
            );
            break;
          case 24:
            Ma(e, u), a & 2048 && kc(u.alternate, u);
            break;
          default:
            Ma(e, u);
        }
        t = t.sibling;
      }
  }
  var Da = 8192;
  function Bu(l, t, e) {
    if (l.subtreeFlags & Da)
      for (l = l.child; l !== null; )
        Co(
          l,
          t,
          e
        ), l = l.sibling;
  }
  function Co(l, t, e) {
    switch (l.tag) {
      case 26:
        Bu(
          l,
          t,
          e
        ), l.flags & Da && l.memoizedState !== null && _h(
          e,
          Ht,
          l.memoizedState,
          l.memoizedProps
        );
        break;
      case 5:
        Bu(
          l,
          t,
          e
        );
        break;
      case 3:
      case 4:
        var u = Ht;
        Ht = Pn(l.stateNode.containerInfo), Bu(
          l,
          t,
          e
        ), Ht = u;
        break;
      case 22:
        l.memoizedState === null && (u = l.alternate, u !== null && u.memoizedState !== null ? (u = Da, Da = 16777216, Bu(
          l,
          t,
          e
        ), Da = u) : Bu(
          l,
          t,
          e
        ));
        break;
      default:
        Bu(
          l,
          t,
          e
        );
    }
  }
  function jo(l) {
    var t = l.alternate;
    if (t !== null && (l = t.child, l !== null)) {
      t.child = null;
      do
        t = l.sibling, l.sibling = null, l = t;
      while (l !== null);
    }
  }
  function Ua(l) {
    var t = l.deletions;
    if ((l.flags & 16) !== 0) {
      if (t !== null)
        for (var e = 0; e < t.length; e++) {
          var u = t[e];
          Xl = u, Ho(
            u,
            l
          );
        }
      jo(l);
    }
    if (l.subtreeFlags & 10256)
      for (l = l.child; l !== null; )
        Ro(l), l = l.sibling;
  }
  function Ro(l) {
    switch (l.tag) {
      case 0:
      case 11:
      case 15:
        Ua(l), l.flags & 2048 && Te(9, l, l.return);
        break;
      case 3:
        Ua(l);
        break;
      case 12:
        Ua(l);
        break;
      case 22:
        var t = l.stateNode;
        l.memoizedState !== null && t._visibility & 2 && (l.return === null || l.return.tag !== 13) ? (t._visibility &= -3, Ln(l)) : Ua(l);
        break;
      default:
        Ua(l);
    }
  }
  function Ln(l) {
    var t = l.deletions;
    if ((l.flags & 16) !== 0) {
      if (t !== null)
        for (var e = 0; e < t.length; e++) {
          var u = t[e];
          Xl = u, Ho(
            u,
            l
          );
        }
      jo(l);
    }
    for (l = l.child; l !== null; ) {
      switch (t = l, t.tag) {
        case 0:
        case 11:
        case 15:
          Te(8, t, t.return), Ln(t);
          break;
        case 22:
          e = t.stateNode, e._visibility & 2 && (e._visibility &= -3, Ln(t));
          break;
        default:
          Ln(t);
      }
      l = l.sibling;
    }
  }
  function Ho(l, t) {
    for (; Xl !== null; ) {
      var e = Xl;
      switch (e.tag) {
        case 0:
        case 11:
        case 15:
          Te(8, e, t);
          break;
        case 23:
        case 22:
          if (e.memoizedState !== null && e.memoizedState.cachePool !== null) {
            var u = e.memoizedState.cachePool.pool;
            u != null && u.refCount++;
          }
          break;
        case 24:
          ha(e.memoizedState.cache);
      }
      if (u = e.child, u !== null) u.return = e, Xl = u;
      else
        l: for (e = l; Xl !== null; ) {
          u = Xl;
          var a = u.sibling, n = u.return;
          if (To(u), u === e) {
            Xl = null;
            break l;
          }
          if (a !== null) {
            a.return = n, Xl = a;
            break l;
          }
          Xl = n;
        }
    }
  }
  var Hv = {
    getCacheForType: function(l) {
      var t = wl(jl), e = t.data.get(l);
      return e === void 0 && (e = l(), t.data.set(l, e)), e;
    },
    cacheSignal: function() {
      return wl(jl).controller.signal;
    }
  }, Bv = typeof WeakMap == "function" ? WeakMap : Map, gl = 0, zl = null, nl = null, fl = 0, bl = 0, bt = null, xe = !1, Yu = !1, $c = !1, fe = 0, Ml = 0, Ne = 0, iu = 0, Wc = 0, St = 0, Gu = 0, Ca = null, nt = null, Fc = !1, Qn = 0, Bo = 0, Xn = 1 / 0, Zn = null, Oe = null, Ll = 0, Me = null, Lu = null, se = 0, Ic = 0, Pc = null, Yo = null, ja = 0, lf = null;
  function pt() {
    return (gl & 2) !== 0 && fl !== 0 ? fl & -fl : _.T !== null ? cf() : Pf();
  }
  function Go() {
    if (St === 0)
      if ((fl & 536870912) === 0 || rl) {
        var l = Wa;
        Wa <<= 1, (Wa & 3932160) === 0 && (Wa = 262144), St = l;
      } else St = 536870912;
    return l = mt.current, l !== null && (l.flags |= 32), St;
  }
  function it(l, t, e) {
    (l === zl && (bl === 2 || bl === 9) || l.cancelPendingCommit !== null) && (Qu(l, 0), De(
      l,
      fl,
      St,
      !1
    )), ta(l, e), ((gl & 2) === 0 || l !== zl) && (l === zl && ((gl & 2) === 0 && (iu |= e), Ml === 4 && De(
      l,
      fl,
      St,
      !1
    )), Vt(l));
  }
  function Lo(l, t, e) {
    if ((gl & 6) !== 0) throw Error(s(327));
    var u = !e && (t & 127) === 0 && (t & l.expiredLanes) === 0 || la(l, t), a = u ? Lv(l, t) : ef(l, t, !0), n = u;
    do {
      if (a === 0) {
        Yu && !u && De(l, t, 0, !1);
        break;
      } else {
        if (e = l.current.alternate, n && !Yv(e)) {
          a = ef(l, t, !1), n = !1;
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
              a = Ca;
              var d = f.current.memoizedState.isDehydrated;
              if (d && (Qu(f, i).flags |= 256), i = ef(
                f,
                i,
                !1
              ), i !== 2) {
                if ($c && !d) {
                  f.errorRecoveryDisabledLanes |= n, iu |= n, a = 4;
                  break l;
                }
                n = nt, nt = a, n !== null && (nt === null ? nt = n : nt.push.apply(
                  nt,
                  n
                ));
              }
              a = i;
            }
            if (n = !1, a !== 2) continue;
          }
        }
        if (a === 1) {
          Qu(l, 0), De(l, t, 0, !0);
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
              De(
                u,
                t,
                St,
                !xe
              );
              break l;
            case 2:
              nt = null;
              break;
            case 3:
            case 5:
              break;
            default:
              throw Error(s(329));
          }
          if ((t & 62914560) === t && (a = Qn + 300 - ot(), 10 < a)) {
            if (De(
              u,
              t,
              St,
              !xe
            ), Ia(u, 0, !0) !== 0) break l;
            se = t, u.timeoutHandle = gd(
              Qo.bind(
                null,
                u,
                e,
                nt,
                Zn,
                Fc,
                t,
                St,
                iu,
                Gu,
                xe,
                n,
                "Throttled",
                -0,
                0
              ),
              a
            );
            break l;
          }
          Qo(
            u,
            e,
            nt,
            Zn,
            Fc,
            t,
            St,
            iu,
            Gu,
            xe,
            n,
            null,
            -0,
            0
          );
        }
      }
      break;
    } while (!0);
    Vt(l);
  }
  function Qo(l, t, e, u, a, n, i, f, d, g, A, N, S, E) {
    if (l.timeoutHandle = -1, N = t.subtreeFlags, N & 8192 || (N & 16785408) === 16785408) {
      N = {
        stylesheets: null,
        count: 0,
        imgCount: 0,
        imgBytes: 0,
        suspenseyImages: [],
        waitingForImages: !0,
        waitingForViewTransition: !1,
        unsuspend: kt
      }, Co(
        t,
        n,
        N
      );
      var B = (n & 62914560) === n ? Qn - ot() : (n & 4194048) === n ? Bo - ot() : 0;
      if (B = Eh(
        N,
        B
      ), B !== null) {
        se = n, l.cancelPendingCommit = B(
          $o.bind(
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
            A,
            N,
            null,
            S,
            E
          )
        ), De(l, n, i, !g);
        return;
      }
    }
    $o(
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
            if (!vt(n(), a)) return !1;
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
  function De(l, t, e, u) {
    t &= ~Wc, t &= ~iu, l.suspendedLanes |= t, l.pingedLanes &= ~t, u && (l.warmLanes |= t), u = l.expirationTimes;
    for (var a = t; 0 < a; ) {
      var n = 31 - yt(a), i = 1 << n;
      u[n] = -1, a &= ~i;
    }
    e !== 0 && Wf(l, e, t);
  }
  function Vn() {
    return (gl & 6) === 0 ? (Ra(0), !1) : !0;
  }
  function tf() {
    if (nl !== null) {
      if (bl === 0)
        var l = nl.return;
      else
        l = nl, It = Fe = null, gc(l), Du = null, ga = 0, l = nl;
      for (; l !== null; )
        bo(l.alternate, l), l = l.return;
      nl = null;
    }
  }
  function Qu(l, t) {
    var e = l.timeoutHandle;
    e !== -1 && (l.timeoutHandle = -1, ah(e)), e = l.cancelPendingCommit, e !== null && (l.cancelPendingCommit = null, e()), se = 0, tf(), zl = l, nl = e = Wt(l.current, null), fl = t, bl = 0, bt = null, xe = !1, Yu = la(l, t), $c = !1, Gu = St = Wc = iu = Ne = Ml = 0, nt = Ca = null, Fc = !1, (t & 8) !== 0 && (t |= t & 32);
    var u = l.entangledLanes;
    if (u !== 0)
      for (l = l.entanglements, u &= t; 0 < u; ) {
        var a = 31 - yt(u), n = 1 << a;
        t |= l[a], u &= ~n;
      }
    return fe = t, on(), e;
  }
  function Xo(l, t) {
    tl = null, _.H = qa, t === Mu || t === Sn ? (t = ur(), bl = 3) : t === nc ? (t = ur(), bl = 4) : bl = t === Cc ? 8 : t !== null && typeof t == "object" && typeof t.then == "function" ? 6 : 1, bt = t, nl === null && (Ml = 1, Cn(
      l,
      Tt(t, l.current)
    ));
  }
  function Zo() {
    var l = mt.current;
    return l === null ? !0 : (fl & 4194048) === fl ? Mt === null : (fl & 62914560) === fl || (fl & 536870912) !== 0 ? l === Mt : !1;
  }
  function Vo() {
    var l = _.H;
    return _.H = qa, l === null ? qa : l;
  }
  function Ko() {
    var l = _.A;
    return _.A = Hv, l;
  }
  function Kn() {
    Ml = 4, xe || (fl & 4194048) !== fl && mt.current !== null || (Yu = !0), (Ne & 134217727) === 0 && (iu & 134217727) === 0 || zl === null || De(
      zl,
      fl,
      St,
      !1
    );
  }
  function ef(l, t, e) {
    var u = gl;
    gl |= 2;
    var a = Vo(), n = Ko();
    (zl !== l || fl !== t) && (Zn = null, Qu(l, t)), t = !1;
    var i = Ml;
    l: do
      try {
        if (bl !== 0 && nl !== null) {
          var f = nl, d = bt;
          switch (bl) {
            case 8:
              tf(), i = 6;
              break l;
            case 3:
            case 2:
            case 9:
            case 6:
              mt.current === null && (t = !0);
              var g = bl;
              if (bl = 0, bt = null, Xu(l, f, d, g), e && Yu) {
                i = 0;
                break l;
              }
              break;
            default:
              g = bl, bl = 0, bt = null, Xu(l, f, d, g);
          }
        }
        Gv(), i = Ml;
        break;
      } catch (A) {
        Xo(l, A);
      }
    while (!0);
    return t && l.shellSuspendCounter++, It = Fe = null, gl = u, _.H = a, _.A = n, nl === null && (zl = null, fl = 0, on()), i;
  }
  function Gv() {
    for (; nl !== null; ) Jo(nl);
  }
  function Lv(l, t) {
    var e = gl;
    gl |= 2;
    var u = Vo(), a = Ko();
    zl !== l || fl !== t ? (Zn = null, Xn = ot() + 500, Qu(l, t)) : Yu = la(
      l,
      t
    );
    l: do
      try {
        if (bl !== 0 && nl !== null) {
          t = nl;
          var n = bt;
          t: switch (bl) {
            case 1:
              bl = 0, bt = null, Xu(l, t, n, 1);
              break;
            case 2:
            case 9:
              if (tr(n)) {
                bl = 0, bt = null, wo(t);
                break;
              }
              t = function() {
                bl !== 2 && bl !== 9 || zl !== l || (bl = 7), Vt(l);
              }, n.then(t, t);
              break l;
            case 3:
              bl = 7;
              break l;
            case 4:
              bl = 5;
              break l;
            case 7:
              tr(n) ? (bl = 0, bt = null, wo(t)) : (bl = 0, bt = null, Xu(l, t, n, 7));
              break;
            case 5:
              var i = null;
              switch (nl.tag) {
                case 26:
                  i = nl.memoizedState;
                case 5:
                case 27:
                  var f = nl;
                  if (i ? Ud(i) : f.stateNode.complete) {
                    bl = 0, bt = null;
                    var d = f.sibling;
                    if (d !== null) nl = d;
                    else {
                      var g = f.return;
                      g !== null ? (nl = g, Jn(g)) : nl = null;
                    }
                    break t;
                  }
              }
              bl = 0, bt = null, Xu(l, t, n, 5);
              break;
            case 6:
              bl = 0, bt = null, Xu(l, t, n, 6);
              break;
            case 8:
              tf(), Ml = 6;
              break l;
            default:
              throw Error(s(462));
          }
        }
        Qv();
        break;
      } catch (A) {
        Xo(l, A);
      }
    while (!0);
    return It = Fe = null, _.H = u, _.A = a, gl = e, nl !== null ? 0 : (zl = null, fl = 0, on(), Ml);
  }
  function Qv() {
    for (; nl !== null && !ry(); )
      Jo(nl);
  }
  function Jo(l) {
    var t = mo(l.alternate, l, fe);
    l.memoizedProps = l.pendingProps, t === null ? Jn(l) : nl = t;
  }
  function wo(l) {
    var t = l, e = t.alternate;
    switch (t.tag) {
      case 15:
      case 0:
        t = so(
          e,
          t,
          t.pendingProps,
          t.type,
          void 0,
          fl
        );
        break;
      case 11:
        t = so(
          e,
          t,
          t.pendingProps,
          t.type.render,
          t.ref,
          fl
        );
        break;
      case 5:
        gc(t);
      default:
        bo(e, t), t = nl = Vs(t, fe), t = mo(e, t, fe);
    }
    l.memoizedProps = l.pendingProps, t === null ? Jn(l) : nl = t;
  }
  function Xu(l, t, e, u) {
    It = Fe = null, gc(t), Du = null, ga = 0;
    var a = t.return;
    try {
      if (Ov(
        l,
        a,
        t,
        e,
        fl
      )) {
        Ml = 1, Cn(
          l,
          Tt(e, l.current)
        ), nl = null;
        return;
      }
    } catch (n) {
      if (a !== null) throw nl = a, n;
      Ml = 1, Cn(
        l,
        Tt(e, l.current)
      ), nl = null;
      return;
    }
    t.flags & 32768 ? (rl || u === 1 ? l = !0 : Yu || (fl & 536870912) !== 0 ? l = !1 : (xe = l = !0, (u === 2 || u === 9 || u === 3 || u === 6) && (u = mt.current, u !== null && u.tag === 13 && (u.flags |= 16384))), ko(t, l)) : Jn(t);
  }
  function Jn(l) {
    var t = l;
    do {
      if ((t.flags & 32768) !== 0) {
        ko(
          t,
          xe
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
        nl = e;
        return;
      }
      if (t = t.sibling, t !== null) {
        nl = t;
        return;
      }
      nl = t = l;
    } while (t !== null);
    Ml === 0 && (Ml = 5);
  }
  function ko(l, t) {
    do {
      var e = Cv(l.alternate, l);
      if (e !== null) {
        e.flags &= 32767, nl = e;
        return;
      }
      if (e = l.return, e !== null && (e.flags |= 32768, e.subtreeFlags = 0, e.deletions = null), !t && (l = l.sibling, l !== null)) {
        nl = l;
        return;
      }
      nl = l = e;
    } while (l !== null);
    Ml = 6, nl = null;
  }
  function $o(l, t, e, u, a, n, i, f, d) {
    l.cancelPendingCommit = null;
    do
      wn();
    while (Ll !== 0);
    if ((gl & 6) !== 0) throw Error(s(327));
    if (t !== null) {
      if (t === l.current) throw Error(s(177));
      if (n = t.lanes | t.childLanes, n |= Vi, py(
        l,
        e,
        n,
        i,
        f,
        d
      ), l === zl && (nl = zl = null, fl = 0), Lu = t, Me = l, se = e, Ic = n, Pc = a, Yo = u, (t.subtreeFlags & 10256) !== 0 || (t.flags & 10256) !== 0 ? (l.callbackNode = null, l.callbackPriority = 0, Kv(ka, function() {
        return ld(), null;
      })) : (l.callbackNode = null, l.callbackPriority = 0), u = (t.flags & 13878) !== 0, (t.subtreeFlags & 13878) !== 0 || u) {
        u = _.T, _.T = null, a = H.p, H.p = 2, i = gl, gl |= 4;
        try {
          jv(l, t, e);
        } finally {
          gl = i, H.p = a, _.T = u;
        }
      }
      Ll = 1, Wo(), Fo(), Io();
    }
  }
  function Wo() {
    if (Ll === 1) {
      Ll = 0;
      var l = Me, t = Lu, e = (t.flags & 13878) !== 0;
      if ((t.subtreeFlags & 13878) !== 0 || e) {
        e = _.T, _.T = null;
        var u = H.p;
        H.p = 2;
        var a = gl;
        gl |= 4;
        try {
          Mo(t, l);
          var n = hf, i = Rs(l.containerInfo), f = n.focusedElem, d = n.selectionRange;
          if (i !== f && f && f.ownerDocument && js(
            f.ownerDocument.documentElement,
            f
          )) {
            if (d !== null && Gi(f)) {
              var g = d.start, A = d.end;
              if (A === void 0 && (A = g), "selectionStart" in f)
                f.selectionStart = g, f.selectionEnd = Math.min(
                  A,
                  f.value.length
                );
              else {
                var N = f.ownerDocument || document, S = N && N.defaultView || window;
                if (S.getSelection) {
                  var E = S.getSelection(), B = f.textContent.length, w = Math.min(d.start, B), El = d.end === void 0 ? w : Math.min(d.end, B);
                  !E.extend && w > El && (i = El, El = w, w = i);
                  var h = Cs(
                    f,
                    w
                  ), y = Cs(
                    f,
                    El
                  );
                  if (h && y && (E.rangeCount !== 1 || E.anchorNode !== h.node || E.anchorOffset !== h.offset || E.focusNode !== y.node || E.focusOffset !== y.offset)) {
                    var m = N.createRange();
                    m.setStart(h.node, h.offset), E.removeAllRanges(), w > El ? (E.addRange(m), E.extend(y.node, y.offset)) : (m.setEnd(y.node, y.offset), E.addRange(m));
                  }
                }
              }
            }
            for (N = [], E = f; E = E.parentNode; )
              E.nodeType === 1 && N.push({
                element: E,
                left: E.scrollLeft,
                top: E.scrollTop
              });
            for (typeof f.focus == "function" && f.focus(), f = 0; f < N.length; f++) {
              var q = N[f];
              q.element.scrollLeft = q.left, q.element.scrollTop = q.top;
            }
          }
          ni = !!vf, hf = vf = null;
        } finally {
          gl = a, H.p = u, _.T = e;
        }
      }
      l.current = t, Ll = 2;
    }
  }
  function Fo() {
    if (Ll === 2) {
      Ll = 0;
      var l = Me, t = Lu, e = (t.flags & 8772) !== 0;
      if ((t.subtreeFlags & 8772) !== 0 || e) {
        e = _.T, _.T = null;
        var u = H.p;
        H.p = 2;
        var a = gl;
        gl |= 4;
        try {
          qo(l, t.alternate, t);
        } finally {
          gl = a, H.p = u, _.T = e;
        }
      }
      Ll = 3;
    }
  }
  function Io() {
    if (Ll === 4 || Ll === 3) {
      Ll = 0, oy();
      var l = Me, t = Lu, e = se, u = Yo;
      (t.subtreeFlags & 10256) !== 0 || (t.flags & 10256) !== 0 ? Ll = 5 : (Ll = 0, Lu = Me = null, Po(l, l.pendingLanes));
      var a = l.pendingLanes;
      if (a === 0 && (Oe = null), pi(e), t = t.stateNode, dt && typeof dt.onCommitFiberRoot == "function")
        try {
          dt.onCommitFiberRoot(
            Pu,
            t,
            void 0,
            (t.current.flags & 128) === 128
          );
        } catch {
        }
      if (u !== null) {
        t = _.T, a = H.p, H.p = 2, _.T = null;
        try {
          for (var n = l.onRecoverableError, i = 0; i < u.length; i++) {
            var f = u[i];
            n(f.value, {
              componentStack: f.stack
            });
          }
        } finally {
          _.T = t, H.p = a;
        }
      }
      (se & 3) !== 0 && wn(), Vt(l), a = l.pendingLanes, (e & 261930) !== 0 && (a & 42) !== 0 ? l === lf ? ja++ : (ja = 0, lf = l) : ja = 0, Ra(0);
    }
  }
  function Po(l, t) {
    (l.pooledCacheLanes &= t) === 0 && (t = l.pooledCache, t != null && (l.pooledCache = null, ha(t)));
  }
  function wn() {
    return Wo(), Fo(), Io(), ld();
  }
  function ld() {
    if (Ll !== 5) return !1;
    var l = Me, t = Ic;
    Ic = 0;
    var e = pi(se), u = _.T, a = H.p;
    try {
      H.p = 32 > e ? 32 : e, _.T = null, e = Pc, Pc = null;
      var n = Me, i = se;
      if (Ll = 0, Lu = Me = null, se = 0, (gl & 6) !== 0) throw Error(s(331));
      var f = gl;
      if (gl |= 4, Ro(n.current), Uo(
        n,
        n.current,
        i,
        e
      ), gl = f, Ra(0, !1), dt && typeof dt.onPostCommitFiberRoot == "function")
        try {
          dt.onPostCommitFiberRoot(Pu, n);
        } catch {
        }
      return !0;
    } finally {
      H.p = a, _.T = u, Po(l, t);
    }
  }
  function td(l, t, e) {
    t = Tt(e, t), t = Uc(l.stateNode, t, 2), l = ze(l, t, 2), l !== null && (ta(l, 2), Vt(l));
  }
  function Sl(l, t, e) {
    if (l.tag === 3)
      td(l, l, e);
    else
      for (; t !== null; ) {
        if (t.tag === 3) {
          td(
            t,
            l,
            e
          );
          break;
        } else if (t.tag === 1) {
          var u = t.stateNode;
          if (typeof t.type.getDerivedStateFromError == "function" || typeof u.componentDidCatch == "function" && (Oe === null || !Oe.has(u))) {
            l = Tt(e, l), e = to(2), u = ze(t, e, 2), u !== null && (eo(
              e,
              u,
              t,
              l
            ), ta(u, 2), Vt(u));
            break;
          }
        }
        t = t.return;
      }
  }
  function uf(l, t, e) {
    var u = l.pingCache;
    if (u === null) {
      u = l.pingCache = new Bv();
      var a = /* @__PURE__ */ new Set();
      u.set(t, a);
    } else
      a = u.get(t), a === void 0 && (a = /* @__PURE__ */ new Set(), u.set(t, a));
    a.has(e) || ($c = !0, a.add(e), l = Xv.bind(null, l, t, e), t.then(l, l));
  }
  function Xv(l, t, e) {
    var u = l.pingCache;
    u !== null && u.delete(t), l.pingedLanes |= l.suspendedLanes & e, l.warmLanes &= ~e, zl === l && (fl & e) === e && (Ml === 4 || Ml === 3 && (fl & 62914560) === fl && 300 > ot() - Qn ? (gl & 2) === 0 && Qu(l, 0) : Wc |= e, Gu === fl && (Gu = 0)), Vt(l);
  }
  function ed(l, t) {
    t === 0 && (t = $f()), l = ke(l, t), l !== null && (ta(l, t), Vt(l));
  }
  function Zv(l) {
    var t = l.memoizedState, e = 0;
    t !== null && (e = t.retryLane), ed(l, e);
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
    u !== null && u.delete(t), ed(l, e);
  }
  function Kv(l, t) {
    return mi(l, t);
  }
  var kn = null, Zu = null, af = !1, $n = !1, nf = !1, Ue = 0;
  function Vt(l) {
    l !== Zu && l.next === null && (Zu === null ? kn = Zu = l : Zu = Zu.next = l), $n = !0, af || (af = !0, wv());
  }
  function Ra(l, t) {
    if (!nf && $n) {
      nf = !0;
      do
        for (var e = !1, u = kn; u !== null; ) {
          if (l !== 0) {
            var a = u.pendingLanes;
            if (a === 0) var n = 0;
            else {
              var i = u.suspendedLanes, f = u.pingedLanes;
              n = (1 << 31 - yt(42 | l) + 1) - 1, n &= a & ~(i & ~f), n = n & 201326741 ? n & 201326741 | 1 : n ? n | 2 : 0;
            }
            n !== 0 && (e = !0, id(u, n));
          } else
            n = fl, n = Ia(
              u,
              u === zl ? n : 0,
              u.cancelPendingCommit !== null || u.timeoutHandle !== -1
            ), (n & 3) === 0 || la(u, n) || (e = !0, id(u, n));
          u = u.next;
        }
      while (e);
      nf = !1;
    }
  }
  function Jv() {
    ud();
  }
  function ud() {
    $n = af = !1;
    var l = 0;
    Ue !== 0 && uh() && (l = Ue);
    for (var t = ot(), e = null, u = kn; u !== null; ) {
      var a = u.next, n = ad(u, t);
      n === 0 ? (u.next = null, e === null ? kn = a : e.next = a, a === null && (Zu = e)) : (e = u, (l !== 0 || (n & 3) !== 0) && ($n = !0)), u = a;
    }
    Ll !== 0 && Ll !== 5 || Ra(l), Ue !== 0 && (Ue = 0);
  }
  function ad(l, t) {
    for (var e = l.suspendedLanes, u = l.pingedLanes, a = l.expirationTimes, n = l.pendingLanes & -62914561; 0 < n; ) {
      var i = 31 - yt(n), f = 1 << i, d = a[i];
      d === -1 ? ((f & e) === 0 || (f & u) !== 0) && (a[i] = Sy(f, t)) : d <= t && (l.expiredLanes |= f), n &= ~f;
    }
    if (t = zl, e = fl, e = Ia(
      l,
      l === t ? e : 0,
      l.cancelPendingCommit !== null || l.timeoutHandle !== -1
    ), u = l.callbackNode, e === 0 || l === t && (bl === 2 || bl === 9) || l.cancelPendingCommit !== null)
      return u !== null && u !== null && gi(u), l.callbackNode = null, l.callbackPriority = 0;
    if ((e & 3) === 0 || la(l, e)) {
      if (t = e & -e, t === l.callbackPriority) return t;
      switch (u !== null && gi(u), pi(e)) {
        case 2:
        case 8:
          e = wf;
          break;
        case 32:
          e = ka;
          break;
        case 268435456:
          e = kf;
          break;
        default:
          e = ka;
      }
      return u = nd.bind(null, l), e = mi(e, u), l.callbackPriority = t, l.callbackNode = e, t;
    }
    return u !== null && u !== null && gi(u), l.callbackPriority = 2, l.callbackNode = null, 2;
  }
  function nd(l, t) {
    if (Ll !== 0 && Ll !== 5)
      return l.callbackNode = null, l.callbackPriority = 0, null;
    var e = l.callbackNode;
    if (wn() && l.callbackNode !== e)
      return null;
    var u = fl;
    return u = Ia(
      l,
      l === zl ? u : 0,
      l.cancelPendingCommit !== null || l.timeoutHandle !== -1
    ), u === 0 ? null : (Lo(l, u, t), ad(l, ot()), l.callbackNode != null && l.callbackNode === e ? nd.bind(null, l) : null);
  }
  function id(l, t) {
    if (wn()) return null;
    Lo(l, t, !0);
  }
  function wv() {
    nh(function() {
      (gl & 6) !== 0 ? mi(
        Jf,
        Jv
      ) : ud();
    });
  }
  function cf() {
    if (Ue === 0) {
      var l = Nu;
      l === 0 && (l = $a, $a <<= 1, ($a & 261888) === 0 && ($a = 256)), Ue = l;
    }
    return Ue;
  }
  function cd(l) {
    return l == null || typeof l == "symbol" || typeof l == "boolean" ? null : typeof l == "function" ? l : en("" + l);
  }
  function fd(l, t) {
    var e = t.ownerDocument.createElement("input");
    return e.name = t.name, e.value = t.value, l.id && e.setAttribute("form", l.id), t.parentNode.insertBefore(e, t), l = new FormData(l), e.parentNode.removeChild(e), l;
  }
  function kv(l, t, e, u, a) {
    if (t === "submit" && e && e.stateNode === a) {
      var n = cd(
        (a[lt] || null).action
      ), i = u.submitter;
      i && (t = (t = i[lt] || null) ? cd(t.formAction) : i.getAttribute("formAction"), t !== null && (n = t, i = null));
      var f = new cn(
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
                if (Ue !== 0) {
                  var d = i ? fd(a, i) : new FormData(a);
                  Tc(
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
                typeof n == "function" && (f.preventDefault(), d = i ? fd(a, i) : new FormData(a), Tc(
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
  for (var ff = 0; ff < Zi.length; ff++) {
    var sf = Zi[ff], $v = sf.toLowerCase(), Wv = sf[0].toUpperCase() + sf.slice(1);
    Rt(
      $v,
      "on" + Wv
    );
  }
  Rt(Ys, "onAnimationEnd"), Rt(Gs, "onAnimationIteration"), Rt(Ls, "onAnimationStart"), Rt("dblclick", "onDoubleClick"), Rt("focusin", "onFocus"), Rt("focusout", "onBlur"), Rt(dv, "onTransitionRun"), Rt(yv, "onTransitionStart"), Rt(vv, "onTransitionCancel"), Rt(Qs, "onTransitionEnd"), vu("onMouseEnter", ["mouseout", "mouseover"]), vu("onMouseLeave", ["mouseout", "mouseover"]), vu("onPointerEnter", ["pointerout", "pointerover"]), vu("onPointerLeave", ["pointerout", "pointerover"]), Ve(
    "onChange",
    "change click focusin focusout input keydown keyup selectionchange".split(" ")
  ), Ve(
    "onSelect",
    "focusout contextmenu dragend focusin keydown keyup mousedown mouseup selectionchange".split(
      " "
    )
  ), Ve("onBeforeInput", [
    "compositionend",
    "keypress",
    "textInput",
    "paste"
  ]), Ve(
    "onCompositionEnd",
    "compositionend focusout keydown keypress keyup mousedown".split(" ")
  ), Ve(
    "onCompositionStart",
    "compositionstart focusout keydown keypress keyup mousedown".split(" ")
  ), Ve(
    "onCompositionUpdate",
    "compositionupdate focusout keydown keypress keyup mousedown".split(" ")
  );
  var Ha = "abort canplay canplaythrough durationchange emptied encrypted ended error loadeddata loadedmetadata loadstart pause play playing progress ratechange resize seeked seeking stalled suspend timeupdate volumechange waiting".split(
    " "
  ), Fv = new Set(
    "beforetoggle cancel close invalid load scroll scrollend toggle".split(" ").concat(Ha)
  );
  function sd(l, t) {
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
            } catch (A) {
              rn(A);
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
            } catch (A) {
              rn(A);
            }
            a.currentTarget = null, n = d;
          }
      }
    }
  }
  function il(l, t) {
    var e = t[_i];
    e === void 0 && (e = t[_i] = /* @__PURE__ */ new Set());
    var u = l + "__bubble";
    e.has(u) || (rd(t, l, 2, !1), e.add(u));
  }
  function rf(l, t, e) {
    var u = 0;
    t && (u |= 4), rd(
      e,
      l,
      u,
      t
    );
  }
  var Wn = "_reactListening" + Math.random().toString(36).slice(2);
  function of(l) {
    if (!l[Wn]) {
      l[Wn] = !0, es.forEach(function(e) {
        e !== "selectionchange" && (Fv.has(e) || rf(e, !1, l), rf(e, !0, l));
      });
      var t = l.nodeType === 9 ? l : l.ownerDocument;
      t === null || t[Wn] || (t[Wn] = !0, rf("selectionchange", !1, t));
    }
  }
  function rd(l, t, e, u) {
    switch (Gd(t)) {
      case 2:
        var a = qh;
        break;
      case 8:
        a = Th;
        break;
      default:
        a = Tf;
    }
    e = a.bind(
      null,
      t,
      e,
      l
    ), a = void 0, !Mi || t !== "touchstart" && t !== "touchmove" && t !== "wheel" || (a = !0), u ? a !== void 0 ? l.addEventListener(t, e, {
      capture: !0,
      passive: a
    }) : l.addEventListener(t, e, !0) : a !== void 0 ? l.addEventListener(t, e, {
      passive: a
    }) : l.addEventListener(t, e, !1);
  }
  function df(l, t, e, u, a) {
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
            if (i = ou(f), i === null) return;
            if (d = i.tag, d === 5 || d === 6 || d === 26 || d === 27) {
              u = n = i;
              continue l;
            }
            f = f.parentNode;
          }
        }
        u = u.return;
      }
    vs(function() {
      var g = n, A = Ni(e), N = [];
      l: {
        var S = Xs.get(l);
        if (S !== void 0) {
          var E = cn, B = l;
          switch (l) {
            case "keypress":
              if (an(e) === 0) break l;
            case "keydown":
            case "keyup":
              E = Vy;
              break;
            case "focusin":
              B = "focus", E = ji;
              break;
            case "focusout":
              B = "blur", E = ji;
              break;
            case "beforeblur":
            case "afterblur":
              E = ji;
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
              E = gs;
              break;
            case "drag":
            case "dragend":
            case "dragenter":
            case "dragexit":
            case "dragleave":
            case "dragover":
            case "dragstart":
            case "drop":
              E = Uy;
              break;
            case "touchcancel":
            case "touchend":
            case "touchmove":
            case "touchstart":
              E = wy;
              break;
            case Ys:
            case Gs:
            case Ls:
              E = Ry;
              break;
            case Qs:
              E = $y;
              break;
            case "scroll":
            case "scrollend":
              E = My;
              break;
            case "wheel":
              E = Fy;
              break;
            case "copy":
            case "cut":
            case "paste":
              E = By;
              break;
            case "gotpointercapture":
            case "lostpointercapture":
            case "pointercancel":
            case "pointerdown":
            case "pointermove":
            case "pointerout":
            case "pointerover":
            case "pointerup":
              E = Ss;
              break;
            case "toggle":
            case "beforetoggle":
              E = Py;
          }
          var w = (t & 4) !== 0, El = !w && (l === "scroll" || l === "scrollend"), h = w ? S !== null ? S + "Capture" : null : S;
          w = [];
          for (var y = g, m; y !== null; ) {
            var q = y;
            if (m = q.stateNode, q = q.tag, q !== 5 && q !== 26 && q !== 27 || m === null || h === null || (q = aa(y, h), q != null && w.push(
              Ba(y, q, m)
            )), El) break;
            y = y.return;
          }
          0 < w.length && (S = new E(
            S,
            B,
            null,
            e,
            A
          ), N.push({ event: S, listeners: w }));
        }
      }
      if ((t & 7) === 0) {
        l: {
          if (S = l === "mouseover" || l === "pointerover", E = l === "mouseout" || l === "pointerout", S && e !== xi && (B = e.relatedTarget || e.fromElement) && (ou(B) || B[ru]))
            break l;
          if ((E || S) && (S = A.window === A ? A : (S = A.ownerDocument) ? S.defaultView || S.parentWindow : window, E ? (B = e.relatedTarget || e.toElement, E = g, B = B ? ou(B) : null, B !== null && (El = z(B), w = B.tag, B !== El || w !== 5 && w !== 27 && w !== 6) && (B = null)) : (E = null, B = g), E !== B)) {
            if (w = gs, q = "onMouseLeave", h = "onMouseEnter", y = "mouse", (l === "pointerout" || l === "pointerover") && (w = Ss, q = "onPointerLeave", h = "onPointerEnter", y = "pointer"), El = E == null ? S : ua(E), m = B == null ? S : ua(B), S = new w(
              q,
              y + "leave",
              E,
              e,
              A
            ), S.target = El, S.relatedTarget = m, q = null, ou(A) === g && (w = new w(
              h,
              y + "enter",
              B,
              e,
              A
            ), w.target = m, w.relatedTarget = El, q = w), El = q, E && B)
              t: {
                for (w = Iv, h = E, y = B, m = 0, q = h; q; q = w(q))
                  m++;
                q = 0;
                for (var V = y; V; V = w(V))
                  q++;
                for (; 0 < m - q; )
                  h = w(h), m--;
                for (; 0 < q - m; )
                  y = w(y), q--;
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
            E !== null && od(
              N,
              S,
              E,
              w,
              !1
            ), B !== null && El !== null && od(
              N,
              El,
              B,
              w,
              !0
            );
          }
        }
        l: {
          if (S = g ? ua(g) : window, E = S.nodeName && S.nodeName.toLowerCase(), E === "select" || E === "input" && S.type === "file")
            var hl = xs;
          else if (qs(S))
            if (Ns)
              hl = sv;
            else {
              hl = cv;
              var Q = iv;
            }
          else
            E = S.nodeName, !E || E.toLowerCase() !== "input" || S.type !== "checkbox" && S.type !== "radio" ? g && Ti(g.elementType) && (hl = xs) : hl = fv;
          if (hl && (hl = hl(l, g))) {
            Ts(
              N,
              hl,
              e,
              A
            );
            break l;
          }
          Q && Q(l, S, g), l === "focusout" && g && S.type === "number" && g.memoizedProps.value != null && qi(S, "number", S.value);
        }
        switch (Q = g ? ua(g) : window, l) {
          case "focusin":
            (qs(Q) || Q.contentEditable === "true") && (pu = Q, Li = g, da = null);
            break;
          case "focusout":
            da = Li = pu = null;
            break;
          case "mousedown":
            Qi = !0;
            break;
          case "contextmenu":
          case "mouseup":
          case "dragend":
            Qi = !1, Hs(N, e, A);
            break;
          case "selectionchange":
            if (ov) break;
          case "keydown":
          case "keyup":
            Hs(N, e, A);
        }
        var el;
        if (Hi)
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
          Su ? zs(l, e) && (sl = "onCompositionEnd") : l === "keydown" && e.keyCode === 229 && (sl = "onCompositionStart");
        sl && (ps && e.locale !== "ko" && (Su || sl !== "onCompositionStart" ? sl === "onCompositionEnd" && Su && (el = hs()) : (me = A, Di = "value" in me ? me.value : me.textContent, Su = !0)), Q = Fn(g, sl), 0 < Q.length && (sl = new bs(
          sl,
          l,
          null,
          e,
          A
        ), N.push({ event: sl, listeners: Q }), el ? sl.data = el : (el = As(e), el !== null && (sl.data = el)))), (el = tv ? ev(l, e) : uv(l, e)) && (sl = Fn(g, "onBeforeInput"), 0 < sl.length && (Q = new bs(
          "onBeforeInput",
          "beforeinput",
          null,
          e,
          A
        ), N.push({
          event: Q,
          listeners: sl
        }), Q.data = el)), kv(
          N,
          l,
          g,
          e,
          A
        );
      }
      sd(N, t);
    });
  }
  function Ba(l, t, e) {
    return {
      instance: l,
      listener: t,
      currentTarget: e
    };
  }
  function Fn(l, t) {
    for (var e = t + "Capture", u = []; l !== null; ) {
      var a = l, n = a.stateNode;
      if (a = a.tag, a !== 5 && a !== 26 && a !== 27 || n === null || (a = aa(l, e), a != null && u.unshift(
        Ba(l, a, n)
      ), a = aa(l, t), a != null && u.push(
        Ba(l, a, n)
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
  function od(l, t, e, u, a) {
    for (var n = t._reactName, i = []; e !== null && e !== u; ) {
      var f = e, d = f.alternate, g = f.stateNode;
      if (f = f.tag, d !== null && d === u) break;
      f !== 5 && f !== 26 && f !== 27 || g === null || (d = g, a ? (g = aa(e, n), g != null && i.unshift(
        Ba(e, g, d)
      )) : a || (g = aa(e, n), g != null && i.push(
        Ba(e, g, d)
      ))), e = e.return;
    }
    i.length !== 0 && l.push({ event: t, listeners: i });
  }
  var Pv = /\r\n?/g, lh = /\u0000|\uFFFD/g;
  function dd(l) {
    return (typeof l == "string" ? l : "" + l).replace(Pv, `
`).replace(lh, "");
  }
  function yd(l, t) {
    return t = dd(t), dd(l) === t;
  }
  function _l(l, t, e, u, a, n) {
    switch (e) {
      case "children":
        typeof u == "string" ? t === "body" || t === "textarea" && u === "" || mu(l, u) : (typeof u == "number" || typeof u == "bigint") && t !== "body" && mu(l, "" + u);
        break;
      case "className":
        ln(l, "class", u);
        break;
      case "tabIndex":
        ln(l, "tabindex", u);
        break;
      case "dir":
      case "role":
      case "viewBox":
      case "width":
      case "height":
        ln(l, e, u);
        break;
      case "style":
        ds(l, u, n);
        break;
      case "data":
        if (t !== "object") {
          ln(l, "data", u);
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
        u = en("" + u), l.setAttribute(e, u);
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
          typeof n == "function" && (e === "formAction" ? (t !== "input" && _l(l, t, "name", a.name, a, null), _l(
            l,
            t,
            "formEncType",
            a.formEncType,
            a,
            null
          ), _l(
            l,
            t,
            "formMethod",
            a.formMethod,
            a,
            null
          ), _l(
            l,
            t,
            "formTarget",
            a.formTarget,
            a,
            null
          )) : (_l(l, t, "encType", a.encType, a, null), _l(l, t, "method", a.method, a, null), _l(l, t, "target", a.target, a, null)));
        if (u == null || typeof u == "symbol" || typeof u == "boolean") {
          l.removeAttribute(e);
          break;
        }
        u = en("" + u), l.setAttribute(e, u);
        break;
      case "onClick":
        u != null && (l.onclick = kt);
        break;
      case "onScroll":
        u != null && il("scroll", l);
        break;
      case "onScrollEnd":
        u != null && il("scrollend", l);
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
        e = en("" + u), l.setAttributeNS(
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
        il("beforetoggle", l), il("toggle", l), Pa(l, "popover", u);
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
        Pa(l, "is", u);
        break;
      case "innerText":
      case "textContent":
        break;
      default:
        (!(2 < e.length) || e[0] !== "o" && e[0] !== "O" || e[1] !== "n" && e[1] !== "N") && (e = Ny.get(e) || e, Pa(l, e, u));
    }
  }
  function yf(l, t, e, u, a, n) {
    switch (e) {
      case "style":
        ds(l, u, n);
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
        typeof u == "string" ? mu(l, u) : (typeof u == "number" || typeof u == "bigint") && mu(l, "" + u);
        break;
      case "onScroll":
        u != null && il("scroll", l);
        break;
      case "onScrollEnd":
        u != null && il("scrollend", l);
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
        if (!us.hasOwnProperty(e))
          l: {
            if (e[0] === "o" && e[1] === "n" && (a = e.endsWith("Capture"), t = e.slice(2, a ? e.length - 7 : void 0), n = l[lt] || null, n = n != null ? n[e] : null, typeof n == "function" && l.removeEventListener(t, n, a), typeof u == "function")) {
              typeof n != "function" && n !== null && (e in l ? l[e] = null : l.hasAttribute(e) && l.removeAttribute(e)), l.addEventListener(t, u, a);
              break l;
            }
            e in l ? l[e] = u : u === !0 ? l.setAttribute(e, "") : Pa(l, e, u);
          }
    }
  }
  function $l(l, t, e) {
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
        il("error", l), il("load", l);
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
                  _l(l, t, n, i, e, null);
              }
          }
        a && _l(l, t, "srcSet", e.srcSet, e, null), u && _l(l, t, "src", e.src, e, null);
        return;
      case "input":
        il("invalid", l);
        var f = n = i = a = null, d = null, g = null;
        for (u in e)
          if (e.hasOwnProperty(u)) {
            var A = e[u];
            if (A != null)
              switch (u) {
                case "name":
                  a = A;
                  break;
                case "type":
                  i = A;
                  break;
                case "checked":
                  d = A;
                  break;
                case "defaultChecked":
                  g = A;
                  break;
                case "value":
                  n = A;
                  break;
                case "defaultValue":
                  f = A;
                  break;
                case "children":
                case "dangerouslySetInnerHTML":
                  if (A != null)
                    throw Error(s(137, t));
                  break;
                default:
                  _l(l, t, u, A, e, null);
              }
          }
        fs(
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
        il("invalid", l), u = i = n = null;
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
                _l(l, t, a, f, e, null);
            }
        t = n, e = i, l.multiple = !!u, t != null ? hu(l, !!u, t, !1) : e != null && hu(l, !!u, e, !0);
        return;
      case "textarea":
        il("invalid", l), n = a = u = null;
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
                _l(l, t, i, f, e, null);
            }
        rs(l, u, a, n);
        return;
      case "option":
        for (d in e)
          if (e.hasOwnProperty(d) && (u = e[d], u != null))
            switch (d) {
              case "selected":
                l.selected = u && typeof u != "function" && typeof u != "symbol";
                break;
              default:
                _l(l, t, d, u, e, null);
            }
        return;
      case "dialog":
        il("beforetoggle", l), il("toggle", l), il("cancel", l), il("close", l);
        break;
      case "iframe":
      case "object":
        il("load", l);
        break;
      case "video":
      case "audio":
        for (u = 0; u < Ha.length; u++)
          il(Ha[u], l);
        break;
      case "image":
        il("error", l), il("load", l);
        break;
      case "details":
        il("toggle", l);
        break;
      case "embed":
      case "source":
      case "link":
        il("error", l), il("load", l);
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
                _l(l, t, g, u, e, null);
            }
        return;
      default:
        if (Ti(t)) {
          for (A in e)
            e.hasOwnProperty(A) && (u = e[A], u !== void 0 && yf(
              l,
              t,
              A,
              u,
              e,
              void 0
            ));
          return;
        }
    }
    for (f in e)
      e.hasOwnProperty(f) && (u = e[f], u != null && _l(l, t, f, u, e, null));
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
        var a = null, n = null, i = null, f = null, d = null, g = null, A = null;
        for (E in e) {
          var N = e[E];
          if (e.hasOwnProperty(E) && N != null)
            switch (E) {
              case "checked":
                break;
              case "value":
                break;
              case "defaultValue":
                d = N;
              default:
                u.hasOwnProperty(E) || _l(l, t, E, null, u, N);
            }
        }
        for (var S in u) {
          var E = u[S];
          if (N = e[S], u.hasOwnProperty(S) && (E != null || N != null))
            switch (S) {
              case "type":
                n = E;
                break;
              case "name":
                a = E;
                break;
              case "checked":
                g = E;
                break;
              case "defaultChecked":
                A = E;
                break;
              case "value":
                i = E;
                break;
              case "defaultValue":
                f = E;
                break;
              case "children":
              case "dangerouslySetInnerHTML":
                if (E != null)
                  throw Error(s(137, t));
                break;
              default:
                E !== N && _l(
                  l,
                  t,
                  S,
                  E,
                  u,
                  N
                );
            }
        }
        Ai(
          l,
          i,
          f,
          d,
          g,
          A,
          n,
          a
        );
        return;
      case "select":
        E = i = f = S = null;
        for (n in e)
          if (d = e[n], e.hasOwnProperty(n) && d != null)
            switch (n) {
              case "value":
                break;
              case "multiple":
                E = d;
              default:
                u.hasOwnProperty(n) || _l(
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
                n !== d && _l(
                  l,
                  t,
                  a,
                  n,
                  u,
                  d
                );
            }
        t = f, e = i, u = E, S != null ? hu(l, !!e, S, !1) : !!u != !!e && (t != null ? hu(l, !!e, t, !0) : hu(l, !!e, e ? [] : "", !1));
        return;
      case "textarea":
        E = S = null;
        for (f in e)
          if (a = e[f], e.hasOwnProperty(f) && a != null && !u.hasOwnProperty(f))
            switch (f) {
              case "value":
                break;
              case "children":
                break;
              default:
                _l(l, t, f, null, u, a);
            }
        for (i in u)
          if (a = u[i], n = e[i], u.hasOwnProperty(i) && (a != null || n != null))
            switch (i) {
              case "value":
                S = a;
                break;
              case "defaultValue":
                E = a;
                break;
              case "children":
                break;
              case "dangerouslySetInnerHTML":
                if (a != null) throw Error(s(91));
                break;
              default:
                a !== n && _l(l, t, i, a, u, n);
            }
        ss(l, S, E);
        return;
      case "option":
        for (var B in e)
          if (S = e[B], e.hasOwnProperty(B) && S != null && !u.hasOwnProperty(B))
            switch (B) {
              case "selected":
                l.selected = !1;
                break;
              default:
                _l(
                  l,
                  t,
                  B,
                  null,
                  u,
                  S
                );
            }
        for (d in u)
          if (S = u[d], E = e[d], u.hasOwnProperty(d) && S !== E && (S != null || E != null))
            switch (d) {
              case "selected":
                l.selected = S && typeof S != "function" && typeof S != "symbol";
                break;
              default:
                _l(
                  l,
                  t,
                  d,
                  S,
                  u,
                  E
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
          S = e[w], e.hasOwnProperty(w) && S != null && !u.hasOwnProperty(w) && _l(l, t, w, null, u, S);
        for (g in u)
          if (S = u[g], E = e[g], u.hasOwnProperty(g) && S !== E && (S != null || E != null))
            switch (g) {
              case "children":
              case "dangerouslySetInnerHTML":
                if (S != null)
                  throw Error(s(137, t));
                break;
              default:
                _l(
                  l,
                  t,
                  g,
                  S,
                  u,
                  E
                );
            }
        return;
      default:
        if (Ti(t)) {
          for (var El in e)
            S = e[El], e.hasOwnProperty(El) && S !== void 0 && !u.hasOwnProperty(El) && yf(
              l,
              t,
              El,
              void 0,
              u,
              S
            );
          for (A in u)
            S = u[A], E = e[A], !u.hasOwnProperty(A) || S === E || S === void 0 && E === void 0 || yf(
              l,
              t,
              A,
              S,
              u,
              E
            );
          return;
        }
    }
    for (var h in e)
      S = e[h], e.hasOwnProperty(h) && S != null && !u.hasOwnProperty(h) && _l(l, t, h, null, u, S);
    for (N in u)
      S = u[N], E = e[N], !u.hasOwnProperty(N) || S === E || S == null && E == null || _l(l, t, N, S, u, E);
  }
  function vd(l) {
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
        if (n && f && vd(i)) {
          for (i = 0, f = a.responseEnd, u += 1; u < e.length; u++) {
            var d = e[u], g = d.startTime;
            if (g > f) break;
            var A = d.transferSize, N = d.initiatorType;
            A && vd(N) && (d = d.responseEnd, i += A * (d < f ? 1 : (f - g) / (d - g)));
          }
          if (--u, t += 8 * (n + i) / (a.duration / 1e3), l++, 10 < l) break;
        }
      }
      if (0 < l) return t / l / 1e6;
    }
    return navigator.connection && (l = navigator.connection.downlink, typeof l == "number") ? l : 5;
  }
  var vf = null, hf = null;
  function In(l) {
    return l.nodeType === 9 ? l : l.ownerDocument;
  }
  function hd(l) {
    switch (l) {
      case "http://www.w3.org/2000/svg":
        return 1;
      case "http://www.w3.org/1998/Math/MathML":
        return 2;
      default:
        return 0;
    }
  }
  function md(l, t) {
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
  function mf(l, t) {
    return l === "textarea" || l === "noscript" || typeof t.children == "string" || typeof t.children == "number" || typeof t.children == "bigint" || typeof t.dangerouslySetInnerHTML == "object" && t.dangerouslySetInnerHTML !== null && t.dangerouslySetInnerHTML.__html != null;
  }
  var gf = null;
  function uh() {
    var l = window.event;
    return l && l.type === "popstate" ? l === gf ? !1 : (gf = l, !0) : (gf = null, !1);
  }
  var gd = typeof setTimeout == "function" ? setTimeout : void 0, ah = typeof clearTimeout == "function" ? clearTimeout : void 0, bd = typeof Promise == "function" ? Promise : void 0, nh = typeof queueMicrotask == "function" ? queueMicrotask : typeof bd < "u" ? function(l) {
    return bd.resolve(null).then(l).catch(ih);
  } : gd;
  function ih(l) {
    setTimeout(function() {
      throw l;
    });
  }
  function Ce(l) {
    return l === "head";
  }
  function Sd(l, t) {
    var e = t, u = 0;
    do {
      var a = e.nextSibling;
      if (l.removeChild(e), a && a.nodeType === 8)
        if (e = a.data, e === "/$" || e === "/&") {
          if (u === 0) {
            l.removeChild(a), wu(t);
            return;
          }
          u--;
        } else if (e === "$" || e === "$?" || e === "$~" || e === "$!" || e === "&")
          u++;
        else if (e === "html")
          Ya(l.ownerDocument.documentElement);
        else if (e === "head") {
          e = l.ownerDocument.head, Ya(e);
          for (var n = e.firstChild; n; ) {
            var i = n.nextSibling, f = n.nodeName;
            n[ea] || f === "SCRIPT" || f === "STYLE" || f === "LINK" && n.rel.toLowerCase() === "stylesheet" || e.removeChild(n), n = i;
          }
        } else
          e === "body" && Ya(l.ownerDocument.body);
      e = a;
    } while (e);
    wu(t);
  }
  function pd(l, t) {
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
  function bf(l) {
    var t = l.firstChild;
    for (t && t.nodeType === 10 && (t = t.nextSibling); t; ) {
      var e = t;
      switch (t = t.nextSibling, e.nodeName) {
        case "HTML":
        case "HEAD":
        case "BODY":
          bf(e), Ei(e);
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
        if (!l[ea])
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
      if (l = Dt(l.nextSibling), l === null) break;
    }
    return null;
  }
  function fh(l, t, e) {
    if (t === "") return null;
    for (; l.nodeType !== 3; )
      if ((l.nodeType !== 1 || l.nodeName !== "INPUT" || l.type !== "hidden") && !e || (l = Dt(l.nextSibling), l === null)) return null;
    return l;
  }
  function _d(l, t) {
    for (; l.nodeType !== 8; )
      if ((l.nodeType !== 1 || l.nodeName !== "INPUT" || l.type !== "hidden") && !t || (l = Dt(l.nextSibling), l === null)) return null;
    return l;
  }
  function Sf(l) {
    return l.data === "$?" || l.data === "$~";
  }
  function pf(l) {
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
  function Dt(l) {
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
  var _f = null;
  function Ed(l) {
    l = l.nextSibling;
    for (var t = 0; l; ) {
      if (l.nodeType === 8) {
        var e = l.data;
        if (e === "/$" || e === "/&") {
          if (t === 0)
            return Dt(l.nextSibling);
          t--;
        } else
          e !== "$" && e !== "$!" && e !== "$?" && e !== "$~" && e !== "&" || t++;
      }
      l = l.nextSibling;
    }
    return null;
  }
  function zd(l) {
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
    switch (t = In(e), l) {
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
  function Ya(l) {
    for (var t = l.attributes; t.length; )
      l.removeAttributeNode(t[0]);
    Ei(l);
  }
  var Ut = /* @__PURE__ */ new Map(), qd = /* @__PURE__ */ new Set();
  function Pn(l) {
    return typeof l.getRootNode == "function" ? l.getRootNode() : l.nodeType === 9 ? l : l.ownerDocument;
  }
  var re = H.d;
  H.d = {
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
    var l = re.f(), t = Vn();
    return l || t;
  }
  function oh(l) {
    var t = du(l);
    t !== null && t.tag === 5 && t.type === "form" ? Qr(t) : re.r(l);
  }
  var Vu = typeof document > "u" ? null : document;
  function Td(l, t, e) {
    var u = Vu;
    if (u && typeof t == "string" && t) {
      var a = At(t);
      a = 'link[rel="' + l + '"][href="' + a + '"]', typeof e == "string" && (a += '[crossorigin="' + e + '"]'), qd.has(a) || (qd.add(a), l = { rel: l, crossOrigin: e, href: t }, u.querySelector(a) === null && (t = u.createElement("link"), $l(t, "link", l), Ql(t), u.head.appendChild(t)));
    }
  }
  function dh(l) {
    re.D(l), Td("dns-prefetch", l, null);
  }
  function yh(l, t) {
    re.C(l, t), Td("preconnect", l, t);
  }
  function vh(l, t, e) {
    re.L(l, t, e);
    var u = Vu;
    if (u && l && t) {
      var a = 'link[rel="preload"][as="' + At(t) + '"]';
      t === "image" && e && e.imageSrcSet ? (a += '[imagesrcset="' + At(
        e.imageSrcSet
      ) + '"]', typeof e.imageSizes == "string" && (a += '[imagesizes="' + At(
        e.imageSizes
      ) + '"]')) : a += '[href="' + At(l) + '"]';
      var n = a;
      switch (t) {
        case "style":
          n = Ku(l);
          break;
        case "script":
          n = Ju(l);
      }
      Ut.has(n) || (l = p(
        {
          rel: "preload",
          href: t === "image" && e && e.imageSrcSet ? void 0 : l,
          as: t
        },
        e
      ), Ut.set(n, l), u.querySelector(a) !== null || t === "style" && u.querySelector(Ga(n)) || t === "script" && u.querySelector(La(n)) || (t = u.createElement("link"), $l(t, "link", l), Ql(t), u.head.appendChild(t)));
    }
  }
  function hh(l, t) {
    re.m(l, t);
    var e = Vu;
    if (e && l) {
      var u = t && typeof t.as == "string" ? t.as : "script", a = 'link[rel="modulepreload"][as="' + At(u) + '"][href="' + At(l) + '"]', n = a;
      switch (u) {
        case "audioworklet":
        case "paintworklet":
        case "serviceworker":
        case "sharedworker":
        case "worker":
        case "script":
          n = Ju(l);
      }
      if (!Ut.has(n) && (l = p({ rel: "modulepreload", href: l }, t), Ut.set(n, l), e.querySelector(a) === null)) {
        switch (u) {
          case "audioworklet":
          case "paintworklet":
          case "serviceworker":
          case "sharedworker":
          case "worker":
          case "script":
            if (e.querySelector(La(n)))
              return;
        }
        u = e.createElement("link"), $l(u, "link", l), Ql(u), e.head.appendChild(u);
      }
    }
  }
  function mh(l, t, e) {
    re.S(l, t, e);
    var u = Vu;
    if (u && l) {
      var a = yu(u).hoistableStyles, n = Ku(l);
      t = t || "default";
      var i = a.get(n);
      if (!i) {
        var f = { loading: 0, preload: null };
        if (i = u.querySelector(
          Ga(n)
        ))
          f.loading = 5;
        else {
          l = p(
            { rel: "stylesheet", href: l, "data-precedence": t },
            e
          ), (e = Ut.get(n)) && Ef(l, e);
          var d = i = u.createElement("link");
          Ql(d), $l(d, "link", l), d._p = new Promise(function(g, A) {
            d.onload = g, d.onerror = A;
          }), d.addEventListener("load", function() {
            f.loading |= 1;
          }), d.addEventListener("error", function() {
            f.loading |= 2;
          }), f.loading |= 4, li(i, t, u);
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
    var e = Vu;
    if (e && l) {
      var u = yu(e).hoistableScripts, a = Ju(l), n = u.get(a);
      n || (n = e.querySelector(La(a)), n || (l = p({ src: l, async: !0 }, t), (t = Ut.get(a)) && zf(l, t), n = e.createElement("script"), Ql(n), $l(n, "link", l), e.head.appendChild(n)), n = {
        type: "script",
        instance: n,
        count: 1,
        state: null
      }, u.set(a, n));
    }
  }
  function bh(l, t) {
    re.M(l, t);
    var e = Vu;
    if (e && l) {
      var u = yu(e).hoistableScripts, a = Ju(l), n = u.get(a);
      n || (n = e.querySelector(La(a)), n || (l = p({ src: l, async: !0, type: "module" }, t), (t = Ut.get(a)) && zf(l, t), n = e.createElement("script"), Ql(n), $l(n, "link", l), e.head.appendChild(n)), n = {
        type: "script",
        instance: n,
        count: 1,
        state: null
      }, u.set(a, n));
    }
  }
  function xd(l, t, e, u) {
    var a = (a = ul.current) ? Pn(a) : null;
    if (!a) throw Error(s(446));
    switch (l) {
      case "meta":
      case "title":
        return null;
      case "style":
        return typeof e.precedence == "string" && typeof e.href == "string" ? (t = Ku(e.href), e = yu(
          a
        ).hoistableStyles, u = e.get(t), u || (u = {
          type: "style",
          instance: null,
          count: 0,
          state: null
        }, e.set(t, u)), u) : { type: "void", instance: null, count: 0, state: null };
      case "link":
        if (e.rel === "stylesheet" && typeof e.href == "string" && typeof e.precedence == "string") {
          l = Ku(e.href);
          var n = yu(
            a
          ).hoistableStyles, i = n.get(l);
          if (i || (a = a.ownerDocument || a, i = {
            type: "stylesheet",
            instance: null,
            count: 0,
            state: { loading: 0, preload: null }
          }, n.set(l, i), (n = a.querySelector(
            Ga(l)
          )) && !n._p && (i.instance = n, i.state.loading = 5), Ut.has(l) || (e = {
            rel: "preload",
            as: "style",
            href: e.href,
            crossOrigin: e.crossOrigin,
            integrity: e.integrity,
            media: e.media,
            hrefLang: e.hrefLang,
            referrerPolicy: e.referrerPolicy
          }, Ut.set(l, e), n || Sh(
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
        return t = e.async, e = e.src, typeof e == "string" && t && typeof t != "function" && typeof t != "symbol" ? (t = Ju(e), e = yu(
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
  function Ku(l) {
    return 'href="' + At(l) + '"';
  }
  function Ga(l) {
    return 'link[rel="stylesheet"][' + l + "]";
  }
  function Nd(l) {
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
    }), $l(t, "link", e), Ql(t), l.head.appendChild(t));
  }
  function Ju(l) {
    return '[src="' + At(l) + '"]';
  }
  function La(l) {
    return "script[async]" + l;
  }
  function Od(l, t, e) {
    if (t.count++, t.instance === null)
      switch (t.type) {
        case "style":
          var u = l.querySelector(
            'style[data-href~="' + At(e.href) + '"]'
          );
          if (u)
            return t.instance = u, Ql(u), u;
          var a = p({}, e, {
            "data-href": e.href,
            "data-precedence": e.precedence,
            href: null,
            precedence: null
          });
          return u = (l.ownerDocument || l).createElement(
            "style"
          ), Ql(u), $l(u, "style", a), li(u, e.precedence, l), t.instance = u;
        case "stylesheet":
          a = Ku(e.href);
          var n = l.querySelector(
            Ga(a)
          );
          if (n)
            return t.state.loading |= 4, t.instance = n, Ql(n), n;
          u = Nd(e), (a = Ut.get(a)) && Ef(u, a), n = (l.ownerDocument || l).createElement("link"), Ql(n);
          var i = n;
          return i._p = new Promise(function(f, d) {
            i.onload = f, i.onerror = d;
          }), $l(n, "link", u), t.state.loading |= 4, li(n, e.precedence, l), t.instance = n;
        case "script":
          return n = Ju(e.src), (a = l.querySelector(
            La(n)
          )) ? (t.instance = a, Ql(a), a) : (u = e, (a = Ut.get(n)) && (u = p({}, e), zf(u, a)), l = l.ownerDocument || l, a = l.createElement("script"), Ql(a), $l(a, "link", u), l.head.appendChild(a), t.instance = a);
        case "void":
          return null;
        default:
          throw Error(s(443, t.type));
      }
    else
      t.type === "stylesheet" && (t.state.loading & 4) === 0 && (u = t.instance, t.state.loading |= 4, li(u, e.precedence, l));
    return t.instance;
  }
  function li(l, t, e) {
    for (var u = e.querySelectorAll(
      'link[rel="stylesheet"][data-precedence],style[data-precedence]'
    ), a = u.length ? u[u.length - 1] : null, n = a, i = 0; i < u.length; i++) {
      var f = u[i];
      if (f.dataset.precedence === t) n = f;
      else if (n !== a) break;
    }
    n ? n.parentNode.insertBefore(l, n.nextSibling) : (t = e.nodeType === 9 ? e.head : e, t.insertBefore(l, t.firstChild));
  }
  function Ef(l, t) {
    l.crossOrigin == null && (l.crossOrigin = t.crossOrigin), l.referrerPolicy == null && (l.referrerPolicy = t.referrerPolicy), l.title == null && (l.title = t.title);
  }
  function zf(l, t) {
    l.crossOrigin == null && (l.crossOrigin = t.crossOrigin), l.referrerPolicy == null && (l.referrerPolicy = t.referrerPolicy), l.integrity == null && (l.integrity = t.integrity);
  }
  var ti = null;
  function Md(l, t, e) {
    if (ti === null) {
      var u = /* @__PURE__ */ new Map(), a = ti = /* @__PURE__ */ new Map();
      a.set(e, u);
    } else
      a = ti, u = a.get(e), u || (u = /* @__PURE__ */ new Map(), a.set(e, u));
    if (u.has(l)) return u;
    for (u.set(l, null), e = e.getElementsByTagName(l), a = 0; a < e.length; a++) {
      var n = e[a];
      if (!(n[ea] || n[Kl] || l === "link" && n.getAttribute("rel") === "stylesheet") && n.namespaceURI !== "http://www.w3.org/2000/svg") {
        var i = n.getAttribute(t) || "";
        i = l + i;
        var f = u.get(i);
        f ? f.push(n) : u.set(i, [n]);
      }
    }
    return u;
  }
  function Dd(l, t, e) {
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
  function Ud(l) {
    return !(l.type === "stylesheet" && (l.state.loading & 3) === 0);
  }
  function _h(l, t, e, u) {
    if (e.type === "stylesheet" && (typeof u.media != "string" || matchMedia(u.media).matches !== !1) && (e.state.loading & 4) === 0) {
      if (e.instance === null) {
        var a = Ku(u.href), n = t.querySelector(
          Ga(a)
        );
        if (n) {
          t = n._p, t !== null && typeof t == "object" && typeof t.then == "function" && (l.count++, l = ei.bind(l), t.then(l, l)), e.state.loading |= 4, e.instance = n, Ql(n);
          return;
        }
        n = t.ownerDocument || t, u = Nd(u), (a = Ut.get(a)) && Ef(u, a), n = n.createElement("link"), Ql(n);
        var i = n;
        i._p = new Promise(function(f, d) {
          i.onload = f, i.onerror = d;
        }), $l(n, "link", u), e.instance = n;
      }
      l.stylesheets === null && (l.stylesheets = /* @__PURE__ */ new Map()), l.stylesheets.set(e, t), (t = e.state.preload) && (e.state.loading & 3) === 0 && (l.count++, e = ei.bind(l), t.addEventListener("load", e), t.addEventListener("error", e));
    }
  }
  var Af = 0;
  function Eh(l, t) {
    return l.stylesheets && l.count === 0 && ai(l, l.stylesheets), 0 < l.count || 0 < l.imgCount ? function(e) {
      var u = setTimeout(function() {
        if (l.stylesheets && ai(l, l.stylesheets), l.unsuspend) {
          var n = l.unsuspend;
          l.unsuspend = null, n();
        }
      }, 6e4 + t);
      0 < l.imgBytes && Af === 0 && (Af = 62500 * eh());
      var a = setTimeout(
        function() {
          if (l.waitingForImages = !1, l.count === 0 && (l.stylesheets && ai(l, l.stylesheets), l.unsuspend)) {
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
  function ei() {
    if (this.count--, this.count === 0 && (this.imgCount === 0 || !this.waitingForImages)) {
      if (this.stylesheets) ai(this, this.stylesheets);
      else if (this.unsuspend) {
        var l = this.unsuspend;
        this.unsuspend = null, l();
      }
    }
  }
  var ui = null;
  function ai(l, t) {
    l.stylesheets = null, l.unsuspend !== null && (l.count++, ui = /* @__PURE__ */ new Map(), t.forEach(zh, l), ui = null, ei.call(l));
  }
  function zh(l, t) {
    if (!(t.state.loading & 4)) {
      var e = ui.get(l);
      if (e) var u = e.get(null);
      else {
        e = /* @__PURE__ */ new Map(), ui.set(l, e);
        for (var a = l.querySelectorAll(
          "link[data-precedence],style[data-precedence]"
        ), n = 0; n < a.length; n++) {
          var i = a[n];
          (i.nodeName === "LINK" || i.getAttribute("media") !== "not all") && (e.set(i.dataset.precedence, i), u = i);
        }
        u && e.set(null, u);
      }
      a = t.instance, i = a.getAttribute("data-precedence"), n = e.get(i) || u, n === u && e.set(null, a), e.set(i, a), this.count++, u = ei.bind(this), a.addEventListener("load", u), a.addEventListener("error", u), n ? n.parentNode.insertBefore(a, n.nextSibling) : (l = l.nodeType === 9 ? l.head : l, l.insertBefore(a, l.firstChild)), t.state.loading |= 4;
    }
  }
  var Qa = {
    $$typeof: W,
    Provider: null,
    Consumer: null,
    _currentValue: J,
    _currentValue2: J,
    _threadCount: 0
  };
  function Ah(l, t, e, u, a, n, i, f, d) {
    this.tag = 1, this.containerInfo = l, this.pingCache = this.current = this.pendingChildren = null, this.timeoutHandle = -1, this.callbackNode = this.next = this.pendingContext = this.context = this.cancelPendingCommit = null, this.callbackPriority = 0, this.expirationTimes = bi(-1), this.entangledLanes = this.shellSuspendCounter = this.errorRecoveryDisabledLanes = this.expiredLanes = this.warmLanes = this.pingedLanes = this.suspendedLanes = this.pendingLanes = 0, this.entanglements = bi(0), this.hiddenUpdates = bi(null), this.identifierPrefix = u, this.onUncaughtError = a, this.onCaughtError = n, this.onRecoverableError = i, this.pooledCache = null, this.pooledCacheLanes = 0, this.formState = d, this.incompleteTransitions = /* @__PURE__ */ new Map();
  }
  function Cd(l, t, e, u, a, n, i, f, d, g, A, N) {
    return l = new Ah(
      l,
      t,
      e,
      i,
      d,
      g,
      A,
      N,
      f
    ), t = 1, n === !0 && (t |= 24), n = ht(3, null, null, t), l.current = n, n.stateNode = l, t = ec(), t.refCount++, l.pooledCache = t, t.refCount++, n.memoizedState = {
      element: u,
      isDehydrated: e,
      cache: t
    }, ic(n), l;
  }
  function jd(l) {
    return l ? (l = zu, l) : zu;
  }
  function Rd(l, t, e, u, a, n) {
    a = jd(a), u.context === null ? u.context = a : u.pendingContext = a, u = Ee(t), u.payload = { element: e }, n = n === void 0 ? null : n, n !== null && (u.callback = n), e = ze(l, u, t), e !== null && (it(e, l, t), Sa(e, l, t));
  }
  function Hd(l, t) {
    if (l = l.memoizedState, l !== null && l.dehydrated !== null) {
      var e = l.retryLane;
      l.retryLane = e !== 0 && e < t ? e : t;
    }
  }
  function qf(l, t) {
    Hd(l, t), (l = l.alternate) && Hd(l, t);
  }
  function Bd(l) {
    if (l.tag === 13 || l.tag === 31) {
      var t = ke(l, 67108864);
      t !== null && it(t, l, 67108864), qf(l, 67108864);
    }
  }
  function Yd(l) {
    if (l.tag === 13 || l.tag === 31) {
      var t = pt();
      t = Si(t);
      var e = ke(l, t);
      e !== null && it(e, l, t), qf(l, t);
    }
  }
  var ni = !0;
  function qh(l, t, e, u) {
    var a = _.T;
    _.T = null;
    var n = H.p;
    try {
      H.p = 2, Tf(l, t, e, u);
    } finally {
      H.p = n, _.T = a;
    }
  }
  function Th(l, t, e, u) {
    var a = _.T;
    _.T = null;
    var n = H.p;
    try {
      H.p = 8, Tf(l, t, e, u);
    } finally {
      H.p = n, _.T = a;
    }
  }
  function Tf(l, t, e, u) {
    if (ni) {
      var a = xf(u);
      if (a === null)
        df(
          l,
          t,
          u,
          ii,
          e
        ), Ld(l, u);
      else if (Nh(
        a,
        l,
        t,
        e,
        u
      ))
        u.stopPropagation();
      else if (Ld(l, u), t & 4 && -1 < xh.indexOf(l)) {
        for (; a !== null; ) {
          var n = du(a);
          if (n !== null)
            switch (n.tag) {
              case 3:
                if (n = n.stateNode, n.current.memoizedState.isDehydrated) {
                  var i = Ze(n.pendingLanes);
                  if (i !== 0) {
                    var f = n;
                    for (f.pendingLanes |= 2, f.entangledLanes |= 2; i; ) {
                      var d = 1 << 31 - yt(i);
                      f.entanglements[1] |= d, i &= ~d;
                    }
                    Vt(n), (gl & 6) === 0 && (Xn = ot() + 500, Ra(0));
                  }
                }
                break;
              case 31:
              case 13:
                f = ke(n, 2), f !== null && it(f, n, 2), Vn(), qf(n, 2);
            }
          if (n = xf(u), n === null && df(
            l,
            t,
            u,
            ii,
            e
          ), n === a) break;
          a = n;
        }
        a !== null && u.stopPropagation();
      } else
        df(
          l,
          t,
          u,
          null,
          e
        );
    }
  }
  function xf(l) {
    return l = Ni(l), Nf(l);
  }
  var ii = null;
  function Nf(l) {
    if (ii = null, l = ou(l), l !== null) {
      var t = z(l);
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
    return ii = l, null;
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
          case Jf:
            return 2;
          case wf:
            return 8;
          case ka:
          case yy:
            return 32;
          case kf:
            return 268435456;
          default:
            return 32;
        }
      default:
        return 32;
    }
  }
  var Of = !1, je = null, Re = null, He = null, Xa = /* @__PURE__ */ new Map(), Za = /* @__PURE__ */ new Map(), Be = [], xh = "mousedown mouseup touchcancel touchend touchstart auxclick dblclick pointercancel pointerdown pointerup dragend dragstart drop compositionend compositionstart keydown keypress keyup input textInput copy cut paste click change contextmenu reset".split(
    " "
  );
  function Ld(l, t) {
    switch (l) {
      case "focusin":
      case "focusout":
        je = null;
        break;
      case "dragenter":
      case "dragleave":
        Re = null;
        break;
      case "mouseover":
      case "mouseout":
        He = null;
        break;
      case "pointerover":
      case "pointerout":
        Xa.delete(t.pointerId);
        break;
      case "gotpointercapture":
      case "lostpointercapture":
        Za.delete(t.pointerId);
    }
  }
  function Va(l, t, e, u, a, n) {
    return l === null || l.nativeEvent !== n ? (l = {
      blockedOn: t,
      domEventName: e,
      eventSystemFlags: u,
      nativeEvent: n,
      targetContainers: [a]
    }, t !== null && (t = du(t), t !== null && Bd(t)), l) : (l.eventSystemFlags |= u, t = l.targetContainers, a !== null && t.indexOf(a) === -1 && t.push(a), l);
  }
  function Nh(l, t, e, u, a) {
    switch (t) {
      case "focusin":
        return je = Va(
          je,
          l,
          t,
          e,
          u,
          a
        ), !0;
      case "dragenter":
        return Re = Va(
          Re,
          l,
          t,
          e,
          u,
          a
        ), !0;
      case "mouseover":
        return He = Va(
          He,
          l,
          t,
          e,
          u,
          a
        ), !0;
      case "pointerover":
        var n = a.pointerId;
        return Xa.set(
          n,
          Va(
            Xa.get(n) || null,
            l,
            t,
            e,
            u,
            a
          )
        ), !0;
      case "gotpointercapture":
        return n = a.pointerId, Za.set(
          n,
          Va(
            Za.get(n) || null,
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
  function Qd(l) {
    var t = ou(l.target);
    if (t !== null) {
      var e = z(t);
      if (e !== null) {
        if (t = e.tag, t === 13) {
          if (t = C(e), t !== null) {
            l.blockedOn = t, ls(l.priority, function() {
              Yd(e);
            });
            return;
          }
        } else if (t === 31) {
          if (t = U(e), t !== null) {
            l.blockedOn = t, ls(l.priority, function() {
              Yd(e);
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
  function ci(l) {
    if (l.blockedOn !== null) return !1;
    for (var t = l.targetContainers; 0 < t.length; ) {
      var e = xf(l.nativeEvent);
      if (e === null) {
        e = l.nativeEvent;
        var u = new e.constructor(
          e.type,
          e
        );
        xi = u, e.target.dispatchEvent(u), xi = null;
      } else
        return t = du(e), t !== null && Bd(t), l.blockedOn = e, !1;
      t.shift();
    }
    return !0;
  }
  function Xd(l, t, e) {
    ci(l) && e.delete(t);
  }
  function Oh() {
    Of = !1, je !== null && ci(je) && (je = null), Re !== null && ci(Re) && (Re = null), He !== null && ci(He) && (He = null), Xa.forEach(Xd), Za.forEach(Xd);
  }
  function fi(l, t) {
    l.blockedOn === t && (l.blockedOn = null, Of || (Of = !0, c.unstable_scheduleCallback(
      c.unstable_NormalPriority,
      Oh
    )));
  }
  var si = null;
  function Zd(l) {
    si !== l && (si = l, c.unstable_scheduleCallback(
      c.unstable_NormalPriority,
      function() {
        si === l && (si = null);
        for (var t = 0; t < l.length; t += 3) {
          var e = l[t], u = l[t + 1], a = l[t + 2];
          if (typeof u != "function") {
            if (Nf(u || e) === null)
              continue;
            break;
          }
          var n = du(e);
          n !== null && (l.splice(t, 3), t -= 3, Tc(
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
  function wu(l) {
    function t(d) {
      return fi(d, l);
    }
    je !== null && fi(je, l), Re !== null && fi(Re, l), He !== null && fi(He, l), Xa.forEach(t), Za.forEach(t);
    for (var e = 0; e < Be.length; e++) {
      var u = Be[e];
      u.blockedOn === l && (u.blockedOn = null);
    }
    for (; 0 < Be.length && (e = Be[0], e.blockedOn === null); )
      Qd(e), e.blockedOn === null && Be.shift();
    if (e = (l.ownerDocument || l).$$reactFormReplay, e != null)
      for (u = 0; u < e.length; u += 3) {
        var a = e[u], n = e[u + 1], i = a[lt] || null;
        if (typeof n == "function")
          i || Zd(e);
        else if (i) {
          var f = null;
          if (n && n.hasAttribute("formAction")) {
            if (a = n, i = n[lt] || null)
              f = i.formAction;
            else if (Nf(a) !== null) continue;
          } else f = i.action;
          typeof f == "function" ? e[u + 1] = f : (e.splice(u, 3), u -= 3), Zd(e);
        }
      }
  }
  function Vd() {
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
  function Mf(l) {
    this._internalRoot = l;
  }
  ri.prototype.render = Mf.prototype.render = function(l) {
    var t = this._internalRoot;
    if (t === null) throw Error(s(409));
    var e = t.current, u = pt();
    Rd(e, u, l, t, null, null);
  }, ri.prototype.unmount = Mf.prototype.unmount = function() {
    var l = this._internalRoot;
    if (l !== null) {
      this._internalRoot = null;
      var t = l.containerInfo;
      Rd(l.current, 2, null, l, null, null), Vn(), t[ru] = null;
    }
  };
  function ri(l) {
    this._internalRoot = l;
  }
  ri.prototype.unstable_scheduleHydration = function(l) {
    if (l) {
      var t = Pf();
      l = { blockedOn: null, target: l, priority: t };
      for (var e = 0; e < Be.length && t !== 0 && t < Be[e].priority; e++) ;
      Be.splice(e, 0, l), e === 0 && Qd(l);
    }
  };
  var Kd = o.version;
  if (Kd !== "19.2.5")
    throw Error(
      s(
        527,
        Kd,
        "19.2.5"
      )
    );
  H.findDOMNode = function(l) {
    var t = l._reactInternals;
    if (t === void 0)
      throw typeof l.render == "function" ? Error(s(188)) : (l = Object.keys(l).join(","), Error(s(268, l)));
    return l = b(t), l = l !== null ? j(l) : null, l = l === null ? null : l.stateNode, l;
  };
  var Mh = {
    bundleType: 0,
    version: "19.2.5",
    rendererPackageName: "react-dom",
    currentDispatcherRef: _,
    reconcilerVersion: "19.2.5"
  };
  if (typeof __REACT_DEVTOOLS_GLOBAL_HOOK__ < "u") {
    var oi = __REACT_DEVTOOLS_GLOBAL_HOOK__;
    if (!oi.isDisabled && oi.supportsFiber)
      try {
        Pu = oi.inject(
          Mh
        ), dt = oi;
      } catch {
      }
  }
  return Ka.createRoot = function(l, t) {
    if (!T(l)) throw Error(s(299));
    var e = !1, u = "", a = Fr, n = Ir, i = Pr;
    return t != null && (t.unstable_strictMode === !0 && (e = !0), t.identifierPrefix !== void 0 && (u = t.identifierPrefix), t.onUncaughtError !== void 0 && (a = t.onUncaughtError), t.onCaughtError !== void 0 && (n = t.onCaughtError), t.onRecoverableError !== void 0 && (i = t.onRecoverableError)), t = Cd(
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
      Vd
    ), l[ru] = t.current, of(l), new Mf(t);
  }, Ka.hydrateRoot = function(l, t, e) {
    if (!T(l)) throw Error(s(299));
    var u = !1, a = "", n = Fr, i = Ir, f = Pr, d = null;
    return e != null && (e.unstable_strictMode === !0 && (u = !0), e.identifierPrefix !== void 0 && (a = e.identifierPrefix), e.onUncaughtError !== void 0 && (n = e.onUncaughtError), e.onCaughtError !== void 0 && (i = e.onCaughtError), e.onRecoverableError !== void 0 && (f = e.onRecoverableError), e.formState !== void 0 && (d = e.formState)), t = Cd(
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
      Vd
    ), t.context = jd(null), e = t.current, u = pt(), u = Si(u), a = Ee(u), a.callback = null, ze(e, a, u), e = u, t.current.lanes = e, ta(t, e), Vt(t), l[ru] = t.current, of(l), new ri(t);
  }, Ka.version = "19.2.5", Ka;
}
var ly;
function Lh() {
  if (ly) return Cf.exports;
  ly = 1;
  function c() {
    if (!(typeof __REACT_DEVTOOLS_GLOBAL_HOOK__ > "u" || typeof __REACT_DEVTOOLS_GLOBAL_HOOK__.checkDCE != "function"))
      try {
        __REACT_DEVTOOLS_GLOBAL_HOOK__.checkDCE(c);
      } catch (o) {
        console.error(o);
      }
  }
  return c(), Cf.exports = Gh(), Cf.exports;
}
var Qh = Lh(), Bf = { exports: {} }, Ja = {};
/**
 * @license React
 * react-jsx-runtime.production.js
 *
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 */
var ty;
function Xh() {
  if (ty) return Ja;
  ty = 1;
  var c = Symbol.for("react.transitional.element"), o = Symbol.for("react.fragment");
  function r(s, T, z) {
    var C = null;
    if (z !== void 0 && (C = "" + z), T.key !== void 0 && (C = "" + T.key), "key" in T) {
      z = {};
      for (var U in T)
        U !== "key" && (z[U] = T[U]);
    } else z = T;
    return T = z.ref, {
      $$typeof: c,
      type: s,
      key: C,
      ref: T !== void 0 ? T : null,
      props: z
    };
  }
  return Ja.Fragment = o, Ja.jsx = r, Ja.jsxs = r, Ja;
}
var ey;
function Zh() {
  return ey || (ey = 1, Bf.exports = Xh()), Bf.exports;
}
var M = Zh();
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
class Kt extends Error {
  constructor(r, s) {
    super(`Expression syntax error at column ${s}: ${r}`);
    oe(this, "position");
    this.position = s, this.name = "ExpressionSyntaxError";
  }
}
function di(c) {
  return c >= "0" && c <= "9";
}
function iy(c) {
  return c >= "a" && c <= "z" || c >= "A" && c <= "Z";
}
function wh(c) {
  return iy(c) || di(c);
}
function kh(c) {
  return c === " " || c === "	" || c === `
` || c === "\r" || c === "\f" || c === "\v";
}
function $h(c) {
  const o = [];
  let r = 0;
  const s = () => r >= c.length, T = (p = 0) => c.charAt(r + p), z = (p) => {
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
    for (; !s() && di(T()); )
      r++;
    if (!s() && T() === ".")
      for (r++; !s() && di(T()); )
        r++;
    const D = c.substring(p, r), X = parseFloat(D);
    return { kind: "Number", text: D, literal: X, position: p };
  }, x = (p, D) => {
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
          throw new Kt(`unknown escape '\\${K}'.`, r);
        X += Z, r += 2;
      } else
        X += k, r++;
    }
    if (s())
      throw new Kt("unterminated string literal.", p);
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
  }, j = () => {
    const p = r, D = T();
    if (di(D))
      return U(p);
    if (D === "'" || D === '"')
      return x(p, D);
    if (D === "_" || iy(D))
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
        if (z("==="))
          return { kind: "StrictEq", text: "===", literal: null, position: p };
        if (z("=="))
          return { kind: "Eq", text: "==", literal: null, position: p };
        throw new Kt("bare '=' is not a valid operator (use '==' or '===').", p);
      case "!":
        return z("!==") ? { kind: "StrictNotEq", text: "!==", literal: null, position: p } : z("!=") ? { kind: "NotEq", text: "!=", literal: null, position: p } : (r++, { kind: "Not", text: "!", literal: null, position: p });
      case "<":
        return z("<=") ? { kind: "LtEq", text: "<=", literal: null, position: p } : (r++, { kind: "Lt", text: "<", literal: null, position: p });
      case ">":
        return z(">=") ? { kind: "GtEq", text: ">=", literal: null, position: p } : (r++, { kind: "Gt", text: ">", literal: null, position: p });
      case "&":
        if (z("&&"))
          return { kind: "And", text: "&&", literal: null, position: p };
        throw new Kt("expected '&&'.", p);
      case "|":
        if (z("||"))
          return { kind: "Or", text: "||", literal: null, position: p };
        throw new Kt("expected '||'.", p);
    }
    throw new Kt(`unexpected character '${D}'.`, p);
  };
  for (; ; ) {
    if (C(), s())
      return o.push({ kind: "EndOfInput", text: "", literal: null, position: r }), o;
    o.push(j());
  }
}
function Wh(c) {
  let o = 0;
  const r = () => {
    const L = c[o];
    if (!L)
      throw new Kt("unexpected end of tokens.", 0);
    return L;
  }, s = () => {
    const L = r();
    return L.kind !== "EndOfInput" && o++, L;
  }, T = (L) => r().kind !== L ? !1 : (s(), !0), z = (L) => {
    const Z = r();
    if (Z.kind !== L)
      throw new Kt(`expected ${L}, got '${Z.text}'.`, Z.position);
    return s(), Z;
  }, C = () => {
    let L = U();
    for (; T("Or"); )
      L = { kind: "BinaryOp", op: "||", left: L, right: U() };
    return L;
  }, U = () => {
    let L = x();
    for (; T("And"); )
      L = { kind: "BinaryOp", op: "&&", left: L, right: x() };
    return L;
  }, x = () => {
    let L = b();
    for (; ; ) {
      const Z = r().kind;
      let ol = null;
      if (Z === "Eq" || Z === "StrictEq" ? ol = "==" : (Z === "NotEq" || Z === "StrictNotEq") && (ol = "!="), ol === null)
        break;
      s(), L = { kind: "BinaryOp", op: ol, left: L, right: b() };
    }
    return L;
  }, b = () => {
    let L = j();
    for (; ; ) {
      const Z = r().kind;
      let ol = null;
      if (Z === "Lt" ? ol = "<" : Z === "Gt" ? ol = ">" : Z === "LtEq" ? ol = "<=" : Z === "GtEq" && (ol = ">="), ol === null)
        break;
      s(), L = { kind: "BinaryOp", op: ol, left: L, right: j() };
    }
    return L;
  }, j = () => T("Not") ? { kind: "UnaryOp", op: "!", operand: j() } : k(), p = () => {
    z("LBracket");
    const L = [];
    if (r().kind !== "RBracket")
      for (L.push(C()); T("Comma"); )
        L.push(C());
    return z("RBracket"), { kind: "Array", items: L };
  }, D = (L) => {
    let Z;
    if (T("Dot"))
      Z = z("Identifier").text;
    else if (T("LBracket")) {
      const ol = z("String");
      z("RBracket"), Z = ol.literal;
    } else
      throw new Kt("'answers' must be followed by .key or ['key'].", L);
    return { kind: "AnswersAccess", key: Z };
  }, X = () => {
    const L = s();
    if (L.text === "answers")
      return D(L.position);
    z("LParen");
    const Z = [];
    if (r().kind !== "RParen")
      for (Z.push(C()); T("Comma"); )
        Z.push(C());
    return z("RParen"), { kind: "Call", name: L.text, args: Z };
  }, k = () => {
    const L = r();
    switch (L.kind) {
      case "Number":
      case "String":
      case "True":
      case "False":
      case "Null":
        return s(), { kind: "Literal", value: L.literal };
      case "LParen": {
        s();
        const Z = C();
        return z("RParen"), Z;
      }
      case "LBracket":
        return p();
      case "Identifier":
        return X();
      default:
        throw new Kt(`unexpected token '${L.text}'.`, L.position);
    }
  }, K = C();
  return z("EndOfInput"), K;
}
function ye(c) {
  return c === void 0 || c === null ? null : typeof c == "boolean" || typeof c == "number" || typeof c == "string" ? c : Array.isArray(c) ? c.map(ye) : null;
}
function su(c, o) {
  const r = ye(c), s = ye(o);
  if (r === null || s === null)
    return r === null && s === null;
  if (typeof r == "number" && typeof s == "number" || typeof r == "string" && typeof s == "string" || typeof r == "boolean" && typeof s == "boolean")
    return r === s;
  if (Array.isArray(r) && Array.isArray(s)) {
    if (r.length !== s.length)
      return !1;
    for (let T = 0; T < r.length; T++)
      if (!su(r[T], s[T]))
        return !1;
    return !0;
  }
  return !1;
}
function Qe(c, o) {
  const r = ye(c), s = ye(o);
  if (typeof r == "number" && typeof s == "number" || typeof r == "string" && typeof s == "string")
    return r < s ? -1 : r > s ? 1 : 0;
  throw new Error("Comparison operators require two numbers or two strings.");
}
function Wu(c) {
  const o = ye(c);
  return o === null ? !1 : typeof o == "boolean" ? o : typeof o == "number" ? o !== 0 : typeof o == "string" || Array.isArray(o) ? o.length > 0 : !0;
}
function Ct(c, o) {
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
      return c.items.map((r) => Ct(r, o));
  }
}
function Fh(c, o) {
  const r = Ct(c.operand, o);
  if (c.op === "!")
    return !Wu(r);
  throw new Error(`Unknown unary operator '${c.op}'.`);
}
function Ih(c, o) {
  if (c.op === "&&") {
    const T = Ct(c.left, o);
    return Wu(T) ? Wu(Ct(c.right, o)) : !1;
  }
  if (c.op === "||") {
    const T = Ct(c.left, o);
    return Wu(T) ? !0 : Wu(Ct(c.right, o));
  }
  const r = Ct(c.left, o), s = Ct(c.right, o);
  switch (c.op) {
    case "==":
      return su(r, s);
    case "!=":
      return !su(r, s);
    case "<":
      return Qe(r, s) < 0;
    case ">":
      return Qe(r, s) > 0;
    case "<=":
      return Qe(r, s) <= 0;
    case ">=":
      return Qe(r, s) >= 0;
    default:
      throw new Error(`Unknown binary operator '${c.op}'.`);
  }
}
function Ph(c, o) {
  switch (c.name) {
    case "has":
    case "isSet":
      return uy(c, o);
    case "isNotSet":
      return !uy(c, o);
    case "in":
      return lm(c, o);
    default:
      throw new Error(`Unknown function '${c.name}'.`);
  }
}
function uy(c, o) {
  if (c.args.length !== 1)
    throw new Error(`${c.name}() takes one argument.`);
  const r = c.args[0];
  if (!r)
    return !1;
  const s = Ct(r, o);
  return typeof s != "string" ? !1 : s in o && o[s] !== null && o[s] !== void 0;
}
function lm(c, o) {
  if (c.args.length !== 2)
    throw new Error("in() takes two arguments: in(value, [array]).");
  const r = c.args[0], s = c.args[1];
  if (!r || !s)
    return !1;
  const T = Ct(r, o), z = Ct(s, o);
  return Array.isArray(z) ? z.some((C) => su(T, C)) : !1;
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
    return Wu(Ct(r, o));
  } catch {
    return !1;
  }
}
function am(c, o) {
  var r;
  if (!c.logic)
    return null;
  for (const s of c.logic)
    if (Lf(s.if, o))
      return ((r = s.then) == null ? void 0 : r.goto) ?? null;
  return null;
}
function Lf(c, o) {
  try {
    return Vh(c) ? im(c, o) : Kh(c) ? nm(c, o) : Jh(c) ? um(c.expression, o) : !1;
  } catch {
    return !1;
  }
}
function nm(c, o) {
  return c.all && c.all.length > 0 ? c.all.every((r) => Lf(r, o)) : c.any && c.any.length > 0 ? c.any.some((r) => Lf(r, o)) : !1;
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
      return su(o, r);
    case "!=":
      return !su(o, r);
    case ">":
      return Qe(o, r) > 0;
    case ">=":
      return Qe(o, r) >= 0;
    case "<":
      return Qe(o, r) < 0;
    case "<=":
      return Qe(o, r) <= 0;
    case "in":
      return ay(r, o);
    case "notIn":
      return !ay(r, o);
    default:
      return !1;
  }
}
function ay(c, o) {
  return Array.isArray(c) ? c.some((r) => su(o, r)) : !1;
}
function Qf(c, o, r) {
  const s = new Set(c.screens.map((U) => U.id)), T = am(c, r);
  if (T && T !== o && s.has(T))
    return { kind: "screen", screenId: T };
  const z = c.screens.find((U) => U.id === o);
  if (z != null && z.nextScreen && z.nextScreen !== o && s.has(z.nextScreen))
    return { kind: "screen", screenId: z.nextScreen };
  if (z && (!z.questions || z.questions.length === 0) && !z.nextScreen)
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
  const T = new Set(o.screens.map((z) => z.id));
  return c.nextScreen && T.has(c.nextScreen) ? { kind: "screen", screenId: c.nextScreen } : Qf(o, r, s);
}
class ku extends Error {
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
class ny {
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
    } catch (z) {
      throw new ku({
        status: 0,
        code: "network",
        message: `Network error calling ${o} ${r}: ${z.message ?? z}`
      });
    }
    if (!T.ok)
      throw await this.toError(T, o, r);
    return T;
  }
  async readJson(o) {
    const r = await o.text();
    if (!r)
      throw new ku({
        status: o.status,
        code: "parse",
        message: `Empty body from ${o.url}`
      });
    try {
      return JSON.parse(r);
    } catch (s) {
      throw new ku({
        status: o.status,
        code: "parse",
        message: `Could not parse JSON from ${o.url}: ${s.message}`,
        raw: r
      });
    }
  }
  async toError(o, r, s) {
    const T = o.status === 404 ? "notFound" : o.status === 410 ? "gone" : o.status === 409 ? "conflict" : o.status === 400 ? "badRequest" : (o.status >= 500, "server"), z = await o.text();
    if (!z)
      return new ku({
        status: o.status,
        code: T,
        message: `${r} ${s} → ${o.status}`
      });
    let C;
    try {
      C = JSON.parse(z);
    } catch {
      return new ku({
        status: o.status,
        code: T,
        message: `${r} ${s} → ${o.status}: ${z.slice(0, 200)}`,
        raw: z
      });
    }
    const U = C.Message ?? C.message, x = C.Errors ?? C.errors, b = Array.isArray(x) ? x.flatMap((j) => {
      const p = j.QuestionId ?? j.questionId, D = j.Message ?? j.message;
      return p && D ? [{ questionId: p, message: D }] : [];
    }) : void 0;
    return new ku({
      status: o.status,
      code: T,
      message: `${r} ${s} → ${o.status}${U ? ": " + U : ""}`,
      serverMessage: U,
      validationErrors: b && b.length > 0 ? b : void 0,
      raw: C
    });
  }
}
const cy = cl.createContext(null), sm = cy.Provider;
function Pl() {
  const c = cl.useContext(cy);
  if (!c)
    throw new Error(
      "useSurveyContext must be used inside <SurveyRenderer>. Question components rely on survey state from the enclosing provider."
    );
  return c;
}
function P(c, o, r) {
  if (c == null) return "";
  if (typeof c == "string") return c;
  if (c[o]) return c[o];
  if (r && c[r]) return c[r];
  const s = Object.keys(c);
  return s.length > 0 ? c[s[0]] : "";
}
const fy = {
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
    yes: "نعم",
    no: "لا"
  }
}, om = { en: fy, ar: rm };
function dm(c, o, r) {
  const s = { ...om, ...r ?? {} };
  return s[c] ?? (o ? s[o] : void 0) ?? s.en ?? fy;
}
const ym = "adp-surveys", vm = 1;
function hm(c = {}) {
  const o = typeof window < "u", r = o && window.parent !== window, s = c.enabled ?? r, T = c.target ?? (o ? window.parent : null), z = c.targetOrigin ?? "*";
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
  const C = (U, x) => {
    const b = {
      source: ym,
      version: vm,
      type: U,
      payload: x
    };
    try {
      T.postMessage(b, z);
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
function Vf(c) {
  return `adp-surveys:resume:${c}`;
}
function mm(c, o) {
  try {
    const r = c.getItem(Vf(o));
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
    c.setItem(Vf(o), JSON.stringify(s));
  } catch {
  }
}
function bm(c, o) {
  try {
    c.removeItem(Vf(o));
  } catch {
  }
}
function Sm({
  question: c,
  registry: o
}) {
  const { ui: r } = Pl(), s = c.type, T = s ? o[s] : void 0;
  return T ? /* @__PURE__ */ M.jsx(T, { question: c }) : /* @__PURE__ */ M.jsx("div", { className: "survey-question survey-question--unknown", children: /* @__PURE__ */ M.jsxs("em", { children: [
    r.unsupportedQuestion,
    " ",
    String(s ?? "missing")
  ] }) });
}
function pm({ question: c }) {
  const { locale: o, schema: r, answers: s, setAnswer: T } = Pl(), z = c.id, C = c.title, U = c.help, x = !!c.required, b = s[z] ?? "";
  return /* @__PURE__ */ M.jsxs("div", { className: "survey-question survey-question--text", children: [
    /* @__PURE__ */ M.jsxs("label", { className: "survey-question__label", htmlFor: `q-${z}`, children: [
      P(C, o, r.defaultLocale),
      x && /* @__PURE__ */ M.jsx("span", { "aria-label": "required", className: "survey-question__required", children: " *" })
    ] }),
    U && /* @__PURE__ */ M.jsx("p", { className: "survey-question__help", children: P(U, o, r.defaultLocale) }),
    /* @__PURE__ */ M.jsx(
      "input",
      {
        id: `q-${z}`,
        className: "survey-question__input",
        type: "text",
        value: b,
        required: x,
        onChange: (j) => T(z, j.target.value)
      }
    )
  ] });
}
function _m({ question: c }) {
  const { locale: o, schema: r, answers: s, setAnswer: T } = Pl(), z = c.id, C = c.title, U = c.help, x = !!c.required, b = Number(c.min ?? 0), j = Number(c.max ?? 10), p = c.lowLabel, D = c.highLabel, X = s[z], k = [];
  for (let K = b; K <= j; K++) k.push(K);
  return /* @__PURE__ */ M.jsxs("fieldset", { className: "survey-question survey-question--nps", children: [
    /* @__PURE__ */ M.jsxs("legend", { className: "survey-question__label", children: [
      P(C, o, r.defaultLocale),
      x && /* @__PURE__ */ M.jsx("span", { "aria-label": "required", className: "survey-question__required", children: " *" })
    ] }),
    U && /* @__PURE__ */ M.jsx("p", { className: "survey-question__help", children: P(U, o, r.defaultLocale) }),
    /* @__PURE__ */ M.jsx("div", { className: "survey-question__nps-scale", role: "radiogroup", children: k.map((K) => {
      const L = X === K;
      return /* @__PURE__ */ M.jsx(
        "button",
        {
          type: "button",
          role: "radio",
          "aria-checked": L,
          className: "survey-question__nps-step" + (L ? " survey-question__nps-step--selected" : ""),
          onClick: () => T(z, K),
          children: K
        },
        K
      );
    }) }),
    (p || D) && /* @__PURE__ */ M.jsxs("div", { className: "survey-question__nps-labels", children: [
      /* @__PURE__ */ M.jsx("span", { children: p ? P(p, o, r.defaultLocale) : "" }),
      /* @__PURE__ */ M.jsx("span", { children: D ? P(D, o, r.defaultLocale) : "" })
    ] })
  ] });
}
function Em({ question: c }) {
  const { locale: o, schema: r } = Pl(), s = c.id, T = c.title, z = c.help, C = c.options ?? [], U = (x, b) => {
    const j = {
      questionId: s,
      option: {
        id: b.id,
        nextScreen: b.nextScreen
      }
    };
    x.currentTarget.dispatchEvent(
      new CustomEvent("survey:navigationListSelect", {
        detail: j,
        bubbles: !0
      })
    );
  };
  return /* @__PURE__ */ M.jsxs("div", { className: "survey-question survey-question--navlist", children: [
    /* @__PURE__ */ M.jsx("div", { className: "survey-question__label", children: P(T, o, r.defaultLocale) }),
    z && /* @__PURE__ */ M.jsx("p", { className: "survey-question__help", children: P(z, o, r.defaultLocale) }),
    /* @__PURE__ */ M.jsx("ul", { className: "survey-navlist", role: "radiogroup", "aria-description": "Selecting an option navigates to the next screen.", children: C.map((x) => {
      const b = x.id, j = x.label;
      return /* @__PURE__ */ M.jsx("li", { className: "survey-navlist__row", children: /* @__PURE__ */ M.jsxs(
        "button",
        {
          type: "button",
          className: "survey-navlist__button",
          onClick: (p) => U(p, x),
          children: [
            /* @__PURE__ */ M.jsx("span", { className: "survey-navlist__label", children: P(j, o, r.defaultLocale) }),
            /* @__PURE__ */ M.jsx("span", { "aria-hidden": "true", className: "survey-navlist__chevron", children: "›" })
          ]
        }
      ) }, b);
    }) })
  ] });
}
function zm({ question: c }) {
  const { locale: o, schema: r, answers: s, setAnswer: T } = Pl(), z = c.id, C = c.title, U = c.help, x = c.placeholder, b = !!c.required, j = c.minLength, p = c.maxLength, D = s[z] ?? "";
  return /* @__PURE__ */ M.jsxs("div", { className: "survey-question survey-question--paragraph", children: [
    /* @__PURE__ */ M.jsxs("label", { className: "survey-question__label", htmlFor: `q-${z}`, children: [
      P(C, o, r.defaultLocale),
      b && /* @__PURE__ */ M.jsx("span", { "aria-label": "required", className: "survey-question__required", children: " *" })
    ] }),
    U && /* @__PURE__ */ M.jsx("p", { className: "survey-question__help", children: P(U, o, r.defaultLocale) }),
    /* @__PURE__ */ M.jsx(
      "textarea",
      {
        id: `q-${z}`,
        className: "survey-question__textarea",
        value: D,
        required: b,
        rows: 5,
        minLength: j,
        maxLength: p,
        placeholder: x ? P(x, o, r.defaultLocale) : void 0,
        onChange: (X) => T(z, X.target.value)
      }
    )
  ] });
}
function Am({ question: c }) {
  const { locale: o, schema: r, answers: s, setAnswer: T } = Pl(), z = c.id, C = c.title, U = c.help, x = !!c.required, b = c.min, j = c.max, p = c.step, D = c.unit, X = s[z], k = X == null ? "" : String(X);
  return /* @__PURE__ */ M.jsxs("div", { className: "survey-question survey-question--number", children: [
    /* @__PURE__ */ M.jsxs("label", { className: "survey-question__label", htmlFor: `q-${z}`, children: [
      P(C, o, r.defaultLocale),
      x && /* @__PURE__ */ M.jsx("span", { "aria-label": "required", className: "survey-question__required", children: " *" })
    ] }),
    U && /* @__PURE__ */ M.jsx("p", { className: "survey-question__help", children: P(U, o, r.defaultLocale) }),
    /* @__PURE__ */ M.jsxs("div", { className: "survey-question__number-wrap", children: [
      /* @__PURE__ */ M.jsx(
        "input",
        {
          id: `q-${z}`,
          className: "survey-question__input",
          type: "number",
          value: k,
          required: x,
          min: b,
          max: j,
          step: p,
          onChange: (K) => {
            const L = K.target.value;
            T(z, L === "" ? null : Number(L));
          }
        }
      ),
      D && /* @__PURE__ */ M.jsx("span", { className: "survey-question__unit", children: P(D, o, r.defaultLocale) })
    ] })
  ] });
}
function qm({ question: c }) {
  const { locale: o, schema: r, answers: s, setAnswer: T } = Pl(), z = c.id, C = c.title, U = c.help, x = !!c.required, b = Number(c.max ?? 5), j = s[z], p = [];
  for (let D = 1; D <= b; D++) p.push(D);
  return /* @__PURE__ */ M.jsxs("fieldset", { className: "survey-question survey-question--rating", children: [
    /* @__PURE__ */ M.jsxs("legend", { className: "survey-question__label", children: [
      P(C, o, r.defaultLocale),
      x && /* @__PURE__ */ M.jsx("span", { "aria-label": "required", className: "survey-question__required", children: " *" })
    ] }),
    U && /* @__PURE__ */ M.jsx("p", { className: "survey-question__help", children: P(U, o, r.defaultLocale) }),
    /* @__PURE__ */ M.jsx("div", { className: "survey-question__rating-scale", role: "radiogroup", children: p.map((D) => {
      const X = typeof j == "number" && D <= j;
      return /* @__PURE__ */ M.jsx(
        "button",
        {
          type: "button",
          role: "radio",
          "aria-checked": j === D,
          "aria-label": `${D}`,
          className: "survey-question__rating-star" + (X ? " survey-question__rating-star--selected" : ""),
          onClick: () => T(z, D),
          children: /* @__PURE__ */ M.jsx("span", { "aria-hidden": "true", children: "★" })
        },
        D
      );
    }) })
  ] });
}
function Tm({ question: c }) {
  const { locale: o, schema: r, answers: s, setAnswer: T } = Pl(), z = c.id, C = c.title, U = c.help, x = !!c.required, b = c.options ?? [], j = s[z];
  return /* @__PURE__ */ M.jsxs("fieldset", { className: "survey-question survey-question--single", children: [
    /* @__PURE__ */ M.jsxs("legend", { className: "survey-question__label", children: [
      P(C, o, r.defaultLocale),
      x && /* @__PURE__ */ M.jsx("span", { "aria-label": "required", className: "survey-question__required", children: " *" })
    ] }),
    U && /* @__PURE__ */ M.jsx("p", { className: "survey-question__help", children: P(U, o, r.defaultLocale) }),
    /* @__PURE__ */ M.jsx("div", { className: "survey-question__options", children: b.map((p) => /* @__PURE__ */ M.jsxs("label", { className: "survey-question__option", children: [
      /* @__PURE__ */ M.jsx(
        "input",
        {
          type: "radio",
          name: `q-${z}`,
          value: p.id,
          checked: j === p.id,
          onChange: () => T(z, p.id)
        }
      ),
      /* @__PURE__ */ M.jsx("span", { children: P(p.label, o, r.defaultLocale) })
    ] }, p.id)) })
  ] });
}
function xm({ question: c }) {
  const { locale: o, schema: r, answers: s, setAnswer: T } = Pl(), z = c.id, C = c.title, U = c.help, x = !!c.required, b = c.options ?? [], j = c.maxSelected, p = s[z] ?? [], D = (X) => {
    if (p.includes(X)) {
      T(z, p.filter((k) => k !== X));
      return;
    }
    j !== void 0 && p.length >= j || T(z, [...p, X]);
  };
  return /* @__PURE__ */ M.jsxs("fieldset", { className: "survey-question survey-question--multi", children: [
    /* @__PURE__ */ M.jsxs("legend", { className: "survey-question__label", children: [
      P(C, o, r.defaultLocale),
      x && /* @__PURE__ */ M.jsx("span", { "aria-label": "required", className: "survey-question__required", children: " *" })
    ] }),
    U && /* @__PURE__ */ M.jsx("p", { className: "survey-question__help", children: P(U, o, r.defaultLocale) }),
    /* @__PURE__ */ M.jsx("div", { className: "survey-question__options", children: b.map((X) => {
      const k = p.includes(X.id);
      return /* @__PURE__ */ M.jsxs("label", { className: "survey-question__option", children: [
        /* @__PURE__ */ M.jsx(
          "input",
          {
            type: "checkbox",
            checked: k,
            onChange: () => D(X.id)
          }
        ),
        /* @__PURE__ */ M.jsx("span", { children: P(X.label, o, r.defaultLocale) })
      ] }, X.id);
    }) })
  ] });
}
function Nm({ question: c }) {
  const { locale: o, schema: r, answers: s, setAnswer: T, ui: z } = Pl(), C = c.id, U = c.title, x = c.help, b = !!c.required, j = c.options ?? [], p = c.placeholder, D = s[C] ?? "", X = p ? P(p, o, r.defaultLocale) : z.selectPlaceholder;
  return /* @__PURE__ */ M.jsxs("div", { className: "survey-question survey-question--dropdown", children: [
    /* @__PURE__ */ M.jsxs("label", { className: "survey-question__label", htmlFor: `q-${C}`, children: [
      P(U, o, r.defaultLocale),
      b && /* @__PURE__ */ M.jsx("span", { "aria-label": "required", className: "survey-question__required", children: " *" })
    ] }),
    x && /* @__PURE__ */ M.jsx("p", { className: "survey-question__help", children: P(x, o, r.defaultLocale) }),
    /* @__PURE__ */ M.jsxs(
      "select",
      {
        id: `q-${C}`,
        className: "survey-question__select",
        value: D,
        required: b,
        onChange: (k) => T(C, k.target.value || null),
        children: [
          /* @__PURE__ */ M.jsx("option", { value: "", children: X }),
          j.map((k) => /* @__PURE__ */ M.jsx("option", { value: k.id, children: P(k.label, o, r.defaultLocale) }, k.id))
        ]
      }
    )
  ] });
}
function Om({ question: c }) {
  const { locale: o, schema: r, answers: s, setAnswer: T } = Pl(), z = c.id, C = c.title, U = c.help, x = !!c.required, b = c.minDate, j = c.maxDate, p = s[z] ?? "";
  return /* @__PURE__ */ M.jsxs("div", { className: "survey-question survey-question--date", children: [
    /* @__PURE__ */ M.jsxs("label", { className: "survey-question__label", htmlFor: `q-${z}`, children: [
      P(C, o, r.defaultLocale),
      x && /* @__PURE__ */ M.jsx("span", { "aria-label": "required", className: "survey-question__required", children: " *" })
    ] }),
    U && /* @__PURE__ */ M.jsx("p", { className: "survey-question__help", children: P(U, o, r.defaultLocale) }),
    /* @__PURE__ */ M.jsx(
      "input",
      {
        id: `q-${z}`,
        className: "survey-question__input",
        type: "date",
        value: p,
        required: x,
        min: b,
        max: j,
        onChange: (D) => T(z, D.target.value || null)
      }
    )
  ] });
}
function Mm({ question: c }) {
  const { locale: o, schema: r, answers: s, setAnswer: T } = Pl(), z = c.id, C = c.title, U = c.help, x = !!c.required, b = c.minDateTime, j = c.maxDateTime, p = s[z] ?? "", D = (X) => {
    if (!X) return;
    const k = X.match(/^(\d{4}-\d{2}-\d{2}T\d{2}:\d{2})/);
    return (k == null ? void 0 : k[1]) ?? void 0;
  };
  return /* @__PURE__ */ M.jsxs("div", { className: "survey-question survey-question--datetime", children: [
    /* @__PURE__ */ M.jsxs("label", { className: "survey-question__label", htmlFor: `q-${z}`, children: [
      P(C, o, r.defaultLocale),
      x && /* @__PURE__ */ M.jsx("span", { "aria-label": "required", className: "survey-question__required", children: " *" })
    ] }),
    U && /* @__PURE__ */ M.jsx("p", { className: "survey-question__help", children: P(U, o, r.defaultLocale) }),
    /* @__PURE__ */ M.jsx(
      "input",
      {
        id: `q-${z}`,
        className: "survey-question__input",
        type: "datetime-local",
        value: D(p) ?? "",
        required: x,
        min: D(b),
        max: D(j),
        onChange: (X) => T(z, X.target.value || null)
      }
    )
  ] });
}
function Dm({ question: c }) {
  const { locale: o, schema: r, answers: s, setAnswer: T } = Pl(), z = c.id, C = c.title, U = c.help, x = !!c.required, b = c.acceptedTypes, j = cl.useRef(null), p = s[z], D = b && b.length > 0 ? b.join(",") : void 0;
  return /* @__PURE__ */ M.jsxs("div", { className: "survey-question survey-question--file", children: [
    /* @__PURE__ */ M.jsxs("label", { className: "survey-question__label", htmlFor: `q-${z}`, children: [
      P(C, o, r.defaultLocale),
      x && /* @__PURE__ */ M.jsx("span", { "aria-label": "required", className: "survey-question__required", children: " *" })
    ] }),
    U && /* @__PURE__ */ M.jsx("p", { className: "survey-question__help", children: P(U, o, r.defaultLocale) }),
    /* @__PURE__ */ M.jsx(
      "input",
      {
        ref: j,
        id: `q-${z}`,
        className: "survey-question__file",
        type: "file",
        required: x,
        accept: D,
        onChange: (X) => {
          var k;
          const K = (k = X.target.files) == null ? void 0 : k[0];
          if (!K) {
            T(z, null);
            return;
          }
          T(z, { name: K.name, size: K.size, type: K.type });
        }
      }
    ),
    (p == null ? void 0 : p.name) && /* @__PURE__ */ M.jsxs("p", { className: "survey-question__file-name", children: [
      "Selected: ",
      p.name
    ] })
  ] });
}
const Yf = 480, Gf = 160;
function Um({ question: c }) {
  const { locale: o, schema: r, answers: s, setAnswer: T, ui: z } = Pl(), C = c.id, U = c.title, x = c.help, b = !!c.required, j = cl.useRef(null), [p, D] = cl.useState(!1), [X, k] = cl.useState(!!s[C]), K = () => {
    var W;
    return ((W = j.current) == null ? void 0 : W.getContext("2d")) ?? null;
  }, L = (W) => {
    const ll = W.target.getBoundingClientRect();
    return {
      x: (W.clientX - ll.left) / ll.width * Yf,
      y: (W.clientY - ll.top) / ll.height * Gf
    };
  }, Z = cl.useCallback(() => {
    var W;
    const ll = (W = j.current) == null ? void 0 : W.toDataURL("image/png");
    ll && T(C, ll);
  }, [C, T]), ol = () => {
    const W = K();
    W && (W.clearRect(0, 0, Yf, Gf), k(!1), T(C, null));
  };
  return cl.useEffect(() => {
    const W = K();
    W && (W.lineWidth = 2, W.lineCap = "round", W.strokeStyle = "#111");
  }, []), /* @__PURE__ */ M.jsxs("div", { className: "survey-question survey-question--signature", children: [
    /* @__PURE__ */ M.jsxs("div", { className: "survey-question__label", children: [
      P(U, o, r.defaultLocale),
      b && /* @__PURE__ */ M.jsx("span", { "aria-label": "required", className: "survey-question__required", children: " *" })
    ] }),
    x && /* @__PURE__ */ M.jsx("p", { className: "survey-question__help", children: P(x, o, r.defaultLocale) }),
    /* @__PURE__ */ M.jsx(
      "canvas",
      {
        ref: j,
        className: "survey-question__signature-canvas",
        width: Yf,
        height: Gf,
        role: "img",
        "aria-label": "signature pad",
        onPointerDown: (W) => {
          W.target.setPointerCapture(W.pointerId);
          const ll = K();
          if (!ll) return;
          const { x: Wl, y: I } = L(W);
          ll.beginPath(), ll.moveTo(Wl, I), D(!0);
        },
        onPointerMove: (W) => {
          if (!p) return;
          const ll = K();
          if (!ll) return;
          const { x: Wl, y: I } = L(W);
          ll.lineTo(Wl, I), ll.stroke(), k(!0);
        },
        onPointerUp: () => {
          D(!1), X && Z();
        }
      }
    ),
    /* @__PURE__ */ M.jsx("div", { className: "survey-question__signature-actions", children: /* @__PURE__ */ M.jsx("button", { type: "button", className: "survey-button survey-button--ghost", onClick: ol, children: z.clearSignature }) })
  ] });
}
function Cm({ question: c }) {
  const { locale: o, schema: r, answers: s, setAnswer: T, ui: z } = Pl(), C = c.id, U = c.title, x = c.help, b = !!c.required, j = c.yesLabel, p = c.noLabel, D = s[C], X = j ? P(j, o, r.defaultLocale) : z.yes, k = p ? P(p, o, r.defaultLocale) : z.no;
  return /* @__PURE__ */ M.jsxs("fieldset", { className: "survey-question survey-question--yesno", children: [
    /* @__PURE__ */ M.jsxs("legend", { className: "survey-question__label", children: [
      P(U, o, r.defaultLocale),
      b && /* @__PURE__ */ M.jsx("span", { "aria-label": "required", className: "survey-question__required", children: " *" })
    ] }),
    x && /* @__PURE__ */ M.jsx("p", { className: "survey-question__help", children: P(x, o, r.defaultLocale) }),
    /* @__PURE__ */ M.jsxs("div", { className: "survey-question__yesno", role: "radiogroup", children: [
      /* @__PURE__ */ M.jsx(
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
      /* @__PURE__ */ M.jsx(
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
  number: Am,
  rating: qm,
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
  onCompleted: z,
  registry: C,
  submissionMeta: U,
  uiLocales: x,
  resumeKey: b,
  storage: j,
  emitHostMessages: p,
  hostMessageOrigin: D,
  hostMessageTarget: X
}) {
  var k;
  const K = s ?? c.defaultLocale ?? "en", L = C ?? jm, Z = cl.useMemo(
    () => dm(K, c.defaultLocale, x),
    [K, c.defaultLocale, x]
  ), ol = j ?? (typeof globalThis < "u" ? globalThis.localStorage : void 0), W = cl.useMemo(() => {
    var G;
    if (!b || !ol) return null;
    const Tl = mm(ol, b);
    return Tl ? Tl.currentScreenId === null || c.screens.some((Et) => Et.id === Tl.currentScreenId) ? Tl : { ...Tl, currentScreenId: ((G = c.screens[0]) == null ? void 0 : G.id) ?? null } : null;
  }, []), [ll, Wl] = cl.useState(() => ({
    ...r ?? {},
    ...(W == null ? void 0 : W.answers) ?? {}
  })), [I, al] = cl.useState(
    () => {
      var G;
      return (W == null ? void 0 : W.currentScreenId) ?? ((G = c.screens[0]) == null ? void 0 : G.id) ?? null;
    }
  ), [Yl, ft] = cl.useState(!1), [_t, st] = cl.useState(null), [Nl, Jt] = cl.useState(!1), jt = cl.useRef((/* @__PURE__ */ new Date()).toISOString()), Gl = cl.useRef(null);
  if (Gl.current === null) {
    const G = {};
    X !== void 0 && (G.target = X), D !== void 0 && (G.targetOrigin = D), p !== void 0 && (G.enabled = p), Gl.current = hm(G);
  }
  const _ = cl.useMemo(
    () => I ? c.screens.find((G) => G.id === I) ?? null : null,
    [c, I]
  );
  cl.useEffect(() => {
    var G;
    T == null || T(I), (G = Gl.current) == null || G.screenChanged(I);
  }, [I, T]);
  const H = cl.useRef(!1);
  cl.useEffect(() => {
    var G;
    H.current || !I || (H.current = !0, (G = Gl.current) == null || G.loaded());
  }, [I]), cl.useEffect(() => {
    !b || !ol || Nl || gm(ol, b, {
      answers: ll,
      currentScreenId: I,
      schemaVersion: c.version
    });
  }, [ll, I, b, ol, Nl, c.version]), cl.useEffect(() => {
    Nl && b && ol && bm(ol, b);
  }, [Nl, b, ol]), cl.useEffect(() => {
    var G;
    _t && ((G = Gl.current) == null || G.error(_t));
  }, [_t]);
  const J = cl.useCallback((G, Tl) => {
    Wl((Et) => ({ ...Et, [G]: Tl }));
  }, []), yl = cl.useCallback(
    (G) => {
      G !== null && al(G);
    },
    []
  ), dl = cl.useCallback(async () => {
    var G;
    ft(!0), st(null);
    try {
      await o({
        schemaVersion: c.version ?? 0,
        answers: ll,
        meta: {
          startedAt: (U == null ? void 0 : U.startedAt) ?? jt.current,
          completedAt: (U == null ? void 0 : U.completedAt) ?? (/* @__PURE__ */ new Date()).toISOString(),
          ...U ?? {}
        }
      }), Jt(!0), z == null || z(I), (G = Gl.current) == null || G.completed({ screenId: I, answers: ll });
    } catch (Tl) {
      st(Tl.message ?? String(Tl));
    } finally {
      ft(!1);
    }
  }, [c.version, ll, U, o, z, I]), v = cl.useCallback(() => {
    if (!I) return;
    const G = Qf(c, I, ll);
    G.kind === "end" ? dl() : yl(G.screenId);
  }, [c, I, ll, yl, dl]), O = cl.useRef(null);
  cl.useEffect(() => {
    Nl || Yl || !I || !_ || O.current === I || !(!_.questions || _.questions.length === 0) || Qf(c, I, ll).kind === "end" && (O.current = I, dl());
  }, [I, _, Nl, Yl, c, ll, dl]);
  const R = cl.useRef(null);
  cl.useEffect(() => {
    const G = R.current;
    if (!G || typeof ResizeObserver > "u") return;
    const Tl = new ResizeObserver((Et) => {
      var rt;
      const Xe = Et[0];
      Xe && ((rt = Gl.current) == null || rt.resize(Math.ceil(Xe.contentRect.height)));
    });
    return Tl.observe(G), () => Tl.disconnect();
  }, []), cl.useEffect(() => {
    const G = R.current;
    if (!G) return;
    const Tl = (Et) => {
      const rt = Et.detail;
      if (!rt || !I) return;
      J(rt.questionId, rt.option.id);
      const Xe = { ...ll, [rt.questionId]: rt.option.id }, Lt = fm(
        rt.option,
        c,
        I,
        Xe
      );
      Lt.kind === "end" ? dl() : yl(Lt.screenId);
    };
    return G.addEventListener("survey:navigationListSelect", Tl), () => G.removeEventListener("survey:navigationListSelect", Tl);
  }, [ll, I, c, J, yl, dl]);
  const Y = cl.useMemo(
    () => ({
      schema: c,
      locale: K,
      direction: Z.direction,
      ui: Z.strings,
      answers: ll,
      setAnswer: J
    }),
    [c, K, Z, ll, J]
  );
  if (Nl)
    return /* @__PURE__ */ M.jsx(
      "div",
      {
        ref: R,
        className: "survey-root survey-root--done",
        dir: Z.direction,
        lang: K,
        children: /* @__PURE__ */ M.jsxs("div", { className: "survey-screen", children: [
          /* @__PURE__ */ M.jsx("h2", { className: "survey-screen__title", children: _ != null && _.title ? P(_.title, K, c.defaultLocale) : Z.strings.thankYou }),
          (_ == null ? void 0 : _.description) && /* @__PURE__ */ M.jsx("p", { className: "survey-screen__description", children: P(_.description, K, c.defaultLocale) })
        ] })
      }
    );
  if (!_)
    return /* @__PURE__ */ M.jsx("div", { ref: R, className: "survey-root", dir: Z.direction, lang: K, children: /* @__PURE__ */ M.jsx("div", { className: "survey-screen", children: /* @__PURE__ */ M.jsx("em", { children: Z.strings.noScreens }) }) });
  const $ = _.questions ?? [], ul = $.length > 0 && ((k = $[$.length - 1]) == null ? void 0 : k.type) === "navigationList", vl = $.length === 0 && !_.nextScreen, Vl = !ul && !vl;
  return /* @__PURE__ */ M.jsx(sm, { value: Y, children: /* @__PURE__ */ M.jsx("div", { ref: R, className: "survey-root", dir: Z.direction, lang: K, children: /* @__PURE__ */ M.jsxs("div", { className: "survey-screen", children: [
    _.title && /* @__PURE__ */ M.jsx("h2", { className: "survey-screen__title", children: P(_.title, K, c.defaultLocale) }),
    _.description && /* @__PURE__ */ M.jsx("p", { className: "survey-screen__description", children: P(_.description, K, c.defaultLocale) }),
    /* @__PURE__ */ M.jsx("div", { className: "survey-screen__questions", children: $.map((G, Tl) => /* @__PURE__ */ M.jsx(Sm, { question: G, registry: L }, G.id ?? Tl)) }),
    Vl && /* @__PURE__ */ M.jsx("div", { className: "survey-screen__actions", children: /* @__PURE__ */ M.jsx(
      "button",
      {
        type: "button",
        className: "survey-button survey-button--primary",
        disabled: Yl,
        onClick: v,
        children: Yl ? Z.strings.submitting : Z.strings.next
      }
    ) }),
    _t && /* @__PURE__ */ M.jsxs("p", { className: "survey-screen__error", role: "alert", children: [
      Z.strings.couldNotSubmit,
      " ",
      _t
    ] })
  ] }) }) });
}
const Hm = ".survey-root{font-family:system-ui,-apple-system,Segoe UI,Roboto,sans-serif;color:#111;max-width:640px;margin:0 auto;padding:32px 16px}.survey-screen{display:flex;flex-direction:column;gap:24px}.survey-screen__title{font-size:1.5rem;font-weight:600;margin:0}.survey-screen__description{color:#555;margin:0}.survey-screen__questions{display:flex;flex-direction:column;gap:24px}.survey-screen__actions{display:flex;justify-content:flex-end}.survey-screen__error{color:#b42318;background:#fef3f2;border:1px solid #fecdca;padding:12px 14px;border-radius:8px;margin:0}.survey-question{display:flex;flex-direction:column;gap:8px}.survey-question__label{font-weight:600;display:block}.survey-question__required{color:#b42318}.survey-question__help{margin:0;color:#666;font-size:.9rem}.survey-question__input{padding:10px 12px;border:1px solid #d0d5dd;border-radius:8px;font:inherit}.survey-question__input:focus-visible{outline:2px solid #2563eb;outline-offset:1px;border-color:#2563eb}.survey-question--nps{border:none;padding:0;margin:0;display:flex;flex-direction:column;gap:8px}.survey-question__nps-scale{display:flex;gap:6px;flex-wrap:wrap}.survey-question__nps-step{min-width:40px;min-height:40px;padding:8px;border:1px solid #d0d5dd;border-radius:8px;background:#fff;font-weight:500;cursor:pointer}.survey-question__nps-step:hover{background:#f5f7fa}.survey-question__nps-step--selected{background:#2563eb;border-color:#2563eb;color:#fff}.survey-question__nps-labels{display:flex;justify-content:space-between;color:#555;font-size:.85rem}.survey-question--navlist{gap:12px}.survey-navlist{list-style:none;padding:0;margin:0;display:flex;flex-direction:column;gap:8px}.survey-navlist__row{margin:0}.survey-navlist__button{width:100%;display:flex;align-items:center;justify-content:space-between;padding:14px 16px;border:1px solid #d0d5dd;border-radius:10px;background:#fff;cursor:pointer;font:inherit;text-align:start}.survey-navlist__button:hover{background:#f5f7fa;border-color:#2563eb}.survey-navlist__chevron{font-size:1.5rem;color:#667085}.survey-root[dir=rtl] .survey-navlist__chevron{transform:scaleX(-1)}.survey-navlist__label{font-weight:500}.survey-question__textarea{padding:10px 12px;border:1px solid #d0d5dd;border-radius:8px;font:inherit;resize:vertical;min-height:96px}.survey-question__textarea:focus-visible{outline:2px solid #2563eb;outline-offset:1px;border-color:#2563eb}.survey-question__number-wrap{display:flex;align-items:center;gap:8px}.survey-question__number-wrap .survey-question__input{flex:1}.survey-question__unit{color:#555;font-size:.9rem}.survey-question__rating-scale{display:flex;gap:4px}.survey-question__rating-star{background:transparent;border:none;cursor:pointer;font-size:1.8rem;line-height:1;color:#d0d5dd;padding:4px}.survey-question__rating-star:hover,.survey-question__rating-star--selected{color:#f5b60c}.survey-question__options{display:flex;flex-direction:column;gap:8px}.survey-question__option{display:flex;align-items:center;gap:8px;padding:8px 12px;border:1px solid #d0d5dd;border-radius:8px;cursor:pointer}.survey-question__option:hover{background:#f5f7fa;border-color:#2563eb}.survey-question__option input{margin:0}.survey-question__select{padding:10px 12px;border:1px solid #d0d5dd;border-radius:8px;font:inherit;background:#fff}.survey-question__yesno{display:flex;gap:12px}.survey-question__yesno-button{flex:1;padding:14px 16px;border:1px solid #d0d5dd;border-radius:10px;background:#fff;font:inherit;font-weight:500;cursor:pointer}.survey-question__yesno-button:hover{background:#f5f7fa;border-color:#2563eb}.survey-question__yesno-button--selected{background:#2563eb;border-color:#2563eb;color:#fff}.survey-question__file{font:inherit}.survey-question__file-name{color:#555;font-size:.9rem;margin:0}.survey-question__signature-canvas{width:100%;max-width:480px;height:auto;aspect-ratio:3 / 1;border:1px dashed #d0d5dd;border-radius:8px;background:#fff;touch-action:none}.survey-question__signature-actions{display:flex;justify-content:flex-start;gap:8px}.survey-button{padding:10px 20px;border-radius:8px;border:1px solid transparent;cursor:pointer;font:inherit;font-weight:600}.survey-button--primary{background:#2563eb;color:#fff}.survey-button--primary:hover{background:#1e40af}.survey-button--ghost{background:#fff;color:#555;border-color:#d0d5dd}.survey-button--ghost:hover{background:#f5f7fa}.survey-button:disabled{opacity:.5;cursor:not-allowed}";
var Ge, Fu, Gt, Le, Iu, fu, Zl, Xf, cu, wa, $u;
class Bm extends HTMLElement {
  constructor() {
    super();
    de(this, Zl);
    /** Schema-mode setter. Assigning this swaps the element into schema mode and
     *  re-renders with the new schema immediately. */
    de(this, Ge, null);
    /** Schema-mode submit handler. In API mode the element manages this itself. */
    de(this, Fu, null);
    de(this, Gt, null);
    de(this, Le, null);
    de(this, Iu, null);
    de(this, fu, null);
    de(this, wa, !1);
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
      Cl(this, Le) || (Yt(this, Le, document.createElement("div")), Cl(this, Le).className = "shift-survey-mount", this.shadowRoot.appendChild(Cl(this, Le))), Cl(this, Gt) || Yt(this, Gt, Qh.createRoot(Cl(this, Le))), ct(this, Zl, cu).call(this), ct(this, Zl, Xf).call(this);
    }
  }
  disconnectedCallback() {
    queueMicrotask(() => {
      var r;
      if (!(this.isConnected || typeof window > "u")) {
        try {
          (r = Cl(this, Gt)) == null || r.unmount();
        } catch {
        }
        Yt(this, Gt, null);
      }
    });
  }
  attributeChangedCallback(r, s, T) {
    s !== T && ((r === "instance-id" || r === "api-base") && (Yt(this, Iu, null), Yt(this, fu, null), ct(this, Zl, Xf).call(this)), ct(this, Zl, cu).call(this));
  }
  // ─── Properties ──────────────────────────────────────────────────────────
  get schema() {
    return Cl(this, Ge);
  }
  set schema(r) {
    Yt(this, Ge, r), ct(this, Zl, cu).call(this);
  }
  get onSubmit() {
    return Cl(this, Fu);
  }
  set onSubmit(r) {
    Yt(this, Fu, r), ct(this, Zl, cu).call(this);
  }
}
Ge = new WeakMap(), Fu = new WeakMap(), Gt = new WeakMap(), Le = new WeakMap(), Iu = new WeakMap(), fu = new WeakMap(), Zl = new WeakSet(), // ─── Internals ───────────────────────────────────────────────────────────
Xf = function() {
  if (Cl(this, Ge)) return;
  const r = this.getAttribute("instance-id");
  if (!r) return;
  const s = this.getAttribute("api-base");
  if (!s) return;
  new ny({ baseUrl: s }).fetchSchema(r).then((z) => {
    Yt(this, Iu, z), ct(this, Zl, cu).call(this);
  }).catch((z) => {
    Yt(this, fu, z), ct(this, Zl, $u).call(this, "survey:error", { message: z.message }), ct(this, Zl, cu).call(this);
  });
}, cu = function() {
  if (!Cl(this, Gt)) return;
  const r = this.getAttribute("api-base"), s = this.getAttribute("instance-id"), T = this.getAttribute("locale") ?? void 0, z = this.getAttribute("mode") === "agent", C = Cl(this, Ge) ?? Cl(this, Iu);
  if (Cl(this, fu) && !C) {
    Cl(this, Gt).render(
      cl.createElement(
        "div",
        { className: "shift-survey-error", role: "alert" },
        Cl(this, fu).message
      )
    );
    return;
  }
  if (!C) {
    Cl(this, Gt).render(
      cl.createElement("div", { className: "shift-survey-loading" }, "Loading…")
    );
    return;
  }
  const U = Cl(this, Ge) ? Cl(this, Fu) ?? ((x) => {
    ct(this, Zl, $u).call(this, "survey:completed", { ...x });
  }) : async (x) => {
    if (!r || !s)
      throw new Error("shift-survey: API mode requires both instance-id and api-base attributes.");
    await new ny({ baseUrl: r }).submitResponse(s, x);
  };
  Cl(this, Gt).render(
    cl.createElement(Rm, {
      schema: C,
      onSubmit: U,
      ...T ? { locale: T } : {},
      // Let the element be the resume key in API mode so two surveys on the
      // same host page don't clobber each other.
      ...s ? { resumeKey: s } : {},
      ...z ? { submissionMeta: { mode: "agent" } } : {},
      // CustomEvents are the web-component's channel; postMessage stays opt-in
      // via iframe auto-detect on the enclosing page (unchanged).
      onScreenChange: (x) => ct(this, Zl, $u).call(this, "survey:screen-changed", { screenId: x }),
      onCompleted: (x) => ct(this, Zl, $u).call(this, "survey:completed", { screenId: x })
    })
  ), Cl(this, wa) || (Yt(this, wa, !0), ct(this, Zl, $u).call(this, "survey:loaded", {}));
}, wa = new WeakMap(), $u = function(r, s) {
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
