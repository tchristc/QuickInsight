using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace QuickInsight.Web.Api._Features.Metric
{
    public class MetricController : Controller
    {
        // GET: Metric
        public ActionResult Index()
        {
            return new JsonResult(){Data= new { success = true }, JsonRequestBehavior = JsonRequestBehavior.AllowGet};
        }
    }
}