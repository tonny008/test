using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace WebAPITest.Controllers
{
    public class PenMapController : ApiController
    {
        // GET api/<controller>
        public IEnumerable<int[]> Get()
        {
            var data = new int[10][];
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = new int[] { 0, 0 };
            }
            var pool = WebAPITest.Controllers.VppDataController.Pool;
            var conn = pool.GetObject();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"AT EPOCH LATEST	SELECT B.DIE_SITE_NR,(case when A.FAIL_CT is null then 0 else a.fail_ct end) as fail_ct ,B.TOTAL_CT
FROM (SELECT DIE_SITE_NR,COUNT(*) FAIL_CT
      FROM USAGE_DEV.ELECJ_POC_WAFER
      WHERE TEST_RESULT = 'F' and die_site_nr is not null
      GROUP BY DIE_SITE_NR
      )A
      right join
      (SELECT DIE_SITE_NR,COUNT(*) TOTAL_CT
      FROM USAGE_DEV.ELECJ_POC_WAFER where die_site_nr is not null
      GROUP BY DIE_SITE_NR 
      )B
      on
A.DIE_SITE_NR = B.DIE_SITE_NR
ORDER BY A.DIE_SITE_NR
";
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    var dieSite = reader.GetString(0);
                    int iDieSite = int.Parse(dieSite.Substring(0, 2));
                    if (iDieSite >=0 && iDieSite<=9)
                    {
                        var failCount = reader.GetInt32(1);
                        var total = reader.GetInt32(2);
                        data[iDieSite][0] = failCount;
                        data[iDieSite][1] = total;
                    }
                }
            }
            pool.PutObject(conn);
            return data;
        }

        // GET api/<controller>/5
        public string Get(int id)
        {
            return "value";
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