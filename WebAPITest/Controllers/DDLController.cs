using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace WebAPITest.Controllers
{
    public class DDLController : ApiController
    {
        // GET api/<controller>
        public IEnumerable<string> Get()
        {
            List<string> list = GetList(@"AT EPOCH LATEST	SELECT wafer_id FROM usage_dev.ELECJ_POC_WAFER group by wafer_id order by max(pn_dm) desc, wafer_id");
            return list;
        }

        // GET api/<controller>/5
        public IEnumerable<string> Get(int id)
        {
            var list = GetList(@"AT EPOCH LATEST	SELECT wafer_lot FROM usage_dev.ELECJ_POC_WAFER group by wafer_lot order by max(pn_dm) desc, wafer_lot");
            return list;
        }

        // POST api/<controller>
        public void Post([FromBody]string value)
        {
        }

        // PUT api/<controller>/5
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE api/<controller>/5
        public void Delete(int id)
        {
        }

        private static List<string> GetList(string sql)
        {
            List<string> list = new List<string>();
            var pool = WebAPITest.Controllers.VppDataController.Pool;
            var conn = pool.GetObject();
            var cmd = conn.CreateCommand();
            cmd.CommandText = sql;

            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    list.Add( reader.GetString(0));
                }
            }
            pool.PutObject(conn);
            return list;
        }

    }
}