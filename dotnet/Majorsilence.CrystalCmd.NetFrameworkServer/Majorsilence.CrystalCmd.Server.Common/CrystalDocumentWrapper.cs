﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.UI.WebControls;
using System.Windows.Documents;
using System.Windows.Navigation;
using System.Xml.Linq;
using ChoETL;
using CrystalDecisions.CrystalReports.Engine;
using CrystalDecisions.Shared;
using Microsoft.Extensions.Logging;

namespace Majorsilence.CrystalCmd.Server.Common
{
    public class CrystalDocumentWrapper
    {
        private readonly ILogger _logger;
        public CrystalDocumentWrapper(ILogger logger)
        {
            _logger = logger;
        }

        public ReportDocument Create(string reportPath, CrystalCmd.Common.Data datafile)
        {
            ReportDocument reportClientDocument=null;
            try
            {
                reportClientDocument = new ReportDocument();

                //reportClientDocument.ReportAppServer = "inproc:jrc";
                reportClientDocument.Load(reportPath);
                ProcessReport(reportClientDocument, datafile);
                return reportClientDocument;
            }
            catch(Exception ex)
            {
                reportClientDocument?.Close();
                reportClientDocument?.Dispose();
               _logger.LogError(ex, "Error while creating report");
                throw;
            }
        }
        private void ProcessReport(ReportDocument reportClientDocument, CrystalCmd.Common.Data datafile) { 

            foreach (var table in datafile.DataTables)
            {
                DataTable dt = CsvReader.CreateTableEtl(table.Value);
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
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while setting data source");
                }
            }

            foreach (var emptyReportName in datafile.EmptyDataTables)
            {
                try
                {
                    var dt = CreateEmptyTableSchema(
                        reportClientDocument
                            .Database
                            .Tables[emptyReportName]);
                    SetDataSource(emptyReportName, dt, reportClientDocument);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while setting empty data source");
                }
            }

            foreach (var table in datafile.SubReportDataTables)
            {
                // fixme: sub report with multiple datatables?
                DataTable dt = CsvReader.CreateTableEtl(table.DataTable);
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
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while setting sub report data source");
                }
            }

            foreach (var emptySubreport in datafile.EmptySubReportDataTables)
            {
                try
                {
                    var dt = CreateEmptyTableSchema(
                        reportClientDocument
                            .Subreports[emptySubreport.ReportName]
                            .Database
                            .Tables[emptySubreport.TableName]);
                    SetSubReport(emptySubreport.ReportName, emptySubreport.TableName, dt, reportClientDocument);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while setting empty sub report data source");
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

            foreach (var subreport in datafile.SubReportParameters)
            {
                try
                {
                    foreach (var parameter in subreport.Parameters)
                    {
                        try
                        {
                            SetSubreportParameterValue(
                                parameter.Key,
                                parameter.Value,
                                reportClientDocument,
                                subreport.ReportName);
                        }
                        catch (Exception ex)
                        {
                            Console.Error.WriteLine(ex);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while setting sub report parameters");
                }
            }

            foreach (var item in datafile.FormulaFieldText)
            {
                SetFormulaText(reportClientDocument, item);
            }

            foreach (var item in datafile.CanGrow)
            {
                reportClientDocument.ReportDefinition.ReportObjects[item.Key].ObjectFormat.EnableCanGrow = item.Value;
            }

            foreach (var item in datafile.Suppress)
            {
                SetSuppress(reportClientDocument, item);
            }

            foreach (var item in datafile.SortByField)
            {
                SetSortOrder(reportClientDocument, item);
            }

            foreach (var item in datafile.Resize)
            {
                SetResize(reportClientDocument, item);
            }

            foreach (var x in datafile.MoveObjectPosition)
            {
                try
                {
                    MoveReportObject(x, reportClientDocument);
                }
                catch (System.IndexOutOfRangeException iore)
                {
                    _logger.LogError(iore, "Error while moving report object");
                }

            }
        }

        private static void SetFormulaText(ReportDocument reportClientDocument, KeyValuePair<string, string> item)
        {
            reportClientDocument.DataDefinition.FormulaFields[item.Key].Text = item.Value;
        }

        private static void SetResize(ReportDocument reportClientDocument, KeyValuePair<string, int> item)
        {
            reportClientDocument.ReportDefinition.ReportObjects[item.Key].Width = item.Value;
        }

        private void SetSuppress(ReportDocument reportClientDocument, KeyValuePair<string, bool> item)
        {
            try
            {
                if (reportClientDocument.ReportDefinition.ReportObjects[item.Key] != null)
                {
                    reportClientDocument.ReportDefinition.ReportObjects[item.Key].ObjectFormat.EnableSuppress = item.Value;
                }
            }
            catch (IndexOutOfRangeException ex)
            {
                _logger.LogError(ex, "Error while setting suppress");
            }
        }

        private static void SetSortOrder(ReportDocument reportClientDocument, KeyValuePair<string, string> item)
        {
            FieldDefinition FieldDef = reportClientDocument.Database.Tables[item.Key].Fields[item.Value];
            reportClientDocument.DataDefinition.SortFields[0].Field = FieldDef;
        }

        private void SetParameterValue(string name, object val, ReportDocument rpt)
        {
            if (rpt.ParameterFields[name] != null)
            {
                SetAnyLayerParameterValue(name, val, rpt);

            }
            else
            {
                _logger.LogWarning("Parameter not found: " + name);
            }
        }

        private void SetSubreportParameterValue(string name, object val, ReportDocument rpt, string subreportName)
        {
            rpt.SetParameterValue(name, val, subreportName);
        }

        private void SetAnyLayerParameterValue(string name, object val, ReportDocument rpt)
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

        private DataTable CreateEmptyTableSchema(
            CrystalDecisions.CrystalReports.Engine.Table table)
        {
            var dt = new DataTable();
            foreach (DatabaseFieldDefinition column in table.Fields)
            {
                var columnName = column.Name;
                var columnType = typeof(object);
                if (Columns.ContainsKey(column.ValueType))
                    columnType = Columns[column.ValueType];
                dt.Columns.Add(columnName, columnType);
            }
            return dt;
        }

        private Dictionary<FieldValueType, Type> Columns = new Dictionary<FieldValueType, Type>()
        {
            { FieldValueType.BooleanField, typeof(bool) },
            { FieldValueType.CurrencyField, typeof(decimal) },
            {FieldValueType.Int16sField, typeof(short) },
            {FieldValueType.Int16uField, typeof(ushort) },
            {FieldValueType.Int32sField, typeof(int) },
            {FieldValueType.Int32uField, typeof(uint) },
            {FieldValueType.Int8sField, typeof(sbyte) },
            {FieldValueType.Int8uField, typeof(byte) },
            {FieldValueType.NumberField, typeof(decimal) },
            {FieldValueType.StringField, typeof(string) },
            {FieldValueType.DateTimeField, typeof(DateTime) },
            {FieldValueType.DateField, typeof(DateTime) },
            {FieldValueType.TimeField, typeof(DateTime) },
            {FieldValueType.BlobField, typeof(byte[]) },
            {FieldValueType.UnknownField, typeof(object) }
        };

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

        private void MoveReportObject(CrystalCmd.Common.MoveObjects item, ReportDocument rpt)
        {
            if (item.Type == CrystalCmd.Common.MoveType.ABSOLUTE)
            {
                switch (item.Pos)
                {
                    case CrystalCmd.Common.MovePosition.LEFT:
                        rpt.ReportDefinition.ReportObjects[item.ObjectName].Left = item.Move;
                        break;
                    case CrystalCmd.Common.MovePosition.TOP:
                        rpt.ReportDefinition.ReportObjects[item.ObjectName].Top = item.Move;
                        break;
                }
            }
            else
            {
                switch (item.Pos)
                {
                    case CrystalCmd.Common.MovePosition.LEFT:
                        rpt.ReportDefinition.ReportObjects[item.ObjectName].Left += item.Move;
                        break;
                    case CrystalCmd.Common.MovePosition.TOP:
                        rpt.ReportDefinition.ReportObjects[item.ObjectName].Top += item.Move;
                        break;
                }
            }
        }
    }
}