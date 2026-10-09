import { test } from "node:test";
import assert from "node:assert/strict";
import { assign, chores, people } from "../chores.js";

test("each chore gets a person", () => {
  const result = assign(chores, people);
  assert.equal(result.length, chores.length);
  assert.ok(result.every((a) => people.includes(a.person)));
});

test("no people gives no assignments", () => {
  assert.deepEqual(assign(chores, []), []);
});
