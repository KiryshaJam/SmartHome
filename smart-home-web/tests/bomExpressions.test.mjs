import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import ts from "typescript";
const source = readFileSync(new URL("../src/utils/bomExpressions.ts", import.meta.url), "utf8");
const js = ts.transpileModule(source, { compilerOptions: { target: ts.ScriptTarget.ES2022, module: ts.ModuleKind.ES2022 } }).outputText;
const { parseExpression, writeExpression, describeExpression, setConditionJoin, expressionReferences, humanizeBomError, initialValueExpression } = await import(`data:text/javascript;base64,${Buffer.from(js).toString("base64")}`);
const context = { parameters: [
  { key: "p12", name: "Число датчиков", type: 1 },
  { key: "p13", name: "Способ подключения", type: 3 },
  { key: "p1", name: "Тип подключения", type: 5, enumLabels: { "1": "Проводное", "2": "Беспроводное" } },
], components: [{ id: 8, name: "Кабель" }, { id: 11, name: "Радиомодуль" }] };
const samples = [
  'p13 == "wired"', 'p12 * 2', 'NOT (p13 == "radio" AND p12 > 4)',
  'NOT (has_8 AND has_11)', 'NOT has_8 OR has_11',
  'p12 >= 3 OR p13 == "wired"', 'p12 + 2 * 3', '(p12 + 2) * 3',
  'p12 - (2 - 3)', 'p12 / (2 / 3)', 'NOT (p12 == 2 OR p12 == 3)',
  'p12 > -2', '-(p12 + 2)', 'p1 == 1', 'true', 'false',
  'p12 % 3', 'p13 == "p12 has_8 AND \\"text\\""'.replaceAll('\\\\"', '\\"'),
  'p12 + 0.123456789123456789123456789', '!has_8 || has_11 && p12 != 4',
];
let count = 0;
for (const sample of samples) {
  const parsed = parseExpression(sample);
  assert.ok(parsed, sample);
  assert.deepEqual(parseExpression(writeExpression(parsed)), parsed, `Meaning changed on round-trip: ${sample}`);
  count++;
}
assert.equal(describeExpression('p13 == "wired"', context), '(Способ подключения равно «Проводное подключение»)');
assert.equal(describeExpression('p1 == 2', context), '(Тип подключения равно «Беспроводное»)');
assert.equal(describeExpression('p12 * 2', context), '(Число датчиков × 2)');
assert.match(describeExpression('NOT (has_8 AND has_11)', context), /Кабель.*Радиомодуль/);
assert.match(describeExpression('p13 == "p12"', context), /«p12»/); // literals must not be replaced with parameter names
assert.equal(writeExpression(parseExpression('0.123456789123456789123456789')), '0.123456789123456789123456789');
for (const invalid of ['', 'p12 +', 'p12 ** 2', 'p12 = 2', '(p12 + 2', 'fetch("url")', 'p12; true', '"bad\\q"', '('.repeat(34)+'1'+')'.repeat(34)]) {
  assert.equal(parseExpression(invalid), null, invalid);
}
assert.deepEqual(setConditionJoin(parseExpression('p1 == 1 AND p1 == 2 AND p1 == 3'), 'OR'), parseExpression('p1 == 1 OR p1 == 2 OR p1 == 3'));
assert.deepEqual(setConditionJoin(parseExpression('p1 == 1 OR p1 == 2'), 'AND'), parseExpression('p1 == 1 AND p1 == 2'));
assert.deepEqual([...expressionReferences('p13 == "p12" AND p1 == 1')].sort(), ['p1', 'p13']);
assert.equal(expressionReferences('unfinished('), null);
assert.equal(humanizeBomError('Заполните Способ подключения (p13). Выберите wired или radio.', {p13:'Способ подключения'}), 'Заполните Способ подключения («Способ подключения»). Выберите проводное подключение или беспроводное подключение.');
assert.equal(describeExpression('p1 * 2', context), '(Тип подключения × 2)');
const numericParameters = [{key:'p12', type:1, name:'Число датчиков'}, {key:'p14', type:2, name:'Длина линии'}];
assert.equal(writeExpression(initialValueExpression(numericParameters, numericParameters[1], 'formula')), '(p12 * 2)');
assert.equal(writeExpression(initialValueExpression(numericParameters, numericParameters[0], 'binding')), 'p12');
assert.equal(writeExpression(initialValueExpression(numericParameters, numericParameters[0], 'formula')), '(p14 * 2)');
console.log(`Expression editor: ${count} precedence/escaping/precision round-trips and named rendering/invalid-input checks passed.`);
