using System;
using System.IO;
using System.Text;

namespace srtm2json
{
	class Program
	{
		static void Main(string[] args)
		{
			const int SRTM_BYTES_PER_VALUE = 2;

			const long SRTM1_FILE_SIZE = 3601 * 3601 * 2;
			const long SRTM3_FILE_SIZE = 1201 * 1201 * 2;

			if (args.Length == 0)
			{
				Console.WriteLine("usage: srtm2json filename");
				return;
			}

			// parse filename argument
			string inputFile = args[0];
			if (!File.Exists(inputFile))
			{
				Console.WriteLine("{0} does not exist.", inputFile);
				Environment.Exit(-1);
			}		

			// determine resolution of file
			SRTMResolution resolution = SRTMResolution.UNKNOWN;
			FileInfo inputFileInfo = new FileInfo(inputFile);			
			long fileSize = inputFileInfo.Length;
			switch (fileSize)
			{
				case SRTM1_FILE_SIZE:
					resolution = SRTMResolution.SRTM1;
					break;
				case SRTM3_FILE_SIZE:
					resolution = SRTMResolution.SRTM3;
					break;
			}
			
			// parse input file
			StringBuilder output = new StringBuilder();			
			
			using (BinaryReader reader = new BinaryReader(new FileStream(inputFile, FileMode.Open)))
			{
				output.Append("{");
				output.Append(String.Format("\"filename\":\"{0}\",", inputFileInfo.Name.Replace(inputFileInfo.Extension, "")));
				output.Append(String.Format("\"resolution\":{0},", (int)resolution));				

				// iterate over col and row indices [0;matrixLength[ within SRTM elevation matrix
				int matrixLength = (3600 / (int)resolution) + 1;

				for (int row = 0; row < matrixLength; row++)
				{
					output.Append(String.Format("\"{0}\":[", row));
					for (int col = 0; col < matrixLength; col++)
					{
						reader.BaseStream.Seek((row * matrixLength + col) * SRTM_BYTES_PER_VALUE, SeekOrigin.Begin);						
						output.Append(ReadBigEndianShort(reader));
						output.Append(col < matrixLength - 1 ? "," : "");
					}
					output.Append("]");
					output.Append(row < matrixLength - 1 ? "," : "");
				}

				output.Append("}");
			}

			// write output file
			String outputFile = inputFileInfo.FullName.Replace(inputFileInfo.Extension, ".json");
			using (StreamWriter writer = new StreamWriter(new FileStream(outputFile, FileMode.Create), new UTF8Encoding(false)))
			{
				writer.Write(output.ToString());
			}						
		}

		private static int ReadBigEndianShort(BinaryReader reader)
		{
			int elevation = reader.ReadByte() * (2 ^ 8);
			elevation += reader.ReadByte();
			return elevation;
		}

		enum SRTMResolution
		{
			UNKNOWN = -1,
			SRTM1 = 1,
			SRTM3 = 3
		}
	}
}
