import { useEffect, useState } from "react";
import { apiDelete, apiGet, apiPut } from "../api/client";
import type { ClassNodeDto } from "../types/api";

export function ClassNodeActions({ id, onSaved }: { id: number; onSaved: () => Promise<void> }) {
  const [nodes, setNodes] = useState<ClassNodeDto[]>([]);
  const [parent, setParent] = useState("");
  const [order, setOrder] = useState(0);
  const [error, setError] = useState("");
  const [busy, setBusy] = useState(false);
  useEffect(() => { let active = true; apiGet<ClassNodeDto[]>("/api/classnodes").then(data => { if (active) { setNodes(data); const n = data.find(n => n.id === id); setParent(n?.parentId?.toString() ?? ""); setOrder(n?.sortOrder ?? 0); } }).catch(e => { if (active) setError(e.message); }); return () => { active = false; }; }, [id]);
  async function run(fn: () => Promise<unknown>) { setBusy(true); setError(""); try { await fn(); await onSaved(); } catch (e) { setError(e instanceof Error ? e.message : "Ошибка"); } finally { setBusy(false); } }
  return <section className="card" style={{ display: "grid", gap: ".6rem" }}>
    <h3>Изменение выбранного класса</h3>
    {error && <div className="error-banner" role="alert">{error}</div>}
    <label>Новый родитель<select className="input" value={parent} onChange={e => setParent(e.target.value)}><option value="">Корень</option>{nodes.filter(n => n.id !== id && !n.isTerminal).map(n => <option key={n.id} value={n.id}>{n.name}</option>)}</select></label>
    <button disabled={busy} onClick={() => void run(() => apiPut(`/api/classnodes/${id}/move`, { newParentId: parent ? Number(parent) : null }))}>Перенести класс</button>
    <label>Порядок<input className="input" type="number" value={order} onChange={e => setOrder(Number(e.target.value))} /></label>
    <button disabled={busy} onClick={() => void run(() => apiPut(`/api/classnodes/${id}/sort-order`, { sortOrder: order }))}>Сохранить порядок</button>
    <button disabled={busy} className="btn-danger" onClick={() => { if (confirm("Удалить выбранный класс?")) void run(() => apiDelete(`/api/classnodes/${id}`)); }}>Удалить класс</button>
  </section>;
}
