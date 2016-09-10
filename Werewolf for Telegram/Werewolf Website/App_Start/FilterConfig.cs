using System.Web;
using System.Web.Mvc;

namespace Werewolf_Website
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new ErrorHandler.AiHandleErrorAttribute());
            filters.Add(new System.Web.Mvc.AuthorizeAttribute());
            //filters.Add(new RequireHttpsAttribute());
        }
    }
}
