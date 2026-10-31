# Airo website

The official Airo landing page lives in `site/` on the `airo-website` branch. It is a static, responsive site built with HTML, CSS and JavaScript. It supports English/Hebrew (including RTL), dark/light themes, a mobile navigation menu, and uses the Airo logo.

## Publish on GitHub Pages

1. Open the Airo repository on GitHub.
2. Go to **Settings → Pages**.
3. Under **Build and deployment**, set **Source** to **GitHub Actions**.
4. Go to **Actions → Deploy Airo website to GitHub Pages** and select **Run workflow** on the `airo-website` branch (or push another change to `site/`).
5. After the workflow succeeds, GitHub Pages should show the site at `https://alonweizzz.github.io/Airo/` (subject to the repository's Pages configuration).

The workflow uploads **only `site/`**, so the web assets must stay inside that folder. The landing page currently points to the development branch and GitHub Actions rather than inventing a public Windows download URL.

## Local preview

Open `site/index.html` in a browser. The site uses Google Fonts when online, with system font fallbacks. No Node.js build step is needed.

## Notes

- The hero illustration is a **concept mockup**, not a live screenshot.
- The Airo desktop app uses a separately installed external UxPlay receiver. The website does not claim the receiver is bundled.
- Roadmap items are planned or exploratory, not shipping promises.
