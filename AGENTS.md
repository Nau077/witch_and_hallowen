# Codex / Agent Instructions (Unity project)

## Goal
Help implement small, safe changes in a Unity 2D pixel-art game project.
Prefer minimal edits, predictable behavior, and Unity-friendly code.

## Non-negotiable rules
1) MINIMAL CHANGES:
   - Do not refactor the whole project.
   - Avoid new architectures/frameworks/DI.
   - Keep current patterns used in the project.

2) SAFETY:
   - Never break existing gameplay.
   - Prefer additive changes (new script) over risky changes inside core systems.
   - If you must change an existing script: keep the public API stable and change as little as possible.

3) FULL FILE OUTPUT:
   - If a file is modified, output the entire file content (not a diff).
   - Mention each changed file path explicitly.

4) UNITY PRACTICES:
   - Use [SerializeField] for inspector references.
   - Avoid heavy allocations in Update.
   - Cache components in Awake/OnEnable when possible.
   - Null-check inspector refs and log clear errors.

5) PROJECT STYLE
   - Keep code simple, readable.
   - Use existing naming patterns (UI/Player/Enemy/skills).
   - Prefer coroutines for simple timed effects.

## Typical tasks
- UI bars/counters (fillAmount, blink, flash on “no resource”)
- Small gameplay fixes and polish
- Layering/sorting fixes for sprites and UI
- Bug fixes with clear reproduction steps

## When uncertain
Before changing anything substantial:
- Search for similar existing code (e.g. ManaBarUI/HealthBarUI style).
- Reuse existing patterns.
- Ask for acceptance criteria only if absolutely necessary; otherwise make a reasonable assumption and state it.

## Testing / Verification
Always provide:
- Steps in Unity Editor (what GameObject to create, what to assign in Inspector).
- A short “checklist if it doesn’t work”.
- If relevant: where to look in Console and what logs mean.

## Output format
1) Summary (what you did)
2) Files changed (paths)
3) Full code for each changed file
4) Unity setup steps
5) Troubleshooting checklist
