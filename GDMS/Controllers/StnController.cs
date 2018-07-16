﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web.Http;
using GDMS.Models;
using Newtonsoft.Json;

namespace GDMS.Controllers
{
    [RequestAuthorize]
    public class StnController : ApiController
    {

        //POST对象 (通过POST只能获取1个对象，因此POST多个数据需要使用类)
        public class StnAjax
        {
            public string userId { get; set; }
            public string systemId { get; set; }
            public string siteId { get; set; }
            public int page { get; set; }
            public int limit { get; set; }
            public string keyword { get; set; }
        }
        //返回对象
        private class Response
        {
            public int code { get; set; }
            public string count { get; set; }
            public string msg { get; set; }
            public object data { get; set; }
        }

        //获取设备列表
        [ActionName("list")]
        public HttpResponseMessage StnList([FromBody] StnAjax stnajax)
        {
            Db db = new Db();
            string where = "";
            if (stnajax.systemId != null) { where = where + " AND B.SYSTEM_ID = '" + stnajax.systemId + "'"; }
            if (stnajax.siteId != null) { where = where + " AND A.SITE_ID = '" + stnajax.siteId + "'"; }
            if (stnajax.keyword != null && stnajax.keyword.Length != 0) { where = where + "AND ( A.NAME LIKE '" + stnajax.keyword + "' or A.DETAIL LIKE '" + stnajax.keyword + "' or A.REMARK LIKE '" + stnajax.keyword + "')"; }
            string sqlnp = @"
                SELECT
                A.ID AS STN_ID,
                A.NAME AS STN_NAME,
                A.DETAIL,
                A.REMARK,
                A.STATUS,
                A.USER_ID,
                A.EDIT_DATE,
                B.ID AS SITE_ID,
                B.NAME AS SITE_NAME,
                C.ID AS SYSTEM_ID,
                C.NAME AS SYSTEM_NAME
                FROM
                GDMS_STN_MAIN A
                LEFT JOIN GDMS_SITE B ON A.SITE_ID = B.ID
                LEFT JOIN GDMS_SYSTEM C ON B.SYSTEM_ID = C.ID
                WHERE B.SYSTEM_ID IN (SELECT SYSTEM_ID FROM GDMS_USER_SYSTEM WHERE USER_ID = '" + stnajax.userId + "') " + where;

            int limit1 = (stnajax.page - 1) * stnajax.limit + 1;
            int limit2 = stnajax.page * stnajax.limit;
            string sql = "SELECT * FROM(SELECT p1.*,ROWNUM rn FROM(" + sqlnp + ")p1)WHERE rn BETWEEN " + limit1 + " AND " + limit2;
            var ds = db.QueryT(sql);
            Response res = new Response();
            ArrayList data = new ArrayList();
            foreach (DataRow col in ds.Rows)
            {
                Dictionary<string, string> dict = new Dictionary<string, string>
                {
                    { "STN_ID", col["STN_ID"].ToString() },
                    { "STN_NAME", col["STN_NAME"].ToString() },
                    { "DETAIL", col["DETAIL"].ToString() },
                    { "REMARK", col["REMARK"].ToString() },
                    { "STATUS", col["STATUS"].ToString() },
                    { "USER_ID", col["USER_ID"].ToString() },
                    { "EDIT_DATE", col["EDIT_DATE"].ToString() },
                    { "SITE_NAME", col["SITE_NAME"].ToString() },
                    { "SYSTEM_NAME", col["SYSTEM_NAME"].ToString() },
                };

                data.Add(dict);
            }

            string sql2 = @"
                SELECT
                COUNT(*) AS COUNT
                FROM
                GDMS_STN_MAIN A
                LEFT JOIN GDMS_SITE B ON A.SITE_ID = B.ID
                LEFT JOIN GDMS_SYSTEM C ON B.SYSTEM_ID = C.ID
                WHERE B.SYSTEM_ID IN (SELECT SYSTEM_ID FROM GDMS_USER_SYSTEM WHERE USER_ID = '" + stnajax.userId + "') " + where;
            var ds2 = db.QueryT(sql2);
            foreach (DataRow col in ds2.Rows)
            {
                res.count = col["count"].ToString();
            }

            res.code = 0;
            res.msg = "";
            res.data = data;

            var resJsonStr = JsonConvert.SerializeObject(res);
            HttpResponseMessage resJson = new HttpResponseMessage
            {
                Content = new StringContent(resJsonStr, Encoding.GetEncoding("UTF-8"), "application/json")
            };
            return resJson;
        }

        //获取select
        [ActionName("select")]
        public HttpResponseMessage DeviceSelect([FromBody] StnAjax stnajax)
        {
            Db db = new Db();
            Response res = new Response();
            Dictionary<string, object> data = new Dictionary<string, object>();

            //查询系统select
            string sql1 = @"
                SELECT
                A.SYSTEM_ID AS SYSTEM_ID,
                B.NAME AS SYSTEM_NAME
                FROM
                GDMS_USER_SYSTEM A
                LEFT JOIN GDMS_SYSTEM B ON A.SYSTEM_ID = B.ID
                WHERE A.USER_ID = '" + stnajax.userId + "'";
            var ds1 = db.QueryT(sql1);
            Dictionary<string, string> dict1 = new Dictionary<string, string>();
            foreach (DataRow col in ds1.Rows)
            {
                dict1.Add(col["SYSTEM_ID"].ToString(), col["SYSTEM_NAME"].ToString());
            }
            data.Add("system", dict1);

            //查询地点select
            string sql2 = @"
                SELECT
                A.SYSTEM_ID AS SYSTEM_ID,
                A.ID AS SITE_ID,
                A.NAME AS SITE_NAME
                FROM
                GDMS_SITE A
                LEFT JOIN GDMS_USER_SYSTEM B ON A.SYSTEM_ID = B.SYSTEM_ID
                WHERE B.USER_ID = '" + stnajax.userId + "' ORDER BY A.SYSTEM_ID ASC ";
            var ds2 = db.QueryT(sql2);
            Dictionary<string, object> siteData = new Dictionary<string, object>();
            Dictionary<string, string> dict2 = new Dictionary<string, string>();
            var index = "0";
            foreach (DataRow col in ds2.Rows)
            {
                if (index == "0" || index == col["SYSTEM_ID"].ToString())
                {
                    dict2.Add(col["SITE_ID"].ToString(), col["SITE_NAME"].ToString());
                    index = col["SYSTEM_ID"].ToString();
                }
                else
                {
                    Dictionary<string, string> temp = new Dictionary<string, string>(dict2);
                    siteData.Add(index, temp);
                    dict2.Clear();
                    dict2.Add(col["SITE_ID"].ToString(), col["SITE_NAME"].ToString());
                    index = col["SYSTEM_ID"].ToString();
                }
            }
            siteData.Add(index, dict2);
            data.Add("site", siteData);

            res.code = 0;
            res.msg = "";
            res.data = data;

            var resJsonStr = JsonConvert.SerializeObject(res);
            HttpResponseMessage resJson = new HttpResponseMessage
            {
                Content = new StringContent(resJsonStr, Encoding.GetEncoding("UTF-8"), "application/json")
            };
            return resJson;
        }

        //删除设备
        [ActionName("del")]
        public HttpResponseMessage DeviceDel([FromBody] StnAjax stnajax)
        {
            Db db = new Db();
            string sql = @"";

            var ds = db.QueryT(sql);
            Response res = new Response();

            res.code = 0;
            res.msg = "";
            res.data = null;

            var resJsonStr = JsonConvert.SerializeObject(res);
            HttpResponseMessage resJson = new HttpResponseMessage
            {
                Content = new StringContent(resJsonStr, Encoding.GetEncoding("UTF-8"), "application/json")
            };
            return resJson;
        }


    }
}
