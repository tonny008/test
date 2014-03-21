using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace WebAPITest.Controllers
{
    public class BoxPlotController : ApiController
    {
        // GET api/<controller>
        public IEnumerable<object> Get()
        {
            List<string> twoLots = GetTwoLots();

            List<object> list = new List<object>();
            var pool = WebAPITest.Controllers.VppDataController.Pool;
            var conn = pool.GetObject();
            var cmd = conn.CreateCommand();
            cmd.CommandText = string.Format(@"AT EPOCH LATEST	select wafer_lot, ETESTER_ID, MEASURE_VL from usage_dev.ELECJ_POC_VPP a, usage_dev.ELECJ_POC_WAFER b
where a.war_id=b.wafer_rc  and b.wafer_lot in 
(select wafer_lot from usage_dev.ELECJ_POC_VPP a, usage_dev.ELECJ_POC_WAFER b
where a.war_id=b.wafer_rc group by wafer_lot order by max(a.pn_dm) desc limit 2) 
order by wafer_lot, ETESTER_ID", twoLots.ToArray());

            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    list.Add(new object[] { reader.GetString(0), reader.GetString(1), reader.GetFloat(2) });
                }
            }
            pool.PutObject(conn);
            return list.ToArray();
        }

        // GET api/<controller>/5
        public IEnumerable<object> Get(int id)
        {
            return GetTwoLots();
        }

        static List<string> twoLots;
        private static List<string> GetTwoLots()
        {
            if (twoLots!=null)
            {
                return twoLots;
            }
            List<string> list = new List<string>();
            var pool = WebAPITest.Controllers.VppDataController.Pool;
            var conn = pool.GetObject();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"AT EPOCH LATEST	select distinct wafer_lot from usage_dev.ELECJ_POC_WAFER limit 2";

            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    list.Add(reader.GetString(0));
                }
            }
            pool.PutObject(conn);
            twoLots = list;
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
    }
}