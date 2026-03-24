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
  var content = document.querySelector('.content');
  var contentInner = content ? content.querySelector('.content-inner') : null;
  var topbarCtx = document.querySelector('.topbar-context');

  /* The overview page is a real DOM node; command pages are <template> elements */
  var overviewPage = document.querySelector('.page[data-page="overview"]');
  var dynamicPage = null; /* container for the currently materialized template */

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
    var cap = 20;
    cards.forEach(function(c) { c.classList.add('card-enter'); });
    requestAnimationFrame(function() {
      requestAnimationFrame(function() {
        cards.forEach(function(c, i) {
          if (i < cap) {
            setTimeout(function() {
              c.style.transition = 'opacity .3s cubic-bezier(.4,0,.2,1), transform .3s cubic-bezier(.4,0,.2,1), border-color .2s, box-shadow .2s';
              c.classList.remove('card-enter');
            }, i * 30);
          } else {
            c.classList.remove('card-enter');
          }
        });
      });
    });
  }

  var activeNav = document.querySelector('.nav-link.active');

  function showPage(pageId) {
    /* Tear down previous dynamic page */
    if (dynamicPage) {
      dynamicPage.remove();
      dynamicPage = null;
    }

    var visiblePage = null;

    if (pageId === 'overview') {
      overviewPage.classList.add('active');
      visiblePage = overviewPage;
    } else {
      overviewPage.classList.remove('active');

      /* Find the template */
      var tpl = document.querySelector('template[data-page="' + pageId + '"]');

      if (!tpl) {
        /* pageId might be an in-page anchor — find the element inside a template */
        var allTemplates = document.querySelectorAll('template[data-page]');
        for (var i = 0; i < allTemplates.length; i++) {
          if (allTemplates[i].content.getElementById(pageId)) {
            tpl = allTemplates[i];
            break;
          }
        }
      }

      if (tpl) {
        dynamicPage = document.createElement('div');
        dynamicPage.className = 'page active';
        dynamicPage.setAttribute('data-page', tpl.getAttribute('data-page'));
        dynamicPage.appendChild(tpl.content.cloneNode(true));
        contentInner.appendChild(dynamicPage);
        visiblePage = dynamicPage;
        staggerCards(dynamicPage);
      }
    }

    /* Update nav highlight */
    if (activeNav) activeNav.classList.remove('active');
    var href = pageId === 'overview' ? '#overview' : '#' + pageId;
    var active = document.querySelector('.nav-link[href="' + href + '"]');
    if (active) {
      active.classList.add('active');
      activeNav = active;
      expandParents(active);
      active.scrollIntoView({ block: 'nearest' });
    }

    if (content) content.scrollTo(0, 0);
    if (topbarCtx) {
      var h = visiblePage ? visiblePage.querySelector('h1, h2') : null;
      topbarCtx.textContent = h ? h.textContent : 'Overview';
    }
    rebuildComposer();
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

  /* ── Composer ── */
  var composer = document.querySelector('[data-composer]');
  var composerBody = composer ? composer.querySelector('.composer-body') : null;
  var composerOutput = composer ? composer.querySelector('.composer-output') : null;
  var composerToggleBtn = document.querySelector('[data-composer-toggle]');

  var savedWidth = localStorage.getItem('opencli-composer-w');
  if (savedWidth && composer) composer.style.width = savedWidth;

  function toggleComposer() {
    if (!composer) return;
    composer.hidden = !composer.hidden;
    if (composerToggleBtn) composerToggleBtn.classList.toggle('active', !composer.hidden);
    localStorage.setItem('opencli-composer', composer.hidden ? 'closed' : 'open');
    if (!composer.hidden) rebuildComposer();
  }

  function getActivePage() {
    return dynamicPage || (overviewPage.classList.contains('active') ? overviewPage : null);
  }

  function rebuildComposer() {
    if (!composerBody || !composer || composer.hidden) return;
    composerBody.innerHTML = '';

    var page = getActivePage();
    if (!page || page.getAttribute('data-page') === 'overview') {
      composerBody.innerHTML = '<div class="composer-empty">Select a command to start composing.</div>';
      updateComposerPreview();
      return;
    }

    var optionCards = page.querySelectorAll('.option-card');
    if (!optionCards.length) {
      composerBody.innerHTML = '<div class="composer-empty">This command has no configurable options.</div>';
      updateComposerPreview();
      return;
    }

    var heading = document.createElement('p');
    heading.className = 'composer-section-title';
    var cmdName = page.querySelector('h2');
    heading.innerHTML = 'Options for <code>' + (cmdName ? cmdName.textContent : '') + '</code>';
    composerBody.appendChild(heading);

    optionCards.forEach(function(card) {
      var nameEl = card.querySelector('.option-head strong code');
      if (!nameEl) return;
      var optName = nameEl.textContent.replace(/ \(hidden\)$/, '');
      var badges = card.querySelectorAll('.badge');
      var isFlag = Array.from(badges).some(function(b) { return b.textContent.trim() === 'flag'; });
      var descEl = card.querySelector(':scope > p');
      var desc = descEl ? descEl.textContent.trim() : '';
      if (desc === 'No description provided.') desc = '';

      var field = document.createElement('div');
      field.className = 'composer-field';

      if (isFlag) {
        var lbl = document.createElement('label');
        lbl.className = 'composer-flag';
        lbl.innerHTML = '<input type="checkbox" data-opt="' + optName + '"><div><span class="composer-opt-name">' + optName + '</span>' + (desc ? '<span class="composer-opt-desc">' + desc + '</span>' : '') + '</div>';
        field.appendChild(lbl);
      } else {
        var placeholder = 'value';
        var pBadge = card.querySelector('.badge-primary');
        if (pBadge) placeholder = pBadge.textContent.trim().replace(/[<>]/g, '');
        field.innerHTML = '<label class="composer-opt-name">' + optName + '</label><input type="text" placeholder="' + placeholder + '" data-opt="' + optName + '" class="composer-input">' + (desc ? '<span class="composer-opt-desc">' + desc + '</span>' : '');
      }
      composerBody.appendChild(field);
    });

    composerBody.addEventListener('input', updateComposerPreview);
    composerBody.addEventListener('change', updateComposerPreview);
    updateComposerPreview();
  }

  function updateComposerPreview() {
    if (!composerOutput) return;
    var page = getActivePage();
    if (!page || page.getAttribute('data-page') === 'overview') {
      composerOutput.textContent = '...';
      return;
    }
    var parts = [];
    var bc = page.querySelector('.breadcrumb');
    if (bc) {
      bc.querySelectorAll('a, .crumb-current').forEach(function(el) { parts.push(el.textContent.trim()); });
    }
    if (composerBody) {
      composerBody.querySelectorAll('[data-opt]').forEach(function(inp) {
        var name = inp.getAttribute('data-opt');
        if (inp.type === 'checkbox' && inp.checked) {
          parts.push(name);
        } else if (inp.type === 'text' && inp.value.trim()) {
          var v = inp.value.trim();
          v = v.indexOf(' ') !== -1 ? '"' + v + '"' : v;
          parts.push(name + ' ' + v);
        }
      });
    }
    composerOutput.textContent = parts.join(' ') || '...';
  }

  function copyComposerCommand() {
    if (!composerOutput) return;
    var text = composerOutput.textContent;
    if (!text || text === '...') return;
    var btn = composer.querySelector('[data-composer-copy]');
    var done = function() {
      if (btn) { btn.classList.add('copied'); setTimeout(function() { btn.classList.remove('copied'); }, 2000); }
    };
    try {
      if (navigator.clipboard) { navigator.clipboard.writeText(text).then(done); }
      else { var ta = document.createElement('textarea'); ta.value = text; document.body.appendChild(ta); ta.select(); document.execCommand('copy'); document.body.removeChild(ta); done(); }
    } catch(e) {}
  }

  /* Restore composer state or auto-open on first visit with wide screen */
  var composerState = localStorage.getItem('opencli-composer');
  if (composer) {
    if (composerState === 'open') {
      composer.hidden = false;
      if (composerToggleBtn) composerToggleBtn.classList.add('active');
      rebuildComposer();
    } else if (!composerState && window.innerWidth >= 1280) {
      composer.hidden = false;
      if (composerToggleBtn) composerToggleBtn.classList.add('active');
      rebuildComposer();
    }
  }

  if (composerToggleBtn) composerToggleBtn.addEventListener('click', toggleComposer);
  if (composer) composer.addEventListener('click', function(e) {
    if (e.target.closest('[data-composer-copy]')) copyComposerCommand();
  });

  /* Composer resize drag */
  var resizeHandle = composer ? composer.querySelector('[data-composer-resize]') : null;
  if (resizeHandle) {
    var dragging = false;
    resizeHandle.addEventListener('mousedown', function(e) {
      e.preventDefault();
      dragging = true;
      resizeHandle.classList.add('dragging');
      document.body.style.cursor = 'col-resize';
      document.body.style.userSelect = 'none';
    });
    document.addEventListener('mousemove', function(e) {
      if (!dragging) return;
      var layoutRight = composer.parentElement.getBoundingClientRect().right;
      var w = layoutRight - e.clientX;
      if (w >= 224 && w <= 576) { composer.style.width = w + 'px'; localStorage.setItem('opencli-composer-w', w + 'px'); }
    });
    document.addEventListener('mouseup', function() {
      if (!dragging) return;
      dragging = false;
      resizeHandle.classList.remove('dragging');
      document.body.style.cursor = '';
      document.body.style.userSelect = '';
    });
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
