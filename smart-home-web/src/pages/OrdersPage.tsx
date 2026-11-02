import { useEffect, useState } from "react";
import { apiGet, apiPost, apiPut } from "../api/client";
import {
  TotalsTable,
  VariantFields,
  variantValues,
  type Spec,
  type Total,
  type Variant,
} from "../components/BomPanel";
import type { ClassNodeDto, ProductDto } from "../types/api";
interface Order {
  id: number;
  number: string;
  date: string;
}
interface Line {
  variantId: number;
  quantity: number;
}
interface History {
  id: number;
  previousId: number | null;
  reason: string;
  createdAt: string;
  lines: string;
}
interface Detail {
  order: Order;
  history: History[];
}
interface Tree {
  path: string;
  depth: number;
  name: string;
  unit: string;
  required_quantity: number;
}
export function OrdersPage() {
  const [orders, setOrders] = useState<Order[]>([]),
    [variants, setVariants] = useState<Variant[]>([]),
    [classes, setClasses] = useState<ClassNodeDto[]>([]),
    [products, setProducts] = useState<ProductDto[]>([]);
  const [detail, setDetail] = useState<Detail | null>(null),
    [number, setNumber] = useState(""),
    [date, setDate] = useState(new Date().toLocaleDateString("en-CA")),
    [lines, setLines] = useState<Line[]>([]),
    [reason, setReason] = useState("");
  const [error, setError] = useState(""),
    [notice, setNotice] = useState(""),
    [busy, setBusy] = useState(false),
    [selectedHistory, setSelectedHistory] = useState<History | null>(null),
    [totals, setTotals] = useState<Total[]>([]),
    [tree, setTree] = useState<Tree[]>([]),
    [classId, setClassId] = useState("");
  const [productId, setProductId] = useState(0),
    [spec, setSpec] = useState<Spec | null>(null),
    [values, setValues] = useState<Record<string, string | number>>({}),
    [variantName, setVariantName] = useState("");
  async function refresh() {
    const [o, v, p, c] = await Promise.all([
      apiGet<Order[]>("/api/bom/orders"),
      apiGet<Variant[]>("/api/bom/variants"),
      apiGet<ProductDto[]>("/api/products"),
      apiGet<ClassNodeDto[]>("/api/classnodes"),
    ]);
    setOrders(o);
    setVariants(v);
    setProducts(p);
    setClasses(c);
  }
  async function run(f: () => Promise<void>) {
    setError("");
    setNotice("");
    setBusy(true);
    try {
      await f();
    } catch (e) {
      setError(e instanceof Error ? e.message : "Ошибка");
    } finally {
      setBusy(false);
    }
  }
  useEffect(() => {
    void run(refresh);
  }, []);
  useEffect(() => {
    let active = true;
    setSpec(null);
    setValues({});
    if (productId)
      void apiGet<Spec>(`/api/bom/products/${productId}`)
        .then((s) => {
          if (active) setSpec(s);
        })
        .catch((e) => {
          if (active) setError(String(e));
        });
    return () => {
      active = false;
    };
  }, [productId]);
  async function open(id: number) {
    const d = await apiGet<Detail>(`/api/bom/orders/${id}`);
    setDetail(d);
    setNumber(d.order.number);
    setDate(d.order.date);
    setLines(JSON.parse(d.history[0]!.lines) as Line[]);
    setSelectedHistory(d.history[0]!);
    setTotals([]);
    setTree([]);
    setReason("");
  }
  return (
    <div className="card bom-panel">
      <h1>Заказы</h1>
      <p className="hint">
        Сохранение заказа фиксирует выбранные варианты. Изменение количества или
        варианта создаёт новую версию заказа с автоматическим пересчётом.
      </p>
      {error && (
        <div className="error-banner" role="alert">
          {error}
        </div>
      )}
      {notice && <p role="status">{notice}</p>}
      <div className="bom-grid">
        <label>
          Открыть заказ
          <select
            className="input"
            value={detail?.order.id ?? ""}
            onChange={(e) => {
              if (e.target.value) void run(() => open(Number(e.target.value)));
            }}
          >
            <option value="">— выберите —</option>
            {orders.map((o) => (
              <option key={o.id} value={o.id}>
                {o.number} · {o.date}
              </option>
            ))}
          </select>
        </label>
        <button
          className="btn-secondary"
          onClick={() => {
            setDetail(null);
            setNumber("");
            setLines([]);
            setSelectedHistory(null);
            setTotals([]);
            setTree([]);
            setReason("");
          }}
        >
          Новый заказ
        </button>
      </div>
      <div className="bom-grid">
        <label>
          Номер
          <input
            className="input"
            maxLength={128}
            disabled={!!detail}
            value={number}
            onChange={(e) => setNumber(e.target.value)}
          />
        </label>
        <label>
          Дата
          <input
            className="input"
            type="date"
            disabled={!!detail}
            value={date}
            onChange={(e) => setDate(e.target.value)}
          />
        </label>
      </div>
      <h3>Позиции заказа</h3>
      {lines.map((l, i) => (
        <div className="bom-grid" key={i}>
          <label>
            Вариант
            <select
              className="input"
              value={l.variantId}
              onChange={(e) =>
                setLines(
                  lines.map((x, j) =>
                    i === j ? { ...x, variantId: Number(e.target.value) } : x,
                  ),
                )
              }
            >
              <option value="0">— выберите —</option>
              {variants.map((v) => (
                <option key={v.id} value={v.id}>
                  {products.find((p) => p.id === v.productId)?.name} / {v.name}{" "}
                  · v{v.revisionId}
                </option>
              ))}
            </select>
          </label>
          <label>
            Количество
            <input
              className="input"
              type="number"
              min="0.000000001"
              step="any"
              value={l.quantity}
              onChange={(e) =>
                setLines(
                  lines.map((x, j) =>
                    i === j ? { ...x, quantity: Number(e.target.value) } : x,
                  ),
                )
              }
            />
          </label>
          <button
            className="btn-ghost"
            onClick={() => setLines(lines.filter((_, j) => j !== i))}
          >
            Удалить позицию
          </button>
        </div>
      ))}
      <button
        className="btn-secondary"
        onClick={() => setLines([...lines, { variantId: 0, quantity: 1 }])}
      >
        Добавить позицию заказа
      </button>
      <details>
        <summary>Создать вариант для заказа</summary>
        <label>
          Изделие / модификация
          <select
            className="input"
            value={productId}
            onChange={(e) => setProductId(Number(e.target.value))}
          >
            <option value="0">— выберите —</option>
            {products.map((p) => (
              <option key={p.id} value={p.id}>
                {p.name}
              </option>
            ))}
          </select>
        </label>
        {spec?.revision ? (
          <>
            <label>
              Имя варианта
              <input
                className="input"
                value={variantName}
                onChange={(e) => setVariantName(e.target.value)}
              />
            </label>
            <VariantFields
              parameters={spec.parameters}
              calculations={spec.effective?.calculations ?? {}}
              values={values}
              onChange={setValues}
            />
            <button
              className="btn-primary"
              disabled={busy || !variantName.trim()}
              onClick={() =>
                void run(async () => {
                  const v = await apiPost<Variant>("/api/bom/variants", {
                    revisionId: spec.revision!.id,
                    name: variantName,
                    values: variantValues(spec.parameters, values),
                  });
                  await refresh();
                  setLines([...lines, { variantId: v.id, quantity: 1 }]);
                  setNotice(
                    "Вариант проверен и добавлен к позициям формы заказа.",
                  );
                })
              }
            >
              Создать и добавить к заказу
            </button>
          </>
        ) : (
          productId > 0 && (
            <p className="hint">
              Сначала сохраните спецификацию изделия в каталоге.
            </p>
          )
        )}
      </details>
      <label>
        Причина изменения
        <input
          className="input"
          maxLength={512}
          value={reason}
          onChange={(e) => setReason(e.target.value)}
        />
      </label>
      <button
        className="btn-primary"
        disabled={busy || !number.trim() || !reason.trim() || !lines.length}
        onClick={() =>
          void run(async () => {
            const body = {
              number,
              date,
              reason,
              expectedRevisionId: detail?.history[0]?.id ?? null,
              lines,
            };
            const r = detail
              ? await apiPut<{ order: Order }>(
                  `/api/bom/orders/${detail.order.id}`,
                  body,
                )
              : await apiPost<{ order: Order }>("/api/bom/orders", body);
            await refresh();
            await open(r.order.id);
            setNotice("Заказ сохранён. Спецификации вариантов зафиксированы.");
          })
        }
      >
        Сохранить заказ и пересчитать
      </button>
      {detail && (
        <>
          <h3>Сохранённый состав и расход</h3>
          <label>
            Версия заказа
            <select
              className="input"
              value={selectedHistory?.id ?? ""}
              onChange={(e) => {
                setSelectedHistory(
                  detail.history.find((h) => h.id === Number(e.target.value)) ??
                    null,
                );
                setTree([]);
                setTotals([]);
              }}
            >
              {detail.history.map((h) => (
                <option key={h.id} value={h.id}>
                  {new Date(h.createdAt).toLocaleString()} · {h.reason} · v
                  {h.id}
                </option>
              ))}
            </select>
          </label>
          {selectedHistory &&
            (JSON.parse(selectedHistory.lines) as Line[]).map((l, i) => (
              <div key={i}>
                {variants.find((v) => v.id === l.variantId)?.name} ×{" "}
                {l.quantity}{" "}
                <button
                  className="btn-ghost"
                  onClick={() =>
                    void run(async () =>
                      setTree(
                        await apiGet<Tree[]>(
                          `/api/bom/variants/${l.variantId}/tree?quantity=${l.quantity}`,
                        ),
                      ),
                    )
                  }
                >
                  Полный состав позиции {i + 1}
                </button>
              </div>
            ))}
          {tree.length > 0 && (
            <div className="table-scroll">
              <table className="data-table">
                <thead>
                  <tr>
                    <th>Уровень</th>
                    <th>Изделие</th>
                    <th>Количество</th>
                    <th>Единица</th>
                  </tr>
                </thead>
                <tbody>
                  {tree.map((t) => (
                    <tr key={t.path}>
                      <td>{t.depth}</td>
                      <td>{t.name}</td>
                      <td>{t.required_quantity}</td>
                      <td>{t.unit}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
          <label>
            Класс материалов
            <select
              className="input"
              value={classId}
              onChange={(e) => setClassId(e.target.value)}
            >
              <option value="">Все конечные компоненты</option>
              {classes.map((c) => (
                <option key={c.id} value={c.id}>
                  {c.name} с подклассами
                </option>
              ))}
            </select>
          </label>
          <button
            className="btn-secondary"
            disabled={busy || !selectedHistory}
            onClick={() =>
              void run(async () =>
                setTotals(
                  await apiGet<Total[]>(
                    `/api/bom/order-revisions/${selectedHistory!.id}/totals${classId ? `?classId=${classId}` : ""}`,
                  ),
                ),
              )
            }
          >
            Рассчитать общий расход заказа
          </button>
          <TotalsTable rows={totals} />
        </>
      )}
    </div>
  );
}
