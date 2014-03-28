using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Vertica.Data.VerticaClient;

namespace WebAPITest.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            ViewBag.Title = "Home Page";
            List<SelectListItem> list = GetList(@"AT EPOCH LATEST	SELECT wafer_id FROM usage_dev.ELECJ_POC_WAFER group by wafer_id order by max(pn_dm) desc, wafer_id");
            list.Insert(0, new SelectListItem() { Text = "=Select Wafer=", Value = "" });
            ViewData["WaferList"] = list;
            list = GetList(@"AT EPOCH LATEST	SELECT wafer_lot FROM usage_dev.ELECJ_POC_WAFER group by wafer_lot order by max(pn_dm) desc, wafer_lot");
            list.Insert(0, new SelectListItem() { Text = "=Select Lot=", Value = "" });
            ViewData["LotList"] = list;

            return View();
        }

        private static List<SelectListItem> GetList(string sql)
        {
            List<SelectListItem> list = new List<SelectListItem>();
            var pool = WebAPITest.Controllers.VppDataController.Pool;
            var conn = pool.GetObject();
            var cmd = conn.CreateCommand();
            cmd.CommandText = sql;

            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    list.Add(new SelectListItem() { Text = reader.GetString(0) });
                }
            }
            pool.PutObject(conn);
            return list;
        }
        public ActionResult BoxPlot()
        {
            ViewBag.Title = "box plot Page";

            return View();
        }

        public ActionResult Test()
        {
            ViewBag.Title = "box plot Page";
            VerticaConnection _conn; //= new VerticaConnection("Host=shr2-vrt-dev-vglb1.houston.hp.com;Database=shr1_vrt_dev;User=svc_usage_dev;Password=Usage_dev_2013!;Pooling=true;MinPoolSize=5");
            Stopwatch sw = new Stopwatch();
            var pool = WebAPITest.Controllers.VppDataController.Pool;
            Parallel.For(0, 50, (i) =>
            {
                sw.Restart();
                _conn = pool.GetObject();
                var cmd = _conn.CreateCommand();
                cmd.CommandText = "select * from USAGE_DEV.ELECJ_POC_WAFER limit 100";
                try
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            Debug.WriteLine(reader.ToString());
                        }
                    }
                    pool.PutObject(_conn);
                }
                catch
                {
                    cmd.Connection.Close();
                    cmd.Connection.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            Debug.WriteLine(reader.ToString());
                        }
                    }
                    pool.PutObject(_conn);
                }
                Debug.WriteLine("first: {0}", sw.Elapsed);
            });
            return View();
        }

    }
}
