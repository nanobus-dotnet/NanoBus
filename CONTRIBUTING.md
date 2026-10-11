# Contributing to NanoBus.Net

Thank you for your interest in contributing to **NanoBus**. This document outlines how to contribute using **GitHub Flow**, our simple branching workflow.

## GitHub Flow Overview

Our workflow uses a single main branch (`main`) and feature branches. Here's the basic process:

1. **Create a branch** from `main` for your feature or fix
2. **Commit and push** your changes
3. **Open a Pull Request (PR)** describing your changes
4. **Discuss and review** with maintainers
5. **Merge** into `main` once approved
6. **Deploy** (if applicable)

## Getting Started

### Prerequisites

- **Git** installed locally
- A **GitHub account**
- Basic familiarity with Git and command line

### Setup Your Development Environment

```bash
# Clone the repository
git clone https://github.com/nanobus-dotnet/NanoBus.git
cd NanoBus

# Create a feature branch
git checkout -b feature/your-feature-name
```

## Making Contributions

### Step 1: Create a Feature Branch

Always branch from `main`:

```bash
git checkout main
git pull origin main
git checkout -b feature/your-descriptive-branch-name
```

**Branch naming convention:**
- `feature/` - new features
- `fix/` - bug fixes
- `docs/` - documentation updates
- `refactor/` - code refactoring
- `test/` - test additions or improvements
- `chore/` - routine maintenance and dependency updates
- `release/` - major releases, structural overhauls, multi-faceted milestone work
- `example/` - examples

### Step 2: Make Your Changes

- **Keep commits atomic** — each commit should represent one logical change
- **Write clear commit messages** — use imperative mood ("Add feature" not "Added feature")
- **Follow code style** — match the existing code conventions in the project

Example commit:
```bash
git add .
git commit -m "Add user authentication feature"
```

### Step 3: Push to GitHub

```bash
git push origin feature/your-descriptive-branch-name
```

### Step 4: Open a Pull Request

1. Go to the repository on GitHub
2. Click **"Compare & pull request"** or the **"Pull requests"** tab
3. Fill out the PR template with:
   - **Title** — clear, concise description
   - **Description** — what changed and why
   - **Related issues** — link any issues (e.g., `Closes #42`)
   - **Screenshots** — if visual changes
   - **Testing notes** — how to verify your changes

### Step 5: Respond to Review Feedback

- Maintainers will review your code
- Address comments by pushing additional commits to the same branch
- Be respectful and open to suggestions

### Step 6: Merge and Deploy

Once approved:
- A maintainer will merge your PR into `main`
- Your branch will be deleted automatically
- Changes will be deployed per our deployment schedule

## Code of Conduct

### We Expect

- **Respectful communication** — treat everyone with kindness
- **Constructive feedback** — help improve ideas, not tear them down
- **Inclusivity** — welcome diverse perspectives and backgrounds

### We Won't Tolerate

- Harassment, discrimination, or hate speech
- Spam or low-effort contributions
- Violations of others' privacy or security

*For more details, see our [CODE_OF_CONDUCT.md](./CODE_OF_CONDUCT.md)*

## Testing & Code Quality

### Before Submitting a PR

- **Run tests** — ensure all tests pass locally
  ```bash
  dotnet test
  ```
- **Build the project** — verify it compiles/builds
  ```bash
  dotnet build
  ```

### PR Checks

Your PR will automatically run:
- Unit tests
- Build verification
- Coverage analysis (if applicable)

**All checks must pass** before merging.

## Documentation

- Update **README.md** if your changes affect setup or usage
- Add **inline comments** for complex logic
- Update **CHANGELOG.md** with your changes (or let maintainers handle it)
- Document new **environment variables** or configuration options

## Questions or Need Help?

- **Open a discussion** — use GitHub Discussions for questions
- **Check existing issues** — your question might already be answered
- **Ask in our community** — chat, Slack, or Discord (if applicable)
- **Email maintainers** — use nanobus.net@gmail.com for sensitive matters (if applicable)

## Recognition

Contributors are appreciated. We recognize contributions by:
- Listing you in our **CONTRIBUTORS.md** or **README.md**
- Acknowledging your PR with a thank you comment
- Crediting you in release notes

