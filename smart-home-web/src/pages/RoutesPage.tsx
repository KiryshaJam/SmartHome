import { RouteResources } from "../components/RouteResources";
import { useEffect, useState } from "react";
import { apiGet } from "../api/client";
import {
  readRoute,
  saveRoute,
  applyRoute,
  calculateRoute,
} from "../api/routes";
import type {
  Choice,
  RouteCalculation,
  RouteDocument,
  RouteLookups,
  RouteRow,
  TechStep,
} from "../types/routes";
import { RouteResults } from "../components/RouteResults";
import {
  useConfirmLeave,
  useUnsavedChanges,
} from "../components/UnsavedChanges";

function Select({
  label,
  value,
  items,
  onChange,
  optional = false,
  disabled = false,
  emptyLabel,
}: {
  label: string;
  value: number | null;
  items: Choice[];
  onChange: (id: number | null) => void;
  optional?: boolean;
  disabled?: boolean;
  emptyLabel?: string;
}) {
  return (
    <label>
      {label}
      <select
        className="input"
        value={value ?? ""}
        disabled={disabled}
        aria-label={label}
        onChange={(e) =>
          onChange(e.target.value ? Number(e.target.value) : null)
        }
      >
        <option value="">{emptyLabel ?? (optional ? "Без выбора" : "Выберите…")}</option>
        {items.map((x) => (
          <option key={x.id} value={x.id}>
            {x.name}
          </option>
        ))}
      </select>
    </label>
  );
}
const empty = (productId = 0): RouteDocument => ({
  setting: {
    id: 0,
    name: "",
    productId,
    revisionId: null,
    kind: "standalone",
    finalStepId: 0,
    parameterId: null,
    parameterValue: null,
  },
  steps: [],
  inputs: [],
});
export function RoutesPage({
  initialProductId = null,
}: {
  initialProductId?: number | null;
}) {
  const [rows, setRows] = useState<RouteRow[]>([]),
    [look, setLook] = useState<RouteLookups | null>(null),
    [doc, setDoc] = useState<RouteDocument | null>(null);
  const [dirty, setDirty] = useState(false),
    [err, setErr] = useState(""),
    [busy, setBusy] = useState(false),
    [template, setTemplate] = useState<number | null>(null),
    [quantity, setQuantity] = useState(1),
    [operation, setOperation] = useState<number | null>(null),
    [result, setResult] = useState<RouteCalculation | null>(null);
  const confirmLeave = useConfirmLeave();
  useUnsavedChanges(dirty);
  const load = async () => {
    const [r, l] = await Promise.all([
      apiGet<RouteRow[]>("/api/routes"),
      apiGet<RouteLookups>("/api/routes/lookups"),
    ]);
    setRows(r);
    setLook(l);
    return r;
  };
  async function run(action: () => Promise<void>) {
    setBusy(true);
    setErr("");
    try {
      await action();
    } catch (e) {
      setErr(e instanceof Error ? e.message : String(e));
    } finally {
      setBusy(false);
    }
  }
  useEffect(() => {
    setDoc(null);
    setDirty(false);
    setResult(null);
    setOperation(null);
    void run(async () => {
      const r = await load();
      if (initialProductId) {
        const found = r.filter(
          (x) => x.productId === initialProductId && x.kind !== "template",
        );
        setDoc(
          found.length === 1
            ? await readRoute(found[0].id)
            : found.length === 0
              ? empty(initialProductId)
              : null,
        );
      }
    });
  }, [initialProductId]);
  function change(next: RouteDocument) {
    setDoc(next);
    setDirty(true);
    setResult(null);
  }
  function setting(p: Partial<RouteDocument["setting"]>) {
    if (doc) change({ ...doc, setting: { ...doc.setting, ...p } });
  }
  function updateStep(id: number, p: Partial<TechStep>) {
    if (doc)
      change({
        ...doc,
        steps: doc.steps.map((x) => (x.id === id ? { ...x, ...p } : x)),
      });
  }
  const nextId = () => Math.min(0, ...(doc?.steps.map((x) => x.id) ?? [])) - 1;
  function addOperation() {
    if (!doc || !look) return;
    const product = look.products.find((x) => x.id === doc.setting.productId);
    if (!product?.unitId) {
      setErr("Выберите изделие с единицей измерения.");
      return;
    }
    const id = nextId();
    change({
      ...doc,
      setting: { ...doc.setting, finalStepId: id },
      steps: [
        ...doc.steps,
        {
          id,
          kind: "production",
          productId: product.id,
          unitId: product.unitId,
          resultName: "",
          number:
            (doc.steps.filter((x) => x.kind === "production").length + 1) * 10,
          operationId: look.operations[0]?.id ?? null,
          workCenterId: look.centers[0]?.id ?? null,
          professionId: look.professions[0]?.id ?? null,
          qualificationId: look.qualifications[0]?.id ?? null,
          tooling: "",
          setupMinutes: 0,
          unitMinutes: 0,
          hourlyRate: 0,
        },
      ],
    });
  }
  async function save() {
    if (!doc) return;
    await run(async () => {
      const saved =
        doc.setting.kind === "application" && !doc.setting.id
          ? await applyRoute(template!, doc.setting)
          : await saveRoute(doc);
      setDoc(saved);
      setDirty(false);
      setResult(null);
      await load();
    });
  }
  async function open(id: number) {
    if (!confirmLeave()) return;
    await run(async () => {
      setDoc(await readRoute(id));
      setDirty(false);
      setResult(null);
      setOperation(null);
      setTemplate(null);
    });
  }
  const application = doc?.setting.kind === "application";
  const parameter = look?.parameters.find(x => x.id === doc?.setting.parameterId);
  const parameterUnit = look?.units.find(x => x.id === parameter?.unitId)?.shortName;
  const parameterLabel = parameter
    ? `${parameter.name}${parameterUnit ? `, ${parameterUnit}` : ""}`
    : "Значение параметра";

  return (
    <div className="page">
      <header className="page-header">
        <div>
          <h1 className="page-title">Маршруты</h1>
          <p className="hint">
            Операции, материалы, труд и прямая себестоимость партии
          </p>
        </div>
        <button
          className="btn-primary"
          disabled={busy || !look}
          onClick={() => {
            if (confirmLeave()) {
              setDoc(empty(initialProductId ?? 0));
              setDirty(false);
              setResult(null);
              setTemplate(null);
              setOperation(null);
              setQuantity(1);
            }
          }}
        >
          Создать маршрут
        </button>
      </header>
      {initialProductId && (
        <p className="hint">
          Выберите маршрут изделия «
          {look?.products.find((x) => x.id === initialProductId)?.name}» в
          списке. Для полного списка откройте «Маршруты» в навигации.
        </p>
      )}
      {err && (
        <div className="error-banner" role="alert">
          {err}
        </div>
      )}
      {look && (
        <RouteResources
          lookups={look}
          onChanged={async () => {
            setLook(await apiGet<RouteLookups>("/api/routes/lookups"));
          }}
        />
      )}
      <section className="card route-table-scroll">
        <table className="data data-table">
          <thead>
            <tr>
              <th>Маршрут</th>
              <th>Изделие</th>
              <th>Исполнение</th>
              <th>Вид</th>
            </tr>
          </thead>
          <tbody>
            {rows
              .filter(
                (r) => !initialProductId || r.productId === initialProductId,
              )
              .map((r) => (
                <tr key={r.id}>
                  <td>
                    <button
                      className="btn-ghost"
                      disabled={busy}
                      onClick={() => void open(r.id)}
                    >
                      {r.name}
                    </button>
                  </td>
                  <td>{r.productName}</td>
                  <td>{r.revisionName ?? "Без именованного исполнения"}</td>
                  <td>
                    {r.kind === "template"
                      ? "Типовой процесс"
                      : r.kind === "application"
                        ? "Применение шаблона"
                        : "Самостоятельный"}
                  </td>
                </tr>
              ))}
          </tbody>
        </table>
        {!rows.length && (
          <p className="hint">Маршрутов пока нет. Создайте первый маршрут.</p>
        )}
      </section>
      {doc && look && (
        <section className="card">
          <h2>{doc.setting.id ? "Редактор маршрута" : "Новый маршрут"}</h2>
          <div className="route-grid">
            <label>
              Название маршрута
              <input
                aria-label="Название маршрута"
                className="input"
                value={doc.setting.name}
                disabled={application && !!doc.setting.id}
                onChange={(e) => setting({ name: e.target.value })}
              />
            </label>
            <Select
              label="Изделие"
              value={doc.setting.productId}
              disabled={!!doc.setting.id || doc.steps.length > 0}
              items={look.products}
              onChange={(v) => setting({ productId: v ?? 0, revisionId: null })}
            />
            <Select
              label="Исполнение (необязательно)"
              optional
              value={doc.setting.revisionId}
              disabled={application && !!doc.setting.id}
              items={look.variants.filter(
                (x) => x.productId === doc.setting.productId,
              )}
              onChange={(v) => setting({ revisionId: v })}
            />
            <label>
              Вид маршрута
              <select
                className="input"
                aria-label="Вид маршрута"
                value={doc.setting.kind}
                disabled={!!doc.setting.id || doc.steps.length > 0}
                onChange={(e) => setting({ kind: e.target.value })}
              >
                <option value="standalone">Самостоятельный</option>
                <option value="template">Типовой процесс (шаблон)</option>
                <option value="application">
                  Применение типового процесса
                </option>
              </select>
            </label>
            {application && !doc.setting.id && (
              <Select
                label="Типовой процесс"
                value={template}
                items={rows.filter(
                  (r) =>
                    r.kind === "template" &&
                    r.productId === doc.setting.productId,
                )}
                onChange={(v) => {
                  setTemplate(v);
                  const t = rows.find((x) => x.id === v);
                  if (t)
                    setting({
                      parameterId: t.parameterId,
                      parameterValue: t.parameterValue,
                      finalStepId: t.finalStepId,
                    });
                }}
              />
            )}
            {!application && (
              <Select
                label="Числовой параметр процесса"
                optional
                value={doc.setting.parameterId}
                items={look.parameters}
                onChange={(v) =>
                  setting({ parameterId: v, parameterValue: v ? 1 : null })
                }
              />
            )}
            {doc.setting.parameterId && (
              <label>
                {parameterLabel}
                <input
                  className="input"
                  type="number"
                  min="0.000001"
                  step="any"
                  aria-label={parameterLabel}
                  value={doc.setting.parameterValue ?? ""}
                  onChange={(e) =>
                    setting({
                      parameterValue: e.target.value
                        ? Number(e.target.value)
                        : null,
                    })
                  }
                />
              </label>
            )}
          </div>
          {application ? (
            <p className="hint">
              Операции общие с шаблоном.{" "}
              {!!doc.setting.id && (
                <button
                  className="btn-ghost"
                  onClick={() => {
                    const t = rows.find(
                      (x) =>
                        x.kind === "template" &&
                        x.finalStepId === doc.setting.finalStepId,
                    );
                    if (t) void open(t.id);
                  }}
                >
                  Открыть шаблон
                </button>
              )}
            </p>
          ) : (
            <>
              <h3>Операции</h3>
              {doc.steps
                .filter(
                  (s) =>
                    s.kind === "production" &&
                    (s.id <= 0 ||
                      !doc.editableStepIds ||
                      doc.editableStepIds.includes(s.id)),
                )
                .sort((a, b) => a.number - b.number)
                .map((s) => (
                  <details className="route-operation" key={s.id}>
                    <summary>
                      {String(s.number).padStart(3, "0")} ·{" "}
                      {s.resultName || "Новая операция"} — редактировать
                    </summary>
                    <div className="route-grid">
                      <label>
                        Номер операции
                        <input
                          aria-label="Номер операции"
                          className="input"
                          type="number"
                          min="1"
                          value={s.number}
                          onChange={(e) =>
                            updateStep(s.id, { number: Number(e.target.value) })
                          }
                        />
                      </label>
                      <label>
                        Действие и результат
                        <input
                          aria-label="Действие и результат"
                          className="input"
                          value={s.resultName}
                          onChange={(e) =>
                            updateStep(s.id, { resultName: e.target.value })
                          }
                        />
                      </label>
                      <Select
                        label="Изделие результата"
                        value={s.productId}
                        items={look.products}
                        onChange={(v) => {
                          const p = look.products.find((x) => x.id === v);
                          if (p?.unitId)
                            updateStep(s.id, {
                              productId: p.id,
                              unitId: p.unitId,
                            });
                        }}
                      />
                      <Select
                        label="Класс операции"
                        value={s.operationId}
                        items={look.operations}
                        onChange={(v) => updateStep(s.id, { operationId: v })}
                      />
                      <Select
                        label="Рабочий центр"
                        value={s.workCenterId}
                        items={look.centers.map((c) => ({
                          ...c,
                          name: `${c.name} — ${c.department}`,
                        }))}
                        onChange={(v) => updateStep(s.id, { workCenterId: v })}
                      />
                      <Select
                        label="Профессия"
                        value={s.professionId}
                        items={look.professions}
                        onChange={(v) => updateStep(s.id, { professionId: v })}
                      />
                      <Select
                        label="Разряд"
                        value={s.qualificationId}
                        items={look.qualifications}
                        onChange={(v) =>
                          updateStep(s.id, { qualificationId: v })
                        }
                      />
                      <label>
                        Инструмент и оснастка
                        <input
                          aria-label="Инструмент и оснастка"
                          className="input"
                          value={s.tooling}
                          onChange={(e) =>
                            updateStep(s.id, { tooling: e.target.value })
                          }
                        />
                      </label>
                      {(
                        ["setupMinutes", "unitMinutes", "hourlyRate"] as const
                      ).map((k, i) => (
                        <label key={k}>
                          {
                            [
                              "Подготовка партии, мин",
                              "Время на единицу, мин",
                              "Ставка, ₽/ч",
                            ][i]
                          }
                          <input
                            aria-label={
                              [
                                "Подготовка партии, мин",
                                "Время на единицу, мин",
                                "Ставка, ₽/ч",
                              ][i]
                            }
                            className="input"
                            type="number"
                            step="any"
                            min="0"
                            value={s[k]}
                            onChange={(e) =>
                              updateStep(s.id, { [k]: Number(e.target.value) })
                            }
                          />
                        </label>
                      ))}
                    </div>
                    <h4>Что требуется</h4>
                    <p className="hint">
                      Материал, результат операции или вложенного маршрута
                    </p>
                    {doc.inputs.map((edge, index) =>
                      edge.stepId !== s.id ? null : (
                        <div className="route-input" key={index}>
                          <Select
                            label="Входной ресурс"
                            value={edge.sourceId}
                            items={[
                              ...doc.steps
                                .filter((x) => x.id !== s.id)
                                .map((x) => ({
                                  id: x.id,
                                  name: `${x.kind === "purchase" ? "Закупка" : "Результат " + x.number}: ${x.resultName}`,
                                })),
                              ...look.sources
                                .filter(
                                  (x) => !doc.steps.some((s) => s.id === x.id),
                                )
                                .map((x) => ({
                                  id: x.id,
                                  name: `Вложенный узел: ${x.resultName}`,
                                })),
                            ]}
                            onChange={(v) =>
                              change({
                                ...doc,
                                inputs: doc.inputs.map((x, i) =>
                                  i === index ? { ...x, sourceId: v ?? 0 } : x,
                                ),
                              })
                            }
                          />
                          <label>
                            Количество ресурса
                            <input
                              aria-label="Количество ресурса"
                              className="input"
                              type="number"
                              min="0.000001"
                              step="any"
                              value={edge.amount}
                              onChange={(e) =>
                                change({
                                  ...doc,
                                  inputs: doc.inputs.map((x, i) =>
                                    i === index
                                      ? { ...x, amount: Number(e.target.value) }
                                      : x,
                                  ),
                                })
                              }
                            />
                          </label>
                          <label>
                            Базовый выпуск
                            <input
                              aria-label="Базовый выпуск"
                              aria-describedby={index === doc.inputs.findIndex(x => x.stepId === s.id) ? `route-base-${s.id}` : undefined}
                              className="input"
                              type="number"
                              min="0.000001"
                              step="any"
                              value={edge.forQuantity}
                              onChange={(e) =>
                                change({
                                  ...doc,
                                  inputs: doc.inputs.map((x, i) =>
                                    i === index
                                      ? {
                                          ...x,
                                          forQuantity: Number(e.target.value),
                                        }
                                      : x,
                                  ),
                                })
                              }
                            />
                            {index === doc.inputs.findIndex(x => x.stepId === s.id) && (
                              <small className="hint" id={`route-base-${s.id}`}>
                                На сколько единиц результата задана норма
                              </small>
                            )}
                          </label>
                          <Select
                            label="Параметр нормы"
                            emptyLabel="Фиксированная норма"
                            optional
                            value={edge.parameterId}
                            items={look.parameters.filter(
                              (x) => x.id === doc.setting.parameterId,
                            )}
                            onChange={(v) =>
                              change({
                                ...doc,
                                inputs: doc.inputs.map((x, i) =>
                                  i === index ? { ...x, parameterId: v } : x,
                                ),
                              })
                            }
                          />
                          <button
                            className="btn-ghost"
                            onClick={() =>
                              change({
                                ...doc,
                                inputs: doc.inputs.filter(
                                  (_, i) => i !== index,
                                ),
                              })
                            }
                          >
                            Убрать вход
                          </button>
                        </div>
                      ),
                    )}
                    <div className="route-actions">
                      <button
                        className="btn-secondary"
                        onClick={() =>
                          change({
                            ...doc,
                            inputs: [
                              ...doc.inputs,
                              {
                                stepId: s.id,
                                sourceId: 0,
                                amount: 1,
                                forQuantity: 1,
                                parameterId: null,
                              },
                            ],
                          })
                        }
                      >
                        Добавить вход
                      </button>
                      <Select
                        label="Добавить закупной материал"
                        optional
                        value={null}
                        items={look.products}
                        onChange={(v) => {
                          const p = look.products.find((x) => x.id === v);
                          if (!p?.unitId) return;
                          const existing = doc.steps.find(
                            (x) => x.kind === "purchase" && x.productId === v,
                          );
                          const id = existing?.id ?? nextId();
                          change({
                            ...doc,
                            steps: existing
                              ? doc.steps
                              : [
                                  ...doc.steps,
                                  {
                                    ...s,
                                    id,
                                    kind: "purchase",
                                    productId: p.id,
                                    unitId: p.unitId,
                                    resultName: p.name,
                                    number: 0,
                                    operationId: null,
                                    workCenterId: null,
                                    professionId: null,
                                    qualificationId: null,
                                    tooling: "",
                                    setupMinutes: 0,
                                    unitMinutes: 0,
                                    hourlyRate: 0,
                                  },
                                ],
                            inputs: [
                              ...doc.inputs,
                              {
                                stepId: s.id,
                                sourceId: id,
                                amount: 1,
                                forQuantity: 1,
                                parameterId: null,
                              },
                            ],
                          });
                        }}
                      />
                      <button
                        className="btn-ghost"
                        onClick={() => {
                          if (doc.inputs.some((x) => x.sourceId === s.id)) {
                            setErr(
                              "Сначала уберите ссылки на результат операции.",
                            );
                            return;
                          }
                          change({
                            ...doc,
                            steps: doc.steps.filter((x) => x.id !== s.id),
                            inputs: doc.inputs.filter((x) => x.stepId !== s.id),
                            setting: {
                              ...doc.setting,
                              finalStepId:
                                doc.setting.finalStepId === s.id
                                  ? 0
                                  : doc.setting.finalStepId,
                            },
                          });
                        }}
                      >
                        Убрать операцию
                      </button>
                    </div>
                  </details>
                ))}
              <div className="route-actions">
                <button className="btn-secondary" onClick={addOperation}>
                  Добавить операцию
                </button>
                <Select
                  label="Итоговая операция"
                  value={doc.setting.finalStepId}
                  items={doc.steps
                    .filter((s) => s.kind === "production")
                    .map((s) => ({
                      id: s.id,
                      name: `${s.number} ${s.resultName}`,
                    }))}
                  onChange={(v) => setting({ finalStepId: v ?? 0 })}
                />
              </div>
            </>
          )}
          <button
            className="btn-primary"
            disabled={
              busy ||
              (!dirty && !!doc.setting.id) ||
              (!doc.setting.id && application && !template)
            }
            onClick={() => void save()}
          >
            {busy ? "Сохранение…" : "Сохранить маршрут"}
          </button>
          {dirty && <span className="hint"> Есть несохранённые изменения</span>}
          {!!doc.setting.id && (
            <section className="route-actions">
              <label>
                Размер партии
                <input
                  aria-label="Размер партии"
                  className="input"
                  type="number"
                  min="0.000001"
                  step="any"
                  value={quantity}
                  onChange={(e) => {
                    setQuantity(Number(e.target.value));
                    setResult(null);
                  }}
                />
              </label>
              <Select
                label="Входы операции"
                value={operation ?? doc.setting.finalStepId}
                items={doc.steps
                  .filter((x) => x.kind === "production")
                  .map((x) => ({
                    id: x.id,
                    name: `${x.number} ${x.resultName}`,
                  }))}
                onChange={(v) => {
                  setOperation(v);
                  setResult(null);
                }}
              />
              <button
                className="btn-warm"
                disabled={busy || dirty || quantity <= 0}
                onClick={() =>
                  void run(async () =>
                    setResult(
                      await calculateRoute(
                        doc.setting.id,
                        quantity,
                        operation ?? undefined,
                      ),
                    ),
                  )
                }
              >
                Рассчитать
              </button>
            </section>
          )}
        </section>
      )}
      {result && <RouteResults result={result} />}
    </div>
  );
}
