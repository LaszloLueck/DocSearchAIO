using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace DocSearchAIO.Controllers
{

    public class TypedPartialViewResponse<T>
    {
        public readonly T PartialViewResponseValue;

        public TypedPartialViewResponse(T partialViewResponseValue)
        {
            PartialViewResponseValue = partialViewResponseValue;
        }
    }
    
    public static class PartialViewHelper
    {
        public static ViewDataDictionary GetPartialViewResponseModel<T>(this TypedPartialViewResponse<T> source)
        {
            return new(new EmptyModelMetadataProvider(), new ModelStateDictionary()){Model = source.PartialViewResponseValue};
        }
    }
}