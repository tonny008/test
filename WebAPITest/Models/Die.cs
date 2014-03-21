using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebAPITest.Models
{
    public class Die
    {
        public string PenId { get; set; }
        public string DieSite { get; set; }

        private string dieId;
        public string DieId
        {
            get { return dieId; }
            set
            {
                dieId = value;
                if (!string.IsNullOrEmpty(dieId))
                    WaferId = DieId.Substring(0, DieId.Length - 8);
            }
        }

        public void GetDieId()
        {
            var pool = WebAPITest.Controllers.VppDataController.Pool;
            var conn = pool.GetObject();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"AT EPOCH LATEST	SELECT wafer_id, row_nr, col_nr FROM usage_dev.ELECJ_POC_WAFER where pn_id=:PanId and die_site_nr=:DieSite";
            cmd.Parameters.Add(new Vertica.Data.VerticaClient.VerticaParameter("PanId", PenId));
            cmd.Parameters.Add(new Vertica.Data.VerticaClient.VerticaParameter("DieSite", DieSite));


            using (var reader = cmd.ExecuteReader())
            {
                if (reader.Read())
                {
                    WaferId = reader.GetString(0);
                    Row = reader.GetInt32(1);
                    Column = reader.GetInt32(2);
                    DieId = string.Format("{0}_R{1:00}_C{2:00}", WaferId, Row, Column);
                }
            }
            pool.PutObject(conn);
        }

        public IEnumerable<int[]> GetWaferMap()
        {
            int[][] data = {
                new int[]{0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
                new int[]{0, 0, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0},
                new int[]{0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0},
                new int[]{1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0},
                new int[]{0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0},
                new int[]{0, 0, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0},
                new int[]{0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0}
            };
            if (string.IsNullOrEmpty(WaferId))
            {
                return data;
            }
            var pool = WebAPITest.Controllers.VppDataController.Pool;
            var conn = pool.GetObject();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @" AT EPOCH LATEST SELECT row_nr, col_nr, test_result FROM usage_dev.ELECJ_POC_WAFER a, (  select max(pn_dm) pn_dm, wafer_rc from usage_dev.ELECJ_POC_WAFER where wafer_id=:WaferId group by wafer_rc ) b
 where a.wafer_rc = b.wafer_rc and a.pn_dm=b.pn_dm";
            cmd.Parameters.Add(new Vertica.Data.VerticaClient.VerticaParameter("WaferId", WaferId));

            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    var row = reader.GetInt32(0)-1;
                    var col = reader.GetInt32(1)-1;
                    var pass = reader.GetString(2);
                    if (pass == "P")
                    {
                        data[row][col] = 2;
                    }
                    else
                    {
                        data[row][col] = 3;
                    }
                }
            }
            pool.PutObject(conn);
            return data;
        }

        public string WaferId { get; set; }
        //{
        //    get { return DieId.Substring(0, DieId.Length - 8); }
        //}

        public int Column { get; set; }
        //{
        //    get { return int.Parse(DieId.Substring(DieId.IndexOf("_C") + 2, 2)); }
        //}

        public int Row { get; set; }
        //{
        //    get { return int.Parse(DieId.Substring(DieId.IndexOf("_R") + 2, 2)); }
        //}
    }
}