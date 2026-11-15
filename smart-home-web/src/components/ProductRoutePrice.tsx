import { useEffect, useState } from "react";
import { apiGet, apiPut } from "../api/client";
import type { ProductDto } from "../types/api";
import type { Choice, RouteRow } from "../types/routes";
import { useUnsavedChanges } from "./UnsavedChanges";
export function ProductRoutePrice({
  product,
  onSaved,
  onOpenRoute,
}: {
  product: ProductDto;
  onSaved: () => Promise<void>;
  onOpenRoute: () => void;
}) {
  const [price, setPrice] = useState(product.purchasePrice?.toString() ?? ""),
    [unit, setUnit] = useState("единицу"),
    [hasRoute, setHasRoute] = useState(false),
    [err, setErr] = useState(""),
    [busy, setBusy] = useState(false);
  useUnsavedChanges(price !== (product.purchasePrice?.toString() ?? ""));
  useEffect(() => {
    let active = true;
    void Promise.all([
      apiGet<(Choice & { shortName: string })[]>("/api/measure-units"),
      apiGet<RouteRow[]>(`/api/routes?productId=${product.id}`),
    ])
      .then(([u, r]) => {
        if (active) {
          setUnit(
            u.find((x) => x.id === product.unitId)?.shortName ?? "единицу",
          );
          setHasRoute(r.length > 0);
        }
      })
      .catch((e) => {
        if (active) setErr(String(e));
      });
    return () => {
      active = false;
    };
  }, [product.id, product.unitId]);
  return (
    <section className="card">
      <label>
        Закупочная цена, ₽ за {unit}
        <input
          className="input"
          type="number"
          min="0"
          step="any"
          value={price}
          onChange={(e) => setPrice(e.target.value)}
          placeholder="Не задана"
        />
      </label>
      <p className="hint">
        Пустое поле — цена не задана. Нулевая цена учитывается как 0 ₽.
      </p>
      <div className="route-actions">
        <button
          className="btn-secondary"
          disabled={busy}
          onClick={() => {
            setBusy(true);
            setErr("");
            void apiPut(`/api/products/${product.id}/price`, {
              purchasePrice: price === "" ? null : Number(price),
            })
              .then(onSaved)
              .catch((e) => setErr(String(e)))
              .finally(() => setBusy(false));
          }}
        >
          Сохранить цену
        </button>
        <button className="btn-ghost" onClick={onOpenRoute}>
          {hasRoute ? "Открыть маршрут" : "Создать маршрут"}
        </button>
      </div>
      {err && <div className="error-banner">{err}</div>}
    </section>
  );
}
