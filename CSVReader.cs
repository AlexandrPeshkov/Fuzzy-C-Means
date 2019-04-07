using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fuzzy_C_Means
{
    public static class CSVReader
    {
        public static CSVFile ReadData(string fileName = "data.csv")
        {
            var separator = ';';
            using (var reader = new StreamReader(fileName, Encoding.GetEncoding("Windows-1251")))
            {
                List<string> headers = new List<string>();
                List<string> names = new List<string>();
                List<List<double>> matrix = new List<List<double>>();

                List<string> all = new List<string>();
                List<List<string>> data = new List<List<string>>();

                while (!reader.EndOfStream)
                {
                    all.Add(reader.ReadLine());
                }

                data = all.Select(l => l.Split(separator).ToList()).ToList();

                headers = data.First().Select(l => l.Trim()).ToList();
                names = data.Skip(1).Select(l => l.First().Trim()).ToList();
                matrix = data.Skip(1).Select(l => l.Skip(1).Select(v => Double.Parse(v)).ToList()).ToList();

    
                return new CSVFile()
                {
                    Headers = headers,
                    Names = names,
                    Matrix = matrix
                };
            }
        }
    }

    public class CSVFile
    {
        public List<string> Headers { get; set; }
        public List<string> Names { get; set; }
        public List<List<double>> Matrix { get; set; }
    }

}
