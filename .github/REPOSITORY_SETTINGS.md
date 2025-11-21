# GitHub Repository Settings Configuration Guide

This document outlines the repository settings you should configure on GitHub.com to protect your repository from unauthorized code changes.

## Quick Links

- Repository Settings: `https://github.com/HivemindOverlord/poe2-mcp/settings`
- Branch Protection: `https://github.com/HivemindOverlord/poe2-mcp/settings/branches`
- Collaborators: `https://github.com/HivemindOverlord/poe2-mcp/settings/access`

---

## 1. General Settings

**Path:** Settings → General

### Repository Visibility
- [ ] **Private** - Only you and collaborators can see this repository
- [ ] **Public** - Anyone can see this repository (current setting)

**Recommendation:** Keep public if you want others to use/fork it, but configure protections below.

### Features
- [x] **Wikis** - Enable if you want a wiki
- [x] **Issues** - Enable for bug reports (recommended)
- [x] **Sponsorships** - Optional
- [x] **Projects** - Optional
- [x] **Discussions** - Enable for Q&A separate from issues (recommended)

### Pull Requests
- [x] **Allow merge commits** - Enable
- [ ] **Allow squash merging** - Optional (cleaner history)
- [ ] **Allow rebase merging** - Optional
- [x] **Always suggest updating pull request branches** - Enable
- [x] **Automatically delete head branches** - Enable (cleanup merged branches)

### Merge Button Settings
- [x] **Allow auto-merge** - Disable (you want manual control)
- [x] **Require PR approval before merging** - Enable (critical!)

---

## 2. Branch Protection Rules (CRITICAL)

**Path:** Settings → Branches → Add rule

### Protect `main` branch

Click "Add rule" and configure:

#### Branch name pattern
```
main
```

#### Protect matching branches
- [x] **Require a pull request before merging** ✅ REQUIRED
  - [x] Require approvals: **1** (minimum - you must approve all PRs)
  - [x] Dismiss stale pull request approvals when new commits are pushed
  - [x] Require review from Code Owners (CODEOWNERS file will enforce this)

- [x] **Require status checks to pass before merging** (if you have CI/CD)
  - [x] Require branches to be up to date before merging
  - Add status checks: (e.g., pytest, linting - if you set up GitHub Actions)

- [x] **Require conversation resolution before merging** ✅ REQUIRED
  - Forces all review comments to be resolved

- [x] **Require signed commits** (optional but recommended for security)
  - Ensures commits are cryptographically signed

- [x] **Require linear history** (optional)
  - Forces squash/rebase merges (cleaner history)

- [x] **Require deployments to succeed before merging** (optional)

#### Do not allow bypassing the above settings
- [x] **Do not allow bypassing the above settings**
  - IMPORTANT: Leave this UNCHECKED so YOU can still push directly if needed
  - OR: Check it and add yourself as an exception if you want strict enforcement

#### Rules applied to everyone including administrators
- [ ] **Include administrators**
  - Uncheck if you want to be able to push directly
  - Check if you want strict enforcement even for yourself

#### Restrict who can push to matching branches
- [x] **Restrict pushes that create matching branches**
  - Add: `HivemindOverlord` (only you can push)

#### Allow force pushes
- [ ] **Allow force pushes** - Disable (prevent rewriting history)

#### Allow deletions
- [ ] **Allow deletions** - Disable (prevent accidental deletion)

---

## 3. Collaborator & Team Access

**Path:** Settings → Collaborators and teams

### Current Access Level
- **HivemindOverlord** - Admin (owner)

### Adding Collaborators (Optional)
If you want to add trusted collaborators:

1. Click "Add people"
2. Search for their GitHub username
3. Select permission level:
   - **Read** - Can view and clone only
   - **Triage** - Can manage issues/PRs but not merge
   - **Write** - Can push to non-protected branches (still blocked from main)
   - **Maintain** - Can manage repo but not change settings
   - **Admin** - Full control (only for highly trusted people)

**Recommendation:** Don't add collaborators unless absolutely necessary. Keep it just you.

---

## 4. Code Security and Analysis

**Path:** Settings → Security & analysis

### Security Features
- [x] **Dependency graph** - Enable (tracks dependencies)
- [x] **Dependabot alerts** - Enable (security vulnerability alerts)
- [x] **Dependabot security updates** - Enable (auto-creates PRs for security fixes)
- [ ] **Dependabot version updates** - Optional (auto-updates dependencies)

### Code Scanning
- [x] **CodeQL analysis** - Enable (if public repo - free security scanning)
  - Set up via: Security → Code scanning → Set up CodeQL

### Secret Scanning
- [x] **Secret scanning** - Enable (detects committed secrets like API keys)
- [x] **Push protection** - Enable (blocks pushes containing secrets)

---

## 5. Actions Permissions (If using GitHub Actions)

**Path:** Settings → Actions → General

### Actions permissions
- [ ] **Disable Actions** - If you don't use GitHub Actions
- [x] **Allow [owner] and select non-[owner], actions and reusable workflows**
  - Only allow approved actions (tightest security)

### Workflow permissions
- [ ] **Read and write permissions** - Disable
- [x] **Read repository contents permission** - Enable (minimal permissions)

---

## 6. Webhooks and Notifications

**Path:** Settings → Webhooks

### Email Notifications
Configure in your personal settings:
- Settings → Notifications → Email notification preferences
- [x] Enable notifications for: Pull requests, Issues, Mentions

### Webhooks (Optional)
Only add if integrating with external services (Discord, Slack, etc.)

---

## 7. Pages Settings (If using GitHub Pages)

**Path:** Settings → Pages

- **Source:** None (disable unless you want a website)

---

## 8. Additional Protections

### Tag Protection Rules
**Path:** Settings → Tags

Create a rule to protect version tags:
- Pattern: `v*` (protects all version tags like v1.0.0)
- Only allow: `HivemindOverlord`

### Rulesets (New Feature - Alternative to Branch Protection)
**Path:** Settings → Rulesets

GitHub's newer, more flexible protection system. You can use this instead of branch protection rules.

---

## Summary Checklist

Essential protections to enable RIGHT NOW:

- [ ] Branch protection on `main` requiring PR approval ✅ CRITICAL
- [ ] CODEOWNERS file enforced (already created in repo) ✅ CRITICAL
- [ ] Require conversation resolution ✅ CRITICAL
- [ ] Dependabot alerts enabled
- [ ] Secret scanning enabled
- [ ] Push protection enabled
- [ ] No collaborators added (unless trusted)
- [ ] Pull request template enforced (already created)
- [ ] Issue templates configured (already created)

---

## Testing Your Protections

After configuring:

1. Try to push directly to `main`:
   ```bash
   git checkout main
   echo "test" >> test.txt
   git add test.txt
   git commit -m "test"
   git push origin main
   ```
   **Expected:** Should be rejected (unless you're excluded from rules)

2. Try creating a PR without approval:
   - Create a branch, make changes, push
   - Open a PR
   - Try to merge without approval
   **Expected:** Merge button disabled until you approve

---

## GitHub CLI Commands (Optional)

You can also configure some settings via CLI:

```bash
# Enable vulnerability alerts
gh repo edit HivemindOverlord/poe2-mcp --enable-vulnerability-alerts

# Enable automated security fixes
gh repo edit HivemindOverlord/poe2-mcp --enable-auto-security-fixes

# View branch protection status
gh api repos/HivemindOverlord/poe2-mcp/branches/main/protection
```

---

## Questions?

If you're unsure about any setting, err on the side of being MORE restrictive. You can always loosen protections later.

**Most Critical Settings:**
1. Branch protection requiring PR approval on `main`
2. CODEOWNERS enforcement
3. No write access for anyone except you
4. Secret scanning with push protection

These four settings alone will prevent 99% of unwanted code changes.
