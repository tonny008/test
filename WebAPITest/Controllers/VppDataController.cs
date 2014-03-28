using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Vertica.Data.VerticaClient;

namespace WebAPITest.Controllers
{
    public class VppDataController : ApiController
    {
        public class ObjectPool
        {
            private ConcurrentBag<VerticaConnection> _objects;
            private Func<VerticaConnection> _objectGenerator;

            public ObjectPool(Func<VerticaConnection> objectGenerator)
            {
                if (objectGenerator == null) throw new ArgumentNullException("objectGenerator");
                _objects = new ConcurrentBag<VerticaConnection>();
                _objectGenerator = objectGenerator;
            }

            public VerticaConnection GetObject()
            {
                VerticaConnection item;
                if (_objects.TryTake(out item))
                {
                    if (item.State == System.Data.ConnectionState.Broken)
                    {
                        item.Close();
                        item.Open();
                    }
                    if (item.State == System.Data.ConnectionState.Closed)
                    {
                        item.Open();
                    }
                    return item;
                };
                return _objectGenerator();
            }

            public void PutObject(VerticaConnection item)
            {
                if (_objects.Count > 6)
                {
                    item.Dispose();
                }
                else
                {
                    _objects.Add(item);
                }
            }

            public void Dispose()
            {
                if (_objects != null)
                {
                    foreach (VerticaConnection item in _objects)
                    {
                        item.Dispose();
                    }
                    _objects = null;
                }
            }
        }

        static ObjectPool pool;
        public static ObjectPool Pool
        {
            get
            {
                if (pool == null)
                {
                    pool = new ObjectPool(() =>
                {
                    VerticaConnection _conn = new VerticaConnection("Host=shr2-vrt-dev-vglb1.houston.hp.com;Database=shr1_vrt_dev;User=svc_usage_dev;Port=5433;Password=Usage_dev_2013!;Pooling=True;");
                    _conn.Open();
                    return _conn;
                }
                );
                }
                return pool;
            }
        }
        // GET api/<controller>
        public IEnumerable<Series> Get()
        {
            Stopwatch sw = Stopwatch.StartNew();
            //VerticaConnectionStringBuilder builder = new VerticaConnectionStringBuilder();
            //builder.Host = "shr2-vrt-dev-vglb1.houston.hp.com";
            //builder.Database = "shr1_vrt_dev";
            //builder.User = "svc_usage_dev";
            //builder.Port = 5433;
            //builder.Password = "Usage_dev_2013!";

            //string strConn = builder.ToString() + ";Pooling=true;";
            var conn = Pool.GetObject();
            Debug.WriteLine(sw.Elapsed);
            var cmd = conn.CreateCommand();
            cmd.CommandText = "AT EPOCH LATEST	select PN_DM, MEASURE_VL, ETESTER_ID, WAR_ID  from usage_dev.ELECJ_POC_VPP order by PN_DM desc limit 100";
            //cmd.CommandText = "select PN_DM, MEASURE_VL, ETESTER_ID, PN_ID, DIE_SITE_NR from usage_dev.ELECJ_POC_VPP order by PN_DM desc limit 100";
            List<Series> re = new List<Series>();
            using (var reader = cmd.ExecuteReader())
            {
                Dictionary<string, List<object[]>> dict = new Dictionary<string, List<object[]>>();

                System.TimeSpan span = new System.TimeSpan(System.DateTime.Parse("1/1/1970").Ticks);
                long minStamp = long.MaxValue, maxStamp = long.MinValue;
                while (reader.Read())
                {
                    string eTest = reader.GetString(2);
                    if (!dict.ContainsKey(eTest))
                    {
                        dict.Add(eTest, new List<object[]>());
                    }
                    long timestamp = (long)(reader.GetDateTime(0).Subtract(span).Ticks / 10000);
                    if (timestamp < minStamp)
                    {
                        minStamp = timestamp;
                    }
                    if (timestamp > maxStamp)
                    {
                        maxStamp = timestamp;
                    }
                    dict[eTest].Add(new object[] { timestamp, reader[1], reader[3] });
                    //list.Add(new object[] { DateTime.Now.AddMinutes(1), 1.2F });
                    //list.Add(new object[] { DateTime.Now.AddMinutes(2), 1.3F });
                    //list.Add(new object[] { DateTime.Now.AddMinutes(3), 1.5F });

                }
                re.Add(new Series() { color = "red", data = new object[] { new object[] { minStamp, 1 }, new object[] { maxStamp, 1 } } });
                re.Add(new Series() { color = "red", data = new object[] { new object[] { minStamp, -1 }, new object[] { maxStamp, -1 } } });
                foreach (var item in dict.Keys)
                {
                    Series s = new Series();
                    s.label = item;
                    s.data = dict[item].ToArray();
                    re.Add(s);
                }

            }
            Pool.PutObject(conn);
            sw.Stop();
            Debug.WriteLine(sw.Elapsed);
            return re;
        }

        // GET api/<controller>/5
        public IEnumerable<Series> Get(int id)
        {
            var conn = Pool.GetObject();

            var cmd = conn.CreateCommand();
            cmd.CommandText = @"AT EPOCH LATEST	select a.PN_ID, a.ETESTER_ID,  a.WAR_ID, a.MEASURE_VL, a.PN_DM 
FROM usage_dev.ELECJ_POC_VPP A,
(
select ETESTER_ID,PN_ID,WAR_ID,MAX(PN_DM) PN_DM
from usage_dev.ELECJ_POC_VPP 
where PN_ID in 
  (SELECT  PN_ID 
  from usage_dev.ELECJ_POC_VPP 
  where etester_ID='et05' group by pn_id order by max(pn_dm) desc limit 100)
GROUP BY ETESTER_ID,PN_ID,WAR_ID
)B
WHERE  A.ETESTER_ID = B.ETESTER_ID
AND A.WAR_ID = B.WAR_ID
AND A.PN_ID = B.PN_ID
AND A.PN_DM = B.PN_DM
order by a.PN_DM desc";
            //cmd.CommandText = "select PN_DM, MEASURE_VL, ETESTER_ID, PN_ID, DIE_SITE_NR from usage_dev.ELECJ_POC_VPP order by PN_DM desc limit 100";
            List<Series> re = new List<Series>();
            using (var reader = cmd.ExecuteReader())
            {
                Dictionary<string, List<object[]>> dict = new Dictionary<string, List<object[]>>();

                System.TimeSpan span = new System.TimeSpan(System.DateTime.Parse("1/1/1970").Ticks);
                string firstPnId = null, lastPnId = null;
                List<string> pnIdList = new List<string>();
                while (reader.Read())
                {
                    string eTest = reader.GetString(1);
                    if (!dict.ContainsKey(eTest))
                    {
                        dict.Add(eTest, new List<object[]>());
                    }
                    var pnId = reader.GetString(0);
                    if (firstPnId == null) firstPnId = pnId;
                    lastPnId = pnId;
                    int index = pnIdList.IndexOf(pnId);
                    if (index == -1 && eTest == "et05")
                    {
                        index = pnIdList.Count;
                        pnIdList.Add(pnId);
                    }
                    dict[eTest].Add(new object[] { index, reader[3], reader[2], pnId, reader[4] });
                }
                re.Add(new Series() { color = "red", data = new object[] { new object[] { 0, 1 }, new object[] { pnIdList.Count - 1, 1 } } });
                re.Add(new Series() { color = "red", data = new object[] { new object[] { 0, -1 }, new object[] { pnIdList.Count - 1, -1 } } });
                foreach (var item in dict.Keys)
                {
                    if (item != "et05")
                    {
                        var list = dict[item];
                        foreach (var dataItem in list)
                        {
                            dataItem[0] = pnIdList.IndexOf((string)dataItem[3]);
                            if ((int)dataItem[0] == -1)
                            {
                                Debug.WriteLine(dataItem);
                            }
                        }
                    }
                    Series s = new Series();
                    s.label = item;
                    s.data = dict[item].ToArray();
                    re.Add(s);
                }

            }
            Pool.PutObject(conn);
            return re;
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

    public class Series
    {
        public string color { get; set; }
        public string label { get; set; }

        public object[] data { get; set; }
    }
}