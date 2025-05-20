using MyAOTFriendlyExtensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace common
{
    public static class RVSerializationFieldACLHelpers
    {
        public static string RemoveRelevantDenyFields<T>(this T Data, string[] DenyList){
            T _InProgress = Data;
            string _InProgressChain = Data.ToJson();
            string DataType = Data.GetType().ToString().ToLowerInvariant();
            string[] DenyListForAllObjects = DenyList.Where(x => !x.Contains(".")).ToArray();
            string[] DenyListForSpecificObjectsAndRelevantToThisObject = DenyList.Where(x => x.ToLowerInvariant().Contains($"{DataType}.")).ToArray();
            foreach (string DenyField in DenyListForAllObjects.Concat(DenyListForSpecificObjectsAndRelevantToThisObject).ToList()) {
                _InProgressChain = _InProgressChain.RemoveFieldFromJson(DenyField);
            }
            return _InProgressChain;
        }
    }
}
