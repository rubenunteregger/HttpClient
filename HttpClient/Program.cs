namespace HttpClient
{
  using Fclp;
  using global::HttpClient.DataTypes;
  using System;
  using System.IO;
  using System.Net.Sockets;
  using System.Text;


  public class Program
  {

    #region PUBLIC

    static void Main(string[] args)
    {
      bool portSetByUser = false;
      ResponseMetaData serverResponseData = null;
      RequestConfig requestConfig = new RequestConfig();

      var parser = new FluentCommandLineParser();
      parser.IsCaseSensitive = false;

      parser.Setup<string>('h', "host")
       .Callback(item => { requestConfig.Host = item; })
       .Required()
       .WithDescription("Remote HTTP server");

      parser.Setup<string>('p', "path")
       .Callback(item => { requestConfig.Path = item; })
       .SetDefault("/")
       .WithDescription("Remote path");

      parser.Setup<bool>('v', "verbose")
       .Callback(verboseFlagSet => requestConfig.Verbose = verboseFlagSet)
       .SetDefault(false)
       .WithDescription("Make output more verbose");

      parser.Setup<string>('d', "dumpcontent")
       .Callback( dumpDataToFlagSet => { requestConfig.ContentDataOutputFile = dumpDataToFlagSet; })
       .SetDefault(string.Empty)
       .WithDescription("Write conten data: to screen if no parameter is passed\r\nWrite conten data: to FILE if FILE is passed as parameter");

      parser.Setup<bool>('s', "ssl")
       .Callback(sslFlagSet => {
         requestConfig.UseSsl = sslFlagSet;
         if (!portSetByUser)
           requestConfig.Port = requestConfig.UseSsl ? 443 : 80;         
       })
       .SetDefault(false)
       .WithDescription("Use HTTPS instead HTTP to connect to the remote server");
      
      parser.Setup<int>("port")
       .Callback(item => { requestConfig.Port = item; portSetByUser = true; })
       .WithDescription("Remote port");
            
      parser.SetupHelp("?", "help")
       .Callback(text => Console.WriteLine(text));

      ICommandLineParserResult result = parser.Parse(args);
      if (result.HasErrors == true)
      {
        Console.WriteLine("{0}\r\n\r\n", result.ErrorText);
        return;
      }
      
      if (requestConfig.Verbose)
      {
        Console.WriteLine("Connecting to {0}://{1}:{2}{3} ...", requestConfig.UseSsl ? "https" : "http", requestConfig.Host, requestConfig.Port, requestConfig.Path);
      }
      
      HttpClient httpClient = new HttpClient(requestConfig.UseSsl);
      try
      {
        httpClient.SendGetRequest(requestConfig);
        serverResponseData = httpClient.GetServerResponse();
      }
      catch (SocketException ex)
      {
        Console.WriteLine("Exception: {0}", ex.Message);
        return;
      }
      catch (Exception ex)
      {
        Console.WriteLine("Exception: {0}", ex.Message);
      }

      if (requestConfig.Verbose && serverResponseData != null)
      {
        DumpHeaders(serverResponseData);
        Console.WriteLine("> CONTENT TRANSFER TYPE: {0}", serverResponseData.ContentTransferType);
      }

      if (serverResponseData != null)
      {
        DumpData(serverResponseData, requestConfig);
      }
    }

    #endregion


    #region PRIVATE

    private static void DumpData(ResponseMetaData serverResponseData, RequestConfig requestConfig)
    {
      bool isText = serverResponseData.ContentType.ToLower().StartsWith("text/");
      bool writeToFile = !string.IsNullOrEmpty(requestConfig.ContentDataOutputFile);

      FileStream outputDataStream = null;

      if (writeToFile)
      {
        Console.WriteLine("> DMP2FILE={0}", requestConfig.ContentDataOutputFile);
        outputDataStream = File.OpenWrite(requestConfig.ContentDataOutputFile);
      }

      foreach (byte[] chunk in serverResponseData.ContentData)
      {
        if (writeToFile)
        {
          outputDataStream.Write(chunk, 0, chunk.Length);
        }

        if (requestConfig.Verbose)
        {
          if (isText)
          {
            Console.WriteLine(Encoding.UTF8.GetString(chunk));
          }
          else
          {
            Console.WriteLine("> DATA: {0} bytes received", chunk.Length);
          }
        }
      }

      if (outputDataStream != null)
      {
        outputDataStream.Close();
      }
    }


    private static  void DumpHeaders(ResponseMetaData serverResponseData)
    {
      if (serverResponseData.Headers != null && serverResponseData.Headers.Count > 0)
      {
        foreach (string key in serverResponseData.Headers.Keys)
        {
          foreach (string value in serverResponseData.Headers[key])
          {
            Console.WriteLine("> HDR: {0}={1}", key, value);
          }
        }
      }
      else
      {
        Console.WriteLine("> HDR: No headers");
      }
    }

    #endregion

  }
}
