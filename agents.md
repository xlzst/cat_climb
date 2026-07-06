# AGENTS.md - AI Coding Assistant Guidelines

## 1. Project Context (WHAT & WHY)
- Stack: Unity 6 LTS (2D URP), C#
- Project: Mobile Verttical Hybrid Casual (Wall-jumping & Doodle Jump mechanics)
- Core Loop: Touch/swipe to control wall-climbing and jumping, utilize One-Way Platforms.

## 2. Boundaries (CRITICAL)
- Allowed Directory: ONLY create/modify files within `Assets/_MyProject/`.
- Forbidden Directory: NEVER touch or modify `Assets/ThirdParty/` or `Packages/`.
- Security: NEVER hard-code secrets, tokens, or API keys in the scripts.

## 3. Architecture & Code Style (HOW)
- Performance: Avoid use `Instantiate()` or `Destroy()` during active gameplay.
- Structure: Strictly separate logic (`GameManager`, `PlayerController`, `MapGenerator`). No monolithic scripts.

## 4. Development Log
- After completing a task, update `log.md`.
- Create `log.md` if it does not exist.
- Add a dated entry describing the completed work.
- Preserve all existing log entries and append new ones chronologically.