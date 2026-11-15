import type { RouteCalculation } from "../types/routes";
const n = (v: number | null) =>
  v == null
    ? "Не задано"
    : v.toLocaleString("ru-RU", { maximumFractionDigits: 4 });
export function RouteResults({ result: r }: { result: RouteCalculation }) {
  return (
    <section className="card route-results">
      <h2>Расчёт партии: {n(r.quantity)}</h2>
      {r.missingPrices.length > 0 && (
        <div className="error-banner">
          Не заданы цены: {r.missingPrices.join(", ")}. Итоговая себестоимость
          не определена.
        </div>
      )}
      <p>
        Материалы: <strong>{n(r.materialCost)} ₽</strong> · Время:{" "}
        <strong>{n(r.minutes)} мин</strong> · Труд:{" "}
        <strong>{n(r.laborCost)} ₽</strong>
      </p>
      <p>
        Прямая себестоимость партии: <strong>{n(r.total)} ₽</strong> · На
        единицу: <strong>{n(r.unitCost)} ₽</strong>
      </p>
      <h3>Полный маршрут</h3>
      <div className="route-table-scroll">
        <table className="data data-table">
          <thead>
            <tr>
              <th>Уровень / операция и результат</th>
              <th>Вид</th>
              <th>Требуется</th>
            </tr>
          </thead>
          <tbody>
            {r.tree.map((x) => (
              <tr key={x.path.join("/")}>
                <td style={{ paddingLeft: `${12 + x.depth * 20}px` }}>
                  {x.depth} ·{" "}
                  {x.number ? String(x.number).padStart(3, "0") + " " : ""}
                  {x.result_name}
                </td>
                <td>{x.kind === "purchase" ? "Закупка" : "Производство"}</td>
                <td>
                  {n(x.required_quantity)} {x.unit}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      <h3>Что требуется выбранной операции</h3>
      <p className="hint">
        Количество — выпуск выбранной операции
      </p>
      <div className="route-table-scroll">
        <table className="data data-table">
          <thead>
            <tr>
              <th>Вход</th>
              <th>Вид</th>
              <th>Норма / выпуск</th>
              <th>Требуется</th>
            </tr>
          </thead>
          <tbody>
            {r.inputs.map((x) => (
              <tr key={x.source_id}>
                <td>{x.name}</td>
                <td>
                  {x.kind === "purchase" ? "Материал" : "Промежуточный результат"}
                </td>
                <td>
                  {n(x.amount)} / {n(x.for_quantity)}
                </td>
                <td>
                  {n(x.required_quantity)} {x.unit}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      <h3>Суммарные закупные материалы</h3>
      <div className="route-table-scroll">
        <table className="data data-table">
          <thead>
            <tr>
              <th>Материал</th>
              <th>Количество</th>
              <th>Цена, ₽</th>
              <th>Стоимость, ₽</th>
            </tr>
          </thead>
          <tbody>
            {r.materials.map((x) => (
              <tr key={x.product_id + ":" + x.unit}>
                <td>{x.name}</td>
                <td>
                  {n(x.required_quantity)} {x.unit}
                </td>
                <td>{n(x.price)}</td>
                <td>{n(x.cost)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      <h3>Труд по операциям и профессиям</h3>
      <div className="route-table-scroll">
        <table className="data data-table">
          <thead>
            <tr>
              <th>Операция</th>
              <th>Профессия</th>
              <th>Выпуск</th>
              <th>Подготовка</th>
              <th>Мин/ед.</th>
              <th>Минуты</th>
              <th>₽/ч</th>
              <th>Труд, ₽</th>
            </tr>
          </thead>
          <tbody>
            {r.labor.map((x) => (
              <tr key={x.step_id}>
                <td>
                  {String(x.number).padStart(3, "0")} {x.name}
                </td>
                <td>{x.profession}</td>
                <td>{n(x.required_quantity)}</td>
                <td>{n(x.setup_minutes)}</td>
                <td>{n(x.unit_minutes)}</td>
                <td>{n(x.minutes)}</td>
                <td>{n(x.hourly_rate)}</td>
                <td>{n(x.cost)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </section>
  );
}
