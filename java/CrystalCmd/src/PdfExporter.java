import com.crystaldecisions.sdk.occa.report.application.ISubreportClientDocument;
import com.crystaldecisions.sdk.occa.report.application.OpenReportOptions;
import com.crystaldecisions.sdk.occa.report.application.ReportClientDocument;
import com.crystaldecisions.sdk.occa.report.data.*;
import com.crystaldecisions.sdk.occa.report.definition.IReportObject;
import com.crystaldecisions.sdk.occa.report.exportoptions.ReportExportFormat;
import com.crystaldecisions.sdk.occa.report.lib.IStrings;
import com.crystaldecisions.sdk.occa.report.lib.ReportSDKException;
import com.crystaldecisions.sdk.occa.report.application.ParameterFieldController;

import java.io.ByteArrayInputStream;
import java.io.FileOutputStream;
import java.io.IOException;
import java.sql.ResultSet;
import java.sql.SQLException;
import java.text.SimpleDateFormat;
import java.time.LocalDateTime;
import java.time.ZoneOffset;
import java.time.format.DateTimeFormatter;
import java.time.format.DateTimeFormatterBuilder;
import java.util.*;

public class PdfExporter {

    public void exportReportToFile(String reportPath, String outputPath, Data datafile)
            throws ReportSDKException, IOException, SQLException {
        ByteArrayInputStream report = exportReport(reportPath, datafile);

        byte[] byteArray;
        int bytesRead;

        byteArray = new byte[1024];

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

        reportClientDocument = new ReportClientDocument();
        reportClientDocument.setReportAppServer(ReportClientDocument.inprocConnectionString);
        reportClientDocument.open(reportPath, OpenReportOptions._openAsReadOnly);

        // Remove sub report connection strings
        try{
            RemoveSubReportsConnectionStrings(reportClientDocument);
        }
        catch(MissingResourceException mre){
            System.out.println("Sub report failure");
            mre.printStackTrace();
        }
        catch (Exception ex) {
            System.out.println("Sub report failure to remove connection strings.");
            ex.printStackTrace();
        }

        try{
            RemoveMainReportConnectionStrings(reportClientDocument);
        }
        catch(Exception ex) {
            System.out.println("Main report failure to remove connection strings.");
            ex.printStackTrace();
        }

        // Object reportSource = reportClientDocument.getReportSource();

        // "06/24/2020 00:00:00"
        SimpleDateFormat fmt = new SimpleDateFormat("yyyy-MM-dd'T'HH:mm:ss");

        // If not data just abort and return early
        if (datafile == null) {
            byteArrayInputStream = (ByteArrayInputStream) reportClientDocument.getPrintOutputController()
                    .export(ReportExportFormat.PDF);

            reportClientDocument.close();
            return byteArrayInputStream;
        }

        // Set Main Report Result Set
        SetMainReport(datafile, reportClientDocument);

        // Sub Reports
        SetSubReports(datafile, reportClientDocument);

        /*
        var z = reportClientDocument.getDataDefController().getDataDefinition().getParameterFields();
        for (var blah : z) {
            if (blah.getParameterType() == ParameterFieldType.reportParameter) {
                System.out.println("hi " + blah.getName() + " " + blah.getType().toVariantTypeString() + " " + blah.getType());

                String theValue;
                switch (blah.getType().toVariantTypeString().toLowerCase()) {
                    case "string":
                        SetParameterValue(reportClientDocument, fmt, Map.entry(blah.getName(), ""));
                        break;
                    case "boolean":
                    case "i1":
                        SetParameterValue(reportClientDocument, fmt, Map.entry(blah.getName(), false));
                        break;
                    case "number":
                    case "decimal":
                        SetParameterValue(reportClientDocument, fmt, Map.entry(blah.getName(), 0));
                        break;
                    case "date":
                    case "datetime":
                        SetParameterValue(reportClientDocument, fmt, Map.entry(blah.getName(), new Date()));
                        break;
                    default:
                        SetParameterValue(reportClientDocument, fmt, Map.entry(blah.getName(), ""));
                        break;
                }

            }

        }
*/

        for (Map.Entry<String, Object> item : datafile.getParameters().entrySet()) {
            SetParameterValue(reportClientDocument, fmt, item);
        }
        for (MoveObjects item : datafile.getMoveObjectPosition()) {
            moveReportObject(reportClientDocument, item);
        }


        byteArrayInputStream = (ByteArrayInputStream) reportClientDocument.getPrintOutputController()
                .export(ConvertExportFormat(datafile.getExportAs()));

        reportClientDocument.close();
        return byteArrayInputStream;
    }
     
    //  
    private static ReportExportFormat ConvertExportFormat(int tipo) {
    	ExportTypes currentTipo = ExportTypes.fromInt(tipo);
    	
    	switch (currentTipo) {
        case CSV:
            return ReportExportFormat.tabSeparatedText;
        case CrystalReport:
        	return ReportExportFormat.crystalReports;
        case Excel:
        	return ReportExportFormat.MSExcel;
        case ExcelDataOnly:
        	return ReportExportFormat.recordToMSExcel;
        case PDF:
        	return ReportExportFormat.PDF;
        case RichText:
        	return ReportExportFormat.RTF;
        case TEXT:
        	return ReportExportFormat.tabSeparatedText;            
        default:
            throw new IllegalArgumentException("Tipo de exportación no soportado: " + tipo);
    	}    
    }

    private static void RemoveMainReportConnectionStrings(ReportClientDocument reportClientDocument) throws ReportSDKException {
        // Remove main report connection strings
        Connections conns = reportClientDocument.getDatabase().getConnections();
        for (int i = 0; i < conns.size(); i++) {
            try {
                reportClientDocument.getDatabaseController().removeConnection(conns.getConnection(i));
            } catch (Exception ex) {
                System.out.println("Connection " + i + " failure");
                ex.printStackTrace();
            }

        }
    }

    private static void RemoveSubReportsConnectionStrings(ReportClientDocument reportClientDocument) throws ReportSDKException {
        IStrings subNames = reportClientDocument.getSubreportController().querySubreportNames();
        for (String name : subNames) {
            try{
                var subReport = reportClientDocument.getSubreportController().getSubreport(name);

                var subConnInfo = subReport.getDatabaseController().getDatabase().getConnections();
                for (int i = 0; i < subConnInfo.size(); i++) {
                     subReport.getDatabaseController().removeConnection(subConnInfo.getConnection(i));
                }


            }
            catch (Exception ex) {
                System.out.println("Sub report " + name + " failure");
                ex.printStackTrace();
            }

        }
    }

    private static void SetMainReport(Data datafile, ReportClientDocument reportClientDocument) {
        for (Map.Entry<String, String> item : datafile.getDataTables().entrySet()) {

            try{
                CsharpResultSet inst = new CsharpResultSet();
                ResultSet result = inst.Execute(item.getValue());

                reportClientDocument.getDatabaseController().setDataSource(result, item.getKey(), item.getKey());

                // inst.close();
            }
            catch (Exception ex) {
                System.out.println("Main report failure");
                ex.printStackTrace();
            }
        }
    }
    
    
    public static int countSubreports(com.crystaldecisions.sdk.occa.report.application.ReportClientDocument report) {
        try {        	
            int subreportNames = report.getSubreportController().getSubreportNames().size();

            return subreportNames;

        } catch (com.crystaldecisions.sdk.occa.report.lib.ReportSDKException e) {
            System.err.println("Failed to retrieve subreports: " + e.getMessage());
            return -1;
        }
    }
    
    private static void SetSubReports(Data datafile, ReportClientDocument reportClientDocument) throws SQLException, IOException {
    	
    	//
    	//System.out.print("Num subreports: " + countSubreports(reportClientDocument) );
    	
    	//
        if (datafile.getSubReportDataTables() != null) {
            for (SubReports item : datafile.getSubReportDataTables()) {
                try {
                    CsharpResultSet inst = new CsharpResultSet();
                    ResultSet result = inst.Execute(item.DataTable);
                    String subReportName = item.ReportName;
                    String tableName = item.TableName;

                    // Set Sub Report ResultSet
                    ISubreportClientDocument subClientDoc = reportClientDocument.getSubreportController().getSubreport(subReportName);
                    subClientDoc.getDatabaseController().setDataSource(result, tableName, tableName);
                    // inst.close();
                } catch (ReportSDKException rse) {
                    rse.printStackTrace();
                } catch (ArrayIndexOutOfBoundsException aiofbe) {
                    // add logging
                    aiofbe.printStackTrace();
                }
            }
        }
    }

    private void SetParameterValue(ReportClientDocument reportClientDocument, SimpleDateFormat fmt,
                                   Map.Entry<String, Object> item) throws ReportSDKException {
        ParameterFieldController parameterFieldController;


        parameterFieldController = reportClientDocument.getDataDefController().getParameterFieldController();

        String name = item.getKey();
        var parameters = reportClientDocument.getDataDefController().getDataDefinition().getParameterFields();
        for (var field : parameters) {

            try {
                if (field.getParameterType() != ParameterFieldType.reportParameter) {
                    continue;
                }
                if (!field.getName().equalsIgnoreCase(name)) {
                    continue;
                }

                System.out.println("hi " + field.getName() + " " + field.getType().toVariantTypeString() + " " + field.getType());
                System.out.println(name);

                field.getType();

                Object val = item.getValue();
                String theValue = "";
                switch (field.getType().toVariantTypeString().toLowerCase()) {
                    case "string":
                        theValue = val.toString();
                        if(isEmptyString(theValue)){
                            theValue = "";
                        }
                        parameterFieldController.setCurrentValue("", name, theValue);
                        break;
                    case "boolean":
                    case "i1":
                        if (isEmptyString(val.toString())) {
                            theValue = "false";
                        } else {
                            theValue = val.toString();
                        }
                        if (theValue == "0") {
                            theValue = "false";
                        } else if (theValue == "1") {
                            theValue = "true";
                        }

                        boolean toSetValue =  Boolean.parseBoolean(theValue);
                        parameterFieldController.setCurrentValue("", name, toSetValue);
                        break;
                    case "number":
                    case "decimal":
                        if (isEmptyString(val.toString())) {
                            theValue = "0";
                        } else {
                            theValue = val.toString();
                        }

                        try {
                            int toSetValueInt = Integer.parseInt(theValue);
                            parameterFieldController.setCurrentValue("", name, toSetValueInt);
                        } catch (Exception e) {
                            // TODO: should this be a BigDecimal?
                            double toSetDouble =  Double.parseDouble(theValue);
                            parameterFieldController.setCurrentValue("", name, toSetDouble);
                        }
                        break;
                    case "date":
                    case "datetime":
                        Date dateValue;
                        if (isEmptyString(val.toString())){
                            dateValue = new Date();
                        }
                        else{
                           /*
                            DateTimeFormatter formatter = DateTimeFormatter.ofPattern(""
                                    + "[yyyy-MM-dd'T'HH:mm:ss]"
                                    + "[yyyy-MM-dd'T'HH:mm:ss.SSSXXX]"
                                    + "[MM/dd/yyyy HH:mm:ss]"
                                    + "[yyyy/MM/dd HH:mm:ss.SSSSSS]"
                                    + "[yyyy-MM-dd HH:mm:ss[.SSS]]"
                                    + "[ddMMMyyyy:HH:mm:ss.SSS[ Z]]"
                            );
                            */
                            DateTimeFormatter formatter = new DateTimeFormatterBuilder()
                                    .append(DateTimeFormatter.ISO_LOCAL_DATE_TIME)
                                    .toFormatter();
                            dateValue = Date.from(LocalDateTime.parse(val.toString(), formatter).toInstant(ZoneOffset.ofHours(0)));
                        }

                        //parameterFieldController.setCurrentValue("", name, Date..parseDouble(theValue));
                        parameterFieldController.setCurrentValue("", name, dateValue);

                        break;
                    default:
                        theValue = val.toString();
                        if(isEmptyString(theValue)){
                            theValue = "";
                        }
                        parameterFieldController.setCurrentValue("", name, theValue);
                        break;
                }


/*
                String theValue;
                switch (par.ParameterValueType) {
                    case ParameterValueKind.CurrencyParameter:
                        theValue = string.IsNullOrWhiteSpace(val ?.ToString()) ?"0" :val.ToString();
                        rpt.SetParameterValue(name, decimal.Parse(theValue));
                        break;
                    case ParameterValueKind.NumberParameter:


                        break;
                    case ParameterValueKind.DateParameter:
                    case ParameterValueKind.DateTimeParameter:
                    case ParameterValueKind.TimeParameter:

                        break;
                    default:
                        theValue = string.IsNullOrWhiteSpace(val ?.ToString()) ?" " :val.ToString();
                        rpt.SetParameterValue(name, theValue);
                        break;
                }
                */
            } catch (Exception ex) {
                System.out.println("Parameter " + name + " failure");
                ex.printStackTrace();
            }


        }


/*
        String theValue;
        switch (blah.getType().toVariantTypeString().toLowerCase()) {
            case "string":
                SetParameterValue(reportClientDocument, fmt, Map.entry(blah.getName(), ""));
                break;
            case "boolean":
            case "i1":
                SetParameterValue(reportClientDocument, fmt, Map.entry(blah.getName(), false));
                break;
            case "number":
            case "decimal":
                SetParameterValue(reportClientDocument, fmt, Map.entry(blah.getName(), 0));
                break;
            case "date":
            case "datetime":
                SetParameterValue(reportClientDocument, fmt, Map.entry(blah.getName(), new Date()));
                break;
            default:
                SetParameterValue(reportClientDocument, fmt, Map.entry(blah.getName(), ""));
                break;
        }



        Date value = null;
        try {
            value = fmt.parse(item.getValue().toString());
        } catch (ParseException e) {
        }


        try {
            if (value == null) {
                parameterFieldController.setCurrentValue("", item.getKey(), item.getValue());
            } else {
                parameterFieldController.setCurrentValue("", item.getKey(), value);
            }
        } catch (com.crystaldecisions.sdk.occa.report.lib.ReportSDKInvalidParameterFieldCurrentValueException notfoundParameter) {
            // doesn't matter, but should add logging
        }
        */

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

    boolean isEmptyString(String string) {
        return string == null || string.isEmpty();
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
