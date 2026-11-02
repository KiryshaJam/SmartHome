import { useEffect, useState } from "react";
import { apiGet, apiPost, apiPut } from "../api/client";
import type { ClassNodeDto, MeasureUnitDto, ProductDto } from "../types/api";

export interface SpecParam {
  key: string;
  name: string;
  type: number;
  required: boolean;
  min: number | null;
  max: number | null;
  enumIds: number[];
  enumLabels?: Record<string, string>;
  default: string | number | null;
}
interface Position {
  id: string;
  componentId: number;
  revisionId: number;
  amount: number;
  forQuantity: number;
  unitId: number;
  sortOrder: number;
  condition: string | null;
  formula: string | null;
  childVariantId: number | null;
  bindings: Record<string, string>;
  excluded: boolean;
  inherited: boolean;
}
interface Rule {
  expression: string;
  message: string;
}
interface Doc {
  positions: Position[];
  parameters: SpecParam[];
  rules: Rule[];
  calculations: Record<string, string>;
}
interface Revision {
  id: number;
  unitId: number;
  previousId: number | null;
  baseRevisionId: number | null;
  document: string;
  reason: string;
  createdAt: string;
}
export interface Spec {
  baseEffective: Doc | null;
  revision: Revision | null;
  effective: Doc | null;
  parameters: SpecParam[];
}
export interface Variant {
  id: number;
  name: string;
  productId: number;
  revisionId: number;
}
export interface Total {
  product_id: number;
  name: string;
  unit: string;
  unit_id: number;
  required_quantity: number;
}
interface Tree extends Total {
  norm: number;
  for_quantity: number;
  path: string;
  depth: number;
  revision_id: number;
  leaf: boolean;
}
const message = (e: unknown) => (e instanceof Error ? e.message : "Ошибка");

export function TotalsTable({ rows }: { rows: Total[] }) {
  return (
    <div className="table-scroll">
      <table className="data-table">
        <thead>
          <tr>
            <th>Материал / компонент</th>
            <th>Количество</th>
            <th>Единица</th>
          </tr>
        </thead>
        <tbody>
          {rows.map((r) => (
            <tr key={`${r.product_id}-${r.unit_id}`}>
              <td>{r.name}</td>
              <td>{r.required_quantity}</td>
              <td>{r.unit}</td>
            </tr>
          ))}
        </tbody>
      </table>
      {!rows.length && (
        <p className="hint">Нет материалов выбранного класса.</p>
      )}
    </div>
  );
}

export function VariantFields({
  parameters,
  calculations,
  values,
  onChange,
}: {
  parameters: SpecParam[];
  calculations: Record<string, string>;
  values: Record<string, string | number>;
  onChange: (v: Record<string, string | number>) => void;
}) {
  return (
    <div className="bom-grid">
      {parameters
        .filter((p) => !calculations[p.key])
        .map((p) => (
          <label key={p.key}>
            {p.name} {p.required ? "*" : ""} <small>({p.key})</small>
            {p.type === 5 ? (
              <select
                className="input"
                value={values[p.key] ?? ""}
                onChange={(e) => {
                  const next = { ...values };
                  if (e.target.value === "") delete next[p.key];
                  else next[p.key] = Number(e.target.value);
                  onChange(next);
                }}
              >
                <option value="">— значение прототипа —</option>
                {p.enumIds.map((id) => (
                  <option key={id} value={id}>
                    {p.enumLabels?.[id] ?? `Значение #${id}`}
                  </option>
                ))}
              </select>
            ) : (
              <input
                className="input"
                type={
                  p.type === 1 || p.type === 2
                    ? "number"
                    : p.type === 4
                      ? "datetime-local"
                      : "text"
                }
                step={p.type === 1 ? "1" : "any"}
                min={p.min ?? undefined}
                max={p.max ?? undefined}
                value={values[p.key] ?? ""}
                placeholder={
                  p.default == null ? "Не задано" : String(p.default)
                }
                onChange={(e) => {
                  const next = { ...values };
                  if (e.target.value === "") delete next[p.key];
                  else
                    next[p.key] =
                      p.type === 1 || p.type === 2
                        ? Number(e.target.value)
                        : e.target.value;
                  onChange(next);
                }}
              />
            )}
          </label>
        ))}
    </div>
  );
}
export function variantValues(
  parameters: SpecParam[],
  values: Record<string, string | number>,
) {
  return Object.fromEntries(
    Object.entries(values).map(([k, v]) => [
      k,
      parameters.find((p) => p.key === k)?.type === 4
        ? new Date(String(v)).toISOString()
        : v,
    ]),
  );
}
export function BomPanel({
  productId,
  onCreated,
}: {
  productId: number;
  onCreated: () => Promise<void>;
}) {
  const [spec, setSpec] = useState<Spec | null>(null),
    [products, setProducts] = useState<ProductDto[]>([]),
    [units, setUnits] = useState<MeasureUnitDto[]>([]),
    [classes, setClasses] = useState<ClassNodeDto[]>([]);
  const [positions, setPositions] = useState<Position[]>([]),
    [rules, setRules] = useState<Rule[]>([]),
    [calculations, setCalculations] = useState<Record<string, string>>({}),
    [unitId, setUnitId] = useState(0),
    [reason, setReason] = useState("");
  const [error, setError] = useState(""),
    [busy, setBusy] = useState(false),
    [notice, setNotice] = useState("");
  const [history, setHistory] = useState<Revision[]>([]),
    [historical, setHistorical] = useState<Spec | null>(null),
    [editor, setEditor] = useState<Position | null>(null),
    [child, setChild] = useState<Spec | null>(null),
    [childVariants, setChildVariants] = useState<Variant[]>([]);
  const [variants, setVariants] = useState<Variant[]>([]),
    [values, setValues] = useState<Record<string, string | number>>({}),
    [variantName, setVariantName] = useState(""),
    [variantId, setVariantId] = useState(0),
    [quantity, setQuantity] = useState(1),
    [classId, setClassId] = useState("");
  const [tree, setTree] = useState<Tree[]>([]),
    [totals, setTotals] = useState<Total[]>([]),
    [preview, setPreview] = useState<string>("");
  const [modName, setModName] = useState(""),
    [modShort, setModShort] = useState("");
  async function load() {
    const [s, p, u, c, h, v] = await Promise.all([
      apiGet<Spec>(`/api/bom/products/${productId}`),
      apiGet<ProductDto[]>("/api/products"),
      apiGet<MeasureUnitDto[]>("/api/measure-units"),
      apiGet<ClassNodeDto[]>("/api/classnodes"),
      apiGet<Revision[]>(`/api/bom/products/${productId}/history`),
      apiGet<Variant[]>(`/api/bom/variants?productId=${productId}`),
    ]);
    setSpec(s);
    setProducts(p);
    setUnits(u);
    setClasses(c);
    setHistory(h);
    setVariants(v);
    const d = s.revision ? (JSON.parse(s.revision.document) as Doc) : null;
    setPositions(d?.positions ?? []);
    setRules(d?.rules ?? []);
    setCalculations(d?.calculations ?? {});
    setUnitId(s.revision?.unitId ?? u[0]?.id ?? 0);
  }
  useEffect(() => {
    let active = true;
    void load().catch((e) => {
      if (active) setError(message(e));
    });
    return () => {
      active = false;
    };
  }, [productId]);
  useEffect(() => {
    let active = true;
    setChild(null);
    setChildVariants([]);
    if (editor?.componentId)
      void Promise.all([
        apiGet<Spec>(
          `/api/bom/products/${editor.componentId}${editor.revisionId ? `?revisionId=${editor.revisionId}` : ""}`,
        ),
        apiGet<Variant[]>(`/api/bom/variants?productId=${editor.componentId}`),
      ])
        .then(([s, v]) => {
          if (active) {
            setChild(s);
            setChildVariants(v);
          }
        })
        .catch((e) => {
          if (active) setError(message(e));
        });
    return () => {
      active = false;
    };
  }, [editor?.componentId, editor?.revisionId]);
  async function run(action: () => Promise<void>) {
    setBusy(true);
    setError("");
    setNotice("");
    try {
      await action();
    } catch (e) {
      setError(message(e));
    } finally {
      setBusy(false);
    }
  }
  function edit(p: Position) {
    setEditor({ ...p, bindings: { ...p.bindings } });
  }
  function exclude(p: Position) {
    setPositions([
      ...positions.filter((x) => x.id !== p.id),
      { ...p, excluded: true, inherited: false },
    ]);
  }
  function remove(p: Position) {
    setPositions(positions.filter((x) => x.id !== p.id));
  }
  const effective = (spec?.baseEffective?.positions ?? [])
    .filter((p) => !positions.some((x) => x.id === p.id))
    .map((p) => ({ ...p, inherited: true }))
    .concat(positions)
    .filter((p) => !p.excluded)
    .sort((a, b) => a.sortOrder - b.sortOrder);
  const shown = historical?.effective?.positions ?? effective;
  const configuration = historical ?? spec;
  const name = (id: number) =>
    products.find((p) => p.id === id)?.name ?? `#${id}`;
  return (
    <section className="bom-panel">
      <h3>Спецификация и варианты</h3>
      {error && (
        <div className="error-banner" role="alert">
          {error}
        </div>
      )}
      {notice && <p role="status">{notice}</p>}
      <label>
        Единица количества изделия
        <select
          className="input"
          value={unitId}
          disabled={!!spec?.revision}
          onChange={(e) => setUnitId(Number(e.target.value))}
        >
          {units.map((u) => (
            <option key={u.id} value={u.id}>
              {u.name} ({u.shortName})
            </option>
          ))}
        </select>
      </label>
      {spec?.revision && (
        <p className="hint">
          Версия {spec.revision.id}
          {spec.revision.baseRevisionId
            ? ` · модификация версии ${spec.revision.baseRevisionId}`
            : ""}
          . Позиции используют зафиксированные версии компонентов. Чтобы взять
          новую, выберите «Текущая версия» при редактировании позиции.
        </p>
      )}
      {historical && (
        <div>
          <strong>История: версия {historical.revision?.id}</strong>
          <button className="btn-ghost" onClick={() => setHistorical(null)}>
            Вернуться к редактированию
          </button>
        </div>
      )}
      <div className="table-scroll">
        <table className="data-table">
          <thead>
            <tr>
              <th>Порядок</th>
              <th>Компонент</th>
              <th>Расход / для количества</th>
              <th>Условие / формула</th>
              <th>Действия</th>
            </tr>
          </thead>
          <tbody>
            {shown.map((p) => (
              <tr key={p.id}>
                <td>{p.sortOrder}</td>
                <td>
                  {name(p.componentId)}
                  <div className="hint">
                    Версия {p.revisionId}
                    {p.inherited ? " · из базы" : ""}
                  </div>
                </td>
                <td>
                  {p.amount} {units.find((u) => u.id === p.unitId)?.shortName} /{" "}
                  {p.forQuantity}
                </td>
                <td>
                  {p.condition ?? "Всегда"}
                  <br />
                  {p.formula ?? "Постоянный расход"}
                </td>
                <td>
                  {!historical && (
                    <>
                      <button className="btn-ghost" onClick={() => edit(p)}>
                        Изменить / заменить
                      </button>
                      {spec?.revision?.baseRevisionId &&
                      spec.baseEffective?.positions.some(
                        (x) => x.id === p.id,
                      ) ? (
                        <button
                          className="btn-ghost"
                          onClick={() => exclude(p)}
                        >
                          Исключить
                        </button>
                      ) : (
                        <button className="btn-ghost" onClick={() => remove(p)}>
                          Удалить позицию
                        </button>
                      )}
                    </>
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      {positions.some((p) => p.excluded) && (
        <p className="hint">
          Исключены:{" "}
          {positions
            .filter((p) => p.excluded)
            .map((p) => (
              <span key={p.id}>
                {name(p.componentId)}{" "}
                <button className="btn-ghost" onClick={() => remove(p)}>
                  Вернуть из базы
                </button>
              </span>
            ))}
        </p>
      )}
      {!historical && (
        <button
          className="btn-secondary"
          onClick={() =>
            setEditor({
              id: crypto.randomUUID(),
              componentId: 0,
              revisionId: 0,
              amount: 1,
              forQuantity: 1,
              unitId: 0,
              sortOrder: effective.length * 10,
              condition: null,
              formula: null,
              childVariantId: null,
              bindings: {},
              excluded: false,
              inherited: false,
            })
          }
        >
          Добавить компонент
        </button>
      )}
      {editor && (
        <form
          className="card-nested bom-form"
          onSubmit={(e) => {
            e.preventDefault();
            if (!child?.revision) {
              setError(
                "Сначала сохраните спецификацию компонента, включая пустую для материала.",
              );
              return;
            }
            const p = {
              ...editor,
              unitId: child.revision.unitId,
              revisionId: editor.revisionId || child.revision.id,
            };
            setPositions([...positions.filter((x) => x.id !== p.id), p]);
            setEditor(null);
          }}
        >
          <h4>Позиция состава</h4>
          <label>
            Компонент
            <select
              className="input"
              required
              value={editor.componentId}
              onChange={(e) =>
                setEditor({
                  ...editor,
                  componentId: Number(e.target.value),
                  revisionId: 0,
                  childVariantId: null,
                  bindings: {},
                })
              }
            >
              <option value="0">— выберите изделие —</option>
              {products
                .filter((p) => p.id !== productId)
                .map((p) => (
                  <option key={p.id} value={p.id}>
                    {p.name}
                  </option>
                ))}
            </select>
          </label>
          <div className="bom-grid">
            <label>
              Расход
              <input
                className="input"
                type="number"
                min="0.000000001"
                step="any"
                required
                value={editor.amount}
                onChange={(e) =>
                  setEditor({ ...editor, amount: Number(e.target.value) })
                }
              />
            </label>
            <label>
              Для количества
              <input
                className="input"
                type="number"
                min="0.000000001"
                step="any"
                required
                value={editor.forQuantity}
                onChange={(e) =>
                  setEditor({ ...editor, forQuantity: Number(e.target.value) })
                }
              />
            </label>
            <label>
              Порядок
              <input
                className="input"
                type="number"
                value={editor.sortOrder}
                onChange={(e) =>
                  setEditor({ ...editor, sortOrder: Number(e.target.value) })
                }
              />
            </label>
          </div>
          <p className="hint">
            Единица расхода:{" "}
            {units.find((u) => u.id === child?.revision?.unitId)?.name ??
              "Задайте единицу в карточке компонента"}
            . Версия: {editor.revisionId || child?.revision?.id || "не задана"}
          </p>
          <button
            type="button"
            className="btn-ghost"
            onClick={() =>
              setEditor({ ...editor, revisionId: 0, childVariantId: null })
            }
          >
            Текущая версия компонента
          </button>
          <label>
            Условие включения
            <input
              className="input"
              value={editor.condition ?? ""}
              placeholder='p1 == "radio"'
              onChange={(e) =>
                setEditor({ ...editor, condition: e.target.value || null })
              }
            />
          </label>
          <label>
            Формула расхода
            <input
              className="input"
              value={editor.formula ?? ""}
              placeholder="p2 * 2 (пусто — постоянный расход)"
              onChange={(e) =>
                setEditor({ ...editor, formula: e.target.value || null })
              }
            />
          </label>
          <label>
            Вложенный вариант
            <select
              className="input"
              value={editor.childVariantId ?? ""}
              onChange={(e) => {
                const v = childVariants.find(
                  (x) => x.id === Number(e.target.value),
                );
                setEditor({
                  ...editor,
                  childVariantId: v?.id ?? null,
                  revisionId: v?.revisionId ?? 0,
                  bindings: {},
                });
              }}
            >
              <option value="">Передача параметров / значения прототипа</option>
              {childVariants.map((v) => (
                <option key={v.id} value={v.id}>
                  {v.name} · версия {v.revisionId}
                </option>
              ))}
            </select>
          </label>
          {!editor.childVariantId &&
            child?.parameters.map((p) => (
              <label key={p.key}>
                Передать в {p.name} ({p.key})
                <input
                  className="input"
                  placeholder="Выражение из параметров родителя"
                  value={editor.bindings[p.key] ?? ""}
                  onChange={(e) => {
                    const b = { ...editor.bindings };
                    if (e.target.value) b[p.key] = e.target.value;
                    else delete b[p.key];
                    setEditor({ ...editor, bindings: b });
                  }}
                />
              </label>
            ))}
          <button className="btn-secondary" type="submit">
            Применить позицию к форме
          </button>
          <button
            className="btn-ghost"
            type="button"
            onClick={() => setEditor(null)}
          >
            Отмена
          </button>
        </form>
      )}
      {historical && (
        <div className="card-nested">
          <strong>Правила исторической версии</strong>
          {historical.effective?.rules.map((r, i) => (
            <p key={i}>
              <code>{r.expression}</code> — {r.message}
            </p>
          ))}
          {Object.entries(historical.effective?.calculations ?? {}).map(
            ([k, v]) => (
              <p key={k}>
                {k} = <code>{v}</code>
              </p>
            ),
          )}
        </div>
      )}
      {!historical && (
        <details>
          <summary>Правила, совместимость и вычисляемые параметры</summary>
          <p className="hint">
            Параметры:{" "}
            {spec?.parameters.map((p) => `${p.name} = ${p.key}`).join("; ") ||
              "Добавьте параметры к классу изделия в справочниках."}
            . Числа, строки в двойных кавычках, == != &lt; &gt; &lt;= &gt;=, AND
            OR NOT, + − * / %, скобки. Каждое ограничение должно быть истинным.
            has_ID проверяет наличие компонента с ID в этой сборке;
            идентификаторы:{" "}
            {effective
              .map((p) => `${name(p.componentId)} = has_${p.componentId}`)
              .join("; ")}
            .
          </p>
          {rules.map((r, i) => (
            <div className="bom-grid" key={i}>
              <label>
                Ограничение
                <input
                  className="input"
                  value={r.expression}
                  onChange={(e) =>
                    setRules(
                      rules.map((x, j) =>
                        j === i ? { ...x, expression: e.target.value } : x,
                      ),
                    )
                  }
                />
              </label>
              <label>
                Причина отказа
                <input
                  className="input"
                  value={r.message}
                  onChange={(e) =>
                    setRules(
                      rules.map((x, j) =>
                        j === i ? { ...x, message: e.target.value } : x,
                      ),
                    )
                  }
                />
              </label>
              <button
                className="btn-ghost"
                onClick={() => setRules(rules.filter((_, j) => j !== i))}
              >
                Удалить правило
              </button>
            </div>
          ))}
          <button
            className="btn-ghost"
            onClick={() =>
              setRules([...rules, { expression: "", message: "" }])
            }
          >
            Добавить ограничение
          </button>
          {spec?.parameters.map((p) => (
            <label key={p.key}>
              Вычислять {p.name} ({p.key})
              <input
                className="input"
                value={calculations[p.key] ?? ""}
                placeholder="Пусто — ввод в варианте"
                onChange={(e) => {
                  const c = { ...calculations };
                  if (e.target.value) c[p.key] = e.target.value;
                  else delete c[p.key];
                  setCalculations(c);
                }}
              />
            </label>
          ))}
        </details>
      )}
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
        disabled={busy || !reason.trim() || !!historical}
        onClick={() =>
          void run(async () => {
            await apiPut(`/api/bom/products/${productId}`, {
              expectedRevisionId: spec?.revision?.id ?? null,
              unitId,
              positions,
              rules,
              calculations,
              reason,
            });
            await load();
            setReason("");
            setNotice("Спецификация сохранена новой версией.");
          })
        }
      >
        Сохранить спецификацию и правила
      </button>
      {spec?.revision && (
        <>
          <details>
            <summary>Создать модификацию</summary>
            <label>
              Имя
              <input
                className="input"
                value={modName}
                onChange={(e) => setModName(e.target.value)}
              />
            </label>
            <label>
              Обозначение
              <input
                className="input"
                value={modShort}
                onChange={(e) => setModShort(e.target.value)}
              />
            </label>
            <button
              className="btn-secondary"
              disabled={busy || !modName || !modShort}
              onClick={() =>
                void run(async () => {
                  await apiPost("/api/bom/modifications", {
                    baseRevisionId: configuration!.revision!.id,
                    name: modName,
                    shortName: modShort,
                    reason: "Создана модификация",
                  });
                  await onCreated();
                  setNotice(
                    "Модификация создана. Выберите её в каталоге изделий.",
                  );
                  setModName("");
                  setModShort("");
                })
              }
            >
              Создать из сохранённой версии
            </button>
          </details>
          <details>
            <summary>История спецификации</summary>
            {history.map((h) => (
              <p key={h.id}>
                {new Date(h.createdAt).toLocaleString()} · {h.reason}{" "}
                <button
                  className="btn-ghost"
                  onClick={() =>
                    void run(async () =>
                      setHistorical(
                        await apiGet<Spec>(
                          `/api/bom/products/${productId}?revisionId=${h.id}`,
                        ),
                      ),
                    )
                  }
                >
                  Показать версию {h.id}
                </button>
              </p>
            ))}
          </details>
          <h4>Вариант исполнения</h4>
          <p className="hint">
            Сначала сохраните спецификацию. Вариант фиксирует её правила,
            параметры и состав на всей глубине.
          </p>
          <label>
            Имя варианта
            <input
              className="input"
              value={variantName}
              onChange={(e) => setVariantName(e.target.value)}
            />
          </label>
          <VariantFields
            parameters={configuration!.parameters}
            calculations={configuration!.effective?.calculations ?? {}}
            values={values}
            onChange={setValues}
          />
          <div className="bom-grid">
            <button
              className="btn-secondary"
              disabled={busy}
              onClick={() =>
                void run(async () => {
                  const v = await apiPost<{
                    name: string;
                    children: unknown[];
                  }>("/api/bom/preview", {
                    revisionId: configuration!.revision!.id,
                    name: variantName || "Проверка",
                    values: variantValues(configuration!.parameters, values),
                  });
                  setPreview(
                    `Конфигурация допустима. ${v.name}: ${v.children.length} позиций верхнего уровня.`,
                  );
                })
              }
            >
              Проверить конфигурацию
            </button>
            <button
              className="btn-primary"
              disabled={busy || !variantName.trim()}
              onClick={() =>
                void run(async () => {
                  const v = await apiPost<Variant>("/api/bom/variants", {
                    revisionId: configuration!.revision!.id,
                    name: variantName,
                    values: variantValues(configuration!.parameters, values),
                  });
                  await load();
                  setVariantId(v.id);
                  setPreview("Вариант сохранён.");
                })
              }
            >
              Сохранить вариант
            </button>
          </div>
          {preview && <p role="status">{preview}</p>}
          <h4>Полный состав и сводные нормы</h4>
          <label>
            Сохранённый вариант
            <select
              className="input"
              value={variantId}
              onChange={(e) => {
                setVariantId(Number(e.target.value));
                setTree([]);
                setTotals([]);
              }}
            >
              <option value="0">— выберите вариант —</option>
              {variants.map((v) => (
                <option key={v.id} value={v.id}>
                  {v.name} · версия {v.revisionId}
                </option>
              ))}
            </select>
          </label>
          <div className="bom-grid">
            <label>
              Количество изделий
              <input
                className="input"
                type="number"
                min="0.000000001"
                step="any"
                value={quantity}
                onChange={(e) => setQuantity(Number(e.target.value))}
              />
            </label>
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
          </div>
          <button
            className="btn-secondary"
            disabled={!variantId || busy}
            onClick={() =>
              void run(async () => {
                const [t, n] = await Promise.all([
                  apiGet<Tree[]>(
                    `/api/bom/variants/${variantId}/tree?quantity=${quantity}`,
                  ),
                  apiGet<Total[]>(
                    `/api/bom/variants/${variantId}/totals?quantity=${quantity}${classId ? `&classId=${classId}` : ""}`,
                  ),
                ]);
                setTree(t);
                setTotals(n);
              })
            }
          >
            Показать состав и рассчитать
          </button>
          {tree.length > 0 && (
            <>
              <div className="table-scroll">
                <table className="data-table">
                  <thead>
                    <tr>
                      <th>Уровень</th>
                      <th>Изделие</th>
                      <th>Норма / для количества</th>
                      <th>Количество</th>
                      <th>Единица</th>
                    </tr>
                  </thead>
                  <tbody>
                    {tree.map((t) => (
                      <tr key={t.path}>
                        <td>{t.depth}</td>
                        <td>
                          {t.name} · v{t.revision_id}
                        </td>
                        <td>
                          {t.norm} / {t.for_quantity}
                        </td>
                        <td>{t.required_quantity}</td>
                        <td>{t.unit}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
              <TotalsTable rows={totals} />
            </>
          )}
        </>
      )}
    </section>
  );
}
