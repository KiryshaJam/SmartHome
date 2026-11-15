import { useEffect, useRef, useState } from "react";
import type { SpecParam } from "./BomPanel";
import {
  connectionOptions, defaultCondition, defaultValue, describeExpression,
  numberLiteral, parseExpression, reference, writeExpression, setConditionJoin, initialValueExpression,
  type Expression, type ExpressionContext,
} from "../utils/bomExpressions";

const comparisons = [["==", "равно"], ["!=", "не равно"], [">", "больше"], ["<", "меньше"], [">=", "не меньше"], ["<=", "не больше"]];
const arithmetic = [["*", "умножить на"], ["/", "разделить на"], ["+", "прибавить"], ["-", "вычесть"], ["%", "остаток от деления на"]];
function negativeNumber(e: Expression) {
  return e.kind === "unary" && e.op === "-" && e.operand.kind === "literal" && e.operand.type === "number" ? `-${e.operand.value}` : null;
}

function NumericLiteralInput({ value, onChange }: { value: string; onChange: (value: string) => void }) {
  const [draft, setDraft] = useState(value);
  useEffect(() => setDraft(value), [value]);
  return <input className="input" type="number" step="any" value={draft} onChange={e => {
    setDraft(e.target.value);
    if (/^-?\d+(?:\.\d+)?$/.test(e.target.value)) onChange(e.target.value);
  }} onBlur={() => {
    const complete = draft.replace(/^(-?)\./, (_, sign: string) => `${sign}0.`).replace(/\.$/, "");
    if (/^-?\d+(?:\.\d+)?$/.test(complete)) { setDraft(complete); if (complete !== value) onChange(complete); }
    else setDraft(value);
  }} />;
}

function ValueControls({ node, onChange, context, label, expected }: {
  node: Expression; onChange: (e: Expression) => void; context: ExpressionContext;
  label: string; expected?: SpecParam;
}) {
  const mode = node.kind === "reference" ? "parameter" : node.kind === "binary" || (node.kind === "unary" && negativeNumber(node) == null) ? "calculation" : node.kind === "literal" ? node.type : "number";
  const numericParameters = context.parameters.filter(p => p.type === 1 || p.type === 2);
  const options = expected?.type === 5 ? expected.enumIds.map(id => ({ value: String(id), label: expected.enumLabels?.[id] ?? "Значение перечисления" })) : [];
  const n = negativeNumber(node);
  const literal = node.kind === "literal" ? node.value : n ?? "0";
  return <div className="friendly-value">
    {mode === "parameter" && node.kind === "reference" && <label>Какой параметр<select className="input" aria-label="Какой параметр" value={node.key} onChange={e => onChange(reference(e.target.value))}>
      {!context.parameters.some(p => p.key === node.key) && <option value={node.key}>Параметр из другой версии</option>}
      {context.parameters.map(p => <option key={p.key} value={p.key}>{p.name}</option>)}
    </select></label>}
    {mode === "number" && (expected?.type === 5 ? <label>Значение<select className="input" aria-label="Значение" value={literal} onChange={e => onChange(numberLiteral(e.target.value))}>
      {!options.some(o => o.value === literal) && <option value={literal}>Значение вне разрешённого списка</option>}
      {options.map(o => <option key={o.value} value={o.value}>{o.label}</option>)}
    </select></label> : <label>Число<NumericLiteralInput value={literal} onChange={value => onChange(numberLiteral(value))} /></label>)}
    {mode === "string" && <label>Значение<StringParameterInput name={expected?.name ?? "Текстовое значение"} value={literal}
      onChange={value => onChange({ kind: "literal", type: "string", value })} /></label>}
    {mode === "boolean" && <label>Значение<select className="input" aria-label="Значение" value={literal} onChange={e => onChange({ kind: "literal", type: "boolean", value: e.target.value })}><option value="true">Да</option><option value="false">Нет</option></select></label>}
    {mode === "calculation" && (node.kind === "binary" && arithmetic.some(([op]) => op === node.op) ? <div className="friendly-calculation">
      <ValueControls label="Исходное значение" node={node.left} onChange={left => onChange({ ...node, left })} context={context} />
      <label>Действие<select className="input" aria-label="Действие" value={node.op} onChange={e => onChange({ ...node, op: e.target.value })}>{arithmetic.map(([op, name]) => <option key={op} value={op}>{name}</option>)}</select></label>
      <ValueControls label="Второе значение" node={node.right} onChange={right => onChange({ ...node, right })} context={context} />
    </div> : <p className="hint">Это выражение доступно в дополнительном режиме ниже.</p>)}
    <details className="expression-source"><summary>{label}: изменить источник</summary>
    <label>{label}<select className="input" aria-label={label} value={mode} onChange={e => {
      if (e.target.value === "parameter") onChange(context.parameters[0] ? reference(context.parameters[0].key) : numberLiteral());
      else if (e.target.value === "calculation") onChange({ kind: "binary", op: "*", left: numericParameters[0] ? reference(numericParameters[0].key) : numberLiteral(), right: numberLiteral("2") });
      else if (e.target.value === "string") onChange({ kind: "literal", type: "string", value: /подключ/i.test(expected?.name ?? "") ? "wired" : "" });
      else onChange(defaultValue(expected?.type === 5 ? expected : undefined));
    }}>
      <option value="parameter" disabled={!context.parameters.length}>Параметр изделия</option>
      <option value="number">{expected?.type === 5 ? "Значение из списка" : "Число"}</option>
      <option value="string">Текстовое значение</option>
      <option value="calculation">Вычисление</option>
      {mode === "boolean" && <option value="boolean">Да / нет</option>}
    </select></label>
    </details>
  </div>;
}

function logicalChildren(node: Expression, op: string): Expression[] {
  return node.kind === "binary" && node.op === op ? [...logicalChildren(node.left, op), ...logicalChildren(node.right, op)] : [node];
}
function joinConditions(nodes: Expression[], op: string): Expression {
  return nodes.reduce((left, right) => ({ kind: "binary", op, left, right }));
}
function ConditionControls({ node, context, onChange }: { node: Expression; context: ExpressionContext; onChange: (e: Expression) => void }) {
  const inverted = node.kind === "unary" && node.op === "NOT";
  const core = inverted ? node.operand : node;
  const isGroup = core.kind === "binary" && ["AND", "OR", "&&", "||"].includes(core.op);
  const isPresence = core.kind === "reference" && core.key.startsWith("has_");
  const isComparison = core.kind === "binary" && comparisons.some(([op]) => op === core.op);
  const groupOp = isGroup && (core.op === "OR" || core.op === "||") ? "OR" : "AND";
  const kind = isGroup ? groupOp : isPresence ? "presence" : isComparison ? "compare" : core.kind === "literal" && core.type === "boolean" ? (core.value === "true" ? "always" : "never") : "advanced";
  function change(next: Expression) { onChange(inverted ? { kind: "unary", op: "NOT", operand: next } : next); }
  const leftOperand = isComparison ? core.left : null;
  const parameter = leftOperand?.kind === "reference" ? context.parameters.find(p => p.key === leftOperand.key) : undefined;
  return <div className="friendly-condition">
    {isGroup ? <>    <div className="bom-grid">
      <label>Что проверять<select className="input" aria-label="Что проверять" value={kind} onChange={e => {
        if (e.target.value === "compare") change(defaultCondition(context.parameters));
        else if (e.target.value === "presence") change(reference(`has_${context.components?.[0]?.id ?? 0}`));
        else if (e.target.value === "always" || e.target.value === "never") onChange({ kind: "literal", type: "boolean", value: e.target.value === "always" ? "true" : "false" });
        else if ((e.target.value === "AND" || e.target.value === "OR") && isGroup) change(setConditionJoin(core, e.target.value));
        else if (e.target.value === "AND" || e.target.value === "OR") change({ kind: "binary", op: e.target.value, left: core, right: defaultCondition(context.parameters) });
      }}>
        <option value="compare" disabled={!context.parameters.length}>Значение параметра</option>
        <option value="presence" disabled={!context.components?.length}>Наличие компонента</option>
        <option value="AND">Выполнены все условия</option>
        <option value="OR">Выполнено хотя бы одно условие</option>
        <option value="always">Всегда разрешено</option>
        <option value="never">Всегда запрещено</option>
        {kind === "advanced" && <option value="advanced">Сложное условие без изменений</option>}
      </select></label>
      <label className="checkbox-label"><input type="checkbox" checked={inverted} onChange={e => onChange(e.target.checked ? { kind: "unary", op: "NOT", operand: core } : core)} />{isGroup ? "Запретить это сочетание" : "Обратить условие"}</label>
    </div>
</> : <details className="expression-source"><summary>Другой тип условия / обратное условие</summary>    <div className="bom-grid">
      <label>Что проверять<select className="input" aria-label="Что проверять" value={kind} onChange={e => {
        if (e.target.value === "compare") change(defaultCondition(context.parameters));
        else if (e.target.value === "presence") change(reference(`has_${context.components?.[0]?.id ?? 0}`));
        else if (e.target.value === "always" || e.target.value === "never") onChange({ kind: "literal", type: "boolean", value: e.target.value === "always" ? "true" : "false" });
        else change({ kind: "binary", op: e.target.value, left: core, right: defaultCondition(context.parameters) });
      }}>
        <option value="compare" disabled={!context.parameters.length}>Значение параметра</option>
        <option value="presence" disabled={!context.components?.length}>Наличие компонента</option>
        <option value="AND">Выполнены все условия</option>
        <option value="OR">Выполнено хотя бы одно условие</option>
        <option value="always">Всегда разрешено</option>
        <option value="never">Всегда запрещено</option>
        {kind === "advanced" && <option value="advanced">Сложное условие без изменений</option>}
      </select></label>
      <label className="checkbox-label"><input type="checkbox" checked={inverted} onChange={e => onChange(e.target.checked ? { kind: "unary", op: "NOT", operand: core } : core)} />{isGroup ? "Запретить это сочетание" : "Обратить условие"}</label>
    </div>
</details>}
    {inverted && !isGroup && <label className="checkbox-label"><input type="checkbox" checked onChange={() => onChange(core)} />Запретить выбранное условие</label>}
    {isComparison && <div className="friendly-comparison">
      {core.left.kind === "reference" ? <label>Параметр<select className="input" aria-label="Параметр" value={core.left.key} onChange={e => {
        const p = context.parameters.find(p => p.key === e.target.value);
        change({ ...core, left: reference(e.target.value), right: defaultValue(p) });
      }}>
        {!parameter && <option value={core.left.key}>Параметр из другой версии</option>}
        {context.parameters.map(p => <option key={p.key} value={p.key}>{p.name}</option>)}
      </select></label> : <ValueControls label="Исходное значение" node={core.left} onChange={left => change({ ...core, left })} context={context} />}
      <label>Сравнение<select className="input" aria-label="Сравнение" value={core.op} onChange={e => change({ ...core, op: e.target.value })}>{comparisons.map(([op, label]) => <option key={op} value={op}>{label}</option>)}</select></label>
      <ValueControls label="С чем сравнить" node={core.right} onChange={right => change({ ...core, right })} context={context} expected={parameter} />
    </div>}
    {isPresence && core.kind === "reference" && <label>Компонент<select className="input" aria-label="Компонент правила" value={core.key} onChange={e => change(reference(e.target.value))}>
      {!context.components?.some(c => `has_${c.id}` === core.key) && <option value={core.key}>Компонент отсутствует в текущем составе</option>}
      {context.components?.map(c => <option key={c.id} value={`has_${c.id}`}>{c.name}</option>)}
    </select></label>}
    {isGroup && logicalChildren(core, core.op).map((child, i, children) => <fieldset key={i} className="condition-item"><legend>Условие {i + 1}</legend>
      <ConditionControls node={child} context={context} onChange={next => change(joinConditions(children.map((x, j) => i === j ? next : x), groupOp))} />
      <button type="button" className="btn-ghost" onClick={() => change(joinConditions(children.filter((_, j) => i !== j), groupOp))}>Убрать условие {i + 1}</button>
    </fieldset>)}
    {!isGroup && <button type="button" className="btn-ghost" onClick={() => onChange({ kind: "binary", op: "AND", left: node, right: defaultCondition(context.parameters) })}>Добавить ещё условие</button>}
    {isGroup && <button type="button" className="btn-secondary" onClick={() => change({ kind: "binary", op: groupOp, left: core, right: defaultCondition(context.parameters) })}>Добавить ещё условие</button>}
    {!isGroup && !isComparison && !isPresence && !(core.kind === "literal" && core.type === "boolean") && <p className="hint">Сложное условие сохранено. Для его изменения откройте дополнительный режим.</p>}
  </div>;
}

export function ExpressionField({ label, value, onChange, parameters, components = [], kind = "condition", resultParameter, emptyLabel }: {
  label: string; value: string; onChange: (s: string) => void; parameters: SpecParam[];
  components?: { id: number; name: string }[];
  kind?: "condition" | "formula" | "binding";
  resultParameter?: SpecParam; emptyLabel?: string;
}) {
  const [advanced, setAdvanced] = useState(false);
  const context = { parameters, components, resultParameter };
  const expression = parseExpression(value);
  const input = useRef<HTMLInputElement>(null);
  const inactive = emptyLabel ?? (kind === "condition" ? "Включать всегда" : kind === "binding" ? "Использовать значение по умолчанию" : "Вводить вручную / постоянный расход");
  function initialExpression() {
    if (kind === "condition") return defaultCondition(parameters);
    return initialValueExpression(parameters, resultParameter, kind);
  }
  return <fieldset className={advanced ? "friendly-expression" : "friendly-expression friendly-expression--simple"}><legend>{label}</legend>
    <label>Способ задания<select className="input" aria-label={`${label}: способ задания`} value={value.trim() ? "expression" : "default"} onChange={e => onChange(e.target.value === "default" ? "" : writeExpression(initialExpression()))}>
      <option value="default">{inactive}</option>
      <option value="expression">{kind === "condition" ? "Задать условие" : kind === "binding" ? "Передать значение" : "Рассчитать по параметрам"}</option>
    </select></label>
    {value.trim() && <p className="expression-reading">{describeExpression(value, context)}</p>}
    {expression && (kind === "condition" ? <ConditionControls node={expression} context={context} onChange={e => onChange(writeExpression(e))} /> : <ValueControls label="Получить значение" node={expression} context={context} expected={resultParameter} onChange={e => onChange(writeExpression(e))} />)}
    {value.trim() && !expression && <p className="hint">Выражение сохранено без изменений. Его текст доступен ниже.</p>}
    <details className="expression-advanced" onToggle={e => setAdvanced(e.currentTarget.open)}><summary>Дополнительно: другие способы задания</summary>
      <p className="hint">Для сложных условий и вычислений. Открыты дополнительные источники значений и типы условий в полях выше. Для обычной настройки этот раздел не нужен. Исходные обозначения параметров:</p>
      <ul>{parameters.map(p => <li key={p.key}>{p.name}: <code>{p.key}</code></li>)}{components.map(c => <li key={c.id}>{c.name} входит в состав: <code>has_{c.id}</code></li>)}</ul>
      <label>Исходное выражение<input ref={input} className="input mono" value={value} onChange={e => onChange(e.target.value)} /></label>
      <label>Вставить обозначение<select className="input" aria-label="Вставить обозначение" value="" onChange={e => {
        const start = input.current?.selectionStart ?? value.length;
        const end = input.current?.selectionEnd ?? value.length;
        onChange(value.slice(0, start) + e.target.value + value.slice(end));
        input.current?.focus();
      }}><option value="">— выбрать —</option>{parameters.map(p => <option key={p.key} value={p.key}>{p.name}</option>)}{components.map(c => <option key={c.id} value={`has_${c.id}`}>{c.name}</option>)}</select></label>
      <p className="hint">Числа, строки в двойных кавычках, сравнения == != &lt; &gt; &lt;= &gt;=, AND OR NOT, арифметика + - * / % и скобки.</p>
    </details>
  </fieldset>;
}

export function StringParameterInput({ name, value, onChange, placeholder = "Не задано" }: {
  name: string; value: string; onChange: (value: string) => void; placeholder?: string;
}) {
  const [custom, setCustom] = useState(false);
  if (!/подключ/i.test(name) || custom) return <input className="input" aria-label={name} value={value} placeholder={placeholder} onChange={e => onChange(e.target.value)} />;
  return <select className="input" aria-label={name} value={value} onChange={e => {
    if (e.target.value === "__custom__") { setCustom(true); onChange(""); }
    else onChange(e.target.value);
  }}>
    <option value="">{placeholder}</option>
    {connectionOptions.map(o => <option key={o.value} value={o.value}>{o.label}</option>)}
    {value && !connectionOptions.some(o => o.value === value) && <option value={value}>{value}</option>}
    <option value="__custom__">Другое значение…</option>
  </select>;
}
