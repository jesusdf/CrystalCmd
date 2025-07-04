import java.io.ByteArrayInputStream;
import java.io.File;
import java.io.IOException;
import java.io.InputStream;
import java.io.OutputStream;
import java.io.PrintWriter;
import java.io.StringWriter;
import java.nio.file.Files;
import java.nio.file.Paths;
import java.nio.file.StandardOpenOption;
import java.sql.SQLException;
import java.util.HashSet;
import org.apache.commons.fileupload.FileItemIterator;
import org.apache.commons.fileupload.FileItemStream;
import org.apache.commons.fileupload.FileUpload;
//import org.apache.commons.fileupload.MultipartStream;
import org.apache.commons.io.IOUtils;

import com.sun.net.httpserver.HttpExchange;
import com.sun.net.httpserver.HttpHandler;
import com.crystaldecisions.sdk.occa.report.lib.ReportSDKException;

public class ServerExport implements HttpHandler {

	HashSet<FileData> files;

	public void handle(HttpExchange t) throws IOException {

		// PROCESS HTTP CONTENT.
		OutputStream os = null;
		try {
			FileItemIterator ii = new FileUpload().getItemIterator(new ExchangeRequestContext(t));
			os = t.getResponseBody();
			files = new HashSet<FileData>();
			while (ii.hasNext()) {
				final FileItemStream is = ii.next();
				final String name = is.getFieldName();

				try (InputStream stream = is.openStream()) {
					final String filename = is.getName();
					FileData file22 = new FileData();
					file22.data = IOUtils.toByteArray(stream);
					file22.fileName = filename;
					file22.name = name;
					files.add(file22);
				}
			}

			if (files.isEmpty()) {
				os.close();
				return;
			}

			File reportPath=null;
			Data reportData=null;
			for (FileData file: files) {
				String name = file.name;
				if(name.equalsIgnoreCase("reportdata")){
					com.google.gson.Gson gson = new com.google.gson.Gson();
					String json = new String(file.data);
					reportData = gson.fromJson(json, Data.class);
				}
				else{
					reportPath = saveReportTemplate(file.data);
				}
			}

			// PROCESS RPT and DATA.

			String templateFilePath = reportPath.getAbsolutePath();
			PdfExporter pdfExport = new PdfExporter();
			ByteArrayInputStream report = pdfExport.exportReportToStream(templateFilePath, reportData);

			Files.delete(Paths.get(templateFilePath));

			t.sendResponseHeaders(200, report.available());

			byte[] byteArray;
			int bytesRead;
			byteArray = new byte[1024];
			while ((bytesRead = report.read(byteArray)) != -1) {
				os.write(byteArray, 0, bytesRead);
			}

		} catch (ReportSDKException e) {
			// TODO Auto-generated catch block
			e.printStackTrace();
			setError(t, e);
		} catch (SQLException e) {
			// TODO Auto-generated catch block
			e.printStackTrace();
			setError(t, e);
		} catch (Exception e) {
			e.printStackTrace();
			setError(t, e);
		}

		os.close();
	}

	private void setError(HttpExchange t, Throwable e) throws IOException {		
		StringWriter sw = new StringWriter();
        PrintWriter pw = new PrintWriter(sw);
        e.printStackTrace(pw);
        String response= "Error. Imposible continue.\nMore info:\n" + sw.toString();		
		t.sendResponseHeaders(500, response.length());
		OutputStream os = t.getResponseBody();
		os.write(response.getBytes());
		os.close();
	}	
		
	private File saveReportTemplate(byte[] reportTemplate) throws IOException {
		File temp = File.createTempFile("temp-crystalcmd-template-", ".tmp");
		// byte[] b = reportTemplate.getBytes(StandardCharsets.UTF_8);
		Files.write(Paths.get(temp.getAbsolutePath()), reportTemplate, StandardOpenOption.WRITE);
		return temp;
	}

	public static class FileData {
		public String name;
		public String fileName;
		public String contentType;
		public byte[] data;
	}

}
