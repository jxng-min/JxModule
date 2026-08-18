using System.Collections.Generic;
using UnityEngine;

namespace JxModule.DataTable
{
    [CreateAssetMenu(fileName = "DataTableConfigure", menuName = "JxModule/DataTable/DataTable Configure")]
    public class DataTableConfigure : ScriptableObject
    {
        [SerializeField] private string dataTableDirectory = "Assets/DataTables/Tables";
        [SerializeField] private string csvDirectory = "Assets/DataTables/CSV";
        [SerializeField] private List<DataTable> dataTables = new();
        
        public string DataTableDirectory => dataTableDirectory;
        public string CsvDirectory => csvDirectory;
        public IReadOnlyList<DataTable> DataTables => dataTables;

#if UNITY_EDITOR
        public const string DataTableDirectoryPropertyName = nameof(dataTableDirectory);
        public const string CsvDirectoryPropertyName = nameof(csvDirectory);
        public const string DataTablesPropertyName = nameof(dataTables);
#endif
    }
}