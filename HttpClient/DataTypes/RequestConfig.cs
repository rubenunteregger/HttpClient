namespace HttpClient.DataTypes
{
  public class RequestConfig
  {

    #region PROPERTIES

    public string Host { get; set; }

    public int Port { get; set; }

    public string Path { get; set; }

    public bool Verbose { get; set; }

    public bool UseSsl { get; set; }

    public string ContentDataOutputFile { get; set; }

    #endregion

  }
}
