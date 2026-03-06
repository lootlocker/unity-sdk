# Issue Tracking & Lifecycle

All SDK work is driven by a tracking issue in [lootlocker/index](https://github.com/lootlocker/index). That issue is the single source of truth for status, decisions, and acceptance criteria. **You must keep it up to date throughout your work.**

## Project Status

This issue will almost always be tracked in project https://github.com/orgs/lootlocker/projects/75. Update the issue's project status as your work progresses:

| Situation | Status to set |
|-----------|--------------|
| You start working on the task | **In Progress** |
| You are blocked and need input from a human | **Blocked** |
| A PR has been opened and is ready for review | **In Review** |

## Architectural Decisions & Questions

Do not make undocumented assumptions. If a question or decision arises during implementation:
- Leave a comment on the tracking issue describing the question or decision clearly.
- Tag @kirre-bylund so it can be addressed.
- Set the project status to **Blocked** and stop work on the affected area until answered.

## Linking PRs

As soon as you open a PR in this repo, post a comment on the tracking issue with the PR link. Also link the PR formally via GitHub's "Development" section on the tracking issue.

## Acceptance Criteria & Definition of Done

Check off items in the tracking issue's Definition of Done as they are completed. If scope changes during implementation, update the acceptance criteria in the tracking issue and leave a comment explaining what changed and why.
