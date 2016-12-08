namespace HttpClient.DataTypes
{
  using System.Collections.Generic;


  public class RequestConfig
  {

    #region PROPERTIES

    public string Host { get; set; }

    public int Port { get; set; }

    public string Path { get; set; }

    public List<string> CustomRequestHeaders { get; set; }

    public bool Verbose { get; set; }

    public bool VerboseHeaders { get; set; }

    public bool VerbosePayload { get; set; }

    public bool UseSsl { get; set; }

    public string ContentDataOutputFile { get; set; }

    #endregion

  }
}
