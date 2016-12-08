namespace HttpClient
{
  using System.Collections.Generic;
  using System.IO;
  using System.Text;


  public class StreamHandler
  {

    #region MEMBERS

    private MyBinaryReader readerStream;
    private BinaryWriter writerStream;

    #endregion


    #region PUBLIC

    public StreamHandler(Stream theStream, string host, int port, string path)
    {
//      this.readerStream = new MyBinaryReader(theStream, 8192, Encoding.UTF8);
      this.readerStream = new MyBinaryReader(theStream, 1048576, Encoding.UTF8);
      this.writerStream = new BinaryWriter(theStream);
    }


    public void SendRequestToServer(string requestString)
    {
      byte[] requestByteArray = Encoding.UTF8.GetBytes(requestString);

      this.writerStream.Write(requestByteArray, 0, requestByteArray.Length);
      this.writerStream.Flush();
    }


    public string ReadLine(bool keepTrailingNewline = false)
    {
      return this.readerStream.ReadLine(keepTrailingNewline);
    }


    public IEnumerable<byte[]> ReceiveResponse(int contentLength)
    {
      yield return this.ReceiveDataChunk(contentLength);
    }


    public IEnumerable<byte[]> ReceiveAllChunks()
    {
      int blockSize = 0;
      int chunkCounter = 0;
      byte[] dataChunk;

      while (true)
      {
        // Read chunk size
        string chunkLenStr = this.readerStream.ReadLine(false);

        // Break out of the loop if it is the last data packet
        if (string.IsNullOrEmpty(chunkLenStr))
        {
          break;
        }

        blockSize = int.Parse(chunkLenStr, System.Globalization.NumberStyles.HexNumber);

        if (blockSize > 0)
        {
          dataChunk = this.ReceiveDataChunk(blockSize);
          this.ReadLine();

          yield return dataChunk;
        }
        else if (blockSize == 0 || chunkLenStr == "0")
        {
          dataChunk = this.ReceiveDataChunk(blockSize);
          this.ReadLine();

          yield return dataChunk;
          break;
        }
        else
        {
         yield return null;
        }

        chunkCounter++;
      }
    }

    public IEnumerable<byte[]> ReadPacketFromStream()
    {
      int maxBlockSize = 65536;
      int chunkCounter = 0;
      byte[] dataChunk;

      while (true)
      {
        if (maxBlockSize > 0)
        {
          dataChunk = this.ReceiveDataChunk(maxBlockSize);
          yield return dataChunk;
        }
        else if (maxBlockSize == 0)
        {
          dataChunk = this.ReceiveDataChunk(maxBlockSize);
          yield return dataChunk;
          break;
        }
        else
        {
          yield return null;
        }

        chunkCounter++;
      }
    }

    #endregion


    #region PRIVATE

    private byte[] ReceiveDataChunk(int contentLength)
    {
      MemoryStream memStream = new MemoryStream();
      byte[] tmpBuffer = new byte[contentLength];
      int totalReceivedNoBytes = 0;
      int bytesRead = 0;

      while (totalReceivedNoBytes < contentLength)
      {
        int maxDataToRead = contentLength - totalReceivedNoBytes;
        bytesRead = this.readerStream.Read(tmpBuffer, 0, maxDataToRead);

        if (bytesRead <= 0)
        {
          break;
        }

        if (totalReceivedNoBytes + bytesRead > contentLength)
        {
          int newPacketSize = bytesRead - ((totalReceivedNoBytes + bytesRead) - contentLength);
          totalReceivedNoBytes += newPacketSize;
          memStream.Write(tmpBuffer, 0, bytesRead);
          break;
        }
        else
        {
          totalReceivedNoBytes += bytesRead;
          memStream.Write(tmpBuffer, 0, bytesRead);
        }
      }

      return memStream.ToArray();
    }

    #endregion

  }
}
