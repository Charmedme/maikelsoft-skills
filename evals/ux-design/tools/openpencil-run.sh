#!/bin/bash
# OpenPencil test of batch 3: one run of the openpencil skill with a real headless OpenPencil server.
# Usage: SCRATCH=<dir> RUNS=<dir> evals/ux-design/tools/openpencil-run.sh <variant> <n> <ux-design plugin folder>
S=${SCRATCH:?set SCRATCH to the folder with op/ (the op CLI, desk/, skillrepo/) and b3/ (the FL-31 mockups)}
OPD=$S/op; export PATH=$OPD/bin:$PATH OPENPENCIL_DESKTOP_BIN=$OPD/desk/openpencil-desktop
R=${RUNS:-runs/b3op}/$1/$2; rm -rf $R; mkdir -p $R
cp -r $(dirname "$0")/../fixtures/fieldline $R/repo
W=$R/repo/docs/work/fl-31-assign-unplanned; mkdir -p $W
cp -r $S/b3/m/docs/work/fl-31-assign-unplanned/mockups $W/
cp $S/b3/m/docs/work/fl-31-assign-unplanned/ux-design.md $W/ 2>/dev/null
sed -i 's/- Design tools: none/- Design tools: OpenPencil 0.8.4/; s/- Mockup tool: html/- Mockup tool: openpencil/' $R/repo/CLAUDE.md
op stop >/dev/null 2>&1; sleep 1
(cd $W/mockups && nohup op start --headless --file $W/mockups/fl-31.op > $R/op.log 2>&1 &)
for i in 1 2 3 4 5 6 7 8; do sleep 2; op status | grep -q true && break; done
cat > $R/mcp.json <<J
{"mcpServers":{"openpencil":{"type":"http","url":"http://127.0.0.1:3100/mcp"}}}
J
P="You work in the repository $R/repo. The checked HTML mockups of story FL-31 are in \`docs/work/fl-31-assign-unplanned/mockups/\` (one HTML file, one section for each state, screenshots in \`shots/\`). The mockup tool of the project is OpenPencil, and the OpenPencil server runs (the op CLI is on the PATH, and the MCP server is connected). Use the skill in \`$3/skills/openpencil/\`: read its SKILL.md and follow it. Read its files by path; its scripts are in \`$3/skills/openpencil/scripts/\`. Put the mockups in OpenPencil, one frame for each state, and save the document as \`docs/work/fl-31-assign-unplanned/mockups/fl-31.op\`. You cannot ask questions."
echo "$P" > $R/prompt.md
cd $R/repo && env -u CLAUDECODE -u CLAUDE_CODE_ENTRYPOINT claude -p "$P" --model sonnet --plugin-dir $OPD/skillrepo --mcp-config $R/mcp.json --strict-mcp-config --permission-mode acceptEdits --allowedTools Read Write Edit Glob Grep Skill "mcp__openpencil__*" "Bash(python3:*)" "Bash(op:*)" "Bash(ls:*)" --disallowedTools Agent WebFetch WebSearch --add-dir $3 --output-format stream-json --verbose --max-budget-usd 6 > $R/transcript.jsonl 2> $R/stderr.log
op save_document filePath=$R/saved.op > /dev/null 2>&1
mkdir -p $R/frames; op export-frames --output-dir $R/frames > $R/export.json 2>&1
op stop >/dev/null 2>&1
python3 - $R <<PY
import json,sys,pathlib
r=pathlib.Path(sys.argv[1]); cost=0
for l in open(r/'transcript.jsonl'):
  try: e=json.loads(l)
  except: continue
  if e.get('type')=='result': cost=e.get('total_cost_usd',0)
print(r, 'cost', round(cost,2), 'frames', len(list((r/'frames').glob('*.png'))))
PY
