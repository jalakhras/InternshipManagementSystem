/**
 * Reduces authored HTML to the small set of tags a question may carry.
 *
 * An allowlist, never a blocklist. Blocklists are a losing game — every listing
 * of "dangerous tags" has been defeated by a tag its author had not heard of —
 * and the set a question actually needs is short enough to write down.
 *
 * This is the browser-side pass. The server runs its own before storing, because
 * anything relying on client code to be safe is not safe: a request can be made
 * without one.
 */

/** Tags a question may contain. Everything else is unwrapped, keeping its text. */
const ALLOWED_TAGS = new Set([
  'B', 'STRONG', 'I', 'EM', 'U',
  'UL', 'OL', 'LI',
  'P', 'BR', 'DIV',
  'CODE', 'PRE',
  'SUB', 'SUP',
  'SPAN',
]);

/**
 * Attributes that may survive. Deliberately almost nothing: `style` alone can
 * hide text, cover the page, or load a remote image that reports who opened the
 * question and when.
 */
const ALLOWED_ATTRIBUTES = new Set(['dir']);

/**
 * Tags dropped whole, contents and all.
 * <p>
 * Unwrapping these would keep their text, and the body of a script tag rendered
 * as visible text is not dangerous but is nonsense sitting in the middle of a
 * question. Nothing inside them is ever content.
 * </p>
 */
const DISCARDED_TAGS = new Set(['SCRIPT', 'STYLE', 'IFRAME', 'OBJECT', 'EMBED', 'TEMPLATE', 'NOSCRIPT']);

export function sanitiseRichText(html: string): string {
  if (!html) {
    return '';
  }

  // Parsed into an inert document rather than a live one: assigning to innerHTML
  // of an attached element would run whatever it contains before we could look.
  const doc = new DOMParser().parseFromString(`<body>${html}</body>`, 'text/html');

  clean(doc.body);

  const result = doc.body.innerHTML.trim();

  // contenteditable leaves this behind on an emptied field, and it would count as
  // content everywhere that checks whether a question has text.
  return result === '<br>' ? '' : result;
}

function clean(node: Element): void {
  // Backwards, because unwrapping a child rewrites the collection underneath.
  for (let i = node.children.length - 1; i >= 0; i--) {
    const child = node.children[i];

    if (DISCARDED_TAGS.has(child.tagName)) {
      child.remove();
      continue;
    }

    clean(child);

    if (!ALLOWED_TAGS.has(child.tagName)) {
      unwrap(child);
      continue;
    }

    for (const attribute of [...child.attributes]) {
      if (!ALLOWED_ATTRIBUTES.has(attribute.name.toLowerCase())) {
        child.removeAttribute(attribute.name);
      }
    }
  }
}

/** Replaces an element with its children, so its text survives but the tag does not. */
function unwrap(element: Element): void {
  const parent = element.parentNode;

  if (!parent) {
    element.remove();
    return;
  }

  while (element.firstChild) {
    parent.insertBefore(element.firstChild, element);
  }

  parent.removeChild(element);
}
