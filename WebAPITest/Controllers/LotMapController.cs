using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace WebAPITest.Controllers
{
    public class LotMapController : ApiController
    {
        // GET api/<controller>
        public IEnumerable<object> Get([FromUri]string lot)
        {
            int[][] data = {
                new int[]{0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
                new int[]{0, 0, 0, 0, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 0, 0, 0, 0},
                new int[]{0, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 0},
                new int[]{2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 0},
                new int[]{0, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 0},
                new int[]{0, 0, 0, 0, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 0, 0, 0, 0},
                new int[]{0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0}
            };
            if (!string.IsNullOrEmpty(lot))
            {
                var pool = WebAPITest.Controllers.VppDataController.Pool;
                var conn = pool.GetObject();
                var cmd = conn.CreateCommand();
                cmd.CommandText = @"AT EPOCH LATEST	SELECT ROW_NR,COL_NR,COUNT(*) FAIL_CT
FROM USAGE_DEV.ELECJ_POC_WAFER
WHERE WAFER_LOT = :LotId  and Test_result='F'
GROUP BY WAFER_LOT,ROW_NR,COL_NR
ORDER BY WAFER_LOT,ROW_NR,COL_NR

";
                cmd.Parameters.Add(new Vertica.Data.VerticaClient.VerticaParameter("LotId", lot));

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var row = reader.GetInt32(0) - 1;
                        var col = reader.GetInt32(1) - 1;
                        var failCount = reader.GetInt32(2);
                        data[row][col] = failCount+3;
                    }
                }
                pool.PutObject(conn);

            }
            return data;
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