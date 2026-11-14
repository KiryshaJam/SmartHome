import { humanizeBomError } from "../utils/bomExpressions";
import { useConfirmLeave, useUnsavedChanges } from "../components/UnsavedChanges";
import { useEffect, useRef, useState } from "react";
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
  updateToken: number;
}
interface Line {
  variantId: number;
  quantity: number;
}
interface Detail {
  order: Order;
  lines: Line[];
}
interface Tree {
  path: string;
  depth: number;
  name: string;
  unit: string;
  required_quantity: number;
}
export function OrdersPage() {
  const confirmLeave = useConfirmLeave();
  const [calculating, setCalculating] = useState(false);
  const [totalsError, setTotalsError] = useState("");
  const [calculationAttempt, setCalculationAttempt] = useState(0);
  const [orders, setOrders] = useState<Order[]>([]),
    [variants, setVariants] = useState<Variant[]>([]),
    [classes, setClasses] = useState<ClassNodeDto[]>([]),
    [products, setProducts] = useState<ProductDto[]>([]);
  const [detail, setDetail] = useState<Detail | null>(null),
    [number, setNumber] = useState(""),
    [date, setDate] = useState(new Date().toLocaleDateString("en-CA")),
    [lines, setLines] = useState<Line[]>([]);
  const [error, setError] = useState(""),
    [notice, setNotice] = useState(""),
    [busy, setBusy] = useState(false),
    [savedLines, setSavedLines] = useState<Line[] | null>(null),
    [totals, setTotals] = useState<Total[] | null>(null),
    [tree, setTree] = useState<Tree[]>([]),
    [classId, setClassId] = useState("");
  const [productId, setProductId] = useState(0),
    [spec, setSpec] = useState<Spec | null>(null),
    [values, setValues] = useState<Record<string, string | number>>({}),
    [variantName, setVariantName] = useState("");
  const dirty = detail ? JSON.stringify(lines) !== JSON.stringify(savedLines) : !!number.trim() || lines.length > 0;
  useUnsavedChanges(dirty);
  const calculationGeneration = useRef(0);
  const openGeneration = useRef(0);
  const treeGeneration = useRef(0);
  useEffect(() => {
    calculationGeneration.current += 1;
    treeGeneration.current += 1;
    setTree([]);
    setTotals(null);
    return () => {
      calculationGeneration.current += 1;
      treeGeneration.current += 1;
    };
  }, [detail?.order.updateToken, detail?.order.id, classId, lines]);
  useEffect(() => {
    if (!detail || dirty) return;
    const generation = ++calculationGeneration.current;
    let active = true;
    setCalculating(true);
    setTotalsError("");
    void apiGet<Total[]>(`/api/bom/orders/${detail.order.id}/totals${classId ? `?classId=${classId}` : ""}`)
      .then(result => { if (active && generation === calculationGeneration.current) setTotals(result); })
      .catch(e => { if (active) setTotalsError(e instanceof Error ? e.message : "Не удалось рассчитать расход"); })
      .finally(() => { if (active) setCalculating(false); });
    return () => { active = false; };
  }, [detail, classId, dirty, lines, calculationAttempt]);
  useEffect(() => () => { openGeneration.current += 1; }, []);
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
          if (active) setError(e instanceof Error ? e.message : "Не удалось загрузить спецификацию");
        });
    return () => {
      active = false;
    };
  }, [productId]);
  async function open(id: number) {
    const generation = ++openGeneration.current;
    const d = await apiGet<Detail>(`/api/bom/orders/${id}`);
    if (generation !== openGeneration.current) return;
    setDetail(d);
    setNumber(d.order.number);
    setDate(d.order.date);
    setLines(d.lines);
    setVariantName("");
    setValues({});
    setSavedLines(d.lines);
    setTotals(null);
    setTree([]);
  }
  return (
    <div className="card bom-panel">
      <h1>Заказы</h1>
      <p className="hint">
        Заказ содержит готовые исполнения с фиксированным составом. После изменения
        позиций сохраните заказ — общий расход рассчитается автоматически.
      </p>
      {error && (
        <div className="error-banner" role="alert">
          {humanizeBomError(error, Object.fromEntries((spec?.parameters ?? []).map(p => [p.key, p.name])), Object.fromEntries(products.map(p => [String(p.id), p.name])))}
        </div>
      )}
      {notice && <p role="status">{notice}</p>}
      <div className="bom-grid">
        <label>
          Открыть заказ
          <select
            className="input"
            disabled={busy}
            value={detail?.order.id ?? ""}
            onChange={(e) => {
              if (e.target.value && confirmLeave()) void run(() => open(Number(e.target.value)));
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
          disabled={busy}
          onClick={() => {
            if (!confirmLeave()) return;
            setError(""); setNotice("");
            openGeneration.current += 1;
            setDetail(null);
            setDate(new Date().toLocaleDateString("en-CA"));
            setNumber("");
            setLines([]);
            setSavedLines(null);
            setTotals(null);
            setTree([]);
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
            disabled={busy || !!detail}
            value={number}
            onChange={(e) => setNumber(e.target.value)}
          />
        </label>
        <label>
          Дата
          <input
            className="input"
            type="date"
            disabled={busy || !!detail}
            value={date}
            onChange={(e) => setDate(e.target.value)}
          />
        </label>
      </div>
      {dirty && <p className="warning-banner" role="status">Заказ не сохранён. Сохраните позиции, чтобы получить их состав и общий расход.</p>}
      <h3>Позиции заказа</h3>
      {lines.map((l, i) => (
        <fieldset className="bom-grid" key={i} disabled={busy}>
          <legend>Позиция {i + 1}</legend>
          <label>
            Исполнение изделия
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
        </fieldset>
      ))}
      <button
        className="btn-secondary"
        disabled={busy}
        onClick={() => setLines([...lines, { variantId: 0, quantity: 1 }])}
      >
        Добавить позицию заказа
      </button>
      <details>
        <summary>Подобрать исполнение для заказа</summary>
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
              Название исполнения (необязательно)
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
              disabled={busy}
              onClick={() =>
                void run(async () => {
                  const v = await apiPost<Variant>("/api/bom/variants", {
                    revisionId: spec.revision!.id,
                    name: variantName.trim() || `${products.find(p => p.id === productId)?.name ?? "Изделие"} — исполнение для заказа`.slice(0, 256),
                    values: variantValues(spec.parameters, values),
                  });
                  await refresh();
                  setLines(current => [...current, { variantId: v.id, quantity: 1 }]);
                  setVariantName("");
                  setValues({});
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
      <button
        className="btn-primary"
        disabled={busy || !number.trim() || !date || !lines.length || lines.some(l => !l.variantId || !Number.isFinite(l.quantity) || l.quantity <= 0)}
        onClick={() =>
          void run(async () => {
            const body = {
              number,
              date,
              expectedUpdateToken: detail?.order.updateToken ?? null,
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
            setNotice("Заказ сохранён. Расход рассчитывается по зафиксированным исполнениям.");
          })
        }
      >
        Сохранить заказ
      </button>
      {detail && !dirty && (
        <>
          <h3>Сохранённый состав и расход</h3>
          {savedLines && savedLines.map((l, i) => (
              <div key={i}>
                {variants.find((v) => v.id === l.variantId)?.name} ×{" "}
                {l.quantity}{" "}
                <button
                  className="btn-ghost"
                  disabled={busy}
                  onClick={() =>
                    void run(async () => {
                      const generation = ++treeGeneration.current;
                      setTree([]);
                      const result = await apiGet<Tree[]>(
                        `/api/bom/variants/${l.variantId}/tree?quantity=${l.quantity}`,
                      );
                      if (generation === treeGeneration.current) setTree(result);
                    })
                  }
                >
                  Полный состав позиции {i + 1}
                </button>
              </div>
            ))}
          {tree.length > 0 && (
            <div className="table-scroll">
              <table className="data data-table">
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
                      <td style={{ paddingLeft: `${.8 + t.depth * 1.2}rem` }}>{t.depth > 0 ? "↳ " : ""}{t.name}</td>
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
          {totalsError && <div className="error-banner" role="alert">{totalsError} <button disabled={calculating} onClick={() => setCalculationAttempt(x => x + 1)}>Повторить расчёт</button></div>}
          {calculating && <p role="status">Расчёт общего расхода…</p>}
          <p className="hint">Расход рассчитывается автоматически по сохранённым позициям заказа. Выбор класса включает его подклассы.</p>
          {totals !== null && <TotalsTable rows={totals} />}
        </>
      )}
    </div>
  );
}
