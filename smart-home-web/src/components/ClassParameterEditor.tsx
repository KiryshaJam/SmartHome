import { useState } from "react";
import { apiPut } from "../api/client";
import type { ClassParameterDto, ParameterGroupDto } from "../types/api";

export function ClassParameterEditor({ parameter: p, groups, onSaved }: { parameter: ClassParameterDto; groups: ParameterGroupDto[]; onSaved: () => Promise<void> }) {
  const [open, setOpen] = useState(false);
  const [group, setGroup] = useState(p.parameterGroupId?.toString() ?? "");
  const [order, setOrder] = useState(p.sortOrder);
  const [required, setRequired] = useState(p.isRequired);
  const [min, setMin] = useState(p.minNumberValue?.toString() ?? "");
  const [max, setMax] = useState(p.maxNumberValue?.toString() ?? "");
  const [error, setError] = useState("");
  const [busy, setBusy] = useState(false);
  return <><button type="button" onClick={() => setOpen(!open)}>Настройки</button>{open && <form style={{ display: "grid", gap: ".5rem", minWidth: "180px" }} onSubmit={e => {
    e.preventDefault(); setBusy(true); setError("");
    void apiPut(`/api/classnodes/${p.classNodeId}/parameters/${p.id}`, { parameterGroupId: group ? Number(group) : null, sortOrder: order, isRequired: required, minNumberValue: min === "" ? null : Number(min), maxNumberValue: max === "" ? null : Number(max) })
      .then(onSaved).then(() => setOpen(false)).catch(e => setError(e.message)).finally(() => setBusy(false));
  }}>
    {error && <div role="alert" className="error-banner">{error}</div>}
    <label>Группа<select className="input" value={group} onChange={e => setGroup(e.target.value)}><option value="">Общие</option>{groups.map(g => <option key={g.id} value={g.id}>{g.name}</option>)}</select></label>
    <label>Порядок<input className="input" type="number" value={order} onChange={e => setOrder(Number(e.target.value))} /></label>
    <label><input type="checkbox" checked={required} onChange={e => setRequired(e.target.checked)} /> Обязательный</label>
    {(p.valueType === 1 || p.valueType === 2) && <><label>Минимум<input className="input" type="number" step="any" value={min} onChange={e => setMin(e.target.value)} /></label><label>Максимум<input className="input" type="number" step="any" value={max} onChange={e => setMax(e.target.value)} /></label></>}
    <button disabled={busy} className="btn-primary">Сохранить настройки</button>
  </form>}</>;
}
