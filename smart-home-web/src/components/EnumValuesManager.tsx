import { useEffect, useState, type FormEvent } from "react";
import { apiDelete, apiGet, apiPost, apiPut } from "../api/client";
import type { EnumClassDto, EnumValueDto } from "../types/api";

export function IconValue({ name }: { name: string | null }) {
  const icons: Record<string, string> = { lightbulb: "💡", thermostat: "🌡️", camera: "📷", plug: "🔌", lock: "🔒", home: "🏠" };
  return name ? <span title={name} aria-label={name}>{icons[name] ?? "◆"}</span> : null;
}

export function EnumValuesManager({ enumClass }: { enumClass: EnumClassDto }) {
  const [values, setValues] = useState<EnumValueDto[] | null>(null);
  const [value, setValue] = useState("");
  const [display, setDisplay] = useState("");
  const [order, setOrder] = useState(1);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");
  const path = `/api/enum-classes/${enumClass.id}/values`;
  useEffect(() => {
    let active = true;
    apiGet<EnumValueDto[]>(path).then(v => { if (active) setValues(v); }).catch(e => { if (active) setError(String(e.message)); });
    return () => { active = false; };
  }, [path]);
  async function run(action: () => Promise<unknown>) {
    setBusy(true); setError("");
    try { await action(); setValues(await apiGet<EnumValueDto[]>(path)); }
    catch (e) { setError(e instanceof Error ? e.message : "Ошибка сохранения"); }
    finally { setBusy(false); }
  }
  function add(e: FormEvent) {
    e.preventDefault();
    void run(async () => {
      await apiPost(path, { stringValue: enumClass.valueType === 1 ? value : null, numberValue: enumClass.valueType === 2 ? Number(value) : null, iconValue: enumClass.valueType === 3 ? value : null, displayName: display || value, sortOrder: order });
      setValue(""); setDisplay(""); setOrder(order + 1);
    });
  }
  return <section className="card-nested">
    <h3>Значения: {enumClass.name}</h3>
    {error && <div className="error-banner" role="alert">{error}</div>}
    {values === null ? <p>Загрузка…</p> : !values.length ? <p className="hint">Значений пока нет.</p> : values.map((v, i) => <div key={v.id} style={{ display: "flex", flexWrap: "wrap", alignItems: "center", gap: ".5rem", marginBottom: ".5rem" }}>
      <IconValue name={v.iconValue} /><span>{v.displayName ?? v.stringValue ?? v.numberValue ?? v.iconValue}</span>
      <span className="hint">Порядок: {v.sortOrder}</span>
      <button type="button" disabled={busy || i === 0} onClick={() => void run(() => apiPut(`${path}/${v.id}/sort-order`, { sortOrder: values[i - 1]!.sortOrder }))}>Выше</button>
      <button type="button" disabled={busy || i === values.length - 1} onClick={() => void run(() => apiPut(`${path}/${v.id}/sort-order`, { sortOrder: values[i + 1]!.sortOrder }))}>Ниже</button>
      <button type="button" className="btn-danger" disabled={busy} onClick={() => { if (confirm("Удалить значение перечисления?")) void run(() => apiDelete(`${path}/${v.id}`)); }}>Удалить</button>
    </div>)}
    <form onSubmit={add} style={{ display: "grid", gap: ".5rem" }}>
      <label>Значение{enumClass.valueType === 3 && " (lightbulb, thermostat, camera, plug, lock, home)"}<input className="input" required maxLength={512} type={enumClass.valueType === 2 ? "number" : "text"} step="any" value={value} onChange={e => setValue(e.target.value)} /></label>
      <label>Подпись<input className="input" maxLength={512} value={display} onChange={e => setDisplay(e.target.value)} /></label>
      <label>Порядок<input className="input" type="number" required value={order} onChange={e => setOrder(Number(e.target.value))} /></label>
      <button className="btn-primary" disabled={busy}>Добавить значение</button>
    </form>
    <p className="hint">Используемые значения сохраняют смысл: для изменения добавьте новый вариант. Удаление используемого варианта запрещено.</p>
  </section>;
}
