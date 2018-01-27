using System;
using System.Web.Mvc;

namespace QuickInsight.Web.Api.Filter
{
    public class ExceptionFilter : IExceptionFilter
    {
        public void OnException(ExceptionContext filterContext)
        {
            if (!filterContext.ExceptionHandled && filterContext.Exception is NullReferenceException)
            {
                //filterContext.Result = new RedirectResult("customErrorPage.html");
                //filterContext.ExceptionHandled = true;
            }
        }
    }
}