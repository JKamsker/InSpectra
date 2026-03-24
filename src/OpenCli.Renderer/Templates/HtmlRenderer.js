(()=>{
  /* ── Theme ── */
  var root = document.documentElement;
  var saved = localStorage.getItem('opencli-theme');
  if (saved) {
    root.setAttribute('data-theme', saved);
  } else if (window.matchMedia('(prefers-color-scheme: dark)').matches) {
    root.setAttribute('data-theme', 'dark');
  }

  document.addEventListener('click', function(e) {
    var btn = e.target.closest('[data-theme-toggle]');
    if (!btn) return;
    var next = root.getAttribute('data-theme') === 'dark' ? 'light' : 'dark';
    root.setAttribute('data-theme', next);
    localStorage.setItem('opencli-theme', next);
  });

  /* ── SPA navigation ── */
  var pages = document.querySelectorAll('.page');
  var navLinks = document.querySelectorAll('.nav-link');
  var content = document.querySelector('.content');
  var topbarCtx = document.querySelector('.topbar-context');

  function expandParents(el) {
    var item = el.closest('.nav-item');
    while (item) {
      item.classList.remove('collapsed');
      item.hidden = false;
      item = item.parentElement ? item.parentElement.closest('.nav-item') : null;
    }
  }

  function staggerCards(page) {
    var cards = page.querySelectorAll('.command-card, .option-card');
    if (!cards.length) return;
    cards.forEach(function(c) { c.classList.add('card-enter'); });
    requestAnimationFrame(function() {
      requestAnimationFrame(function() {
        cards.forEach(function(c, i) {
          setTimeout(function() {
            c.style.transition = 'opacity .3s cubic-bezier(.4,0,.2,1), transform .3s cubic-bezier(.4,0,.2,1), border-color .2s, box-shadow .2s';
            c.classList.remove('card-enter');
          }, i * 30);
        });
      });
    });
  }

  function showPage(pageId) {
    pages.forEach(function(p) { p.classList.remove('active'); });
    var target = document.querySelector('[data-page="' + pageId + '"]');
    if (target) {
      target.classList.add('active');
      staggerCards(target);
    }

    navLinks.forEach(function(l) { l.classList.remove('active'); });
    var href = pageId === 'overview' ? '#overview' : '#' + pageId;
    var active = document.querySelector('.nav-link[href="' + href + '"]');
    if (active) {
      active.classList.add('active');
      expandParents(active);
      active.scrollIntoView({ block: 'nearest' });
    }

    if (content) content.scrollTo(0, 0);
    if (topbarCtx) {
      var h = target ? target.querySelector('h1, h2') : null;
      topbarCtx.textContent = h ? h.textContent : 'Overview';
    }
  }

  function handleNav(e) {
    var toggle = e.target.closest('[data-nav-toggle]');
    if (toggle) {
      e.preventDefault();
      e.stopPropagation();
      var item = toggle.closest('.nav-item');
      if (item) item.classList.toggle('collapsed');
      return;
    }

    var link = e.target.closest('a[href^="#"]');
    if (!link) return;
    var href = link.getAttribute('href');
    if (!href || href === '#') return;
    e.preventDefault();
    var pageId = href === '#overview' ? 'overview' : href.slice(1);
    history.pushState(null, '', href);
    showPage(pageId);
  }

  var sidebar = document.querySelector('.sidebar-nav');
  if (sidebar) sidebar.addEventListener('click', handleNav);
  if (content) content.addEventListener('click', handleNav);

  window.addEventListener('popstate', function() {
    showPage(window.location.hash.slice(1) || 'overview');
  });

  if (window.location.hash && window.location.hash !== '#overview') {
    showPage(window.location.hash.slice(1));
  }

  /* ── Search / filter ── */
  var search = document.querySelector('[data-nav-search]');
  if (!search) return;
  var roots = function() { return Array.from(document.querySelectorAll('.nav-tree > .nav-item')); };
  var visit = function(item, query) {
    var label = (item.dataset.label || '').toLowerCase();
    var children = Array.from(item.querySelectorAll(':scope > .nav-tree > .nav-item'));
    var childMatch = children.map(function(c) { return visit(c, query); }).some(Boolean);
    var selfMatch = query === '' || label.indexOf(query) !== -1;
    item.hidden = !(selfMatch || childMatch);
    return selfMatch || childMatch;
  };
  search.addEventListener('input', function() {
    var query = search.value.trim().toLowerCase();
    roots().forEach(function(item) { visit(item, query); });
  });
})();
