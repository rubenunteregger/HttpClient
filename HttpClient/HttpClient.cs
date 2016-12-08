namespace HttpClient
{
  using global::HttpClient.DataTypes;
  using System;
  using System.Collections;
  using System.Collections.Generic;
  using System.IO;
  using System.Net.Security;
  using System.Net.Sockets;
  using System.Security.Cryptography.X509Certificates;


  public class HttpClient
  {

    #region MEMBERS
    
    private Hashtable certificateErrors;
    private Stream theStream;
    private SslStream tmpSslStream;
    private TcpClient tcpClient;
    private StreamHandler streamHandler;
    private ResponseMetaData responseMetaData;
    private RequestConfig requestConfig;

    #endregion


    #region PUBLIC

    public HttpClient(bool useSsl)
    {
      this.certificateErrors = new Hashtable();
      this.responseMetaData = new ResponseMetaData();
    }


    public void SendGetRequest(RequestConfig requestConfig)
    {
      string headers = string.Empty;
      this.requestConfig = requestConfig;
      this.tcpClient = new TcpClient(this.requestConfig.Host, this.requestConfig.Port);
      tcpClient.NoDelay = true;

      if (this.requestConfig.UseSsl)
      { 
        tmpSslStream = new SslStream(tcpClient.GetStream(), false, new RemoteCertificateValidationCallback(this.AcceptAnyServerCertificate), null);
        tmpSslStream.AuthenticateAsClient(this.requestConfig.Host);
        this.theStream = tmpSslStream;
      }
      else
      {
        this.theStream = tcpClient.GetStream();
      }

      this.streamHandler = new StreamHandler(theStream, this.requestConfig.Host, this.requestConfig.Port, this.requestConfig.Path);
      string Headers = string.Join(Environment.NewLine, this.requestConfig.CustomRequestHeaders);
      string request = string.Format("GET {0} HTTP/1.1\r\n{1}\r\n\r\n", this.requestConfig.Path, Headers);
      this.streamHandler.SendRequestToServer(request);
    }


    public ResponseMetaData GetServerResponse()
    {
      byte[] buffer = new byte[2048];

      // Fetch server response line
      this.responseMetaData.ServerResponseCode = this.streamHandler.ReadLine(false);
      if (string.IsNullOrEmpty(this.responseMetaData.ServerResponseCode) || this.responseMetaData.ServerResponseCode.ToLower().StartsWith("http/1.") == false)
      {
        throw new Exception("The server status line is invalid");
      }

      // Fetch server response headers
      this.ReceiveClientRequestHeaders();

      // Fetch response content data
      this.responseMetaData.ContentLength = this.DetermineContentLength();
      this.responseMetaData.ContentTransferType = this.DetermineContentTransferType();
      this.responseMetaData.ContentType = this.DetermineContentType();


      if (this.responseMetaData.ContentTransferType == ContentTransferType.FixedDataLength)
      {
        this.responseMetaData.ContentData = this.streamHandler.ReceiveResponse(this.responseMetaData.ContentLength);
      }
      else if (this.responseMetaData.ContentTransferType == ContentTransferType.Chunked)
      {
        this.responseMetaData.ContentData = this.streamHandler.ReceiveAllChunks();
      }
      else if (this.responseMetaData.ContentTransferType == ContentTransferType.UnknownStreamLength)
      {
        this.responseMetaData.ContentData = this.streamHandler.ReadPacketFromStream();
      }

      return this.responseMetaData;
    }

    #endregion


    #region PRIVATE

    private string DetermineContentType()
    {
      string contentType = string.Empty;
      
      if (this.responseMetaData.Headers.ContainsKey("Content-Type") && this.responseMetaData.Headers["Content-Type"].Count == 1)
      {
        contentType = this.responseMetaData.Headers["Content-Type"][0];
      }

      return contentType;
    }


    private ContentTransferType DetermineContentTransferType()
    {
      ContentTransferType transferType = ContentTransferType.Unknown;

      try
      {
        if (this.responseMetaData.Headers.ContainsKey("Content-Length") && this.responseMetaData.Headers["Content-Length"].Count == 1)
        {
          string contentLengthStr = this.responseMetaData.Headers["Content-Length"][0].ToString();
          transferType = ContentTransferType.FixedDataLength;
        }
        else if (this.responseMetaData.Headers.ContainsKey("Transfer-Encoding") && this.responseMetaData.Headers["Transfer-Encoding"].Count == 1)
        {
          transferType = ContentTransferType.Chunked;
        }
        else
        {
          transferType = ContentTransferType.UnknownStreamLength;
        }
      }
      catch (Exception)
      {
        transferType = ContentTransferType.Unknown;
      }

      return transferType;
    }

    private int DetermineContentLength()
    {
      int contentLengthInt = -1;

      try
      {
        if (this.responseMetaData.Headers.ContainsKey("Content-Length") && this.responseMetaData.Headers["Content-Length"].Count == 1)
        {
          string contentLengthStr = this.responseMetaData.Headers["Content-Length"][0].ToString();
          contentLengthInt = int.Parse(contentLengthStr);
        }
        else if (this.responseMetaData.Headers.ContainsKey("Transfer-Encoding") && this.responseMetaData.Headers["Transfer-Encoding"].Count == 1)
        {
          contentLengthInt = -1;
        }
        else
        {
          contentLengthInt = 0;
        }
      }
      catch (Exception ex)
      {
        contentLengthInt = 0;
      }

      return contentLengthInt;
    }


    private void ReceiveClientRequestHeaders()
    {
      string httpHeader;
      this.responseMetaData.Headers = new Dictionary<string, List<string>>();

      do
      {
        httpHeader = this.streamHandler.ReadLine(false);

        if (string.IsNullOrEmpty(httpHeader) || string.IsNullOrWhiteSpace(httpHeader))
        {
          break;
        }

        if (!httpHeader.Contains(":"))
        {
          continue;
        }

        string[] httpHeaders = httpHeader.Split(new string[] { ":" }, 2, StringSplitOptions.None);
        httpHeaders[0] = httpHeaders[0].Trim();
        httpHeaders[1] = httpHeaders[1].Trim();
        
        if (!this.responseMetaData.Headers.ContainsKey(httpHeaders[0]))
        {
          this.responseMetaData.Headers.Add(httpHeaders[0], new List<string>());
        }

        this.responseMetaData.Headers[httpHeaders[0]].Add(httpHeaders[1]);
      }
      while (!string.IsNullOrWhiteSpace(httpHeader));
    }


    private bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
    {
      if (sslPolicyErrors == SslPolicyErrors.None)
      {
        return true;
      }
      
      return false;
    }


    private bool AcceptAnyServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
    {
      return true;
    }

    #endregion

  }
}
