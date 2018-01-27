using System.Web.Mvc;
using MediatR;

namespace QuickInsight.Web.Api.Controllers
{
    public class HomeController : Controller
    {
        public HomeController(IMediator mediator)
        {

        }

        public ActionResult Index()
        {
            ViewBag.Title = "Home Page";

            return View();
        }
    }
}
