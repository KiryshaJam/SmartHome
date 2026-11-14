import type { SpecParam } from "../components/BomPanel";

export type Expression =
  | { kind: "reference"; key: string }
  | { kind: "literal"; type: "number" | "string" | "boolean"; value: string }
  | { kind: "unary"; op: "NOT" | "-"; operand: Expression }
  | { kind: "binary"; op: string; left: Expression; right: Expression };
export interface ExpressionContext {
  parameters: SpecParam[];
  components?: { id: number; name: string }[];
  resultParameter?: SpecParam;
}
const priorities: Record<string, number> = { OR: 1, "||": 1, AND: 2, "&&": 2, "==": 3, "!=": 3, "<": 4, ">": 4, "<=": 4, ">=": 4, "+": 5, "-": 5, "*": 6, "/": 6, "%": 6 };
const words: Record<string, string> = { "==": "равно", "!=": "не равно", "<": "меньше", ">": "больше", "<=": "не больше", ">=": "не меньше", AND: "и", "&&": "и", OR: "или", "||": "или", "+": "+", "-": "−", "*": "×", "/": "÷", "%": "остаток от деления на" };
export const connectionOptions = [
  { value: "wired", label: "Проводное подключение" },
  { value: "radio", label: "Беспроводное подключение" },
  { value: "hybrid", label: "Гибридное подключение" },
];
export const stringLabel = (value: string) => connectionOptions.find(x => x.value === value)?.label ?? value;
export const numberLiteral = (value = "1"): Expression => ({ kind: "literal", type: "number", value });
export function reference(key: string): Expression { return { kind: "reference", key }; }

/** Mirrors the server's precedence; never evaluates expressions or changes saved text. */
export function parseExpression(text: string): Expression | null {
  if (!text.trim() || text.length > 1024) return null;
  const tokens: string[] = [];
  let rest = text.trim();
  while (rest) {
    const match = /^(p[0-9]+|has_[A-Za-z0-9_]+|[0-9]+(?:\.[0-9]+)?|"(?:[^"\\]|\\.)*"|==|!=|<=|>=|&&|\|\||[()+*/%<>!\-]|AND\b|OR\b|NOT\b|true\b|false\b)/.exec(rest);
    if (!match || tokens.length >= 256) return null;
    tokens.push(match[0]); rest = rest.slice(match[0].length).trimStart();
  }
  let at = 0;
  function parse(min: number, depth: number): Expression {
    if (depth > 32 || at >= tokens.length) throw new Error("Incomplete expression");
    const token = tokens[at++];
    let left: Expression;
    if (token === "(") {
      left = parse(0, depth + 1);
      if (tokens[at++] !== ")") throw new Error("Unclosed parenthesis");
    } else if (token === "NOT" || token === "!" || token === "-") {
      left = { kind: "unary", op: token === "!" ? "NOT" : token, operand: parse(7, depth + 1) };
    } else if (token.startsWith('"')) {
      left = { kind: "literal", type: "string", value: JSON.parse(token) as string };
    } else if (token === "true" || token === "false") {
      left = { kind: "literal", type: "boolean", value: token };
    } else if (/^[0-9]/.test(token)) {
      left = numberLiteral(token);
    } else if (/^(p\d+|has_[A-Za-z0-9_]+)$/.test(token)) {
      left = reference(token);
    } else throw new Error("Unexpected token");
    while (at < tokens.length && (priorities[tokens[at]] ?? -1) >= min) {
      const op = tokens[at++];
      left = { kind: "binary", op, left, right: parse(priorities[op] + 1, depth + 1) };
    }
    return left;
  }
  try { const result = parse(0, 0); return at === tokens.length ? result : null; }
  catch { return null; }
}
export function writeExpression(e: Expression): string {
  if (e.kind === "reference") return e.key;
  if (e.kind === "literal") return e.type === "string" ? JSON.stringify(e.value) : e.value;
  if (e.kind === "unary") return `${e.op === "NOT" ? "NOT " : "-"}(${writeExpression(e.operand)})`;
  return `(${writeExpression(e.left)} ${e.op} ${writeExpression(e.right)})`;
}
export function describeExpression(text: string, context: ExpressionContext, empty = "Не задано"): string {
  if (!text.trim()) return empty;
  const expression = parseExpression(text);
  if (!expression) return "Сложное выражение — доступно в дополнительном режиме";
  function describe(e: Expression, expected?: SpecParam): string {
    if (e.kind === "reference") {
      if (e.key.startsWith("has_")) {
        const component = context.components?.find(c => `has_${c.id}` === e.key);
        return component ? `«${component.name}» входит в состав` : "Компонент входит в состав";
      }
      return context.parameters.find(p => p.key === e.key)?.name ?? "Параметр из другой версии";
    }
    if (e.kind === "literal") {
      if (e.type === "string") return `«${expected && /подключ/i.test(expected.name) ? stringLabel(e.value) : e.value}»`;
      if (e.type === "boolean") return e.value === "true" ? "Да" : "Нет";
      if (expected?.type === 5) return `«${expected.enumLabels?.[e.value] ?? "Значение перечисления"}»`;
      return e.value;
    }
    if (e.kind === "unary") return e.op === "NOT" ? `Не выполняется: (${describe(e.operand)})` : `−(${describe(e.operand)})`;
    const left = e.left, right = e.right;
    const p = left.kind === "reference" ? context.parameters.find(p => p.key === left.key) : undefined;
    const rightP = right.kind === "reference" ? context.parameters.find(p => p.key === right.key) : undefined;
    const comparison = ["==", "!=", "<", ">", "<=", ">="].includes(e.op);
    return `(${describe(e.left, comparison ? rightP : undefined)} ${words[e.op] ?? e.op} ${describe(e.right, comparison ? p : undefined)})`;
  }
  return describe(expression, context.resultParameter);
}
export function defaultValue(p?: SpecParam): Expression {
  if (p?.type === 5) return numberLiteral(String(p.default ?? p.enumIds[0] ?? 0));
  if (p?.type === 3) return { kind: "literal", type: "string", value: String(p.default ?? (/подключ/i.test(p.name) ? "wired" : "")) };
  return numberLiteral(String(p?.default ?? p?.min ?? 1));
}
export function defaultCondition(parameters: SpecParam[]): Expression {
  const p = parameters.find(p => p.type === 3 || p.type === 5) ?? parameters[0];
  return p ? { kind: "binary", op: "==", left: reference(p.key), right: defaultValue(p) } : { kind: "literal", type: "boolean", value: "true" };
}

export function expressionReferences(text: string): Set<string> | null {
  if (!text.trim()) return new Set();
  const expression = parseExpression(text);
  if (!expression) return null;
  const keys = new Set<string>();
  function visit(node: Expression) {
    if (node.kind === "reference") keys.add(node.key);
    else if (node.kind === "unary") visit(node.operand);
    else if (node.kind === "binary") { visit(node.left); visit(node.right); }
  }
  visit(expression);
  return keys;
}

export function humanizeBomError(text: string, parameterNames: Record<string, string>, componentNames: Record<string, string> = {}): string {
  return text.replace(/\bp\d+\b/g, key => `«${parameterNames[key] ?? "параметр изделия"}»`)
    .replace(/\bhas_(\d+)\b/g, (_, id: string) => `«${componentNames[id] ?? "компонент"}»`)
    .replace(/\bwired\b/g, "проводное подключение").replace(/\bradio\b/g, "беспроводное подключение").replace(/\bhybrid\b/g, "гибридное подключение");
}

/** Changes all/any without inserting or losing an existing condition. */
export function setConditionJoin(node: Expression, operator: "AND" | "OR"): Expression {
  if (node.kind !== "binary" || !["AND", "OR", "&&", "||"].includes(node.op)) return node;
  const previousOperator = node.op;
  function operands(e: Expression): Expression[] {
    return e.kind === "binary" && e.op === previousOperator ? [...operands(e.left), ...operands(e.right)] : [e];
  }
  return operands(node).reduce((left, right) => ({ kind: "binary", op: operator, left, right }));
}

export function initialValueExpression(parameters: SpecParam[], result: SpecParam | undefined, kind: "formula" | "binding"): Expression {
  const numeric = !result || result.type === 1 || result.type === 2;
  const p = parameters.find(p => (kind === "binding" || p.key !== result?.key) && (numeric ? p.type === 1 || p.type === 2 : p.type === result.type));
  if (kind === "formula" && numeric) return { kind: "binary", op: "*", left: p ? reference(p.key) : numberLiteral(), right: numberLiteral("2") };
  return p ? reference(p.key) : defaultValue(result);
}
