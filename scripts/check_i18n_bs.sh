#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
EXPLANATION_FILE="$ROOT_DIR/AiAgents.MathTutorAgent/Application/Services/ExplanationService.cs"
LOCALIZER_FILE="$ROOT_DIR/AiAgents.MathTutorAgent/Application/Services/MathContentLocalizationService.cs"

if [[ ! -f "$EXPLANATION_FILE" || ! -f "$LOCALIZER_FILE" ]]; then
  echo "[i18n-check] Missing expected files." >&2
  exit 2
fi

missing=0
tmp_templates="$(mktemp)"
trap 'rm -f "$tmp_templates"' EXIT

# 1) Every hardcoded explanation template from ExplanationService must have
# an exact Bosnian mapping key in MathContentLocalizationService.
rg -No 'return \("([^"]+)"' "$EXPLANATION_FILE" --replace '$1' > "$tmp_templates"
rg -No '^\s*"(Example:[^"]+)"\s*\);\s*$' "$EXPLANATION_FILE" --replace '$1' >> "$tmp_templates"

while IFS= read -r template; do
  if ! rg -Fq "[\"$template\"] =" "$LOCALIZER_FILE"; then
    echo "[i18n-check] Missing exact BS translation key for template:" >&2
    echo "  $template" >&2
    missing=1
  fi
done < <(sort -u "$tmp_templates" | sed '/^$/d')

# 2) Guard against known awkward mixed-output phrases.
if rg -n 'Kolika je površina za|Koliki je obim za|Koliki je obim kruga za' "$LOCALIZER_FILE" >/dev/null; then
  echo "[i18n-check] Found awkward translation pattern in LocalizeQuestionText output." >&2
  missing=1
fi

# 3) Guard against unlocalized source page labels in explanation references.
if rg -n 'Page \{chunk\.PageNumber\}' "$EXPLANATION_FILE" >/dev/null; then
  echo "[i18n-check] Found hardcoded English page label in explanation references." >&2
  missing=1
fi

if [[ $missing -ne 0 ]]; then
  echo "[i18n-check] FAILED" >&2
  exit 1
fi

echo "[i18n-check] OK"
