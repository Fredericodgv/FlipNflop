# C# Coding Standards & Guidelines for FlipNflop

> **Location:** `Assets/.agents/csharp_coding_standards.md`  
> **Target:** All C# scripts in `Assets/Scripts/`  
> **Mandatory Rule:** Any agent or developer modifying or creating C# scripts in this project MUST strictly follow these rules.

---

## 1. Documentation & Commenting Rules

### 1.1. Summary Documentation (`/// <summary>`)
- Every class, interface, enum, struct, public/protected/private method, and property must have a `/// <summary>` XML documentation block in **English**.
- **Content Requirements:**
  - Clear explanation of what the method or class does.
  - **External Dependencies & Connections:** Briefly state any external connections or references (e.g., calls to `LevelManager`, `SignalColorManager`, events, singletons, or UI managers).
- **Example:**
```csharp
/// <summary>
/// Calculates the expected logical timeline for flip-flop state transitions.
/// Interacts with <see cref="LevelData"/> to parse input signals and generate reference corners.
/// </summary>
public static List<Vector2> GenerateReferencePath(LevelData levelData)
{
    // ...
}
```

### 1.2. Removal & Translation of In-line / Portuguese Comments
- **No Portuguese Comments:** Remove any comments in Portuguese (`// ...` or `/* ... */`), or translate their intent into English and include them in the XML `/// <summary>` block.
- **Minimal In-Method Comments:** Avoid placing inline comments (`// ...`) inside method bodies. Inline comments should only exist when there is an **ABSOLUTE/CRITICAL NEED** to explain unusually intricate or non-obvious algorithms.

---

## 2. Naming Conventions & Inspector Attributes (English Only)

### 2.1. Variable & Member Naming
- All variable names, field names, parameter names, properties, and method names **must be in English**.
- Follow standard C# / Unity naming conventions:
  - Private/Protected fields: `camelCase` or `_camelCase`.
  - Public fields, Properties, Methods, Events: `PascalCase`.

### 2.2. Unity Inspector Attributes (`[Header]`, `[Tooltip]`)
- All `[Header("...")]` section titles must be in **English**.
- All `[Tooltip("...")]` description strings must be in **English**.

### 2.3. Code Organization with Regions (`#region`)
- Standardize all `#region` and `#endregion` block titles into **English**.
- Use `#region` blocks to group large/complex code sections (e.g., `#region Unity Life Cycle`, `#region Movement Logic`, `#region Event Handlers`, `#region Public API`).

---

## 3. Checklist for Agents Before Finishing Edits

Before completing any C# editing task:
- [ ] Are all `/// <summary>` tags written in English?
- [ ] Does every method summary mention external script dependencies if present?
- [ ] Are all Portuguese comments removed or converted to English XML summaries?
- [ ] Are inline comments inside methods kept to a absolute minimum?
- [ ] Are variable names, `[Header]`, `[Tooltip]`, and `#region` names in English?
