
import com.google.gson.annotations.SerializedName;

public enum ExportTypes {
	@SerializedName("0")
	PDF(0),
	@SerializedName("1")
    CSV(1),
    @SerializedName("2")
    CrystalReport(2),
    @SerializedName("3")
    Excel(3),
    @SerializedName("4")
    ExcelDataOnly(4),    
    @SerializedName("5")
    RichText(5),
    @SerializedName("6")
    TEXT(6),
    @SerializedName("7")
    WordDoc(7);
    
    private final int levelCode;

	ExportTypes(int levelCode) {
		this.levelCode = levelCode;
	}
	
	public static ExportTypes fromInt(int code) {
	    for (ExportTypes tipo : ExportTypes.values()) {
	        if (tipo.levelCode == code) {
	            return tipo;
	        }
	    }
	    throw new IllegalArgumentException("Código desconocido para ExportTypes: " + code);
	}

	
}