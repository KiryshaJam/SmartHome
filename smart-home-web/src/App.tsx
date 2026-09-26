import { useState } from "react";
import { AppShell, type AppView } from "./components/AppShell";
import { ClassifierPage } from "./pages/ClassifierPage";
import { Dashboard } from "./pages/Dashboard";
import { DevicesPage } from "./pages/DevicesPage";
import { SchemaPage } from "./pages/SchemaPage";
import { SearchPage } from "./pages/SearchPage";

export default function App() {
  const [productId, setProductId] = useState<number | null>(null);
  const [view, setView] = useState<AppView>("dashboard");

  return (
    <AppShell active={view} onNavigate={setView}>
      {view === "dashboard" && <Dashboard onNavigate={setView} />}
      {view === "classifier" && <ClassifierPage />}
      {view === "schema" && <SchemaPage />}
      {view === "devices" && <DevicesPage initialId={productId} />}
      {view === "search" && <SearchPage onOpenProduct={id => { setProductId(id); setView("devices"); }} />}
    </AppShell>
  );
}
