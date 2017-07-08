using System.Web.Mvc;

namespace MvcApplication1.Controllers
{
    public class HomeController : Controller
    {
        public string Index()
        {
            return "Hello from gil's k-means service:<br>" + 
                "<br> &rarr; to see the results of the Google 4000 set, enter the following URL:<br>" +
                "&rarr; &emsp; http://kmeans-gil.apphb.com/clustering <br>" +
                "<br>&rarr; you can control running parameters on said coordiates, like so:" +
                "<br>&rarr; &emsp; http://kmeans-gil.apphb.com/clustering?num_clusters=4&max_iterations=10 <br>";
        }
    }
}
