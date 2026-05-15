/** Соответствует SmartHome.Models.ParameterValueType */
export type ParameterValueType = 1 | 2 | 3 | 4 | 5;

export const ParameterValueTypes = {
  Integer: 1 as const,
  Number: 2 as const,
  String: 3 as const,
  DateTime: 4 as const,
  Enum: 5 as const,
};

export interface ClassNodeDto {
  id: number;
  name: string;
  shortName: string;
  isTerminal: boolean;
  sortOrder: number;
  parentId: number | null;
  measureUnitId: number | null;
  measureUnitName: string | null;
  measureUnitShortName: string | null;
}

export interface TreeNodeDto {
  id: number;
  name: string;
  shortName: string;
  isTerminal: boolean;
  sortOrder: number;
  parentId: number | null;
  children: TreeNodeDto[];
}

export interface ProductDto {
  id: number;
  name: string;
  shortName: string;
  classNodeId: number;
  classNodeName: string;
}

export interface ProductParameterValueDto {
  id: number;
  productId: number;
  productName: string;
  classParameterId: number;
  parameterDefinitionId: number;
  parameterName: string;
  parameterShortName: string;
  valueType: ParameterValueType;
  parameterGroupId: number | null;
  parameterGroupName: string | null;
  sortOrder: number;
  isRequired: boolean;
  minNumberValue: number | null;
  maxNumberValue: number | null;
  measureUnitId: number | null;
  measureUnitName: string | null;
  measureUnitShortName: string | null;
  enumClassId: number | null;
  enumClassName: string | null;
  integerValue: number | null;
  numberValue: number | null;
  stringValue: string | null;
  dateTimeValue: string | null;
  enumValueId: number | null;
  enumDisplayName: string | null;
  enumStringValue: string | null;
  enumNumberValue: number | null;
  enumIconValue: string | null;
}

export interface ProductWithParametersDto {
  id: number;
  name: string;
  shortName: string;
  classNodeId: number;
  classNodeName: string;
  parameters: ProductParameterValueDto[];
}

export interface ClassParameterDto {
  id: number;
  classNodeId: number;
  classNodeName: string;
  parameterDefinitionId: number;
  parameterName: string;
  parameterShortName: string;
  valueType: ParameterValueType;
  measureUnitId: number | null;
  measureUnitName: string | null;
  measureUnitShortName: string | null;
  enumClassId: number | null;
  enumClassName: string | null;
  parameterGroupId: number | null;
  parameterGroupName: string | null;
  sortOrder: number;
  isRequired: boolean;
  isInherited: boolean;
  minNumberValue: number | null;
  maxNumberValue: number | null;
}

export interface ProductFilterDto {
  classNodeId: number;
  classParameterId?: number | null;
  integerValue?: number | null;
  numberFrom?: number | null;
  numberTo?: number | null;
  stringContains?: string | null;
  dateFrom?: string | null;
  dateTo?: string | null;
  enumValueId?: number | null;
}

export interface CreateProductDto {
  name: string;
  shortName: string;
  classNodeId: number;
}

export interface WriteProductParameterValueDto {
  integerValue?: number | null;
  numberValue?: number | null;
  stringValue?: string | null;
  dateTimeValue?: string | null;
  enumValueId?: number | null;
}

export interface EnumValueDto {
  id: number;
  enumClassId: number;
  stringValue: string | null;
  numberValue: number | null;
  iconValue: string | null;
  displayName: string | null;
  sortOrder: number;
}

/** SmartHome.Models.EnumValueType */
export type EnumValueTypeNum = 1 | 2 | 3;

export interface EnumClassDto {
  id: number;
  name: string;
  shortName: string;
  valueType: EnumValueTypeNum;
  sortOrder: number;
  measureUnitId: number | null;
  measureUnitName: string | null;
  measureUnitShortName: string | null;
}

export interface MeasureUnitDto {
  id: number;
  name: string;
  shortName: string;
}

export interface ParameterGroupDto {
  id: number;
  name: string;
  shortName: string;
  sortOrder: number;
}

export interface ParameterDefinitionDto {
  id: number;
  name: string;
  shortName: string;
  valueType: ParameterValueType;
  measureUnitId: number | null;
  measureUnitName: string | null;
  measureUnitShortName: string | null;
  enumClassId: number | null;
  enumClassName: string | null;
}

export interface CreateClassNodeDto {
  name: string;
  shortName: string;
  isTerminal: boolean;
  sortOrder: number;
  parentId?: number | null;
  measureUnitId?: number | null;
}

export interface CreateParameterGroupDto {
  name: string;
  shortName: string;
  sortOrder: number;
}

export interface CreateParameterDefinitionDto {
  name: string;
  shortName: string;
  valueType: ParameterValueType;
  measureUnitId?: number | null;
  enumClassId?: number | null;
}

export interface CreateClassParameterDto {
  parameterDefinitionId: number;
  parameterGroupId?: number | null;
  sortOrder: number;
  isRequired: boolean;
  minNumberValue?: number | null;
  maxNumberValue?: number | null;
}

export interface CreateMeasureUnitDto {
  name: string;
  shortName: string;
}

export interface CreateEnumClassDto {
  name: string;
  shortName: string;
  valueType: EnumValueTypeNum;
  sortOrder: number;
  measureUnitId?: number | null;
}
