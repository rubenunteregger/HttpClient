namespace HttpClient
{
  using Fclp;
  using global::HttpClient.DataTypes;
  using System;
  using System.Collections.Generic;
  using System.IO;
  using System.Net.Sockets;
  using System.Text;


  public class Program
  {

    #region MEMBERS

    private static ResponseMetaData serverResponseData = null;
    private static RequestConfig requestConfig = new RequestConfig();

    #endregion


    #region PUBLIC

    static void Main(string[] args)
    {
      HttpClient httpClient = null;
      bool interruptExecution = false;

      // Parse command line parameters
      interruptExecution = ParseCommandLineParameters(args);
      if (interruptExecution)
      {
        return;
      }
      
      if (requestConfig.Verbose)
      {
        Console.WriteLine("> REQUESTING {0}://{1}:{2}{3} ...", requestConfig.UseSsl ? "https" : "http", requestConfig.Host, requestConfig.Port, requestConfig.Path);
      }
      
      // Send request to server
      try
      {
        httpClient = new HttpClient(requestConfig.UseSsl);
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

      // Print server response headers and content data
      if (requestConfig.VerboseHeaders && serverResponseData != null)
      {
        DumpHeaders(serverResponseData);
      }

      // Print calculated data
      if (requestConfig.Verbose && serverResponseData != null)
      {
        Console.WriteLine("> CONTENT TRANSFER TYPE: {0}", serverResponseData.ContentTransferType);
        Console.WriteLine("> CONTENT LENGTH: {0}", serverResponseData.ContentLength);
      }

      // Print/Save server content data
      if (serverResponseData != null)
      {
        DumpData(serverResponseData, requestConfig);
      }
    }

    #endregion


    #region PRIVATE

    private static bool ParseCommandLineParameters(string[] args)
    {
      bool portAlreadySet = false;
      bool verbosityAlreadySet = false;
      bool interruptExecution = false;

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

      parser.Setup<bool>('v')
       .Callback(verboseFlagSet =>  {
         if (verboseFlagSet == true)
         {
           requestConfig.Verbose = verboseFlagSet;
           verbosityAlreadySet = true;
         }
       })
       .SetDefault(false)
       .WithDescription("Make output verbose");

      parser.Setup<bool>("vv")
       .Callback(verboseFlagSet => {
         if (verboseFlagSet == true)
         {
           requestConfig.Verbose = verboseFlagSet;
           requestConfig.VerboseHeaders = verboseFlagSet;
           verbosityAlreadySet = true;
         }
       })
       .SetDefault(false)
       .WithDescription("Make output verbose and print server response headers");

      parser.Setup<bool>("vvv")
       .Callback(verboseFlagSet => {
         if (verboseFlagSet == true)
         {
           requestConfig.Verbose = verboseFlagSet;
           requestConfig.VerboseHeaders = verboseFlagSet;
           requestConfig.VerbosePayload = verboseFlagSet;
           verbosityAlreadySet = true;
         }
       })
       .SetDefault(false)
       .WithDescription("Make output verbose and print server response headers plus server response content data");

      parser.Setup<string>('w', "write")
       .Callback(dumpDataToFlagSet => { requestConfig.ContentDataOutputFile = dumpDataToFlagSet; })
       .SetDefault(string.Empty)
       .WithDescription("Write conten data to FILE_NAME parameter");

      parser.Setup<bool>('s', "ssl")
       .Callback(sslFlagSet => {
         requestConfig.UseSsl = sslFlagSet;
         if (!portAlreadySet)
           requestConfig.Port = requestConfig.UseSsl ? 443 : 80;
       })
       .SetDefault(false)
       .WithDescription("Use HTTPS instead HTTP to connect to the remote server");

      parser.Setup<int>("port")
       .Callback(item => { requestConfig.Port = item; portAlreadySet = true; })
       .WithDescription("Remote port");
      
      parser.Setup<List<string>>("headers")
       .Callback(item => requestConfig.CustomRequestHeaders = item )
       .WithDescription("Custom HTTP request headers separated by a comma: Host: www.test.com, User-Agent: Minary");


      parser.SetupHelp("?", "help")
       .Callback(text => {
         Console.WriteLine(text);
         interruptExecution = true;
       });

      ICommandLineParserResult result = parser.Parse(args);
      if (result.HasErrors == true)
      {
        Console.WriteLine("{0}\r\n\r\n", result.ErrorText);
        interruptExecution = true;
      }

      return interruptExecution;
    }


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

        if (requestConfig.VerbosePayload)
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
