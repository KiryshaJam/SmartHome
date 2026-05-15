import { formatDateTimeLocal, isoToDateTimeLocal } from "../utils/datetime";

export function ParameterValueEditor({
  valueType,
  integerValue,
  numberValue,
  stringValue,
  dateTimeValue,
  enumValueId,
  minNumberValue,
  maxNumberValue,
  enumOptions,
  onIntChange,
  onNumChange,
  onStrChange,
  onDateChange,
  onEnumChange,
}: {
  valueType: number;
  integerValue: number | null;
  numberValue: number | null;
  stringValue: string | null;
  dateTimeValue: string | null;
  enumValueId: number | null;
  minNumberValue: number | null;
  maxNumberValue: number | null;
  enumOptions: { id: number; label: string }[];
  onIntChange: (v: number | null) => void;
  onNumChange: (v: number | null) => void;
  onStrChange: (v: string) => void;
  onDateChange: (v: string | null) => void;
  onEnumChange: (v: number | null) => void;
}) {
  if (valueType === 1) {
    return (
      <input
        className="input mono"
        type="number"
        step={1}
        min={minNumberValue ?? undefined}
        max={maxNumberValue ?? undefined}
        value={integerValue ?? ""}
        onChange={(e) => {
          const raw = e.target.value;
          onIntChange(raw === "" ? null : Number.parseInt(raw, 10));
        }}
      />
    );
  }
  if (valueType === 2) {
    return (
      <input
        className="input mono"
        type="number"
        step="any"
        min={minNumberValue ?? undefined}
        max={maxNumberValue ?? undefined}
        value={numberValue ?? ""}
        onChange={(e) => {
          const raw = e.target.value;
          onNumChange(raw === "" ? null : Number.parseFloat(raw));
        }}
      />
    );
  }
  if (valueType === 3) {
    return (
      <input
        className="input"
        type="text"
        value={stringValue ?? ""}
        onChange={(e) => onStrChange(e.target.value)}
      />
    );
  }
  if (valueType === 4) {
    return (
      <input
        className="input mono"
        type="datetime-local"
        value={isoToDateTimeLocal(dateTimeValue)}
        onChange={(e) => onDateChange(formatDateTimeLocal(e.target.value))}
      />
    );
  }
  if (valueType === 5) {
    return (
      <select
        className="input"
        value={enumValueId ?? ""}
        onChange={(e) => {
          const v = e.target.value;
          onEnumChange(v === "" ? null : Number.parseInt(v, 10));
        }}
      >
        <option value="">— выберите значение —</option>
        {enumOptions.map((o) => (
          <option key={o.id} value={o.id}>
            {o.label}
          </option>
        ))}
      </select>
    );
  }
  return <span className="hint">Неизвестный тип</span>;
}
