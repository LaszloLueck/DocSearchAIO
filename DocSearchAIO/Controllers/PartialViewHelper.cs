using DocSearchAIO.Scheduler;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace DocSearchAIO.Controllers
{
    public class TypedPartialViewResponse<T> : GenericSource<T>
    {
        public TypedPartialViewResponse(T partialViewResponseValue) : base(partialViewResponseValue)
        {
        }
    }

    public static class PartialViewHelper
    {
        public static ViewDataDictionary PartialViewResponseModel<T>(this TypedPartialViewResponse<T> source)
        {
            return new(new EmptyModelMetadataProvider(), new ModelStateDictionary())
                {Model = source.Value};
        }
    }
}