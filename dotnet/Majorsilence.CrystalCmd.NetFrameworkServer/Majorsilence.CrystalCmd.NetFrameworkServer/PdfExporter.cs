﻿using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using ChoETL;
using CrystalDecisions.CrystalReports.Engine;
using CrystalDecisions.Shared;
using Majorsilence.CrystalCmd.Client;

namespace Majorsilence.CrystalCmd.NetFrameworkServer
{
    public class PdfExporter
    {

        public byte[] exportReportToStream(string reportPath, Client.Data datafile)
        {
            using (var reportClientDocument = new ReportDocument())
            {
                //reportClientDocument.ReportAppServer = "inproc:jrc";
                reportClientDocument.Load(reportPath);


                foreach (var table in datafile.DataTables)
                {
                    DataTable dt = CreateTableEtl(table.Value);
                    try
                    {
                        int idx = 0;
                        bool converted = int.TryParse(table.Key, out idx);
                        if (converted)
                        {
                            SetDataSource(idx, dt, reportClientDocument);
                        }
                        else
                        {
                            SetDataSource(table.Key, dt, reportClientDocument);
                        }
                    }
                    catch (Exception)
                    {
                        // some report data sources are optional
                        // TODO: logging
                    }
                }

                foreach (var table in datafile.SubReportDataTables)
                {
                    // fixme: sub report with multiple datatables?
                    DataTable dt = CreateTableEtl(table.DataTable);
                    try
                    {
                        int idx = 0;
                        bool converted = int.TryParse(table.TableName, out idx);
                        if (converted)
                        {
                            SetSubReport(table.ReportName, idx, dt, reportClientDocument);
                        }
                        else
                        {
                            SetSubReport(table.ReportName, table.TableName, dt, reportClientDocument);
                        }

                    }
                    catch (Exception)
                    {
                        // some sub reports are optional
                        // TODO: logging
                    }
                }



                foreach (var param in datafile.Parameters)
                {
                    SetParameterValue(param.Key, param.Value, reportClientDocument);
                }

                foreach (ParameterField x in reportClientDocument.ParameterFields)
                {
                    if (x.HasCurrentValue == false && x.ReportParameterType == ParameterType.ReportParameter)
                    {
                        // to get things up and running, add defaults for missing parameters

                        SetParameterValue(x.Name, "", reportClientDocument);
                    }
                }

                foreach (var x in datafile.MoveObjectPosition)
                {
                    try
                    {
                        MoveReportObject(x, reportClientDocument);
                    }
                    catch (System.IndexOutOfRangeException)
                    {
                        // TODO: Add logging about bad move objects
                    }

                }


                return ExportPDF(reportClientDocument);
            }
        }


        private void SetParameterValue(string name, object val, ReportDocument rpt)
        {
            if (rpt.ParameterFields[name] != null)
            {
                var par = rpt.ParameterFields[name];
                string theValue;
                switch (par.ParameterValueType)
                {
                    case ParameterValueKind.BooleanParameter:
                        theValue = string.IsNullOrWhiteSpace(val?.ToString()) ? "false" : val.ToString();
                        if (theValue == "0")
                        {
                            theValue = "false";
                        }
                        else if (theValue == "1")
                        {
                            theValue = "true";
                        }
                        rpt.SetParameterValue(name, bool.Parse(theValue));
                        break;
                    case ParameterValueKind.CurrencyParameter:
                        theValue = string.IsNullOrWhiteSpace(val?.ToString()) ? "0" : val.ToString();
                        rpt.SetParameterValue(name, decimal.Parse(theValue));
                        break;
                    case ParameterValueKind.NumberParameter:
                        theValue = string.IsNullOrWhiteSpace(val?.ToString()) ? "0" : val.ToString();
                        try
                        {
                            rpt.SetParameterValue(name, int.Parse(theValue));
                        }
                        catch (Exception)
                        {
                            rpt.SetParameterValue(name, decimal.Parse(theValue));
                        }

                        break;
                    case ParameterValueKind.DateParameter:
                    case ParameterValueKind.DateTimeParameter:
                    case ParameterValueKind.TimeParameter:
                        theValue = string.IsNullOrWhiteSpace(val?.ToString()) ? DateTime.Now.ToLongDateString() : val.ToString();
                        rpt.SetParameterValue(name, DateTime.Parse(theValue));
                        break;
                    default:
                        theValue = string.IsNullOrWhiteSpace(val?.ToString()) ? " " : val.ToString();
                        rpt.SetParameterValue(name, theValue);
                        break;
                }

            }
            else
            {
                Console.WriteLine(name);
            }
        }

        private void SetDataSource(string tableName, DataTable val, ReportDocument rpt)
        {
            rpt.Database.Tables[tableName].SetDataSource(val);
        }

        private void SetDataSource(int idx, DataTable val, ReportDocument rpt)
        {
            rpt.Database.Tables[idx].SetDataSource(val);
        }

        private void SetSubReport(string rptName, string reportTableName, DataTable dataSource, ReportDocument rpt)
        {
            if (string.IsNullOrWhiteSpace(reportTableName))
            {
                rpt.Subreports[rptName].SetDataSource(dataSource);
            }
            else
            {
                rpt.Subreports[rptName].Database.Tables[reportTableName].SetDataSource(dataSource);
            }

        }

        private void SetSubReport(string rptName, int idx, DataTable dataSource, ReportDocument rpt)
        {
            rpt.Subreports[rptName].Database.Tables[idx].SetDataSource(dataSource);
        }

        private byte[] ExportPDF(ReportDocument rpt)
        {

            string fileName = System.IO.Path.GetTempFileName();
            CrystalDecisions.Shared.ExportFormatType exp = ExportFormatType.PortableDocFormat;
            rpt.ExportToDisk(exp, fileName);

            byte[] myData;
            using (var fs = new FileStream(fileName, FileMode.Open, FileAccess.Read))
            {
                myData = new byte[Convert.ToInt32(fs.Length - 1) + 1];
                fs.Read(myData, 0, Convert.ToInt32(fs.Length));
                fs.Close();
            }

            try
            {
                System.IO.File.Delete(fileName);
            }
            catch (Exception)
            {
                // fixme: data cleanup
            }

            return myData;
        }


        private DataTable CreateTableEtl(string csv)
        {
            string[] headers = null;
            string[] columntypes = null;
            DataTable dt = new DataTable();
            using (var reader = ChoCSVReader.LoadText(ConvertToWindowsEOL(csv), new ChoCSVRecordConfiguration()
            {
                MaxLineSize = int.MaxValue / 5,

            }).WithFirstLineHeader().QuoteAllFields())
            {
                reader.Configuration.MayContainEOLInData = true;
                int rowIdx = 0;
                ChoDynamicObject e;

                while ((e = reader.Read()) != null)
                {
                    if (rowIdx == 0)
                    {
                        headers = e.Keys.ToArray();
                        columntypes = e.Values.Select(p => p.ToString()).ToArray();
                        rowIdx = rowIdx + 1;

                        for (int i = 0; i < headers.Length; i++)
                        {
                            dt.Columns.Add(headers[i], Type.GetType($"System.{columntypes[i]}", false, true));
                        }
                        continue;
                    }


                    DataRow dr = dt.NewRow();

                    var columns = e.Values.ToList();
                    for (int i = 0; i < headers.Length; i++)
                    {
                        var cleaned = columns[i]?.ToString();
                        if (string.Equals(columntypes[i], "string", StringComparison.InvariantCultureIgnoreCase) && string.IsNullOrWhiteSpace(cleaned))
                        {
                            dr[i] = "";
                        }
                        else if (string.IsNullOrWhiteSpace(cleaned))
                        {
                            dr[i] = DBNull.Value;
                        }
                        else if (string.Equals(columntypes[i], "byte[]", StringComparison.InvariantCultureIgnoreCase))
                        {
                            dr[i] = HexStringToByteArray(cleaned);
                        }
                        else
                        {

                            dr[i] = cleaned;
                        }
                    }

                    dt.Rows.Add(dr);
                }

            }


            return dt;
        }

        private static byte[] HexStringToByteArray(string cleaned)
        {
            String[] arr = cleaned.Split('-');
            byte[] array = new byte[arr.Length];
            for (int i = 0; i < arr.Length; i++)
            {
                array[i] = Convert.ToByte(arr[i], 16);
            }
            return array;
        }

        private void MoveReportObject(MoveObjects item, ReportDocument rpt)
        {
            if (item.Type == MoveType.ABSOLUTE)
            {
                switch (item.Pos)
                {
                    case MovePosition.LEFT:
                        rpt.ReportDefinition.ReportObjects[item.ObjectName].Left = item.Move;
                        break;
                    case MovePosition.TOP:
                        rpt.ReportDefinition.ReportObjects[item.ObjectName].Top = item.Move;
                        break;
                }
            }
            else
            {
                switch (item.Pos)
                {
                    case MovePosition.LEFT:
                        rpt.ReportDefinition.ReportObjects[item.ObjectName].Left += item.Move;
                        break;
                    case MovePosition.TOP:
                        rpt.ReportDefinition.ReportObjects[item.ObjectName].Top += item.Move;
                        break;
                }
            }
        }

        private static string ConvertToWindowsEOL(string readData)
        {
            // see https://stackoverflow.com/questions/31053/regex-c-replace-n-with-r-n for regex explanation
            readData = Regex.Replace(readData, "(?<!\r)\n", "\r\n");
            return readData;
        }

    }



}