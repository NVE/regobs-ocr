namespace SnowProfileScanner.Models
{
    /**
     * Resultat fra klassifisering av et symbol
     */ 
    public interface ICustomVistionDecodeResult
    {
        string id { get; set; }
        string project { get; set; }
        string iteration { get; set; }
        string created { get; set; }
        IPrediction[] predictions { get; set; }
    }

    public interface IPrediction
    {
        double Probability { get; set; }
        string TagId { get; set; }
        string TagName { get; set; }
    }

}
