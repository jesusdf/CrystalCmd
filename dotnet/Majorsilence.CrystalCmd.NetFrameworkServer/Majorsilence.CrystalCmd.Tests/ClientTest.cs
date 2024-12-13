﻿using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Threading.Tasks;
using Majorsilence.CrystalCmd.Common;
using System.IO;
using System.Net.Http;

namespace Majorsilence.CrystalCmd.Tests
{

    [TestFixture]
    public class ClientTest
    {

        private readonly string exportUrl = "http://localhost:44355/export";
        private readonly string username = "user";
        private readonly string password = "password";

        [Test]
        public async Task Test_ConnectToServerWritePdfAsync()
        {
            DataTable dt = GetTable();
            var reportData = new Common.Data()
            {
                DataTables = new Dictionary<string, string>(),
                MoveObjectPosition = new List<Common.MoveObjects>(),
                Parameters = new Dictionary<string, object>()
            };
            reportData.AddData("EMPLOYEE", dt);

            var list = GetList();
            var reportDataList = new Data()
            {
                DataTables = new Dictionary<string, string>(),
                MoveObjectPosition = new List<MoveObjects>(),
                Parameters = new Dictionary<string, object>()
            };
            reportDataList.AddData("Employee", list);

            await CreatePdfFromReport("the_dotnet_dataset_report.rpt", "the_dataset_report.pdf", reportData);

            await CreatePdfFromReport("the_dotnet_dataset_report.rpt", "the_dataset_report_ienumerable.pdf", reportDataList);

            await CreatePdfFromReport("thereport.rpt", "thereport.pdf", new Data());

            var parameterReportData = new Data();
            parameterReportData.Parameters.Add("MyParameter", "My First Parameter");
            parameterReportData.Parameters.Add("MyParameter2", true);

            await CreatePdfFromReport("thereport_wth_parameters.rpt", "thereport_wth_parameters.pdf", parameterReportData);

            var subreportParameterData = new Data();
            subreportParameterData.SubReportParameters.Add(new SubReportParameters()
            {
                Parameters = parameterReportData.Parameters,
                ReportName = "thereport_wth_parameters.rpt"
            });
            await CreatePdfFromReport("thereport_with_subreport_with_parameters.rpt", "thereport_with_subreport_with_parameters.pdf", subreportParameterData);

            var subreportDatatableData = new Data();
            subreportDatatableData.AddData("the_dotnet_dataset_report.rpt", "Employee", dt);
            await CreatePdfFromReport("thereport_with_subreport_with_dotnet_dataset.rpt", "thereport_with_subreport_with_dotnet_dataset.pdf", subreportDatatableData);


            var fullData = new Data();
            fullData.AddData("EMPLOYEE", dt);
            fullData.AddData("the_dotnet_dataset_report_with_params", "Employee", dt);
            fullData.Parameters = parameterReportData.Parameters;
            fullData.SubReportParameters.Add(new SubReportParameters()
            {
                Parameters = parameterReportData.Parameters,
                ReportName = "the_dotnet_dataset_report_with_params"
            });
            await CreatePdfFromReport("the_dotnet_dataset_report_with_params_and_subreport.rpt", "the_dotnet_dataset_report_with_params_and_subreport.pdf", fullData);

            var emptySubreportData = new Data();
            emptySubreportData.SetEmptyTable("EMPLOYEE");
            emptySubreportData.SetEmptyTable("the_dotnet_dataset_report_with_params", "Employee");
            emptySubreportData.Parameters = parameterReportData.Parameters;
            emptySubreportData.SubReportParameters.Add(new SubReportParameters()
            {
                Parameters = parameterReportData.Parameters,
                ReportName = "the_dotnet_dataset_report_with_params"
            });
            await CreatePdfFromReport("the_dotnet_dataset_report_with_params_and_subreport.rpt", "report_with_empty_subreport_datatable.pdf", emptySubreportData);

            Assert.That(System.IO.File.Exists("report_with_empty_subreport_datatable.pdf"));
        }


        [Test]
        public async Task Test_ClientBackwardsCompat()
        {
            DataTable dt = GetTable();
            var reportData = new Client.Data()
            {
                DataTables = new Dictionary<string, string>(),
                MoveObjectPosition = new List<Common.MoveObjects>(),
                Parameters = new Dictionary<string, object>()
            };
            reportData.AddData("EMPLOYEE", dt);

            var list = GetList();
            var reportDataList = new Client.Data()
            {
                DataTables = new Dictionary<string, string>(),
                MoveObjectPosition = new List<MoveObjects>(),
                Parameters = new Dictionary<string, object>()
            };
            reportDataList.AddData("Employee", list);

            await CreatePdfFromReport("the_dotnet_dataset_report.rpt", "the_dataset_report.pdf", reportData);

            await CreatePdfFromReport("the_dotnet_dataset_report.rpt", "the_dataset_report_ienumerable.pdf", reportDataList);

            await CreatePdfFromReport("thereport.rpt", "thereport.pdf", new Data());

            var parameterReportData = new Client.Data();
            parameterReportData.Parameters.Add("MyParameter", "My First Parameter");
            parameterReportData.Parameters.Add("MyParameter2", true);

            await CreatePdfFromReport("thereport_wth_parameters.rpt", "thereport_wth_parameters.pdf", parameterReportData);

            var subreportParameterData = new Client.Data();
            subreportParameterData.SubReportParameters.Add(new SubReportParameters()
            {
                Parameters = parameterReportData.Parameters,
                ReportName = "thereport_wth_parameters.rpt"
            });
            await CreatePdfFromReport("thereport_with_subreport_with_parameters.rpt", "thereport_with_subreport_with_parameters.pdf", subreportParameterData);

            var subreportDatatableData = new Client.Data();
            subreportDatatableData.AddData("the_dotnet_dataset_report.rpt", "Employee", dt);
            await CreatePdfFromReport("thereport_with_subreport_with_dotnet_dataset.rpt", "thereport_with_subreport_with_dotnet_dataset.pdf", subreportDatatableData);


            var fullData = new Client.Data();
            fullData.AddData("EMPLOYEE", dt);
            fullData.AddData("the_dotnet_dataset_report_with_params", "Employee", dt);
            fullData.Parameters = parameterReportData.Parameters;
            fullData.SubReportParameters.Add(new SubReportParameters()
            {
                Parameters = parameterReportData.Parameters,
                ReportName = "the_dotnet_dataset_report_with_params"
            });
            await CreatePdfFromReport("the_dotnet_dataset_report_with_params_and_subreport.rpt", "the_dotnet_dataset_report_with_params_and_subreport.pdf", fullData);

            var emptySubreportData = new Client.Data();
            emptySubreportData.SetEmptyTable("EMPLOYEE");
            emptySubreportData.SetEmptyTable("the_dotnet_dataset_report_with_params", "Employee");
            emptySubreportData.Parameters = parameterReportData.Parameters;
            emptySubreportData.SubReportParameters.Add(new SubReportParameters()
            {
                Parameters = parameterReportData.Parameters,
                ReportName = "the_dotnet_dataset_report_with_params"
            });
            await CreatePdfFromReport("the_dotnet_dataset_report_with_params_and_subreport.rpt", "report_with_empty_subreport_datatable.pdf", emptySubreportData);

            Assert.That(System.IO.File.Exists("report_with_empty_subreport_datatable.pdf"));
        }

        [Test]
        public async Task Test_ServerCompressedStream()
        {
            DataTable dt = GetTable();
            var reportData = new Common.Data()
            {
                DataTables = new Dictionary<string, string>(),
                MoveObjectPosition = new List<Common.MoveObjects>(),
                Parameters = new Dictionary<string, object>()
            };
            reportData.AddData("EMPLOYEE", dt);

            var list = GetList();
            var reportDataList = new Data()
            {
                DataTables = new Dictionary<string, string>(),
                MoveObjectPosition = new List<MoveObjects>(),
                Parameters = new Dictionary<string, object>()
            };
            reportDataList.AddData("Employee", list);

            await CreatePdfFromReportCompressedStream("the_dotnet_dataset_report.rpt", "the_dataset_report.pdf", reportData);

            Assert.That(System.IO.File.Exists("the_dataset_report.pdf"));
        }
        private async Task CreatePdfFromReport(string reportPath, string pdfOutputPath, Data reportData)
        {
            Console.WriteLine($"Creating pdf {pdfOutputPath} from report {reportPath}");

            using (var fstream = new FileStream(reportPath, FileMode.Open))
            using (var fstreamOut = new FileStream(pdfOutputPath, FileMode.OpenOrCreate | FileMode.Append))
            {
                var rpt = new Client.Report(this.exportUrl, username: this.username, password: this.password);
                using (var stream = await rpt.GenerateAsync(reportData, fstream))
                {
                    await stream.CopyToAsync(fstreamOut);
                }
            }
        }
        private async Task CreatePdfFromReportCompressedStream(string reportPath, string pdfOutputPath, Data reportData)
        {
            Console.WriteLine($"Creating pdf {pdfOutputPath} from report {reportPath} with a gzipped compressed stream");
            using (var httpClient = new HttpClient())
            using (var fstream = new FileStream(reportPath, FileMode.Open))
            using (var fstreamOut = new FileStream(pdfOutputPath, FileMode.OpenOrCreate | FileMode.Append))
            {
                httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.110 Safari/537.3");
                var rpt = new Client.Report(this.exportUrl, username: this.username, password: this.password);
                using (var stream = await rpt.GenerateViaCompressedPostAsync(reportData, fstream, httpClient))
                {
                    await stream.CopyToAsync(fstreamOut);
                }
            }
        }

        static DataTable GetTable()
        {
            // Here we create a DataTable with four columns.
            DataTable table = new DataTable();
            table.Columns.Add("EMPLOYEE_ID", typeof(int));
            table.Columns.Add("LAST_NAME", typeof(string));
            table.Columns.Add("FIRST_NAME", typeof(string));
            table.Columns.Add("BIRTH_DATE", typeof(DateTime));
            table.Columns.Add("TestData", typeof(byte[]));

            // Here we add five DataRows.
            table.Rows.Add(25, "Indocin, Hi there", "David", DateTime.Now, System.Text.Encoding.UTF8.GetBytes("Hello world"));
            table.Rows.Add(50, "Enebrel", "Sam", DateTime.Now, System.Text.Encoding.UTF8.GetBytes("Hello world"));
            table.Rows.Add(10, "Hydralazine", "Christoff", DateTime.Now, System.Text.Encoding.UTF8.GetBytes("Hello world"));
            table.Rows.Add(21, "Combivent", "Janet", DateTime.Now, System.Text.Encoding.UTF8.GetBytes("Hello world"));
            table.Rows.Add(100, "Dilantin", "Melanie", DateTime.Now, System.Text.Encoding.UTF8.GetBytes("Hello world"));
            table.Rows.Add(101, "Hello", "World", DateTime.Now, System.Text.Encoding.UTF8.GetBytes("Hello world"));
            return table;
        }

        static IEnumerable<Employee> GetList()
        {
            // Here we create a DataTable with four columns.
            var list = new List<Employee>();

            // Here we add five DataRows.
            list.Add(new Employee()
            {
                BIRTH_DATE = DateTime.Now,
                EMPLOYEE_ID = 25,
                FIRST_NAME = "David",
                LAST_NAME = "Indocin",
                TestData = System.Text.Encoding.UTF8.GetBytes("Hello world")
            });
            list.Add(new Employee()
            {
                BIRTH_DATE = DateTime.Now,
                EMPLOYEE_ID = 50,
                FIRST_NAME = "Sam",
                LAST_NAME = "Enebrel",
                TestData = System.Text.Encoding.UTF8.GetBytes("Hello world")
            });
            list.Add(new Employee()
            {
                BIRTH_DATE = DateTime.Now,
                EMPLOYEE_ID = 10,
                FIRST_NAME = "Christoff",
                LAST_NAME = "Hydralazine",
                TestData = System.Text.Encoding.UTF8.GetBytes("Hello world")
            });
            list.Add(new Employee()
            {
                BIRTH_DATE = DateTime.Now,
                EMPLOYEE_ID = 21,
                FIRST_NAME = "Janet",
                LAST_NAME = "Combivent",
                TestData = System.Text.Encoding.UTF8.GetBytes("Hello world")
            });
            list.Add(new Employee()
            {
                BIRTH_DATE = DateTime.Now,
                EMPLOYEE_ID = 100,
                FIRST_NAME = "Melanie",
                LAST_NAME = "Dilantin",
                TestData = System.Text.Encoding.UTF8.GetBytes("Hello world")
            });
            list.Add(new Employee()
            {
                BIRTH_DATE = DateTime.Now,
                EMPLOYEE_ID = 101,
                FIRST_NAME = "Hello",
                LAST_NAME = "World",
                TestData = System.Text.Encoding.UTF8.GetBytes("Hello world")
            });
            list.Add(new Employee()
            {
                BIRTH_DATE = DateTime.Now,
                EMPLOYEE_ID = 102,
                FIRST_NAME = "IEnumerable",
                LAST_NAME = "List",
                TestData = System.Text.Encoding.UTF8.GetBytes("Hello world")
            });

            return list;
        }

    }
}
