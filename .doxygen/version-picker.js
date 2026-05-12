/**
 * version-picker.js — LootLocker Unity SDK
 *
 * Replaces the static #ll-topnav-version span with a <select> dropdown
 * that lets users navigate between published doc versions.
 *
 * The list of available versions is fetched from versions.json at the root
 * of the published site (one level above the current version directory).
 * Falls back gracefully to the static badge when versions.json is unavailable
 * (e.g. local Doxygen builds).
 *
 * Expected versions.json shape:
 *   [
 *     { "version": "latest", "path": "latest/" },
 *     { "version": "v2.5.0", "path": "v2.5.0/" }
 *   ]
 *
 * URL resolution:
 *   The script detects the current version directory by looking at the last
 *   path segment of the page URL and resolves versions.json relative to the
 *   site root (two levels up from the current page, one for the HTML file
 *   and one for the version directory).
 */
(function () {
  'use strict';

  /** Return the base URL of the versioned site root (the directory that
   *  contains latest/, v2.x.y/, versions.json, etc.).
   *  Works for both GH Pages (/repo/version/page.html) and CloudFront
   *  (/sdk/version/page.html). */
  function siteRoot() {
    var loc = window.location;
    // pathname segments, e.g. ['', 'unity-sdk', 'latest', 'index.html']
    var parts = loc.pathname.split('/').filter(function (p, i) { return i === 0 || p !== ''; });
    // Drop the filename (last segment) and the version dir (second-to-last)
    var rootParts = parts.slice(0, parts.length - 2);
    return loc.protocol + '//' + loc.host + rootParts.join('/') + '/';
  }

  /** Detect the current version path segment from the URL. */
  function currentVersionPath() {
    var parts = window.location.pathname.split('/').filter(Boolean);
    // Second-to-last non-empty segment is the version directory
    return parts.length >= 2 ? parts[parts.length - 2] + '/' : 'latest/';
  }

  /** Preserve the current page name when switching versions (best-effort).
   *  Falls back to index.html if the page doesn't exist in the target. */
  function currentPage() {
    var parts = window.location.pathname.split('/').filter(Boolean);
    return parts.length >= 1 ? parts[parts.length - 1] : 'index.html';
  }

  function buildPicker(versions) {
    var badge = document.getElementById('ll-topnav-version');
    if (!badge) return;

    var currentPath = currentVersionPath();
    var root = siteRoot();
    var page = currentPage();

    var select = document.createElement('select');
    select.id = 'll-version-picker';
    select.setAttribute('aria-label', 'Select documentation version');
    select.title = 'Switch version';

    versions.forEach(function (entry) {
      var opt = document.createElement('option');
      opt.value = root + entry.path + page;
      opt.textContent = entry.version;
      if (entry.path === currentPath) {
        opt.selected = true;
      }
      select.appendChild(opt);
    });

    select.addEventListener('change', function () {
      window.location.href = select.value;
    });

    badge.parentNode.replaceChild(select, badge);
  }

  function init() {
    var url = siteRoot() + 'versions.json';
    fetch(url, { cache: 'no-cache' })
      .then(function (r) {
        if (!r.ok) throw new Error('versions.json not found');
        return r.json();
      })
      .then(function (versions) {
        if (Array.isArray(versions) && versions.length > 0) {
          buildPicker(versions);
        }
      })
      .catch(function () {
        // Graceful fallback — keep the static badge as-is
      });
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', init);
  } else {
    init();
  }
}());
