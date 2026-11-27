#!/usr/bin/env python3
"""Post-process the generated Doxygen HTML so the reference docs are indexable.

Doxygen output is hard for search engines to handle correctly:

* there is no <link rel="canonical">, even though every release is published to
  both a versioned directory (v1.2.3/) and latest/ with identical content, so
  Google reports "Duplicate without user-selected canonical";
* <title> is not unique - every *-members.html page is titled "Member List";
* there is no <meta name="description">;
* the page heading is a <div>, so no page has an <h1>.

This script rewrites the generated HTML in place and writes a sitemap.xml that
lists the canonical (latest/) URL of every indexable page.

Usage:
    python3 .doxygen/seo-postprocess.py \\
        --html-dir docs/html \\
        --base-url https://ref.lootlocker.com/unity \\
        --project "LootLocker Unity SDK"

Pass --noindex to mark the tree as non-indexable instead. That is used for
branch preview deployments, which duplicate latest/.
"""

import argparse
import html
import os
import re
import sys

HEADERTITLE_RE = re.compile(
    r'<div class="headertitle">\s*<div class="title">(.*?)</div>\s*</div>',
    re.DOTALL,
)
TITLE_RE = re.compile(r"<title>(.*?)</title>", re.DOTALL)
CONTENTS_RE = re.compile(r'<div class="contents">', re.IGNORECASE)
PARAGRAPH_RE = re.compile(r"<p>(.*?)</p>", re.DOTALL)
TEXTBLOCK_RE = re.compile(r'<div class="textblock">(.*?)</div>', re.DOTALL)
TAG_RE = re.compile(r"<[^>]+>")
NAVPATH_RE = re.compile(r'<div id="nav-path"[^>]*>(.*?)</div>', re.DOTALL)
NAVELEM_RE = re.compile(r'<li class="navelem">(.*?)</li>', re.DOTALL)
CANONICAL_RE = re.compile(r'\n?<link rel="canonical"[^>]*>', re.IGNORECASE)
DESCRIPTION_RE = re.compile(r'\n?<meta name="description"[^>]*>', re.IGNORECASE)
ROBOTS_RE = re.compile(r'\n?<meta name="robots"[^>]*>', re.IGNORECASE)

# Doxygen's crawler helper is a link dump with no content of its own.
NOINDEX_PAGES = {"doxygen_crawl.html"}

# Doxygen member-index suffixes, e.g. functions_prop_b.html -> "Properties".
TYPE_LABELS = {
    "func": "Functions",
    "prop": "Properties",
    "vars": "Variables",
    "enum": "Enums",
    "eval": "Enumerators",
    "type": "Types",
    "related": "Related",
    "attrib": "Attributes",
    "defs": "Defines",
    "typedef": "Typedefs",
    "typedefs": "Typedefs",
    "union": "Unions",
    "struct": "Structs",
    "class": "Classes",
    "interface": "Interfaces",
    "friend": "Friends",
    "signal": "Signals",
    "slot": "Slots",
}

MAX_DESCRIPTION = 155
SEPARATOR = " \u2013 "


def text_of(fragment):
    """Strip markup and entities from an HTML fragment."""
    return re.sub(r"\s+", " ", html.unescape(TAG_RE.sub(" ", fragment))).strip()


def page_heading(doc):
    match = HEADERTITLE_RE.search(doc)
    return text_of(match.group(1)) if match else ""


def breadcrumb(doc):
    """Return the nav-path trail, e.g. ['LootLockerSDK', 'Source', 'LootLockerSDK']."""
    match = NAVPATH_RE.search(doc)
    if not match:
        return []
    return [text_of(item) for item in NAVELEM_RE.findall(match.group(1))]


def title_suffix(filename, base):
    """Describe the letter/type suffix of a Doxygen member-index page."""
    parts = filename[: -len(".html")].split("_")
    if len(parts) < 2:
        return ""
    last = parts[-1]
    if len(last) == 1 and last.isalpha():
        suffix = SEPARATOR + last.upper()
    elif last == "~":
        # Doxygen's bucket for members that do not start with a letter.
        suffix = SEPARATOR + "Symbols"
    else:
        label = TYPE_LABELS.get(last)
        if not label or label.lower() in base.lower():
            return ""
        suffix = SEPARATOR + label
    return "" if base.endswith(suffix) else suffix


def derived_heading(doc, filename, project):
    """Build a heading for pages Doxygen renders without a headertitle."""
    match = TITLE_RE.search(doc)
    base = text_of(match.group(1)) if match else project
    prefix = f"{project}: "
    if base.startswith(prefix):
        base = base[len(prefix):]
    return base + title_suffix(filename, base)


def disambiguate(heading, doc):
    """Qualify directory headings with their path, which Doxygen omits."""
    if not heading.endswith(" Directory Reference"):
        return heading
    trail = breadcrumb(doc)
    if len(trail) < 2:
        return heading
    return "/".join(trail) + " Directory Reference"


def page_title(doc, project, filename):
    heading = page_heading(doc)
    if heading:
        heading = disambiguate(heading, doc)
        if heading.lower().startswith(project.lower()):
            return heading
        return f"{project}: {heading}"
    return f"{project}: {derived_heading(doc, filename, project)}"


def page_description(doc, fallback):
    match = CONTENTS_RE.search(doc)
    if not match:
        return fallback
    body = doc[match.end():]
    for pattern in (PARAGRAPH_RE, TEXTBLOCK_RE):
        found = pattern.search(body)
        if not found:
            continue
        text = re.sub(r"\s*More\.\.\.$", "", text_of(found.group(1)))
        if not text:
            continue
        if len(text) > MAX_DESCRIPTION:
            text = text[:MAX_DESCRIPTION].rsplit(" ", 1)[0].rstrip(",.;:") + "\u2026"
        return text
    return fallback


def canonical_url(base_url, filename):
    if filename == "index.html":
        return f"{base_url}/latest/"
    return f"{base_url}/latest/{filename}"


def rewrite_page(doc, filename, project, base_url, noindex):
    """Return the page with canonical/description/title/heading fixed."""
    doc = CANONICAL_RE.sub("", doc)
    doc = DESCRIPTION_RE.sub("", doc)
    doc = ROBOTS_RE.sub("", doc)

    if noindex or filename in NOINDEX_PAGES:
        return doc.replace(
            "</title>", '</title>\n<meta name="robots" content="noindex"/>', 1
        )

    title = page_title(doc, project, filename)
    heading = page_heading(doc)
    description = page_description(
        doc, f"{heading or title} \u2014 {project} API reference documentation."
    )
    canonical = canonical_url(base_url, filename)

    doc = TITLE_RE.sub(
        lambda _m: "<title>" + html.escape(title, quote=False) + "</title>", doc, count=1
    )
    doc = doc.replace(
        "</title>",
        "</title>\n"
        f'<link rel="canonical" href="{html.escape(canonical, quote=True)}"/>\n'
        f'<meta name="description" content="{html.escape(description, quote=True)}"/>',
        1,
    )

    # Promote the page heading to a real <h1>. The wrapper div is kept so the
    # existing `div.header .title` styling keeps applying unchanged.
    if HEADERTITLE_RE.search(doc):
        qualified = disambiguate(heading, doc)
        if qualified != heading:
            doc = HEADERTITLE_RE.sub(
                lambda _m: '<div class="headertitle"><div class="title">'
                + html.escape(qualified)
                + "</div></div>",
                doc,
                count=1,
            )
        return HEADERTITLE_RE.sub(
            lambda m: '<div class="headertitle"><h1 class="title">' + m.group(1) + "</h1></div>",
            doc,
            count=1,
        )

    # Member index pages have no heading at all, so add one in the same shape
    # Doxygen uses for the pages that do.
    heading = html.escape(derived_heading(doc, filename, project))
    return CONTENTS_RE.sub(
        '<div class="header">\n'
        f'  <div class="headertitle"><h1 class="title">{heading}</h1></div>\n'
        "</div><!--header-->\n"
        '<div class="contents">',
        doc,
        count=1,
    )


def write_sitemap(html_dir, base_url, filenames):
    entries = "\n".join(
        "  <url>\n    <loc>"
        + html.escape(canonical_url(base_url, name), quote=False)
        + "</loc>\n  </url>"
        for name in filenames
    )
    body = (
        '<?xml version="1.0" encoding="UTF-8"?>\n'
        '<urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">\n'
        f"{entries}\n"
        "</urlset>\n"
    )
    path = os.path.join(html_dir, "sitemap.xml")
    with open(path, "wb") as handle:
        handle.write(body.encode("utf-8"))
    return path


def main():
    parser = argparse.ArgumentParser(description="Make Doxygen HTML output SEO compliant.")
    parser.add_argument("--html-dir", default="docs/html")
    parser.add_argument("--base-url", required=True, help="e.g. https://ref.lootlocker.com/unity")
    parser.add_argument("--project", required=True, help="e.g. LootLocker Unity SDK")
    parser.add_argument(
        "--noindex",
        action="store_true",
        help="mark the tree as non-indexable instead of adding canonicals",
    )
    args = parser.parse_args()

    if not os.path.isdir(args.html_dir):
        sys.exit(f"seo-postprocess: {args.html_dir} does not exist")

    base_url = args.base_url.rstrip("/")
    filenames = sorted(
        name
        for name in os.listdir(args.html_dir)
        if name.endswith(".html") and os.path.isfile(os.path.join(args.html_dir, name))
    )
    if not filenames:
        sys.exit(f"seo-postprocess: no HTML files found in {args.html_dir}")

    indexable = []
    titles = {}
    for name in filenames:
        path = os.path.join(args.html_dir, name)
        with open(path, "rb") as handle:
            doc = handle.read().decode("utf-8")
        updated = rewrite_page(doc, name, args.project, base_url, args.noindex)
        if updated != doc:
            with open(path, "wb") as handle:
                handle.write(updated.encode("utf-8"))
        if args.noindex or name in NOINDEX_PAGES:
            continue
        indexable.append(name)
        match = TITLE_RE.search(updated)
        titles.setdefault(text_of(match.group(1)) if match else "", []).append(name)

    if args.noindex:
        # A noindex tree must not advertise a sitemap of latest/ URLs.
        sitemap = os.path.join(args.html_dir, "sitemap.xml")
        if os.path.exists(sitemap):
            os.remove(sitemap)
        print(f"seo-postprocess: marked {len(filenames)} pages noindex")
        return

    sitemap = write_sitemap(args.html_dir, base_url, indexable)
    duplicates = {title: pages for title, pages in titles.items() if len(pages) > 1}
    print(
        f"seo-postprocess: {len(filenames)} pages, {len(indexable)} indexable, "
        f"{len(duplicates)} duplicate titles, sitemap -> {sitemap}"
    )
    for title, pages in sorted(duplicates.items()):
        print(
            f"seo-postprocess: warning: duplicate title {title!r} on {', '.join(pages[:5])}",
            file=sys.stderr,
        )


if __name__ == "__main__":
    main()
