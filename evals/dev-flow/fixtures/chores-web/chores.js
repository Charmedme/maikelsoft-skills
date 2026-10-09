// Pure logic for the household chore list. No DOM access in this file.

export const people = ["Anna", "Bram", "Chris"];

export const chores = [
  { id: "dishes", name: "Dishes" },
  { id: "trash", name: "Take out the trash" },
  { id: "bathroom", name: "Clean the bathroom" },
  { id: "vacuum", name: "Vacuum the living room" },
];

// Gives each chore to a person. Today the list is fixed: chore i goes to person i modulo the number of people.
export function assign(choreList, peopleList) {
  if (peopleList.length === 0) return [];
  return choreList.map((chore, i) => ({ chore: chore.id, person: peopleList[i % peopleList.length] }));
}
