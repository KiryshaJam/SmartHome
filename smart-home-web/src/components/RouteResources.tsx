import { useState, type FormEvent } from "react";
import { createDepartment, createWorkCenter } from "../api/routes";
import type { RouteLookups } from "../types/routes";
import { useUnsavedChanges } from "./UnsavedChanges";

export function RouteResources({
  lookups,
  onChanged,
}: {
  lookups: RouteLookups;
  onChanged: () => Promise<void>;
}) {
  const [departmentName, setDepartmentName] = useState("");
  const [departmentType, setDepartmentType] = useState("");
  const [centerName, setCenterName] = useState("");
  const [centerType, setCenterType] = useState("");
  const [departmentId, setDepartmentId] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");
  const [notice, setNotice] = useState("");
  useUnsavedChanges(Boolean(departmentName || centerName));

  async function submit(e: FormEvent, kind: "department" | "center") {
    e.preventDefault();
    setBusy(true);
    setError("");
    setNotice("");
    try {
      if (kind === "department") {
        const created = await createDepartment(
          departmentName.trim(),
          Number(departmentType),
        );
        setDepartmentName("");
        setDepartmentId(String(created.id));
        setNotice(`Подразделение «${created.name}» создано.`);
      } else {
        const created = await createWorkCenter(
          centerName.trim(),
          Number(centerType),
          Number(departmentId),
        );
        setCenterName("");
        setNotice(
          `Рабочий центр «${created.name}» создан. Его можно выбрать в операции.`,
        );
      }
      await onChanged();
    } catch (e) {
      setError(e instanceof Error ? e.message : String(e));
    } finally {
      setBusy(false);
    }
  }

  return (
    <details className="card route-resources" id="route-resources">
      <summary>Подразделения и рабочие центры</summary>
      <p className="hint">
        Сначала создайте подразделение, затем рабочий центр в нём. Типы
        добавляются в классификаторе в ветвях «Типы подразделений» и «Типы
        рабочих центров» как конечные классы.
      </p>
      {error && (
        <div className="error-banner" role="alert">
          {error}
        </div>
      )}
      {notice && <p role="status">{notice}</p>}
      <div className="route-grid">
        <form onSubmit={(e) => void submit(e, "department")}>
          <h3>Новое подразделение</h3>
          <fieldset disabled={busy}>
            <label>
              Название подразделения
              <input
                className="input"
                required
                maxLength={256}
                value={departmentName}
                placeholder="Например, участок сборки датчиков"
                onChange={(e) => setDepartmentName(e.target.value)}
              />
            </label>
            <label>
              Тип подразделения
              <select
                className="input"
                required
                value={departmentType}
                onChange={(e) => setDepartmentType(e.target.value)}
              >
                <option value="">Выберите тип…</option>
                {lookups.departmentTypes.map((t) => (
                  <option key={t.id} value={t.id}>
                    {t.name}
                  </option>
                ))}
              </select>
            </label>
            {!lookups.departmentTypes.length && (
              <p className="hint">
                Добавьте конечный тип в ветви «Типы подразделений»
                классификатора.
              </p>
            )}
            <button
              className="btn-secondary"
              type="submit"
              disabled={!departmentName.trim() || !departmentType}
            >
              Создать подразделение
            </button>
          </fieldset>
        </form>
        <form onSubmit={(e) => void submit(e, "center")}>
          <h3>Новый рабочий центр</h3>
          <fieldset disabled={busy}>
            <label>
              Название рабочего центра
              <input
                className="input"
                required
                maxLength={256}
                value={centerName}
                placeholder="Например, пост проверки датчиков"
                onChange={(e) => setCenterName(e.target.value)}
              />
            </label>
            <label>
              Подразделение центра
              <select
                className="input"
                required
                value={departmentId}
                onChange={(e) => setDepartmentId(e.target.value)}
              >
                <option value="">Выберите подразделение…</option>
                {lookups.departments.map((d) => (
                  <option key={d.id} value={d.id}>
                    {d.name}
                  </option>
                ))}
              </select>
            </label>
            <label>
              Тип рабочего центра
              <select
                className="input"
                required
                value={centerType}
                onChange={(e) => setCenterType(e.target.value)}
              >
                <option value="">Выберите тип…</option>
                {lookups.centerTypes.map((t) => (
                  <option key={t.id} value={t.id}>
                    {t.name}
                  </option>
                ))}
              </select>
            </label>
            {!lookups.centerTypes.length && (
              <p className="hint">
                Добавьте конечный тип в ветви «Типы рабочих центров»
                классификатора.
              </p>
            )}
            <button
              className="btn-secondary"
              type="submit"
              disabled={!centerName.trim() || !centerType || !departmentId}
            >
              Создать рабочий центр
            </button>
          </fieldset>
        </form>
      </div>
      <h3>Созданные подразделения</h3>
      <ul>
        {lookups.departments.map((d) => (
          <li key={d.id}>
            {d.name} —{" "}
            {lookups.departmentTypes.find((t) => t.id === d.typeId)?.name ??
              "Тип не найден"}
          </li>
        ))}
      </ul>
      <h3>Созданные рабочие центры</h3>
      <ul>
        {lookups.centers.map((c) => (
          <li key={c.id}>
            {c.name} — {c.department}
          </li>
        ))}
      </ul>
    </details>
  );
}
