using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MyAOTFriendlyExtensions;

namespace common
{
    public static class RVSerializationFieldACLHelpers
    {
        public static string RemoveRelevantDenyFields<T>(this T Data, string[] DenyList)
        {
            T _InProgress = Data;
            string _InProgressChain = Data.ToJson();
            string DataType = Data.GetType().ToString().ToLowerInvariant();
            string[] DenyListForAllObjects = DenyList.Where(x => !x.Contains(".")).ToArray();
            string[] DenyListForSpecificObjectsAndRelevantToThisObject = DenyList
                .Where(x => x.ToLowerInvariant().Contains($"{DataType}."))
                .ToArray();

            string[] FullDenyFieldList = DenyListForAllObjects
                .Concat(DenyListForSpecificObjectsAndRelevantToThisObject)
                .ToArray();

            _InProgressChain = _InProgressChain.RemoveFieldFromJsonMultiple(FullDenyFieldList);

            return _InProgressChain;
        }
    }
}
