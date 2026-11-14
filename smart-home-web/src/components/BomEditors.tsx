import { stringLabel } from "../utils/bomExpressions";
import { StringParameterInput } from "./FriendlyExpression";
import { useState } from "react";
import type { SpecParam } from "./BomPanel";
import { isoToDateTimeLocal, formatDateTimeLocal } from "../utils/datetime";

const types: Record<number, string> = { 1: "Целое число", 2: "Число", 3: "Строка", 4: "Дата и время", 5: "Перечисление" };
export function parameterValue(p: SpecParam, value: string | number | null | undefined) {
  if (value == null) return "не задано";
  if (p.type === 5) return p.enumLabels?.[String(value)] ?? String(value);
  if (p.type === 4) return new Date(String(value)).toLocaleString("ru-RU");
  return p.type === 3 && /подключ/i.test(p.name) ? stringLabel(String(value)) : String(value);
}

export function SpecParameters({ parameters, options, onChange, readOnly = false, canRemove }: {
  parameters: SpecParam[];
  options: Record<string, Record<string, string>>;
  onChange: (parameters: SpecParam[]) => void;
  readOnly?: boolean;
  canRemove?: (p: SpecParam) => boolean;
}) {
  function change(index: number, patch: Partial<SpecParam>) {
    onChange(parameters.map((p, i) => i === index ? { ...p, ...patch } : p));
  }
  return <>
    <p className="hint">Укажите, что пользователь сможет выбрать при заказе изделия. Здесь можно ограничить диапазон, разрешить значения из списка и задать исходные значения.</p>
    {!parameters.length && <p className="hint">У изделия нет настраиваемых параметров. Можно перейти к компонентам; для обычного материала параметры не нужны.</p>}
    {parameters.map((p, i) => <fieldset className="card-nested bom-form" key={p.key} disabled={readOnly}>
      <legend>{p.name} · {types[p.type]}</legend>
      <label className="checkbox-label"><input type="checkbox" checked={p.required} onChange={e => change(i, { required: e.target.checked })} /> Обязателен для исполнения</label>
      {(p.type === 1 || p.type === 2) && <div className="bom-grid">
        <label>Минимум<input className="input" type="number" step={p.type === 1 ? "1" : "any"} value={p.min ?? ""} onChange={e => change(i, { min: e.target.value === "" ? null : Number(e.target.value) })} /></label>
        <label>Максимум<input className="input" type="number" step={p.type === 1 ? "1" : "any"} value={p.max ?? ""} onChange={e => change(i, { max: e.target.value === "" ? null : Number(e.target.value) })} /></label>
      </div>}
      {p.type === 5 && <div>
        <p>Допустимые значения (снимите отметку, чтобы запретить)</p>
        {Object.entries({ ...p.enumLabels, ...options[p.key] }).map(([id, label]) => <label className="checkbox-label" key={id}>
          <input type="checkbox" checked={p.enumIds.includes(Number(id))} onChange={e => change(i, { enumIds: e.target.checked ? [...p.enumIds, Number(id)] : p.enumIds.filter(x => x !== Number(id)), enumLabels: { ...p.enumLabels, ...options[p.key] } })} /> {label}
        </label>)}
        {!p.enumIds.length && <p className="hint">Ни одного допустимого значения: заполнить этот параметр будет нельзя.</p>}
      </div>}
      <label>Значение по умолчанию
        {p.type === 5 ? <select className="input" value={p.default ?? ""} onChange={e => change(i, { default: e.target.value === "" ? null : Number(e.target.value) })}>
          <option value="">Не задано</option>
          {p.enumIds.map(id => <option key={id} value={id}>{options[p.key]?.[id] ?? p.enumLabels?.[id] ?? id}</option>)}
          {p.default != null && !p.enumIds.includes(Number(p.default)) && <option value={p.default}>Вне допустимых значений: {p.default}</option>}
        </select> : p.type === 3 ? <StringParameterInput name={`Значение по умолчанию: ${p.name}`} value={p.default == null ? "" : String(p.default)} onChange={value => change(i, { default: value || null })} /> : <input className="input" type={p.type === 1 || p.type === 2 ? "number" : p.type === 4 ? "datetime-local" : "text"}
          step={p.type === 1 ? "1" : "any"} value={p.type === 4 ? isoToDateTimeLocal(p.default == null ? null : String(p.default)) : p.default ?? ""}
          onChange={e => change(i, { default: e.target.value === "" ? null : p.type === 1 || p.type === 2 ? Number(e.target.value) : p.type === 4 ? formatDateTimeLocal(e.target.value) : e.target.value })} />}
      </label>
      <p className="hint">Оставьте пустым, если значение должен выбрать пользователь. Для вычисляемого параметра значение задаёт формула.</p>
      {!readOnly && <details><summary>Не использовать этот параметр</summary>
        <p className="hint">Параметр останется в справочнике, но не будет предлагаться при подборе этого изделия. Его можно вернуть кнопкой подключения параметров.</p>
        <button type="button" className="btn-ghost" disabled={canRemove ? !canRemove(p) : false} onClick={() => onChange(parameters.filter((_, j) => j !== i))}>Убрать «{p.name}» из настройки</button>
        {canRemove && !canRemove(p) && <p className="hint">Сначала уберите использование этого параметра из условий и формул.</p>}
      </details>}
    </fieldset>)}
  </>;
}

export interface ResolvedNode {
  productId: number; name: string; revisionId: number; unit: string; amount: number; forQuantity: number;
  quantity: number; values: Record<string, string | number>; source?: { parameters: SpecParam[] };
  children: ResolvedNode[];
}
export function ConfigurationResult({ node }: { node: ResolvedNode }) {
  const rows: { node: ResolvedNode; depth: number; quantity: number; path: string }[] = [];
  function visit(n: ResolvedNode, depth: number, quantity: number, path: string) {
    rows.push({ node: n, depth, quantity, path });
    n.children.forEach((child, i) => visit(child, depth + 1, quantity * child.quantity, `${path}.${i}`));
  }
  visit(node, 0, 1, "0");
  return <div className="table-scroll"><table className="data data-table">
    <caption>Конфигурация на одну единицу изделия: состав и фактические параметры</caption>
    <thead><tr><th>Компонент / версия</th><th>Количество</th><th>Параметры, включая вычисленные</th></tr></thead>
    <tbody>{rows.map(r => <tr key={r.path}>
      <td style={{ paddingLeft: `${.8 + r.depth * 1.2}rem` }}>{r.depth > 0 ? "↳ " : ""}{r.node.name} · версия {r.node.revisionId}</td>
      <td>{r.quantity} {r.node.unit}</td>
      <td>{Object.entries(r.node.values).map(([key, value]) => {
        const p = r.node.source?.parameters.find(p => p.key === key);
        return <div key={key}>{p?.name ?? "Сохранённый параметр"}: {p ? parameterValue(p, value) : String(value)}</div>;
      })}</td>
    </tr>)}</tbody>
  </table></div>;
}

export function CompatibilityBuilder({ components, onAdd }: {
  components: { id: number; name: string }[];
  onAdd: (rule: { expression: string; message: string }) => void;
}) {
  const [kind, setKind] = useState("incompatible");
  const [first, setFirst] = useState(0);
  const [second, setSecond] = useState(0);
  const a = components.find(c => c.id === first);
  const b = components.find(c => c.id === second);
  return <details className="card-nested">
    <summary>Быстрое правило совместимости</summary>
    <div className="bom-grid">
      <label>Правило<select className="input" value={kind} onChange={e => setKind(e.target.value)}>
        <option value="incompatible">Нельзя использовать вместе</option>
        <option value="required">Первый требует второй</option>
      </select></label>
      <label>Первый компонент<select className="input" value={first} onChange={e => setFirst(Number(e.target.value))}>
        <option value="0">— выбрать —</option>{components.map(c => <option key={c.id} value={c.id}>{c.name}</option>)}
      </select></label>
      <label>Второй компонент<select className="input" value={second} onChange={e => setSecond(Number(e.target.value))}>
        <option value="0">— выбрать —</option>{components.map(c => <option key={c.id} value={c.id}>{c.name}</option>)}
      </select></label>
      <button type="button" className="btn-secondary" disabled={!a || !b || first === second} onClick={() => {
        if (!a || !b) return;
        onAdd(kind === "incompatible"
          ? { expression: `NOT (has_${a.id} AND has_${b.id})`, message: `${a.name} и ${b.name} нельзя использовать вместе.` }
          : { expression: `NOT has_${a.id} OR has_${b.id}`, message: `${a.name} требует компонент «${b.name}».` });
      }}>Добавить правило совместимости</button>
    </div>
    <p className="hint">Правило проверяет включённые компоненты этой сборки. После добавления можно изменить выражение и текст отказа ниже.</p>
  </details>;
}
