using System.Collections.Generic;
using System.Web.Http;

namespace MvcApplication1.Controllers
{
    public class ValuesController : ApiController
    {
        // GET values
        public IEnumerable<string> Get()
        {
            return new string[] { "Abraham", "Itzhak", "Jacob" };
        }

        // GET values/5
        public string Get(int id)
        {
            return "Sarah";
        }

        // POST api/values
        public void Post([FromBody]string value)
        {
        }

        // PUT api/values/5
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE api/values/5
        public void Delete(int id)
        {
        }
    }
}
