import com.crystaldecisions.sdk.occa.report.application.ISubreportClientDocument;
import com.crystaldecisions.sdk.occa.report.application.OpenReportOptions;
import com.crystaldecisions.sdk.occa.report.application.ReportClientDocument;
import com.crystaldecisions.sdk.occa.report.definition.IReportObject;
import com.crystaldecisions.sdk.occa.report.exportoptions.ReportExportFormat;
import com.crystaldecisions.sdk.occa.report.lib.ReportSDKException;
import com.crystaldecisions.sdk.occa.report.application.ParameterFieldController;

import java.io.ByteArrayInputStream;
import java.io.FileOutputStream;
import java.io.IOException;
import java.sql.ResultSet;
import java.sql.SQLException;
import java.util.Iterator;
import java.util.Map;

public class PdfExporter {
	
	public void exportReportToFile(String reportPath, String outputPath, Data datafile)
			throws ReportSDKException, IOException, SQLException {
		ByteArrayInputStream report = exportReport(reportPath, datafile);
		
		byte[] byteArray;
		int bytesRead;
		
		byteArray = new byte[1024];
		/*
		 * while((bytesRead = byteArrayInputStream.read(byteArray)) != -1) {
		 * response.getOutputStream().write(byteArray, 0, bytesRead); }
		 */
		FileOutputStream fos = new FileOutputStream(outputPath);
		while ((bytesRead = report.read(byteArray)) != -1) {
			fos.write(byteArray, 0, bytesRead);
		}
		fos.close();

	}
	
	public ByteArrayInputStream exportReportToStream(String reportPath, Data datafile)
			throws ReportSDKException, IOException, SQLException {
	
		return exportReport(reportPath, datafile);
	}
	
	
	private ByteArrayInputStream exportReport(String reportPath, Data datafile)
			throws ReportSDKException, IOException, SQLException {

		ReportClientDocument reportClientDocument;
		ByteArrayInputStream byteArrayInputStream;
	

		/*
		 * Instantiate ReportClientDocument and specify the Java Print Engine as the
		 * report processor. Open a rpt file and export to PDF. Stream PDF back to web
		 * browser.
		 */

		reportClientDocument = new ReportClientDocument();

		reportClientDocument.setReportAppServer(ReportClientDocument.inprocConnectionString);

		reportClientDocument.open(reportPath, OpenReportOptions._openAsReadOnly);

		// Object reportSource = reportClientDocument.getReportSource();

		if (datafile != null) {
			for (Map.Entry<String, Object> item : datafile.getParameters().entrySet()) {
				ParameterFieldController parameterFieldController;

				parameterFieldController = reportClientDocument.getDataDefController().getParameterFieldController();
				parameterFieldController.setCurrentValue("", item.getKey(), item.getValue());
				/*
				 * parameterFieldController.setCurrentValue("", "StringParam", "Hello");
				 * parameterFieldController.setCurrentValue("sub", "StringParam",
				 * "Subreport string value"); parameterFieldController.setCurrentValue("",
				 * "BooleanParam", new Boolean(true));
				 * parameterFieldController.setCurrentValue("", "CurrencyParam", new
				 * Double(123.45)); parameterFieldController.setCurrentValue("", "NumberParam",
				 * new Integer(123));
				 * 
				 */
				// rpt.SetParameterValue(item.Key, item.Value);
			}
			for (Map.Entry<String, String> item : datafile.getDataTables().entrySet()) {

				CsharpResultSet inst = new CsharpResultSet();
				ResultSet result = inst.Execute(item.getValue());

				reportClientDocument.getDatabaseController().setDataSource(result, item.getKey(), item.getKey());
			}
			for (Map.Entry<String, String> item : datafile.getSubReportDataTables().entrySet()) {

				CsharpResultSet inst = new CsharpResultSet();
				ResultSet result = inst.Execute(item.getValue());
				String subReportName = item.getKey();
				//set resultSet for sub report
				ISubreportClientDocument subClientDoc = reportClientDocument.getSubreportController().getSubreport(subReportName);
				String subTableAlias = subClientDoc.getDatabaseController().getDatabase().getTables().getTable(0).getAlias();
				subClientDoc.getDatabaseController().setDataSource(result, subTableAlias, subTableAlias);
			}
			for (Iterator<MoveObjects> itr = datafile.getMoveObjectPosition().iterator(); itr.hasNext();) {
				MoveObjects item = itr.next();
				moveReportObject(reportClientDocument, item);
			}
		}

		byteArrayInputStream = (ByteArrayInputStream) reportClientDocument.getPrintOutputController()
				.export(ReportExportFormat.PDF);

		reportClientDocument.close();
		return byteArrayInputStream;
	}

	private void moveReportObject(ReportClientDocument reportClientDocument, MoveObjects item)
			throws ReportSDKException {
		IReportObject control = reportClientDocument.getReportDefController().findObjectByName(item.ObjectName);

		if (item.Pos == MovePosition.LEFT) {
			control.setLeft(item.Move);
		}

		if (item.Type == MoveType.ABSOLUTE) {
			switch (item.Pos) {
			case LEFT:
				control.setLeft(item.Move);
				break;
			case TOP:
				control.setTop(item.Move);
				break;
			}
		} else {
			switch (item.Pos) {
			case LEFT:
				control.setLeft(control.getLeft() + item.Move);
				break;
			case TOP:
				control.setTop(control.getTop() + item.Move);
				break;
			}
		}
	}

}
