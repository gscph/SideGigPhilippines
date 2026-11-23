// The landing page's few browser helpers (PageScript.cs calls them). Storage can be blocked (private tabs, the
// Facebook in-app browser), so every read and write is wrapped: the page works without it.

export function read(key) {
    try { return localStorage.getItem(key); } catch { return null; }
}

export function write(key, value) {
    try {
        if (value === null || value === undefined) localStorage.removeItem(key);
        else localStorage.setItem(key, value);
    } catch { /* storage blocked: nothing to remember */ }
}

export function browserLanguage() {
    return (navigator.languages && navigator.languages[0]) || navigator.language || "";
}

export function setHtmlLang(lang) {
    document.documentElement.lang = lang === "tl" ? "fil" : "en";
}

export function isOnline() {
    return navigator.onLine !== false;
}

// Scrolls a section into view. The form is centred so the button stays above a phone's keyboard; the field gets
// focus only on wide screens (opening the keyboard by surprise hides the card on a phone).
export function scrollToSection(id, focusSelector) {
    const target = document.getElementById(id);
    if (!target) return;
    const center = !!focusSelector;
    target.scrollIntoView({ behavior: "smooth", block: center ? "center" : "start" });
    if (focusSelector && window.matchMedia("(min-width: 960px)").matches) {
        const field = target.querySelector(focusSelector);
        if (field) setTimeout(() => field.focus({ preventScroll: true }), 400);
    }
}

export function scrollToTop() {
    window.scrollTo({ top: 0, behavior: "smooth" });
}

// Tells .NET whether a section is on screen (the header hides its "Early access" button while the form shows).
const watchers = new Map();
export function watchSection(id, dotnet) {
    const target = document.getElementById(id);
    if (!target || !("IntersectionObserver" in window)) return;
    const observer = new IntersectionObserver(entries => {
        for (const e of entries) dotnet.invokeMethodAsync("SectionVisible", e.isIntersecting);
    }, { threshold: 0.15 });
    observer.observe(target);
    watchers.set(id, observer);
}

export function unwatchSection(id) {
    const observer = watchers.get(id);
    if (observer) { observer.disconnect(); watchers.delete(id); }
}

// The phone's share sheet; without it the link is copied. Returns "shared", "copied" or "none".
export async function share(title, text, url) {
    try {
        if (navigator.share) { await navigator.share({ title, text, url }); return "shared"; }
    } catch (e) {
        if (e && e.name === "AbortError") return "none";
    }
    try { await navigator.clipboard.writeText(url); return "copied"; } catch { return "none"; }
}
