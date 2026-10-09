import { assign, chores, people } from "./chores.js";

const list = document.querySelector("#assignments");
for (const { chore, person } of assign(chores, people)) {
  const item = document.createElement("li");
  const name = chores.find((c) => c.id === chore).name;
  item.textContent = `${name}: ${person}`;
  list.append(item);
}
