using UnityEngine;
using System.Collections;
using System.Xml.Serialization;
using System.Xml;
using System.IO;
using System.Text;

namespace CreativeSpore.RpgMapEditor
{
	public class UtilsSerialize
	{
		public static string Serialize<T>(T value) 
		{
			
			if(value == null) {
				return null;
			}
			
			XmlSerializer serializer = new XmlSerializer(typeof(T));
			
			XmlWriterSettings settings = new XmlWriterSettings();
			settings.Encoding = new UnicodeEncoding(false, false); // no BOM in a .NET string
			settings.Indent = false;
			settings.OmitXmlDeclaration = false;
			
			using(StringWriter textWriter = new StringWriter()) {
				using(XmlWriter xmlWriter = XmlWriter.Create(textWriter, settings)) {
					serializer.Serialize(xmlWriter, value);
				}
				return textWriter.ToString();
			}
		}
		
		public static T Deserialize<T>(string xml) 
		{
			
			if(string.IsNullOrEmpty(xml)) {
				return default(T);
			}
			
			XmlSerializer serializer = new XmlSerializer(typeof(T));
			
			XmlReaderSettings settings = new XmlReaderSettings();
			// No settings need modifying here
			
			using(StringReader textReader = new StringReader(xml)) {
				using(XmlReader xmlReader = XmlReader.Create(textReader, settings)) {
					return (T) serializer.Deserialize(xmlReader);
				}
			}
		}	

		public static string Zip(string value)
		{
			//Transform string into byte[]  
			byte[] byteArray = new byte[value.Length];
			int indexBA = 0;
			foreach (char item in value.ToCharArray())
			{
				byteArray[indexBA++] = (byte)item;
			}
			
			//Prepare for compress
			System.IO.MemoryStream ms = new System.IO.MemoryStream();
			System.IO.Compression.GZipStream sw = new System.IO.Compression.GZipStream(ms, System.IO.Compression.CompressionMode.Compress);
			
			//Compress
			sw.Write(byteArray, 0, byteArray.Length);
			//Close, DO NOT FLUSH cause bytes will go missing...
			sw.Close();
			
			//Transform byte[] zip data to string
			byteArray = ms.ToArray();
			System.Text.StringBuilder sB = new System.Text.StringBuilder(byteArray.Length);
			foreach (byte item in byteArray)
			{
				sB.Append((char)item);
			}
			ms.Close();
			sw.Dispose();
			ms.Dispose();
			return sB.ToString();
		}
		
		public static string UnZip(string value)
		{
			//Transform string into byte[]
			byte[] byteArray = new byte[value.Length];
			int indexBA = 0;
			foreach (char item in value.ToCharArray())
			{
				byteArray[indexBA++] = (byte)item;
			}
			
			//Prepare for decompress
			System.IO.MemoryStream ms = new System.IO.MemoryStream(byteArray);
			System.IO.Compression.GZipStream sr = new System.IO.Compression.GZipStream(ms,
			                                                                           System.IO.Compression.CompressionMode.Decompress);
			
			//Reset variable to collect uncompressed result
			byteArray = new byte[byteArray.Length];
			
			//Decompress
			int rByte = sr.Read(byteArray, 0, byteArray.Length);
			
			//Transform byte[] unzip data to string
			System.Text.StringBuilder sB = new System.Text.StringBuilder(rByte);
			//Read the number of bytes GZipStream red and do not a for each bytes in
			//resultByteArray;
			for (int i = 0; i < rByte; i++)
			{
				sB.Append((char)byteArray[i]);
			}
			sr.Close();
			ms.Close();
			sr.Dispose();
			ms.Dispose();
			return sB.ToString();
		}
	}
}