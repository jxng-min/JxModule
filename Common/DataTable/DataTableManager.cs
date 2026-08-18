using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace JxModule.DataTable
{
    public static class DataTableManager
    {
        private static DataTable[] _dataTables;
        private static bool _isLoaded;
        private static Dictionary<Type, List<DataTable>> _dataTableDict;
        
        private static void InitDataTables()
        {
            if (_isLoaded)
            {
                return;
            }

            var configure = Resources.Load<DataTableConfigure>("DataTable Configure");
            if (!configure)
            {
                Debug.LogError("DataTableConfigure is not initialized.");
                return;
            }
            
            _dataTables = configure.DataTables
                .Where(x => x)
                .ToArray();
            
            _dataTableDict = new Dictionary<Type, List<DataTable>>();
            
            foreach (var dataTable in _dataTables)
            {
                var rowName = dataTable.dataTableRowName;
                var key = Type.GetType(rowName);
                
                if (key == null)
                {
                    Debug.LogWarning($"DataTable row type not found: {rowName}");
                    continue;
                }

                if (!_dataTableDict.TryGetValue(key, out var list))
                {
                    list = new List<DataTable>();
                    _dataTableDict.Add(key, list);
                }

                list.Add(dataTable);
            }
            
            _isLoaded = true;
        }

        public static T FindRow<T>(string id) where T : DataTableRowBase
        {
            InitDataTables();
            
            var typeList = GetDerivedTypes(typeof(T));
            T result = null;

            foreach (var type in typeList)
            {
                var dataTables = _dataTableDict[type];
                if (dataTables == null)
                {
                    continue;
                }
                
                foreach (var dataTable in dataTables)
                {
                    result = dataTable.Find<T>(id);
                    if (result)
                    {
                        return result;
                    }
                }
            }

            return result;
        }

        private static List<Type> GetDerivedTypes(Type baseType)
        {
            InitDataTables();
            
            var typeList = _dataTableDict.Keys.ToList();
            var derivedTypes = typeList.FindAll(baseType.IsAssignableFrom);
            return derivedTypes;
        }

        public static DataTable FindTable<T>(string tableName)
        {
            InitDataTables();

            if (!_dataTableDict.TryGetValue(typeof(T), out var tableList))
            {
                Debug.LogWarning($"DataTable type not found: {tableName}");
                return null;
            }

            var table = tableList.Find(x => x.name == tableName);
            if (table == null)
            {
                Debug.LogWarning($"DataTable not found: {tableName}");
            }

            return table;
        }

        public static T FindRow<T>() where T : DataTableRowBase
        {
            InitDataTables();
            
            var typeList = GetDerivedTypes(typeof(T));

            foreach (var type in typeList)
            {
                var dataTables = _dataTableDict[type];
                foreach (var dataTable in dataTables)
                {
                    var rows = dataTable.Get<T>();
                    if (rows.Count > 0)
                    {
                        return rows[0];
                    }
                }
            }

            return null;
        }

        public static T FindRow<T>(Predicate<T> predicate) where T : DataTableRowBase
        {
            InitDataTables();
            
            var typeList = GetDerivedTypes(typeof(T));

            foreach (var type in typeList)
            {
                var dataTables = _dataTableDict[type];
                foreach (var dataTable in dataTables)
                {
                    var row = dataTable.Find<T>(predicate);
                    if (row)
                    {
                        return row;
                    }
                }
            }

            return null;
        }
        
        public static List<T> FindAllRows<T>() where T : DataTableRowBase
        {
            InitDataTables();
            
            var result = new List<T>();
            
            var typeList = GetDerivedTypes(typeof(T));
            foreach (var type in typeList)
            {
                var dataTables = _dataTableDict[type];
                foreach (var dataTable in dataTables)
                {
                    result.AddRange(dataTable.FindAll<T>());
                }
            }

            return result;
        }

        public static List<T> FindAllRows<T>(Predicate<T> predicate) where T : DataTableRowBase
        {
            InitDataTables();
            
            var result = new List<T>();
            
            var typeList = GetDerivedTypes(typeof(T));
            foreach (var type in typeList)
            {
                var dataTables = _dataTableDict[type];
                foreach (var dataTable in dataTables)
                {
                    result.AddRange(dataTable.FindAll<T>(predicate));
                }
            }
            
            return result;
        }
    }
}