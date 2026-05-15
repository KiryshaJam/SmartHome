import { useEffect, useState } from "react";
import { apiGet } from "../api/client";
import type { ClassNodeDto, ProductDto } from "../types/api";
import type { AppView } from "./AppShell";

export function Dashboard({ onNavigate }: { onNavigate: (v: AppView) => void }) {
  const [products, setProducts] = useState<ProductDto[] | null>(null);
  const [classes, setClasses] = useState<ClassNodeDto[] | null>(null);
  const [err, setErr] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    (async () => {
      try {
        const [p, c] = await Promise.all([
          apiGet<ProductDto[]>("/api/products"),
          apiGet<ClassNodeDto[]>("/api/classnodes"),
        ]);
        if (!cancelled) {
          setProducts(p);
          setClasses(c);
        }
      } catch (e) {
        if (!cancelled) setErr(e instanceof Error ? e.message : "Ошибка загрузки");
      }
    })();
    return () => {
      cancelled = true;
    };
  }, []);

  return (
    <div style={{ display: "flex", flexDirection: "column", gap: "1.25rem" }}>
      <header>
        <h1 style={{ margin: 0, fontSize: "1.75rem", fontWeight: 700 }}>Обзор</h1>
      </header>
      {err && <div className="error-banner">{err}</div>}
      <div
        style={{
          display: "grid",
          gridTemplateColumns: "repeat(auto-fill, minmax(220px, 1fr))",
          gap: "1rem",
        }}
      >
        <div className="card" style={{ padding: "1.15rem" }}>
          <div className="hint">Устройств в каталоге</div>
          <div style={{ fontSize: "2rem", fontWeight: 700, marginTop: "0.25rem" }}>
            {products ? products.length : "—"}
          </div>
        </div>
        <div className="card" style={{ padding: "1.15rem" }}>
          <div className="hint">Классов в иерархии</div>
          <div style={{ fontSize: "2rem", fontWeight: 700, marginTop: "0.25rem" }}>
            {classes ? classes.length : "—"}
          </div>
        </div>
      </div>
      <div className="card" style={{ padding: "1.15rem" }}>
        <div style={{ fontWeight: 600, marginBottom: "0.75rem" }}>Быстрые переходы</div>
        <div style={{ display: "flex", flexWrap: "wrap", gap: "0.5rem" }}>
          <button type="button" className="btn-ghost" onClick={() => onNavigate("classifier")}>
            Классификатор
          </button>
          <button type="button" className="btn-primary" onClick={() => onNavigate("schema")}>
            Справочники параметров
          </button>
          <button type="button" className="btn-warm" onClick={() => onNavigate("devices")}>
            Каталог устройств
          </button>
          <button type="button" className="btn-ghost" onClick={() => onNavigate("search")}>
            Поиск с фильтром
          </button>
        </div>
      </div>
    </div>
  );
}
