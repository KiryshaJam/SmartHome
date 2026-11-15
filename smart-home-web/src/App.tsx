import { RoutesPage } from "./pages/RoutesPage";
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
  const [routeProductId, setRouteProductId] = useState<number | null>(null);
  const [view, setView] = useState<AppView>("dashboard");

  const navigate = (v: AppView) => {
    if ((v !== view || (v === "routes" && routeProductId !== null)) && confirmLeave()) {
      if (v === "routes") setRouteProductId(null);
      setView(v);
    }
  };

  return (
    <AppShell active={view} onNavigate={navigate}>
      {view === "dashboard" && <Dashboard onNavigate={navigate} />}
      {view === "classifier" && <ClassifierPage />}
      {view === "routes" && <RoutesPage initialProductId={routeProductId} />}
      {view === "orders" && <OrdersPage />}
      {view === "schema" && <SchemaPage />}
      {view === "devices" && <DevicesPage initialId={productId} onOpenRoute={id => { if(confirmLeave()){setRouteProductId(id);setView("routes");} }} />}
      {view === "search" && <SearchPage onOpenProduct={id => { setProductId(id); setView("devices"); }} />}
    </AppShell>
  );
}
