import { type MouseEvent, useCallback, useEffect, useRef, useState } from "react";
import { ciGuideNavLinks, ciGuideSectionIds, pipelineSteps, type UsageTab } from "./CIGuidePageData";
import {
  CIGuideInputsSection,
  CIGuidePagesSection,
  CIGuidePrerequisitesSection,
  CIGuideUsageSection,
} from "./CIGuideSections";

export function CIGuidePage({ section }: { section?: string }) {
  const [activeUsageTab, setActiveUsageTab] = useState<UsageTab>("from-source");
  const scrollingRef = useRef(false);
  const scrollTimerRef = useRef<ReturnType<typeof setTimeout>>(undefined);

  useEffect(() => {
    if (!section) {
      return;
    }

    const element = document.getElementById(section);
    if (element) {
      element.scrollIntoView({ behavior: "smooth", block: "start" });
    }
  }, [section]);

  useEffect(() => {
    const sectionObserver = new IntersectionObserver(
      (entries) => {
        if (scrollingRef.current) {
          return;
        }

        for (const entry of entries) {
          if (entry.isIntersecting) {
            history.replaceState(null, "", `#/guide/${entry.target.id}`);
          }
        }
      },
      { rootMargin: "-20% 0px -60% 0px", threshold: 0 },
    );

    const heroObserver = new IntersectionObserver(
      (entries) => {
        if (scrollingRef.current) {
          return;
        }

        for (const entry of entries) {
          if (entry.isIntersecting) {
            history.replaceState(null, "", "#/guide");
          }
        }
      },
      { threshold: 0.1 },
    );

    for (const id of ciGuideSectionIds) {
      const element = document.getElementById(id);
      if (element) {
        sectionObserver.observe(element);
      }
    }

    const hero = document.querySelector(".ci-guide-hero");
    if (hero) {
      heroObserver.observe(hero);
    }

    return () => {
      sectionObserver.disconnect();
      heroObserver.disconnect();
    };
  }, []);

  const scrollTo = useCallback((event: MouseEvent<HTMLAnchorElement>, id: string) => {
    event.preventDefault();
    scrollingRef.current = true;
    clearTimeout(scrollTimerRef.current);
    history.replaceState(null, "", `#/guide/${id}`);

    const element = document.getElementById(id);
    if (element) {
      element.scrollIntoView({ behavior: "smooth", block: "start" });
    }

    scrollTimerRef.current = setTimeout(() => {
      scrollingRef.current = false;
    }, 800);
  }, []);

  useEffect(() => () => clearTimeout(scrollTimerRef.current), []);

  return (
    <main className="ci-guide-page">
      <section className="ci-guide-hero">
        <div className="ci-guide-hero-glow" aria-hidden="true" />
        <div className="ci-guide-badge">
          <span className="ci-guide-dot" />
          GitHub Actions
        </div>
        <h1>
          Automate your <span className="ci-guide-accent">CLI docs</span>
        </h1>
        <p className="ci-guide-hero-sub">
          Generate InSpectraUI documentation in CI and deploy to GitHub Pages, attach as a
          release asset, or download as an artifact. One workflow call, zero config.
        </p>

        <div className="ci-guide-pipeline" aria-hidden="true">
          {pipelineSteps.map((step, index) => (
            <div key={step.label} className="ci-guide-pipe-group">
              {index > 0 && (
                <div className="ci-guide-pipe-line">
                  <div className="ci-guide-pipe-pulse" />
                </div>
              )}
              <div className="ci-guide-pipe-node">
                <span className="ci-guide-pipe-ring" />
                <div className="ci-guide-pipe-text">
                  <span className="ci-guide-pipe-label">{step.label}</span>
                  <span className="ci-guide-pipe-sub">{step.sub}</span>
                </div>
              </div>
            </div>
          ))}
        </div>
      </section>

      <nav className="ci-guide-nav" aria-label="Page sections">
        {ciGuideNavLinks.map((link) => (
          <a
            key={link.id}
            href={`#/guide/${link.id}`}
            className="ci-guide-nav-link"
            onClick={(event) => scrollTo(event, link.id)}
          >
            <span className="ci-guide-nav-num">{link.num}</span>
            {link.label}
          </a>
        ))}
      </nav>

      <div className="ci-guide-timeline">
        <CIGuideUsageSection activeUsageTab={activeUsageTab} onSelectUsageTab={setActiveUsageTab} />
        <CIGuideInputsSection />
        <CIGuidePagesSection />
        <CIGuidePrerequisitesSection />
      </div>
    </main>
  );
}
