## Pull Request - Please Read Before Submitting

**IMPORTANT**: This repository has a gated contribution policy. Please read [CONTRIBUTING.md](../CONTRIBUTING.md) before submitting.

### Pre-Submission Checklist

- [ ] I have opened an issue and received approval from the maintainer before creating this PR
- [ ] I have read and understood the [CONTRIBUTING.md](../CONTRIBUTING.md) guidelines
- [ ] I understand that this PR may be closed without merging

### Description

**What does this PR do?**

(Provide a clear, concise description of the changes)

**Related Issue:**

Closes #(issue number)

### Type of Change

- [ ] Bug fix (non-breaking change that fixes an issue)
- [ ] New feature (non-breaking change that adds functionality)
- [ ] Breaking change (fix or feature that would cause existing functionality to not work as expected)
- [ ] Documentation update
- [ ] Code refactoring (no functional changes)
- [ ] Performance improvement
- [ ] Test coverage improvement

### Changes Made

(Detailed list of changes)

-
-
-

### Testing

- [ ] All existing tests pass (`pytest`)
- [ ] I have added new tests for my changes
- [ ] I have tested this with real character data (if applicable)
- [ ] I have tested with the MCP server running in Claude Desktop (if applicable)

**Test Results:**
```
(Paste pytest output or test results here)
```

### Code Quality

- [ ] Code follows the async-first architecture (see CLAUDE.md)
- [ ] All functions have type hints
- [ ] Public methods have docstrings
- [ ] No exceptions raised to MCP layer (all caught and returned as error TextContent)
- [ ] Follows the dual import pattern
- [ ] No hardcoded paths (uses BASE_DIR, DATA_DIR from config.py)

### Documentation

- [ ] I have updated CLAUDE.md (if architecture changed)
- [ ] I have updated README.md (if user-facing changes)
- [ ] I have updated relevant docs/ files
- [ ] I have added inline comments for complex logic

### Security

- [ ] No sensitive information (API keys, tokens, credentials) committed
- [ ] No SQL injection vulnerabilities introduced
- [ ] No XSS vulnerabilities introduced
- [ ] Input validation added where necessary

### Breaking Changes

**Does this PR introduce breaking changes?**

- [ ] No
- [ ] Yes (describe below)

**If yes, describe the breaking changes and migration path:**

(Describe what breaks and how users should update their code)

### Additional Context

(Any additional information, screenshots, or context about the PR)

---

**By submitting this PR, I acknowledge:**
- This PR may be closed without merging at the maintainer's discretion
- I have followed all guidelines in CONTRIBUTING.md
- I accept the project's contribution policy
