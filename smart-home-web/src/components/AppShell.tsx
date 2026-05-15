import type { ReactNode } from "react";

export type AppView = "dashboard" | "classifier" | "schema" | "devices" | "search";

const nav: { id: AppView; label: string; desc: string }[] = [
  { id: "dashboard", label: "Обзор", desc: "Сводка и быстрые действия" },
  { id: "classifier", label: "Классификатор", desc: "Иерархия классов и новые узлы" },
  { id: "schema", label: "Справочники", desc: "Группы, свойства и привязка к классам" },
  { id: "devices", label: "Устройства", desc: "Каталог и параметры изделий" },
  { id: "search", label: "Поиск", desc: "Фильтрация по классу и параметрам" },
];

export function AppShell({
  active,
  onNavigate,
  children,
}: {
  active: AppView;
  onNavigate: (v: AppView) => void;
  children: ReactNode;
}) {
  return (
    <div
      style={{
        display: "grid",
        gridTemplateColumns: "280px 1fr",
        minHeight: "100vh",
      }}
    >
      <aside
        className="card"
        style={{
          margin: "1.25rem",
          marginRight: 0,
          padding: "1.35rem",
          display: "flex",
          flexDirection: "column",
          gap: "1.25rem",
          borderRadius: "var(--radius)",
        }}
      >
        <div>
          <div style={{ fontSize: "1.35rem", fontWeight: 700, letterSpacing: "-0.02em" }}>
            Умный дом
          </div>
          <div className="hint" style={{ marginTop: "0.25rem" }}>
            Каталог и параметры изделий
          </div>
        </div>
        <nav style={{ display: "flex", flexDirection: "column", gap: "0.35rem" }}>
          {nav.map((item) => {
            const isActive = item.id === active;
            return (
              <button
                key={item.id}
                type="button"
                onClick={() => onNavigate(item.id)}
                className="btn-ghost"
                style={{
                  textAlign: "left",
                  padding: "0.75rem 0.85rem",
                  border: isActive
                    ? "1px solid rgba(56, 189, 248, 0.45)"
                    : "1px solid var(--border)",
                  background: isActive ? "var(--accent-soft)" : "transparent",
                }}
              >
                <div style={{ fontWeight: 600 }}>{item.label}</div>
                <div className="hint" style={{ fontSize: "0.78rem" }}>
                  {item.desc}
                </div>
              </button>
            );
          })}
        </nav>
        <div style={{ marginTop: "auto", fontSize: "0.78rem", color: "var(--text-muted)" }}>
          API:{" "}
          <span className="mono">
            {import.meta.env.DEV ? "proxy /api → localhost:5156" : import.meta.env.VITE_API_URL ?? "—"}
          </span>
        </div>
      </aside>
      <main style={{ padding: "1.25rem", minWidth: 0 }}>{children}</main>
    </div>
  );
}
