﻿using DbComponent;
using GemBox.Spreadsheet;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Web;


namespace JingWuTong.Handle
{
    /// <summary>
    /// exportAll_Timesharing_Reports 的摘要说明
    /// </summary>
    public class exportAll_Timesharing_Reports : IHttpHandler
    {
        List<dataStruct> tmpList = new List<dataStruct>();

        DataTable allEntitys = null;
        DataTable devtypes = null;
        DataTable dUser = null;
        DataTable zfData = null;
        DataTable Data = null;



        int statusvalue = 0;  //正常参考值
        int zxstatusvalue = 0;//在线参考值

        int sheetrows = 0;
        int dataindex = 0;
        string begintime = "";
        string endtime = "";
        int countTime;

        public void ProcessRequest(HttpContext context)
        {
            context.Response.ContentType = "text/plain";
            string type = context.Request.Form["type"];
            begintime = context.Request.Form["begintime"];
            endtime = context.Request.Form["endtime"];
            string ssdd = context.Request.Form["ssdd"];
            string sszd = context.Request.Form["sszd"];
            string requesttype = context.Request.Form["requesttype"];
            string search = context.Request.Form["search"];
            string sreachcondi = "";

            int onlinevalue = int.Parse(context.Request.Form["onlinevalue"]) * 60;
            int usedvalue = int.Parse(context.Request.Form["usedvalue"]) * 60;


            if (search != "")
            {
                sreachcondi = " (de.[DevId] like '%" + search + "%' or us.[XM] like '%" + search + "%' or us.[JYBH] like '%" + search + "%' ) and ";
            }

            foreach (var key in ConfigurationManager.AppSettings.AllKeys)
            {
                countTime += (key.Contains("Time")) ? 1 : 0;
            }



            allEntitys = SQLHelper.ExecuteRead(CommandType.Text, "SELECT BMDM,SJBM,BMMC,isnull(Sort,0) as Sort,id from [Entity] ", "11");
            devtypes = SQLHelper.ExecuteRead(CommandType.Text, "SELECT TypeName,ID FROM [dbo].[DeviceType] where ID<7  ORDER by Sort ", "11");
            dUser = SQLHelper.ExecuteRead(CommandType.Text, "SELECT en.SJBM,us.BMDM FROM [dbo].[ACL_USER] us left join Device de on de.JYBH = us.JYBH left join Entity en on de.BMDM = en.BMDM where " + sreachcondi + " us.[JYBH] <>''", "user");
            zfData = SQLHelper.ExecuteRead(CommandType.Text, "SELECT VideLength, [FileSize] ,[UploadCnt],[GFUploadCnt],de.BMDM,de.DevId,[Time] FROM [EveryDayInfo_ZFJLY_Hour] al left join Device de on de.DevId = al.DevId  left join ACL_USER as us on de.JYBH = us.JYBH     where " + sreachcondi + "   [Time] >='" + begintime + "' and [Time] <='" + endtime + " 23:59' ", "Alarm_EveryDayInfo");
            Data = SQLHelper.ExecuteRead(CommandType.Text, "SELECT OnlineTime, [HandleCnt] ,[CXCnt],de.BMDM,de.DevId,[Time] FROM [EverydayInfo_Hour] al left join Device de on de.DevId = al.DevId  left join ACL_USER as us on de.JYBH = us.JYBH     where " + sreachcondi + "   [Time] >='" + begintime + "' and [Time] <='" + endtime + " 23:59' ", "Alarm_EveryDayInfo");







            int days = Convert.ToInt16(context.Request.Form["dates"]);



            statusvalue = days * usedvalue;//超过10分钟算使用
            zxstatusvalue = days * onlinevalue;//在线参考值


            ExcelFile excelFile = new ExcelFile();
            var tmpath = "";
            tmpath = HttpContext.Current.Server.MapPath("templet\\0.xls");
            excelFile.LoadXls(tmpath);
            //所有大队

            for (int h = 0; h < devtypes.Rows.Count; h++)
            {

                ExcelWorksheet sheet = excelFile.Worksheets[devtypes.Rows[h]["TypeName"].ToString()];
                sheetrows = 0;
                InsertRowdata(sheet, devtypes.Rows[h]["id"].ToString(), devtypes.Rows[h]["TypeName"].ToString(), "331000000000", "支队", "台州交警局");




            }


            tmpath = HttpContext.Current.Server.MapPath("upload\\" + begintime.Replace("/", "-") + "_" + endtime.Replace("/", "-") + "分时段报表.xls");
            excelFile.SaveXls(tmpath);
            context.Response.Redirect(tmpath);

            //string reTitle = ExportExcel(dtreturns, type, begintime, endtime, ssdd, sszd);

        }

        public CellStyle Titlestyle()
        {
            CellStyle style = new CellStyle();
            //设置水平对齐模式
            style.HorizontalAlignment = HorizontalAlignmentStyle.Center;
            //设置垂直对齐模式
            style.VerticalAlignment = VerticalAlignmentStyle.Center;
            //设置字体
            style.Font.Size = 12 * 20; //PT=20
            style.Font.Weight = ExcelFont.BoldWeight;
            style.FillPattern.SetPattern(FillPatternStyle.Solid,ColorTranslator.FromHtml("#ccffcc"), Color.Empty);
            style.Borders.SetBorders(MultipleBorders.Outside, Color.FromArgb(0, 0, 0), LineStyle.Thin);
            //  style.Font.Color = Color.Blue;
            return style;
        }

        public void InsertTitle(ExcelWorksheet sheet, string type)
        {
            CellRange range;
            CellStyle style;
            int mergedint = 0;
            int h = 0;
            switch (type)
            {
                case "1":
                case "2":
                case "3":
                    mergedint = 1 + countTime * 3;
                    sheet.Rows[sheetrows].Cells["A"].Value = "部门";
                    sheet.Rows[sheetrows].Cells["B"].Value = "配发数";
                    foreach (var key in ConfigurationManager.AppSettings.AllKeys)
                    {
                        if (!key.Contains("Time")) continue;
                        sheet.Rows[sheetrows].Cells[2 + h].Value = "设备使用数量（台）";
                        sheet.Rows[sheetrows].Cells[3 + h].Value = "在线时长总和(小时)";
                        sheet.Rows[sheetrows].Cells[4 + h].Value = "设备使用率";
                        h += 3;
                    }
                
                 
                    range = sheet.Cells.GetSubrangeAbsolute(sheetrows, 0, sheetrows, mergedint);
                    style = new CellStyle();
                    style.Font.Size = 12 * 20; //PT=20
                    style.Borders.SetBorders(MultipleBorders.Outside, Color.FromArgb(0, 0, 0), LineStyle.Thin);
                    range.Style = style;
                    //      range.Style.Borders.SetBorders(MultipleBorders.Outside, Color.FromArgb(0, 0, 0), LineStyle.Thin);
                    break;
                case "4":
                    sheet.Rows[sheetrows].Cells["A"].Value = "序号";
                    sheet.Rows[sheetrows].Cells["B"].Value = "部门";
                    sheet.Rows[sheetrows].Cells["C"].Value = "设备配发数(台)";
                    sheet.Rows[sheetrows].Cells["D"].Value = "警员数";
                    sheet.Rows[sheetrows].Cells["E"].Value = "警务通处罚数";
                    sheet.Rows[sheetrows].Cells["F"].Value = "人均处罚量";
                    sheet.Rows[sheetrows].Cells["G"].Value = "查询量";
                    sheet.Rows[sheetrows].Cells["H"].Value = "设备平均处罚量";
                    sheet.Rows[sheetrows].Cells["I"].Value = "设备平均处罚量排名";
                    sheet.Rows[sheetrows].Cells["J"].Value = "无处罚数的警务通（台）";
                    range = sheet.Cells.GetSubrangeAbsolute(sheetrows, 0, sheetrows, 9);
                    style = new CellStyle();
                    style.Borders.SetBorders(MultipleBorders.Outside, Color.FromArgb(0, 0, 0), LineStyle.Thin);
                    range.Style = style;
                    break;
                case "6":
                    sheet.Rows[sheetrows].Cells["A"].Value = "序号";
                    sheet.Rows[sheetrows].Cells["B"].Value = "部门";
                    sheet.Rows[sheetrows].Cells["C"].Value = "设备配发数(台)";
                    sheet.Rows[sheetrows].Cells["D"].Value = "辅警数";
                    sheet.Rows[sheetrows].Cells["E"].Value = "违停采集（例）";
                    sheet.Rows[sheetrows].Cells["F"].Value = "人均处罚量";
                    sheet.Rows[sheetrows].Cells["G"].Value = "查询量";
                    sheet.Rows[sheetrows].Cells["H"].Value = "设备平均处罚量";
                    sheet.Rows[sheetrows].Cells["I"].Value = "设备平均处罚量排名";
                    sheet.Rows[sheetrows].Cells["J"].Value = "本月无采集违停设备（台）";
                    range = sheet.Cells.GetSubrangeAbsolute(sheetrows, 0, sheetrows, 9);
                    style = new CellStyle();
                    style.Borders.SetBorders(MultipleBorders.Outside, Color.FromArgb(0, 0, 0), LineStyle.Thin);
                    range.Style = style;
                    break;
                case "5":
                    sheet.Rows[sheetrows].Cells["A"].Value = "序号";
                    sheet.Rows[sheetrows].Cells["B"].Value = "部门";
                    sheet.Rows[sheetrows].Cells["C"].Value = "设备配发数(台)";
                    sheet.Rows[sheetrows].Cells["D"].Value = "设备使用数量（台）";
                    sheet.Rows[sheetrows].Cells["E"].Value = "设备未使用数量（台）";
                    sheet.Rows[sheetrows].Cells["F"].Value = "视频时长总和(小时)";
                    sheet.Rows[sheetrows].Cells["G"].Value = "视频大小(GB)";
                    sheet.Rows[sheetrows].Cells["H"].Value = "设备使用率";
                    sheet.Rows[sheetrows].Cells["I"].Value = "使用率排名";
                    range = sheet.Cells.GetSubrangeAbsolute(sheetrows, 0, sheetrows, 8);
                    style = new CellStyle();
                    style.Borders.SetBorders(MultipleBorders.Outside, Color.FromArgb(0, 0, 0), LineStyle.Thin);
                    range.Style = style;
                    break;

            }
            sheetrows += 1;
        }

        public void InsertRowdata(ExcelWorksheet sheet, string type, string typename, string sjbm, string reporttype, string title)
        {
            DataTable dtreturns = new DataTable(); //返回数据表
            int mergedint = 0;
            switch (type)
            {
                case "1":
                case "2":
                case "3":
                    mergedint = 1 + countTime * 3;
                    break;
                case "4":
                case "6":
                    mergedint = 2 + countTime * 5;
                    break;
                case "5":
                    mergedint = 1 + countTime * 6;
                    break;
            }

            for (int h = 0; h < mergedint; h++)
            {
                dtreturns.Columns.Add(h.ToString());
            }

            dataindex = 0;
            string pxstring = "";
            OrderedEnumerableRowCollection<DataRow> rows;
            if (reporttype == "支队")
            {
                rows = from p in allEntitys.AsEnumerable()
                       where (p.Field<string>("SJBM") == sjbm)
                       orderby p.Field<int>("Sort") descending
                       select p;
            }
            else
            {
                rows = from p in allEntitys.AsEnumerable()
                       where (p.Field<string>("SJBM") == sjbm || p.Field<string>("BMDM") == sjbm)
                       orderby p.Field<int>("Sort") descending
                       select p;
            }
            foreach (var entityitem in rows)
            {
                if (type != "5" && entityitem["BMDM"].ToString() == "33100000000x") continue;//如果不是执法记录仪，跳出“局机关”单位
                DataRow dr = dtreturns.NewRow();
                dataindex += 1;
                dr["1"] = dataindex;// entityitem["BMMC"].ToString();  //序号
                dr["2"] = entityitem["BMMC"].ToString();  //部门名称

                var entityids = GetSonID(entityitem["BMDM"].ToString());
                List<string> strList = new List<string>();
                strList.Add(entityitem["BMDM"].ToString());
                if (!(reporttype != "支队" && entityitem["SJBM"].ToString() == "331000000000"))  //非支队报表下的大队单位，只显示本级
                {
                    foreach (entityStruct item in entityids)
                    {
                        strList.Add(item.BMDM);
                    }
                }
                switch (type)
                {
                    case "1":
                    case "2":
                    case "3":
                       
                        break;
                    case "4":
                    case "6":
                       
                       
                        break;
                    case "5":
                       
                        break;

                }



            }
            DataRow drtz = dtreturns.NewRow();
           
            dtreturns.Rows.Add(drtz);
            insertSheet(dtreturns, sheet, type, typename, reporttype, title);
            if (reporttype != "支队") return;
            foreach (var entityitem in rows)
            {
                if (type != "5" && entityitem["BMDM"].ToString() == "33100000000x") continue;//如果不是执法记录仪，跳出“局机关”单位
                InsertRowdata(sheet, type, typename, entityitem["BMDM"].ToString(), "大队", entityitem["BMMC"].ToString());
            }

        }

        public void insertSheet(DataTable dt, ExcelWorksheet sheet, string type, string typename, string reporttype, string title)
        {
            int mergedint = 0;
            switch (type)
            {
                case "1":
                case "2":
                case "3":
                    mergedint = 1+countTime*3;
                    break;
                case "4":
                case "6":
                    mergedint = 2+ countTime*5;
                    break;
                case "5":
                    mergedint = 1 + countTime * 6;
                    break;
            }
            CellRange range = sheet.Rows[sheetrows].Cells.GetSubrangeAbsolute(sheetrows, 0, sheetrows, mergedint);//GetSubrange("A1", "G1");
            range.Value = begintime.Replace("/", "-") + "_" + endtime.Replace("/", "-") + title + typename + "报表";
            range.Merged = true;
            range.Style = Titlestyle();
            sheetrows += 1;
            InsertTitle(sheet, type);//标题添加
            sheet.Rows[0].Cells[0].Style.FillPattern.PatternBackgroundColor = Color.Black;

            for (int h = 0; h < dt.Rows.Count; h++)
            {
                for (int n = 0; n < dt.Columns.Count; n++)
                {
                    sheet.Rows[sheetrows + h].Cells[n].Value = dt.Rows[h][n].ToString();
                    if (dt.Rows[h][n].ToString() != "") sheet.Rows[sheetrows + h].Cells[n].Style.Borders.SetBorders(MultipleBorders.Outside, Color.FromArgb(0, 0, 0), LineStyle.Thin);

                }

            }
            sheetrows += dt.Rows.Count + 1;
        }


        public IEnumerable<entityStruct> GetSonID(string p_id)
        {
            try
            {
                var query = (from p in allEntitys.AsEnumerable()
                             where (p.Field<string>("SJBM") == p_id)
                             select new entityStruct
                             {
                                 BMDM = p.Field<string>("BMDM"),
                                 SJBM = p.Field<string>("SJBM")
                             }).ToList<entityStruct>();
                return query.ToList().Concat(query.ToList().SelectMany(t => GetSonID(t.BMDM)));
            }
            catch (Exception e)
            {
                return null;
            }

        }

        public List<dataStruct> findallchildren(string parentid, DataTable dt)
        {
            var list = (from p in dt.AsEnumerable()
                        where p.Field<string>("ParentID") == parentid
                        select new dataStruct
                        {
                            BMDM = p.Field<string>("BMDM"),
                            ParentID = p.Field<string>("ParentID"),
                            在线时长 = p.Field<int>("在线时长"),
                            文件大小 = p.Field<int>("文件大小"),
                            AlarmType = p.Field<int>("AlarmType"),
                            DevId = p.Field<string>("DevId")
                        }).ToList<dataStruct>();
            if (list.Count != 0)
            {
                tmpList.AddRange(list);
            }
            foreach (dataStruct single in list)
            {
                List<dataStruct> tmpChildren = findallchildren(single.BMDM, dt);

            }
            return tmpList;
        }

        public class entityStruct
        {
            public string BMDM;
            public string SJBM;
        }

        public class dataStruct
        {
            public string BMDM = "BMDM";
            public string ParentID = "ParentID";
            public Int64 在线时长 = 0;
            public Int64 文件大小 = 0;
            public int AlarmType = 0;
            public string DevId = "DevId";
            public int UploadCnt = 0;
            public int GFUploadCnt = 0;
            public int HandleCnt = 0;
            public int CXCnt = 0;
        }



        public bool IsReusable
        {
            get
            {
                return false;
            }
        }
    }
}