namespace HttpClient.DataTypes
{
  using System.Collections.Generic;


  public class ResponseMetaData
  {

    #region PROPERTIES

    public string ContentType { get;  set; }

    public int ContentLength { get; set; }

    public ContentTransferType ContentTransferType { get; set; }

    public string ServerResponseCode { get; set; }

    public Dictionary<string, List<string>> Headers { get; set; }

    public IEnumerable<byte[]> ContentData { get; set; }

    #endregion

  }
}
