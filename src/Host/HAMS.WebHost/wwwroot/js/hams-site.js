// Shared vanilla-JS behavior for every Razor Pages page in the frontend migration
// (see the frontend migration plan) - deliberately no framework/library beyond Bootstrap's own
// bundled JS. Two responsibilities: auto-show the post-redirect-get flash message toast, and the
// cascading-<select> pattern that replaces MudSelect's ValueChanged-triggered reload used on
// nearly every admin/staff page (School -> Academic Year -> Grade -> Class, etc.).

document.addEventListener("DOMContentLoaded", () => {
    document.querySelectorAll("[data-hams-autotoast]").forEach((el) => {
        new bootstrap.Toast(el).show();
    });

    initCascadingSelects(document);
});

/**
 * Wires every <select data-hams-cascade-target="..."> in `root` so that changing it fetches
 * fresh <option>s for its dependent select(s) from a JSON endpoint, matching the shape:
 *   [{ "value": "...", "text": "..." }, ...]
 * Required data attributes on the SOURCE select:
 *   data-hams-cascade-target   id of the <select> to repopulate
 *   data-hams-cascade-url      URL to fetch (a Razor Pages handler, e.g. ?handler=AcademicYears)
 *   data-hams-cascade-param    query string parameter name to send the source's value as
 * Optional on the TARGET select:
 *   data-hams-placeholder      placeholder option text (defaults to "(Select an option)")
 * Chained cascades (A -> B -> C) work automatically: repopulating B dispatches a "change" event
 * on B, which triggers B's own cascade into C if B has one configured.
 */
function initCascadingSelects(root) {
    root.querySelectorAll("select[data-hams-cascade-target]").forEach((source) => {
        source.addEventListener("change", () => onCascadeSourceChanged(source));
    });
}

async function onCascadeSourceChanged(source) {
    const targetId = source.dataset.hamsCascadeTarget;
    const target = document.getElementById(targetId);
    if (!target) return;

    const url = source.dataset.hamsCascadeUrl;
    const param = source.dataset.hamsCascadeParam;
    const placeholder = target.dataset.hamsPlaceholder || "(Select an option)";

    // Clear and disable every select downstream of the one that just changed - matches the
    // MudBlazor pages' own ValueChanged handlers, which always reset dependent selections rather
    // than leaving a stale, now-invalid choice selected.
    resetCascadeChain(target, placeholder);

    if (!source.value) {
        return;
    }

    target.disabled = true;
    try {
        const fetchUrl = new URL(url, window.location.origin);
        fetchUrl.searchParams.set(param, source.value);
        const response = await fetch(fetchUrl.toString(), { headers: { Accept: "application/json" } });
        if (!response.ok) return;

        const options = await response.json();
        populateSelect(target, options, placeholder);
    } finally {
        target.disabled = false;
    }
}

function populateSelect(select, options, placeholder) {
    select.innerHTML = "";
    select.appendChild(new Option(placeholder, ""));
    for (const option of options) {
        select.appendChild(new Option(option.text, option.value));
    }
}

function resetCascadeChain(select, placeholder) {
    select.innerHTML = "";
    select.appendChild(new Option(placeholder, ""));

    const nextId = select.dataset.hamsCascadeTarget;
    if (nextId) {
        const next = document.getElementById(nextId);
        if (next) {
            resetCascadeChain(next, next.dataset.hamsPlaceholder || "(Select an option)");
        }
    }
}
