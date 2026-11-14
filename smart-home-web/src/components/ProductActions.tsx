import { useEffect, useState } from "react";
import { apiGet, apiPut } from "../api/client";
import type { ClassNodeDto, ProductWithParametersDto } from "../types/api";

export function ProductActions({ card, onSaved }: { card: ProductWithParametersDto; onSaved: () => Promise<void> }) {
  const [classes, setClasses] = useState<ClassNodeDto[]>([]);
  const [target, setTarget] = useState(card.classNodeId);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");
  useEffect(() => { let active = true; apiGet<ClassNodeDto[]>("/api/classnodes/leaves").then(c => { if (active) setClasses(c); }).catch(e => { if (active) setError(e.message); }); return () => { active = false; }; }, []);
  async function run(action: () => Promise<unknown>) { setBusy(true); setError(""); try { await action(); await onSaved(); } catch (e) { setError(e instanceof Error ? e.message : "Ошибка"); } finally { setBusy(false); } }
  return <section className="card-nested" style={{ display: "grid", gap: ".5rem" }}>
    {error && <div className="error-banner" role="alert">{error}</div>}
    <label>Перенести в класс<select className="input" value={target} onChange={e => setTarget(Number(e.target.value))}>{classes.map(c => <option key={c.id} value={c.id}>{c.name}</option>)}</select></label>
    <button disabled={busy || target === card.classNodeId} onClick={() => void run(() => apiPut(`/api/products/${card.id}/move`, { newClassNodeId: target }))}>Перенести изделие</button>
  </section>;
}
