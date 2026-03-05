# Subscrio Documentation

This repository contains the documentation for Subscrio, built with [MkDocs](https://www.mkdocs.org/) and the [Material for MkDocs](https://squidfunk.github.io/mkdocs-material/) theme.

## Directory Structure

```
.
├── docs/                  # Source documentation files (markdown)
│   ├── assets/           # Images, favicons, and other static assets
│   ├── blog/             # Blog posts
│   └── reference/        # API reference documentation
├── material/             # Custom Material theme extensions and plugins
├── overrides/            # Theme customization (templates, CSS, JS)
├── includes/             # Included markdown snippets
├── site/                 # Generated site output (gitignored)
├── mkdocs.yml           # MkDocs configuration file
├── requirements.txt     # Python dependencies
└── CNAME                # Custom domain for GitHub Pages (docs.subscrio.com)
```

### Key Directories

- **`docs/`** - Contains all source documentation files written in Markdown
- **`material/`** - Custom extensions and plugins for the Material theme
- **`overrides/`** - Theme customizations including custom templates, CSS, and JavaScript
- **`site/`** - Generated HTML output (created when you run `python -m mkdocs build`, not committed to git)
- **`includes/`** - Reusable markdown snippets included in documentation

## Getting Started

### Prerequisites

- Python 3.x
- pip

### Installation

1. Install dependencies:
```bash
pip install -r requirements.txt
```

2. Preview locally:
```bash
python -m mkdocs serve
```

Visit `http://127.0.0.1:8000` to view the documentation.

## Publishing to GitHub Pages

### Option 1: Using mkdocs gh-deploy (Recommended)

The easiest way to publish is using MkDocs' built-in GitHub Pages deployment:

```bash
python -m mkdocs gh-deploy
```

This command will:
1. Build the documentation site
2. Commit the generated `site/` directory to the `gh-pages` branch
3. Push it to GitHub

**Note:** Make sure you have write access to the repository and your GitHub Pages settings are configured to serve from the `gh-pages` branch.

### Option 2: Manual Deployment

1. Build the site:
```bash
python -m mkdocs build
```

2. Copy the `CNAME` file to the `site/` directory (if using a custom domain):
```bash
cp CNAME site/
```

3. Push the `site/` directory to the `gh-pages` branch:
```bash
cd site
git init
git add -A
git commit -m "Deploy documentation"
git push -f git@github.com:Saas-Experts-Co/subscrio.git main:gh-pages
```

### Option 3: GitHub Actions (Automated)

The repository includes a workflow that automatically builds and deploys the docs when you push to `main` (when files under `docs/` change). The workflow is at `.github/workflows/deploy-docs.yml`.

It installs dependencies from `docs/requirements.txt`, runs `mkdocs build` from the `docs/` directory, copies `docs/CNAME` for the custom domain, and deploys the built site to the `gh-pages` branch using [peaceiris/actions-gh-pages](https://github.com/peaceiris/actions-gh-pages).

Ensure **Settings → Pages** is set to serve from the `gh-pages` branch. Optional: set the `GOOGLE_ANALYTICS_KEY` repository variable if you use analytics.

## Custom Domain

The site is configured to use the custom domain `docs.subscrio.com`. The `CNAME` file in the root directory contains this domain and will be included in deployments. Make sure your DNS is configured to point to GitHub Pages.
