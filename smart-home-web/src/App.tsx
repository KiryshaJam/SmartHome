import { UnsavedChangesProvider, useConfirmLeave } from "./components/UnsavedChanges";
import { OrdersPage } from "./pages/OrdersPage";
import { useState } from "react";
import { AppShell, type AppView } from "./components/AppShell";
import { ClassifierPage } from "./pages/ClassifierPage";
import { Dashboard } from "./pages/Dashboard";
import { DevicesPage } from "./pages/DevicesPage";
import { SchemaPage } from "./pages/SchemaPage";
import { SearchPage } from "./pages/SearchPage";

export default function App() {
  return <UnsavedChangesProvider><Application /></UnsavedChangesProvider>;
}

function Application() {
  const confirmLeave = useConfirmLeave();
  const [productId, setProductId] = useState<number | null>(null);
  const [view, setView] = useState<AppView>("dashboard");

  return (
    <AppShell active={view} onNavigate={v => { if (v !== view && confirmLeave()) setView(v); }}>
      {view === "dashboard" && <Dashboard onNavigate={v => { if (v !== view && confirmLeave()) setView(v); }} />}
      {view === "classifier" && <ClassifierPage />}
      {view === "orders" && <OrdersPage />}
      {view === "schema" && <SchemaPage />}
      {view === "devices" && <DevicesPage initialId={productId} />}
      {view === "search" && <SearchPage onOpenProduct={id => { setProductId(id); setView("devices"); }} />}
    </AppShell>
  );
}
