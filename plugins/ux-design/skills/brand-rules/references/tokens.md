# Tokens in the DTCG format

The format is the Design Tokens Format Module 2025.10 (the first stable version, 2025-10-28). File name: `tokens.json` or `<name>.tokens.json`.

## Shape

```json
{
  "color": {
    "$type": "color",
    "neutral-900": {"$value": {"colorSpace": "srgb", "components": [0.106, 0.141, 0.188], "hex": "#1b2430"}, "$description": "Darkest neutral. Use only through the semantic tokens."},
    "text": {"$value": "{color.neutral-900}", "$description": "Body text on bg and surface.",
             "$extensions": {"org.maikelsoft.contrast": {"on": ["{color.bg}", "{color.surface}"], "min": 4.5}}},
    "bg": {"$value": {"colorSpace": "srgb", "components": [0.961, 0.969, 0.98], "hex": "#f5f7fa"}, "$description": "Page background."}
  },
  "space": {
    "$type": "dimension",
    "2": {"$value": {"value": 8, "unit": "px"}, "$description": "Gap between controls in a group."}
  },
  "font": {
    "$type": "fontFamily",
    "body": {"$value": ["IBM Plex Sans", "Segoe UI", "system-ui", "sans-serif"], "$description": "All UI text."}
  },
  "typography": {
    "$type": "typography",
    "body": {"$value": {"fontFamily": "{font.body}", "fontSize": {"value": 14, "unit": "px"}, "fontWeight": 400, "lineHeight": 1.43}, "$description": "Table cells and body text."}
  }
}
```

## Rules

- A token is an object with `$value`. A group is an object without `$value`. Put `$type` on the group when all its tokens have the same type.
- Names: no `.`, `{`, or `}`, and no `$` at the start. Use lower case and `-`. Claude Design also needs names of max 64 characters from `A-Z a-z 0-9 _ . -`.
- Types: `color`, `dimension` (`px` or `rem`), `fontFamily`, `fontWeight`, `duration` (`ms` or `s`), `number`, `shadow`, `typography`, and the other types of the format.
- Two layers: primitives (`neutral-900`, `teal-600`) and semantic tokens (`text`, `accent`, `danger`) that alias them. The UI uses only the semantic tokens.
- Each token has a `$description` that says where to use it. Claude Design shows it as the usage note.
- Each text color and each icon color has a contrast pair. The pair names the backgrounds of the color and the minimum ratio. The minimum is 4.5 for text, and 3 for large text, icons, and borders of controls.
- A theme is a second file, for example `tokens.dark.json`, with the same paths and only the values that change. Check it with `--theme dark=tokens.dark.json`.

## brandcheck.py rules

| Rule | Meaning |
|------|---------|
| TK-NAME | A name has a character that the format does not allow. |
| TK-TYPE | No `$type`, or an unknown type. |
| TK-VALUE | The value does not fit its type. |
| TK-ALIAS | The alias points to a missing token, makes a loop, or points to another type. |
| TK-USAGE | No `$description` (a warning). |
| TK-THEME | A theme file has a path that the base file does not have. |
| TK-PAIR, TK-CONTRAST | A contrast pair cannot be computed, or it fails in a theme. |
| CD-* | `to-cd`: a value, a name, or a usage note that Claude Design cannot read. |
| CP-BANNED | `copy`: a word of the "Do not use" column is in the text. |
