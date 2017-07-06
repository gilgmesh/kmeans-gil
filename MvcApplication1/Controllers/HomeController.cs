using System.Web.Mvc;

namespace MvcApplication1.Controllers
{
    public class HomeController : Controller
    {
        public string Index()
        {
            return "Hello from gil's k-means service\n   to see the results of the Google 4000 set, enter the following URL:\n" +
                "      http://kmeans-gil.apphb.com/clustering";
        }
    }
}
