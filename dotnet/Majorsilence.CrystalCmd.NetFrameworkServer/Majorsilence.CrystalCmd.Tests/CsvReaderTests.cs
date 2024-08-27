﻿using Majorsilence.CrystalCmd.Server.Common;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Majorsilence.CrystalCmd.Tests
{
    [TestFixture]
    public class CsvReaderTests
    {

        private readonly Mock<ILogger> _mockLogger;

        public CsvReaderTests()
        {
            // Set up the mock logger
            _mockLogger = new Mock<ILogger>();

            string workingfolder = WorkingFolder.GetMajorsilenceTempFolder();
            if (!System.IO.Directory.Exists(workingfolder))
            {
                System.IO.Directory.CreateDirectory(workingfolder);
            }

        }

        [Test]
        public void ColumnNamesCanContainPeriods()
        {
            var export = new Majorsilence.CrystalCmd.Server.Common.Exporter(_mockLogger.Object);

            DataTable dt = GetTable();
            var reportData = new Common.Data()
            {
                DataTables = new Dictionary<string, string>(),
                MoveObjectPosition = new List<Common.MoveObjects>(),
                Parameters = new Dictionary<string, object>(),
                ExportAs = Common.ExportTypes.PDF
            };
            reportData.AddData("EMPLOYEE", dt);

            var dtFromCsv = CsvReader.CreateTableEtl(reportData.DataTables.FirstOrDefault().Value);
            Assert.That(dtFromCsv.Columns.Contains("Special.Column"));
        }

        static DataTable GetTable()
        {
            // Here we create a DataTable with four columns.
            DataTable table = new DataTable();
            table.Columns.Add("Special.Column", typeof(string));
            table.Columns.Add("EMPLOYEE_ID", typeof(int));
            table.Columns.Add("LAST_NAME", typeof(string));
            table.Columns.Add("FIRST_NAME", typeof(string));
            table.Columns.Add("BIRTH_DATE", typeof(DateTime));
            table.Columns.Add("TestData", typeof(byte[]));

            // Here we add five DataRows.
            table.Rows.Add("Test column name with period", 25, "Indocin, Hi there", "David", DateTime.Now, System.Text.Encoding.UTF8.GetBytes("Hello world"));
            table.Rows.Add("Test column name with period", 50, "Enebrel", "Sam", DateTime.Now, System.Text.Encoding.UTF8.GetBytes("Hello world"));
            table.Rows.Add("Test column name with period", 10, "Hydralazine", "Christoff", DateTime.Now, System.Text.Encoding.UTF8.GetBytes("Hello world"));
            table.Rows.Add("Test column name with period", 21, "Combivent", "Janet", DateTime.Now, System.Text.Encoding.UTF8.GetBytes("Hello world"));
            table.Rows.Add("Test column name with period", 100, "Dilantin", "Melanie", DateTime.Now, System.Text.Encoding.UTF8.GetBytes("Hello world"));
            table.Rows.Add("Test column name with period", 101, "Hello", "World", DateTime.Now, System.Text.Encoding.UTF8.GetBytes("Hello world"));
            return table;
        }
    }
}
