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
    <div className="app-layout">
      <aside className="app-sidebar">
        <div>
          <div className="sidebar-brand">Умный дом</div>
          <div className="sidebar-tagline">Каталог и параметры изделий</div>
        </div>
        <nav className="app-nav">
          {nav.map((item) => {
            const isActive = item.id === active;
            return (
              <button
                key={item.id}
                type="button"
                onClick={() => onNavigate(item.id)}
                className={isActive ? "nav-item nav-item--active" : "nav-item"}
              >
                <div className="nav-item__label">{item.label}</div>
                <div className="nav-item__desc">{item.desc}</div>
              </button>
            );
          })}
        </nav>
        <div className="sidebar-api">
          API:{" "}
          <span className="mono">
            {import.meta.env.DEV ? "proxy /api → localhost:5156" : import.meta.env.VITE_API_URL ?? "—"}
          </span>
        </div>
      </aside>
      <main className="app-main">{children}</main>
    </div>
  );
}
