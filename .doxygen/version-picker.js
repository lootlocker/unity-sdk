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

  /** Normalize pathname: treat a trailing slash as /index.html so that
   *  directory-style URLs (e.g. /latest/) are handled correctly. */
  function normalizePath(pathname) {
    return pathname.endsWith('/') ? pathname + 'index.html' : pathname;
  }

  /** Fetch versions.json by trying candidate site roots 2 then 3 path segments
   *  above the current page.  This covers both the common Doxygen layout
   *  (/<repo>/<version>/page.html) and the search subdirectory
   *  (/<repo>/<version>/search/page.html).
   *  Resolves with { root, versions } on success. */
  function fetchVersions() {
    var loc = window.location;
    var parts = normalizePath(loc.pathname).split('/');
    var tried = Promise.reject(new Error('no candidates'));
    for (var strip = 2; strip <= 3; strip++) {
      (function (s) {
        if (parts.length > s) {
          var rootPath = parts.slice(0, parts.length - s).join('/') + '/';
          var root = loc.protocol + '//' + loc.host + rootPath;
          tried = tried.catch(function () {
            return fetch(root + 'versions.json', { cache: 'no-cache' })
              .then(function (r) {
                if (!r.ok) throw new Error('versions.json not found at ' + root);
                return r.json().then(function (v) { return { root: root, versions: v }; });
              });
          });
        }
      })(strip);
    }
    return tried;
  }

  function buildPicker(result) {
    var badge = document.getElementById('ll-topnav-version');
    if (!badge) return;

    var loc = window.location;
    var root = result.root;
    var versions = result.versions;

    // Derive the version dir and page path relative to it from the current URL
    var rootPath = new URL(root).pathname;                          // e.g. '/unity-sdk/'
    var relative = normalizePath(loc.pathname).slice(rootPath.length); // e.g. 'latest/foo.html'
    var slash = relative.indexOf('/');
    var currentVersionPath = slash !== -1 ? relative.slice(0, slash + 1) : 'latest/';
    var page = slash !== -1 ? relative.slice(slash + 1) : 'index.html';
    var suffix = loc.search + loc.hash;  // preserve query string and anchor

    var select = document.createElement('select');
    select.id = 'll-version-picker';
    select.setAttribute('aria-label', 'Select documentation version');
    select.title = 'Switch version';

    versions.forEach(function (entry) {
      var opt = document.createElement('option');
      opt.value = root + entry.path + page + suffix;
      opt.textContent = entry.version;
      if (entry.path === currentVersionPath) {
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
    fetchVersions()
      .then(function (result) {
        if (Array.isArray(result.versions) && result.versions.length > 0) {
          buildPicker(result);
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
