# GitHub Workflow: Issues & PRs

## Reading Issues

```bash
# List all open issues
gh issue list

# List by milestone
gh issue list --milestone "Sprint 1: Critical Fixes"

# List by label
gh issue list --label "P0-critical"
gh issue list --label "type:bug"
gh issue list --label "area:canvas"

# View specific issue details
gh issue view 22

# Search issues
gh issue list --search "canvas"
```

## Updating Issues

```bash
# Close an issue (when done)
gh issue close 7

# Close with a comment
gh issue close 7 --comment "Fixed in commit abc1234"

# Add a comment to an issue
gh issue comment 22 --body "Phase 1-3 implemented in PR #6. Phase 4 (final swap) still pending."

# Edit issue title/body
gh issue edit 22 --title "New title"
gh issue edit 22 --body "Updated description"

# Change labels
gh issue edit 22 --add-label "in-progress"
gh issue edit 22 --remove-label "P0-critical" --add-label "P1-important"

# Assign to someone
gh issue edit 22 --add-assignee rasyidf

# Move to different milestone
gh issue edit 22 --milestone "Sprint 2: Canvas Completion"
```

## Referencing Issues in Commits & PRs

### Auto-close issues from commits
Use these keywords in commit messages:
```
git commit -m "fix: atomic file writes

Closes #7"
```

Keywords that auto-close: `Closes`, `Fixes`, `Resolves` (case-insensitive)

### Reference without closing (partial work)
```
git commit -m "feat(canvas): Phase 2 features

Related to #22
Partially addresses #32"
```

### PR body references
In the PR description:
```markdown
## Related Issues
- Closes #7, #8, #9       (will auto-close on merge)
- Partially addresses: #22  (won't close, just links)
- Related: #23, #24         (won't close, just links)
```

## Creating PRs that Reference Issues

```bash
# Create PR with issue references
gh pr create --title "fix: atomic file writes" \
  --body "Closes #7" \
  --label "type:bug" \
  --milestone "Sprint 1: Critical Fixes"

# Update PR description
gh pr edit 6 --body "Updated description with Closes #7, #8"
```

## Workflow Pattern

1. **Pick issue** → `gh issue view 22`
2. **Create branch** → `git checkout -b fix/issue-7-atomic-writes`
3. **Work on it** → make changes, commit with `Closes #7` in message
4. **Push + PR** → `git push -u origin fix/issue-7-atomic-writes && gh pr create`
5. **Link milestone** → `gh pr edit N --milestone "Sprint 1: Critical Fixes"`
6. **On merge** → GitHub auto-closes referenced issues

## Current State

### PR #6 (refactor/codebase-modernization)
- 11 commits, +5631/-1003 lines
- Partially addresses: #22 (EditorCanvas Phase 1-3), #32 (select-same), #13 (region query added)
- Does NOT close any issues (created before issues existed)
- Should be merged first, then Sprint 1 fixes start as separate PRs

### Labels
| Label | Color | Purpose |
|-------|-------|---------|
| P0-critical | Red | Must fix before release |
| P1-important | Orange | High priority |
| P2-nice-to-have | Yellow | Medium priority |
| P3-future | Green | Low priority / future |
| area:canvas | Blue | Canvas/rendering |
| area:ui | Purple | UI/UX |
| area:data | Teal | Data layer |
| area:infra | Light blue | CI/build/infra |
| area:perf | Pink | Performance |
| type:feature | Cyan | New feature |
| type:bug | Red | Bug fix |

### Milestones
1. Sprint 1: Critical Fixes (6 P0 bugs)
2. Sprint 2: Canvas Completion (Phase 4 + perf)
3. Sprint 3: Data & Reliability (async, backup, tests)
4. Sprint 4: UX Polish (accessibility, DnD, notifications)
5. Sprint 5: Future-Proofing (STJ, compression, rotation)
