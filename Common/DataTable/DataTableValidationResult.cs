#if UNITY_EDITOR
using System.Collections.Generic;

namespace JxModule.DataTable
{
    public class DataTableValidationResult
    {
        public bool IsValid => _errors.Count == 0;
        public IReadOnlyList<string> Errors => _errors;

        private readonly List<string> _errors = new();

        public void AddError(string error)
        {
            _errors.Add(error);
        }
    }
}
#endif