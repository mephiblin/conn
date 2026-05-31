---
name: conn-unity-uiux-debug
description: Use for iterative UI/UX debugging in this Unity RPG project, especially inventory, skill dice loadout, shops, quest-to-dungeon flow, combat HUD, cursor state, debug overlays, and map generator UX. Follow a scorecard-driven loop, update doc/dev/uiux_scorecard.md, run Unity tests, and commit/push medium-or-higher changes.
---

# Conn Unity UI/UX Debug Loop

## Goal

Improve runtime UI/UX until the current scope reaches a documented 10/10 score against commercial RPG standards:

- Clear information hierarchy and low cognitive load.
- Distinct screens for inventory, skills, quests, shops, combat, and debug.
- Immediate, legible feedback for player input and combat results.
- Stable layout across the expected desktop play viewport.
- No developer/debug surface visible during normal play unless explicitly toggled.

If a 10/10 result requires unavailable art, animation, audio, input mapping, user preference, or live playtest evidence, stop that item, document the blocker in `doc/dev/uiux_scorecard.md`, and say what the user must decide or provide.

## Loop

1. Read `doc/dev/uiux_scorecard.md` and the UI code touched by the relevant item.
2. Pick the lowest-scoring item that can be improved with local code or docs.
3. Define one concrete acceptance check before editing.
4. Make the smallest coherent change.
5. Run targeted tests first, then broader tests when shared runtime code changed.
6. Update the scorecard with the new score, evidence, test command, commit hash if available, and remaining blocker if not 10/10.
7. Commit and push when the change is medium importance or higher, or when the user explicitly requested commit/push cadence.
8. Repeat until every in-scope item is 10/10 or documented as blocked.

## Scoring

Use integer or half-point scores.

- 10: Shippable for the current prototype scope; direct path, clear feedback, no avoidable debug clutter, validated by tests or screenshot/manual run.
- 8-9: Good prototype UX, but polish or visual assets still limit commercial quality.
- 6-7: Functional but still causes hesitation, extra reading, weak feedback, or layout friction.
- 4-5: Usable only with developer knowledge.
- 0-3: Broken, hidden, misleading, or blocks progress.

## Unity Validation Commands

Use the installed Unity editor:

```bash
"/home/inri/Unity/Hub/Editor/6000.4.8f1/Editor/Unity" -batchmode -nographics -projectPath . -runTests -testPlatform EditMode -testResults "tmp/editmode-results.xml" -logFile "tmp/editmode.log"
```

Targeted filters:

```bash
-testFilter Conn.Tests.EditMode.GameFlowPlaytestTests
-testFilter Conn.Tests.EditMode.RuntimeCoreRulesTests
-testFilter Conn.Tests.EditMode.MapGenerationTests
```

After tests, inspect result summaries:

```bash
rg -n "testcasecount|result=|failed=|<test-case.*Failed|<message|Error|error CS" tmp/*.xml tmp/*.log
```

Unity license handshake errors in batchmode logs are noise only when test result XML reports `failed="0"`.

## Commit Discipline

- Never revert user changes.
- Use focused commits with Korean or English messages that name the UX area.
- Before commit: `git diff --check`, targeted tests, `git status --short`.
- After commit: `git push`.
- Record the commit hash in the scorecard when it closes or materially improves an item.
