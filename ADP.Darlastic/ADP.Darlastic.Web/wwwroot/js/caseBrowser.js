// Two things the case browser page needs from the host that frames it, neither of which Blazor can
// do on its own: hand a renewed token to a document in an iframe, and size that iframe to the space
// the host's layout actually left it.

// ---------------------------------------------------------------- token renewal

// The case browser is a plain HTML page served by ADP.Darlastic.API, so it has no auth stack and no
// way to refresh anything. The Blazor page that framed it does hold a real session, so it mints a
// fresh token before the old one expires and posts it in. Reloading the frame would work too, and
// would throw away whatever case the steward was reading — which is the whole reason this exists.
export function postToken(iframeId, token, expires, actor) {
    const frame = document.getElementById(iframeId);
    if (!frame || !frame.contentWindow) return false;

    // Target the frame's OWN origin, not ours. They are the same wherever the SPA is served by its
    // API host (every Darlastic host today), but a host that serves the SPA separately — a static
    // site in front of an API on another domain — would otherwise have every renewal silently
    // dropped by postMessage, and the page would just stop working an hour in.
    let target;
    try {
        target = new URL(frame.src, window.location.href).origin;
    } catch {
        return false;
    }

    frame.contentWindow.postMessage({ type: "darlastic-token", token, expires, actor }, target);
    return true;
}

// ---------------------------------------------------------------- sizing

const fitted = new Map();

// Fill the viewport from wherever the host's chrome leaves off, and cancel the vertical gutter the
// content container puts around every page — 24px of MudContainer padding costs a whole case row on
// a laptop, and an embedded full-bleed tool is the one page that should not pay it.
//
// All of it measured rather than assumed. This component ships in a package: the app bar height, the
// container padding and the breakpoint that changes it belong to whichever host mounts it, so a
// hard-coded `calc(100vh - 64px)` would be right in the host it was written against and quietly
// wrong — an inner scrollbar, or a frame tucked under the app bar — in the next one.
//
// Only the vertical gutter is cancelled. Horizontal padding varies with the breakpoint, and a
// negative inline margin guessing at it would overflow the OUTER page: trading an inner scrollbar
// for a worse one.
export function fitToViewport(containerId) {
    releaseViewport(containerId);

    const el = document.getElementById(containerId);
    if (!el) return false;

    const apply = () => {
        const parent = el.parentElement;
        if (parent) {
            const cs = window.getComputedStyle(parent);
            el.style.marginTop = -(parseFloat(cs.paddingTop) || 0) + "px";
            el.style.marginBottom = -(parseFloat(cs.paddingBottom) || 0) + "px";
        }
        // Measured AFTER the margins land, so the reclaimed gutter is already reflected.
        const top = el.getBoundingClientRect().top;
        el.style.height = Math.max(320, window.innerHeight - top) + "px";
    };

    apply();
    window.addEventListener("resize", apply);
    fitted.set(containerId, apply);
    return true;
}

export function releaseViewport(containerId) {
    const apply = fitted.get(containerId);
    if (apply) {
        window.removeEventListener("resize", apply);
        fitted.delete(containerId);
    }
}
