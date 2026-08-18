#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace JxModule.DataTable
{
    public static class DataTableValidator
    {
        public static DataTableValidationResult Validate(DataTable dataTable)
        {
            var result = new DataTableValidationResult();

            if (!dataTable)
            {
                result.AddError("DataTable is null.");
                return result;
            }

            if (!dataTable.dataTableCsv)
            {
                result.AddError("CSV is missing.");
                return result;
            }

            var rowType = dataTable.GetRowType();
            if (rowType == null)
            {
                result.AddError("Row Type is missing.");
                return result;
            }

            var csvRows = CsvReader.Read(dataTable.dataTableCsv);
            if (csvRows == null)
            {
                result.AddError("Failed to read CSV.");
                return result;
            }

            ValidateHeaders(rowType, csvRows, result);
            ValidateRows(rowType, csvRows, result);

            return result;
        }

        private static void ValidateHeaders(Type rowType,
                                            List<Dictionary<string, string>> rows,
                                            DataTableValidationResult result)
        {
            if (rows.Count == 0)
            {
                result.AddError("CSV has no rows.");
                return;
            }

            var headers = rows[0].Keys.ToList();

            if (!headers.Contains("rowID", StringComparer.OrdinalIgnoreCase))
            {
                result.AddError("Required column 'rowID' is missing.");
            }

            if (!headers.Contains("isEnable", StringComparer.OrdinalIgnoreCase))
            {
                result.AddError("Required column 'isEnable' is missing.");
            }

            var fields = GetSerializedFields(rowType);

            foreach (var field in fields)
            {
                if (headers.Contains(field.Name, StringComparer.OrdinalIgnoreCase))
                {
                    continue;
                }

                result.AddError($"Required column '{field.Name}' is missing.");
            }
        }

        private static void ValidateRows(Type rowType,
                                         List<Dictionary<string, string>> rows,
                                         DataTableValidationResult result)
        {
            var rowIDs = new HashSet<string>();
            var fields = GetSerializedFields(rowType);

            for (var i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                var rowNumber = i + 2;

                if (!TryGetValue(row, "rowID", out var rowID))
                {
                    result.AddError($"Row {rowNumber}: 'rowID' column is missing.");

                    continue;
                }

                if (string.IsNullOrWhiteSpace(rowID))
                {
                    result.AddError($"Row {rowNumber}: rowID is empty.");
                }
                else if (!rowIDs.Add(rowID))
                {
                    result.AddError($"Row {rowNumber}: Duplicate rowID '{rowID}'.");
                }

                if (!TryGetValue(row, "isEnable", out var isEnableRaw))
                {
                    result.AddError($"Row {rowNumber}: 'isEnable' column is missing.");
                }
                else if (!bool.TryParse(isEnableRaw, out _))
                {
                    result.AddError($"Row {rowNumber}: Cannot convert '{isEnableRaw}' to bool for 'isEnable'.");
                }

                foreach (var field in fields)
                {
                    if (!TryGetValue(row, field.Name, out var rawValue))
                    {
                        continue;
                    }

                    ValidateValue(field, rawValue, rowID, rowNumber, result);
                }
            }
        }

        private static void ValidateValue(FieldInfo field,
                                          string rawValue,
                                          string rowID,
                                          int rowNumber,
                                          DataTableValidationResult result)
        {
            var type = field.FieldType;

            if (type == typeof(string))
            {
                return;
            }

            if (type == typeof(int))
            {
                if (!int.TryParse(rawValue, out _))
                {
                    AddConversionError(field, rawValue, rowID, rowNumber, result);
                }

                return;
            }

            if (type == typeof(float))
            {
                if (!float.TryParse(rawValue, out _))
                {
                    AddConversionError(field, rawValue, rowID, rowNumber, result);
                }

                return;
            }

            if (type == typeof(double))
            {
                if (!double.TryParse(rawValue, out _))
                {
                    AddConversionError(field, rawValue, rowID, rowNumber, result);
                }

                return;
            }

            if (type == typeof(bool))
            {
                if (!bool.TryParse(rawValue, out _))
                {
                    AddConversionError(field, rawValue, rowID, rowNumber, result);
                }

                return;
            }

            if (type == typeof(Color))
            {
                if (!string.IsNullOrWhiteSpace(rawValue) &&
                    !ColorUtility.TryParseHtmlString(rawValue, out _))
                {
                    AddConversionError(field, rawValue, rowID, rowNumber, result);
                }

                return;
            }

            if (type.IsEnum)
            {
                if (!Enum.TryParse(type, rawValue, true, out _))
                {
                    AddConversionError(field, rawValue, rowID, rowNumber, result);
                }

                return;
            }

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            {
                ValidateList(field, rawValue, rowID, rowNumber, result);
            }
        }

        private static void ValidateList(FieldInfo field,
                                         string rawValue,
                                         string rowID,
                                         int rowNumber,
                                         DataTableValidationResult result)
        {
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return;
            }

            var elementType = field.FieldType.GetGenericArguments()[0];
            var values = rawValue.SplitStringList();

            foreach (var value in values)
            {
                if (elementType == typeof(string))
                {
                    continue;
                }

                if (elementType == typeof(int))
                {
                    if (!int.TryParse(value, out _))
                    {
                        AddConversionError(field, value, rowID, rowNumber, result);
                    }

                    continue;
                }

                if (elementType == typeof(float))
                {
                    if (!float.TryParse(value, out _))
                    {
                        AddConversionError(field, value, rowID, rowNumber, result);
                    }

                    continue;
                }

                if (elementType == typeof(bool))
                {
                    if (!bool.TryParse(value, out _))
                    {
                        AddConversionError(field, value, rowID, rowNumber, result);
                    }

                    continue;
                }

                if (elementType == typeof(Color))
                {
                    if (!ColorUtility.TryParseHtmlString(value, out _))
                    {
                        AddConversionError(field, value, rowID, rowNumber, result);
                    }

                    continue;
                }

                if (elementType.IsEnum &&
                    !Enum.TryParse(elementType, value, true, out _))
                {
                    AddConversionError(field, value, rowID, rowNumber, result);
                }
            }
        }

        private static void AddConversionError(FieldInfo field,
                                               string rawValue,
                                               string rowID,
                                               int rowNumber,
                                               DataTableValidationResult result)
        {
            result.AddError($"Row {rowNumber} [{rowID}] - Column '{field.Name}': Cannot convert '{rawValue}' to {field.FieldType.Name}.");
        }

        private static List<FieldInfo> GetSerializedFields(Type rowType)
        {
            return rowType
                .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(IsSerializedField)
                .Where(x =>
                    !string.Equals(x.Name, "rowID", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(x.Name, "isEnable", StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        private static bool IsSerializedField(FieldInfo field)
        {
            if (field.IsStatic)
            {
                return false;
            }

            if (field.IsNotSerialized)
            {
                return false;
            }

            if (field.IsPublic)
            {
                return true;
            }

            return field.GetCustomAttribute<SerializeField>() != null;
        }

        private static bool TryGetValue(Dictionary<string, string> row, string key, out string value)
        {
            var actualKey = row.Keys.FirstOrDefault(x => x.Equals(key, StringComparison.OrdinalIgnoreCase));
            if (actualKey == null)
            {
                value = null;
                return false;
            }

            value = row[actualKey];
            return true;
        }
    }
}
#endif
